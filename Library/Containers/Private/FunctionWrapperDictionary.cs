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

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using FunctionWrapper = Eagle._Wrappers.Function;

using FunctionPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Function>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to function
    /// wrapper values.  It extends the underlying wrapper dictionary with a
    /// helper for producing a filtered list of its functions, optionally
    /// selected by their flags.
    /// </summary>
    [ObjectId("17f0e175-db07-43d8-a9ca-a7bab22700dd")]
    internal sealed class FunctionWrapperDictionary :
            WrapperDictionary<string, FunctionWrapper>
    {
        /// <summary>
        /// Constructs an empty function wrapper dictionary.
        /// </summary>
        public FunctionWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the functions of the dictionary that match the
        /// specified flag and name criteria to the specified list.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags that a function must have in order to be included.  Use
        /// <see cref="FunctionFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a function must not have in order to be included.  Use
        /// <see cref="FunctionFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if a function must have all of the flags specified by
        /// <paramref name="hasFlags" />; zero if having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if a function must lack all of the flags specified by
        /// <paramref name="notHasFlags" />; zero if lacking any of them is
        /// sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which functions are included.  This
        /// parameter may be null to include all functions.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the arguments and flags of each function along
        /// with its name; zero to include only the name of each function.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the matching functions.  When null, a new list
        /// is created; otherwise, the matching functions are appended to the
        /// existing list.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

                    foreach (FunctionPair pair in this)
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

                foreach (FunctionPair pair in this)
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
