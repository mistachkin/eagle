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
    /// <summary>
    /// This interface is implemented by interactive hosts that support
    /// colorized output.  It extends <see cref="IInteractiveHost" /> with the
    /// ability to query, adjust, set, and reset the foreground and background
    /// colors used when writing to the host, including support for named
    /// theme colors.
    /// </summary>
    [ObjectId("1532fb48-6373-450a-b1d8-d60b3295ff64")]
    public interface IColorHost : IInteractiveHost
    {
        /// <summary>
        /// Gets or sets a value indicating whether colorized output is
        /// disabled for this host.  When true, color operations have no
        /// visible effect.
        /// </summary>
        bool NoColor { get; set; }

        /// <summary>
        /// Resets the host foreground and background colors to their default
        /// values.
        /// </summary>
        /// <returns>
        /// True if the colors were reset; otherwise, false.
        /// </returns>
        bool ResetColors();
        /// <summary>
        /// Gets the current foreground and background colors of the host.
        /// </summary>
        /// <param name="foregroundColor">
        /// Upon success, receives the current foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the current background color.
        /// </param>
        /// <returns>
        /// True if the colors were obtained; otherwise, false.
        /// </returns>
        bool GetColors(ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor);
        /// <summary>
        /// Adjusts the specified foreground and background colors as
        /// necessary so that they are suitable for use by the host (e.g. to
        /// avoid an unreadable combination).
        /// </summary>
        /// <param name="foregroundColor">
        /// On input, the desired foreground color; on output, the adjusted
        /// foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the desired background color; on output, the adjusted
        /// background color.
        /// </param>
        /// <returns>
        /// True if the colors were adjusted; otherwise, false.
        /// </returns>
        bool AdjustColors(ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor);
        /// <summary>
        /// Sets the host foreground color.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to set.
        /// </param>
        /// <returns>
        /// True if the foreground color was set; otherwise, false.
        /// </returns>
        bool SetForegroundColor(ConsoleColor foregroundColor);
        /// <summary>
        /// Sets the host background color.
        /// </summary>
        /// <param name="backgroundColor">
        /// The background color to set.
        /// </param>
        /// <returns>
        /// True if the background color was set; otherwise, false.
        /// </returns>
        bool SetBackgroundColor(ConsoleColor backgroundColor);
        /// <summary>
        /// Sets the host foreground and/or background colors.
        /// </summary>
        /// <param name="foreground">
        /// Non-zero to set the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to set the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to set, used only when
        /// <paramref name="foreground" /> is true.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to set, used only when
        /// <paramref name="background" /> is true.
        /// </param>
        /// <returns>
        /// True if the requested colors were set; otherwise, false.
        /// </returns>
        bool SetColors(bool foreground, bool background,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// Gets the foreground and/or background colors associated with a
        /// named entry within a color theme.
        /// </summary>
        /// <param name="theme">
        /// The name of the color theme to query, or null to use the active
        /// theme.
        /// </param>
        /// <param name="name">
        /// The name of the color entry within the theme.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to obtain the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to obtain the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// Upon success, receives the foreground color, when requested.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the background color, when requested.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetColors(string theme, string name, bool foreground,
            bool background, ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor, ref Result error);
        /// <summary>
        /// Sets the foreground and/or background colors associated with a
        /// named entry within a color theme.
        /// </summary>
        /// <param name="theme">
        /// The name of the color theme to modify, or null to use the active
        /// theme.
        /// </param>
        /// <param name="name">
        /// The name of the color entry within the theme.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to set the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to set the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to set, used only when
        /// <paramref name="foreground" /> is true.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to set, used only when
        /// <paramref name="background" /> is true.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetColors(string theme, string name, bool foreground,
            bool background, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor, ref Result error);
    }
}
