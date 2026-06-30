/*
 * Parse.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// This class implements the Eagle <c>parse</c> command, which exposes the
    /// engine parser as an ensemble of sub-commands (<c>command</c>,
    /// <c>expression</c>, <c>options</c>, and <c>script</c>) that parse text
    /// without evaluating it and return the resulting parse state or token
    /// information.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("a4841c1c-f336-4060-9d1a-0e8544a42bd0")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("string")]
    internal sealed class Parse : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>parse</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Parse(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// The dictionary of sub-commands supported by the <c>parse</c>
        /// command ensemble, namely <c>command</c>, <c>expression</c>,
        /// <c>options</c>, and <c>script</c>.
        /// </summary>
        private readonly EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "command", "expression", "options", "script"
        });

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the dictionary of sub-commands supported by this command
        /// ensemble.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>parse</c> command.  It dispatches to the
        /// requested sub-command (<c>command</c>, <c>expression</c>,
        /// <c>options</c>, or <c>script</c>), parses the supplied text using the
        /// engine parser without evaluating it, and returns the resulting parse
        /// state or token information.
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
        /// command name; element one is the sub-command name; the remaining
        /// elements are the sub-command options and operands.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the parse result for the selected
        /// sub-command (for example, the textual parse state, the round-trip
        /// token list, or an empty string).  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, an unknown sub-command or option is used, the text
        /// cannot be parsed, the interpreter is null, or the argument list is
        /// null, with details placed in <paramref name="result" />.
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
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            null, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            if (code == ReturnCode.Ok)
                            {
                                switch (subCommand)
                                {
                                    case "command":
                                        {
                                            if (arguments.Count >= 3)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.Parse_Command, interpreter);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 1) == arguments.Count))
                                                    {
                                                        IVariant value = null;
                                                        EngineFlags engineFlags = interpreter.EngineFlags;

                                                        if (options.IsPresent("-engineflags", ref value))
                                                            engineFlags = (EngineFlags)value.Value;

                                                        SubstitutionFlags substitutionFlags = interpreter.SubstitutionFlags;

                                                        if (options.IsPresent("-substitutionflags", ref value))
                                                            substitutionFlags = (SubstitutionFlags)value.Value;

                                                        int startIndex = 0;

                                                        if (options.IsPresent("-startindex", ref value))
                                                            startIndex = (int)value.Value;

                                                        int characters = arguments[argumentIndex].Length;

                                                        if (options.IsPresent("-characters", ref value))
                                                            characters = (int)value.Value;

                                                        bool nested = false;

                                                        if (options.IsPresent("-nested", ref value))
                                                            nested = (bool)value.Value;

                                                        bool noReady = false;

                                                        if (options.IsPresent("-noready", ref value))
                                                            noReady = (bool)value.Value;

                                                        IParseState state = new ParseState(
                                                            engineFlags, substitutionFlags);

                                                        code = Parser.ParseCommand(
                                                            interpreter, arguments[argumentIndex],
                                                            startIndex, characters, nested, state,
                                                            noReady, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: Success, return the entire
                                                            //       state as a string.
                                                            //
                                                            result = state.ToString();
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
                                                            result = "wrong # args: should be \"parse command ?options? text\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"parse command ?options? text\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "expression":
                                        {
                                            if (arguments.Count >= 3)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.Parse_Expression, interpreter);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 1) == arguments.Count))
                                                    {
                                                        IVariant value = null;
                                                        EngineFlags engineFlags = interpreter.EngineFlags;

                                                        if (options.IsPresent("-engineflags", ref value))
                                                            engineFlags = (EngineFlags)value.Value;

                                                        SubstitutionFlags substitutionFlags = interpreter.SubstitutionFlags;

                                                        if (options.IsPresent("-substitutionflags", ref value))
                                                            substitutionFlags = (SubstitutionFlags)value.Value;

                                                        int startIndex = 0;

                                                        if (options.IsPresent("-startindex", ref value))
                                                            startIndex = (int)value.Value;

                                                        int characters = arguments[argumentIndex].Length;

                                                        if (options.IsPresent("-characters", ref value))
                                                            characters = (int)value.Value;

                                                        bool noReady = false;

                                                        if (options.IsPresent("-noready", ref value))
                                                            noReady = (bool)value.Value;

                                                        IParseState state = new ParseState(
                                                            engineFlags, substitutionFlags);

                                                        code = ExpressionParser.ParseExpression(
                                                            interpreter, arguments[argumentIndex],
                                                            startIndex, characters, state, noReady,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: Success, return the entire
                                                            //       state as a string.
                                                            //
                                                            result = state.ToString();
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
                                                            result = "wrong # args: should be \"parse expression ?options? text\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"parse expression ?options? text\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "options":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.Parse_Options);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) == arguments.Count))
                                                    {
                                                        IVariant value = null;
                                                        string optionsVarName = Vars.OptionSet.Options;

                                                        if (options.IsPresent("-optionsvar", ref value))
                                                            optionsVarName = value.ToString();

                                                        OptionBehaviorFlags flags = OptionBehaviorFlags.Default;

                                                        if (options.IsPresent("-flags", ref value))
                                                            flags = (OptionBehaviorFlags)value.Value;

                                                        bool indexes = false;

                                                        if (options.IsPresent("-indexes"))
                                                            indexes = true;

                                                        bool allowInteger = false;

                                                        if (options.IsPresent("-allowinteger"))
                                                            allowInteger = true;

                                                        bool strict = false;

                                                        if (options.IsPresent("-strict"))
                                                            strict = true;

                                                        bool verbose = false;

                                                        if (options.IsPresent("-verbose"))
                                                            verbose = true;

                                                        bool noCase = false;

                                                        if (options.IsPresent("-nocase"))
                                                            noCase = true;

                                                        bool noValue = false;

                                                        if (options.IsPresent("-novalue"))
                                                            noValue = true;

                                                        bool noSet = false;

                                                        if (options.IsPresent("-noset"))
                                                            noSet = true;

                                                        bool noReady = false;

                                                        if (options.IsPresent("-noready"))
                                                            noReady = true;

                                                        bool simple = false;

                                                        if (options.IsPresent("-simple"))
                                                            simple = true;

                                                        OptionDictionary newOptions = null;
                                                        AppDomain appDomain = interpreter.GetAppDomain();
                                                        CultureInfo cultureInfo = interpreter.InternalCultureInfo;

                                                        if (simple)
                                                        {
                                                            newOptions = OptionDictionary.FromString(
                                                                interpreter, arguments[argumentIndex],
                                                                appDomain, Value.GetTypeValueFlags(
                                                                allowInteger, strict, verbose, noCase),
                                                                cultureInfo, ref result);
                                                        }
                                                        else
                                                        {
                                                            newOptions = OptionDictionary.FromString(
                                                                interpreter, arguments[argumentIndex],
                                                                appDomain, allowInteger, strict, verbose,
                                                                noCase, cultureInfo, ref result);
                                                        }

                                                        if (newOptions != null)
                                                        {
                                                            StringList list = StringList.FromString(
                                                                arguments[argumentIndex + 1], ref result);

                                                            if (list != null)
                                                            {
                                                                ArgumentList newArguments = new ArgumentList(list);

                                                                int nextIndex = Index.Invalid;
                                                                int endIndex = Index.Invalid;

                                                                code = interpreter.GetOptions(
                                                                    newOptions, newArguments, 0, 0, Index.Invalid, flags,
                                                                    noCase, noValue, noSet, ref nextIndex, ref endIndex,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    VariableFlags variableFlags = VariableFlags.None;

                                                                    if (noReady)
                                                                        variableFlags |= VariableFlags.NoReady;

                                                                    if (indexes)
                                                                    {
                                                                        code = interpreter.SetVariableValue2(
                                                                            variableFlags, optionsVarName,
                                                                            Vars.OptionSet.NextIndex, nextIndex.ToString(),
                                                                            null, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            code = interpreter.SetVariableValue2(
                                                                                variableFlags, optionsVarName,
                                                                                Vars.OptionSet.EndIndex, endIndex.ToString(),
                                                                                null, ref result);
                                                                        }
                                                                    }

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        foreach (KeyValuePair<string, IOption> pair in newOptions)
                                                                        {
                                                                            IOption option = pair.Value;

                                                                            if (option == null)
                                                                                continue;

                                                                            if (option.IsIgnored(newOptions))
                                                                                continue;

                                                                            /* REUSED */
                                                                            value = null;

                                                                            bool present = option.IsPresent(newOptions, ref value);

                                                                            if (present &&
                                                                                !option.CanBePresent(newOptions, ref result))
                                                                            {
                                                                                code = ReturnCode.Error;
                                                                                break;
                                                                            }

                                                                            code = interpreter.SetVariableValue2(
                                                                                variableFlags, optionsVarName, pair.Key,
                                                                                present.ToString(), null, ref result);

                                                                            if (code != ReturnCode.Ok)
                                                                                break;

                                                                            if (option.MustHaveValue(newOptions))
                                                                            {
                                                                                //
                                                                                // NOTE: If the option was not actually present,
                                                                                //       grab and use the default value instead.
                                                                                //
                                                                                if (!present)
                                                                                    value = option.Value;

                                                                                //
                                                                                // NOTE: Only set the value if the option was
                                                                                //       actually present OR there is a bonafide
                                                                                //       default value.
                                                                                //
                                                                                if (present || (value != null))
                                                                                {
                                                                                    string index = pair.Key +
                                                                                        Characters.Comma + Vars.OptionSet.Value;

                                                                                    code = interpreter.SetVariableValue2(
                                                                                        variableFlags, optionsVarName, index,
                                                                                        (value != null) ? value.ToString() : null,
                                                                                        null, ref result);

                                                                                    if (code != ReturnCode.Ok)
                                                                                        break;
                                                                                }
                                                                            }
                                                                        }

                                                                        if (code == ReturnCode.Ok)
                                                                            result = String.Empty;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = ReturnCode.Error;
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
                                                            result = "wrong # args: should be \"parse options ?options? optionList argumentList\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"parse options ?options? options optionList argumentList\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "script":
                                        {
                                            if (arguments.Count >= 3)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.Parse_Script, interpreter);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 1) == arguments.Count))
                                                    {
                                                        IVariant value = null;
                                                        EngineFlags engineFlags = interpreter.EngineFlags;

                                                        if (options.IsPresent("-engineflags", ref value))
                                                            engineFlags = (EngineFlags)value.Value;

                                                        SubstitutionFlags substitutionFlags = interpreter.SubstitutionFlags;

                                                        if (options.IsPresent("-substitutionflags", ref value))
                                                            substitutionFlags = (SubstitutionFlags)value.Value;

                                                        string fileName = null;

                                                        if (options.IsPresent("-filename", ref value))
                                                            fileName = value.ToString();

                                                        int currentLine = Parser.StartLine;

                                                        if (options.IsPresent("-currentline", ref value))
                                                            currentLine = (int)value.Value;

                                                        int startIndex = 0;

                                                        if (options.IsPresent("-startindex", ref value))
                                                            startIndex = (int)value.Value;

                                                        int characters = arguments[argumentIndex].Length;

                                                        if (options.IsPresent("-characters", ref value))
                                                            characters = (int)value.Value;

                                                        bool nested = false;

                                                        if (options.IsPresent("-nested", ref value))
                                                            nested = (bool)value.Value;

                                                        bool syntax = false;

                                                        if (options.IsPresent("-syntax", ref value))
                                                            syntax = (bool)value.Value;

                                                        bool strict = false;

                                                        if (options.IsPresent("-strict", ref value))
                                                            strict = (bool)value.Value;

                                                        bool roundTrip = false;

                                                        if (options.IsPresent("-roundtrip", ref value))
                                                            roundTrip = (bool)value.Value;

                                                        bool noReady = false;

                                                        if (options.IsPresent("-noready", ref value))
                                                            noReady = (bool)value.Value;

                                                        IParseState state = new ParseState(
                                                            engineFlags, substitutionFlags);

                                                        TokenList tokens = null;

                                                        code = Parser.ParseScript(
                                                            interpreter, fileName, currentLine,
                                                            arguments[argumentIndex], startIndex,
                                                            characters, engineFlags, substitutionFlags,
                                                            nested, noReady, syntax, strict, ref state,
                                                            ref tokens, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (roundTrip)
                                                            {
                                                                //
                                                                // NOTE: Return only the tokens that
                                                                //       are absolutely necessary to
                                                                //       rebuild the script text.
                                                                //
                                                                TokenList newTokens = new TokenList();

                                                                for (int index = 0; index < tokens.Count; index++)
                                                                {
                                                                    IToken token = tokens[index];

                                                                    if ((token.Type == TokenType.Variable) ||
                                                                        (token.Type == TokenType.VariableNameOnly))
                                                                    {
                                                                        index += token.Components;
                                                                    }
                                                                    else if ((token.Type != TokenType.Separator) &&
                                                                        (token.Components != 0))
                                                                    {
                                                                        continue;
                                                                    }

                                                                    newTokens.Add(token);
                                                                }

                                                                result = newTokens.ToString();
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: Replace final token list
                                                                //       with the one we have been
                                                                //       building.
                                                                //
                                                                state.Tokens = tokens;

                                                                //
                                                                // NOTE: Success, return the entire
                                                                //       state as a string.
                                                                //
                                                                result = state.ToString();
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
                                                            result = "wrong # args: should be \"parse script ?options? text\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"parse script ?options? text\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            result = ScriptOps.BadSubCommand(
                                                interpreter, null, null, subCommand, this, null, null);

                                            code = ReturnCode.Error;
                                            break;
                                        }
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"parse type ?arg ...?\"";
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
