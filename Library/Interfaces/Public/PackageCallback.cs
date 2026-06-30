/*
 * PackageCallback.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that provide a fallback
    /// handler used to satisfy a package request when the package cannot
    /// otherwise be found by an Eagle interpreter.
    /// </summary>
    [ObjectId("6369a999-b50c-42c5-909d-caf222bd2148")]
    public interface IPackageCallback
    {
        /// <summary>
        /// Called to attempt to satisfy a package request that could not be
        /// fulfilled through the normal package resolution mechanism.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this request.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="name">
        /// The name of the package being requested.
        /// </param>
        /// <param name="version">
        /// The version of the package being requested, if any.
        /// </param>
        /// <param name="text">
        /// The script or other text associated with this package request, if
        /// any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how this package request is processed.
        /// </param>
        /// <param name="exact">
        /// Non-zero if the requested version must match exactly; otherwise,
        /// zero.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of satisfying the
        /// request.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode PackageFallback(
            Interpreter interpreter, // TODO: Change to use the IInterpreter type.
            string name,
            Version version,
            string text,
            PackageFlags flags,
            bool exact,
            ref Result result
        );
    }
}
