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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("272c4460-6219-4683-a719-e297146d5992")]
    public class UriDictionary<T> : Dictionary<Uri, T> where T : new()
    {
        #region Public Constructors
        public UriDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        public bool ContainsSchemeAndServer(
            Uri uri
            )
        {
            return Contains(
                uri, UriComponents.SchemeAndServer, UriFormat.Unescaped,
                SharedStringOps.SystemNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

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
