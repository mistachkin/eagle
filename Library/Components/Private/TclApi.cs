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
    /// <summary>
    /// This class wraps the native Tcl C API for use by Eagle's optional Tcl
    /// integration.  It holds the loaded Tcl library module, the function
    /// pointer delegates for each supported native Tcl routine, and the
    /// associated bookkeeping needed to load, call into, and cleanly unload
    /// Tcl from a managed interpreter.
    /// </summary>
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
        /// <summary>
        /// The required size, in bytes, of the native NativeStubs structure
        /// provided by the native Garuda code.
        /// </summary>
        private static readonly int SizeOfNativeStubs = 49 * IntPtr.Size;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Native Stubs Structure
        //
        // WARNING: The size and layout of this structure MUST match the
        //          native "ClrTclStubs" structure defined in the Garuda
        //          source code file "GarudaInt.h" exactly.
        //
        /// <summary>
        /// This structure mirrors the native "ClrTclStubs" structure defined
        /// in the Garuda source code; its size and field layout MUST match that
        /// native structure exactly.  Each field holds the native function
        /// pointer for one supported Tcl API routine.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [ObjectId("7895bd71-e1dc-4fe6-ba57-78bc7a7c1e42")]
        internal struct NativeStubs
        {
            /// <summary>
            /// The size of this structure, in bytes.
            /// </summary>
            public UIntPtr sizeOf; /* The size of this structure, in bytes. */
            /// <summary>
            /// The native function pointer for the Tcl_GetVersion routine.
            /// </summary>
            public IntPtr getVersion;
            /// <summary>
            /// The native function pointer for the Tcl_FindExecutable routine.
            /// </summary>
            public IntPtr findExecutable;
            /// <summary>
            /// The native function pointer for the Tcl_CreateInterp routine.
            /// </summary>
            public IntPtr createInterp;
            /// <summary>
            /// The native function pointer for the Tcl_Preserve routine.
            /// </summary>
            public IntPtr preserve;
            /// <summary>
            /// The native function pointer for the Tcl_Release routine.
            /// </summary>
            public IntPtr release;
            /// <summary>
            /// The native function pointer for the Tcl_ObjGetVar2 routine.
            /// </summary>
            public IntPtr objGetVar2;
            /// <summary>
            /// The native function pointer for the Tcl_ObjSetVar2 routine.
            /// </summary>
            public IntPtr objSetVar2;
            /// <summary>
            /// The native function pointer for the Tcl_UnsetVar2 routine.
            /// </summary>
            public IntPtr unsetVar2;
            /// <summary>
            /// The native function pointer for the Tcl_Init routine.
            /// </summary>
            public IntPtr init;
            /// <summary>
            /// The native function pointer for the Tcl_InitMemory routine.
            /// </summary>
            public IntPtr initMemory;
            /// <summary>
            /// The native function pointer for the Tcl_MakeSafe routine.
            /// </summary>
            public IntPtr makeSafe;
            /// <summary>
            /// The native function pointer for the Tcl_GetObjType routine.
            /// </summary>
            public IntPtr getObjType;
            /// <summary>
            /// The native function pointer for the Tcl_AppendAllObjTypes
            /// routine.
            /// </summary>
            public IntPtr appendAllObjTypes;
            /// <summary>
            /// The native function pointer for the Tcl_ConvertToType routine.
            /// </summary>
            public IntPtr convertToType;
            /// <summary>
            /// The native function pointer for the Tcl_CreateObjCommand
            /// routine.
            /// </summary>
            public IntPtr createObjCommand;
            /// <summary>
            /// The native function pointer for the Tcl_DeleteCommandFromToken
            /// routine.
            /// </summary>
            public IntPtr deleteCommandFromToken;
            /// <summary>
            /// The native function pointer for the Tcl_DeleteInterp routine.
            /// </summary>
            public IntPtr deleteInterp;
            /// <summary>
            /// The native function pointer for the Tcl_InterpDeleted routine.
            /// </summary>
            public IntPtr interpDeleted;
            /// <summary>
            /// The native function pointer for the Tcl_InterpActive routine.
            /// </summary>
            public IntPtr interpActive;
            /// <summary>
            /// The native function pointer for the Tcl_GetErrorLine routine.
            /// </summary>
            public IntPtr getErrorLine;
            /// <summary>
            /// The native function pointer for the Tcl_SetErrorLine routine.
            /// </summary>
            public IntPtr setErrorLine;
            /// <summary>
            /// The native function pointer for the Tcl_NewObj routine.
            /// </summary>
            public IntPtr newObj;
            /// <summary>
            /// The native function pointer for the Tcl_NewUnicodeObj routine.
            /// </summary>
            public IntPtr newUnicodeObj;
            /// <summary>
            /// The native function pointer for the Tcl_NewStringObj routine.
            /// </summary>
            public IntPtr newStringObj;
            /// <summary>
            /// The native function pointer for the Tcl_NewByteArrayObj routine.
            /// </summary>
            public IntPtr newByteArrayObj;
            /// <summary>
            /// The native function pointer for the Tcl_DbIncrRefCount routine.
            /// </summary>
            public IntPtr dbIncrRefCount;
            /// <summary>
            /// The native function pointer for the Tcl_DbDecrRefCount routine.
            /// </summary>
            public IntPtr dbDecrRefCount;
            /// <summary>
            /// The native function pointer for the Tcl_CommandComplete routine.
            /// </summary>
            public IntPtr commandComplete;
            /// <summary>
            /// The native function pointer for the Tcl_AllowExceptions routine.
            /// </summary>
            public IntPtr allowExceptions;
            /// <summary>
            /// The native function pointer for the Tcl_EvalObjEx routine.
            /// </summary>
            public IntPtr evalObjEx;
            /// <summary>
            /// The native function pointer for the Tcl_EvalFile routine.
            /// </summary>
            public IntPtr evalFile;
            /// <summary>
            /// The native function pointer for the Tcl_RecordAndEvalObj
            /// routine.
            /// </summary>
            public IntPtr recordAndEvalObj;
            /// <summary>
            /// The native function pointer for the Tcl_ExprObj routine.
            /// </summary>
            public IntPtr exprObj;
            /// <summary>
            /// The native function pointer for the Tcl_SubstObj routine.
            /// </summary>
            public IntPtr substObj;
            /// <summary>
            /// The native function pointer for the Tcl_CancelEval routine.
            /// </summary>
            public IntPtr cancelEval;
            /// <summary>
            /// The native function pointer for the Tcl_Canceled routine.
            /// </summary>
            public IntPtr canceled;
            /// <summary>
            /// The native function pointer for the TclResetCancellation
            /// routine.
            /// </summary>
            public IntPtr resetCancellation;
            /// <summary>
            /// The native function pointer for the TclSetInterpCancelFlags
            /// routine.
            /// </summary>
            public IntPtr setInterpCancelFlags;
            /// <summary>
            /// The native function pointer for the Tcl_DoOneEvent routine.
            /// </summary>
            public IntPtr doOneEvent;
            /// <summary>
            /// The native function pointer for the Tcl_ResetResult routine.
            /// </summary>
            public IntPtr resetResult;
            /// <summary>
            /// The native function pointer for the Tcl_GetObjResult routine.
            /// </summary>
            public IntPtr getObjResult;
            /// <summary>
            /// The native function pointer for the Tcl_SetObjResult routine.
            /// </summary>
            public IntPtr setObjResult;
            /// <summary>
            /// The native function pointer for the Tcl_GetUnicodeFromObj
            /// routine.
            /// </summary>
            public IntPtr getUnicodeFromObj;
            /// <summary>
            /// The native function pointer for the Tcl_GetStringFromObj
            /// routine.
            /// </summary>
            public IntPtr getStringFromObj;
            /// <summary>
            /// The native function pointer for the Tcl_CreateExitHandler
            /// routine.
            /// </summary>
            public IntPtr createExitHandler;
            /// <summary>
            /// The native function pointer for the Tcl_DeleteExitHandler
            /// routine.
            /// </summary>
            public IntPtr deleteExitHandler;
            /// <summary>
            /// The native function pointer for the Tcl_FinalizeThread routine.
            /// </summary>
            public IntPtr finalizeThread;
            /// <summary>
            /// The native function pointer for the Tcl_Finalize routine.
            /// </summary>
            public IntPtr finalize;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constants
        //
        // WARNING: Do not change these as they must be UTF-8 encodings.
        //
        /// <summary>
        /// The encoding used when converting strings received from the native
        /// Tcl library into managed strings.  This must be a UTF-8 encoding.
        /// </summary>
        public static readonly Encoding FromEncoding = TclEncoding.Tcl;
        /// <summary>
        /// The encoding used when converting managed strings into the form
        /// passed to the native Tcl library.  This must be a UTF-8 encoding.
        /// </summary>
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
        /// <summary>
        /// The default value, used to initialize each instance, controlling
        /// whether Tcl_AllowExceptions should be called prior to evaluating
        /// scripts.  This may be overridden on a per-call basis using the
        /// method overloads that include the exceptions argument.
        /// </summary>
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
        /// <summary>
        /// The number of pointer-size fields that precede the errorLine field
        /// in the Tcl Interp structure for Tcl 8.4, 8.5, and 8.6.
        /// </summary>
        private const int PTRS_BEFORE_ERRORLINE = 2;

        //
        // HACK: There are 15 pointer size fields and 7 integer size fields in the
        //       Interp structure prior to the numLevels field in Tcl 8.4, 8.5, and
        //       8.6; however, this is not 100% reliable because it makes various
        //       assumptions about the internal (private) layout of the Interp
        //       structure.
        //
        /// <summary>
        /// The number of pointer-size fields that precede the numLevels field
        /// in the Tcl Interp structure for Tcl 8.4, 8.5, and 8.6.
        /// </summary>
        private const int PTRS_BEFORE_NUMLEVELS = 15;
        /// <summary>
        /// The number of integer-size fields that precede the numLevels field
        /// in the Tcl Interp structure for Tcl 8.4, 8.5, and 8.6.
        /// </summary>
        private const int INTS_BEFORE_NUMLEVELS = 7;

        //
        // NOTE: The offset into the Tcl_Interp structure for the errorLine member,
        //       in bytes.
        //
        /// <summary>
        /// The offset, in bytes, of the errorLine member within the Tcl_Interp
        /// structure.
        /// </summary>
        private static int INTERP_ERRORLINE_OFFSET;

        //
        // NOTE: The offset into the Tcl_Interp structure for the numLevels member,
        //       in bytes.
        //
        /// <summary>
        /// The offset, in bytes, of the numLevels member within the Tcl_Interp
        /// structure.
        /// </summary>
        internal static int INTERP_NUMLEVELS_OFFSET;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The GC handle used to pin this object in memory while it is hooked
        /// to Tcl via a native exit handler.
        /// </summary>
        private GCHandle handle; /* TclApi */
        /// <summary>
        /// The managed delegate registered as the Tcl exit handler for this
        /// object, or null if no exit handler is currently installed.
        /// </summary>
        private Tcl_ExitProc exitProc;
        /// <summary>
        /// The mapping from native Tcl delegate type to the native function
        /// pointer (address) for the associated Tcl API routine.
        /// </summary>
        private TypeIntPtrDictionary addresses;
        /// <summary>
        /// The mapping from native Tcl delegate type to the managed delegate
        /// instance used to call into the associated Tcl API routine.
        /// </summary>
        private TypeDelegateDictionary delegates;
        /// <summary>
        /// The mapping that indicates which native Tcl delegates are purely
        /// optional (i.e. the remaining ones are absolutely required).
        /// </summary>
        private TypeBoolDictionary optional;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        /// <summary>
        /// Initializes the static state for this class, computing the offsets
        /// into the Tcl_Interp structure that are used by this class.
        /// </summary>
        static TclApi()
        {
            Initialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class that wraps a loaded native
        /// Tcl library for use by the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that now owns this object.
        /// </param>
        /// <param name="build">
        /// The Tcl build instance corresponding to the loaded version of Tcl.
        /// </param>
        /// <param name="fileName">
        /// The file name associated with the loaded native Tcl module.
        /// </param>
        /// <param name="module">
        /// The native module handle for the loaded Tcl library.  This handle is
        /// not owned by this object because it may be shared by multiple
        /// interpreters.
        /// </param>
        /// <param name="stubs">
        /// The pointer to the native stubs structure, or zero when stubs are
        /// not in use.  This pointer is not owned by this object because it may
        /// be shared by multiple interpreters.
        /// </param>
        /// <param name="loadFlags">
        /// The flags that were used to load the native Tcl library.
        /// </param>
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
        /// <summary>
        /// Finalizes this object, releasing any unmanaged resources that it
        /// still holds.
        /// </summary>
        ~TclApi() /* throw */
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// When non-zero, this object is currently in the process of being
        /// disposed; this is used to prevent re-entrancy.
        /// </summary>
        private bool disposing;
        /// <summary>
        /// When non-zero, this object has been disposed and should no longer be
        /// used.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// Throws an exception if this object has been disposed and the
        /// interpreter is configured to throw on access to disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(TclApi).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of this object, unhooking the Tcl exit handler if necessary
        /// and releasing the resources that it holds.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the public
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
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
        /// <summary>
        /// Disposes of this object, releasing all resources that it holds and
        /// suppressing finalization.
        /// </summary>
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// Creates a deep copy of this object, adding a new reference to the
        /// underlying native Tcl module.
        /// </summary>
        /// <returns>
        /// The newly created copy of this object, or null if the copy could not
        /// be created.
        /// </returns>
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
        /// <summary>
        /// Creates a new instance of this class that wraps a loaded native Tcl
        /// library, resolving its file name and native delegates.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that will own the new object.
        /// </param>
        /// <param name="build">
        /// The Tcl build instance corresponding to the loaded version of Tcl.
        /// </param>
        /// <param name="fileName">
        /// The file name associated with the loaded native Tcl module.
        /// </param>
        /// <param name="module">
        /// The native module handle for the loaded Tcl library.
        /// </param>
        /// <param name="stubs">
        /// The pointer to the native stubs structure, or zero when stubs are
        /// not in use.
        /// </param>
        /// <param name="loadFlags">
        /// The flags that were used to load the native Tcl library.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created object, or null if it could not be created.
        /// </returns>
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
        /// <summary>
        /// Resolves and stores the file name associated with the loaded native
        /// Tcl module, querying the module handle when no file name has yet been
        /// provided.
        /// </summary>
        /// <param name="fileName">
        /// The file name associated with the loaded native Tcl module, if
        /// already known.
        /// </param>
        /// <param name="module">
        /// The native module handle to query for its file name when one is not
        /// already known.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the file name is known (or was successfully resolved);
        /// otherwise, false.
        /// </returns>
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
                            module, typeof(Tcl_CreateInterp).Name, ref error);

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
        /// <summary>
        /// Marshals the native stubs structure from the specified unmanaged
        /// pointer into a managed NativeStubs value.
        /// </summary>
        /// <param name="stubs">
        /// The pointer to the native stubs structure.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The marshaled NativeStubs value boxed as an object, or null on
        /// failure.
        /// </returns>
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

        /// <summary>
        /// Populates the dictionary of native function pointer addresses with
        /// an entry for each supported Tcl API delegate type, initially set to
        /// zero.
        /// </summary>
        /// <param name="clear">
        /// Non-zero to clear any existing entries before populating the
        /// dictionary.
        /// </param>
        /// <param name="stubs">
        /// Non-zero if the native stubs mechanism is in use.  This parameter is
        /// not used.
        /// </param>
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

        /// <summary>
        /// Copies the native function pointer addresses from the specified
        /// native stubs structure into the dictionary of addresses.
        /// </summary>
        /// <param name="nativeStubs">
        /// The native stubs structure containing the function pointer addresses
        /// to copy.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the addresses were successfully copied; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Populates the dictionary of native Tcl delegates with an entry for
        /// each supported Tcl API delegate type, initially set to null, and
        /// records which of those delegates are purely optional.
        /// </summary>
        /// <param name="clear">
        /// Non-zero to clear any existing entries before populating the
        /// dictionaries.
        /// </param>
        /// <param name="stubs">
        /// Non-zero if the native stubs mechanism is in use, which affects which
        /// delegates are considered optional.
        /// </param>
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

        /// <summary>
        /// Resolves and stores the managed delegates for the supported Tcl API
        /// routines, using either the native stubs structure or the loaded
        /// module handle.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the delegates were successfully resolved; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Clears the record of the Tcl exit handler without attempting to
        /// delete it from Tcl.  This is used in cases where the exit handler has
        /// already been removed as part of Tcl_Finalize or Tcl_Exit processing.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// Installs the Tcl exit handler for this object, pinning this object in
        /// memory and informing Tcl of the native callback delegate.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// Removes the Tcl exit handler for this object, unhooking the native
        /// callback delegate from Tcl, freeing the GC handle that pins this
        /// object in memory, and clearing the callback delegate reference.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// Attempts to find the native thread identifier associated with the
        /// specified native Tcl interpreter via the native package subsystem.
        /// </summary>
        /// <param name="interp">
        /// The native Tcl interpreter to look up.
        /// </param>
        /// <param name="threadId">
        /// Upon success, this parameter will be modified to contain the native
        /// thread identifier associated with the interpreter.
        /// </param>
        /// <returns>
        /// True if the interpreter was found and its thread identifier was
        /// determined; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Computes and stores the byte offsets into the Tcl_Interp structure
        /// for the numLevels and errorLine members, accounting for native
        /// structure packing.
        /// </summary>
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
        /// <summary>
        /// Checks whether the specified Tcl API object has a valid native
        /// module handle.
        /// </summary>
        /// <param name="tclApi">
        /// The Tcl API object to check.
        /// </param>
        /// <returns>
        /// True if the Tcl API object has a valid native module handle;
        /// otherwise, false.
        /// </returns>
        public static bool CheckModule(
            ITclApi tclApi
            )
        {
            Result error = null;

            return CheckModule(tclApi, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Checks whether the specified Tcl API object has a valid native
        /// module handle.
        /// </summary>
        /// <param name="tclApi">
        /// The Tcl API object to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the Tcl API object has a valid native module handle;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Validates that the specified native stubs structure is at least the
        /// expected size, both as marshaled and as reported by its own size
        /// field.
        /// </summary>
        /// <param name="nativeStubs">
        /// The native stubs structure to validate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the structure is at least the expected size; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// Reads the numLevels member from the native Tcl interpreter
        /// structure.  This is only used when interfacing with Tcl 8.5 or
        /// earlier.
        /// </summary>
        /// <param name="tclApi">
        /// The Tcl API object associated with the native interpreter.
        /// </param>
        /// <param name="interp">
        /// The native Tcl interpreter to read from.
        /// </param>
        /// <returns>
        /// The value of the numLevels member, or zero on failure.
        /// </returns>
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
        /// <summary>
        /// Reads the errorLine member from the native Tcl interpreter
        /// structure.  This is only used when interfacing with Tcl 8.5 or
        /// earlier.
        /// </summary>
        /// <param name="tclApi">
        /// The Tcl API object associated with the native interpreter.
        /// </param>
        /// <param name="interp">
        /// The native Tcl interpreter to read from.
        /// </param>
        /// <returns>
        /// The value of the errorLine member, or zero on failure.
        /// </returns>
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
        /// <summary>
        /// Writes a new value into the errorLine member of the native Tcl
        /// interpreter structure.  This is only used when interfacing with Tcl
        /// 8.5 or earlier.
        /// </summary>
        /// <param name="tclApi">
        /// The Tcl API object associated with the native interpreter.
        /// </param>
        /// <param name="interp">
        /// The native Tcl interpreter to write to.
        /// </param>
        /// <param name="line">
        /// The new error line value to write.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// Gets the Tcl API object associated with the specified interpreter, if
        /// any.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose Tcl API object is to be returned.
        /// </param>
        /// <returns>
        /// The Tcl API object associated with the interpreter, or null if there
        /// is none (or the interpreter has been disposed).
        /// </returns>
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

        /// <summary>
        /// Sets the Tcl API object associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose Tcl API object is to be set.
        /// </param>
        /// <param name="tclApi">
        /// The Tcl API object to associate with the interpreter.
        /// </param>
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
        /// <summary>
        /// Determines whether the specified interpreter is tracking a native
        /// Tcl interpreter that matches the given criteria.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match against the names of the tracked native
        /// Tcl interpreters.
        /// </param>
        /// <param name="interp">
        /// The native Tcl interpreter to match against the tracked
        /// interpreters.
        /// </param>
        /// <returns>
        /// True if a matching native Tcl interpreter is being tracked;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Adds a native Tcl interpreter to the set of interpreters tracked by
        /// the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that should track the native Tcl interpreter.
        /// </param>
        /// <param name="interpName">
        /// The name to associate with the native Tcl interpreter.
        /// </param>
        /// <param name="interp">
        /// The native Tcl interpreter to add.
        /// </param>
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

        /// <summary>
        /// Removes a native Tcl interpreter from the set of interpreters tracked
        /// by the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that is tracking the native Tcl interpreter.
        /// </param>
        /// <param name="interp">
        /// The native Tcl interpreter to remove.
        /// </param>
        /// <returns>
        /// The number of tracked native Tcl interpreters that were removed.
        /// </returns>
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
        /// <summary>
        /// The interpreter that owns this object.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that owns this object.
        /// </summary>
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

        #region ISynchronizeBase Members
        /// <summary>
        /// The object used to synchronize access to this instance.
        /// </summary>
        private object syncRoot;
        /// <summary>
        /// Gets the object used to synchronize access to this instance.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                CheckDisposed();

                return syncRoot;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        /// <summary>
        /// Attempts to acquire the synchronization lock for this instance
        /// without waiting.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be modified to contain non-zero if
        /// the lock was acquired; otherwise, zero.
        /// </param>
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

        /// <summary>
        /// Attempts to acquire the synchronization lock for this instance,
        /// waiting up to the configured wait-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be modified to contain non-zero if
        /// the lock was acquired; otherwise, zero.
        /// </param>
        public void TryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(
                syncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the synchronization lock for this instance
        /// without waiting and without checking whether this object has been
        /// disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be modified to contain non-zero if
        /// the lock was acquired; otherwise, zero.
        /// </param>
        public void TryLockNoThrow(
            ref bool locked
            )
        {
            // CheckDisposed(); /* EXEMPT */

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the synchronization lock for this instance,
        /// waiting up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the lock, in milliseconds.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be modified to contain non-zero if
        /// the lock was acquired; otherwise, zero.
        /// </param>
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

        /// <summary>
        /// Releases the synchronization lock for this instance, if it is held.
        /// </summary>
        /// <param name="locked">
        /// On input, non-zero if the lock is currently held by the caller.  Upon
        /// return, this parameter will be modified to contain zero if the lock
        /// was released.
        /// </param>
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
        /// <summary>
        /// The Tcl build instance corresponding to the loaded version of Tcl.
        /// </summary>
        private TclBuild build;
        /// <summary>
        /// Gets the Tcl build instance corresponding to the loaded version of
        /// Tcl.
        /// </summary>
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

        /// <summary>
        /// The file name associated with the loaded native Tcl module.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets the file name associated with the loaded native Tcl module.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The native module handle for the loaded Tcl library.  This handle is
        /// not owned by this object.
        /// </summary>
        private IntPtr module;
        /// <summary>
        /// Gets the native module handle for the loaded Tcl library.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The pointer to the native stubs structure, or zero when stubs are
        /// not in use.  This pointer is not owned by this object.
        /// </summary>
        private IntPtr stubs;
        /// <summary>
        /// Gets the pointer to the native stubs structure, or zero when stubs
        /// are not in use.
        /// </summary>
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

        /// <summary>
        /// The flags that were used to load the native Tcl library.
        /// </summary>
        private LoadFlags loadFlags;
        /// <summary>
        /// Gets the flags that were used to load the native Tcl library.
        /// </summary>
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

        /// <summary>
        /// The flags that will be used when unloading the native Tcl library.
        /// </summary>
        private UnloadFlags unloadFlags;
        /// <summary>
        /// Gets the flags that will be used when unloading the native Tcl
        /// library.
        /// </summary>
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

        /// <summary>
        /// When non-zero, Tcl_AllowExceptions is called prior to evaluating
        /// scripts.
        /// </summary>
        private bool exceptions;
        /// <summary>
        /// Gets or sets a value indicating whether Tcl_AllowExceptions is
        /// called prior to evaluating scripts.
        /// </summary>
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

        /// <summary>
        /// Builds a list of name/value pairs describing the current state of
        /// this object, suitable for diagnostic display.
        /// </summary>
        /// <param name="all">
        /// Non-zero to include all available details, such as the Tcl build
        /// information.
        /// </param>
        /// <returns>
        /// The list of name/value pairs describing this object.
        /// </returns>
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
                    interpreter.IdNoThrow.ToString() : _String.Null);

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

        /// <summary>
        /// Creates a deep copy of this object, adding a new reference to the
        /// underlying native Tcl module.
        /// </summary>
        /// <param name="tclApi">
        /// Upon success, this parameter will be modified to contain the newly
        /// created copy of this object.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// Checks whether the specified native Tcl interpreter is valid for use
        /// from the current thread.
        /// </summary>
        /// <param name="interp">
        /// The native Tcl interpreter to check.
        /// </param>
        /// <returns>
        /// True if the interpreter is valid for use from the current thread;
        /// otherwise, false.
        /// </returns>
        public bool CheckInterp(
            IntPtr interp
            )
        {
            CheckDisposed();

            Result error = null;

            return CheckInterp(interp, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Checks whether the specified native Tcl interpreter is valid for use
        /// from the current thread.
        /// </summary>
        /// <param name="interp">
        /// The native Tcl interpreter to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the interpreter is valid for use from the current thread;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Checks whether the specified native Tcl object is valid for use from
        /// the current thread.
        /// </summary>
        /// <param name="objPtr">
        /// The native Tcl object to check.
        /// </param>
        /// <returns>
        /// True if the object is valid for use from the current thread;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Gets the native thread identifier associated with the specified
        /// native Tcl object, via its owning interpreter.
        /// </summary>
        /// <param name="objPtr">
        /// The native Tcl object whose associated thread identifier is to be
        /// returned.
        /// </param>
        /// <returns>
        /// The native thread identifier associated with the object, or zero if
        /// it could not be determined.
        /// </returns>
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

        /// <summary>
        /// Checks whether the specified native Tcl object is valid for use from
        /// the current thread.
        /// </summary>
        /// <param name="objPtr">
        /// The native Tcl object to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the object is valid for use from the current thread;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_GetVersion routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_FindExecutable routine,
        /// or null if it is not available.
        /// </summary>
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
        /// <summary>
        /// Gets the managed delegate for the native TclKit_SetKitPath routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_CreateInterp routine,
        /// or null if it is not available.
        /// </summary>
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
        /// <summary>
        /// Gets the managed delegate for the native TclKit_AppInit routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_Preserve routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_Release routine, or null
        /// if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_ObjGetVar2 routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_ObjSetVar2 routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_UnsetVar2 routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_Init routine, or null if
        /// it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_InitMemory routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_MakeSafe routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_GetObjType routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_AppendAllObjTypes
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_ConvertToType routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_CreateObjCommand
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_DeleteCommandFromToken
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_DeleteInterp routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_InterpDeleted routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_InterpActive routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_GetErrorLine routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_SetErrorLine routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_NewObj routine, or null
        /// if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_NewUnicodeObj routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_NewStringObj routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_NewByteArrayObj routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_DbIncrRefCount routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_DbDecrRefCount routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_CommandComplete routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_AllowExceptions routine,
        /// or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_EvalObjEx routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_EvalFile routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_RecordAndEvalObj
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_ExprObj routine, or null
        /// if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_SubstObj routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_CancelEval routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_Canceled routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native TclResetCancellation
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native TclSetInterpCancelFlags
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_DoOneEvent routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_ResetResult routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_GetObjResult routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_SetObjResult routine, or
        /// null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_GetUnicodeFromObj
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_GetStringFromObj
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_CreateExitHandler
        /// routine, or null if it is not available.
        /// </summary>
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

        /// <summary>
        /// Gets the managed delegate for the native Tcl_DeleteExitHandler
        /// routine, or null if it is not available.
        /// </summary>
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
        /// <summary>
        /// Gets the managed delegate for the native Tcl_FinalizeThread routine,
        /// or null if it is not available.
        /// </summary>
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
        /// <summary>
        /// Gets the managed delegate for the native Tcl_Finalize routine, or
        /// null if it is not available.
        /// </summary>
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
        /// <summary>
        /// The callback invoked directly by the native Tcl runtime when Tcl is
        /// being finalized.  It unloads the associated Tcl wrapper and releases
        /// the GC handle that pins the corresponding object in memory.
        /// </summary>
        /// <param name="clientData">
        /// The client data supplied to Tcl_CreateExitHandler when the callback
        /// was installed; this is the GC handle that refers to the associated
        /// Tcl API object.
        /// </param>
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
        /// <summary>
        /// Returns a string representation of this object, based on its current
        /// state.
        /// </summary>
        /// <returns>
        /// The string representation of this object.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToList(false).ToString();
        }
        #endregion
    }
}
