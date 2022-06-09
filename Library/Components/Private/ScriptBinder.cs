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
    [ObjectId("0e087802-e964-4900-b687-79bbc4332079")]
    internal sealed class ScriptBinder : Binder, IScriptBinder, IHaveInterpreter
    {
        #region Private Constants
        //
        // NOTE: The method attributes of static constructors in the CLR.
        //
        // HACK: This is purposely not read-only.
        //
        private static MethodAttributes cctorMethodAttributes =
            MethodAttributes.Static | MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: What interpreter do we belong to?
        //
        private Interpreter interpreter;

        //
        // NOTE: What is the default binder?  Normally, this simply returns
        //       the value of Type.DefaultBinder.
        //
        private IBinder defaultBinder;

        //
        // NOTE: What is our fallback binder?
        //
        private IBinder fallbackBinder;

        //
        // NOTE: What is our parent binder?  This will almost always be null
        //       for the default IScriptBinder implementation.
        //
        private IScriptBinder parentBinder;

        //
        // NOTE: What are the binding flags when they are not specified by a
        //       caller?
        //
        private BindingFlags defaultBindingFlags;

        //
        // NOTE: Is this binder operating in "debug" mode?
        //
        private bool debug;

        //
        // NOTE: What dynamic string-to-type conversions do we support?
        //
        private TypeChangeTypeCallbackDictionary changeTypes;

        //
        // NOTE: What dynamic type-to-string conversions do we support?
        //
        private TypeToStringCallbackDictionary toStringTypes;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
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

        private static IClientData GetBinderClientData()
        {
            IAnyPair<Interpreter, IClientData> anyPair =
                Interpreter.GetActivePair(typeof(BinderClientData));

            return (anyPair != null) ? anyPair.Y : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IScriptBinder Members
        public IBinder DefaultBinder
        {
            get { return defaultBinder; }
            set { defaultBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IBinder FallbackBinder
        {
            get { return fallbackBinder; }
            set { fallbackBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IScriptBinder ParentBinder
        {
            get { return parentBinder; }
            set { parentBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public BindingFlags DefaultBindingFlags
        {
            get { return defaultBindingFlags; }
            set { defaultBindingFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public bool IsCoreCallback(
            Delegate callback /* in */
            )
        {
            return IsCoreToStringCallback(callback as ToStringCallback) ||
                IsCoreChangeTypeCallback(callback as ChangeTypeCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool IsCoreStringListToStringCallback(
            ToStringCallback callback /* in */
            )
        {
            //
            // HACK: There are currently no custom ToString callbacks defined
            //       (i.e. in the ConversionOps.Dynamic._ToString class).
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public bool HasToStringTypes()
        {
            return (toStringTypes != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public bool HasToStringCallback(
            Type type,     /* in */
            bool primitive /* in */
            )
        {
            ToStringCallback callback = null;

            return HasToStringCallback(type, primitive, ref callback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public bool HasChangeTypes()
        {
            return (changeTypes != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public bool HasChangeTypeCallback(
            Type type,     /* in */
            bool primitive /* in */
            )
        {
            ChangeTypeCallback callback = null;

            return HasChangeTypeCallback(type, primitive, ref callback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
