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
    /// <summary>
    /// This class provides a collection of static helper methods used to parse,
    /// format, query, and modify a custom textual "attribute flags" format.  In
    /// this format, each set of single-character flags is associated with a
    /// 64-bit integer key; the flags for the default key may appear without a
    /// surrounding key prefix, while the flags for any other key are enclosed in
    /// braces and preceded by the hexadecimal key and a separator.
    /// </summary>
    [ObjectId("17b64eff-174f-4856-bafd-df94c759de17")]
    internal static class AttributeFlags
    {
        #region Private Constants
        /// <summary>
        /// The character used to separate a hexadecimal key from its associated
        /// flags within a complex (keyed) flag specification.
        /// </summary>
        private static readonly char NameSeparator = Characters.Colon;

        /// <summary>
        /// The number of hexadecimal digits used to represent a key in the
        /// legacy fixed-width flag format (i.e. a 64-bit integer).
        /// </summary>
        private static readonly int NameLength = 16; // (i.e. hexadecimal 64-bit integer)

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The meta-character that switches a change operation into add mode, so
        /// that subsequent flag characters are added.
        /// </summary>
        public const char AddCharacter = Characters.PlusSign;

        /// <summary>
        /// The meta-character that switches a change operation into remove mode,
        /// so that subsequent flag characters are removed.
        /// </summary>
        private const char RemoveCharacter = Characters.MinusSign;

        /// <summary>
        /// The meta-character that clears the existing flags for the key and
        /// switches a change operation into add mode.
        /// </summary>
        private const char SetCharacter = Characters.EqualSign;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The meta-character that expands to all digit, uppercase alphabet, and
        /// lowercase alphabet flag characters.
        /// </summary>
        private const char AllChars = Characters.Asterisk;

        /// <summary>
        /// The meta-character that expands to all digit flag characters.
        /// </summary>
        private const char AllDigitChars = Characters.NumberSign;

        /// <summary>
        /// The meta-character that expands to all uppercase and lowercase
        /// alphabet flag characters.
        /// </summary>
        private const char AllAlphabetChars = Characters.ExclamationMark;

        /// <summary>
        /// The meta-character that expands to all uppercase alphabet flag
        /// characters.
        /// </summary>
        private const char AllUpperAlphabetChars = Characters.DollarSign;

        /// <summary>
        /// The meta-character that expands to all lowercase alphabet flag
        /// characters.
        /// </summary>
        private const char AllLowerAlphabetChars = Characters.AtSign;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The composite format string used to render a keyed set of flags in
        /// the legacy format, where the key is a fixed-width hexadecimal integer
        /// immediately followed by the flags, all enclosed in braces.
        /// </summary>
        private static readonly string LegacyFlagFormat =
            "{{{0:X" + NameLength.ToString() + "}{1}}}";

        /// <summary>
        /// The composite format string used to render a keyed set of flags in
        /// the current format, where the variable-width hexadecimal key and the
        /// flags are separated by a colon and enclosed in braces.
        /// </summary>
        private static readonly string FlagFormat = "{{{0:X}:{1}}}";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The key value that represents the absence of a key, whose flags are
        /// rendered without any surrounding key prefix or braces.
        /// </summary>
        private static readonly long NoKey = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constants
        /// <summary>
        /// The default key used for flags that are not associated with an
        /// explicit key (i.e. the no-key value).
        /// </summary>
        public static readonly long DefaultKey = NoKey;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        /// <summary>
        /// This method converts a hexadecimal name string into its
        /// corresponding 64-bit integer key.
        /// </summary>
        /// <param name="name">
        /// The hexadecimal name string to convert.
        /// </param>
        /// <param name="key">
        /// Upon success, receives the key parsed from the name.
        /// </param>
        /// <returns>
        /// True if the entire name was parsed as a hexadecimal integer;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified character is valid
        /// within a key name (i.e. a hexadecimal digit).
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is valid within a key name; otherwise, false.
        /// </returns>
        private static bool CharIsValidName(
            char character
            )
        {
            return Parser.IsHexadecimalDigit(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is valid as a
        /// flag value character (i.e. an identifier character).
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is valid as a flag value character; otherwise,
        /// false.
        /// </returns>
        private static bool CharIsValidValue(
            char character
            )
        {
            return Parser.IsIdentifier(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method expands a flag character into the set of characters it
        /// represents, handling the meta-characters that stand for groups of
        /// digit and alphabet characters.
        /// </summary>
        /// <param name="character">
        /// The flag (or meta-) character to expand.
        /// </param>
        /// <returns>
        /// The set of characters represented by the input character, or null
        /// when the character is not a recognized meta-character and is not a
        /// valid flag value character.
        /// </returns>
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

        /// <summary>
        /// This method builds a dictionary that maps each distinct character in
        /// the specified sequence to the number of times it occurs.
        /// </summary>
        /// <param name="characters">
        /// The sequence of characters to tally.
        /// </param>
        /// <returns>
        /// A dictionary mapping each character to its occurrence count, or null
        /// when the input sequence is null.
        /// </returns>
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

        /// <summary>
        /// This method tallies the characters in the specified string builder
        /// into the specified character-count dictionary, creating the
        /// dictionary when it does not yet exist.
        /// </summary>
        /// <param name="characters">
        /// The characters to tally.
        /// </param>
        /// <param name="keyFlags">
        /// The dictionary mapping each character to its occurrence count.  When
        /// null upon entry, a new dictionary is created and returned via this
        /// parameter.
        /// </param>
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

        /// <summary>
        /// This method flattens a character-count dictionary back into the
        /// sequence of its keys, optionally sorting them.
        /// </summary>
        /// <param name="dictionary">
        /// The character-count dictionary whose keys are flattened.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the resulting characters are sorted.
        /// </param>
        /// <returns>
        /// A string containing the dictionary keys (as the underlying
        /// enumerable of characters), or null when the dictionary is null.
        /// </returns>
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

        /// <summary>
        /// This method merges a per-key dictionary of character-count
        /// dictionaries into a per-key dictionary of flag strings, optionally
        /// sorting the flags for each key.
        /// </summary>
        /// <param name="flags">
        /// The per-key character-count dictionaries to merge.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the flags for each key are sorted.
        /// </param>
        /// <returns>
        /// A dictionary mapping each key to its merged flag string.
        /// </returns>
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

        /// <summary>
        /// This method merges the flag characters accumulated for a single key
        /// into the per-key character-count dictionaries, creating the entry for
        /// the key when it does not yet exist.
        /// </summary>
        /// <param name="X">
        /// The per-key character-count dictionaries into which the flags are
        /// merged.
        /// </param>
        /// <param name="Y">
        /// The accumulated flag characters to merge for the specified key.
        /// </param>
        /// <param name="key">
        /// The key whose flag characters are being merged.
        /// </param>
        /// <param name="strict">
        /// When non-zero, a null existing entry for the key is treated as a
        /// failure; otherwise, it is replaced with a new dictionary.
        /// </param>
        /// <returns>
        /// True if the flags were merged; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method computes the union of two flag dictionaries, summing
        /// the values for any keys present in both.
        /// </summary>
        /// <param name="X">
        /// The first dictionary to combine.
        /// </param>
        /// <param name="Y">
        /// The second dictionary to combine.
        /// </param>
        /// <returns>
        /// A new dictionary containing the union of the two dictionaries, or
        /// null if both are null.
        /// </returns>
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

        /// <summary>
        /// This method computes the union of the flag characters contained in
        /// two strings.
        /// </summary>
        /// <param name="X">
        /// The first string of flag characters to combine.
        /// </param>
        /// <param name="Y">
        /// The second string of flag characters to combine.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the resulting flag characters are sorted.
        /// </param>
        /// <returns>
        /// A string containing the union of the flag characters, or null if
        /// both strings are null.
        /// </returns>
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

        /// <summary>
        /// This method computes the intersection of two flag dictionaries,
        /// summing the values for the keys present in both.
        /// </summary>
        /// <param name="X">
        /// The first dictionary to combine.
        /// </param>
        /// <param name="Y">
        /// The second dictionary to combine.
        /// </param>
        /// <returns>
        /// A new dictionary containing the intersection of the two
        /// dictionaries, or null if either is null.
        /// </returns>
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

        /// <summary>
        /// This method computes the intersection of the flag characters
        /// contained in two strings.
        /// </summary>
        /// <param name="X">
        /// The first string of flag characters to combine.
        /// </param>
        /// <param name="Y">
        /// The second string of flag characters to combine.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the resulting flag characters are sorted.
        /// </param>
        /// <returns>
        /// A string containing the intersection of the flag characters, or
        /// null if either string is null.
        /// </returns>
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

        /// <summary>
        /// This method computes the difference of two flag dictionaries,
        /// containing the entries from the first that are not present in the
        /// second.
        /// </summary>
        /// <param name="X">
        /// The dictionary whose entries are retained.
        /// </param>
        /// <param name="Y">
        /// The dictionary whose entries are removed from the first.
        /// </param>
        /// <returns>
        /// A new dictionary containing the difference of the two dictionaries,
        /// or null if the first dictionary is null.
        /// </returns>
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

        /// <summary>
        /// This method computes the difference of the flag characters
        /// contained in two strings.
        /// </summary>
        /// <param name="X">
        /// The string of flag characters that are retained.
        /// </param>
        /// <param name="Y">
        /// The string of flag characters that are removed from the first.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the resulting flag characters are sorted.
        /// </param>
        /// <returns>
        /// A string containing the difference of the flag characters, or null
        /// if the first string is null.
        /// </returns>
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
        /// <summary>
        /// This method parses a textual attribute flags specification into a
        /// dictionary that maps each key to its set of flags.  In complex mode,
        /// keyed flags enclosed in braces are recognized; in simple mode, only
        /// the flags for the default key are recognized.
        /// </summary>
        /// <param name="text">
        /// The textual attribute flags specification to parse.
        /// </param>
        /// <param name="complex">
        /// When non-zero, complex (keyed, braced) specifications are permitted;
        /// otherwise, only simple specifications are permitted.
        /// </param>
        /// <param name="space">
        /// When non-zero, whitespace characters in the text are ignored.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the flags for each key are sorted in the result.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// A dictionary mapping each key to its flag string, or null on failure.
        /// </returns>
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

            StringBuilder name = StringBuilderFactory.CreateNoCache(); /* EXEMPT */
            StringBuilder value = StringBuilderFactory.CreateNoCache(); /* EXEMPT */

            IDictionary<long, IDictionary<char, long>> perKeyFlags =
                new Dictionary<long, IDictionary<char, long>>();

            bool nameSeparatorOk = true;
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

                            nameSeparatorOk = true;
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

                                if (character == NameSeparator)
                                {
                                    if (!complex)
                                    {
                                        error = String.Format(
                                            "unexpected name separator at index {0}, simple-only mode",
                                            index);

                                        return null;
                                    }

                                    if (!nameSeparatorOk)
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

                                    nameSeparatorOk = false;
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

                                if (character == NameSeparator)
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

                                    if (!nameSeparatorOk)
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

                                    nameSeparatorOk = false;
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

        /// <summary>
        /// This method renders a dictionary of per-key flags back into its
        /// textual attribute flags representation.
        /// </summary>
        /// <param name="flags">
        /// The dictionary mapping each key to its flag string.
        /// </param>
        /// <param name="legacy">
        /// When non-zero, keyed flags are rendered using the legacy fixed-width
        /// key format; otherwise, the current key format is used.
        /// </param>
        /// <param name="compact">
        /// When non-zero, duplicate flag characters are collapsed before
        /// rendering.
        /// </param>
        /// <param name="space">
        /// When non-zero, a space is inserted between successive keyed groups.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the keys and the flags within each key are sorted.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The textual attribute flags representation, or null on failure.
        /// </returns>
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
                StringBuilder result = StringBuilderFactory.Create();
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

                return StringBuilderCache.GetStringAndRelease(ref result);
            }

            error = "invalid flags";
            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the flags associated with the
        /// specified key include some or all of the specified flag characters.
        /// </summary>
        /// <param name="flags">
        /// The dictionary mapping each key to its flag string.
        /// </param>
        /// <param name="key">
        /// The key whose flags are tested.
        /// </param>
        /// <param name="haveFlags">
        /// The flag characters to test for.  An empty string tests for the
        /// presence of no flags (have-none).  A null value always matches.
        /// </param>
        /// <param name="all">
        /// When non-zero, all of the specified flags must be present; otherwise,
        /// any one of them being present is sufficient.
        /// </param>
        /// <param name="strict">
        /// When non-zero, an invalid flag value character causes the test to
        /// fail; otherwise, such characters are ignored.
        /// </param>
        /// <returns>
        /// True if the required flags are present according to the specified
        /// criteria; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method applies a sequence of change operations to the flags
        /// associated with the specified key, returning a new dictionary with
        /// the modified flags.  The change string may contain add, remove, and
        /// set meta-characters as well as the meta-characters that expand to
        /// groups of flag characters.
        /// </summary>
        /// <param name="flags">
        /// The dictionary mapping each key to its flag string.
        /// </param>
        /// <param name="key">
        /// The key whose flags are changed.
        /// </param>
        /// <param name="changeFlags">
        /// The change specification to apply.  Flags default to being added
        /// unless a remove or set meta-character is encountered.
        /// </param>
        /// <param name="sort">
        /// When non-zero, the resulting flags for the key are sorted.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// A new dictionary containing the modified flags, or null on failure.
        /// </returns>
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

        /// <summary>
        /// This method verifies that the specified text is a well-formed textual
        /// attribute flags specification by attempting to parse it.
        /// </summary>
        /// <param name="text">
        /// The textual attribute flags specification to verify.
        /// </param>
        /// <param name="complex">
        /// When non-zero, complex (keyed, braced) specifications are permitted;
        /// otherwise, only simple specifications are permitted.
        /// </param>
        /// <param name="space">
        /// When non-zero, whitespace characters in the text are ignored.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the text was parsed successfully; otherwise, false.
        /// </returns>
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
