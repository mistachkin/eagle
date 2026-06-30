/*
 * ClientDataList.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a linked list of objects that implement the
    /// <see cref="IClientData" /> interface.  It extends the standard generic
    /// linked list with conversion to the Eagle string list format, including
    /// optional separator selection and pattern matching.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("a84e03f3-7935-4976-8dd9-d8ad33153fd9")]
    internal sealed class ClientDataList : LinkedList<IClientData>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ClientDataList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the list.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private ClientDataList(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts this list to a string, using the specified separator between
        /// elements and optionally including only those elements matching the
        /// specified pattern.
        /// </summary>
        /// <param name="separator">
        /// The string placed between consecutive elements in the result.  This
        /// parameter may be null.
        /// </param>
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
        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
            return GenericOps<IClientData>.EnumerableToString(
                this, ToStringFlags.None, separator, pattern, noCase);
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
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(Characters.SpaceString, pattern, noCase);
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
    }
}
