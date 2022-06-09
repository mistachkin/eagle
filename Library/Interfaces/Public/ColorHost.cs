/*
 * ColorHost.cs --
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
    [ObjectId("1532fb48-6373-450a-b1d8-d60b3295ff64")]
    public interface IColorHost : IInteractiveHost
    {
        bool NoColor { get; set; }

        bool ResetColors();
        bool GetColors(ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor);
        bool AdjustColors(ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor);
        bool SetForegroundColor(ConsoleColor foregroundColor);
        bool SetBackgroundColor(ConsoleColor backgroundColor);
        bool SetColors(bool foreground, bool background,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        ReturnCode GetColors(string theme, string name, bool foreground,
            bool background, ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor, ref Result error);
        ReturnCode SetColors(string theme, string name, bool foreground,
            bool background, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, ref Result error);
    }
}
