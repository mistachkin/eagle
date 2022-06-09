/*
 * AliasWrapperDictionary.cs --
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
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("4e42c6a9-dd44-4f10-b668-5cdbe71a1266")]
    internal sealed class AliasWrapperDictionary :
            WrapperDictionary<string, _Wrappers.Alias>
    {
        public AliasWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ToList(
            AliasFlags hasFlags,
            AliasFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList;

            //
            // NOTE: If no flags were supplied, we do not bother filtering on
            //       them.
            //
            if ((hasFlags == AliasFlags.None) &&
                (notHasFlags == AliasFlags.None))
            {
                inputList = new StringList(this.Keys);
            }
            else
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, _Wrappers.Alias> pair in this)
                {
                    IAlias alias = pair.Value;

                    if (alias == null)
                        continue;

                    AliasFlags flags = alias.AliasFlags;

                    if (((hasFlags == AliasFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == AliasFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        inputList.Add(pair.Key);
                    }
                }
            }

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
