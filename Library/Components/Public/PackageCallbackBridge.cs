/*
 * PackageCallbackBridge.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class provides a marshal-by-reference bridge that adapts an
    /// <see cref="IPackageCallback" /> implementation, allowing the package
    /// fallback callback to be invoked across application domain boundaries.
    /// </summary>
    [ObjectId("2de30e01-bef2-44e6-a18c-b44ee35e0191")]
    public sealed class PackageCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped package callback that this bridge forwards calls to.
        /// </summary>
        private IPackageCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified package callback.
        /// </summary>
        /// <param name="callback">
        /// The package callback to wrap.
        /// </param>
        private PackageCallbackBridge(
            IPackageCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards a package fallback request to the wrapped
        /// package callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the package fallback.  This parameter
        /// may be null.
        /// </param>
        /// <param name="name">
        /// The name of the package being requested.  This parameter may be
        /// null.
        /// </param>
        /// <param name="version">
        /// The version of the package being requested.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The script text associated with the package request.  This parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the package fallback.
        /// </param>
        /// <param name="exact">
        /// Non-zero if the requested package version must match exactly.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the result;
        /// upon failure, it will be modified to contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode PackageFallbackCallback(
            Interpreter interpreter,
            string name,
            Version version,
            string text,
            PackageFlags flags,
            bool exact,
            ref Result result
            )
        {
            if (callback == null)
            {
                result = "invalid package callback";
                return ReturnCode.Error;
            }

            return callback.PackageFallback(
                interpreter, name, version, text, flags, exact, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new bridge that wraps the specified package
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The package callback to wrap.  This parameter may not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
        public static PackageCallbackBridge Create(
            IPackageCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid package callback";
                return null;
            }

            return new PackageCallbackBridge(callback);
        }
        #endregion
    }
}
