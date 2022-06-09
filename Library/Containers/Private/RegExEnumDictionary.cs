/*
 * RegExEnumDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("17d57884-7b6c-479d-9519-4b6b81ba9562")]
    internal sealed class RegExEnumDictionary : Dictionary<Regex, Enum>
    {
        public RegExEnumDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public RegExEnumDictionary(IEnumerable<Regex> keys, Type enumType, IEnumerable<Enum> values)
            : this()
        {
            Add(keys, enumType, values);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Add(IEnumerable<Regex> keys, Type enumType, IEnumerable<Enum> values)
        {
            Result error = null;

            return Add(keys, enumType, values, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Add(IEnumerable<Regex> keys, Type enumType, IEnumerable<Enum> values, ref Result error)
        {
            object zeroValue = EnumOps.TryGet(enumType, 0, ref error);

            if (zeroValue != null)
            {
                IEnumerator<Enum> enumerator = values.GetEnumerator(); /* throw */
                bool moveNext = true;

                foreach (Regex key in keys)
                {
                    //
                    // NOTE: If we run out of values before keys, zero fill the
                    //       rest.
                    //
                    object value = zeroValue;

                    //
                    // NOTE: Are we able to continue moving through the items?
                    //
                    if (moveNext)
                    {
                        //
                        // NOTE: Move to the next item.  If this fails, there are
                        //       no more items and we cannot move any farther.
                        //
                        if (!enumerator.MoveNext())
                            moveNext = false;

                        //
                        // NOTE: Get the value of the current item.
                        //
                        value = enumerator.Current;
                    }

                    //
                    // NOTE: Add this key/value pair to the dictionary.
                    //
                    this.Add(key, (Enum)value); /* throw */
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(string pattern, bool noCase)
        {
            RegExList list = new RegExList(this.Keys);

            return ParserOps<Regex>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), pattern, noCase);
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
