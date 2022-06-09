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

        #region Static "Factory" Methods
        public static ObjectDictionary FromObject(
            object value,
            bool addOnly,
            ref Result error
            )
        {
            string stringValue;

            if (value is string)
                stringValue = (string)value;
            else
                stringValue = StringOps.GetStringFromObject(value);

            StringDictionary dictionary1 = StringDictionary.FromString(
                stringValue, addOnly, ref error);

            if (dictionary1 == null)
                return null;

            ObjectDictionary dictionary2 = new ObjectDictionary();

            foreach (KeyValuePair<string, string> pair in dictionary1)
                dictionary2[pair.Key] = pair.Value;

            return dictionary2;
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
            IDictionary<string, object> dictionary
            )
        {
            foreach (KeyValuePair<string, object> pair in dictionary)
                this.Add(pair.Key, pair.Value);
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
                Characters.Space.ToString(), null, false);
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
                Characters.Space.ToString(), pattern, noCase);
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
                Characters.Space.ToString(), pattern, regExOptions);
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
                Characters.Space.ToString(), null, false);
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
                Characters.Space.ToString(), pattern, noCase);
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
                Characters.Space.ToString(), pattern, regExOptions);
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
                Characters.Space.ToString(), null, false);
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
