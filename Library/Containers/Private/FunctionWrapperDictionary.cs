/*
 * FunctionWrapperDictionary.cs --
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
    [ObjectId("17f0e175-db07-43d8-a9ca-a7bab22700dd")]
    internal sealed class FunctionWrapperDictionary :
            WrapperDictionary<string, _Wrappers.Function>
    {
        public FunctionWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ToList(
            FunctionFlags hasFlags,
            FunctionFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            bool full,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList;

            //
            // NOTE: If no flags were supplied, we do not bother filtering on
            //       them.
            //
            if ((hasFlags == FunctionFlags.None) &&
                (notHasFlags == FunctionFlags.None))
            {
                if (full)
                {
                    inputList = new StringList();

                    foreach (KeyValuePair<string, _Wrappers.Function> pair in this)
                    {
                        IFunction function = pair.Value;

                        if (function == null)
                            continue;

                        inputList.Add(StringList.MakeList(
                            function.Arguments.ToString(),
                            function.Flags.ToString(),
                            pair.Key));
                    }
                }
                else
                {
                    inputList = new StringList(this.Keys);
                }
            }
            else
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, _Wrappers.Function> pair in this)
                {
                    IFunction function = pair.Value;

                    if (function == null)
                        continue;

                    FunctionFlags flags = function.Flags;

                    if (((hasFlags == FunctionFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == FunctionFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        if (full)
                        {
                            inputList.Add(StringList.MakeList(
                                function.Arguments.ToString(),
                                function.Flags.ToString(),
                                pair.Key));
                        }
                        else
                        {
                            inputList.Add(pair.Key);
                        }
                    }
                }
            }

            if (list == null)
                list = new StringList();

            return GenericOps<string>.FilterList(
                inputList, list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);
        }
    }
}
