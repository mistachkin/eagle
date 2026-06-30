/*
 * CommandDataList.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents an ordered, dynamically sized list of command
    /// data objects, each of which implements the
    /// <see cref="ICommandData" /> interface.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("344684c4-2a5d-4975-88bc-c555228ef10b")]
    public sealed class CommandDataList : List<ICommandData>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public CommandDataList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the elements copied
        /// from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied into the new instance.
        /// </param>
        public CommandDataList(IEnumerable<ICommandData> collection)
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the elements of this list into a string
        /// representation, optionally filtering them using the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the elements included in the resulting
        /// string, or null to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the matching elements of this list.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<ICommandData>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method converts all the elements of this list into a string
        /// representation.
        /// </summary>
        /// <returns>
        /// The string representation of all the elements of this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
