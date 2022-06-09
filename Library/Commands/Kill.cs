/*
 * Kill.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

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
    [ObjectId("0fa1453c-4d4b-4141-af47-68197f25eee7")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Kill : Core
    {
        public Kill(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),   // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null), // simple switch
                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                            {
                                bool all = false;

                                if (options.IsPresent("-all"))
                                    all = true;

                                bool self = true;

#if SHELL
                                if (interpreter.IsKioskLock())
                                    self = false;
#endif

                                bool force = false;

                                if (options.IsPresent("-force"))
                                    force = true;

                                code = ProcessOps.KillProcess(
                                    interpreter, arguments[argumentIndex],
                                    interpreter.InternalCultureInfo, all,
                                    self, force, ref result);
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(
                                        options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    result = "wrong # args: should be \"kill ?-all? ?-force? ?--? process\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"kill ?-all? ?-force? ?--? process\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
