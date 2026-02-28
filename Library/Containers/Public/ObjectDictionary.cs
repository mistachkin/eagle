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
using StringPair = System.Collections.Generic.KeyValuePair<string, string>;
using ObjectPair = System.Collections.Generic.KeyValuePair<string, object>;

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
        #region Private Data
        //
        // NOTE: When this field is non-zero, the overridden ToString method
        //       will include all the keys and values, not just the keys.
        //
        private readonly bool viaScript = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

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

        #region Private Constructors
        internal ObjectDictionary(
            bool viaScript /* in */
            )
            : this()
        {
            this.viaScript = viaScript;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ObjectDictionary PrivateFromString(
            string value,
            bool viaScript,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            StringDictionary dictionary1 = StringDictionary.FromString(
                value, addOnly, keysOnly, ref error);

            if (dictionary1 == null)
                return null;

            ObjectDictionary dictionary2 = new ObjectDictionary(viaScript);

            foreach (StringPair pair in dictionary1)
                dictionary2[pair.Key] = pair.Value;

            return dictionary2;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ObjectDictionary FromObject(
            object value,
            bool viaScript,
            bool addOnly,
            ref Result error
            )
        {
            return FromObject(value, viaScript, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectDictionary FromObject(
            object value,
            bool viaScript,
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
                stringValue, viaScript, addOnly, keysOnly, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectDictionary FromString(
            string value,
            bool viaScript,
            bool addOnly,
            ref Result error
            )
        {
            return FromString(
                value, viaScript, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectDictionary FromString(
            string value,
            bool viaScript,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            return PrivateFromString(
                value, viaScript, addOnly, keysOnly, ref error);
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
            foreach (ObjectPair pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CanTraverse(
            IEnumerable keys, /* in */
            bool viaScript    /* in */
            )
        {
            object value = null;
            Result error = null;

            return TryTraverse(keys, viaScript, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public long TraverseAndCount(
            Interpreter interpreter,
            string pattern,
            bool noCase
            )
        {
            long count = 0;

            foreach (ObjectPair pair in this)
            {
                if ((pattern != null) && !Parser.StringMatch(
                        interpreter, pair.Key, 0, pattern, 0,
                        noCase))
                {
                    continue;
                }

                ObjectDictionary dictionary = pair.Value as ObjectDictionary;

                if (dictionary == null)
                {
                    count++; // NOTE: Another (matching) key, normal value.
                    continue;
                }

                count += dictionary.TraverseAndCount(
                    interpreter, pattern, noCase); /* RECURSIVE */
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary TraverseAndCreate(
            IEnumerable keys,        /* in */
            int startIndex,          /* in */
            int stopIndex,           /* in */
            bool viaScript,          /* in */
            ref int changeCount,     /* in, out */
            ref bool stopOnNotFound, /* in, out */
            ref Result error         /* out */
            )
        {
            if (keys == null)
            {
                error = "invalid dictionary key collection";
                return null;
            }

            StringList localKeys = new StringList(keys);
            int count = localKeys.Count;

            if (count == 0)
            {
                error = "empty dictionary key collection";
                return null;
            }

            if (startIndex >= 0)
            {
                if (startIndex >= count)
                {
                    error = String.Format(
                        "start index {0} must be less than count {1}",
                        startIndex, count);

                    return null;
                }
            }
            else
            {
                error = String.Format(
                    "start index {0} cannot be less than zero",
                    startIndex);

                return null;
            }

            if (stopIndex >= 0)
            {
                if (stopIndex >= count)
                {
                    error = String.Format(
                        "stop index {0} must be less than count {1}",
                        stopIndex, count);

                    return null;
                }
            }
            else
            {
                stopIndex = count - 1;
            }

            if (startIndex > stopIndex)
            {
                error = String.Format(
                    "start index {0} must be less than stop index {1}",
                    startIndex, stopIndex);

                return null;
            }

            ObjectDictionary dictionary = this;

            for (int index = startIndex; index <= stopIndex; index++)
            {
                string localKey = localKeys[index];

                if (localKey == null)
                {
                    error = String.Format(
                        "invalid dictionary key #{0}", index + 1);

                    return null;
                }

                object localValue;
                ObjectDictionary localDictionary;

                if (dictionary.TryGetValue(localKey, out localValue))
                {
                    localDictionary = localValue as ObjectDictionary;

                    if (localDictionary != null)
                    {
                        dictionary = localDictionary;
                    }
                    else
                    {
                        localDictionary = ObjectDictionary.FromString(
                            StringOps.GetStringFromObject(localValue),
                            viaScript, false, ref error);

                        if (localDictionary == null)
                            return null;

                        dictionary[localKey] = localDictionary;
                        dictionary = localDictionary;

                        changeCount++;
                    }
                }
                else if (stopOnNotFound)
                {
                    //
                    // HACK: This is not an error, per se; therefore,
                    //       do not set an error message.  When the
                    //       caller sets the "stopOnNotFound" flag to
                    //       true, they are also expected to check it
                    //       upon return.  When it is false, they can
                    //       simply skip any subsequent (dictionary)
                    //       processing and return success.
                    //
                    stopOnNotFound = false;
                    return null;
                }
                else
                {
                    localDictionary = new ObjectDictionary(true);

                    dictionary[localKey] = localDictionary;
                    dictionary = localDictionary;

                    changeCount++;
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryTraverse(
            IEnumerable keys, /* in */
            bool viaScript,   /* in */
            ref object value, /* out */
            ref Result error  /* out */
            )
        {
            if (keys == null)
            {
                error = "invalid dictionary key collection";
                return false;
            }

            ObjectDictionary dictionary = this;
            StringList localKeys = new StringList(keys);
            int count = localKeys.Count;

            if (count == 0)
            {
                value = dictionary;
                return true;
            }

            string localKey;
            object localValue;

            for (int index = 0; index < count - 1; index++)
            {
                localKey = localKeys[index];

                if (localKey == null)
                    continue;

                if (!dictionary.TryGetValue(localKey, out localValue))
                {
                    error = String.Format(
                        "cannot find work dictionary key {0}",
                        FormatOps.DisplayTraverseList(
                            localKeys.GetRange(0, index + 1)));

                    return false;
                }

                ObjectDictionary savedDictionary = dictionary;

                dictionary = localValue as ObjectDictionary;

                if (dictionary != null)
                    continue;

                dictionary = ObjectDictionary.FromString(
                    StringOps.GetStringFromObject(localValue),
                    viaScript, false, ref error);

                if (dictionary == null)
                    return false;

                savedDictionary[localKey] = dictionary;
            }

            localKey = localKeys[count - 1];

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
                        localKeys.GetRange(0, count)));

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
            return viaScript ?
                KeysAndValuesToString(null, false) :
                ToString(null, false);
        }
        #endregion
    }
}
