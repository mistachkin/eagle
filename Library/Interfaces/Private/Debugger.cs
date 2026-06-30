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
    /// <summary>
    /// This interface extends the debugger state with the operations used to
    /// drive the Eagle script debugger, including managing its interactive
    /// loops and active evaluation count, advancing the step counter,
    /// manipulating breakpoints, and dispatching watchpoint and breakpoint
    /// events.
    /// </summary>
    [ObjectId("198a1ed1-9f54-46e5-8dea-9e7e7f832673")]
    internal interface IDebugger : IDebuggerData
    {
        /// <summary>
        /// This method adds diagnostic information about the debugger to the
        /// specified list.
        /// </summary>
        /// <param name="list">
        /// The list to add the diagnostic information to.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included.
        /// </param>
        void AddInfo(StringPairList list, DetailFlags detailFlags);

        /// <summary>
        /// This method checks for and processes any pending debugger callbacks.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        void CheckCallbacks(Interpreter interpreter);

        /// <summary>
        /// This method records entry into a debugger interactive loop.
        /// </summary>
        /// <returns>
        /// The resulting number of active debugger interactive loops.
        /// </returns>
        int EnterLoop();

        /// <summary>
        /// This method records exit from a debugger interactive loop.
        /// </summary>
        /// <returns>
        /// The resulting number of active debugger interactive loops.
        /// </returns>
        int ExitLoop();

        /// <summary>
        /// This method increases or decreases the active debugger evaluation
        /// count.
        /// </summary>
        /// <param name="active">
        /// Non-zero to increase the active evaluation count; zero to decrease
        /// it.
        /// </param>
        /// <returns>
        /// The resulting active debugger evaluation count.
        /// </returns>
        int SetActive(bool active); /* increase or decrease active debugger eval count */

        /// <summary>
        /// This method advances the debugger step counter.
        /// </summary>
        /// <returns>
        /// The resulting step counter value.
        /// </returns>
        long NextStep(); /* advance the step counter */

        /// <summary>
        /// This method conditionally advances the debugger step counter.
        /// </summary>
        /// <returns>
        /// True if the step counter has now reached zero; otherwise, false.
        /// </returns>
        bool MaybeNextStep(); /* maybe advance the step counter, return non-zero if NOW zero */

#if DEBUGGER_BREAKPOINTS
        //
        // TODO: Change these to use the IInterpreter type.
        //
        /// <summary>
        /// This method returns the list of breakpoints whose script locations
        /// match the specified pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match breakpoint locations.  This parameter may
        /// be null to match all breakpoints.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching breakpoints.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetBreakpointList(Interpreter interpreter, string pattern,
            bool noCase, ref IStringList list, ref Result error);

        /// <summary>
        /// This method determines whether a breakpoint is set at the specified
        /// script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="location">
        /// The script location to check for a breakpoint.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint is set at the
        /// specified location.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value.
        /// </returns>
        ReturnCode MatchBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match);

        /// <summary>
        /// This method determines whether a breakpoint is set at the specified
        /// script location, reporting any error.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="location">
        /// The script location to check for a breakpoint.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint is set at the
        /// specified location.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MatchBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match, ref Result error);

        /// <summary>
        /// This method clears the breakpoint at the specified script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="location">
        /// The script location to clear a breakpoint from.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint was cleared at the
        /// specified location.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ClearBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match, ref Result error);

        /// <summary>
        /// This method sets a breakpoint at the specified script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="location">
        /// The script location to set a breakpoint at.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint was set at the
        /// specified location.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetBreakpoint(Interpreter interpreter,
            IScriptLocation location, ref bool match, ref Result error);
#endif

        /// <summary>
        /// This method initializes the debugger to its default state (i.e.
        /// enabled and inactive).
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Initialize(ref Result error); /* initialize the debugger to its
                                                  * default state (i.e. enabled and
                                                  * inactive) */

        /// <summary>
        /// This method disables all debugging features so that the script runs
        /// at full speed.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Reset(ref Result error); /* disables all debugging features
                                             * (i.e. run script at full speed) */

        /// <summary>
        /// This method temporarily suspends stepping through code.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Suspend(ref Result error); /* temporarily suspend stepping
                                               * through code */

        /// <summary>
        /// This method resumes from a temporary suspension of stepping through
        /// code.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Resume(ref Result error); /* resume from temporary suspension
                                              * of stepping through code */

        /// <summary>
        /// This method returns all commands from the interactive queue.
        /// </summary>
        /// <param name="result">
        /// Upon success, this receives all commands from the interactive queue;
        /// upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode DumpCommands(
            ref Result result
            ); /* return all commands from the interactive queue. */

        /// <summary>
        /// This method clears all commands from the interactive queue.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ClearCommands(
            ref Result error
            ); /* clear all commands from the interactive queue. */

        /// <summary>
        /// This method adds a command to the interactive queue.
        /// </summary>
        /// <param name="text">
        /// The command text to add to the interactive queue.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode EnqueueCommand(
            string text,
            ref Result error
            ); /* add a command to the interactive queue. */

        /// <summary>
        /// This method adds one or more commands to the interactive queue.
        /// </summary>
        /// <param name="text">
        /// The command text to add to the interactive queue.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode EnqueueBuffer(
            string text,
            ref Result error
            ); /* add command(s) to the interactive queue. */

        //
        // TODO: Change these to use the IInterpreter type.
        //
        /// <summary>
        /// This method dispatches a variable watchpoint to the interactive
        /// debugger loop.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the current interactive loop.
        /// </param>
        /// <param name="result">
        /// Upon success, this may receive a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Watchpoint(
            Interpreter interpreter, IInteractiveLoopData loopData,
            ref Result result);

        /// <summary>
        /// This method dispatches a breakpoint to the interactive debugger
        /// loop.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the current interactive loop.
        /// </param>
        /// <param name="result">
        /// Upon success, this may receive a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Breakpoint(
            Interpreter interpreter, IInteractiveLoopData loopData,
            ref Result result);
    }
}
