/*
 * BoxHost.cs --
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
    [ObjectId("cafcf71f-cd71-4d10-9658-8862b231815a")]
    public interface IBoxHost : IInteractiveHost
    {
        bool BeginBox(string name, StringPairList list, IClientData clientData);
        bool EndBox(string name, StringPairList list, IClientData clientData);

        bool WriteBox(string name, string value, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top);
        bool WriteBox(string name, string value, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top);

        bool WriteBox(string name, string value, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        bool WriteBox(string name, string value, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteBox(string name, string value, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor, ConsoleColor boxBackgroundColor);
        bool WriteBox(string name, string value, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor);

        bool WriteBox(string name, StringPairList list, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top);
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top);

        bool WriteBox(string name, StringPairList list, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        bool WriteBox(string name, StringPairList list, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor, ConsoleColor boxBackgroundColor);
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor);
    }
}
