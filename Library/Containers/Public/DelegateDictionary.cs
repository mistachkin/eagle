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

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("90375f51-488b-4fa6-9aed-510f70eefac4")]
    public sealed class DelegateDictionary : Dictionary<string, Delegate>
    {
        #region Public Constructors
        public DelegateDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public DelegateDictionary(
            params IPair<object>[] pairs
            )
            : this(pairs as IEnumerable<IPair<object>>)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        private DelegateDictionary(
            IDictionary<string, Delegate> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        private DelegateDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
