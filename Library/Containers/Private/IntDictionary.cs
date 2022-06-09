/*
 * IntDictionary.cs --
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
using System.Globalization;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
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
    [ObjectId("d0f3e89e-8835-471a-aaff-81a0b97c49ef")]
    internal sealed class IntDictionary : Dictionary<string, int>
    {
        #region Public Constructors
        public IntDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            IDictionary<string, int> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            IEnumerable<string> collection,
            CultureInfo cultureInfo
            )
            : this()
        {
            Add(collection, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            IEnumerable<string> collection
            )
            : this()
        {
            AddKeys(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private IntDictionary(
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

        #region Public Static Methods
        public static string FastSerialize(
            IntDictionary dictionary,
            ref Result error
            )
        {
            if (dictionary == null)
            {
                error = "invalid dictionary value";
                return null;
            }

            int count = dictionary.Count;

            if (count == 0)
                return String.Empty;

            //
            // HACK: Using StringList in this method feels a bit too heavy;
            //       however, it is certainly easy.
            //
            StringList list = new StringList(count * 2);

            foreach (KeyValuePair<string, int> pair in dictionary)
            {
                list.Add(pair.Key);
                list.Add(pair.Value.ToString(CultureInfo.InvariantCulture));
            }

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public static IntDictionary FastDeserialize(
            string value,
            bool failOnError,
            ref Result error
            )
        {
            if (value == null)
            {
                error = "invalid string value";
                return null;
            }

            //
            // HACK: Using StringList in this method feels a bit too heavy;
            //       however, it is certainly easy.
            //
            StringList list = StringList.FromString(value, ref error);

            if (list == null)
                return null;

            int count = list.Count;

            if (count == 0)
                return new IntDictionary();

            IntDictionary dictionary = new IntDictionary(count / 2);

            for (int index = 0; index < count; index += 2)
            {
                int nextIndex = index + 1;

                if (nextIndex >= count)
                {
                    error = String.Format(
                        "integer element at index {0} missing", nextIndex);

                    if (failOnError)
                        return null;
                    else
                        break;
                }

                string stringValue = list[nextIndex];
                int intValue;

                if (!int.TryParse(stringValue,
                        NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out intValue))
                {
                    error = String.Format(
                        "list element {0} at index {1} is not an integer",
                        FormatOps.WrapOrNull(stringValue), nextIndex);

                    if (failOnError)
                        return null;
                    else
                        break;
                }

                string localKey = list[index];

                if (localKey == null)
                {
                    error = String.Format(
                        "list element {0} at index {1} is an invalid key",
                        FormatOps.WrapOrNull(localKey), index);

                    if (failOnError)
                        return null;
                    else
                        break;
                }

                dictionary.Add(localKey, intValue);
            }

            return dictionary;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Add(
            IEnumerable<string> collection,
            CultureInfo cultureInfo
            )
        {
            foreach (string item in collection)
            {
                //
                // NOTE: We require a list of lists to process into name/value
                //       pairs.  The name is always a string and the value must
                //       parse as a valid integer.
                //
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        null, item, 0, Length.Invalid, true,
                        ref list) == ReturnCode.Ok)
                {
                    //
                    // NOTE: We require at least a name and a value, extra
                    //       elements are silently ignored.
                    //
                    if (list.Count >= 2)
                    {
                        string key = list[0];

                        //
                        // NOTE: *WARNING* Empty array element names are
                        //       allowed, please do not change this to
                        //       "!String.IsNullOrEmpty".
                        //
                        if (key != null)
                        {
                            //
                            // NOTE: Attempt to parse the list element as a
                            //       valid integer; if not, it will be silently
                            //       ignored.
                            //
                            int value = 0;

                            if (Value.GetInteger2(list[1], ValueFlags.AnyInteger,
                                    cultureInfo, ref value) == ReturnCode.Ok)
                            {
                                if (this.ContainsKey(key))
                                    this[key] += value;
                                else
                                    this.Add(key, value);
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddKeys(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
            {
                if (item == null)
                    continue;

                int value;

                if (TryGetValue(item, out value))
                    value += 1;
                else
                    value = 1;

                this[item] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, int>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
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
