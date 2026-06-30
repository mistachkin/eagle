/*
 * Package.cs --
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

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements a wrapper around an <see cref="IPackage" />
    /// object, forwarding the package interface to the wrapped instance.  It
    /// is used so a package can participate in the interpreter as an
    /// identifiable, token-bearing entity.
    /// </summary>
    [ObjectId("1f1ce60a-9ba6-4a8e-9ed3-f758e37d62d7")]
    internal sealed class Package : Default, IPackage
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public Package()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped <see cref="IPackage" /> object, or null if none has been
        /// set.
        /// </summary>
        internal IPackage package;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped package.
        /// </summary>
        public string Name
        {
            get { return (package != null) ? package.Name : null; }
            set { if (package != null) { package.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped package.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (package != null) ? package.Kind : IdentifierKind.None; }
            set { if (package != null) { package.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped package.
        /// </summary>
        public Guid Id
        {
            get { return (package != null) ? package.Id : Guid.Empty; }
            set { if (package != null) { package.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped package.
        /// </summary>
        public IClientData ClientData
        {
            get { return (package != null) ? package.ClientData : null; }
            set { if (package != null) { package.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped package.
        /// </summary>
        public string Group
        {
            get { return (package != null) ? package.Group : null; }
            set { if (package != null) { package.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped package.
        /// </summary>
        public string Description
        {
            get { return (package != null) ? package.Description : null; }
            set { if (package != null) { package.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// Gets or sets the initialized state of the wrapped package.
        /// </summary>
        public bool Initialized
        {
            get { return (package != null) ? package.Initialized : false; }
            set { if (package != null) { package.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the initialize operation to the wrapped
        /// package.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this package is operating in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, package-specific data supplied when this package was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result; upon failure, it contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (package == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return package.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the terminate operation to the wrapped
        /// package.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this package is operating in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, package-specific data supplied when this package was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result; upon failure, it contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (package == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return package.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackageData Members
        /// <summary>
        /// Gets or sets the index file name of the wrapped package.
        /// </summary>
        public string IndexFileName
        {
            get { return (package != null) ? package.IndexFileName : null; }
            set { if (package != null) { package.IndexFileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the provide file name of the wrapped package.
        /// </summary>
        public string ProvideFileName
        {
            get { return (package != null) ? package.ProvideFileName : null; }
            set { if (package != null) { package.ProvideFileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the package flags of the wrapped package.
        /// </summary>
        public PackageFlags Flags
        {
            get { return (package != null) ? package.Flags : PackageFlags.None; }
            set { if (package != null) { package.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the loaded version of the wrapped package.
        /// </summary>
        public Version Loaded
        {
            get { return (package != null) ? package.Loaded : null; }
            set { if (package != null) { package.Loaded = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the if-needed version dictionary of the wrapped
        /// package.
        /// </summary>
        public VersionStringDictionary IfNeeded
        {
            get { return (package != null) ? package.IfNeeded : null; }
            set { if (package != null) { package.IfNeeded = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the was-needed version of the wrapped package.
        /// </summary>
        public string WasNeeded
        {
            get { return (package != null) ? package.WasNeeded : null; }
            set { if (package != null) { package.WasNeeded = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackage Members
        /// <summary>
        /// This method forwards the select operation to the wrapped package.
        /// </summary>
        /// <param name="preference">
        /// The preference used to select among the available package versions.
        /// </param>
        /// <param name="version">
        /// Upon success, this is set to the selected package version.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Select(
            PackagePreference preference,
            ref Version version,
            ref Result error
            )
        {
            if (package == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return package.Select(preference, ref version, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the load operation to the wrapped package.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this package is operating in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="version">
        /// The package version involved in the operation.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result; upon failure, it contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Load(
            Interpreter interpreter,
            Version version,
            ref Result result
            )
        {
            if (package == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return package.Load(interpreter, version, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the object wrapped by this instance
        /// represents a resource that requires disposal.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the underlying <see cref="IPackage" /> object wrapped
        /// by this instance.
        /// </summary>
        public override object Object
        {
            get { return package; }
            set { package = (IPackage)value; } /* throw */
        }
        #endregion
    }
}
