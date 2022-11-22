/*
 * IntPtrDictionary.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("0cbceb62-1dbb-4fa3-b73a-f99d077599f0")]
    internal sealed class IntPtrDictionary : Dictionary<string, IntPtr>
    {
        #region Public Constructors
        public IntPtrDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntPtrDictionary(IDictionary<string, IntPtr> dictionary)
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Dead Code
#if DEAD_CODE
        public bool RemoveAny(
            IntPtr value
            )
        {
            return RemoveAll(value, 1) > 0;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public int RemoveAll(
            IntPtr value,
            int limit
            )
        {
            int removed = 0;
            StringList list = new StringList();

            foreach (KeyValuePair<string, IntPtr> pair in this)
                if (pair.Value == value)
                    list.Add(pair.Key);

            foreach (string element in list)
                if ((limit == Limits.Unlimited) || (removed < limit))
                    removed += ConversionOps.ToInt(this.Remove(element));

            return removed;
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

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IDictionary<string, IntPtr> dictionary
            )
        {
            foreach (KeyValuePair<string, IntPtr> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void MaybeAdd(
            IDictionary<string, IntPtr> inputDictionary,
            ref IntPtrDictionary outputDictionary
            )
        {
            foreach (KeyValuePair<string, IntPtr> pair in inputDictionary)
            {
                if (this.ContainsKey(pair.Key))
                {
                    if (outputDictionary == null)
                        outputDictionary = new IntPtrDictionary();

                    outputDictionary.Add(pair.Key, pair.Value);
                    continue;
                }

                this.Add(pair.Key, pair.Value);
            }
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
