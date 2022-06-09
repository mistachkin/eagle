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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("05bd2465-eb8a-4df1-b14d-3d3251d48d04")]
    internal class UpdateData : IUpdateData
    {
        #region Public Constructors
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

        #region IUpdateData Members
        private string targetDirectory;
        public virtual string TargetDirectory
        {
            get { return targetDirectory; }
            set { targetDirectory = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri uri;
        public virtual Uri Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] publicKeyToken;
        public virtual byte[] PublicKeyToken
        {
            get { return publicKeyToken; }
            set { publicKeyToken = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string culture;
        public virtual string Culture
        {
            get { return culture; }
            set { culture = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Version patchLevel;
        public virtual Version PatchLevel
        {
            get { return patchLevel; }
            set { patchLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime? timeStamp;
        public virtual DateTime? TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ActionType actionType;
        public virtual ActionType ActionType
        {
            get { return actionType; }
            set { actionType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReleaseType releaseType;
        public virtual ReleaseType ReleaseType
        {
            get { return releaseType; }
            set { releaseType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private UpdateType updateType;
        public virtual UpdateType UpdateType
        {
            get { return updateType; }
            set { updateType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool wantScripts;
        public virtual bool WantScripts
        {
            get { return wantScripts; }
            set { wantScripts = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool quiet;
        public virtual bool Quiet
        {
            get { return quiet; }
            set { quiet = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool prompt;
        public virtual bool Prompt
        {
            get { return prompt; }
            set { prompt = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool automatic;
        public virtual bool Automatic
        {
            get { return automatic; }
            set { automatic = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
