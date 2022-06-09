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
    [ObjectId("51e232aa-edb5-4dd1-b223-010e6450c339")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBridge /* It's how Tcl/Tk is done. */ : IDisposable
    {
        #region Private Data
        private GCHandle handle; /* TclBridge */
        private Interpreter interpreter;
        private IExecute execute;
        private IClientData clientData;
        private IntPtr interp;
        private int objCmdProcLevels; // NOTE: Nesting level for ObjCmdProc.
        private Tcl_ObjCmdProc objCmdProc;
        private Tcl_CmdDeleteProc cmdDeleteProc;
        private IntPtr token;
        private bool fromThread;
        private bool forceDelete;
        private bool noComplain;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
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
        public bool Match(
            IExecute execute
            )
        {
            CheckDisposed();

            return InternalMatch(execute);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Match(
            IntPtr interp
            )
        {
            CheckDisposed();

            return InternalMatch(interp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            // CheckDisposed(); /* EXEMPT: During disposal. */

            string result = EntityOps.GetNameNoThrow(execute);

            return (result != null) ? result : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
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
        private bool disposing;
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(TclBridge).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~TclBridge() /* throw */
        {
            Dispose(false);
        }
        #endregion
    }
}
