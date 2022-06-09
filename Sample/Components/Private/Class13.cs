/*
 * Class13.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Sample
{
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("37bd41bf-c1bf-446d-8265-3df7ed66e36b")]
    internal sealed class Class13
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        : ScriptMarshalByRefObject, IPackageCallback
#endif
    {
        #region Private Constants
        //
        // NOTE: This is the base name of the "stub" package that may be
        //       provided by this class.  This value will be prefixed with
        //       the name of this class followed by a delimiter.
        //
        private static readonly string StubPackageSuffix = "Stub";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the base name of the "embedded" package that may be
        //       provided by this class.  This value will be prefixed with
        //       the name of this class followed by a delimiter.
        //
        private static readonly string EmbeddedPackageSuffix = "Embedded";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the package version to provide when the caller did
        //       not specify one.
        //
        private static readonly Version ProvidePackageVersion =
            new Version(1, 0);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This script is used to provide a package to the interpreter.
        //       This will be passed to the String.Format method with the
        //       package name and version values as the only replaceable
        //       parameters.
        //
        private static readonly string ProvidePackageScript =
            "package provide {0} {1};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the name of the embedded resource that contains the
        //       sample package script file.
        //
        private static readonly string EmbeddedScriptFileName =
            "sample.eagle";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the script to be evaluated in order to provide the
        //       extra packages to the interpreter.  This will be passed to
        //       the String.Format method with the extra package directory
        //       value as the only replaceable parameter.
        //
        private static readonly string ScanPackagesScript =
            "package scan -host -normal -refresh -- {0};";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the configured plugin instance.  It will be used to
        //       query for an embedded resource string containing the sample
        //       package script.
        //
        private IPlugin plugin;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the configured "stub" package name.  It will be used
        //       by the callback to match against the package name.
        //
        private string stubPackageName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the configured "embedded" package name.  It will be
        //       used by the callback to match against the package name.
        //
        private string embeddedPackageName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This field is used to hold the fully qualified name of an
        //       extra directory to search for packages.  Generally, this
        //       directory must contain one or more package index files in
        //       order to be useful.
        //
        private string extraPackageDirectory;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Class13(
            IPlugin plugin,              /* in */
            string stubPackageName,      /* in */
            string embeddedPackageName,  /* in */
            string extraPackageDirectory /* in */
            )
        {
            this.plugin = plugin;
            this.stubPackageName = stubPackageName;
            this.embeddedPackageName = embeddedPackageName;
            this.extraPackageDirectory = extraPackageDirectory;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static string GetDefaultPackageName(
            bool stub /* in */
            )
        {
            return String.Format(
                "{0}{1}{2}", typeof(Class13).Name, Type.Delimiter,
                stub ? StubPackageSuffix : EmbeddedPackageSuffix);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackageCallback Members
        public ReturnCode PackageFallback(
            Interpreter interpreter, /* in */
            string name,             /* in */
            Version version,         /* in */
            string text,             /* in: NOT USED */
            PackageFlags flags,      /* in: NOT USED */
            bool exact,              /* in: NOT USED */
            ref Result result        /* in, out */
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                result = new ResultList(result, "invalid interpreter");
                return ReturnCode.Error;
            }

            //
            // NOTE: If the requested package is the "stub" package, simply
            //       provide the requested package and version.
            //
            Result localResult = null; /* REUSED */

            if ((stubPackageName != null) && Utility.SystemStringEquals(
                    name, stubPackageName))
            {
                //
                // NOTE: Evaluate the script to provide exactly the specified
                //       package and version.  This script should not require
                //       full trust because it only uses the [package provide]
                //       sub-command, which is considered "safe".
                //
                if (version == null)
                    version = ProvidePackageVersion;

                localResult = null;

                if (interpreter.EvaluateScript(
                        String.Format(ProvidePackageScript, name, version),
                        ref localResult) == ReturnCode.Ok)
                {
                    result = localResult;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = new ResultList(result, localResult);
                    return ReturnCode.Error;
                }
            }

            //
            // NOTE: If the requested package is the "embedded" package, try
            //       to query the configured plugin instance for the resource
            //       string.  Then, evaluate that string as a script, which
            //       may provide one or more packages.  Upon success, always
            //       provide the requested package and version.
            //
            if ((embeddedPackageName != null) && Utility.SystemStringEquals(
                    name, embeddedPackageName))
            {
                if (plugin == null)
                {
                    result = new ResultList(result, "invalid plugin");
                    return ReturnCode.Error;
                }

                string resourceValue;

                localResult = null;

                resourceValue = plugin.GetString(interpreter,
                    EmbeddedScriptFileName, interpreter.CultureInfo,
                    ref localResult);

                if (resourceValue == null)
                {
                    result = new ResultList(result, localResult);
                    return ReturnCode.Error;
                }

                //
                // NOTE: Ok, we successfully queried the script text from the
                //       plugin.  Normalize the line-endings (to Unix style),
                //       so it can be evaluated by the script engine.
                //
                resourceValue = Utility.NormalizeLineEndings(resourceValue);

                //
                // NOTE: Evaluate the script text obtained from the plugin.
                //       Generally, the script will only declare procedures
                //       and/or provide packages; therefore, there should be
                //       no reason to evaluate it with full trust.
                //
                localResult = null;

                if (interpreter.EvaluateScript(
                        resourceValue, ref localResult) != ReturnCode.Ok)
                {
                    result = new ResultList(result, localResult);
                    return ReturnCode.Error;
                }

                //
                // NOTE: Evaluate the script to provide exactly the specified
                //       package and version.  This script should not require
                //       full trust because it only uses the [package provide]
                //       sub-command, which is considered "safe".
                //
                if (version == null)
                    version = ProvidePackageVersion;

                localResult = null;

                if (interpreter.EvaluateScript(
                        String.Format(ProvidePackageScript, name,
                        version), ref localResult) != ReturnCode.Ok)
                {
                    result = new ResultList(result, localResult);
                    return ReturnCode.Error;
                }

                result = localResult;
                return ReturnCode.Ok;
            }

            //
            // NOTE: If there is an extra package directory configured, use
            //       it with the [package scan] command now.  This may cause
            //       one or more package index files to be evaluated.  Since
            //       this script must use an "unsafe" [package] sub-command,
            //       make sure to evaluate it with full trust.
            //
            if (extraPackageDirectory == null)
            {
                result = new ResultList(
                    result, "invalid extra package directory");

                return ReturnCode.Error;
            }

            localResult = null;

            if (interpreter.EvaluateTrustedScript(
                    String.Format(ScanPackagesScript, Parser.Quote(
                    extraPackageDirectory)), TrustFlags.MaybeMarkTrusted,
                    ref localResult) == ReturnCode.Ok)
            {
                result = localResult;
                return ReturnCode.Ok;
            }
            else
            {
                result = new ResultList(result, localResult);
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Class13));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    plugin = null; /* NOT OWNED: DO NOT DISPOSE */
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Class13()
        {
            Dispose(false);
        }
        #endregion
    }
}
