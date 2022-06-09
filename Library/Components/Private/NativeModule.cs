/*
 * NativeModule.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("20e0292a-25ad-4817-9ca3-2b86d7f4f002")]
    internal sealed class NativeModule : IModule, IDisposable
    {
        #region Private Data
        private readonly object syncRoot = new object();
        private Interpreter interpreter;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public NativeModule(
            string name,
            string group,
            string description,
            IClientData clientData,
            Interpreter interpreter,
            ModuleFlags flags,
            string fileName,
            long token
            )
        {
            this.kind = IdentifierKind.NativeModule;
            this.id = Guid.Empty;
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.interpreter = interpreter;
            this.flags = flags;
            this.fileName = fileName;
            this.token = token;
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

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
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

        #region IModule Members
        private ModuleFlags flags;
        public ModuleFlags Flags
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return flags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fileName;
        public string FileName
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return fileName;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private IntPtr module;
        public IntPtr Module
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

        private int referenceCount;
        public int ReferenceCount
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return referenceCount;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Load(
            ref Result error
            )
        {
            CheckDisposed();

            int loaded = 0;

            return Load(ref loaded, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Load(
            ref int loaded,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    if (module != IntPtr.Zero)
                        return ReturnCode.Ok;

                    if (String.IsNullOrEmpty(fileName))
                    {
                        error = "invalid file name";
                        return ReturnCode.Error;
                    }

                    int lastError;

                    module = NativeOps.LoadLibrary(
                        fileName, out lastError); /* throw */

                    if (NativeOps.IsValidHandle(module))
                    {
                        Interlocked.Increment(ref loaded);
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "LoadLibrary({1}) failed with error {0}: {2}",
                            lastError, FormatOps.WrapOrNull(fileName),
                            NativeOps.GetDynamicLoadingError(lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
                finally
                {
                    //
                    // NOTE: If the module handle is valid then we know
                    //       the module was loaded successfully -OR- was
                    //       already loaded; therefore, increment the
                    //       reference count.
                    //
                    if (module != IntPtr.Zero)
                        Interlocked.Increment(ref referenceCount);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unload(
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateUnload(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Unload(
            ref int loaded,
            ref Result error
            )
        {
            CheckDisposed();

            return UnloadNoThrow(ref loaded, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        public static ReturnCode Load(
            Interpreter interpreter,
            string name,
            ModuleFlags flags,
            string fileName,
            ref int loaded,
            ref IModule module,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (module != null)
            {
                error = "cannot overwrite valid native module";
                return ReturnCode.Error;
            }

            NativeModule newModule = new NativeModule(
                name, null, null, _Public.ClientData.Empty,
                interpreter, flags, fileName, 0);

            if (newModule.Load(ref loaded, ref error) == ReturnCode.Ok)
            {
                module = newModule;
                return ReturnCode.Ok;
            }
            else
            {
                newModule.Dispose();
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private ReturnCode PrivateUnload(
            ref Result error
            )
        {
            int loaded = 1;

            return UnloadNoThrow(ref loaded, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods (Internal Use Only)
        public ReturnCode UnloadNoThrow( /* EXEMPT: object-15.11 */
            ref int loaded,
            ref Result error
            )
        {
            // CheckDisposed(); /* EXEMPT */

            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the module was already loaded previously,
                //       do nothing.
                //
                if (module == IntPtr.Zero)
                    return ReturnCode.Ok;

                //
                // NOTE: If there are still outstanding references to
                //       the native module, do nothing.
                //
                if (Interlocked.Decrement(ref referenceCount) > 0)
                    return ReturnCode.Ok;

                //
                // NOTE: If the native module has been locked in place
                //       (because it cannot be cleanly unloaded?), then
                //       leave it alone.
                //
                if (FlagOps.HasFlags(
                        flags, ModuleFlags.NoUnload, true))
                {
                    return ReturnCode.Ok;
                }

                try
                {
                    int lastError;

                    if (NativeOps.FreeLibrary(
                            module, out lastError)) /* throw */
                    {
                        Interlocked.Decrement(ref loaded);

                        module = IntPtr.Zero;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                            lastError, module, NativeOps.GetDynamicLoadingError(
                            lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~NativeModule()
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return (fileName != null) ? fileName : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(NativeModule).Name);
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
                        // NOTE: Get rid of the module file name.
                        //
                        fileName = null;

                        //
                        // NOTE: Get rid of other stuff...
                        //
                        clientData = null;
                        description = null;

                        //
                        // NOTE: We do not own the interpreter, just
                        //       clear our reference to it.
                        //
                        interpreter = null;
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    ReturnCode unloadCode;
                    Result unloadError = null;

                    unloadCode = PrivateUnload(ref unloadError);

                    if (unloadCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, unloadCode, unloadError);
                    }

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
