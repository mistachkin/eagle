/*
 * AnyValueTypeData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines methods for retrieving named values as specific
    /// value types, such as booleans, the various integer and floating-point
    /// types, characters, dates and times, time spans, and enumerations.
    /// Each method reports its outcome both through its return value and
    /// through an error parameter.
    /// </summary>
    [ObjectId("dc0e17d1-eea9-497b-9383-07776ba7f909")]
    public interface IAnyValueTypeData
    {
        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// boolean.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetBoolean(
            string name,
            bool toString,
            out bool value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// nullable boolean.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetNullableBoolean(
            string name,
            bool toString,
            out bool? value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// signed byte.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetSignedByte(
            string name,
            bool toString,
            out sbyte value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as an
        /// unsigned byte.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetByte(
            string name,
            bool toString,
            out byte value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// 16-bit signed integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetNarrowInteger(
            string name,
            bool toString,
            out short value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// 16-bit unsigned integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetUnsignedNarrowInteger(
            string name,
            bool toString,
            out ushort value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// character.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetCharacter(
            string name,
            bool toString,
            out char value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// 32-bit signed integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetInteger(
            string name,
            bool toString,
            out int value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// 32-bit unsigned integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetUnsignedInteger(
            string name,
            bool toString,
            out uint value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// 64-bit signed integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetWideInteger(
            string name,
            bool toString,
            out long value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// 64-bit unsigned integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetUnsignedWideInteger(
            string name,
            bool toString,
            out ulong value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// decimal.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetDecimal(
            string name,
            bool toString,
            out decimal value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// single-precision floating-point value.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetSingle(
            string name,
            bool toString,
            out float value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// double-precision floating-point value.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetDouble(
            string name,
            bool toString,
            out double value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// date and time.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="format">
        /// The custom format string to use when parsing the value, or null to
        /// use the default formats.
        /// </param>
        /// <param name="kind">
        /// The <see cref="DateTimeKind" /> to assume for the parsed value.
        /// </param>
        /// <param name="styles">
        /// The <see cref="DateTimeStyles" /> that control how the value is
        /// parsed.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetDateTime(
            string name,
            string format,
            DateTimeKind kind,
            DateTimeStyles styles,
            bool toString,
            out DateTime value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// time span.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetTimeSpan(
            string name,
            bool toString,
            out TimeSpan value,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name as a
        /// value of the specified enumerated type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when interpreting the value.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the value should be converted to.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert the underlying value to its string
        /// representation before it is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetEnum(
            Interpreter interpreter,
            string name,
            Type enumType,
            bool toString,
            out Enum value,
            ref Result error
            );
    }
}
