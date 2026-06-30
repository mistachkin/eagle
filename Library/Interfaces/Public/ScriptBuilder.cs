/*
 * ScriptBuilder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the script builder, which
    /// incrementally assembles a script from text fragments, argument lists,
    /// existing scripts, and other script builders, and then produces the
    /// combined result as a string or as an <see cref="IScript" />.  It
    /// extends <see cref="IIdentifier" />.
    /// </summary>
    [ObjectId("14e75abb-1d52-4c3d-9d65-299f1665a475")]
    public interface IScriptBuilder : IIdentifier
    {
        /// <summary>
        /// Gets the number of fragments that have been added to this script
        /// builder.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Removes all previously added fragments from this script builder.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Clear(ref Result error);
        /// <summary>
        /// Appends the specified text fragment to this script builder.
        /// </summary>
        /// <param name="text">
        /// The text fragment to append.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Add(string text, ref Result error);
        /// <summary>
        /// Appends the specified arguments, as a single command, to this script
        /// builder.
        /// </summary>
        /// <param name="arguments">
        /// The list of arguments to append as a command.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Add(IStringList arguments, ref Result error);
        /// <summary>
        /// Appends the specified script to this script builder.
        /// </summary>
        /// <param name="script">
        /// The script to append.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Add(IScript script, ref Result error);
        /// <summary>
        /// Appends the fragments of the specified script builder to this script
        /// builder.
        /// </summary>
        /// <param name="builder">
        /// The script builder whose fragments are to be appended.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Add(IScriptBuilder builder, ref Result error);

        /// <summary>
        /// Returns the assembled script as a string.
        /// </summary>
        /// <param name="nested">
        /// True if the result will be embedded within another script and
        /// should be made suitable for nesting; otherwise, false.
        /// </param>
        /// <returns>
        /// The assembled script as a string.
        /// </returns>
        string GetString(bool nested);
        /// <summary>
        /// Returns the assembled script as an <see cref="IScript" /> instance.
        /// </summary>
        /// <param name="nested">
        /// True if the result will be embedded within another script and
        /// should be made suitable for nesting; otherwise, false.
        /// </param>
        /// <returns>
        /// The assembled script as an <see cref="IScript" /> instance.
        /// </returns>
        IScript GetScript(bool nested);
    }
}
