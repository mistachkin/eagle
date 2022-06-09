/*
 * Class11.cs --
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

namespace Sample
{
    /// <summary>
    /// This is an example "custom interactive extension command" class
    /// that inherits default functionality and implements the appropriate
    /// interface(s).
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("e2219c9b-773d-4066-896b-1e61f9fc415b")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("example")]
    [ObjectName("#class11")] /* HACK: Force new [interactive] name. */
    internal sealed class Class11 : Eagle._Commands.Default
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
        public Class11(
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

            //
            // NOTE: Override the default command description for use by the
            //       interactive help system.
            //
            this.Description = "This command demonstrates how to create an " +
                "interactive extension command with a custom help message.";

            //
            // NOTE: Override the default command syntax to include the list
            //       of all arguments, both required and optional, supported
            //       by this custom interactive extension command.
            //
            this.Syntax = "string ?arg ...?";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members (Required)
        /// <summary>
        /// Execute the command and return the appropriate result and return
        /// code.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this command was initially created,
        /// if any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to this command by the script
        /// being evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the command.
        /// Upon failure, this must contain an appropriate error message.
        /// If no result is explicitly set by the command, a default result
        /// of null will be used by the engine.
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
                    this, 1, arguments, this.Syntax);

                return ReturnCode.Error;
            }

            result = arguments.ToString();
            return ReturnCode.Ok;
        }
        #endregion
    }
}
