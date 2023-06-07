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
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

namespace Eagle._Components.Public
{
    [ObjectId("1d8b24ad-d959-43bb-92a6-e20bcb369d04")]
    public class Variant : Number
    {
        private static readonly object staticSyncRoot = new object();
        private static TypeDelegateDictionary variantTypes;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static Variant()
        {
            InitializeTypes();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal new static void InitializeTypes()
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (variantTypes == null)
                {
                    variantTypes = new TypeDelegateDictionary();
                    variantTypes.Add(typeof(Number), null);
                    variantTypes.Add(typeof(DateTime), null);
                    variantTypes.Add(typeof(TimeSpan), null);
                    variantTypes.Add(typeof(Guid), null);
                    variantTypes.Add(typeof(string), null);
                    variantTypes.Add(typeof(StringList), null);
                    variantTypes.Add(typeof(StringDictionary), null);
                    variantTypes.Add(typeof(IObject), null);
                    variantTypes.Add(typeof(ICallFrame), null);
                    variantTypes.Add(typeof(Interpreter), null);
                    variantTypes.Add(typeof(Type), null);
                    variantTypes.Add(typeof(TypeList), null);
                    variantTypes.Add(typeof(EnumList), null);
                    variantTypes.Add(typeof(Uri), null);
                    variantTypes.Add(typeof(Version), null);
                    variantTypes.Add(typeof(ReturnCodeList), null);
                    variantTypes.Add(typeof(IIdentifier), null);
                    variantTypes.Add(typeof(IAlias), null);
                    variantTypes.Add(typeof(IOption), null);
                    variantTypes.Add(typeof(INamespace), null);
                    variantTypes.Add(typeof(SecureString), null);
                    variantTypes.Add(typeof(Encoding), null);
                    variantTypes.Add(typeof(CultureInfo), null);
                    variantTypes.Add(typeof(IPlugin), null);
                    variantTypes.Add(typeof(IExecute), null);
                    variantTypes.Add(typeof(ICallback), null);
                    variantTypes.Add(typeof(IRuleSet), null);
                    variantTypes.Add(typeof(byte[]), null);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static TypeList GetTypes()
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (variantTypes == null)
                    return null;

                return new TypeList(variantTypes.Keys);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveType(
            Type type
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (variantTypes == null)
                    return false;

                if (type == null)
                    return false;

                return variantTypes.ContainsKey(type);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual void Clear(bool @base)
        {
            if (@base)
                base.Value = null;

            this.dateTimeValue = null;
            this.timeSpanValue = null;
            this.guidValue = null;
            this.stringValue = null;
            this.listValue = null;
            this.dictionaryValue = null;
            this.objectValue = null;
            this.frameValue = null;
            this.interpreterValue = null;
            this.typeValue = null;
            this.typeListValue = null;
            this.enumListValue = null;
            this.uriValue = null;
            this.versionValue = null;
            this.returnCodeListValue = null;
            this.identifierValue = null;
            this.aliasValue = null;
            this.optionValue = null;
            this.namespaceValue = null;
            this.secureStringValue = null;
            this.encodingValue = null;
            this.cultureInfoValue = null;
            this.pluginValue = null;
            this.executeValue = null;
            this.callbackValue = null;
            this.ruleSetValue = null;
            this.byteArrayValue = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: We cannot use the clear method in this constructor because
        //         the base class constructor relies upon our overridden Value
        //         property to set the actual value.  Calling the clear method
        //         in this constructor negates the work done by our overridden
        //         Value property, leaving our value invalid for all types not
        //         supported directly by our base class.
        //
        public Variant(object value)
            : base(value) /* may throw */
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(bool value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(sbyte value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(byte value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(short value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(ushort value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(char value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(int value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(uint value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(long value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(ulong value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Enum value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(ReturnCode value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(MatchMode value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(MidpointRounding value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(decimal value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(float value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(double value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(DateTime value)
            : base()
        {
            Clear(false);

            this.dateTimeValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(TimeSpan value)
            : base()
        {
            Clear(false);

            this.timeSpanValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Guid value)
            : base()
        {
            Clear(false);

            this.guidValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Number value)
            : base(value)
        {
            Clear(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(string value)
            : base()
        {
            Clear(false);

            this.stringValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(StringList value)
            : base()
        {
            Clear(false);

            this.listValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(StringDictionary value)
            : base()
        {
            Clear(false);

            this.dictionaryValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(IObject value)
            : base()
        {
            Clear(false);

            this.objectValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(ICallFrame value)
            : base()
        {
            Clear(false);

            this.frameValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Interpreter value)
        {
            Clear(false);

            this.interpreterValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Type value)
        {
            Clear(false);

            this.typeValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(TypeList value)
        {
            Clear(false);

            this.typeListValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(EnumList value)
        {
            Clear(false);

            this.enumListValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Uri value)
        {
            Clear(false);

            this.uriValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Version value)
        {
            Clear(false);

            this.versionValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(ReturnCodeList value)
        {
            Clear(false);

            this.returnCodeListValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(IIdentifier value)
        {
            Clear(false);

            this.identifierValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(IAlias value)
        {
            Clear(false);

            this.aliasValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(IOption value)
        {
            Clear(false);

            this.optionValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(INamespace value)
        {
            Clear(false);

            this.namespaceValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(SecureString value)
        {
            Clear(false);

            this.secureStringValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Encoding value)
        {
            Clear(false);

            this.encodingValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(CultureInfo value)
        {
            Clear(false);

            this.cultureInfoValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(IPlugin value)
        {
            Clear(false);

            this.pluginValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(IExecute value)
        {
            Clear(false);

            this.executeValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(ICallback value)
        {
            Clear(false);

            this.callbackValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(IRuleSet value)
        {
            Clear(false);

            this.ruleSetValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(byte[] value)
        {
            Clear(false);

            this.byteArrayValue = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Variant(Variant value)
            : base(/* Number */ value)
        {
            object @object = (value != null) ? value.Value : null;

            if (@object is DateTime)
                this.dateTimeValue = (DateTime)@object; /* ValueType, Deep Copy */

            if (@object is TimeSpan)
                this.timeSpanValue = (TimeSpan)@object; /* ValueType, Deep Copy */

            if (@object is Guid)
                this.guidValue = (Guid)@object; /* ValueType, Deep Copy */

            if (@object is string)
                this.stringValue = (string)@object; /* Immutable, Deep Copy */

            if (@object is StringList)
                this.listValue = new StringList((StringList)@object); /* Deep Copy */

            if (@object is StringDictionary)
                this.dictionaryValue = new StringDictionary((IDictionary<string, string>)@object); /* Deep Copy */

            if (@object is IObject)
                this.objectValue = (IObject)@object; /* Shallow Copy */

            if (@object is ICallFrame)
                this.frameValue = (ICallFrame)@object; /* Shallow Copy */

            if (@object is Interpreter)
                this.interpreterValue = (Interpreter)@object; /* Shallow Copy */

            if (@object is Type)
                this.typeValue = (Type)@object; /* Shallow Copy */

            if (@object is TypeList)
                this.typeListValue = (TypeList)@object; /* Shallow Copy */

            if (@object is EnumList)
                this.enumListValue = (EnumList)@object; /* Shallow Copy */

            if (@object is Uri)
                this.uriValue = (Uri)@object; /* Shallow Copy */

            if (@object is Version)
                this.versionValue = (Version)@object; /* Shallow Copy */

            if (@object is ReturnCodeList)
                this.returnCodeListValue = (ReturnCodeList)@object; /* Shallow Copy */

            if (@object is IIdentifier)
                this.identifierValue = (IIdentifier)@object; /* Shallow Copy */

            if (@object is IAlias)
                this.aliasValue = (IAlias)@object; /* Shallow Copy */

            if (@object is IOption)
                this.optionValue = (IOption)@object; /* Shallow Copy */

            if (@object is INamespace)
                this.namespaceValue = (INamespace)@object; /* Shallow Copy */

            if (@object is SecureString)
                this.secureStringValue = (SecureString)@object; /* Shallow Copy */

            if (@object is Encoding)
                this.encodingValue = (Encoding)@object; /* Shallow Copy */

            if (@object is CultureInfo)
                this.cultureInfoValue = (CultureInfo)@object; /* Shallow Copy */

            if (@object is IPlugin)
                this.pluginValue = (IPlugin)@object; /* Shallow Copy */

            if (@object is IExecute)
                this.executeValue = (IExecute)@object; /* Shallow Copy */

            if (@object is ICallback)
                this.callbackValue = (ICallback)@object; /* Shallow Copy */

            if (@object is IRuleSet)
                this.ruleSetValue = (IRuleSet)@object; /* Shallow Copy */

            if (@object is byte[])
                this.byteArrayValue = (byte[])@object; /* Shallow Copy */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            if (dateTimeValue is DateTime)
            {
                return FormatOps.Iso8601DateTime(dateTimeValue);
            }
            else if (timeSpanValue is TimeSpan)
            {
                return timeSpanValue.ToString();
            }
            else if (guidValue is Guid)
            {
                return guidValue.ToString();
            }
            else if (stringValue is string)
            {
                return stringValue;
            }
            else if (listValue is StringList)
            {
                return listValue.ToString();
            }
            else if (dictionaryValue is StringDictionary)
            {
                return dictionaryValue.ToString();
            }
            else if (objectValue is IObject)
            {
                return objectValue.ToString();
            }
            else if (frameValue is ICallFrame)
            {
                return frameValue.ToString();
            }
            else if (interpreterValue is Interpreter)
            {
                return interpreterValue.ToString();
            }
            else if (typeValue is Type)
            {
                return typeValue.ToString();
            }
            else if (typeListValue is TypeList)
            {
                return typeListValue.ToString();
            }
            else if (enumListValue is EnumList)
            {
                return enumListValue.ToString();
            }
            else if (uriValue is Uri)
            {
                return uriValue.ToString();
            }
            else if (versionValue is Version)
            {
                return versionValue.ToString();
            }
            else if (returnCodeListValue is ReturnCodeList)
            {
                return returnCodeListValue.ToString();
            }
            else if (identifierValue is IIdentifier)
            {
                return identifierValue.ToString();
            }
            else if (aliasValue is IAlias)
            {
                return aliasValue.ToString();
            }
            else if (optionValue is IOption)
            {
                return optionValue.ToString();
            }
            else if (namespaceValue is INamespace)
            {
                return namespaceValue.ToString();
            }
            else if (secureStringValue is SecureString)
            {
                return secureStringValue.ToString();
            }
            else if (encodingValue is Encoding)
            {
                return encodingValue.ToString();
            }
            else if (cultureInfoValue is CultureInfo)
            {
                return cultureInfoValue.ToString();
            }
            else if (pluginValue is IPlugin)
            {
                return pluginValue.ToString();
            }
            else if (executeValue is IExecute)
            {
                return executeValue.ToString();
            }
            else if (callbackValue is ICallback)
            {
                return callbackValue.ToString();
            }
            else if (ruleSetValue is IRuleSet)
            {
                return ruleSetValue.ToString();
            }
            else if (byteArrayValue is byte[])
            {
                return Convert.ToBase64String(byteArrayValue,
                    Base64FormattingOptions.InsertLineBreaks);
            }
            else
            {
                return base.ToString();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToDateTime(Variant variant, ref DateTime value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsDateTime())
                    {
                        value = (DateTime)variant.Value;

                        result = true;
                    }
                    else
                    {
                        if (variant.IsString())
                        {
                            DateTime dateTime = DateTime.MinValue;

                            if (_Public.Value.TryParseDateTime(
                                    (string)variant.Value, true, out dateTime))
                            {
                                value = dateTime;

                                result = true;
                            }
                        }
                        else if (variant.IsList())
                        {
                            DateTime dateTime = DateTime.MinValue;

                            if (_Public.Value.TryParseDateTime(
                                    variant.Value.ToString(), true, out dateTime))
                            {
                                value = dateTime;

                                result = true;
                            }
                        }
                        else
                        {
                            IConvertible convertible = variant.Value as IConvertible;

                            if (convertible != null)
                            {
                                value = convertible.ToDateTime(
                                    _Public.Value.GetDateTimeFormatProvider(null));

                                result = true;
                            }
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

        public static bool ToTimeSpan(Variant variant, ref TimeSpan value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsTimeSpan())
                    {
                        value = (TimeSpan)variant.Value;

                        result = true;
                    }
                    else
                    {
                        if (variant.IsString())
                        {
                            TimeSpan timeSpan = TimeSpan.Zero;

                            if (TimeSpan.TryParse((string)variant.Value, out timeSpan))
                            {
                                value = timeSpan;

                                result = true;
                            }
                        }
                        else if (variant.IsList())
                        {
                            TimeSpan timeSpan = TimeSpan.Zero;

                            if (TimeSpan.TryParse(variant.Value.ToString(), out timeSpan))
                            {
                                value = timeSpan;

                                result = true;
                            }
                        }
                        else
                        {
                            IConvertible convertible = variant.Value as IConvertible;

                            if (convertible != null)
                            {
                                value = new TimeSpan(convertible.ToInt64(null));

                                result = true;
                            }
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

        public static bool ToGuid(Variant variant, ref Guid value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsGuid())
                        value = (Guid)variant.Value;
                    else if (variant.IsString())
                        value = new Guid((string)variant.Value); /* throw */
                    else if (variant.IsList())
                        value = new Guid(variant.Value.ToString()); /* throw */
                    else
                        value = new Guid(variant.Value.ToString()); /* throw */

                    result = true;
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToString(Variant variant, ref string value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsString())
                        value = (string)variant.Value;
                    else if (variant.IsList())
                        value = variant.Value.ToString();
                    else
                        value = Convert.ToString(variant.Value); /* throw */

                    result = true;
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToList(Variant variant, ref StringList value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsList())
                        value = (StringList)variant.Value;
                    else if (variant.IsString())
                        value = new StringList(new string[] {
                            (string)variant.Value
                        });
                    else
                        value = new StringList(new string[] {
                            Convert.ToString(variant.Value) /* throw */
                        });

                    result = true;
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToDictionary(Variant variant, ref StringDictionary value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsDictionary())
                        value = (StringDictionary)variant.Value;
                    else if (variant.IsList())
                        value = new StringDictionary(
                            (StringList)variant.Value, false, true);
                    else if (variant.IsString())
                        value = new StringDictionary(new string[] {
                            (string)variant.Value
                        }, false, true);
                    else
                        value = new StringDictionary(new string[] {
                            Convert.ToString(variant.Value) /* throw */
                        }, false, true);

                    result = true;
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ToObject(Variant variant, ref IObject value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsObject())
                    {
                        value = (IObject)variant.Value;

                        result = true;
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

        public static bool ToCallFrame(Variant variant, ref ICallFrame value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsCallFrame())
                    {
                        value = (ICallFrame)variant.Value;

                        result = true;
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

        public static bool ToInterpreter(Variant variant, ref Interpreter value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsInterpreter())
                    {
                        value = (Interpreter)variant.Value;

                        result = true;
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

        public static bool ToType(Variant variant, ref Type value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsType())
                    {
                        value = (Type)variant.Value;

                        result = true;
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

        public static bool ToTypeList(Variant variant, ref TypeList value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsTypeList())
                    {
                        value = (TypeList)variant.Value;

                        result = true;
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

        public static bool ToEnumList(Variant variant, ref EnumList value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsEnumList())
                    {
                        value = (EnumList)variant.Value;

                        result = true;
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

        public static bool ToUri(Variant variant, ref Uri value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsUri())
                    {
                        value = (Uri)variant.Value;

                        result = true;
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

        public static bool ToVersion(Variant variant, ref Version value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsVersion())
                    {
                        value = (Version)variant.Value;

                        result = true;
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

        public static bool ToReturnCodeList(Variant variant, ref ReturnCodeList value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsReturnCodeList())
                    {
                        value = (ReturnCodeList)variant.Value;

                        result = true;
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

        public static bool ToIdentifier(Variant variant, ref IIdentifier value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsIdentifier())
                    {
                        value = (IIdentifier)variant.Value;

                        result = true;
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

        public static bool ToAlias(Variant variant, ref IAlias value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsAlias())
                    {
                        value = (IAlias)variant.Value;

                        result = true;
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

        public static bool ToOption(Variant variant, ref IOption value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsOption())
                    {
                        value = (IOption)variant.Value;

                        result = true;
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

        public static bool ToNamespace(Variant variant, ref INamespace value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsNamespace())
                    {
                        value = (INamespace)variant.Value;

                        result = true;
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

        public static bool ToSecureString(Variant variant, ref SecureString value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsSecureString())
                    {
                        value = (SecureString)variant.Value;

                        result = true;
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

        public static bool ToEncoding(Variant variant, ref Encoding value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsEncoding())
                    {
                        value = (Encoding)variant.Value;

                        result = true;
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

        public static bool ToCultureInfo(Variant variant, ref CultureInfo value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsCultureInfo())
                    {
                        value = (CultureInfo)variant.Value;

                        result = true;
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

        public static bool ToPlugin(Variant variant, ref IPlugin value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsPlugin())
                    {
                        value = (IPlugin)variant.Value;

                        result = true;
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

        public static bool ToExecute(Variant variant, ref IExecute value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsExecute())
                    {
                        value = (IExecute)variant.Value;

                        result = true;
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

        public static bool ToCallback(Variant variant, ref ICallback value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsCallback())
                    {
                        value = (ICallback)variant.Value;

                        result = true;
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

        public static bool ToRuleSet(Variant variant, ref IRuleSet value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsRuleSet())
                    {
                        value = (IRuleSet)variant.Value;

                        result = true;
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

        public static bool ToByteArray(Variant variant, ref byte[] value)
        {
            bool result = false;

            try
            {
                if (variant != null)
                {
                    if (variant.IsByteArray())
                    {
                        value = (byte[])variant.Value;

                        result = true;
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

        public virtual bool ToDateTime(ref DateTime value)
        {
            return ToDateTime(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToTimeSpan(ref TimeSpan value)
        {
            return ToTimeSpan(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToGuid(ref Guid value)
        {
            return ToGuid(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToString(ref string value)
        {
            return ToString(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToList(ref StringList value)
        {
            return ToList(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToDictionary(ref StringDictionary value)
        {
            return ToDictionary(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToObject(ref IObject value)
        {
            return ToObject(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToCallFrame(ref ICallFrame value)
        {
            return ToCallFrame(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToInterpreter(ref Interpreter value)
        {
            return ToInterpreter(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToType(ref Type value)
        {
            return ToType(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToTypeList(ref TypeList value)
        {
            return ToTypeList(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToEnumList(ref EnumList value)
        {
            return ToEnumList(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToUri(ref Uri value)
        {
            return ToUri(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToVersion(ref Version value)
        {
            return ToVersion(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToReturnCodeList(ref ReturnCodeList value)
        {
            return ToReturnCodeList(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToIdentifier(ref IIdentifier value)
        {
            return ToIdentifier(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToAlias(ref IAlias value)
        {
            return ToAlias(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToOption(ref IOption value)
        {
            return ToOption(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToNamespace(ref INamespace value)
        {
            return ToNamespace(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToSecureString(ref SecureString value)
        {
            return ToSecureString(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToEncoding(ref Encoding value)
        {
            return ToEncoding(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToCultureInfo(ref CultureInfo value)
        {
            return ToCultureInfo(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToPlugin(ref IPlugin value)
        {
            return ToPlugin(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToExecute(ref IExecute value)
        {
            return ToExecute(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToCallback(ref ICallback value)
        {
            return ToCallback(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToRuleSet(ref IRuleSet value)
        {
            return ToRuleSet(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool ToByteArray(ref byte[] value)
        {
            return ToByteArray(this, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsNumber()
        {
            return ((dateTimeValue == null) && (timeSpanValue == null) &&
                    (guidValue == null) && (stringValue == null) &&
                    (listValue == null) && (dictionaryValue == null) &&
                    (objectValue == null) && (frameValue == null) &&
                    (interpreterValue == null) && (typeValue == null) &&
                    (typeListValue == null) && (enumListValue == null) &&
                    (uriValue == null) && (versionValue == null) &&
                    (returnCodeListValue == null) && (aliasValue == null) &&
                    (optionValue == null) && (namespaceValue == null) &&
                    (secureStringValue == null) && (encodingValue == null) &&
                    (cultureInfoValue == null) && (pluginValue == null) &&
                    (executeValue == null) && (callbackValue == null) &&
                    (ruleSetValue == null) && (byteArrayValue == null));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsDateTime()
        {
            return (dateTimeValue is DateTime);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsTimeSpan()
        {
            return (timeSpanValue is TimeSpan);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsGuid()
        {
            return (guidValue is Guid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsString()
        {
            return (stringValue is string);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsList()
        {
            return (listValue is StringList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsDictionary()
        {
            return (dictionaryValue is StringDictionary);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsObject()
        {
            return (objectValue is IObject);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsCallFrame()
        {
            return (frameValue is ICallFrame);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsInterpreter()
        {
            return (interpreterValue is Interpreter);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsType()
        {
            return (typeValue is Type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsTypeList()
        {
            return (typeListValue is TypeList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsEnumList()
        {
            return (enumListValue is EnumList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsUri()
        {
            return (uriValue is Uri);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsVersion()
        {
            return (versionValue is Version);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsReturnCodeList()
        {
            return (returnCodeListValue is ReturnCodeList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsIdentifier()
        {
            return (identifierValue is IIdentifier);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsAlias()
        {
            return (aliasValue is IAlias);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsOption()
        {
            return (optionValue is IOption);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsNamespace()
        {
            return (namespaceValue is INamespace);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsSecureString()
        {
            return (secureStringValue is SecureString);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsEncoding()
        {
            return (encodingValue is Encoding);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsCultureInfo()
        {
            return (cultureInfoValue is CultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsPlugin()
        {
            return (pluginValue is IPlugin);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsExecute()
        {
            return (executeValue is IExecute);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsCallback()
        {
            return (callbackValue is ICallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsRuleSet()
        {
            return (ruleSetValue is IRuleSet);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool IsByteArray()
        {
            return (byteArrayValue is byte[]);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static TypeList Types(Variant value /* NOT USED */)
        {
            return GetTypes();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override TypeList Types()
        {
            return Types(this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSupported(Variant value /* NOT USED */, Type type)
        {
            return HaveType(type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsSupported(Type type)
        {
            return (IsSupported(this, type) || base.IsSupported(type));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ConvertTo(Type type)
        {
            bool result = false;

            if (type == typeof(DateTime))
            {
                DateTime dateTime = DateTime.MinValue;

                if (result = ToDateTime(ref dateTime))
                {
                    Clear(true);

                    dateTimeValue = dateTime;
                }
            }
            else if (type == typeof(TimeSpan))
            {
                TimeSpan timeSpan = TimeSpan.Zero;

                if (result = ToTimeSpan(ref timeSpan))
                {
                    Clear(true);

                    timeSpanValue = timeSpan;
                }
            }
            else if (type == typeof(Guid))
            {
                Guid guid = Guid.Empty;

                if (result = ToGuid(ref guid))
                {
                    Clear(true);

                    guidValue = guid;
                }
            }
            if (type == typeof(string))
            {
                string @string = null;

                if (result = ToString(ref @string))
                {
                    Clear(true);

                    stringValue = @string;
                }
            }
            else if (type == typeof(StringList))
            {
                StringList list = null;

                if (result = ToList(ref list))
                {
                    Clear(true);

                    listValue = list;
                }
            }
            else if (type == typeof(StringDictionary))
            {
                StringDictionary dictionary = null;

                if (result = ToDictionary(ref dictionary))
                {
                    Clear(true);

                    dictionaryValue = dictionary;
                }
            }
            else if (type == typeof(IObject))
            {
                IObject @object = null;

                if (result = ToObject(ref @object))
                {
                    Clear(true);

                    objectValue = @object;
                }
            }
            else if (type == typeof(ICallFrame))
            {
                ICallFrame frame = null;

                if (result = ToCallFrame(ref frame))
                {
                    Clear(true);

                    frameValue = frame;
                }
            }
            else if (type == typeof(Interpreter))
            {
                Interpreter interpreter = null;

                if (result = ToInterpreter(ref interpreter))
                {
                    Clear(true);

                    interpreterValue = interpreter;
                }
            }
            else if (type == typeof(Type))
            {
                Type _type = null;

                if (result = ToType(ref _type))
                {
                    Clear(true);

                    typeValue = _type;
                }
            }
            else if (type == typeof(TypeList))
            {
                TypeList typeList = null;

                if (result = ToTypeList(ref typeList))
                {
                    Clear(true);

                    typeListValue = typeList;
                }
            }
            else if (type == typeof(EnumList))
            {
                EnumList enumList = null;

                if (result = ToEnumList(ref enumList))
                {
                    Clear(true);

                    enumListValue = enumList;
                }
            }
            else if (type == typeof(Uri))
            {
                Uri uri = null;

                if (result = ToUri(ref uri))
                {
                    Clear(true);

                    uriValue = uri;
                }
            }
            else if (type == typeof(Version))
            {
                Version version = null;

                if (result = ToVersion(ref version))
                {
                    Clear(true);

                    versionValue = version;
                }
            }
            else if (type == typeof(ReturnCodeList))
            {
                ReturnCodeList returnCodeList = null;

                if (result = ToReturnCodeList(ref returnCodeList))
                {
                    Clear(true);

                    returnCodeListValue = returnCodeList;
                }
            }
            else if (type == typeof(IIdentifier))
            {
                IIdentifier identifier = null;

                if (result = ToIdentifier(ref identifier))
                {
                    Clear(true);

                    identifierValue = identifier;
                }
            }
            else if (type == typeof(IAlias))
            {
                IAlias alias = null;

                if (result = ToAlias(ref alias))
                {
                    Clear(true);

                    aliasValue = alias;
                }
            }
            else if (type == typeof(IOption))
            {
                IOption option = null;

                if (result = ToOption(ref option))
                {
                    Clear(true);

                    optionValue = option;
                }
            }
            else if (type == typeof(INamespace))
            {
                INamespace @namespace = null;

                if (result = ToNamespace(ref @namespace))
                {
                    Clear(true);

                    namespaceValue = @namespace;
                }
            }
            else if (type == typeof(SecureString))
            {
                SecureString secureString = null;

                if (result = ToSecureString(ref secureString))
                {
                    Clear(true);

                    secureStringValue = secureString;
                }
            }
            else if (type == typeof(Encoding))
            {
                Encoding encoding = null;

                if (result = ToEncoding(ref encoding))
                {
                    Clear(true);

                    encodingValue = encoding;
                }
            }
            else if (type == typeof(CultureInfo))
            {
                CultureInfo cultureInfo = null;

                if (result = ToCultureInfo(ref cultureInfo))
                {
                    Clear(true);

                    cultureInfoValue = cultureInfo;
                }
            }
            else if (type == typeof(IPlugin))
            {
                IPlugin plugin = null;

                if (result = ToPlugin(ref plugin))
                {
                    Clear(true);

                    pluginValue = plugin;
                }
            }
            else if (type == typeof(IExecute))
            {
                IExecute execute = null;

                if (result = ToExecute(ref execute))
                {
                    Clear(true);

                    executeValue = execute;
                }
            }
            else if (type == typeof(ICallback))
            {
                ICallback callback = null;

                if (result = ToCallback(ref callback))
                {
                    Clear(true);

                    callbackValue = callback;
                }
            }
            else if (type == typeof(IRuleSet))
            {
                IRuleSet ruleSet = null;

                if (result = ToRuleSet(ref ruleSet))
                {
                    Clear(true);

                    ruleSetValue = ruleSet;
                }
            }
            else if (type == typeof(byte[]))
            {
                byte[] byteArray = null;

                if (result = ToByteArray(ref byteArray))
                {
                    Clear(true);

                    byteArrayValue = byteArray;
                }
            }
            else if (result = base.ConvertTo(type))
            {
                Clear(false);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override object BaseValue
        {
            get { return base.Value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private DateTime? dateTimeValue; // System.DateTime; however, we need it NULLABLE.
        private TimeSpan? timeSpanValue; // System.TimeSpan; however, we need it NULLABLE.
        private Guid? guidValue;         // System.Guid; however, we need it NULLABLE.
        private string stringValue;
        private StringList listValue;
        private StringDictionary dictionaryValue;
        private IObject objectValue;
        private ICallFrame frameValue;
        private Interpreter interpreterValue;
        private Type typeValue;
        private TypeList typeListValue;
        private EnumList enumListValue;
        private Uri uriValue;
        private Version versionValue;
        private ReturnCodeList returnCodeListValue;
        private IIdentifier identifierValue;
        private IAlias aliasValue;
        private IOption optionValue;
        private INamespace namespaceValue;
        private SecureString secureStringValue;
        private Encoding encodingValue;
        private CultureInfo cultureInfoValue;
        private IPlugin pluginValue;
        private IExecute executeValue;
        private ICallback callbackValue;
        private IRuleSet ruleSetValue;
        private byte[] byteArrayValue;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        //
        // NOTE: This is a mutable class returning a non-flattened value
        //       for the IGetValue.Value property, mostly due to backward
        //       compatibility.
        //
        public override object Value
        {
            get
            {
                if (dateTimeValue is DateTime)
                    return dateTimeValue;
                else if (timeSpanValue is TimeSpan)
                    return timeSpanValue;
                else if (guidValue is Guid)
                    return guidValue;
                else if (stringValue is string)
                    return stringValue;
                else if (listValue is StringList)
                    return listValue;
                else if (dictionaryValue is StringDictionary)
                    return dictionaryValue;
                else if (objectValue is IObject)
                    return objectValue;
                else if (frameValue is ICallFrame)
                    return frameValue;
                else if (interpreterValue is Interpreter)
                    return interpreterValue;
                else if (typeValue is Type)
                    return typeValue;
                else if (typeListValue is TypeList)
                    return typeListValue;
                else if (enumListValue is EnumList)
                    return enumListValue;
                else if (uriValue is Uri)
                    return uriValue;
                else if (versionValue is Version)
                    return versionValue;
                else if (returnCodeListValue is ReturnCodeList)
                    return returnCodeListValue;
                else if (aliasValue is IAlias)
                    return aliasValue;
                else if (optionValue is IOption)
                    return optionValue;
                else if (namespaceValue is INamespace)
                    return namespaceValue;
                else if (secureStringValue is SecureString)
                    return secureStringValue;
                else if (encodingValue is Encoding)
                    return encodingValue;
                else if (cultureInfoValue is CultureInfo)
                    return cultureInfoValue;
                else if (pluginValue is IPlugin)
                    return pluginValue;
                else if (executeValue is IExecute)
                    return executeValue;
                else if (callbackValue is ICallback)
                    return callbackValue;
                else if (ruleSetValue is IRuleSet)
                    return ruleSetValue;
                else if (byteArrayValue is byte[])
                    return byteArrayValue;
                else
                    return base.Value;
            }
            set
            {
                if (value is Number)
                {
                    Clear(true); /* enforce logical union */

                    base.Value = ((Number)value).Value; /* cannot fail */
                }
                else if (value is DateTime)
                {
                    Clear(true); /* enforce logical union */

                    this.dateTimeValue = (DateTime)value; /* cannot fail */
                }
                else if (value is TimeSpan)
                {
                    Clear(true); /* enforce logical union */

                    this.timeSpanValue = (TimeSpan)value; /* cannot fail */
                }
                else if (value is Guid)
                {
                    Clear(true); /* enforce logical union */

                    this.guidValue = (Guid)value; /* cannot fail */
                }
                else if (value is string)
                {
                    Clear(true); /* enforce logical union */

                    this.stringValue = (string)value; /* cannot fail */
                }
                else if (value is StringList)
                {
                    Clear(true); /* enforce logical union */

                    this.listValue = (StringList)value; /* cannot fail */
                }
                else if (value is StringDictionary)
                {
                    Clear(true); /* enforce logical union */

                    this.dictionaryValue = (StringDictionary)value; /* cannot fail */
                }
                else if (value is IObject)
                {
                    Clear(true); /* enforce logical union */

                    this.objectValue = (IObject)value; /* cannot fail */
                }
                else if (value is ICallFrame)
                {
                    Clear(true); /* enforce logical union */

                    this.frameValue = (ICallFrame)value; /* cannot fail */
                }
                else if (value is Interpreter)
                {
                    Clear(true); /* enforce logical union */

                    this.interpreterValue = (Interpreter)value; /* cannot fail */
                }
                else if (value is Type)
                {
                    Clear(true); /* enforce logical union */

                    this.typeValue = (Type)value; /* cannot fail */
                }
                else if (value is TypeList)
                {
                    Clear(true); /* enforce logical union */

                    this.typeListValue = (TypeList)value; /* cannot fail */
                }
                else if (value is EnumList)
                {
                    Clear(true); /* enforce logical union */

                    this.enumListValue = (EnumList)value; /* cannot fail */
                }
                else if (value is Uri)
                {
                    Clear(true); /* enforce logical union */

                    this.uriValue = (Uri)value; /* cannot fail */
                }
                else if (value is Version)
                {
                    Clear(true); /* enforce logical union */

                    this.versionValue = (Version)value; /* cannot fail */
                }
                else if (value is ReturnCodeList)
                {
                    Clear(true); /* enforce logical union */

                    this.returnCodeListValue = (ReturnCodeList)value; /* cannot fail */
                }
                else if (value is IAlias)
                {
                    Clear(true); /* enforce logical union */

                    this.aliasValue = (IAlias)value; /* cannot fail */
                }
                else if (value is IOption)
                {
                    Clear(true); /* enforce logical union */

                    this.optionValue = (IOption)value; /* cannot fail */
                }
                else if (value is INamespace)
                {
                    Clear(true); /* enforce logical union */

                    this.namespaceValue = (INamespace)value; /* cannot fail */
                }
                else if (value is SecureString)
                {
                    Clear(true); /* enforce logical union */

                    this.secureStringValue = (SecureString)value; /* cannot fail */
                }
                else if (value is Encoding)
                {
                    Clear(true); /* enforce logical union */

                    this.encodingValue = (Encoding)value; /* cannot fail */
                }
                else if (value is CultureInfo)
                {
                    Clear(true); /* enforce logical union */

                    this.cultureInfoValue = (CultureInfo)value; /* cannot fail */
                }
                else if (value is IPlugin)
                {
                    Clear(true); /* enforce logical union */

                    this.pluginValue = (IPlugin)value; /* cannot fail */
                }
                else if (value is IExecute)
                {
                    Clear(true); /* enforce logical union */

                    this.executeValue = (IExecute)value; /* cannot fail */
                }
                else if (value is ICallback)
                {
                    Clear(true); /* enforce logical union */

                    this.callbackValue = (ICallback)value; /* cannot fail */
                }
                else if (value is IRuleSet)
                {
                    Clear(true); /* enforce logical union */

                    this.ruleSetValue = (IRuleSet)value; /* cannot fail */
                }
                else if (value is byte[])
                {
                    Clear(true); /* enforce logical union */

                    this.byteArrayValue = (byte[])value; /* cannot fail */
                }
                else
                {
                    base.Value = value; /* may be null, can fail with throw */

                    Clear(false); /* enforce logical union */
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public override object Clone()
        {
            return new Variant(this);
        }
        #endregion
    }
}
