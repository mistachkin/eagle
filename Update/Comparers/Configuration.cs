/*
 * Configuration.cs --
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
using System.Runtime.InteropServices;
using System.Text;
using Eagle._Components.Private;
using Eagle._Components.Shared;

namespace Eagle._Comparers
{
    //
    // WARNING: For purposes of this IEqualityComparer implementation, only
    //          lookup fields of the configuration are actually considered
    //          in the comparisons (i.e. publicKeyToken, name, culture, and
    //          build type).
    //
    /// <summary>
    /// This class implements an equality comparer for configuration objects.
    /// Only the lookup fields of a configuration (i.e. its public key token,
    /// name, culture, and build type) are considered when comparing two
    /// instances.
    /// </summary>
    [Guid("330569ee-973a-459a-8dfc-665aabba72f6")]
    internal sealed class _Configuration : IEqualityComparer<Configuration>
    {
        #region Private Data
        /// <summary>
        /// The string comparison type used when comparing configuration string
        /// fields.
        /// </summary>
        private StringComparison comparisonType =
            StringOps.GetSystemComparisonType(false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The encoding used to convert configuration string fields to bytes
        /// when computing hash codes.
        /// </summary>
        private Encoding encoding = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The comparer used to compare and hash the public key token field.
        /// </summary>
        private ByteArray publicKeyTokenComparer = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The comparer used to compare and hash the culture field.
        /// </summary>
        private _CultureInfo cultureInfoComparer = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class, initializing the public key
        /// token and culture sub-comparers.
        /// </summary>
        private _Configuration()
        {
            publicKeyTokenComparer = new ByteArray();
            cultureInfoComparer = new _CultureInfo();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified string
        /// comparison type and encoding.
        /// </summary>
        /// <param name="comparisonType">
        /// The string comparison type to use when comparing configuration
        /// string fields.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when converting configuration string fields to
        /// bytes for hashing.
        /// </param>
        public _Configuration(
            StringComparison comparisonType,
            Encoding encoding
            )
            : this()
        {
            this.comparisonType = comparisonType;
            this.encoding = encoding;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<byte[]> Members
        /// <summary>
        /// This method determines whether two configuration objects are equal,
        /// comparing only their lookup fields.
        /// </summary>
        /// <param name="x">
        /// The first configuration object to compare.  This parameter may be
        /// null.
        /// </param>
        /// <param name="y">
        /// The second configuration object to compare.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the two configuration objects are considered equal;
        /// otherwise, false.
        /// </returns>
        public bool Equals(
            Configuration x,
            Configuration y
            )
        {
            if ((x == null) && (y == null))
            {
                return true;
            }
            else if ((x == null) || (y == null))
            {
                return false;
            }
            else
            {
                //
                // HACK: An "Id" of less than zero means "any".
                //
                if ((x.Id >= 0) && (y.Id >= 0) && (x.Id != y.Id))
                    return false;

                if (!StringOps.Equals(
                        x.ProtocolId, y.ProtocolId, comparisonType))
                {
                    return false;
                }

                if ((publicKeyTokenComparer != null) &&
                    !publicKeyTokenComparer.Equals(
                        x.PublicKeyToken, y.PublicKeyToken))
                {
                    return false;
                }

                if (!StringOps.Equals(x.Name, y.Name, comparisonType))
                    return false;

                if ((cultureInfoComparer != null) &&
                    !cultureInfoComparer.Equals(x.Culture, y.Culture))
                {
                    return false;
                }

                //
                // HACK: This build type checking is new as of Beta 47.
                //
                if (x.BuildType != y.BuildType)
                    return false;

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash code for the specified configuration
        /// object based on its lookup fields.
        /// </summary>
        /// <param name="obj">
        /// The configuration object to compute a hash code for.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The computed hash code for the specified configuration object.
        /// </returns>
        public int GetHashCode(
            Configuration obj
            )
        {
            int result = 0;

            if ((obj != null) && (obj.ProtocolId != null) &&
                (encoding != null))
            {
                result ^= unchecked((int)HashOps.HashFnv1UInt(
                    encoding.GetBytes(obj.ProtocolId), true));
            }

            if ((obj != null) && (obj.PublicKeyToken != null) &&
                (publicKeyTokenComparer != null))
            {
                result ^= publicKeyTokenComparer.GetHashCode(
                    obj.PublicKeyToken);
            }

            if ((obj != null) && (obj.Name != null) &&
                (encoding != null))
            {
                result ^= unchecked((int)HashOps.HashFnv1UInt(
                    encoding.GetBytes(obj.Name), true));
            }

            if ((obj != null) && (obj.Culture != null) &&
                (cultureInfoComparer != null))
            {
                result ^= cultureInfoComparer.GetHashCode(
                    obj.Culture);
            }

            return result;
        }
        #endregion
    }
}
