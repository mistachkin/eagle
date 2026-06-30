/*
 * UriDictionary.cs --
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
using Eagle._Components.Private;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps URIs (<see cref="Uri" />)
    /// to values of the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the values stored in this dictionary.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("272c4460-6219-4683-a719-e297146d5992")]
    public class UriDictionary<T> :
#if FAST_DICTIONARY
            FastDictionary<Uri, T>
#else
            Dictionary<Uri, T>
#endif
            where T : new()
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty dictionary of URIs.
        /// </summary>
        public UriDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary of URIs that maps each URI in the specified
        /// collection to the supplied value.
        /// </summary>
        /// <param name="collection">
        /// The collection of URIs to add as keys to the new dictionary.
        /// </param>
        /// <param name="value">
        /// The value to associate with each URI key.
        /// </param>
        public UriDictionary(
            IEnumerable<Uri> collection,
            T value
            )
        {
            foreach (Uri item in collection)
                base.Add(item, value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a dictionary of URIs from previously serialized data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context describing the source and destination of the
        /// serialized data.
        /// </param>
        protected UriDictionary(
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

        #region Public Methods
        /// <summary>
        /// This method determines whether this dictionary contains a key URI
        /// that matches the specified URI when comparing only the scheme and
        /// server components, ignoring case.
        /// </summary>
        /// <param name="uri">
        /// The URI to search for.
        /// </param>
        /// <returns>
        /// True if a matching key URI is found; otherwise, false.
        /// </returns>
        public bool ContainsSchemeAndServer(
            Uri uri
            )
        {
            return Contains(
                uri, UriComponents.SchemeAndServer, UriFormat.Unescaped,
                SharedStringOps.SystemNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this dictionary contains a key URI
        /// that matches the specified URI when comparing the indicated
        /// components using the supplied format and comparison rules.
        /// </summary>
        /// <param name="uri">
        /// The URI to search for.
        /// </param>
        /// <param name="partsToCompare">
        /// The URI components to compare.
        /// </param>
        /// <param name="compareFormat">
        /// The format to use when comparing the URI components.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when comparing the URI
        /// components.
        /// </param>
        /// <returns>
        /// True if a matching key URI is found; otherwise, false.
        /// </returns>
        public bool Contains(
            Uri uri,
            UriComponents partsToCompare,
            UriFormat compareFormat,
            StringComparison comparisonType
            )
        {
            foreach (Uri item in this.Keys)
            {
                if (Uri.Compare(item, uri, partsToCompare,
                        compareFormat, comparisonType) == 0)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
