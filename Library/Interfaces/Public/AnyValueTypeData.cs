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
    [ObjectId("dc0e17d1-eea9-497b-9383-07776ba7f909")]
    public interface IAnyValueTypeData
    {
        bool TryGetBoolean(
            string name,
            bool toString,
            out bool value,
            ref Result error
            );

        bool TryGetNullableBoolean(
            string name,
            bool toString,
            out bool? value,
            ref Result error
            );

        bool TryGetSignedByte(
            string name,
            bool toString,
            out sbyte value,
            ref Result error
            );

        bool TryGetByte(
            string name,
            bool toString,
            out byte value,
            ref Result error
            );

        bool TryGetNarrowInteger(
            string name,
            bool toString,
            out short value,
            ref Result error
            );

        bool TryGetUnsignedNarrowInteger(
            string name,
            bool toString,
            out ushort value,
            ref Result error
            );

        bool TryGetCharacter(
            string name,
            bool toString,
            out char value,
            ref Result error
            );

        bool TryGetInteger(
            string name,
            bool toString,
            out int value,
            ref Result error
            );

        bool TryGetUnsignedInteger(
            string name,
            bool toString,
            out uint value,
            ref Result error
            );

        bool TryGetWideInteger(
            string name,
            bool toString,
            out long value,
            ref Result error
            );

        bool TryGetUnsignedWideInteger(
            string name,
            bool toString,
            out ulong value,
            ref Result error
            );

        bool TryGetDecimal(
            string name,
            bool toString,
            out decimal value,
            ref Result error
            );

        bool TryGetSingle(
            string name,
            bool toString,
            out float value,
            ref Result error
            );

        bool TryGetDouble(
            string name,
            bool toString,
            out double value,
            ref Result error
            );

        bool TryGetDateTime(
            string name,
            string format,
            DateTimeKind kind,
            DateTimeStyles styles,
            bool toString,
            out DateTime value,
            ref Result error
            );

        bool TryGetTimeSpan(
            string name,
            bool toString,
            out TimeSpan value,
            ref Result error
            );

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
