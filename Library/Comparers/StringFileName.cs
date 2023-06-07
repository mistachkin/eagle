/*
 * StringFileName.cs --
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

namespace Eagle._Comparers
{
    [ObjectId("7300d32f-8c23-49fc-9234-8812ad5813b9")]
    internal sealed class StringFileName :
        IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        private Encoding encoding;
        private PathComparisonType pathComparisonType;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // NOTE: This is used to synchronize access to the cache dictionary,
        //       below.
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is a cache for instances of this class, stored on the
        //       basis of the chosen path comparison type.
        //
        private static Dictionary<PathComparisonType, StringFileName> cache;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static StringFileName Create(
            PathComparisonType pathComparisonType
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringFileName value;

                if (cache != null)
                {
                    if (cache.TryGetValue(pathComparisonType, out value))
                        return value;
                }
                else
                {
                    cache = new Dictionary<PathComparisonType, StringFileName>();
                }

                value = new StringFileName(pathComparisonType);
                cache[pathComparisonType] = value;

                return value;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static int ClearCache()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (cache == null)
                    return Count.Invalid;

                int result = cache.Count;

                cache.Clear();
                cache = null;

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private StringFileName(
            PathComparisonType pathComparisonType
            )
            : this(Encoding.UTF8, pathComparisonType)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        private StringFileName(
            Encoding encoding,
            PathComparisonType pathComparisonType
            )
        {
            this.encoding = encoding;
            this.pathComparisonType = pathComparisonType;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool UseStringCompare()
        {
            return ((pathComparisonType == PathComparisonType.None) ||
                (pathComparisonType == PathComparisonType.String));
        }

        ///////////////////////////////////////////////////////////////////////

        private bool UseDeepestFirst()
        {
            return pathComparisonType == PathComparisonType.DeepestFirst;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string[] SplitFileName(
            string fileName
            )
        {
            if (fileName == null)
                return null;

            return PathOps.MaybeSplit(fileName);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        public int Compare(
            string x,
            string y
            )
        {
            if (UseStringCompare())
                return SharedStringOps.Compare(x, y, PathOps.ComparisonType);

            ///////////////////////////////////////////////////////////////////

            if ((x == null) && (y == null))
            {
                return 0;
            }
            else if (x == null)
            {
                if (UseDeepestFirst())
                    return 1;

                return -1;
            }
            else if (y == null)
            {
                if (UseDeepestFirst())
                    return -1;

                return 1;
            }
            else
            {
                string[] px = SplitFileName(x);
                string[] py = SplitFileName(y);

                int lx = px.Length;
                int ly = py.Length;

                int result = 0;

                for (int index = 0; index < Math.Min(lx, ly); index++)
                {
                    result = SharedStringOps.Compare(
                        px[index], py[index], PathOps.ComparisonType);

                    if (result != 0)
                        break;
                }

                if (lx == ly)
                    return result;

                if (UseDeepestFirst())
                {
                    if (lx > ly)
                        return -1;

                    return 1;
                }
                else
                {
                    if (lx > ly)
                        return 1;

                    return -1;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        public bool Equals(
            string x,
            string y
            )
        {
            return Compare(x, y) == 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(string obj)
        {
            int result = 0;

            if ((obj != null) && (encoding != null))
            {
                if (!UseStringCompare())
                    result = SplitFileName(obj).Length;

                result ^= unchecked((int)MathOps.HashFnv1UInt(
                    encoding.GetBytes(obj), true));
            }

            return result;
        }
        #endregion
    }
}
