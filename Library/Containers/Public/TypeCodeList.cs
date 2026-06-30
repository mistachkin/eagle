/*
 * TypeCodeList.cs --
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
    /// This class represents a list of runtime type codes
    /// (<see cref="TypeCode" />).
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d5081bdf-71e4-4add-9f6d-9af95b2d8cf9")]
    public sealed class TypeCodeList : List<TypeCode>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty list of type codes.
        /// </summary>
        public TypeCodeList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of type codes that contains the type codes copied
        /// from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of type codes whose elements are copied into the new
        /// list.
        /// </param>
        public TypeCodeList(
            IEnumerable<TypeCode> collection /* in */
            )
            : base(collection)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this list, with the
        /// type codes separated by spaces.
        /// </summary>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public override string ToString()
        {
            return ParserOps<TypeCode>.ListToString(this,
                Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }
        #endregion
    }
}
