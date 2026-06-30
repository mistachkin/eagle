/*
 * RegExEnumDictionary.cs --
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
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    System.Text.RegularExpressions.Regex, System.Enum>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    System.Text.RegularExpressions.Regex, System.Enum>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps regular expressions to
    /// enumerated values.  It extends the underlying generic dictionary with
    /// helpers for bulk-adding key/value pairs and for producing a filtered
    /// string form of its keys.
    /// </summary>
    [ObjectId("17d57884-7b6c-479d-9519-4b6b81ba9562")]
    internal sealed class RegExEnumDictionary : SomeDictionary
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public RegExEnumDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// specified regular expression keys and enumerated values.
        /// </summary>
        /// <param name="keys">
        /// The regular expressions to use as the keys of the dictionary.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the values belong to.
        /// </param>
        /// <param name="values">
        /// The enumerated values to associate with the keys.  If there are
        /// fewer values than keys, the remaining keys are associated with the
        /// zero value of the enumerated type.
        /// </param>
        public RegExEnumDictionary(IEnumerable<Regex> keys, Type enumType, IEnumerable<Enum> values)
            : this()
        {
            Add(keys, enumType, values);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified regular expression keys and their
        /// associated enumerated values to the dictionary.
        /// </summary>
        /// <param name="keys">
        /// The regular expressions to use as the keys of the dictionary.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the values belong to.
        /// </param>
        /// <param name="values">
        /// The enumerated values to associate with the keys.  If there are
        /// fewer values than keys, the remaining keys are associated with the
        /// zero value of the enumerated type.
        /// </param>
        /// <returns>
        /// True if the key/value pairs were successfully added; otherwise,
        /// false.
        /// </returns>
        public bool Add(IEnumerable<Regex> keys, Type enumType, IEnumerable<Enum> values)
        {
            Result error = null;

            return Add(keys, enumType, values, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified regular expression keys and their
        /// associated enumerated values to the dictionary.
        /// </summary>
        /// <param name="keys">
        /// The regular expressions to use as the keys of the dictionary.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the values belong to.
        /// </param>
        /// <param name="values">
        /// The enumerated values to associate with the keys.  If there are
        /// fewer values than keys, the remaining keys are associated with the
        /// zero value of the enumerated type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the key/value pairs were successfully added; otherwise,
        /// false.
        /// </returns>
        public bool Add(IEnumerable<Regex> keys, Type enumType, IEnumerable<Enum> values, ref Result error)
        {
            object zeroValue = EnumOps.TryGet(enumType, 0, ref error);

            if (zeroValue != null)
            {
                IEnumerator<Enum> enumerator = values.GetEnumerator(); /* throw */
                bool moveNext = true;

                foreach (Regex key in keys)
                {
                    //
                    // NOTE: If we run out of values before keys, zero fill the
                    //       rest.
                    //
                    object value = zeroValue;

                    //
                    // NOTE: Are we able to continue moving through the items?
                    //
                    if (moveNext)
                    {
                        //
                        // NOTE: Move to the next item.  If this fails, there are
                        //       no more items and we cannot move any farther.
                        //
                        if (!enumerator.MoveNext())
                            moveNext = false;

                        //
                        // NOTE: Get the value of the current item.
                        //
                        value = enumerator.Current;
                    }

                    //
                    // NOTE: Add this key/value pair to the dictionary.
                    //
                    this.Add(key, (Enum)value); /* throw */
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys that are included in the result.
        /// This parameter may be null, in which case all keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The matching keys formatted as a string.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            RegExList list = new RegExList(this.Keys);

            return ParserOps<Regex>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all of the keys of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The keys of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
