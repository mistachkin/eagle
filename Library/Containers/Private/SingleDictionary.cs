/*
 * SingleDictionary.cs --
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

#if FAST_DICTIONARY
using Eagle._Containers.Public;
#endif

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<string, float>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<string, float>;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to
    /// single-precision floating-point values.  It extends the underlying
    /// generic dictionary without adding any further behavior.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("134668cd-45bc-4283-9c94-b51700abe4c3")]
    internal sealed class SingleDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public SingleDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public SingleDictionary(
            IDictionary<string, float> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// equality comparer when comparing keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        public SingleDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary and that uses the
        /// specified equality comparer when comparing keys.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        public SingleDictionary(
            IDictionary<string, float> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private SingleDictionary(
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
