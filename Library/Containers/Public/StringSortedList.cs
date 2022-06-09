/*
 * StringSortedList.cs --
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

using System.Collections;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c574dd78-aa93-4496-9269-30a7f9f3652b")]
    public sealed class StringSortedList : SortedList<string, string>
    {
        public StringSortedList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IDictionary<string, string> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IDictionary<string, string> dictionary,
            IComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IEnumerable<char> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IEnumerable<KeyValuePair<string, ISubCommand>> collection
            )
            : this()
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IEnumerable<string> collection
            )
            : this()
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IEnumerable<Argument> collection
            )
            : this()
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringSortedList(
            IList list,
            int startIndex
            )
            : this()
        {
            Add(list, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<char> collection
            )
        {
            foreach (char item in collection)
                this.Add(item.ToString(), null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<string> collection,
            bool strict
            )
        {
            foreach (string item in collection)
            {
                if (!strict && (item == null))
                    continue;

                this.Add(item, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<KeyValuePair<string, ISubCommand>> collection,
            bool strict
            )
        {
            foreach (KeyValuePair<string, ISubCommand> item in collection)
            {
                if (!strict && (item.Key == null))
                    continue;

                this.Add(item.Key, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Argument> collection,
            bool strict
            )
        {
            foreach (Argument item in collection)
            {
                if (!strict && (item == null))
                    continue;

                this.Add(item, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                if (list[index] == null)
                    continue;

                this.Add(list[index].ToString(), null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringSortedList GetRange(
            IList list,
            int firstIndex
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringSortedList GetRange(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            StringSortedList range = null;

            if (list != null)
            {
                range = new StringSortedList();

                for (int index = firstIndex; index <= lastIndex; index++)
                {
                    if (list[index] == null)
                        continue;

                    range.Add(list[index].ToString(), null);
                }
            }

            return range;
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<string>.ListToString(
                this.Keys, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
