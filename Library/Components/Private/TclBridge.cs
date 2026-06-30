/*
 * TclBridge.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Tcl.Delegates;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private.Tcl;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private.Tcl
{
    /// <summary>
    /// This class bridges an Eagle command (i.e. an <see cref="IExecute" />)
    /// into a native Tcl interpreter by creating a Tcl command that, when
    /// evaluated, marshals its arguments into Eagle, executes the associated
    /// Eagle command, and marshals the result back to Tcl.  It also manages the
    /// lifetime of the created Tcl command, deleting it (and removing itself
    /// from the containing interpreter) when the Eagle command or the Tcl
    /// command goes away.
    /// </summary>
    [ObjectId("51e232aa-edb5-4dd1-b223-010e6450c339")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBridge /* It's how Tcl/Tk is done. */ : IDisposable
    {
        #region Private Data
        /// <summary>
        /// The garbage collector handle that keeps this object pinned in memory
        /// (i.e. alive) for as long as the native Tcl runtime may call back into
        /// it.
        /// </summary>
        private GCHandle handle; /* TclBridge */

        /// <summary>
        /// The Eagle interpreter that owns this bridge and its associated Eagle
        /// command.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// The Eagle command that is invoked when the bridged Tcl command is
        /// evaluated.
        /// </summary>
        private IExecute execute;

        /// <summary>
        /// Optional, opaque, caller-defined data passed to the Eagle command
        /// when it is executed.  May be null.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// The native pointer to the Tcl interpreter that contains the bridged
        /// Tcl command.
        /// </summary>
        private IntPtr interp;

        /// <summary>
        /// The current nesting level for active calls into the
        /// <see cref="ObjCmdProc" /> callback.
        /// </summary>
        private int objCmdProcLevels; // NOTE: Nesting level for ObjCmdProc.

        /// <summary>
        /// The delegate, held to prevent garbage collection, that the native
        /// Tcl runtime invokes when the bridged command is evaluated.
        /// </summary>
        private Tcl_ObjCmdProc objCmdProc;

        /// <summary>
        /// The delegate, held to prevent garbage collection, that the native
        /// Tcl runtime invokes when the bridged command is deleted.
        /// </summary>
        private Tcl_CmdDeleteProc cmdDeleteProc;

        /// <summary>
        /// The native pointer to the token that identifies the created Tcl
        /// command, used later to delete it.
        /// </summary>
        private IntPtr token;

        /// <summary>
        /// Non-zero if the bridged Tcl command belongs to an isolated Tcl
        /// thread.
        /// </summary>
        private bool fromThread;

        /// <summary>
        /// Non-zero if the Tcl command should be forcibly deleted during
        /// disposal, even when it is actively being used.
        /// </summary>
        private bool forceDelete;

        /// <summary>
        /// Non-zero if errors encountered while deleting the Tcl command during
        /// disposal should be ignored.
        /// </summary>
        private bool noComplain;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class, capturing the Eagle and Tcl
        /// state needed to dispatch and clean up the bridged command, and pins
        /// the new object in memory so the native Tcl runtime can safely call
        /// back into it.
        /// </summary>
        /// <param name="interpreter">
        /// The Eagle interpreter that owns this bridge.
        /// </param>
        /// <param name="execute">
        /// The Eagle command to invoke when the bridged Tcl command is
        /// evaluated.
        /// </param>
        /// <param name="clientData">
        /// Optional, opaque, caller-defined data passed to the Eagle command.
        /// May be null.
        /// </param>
        /// <param name="interp">
        /// The native pointer to the Tcl interpreter that contains the bridged
        /// command.
        /// </param>
        /// <param name="objCmdProc">
        /// The delegate the native Tcl runtime invokes when the command is
        /// evaluated.
        /// </param>
        /// <param name="cmdDeleteProc">
        /// The delegate the native Tcl runtime invokes when the command is
        /// deleted.
        /// </param>
        /// <param name="token">
        /// The native pointer to the token that identifies the created Tcl
        /// command.
        /// </param>
        /// <param name="fromThread">
        /// Non-zero if the bridged Tcl command belongs to an isolated Tcl
        /// thread.
        /// </param>
        /// <param name="forceDelete">
        /// Non-zero if the Tcl command should be forcibly deleted during
        /// disposal.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero if errors encountered while deleting the Tcl command during
        /// disposal should be ignored.
        /// </param>
        private TclBridge(
            Interpreter interpreter,
            IExecute execute,
            IClientData clientData,
            IntPtr interp,
            Tcl_ObjCmdProc objCmdProc,
            Tcl_CmdDeleteProc cmdDeleteProc,
            IntPtr token,
            bool fromThread,
            bool forceDelete,
            bool noComplain
            )
        {
            //
            // NOTE: Lock this object in memory until we are disposed.
            //
            handle = GCHandle.Alloc(this, GCHandleType.Normal); /* throw */

            //
            // NOTE: This will be used to keep track of the nesting levels for
            //       the number of calls active to our Tcl_ObjCmdProc callback.
            //
            objCmdProcLevels = 0;

            //
            // NOTE: Setup the information we need to make callbacks.
            //
            this.interpreter = interpreter;
            this.execute = execute;
            this.clientData = clientData;

            //
            // NOTE: We need the Tcl interpreter later on (during cleanup) as well.
            //
            this.interp = interp;

            //
            // NOTE: Hold on to these delegates to prevent exceptions from
            //       being thrown when they magically "go away".
            //
            this.objCmdProc = objCmdProc;
            this.cmdDeleteProc = cmdDeleteProc;

            //
            // NOTE: Keep track of the created Tcl command token.
            //
            this.token = token;

            //
            // NOTE: Does this Tcl command belong to an isolated Tcl thread?
            //
            this.fromThread = fromThread;

            //
            // NOTE: Do they want to forcibly delete the Tcl command during dispose?
            //
            this.forceDelete = forceDelete;

            //
            // NOTE: Do they want to ignore errors from deleting the Tcl command
            //       during dispose?
            //
            this.noComplain = noComplain;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether this bridge is associated with the
        /// specified Eagle command.
        /// </summary>
        /// <param name="execute">
        /// The Eagle command to compare against the one associated with this
        /// bridge.
        /// </param>
        /// <returns>
        /// True if the specified Eagle command is the one associated with this
        /// bridge; otherwise, false.
        /// </returns>
        public bool Match(
            IExecute execute
            )
        {
            CheckDisposed();

            return InternalMatch(execute);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this bridge is associated with the
        /// specified Tcl interpreter.
        /// </summary>
        /// <param name="interp">
        /// The native pointer to the Tcl interpreter to compare against the one
        /// associated with this bridge.
        /// </param>
        /// <returns>
        /// True if the specified Tcl interpreter is the one associated with this
        /// bridge; otherwise, false.
        /// </returns>
        public bool Match(
            IntPtr interp
            )
        {
            CheckDisposed();

            return InternalMatch(interp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this bridge has the specified
        /// isolated-thread association.
        /// </summary>
        /// <param name="fromThread">
        /// The isolated-thread flag to compare against the one associated with
        /// this bridge.  May be null.
        /// </param>
        /// <returns>
        /// True if the specified flag matches the one associated with this
        /// bridge; otherwise, false.
        /// </returns>
        public bool Match(
            bool? fromThread
            )
        {
            CheckDisposed();

            return InternalMatch(fromThread);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method determines whether the specified Eagle command is the
        /// one associated with this bridge.
        /// </summary>
        /// <param name="execute">
        /// The Eagle command to compare against the one associated with this
        /// bridge.
        /// </param>
        /// <returns>
        /// True if the specified Eagle command is the one associated with this
        /// bridge; otherwise, false.
        /// </returns>
#if TCL_WRAPPER
        internal
#else
        public
#endif
        bool InternalMatch(
            IExecute execute
            )
        {
            return Object.ReferenceEquals(execute, this.execute);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified Tcl interpreter is the
        /// one associated with this bridge.
        /// </summary>
        /// <param name="interp">
        /// The native pointer to the Tcl interpreter to compare against the one
        /// associated with this bridge.
        /// </param>
        /// <returns>
        /// True if the specified Tcl interpreter is the one associated with this
        /// bridge; otherwise, false.
        /// </returns>
#if TCL_WRAPPER
        internal
#else
        public
#endif
        bool InternalMatch(
            IntPtr interp
            )
        {
            return interp == this.interp;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified isolated-thread flag
        /// matches the one associated with this bridge.
        /// </summary>
        /// <param name="fromThread">
        /// The isolated-thread flag to compare against the one associated with
        /// this bridge.  May be null.
        /// </param>
        /// <returns>
        /// True if the specified flag matches the one associated with this
        /// bridge; otherwise, false.
        /// </returns>
#if TCL_WRAPPER
        internal
#else
        public
#endif
        bool InternalMatch(
            bool? fromThread
            )
        {
            return fromThread == this.fromThread;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this object, which is
        /// the name of the associated Eagle command.
        /// </summary>
        /// <returns>
        /// The name of the associated Eagle command, or an empty string if it
        /// has no name.
        /// </returns>
        public override string ToString()
        {
            // CheckDisposed(); /* EXEMPT: During disposal. */

            string result = EntityOps.GetNameNoThrow(execute);

            return (result != null) ? result : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        /// <summary>
        /// This method creates a bridge that associates a named Tcl command
        /// with the specified Eagle command, registering the command with the
        /// Tcl interpreter.  The bridge handles marshalling of command arguments
        /// and results as well as the lifetime of both the Tcl and Eagle
        /// commands.
        /// </summary>
        /// <param name="interpreter">
        /// The Eagle interpreter that will own the bridge.
        /// </param>
        /// <param name="execute">
        /// The Eagle command to invoke when the bridged Tcl command is
        /// evaluated.
        /// </param>
        /// <param name="clientData">
        /// Optional, opaque, caller-defined data passed to the Eagle command.
        /// May be null.
        /// </param>
        /// <param name="interp">
        /// The native pointer to the Tcl interpreter in which to create the
        /// command.
        /// </param>
        /// <param name="name">
        /// The name of the Tcl command to be created.  An empty name is allowed.
        /// </param>
        /// <param name="fromThread">
        /// Non-zero if the bridged Tcl command belongs to an isolated Tcl
        /// thread.
        /// </param>
        /// <param name="forceDelete">
        /// Non-zero if the Tcl command should be forcibly deleted during
        /// disposal.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero if errors encountered while deleting the Tcl command during
        /// disposal should be ignored.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the bridge could not be created.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
        public static TclBridge Create(
            Interpreter interpreter,
            IExecute execute,
            IClientData clientData,
            IntPtr interp,
            string name,
            bool fromThread,
            bool forceDelete,
            bool noComplain,
            ref Result error
            )
        {
            //
            // NOTE: Create and return a TclBridge object that creates and
            //       associates a named Tcl command with the specified Eagle
            //       command.
            //
            //       The marshalling of the command arguments and the result
            //       will be handled by this class (via the ObjCmdProc wrapper).
            //
            //       Tcl command lifetime management will also be handled by
            //       this class (via the CmdDeleteProc).
            //
            //       Eagle command lifetime management will also be handled by
            //       this class.  The Tcl command will be deleted if the Eagle
            //       command is deleted.
            //
            if (interpreter != null)
            {
                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                if (TclApi.CheckModule(tclApi, ref error))
                {
                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (execute != null)
                        {
                            //
                            // NOTE: *WARNING* Empty Tcl command/procedure names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                //
                                // NOTE: Create a TclBridge object to handle the command
                                //       callbacks from Tcl.
                                //
                                ReturnCode code = ReturnCode.Ok;
                                TclBridge result = null;

                                try
                                {
                                    result = new TclBridge(
                                        interpreter,
                                        execute,
                                        clientData,
                                        interp,
                                        new Tcl_ObjCmdProc(ObjCmdProc),
                                        new Tcl_CmdDeleteProc(CmdDeleteProc),
                                        IntPtr.Zero,
                                        fromThread,
                                        forceDelete,
                                        noComplain);

                                    //
                                    // NOTE: Create the Tcl command that calls into the ObjCmdProc
                                    //       callback TclBridge dispatcher methods and save the
                                    //       created Tcl command token for later deletion.
                                    //
                                    code = TclWrapper.CreateCommand(
                                        tclApi,
                                        interp,
                                        name,
                                        result.objCmdProc,
                                        GCHandle.ToIntPtr(result.handle),
                                        result.cmdDeleteProc,
                                        ref result.token,
                                        ref error);

                                    if (code == ReturnCode.Ok)
                                        return result;
                                }
                                catch (Exception e)
                                {
                                    error = e;
                                    code = ReturnCode.Error;
                                }
                                finally
                                {
                                    if ((code != ReturnCode.Ok) &&
                                        (result != null))
                                    {
                                        //
                                        // NOTE: Dispose and clear the partially created TclBridge
                                        //       object because the Tcl command creation failed.
                                        //       This can throw an exception if the command token
                                        //       is valid and we cannot manage to delete it; however,
                                        //       since Tcl command creation is the very last step
                                        //       above, this corner case should be rare.
                                        //
                                        result.Dispose(); /* throw */
                                        result = null;
                                    }
                                }
                            }
                            else
                            {
                                error = "invalid command name";
                            }
                        }
                        else
                        {
                            error = "invalid command target";
                        }
                    }
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl Command Callbacks
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // **** WARNING ***** BEGIN CODE DIRECTLY CALLED BY THE NATIVE TCL RUNTIME ***** WARNING **** /
        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // -- ObjCmdProc --
        //
        // When proc is invoked, the clientData and interp parameters will be copies of
        // the clientData and interp arguments given to Tcl_CreateObjCommand. Typically,
        // clientData points to an application-specific data structure that describes
        // what to do when the command procedure is invoked. Objc and objv describe the
        // arguments to the command, objc giving the number of argument objects
        // (including the command name) and objv giving the values of the arguments. The
        // objv array will contain objc values, pointing to the argument objects. Unlike
        // argv[argv] used in a string-based command procedure, objv[objc] will not
        // contain NULL. Additionally, when proc is invoked, it must not modify the
        // contents of the objv array by assigning new pointer values to any element of
        // the array (for example, objv[2] = NULL) because this will cause memory to be
        // lost and the runtime stack to be corrupted. The CONST in the declaration of
        // objv will cause ANSI-compliant compilers to report any such attempted
        // assignment as an error. However, it is acceptable to modify the internal
        // representation of any individual object argument. For instance, the user may
        // call Tcl_GetIntFromObj on objv[2] to obtain the integer representation of that
        // object; that call may change the type of the object that objv[2] points at,
        // but will not change where objv[2] points. proc must return an integer code
        // that is either TCL_OK, TCL_ERROR, TCL_RETURN, TCL_BREAK, or TCL_CONTINUE. See
        // the Tcl overview man page for details on what these codes mean. Most normal
        // commands will only return TCL_OK or TCL_ERROR. In addition, if proc needs to
        // return a non-empty result, it can call Tcl_SetObjResult to set the
        // interpreter's result. In the case of a TCL_OK return code this gives the
        // result of the command, and in the case of TCL_ERROR this gives an error
        // message. Before invoking a command procedure, Tcl_EvalObjEx sets interpreter's
        // result to point to an object representing an empty string, so simple commands
        // can return an empty result by doing nothing at all. The contents of the objv
        // array belong to Tcl and are not guaranteed to persist once proc returns: proc
        // should not modify them. Call Tcl_SetObjResult if you want to return something
        // from the objv array.
        //
        /// <summary>
        /// This method is the object-based command callback invoked by the
        /// native Tcl runtime when a bridged Tcl command is evaluated.  It
        /// rehydrates the bridge from the client data, marshals the Tcl
        /// arguments into Eagle, executes the associated Eagle command, and
        /// sets the Tcl interpreter result from the Eagle result.
        /// </summary>
        /// <param name="clientData">
        /// The native pointer to the garbage collector handle that identifies
        /// the bridge associated with the command.
        /// </param>
        /// <param name="interp">
        /// The native pointer to the Tcl interpreter that is evaluating the
        /// command.
        /// </param>
        /// <param name="objc">
        /// The number of argument objects supplied to the command, including the
        /// command name itself.
        /// </param>
        /// <param name="objv">
        /// The native pointer to the array of argument objects supplied to the
        /// command.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode ObjCmdProc(
            IntPtr clientData,
            IntPtr interp,
            int objc,
            IntPtr objv
            )
        {
            ReturnCode code;

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
                    // NOTE: Attempt to cast the handle to a TclBridge object; if this
                    //       fails, we cannot continue to handle this call.
                    //
                    TclBridge tclBridge = handle.Target as TclBridge;

                    if (tclBridge != null)
                    {
                        Interlocked.Increment(ref tclBridge.objCmdProcLevels);

                        try
                        {
                            //
                            // NOTE: Grab the interpreter reference NOW, it may go bye bye if the
                            //       TclBridge object gets disposed.
                            //
                            Interpreter interpreter = tclBridge.interpreter;

                            if (interpreter != null)
                            {
                                //
                                // NOTE: Cache the fields of the interpreter object that we will
                                //       need to access below without holding a lock.
                                //
                                ITclApi tclApi = TclApi.GetTclApi(interpreter);
                                EngineFlags savedEngineFlags = interpreter.BeginExternalExecution();
                                bool noCacheArgument = false;

#if ARGUMENT_CACHE
                                if (EngineFlagOps.HasNoCacheArgument(savedEngineFlags))
                                    noCacheArgument = true;
#endif

                                try
                                {
                                    if (tclApi != null)
                                    {
                                        Result result = null;
                                        IExecute execute = tclBridge.execute;

                                        if (execute != null)
                                        {
                                            ArgumentList arguments = new ArgumentList();

                                            for (int index = 0; index < objc; index++)
                                            {
                                                IntPtr objPtr =
                                                    Marshal.ReadIntPtr(objv, index * IntPtr.Size);

                                                string value =
                                                    TclWrapper.GetString(tclApi, objPtr);

                                                if (value == null)
                                                    value = String.Empty;

                                                arguments.Add(Argument.GetOrCreate(
                                                    interpreter, value, noCacheArgument));
                                            }

                                            string name = (arguments.Count > 0) ? arguments[0] : null;

                                            try
                                            {
                                                code = interpreter.Execute(
                                                    name, execute, tclBridge.clientData, arguments,
                                                    ref result);
                                            }
                                            catch (Exception e)
                                            {
                                                result = String.Format(
                                                    "caught exception while executing: {0}",
                                                    e);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "invalid execute object";
                                            code = ReturnCode.Error;
                                        }

                                        //
                                        // NOTE: Set the Tcl interpreter result to the result of the
                                        //       Eagle command.
                                        //
                                        if (!String.IsNullOrEmpty(result))
                                            TclWrapper.SetResult(
                                                tclApi, interp, TclWrapper.NewString(tclApi, result));
                                        else
                                            TclWrapper.ResetResult(tclApi, interp);
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: There is no available Tcl API object; therefore, we
                                        //       cannot set the Tcl interpreter result.
                                        //
                                        TraceOps.DebugTrace(
                                            "invalid Tcl API object",
                                            typeof(Tcl_ObjCmdProc).Name,
                                            TracePriority.MarshalError);

                                        code = ReturnCode.Error;
                                    }
                                }
                                finally
                                {
                                    /* IGNORED */
                                    interpreter.EndAndCleanupExternalExecution(savedEngineFlags);
                                }
                            }
                            else
                            {
                                //
                                // NOTE: An invalid interpreter means that we have no Tcl API object to
                                //       work with either, punt on setting the Tcl interpreter result.
                                //
                                TraceOps.DebugTrace(
                                    "invalid interpreter",
                                    typeof(Tcl_ObjCmdProc).Name,
                                    TracePriority.MarshalError);

                                code = ReturnCode.Error;
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref tclBridge.objCmdProcLevels);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: What now?  We have no way of communicating with Tcl at this
                        //       point.
                        //
                        TraceOps.DebugTrace(
                            "invalid Tcl bridge object",
                            typeof(Tcl_ObjCmdProc).Name,
                            TracePriority.MarshalError);

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Again, nothing we can do at this point.
                    //
                    TraceOps.DebugTrace(
                        "invalid GC handle",
                        typeof(Tcl_ObjCmdProc).Name,
                        TracePriority.MarshalError);

                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_ObjCmdProc).Name,
                    TracePriority.NativeError);

                //
                // NOTE: At this point, we may not even be able to get to the Tcl API object
                //       we need to set the Tcl interpreter result; therefore, we are going
                //       to punt for now.
                //
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        //
        // -- CmdDeleteProc --
        //
        // DeleteProc will be invoked when (if) name is deleted. This can occur through a
        // call to Tcl_DeleteCommand, Tcl_DeleteCommandFromToken, or Tcl_DeleteInterp, or
        // by replacing name in another call to Tcl_CreateObjCommand. DeleteProc is
        // invoked before the command is deleted, and gives the application an
        // opportunity to release any structures associated with the command.
        //
        /// <summary>
        /// This method is the command deletion callback invoked by the native
        /// Tcl runtime when a bridged Tcl command is deleted.  It rehydrates the
        /// bridge from the client data, removes it from the containing
        /// interpreter, and disposes of it.
        /// </summary>
        /// <param name="clientData">
        /// The native pointer to the garbage collector handle that identifies
        /// the bridge associated with the command being deleted.
        /// </param>
        private static void CmdDeleteProc(
            IntPtr clientData
            )
        {
            //
            // NOTE: We need to kill the associated TclBridge object
            //       instance and remove any references to the bridge
            //       from the containing interpreter.
            //
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
                    // NOTE: Attempt to cast the handle to a TclBridge object; if this
                    //       fails, we cannot continue to handle this call.
                    //
                    TclBridge tclBridge = handle.Target as TclBridge;

                    if (tclBridge != null)
                    {
                        //
                        // NOTE: Skip messing with the TclBridge or interpreter object if it is
                        //       already being disposed (i.e. we are NOT being called directly
                        //       due to the command being removed from the Tcl interpreter or
                        //       the Tcl interpreter being deleted). The caller of the dispose
                        //       method will handle removing the TclBridge object from the
                        //       collection in the interpreter.
                        //
                        if (!tclBridge.disposing)
                        {
                            //
                            // NOTE: Grab the associated interpreter from the TclBridge
                            //       object.
                            //
                            Interpreter interpreter = tclBridge.interpreter;

                            //
                            // NOTE: Remove all instances of the TclBridge object from its
                            //       interpreter.
                            //
                            if (interpreter != null)
                                /* IGNORED */
                                interpreter.RemoveTclBridges(tclBridge);

                            //
                            // NOTE: Prevent the Dispose method from trying to delete the Tcl
                            //       command itself (since it is already being deleted).
                            //
                            tclBridge.token = IntPtr.Zero;

                            //
                            // NOTE: Cleanup all the resources used by this TclBridge object.
                            //       In theory, this object disposal can throw; however, in
                            //       practice we know that it does not attempt to do anything
                            //       that can actually "fail" unless it has a valid command
                            //       token, which we have already cleared (Tcl has notified us,
                            //       by calling this delegate, that it has already deleted the
                            //       command in question).
                            //
                            tclBridge.Dispose(); /* throw */
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "invalid Tcl bridge object",
                            typeof(Tcl_CmdDeleteProc).Name,
                            TracePriority.MarshalError);
                    }
                }
                else
                {
                    TraceOps.DebugTrace(
                        "invalid GC handle",
                        typeof(Tcl_CmdDeleteProc).Name,
                        TracePriority.MarshalError);
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_CmdDeleteProc).Name,
                    TracePriority.NativeError);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ***** WARNING ***** END CODE DIRECTLY CALLED BY THE NATIVE TCL RUNTIME ***** WARNING ***** /
        ///////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object is currently being disposed; used to prevent
        /// re-entrant disposal.
        /// </summary>
        private bool disposing;

        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this object has been disposed and
        /// the interpreter is configured to throw on access to disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(TclBridge).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this object.  If
        /// necessary, it deletes the associated Tcl command, frees the garbage
        /// collector handle that keeps this object alive, and clears its
        /// references to the Eagle and Tcl state.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            ) /* throw */
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(TclBridge).Name,
                TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (!this.disposing)
                {
                    //
                    // NOTE: We are now disposing this object (prevent re-entrancy).
                    //
                    this.disposing = true;

                    //
                    // NOTE: This method should not normally throw; however, if it does
                    //       we do not want our disposing flag to be stuck set to true.
                    //
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
                        // NOTE: If necessary (and possible), delete the Tcl command via the
                        //       token we saved earlier (when the Tcl command was created).
                        //
                        ReturnCode deleteCode = ReturnCode.Ok;
                        Result deleteError = null;

                        //
                        // NOTE: If we have a valid command token then we are still hooked to
                        //       Tcl via our inbound native delegates and we must unhook
                        //       successfully or throw to prevent our internal object state
                        //       from being made inconsistent.
                        //
                        if (token != IntPtr.Zero)
                        {
                            if (interpreter != null)
                            {
                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                //
                                // BUGFIX: We want to force deletion of this bridged command
                                //         if the force flag was specified upon creation OR
                                //         if the command is not actively being used.
                                //
                                deleteCode = TclWrapper.DeleteCommandFromToken(
                                    tclApi, interp, forceDelete || (objCmdProcLevels == 0),
                                    ref token, ref deleteError);
                            }
                            else
                            {
                                deleteError = "invalid interpreter";
                                deleteCode = ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Did we succeed in deleting the command from Tcl, if it
                        //       was necessary?
                        //
                        if (!noComplain && (deleteCode != ReturnCode.Ok))
                        {
                            //
                            // NOTE: If the command deletion was necessary and it failed
                            //       for any reason, complain very loudly.
                            //
                            DebugOps.Complain(interpreter, deleteCode, deleteError);

                            //
                            // BUGFIX: Also, we must throw an exception here to prevent
                            //         the delegates from being disposed while Tcl still
                            //         refers to them (tclLoad-1.2 GC race).
                            //
                            throw new ScriptException(deleteCode, deleteError);
                        }

                        //
                        // NOTE: If necessary, release the GCHandle that is keeping this
                        //       object alive.
                        //
                        if (handle.IsAllocated)
                            handle.Free();

                        //
                        // NOTE: We do not own these objects; therefore, we just null out
                        //       the references to them (in case we are the only thing
                        //       keeping them alive).
                        //
                        interpreter = null;
                        execute = null;
                        clientData = null;

                        //
                        // NOTE: Zero out our Tcl interpreter.  We do not delete it because
                        //       we do not own it.
                        //
                        interp = IntPtr.Zero;

                        //
                        // NOTE: Zero out our created Tcl command token.  We should not need
                        //       to call Tcl to delete the actual command because by this time
                        //       it should already have been deleted.
                        //
                        token = IntPtr.Zero;

                        //
                        // NOTE: Finally, we should be able to safely remove our references
                        //       to the Tcl callback delegates at this point because we already
                        //       deleted the Tcl command related to them.
                        //
                        objCmdProc = null;
                        cmdDeleteProc = null;

                        //
                        // NOTE: Zero out our command nesting level.
                        //
                        objCmdProcLevels = 0;

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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources used by this object, deleting the
        /// associated Tcl command if necessary, and suppresses finalization.
        /// </summary>
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes an instance of this class, releasing any resources that
        /// were not released by an explicit call to the <see cref="Dispose()" />
        /// method.
        /// </summary>
        ~TclBridge() /* throw */
        {
            Dispose(false);
        }
        #endregion
    }
}
