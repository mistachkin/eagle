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
    /// <summary>
    /// This class compares and tests file name strings for equality, optionally
    /// splitting each path into its components and ordering them according to
    /// the configured <see cref="PathComparisonType" /> (for example, deepest
    /// path first).  Instances are cached on the basis of the chosen comparison
    /// type.
    /// </summary>
    [ObjectId("7300d32f-8c23-49fc-9234-8812ad5813b9")]
    internal sealed class StringFileName :
        IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The encoding used to convert file name strings to bytes when
        /// computing hash codes.
        /// </summary>
        private Encoding encoding;

        /// <summary>
        /// The path comparison strategy that determines how file name strings
        /// are ordered.
        /// </summary>
        private PathComparisonType pathComparisonType;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // NOTE: This is used to synchronize access to the cache dictionary,
        //       below.
        //
        /// <summary>
        /// The object used to synchronize access to the instance cache.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is a cache for instances of this class, stored on the
        //       basis of the chosen path comparison type.
        //
        /// <summary>
        /// The cache of instances of this class, keyed by the path comparison
        /// type each instance was created with.
        /// </summary>
        private static Dictionary<PathComparisonType, StringFileName> cache;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// Returns an instance of this class configured for the specified path
        /// comparison type, creating and caching a new instance if one does not
        /// already exist for that type.
        /// </summary>
        /// <param name="pathComparisonType">
        /// The path comparison strategy that determines how file name strings
        /// are ordered.
        /// </param>
        /// <returns>
        /// A cached or newly created instance of this class for the specified
        /// path comparison type.
        /// </returns>
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
        /// <summary>
        /// Clears the cache of instances of this class.
        /// </summary>
        /// <returns>
        /// The number of cached instances that were removed, or an invalid
        /// count if the cache had not yet been created.
        /// </returns>
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
        /// <summary>
        /// Constructs an instance of this class for the specified path
        /// comparison type, using the UTF-8 encoding.
        /// </summary>
        /// <param name="pathComparisonType">
        /// The path comparison strategy that determines how file name strings
        /// are ordered.
        /// </param>
        private StringFileName(
            PathComparisonType pathComparisonType
            )
            : this(Encoding.UTF8, pathComparisonType)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class for the specified encoding and
        /// path comparison type.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to convert file name strings to bytes when
        /// computing hash codes.
        /// </param>
        /// <param name="pathComparisonType">
        /// The path comparison strategy that determines how file name strings
        /// are ordered.
        /// </param>
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
        /// <summary>
        /// Determines whether file names should be compared as plain strings,
        /// without splitting them into path components.
        /// </summary>
        /// <returns>
        /// True if a plain string comparison should be used; otherwise, false.
        /// </returns>
        private bool UseStringCompare()
        {
            return ((pathComparisonType == PathComparisonType.None) ||
                (pathComparisonType == PathComparisonType.String));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether deeper (longer) paths should sort before
        /// shallower (shorter) ones.
        /// </summary>
        /// <returns>
        /// True if deeper paths should sort first; otherwise, false.
        /// </returns>
        private bool UseDeepestFirst()
        {
            return pathComparisonType == PathComparisonType.DeepestFirst;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Splits the specified file name into its path components.
        /// </summary>
        /// <param name="fileName">
        /// The file name to split, or null.
        /// </param>
        /// <returns>
        /// An array of the path components of the file name, or null if the
        /// file name is null.
        /// </returns>
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
        /// <summary>
        /// Compares two file name strings and returns a value indicating their
        /// relative order according to the configured path comparison strategy.
        /// </summary>
        /// <param name="x">
        /// The first file name to compare.
        /// </param>
        /// <param name="y">
        /// The second file name to compare.
        /// </param>
        /// <returns>
        /// Less than zero if <paramref name="x" /> is less than
        /// <paramref name="y" />, zero if they are equal, and greater than zero
        /// if <paramref name="x" /> is greater than <paramref name="y" />.
        /// </returns>
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
        /// <summary>
        /// Determines whether two file name strings are equal according to this
        /// comparer's ordering.
        /// </summary>
        /// <param name="x">
        /// The first file name to compare.
        /// </param>
        /// <param name="y">
        /// The second file name to compare.
        /// </param>
        /// <returns>
        /// True if the file names are considered equal; otherwise, false.
        /// </returns>
        public bool Equals(
            string x,
            string y
            )
        {
            return Compare(x, y) == 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a hash code for the specified file name string that is
        /// consistent with this comparer's notion of equality.
        /// </summary>
        /// <param name="obj">
        /// The file name for which a hash code is to be computed.
        /// </param>
        /// <returns>
        /// A hash code for the specified file name.
        /// </returns>
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
