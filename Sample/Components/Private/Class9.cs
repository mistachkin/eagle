/*
 * Class9.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Sample
{
    /// <summary>
    /// This class is a sample script binder that demonstrates how to wrap an
    /// existing <see cref="IScriptBinder" /> for an Eagle interpreter.  It
    /// forwards every binder operation to a parent binder while registering
    /// custom to-string and change-type callbacks for the sample type
    /// <see cref="Class2" />.  It implements <see cref="IScriptBinder" />,
    /// <see cref="IGetInterpreter" />, and <see cref="IDisposable" />.
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("d8e4e05f-cd71-4cbe-9ba6-f87a653a96d2")]
    internal sealed class Class9 : IScriptBinder, IGetInterpreter, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this sample script binder that wraps
        /// the specified parent binder and registers its custom callbacks for
        /// the sample type <see cref="Class2" />.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that this script binder is associated with.
        /// </param>
        /// <param name="parentBinder">
        /// The parent script binder that all binder operations are forwarded
        /// to.  This parameter may be null.
        /// </param>
        public Class9(
            Interpreter interpreter,
            IScriptBinder parentBinder
            )
        {
            this.interpreter = interpreter;
            this.parentBinder = parentBinder;

            ///////////////////////////////////////////////////////////////////

            AddClass2();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method registers the custom to-string and change-type
        /// callbacks for the sample type <see cref="Class2" /> with the parent
        /// binder, complaining via the interpreter if either registration
        /// fails.
        /// </summary>
        private void AddClass2()
        {
            if (parentBinder != null)
            {
                ReturnCode code;
                Result error = null; /* REUSED */

                code = parentBinder.AddToStringCallback(
                    typeof(Class2), FromClass2, ref error);

                if (code != ReturnCode.Ok)
                    Utility.Complain(interpreter, code, error);

                error = null;

                code = parentBinder.AddChangeTypeCallback(
                    typeof(Class2), ToClass2, ref error);

                if (code != ReturnCode.Ok)
                    Utility.Complain(interpreter, code, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unregisters the custom to-string and change-type
        /// callbacks for the sample type <see cref="Class2" /> from the parent
        /// binder, complaining via the interpreter if either removal fails.
        /// </summary>
        private void RemoveClass2()
        {
            if (parentBinder != null)
            {
                ReturnCode code;
                Result error = null; /* REUSED */

                code = parentBinder.RemoveToStringCallback(
                    typeof(Class2), ref error);

                if (code != ReturnCode.Ok)
                    Utility.Complain(interpreter, code, error);

                error = null;

                code = parentBinder.RemoveChangeTypeCallback(
                    typeof(Class2), ref error);

                if (code != ReturnCode.Ok)
                    Utility.Complain(interpreter, code, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the to-string callback for the sample type
        /// <see cref="Class2" />.  It converts a <see cref="Class2" /> instance
        /// into its string representation, which is its description.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the conversion is being performed.
        /// </param>
        /// <param name="type">
        /// The target type associated with the conversion.
        /// </param>
        /// <param name="value">
        /// The value to convert to a string.  It must be a
        /// <see cref="Class2" /> instance.
        /// </param>
        /// <param name="options">
        /// The conversion options, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The extra client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control marshalling behavior for the conversion.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the string representation of
        /// <paramref name="value" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode FromClass2(
            Interpreter interpreter, /* NOT USED */
            Type type, /* NOT USED */
            object value,
            OptionDictionary options, /* NOT USED */
            CultureInfo cultureInfo, /* NOT USED */
            IClientData clientData, /* NOT USED */
            ref MarshalFlags marshalFlags, /* NOT USED */
            ref string text,
            ref Result error
            )
        {
            if (value is Class2)
            {
                text = ((Class2)value).Description;
                return ReturnCode.Ok;
            }
            else
            {
                error = "type mismatch, need Class2";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the change-type callback for the sample type
        /// <see cref="Class2" />.  It converts the supplied string into a
        /// <see cref="Class2" /> instance by looking up an interpreter command
        /// with that name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the conversion is being performed; it is
        /// used to look up the command named by <paramref name="text" />.
        /// </param>
        /// <param name="type">
        /// The target type associated with the conversion.
        /// </param>
        /// <param name="text">
        /// The string value to convert; it is treated as the name of an
        /// interpreter command.
        /// </param>
        /// <param name="options">
        /// The conversion options, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The extra client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control marshalling behavior for the conversion.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting object (the located command).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode ToClass2(
            Interpreter interpreter,
            Type type, /* NOT USED */
            string text,
            OptionDictionary options, /* NOT USED */
            CultureInfo cultureInfo, /* NOT USED */
            IClientData clientData, /* NOT USED */
            ref MarshalFlags marshalFlags, /* NOT USED */
            ref object value, /* Sample.Class2 */
            ref Result error
            )
        {
            long token = 0;
            ICommand command = null;

            if (interpreter.GetCommand(
                    text, LookupFlags.NoWrapper, ref token, ref command,
                    ref error) == ReturnCode.Ok)
            {
                value = command;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// Stores the interpreter that this script binder is associated with.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that this script binder is associated with.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBinder Members
        /// <summary>
        /// This method selects a field from the supplied candidates that
        /// matches the specified binding constraints, by forwarding to the
        /// parent binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how candidates are matched.
        /// </param>
        /// <param name="match">
        /// The array of candidate fields to select from.
        /// </param>
        /// <param name="value">
        /// The value that will be assigned to the selected field.
        /// </param>
        /// <param name="culture">
        /// The culture to use when matching, if any.
        /// </param>
        /// <returns>
        /// The selected <see cref="FieldInfo" />.
        /// </returns>
        public FieldInfo BindToField(
            BindingFlags bindingAttr,
            FieldInfo[] match,
            object value,
            CultureInfo culture
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.BindToField(
                bindingAttr, match, value, culture);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a method from the supplied candidates that
        /// matches the specified binding constraints and arguments, by
        /// forwarding to the parent binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how candidates are matched.
        /// </param>
        /// <param name="match">
        /// The array of candidate methods to select from.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the method; they may be reordered to
        /// match the selected method.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the arguments.
        /// </param>
        /// <param name="culture">
        /// The culture to use when matching, if any.
        /// </param>
        /// <param name="names">
        /// The optional names of the supplied arguments.
        /// </param>
        /// <param name="state">
        /// Upon return, receives binder-specific state that can be used to
        /// restore the original argument order.
        /// </param>
        /// <returns>
        /// The selected <see cref="MethodBase" />.
        /// </returns>
        public MethodBase BindToMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            ref object[] args,
            ParameterModifier[] modifiers,
            CultureInfo culture,
            string[] names,
            out object state
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.BindToMethod(
                bindingAttr, match, ref args, modifiers, culture, names,
                out state);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the supplied value to the specified type, by
        /// forwarding to the parent binder.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="type">
        /// The type to convert <paramref name="value" /> to.
        /// </param>
        /// <param name="culture">
        /// The culture to use for the conversion, if any.
        /// </param>
        /// <returns>
        /// The converted value.
        /// </returns>
        public object ChangeType(
            object value,
            Type type,
            CultureInfo culture
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.ChangeType(value, type, culture);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the original order of an argument array using
        /// the binder-specific state produced by <see cref="BindToMethod" />,
        /// by forwarding to the parent binder.
        /// </summary>
        /// <param name="args">
        /// The argument array to reorder.
        /// </param>
        /// <param name="state">
        /// The binder-specific state that describes the original argument
        /// order.
        /// </param>
        public void ReorderArgumentArray(
            ref object[] args,
            object state
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            parentBinder.ReorderArgumentArray(ref args, state);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a method from the supplied candidates that
        /// matches the specified binding constraints and parameter types, by
        /// forwarding to the parent binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how candidates are matched.
        /// </param>
        /// <param name="match">
        /// The array of candidate methods to select from.
        /// </param>
        /// <param name="types">
        /// The parameter types used to match a candidate.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the parameter types.
        /// </param>
        /// <returns>
        /// The selected <see cref="MethodBase" />.
        /// </returns>
        public MethodBase SelectMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            Type[] types,
            ParameterModifier[] modifiers
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.SelectMethod(
                bindingAttr, match, types, modifiers);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a property from the supplied candidates that
        /// matches the specified binding constraints, return type, and index
        /// parameter types, by forwarding to the parent binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how candidates are matched.
        /// </param>
        /// <param name="match">
        /// The array of candidate properties to select from.
        /// </param>
        /// <param name="returnType">
        /// The return type used to match a candidate.
        /// </param>
        /// <param name="indexes">
        /// The index parameter types used to match a candidate.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the index parameter types.
        /// </param>
        /// <returns>
        /// The selected <see cref="PropertyInfo" />.
        /// </returns>
        public PropertyInfo SelectProperty(
            BindingFlags bindingAttr,
            PropertyInfo[] match,
            Type returnType,
            Type[] indexes,
            ParameterModifier[] modifiers
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.SelectProperty(
                bindingAttr, match, returnType, indexes, modifiers);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptBinder Members
        /// <summary>
        /// Gets or sets the default binder, as forwarded to and from the parent
        /// binder.  When no parent binder is present, the getter returns null
        /// and the setter has no effect.
        /// </summary>
        public IBinder DefaultBinder
        {
            get
            {
                CheckDisposed();

                return (parentBinder != null) ?
                    parentBinder.DefaultBinder : null;
            }
            set
            {
                CheckDisposed();

                if (parentBinder != null)
                    parentBinder.DefaultBinder = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the fallback binder, as forwarded to and from the
        /// parent binder.  When no parent binder is present, the getter returns
        /// null and the setter has no effect.
        /// </summary>
        public IBinder FallbackBinder
        {
            get
            {
                CheckDisposed();

                return (parentBinder != null) ?
                    parentBinder.FallbackBinder : null;
            }
            set
            {
                CheckDisposed();

                if (parentBinder != null)
                    parentBinder.FallbackBinder = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the parent script binder that all binder operations are
        /// forwarded to.
        /// </summary>
        private IScriptBinder parentBinder;
        /// <summary>
        /// Gets or sets the parent script binder that all binder operations are
        /// forwarded to.
        /// </summary>
        public IScriptBinder ParentBinder
        {
            get { CheckDisposed(); return parentBinder; }
            set { CheckDisposed(); parentBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the default binding flags, as forwarded to and from the
        /// parent binder.  When no parent binder is present, the getter returns
        /// <see cref="BindingFlags.Default" /> and the setter has no effect.
        /// </summary>
        public BindingFlags DefaultBindingFlags
        {
            get
            {
                CheckDisposed();

                return (parentBinder != null) ?
                    parentBinder.DefaultBindingFlags : BindingFlags.Default;
            }
            set
            {
                CheckDisposed();

                if (parentBinder != null)
                    parentBinder.DefaultBindingFlags = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether debugging is enabled, as
        /// forwarded to and from the parent binder.  When no parent binder is
        /// present, the getter returns false and the setter has no effect.
        /// </summary>
        public bool Debug
        {
            get
            {
                CheckDisposed();

                return (parentBinder != null) ? parentBinder.Debug : false;
            }
            set
            {
                CheckDisposed();

                if (parentBinder != null) parentBinder.Debug = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified method is allowed to be
        /// invoked, by forwarding to the parent binder.
        /// </summary>
        /// <param name="method">
        /// The method to check.
        /// </param>
        /// <returns>
        /// True if the method is allowed; otherwise, false.
        /// </returns>
        public bool IsAllowed(
            MethodBase method
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.IsAllowed(method);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves an opaque object instance from its string
        /// representation, by forwarding to the parent binder.
        /// </summary>
        /// <param name="text">
        /// The string representation of the object to resolve.
        /// </param>
        /// <param name="types">
        /// The candidate types to consider when resolving the object, if any.
        /// </param>
        /// <param name="appDomain">
        /// The application domain associated with the object, if any.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags that control how the object is resolved.
        /// </param>
        /// <param name="objectType">
        /// The expected type of the object, if any.
        /// </param>
        /// <param name="proxyType">
        /// The proxy type associated with the object, if any.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how the value is interpreted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during resolution, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved typed instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode GetObject(
            string text,
            TypeList types,
            AppDomain appDomain,
            BindingFlags bindingFlags,
            Type objectType,
            Type proxyType,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedInstance value,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.GetObject(
                text, types, appDomain, bindingFlags, objectType, proxyType,
                valueFlags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a member of a typed instance from its string
        /// representation, by forwarding to the parent binder.
        /// </summary>
        /// <param name="text">
        /// The string representation of the member to resolve.
        /// </param>
        /// <param name="typedInstance">
        /// The typed instance whose member is being resolved.
        /// </param>
        /// <param name="memberTypes">
        /// The kinds of members to consider when resolving.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags that control how the member is resolved.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how the value is interpreted.
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
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode GetMember(
            string text,
            ITypedInstance typedInstance,
            MemberTypes memberTypes,
            BindingFlags bindingFlags,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedMember value,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.GetMember(
                text, typedInstance, memberTypes, bindingFlags, valueFlags,
                cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value matches the
        /// specified type for marshalling purposes, by forwarding to the parent
        /// binder.
        /// </summary>
        /// <param name="value">
        /// The value to check.
        /// </param>
        /// <param name="type">
        /// The type to match against.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control marshalling behavior for the check.
        /// </param>
        /// <returns>
        /// True if the value matches the type; otherwise, false.
        /// </returns>
        public bool DoesMatchType(
            object value,
            Type type,
            MarshalFlags marshalFlags
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.DoesMatchType(value, type, marshalFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified callback is one of the
        /// built-in core callbacks, by forwarding to the parent binder.
        /// </summary>
        /// <param name="callback">
        /// The callback delegate to check.
        /// </param>
        /// <returns>
        /// True if the callback is a core callback; otherwise, false.
        /// </returns>
        public bool IsCoreCallback(
            Delegate callback
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.IsCoreCallback(callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified to-string callback is
        /// the built-in core callback used for string lists, by forwarding to
        /// the parent binder.
        /// </summary>
        /// <param name="callback">
        /// The to-string callback to check.
        /// </param>
        /// <returns>
        /// True if the callback is the core string list to-string callback;
        /// otherwise, false.
        /// </returns>
        public bool IsCoreStringListToStringCallback(
            ToStringCallback callback
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.IsCoreStringListToStringCallback(callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified change-type callback is
        /// the built-in core callback used for string lists, by forwarding to
        /// the parent binder.
        /// </summary>
        /// <param name="callback">
        /// The change-type callback to check.
        /// </param>
        /// <returns>
        /// True if the callback is the core string list change-type callback;
        /// otherwise, false.
        /// </returns>
        public bool IsCoreStringListChangeTypeCallback(
            ChangeTypeCallback callback
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.IsCoreStringListChangeTypeCallback(callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any types have registered to-string
        /// callbacks, by forwarding to the parent binder.
        /// </summary>
        /// <returns>
        /// True if any to-string types are registered; otherwise, false.
        /// </returns>
        public bool HasToStringTypes()
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasToStringTypes();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method lists the types that have registered to-string
        /// callbacks, by forwarding to the parent binder.
        /// </summary>
        /// <param name="types">
        /// Upon success, receives the list of types that have registered
        /// to-string callbacks.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode ListToStrings(
            ref TypeList types,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.ListToStrings(ref types, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified to-string callback is
        /// one of the built-in core callbacks, by forwarding to the parent
        /// binder.
        /// </summary>
        /// <param name="callback">
        /// The to-string callback to check.
        /// </param>
        /// <returns>
        /// True if the callback is a core to-string callback; otherwise, false.
        /// </returns>
        public bool IsCoreToStringCallback(
            ToStringCallback callback
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.IsCoreToStringCallback(callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type has a registered
        /// to-string callback, by forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to also consider primitive types.
        /// </param>
        /// <returns>
        /// True if the type has a registered to-string callback; otherwise,
        /// false.
        /// </returns>
        public bool HasToStringCallback(
            Type type,
            bool primitive
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasToStringCallback(type, primitive);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type has a registered
        /// to-string callback and, if so, returns it, by forwarding to the
        /// parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to also consider primitive types.
        /// </param>
        /// <param name="callback">
        /// Upon success, receives the registered to-string callback for the
        /// type.
        /// </param>
        /// <returns>
        /// True if the type has a registered to-string callback; otherwise,
        /// false.
        /// </returns>
        public bool HasToStringCallback(
            Type type,
            bool primitive,
            ref ToStringCallback callback
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasToStringCallback(
                type, primitive, ref callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method registers a to-string callback for the specified type,
        /// by forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to register the callback for.
        /// </param>
        /// <param name="callback">
        /// The to-string callback to register.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode AddToStringCallback(
            Type type,
            ToStringCallback callback,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.AddToStringCallback(type, callback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unregisters the to-string callback for the specified
        /// type, by forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to remove the callback for.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode RemoveToStringCallback(
            Type type,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.RemoveToStringCallback(type, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified to-string callback to convert a
        /// value into its string representation, by forwarding to the parent
        /// binder.
        /// </summary>
        /// <param name="callback">
        /// The to-string callback to invoke.
        /// </param>
        /// <param name="type">
        /// The target type associated with the conversion.
        /// </param>
        /// <param name="value">
        /// The value to convert to a string.
        /// </param>
        /// <param name="options">
        /// The conversion options, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The extra client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control marshalling behavior for the conversion.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the string representation of
        /// <paramref name="value" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode InvokeToStringCallback(
            ToStringCallback callback,
            Type type,
            object value,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            ref MarshalFlags marshalFlags,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.InvokeToStringCallback(
                callback, type, value, options, cultureInfo, clientData,
                ref marshalFlags, ref text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a value into its string representation using
        /// the supplied change-type data, by forwarding to the parent binder.
        /// </summary>
        /// <param name="changeTypeData">
        /// The data describing the value to convert and how to convert it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode ToString(
            IChangeTypeData changeTypeData,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.ToString(changeTypeData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any types have registered change-type
        /// callbacks, by forwarding to the parent binder.
        /// </summary>
        /// <returns>
        /// True if any change-type types are registered; otherwise, false.
        /// </returns>
        public bool HasChangeTypes()
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasChangeTypes();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method lists the types that have registered change-type
        /// callbacks, by forwarding to the parent binder.
        /// </summary>
        /// <param name="types">
        /// Upon success, receives the list of types that have registered
        /// change-type callbacks.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode ListChangeTypes(
            ref TypeList types,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.ListChangeTypes(ref types, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified change-type callback is
        /// one of the built-in core callbacks, by forwarding to the parent
        /// binder.
        /// </summary>
        /// <param name="callback">
        /// The change-type callback to check.
        /// </param>
        /// <returns>
        /// True if the callback is a core change-type callback; otherwise,
        /// false.
        /// </returns>
        public bool IsCoreChangeTypeCallback(
            ChangeTypeCallback callback
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.IsCoreChangeTypeCallback(callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type has a registered
        /// change-type callback, by forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to also consider primitive types.
        /// </param>
        /// <returns>
        /// True if the type has a registered change-type callback; otherwise,
        /// false.
        /// </returns>
        public bool HasChangeTypeCallback(
            Type type,
            bool primitive
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasChangeTypeCallback(type, primitive);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type has a registered
        /// change-type callback and, if so, returns it, by forwarding to the
        /// parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="primitive">
        /// Non-zero to also consider primitive types.
        /// </param>
        /// <param name="callback">
        /// Upon success, receives the registered change-type callback for the
        /// type.
        /// </param>
        /// <returns>
        /// True if the type has a registered change-type callback; otherwise,
        /// false.
        /// </returns>
        public bool HasChangeTypeCallback(
            Type type,
            bool primitive,
            ref ChangeTypeCallback callback
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasChangeTypeCallback(
                type, primitive, ref callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method registers a change-type callback for the specified type,
        /// by forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to register the callback for.
        /// </param>
        /// <param name="callback">
        /// The change-type callback to register.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode AddChangeTypeCallback(
            Type type,
            ChangeTypeCallback callback,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.AddChangeTypeCallback(
                type, callback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unregisters the change-type callback for the specified
        /// type, by forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type to remove the callback for.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode RemoveChangeTypeCallback(
            Type type,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.RemoveChangeTypeCallback(type, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified change-type callback to convert a
        /// string into a value of the target type, by forwarding to the parent
        /// binder.
        /// </summary>
        /// <param name="callback">
        /// The change-type callback to invoke.
        /// </param>
        /// <param name="type">
        /// The target type associated with the conversion.
        /// </param>
        /// <param name="text">
        /// The string value to convert.
        /// </param>
        /// <param name="options">
        /// The conversion options, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the conversion, if any.
        /// </param>
        /// <param name="clientData">
        /// The extra client data for the conversion, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control marshalling behavior for the conversion.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting converted value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode InvokeChangeTypeCallback(
            ChangeTypeCallback callback,
            Type type,
            string text,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            ref MarshalFlags marshalFlags,
            ref object value,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.InvokeChangeTypeCallback(
                callback, type, text, options, cultureInfo, clientData,
                ref marshalFlags, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a value to its target type using the supplied
        /// change-type data, by forwarding to the parent binder.
        /// </summary>
        /// <param name="changeTypeData">
        /// The data describing the value to convert and how to convert it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode ChangeType(
            IChangeTypeData changeTypeData,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.ChangeType(changeTypeData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reorders the candidate method indexes (and their
        /// associated argument arrays) into a preferred selection order, by
        /// forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type whose methods are being reordered.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during reordering, if any.
        /// </param>
        /// <param name="methods">
        /// The candidate methods being considered.
        /// </param>
        /// <param name="reorderFlags">
        /// The flags that control how the indexes are reordered.
        /// </param>
        /// <param name="methodIndexList">
        /// Upon success, receives the reordered list of method indexes.
        /// </param>
        /// <param name="argsList">
        /// Upon success, receives the reordered list of argument arrays.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode ReorderMethodIndexes(
            Type type,
            CultureInfo cultureInfo,
            MethodBase[] methods,
            ReorderFlags reorderFlags,
            ref IntList methodIndexList,
            ref ObjectArrayList argsList,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.ReorderMethodIndexes(
                type, cultureInfo, methods, reorderFlags,
                ref methodIndexList, ref argsList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the best-matching method index from the supplied
        /// candidates and arguments, by forwarding to the parent binder.
        /// </summary>
        /// <param name="type">
        /// The type whose methods are being selected from.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during selection, if any.
        /// </param>
        /// <param name="parameterTypes">
        /// The parameter types used to match a candidate.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshal flags used during matching.
        /// </param>
        /// <param name="methods">
        /// The candidate methods being considered.
        /// </param>
        /// <param name="args">
        /// The arguments to be matched against the candidate methods.
        /// </param>
        /// <param name="methodIndexList">
        /// The list of candidate method indexes to choose from.
        /// </param>
        /// <param name="argsList">
        /// The list of argument arrays corresponding to the candidate methods.
        /// </param>
        /// <param name="index">
        /// Upon success, receives the index into
        /// <paramref name="methodIndexList" /> of the selected method.
        /// </param>
        /// <param name="methodIndex">
        /// Upon success, receives the selected method index.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode SelectMethodIndex(
            Type type,
            CultureInfo cultureInfo,
            TypeList parameterTypes,
            MarshalFlagsList parameterMarshalFlags,
            MethodBase[] methods,
            object[] args,
            IntList methodIndexList,
            ObjectArrayList argsList,
            ref int index,
            ref int methodIndex,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.SelectMethodIndex(
                type, cultureInfo, parameterTypes, parameterMarshalFlags,
                methods, args, methodIndexList, argsList, ref index,
                ref methodIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the most appropriate type for a value from the
        /// supplied candidate types, by forwarding to the parent binder.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the type is being selected.
        /// </param>
        /// <param name="oldValue">
        /// The previous value, if any.
        /// </param>
        /// <param name="newValue">
        /// The new value for which a type is being selected.
        /// </param>
        /// <param name="types">
        /// The candidate types to choose from.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use during selection, if any.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags that influence type selection.
        /// </param>
        /// <param name="type">
        /// Upon success, receives the selected type.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, another
        /// <see cref="ReturnCode" /> value on failure.
        /// </returns>
        public ReturnCode SelectType(
            Interpreter interpreter,
            object oldValue,
            object newValue,
            TypeList types,
            CultureInfo cultureInfo,
            ObjectFlags objectFlags,
            ref Type type,
            ref Result error
            )
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.SelectType(
                interpreter, oldValue, newValue, types, cultureInfo,
                objectFlags, ref type, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this script binder has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this script binder has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this script binder has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(Class9));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this script binder.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    RemoveClass2();

                    //
                    // WARNING: Not owned, do not dispose.
                    //
                    interpreter = null;
                    parentBinder = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this script binder and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this script binder, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Class9()
        {
            Dispose(false);
        }
        #endregion
    }
}
