/*
 * ArgumentManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the contract for managing command arguments and
    /// options.  It provides methods to validate and extract options from an
    /// argument list, to query and set the script-level arguments (i.e. the
    /// <c>argv</c> variable), and to merge multiple argument lists together.
    /// </summary>
    [ObjectId("ea382119-493f-4a78-bc45-24fd7ed0b4e9")]
    public interface IArgumentManager
    {
        ///////////////////////////////////////////////////////////////////////
        // ARGUMENT & OPTION HANDLING
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The "listCount" argument is only used when processing any
        //       MustBeIndex (e.g. "end-<int>") style options that apply to a
        //       given list.  When there are no index style options, simply
        //       pass zero as the value.
        //
        /// <summary>
        /// Validates the options present in the specified argument list,
        /// checking them against the supplied option definitions.
        /// </summary>
        /// <param name="options">
        /// The dictionary of supported option definitions.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to be checked for options.
        /// </param>
        /// <param name="listCount">
        /// The number of elements in the list that index style options (e.g.
        /// "end-&lt;int&gt;") apply to.  Pass zero when there are no index
        /// style options.
        /// </param>
        /// <param name="startIndex">
        /// The index into the argument list at which to begin processing.
        /// </param>
        /// <param name="stopIndex">
        /// The index into the argument list at which to stop processing.
        /// </param>
        /// <param name="nextIndex">
        /// Upon return, receives the index of the first argument following the
        /// processed options.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CheckOptions(
            OptionDictionary options,
            ArgumentList arguments,
            int listCount,
            int startIndex,
            int stopIndex,
            ref int nextIndex,
            ref Result error
            );

        /// <summary>
        /// Extracts and processes the options present in the specified
        /// argument list, checking them against the supplied option
        /// definitions and storing the resulting values into those
        /// definitions.
        /// </summary>
        /// <param name="options">
        /// The dictionary of supported option definitions, which also receives
        /// the processed option values.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to be processed for options.
        /// </param>
        /// <param name="listCount">
        /// The number of elements in the list that index style options (e.g.
        /// "end-&lt;int&gt;") apply to.  Pass zero when there are no index
        /// style options.
        /// </param>
        /// <param name="startIndex">
        /// The index into the argument list at which to begin processing.
        /// </param>
        /// <param name="stopIndex">
        /// The index into the argument list at which to stop processing.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat an unrecognized option as an error; otherwise,
        /// processing stops at the first non-option argument.
        /// </param>
        /// <param name="nextIndex">
        /// Upon return, receives the index of the first argument following the
        /// processed options.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetOptions(
            OptionDictionary options,
            ArgumentList arguments,
            int listCount,
            int startIndex,
            int stopIndex,
            bool strict,
            ref int nextIndex,
            ref Result error
            );

        /// <summary>
        /// Extracts and processes the options present in the specified
        /// argument list on behalf of the specified identifier, checking them
        /// against the supplied option definitions and storing the resulting
        /// values into those definitions.
        /// </summary>
        /// <param name="identifier">
        /// The identifier (e.g. the command) on whose behalf the options are
        /// being processed.
        /// </param>
        /// <param name="options">
        /// The dictionary of supported option definitions, which also receives
        /// the processed option values.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to be processed for options.
        /// </param>
        /// <param name="argumentIndex">
        /// On input, the index into the argument list at which to begin
        /// processing; upon return, receives the index of the first argument
        /// following the processed options.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetOptions(
            IIdentifier identifier,
            OptionDictionary options,
            ArgumentList arguments,
            ref int argumentIndex,
            ref Result error
            );

        //
        // NOTE: Get, set, or reset the arguments (i.e. the "argv" variable)
        //       for a script to use.
        //
        /// <summary>
        /// Gets the script-level arguments (i.e. the <c>argv</c> variable)
        /// currently in effect.
        /// </summary>
        /// <param name="arguments">
        /// Upon success, receives the list of script-level arguments.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetArguments(
            ref IList<string> arguments,
            ref Result error
            );

        /// <summary>
        /// Gets the script-level arguments (i.e. the <c>argv</c> variable)
        /// currently in effect.
        /// </summary>
        /// <param name="arguments">
        /// Upon success, receives the list of script-level arguments.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to return an error when the arguments cannot be obtained;
        /// otherwise, a missing or invalid value is tolerated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetArguments(
            ref IList<string> arguments,
            bool failOnError,
            ref Result error
            );

        /// <summary>
        /// Sets the script-level arguments (i.e. the <c>argv</c> variable) to
        /// the specified list of values.
        /// </summary>
        /// <param name="arguments">
        /// The list of script-level arguments to set.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetArguments(
            IList<string> arguments,
            ref Result error
            );

        /// <summary>
        /// Sets the script-level arguments (i.e. the <c>argv</c> variable) to
        /// the specified list of values.
        /// </summary>
        /// <param name="arguments">
        /// The list of script-level arguments to set.  This parameter may be
        /// null only when <paramref name="failOnNull" /> is false.
        /// </param>
        /// <param name="failOnNull">
        /// Non-zero to return an error when <paramref name="arguments" /> is
        /// null; otherwise, a null value is tolerated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetArguments(
            IList<string> arguments,
            bool failOnNull,
            ref Result error
            );

        //
        // NOTE: Merge two sets of arguments, including those that represent
        //       valid options, and return the resulting list.
        //
        /// <summary>
        /// Merges two sets of arguments, including those that represent valid
        /// options, and returns the resulting list.
        /// </summary>
        /// <param name="options">
        /// The dictionary of supported option definitions used while merging.
        /// </param>
        /// <param name="arguments1">
        /// The first list of arguments to merge.
        /// </param>
        /// <param name="arguments2">
        /// The second list of arguments to merge.
        /// </param>
        /// <param name="startIndex1">
        /// The index into <paramref name="arguments1" /> at which to begin
        /// merging.
        /// </param>
        /// <param name="startIndex2">
        /// The index into <paramref name="arguments2" /> at which to begin
        /// merging.
        /// </param>
        /// <param name="skipFirst1">
        /// Non-zero to skip the first element of
        /// <paramref name="arguments1" /> while merging.
        /// </param>
        /// <param name="skipFirst2">
        /// Non-zero to skip the first element of
        /// <paramref name="arguments2" /> while merging.
        /// </param>
        /// <param name="useRemaining1">
        /// Non-zero to include any remaining arguments from
        /// <paramref name="arguments1" /> in the merged result.
        /// </param>
        /// <param name="arguments">
        /// Upon success, receives the merged list of arguments.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MergeArguments(
            OptionDictionary options,
            ArgumentList arguments1,
            ArgumentList arguments2,
            int startIndex1,
            int startIndex2,
            bool skipFirst1,
            bool skipFirst2,
            bool useRemaining1,
            ref ArgumentList arguments,
            ref Result error
            );
    }
}
