/*
 * Exit.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>exit</c> command, which terminates
    /// the interpreter (and, by default, the host application) with an
    /// optional exit code and message.  In a "safe" interpreter the command
    /// only marks the interpreter as exited rather than exiting the host
    /// application.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("c950492b-f20d-41b8-8a6f-b4c2ce7cbba6")]
    /*
     * POLICY: We disallow certain "unsafe" options.  In a "safe" interpreter,
     *         the only thing this command will do is cause the interpreter
     *         to be marked as exited (i.e. no interpreter state information
     *         will be lost and the host application will not exit).
     */
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Exit : Core
    {
        #region Private Constants
        /// <summary>
        /// The error message used when this command is invoked with an
        /// incorrect number of arguments.
        /// </summary>
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"exit ?options? ?returnCode?\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>exit</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Exit(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>exit</c> command.  It parses any
        /// supported options and the optional exit code, then requests that
        /// the interpreter (and, unless running in a "safe" interpreter, the
        /// host application) exit with the resolved exit code.
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
        /// command name; the remaining elements supply any options and an
        /// optional exit code.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter is null, the
        /// argument list is null, the wrong number of arguments is supplied,
        /// an option or exit code is invalid, or the exit request fails, with
        /// details placed in <paramref name="result" />.
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

            if (arguments.Count == 0)
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            ReturnCode code;

            OptionDictionary options =
                CommandOptions.GetCommandOptions(
                    CommandOptionType.Exit);

            int argumentIndex = Index.Invalid;

            if (arguments.Count > 1)
            {
                code = interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid,
                    false, ref argumentIndex, ref result);
            }
            else
            {
                code = ReturnCode.Ok;
            }

            if (code == ReturnCode.Ok)
            {
                if ((argumentIndex == Index.Invalid) ||
                    ((argumentIndex + 1) == arguments.Count))
                {
                    IVariant value = null;
                    string message = null;

                    if (options.IsPresent("-message", ref value))
                        message = value.ToString();

                    ExitCode exitCode = ResultOps.SuccessExitCode();

                    if (options.IsPresent("-current"))
                        exitCode = interpreter.ExitCodeNoThrow;

                    bool force = false;

                    if (options.IsPresent("-force"))
                        force = true;

                    bool fail = false;

                    if (options.IsPresent("-fail"))
                        fail = true;

                    bool noDispose = false;

                    if (options.IsPresent("-nodispose"))
                        noDispose = true;

                    bool noComplain = false;

                    if (options.IsPresent("-nocomplain"))
                        noComplain = true;

                    //
                    // NOTE: Was an explicit exit code specified?
                    //
                    if (argumentIndex != Index.Invalid)
                    {
                        object enumValue = EnumOps.TryParse(
                            typeof(ExitCode), arguments[argumentIndex],
                            true, true, ref result);

                        if (enumValue is ExitCode)
                        {
                            exitCode = (ExitCode)enumValue;
                        }
                        else
                        {
                            result = ScriptOps.BadValue(null,
                                "exit code", arguments[argumentIndex],
                                Enum.GetNames(typeof(ExitCode)), null,
                                ", or an integer");

                            code = ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: Make sure we succeeded at converting the
                    //       exit code to an integer.
                    //
                    if (code == ReturnCode.Ok)
                    {
                        code = RuntimeOps.Exit(
                            interpreter, clientData, arguments, message,
                            exitCode, force, fail, noDispose, noComplain,
                            ref result);

                        if (code == ReturnCode.Ok)
                            result = String.Empty;
                    }
                }
                else
                {
                    if ((argumentIndex != Index.Invalid) &&
                        Option.LooksLikeOption(arguments[argumentIndex]))
                    {
                        result = OptionDictionary.BadOption(
                            options, arguments[argumentIndex],
                            !interpreter.InternalIsSafe());
                    }
                    else
                    {
                        result = WrongNumArgs;
                    }

                    code = ReturnCode.Error;
                }
            }

            return code;
        }
        #endregion
    }
}
