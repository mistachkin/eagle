/*
 * CommandWrapperDictionary.cs --
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
using Alias = Eagle._Commands.Alias;

using CommandWrapper = Eagle._Wrappers.Command;

using CommandPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Command>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps command names to the command
    /// wrapper objects that manage them.  It extends the generic wrapper
    /// dictionary with support for producing a filtered list of command names,
    /// optionally restricted by command flags.
    /// </summary>
    [ObjectId("d9cd17c1-34e2-4e73-a96a-18365eaa9186")]
    internal sealed class CommandWrapperDictionary : WrapperDictionary<string, CommandWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public CommandWrapperDictionary()
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
        public CommandWrapperDictionary(
            IDictionary<string, CommandWrapper> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of the command names contained in this
        /// dictionary, optionally filtered by command flags and by a name
        /// pattern.  System aliases and null commands are always excluded.
        /// </summary>
        /// <param name="hasFlags">
        /// The command flags that an entry must have in order to be included.
        /// If this is <see cref="CommandFlags.None" />, no filtering is performed
        /// based on required flags.
        /// </param>
        /// <param name="notHasFlags">
        /// The command flags that an entry must not have in order to be included.
        /// If this is <see cref="CommandFlags.None" />, no filtering is performed
        /// based on prohibited flags.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if an entry must have all of the flags specified by
        /// <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if an entry must have all of the flags specified by
        /// <paramref name="notHasFlags" /> in order to be excluded; otherwise,
        /// having any of them is sufficient to exclude it.
        /// </param>
        /// <param name="pattern">
        /// The pattern that each command name must match in order to be included
        /// in the result.  This parameter may be null, in which case all names
        /// are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero if each resulting element should include the command flags
        /// in addition to its name.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of matching command names.  If this
        /// is null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode ToList(
            CommandFlags hasFlags,
            CommandFlags notHasFlags,
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
            // NOTE: If no flags were supplied, we do not bother filtering on them.
            //
            if ((hasFlags == CommandFlags.None) && (notHasFlags == CommandFlags.None))
            {
                if (full)
                {
                    inputList = new StringList();

                    foreach (CommandPair pair in this)
                    {
                        ICommand command = pair.Value;

                        if (command == null)
                            continue;

                        if (Alias.IsSystemAlias(command))
                            continue;

                        inputList.Add(StringList.MakeList(
                            command.Flags.ToString(),
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

                foreach (CommandPair pair in this)
                {
                    ICommand command = pair.Value;

                    if (command == null)
                        continue;

                    if (Alias.IsSystemAlias(command))
                        continue;

                    CommandFlags flags = command.Flags;

                    if (((hasFlags == CommandFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == CommandFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        if (full)
                        {
                            inputList.Add(StringList.MakeList(
                                command.Flags.ToString(),
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

            return GenericOps<string>.FilterList(inputList, list, Index.Invalid,
                Index.Invalid, ToStringFlags.None, pattern, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts this dictionary to a string in the Eagle list format.
        /// </summary>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
