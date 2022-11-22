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
        private static bool DefaultAddRange = true; /* COMPAT: Eagle beta. */

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: By default, allow squashing of superfluous and/or duplicate
        //       results in the list when converting to a string?
        //
        private static bool DefaultSquash = true; /* COMPAT: Eagle beta. */

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: By default, omit any null / empty results in the list when
        //       converting to a string?
        //
        private static bool DefaultSkipEmpty = false;

        ///////////////////////////////////////////////////////////////////////

        private bool addRange;
        private bool squash;
        private bool skipEmpty;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        #region Dead Code
#if DEAD_CODE
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
        public ResultList()
            : base()
        {
            Initialize(ResultFlags.DefaultListMask);
        }

        ///////////////////////////////////////////////////////////////////////

        public ResultList(
            IEnumerable<Result> collection
            )
            : base(collection)
        {
            Initialize(ResultFlags.DefaultListMask);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public ResultList(
            params Result[] results
            )
            : base(results)
        {
            Initialize(ResultFlags.DefaultListMask);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public int Find(
            string result
            )
        {
            return Find(result,
                SharedStringOps.GetSystemComparisonType(false));
        }

        ///////////////////////////////////////////////////////////////////////

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
        public new void Add(
            Result item
            )
        {
            /* IGNORED */
            MaybeAddRange(item);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public string ToRawString()
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (Result element in this)
                result.Append(element);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString(
            ToStringFlags toStringFlags,
            string separator
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (Result element in this)
            {
                if (element != null)
                {
                    if ((separator != null) && (result.Length > 0))
                        result.Append(separator);

                    result.Append(element.ToString(toStringFlags));
                }
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached String Helper Methods
#if CACHE_RESULT_TOSTRING
        #region Dead Code
#if DEAD_CODE
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
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
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
