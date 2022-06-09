/*
 * DateTimeListDictionary.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Containers.Private
{
    [ObjectId("5abc22da-28bb-4d04-9c9d-c1067aa50c4f")]
    internal sealed class DateTimeListDictionary :
            Dictionary<string, List<DateTime>>,
            IDictionary<string, List<DateTime>>
    {
        #region Public Constructors
        public DateTimeListDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private int Compact(
            List<DateTime> value,
            DateTime epoch
            )
        {
            if (value == null)
            {
                //
                // NOTE: This is impossible to hit because all
                //       callers check for a null value prior
                //       to calling this method.
                //
                return _Constants.Count.Invalid; /* IMPOSSIBLE */
            }

            int count = value.Count;

            if (count == 0)
            {
                //
                // NOTE: This is impossible to hit because the
                //       list of DateTime values is created with
                //       at least one element -AND- zero element
                //       lists are removed by the caller to this
                //       method.
                //
                return _Constants.Count.Invalid; /* IMPOSSIBLE */
            }

            int index = value.BinarySearch(epoch);

            if (index < 0)
                index = ~index;

            if (index > count)
            {
                //
                // NOTE: This is impossible to hit because the
                //       BinarySearch method does not return a
                //       positive value greater then the final
                //       index in the list.
                //
                return _Constants.Count.Invalid; /* IMPOSSIBLE */
            }

            value.RemoveRange(0, index);
            return index;
        }

        ///////////////////////////////////////////////////////////////////////

        private int Compact(
            DateTime epoch
            )
        {
            int count = 0;
            StringList keys = new StringList(base.Keys);

            foreach (string key in keys)
            {
                if (key == null)
                    continue;

                List<DateTime> value;

                if (!base.TryGetValue(key, out value))
                    continue;

                if (value == null)
                {
                    base.Remove(key);
                    continue;
                }

                int valueCount = Compact(value, epoch);

                if (valueCount > 0)
                {
                    count += valueCount;

                    if (value.Count == 0)
                        base.Remove(key);
                }
            }

            return count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<string, IntArgumentPair> Overrides
        List<DateTime> IDictionary<string, List<DateTime>>.this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        void IDictionary<string, List<DateTime>>.Add(
            string key,
            List<DateTime> value
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<string, List<DateTime>> Overrides
        public new List<DateTime> this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Add(
            string key,
            List<DateTime> value
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        public DateTime Now
        {
            get { return TimeOps.GetUtcNow(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public int CountFrom(
            string key,
            TimeSpan timeSpan
            )
        {
            return CountFrom(key, Now.Subtract(timeSpan));
        }

        ///////////////////////////////////////////////////////////////////////

        public int CountFrom(
            string key,
            DateTime? epoch
            )
        {
            List<DateTime> list;

            if (!base.TryGetValue(key, out list))
                return 0;

            int count = list.Count;

            if (epoch == null)
                return count;

            int index = list.BinarySearch((DateTime)epoch);

            if (index < 0)
                index = ~index;

            return count - index;
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string key,
            DateTime value,
            DateTime? epoch
            )
        {
            if (epoch != null)
                Compact((DateTime)epoch);

            List<DateTime> list;

            if (base.TryGetValue(key, out list))
            {
                int index = list.BinarySearch(value);

                if (index < 0)
                    index = ~index;

                list.Insert(index, value);
            }
            else
            {
                list = new List<DateTime>();
                list.Add(value);

                base.Add(key, list);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList result = new StringList();
            StringList keys = new StringList(this.Keys);

            foreach (string key in keys)
            {
                if (key == null)
                    continue;

                if ((pattern != null) && !Parser.StringMatch(
                        null, key, 0, pattern, 0, noCase))
                {
                    continue;
                }

                result.Add(key);

                List<DateTime> values;

                if (!base.TryGetValue(key, out values))
                    continue;

                if (values == null)
                    continue;

                StringList subResult = new StringList();

                foreach (DateTime value in values)
                {
                    subResult.Add(
                        FormatOps.TraceDateTime(value, false));
                }

                result.Add(subResult.ToString());
            }

            return result.ToString();
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
