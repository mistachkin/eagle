/*
 * Switch.cs --
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
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>switch</c> command, which matches a
    /// string against a series of patterns and evaluates the body associated
    /// with the first pattern that matches.  Matching may be performed using
    /// exact, glob, regular expression, substring, or integer modes, and an
    /// optional <c>default</c> arm matches when no other pattern does.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("b4d8bb06-f6bf-4343-8b8a-b00184c14aa3")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("conditional")]
    internal sealed class Switch : Core
    {
        /// <summary>
        /// The reserved pattern word that, when it appears as the final
        /// pattern in the list, always matches the input string.
        /// </summary>
        private const string Default = "default";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the <c>switch</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Switch(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>switch</c> command.  It parses any
        /// leading switches that select the match mode and options (for
        /// example <c>-exact</c>, <c>-glob</c>, <c>-regexp</c>,
        /// <c>-substring</c>, <c>-integer</c>, <c>-nocase</c>, and
        /// <c>-subst</c>), then matches the input string against each pattern
        /// and evaluates the body of the first matching arm.  The pattern and
        /// body words may be supplied either as separate arguments or grouped
        /// into a single list argument.
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
        /// command name, followed by optional switches, the string to match,
        /// and the pattern/body pairs (optionally including a final
        /// <c>default</c> arm).  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the matching arm's body
        /// (or an empty string when nothing matched).  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when a matching arm's body (if any) is
        /// evaluated successfully; otherwise, a non-Ok value such as
        /// <see cref="ReturnCode.Error" /> when the arguments are invalid, an
        /// option is unrecognized, the pattern/body list is malformed, or the
        /// evaluated body itself fails, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    OptionDictionary options =
                        CommandOptions.GetCommandOptions(
                            CommandOptionType.Switch);

                    int argumentIndex = Index.Invalid;

                    if (arguments.Count > 1)
                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);
                    else
                        code = ReturnCode.Ok;

                    if (code == ReturnCode.Ok)
                    {
                        if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                        {
                            MatchMode mode = StringOps.DefaultSwitchMatchMode;

                            if (options.IsPresent("-integer"))
                                mode = MatchMode.Integer;
                            else if (options.IsPresent("-regexp"))
                                mode = MatchMode.RegExp;
                            else if (options.IsPresent("-glob"))
                                mode = MatchMode.Glob;
                            else if (options.IsPresent("-substring"))
                                mode = MatchMode.SubString;
                            else if (options.IsPresent("-exact"))
                                mode = MatchMode.Exact;

                            if (options.IsPresent("-subst"))
                                mode |= MatchMode.Substitute;

                            bool noCase = false;

                            if (options.IsPresent("-nocase"))
                                noCase = true;

                            bool splitList = false;
                            StringList list = null;
                            IScriptLocation location = null;

                            //
                            // NOTE: Is there only one argument following the string to match?
                            //
                            if ((argumentIndex + 2) == arguments.Count)
                            {
                                code = ListOps.GetOrCopyOrSplitList(
                                    interpreter, arguments[argumentIndex + 1], true, ref list,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if (list.Count > 0)
                                    {
                                        location = arguments[argumentIndex + 1];
                                        splitList = true;
                                    }
                                    else
                                    {
                                        result = "wrong # args: should be \"switch ?switches? string {pattern body ... ?default body?}\"";
                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                //
                                // TODO: Make sure this is always accurate.
                                //
                                code = ScriptOps.GetLocation(
                                    interpreter, arguments, argumentIndex + 1, ref location,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    list = ArgumentList.GetRangeAsStringList(arguments, argumentIndex + 1);
                                }
                            }

                            //
                            // NOTE: Ok, now we should have a list of patterns and bodies
                            //       if everything went Ok above.
                            //
                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Complain if there is an odd number of words in the
                                //       list of patterns and bodies.
                                //
                                if ((list.Count % 2) == 0)
                                {
                                    //
                                    // NOTE: Complain if the last body is a continuation.
                                    //
                                    if (!SharedStringOps.SystemEquals(
                                            list[list.Count - 1], Characters.MinusSign.ToString()))
                                    {
                                        //
                                        // NOTE: Get the text to match against.
                                        //
                                        string input = arguments[argumentIndex];

                                        //
                                        // NOTE: We need to return an empty string if we do not
                                        //       match anything.
                                        //
                                        result = String.Empty;

                                        //
                                        // NOTE: Search the patterns for a match.
                                        //
                                        for (int index = 0; index < list.Count; index += 2)
                                        {
                                            Result pattern = list[index];

                                            bool match = false;

                                            if ((index == (list.Count - 2)) &&
                                                SharedStringOps.SystemEquals(pattern, Switch.Default))
                                            {
                                                //
                                                // NOTE: Default pattern at end always matches.
                                                //
                                                match = true;
                                            }
                                            else
                                            {
                                                if ((mode & MatchMode.Substitute) == MatchMode.Substitute)
                                                    code = interpreter.SubstituteString(pattern, ref pattern);

                                                if (code != ReturnCode.Ok)
                                                {
                                                    result = pattern;
                                                    break;
                                                }

                                                code = StringOps.Match(
                                                    interpreter, mode, input, pattern, noCase, ref match,
                                                    ref result);

                                                if (code != ReturnCode.Ok)
                                                    break;
                                            }

                                            if (!match)
                                                continue;

                                            //
                                            // NOTE: We've got a match. Find a body to execute, skipping
                                            //       bodies that are "-".
                                            //
                                            for (int index2 = index + 1; ; index2 += 2)
                                            {
                                                if (index2 >= list.Count)
                                                {
                                                    result = "fall-out when searching for body to match pattern";
                                                    code = ReturnCode.Error;

                                                    goto switch_done;
                                                }

                                                if (!SharedStringOps.SystemEquals(
                                                        list[index2], Characters.MinusSign.ToString()))
                                                {
                                                    code = interpreter.EvaluateScript(list[index2], location, ref result);

                                                    if (code == ReturnCode.Error)
                                                    {
                                                        /* IGNORED */
                                                        Engine.AddErrorInformation(interpreter, result,
                                                            String.Format("{0}    (\"{1}\" arm line {2})",
                                                                Environment.NewLine, FormatOps.Ellipsis(pattern),
                                                                Interpreter.GetErrorLine(interpreter)));
                                                    }

                                                    goto switch_done;
                                                }
                                            }
                                        }

                                    switch_done:
                                        ;
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "no body specified for pattern \"{0}\"",
                                            list[list.Count - 2]);

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    Result error = "extra switch pattern with no body";

                                    if (splitList)
                                    {
                                        /*
                                         * Check if this can be due to a badly placed comment
                                         * in the switch block.
                                         *
                                         * The following is an heuristic to detect the infamous
                                         * "comment in switch" error: just check if a pattern
                                         * begins with '#'.
                                         */

                                        for (int index = 0; index < list.Count; index++)
                                        {
                                            if (!String.IsNullOrEmpty(list[index]) &&
                                                (list[index][0] == Characters.NumberSign))
                                            {
                                                error = "extra switch pattern with no body, " +
                                                        "this may be due to a comment " +
                                                        "incorrectly placed outside of a " +
                                                        "switch body - see the \"switch\" " +
                                                        "documentation";

                                                break;
                                            }
                                        }
                                    }

                                    result = error;
                                    code = ReturnCode.Error;
                                }
                            }
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
                                result = "wrong # args: should be \"switch ?switches? string pattern body ... ?default body?\"";
                            }

                            code = ReturnCode.Error;
                        }
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
