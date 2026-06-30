/*
 * Regsub.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text.RegularExpressions;
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
    /// This class implements the Eagle <c>regsub</c> command, which performs
    /// regular expression based substitution on an input string and either
    /// returns the resulting string or stores it in a variable while
    /// returning the number of matches.  It supports literal, script-eval,
    /// and command callback replacement modes along with the usual regular
    /// expression option switches.  See <c>core_language.md</c> for the
    /// command syntax and semantics.
    /// </summary>
    [ObjectId("2d0df297-03b8-4375-bd55-e3d9abd31a94")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Regsub : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>regsub</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Regsub(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>regsub</c> command.  It parses the
        /// option switches, compiles the regular expression pattern, performs
        /// the requested substitution over the input string (optionally
        /// honoring a start index and a match limit), and either returns the
        /// resulting string or, when a variable name is supplied, stores that
        /// string in the named variable and returns the number of matches.
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
        /// command name, followed by optional switches and then the pattern,
        /// input string, replacement specification, and an optional variable
        /// name.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains either the substituted string or, when
        /// a variable name was supplied, the number of matches.  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the arguments or options are
        /// invalid, the regular expression cannot be compiled, the
        /// replacement script fails, the interpreter is null, or the argument
        /// list is null, with details placed in <paramref name="result" />.
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
                    if (arguments.Count >= 4)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Regsub);

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, true, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) &&
                                ((argumentIndex + 2) < arguments.Count) &&
                                ((argumentIndex + 4) >= arguments.Count))
                            {
                                string pattern = arguments[argumentIndex];
                                string input = arguments[argumentIndex + 1];
                                string replacement = arguments[argumentIndex + 2];
                                IScriptLocation replacementLocation = arguments[argumentIndex + 2];
                                string varName = null;

                                if ((argumentIndex + 3) < arguments.Count)
                                    varName = arguments[argumentIndex + 3];

                                IVariant value = null;

                                RegexOptions regExOptions =
                                    StringOps.DefaultRegExSyntaxOptions;

                                if (options.IsPresent("-options", ref value))
                                    regExOptions = (RegexOptions)value.Value;

                                int valueIndex = Index.Invalid;
                                int length = input.Length;
                                int startIndex = 0;

                                if (options.IsPresent("-start", ref value))
                                {
                                    //
                                    // NOTE: Handle "end-X", etc.
                                    //
                                    code = Value.GetIndex(
                                        value.ToString(), length,
                                        ValueFlags.AnyIndex,
                                        interpreter.InternalCultureInfo,
                                        ref startIndex, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        if (startIndex < 0)
                                            startIndex = 0;

                                        if (startIndex > length)
                                            startIndex = length;
                                    }
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    int count = 1;

                                    if (options.IsPresent("-count", ref value))
                                        count = (int)value.Value;

                                    string text = null;
                                    IScriptLocation textLocation = null;

                                    if (options.IsPresent("-eval", ref value, ref valueIndex))
                                    {
                                        text = value.ToString();
                                        textLocation = arguments[valueIndex + 1];
                                    }

                                    bool command = false;

                                    if (options.IsPresent("-command"))
                                        command = true;

                                    if ((text == null) || !command)
                                    {
                                        bool verbatim = false;

                                        if (options.IsPresent("-verbatim"))
                                            verbatim = true;

                                        bool literal = false;

                                        if (options.IsPresent("-literal"))
                                            literal = true;

                                        bool all = false;

                                        if (options.IsPresent("-all"))
                                            all = true;

                                        bool quote = false;

                                        if (options.IsPresent("-quote"))
                                            quote = true;

                                        bool strict = true; // COMPAT: Tcl.

                                        if (options.IsPresent("-nostrict"))
                                            strict = false;

                                        bool extra = false; // COMPAT: Tcl.

                                        if (options.IsPresent("-extra"))
                                            extra = true;

                                        if (options.IsPresent("-ecma"))
                                            regExOptions |= RegexOptions.ECMAScript;

                                        if (options.IsPresent("-compiled"))
                                            regExOptions |= RegexOptions.Compiled;

                                        if (options.IsPresent("-explicit"))
                                            regExOptions |= RegexOptions.ExplicitCapture;

                                        if (options.IsPresent("-reverse"))
                                            regExOptions |= RegexOptions.RightToLeft;

                                        if (options.IsPresent("-expanded"))
                                            regExOptions |= RegexOptions.IgnorePatternWhitespace;

                                        if (options.IsPresent("-line"))
                                        {
                                            regExOptions &= ~RegexOptions.Singleline;
                                            regExOptions |= RegexOptions.Multiline;
                                        }

                                        if (options.IsPresent("-lineanchor"))
                                            regExOptions |= RegexOptions.Multiline;

                                        if (options.IsPresent("-linestop"))
                                            regExOptions &= ~RegexOptions.Singleline;

                                        if (options.IsPresent("-nocase"))
                                            regExOptions |= RegexOptions.IgnoreCase;

                                        if (options.IsPresent("-noculture"))
                                            regExOptions |= RegexOptions.CultureInvariant;

                                        Regex regEx = null;

                                        try
                                        {
                                            regEx = RegExOps.Create(pattern, regExOptions);
                                        }
                                        catch (Exception e)
                                        {
                                            Engine.SetExceptionErrorCode(interpreter, e);

                                            result = String.Format(
                                                "couldn't compile regular expression pattern: {0}",
                                                e.Message);

                                            code = ReturnCode.Error;
                                        }

                                        //
                                        // NOTE: If the result is still Ok, then we know that the regular
                                        //       expression pattern was compiled and the regEx object was
                                        //       created successfully.
                                        //
                                        if (code == ReturnCode.Ok)
                                        {
                                            int matchCount = 0; // no matches yet.

                                            //
                                            // BUGFIX: For the "-all" case, replacement must still honor the
                                            //         start index (so any prefix before it is preserved and
                                            //         not searched).  Use a per-call replacement limit that
                                            //         is an upper bound on the number of (possibly empty)
                                            //         matches in the input -- its length plus one -- which
                                            //         is effectively (overkill for?) "all", i.e. as it mean
                                            //         that one-more-than-every-single-character would match
                                            //         (impossible), while still passing the start index to
                                            //         the underlying Replace overload.
                                            //
                                            int allCount = input.Length + 1;

                                            //
                                            // NOTE: Place the script to evaluate in the callback into a
                                            //       ClientData object for use by the callback itself.
                                            //
                                            RegsubClientData regsubClientData = new RegsubClientData(null,
                                                regEx, pattern, input, replacement, replacementLocation,
                                                text, textLocation, 0, quote, extra, strict, verbatim,
                                                literal);

                                            //
                                            // NOTE: Push our interpreter and the necessary RegsubClientData
                                            //       instance onto the stack to guarantee that they can be
                                            //       fetched from inside the callback.  Technically, it
                                            //       should not be necessary to push the interpreter itself
                                            //       because the engine should push it for us; however,
                                            //       better safe than sorry (i.e. in case we are called
                                            //       outside the scope of a script being evaluated, etc).
                                            //       Also, the RegsubClientData instance is now passed using
                                            //       this method, so we always need to push something anyhow.
                                            //
                                            GlobalState.PushActiveInterpreter(interpreter, regsubClientData);

                                            try
                                            {
                                                if (text != null)
                                                {
                                                    //
                                                    // NOTE: Perform the replacements using our custom match
                                                    //       evaluator which will then evaluate the provided
                                                    //       script to obtain the final results.
                                                    //
                                                    if (all)
                                                    {
                                                        result = regEx.Replace(
                                                            input, RegExOps.RegsubEvaluateMatchCallback,
                                                            allCount, startIndex);
                                                    }
                                                    else
                                                    {
                                                        result = regEx.Replace(
                                                            input, RegExOps.RegsubEvaluateMatchCallback,
                                                            count, startIndex);
                                                    }
                                                }
                                                else if (command)
                                                {
                                                    //
                                                    // NOTE: Perform the replacements using the command match
                                                    //       evaluator which will then evaluate the provided
                                                    //       replacement script fragment, with the match text
                                                    //       appended to it in order to obtain the results.
                                                    //       This is designed to conform with TIP #463.
                                                    //
                                                    if (all)
                                                    {
                                                        result = regEx.Replace(
                                                            input, RegExOps.RegsubCommandMatchCallback,
                                                            allCount, startIndex);
                                                    }
                                                    else
                                                    {
                                                        result = regEx.Replace(
                                                            input, RegExOps.RegsubCommandMatchCallback,
                                                            count, startIndex);
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: Perform the replacements using our custom match
                                                    //       evaluator which will simply count the number of
                                                    //       matches and return them verbatim to obtain the
                                                    //       final results.
                                                    //
                                                    if (all)
                                                    {
                                                        result = regEx.Replace(
                                                            input, RegExOps.RegsubNormalMatchCallback,
                                                            allCount, startIndex);
                                                    }
                                                    else
                                                    {
                                                        result = regEx.Replace(
                                                            input, RegExOps.RegsubNormalMatchCallback,
                                                            count, startIndex);
                                                    }
                                                }

                                                //
                                                // NOTE: Extract the match count from the regsub clientData so that,
                                                //       if necessary, it can be used for the command result (below).
                                                //
                                                matchCount = regsubClientData.Count;
                                            }
                                            catch (ScriptException e)
                                            {
                                                //
                                                // NOTE: Our callback threw an error (it wanted to
                                                //       halt processing of the matches and/or return
                                                //       an error).
                                                //
                                                if (e.ReturnCode == ReturnCode.Break)
                                                {
                                                    //
                                                    // NOTE: This is considered success.
                                                    //
                                                    result = String.Empty;
                                                    code = ReturnCode.Ok;
                                                }
                                                else if (e.ReturnCode != ReturnCode.Ok)
                                                {
                                                    //
                                                    // NOTE: This is considered failure.
                                                    //
                                                    Engine.SetExceptionErrorCode(interpreter, e);

                                                    result = e.Message;
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                //
                                                // NOTE: Other (non-script) exceptions are always
                                                //       considered failures in this context.
                                                //
                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                            finally
                                            {
                                                //
                                                // NOTE: Pop our interpreter from the stack if we
                                                //       pushed it previously.
                                                //
                                                /* IGNORED */
                                                GlobalState.PopActiveInterpreter();
                                            }

                                            //
                                            // NOTE: Did we succeed thus far?
                                            //
                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // NOTE: If they provided a variable name to store the
                                                //       results into we return the match count as the
                                                //       command result.
                                                //
                                                if (varName != null)
                                                {
                                                    code = interpreter.SetVariableValue(
                                                        VariableFlags.None, varName, result, null,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = matchCount;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = "-command cannot be used with -eval option";
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
                                    result = "wrong # args: should be \"regsub ?switches? exp string subSpec ?varName?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"regsub ?switches? exp string subSpec ?varName?\"";
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
