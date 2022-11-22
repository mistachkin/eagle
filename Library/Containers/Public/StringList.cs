/*
 * StringList.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;

#if LIST_CACHE
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("17df73b1-f419-498d-aeca-4be513e25310")]
    public sealed class StringList : List<string>, IList<string>,
            ICollection<string>, IStringList, IGetValue
#if LIST_CACHE
            , IReadOnly
#endif
    {
        #region Private Static Data
#if CACHE_STRINGLIST_TOSTRING && CACHE_STATISTICS
        private static long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf];
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        private static readonly string DefaultSeparator =
            Characters.Space.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly bool DefaultEmpty = true;

        public static readonly string EmptyElement =
            Characters.OpenBrace.ToString() + Characters.CloseBrace.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if CACHE_STRINGLIST_TOSTRING
        private string @string; /* CACHE */
#endif

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE
        private bool isReadOnly;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            object value
            )
        {
            AddObjectOrObjects(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable collection
            )
        {
            AddObjects(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable collection,
            bool @null
            )
        {
            AddObjects(collection, @null);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable<char> collection
            )
        {
            AddChars(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable<string> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable<StringBuilder> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable<IPair<string>> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable<Argument> collection
            )
        {
            AddObjects(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IEnumerable<Result> collection
            )
        {
            AddObjects(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IList list,
            int startIndex
            )
        {
            Add(list, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            IList<string> list,
            int startIndex
            )
        {
            Add(list, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            string[] array,
            int startIndex
            )
        {
            Add(array, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            params string[] strings
            )
            : base(strings)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            StringTransformCallback callback,
            IEnumerable<string> collection
            )
        {
            Add(callback, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            StringTransformCallback callback,
            IEnumerable<Argument> collection
            )
        {
            Add(callback, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringList(
            StringTransformCallback callback,
            IEnumerable<Result> collection
            )
        {
            Add(callback, collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
        internal StringList( /* NOTE: For use by PrivateShellMainCore only. */
            IList<string> list,
            int startIndex,
            bool noTrim
            )
        {
            Add(list, startIndex, noTrim);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Static Methods
        #region Factory Methods
#if LIST_CACHE
        internal static StringList MaybeReadOnly(
            bool readOnly
            )
        {
            StringList list = new StringList();

            list.isReadOnly = readOnly;

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static StringList MaybeReadOnly(
            IEnumerable<string> collection,
            bool readOnly
            )
        {
            StringList list = new StringList(collection);

            list.isReadOnly = readOnly;

            return list;
        }
#endif
        #endregion
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

        #region IReadOnly Members
#if LIST_CACHE
        public bool IsReadOnly
        {
            get { return isReadOnly; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new StringList(this);
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
            return this[index];
        }

        ///////////////////////////////////////////////////////////////////////

        public IPair<string> GetPair(
            int index
            )
        {
            return new StringPair(this[index]);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Insert / Add Methods
        public void Add(
            string key,
            string value
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(key);
            base.Add(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string key,
            string value,
            bool normalize,
            bool ellipsis
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(key);

            string localValue = value;

            if (normalize)
            {
                localValue = StringOps.NormalizeWhiteSpace(
                    localValue, Characters.Space,
                    WhiteSpaceFlags.FormattedUse);
            }

            if (ellipsis)
                localValue = FormatOps.Ellipsis(localValue);

            base.Add(localValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringBuilder item
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add((item != null) ? item.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string[] array,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < array.Length; index++)
                base.Add(array[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IList list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < list.Count; index++)
                base.Add(StringOps.GetStringFromObject(list[index]));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IStringList list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < list.Count; index++)
                base.Add(list.GetItem(index));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IList<string> list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < list.Count; index++)
                base.Add(list[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<string> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<StringBuilder> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IDictionary<string, string> dictionary
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (KeyValuePair<string, string> pair in dictionary)
                Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Argument> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Result> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<IPair<string>> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (IPair<string> item in collection)
                Add(item.X, item.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<string> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (string item in collection)
                base.Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<Argument> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (Argument item in collection)
                base.Add(callback(StringOps.GetStringFromObject(item)));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<Result> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (Result item in collection)
                base.Add(callback(StringOps.GetStringFromObject(item)));
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method adds a null item if the final item currently in
        //       the list is not null -OR- the list is empty.  It returns true
        //       if an item was actually added.
        //
        public bool MaybeAddNull()
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            int count = base.Count;

            if (count == 0)
            {
                base.Add(null);
                return true;
            }

            string item = base[count - 1];

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
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            while (base.Count < count)
                base.Add(null);

            return (base.Count == count);
        }

        ///////////////////////////////////////////////////////////////////////

        public int MaybeAddRange(
            IEnumerable<string> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (string item in collection)
            {
                base.Add(item);
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public int MaybeAddRange(
            IEnumerable<IPair<string>> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (IPair<string> item in collection)
            {
                base.Add(
                    (item != null) ? item.ToString() : null);

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
#if CACHE_STRINGLIST_TOSTRING
            bool canUseCachedString = CanUseCachedString(
                separator, pattern, empty, noCase);

            if (canUseCachedString && (@string != null))
            {
#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Hit]);
#endif

                return @string;
            }

#if CACHE_STATISTICS
            Interlocked.Increment(
                ref cacheCounts[(int)CacheCountType.Miss]);
#endif
#endif

            if (empty)
            {
                string result = ParserOps<string>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);

#if CACHE_STRINGLIST_TOSTRING
                if (canUseCachedString)
                    @string = result;
#endif

                return result;
            }
            else
            {
                StringList result = new StringList();

                foreach (string element in this)
                {
                    if (String.IsNullOrEmpty(element))
                        continue;

                    result.Add(element);
                }

                return ParserOps<string>.ListToString(
                    result, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString()
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (string element in this)
                result.Append(element);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString(
            string separator
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (string element in this)
            {
                if (result.Length > 0)
                    result.Append(separator);

                result.Append(element);
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToList Methods
        public IStringList ToList()
        {
            return new StringList(this); /* NOTE: Gee, that was easy. */
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
            StringList inputList;
            StringList outputList = new StringList();

            if (empty)
            {
                inputList = this;
            }
            else
            {
                inputList = new StringList();

                foreach (string element in this)
                {
                    if (String.IsNullOrEmpty(element))
                        continue;

                    inputList.Add(element);
                }
            }

            ReturnCode code;
            Result error = null;

            code = GenericOps<string>.FilterList(
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

        #region Public Methods
        #region Add Methods
        public void Add(
            string item1,
            string item2,
            params string[] strings
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            Add(item1, item2);
            Add(strings);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddChars(
            IEnumerable<char> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (char item in collection)
                base.Add(item.ToString());
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddObjects(
            IEnumerable collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddObjects(
            IEnumerable collection,
            bool @null
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, @null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddObjectOrObjects(
            object value
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (value != null)
            {
                IEnumerable enumerable = value as IEnumerable;

                if (enumerable != null)
                    AddObjects(enumerable, true);
                else
                    AddObjects(new object[] { value }, true);
            }
            else
            {
                base.Add(null);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Range Methods
        public void InsertRange(
            int index,
            IList list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.InsertRange(index, new StringList(list, startIndex));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Search Methods
        public bool Contains(
            string item,
            StringComparison comparisonType
            )
        {
            for (int index = 0; index < base.Count; index++)
            {
                if (SharedStringOps.Equals(
                        base[index], item, comparisonType))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public int IndexOf(
            string value,
            int startIndex,
            StringComparison comparisonType
            )
        {
            for (int index = startIndex; index < base.Count; index++)
            {
                if (SharedStringOps.Equals(
                        base[index], value, comparisonType))
                {
                    return index;
                }
            }

            return Index.Invalid;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        internal void MakeUnique()
        {
            IEnumerable<string> collection = CopyUnique();

#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Clear();

            if (collection != null)
                base.AddRange(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        #region Copy Methods
        private IEnumerable<string> CopyUnique()
        {
            StringDictionary dictionary = new StringDictionary();

            foreach (string element in this)
            {
                if (element == null) continue;
                dictionary[element] = null;
            }

            return new StringList(dictionary.Keys);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Methods
        private void MaybeTrimAndAdd(
            string item,
            bool noTrim
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (!noTrim && (item != null))
                item = item.Trim();

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        private void Add(
            IGetValue getValue
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (getValue != null)
            {
                if (AddRange(getValue.Value as IEnumerable, true))
                    return;

                base.Add(getValue.String);
                return;
            }

            base.Add(null);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool AddRange(
            IEnumerable collection,
            bool @null
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (collection == null)
                return false;

            IEnumerable<string> localCollection =
                collection as IEnumerable<string>;

            //
            // HACK: This is an optimization and there MAY be null
            //       values hiding in the collection; therefore,
            //       only use the optimization if null values are
            //       requested.
            //
            if (@null && (localCollection != null))
            {
                base.AddRange(localCollection);
                return true;
            }

            foreach (object item in collection)
            {
                if (item != null)
                    base.Add(StringOps.GetStringFromObject(item));
                else if (@null)
                    base.Add(null);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private void Add(
            IList<string> list,
            int startIndex,
            bool noTrim
            )
        {
            for (int index = startIndex; index < list.Count; index++)
                MaybeTrimAndAdd(list[index], noTrim);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Argument Handling Methods
        public static StringList NullIfEmpty(
            StringList list,
            int firstIndex
            )
        {
            if (list == null)
                return null;

            if (firstIndex == Index.Invalid)
                firstIndex = 0;

            if ((firstIndex < 0) || (firstIndex >= list.Count))
                return null;

            //
            // NOTE: If there are elements beyond the first index or the
            //       element at the first index is not empty, then return
            //       the range starting from the first index; otherwise,
            //       return null.
            //
            if (((firstIndex + 1) < list.Count) ||
                !String.IsNullOrEmpty(list[firstIndex]))
            {
                return GetRange(list, firstIndex);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Range Methods
        public static StringList GetRange(
            IList list,
            int firstIndex
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetRange(
            IList list,
            int firstIndex,
            bool nullIfEmpty
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid,
                nullIfEmpty);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetRange(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            return GetRange(list, firstIndex, lastIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetRange(
            IList list,
            int firstIndex,
            int lastIndex,
            bool nullIfEmpty
            )
        {
            if (list == null)
                return null;

            StringList range = null;

            if (firstIndex == Index.Invalid)
                firstIndex = 0;

            if (lastIndex == Index.Invalid)
                lastIndex = list.Count - 1;

            if ((!nullIfEmpty ||
                    ((list.Count > 0) && ((lastIndex - firstIndex) > 0))))
            {
                range = new StringList();

                for (int index = firstIndex; index <= lastIndex; index++)
                    range.Add(StringOps.GetStringFromObject(list[index]));
            }

            return range;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public static string MakeList(
            params string[] strings
            )
        {
            return MakeList((IEnumerable<string>)strings);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeList(
            IEnumerable<string> collection
            )
        {
            return new StringList(collection).ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeListWithoutNulls(
            params string[] strings
            )
        {
            return MakeListWithoutNulls((IEnumerable<string>)strings);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeListWithoutNulls(
            IEnumerable<string> collection
            )
        {
            return new StringList(collection, false).ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeList(
            params object[] objects
            )
        {
            return MakeList((IEnumerable)objects);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeList(
            IEnumerable collection
            )
        {
            return new StringList(collection).ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeListWithoutNulls(
            params object[] objects
            )
        {
            return MakeListWithoutNulls((IEnumerable)objects);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeListWithoutNulls(
            IEnumerable collection
            )
        {
            return new StringList(collection, false).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        public static StringList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList FromString(
            string value,
            ref Result error
            )
        {
            //
            // TODO: *PERF* We cannot have this call to SplitList perform any
            //       caching because we do not know exactly what the resulting
            //       list will be used for.
            //
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, _Constants.Length.Invalid,
                    false, ref list, ref error) != ReturnCode.Ok)
            {
                list = null;
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList MaybeCreate(
            IEnumerable<string> collection
            )
        {
            if (collection == null)
                return null;

            if (collection is StringList)
                return (StringList)collection;

            return new StringList(collection);
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

        ///////////////////////////////////////////////////////////////////////

        #region Cached String Helper Methods
#if CACHE_STRINGLIST_TOSTRING
        private void InvalidateCachedString(
            bool children /* NOT USED */
            )
        {
            @string = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CanUseCachedString(
            string separator,
            string pattern,
            bool empty,
            bool noCase
            )
        {
            if (!Parser.IsListSeparator(separator))
                return false;

            if (pattern != null)
                return false;

            if (!empty)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        public static bool HaveCacheCounts()
        {
            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string CacheCountsToString(bool empty)
        {
            return FormatOps.CacheCounts(cacheCounts, empty);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Explicit ICollection<string> Overrides
        void ICollection<string>.Add(
            string item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        void ICollection<string>.Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        bool ICollection<string>.Remove(
            string item
            )
        {
            InvalidateCachedString(false);

            return base.Remove(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<string> Overrides
        public new void Add(
            string item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool Remove(
            string item
            )
        {
            InvalidateCachedString(false);

            return base.Remove(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IList<string> Overrides
        void IList<string>.Insert(
            int index,
            string item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        void IList<string>.RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        string IList<string>.this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IList<string> Overrides
        public new void Insert(
            int index,
            string item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new string this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region List<string> Overrides
        public new void AddRange(
            IEnumerable<string> collection
            )
        {
            InvalidateCachedString(false);

            base.AddRange(collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void InsertRange(
            int index,
            IEnumerable<string> collection
            )
        {
            InvalidateCachedString(false);

            base.InsertRange(index, collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new int RemoveAll(
            Predicate<string> match
            )
        {
            InvalidateCachedString(false);

            return base.RemoveAll(match); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void RemoveRange(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.RemoveRange(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Reverse()
        {
            InvalidateCachedString(false);

            base.Reverse(); /* O(N) */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Reverse(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.Reverse(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort()
        {
            InvalidateCachedString(false);

            base.Sort();
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort(
            Comparison<string> comparison
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparison); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort(
            IComparer<string> comparer
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparer); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort(
            int index,
            int count,
            IComparer<string> comparer)
        {
            InvalidateCachedString(false);

            base.Sort(index, count, comparer); /* throw */
        }
        #endregion
#endif
        #endregion
    }
}
