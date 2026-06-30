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
    /// This class implements the Eagle <c>subst</c> command, which performs
    /// backslash, command, and variable substitutions on a string and returns
    /// the resulting value.  The <c>-nobackslashes</c>, <c>-nocommands</c>, and
    /// <c>-novariables</c> options selectively disable each kind of
    /// substitution.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("3bd40041-0cbc-47c3-8624-518b8fd6f0a3")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("engine")]
    internal sealed class Subst : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>subst</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Subst(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>subst</c> command.  It parses the
        /// substitution options, performs the requested backslash, command,
        /// and variable substitutions on the supplied string within a fresh
        /// substitution call frame, and returns the substituted value.
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
        /// command name; it is followed by any of the <c>-nobackslashes</c>,
        /// <c>-nocommands</c>, and <c>-novariables</c> options and then the
        /// string to be substituted.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the string after the requested
        /// substitutions have been applied.  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the substituted
        /// string placed in <paramref name="result" />; otherwise, a non-Ok
        /// value (e.g. <see cref="ReturnCode.Error" />) when the wrong number
        /// of arguments is supplied, an option is invalid, the interpreter is
        /// null, the argument list is null, or a substitution fails, with
        /// details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Subst);

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
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"subst\" body line {1})",
                                            Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                                }

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
