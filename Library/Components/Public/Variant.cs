/*
 * Variant.cs --
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
using System.Security;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Value = Eagle._Components.Public.Value;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Public
{
    [ObjectId("1d8b24ad-d959-43bb-92a6-e20bcb369d04")]
    public sealed class Variant :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IVariant, ICloneable
    {
        #region Private Constants
        private static readonly NumberType[] numberTypes = {
            NumberType.Integral, NumberType.FloatingPoint,
            NumberType.FixedPoint
        };

        ///////////////////////////////////////////////////////////////////////

        private static readonly TypeCode[] integralTypeCodes = {
            TypeCode.Int64, TypeCode.Int32, TypeCode.Int16,
            TypeCode.Byte, TypeCode.Boolean
        };

        ///////////////////////////////////////////////////////////////////////

        private static readonly TypeCode[] floatingTypeCodes = {
            TypeCode.Double, TypeCode.Single
        };

        ///////////////////////////////////////////////////////////////////////

        private static readonly TypeCode[] fixedTypeCodes = {
            TypeCode.Decimal
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static Variant()
        {
            NumberOps.InitializeTypes();
            VariantOps.InitializeTypes();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        #region Number Constructors (Base Class)
        //
        // HACK: This is needed for use by the GetFramework method family.
        //
        public Variant()
        {
            Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: We cannot use the clear method in this constructor because
        //         the base class constructor relies upon our overridden Value
        //         property to set the actual value.  Calling the clear method
        //         in this constructor negates the work done by our overridden
        //         Value property, leaving our value invalid for all types not
        //         supported directly by our base class.
        //
        public Variant(
            object value /* in */
            )
        {
            SetValueOrThrow(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            bool value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            sbyte value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            byte value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            short value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            ushort value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            char value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            int value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            uint value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            long value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            ulong value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            Enum value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            decimal value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            float value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            double value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IGetValue value /* in */
            )
        {
            if (value != null)
                SetValueOrThrow(value.Value); /* throw */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variant Constructors (This Class)
        public Variant(
            DateTime value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            TimeSpan value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            Guid value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            string value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            StringList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            StringDictionary value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IObject value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            ICallFrame value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            Interpreter value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            Type value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            TypeList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            EnumList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            Uri value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            Version value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            ReturnCodeList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IAlias value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IOption value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            INamespace value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            SecureString value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            Encoding value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            CultureInfo value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IPlugin value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IExecute value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            ICallback value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IRuleSet value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            IIdentifier value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public Variant(
            byte[] value /* in */
            )
        {
            SetValueNoThrow(value);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Value Helper Methods
        private IEnumerable<NumberType> GetNumberTypes()
        {
            return numberTypes;
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<TypeCode> GetTypeCodes(
            NumberType numberType /* in */
            )
        {
            switch (numberType)
            {
                case NumberType.Integral:
                    {
                        return integralTypeCodes;
                    }
                case NumberType.FloatingPoint:
                    {
                        return floatingTypeCodes;
                    }
                case NumberType.FixedPoint:
                    {
                        return fixedTypeCodes;
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void Clear()
        {
            value = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetValueNoThrow(
            object value /* in */
            )
        {
            this.value = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetValueOrThrow(
            object value /* in */
            )
        {
            IGetValue getValue = value as IGetValue;

            if (getValue != null)
            {
                if (Object.ReferenceEquals(getValue, this))
                    return;

                SetValueOrThrow(getValue.Value); /* RECURSIVE */
            }
            else if (value == null)
            {
                Clear();
            }
            if (value is StringList)
            {
                SetValueNoThrow(new StringList(
                    (StringList)value)); /* Deep Copy */
            }
            else if (value is StringDictionary)
            {
                SetValueNoThrow(new StringDictionary(
                    (IDictionary<string, string>)value)); /* Deep Copy */
            }
            else if (VariantOps.HaveType(value))
            {
                SetValueNoThrow(value);
            }
            else
            {
                Type type = null;

                if (NumberOps.HaveType(value, ref type))
                {
                    SetValueNoThrow(value);
                }
                else
                {
                    throw new ScriptException(
                        String.Format("cannot set {0} value",
                        FormatOps.TypeName(typeof(INumber))),
                        new ArgumentException(String.Format(
                            "unsupported type {0}",
                            FormatOps.TypeName(type))));
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Math Helper Methods
        private static string UnsupportedOperatorType(
            TypeCode typeCode,              /* in */
            IIdentifierName identifierName, /* in */
            Lexeme lexeme                   /* in */
            )
        {
            return String.Format(
                "unsupported operator type {0} for operand type {1}",
                FormatOps.OperatorName(identifierName, lexeme),
                typeCode);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string UnsupportedOperandType(
            string prefix,                  /* in */
            TypeCode typeCode,              /* in */
            IIdentifierName identifierName, /* in */
            Lexeme lexeme                   /* in */
            )
        {
            if (prefix != null)
                prefix = String.Format("{0} ", prefix);

            return String.Format(
                "unsupported {0}operand type {1} for operator {2}",
                prefix, typeCode, FormatOps.OperatorName(identifierName,
                lexeme));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string UnsupportedOperandTypes(
            TypeCode typeCode1,             /* in */
            TypeCode typeCode2,             /* in */
            IIdentifierName identifierName, /* in */
            Lexeme lexeme                   /* in */
            )
        {
            return String.Format(
                "type mismatch for operator {0}, {1} versus {2}",
                FormatOps.OperatorName(identifierName, lexeme),
                typeCode1, typeCode2);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        //
        // NOTE: This is a mutable class returning a non-flattened value
        //       for the IGetValue.Value property, mostly due to backward
        //       compatibility.
        //
        private object value;
        public object Value
        {
            get { return value; }
            set { SetValueOrThrow(value); /* throw */ }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            get
            {
                string stringValue = ToString();

                return (stringValue != null) ?
                    stringValue.Length : _Constants.Length.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IConvert Members
        public bool MatchNumberType(
            NumberType numberType /* in */
            )
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    {
                        return (numberType == NumberType.Integral);
                    }
                case TypeCode.Single:
                case TypeCode.Double:
                    {
                        return (numberType == NumberType.FloatingPoint);
                    }
                case TypeCode.Decimal:
                    {
                        return (numberType == NumberType.FixedPoint);
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MatchTypeCode(
            TypeCode typeCode /* in */
            )
        {
            return Convert.GetTypeCode(value) == typeCode;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ConvertTo(
            TypeCode typeCode /* in */
            )
        {
            if (MatchTypeCode(typeCode))
                return true;

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    {
                        bool boolValue = false;

                        if (ToBoolean(ref boolValue))
                        {
                            SetValueNoThrow(boolValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Char:
                    {
                        char charValue = Characters.Null;

                        if (ToCharacter(ref charValue))
                        {
                            SetValueNoThrow(charValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.SByte:
                    {
                        sbyte sbyteValue = 0;

                        if (ToSignedByte(ref sbyteValue))
                        {
                            SetValueNoThrow(sbyteValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Byte:
                    {
                        byte byteValue = 0;

                        if (ToByte(ref byteValue))
                        {
                            SetValueNoThrow(byteValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Int16:
                    {
                        short shortValue = 0;

                        if (ToNarrowInteger(ref shortValue))
                        {
                            SetValueNoThrow(shortValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.UInt16:
                    {
                        ushort ushortValue = 0;

                        if (ToUnsignedNarrowInteger(ref ushortValue))
                        {
                            SetValueNoThrow(ushortValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Int32:
                    {
                        int intValue = 0;

                        if (ToInteger(ref intValue))
                        {
                            SetValueNoThrow(intValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.UInt32:
                    {
                        uint uintValue = 0;

                        if (ToUnsignedInteger(ref uintValue))
                        {
                            SetValueNoThrow(uintValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Int64:
                    {
                        long longValue = 0;

                        if (ToWideInteger(ref longValue))
                        {
                            SetValueNoThrow(longValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.UInt64:
                    {
                        ulong ulongValue = 0;

                        if (ToUnsignedWideInteger(ref ulongValue))
                        {
                            SetValueNoThrow(ulongValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Single:
                    {
                        float floatValue = 0.0f;

                        if (ToSingle(ref floatValue))
                        {
                            SetValueNoThrow(floatValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Double:
                    {
                        double doubleValue = 0.0;

                        if (ToDouble(ref doubleValue))
                        {
                            SetValueNoThrow(doubleValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.Decimal:
                    {
                        decimal decimalValue = Decimal.Zero;

                        if (ToDecimal(ref decimalValue))
                        {
                            SetValueNoThrow(decimalValue);
                            return true;
                        }

                        break;
                    }
                case TypeCode.DateTime:
                    {
                        DateTime dateTime = DateTime.MinValue;

                        if (ToDateTime(ref dateTime))
                        {
                            SetValueNoThrow(dateTime);
                            return true;
                        }

                        break;
                    }
                case TypeCode.String:
                    {
                        string stringValue = null;

                        if (ToString(ref stringValue))
                        {
                            SetValueNoThrow(stringValue);
                            return true;
                        }

                        break;
                    }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ConvertTo(
            Type type /* in */
            )
        {
            TypeCode typeCode = TypeCode.Empty;

            if (NumberOps.HaveTypeCode(
                    type, ref typeCode) &&
                (typeCode != TypeCode.Object) &&
                ConvertTo(typeCode))
            {
                return true;
            }

            if (type == typeof(ReturnCode))
            {
                ReturnCode code = ReturnCode.Ok;

                if (ToReturnCode(ref code))
                {
                    SetValueNoThrow(code);
                    return true;
                }
            }
            else if (type == typeof(MatchMode))
            {
                MatchMode mode = MatchMode.None;

                if (ToMatchMode(ref mode))
                {
                    SetValueNoThrow(mode);
                    return true;
                }
            }
            else if (type == typeof(MidpointRounding))
            {
                MidpointRounding rounding = MidpointRounding.ToEven;

                if (ToMidpointRounding(ref rounding))
                {
                    SetValueNoThrow(rounding);
                    return true;
                }
            }
            else if (type == typeof(TimeSpan))
            {
                TimeSpan timeSpan = TimeSpan.Zero;

                if (ToTimeSpan(ref timeSpan))
                {
                    SetValueNoThrow(timeSpan);
                    return true;
                }
            }
            else if (type == typeof(Guid))
            {
                Guid guid = Guid.Empty;

                if (ToGuid(ref guid))
                {
                    SetValueNoThrow(guid);
                    return true;
                }
            }
            else if (type == typeof(StringList))
            {
                StringList list = null;

                if (ToList(ref list))
                {
                    SetValueNoThrow(list);
                    return true;
                }
            }
            else if (type == typeof(StringDictionary))
            {
                StringDictionary dictionary = null;

                if (ToDictionary(ref dictionary))
                {
                    SetValueNoThrow(dictionary);
                    return true;
                }
            }
            else if (type == typeof(IObject))
            {
                IObject @object = null;

                if (ToObject(ref @object))
                {
                    SetValueNoThrow(@object);
                    return true;
                }
            }
            else if (type == typeof(ICallFrame))
            {
                ICallFrame frame = null;

                if (ToCallFrame(ref frame))
                {
                    SetValueNoThrow(frame);
                    return true;
                }
            }
            else if (type == typeof(Interpreter))
            {
                Interpreter interpreter = null;

                if (ToInterpreter(ref interpreter))
                {
                    SetValueNoThrow(interpreter);
                    return true;
                }
            }
            else if (type == typeof(Type))
            {
                Type _type = null;

                if (ToType(ref _type))
                {
                    SetValueNoThrow(_type);
                    return true;
                }
            }
            else if (type == typeof(TypeList))
            {
                TypeList typeList = null;

                if (ToTypeList(ref typeList))
                {
                    SetValueNoThrow(typeList);
                    return true;
                }
            }
            else if (type == typeof(EnumList))
            {
                EnumList enumList = null;

                if (ToEnumList(ref enumList))
                {
                    SetValueNoThrow(enumList);
                    return true;
                }
            }
            else if (type == typeof(Uri))
            {
                Uri uri = null;

                if (ToUri(ref uri))
                {
                    SetValueNoThrow(uri);
                    return true;
                }
            }
            else if (type == typeof(Version))
            {
                Version version = null;

                if (ToVersion(ref version))
                {
                    SetValueNoThrow(version);
                    return true;
                }
            }
            else if (type == typeof(ReturnCodeList))
            {
                ReturnCodeList returnCodeList = null;

                if (ToReturnCodeList(ref returnCodeList))
                {
                    SetValueNoThrow(returnCodeList);
                    return true;
                }
            }
            else if (type == typeof(IAlias))
            {
                IAlias alias = null;

                if (ToAlias(ref alias))
                {
                    SetValueNoThrow(alias);
                    return true;
                }
            }
            else if (type == typeof(IOption))
            {
                IOption option = null;

                if (ToOption(ref option))
                {
                    SetValueNoThrow(option);
                    return true;
                }
            }
            else if (type == typeof(INamespace))
            {
                INamespace @namespace = null;

                if (ToNamespace(ref @namespace))
                {
                    SetValueNoThrow(@namespace);
                    return true;
                }
            }
            else if (type == typeof(SecureString))
            {
                SecureString secureString = null;

                if (ToSecureString(ref secureString))
                {
                    SetValueNoThrow(secureString);
                    return true;
                }
            }
            else if (type == typeof(Encoding))
            {
                Encoding encoding = null;

                if (ToEncoding(ref encoding))
                {
                    SetValueNoThrow(encoding);
                    return true;
                }
            }
            else if (type == typeof(CultureInfo))
            {
                CultureInfo cultureInfo = null;

                if (ToCultureInfo(ref cultureInfo))
                {
                    SetValueNoThrow(cultureInfo);
                    return true;
                }
            }
            else if (type == typeof(IPlugin))
            {
                IPlugin plugin = null;

                if (ToPlugin(ref plugin))
                {
                    SetValueNoThrow(plugin);
                    return true;
                }
            }
            else if (type == typeof(IExecute))
            {
                IExecute execute = null;

                if (ToExecute(ref execute))
                {
                    SetValueNoThrow(execute);
                    return true;
                }
            }
            else if (type == typeof(ICallback))
            {
                ICallback callback = null;

                if (ToCallback(ref callback))
                {
                    SetValueNoThrow(callback);
                    return true;
                }
            }
            else if (type == typeof(IRuleSet))
            {
                IRuleSet ruleSet = null;

                if (ToRuleSet(ref ruleSet))
                {
                    SetValueNoThrow(ruleSet);
                    return true;
                }
            }
            else if (type == typeof(IIdentifier))
            {
                IIdentifier identifier = null;

                if (ToIdentifier(ref identifier))
                {
                    SetValueNoThrow(identifier);
                    return true;
                }
            }
            else if (type == typeof(byte[]))
            {
                byte[] byteArray = null;

                if (ToByteArray(ref byteArray))
                {
                    SetValueNoThrow(byteArray);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MaybeConvertWith(
            IConvert convert2, /* in */
            bool skip1,        /* in */
            bool skip2         /* in */
            )
        {
            if (convert2 == null)
                return false;

            IEnumerable<NumberType> numberTypes = GetNumberTypes();

            if (numberTypes == null)
                return false;

            foreach (NumberType numberType in numberTypes)
            {
                if (numberType == NumberType.Integral)
                {
                    if (!MatchNumberType(numberType) ||
                        !convert2.MatchNumberType(numberType))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!MatchNumberType(numberType) &&
                        !convert2.MatchNumberType(numberType))
                    {
                        continue;
                    }
                }

                IEnumerable<TypeCode> typeCodes = GetTypeCodes(numberType);

                if (typeCodes == null)
                    continue;

                foreach (TypeCode typeCode in typeCodes)
                {
                    bool match1 = MatchTypeCode(typeCode);
                    bool match2 = convert2.MatchTypeCode(typeCode);

                    if (!match1 && !match2)
                        continue;

                    if (!skip1 && !match1 && !ConvertTo(typeCode))
                        return false;

                    if (!skip2 && !match2 && !convert2.ConvertTo(typeCode))
                        return false;

                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMath Members
        public ReturnCode Calculate(
            IIdentifierName identifierName, /* in */
            Lexeme lexeme,                  /* in */
            IConvert convert,               /* in */
            ref Argument result,            /* in */
            ref Result error                /* in */
            )
        {
            object value1 = value;
            TypeCode typeCode1 = Convert.GetTypeCode(value1);

            object value2 = null;
            TypeCode typeCode2 = TypeCode.Empty;

            if (convert != null)
            {
                value2 = convert.Value;
                typeCode2 = Convert.GetTypeCode(value2);
            }

            switch (lexeme)
            {
                case Lexeme.BitwiseNot: /* Arity.Unary */
                case Lexeme.LogicalNot: /* Arity.Unary */
                case Lexeme.Minus:      /* Arity.UnaryAndBinary */
                case Lexeme.Plus:       /* Arity.UnaryAndBinary */
                    {
                        //
                        // NOTE: The first (and maybe only) type
                        //       code value will be checked below,
                        //       by the operator itself.  For the
                        //       (two) operators that can accept
                        //       either one or two operands (i.e.
                        //       Minus and Plus), that extra type
                        //       code handling is also checked by
                        //       the operators themselves.
                        //
                        break;
                    }
                default:
                    {
                        //
                        // NOTE: All other operators do require
                        //       both type codes to be equal.
                        //
                        if (typeCode1 != typeCode2) /* IMPOSSIBLE? */
                        {
                            //
                            // HACK: It is like that this code
                            //       is impossible to hit.
                            //
                            error = UnsupportedOperandTypes(
                                typeCode1, typeCode2,
                                identifierName, lexeme);

                            return ReturnCode.Error;
                        }

                        break;
                    }
            }

            switch (lexeme)
            {
                case Lexeme.Exponent:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = MathOps.Pow(
                                        ConversionOps.ToInt((bool)value1),
                                        ConversionOps.ToInt((bool)value2));

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = MathOps.Pow((int)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.Pow((long)value1, (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = Math.Pow((double)value1, (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    if (!ConvertTo(TypeCode.Double))
                                    {
                                        error = UnsupportedOperandType("1st",
                                            typeCode1, identifierName, lexeme);

                                        return ReturnCode.Error;
                                    }

                                    value1 = value; /* CONVERTED */

                                    if ((convert == null) ||
                                        !convert.ConvertTo(TypeCode.Double))
                                    {
                                        error = UnsupportedOperandType("2nd",
                                            typeCode2, identifierName, lexeme);

                                        return ReturnCode.Error;
                                    }

                                    value2 = convert.Value; /* CONVERTED */

                                    goto case TypeCode.Double;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.Multiply:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) *
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 * (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 * (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 * (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 * (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.Divide:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) /
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 / (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 / (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 / (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 / (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.Modulus:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) %
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 % (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 % (long)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.Plus:
                    {
                        if (value2 != null)
                        {
                            if (typeCode2 != typeCode1)
                            {
                                if ((convert != null) &&
                                    convert.ConvertTo(typeCode1))
                                {
                                    value2 = convert.Value; /* CONVERTED */
                                    goto case Lexeme.Plus;
                                }
                                else
                                {
                                    error = UnsupportedOperandType("2nd",
                                        typeCode2, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                            }

                            switch (typeCode1)
                            {
                                case TypeCode.Boolean:
                                    {
                                        result = ConversionOps.ToInt((bool)value1) +
                                            ConversionOps.ToInt((bool)value2);

                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int32:
                                    {
                                        result = ((int)value1 + (int)value2);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int64:
                                    {
                                        result = ((long)value1 + (long)value2);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Double:
                                    {
                                        result = ((double)value1 + (double)value2);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Decimal:
                                    {
                                        result = ((decimal)value1 + (decimal)value2);
                                        return ReturnCode.Ok;
                                    }
                                default:
                                    {
                                        error = UnsupportedOperandType(null,
                                            typeCode1, identifierName, lexeme);

                                        return ReturnCode.Error;
                                    }
                            }
                        }
                        else
                        {
                            switch (typeCode1)
                            {
                                case TypeCode.Boolean:
                                    {
                                        result = +ConversionOps.ToInt((bool)value1);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int32:
                                    {
                                        result = +(int)value1;
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int64:
                                    {
                                        result = +(long)value1;
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Double:
                                    {
                                        result = +(double)value1;
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Decimal:
                                    {
                                        result = +(decimal)value1;
                                        return ReturnCode.Ok;
                                    }
                                default:
                                    {
                                        error = UnsupportedOperandType("1st",
                                            typeCode1, identifierName, lexeme);

                                        return ReturnCode.Error;
                                    }
                            }
                        }
                    }
                case Lexeme.Minus:
                    {
                        if (value2 != null)
                        {
                            if (typeCode2 != typeCode1)
                            {
                                if ((convert != null) &&
                                    convert.ConvertTo(typeCode1))
                                {
                                    value2 = convert.Value; /* CONVERTED */
                                    goto case Lexeme.Minus;
                                }
                                else
                                {
                                    error = UnsupportedOperandType("2nd",
                                        typeCode2, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                            }

                            switch (typeCode1)
                            {
                                case TypeCode.Boolean:
                                    {
                                        result = ConversionOps.ToInt((bool)value1) -
                                            ConversionOps.ToInt((bool)value2);

                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int32:
                                    {
                                        result = ((int)value1 - (int)value2);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int64:
                                    {
                                        result = ((long)value1 - (long)value2);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Double:
                                    {
                                        result = ((double)value1 - (double)value2);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Decimal:
                                    {
                                        result = ((decimal)value1 - (decimal)value2);
                                        return ReturnCode.Ok;
                                    }
                                default:
                                    {
                                        error = UnsupportedOperandType(null,
                                            typeCode1, identifierName, lexeme);

                                        return ReturnCode.Error;
                                    }
                            }
                        }
                        else
                        {
                            switch (typeCode1)
                            {
                                case TypeCode.Boolean:
                                    {
                                        result = -ConversionOps.ToInt((bool)value1);
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int32:
                                    {
                                        result = -(int)value1;
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Int64:
                                    {
                                        result = -(long)value1;
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Double:
                                    {
                                        result = -(double)value1;
                                        return ReturnCode.Ok;
                                    }
                                case TypeCode.Decimal:
                                    {
                                        result = -(decimal)value1;
                                        return ReturnCode.Ok;
                                    }
                                default:
                                    {
                                        error = UnsupportedOperandType("1st",
                                            typeCode1, identifierName, lexeme);

                                        return ReturnCode.Error;
                                    }
                            }
                        }
                    }
                case Lexeme.LeftShift:
                    {
                        //
                        // HACK: *SPECIAL* Since the shift and rotate
                        //       operators require the second operand
                        //       to (always) be of type System.Int32,
                        //       make sure to convert it to that type
                        //       now, if needed.
                        //
                        if (typeCode1 == TypeCode.Int64)
                        {
                            if ((convert != null) &&
                                convert.ConvertTo(TypeCode.Int32))
                            {
                                value2 = convert.Value; /* CONVERTED */
                            }
                            else
                            {
                                error = UnsupportedOperandType("2nd",
                                    typeCode2, identifierName, lexeme);

                                return ReturnCode.Error;
                            }
                        }

                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = MathOps.LeftShift(
                                        ConversionOps.ToInt((bool)value1),
                                        ConversionOps.ToInt((bool)value2));

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = MathOps.LeftShift((int)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.LeftShift((long)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.RightShift:
                    {
                        //
                        // HACK: *SPECIAL* Since the shift and rotate
                        //       operators require the second operand
                        //       to (always) be of type System.Int32,
                        //       make sure to convert it to that type
                        //       now, if needed.
                        //
                        if (typeCode1 == TypeCode.Int64)
                        {
                            if ((convert != null) &&
                                convert.ConvertTo(TypeCode.Int32))
                            {
                                value2 = convert.Value; /* CONVERTED */
                            }
                            else
                            {
                                error = UnsupportedOperandType("2nd",
                                    typeCode2, identifierName, lexeme);

                                return ReturnCode.Error;
                            }
                        }

                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = MathOps.RightShift(
                                        ConversionOps.ToInt((bool)value1),
                                        ConversionOps.ToInt((bool)value2));

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = MathOps.RightShift((int)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.RightShift((long)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LeftRotate:
                    {
                        //
                        // HACK: *SPECIAL* Since the shift and rotate
                        //       operators require the second operand
                        //       to (always) be of type System.Int32,
                        //       make sure to convert it to that type
                        //       now, if needed.
                        //
                        if (typeCode1 == TypeCode.Int64)
                        {
                            if ((convert != null) &&
                                convert.ConvertTo(TypeCode.Int32))
                            {
                                value2 = convert.Value; /* CONVERTED */
                            }
                            else
                            {
                                error = UnsupportedOperandType("2nd",
                                    typeCode2, identifierName, lexeme);

                                return ReturnCode.Error;
                            }
                        }

                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = MathOps.LeftRotate(
                                        ConversionOps.ToInt((bool)value1),
                                        ConversionOps.ToInt((bool)value2));

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = MathOps.LeftRotate((int)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.LeftRotate((long)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.RightRotate:
                    {
                        //
                        // HACK: *SPECIAL* Since the shift and rotate
                        //       operators require the second operand
                        //       to (always) be of type System.Int32,
                        //       make sure to convert it to that type
                        //       now, if needed.
                        //
                        if (typeCode1 == TypeCode.Int64)
                        {
                            if ((convert != null) &&
                                convert.ConvertTo(TypeCode.Int32))
                            {
                                value2 = convert.Value; /* CONVERTED */
                            }
                            else
                            {
                                error = UnsupportedOperandType("2nd",
                                    typeCode2, identifierName, lexeme);

                                return ReturnCode.Error;
                            }
                        }

                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = MathOps.RightRotate(
                                        ConversionOps.ToInt((bool)value1),
                                        ConversionOps.ToInt((bool)value2));

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = MathOps.RightRotate((int)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.RightRotate((long)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LessThan:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) <
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 < (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 < (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 < (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 < (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.GreaterThan:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) >
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 > (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 > (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 > (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 > (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LessThanOrEqualTo:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) <=
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 <= (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 <= (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 <= (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 <= (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.GreaterThanOrEqualTo:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) >=
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 >= (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 >= (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 >= (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 >= (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.Equal:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) ==
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 == (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 == (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 == (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 == (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.NotEqual:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ConversionOps.ToInt((bool)value1) !=
                                        ConversionOps.ToInt((bool)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ((int)value1 != (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ((long)value1 != (long)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Double:
                                {
                                    result = ((double)value1 != (double)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Decimal:
                                {
                                    result = ((decimal)value1 != (decimal)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.BitwiseAnd:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = (bool)value1 & (bool)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Byte:
                                {
                                    result = (byte)value1 & (byte)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = (int)value1 & (int)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = (long)value1 & (long)value2;
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.BitwiseXor:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = (bool)value1 ^ (bool)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Byte:
                                {
                                    result = (byte)value1 ^ (byte)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = (int)value1 ^ (int)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = (long)value1 ^ (long)value2;
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.BitwiseOr:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = (bool)value1 | (bool)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Byte:
                                {
                                    result = (byte)value1 | (byte)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = (int)value1 | (int)value2;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = (long)value1 | (long)value2;
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.BitwiseEqv:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = LogicOps.Eqv((bool)value1, (bool)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Byte:
                                {
                                    result = LogicOps.Eqv((byte)value1, (byte)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = LogicOps.Eqv((int)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = LogicOps.Eqv((long)value1, (long)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.BitwiseImp:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = LogicOps.Imp((bool)value1, (bool)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Byte:
                                {
                                    result = LogicOps.Imp((byte)value1, (byte)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = LogicOps.Imp((int)value1, (int)value2);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = LogicOps.Imp((long)value1, (long)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LogicalAnd:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = LogicOps.And((bool)value1, (bool)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LogicalXor:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = LogicOps.Xor((bool)value1, (bool)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LogicalOr:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = LogicOps.Or((bool)value1, (bool)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LogicalEqv:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = LogicOps.Eqv((bool)value1, (bool)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LogicalImp:
                    {
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = LogicOps.Imp((bool)value1, (bool)value2);
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.LogicalNot:
                    {
                        //
                        // HACK: *SPECIAL* Since the unary operators do
                        //       not have a "fixup" phase, perform the
                        //       conversion to boolean for the one (and
                        //       only) operand now.
                        //
                        if (typeCode1 != TypeCode.Boolean)
                        {
                            if (ConvertTo(TypeCode.Boolean))
                            {
                                value1 = value; /* CONVERTED */
                                typeCode1 = TypeCode.Boolean;
                            }
                            else
                            {
                                error = UnsupportedOperandType("1st",
                                    typeCode1, identifierName, lexeme);

                                return ReturnCode.Error;
                            }
                        }

                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = !(bool)value1;
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                case Lexeme.BitwiseNot:
                    {
                        //
                        // BUGBUG: This is not correct.  We need to use
                        //         the smallest type possible here.
                        //
                        switch (typeCode1)
                        {
                            case TypeCode.Boolean:
                                {
                                    result = ~ConversionOps.ToInt((bool)value1);
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Byte:
                                {
                                    result = ~(byte)value1;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int32:
                                {
                                    result = ~(int)value1;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = ~(long)value1;
                                    return ReturnCode.Ok;
                                }
                            default:
                                {
                                    error = UnsupportedOperandType(null,
                                        typeCode1, identifierName, lexeme);

                                    return ReturnCode.Error;
                                }
                        }
                    }
                default:
                    {
                        error = UnsupportedOperatorType(
                            typeCode1, identifierName, lexeme);

                        return ReturnCode.Error;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode StringCompare(
            IIdentifierName identifierName,  /* in */
            Lexeme lexeme,                   /* in */
            IConvert convert,                /* in */
            StringComparison comparisonType, /* in */
            ref Argument result,             /* out */
            ref Result error                 /* out */
            )
        {
            if (convert == null)
            {
                error = "missing operand for string compare";
                return ReturnCode.Error;
            }

            object value1 = value;
            TypeCode typeCode1 = Convert.GetTypeCode(value1);

            if (typeCode1 != TypeCode.String)
            {
                error = UnsupportedOperandType("1st",
                    typeCode1, identifierName, lexeme);

                return ReturnCode.Error;
            }

            object value2 = convert.Value;
            TypeCode typeCode2 = Convert.GetTypeCode(value2);

            if (typeCode2 != TypeCode.String)
            {
                error = UnsupportedOperandType("2nd",
                    typeCode2, identifierName, lexeme);

                return ReturnCode.Error;
            }

            switch (lexeme)
            {
                case Lexeme.GreaterThan: /* MaybeString */
                case Lexeme.StringGreaterThan: /* String */
                    {
                        result = SharedStringOps.Compare(
                            (string)value1, (string)value2,
                            comparisonType) > 0;

                        return ReturnCode.Ok;
                    }
                case Lexeme.GreaterThanOrEqualTo: /* MaybeString */
                case Lexeme.StringGreaterThanOrEqualTo: /* String */
                    {
                        result = SharedStringOps.Compare(
                            (string)value1, (string)value2,
                            comparisonType) >= 0;

                        return ReturnCode.Ok;
                    }
                case Lexeme.LessThan: /* MaybeString */
                case Lexeme.StringLessThan: /* String */
                    {
                        result = SharedStringOps.Compare(
                            (string)value1, (string)value2,
                            comparisonType) < 0;

                        return ReturnCode.Ok;
                    }
                case Lexeme.LessThanOrEqualTo: /* MaybeString */
                case Lexeme.StringLessThanOrEqualTo: /* String */
                    {
                        result = SharedStringOps.Compare(
                            (string)value1, (string)value2,
                            comparisonType) <= 0;

                        return ReturnCode.Ok;
                    }
                case Lexeme.Equal: /* MaybeString */
                case Lexeme.StringEqual: /* String */
                    {
                        result = SharedStringOps.Equals(
                            (string)value1, (string)value2,
                            comparisonType);

                        return ReturnCode.Ok;
                    }
                case Lexeme.NotEqual: /* MaybeString */
                case Lexeme.StringNotEqual: /* String */
                    {
                        result = !SharedStringOps.Equals(
                            (string)value1, (string)value2,
                            comparisonType);

                        return ReturnCode.Ok;
                    }
                default:
                    {
                        error = UnsupportedOperatorType(
                            typeCode1, identifierName, lexeme);

                        return ReturnCode.Error;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ListMayContain(
            IIdentifierName identifierName,  /* in */
            Lexeme lexeme,                   /* in */
            IConvert convert,                /* in */
            StringComparison comparisonType, /* in */
            ref Argument result,             /* out */
            ref Result error                 /* out */
            )
        {
            if (convert == null)
            {
                error = "missing operand for list containment";
                return ReturnCode.Error;
            }

            object value1 = value;
            TypeCode typeCode1 = Convert.GetTypeCode(value1);

            if (typeCode1 != TypeCode.String)
            {
                error = UnsupportedOperandType("1st",
                    typeCode1, identifierName, lexeme);

                return ReturnCode.Error;
            }

            object value2 = convert.Value;
            TypeCode typeCode2 = Convert.GetTypeCode(value2);

            if (!(value2 is StringList))
            {
                error = UnsupportedOperandType("2nd",
                    typeCode2, identifierName, lexeme);

                return ReturnCode.Error;
            }

            switch (lexeme)
            {
                case Lexeme.ListIn:
                    {
                        result = ((StringList)value2).Contains(
                            (string)value1, comparisonType);

                        return ReturnCode.Ok;
                    }
                case Lexeme.ListNotIn:
                    {
                        result = !((StringList)value2).Contains(
                            (string)value1, comparisonType);

                        return ReturnCode.Ok;
                    }
                default:
                    {
                        error = UnsupportedOperatorType(
                            typeCode1, identifierName, lexeme);

                        return ReturnCode.Error;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INumber Members
        public bool IsBoolean()
        {
            return (value is bool);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSignedByte()
        {
            return (value is sbyte);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsByte()
        {
            return (value is byte);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsNarrowInteger()
        {
            return (value is short);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsUnsignedNarrowInteger()
        {
            return (value is ushort);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsCharacter()
        {
            return (value is char);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsInteger()
        {
            return (value is int);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsUnsignedInteger()
        {
            return (value is uint);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsWideInteger()
        {
            return (value is long);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsUnsignedWideInteger()
        {
            return (value is ulong);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsReturnCode()
        {
            return (value is ReturnCode);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDecimal()
        {
            return (value is decimal);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSingle()
        {
            return (value is float);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDouble()
        {
            return (value is double);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsIntegral()
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsEnum()
        {
            return (value is Enum);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsIntegralOrEnum()
        {
            return IsIntegral() || IsEnum();
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsFixedPoint()
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsFloatingPoint()
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToBoolean(
            ref bool value /* out */
            )
        {
            return NumberOps.ToBoolean(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToSignedByte(
            ref sbyte value /* out */
            )
        {
            return NumberOps.ToSignedByte(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToByte(
            ref byte value /* out */
            )
        {
            return NumberOps.ToByte(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToNarrowInteger(
            ref short value /* out */
            )
        {
            return NumberOps.ToNarrowInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToUnsignedNarrowInteger(
            ref ushort value /* out */
            )
        {
            return NumberOps.ToUnsignedNarrowInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToCharacter(
            ref char value /* out */
            )
        {
            return NumberOps.ToCharacter(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToInteger(
            ref int value /* out */
            )
        {
            return NumberOps.ToInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToUnsignedInteger(
            ref uint value /* out */
            )
        {
            return NumberOps.ToUnsignedInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToWideInteger(
            ref long value /* out */
            )
        {
            return NumberOps.ToWideInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToUnsignedWideInteger(
            ref ulong value /* out */
            )
        {
            return NumberOps.ToUnsignedWideInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToReturnCode(
            ref ReturnCode value /* out */
            )
        {
            return NumberOps.ToReturnCode(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToMatchMode(
            ref MatchMode value /* out */
            )
        {
            return NumberOps.ToMatchMode(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToMidpointRounding(
            ref MidpointRounding value /* out */
            )
        {
            return NumberOps.ToMidpointRounding(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToDecimal(
            ref decimal value /* out */
            )
        {
            return NumberOps.ToDecimal(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToSingle(
            ref float value /* out */
            )
        {
            return NumberOps.ToSingle(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToDouble(
            ref double value /* out */
            )
        {
            return NumberOps.ToDouble(this, null, ref value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IVariant Members
        public bool IsNumber()
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDateTime()
        {
            return (value is DateTime);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsTimeSpan()
        {
            return (value is TimeSpan);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsGuid()
        {
            return (value is Guid);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsString()
        {
            return (value is string);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsList()
        {
            return (value is StringList);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDictionary()
        {
            return (value is StringDictionary);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsObject()
        {
            return (value is IObject);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsCallFrame()
        {
            return (value is ICallFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsInterpreter()
        {
            return (value is Interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsType()
        {
            return (value is Type);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsTypeList()
        {
            return (value is TypeList);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsEnumList()
        {
            return (value is EnumList);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsUri()
        {
            return (value is Uri);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsVersion()
        {
            return (value is Version);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsReturnCodeList()
        {
            return (value is ReturnCodeList);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsAlias()
        {
            return (value is IAlias);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsOption()
        {
            return (value is IOption);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsNamespace()
        {
            return (value is INamespace);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSecureString()
        {
            return (value is SecureString);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsEncoding()
        {
            return (value is Encoding);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsCultureInfo()
        {
            return (value is CultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsPlugin()
        {
            return (value is IPlugin);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsExecute()
        {
            return (value is IExecute);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsCallback()
        {
            return (value is ICallback);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsRuleSet()
        {
            return (value is IRuleSet);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsIdentifier()
        {
            return (value is IIdentifier);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsByteArray()
        {
            return (value is byte[]);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToDateTime(
            ref DateTime value /* out */
            )
        {
            return VariantOps.ToDateTime(
                this, _Value.GetDefaultCulture(), ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToTimeSpan(
            ref TimeSpan value /* out */
            )
        {
            return VariantOps.ToTimeSpan(
                this, _Value.GetDefaultCulture(), ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToGuid(
            ref Guid value /* out */
            )
        {
            return VariantOps.ToGuid(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToString(
            ref string value /* out */
            )
        {
            return VariantOps.ToString(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToList(
            ref StringList value /* out */
            )
        {
            return VariantOps.ToList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToDictionary(
            ref StringDictionary value /* out */
            )
        {
            return VariantOps.ToDictionary(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToObject(
            ref IObject value /* out */
            )
        {
            return VariantOps.ToObject(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToCallFrame(
            ref ICallFrame value /* out */
            )
        {
            return VariantOps.ToCallFrame(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToInterpreter(
            ref Interpreter value /* out */
            )
        {
            return VariantOps.ToInterpreter(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToType(
            ref Type value /* out */
            )
        {
            return VariantOps.ToType(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToTypeList(
            ref TypeList value /* out */
            )
        {
            return VariantOps.ToTypeList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToEnumList(
            ref EnumList value /* out */
            )
        {
            return VariantOps.ToEnumList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToUri(
            ref Uri value /* out */
            )
        {
            return VariantOps.ToUri(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToVersion(
            ref Version value /* out */
            )
        {
            return VariantOps.ToVersion(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToReturnCodeList(
            ref ReturnCodeList value /* out */
            )
        {
            return VariantOps.ToReturnCodeList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToAlias(
            ref IAlias value /* out */
            )
        {
            return VariantOps.ToAlias(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToOption(
            ref IOption value /* out */
            )
        {
            return VariantOps.ToOption(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToNamespace(
            ref INamespace value /* out */
            )
        {
            return VariantOps.ToNamespace(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToSecureString(
            ref SecureString value /* out */
            )
        {
            return VariantOps.ToSecureString(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToEncoding(
            ref Encoding value /* out */
            )
        {
            return VariantOps.ToEncoding(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToCultureInfo(
            ref CultureInfo value /* out */
            )
        {
            return VariantOps.ToCultureInfo(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToPlugin(
            ref IPlugin value /* out */
            )
        {
            return VariantOps.ToPlugin(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToExecute(
            ref IExecute value /* out */
            )
        {
            return VariantOps.ToExecute(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToCallback(
            ref ICallback value /* out */
            )
        {
            return VariantOps.ToCallback(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToRuleSet(
            ref IRuleSet value /* out */
            )
        {
            return VariantOps.ToRuleSet(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToIdentifier(
            ref IIdentifier value /* out */
            )
        {
            return VariantOps.ToIdentifier(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ToByteArray(
            ref byte[] value /* out */
            )
        {
            return VariantOps.ToByteArray(this, null, ref value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new Variant(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(
            object obj /* in */
            )
        {
            IGetValue getValue = obj as IGetValue;

            if (getValue == null)
                return false;

            return GenericOps<object>.Equals(
                this.Value, getValue.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return GenericOps<object>.GetHashCode(this.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            object localValue = value;

            if (localValue is string)
            {
                return (string)localValue;
            }
            else if (localValue is byte[])
            {
                return Convert.ToBase64String((byte[])localValue,
                    Base64FormattingOptions.InsertLineBreaks);
            }
            else if (localValue is DateTime)
            {
                return FormatOps.Iso8601DateTime(
                    (DateTime)localValue);
            }
            else if (VariantOps.HaveType(localValue))
            {
                return localValue.ToString();
            }
            else
            {
                return GenericOps<object>.ToString(
                    localValue, String.Empty);
            }
        }
        #endregion
    }
}
