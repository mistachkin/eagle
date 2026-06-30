/*
 * IntArgumentInfoDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    int, Eagle._Components.Private.ArgumentInfo>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    int, Eagle._Components.Private.ArgumentInfo>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps integer keys to argument
    /// information values.  It extends the underlying generic dictionary of
    /// <see cref="ArgumentInfo" /> objects with a helper for producing a
    /// filtered string form of its values.
    /// </summary>
    [ObjectId("a4e59dc4-3b6c-4f41-a633-8b03a086e6b0")]
    internal sealed class IntArgumentInfoDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty integer-to-argument-information dictionary.
        /// </summary>
        public IntArgumentInfoDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method produces a string containing the values of the
        /// dictionary that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which values are included.  This parameter
        /// may be null to include all values.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching values formatted as a string.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<ArgumentInfo>.ListToString(
                new ArgumentInfoList(this.Values),
                Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all the values of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The list of values formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
