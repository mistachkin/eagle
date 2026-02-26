/*
 * ObjectDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2327d197-2cd8-440e-babe-1c9bd85a3cd4")]
    public sealed class ObjectDictionary : Dictionary<string, object>
    {
        #region Public Constructors
        public ObjectDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary(
            IDictionary<string, object> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary(
            IDictionary<string, object> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary(
            IEnumerable<object> collection
            )
            : this()
        {
            foreach (object item in collection)
                this.Add((this.Count + 1).ToString(), item);
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary(
            IDictionary dictionary
            )
            : this()
        {
            foreach (DictionaryEntry entry in dictionary)
                this.Add(entry.Key.ToString(), entry.Value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ObjectDictionary PrivateFromString(
            string value,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            StringDictionary dictionary1 = StringDictionary.FromString(
                value, addOnly, keysOnly, ref error);

            if (dictionary1 == null)
                return null;

            ObjectDictionary dictionary2 = new ObjectDictionary();

            foreach (KeyValuePair<string, string> pair in dictionary1)
                dictionary2[pair.Key] = pair.Value;

            return dictionary2;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ObjectDictionary FromObject(
            object value,
            bool addOnly,
            ref Result error
            )
        {
            return FromObject(value, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectDictionary FromObject(
            object value,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            string stringValue;

            if (value is string)
                stringValue = (string)value;
            else
                stringValue = StringOps.GetStringFromObject(value);

            return PrivateFromString(
                stringValue, addOnly, keysOnly, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectDictionary FromString(
            string value,
            bool addOnly,
            ref Result error
            )
        {
            return FromString(value, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectDictionary FromString(
            string value,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            return PrivateFromString(value, addOnly, keysOnly, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private ObjectDictionary(
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

        #region Public Methods
        public void Add(
            IDictionary<string, object> dictionary /* in */
            )
        {
            foreach (KeyValuePair<string, object> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryTraverse(
            StringList keys,  /* in */
            ref object value, /* out */
            ref Result error  /* out */
            )
        {
            if (keys == null)
            {
                error = "invalid dictionary key list";
                return false;
            }

            ObjectDictionary dictionary = this;
            int count = keys.Count;

            if (count == 0)
            {
                value = dictionary;
                return true;
            }

            string localKey;
            object localValue;

            for (int index = 0; index < count - 1; index++)
            {
                localKey = keys[index];

                if (localKey == null)
                    continue;

                if (!dictionary.TryGetValue(localKey, out localValue))
                {
                    error = String.Format(
                        "cannot find work dictionary key {0}",
                        FormatOps.DisplayTraverseList(
                            keys.GetRange(0, index + 1)));

                    return false;
                }

                dictionary = localValue as ObjectDictionary;

                if (dictionary == null)
                {
                    error = String.Format(
                        "cannot traverse dictionary key {0}, wrong type {1}",
                        FormatOps.DisplayTraverseList(
                            keys.GetRange(0, index + 1)),
                        MarshalOps.GetErrorTypeName(localValue));

                    return false;
                }
            }

            localKey = keys[count - 1];

            if (localKey == null)
            {
                value = dictionary;
                return true;
            }

            if (!dictionary.TryGetValue(localKey, out localValue))
            {
                error = String.Format(
                    "cannot find final dictionary key {0}",
                    FormatOps.DisplayTraverseList(
                        keys.GetRange(0, count)));

                return false;
            }

            value = localValue;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string KeysToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, false, mode, pattern, null, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string separator
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, false, true, mode, null, pattern, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
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
                Characters.SpaceString, pattern, noCase);
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
