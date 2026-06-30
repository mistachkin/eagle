/*
 * ObjectArrayList.cs --
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a list of object arrays, where each element is
    /// itself an array of objects (e.g. a list of rows).  It extends the
    /// standard generic list with conversion to the Eagle string list format
    /// and the ability to be cloned.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d29cbbf9-8a2e-40b4-bbc1-903f3d6fdc95")]
    public sealed class ObjectArrayList : List<object[]>, ICloneable
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ObjectArrayList()
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
        public ObjectArrayList(
            IEnumerable<object[]> collection /* in */
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that has the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements that the new list can initially store.
        /// </param>
        public ObjectArrayList(
            int capacity /* in */
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class that contains the specified
        /// object arrays.
        /// </summary>
        /// <param name="objects">
        /// The object arrays used to populate the new list.
        /// </param>
        public ObjectArrayList(
            params object[][] objects /* in */
            )
            : base(objects)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts this list to a string in the Eagle list format.  Each
        /// element array is itself converted to a nested list, optionally
        /// including only those sub-elements matching the specified pattern.
        /// Null elements and sub-elements are skipped.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each sub-element must match in order to be included
        /// in the resulting string.  This parameter may be null, in which case
        /// all sub-elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public string ToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = new StringList();

            foreach (object[] element in this)
            {
                if (element == null)
                    continue;

                StringList subList = new StringList();

                foreach (object subElement in element)
                {
                    if (subElement == null)
                        continue;

                    string subElementString =
                        StringOps.GetStringFromObject(subElement);

                    if ((pattern != null) && !StringOps.Match(
                            null, StringOps.DefaultMatchMode,
                            subElementString, pattern, noCase))
                    {
                        continue;
                    }

                    subList.Add(subElementString);
                }

                list.Add(subList.ToString());
            }

            return list.ToString();
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
        /// Creates a new list that is a shallow copy of this list.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            return new ObjectArrayList(this);
        }
        #endregion
    }
}
