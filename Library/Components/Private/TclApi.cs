/*
 * TclApi.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Tcl.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Encodings;
using Eagle._Interfaces.Private.Tcl;

namespace Eagle._Components.Private.Tcl
{
    [ObjectId("8ba3b5de-c1f8-4d89-b75d-2887966a0670")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclApi : ITclApi, IDisposable, ICloneable
    {
        //
        // NOTE: This is the required size for the NativeStubs struct
        //       provided by the native Garuda code.
        //
        // TODO: Update if the number of members (or the size) changes.
        //
        private static readonly int SizeOfNativeStubs = 49 * IntPtr.Size;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Native Stubs Structure
        //
        // WARNING: The size and layout of this structure MUST match the
        //          native "ClrTclStubs" structure defined in the Garuda
        //          source code file "GarudaInt.h" exactly.
        //
        [StructLayout(LayoutKind.Sequential)]
        [ObjectId("7895bd71-e1dc-4fe6-ba57-78bc7a7c1e42")]
        internal struct NativeStubs
        {
            public UIntPtr sizeOf; /* The size of this structure, in bytes. */
            public IntPtr getVersion;
            public IntPtr findExecutable;
            public IntPtr createInterp;
            public IntPtr preserve;
            public IntPtr release;
            public IntPtr objGetVar2;
            public IntPtr objSetVar2;
            public IntPtr unsetVar2;
            public IntPtr init;
            public IntPtr initMemory;
            public IntPtr makeSafe;
            public IntPtr getObjType;
            public IntPtr appendAllObjTypes;
            public IntPtr convertToType;
            public IntPtr createObjCommand;
            public IntPtr deleteCommandFromToken;
            public IntPtr deleteInterp;
            public IntPtr interpDeleted;
            public IntPtr interpActive;
            public IntPtr getErrorLine;
            public IntPtr setErrorLine;
            public IntPtr newObj;
            public IntPtr newUnicodeObj;
            public IntPtr newStringObj;
            public IntPtr newByteArrayObj;
            public IntPtr dbIncrRefCount;
            public IntPtr dbDecrRefCount;
            public IntPtr commandComplete;
            public IntPtr allowExceptions;
            public IntPtr evalObjEx;
            public IntPtr evalFile;
            public IntPtr recordAndEvalObj;
            public IntPtr exprObj;
            public IntPtr substObj;
            public IntPtr cancelEval;
            public IntPtr canceled;
            public IntPtr resetCancellation;
            public IntPtr setInterpCancelFlags;
            public IntPtr doOneEvent;
            public IntPtr resetResult;
            public IntPtr getObjResult;
            public IntPtr setObjResult;
            public IntPtr getUnicodeFromObj;
            public IntPtr getStringFromObj;
            public IntPtr createExitHandler;
            public IntPtr deleteExitHandler;
            public IntPtr finalizeThread;
            public IntPtr finalize;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constants
        //
        // WARNING: Do not change these as they must be UTF-8 encodings.
        //
        public static readonly Encoding FromEncoding = TclEncoding.Tcl;
        public static readonly Encoding ToEncoding = TclEncoding.Tcl;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Static Data
        //
        // NOTE: By default, should Tcl_AllowExceptions be called prior to
        //       evaluating scripts?  This can be overridden on a per-call
        //       basis using the various method overloads that include the
        //       "exceptions" bool argument.
        //
        public static bool DefaultExceptions = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // NOTE: There are 2 pointer size fields in the Interp structure prior to
        //       the errorLine field in Tcl 8.4, 8.5, and 8.6.  This member also
        //       resides in the public Tcl_Interp struct; therefore, it should be
        //       100% reliable.
        //
        private const int PTRS_BEFORE_ERRORLINE = 2;

        //
        // HACK: There are 15 pointer size fields and 7 integer size fields in the
        //       Interp structure prior to the numLevels field in Tcl 8.4, 8.5, and
        //       8.6; however, this is not 100% reliable because it makes various
        //       assumptions about the internal (private) layout of the Interp
        //       structure.
        //
        private const int PTRS_BEFORE_NUMLEVELS = 15;
        private const int INTS_BEFORE_NUMLEVELS = 7;

        //
        // NOTE: The offset into the Tcl_Interp structure for the errorLine member,
        //       in bytes.
        //
        private static int INTERP_ERRORLINE_OFFSET;

        //
        // NOTE: The offset into the Tcl_Interp structure for the numLevels member,
        //       in bytes.
        //
        internal static int INTERP_NUMLEVELS_OFFSET;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private GCHandle handle; /* TclApi */
        private Tcl_ExitProc exitProc;
        private TypeIntPtrDictionary addresses;
        private TypeDelegateDictionary delegates;
        private TypeBoolDictionary optional;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static TclApi()
        {
            Initialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private TclApi(
            Interpreter interpreter,
            TclBuild build,
            string fileName,
            IntPtr module,
            IntPtr stubs,
            LoadFlags loadFlags
            )
        {
            //
            // NOTE: Create the synchronization object for this Tcl instance.
            //
            syncRoot = new object();

            //
            // NOTE: This is the interpreter that now owns this object.
            //
            this.interpreter = interpreter;

            //
            // NOTE: Keep track of the Tcl build instance corresponding to the loaded
            //       version of Tcl.
            //
            this.build = build;

            //
            // NOTE: Keep track of the file name associated with the loaded module so
            //       that the Tcl wrapper object can manage the module reference counts
            //       correctly during unload.
            //
            this.fileName = fileName;

            //
            // NOTE: This module handle cannot be owned by this object because it
            //       may be shared by multiple interpreters.  Normally, it would not
            //       be a big problem to have this object cleanup the module via
            //       FreeLibrary; however, the design of Tcl requires us to call
            //       Tcl_Finalize prior to actually calling FreeLibrary and we cannot
            //       do that until we are sure that no other objects are using the Tcl
            //       library.
            //
            this.module = module;

            //
            // NOTE: This structure pointer cannot be owned by this object because it
            //       may be shared by multiple interpreters.
            //
            this.stubs = stubs;

            //
            // NOTE: Initially, we have no Tcl exit handler.
            //
            this.exitProc = null;

            //
            // NOTE: Setup our load flags.  These are provided directly by the caller
            //       for now.
            //
            this.loadFlags = loadFlags;

            //
            // NOTE: Setup our default unload flags.  For now, these should not ever
            //       be changed except by the test suite (in the tests "library-3.*",
            //       which are specifically designed to test the exit handler).
            //
            this.unloadFlags = UnloadFlags.FromExitHandler;

            //
            // NOTE: Set the default "allow exceptions" setting for Tcl.
            //
            this.exceptions = DefaultExceptions;

            //
            // NOTE: Initialize the dictionaries of addresses and delegates based on
            //       their type signatures.
            //
            InitializeAddresses(true, (stubs != IntPtr.Zero));
            InitializeDelegates(true, (stubs != IntPtr.Zero));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~TclApi() /* throw */
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposing;
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(TclApi).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(bool disposing) /* throw */
        {
            lock (syncRoot)
            {
                if (!disposed)
                {
                    if (!this.disposing)
                    {
                        //
                        // NOTE: We are now disposing this object (prevent re-entrancy).
                        //
                        this.disposing = true;

                        try
                        {
                            //if (disposing)
                            //{
                            //    ////////////////////////////////////
                            //    // dispose managed resources here...
                            //    ////////////////////////////////////
                            //}

                            //////////////////////////////////////
                            // release unmanaged resources here...
                            //////////////////////////////////////

                            //
                            // NOTE: If necessary (and possible), delete the Tcl exit handler.
                            //
                            ReturnCode deleteCode = ReturnCode.Ok;
                            Result deleteError = null;

                            //
                            // NOTE: If we have a valid exit handler then we are still hooked to
                            //       Tcl via our inbound native delegates and we must unhook
                            //       successfully or throw to prevent our internal object state
                            //       from being made inconsistent.
                            //
                            if (exitProc != null)
                            {
                                if (handle.IsAllocated)
                                {
                                    deleteCode = UnsetExitHandler(ref deleteError);
                                }
                                else
                                {
                                    deleteError = "invalid GC handle";
                                    deleteCode = ReturnCode.Error;
                                }
                            }

                            //
                            // NOTE: Did we succeed in deleting the command from Tcl, if it
                            //       was necessary?
                            //
                            if (deleteCode != ReturnCode.Ok)
                            {
                                //
                                // NOTE: If the command deletion was necessary and it failed
                                //       for any reason, complain very loudly.
                                //
                                DebugOps.Complain(interpreter, deleteCode, deleteError);

                                //
                                // NOTE: Also, we must throw an exception here to prevent
                                //       the delegates from being disposed while Tcl still
                                //       refers to them.
                                //
                                throw new ScriptException(deleteCode, deleteError);
                            }

                            //
                            // NOTE: Remove our optional indicators for the Tcl API, we are done
                            //       with them.
                            //
                            if (optional != null)
                            {
                                optional.Clear();
                                optional = null;
                            }

                            //
                            // NOTE: Remove our delegates for the Tcl API, we are done with them.
                            //
                            if (delegates != null)
                            {
                                delegates.Clear();
                                delegates = null;
                            }

                            //
                            // NOTE: We do NOT own this structure pointer; therefore, simply zero
                            //       out our reference to it.
                            //
                            stubs = IntPtr.Zero;

                            //
                            // NOTE: We do NOT own this module handle; therefore, simply zero out
                            //       our reference to it.
                            //
                            module = IntPtr.Zero;

                            //
                            // NOTE: Clear out the file name too.
                            //
                            fileName = null;

                            //
                            // NOTE: We do not own these objects; therefore, we just null out
                            //       the references to them (in case we are the only thing
                            //       keeping them alive).
                            //
                            interpreter = null;

                            //
                            // NOTE: This object is now disposed.
                            //
                            disposed = true;
                        }
                        finally
                        {
                            //
                            // NOTE: We are no longer disposing this object.
                            //
                            this.disposing = false;
                        }
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone() /* DEEP COPY */
        {
            CheckDisposed();

            ITclApi tclApi = null;
            Result error = null;

            if (Copy(ref tclApi, ref error) == ReturnCode.Ok)
            {
                return tclApi;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "copy failed: {0}", error),
                    typeof(TclApi).Name,
                    TracePriority.NativeError);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        public static ITclApi Create(
            Interpreter interpreter,
            TclBuild build,
            string fileName,
            IntPtr module,
            IntPtr stubs,
            LoadFlags loadFlags,
            ref Result error
            )
        {
            TclApi result = new TclApi(
                interpreter, build, fileName, module, stubs, loadFlags);

            if (!result.SetFileName(fileName, module, ref error) ||
                !result.SetDelegates(ref error))
            {
                result.Dispose();
                result = null;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region File Name Management Members
        private bool SetFileName(
            string fileName,
            IntPtr module,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(fileName))
                return true; // NOTE: Already "resolved".

            try
            {
                lock (syncRoot)
                {
                    if (module != IntPtr.Zero)
                    {
                        string localFileName = PathOps.GetNativeModuleFileName(
                            module, ref error);

                        if (localFileName != null)
                        {
                            this.fileName = localFileName;
                            return true;
                        }
                    }
                    else
                    {
                        error = "module is invalid";
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Function Pointer Management Members
        private static object NativeStubsFromIntPtr(
            IntPtr stubs,
            ref Result error
            )
        {
            try
            {
                return Marshal.PtrToStructure(stubs, typeof(NativeStubs));
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private void InitializeAddresses(
            bool clear,
            bool stubs /* NOT USED */
            )
        {
            lock (syncRoot)
            {
                if (addresses == null)
                    addresses = new TypeIntPtrDictionary();
                else if (clear)
                    addresses.Clear();

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_GetVersion), IntPtr.Zero);
                addresses.Add(typeof(Tcl_FindExecutable), IntPtr.Zero);

                ///////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
                addresses.Add(typeof(TclKit_SetKitPath), IntPtr.Zero);
#endif

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_CreateInterp), IntPtr.Zero);
                addresses.Add(typeof(Tcl_Preserve), IntPtr.Zero);
                addresses.Add(typeof(Tcl_Release), IntPtr.Zero);
                addresses.Add(typeof(Tcl_ObjGetVar2), IntPtr.Zero);
                addresses.Add(typeof(Tcl_ObjSetVar2), IntPtr.Zero);
                addresses.Add(typeof(Tcl_UnsetVar2), IntPtr.Zero);

                ///////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
                addresses.Add(typeof(TclKit_AppInit), IntPtr.Zero);
#endif

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_Init), IntPtr.Zero);
                addresses.Add(typeof(Tcl_InitMemory), IntPtr.Zero);
                addresses.Add(typeof(Tcl_MakeSafe), IntPtr.Zero);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                addresses.Add(typeof(Tcl_RegisterObjType), IntPtr.Zero); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_GetObjType), IntPtr.Zero);
                addresses.Add(typeof(Tcl_AppendAllObjTypes), IntPtr.Zero);
                addresses.Add(typeof(Tcl_ConvertToType), IntPtr.Zero);
                addresses.Add(typeof(Tcl_CreateObjCommand), IntPtr.Zero);
                addresses.Add(typeof(Tcl_DeleteCommandFromToken), IntPtr.Zero);
                addresses.Add(typeof(Tcl_DeleteInterp), IntPtr.Zero);
                addresses.Add(typeof(Tcl_InterpDeleted), IntPtr.Zero);
                addresses.Add(typeof(Tcl_InterpActive), IntPtr.Zero); /* TIP #335 */
                addresses.Add(typeof(Tcl_GetErrorLine), IntPtr.Zero); /* TIP #336 */
                addresses.Add(typeof(Tcl_SetErrorLine), IntPtr.Zero); /* TIP #336 */
                addresses.Add(typeof(Tcl_NewObj), IntPtr.Zero);
                addresses.Add(typeof(Tcl_NewUnicodeObj), IntPtr.Zero);
                addresses.Add(typeof(Tcl_NewStringObj), IntPtr.Zero);
                addresses.Add(typeof(Tcl_NewByteArrayObj), IntPtr.Zero);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                addresses.Add(typeof(Tcl_DuplicateObj), IntPtr.Zero); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_DbIncrRefCount), IntPtr.Zero);
                addresses.Add(typeof(Tcl_DbDecrRefCount), IntPtr.Zero);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                addresses.Add(typeof(Tcl_DbIsShared), IntPtr.Zero); /* NOT USED */
                addresses.Add(typeof(Tcl_InvalidateStringRep), IntPtr.Zero); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_CommandComplete), IntPtr.Zero);
                addresses.Add(typeof(Tcl_AllowExceptions), IntPtr.Zero);
                addresses.Add(typeof(Tcl_EvalObjEx), IntPtr.Zero);
                addresses.Add(typeof(Tcl_EvalFile), IntPtr.Zero);
                addresses.Add(typeof(Tcl_RecordAndEvalObj), IntPtr.Zero);
                addresses.Add(typeof(Tcl_ExprObj), IntPtr.Zero);
                addresses.Add(typeof(Tcl_SubstObj), IntPtr.Zero);
                addresses.Add(typeof(Tcl_CancelEval), IntPtr.Zero);          /* TIP #285 */
                addresses.Add(typeof(Tcl_Canceled), IntPtr.Zero);            /* TIP #285 */
                addresses.Add(typeof(TclResetCancellation), IntPtr.Zero);    /* TIP #285 */
                addresses.Add(typeof(TclSetInterpCancelFlags), IntPtr.Zero); /* TIP #285 */
                addresses.Add(typeof(Tcl_DoOneEvent), IntPtr.Zero);
                addresses.Add(typeof(Tcl_ResetResult), IntPtr.Zero);
                addresses.Add(typeof(Tcl_GetObjResult), IntPtr.Zero);
                addresses.Add(typeof(Tcl_SetObjResult), IntPtr.Zero);
                addresses.Add(typeof(Tcl_GetUnicodeFromObj), IntPtr.Zero);
                addresses.Add(typeof(Tcl_GetStringFromObj), IntPtr.Zero);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                addresses.Add(typeof(Tcl_GetByteArrayFromObj), IntPtr.Zero); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_CreateExitHandler), IntPtr.Zero);
                addresses.Add(typeof(Tcl_DeleteExitHandler), IntPtr.Zero);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Threading
#if TCL_THREADS
                addresses.Add(typeof(Tcl_FinalizeThread), IntPtr.Zero);
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                addresses.Add(typeof(Tcl_Finalize), IntPtr.Zero);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetAddresses(
            NativeStubs nativeStubs,
            ref Result error
            )
        {
            addresses[typeof(Tcl_GetVersion)] = nativeStubs.getVersion;
            addresses[typeof(Tcl_FindExecutable)] = nativeStubs.findExecutable;
            addresses[typeof(Tcl_CreateInterp)] = nativeStubs.createInterp;
            addresses[typeof(Tcl_Preserve)] = nativeStubs.preserve;
            addresses[typeof(Tcl_Release)] = nativeStubs.release;
            addresses[typeof(Tcl_ObjGetVar2)] = nativeStubs.objGetVar2;
            addresses[typeof(Tcl_ObjSetVar2)] = nativeStubs.objSetVar2;
            addresses[typeof(Tcl_UnsetVar2)] = nativeStubs.unsetVar2;
            addresses[typeof(Tcl_Init)] = nativeStubs.init;
            addresses[typeof(Tcl_InitMemory)] = nativeStubs.initMemory;
            addresses[typeof(Tcl_MakeSafe)] = nativeStubs.makeSafe;
            addresses[typeof(Tcl_GetObjType)] = nativeStubs.getObjType;
            addresses[typeof(Tcl_AppendAllObjTypes)] = nativeStubs.appendAllObjTypes;
            addresses[typeof(Tcl_ConvertToType)] = nativeStubs.convertToType;
            addresses[typeof(Tcl_CreateObjCommand)] = nativeStubs.createObjCommand;
            addresses[typeof(Tcl_DeleteCommandFromToken)] = nativeStubs.deleteCommandFromToken;
            addresses[typeof(Tcl_DeleteInterp)] = nativeStubs.deleteInterp;
            addresses[typeof(Tcl_InterpDeleted)] = nativeStubs.interpDeleted;
            addresses[typeof(Tcl_InterpActive)] = nativeStubs.interpActive; /* TIP #335 */
            addresses[typeof(Tcl_GetErrorLine)] = nativeStubs.getErrorLine; /* TIP #336 */
            addresses[typeof(Tcl_SetErrorLine)] = nativeStubs.setErrorLine; /* TIP #336 */
            addresses[typeof(Tcl_NewObj)] = nativeStubs.newObj;
            addresses[typeof(Tcl_NewUnicodeObj)] = nativeStubs.newUnicodeObj;
            addresses[typeof(Tcl_NewStringObj)] = nativeStubs.newStringObj;
            addresses[typeof(Tcl_NewByteArrayObj)] = nativeStubs.newByteArrayObj;
            addresses[typeof(Tcl_DbIncrRefCount)] = nativeStubs.dbIncrRefCount;
            addresses[typeof(Tcl_DbDecrRefCount)] = nativeStubs.dbDecrRefCount;
            addresses[typeof(Tcl_CommandComplete)] = nativeStubs.commandComplete;
            addresses[typeof(Tcl_AllowExceptions)] = nativeStubs.allowExceptions;
            addresses[typeof(Tcl_EvalObjEx)] = nativeStubs.evalObjEx;
            addresses[typeof(Tcl_EvalFile)] = nativeStubs.evalFile;
            addresses[typeof(Tcl_RecordAndEvalObj)] = nativeStubs.recordAndEvalObj;
            addresses[typeof(Tcl_ExprObj)] = nativeStubs.exprObj;
            addresses[typeof(Tcl_SubstObj)] = nativeStubs.substObj;
            addresses[typeof(Tcl_CancelEval)] = nativeStubs.cancelEval;                    /* TIP #285 */
            addresses[typeof(Tcl_Canceled)] = nativeStubs.canceled;                        /* TIP #285 */
            addresses[typeof(TclResetCancellation)] = nativeStubs.resetCancellation;       /* TIP #285 */
            addresses[typeof(TclSetInterpCancelFlags)] = nativeStubs.setInterpCancelFlags; /* TIP #285 */
            addresses[typeof(Tcl_DoOneEvent)] = nativeStubs.doOneEvent;
            addresses[typeof(Tcl_ResetResult)] = nativeStubs.resetResult;
            addresses[typeof(Tcl_GetObjResult)] = nativeStubs.getObjResult;
            addresses[typeof(Tcl_SetObjResult)] = nativeStubs.setObjResult;
            addresses[typeof(Tcl_GetUnicodeFromObj)] = nativeStubs.getUnicodeFromObj;
            addresses[typeof(Tcl_GetStringFromObj)] = nativeStubs.getStringFromObj;
            addresses[typeof(Tcl_CreateExitHandler)] = nativeStubs.createExitHandler;
            addresses[typeof(Tcl_DeleteExitHandler)] = nativeStubs.deleteExitHandler;

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Threading
#if TCL_THREADS
            addresses[typeof(Tcl_FinalizeThread)] = nativeStubs.finalizeThread;
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            addresses[typeof(Tcl_Finalize)] = nativeStubs.finalize;

            ///////////////////////////////////////////////////////////////////////////////////////////

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Delegate Management Members
        private void InitializeDelegates(
            bool clear,
            bool stubs
            )
        {
            lock (syncRoot)
            {
                if (delegates == null)
                    delegates = new TypeDelegateDictionary();
                else if (clear)
                    delegates.Clear();

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_GetVersion), null);
                delegates.Add(typeof(Tcl_FindExecutable), null);

                ///////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
                delegates.Add(typeof(TclKit_SetKitPath), null);
#endif

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_CreateInterp), null);
                delegates.Add(typeof(Tcl_Preserve), null);
                delegates.Add(typeof(Tcl_Release), null);
                delegates.Add(typeof(Tcl_ObjGetVar2), null);
                delegates.Add(typeof(Tcl_ObjSetVar2), null);
                delegates.Add(typeof(Tcl_UnsetVar2), null);

                ///////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
                delegates.Add(typeof(TclKit_AppInit), null);
