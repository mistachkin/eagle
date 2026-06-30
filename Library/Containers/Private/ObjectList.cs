/*
 * ObjectList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
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
    /// This class represents a list of arbitrary object references.  It extends
    /// the standard generic list with the ability to add ranges from a
    /// non-generic collection, to be converted to the Eagle list format, and to
    /// be cloned.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e2a2b42b-4899-4176-a2b4-3dedb97addde")]
    internal sealed class ObjectList : List<object>, ICloneable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ObjectList()
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
        public ObjectList(
            IEnumerable<object> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the specified
        /// objects.
        /// </summary>
        /// <param name="objects">
        /// The objects to be added to the new list.
        /// </param>
        public ObjectList(
            params object[] objects
            )
            : base(objects)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Adds the elements of the specified non-generic collection to the end
        /// of this list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are added to this list.
        /// </param>
        public void AddRange(
            IEnumerable collection
            )
        {
            foreach (object item in collection)
                base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts this list to a string in the Eagle list format, optionally
        /// including only those elements matching the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the elements, or null to include all of
        /// them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string, in the Eagle list format, that represents this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<object>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts this list to a string in the Eagle list format.
        /// </summary>
        /// <returns>
        /// The string, in the Eagle list format, that represents this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// Creates a new list that is a copy of this one.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            return new ObjectList(this);
        }
        #endregion
    }
}
