/*
 * VariantOps.cs --
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
using System.Security;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the helper methods used to support "variant"
    /// values, i.e. values that may hold one of a number of well-known managed
    /// types.  It maintains the set of types that are recognized as variant
    /// types and provides methods to query that set as well as to convert an
    /// arbitrary value into one of those specific types.
    /// </summary>
    [ObjectId("633ce5ab-0a38-4324-8aab-8a2ececbe57d")]
    internal static class VariantOps
    {
        #region Private Static Data
        /// <summary>
        /// The object used to synchronize access to the static state
        /// maintained by this class.
        /// </summary>
        private static readonly object syncRoot = new object();
        /// <summary>
        /// Maps each recognized variant type to its associated type code; a
        /// type code of <see cref="TypeCode.Empty" /> indicates that no
        /// specific type code is associated with that type.
        /// </summary>
        private static TypeTypeCodeDictionary types;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Type Management Support
        /// <summary>
        /// This method ensures that the set of recognized variant types has
        /// been created and populated.
        /// </summary>
        public static void InitializeTypes()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                MaybeInitializeTypes(false, ref types);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified dictionary with the set of
        /// recognized variant types and their associated type codes, creating
        /// the dictionary first if necessary.
        /// </summary>
        /// <param name="force">
        /// Non-zero to recreate the dictionary even if it has already been
        /// created.
        /// </param>
        /// <param name="types">
        /// The dictionary to populate; upon return, this contains the
        /// recognized variant types and their associated type codes.
        /// </param>
        public static void MaybeInitializeTypes(
            bool force,                      /* in */
            ref TypeTypeCodeDictionary types /* in, out */
            )
        {
            if (force || (types == null))
                types = new TypeTypeCodeDictionary();

            types[typeof(INumber)] = TypeCode.Empty;
            types[typeof(DateTime)] = TypeCode.DateTime;
            types[typeof(TimeSpan)] = TypeCode.Empty;
            types[typeof(Guid)] = TypeCode.Empty;
            types[typeof(string)] = TypeCode.String;
            types[typeof(StringList)] = TypeCode.Empty;
            types[typeof(StringDictionary)] = TypeCode.Empty;
            types[typeof(IObject)] = TypeCode.Empty;
            types[typeof(ICallFrame)] = TypeCode.Empty;
            types[typeof(Interpreter)] = TypeCode.Empty;
            types[typeof(Type)] = TypeCode.Empty;
            types[typeof(TypeList)] = TypeCode.Empty;
            types[typeof(EnumList)] = TypeCode.Empty;
            types[typeof(Uri)] = TypeCode.Empty;
            types[typeof(Version)] = TypeCode.Empty;
            types[typeof(ReturnCodeList)] = TypeCode.Empty;
            types[typeof(IAlias)] = TypeCode.Empty;
            types[typeof(IOption)] = TypeCode.Empty;
            types[typeof(INamespace)] = TypeCode.Empty;
            types[typeof(SecureString)] = TypeCode.Empty;
            types[typeof(Encoding)] = TypeCode.Empty;
            types[typeof(CultureInfo)] = TypeCode.Empty;
            types[typeof(IPlugin)] = TypeCode.Empty;
            types[typeof(IExecute)] = TypeCode.Empty;
            types[typeof(ICallback)] = TypeCode.Empty;
            types[typeof(IRuleSet)] = TypeCode.Empty;
            types[typeof(IIdentifier)] = TypeCode.Empty;
            types[typeof(byte[])] = TypeCode.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is one of the
        /// recognized variant types.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is a recognized variant type; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the type of the specified value is
        /// one of the recognized variant types.
        /// </summary>
        /// <param name="value">
        /// The value whose type is to be checked.
        /// </param>
        /// <returns>
        /// True if the value's type is a recognized variant type; otherwise,
        /// false.
        /// </returns>
        public static bool HaveType(
            object value /* in */
            )
        {
            Type type = null;

            return HaveType(value, ref type);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the type of the specified value is
        /// one of the recognized variant types, also returning that type.
        /// </summary>
        /// <param name="value">
        /// The value whose type is to be checked.
        /// </param>
        /// <param name="type">
        /// Upon return, this contains the type of the specified value, or null
        /// if it could not be determined.
        /// </param>
        /// <returns>
        /// True if the value's type is a recognized variant type; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified type is a recognized
        /// variant type that has an associated type code.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type has an associated type code; otherwise, false.
        /// </returns>
        public static bool HaveTypeCode(
            Type type /* in */
            )
        {
            TypeCode typeCode = TypeCode.Empty;

            return HaveTypeCode(type, ref typeCode);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is a recognized
        /// variant type and, if so, returns its associated type code.  Any
        /// enumeration type is treated as the <see cref="Enum" /> type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="typeCode">
        /// Upon success, this contains the type code associated with the type.
        /// </param>
        /// <returns>
        /// True if the type is a recognized variant type; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the type of the specified value is a
        /// recognized variant type that has an associated type code.
        /// </summary>
        /// <param name="value">
        /// The value whose type is to be checked.
        /// </param>
        /// <returns>
        /// True if the value's type has an associated type code; otherwise,
        /// false.
        /// </returns>
        public static bool HaveTypeCode(
            object value /* in */
            )
        {
            TypeCode typeCode = TypeCode.Empty;

            return HaveTypeCode(value, ref typeCode);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the type of the specified value is a
        /// recognized variant type and, if so, returns its associated type
        /// code.
        /// </summary>
        /// <param name="value">
        /// The value whose type is to be checked.
        /// </param>
        /// <param name="typeCode">
        /// Upon success, this contains the type code associated with the
        /// value's type.
        /// </param>
        /// <returns>
        /// True if the value's type is a recognized variant type; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method returns the list of recognized variant types.
        /// </summary>
        /// <returns>
        /// A new list containing the recognized variant types, or null if the
        /// set of recognized types has not been initialized.
        /// </returns>
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

        /// <summary>
        /// This method appends the recognized variant types to the specified
        /// list, creating the list first if necessary.
        /// </summary>
        /// <param name="types">
        /// The list to which the recognized variant types are appended; upon
        /// return, this contains those types.
        /// </param>
        /// <returns>
        /// True if the recognized variant types were appended; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified value provider holds a
        /// non-null value and, if so, extracts both the underlying object and
        /// its string representation for use by the conversion methods.
        /// </summary>
        /// <param name="getValue">
        /// The value provider to inspect.
        /// </param>
        /// <param name="objectValue">
        /// Upon success, this contains the underlying object value; otherwise,
        /// it is null.
        /// </param>
        /// <param name="stringValue">
        /// Upon success, this contains the string representation of the
        /// underlying object value; otherwise, it is null.
        /// </param>
        /// <returns>
        /// True if a non-null value was extracted; otherwise, false.
        /// </returns>
        private static bool CanConvert(
            IGetValue getValue,     /* in */
            out object objectValue, /* out */
            out string stringValue  /* out */
            )
        {
            objectValue = null;
            stringValue = null;

            if (getValue == null)
                return false;

            object localObjectValue = getValue.Value;

            if (localObjectValue == null)
                return false;

            string localStringValue;

            if (localObjectValue is string)
            {
                localStringValue = (string)localObjectValue;
            }
            else
            {
                //
                // NOTE: The MSDN documentation seems to suggest
                //       this method cannot throw any exceptions.
                //
                localStringValue = Convert.ToString(localObjectValue);
            }

            objectValue = localObjectValue;
            stringValue = localStringValue;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="DateTime" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use when converting a
        /// convertible value.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted date and time value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToDateTime(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref DateTime value       /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            DateTime dateTime;

            if (objectValue is DateTime)
            {
                value = (DateTime)objectValue;
                return true;
            }
            else if ((objectValue is string) || (objectValue is StringList))
            {
                dateTime = DateTime.MinValue;

                if (Value.TryParseDateTime(
                        stringValue, true, out dateTime))
                {
                    value = dateTime;
                    return true;
                }
            }
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = convertible.ToDateTime(cultureInfo);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use when converting a
        /// convertible value.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted time interval value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToTimeSpan(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in */
            ref TimeSpan value       /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is TimeSpan)
            {
                value = (TimeSpan)objectValue;
                return true;
            }
            else if ((objectValue is string) || (objectValue is StringList))
            {
                TimeSpan timeSpan = TimeSpan.Zero;

                if (TimeSpan.TryParse(
                        stringValue, out timeSpan))
                {
                    value = timeSpan;
                    return true;
                }
            }
            else
            {
                IConvertible convertible = objectValue as IConvertible;

                if (convertible != null)
                {
                    value = new TimeSpan(
                        convertible.ToInt64(cultureInfo));

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="Guid" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted globally unique
        /// identifier value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToGuid(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref Guid value           /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is Guid)
            {
                value = (Guid)objectValue;
                return true;
            }
            else if (stringValue != null)
            {
                try
                {
                    value = new Guid(stringValue); /* throw */
                    return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a string.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted string value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToString(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref string value         /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            value = stringValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="StringList" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted list value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToList(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref StringList value     /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is StringList)
                value = (StringList)objectValue;
            else
                value = new StringList(new string[] { stringValue });

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="StringDictionary" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted dictionary value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToDictionary(
            IGetValue getValue,        /* in */
            CultureInfo cultureInfo,   /* in: NOT USED */
            ref StringDictionary value /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is StringDictionary)
            {
                value = (StringDictionary)getValue.Value;
            }
            else if (objectValue is StringList)
            {
                value = new StringDictionary(
                    (StringList)objectValue, false, true);
            }
            else
            {
                value = new StringDictionary(
                    new string[] { stringValue }, false, true);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="IObject" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted object value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToObject(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref IObject value        /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is IObject)
            {
                value = (IObject)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="ICallFrame" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted call frame value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToCallFrame(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref ICallFrame value     /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is ICallFrame)
            {
                value = (ICallFrame)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="Interpreter" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted interpreter value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToInterpreter(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref Interpreter value    /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is Interpreter)
            {
                value = (Interpreter)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="Type" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted type value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToType(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref Type value           /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is Type)
            {
                value = (Type)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="TypeList" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted type list value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToTypeList(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref TypeList value       /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is TypeList)
            {
                value = (TypeList)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="EnumList" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted enumeration list value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToEnumList(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref EnumList value       /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is EnumList)
            {
                value = (EnumList)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="Uri" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted uniform resource
        /// identifier value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToUri(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref Uri value            /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is Uri)
            {
                value = (Uri)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="Version" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted version value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToVersion(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref Version value        /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is Version)
            {
                value = (Version)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="ReturnCodeList" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted return code list value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToReturnCodeList(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref ReturnCodeList value /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is ReturnCodeList)
            {
                value = (ReturnCodeList)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="IAlias" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted alias value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToAlias(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref IAlias value         /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is IAlias)
            {
                value = (IAlias)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="IOption" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted option value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToOption(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref IOption value        /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is IOption)
            {
                value = (IOption)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="INamespace" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted namespace value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToNamespace(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref INamespace value     /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is INamespace)
            {
                value = (INamespace)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="SecureString" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted secure string value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToSecureString(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref SecureString value   /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is SecureString)
            {
                value = (SecureString)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="Encoding" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted encoding value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToEncoding(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref Encoding value       /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is Encoding)
            {
                value = (Encoding)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a <see cref="CultureInfo" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted culture value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToCultureInfo(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref CultureInfo value    /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is CultureInfo)
            {
                value = (CultureInfo)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="IPlugin" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted plugin value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToPlugin(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref IPlugin value        /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is IPlugin)
            {
                value = (IPlugin)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="IExecute" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted executable value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToExecute(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref IExecute value       /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is IExecute)
            {
                value = (IExecute)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="ICallback" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted callback value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToCallback(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref ICallback value      /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is ICallback)
            {
                value = (ICallback)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="IRuleSet" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted rule set value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToRuleSet(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref IRuleSet value       /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is IRuleSet)
            {
                value = (IRuleSet)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into an <see cref="IIdentifier" />.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted identifier value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToIdentifier(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref IIdentifier value    /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is IIdentifier)
            {
                value = (IIdentifier)objectValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the value held by the specified
        /// value provider into a byte array.
        /// </summary>
        /// <param name="getValue">
        /// The value provider whose value is to be converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific information; this parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the converted byte array value.
        /// </param>
        /// <returns>
        /// True if the value was converted successfully; otherwise, false.
        /// </returns>
        public static bool ToByteArray(
            IGetValue getValue,      /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            ref byte[] value         /* out */
            )
        {
            object objectValue;
            string stringValue;

            if (!CanConvert(getValue, out objectValue, out stringValue))
                return false;

            if (objectValue is byte[])
            {
                value = (byte[])objectValue;
                return true;
            }

            return false;
        }
        #endregion
    }
}
