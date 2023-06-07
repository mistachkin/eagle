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
    [ObjectId("1e19eb74-75ff-4656-988e-04ecedc7a995")]
    internal sealed class StringRandomComparer :
        IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        private int levels;
        private Interpreter interpreter;
        private bool ascending;
        private string indexText;
        private bool leftOnly;
        private bool unique;
        private CultureInfo cultureInfo;
        private IProvideEntropy provideEntropy;
        private RandomNumberGenerator randomNumberGenerator;
        private Dictionary<IPair<string>, int> comparisons;
        private IntDictionary duplicates;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private StringRandomComparer()
        {
            comparisons = new Dictionary<IPair<string>, int>();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        public bool Equals(
            string left,
            string right
            )
        {
            return ListOps.ComparerEquals<string>(this, left, right);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            string value
            )
        {
            return ListOps.ComparerGetHashCode<string>(this, value, false);
        }
        #endregion
    }
}
