/*
 * InformationHost.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by a host that can write diagnostic and
    /// informational details about the interpreter and its state.  It extends
    /// the interactive host contract (<see cref="IInteractiveHost" />) with
    /// methods that format and write information about call frames, the call
    /// stack, flags, results, tokens, traces, variables, objects, and other
    /// interpreter state, most of which provide an overload that accepts
    /// explicit foreground and background colors.
    /// </summary>
    [ObjectId("38c00d90-386c-4ff5-a9c6-05e572717fd9")]
    public interface IInformationHost : IInteractiveHost
    {
        /// <summary>
        /// This method saves the host's current cursor position so that it can
        /// later be restored.
        /// </summary>
        /// <returns>
        /// True if the position was saved; otherwise, false.
        /// </returns>
        bool SavePosition();
        /// <summary>
        /// This method restores the host's cursor position previously saved
        /// with <see cref="SavePosition" />.
        /// </summary>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after restoring the
        /// position.
        /// </param>
        /// <returns>
        /// True if the position was restored; otherwise, false.
        /// </returns>
        bool RestorePosition(bool newLine);

        //
        // TODO: Change these to use the IInterpreter type.
        //
        /// <summary>
        /// This method writes announcement information associated with a
        /// breakpoint to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the announcement is associated with.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteAnnouncementInfo(
            Interpreter interpreter, BreakpointType breakpointType,
            string value, bool newLine);
        /// <summary>
        /// This method writes announcement information associated with a
        /// breakpoint to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the announcement is associated with.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteAnnouncementInfo(
            Interpreter interpreter, BreakpointType breakpointType,
            string value, bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the arguments associated with a
        /// breakpoint to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the breakpoint.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the information is associated with.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint the information is associated with.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to write information about.
        /// </param>
        /// <param name="result">
        /// The result value associated with the breakpoint.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteArgumentInfo(Interpreter interpreter, ReturnCode code,
            BreakpointType breakpointType, string breakpointName,
            ArgumentList arguments, Result result, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the arguments associated with a
        /// breakpoint to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the breakpoint.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the information is associated with.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint the information is associated with.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to write information about.
        /// </param>
        /// <param name="result">
        /// The result value associated with the breakpoint.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteArgumentInfo(Interpreter interpreter, ReturnCode code,
            BreakpointType breakpointType, string breakpointName,
            ArgumentList arguments, Result result, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes a representation of a single call frame to the
        /// host output, using the specified affixes and separator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
        /// </param>
        /// <param name="type">
        /// A string describing the kind of call frame being written.
        /// </param>
        /// <param name="prefix">
        /// The text to write before the call frame information.
        /// </param>
        /// <param name="suffix">
        /// The text to write after the call frame information.
        /// </param>
        /// <param name="separator">
        /// The character used to separate parts of the call frame information.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallFrame(Interpreter interpreter, ICallFrame frame,
            string type, string prefix, string suffix, char separator,
            DetailFlags detailFlags, bool newLine);

        /// <summary>
        /// This method writes information about a single call frame to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallFrameInfo(Interpreter interpreter, ICallFrame frame,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about a single call frame to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallFrameInfo(Interpreter interpreter, ICallFrame frame,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes a representation of the specified call stack to
        /// the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallStack(Interpreter interpreter, CallStack callStack,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes a representation of the specified call stack to
        /// the host output, limited to a maximum number of frames.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallStack(Interpreter interpreter, CallStack callStack,
            int limit, DetailFlags detailFlags, bool newLine);

        /// <summary>
        /// This method writes information about the specified call stack to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallStackInfo(Interpreter interpreter, CallStack callStack,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the specified call stack to the
        /// host output, limited to a maximum number of frames.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallStackInfo(Interpreter interpreter, CallStack callStack,
            int limit, DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the specified call stack to the
        /// host output, limited to a maximum number of frames and using the
        /// specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCallStackInfo(Interpreter interpreter, CallStack callStack,
            int limit, DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

#if DEBUGGER
        /// <summary>
        /// This method writes information about the interpreter's script
        /// debugger to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteDebuggerInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the interpreter's script
        /// debugger to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteDebuggerInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
#endif

        /// <summary>
        /// This method writes information about the interpreter's various flag
        /// sets to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to write information about.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to write information about.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to write information about.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to write information about.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteFlagInfo(Interpreter interpreter, EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags, EventFlags eventFlags,
            ExpressionFlags expressionFlags, HeaderFlags headerFlags,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the interpreter's various flag
        /// sets to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to write information about.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to write information about.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to write information about.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to write information about.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteFlagInfo(Interpreter interpreter, EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags, EventFlags eventFlags,
            ExpressionFlags expressionFlags, HeaderFlags headerFlags,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the specified host to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteHostInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the specified host to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteHostInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the specified interpreter to
        /// the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteInterpreterInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the specified interpreter to
        /// the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteInterpreterInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the interpreter's execution
        /// engine to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteEngineInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the interpreter's execution
        /// engine to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteEngineInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the entities defined in the
        /// interpreter to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteEntityInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the entities defined in the
        /// interpreter to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteEntityInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the native call stack to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteStackInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the native call stack to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteStackInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the interpreter's control state
        /// to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteControlInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the interpreter's control state
        /// to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteControlInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the interpreter's test state to
        /// the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteTestInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the interpreter's test state to
        /// the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteTestInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the specified token to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the token is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="token">
        /// The token to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteTokenInfo(Interpreter interpreter, IToken token,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the specified token to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the token is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="token">
        /// The token to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteTokenInfo(Interpreter interpreter, IToken token,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the specified trace to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the trace is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteTraceInfo(Interpreter interpreter, ITraceInfo traceInfo,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the specified trace to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the trace is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteTraceInfo(Interpreter interpreter, ITraceInfo traceInfo,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the variable is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteVariableInfo(Interpreter interpreter, IVariable variable,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the variable is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteVariableInfo(Interpreter interpreter, IVariable variable,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the specified object to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the object is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The object to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteObjectInfo(Interpreter interpreter, IObject @object,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the specified object to the
        /// host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the object is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The object to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteObjectInfo(Interpreter interpreter, IObject @object,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about the most recent complaint
        /// raised by the interpreter to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteComplaintInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about the most recent complaint
        /// raised by the interpreter to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteComplaintInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

#if HISTORY
        /// <summary>
        /// This method writes information about the interpreter's command
        /// execution history to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which history entries are written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteHistoryInfo(Interpreter interpreter,
            IHistoryFilter historyFilter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes information about the interpreter's command
        /// execution history to the host output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which history entries are written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteHistoryInfo(Interpreter interpreter,
            IHistoryFilter historyFilter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);
#endif

        /// <summary>
        /// This method writes custom, host-specific information to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCustomInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        /// <summary>
        /// This method writes custom, host-specific information to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteCustomInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes complete information about a result, including
        /// the previous result, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="previousResult">
        /// The previous result value to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteAllResultInfo(ReturnCode code, Result result, int errorLine,
            Result previousResult, DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes complete information about a result, including
        /// the previous result, to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="previousResult">
        /// The previous result value to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteAllResultInfo(ReturnCode code, Result result, int errorLine,
            Result previousResult, DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// This method writes information about a named result to the host
        /// output.
        /// </summary>
        /// <param name="name">
        /// The name associated with the result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteResultInfo(string name, ReturnCode code, Result result,
            int errorLine, DetailFlags detailFlags, bool newLine);
        /// <summary>
        /// This method writes information about a named result to the host
        /// output, using the specified colors.
        /// </summary>
        /// <param name="name">
        /// The name associated with the result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        bool WriteResultInfo(string name, ReturnCode code, Result result,
            int errorLine, DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

#if SHELL
        /// <summary>
        /// This method writes the interactive loop header to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the header is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the header is associated
        /// with.
        /// </param>
        /// <param name="result">
        /// The result value to include in the header, if any.
        /// </param>
        void WriteHeader(Interpreter interpreter, IInteractiveLoopData loopData,
            Result result);

        /// <summary>
        /// This method writes the interactive loop footer to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the footer is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the footer is associated
        /// with.
        /// </param>
        /// <param name="result">
        /// The result value to include in the footer, if any.
        /// </param>
        void WriteFooter(Interpreter interpreter, IInteractiveLoopData loopData,
            Result result);
#endif
    }
}
