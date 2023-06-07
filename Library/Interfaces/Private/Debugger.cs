/*
 * Debugger.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    //
    // NOTE: This interface is currently private; however, it may be "promoted"
    //       to public at some point.
    //
    [ObjectId("198a1ed1-9f54-46e5-8dea-9e7e7f832673")]
    internal interface IDebugger : IDebuggerData
    {
        void AddInfo(StringPairList list, DetailFlags detailFlags);
        void CheckCallbacks(Interpreter interpreter);

        int EnterLoop();
        int ExitLoop();

        int SetActive(bool active); /* increase or decrease active debugger eval count */
        long NextStep(); /* advance the step counter */
        bool MaybeNextStep(); /* maybe advance the step counter, return non-zero if NOW zero */

#if DEBUGGER_BREAKPOINTS
        //
        // TODO: Change these to use the IInterpreter type.
        //
        ReturnCode GetBreakpointList(Interpreter interpreter, string pattern,
            bool noCase, ref IStringList list, ref Result error);

        ReturnCode MatchBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match);

        ReturnCode MatchBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match, ref Result error);

        ReturnCode ClearBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match, ref Result error);

        ReturnCode SetBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match, ref Result error);
#endif

        ReturnCode Initialize(ref Result error); /* initialize the debugger to its
                                                  * default state (i.e. enabled and
                                                  * inactive) */

        ReturnCode Reset(ref Result error); /* disables all debugging features
                                             * (i.e. run script at full speed) */

        ReturnCode Suspend(ref Result error); /* temporarily suspend stepping
                                               * through code */

        ReturnCode Resume(ref Result error); /* resume from temporary suspension
                                              * of stepping through code */

        ReturnCode DumpCommands(
            ref Result result
            ); /* return all commands from the interactive queue. */

        ReturnCode ClearCommands(
            ref Result error
            ); /* clear all commands from the interactive queue. */

        ReturnCode EnqueueCommand(
            string text,
            ref Result error
            ); /* add a command to the interactive queue. */

        ReturnCode EnqueueBuffer(
            string text,
            ref Result error
            ); /* add command(s) to the interactive queue. */

        //
        // TODO: Change these to use the IInterpreter type.
        //
        ReturnCode Watchpoint(
            Interpreter interpreter, IInteractiveLoopData loopData,
            ref Result result);

        ReturnCode Breakpoint(
            Interpreter interpreter, IInteractiveLoopData loopData,
            ref Result result);
    }
}
