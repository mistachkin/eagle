/*
 * NumberOps.cs --
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

#if NET_40
using System.Numerics;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

#if NET_40
using Eagle._Constants;
#endif

using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("9cf2e8f7-39ea-4fa6-8799-c0d75d5794c5")]
    internal static class NumberOps
    {
        #region Private Constants
#if NET_40
        private static readonly BigInteger MinimumDecimal =
            (BigInteger)decimal.MinValue;

        private static readonly BigInteger MaximumDecimal =
            (BigInteger)decimal.MaxValue;

        ///////////////////////////////////////////////////////////////////////

        private static readonly BigInteger MinimumSingle =
            (BigInteger)Math.Truncate((double)float.MinValue);

        private static readonly BigInteger MaximumSingle =
            (BigInteger)Math.Truncate((double)float.MaxValue);

        ///////////////////////////////////////////////////////////////////////

        private static readonly BigInteger MinimumDouble =
            (BigInteger)Math.Truncate((double)double.MinValue);

        private static readonly BigInteger MaximumDouble =
            (BigInteger)Math.Truncate((double)double.MaxValue);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static readonly object syncRoot = new object();
        private static TypeTypeCodeDictionary types;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Type Management Support
        public static void InitializeTypes()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                MaybeInitializeTypes(false, ref types);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeInitializeTypes(
            bool force,                      /* in */
            ref TypeTypeCodeDictionary types /* in, out */
            )
        {
            if (force || (types == null))
                types = new TypeTypeCodeDictionary();

            types[typeof(bool)] = TypeCode.Boolean;
            types[typeof(sbyte)] = TypeCode.SByte;
            types[typeof(byte)] = TypeCode.Byte;
            types[typeof(short)] = TypeCode.Int16;
            types[typeof(ushort)] = TypeCode.UInt16;
            types[typeof(char)] = TypeCode.Char;
            types[typeof(int)] = TypeCode.Int32;
            types[typeof(uint)] = TypeCode.UInt32;
            types[typeof(long)] = TypeCode.Int64;
            types[typeof(ulong)] = TypeCode.UInt64;
            types[typeof(Enum)] = TypeCode.Empty;
            types[typeof(ReturnCode)] = TypeCode.Empty;
            types[typeof(MatchMode)] = TypeCode.Empty;
            types[typeof(MidpointRounding)] = TypeCode.Empty;
            types[typeof(decimal)] = TypeCode.Decimal;

#if NET_40
            types[typeof(BigInteger)] = _TypeCode.BigInteger;
#endif

            types[typeof(float)] = TypeCode.Single;
            types[typeof(double)] = TypeCode.Double;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveType(
            Type type /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types == null)
                    return false;

                if (type == null)
                    return false;

                return types.ContainsKey(type);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveType(
            object value /* in */
            )
        {
            Type type = null;

            return HaveType(value, ref type);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveType(
            object value, /* in */
            ref Type type /* out */
            )
        {
            if (value == null)
                return false;

            type = AppDomainOps.MaybeGetTypeOrObject(value);

            if (type == null)
                return false;

            return HaveType(type);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveTypeCode(
            Type type /* in */
            )
        {
            TypeCode typeCode = TypeCode.Empty;

            return HaveTypeCode(type, ref typeCode);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveTypeCode(
            Type type,            /* in */
            ref TypeCode typeCode /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types == null)
                    return false;

                if (type == null)
                    return false;

                if (type.IsEnum)
                    type = typeof(Enum);

                TypeCode localTypeCode;

                if (types.TryGetValue(type, out localTypeCode))
                {
                    typeCode = localTypeCode;
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveTypeCode(
            object value /* in */
            )
        {
            TypeCode typeCode = TypeCode.Empty;

            return HaveTypeCode(value, ref typeCode);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveTypeCode(
            object value,         /* in */
            ref TypeCode typeCode /* out */
            )
        {
            if (value == null)
                return false;

            Type type = AppDomainOps.MaybeGetTypeOrObject(value);

            if (type == null)
                return false;

            return HaveTypeCode(type, ref typeCode);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TypeCode GetTypeCode(
            object value /* in */
            )
        {
#if NET_40
            if (value is BigInteger)
                return _TypeCode.BigInteger;
#endif

            return Convert.GetTypeCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        public static bool IsBigIntegerExponent(
            Lexeme lexeme,      /* in */
            TypeCode typeCode1, /* in */
            TypeCode? typeCode2 /* in */
            )
        {
            if ((lexeme == Lexeme.Exponent) &&
                (typeCode1 == _TypeCode.BigInteger) &&
                ((typeCode2 == null) ||
                ((TypeCode)typeCode2 == TypeCode.Int32)))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetRotateBits(
            int? bits /* in: OPTIONAL */
            )
        {
            if (bits != null)
            {
                int localBits = (int)bits;

                if (localBits != 0)
                    return localBits;
            }

            return ConversionOps.LongBits; // TODO: Good default?
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static int? GetRotateBits(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
#if NET_40
            if (interpreter != null)
            {
                return GetRotateBits(
                    interpreter.InternalBigIntegerRotateBits);
            }
#endif

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static TypeList GetTypes()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types == null)
                    return null;

                return new TypeList(types.Keys);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool AddTypes(
            ref TypeList types /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                TypeList localTypes = GetTypes();

                if (localTypes == null)
                    return false;

                if (types == null)
                    types = new TypeList();

                types.AddRange(localTypes);
                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Type Conversion Support
        private static bool CanConvert(
            IGetValue getValue,    /* in */
            out object objectValue /* out */
            )
        {
            objectValue = null;

            if (getValue == null)
                return false;

            object localObjectValue = getValue.Value;

            if (localObjectValue == null)
                return false;

            objectValue = localObjectValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToBoolean(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref bool value           /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is bool)
            {
                value = (bool)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToBoolean(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToBoolean(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToSignedByte(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref sbyte value          /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is sbyte)
            {
                value = (sbyte)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToSignedByte(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToSByte(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToByte(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref byte value           /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is byte)
            {
                value = (byte)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToByte(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToByte(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToNarrowInteger(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref short value          /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is short)
            {
                value = (short)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToNarrowInteger(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToInt16(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedNarrowInteger(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref ushort value         /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is ushort)
            {
                value = (ushort)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToUnsignedNarrowInteger(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToUInt16(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToCharacter(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref char value           /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is char)
            {
                value = (char)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToCharacter(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToChar(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToInteger(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref int value            /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is int)
            {
                value = (int)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToInteger(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToInt32(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedInteger(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref uint value           /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is uint)
            {
                value = (uint)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToUnsignedInteger(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToUInt32(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToWideInteger(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref long value           /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is long)
            {
                value = (long)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToWideInteger(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToInt64(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedWideInteger(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref ulong value          /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is ulong)
            {
                value = (ulong)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToUnsignedWideInteger(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToUInt64(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToReturnCode(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref ReturnCode value     /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is ReturnCode)
            {
                value = (ReturnCode)objectValue;
                return true;
            }
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = (ReturnCode)convertible.ToUInt64(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToMatchMode(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref MatchMode value      /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is MatchMode)
            {
                value = (MatchMode)objectValue;
                return true;
            }
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = (MatchMode)convertible.ToUInt64(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToMidpointRounding(
            IGetValue getValue,        /* in */
            CultureInfo cultureInfo,   /* in */
            ref MidpointRounding value /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is MidpointRounding)
            {
                value = (MidpointRounding)objectValue;
                return true;
            }
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = (MidpointRounding)convertible.ToUInt64(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToDecimal(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref decimal value        /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is decimal)
            {
                value = (decimal)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToDecimal(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToDecimal(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        public static bool ToBigInteger(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref BigInteger value     /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is BigInteger)
            {
                value = (BigInteger)objectValue;
                return true;
            }
            else
            {
                switch (GetTypeCode(objectValue))
                {
                    case TypeCode.Boolean:
                        {
                            if (objectValue is bool)
                            {
                                value = new BigInteger(
                                    (int)((bool)objectValue ? 1 : 0));

                                return true;
                            }

                            break;
                        }
                    case TypeCode.Char: /* RAW: UInt16 */
                        {
                            if (objectValue is char)
                            {
                                value = new BigInteger(
                                    (uint)(char)objectValue);

                                return true;
                            }

                            break;
                        }
                    case TypeCode.SByte:
                        {
                            if (objectValue is sbyte)
                            {
                                value = new BigInteger(
                                    (int)(sbyte)objectValue);

                                return true;
                            }

                            break;
                        }
                    case TypeCode.Byte:
                        {
                            if (objectValue is byte)
                            {
                                value = new BigInteger(
                                    (uint)(byte)objectValue);

                                return true;
                            }

                            break;
                        }
                    case TypeCode.Int16:
                        {
                            if (objectValue is short)
                            {
                                value = new BigInteger(
                                    (int)(short)objectValue);

                                return true;
                            }

                            break;
                        }
                    case TypeCode.UInt16:
                        {
                            if (objectValue is ushort)
                            {
                                value = new BigInteger(
                                    (uint)(ushort)objectValue);

                                return true;
                            }

                            break;
                        }
                    case TypeCode.Int32:
                        {
                            if (objectValue is int)
                            {
                                value = new BigInteger((int)objectValue);
                                return true;
                            }

                            break;
                        }
                    case TypeCode.UInt32:
                        {
                            if (objectValue is uint)
                            {
                                value = new BigInteger((uint)objectValue);
                                return true;
                            }

                            break;
                        }
                    case TypeCode.Int64:
                        {
                            if (objectValue is long)
                            {
                                value = new BigInteger((long)objectValue);
                                return true;
                            }

                            break;
                        }
                    case TypeCode.UInt64:
                        {
                            if (objectValue is ulong)
                            {
                                value = new BigInteger((ulong)objectValue);
                                return true;
                            }

                            break;
                        }
                    case TypeCode.Single:
                        {
                            if (objectValue is float)
                            {
                                value = new BigInteger((float)objectValue);
                                return true;
                            }

                            break;
                        }
                    case TypeCode.Double:
                        {
                            if (objectValue is double)
                            {
                                value = new BigInteger((double)objectValue);
                                return true;
                            }

                            break;
                        }
                    case TypeCode.Decimal:
                        {
                            if (objectValue is decimal)
                            {
                                value = new BigInteger((decimal)objectValue);
                                return true;
                            }

                            break;
                        }
                }
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool ToSingle(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref float value          /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is float)
            {
                value = (float)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToSingle(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToSingle(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToDouble(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref double value         /* out */
            )
        {
            object objectValue;

            if (!CanConvert(getValue, out objectValue))
                return false;

            if (objectValue is double)
            {
                value = (double)objectValue;
                return true;
            }
#if NET_40
            else if (objectValue is BigInteger)
            {
                return ToDouble(
                    (BigInteger)objectValue, cultureInfo, ref value);
            }
#endif
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToDouble(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        public static bool ToBoolean(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref bool value           /* out */
            )
        {
            value = (bigInteger != BigInteger.Zero);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToSignedByte(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref sbyte value          /* out */
            )
        {
            if ((bigInteger >= sbyte.MinValue) &&
                (bigInteger <= sbyte.MaxValue))
            {
                value = (sbyte)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToByte(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref byte value           /* out */
            )
        {
            if ((bigInteger >= byte.MinValue) &&
                (bigInteger <= byte.MaxValue))
            {
                value = (byte)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToNarrowInteger(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref short value          /* out */
            )
        {
            if ((bigInteger >= short.MinValue) &&
                (bigInteger <= short.MaxValue))
            {
                value = (short)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedNarrowInteger(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref ushort value         /* out */
            )
        {
            if ((bigInteger >= ushort.MinValue) &&
                (bigInteger <= ushort.MaxValue))
            {
                value = (ushort)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToCharacter(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref char value           /* out */
            )
        {
            if ((bigInteger >= char.MinValue) &&
                (bigInteger <= char.MaxValue))
            {
                value = (char)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToInteger(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref int value            /* out */
            )
        {
            if ((bigInteger >= int.MinValue) &&
                (bigInteger <= int.MaxValue))
            {
                value = (int)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedInteger(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref uint value           /* out */
            )
        {
            if ((bigInteger >= uint.MinValue) &&
                (bigInteger <= uint.MaxValue))
            {
                value = (uint)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToWideInteger(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref long value           /* out */
            )
        {
            if ((bigInteger >= long.MinValue) &&
                (bigInteger <= long.MaxValue))
            {
                value = (long)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedWideInteger(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref ulong value          /* out */
            )
        {
            if ((bigInteger >= ulong.MinValue) &&
                (bigInteger <= ulong.MaxValue))
            {
                value = (ulong)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToDecimal(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref decimal value        /* out */
            )
        {
            if ((bigInteger >= MinimumDecimal) &&
                (bigInteger <= MaximumDecimal))
            {
                value = (decimal)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToSingle(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref float value          /* out */
            )
        {
            if ((bigInteger >= MinimumSingle) &&
                (bigInteger <= MaximumSingle))
            {
                value = (float)bigInteger;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToDouble(
            BigInteger bigInteger,   /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref double value         /* out */
            )
        {
            if ((bigInteger >= MinimumDouble) &&
                (bigInteger <= MaximumDouble))
            {
                value = (double)bigInteger;
                return true;
            }

            return false;
        }
#endif
        #endregion
    }
}
