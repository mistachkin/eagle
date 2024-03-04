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
    [ObjectId("2d0df297-03b8-4375-bd55-e3d9abd31a94")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Regsub : Core
    {
        public Regsub(
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
                    if (arguments.Count >= 4)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(typeof(RegexOptions), OptionFlags.MustHaveEnumValue, Index.Invalid,
                                Index.Invalid, "-options", new Variant(StringOps.DefaultRegExSyntaxOptions)),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),
                            new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-count", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-ecma", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-compiled", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-explicit", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-quote", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nostrict", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-reverse", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-eval", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-command", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-literal", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-verbatim", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-extra", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-expanded", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-line", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-lineanchor", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-linestop", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noculture", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-start", null),
                            Option.CreateEndOfOptions()
                        });

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
                                                            input, RegExOps.RegsubEvaluateMatchCallback);
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
                                                            input, RegExOps.RegsubCommandMatchCallback);
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
                                                            input, RegExOps.RegsubNormalMatchCallback);
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
