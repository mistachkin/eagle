/*
 * TypeChangeTypeCallbackDictionary.cs --
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
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("e5b4fd82-6b1f-4a20-8074-45629b66c413")]
    internal sealed class TypeChangeTypeCallbackDictionary : Dictionary<Type, ChangeTypeCallback>
    {
        #region Public Constructors
        public TypeChangeTypeCallbackDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public TypeChangeTypeCallbackDictionary(
            IDictionary<Type, ChangeTypeCallback> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
