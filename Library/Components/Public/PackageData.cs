/*
 * PackageData.cs --
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
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0d6449fa-ae39-4344-b4c3-1906457a73cf")]
    public class PackageData : IPackageData
    {
        public PackageData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string indexFileName,
            string provideFileName,
            PackageFlags flags,
            Version loaded,
            VersionStringDictionary ifNeeded,
            long token
            )
        {
            this.kind = IdentifierKind.PackageData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.indexFileName = indexFileName;
            this.provideFileName = provideFileName;
            this.flags = flags;
            this.clientData = clientData;
            this.loaded = loaded;
            this.ifNeeded = ifNeeded;
            this.token = token;
        }

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

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
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

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
