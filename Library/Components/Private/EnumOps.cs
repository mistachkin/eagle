/*
 * EnumOps.cs --
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

#if DATA
using System.Data;
#endif

using System.Globalization;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using EnumCacheDictionary = Eagle._Containers.Private.TypePairDictionary<
    Eagle._Containers.Public.StringList, Eagle._Containers.Public.UlongList>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a centralized collection of helper methods for
    /// working with enumerated types.  It supports parsing strings into
    /// enumeration values (including flag enumerations and integer values),
    /// converting enumeration values to their underlying integral values,
    /// manipulating flag enumeration values via operators, and caching the
    /// name and value metadata for enumerated types.
    /// </summary>
    [ObjectId("32db1eb0-d7c8-4a31-82bf-215ae3d9086d")]
    internal static class EnumOps
    {
        #region Private Constants
#if DATA
        //
        // NOTE: This integer value is used with CommandBehavior values.
        //       When set, extra SQLiteDataReader.Read calls are not
        //       performed within the ExecuteScalar methods for write
        //       transactions.  This value should be used with extreme
        //       care because it can cause unusual behavior.  It is
        //       intended for use only by legacy applications that rely
        //       on the old, incorrect behavior.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// A special integer value used together with CommandBehavior values
        /// that, when set, suppresses extra SQLiteDataReader.Read calls within
        /// the ExecuteScalar methods for write transactions.
        /// </summary>
        private static CommandBehavior SkipExtraReads =
            (CommandBehavior)0x10000000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This integer value is used with CommandBehavior values.
        //       When set, extra SQLiteDataReader.Read calls are performed
        //       within the ExecuteScalar() methods for all transactions.
        //       This value should be used with extreme care because it
        //       can cause unusual behavior.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// A special integer value used together with CommandBehavior values
        /// that, when set, forces extra SQLiteDataReader.Read calls to be
        /// performed within the ExecuteScalar methods for all transactions.
        /// </summary>
        public const CommandBehavior ForceExtraReads =
            (CommandBehavior)0x20000000;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only to allow for ad-hoc
        //       "customization" (i.e. via a script using something
        //       like [object invoke -flags +NonPublic]).
        //
        // NOTE: The default value here is designed to be compatible
        //       with the .NET Framework (internal) semantics for the
        //       treatment of enumerated values as integer values.
        //
        /// <summary>
        /// When non-zero, a null enumeration value, value type, or type is
        /// treated as a wide (64-bit) integer; otherwise, it is not.
        /// </summary>
        private static bool TreatNullAsWideInteger = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, a boolean string is permitted as an integer value
        /// when parsing enumeration values; otherwise, it is not.
        /// </summary>
        private static bool TreatBooleanAsInteger = false; // COMPAT: Eagle.

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // HACK: This is purposely not read-only.
        //
        // NOTE: When non-zero, the [generic] built-in Enum.TryParse
        //       method provided by the .NET Framework 4.0 and later
        //       will be used; otherwise, the legacy TryParseFast
        //       method will be used.
        //
        /// <summary>
        /// When non-zero, the generic built-in Enum.TryParse method provided
        /// by the .NET Framework 4.0 and later is used; otherwise, the legacy
        /// TryParseFast method is used.
        /// </summary>
        private static bool UseBuiltInTryParse = true;
#endif

        ///////////////////////////////////////////////////////////////////////

#if !NET_40 && !MONO && NET_20_FAST_ENUM
        //
        // HACK: This is purposely not read-only.
        //
        // NOTE: When non-zero, the private Enum.InternalGetEnumValues
        //       method will be used to obtain the lists of names and
        //       values for an enumerated type.  Since this does not
        //       work reliably (i.e. it can cause stability and issues
        //       as well as intermittently returning the wrong result),
        //       it is disabled by default.
        //
        /// <summary>
        /// When non-zero, the private Enum.InternalGetEnumValues method is used
        /// to obtain the lists of names and values for an enumerated type;
        /// otherwise, it is not.  This is disabled by default because it does
        /// not work reliably.
        /// </summary>
        private static bool UseInternalGetValues = false;

        //
        // NOTE: Used by the "GetNamesAndValuesInternal" method.
        //
        /// <summary>
        /// The name of the private Enum.InternalGetEnumValues method used by
        /// the GetNamesAndValuesInternal method.
        /// </summary>
        private const string GetValuesMethodName = "InternalGetEnumValues";
#endif

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// The name of the generic Enum.TryParse method used when locating it
        /// via reflection.
        /// </summary>
        private const string TryParseMethodName = "TryParse";
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags operator character used to select an enumeration table.
        /// </summary>
        internal const char SelectTableOperator = Characters.Slash;

        /// <summary>
        /// The flags operator character used to add flag bits.
        /// </summary>
        internal const char AddFlagOperator = Characters.PlusSign;

        /// <summary>
        /// The flags operator character used to remove flag bits.
        /// </summary>
        internal const char RemoveFlagOperator = Characters.MinusSign;

        /// <summary>
        /// The flags operator character used to set the overall value equal to
        /// the specified value.
        /// </summary>
        internal const char SetFlagOperator = Characters.EqualSign;

        /// <summary>
        /// The flags operator character used to set the overall value equal to
        /// the specified value and then switch to adding flag bits.
        /// </summary>
        internal const char SetAddFlagOperator = Characters.Colon;

        /// <summary>
        /// The flags operator character used to keep (bitwise 'AND') flag bits.
        /// </summary>
        internal const char KeepFlagOperator = Characters.Ampersand;

        /// <summary>
        /// The default flags operator character to use when none is specified.
        /// </summary>
        internal static readonly char DefaultFlagOperator = SetAddFlagOperator;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default string of permitted flags operator characters to use
        /// when an empty operator string is specified.
        /// </summary>
        private static string DefaultFlagOperators = AddFlagOperator.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cache Data
        #region TryParse MethodInfo
#if NET_40
        /// <summary>
        /// The object used to synchronize access to the cached TryParse method
        /// information.
        /// </summary>
        private static object tryParseSyncRoot = new object();

        /// <summary>
        /// The cached generic Enum.TryParse method information, prior to being
        /// constructed for a specific enumeration type.
        /// </summary>
        private static MethodInfo tryParse = null;

        /// <summary>
        /// The cache of TryParse method information, keyed by enumeration type,
        /// each already constructed for its specific enumeration type.
        /// </summary>
        private static Dictionary<Type, MethodInfo> tryParseCache;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Names / Values
        /// <summary>
        /// The object used to synchronize access to the cache of enumeration
        /// names and values.
        /// </summary>
        private static object cacheSyncRoot = new object();

        /// <summary>
        /// The cache of enumeration names and values, keyed by enumeration
        /// type.
        /// </summary>
        private static EnumCacheDictionary cache = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cache Methods
        /// <summary>
        /// This method clears all cached enumeration data, including the cached
        /// names and values and the cached TryParse method information.
        /// </summary>
        /// <returns>
        /// The total number of cache entries that were removed.
        /// </returns>
        public static int ClearCache()
        {
            int result = ClearNamesAndValuesCache();

#if NET_40
            result += ClearTryParseCache();
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the cache of enumeration names and values.
        /// </summary>
        /// <returns>
        /// The number of cache entries that were removed, or
        /// <see cref="Count.Invalid" /> if the cache was not allocated.
        /// </returns>
        private static int ClearNamesAndValuesCache()
        {
            lock (cacheSyncRoot) /* TRANSACTIONAL */
            {
                if (cache == null)
                    return Count.Invalid;

                int result = cache.Count;

                cache.Clear();
                cache = null;

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method clears the cached TryParse method information.
        /// </summary>
        /// <returns>
        /// The number of cache entries that were removed.
        /// </returns>
        private static int ClearTryParseCache()
        {
            lock (tryParseSyncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (tryParse != null)
                {
                    result++;

                    tryParse = null;
                }

                if (tryParseCache != null)
                {
                    result += tryParseCache.Count;

                    tryParseCache.Clear();
                    tryParseCache = null;
                }

                return result;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method gets the enumerated value of a named field or property
        /// of an object, which is expected to be a flags enumeration.
        /// </summary>
        /// <param name="object">
        /// The object whose field or property value is to be obtained.
        /// </param>
        /// <param name="memberName">
        /// The name of the field or property to obtain.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match the member name in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value of the named member, or null if it could not be
        /// obtained.
        /// </returns>
        public static Enum GetFlags(
            object @object,    /* in */
            string memberName, /* in */
            bool noCase,       /* in */
            ref Result error   /* out */
            )
        {
            if (@object == null)
            {
                error = "invalid object";
                return null;
            }

            Type type = @object.GetType();

            if (type == null)
            {
                error = "invalid type";
                return null;
            }

            try
            {
                MemberTypes memberTypes = ObjectOps.GetMemberTypes(
                    MetaMemberTypes.FlagsEnum, true);

                BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                    MetaBindingFlags.FlagsEnum, true);

                if (noCase)
                    bindingFlags |= BindingFlags.IgnoreCase;

                MemberInfo[] memberInfos = type.GetMember(
                    memberName, memberTypes, bindingFlags);

                if (memberInfos == null)
                {
                    error = String.Format(
                        "missing members matching {0} for type {1}",
                        FormatOps.WrapOrNull(memberName),
                        MarshalOps.GetErrorTypeName(type));

                    return null;
                }

                int length = memberInfos.Length;

                if (length <= 0)
                {
                    error = String.Format(
                        "no member ({0}) matching {1} for type {2}",
                        length, FormatOps.WrapOrNull(memberName),
                        MarshalOps.GetErrorTypeName(type));

                    return null;
                }
                else if (length > 1)
                {
                    error = String.Format(
                        "ambiguous members ({0}) matching {1} for type {2}",
                        length, FormatOps.WrapOrNull(memberName),
                        MarshalOps.GetErrorTypeName(type));

                    return null;
                }

                MemberInfo memberInfo = memberInfos[0];
                FieldInfo fieldInfo = memberInfo as FieldInfo;

                if (fieldInfo != null)
                {
                    Type fieldType = fieldInfo.FieldType;

                    if ((fieldType == null) || !fieldType.IsEnum)
                    {
                        error = String.Format(
                            "field {0} for type {1} is not an enumeration: {2}",
                            MarshalOps.GetErrorMemberName(fieldInfo),
                            MarshalOps.GetErrorTypeName(type),
                            MarshalOps.GetErrorTypeName(fieldType));

                        return null;
                    }

                    object fieldValue = fieldInfo.GetValue(@object);

                    if (!(fieldValue is Enum))
                    {
                        error = String.Format(
                            "field {0} for type {1} value is not an enumeration: {2}",
                            MarshalOps.GetErrorMemberName(fieldInfo),
                            MarshalOps.GetErrorTypeName(type),
                            FormatOps.WrapOrNull(fieldValue));

                        return null;
                    }

                    return (Enum)fieldValue;
                }

                PropertyInfo propertyInfo = memberInfo as PropertyInfo;

                if (propertyInfo == null)
                {
                    error = String.Format(
                        "member {0} for type {1} is not a field or property",
                        MarshalOps.GetErrorMemberName(memberInfo),
                        MarshalOps.GetErrorTypeName(type));

                    return null;
                }

                Type propertyType = propertyInfo.PropertyType;

                if ((propertyType == null) || !propertyType.IsEnum)
                {
                    error = String.Format(
                        "property {0} for type {1} is not an enumeration: {2}",
                        MarshalOps.GetErrorMemberName(propertyInfo),
                        MarshalOps.GetErrorTypeName(type),
                        MarshalOps.GetErrorTypeName(propertyType));

                    return null;
                }

                object propertyValue = propertyInfo.GetValue(@object, null);

                if (!(propertyValue is Enum))
                {
                    error = String.Format(
                        "property {0} for type {1} value is not an enumeration: {2}",
                        MarshalOps.GetErrorMemberName(propertyInfo),
                        MarshalOps.GetErrorTypeName(type),
                        FormatOps.WrapOrNull(propertyValue));

                    return null;
                }

                return (Enum)propertyValue;
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is an enumerated
        /// type that has the FlagsAttribute applied to it.
        /// </summary>
        /// <param name="enumType">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is a flags enumeration; otherwise, false.
        /// </returns>
        public static bool IsFlags(
            Type enumType /* in */
            )
        {
            if ((enumType == null) || !enumType.IsEnum)
                return false;

            return enumType.IsDefined(typeof(FlagsAttribute), false);
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        /// <summary>
        /// This method converts a value to the specified enumerated type and
        /// then to its underlying integral value, also reporting the type code
        /// of that underlying value.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to convert the value to.
        /// </param>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="typeCode">
        /// Upon success, this parameter will be modified to contain the type
        /// code of the underlying integral value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The underlying integral value of the converted enumerated value, or
        /// null if it could not be obtained.
        /// </returns>
        public static object ConvertToTypeCodeValue(
            Type enumType,           /* in */
            object value,            /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            ref TypeCode typeCode,   /* out */
            ref Result error         /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            object enumValue = TryGet(enumType, value, ref error);

            if (enumValue == null)
                return null;

            IConvertible convertible = enumValue as IConvertible;

            if (convertible == null)
            {
                error = String.Format(
                    "enumerated type {0} value is not convertible",
                    FormatOps.TypeName(enumType));

                return null;
            }

            try
            {
                typeCode = convertible.GetTypeCode();

                switch (typeCode)
                {
                    case TypeCode.Boolean: /* signed, based on int */
                        return convertible.ToBoolean(cultureInfo);
                    case TypeCode.Char:
                        return convertible.ToChar(cultureInfo);
                    case TypeCode.SByte:
                        return convertible.ToSByte(cultureInfo);
                    case TypeCode.Byte:
                        return convertible.ToByte(cultureInfo);
                    case TypeCode.Int16:
                        return convertible.ToInt16(cultureInfo);
                    case TypeCode.UInt16:
                        return convertible.ToUInt16(cultureInfo);
                    case TypeCode.Int32:
                        return convertible.ToInt32(cultureInfo);
                    case TypeCode.UInt32:
                        return convertible.ToUInt32(cultureInfo);
                    case TypeCode.Int64:
                        return convertible.ToInt64(cultureInfo);
                    case TypeCode.UInt64:
                        return convertible.ToUInt64(cultureInfo);
                    default:
                        {
                            error = String.Format(
                                "enumerated type {0} value {1} " +
                                "has unsupported type code {2}",
                                FormatOps.TypeName(enumType),
                                FormatOps.WrapOrNull(enumValue),
                                FormatOps.WrapOrNull(typeCode));

                            break;
                        }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the value of the specified
        /// enumerated type that corresponds to the specified underlying value.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to obtain the value as.
        /// </param>
        /// <param name="value">
        /// The underlying value to convert into an enumerated value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryGet(
            Type enumType,   /* in */
            object value,    /* in */
            ref Result error /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            try
            {
                //
                // NOTE: Try to get the value as the specified enumeration
                //       type.
                //
                return Enum.ToObject(enumType, value);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a leading or trailing "None" or "Default" name
        /// (and its associated separator) from the string representation of an
        /// enumerated value.
        /// </summary>
        /// <param name="value">
        /// The enumeration string value to fix up.
        /// </param>
        /// <returns>
        /// The fixed up enumeration string value.
        /// </returns>
        public static string FixupEnumString( /* CORE */
            string value /* in */
            )
        {
            string result = value;
            int length;

            if (StringOps.IsNullOrEmpty(result, out length))
                return result;

            const string separator = ", ";
            string[] names = { "None", "Default" };

            foreach (string name in names)
            {
                int nameLength;

                if (StringOps.IsNullOrEmpty(name, out nameLength))
                    continue;

                if (nameLength >= length) /* NOTE: Efficiency. */
                    continue;

                string prefix = String.Format("{0}{1}", name, separator);
                int prefixLength = prefix.Length;

                if ((prefixLength < length) &&
                    SharedStringOps.SystemStartsWith(result, prefix))
                {
                    result = result.Substring(prefixLength);
                }

                string suffix = String.Format("{0}{1}", separator, name);
                int suffixLength = suffix.Length;

                if ((suffixLength < length) &&
                    SharedStringOps.SystemEndsWith(result, suffix))
                {
                    result = result.Substring(0, length - suffixLength);
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Support Methods
        /// <summary>
        /// This method determines whether the leading non-identifier character
        /// of the specified value should be ignored when parsing it.
        /// </summary>
        /// <param name="value">
        /// The value being parsed.
        /// </param>
        /// <param name="ignoreLeading">
        /// Non-zero to consider ignoring the leading non-identifier character.
        /// </param>
        /// <returns>
        /// True if the leading character should be ignored; otherwise, false.
        /// </returns>
        private static bool ShouldIgnoreLeading(
            string value,      /* in */
            bool ignoreLeading /* in */
            )
        {
            if ((value == null) || !ignoreLeading)
                return false;

            int length = value.Length;

            if (length < 2)
                return false;

            char firstCharacter = value[0];
            char secondCharacter = value[1];

            if (Parser.IsIdentifier(firstCharacter))
                return false;

            if (!Parser.IsIdentifier(secondCharacter))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified enumerated value should
        /// be treated as a wide (64-bit) integer, based on its underlying type.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value should be treated as a wide integer; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldTreatAsWideInteger(
            Enum value /* in */
            )
        {
            if (value == null)
                return TreatNullAsWideInteger; /* NOTE: Per framework classes. */

            Type type = value.GetType();

            if ((type == null) || !type.IsEnum)
                return false;

            return ShouldTreatAsWideInteger(Enum.GetUnderlyingType(type));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value should be treated
        /// as a wide (64-bit) integer, based on its type.
        /// </summary>
        /// <param name="value">
        /// The value to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value should be treated as a wide integer; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldTreatAsWideInteger(
            object value /* in */
            )
        {
            if (value == null)
                return TreatNullAsWideInteger; /* NOTE: Per framework classes. */

            Type type = value.GetType();

            if ((type == null) || !type.IsValueType)
                return false;

            return ShouldTreatAsWideInteger(type);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether values of the specified type should
        /// be treated as a wide (64-bit) integer.
        /// </summary>
        /// <param name="type">
        /// The type to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the type should be treated as a wide integer; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldTreatAsWideInteger(
            Type type /* in */
            )
        {
            if (type == null)
                return TreatNullAsWideInteger; /* NOTE: Per framework classes. */

            return (type == typeof(long)) || (type == typeof(ulong));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces the common flag delimiter characters (comma,
        /// pipe, and semicolon) within a flags string with spaces so that it
        /// may be parsed as a list.
        /// </summary>
        /// <param name="value">
        /// The flags string value to fix up.
        /// </param>
        /// <returns>
        /// The fixed up flags string value.
        /// </returns>
        private static string FixupFlagsString(
            string value /* in */
            )
        {
            string result = value;

            if (!String.IsNullOrEmpty(result))
            {
                char[] characters = {
                    Characters.Comma, Characters.Pipe, Characters.SemiColon
                };

                if (result.IndexOfAny(characters) != Index.Invalid)
                {
                    StringBuilder builder = StringBuilderFactory.Create(
                        result);

                    int length = characters.Length;

                    for (int index = 0; index < length; index++)
                    {
                        builder = builder.Replace(
                            characters[index], Characters.Space);
                    }

                    result = StringBuilderCache.GetStringAndRelease(
                        ref builder);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method commits new enumeration names and values into the
        /// caller-supplied name and value lists, either by appending to the
        /// existing lists, reusing the supplied collections, or copying them.
        /// </summary>
        /// <param name="enumNames">
        /// Upon return, this parameter will contain the combined enumeration
        /// names.
        /// </param>
        /// <param name="enumValues">
        /// Upon return, this parameter will contain the combined enumeration
        /// values.
        /// </param>
        /// <param name="newEnumNames">
        /// The new enumeration names to commit.  This parameter may be null.
        /// </param>
        /// <param name="newEnumValues">
        /// The new enumeration values to commit.  This parameter may be null.
        /// </param>
        /// <param name="forceCopy">
        /// Non-zero to force the new names and values to be copied into new
        /// lists instead of being reused directly.
        /// </param>
        private static void CommitNamesAndValues(
            ref StringList enumNames,         /* in, out */
            ref UlongList enumValues,         /* in, out */
            IEnumerable<string> newEnumNames, /* in */
            IEnumerable<ulong> newEnumValues, /* in */
            bool forceCopy                    /* in */
            )
        {
            if (newEnumNames != null)
            {
                if (enumNames != null)
                    enumNames.AddRange(newEnumNames);
                else if (!forceCopy && (newEnumNames is StringList))
                    enumNames = (StringList)newEnumNames;
                else
                    enumNames = new StringList(newEnumNames);
            }

            if (newEnumValues != null)
            {
                if (enumValues != null)
                    enumValues.AddRange(newEnumValues);
                else if (!forceCopy && (newEnumValues is UlongList))
                    enumValues = (UlongList)newEnumValues;
                else
                    enumValues = new UlongList(newEnumValues);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an error message indicating that an invalid
        /// flags operator character was encountered.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type being parsed.
        /// </param>
        /// <param name="operator">
        /// The invalid flags operator character.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
        private static string BadFlagsOperatorError(
            Type enumType,
            char @operator
            )
        {
            return String.Format(
                "bad {0} flags operator {1}, must " +
                "be {2}, {3}, {4}, {5}, {6}, or {7}",
                FormatOps.TypeName(enumType),
                FormatOps.WrapOrNull(@operator),
                FormatOps.WrapOrNull(SelectTableOperator),
                FormatOps.WrapOrNull(AddFlagOperator),
                FormatOps.WrapOrNull(RemoveFlagOperator),
                FormatOps.WrapOrNull(SetFlagOperator),
                FormatOps.WrapOrNull(SetAddFlagOperator),
                FormatOps.WrapOrNull(KeepFlagOperator));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an error message indicating that an invalid
        /// enumeration value was encountered, including the list of valid
        /// names.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type being parsed.
        /// </param>
        /// <param name="enumName">
        /// The invalid enumeration name or value.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
        private static string BadValueError(
            Type enumType,
            string enumName
            )
        {
            return ScriptOps.BadValue(
                null, String.Format("{0} value", FormatOps.TypeName(
                enumType)), enumName, Enum.GetNames(enumType), null,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an error message indicating that the number of
        /// enumeration names does not match the number of enumeration values.
        /// </summary>
        /// <param name="count1">
        /// The number of enumeration names.
        /// </param>
        /// <param name="count2">
        /// The number of enumeration values.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
        private static string CountMismatchError(
            int count1,
            int count2
            )
        {
            return String.Format(
                "count mismatch, enumeration names {0} " +
                "versus enumeration values {1}", count1, count2);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private TryParse* Methods
        /// <summary>
        /// This method attempts to parse a string as an integer value (signed
        /// or unsigned) and convert it into a value of the specified
        /// enumerated type, verifying that the round-trip conversion matches.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseInteger(
            Type enumType,           /* in */
            string value,            /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            try
            {
                uint[] uintValue = { 0, 0 };

                if (Value.GetUnsignedInteger2(
                        value, ValueFlags.AnyWideInteger |
                        ValueFlags.Unsigned, cultureInfo, ref uintValue[0],
                        ref error) == ReturnCode.Ok)
                {
                    object enumValue = TryGet(
                        enumType, uintValue[0], ref error);

                    if (enumValue != null)
                    {
                        //
                        // BUGFIX: Verify that the returned Enum value
                        //         is numerically identical to the parsed
                        //         unsigned integer.  This makes things a
                        //         bit slower; however, it is necessary.
                        //
                        uintValue[1] = ToUInt(
                            enumType, enumValue as Enum, cultureInfo);

                        if ((enumValue != null) &&
                            (enumValue.GetType() == enumType) &&
                            (uintValue[0] == uintValue[1]))
                        {
                            return enumValue;
                        }
                        else
                        {
                            error = String.Format(
                                "bad {0}, unsigned integer value {1} " +
                                "(parsed from {2}), does not match " +
                                "converted unsigned integer value {3}",
                                FormatOps.TypeName(enumType),
                                FormatOps.WrapOrNull(uintValue[0]),
                                FormatOps.WrapOrNull(value),
                                FormatOps.WrapOrNull(uintValue[1]));
                        }
                    }
                }
                else
                {
                    int[] intValue = { 0, 0 };

                    if (Value.GetInteger2(
                            value, ValueFlags.AnyWideInteger, cultureInfo,
                            ref intValue[0], ref error) == ReturnCode.Ok)
                    {
                        object enumValue = TryGet(
                            enumType, intValue[0], ref error);

                        if (enumValue != null)
                        {
                            //
                            // BUGFIX: Verify that the returned Enum value
                            //         is numerically identical to the parsed
                            //         signed integer.  This makes things a
                            //         bit slower; however, it is necessary.
                            //
                            intValue[1] = ToInt(
                                enumType, enumValue as Enum, cultureInfo);

                            if ((enumValue != null) &&
                                (enumValue.GetType() == enumType) &&
                                (intValue[0] == intValue[1]))
                            {
                                return enumValue;
                            }
                            else
                            {
                                error = String.Format(
                                    "bad {0}, integer value {1} " +
                                    "(parsed from {2}), does not match " +
                                    "converted integer value {3}",
                                    FormatOps.TypeName(enumType),
                                    FormatOps.WrapOrNull(intValue[0]),
                                    FormatOps.WrapOrNull(value),
                                    FormatOps.WrapOrNull(intValue[1]));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string as a wide (64-bit) integer
        /// value (signed or unsigned) and convert it into a value of the
        /// specified enumerated type, verifying that the round-trip conversion
        /// matches.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseWideInteger(
            Type enumType,           /* in */
            string value,            /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            try
            {
                ulong[] ulongValue = { 0, 0 };

                if (Value.GetUnsignedWideInteger2(value,
                        ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                        cultureInfo, ref ulongValue[0],
                        ref error) == ReturnCode.Ok)
                {
                    object enumValue = TryGet(
                        enumType, ulongValue[0], ref error);

                    if (enumValue != null)
                    {
                        //
                        // BUGFIX: Verify that the returned Enum value
                        //         is numerically identical to the parsed
                        //         unsigned long integer.  This makes
                        //         things a bit slower; however, it is
                        //         necessary.
                        //
                        ulongValue[1] = ToULong(
                            enumType, enumValue as Enum, cultureInfo);

                        if ((enumValue != null) &&
                            (enumValue.GetType() == enumType) &&
                            (ulongValue[0] == ulongValue[1]))
                        {
                            return enumValue;
                        }
                        else
                        {
                            error = String.Format(
                                "bad {0}, unsigned wide integer value " +
                                "{1} (parsed from {2}), does not match " +
                                "converted unsigned wide integer value {3}",
                                FormatOps.TypeName(enumType),
                                FormatOps.WrapOrNull(ulongValue[0]),
                                FormatOps.WrapOrNull(value),
                                FormatOps.WrapOrNull(ulongValue[1]));
                        }
                    }
                }
                else
                {
                    long[] longValue = { 0, 0 };

                    if (Value.GetWideInteger2(
                            value, ValueFlags.AnyWideInteger, cultureInfo,
                            ref longValue[0], ref error) == ReturnCode.Ok)
                    {
                        object enumValue = TryGet(
                            enumType, longValue[0], ref error);

                        if (enumValue != null)
                        {
                            //
                            // BUGFIX: Verify that the returned Enum value
                            //         is numerically identical to the parsed
                            //         signed long integer.  This makes things
                            //         a bit slower; however, it is necessary.
                            //
                            longValue[1] = ToLong(
                                enumType, enumValue as Enum, cultureInfo);

                            if ((enumValue != null) &&
                                (enumValue.GetType() == enumType) &&
                                (longValue[0] == longValue[1]))
                            {
                                return enumValue;
                            }
                            else
                            {
                                error = String.Format(
                                    "bad {0}, wide integer value {1} " +
                                    "(parsed from {2}), does not match " +
                                    "converted wide integer value {3}",
                                    FormatOps.TypeName(enumType),
                                    FormatOps.WrapOrNull(longValue[0]),
                                    FormatOps.WrapOrNull(value),
                                    FormatOps.WrapOrNull(longValue[1]));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string as a boolean value and
        /// convert it into a value of the specified enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseBooleanOnly(
            Type enumType,          /* in */
            string value,           /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            )
        {
            Result error = null;

            return TryParseBooleanOnly(
                enumType, value, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string as a boolean value and
        /// convert it into a value of the element type of the specified
        /// enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseBooleanOnly(
            Type enumType,           /* in */
            string value,            /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            Type elementType = null;

            if (!MarshalOps.IsEnumType(
                    enumType, true, true, ref elementType))
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            if ((elementType == null) || !elementType.IsEnum)
            {
                error = String.Format(
                    "element type {0} is not an enumeration",
                    FormatOps.TypeName(elementType));

                return null;
            }

            bool boolValue = false;

            if (!Value.TryParseBooleanOnly(
                    value, ValueFlags.AnyBoolean, ref boolValue))
            {
                error = String.Format(
                    "expected boolean but got {0}",
                    FormatOps.WrapOrNull(value));

                return null;
            }

            return TryGet(elementType,
                ConversionOps.ToInt(boolValue), ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string as some kind of integer
        /// value and convert it into a value of the specified enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseSomeKindOfInteger(
            Type enumType,          /* in */
            string value,           /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            )
        {
            Result error = null;

            return TryParseSomeKindOfInteger(
                enumType, value, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string as some kind of integer
        /// value and convert it into a value of the element type of the
        /// specified enumerated type, choosing between wide integer and
        /// integer parsing based on the underlying type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseSomeKindOfInteger(
            Type enumType,           /* in */
            string value,            /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            Type elementType = null;

            if (!MarshalOps.IsEnumType(
                    enumType, true, true, ref elementType))
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            if ((elementType == null) || !elementType.IsEnum)
            {
                error = String.Format(
                    "element type {0} is not an enumeration",
                    FormatOps.TypeName(elementType));

                return null;
            }

            if (ShouldTreatAsWideInteger(
                    Enum.GetUnderlyingType(elementType)))
            {
                return TryParseWideInteger(
                    elementType, value, cultureInfo, ref error);
            }
            else
            {
                return TryParseInteger(
                    elementType, value, cultureInfo, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a convertible value into an unsigned long
        /// (64-bit) integer, treating it as signed or unsigned based on its
        /// type code.
        /// </summary>
        /// <param name="convertible">
        /// The convertible value to convert.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The unsigned long integer value, or zero if the value is null or its
        /// type code is not supported.
        /// </returns>
        private static ulong ConvertibleToULong(
            IConvertible convertible, /* in */
            CultureInfo cultureInfo   /* in: OPTIONAL */
            )
        {
            if (convertible != null)
            {
                TypeCode typeCode = convertible.GetTypeCode();

                switch (typeCode)
                {
                    case TypeCode.Boolean: /* signed, based on int */
                        {
                            return ConversionOps.ToULong(
                                convertible.ToBoolean(cultureInfo));
                        }
                    case TypeCode.SByte:
                        {
                            return ConversionOps.ToULong(
                                convertible.ToSByte(cultureInfo));
                        }
                    case TypeCode.Int16:
                        {
                            return ConversionOps.ToULong(
                                convertible.ToInt16(cultureInfo));
                        }
                    case TypeCode.Int32:
                        {
                            return ConversionOps.ToULong(
                                convertible.ToInt32(cultureInfo));
                        }
                    case TypeCode.Int64:
                        {
                            return ConversionOps.ToULong(
                                convertible.ToInt64(cultureInfo));
                        }
                    case TypeCode.Byte:
                    case TypeCode.Char:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        {
                            return convertible.ToUInt64(cultureInfo);
                        }
                }
            }

            return 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public TryParse Methods
        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParse(
            Type enumType,     /* in */
            string value,      /* in */
            bool allowInteger, /* in */
            bool noCase        /* in */
            )
        {
            Result error = null;

            return TryParse(
                enumType, value, allowInteger, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParse( /* OVERLOAD */
            Type enumType,     /* in */
            string value,      /* in */
            bool allowInteger, /* in */
            bool noCase,       /* in */
            ref Result error   /* out */
            )
        {
            return TryParse(
                enumType, value, allowInteger, true, true, noCase,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type, dispatching to either the built-in or fast parsing
        /// implementation.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="ignoreLeading">
        /// Non-zero to ignore a leading non-identifier character when matching
        /// enumeration names.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParse( /* OVERLOAD */
            Type enumType,        /* in */
            string value,         /* in */
            bool allowInteger,    /* in */
            bool ignoreLeading,   /* in */
            bool errorOnNotFound, /* in */
            bool noCase,          /* in */
            ref Result error      /* out */
            )
        {
#if NET_40
            if (UseBuiltInTryParse)
            {
                return TryParseBuiltIn(
                    enumType, value, allowInteger, ignoreLeading,
                    errorOnNotFound, noCase, ref error);
            }
#endif

            return TryParseFast(
                enumType, value, allowInteger, ignoreLeading,
                errorOnNotFound, noCase, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private TryParse Methods
        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type, using the supplied name and value lists.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="enumNames">
        /// The list of valid enumeration names.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="enumValues">
        /// The list of valid enumeration values.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParse( /* INTERNAL */
            Type enumType,        /* in */
            string value,         /* in */
            StringList enumNames, /* in: MAYBE OPTIONAL */
            UlongList enumValues, /* in: MAYBE OPTIONAL */
            bool allowInteger,    /* in */
            bool noCase,          /* in */
            ref Result error      /* out */
            )
        {
            return TryParse(
                enumType, value, enumNames, enumValues,
                allowInteger, true, true, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type, using the supplied name and value lists, and
        /// dispatching to either the built-in or fast parsing implementation.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="enumNames">
        /// The list of valid enumeration names.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="enumValues">
        /// The list of valid enumeration values.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="ignoreLeading">
        /// Non-zero to ignore a leading non-identifier character when matching
        /// enumeration names.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParse( /* OVERLOAD */
            Type enumType,        /* in */
            string value,         /* in */
            StringList enumNames, /* in: MAYBE OPTIONAL */
            UlongList enumValues, /* in: MAYBE OPTIONAL */
            bool allowInteger,    /* in */
            bool ignoreLeading,   /* in */
            bool errorOnNotFound, /* in */
            bool noCase,          /* in */
            ref Result error      /* out */
            )
        {
#if NET_40
            if (UseBuiltInTryParse)
            {
                return TryParseBuiltIn(
                    enumType, value, allowInteger, ignoreLeading,
                    errorOnNotFound, noCase, ref error);
            }
#endif

            return TryParseFast(
                enumType, value, enumNames, enumValues, allowInteger,
                ignoreLeading, errorOnNotFound, noCase, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private TryParseBuiltIn Methods (NetFx 4.0)
#if NET_40
        /// <summary>
        /// This method verifies that the parameters of a candidate generic
        /// Enum.TryParse method match the expected signature: a string, a
        /// boolean, and a reference to a generic value-type constrained type.
        /// </summary>
        /// <param name="parameterInfos">
        /// The parameter information for the candidate method.
        /// </param>
        /// <returns>
        /// True if the parameters match the expected signature; otherwise,
        /// false.
        /// </returns>
        private static bool CheckTryParseParameterInfos(
            ParameterInfo[] parameterInfos /* in */
            )
        {
            //
            // NOTE: There must be exactly 3 parameters.  The first must be a
            //       string, the second must be a boolean, and the final one
            //       must be a reference to a generic ValueType constrainted
            //       type.
            //
            if ((parameterInfos == null) || (parameterInfos.Length != 3))
                return false;

            if (parameterInfos[0].ParameterType != typeof(string))
                return false;

            if (parameterInfos[1].ParameterType != typeof(bool))
                return false;

            if (!parameterInfos[2].ParameterType.IsByRef ||
                !parameterInfos[2].ParameterType.ContainsGenericParameters)
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates, via reflection, the generic Enum.TryParse
        /// method with the expected signature, caching it for subsequent use.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The method information for the generic Enum.TryParse method, or null
        /// if it could not be found.
        /// </returns>
        private static MethodInfo GetTryParseMethodInfo(
            ref Result error /* out */
            )
        {
            lock (tryParseSyncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    if (tryParse != null)
                    {
                        //
                        // NOTE: Return the existing (cached) method object.
                        //
                        return tryParse;
                    }

                    MethodInfo[] methodInfos = typeof(Enum).GetMethods(
                        ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PublicStaticMethod, true));

                    if (methodInfos == null)
                        goto error;

                    int length = methodInfos.Length;

                    for (int index = 0; index < length; index++)
                    {
                        MethodInfo methodInfo = methodInfos[index];

                        if (methodInfo == null)
                            continue;

                        if (String.Compare(
                                methodInfo.Name, TryParseMethodName,
                                SharedStringOps.SystemComparisonType) != 0)
                        {
                            continue;
                        }

                        if (CheckTryParseParameterInfos(
                                methodInfo.GetParameters()))
                        {
                            tryParse = methodInfo;
                            return tryParse;
                        }
                    }

                error:

                    error = String.Format(
                        "cannot find {0} method of {1} type",
                        FormatOps.WrapOrNull(TryParseMethodName),
                        FormatOps.WrapOrNull(typeof(Enum)));
                }
                catch (Exception e)
                {
                    error = e;
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the generic Enum.TryParse method constructed for
        /// the specified enumerated type, caching the constructed method for
        /// subsequent use.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to construct the generic method for.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The constructed method information for the Enum.TryParse method, or
        /// null if it could not be obtained.
        /// </returns>
        private static MethodInfo GetTryParseMethodInfo(
            Type enumType,   /* in */
            ref Result error /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            lock (tryParseSyncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    if (tryParseCache == null)
                        tryParseCache = new Dictionary<Type, MethodInfo>();

                    MethodInfo methodInfo;

                    if (tryParseCache.TryGetValue(
                            enumType, out methodInfo) && (methodInfo != null))
                    {
                        //
                        // NOTE: Return cached TryParse method for enumeration.
                        //
                        return methodInfo;
                    }
                    else
                    {
                        methodInfo = GetTryParseMethodInfo(ref error);

                        if (methodInfo == null)
                            return null;

                        //
                        // NOTE: Construct TryParse method with enumeration
                        //       type we have been passed.
                        //
                        methodInfo = methodInfo.MakeGenericMethod(enumType);

                        if (methodInfo == null)
                        {
                            error = String.Format(
                                "type {0} cannot make generic {1} method",
                                FormatOps.TypeName(enumType),
                                FormatOps.WrapOrNull(TryParseMethodName));

                            return null;
                        }

                        //
                        // NOTE: Cache TryParse method and then return it.
                        //
                        tryParseCache[enumType] = methodInfo;

                        return methodInfo;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type using the generic built-in Enum.TryParse method
        /// provided by the .NET Framework 4.0 and later.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="ignoreLeading">
        /// Non-zero to ignore a leading non-identifier character when matching
        /// enumeration names.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.  This
        /// parameter is not used.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseBuiltIn(
            Type enumType,        /* in */
            string value,         /* in */
            bool allowInteger,    /* in */
            bool ignoreLeading,   /* in */
            bool errorOnNotFound, /* in: NOT USED */
            bool noCase,          /* in */
            ref Result error      /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            if (String.IsNullOrEmpty(value))
            {
                error = String.Format(
                    "invalid {0} value",
                    FormatOps.TypeName(enumType));

                return null;
            }

            //
            // HACK: This call assumes that no "field" of an enumerated type
            //       can begin with a digit, plus sign, or minus sign.  This
            //       is always true in C# because the fields are identifiers
            //       and no identifier in C# may begin with anything except
            //       a letter or underscore.  However, this may not be true
            //       for other languages built on the CLR.
            //
            object enumValue = null; /* REUSED */
            Result integerError = null;
            Result booleanError = null;

            if (allowInteger)
            {
                if (Parser.IsInteger(value[0], true))
                {
                    enumValue = TryParseSomeKindOfInteger(
                        enumType, value, null, ref integerError);

                    if (enumValue != null)
                        return enumValue;
                }

                if (TreatBooleanAsInteger &&
                    Parser.IsBoolean(value[0]))
                {
                    enumValue = TryParseBooleanOnly(
                        enumType, value, null, ref booleanError);

                    if (enumValue != null)
                        return enumValue;
                }
            }

            //
            // NOTE: Attempt to obtain the necessary TryParse method for the
            //       specified enumeration type.  If this fails, we cannot
            //       continue.
            //
            MethodInfo methodInfo = GetTryParseMethodInfo(
                enumType, ref error);

            if (methodInfo == null)
                return null;

            try
            {
                //
                // NOTE: Try to parse the name without the leading character
                //       if it is not an identifier character.
                //
                if (ShouldIgnoreLeading(value, ignoreLeading))
                    value = value.Substring(1);

                //
                // NOTE: We expect the [generic] Enum.TryParse method to accept
                //       exactly 3 arguments.  First argument must be a string.
                //       Second argument must be a boolean.  Third argument is
                //       supposed to be a ByRef generic enumerated type.
                //
                object[] args = { value, noCase, null };

                //
                // NOTE: Attempt to invoke the TryParse method that has been
                //       fully constructed for the proper enumeration type.
                //
                bool? success = methodInfo.Invoke(null, args) as bool?;

                //
                // NOTE: Make sure the method returned a boolean and that it
                //       has a non-zero value.
                //
                if ((success != null) && (bool)success)
                {
                    //
                    // NOTE: Success, extract third argument as enumeration
                    //       valued parsed result.
                    //
                    enumValue = args[2]; /* NOTE: Cannot fail (?). */
                }
                else
                {
                    error = BadValueError(enumType, value);
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            if (enumValue == null)
            {
                error = ResultOps.MaybeCombine(
                    error, integerError, booleanError);
            }

            return enumValue;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region GetNamesAndValues Methods (NetFx 2.0/3.5/4.0, and Mono)
        /// <summary>
        /// This method determines whether the enumeration names and values need
        /// to be obtained prior to parsing, which is not necessary when the
        /// built-in TryParse method is being used.
        /// </summary>
        /// <returns>
        /// True if the names and values should be obtained; otherwise, false.
        /// </returns>
        private static bool ShouldGetNamesAndValues()
        {
#if NET_40
            if (UseBuiltInTryParse)
                return false;
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the lists of names and underlying values for the
        /// specified enumerated type, dispatching to the internal or slow
        /// implementation as appropriate.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to obtain the names and values for.
        /// </param>
        /// <param name="enumNames">
        /// Upon return, this parameter will contain the enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// Upon return, this parameter will contain the enumeration values.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetNamesAndValues(
            Type enumType,            /* in */
            ref StringList enumNames, /* in, out */
            ref UlongList enumValues, /* in, out */
            ref Result error          /* out */
            )
        {
#if !NET_40 && !MONO && NET_20_FAST_ENUM
            if (UseInternalGetValues)
            {
                return GetNamesAndValuesInternal(
                    enumType, ref enumNames, ref enumValues, ref error);
            }
#endif

            return GetNamesAndValuesSlow(
                enumType, ref enumNames, ref enumValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_40 && !MONO && NET_20_FAST_ENUM
        /// <summary>
        /// This method obtains the lists of names and underlying values for the
        /// specified enumerated type by invoking the private
        /// Enum.InternalGetEnumValues method via reflection, falling back on
        /// the slow implementation if necessary.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to obtain the names and values for.
        /// </param>
        /// <param name="enumNames">
        /// Upon return, this parameter will contain the enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// Upon return, this parameter will contain the enumeration values.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode GetNamesAndValuesInternal(
            Type enumType,            /* in */
            ref StringList enumNames, /* in, out */
            ref UlongList enumValues, /* in, out */
            ref Result error          /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return ReturnCode.Error;
            }

            int retry = 0;

        fallback:

            if (!CommonOps.Runtime.IsMono() &&
                (retry == 0) && (enumType.BaseType == typeof(Enum)))
            {
                try
                {

                retry:

                    //
                    // HACK: This method is private in the .NET Framework and
                    //       we need to use it; therefore, we use reflection
                    //       to invoke it.
                    //
                    // BUGBUG: Something inside this method occasionally throws
                    //         System.ExecutionEngineException.  It appears to
                    //         be some kind of race condition with the GC.
                    //
                    object[] args = { enumType, null, null };

                    typeof(Enum).InvokeMember(
                        GetValuesMethodName, ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStaticMethod, true), null,
                        null, args); /* throw */

                    //
                    // BUGBUG: Why is this required?  The above call seems to
                    //         intermittently fail on our custom Boolean enum
                    //         type; however, the operation sometimes succeeds
                    //         upon being retried?
                    //
                    if ((retry < 1) && ((args == null) || (args[1] == null) ||
                        (args[2] == null)))
                    {
                        retry++;

                        goto retry;
                    }

                    //
                    // HACK: If we still fail, fallback on the known-good
                    //       method.
                    //
                    if ((args == null) ||
                        (args[1] == null) || (args[2] == null))
                    {
                        retry++;

                        goto fallback;
                    }

                    CommitNamesAndValues(
                        ref enumNames, ref enumValues, (string[])args[2],
                        (ulong[])args[1], false);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
            {
                //
                // NOTE: *FALLBACK* In case the fast method does not work.
                //       Even though the InternalGetEnumValues does fail on
                //       occasion, it does not seem to do so repeatedly;
                //       therefore, this code path should be rarely used when
                //       not running on Mono.
                //
                // TraceOps.DebugTrace(String.Format(
                //     "GetNamesAndValuesInternal: fallback, enumType = {0}",
                //     FormatOps.WrapOrNull(enumType)), typeof(EnumOps).Name,
                //     TracePriority.EnumDebug);
                //
                return GetNamesAndValuesSlow(
                    enumType, ref enumNames, ref enumValues, ref error);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the lists of names and underlying values for the
        /// specified enumerated type by reflecting over its public static
        /// fields, caching the result for subsequent use.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to obtain the names and values for.
        /// </param>
        /// <param name="enumNames">
        /// Upon return, this parameter will contain the enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// Upon return, this parameter will contain the enumeration values.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode GetNamesAndValuesSlow(
            Type enumType,            /* in */
            ref StringList enumNames, /* in, out */
            ref UlongList enumValues, /* in, out */
            ref Result error          /* out */
            )
        {
            lock (cacheSyncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    if (cache == null)
                        cache = new EnumCacheDictionary();

                retry:

                    IAnyPair<StringList, UlongList> anyPair;

                    if (cache.TryGetValue(enumType, out anyPair))
                    {
                        if (anyPair == null)
                        {
                            //
                            // NOTE: This cache entry is missing?  Try to
                            //       remove it from the cache and just
                            //       fetch the names/values for this enum
                            //       type again.
                            //
                            cache.Remove(enumType);

                            goto retry;
                        }

                        StringList names = anyPair.X;
                        UlongList values = anyPair.Y;

                        if ((names == null) || (values == null))
                        {
                            //
                            // NOTE: This cache entry is corrupted?  Try
                            //       to remove it from the cache and just
                            //       fetch the names/values for this enum
                            //       type again.
                            //
                            cache.Remove(enumType);

                            goto retry;
                        }

                        CommitNamesAndValues(
                            ref enumNames, ref enumValues, names, values,
                            false);
                    }
                    else
                    {
                        PairList<object> pairs = new PairList<object>();

                        //
                        // NOTE: Get all the static public fields, these
                        //       are the values.
                        //
                        FieldInfo[] fieldInfos = enumType.GetFields(
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.EnumField, true));

                        if (fieldInfos != null)
                        {
                            foreach (FieldInfo fieldInfo in fieldInfos)
                            {
                                //
                                // NOTE: Add the name and the value itself
                                //       to the list (via our Pair object).
                                //
                                pairs.Add(new ObjectPair(fieldInfo.Name,
                                    fieldInfo.GetValue(null)));
                            }
                        }

                        //
                        // NOTE: Sort the list based on the underlying
                        //       integral values.
                        //
                        pairs.Sort(new _Comparers.Pair<object>(
                            PairComparison.LYRY, true)); /* throw */

                        //
                        // NOTE: Populate the result lists.
                        //
                        StringList names = new StringList();
                        UlongList values = new UlongList();

                        foreach (Pair<object> pair in pairs)
                        {
                            names.Add((string)pair.X); /* throw */

                            values.Add(ToULong(
                                enumType, (Enum)pair.Y, null)); /* throw */
                        }

                        //
                        // NOTE: Commit changes to the variables provided
                        //       by the caller.
                        //
                        CommitNamesAndValues(
                            ref enumNames, ref enumValues, names, values,
                            false);

                        //
                        // NOTE: Save in the cache for the next usage (this
                        //       is especially important for commonly used
                        //       enum types like Boolean).
                        //
                        cache.Add(enumType,
                            new AnyPair<StringList, UlongList>(names, values));
                    }

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private TryParseFast Methods (NetFx 2.0/3.5, and Mono)
        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type using the legacy fast parsing implementation,
        /// obtaining the enumeration names and values as necessary.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="ignoreLeading">
        /// Non-zero to ignore a leading non-identifier character when matching
        /// enumeration names.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseFast(
            Type enumType,        /* in */
            string value,         /* in */
            bool allowInteger,    /* in */
            bool ignoreLeading,   /* in */
            bool errorOnNotFound, /* in */
            bool noCase,          /* in */
            ref Result error      /* out */
            )
        {
            StringList enumNames = null;
            UlongList enumValues = null;

            if (ShouldGetNamesAndValues() && GetNamesAndValues(
                    enumType, ref enumNames, ref enumValues,
                    ref error) != ReturnCode.Ok)
            {
                return null;
            }

            return TryParseFast(
                enumType, value, enumNames, enumValues, allowInteger,
                ignoreLeading, errorOnNotFound, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the specified
        /// enumerated type using the legacy fast parsing implementation and the
        /// supplied name and value lists.  The string may consist of multiple
        /// comma-separated names and/or integer values to be combined.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="enumNames">
        /// The list of valid enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// The list of valid enumeration values.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="ignoreLeading">
        /// Non-zero to ignore a leading non-identifier character when matching
        /// enumeration names.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseFast(
            Type enumType,        /* in */
            string value,         /* in */
            StringList enumNames, /* in */
            UlongList enumValues, /* in */
            bool allowInteger,    /* in */
            bool ignoreLeading,   /* in */
            bool errorOnNotFound, /* in */
            bool noCase,          /* in */
            ref Result error      /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            if (String.IsNullOrEmpty(value))
            {
                error = String.Format(
                    "invalid {0} value",
                    FormatOps.TypeName(enumType));

                return null;
            }

            if (enumNames == null)
            {
                error = "invalid enumeration names";
                return null;
            }

            if (enumValues == null)
            {
                error = "invalid enumeration values";
                return null;
            }

            int count1 = enumNames.Count;
            int count2 = enumValues.Count;

            if (count1 != count2)
            {
                error = CountMismatchError(count1, count2);
                return null;
            }

            //
            // HACK: This call assumes that no "field" of an enumerated type
            //       can begin with a digit, plus sign, or minus sign.  This
            //       is always true in C# because the fields are identifiers
            //       and no identifier in C# may begin with anything except
            //       a letter or underscore.  However, this may not be true
            //       for other languages built on the CLR.
            //
            object enumValue = null; /* REUSED */
            Result integerError = null;
            Result booleanError = null;

            if (allowInteger)
            {
                if (Parser.IsInteger(value[0], true))
                {
                    enumValue = TryParseSomeKindOfInteger(
                        enumType, value, null, ref integerError);

                    if (enumValue != null)
                        return enumValue;
                }

                if (TreatBooleanAsInteger &&
                    Parser.IsBoolean(value[0]))
                {
                    enumValue = TryParseBooleanOnly(
                        enumType, value, null, ref booleanError);

                    if (enumValue != null)
                        return enumValue;
                }
            }

            //
            // NOTE: Break string into multiple values, if necessary.
            //
            string[] localValues = value.Split(Characters.Comma);

            if (localValues == null)
            {
                error = String.Format(
                    "could not parse {0} value {1}",
                    FormatOps.WrapOrNull(enumType),
                    FormatOps.WrapOrNull(value));

                return null;
            }

            //
            // NOTE: This unsigned long integer will be used to determine
            //       the final result (i.e. because the string value may
            //       consist of multiple values, separated by commas).
            //
            ulong newValue = 0;

            //
            // NOTE: Figure out how to compare name strings.  This will be
            //       ordinal matching.
            //
            StringComparison comparisonType =
                SharedStringOps.GetSystemComparisonType(noCase);

            foreach (string localValue in localValues)
            {
                //
                // NOTE: Skip over null and empty entries.
                //
                if (String.IsNullOrEmpty(localValue))
                    continue;

                //
                // NOTE: Grab the current item and clean it.
                //
                string enumName = localValue.Trim();

                //
                // NOTE: Skip over entries that consist of only whitespace.
                //
                if (String.IsNullOrEmpty(enumName))
                    continue;

                //
                // HACK: *NEW* Support having an integer within the "list" of
                //       entries to process.  As stated above, this relies on
                //       "fields" of an enumerated type being unable to begin
                //       with a digit, plus sign, or minus sign.  Also, the
                //       built-in enumeration value parser provided by .NET
                //       4.0 does *not* support this syntax.
                //
                if (allowInteger)
                {
                    IConvertible convertible; /* REUSED */

                    if (Parser.IsInteger(enumName[0], true))
                    {
                        convertible = TryParseSomeKindOfInteger(
                            enumType, enumName, null) as IConvertible;

                        if (convertible != null)
                        {
                            newValue |= ConvertibleToULong(convertible, null);
                            continue;
                        }
                    }

                    if (TreatBooleanAsInteger &&
                        Parser.IsBoolean(enumName[0]))
                    {
                        convertible = TryParseBooleanOnly(
                            enumType, enumName, null) as IConvertible;

                        if (convertible != null)
                        {
                            newValue |= ConvertibleToULong(convertible, null);
                            continue;
                        }
                    }
                }

                //
                // NOTE: Try to find the name in the list of valid ones for
                //       this enumerated type.
                //
                int index = enumNames.IndexOf(enumName, 0, comparisonType);

                //
                // NOTE: Try to find the name without the leading character
                //       if it is not an identifier character.
                //
                if ((index == Index.Invalid) &&
                    ShouldIgnoreLeading(enumName, ignoreLeading))
                {
                    index = enumNames.IndexOf(
                        enumName.Substring(1), 0, comparisonType);
                }

                //
                // NOTE: Did we find the name in the list of valid ones for
                //       this enumerated type?
                //
                if (index != Index.Invalid)
                {
                    //
                    // NOTE: Found it.  Combine the underlying value with
                    //       our result so far.
                    //
                    newValue |= enumValues[index];
                }
                else if (errorOnNotFound)
                {
                    error = BadValueError(enumType, enumName);
                    goto error;
                }
            }

            enumValue = TryGet(enumType, newValue, ref error);

        error:

            if (enumValue == null)
            {
                error = ResultOps.MaybeCombine(
                    error, integerError, booleanError);
            }

            return enumValue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
        #region Private TryParseSlow Methods (Obsolete)
#if DEAD_CODE
        /// <summary>
        /// This method attempts to parse the specified value as a member of
        /// the specified enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value against.
        /// </param>
        /// <param name="value">
        /// The value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// When non-zero, integer values are permitted.
        /// </param>
        /// <param name="ignoreLeading">
        /// When non-zero, a leading non-identifier character may be ignored
        /// when the value cannot otherwise be parsed.
        /// </param>
        /// <param name="errorOnNotFound">
        /// When non-zero, a failure to find a matching member is treated as an
        /// error.
        /// </param>
        /// <param name="noCase">
        /// When non-zero, the value is matched without regard to case.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The parsed enumerated value, or null if the value could not be
        /// parsed.
        /// </returns>
        [Obsolete()]
        private static object TryParseSlow( /* NOT USED */
            Type enumType,        /* in */
            string value,         /* in */
            bool allowInteger,    /* in: NOT USED */
            bool ignoreLeading,   /* in */
            bool errorOnNotFound, /* in: NOT USED */
            bool noCase,          /* in */
            ref Result error      /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            if (String.IsNullOrEmpty(value))
            {
                error = BadValueError(enumType, value);
                return null;
            }

            try
            {
                //
                // NOTE: First, try for an exact match.
                //
                // NOTE: No TryParse for enumerations, eh?
                //
                return Enum.Parse(
                    enumType, value, noCase); /* throw */
            }
            catch
            {
                if (ShouldIgnoreLeading(value, ignoreLeading))
                {
                    try
                    {
                        //
                        // NOTE: Ok, now try to remove the leading
                        //       non-identifier character.
                        //
                        // NOTE: No TryParse for enumerations, eh?
                        //
                        return Enum.Parse(
                            enumType, value.Substring(1),
                            noCase); /* throw */
                    }
                    catch
                    {
                        // do nothing.
                    }
                }
            }

            return null;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private ToUIntOrULong Methods
        /// <summary>
        /// This method converts a value into either an unsigned integer or an
        /// unsigned long integer, depending on the size of its underlying type.
        /// </summary>
        /// <param name="value">
        /// The value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned value, or null if it could not be converted.
        /// </returns>
        private static ulong? ToUIntOrULong(
            object value /* in */
            ) /* SAFE */
        {
            return ToUIntOrULong(value, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a value into either an unsigned integer or an
        /// unsigned long integer, depending on the size of its underlying type.
        /// </summary>
        /// <param name="value">
        /// The value to convert.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned value, or null if it could not be converted.
        /// </returns>
        private static ulong? ToUIntOrULong(
            object value,           /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
            if (value is Enum)
                return ToUIntOrULong((Enum)value, cultureInfo);

            return ShouldTreatAsWideInteger(value) ?
                ConversionOps.ToULong(value, cultureInfo) :
                ConversionOps.ToUInt(value, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into either an unsigned
        /// integer or an unsigned long integer, depending on the size of its
        /// underlying type.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned value.
        /// </returns>
        private static ulong ToUIntOrULong(
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
            return ShouldTreatAsWideInteger(value) ?
                ToULong(value, cultureInfo) :
                ToUInt(value, cultureInfo);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private ToInt / ToUInt Methods
        /// <summary>
        /// This method converts an enumerated value into a signed integer,
        /// possibly losing information if the underlying value does not fit.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted signed integer value.
        /// </returns>
        private static int ToInt(
            Type enumType,          /* in */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* LOSSY */
        {
            return ConversionOps.ToInt(ToLong(enumType, value, cultureInfo));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned integer,
        /// possibly losing information if the underlying value does not fit.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned integer value.
        /// </returns>
        private static uint ToUInt(
            Type enumType,          /* in */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* LOSSY */
        {
            return ConversionOps.ToUInt(ToULong(enumType, value, cultureInfo));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned integer,
        /// possibly losing information if the underlying value does not fit.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned integer value.
        /// </returns>
        private static uint ToUInt(
            Enum value /* in */
            ) /* LOSSY */
        {
            return ToUInt(
                (value != null) ? value.GetType() : null, value, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned integer,
        /// possibly losing information if the underlying value does not fit.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to convert.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned integer value.
        /// </returns>
        private static uint ToUInt(
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* LOSSY */
        {
            return ToUInt(
                (value != null) ? value.GetType() : null, value,
                cultureInfo);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private ToLong / ToULong Methods
#if !MONO
        /// <summary>
        /// This method converts an enumerated value into a signed long (64-bit)
        /// integer using the fast (reflection-based) implementation.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted signed long integer value.
        /// </returns>
        private static long ToLongFast(
            Type enumType,          /* in */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
            return ConversionOps.ToLong(ToULongFast(
                enumType, value, cultureInfo)); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned long
        /// (64-bit) integer using the fast implementation, which invokes the
        /// private Enum.ToUInt64 method via reflection.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.  This parameter is not used.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter is not used.
        /// </param>
        /// <returns>
        /// The converted unsigned long integer value.
        /// </returns>
        private static ulong ToULongFast(
            Type enumType,          /* in: NOT USED */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: NOT USED */
            ) /* SAFE */
        {
            //
            // HACK: This method is private in the .NET Framework and we
            //       need to use it; therefore, we use reflection to
            //       invoke it.
            //
            return (ulong)typeof(Enum).InvokeMember("ToUInt64",
                ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PrivateStaticMethod, true),
                null, null, new object[] { value }); /* throw */
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into a signed long (64-bit)
        /// integer using the slow (non-reflection-based) implementation.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted signed long integer value.
        /// </returns>
        private static long ToLongSlow(
            Type enumType,          /* in */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
            //
            // NOTE: Use the routine for converting to an unsigned long
            //       integer and then safely convert it to a signed long
            //       integer.
            //
            return ConversionOps.ToLong(
                ToULongSlow(enumType, value, cultureInfo));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned long
        /// (64-bit) integer using the slow implementation, which switches on
        /// the underlying type code of the value.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.  This parameter is not used.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned long integer value.
        /// </returns>
        private static ulong ToULongSlow(
            Type enumType,          /* in: NOT USED */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
            TypeCode typeCode = NumberOps.GetTypeCode(value);

            switch (typeCode)
            {
                case TypeCode.Boolean: /* signed, based on int */
                    {
                        return ConversionOps.ToULong(Convert.ToBoolean(
                            value, cultureInfo));
                    }
                case TypeCode.SByte:
                    {
                        return ConversionOps.ToULong(Convert.ToSByte(
                            value, cultureInfo));
                    }
                case TypeCode.Int16:
                    {
                        return ConversionOps.ToULong(Convert.ToInt16(
                            value, cultureInfo));
                    }
                case TypeCode.Int32:
                    {
                        return ConversionOps.ToULong(Convert.ToInt32(
                            value, cultureInfo));
                    }
                case TypeCode.Int64:
                    {
                        return ConversionOps.ToULong(Convert.ToInt64(
                            value, cultureInfo));
                    }
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        return Convert.ToUInt64(value, cultureInfo);
                    }
                default:
                    {
                        //
                        // NOTE: We have no idea what this is, punt.
                        //
                        throw new ScriptException(String.Format(
                            "type mismatch, type code {0} is not supported",
                            FormatOps.WrapOrNull(typeCode)));
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into a signed long (64-bit)
        /// integer, dispatching to the fast or slow implementation depending on
        /// the runtime.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted signed long integer value.
        /// </returns>
        private static long ToLong(
            Type enumType,          /* in */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
                return ToLongFast(enumType, value, cultureInfo);
            else
#endif
                return ToLongSlow(enumType, value, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned long
        /// (64-bit) integer, dispatching to the fast or slow implementation
        /// depending on the runtime.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the value.
        /// </param>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned long integer value.
        /// </returns>
        private static ulong ToULong(
            Type enumType,          /* in */
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
                return ToULongFast(enumType, value, cultureInfo);
            else
#endif
                return ToULongSlow(enumType, value, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned long
        /// (64-bit) integer.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned long integer value.
        /// </returns>
        private static ulong ToULong(
            Enum value /* in */
            ) /* SAFE */
        {
            return ToULong(
                (value != null) ? value.GetType() : null, value, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an enumerated value into an unsigned long
        /// (64-bit) integer.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to convert.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted unsigned long integer value.
        /// </returns>
        private static ulong ToULong(
            Enum value,             /* in */
            CultureInfo cultureInfo /* in: OPTIONAL */
            ) /* SAFE */
        {
            return ToULong(
                (value != null) ? value.GetType() : null, value,
                cultureInfo);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public ToLong / ToULong Methods
        /// <summary>
        /// This method converts an enumerated value into a signed long (64-bit)
        /// integer.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The converted signed long integer value.
        /// </returns>
        public static long ToLong(
            Enum value /* in */
            ) /* SAFE */
        {
            return ToLong(
                (value != null) ? value.GetType() : null, value, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public ToUIntOrULong Methods
        //
        // TODO: *HACK* What is the true purpose of this method?  What test
        //       cases will reveal why it is necessary?
        //
        /// <summary>
        /// This method converts an enumerated value into either an unsigned
        /// integer or an unsigned long integer, depending on the size of its
        /// underlying type.
        /// </summary>
        /// <param name="value">
        /// The enumerated value to convert.
        /// </param>
        /// <returns>
        /// The converted unsigned value.
        /// </returns>
        public static ulong ToUIntOrULong(
            Enum value /* in */
            ) /* SAFE */
        {
            return ToUIntOrULong(value, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private TryParseFlags Methods
        /// <summary>
        /// This method validates that every character in the specified operator
        /// string is a valid flags operator, substituting the default operators
        /// when an empty string is supplied.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type being parsed.
        /// </param>
        /// <param name="operators">
        /// The string of operator characters to validate.  Upon return, this
        /// parameter will be trimmed or replaced with the default operators.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if all operator characters are valid; otherwise, false.
        /// </returns>
        private static bool AreFlagsOperators(
            Type enumType,        /* in */
            ref string operators, /* in, out */
            ref Result error      /* out */
            )
        {
            if (operators == null)
            {
                error = "invalid operator string";
                return false;
            }

            int length = operators.Length;

            if (length > 0)
            {
                operators = operators.Trim();
                length = operators.Length;
            }
            else
            {
                operators = DefaultFlagOperators;
            }

            for (int index = 0; index < length; index++)
            {
                char @operator = operators[index];

                switch (@operator)
                {
                    case SelectTableOperator:
                    case AddFlagOperator:
                    case RemoveFlagOperator:
                    case SetFlagOperator:
                    case SetAddFlagOperator:
                    case KeepFlagOperator:
                        {
                            //
                            // NOTE: Operator is valid, do nothing.
                            //
                            break;
                        }
                    default:
                        {
                            //
                            // NOTE: Any other operator character
                            //       is invalid.
                            //
                            error = BadFlagsOperatorError(
                                enumType, @operator);

                            return false;
                        }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified flag values are
        /// entirely unmasked, that is, none of their bits fall outside of the
        /// specified mask values.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type being parsed.
        /// </param>
        /// <param name="flagValues">
        /// The flag values to check.
        /// </param>
        /// <param name="maskValues">
        /// The mask values defining the permitted bits.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if all flag value bits are within the mask; otherwise, false.
        /// </returns>
        private static bool AreFlagsValuesUnmasked(
            Type enumType,           /* in */
            object flagValues,       /* in */
            object maskValues,       /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (!(flagValues is Enum))
            {
                error = String.Format(
                    "bad {0} flags values {1}: not an enumeration",
                    FormatOps.TypeName(enumType),
                    FormatOps.WrapOrNull(flagValues));

                return false;
            }

            if (!(maskValues is Enum))
            {
                error = String.Format(
                    "bad {0} mask values {1}: not an enumeration",
                    FormatOps.TypeName(enumType),
                    FormatOps.WrapOrNull(maskValues));

                return false;
            }

            ulong flagUlongValues = ToULong(
                enumType, (Enum)flagValues, cultureInfo); /* throw */

            ulong maskUlongValues = ToULong(
                enumType, (Enum)maskValues, cultureInfo); /* throw */

            flagUlongValues &= ~maskUlongValues;

            if (flagUlongValues != 0)
            {
                error = String.Format(
                    "bad {0} flags value(s) {1} ({2}), must be {3} ({4})",
                     FormatOps.TypeName(enumType),
                     FormatOps.WrapOrNull(flagValues),
                     FormatOps.WrapOrNull(flagUlongValues),
                     FormatOps.WrapOrNull(maskValues),
                     FormatOps.WrapOrNull(maskUlongValues));

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified flags operator is
        /// unmasked, that is, present in the specified string of permitted
        /// operators.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type being parsed.
        /// </param>
        /// <param name="operator">
        /// The flags operator character to check.
        /// </param>
        /// <param name="operators">
        /// The string of permitted operator characters.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the operator is permitted; otherwise, false.
        /// </returns>
        private static bool IsFlagsOperatorUnmasked(
            Type enumType,    /* in */
            char @operator,   /* in */
            string operators, /* in */
            ref Result error  /* out */
            )
        {
            if ((operators == null) ||
                (operators.IndexOf(@operator) == Index.Invalid))
            {
                error = String.Format(
                    "bad {0} flags operator {1}, must be {2}",
                    FormatOps.TypeName(enumType),
                    FormatOps.WrapOrNull(@operator),
                    FormatOps.WrapOrNull(GenericOps<char>.ListToEnglish(
                        (operators != null) ? operators.ToCharArray() : null,
                        ", ", Characters.SpaceString, "or ")));

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the initial ("old") flags enumeration value,
        /// either by parsing the specified string value or by using a value of
        /// zero when the string is null or empty.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.  This parameter may be null or empty.
        /// </param>
        /// <param name="enumNames">
        /// The list of valid enumeration names.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="enumValues">
        /// The list of valid enumeration values.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="enumValue">
        /// Upon return, this parameter will contain the obtained enumeration
        /// value, or null if it could not be obtained.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        private static void GetFlagsValue(
            Type enumType,           /* in */
            string value,            /* in */
            StringList enumNames,    /* in: MAYBE OPTIONAL */
            UlongList enumValues,    /* in: MAYBE OPTIONAL */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            bool allowInteger,       /* in */
            bool noCase,             /* in */
            out object enumValue,    /* out */
            ref Result error         /* out */
            )
        {
            bool haveValue = false;

            if (!String.IsNullOrEmpty(value))
            {
                haveValue = true;

                enumValue = TryParse(
                    enumType, value, enumNames, enumValues,
                    allowInteger, noCase, ref error);
            }
            else
            {
                enumValue = TryGet(enumType, 0, ref error);
            }

            if (enumValue == null)
            {
                error = ResultOps.MaybeCombine(haveValue ? String.Format(
                    "invalid {0} old value {1}", FormatOps.TypeName(enumType),
                    FormatOps.WrapOrNull(value)) : null, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method applies the specified flags operator to combine the
        /// second operand enumeration value into the first operand enumeration
        /// value.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type of the operands.
        /// </param>
        /// <param name="operand2EnumValue">
        /// The second (right-hand) operand enumeration value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="operand1EnumValue">
        /// The first (left-hand) operand enumeration value.  Upon return, this
        /// parameter will contain the result of the operation, or null on
        /// failure.
        /// </param>
        /// <param name="operator">
        /// The flags operator character to apply.  Upon return, this parameter
        /// may be modified (for example, the set-add operator is changed to the
        /// add operator).
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        private static void PerformFlagsOperator(
            Type enumType,                /* in */
            object operand2EnumValue,     /* in */
            CultureInfo cultureInfo,      /* in */
            ref object operand1EnumValue, /* in, out */
            ref char @operator,           /* in, out */
            ref Result error              /* out */
            )
        {
            switch (@operator)
            {
                case SelectTableOperator:
                    {
                        //
                        // NOTE: Do nothing.
                        //
                        break;
                    }
                case AddFlagOperator:
                    {
                        //
                        // NOTE: Add the specified flag bits.
                        //
                        operand1EnumValue = TryGet(enumType,
                            ToULong(enumType,
                                (Enum)operand1EnumValue, cultureInfo) |
                            ToULong(enumType,
                                (Enum)operand2EnumValue, cultureInfo),
                            ref error);

                        break;
                    }
                case RemoveFlagOperator:
                    {
                        //
                        // NOTE: Remove the specified flag bits.
                        //
                        operand1EnumValue = TryGet(enumType,
                            ToULong(enumType,
                                (Enum)operand1EnumValue, cultureInfo) &
                            ~ToULong(enumType,
                                (Enum)operand2EnumValue, cultureInfo),
                            ref error);

                        break;
                    }
                case SetFlagOperator:
                    {
                        //
                        // NOTE: Set the overall value equal to the current
                        //       value.  This should be used only very rarely
                        //       and is supported primarily for completeness.
                        //
                        operand1EnumValue = operand2EnumValue;

                        break;
                    }
                case SetAddFlagOperator:
                    {
                        //
                        // NOTE: Set the overall value equal to the current
                        //       value and then reset the operator to add.
                        //
                        @operator = AddFlagOperator;
                        operand1EnumValue = operand2EnumValue;

                        break;
                    }
                case KeepFlagOperator:
                    {
                        //
                        // NOTE: Bitwise 'AND' the specified flag bits.
                        //
                        operand1EnumValue = TryGet(enumType,
                            ToULong(enumType,
                                (Enum)operand1EnumValue, cultureInfo) &
                            ToULong(enumType,
                                (Enum)operand2EnumValue, cultureInfo),
                            ref error);

                        break;
                    }
                default:
                    {
                        //
                        // NOTE: Any other operator character is invalid.
                        //
                        error = BadFlagsOperatorError(
                            enumType, @operator);

                        operand1EnumValue = null;
                        break;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public TryParseFlags Methods
        //
        // NOTE: This overload is only for use by the "Eagle._Commands.Host"
        //       and "Eagle._Components.Private.TraceLimits" classes.
        //
        /// <summary>
        /// This method parses a flags enumeration value by applying the
        /// operators and names in the "new" value string to the "old" value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The flags enumerated type to parse the value as.
        /// </param>
        /// <param name="oldValue">
        /// The initial enumeration value, as a string.  This parameter may be
        /// null or empty.
        /// </param>
        /// <param name="newValue">
        /// The string of operators and names to apply to the initial value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit values to be expressed as integers.
        /// </param>
        /// <param name="errorOnNop">
        /// Non-zero to treat an empty "new" value as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <returns>
        /// The resulting enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParseFlags(
            Interpreter interpreter, /* in: OPTIONAL */
            Type enumType,           /* in */
            string oldValue,         /* in */
            string newValue,         /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            bool allowInteger,       /* in */
            bool errorOnNop,         /* in */
            bool noCase              /* in */
            )
        {
            Result error = null;

            return TryParseFlags(
                interpreter, enumType, oldValue, newValue, cultureInfo,
                allowInteger, errorOnNop, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a flags enumeration value by applying the
        /// operators and names in the "new" value string to the "old" value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The flags enumerated type to parse the value as.
        /// </param>
        /// <param name="oldValue">
        /// The initial enumeration value, as a string.  This parameter may be
        /// null or empty.
        /// </param>
        /// <param name="newValue">
        /// The string of operators and names to apply to the initial value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit values to be expressed as integers.
        /// </param>
        /// <param name="errorOnNop">
        /// Non-zero to treat an empty "new" value as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The resulting enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParseFlags( /* COMPAT: Eagle beta. */
            Interpreter interpreter, /* in: OPTIONAL */
            Type enumType,           /* in */
            string oldValue,         /* in */
            string newValue,         /* in */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            bool allowInteger,       /* in */
            bool errorOnNop,         /* in */
            bool noCase,             /* in */
            ref Result error         /* out */
            )
        {
            return TryParseFlags(
                interpreter, enumType, oldValue, newValue, null, null,
                cultureInfo, allowInteger, errorOnNop, false, noCase,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a flags enumeration value by applying the
        /// operators and names in the "new" value string to the "old" value,
        /// honoring an optional set of mask values and mask operators.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The flags enumerated type to parse the value as.
        /// </param>
        /// <param name="oldValue">
        /// The initial enumeration value, as a string.  This parameter may be
        /// null or empty.
        /// </param>
        /// <param name="newValue">
        /// The string of operators and names to apply to the initial value.
        /// </param>
        /// <param name="maskValues">
        /// The mask values that constrain which flag values may be used.  This
        /// parameter may be null.
        /// </param>
        /// <param name="maskOperators">
        /// The string of operators that constrain which flag operators may be
        /// used.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit values to be expressed as integers.
        /// </param>
        /// <param name="errorOnNop">
        /// Non-zero to treat an empty "new" value as an error.
        /// </param>
        /// <param name="errorOnMask">
        /// Non-zero to treat the use of a masked value or operator as an error
        /// instead of silently skipping it.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The resulting enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParseFlags(
            Interpreter interpreter, /* in: OPTIONAL */
            Type enumType,           /* in */
            string oldValue,         /* in */
            string newValue,         /* in */
            string maskValues,       /* in: OPTIONAL */
            string maskOperators,    /* in: OPTIONAL */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            bool allowInteger,       /* in */
            bool errorOnNop,         /* in */
            bool errorOnMask,        /* in */
            bool noCase,             /* in */
            ref Result error         /* out */
            )
        {
            StringList enumNames = null;
            UlongList enumValues = null;

            if (ShouldGetNamesAndValues() && GetNamesAndValues(
                    enumType, ref enumNames, ref enumValues,
                    ref error) != ReturnCode.Ok)
            {
                return null;
            }

            return TryParseFlags(
                interpreter, enumType, oldValue, newValue, maskValues,
                maskOperators, enumNames, enumValues, cultureInfo,
                allowInteger, errorOnNop, errorOnMask, noCase, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private TryParseFlags Methods
        /// <summary>
        /// This method checks whether the specified item is one of the special,
        /// recognized flag names (currently only for the CommandBehavior type)
        /// and, if so, returns its corresponding enumeration value.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type being parsed.
        /// </param>
        /// <param name="item">
        /// The item string to check.
        /// </param>
        /// <param name="itemEnumValue">
        /// Upon return, this parameter will contain the special enumeration
        /// value, or null if the item is not a special flag.
        /// </param>
        /// <returns>
        /// True if the item is a recognized special flag; otherwise, false.
        /// </returns>
        private static bool CheckForSpecialFlags(
            Type enumType,           /* in */
            string item,             /* in */
            out object itemEnumValue /* out */
            )
        {
#if DATA
            //
            // HACK: *SPECIAL* Starting with the 1.0.118.0 release of
            //       System.Data.SQLite, these "extra" values will be
            //       recognized by the SQLiteCommand class.
            //
            if (enumType != typeof(CommandBehavior))
            {
                itemEnumValue = null;
                return false;
            }

            ///////////////////////////////////////////////////////////////////

            if (SharedStringOps.SystemEquals(item, "SkipExtraReads"))
            {
                itemEnumValue = SkipExtraReads;
                return true;
            }
            else if (SharedStringOps.SystemEquals(item, "ForceExtraReads"))
            {
                itemEnumValue = ForceExtraReads;
                return true;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            itemEnumValue = null;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a flags enumeration value by applying the
        /// operators and names in the "new" value string to the "old" value,
        /// using the supplied name and value lists and honoring an optional set
        /// of mask values and mask operators.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The flags enumerated type to parse the value as.
        /// </param>
        /// <param name="oldValue">
        /// The initial enumeration value, as a string.  This parameter may be
        /// null or empty.
        /// </param>
        /// <param name="newValue">
        /// The string of operators and names to apply to the initial value.
        /// </param>
        /// <param name="maskValues">
        /// The mask values that constrain which flag values may be used.  This
        /// parameter may be null.
        /// </param>
        /// <param name="maskOperators">
        /// The string of operators that constrain which flag operators may be
        /// used.  This parameter may be null.
        /// </param>
        /// <param name="enumNames">
        /// The list of valid enumeration names.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="enumValues">
        /// The list of valid enumeration values.  This parameter may be null
        /// when using the built-in parsing implementation.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit values to be expressed as integers.
        /// </param>
        /// <param name="errorOnNop">
        /// Non-zero to treat an empty "new" value as an error.
        /// </param>
        /// <param name="errorOnMask">
        /// Non-zero to treat the use of a masked value or operator as an error
        /// instead of silently skipping it.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The resulting enumerated value, or null if it could not be obtained.
        /// </returns>
        private static object TryParseFlags(
            Interpreter interpreter, /* in: OPTIONAL */
            Type enumType,           /* in */
            string oldValue,         /* in */
            string newValue,         /* in */
            string maskValues,       /* in: OPTIONAL */
            string maskOperators,    /* in: OPTIONAL */
            StringList enumNames,    /* in: MAYBE OPTIONAL */
            UlongList enumValues,    /* in: MAYBE OPTIONAL */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            bool allowInteger,       /* in */
            bool errorOnNop,         /* in */
            bool errorOnMask,        /* in */
            bool noCase,             /* in */
            ref Result error         /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return null;
            }

            object maskEnumValues;
            Result localError; /* REUSED */

            if (maskValues != null)
            {
                localError = null;

                maskEnumValues = TryParse(
                    enumType, maskValues, enumNames, enumValues,
                    allowInteger, noCase, ref localError);

                if (maskEnumValues == null)
                {
                    error = localError;
                    return null;
                }
            }
            else
            {
                maskEnumValues = null; /* NOTE: Nothing is masked. */
            }

            localError = null;

            if ((maskOperators != null) && !AreFlagsOperators(
                    enumType, ref maskOperators, ref localError))
            {
                error = localError;
                return null;
            }

            //
            // NOTE: To make scripting easier, a null or empty string used for
            //       the "new" enumeration value will not be used.  For strict
            //       mode, this condition will cause an error; otherwise, only
            //       the "old" enumeration value will be considered.
            //
            if (!String.IsNullOrEmpty(newValue))
            {
                //
                // NOTE: First, attempt to parse the "old" enumeration value.
                //       If this fails, we cannot continue.  To make scripting
                //       easier, a null or empty string will be converted to an
                //       "old" enumeration value of zero.
                //
                object oldEnumValue;

                localError = null;

                GetFlagsValue(
                    enumType, oldValue, enumNames, enumValues, cultureInfo,
                    allowInteger, noCase, out oldEnumValue, ref localError);

                if (oldEnumValue == null)
                {
                    error = localError;
                    return null;
                }

                //
                // HACK: If necessary, transform common flag delimiters to
                //       spaces so that we can try to parse the value as a
                //       list.  This treatment of delimiters may be too
                //       liberal; however, it does make enumeration values
                //       easier to use from scripts.
                //
                newValue = FixupFlagsString(newValue);

                //
                // NOTE: Break "new" value into a list, each element should
                //       be a value of the specified enumeration type, or an
                //       integer of some kind, optionally preceded by a flags
                //       operator indicating how it should be applied to the
                //       "old" enumeration value.
                //
                StringList list = null;

                localError = null;

                if (ParserOps<string>.SplitList(
                        interpreter, newValue, 0, Length.Invalid, true,
                        ref list, ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    return null;
                }

                //
                // NOTE: MINOR OPTIMIZATION: If there are no list items to
                //       handle, skip the main processing loop (i.e. we can
                //       simply return the old enumeration value now).  It
                //       was already validated (above).  This conditional
                //       can only really be hit if/when the new value was
                //       non-empty and consisted solely of whitespace.
                //
                int count = list.Count;

                if (count == 0)
                    return oldEnumValue;

                //
                // NOTE: Initial default operator is ":"; however, this may
                //       be changed within the loop for subsequent flagging
                //       operations.  If an operator mask is in place, this
                //       operator may not be among them.  In that case, the
                //       caller *must* prefix the new value with one of the
                //       unmasked operators.
                //
                char @operator = DefaultFlagOperator;

                for (int index = 0; index < count; index++)
                {
                    //
                    // NOTE: Grab next list item.  Ignore invalid and empty
                    //       list items.
                    //
                    string item = list[index];

                    if (String.IsNullOrEmpty(item))
                        continue;

                    //
                    // BUGFIX: Spaces are not allowed as either operators
                    //         -OR- names; therefore, remove any leading
                    //         (or trailing) spaces now.  If that leaves
                    //         the string empty, skip it.
                    //
                    item = item.Trim();

                    if (String.IsNullOrEmpty(item))
                        continue;

                    //
                    // NOTE: This used to check if the character was a
                    //       letter or an underscore (i.e. but crucially
                    //       not a digit) because it was assumed that
                    //       numeric values would not be used for flag
                    //       values; however, this behavior has now been
                    //       changed.
                    //
                    char character = item[0];

                    if (!Parser.IsIdentifier(character))
                    {
                        //
                        // NOTE: Must be some kind of operator (this will
                        //       be validated below).
                        //
                        @operator = character;

                        //
                        // NOTE: Skip over the leading operator character
                        //       and get the rest of the name.  It should
                        //       be noted this statement cannot throw an
                        //       exception because we know the item string
                        //       is not empty (i.e. length is at least one
                        //       -AND- the Substring method is documented
                        //       to throw ArgumentOutOfRangeException only
                        //       when the index is greater than the length
                        //       of the string, not simply equal to it).
                        //       Again, make sure to trim any leading -OR-
                        //       trailing whitespace (i.e. before checking
                        //       for an empty string, below).
                        //
                        item = item.Substring(1).Trim();

                        //
                        // BUGFIX: Without this check, just an operator is
                        //         an error; however, that should actually
                        //         be allowed.
                        //
                        if (String.IsNullOrEmpty(item))
                        {
                            string operators = @operator.ToString();

                            if (AreFlagsOperators(
                                    enumType, ref operators, ref localError))
                            {
                                continue;
                            }
                            else
                            {
                                oldEnumValue = null;
                                break;
                            }
                        }
                    }

                    object itemEnumValue;

                    if (!CheckForSpecialFlags(
                            enumType, item, out itemEnumValue))
                    {
                        localError = null;

                        itemEnumValue = TryParse(
                            enumType, item, enumNames, enumValues,
                            allowInteger, noCase, ref localError);
                    }

                    if (itemEnumValue == null)
                    {
                        oldEnumValue = null;
                        break;
                    }

                    try
                    {
                        localError = null;

                        if ((maskEnumValues != null) &&
                            !AreFlagsValuesUnmasked(
                                enumType, itemEnumValue,
                                maskEnumValues, cultureInfo,
                                ref localError)) /* throw */
                        {
                            if (errorOnMask)
                            {
                                oldEnumValue = null;
                                break;
                            }
                            else
                            {
                                //
                                // NOTE: One or more of the flag values
                                //       are masked (i.e. they cannot be
                                //       used), skip them.
                                //
                                continue;
                            }
                        }

                        localError = null;

                        if ((maskOperators != null) &&
                            !IsFlagsOperatorUnmasked(
                                enumType, @operator, maskOperators,
                                ref localError)) /* throw */
                        {
                            if (errorOnMask)
                            {
                                oldEnumValue = null;
                                break;
                            }
                            else
                            {
                                //
                                // NOTE: This flag operator is masked
                                //       (i.e. it cannot be used), skip
                                //       it.
                                //
                                continue;
                            }
                        }

                        localError = null;

                        PerformFlagsOperator(
                            enumType, itemEnumValue, cultureInfo,
                            ref oldEnumValue, ref @operator,
                            ref localError); /* throw */
                    }
                    catch (Exception e)
                    {
                        localError = e;
                        oldEnumValue = null;
                    }

                    if (oldEnumValue == null)
                        break;
                }

                //
                // NOTE: Return modified enumeration value.  This value may
                //       be null due to an error from the processing loop
                //       (above).
                //
                if (oldEnumValue == null)
                    error = localError;

                return oldEnumValue;
            }
            else if (errorOnNop)
            {
                error = String.Format(
                    "invalid {0} new value {1}",
                    FormatOps.TypeName(enumType),
                    FormatOps.WrapOrNull(newValue));

                return null;
            }
            else
            {
                object oldEnumValue;

                localError = null;

                GetFlagsValue(
                    enumType, oldValue, enumNames, enumValues, cultureInfo,
                    allowInteger, noCase, out oldEnumValue, ref localError);

                if (oldEnumValue == null)
                {
                    error = localError;
                    return null;
                }

                return oldEnumValue;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public TryParseTables Methods
        /// <summary>
        /// This method fills the parameter tables for the specified enumerated
        /// type with all of its names and values, grouped according to the
        /// parameter index attributes applied to the type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to fill the tables for.
        /// </param>
        /// <param name="tables">
        /// Upon return, this parameter will contain the filled parameter
        /// tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode FillTables(
            Type enumType,                 /* in */
            ref ObjectDictionary[] tables, /* in, out */
            ref Result error               /* out */
            )
        {
            StringList enumNames = null;
            UlongList enumValues = null;

            if (GetNamesAndValues(
                    enumType, ref enumNames, ref enumValues,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            int?[] parameterIndexes = null;

            if (AttributeOps.GetParameterIndexes(
                    enumType, enumNames, ref parameterIndexes,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return FillTables(
                enumType, enumNames, enumValues, parameterIndexes,
                ref tables, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a string of operators and names into a set of
        /// parameter tables for the specified enumerated type, grouped
        /// according to the parameter index attributes applied to the type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string of operators and names to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="errorOnEmptyList">
        /// Non-zero to treat an empty modifiers list as an error.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.
        /// </param>
        /// <param name="tables">
        /// Upon return, this parameter will contain the resulting parameter
        /// tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode TryParseTables(
            Interpreter interpreter,       /* in: OPTIONAL */
            Type enumType,                 /* in */
            string value,                  /* in */
            CultureInfo cultureInfo,       /* in: OPTIONAL */
            bool noCase,                   /* in */
            bool errorOnEmptyList,         /* in */
            bool errorOnNotFound,          /* in */
            ref ObjectDictionary[] tables, /* in, out */
            ref Result error               /* out */
            )
        {
            StringList enumNames = null;
            UlongList enumValues = null;

            if (GetNamesAndValues(
                    enumType, ref enumNames, ref enumValues,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            int?[] parameterIndexes = null;

            if (AttributeOps.GetParameterIndexes(
                    enumType, enumNames, ref parameterIndexes,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return TryParseTables(
                interpreter, enumType, value, enumNames, enumValues,
                parameterIndexes, cultureInfo, noCase, errorOnEmptyList,
                errorOnNotFound, ref tables, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the combined unsigned long integer value for
        /// each parameter table and stores it into the corresponding element of
        /// the supplied parameter values array.
        /// </summary>
        /// <param name="tables">
        /// The parameter tables to combine.
        /// </param>
        /// <param name="parameterValues">
        /// Upon return, each element of this array will contain the combined
        /// value for the corresponding table.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="errorOnBadValue">
        /// Non-zero to treat a value that cannot be converted as an error
        /// instead of silently skipping it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetParameterValuesFromTables(
            ObjectDictionary[] tables, /* in */
            ulong[] parameterValues,   /* in */
            CultureInfo cultureInfo,   /* in: OPTIONAL */
            bool errorOnBadValue,      /* in */
            ref Result error           /* out */
            )
        {
            if (tables == null)
            {
                error = "invalid enumeration tables";
                return ReturnCode.Error;
            }

            if (parameterValues == null)
            {
                error = "invalid parameter values";
                return ReturnCode.Error;
            }

            Array.Clear(parameterValues, 0, parameterValues.Length);

            for (int index = 0; index < tables.Length; index++)
            {
                ObjectDictionary table = tables[index];

                if (table == null)
                    continue;

                foreach (KeyValuePair<string, object> pair in table)
                {
                    ulong? ulongValue = EnumOps.ToUIntOrULong(
                        pair.Value, cultureInfo);

                    if (ulongValue == null)
                    {
                        if (errorOnBadValue)
                        {
                            error = String.Format(
                                "bad value of type {0} for {1}",
                                FormatOps.TypeName(typeof(ulong)),
                                FormatOps.WrapOrNull(pair.Key));

                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    parameterValues[index] |= (ulong)ulongValue;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private TryParseTables Methods
        /// <summary>
        /// This method fills the parameter tables with the supplied enumeration
        /// names and values, grouped according to the supplied parameter
        /// indexes.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type the names and values belong to.
        /// </param>
        /// <param name="enumNames">
        /// The list of enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// The list of enumeration values.
        /// </param>
        /// <param name="parameterIndexes">
        /// The parameter index for each enumeration name, used to determine
        /// which table it belongs to.
        /// </param>
        /// <param name="tables">
        /// Upon return, this parameter will contain the filled parameter
        /// tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode FillTables(
            Type enumType,                 /* in */
            StringList enumNames,          /* in */
            UlongList enumValues,          /* in */
            int?[] parameterIndexes,       /* in */
            ref ObjectDictionary[] tables, /* in, out */
            ref Result error               /* out */
            )
        {
            if (enumNames == null)
            {
                error = "invalid enumeration names";
                return ReturnCode.Error;
            }

            if (enumValues == null)
            {
                error = "invalid enumeration values";
                return ReturnCode.Error;
            }

            int count1 = enumNames.Count;
            int count2 = enumValues.Count;

            if (count1 != count2)
            {
                error = CountMismatchError(count1, count2);
                return ReturnCode.Error;
            }

            if (parameterIndexes == null)
            {
                error = "invalid parameter indexes";
                return ReturnCode.Error;
            }

            for (int index = 0; index < count1; index++)
            {
                string enumName = enumNames[index];

                ObjectDictionary table = GetTable(
                    ref tables, parameterIndexes,
                    index);

                if (table == null)
                    continue;

                table[enumName] = enumValues[index];
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the enumeration names (and their values) matching
        /// the specified pattern to the appropriate parameter tables.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type the names and values belong to.
        /// </param>
        /// <param name="enumNames">
        /// The list of enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// The list of enumeration values.
        /// </param>
        /// <param name="parameterIndexes">
        /// The parameter index for each enumeration name, used to determine
        /// which table it belongs to.
        /// </param>
        /// <param name="pattern">
        /// The string-matching pattern used to select names.  This parameter
        /// may be null, in which case all names are selected.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match names in a case-insensitive manner.
        /// </param>
        /// <param name="tables">
        /// Upon return, this parameter will contain the updated parameter
        /// tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode AddToTable(
            Interpreter interpreter,       /* in: OPTIONAL */
            Type enumType,                 /* in */
            StringList enumNames,          /* in */
            UlongList enumValues,          /* in */
            int?[] parameterIndexes,       /* in */
            string pattern,                /* in: OPTIONAL */
            bool noCase,                   /* in */
            ref ObjectDictionary[] tables, /* in, out */
            ref Result error               /* out */
            )
        {
            if (enumNames == null)
            {
                error = "invalid enumeration names";
                return ReturnCode.Error;
            }

            if (enumValues == null)
            {
                error = "invalid enumeration values";
                return ReturnCode.Error;
            }

            int count1 = enumNames.Count;
            int count2 = enumValues.Count;

            if (count1 != count2)
            {
                error = CountMismatchError(count1, count2);
                return ReturnCode.Error;
            }

            if (parameterIndexes == null)
            {
                error = "invalid parameter indexes";
                return ReturnCode.Error;
            }

            for (int index = 0; index < count1; index++)
            {
                string enumName = enumNames[index];

                if (enumName == null)
                    continue;

                if ((pattern == null) || Parser.StringMatch(
                        interpreter, enumName, 0, pattern, 0,
                        noCase))
                {
                    ObjectDictionary table = GetTable(
                        ref tables, parameterIndexes,
                        index);

                    if (table == null)
                    {
                        error = String.Format(
                            "missing table for {0} name {1}",
                            FormatOps.TypeName(enumType),
                            FormatOps.WrapOrNull(enumName));

                        return ReturnCode.Error;
                    }

                    table[enumName] = enumValues[index];
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes from the specified table any entries whose key
        /// does not match the specified pattern, keeping only the matching
        /// entries.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="table">
        /// The table to modify in place.
        /// </param>
        /// <param name="pattern">
        /// The string-matching pattern used to select entries to keep.  This
        /// parameter may be null, in which case all entries match.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match keys in a case-insensitive manner.
        /// </param>
        private static void KeepFromTable(
            Interpreter interpreter, /* in: OPTIONAL */
            ObjectDictionary table,  /* in, out */
            string pattern,          /* in: OPTIONAL */
            bool noCase              /* in */
            )
        {
            if (table != null)
            {
                IntDictionary matches = new IntDictionary();
                StringList keys = new StringList(table.Keys);
                int count; /* REUSED */

                foreach (string key in keys) /* PASS #1: Gather. */
                {
                    if (key == null)
                        continue;

                    if ((pattern == null) || Parser.StringMatch(
                            interpreter, key, 0, pattern, 0,
                            noCase))
                    {
                        if (matches.TryGetValue(key, out count))
                            count++;
                        else
                            count = 1;

                        matches[key] = count;
                    }
                }

                foreach (string key in keys) /* PASS #2: Remove? */
                {
                    if (!matches.TryGetValue(
                            key, out count) || (count <= 0))
                    {
                        table.Remove(key);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes from the specified table any entries whose key
        /// matches the specified pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="table">
        /// The table to modify in place.
        /// </param>
        /// <param name="pattern">
        /// The string-matching pattern used to select entries to remove.  This
        /// parameter may be null, in which case all entries match.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match keys in a case-insensitive manner.
        /// </param>
        private static void RemoveFromTable(
            Interpreter interpreter, /* in: OPTIONAL */
            ObjectDictionary table,  /* in, out */
            string pattern,          /* in: OPTIONAL */
            bool noCase              /* in */
            )
        {
            if (table != null)
            {
                IntDictionary matches = new IntDictionary();
                StringList keys = new StringList(table.Keys);
                int count; /* REUSED */

                foreach (string key in keys) /* PASS #1: Gather. */
                {
                    if (key == null)
                        continue;

                    if ((pattern == null) || Parser.StringMatch(
                            interpreter, key, 0, pattern, 0,
                            noCase))
                    {
                        if (matches.TryGetValue(key, out count))
                            count++;
                        else
                            count = 1;

                        matches[key] = count;
                    }
                }

                foreach (string key in keys) /* PASS #2: Remove? */
                {
                    if (matches.TryGetValue(
                            key, out count) && (count > 0))
                    {
                        table.Remove(key);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the index of the first enumeration name matching
        /// the specified pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumNames">
        /// The list of enumeration names to search.
        /// </param>
        /// <param name="pattern">
        /// The string-matching pattern used to select a name.  This parameter
        /// may be null, in which case the first name matches.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match names in a case-insensitive manner.
        /// </param>
        /// <returns>
        /// The index of the first matching name, or
        /// <see cref="_Constants.Index.Invalid" /> if no name matches.
        /// </returns>
        private static int FindName(
            Interpreter interpreter, /* in: OPTIONAL */
            StringList enumNames,    /* in */
            string pattern,          /* in */
            bool noCase              /* in */
            )
        {
            if (enumNames != null)
            {
                int count = enumNames.Count;

                for (int index = 0; index < count; index++)
                {
                    if ((pattern == null) || Parser.StringMatch(
                            interpreter, enumNames[index], 0,
                            pattern, 0, noCase))
                    {
                        return index;
                    }
                }
            }

            return Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the parameter table corresponding to the
        /// enumeration name at the specified index, allocating the table array
        /// and the table itself as necessary.
        /// </summary>
        /// <param name="tables">
        /// The array of parameter tables.  Upon return, this parameter may be
        /// allocated or have an element allocated.
        /// </param>
        /// <param name="parameterIndexes">
        /// The parameter index for each enumeration name.
        /// </param>
        /// <param name="index">
        /// The index of the enumeration name whose table is to be obtained.
        /// </param>
        /// <returns>
        /// The corresponding parameter table, or null if it could not be
        /// obtained.
        /// </returns>
        private static ObjectDictionary GetTable(
            ref ObjectDictionary[] tables, /* in, out */
            int?[] parameterIndexes,       /* in */
            int index                      /* in */
            )
        {
            if (parameterIndexes == null)
                return null;

            int parameterLength = parameterIndexes.Length;

            if ((index < 0) || (index >= parameterLength))
                return null;

            int? parameterIndex = parameterIndexes[index];

            if (parameterIndex == null)
                return null;

            int? maximumParameterIndex = MathOps.Max(
                parameterIndexes);

            if (maximumParameterIndex == null)
                return null;

            if (tables == null)
            {
                tables = new ObjectDictionary[
                    (int)maximumParameterIndex + 1];
            }

            int tableIndex = (int)parameterIndex;
            int length = tables.Length;

            if ((tableIndex < 0) || (tableIndex >= length))
                return null;

            ObjectDictionary table = tables[tableIndex];

            if (table == null)
            {
                table = new ObjectDictionary();
                tables[tableIndex] = table;
            }

            return table;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a string of operators and names into the supplied
        /// parameter tables, using the supplied name and value lists and
        /// parameter indexes.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string of operators and names to parse.
        /// </param>
        /// <param name="enumNames">
        /// The list of enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// The list of enumeration values.
        /// </param>
        /// <param name="parameterIndexes">
        /// The parameter index for each enumeration name, used to determine
        /// which table it belongs to.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names in a case-insensitive manner.
        /// </param>
        /// <param name="errorOnEmptyList">
        /// Non-zero to treat an empty modifiers list as an error.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.
        /// </param>
        /// <param name="tables">
        /// Upon return, this parameter will contain the resulting parameter
        /// tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode TryParseTables(
            Interpreter interpreter,       /* in: OPTIONAL */
            Type enumType,                 /* in */
            string value,                  /* in */
            StringList enumNames,          /* in */
            UlongList enumValues,          /* in */
            int?[] parameterIndexes,       /* in */
            CultureInfo cultureInfo,       /* in: OPTIONAL */
            bool noCase,                   /* in */
            bool errorOnEmptyList,         /* in */
            bool errorOnNotFound,          /* in */
            ref ObjectDictionary[] tables, /* in, out */
            ref Result error               /* out */
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(value))
            {
                error = String.Format(
                    "invalid {0} value",
                    FormatOps.TypeName(enumType));

                return ReturnCode.Error;
            }

            if (enumNames == null)
            {
                error = "invalid enumeration names";
                return ReturnCode.Error;
            }

            if (enumValues == null)
            {
                error = "invalid enumeration values";
                return ReturnCode.Error;
            }

            int count1 = enumNames.Count;
            int count2 = enumValues.Count;

            if (count1 != count2)
            {
                error = CountMismatchError(count1, count2);
                return ReturnCode.Error;
            }

            if (parameterIndexes == null)
            {
                error = "invalid parameter indexes";
                return ReturnCode.Error;
            }

            string newValue = FixupFlagsString(value);
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, newValue, 0, Length.Invalid,
                    true, ref list, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (list.Count == 0)
            {
                if (errorOnEmptyList)
                {
                    error = "empty modifiers list";
                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            StringComparison comparisonType =
                SharedStringOps.GetSystemComparisonType(noCase);

            ObjectDictionary table = null;
            bool haveTable = false;
            char @operator = DefaultFlagOperator;

            foreach (string element in list)
            {
                if (String.IsNullOrEmpty(element))
                    continue;

                string enumName = element.Trim();

                if (String.IsNullOrEmpty(enumName))
                    continue;

                char character = enumName[0];

                if (!Parser.IsIdentifier(character))
                {
                    @operator = character;

                    enumName = enumName.Substring(1).Trim();

                    if (String.IsNullOrEmpty(enumName))
                        continue;
                }

                int index; /* REUSED */
                string pattern; /* REUSED */
                bool exact;

                if (@operator == SelectTableOperator)
                {
                    if (tables == null)
                    {
                        error = "invalid enumeration tables";
                        return ReturnCode.Error;
                    }

                    pattern = enumName;
                    index = Index.Invalid;

                    if (Value.GetIndex(
                            pattern, tables.Length,
                            ValueFlags.AnyInteger,
                            cultureInfo, ref index,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    table = GetTable(
                        ref tables, parameterIndexes,
                        index);

                    if (table == null)
                    {
                        error = String.Format(
                            "missing table for {0} index {1}",
                            FormatOps.TypeName(enumType),
                            FormatOps.WrapOrNull(pattern));

                        return ReturnCode.Error;
                    }

                    haveTable = true;
                    @operator = DefaultFlagOperator;

                    continue;
                }
                else
                {
                    index = enumNames.IndexOf(
                        enumName, 0, comparisonType);

                    if (index != Index.Invalid)
                    {
                        exact = true;
                    }
                    else
                    {
                        exact = false;

                        index = FindName(
                            interpreter, enumNames, enumName,
                            noCase);
                    }

                    if (index != Index.Invalid)
                    {
                        pattern = enumName;
                        enumName = enumNames[index];

                        if (!haveTable || (table == null))
                        {
                            table = GetTable(
                                ref tables, parameterIndexes,
                                index);

                            if (table == null)
                            {
                                error = String.Format(
                                    "missing table for {0} name {1}",
                                    FormatOps.TypeName(enumType),
                                    FormatOps.WrapOrNull(enumName));

                                return ReturnCode.Error;
                            }
                        }
                    }
                    else if (errorOnNotFound)
                    {
                        error = BadValueError(
                            enumType, enumName);

                        return ReturnCode.Error;
                    }
                    else
                    {
                        continue;
                    }
                }

                switch (@operator)
                {
                    case AddFlagOperator:
                        {
                            if (exact)
                            {
                                object enumValue = TryGet(
                                    enumType, enumValues[index],
                                    ref error);

                                if (enumValue == null)
                                    return ReturnCode.Error;

                                table[enumName] = enumValue;
                            }
                            else if (AddToTable(
                                    interpreter, enumType,
                                    enumNames, enumValues,
                                    parameterIndexes, pattern,
                                    noCase, ref tables,
                                    ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            break;
                        }
                    case RemoveFlagOperator:
                        {
                            RemoveFromTable(
                                interpreter, table,
                                pattern, noCase);

                            break;
                        }
                    case SetFlagOperator:
                        {
                            table.Clear();

                            goto case AddFlagOperator;
                        }
                    case SetAddFlagOperator:
                        {
                            table.Clear();
                            @operator = AddFlagOperator;

                            goto case AddFlagOperator;
                        }
                    case KeepFlagOperator:
                        {
                            KeepFromTable(
                                interpreter, table,
                                pattern, noCase);

                            break;
                        }
                    default:
                        {
                            error = BadFlagsOperatorError(
                                enumType, @operator);

                            return ReturnCode.Error;
                        }
                }

                haveTable = false;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
