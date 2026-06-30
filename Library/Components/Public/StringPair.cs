/*
 * StringPair.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using Eagle._Attributes;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents an ordered pair of string values.  It is a
    /// specialization of <see cref="Pair{T}" /> for the string type and adds
    /// convenient conversions from a single string value.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2e342815-d397-43af-a5ac-8a8e5947967f")]
    public sealed class StringPair : Pair<string>
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        /// <summary>
        /// Constructs a new instance of this class that contains no string
        /// values.
        /// </summary>
        public StringPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class using the specified value
        /// for its first element.
        /// </summary>
        /// <param name="x">
        /// The value to use for the first element of the pair.
        /// </param>
        public StringPair(string x)
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class using the specified values
        /// for its first and second elements.
        /// </summary>
        /// <param name="x">
        /// The value to use for the first element of the pair.
        /// </param>
        /// <param name="y">
        /// The value to use for the second element of the pair.
        /// </param>
        public StringPair(string x, string y)
            : base(x, y)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        /// <summary>
        /// This method creates a new string pair from the specified string
        /// value, using it for the first element of the pair.
        /// </summary>
        /// <param name="value">
        /// The string value to use for the first element of the pair.
        /// </param>
        /// <returns>
        /// The newly created string pair.
        /// </returns>
        public static StringPair FromString(string value)
        {
            return new StringPair(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        /// <summary>
        /// This operator implicitly converts the specified string value into a
        /// string pair, using it for the first element of the pair.
        /// </summary>
        /// <param name="value">
        /// The string value to convert into a string pair.
        /// </param>
        /// <returns>
        /// The newly created string pair.
        /// </returns>
        public static implicit operator StringPair(string value)
        {
            return FromString(value);
        }
        #endregion
    }
}
