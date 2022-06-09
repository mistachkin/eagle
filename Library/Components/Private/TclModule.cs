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
    [ObjectId("1c2bc9c1-93da-4106-8fd2-34a36323da28")]
    internal sealed class TclModule
    {
        #region Public Constructors
        public TclModule()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        private string fileName;
        public string FileName
        {
            get { return fileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IntPtr module;
        public IntPtr Module
        {
            get { return module; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int referenceCount;
        public int ReferenceCount
        {
            get { return referenceCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int lockCount;
        public int LockCount
        {
            get { return lockCount; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        public int AddReference()
        {
            return Interlocked.Increment(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public int ReleaseReference()
        {
            return Interlocked.Decrement(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public int Lock()
        {
            return Interlocked.Increment(ref lockCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public int Unlock()
        {
            return Interlocked.Decrement(ref lockCount);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion
    }
}
