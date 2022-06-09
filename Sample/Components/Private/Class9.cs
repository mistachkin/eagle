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
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("d8e4e05f-cd71-4cbe-9ba6-f87a653a96d2")]
    internal sealed class Class9 : IScriptBinder, IGetInterpreter, IDisposable
    {
        #region Public Constructors
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
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBinder Members
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

        private IScriptBinder parentBinder;
        public IScriptBinder ParentBinder
        {
            get { CheckDisposed(); return parentBinder; }
            set { CheckDisposed(); parentBinder = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public bool HasToStringTypes()
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasToStringTypes();
        }

        ///////////////////////////////////////////////////////////////////////

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

        public bool HasChangeTypes()
        {
            CheckDisposed();

            if (parentBinder == null)
                throw new InvalidOperationException();

            return parentBinder.HasChangeTypes();
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(Class9));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Class9()
        {
            Dispose(false);
        }
        #endregion
    }
}
