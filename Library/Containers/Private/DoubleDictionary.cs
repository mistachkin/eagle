/*
 * DoubleDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<string, double>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<string, double>;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to double
    /// values.  It extends the underlying generic dictionary of doubles so
    /// that it may be referred to by a simple name throughout the library.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("89e7e6d4-4366-472a-8d8e-f62acc65229e")]
    internal sealed class DoubleDictionary : SomeDictionary
    {
        /// <summary>
        /// Constructs an empty double dictionary.
        /// </summary>
        public DoubleDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs a double dictionary that is initialized with the entries
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public DoubleDictionary(
            IDictionary<string, double> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty double dictionary that uses the specified
        /// equality comparer when comparing keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer.
        /// </param>
        public DoubleDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs a double dictionary that is initialized with the entries
        /// copied from the specified dictionary and that uses the specified
        /// equality comparer when comparing keys.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer.
        /// </param>
        public DoubleDictionary(
            IDictionary<string, double> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a double dictionary from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private DoubleDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Formats a double-precision floating-point value using semantics
        /// that match those of the Tcl <c>tcl_precision</c> variable.
        /// </summary>
        /// <param name="provider">
        /// The format provider to use for the <see cref="double" /> values.
        /// </param>
        /// <param name="value">
        /// The double-precision floating-point value to format.
        /// </param>
        /// <param name="precision">
        /// The maximum number of significant digits to include, from zero
        /// through seventeen.  Zero selects the shortest round-trippable
        /// representation.
        /// </param>
        /// <returns>
        /// The invariant-culture string representation of
        /// <paramref name="value" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="precision" /> is less than zero or greater
        /// than seventeen.
        /// </exception>
        private static string FormatValue(
            IFormatProvider provider, /* in: OPTIONAL */
            double value,             /* in */
            int precision             /* in */
            )
        {
            string format = FormatOps.GetPrecisionFormat(precision);

            if (format == null)
                return value.ToString();

            return String.Format(provider, format, value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Returns the key / value pairs within this instance, with the
        /// floating-point values formatted to strings via the specified
        /// provider, if any.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used for format the floating-point
        /// values, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to be used when formatting the floating-point
        /// values.
        /// </param>
        /// <returns>
        /// The list of key / value pairs within this instance, with the
        /// floating-point values formatted to strings via the specified
        /// provider, if any.
        /// </returns>
        public StringDictionary ToList(
            Interpreter interpreter, /* in: OPTIONAL */
            CultureInfo cultureInfo  /* in: OPTIONAL */
            )
        {
            int precision = 0;

            if (interpreter != null)
            {
                if (cultureInfo == null)
                    cultureInfo = interpreter.CultureInfo;

                precision = interpreter.Precision;
            }

            StringDictionary result = new StringDictionary();

            foreach (KeyValuePair<string, double> pair in this)
            {
                result.Add(pair.Key, FormatValue(
                    cultureInfo, pair.Value, precision));
            }

            return result;
        }
        #endregion
    }
}
