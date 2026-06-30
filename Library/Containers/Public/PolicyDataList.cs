/*
 * PolicyDataList.cs --
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
    /// This class represents a strongly typed list of policy data objects,
    /// each of which describes a policy used by the script engine.  It
    /// extends the generic list type with support for formatting its
    /// elements as a string list and, when serialization is enabled,
    /// supports being serialized.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("3eec821f-786a-4702-b9cb-512d78602cc6")]
    public sealed class PolicyDataList : List<IPolicyData>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PolicyDataList()
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
        /// The collection whose policy data elements are copied into the new
        /// list.
        /// </param>
        public PolicyDataList(
            IEnumerable<IPolicyData> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string representation of the elements
        /// contained in this list, optionally limiting the elements to those
        /// matching the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match the string representation of the elements
        /// to include, if any.  This parameter may be null, in which case all
        /// elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// A string containing the formatted list of policy data elements.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<IPolicyData>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string representation of all the elements
        /// contained in this list.
        /// </summary>
        /// <returns>
        /// A string containing the formatted list of policy data elements.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
