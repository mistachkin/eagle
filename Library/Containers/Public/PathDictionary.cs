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
    /// <summary>
    /// This class represents a dictionary that maps file system paths (as
    /// strings) to associated values.  Keys are normalized using a configurable
    /// path translation type and compared using the platform-appropriate path
    /// comparison rules.  In addition, the dictionary tracks the relative
    /// insertion order of its keys so that ordered lists of keys or key/value
    /// pairs can be produced.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the values stored in this dictionary.  This type must have a
    /// public, parameterless constructor.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0a2942ed-d174-4549-8cdf-8e8cf003d9b9")]
    public class PathDictionary<T> :
#if FAST_DICTIONARY
            FastDictionary<string, T>,
#else
            Dictionary<string, T>,
#endif
            IDictionary<string, T> where T : new()
    {
        #region Private Data
        //
        // NOTE: This is the mapping of dictionary keys to their respective
        //       relative ordering in returned lists.
        //
        /// <summary>
        /// The mapping of dictionary keys to their relative ordering (i.e.
        /// their position in the lists returned by the ordering-aware methods).
        /// </summary>
        private IntDictionary ordering;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the value of the next index that will be added to
        //       the ordering dictionary.  The first list index is zero and
        //       there can be no gaps.  When a key is removed from the main
        //       dictionary, the index values above it are all adjusted down
        //       one and this index is also adjusted down one.
        //
        /// <summary>
        /// The value of the next index that will be assigned within the
        /// ordering mapping.  The first index is zero and there can be no gaps.
        /// </summary>
        private int nextIndex;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class that uses the default
        /// path translation type.
        /// </summary>
        public PathDictionary()
            : this(PathTranslationType.Default)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements (and
        /// ordering) copied from the specified dictionary, using its path
        /// translation type.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements and ordering are copied into the new
        /// dictionary.
        /// </param>
        public PathDictionary(
            PathDictionary<T> dictionary /* in */
            )
            : this(dictionary, dictionary.TranslationType)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// equality comparer for its keys and the default path translation
        /// type.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use the
        /// default comparer for the key type.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class from previously serialized
        /// data.  This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for this dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context, describing the source and destination of the
        /// serialized data.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class, using the default path
        /// translation type, that contains the keys from the specified
        /// collection (each associated with a newly created value).
        /// </summary>
        /// <param name="collection">
        /// The collection of path keys used to populate the new dictionary.
        /// </param>
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
        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// path translation type and the platform-appropriate path comparer.
        /// </summary>
        /// <param name="translationType">
        /// The path translation type used to normalize keys.
        /// </param>
        private PathDictionary(
            PathTranslationType translationType /* in */
            )
            : base(new _Comparers.StringCustom(PathOps.ComparisonType))
        {
            this.translationType = translationType;
            InitializeTheOrdering();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements (and
        /// ordering) copied from the specified dictionary, using the specified
        /// path translation type and the platform-appropriate path comparer.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements and ordering are copied into the new
        /// dictionary.
        /// </param>
        /// <param name="translationType">
        /// The path translation type used to normalize keys.
        /// </param>
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

        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// equality comparer for its keys and the specified path translation
        /// type.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        /// <param name="translationType">
        /// The path translation type used to normalize keys.
        /// </param>
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
        /// <summary>
        /// Creates a new dictionary containing all directories found beneath the
        /// specified path, optionally searching recursively.
        /// </summary>
        /// <param name="path">
        /// The root path whose directories are enumerated.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to search all subdirectories recursively; otherwise, only
        /// the immediate subdirectories are included.
        /// </param>
        /// <returns>
        /// The new dictionary, or null if the directories could not be
        /// enumerated.
        /// </returns>
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

        /// <summary>
        /// Creates a new dictionary containing the directories found beneath the
        /// specified path that match the specified search pattern, using the
        /// specified search option.
        /// </summary>
        /// <param name="path">
        /// The root path whose directories are enumerated.
        /// </param>
        /// <param name="searchPattern">
        /// The search pattern used to match directory names.
        /// </param>
        /// <param name="searchOption">
        /// The search option that specifies whether to search only the current
        /// directory or all subdirectories.
        /// </param>
        /// <returns>
        /// The new dictionary, or null if the directories could not be
        /// enumerated.
        /// </returns>
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

            Array.Sort(directories); /* O(N) */

            return CreateFrom(directories);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new dictionary whose keys are the specified paths, each
        /// associated with a default value.  Null and duplicate paths are
        /// skipped.
        /// </summary>
        /// <param name="paths">
        /// The collection of paths used to populate the new dictionary.
        /// </param>
        /// <returns>
        /// The new dictionary, or null if the specified collection is null.
        /// </returns>
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
        /// <summary>
        /// Initializes the ordering mapping to an empty state, using the same
        /// comparer as this dictionary.
        /// </summary>
        private void InitializeTheOrdering()
        {
            ordering = new IntDictionary(this.Comparer);
            nextIndex = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the ordering mapping and resets the next index to zero.
        /// </summary>
        private void ClearTheOrdering()
        {
            if (ordering != null)
                ordering.Clear();

            nextIndex = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Copies the ordering mapping (and next index) from the specified
        /// dictionary into this dictionary, if both are available.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose ordering information is copied.
        /// </param>
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

        /// <summary>
        /// Rebuilds the ordering mapping from the current contents of this
        /// dictionary when the ordering is detected to be out of sync (i.e. its
        /// count does not match the dictionary count).
        /// </summary>
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

        /// <summary>
        /// Adds the specified key to the ordering mapping, assigning it the next
        /// available index.
        /// </summary>
        /// <param name="key">
        /// The key to add to the ordering mapping.
        /// </param>
        /// <param name="errorOnFound">
        /// Non-zero if a trace error should be emitted when the key is already
        /// present in the ordering mapping.
        /// </param>
        /// <returns>
        /// True if the key was added to the ordering mapping; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Removes the specified key from the ordering mapping, adjusting the
        /// indexes of the remaining keys so that there are no gaps.
        /// </summary>
        /// <param name="key">
        /// The key to remove from the ordering mapping.
        /// </param>
        /// <returns>
        /// True if the key was removed from the ordering mapping; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// Restores the private data of this dictionary (the path translation
        /// type, the ordering mapping, and the next index) from previously
        /// serialized data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for this dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context, describing the source and destination of the
        /// serialized data.
        /// </param>
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
        /// <summary>
        /// Gets or sets the value associated with the specified key, translating
        /// the key according to the path translation type.
        /// </summary>
        /// <param name="key">
        /// The key whose associated value is retrieved or set.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
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

        /// <summary>
        /// Adds an entry with the specified key and value to this dictionary,
        /// translating the key according to the path translation type and
        /// recording it in the ordering mapping.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The value of the entry to add.
        /// </param>
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

        /// <summary>
        /// Determines whether this dictionary contains an entry with the
        /// specified key, translating the key according to the path translation
        /// type.
        /// </summary>
        /// <param name="key">
        /// The key to locate in this dictionary.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an entry with the specified key;
        /// otherwise, false.
        /// </returns>
        bool IDictionary<string, T>.ContainsKey(
            string key /* in */
            )
        {
            return base.ContainsKey(
                PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the entry with the specified key from this dictionary,
        /// translating the key according to the path translation type and
        /// updating the ordering mapping.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to remove.
        /// </param>
        /// <returns>
        /// True if the entry was found and removed; otherwise, false.
        /// </returns>
        bool IDictionary<string, T>.Remove(
            string key /* in */
            )
        {
            key = PathOps.TranslatePath(key, translationType);
            RemoveFromTheOrdering(key);
            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to get the value associated with the specified key,
        /// translating the key according to the path translation type.
        /// </summary>
        /// <param name="key">
        /// The key whose associated value is retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified key;
        /// otherwise, receives the default value for the value type.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an entry with the specified key;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Gets the collection of keys in this dictionary.  Getting this
        /// property is not supported and always throws
        /// <see cref="NotSupportedException" />, because the ordering-aware
        /// accessors must be used instead.
        /// </summary>
        public new ICollection<string> Keys
        {
            get { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the collection of values in this dictionary.  Getting this
        /// property is not supported and always throws
        /// <see cref="NotSupportedException" />, because the ordering-aware
        /// accessors must be used instead.
        /// </summary>
        public new ICollection<T> Values
        {
            get { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the value associated with the specified key, translating
        /// the key according to the path translation type.
        /// </summary>
        /// <param name="key">
        /// The key whose associated value is retrieved or set.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
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

        /// <summary>
        /// Adds an entry with the specified key and value to this dictionary,
        /// translating the key according to the path translation type and
        /// recording it in the ordering mapping.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The value of the entry to add.
        /// </param>
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

        /// <summary>
        /// Removes all entries from this dictionary and clears the ordering
        /// mapping.
        /// </summary>
        public new void Clear()
        {
            ClearTheOrdering();
            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this dictionary contains an entry with the
        /// specified key, translating the key according to the path translation
        /// type.
        /// </summary>
        /// <param name="key">
        /// The key to locate in this dictionary.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an entry with the specified key;
        /// otherwise, false.
        /// </returns>
        public new bool ContainsKey(
            string key /* in */
            )
        {
            return base.ContainsKey(
                PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns an enumerator that iterates over the entries of this
        /// dictionary.
        /// </summary>
        /// <returns>
        /// An enumerator for the entries of this dictionary.
        /// </returns>
        public new IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return GetBaseEnumerator();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the entry with the specified key from this dictionary,
        /// translating the key according to the path translation type and
        /// updating the ordering mapping.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to remove.
        /// </param>
        /// <returns>
        /// True if the entry was found and removed; otherwise, false.
        /// </returns>
        public new bool Remove(
            string key /* in */
            )
        {
            key = PathOps.TranslatePath(key, translationType);
            RemoveFromTheOrdering(key);
            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to get the value associated with the specified key,
        /// translating the key according to the path translation type.
        /// </summary>
        /// <param name="key">
        /// The key whose associated value is retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified key;
        /// otherwise, receives the default value for the value type.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an entry with the specified key;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// The path translation type used to normalize the keys of this
        /// dictionary.
        /// </summary>
        private PathTranslationType translationType;
        /// <summary>
        /// Gets the path translation type used to normalize the keys of this
        /// dictionary.
        /// </summary>
        public PathTranslationType TranslationType
        {
            get { return translationType; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Gets the keys of this dictionary in their relative insertion order.
        /// </summary>
        /// <param name="reverse">
        /// Non-zero to return the keys in reverse order.
        /// </param>
        /// <returns>
        /// The list of keys in order, or null if the ordering information is
        /// missing.
        /// </returns>
        public virtual StringList GetKeysInOrder(
            bool reverse /* in */
            )
        {
            return GetKeyOrKeysInOrder(Index.Invalid, reverse);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the key/value pairs of this dictionary in their relative
        /// insertion order.
        /// </summary>
        /// <param name="reverse">
        /// Non-zero to return the pairs in reverse order.
        /// </param>
        /// <returns>
        /// The key/value pairs in order, or null if the ordering information is
        /// missing.
        /// </returns>
        public virtual IEnumerable<KeyValuePair<string, T>> GetPairsInOrder(
            bool reverse /* in */
            )
        {
            return GetPairOrPairsInOrder(Index.Invalid, reverse);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the key at the specified ordinal position within the relative
        /// insertion order of this dictionary.
        /// </summary>
        /// <param name="index">
        /// The zero-based ordinal position of the key to retrieve.
        /// </param>
        /// <param name="reverse">
        /// Non-zero to interpret the position relative to the reversed order.
        /// </param>
        /// <returns>
        /// The key at the specified position, or null if there is no such key.
        /// </returns>
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

        /// <summary>
        /// Adds each key in the specified collection to this dictionary, each
        /// associated with a newly created value.
        /// </summary>
        /// <param name="collection">
        /// The collection of path keys to add to this dictionary.
        /// </param>
        public virtual void Add(
            IEnumerable<string> collection /* in */
            )
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified key to this dictionary, associated with a newly
        /// created value.
        /// </summary>
        /// <param name="key">
        /// The path key to add to this dictionary.
        /// </param>
        public virtual void Add(
            string key /* in */
            )
        {
            this.Add(key, new T());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this dictionary contains an entry with the
        /// specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate in this dictionary.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an entry with the specified key;
        /// otherwise, false.
        /// </returns>
        public virtual bool Contains(
            string key /* in */
            )
        {
            return this.ContainsKey(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds an entry with the specified key and value to this dictionary,
        /// then returns the stored value.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The value of the entry to add.
        /// </param>
        /// <param name="reserved">
        /// Reserved for future use; this parameter is currently ignored.
        /// </param>
        /// <returns>
        /// The value now associated with the specified key.
        /// </returns>
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

        /// <summary>
        /// Converts the keys of this dictionary, in their relative insertion
        /// order, to a string in the Eagle list format, optionally including
        /// only those keys matching the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included in the
        /// resulting string.  This parameter may be null, in which case all
        /// keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public virtual string ToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            return ToString(pattern, noCase, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys of this dictionary, in their relative insertion
        /// order, to a string in the Eagle list format, optionally including
        /// only those keys matching the specified pattern and optionally in
        /// reverse order.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included in the
        /// resulting string.  This parameter may be null, in which case all
        /// keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="reverse">
        /// Non-zero to emit the keys in reverse order.
        /// </param>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public virtual string ToString(
            string pattern, /* in */
            bool noCase,    /* in */
            bool reverse    /* in */
            )
        {
            return ParserOps<string>.ListToString(
                GetKeysInOrder(reverse), Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern,
                noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Reports an internal error within this class by (optionally) breaking
        /// into the debugger and emitting a diagnostic trace message.
        /// </summary>
        /// <param name="message">
        /// The error message to report.
        /// </param>
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

        /// <summary>
        /// Returns an enumerator, provided by the base dictionary, that
        /// iterates over the entries of this dictionary.
        /// </summary>
        /// <returns>
        /// An enumerator for the entries of this dictionary.
        /// </returns>
        private IEnumerator<KeyValuePair<string, T>> GetBaseEnumerator()
        {
            return base.GetEnumerator();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// Gets either a single key (at the specified ordinal position) or all
        /// keys, in their relative insertion order, as a list.
        /// </summary>
        /// <param name="index">
        /// The zero-based ordinal position of the key to retrieve, or an invalid
        /// index to retrieve all keys in order.
        /// </param>
        /// <param name="reverse">
        /// Non-zero to return the keys in reverse order.
        /// </param>
        /// <returns>
        /// The list of keys, or null if the ordering information is missing or
        /// invalid.
        /// </returns>
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

        /// <summary>
        /// Gets either a single key/value pair (at the specified ordinal
        /// position) or all key/value pairs, in their relative insertion order,
        /// as a list.
        /// </summary>
        /// <param name="index">
        /// The zero-based ordinal position of the pair to retrieve, or an
        /// invalid index to retrieve all pairs in order.
        /// </param>
        /// <param name="reverse">
        /// Non-zero to return the pairs in reverse order.
        /// </param>
        /// <returns>
        /// The list of key/value pairs, or null if the ordering information is
        /// missing or invalid.
        /// </returns>
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

        /// <summary>
        /// Adds each key in the specified collection to this dictionary, each
        /// associated with a newly created value, optionally skipping keys that
        /// are already present.
        /// </summary>
        /// <param name="collection">
        /// The collection of path keys to add to this dictionary.
        /// </param>
        /// <param name="merge">
        /// Non-zero to skip keys that are already present in this dictionary;
        /// otherwise, every key is added (which may throw on a duplicate key).
        /// </param>
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
        /// <summary>
        /// Populates the specified serialization information with the data
        /// needed to serialize this dictionary, including its path translation
        /// type, ordering mapping, and next index.
        /// </summary>
        /// <param name="info">
        /// The object that receives the serialized data for this dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context, describing the source and destination of the
        /// serialized data.
        /// </param>
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
        /// <summary>
        /// Converts the keys of this dictionary, in their relative insertion
        /// order, to a string in the Eagle list format.
        /// </summary>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
