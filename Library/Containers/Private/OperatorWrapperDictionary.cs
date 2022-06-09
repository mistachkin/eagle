/*
 * OperatorWrapperDictionary.cs --
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
    [ObjectId("9d65ae2d-b85f-42df-a860-6357d2b09246")]
    internal sealed class OperatorWrapperDictionary :
            WrapperDictionary<string, _Wrappers.Operator>
    {
        public OperatorWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ToList(
            OperatorFlags hasFlags,
            OperatorFlags notHasFlags,
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
            if ((hasFlags == OperatorFlags.None) &&
                (notHasFlags == OperatorFlags.None))
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, _Wrappers.Operator> pair in this)
                {
                    _Wrappers.Operator @operator = pair.Value;

                    if (@operator == null)
                        continue;

                    inputList.Add(StringList.MakeList(
                        @operator.Lexeme.ToString(),
                        @operator.Operands.ToString(),
                        @operator.Flags.ToString(),
                        pair.Key));
                }
            }
            else
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, _Wrappers.Operator> pair in this)
                {
                    _Wrappers.Operator @operator = pair.Value;

                    if (@operator == null)
                        continue;

                    OperatorFlags flags = @operator.Flags;

                    if (((hasFlags == OperatorFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == OperatorFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        inputList.Add(StringList.MakeList(
                            @operator.Lexeme.ToString(),
                            @operator.Operands.ToString(),
                            @operator.Flags.ToString(),
                            pair.Key));
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
