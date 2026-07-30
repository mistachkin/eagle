/*
 * Regexp.cs --
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
    /// This class implements the Eagle <c>regexp</c> command, which matches a
    /// regular expression against an input string, optionally capturing the
    /// overall match and any sub-match groups into variables or returning them
    /// inline.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("3b73b31e-24ef-4161-9213-ebf943c0a628")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Regexp : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>regexp</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Regexp(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>regexp</c> command.  It parses any
        /// leading switches, compiles the regular expression pattern, and
        /// matches it against the input string; depending on the supplied
        /// options it can match all occurrences, report match indexes, return
        /// the matches inline, or store the overall match and sub-match group
        /// values into the named match variables.
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
        /// command name, followed by any switches, then the regular expression
        /// pattern and the input string, and finally any optional match and
        /// sub-match variable names.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the number of matches found (in
        /// <c>-all</c> mode), a boolean-style count indicating whether a match
        /// occurred, or the list of matched values (in <c>-inline</c> mode).
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, an option is invalid, the pattern fails to compile, a
        /// match variable cannot be set, the interpreter is null, or the
        /// argument list is null, with details placed in
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
                    if (arguments.Count >= 3)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Regexp);

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, true, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                            {
                                bool all = false;
                                bool debug = false;
                                bool indexes = false;
                                bool global = false;
                                bool inline = false;
                                bool noEmpty = false;

                                string pattern = arguments[argumentIndex];
                                string input = arguments[argumentIndex + 1];

                                IVariant value = null;

                                RegexOptions regExOptions =
                                    StringOps.DefaultRegExSyntaxOptions;

                                if (options.IsPresent("-options", ref value))
                                    regExOptions = (RegexOptions)value.Value;

                                int skip = Index.Invalid;

                                if (options.IsPresent("-skip", ref value))
                                    skip = (int)value.Value;

                                int limit = Count.Invalid;

                                if (options.IsPresent("-limit", ref value))
                                    limit = (int)value.Value;

                                int length = input.Length;
                                bool haveLength = false;

                                if (options.IsPresent("-length", ref value))
                                {
                                    haveLength = true;
                                    length = (int)value.Value;

                                    if ((length < 0) || (length > input.Length))
                                        length = input.Length;
                                }

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
                                    if (options.IsPresent("-all"))
                                        all = true;

                                    if (options.IsPresent("-debug"))
                                        debug = true;

                                    if (options.IsPresent("-indexes") || options.IsPresent("-indices"))
                                        indexes = true;

                                    if (options.IsPresent("-global"))
                                        global = true;

                                    if (options.IsPresent("-inline"))
                                        inline = true;

                                    if (options.IsPresent("-noempty"))
                                        noEmpty = true;

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

                                    int variableStartIndex = argumentIndex + 2;

                                    if (!inline || (variableStartIndex >= arguments.Count))
                                    {
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
                                            //
                                            // NOTE: Inline matches list, only created and populated when
                                            //       we are operating in inline mode.
                                            //
                                            StringList matches = null;

                                            if (inline)
                                            {
                                                //
                                                // NOTE: Inline mode, create an empty inline matches
                                                //       list.
                                                //
                                                matches = new StringList();
                                            }

                                            /*
                                             * The following loop is to handle multiple matches within the
                                             * same source string;  each iteration handles one match.  If "-all"
                                             * hasn't been specified then the loop body only gets executed once.
                                             * We terminate the loop when the starting offset is past the end of the
                                             * string.
                                             */

                                            int variableNextIndex = variableStartIndex;
                                            int matchIndex = startIndex;
                                            int matchLength = length;
                                            int matchCount = 0;
                                            Match match = null;

                                            while (true)
                                            {
                                                //
                                                // NOTE: Perform the regular expresssion matching.  This cannot
                                                //       raise an exception because we know the input argument
                                                //       is valid.
                                                //
                                                if (matchLength < 0)
                                                    matchLength = 0;

                                                if ((matchIndex + matchLength) > input.Length)
                                                    matchLength = input.Length - matchIndex;

                                                if (debug)
                                                {
                                                    TraceOps.DebugWriteTo(interpreter, String.Format(
                                                        "{0}: Trying index {1}, length {2}", this.Name,
                                                        matchIndex, matchLength), false);
                                                }

                                                if (match != null)
                                                {
                                                    match = match.NextMatch();
                                                }
                                                else if (haveLength)
                                                {
                                                    //
                                                    // NOTE: When the (Eagle-specific) -length option is
                                                    //       used, the input IS explicitly windowed to
                                                    //       [matchIndex, matchIndex + matchLength); use the
                                                    //       3-argument Match overload so that window is
                                                    //       honored.  Within an explicit window the "^"/"$"
                                                    //       anchors bind to the window boundaries (this has
                                                    //       no Tcl equivalent -- -length is an Eagle
                                                    //       extension).
                                                    //
                                                    match = regEx.Match(input, matchIndex, matchLength);
                                                }
                                                else
                                                {
                                                    //
                                                    // BUGFIX: With -start only (no -length window), use the
                                                    //         2-argument Match overload (start index only)
                                                    //         rather than the 3-argument one (start +
                                                    //         length).  The 3-argument overload treats the
                                                    //         substring [matchIndex, matchIndex + matchLength)
                                                    //         as the ENTIRE input, which makes the "^" and "$"
                                                    //         anchors bind to the -start offset instead of the
                                                    //         true string boundaries.  The 2-argument overload
                                                    //         begins the search at matchIndex while keeping the
                                                    //         whole string as context, so -start no longer
                                                    //         re-anchors "^"/"$" (COMPAT: Tcl; F31).
                                                    //
                                                    match = regEx.Match(input, matchIndex);
                                                }

                                                //
                                                // NOTE: Did the overall match succeed?
                                                //
                                                if (match.Success)
                                                {
                                                    if (debug)
                                                    {
                                                        TraceOps.DebugWriteTo(interpreter, String.Format(
                                                            "{0}: Match success {1}", this.Name,
                                                            FormatOps.DisplayRegExMatch(match)), false);
                                                    }

                                                    //
                                                    // NOTE: We found another match.
                                                    //
                                                    matchCount++;

                                                    //
                                                    // NOTE: Check if we should return this match.
                                                    //
                                                    if ((limit < 0) || ((limit >= 0) && (matchCount <= limit)))
                                                    {
                                                        //
                                                        // NOTE: Advance the argument index just beyond the
                                                        //       pattern and input arguments.
                                                        //
                                                        if (!global)
                                                            variableNextIndex = variableStartIndex;

                                                        //
                                                        // NOTE: Process each match group and either set the
                                                        //       corresponding variable value or add it to the
                                                        //       inline result.
                                                        //
                                                        GroupCollection groups = match.Groups;

                                                        if (groups != null)
                                                        {
                                                            int groupCount = groups.Count;
                                                            int groupIndex = 0;

                                                            if (skip >= 0) groupIndex = skip;

                                                            if ((groupIndex >= 0) && (groupIndex < groupCount))
                                                            {
                                                                for (; groupIndex < groupCount; groupIndex++)
                                                                {
                                                                    //
                                                                    // NOTE: Having a null group should not happen; but,
                                                                    //       just in case it does, skip over them.
                                                                    //
                                                                    Group group = groups[groupIndex];

                                                                    if (group == null)
                                                                        continue;

                                                                    //
                                                                    // NOTE: Set the value for this match group based on
                                                                    //       whether we are operating in indexes mode.
                                                                    //
                                                                    string matchValue;

                                                                    if (group.Success)
                                                                    {
                                                                        if (indexes)
                                                                        {
                                                                            //
                                                                            // NOTE: Return the first and last indexes of
                                                                            //       this match group.
                                                                            //
                                                                            matchValue = StringList.MakeList(
                                                                                group.Index, group.Index + group.Length - 1);
                                                                        }
                                                                        else
                                                                        {
                                                                            //
                                                                            // NOTE: Return the value of this match group.
                                                                            //
                                                                            matchValue = group.Value;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (indexes)
                                                                        {
                                                                            //
                                                                            // NOTE: Return invalid indexes for this match
                                                                            //       group (we did not match this group).
                                                                            //
                                                                            matchValue = StringList.MakeList(
                                                                                Index.Invalid, Index.Invalid);
                                                                        }
                                                                        else
                                                                        {
                                                                            //
                                                                            // NOTE: Return an empty value for this match
                                                                            //       group (we did not match this group).
                                                                            //
                                                                            matchValue = String.Empty;
                                                                        }
                                                                    }

                                                                    //
                                                                    // NOTE: Possibly skip over this match if it is empty?
                                                                    //
                                                                    if (!noEmpty || !String.IsNullOrEmpty(matchValue))
                                                                    {
                                                                        //
                                                                        // NOTE: Are we using inline mode?
                                                                        //
                                                                        if (inline)
                                                                        {
                                                                            //
                                                                            // NOTE: Inline mode, add match value to inline
                                                                            //       matches list.
                                                                            //
                                                                            matches.Add(matchValue);
                                                                        }
                                                                        else
                                                                        {
                                                                            //
                                                                            // NOTE: If they supplied a variable name for this match
                                                                            //       group, attempt to set the variable now.
                                                                            //
                                                                            if (variableNextIndex < arguments.Count)
                                                                            {
                                                                                int savedVariableNextIndex = variableNextIndex;
                                                                                string varName = arguments[variableNextIndex];

                                                                                //
                                                                                // NOTE: Potentially re-entrant here due to variable
                                                                                //       traces.
                                                                                //
                                                                                code = interpreter.SetVariableValue(
                                                                                    VariableFlags.None, varName, matchValue, null,
                                                                                    ref result);

                                                                                if (code != ReturnCode.Ok)
                                                                                    break;

                                                                                //
                                                                                // NOTE: Advance to the next match variable name, if any.
                                                                                //
                                                                                variableNextIndex++;

                                                                                if (debug)
                                                                                {
                                                                                    TraceOps.DebugWriteTo(interpreter, String.Format(
                                                                                        "{0}: Set match variable {1} ({2} ==> {3})",
                                                                                        this.Name, FormatOps.WrapOrNull(varName),
                                                                                        savedVariableNextIndex, variableNextIndex),
                                                                                        false);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        //
                                                        // NOTE: If the inner loop failed to set a match variable, break
                                                        //       out of the outer loop as well.
                                                        //
                                                        if (code != ReturnCode.Ok)
                                                            break;

                                                        //
                                                        // NOTE: If we are not in inline mode, fill in any remaining match
                                                        //       variables with an empty string or "-1 -1" if we are in
                                                        //       indexes mode.
                                                        //
                                                        if (!global && !inline)
                                                        {
                                                            int savedVariableNextIndex = variableNextIndex;

                                                            code = RegExOps.NoMatchVariableValues(
                                                                interpreter, arguments, ref variableNextIndex, indexes,
                                                                noEmpty, ref result);

                                                            if (code != ReturnCode.Ok)
                                                                break;

                                                            if (debug)
                                                            {
                                                                int varCount = variableNextIndex - savedVariableNextIndex;

                                                                TraceOps.DebugWriteTo(interpreter, String.Format(
                                                                    "{0}: Emptied {1} match variables ({2} ==> {3})",
                                                                    this.Name, varCount, savedVariableNextIndex,
                                                                    variableNextIndex), false);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (debug)
                                                    {
                                                        TraceOps.DebugWriteTo(interpreter, String.Format(
                                                            "{0}: Match failure", this.Name), false);
                                                    }

                                                    //
                                                    // NOTE: We are done matching.
                                                    //
                                                    break;
                                                }

                                                //
                                                // NOTE: Only keep going if we want all matches.
                                                //
                                                if (!all)
                                                    break;

                                                //
                                                // NOTE: Adjust the match index and length to remove what we have
                                                //       already matched.
                                                //
                                                Group groupZero = RegExOps.GetMatchGroup(match, 0);

                                                if (groupZero == null)
                                                {
                                                    result = String.Format(
                                                        "cannot advance beyond {0} / {1}: group zero missing",
                                                        matchIndex, matchLength);

                                                    code = ReturnCode.Error;
                                                    break;
                                                }

                                                if ((groupZero.Index + groupZero.Length) == matchIndex)
                                                    matchIndex++;
                                                else
                                                    matchIndex = (groupZero.Index + groupZero.Length);

                                                //
                                                // NOTE: Did we run out of input string to match against?
                                                //
                                                if (matchIndex >= length)
                                                    break;
                                            }

                                            //
                                            // NOTE: If we did not encounter an error above (setting a match variable),
                                            //       we will now set the overall command result based on whether or not
                                            //       we are using inline mode.
                                            //
                                            if (code == ReturnCode.Ok)
                                            {
                                                if (inline)
                                                    result = matches;
                                                else
                                                    result = (all ? matchCount : ConversionOps.ToInt(matchCount != 0));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = "regexp match variables not allowed when using -inline";

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
                                    result = "wrong # args: should be \"regexp ?switches? exp string ?matchVar? ?subMatchVar subMatchVar ...?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"regexp ?switches? exp string ?matchVar? ?subMatchVar subMatchVar ...?\"";
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
