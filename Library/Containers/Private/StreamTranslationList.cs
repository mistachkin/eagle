/*
 * StreamTranslationList.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("8298f31d-d97f-4555-b300-a2a1dacc2806")]
    internal sealed class StreamTranslationList : List<StreamTranslation>, ICloneable
    {
        public StreamTranslationList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StreamTranslationList(
            StreamTranslation inTranslation,
            StreamTranslation outTranslation
            )
            : base(new StreamTranslation[] { inTranslation, outTranslation })
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StreamTranslationList(IEnumerable<StreamTranslation> collection)
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<StreamTranslation>.ListToString(this, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new StreamTranslationList(this);
        }
        #endregion
    }
}

