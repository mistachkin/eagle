/*
 * PairList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a list of pairs, where each element associates two
    /// values of the same type.  It extends the standard generic list with a
    /// type name suitable for use within Eagle.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the two values associated by each pair in the list.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("fc0e1512-3837-462e-a7bb-01460fcede60")]
    internal class PairList<T> : List<IPair<T>>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PairList()
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
        public PairList(
            IEnumerable<IPair<T>> collection
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
        public PairList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the specified
        /// pairs.
        /// </summary>
        /// <param name="pairs">
        /// The pairs to be added to the new list.
        /// </param>
        public PairList(
            params IPair<T>[] pairs
            )
            : base(pairs)
        {
            // do nothing.
        }
    }
}
