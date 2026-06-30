/*
 * ProcedureWrapperDictionary.cs --
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

using ProcedureWrapper = Eagle._Wrappers.Procedure;

using ProcedurePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Procedure>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to instances
    /// of the procedure wrapper type.  It extends the generic wrapper
    /// dictionary with a helper for producing a filtered string list of its
    /// procedures.
    /// </summary>
    [ObjectId("abe58e55-3407-48a0-b09d-7b997f81cb37")]
    internal sealed class ProcedureWrapperDictionary :
            WrapperDictionary<string, ProcedureWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ProcedureWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public ProcedureWrapperDictionary(
            IDictionary<string, ProcedureWrapper> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of the procedures in this dictionary,
        /// optionally filtered by their flags and by a pattern, and optionally
        /// including the flags of each procedure in the resulting list.
        /// </summary>
        /// <param name="hasFlags">
        /// The procedure flags that a procedure must have in order to be
        /// included in the resulting list.  If this is
        /// <see cref="ProcedureFlags.None" />, no filtering based on required
        /// flags is performed.
        /// </param>
        /// <param name="notHasFlags">
        /// The procedure flags that a procedure must not have in order to be
        /// included in the resulting list.  If this is
        /// <see cref="ProcedureFlags.None" />, no filtering based on excluded
        /// flags is performed.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if a procedure must have all of the flags specified by
        /// <paramref name="hasFlags" />; otherwise, having any of those flags is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if a procedure must have all of the flags specified by
        /// <paramref name="notHasFlags" /> to be excluded; otherwise, having any
        /// of those flags causes it to be excluded.
        /// </param>
        /// <param name="pattern">
        /// The pattern that each procedure name must match in order to be
        /// included in the resulting list.  This parameter may be null, in which
        /// case all procedures are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero if each entry in the resulting list should include the flags
        /// of the procedure in addition to its name.
        /// </param>
        /// <param name="list">
        /// Upon success, this is the list that receives the matching procedures.
        /// If null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public ReturnCode ToList(
            ProcedureFlags hasFlags,
            ProcedureFlags notHasFlags,
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
            if ((hasFlags == ProcedureFlags.None) &&
                (notHasFlags == ProcedureFlags.None))
            {
                if (full)
                {
                    inputList = new StringList();

                    foreach (ProcedurePair pair in this)
                    {
                        IProcedure procedure = pair.Value;

                        if (procedure == null)
                            continue;

                        inputList.Add(StringList.MakeList(
                            procedure.Flags.ToString(),
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

                foreach (ProcedurePair pair in this)
                {
                    IProcedure procedure = pair.Value;

                    if (procedure == null)
                        continue;

                    ProcedureFlags flags = procedure.Flags;

                    if (((hasFlags == ProcedureFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == ProcedureFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        if (full)
                        {
                            inputList.Add(StringList.MakeList(
                                procedure.Flags.ToString(),
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
