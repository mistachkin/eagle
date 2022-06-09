/*
 * ScriptThread.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("a93e417d-5ad1-413e-bd26-feb58d04a0f0")]
    public interface IScriptThread : IGetInterpreter
    {
        ///////////////////////////////////////////////////////////////////////
        // OWNED RESOURCES PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        Thread Thread { get; }

        ///////////////////////////////////////////////////////////////////////
        // OBJECT IDENTITY & AFFINITY PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        long Id { get; }
        string Name { get; }
        int ActiveCount { get; } // NOTE: How many in this AppDomain?

        ///////////////////////////////////////////////////////////////////////
        // THREAD CREATION & SETUP PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        ThreadFlags ThreadFlags { get; }
        int MaxStackSize { get; }
        int Timeout { get; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD CREATION PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool UserInterface { get; }
        bool IsBackground { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER CREATION & SETUP PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        IEnumerable<string> Args { get; }
        IHost Host { get; }
        CreateFlags CreateFlags { get; }
        HostCreateFlags HostCreateFlags { get; }
        InitializeFlags InitializeFlags { get; }
        ScriptFlags ScriptFlags { get; }
        InterpreterFlags InterpreterFlags { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER HANDLING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool UseSelf { get; }
        bool UseActiveStack { get; }

        ///////////////////////////////////////////////////////////////////////
        // ERROR HANDLING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool Quiet { get; }
        bool NoBackgroundError { get; }

        ///////////////////////////////////////////////////////////////////////
        // EVENT HANDLING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        IScript Script { get; }
        string VarName { get; }
        EventWaitFlags EventWaitFlags { get; }
        VariableFlags EventVariableFlags { get; }
        bool NoComplain { get; }

        ///////////////////////////////////////////////////////////////////////
        // DIAGNOSTIC READ-WRITE PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool Verbose { get; set; }
        bool Debug { get; set; }
        ReturnCode ReturnCode { get; set; }
        Result Result { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD STATE PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool IsAlive { get; }
        bool IsBusy { get; }
        bool IsDisposed { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER DISPOSAL & PURGING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool UsePool { get; }
        bool PurgeGlobal { get; }
        bool NoAbort { get; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD STATE METHODS
        ///////////////////////////////////////////////////////////////////////

        bool Start();

        bool Stop();
        bool Stop(bool force);

        ///////////////////////////////////////////////////////////////////////
        // CLR OBJECT INTEGRATION METHODS
        ///////////////////////////////////////////////////////////////////////

        ReturnCode AddObject(object value);
        ReturnCode AddObject(object value, ref Result result);
        ReturnCode AddObject(object value, bool alias, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType,
            object value, bool alias, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType, object value,
            bool alias, bool aliasReference, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType,
            ObjectFlags objectFlags, object value, bool alias,
            bool aliasReference, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType,
            string objectName, ObjectFlags objectFlags, object value,
            bool alias, bool aliasReference, ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONOUS WAIT METHODS
        ///////////////////////////////////////////////////////////////////////

        bool WaitForStart();
        bool WaitForStart(int timeout);
        bool WaitForStart(int timeout, bool strict);

        bool WaitForEnd();
        bool WaitForEnd(int timeout);
        bool WaitForEnd(int timeout, bool strict);

        bool WaitForEmpty();
        bool WaitForEmpty(int timeout);
        bool WaitForEmpty(int timeout, bool strict);
        bool WaitForEmpty(int timeout, bool idle, bool strict);

        bool WaitForEvent();
        bool WaitForEvent(int timeout);
        bool WaitForEvent(int timeout, bool strict);
        bool WaitForEvent(int timeout, bool idle, bool strict);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS QUEUEING METHODS (VIA EVENT MANAGER)
        ///////////////////////////////////////////////////////////////////////

        bool Queue(EventCallback callback, IClientData clientData);
        bool Queue(DateTime dateTime, EventCallback callback,
            IClientData clientData);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS EVALUATION METHODS (VIA ENGINE)
        ///////////////////////////////////////////////////////////////////////

        bool Queue(string text);
        bool Queue(DateTime dateTime, string text);
        bool Queue(string text, AsynchronousCallback callback,
            IClientData clientData);

        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONOUS EVALUATION METHODS (VIA EVENT MANAGER AND/OR ENGINE)
        ///////////////////////////////////////////////////////////////////////

        ReturnCode Send(string text, ref Result result);
        ReturnCode Send(string text, bool useEngine, ref Result result);
        ReturnCode Send(string text, int timeout, bool useEngine,
            ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SIGNALING METHODS
        ///////////////////////////////////////////////////////////////////////

        bool Signal(string value);
        bool WakeUp();

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SCRIPT CANCELLATION METHODS
        ///////////////////////////////////////////////////////////////////////

        bool Cancel(CancelFlags cancelFlags);
        bool Cancel(CancelFlags cancelFlags, ref Result error);

        bool ResetCancel(CancelFlags cancelFlags);
        bool ResetCancel(CancelFlags cancelFlags, ref Result error);

        ///////////////////////////////////////////////////////////////////////
        // CLEANUP METHODS (NON-PRIMARY THREAD CONTEXTS)
        ///////////////////////////////////////////////////////////////////////

        bool Cleanup();
    }
}
