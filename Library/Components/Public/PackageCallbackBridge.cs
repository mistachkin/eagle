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
    [ObjectId("2de30e01-bef2-44e6-a18c-b44ee35e0191")]
    public sealed class PackageCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IPackageCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private PackageCallbackBridge(
            IPackageCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
