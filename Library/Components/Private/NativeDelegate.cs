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
    /// <summary>
    /// This class represents a native (unmanaged) function exported from a
    /// native module (shared library) that has been wrapped so it can be
    /// resolved and invoked from managed code.  It looks up the export by name
    /// within a loaded <see cref="IModule" />, obtains its address, and builds
    /// a managed delegate (of a dynamically created delegate type) for the
    /// function pointer so the native function can be called via
    /// <see cref="Invoke(object[], ref object, ref Result)" />.  It implements
    /// <see cref="IDelegate" /> and owns the lifetime of the underlying native
    /// module, unloading it when disposed.
    /// </summary>
    [ObjectId("75ccd0b0-f629-4330-aa74-28f2aed51414")]
    internal sealed class NativeDelegate : IDelegate, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// native delegate.
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// The interpreter that owns this native delegate, used for diagnostic
        /// reporting.  This object is not owned by this native delegate.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// The number of times the underlying native module has been loaded by
        /// this native delegate, used to balance loads against unloads.
        /// </summary>
        private int moduleLoaded;

        /// <summary>
        /// The managed delegate that wraps the resolved native function
        /// pointer.  There is intentionally no property for this field; use
        /// <see cref="Invoke(object[], ref object, ref Result)" /> to call it.
        /// </summary>
        private Delegate @delegate; // NOTE: No property, use Invoke().
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a native delegate from the fully specified set of
        /// identity, type, and native function location parameters.  The native
        /// function is not resolved by this constructor; call
        /// <see cref="Resolve(IModule, string, ref Result)" /> to perform the
        /// lookup.
        /// </summary>
        /// <param name="name">
        /// The name of this native delegate.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group associated with this native delegate.  This parameter may
        /// be null.
        /// </param>
        /// <param name="description">
        /// The description of this native delegate.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this native delegate, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that owns this native delegate.  This object is not
        /// owned by this native delegate.
        /// </param>
        /// <param name="callingConvention">
        /// The calling convention of the native function.
        /// </param>
        /// <param name="returnType">
        /// The managed return type of the native function.
        /// </param>
        /// <param name="parameterTypes">
        /// The managed parameter types of the native function.
        /// </param>
        /// <param name="type">
        /// The dynamically created delegate type used to invoke the native
        /// function.
        /// </param>
        /// <param name="module">
        /// The native module that exports the native function.
        /// </param>
        /// <param name="functionName">
        /// The name of the exported native function to resolve.
        /// </param>
        /// <param name="address">
        /// The address of the resolved native function, if already known.
        /// </param>
        /// <param name="token">
        /// The token identifying this native delegate.
        /// </param>
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
        /// <summary>
        /// This method unloads the underlying native module associated with
        /// this native delegate.  It assumes the synchronization lock is
        /// already held.
        /// </summary>
        /// <param name="loaded">
        /// The number of outstanding loads of the native module; this is
        /// decremented as the module is unloaded.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// module could not be unloaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
        private ReturnCode UnloadModule(
            ref int loaded,
            ref Result error
            )
        {
            return RuntimeOps.UnloadNativeModule(
                module, ref loaded, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally unloads the native module currently
        /// associated with this native delegate when it differs from the
        /// supplied module, then clears the current module reference.  It does
        /// nothing when there is no current module or when the supplied module
        /// is the same as the current one.
        /// </summary>
        /// <param name="module">
        /// The native module that is about to be associated with this native
        /// delegate; if it is the same as the current module, no unloading is
        /// performed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the current
        /// native module could not be unloaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method performs the actual resolution of the native function:
        /// it validates the delegate type, module, and function name, loads the
        /// native module, looks up the address of the exported function, and
        /// builds the managed delegate used to invoke it.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// function could not be resolved.
        /// </param>
        /// <param name="exception">
        /// Upon failure caused by an exception, receives the exception that was
        /// caught during resolution.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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
        /// <summary>
        /// The name of this native delegate.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name of this native delegate.
        /// </summary>
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this native delegate.
        /// </summary>
        private IdentifierKind kind;

        /// <summary>
        /// Gets or sets the kind of identifier represented by this native
        /// delegate.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier of this native delegate.
        /// </summary>
        private Guid id;

        /// <summary>
        /// Gets or sets the unique identifier of this native delegate.
        /// </summary>
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this native delegate, if any.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the client data associated with this native delegate.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The group associated with this native delegate.
        /// </summary>
        private string group;

        /// <summary>
        /// Gets or sets the group associated with this native delegate.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of this native delegate.
        /// </summary>
        private string description;

        /// <summary>
        /// Gets or sets the description of this native delegate.
        /// </summary>
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The token identifying this native delegate.
        /// </summary>
        private long token;

        /// <summary>
        /// Gets or sets the token identifying this native delegate.
        /// </summary>
        public long Token
        {
            get { CheckDisposed(); return token; }
            set { CheckDisposed(); token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegate Members
        /// <summary>
        /// The calling convention of the native function.
        /// </summary>
        private CallingConvention callingConvention;

        /// <summary>
        /// Gets the calling convention of the native function.
        /// </summary>
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

        /// <summary>
        /// The managed return type of the native function.
        /// </summary>
        private Type returnType;

        /// <summary>
        /// Gets the managed return type of the native function.
        /// </summary>
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

        /// <summary>
        /// The managed parameter types of the native function.
        /// </summary>
        private TypeList parameterTypes;

        /// <summary>
        /// Gets the managed parameter types of the native function.
        /// </summary>
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

        /// <summary>
        /// The dynamically created delegate type used to invoke the native
        /// function.
        /// </summary>
        private Type type;

        /// <summary>
        /// Gets the dynamically created delegate type used to invoke the native
        /// function.
        /// </summary>
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

        /// <summary>
        /// The native module that exports the native function.
        /// </summary>
        private IModule module;

        /// <summary>
        /// Gets the native module that exports the native function.
        /// </summary>
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

        /// <summary>
        /// The name of the exported native function.
        /// </summary>
        private string functionName;

        /// <summary>
        /// Gets the name of the exported native function.
        /// </summary>
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

        /// <summary>
        /// The address of the resolved native function.
        /// </summary>
        private IntPtr address;

        /// <summary>
        /// Gets the address of the resolved native function.
        /// </summary>
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

        /// <summary>
        /// Gets the reflected method information for the invocation method of
        /// the managed delegate that wraps the native function, or null if the
        /// native function has not yet been resolved.
        /// </summary>
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

        /// <summary>
        /// This method resolves the native function, optionally using the
        /// supplied native module and function name, looking up its address and
        /// building the managed delegate used to invoke it.
        /// </summary>
        /// <param name="module">
        /// The native module to associate with this native delegate prior to
        /// resolution; if not null, any previously associated module is
        /// unloaded first.  This parameter may be null to keep the current
        /// module.
        /// </param>
        /// <param name="functionName">
        /// The name of the exported native function to resolve; if not null, it
        /// replaces the current function name.  This parameter may be null to
        /// keep the current function name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// function could not be resolved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method resolves the native function, optionally using the
        /// supplied native module and function name, looking up its address and
        /// building the managed delegate used to invoke it.
        /// </summary>
        /// <param name="module">
        /// The native module to associate with this native delegate prior to
        /// resolution; if not null, any previously associated module is
        /// unloaded first.  This parameter may be null to keep the current
        /// module.
        /// </param>
        /// <param name="functionName">
        /// The name of the exported native function to resolve; if not null, it
        /// replaces the current function name.  This parameter may be null to
        /// keep the current function name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// function could not be resolved.
        /// </param>
        /// <param name="exception">
        /// Upon failure caused by an exception, receives the exception that was
        /// caught during resolution.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method unresolves the native function, unloading the underlying
        /// native module and clearing the managed delegate, address, function
        /// name, and module references.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// function could not be unresolved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
        public ReturnCode Unresolve(
            ref Result error
            )
        {
            CheckDisposed();

            Exception exception = null;

            return Unresolve(ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unresolves the native function, unloading the underlying
        /// native module and clearing the managed delegate, address, function
        /// name, and module references.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// function could not be unresolved.
        /// </param>
        /// <param name="exception">
        /// Upon failure caused by an exception, receives the exception that was
        /// caught during unresolution.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method invokes the resolved native function with the supplied
        /// arguments and returns its result.
        /// </summary>
        /// <param name="args">
        /// The arguments to pass to the native function.  This parameter may be
        /// null when the native function takes no arguments.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, receives the value returned by the native function.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// function could not be invoked.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method invokes the resolved native function with the supplied
        /// arguments and returns its result.
        /// </summary>
        /// <param name="args">
        /// The arguments to pass to the native function.  This parameter may be
        /// null when the native function takes no arguments.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, receives the value returned by the native function.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// function could not be invoked.
        /// </param>
        /// <param name="exception">
        /// Upon failure caused by an exception, receives the exception that was
        /// caught during invocation.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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
        /// <summary>
        /// This method returns a string representation of this native delegate.
        /// </summary>
        /// <returns>
        /// The name of the exported native function, or an empty string when no
        /// function name has been set.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return (functionName != null) ? functionName : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this native delegate, releasing any unmanaged resources it
        /// still holds (including unloading the underlying native module).
        /// </summary>
        ~NativeDelegate()
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this native delegate has been disposed and is no longer
        /// usable.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an <see cref="ObjectDisposedException" /> if this
        /// native delegate has been disposed and the owning interpreter is
        /// configured to throw on disposed object access.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(NativeDelegate).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this native delegate,
        /// disposing managed resources only when requested and always releasing
        /// unmanaged resources (including unloading the underlying native
        /// module and clearing the interpreter reference).
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (so managed resources may also be
        /// released); zero if it is being called from the finalizer.
        /// </param>
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
        /// <summary>
        /// This method releases all resources used by this native delegate and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
