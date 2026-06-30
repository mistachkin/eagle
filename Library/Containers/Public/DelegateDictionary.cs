/*
 * DelegateDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Interfaces.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, System.Delegate>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, System.Delegate>;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps a string name to an
    /// associated <see cref="Delegate" /> instance.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("90375f51-488b-4fa6-9aed-510f70eefac4")]
    public sealed class DelegateDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public DelegateDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, adding the name and value of
        /// each specified name/value pair to the dictionary.
        /// </summary>
        /// <param name="pairs">
        /// The array of name/value pairs to add to the dictionary.  The name
        /// is taken from the X component and the associated
        /// <see cref="Delegate" /> is taken from the Y component.
        /// </param>
        public DelegateDictionary(
            params IPair<object>[] pairs
            )
            : this(pairs as IEnumerable<IPair<object>>)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, adding the name and value of
        /// each name/value pair in the specified collection to the dictionary.
        /// </summary>
        /// <param name="collection">
        /// The collection of name/value pairs to add to the dictionary.  The
        /// name is taken from the X component and the associated
        /// <see cref="Delegate" /> is taken from the Y component.
        /// </param>
        public DelegateDictionary(
            IEnumerable<IPair<object>> collection
            )
        {
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class, copying the name/value pairs
        /// from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose name/value pairs are copied into the new
        /// dictionary.
        /// </param>
        private DelegateDictionary(
            IDictionary<string, Delegate> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// equality comparer for the dictionary names.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use when comparing names, or null to use
        /// the default equality comparer.
        /// </param>
        private DelegateDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, copying the name/value pairs
        /// from the specified dictionary and using the specified equality
        /// comparer for the dictionary names.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose name/value pairs are copied into the new
        /// dictionary.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use when comparing names, or null to use
        /// the default equality comparer.
        /// </param>
        private DelegateDictionary(
            IDictionary<string, Delegate> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class using previously serialized
        /// data.  This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the data needed to deserialize this instance.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source and destination of
        /// the serialized stream.
        /// </param>
        private DelegateDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Adds the name and value of each name/value pair in the specified
        /// collection to the dictionary.
        /// </summary>
        /// <param name="collection">
        /// The collection of name/value pairs to add to the dictionary.  The
        /// name is taken from the X component and the associated
        /// <see cref="Delegate" /> is taken from the Y component.
        /// </param>
        private void Add(
            IEnumerable<IPair<object>> collection
            )
        {
            foreach (IPair<object> item in collection)
                this.Add(item.X.ToString(), (Delegate)item.Y);
        }
        #endregion
    }
}
