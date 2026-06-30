/*
 * IntPtrList.cs --
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

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a list of native pointer values.  It extends the
    /// underlying generic list of <see cref="IntPtr" /> values, supports
    /// cloning, and provides a helper for producing a filtered string form of
    /// its elements.
    /// </summary>
    [ObjectId("120b198d-4480-435f-8025-3efc21cf4856")]
    internal sealed class IntPtrList : List<IntPtr>, ICloneable
    {
        /// <summary>
        /// Constructs an empty native pointer list.
        /// </summary>
        public IntPtrList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a native pointer list that is initialized with the
        /// elements copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied into the new list.
        /// </param>
        public IntPtrList(IEnumerable<IntPtr> collection)
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the elements of the list
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which elements are included.  This
        /// parameter may be null to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching elements formatted as a string.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<IntPtr>.ListToString(this, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all the elements of the
        /// list.
        /// </summary>
        /// <returns>
        /// The list of elements formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new list that is a shallow copy of this list.
        /// </summary>
        /// <returns>
        /// A new list containing the same elements as this list.
        /// </returns>
        public object Clone()
        {
            return new IntPtrList(this);
        }
        #endregion
    }
}
