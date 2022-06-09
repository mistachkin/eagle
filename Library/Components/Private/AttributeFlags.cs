/*
 * AttributeFlags.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("17b64eff-174f-4856-bafd-df94c759de17")]
    internal static class AttributeFlags
    {
        #region Private Constants
        private static readonly char NameSepatator = Characters.Colon;
        private static readonly int NameLength = 16; // (i.e. hexadecimal 64-bit integer)

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const char AddCharacter = Characters.PlusSign;
        private const char RemoveCharacter = Characters.MinusSign;
        private const char SetCharacter = Characters.EqualSign;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const char AllChars = Characters.Asterisk;
        private const char AllDigitChars = Characters.NumberSign;
        private const char AllAlphabetChars = Characters.ExclamationMark;
        private const char AllUpperAlphabetChars = Characters.DollarSign;
        private const char AllLowerAlphabetChars = Characters.AtSign;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string LegacyFlagFormat =
            "{{{0:X" + NameLength.ToString() + "}{1}}}";

        private static readonly string FlagFormat = "{{{0:X}:{1}}}";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly long NoKey = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly long DefaultKey = NoKey;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static bool HexadecimalNameToKey(
            string name,
            ref long key
            )
        {
            if (name != null)
            {
                int length = name.Length;

                if ((length > 0) &&
                    Parser.ParseHexadecimal(name, 0, length, ref key) == length)
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool CharIsValidName(
            char character
            )
        {
            return Parser.IsHexadecimalDigit(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool CharIsValidValue(
            char character
            )
        {
            return Parser.IsIdentifier(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IEnumerable<char> GetChars(
            char character
            )
        {
            switch (character)
            {
                case AllChars:
                    {
                        return new CharList(
                            Characters.DigitChars,
                            Characters.UpperAlphabetChars,
                            Characters.LowerAlphabetChars);
                    }
                case AllDigitChars:
                    {
                        return new CharList(
                            Characters.DigitChars);
                    }
                case AllAlphabetChars:
                    {
                        return new CharList(
                            Characters.UpperAlphabetChars,
                            Characters.LowerAlphabetChars);
                    }
                case AllUpperAlphabetChars:
                    {
                        return new CharList(
                            Characters.UpperAlphabetChars);
                    }
                case AllLowerAlphabetChars:
                    {
                        return new CharList(
                            Characters.LowerAlphabetChars);
                    }
                default:
                    {
                        if (CharIsValidValue(character))
                            return new CharList(character);

                        break;
                    }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IDictionary<char, long> CharsToDictionary(
            IEnumerable<char> characters
            )
        {
            if (characters != null)
            {
                IDictionary<char, long> result = new CharLongDictionary();

                foreach (char character in characters)
                {
                    if (result.ContainsKey(character))
                        result[character]++;
                    else
                        result.Add(character, 1);
                }

                return result;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void CharsToDictionary(
            StringBuilder characters,
            ref IDictionary<char, long> keyFlags
            )
        {
            if (characters != null)
            {
                if (keyFlags == null)
                    keyFlags = new CharLongDictionary();

                for (int index = 0; index < characters.Length; index++)
                {
                    char character = characters[index];

                    if (keyFlags.ContainsKey(character))
                        keyFlags[character]++;
                    else
                        keyFlags.Add(character, 1);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IEnumerable<char> DictionaryToChars(
            IDictionary<char, long> dictionary,
            bool sort
            )
        {
            if (dictionary != null)
            {
                StringList keys = new StringList(dictionary.Keys);

                if (sort)
                    keys.Sort(); /* NOTE: O(N^2) is the worst case. */

                return keys.ToString(null, null, false);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IDictionary<long, string> Merge(
            IDictionary<long, IDictionary<char, long>> flags,
            bool sort
            )
        {
            IDictionary<long, string> result = new LongStringDictionary();

            foreach (KeyValuePair<long, IDictionary<char, long>> pair in flags)
            {
                result.Add(pair.Key, (string)DictionaryToChars(
                    pair.Value, sort)); /* ADD ONLY */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool Union(
            IDictionary<long, IDictionary<char, long>> X,
            StringBuilder Y,
            long key,
            bool strict
            )
        {
            if ((X == null) || (Y == null))
                return false;

            IDictionary<char, long> keyFlags;

            if (!X.TryGetValue(key, out keyFlags))
            {
                keyFlags = new CharLongDictionary();
                X.Add(key, keyFlags); /* ADD ONLY */
            }
            else if (keyFlags == null)
            {
                //
                // NOTE: This code cannot be reached.
                //
                if (strict)
                    return false;

                keyFlags = new CharLongDictionary();
                X[key] = keyFlags; /* REPLACE ONLY */
            }

            /* NO RESULT */
            CharsToDictionary(Y, ref keyFlags);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static IDictionary<char, long> Union(
            IDictionary<char, long> X,
            IDictionary<char, long> Y
            )
        {
            IDictionary<char, long> result = null;

            if ((X != null) || (Y != null))
            {
                result = new CharLongDictionary();

                if (X != null)
                {
                    foreach (KeyValuePair<char, long> pair in X)
                        result.Add(pair); /* ADD ONLY */

                    if (Y != null)
                    {
                        foreach (KeyValuePair<char, long> pair in Y)
                        {
                            if (result.ContainsKey(pair.Key))
                                result[pair.Key] += pair.Value;
                            else
                                result.Add(pair);
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<char, long> pair in Y)
                        result.Add(pair); /* ADD ONLY */
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string Union( /* NOT USED */
            string X,
            string Y,
            bool sort
            )
        {
            return (string)DictionaryToChars(
                Union(CharsToDictionary(X),
                CharsToDictionary(Y)), sort);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IDictionary<char, long> Intersection(
            IDictionary<char, long> X,
            IDictionary<char, long> Y
            )
        {
            IDictionary<char, long> result = null;

            if ((X != null) && (Y != null))
            {
                result = new CharLongDictionary();

                foreach (KeyValuePair<char, long> pair in X)
                {
                    long value;

                    if (Y.TryGetValue(pair.Key, out value))
                    {
                        result.Add(pair.Key,
                            pair.Value + value); /* ADD ONLY */
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string Intersection( /* NOT USED */
            string X,
            string Y,
            bool sort
            )
        {
            return (string)DictionaryToChars(
                Intersection(CharsToDictionary(X),
                CharsToDictionary(Y)), sort);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IDictionary<char, long> Difference(
            IDictionary<char, long> X,
            IDictionary<char, long> Y
            )
        {
            IDictionary<char, long> result = null;

            if (X != null)
            {
                result = new CharLongDictionary();

                foreach (KeyValuePair<char, long> pair in X)
                    if ((Y == null) || !Y.ContainsKey(pair.Key))
                        result.Add(pair); /* ADD ONLY */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string Difference( /* NOT USED */
            string X,
            string Y,
            bool sort
            )
        {
            return (string)DictionaryToChars(
                Difference(CharsToDictionary(X),
                CharsToDictionary(Y)), sort);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static IDictionary<long, string> Parse( /* 0.0 */
            string text,
            bool complex,
            bool space,
            bool sort,
            ref Result error
            )
        {
            if (text == null)
            {
                error = "invalid flags";
                return null;
            }

            StringBuilder name = StringOps.NewStringBuilder();
            StringBuilder value = StringOps.NewStringBuilder();

            IDictionary<long, IDictionary<char, long>> perKeyFlags =
                new Dictionary<long, IDictionary<char, long>>();

            bool nameSepatatorOk = true;
            bool haveName = false;
            bool open = false;
            long nonComplexKey = DefaultKey;
            long key = nonComplexKey;

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                switch (character)
                {
                    case Characters.OpenBrace:
                        {
                            if (!complex)
                            {
                                error = String.Format(
                                    "unexpected open brace at index {0}, simple-only mode",
                                    index);

                                return null;
                            }

                            if (open)
                            {
                                error = String.Format(
                                    "unexpected open brace at index {0}, already open",
                                    index);

                                return null;
                            }

                            //
                            // NOTE: This code cannot be reached.
                            //
                            // if (name.Length > 0)
                            // {
                            //     error = String.Format(
                            //         "unexpected name at index {0}",
                            //         index);
                            //
                            //     return null;
                            // }
                            //

                            if (value.Length > 0)
                            {
                                if (!Union(perKeyFlags, value, nonComplexKey, false))
                                {
                                    //
                                    // NOTE: This code cannot be reached.
                                    //
                                    error = String.Format(
                                        "union of flags failed at index {0} for key {1}",
                                        index, nonComplexKey);

                                    return null;
                                }

                                value.Length = 0;
                            }

                            open = true;
                            break;
                        }
                    case Characters.CloseBrace:
                        {
                            if (!complex)
                            {
                                error = String.Format(
                                    "unexpected close brace at index {0}, simple-only mode",
                                    index);

                                return null;
                            }

                            if (!open)
                            {
                                error = String.Format(
                                    "unexpected close brace at index {0}, already closed",
                                    index);

                                return null;
                            }

                            if (!haveName)
                            {
                                error = String.Format(
                                    "unexpected close brace at index {0}, name incomplete",
                                    index);

                                return null;
                            }

                            if (name.Length == 0)
                            {
                                //
                                // NOTE: This code cannot be reached.
                                //
                                error = String.Format(
                                    "unexpected close brace at index {0}, name missing",
                                    index);

                                return null;
                            }

                            if (!HexadecimalNameToKey(name.ToString(), ref key))
                            {
                                //
                                // NOTE: This code cannot be reached.
                                //
                                error = String.Format(
                                    "invalid name {0}, must be a hexadecimal long integer",
                                    FormatOps.WrapOrNull(name));

                                return null;
                            }

                            name.Length = 0;

                            if (!Union(perKeyFlags, value, key, false))
                            {
                                //
                                // NOTE: This code cannot be reached.
                                //
                                error = String.Format(
                                    "union of flags failed at index {0} for key {1}",
                                    index, key);

                                return null;
                            }

                            value.Length = 0;

                            nameSepatatorOk = true;
                            haveName = false;
                            open = false;
                            break;
                        }
                    default:
                        {
                            if (haveName || !open)
                            {
                                if (space && Parser.IsWhiteSpace(character))
                                    continue;

                                if (character == NameSepatator)
                                {
                                    if (!complex)
                                    {
                                        error = String.Format(
                                            "unexpected name separator at index {0}, simple-only mode",
                                            index);

                                        return null;
                                    }

                                    if (!nameSepatatorOk)
                                    {
                                        error = String.Format(
                                            "unexpected name separator at index {0}, already seen?",
                                            index);

                                        return null;
                                    }

                                    if (name.Length == 0)
                                    {
                                        error = String.Format(
                                            "unexpected name separator at index {0}, name missing",
                                            index);

                                        return null;
                                    }

                                    if (value.Length > 0)
                                    {
                                        error = String.Format(
                                            "unexpected name separator at index {0}, already complete",
                                            index);

                                        return null;
                                    }

                                    nameSepatatorOk = false;
                                    continue;
                                }

                                if (!CharIsValidValue(character))
                                {
                                    error = String.Format(
                                        "invalid value character '{0}' at index {1}",
                                        character, index);

                                    return null;
                                }

                                value.Append(character);
                            }
                            else
                            {
                                if (space && Parser.IsWhiteSpace(character))
                                    continue;

                                if (character == NameSepatator)
                                {
                                    if (!complex)
                                    {
                                        //
                                        // NOTE: This code cannot be reached.
                                        //
                                        error = String.Format(
                                            "unexpected name separator at index {0}, simple-only mode",
                                            index);

                                        return null;
                                    }

                                    if (!nameSepatatorOk)
                                    {
                                        //
                                        // NOTE: This code cannot be reached.
                                        //
                                        error = String.Format(
                                            "unexpected name separator at index {0}, already seen?",
                                            index);

                                        return null;
                                    }

                                    if (name.Length == 0)
                                    {
                                        error = String.Format(
                                            "unexpected name separator at index {0}, name missing",
                                            index);

                                        return null;
                                    }

                                    nameSepatatorOk = false;
                                    haveName = true;
                                    continue;
                                }

                                if (!CharIsValidName(character))
                                {
                                    error = String.Format(
                                        "invalid name character '{0}' at index {1}",
                                        character, index);

                                    return null;
                                }

                                name.Append(character);

                                if (name.Length == NameLength)
                                    haveName = true;
                            }
                            break;
                        }
                }
            }

            if (open)
            {
                error = "close brace expected";

                return null;
            }

            //
            // NOTE: This code cannot be reached.
            //
            // if (name.Length > 0)
            // {
            //     error = String.Format(
            //         "unexpected name at index {0}",
            //         text.Length);
            //
            //     return null;
            // }
            //

            if (value.Length > 0)
            {
                if (!Union(perKeyFlags, value, nonComplexKey, false))
                {
                    //
                    // NOTE: This code cannot be reached.
                    //
                    error = String.Format(
                        "union of flags failed at index {0} for key {1}",
                        text.Length, nonComplexKey);

                    return null;
                }

                /* value.Length = 0; */
            }

            //
            // NOTE: Return the merged flags dictionary to the caller, sorted
            //       if necessary.
            //
            return Merge(perKeyFlags, sort);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Format( /* 0.1 */
            IDictionary<long, string> flags,
            bool legacy,
            bool compact,
            bool space,
            bool sort,
            ref Result error
            )
        {
            if (flags != null)
            {
                StringBuilder result = StringOps.NewStringBuilder();
                LongList keys = new LongList(flags.Keys);

                if (sort)
                    keys.Sort(); /* NOTE: O(N^2) is the worst case. */

                for (int index = 0; index < keys.Count; index++)
                {
                    if (space && (index > 0) && (result.Length > 0))
                        result.Append(Characters.Space);

                    long key = keys[index];

                    string keyFlags = compact ? (string)DictionaryToChars(
                        CharsToDictionary(flags[key]), sort) : flags[key];

                    if (key != NoKey)
                    {
                        result.AppendFormat(
                            legacy ? LegacyFlagFormat : FlagFormat,
                            key, keyFlags);
                    }
                    else
                    {
                        result.Append(keyFlags);
                    }
                }

                return result.ToString();
            }

            error = "invalid flags";
            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Have( /* 1.1 */
            IDictionary<long, string> flags,
            long key,
            string haveFlags,
            bool all,
            bool strict
            )
        {
            if (flags != null)
            {
                if (haveFlags != null)
                {
                    if (flags.ContainsKey(key))
                    {
                        IDictionary<char, long> keyFlags =
                            CharsToDictionary(flags[key]);

                        if (keyFlags != null)
                        {
                            if (haveFlags.Length == 0)
                                return true; // have-none

                            for (int index = 0; index < haveFlags.Length; index++)
                            {
                                char character = haveFlags[index];

                                if (!CharIsValidValue(character))
                                {
                                    if (strict)
                                        return false; // fail
                                    else
                                        continue; // ignore
                                }

                                if (keyFlags.ContainsKey(character))
                                {
                                    if (!all)
                                        return true; // have-any

                                    continue; // have-all
                                }

                                if (all)
                                    return false; // not-have-all
                            }

                            return all; // have-all (?)
                        }
                    }
                    else if (haveFlags.Length == 0)
                    {
                        return true; // have-none
                    }
                }
                else
                {
                    return true;
                }
            }
            else if (haveFlags == null)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IDictionary<long, string> Change( /* 1.2 */
            IDictionary<long, string> flags,
            long key,
            string changeFlags,
            bool sort,
            ref Result error
            )
        {
            if (flags != null)
            {
                if (changeFlags != null)
                {
                    IDictionary<long, string> newFlags = new LongStringDictionary(flags);

                    IDictionary<char, long> keyFlags = newFlags.ContainsKey(key) ?
                        CharsToDictionary(newFlags[key]) : new CharLongDictionary();

                    bool add = true; /* NOTE: Default to add mode. */

                    for (int index = 0; index < changeFlags.Length; index++)
                    {
                        char character = changeFlags[index];

                        switch (character)
                        {
                            case AddCharacter:
                                {
                                    add = true;

                                    break;
                                }
                            case RemoveCharacter:
                                {
                                    add = false;

                                    break;
                                }
                            case SetCharacter:
                                {
                                    keyFlags.Clear();

                                    add = true;

                                    break;
                                }
                            case AllChars:
                            case AllDigitChars:
                            case AllAlphabetChars:
                            case AllUpperAlphabetChars:
                            case AllLowerAlphabetChars:
                                {
                                    IEnumerable<char> characters = GetChars(character);

                                    if (characters != null)
                                    {
                                        foreach (char newCharacter in characters)
                                        {
                                            if (add)
                                            {
                                                if (keyFlags.ContainsKey(newCharacter))
                                                    keyFlags[newCharacter]++;
                                                else
                                                    keyFlags.Add(newCharacter, 1);
                                            }
                                            else if (keyFlags.ContainsKey(newCharacter))
                                            {
                                                /* IGNORED */
                                                keyFlags.Remove(newCharacter);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "invalid change meta-character '{0}' at index {1}",
                                            character, index);

                                        return null;
                                    }
                                    break;
                                }
                            default:
                                {
                                    if (!CharIsValidValue(character))
                                    {
                                        error = String.Format(
                                            "invalid change character '{0}' at index {1}",
                                            character, index);

                                        return null;
                                    }

                                    if (add)
                                    {
                                        if (keyFlags.ContainsKey(character))
                                            keyFlags[character]++;
                                        else
                                            keyFlags.Add(character, 1);
                                    }
                                    else if (keyFlags.ContainsKey(character))
                                    {
                                        /* IGNORED */
                                        keyFlags.Remove(character);
                                    }
                                    break;
                                }
                        }
                    }

                    if (newFlags.ContainsKey(key))
                    {
                        if (keyFlags.Count > 0)
                        {
                            newFlags[key] = (string)DictionaryToChars(
                                keyFlags, sort);
                        }
                        else
                        {
                            /* IGNORED */
                            newFlags.Remove(key);
                        }
                    }
                    else if (keyFlags.Count > 0)
                    {
                        newFlags.Add(key, (string)DictionaryToChars(
                            keyFlags, sort)); /* ADD ONLY */
                    }

                    return newFlags;
                }
                else
                {
                    error = "invalid change flags";
                }
            }
            else
            {
                error = "invalid flags";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Verify( /* 2.1 */
            string text,
            bool complex,
            bool space,
            ref Result error
            )
        {
            return (Parse(text, complex, space, false, ref error) != null);
        }
        #endregion
    }
}
