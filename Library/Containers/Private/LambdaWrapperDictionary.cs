/*
 * LambdaWrapperDictionary.cs --
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
using Eagle._Interfaces.Private;

using LambdaWrapper = Eagle._Wrappers.Lambda;

using LambdaPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Lambda>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps lambda names to their
    /// <see cref="LambdaWrapper" /> instances.  It adds support for producing a
    /// filtered list of lambda names based on their procedure flags and an
    /// optional name pattern.
    /// </summary>
    [ObjectId("b1b72fd7-6519-43e4-81ec-e989965a3a18")]
    internal sealed class LambdaWrapperDictionary :
            WrapperDictionary<string, LambdaWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public LambdaWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of the lambda names in this dictionary,
        /// optionally filtered by the procedure flags of each lambda and by a
        /// name pattern.
        /// </summary>
        /// <param name="hasFlags">
        /// The procedure flags that a lambda must have in order to be included.
        /// If this is <see cref="ProcedureFlags.None" />, this filter is not
        /// applied.
        /// </param>
        /// <param name="notHasFlags">
        /// The procedure flags that a lambda must not have in order to be
        /// included.  If this is <see cref="ProcedureFlags.None" />, this filter
        /// is not applied.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if a lambda must have all of the flags specified by
        /// <paramref name="hasFlags" />; zero if having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if a lambda must have all of the flags specified by
        /// <paramref name="notHasFlags" /> to be excluded; zero if having any
        /// of them is sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the lambda names that are included in the
        /// result.  This parameter may be null, in which case all names are
        /// included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the list of matching lambda names.  If this is
        /// null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ToList(
            ProcedureFlags hasFlags,
            ProcedureFlags notHasFlags,
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
            if ((hasFlags == ProcedureFlags.None) &&
                (notHasFlags == ProcedureFlags.None))
            {
                inputList = new StringList(this.Keys);
            }
            else
            {
                inputList = new StringList();

                foreach (LambdaPair pair in this)
                {
                    ILambda lambda = pair.Value;

                    if (lambda == null)
                        continue;

                    ProcedureFlags flags = lambda.Flags;

                    if (((hasFlags == ProcedureFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == ProcedureFlags.None) ||
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
    }
}
