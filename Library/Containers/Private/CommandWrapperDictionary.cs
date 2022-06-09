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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("d9cd17c1-34e2-4e73-a96a-18365eaa9186")]
    internal sealed class CommandWrapperDictionary : WrapperDictionary<string, _Wrappers.Command>
    {
        public CommandWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CommandWrapperDictionary(
            IDictionary<string, _Wrappers.Command> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode ToList(
            CommandFlags hasFlags,
            CommandFlags notHasFlags,
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
            // NOTE: If no flags were supplied, we do not bother filtering on them.
            //
            if ((hasFlags == CommandFlags.None) && (notHasFlags == CommandFlags.None))
            {
                inputList = new StringList(this.Keys);
            }
            else
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, _Wrappers.Command> pair in this)
                {
                    ICommand command = pair.Value;

                    if (command == null)
                        continue;

                    CommandFlags flags = command.Flags;

                    if (((hasFlags == CommandFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == CommandFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        inputList.Add(pair.Key);
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
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
