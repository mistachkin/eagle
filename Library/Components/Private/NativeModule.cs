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
    /// <summary>
    /// This class represents a native (unmanaged) library module that has been
    /// loaded into the process on behalf of an interpreter.  It tracks the
    /// associated file name, native module handle, and reference count, and
    /// provides methods to load and unload the underlying library.
    /// </summary>
    [ObjectId("20e0292a-25ad-4817-9ca3-2b86d7f4f002")]
    internal sealed class NativeModule : IModule, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the loading state of this
        /// native module.
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// The interpreter that this native module is associated with.  This
        /// object does not own the interpreter.
        /// </summary>
        private Interpreter interpreter;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a native module from the specified identity, interpreter,
        /// flags, file name, and token.
        /// </summary>
        /// <param name="name">
        /// The name of this native module.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this native module.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this native module.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this native module, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that this native module is associated with.  This
        /// object does not own the interpreter.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the loading and unloading behavior of this
        /// native module.
        /// </param>
        /// <param name="fileName">
        /// The fully qualified file name of the native library to load.
        /// </param>
        /// <param name="token">
        /// The token used to identify this native module within the
        /// interpreter.
        /// </param>
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
        /// <summary>
        /// Stores the name of this native module.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this native module.
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
        /// Stores the identifier kind of this native module.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this native module.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this native module.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this native module.
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
        /// Stores the client data associated with this native module.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this native module.
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
        /// Stores the group of this native module.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this native module.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this native module.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this native module.
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
        /// Stores the token used to identify this native module within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this native module within the
        /// interpreter.
        /// </summary>
        public long Token
        {
            get { CheckDisposed(); return token; }
            set { CheckDisposed(); token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IModule Members
        /// <summary>
        /// Stores the flags controlling the loading and unloading behavior of
        /// this native module.
        /// </summary>
        private ModuleFlags flags;
        /// <summary>
        /// Gets the flags controlling the loading and unloading behavior of this
        /// native module.
        /// </summary>
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

        /// <summary>
        /// Stores the fully qualified file name of the native library backing
        /// this native module.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets the fully qualified file name of the native library backing this
        /// native module.
        /// </summary>
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

        /// <summary>
        /// Stores the native handle to the loaded library, or
        /// <see cref="IntPtr.Zero" /> when the library is not currently loaded.
        /// </summary>
        private IntPtr module;
        /// <summary>
        /// Gets the native handle to the loaded library, or
        /// <see cref="IntPtr.Zero" /> when the library is not currently loaded.
        /// </summary>
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

        /// <summary>
        /// Stores the number of outstanding references to the loaded native
        /// library.
        /// </summary>
        private int referenceCount;
        /// <summary>
        /// Gets the number of outstanding references to the loaded native
        /// library.
        /// </summary>
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

        /// <summary>
        /// This method loads the native library backing this native module into
        /// the process.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// library could not be loaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode Load(
            ref Result error
            )
        {
            CheckDisposed();

            int loaded = 0;

            return Load(ref loaded, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the native library backing this native module into
        /// the process, tracking the number of successful load operations.
        /// </summary>
        /// <param name="loaded">
        /// Incremented by one when the native library is loaded successfully (or
        /// is already loaded).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// library could not be loaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method unloads the native library backing this native module
        /// from the process.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// library could not be unloaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode Unload(
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateUnload(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unloads the native library backing this native module
        /// from the process, tracking the number of successful load operations.
        /// </summary>
        /// <param name="loaded">
        /// Decremented by one when the native library is unloaded successfully.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// library could not be unloaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method creates a new native module for the specified file and
        /// loads its native library into the process.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that the new native module will be associated with.
        /// This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to assign to the new native module.  This parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the loading and unloading behavior of the new
        /// native module.
        /// </param>
        /// <param name="fileName">
        /// The fully qualified file name of the native library to load.
        /// </param>
        /// <param name="loaded">
        /// Incremented by one when the native library is loaded successfully.
        /// </param>
        /// <param name="module">
        /// Must be null on input; upon success, receives the newly created
        /// native module.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// module could not be created or loaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

            if (FlagOps.HasFlags(
                    flags, ModuleFlags.TrustedOnly, true) &&
                !RuntimeOps.IsFileTrusted(
                    interpreter, null, fileName, IntPtr.Zero))
            {
                error = "module file is not Authenticode signed or cannot be trusted";
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
        /// <summary>
        /// This method unloads the native library backing this native module
        /// from the process, without checking whether this object has been
        /// disposed.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// library could not be unloaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method unloads the native library backing this native module
        /// from the process, decrementing the reference count and only freeing
        /// the library when no references remain.  It does not throw if this
        /// object has been disposed.
        /// </summary>
        /// <param name="loaded">
        /// Decremented by one when the native library is unloaded successfully.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the native
        /// library could not be unloaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// Finalizes this native module, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~NativeModule()
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this native module.
        /// </summary>
        /// <returns>
        /// The file name of the native library backing this native module, or
        /// an empty string when no file name is available.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return (fileName != null) ? fileName : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this native module has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this native module has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this native module has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(NativeModule).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this native module.  It
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
        /// <summary>
        /// This method releases all resources held by this native module and
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
