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

using OperatorWrapper = Eagle._Wrappers.Operator;

using OperatorPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Operator>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary of expression operator wrappers, keyed
    /// by name.  It extends the generic wrapper dictionary with a type name
    /// suitable for use within Eagle and the ability to produce a filtered list
    /// of its contents.
    /// </summary>
    [ObjectId("9d65ae2d-b85f-42df-a860-6357d2b09246")]
    internal sealed class OperatorWrapperDictionary :
            WrapperDictionary<string, OperatorWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public OperatorWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a list of the operators in this dictionary, optionally
        /// filtered by their flags and by a name pattern, and appends the
        /// matching entries to the supplied list.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags that an operator must have in order to be included, or
        /// <see cref="OperatorFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that an operator must not have in order to be included, or
        /// <see cref="OperatorFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if an operator must have all of the flags specified via
        /// <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if an operator must lack all of the flags specified via
        /// <paramref name="notHasFlags" />; otherwise, lacking any of them is
        /// sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the entries, or null to include all of
        /// them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the lexeme, operands, and flags of each operator
        /// in addition to its name.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the matching entries.  If this is null, a new
        /// list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// operation could not be completed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ToList(
            OperatorFlags hasFlags,
            OperatorFlags notHasFlags,
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
            if ((hasFlags == OperatorFlags.None) &&
                (notHasFlags == OperatorFlags.None))
            {
                if (full)
                {
                    inputList = new StringList();

                    foreach (OperatorPair pair in this)
                    {
                        IOperator @operator = pair.Value;

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
                    inputList = new StringList(this.Keys);
                }
            }
            else
            {
                inputList = new StringList();

                foreach (OperatorPair pair in this)
                {
                    IOperator @operator = pair.Value;

                    if (@operator == null)
                        continue;

                    OperatorFlags flags = @operator.Flags;

                    if (((hasFlags == OperatorFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == OperatorFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        if (full)
                        {
                            inputList.Add(StringList.MakeList(
                                @operator.Lexeme.ToString(),
                                @operator.Operands.ToString(),
                                @operator.Flags.ToString(),
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
