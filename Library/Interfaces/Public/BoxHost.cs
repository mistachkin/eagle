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
    /// <summary>
    /// This interface extends <see cref="IInteractiveHost" /> with the ability
    /// to render boxed output, i.e. text framed within a visible box, to the
    /// host.  It provides methods to begin and end a box as well as a family of
    /// overloads for writing the content of a box, optionally with control over
    /// the minimum width, cursor position, and foreground and background
    /// colors of both the content and the box itself.
    /// </summary>
    [ObjectId("cafcf71f-cd71-4d10-9658-8862b231815a")]
    public interface IBoxHost : IInteractiveHost
    {
        /// <summary>
        /// Begins rendering a box with the specified name and content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the content of the box.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully begun; otherwise, false.
        /// </returns>
        bool BeginBox(string name, StringPairList list, IClientData clientData);
        /// <summary>
        /// Ends rendering a box with the specified name and content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the content of the box.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully ended; otherwise, false.
        /// </returns>
        bool EndBox(string name, StringPairList list, IClientData clientData);

        /// <summary>
        /// Writes the specified string value as the content of a box with the
        /// specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, string value, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top);
        /// <summary>
        /// Writes the specified string value as the content of a box with the
        /// specified name, padding the content to a minimum width.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, string value, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top);

        /// <summary>
        /// Writes the specified string value as the content of a box with the
        /// specified name, using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, string value, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        /// <summary>
        /// Writes the specified string value as the content of a box with the
        /// specified name, padding the content to a minimum width and using the
        /// specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, string value, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// Writes the specified string value as the content of a box with the
        /// specified name, using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, string value, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor, ConsoleColor boxBackgroundColor);
        /// <summary>
        /// Writes the specified string value as the content of a box with the
        /// specified name, padding the content to a minimum width and using the
        /// specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, string value, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor);

        /// <summary>
        /// Writes the specified list of name/value pairs as the content of a
        /// box with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top);
        /// <summary>
        /// Writes the specified list of name/value pairs as the content of a
        /// box with the specified name, padding the content to a minimum width.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top);

        /// <summary>
        /// Writes the specified list of name/value pairs as the content of a
        /// box with the specified name, using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        /// <summary>
        /// Writes the specified list of name/value pairs as the content of a
        /// box with the specified name, padding the content to a minimum width
        /// and using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// Writes the specified list of name/value pairs as the content of a
        /// box with the specified name, using the specified content and box
        /// colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            bool newLine, bool restore, ref int left, ref int top,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor, ConsoleColor boxBackgroundColor);
        /// <summary>
        /// Writes the specified list of name/value pairs as the content of a
        /// box with the specified name, padding the content to a minimum width
        /// and using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        bool WriteBox(string name, StringPairList list, IClientData clientData,
            int minimumLength, bool newLine, bool restore, ref int left,
            ref int top, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor);
    }
}
