/*
 * Class12.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Commands = Eagle._Commands;
using _SubCommands = Eagle._SubCommands;

namespace Sample
{
    /// <summary>
    /// Declare a "custom command" class that inherits ensemble functionality
    /// and implements the appropriate interface(s).
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("4e37ebdb-e8cd-4bf8-9488-216af3f22c34")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("example")]
    internal sealed class Class12 : _Commands.Ensemble
    {
        #region Public Constructor (Required)
        /// <summary>
        /// Constructs an instance of this custom command class.
        /// </summary>
        /// <param name="commandData">
        /// An instance of the command data class containing the properties
        /// used to initialize the new instance of this custom command class.
        /// Typically, the only thing done with this parameter is to pass it
        /// along to the base class constructor.
        /// </param>
        public Class12(
            ICommandData commandData /* in */
            )
            : base(commandData)
        {
            //
            // NOTE: Typically, nothing much is done here.  Any non-trivial
            //       initialization should be done in IState.Initialize and
            //       cleaned up in IState.Terminate.
            //
            this.Flags |= Utility.GetCommandFlags(GetType().BaseType) |
                Utility.GetCommandFlags(this); /* HIGHLY RECOMMENDED */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members (Required)
        /// <summary>
        /// Optionally initializes any state information required by the
        /// command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this command was initially created, if
        /// any.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the command.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
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

            ///////////////////////////////////////////////////////////////////
            // **************** BEGIN CUSTOM SUB-COMMAND LIST *****************
            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The [class12 example1] sub-command is implemented by this
            //       class; however, it is re-dispatched via the core library.
            //
            code = base.AddOrUpdateSubCommand(
                "example1", null, null, SubCommandFlags.Core, ref result);

            if (code != ReturnCode.Ok)
                return code;

            //
            // NOTE: The [class12 example2] sub-command is implemented by this
            //       class.  It is handled locally without being re-dispatched
            //       by the core library.
            //
            code = base.AddOrUpdateSubCommand(
                "example2", null, null, SubCommandFlags.None, ref result);

            if (code != ReturnCode.Ok)
                return code;

            //
            // NOTE: The [class12 example3] sub-command is implemented by the
            //       private Example3 class (below).  It is dispatched by the
            //       core library.
            //
            code = base.AddOrUpdateSubCommand(
                "example3", new Example3(null), null, SubCommandFlags.None,
                ref result);

            if (code != ReturnCode.Ok)
                return code;

            ///////////////////////////////////////////////////////////////////
            // ***************** END CUSTOM SUB-COMMAND LIST ******************
            ///////////////////////////////////////////////////////////////////

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members (Required)
        /// <summary>
        /// Execute the sub-command and return the appropriate result and return
        /// code.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this sub-command was initially
        /// created, if any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to this sub-command by the script
        /// being evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the sub-command.
        /// Upon failure, this must contain an appropriate error message.
        /// If no result is explicitly set by the sub-command, a default
        /// result of null will be used by the engine.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.  Other
        /// return codes may be used to implement custom control structures.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: In truth, almost everything in this method is optional;
            //       however, there are certain conventions present in this
            //       example that custom commands are encouraged to follow.
            //
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                //
                // NOTE: We require a valid argument list.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // **************** BEGIN CUSTOM SUB-COMMAND CODE *****************
            ///////////////////////////////////////////////////////////////////

            if (arguments.Count < 2)
            {
                //
                // TODO: Use the ISyntax interface to get this error string
                //       from the "resource manager".
                //
                result = Utility.WrongNumberOfArguments(
                    this, 1, arguments, "option ?arg ...?");

                return ReturnCode.Error;
            }

            //
            // NOTE: This variable will hold the return code for this command
            //       execution.
            //
            ReturnCode code;

            //
            // NOTE: What is the name of the sub-command being executed?
            //
            string subCommand = arguments[1];

            //
            // NOTE: Has the sub-command already been executed?  This will be
            //       set within the TryExecuteSubCommandFromEnsemble method if
            //       necessary.
            //
            bool tried = false;

            //
            // NOTE: Check if the sub-command needs to be executed using the
            //       ensemble.  If so, this will also execute it, reset the
            //       "tried" parameter to true, and return its result.
            //
            code = Utility.TryExecuteSubCommandFromEnsemble(interpreter, this,
                clientData, arguments, true, false, ref subCommand, ref tried,
                ref result);

            //
            // NOTE: Does the sub-command need to be handled locally?  If so,
            //       determine which one is being requested and then execute
            //       the code for it; otherwise, skip local handling and let
            //       the existing return code and result stand.
            //
            if ((code == ReturnCode.Ok) && !tried)
            {
                switch (subCommand)
                {
                    case "example1":
                    case "example2":
                        {
                            if (arguments.Count == 2)
                            {
                                //
                                // NOTE: Execute "example?" sub-command,
                                //       which is defined to return the
                                //       next integer sequence number
                                //       used for various identifiers
                                //       associated with this interpreter.
                                //
                                result = interpreter.NextId();
                            }
                            else
                            {
                                //
                                // TODO: Use the ISyntax interface to get
                                //       this error string from the
                                //       "resource manager".
                                //
                                result = Utility.WrongNumberOfArguments(
                                    this, 2, arguments, null);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    default:
                        {
                            //
                            // NOTE: The sub-command is unknown and must
                            //       result in an error.
                            //
                            result = Utility.BadSubCommand(
                                interpreter, null, null, subCommand, this,
                                null, null);

                            code = ReturnCode.Error;
                            break;
                        }
                }
            }

            //
            // NOTE: Finally, return one of the standard status codes that
            //       indicate success / failure of this command OR a custom
            //       status code that indicates success / failure of this
            //       command.
            //
            return code;

            ///////////////////////////////////////////////////////////////////
            // ***************** END CUSTOM SUB-COMMAND CODE ******************
            ///////////////////////////////////////////////////////////////////
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private ISubCommand Class
        /// <summary>
        /// Declare a "custom sub-command" class that inherits default
        /// functionality and implements the appropriate interface(s).
        /// </summary>
        //
        // FIXME: Always change this GUID.
        //
        [ObjectId("2fb104fc-aa5b-4864-a4a5-81a67732665e")]
        [ObjectGroup("example")]
        private sealed class Example3 : _SubCommands.Default
        {
            #region Public Constructors
            public Example3(
                ISubCommandData subCommandData
                )
                : base(subCommandData)
            {
                //
                // NOTE: Normally, this flags assignment is performed by
                //       _Commands.Core for all commands residing in the
                //       core library; however, this class does not inherit
                //       from _Commands.Core.
                //
                this.CommandFlags |=
                    Utility.GetCommandFlags(GetType().BaseType) |
                    Utility.GetCommandFlags(this);

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

                subCommands.Add("example3", null);

                return subCommands;
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

                if (arguments.Count < 2)
                {
                    result = Utility.WrongNumberOfArguments(
                        this, 1, arguments, "option ?arg ...?");

                    return ReturnCode.Error;
                }

                ReturnCode code;
                string subCommand = arguments[1];
                bool tried = false;

                code = Utility.TryExecuteSubCommandFromEnsemble(
                    interpreter, this, clientData, arguments, true,
                    false, ref subCommand, ref tried, ref result);

                if ((code == ReturnCode.Ok) && !tried)
                {
                    switch (subCommand)
                    {
                        case "example3":
                            {
                                if (arguments.Count == 2)
                                {
                                    //
                                    // NOTE: Execute "example3" sub-command,
                                    //       which is defined to return the
                                    //       next integer sequence number
                                    //       used for various identifiers
                                    //       associated with this interpreter.
                                    //
                                    result = interpreter.NextId();
                                }
                                else
                                {
                                    result = Utility.WrongNumberOfArguments(
                                        this, 2, arguments, null);

                                    code = ReturnCode.Error;
                                }
                                break;
                            }
                        default:
                            {
                                result = Utility.BadSubCommand(
                                    interpreter, null, null, subCommand,
                                    this, null, null);

                                code = ReturnCode.Error;
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
