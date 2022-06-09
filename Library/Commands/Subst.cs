/*
 * Subst.cs --
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
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("3bd40041-0cbc-47c3-8624-518b8fd6f0a3")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("engine")]
    internal sealed class Subst : Core
    {
        public Subst(
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
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] { 
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nobackslashes", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocommands", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-novariables", null) /*,
                            Option.CreateEndOfOptions() */
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                            {
                                SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;

                                if (options.IsPresent("-nobackslashes"))
                                    substitutionFlags &= ~SubstitutionFlags.Backslashes;

                                if (options.IsPresent("-nocommands"))
                                    substitutionFlags &= ~SubstitutionFlags.Commands;

                                if (options.IsPresent("-novariables"))
                                    substitutionFlags &= ~SubstitutionFlags.Variables;

                                string name = StringList.MakeList("subst");

                                ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                    CallFrameFlags.Substitute);

                                interpreter.PushAutomaticCallFrame(frame);

                                code = interpreter.SubstituteString(
                                    arguments[argumentIndex], substitutionFlags, ref result);

                                if (code == ReturnCode.Error)
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"subst\" body line {1})",
                                            Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                                //
                                // NOTE: Pop the original call frame that we pushed above and 
                                //       any intervening scope call frames that may be leftover 
                                //       (i.e. they were not explicitly closed).
                                //
                                /* IGNORED */
                                interpreter.PopScopeCallFramesAndOneMore();
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
                                    result = "wrong # args: should be \"subst ?-nobackslashes? ?-nocommands? ?-novariables? string\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"subst ?-nobackslashes? ?-nocommands? ?-novariables? string\"";
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
