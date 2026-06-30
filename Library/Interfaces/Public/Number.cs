/*
 * Number.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NET_40
using System.Numerics;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that wrap a numeric value
    /// and can report its underlying type and convert it to the various
    /// supported numeric and related types.  It extends <see cref="IMath" />
    /// with type-test (Is*) and conversion (To*) members.
    /// </summary>
    [ObjectId("4637fdfb-6019-496b-a19a-471cae317fb2")]
    public interface INumber : IMath
    {
        /// <summary>
        /// Determines whether the wrapped value is a boolean.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a boolean; otherwise, false.
        /// </returns>
        bool IsBoolean();
        /// <summary>
        /// Determines whether the wrapped value is a signed byte.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a signed byte; otherwise, false.
        /// </returns>
        bool IsSignedByte();
        /// <summary>
        /// Determines whether the wrapped value is a byte.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a byte; otherwise, false.
        /// </returns>
        bool IsByte();
        /// <summary>
        /// Determines whether the wrapped value is a narrow (16-bit) integer.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a narrow integer; otherwise, false.
        /// </returns>
        bool IsNarrowInteger();
        /// <summary>
        /// Determines whether the wrapped value is an unsigned narrow (16-bit)
        /// integer.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an unsigned narrow integer; otherwise,
        /// false.
        /// </returns>
        bool IsUnsignedNarrowInteger();
        /// <summary>
        /// Determines whether the wrapped value is a character.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a character; otherwise, false.
        /// </returns>
        bool IsCharacter();
        /// <summary>
        /// Determines whether the wrapped value is an integer (32-bit).
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an integer; otherwise, false.
        /// </returns>
        bool IsInteger();
        /// <summary>
        /// Determines whether the wrapped value is an unsigned integer
        /// (32-bit).
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an unsigned integer; otherwise, false.
        /// </returns>
        bool IsUnsignedInteger();
        /// <summary>
        /// Determines whether the wrapped value is a wide (64-bit) integer.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a wide integer; otherwise, false.
        /// </returns>
        bool IsWideInteger();
        /// <summary>
        /// Determines whether the wrapped value is an unsigned wide (64-bit)
        /// integer.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an unsigned wide integer; otherwise,
        /// false.
        /// </returns>
        bool IsUnsignedWideInteger();

#if NET_40
        /// <summary>
        /// Determines whether the wrapped value is an arbitrary-precision
        /// integer.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an arbitrary-precision integer;
        /// otherwise, false.
        /// </returns>
        bool IsBigInteger();
#endif

        /// <summary>
        /// Determines whether the wrapped value is a return code.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a return code; otherwise, false.
        /// </returns>
        bool IsReturnCode();
        /// <summary>
        /// Determines whether the wrapped value is a match mode.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a match mode; otherwise, false.
        /// </returns>
        bool IsMatchMode();
        /// <summary>
        /// Determines whether the wrapped value is a midpoint rounding mode.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a midpoint rounding mode; otherwise,
        /// false.
        /// </returns>
        bool IsMidpointRounding();

        /// <summary>
        /// Determines whether the wrapped value is a decimal.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a decimal; otherwise, false.
        /// </returns>
        bool IsDecimal();
        /// <summary>
        /// Determines whether the wrapped value is a single-precision
        /// floating-point number.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a single-precision floating-point
        /// number; otherwise, false.
        /// </returns>
        bool IsSingle();
        /// <summary>
        /// Determines whether the wrapped value is a double-precision
        /// floating-point number.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a double-precision floating-point
        /// number; otherwise, false.
        /// </returns>
        bool IsDouble();

        /// <summary>
        /// Determines whether the wrapped value is an integral type.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is integral; otherwise, false.
        /// </returns>
        bool IsIntegral();
        /// <summary>
        /// Determines whether the wrapped value is an enumerated type.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an enumerated type; otherwise, false.
        /// </returns>
        bool IsEnum();
        /// <summary>
        /// Determines whether the wrapped value is an integral or enumerated
        /// type.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is integral or enumerated; otherwise,
        /// false.
        /// </returns>
        bool IsIntegralOrEnum();
        /// <summary>
        /// Determines whether the wrapped value is a fixed-point type.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is fixed-point; otherwise, false.
        /// </returns>
        bool IsFixedPoint();
        /// <summary>
        /// Determines whether the wrapped value is a floating-point type.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is floating-point; otherwise, false.
        /// </returns>
        bool IsFloatingPoint();

        /// <summary>
        /// Attempts to convert the wrapped value to a boolean.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted boolean value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToBoolean(ref bool value);
        /// <summary>
        /// Attempts to convert the wrapped value to a signed byte.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted signed byte value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToSignedByte(ref sbyte value);
        /// <summary>
        /// Attempts to convert the wrapped value to a byte.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted byte value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToByte(ref byte value);
        /// <summary>
        /// Attempts to convert the wrapped value to a narrow (16-bit) integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted narrow integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToNarrowInteger(ref short value);
        /// <summary>
        /// Attempts to convert the wrapped value to an unsigned narrow
        /// (16-bit) integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted unsigned narrow integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToUnsignedNarrowInteger(ref ushort value);
        /// <summary>
        /// Attempts to convert the wrapped value to a character.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted character value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToCharacter(ref char value);
        /// <summary>
        /// Attempts to convert the wrapped value to an integer (32-bit).
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToInteger(ref int value);
        /// <summary>
        /// Attempts to convert the wrapped value to an unsigned integer
        /// (32-bit).
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted unsigned integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToUnsignedInteger(ref uint value);
        /// <summary>
        /// Attempts to convert the wrapped value to a wide (64-bit) integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted wide integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToWideInteger(ref long value);
        /// <summary>
        /// Attempts to convert the wrapped value to an unsigned wide (64-bit)
        /// integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted unsigned wide integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToUnsignedWideInteger(ref ulong value);

#if NET_40
        /// <summary>
        /// Attempts to convert the wrapped value to an arbitrary-precision
        /// integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted arbitrary-precision integer
        /// value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToBigInteger(ref BigInteger value);
#endif

        /// <summary>
        /// Attempts to convert the wrapped value to a return code.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted return code value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToReturnCode(ref ReturnCode value);
        /// <summary>
        /// Attempts to convert the wrapped value to a match mode.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted match mode value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToMatchMode(ref MatchMode value);
        /// <summary>
        /// Attempts to convert the wrapped value to a midpoint rounding mode.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted midpoint rounding mode value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToMidpointRounding(ref MidpointRounding value);

        /// <summary>
        /// Attempts to convert the wrapped value to a decimal.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted decimal value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToDecimal(ref decimal value);
        /// <summary>
        /// Attempts to convert the wrapped value to a single-precision
        /// floating-point number.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted single-precision value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToSingle(ref float value);
        /// <summary>
        /// Attempts to convert the wrapped value to a double-precision
        /// floating-point number.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted double-precision value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToDouble(ref double value);
    }
}
