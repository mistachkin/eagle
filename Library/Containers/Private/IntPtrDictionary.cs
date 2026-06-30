/*
 * IntPtrDictionary.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, System.IntPtr>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, System.IntPtr>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to native
    /// pointer values.  It extends the underlying generic dictionary of
    /// <see cref="IntPtr" /> values with helpers for bulk insertion, for
    /// removing entries by value, and for producing a filtered string form of
    /// its keys.
    /// </summary>
    [ObjectId("0cbceb62-1dbb-4fa3-b73a-f99d077599f0")]
    internal sealed class IntPtrDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty native pointer dictionary.
        /// </summary>
        public IntPtrDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a native pointer dictionary that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public IntPtrDictionary(IDictionary<string, IntPtr> dictionary)
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method removes the first entry whose value equals the specified
        /// native pointer.
        /// </summary>
        /// <param name="value">
        /// The native pointer value to remove.
        /// </param>
        /// <returns>
        /// True if an entry was removed; otherwise, false.
        /// </returns>
        public bool RemoveAny(
            IntPtr value
            )
        {
            return RemoveAll(value, 1) > 0;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the entries whose value equals the specified
        /// native pointer, up to the specified limit.
        /// </summary>
        /// <param name="value">
        /// The native pointer value to remove.
        /// </param>
        /// <param name="limit">
        /// The maximum number of entries to remove, or
        /// <see cref="Limits.Unlimited" /> to remove all matching entries.
        /// </param>
        /// <returns>
        /// The number of entries that were removed.
        /// </returns>
        public int RemoveAll(
            IntPtr value,
            int limit
            )
        {
            int removed = 0;
            StringList list = new StringList();

            foreach (KeyValuePair<string, IntPtr> pair in this)
                if (pair.Value == value)
                    list.Add(pair.Key);

            foreach (string element in list)
                if ((limit == Limits.Unlimited) || (removed < limit))
                    removed += ConversionOps.ToInt(this.Remove(element));

            return removed;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null to include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all of the key/value pairs contained in the
        /// specified dictionary to this dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are added to this dictionary.
        /// </param>
        public void Add(
            IDictionary<string, IntPtr> dictionary
            )
        {
            foreach (KeyValuePair<string, IntPtr> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the key/value pairs from the specified input
        /// dictionary to this dictionary, except for those whose keys are
        /// already present; the conflicting pairs are placed into the output
        /// dictionary instead.
        /// </summary>
        /// <param name="inputDictionary">
        /// The dictionary whose key/value pairs are to be added.
        /// </param>
        /// <param name="outputDictionary">
        /// Receives the key/value pairs from <paramref name="inputDictionary" />
        /// whose keys are already present in this dictionary.  When null and a
        /// conflicting pair is encountered, a new dictionary is created.
        /// </param>
        public void MaybeAdd(
            IDictionary<string, IntPtr> inputDictionary,
            ref IntPtrDictionary outputDictionary
            )
        {
            foreach (KeyValuePair<string, IntPtr> pair in inputDictionary)
            {
                if (this.ContainsKey(pair.Key))
                {
                    if (outputDictionary == null)
                        outputDictionary = new IntPtrDictionary();

                    outputDictionary.Add(pair.Key, pair.Value);
                    continue;
                }

                this.Add(pair.Key, pair.Value);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all the keys of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The list of keys formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
