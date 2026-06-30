/*
 * ScriptBinder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Private
{
    //
    // HACK: This class derives from the Binder type solely for the purpose
    //       of allowing other components to pass it around in place of an
    //       actual Binder object.  The .NET Framework should have defined
    //       a formal IBinder interface and had the built-in Binder classes
    //       implement it.  The methods of the base class are never called.
    //       When this class needs to fallback to "default" binding behavior,
    //       it uses the Binder object contained in the "binder" field of
    //       this class, not the methods of the base class.
    //
    /// <summary>
    /// This class is the default implementation of the
    /// <see cref="IScriptBinder" /> interface used by the marshalling
    /// subsystem.  It derives from <see cref="Binder" /> so that it can be
    /// passed wherever a <see cref="Binder" /> is required, and it manages the
    /// dynamic string-to-type and type-to-string conversion callbacks used when
    /// converting values between the script engine and the CLR.  When no custom
    /// conversion applies, it falls back to a configured fallback binder and/or
    /// the default binder.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0e087802-e964-4900-b687-79bbc4332079")]
    internal sealed class ScriptBinder : Binder, IScriptBinder, IHaveInterpreter
    {
        #region Private Constants
        //
        // NOTE: The method attributes of static constructors in the CLR.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The combination of method attributes that identifies a static
        /// constructor (type initializer) in the CLR.  It is used to detect,
        /// and by default disallow, calls to such constructors.
        /// </summary>
        private static MethodAttributes cctorMethodAttributes =
            MethodAttributes.Static | MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: What interpreter do we belong to?
        //
        /// <summary>
        /// The interpreter that this binder belongs to.
        /// </summary>
        private Interpreter interpreter;

        //
        // NOTE: What is the default binder?  Normally, this simply returns
        //       the value of Type.DefaultBinder.
        //
        /// <summary>
        /// The default binder used when no fallback binder is available.
        /// Normally, this simply wraps the value of
        /// <see cref="Type.DefaultBinder" />.
        /// </summary>
        private IBinder defaultBinder;

        //
        // NOTE: What is our fallback binder?
        //
        /// <summary>
        /// The fallback binder to use before resorting to the default binder.
        /// This may be null.
        /// </summary>
        private IBinder fallbackBinder;

        //
        // NOTE: What is our parent binder?  This will almost always be null
        //       for the default IScriptBinder implementation.
        //
        /// <summary>
        /// The parent script binder, if any.  This will almost always be null
        /// for the default <see cref="IScriptBinder" /> implementation.
        /// </summary>
        private IScriptBinder parentBinder;

        //
        // NOTE: What are the binding flags when they are not specified by a
        //       caller?
        //
        /// <summary>
        /// The binding flags to use when they are not otherwise specified by a
        /// caller.
        /// </summary>
        private BindingFlags defaultBindingFlags;

        //
        // NOTE: Is this binder operating in "debug" mode?
        //
        /// <summary>
        /// When non-zero, this binder is operating in "debug" mode and emits
        /// extra diagnostic trace output.
        /// </summary>
        private bool debug;

        //
        // NOTE: What dynamic string-to-type conversions do we support?
        //
        /// <summary>
        /// The dynamic string-to-type conversion callbacks supported by this
        /// binder, keyed by target type.
        /// </summary>
        private TypeChangeTypeCallbackDictionary changeTypes;

        //
        // NOTE: What dynamic type-to-string conversions do we support?
        //
        /// <summary>
        /// The dynamic type-to-string conversion callbacks supported by this
        /// binder, keyed by source type.
        /// </summary>
        private TypeToStringCallbackDictionary toStringTypes;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class, initializing the default
        /// binder, default binding flags, and the dynamic conversion callback
        /// dictionaries.
        /// </summary>
        /// <param name="noDefaultBinder">
        /// Non-zero to skip creating a default binder; otherwise, a default
        /// binder wrapping <see cref="Type.DefaultBinder" /> is created.
        /// </param>
        private ScriptBinder(
            bool noDefaultBinder
            )
        {
            defaultBinder = !noDefaultBinder ?
                new DefaultBinder(Type.DefaultBinder) : null;

            defaultBindingFlags = ObjectOps.GetBindingFlags(
                MetaBindingFlags.Default, true);

            changeTypes = new TypeChangeTypeCallbackDictionary(
                ConversionOps.Dynamic.ChangeTypes);

            toStringTypes = new TypeToStringCallbackDictionary(
                ConversionOps.Dynamic.ToStringTypes);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class for the specified
        /// interpreter, with an optional fallback binder and debug mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that this binder will belong to.
        /// </param>
        /// <param name="fallbackBinder">
        /// The fallback binder to use before the default binder, or null for
        /// none.
        /// </param>
        /// <param name="noDefaultBinder">
        /// Non-zero to skip creating a default binder; otherwise, a default
        /// binder is created.
        /// </param>
        /// <param name="debug">
        /// Non-zero to enable "debug" mode, which emits extra diagnostic trace
        /// output.
        /// </param>
        public ScriptBinder(
            Interpreter interpreter,
            IBinder fallbackBinder, /* MAY BE NULL */
            bool noDefaultBinder,
            bool debug
            )
            : this(noDefaultBinder)
        {
            this.interpreter = interpreter;
            this.fallbackBinder = fallbackBinder;
            this.debug = debug;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method examines a value to see if it is a
        /// <see cref="MarshalClientData" /> wrapper and, if so, unpacks the
        /// wrapped value along with its associated options and marshal flags.
        /// </summary>
        /// <param name="value">
        /// On input, the value to examine.  On output, if the input was a
        /// <see cref="MarshalClientData" /> wrapper, this is the unwrapped data
        /// value; otherwise, it is left unchanged.
        /// </param>
        /// <param name="marshalClientData">
        /// Upon return, the <see cref="MarshalClientData" /> wrapper that was
        /// unpacked, or null if the value was not a wrapper.
        /// </param>
        /// <param name="options">
        /// Upon return, the options carried by the wrapper, or null if the
        /// value was not a wrapper.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, the marshal flags carried by the wrapper, or
        /// <see cref="MarshalFlags.None" /> if the value was not a wrapper.
        /// </param>
        private static void MaybeUnpackMarshalClientData(
            ref object value,                        /* in, out */
            out MarshalClientData marshalClientData, /* out */
            out OptionDictionary options,            /* out */
            out MarshalFlags marshalFlags            /* out */
            )
        {
            marshalClientData = value as MarshalClientData;

            if (marshalClientData != null)
            {
                value = marshalClientData.Data;
                options = marshalClientData.Options;
                marshalFlags = marshalClientData.MarshalFlags;
            }
            else
            {
                options = null;
                marshalFlags = MarshalFlags.None;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the results of a type conversion back into a
        /// <see cref="MarshalClientData" /> wrapper and updates the marshal
        /// flags to reflect those results.
        /// </summary>
        /// <param name="marshalClientData">
        /// The <see cref="MarshalClientData" /> wrapper to update, or null if
        /// there is none.
        /// </param>
        /// <param name="changeTypeData">
        /// The type conversion helper object containing the new value, options,
        /// and marshal flags, or null if there is none.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, the marshal flags taken from <paramref name="changeTypeData" />.
        /// </param>
        private static void MaybeUpdateMarshalClientData(
            MarshalClientData marshalClientData, /* in */
            IChangeTypeData changeTypeData,      /* in */
            ref MarshalFlags marshalFlags        /* out */
            )
        {
            if (changeTypeData != null)
            {
                marshalFlags = changeTypeData.MarshalFlags;

                if (marshalClientData != null)
                {
                    marshalClientData.Options = changeTypeData.Options;
                    marshalClientData.MarshalFlags = marshalFlags;
                    marshalClientData.Data = changeTypeData.NewValue;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fetches the binder client data associated with the
        /// active interpreter on the current thread.
        /// </summary>
        /// <returns>
        /// The client data for the active <see cref="BinderClientData" />, or
        /// null if none is available.
        /// </returns>
        private static IClientData GetBinderClientData()
        {
            IAnyPair<Interpreter, IClientData> anyPair =
                Interpreter.GetActivePair(typeof(BinderClientData));

            return (anyPair != null) ? anyPair.Y : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fetches the binder client data associated with the
        /// active interpreter on the current thread, optionally supplying its
        /// options when none were provided by the caller.
        /// </summary>
        /// <param name="options">
        /// On input, the caller-supplied options, if any.  On output, if no
        /// options were supplied and binder client data is available, this is
        /// set to the options carried by that binder client data.
        /// </param>
        /// <param name="clientData">
        /// Upon return, the client data carried by the active binder client
        /// data, or null if none is available.
        /// </param>
        private static void GetBinderClientData(
            ref OptionDictionary options, /* in, out */
            out IClientData clientData    /* out */
            )
        {
            BinderClientData binderClientData =
                GetBinderClientData() as BinderClientData;

            if (binderClientData != null)
            {
                if (options == null)
                    options = binderClientData.Options;

                clientData = binderClientData.ClientData;
            }
            else
            {
                clientData = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely invokes a type-to-string conversion callback,
        /// trapping any exception it raises and reporting it as an error.
        /// </summary>
        /// <param name="callback">
        /// The type-to-string conversion callback to invoke.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context for the conversion.
        /// </param>
        /// <param name="type">
        /// The type of the value being converted to a string.
        /// </param>
        /// <param name="value">
        /// The value to convert to a string.
        /// </param>
        /// <param name="options">
        /// The options that control the conversion, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags that control the conversion; this may be
        /// modified by the callback.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the resulting string representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode InvokeToStringCallback(
            ToStringCallback callback,     /* in */
            Interpreter interpreter,       /* in */
            Type type,                     /* in */
            object value,                  /* in */
            OptionDictionary options,      /* in */
            CultureInfo cultureInfo,       /* in */
            IClientData clientData,        /* in */
            ref MarshalFlags marshalFlags, /* in, out */
            ref string text,               /* out */
            ref Result error               /* out */
            )
        {
            try
            {
                if (callback == null)
                {
                    error = "invalid callback";
                    return ReturnCode.Error;
                }

                return callback.Invoke(
                    interpreter, type, value, options, cultureInfo,
                    clientData, ref marshalFlags, ref text, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely invokes a string-to-type conversion callback,
        /// trapping any exception it raises and reporting it as an error.
        /// </summary>
        /// <param name="callback">
        /// The string-to-type conversion callback to invoke.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context for the conversion.
        /// </param>
        /// <param name="type">
        /// The type to convert the string value into.
        /// </param>
        /// <param name="text">
        /// The string value to convert.
        /// </param>
        /// <param name="options">
        /// The options that control the conversion, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags that control the conversion; this may be
        /// modified by the callback.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting converted value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode InvokeChangeTypeCallback(
            ChangeTypeCallback callback,   /* in */
            Interpreter interpreter,       /* in */
            Type type,                     /* in */
            string text,                   /* in */
            OptionDictionary options,      /* in */
            CultureInfo cultureInfo,       /* in */
            IClientData clientData,        /* in */
            ref MarshalFlags marshalFlags, /* in, out */
            ref object value,              /* out */
            ref Result error               /* out */
            )
        {
            try
            {
                if (callback == null)
                {
                    error = "invalid callback";
                    return ReturnCode.Error;
                }

                return callback.Invoke(
                    interpreter, type, text, options, cultureInfo,
                    clientData, ref marshalFlags, ref value, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// Gets or sets the interpreter that this binder belongs to.
        /// </summary>
        public Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IScriptBinder Members
        /// <summary>
        /// Gets or sets the default binder used when no fallback binder is
        /// available.
        /// </summary>
        public IBinder DefaultBinder
        {
            get { return defaultBinder; }
            set { defaultBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the fallback binder used before resorting to the
        /// default binder.  This may be null.
        /// </summary>
        public IBinder FallbackBinder
        {
            get { return fallbackBinder; }
            set { fallbackBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the parent script binder, if any.
        /// </summary>
        public IScriptBinder ParentBinder
        {
            get { return parentBinder; }
            set { parentBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the binding flags to use when they are not otherwise
        /// specified by a caller.
        /// </summary>
        public BindingFlags DefaultBindingFlags
        {
            get { return defaultBindingFlags; }
            set { defaultBindingFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this binder is operating in
        /// "debug" mode, which emits extra diagnostic trace output.
        /// </summary>
        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is an extensibility point for use with custom IScriptBinder
        //       implementations.  By default, it disallows static constructors
        //       from being called.
        //
        /// <summary>
        /// This method determines whether the specified method is allowed to be
        /// invoked through this binder.  By default, it disallows static
        /// constructors (type initializers) from being called.
        /// </summary>
        /// <param name="method">
        /// The method to check, or null.
        /// </param>
        /// <returns>
        /// True if the method is allowed to be invoked; otherwise, false.
        /// </returns>
        public bool IsAllowed(
            MethodBase method /* in */
            )
        {
            if (method == null)
                return true; /* TODO: Good default?  Caller should check. */

            if (!FlagOps.HasFlags(
                    method.Attributes, cctorMethodAttributes, true))
            {
                return true;
            }

            if (!SharedStringOps.SystemEquals(
                    method.Name, ConstructorInfo.TypeConstructorName))
            {
                return true;
            }

            ConstructorInfo constructorInfo = method as ConstructorInfo;

            if (constructorInfo == null)
                return true;

            ParameterInfo[] parameterInfo = constructorInfo.GetParameters();

            if ((parameterInfo == null) || (parameterInfo.Length != 0))
                return true;

            //
            // NOTE: If we reach this point, the method is not allowed because
            //       it fits all the criteria for a static constructor in the
            //       CLR.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is an extensibility point for use with custom IScriptBinder
        //       implementations.  By default, it does nothing.
        //
        /// <summary>
        /// This method is an extensibility point that resolves a string into a
        /// typed object instance.  The default implementation does nothing and
        /// defers to the built-in semantics.
        /// </summary>
        /// <param name="text">
        /// The string to resolve into an object instance.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to resolve the object.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags that control how the object is resolved.
        /// </param>
        /// <param name="objectType">
        /// The expected type of the object instance.
        /// </param>
        /// <param name="proxyType">
        /// The proxy type to use, if any.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that control how the string is interpreted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during resolution, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved typed object instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Continue" /> to use the built-in semantics;
        /// otherwise, a code indicating the result of custom resolution.
        /// </returns>
        public ReturnCode GetObject(
            string text,               /* in */
            TypeList types,            /* in */
            AppDomain appDomain,       /* in */
            BindingFlags bindingFlags, /* in */
            Type objectType,           /* in */
            Type proxyType,            /* in */
            ValueFlags valueFlags,     /* in */
            CultureInfo cultureInfo,   /* in */
            ref ITypedInstance value,  /* out */
            ref Result error           /* out */
            )
        {
            //
            // NOTE: Do nothing and return Continue.  The built-in semantics
            //       will be used.
            //
            return ReturnCode.Continue;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is an extensibility point for use with custom IScriptBinder
        //       implementations.  By default, it does nothing.
        //
        /// <summary>
        /// This method is an extensibility point that resolves a string into a
        /// typed member of an instance.  The default implementation does
        /// nothing and defers to the built-in semantics.
        /// </summary>
        /// <param name="text">
        /// The string naming the member to resolve.
        /// </param>
        /// <param name="typedInstance">
        /// The typed instance whose member is being resolved.
        /// </param>
        /// <param name="memberTypes">
        /// The kinds of members to consider.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags that control how the member is resolved.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that control how the string is interpreted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during resolution, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved typed member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Continue" /> to use the built-in semantics;
        /// otherwise, a code indicating the result of custom resolution.
        /// </returns>
        public ReturnCode GetMember(
            string text,                  /* in */
            ITypedInstance typedInstance, /* in */
            MemberTypes memberTypes,      /* in */
            BindingFlags bindingFlags,    /* in */
            ValueFlags valueFlags,        /* in */
            CultureInfo cultureInfo,      /* in */
            ref ITypedMember value,       /* out */
            ref Result error              /* out */
            )
        {
            //
            // NOTE: Do nothing and return Continue.  The built-in semantics
            //       will be used.
            //
            return ReturnCode.Continue;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value is assignable to,
        /// or otherwise compatible with, the specified type.
        /// </summary>
        /// <param name="value">
        /// The value to test, which may be null.
        /// </param>
        /// <param name="type">
        /// The type to test the value against.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags that influence the comparison.
        /// </param>
        /// <returns>
        /// True if the value matches the type; otherwise, false.
        /// </returns>
        public bool DoesMatchType(
            object value,             /* in */
            Type type,                /* in */
            MarshalFlags marshalFlags /* in */
            )
        {
            if (type != null)
            {
                if (value != null)
                {
                    Type objectType = AppDomainOps.MaybeGetType(value);

                    return MarshalOps.IsSameReferenceType(objectType, type, marshalFlags) ||
                        MarshalOps.IsSameValueType(objectType, type);
                }
                else if (!MarshalOps.IsValueType(type) || MarshalOps.IsNullableType(type))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified callback is one of the
        /// built-in (core) conversion callbacks, whether it converts to a
        /// string or from a string.
        /// </summary>
        /// <param name="callback">
        /// The callback delegate to test.
        /// </param>
        /// <returns>
        /// True if the callback is a built-in conversion callback; otherwise,
        /// false.
        /// </returns>
        public bool IsCoreCallback(
            Delegate callback /* in */
            )
        {
            return IsCoreToStringCallback(callback as ToStringCallback) ||
                IsCoreChangeTypeCallback(callback as ChangeTypeCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type-to-string callback
        /// is the built-in callback that produces a string list representation.
        /// </summary>
        /// <param name="callback">
        /// The type-to-string callback to test.
        /// </param>
        /// <returns>
        /// True if the callback is the built-in string-list conversion
        /// callback; otherwise, false.
        /// </returns>
        public bool IsCoreStringListToStringCallback(
            ToStringCallback callback /* in */
            )
        {
            //
            // HACK: There is only one method that handles the conversion to a
            //       String.
            //
            return (callback == ConversionOps.Dynamic._ToString.FromDateTime);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string-to-type callback
        /// is the built-in callback that converts a string into a string list.
        /// </summary>
        /// <param name="callback">
        /// The string-to-type callback to test.
        /// </param>
        /// <returns>
        /// True if the callback is the built-in string-list conversion
        /// callback; otherwise, false.
        /// </returns>
        public bool IsCoreStringListChangeTypeCallback(
            ChangeTypeCallback callback /* in */
            )
        {
            //
            // HACK: There is only one method that handles the conversion to a
            //       StringList.
            //
            return (callback == ConversionOps.Dynamic.ChangeType.ToStringList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any type-to-string conversion
        /// callbacks are available.
        /// </summary>
        /// <returns>
        /// True if type-to-string conversions are available; otherwise, false.
        /// </returns>
        public bool HasToStringTypes()
        {
            return (toStringTypes != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the list of types for which type-to-string
        /// conversion callbacks are registered.
        /// </summary>
        /// <param name="types">
        /// Upon success, receives the list of types that have a registered
        /// type-to-string conversion callback.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ListToStrings(
            ref TypeList types, /* out */
            ref Result error    /* out */
            )
        {
            if (this.toStringTypes == null)
            {
                error = "types not available";
                return ReturnCode.Error;
            }

            types = new TypeList(this.toStringTypes.Keys);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type-to-string callback
        /// is implemented by the built-in dynamic type conversion class.
        /// </summary>
        /// <param name="callback">
        /// The type-to-string callback to test.
        /// </param>
        /// <returns>
        /// True if the callback is a built-in type-to-string callback;
        /// otherwise, false.
        /// </returns>
        public bool IsCoreToStringCallback(
            ToStringCallback callback /* in */
            )
        {
            if (callback != null)
            {
                MethodInfo methodInfo = callback.Method;

                if (methodInfo != null)
                {
                    Type type = methodInfo.DeclaringType;

                    if (type == typeof(ConversionOps.Dynamic._ToString))
                        return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a type-to-string conversion callback
        /// is registered for the specified type.
        /// </summary>
        /// <param name="type">
        /// The type to look up.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to require that the callback be a built-in (primitive)
        /// conversion; otherwise, any registered callback qualifies.
        /// </param>
        /// <returns>
        /// True if a matching type-to-string callback is registered; otherwise,
        /// false.
        /// </returns>
        public bool HasToStringCallback(
            Type type,     /* in */
            bool primitive /* in */
            )
        {
            ToStringCallback callback = null;

            return HasToStringCallback(type, primitive, ref callback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a type-to-string conversion callback
        /// is registered for the specified type and, if so, returns it.
        /// </summary>
        /// <param name="type">
        /// The type to look up.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to require that the callback be a built-in (primitive)
        /// conversion; otherwise, any registered callback qualifies.
        /// </param>
        /// <param name="callback">
        /// Upon return, receives the matching type-to-string callback, if any.
        /// </param>
        /// <returns>
        /// True if a matching type-to-string callback is registered; otherwise,
        /// false.
        /// </returns>
        public bool HasToStringCallback(
            Type type,                    /* in */
            bool primitive,               /* in */
            ref ToStringCallback callback /* out */
            )
        {
            if (type != null)
            {
                if (toStringTypes != null)
                {
                    if (toStringTypes.TryGetValue(type, out callback))
                    {
                        if (primitive)
                        {
                            //
                            // NOTE: If the callback is null then the type entry is
                            //       simply ignored (i.e. because it is invalid).
                            //       Also, if the callback is not implemented by the
                            //       built-in dynamic type conversions class, it is
                            //       not considered to be a primitive type.
                            //
                            if ((callback != null) &&
                                IsCoreToStringCallback(callback))
                            {
                                return true;
                            }
                        }
                        else if (callback != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method registers a type-to-string conversion callback for the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type to associate with the callback.
        /// </param>
        /// <param name="callback">
        /// The type-to-string conversion callback to register.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode AddToStringCallback(
            Type type,                 /* in */
            ToStringCallback callback, /* in */
            ref Result error           /* out */
            )
        {
            if (toStringTypes == null)
            {
                error = "types not available";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (callback == null)
            {
                error = "invalid callback";
                return ReturnCode.Error;
            }

            if (toStringTypes.ContainsKey(type))
            {
                error = "type already exists";
                return ReturnCode.Error;
            }

            toStringTypes.Add(type, callback);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unregisters the type-to-string conversion callback
        /// associated with the specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose callback is to be removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RemoveToStringCallback(
            Type type,       /* in */
            ref Result error /* out */
            )
        {
            if (toStringTypes == null)
            {
                error = "types not available";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (!toStringTypes.ContainsKey(type))
            {
                error = "type not found";
                return ReturnCode.Error;
            }

            if (toStringTypes.Remove(type))
                return ReturnCode.Ok;

            error = "could not remove type";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes a type-to-string conversion callback using this
        /// binder's interpreter as the context.
        /// </summary>
        /// <param name="callback">
        /// The type-to-string conversion callback to invoke.
        /// </param>
        /// <param name="type">
        /// The type of the value being converted to a string.
        /// </param>
        /// <param name="value">
        /// The value to convert to a string.
        /// </param>
        /// <param name="options">
        /// The options that control the conversion, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags that control the conversion; this may be
        /// modified by the callback.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the resulting string representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode InvokeToStringCallback(
            ToStringCallback callback,     /* in */
            Type type,                     /* in */
            object value,                  /* in */
            OptionDictionary options,      /* in */
            CultureInfo cultureInfo,       /* in */
            IClientData clientData,        /* in */
            ref MarshalFlags marshalFlags, /* in, out */
            ref string text,               /* out */
            ref Result error               /* out */
            )
        {
            return InvokeToStringCallback(
                callback, interpreter, type, value, options, cultureInfo,
                clientData, ref marshalFlags, ref text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value described by the specified type
        /// conversion helper into its string representation, applying any
        /// registered type-to-string conversion callback for the value's type.
        /// </summary>
        /// <param name="changeTypeData">
        /// The type conversion helper object describing the value to convert;
        /// its result fields are updated in place.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ToString(
            IChangeTypeData changeTypeData, /* in, out */
            ref Result error                /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (changeTypeData != null)
            {
                //
                // NOTE: Grab the primary data we need from the helper object.
                //
                Type type = changeTypeData.Type;
                object value = changeTypeData.OldValue;
                MarshalFlags marshalFlags = changeTypeData.MarshalFlags;
                OptionDictionary options = changeTypeData.Options;
                CultureInfo cultureInfo = changeTypeData.CultureInfo;
                IClientData clientData = changeTypeData.ClientData;

                //
                // NOTE: This will be reused whenever we need to lookup and/or
                //       invoke a ToString callback.
                //
                ToStringCallback callback = null;

                //
                // NOTE: See if there is a ToString callback for the specified
                //       type.  If so, invoke it.
                //
                if ((code == ReturnCode.Ok) &&
                    HasToStringCallback(type, false, ref callback))
                {
                    changeTypeData.Attempted = true;

                    string text = null;

                    code = InvokeToStringCallback(
                        callback, type, value, options, cultureInfo,
                        clientData, ref marshalFlags, ref text,
                        ref error);

                    if (code == ReturnCode.Ok)
                    {
                        value = text;
                        changeTypeData.Converted = true;
                    }
                }

                //
                // NOTE: Update the marshal flags now.
                //
                changeTypeData.MarshalFlags = marshalFlags;

                //
                // NOTE: Store the new value back into the helper object.
                //
                changeTypeData.NewValue = value;

                //
                // NOTE: Do we consider the type conversions performed, if any, to
                //       be a success (i.e. do the types more-or-less match now)?
                //
                changeTypeData.DoesMatch = DoesMatchType(
                    value, typeof(string), marshalFlags);

                //
                // NOTE: In debug mode, show some diagnostic output.
                //
                if (debug)
                {
                    object oldValue = changeTypeData.OldValue;
                    object newValue = changeTypeData.NewValue;

                    TraceOps.DebugTrace(String.Format(
                        "ToString: caller = {0}, oldValue = {1}, " +
                        "oldType = {2}, marshalFlags = {3}, newValue = {4}, newType = {5}, " +
                        "fromType = {6}, options = {7}, cultureInfo = {8}, clientData = {9}, " +
                        "callback = {10}, wasObject = {11}, attempted = {12}, converted = {13}, " +
                        "doesMatch = {14}, code = {15}, error = {16}",
                        FormatOps.DisplayString(changeTypeData.Caller),
                        FormatOps.WrapOrNull(true, true, oldValue),
                        FormatOps.TypeName(oldValue),
                        FormatOps.WrapOrNull(changeTypeData.MarshalFlags),
                        FormatOps.WrapOrNull(true, true, newValue),
                        FormatOps.TypeName(newValue),
                        FormatOps.WrapOrNull(type), FormatOps.WrapOrNull(true, true, options),
                        FormatOps.WrapOrNull(cultureInfo), FormatOps.WrapOrNull(clientData),
                        FormatOps.WrapOrNull(FormatOps.DelegateName(callback)),
                        changeTypeData.WasObject, changeTypeData.Attempted,
                        changeTypeData.Converted, changeTypeData.DoesMatch, code,
                        FormatOps.WrapOrNull(true, true, error)),
                        typeof(ScriptBinder).Name, TracePriority.MarshalDebug);
                }
            }
            else
            {
                error = "invalid change type data";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any string-to-type conversion
        /// callbacks are available.
        /// </summary>
        /// <returns>
        /// True if string-to-type conversions are available; otherwise, false.
        /// </returns>
        public bool HasChangeTypes()
        {
            return (changeTypes != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the list of types for which string-to-type
        /// conversion callbacks are registered.
        /// </summary>
        /// <param name="types">
        /// Upon success, receives the list of types that have a registered
        /// string-to-type conversion callback.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ListChangeTypes(
            ref TypeList types, /* out */
            ref Result error    /* out */
            )
        {
            if (this.changeTypes == null)
            {
                error = "types not available";
                return ReturnCode.Error;
            }

            types = new TypeList(this.changeTypes.Keys);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string-to-type callback
        /// is implemented by the built-in dynamic type conversion class.
        /// </summary>
        /// <param name="callback">
        /// The string-to-type callback to test.
        /// </param>
        /// <returns>
        /// True if the callback is a built-in string-to-type callback;
        /// otherwise, false.
        /// </returns>
        public bool IsCoreChangeTypeCallback(
            ChangeTypeCallback callback /* in */
            )
        {
            if (callback != null)
            {
                MethodInfo methodInfo = callback.Method;

                if (methodInfo != null)
                {
                    Type type = methodInfo.DeclaringType;

                    if (type == typeof(ConversionOps.Dynamic.ChangeType))
                        return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a string-to-type conversion callback
        /// is registered for the specified type.
        /// </summary>
        /// <param name="type">
        /// The type to look up.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to require that the callback be a built-in (primitive)
        /// conversion; otherwise, any registered callback qualifies.
        /// </param>
        /// <returns>
        /// True if a matching string-to-type callback is registered; otherwise,
        /// false.
        /// </returns>
        public bool HasChangeTypeCallback(
            Type type,     /* in */
            bool primitive /* in */
            )
        {
            ChangeTypeCallback callback = null;

            return HasChangeTypeCallback(type, primitive, ref callback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a string-to-type conversion callback
        /// is registered for the specified type and, if so, returns it.
        /// </summary>
        /// <param name="type">
        /// The type to look up.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to require that the callback be a built-in (primitive)
        /// conversion that does not deal with opaque object or interpreter
        /// handles; otherwise, any registered callback qualifies.
        /// </param>
        /// <param name="callback">
        /// Upon return, receives the matching string-to-type callback, if any.
        /// </param>
        /// <returns>
        /// True if a matching string-to-type callback is registered; otherwise,
        /// false.
        /// </returns>
        public bool HasChangeTypeCallback(
            Type type,                      /* in */
            bool primitive,                 /* in */
            ref ChangeTypeCallback callback /* out */
            )
        {
            if (type != null)
            {
                if (changeTypes != null)
                {
                    if (changeTypes.TryGetValue(type, out callback))
                    {
                        if (primitive)
                        {
                            //
                            // NOTE: If the callback is null then the type entry is
                            //       simply ignored (i.e. because it is invalid).
                            //       Also, if the callback deals with opaque object
                            //       or interpreter handles, or it is not implemented
                            //       by the built-in dynamic type conversion class,
                            //       it is not considered to be a primitive type.
                            //
                            if ((callback != null) &&
                                (callback != ConversionOps.Dynamic.ChangeType.ToObject) &&
                                (callback != ConversionOps.Dynamic.ChangeType.ToInterpreter) &&
                                IsCoreChangeTypeCallback(callback))
                            {
                                return true;
                            }
                        }
                        else if (callback != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method registers a string-to-type conversion callback for the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type to associate with the callback.
        /// </param>
        /// <param name="callback">
        /// The string-to-type conversion callback to register.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode AddChangeTypeCallback(
            Type type,                   /* in */
            ChangeTypeCallback callback, /* in */
            ref Result error             /* out */
            )
        {
            if (changeTypes == null)
            {
                error = "types not available";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (callback == null)
            {
                error = "invalid callback";
                return ReturnCode.Error;
            }

            if (changeTypes.ContainsKey(type))
            {
                error = "type already exists";
                return ReturnCode.Error;
            }

            changeTypes.Add(type, callback);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unregisters the string-to-type conversion callback
        /// associated with the specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose callback is to be removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RemoveChangeTypeCallback(
            Type type,       /* in */
            ref Result error /* out */
            )
        {
            if (changeTypes == null)
            {
                error = "types not available";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (!changeTypes.ContainsKey(type))
            {
                error = "type not found";
                return ReturnCode.Error;
            }

            if (changeTypes.Remove(type))
                return ReturnCode.Ok;

            error = "could not remove type";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes a string-to-type conversion callback using this
        /// binder's interpreter as the context.
        /// </summary>
        /// <param name="callback">
        /// The string-to-type conversion callback to invoke.
        /// </param>
        /// <param name="type">
        /// The type to convert the string value into.
        /// </param>
        /// <param name="text">
        /// The string value to convert.
        /// </param>
        /// <param name="options">
        /// The options that control the conversion, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags that control the conversion; this may be
        /// modified by the callback.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting converted value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode InvokeChangeTypeCallback(
            ChangeTypeCallback callback,   /* in */
            Type type,                     /* in */
            string text,                   /* in */
            OptionDictionary options,      /* in */
            CultureInfo cultureInfo,       /* in */
            IClientData clientData,        /* in */
            ref MarshalFlags marshalFlags, /* in, out */
            ref object value,              /* out */
            ref Result error               /* out */
            )
        {
            return InvokeChangeTypeCallback(
                callback, interpreter, type, text, options, cultureInfo,
                clientData, ref marshalFlags, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value described by the specified type
        /// conversion helper into the requested type, trying any registered
        /// string-to-type callbacks, opaque object handle lookups, enum and
        /// primitive conversions, and conversion operators in turn.
        /// </summary>
        /// <param name="changeTypeData">
        /// The type conversion helper object describing the value to convert
        /// and the target type; its result fields are updated in place.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ChangeType(
            IChangeTypeData changeTypeData, /* in, out */
            ref Result error                /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (changeTypeData != null)
            {
                //
                // NOTE: Grab the primary data we need from the helper object.
                //
                Type type = changeTypeData.Type;
                object value = changeTypeData.OldValue;
                MarshalFlags marshalFlags = changeTypeData.MarshalFlags;
                OptionDictionary options = changeTypeData.Options;
                CultureInfo cultureInfo = changeTypeData.CultureInfo;
                IClientData clientData = changeTypeData.ClientData;

                //
                // NOTE: This will be reused whenever we need to lookup and/or invoke
                //       a ChangeType callback.
                //
                ChangeTypeCallback callback = null;

                //
                // NOTE: First, try to find an underlying object based on the string,
                //       which may be an opaque object handle.
                //
                if ((code == ReturnCode.Ok) && (value is string) &&
                    !changeTypeData.NoHandle &&
                    HasChangeTypeCallback(typeof(object), false, ref callback) &&
                    (interpreter != null) &&
                    (interpreter.DoesObjectExist((string)value) == ReturnCode.Ok))
                {
                    changeTypeData.Attempted = true;

                    code = InvokeChangeTypeCallback(
                        callback, type, (string)value, options, cultureInfo,
                        clientData, ref marshalFlags, ref value, ref error);

                    if (code == ReturnCode.Ok)
                        changeTypeData.WasObject = true;
                }

                //
                // NOTE: Next, see if the underlying object value requires further
                //       conversion.
                //
                if ((code == ReturnCode.Ok) && (value is string) &&
                    (type != typeof(object)) &&
                    HasChangeTypeCallback(type, false, ref callback))
                {
                    changeTypeData.Attempted = true;

                    code = InvokeChangeTypeCallback(
                        callback, type, (string)value, options, cultureInfo,
                        clientData, ref marshalFlags, ref value, ref error);

                    if (code == ReturnCode.Ok)
                        changeTypeData.Converted = true;
                }
                else if ((code == ReturnCode.Ok) && (value is string) &&
                    (type != null) && MarshalOps.IsEnumType(type, true, true) &&
                    HasChangeTypeCallback(typeof(Enum), false, ref callback))
                {
                    changeTypeData.Attempted = true;

                    code = InvokeChangeTypeCallback(
                        callback, type, (string)value, options, cultureInfo,
                        clientData, ref marshalFlags, ref value, ref error);

                    if (code == ReturnCode.Ok)
                        changeTypeData.Converted = true;
                }
                else if ((code == ReturnCode.Ok) && (value is string) &&
                    (type != null) && MarshalOps.IsPrimitiveType(type, true) &&
                    HasChangeTypeCallback(typeof(ValueType), false, ref callback))
                {
                    changeTypeData.Attempted = true;

                    code = InvokeChangeTypeCallback(
                        callback, type, (string)value, options, cultureInfo,
                        clientData, ref marshalFlags, ref value, ref error);

                    if (code == ReturnCode.Ok)
                        changeTypeData.Converted = true;
                }
                else if ((code == ReturnCode.Ok) && (value is string) &&
                    ConversionOps.IsDelegateType(type, false) &&
                    HasChangeTypeCallback(typeof(Delegate), false, ref callback))
                {
                    changeTypeData.Attempted = true;

                    code = InvokeChangeTypeCallback(
                        callback, type, (string)value, options, cultureInfo,
                        clientData, ref marshalFlags, ref value, ref error);

                    if (code == ReturnCode.Ok)
                        changeTypeData.Converted = true;
                }
                else if ((code == ReturnCode.Ok) && (value is string) &&
                    (type != null))
                {
                    try
                    {
                        //
                        // NOTE: Try looking up an implicit operator that will
                        //       convert the string to the requested type.
                        //
                        MethodInfo methodInfo = type.GetMethod(
                            MarshalOps.ImplicitOperatorMethodName,
                            new Type[] { typeof(string) });

                        //
                        // NOTE: Failing that, try looking up an explicit operator
                        //       that will convert the string to the requested type.
                        //
                        if (methodInfo == null)
                            methodInfo = type.GetMethod(
                                MarshalOps.ExplicitOperatorMethodName,
                                new Type[] { typeof(string) });

                        //
                        // NOTE: Failing that, if this is a reference type, get the
                        //       "element type" and try to lookup an implicit and/or
                        //       explicit operator in that context.
                        //
                        if ((methodInfo == null) && type.IsByRef)
                        {
                            Type byRefElementType = type.GetElementType();

                            if (byRefElementType != null)
                            {
                                methodInfo = byRefElementType.GetMethod(
                                    MarshalOps.ImplicitOperatorMethodName,
                                    new Type[] { typeof(string) });

                                if (methodInfo == null)
                                    methodInfo = byRefElementType.GetMethod(
                                        MarshalOps.ExplicitOperatorMethodName,
                                        new Type[] { typeof(string) });
                            }
                        }

                        //
                        // NOTE: Did we find an operator method to use?
                        //
                        if (methodInfo != null)
                        {
                            changeTypeData.Attempted = true;

                            value = methodInfo.Invoke(
                                null /* static */, new object[] { value });

                            changeTypeData.Converted = true;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Update the marshal flags now.
                //
                changeTypeData.MarshalFlags = marshalFlags;

                //
                // NOTE: Store the new value back into the helper object.
                //
                changeTypeData.NewValue = value;

                //
                // NOTE: Do we consider the type conversions performed, if any, to
                //       be a success (i.e. do the types more-or-less match now)?
                //
                changeTypeData.DoesMatch = DoesMatchType(
                    value, type, marshalFlags);

                //
                // NOTE: In debug mode, show some diagnostic output.
                //
                if (debug)
                {
                    object oldValue = changeTypeData.OldValue;
                    object newValue = changeTypeData.NewValue;

                    TraceOps.DebugTrace(String.Format(
                        "ChangeType: caller = {0}, oldValue = {1}, " +
                        "oldType = {2}, marshalFlags = {3}, newValue = {4}, newType = {5}, " +
                        "toType = {6}, options = {7}, cultureInfo = {8}, clientData = {9}, " +
                        "callback = {10}, wasObject = {11}, attempted = {12}, converted = {13}, " +
                        "doesMatch = {14}, code = {15}, error = {16}",
                        FormatOps.DisplayString(changeTypeData.Caller),
                        FormatOps.WrapOrNull(true, true, oldValue),
                        FormatOps.TypeName(oldValue),
                        FormatOps.WrapOrNull(changeTypeData.MarshalFlags),
                        FormatOps.WrapOrNull(true, true, newValue),
                        FormatOps.TypeName(newValue),
                        FormatOps.WrapOrNull(type), FormatOps.WrapOrNull(true, true, options),
                        FormatOps.WrapOrNull(cultureInfo), FormatOps.WrapOrNull(clientData),
                        FormatOps.WrapOrNull(FormatOps.DelegateName(callback)),
                        changeTypeData.WasObject, changeTypeData.Attempted,
                        changeTypeData.Converted, changeTypeData.DoesMatch, code,
                        FormatOps.WrapOrNull(true, true, error)),
                        typeof(ScriptBinder).Name, TracePriority.MarshalDebug);
                }
            }
            else
            {
                error = "invalid change type data";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is an extensibility point that reorders the candidate
        /// method indexes (and their corresponding argument arrays) considered
        /// during overload resolution.  The default implementation falls back
        /// to the built-in behavior.
        /// </summary>
        /// <param name="type">
        /// The type whose methods are being considered.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the operation, if any.
        /// </param>
        /// <param name="methods">
        /// The candidate methods being considered.
        /// </param>
        /// <param name="reorderFlags">
        /// The flags that control how the reordering is performed.
        /// </param>
        /// <param name="methodIndexList">
        /// The list of candidate method indexes to reorder, in place.
        /// </param>
        /// <param name="argsList">
        /// The list of argument arrays corresponding to the method indexes, to
        /// reorder in place.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Continue" /> to use the built-in behavior;
        /// otherwise, a code indicating the result of custom reordering.
        /// </returns>
        public ReturnCode ReorderMethodIndexes(
            Type type,                    /* in */
            CultureInfo cultureInfo,      /* in */
            MethodBase[] methods,         /* in */
            ReorderFlags reorderFlags,    /* in */
            ref IntList methodIndexList,  /* in, out */
            ref ObjectArrayList argsList, /* in, out */
            ref Result error              /* out */
            )
        {
            //
            // FIXME: For now, always fallback to the default behavior.
            //
            return ReturnCode.Continue;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is an extensibility point that selects which candidate
        /// method should be invoked during overload resolution.  The default
        /// implementation falls back to the built-in behavior, which selects
        /// the first method that matches.
        /// </summary>
        /// <param name="type">
        /// The type whose methods are being considered.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the operation, if any.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types supplied by the caller.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The list of per-parameter marshalling flags.
        /// </param>
        /// <param name="methods">
        /// The candidate methods being considered.
        /// </param>
        /// <param name="args">
        /// The argument values supplied by the caller.
        /// </param>
        /// <param name="methodIndexList">
        /// The list of candidate method indexes.
        /// </param>
        /// <param name="argsList">
        /// The list of argument arrays corresponding to the method indexes.
        /// </param>
        /// <param name="index">
        /// On input and output, the index into the candidate list that is
        /// being selected.
        /// </param>
        /// <param name="methodIndex">
        /// On input and output, the resolved method index that was selected.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Continue" /> to use the built-in behavior;
        /// otherwise, a code indicating the result of custom selection.
        /// </returns>
        public ReturnCode SelectMethodIndex(
            Type type,                              /* in */
            CultureInfo cultureInfo,                /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in */
            MethodBase[] methods,                   /* in */
            object[] args,                          /* in */
            IntList methodIndexList,                /* in */
            ObjectArrayList argsList,               /* in */
            ref int index,                          /* in, out */
            ref int methodIndex,                    /* in, out */
            ref Result error                        /* out */
            )
        {
            //
            // FIXME: For now, always fallback to the default behavior, which is to
            //        select the first method that matches.  More sophisticated logic
            //        may need to be added here later.
            //
            return ReturnCode.Continue;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the most appropriate type from a list of
        /// candidate types, optionally preferring the type with the most
        /// similar name and/or the most members.  When no preference applies,
        /// it falls back to the built-in type selection semantics.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation.
        /// </param>
        /// <param name="oldValue">
        /// The original value being converted, if any.
        /// </param>
        /// <param name="newValue">
        /// The new (converted) value, if any.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to choose from.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during the operation, if any.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags that control how the type is selected.
        /// </param>
        /// <param name="type">
        /// On input, the currently selected type.  On output, the type that was
        /// selected, if it changed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a type was selected;
        /// <see cref="ReturnCode.Continue" /> to use the built-in type
        /// selection semantics; otherwise, an error code.
        /// </returns>
        public ReturnCode SelectType(
            Interpreter interpreter, /* in */
            object oldValue,         /* in */
            object newValue,         /* in */
            TypeList types,          /* in */
            CultureInfo cultureInfo, /* in */
            ObjectFlags objectFlags, /* in */
            ref Type type,           /* in, out */
            ref Result error         /* out */
            )
        {
            if ((types != null) && (types.Count > 1) && FlagOps.HasFlags(
                    objectFlags, ObjectFlags.SelectTypeMask, false))
            {
                Type typeWithMostSimilarName = null;

                if ((oldValue != null) &&
                    FlagOps.HasFlags(objectFlags, ObjectFlags.PreferSimilarName, true))
                {
                    StringComparison comparisonType =
                        SharedStringOps.GetSystemComparisonType(
                            FlagOps.HasFlags(objectFlags, ObjectFlags.NoCase, true));

                    typeWithMostSimilarName = RuntimeOps.GetTypeWithMostSimilarName(
                        types, StringOps.GetStringFromObject(oldValue), comparisonType);

                    if ((typeWithMostSimilarName == null) && FlagOps.HasFlags(
                            objectFlags, ObjectFlags.RejectDissimilarNames, true))
                    {
                        goto done;
                    }
                }

                if (FlagOps.HasFlags(objectFlags, ObjectFlags.PreferMoreMembers, true))
                {
                    Type typeWithMostMembers = RuntimeOps.GetTypeWithMostMembers(
                        types, DefaultBindingFlags);

                    if ((typeWithMostMembers != null) &&
                        !Object.ReferenceEquals(typeWithMostMembers, type) &&
                        ((typeWithMostSimilarName == null) || Object.ReferenceEquals(
                            typeWithMostMembers, typeWithMostSimilarName)))
                    {
                        type = typeWithMostMembers;
                        return ReturnCode.Ok;
                    }
                }
            }

        done:

            //
            // NOTE: Do nothing and return Continue.  The built-in type selection
            //       semantics will be used.
            //
            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Binder / IBinder Members
        /// <summary>
        /// This method selects the field that best matches the supplied value,
        /// performing any necessary type conversions on the value first.  When
        /// no field matches, it defers to the fallback or default binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control field selection.
        /// </param>
        /// <param name="match">
        /// The candidate fields to consider.
        /// </param>
        /// <param name="value">
        /// The value to be assigned to the field, possibly wrapped in marshal
        /// client data.
        /// </param>
        /// <param name="culture">
        /// The culture to use during type conversion, if any.
        /// </param>
        /// <returns>
        /// The matching field, or null if none could be selected.
        /// </returns>
        public override FieldInfo BindToField(
            BindingFlags bindingAttr, /* in */
            FieldInfo[] match,        /* in, out */
            object value,             /* in */
            CultureInfo culture       /* in */
            )
        {
            MarshalClientData marshalClientData;
            OptionDictionary options;
            MarshalFlags marshalFlags;

            MaybeUnpackMarshalClientData(
                ref value, out marshalClientData, out options,
                out marshalFlags);

            //
            // HACK: Mono may not be passing something valid to us.
            //
            if (match != null)
            {
                FieldInfo fieldInfo = null;
                ReturnCode code = ReturnCode.Ok;
                Result error = null;

                //
                // NOTE: This is the client data for the current call to
                //       [object invoke] (etc).
                //
                IClientData clientData;

                //
                // NOTE: Attempt to fetch the binder data from the active
                //       interpreter stack.
                //
                GetBinderClientData(ref options, out clientData);

                //
                // NOTE: Check each potential field for a match with a
                //       compatible type.
                //
                for (int matchIndex = 0; matchIndex < match.Length; matchIndex++)
                {
                    FieldInfo thisFieldInfo = match[matchIndex];

                    if (thisFieldInfo != null)
                    {
                        //
                        // NOTE: Create our helper object to hold all the necessary
                        //       input and output parameters necessary for the type
                        //       conversions.
                        //
                        IChangeTypeData changeTypeData = new ChangeTypeData(
                            "IBinder.BindToField", thisFieldInfo.FieldType, value,
                            options, culture, clientData, marshalFlags);

                        //
                        // NOTE: Try to change the type of the value.
                        //
                        code = ChangeType(changeTypeData, ref error);

                        //
                        // NOTE: Update the marshal client data now.
                        //
                        MaybeUpdateMarshalClientData(
                            marshalClientData, changeTypeData, ref marshalFlags);

                        //
                        // NOTE: Did we succeed AND did we actually do something?
                        //
                        if ((code == ReturnCode.Ok) &&
                            (changeTypeData.WasObject || changeTypeData.Converted))
                        {
                            //
                            // NOTE: If we translated an opaque object handle or
                            //       converted the object value to another type,
                            //       we must count that as a matched field.
                            //
                            fieldInfo = thisFieldInfo;
                            break;
                        }
                        else if (code != ReturnCode.Ok)
                        {
                            break;
                        }
                    }
                }

                //
                // NOTE: Did we find a matching field?
                //
                if ((code == ReturnCode.Ok) && (fieldInfo != null))
                {
                    return fieldInfo;
                }
                else if ((code != ReturnCode.Ok) && !FlagOps.HasFlags(
                        marshalFlags, MarshalFlags.NoBindToFieldThrow, true))
                {
                    throw new ScriptException(code, error);
                }
            }

            //
            // NOTE: When forbidden from doing so, skip calling the default
            //       BindToField method.
            //
            if (!FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.SkipBindToField, true))
            {
                IBinder binder = FallbackBinder;

                if (binder != null)
                {
                    return binder.BindToField(
                        bindingAttr, match, value, culture);
                }

                IBinder defaultBinder = DefaultBinder;

                if (defaultBinder != null)
                {
                    return defaultBinder.BindToField(
                        bindingAttr, match, value, culture);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the method that best matches the supplied
        /// arguments, performing any necessary type conversions on the
        /// arguments first using transactional (all-or-nothing) semantics.
        /// When no method matches, it defers to the fallback or default binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control method selection.
        /// </param>
        /// <param name="match">
        /// The candidate methods to consider.
        /// </param>
        /// <param name="args">
        /// On input, the argument values supplied by the caller.  On output,
        /// the converted argument values for the selected method.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers, if any.
        /// </param>
        /// <param name="culture">
        /// The culture to use during type conversion, if any.
        /// </param>
        /// <param name="names">
        /// The names of the parameters, if any.
        /// </param>
        /// <param name="state">
        /// Upon return, receives binder state that can be passed to
        /// <see cref="ReorderArgumentArray" />.
        /// </param>
        /// <returns>
        /// The matching method, or null if none could be selected.
        /// </returns>
        public override MethodBase BindToMethod(
            BindingFlags bindingAttr,      /* in */
            MethodBase[] match,            /* in, out */
            ref object[] args,             /* in, out */
            ParameterModifier[] modifiers, /* in, out */
            CultureInfo culture,           /* in */
            string[] names,                /* in */
            out object state               /* out */
            )
        {
            //
            // HACK: Mono may not be passing something valid to us.
            //
            if ((match != null) && (args != null))
            {
                MethodBase methodBase = null;
                ReturnCode code = ReturnCode.Ok;
                Result error = null;

                //
                // NOTE: These are the options and client data for the current
                //       call to [object invoke] (etc).
                //
                OptionDictionary options = null;
                IClientData clientData;

                //
                // NOTE: Attempt to fetch the binder data from the active
                //       interpreter stack.
                //
                GetBinderClientData(ref options, out clientData);

                //
                // NOTE: Create a new argument array for working storage while
                //       we perform the type conversions.  This is critical for
                //       the transactional (all-or-nothing) semantics of this
                //       method.
                //
                object[] newArgs = new object[args.Length];

                for (int matchIndex = 0; matchIndex < match.Length; matchIndex++)
                {
                    MethodBase thisMethodBase = match[matchIndex];

                    if (thisMethodBase != null)
                    {
                        ParameterInfo[] parameterInfo = thisMethodBase.GetParameters();

                        if (parameterInfo != null)
                        {
                            int count = parameterInfo.Length;

                            for (int index = 0; index < args.Length; index++)
                            {
                                //
                                // NOTE: Match up index with Position from ParameterInfo.
                                //
                                //       Check destination type and in/out attributes
                                //       (actually, this must be done in the [object]
                                //       command itself, not here).
                                //
                                //       Use temporary array of arguments until ready to
                                //       call base class method (below).
                                //
                                foreach (ParameterInfo thisParameterInfo in parameterInfo)
                                {
                                    if ((thisParameterInfo != null) &&
                                        (thisParameterInfo.Position == index))
                                    {
                                        //
                                        // NOTE: Create our helper object to hold all the necessary
                                        //       input and output parameters necessary for the type
                                        //       conversions.
                                        //
                                        IChangeTypeData changeTypeData = new ChangeTypeData(
                                            "IBinder.BindToMethod", thisParameterInfo.ParameterType,
                                            args[index], options, culture, clientData,
                                            MarshalFlags.None);

                                        //
                                        // NOTE: Try to change the type of the value.
                                        //
                                        code = ChangeType(changeTypeData, ref error);

                                        //
                                        // NOTE: Did we succeed AND did we actually do something?
                                        //
                                        if ((code == ReturnCode.Ok) &&
                                            (changeTypeData.WasObject || changeTypeData.Converted))
                                        {
                                            //
                                            // NOTE: If we translated an opaque object handle or
                                            //       converted the object value to another type,
                                            //       we must store the new value and count that
                                            //       as a converted parameter.
                                            //
                                            newArgs[index] = changeTypeData.NewValue;
                                            count--;
                                        }

                                        break;
                                    }
                                }

                                if (code != ReturnCode.Ok)
                                    break;
                            }

                            if (code != ReturnCode.Ok)
                                break;

                            //
                            // NOTE: Is this method a match for all the parameters?
                            //
                            if (count == 0)
                            {
                                methodBase = thisMethodBase;
                                break;
                            }
                        }
                    }
                }

                //
                // NOTE: Did we find a matching method?
                //
                if ((code == ReturnCode.Ok) && (methodBase != null))
                {
                    //
                    // NOTE: Ok, commit changes to args array.
                    //
                    for (int index = 0; index < args.Length; index++)
                        args[index] = newArgs[index];

                    state = null; // NOTE: Or maybe "new object();"?
                    return methodBase;
                }
                else if (code != ReturnCode.Ok)
                {
                    throw new ScriptException(code, error);
                }
            }
#if (DEBUG || FORCE_TRACE) && MONO
            else
            {
                //
                // FIXME: Remove this when Mono fixes this bug.
                //
                TraceOps.DebugTrace(String.Format(
                    "BindToMethod: null arguments (?), " +
                    "match = {0}, args = {1}",
                    (match == null), (args == null)),
                    typeof(ScriptBinder).Name,
                    TracePriority.MarshalDebug);
            }
#endif

            IBinder binder = FallbackBinder;

            if (binder != null)
            {
                return binder.BindToMethod(
                    bindingAttr, match, ref args, modifiers, culture, names,
                    out state);
            }

            IBinder defaultBinder = DefaultBinder;

            if (defaultBinder != null)
            {
                return defaultBinder.BindToMethod(
                    bindingAttr, match, ref args, modifiers, culture, names,
                    out state);
            }

            state = null;
            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the supplied value to the requested type,
        /// applying the registered conversion callbacks and opaque object
        /// handle lookups.  When no conversion applies, it defers to the
        /// fallback or default binder.
        /// </summary>
        /// <param name="value">
        /// The value to convert, possibly wrapped in marshal client data.
        /// </param>
        /// <param name="type">
        /// The type to convert the value into.
        /// </param>
        /// <param name="culture">
        /// The culture to use during the conversion, if any.
        /// </param>
        /// <returns>
        /// The converted value, or the original value if no conversion was
        /// performed.
        /// </returns>
        public override object ChangeType(
            object value,       /* in */
            Type type,          /* in */
            CultureInfo culture /* in */
            ) /* throw */
        {
            MarshalClientData marshalClientData;
            OptionDictionary options;
            MarshalFlags marshalFlags;

            MaybeUnpackMarshalClientData(
                ref value, out marshalClientData, out options,
                out marshalFlags);

            if (debug)
            {
                TraceOps.DebugTrace(String.Format(
                    "ChangeType: value = {0}, valueType = {1}, type = {2}, " +
                    "cultureInfo = {3}, marshalClientData = {4}, marshalFlags = {5}",
                    FormatOps.WrapOrNull(true, true, value),
                    FormatOps.TypeName(value),
                    FormatOps.WrapOrNull(type), FormatOps.WrapOrNull(culture),
                    FormatOps.WrapOrNull(marshalClientData), FormatOps.WrapOrNull(
                    marshalFlags)), typeof(ScriptBinder).Name,
                    TracePriority.MarshalDebug);
            }

#if MONO || MONO_HACKS
            //
            // HACK: *MONO* As of Mono 2.8.0, it seems that Mono will call
            //       the ChangeType method of a custom binder even if the
            //       value type and the desired type are identical.  This
            //       is a problem for us; therefore, prevent it.
            //       https://bugzilla.novell.com/show_bug.cgi?id=471359
            //
            // BUGFIX: Everything is assignable to System.Object and we always
            //         want to lookup opaque object handles; therefore, skip
            //         this hack if the destination type is System.Object.
            //
            if ((type != typeof(object)) && CommonOps.Runtime.IsMono() &&
                DoesMatchType(value, type, marshalFlags))
            {
                return value;
            }
#endif

            ReturnCode code = ReturnCode.Ok;
            Result error = null;

            //
            // NOTE: This is the client data for the current call to
            //       [object invoke] (etc).
            //
            IClientData clientData;

            //
            // NOTE: Attempt to fetch the binder data from the active
            //       interpreter stack.
            //
            GetBinderClientData(ref options, out clientData);

            //
            // NOTE: Create our helper object to hold all the necessary
            //       input and output parameters necessary for the type
            //       conversions.
            //
            IChangeTypeData changeTypeData = new ChangeTypeData(
                "IBinder.ChangeType", type, value, options, culture,
                clientData, marshalFlags);

            //
            // NOTE: Try to change the type of the value.
            //
            code = ChangeType(changeTypeData, ref error);

            //
            // NOTE: Update the marshal client data now.
            //
            MaybeUpdateMarshalClientData(
                marshalClientData, changeTypeData, ref marshalFlags);

            //
            // NOTE: Did we succeed AND did we actually do something?
            //
            if ((code == ReturnCode.Ok) &&
                (changeTypeData.WasObject || changeTypeData.Converted))
            {
                return changeTypeData.NewValue;
            }
            else if ((code != ReturnCode.Ok) && !FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.NoChangeTypeThrow, true))
            {
                throw new ScriptException(code, error);
            }

            //
            // NOTE: When forbidden from doing so, skip calling the default
            //       ChangeType method.
            //
            if (!FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.SkipChangeType, true))
            {
                //
                // WARNING: Per MSDN, the default ChangeType does not do
                //          anything except throw exceptions (i.e. it
                //          does not actually convert or change anything).
                //
                IBinder binder = FallbackBinder;

                if (binder != null)
                    return binder.ChangeType(value, type, culture);

                IBinder defaultBinder = DefaultBinder;

                if (defaultBinder != null)
                    return defaultBinder.ChangeType(value, type, culture);
            }

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores an argument array to its original order after a
        /// method invocation.  It defers to the fallback or default binder.
        /// </summary>
        /// <param name="args">
        /// The argument array to reorder, in place.
        /// </param>
        /// <param name="state">
        /// The binder state that was produced by <see cref="BindToMethod" />.
        /// </param>
        public override void ReorderArgumentArray(
            ref object[] args, /* in, out */
            object state       /* in */
            )
        {
            IBinder binder = FallbackBinder;

            if (binder != null)
            {
                binder.ReorderArgumentArray(ref args, state);
                return;
            }

            IBinder defaultBinder = DefaultBinder;

            if (defaultBinder != null)
            {
                defaultBinder.ReorderArgumentArray(ref args, state);
                return;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a method from a set of candidates based on the
        /// specified argument types.  It defers to the fallback or default
        /// binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control method selection.
        /// </param>
        /// <param name="match">
        /// The candidate methods to consider.
        /// </param>
        /// <param name="types">
        /// The argument types used to select a method.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers, if any.
        /// </param>
        /// <returns>
        /// The selected method, or null if none could be selected.
        /// </returns>
        public override MethodBase SelectMethod(
            BindingFlags bindingAttr,     /* in */
            MethodBase[] match,           /* in, out */
            Type[] types,                 /* in */
            ParameterModifier[] modifiers /* in, out */
            )
        {
            IBinder binder = FallbackBinder;

            if (binder != null)
            {
                return binder.SelectMethod(
                    bindingAttr, match, types, modifiers);
            }

            IBinder defaultBinder = DefaultBinder;

            if (defaultBinder != null)
            {
                return defaultBinder.SelectMethod(
                    bindingAttr, match, types, modifiers);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a property from a set of candidates based on the
        /// specified return type and index parameter types.  It defers to the
        /// fallback or default binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control property selection.
        /// </param>
        /// <param name="match">
        /// The candidate properties to consider.
        /// </param>
        /// <param name="returnType">
        /// The expected return type of the property.
        /// </param>
        /// <param name="indexes">
        /// The types of the property index parameters, if any.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers, if any.
        /// </param>
        /// <returns>
        /// The selected property, or null if none could be selected.
        /// </returns>
        public override PropertyInfo SelectProperty(
            BindingFlags bindingAttr,     /* in */
            PropertyInfo[] match,         /* in, out */
            Type returnType,              /* in */
            Type[] indexes,               /* in */
            ParameterModifier[] modifiers /* in, out */
            )
        {
            IBinder binder = FallbackBinder;

            if (binder != null)
            {
                return binder.SelectProperty(
                    bindingAttr, match, returnType, indexes, modifiers);
            }

            IBinder defaultBinder = DefaultBinder;

            if (defaultBinder != null)
            {
                return defaultBinder.SelectProperty(
                    bindingAttr, match, returnType, indexes, modifiers);
            }

            return null;
        }
        #endregion
    }
}
