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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("a84e03f3-7935-4976-8dd9-d8ad33153fd9")]
    internal sealed class ClientDataList : LinkedList<IClientData>
    {
        public ClientDataList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
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

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
