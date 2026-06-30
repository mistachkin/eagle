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
    /// <summary>
    /// This class is a sample package callback implementation that can provide
    /// a "stub" package, an "embedded" package (sourced from a plugin resource
    /// string), and/or scan an extra directory for packages, on demand, when
    /// the interpreter is unable to locate a requested package.
    /// </summary>
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
        /// <summary>
        /// The base name of the "stub" package that may be provided by this
        /// class; it is prefixed with the name of this class followed by a
        /// delimiter to form the full package name.
        /// </summary>
        //
        // NOTE: This is the base name of the "stub" package that may be
        //       provided by this class.  This value will be prefixed with
        //       the name of this class followed by a delimiter.
        //
        private static readonly string StubPackageSuffix = "Stub";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base name of the "embedded" package that may be provided by
        /// this class; it is prefixed with the name of this class followed by
        /// a delimiter to form the full package name.
        /// </summary>
        //
        // NOTE: This is the base name of the "embedded" package that may be
        //       provided by this class.  This value will be prefixed with
        //       the name of this class followed by a delimiter.
        //
        private static readonly string EmbeddedPackageSuffix = "Embedded";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The package version to provide when the caller did not specify one.
        /// </summary>
        //
        // NOTE: This is the package version to provide when the caller did
        //       not specify one.
        //
        private static readonly Version ProvidePackageVersion =
            new Version(1, 0);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script template used to provide a package to the interpreter;
        /// it is passed to the <c>String.Format</c> method with the package
        /// name and version values as the only replaceable parameters.
        /// </summary>
        //
        // NOTE: This script is used to provide a package to the interpreter.
        //       This will be passed to the String.Format method with the
        //       package name and version values as the only replaceable
        //       parameters.
        //
        private static readonly string ProvidePackageScript =
            "package provide {0} {1};";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the embedded resource that contains the sample package
        /// script file.
        /// </summary>
        //
        // NOTE: This is the name of the embedded resource that contains the
        //       sample package script file.
        //
        private static readonly string EmbeddedScriptFileName =
            "sample.eagle";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script template evaluated in order to scan an extra directory
        /// for packages; it is passed to the <c>String.Format</c> method with
        /// the extra package directory value as the only replaceable parameter.
        /// </summary>
        //
        // NOTE: This is the script to be evaluated in order to provide the
        //       extra packages to the interpreter.  This will be passed to
        //       the String.Format method with the extra package directory
        //       value as the only replaceable parameter.
        //
        private static readonly string ScanPackagesScript =
            "package scan -host -normal -primary -tagged -refresh -- {0};";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The configured plugin instance used to query for an embedded
        /// resource string containing the sample package script.
        /// </summary>
        //
        // NOTE: This is the configured plugin instance.  It will be used to
        //       query for an embedded resource string containing the sample
        //       package script.
        //
        private IPlugin plugin;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configured "stub" package name used by the callback to match
        /// against the requested package name.
        /// </summary>
        //
        // NOTE: This is the configured "stub" package name.  It will be used
        //       by the callback to match against the package name.
        //
        private string stubPackageName;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configured "embedded" package name used by the callback to
        /// match against the requested package name.
        /// </summary>
        //
        // NOTE: This is the configured "embedded" package name.  It will be
        //       used by the callback to match against the package name.
        //
        private string embeddedPackageName;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The fully qualified name of an extra directory to search for
        /// packages; generally, this directory must contain one or more
        /// package index files in order to be useful.
        /// </summary>
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
        /// <summary>
        /// Constructs a new instance of this package callback.
        /// </summary>
        /// <param name="plugin">
        /// The plugin instance used to query for the embedded sample package
        /// script resource string.
        /// </param>
        /// <param name="stubPackageName">
        /// The name of the "stub" package this callback should provide.
        /// </param>
        /// <param name="embeddedPackageName">
        /// The name of the "embedded" package this callback should provide.
        /// </param>
        /// <param name="extraPackageDirectory">
        /// The fully qualified name of an extra directory to scan for packages.
        /// </param>
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
        /// <summary>
        /// This method builds the default package name for either the "stub"
        /// or "embedded" package, based on the name of this class.
        /// </summary>
        /// <param name="stub">
        /// Non-zero to build the name for the "stub" package; zero to build
        /// the name for the "embedded" package.
        /// </param>
        /// <returns>
        /// The default package name.
        /// </returns>
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
        /// <summary>
        /// This method is invoked when the interpreter is unable to locate a
        /// requested package.  It attempts to provide the "stub" package, the
        /// "embedded" package (sourced from a plugin resource), or to scan the
        /// configured extra package directory, depending on the requested
        /// package name and configuration.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter requesting the package.
        /// </param>
        /// <param name="name">
        /// The name of the package being requested.
        /// </param>
        /// <param name="version">
        /// The version of the package being requested, or null if any version
        /// is acceptable.
        /// </param>
        /// <param name="text">
        /// Additional package text; this parameter is not used.
        /// </param>
        /// <param name="flags">
        /// The flags associated with the package request; this parameter is
        /// not used.
        /// </param>
        /// <param name="exact">
        /// Non-zero if an exact version match is required; this parameter is
        /// not used.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of providing the package; upon
        /// failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// Stores a value indicating whether this package callback has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this package callback has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this package callback has been disposed and the engine
        /// is configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Class13));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this package callback.
        /// It implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// This method releases all resources held by this package callback and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this package callback, releasing any resources that were
        /// not released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Class13()
        {
            Dispose(false);
        }
        #endregion
    }
}
