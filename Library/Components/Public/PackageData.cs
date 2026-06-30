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
    /// <summary>
    /// This class represents the metadata describing a script package known to
    /// an interpreter, including its identity, the files used to index and
    /// provide it, its flags, the version currently loaded (if any), and the
    /// scripts used to satisfy a request for it on demand.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0d6449fa-ae39-4344-b4c3-1906457a73cf")]
    public class PackageData : IPackageData
    {
        /// <summary>
        /// Constructs a package data instance from the fully specified set of
        /// identity, file, flag, version, and token parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this package.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this package.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this package.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this package, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="indexFileName">
        /// The name of the package index file associated with this package.
        /// This parameter may be null.
        /// </param>
        /// <param name="provideFileName">
        /// The name of the file that provided this package.  This parameter may
        /// be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling this package's behavior.
        /// </param>
        /// <param name="loaded">
        /// The version of this package that is currently loaded, or null if it
        /// is not loaded.
        /// </param>
        /// <param name="ifNeeded">
        /// The collection mapping package versions to the scripts used to
        /// provide them on demand.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this package within the interpreter.
        /// </param>
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
        /// <summary>
        /// Stores the name of this package.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this package.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this package.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this package.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this package.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this package.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this package.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this package.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this package.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this package.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this package.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this package.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackageData Members
        /// <summary>
        /// Stores the name of the package index file associated with this
        /// package.
        /// </summary>
        private string indexFileName;
        /// <summary>
        /// Gets or sets the name of the package index file associated with this
        /// package.
        /// </summary>
        public virtual string IndexFileName
        {
            get { return indexFileName; }
            set { indexFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the file that provided this package.
        /// </summary>
        private string provideFileName;
        /// <summary>
        /// Gets or sets the name of the file that provided this package.
        /// </summary>
        public virtual string ProvideFileName
        {
            get { return provideFileName; }
            set { provideFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling this package's behavior.
        /// </summary>
        private PackageFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling this package's behavior.
        /// </summary>
        public virtual PackageFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the version of this package that is currently loaded, or null
        /// if it is not loaded.
        /// </summary>
        private Version loaded;
        /// <summary>
        /// Gets or sets the version of this package that is currently loaded, or
        /// null if it is not loaded.
        /// </summary>
        public virtual Version Loaded
        {
            get { return loaded; }
            set { loaded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection mapping package versions to the scripts used to
        /// provide them on demand.
        /// </summary>
        private VersionStringDictionary ifNeeded;
        /// <summary>
        /// Gets or sets the collection mapping package versions to the scripts
        /// used to provide them on demand.
        /// </summary>
        public virtual VersionStringDictionary IfNeeded
        {
            get { return ifNeeded; }
            set { ifNeeded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the version string most recently used to satisfy a request
        /// for this package on demand.
        /// </summary>
        private string wasNeeded;
        /// <summary>
        /// Gets or sets the version string most recently used to satisfy a
        /// request for this package on demand.
        /// </summary>
        public virtual string WasNeeded
        {
            get { return wasNeeded; }
            set { wasNeeded = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this package within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this package within the
        /// interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the name of this package, or an empty string
        /// when it has no name.
        /// </summary>
        /// <returns>
        /// The name of this package.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
