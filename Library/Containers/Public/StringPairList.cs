/*
 * StringPairList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
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
    [ObjectId("fa3c3c95-bcc7-4c71-ab7f-94e534f9aad2")]
    public sealed class StringPairList : List<IPair<string>>,
            IStringList, IGetValue
    {
        #region Private Constants
        private static readonly string DefaultSeparator =
            Characters.Space.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly bool DefaultEmpty = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringPairList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IEnumerable<IPair<string>> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            params IPair<string>[] pairs
            )
            : base(pairs)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            string[] array,
            int startIndex
            )
        {
            Add(array, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            params string[] strings
            )
        {
            Add(strings);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IEnumerable<string> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IEnumerable<StringBuilder> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IDictionary<string, string> dictionary
            )
        {
            Add(dictionary);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue Members
        //
        // NOTE: This must call ToString to provide a "flattened" value
        //       because this is a mutable class.
        //
        public object Value
        {
            get { return ToString(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            get
            {
                string stringValue = ToString();

                return (stringValue != null) ?
                    stringValue.Length : _Constants.Length.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new StringPairList(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStringList Members
        #region Properties
        private string separator;
        public string Separator
        {
            get { return separator; }
            set { separator = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE
        private string cacheKey;
        public string CacheKey
        {
            get { return cacheKey; }
            set { cacheKey = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Get Methods
        public string GetItem(
            int index
            )
        {
            return this[index].ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public IPair<string> GetPair(
            int index
            )
        {
            return this[index];
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Insert / Add Methods
        public void Insert(
            int index,
            string item
            )
        {
            if (item != null)
                this.Insert(index, new StringPair(item));
            else
                this.Insert(index, (IPair<string>)null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string item
            )
        {
            if (item != null)
                this.Add(new StringPair(item));
            else
                this.Add((IPair<string>)null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string key,
            string value
            )
        {
            this.Add(new StringPair(key, value));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string key,
            string value,
            bool normalize,
            bool ellipsis
            )
        {
            string localValue = value;

            if (normalize)
            {
                localValue = StringOps.NormalizeWhiteSpace(
                    localValue, Characters.Space,
                    WhiteSpaceFlags.FormattedUse);
            }

            if (ellipsis)
                localValue = FormatOps.Ellipsis(localValue);

            this.Add(new StringPair(key, localValue));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringBuilder item
            )
        {
            this.Add((item != null) ? item.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string[] array,
            int startIndex
            )
        {
            for (int index = startIndex; index < array.Length; index++)
                Add(array[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                object item = list[index];

                if (item != null)
                    Add(item.ToString());
                else
                    Add((string)null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IStringList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                IPair<string> item = list.GetPair(index);

                if (item != null)
                    Add(item);
                else
                    Add((IPair<string>)null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<StringBuilder> collection
            )
        {
            foreach (StringBuilder item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IDictionary<string, string> dictionary
            )
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
                Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Argument> collection
            )
        {
            foreach (Argument item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Result> collection
            )
        {
            foreach (Result item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<IPair<string>> collection
            )
        {
            foreach (IPair<string> item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<Argument> collection
            )
        {
            foreach (Argument item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<Result> collection
            )
        {
            foreach (Result item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method adds a null item if the final item currently in
        //       the list is not null -OR- the list is empty.  It returns true
        //       if an item was actually added.
        //
        public bool MaybeAddNull()
        {
            int count = base.Count;

            if (count == 0)
            {
                base.Add(null);
                return true;
            }

            IPair<string> item = base[count - 1];

            if (item == null)
                return false;

            base.Add(null);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MaybeFillWithNull(
            int count
            )
        {
            while (base.Count < count)
                base.Add(null);

            return (base.Count == count);
        }

        ///////////////////////////////////////////////////////////////////////

        public int MaybeAddRange(
            IEnumerable<string> collection
            )
        {
            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (string item in collection)
            {
                Add(item);
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public int MaybeAddRange(
            IEnumerable<IPair<string>> collection
            )
        {
            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (IPair<string> item in collection)
            {
                base.Add(item);
                result++;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            bool empty
            )
        {
            return ToString(null, empty, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            string separator = Separator; /* PROPERTY */

            if (separator == null)
                separator = DefaultSeparator;

            return ToString(separator, pattern, empty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
            return ToString(separator, pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string separator,
            string pattern,
            bool empty,
            bool noCase
            )
        {
            if (empty)
            {
                return ParserOps<IPair<string>>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
            else
            {
                StringPairList result = new StringPairList();

                foreach (IPair<string> element in this)
                {
                    if (element == null)
                        continue;

                    if (String.IsNullOrEmpty(element.X) &&
                        String.IsNullOrEmpty(element.Y))
                    {
                        continue;
                    }

                    result.Add(element);
                }

                return ParserOps<IPair<string>>.ListToString(
                    result, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString()
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (IPair<string> element in this)
            {
                if (element != null)
                {
                    result.Append(element.X);
                    result.Append(element.Y);
                }
                else
                {
                    result.Append((string)null);
                    result.Append((string)null);
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString(
            string separator
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (IPair<string> element in this)
            {
                if (result.Length > 0)
                    result.Append(separator);

                if (element != null)
                {
                    result.Append(element.X);
                    result.Append(element.Y);
                }
                else
                {
                    result.Append((string)null);
                    result.Append((string)null);
                }
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToList Methods
        public IStringList ToList()
        {
            return new StringList(this);
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList(
            string pattern,
            bool noCase
            )
        {
            return ToList(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            StringPairList inputList;
            StringPairList outputList = new StringPairList();

            if (empty)
            {
                inputList = this;
            }
            else
            {
                inputList = new StringPairList();

                foreach (IPair<string> element in this)
                {
                    if (element == null)
                        continue;

                    if (String.IsNullOrEmpty(element.X) &&
                        String.IsNullOrEmpty(element.Y))
                    {
                        continue;
                    }

                    inputList.Add(element);
                }
            }

            ReturnCode code;
            Result error = null;

            code = GenericOps<IPair<string>>.FilterList(
                inputList, outputList, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);

            if (code != ReturnCode.Ok)
            {
                DebugOps.Complain(code, error);

                //
                // TODO: Return null in the error case here?
                //
                outputList = null;
            }

            return outputList;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Factory Methods
        public static StringPairList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringPairList FromString(
            string value,
            ref Result error
            )
        {
            //
            // TODO: *PERF* We cannot have this call to SplitList perform any
            //       caching because we do not know exactly what the resulting
            //       list will be used for.
            //
            StringList list1 = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, _Constants.Length.Invalid,
                    false, ref list1, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            StringPairList list2 = null;

            if (list1 != null)
            {
                list2 = new StringPairList();

                foreach (string element1 in list1)
                {
                    if (String.IsNullOrEmpty(element1))
                    {
                        list2.Add((IPair<string>)null);
                        continue;
                    }

                    StringList subList1 = null;

                    if (ParserOps<string>.SplitList(
                            null, element1, 0, _Constants.Length.Invalid,
                            false, ref subList1, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    int count = subList1.Count;

                    if (count == 0)
                        continue;

                    string localKey = subList1[0];

                    if (String.IsNullOrEmpty(localKey))
                        localKey = null;

                    string localValue = null;

                    if (count >= 2)
                    {
                        localValue = subList1[1];

                        if (String.IsNullOrEmpty(localValue))
                            localValue = null;
                    }

                    list2.Add(localKey, localValue);
                }
            }

            return list2;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(DefaultEmpty);
        }
        #endregion
    }
}
