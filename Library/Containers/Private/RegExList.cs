/*
 * RegExList.cs --
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a list of regular expressions.  It extends the
    /// standard generic list with bulk-add helpers, conversion to the Eagle
    /// string list format, and support for cloning.
    /// </summary>
    [ObjectId("2ea84c90-4599-4776-bef4-db5c6b10758d")]
    internal sealed class RegExList : List<Regex>, ICloneable
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public RegExList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied into the new list.
        /// </param>
        public RegExList(IEnumerable<Regex> collection)
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the two specified collections.
        /// </summary>
        /// <param name="collection1">
        /// The first collection whose elements are copied into the new list.
        /// This parameter may be null.
        /// </param>
        /// <param name="collection2">
        /// The second collection whose elements are copied into the new list.
        /// This parameter may be null.
        /// </param>
        public RegExList(
            IEnumerable<Regex> collection1,
            IEnumerable<Regex> collection2
            )
            : base()
        {
            if (collection1 != null)
                Add(collection1);

            if (collection2 != null)
                Add(collection2);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the keys copied
        /// from the two specified dictionaries.
        /// </summary>
        /// <param name="dictionary1">
        /// The first dictionary whose keys are copied into the new list.  This
        /// parameter may be null.
        /// </param>
        /// <param name="dictionary2">
        /// The second dictionary whose keys are copied into the new list.  This
        /// parameter may be null.
        /// </param>
        public RegExList(
            IDictionary<Regex, Enum> dictionary1,
            IDictionary<Regex, Enum> dictionary2
            )
            : base()
        {
            if (dictionary1 != null)
                Add(dictionary1);

            if (dictionary2 != null)
                Add(dictionary2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all of the elements of the specified collection to
        /// the list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are added to the list.
        /// </param>
        public void Add(
            IEnumerable<Regex> collection
            )
        {
            foreach (Regex item in collection)
                base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all of the keys of the specified dictionary to the
        /// list.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys are added to the list.
        /// </param>
        public void Add(
            IDictionary<Regex, Enum> dictionary
            )
        {
            Add(dictionary.Keys);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts this list to a string in the Eagle list format, optionally
        /// including only those elements matching the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each element must match in order to be included in
        /// the resulting string.  This parameter may be null, in which case all
        /// elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<Regex>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts this list to a string in the Eagle list format.
        /// </summary>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new list that is a copy of this list.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            return new RegExList(this);
        }
        #endregion
    }
}
