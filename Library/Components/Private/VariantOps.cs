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
    [ObjectId("633ce5ab-0a38-4324-8aab-8a2ececbe57d")]
    internal static class VariantOps
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

        private static bool HaveType(
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
