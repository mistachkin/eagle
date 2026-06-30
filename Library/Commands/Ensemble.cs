/*
 * Ensemble.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle ensemble command wrapper, which
    /// presents a single command whose invocation is dispatched to one of
    /// its named sub-commands.  It also supplies the built-in
    /// <c>about</c>, <c>isolated</c>, and <c>options</c> sub-commands.  See
    /// <c>core_language.md</c> for the ensemble syntax and semantics.
    /// </summary>
    [ObjectId("310a4c0c-135d-4ded-b2b9-ed2d2182f2ef")]
    [CommandFlags(
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.Ensemble
    )]
    [ObjectGroup("ensemble")]
    public class Ensemble : Default, IEnsembleData, IEnsembleManager
    {
        #region Private Data
        /// <summary>
        /// The data used to create and identify this command, saved for later
        /// use by the <see cref="Initialize" /> method.  This field may be
        /// null.
        /// </summary>
        private ICommandData commandData;

        /// <summary>
        /// The ensemble data associated with this command, saved for later use
        /// by the <see cref="Initialize" /> method.  This field may be null.
        /// </summary>
        private IEnsembleData ensembleData;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the Eagle ensemble command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Ensemble(
            ICommandData commandData
            )
            : base(commandData)
        {
            //
            // NOTE: This is not a strictly vanilla "command", it is a
            //       wrapped ensemble.
            //
            this.Kind |= IdentifierKind.Ensemble;

            //
            // NOTE: Normally, this flags assignment is performed by
            //       _Commands.Core for all commands residing in the core
            //       library; however, this class does not inherit from
            //       _Commands.Core.
            //
            if ((commandData == null) || !FlagOps.HasFlags(
                    commandData.Flags, CommandFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetCommandFlags(GetType().BaseType) |
                    AttributeOps.GetCommandFlags(this);
            }

            //
            // NOTE: Save the command data for later use by the Initialize
            //       method.  There is no ensemble data; therefore, set it
            //       to null.
            //
            this.commandData = commandData;
            this.ensembleData = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the Eagle ensemble command using the
        /// supplied ensemble data.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        /// <param name="ensembleData">
        /// The ensemble data that configures the sub-command dispatch for this
        /// command.  This parameter may be null.
        /// </param>
        public Ensemble(
            ICommandData commandData,  /* in */
            IEnsembleData ensembleData /* in */
            )
            : this(commandData)
        {
            //
            // NOTE: Save the command and ensemble data for later use by the
            //       Initialize method.
            //
            this.commandData = commandData;
            this.ensembleData = ensembleData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method returns the core sub-command handler for this ensemble,
        /// i.e. the <see cref="IExecute" /> configured to dispatch its
        /// sub-commands, cast to the <see cref="ISubCommand" /> interface.
        /// </summary>
        /// <returns>
        /// The core sub-command handler as an <see cref="ISubCommand" />, or
        /// null if the configured handler is not an <see cref="ISubCommand" />.
        /// </returns>
        private ISubCommand GetCoreSubCommand()
        {
            return SubCommandExecute as ISubCommand;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method establishes the sub-command handler and the dictionary
        /// of built-in sub-commands (<c>about</c>, <c>isolated</c>, and
        /// <c>options</c>) for this ensemble, creating them when they have not
        /// already been configured.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, used to build a
        /// default sub-command handler when one is not otherwise available.
        /// This parameter may be null.
        /// </param>
        /// <param name="ensembleData">
        /// The ensemble data that may supply a pre-configured sub-command
        /// handler.  This parameter may be null.
        /// </param>
        private void SetupSubCommands(
            ICommandData commandData,  /* in */
            IEnsembleData ensembleData /* in */
            )
        {
            //
            // NOTE: Get the IExecute configured to handle the sub-commands
            //       for this ensemble.
            //
            IExecute execute = this.SubCommandExecute;

            if (execute == null)
            {
                if (ensembleData != null)
                {
                    execute = ensembleData.SubCommandExecute;
                }
                else
                {
                    ISubCommandData subCommandData = null;

                    if (commandData != null)
                    {
                        subCommandData = new SubCommandData(
                            commandData.Name,
                            commandData.Group,
                            commandData.Description,
                            commandData.ClientData,
                            commandData.TypeName,
                            commandData.Type,
                            ScriptOps.GetSubCommandNameIndex(),
                            commandData.Flags,
                            SubCommandFlags.None,
                            this,
                            commandData.Token
                        );
                    }

                    execute = new SubCommand(subCommandData, this.Plugin);
                }

                //
                // NOTE: Set the IExecute that we either obtained from the
                //       passed IEnsembleData -OR- the one that we created
                //       ourselves.
                //
                this.SubCommandExecute = execute;
            }

            EnsembleDictionary subCommands = this.SubCommands;

            if (subCommands == null)
            {
                subCommands = new EnsembleDictionary();

                subCommands["about"] = execute as ISubCommand;
                subCommands["isolated"] = execute as ISubCommand;
                subCommands["options"] = execute as ISubCommand;

                this.SubCommands = subCommands;
            }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// This method initializes the ensemble command.  It first invokes the
        /// base class initialization and, upon success, establishes the
        /// sub-commands implemented directly by this class.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message produced
        /// by the base class initialization.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// from the base class initialization with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Initialize(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: Call the overridden method in the base class to start
            //       initialization.
            //
            ReturnCode code = base.Initialize(
                interpreter, clientData, ref result);

            if (code != ReturnCode.Ok)
                return code;

            //
            // NOTE: Setup the sub-commands that are implemented directly by
            //       this class.
            //
            SetupSubCommands(commandData, ensembleData);

            //
            // NOTE: Success.
            //
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsembleData Members
        /// <summary>
        /// The <see cref="IExecute" /> entity configured to dispatch the
        /// sub-commands for this ensemble.  This field may be null.
        /// </summary>
        private IExecute subCommandExecute;

        /// <summary>
        /// Gets or sets the <see cref="IExecute" /> entity configured to
        /// dispatch the sub-commands for this ensemble.
        /// </summary>
        public virtual IExecute SubCommandExecute
        {
            get { return subCommandExecute; }
            set { subCommandExecute = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsembleManager Members
        /// <summary>
        /// This method adds a sub-command to this ensemble, or updates the
        /// existing sub-command with the same name.  When no sub-command is
        /// supplied and the core flag is set, the core sub-command handler is
        /// used.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  When the
        /// argument cache is enabled, it is notified so that any cached
        /// arguments for the affected name can be cleared.  This parameter may
        /// be null.
        /// </param>
        /// <param name="name">
        /// The name of the sub-command to add or update.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="subCommand">
        /// The sub-command to associate with <paramref name="name" />.  This
        /// parameter may be null, in which case the core sub-command handler
        /// may be substituted depending on <paramref name="flags" />.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data associated with the sub-command,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the sub-command is added or updated,
        /// such as whether the core sub-command handler should be used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the name is null or the
        /// sub-command dictionary is not available, with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public virtual ReturnCode AddOrUpdateSubCommand(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ISubCommand subCommand,  /* in */
            IClientData clientData,  /* in */
            SubCommandFlags flags,   /* in */
            ref Result error         /* out */
            )
        {
            if (name == null)
            {
                error = "invalid sub-command name";
                return ReturnCode.Error;
            }

            EnsembleDictionary subCommands = this.SubCommands;

            if (subCommands == null)
            {
                error = "sub-commands not available";
                return ReturnCode.Error;
            }

            if ((subCommand == null) &&
                FlagOps.HasFlags(flags, SubCommandFlags.Core, true))
            {
                subCommand = GetCoreSubCommand();
            }

            subCommands[name] = subCommand;

            if (subCommand != null)
            {
                EnsembleDictionary subSubCommands = subCommand.SubCommands;

                if (subSubCommands != null)
                    subSubCommands[name] = subCommand;
            }

#if ARGUMENT_CACHE
            if (interpreter != null)
            {
                /* IGNORED */
                interpreter.MaybeClearArgumentCache(name);
            }
#endif

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the ensemble command.  It dispatches the
        /// invocation to the configured sub-command handler, which selects and
        /// runs the appropriate sub-command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; the remaining elements identify the sub-command and
        /// its arguments.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the dispatched
        /// sub-command.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The <see cref="ReturnCode" /> produced by the dispatched
        /// sub-command on success; otherwise, <see cref="ReturnCode.Error" />
        /// when no sub-command handler is configured, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            IExecute execute = this.SubCommandExecute;

            if (execute == null)
            {
                result = "invalid sub-command execute";
                return ReturnCode.Error;
            }

            return execute.Execute(
                interpreter, clientData, arguments, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal ISubCommand Class
        /// <summary>
        /// This class implements the default sub-command handler for an
        /// ensemble.  It dispatches the built-in <c>about</c>,
        /// <c>isolated</c>, and <c>options</c> sub-commands and forwards any
        /// other sub-command to the associated command.
        /// </summary>
        [ObjectId("b3412397-935b-412d-937b-67341cca3f2d")]
        [ObjectGroup("ensemble")]
        internal sealed class SubCommand : _SubCommands.Default
        {
            #region Public Constructors
            /// <summary>
            /// Constructs an instance of the ensemble sub-command handler.
            /// </summary>
            /// <param name="subCommandData">
            /// The data used to create and identify this sub-command, such as
            /// its name and flags.  This parameter may be null.
            /// </param>
            /// <param name="plugin">
            /// The plugin associated with this sub-command, saved for later
            /// use by the <see cref="Execute" /> method.  This parameter may
            /// be null.
            /// </param>
            public SubCommand(
                ISubCommandData subCommandData, /* in */
                IPlugin plugin                  /* in */
                )
                : base(subCommandData)
            {
                //
                // NOTE: Save the associated plugin (if any) for later use
                //       by the Execute method.
                //
                this.plugin = plugin;

                //
                // NOTE: Normally, this flags assignment is performed by
                //       _Commands.Core for all commands residing in the
                //       core library; however, this class does not inherit
                //       from _Commands.Core.
                //
                if ((subCommandData == null) || !FlagOps.HasFlags(
                        subCommandData.CommandFlags, CommandFlags.NoAttributes,
                        true))
                {
                    this.CommandFlags |=
                        AttributeOps.GetCommandFlags(GetType().BaseType) |
                        AttributeOps.GetCommandFlags(this);
                }

                //
                // NOTE: Setup the list of sub-commands that we _directly_
                //       support in our Execute method.
                //
                this.SubCommands = CreateSubCommands();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Static Methods
            /// <summary>
            /// This method creates the dictionary of built-in sub-commands
            /// (<c>about</c>, <c>isolated</c>, and <c>options</c>) that are
            /// handled directly by the <see cref="Execute" /> method.
            /// </summary>
            /// <returns>
            /// A newly created <see cref="EnsembleDictionary" /> populated with
            /// the names of the directly supported sub-commands.
            /// </returns>
            private static EnsembleDictionary CreateSubCommands()
            {
                EnsembleDictionary subCommands = new EnsembleDictionary();

                subCommands.Add("about", null);
                subCommands.Add("isolated", null);
                subCommands.Add("options", null);

                return subCommands;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IHavePlugin Members
            /// <summary>
            /// The plugin associated with this sub-command, used by the
            /// <see cref="Execute" /> method to service the built-in
            /// sub-commands.  This field may be null.
            /// </summary>
            private IPlugin plugin;

            /// <summary>
            /// Gets or sets the plugin associated with this sub-command, used
            /// by the <see cref="Execute" /> method to service the built-in
            /// sub-commands.
            /// </summary>
            public IPlugin Plugin
            {
                get { return plugin; }
                set { plugin = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IExecute Members
            /// <summary>
            /// This method executes the ensemble sub-command.  It selects the
            /// sub-command named in the arguments, services the built-in
            /// <c>about</c>, <c>isolated</c>, and <c>options</c> sub-commands
            /// directly, and forwards any other sub-command to the associated
            /// command.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context this sub-command is executing in.  This
            /// parameter should not be null.
            /// </param>
            /// <param name="clientData">
            /// The extra, command-specific data supplied when this sub-command
            /// was created, if any.  This parameter may be null.
            /// </param>
            /// <param name="arguments">
            /// The list of arguments for this invocation.  The element at the
            /// name index is the sub-command name; the remaining elements are
            /// its arguments.  This parameter should not be null.
            /// </param>
            /// <param name="result">
            /// Upon success, this contains the result produced by the selected
            /// sub-command.  Upon failure, this contains an appropriate error
            /// message.
            /// </param>
            /// <returns>
            /// <see cref="ReturnCode.Ok" /> on success; otherwise,
            /// <see cref="ReturnCode.Error" /> when the interpreter or argument
            /// list is null, too few arguments are supplied, the required
            /// plugin is unavailable, or an unknown sub-command is requested,
            /// with details placed in <paramref name="result" />.
            /// </returns>
            public override ReturnCode Execute(
                Interpreter interpreter, /* in */
                IClientData clientData,  /* in */
                ArgumentList arguments,  /* in */
                ref Result result        /* out */
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }

                int nameIndex = this.NameIndex;
                int nextIndex = nameIndex + 1;

                if (arguments.Count < nextIndex)
                {
                    result = String.Format(
                        "wrong # args: should be \"{0} option ?arg ...?\"",
                        this.Name);

                    return ReturnCode.Error;
                }

                ReturnCode code;
                string subCommand = arguments[nameIndex];
                bool tried = false;

                code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                    interpreter, this, clientData, arguments, true,
                    null, ref subCommand, ref tried, ref result);

                if ((code == ReturnCode.Ok) && !tried)
                {
                    switch (subCommand)
                    {
                        case "about":
                            {
                                if (arguments.Count == nextIndex)
                                {
                                    IPlugin plugin = this.Plugin;

                                    if (plugin != null)
                                    {
                                        code = plugin.About(
                                            interpreter, ref result);
                                    }
                                    else
                                    {
                                        result = "invalid sub-command plugin";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = String.Format(
                                        "wrong # args: should be \"{0} {1}\"",
                                        this.Name, subCommand);

                                    code = ReturnCode.Error;
                                }
                                break;
                            }
                        case "isolated":
                            {
                                if (arguments.Count == nextIndex)
                                {
                                    IPlugin plugin = this.Plugin;

                                    if (plugin != null)
                                    {
                                        result = AppDomainOps.IsCross(
                                            interpreter, plugin);

                                        code = ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        result = "invalid sub-command plugin";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = String.Format(
                                        "wrong # args: should be \"{0} {1}\"",
                                        this.Name, subCommand);

                                    code = ReturnCode.Error;
                                }
                                break;
                            }
                        case "options":
                            {
                                if (arguments.Count == nextIndex)
                                {
                                    IPlugin plugin = this.Plugin;

                                    if (plugin != null)
                                    {
                                        code = plugin.Options(
                                            interpreter, ref result);
                                    }
                                    else
                                    {
                                        result = "invalid sub-command plugin";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = String.Format(
                                        "wrong # args: should be \"{0} {1}\"",
                                        this.Name, subCommand);

                                    code = ReturnCode.Error;
                                }
                                break;
                            }
                        default:
                            {
                                ICommand command = this.Command;

                                if (command != null)
                                {
                                    //
                                    // BUGFIX: Use the entity execution wrapper
                                    //         provided by the interpreter so
                                    //         that hidden commands are handled
                                    //         correctly.
                                    //
                                    code = interpreter.Execute(
                                        command.Name, command, clientData,
                                        arguments, ref result);
                                }
                                else
                                {
                                    result = ScriptOps.BadSubCommand(
                                        interpreter, null, null, subCommand,
                                        this, null, null);

                                    code = ReturnCode.Error;
                                }
                                break;
                            }
                    }
                }

                return code;
            }
            #endregion
        }
        #endregion
    }
}
