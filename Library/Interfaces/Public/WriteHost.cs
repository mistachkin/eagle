/*
 * WriteHost.cs --
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
using Eagle._Containers.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Interfaces.Public
{
    [ObjectId("6232cc30-2233-4248-9b4e-4e8bb71bb66a")]
    public interface IWriteHost : IInteractiveHost
    {
        bool Write(char value, bool newLine);
        bool Write(char value, int count);
        bool Write(char value, int count, bool newLine);
        bool Write(char value, int count, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        bool Write(char value, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool Write(string value, ConsoleColor foregroundColor);
        bool Write(string value, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool Write(string value, bool newLine);
        bool Write(string value, bool newLine, ConsoleColor foregroundColor);
        bool Write(string value, bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteFormat(StringPairList list, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        bool WriteLine(string value, ConsoleColor foregroundColor);
        bool WriteLine(string value, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);
    }
}
