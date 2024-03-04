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
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("9cf2e8f7-39ea-4fa6-8799-c0d75d5794c5")]
    internal static class NumberOps
    {
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
        #endregion
    }
}
