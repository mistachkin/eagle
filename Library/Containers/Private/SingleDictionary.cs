/*
 * SingleDictionary.cs --
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
using SomeDictionary = Eagle._Containers.Public.FastDictionary<string, float>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<string, float>;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to
    /// single-precision floating-point values.  It extends the underlying
    /// generic dictionary without adding any further behavior.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("134668cd-45bc-4283-9c94-b51700abe4c3")]
    internal sealed class SingleDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public SingleDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public SingleDictionary(
            IDictionary<string, float> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// equality comparer when comparing keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        public SingleDictionary(
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
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary and that uses the
        /// specified equality comparer when comparing keys.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        public SingleDictionary(
            IDictionary<string, float> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private SingleDictionary(
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
        /// Formats a single-precision floating-point value using semantics
        /// that match those of the Tcl <c>tcl_precision</c> variable.
        /// </summary>
        /// <param name="provider">
        /// The format provider to use for the <see cref="float" /> values.
        /// </param>
        /// <param name="value">
        /// The single-precision floating-point value to format.
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
            float value,              /* in */
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

            foreach (KeyValuePair<string, float> pair in this)
            {
                result.Add(pair.Key, FormatValue(
                    cultureInfo, pair.Value, precision));
            }

            return result;
        }
        #endregion
    }
}
