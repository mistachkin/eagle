/*
 * ChannelDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("62bcf37b-a971-4dc1-a59c-0f6adc16278f")]
    internal sealed class ChannelDictionary : Dictionary<string, IChannel>
    {
        public ChannelDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(string pattern, bool noCase)
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ToList(
            string pattern,
            bool noCase,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList = new StringList(this.Keys);

            if (list == null)
                list = new StringList();

            return GenericOps<string>.FilterList(
                inputList, list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);
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

