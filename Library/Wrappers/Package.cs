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
    [ObjectId("1f1ce60a-9ba6-4a8e-9ed3-f758e37d62d7")]
    internal sealed class Package : Default, IPackage
    {
        #region Public Constructors
        public Package(
            long token,
            IPackage package
            )
            : base(token)
        {
            this.package = package;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IPackage package;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (package != null) ? package.Name : null; }
            set { if (package != null) { package.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (package != null) ? package.Kind : IdentifierKind.None; }
            set { if (package != null) { package.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (package != null) ? package.Id : Guid.Empty; }
            set { if (package != null) { package.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (package != null) ? package.Group : null; }
            set { if (package != null) { package.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (package != null) ? package.Description : null; }
            set { if (package != null) { package.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (package != null) ? package.ClientData : null; }
            set { if (package != null) { package.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public bool Initialized
        {
            get { return (package != null) ? package.Initialized : false; }
            set { if (package != null) { package.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (package != null)
                return package.Initialize(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (package != null)
                return package.Terminate(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackageData Members
        public string IndexFileName
        {
            get { return (package != null) ? package.IndexFileName : null; }
            set { if (package != null) { package.IndexFileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string ProvideFileName
        {
            get { return (package != null) ? package.ProvideFileName : null; }
            set { if (package != null) { package.ProvideFileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public PackageFlags Flags
        {
            get { return (package != null) ? package.Flags : PackageFlags.None; }
            set { if (package != null) { package.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Version Loaded
        {
            get { return (package != null) ? package.Loaded : null; }
            set { if (package != null) { package.Loaded = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public VersionStringDictionary IfNeeded
        {
            get { return (package != null) ? package.IfNeeded : null; }
            set { if (package != null) { package.IfNeeded = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string WasNeeded
        {
            get { return (package != null) ? package.WasNeeded : null; }
            set { if (package != null) { package.WasNeeded = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackage Members
        public ReturnCode Select(
            PackagePreference preference,
            ref Version version,
            ref Result error
            )
        {
            if (package != null)
                return package.Select(preference, ref version, ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Load(
            Interpreter interpreter,
            Version version,
            ref Result result
            )
        {
            if (package != null)
                return package.Load(interpreter, version, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return package; }
        }
        #endregion
    }
}
