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

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("134668cd-45bc-4283-9c94-b51700abe4c3")]
    internal sealed class SingleDictionary : Dictionary<string, float>
    {
        #region Public Constructors
        public SingleDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
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
