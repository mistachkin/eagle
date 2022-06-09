/*
 * DoubleDictionary.cs --
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
    [ObjectId("89e7e6d4-4366-472a-8d8e-f62acc65229e")]
    internal sealed class DoubleDictionary : Dictionary<string, double>
    {
        public DoubleDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public DoubleDictionary(
            IDictionary<string, double> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public DoubleDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public DoubleDictionary(
            IDictionary<string, double> dictionary,
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
        private DoubleDictionary(
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
