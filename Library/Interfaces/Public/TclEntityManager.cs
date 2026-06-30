/*
 * TclEntityManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

#if TCL_WRAPPER
using Eagle._Components.Private.Tcl;
#endif

using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

#if TCL_WRAPPER
using Eagle._Interfaces.Private.Tcl;
#endif

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the lifetime of
    /// the native Tcl entities associated with an interpreter, including Tcl
    /// interpreters, Tcl threads, and the command bridges that expose Eagle
    /// commands to native Tcl.
    /// </summary>
    [ObjectId("2ff0ef59-fe15-4c0d-84b1-854972c34348")]
    public interface ITclEntityManager
    {
        ///////////////////////////////////////////////////////////////////////
        // TCL INTERPRETER SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether native Tcl interpreter support is currently
        /// available.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if Tcl interpreter support is available; otherwise, false.
        /// </returns>
        //
        // NOTE: Is Tcl interpreter support available?
        //
        bool HasTclInterpreters(ref Result error);

        /// <summary>
        /// Determines whether a Tcl interpreter with the specified name
        /// currently exists.
        /// </summary>
        /// <param name="name">
        /// The name of the Tcl interpreter to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the named Tcl interpreter exists;
        /// otherwise, a non-Ok value.
        /// </returns>
        ReturnCode DoesTclInterpreterExist(string name);

        /// <summary>
        /// Looks up a previously created Tcl interpreter by name and returns
        /// the native handle to it.
        /// </summary>
        /// <param name="name">
        /// The name of the Tcl interpreter to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="interp">
        /// Upon success, this will contain the native handle for the located
        /// Tcl interpreter.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetTclInterpreter(
            string name,
            LookupFlags lookupFlags,
            ref IntPtr interp,
            ref Result error
            );

        /// <summary>
        /// Creates a new native Tcl interpreter.  Upon success, the
        /// <paramref name="result" /> parameter is set to the name of the
        /// newly created Tcl interpreter.
        /// </summary>
        /// <param name="createFlags">
        /// The flags used to control how the Tcl interpreter is created.
        /// </param>
        /// <param name="result">
        /// Upon success, this is set to the name of the newly created Tcl
        /// interpreter.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // NOTE: For CreateTclInterpreter, "result" is set to interpName upon
        //       success.
        //
        ReturnCode CreateTclInterpreter(
            TclCreateFlags createFlags,
            ref Result result
            );

        /// <summary>
        /// Deletes a previously created Tcl interpreter by name.
        /// </summary>
        /// <param name="name">
        /// The name of the Tcl interpreter to delete.
        /// </param>
        /// <param name="result">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode DeleteTclInterpreter(
            string name,
            ref Result result
            );

#if TCL_THREADS
        ///////////////////////////////////////////////////////////////////////
        // TCL THREAD SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether native Tcl thread support is currently
        /// available.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if Tcl thread support is available; otherwise, false.
        /// </returns>
        //
        // NOTE: Is Tcl thread support available?
        //
        bool HasTclThreads(ref Result error);
        /// <summary>
        /// Determines whether a Tcl thread with the specified name currently
        /// exists.
        /// </summary>
        /// <param name="name">
        /// The name of the Tcl thread to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the named Tcl thread exists;
        /// otherwise, a non-Ok value.
        /// </returns>
        ReturnCode DoesTclThreadExist(string name);

        ///////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        /// <summary>
        /// Looks up a previously created Tcl thread by name and returns the
        /// object that manages it.
        /// </summary>
        /// <param name="name">
        /// The name of the Tcl thread to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="thread">
        /// Upon success, this will contain the object that manages the located
        /// Tcl thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetTclThread(
            string name,
            LookupFlags lookupFlags,
            ref TclThread thread,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new native Tcl thread, which hosts a dedicated Tcl
        /// interpreter.  Upon success, the <paramref name="result" /> parameter
        /// is set to the name of the newly created Tcl thread.
        /// </summary>
        /// <param name="callback">
        /// The callback to invoke with the result of the operations performed
        /// on the Tcl thread.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to supply to the callback, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time to wait, in milliseconds, for the Tcl
        /// thread to start.
        /// </param>
        /// <param name="threadFlags">
        /// The flags used to control how the Tcl thread is created.
        /// </param>
        /// <param name="result">
        /// Upon success, this is set to the name of the newly created Tcl
        /// thread.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // NOTE: For CreateTclThread, "result" is set to threadName upon
        //       success.
        //
        ReturnCode CreateTclThread(
            ResultCallback callback,
            IClientData clientData,
            int timeout,
            TclThreadFlags threadFlags,
            ref Result result
            );

        /// <summary>
        /// Deletes a previously created Tcl thread by name.
        /// </summary>
        /// <param name="name">
        /// The name of the Tcl thread to delete.
        /// </param>
        /// <param name="threadFlags">
        /// The flags used to control how the Tcl thread is deleted.
        /// </param>
        /// <param name="result">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode DeleteTclThread(
            string name,
            TclThreadFlags threadFlags,
            ref Result result
            );
#endif

        ///////////////////////////////////////////////////////////////////////
        // TCL COMMAND SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether native Tcl command bridge support is currently
        /// available.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if Tcl bridge support is available; otherwise, false.
        /// </returns>
        //
        // NOTE: Is Tcl bridge support available?
        //
        bool HasTclBridges(ref Result error);
        /// <summary>
        /// Determines whether a Tcl command bridge with the specified name
        /// currently exists.
        /// </summary>
        /// <param name="name">
        /// The name of the Tcl command bridge to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the named Tcl bridge exists;
        /// otherwise, a non-Ok value.
        /// </returns>
        ReturnCode DoesTclBridgeExist(string name);

        /// <summary>
        /// Creates a command bridge that exposes the specified Eagle execute
        /// entity as a command within a native Tcl interpreter.  Upon success,
        /// the <paramref name="result" /> parameter is set to the name of the
        /// newly created bridge.
        /// </summary>
        /// <param name="execute">
        /// The entity to invoke when the bridged Tcl command is executed.
        /// This parameter should not be null.
        /// </param>
        /// <param name="interpName">
        /// The name of the Tcl interpreter to add the bridged command to.
        /// </param>
        /// <param name="commandName">
        /// The name of the command to create within the Tcl interpreter.
        /// </param>
        /// <param name="clientData">
        /// The extra data to supply to the execute entity, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="commandFlags">
        /// The flags used to control how the bridged command is created.
        /// </param>
        /// <param name="result">
        /// Upon success, this is set to the name of the newly created bridge.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // NOTE: For AddTclBridge, "result" is set to bridgeName upon success.
        //
        ReturnCode AddTclBridge(
            IExecute execute,
            string interpName,
            string commandName,
            IClientData clientData,
            TclCommandFlags commandFlags,
            ref Result result
            );

        /// <summary>
        /// Creates a command bridge that exposes the standard Eagle command
        /// handler as a command within a native Tcl interpreter.  Upon success,
        /// the <paramref name="result" /> parameter is set to the name of the
        /// newly created bridge.
        /// </summary>
        /// <param name="interpName">
        /// The name of the Tcl interpreter to add the bridged command to.
        /// </param>
        /// <param name="commandName">
        /// The name of the command to create within the Tcl interpreter.
        /// </param>
        /// <param name="clientData">
        /// The extra data to supply to the command handler, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="commandFlags">
        /// The flags used to control how the bridged command is created.
        /// </param>
        /// <param name="result">
        /// Upon success, this is set to the name of the newly created bridge.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // NOTE: For AddStandardTclBridge, "result" is set to bridgeName upon
        //       success.
        //
        ReturnCode AddStandardTclBridge(
            string interpName,
            string commandName,
            IClientData clientData,
            TclCommandFlags commandFlags,
            ref Result result
            );

        /// <summary>
        /// Removes a previously created Tcl command bridge from a native Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the Tcl interpreter to remove the bridged command from.
        /// </param>
        /// <param name="commandName">
        /// The name of the bridged command to remove from the Tcl interpreter.
        /// </param>
        /// <param name="clientData">
        /// The extra data originally associated with the bridged command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="commandFlags">
        /// The flags used to control how the bridged command is removed.
        /// </param>
        /// <param name="result">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveTclBridge(
            string interpName,
            string commandName,
            IClientData clientData,
            TclCommandFlags commandFlags,
            ref Result result
            );
    }
}
