/*
 * TclManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private.Tcl;
using Eagle._Components.Public;

#if TCL_WRAPPER
using Eagle._Interfaces.Private.Tcl;
#endif

namespace Eagle._Interfaces.Public
{
    [ObjectId("e3c85670-a38f-4f0a-b99e-e6c39c51359b")]
    public interface ITclManager
    {
        ///////////////////////////////////////////////////////////////////////
        // TCL SYNCHRONIZATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: You should not need to use this.
        //
        object TclSyncRoot { get; }

        ///////////////////////////////////////////////////////////////////////
        // TCL READ-ONLY SUPPORT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For dealing with read-only mode.
        //
        bool TclReadOnly { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // TCL LOADER SUPPORT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is Tcl loaded and available?
        //
        bool HasTcl(ref Result error);

        //
        // NOTE: Is the current thread the correct one for Tcl usage?
        //
        bool IsTclThread();

        //
        // NOTE: Returns the patch level of Tcl that was previously loaded.
        //
        ReturnCode GetTclPatchLevel(
            ref Version patchLevel,
            ref Result error
            );

        //
        // NOTE: Attempt to find and load the native Tcl library now.
        //
        ReturnCode LoadTcl(
            FindFlags findFlags,
            LoadFlags loadFlags,
            IEnumerable<string> paths,
            string text,
            Version minimumRequired,
            Version maximumRequired,
            Version unknown,
            IClientData clientData,
            ref Result result
            );

        //
        // NOTE: Attempt to unload the native Tcl library now.
        //
        ReturnCode UnloadTcl(
            UnloadFlags unloadFlags,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        //
        // NOTE: You should not need to use this either.
        //
        ITclApi TclApi { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // TCL INTERPRETER SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode IsTclInterpreterCanceled(
            string name,
            ref Result error
            );

        ReturnCode IsTclInterpreterCanceled(
            string name,
            Tcl_CanceledFlags flags,
            ref Result error
            );

        bool IsTclInterpreterReady(
            string name,
            bool deleted,
            ref Result error
            );

        bool IsTclInterpreterActive(string name);
        int GetTclInterpreterErrorLine(string name);

#if TCL_THREADS
        ///////////////////////////////////////////////////////////////////////
        // TCL THREAD EVENT SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode QueueTclThreadEvent(
            string threadName,
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result
            );

        ReturnCode QueueTclThreadEvent(
            string threadName,
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result,
            ref int errorLine
            );
#endif

        ///////////////////////////////////////////////////////////////////////
        // TCL SCRIPT CANCELLATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode CancelTclEvaluate(
            string interpName,
            Result result,
            ref Result error
            );

        ReturnCode CancelTclEvaluate(
            string interpName,
            Result result,
            Tcl_EvalFlags flags,
            ref Result error
            );

        ReturnCode CancelTclEvaluate(
            string interpName,
            Result result,
            Tcl_EvalFlags flags,
            ref IClientData clientData,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL EXPRESSION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            ref Result result
            );

        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL SCRIPT EVALUATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        bool TclExceptions { get; set; }

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            ref Result result
            );

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref Result result
            );

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref Result result
            );

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL FILE EVALUATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            ref Result result
            );

        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            bool exceptions,
            ref Result result
            );

        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL INTERACTIVE SCRIPT EVALUATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            ref Result result
            );

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref Result result
            );

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref Result result
            );

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL STRING SUBSTITUTION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            ref Result result
            );

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            ref Result result
            );

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            bool exceptions,
            ref Result result
            );

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            ref IClientData clientData,
            ref Result result
            );

        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL VARIABLE SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetTclVariableValue(
            string interpName,
            Tcl_VarFlags flags,
            string name,
            ref Result value,
            ref Result error
            );

        ReturnCode SetTclVariableValue(
            string interpName,
            Tcl_VarFlags flags,
            string name,
            ref Result value,
            ref Result error
            );

        ReturnCode UnsetTclVariableValue(
            string interpName,
            Tcl_VarFlags flags,
            string name,
            ref Result error
            );
    }
}
