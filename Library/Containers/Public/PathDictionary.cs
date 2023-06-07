/*
 * PathDictionary.cs --
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
using System.IO;

#if SERIALIZATION
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0a2942ed-d174-4549-8cdf-8e8cf003d9b9")]
    public class PathDictionary<T> :
            Dictionary<string, T>, IDictionary<string, T> where T : new()
    {
        #region Private Data
        //
        // NOTE: This is the mapping of dictionary keys to their respective
        //       relative ordering in returned lists.
        //
        private IntDictionary ordering;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the value of the next index that will be added to
        //       the ordering dictionary.  The first list index is zero and
        //       there can be no gaps.  When a key is removed from the main
        //       dictionary, the index values above it are all adjusted down
        //       one and this index is also adjusted down one.
        //
        private int nextIndex;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public PathDictionary()
            : this(PathTranslationType.Default)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PathDictionary(
            PathDictionary<T> dictionary /* in */
            )
            : this(dictionary, dictionary.TranslationType)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PathDictionary(
            IEqualityComparer<string> comparer /* in */
            )
            : this(comparer, PathTranslationType.Default)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        protected PathDictionary(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
            : base(info, context)
        {
            this.translationType = PathTranslationType.Default;
            InitializeTheOrdering();
            PopulatePrivateData(info, context);
            MaybePopulateTheOrderingViaSelf();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
        internal PathDictionary(
            IEnumerable<string> collection /* in */
            )
            : this(PathTranslationType.Default)
        {
            InitializeTheOrdering();
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private PathDictionary(
            PathTranslationType translationType /* in */
            )
            : base(new _Comparers.StringCustom(PathOps.ComparisonType))
        {
            this.translationType = translationType;
            InitializeTheOrdering();
        }

        ///////////////////////////////////////////////////////////////////////

        private PathDictionary(
            PathDictionary<T> dictionary,       /* in */
            PathTranslationType translationType /* in */
            )
            : base(dictionary, new _Comparers.StringCustom(PathOps.ComparisonType))
        {
            this.translationType = translationType;
            InitializeTheOrdering();
            MaybePopulateTheOrderingViaOther(dictionary);
            MaybePopulateTheOrderingViaSelf();
        }

        ///////////////////////////////////////////////////////////////////////

        private PathDictionary(
            IEqualityComparer<string> comparer, /* in */
            PathTranslationType translationType /* in */
            )
            : base(comparer)
        {
            this.translationType = translationType;
            InitializeTheOrdering();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        internal static PathDictionary<T> ForAllDirectories(
            string path,
            bool recursive
            )
        {
            return ForDirectories(
                path, Characters.Asterisk.ToString(),
                FileOps.GetSearchOption(recursive));
        }

        ///////////////////////////////////////////////////////////////////////

        private static PathDictionary<T> ForDirectories(
            string path,
            string searchPattern,
            SearchOption searchOption
            )
        {
            string[] directories = Directory.GetDirectories(
                path, searchPattern, searchOption);

            if (directories == null)
                return null;

            Array.Sort(directories);

            return CreateFrom(directories);
        }

        ///////////////////////////////////////////////////////////////////////

        private static PathDictionary<T> CreateFrom(
            IEnumerable<string> paths
            )
        {
            if (paths == null)
                return null;

            PathDictionary<T> result = new PathDictionary<T>();

            foreach (string path in paths)
            {
                if (path == null)
                    continue;

                if (result.ContainsKey(path)) /* EXEMPT */
                    continue;

                result.Add(path, default(T));
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void InitializeTheOrdering()
        {
            ordering = new IntDictionary(this.Comparer);
            nextIndex = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private void ClearTheOrdering()
        {
            if (ordering != null)
                ordering.Clear();

            nextIndex = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybePopulateTheOrderingViaOther(
            PathDictionary<T> dictionary /* in */
            )
        {
            if ((ordering != null) && (dictionary != null))
            {
                IntDictionary dictionaryOrdering = dictionary.ordering;

                ordering = (dictionaryOrdering != null) ?
                    new IntDictionary(dictionaryOrdering) : null;

                nextIndex = dictionary.nextIndex;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybePopulateTheOrderingViaSelf()
        {
            int count = this.Count;

            if (count == 0)
                return;

            if ((ordering != null) && (ordering.Count != count))
            {
                TraceInternalError(
                    "MaybePopulateTheOrderingViaSelf: falling back...");

                ordering.Clear();
                nextIndex = 0;

                IEnumerator<KeyValuePair<string, T>> enumerator =
                    GetBaseEnumerator();

                if (enumerator != null)
                {
                    while (true)
                    {
                        if (!enumerator.MoveNext())
                            break;

                        ordering.Add(enumerator.Current.Key, nextIndex++);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool AddToTheOrdering(
            string key,       /* in */
            bool errorOnFound /* in */
            )
        {
            if ((ordering != null) && (key != null))
            {
                if (ordering.ContainsKey(key))
                {
                    if (errorOnFound)
                    {
                        TraceInternalError(String.Format(
                            "AddToOrdering: ordering was already " +
                            "present for {0}", FormatOps.WrapOrNull(
                            key)));
                    }

                    return false;
                }

                ordering.Add(key, nextIndex++);
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool RemoveFromTheOrdering(
            string key /* in */
            )
        {
            if ((ordering != null) && (key != null))
            {
                int oldIndex;

                if (ordering.TryGetValue(key, out oldIndex))
                {
                    IntDictionary localOrdering = new IntDictionary(ordering);

                    foreach (KeyValuePair<string, int> pair in localOrdering)
                    {
                        int localIndex = pair.Value;

                        if (localIndex >= oldIndex)
                            ordering[pair.Key]--;
                    }

                    nextIndex--;
                }
                else if (this.ContainsKey(key))
                {
                    TraceInternalError(String.Format(
                        "RemoveFromOrdering: ordering was not " +
                        "present for {0}", FormatOps.WrapOrNull(
                        key)));
                }

                return ordering.Remove(key);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        private void PopulatePrivateData(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
        {
            try
            {
                PathTranslationType localTranslationType;
                IntDictionary localOrdering;
                int localNextIndex;

                if (info != null)
                {
                    localTranslationType = (PathTranslationType)info.GetValue(
                        "translationType", typeof(PathTranslationType));

#if false
                    //
                    // BUGBUG: Why doesn't this actually work?  It appears to;
                    //         however, when it is deserialized in the other
                    //         AppDomain, the dictionary always ends up empty
                    //         (but not null).
                    //
                    localOrdering = info.GetValue(
                        "ordering", typeof(IntDictionary)) as IntDictionary;
#else
                    Result error = null;

                    localOrdering = IntDictionary.FastDeserialize(
                        info.GetString("ordering"), true, ref error);

                    if (localOrdering == null)
                        TraceInternalError(error);
#endif

                    localNextIndex = info.GetInt32("nextIndex");

                    if ((localOrdering == null) ||
                        (localOrdering.Count != localNextIndex))
                    {
                        TraceInternalError(String.Format(
                            "PopulatePrivateData: local ordering {0} is " +
                            "invalid or wrong {1}", FormatOps.WrapOrNull(
                            localOrdering), localNextIndex));

                        localOrdering = new IntDictionary();
                        localNextIndex = 0;
                    }
                }
                else
                {
                    localTranslationType = PathTranslationType.Default;
                    localOrdering = new IntDictionary();
                    localNextIndex = 0;
                }

                translationType = localTranslationType;
                ordering = localOrdering;
                nextIndex = localNextIndex;
            }
            catch (Exception e)
            {
                TraceInternalError(
                    String.Format("PopulatePrivateData: {0}", e));
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Collections.Generic.IDictionary<string, TValue> Overrides
        T IDictionary<string, T>.this[string key]
        {
            get { return base[PathOps.TranslatePath(key, translationType)]; }
            set
            {
                key = PathOps.TranslatePath(key, translationType);
                AddToTheOrdering(key, false);
                base[key] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        void IDictionary<string, T>.Add(
            string key, /* in */
            T value     /* in */
            )
        {
            key = PathOps.TranslatePath(key, translationType);
            AddToTheOrdering(key, true);
            base.Add(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<string, T>.ContainsKey(
            string key /* in */
            )
        {
            return base.ContainsKey(
                PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<string, T>.Remove(
            string key /* in */
            )
        {
            key = PathOps.TranslatePath(key, translationType);
            RemoveFromTheOrdering(key);
            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<string, T>.TryGetValue(
            string key, /* in */
            out T value /* in */
            )
        {
            return base.TryGetValue(
                PathOps.TranslatePath(key, translationType), out value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Collections.Generic.Dictionary<string, TValue> Overrides
        public new KeyCollection Keys
        {
            get { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public new ValueCollection Values
        {
            get { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public new T this[string key]
        {
            get { return base[PathOps.TranslatePath(key, translationType)]; }
            set
            {
                key = PathOps.TranslatePath(key, translationType);
                AddToTheOrdering(key, false);
                base[key] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Add(
            string key, /* in */
            T value     /* in */
            )
        {
            key = PathOps.TranslatePath(key, translationType);
            AddToTheOrdering(key, true);
            base.Add(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Clear()
        {
            ClearTheOrdering();
            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool ContainsKey(
            string key /* in */
            )
        {
            return base.ContainsKey(
                PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        public new Dictionary<string, T>.Enumerator GetEnumerator()
        {
            return (Dictionary<string, T>.Enumerator)GetBaseEnumerator();
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool Remove(
            string key /* in */
            )
        {
            key = PathOps.TranslatePath(key, translationType);
            RemoveFromTheOrdering(key);
            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool TryGetValue(
            string key, /* in */
            out T value /* in */
            )
        {
            return base.TryGetValue(
                PathOps.TranslatePath(key, translationType), out value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private PathTranslationType translationType;
        public PathTranslationType TranslationType
        {
            get { return translationType; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual StringList GetKeysInOrder(
            bool reverse /* in */
            )
        {
            return GetKeyOrKeysInOrder(Index.Invalid, reverse);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual IEnumerable<KeyValuePair<string, T>> GetPairsInOrder(
            bool reverse /* in */
            )
        {
            return GetPairOrPairsInOrder(Index.Invalid, reverse);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string GetNthKeyOrNull(
            int index,   /* in */
            bool reverse /* in */
            )
        {
            StringList list = GetKeyOrKeysInOrder(index, reverse);

            if (list == null)
                return null;

            if ((index < 0) || (index >= list.Count))
                return null;

            return list[index];
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Add(
            IEnumerable<string> collection /* in */
            )
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Add(
            string key /* in */
            )
        {
            this.Add(key, new T());
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Contains(
            string key /* in */
            )
        {
            return this.ContainsKey(key);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual T Add( /* NOT USED */
            string key,   /* in */
            T value,      /* in */
            bool reserved /* in */
            )
        {
            this.Add(key, value);
            return this[key];
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            return ToString(pattern, noCase, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            string pattern, /* in */
            bool noCase,    /* in */
            bool reverse    /* in */
            )
        {
            return ParserOps<string>.ListToString(
                GetKeysInOrder(reverse), Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), pattern,
                noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static void TraceInternalError(
            string message /* in */
            )
        {
            DebugOps.MaybeBreak(message);

            TraceOps.DebugTrace(
                message, typeof(PathDictionary<T>).Name,
                TracePriority.PathError);
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerator<KeyValuePair<string, T>> GetBaseEnumerator()
        {
            return base.GetEnumerator();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        protected virtual StringList GetKeyOrKeysInOrder(
            int index,   /* in */
            bool reverse /* in */
            )
        {
            if (ordering == null)
            {
                TraceInternalError(
                    "GetKeyOrKeysInOrder: missing ordering information");

                return null;
            }

            StringList list = new StringList(
                (index != Index.Invalid) ? 1 : this.Count);

            if (index == Index.Invalid)
                list.MaybeFillWithNull(ordering.Count);

            foreach (KeyValuePair<string, int> pair in ordering)
            {
                string key = pair.Key;
                int localIndex = pair.Value;

                if (index != Index.Invalid)
                {
                    if (localIndex != index)
                        continue;

                    list.Add(PathOps.TranslatePath(
                        key, translationType));

                    break;
                }
                else
                {
                    if ((localIndex < 0) || (localIndex >= list.Count))
                    {
                        TraceInternalError(String.Format(
                            "GetKeyOrKeysInOrder: key {0} index " +
                            "value {1} is out-of-bounds (0 to {2})",
                            FormatOps.WrapOrNull(key), localIndex,
                            list.Count - 1));

                        list = null;
                        break;
                    }

                    list[localIndex] = PathOps.TranslatePath(
                        key, translationType);
                }
            }

            if (reverse && (list != null))
                list.Reverse();

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual IEnumerable<KeyValuePair<string, T>> GetPairOrPairsInOrder(
            int index,   /* in */
            bool reverse /* in */
            )
        {
            if (ordering == null)
            {
                TraceInternalError(
                    "GetPairOrPairsInOrder: missing ordering information");

                return null;
            }

            List<KeyValuePair<string, T>> list =
                new List<KeyValuePair<string, T>>(
                    (index != Index.Invalid) ? 1 : this.Count);

            while (list.Count < ordering.Count)
                list.Add(new KeyValuePair<string, T>());

            foreach (KeyValuePair<string, int> pair in ordering)
            {
                string key = pair.Key;
                int localIndex = pair.Value;

                T value;

                if ((key == null) || !this.TryGetValue(key, out value))
                {
                    TraceInternalError(String.Format(
                        "GetPairOrPairsInOrder: value for key {0} is missing",
                        FormatOps.WrapOrNull(key)));

                    list = null;
                    break;
                }

                if (index != Index.Invalid)
                {
                    if (localIndex != index)
                        continue;

                    list.Add(new KeyValuePair<string, T>(
                        PathOps.TranslatePath(key, translationType), value));

                    break;
                }
                else
                {
                    if ((localIndex < 0) || (localIndex >= list.Count))
                    {
                        TraceInternalError(String.Format(
                            "GetPairOrPairsInOrder: key {0} index " +
                            "value {1} is out-of-bounds (0 to {2})",
                            FormatOps.WrapOrNull(key), localIndex,
                            list.Count - 1));

                        list = null;
                        break;
                    }

                    list[localIndex] = new KeyValuePair<string, T>(
                        PathOps.TranslatePath(key, translationType), value);
                }
            }

            if (reverse && (list != null))
                list.Reverse(); /* O(N) */

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal virtual void Add(
            IEnumerable<string> collection, /* in */
            bool merge                      /* in */
            )
        {
            foreach (string item in collection)
                if (!merge || !this.ContainsKey(item))
                    this.Add(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
        {
            info.AddValue("translationType",
                translationType, typeof(PathTranslationType));

#if false
            //
            // BUGBUG: Why doesn't this actually work?  It appears to;
            //         however, when it is deserialized in the other
            //         AppDomain, the dictionary always ends up empty
            //         (but not null).
            //
            info.AddValue("ordering", ordering, typeof(IntDictionary));
#else
            string value;
            Result error = null;

            value = IntDictionary.FastSerialize(ordering, ref error);

            info.AddValue("ordering", value);

            if (value == null)
                TraceInternalError(error);
#endif

            info.AddValue("nextIndex", nextIndex);

            base.GetObjectData(info, context);
        }
#endif
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
