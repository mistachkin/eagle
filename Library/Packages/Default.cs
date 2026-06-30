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
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="IPackage" /> interface, which represents a script package
    /// within the Eagle engine.  It supplies the common identity, state, and
    /// package-data storage (including the set of available <c>ifneeded</c>
    /// scripts) shared by all packages; its <see cref="Select" /> and
    /// <see cref="Load" /> methods are no-op placeholders that derived classes
    /// override.  See <c>core_language.md</c> for package management
    /// semantics.
    /// </summary>
    [ObjectId("d97bbc96-0d1e-4263-82cd-f963ddb3f6ac")]
    public class Default : IPackage
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default package, optionally
        /// initializing it from the supplied package data.
        /// </summary>
        /// <param name="packageData">
        /// The data used to create and identify this package, such as its name
        /// and the set of available <c>ifneeded</c> scripts.  This parameter
        /// may be null, in which case the package is left with default property
        /// values.
        /// </param>
        public Default(
            IPackageData packageData
            )
        {
            kind = IdentifierKind.Package;

            if ((packageData == null) ||
                !FlagOps.HasFlags(packageData.Flags,
                    PackageFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (packageData != null)
            {
                id = packageData.Id;

                EntityOps.MaybeSetupId(this);

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
        /// <summary>
        /// The name of this package.
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
        /// The kind of identifier represented by this package.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this package.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier for this package.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier for this package.
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
        /// The extra, caller-specific data associated with this package, if
        /// any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, caller-specific data associated with this
        /// package.
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
        /// The name of the group this package belongs to, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the name of the group this package belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this package, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this package.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// The net number of times this package has been initialized; greater
        /// than zero indicates the package is currently initialized.
        /// </summary>
        private int initializeCount;
        /// <summary>
        /// Gets or sets a value indicating whether this package is currently
        /// initialized.  Setting this property to true increments, and false
        /// decrements, the internal initialization count.
        /// </summary>
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

        /// <summary>
        /// Initializes this package, incrementing its internal initialization
        /// count.  This default implementation always succeeds.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this package is being initialized in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data supplied for this operation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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

        /// <summary>
        /// Terminates this package, decrementing its internal initialization
        /// count.  This default implementation always succeeds.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this package is being terminated in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data supplied for this operation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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
        /// <summary>
        /// The name of the package index file that defined this package, if
        /// any.
        /// </summary>
        private string indexFileName;
        /// <summary>
        /// Gets or sets the name of the package index file that defined this
        /// package.
        /// </summary>
        public virtual string IndexFileName
        {
            get { return indexFileName; }
            set { indexFileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the file that provided this package, if any.
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
        /// The flags that control the behavior of this package.
        /// </summary>
        private PackageFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this package.
        /// </summary>
        public virtual PackageFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The version of this package that is currently loaded, if any.
        /// </summary>
        private Version loaded;
        /// <summary>
        /// Gets or sets the version of this package that is currently loaded.
        /// </summary>
        public virtual Version Loaded
        {
            get { return loaded; }
            set { loaded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The collection of <c>ifneeded</c> scripts for this package, keyed
        /// by version.
        /// </summary>
        private VersionStringDictionary ifNeeded;
        /// <summary>
        /// Gets or sets the collection of <c>ifneeded</c> scripts for this
        /// package, keyed by version.
        /// </summary>
        public virtual VersionStringDictionary IfNeeded
        {
            get { return ifNeeded; }
            set { ifNeeded = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The <c>ifneeded</c> script that was actually evaluated to load this
        /// package, if any.
        /// </summary>
        private string wasNeeded;
        /// <summary>
        /// Gets or sets the <c>ifneeded</c> script that was actually evaluated
        /// to load this package.
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
        /// The interpreter token that uniquely identifies this package within
        /// its containing collection.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the interpreter token that uniquely identifies this
        /// package within its containing collection.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackage Members
        /// <summary>
        /// Selects a version of this package to satisfy a request, according
        /// to the supplied preference.  This default implementation performs
        /// no work and always succeeds; derived classes override it to perform
        /// the actual selection.
        /// </summary>
        /// <param name="preference">
        /// The preference that governs how a candidate version is chosen.
        /// </param>
        /// <param name="version">
        /// Upon success, receives the selected version of this package.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Select(
            PackagePreference preference,
            ref Version version,
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Loads the specified version of this package.  This default
        /// implementation performs no work and always succeeds; derived
        /// classes override it to perform the actual loading.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which to load the package.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="version">
        /// The version of this package to load.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result produced while loading
        /// the package.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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
