/*
 * TclModule.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Components.Private.Tcl
{
    /// <summary>
    /// This class represents a single loaded native Tcl module (i.e. the
    /// shared library containing the native Tcl library), tracking its file
    /// name, its operating system module handle, and the reference and lock
    /// counts used to manage its lifetime within the native Tcl integration
    /// subsystem.
    /// </summary>
    [ObjectId("1c2bc9c1-93da-4106-8fd2-34a36323da28")]
    internal sealed class TclModule
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of the native Tcl module class.
        /// </summary>
        public TclModule()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the native Tcl module class using the
        /// specified file name, module handle, and reference count, with an
        /// initial lock count of zero.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native Tcl module.
        /// </param>
        /// <param name="module">
        /// The operating system module handle for the native Tcl module.
        /// </param>
        /// <param name="referenceCount">
        /// The initial reference count for the native Tcl module.
        /// </param>
        public TclModule(
            string fileName,
            IntPtr module,
            int referenceCount
            )
            : this(fileName, module, referenceCount, 0)
        {
            this.fileName = fileName;
            this.module = module;
            this.referenceCount = referenceCount;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the native Tcl module class using the
        /// specified file name, module handle, reference count, and lock
        /// count.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native Tcl module.
        /// </param>
        /// <param name="module">
        /// The operating system module handle for the native Tcl module.
        /// </param>
        /// <param name="referenceCount">
        /// The initial reference count for the native Tcl module.
        /// </param>
        /// <param name="lockCount">
        /// The initial lock count for the native Tcl module.
        /// </param>
        public TclModule(
            string fileName,
            IntPtr module,
            int referenceCount,
            int lockCount
            )
            : this()
        {
            this.fileName = fileName;
            this.module = module;
            this.referenceCount = referenceCount;
            this.lockCount = lockCount;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Stores the file name of the native Tcl module.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets the file name of the native Tcl module.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the operating system module handle for the native Tcl
        /// module.
        /// </summary>
        private IntPtr module;
        /// <summary>
        /// Gets the operating system module handle for the native Tcl module.
        /// </summary>
        public IntPtr Module
        {
            get { return module; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current reference count for the native Tcl module.
        /// </summary>
        private int referenceCount;
        /// <summary>
        /// Gets the current reference count for the native Tcl module.
        /// </summary>
        public int ReferenceCount
        {
            get { return referenceCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current lock count for the native Tcl module.
        /// </summary>
        private int lockCount;
        /// <summary>
        /// Gets the current lock count for the native Tcl module.
        /// </summary>
        public int LockCount
        {
            get { return lockCount; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method returns the operating system module handle for the
        /// native Tcl module, optionally returning a placeholder handle that
        /// merely indicates whether a valid handle is present.
        /// </summary>
        /// <param name="load">
        /// Non-zero to return the actual module handle; zero to return a
        /// placeholder handle indicating only whether the module handle is
        /// valid.
        /// </param>
        /// <returns>
        /// The actual module handle when <paramref name="load" /> is non-zero;
        /// otherwise, a non-zero placeholder handle if the module handle is
        /// valid or a zero handle if it is not.
        /// </returns>
        public IntPtr GetModule(
            bool load
            )
        {
            if (load)
                return module;

            return NativeOps.IsValidHandle(module) ?
                NativeOps.IntPtrOne : IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the native Tcl module is still valid and
        /// loaded, checking the stored file name and module handle and, on
        /// Windows, confirming that the handle still matches the one currently
        /// associated with the file name.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the native Tcl module was verified
        /// successfully; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode VerifyModule(
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid Tcl native module file name";
                return ReturnCode.Error;
            }

            if (!NativeOps.IsValidHandle(module))
            {
                error = "invalid Tcl native module handle";
                return ReturnCode.Error;
            }

            //
            // HACK: We cannot actually verify the native module handle on any
            //       non-Windows operating system.
            //
            if (!PlatformOps.IsWindowsOperatingSystem())
                return ReturnCode.Ok;

            try
            {
                IntPtr newModule = NativeOps.GetModuleHandle(fileName);

                if (newModule == IntPtr.Zero)
                {
                    error = String.Format(
                        "bad Tcl native module handle {0}, file name {1} is " +
                        "no longer loaded", module, FormatOps.WrapOrNull(
                        fileName));

                    TraceOps.DebugTrace(String.Format(
                        "VerifyModule: {0}", FormatOps.WrapOrNull(error)),
                        typeof(TclModule).Name, TracePriority.NativeError);

                    return ReturnCode.Error;
                }

                if (newModule != module)
                {
                    //
                    // NOTE: This situation should really never happen.  If it
                    //       does, that indicates that the native Tcl module
                    //       was unloaded and then reloaded out from under the
                    //       native Tcl integration subsystem.
                    //
                    error = String.Format(
                        "bad Tcl native module handle {0}, got {1} for file " +
                        "name {2}", module, newModule, FormatOps.WrapOrNull(
                        fileName));

                    TraceOps.DebugTrace(String.Format(
                        "VerifyModule: {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(TclModule).Name, TracePriority.NativeError);

                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically increments the reference count for the
        /// native Tcl module.
        /// </summary>
        /// <returns>
        /// The reference count after it has been incremented.
        /// </returns>
        public int AddReference()
        {
            return Interlocked.Increment(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically decrements the reference count for the
        /// native Tcl module.
        /// </summary>
        /// <returns>
        /// The reference count after it has been decremented.
        /// </returns>
        public int ReleaseReference()
        {
            return Interlocked.Decrement(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically increments the lock count for the native Tcl
        /// module.
        /// </summary>
        /// <returns>
        /// The lock count after it has been incremented.
        /// </returns>
        public int Lock()
        {
            return Interlocked.Increment(ref lockCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically decrements the lock count for the native Tcl
        /// module.
        /// </summary>
        /// <returns>
        /// The lock count after it has been decremented.
        /// </returns>
        public int Unlock()
        {
            return Interlocked.Decrement(ref lockCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the lock count for the native Tcl module based
        /// on whether the module is being unloaded and whether it should be
        /// unlocked.
        /// </summary>
        /// <param name="unload">
        /// Non-zero if the native Tcl module is being unloaded; zero if it is
        /// being loaded, in which case the lock count is incremented.
        /// </param>
        /// <param name="unlock">
        /// Non-zero to decrement the lock count when the native Tcl module is
        /// being unloaded; zero to leave the lock count unchanged.
        /// </param>
        /// <returns>
        /// The resulting lock count after any adjustment.
        /// </returns>
        public int AdjustLockCount(
            bool unload,
            bool unlock
            )
        {
            if (!unload)
                return Lock();

            return unlock ? Unlock() : lockCount;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a list of name/value pairs describing the
        /// current state of the native Tcl module, including its file name,
        /// module handle, reference count, and lock count.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs describing the native Tcl module.
        /// </returns>
        public StringPairList ToList()
        {
            StringPairList list = new StringPairList();

            list.Add("fileName", fileName);
            list.Add("module", module.ToString());
            list.Add("referenceCount", referenceCount.ToString());
            list.Add("lockCount", lockCount.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of the native Tcl
        /// module.
        /// </summary>
        /// <returns>
        /// A string representation of the native Tcl module.
        /// </returns>
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion
    }
}
