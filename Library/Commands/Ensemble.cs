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
    [ObjectId("310a4c0c-135d-4ded-b2b9-ed2d2182f2ef")]
    [CommandFlags(CommandFlags.Ensemble)]
    [ObjectGroup("ensemble")]
    public class Ensemble : Default, IEnsembleData, IEnsembleManager
    {
        #region Private Data
        private ICommandData commandData;
        private IEnsembleData ensembleData;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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

        public Ensemble(
            ICommandData commandData,
            IEnsembleData ensembleData
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
        private ISubCommand GetCoreSubCommand()
        {
            return SubCommandExecute as ISubCommand;
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetupSubCommands(
            ICommandData commandData,
            IEnsembleData ensembleData
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
        public override ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
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
        private IExecute subCommandExecute;
        public virtual IExecute SubCommandExecute
        {
            get { return subCommandExecute; }
            set { subCommandExecute = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsembleManager Members
        public virtual ReturnCode AddOrUpdateSubCommand(
            Interpreter interpreter,
            string name,
            ISubCommand subCommand,
            IClientData clientData,
            SubCommandFlags flags,
            ref Result error
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
                interpreter.ClearArgumentCache();
            }
#endif

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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
        [ObjectId("b3412397-935b-412d-937b-67341cca3f2d")]
        [ObjectGroup("ensemble")]
        internal sealed class SubCommand : _SubCommands.Default
        {
            #region Public Constructors
            public SubCommand(
                ISubCommandData subCommandData,
                IPlugin plugin
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
            private IPlugin plugin;
            public IPlugin Plugin
            {
                get { return plugin; }
                set { plugin = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IExecute Members
            public override ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
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
                    false, ref subCommand, ref tried, ref result);

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
