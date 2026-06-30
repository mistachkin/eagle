/*
 * SnippetManager.cs --
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
    /// This interface is implemented by entities that manage a collection of
    /// named script snippets.  It defines methods to query, list, evaluate,
    /// add, and remove snippets, including bulk operations that load snippets
    /// from a path or from certificates.
    /// </summary>
    [ObjectId("b4a2f0a6-2d23-47be-8bec-9c2d89a82969")]
    public interface ISnippetManager
    {
        /// <summary>
        /// Determines whether a snippet with the specified name is present.
        /// </summary>
        /// <param name="name">
        /// The name of the snippet to check for.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.  This parameter
        /// is not used.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> that control how the snippet is
        /// looked up.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the snippet is present; otherwise, a
        /// non-Ok value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode HaveSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in: NOT USED */
            LookupFlags lookupFlags,   /* in */
            ref Result error           /* out */
            );

        /// <summary>
        /// Gets the snippet with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the snippet to get.  This parameter should not be null.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> that control how the snippet is
        /// looked up.
        /// </param>
        /// <param name="snippet">
        /// Upon success, this is set to the requested snippet.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref ISnippet snippet,      /* out */
            ref Result error           /* out */
            );

        /// <summary>
        /// Lists the names of the snippets that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which snippet names are included, or null
        /// to include all snippets.  This parameter is optional.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> for this operation.  This parameter
        /// is not used.
        /// </param>
        /// <param name="names">
        /// Upon success, this is set to the matching snippet names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListSnippets(
            string pattern,                /* in: OPTIONAL */
            bool noCase,                   /* in */
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in: NOT USED */
            ref IEnumerable<string> names, /* in, out */
            ref Result error               /* out */
            );

        /// <summary>
        /// Evaluates the snippet with the specified name as a script.
        /// </summary>
        /// <param name="name">
        /// The name of the snippet to evaluate.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> that control how the snippet is
        /// looked up.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// snippet.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref Result result          /* out */
            );

        /// <summary>
        /// Evaluates the snippet with the specified name as a script, also
        /// reporting the line number where an error occurred, if any.
        /// </summary>
        /// <param name="name">
        /// The name of the snippet to evaluate.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> that control how the snippet is
        /// looked up.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// snippet.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this is set to the line number within the snippet
        /// where the error occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref Result result,         /* out */
            ref int errorLine          /* out */
            );

        /// <summary>
        /// Removes all snippets, optionally accumulating the number removed.
        /// </summary>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> for this operation.  This parameter
        /// is not used.
        /// </param>
        /// <param name="count">
        /// On input, the initial count; upon return, this is increased by the
        /// number of snippets that were removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ClearSnippets(
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in: NOT USED */
            ref int count,             /* in, out */
            ref Result error           /* out */
            );

        /// <summary>
        /// Adds a new snippet with the specified text.
        /// </summary>
        /// <param name="text">
        /// The script text of the snippet to add.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> for this operation.
        /// </param>
        /// <param name="name">
        /// Upon success, this is set to the name assigned to the new snippet.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddSnippet(
            string text,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref string name,           /* out */
            ref Result error           /* out */
            );

        /// <summary>
        /// Removes the snippet with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the snippet to remove.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> that control how the snippet is
        /// looked up.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveSnippet(
            string name,               /* in */
            SnippetFlags snippetFlags, /* in */
            LookupFlags lookupFlags,   /* in */
            ref Result error           /* out */
            );

        /// <summary>
        /// Adds snippets loaded from the files under the specified path that
        /// match the specified pattern.
        /// </summary>
        /// <param name="path">
        /// The path of the directory to load snippet files from.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which files are loaded, or null to load
        /// all files.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> for this operation.
        /// </param>
        /// <param name="names">
        /// On input, an existing collection of names; upon return, this also
        /// contains the names of the snippets that were added.
        /// </param>
        /// <param name="errors">
        /// On input, an existing list of errors; upon return, this also
        /// contains any errors encountered while adding snippets.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode AddSnippets(
            string path,                   /* in */
            string pattern,                /* in */
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in */
            ref IEnumerable<string> names, /* in, out */
            ref ResultList errors          /* in, out */
            );

        /// <summary>
        /// Adds snippets derived from the certificates found under the
        /// specified path.
        /// </summary>
        /// <param name="path">
        /// The path of the directory to load certificates from.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> for this operation.
        /// </param>
        /// <param name="names">
        /// On input, an existing collection of names; upon return, this also
        /// contains the names of the snippets that were added.
        /// </param>
        /// <param name="errors">
        /// On input, an existing list of errors; upon return, this also
        /// contains any errors encountered while adding snippets.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode AddSnippetsForCertificates(
            string path,                   /* in */
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in */
            ref IEnumerable<string> names, /* in, out */
            ref ResultList errors          /* in, out */
            );

        /// <summary>
        /// Removes multiple snippets, reporting the names that were removed.
        /// </summary>
        /// <param name="snippetFlags">
        /// The <see cref="SnippetFlags" /> for this operation.
        /// </param>
        /// <param name="lookupFlags">
        /// The <see cref="LookupFlags" /> for this operation.
        /// </param>
        /// <param name="names">
        /// On input, an existing collection of names; upon return, this also
        /// contains the names of the snippets that were removed.
        /// </param>
        /// <param name="errors">
        /// On input, an existing list of errors; upon return, this also
        /// contains any errors encountered while removing snippets.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode RemoveSnippets(
            SnippetFlags snippetFlags,     /* in */
            LookupFlags lookupFlags,       /* in */
            ref IEnumerable<string> names, /* in, out */
            ref ResultList errors          /* in, out */
            );
    }
}
