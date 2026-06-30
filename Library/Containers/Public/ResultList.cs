/*
 * ResultList.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents an ordered, growable collection of
    /// <see cref="Result" /> objects.  In addition to the normal list
    /// behavior it inherits, it can transparently flatten nested result
    /// lists as items are added, squash a single-element (or empty) list down
    /// to a bare string, and optionally skip null or empty elements when
    /// producing its string form.  These behaviors are controlled per-instance
    /// via <see cref="ResultFlags" /> supplied at construction time.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6a4ae770-2976-4753-bc9b-ee9dc47e409e")]
    public class ResultList : List<Result>, ICloneable
    {
        #region Private Data
        //
        // HACK: By default, when adding result lists into this result list,
        //       add them as a range of items, rather than as one item.
        //
        /// <summary>
        /// The default value for the per-instance flag controlling whether a
        /// result that is itself a collection of results is added as a range of
        /// items rather than as a single item.
        /// </summary>
        private static bool DefaultAddRange = true; /* COMPAT: Eagle beta. */

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: By default, allow squashing of superfluous and/or duplicate
        //       results in the list when converting to a string?
        //
        /// <summary>
        /// The default value for the per-instance flag controlling whether
        /// superfluous and/or duplicate results are squashed when the list is
        /// converted to a string.
        /// </summary>
        private static bool DefaultSquash = true; /* COMPAT: Eagle beta. */

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: By default, omit any null / empty results in the list when
        //       converting to a string?
        //
        /// <summary>
        /// The default value for the per-instance flag controlling whether null
        /// or empty results are omitted when the list is converted to a string.
        /// </summary>
        private static bool DefaultSkipEmpty = false;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, a result that is itself a collection of results is
        /// added as a range of items rather than as a single item.
        /// </summary>
        private bool addRange;

        /// <summary>
        /// When non-zero, superfluous and/or duplicate results are squashed
        /// when this list is converted to a string.
        /// </summary>
        private bool squash;

        /// <summary>
        /// When non-zero, null or empty results are omitted when this list is
        /// converted to a string.
        /// </summary>
        private bool skipEmpty;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs a result list with the specified initial capacity and
        /// per-instance behavior flags.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of elements the list can store before resizing.
        /// </param>
        /// <param name="addRange">
        /// Non-zero if a result that is itself a collection of results should
        /// be added as a range of items rather than as a single item.
        /// </param>
        /// <param name="squash">
        /// Non-zero if superfluous and/or duplicate results should be squashed
        /// when this list is converted to a string.
        /// </param>
        /// <param name="skipEmpty">
        /// Non-zero if null or empty results should be omitted when this list
        /// is converted to a string.
        /// </param>
        private ResultList(
            int capacity,
            bool addRange,
            bool squash,
            bool skipEmpty
            )
            : base(capacity)
        {
            Initialize(addRange, squash, skipEmpty);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty result list, configuring its per-instance
        /// behavior from the specified result flags.
        /// </summary>
        /// <param name="flags">
        /// The flags used to configure the range-adding, squashing, and
        /// skip-empty behavior of this list.
        /// </param>
        internal ResultList(
            ResultFlags flags
            )
            : base()
        {
            Initialize(flags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty result list using the default per-instance
        /// behavior.
        /// </summary>
        public ResultList()
            : base()
        {
            Initialize(ResultFlags.DefaultListMask);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a result list initially populated with the elements
        /// copied from the specified collection, using the default per-instance
        /// behavior.
        /// </summary>
        /// <param name="collection">
        /// The collection of results whose elements are copied into the new
        /// list.
        /// </param>
        public ResultList(
            IEnumerable<Result> collection
            )
            : base(collection)
        {
            Initialize(ResultFlags.DefaultListMask);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a result list by flattening the specified collection of
        /// result lists, appending the elements of each contained list in turn,
        /// using the default per-instance behavior.
        /// </summary>
        /// <param name="collection">
        /// The collection of result lists whose elements are flattened into the
        /// new list.
        /// </param>
        public ResultList(
            IEnumerable<ResultList> collection
            )
            : base()
        {
            Initialize(ResultFlags.DefaultListMask);

            foreach (ResultList item in collection)
                this.AddRange(item); // NOTE: Flatten.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a result list initially populated with the specified
        /// results, using the default per-instance behavior.
        /// </summary>
        /// <param name="results">
        /// The results used to initially populate the new list.
        /// </param>
        public ResultList(
            params Result[] results
            )
            : base(results)
        {
            Initialize(ResultFlags.DefaultListMask);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a result list initially populated with the specified
        /// results, configuring its per-instance behavior from the specified
        /// result flags.
        /// </summary>
        /// <param name="flags">
        /// The flags used to configure the range-adding, squashing, and
        /// skip-empty behavior of this list.
        /// </param>
        /// <param name="results">
        /// The results used to initially populate the new list.
        /// </param>
        public ResultList(
            ResultFlags flags,
            params Result[] results
            )
            : base(results)
        {
            Initialize(flags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method configures the per-instance behavior flags of this list
        /// by translating the specified result flags into explicit (or default)
        /// settings.
        /// </summary>
        /// <param name="flags">
        /// The flags used to configure the range-adding, squashing, and
        /// skip-empty behavior of this list.
        /// </param>
        private void Initialize(
            ResultFlags flags /* in */
            )
        {
            bool? addRange = null;

            if (FlagOps.HasFlags(flags, ResultFlags.AddRange, true))
                addRange = true;
            else if (FlagOps.HasFlags(flags, ResultFlags.NoAddRange, true))
                addRange = false;

            bool? squash = null;

            if (FlagOps.HasFlags(flags, ResultFlags.Squash, true))
                squash = true;
            else if (FlagOps.HasFlags(flags, ResultFlags.NoSquash, true))
                squash = false;

            bool? skipEmpty = null;

            if (FlagOps.HasFlags(flags, ResultFlags.SkipEmpty, true))
                skipEmpty = true;
            else if (FlagOps.HasFlags(flags, ResultFlags.NoSkipEmpty, true))
                skipEmpty = false;

            Initialize(addRange, squash, skipEmpty);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method configures the per-instance behavior flags of this list,
        /// falling back to the corresponding default values for any flag that is
        /// not explicitly specified.
        /// </summary>
        /// <param name="addRange">
        /// Non-zero if a result that is itself a collection of results should be
        /// added as a range of items rather than as a single item; null to use
        /// the default.
        /// </param>
        /// <param name="squash">
        /// Non-zero if superfluous and/or duplicate results should be squashed
        /// when this list is converted to a string; null to use the default.
        /// </param>
        /// <param name="skipEmpty">
        /// Non-zero if null or empty results should be omitted when this list is
        /// converted to a string; null to use the default.
        /// </param>
        private void Initialize(
            bool? addRange, /* in: OPTIONAL */
            bool? squash,   /* in: OPTIONAL */
            bool? skipEmpty /* in: OPTIONAL */
            )
        {
            if (addRange != null)
                this.addRange = (bool)addRange;
            else
                this.addRange = DefaultAddRange;

            if (squash != null)
                this.squash = (bool)squash;
            else
                this.squash = DefaultSquash;

            if (skipEmpty != null)
                this.skipEmpty = (bool)skipEmpty;
            else
                this.skipEmpty = DefaultSkipEmpty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new result list containing the elements of the
        /// two specified collections, in order.  Either collection may be null,
        /// in which case it contributes no elements.
        /// </summary>
        /// <param name="collection1">
        /// The first collection of results to include.  This parameter may be
        /// null.
        /// </param>
        /// <param name="collection2">
        /// The second collection of results to include.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created result list containing the combined elements.
        /// </returns>
        internal static ResultList Combine(
            IEnumerable<Result> collection1,
            IEnumerable<Result> collection2
            )
        {
            ResultList collection = new ResultList();

            if (collection1 != null)
                foreach (Result item in collection1)
                    collection.Add(item);

            if (collection2 != null)
                foreach (Result item in collection2)
                    collection.Add(item);

            return collection;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Search Methods
        /// <summary>
        /// This method searches the list for the first element whose string
        /// value equals the specified string, using the system default
        /// comparison.
        /// </summary>
        /// <param name="result">
        /// The string value to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the first matching element, or
        /// <see cref="Index.Invalid" /> if no matching element is found.
        /// </returns>
        public int Find(
            string result
            )
        {
            return Find(result,
                SharedStringOps.GetSystemComparisonType(false));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the list for the first element whose string
        /// value equals the specified string, using the specified comparison.
        /// </summary>
        /// <param name="result">
        /// The string value to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules used to compare each element with the
        /// specified value.
        /// </param>
        /// <returns>
        /// The zero-based index of the first matching element, or
        /// <see cref="Index.Invalid" /> if no matching element is found.
        /// </returns>
        public int Find(
            string result,
            StringComparison comparisonType
            )
        {
            for (int index = 0; index < this.Count; index++)
            {
                if (SharedStringOps.Equals(
                        this[index], result, comparisonType))
                {
                    return index;
                }
            }

            return Index.Invalid;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Methods
        /// <summary>
        /// This method adds the specified result to the list.  This override
        /// honors the per-instance range-adding behavior, optionally flattening
        /// a result that is itself a collection of results.
        /// </summary>
        /// <param name="item">
        /// The result to add.  This parameter may be null.
        /// </param>
        public new void Add(
            Result item
            )
        {
            /* IGNORED */
            MaybeAddRange(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified result to the list.  When range-adding
        /// is enabled and the result wraps a collection of results, the elements
        /// of that collection are added as a range; otherwise, the result is
        /// added as a single item.
        /// </summary>
        /// <param name="item">
        /// The result to add.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the result was added as a range of items; otherwise, false.
        /// </returns>
        public bool MaybeAddRange(
            Result item
            )
        {
            if (addRange && (item != null))
            {
                IEnumerable<Result> collection =
                    item.Value as IEnumerable<Result>;

                if (collection != null)
                {
                    base.AddRange(collection);
                    return true;
                }
            }

            base.Add(item);
            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AddRange Methods
        /// <summary>
        /// This method appends the elements of the specified collection of
        /// strings to the list, converting each string into a result.  A null
        /// collection is ignored.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to append.  This parameter may be null.
        /// </param>
        public void AddRange(
            IEnumerable<string> collection
            )
        {
            if (collection == null)
                return;

            foreach (string item in collection)
                base.Add(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method converts the list to a string, joining the matching
        /// elements with a single space and honoring the per-instance squashing
        /// and skip-empty behavior.
        /// </summary>
        /// <param name="pattern">
        /// An optional pattern used to filter which elements are included.  This
        /// parameter may be null to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed without regard to
        /// case.
        /// </param>
        /// <returns>
        /// The string representation of the list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the list to a string, joining the matching
        /// elements with the specified separator and honoring the per-instance
        /// squashing and skip-empty behavior.  When squashing is enabled, the
        /// resulting string is not guaranteed to be a well-formed list.
        /// </summary>
        /// <param name="separator">
        /// The string used to separate adjacent elements.
        /// </param>
        /// <param name="pattern">
        /// An optional pattern used to filter which elements are included.  This
        /// parameter may be null to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed without regard to
        /// case.
        /// </param>
        /// <returns>
        /// The string representation of the list.
        /// </returns>
        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
            IList<Result> list;
            string stringValue; /* REUSED */

            if (skipEmpty)
            {
                list = null;

                foreach (Result result in this)
                {
                    if (result == null)
                        continue;

                    if (result.Value == null)
                        continue;

                    stringValue = result.ToString();

                    if (String.IsNullOrEmpty(stringValue))
                        continue;

                    if (list == null)
                        list = new ResultList();

                    list.Add(result);
                }
            }
            else
            {
                list = this;
            }

            if (squash)
            {
                //
                // HACK: The caller of this method should NOT rely upon
                //       the resulting string being a well-formed list
                //       as this is no longer guaranteed.
                //
                if ((list == null) || (list.Count == 0))
                {
                    return String.Empty;
                }
                else if (list.Count == 1)
                {
                    Result result = list[0];

                    if (result != null)
                    {
                        stringValue = result.ToString();

                        if (!String.IsNullOrEmpty(stringValue))
                            return stringValue;
                    }

                    return String.Empty;
                }
            }

            return ParserOps<Result>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the list to a string by concatenating the raw
        /// string form of every element, with no separator and without any
        /// squashing or skip-empty processing.
        /// </summary>
        /// <returns>
        /// The concatenated string representation of the list.
        /// </returns>
        public string ToRawString()
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (Result element in this)
                result.Append(element);

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the list to a string by concatenating the raw
        /// string form of every non-null element, separated by the specified
        /// separator and formatted using the specified flags, without any
        /// squashing or skip-empty processing.
        /// </summary>
        /// <param name="toStringFlags">
        /// The flags used to control how each element is converted to its string
        /// form.
        /// </param>
        /// <param name="separator">
        /// The string used to separate adjacent elements.  This parameter may be
        /// null for no separator.
        /// </param>
        /// <returns>
        /// The concatenated string representation of the list.
        /// </returns>
        public string ToRawString(
            ToStringFlags toStringFlags,
            string separator
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (Result element in this)
            {
                if (element != null)
                {
                    if ((separator != null) && (result.Length > 0))
                        result.Append(separator);

                    result.Append(element.ToString(toStringFlags));
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached String Helper Methods
#if CACHE_RESULT_TOSTRING
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method discards any cached string representation associated with
        /// this list, optionally cascading the invalidation to each contained
        /// result.
        /// </summary>
        /// <param name="children">
        /// Non-zero if the cached string of each contained result should also be
        /// invalidated.
        /// </param>
        private void InvalidateCachedString(
            bool children
            )
        {
            // @string = null; /* NOTE: No cached string. */

            if (children)
            {
                foreach (Result result in this)
                {
                    if (result == null)
                        continue;

                    result.InvalidateCachedString(children);
                }
            }
        }
#endif
        #endregion
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method converts the list to its default string representation,
        /// joining all elements with a single space and honoring the
        /// per-instance squashing and skip-empty behavior.
        /// </summary>
        /// <returns>
        /// The string representation of the list.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new result list that is a copy of this one,
        /// preserving the squashing and skip-empty behavior and cloning each
        /// non-null element.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            ResultList list = new ResultList(
                this.Capacity, squash, skipEmpty);

            foreach (Result element in this)
            {
                list.Add((element != null) ?
                    element.Clone() as Result : null);
            }

            return list;
        }
        #endregion
    }
}
