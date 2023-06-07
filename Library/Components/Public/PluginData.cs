/*
 * PluginData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("174bf0ab-119e-4322-9f5f-a3423fb750e1")]
    public class PluginData : IPluginData
    {
        public PluginData(
            string name,
            string group,
            string description,
            IClientData clientData,
            PluginFlags flags,
            Version version,
            Uri uri,
            Uri updateUri,
            AppDomain appDomain,
            Assembly assembly,
            AssemblyName assemblyName,
            DateTime? dateTime,
            string fileName,
            string typeName,
            CommandDataList commands,
            PolicyDataList policies,
            LongList commandTokens,
            LongList functionTokens,
            LongList policyTokens,
            LongList traceTokens,
            ResourceManager resourceManager,
            ObjectDictionary auxiliaryData,
            long token
            )
        {
            this.kind = IdentifierKind.PluginData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.flags = flags;
            this.version = version;
            this.uri = uri;
            this.updateUri = updateUri;
            this.appDomain = appDomain;
            this.assembly = assembly;
            this.assemblyName = assemblyName;
            this.dateTime = dateTime;
            this.fileName = fileName;
            this.typeName = typeName;
            this.commands = commands;
            this.policies = policies;
            this.commandTokens = commandTokens;
            this.functionTokens = functionTokens;
            this.policyTokens = policyTokens;
            this.traceTokens = traceTokens;
            this.resourceManager = resourceManager;
            this.auxiliaryData = auxiliaryData;
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

        #region ITypeAndName Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPluginData Members
        private PluginFlags flags;
        public virtual PluginFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Version version;
        public virtual Version Version
        {
            get { return version; }
            set { version = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri uri;
        public virtual Uri Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri updateUri;
        public virtual Uri UpdateUri
        {
            get { return updateUri; }
            set { updateUri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        [NonSerialized()]
#endif
        private AppDomain appDomain;
        public virtual AppDomain AppDomain
        {
            get { return appDomain; }
            set { appDomain = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        [NonSerialized()]
#endif
        private Assembly assembly;
        public virtual Assembly Assembly
        {
            get { return assembly; }
            set { assembly = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private AssemblyName assemblyName;
        public virtual AssemblyName AssemblyName
        {
            get { return assemblyName; }
            set { assemblyName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime? dateTime;
        public virtual DateTime? DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fileName;
        public virtual string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private CommandDataList commands;
        public virtual CommandDataList Commands
        {
            get { return commands; }
            set { commands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDataList policies;
        public virtual PolicyDataList Policies
        {
            get { return policies; }
            set { policies = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private LongList commandTokens;
        public virtual LongList CommandTokens
        {
            get { return commandTokens; }
            set { commandTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private LongList functionTokens;
        public virtual LongList FunctionTokens
        {
            get { return functionTokens; }
            set { functionTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private LongList policyTokens;
        public virtual LongList PolicyTokens
        {
            get { return policyTokens; }
            set { policyTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private LongList traceTokens;
        public virtual LongList TraceTokens
        {
            get { return traceTokens; }
            set { traceTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        [NonSerialized()]
#endif
        private ResourceManager resourceManager;
        public virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        [NonSerialized()]
#endif
        private ObjectDictionary auxiliaryData;
        public virtual ObjectDictionary AuxiliaryData
        {
            get { return auxiliaryData; }
            set { auxiliaryData = value; }
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