#endif

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_Init), null);
                delegates.Add(typeof(Tcl_InitMemory), null);
                delegates.Add(typeof(Tcl_MakeSafe), null);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                delegates.Add(typeof(Tcl_RegisterObjType), null); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_GetObjType), null);
                delegates.Add(typeof(Tcl_AppendAllObjTypes), null);
                delegates.Add(typeof(Tcl_ConvertToType), null);
                delegates.Add(typeof(Tcl_CreateObjCommand), null);
                delegates.Add(typeof(Tcl_DeleteCommandFromToken), null);
                delegates.Add(typeof(Tcl_DeleteInterp), null);
                delegates.Add(typeof(Tcl_InterpDeleted), null);
                delegates.Add(typeof(Tcl_InterpActive), null); /* TIP #335 */
                delegates.Add(typeof(Tcl_GetErrorLine), null); /* TIP #336 */
                delegates.Add(typeof(Tcl_SetErrorLine), null); /* TIP #336 */
                delegates.Add(typeof(Tcl_NewObj), null);
                delegates.Add(typeof(Tcl_NewUnicodeObj), null);
                delegates.Add(typeof(Tcl_NewStringObj), null);
                delegates.Add(typeof(Tcl_NewByteArrayObj), null);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                delegates.Add(typeof(Tcl_DuplicateObj), null); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_DbIncrRefCount), null);
                delegates.Add(typeof(Tcl_DbDecrRefCount), null);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                delegates.Add(typeof(Tcl_DbIsShared), null);          /* NOT USED */
                delegates.Add(typeof(Tcl_InvalidateStringRep), null); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_CommandComplete), null);
                delegates.Add(typeof(Tcl_AllowExceptions), null);
                delegates.Add(typeof(Tcl_EvalObjEx), null);
                delegates.Add(typeof(Tcl_EvalFile), null);
                delegates.Add(typeof(Tcl_RecordAndEvalObj), null);
                delegates.Add(typeof(Tcl_ExprObj), null);
                delegates.Add(typeof(Tcl_SubstObj), null);
                delegates.Add(typeof(Tcl_CancelEval), null);          /* TIP #285 */
                delegates.Add(typeof(Tcl_Canceled), null);            /* TIP #285 */
                delegates.Add(typeof(TclResetCancellation), null);    /* TIP #285 */
                delegates.Add(typeof(TclSetInterpCancelFlags), null); /* TIP #285 */
                delegates.Add(typeof(Tcl_DoOneEvent), null);
                delegates.Add(typeof(Tcl_ResetResult), null);
                delegates.Add(typeof(Tcl_GetObjResult), null);
                delegates.Add(typeof(Tcl_SetObjResult), null);
                delegates.Add(typeof(Tcl_GetUnicodeFromObj), null);
                delegates.Add(typeof(Tcl_GetStringFromObj), null);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                delegates.Add(typeof(Tcl_GetByteArrayFromObj), null); /* NOT USED */
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_CreateExitHandler), null);
                delegates.Add(typeof(Tcl_DeleteExitHandler), null);

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Threading
#if TCL_THREADS
                delegates.Add(typeof(Tcl_FinalizeThread), null);
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                delegates.Add(typeof(Tcl_Finalize), null);

                ///////////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: Which of the above delegates are purely optional
                //       (i.e. the rest are absolutely required)?
                //
                if (optional == null)
                    optional = new TypeBoolDictionary();
                else if (clear)
                    optional.Clear();

                ///////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
                optional.Add(typeof(TclKit_SetKitPath), true); /* OPTIONAL: stardll */
                optional.Add(typeof(TclKit_AppInit), true);    /* OPTIONAL: stardll */
