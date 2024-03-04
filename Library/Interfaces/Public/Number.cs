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
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("4637fdfb-6019-496b-a19a-471cae317fb2")]
    public interface INumber : IMath
    {
        bool IsBoolean();
        bool IsSignedByte();
        bool IsByte();
        bool IsNarrowInteger();
        bool IsUnsignedNarrowInteger();
        bool IsCharacter();
        bool IsInteger();
        bool IsUnsignedInteger();
        bool IsWideInteger();
        bool IsUnsignedWideInteger();

        bool IsReturnCode();

        bool IsDecimal();
        bool IsSingle();
        bool IsDouble();

        bool IsIntegral();
        bool IsEnum();
        bool IsIntegralOrEnum();
        bool IsFixedPoint();
        bool IsFloatingPoint();

        bool ToBoolean(ref bool value);
        bool ToSignedByte(ref sbyte value);
        bool ToByte(ref byte value);
        bool ToNarrowInteger(ref short value);
        bool ToUnsignedNarrowInteger(ref ushort value);
        bool ToCharacter(ref char value);
        bool ToInteger(ref int value);
        bool ToUnsignedInteger(ref uint value);
        bool ToWideInteger(ref long value);
        bool ToUnsignedWideInteger(ref ulong value);

        bool ToReturnCode(ref ReturnCode value);
        bool ToMatchMode(ref MatchMode value);
        bool ToMidpointRounding(ref MidpointRounding value);

        bool ToDecimal(ref decimal value);
        bool ToSingle(ref float value);
        bool ToDouble(ref double value);
    }
}
