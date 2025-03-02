/*
 * StringPairDictionary.cs --
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
    [ObjectId("a22cdd6d-d3b5-4336-a4f0-c54cd618004f")]
    internal sealed class StringPairDictionary :
        Dictionary<string, IPair<string>>
    {
        #region Public Constructors
        public StringPairDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairDictionary(
            IEnumerable<string> collection
            )
            : base()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairDictionary(
            IDictionary<string, string> dictionary
            )
            : base()
        {
            Add(dictionary);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairDictionary(
            IDictionary<string, IPair<string>> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Add(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
            {
                if (item == null)
                    continue;

                Add(item, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IDictionary<string, string> dictionary
            )
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
                Add(pair.Key, new StringPair(pair.Value));
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairDictionary Filter(
            string pattern,
            bool noCase
            )
        {
            StringPairDictionary dictionary = new StringPairDictionary();

            foreach (KeyValuePair<string, IPair<string>> pair in this)
            {
                if ((pattern == null) || Parser.StringMatch(
                        null, pair.Key, 0, pattern, 0, noCase))
                {
                    dictionary.Add(pair.Key, pair.Value);
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString,
                pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
