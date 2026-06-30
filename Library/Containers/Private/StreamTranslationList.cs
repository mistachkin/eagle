/*
 * StreamTranslationList.cs --
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
    /// This class represents a list of stream translation values
    /// (<see cref="StreamTranslation" />).
    /// </summary>
    [ObjectId("8298f31d-d97f-4555-b300-a2a1dacc2806")]
    internal sealed class StreamTranslationList : List<StreamTranslation>, ICloneable
    {
        /// <summary>
        /// Constructs an empty list of stream translations.
        /// </summary>
        public StreamTranslationList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of stream translations that contains the specified
        /// input and output translations.
        /// </summary>
        /// <param name="inTranslation">
        /// The stream translation to use for input.
        /// </param>
        /// <param name="outTranslation">
        /// The stream translation to use for output.
        /// </param>
        public StreamTranslationList(
            StreamTranslation inTranslation,
            StreamTranslation outTranslation
            )
            : base(new StreamTranslation[] { inTranslation, outTranslation })
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of stream translations that contains the elements
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of stream translations whose elements are copied into
        /// the new list.
        /// </param>
        public StreamTranslationList(IEnumerable<StreamTranslation> collection)
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
            return ParserOps<StreamTranslation>.ListToString(this, Index.Invalid, Index.Invalid,
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
        /// This method creates a new list of stream translations that is a copy
        /// of this list.
        /// </summary>
        /// <returns>
        /// The new list that is a copy of this list.
        /// </returns>
        public object Clone()
        {
            return new StreamTranslationList(this);
        }
        #endregion
    }
}

