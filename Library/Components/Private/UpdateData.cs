/*
 * UpdateData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Shared;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class holds the data describing a software update to be checked
    /// for and/or applied, including the identity and origin of the update
    /// (name, group, description, URI, public key token, culture, patch level,
    /// and time stamp), the local target directory, and the various options
    /// (action, release, and update types together with the script, quiet,
    /// prompt, and automatic flags) that govern how the update is processed.
    /// It implements <see cref="IUpdateData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("05bd2465-eb8a-4df1-b14d-3d3251d48d04")]
    internal class UpdateData : IUpdateData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified target
        /// directory and update options, leaving the identity and origin
        /// properties unset.  This constructor delegates to the primary
        /// constructor.
        /// </summary>
        /// <param name="targetDirectory">
        /// The local directory targeted by this update.  This parameter may be
        /// null.
        /// </param>
        /// <param name="actionType">
        /// The action to be performed for this update.
        /// </param>
        /// <param name="releaseType">
        /// The release type associated with this update.
        /// </param>
        /// <param name="updateType">
        /// The kind of update represented by this instance.
        /// </param>
        /// <param name="wantScripts">
        /// Non-zero if update scripts are wanted for this update.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress diagnostic output while processing this update.
        /// </param>
        /// <param name="prompt">
        /// Non-zero to prompt before applying this update.
        /// </param>
        /// <param name="automatic">
        /// Non-zero if this update is being processed automatically.
        /// </param>
        public UpdateData(
            string targetDirectory,
            ActionType actionType,
            ReleaseType releaseType,
            UpdateType updateType,
            bool wantScripts,
            bool quiet,
            bool prompt,
            bool automatic
            )
            : this(null, null, null, null, null, null, null, null, null,
                   targetDirectory, actionType, releaseType, updateType,
                   wantScripts, quiet, prompt, automatic)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, populating its identity and
        /// origin properties from the specified plugin data and using the
        /// specified update options.  This constructor delegates to another
        /// constructor overload.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data used to populate the identity and origin properties
        /// of this update.  This parameter may be null.
        /// </param>
        /// <param name="actionType">
        /// The action to be performed for this update.
        /// </param>
        /// <param name="releaseType">
        /// The release type associated with this update.
        /// </param>
        /// <param name="updateType">
        /// The kind of update represented by this instance.
        /// </param>
        /// <param name="wantScripts">
        /// Non-zero if update scripts are wanted for this update.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress diagnostic output while processing this update.
        /// </param>
        /// <param name="prompt">
        /// Non-zero to prompt before applying this update.
        /// </param>
        /// <param name="automatic">
        /// Non-zero if this update is being processed automatically.
        /// </param>
        public UpdateData(
            IPluginData pluginData,
            ActionType actionType,
            ReleaseType releaseType,
            UpdateType updateType,
            bool wantScripts,
            bool quiet,
            bool prompt,
            bool automatic
            )
            : this((string)null, actionType, releaseType, updateType,
                   wantScripts, quiet, prompt, automatic)
        {
            UsePluginData(pluginData);

            if (pluginData != null)
                UseAssemblyName(pluginData.AssemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the fully specified set
        /// of identity, origin, target, and option parameters.  This is the
        /// most general constructor; the other constructor overloads delegate
        /// to it.
        /// </summary>
        /// <param name="name">
        /// The name of this update.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this update.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this update.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this update.  This parameter may be
        /// null.
        /// </param>
        /// <param name="uri">
        /// The uniform resource identifier from which this update originates.
        /// This parameter may be null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token used to verify this update.  This parameter may
        /// be null.
        /// </param>
        /// <param name="culture">
        /// The culture name associated with this update.  This parameter may be
        /// null.
        /// </param>
        /// <param name="patchLevel">
        /// The version (patch level) of this update.  This parameter may be
        /// null.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp associated with this update.  This parameter may be
        /// null.
        /// </param>
        /// <param name="targetDirectory">
        /// The local directory targeted by this update.  This parameter may be
        /// null.
        /// </param>
        /// <param name="actionType">
        /// The action to be performed for this update.
        /// </param>
        /// <param name="releaseType">
        /// The release type associated with this update.
        /// </param>
        /// <param name="updateType">
        /// The kind of update represented by this instance.
        /// </param>
        /// <param name="wantScripts">
        /// Non-zero if update scripts are wanted for this update.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress diagnostic output while processing this update.
        /// </param>
        /// <param name="prompt">
        /// Non-zero to prompt before applying this update.
        /// </param>
        /// <param name="automatic">
        /// Non-zero if this update is being processed automatically.
        /// </param>
        public UpdateData(
            string name,
            string group,
            string description,
            IClientData clientData,
            Uri uri,
            byte[] publicKeyToken,
            string culture,
            Version patchLevel,
            DateTime? timeStamp,
            string targetDirectory,
            ActionType actionType,
            ReleaseType releaseType,
            UpdateType updateType,
            bool wantScripts,
            bool quiet,
            bool prompt,
            bool automatic
            )
        {
            this.kind = IdentifierKind.UpdateData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.Uri = uri;
            this.PublicKeyToken = publicKeyToken;
            this.Culture = culture;
            this.PatchLevel = patchLevel;
            this.TimeStamp = timeStamp;
            this.TargetDirectory = targetDirectory;
            this.ActionType = actionType;
            this.ReleaseType = releaseType;
            this.UpdateType = updateType;
            this.WantScripts = wantScripts;
            this.Quiet = quiet;
            this.Prompt = prompt;
            this.Automatic = automatic;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method populates the identity, origin, and target properties of
        /// this update from the specified plugin data.  When the plugin data is
        /// null, this method does nothing.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data used to populate this update.  This parameter may be
        /// null.
        /// </param>
        private void UsePluginData(
            IPluginData pluginData
            )
        {
            if (pluginData == null)
                return;

            this.name = pluginData.Name;
            this.group = pluginData.Group;
            this.description = pluginData.Description;
            this.clientData = pluginData.ClientData;
            this.Uri = pluginData.UpdateUri; /* obviously? */
            this.PatchLevel = pluginData.Version;
            this.TimeStamp = pluginData.DateTime;

            string fileName = pluginData.FileName;

            if (!String.IsNullOrEmpty(fileName))
                this.targetDirectory = Path.GetDirectoryName(fileName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the name, patch level, public key token, and
        /// culture of this update from the specified assembly name.  When the
        /// assembly name is null, this method does nothing.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name used to populate this update.  This parameter may
        /// be null.
        /// </param>
        private void UseAssemblyName(
            AssemblyName assemblyName
            )
        {
            if (assemblyName == null)
                return;

            this.name = assemblyName.Name;
            this.patchLevel = assemblyName.Version;
            this.publicKeyToken = assemblyName.GetPublicKeyToken();

            CultureInfo cultureInfo = assemblyName.CultureInfo;

            if (cultureInfo != null)
                this.culture = cultureInfo.Name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this update.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this update.
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
        /// Stores the identifier kind of this update.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this update.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this update.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this update.
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
        /// Stores the client data associated with this update.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this update.
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
        /// Stores the group of this update.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this update.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this update.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this update.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUpdateData Members
        /// <summary>
        /// Stores the local directory targeted by this update.
        /// </summary>
        private string targetDirectory;
        /// <summary>
        /// Gets or sets the local directory targeted by this update.
        /// </summary>
        public virtual string TargetDirectory
        {
            get { return targetDirectory; }
            set { targetDirectory = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the uniform resource identifier from which this update
        /// originates.
        /// </summary>
        private Uri uri;
        /// <summary>
        /// Gets or sets the uniform resource identifier from which this update
        /// originates.
        /// </summary>
        public virtual Uri Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the public key token used to verify this update.
        /// </summary>
        private byte[] publicKeyToken;
        /// <summary>
        /// Gets or sets the public key token used to verify this update.
        /// </summary>
        public virtual byte[] PublicKeyToken
        {
            get { return publicKeyToken; }
            set { publicKeyToken = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the culture name associated with this update.
        /// </summary>
        private string culture;
        /// <summary>
        /// Gets or sets the culture name associated with this update.
        /// </summary>
        public virtual string Culture
        {
            get { return culture; }
            set { culture = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the version (patch level) of this update.
        /// </summary>
        private Version patchLevel;
        /// <summary>
        /// Gets or sets the version (patch level) of this update.
        /// </summary>
        public virtual Version PatchLevel
        {
            get { return patchLevel; }
            set { patchLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the time stamp associated with this update.
        /// </summary>
        private DateTime? timeStamp;
        /// <summary>
        /// Gets or sets the time stamp associated with this update.
        /// </summary>
        public virtual DateTime? TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the action to be performed for this update.
        /// </summary>
        private ActionType actionType;
        /// <summary>
        /// Gets or sets the action to be performed for this update.
        /// </summary>
        public virtual ActionType ActionType
        {
            get { return actionType; }
            set { actionType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the release type associated with this update.
        /// </summary>
        private ReleaseType releaseType;
        /// <summary>
        /// Gets or sets the release type associated with this update.
        /// </summary>
        public virtual ReleaseType ReleaseType
        {
            get { return releaseType; }
            set { releaseType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the kind of update represented by this instance.
        /// </summary>
        private UpdateType updateType;
        /// <summary>
        /// Gets or sets the kind of update represented by this instance.
        /// </summary>
        public virtual UpdateType UpdateType
        {
            get { return updateType; }
            set { updateType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether update scripts are wanted for this
        /// update.
        /// </summary>
        private bool wantScripts;
        /// <summary>
        /// Gets or sets a value indicating whether update scripts are wanted
        /// for this update.
        /// </summary>
        public virtual bool WantScripts
        {
            get { return wantScripts; }
            set { wantScripts = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether diagnostic output is suppressed
        /// while processing this update.
        /// </summary>
        private bool quiet;
        /// <summary>
        /// Gets or sets a value indicating whether diagnostic output is
        /// suppressed while processing this update.
        /// </summary>
        public virtual bool Quiet
        {
            get { return quiet; }
            set { quiet = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether to prompt before applying this
        /// update.
        /// </summary>
        private bool prompt;
        /// <summary>
        /// Gets or sets a value indicating whether to prompt before applying
        /// this update.
        /// </summary>
        public virtual bool Prompt
        {
            get { return prompt; }
            set { prompt = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this update is being processed
        /// automatically.
        /// </summary>
        private bool automatic;
        /// <summary>
        /// Gets or sets a value indicating whether this update is being
        /// processed automatically.
        /// </summary>
        public virtual bool Automatic
        {
            get { return automatic; }
            set { automatic = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method builds a human-readable string describing the identity,
        /// origin, target, and option properties of this update, suitable for
        /// use in trace and diagnostic output.
        /// </summary>
        /// <returns>
        /// The trace string describing this update.
        /// </returns>
        public virtual string ToTraceString()
        {
            IStringList list = new StringPairList();

            list.Add("id", FormatOps.WrapOrNull(id));
            list.Add("name", FormatOps.WrapOrNull(name));
            list.Add("group", FormatOps.WrapOrNull(group));
            list.Add("description", FormatOps.WrapOrNull(description));
            list.Add("clientData", FormatOps.WrapOrNull(clientData));

            list.Add("uri", FormatOps.WrapOrNull(Uri));

            list.Add("publicKeyToken",
                ArrayOps.ToHexadecimalString(PublicKeyToken));

            list.Add("culture", FormatOps.WrapOrNull(Culture));
            list.Add("patchLevel", FormatOps.WrapOrNull(PatchLevel));
            list.Add("timeStamp", FormatOps.UpdateDateTime(TimeStamp));

            list.Add("targetDirectory",
                FormatOps.WrapOrNull(TargetDirectory));

            list.Add("actionType", FormatOps.WrapOrNull(ActionType));
            list.Add("releaseType", FormatOps.WrapOrNull(ReleaseType));
            list.Add("updateType", FormatOps.WrapOrNull(UpdateType));
            list.Add("wantScripts", FormatOps.WrapOrNull(WantScripts));
            list.Add("quiet", FormatOps.WrapOrNull(Quiet));
            list.Add("prompt", FormatOps.WrapOrNull(Prompt));
            list.Add("automatic", FormatOps.WrapOrNull(Automatic));

            return list.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this update.
        /// </summary>
        /// <returns>
        /// The name of this update, or an empty string when the name is null.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
