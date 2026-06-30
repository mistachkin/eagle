/*
 * TclThreadList.cs --
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
using Eagle._Components.Private.Tcl;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private.Tcl
{
    /// <summary>
    /// This class represents a list of native Tcl thread objects
    /// (<see cref="TclThread" />).
    /// </summary>
    [ObjectId("f9b786c5-37dd-41b0-840d-90fcb941442f")]
    internal sealed class TclThreadList : List<TclThread>, ICloneable
    {
        /// <summary>
        /// Constructs an empty list of Tcl threads.
        /// </summary>
        public TclThreadList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of Tcl threads that contains the elements copied
        /// from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of Tcl threads whose elements are copied into the new
        /// list.
        /// </param>
        public TclThreadList(IEnumerable<TclThread> collection)
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the elements of this list
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the elements that are included in the
        /// result.  This parameter may be null, in which case all elements are
        /// included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The list of matching elements formatted as a string.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<TclThread>.ListToString(this, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all of the elements of this
        /// list.
        /// </summary>
        /// <returns>
        /// The elements of this list formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new list of Tcl threads that is a copy of this
        /// list.
        /// </summary>
        /// <returns>
        /// The new list that is a copy of this list.
        /// </returns>
        public object Clone()
        {
            return new TclThreadList(this);
        }
        #endregion
    }
}
