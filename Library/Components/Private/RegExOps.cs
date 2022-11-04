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
    [ObjectId("b45f7d61-390b-4aae-a80b-cd88a1444bdf")]
    internal static class RegExOps
    {
        #region Private Constants
        //
        // NOTE: This prefix indicates the regular expression is "advanced";
        //       since almost all .NET regular expressions already have these
        //       features, this is simply ignored and removed.
        //
        private const string AdvancedPrefix = "***:";

        //
        // NOTE: This prefix indicates the regular expression is actually a
        //       literal string to be matched.
        //
        private const string LiteralPrefix = "***=";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Regular Expression Support Methods
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

        private static string GetMatchValue(
            Match match
            )
        {
            return GetMatchValue(match, 0);
        }

        ///////////////////////////////////////////////////////////////////////

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

            StringBuilder builder = StringOps.NewStringBuilder();

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                HandleSubSpecChar(
                    regEx, match, pattern, input, replacement,
                    text, quote, extra, strict, builder,
                    character, ref index);
            }

            return builder.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        //
        // TODO: In the future, perhaps consider pulling from a cache here?
        //
        public static Regex Create(string pattern)
        {
            MaybeMutatePattern(ref pattern);
            return new Regex(pattern);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: In the future, perhaps consider pulling from a cache here?
        //
        public static Regex Create(
            string pattern,
            RegexOptions regExOptions
            )
        {
            MaybeMutatePattern(ref pattern);
            return new Regex(pattern, regExOptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Regular Expression Support Methods
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
                error = "invalid arguments";
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
        public static string RegsubNormalMatchCallback(
            Match match
            )
        {
            //
            // NOTE: Attempt to obtain the parameters that were passed in
            ///      from the [regsub] command caller and verify them.
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

        public static string RegsubCommandMatchCallback(
            Match match
            )
        {
            //
            // NOTE: Attempt to obtain the parameters that were passed in
            ///      from the [regsub] command caller and verify them.
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

        public static string RegsubEvaluateMatchCallback(
            Match match
            )
        {
            //
            // NOTE: Attempt to obtain the parameters that were passed in
            ///      from the [regsub] command caller and verify them.
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
