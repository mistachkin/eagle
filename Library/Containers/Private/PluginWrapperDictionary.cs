/*
 * PluginWrapperDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("bc93cd1f-ed24-4078-85a3-c8f658a605f9")]
    internal sealed class PluginWrapperDictionary :
            WrapperDictionary<string, _Wrappers.Plugin>
    {
        #region Public Constructors
        public PluginWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PluginWrapperDictionary(
            IDictionary<string, _Wrappers.Plugin> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private PluginWrapperDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private StringList ToList()
        {
            StringList list = new StringList();

            foreach (KeyValuePair<string, _Wrappers.Plugin> pair in this)
            {
                IPlugin plugin = pair.Value;

                if (plugin == null)
                    continue;

                list.Add(StringList.MakeList(plugin.FileName, plugin.Name));
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public ReturnCode ToList(
            PluginFlags hasFlags,
            PluginFlags notHasFlags,
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
            // NOTE: If no flags were supplied, do not bother filtering on
            //       them.
            //
            if ((hasFlags == PluginFlags.None) &&
                (notHasFlags == PluginFlags.None))
            {
                inputList = ToList();
            }
            else
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, _Wrappers.Plugin> pair in this)
                {
                    IPlugin plugin = pair.Value;

                    if (plugin == null)
                        continue;

                    PluginFlags flags = plugin.Flags;

                    if (((hasFlags == PluginFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == PluginFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        inputList.Add(StringList.MakeList(
                            plugin.FileName, plugin.Name));
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

        public override string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = ToList();

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion
    }
}
