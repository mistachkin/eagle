/*
 * Convert.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by values that support conversion
    /// between numeric types and Common Language Runtime types.  It extends
    /// <see cref="IValue" /> with members to query whether the value matches
    /// a given <see cref="NumberType" /> or <see cref="TypeCode" /> and to
    /// convert the value to a requested type.
    /// </summary>
    [ObjectId("7d1043f5-12a6-458e-b927-5b202671b73d")]
    public interface IConvert : IValue
    {
        /// <summary>
        /// Determines whether the value matches the specified numeric type.
        /// </summary>
        /// <param name="numberType">
        /// The numeric type to test the value against.
        /// </param>
        /// <returns>
        /// True if the value matches the specified numeric type; otherwise,
        /// false.
        /// </returns>
        bool MatchNumberType(NumberType numberType);

        /// <summary>
        /// Determines whether the value matches the specified type code.
        /// </summary>
        /// <param name="typeCode">
        /// The type code to test the value against.
        /// </param>
        /// <returns>
        /// True if the value matches the specified type code; otherwise,
        /// false.
        /// </returns>
        bool MatchTypeCode(TypeCode typeCode);

        /// <summary>
        /// Converts this value to the type identified by the specified type
        /// code.
        /// </summary>
        /// <param name="typeCode">
        /// The type code identifying the target type to convert this value
        /// to.
        /// </param>
        /// <returns>
        /// True if the value was successfully converted; otherwise, false.
        /// </returns>
        bool ConvertTo(TypeCode typeCode);

        /// <summary>
        /// Converts this value to the specified type.
        /// </summary>
        /// <param name="type">
        /// The target type to convert this value to.
        /// </param>
        /// <returns>
        /// True if the value was successfully converted; otherwise, false.
        /// </returns>
        bool ConvertTo(Type type);

        /// <summary>
        /// Attempts to convert this value and another value so that they
        /// share a compatible type, optionally skipping conversion of either
        /// operand.
        /// </summary>
        /// <param name="convert2">
        /// The other value to be made compatible with this value.
        /// </param>
        /// <param name="skip1">
        /// Non-zero to skip converting this value.
        /// </param>
        /// <param name="skip2">
        /// Non-zero to skip converting the <paramref name="convert2" />
        /// value.
        /// </param>
        /// <returns>
        /// True if the values were successfully made compatible; otherwise,
        /// false.
        /// </returns>
        bool MaybeConvertWith(
            IConvert convert2,
            bool skip1,
            bool skip2
        );
    }
}
