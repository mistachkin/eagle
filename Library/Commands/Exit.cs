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
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"exit ?options? ?returnCode?\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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

            if (arguments.Count == 0)
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            ReturnCode code;

            OptionDictionary options = new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-message", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-force", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-fail", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-nodispose", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-current",
                    null),
                Option.CreateEndOfOptions()
            });

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
                    // NOTE: Make sure we succeeded at coverting the
                    //       exit code to an integer.
                    //
                    if (code == ReturnCode.Ok)
                    {
                        code = RuntimeOps.Exit(
                            interpreter, message, exitCode, force,
                            fail, noDispose, noComplain, ref result);

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