#endif

                ///////////////////////////////////////////////////////////////////////////////////////

                optional.Add(typeof(Tcl_CancelEval), true);          /* OPTIONAL: TIP #285 */
                optional.Add(typeof(Tcl_Canceled), true);            /* OPTIONAL: TIP #285 */
                optional.Add(typeof(TclResetCancellation), true);    /* OPTIONAL: TIP #285 */
                optional.Add(typeof(TclSetInterpCancelFlags), true); /* OPTIONAL: TIP #285 */
                optional.Add(typeof(Tcl_InterpActive), true);        /* OPTIONAL: TIP #335 */
                optional.Add(typeof(Tcl_GetErrorLine), true);        /* OPTIONAL: TIP #336 */
                optional.Add(typeof(Tcl_SetErrorLine), true);        /* OPTIONAL: TIP #336 */

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                if (stubs)
                {
                    optional.Add(typeof(Tcl_RegisterObjType), true);     /* UNAVAILABLE WITH STUBS */
                    optional.Add(typeof(Tcl_DuplicateObj), true);        /* UNAVAILABLE WITH STUBS */
                    optional.Add(typeof(Tcl_DbIsShared), true);          /* UNAVAILABLE WITH STUBS */
                    optional.Add(typeof(Tcl_InvalidateStringRep), true); /* UNAVAILABLE WITH STUBS */
                    optional.Add(typeof(Tcl_GetByteArrayFromObj), true); /* UNAVAILABLE WITH STUBS */
                }
