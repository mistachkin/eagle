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

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using AliasWrapper = Eagle._Wrappers.Alias;

using AliasPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Alias>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to alias
    /// wrapper objects.  It extends the wrapper dictionary base class with
    /// helpers for producing a filtered list of its keys.
    /// </summary>
    [ObjectId("4e42c6a9-dd44-4f10-b668-5cdbe71a1266")]
    internal sealed class AliasWrapperDictionary :
            WrapperDictionary<string, AliasWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public AliasWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of the dictionary keys, optionally
        /// filtered by the alias flags of their associated values and by a
        /// name pattern.
        /// </summary>
        /// <param name="hasFlags">
        /// The alias flags that an entry must have in order to be included.
        /// If this is <see cref="AliasFlags.None" />, no filtering based on
        /// required flags is performed.
        /// </param>
        /// <param name="notHasFlags">
        /// The alias flags that an entry must not have in order to be
        /// included.  If this is <see cref="AliasFlags.None" />, no filtering
        /// based on excluded flags is performed.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if an entry must have all of the flags specified by
        /// <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if an entry must have all of the flags specified by
        /// <paramref name="notHasFlags" /> in order to be excluded; otherwise,
        /// having any of them is sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included.  This
        /// parameter may be null, in which case all keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of matching keys.  If this is null,
        /// a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

                foreach (AliasPair pair in this)
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
        /// <summary>
        /// This method produces a string containing all of the keys of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The keys of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
