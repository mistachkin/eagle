/*
 * NativeDelegate.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("75ccd0b0-f629-4330-aa74-28f2aed51414")]
    internal sealed class NativeDelegate : IDelegate, IDisposable
    {
        #region Private Data
        private readonly object syncRoot = new object();
        private Interpreter interpreter;
        private int moduleLoaded;
        private Delegate @delegate; // NOTE: No property, use Invoke().
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public NativeDelegate(
            string name,
            string group,
            string description,
            IClientData clientData,
            Interpreter interpreter,
            CallingConvention callingConvention,
            Type returnType,
            TypeList parameterTypes,
            Type type,
            IModule module,
            string functionName,
            IntPtr address,
            long token
            )
        {
            this.kind = IdentifierKind.NativeDelegate;
            this.id = Guid.Empty;
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.interpreter = interpreter;
            this.callingConvention = callingConvention;
            this.returnType = returnType;
            this.parameterTypes = parameterTypes;
            this.type = type;
            this.module = module;
            this.functionName = functionName;
            this.address = address;
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        //
        // NOTE: This method assumes the lock is held.
        //
        private ReturnCode UnloadModule(
            ref int loaded,
            ref Result error
            )
        {
            return RuntimeOps.UnloadNativeModule(
                module, ref loaded, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode MaybeUnloadModule(
            IModule module,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (this.module == null)
                    return ReturnCode.Ok;

                if (Object.ReferenceEquals(module, this.module))
                    return ReturnCode.Ok;

                if (moduleLoaded > 0)
                {
                    if (RuntimeOps.UnloadNativeModule(
                            this.module, ref moduleLoaded,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                this.module = null;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode PrivateResolve(
            ref Result error,
            ref Exception exception
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (type == null)
                {
                    error = "invalid type";
                    return ReturnCode.Error;
                }

                if (!ConversionOps.IsDelegateType(type, false))
                {
                    error = "type is not a delegate type";
                    return ReturnCode.Error;
                }

                if (module == null)
                {
                    error = "invalid module";
                    return ReturnCode.Error;
                }

                if (String.IsNullOrEmpty(functionName))
                {
                    error = "invalid export name";
                    return ReturnCode.Error;
                }

                if (module.Load(ref moduleLoaded, ref error) == ReturnCode.Ok)
                {
                    try
                    {
                        int lastError;

                        address = NativeOps.GetProcAddress(
                            module.Module, functionName,
                            out lastError); /* throw */

                        if (address != IntPtr.Zero)
                        {
                            //
                            // NOTE: The GetDelegateForFunctionPointer method
                            //       of the Marshal class is how we get the
                            //       delegate we need to invoke the library
                            //       function itself and this is why we went
                            //       through all the trouble to creating and
                            //       populating the delegate type dynamically.
                            //       To see exactly how this is accomplished,
                            //       please refer to CreateNativeDelegateType
                            //       in DelegateOps).
                            //
                            @delegate = Marshal.GetDelegateForFunctionPointer(
                                address, type); /* throw */

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "GetProcAddress({1}, \"{2}\") failed with " +
                                "error {0}: {3}", lastError, module,
                                functionName, NativeOps.GetDynamicLoadingError(
                                lastError));
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;

                        exception = e;
                    }
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public long Token
        {
            get { CheckDisposed(); return token; }
            set { CheckDisposed(); token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegate Members
        private CallingConvention callingConvention;
        public CallingConvention CallingConvention
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return callingConvention;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type returnType;
        public Type ReturnType
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return returnType;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private TypeList parameterTypes;
        public TypeList ParameterTypes
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return parameterTypes;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type type;
        public Type Type
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return type;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private IModule module;
        public IModule Module
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return module;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private string functionName;
        public string FunctionName
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return functionName;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private IntPtr address;
        public IntPtr Address
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return address;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodInfo MethodInfo
        {
            get
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (@delegate != null)
                    {
                        Type type = @delegate.GetType();

                        if (type != null)
                        {
                            return type.GetMethod(
                                DelegateOps.InvokeMethodName,
                                ObjectOps.GetBindingFlags(
                                    MetaBindingFlags.PublicInstance,
                                    true));
                        }
                    }
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Resolve(
            IModule module,
            string functionName,
            ref Result error
            )
        {
            CheckDisposed();

            Exception exception = null;

            return Resolve(module, functionName, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Resolve(
            IModule module,
            string functionName,
            ref Result error,
            ref Exception exception
            )
        {
            CheckDisposed();

            ReturnCode code;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (module != null)
                {
                    code = MaybeUnloadModule(module, ref error);

                    if (code == ReturnCode.Ok)
                        this.module = module;
                }
                else
                {
                    code = ReturnCode.Ok;
                }

                if ((code == ReturnCode.Ok) && (functionName != null))
                    this.functionName = functionName;
            }

            if (code == ReturnCode.Ok)
                code = PrivateResolve(ref error, ref exception);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unresolve(
            ref Result error
            )
        {
            CheckDisposed();

            Exception exception = null;

            return Unresolve(ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unresolve(
            ref Result error,
            ref Exception exception
            )
        {
            CheckDisposed();

            ReturnCode code;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((moduleLoaded > 0) && (module != null))
                    code = module.Unload(ref moduleLoaded, ref error);
                else
                    code = ReturnCode.Ok;

                if (code == ReturnCode.Ok)
                {
                    if (@delegate != null)
                        @delegate = null;

                    if (address != IntPtr.Zero)
                        address = IntPtr.Zero;

                    if (functionName != null)
                        functionName = null;

                    if (module != null)
                        module = null;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Invoke(
            object[] args,
            ref object returnValue,
            ref Result error
            )
        {
            CheckDisposed();

            Exception exception = null;

            return Invoke(args, ref returnValue, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Invoke(
            object[] args,
            ref object returnValue,
            ref Result error,
            ref Exception exception
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    if (@delegate != null)
                    {
                        returnValue = @delegate.DynamicInvoke(args);

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "invalid delegate";
                    }
                }
                catch (Exception e)
                {
                    error = e;

                    exception = e;
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return (functionName != null) ? functionName : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~NativeDelegate()
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(NativeDelegate).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        //
                        // NOTE: Get rid of our type references.
                        //
                        returnType = null;
                        parameterTypes = null;
                        type = null;

                        //
                        // NOTE: Get rid of the native function name and
                        //       address we looked up previously, if any.
                        //
                        functionName = null;
                        address = IntPtr.Zero;

                        //
                        // NOTE: Get rid of other stuff...
                        //
                        name = null;
                        clientData = null;
                        description = null;

                        //
                        // NOTE: Get rid of the delegate object itself.
                        //
                        @delegate = null;
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    //
                    // NOTE: Finally, unload our underlying native module.
                    //
                    if (module != null)
                    {
                        ReturnCode unloadCode;
                        Result unloadError = null;

                        unloadCode = UnloadModule(
                            ref moduleLoaded, ref unloadError);

                        if (unloadCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, unloadCode, unloadError);
                        }

                        module = null;
                    }

                    //
                    // NOTE: We do not own the interpreter, just clear our
                    //       reference to it.
                    //
                    interpreter = null;

                    //
                    // NOTE: This object is now disposed.
                    //
                    disposed = true;
                }
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
    }
}
