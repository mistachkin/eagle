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
    /// <summary>
    /// This class holds the metadata describing an Eagle plugin -- its
    /// identity (name, group, description, and client data), its flags and
    /// version, the URIs associated with it, the application domain, assembly,
    /// and type it was loaded from, and the commands, functions, policies, and
    /// traces it has registered with an interpreter.  It implements
    /// <see cref="IPluginData" /> and is primarily used as the data carrier
    /// portion of a plugin wrapper.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("174bf0ab-119e-4322-9f5f-a3423fb750e1")]
    public class PluginData : IPluginData
    {
        /// <summary>
        /// Constructs plugin data from the fully specified set of identity,
        /// flag, location, and registration parameters.
        /// </summary>
        /// <param name="name">
        /// The name of the plugin.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the plugin.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the plugin.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the plugin.  This parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the plugin's behavior.
        /// </param>
        /// <param name="version">
        /// The version of the plugin.  This parameter may be null.
        /// </param>
        /// <param name="uri">
        /// The URI associated with the plugin.  This parameter may be null.
        /// </param>
        /// <param name="updateUri">
        /// The update URI associated with the plugin.  This parameter may be
        /// null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain the plugin was loaded into.  This parameter
        /// may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly the plugin was loaded from.  This parameter may be
        /// null.
        /// </param>
        /// <param name="assemblyName">
        /// The name of the assembly the plugin was loaded from.  This parameter
        /// may be null.
        /// </param>
        /// <param name="dateTime">
        /// The date and time associated with the plugin, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name the plugin was loaded from.  This parameter may be
        /// null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type implementing the plugin.  This parameter may be
        /// null.
        /// </param>
        /// <param name="commands">
        /// The list of commands provided by the plugin.  This parameter may be
        /// null.
        /// </param>
        /// <param name="policies">
        /// The list of policies provided by the plugin.  This parameter may be
        /// null.
        /// </param>
        /// <param name="commandTokens">
        /// The list of tokens for the commands registered by the plugin.  This
        /// parameter may be null.
        /// </param>
        /// <param name="functionTokens">
        /// The list of tokens for the functions registered by the plugin.  This
        /// parameter may be null.
        /// </param>
        /// <param name="policyTokens">
        /// The list of tokens for the policies registered by the plugin.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceTokens">
        /// The list of tokens for the traces registered by the plugin.  This
        /// parameter may be null.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager associated with the plugin.  This parameter may
        /// be null.
        /// </param>
        /// <param name="auxiliaryData">
        /// The auxiliary data associated with the plugin.  This parameter may
        /// be null.
        /// </param>
        /// <param name="token">
        /// The token identifying the plugin within an interpreter.
        /// </param>
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
        /// <summary>
        /// Stores the name of the plugin.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the plugin.
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
        /// Stores the identifier kind of the plugin.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of the plugin.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of the plugin.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of the plugin.
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
        /// Stores the client data associated with the plugin.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the plugin.
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
        /// Stores the group of the plugin.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of the plugin.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of the plugin.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of the plugin.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Stores the name of the type implementing the plugin.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the type implementing the plugin.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the type implementing the plugin.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type implementing the plugin.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPluginData Members
        /// <summary>
        /// Stores the flags controlling the plugin's behavior.
        /// </summary>
        private PluginFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling the plugin's behavior.
        /// </summary>
        public virtual PluginFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the version of the plugin.
        /// </summary>
        private Version version;
        /// <summary>
        /// Gets or sets the version of the plugin.
        /// </summary>
        public virtual Version Version
        {
            get { return version; }
            set { version = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the URI associated with the plugin.
        /// </summary>
        private Uri uri;
        /// <summary>
        /// Gets or sets the URI associated with the plugin.
        /// </summary>
        public virtual Uri Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the update URI associated with the plugin.
        /// </summary>
        private Uri updateUri;
        /// <summary>
        /// Gets or sets the update URI associated with the plugin.
        /// </summary>
        public virtual Uri UpdateUri
        {
            get { return updateUri; }
            set { updateUri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the application domain the plugin was loaded into.
        /// </summary>
#if SERIALIZATION
        [NonSerialized()]
#endif
        private AppDomain appDomain;
        /// <summary>
        /// Gets or sets the application domain the plugin was loaded into.
        /// </summary>
        public virtual AppDomain AppDomain
        {
            get { return appDomain; }
            set { appDomain = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the assembly the plugin was loaded from.
        /// </summary>
#if SERIALIZATION
        [NonSerialized()]
#endif
        private Assembly assembly;
        /// <summary>
        /// Gets or sets the assembly the plugin was loaded from.
        /// </summary>
        public virtual Assembly Assembly
        {
            get { return assembly; }
            set { assembly = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the assembly the plugin was loaded from.
        /// </summary>
        private AssemblyName assemblyName;
        /// <summary>
        /// Gets or sets the name of the assembly the plugin was loaded from.
        /// </summary>
        public virtual AssemblyName AssemblyName
        {
            get { return assemblyName; }
            set { assemblyName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the date and time associated with the plugin, if any.
        /// </summary>
        private DateTime? dateTime;
        /// <summary>
        /// Gets or sets the date and time associated with the plugin, if any.
        /// </summary>
        public virtual DateTime? DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the file name the plugin was loaded from.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets or sets the file name the plugin was loaded from.
        /// </summary>
        public virtual string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of commands provided by the plugin.
        /// </summary>
        private CommandDataList commands;
        /// <summary>
        /// Gets or sets the list of commands provided by the plugin.
        /// </summary>
        public virtual CommandDataList Commands
        {
            get { return commands; }
            set { commands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of policies provided by the plugin.
        /// </summary>
        private PolicyDataList policies;
        /// <summary>
        /// Gets or sets the list of policies provided by the plugin.
        /// </summary>
        public virtual PolicyDataList Policies
        {
            get { return policies; }
            set { policies = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of tokens for the commands registered by the plugin.
        /// </summary>
        private LongList commandTokens;
        /// <summary>
        /// Gets or sets the list of tokens for the commands registered by the
        /// plugin.
        /// </summary>
        public virtual LongList CommandTokens
        {
            get { return commandTokens; }
            set { commandTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of tokens for the functions registered by the
        /// plugin.
        /// </summary>
        private LongList functionTokens;
        /// <summary>
        /// Gets or sets the list of tokens for the functions registered by the
        /// plugin.
        /// </summary>
        public virtual LongList FunctionTokens
        {
            get { return functionTokens; }
            set { functionTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of tokens for the policies registered by the plugin.
        /// </summary>
        private LongList policyTokens;
        /// <summary>
        /// Gets or sets the list of tokens for the policies registered by the
        /// plugin.
        /// </summary>
        public virtual LongList PolicyTokens
        {
            get { return policyTokens; }
            set { policyTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of tokens for the traces registered by the plugin.
        /// </summary>
        private LongList traceTokens;
        /// <summary>
        /// Gets or sets the list of tokens for the traces registered by the
        /// plugin.
        /// </summary>
        public virtual LongList TraceTokens
        {
            get { return traceTokens; }
            set { traceTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the resource manager associated with the plugin.
        /// </summary>
#if SERIALIZATION
        [NonSerialized()]
#endif
        private ResourceManager resourceManager;
        /// <summary>
        /// Gets or sets the resource manager associated with the plugin.
        /// </summary>
        public virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the auxiliary data associated with the plugin.
        /// </summary>
#if SERIALIZATION
        [NonSerialized()]
#endif
        private ObjectDictionary auxiliaryData;
        /// <summary>
        /// Gets or sets the auxiliary data associated with the plugin.
        /// </summary>
        public virtual ObjectDictionary AuxiliaryData
        {
            get { return auxiliaryData; }
            set { auxiliaryData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token identifying the plugin within an interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token identifying the plugin within an interpreter.
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
        /// This method produces a string describing the plugin using its name
        /// only.
        /// </summary>
        /// <returns>
        /// A string containing the name of the plugin (or an empty string when
        /// it has no name).
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}

