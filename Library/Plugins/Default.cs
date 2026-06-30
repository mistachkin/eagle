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
    /// <summary>
    /// This class provides the default, base implementation of the
    /// <see cref="IPlugin" /> interface.  Most plugins, including those
    /// built into the core library, derive from this class and override
    /// only the behavior they need to customize, inheriting the standard
    /// plugin functionality (e.g. command, policy, function, and trace
    /// management) from this class.
    /// </summary>
    [ObjectId("8c30d1ad-e753-4334-82ff-ea395e2542b5")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IPlugin
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <see cref="Default" /> plugin.
        /// This is the constructor used by the core library to create an
        /// instance of the plugin.  When the supplied <see cref="IPluginData" />
        /// is present, its identifying information (e.g. <see cref="Id" />,
        /// <see cref="Name" />, <see cref="Flags" />, <see cref="Version" />,
        /// assembly metadata, etc.) and any pre-built entity collections are
        /// copied into the new instance; otherwise, empty command, policy,
        /// and token collections are created.  A <see cref="ResourceManager" />
        /// (unless <see cref="PluginFlags.NoResources" /> is set) and an
        /// <see cref="AuxiliaryData" /> dictionary (unless
        /// <see cref="PluginFlags.NoAuxiliaryData" /> is set) are also
        /// established for the new instance.
        /// </summary>
        /// <param name="pluginData">
        /// An instance of the <see cref="IPluginData" /> component used to
        /// hold the data necessary to fully initialize the plugin instance.
        /// This parameter may be null.  Derived plugins are free to override
        /// this constructor; however, they are very strongly encouraged to
        /// call this constructor (i.e. the base class constructor) in that
        /// case.
        /// </param>
        public Default(
            IPluginData pluginData
            )
        {
            kind = IdentifierKind.Plugin;

            if ((pluginData == null) ||
                !FlagOps.HasFlags(pluginData.Flags,
                    PluginFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            //
            // NOTE: Is the supplied plugin data valid?
            //
            if (pluginData != null)
            {
                id = pluginData.Id;

                EntityOps.MaybeSetupId(this);

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
        /// Returns the name of the package to add to the
        /// <see cref="Interpreter" /> when this plugin is initialized (i.e.
        /// the name used with <c>[package provide]</c> via
        /// <see cref="Initialize" />).  Generally, this name is based on the
        /// name of the containing <see cref="Assembly" /> and/or the fully
        /// qualified type name.
        /// </summary>
        /// <param name="simple">
        /// When <c>true</c>, the simple (i.e. unqualified) name for the
        /// plugin should be used; otherwise, the more fully qualified name
        /// is used.
        /// </param>
        /// <returns>
        /// The name of the package to add to the <see cref="Interpreter" />
        /// that this plugin is being loaded into -OR- null if the name
        /// cannot be determined.
        /// </returns>
        protected virtual string GetPackageName(
            bool simple
            )
        {
            return RuntimeOps.GetPluginPackageName(this, simple);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the <see cref="PackageFlags" /> for the package to add to
        /// the <see cref="Interpreter" /> when this plugin is initialized.
        /// Since this name is always coming from a plugin, the base
        /// implementation simply returns <see cref="PackageFlags.Plugin" />.
        /// </summary>
        /// <returns>
        /// The <see cref="PackageFlags" /> for the package to add to the
        /// <see cref="Interpreter" /> that this plugin is being loaded into
        /// -OR- <see cref="PackageFlags.None" /> if they cannot be
        /// determined.
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
        /// The backing field for the <see cref="Name" /> property; holds the
        /// name of this plugin.  This will normally be set based on the
        /// <see cref="IPluginData" /> provided to the constructor of this
        /// class; however, it can be manually reset at any time.
        /// </summary>
        private string name;
        /// <summary>
        /// The name of this plugin.  This is typically the package name
        /// (e.g. as returned by <see cref="GetPackageName" />) and is used to
        /// identify the plugin within the <see cref="Interpreter" />.
        /// </summary>
        /// <value>
        /// The name of this plugin, or null if it has not been set.
        /// </value>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The backing field for the <see cref="Kind" /> property; holds the
        /// kind of identifier for this object instance.  For plugins, this
        /// should always be <see cref="IdentifierKind.Plugin" />.  It is set
        /// to that value by the constructor of this class.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// The <see cref="IdentifierKind" /> represented by this plugin
        /// instance; for plugins this is normally
        /// <see cref="IdentifierKind.Plugin" />.
        /// </summary>
        /// <value>
        /// The kind of identifier associated with this instance.
        /// </value>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Id" /> property; holds the
        /// unique identifier of this plugin class (or instance).  The default
        /// value is <see cref="Guid.Empty" />.  This will normally be derived
        /// from the <c>ObjectId</c> attribute (or the <see cref="IPluginData" />
        /// provided to the constructor of this class); however, it can be
        /// manually reset at any time.
        /// </summary>
        private Guid id;
        /// <summary>
        /// The unique <see cref="Guid" /> identifier associated with this
        /// plugin class (or instance).
        /// </summary>
        /// <value>
        /// The unique identifier for this plugin, or <see cref="Guid.Empty" />
        /// if none has been assigned.
        /// </value>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The backing field for the <see cref="ClientData" /> property; holds
        /// the <see cref="IClientData" /> passed to the original method that
        /// created this plugin instance.  This value will be passed to the
        /// <see cref="Initialize" /> and <see cref="Terminate" /> methods
        /// whenever they are called by the core library.  This will normally
        /// be set based on the <see cref="IPluginData" /> provided to the
        /// constructor of this class; however, it can be manually reset at
        /// any time.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// The <see cref="IClientData" /> instance associated with this
        /// plugin, passed to the <see cref="Initialize" /> and
        /// <see cref="Terminate" /> methods.
        /// </summary>
        /// <value>
        /// The anonymous client data for this plugin, which may be null.
        /// </value>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The backing field for the <see cref="Group" /> property; holds the
        /// logical group name for this plugin.  The default value is null.
        /// This property is not currently used by the core library.  This
        /// will normally be set based on the <see cref="IPluginData" />
        /// provided to the constructor of this class; however, it can be
        /// manually reset at any time.
        /// </summary>
        private string group;
        /// <summary>
        /// The logical group name associated with this plugin.  This is not
        /// currently used by the core library and is reserved for use by the
        /// plugin and/or its host application.
        /// </summary>
        /// <value>
        /// The group name for this plugin, which may be null.
        /// </value>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Description" /> property;
        /// holds the description associated with this plugin class (or
        /// instance).  The default value is null.  This property is not
        /// currently used by the core library.  This will normally be set
        /// based on the <see cref="IPluginData" /> provided to the
        /// constructor of this class; however, it can be manually reset at
        /// any time.
        /// </summary>
        private string description;
        /// <summary>
        /// The human-readable description associated with this plugin class
        /// (or instance).
        /// </summary>
        /// <value>
        /// The description for this plugin, which may be null.
        /// </value>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// The backing field for the <see cref="Initialized" /> property;
        /// holds the net number of times this plugin has been initialized
        /// minus the number of times it has been terminated.  It is
        /// manipulated atomically (via <see cref="Interlocked" />) and should
        /// generally only be set by the <see cref="Initialize" /> and
        /// <see cref="Terminate" /> methods.
        /// </summary>
        private int initializeCount;
        /// <summary>
        /// Non-zero if this plugin has (ever) been initialized within an
        /// <see cref="Interpreter" />.  This implements the
        /// <see cref="IState" /> portion of the <see cref="IPlugin" />
        /// contract; the getter reports <c>true</c> when the net
        /// initialization count is greater than zero, and the setter
        /// increments (when set to <c>true</c>) or decrements (when set to
        /// <c>false</c>) that count.
        /// </summary>
        /// <value>
        /// Non-zero if this plugin is currently considered initialized.
        /// </value>
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
        /// Initializes the plugin within the specified
        /// <see cref="Interpreter" /> and adds any contained commands,
        /// policies, and functions.  Subject to the configured
        /// <see cref="PluginFlags" />, this method loads the commands
        /// (via <c>AddCommands</c>) and policies (via <c>AddPolicies</c>)
        /// declared by the plugin, formally provides the package to the
        /// interpreter (via <c>PkgProvide</c> using
        /// <see cref="GetPackageName" /> and <see cref="GetPackageFlags" />),
        /// and then marks the plugin as initialized.
        /// WARNING: PLEASE DO NOT CHANGE THIS METHOD BECAUSE DERIVED PLUGINS
        ///          DEPEND ON ITS EXACT SEMANTICS.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  A
        /// valid interpreter is required unless the
        /// <see cref="PluginFlags.NoInitialize" /> flag is set.
        /// </param>
        /// <param name="clientData">
        /// The extra <see cref="IClientData" /> supplied when this plugin was
        /// initially created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational message (e.g. the
        /// plugin name and version).  Upon failure, this must contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
                        // NOTE: If this is a core library plugin -AND-
                        //       we are using the built-in command data,
                        //       make sure to skip querying the command
                        //       flags from the managed types.
                        //
                        CommandFlags commandFlags = CommandFlags.None;

                        if (PluginClientData.ShouldUseBuiltIns(clientData))
                            commandFlags |= CommandFlags.NoAttributes;

                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of loading all valid commands
                        //       (i.e. classes that implement ICommand,
                        //       directly or indirectly) in this plugin.
                        //
                        code = interpreter.AddCommands(
                            this, clientData, commandFlags, ref result);

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
                        // NOTE: If this is a core library plugin -AND-
                        //       we are using the built-in command data,
                        //       make sure to skip querying the command
                        //       flags from the managed types.
                        //
                        PolicyFlags policyFlags = PolicyFlags.None;

                        //
                        // TODO: There is no built-in data for policies
                        //       yet.  When that changes, enable this.
                        //
                        // if (PluginClientData.ShouldUseBuiltIns(clientData))
                        //     policyFlags |= PolicyFlags.NoAttributes;

                        //
                        // NOTE: Call the interpreter helper method that
                        //       takes care of loading all valid policies
                        //       (i.e. methods that are flagged as a "policy"
                        //       and are of the appropriate delegate type(s))
                        //       in this plugin.
                        //
                        code = interpreter.AddPolicies(
                            this, clientData, policyFlags, ref result);
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
                        // NOTE: Formally "provide" (i.e. announce)
                        //       this package to the interpreter so
                        //       that scripts can easily detect it.
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
        /// Terminates the plugin within the specified
        /// <see cref="Interpreter" /> and removes any contained traces,
        /// policies, functions, and commands.  Subject to the configured
        /// <see cref="PluginFlags" />, this method unloads the traces (via
        /// <c>RemoveTraces</c>), policies (via <c>RemovePolicies</c>),
        /// functions (via <c>RemoveFunctions</c>), and commands (via
        /// <c>RemoveCommands</c>) associated with the plugin, formally
        /// withdraws the package from the interpreter (via
        /// <c>WithdrawPackage</c>), and then marks the plugin as no longer
        /// initialized.  This is the inverse of <see cref="Initialize" />.
        /// WARNING: PLEASE DO NOT CHANGE THIS METHOD BECAUSE DERIVED PLUGINS
        ///          DEPEND ON ITS EXACT SEMANTICS.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  A
        /// valid interpreter is required unless the
        /// <see cref="PluginFlags.NoTerminate" /> flag is set.
        /// </param>
        /// <param name="clientData">
        /// The extra <see cref="IClientData" /> supplied when this plugin was
        /// initially created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational message (e.g. the
        /// plugin name and version).  Upon failure, this must contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
                            this, clientData, PolicyFlags.None,
                            ref result);
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

        #region ITypeAndName Members
        /// <summary>
        /// The backing field for the <see cref="TypeName" /> property; holds
        /// the full name for the type that implements this plugin instance.
        /// This will normally be set based on the <see cref="IPluginData" />
        /// provided to the constructor of this class; however, it can be
        /// manually reset at any time.
        /// </summary>
        private string typeName;
        /// <summary>
        /// The fully qualified name of the <see cref="Type" /> that
        /// implements this plugin instance.
        /// </summary>
        /// <value>
        /// The fully qualified type name for this plugin, which may be null.
        /// </value>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Type" /> property; holds the
        /// optional <see cref="Type" /> instance that corresponds to the type
        /// name specified by the associated <see cref="TypeName" /> string.
        /// </summary>
        private Type type;
        /// <summary>
        /// The <see cref="Type" /> that implements this plugin instance,
        /// corresponding to the configured <see cref="TypeName" />.
        /// </summary>
        /// <value>
        /// The implementing type for this plugin, which may be null.
        /// </value>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPluginData Members
        /// <summary>
        /// The backing field for the <see cref="Flags" /> property; holds the
        /// flags for this plugin class and instance combined.  See the
        /// <see cref="PluginFlags" /> enumeration for a full list of values
        /// and their associated meanings.  This will normally be set based on
        /// the <see cref="IPluginData" /> provided to the constructor of this
        /// class; however, it can be manually reset at any time.
        /// </summary>
        private PluginFlags flags;
        /// <summary>
        /// The <see cref="PluginFlags" /> for this plugin class and instance
        /// combined.  These flags govern much of the behavior of
        /// <see cref="Initialize" /> and <see cref="Terminate" />.
        /// </summary>
        /// <value>
        /// The combined plugin flags for this instance.
        /// </value>
        public virtual PluginFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Version" /> property; holds
        /// the version for this plugin class.  This will normally be set
        /// based on the <see cref="IPluginData" /> provided to the
        /// constructor of this class; however, it can be manually reset at
        /// any time.
        /// </summary>
        private Version version;
        /// <summary>
        /// The <see cref="Version" /> of this plugin.  This is reported
        /// (together with the <see cref="Name" />) when the package is
        /// provided to the <see cref="Interpreter" /> by
        /// <see cref="Initialize" />.
        /// </summary>
        /// <value>
        /// The version of this plugin, which may be null.
        /// </value>
        public virtual Version Version
        {
            get { return version; }
            set { version = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Uri" /> property; holds the
        /// URI associated with this plugin class.  This should have a value
        /// that represents the origin of the plugin.  This will normally be
        /// set based on the <see cref="IPluginData" /> provided to the
        /// constructor of this class; however, it can be manually reset at
        /// any time.  The exact format of this URI is unspecified; however,
        /// it may contain the name and/or version of the plugin.
        /// </summary>
        private Uri uri;
        /// <summary>
        /// The <see cref="Uri" /> representing the origin of this plugin.
        /// </summary>
        /// <value>
        /// The origin URI for this plugin, which may be null.
        /// </value>
        public virtual Uri Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="UpdateUri" /> property; holds
        /// the update URI associated with this plugin class.  This should
        /// have a value that can be used to check for updates.  This will
        /// normally be set based on the <see cref="IPluginData" /> provided
        /// to the constructor of this class; however, it can be manually
        /// reset at any time.  The exact format of this URI is unspecified;
        /// however, it may contain the name and/or version of the plugin.
        /// </summary>
        private Uri updateUri;
        /// <summary>
        /// The <see cref="Uri" /> that may be used to check for updates to
        /// this plugin.
        /// </summary>
        /// <value>
        /// The update URI for this plugin, which may be null.
        /// </value>
        public virtual Uri UpdateUri
        {
            get { return updateUri; }
            set { updateUri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="AppDomain" /> property; holds
        /// the application domain hosting this plugin instance.  When the
        /// plugin has been loaded into an isolated application domain, this
        /// will always be a different application domain than the one
        /// containing the parent <see cref="Interpreter" />.  Therefore, care
        /// must be taken to avoid using parameter types that cannot be easily
        /// marshalled between application domains when calling instance
        /// methods of the interpreter or other core library components.
        /// Types in the .NET Framework and/or the core library that are
        /// marked as serializable and/or derive from
        /// <c>[Script]MarshalByRefObject</c> should always be safe to use
        /// when calling such methods.  This will normally be set based on the
        /// <see cref="IPluginData" /> provided to the constructor of this
        /// class; however, it can be manually reset at any time.
        /// </summary>
        private AppDomain appDomain;
        /// <summary>
        /// The <see cref="AppDomain" /> hosting this plugin instance.
        /// </summary>
        /// <value>
        /// The application domain that hosts this plugin, which may be null.
        /// </value>
        public virtual AppDomain AppDomain
        {
            get { return appDomain; }
            set { appDomain = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Assembly" /> property; holds
        /// the assembly containing this plugin instance.  When the plugin has
        /// been loaded into an isolated application domain, this value will be
        /// null.  This will normally be set based on the
        /// <see cref="IPluginData" /> provided to the constructor of this
        /// class; however, it can be manually reset at any time.
        /// </summary>
        private Assembly assembly;
        /// <summary>
        /// The <see cref="Assembly" /> containing this plugin instance, or
        /// null when the plugin has been loaded into an isolated application
        /// domain.
        /// </summary>
        /// <value>
        /// The assembly that contains this plugin, which may be null.
        /// </value>
        public virtual Assembly Assembly
        {
            get { return assembly; }
            set { assembly = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="AssemblyName" /> property;
        /// holds the name for the assembly containing this plugin instance.
        /// This will normally be set based on the <see cref="IPluginData" />
        /// provided to the constructor of this class; however, it can be
        /// manually reset at any time.  It is especially useful when the
        /// <see cref="Assembly" /> itself is null due to isolation, since it
        /// allows the <see cref="ResourceManager" /> to be created from the
        /// name alone.
        /// </summary>
        private AssemblyName assemblyName;
        /// <summary>
        /// The <see cref="AssemblyName" /> of the assembly containing this
        /// plugin instance.
        /// </summary>
        /// <value>
        /// The name of the assembly that contains this plugin, which may be
        /// null.
        /// </value>
        public virtual AssemblyName AssemblyName
        {
            get { return assemblyName; }
            set { assemblyName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DateTime" /> property; holds
        /// the creation time of the assembly containing this plugin instance.
        /// This will normally be set based on the <see cref="IPluginData" />
        /// provided to the constructor of this class; however, it can be
        /// manually reset at any time.
        /// </summary>
        private DateTime? dateTime;
        /// <summary>
        /// The creation time of the <see cref="Assembly" /> containing this
        /// plugin instance.
        /// </summary>
        /// <value>
        /// The assembly creation time stamp, or null if it is unknown.
        /// </value>
        public virtual DateTime? DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FileName" /> property; holds
        /// the full path and file name for the assembly containing this
        /// plugin instance.  This will normally be set based on the
        /// <see cref="IPluginData" /> provided to the constructor of this
        /// class; however, it can be manually reset at any time.
        /// </summary>
        private string fileName;
        /// <summary>
        /// The full path and file name of the <see cref="Assembly" />
        /// containing this plugin instance.
        /// </summary>
        /// <value>
        /// The path to the assembly file for this plugin, which may be null.
        /// </value>
        public virtual string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Commands" /> property; holds
        /// the list of commands associated with this plugin instance.  This
        /// will be populated by the core library while the plugin is being
        /// loaded (e.g. by <see cref="Initialize" />).  It should not
        /// normally be modified by the plugin class.
        /// </summary>
        private CommandDataList commands;
        /// <summary>
        /// The <see cref="CommandDataList" /> of commands associated with
        /// this plugin instance.
        /// </summary>
        /// <value>
        /// The collection of commands provided by this plugin.
        /// </value>
        public virtual CommandDataList Commands
        {
            get { return commands; }
            set { commands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Policies" /> property; holds
        /// the list of policies associated with this plugin instance.  This
        /// will be populated by the core library while the plugin is being
        /// loaded into the <see cref="Interpreter" /> (e.g. by
        /// <see cref="Initialize" />).  It should not normally be modified by
        /// the plugin class.
        /// </summary>
        private PolicyDataList policies;
        /// <summary>
        /// The <see cref="PolicyDataList" /> of policies associated with this
        /// plugin instance.
        /// </summary>
        /// <value>
        /// The collection of policies provided by this plugin.
        /// </value>
        public virtual PolicyDataList Policies
        {
            get { return policies; }
            set { policies = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="CommandTokens" /> property;
        /// holds the list of command tokens associated with this plugin
        /// instance.  This will be populated by the core library while the
        /// plugin is being loaded into the <see cref="Interpreter" /> (e.g.
        /// by <see cref="Initialize" />).
        /// </summary>
        private LongList commandTokens;
        /// <summary>
        /// The <see cref="LongList" /> of command tokens registered by this
        /// plugin instance within the <see cref="Interpreter" />.
        /// </summary>
        /// <value>
        /// The collection of interpreter command tokens owned by this plugin.
        /// </value>
        public virtual LongList CommandTokens
        {
            get { return commandTokens; }
            set { commandTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="FunctionTokens" /> property;
        /// holds the list of function tokens associated with this plugin
        /// instance.  This will be populated by the core library while the
        /// plugin is being loaded into the <see cref="Interpreter" />.
        /// </summary>
        private LongList functionTokens;
        /// <summary>
        /// The <see cref="LongList" /> of function tokens registered by this
        /// plugin instance within the <see cref="Interpreter" />.
        /// </summary>
        /// <value>
        /// The collection of interpreter function tokens owned by this
        /// plugin.
        /// </value>
        public virtual LongList FunctionTokens
        {
            get { return functionTokens; }
            set { functionTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="PolicyTokens" /> property;
        /// holds the list of policy tokens associated with this plugin
        /// instance.  This will be populated by the core library while the
        /// plugin is being loaded into the <see cref="Interpreter" />.
        /// </summary>
        private LongList policyTokens;
        /// <summary>
        /// The <see cref="LongList" /> of policy tokens registered by this
        /// plugin instance within the <see cref="Interpreter" />.
        /// </summary>
        /// <value>
        /// The collection of interpreter policy tokens owned by this plugin.
        /// </value>
        public virtual LongList PolicyTokens
        {
            get { return policyTokens; }
            set { policyTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="TraceTokens" /> property;
        /// holds the list of trace tokens associated with this plugin
        /// instance.  This will be populated by the core library while the
        /// plugin is being loaded into the <see cref="Interpreter" />.
        /// </summary>
        private LongList traceTokens;
        /// <summary>
        /// The <see cref="LongList" /> of trace tokens registered by this
        /// plugin instance within the <see cref="Interpreter" />.
        /// </summary>
        /// <value>
        /// The collection of interpreter trace tokens owned by this plugin.
        /// </value>
        public virtual LongList TraceTokens
        {
            get { return traceTokens; }
            set { traceTokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ResourceManager" /> property;
        /// holds the resource manager associated with this plugin instance.
        /// This will be used as the basis for locating the resource strings
        /// requested via the <see cref="GetString" /> method (and streams via
        /// <see cref="GetStream" />).  This will normally be set based on the
        /// <see cref="IPluginData" /> provided to the constructor of this
        /// class (or created from the <see cref="Assembly" /> or
        /// <see cref="AssemblyName" />); however, it can be manually reset at
        /// any time.
        /// </summary>
        private ResourceManager resourceManager;
        /// <summary>
        /// The <see cref="ResourceManager" /> used to locate the resource
        /// strings and streams for this plugin instance.
        /// </summary>
        /// <value>
        /// The resource manager for this plugin, which may be null (e.g. when
        /// <see cref="PluginFlags.NoResources" /> is set).
        /// </value>
        public virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="AuxiliaryData" /> property;
        /// holds the auxiliary data associated with this plugin instance.
        /// This is reserved for use by the plugin itself.  The core library
        /// does not use this property.
        /// </summary>
        private ObjectDictionary auxiliaryData;
        /// <summary>
        /// The auxiliary <see cref="ObjectDictionary" /> associated with this
        /// plugin instance; this is reserved for use by the plugin itself.
        /// </summary>
        /// <value>
        /// The arbitrary per-plugin data dictionary, which may be null (e.g.
        /// when <see cref="PluginFlags.NoAuxiliaryData" /> is set).
        /// </value>
        public virtual ObjectDictionary AuxiliaryData
        {
            get { return auxiliaryData; }
            set { auxiliaryData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The backing field for the <see cref="Token" /> property; holds the
        /// token for this plugin instance.  This will normally be set based
        /// on the <see cref="IPluginData" /> provided to the constructor of
        /// this class (and may be zero at that point); however, it can be
        /// manually reset at any time.
        /// </summary>
        private long token;
        /// <summary>
        /// The token that identifies this plugin instance within the
        /// <see cref="Interpreter" />.
        /// </summary>
        /// <value>
        /// The interpreter wrapper token for this plugin, or zero if none has
        /// been assigned.
        /// </value>
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
        /// Calculates and returns the notification types supported by this
        /// plugin instance.  The base implementation supports no notification
        /// types and therefore returns <see cref="NotifyType.None" />.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.
        /// </param>
        /// <returns>
        /// The <see cref="NotifyType" /> values supported by this plugin
        /// instance.
        /// </returns>
        public virtual NotifyType GetTypes(
            Interpreter interpreter
            )
        {
            return NotifyType.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calculates and returns the notification flags supported by this
        /// plugin instance.  The base implementation supports no notification
        /// flags and therefore returns <see cref="NotifyFlags.None" />.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.
        /// </param>
        /// <returns>
        /// The <see cref="NotifyFlags" /> supported by this plugin instance.
        /// </returns>
        public virtual NotifyFlags GetFlags(
            Interpreter interpreter
            )
        {
            return NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called by the core library when the plugin needs to
        /// receive a notification supported by it (i.e. one matching the
        /// <see cref="NotifyType" /> and <see cref="NotifyFlags" /> reported
        /// by <see cref="GetTypes" /> and <see cref="GetFlags" />).  The base
        /// implementation does nothing and simply returns success.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="eventArgs">
        /// The <see cref="IScriptEventArgs" /> context data associated with
        /// this notification.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The <see cref="IClientData" /> associated with this notification.
        /// This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The <see cref="ArgumentList" /> of script arguments associated
        /// with this notification.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// The <see cref="Result" /> associated with this notification.  This
        /// parameter is used for input and output and may be null.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
        /// This optional method is designed to handle arbitrary execution
        /// requests from other plugins and/or the <see cref="Interpreter" />
        /// itself.  It is legal to return success without performing any
        /// action; the base implementation does exactly that.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra <see cref="IClientData" /> to be used when servicing the
        /// execution request, if any.  This parameter must be treated as
        /// strictly optional when servicing the execution request.  Any
        /// execution request that would succeed when this parameter is
        /// non-null must also succeed when this parameter is null.
        /// </param>
        /// <param name="request">
        /// The object that must contain the data required to service the
        /// execution request, if any.  If the execution request can be
        /// properly serviced without any data, this parameter may be null.
        /// </param>
        /// <param name="response">
        /// This object must be modified to contain the result of the
        /// execution request, if any.  If the execution request does not
        /// require data to be included in the response, this parameter may be
        /// modified to contain null.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
        /// This method is called after <see cref="Initialize" />, if it was
        /// successful, to give the plugin an opportunity to perform any
        /// follow-up work that must occur once initialization has completed.
        /// The base implementation does nothing.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra <see cref="IClientData" /> supplied to the
        /// <see cref="Initialize" /> method, if any.
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
        /// This method is used to obtain a <see cref="Type" /> or instance of
        /// a type within the plugin, identified by its <c>ObjectId</c>
        /// attribute.  The base implementation does not expose any framework
        /// types and therefore always fails.
        /// </summary>
        /// <param name="id">
        /// The <see cref="Guid" /> value associated with the <c>ObjectId</c>
        /// attribute for the target type.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The <see cref="FrameworkFlags" /> that determine the semantics of
        /// the lookup process used to locate the target type.
        /// </param>
        /// <param name="result">
        /// Upon success, the <see cref="Result.Value" /> property will be the
        /// target type itself (<see cref="Type" />) or an instance of it.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
        /// This method is called by the core library when a named stream is
        /// needed, typically resolved against the plugin
        /// <see cref="ResourceManager" />.  The base implementation provides
        /// no streams and therefore returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the <see cref="Stream" /> to return.  This parameter
        /// may not be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The target <see cref="CultureInfo" /> for the stream to return.
        /// This parameter may be null to indicate the invariant culture.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested <see cref="Stream" /> upon success or null upon
        /// failure.
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
        /// This method is called by the core library when a resource string
        /// is needed, typically resolved against the plugin
        /// <see cref="ResourceManager" />.  The base implementation provides
        /// no resource strings and therefore returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the resource string to return.  This parameter may not
        /// be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The target <see cref="CultureInfo" /> for the resource string to
        /// return.  This parameter may be null to indicate the invariant
        /// culture.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested resource string upon success or null upon failure.
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
        /// This method may be called to request the URI for this plugin class
        /// (or instance), such as the origin <see cref="Uri" /> or the
        /// <see cref="UpdateUri" />.  The base implementation provides no URIs
        /// and therefore returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the URI being requested -OR- null to return the
        /// default <see cref="Uri" /> for the plugin.
        /// </param>
        /// <param name="cultureInfo">
        /// The target <see cref="CultureInfo" /> for the URI to return.  This
        /// parameter may be null to indicate the invariant culture.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested <see cref="Uri" /> upon success or null upon
        /// failure.
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
        /// This method may be called to request the license certificate file
        /// name for this plugin class (or instance).  The base implementation
        /// provides no certificate file name and therefore returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the license certificate file name being requested -OR-
        /// null to return the default license certificate file name for the
        /// plugin.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested certificate file name upon success or null upon
        /// failure.
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
        /// This method is called by the core library to request licensing
        /// information (i.e. the license certificate) about this plugin class
        /// (or instance).  The base implementation provides no certificate
        /// and therefore returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the license certificate being requested -OR- null to
        /// return the default license certificate for the plugin.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested <see cref="IIdentifier" /> (representing the
        /// certificate) upon success or null upon failure.
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
        /// This method is called by the core library to request key pair
        /// information about this plugin class (or instance).  The base
        /// implementation provides no key pair and therefore returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the key pair being requested -OR- null to return the
        /// default key pair for the plugin.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested <see cref="IIdentifier" /> (representing the key
        /// pair) upon success or null upon failure.
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
        /// This method is called by the core library to request key ring
        /// information about this plugin class (or instance).  The base
        /// implementation provides no key ring and therefore returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the key ring being requested -OR- null to return the
        /// default key ring for the plugin.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested <see cref="IIdentifier" /> (representing the key
        /// ring) upon success or null upon failure.
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
        /// This method is called by the core library to request that
        /// additional human-readable information about this plugin class (or
        /// instance) be written to the interpreter host, if applicable.  The
        /// base implementation writes nothing and simply returns success.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the requested information.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
        /// This method is called by the core library to request additional
        /// human-readable "about" information (e.g. authorship or copyright
        /// details) about this plugin class (or instance).  The base
        /// implementation provides no such information and simply returns
        /// success.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the requested information.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
        /// This method is called by the core library to request the list of
        /// compile-time and/or runtime options for this plugin class (or
        /// instance).  The base implementation reports no options and simply
        /// returns success.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the requested information.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
        /// This method is called by the core library to request its status
        /// string, if any.  The intent of the status string is that it may be
        /// included with the overall core library version.  The base
        /// implementation provides no status string and simply returns
        /// success.
        /// </summary>
        /// <param name="interpreter">
        /// The <see cref="Interpreter" /> context we are executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the requested information.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success,
        /// <see cref="ReturnCode.Error" /> on failure.
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
