/*
 * StringRandom.cs --
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
using System.Globalization;
using System.Security.Cryptography;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares two strings by producing a pseudo-random ordering,
    /// using a cryptographically strong source of entropy.  It is used to
    /// implement random list sorting for the script engine, and it tracks
    /// duplicate elements so that uniqueness may optionally be enforced.
    /// </summary>
    [ObjectId("1e19eb74-75ff-4656-988e-04ecedc7a995")]
    internal sealed class StringRandomComparer :
        IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The current recursion depth used while tracking duplicate elements.
        /// </summary>
        private int levels;

        /// <summary>
        /// The interpreter context used when extracting the elements to be
        /// compared.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Non-zero if the elements are being sorted in ascending order.
        /// </summary>
        private bool ascending;

        /// <summary>
        /// The list index used to extract the sub-element to compare from each
        /// value, or null to compare the entire value.
        /// </summary>
        private string indexText;

        /// <summary>
        /// Non-zero to apply index-based extraction to the left value only.
        /// </summary>
        private bool leftOnly;

        /// <summary>
        /// Non-zero to enforce uniqueness by tracking and counting duplicate
        /// elements.
        /// </summary>
        private bool unique;

        /// <summary>
        /// The culture used when extracting and comparing the elements.
        /// </summary>
        private CultureInfo cultureInfo;

        /// <summary>
        /// The entropy provider used to obtain random bytes when comparing, if
        /// any.
        /// </summary>
        private IProvideEntropy provideEntropy;

        /// <summary>
        /// The random number generator used to obtain random bytes when
        /// comparing, if any.
        /// </summary>
        private RandomNumberGenerator randomNumberGenerator;

        /// <summary>
        /// The cache of previously computed comparison results, keyed by the
        /// pair of strings being compared, used to keep sorting results
        /// consistent.
        /// </summary>
        private Dictionary<IPair<string>, int> comparisons;

        /// <summary>
        /// The dictionary used to track the number of duplicate elements
        /// encountered during comparison.
        /// </summary>
        private IntDictionary duplicates;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class, initializing the cache of
        /// comparison results.
        /// </summary>
        private StringRandomComparer()
        {
            comparisons = new Dictionary<IPair<string>, int>();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with the specified comparison
        /// settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when extracting the elements to be
        /// compared.
        /// </param>
        /// <param name="ascending">
        /// Non-zero if the elements are being sorted in ascending order.
        /// </param>
        /// <param name="indexText">
        /// The list index used to extract the sub-element to compare from each
        /// value, or null to compare the entire value.
        /// </param>
        /// <param name="leftOnly">
        /// Non-zero to apply index-based extraction to the left value only.
        /// </param>
        /// <param name="unique">
        /// Non-zero to enforce uniqueness by tracking and counting duplicate
        /// elements.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when extracting and comparing the elements.
        /// </param>
        /// <param name="provideEntropy">
        /// The entropy provider used to obtain random bytes when comparing, if
        /// any.
        /// </param>
        /// <param name="randomNumberGenerator">
        /// The random number generator used to obtain random bytes when
        /// comparing, if any.
        /// </param>
        /// <param name="duplicates">
        /// The dictionary used to track the number of duplicate elements.  When
        /// null, a new dictionary is created and stored here.
        /// </param>
        public StringRandomComparer(
            Interpreter interpreter,
            bool ascending,
            string indexText,
            bool leftOnly,
            bool unique,
            CultureInfo cultureInfo,
            IProvideEntropy provideEntropy,
            RandomNumberGenerator randomNumberGenerator,
            ref IntDictionary duplicates
            )
            : this()
        {
            if (duplicates == null)
                duplicates = new IntDictionary(new StringCustom(this, this));

            this.levels = 0;
            this.interpreter = interpreter;
            this.ascending = ascending;
            this.indexText = indexText;
            this.leftOnly = leftOnly;
            this.unique = unique;
            this.cultureInfo = cultureInfo;
            this.provideEntropy = provideEntropy;
            this.randomNumberGenerator = randomNumberGenerator;
            this.duplicates = duplicates;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        /// <summary>
        /// This method compares two strings, producing a pseudo-random ordering
        /// for non-equal strings while keeping the results consistent for
        /// repeated comparisons of the same pair of strings.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number, indicating the
        /// relative order of the two strings.
        /// </returns>
        public int Compare(
            string left,
            string right
            )
        {
            ListOps.GetElementsToCompare(
                interpreter, ascending, indexText, leftOnly, false,
                cultureInfo, ref left, ref right); /* throw */

            //
            // NOTE: Prevent List.Sort from throwing an exception when it
            //       tries to compare to identical objects or strings.
            //
            if (String.ReferenceEquals(left, right) ||
                SharedStringOps.SystemEquals(left, right))
            {
                ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                    unique, 0, ref levels); /* throw */

                return 0;
            }

            //
            // NOTE: Prevent List.Sort from throwing an exception when it
            //       tries to check consistency with previous comparison
            //       results.
            //
            IPair<string> pair = new StringPair(left, right);

            if (comparisons.ContainsKey(pair))
            {
                ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                    unique, comparisons[pair], ref levels); /* throw */

                return comparisons[pair];
            }

            byte[] bytes;

            if (provideEntropy != null)
            {
                bytes = new byte[1];

                /* NO RESULT */
                provideEntropy.GetBytes(ref bytes);
            }
            else if (randomNumberGenerator != null)
            {
                bytes = new byte[1];

                /* NO RESULT */
                randomNumberGenerator.GetBytes(bytes);
            }
            else
            {
                throw new ScriptException(
                    "random number generator not available");
            }

            int result;

            switch (bytes[0] % 3)
            {
                case 0:
                    result = -1;
                    break;
                case 1:
                    result = 0; // BUGBUG: Makes -unique "malfunction".
                    break;
                default:
                    result = 1;
                    break;
            }

            comparisons.Add(pair, result);

            ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                unique, result, ref levels); /* throw */

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        /// <summary>
        /// This method determines whether two strings are considered equal by
        /// this comparer.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two strings are considered equal; otherwise, false.
        /// </returns>
        public bool Equals(
            string left,
            string right
            )
        {
            return ListOps.ComparerEquals<string>(this, left, right);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for the specified string.
        /// </summary>
        /// <param name="value">
        /// The string for which a hash code is computed.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// A hash code for the specified string.
        /// </returns>
        public int GetHashCode(
            string value
            )
        {
            return ListOps.ComparerGetHashCode<string>(this, value, false);
        }
        #endregion
    }
}
