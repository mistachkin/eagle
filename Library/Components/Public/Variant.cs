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

#if NET_40
using System.Numerics;
#endif

using System.Security;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;

#if NET_40
using Eagle._Constants;
#endif

using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Value = Eagle._Components.Public.Value;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a dynamically typed numeric and scalar value used
    /// by the Eagle expression engine.  A variant wraps a single underlying
    /// value -- a boolean, one of the signed or unsigned integer types, a
    /// big integer, a floating-point or fixed-point number, a string, a list,
    /// a dictionary, or one of many other supported reference types -- and
    /// provides the type queries, conversions, and arithmetic, bitwise,
    /// logical, comparison, and string operators needed to evaluate
    /// expressions.  It implements <see cref="IVariant" /> (and, through it,
    /// the conversion, number, and math interfaces) and is mutable: its value
    /// can be replaced or converted in place.  See <c>expressions.md</c> for
    /// the supported operators and operand types.
    /// </summary>
    [ObjectId("1d8b24ad-d959-43bb-92a6-e20bcb369d04")]
    public sealed class Variant :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IVariant, ICloneable
    {
        #region Private Constants
        /// <summary>
        /// The ordered set of numeric categories considered when matching or
        /// converting operands for arithmetic operations.
        /// </summary>
        private static readonly NumberType[] numberTypes = {
            NumberType.Integral, NumberType.FloatingPoint,
            NumberType.FixedPoint
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The integral type codes, in preferred (widest to narrowest) order,
        /// used when looking for a common integral type between operands.
        /// </summary>
        private static readonly TypeCode[] integralTypeCodes = {
#if NET_40
            _TypeCode.BigInteger,
#endif
            TypeCode.Int64, TypeCode.Int32, TypeCode.Int16,
            TypeCode.Byte, TypeCode.Boolean
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The floating-point type codes, in preferred (widest to narrowest)
        /// order, used when looking for a common floating-point type between
        /// operands.
        /// </summary>
        private static readonly TypeCode[] floatingTypeCodes = {
            TypeCode.Double, TypeCode.Single
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The fixed-point type codes used when looking for a common
        /// fixed-point type between operands.
        /// </summary>
        private static readonly TypeCode[] fixedTypeCodes = {
            TypeCode.Decimal
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Constructor
        /// <summary>
        /// Initializes the static state shared by all variant instances,
        /// including the number and variant type lookup tables.
        /// </summary>
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
        /// <summary>
        /// Constructs a new instance with no underlying value (a null value).
        /// </summary>
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
        /// <summary>
        /// Constructs a new instance from an arbitrary object value, throwing
        /// if the value is of an unsupported type.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            object value /* in */
            )
        {
            SetValueOrThrow(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified boolean value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            bool value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified signed byte value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            sbyte value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified byte value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            byte value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified narrow (16-bit)
        /// integer value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            short value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified unsigned narrow
        /// (16-bit) integer value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            ushort value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified character value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            char value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified integer value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            int value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified unsigned integer
        /// value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            uint value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified wide (64-bit) integer
        /// value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            long value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified unsigned wide (64-bit)
        /// integer value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            ulong value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// Constructs a new instance from the specified arbitrary-precision
        /// big integer value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            BigInteger value /* in */
            )
        {
            SetValueNoThrow(value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified enumeration value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            Enum value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified decimal (fixed-point)
        /// value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            decimal value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified single-precision
        /// floating-point value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            float value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified double-precision
        /// floating-point value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            double value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the value carried by the specified
        /// value provider, if any.
        /// </summary>
        /// <param name="value">
        /// The value provider whose value is stored in the new instance; if
        /// this is null, the new instance has no underlying value.
        /// </param>
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
        /// <summary>
        /// Constructs a new instance from the specified date and time value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            DateTime value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified time interval value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            TimeSpan value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified globally unique
        /// identifier value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            Guid value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified string value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            string value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified list value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            StringList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified dictionary value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            StringDictionary value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified opaque object wrapper
        /// value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            IObject value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified call frame value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            ICallFrame value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified interpreter value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            Interpreter value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified type value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            Type value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified list of types value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            TypeList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified list of enumerated
        /// values.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            EnumList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified uniform resource
        /// identifier value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            Uri value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified version value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            Version value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified list of return codes
        /// value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            ReturnCodeList value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified alias value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            IAlias value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified option value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            IOption value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified namespace value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            INamespace value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified secure string value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            SecureString value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified character encoding
        /// value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            Encoding value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified culture value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            CultureInfo value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified plugin value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            IPlugin value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified executable entity
        /// value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            IExecute value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified callback value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            ICallback value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified rule set value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            IRuleSet value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified identifier value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
        public Variant(
            IIdentifier value /* in */
            )
        {
            SetValueNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance from the specified byte array value.
        /// </summary>
        /// <param name="value">
        /// The underlying value to store in the new instance.
        /// </param>
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
        /// <summary>
        /// This method returns the ordered set of numeric categories
        /// considered when matching or converting operands.
        /// </summary>
        /// <returns>
        /// The supported numeric categories, in preferred order.
        /// </returns>
        private IEnumerable<NumberType> GetNumberTypes()
        {
            return numberTypes;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the type codes belonging to the specified
        /// numeric category, in preferred order.
        /// </summary>
        /// <param name="numberType">
        /// The numeric category whose type codes are requested.
        /// </param>
        /// <returns>
        /// The type codes for the specified category, or null if the category
        /// is not recognized.
        /// </returns>
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

        /// <summary>
        /// This method resets the instance so that it has no underlying value
        /// (a null value).
        /// </summary>
        private void Clear()
        {
            value = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the specified value directly as the underlying
        /// value, without any type validation or conversion.
        /// </summary>
        /// <param name="value">
        /// The value to store as the underlying value.
        /// </param>
        private void SetValueNoThrow(
            object value /* in */
            )
        {
            this.value = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates and stores the specified value as the
        /// underlying value, unwrapping value providers and deep-copying
        /// lists and dictionaries, and throwing if the value is of an
        /// unsupported type.
        /// </summary>
        /// <param name="value">
        /// The value to validate and store as the underlying value.
        /// </param>
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
            else if (value is StringList)
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

                if (NumberOps.HaveType(value, ref type) ||
                    NumberOps.HaveTypeCode(type))
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
        /// <summary>
        /// This method formats an error message indicating that the specified
        /// operator is not supported for the specified operand type.
        /// </summary>
        /// <param name="typeCode">
        /// The type code of the operand involved.
        /// </param>
        /// <param name="identifierName">
        /// The identifier name of the operator, used for formatting.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme of the operator, used for formatting.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
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

        /// <summary>
        /// This method formats an error message indicating that an operand of
        /// the specified type is not supported for the specified operator.
        /// </summary>
        /// <param name="prefix">
        /// An optional prefix (e.g. <c>1st</c> or <c>2nd</c>) identifying
        /// which operand is involved; may be null.
        /// </param>
        /// <param name="typeCode">
        /// The type code of the operand involved.
        /// </param>
        /// <param name="identifierName">
        /// The identifier name of the operator, used for formatting.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme of the operator, used for formatting.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
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

        /// <summary>
        /// This method formats an error message indicating that the two
        /// operand types do not match for the specified operator.
        /// </summary>
        /// <param name="typeCode1">
        /// The type code of the first operand.
        /// </param>
        /// <param name="typeCode2">
        /// The type code of the second operand.
        /// </param>
        /// <param name="identifierName">
        /// The identifier name of the operator, used for formatting.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme of the operator, used for formatting.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
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
        /// <summary>
        /// The underlying value wrapped by this instance.
        /// </summary>
        private object value;
        /// <summary>
        /// Gets or sets the underlying value wrapped by this instance.  The
        /// setter validates the value, throwing if it is of an unsupported
        /// type.
        /// </summary>
        public object Value
        {
            get { return value; }
            set { SetValueOrThrow(value); /* throw */ }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length, in characters, of the string form of the
        /// underlying value, or an invalid length if there is no string form.
        /// </summary>
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

        /// <summary>
        /// Gets the string form of the underlying value.
        /// </summary>
        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IConvert Members
        /// <summary>
        /// This method determines whether the underlying value belongs to the
        /// specified numeric category.
        /// </summary>
        /// <param name="numberType">
        /// The numeric category to test against.
        /// </param>
        /// <returns>
        /// True if the underlying value belongs to the specified category;
        /// otherwise, false.
        /// </returns>
        public bool MatchNumberType(
            NumberType numberType /* in */
            )
        {
            switch (NumberOps.GetTypeCode(value))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
#if NET_40
                case _TypeCode.BigInteger:
#endif
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

        /// <summary>
        /// This method determines whether the underlying value has the
        /// specified type code.
        /// </summary>
        /// <param name="typeCode">
        /// The type code to test against.
        /// </param>
        /// <returns>
        /// True if the underlying value has the specified type code;
        /// otherwise, false.
        /// </returns>
        public bool MatchTypeCode(
            TypeCode typeCode /* in */
            )
        {
            return NumberOps.GetTypeCode(value) == typeCode;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a value of the specified type code
        /// requires its shift or rotate count operand to be converted to a
        /// 32-bit integer.
        /// </summary>
        /// <param name="typeCode">
        /// The type code of the value being shifted or rotated.
        /// </param>
        /// <returns>
        /// True if the type requires the count operand to be a 32-bit integer;
        /// otherwise, false.
        /// </returns>
        public bool CanShiftOrRotate(
            TypeCode typeCode /* in */
            )
        {
            switch (typeCode)
            {
                case TypeCode.Int64:
#if NET_40
                case _TypeCode.BigInteger:
#endif
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the underlying value, in place, to the type
        /// identified by the specified type code, if such a conversion is
        /// possible.
        /// </summary>
        /// <param name="typeCode">
        /// The type code of the target type.
        /// </param>
        /// <returns>
        /// True if the underlying value already had, or was converted to, the
        /// specified type; otherwise, false.
        /// </returns>
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
#if NET_40
                case _TypeCode.BigInteger:
                    {
                        BigInteger bigIntegerValue = BigInteger.Zero;

                        if (ToBigInteger(ref bigIntegerValue))
                        {
                            SetValueNoThrow(bigIntegerValue);
                            return true;
                        }

                        break;
                    }
#endif
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

        /// <summary>
        /// This method converts the underlying value, in place, to the
        /// specified type, if such a conversion is possible.  It first tries
        /// the fast type-code path and then falls back to handling the many
        /// supported reference and value types individually.
        /// </summary>
        /// <param name="type">
        /// The target type to convert the underlying value to.
        /// </param>
        /// <returns>
        /// True if the underlying value already had, or was converted to, the
        /// specified type; otherwise, false.
        /// </returns>
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

            //
            // NOTE: This is the slow path.  It is somewhat rare
            //       and is not generally hit when dealing with
            //       with typical mathematical expressions that
            //       arise for [if], [while], etc.
            //
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

        /// <summary>
        /// This method attempts to bring this instance and another convertible
        /// value to a common type so that they can be used together as
        /// operands, converting either or both as needed.
        /// </summary>
        /// <param name="convert2">
        /// The other convertible value to coordinate with.
        /// </param>
        /// <param name="skip1">
        /// When true, this instance is not converted, even if a conversion
        /// would otherwise be performed.
        /// </param>
        /// <param name="skip2">
        /// When true, the other value is not converted, even if a conversion
        /// would otherwise be performed.
        /// </param>
        /// <returns>
        /// True if a common type was found (and the necessary conversions, if
        /// any, succeeded); otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method evaluates an arithmetic, bitwise, logical, comparison,
        /// shift, or rotate operator using the underlying value as the first
        /// operand and the value carried by the specified converter, if any,
        /// as the second operand.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier name of the operator, used for error formatting.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme identifying which operator to evaluate.
        /// </param>
        /// <param name="convert">
        /// The converter carrying the second operand, or null for a unary
        /// operator.
        /// </param>
        /// <param name="bits">
        /// The optional bit width used when rotating big integer values; may
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the computed result of the operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        public ReturnCode Calculate(
            IIdentifierName identifierName, /* in */
            Lexeme lexeme,                  /* in */
            IConvert convert,               /* in */
            int? bits,                      /* in */
            ref Argument result,            /* in */
            ref Result error                /* in */
            )
        {
            object value1 = value;
            TypeCode typeCode1 = NumberOps.GetTypeCode(value1);

            object value2 = null;
            TypeCode typeCode2 = TypeCode.Empty;

            if (convert != null)
            {
                value2 = convert.Value;
                typeCode2 = NumberOps.GetTypeCode(value2);
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
                        // NOTE: Almost all other operators do
                        //       require both type codes to be
                        //       equal.  The only real special
                        //       case here is Exponent with the
                        //       operand types of BigInteger
                        //       and Int32, which only applies
                        //       when compiling for the .NET
                        //       Framework 4.0 or later.
                        //
                        if (typeCode1 != typeCode2) /* IMPOSSIBLE? */
                        {
#if NET_40
                            if (!NumberOps.IsBigIntegerExponent(
                                    lexeme, typeCode1, typeCode2))
#endif
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
                        }

                        break;
                    }
            }

            switch (lexeme)
            {
                case Lexeme.Exponent:
                    {
#if NET_40
                        //
                        // HACK: *SPECIAL* Since the exponent operator
                        //       always requires the second operand to
                        //       be of type System.Int32, make sure to
                        //       convert it to that type now, if needed.
                        //
                        if (typeCode1 == _TypeCode.BigInteger)
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
#endif

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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = BigInteger.Pow(
                                        (BigInteger)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 * (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
                                    //
                                    // NOTE: Tcl integer division is floored (the
                                    //       quotient rounds toward negative
                                    //       infinity), unlike the .NET "/"
                                    //       operator (which truncates toward
                                    //       zero); apply the correction when
                                    //       there is a non-zero remainder and the
                                    //       operand signs differ, keeping "/"
                                    //       consistent with the floored "%" below
                                    //       so that (a/b)*b + (a%b) == a holds.
                                    //
                                    int dividend = (int)value1;
                                    int divisor = (int)value2;
                                    int quotient = dividend / divisor;
                                    int remainder = dividend % divisor;

                                    if ((remainder != 0) &&
                                        ((remainder < 0) != (divisor < 0)))
                                    {
                                        quotient--;
                                    }

                                    result = quotient;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    long dividend = (long)value1;
                                    long divisor = (long)value2;
                                    long quotient = dividend / divisor;
                                    long remainder = dividend % divisor;

                                    if ((remainder != 0) &&
                                        ((remainder < 0) != (divisor < 0)))
                                    {
                                        quotient--;
                                    }

                                    result = quotient;
                                    return ReturnCode.Ok;
                                }
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    BigInteger dividend = (BigInteger)value1;
                                    BigInteger divisor = (BigInteger)value2;
                                    BigInteger quotient = dividend / divisor;
                                    BigInteger remainder = dividend % divisor;

                                    if (!remainder.IsZero &&
                                        ((remainder.Sign < 0) != (divisor.Sign < 0)))
                                    {
                                        quotient--;
                                    }

                                    result = quotient;
                                    return ReturnCode.Ok;
                                }
#endif
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
                                    //
                                    // NOTE: Tcl modulo yields a result with the
                                    //       same sign as the divisor (floored),
                                    //       unlike the .NET "%" operator (which
                                    //       uses the sign of the dividend); apply
                                    //       the correction when the signs differ.
                                    //
                                    int dividend = (int)value1;
                                    int divisor = (int)value2;
                                    int remainder = dividend % divisor;

                                    if ((remainder != 0) &&
                                        ((remainder < 0) != (divisor < 0)))
                                    {
                                        remainder += divisor;
                                    }

                                    result = remainder;
                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    long dividend = (long)value1;
                                    long divisor = (long)value2;
                                    long remainder = dividend % divisor;

                                    if ((remainder != 0) &&
                                        ((remainder < 0) != (divisor < 0)))
                                    {
                                        remainder += divisor;
                                    }

                                    result = remainder;
                                    return ReturnCode.Ok;
                                }
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    BigInteger dividend = (BigInteger)value1;
                                    BigInteger divisor = (BigInteger)value2;
                                    BigInteger remainder = dividend % divisor;

                                    if (!remainder.IsZero &&
                                        ((remainder.Sign < 0) != (divisor.Sign < 0)))
                                    {
                                        remainder += divisor;
                                    }

                                    result = remainder;
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                                case _TypeCode.BigInteger:
                                    {
                                        result = ((BigInteger)value1 + (BigInteger)value2);
                                        return ReturnCode.Ok;
                                    }
#endif
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
#if NET_40
                                case _TypeCode.BigInteger:
                                    {
                                        result = +(BigInteger)value1;
                                        return ReturnCode.Ok;
                                    }
#endif
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
#if NET_40
                                case _TypeCode.BigInteger:
                                    {
                                        result = ((BigInteger)value1 - (BigInteger)value2);
                                        return ReturnCode.Ok;
                                    }
#endif
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
#if NET_40
                                case _TypeCode.BigInteger:
                                    {
                                        result = -(BigInteger)value1;
                                        return ReturnCode.Ok;
                                    }
#endif
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
                        if (CanShiftOrRotate(typeCode1))
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
                                    result = MathOps.LeftShift(
                                        (int)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.LeftShift(
                                        (long)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = MathOps.LeftShift(
                                        (BigInteger)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
#endif
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
                        if (CanShiftOrRotate(typeCode1))
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
                                    result = MathOps.RightShift(
                                        (int)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.RightShift(
                                        (long)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = MathOps.RightShift(
                                        (BigInteger)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
#endif
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
                        if (CanShiftOrRotate(typeCode1))
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
                                    result = MathOps.LeftRotate(
                                        (int)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.LeftRotate(
                                        (long)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = MathOps.LeftRotate(
                                        (BigInteger)value1, (int)value2,
                                        NumberOps.GetRotateBits(bits));

                                    return ReturnCode.Ok;
                                }
#endif
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
                        if (CanShiftOrRotate(typeCode1))
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
                                    result = MathOps.RightRotate(
                                        (int)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
                            case TypeCode.Int64:
                                {
                                    result = MathOps.RightRotate(
                                        (long)value1, (int)value2);

                                    return ReturnCode.Ok;
                                }
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = MathOps.RightRotate(
                                        (BigInteger)value1, (int)value2,
                                        NumberOps.GetRotateBits(bits));

                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 < (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 > (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 <= (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 >= (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 == (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 != (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 & (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 ^ (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ((BigInteger)value1 | (BigInteger)value2);
                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = LogicOps.Eqv(
                                        (BigInteger)value1, (BigInteger)value2);

                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = LogicOps.Imp(
                                        (BigInteger)value1, (BigInteger)value2);

                                    return ReturnCode.Ok;
                                }
#endif
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
#if NET_40
                            case _TypeCode.BigInteger:
                                {
                                    result = ~(BigInteger)value1;
                                    return ReturnCode.Ok;
                                }
#endif
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

        /// <summary>
        /// This method evaluates a string comparison operator using the
        /// underlying value as the first operand and the value carried by the
        /// specified converter as the second operand.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier name of the operator, used for error formatting.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme identifying which comparison operator to evaluate.
        /// </param>
        /// <param name="convert">
        /// The converter carrying the second operand.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the boolean result of the comparison.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
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
            TypeCode typeCode1 = NumberOps.GetTypeCode(value1);

            if (typeCode1 != TypeCode.String)
            {
                error = UnsupportedOperandType("1st",
                    typeCode1, identifierName, lexeme);

                return ReturnCode.Error;
            }

            object value2 = convert.Value;
            TypeCode typeCode2 = NumberOps.GetTypeCode(value2);

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

        /// <summary>
        /// This method evaluates a list containment operator, testing whether
        /// the underlying string value is (or is not) an element of the list
        /// carried by the specified converter.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier name of the operator, used for error formatting.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme identifying which containment operator to evaluate.
        /// </param>
        /// <param name="convert">
        /// The converter carrying the list operand.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules used when testing for membership.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the boolean result of the containment test.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
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
            TypeCode typeCode1 = NumberOps.GetTypeCode(value1);

            if (typeCode1 != TypeCode.String)
            {
                error = UnsupportedOperandType("1st",
                    typeCode1, identifierName, lexeme);

                return ReturnCode.Error;
            }

            object value2 = convert.Value;
            TypeCode typeCode2 = NumberOps.GetTypeCode(value2);

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
        /// <summary>
        /// This method determines whether the underlying value is a boolean.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a boolean; otherwise, false.
        /// </returns>
        public bool IsBoolean()
        {
            return (value is bool);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a signed
        /// byte.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a signed byte; otherwise, false.
        /// </returns>
        public bool IsSignedByte()
        {
            return (value is sbyte);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a byte.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a byte; otherwise, false.
        /// </returns>
        public bool IsByte()
        {
            return (value is byte);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a narrow
        /// (16-bit) integer.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a narrow integer; otherwise, false.
        /// </returns>
        public bool IsNarrowInteger()
        {
            return (value is short);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an unsigned
        /// narrow (16-bit) integer.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an unsigned narrow integer;
        /// otherwise, false.
        /// </returns>
        public bool IsUnsignedNarrowInteger()
        {
            return (value is ushort);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a character.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a character; otherwise, false.
        /// </returns>
        public bool IsCharacter()
        {
            return (value is char);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an integer.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an integer; otherwise, false.
        /// </returns>
        public bool IsInteger()
        {
            return (value is int);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an unsigned
        /// integer.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an unsigned integer; otherwise,
        /// false.
        /// </returns>
        public bool IsUnsignedInteger()
        {
            return (value is uint);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a wide
        /// (64-bit) integer.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a wide integer; otherwise, false.
        /// </returns>
        public bool IsWideInteger()
        {
            return (value is long);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an unsigned
        /// wide (64-bit) integer.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an unsigned wide integer;
        /// otherwise, false.
        /// </returns>
        public bool IsUnsignedWideInteger()
        {
            return (value is ulong);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method determines whether the underlying value is an
        /// arbitrary-precision big integer.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a big integer; otherwise, false.
        /// </returns>
        public bool IsBigInteger()
        {
            return (value is BigInteger);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a return
        /// code.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a return code; otherwise, false.
        /// </returns>
        public bool IsReturnCode()
        {
            return (value is ReturnCode);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a match
        /// mode.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a match mode; otherwise, false.
        /// </returns>
        public bool IsMatchMode()
        {
            return (value is MatchMode);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a midpoint
        /// rounding mode.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a midpoint rounding mode;
        /// otherwise, false.
        /// </returns>
        public bool IsMidpointRounding()
        {
            return (value is MidpointRounding);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a decimal
        /// (fixed-point) number.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a decimal; otherwise, false.
        /// </returns>
        public bool IsDecimal()
        {
            return (value is decimal);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a
        /// single-precision floating-point number.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a single; otherwise, false.
        /// </returns>
        public bool IsSingle()
        {
            return (value is float);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a
        /// double-precision floating-point number.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a double; otherwise, false.
        /// </returns>
        public bool IsDouble()
        {
            return (value is double);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is of an
        /// integral type.
        /// </summary>
        /// <returns>
        /// True if the underlying value is integral; otherwise, false.
        /// </returns>
        public bool IsIntegral()
        {
            switch (NumberOps.GetTypeCode(value))
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
#if NET_40
                case _TypeCode.BigInteger:
#endif
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an
        /// enumerated value.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an enumerated value; otherwise,
        /// false.
        /// </returns>
        public bool IsEnum()
        {
            return (value is Enum);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is of an
        /// integral type or is an enumerated value.
        /// </summary>
        /// <returns>
        /// True if the underlying value is integral or an enumerated value;
        /// otherwise, false.
        /// </returns>
        public bool IsIntegralOrEnum()
        {
            return IsIntegral() || IsEnum();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is of a
        /// fixed-point type.
        /// </summary>
        /// <returns>
        /// True if the underlying value is fixed-point; otherwise, false.
        /// </returns>
        public bool IsFixedPoint()
        {
            switch (NumberOps.GetTypeCode(value))
            {
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is of a
        /// floating-point type.
        /// </summary>
        /// <returns>
        /// True if the underlying value is floating-point; otherwise, false.
        /// </returns>
        public bool IsFloatingPoint()
        {
            switch (NumberOps.GetTypeCode(value))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a boolean.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted boolean value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToBoolean(
            ref bool value /* out */
            )
        {
            return NumberOps.ToBoolean(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a signed
        /// byte.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted signed byte value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToSignedByte(
            ref sbyte value /* out */
            )
        {
            return NumberOps.ToSignedByte(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a byte.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted byte value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToByte(
            ref byte value /* out */
            )
        {
            return NumberOps.ToByte(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a narrow
        /// (16-bit) integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted narrow integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToNarrowInteger(
            ref short value /* out */
            )
        {
            return NumberOps.ToNarrowInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an unsigned
        /// narrow (16-bit) integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted unsigned narrow integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToUnsignedNarrowInteger(
            ref ushort value /* out */
            )
        {
            return NumberOps.ToUnsignedNarrowInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a
        /// character.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted character value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToCharacter(
            ref char value /* out */
            )
        {
            return NumberOps.ToCharacter(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToInteger(
            ref int value /* out */
            )
        {
            return NumberOps.ToInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an unsigned
        /// integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted unsigned integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToUnsignedInteger(
            ref uint value /* out */
            )
        {
            return NumberOps.ToUnsignedInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a wide
        /// (64-bit) integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted wide integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToWideInteger(
            ref long value /* out */
            )
        {
            return NumberOps.ToWideInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an unsigned
        /// wide (64-bit) integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted unsigned wide integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToUnsignedWideInteger(
            ref ulong value /* out */
            )
        {
            return NumberOps.ToUnsignedWideInteger(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method attempts to convert the underlying value to an
        /// arbitrary-precision big integer.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted big integer value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToBigInteger(
            ref BigInteger value /* out */
            )
        {
            return NumberOps.ToBigInteger(this, null, ref value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a return
        /// code.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted return code value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToReturnCode(
            ref ReturnCode value /* out */
            )
        {
            return NumberOps.ToReturnCode(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a match
        /// mode.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted match mode value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToMatchMode(
            ref MatchMode value /* out */
            )
        {
            return NumberOps.ToMatchMode(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a midpoint
        /// rounding mode.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted midpoint rounding mode value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToMidpointRounding(
            ref MidpointRounding value /* out */
            )
        {
            return NumberOps.ToMidpointRounding(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a decimal
        /// (fixed-point) number.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted decimal value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToDecimal(
            ref decimal value /* out */
            )
        {
            return NumberOps.ToDecimal(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a
        /// single-precision floating-point number.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted single value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToSingle(
            ref float value /* out */
            )
        {
            return NumberOps.ToSingle(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a
        /// double-precision floating-point number.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted double value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToDouble(
            ref double value /* out */
            )
        {
            return NumberOps.ToDouble(this, null, ref value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IVariant Members
        /// <summary>
        /// This method determines whether the underlying value is of any
        /// numeric type.
        /// </summary>
        /// <returns>
        /// True if the underlying value is numeric; otherwise, false.
        /// </returns>
        public bool IsNumber()
        {
            switch (NumberOps.GetTypeCode(value))
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
#if NET_40
                case _TypeCode.BigInteger:
#endif
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

        /// <summary>
        /// This method determines whether the underlying value is a date and
        /// time.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a date and time; otherwise, false.
        /// </returns>
        public bool IsDateTime()
        {
            return (value is DateTime);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a time
        /// interval.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a time interval; otherwise, false.
        /// </returns>
        public bool IsTimeSpan()
        {
            return (value is TimeSpan);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a globally
        /// unique identifier.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a globally unique identifier;
        /// otherwise, false.
        /// </returns>
        public bool IsGuid()
        {
            return (value is Guid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a string.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a string; otherwise, false.
        /// </returns>
        public bool IsString()
        {
            return (value is string);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a list.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a list; otherwise, false.
        /// </returns>
        public bool IsList()
        {
            return (value is StringList);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a dictionary.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a dictionary; otherwise, false.
        /// </returns>
        public bool IsDictionary()
        {
            return (value is StringDictionary);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an opaque
        /// object wrapper.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an opaque object wrapper;
        /// otherwise, false.
        /// </returns>
        public bool IsObject()
        {
            return (value is IObject);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a call frame.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a call frame; otherwise, false.
        /// </returns>
        public bool IsCallFrame()
        {
            return (value is ICallFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an
        /// interpreter.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an interpreter; otherwise, false.
        /// </returns>
        public bool IsInterpreter()
        {
            return (value is Interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a type.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a type; otherwise, false.
        /// </returns>
        public bool IsType()
        {
            return (value is Type);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a list of
        /// types.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a list of types; otherwise, false.
        /// </returns>
        public bool IsTypeList()
        {
            return (value is TypeList);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a list of
        /// enumerated values.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a list of enumerated values;
        /// otherwise, false.
        /// </returns>
        public bool IsEnumList()
        {
            return (value is EnumList);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a uniform
        /// resource identifier.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a uniform resource identifier;
        /// otherwise, false.
        /// </returns>
        public bool IsUri()
        {
            return (value is Uri);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a version.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a version; otherwise, false.
        /// </returns>
        public bool IsVersion()
        {
            return (value is Version);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a list of
        /// return codes.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a list of return codes; otherwise,
        /// false.
        /// </returns>
        public bool IsReturnCodeList()
        {
            return (value is ReturnCodeList);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an alias.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an alias; otherwise, false.
        /// </returns>
        public bool IsAlias()
        {
            return (value is IAlias);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an option.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an option; otherwise, false.
        /// </returns>
        public bool IsOption()
        {
            return (value is IOption);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a namespace.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a namespace; otherwise, false.
        /// </returns>
        public bool IsNamespace()
        {
            return (value is INamespace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a secure
        /// string.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a secure string; otherwise, false.
        /// </returns>
        public bool IsSecureString()
        {
            return (value is SecureString);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a character
        /// encoding.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a character encoding; otherwise,
        /// false.
        /// </returns>
        public bool IsEncoding()
        {
            return (value is Encoding);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a culture.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a culture; otherwise, false.
        /// </returns>
        public bool IsCultureInfo()
        {
            return (value is CultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a plugin.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a plugin; otherwise, false.
        /// </returns>
        public bool IsPlugin()
        {
            return (value is IPlugin);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an
        /// executable entity.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an executable entity; otherwise,
        /// false.
        /// </returns>
        public bool IsExecute()
        {
            return (value is IExecute);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a callback.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a callback; otherwise, false.
        /// </returns>
        public bool IsCallback()
        {
            return (value is ICallback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a rule set.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a rule set; otherwise, false.
        /// </returns>
        public bool IsRuleSet()
        {
            return (value is IRuleSet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is an
        /// identifier.
        /// </summary>
        /// <returns>
        /// True if the underlying value is an identifier; otherwise, false.
        /// </returns>
        public bool IsIdentifier()
        {
            return (value is IIdentifier);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the underlying value is a byte array.
        /// </summary>
        /// <returns>
        /// True if the underlying value is a byte array; otherwise, false.
        /// </returns>
        public bool IsByteArray()
        {
            return (value is byte[]);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a date and
        /// time.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted date and time value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToDateTime(
            ref DateTime value /* out */
            )
        {
            return VariantOps.ToDateTime(
                this, _Value.GetDefaultCulture(), ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a time
        /// interval.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted time interval value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToTimeSpan(
            ref TimeSpan value /* out */
            )
        {
            return VariantOps.ToTimeSpan(
                this, _Value.GetDefaultCulture(), ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a globally
        /// unique identifier.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted globally unique identifier
        /// value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToGuid(
            ref Guid value /* out */
            )
        {
            return VariantOps.ToGuid(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a string.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted string value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToString(
            ref string value /* out */
            )
        {
            return VariantOps.ToString(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a list.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToList(
            ref StringList value /* out */
            )
        {
            return VariantOps.ToList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a
        /// dictionary.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted dictionary value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToDictionary(
            ref StringDictionary value /* out */
            )
        {
            return VariantOps.ToDictionary(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an opaque
        /// object wrapper.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted opaque object wrapper value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToObject(
            ref IObject value /* out */
            )
        {
            return VariantOps.ToObject(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a call
        /// frame.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted call frame value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToCallFrame(
            ref ICallFrame value /* out */
            )
        {
            return VariantOps.ToCallFrame(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an
        /// interpreter.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted interpreter value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToInterpreter(
            ref Interpreter value /* out */
            )
        {
            return VariantOps.ToInterpreter(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a type.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted type value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToType(
            ref Type value /* out */
            )
        {
            return VariantOps.ToType(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a list of
        /// types.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list of types value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToTypeList(
            ref TypeList value /* out */
            )
        {
            return VariantOps.ToTypeList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a list of
        /// enumerated values.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list of enumerated values.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToEnumList(
            ref EnumList value /* out */
            )
        {
            return VariantOps.ToEnumList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a uniform
        /// resource identifier.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted uniform resource identifier
        /// value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToUri(
            ref Uri value /* out */
            )
        {
            return VariantOps.ToUri(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a version.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted version value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToVersion(
            ref Version value /* out */
            )
        {
            return VariantOps.ToVersion(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a list of
        /// return codes.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list of return codes value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToReturnCodeList(
            ref ReturnCodeList value /* out */
            )
        {
            return VariantOps.ToReturnCodeList(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an alias.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted alias value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToAlias(
            ref IAlias value /* out */
            )
        {
            return VariantOps.ToAlias(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an option.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted option value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToOption(
            ref IOption value /* out */
            )
        {
            return VariantOps.ToOption(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a
        /// namespace.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted namespace value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToNamespace(
            ref INamespace value /* out */
            )
        {
            return VariantOps.ToNamespace(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a secure
        /// string.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted secure string value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToSecureString(
            ref SecureString value /* out */
            )
        {
            return VariantOps.ToSecureString(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a character
        /// encoding.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted character encoding value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToEncoding(
            ref Encoding value /* out */
            )
        {
            return VariantOps.ToEncoding(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a culture.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted culture value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToCultureInfo(
            ref CultureInfo value /* out */
            )
        {
            return VariantOps.ToCultureInfo(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a plugin.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted plugin value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToPlugin(
            ref IPlugin value /* out */
            )
        {
            return VariantOps.ToPlugin(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an
        /// executable entity.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted executable entity value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToExecute(
            ref IExecute value /* out */
            )
        {
            return VariantOps.ToExecute(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a callback.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted callback value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToCallback(
            ref ICallback value /* out */
            )
        {
            return VariantOps.ToCallback(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a rule set.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted rule set value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToRuleSet(
            ref IRuleSet value /* out */
            )
        {
            return VariantOps.ToRuleSet(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to an
        /// identifier.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted identifier value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToIdentifier(
            ref IIdentifier value /* out */
            )
        {
            return VariantOps.ToIdentifier(this, null, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the underlying value to a byte
        /// array.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted byte array value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        public bool ToByteArray(
            ref byte[] value /* out */
            )
        {
            return VariantOps.ToByteArray(this, null, ref value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new instance that is a copy of this instance.
        /// </summary>
        /// <returns>
        /// A new instance wrapping the same underlying value as this instance.
        /// </returns>
        public object Clone()
        {
            return new Variant(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is equal to
        /// this instance, by comparing the underlying values.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this instance.
        /// </param>
        /// <returns>
        /// True if the specified object provides a value equal to this
        /// instance's underlying value; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns a hash code for this instance, derived from its
        /// underlying value.
        /// </summary>
        /// <returns>
        /// A hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            return GenericOps<object>.GetHashCode(this.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this instance, formatting
        /// strings, byte arrays, dates, and other supported types as needed.
        /// </summary>
        /// <returns>
        /// The string form of the underlying value.
        /// </returns>
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
