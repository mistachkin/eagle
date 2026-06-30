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

using PluginWrapper = Eagle._Wrappers.Plugin;

using PluginPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Plugin>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary of plugin wrappers, keyed by name.
    /// It extends the generic wrapper dictionary with a type name suitable for
    /// use within Eagle and the ability to produce a filtered list of its
    /// contents.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("bc93cd1f-ed24-4078-85a3-c8f658a605f9")]
    internal sealed class PluginWrapperDictionary :
            WrapperDictionary<string, PluginWrapper>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PluginWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new dictionary.
        /// </param>
        public PluginWrapperDictionary(
            IDictionary<string, PluginWrapper> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for this dictionary.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized stream associated with
        /// this dictionary.
        /// </param>
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
        /// <summary>
        /// Builds a list that pairs the file name and name of each plugin in
        /// this dictionary.
        /// </summary>
        /// <returns>
        /// A list containing the file name and name of each non-null plugin in
        /// this dictionary.
        /// </returns>
        private StringList ToList()
        {
            StringList list = new StringList();

            foreach (PluginPair pair in this)
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
        /// <summary>
        /// Builds a list of the plugins in this dictionary, optionally filtered
        /// by their flags and by a name pattern, and appends the matching
        /// entries to the supplied list.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags that a plugin must have in order to be included, or
        /// <see cref="PluginFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a plugin must not have in order to be included, or
        /// <see cref="PluginFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if a plugin must have all of the flags specified via
        /// <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if a plugin must lack all of the flags specified via
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
        /// Non-zero to include the file name of each plugin in addition to its
        /// name.
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
            PluginFlags hasFlags,
            PluginFlags notHasFlags,
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

                foreach (PluginPair pair in this)
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

        /// <summary>
        /// Converts the plugins in this dictionary to a string in the Eagle
        /// list format, optionally including only those entries matching the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the entries, or null to include all of
        /// them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string, in the Eagle list format, that represents the matching
        /// plugins in this dictionary.
        /// </returns>
        public override string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = ToList();

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion
    }
}
