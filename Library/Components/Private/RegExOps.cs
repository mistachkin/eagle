/*
 * RegExOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods that support the regular
    /// expression handling used by Eagle, including pattern mutation for the
    /// "advanced" and "literal" prefixes, regular expression creation, match
    /// value and group extraction, and the substitution-specification
    /// translation and match-evaluator callback machinery used by the
    /// <c>regsub</c> command.
    /// </summary>
    [ObjectId("b45f7d61-390b-4aae-a80b-cd88a1444bdf")]
    internal static class RegExOps
    {
        #region Private Constants
        //
        // NOTE: This prefix indicates the regular expression is "advanced";
        //       since almost all .NET regular expressions already have these
        //       features, this is simply ignored and removed.
        //
        /// <summary>
        /// This prefix indicates the regular expression is "advanced"; since almost
        /// all .NET regular expressions already have these features, it is simply
        /// ignored and removed.
        /// </summary>
        private const string AdvancedPrefix = "***:";

        //
        // NOTE: This prefix indicates the regular expression is actually a
        //       literal string to be matched.
        //
        /// <summary>
        /// This prefix indicates the regular expression is actually a literal string
        /// to be matched.
        /// </summary>
        private const string LiteralPrefix = "***=";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the <see cref="RegexOptions.Compiled" /> option is added
        /// when creating a regular expression via the single-argument <c>Create</c>
        /// method.
        /// </summary>
        private static bool ForceCompiled1 = true;
        /// <summary>
        /// When non-zero, the <see cref="RegexOptions.Compiled" /> option is added
        /// when creating a regular expression via the <c>Create</c> method that
        /// accepts options.
        /// </summary>
        private static bool ForceCompiled2 = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Regular Expression Support Methods
        /// <summary>
        /// This method examines the specified regular expression pattern and, when it
        /// begins with the "advanced" or "literal" prefix, removes that prefix
        /// (escaping the remainder for the literal prefix) so the pattern is suitable
        /// for use with the .NET regular expression engine.
        /// </summary>
        /// <param name="pattern">
        /// Upon input, the regular expression pattern to examine, which may be null.
        /// Upon output, the pattern with any recognized prefix removed and, for the
        /// literal prefix, with its remaining characters escaped.
        /// </param>
        private static void MaybeMutatePattern(
            ref string pattern /* in, out */
            )
        {
            if (pattern != null)
            {
                if ((AdvancedPrefix != null) && SharedStringOps.StartsWith(
                        pattern, AdvancedPrefix, StringComparison.Ordinal))
                {
                    pattern = pattern.Substring(AdvancedPrefix.Length);
                    return;
                }

                if ((LiteralPrefix != null) && SharedStringOps.StartsWith(
                        pattern, LiteralPrefix, StringComparison.Ordinal))
                {
                    pattern = pattern.Substring(LiteralPrefix.Length);
                    pattern = Regex.Escape(pattern);
                    return;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves and validates the interpreter and regsub client data
        /// associated with the active <c>regsub</c> command invocation, for use by the
        /// match-evaluator callback methods.  It throws a
        /// <see cref="ScriptException" /> when the required data is missing or invalid,
        /// or when the interpreter is not ready.
        /// </summary>
        /// <param name="interpreter">
        /// Upon success, receives the interpreter associated with the active
        /// <c>regsub</c> command.
        /// </param>
        /// <param name="regsubClientData">
        /// Upon success, receives the client data associated with the active
        /// <c>regsub</c> command.
        /// </param>
        private static void RegsubMatchCallbackPrologue(
            out Interpreter interpreter,
            out RegsubClientData regsubClientData
            )
        {
            IAnyPair<Interpreter, IClientData> anyPair =
                Interpreter.GetActivePair(typeof(RegsubClientData));

            if (anyPair == null)
            {
                throw new ScriptException(
                    ReturnCode.Error, "missing regsub data pair");
            }

            interpreter = anyPair.X;

            if (interpreter == null)
            {
                throw new ScriptException(
                    ReturnCode.Error, "invalid interpreter");
            }

            regsubClientData = anyPair.Y as RegsubClientData;

            if (regsubClientData == null)
            {
                throw new ScriptException(
                    ReturnCode.Error, "invalid clientData");
            }

            ReturnCode code;
            Result error = null;

            code = Interpreter.Ready(interpreter, ref error);

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a decimal match group index from the
        /// specified text, starting at the given index.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index within <paramref name="text" /> at which to begin parsing.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to consider while parsing.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index immediately following the last parsed
        /// digit.
        /// </param>
        /// <param name="groupIndex">
        /// Upon success, receives the parsed match group index.
        /// </param>
        /// <returns>
        /// True if a match group index was successfully parsed; otherwise, false.
        /// </returns>
        private static bool ParseGroupIndex(
            string text,
            int startIndex,
            int characters,
            ref int stopIndex,
            ref int groupIndex
            )
        {
            int length;
            long number = 0;

            length = Parser.ParseDecimal(
                text, startIndex, characters, ref number);

            if (length > 0)
            {
                //
                // NOTE: Set the stopping index based on the number of
                //       digits parsed above.  The calling method will
                //       need to subtract one from this value prior to
                //       returning to its caller because the index will
                //       be incremented again after that point.
                //
                stopIndex = startIndex + length;

                //
                // NOTE: Always convert the number to a 32-bit integer,
                //       which may by lossy; however, we do not care
                //       because group indexes cannot exceed 32-bits.
                //
                groupIndex = ConversionOps.ToInt(number);

                //
                // NOTE: An integer value was parsed; therefore, we
                //       return success.
                //
                return true;
            }

            //
            // NOTE: For some reason, we were not able to parse any
            //       integer value.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a match group name, delimited by the
        /// less-than and greater-than signs, from the specified text starting at the
        /// given index.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index within <paramref name="text" /> at which to begin parsing, which
        /// must refer to the opening less-than sign.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to consider while parsing.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index immediately following the closing
        /// greater-than sign.
        /// </param>
        /// <param name="groupName">
        /// Upon success, receives the parsed match group name.
        /// </param>
        /// <returns>
        /// True if a match group name was successfully parsed; otherwise, false.
        /// </returns>
        private static bool ParseGroupName(
            string text,
            int startIndex,
            int characters,
            ref int stopIndex,
            ref string groupName
            )
        {
            if (String.IsNullOrEmpty(text))
                return false;

            int length = text.Length;
            int index = startIndex;

            if ((index < 0) || (index >= length))
                return false;

            if (text[index] != Characters.LessThanSign)
                return false;

            index++;

            while ((index < length) &&
                (text[index] != Characters.GreaterThanSign))
            {
                index++;
            }

            if (index >= length)
                return false;

            stopIndex = index + 1;

            groupName = text.Substring(
                startIndex + 1, index - (startIndex + 1));

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the entire match (group zero) for the
        /// specified regular expression match.
        /// </summary>
        /// <param name="match">
        /// The regular expression match, which may be null.
        /// </param>
        /// <returns>
        /// The value of the entire match, or null if it is not available.
        /// </returns>
        private static string GetMatchValue(
            Match match
            )
        {
            return GetMatchValue(match, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the named match group for the specified
        /// regular expression match.
        /// </summary>
        /// <param name="regEx">
        /// The regular expression used to resolve the group name to a group number,
        /// which may be null.
        /// </param>
        /// <param name="match">
        /// The regular expression match, which may be null.
        /// </param>
        /// <param name="groupName">
        /// The name of the match group whose value is returned, which may be null.
        /// </param>
        /// <returns>
        /// The value of the named match group, or null if it is not available.
        /// </returns>
        private static string GetMatchValue(
            Regex regEx,
            Match match,
            string groupName
            )
        {
            if ((regEx == null) || (groupName == null))
                return null;

            return GetMatchValue(match, regEx.GroupNumberFromName(groupName));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list containing the value of every match group for the
        /// specified regular expression match.
        /// </summary>
        /// <param name="match">
        /// The regular expression match, which may be null.
        /// </param>
        /// <returns>
        /// A list of match group values, or null if the match or its group collection
        /// is not available.
        /// </returns>
        private static StringList GetMatchList(
            Match match
            )
        {
            if (match == null)
                return null;

            GroupCollection groups = match.Groups;

            if (groups == null)
                return null;

            StringList list = new StringList();

            foreach (Group group in groups)
            {
                if (group != null)
                    list.Add(group.Value);
                else
                    list.Add((string)null);
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles a backslash escape sequence or meta-character within a
        /// <c>regsub</c> substitution specification that is not one of the specially
        /// recognized sequences, appending the appropriate text to the output builder.
        /// A backslash followed by a digit selects the corresponding match group.
        /// </summary>
        /// <param name="match">
        /// The current regular expression match, if any, which may be null.
        /// </param>
        /// <param name="builder">
        /// The builder receiving the partially translated substitution specification.
        /// </param>
        /// <param name="quote">
        /// Non-zero to apply list element quoting to appended match values.
        /// </param>
        /// <param name="strict">
        /// Non-zero to conform strictly to the Tcl documentation when handling
        /// unrecognized escape sequences.
        /// </param>
        /// <param name="character">
        /// The current character being processed.
        /// </param>
        /// <param name="nextCharacter">
        /// The character immediately following <paramref name="character" />.
        /// </param>
        private static void HandleSubSpecOtherEscapeOrMetaChar(
            Match match,           // current Regex match, if any.
            StringBuilder builder, // [regsub] subSpec, partially translated.
            bool quote,            // use list element quoting?
            bool strict,           // strict conformance to the Tcl docs.
            char character,        // current character within "text".
            char nextCharacter     // next character within "text".
            )
        {
            //
            // NOTE: Is this a backslash followed by a digit?  If so, we
            //       need to append the applicable match group value, if
            //       any.
            //
            if (StringOps.CharIsAsciiDigit(nextCharacter))
            {
                if (match != null)
                {
                    //
                    // NOTE: What is the match group being used?
                    //
                    int groupIndex = nextCharacter - Characters.Zero;

                    //
                    // NOTE: Is the specified match group within the
                    //       available ones?
                    //
                    GroupCollection groups = match.Groups;

                    if ((groups != null) &&
                        (groupIndex >= 0) && (groupIndex < groups.Count))
                    {
                        //
                        // NOTE: Grab the specified match group and then
                        //       make sure its valid.
                        //
                        Group group = groups[groupIndex];

                        if (group != null)
                        {
                            //
                            // NOTE: Append the value of the match group,
                            //       quoting it if requested.
                            //
                            string matchValue = group.Value;

                            builder.Append(quote ?
                                Parser.Quote(matchValue) : matchValue);
                        }
                    }
                }
                else
                {
                    //
                    // NOTE: We hit a properly escaped subSpec, insert
                    //       its .NET Framework equivalent, which will
                    //       include a dollar sign prefix.  An example
                    //       is "\1" to "$1".
                    //
                    builder.Append(
                        Characters.DollarSign.ToString() +
                        nextCharacter.ToString());
                }
            }
            else
            {
                if (strict)
                {
                    //
                    // BUGFIX: No, we do not actually want to do that.
                    //         Even though this portion of the subSpec
                    //         pattern argument handling for [regsub] is
                    //         poorly specified in the Tcl documentation,
                    //         what we actually need to do here is insert
                    //         a literal backslash followed by the literal
                    //         character we just encountered.
                    //
                    //         The exact rule is as follows:
                    //
                    //         "Any backslash in the subSpec pattern
                    //          argument NOT followed by an ampersand,
                    //          a single decimal digit, or another
                    //          backslash is treated as a literal
                    //          backslash."
                    //
                    //         As a consequence of the above rule, any
                    //         backslash followed by any character NOT
                    //         covered by the above rule will be inserted
                    //         into the output string literally.
                    //
                    builder.Append(character);
                    builder.Append(nextCharacter);
                }
                else
                {
                    //
                    // NOTE: We hit an "escaped" character that we do not
                    //       recognize, just insert it unescaped.
                    //
                    builder.Append(nextCharacter);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the character immediately following a backslash within
        /// a <c>regsub</c> substitution specification, appending the appropriate
        /// replacement text to the output builder and advancing the processing index
        /// as needed.
        /// </summary>
        /// <param name="regEx">
        /// The original regular expression, which may be null.
        /// </param>
        /// <param name="match">
        /// The current regular expression match, if any, which may be null.
        /// </param>
        /// <param name="builder">
        /// The builder receiving the partially translated substitution specification.
        /// </param>
        /// <param name="pattern">
        /// The original pattern string, which may be null.
        /// </param>
        /// <param name="input">
        /// The original input string, which may be null.
        /// </param>
        /// <param name="replacement">
        /// The original replacement string, which may be null.
        /// </param>
        /// <param name="quote">
        /// Non-zero to apply list element quoting to appended match values.
        /// </param>
        /// <param name="extra">
        /// Non-zero to permit the non-standard <c>\P</c>, <c>\I</c>, <c>\S</c>,
        /// <c>\M#</c>, and <c>\N&lt;n&gt;</c> substitutions.
        /// </param>
        /// <param name="strict">
        /// Non-zero to conform strictly to the Tcl documentation when handling
        /// unrecognized escape sequences.
        /// </param>
        /// <param name="character">
        /// The current character being processed (the backslash).
        /// </param>
        /// <param name="nextCharacter">
        /// The character immediately following <paramref name="character" />.
        /// </param>
        /// <param name="index">
        /// Upon input, the index of the current character within the text being
        /// processed.  Upon output, the index may be advanced past any additional
        /// characters consumed by an extended substitution.
        /// </param>
        private static void HandleSubSpecEscapeOrMetaChar(
            Regex regEx,           // original regular expression.
            Match match,           // current Regex match, if any.
            StringBuilder builder, // [regsub] subSpec, partially translated.
            string pattern,        // original pattern string.
            string input,          // original input string.
            string replacement,    // original replacement string.
            bool quote,            // use list element quoting?
            bool extra,            // \P|I|S|M#|N<n> permitted in subSpec.
            bool strict,           // strict conformance to the Tcl docs.
            char character,        // current character within "text".
            char nextCharacter,    // next character within "text".
            ref int index          // index within "text".
            )
        {
            switch (nextCharacter)
            {
                case Characters.Ampersand:
                    {
                        //
                        // NOTE: We hit an escaped ampersand, insert the
                        //       literal ampersand.
                        //
                        builder.Append(nextCharacter);
                        break;
                    }
                case Characters.Backslash:
                    {
                        //
                        // NOTE: We hit an escaped backslash, insert the
                        //       literal backslash.
                        //
                        builder.Append(nextCharacter);
                        break;
                    }
                case Characters.P:
                    {
                        //
                        // NOTE: This feature is not supported unless the
                        //       "extra" (i.e. non-standard) substitutions
                        //       are allowed.
                        //
                        if (!extra) goto default;

                        //
                        // NOTE: Append the original "exp" (pattern string)
                        //       argument.
                        //
                        builder.Append(pattern);
                        break;
                    }
                case Characters.I:
                    {
                        //
                        // NOTE: This feature is not supported unless the
                        //       "extra" (i.e. non-standard) substitutions
                        //       are allowed.
                        //
                        if (!extra) goto default;

                        //
                        // NOTE: Append the original "string" (input string)
                        //       argument.
                        //
                        builder.Append(input);
                        break;
                    }
                case Characters.S:
                    {
                        //
                        // NOTE: This feature is not supported unless the
                        //       "extra" (i.e. non-standard) substitutions
                        //       are allowed.
                        //
                        if (!extra) goto default;

                        //
                        // NOTE: Append the original "subSpec" (replacement
                        //       string) argument.
                        //
                        builder.Append(replacement);
                        break;
                    }
                case Characters.M:
                    {
                        //
                        // NOTE: This feature is not supported unless the
                        //       "extra" (i.e. non-standard) substitutions
                        //       are allowed.
                        //
                        if (!extra) goto default;

                        //
                        // NOTE: Keep advancing the index until all digits
                        //       have been consumed.
                        //
                        int startIndex = index + 2;
                        int stopIndex = Index.Invalid;
                        int groupIndex = Index.Invalid;

                        if ((replacement != null) && ParseGroupIndex(
                                replacement, startIndex,
                                replacement.Length - startIndex,
                                ref stopIndex, ref groupIndex))
                        {
                            string matchValue = GetMatchValue(
                                match, groupIndex);

                            if (matchValue != null)
                                builder.Append(matchValue);

                            index = stopIndex - 2;
                        }

                        break;
                    }
                case Characters.N:
                    {
                        //
                        // NOTE: This feature is not supported unless the
                        //       "extra" (i.e. non-standard) substitutions
                        //       are allowed.
                        //
                        if (!extra) goto default;

                        //
                        // NOTE: Keep advancing the index until the group
                        //       name has been consumed.
                        //
                        int startIndex = index + 2;
                        int stopIndex = Index.Invalid;
                        string groupName = null;

                        if ((replacement != null) && ParseGroupName(
                                replacement, startIndex,
                                replacement.Length - startIndex,
                                ref stopIndex, ref groupName))
                        {
                            string matchValue = GetMatchValue(
                                regEx, match, groupName);

                            if (matchValue != null)
                                builder.Append(matchValue);

                            index = stopIndex - 2;
                        }

                        break;
                    }
                default:
                    {
                        //
                        // NOTE: Handle some other kind of escape sequence
                        //       or meta-character.
                        //
                        HandleSubSpecOtherEscapeOrMetaChar(
                            match, builder, quote, strict, character,
                            nextCharacter);

                        break;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles a single character within a <c>regsub</c> substitution
        /// specification, translating ampersand and backslash escape sequences into
        /// their replacement text and appending the result to the output builder,
        /// while passing any other character through verbatim.
        /// </summary>
        /// <param name="regEx">
        /// The original regular expression, which may be null.
        /// </param>
        /// <param name="match">
        /// The current regular expression match, if any, which may be null.
        /// </param>
        /// <param name="pattern">
        /// The original pattern string, which may be null.
        /// </param>
        /// <param name="input">
        /// The original input string, which may be null.
        /// </param>
        /// <param name="replacement">
        /// The original replacement string, which may be null.
        /// </param>
        /// <param name="text">
        /// The string containing the substitution specifications to process.
        /// </param>
        /// <param name="quote">
        /// Non-zero to apply list element quoting to appended match values.
        /// </param>
        /// <param name="extra">
        /// Non-zero to permit the non-standard <c>\P</c>, <c>\I</c>, <c>\S</c>,
        /// <c>\M#</c>, and <c>\N&lt;n&gt;</c> substitutions.
        /// </param>
        /// <param name="strict">
        /// Non-zero to conform strictly to the Tcl documentation when handling
        /// unrecognized escape sequences.
        /// </param>
        /// <param name="builder">
        /// The builder receiving the partially translated substitution specification.
        /// </param>
        /// <param name="character">
        /// The current character being processed.
        /// </param>
        /// <param name="index">
        /// Upon input, the index of the current character within
        /// <paramref name="text" />.  Upon output, the index may be advanced past any
        /// additional characters consumed while handling an escape sequence.
        /// </param>
        private static void HandleSubSpecChar(
            Regex regEx,           // original regular expression.
            Match match,           // current Regex match, if any.
            string pattern,        // original pattern string.
            string input,          // original input string.
            string replacement,    // original replacement string.
            string text,           // string with the subSpecs to process.
            bool quote,            // use list element quoting?
            bool extra,            // \P|I|S|M#|N<n> permitted in subSpec.
            bool strict,           // strict conformance to the Tcl docs.
            StringBuilder builder, // [regsub] subSpec, partially translated.
            char character,        // current character within "text".
            ref int index          // index within "text".
            )
        {
            //
            // NOTE: We handle the following Tcl compatible regsub subSpecs:
            //
            //       "\&"          : Always replaced with a literal
            //                       ampersand.
            //
            //       "\\"          : Always replaced with a literal
            //                       backslash.
            //
            //       "&"           : Translated to "$&" OR replaced with
            //                       the portion of string that matched
            //                       exp if a valid match was supplied.
            //
            //       "\n" (0 to 9) : Translated to "$n", OR replaced with
            //                       the portion of string that matched the
            //                       Nth parenthesized subexpression of exp,
            //                       except for "\0" (which will be treated
            //                       just like "&") if a valid match was
            //                       supplied.
            //
            //       We also handle the following custom extensions
            //       typically used with "regsub -eval" (these are ONLY
            //       recognized if the "extra" parameter is true; otherwise,
            //       they are ignored):
            //
            //       "\P"          : Always replaced with the original
            //                       pattern.  ONLY recognized if the
            //                       "extra" parameter is true.
            //
            //       "\I"          : Always replaced with the original
            //                       input string.  ONLY recognized if
            //                       the "extra" parameter is true.
            //
            //       "\S"          : Always replaced with the original
            //                       replacement (subSpec).  ONLY
            //                       recognized if the "extra" parameter
            //                       is true.
            //
            //       "\M#"         : Always replaced with the text of
            //                       the Nth parenthesized subexpression.
            //
            //       "\N<n>"       : Always replaced with the text of
            //                       the specified named parenthesized
            //                       subexpression.
            //
            //       Anything else will be passed through verbatim.
            //
            switch (character)
            {
                case Characters.Ampersand:
                    {
                        //
                        // NOTE: If a match was supplied, replace this with
                        //       the entire matched value.
                        //
                        if (match != null)
                        {
                            //
                            // NOTE: Append the value of the entire match,
                            //       quoting it if requested.
                            //
                            string matchValue = match.Value;

                            builder.Append(quote ?
                                Parser.Quote(matchValue) : matchValue);
                        }
                        else
                        {
                            //
                            // NOTE: Translate an unescaped ampersand to the
                            //       .NET Framework equivalent, which includes
                            //       the dollar sign prefix and represents the
                            //       entired matched expression.  An example
                            //       is "&" to "$&".
                            //
                            builder.Append(
                                Characters.DollarSign.ToString() +
                                character.ToString());
                        }

                        break;
                    }
                case Characters.Backslash:
                    {
                        //
                        // NOTE: Are there more characters remaining after
                        //       this one?
                        //
                        if ((index + 1) < text.Length)
                        {
                            //
                            // NOTE: Something is escaped with a backslash,
                            //       we need to look at the next character.
                            //
                            char nextCharacter = text[index + 1];

                            //
                            // NOTE: Handle the escaped character.
                            //
                            HandleSubSpecEscapeOrMetaChar(
                                regEx, match, builder, pattern, input,
                                replacement, quote, extra, strict,
                                character, nextCharacter, ref index);

                            //
                            // NOTE: Now, skip beyond the escaped character
                            //       that was just handled.
                            //
                            index++;
                        }
                        else
                        {
                            //
                            // NOTE: Hit an isolated backslash at the end
                            //       of the string, just insert a literal
                            //       backslash.
                            //
                            builder.Append(character);
                        }

                        break;
                    }
                default:
                    {
                        //
                        // NOTE: Anything not handled gets added verbatim.
                        //
                        builder.Append(character);
                        break;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates an entire <c>regsub</c> substitution specification,
        /// processing each character in turn and producing the final replacement text.
        /// </summary>
        /// <param name="regEx">
        /// The original regular expression, which may be null.
        /// </param>
        /// <param name="match">
        /// The current regular expression match, if any, which may be null.
        /// </param>
        /// <param name="pattern">
        /// The original pattern string, which may be null.
        /// </param>
        /// <param name="input">
        /// The original input string, which may be null.
        /// </param>
        /// <param name="replacement">
        /// The original replacement string, which may be null.
        /// </param>
        /// <param name="text">
        /// The string containing the substitution specifications to process.
        /// </param>
        /// <param name="quote">
        /// Non-zero to apply list element quoting to appended match values.
        /// </param>
        /// <param name="extra">
        /// Non-zero to permit the non-standard <c>\P</c>, <c>\I</c>, <c>\S</c>,
        /// <c>\M#</c>, and <c>\N&lt;n&gt;</c> substitutions.
        /// </param>
        /// <param name="strict">
        /// Non-zero to conform strictly to the Tcl documentation when handling
        /// unrecognized escape sequences.
        /// </param>
        /// <returns>
        /// The translated substitution specification, or the original text when it is
        /// null or empty.
        /// </returns>
        private static string TranslateSubSpec(
            Regex regEx,        // original regular expression.
            Match match,        // current Regex match, if any.
            string pattern,     // original pattern string.
            string input,       // original input string.
            string replacement, // original replacement string.
            string text,        // string with the subSpecs to process.
            bool quote,         // use list element quoting?
            bool extra,         // \P|I|S|M#|N<n> permitted in subSpec.
            bool strict         // strict conformance to the Tcl docs.
            )
        {
            //
            // NOTE: Garbage in, garbage out.
            //
            if (String.IsNullOrEmpty(text))
                return text;

            StringBuilder builder = StringBuilderFactory.Create();

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                HandleSubSpecChar(
                    regEx, match, pattern, input, replacement,
                    text, quote, extra, strict, builder,
                    character, ref index);
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        //
        // TODO: In the future, perhaps consider pulling from a cache here?
        //
        /// <summary>
        /// This method creates a regular expression from the specified pattern, first
        /// applying any recognized "advanced" or "literal" prefix handling and
        /// optionally forcing the compiled option.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern, which may begin with a recognized prefix.
        /// </param>
        /// <returns>
        /// The newly created regular expression.
        /// </returns>
        public static Regex Create(string pattern)
        {
            MaybeMutatePattern(ref pattern);

            RegexOptions regExOptions = RegexOptions.None;

            if (ForceCompiled1)
                regExOptions |= RegexOptions.Compiled;

            return new Regex(pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: In the future, perhaps consider pulling from a cache here?
        //
        /// <summary>
        /// This method creates a regular expression from the specified pattern and
        /// options, first applying any recognized "advanced" or "literal" prefix
        /// handling and optionally forcing the compiled option.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern, which may begin with a recognized prefix.
        /// </param>
        /// <param name="regExOptions">
        /// The options used to create the regular expression.
        /// </param>
        /// <returns>
        /// The newly created regular expression.
        /// </returns>
        public static Regex Create(
            string pattern,
            RegexOptions regExOptions
            )
        {
            MaybeMutatePattern(ref pattern);

            if (ForceCompiled2)
                regExOptions |= RegexOptions.Compiled;

            return new Regex(pattern, regExOptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Regular Expression Support Methods
        /// <summary>
        /// This method gets the match group with the specified index from the
        /// specified regular expression match.
        /// </summary>
        /// <param name="match">
        /// The regular expression match, which may be null.
        /// </param>
        /// <param name="groupIndex">
        /// The index of the match group to return.
        /// </param>
        /// <returns>
        /// The match group with the specified index, or null if it is not available.
        /// </returns>
        public static Group GetMatchGroup(
            Match match,
            int groupIndex
            )
        {
            if ((match == null) || (groupIndex < 0))
                return null;

            GroupCollection groups = match.Groups;

            if ((groups == null) || (groupIndex >= groups.Count))
                return null;

            return groups[groupIndex];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the match group with the specified index
        /// participated in the specified regular expression match.
        /// </summary>
        /// <param name="match">
        /// The regular expression match, which may be null.
        /// </param>
        /// <param name="groupIndex">
        /// The index of the match group to query.
        /// </param>
        /// <returns>
        /// True if the specified match group participated in the match; otherwise,
        /// false.
        /// </returns>
        public static bool GetMatchSuccess(
            Match match,
            int groupIndex
            )
        {
            int startIndex;
            int length;
            string value;

            return GetMatchSuccess(
                match, groupIndex, out startIndex, out length, out value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the match group with the specified index
        /// participated in the specified regular expression match, also returning its
        /// starting index, length, and value.
        /// </summary>
        /// <param name="match">
        /// The regular expression match, which may be null.
        /// </param>
        /// <param name="groupIndex">
        /// The index of the match group to query.
        /// </param>
        /// <param name="startIndex">
        /// Upon success, receives the starting index of the match group; upon failure,
        /// receives an invalid index.
        /// </param>
        /// <param name="length">
        /// Upon success, receives the length of the match group; upon failure, receives
        /// an invalid length.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the match group; upon failure, receives
        /// null.
        /// </param>
        /// <returns>
        /// True if the specified match group participated in the match; otherwise,
        /// false.
        /// </returns>
        public static bool GetMatchSuccess(
            Match match,
            int groupIndex,
            out int startIndex,
            out int length,
            out string value
            )
        {
            Group group = GetMatchGroup(match, groupIndex);

            if (group == null)
            {
                startIndex = Index.Invalid;
                length = Length.Invalid;
                value = null;

                return false;
            }

            startIndex = group.Index;
            length = group.Length;
            value = group.Value;

            return group.Success;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the match group with the specified index from
        /// the specified regular expression match.
        /// </summary>
        /// <param name="match">
        /// The regular expression match, which may be null.
        /// </param>
        /// <param name="groupIndex">
        /// The index of the match group whose value is returned.
        /// </param>
        /// <returns>
        /// The value of the specified match group, or null if it is not available.
        /// </returns>
        public static string GetMatchValue(
            Match match,
            int groupIndex
            )
        {
            Group group = GetMatchGroup(match, groupIndex);

            if (group == null)
                return null;

            return group.Value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates a <c>regsub</c> substitution specification using
        /// default options, producing the final replacement text.
        /// </summary>
        /// <param name="regEx">
        /// The original regular expression, which may be null.
        /// </param>
        /// <param name="match">
        /// The current regular expression match, if any, which may be null.
        /// </param>
        /// <param name="text">
        /// The string containing the substitution specifications to process.
        /// </param>
        /// <returns>
        /// The translated substitution specification, or the original text when it is
        /// null or empty.
        /// </returns>
        public static string TranslateSubSpec(
            Regex regEx,        /* in */
            Match match,        /* in */
            string text         /* in */
            )
        {
            return TranslateSubSpec(
                regEx, match, null, null, null, text, false, false, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the "no match" value into each of the variables named by
        /// the remaining arguments, for use when a regular expression match did not
        /// succeed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which the variables are set.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments whose elements, starting at
        /// <paramref name="nextIndex" />, name the variables to set.
        /// </param>
        /// <param name="nextIndex">
        /// Upon input, the index of the first argument naming a variable to set.  Upon
        /// output, the index immediately following the last argument that was
        /// processed.
        /// </param>
        /// <param name="indexes">
        /// Non-zero to store a pair of invalid indexes as the "no match" value;
        /// otherwise, the empty string is stored.
        /// </param>
        /// <param name="noEmpty">
        /// Non-zero to skip setting a variable when the "no match" value is null or
        /// empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
        /// code.
        /// </returns>
        public static ReturnCode NoMatchVariableValues(
            Interpreter interpreter, /* in */
            ArgumentList arguments,  /* in */
            ref int nextIndex,       /* in, out */
            bool indexes,            /* in */
            bool noEmpty,            /* in */
            ref Result error         /* out */
            )
        {
            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            if (nextIndex < 0)
            {
                error = "negative argument index";
                return ReturnCode.Error;
            }

            int count = arguments.Count;

            for (; nextIndex < count; nextIndex++)
            {
                string matchValue;

                if (indexes)
                {
                    matchValue = StringList.MakeList(
                        Index.Invalid, Index.Invalid);
                }
                else
                {
                    matchValue = String.Empty;
                }

                if (!noEmpty || !String.IsNullOrEmpty(matchValue))
                {
                    ReturnCode code;

                    code = interpreter.SetVariableValue(
                        VariableFlags.None, arguments[nextIndex],
                        matchValue, null, ref error);

                    if (code != ReturnCode.Ok)
                        return code;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Text.RegularExpressions.MatchEvaluator Callback Methods
        /// <summary>
        /// This method is the match-evaluator callback used by the <c>regsub</c>
        /// command for normal (non-command, non-evaluated) replacements, producing the
        /// replacement text for the specified match based on the active regsub client
        /// data.
        /// </summary>
        /// <param name="match">
        /// The current regular expression match.
        /// </param>
        /// <returns>
        /// The replacement text for the specified match.
        /// </returns>
        public static string RegsubNormalMatchCallback(
            Match match
            )
        {
            //
            // NOTE: Attempt to obtain the parameters that were passed in
            //       from the [regsub] command caller and verify them.
            //
            Interpreter interpreter;
            RegsubClientData regsubClientData;

            RegsubMatchCallbackPrologue(
                out interpreter, out regsubClientData);

            //
            // NOTE: Keep track of how many matches we have been given.
            //
            regsubClientData.Count++;

            //
            // NOTE: Get some additional parameters we need to perform the
            //       callback from the client data.
            //
            Regex regEx = regsubClientData.RegEx;
            string pattern = regsubClientData.Pattern;
            string input = regsubClientData.Input;
            string replacement = regsubClientData.Replacement;

            bool quote = regsubClientData.Quote;
            bool extra = regsubClientData.Extra;
            bool strict = regsubClientData.Strict;
            bool verbatim = regsubClientData.Verbatim;
            bool literal = regsubClientData.Literal;

            if (literal)
            {
                //
                // NOTE: Use the replacement text literally without any
                //       translations.
                //
                return replacement;
            }
            else
            {
                //
                // NOTE: Perform our custom replacements and return the
                //       result.
                //
                return !verbatim ?
                    TranslateSubSpec(
                        regEx, match, pattern, input, replacement,
                        replacement, quote, extra, strict) :
                    GetMatchValue(match);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the match-evaluator callback used by the <c>regsub</c>
        /// command when a command prefix is supplied, building and evaluating the
        /// command with the match values and returning its result as the replacement
        /// text.
        /// </summary>
        /// <param name="match">
        /// The current regular expression match.
        /// </param>
        /// <returns>
        /// The replacement text for the specified match.
        /// </returns>
        public static string RegsubCommandMatchCallback(
            Match match
            )
        {
            //
            // NOTE: Attempt to obtain the parameters that were passed in
            //       from the [regsub] command caller and verify them.
            //
            Interpreter interpreter;
            RegsubClientData regsubClientData;

            RegsubMatchCallbackPrologue(
                out interpreter, out regsubClientData);

            if (regsubClientData.Literal)
            {
                throw new ScriptException(ReturnCode.Error,
                    "-literal cannot be combined with -command");
            }

            string replacement = regsubClientData.Replacement;
            StringList words = null;
            Result result = null; /* REUSED */

            if (ParserOps<string>.SplitList(
                    interpreter, replacement, 0, Length.Invalid, true,
                    ref words, ref result) != ReturnCode.Ok)
            {
                throw new ScriptException(ReturnCode.Error, result);
            }

            if (words.Count < 1)
            {
                throw new ScriptException(ReturnCode.Error,
                    "command prefix must be a list of at least one element");
            }

            IScriptLocation replacementLocation =
                regsubClientData.ReplacementLocation;

            bool verbatim = regsubClientData.Verbatim;

            StringList matches = GetMatchList(match);

            if (matches == null)
            {
                throw new ScriptException(
                    ReturnCode.Error, "could not build match list");
            }

            ReturnCode code;

            result = null;

            code = interpreter.EvaluateScript(
                ListOps.Concat(words.ToString(), matches.ToString()),
                replacementLocation, ref result);

            if (code != ReturnCode.Ok)
            {
                /* IGNORED */
                Engine.AddErrorInformation(
                    interpreter, result, String.Format(
                        "{0}    (-command substitution computation script)",
                        Environment.NewLine));

                //
                // NOTE: This is our only way out of here.  This exception
                //       will be caught by the command handler for regsub
                //       and converted into a script error.
                //
                throw new ScriptException(code, result);
            }

            return !verbatim ? (string)result : GetMatchValue(match);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the match-evaluator callback used by the <c>regsub</c>
        /// command when a script is to be evaluated, translating and evaluating the
        /// script for the specified match and returning its result as the replacement
        /// text.
        /// </summary>
        /// <param name="match">
        /// The current regular expression match.
        /// </param>
        /// <returns>
        /// The replacement text for the specified match.
        /// </returns>
        public static string RegsubEvaluateMatchCallback(
            Match match
            )
        {
            //
            // NOTE: Attempt to obtain the parameters that were passed in
            //       from the [regsub] command caller and verify them.
            //
            Interpreter interpreter;
            RegsubClientData regsubClientData;

            RegsubMatchCallbackPrologue(
                out interpreter, out regsubClientData);

            if (regsubClientData.Literal)
            {
                throw new ScriptException(ReturnCode.Error,
                    "-literal cannot be combined with -eval");
            }

            //
            // NOTE: Get the script to evaluate from the client data.
            //
            string text = regsubClientData.Text;

            if (String.IsNullOrEmpty(text))
            {
                //
                // NOTE: This is allowed, translate a null or empty script
                //       into a null or empty value.
                //
                return text;
            }

            //
            // NOTE: Grab the script location associated with the script
            //       to be evaluated.
            //
            IScriptLocation textLocation = regsubClientData.TextLocation;

            //
            // NOTE: Keep track of how many matches we have been given.
            //
            regsubClientData.Count++;

            //
            // NOTE: Get some additional parameters we need to perform the
            //       script callback from the client data.
            //
            Regex regEx = regsubClientData.RegEx;
            string pattern = regsubClientData.Pattern;
            string input = regsubClientData.Input;
            string replacement = regsubClientData.Replacement;

            bool quote = regsubClientData.Quote;
            bool extra = regsubClientData.Extra;
            bool strict = regsubClientData.Strict;
            bool verbatim = regsubClientData.Verbatim;

            //
            // NOTE: Special processing to pass data to the script to be
            //       evaluated.
            //
            // WARNING: Cannot cache list representation here, the list is
            //          modified below.
            //
            ReturnCode code;
            StringList list = null;
            Result result = null; /* REUSED */

            code = ParserOps<string>.SplitList(
                interpreter, text, 0, Length.Invalid, false, ref list,
                ref result);

            if (code == ReturnCode.Ok)
            {
                for (int index = 0; index < list.Count; index++)
                {
                    string newReplacement = replacement;
                    string element = list[index];

                    if (String.IsNullOrEmpty(newReplacement))
                        newReplacement = element;

                    list[index] = TranslateSubSpec(
                        regEx, match, pattern, input,
                        newReplacement, element, quote,
                        extra, strict);
                }

                result = null;

                code = interpreter.EvaluateScript(
                    list.ToString(), textLocation, ref result);
            }

            if (code != ReturnCode.Ok)
            {
                /* IGNORED */
                Engine.AddErrorInformation(
                    interpreter, result, String.Format(
                        "{0}    (-regsub command)",
                        Environment.NewLine));

                //
                // NOTE: This is our only way out of here.  This exception
                //       will be caught by the command handler for regsub
                //       and converted into a script error.
                //
                throw new ScriptException(code, result);
            }

            return !verbatim ? (string)result : GetMatchValue(match);
        }
        #endregion
    }
}
