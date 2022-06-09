/*
 * Default.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Packages
{
    [ObjectId("d97bbc96-0d1e-4263-82cd-f963ddb3f6ac")]
    public class Default : IPackage
    {
        #region Public Constructors
        public Default(
            IPackageData packageData
            )
        {
            kind = IdentifierKind.Package;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            if (packageData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, packageData.Group);

                name = packageData.Name;
                description = packageData.Description;
                indexFileName = packageData.IndexFileName;
                provideFileName = packageData.ProvideFileName;
                flags = packageData.Flags;
                clientData = packageData.ClientData;
                loaded = packageData.Loaded;

                VersionStringDictionary ifNeeded = packageData.IfNeeded;

                if (ifNeeded != null)
                    this.ifNeeded = ifNeeded; // use (or "attach to") their versions.
                else
                    this.ifNeeded = new VersionStringDictionary(); // brand new package, create new list.

                token = packageData.Token;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        private int initializeCount;
        public virtual bool Initialized
        {
            get
            {
                return Interlocked.CompareExchange(
                    ref initializeCount, 0, 0) > 0;
            }
            set
            {
                if (value)
                    Interlocked.Increment(ref initializeCount);
                else
                    Interlocked.Decrement(ref initializeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            Interlocked.Increment(ref initializeCount);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            Interlocked.Decrement(ref initializeCount);
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackageData Members
        private string indexFileName;
        public virtual string IndexFileName
        {
            get { return indexFileName; }
            set { indexFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string provideFileName;
        public virtual string ProvideFileName
        {
            get { return provideFileName; }
            set { provideFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PackageFlags flags;
        public virtual PackageFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Version loaded;
        public virtual Version Loaded
        {
            get { return loaded; }
            set { loaded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private VersionStringDictionary ifNeeded;
        public virtual VersionStringDictionary IfNeeded
        {
            get { return ifNeeded; }
            set { ifNeeded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string wasNeeded;
        public virtual string WasNeeded
        {
            get { return wasNeeded; }
            set { wasNeeded = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackage Members
        public virtual ReturnCode Select(
            PackagePreference preference,
            ref Version version,
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode Load(
            Interpreter interpreter,
            Version version,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
