/*
 * TypePairDictionary.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps types to a pair of values
    /// associated with those types.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first value in each associated pair.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second value in each associated pair.
    /// </typeparam>
    [ObjectId("6c1d18ae-540c-458a-9c07-de27dd62b4df")]
    internal sealed class TypePairDictionary<T1, T2> :
#if FAST_DICTIONARY
        FastDictionary<Type, IAnyPair<T1, T2>>
#else
        Dictionary<Type, IAnyPair<T1, T2>>
#endif
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public TypePairDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method builds a string representation of the type names and
        /// their associated pair values in this dictionary, optionally filtered
        /// by a match pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the type names.  This parameter may be
        /// null to include all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string representation of the (optionally filtered) type names
        /// and their associated pair values in this dictionary.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<Type, IAnyPair<T1, T2>>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a string representation of the type names in this
        /// dictionary, optionally filtered by a match pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the type names.  This parameter may be
        /// null to include all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string representation of the (optionally filtered) type names in
        /// this dictionary.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method builds a string representation of the type names in this
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The string representation of the type names in this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
