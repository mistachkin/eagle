/*
 * PackageIndexDictionary.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;

using PackageIndexAnyPair = Eagle._Components.Public.MutableAnyPair<
    string, Eagle._Components.Public.PackageIndexFlags>;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps a package index path to its
    /// associated name and package index flags, keyed by path.  It extends the
    /// path dictionary with a type name suitable for use within Eagle.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("362da258-da0d-4a28-837c-ee22ffc29cc8")]
    internal sealed class PackageIndexDictionary :
            PathDictionary<PackageIndexAnyPair>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PackageIndexDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new dictionary.
        /// </param>
        public PackageIndexDictionary(
            PackageIndexDictionary dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for this dictionary.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized stream associated with
        /// this dictionary.
        /// </param>
        private PackageIndexDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion
    }
}
