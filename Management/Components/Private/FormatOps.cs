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
    [ObjectId("4142d1fc-c0c4-41ed-8940-b99588c420ae")]
    internal static class FormatOps
    {
        private const string UnknownType = "Unknown";

        ///////////////////////////////////////////////////////////////////////

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

