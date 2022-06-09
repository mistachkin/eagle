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
    [ObjectId("38c00d90-386c-4ff5-a9c6-05e572717fd9")]
    public interface IInformationHost : IInteractiveHost
    {
        bool SavePosition();
        bool RestorePosition(bool newLine);

        //
        // TODO: Change these to use the IInterpreter type.
        //
        bool WriteAnnouncementInfo(
            Interpreter interpreter, BreakpointType breakpointType,
            string value, bool newLine);
        bool WriteAnnouncementInfo(
            Interpreter interpreter, BreakpointType breakpointType,
            string value, bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteArgumentInfo(Interpreter interpreter, ReturnCode code,
            BreakpointType breakpointType, string breakpointName,
            ArgumentList arguments, Result result, DetailFlags detailFlags,
            bool newLine);
        bool WriteArgumentInfo(Interpreter interpreter, ReturnCode code,
            BreakpointType breakpointType, string breakpointName,
            ArgumentList arguments, Result result, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteCallFrame(Interpreter interpreter, ICallFrame frame,
            string type, string prefix, string suffix, char separator,
            DetailFlags detailFlags, bool newLine);

        bool WriteCallFrameInfo(Interpreter interpreter, ICallFrame frame,
            DetailFlags detailFlags, bool newLine);
        bool WriteCallFrameInfo(Interpreter interpreter, ICallFrame frame,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteCallStack(Interpreter interpreter, CallStack callStack,
            DetailFlags detailFlags, bool newLine);
        bool WriteCallStack(Interpreter interpreter, CallStack callStack,
            int limit, DetailFlags detailFlags, bool newLine);

        bool WriteCallStackInfo(Interpreter interpreter, CallStack callStack,
            DetailFlags detailFlags, bool newLine);
        bool WriteCallStackInfo(Interpreter interpreter, CallStack callStack,
            int limit, DetailFlags detailFlags, bool newLine);
        bool WriteCallStackInfo(Interpreter interpreter, CallStack callStack,
            int limit, DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

#if DEBUGGER
        bool WriteDebuggerInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine);
        bool WriteDebuggerInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
#endif

        bool WriteFlagInfo(Interpreter interpreter, EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags, EventFlags eventFlags,
            ExpressionFlags expressionFlags, HeaderFlags headerFlags,
            DetailFlags detailFlags, bool newLine);
        bool WriteFlagInfo(Interpreter interpreter, EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags, EventFlags eventFlags,
            ExpressionFlags expressionFlags, HeaderFlags headerFlags,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteHostInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        bool WriteHostInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteInterpreterInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine);
        bool WriteInterpreterInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteEngineInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        bool WriteEngineInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteEntityInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        bool WriteEntityInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteStackInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        bool WriteStackInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteControlInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        bool WriteControlInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteTestInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        bool WriteTestInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteTokenInfo(Interpreter interpreter, IToken token,
            DetailFlags detailFlags, bool newLine);
        bool WriteTokenInfo(Interpreter interpreter, IToken token,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteTraceInfo(Interpreter interpreter, ITraceInfo traceInfo,
            DetailFlags detailFlags, bool newLine);
        bool WriteTraceInfo(Interpreter interpreter, ITraceInfo traceInfo,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteVariableInfo(Interpreter interpreter, IVariable variable,
            DetailFlags detailFlags, bool newLine);
        bool WriteVariableInfo(Interpreter interpreter, IVariable variable,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteObjectInfo(Interpreter interpreter, IObject @object,
            DetailFlags detailFlags, bool newLine);
        bool WriteObjectInfo(Interpreter interpreter, IObject @object,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteComplaintInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine);
        bool WriteComplaintInfo(Interpreter interpreter,
            DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

#if HISTORY
        bool WriteHistoryInfo(Interpreter interpreter,
            IHistoryFilter historyFilter, DetailFlags detailFlags,
            bool newLine);
        bool WriteHistoryInfo(Interpreter interpreter,
            IHistoryFilter historyFilter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);
#endif

        bool WriteCustomInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine);
        bool WriteCustomInfo(Interpreter interpreter, DetailFlags detailFlags,
            bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteAllResultInfo(ReturnCode code, Result result, int errorLine,
            Result previousResult, DetailFlags detailFlags, bool newLine);
        bool WriteAllResultInfo(ReturnCode code, Result result, int errorLine,
            Result previousResult, DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteResultInfo(string name, ReturnCode code, Result result,
            int errorLine, DetailFlags detailFlags, bool newLine);
        bool WriteResultInfo(string name, ReturnCode code, Result result,
            int errorLine, DetailFlags detailFlags, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

#if SHELL
        void WriteHeader(Interpreter interpreter, IInteractiveLoopData loopData,
            Result result);

        void WriteFooter(Interpreter interpreter, IInteractiveLoopData loopData,
            Result result);
#endif
    }
}
