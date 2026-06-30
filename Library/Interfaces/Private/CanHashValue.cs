/*
 * CanHashValue.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface represents an entity whose value can be reduced to a
    /// cryptographic hash, exposing both the cached hash bytes and a method to
    /// (re)compute them.
    /// </summary>
    [ObjectId("9e271f2a-8a46-44fe-8b6e-43dd11eb7559")]
    internal interface ICanHashValue
    {
        /// <summary>
        /// This method computes and returns the hash of the underlying value.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The computed hash value, as an array of bytes, or null if the hash
        /// could not be computed.
        /// </returns>
        byte[] GetHashValue(ref Result error);

        /// <summary>
        /// Gets or sets the cached hash value, as an array of bytes.
        /// </summary>
        byte[] HashValue { get; set; }
    }
}
