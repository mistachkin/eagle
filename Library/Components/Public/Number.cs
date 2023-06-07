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
using Eagle._Components.Private;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("83d21bd9-be3d-47c4-9506-37e20dedf1c7")]
    public class Number :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IValue, ICloneable
    {
        private static readonly object staticSyncRoot = new object();
        private static TypeDelegateDictionary numberTypes;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static Number()
        {
            InitializeTypes();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static void InitializeTypes()
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (numberTypes == null)
                {
                    numberTypes = new TypeDelegateDictionary();
                    numberTypes.Add(typeof(bool), null);
                    numberTypes.Add(typeof(sbyte), null);
                    numberTypes.Add(typeof(byte), null);
                    numberTypes.Add(typeof(short), null);
                    numberTypes.Add(typeof(ushort), null);
                    numberTypes.Add(typeof(char), null);
                    numberTypes.Add(typeof(int), null);
                    numberTypes.Add(typeof(uint), null);
                    numberTypes.Add(typeof(long), null);
                    numberTypes.Add(typeof(ulong), null);
                    numberTypes.Add(typeof(Enum), null);
                    numberTypes.Add(typeof(ReturnCode), null);
                    numberTypes.Add(typeof(MatchMode), null);
                    numberTypes.Add(typeof(MidpointRounding), null);
                    numberTypes.Add(typeof(decimal), null);
                    numberTypes.Add(typeof(float), null);
                    numberTypes.Add(typeof(double), null);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static TypeList GetTypes()
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (numberTypes == null)
                    return null;

                return new TypeList(numberTypes.Keys);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveType(
            Type type
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (numberTypes == null)
                    return false;

                if (type == null)
                    return false;

                return numberTypes.ContainsKey(type);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSupported(
            object value,
            ref Type type
            )
        {
            if (value == null)
                return true;

            type = AppDomainOps.MaybeGetTypeOrObject(value);

            return IsSupported(null, type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number()
        {
            numberValue = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(object value) /* throw */
        {
            //
            // NOTE: This is virtual; therefore, this may throw an exception if
            //       the derived class does not support this type OR if somebody
            //       is using this class directly and we do not support this type.
            //
            this.Value = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(bool value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(sbyte value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(byte value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(short value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(ushort value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(char value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(int value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(uint value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(long value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(ulong value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(Enum value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(ReturnCode value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(MatchMode value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(MidpointRounding value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(decimal value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(float value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(double value)
        {
            numberValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Number(Number value)
        {
            if (value != null)
                numberValue = value.Value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(object obj)
        {
            Number number = obj as Number;

            if (number != null)
                return GenericOps<object>.Equals(this.Value, number.Value);
            else
                return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return GenericOps<object>.GetHashCode(this.Value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return GenericOps<object>.ToString(this.Value, String.Empty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBoolean(Number number, ref bool value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsBoolean())
                    {
                        value = (bool)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToBoolean(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToSignedByte(Number number, ref sbyte value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsSignedByte())
                    {
                        value = (sbyte)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToSByte(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToByte(Number number, ref byte value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsByte())
                    {
                        value = (byte)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToByte(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToNarrowInteger(Number number, ref short value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsNarrowInteger())
                    {
                        value = (short)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToInt16(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedNarrowInteger(Number number, ref ushort value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsUnsignedNarrowInteger())
                    {
                        value = (ushort)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToUInt16(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToCharacter(Number number, ref char value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsCharacter())
                    {
                        value = (char)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToChar(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToInteger(Number number, ref int value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsInteger())
                    {
                        value = (int)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToInt32(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedInteger(Number number, ref uint value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsUnsignedInteger())
                    {
                        value = (uint)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToUInt32(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToWideInteger(Number number, ref long value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsWideInteger())
                    {
                        value = (long)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToInt64(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToUnsignedWideInteger(Number number, ref ulong value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsUnsignedWideInteger())
                    {
                        value = (ulong)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToUInt64(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToReturnCode(Number number, ref ReturnCode value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsReturnCode())
                    {
                        value = (ReturnCode)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = (ReturnCode)convertible.ToUInt64(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToMatchMode(Number number, ref MatchMode value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsMatchMode())
                    {
                        value = (MatchMode)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = (MatchMode)convertible.ToUInt64(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToMidpointRounding(Number number, ref MidpointRounding value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsMidpointRounding())
                    {
                        value = (MidpointRounding)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = (MidpointRounding)convertible.ToUInt64(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToDecimal(Number number, ref decimal value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsDecimal())
                    {
                        value = (decimal)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToDecimal(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToSingle(Number number, ref float value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsSingle())
                    {
                        value = (float)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToSingle(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToDouble(Number number, ref double value)
        {
            bool result = false;

            try
            {
                if (number != null)
                {
                    if (number.IsDouble())
                    {
                        value = (double)number.Value;

                        result = true;
                    }
                    else
                    {
                        IConvertible convertible = number.Value as IConvertible;

                        if (convertible != null)
                        {
                            value = convertible.ToDouble(null);

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToBoolean(ref bool value)
        {
            return ToBoolean(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToSignedByte(ref sbyte value)
        {
            return ToSignedByte(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToByte(ref byte value)
        {
            return ToByte(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToNarrowInteger(ref short value)
        {
            return ToNarrowInteger(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToUnsignedNarrowInteger(ref ushort value)
        {
            return ToUnsignedNarrowInteger(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToCharacter(ref char value)
        {
            return ToCharacter(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToInteger(ref int value)
        {
            return ToInteger(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToUnsignedInteger(ref uint value)
        {
            return ToUnsignedInteger(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToWideInteger(ref long value)
        {
            return ToWideInteger(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToUnsignedWideInteger(ref ulong value)
        {
            return ToUnsignedWideInteger(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToReturnCode(ref ReturnCode value)
        {
            return ToReturnCode(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToMatchMode(ref MatchMode value)
        {
            return ToMatchMode(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToMidpointRounding(ref MidpointRounding value)
        {
            return ToMidpointRounding(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToDecimal(ref decimal value)
        {
            return ToDecimal(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToSingle(ref float value)
        {
            return ToSingle(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToDouble(ref double value)
        {
            return ToDouble(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsBoolean()
        {
            return (numberValue is bool);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsSignedByte()
        {
            return (numberValue is sbyte);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsByte()
        {
            return (numberValue is byte);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsNarrowInteger()
        {
            return (numberValue is short);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsUnsignedNarrowInteger()
        {
            return (numberValue is ushort);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsCharacter()
        {
            return (numberValue is char);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsInteger()
        {
            return (numberValue is int);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsUnsignedInteger()
        {
            return (numberValue is uint);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsWideInteger()
        {
            return (numberValue is long);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsUnsignedWideInteger()
        {
            return (numberValue is ulong);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsReturnCode()
        {
            return (numberValue is ReturnCode);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsMatchMode()
        {
            return (numberValue is MatchMode);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsMidpointRounding()
        {
            return (numberValue is MidpointRounding);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsDecimal()
        {
            return (numberValue is decimal);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsSingle()
        {
            return (numberValue is float);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsDouble()
        {
            return (numberValue is double);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsIntegral()
        {
            return IsBoolean() || IsSignedByte() || IsByte() ||
                IsNarrowInteger() || IsUnsignedNarrowInteger() ||
                IsCharacter() || IsInteger() || IsUnsignedInteger() ||
                IsWideInteger() || IsUnsignedWideInteger();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsEnum()
        {
            return IsReturnCode() || IsMatchMode() || IsMidpointRounding();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsIntegralOrEnum()
        {
            return IsIntegral() || IsEnum();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsFixedPoint()
        {
            return IsDecimal();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsFloatingPoint()
        {
            return IsSingle() || IsDouble();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static TypeList Types(Number value /* NOT USED */)
        {
            return GetTypes();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual TypeList Types()
        {
            return Types(this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSupported(Number value /* NOT USED */, Type type)
        {
            if (type == null)
                return false;

            return type.IsEnum ?
                HaveType(typeof(Enum)) : HaveType(type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSupported(object value)
        {
            Type type = null;

            return IsSupported(value, ref type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsSupported(Type type)
        {
            return IsSupported(this, type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ConvertTo(Type type)
        {
            bool result = false;

            if (type == typeof(bool))
            {
                bool boolValue = false;

                if (result = ToBoolean(ref boolValue))
                    numberValue = boolValue;
            }
            else if (type == typeof(sbyte))
            {
                sbyte sbyteValue = 0;

                if (result = ToSignedByte(ref sbyteValue))
                    numberValue = sbyteValue;
            }
            else if (type == typeof(byte))
            {
                byte byteValue = 0;

                if (result = ToByte(ref byteValue))
                    numberValue = byteValue;
            }
            else if (type == typeof(short))
            {
                short shortValue = 0;

                if (result = ToNarrowInteger(ref shortValue))
                    numberValue = shortValue;
            }
            else if (type == typeof(ushort))
            {
                ushort ushortValue = 0;

                if (result = ToUnsignedNarrowInteger(ref ushortValue))
                    numberValue = ushortValue;
            }
            else if (type == typeof(char))
            {
                char charValue = Characters.Null;

                if (result = ToCharacter(ref charValue))
                    numberValue = charValue;
            }
            else if (type == typeof(int))
            {
                int intValue = 0;

                if (result = ToInteger(ref intValue))
                    numberValue = intValue;
            }
            else if (type == typeof(uint))
            {
                uint uintValue = 0;

                if (result = ToUnsignedInteger(ref uintValue))
                    numberValue = uintValue;
            }
            else if (type == typeof(long))
            {
                long longValue = 0;

                if (result = ToWideInteger(ref longValue))
                    numberValue = longValue;
            }
            else if (type == typeof(ulong))
            {
                ulong ulongValue = 0;

                if (result = ToUnsignedWideInteger(ref ulongValue))
                    numberValue = ulongValue;
            }
            else if (type == typeof(ReturnCode))
            {
                ReturnCode code = ReturnCode.Ok;

                if (result = ToReturnCode(ref code))
                    numberValue = code;
            }
            else if (type == typeof(MatchMode))
            {
                MatchMode mode = MatchMode.None;

                if (result = ToMatchMode(ref mode))
                    numberValue = mode;
            }
            else if (type == typeof(MidpointRounding))
            {
                MidpointRounding rounding = MidpointRounding.ToEven;

                if (result = ToMidpointRounding(ref rounding))
                    numberValue = rounding;
            }
            else if (type == typeof(decimal))
            {
                decimal decimalValue = Decimal.Zero;

                if (result = ToDecimal(ref decimalValue))
                    numberValue = decimalValue;
            }
            else if (type == typeof(float))
            {
                float floatValue = 0.0f;

                if (result = ToSingle(ref floatValue))
                    numberValue = floatValue;
            }
            else if (type == typeof(double))
            {
                double doubleValue = 0.0;

                if (result = ToDouble(ref doubleValue))
                    numberValue = doubleValue;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual object BaseValue
        {
            get { return null; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        //
        // NOTE: This is a mutable class returning a non-flattened value
        //       for the IGetValue.Value property, mostly due to backward
        //       compatibility.
        //
        private object numberValue;
        public virtual object Value /* throw */
        {
            get { return numberValue; }
            set
            {
                Type type = null;

                if (IsSupported(value, ref type))
                {
                    numberValue = value;
                }
                else
                {
                    throw new ScriptException(
                        "cannot set number value",
                        new ArgumentException(String.Format(
                            "unsupported type {0}",
                            FormatOps.TypeName(type))));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual int Length
        {
            get
            {
                string stringValue = ToString();

                return (stringValue != null) ?
                    stringValue.Length : _Constants.Length.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
        #endregion
    }
}
