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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _ClientData = Eagle._Components.Public.ClientData;

namespace Eagle._Plugins
{
    [ObjectId("8c30d1ad-e753-4334-82ff-ea395e2542b5")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IPlugin
    {
        #region Public Constructors
        /// <summary>
        ///   This is the constructor used by the core library to create an
        ///   instance of the plugin, passing the necessary data to be used
        ///   for initializing the plugin.
        /// </summary>
        ///
        /// <param name="pluginData">
        ///   An instance of the plugin data component used to hold the data
        ///   necessary to fully initialize the plugin instance.  This
        ///   parameter may be null.  Derived plugins are free to override
        ///   this constructor; however, they are very strongly encouraged to
        ///   call this constructor (i.e. the base class constructor) in that
        ///   case.
        /// </param>
        public Default(
            IPluginData pluginData
            )
        {
            kind = IdentifierKind.Plugin;

            //
            // VIRTUAL: Id of the deepest derived class.
            //
            id = AttributeOps.GetObjectId(this);

            //
            // VIRTUAL: Group of the deepest derived class.
            //
            group = AttributeOps.GetObjectGroups(this);

            //
            // NOTE: Is the supplied plugin data valid?
            //
            if (pluginData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, pluginData.Group);

                name = pluginData.Name;
                description = pluginData.Description;
                flags = pluginData.Flags;
                clientData = pluginData.ClientData;
                version = pluginData.Version;
                uri = pluginData.Uri;
                appDomain = pluginData.AppDomain;
                assembly = pluginData.Assembly;
                assemblyName = pluginData.AssemblyName;
                dateTime = pluginData.DateTime;
                fileName = pluginData.FileName;
                typeName = pluginData.TypeName;
            }

            //
            // NOTE: Are we going to use their command list or create an
            //       entirely new list?
            //
            if ((pluginData != null) && (pluginData.Commands != null))
                commands = pluginData.Commands;
            else
                commands = new CommandDataList();

            //
            // NOTE: Are we going to use their policy list or create an
            //       entirely new list?
            //
            if ((pluginData != null) && (pluginData.Policies != null))
                policies = pluginData.Policies;
            else
                policies = new PolicyDataList();

            //
            // NOTE: Are we going to use their command tokens or create an
            //       entirely new list?
            //
            if ((pluginData != null) && (pluginData.CommandTokens != null))
                commandTokens = pluginData.CommandTokens;
            else
                commandTokens = new LongList();

            //
            // NOTE: Are we going to use their command tokens or create an
            //       entirely new list?
            //
            if ((pluginData != null) && (pluginData.FunctionTokens != null))
                functionTokens = pluginData.FunctionTokens;
            else
                functionTokens = new LongList();

            //
            // NOTE: Are we going to use their policy tokens or create an
            //       entirely new list?
            //
            if ((pluginData != null) && (pluginData.PolicyTokens != null))
                policyTokens =  pluginData.PolicyTokens;
            else
                policyTokens = new LongList();

            //
            // NOTE: Are we going to use their trace tokens or create an
            //       entirely new list?
            //
            if ((pluginData != null) && (pluginData.TraceTokens != null))
                traceTokens = pluginData.TraceTokens;
            else
                traceTokens = new LongList();

            //
            // NOTE: Are we going to use the resource manager they specified or
            //       create a new one based on the plugin name and assembly?
            //
            if ((pluginData != null) && (pluginData.ResourceManager != null))
            {
                resourceManager = pluginData.ResourceManager;
            }
            else
            {
                //
                // NOTE: If the assembly is null we are probably loaded into an
                //       isolated application domain.  Therefore, in that case,
                //       and only in that case, since we are executing in the
                //       target application domain, load the assembly based on
                //       the assembly name and then use that to create the
                //       resource manager.  However, do not simply set the
                //       assembly field of this plugin to any non-null value
                //       because we do not want to cause issues with the
                //       interpreter plugin manager later.  Also, skip attempts
                //       to create a resource manager if the NoResources flag
                //       has been set on the plugin.
                //
                if (!FlagOps.HasFlags(flags, PluginFlags.NoResources, true))
                {
                    if (assembly != null)
                    {
                        resourceManager = RuntimeOps.NewResourceManager(
                            assembly);
                    }
                    else if (assemblyName != null)
                    {
                        resourceManager = RuntimeOps.NewResourceManager(
                            assemblyName);
                    }
                }
            }

            //
            // NOTE: Are we going to use the auxiliary data they specified or
            //       create a new one?
            //
            if ((pluginData != null) && (pluginData.AuxiliaryData != null))
            {
                auxiliaryData = pluginData.AuxiliaryData;
            }
            else
            {
                if (!FlagOps.HasFlags(
                        flags, PluginFlags.NoAuxiliaryData, true))
                {
                    auxiliaryData = new ObjectDictionary();
                }
            }

            //
            // NOTE: Also store the plugin token (which may be zero at this
            //       point).
            //
            if (pluginData != null)
                token = pluginData.Token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        ///   Returns the name of the package to add to the interpreter when
        ///   this plugin is initialized.  Generally, this name is based on
        ///   the name of the containing assembly and/or the fully qualified
        ///   type name.
        /// </summary>
        ///
        /// <param name="simple">
        ///   Non-zero if the simple name for the plugin should be used.
        /// </param>
        ///
        /// <returns>
        ///   The name of the package to add to the interpreter that this
        ///   plugin is being loaded into -OR- null if the name cannot be
        ///   determined.
        /// </returns>
        protected virtual string GetPackageName(
            bool simple
            )
        {
            return RuntimeOps.GetPluginPackageName(this, simple);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   Returns the flags for the package to add to the interpreter when
        ///   this plugin is initialized.
        /// </summary>
        ///
        /// <returns>
        ///   The flags for the package to add to the interpreter that this
        ///   plugin is being loaded into -OR- <see cref="PackageFlags.None" />
        ///   if they cannot be determined.
        /// </returns>
        protected virtual PackageFlags GetPackageFlags()
        {
            //
            // NOTE: We know the package is coming from a plugin because
            //       we are that plugin.
            //
            return PackageFlags.Plugin;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        ///   The name of this plugin.  This will normally be set based on the
        ///   plugin data provided to the constructor of this class; however,
        ///   it can be manually reset at any time.
        /// </summary>
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        ///   The kind of identifier for this object instance.  For plugins,
        ///   this should always be "Plugin".
        /// </summary>
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The unique Id of this plugin class (or instance).  The default
        ///   value is Guid.Empty.  This will normally be set based on the
        ///   plugin data provided to the constructor of this class; however,
        ///   it can be manually reset at any time.
        /// </summary>
        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        ///   The logical group name for this plugin.  The default value is
        ///   null.  This property is not currently used by the core library.
        ///   This will normally be set based on the plugin data provided to
        ///   the constructor of this class; however, it can be manually reset
        ///   at any time.
        /// </summary>
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The description associated with this plugin class (or instance).
        ///   The default value is null.  This property is not currently used
        ///   by the core library.  This will normally be set based on the
        ///   plugin data provided to the constructor of this class; however,
        ///   it can be manually reset at any time.
        /// </summary>
        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        ///   The client data passed to the original method that created this
        ///   plugin instance.  This value will be passed to the Initialize and
        ///   Terminate methods whenever they are called by the core library.
        ///   This will normally be set based on the plugin data provided to
        ///   the constructor of this class; however, it can be manually reset
        ///   at any time.
        /// </summary>
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        ///   This property is used to keep track of whether or not the plugin
        ///   has (ever) been initialized.  Generally, it should only be set
        ///   by the Initialize and Terminate methods.
        /// </summary>
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

        /// <summary>
        ///   Initialize the plugin and add any contained commands.
        ///
        ///   WARNING: PLEASE DO NOT CHANGE THIS METHOD BECAUSE DERIVED PLUGINS
        ///            DEPEND ON ITS EXACT SEMANTICS.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.
        /// </param>
        ///
        /// <param name="clientData">
        ///   The extra data supplied when this plugin was initially created,
        ///   if any.
        /// </param>
        ///
        /// <param name="result">
        ///   Upon success, this may contain an informational message.
        ///   Upon failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Initialize: interpreter = {0}, plugin = {1}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(this)), typeof(Default).Name,
                TracePriority.PluginDebug);

            PluginFlags flags = this.Flags;
            ReturnCode code;

            if (FlagOps.HasFlags(flags, PluginFlags.NoInitialize, true))
            {
                if (!FlagOps.HasFlags(
                        flags, PluginFlags.NoInitializeFlag, true))
                {
                    Interlocked.Increment(ref initializeCount);
                }

                if (!FlagOps.HasFlags(flags, PluginFlags.NoResult, true))
                    result = String.Empty;

                code = ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: We require a valid interpreter context.
                //
                if (interpreter != null)
                {
                    code = ReturnCode.Ok;

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoCommands, true))
                    {
                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of loading all valid commands
                        //       (i.e. classes that implement ICommand,
                        //       directly or indirectly) in this plugin.
                        //
                        code = interpreter.AddCommands(
                            this, clientData, CommandFlags.None,
                            ref result);

                        if (code == ReturnCode.Ok)
                        {
                            code = interpreter.MoveExposedAndHiddenCommands(
                                flags, ref result);
                        }
                    }

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoPolicies, true))
                    {
                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of loading all valid policies
                        //       (i.e. methods that are flagged as a "policy"
                        //       and are of the appropriate delegate type(s))
                        //       in this plugin.
                        //
                        code = interpreter.AddPolicies(
                            this, clientData, ref result);
                    }

                    Version version = null;

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoProvide, true))
                    {
                        //
                        // NOTE: Grab the plugin version now, if
                        //       necessary, since we know that we
                        //       need it.
                        //
                        if (version == null)
                            version = this.Version;

                        //
                        // NOTE: Formally "provide" (i.e. announce) this
                        //       package to the interpreter so that scripts
                        //       can easily detect it.
                        //
                        code = interpreter.PkgProvide(GetPackageName(
                            FlagOps.HasFlags(flags, PluginFlags.SimpleName,
                            true)), version, _ClientData.Empty, GetPackageFlags(),
                            ref result);
                    }

                    //
                    // NOTE: If the above steps succeeded, mark the
                    //       plugin as initialized and return an
                    //       appropriate result.
                    //
                    if (code == ReturnCode.Ok)
                    {
                        if (!FlagOps.HasFlags(
                                flags, PluginFlags.NoInitializeFlag, true))
                        {
                            Interlocked.Increment(ref initializeCount);
                        }

                        //
                        // NOTE: Returning the loaded plugin name
                        //       and version is HIGHLY RECOMMENDED
                        //       here.
                        //
                        if (!FlagOps.HasFlags(
                                flags, PluginFlags.NoResult, true))
                        {
                            //
                            // NOTE: Grab the plugin version now, if
                            //       necessary, since we know that
                            //       we need it.
                            //
                            if (version == null)
                                version = this.Version;

                            result = StringList.MakeList(
                                this.Name, version);
                        }
                    }
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   Terminate the plugin and remove any contained commands.
        ///
        ///   WARNING: PLEASE DO NOT CHANGE THIS METHOD BECAUSE DERIVED PLUGINS
        ///            DEPEND ON ITS EXACT SEMANTICS.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.
        /// </param>
        ///
        /// <param name="clientData">
        ///   The extra data supplied when this plugin was initially created,
        ///   if any.
        /// </param>
        ///
        /// <param name="result">
        ///   Upon success, this may contain an informational message.
        ///   Upon failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Terminate: interpreter = {0}, plugin = {1}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(this)), typeof(Default).Name,
                TracePriority.PluginDebug);

            PluginFlags flags = this.Flags;
            ReturnCode code;

            if (FlagOps.HasFlags(flags, PluginFlags.NoTerminate, true))
            {
                if (!FlagOps.HasFlags(
                        flags, PluginFlags.NoInitializeFlag, true))
                {
                    Interlocked.Decrement(ref initializeCount);
                }

                if (!FlagOps.HasFlags(flags, PluginFlags.NoResult, true))
                    result = null;

                code = ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: We require a valid interpreter context.
                //
                if (interpreter != null)
                {
                    code = ReturnCode.Ok;

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoTraces, true))
                    {
                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of unloading all previously
                        //       loaded traces (i.e. methods that are
                        //       flagged as a "trace" and are of the
                        //       appropriate delegate type(s)) from this
                        //       plugin.
                        //
                        code = interpreter.RemoveTraces(
                            this, clientData, ref result);
                    }

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoPolicies, true))
                    {
                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of unloading all previously
                        //       loaded policies (i.e. methods that are
                        //       flagged as a "policy" and are of the
                        //       appropriate delegate type(s)) from this
                        //       plugin.
                        //
                        code = interpreter.RemovePolicies(
                            this, clientData, ref result);
                    }

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoFunctions, true))
                    {
                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of unloading all previously
                        //       loaded functions (i.e. classes that
                        //       implement IFunction, directly or
                        //       indirectly) from this plugin.
                        //
                        code = interpreter.RemoveFunctions(
                            this, clientData, FunctionFlags.None,
                            ref result);
                    }

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoCommands, true))
                    {
                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of unloading all previously
                        //       loaded commands (i.e. classes that
                        //       implement ICommand, directly or
                        //       indirectly) from this plugin.
                        //
                        code = interpreter.RemoveCommands(
                            this, clientData, CommandFlags.None,
                            ref result);
                    }

                    Version version = null;

                    if ((code == ReturnCode.Ok) && !FlagOps.HasFlags(
                            flags, PluginFlags.NoProvide, true))
                    {
                        //
                        // NOTE: Grab the plugin version now, if
                        //       necessary, since we know that we
                        //       need it.
                        //
                        if (version == null)
                            version = this.Version;

                        //
                        // NOTE: Formally "withdraw" (i.e. unannounce)
                        //       this package from the interpreter so
                        //       that scripts can no longer detect it.
                        //
                        code = interpreter.WithdrawPackage(GetPackageName(
                            FlagOps.HasFlags(flags, PluginFlags.SimpleName,
                            true)), version, ref result);
                    }

                    //
                    // NOTE: If the above steps succeeded, mark the
                    //       plugin as not initialized and return an
                    //       appropriate result.
                    //
                    if (code == ReturnCode.Ok)
                    {
                        if (!FlagOps.HasFlags(
                                flags, PluginFlags.NoInitializeFlag, true))
                        {
                            Interlocked.Decrement(ref initializeCount);
                        }

                        //
                        // NOTE: Returning the unloaded plugin name
                        //       and version is HIGHLY RECOMMENDED
                        //       here.
                        //
                        if (!FlagOps.HasFlags(
                                flags, PluginFlags.NoResult, true))
                        {
                            //
                            // NOTE: Grab the plugin version now, if
                            //       necessary, since we know that
                            //       we need it.
                            //
                            if (version == null)
                                version = this.Version;

                            result = StringList.MakeList(
                                this.Name, version);
                        }
                    }
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPluginData Members
        /// <summary>
        ///   The flags for this plugin class and instance combined.  See the
        ///   PluginFlags enumeration for a full list of values and their
        ///   associated meanings.  This will normally be set based on the
        ///   plugin data provided to the constructor of this class; however,
        ///   it can be manually reset at any time.
        /// </summary>
        private PluginFlags flags;
        public virtual PluginFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The version for this plugin class.  This will normally be set
        ///   based on the plugin data provided to the constructor of this
        ///   class; however, it can be manually reset at any time.
        /// </summary>
        private Version version;
        public virtual Version Version
        {
            get { return version; }
            set { version = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The URI associated with this plugin class.  This should have a
        ///   value that represents the origin of the plugin.  This will
        ///   normally be set based on the plugin data provided to the
        ///   constructor of this class; however, it can be manually reset at
        ///   any time.  The exact format of this URI is unspecified; however,
        ///   it may contain the name and/or version of the plugin.
        /// </summary>
        private Uri uri;
        public virtual Uri Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The update URI associated with this plugin class.  This should
        ///   have a value that can be used to check for updates.  This will
        ///   normally be set based on the plugin data provided to the
        ///   constructor of this class; however, it can be manually reset at
        ///   any time.  The exact format of this URI is unspecified; however,
        ///   it may contain the name and/or version of the plugin.
        /// </summary>
        private Uri updateUri;
        public virtual Uri UpdateUri
        {
            get { return updateUri; }
            set { updateUri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The application domain hosting this plugin instance.  When the
        ///   plugin has been loaded into an isolated application domain, this
        ///   will always be a different application domain than the one
        ///   containing the parent interpreter.  Therefore, care must be taken
        ///   to avoid using parameter types that cannot be easily marshalled
        ///   between application domains when calling instance methods of the
        ///   interpreter or other core library components.  Types in the .NET
        ///   Framework and/or the core library that are marked as serializable
        ///   and/or derive from [Script]MarshalByRefObject should always be
        ///   safe to use when calling such methods.  This will normally be set
        ///   based on the plugin data provided to the constructor of this
        ///   class; however, it can be manually reset at any time.
        /// </summary>
        private AppDomain appDomain;
        public virtual AppDomain AppDomain
        {
            get { return appDomain; }
            set { appDomain = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The assembly containing this plugin instance.  When the plugin
        ///   has been loaded into an isolated application domain, this value
        ///   will be null.  This will normally be set based on the plugin data
        ///   provided to the constructor of this class; however, it can be
        ///   manually reset at any time.
        /// </summary>
        private Assembly assembly;
        public virtual Assembly Assembly
        {
            get { return assembly; }
            set { assembly = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The name for the assembly containing this plugin instance.  This
        ///   will normally be set based on the plugin data provided to the
        ///   constructor of this class; however, it can be manually reset at
        ///   any time.
        /// </summary>
        private AssemblyName assemblyName;
        public virtual AssemblyName AssemblyName
        {
            get { return assemblyName; }
            set { assemblyName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The creation time of the assembly containing this plugin
        ///   instance.  This will normally be set based on the plugin data
        ///   provided to the constructor of this class; however, it can be
        ///   manually reset at any time.
        /// </summary>
        private DateTime? dateTime;
        public virtual DateTime? DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The full path and file name for the assembly containing this
        ///   plugin instance.  This will normally be set based on the plugin
        ///   data provided to the constructor of this class; however, it can
        ///   be manually reset at any time.
        /// </summary>
        private string fileName;
        public virtual string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The full name for the type that implements this plugin instance.
        ///   This will normally be set based on the plugin data provided to
        ///   the constructor of this class; however, it can be manually reset
        ///   at any time.
        /// </summary>
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The list of commands associated with this plugin instance.  This
        ///   will be populated by the core library while the plugin is being
        ///   loaded.  It should not normally be modified by the plugin class.
        /// </summary>
        private CommandDataList commands;
        public virtual CommandDataList Commands
        {
            get { return commands; }
            set { commands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The list of policies associated with this plugin instance.  This
        ///   will be populated by the core library while the plugin is being
        ///   loaded into the interpreter.  It should not normally be modified
        ///   by the plugin class.
        /// </summary>
        private PolicyDataList policies;
        public virtual PolicyDataList Policies
        {
            get { return policies; }
            set { policies = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The list of command tokens associated with this plugin instance.
        ///   This will be populated by the core library while the plugin is
        ///   being loaded into the interpreter.
        /// </summary>
        private LongList commandTokens;
        public virtual LongList CommandTokens
        {
            get { return commandTokens; }
            set { commandTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The list of function tokens associated with this plugin instance.
        ///   This will be populated by the core library while the plugin is
        ///   being loaded into the interpreter.
        /// </summary>
        private LongList functionTokens;
        public virtual LongList FunctionTokens
        {
            get { return functionTokens; }
            set { functionTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The list of policy tokens associated with this plugin instance.
        ///   This will be populated by the core library while the plugin is
        ///   being loaded into the interpreter.
        /// </summary>
        private LongList policyTokens;
        public virtual LongList PolicyTokens
        {
            get { return policyTokens; }
            set { policyTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The list of trace tokens associated with this plugin instance.
        ///   This will be populated by the core library while the plugin is
        ///   being loaded into the interpreter.
        /// </summary>
        private LongList traceTokens;
        public virtual LongList TraceTokens
        {
            get { return traceTokens; }
            set { traceTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The resource manager associated with this plugin instance.  This
        ///   will be used as the basis for locating the resource strings
        ///   requested via the GetString method.  This will normally be set
        ///   based on the plugin data provided to the constructor of this
        ///   class; however, it can be manually reset at any time.
        /// </summary>
        private ResourceManager resourceManager;
        public virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   The auxiliary data associated with this plugin instance.  This
        ///   is reserved for use by the plugin itself.  The core library does
        ///   not use this property.
        /// </summary>
        private ObjectDictionary auxiliaryData;
        public virtual ObjectDictionary AuxiliaryData
        {
            get { return auxiliaryData; }
            set { auxiliaryData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        ///   The token for this plugin instance.  This will normally be set
        ///   based on the plugin data provided to the constructor of this
        ///   class; however, it can be manually reset at any time.
        /// </summary>
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        #region INotify Members
        /// <summary>
        ///   This method is supposed to calculate and return the notification
        ///   types supported by this plugin instance.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.
        /// </param>
        ///
        /// <returns>
        ///   The notification types supported by this plugin instance.
        /// </returns>
        public virtual NotifyType GetTypes(
            Interpreter interpreter
            )
        {
            return NotifyType.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is supposed to calculate and return the notification
        ///   flags supported by this plugin instance.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.
        /// </param>
        ///
        /// <returns>
        ///   The notification flags supported by this plugin instance.
        /// </returns>
        public virtual NotifyFlags GetFlags(
            Interpreter interpreter
            )
        {
            return NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library when the plugin needs
        ///   to receive a notification supported by it.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="eventArgs">
        ///   The context data associated with this notification.  This
        ///   parameter may be null.
        /// </param>
        ///
        /// <param name="clientData">
        ///   The client data associated with this notification.  This
        ///   parameter may be null.
        /// </param>
        ///
        /// <param name="arguments">
        ///   The script arguments associated with this notification.  This
        ///   parameter may be null.
        /// </param>
        ///
        /// <param name="result">
        ///   The result associated with this notification.  This parameter
        ///   is used for input and output and may be null.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode Notify(
            Interpreter interpreter,
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteRequest Members
        /// <summary>
        ///   This optional method is designed to handle arbitrary execution
        ///   requests from other plugins and/or the interpreter itself.  It
        ///   is legal to return success without performing any action.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="clientData">
        ///   The extra data to be used when servicing the execution request,
        ///   if any.  This parameter must be treated as strictly optional when
        ///   servicing the execution request.  Any execution request that
        ///   would succeed when this parameter is non-null must also succeed
        ///   when this parameter is null.
        /// </param>
        ///
        /// <param name="request">
        ///   The object must contain the data required to service the
        ///   execution request, if any.  If the execution request can be
        ///   properly serviced without any data, this parameter may be null.
        /// </param>
        ///
        /// <param name="response">
        ///   This object must be modified to contain the result of the
        ///   execution request, if any.  If the execution request does not
        ///   require data to be included in the response, this parameter
        ///   may be modified to contain null.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.
        ///   Upon failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            object request,
            ref object response,
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        /// <summary>
        ///   This method is called after <see cref="Initialize" />, if it was
        ///   successful.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="clientData">
        ///   The extra data supplied to the <see cref="Initialize" /> method,
        ///   if any.
        /// </param>
        public virtual void PostInitialize(
            Interpreter interpreter,
            IClientData clientData
            )
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is used to obtain a <see cref="Type" /> or instance
        ///   of a type within the plugin.
        /// </summary>
        ///
        /// <param name="id">
        ///   The <see cref="Guid" /> value associated with the
        ///   <see cref="ObjectId" /> attribute for the target type.
        /// </param>
        ///
        /// <param name="flags">
        ///   These flags determine the semantics of the lookup process used
        ///   to locate the target type.
        /// </param>
        ///
        /// <param name="result">
        ///   Upon success, the <see cref="Result.Value" /> property will be
        ///   the target type iself (<see cref="Type" />) or an instance of
        ///   it.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode GetFramework(
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library when a named stream
        ///   is needed.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="name">
        ///   The name of the stream to return.  This parameter may not be
        ///   null.
        /// </param>
        ///
        /// <param name="cultureInfo">
        ///   The target culture for the resource string to return.  This
        ///   parameter may be null to indicate the invariant culture.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.  Upon
        ///   failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   The requested stream upon success or null upon failure.
        /// </returns>
        public virtual Stream GetStream(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library when a resource string
        ///   is needed.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="name">
        ///   The name of the resource string to return.  This parameter may
        ///   not be null.
        /// </param>
        ///
        /// <param name="cultureInfo">
        ///   The target culture for the resource string to return.  This
        ///   parameter may be null to indicate the invariant culture.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.  Upon
        ///   failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   The requested resource string upon success or null upon failure.
        /// </returns>
        public virtual string GetString(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method may be called to request the URI for this plugin
        ///   class (or instance).
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="name">
        ///   The name of the URI being requested -OR- null to return the
        ///   default URI for the plugin.
        /// </param>
        ///
        /// <param name="cultureInfo">
        ///   The target culture for the URI to return.  This parameter may
        ///   be null to indicate the invariant culture.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.  Upon
        ///   failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        ///
        /// </returns>
        public virtual Uri GetUri(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method may be called to request the license certificate
        ///   file name for this plugin class (or instance).
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="name">
        ///   The name of the license certificate file name being requested
        ///   -OR- null to return the default license certificate file name
        ///   for the plugin.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.  Upon
        ///   failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   The requested certificate file name upon success or null upon
        ///   failure.
        /// </returns>
        public virtual string GetCertificateFileName(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library to request licensing
        ///   information about this plugin class (or instance).
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="name">
        ///   The name of the license certificate being requested -OR- null
        ///   to return the default license certificate for the plugin.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.  Upon
        ///   failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   The requested identifier upon success or null upon failure.
        /// </returns>
        public virtual IIdentifier GetCertificate(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library to request key pair
        ///   information about this plugin class (or instance).
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="name">
        ///   The name of the license certificate being requested -OR- null
        ///   to return the default license certificate for the plugin.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.  Upon
        ///   failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   The requested identifier upon success or null upon failure.
        /// </returns>
        public virtual IIdentifier GetKeyPair(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library to request key ring
        ///   information about this plugin class (or instance).
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="name">
        ///   The name of the license certificate being requested -OR- null
        ///   to return the default license certificate for the plugin.
        /// </param>
        ///
        /// <param name="error">
        ///   Upon success, the value of this parameter is undefined.  Upon
        ///   failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   The requested identifier upon success or null upon failure.
        /// </returns>
        public virtual IIdentifier GetKeyRing(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library to request additional
        ///   human-readable information about this plugin class (or instance)
        ///   be written to the interpreter host, if applicable.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="result">
        ///   Upon success, this must contain the requested information.
        ///   Upon failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode Banner(
            Interpreter interpreter,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library to request additional
        ///   human-readable information about this plugin class (or instance).
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="result">
        ///   Upon success, this must contain the requested information.
        ///   Upon failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library to request the list of
        ///   compile-time and/or runtime options for this plugin class (or
        ///   instance).
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="result">
        ///   Upon success, this must contain the requested information.
        ///   Upon failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode Options(
            Interpreter interpreter,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        ///   This method is called by the core library to request its status
        ///   string, if any.  The intent of the status string is that it may
        ///   be included with the overall core library version.
        /// </summary>
        ///
        /// <param name="interpreter">
        ///   The interpreter context we are executing in.  This parameter may
        ///   be null.
        /// </param>
        ///
        /// <param name="result">
        ///   Upon success, this must contain the requested information.
        ///   Upon failure, this must contain an appropriate error message.
        /// </param>
        ///
        /// <returns>
        ///   ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public virtual ReturnCode Status(
            Interpreter interpreter,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