#endif
                #endregion
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetDelegates(
            ref Result error
            )
        {
            try
            {
                lock (syncRoot)
                {
                    if (stubs != IntPtr.Zero)
                    {
                        object nativeStubs = NativeStubsFromIntPtr(
                            stubs, ref error);

                        if (nativeStubs == null)
                            return false;

                        if (!CheckSizeOfNativeStubs(
                                (NativeStubs)nativeStubs, ref error))
                        {
                            return false;
                        }

                        if (!SetAddresses(
                                (NativeStubs)nativeStubs, ref error))
                            return false;

                        if (RuntimeOps.SetNativeDelegates(
                                "Tcl API", addresses, delegates, optional,
                                ref error) == ReturnCode.Ok)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (RuntimeOps.SetNativeDelegates(
                                "Tcl API", module, delegates, optional,
                                ref error) == ReturnCode.Ok)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Exit Handler Management Members
        public ReturnCode ClearExitHandler(
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot)
            {
                //
                // NOTE: There are some cases where we do not need to delete
                //       the Tcl exit handler (i.e. it was already done as
                //       part of the Tcl_Finalize/Tcl_Exit processing);
                //       therefore, we simply clear it out here so that the
                //       disposal code does not try to bother with it.
                //
                exitProc = null;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode SetExitHandler(
            ref Result error
            )
        {
            CheckDisposed();

            ReturnCode code;

            try
            {
                lock (syncRoot)
                {
                    //
                    // NOTE: Lock this object in memory until we are disposed.
                    //
                    handle = GCHandle.Alloc(this, GCHandleType.Normal); /* throw */

                    //
                    // NOTE: Hold on to this delegate to prevent exceptions from
                    //       being thrown when it magically "goes away".
                    //
                    exitProc = new Tcl_ExitProc(ExitProc);

                    //
                    // NOTE: Inform Tcl of our exit handler.  We must now be very
                    //       careful about keeping this object around until we
                    //       unhook it later.
                    //
                    code = TclWrapper.CreateExitHandler(
                        this, exitProc, GCHandle.ToIntPtr(handle), ref error);
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode UnsetExitHandler(
            ref Result error
            )
        {
            CheckDisposed();

            ReturnCode code;

            try
            {
                lock (syncRoot)
                {
                    bool haveExitProc = (exitProc != null);

                    //
                    // NOTE: Unhook our exit handler from Tcl, if necessary.
                    //
                    code = haveExitProc ?
                        TclWrapper.DeleteExitHandler(
                            this, exitProc, GCHandle.ToIntPtr(handle), ref error) :
                        ReturnCode.Ok;

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: If necessary, release the GCHandle that is
                        //       keeping this object alive.
                        //
                        if (handle.IsAllocated)
                            handle.Free();

                        //
                        // NOTE: Finally, we should be able to safely remove
                        //       our reference to the Tcl callback delegate
                        //       at this point because we already deleted the
                        //       Tcl exit handler related to it.
                        //
                        if (haveExitProc)
                            exitProc = null;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Package Integration Members
        private static bool CheckNativePackageInterp(
            IntPtr interp,
            ref long threadId
            )
        {
#if NATIVE_PACKAGE
            return NativePackage.FindTclInterpreterThreadId(
                interp, ref threadId);
#else
            return false;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Module Helper Members
        public static void Initialize()
        {
            //
            // HACK: This should normally be okay for 8.4, 8.5, and 8.6; however,
            //       it is not 100% reliable because it makes various assumptions
            //       about the internal (private) layout of the Interp structure.
            //
            INTERP_NUMLEVELS_OFFSET = (PTRS_BEFORE_NUMLEVELS * IntPtr.Size) +
                (INTS_BEFORE_NUMLEVELS * sizeof(int));

            //
            // HACK: Account for structure packing since the errorLine field will
            //       probably be padded to the native word size.  This is not 100%
            //       reliable because it makes assumptions about the structure
            //       packing that was in use when the Tcl library was compiled.
            //
            INTERP_NUMLEVELS_OFFSET += (INTERP_NUMLEVELS_OFFSET % IntPtr.Size);

            //
            // NOTE: This member resides in the public Tcl_Interp struct;
            //       therefore, it should be 100% reliable.
            //
            INTERP_ERRORLINE_OFFSET = PTRS_BEFORE_ERRORLINE * IntPtr.Size;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Any caller of this method overload should report negative
        //         return values to the user.
        //
        public static bool CheckModule(
            ITclApi tclApi
            )
        {
            Result error = null;

            return CheckModule(tclApi, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CheckModule(
            ITclApi tclApi,
            ref Result error
            )
        {
            if (tclApi != null)
            {
                //
                // NOTE: Wrap this lock statement in a try because the origin
                //       of this Tcl API object is unknown (via TclWrapper).
                //
                try
                {
                    IntPtr module;

                    lock (tclApi.SyncRoot)
                    {
                        module = tclApi.Module;
                    }

                    if (module != IntPtr.Zero)
                        return true;
                    else
                        error = "invalid Tcl API module";
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid Tcl API object";
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Stubs Helper Methods
        private static bool CheckSizeOfNativeStubs(
            NativeStubs nativeStubs,
            ref Result error
            )
        {
            int marshalSizeOf = Marshal.SizeOf(nativeStubs);

            TraceOps.DebugTrace(String.Format(
                "CheckNativeStubs: marshalSizeOf = {0}, expectedSizeOf = {1}",
                marshalSizeOf, SizeOfNativeStubs), typeof(TclApi).Name,
                TracePriority.NativeDebug);

            if (marshalSizeOf < SizeOfNativeStubs)
            {
                error = String.Format(
                    "marshal {0} size mismatch, have {1}, need at least {2}",
                    typeof(NativeStubs).Name, marshalSizeOf, SizeOfNativeStubs);

                TraceOps.DebugTrace(String.Format(
                    "CheckNativeStubs: result = {0}, error = {1}",
                    false, FormatOps.WrapOrNull(true, true, error)),
                    typeof(TclApi).Name, TracePriority.NativeDebug);

                return false;
            }

            int structSizeOf = ConversionOps.ToInt(nativeStubs.sizeOf);

            TraceOps.DebugTrace(String.Format(
                "CheckNativeStubs: structSizeOf = {0}, expectedSizeOf = {1}",
                structSizeOf, SizeOfNativeStubs), typeof(TclApi).Name,
                TracePriority.NativeDebug);

            if (structSizeOf < SizeOfNativeStubs)
            {
                error = String.Format(
                    "internal {0} size mismatch, have {1}, need at least {2}",
                    typeof(NativeStubs).Name, structSizeOf, SizeOfNativeStubs);

                TraceOps.DebugTrace(String.Format(
                    "CheckNativeStubs: result = {0}, error = {1}",
                    false, FormatOps.WrapOrNull(true, true, error)),
                    typeof(TclApi).Name, TracePriority.NativeError);

                return false;
            }

            TraceOps.DebugTrace(String.Format(
                "CheckNativeStubs: result = {0}, error = {1}",
                true, FormatOps.WrapOrNull(true, true, error)),
                typeof(TclApi).Name, TracePriority.NativeDebug);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Interpreter Helper Members
        //
        // HACK: Only used when interfacing with Tcl 8.5 or earlier.
        //       No longer necessary in Tcl 8.6 due to TIP #335.
        //
#if TCL_WRAPPER
        internal
#else
        public
#endif
        static int GetNumLevels(
            ITclApi tclApi,
            IntPtr interp
            )
        {
            int result = 0;

            try
            {
                if (CheckModule(tclApi) && tclApi.CheckInterp(interp))
                    result = Marshal.ReadInt32(interp, INTERP_NUMLEVELS_OFFSET);
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Only used when interfacing with Tcl 8.5 or earlier.
        //       No longer necessary in Tcl 8.6 due to TIP #336.
        //
#if TCL_WRAPPER
        internal
#else
        public
#endif
        static int _GetErrorLine(
            ITclApi tclApi,
            IntPtr interp
            )
        {
            int result = 0;

            try
            {
                if (CheckModule(tclApi) && tclApi.CheckInterp(interp))
                    result = Marshal.ReadInt32(interp, INTERP_ERRORLINE_OFFSET);
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Only used when interfacing with Tcl 8.5 or earlier.
        //       No longer necessary in Tcl 8.6 due to TIP #336.
        //
#if TCL_WRAPPER
        internal
#else
        public
#endif
        static ReturnCode _SetErrorLine(
            ITclApi tclApi,
            IntPtr interp,
            int line,
            ref Result error
            )
        {
            try
            {
                if (CheckModule(tclApi, ref error))
                {
                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        //
                        // NOTE: This may look risky; however, since this offset is part
                        //       of the public Tcl API (tcl.h), it should be relatively
                        //       safe.
                        //
                        Marshal.WriteInt32(interp, INTERP_ERRORLINE_OFFSET, line);

                        return ReturnCode.Ok;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static ITclApi Helper Members
#if TCL_WRAPPER
        internal
#else
        public
#endif
        static ITclApi GetTclApi(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: Do not use the TclSyncRoot property here (in case
                //         the interpreter has been disposed), use the field
                //         instead because this method is indirectly called
                //         from the GC thread.
                //
                lock (interpreter.InternalTclSyncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Technically, accessing this method should require
                    //       using the SyncRoot property; however, since we are
                    //       only reading one boolean field, it should be safe.
                    //
                    if (!interpreter.Disposed)
                        return interpreter.InternalTclApi;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static void SetTclApi(
            Interpreter interpreter,
            ITclApi tclApi
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: Do not use the TclSyncRoot property here (in case
                //         the interpreter has been disposed), use the field
                //         instead because this method is indirectly called
                //         from the GC thread.
                //
                lock (interpreter.InternalTclSyncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Technically, accessing this method should require
                    //       using the SyncRoot property; however, since we are
                    //       only reading one boolean field, it should be safe.
                    //
                    if (!interpreter.Disposed)
                        interpreter.InternalTclApi = tclApi;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Native Package Helper Members
#if NATIVE_PACKAGE
#if TCL_WRAPPER
        internal
#else
        public
#endif
        static bool HasInterp(
            Interpreter interpreter,
            string pattern,
            IntPtr interp
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.HasTclInterpreter(pattern, interp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static void AddInterp(
            Interpreter interpreter,
            string interpName,
            IntPtr interp
            )
        {
            if (interpreter != null)
                interpreter.AddTclInterpreter(interpName, interp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static int RemoveInterp(
            Interpreter interpreter,
            IntPtr interp
            )
        {
            if (interpreter == null)
                return 0;

            return interpreter.RemoveTclInterpreter(interp);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return interpreter;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        private object syncRoot;
        public object SyncRoot
        {
            get
            {
                CheckDisposed();

                return syncRoot;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TryLock(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TryLock(
            int timeout,
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void ExitLock(
            ref bool locked
            )
        {
            if (RuntimeOps.ShouldCheckDisposedOnExitLock(locked)) /* EXEMPT */
                CheckDisposed();

            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ITclApi Members
        private TclBuild build;
        public TclBuild Build
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return build;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private IntPtr stubs;
        public IntPtr Stubs
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return stubs;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private LoadFlags loadFlags;
        public LoadFlags LoadFlags
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return loadFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private UnloadFlags unloadFlags;
        public UnloadFlags UnloadFlags
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return unloadFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool exceptions;
        public bool Exceptions
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return exceptions;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    exceptions = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringPairList ToList(
            bool all
            )
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            lock (syncRoot)
            {
                if (all)
                {
                    list.Add("build", (build != null) ?
                        build.ToString() : _String.Null);
                }

                list.Add("interpreter", (interpreter != null) ?
                    interpreter.InternalToString() : _String.Null);

                list.Add("fileName", (fileName != null) ?
                    fileName : _String.Null);

                list.Add("module", module.ToString());
                list.Add("stubs", stubs.ToString());

                list.Add("handle", handle.IsAllocated ?
                    GCHandle.ToIntPtr(handle).ToString() : _String.Null);

                list.Add("loadFlags", loadFlags.ToString());
                list.Add("unloadFlags", unloadFlags.ToString());
                list.Add("exceptions", exceptions.ToString());

                string methodName = FormatOps.DelegateMethodName(
                    exitProc, false, false);

                list.Add("exitProc", (methodName != null) ?
                    methodName : _String.Null);
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode Copy(
            ref ITclApi tclApi,
            ref Result error
            ) /* DEEP COPY */
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;
            IntPtr module = IntPtr.Zero;

            try
            {
                lock (syncRoot)
                {
                    module = TclWrapper.AddModuleReference(
                        fileName, true, FlagOps.HasFlags(
                        loadFlags, LoadFlags.SetDllDirectory,
                        true), ref error); /* throw */

                    if (NativeOps.IsValidHandle(module))
                    {
                        tclApi = Create(
                            interpreter, build, fileName,
                            module, stubs, loadFlags,
                            ref error);

                        if (tclApi == null)
                            code = ReturnCode.Error;
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if ((code != ReturnCode.Ok) &&
                    NativeOps.IsValidHandle(module))
                {
                    int lastError;

                    if (NativeOps.FreeLibrary(
                            module, out lastError)) /* throw */
                    {
                        TraceOps.DebugTrace(String.Format(
                            "FreeLibrary (Copy): success, module = {0}",
                            module), typeof(TclApi).Name,
                            TracePriority.NativeDebug);

                        module = IntPtr.Zero;
                    }
                    else
                    {
                        throw new ScriptException(String.Format(
                            "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                            lastError, module, NativeOps.GetDynamicLoadingError(
                            lastError)));
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Any caller of this method overload should report negative
        //         return values to the user.
        //
        public bool CheckInterp(
            IntPtr interp
            )
        {
            CheckDisposed();

            Result error = null;

            return CheckInterp(interp, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool CheckInterp(
            IntPtr interp,
            ref Result error
            )
        {
            CheckDisposed();

            if (interp != IntPtr.Zero)
            {
                Interpreter interpreter;

                lock (syncRoot)
                {
                    interpreter = this.interpreter;
                }

                if (interpreter != null)
                {
                    long currentThreadId = GlobalState.GetCurrentNativeThreadId();
                    long threadId = 0;

                    if (interpreter.FindTclInterpreterThreadId(
                            interp, ref threadId) ||
                        CheckNativePackageInterp(interp, ref threadId))
                    {
                        //
                        // NOTE: For now, this is all we can validate.
                        //
                        if (threadId == currentThreadId)
                            return true;
                    }

                    error = String.Format(
                        "wrong thread for Tcl interpreter 0x{0:X}, current " +
                        "thread {1} does not match Tcl interpreter thread {2}",
                        interp, currentThreadId, threadId);
                }
                else
                {
                    error = "invalid interpreter";
                }
            }
            else
            {
                error = "invalid Tcl interpreter";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Any caller of this method overload should report negative
        //         return values to the user.
        //
        public bool CheckObjPtr(
            IntPtr objPtr
            )
        {
            CheckDisposed();

            Result error = null;

            return CheckObjPtr(objPtr, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !TCL_THREADS
        private long GetThreadIdForObjPtr(
            IntPtr objPtr
            )
        {
            //
            // TODO: Figure out how to lookup the associated Tcl interpreter
            //       and then use that to find the native thread identifier.
            //
            lock (syncRoot)
            {
                return (interpreter != null) ?
                    interpreter.GetTclThreadId() : 0;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool CheckObjPtr(
            IntPtr objPtr,
            ref Result error
            )
        {
            CheckDisposed();

            if (objPtr != IntPtr.Zero)
            {
#if !TCL_THREADS
                //
                // HACK: Technically, this check is not correct.  However, we know that
                //       Eagle does not, by default, create Tcl interpreters on a thread
                //       that is not the primary thread for the interpreter; therefore,
                //       this will work for now.
                //
                long currentThreadId = GlobalState.GetCurrentNativeThreadId();
                long threadId = GetThreadIdForObjPtr(objPtr);

                if (threadId == currentThreadId)
                {
                    //
                    // NOTE: For now, this is all we can validate.
                    //
                    return true;
                }

                error = String.Format(
                    "wrong thread for Tcl object 0x{0:X}, current " +
                    "thread {1} does not match object thread {2}",
                    objPtr, currentThreadId, threadId);
#else
                //
                // NOTE: We can no longer validate anything about the nature of the Tcl object
                //       being passed to us because we create isolated Tcl threads and the Tcl
                //       objects do not really have any direct association with their parent
                //       Tcl interpreter.
                //
                return true;
#endif
            }
            else
            {
                error = "invalid Tcl object";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_GetVersion GetVersion
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_GetVersion)delegates[typeof(Tcl_GetVersion)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_FindExecutable FindExecutable
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_FindExecutable)delegates[typeof(Tcl_FindExecutable)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
        public TclKit_SetKitPath Kit_SetKitPath
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (TclKit_SetKitPath)delegates[typeof(TclKit_SetKitPath)] : null;
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_CreateInterp CreateInterp
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_CreateInterp)delegates[typeof(Tcl_CreateInterp)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
        public TclKit_AppInit Kit_AppInit
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (TclKit_AppInit)delegates[typeof(TclKit_AppInit)] : null;
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_Preserve Preserve
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_Preserve)delegates[typeof(Tcl_Preserve)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_Release Release
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_Release)delegates[typeof(Tcl_Release)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_ObjGetVar2 ObjGetVar2
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_ObjGetVar2)delegates[typeof(Tcl_ObjGetVar2)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_ObjSetVar2 ObjSetVar2
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_ObjSetVar2)delegates[typeof(Tcl_ObjSetVar2)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_UnsetVar2 UnsetVar2
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_UnsetVar2)delegates[typeof(Tcl_UnsetVar2)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_Init Init
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_Init)delegates[typeof(Tcl_Init)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_InitMemory InitMemory
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_InitMemory)delegates[typeof(Tcl_InitMemory)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_MakeSafe MakeSafe
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_MakeSafe)delegates[typeof(Tcl_MakeSafe)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public Tcl_RegisterObjType RegisterObjType /* NOT USED */
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_RegisterObjType)delegates[typeof(Tcl_RegisterObjType)] : null;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_GetObjType GetObjType
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_GetObjType)delegates[typeof(Tcl_GetObjType)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_AppendAllObjTypes AppendAllObjTypes
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_AppendAllObjTypes)delegates[typeof(Tcl_AppendAllObjTypes)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_ConvertToType ConvertToType
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_ConvertToType)delegates[typeof(Tcl_ConvertToType)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_CreateObjCommand CreateObjCommand
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_CreateObjCommand)delegates[typeof(Tcl_CreateObjCommand)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_DeleteCommandFromToken DeleteCommandFromToken
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DeleteCommandFromToken)delegates[typeof(Tcl_DeleteCommandFromToken)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_DeleteInterp DeleteInterp
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DeleteInterp)delegates[typeof(Tcl_DeleteInterp)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_InterpDeleted InterpDeleted
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_InterpDeleted)delegates[typeof(Tcl_InterpDeleted)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_InterpActive InterpActive
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_InterpActive)delegates[typeof(Tcl_InterpActive)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_GetErrorLine GetErrorLine
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_GetErrorLine)delegates[typeof(Tcl_GetErrorLine)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_SetErrorLine SetErrorLine
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_SetErrorLine)delegates[typeof(Tcl_SetErrorLine)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_NewObj NewObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_NewObj)delegates[typeof(Tcl_NewObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_NewUnicodeObj NewUnicodeObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_NewUnicodeObj)delegates[typeof(Tcl_NewUnicodeObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_NewStringObj NewStringObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_NewStringObj)delegates[typeof(Tcl_NewStringObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_NewByteArrayObj NewByteArrayObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_NewByteArrayObj)delegates[typeof(Tcl_NewByteArrayObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public Tcl_DuplicateObj DuplicateObj /* NOT USED */
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DuplicateObj)delegates[typeof(Tcl_DuplicateObj)] : null;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_DbIncrRefCount DbIncrRefCount
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DbIncrRefCount)delegates[typeof(Tcl_DbIncrRefCount)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_DbDecrRefCount DbDecrRefCount
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DbDecrRefCount)delegates[typeof(Tcl_DbDecrRefCount)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public Tcl_DbIsShared DbIsShared /* NOT USED */
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DbIsShared)delegates[typeof(Tcl_DbIsShared)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_InvalidateStringRep InvalidateStringRep /* NOT USED */
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_InvalidateStringRep)delegates[typeof(Tcl_InvalidateStringRep)] : null;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_CommandComplete CommandComplete
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_CommandComplete)delegates[typeof(Tcl_CommandComplete)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_AllowExceptions AllowExceptions
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_AllowExceptions)delegates[typeof(Tcl_AllowExceptions)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_EvalObjEx EvalObjEx
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_EvalObjEx)delegates[typeof(Tcl_EvalObjEx)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_EvalFile EvalFile
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_EvalFile)delegates[typeof(Tcl_EvalFile)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_RecordAndEvalObj RecordAndEvalObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_RecordAndEvalObj)delegates[typeof(Tcl_RecordAndEvalObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_ExprObj ExprObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_ExprObj)delegates[typeof(Tcl_ExprObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_SubstObj SubstObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_SubstObj)delegates[typeof(Tcl_SubstObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_CancelEval CancelEval
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_CancelEval)delegates[typeof(Tcl_CancelEval)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_Canceled Canceled
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_Canceled)delegates[typeof(Tcl_Canceled)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public TclResetCancellation ResetCancellation
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (TclResetCancellation)delegates[typeof(TclResetCancellation)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public TclSetInterpCancelFlags SetInterpCancelFlags
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (TclSetInterpCancelFlags)delegates[typeof(TclSetInterpCancelFlags)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_DoOneEvent DoOneEvent
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DoOneEvent)delegates[typeof(Tcl_DoOneEvent)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_ResetResult ResetResult
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_ResetResult)delegates[typeof(Tcl_ResetResult)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_GetObjResult GetObjResult
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_GetObjResult)delegates[typeof(Tcl_GetObjResult)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_SetObjResult SetObjResult
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_SetObjResult)delegates[typeof(Tcl_SetObjResult)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_GetUnicodeFromObj GetUnicodeFromObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_GetUnicodeFromObj)delegates[typeof(Tcl_GetUnicodeFromObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_GetStringFromObj GetStringFromObj
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_GetStringFromObj)delegates[typeof(Tcl_GetStringFromObj)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public Tcl_GetByteArrayFromObj GetByteArrayFromObj /* NOT USED */
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_GetByteArrayFromObj)delegates[typeof(Tcl_GetByteArrayFromObj)] : null;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_CreateExitHandler CreateExitHandler
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_CreateExitHandler)delegates[typeof(Tcl_CreateExitHandler)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Tcl_DeleteExitHandler DeleteExitHandler
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_DeleteExitHandler)delegates[typeof(Tcl_DeleteExitHandler)] : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_THREADS
        public Tcl_FinalizeThread FinalizeThread
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_FinalizeThread)delegates[typeof(Tcl_FinalizeThread)] : null;
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Without the leading underscore this name will clash with the destructor.
        //
        public Tcl_Finalize _Finalize
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return (delegates != null) ?
                        (Tcl_Finalize)delegates[typeof(Tcl_Finalize)] : null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl Exit Handler Callbacks
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // **** WARNING ***** BEGIN CODE DIRECTLY CALLED BY THE NATIVE TCL RUNTIME ***** WARNING **** /
        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // -- ExitProc --
        //
        // The clientData parameter to proc is a copy of the clientData argument
        // given to Tcl_CreateExitHandler or Tcl_CreateThreadExitHandler when the
        // callback was created.  Typically, clientData points to a data structure
        // containing application-specific information about what to do in proc.
        //
        private static void ExitProc(
            IntPtr clientData
            )
        {
            try
            {
                //
                // NOTE: Rehydrate the handle from the clientData that Tcl just
                //       passed us.
                //
                GCHandle handle = GCHandle.FromIntPtr(clientData); /* throw */

                //
                // NOTE: Make sure the handle has a valid target.
                //
                if (handle.IsAllocated && (handle.Target != null))
                {
                    //
                    // NOTE: Attempt to cast the handle to a TclApi object; if
                    //       this fails, we cannot continue to handle this call.
                    //
                    ITclApi tclApi = handle.Target as ITclApi;

                    if (tclApi != null)
                    {
                        //
                        // NOTE: Grab the associated interpreter from the TclApi
                        //       object.
                        //
                        Interpreter interpreter = tclApi.Interpreter;

                        //
                        // NOTE: We are being called from inside Tcl_Finalize;
                        //       therefore, we must skip it.  However, we may
                        //       still need to free the actual library handle.
                        //
                        UnloadFlags unloadFlags = tclApi.UnloadFlags;

                        if (interpreter != null)
                        {
                            //
                            // NOTE: Since there is a valid interpreter context,
                            //       consult its Tcl unload flags as well.  At
                            //       the moment, this is primarily designed for
                            //       use by the test suite.
                            //
                            unloadFlags |= interpreter.TclExitUnloadFlags;

                            //
                            // NOTE: Attempt to dispose the Tcl related objects
                            //       from the interpreter if this is the primary
                            //       Tcl thread.  If for any reason we cannot do
                            //       this, there is a problem.
                            //
                            if (interpreter.IsTclThread())
                            {
                                ReturnCode disposeCode;
                                Result disposeError = null;

                                //
                                // NOTE: If necessary, attempt to notify our
                                //       parent interpreter that Tcl has been
                                //       unloaded.
                                //
                                disposeCode = interpreter.DisposeTcl(
                                    false, false, true, ref disposeError);

                                if (disposeCode != ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: If we failed somehow, complain
                                    //       loudly (there is not much else
                                    //       we can do at this point).
                                    //
                                    DebugOps.Complain(
                                        interpreter, disposeCode, disposeError);
                                }
                            }
                        }

                        //
                        // NOTE: Since the Tcl primary interpreter should
                        //       already be deleted now, just pass an invalid
                        //       Tcl interpreter handle to Unload, which will
                        //       cause it to skip attempting to delete the Tcl
                        //       primary interpreter.
                        //
                        ReturnCode unloadCode;
                        Result unloadError = null;

                        unloadCode = TclWrapper.Unload(
                            interpreter, unloadFlags, ref tclApi,
                            ref unloadError);

                        if (unloadCode == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Release the GCHandle that is keeping this
                            //       object alive.  This is necessary because
                            //       we are being called by Tcl and our
                            //       UnsetExitHandler method will never be
                            //       called.
                            //
                            if (handle.IsAllocated)
                                handle.Free();
                        }
                        else
                        {
                            //
                            // NOTE: If we failed somehow, complain loudly
                            //       (there is not much else we can do at
                            //       this point).
                            //
                            DebugOps.Complain(
                                interpreter, unloadCode, unloadError);
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "invalid Tcl API object",
                            typeof(Tcl_ExitProc).Name,
                            TracePriority.MarshalError);
                    }
                }
                else
                {
                    TraceOps.DebugTrace(
                        "invalid GC handle",
                        typeof(Tcl_ExitProc).Name,
                        TracePriority.MarshalError);
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_ExitProc).Name,
                    TracePriority.NativeError);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ***** WARNING ***** END CODE DIRECTLY CALLED BY THE NATIVE TCL RUNTIME ***** WARNING ***** /
        ///////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return ToList(false).ToString();
        }
        #endregion
    }
}
