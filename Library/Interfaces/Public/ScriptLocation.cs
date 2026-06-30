/*
 * ScriptLocation.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface describes the location of a script, including its file
    /// name, the range of lines it spans, and whether it was obtained via
    /// the <c>source</c> command.  It also supports conversion to a list of
    /// name/value pairs.
    /// </summary>
    [ObjectId("2b42339e-bc7c-40bf-91bc-637e63006842")]
    public interface IScriptLocation
    {
        /// <summary>
        /// Gets or sets the file name associated with this script location, if
        /// any.
        /// </summary>
        string FileName { get; set; }
        /// <summary>
        /// Gets or sets the line number where this script location begins.
        /// </summary>
        int StartLine { get; set; }
        /// <summary>
        /// Gets or sets the line number where this script location ends.
        /// </summary>
        int EndLine { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this script location was
        /// obtained via the <c>source</c> command.
        /// </summary>
        bool ViaSource { get; set; }

        /// <summary>
        /// Returns this script location as a list of name/value pairs.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs representing this script location.
        /// </returns>
        StringPairList ToList();
        /// <summary>
        /// Returns this script location as a list of name/value pairs, optionally
        /// scrubbing potentially sensitive information such as the file name.
        /// </summary>
        /// <param name="scrub">
        /// True to scrub potentially sensitive information from the
        /// result; otherwise, false.
        /// </param>
        /// <returns>
        /// A list of name/value pairs representing this script location.
        /// </returns>
        StringPairList ToList(bool scrub);
    }
}
