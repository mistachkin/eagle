/*
 * FormatOps.cs --
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

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods used to format the various
    /// human-readable messages emitted by the management (PowerShell cmdlet)
    /// components.
    /// </summary>
    [ObjectId("4142d1fc-c0c4-41ed-8940-b99588c420ae")]
    internal static class FormatOps
    {
        /// <summary>
        /// The placeholder text used in a formatted message when an associated
        /// type is not available (null).
        /// </summary>
        private const string UnknownType = "Unknown";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a message describing the object that a cmdlet is
        /// currently processing.
        /// </summary>
        /// <param name="type">
        /// The type of the cmdlet that is performing the processing.  This
        /// parameter may be null, in which case a placeholder is used.
        /// </param>
        /// <param name="noun">
        /// A short noun describing the kind of object being processed.
        /// </param>
        /// <param name="text">
        /// The textual representation of the object being processed.
        /// </param>
        /// <returns>
        /// The formatted message string.
        /// </returns>
        public static string ScriptMessage(
            Type type,
            string noun,
            string text
            )
        {
            return String.Format(
                "The \"{0}\" cmdlet is processing the {1}: {{{2}}}.",
                (type != null) ? type.ToString() : UnknownType,
                noun,
                text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a message describing the value of a named set of
        /// flags.
        /// </summary>
        /// <param name="prefix">
        /// The text to place at the start of the formatted message.
        /// </param>
        /// <param name="name">
        /// The name of the flags being described.
        /// </param>
        /// <param name="value">
        /// The textual representation of the flags value.
        /// </param>
        /// <returns>
        /// The formatted message string.
        /// </returns>
        public static string FlagsMessage(
            string prefix,
            string name,
            string value
            )
        {
            return String.Format(
                "{0}{1} are \"{2}\".",
                prefix,
                name,
                value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a message indicating whether a named feature is
        /// enabled or disabled.
        /// </summary>
        /// <param name="name">
        /// The name of the feature being described.
        /// </param>
        /// <param name="plural">
        /// Non-zero if the name is grammatically plural (selecting "are" rather
        /// than "is" in the formatted message).
        /// </param>
        /// <param name="value">
        /// Non-zero if the feature is enabled; otherwise, zero.
        /// </param>
        /// <returns>
        /// The formatted message string.
        /// </returns>
        public static string EnabledMessage(
            string name,
            bool plural,
            bool value
            )
        {
            return String.Format(
                "{0} {1} {2}.",
                name,
                plural ? "are" : "is",
                value ? "enabled" : "disabled");
        }
    }
}
