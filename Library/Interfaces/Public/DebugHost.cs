/*
 * DebugHost.cs --
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

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Interfaces.Public
{
    [ObjectId("56c86c61-db4e-43ba-8bc9-938247ec95e6")]
    public interface IDebugHost : IInteractiveHost
    {
        IHost Clone();
        IHost Clone(Interpreter interpreter); // TODO: Change this to use the IInterpreter type.

        HostTestFlags GetTestFlags();

        ReturnCode Cancel(bool force, ref Result error);
        ReturnCode Exit(bool force, ref Result error);

        bool WriteDebugLine();
        bool WriteDebugLine(string value);

        bool WriteDebug(char value);
        bool WriteDebug(char value, bool newLine);
        bool WriteDebug(char value, int count, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        bool WriteDebug(string value);
        bool WriteDebug(string value, bool newLine);
        bool WriteDebug(string value, bool newLine,
            ConsoleColor foregroundColor);
        bool WriteDebug(string value, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteErrorLine();
        bool WriteErrorLine(string value);

        bool WriteError(char value);
        bool WriteError(char value, bool newLine);
        bool WriteError(char value, int count, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        bool WriteError(string value);
        bool WriteError(string value, bool newLine);
        bool WriteError(string value, bool newLine,
            ConsoleColor foregroundColor);
        bool WriteError(string value, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteResult(ReturnCode code, Result result, bool newLine);
        bool WriteResult(ReturnCode code, Result result, bool raw,
            bool newLine);

        bool WriteResult(ReturnCode code, Result result, int errorLine,
            bool newLine);
        bool WriteResult(ReturnCode code, Result result, int errorLine,
            bool raw, bool newLine);

        bool WriteResult(string prefix, ReturnCode code, Result result,
            int errorLine, bool newLine);
        bool WriteResult(string prefix, ReturnCode code, Result result,
            int errorLine, bool raw, bool newLine);
    }
}
