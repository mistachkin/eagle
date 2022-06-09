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
    [ObjectId("2ff0ef59-fe15-4c0d-84b1-854972c34348")]
    public interface ITclEntityManager
    {
        ///////////////////////////////////////////////////////////////////////
        // TCL INTERPRETER SUPPORT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is Tcl interpreter support available?
        //
        bool HasTclInterpreters(ref Result error);

        ReturnCode DoesTclInterpreterExist(string name);

        ReturnCode GetTclInterpreter(
            string name,
            LookupFlags lookupFlags,
            ref IntPtr interp,
            ref Result error
            );

        //
        // NOTE: For CreateTclInterpreter, "result" is set to interpName upon
        //       success.
        //
        ReturnCode CreateTclInterpreter(
            bool initialize,
            bool memory,
            bool safe,
            ref Result result
            );

        ReturnCode DeleteTclInterpreter(
            string name,
            ref Result result
            );

#if TCL_THREADS
        ///////////////////////////////////////////////////////////////////////
        // TCL THREAD SUPPORT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is Tcl thread support available?
        //
        bool HasTclThreads(ref Result error);
        ReturnCode DoesTclThreadExist(string name);

        ///////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        ReturnCode GetTclThread(
            string name,
            LookupFlags lookupFlags,
            ref TclThread thread,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For CreateTclThread, "result" is set to threadName upon
        //       success.
        //
        ReturnCode CreateTclThread(
            ResultCallback callback,
            IClientData clientData,
            int timeout,
            bool generic,
            bool debug,
            bool wait,
            ref Result result
            );

        ReturnCode DeleteTclThread(
            string name,
            bool strict,
            ref Result result
            );
#endif

        ///////////////////////////////////////////////////////////////////////
        // TCL COMMAND SUPPORT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is Tcl bridge support available?
        //
        bool HasTclBridges(ref Result error);
        ReturnCode DoesTclBridgeExist(string name);

        //
        // NOTE: For AddTclBridge, "result" is set to bridgeName upon success.
        //
        ReturnCode AddTclBridge(
            IExecute execute,
            string interpName,
            string commandName,
            IClientData clientData,
            bool forceDelete,
            bool noComplain,
            ref Result result
            );

        //
        // NOTE: For AddStandardTclBridge, "result" is set to bridgeName upon
        //       success.
        //
        ReturnCode AddStandardTclBridge(
            string interpName,
            string commandName,
            IClientData clientData,
            bool forceDelete,
            bool noComplain,
            ref Result result
            );

        ReturnCode RemoveTclBridge(
            string interpName,
            string commandName,
            IClientData clientData,
            ref Result result
            );
    }
}
