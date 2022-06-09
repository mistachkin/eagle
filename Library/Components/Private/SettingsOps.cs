/*
 * SettingsOps.cs --
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
using System.IO;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("63931324-d1cc-43a0-8e19-083eb3cb21a0")]
    internal static class SettingsOps
    {
        #region Private Constants
        private const string NoColorSuffix = "NoColor";

        ///////////////////////////////////////////////////////////////////////

        private const string NameOnlyFormat = "SET {{{0}}}";
        private const string NameAndValueFormat = "SET {{{0}}} = {{{1}}}";
        private const string NameAndErrorFormat = "SET {{{0}}} --> {1}";
        private const string FullFormat = "SET {{{0}}} = {{{2}}} --> {1}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ReturnCode ProcessLines(
            IEnumerable<string> lines,       /* in */
            ref StringDictionary dictionary, /* in, out */
            ref Result error                 /* out */
            )
        {
            if (lines == null)
            {
                error = "missing lines to process";
                return ReturnCode.Error;
            }

            foreach (string line in lines)
            {
                if (String.IsNullOrEmpty(line))
                    continue;

                string trimLine = line.Trim();

                if (String.IsNullOrEmpty(trimLine))
                    continue;

                char trimChar = trimLine[0];

                if ((trimChar == Characters.Comment) ||
                    (trimChar == Characters.AltComment))
                {
                    continue;
                }

                int index = trimLine.IndexOf(Characters.EqualSign);

                if (index == Index.Invalid)
                    continue;

                if ((index <= 0) || ((index + 1) >= trimLine.Length))
                    continue;

                string name = trimLine.Substring(0, index).Trim();

                if (name == null)
                    continue; /* IMPOSSIBLE */

                string value = trimLine.Substring(index + 1).Trim();

                if (dictionary == null)
                    dictionary = new StringDictionary();

                dictionary[name] = value;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadStream(
            Encoding encoding,               /* in: OPTIONAL */
            Stream stream,                   /* in */
            ref StringDictionary dictionary, /* in, out */
            ref Result error                 /* out */
            )
        {
            if (stream == null)
            {
                error = "invalid stream";
                return ReturnCode.Error;
            }

            //
            // NOTE: The encoding used here CANNOT be null; therefore,
            //       reset it to the default encoding associated with
            //       this method.
            //
            if (encoding == null)
                encoding = StringOps.GetEncoding(EncodingType.Profile);

            try
            {
                using (StreamReader streamReader = new StreamReader(
                        stream)) /* throw */
                {
                    return ProcessLines(
                        streamReader.ReadToEnd().Split(
                        Characters.LineTerminatorChars,
                        StringSplitOptions.RemoveEmptyEntries),
                        ref dictionary, ref error); /* throw */
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadFile(
            Encoding encoding,               /* in: OPTIONAL */
            string fileName,                 /* in */
            ref StringDictionary dictionary, /* in, out */
            ref Result error                 /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't read file \"{0}\": no such file or directory",
                    fileName);

                return ReturnCode.Error;
            }

            //
            // NOTE: The encoding used here CANNOT be null; therefore,
            //       reset it to the default encoding associated with
            //       this method.
            //
            if (encoding == null)
                encoding = StringOps.GetEncoding(EncodingType.Profile);

            try
            {
                return ProcessLines(File.ReadAllLines(
                    fileName, encoding), ref dictionary,
                    ref error); /* throw */
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode WriteFile(
            Encoding encoding,           /* in: OPTIONAL */
            string fileName,             /* in */
            StringDictionary dictionary, /* in */
            ref Result error             /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't write file \"{0}\": file already exists",
                    fileName);

                return ReturnCode.Error;
            }

            if (dictionary == null)
            {
                error = "invalid dictionary";
                return ReturnCode.Error;
            }

            //
            // NOTE: The encoding used here CANNOT be null; therefore,
            //       reset it to the default encoding associated with
            //       this method.
            //
            if (encoding == null)
                encoding = StringOps.GetEncoding(EncodingType.Profile);

            try
            {
                StringList lines = new StringList(dictionary.Count);

                foreach (KeyValuePair<string, string> pair in dictionary)
                {
                    string trimLine = String.Format(
                        "{0}{1}{2}{1}{3}", pair.Key, Characters.Space,
                        Characters.EqualSign, pair.Value).Trim();

                    lines.Add(trimLine);
                }

                File.WriteAllLines(fileName, lines.ToArray(), encoding);
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static FieldInfo GetFieldInfo(
            Type type,                 /* in */
            string name,               /* in */
            BindingFlags bindingFlags, /* in */
            ref Result error           /* out */
            )
        {
            try
            {
                Type localType = type;

                while (true)
                {
                    if (localType == null)
                        break;

                    FieldInfo fieldInfo = localType.GetField(
                        name, bindingFlags); /* throw */

                    if (fieldInfo != null)
                        return fieldInfo;

                    localType = localType.BaseType;
                }

                error = String.Format(
                    "field {0} of {1} not found",
                    FormatOps.WrapOrNull(name),
                    MarshalOps.GetErrorTypeName(type));
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static PropertyInfo GetPropertyInfo(
            Type type,                 /* in */
            string name,               /* in */
            BindingFlags bindingFlags, /* in */
            bool canRead,              /* in */
            bool canWrite,             /* in */
            ref Result error           /* out */
            )
        {
            try
            {
                Type localType = type;

                while (true)
                {
                    if (localType == null)
                        break;

                    PropertyInfo propertyInfo = localType.GetProperty(
                        name, bindingFlags); /* throw */

                    if ((propertyInfo != null) &&
                        (!canRead || propertyInfo.CanRead) &&
                        (!canWrite || propertyInfo.CanWrite))
                    {
                        return propertyInfo;
                    }

                    localType = localType.BaseType;
                }

                error = String.Format(
                    "{0}property {1} of {2} not found",
                    canWrite ? "writable " : String.Empty,
                    FormatOps.WrapOrNull(name),
                    MarshalOps.GetErrorTypeName(type));
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IEnumerable<PropertyInfo> FilterPropertyInfos(
            IEnumerable<PropertyInfo> propertyInfos, /* in */
            bool canRead,                            /* in */
            bool canWrite                            /* in */
            )
        {
            List<PropertyInfo> result = null;

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo == null)
                    continue;

                Type propertyType = propertyInfo.PropertyType;

                if (propertyType == null)
                    continue;

                if (!propertyType.IsEnum &&
                    (propertyType != typeof(bool)) &&
                    (propertyType != typeof(int)) &&
                    (propertyType != typeof(char)) &&
                    (propertyType != typeof(string)) &&
                    (propertyType != typeof(Guid)) &&
                    (propertyType != typeof(StringList)) &&
                    (propertyType != typeof(IEnumerable<string>)) &&
                    (propertyType != typeof(RuleSet)) &&
                    (propertyType != typeof(IRuleSet)) &&
                    (propertyType != typeof(Encoding)))
                {
                    continue;
                }

                if (canRead && !propertyInfo.CanRead)
                    continue;

                if (canWrite && !propertyInfo.CanWrite)
                    continue;

                if (result == null)
                    result = new List<PropertyInfo>();

                result.Add(propertyInfo);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IEnumerable<PropertyInfo> GetPropertyInfos(
            Type type,                 /* in */
            BindingFlags bindingFlags, /* in */
            bool canWrite,             /* in */
            ref Result error           /* out */
            )
        {
            try
            {
                if (type != null)
                {
                    PropertyInfo[] propertyInfos = type.GetProperties(
                        bindingFlags); /* throw */

                    if (propertyInfos != null)
                    {
                        return FilterPropertyInfos(
                            propertyInfos, true, canWrite);
                    }
                }

                error = String.Format(
                    "{0}properties of {1} not found",
                    canWrite ? "writable " : String.Empty,
                    MarshalOps.GetErrorTypeName(type));
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetFieldValue(
            FieldInfo fieldInfo, /* in */
            object @object,      /* in: OPTIONAL */
            ref object value,    /* out */
            ref Result error     /* out */
            )
        {
            if (fieldInfo == null)
            {
                error = "invalid field info";
                return ReturnCode.Error;
            }

            try
            {
                value = fieldInfo.GetValue(
                    @object); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SetFieldValue(
            FieldInfo fieldInfo, /* in */
            object @object,      /* in: OPTIONAL */
            object value,        /* in: OPTIONAL */
            ref Result error     /* out */
            )
        {
            if (fieldInfo == null)
            {
                error = "invalid field info";
                return ReturnCode.Error;
            }

            try
            {
                fieldInfo.SetValue(
                    @object, value); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetPropertyValueFromString(
            Interpreter interpreter,   /* in: OPTIONAL (?) */
            PropertyInfo propertyInfo, /* in */
            object @object,            /* in */
            string newText,            /* in */
            CultureInfo cultureInfo,   /* in: OPTIONAL */
            ref object newValue,       /* out */
            ref Result error           /* out */
            )
        {
            if (propertyInfo == null)
            {
                error = "invalid property info";
                return ReturnCode.Error;
            }

            Type propertyType = propertyInfo.PropertyType;

            if (propertyType == null)
            {
                error = "invalid property type";
                return ReturnCode.Error;
            }

            Result localError; /* REUSED */

            if (propertyType.IsEnum)
            {
                object enumValue; /* REUSED */

                if (EnumOps.IsFlags(propertyType))
                {
                    string oldText = null;

                    if (propertyInfo.CanRead)
                    {
                        enumValue = null;
                        localError = null;

                        if (GetPropertyValue(
                                propertyInfo, @object, ref enumValue,
                                ref localError) == ReturnCode.Ok)
                        {
                            if (enumValue != null)
                                oldText = enumValue.ToString();
                        }
                        else
                        {
                            goto done;
                        }
                    }

                    localError = null;

                    enumValue = EnumOps.TryParseFlags(
                        interpreter, propertyType, oldText,
                        newText, cultureInfo, true, true,
                        true, ref localError);

                    if (enumValue != null)
                    {
                        newValue = enumValue;
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    localError = null;

                    enumValue = EnumOps.TryParse(
                        propertyType, newText, true,
                        true, ref localError);

                    if (enumValue != null)
                    {
                        newValue = enumValue;
                        return ReturnCode.Ok;
                    }
                    else if (propertyType == typeof(ConsoleColor))
                    {
                        ResultList errors = null;

                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        localError = null;

                        enumValue = EnumOps.TryParse(
                            typeof(HostColor), newText,
                            true, true, ref localError);

                        if (enumValue is HostColor)
                        {
                            //
                            // HACK: Automagically convert host
                            //       color into console color.
                            //       This cannot fail.
                            //
                            enumValue = (ConsoleColor)enumValue;

                            newValue = enumValue;
                            return ReturnCode.Ok;
                        }

                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        localError = errors;
                    }
                }
            }
            else if (propertyType == typeof(bool))
            {
                bool boolValue = false;

                localError = null;

                if (Value.GetBoolean2(
                        newText, ValueFlags.AnyBoolean,
                        cultureInfo, ref boolValue,
                        ref localError) == ReturnCode.Ok)
                {
                    newValue = boolValue;
                    return ReturnCode.Ok;
                }
            }
            else if (propertyType == typeof(int))
            {
                int intValue = 0;

                localError = null;

                if (Value.GetInteger2(
                        newText, ValueFlags.AnyInteger,
                        cultureInfo, ref intValue,
                        ref localError) == ReturnCode.Ok)
                {
                    newValue = intValue;
                    return ReturnCode.Ok;
                }
            }
            else if (propertyType == typeof(char))
            {
                //
                // HACK: Just grab the first character of the
                //       string.  This cannot fail.
                //
                // TODO: Why was this being done?
                //
                char charValue = !String.IsNullOrEmpty(newText) ?
                    newText[0] : Characters.Null;

                newValue = charValue;
                return ReturnCode.Ok;
            }
            else if (propertyType == typeof(string))
            {
                //
                // HACK: Do nothing, return the initial text
                //       as the value.  This cannot fail.
                //
                newValue = newText;
                return ReturnCode.Ok;
            }
            else if (propertyType == typeof(Guid))
            {
                Guid guidValue = Guid.Empty;

                localError = null;

                if (Value.GetGuid(
                        newText, cultureInfo, ref guidValue,
                        ref localError) == ReturnCode.Ok)
                {
                    newValue = guidValue;
                    return ReturnCode.Ok;
                }
            }
            else if ((propertyType == typeof(StringList) ||
                (propertyType == typeof(IEnumerable<string>))))
            {
                StringList listValue = null;

                //
                // WARNING: Cannot cache list representation
                //          here, the list may be modified via
                //          the public property in the future.
                //
                localError = null;

                if (ParserOps<string>.SplitList(
                        null, newText, 0, Length.Invalid,
                        false, ref listValue,
                        ref localError) == ReturnCode.Ok)
                {
                    newValue = listValue;
                    return ReturnCode.Ok;
                }
            }
            else if ((propertyType == typeof(RuleSet) ||
                (propertyType == typeof(IRuleSet))))
            {
                IRuleSet ruleSetValue;

                localError = null;

                ruleSetValue = RuleSet.Create(
                    newText, cultureInfo, ref localError);

                if (ruleSetValue != null)
                {
                    newValue = ruleSetValue;
                    return ReturnCode.Ok;
                }
            }
            else if (propertyType == typeof(Encoding))
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                Encoding encodingValue = null;

                localError = null;

                if (interpreter.GetEncoding(
                        newText, LookupFlags.Default,
                        ref encodingValue,
                        ref localError) == ReturnCode.Ok)
                {
                    newValue = encodingValue;
                    return ReturnCode.Ok;
                }
            }
            else
            {
                localError = "unsupported property type";
            }

        done:

            error = localError;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetPropertyValue(
            PropertyInfo propertyInfo, /* in */
            object @object,            /* in: OPTIONAL */
            ref object value,          /* out */
            ref Result error           /* out */
            )
        {
            if (propertyInfo == null)
            {
                error = "invalid property info";
                return ReturnCode.Error;
            }

            try
            {
                if (propertyInfo.CanRead)
                {
                    value = propertyInfo.GetValue(
                        @object, null); /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "property cannot be read";
                    return ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SetPropertyValue(
            PropertyInfo propertyInfo, /* in */
            object @object,            /* in: OPTIONAL */
            object value,              /* in: OPTIONAL */
            ref Result error           /* out */
            )
        {
            if (propertyInfo == null)
            {
                error = "invalid property info";
                return ReturnCode.Error;
            }

            try
            {
                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(
                        @object, value, null); /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "property cannot be written";
                    return ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsConsoleColorNone(
            object value /* in */
            )
        {
            return (value is ConsoleColor) &&
                ((ConsoleColor)value == _ConsoleColor.None);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsConsoleColorInvalid(
            object value /* in */
            )
        {
            return (value is ConsoleColor) &&
                ((ConsoleColor)value == _ConsoleColor.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool WriteNameError(
            IDebugHost debugHost, /* in: OPTIONAL */
            string name,          /* in: OPTIONAL */
            object value,         /* in: OPTIONAL */
            Result error          /* in: OPTIONAL */
            )
        {
            try
            {
                if (debugHost != null)
                {
                    return debugHost.WriteResult(
                        ReturnCode.Error, String.Format(
                        (value != null) ?
                            FullFormat :
                            NameAndErrorFormat,
                        name, error, value), true);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SettingsOps).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool WriteNameException(
            IDebugHost debugHost, /* in: OPTIONAL */
            string name,          /* in: OPTIONAL */
            object value,         /* in: OPTIONAL */
            Exception exception   /* in: OPTIONAL */
            )
        {
            try
            {
                if (debugHost != null)
                {
                    return debugHost.WriteResult(
                        ReturnCode.Error, String.Format(
                        (value != null) ?
                            FullFormat :
                            NameAndErrorFormat,
                        name, exception, value), true);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SettingsOps).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool WriteError(
            IDebugHost debugHost,      /* in: OPTIONAL */
            PropertyInfo propertyInfo, /* in: OPTIONAL */
            object value,              /* in: OPTIONAL */
            Result error               /* in */
            )
        {
            return WriteNameError(
                debugHost, (propertyInfo != null) ?
                    propertyInfo.Name : String.Empty,
                value, error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool WriteException(
            IDebugHost debugHost,      /* in: OPTIONAL */
            PropertyInfo propertyInfo, /* in: OPTIONAL */
            object value,              /* in: OPTIONAL */
            Exception exception        /* in */
            )
        {
            return WriteNameException(
                debugHost, (propertyInfo != null) ?
                    propertyInfo.Name : String.Empty,
                value, exception);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool WriteNameAndValue(
            IInteractiveHost interactiveHost, /* in: OPTIONAL */
            PropertyInfo propertyInfo,        /* in: OPTIONAL */
            object value                      /* in: OPTIONAL */
            )
        {
            try
            {
                if (interactiveHost != null)
                {
                    if (IsConsoleColorInvalid(value))
                        value = HostColor.Invalid.ToString();
                    else if (IsConsoleColorNone(value))
                        value = HostColor.None.ToString();

                    return interactiveHost.WriteLine(String.Format(
                        (value != null) ?
                            NameAndValueFormat :
                            NameOnlyFormat,
                        (propertyInfo != null) ?
                            propertyInfo.Name : String.Empty,
                        value));
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SettingsOps).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadForInterpreter(
            StringDictionary dictionary,                 /* in */
            CultureInfo cultureInfo,                     /* in: OPTIONAL */
            bool merge,                                  /* in */
            bool expand,                                 /* in */
            ref InterpreterSettings interpreterSettings, /* in, out */
            ref Result error                             /* out */
            )
        {
            if (dictionary == null)
            {
                error = "invalid dictionary";
                return ReturnCode.Error;
            }

            if (!merge && (interpreterSettings != null))
            {
                error = "cannot overwrite valid interpreter settings";
                return ReturnCode.Error;
            }

            InterpreterSettings profileInterpreterSettings =
                new InterpreterSettings();

            BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                MetaBindingFlags.InterpreterSettings, true);

            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                string name = pair.Key;

                if (String.IsNullOrEmpty(name))
                    continue;

                PropertyInfo propertyInfo;
                Result localError = null; /* REUSED */

                propertyInfo = GetPropertyInfo(
                    typeof(InterpreterSettings),
                    name, bindingFlags, false,
                    true, ref localError);

                if (propertyInfo == null)
                {
                    error = localError;
                    return ReturnCode.Error;
                }

                object value = null;

                localError = null;

                if (GetPropertyValueFromString(null,
                        propertyInfo, profileInterpreterSettings,
                        pair.Value, cultureInfo, ref value,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    return ReturnCode.Error;
                }

                localError = null;

                if (SetPropertyValue(propertyInfo,
                        profileInterpreterSettings, value,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    return ReturnCode.Error;
                }
            }

            if (expand)
                InterpreterSettings.Expand(profileInterpreterSettings);

            InterpreterSettings newInterpreterSettings;

            if (merge && (interpreterSettings != null))
                newInterpreterSettings = interpreterSettings;
            else
                newInterpreterSettings = new InterpreterSettings();

            StringList merged = InterpreterSettings.Copy(
                profileInterpreterSettings, newInterpreterSettings,
                false);

            TraceOps.DebugTrace(String.Format(
                "LoadForInterpreter: merged = {0}",
                FormatOps.WrapOrNull(merged)),
                typeof(SettingsOps).Name,
                TracePriority.StartupDebug3);

            interpreterSettings = newInterpreterSettings;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool LoadForHost(
            Interpreter interpreter,     /* in */
            IDebugHost debugHost,        /* in */
            Type type,                   /* in */
            string fileName,             /* in */
            StringDictionary dictionary, /* in */
            CultureInfo cultureInfo,     /* in: OPTIONAL */
            BindingFlags bindingFlags,   /* in */
            bool verbose,                /* in */
            ref Result error             /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            if (debugHost == null)
            {
                error = "interpreter host not available";
                return false;
            }

            if (type == null)
            {
                error = "invalid type";
                return false;
            }

            if (dictionary == null)
            {
                error = "invalid dictionary";
                return false;
            }

            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                string name = pair.Key;

                if (String.IsNullOrEmpty(name))
                    continue;

                PropertyInfo propertyInfo;
                Result propertyError = null;

                propertyInfo = GetPropertyInfo(type,
                    name, bindingFlags, false, true,
                    ref propertyError);

                if (propertyInfo == null)
                {
                    if (verbose)
                    {
                        WriteNameError(
                            debugHost, name, null,
                            propertyError);
                    }

                    continue;
                }

                Result localError; /* REUSED */
                string text = pair.Value;
                object value = null;

                localError = null;

                if (GetPropertyValueFromString(
                        interpreter, propertyInfo, debugHost,
                        text, cultureInfo, ref value,
                        ref localError) != ReturnCode.Ok)
                {
                    if (verbose)
                    {
                        WriteError(
                            debugHost, propertyInfo, text,
                            localError);
                    }

                    continue;
                }

                localError = null;

                if (SetPropertyValue(
                        propertyInfo, debugHost, value,
                        ref localError) == ReturnCode.Ok)
                {
                    if (verbose)
                    {
                        WriteNameAndValue(
                            debugHost, propertyInfo, value);
                    }
                }
                else
                {
                    if (verbose)
                    {
                        if ((localError != null) &&
                            (localError.Value is Exception))
                        {
                            WriteException(
                                debugHost, propertyInfo, value,
                                localError.Value as Exception);
                        }
                        else
                        {
                            WriteError(
                                debugHost, propertyInfo, value,
                                localError);
                        }
                    }
                }
            }

            TraceOps.DebugTrace(String.Format(
                "LoadForHost: fileName = {0}",
                FormatOps.WrapOrNull(fileName)),
                typeof(SettingsOps).Name,
                TracePriority.HostDebug2);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static ReturnCode GetFieldValue(
            Type type,                 /* in */
            string name,               /* in */
            BindingFlags bindingFlags, /* in */
            object @object,            /* in: OPTIONAL */
            ref object value,          /* out */
            ref Result error           /* out */
            )
        {
            FieldInfo fieldInfo = GetFieldInfo(
                type, name, bindingFlags, ref error);

            if (fieldInfo == null)
                return ReturnCode.Error;

            return GetFieldValue(
                fieldInfo, @object, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetFieldValue(
            Type type,                 /* in */
            string name,               /* in */
            BindingFlags bindingFlags, /* in */
            object @object,            /* in: OPTIONAL */
            object value,              /* in: OPTIONAL */
            ref Result error           /* out */
            )
        {
            FieldInfo fieldInfo = GetFieldInfo(
                type, name, bindingFlags, ref error);

            if (fieldInfo == null)
                return ReturnCode.Error;

            return SetFieldValue(
                fieldInfo, @object, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetPropertyValue(
            Type type,                 /* in */
            string name,               /* in */
            BindingFlags bindingFlags, /* in */
            object @object,            /* in: OPTIONAL */
            ref object value,          /* out */
            ref Result error           /* out */
            )
        {
            PropertyInfo propertyInfo = GetPropertyInfo(
                type, name, bindingFlags, true, false, ref error);

            if (propertyInfo == null)
                return ReturnCode.Error;

            return GetPropertyValue(
                propertyInfo, @object, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetPropertyValue(
            Type type,                 /* in */
            string name,               /* in */
            BindingFlags bindingFlags, /* in */
            object @object,            /* in: OPTIONAL */
            object value,              /* in: OPTIONAL */
            ref Result error           /* out */
            )
        {
            PropertyInfo propertyInfo = GetPropertyInfo(
                type, name, bindingFlags, false, true, ref error);

            if (propertyInfo == null)
                return ReturnCode.Error;

            return SetPropertyValue(
                propertyInfo, @object, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CouldBeDocument(
            string path
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            string extension = PathOps.GetExtension(path);

            if (String.IsNullOrEmpty(extension))
                return false;

            if (SharedStringOps.Equals(extension,
                    FileExtension.Profile, PathOps.ComparisonType))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadForInterpreter(
            Encoding encoding,                           /* in: OPTIONAL */
            string fileName,                             /* in: OPTIONAL */
            Stream stream,                               /* in */
            CultureInfo cultureInfo,                     /* in: OPTIONAL */
            bool merge,                                  /* in */
            bool expand,                                 /* in */
            ref InterpreterSettings interpreterSettings, /* in, out */
            ref Result error                             /* out */
            )
        {
            StringDictionary dictionary = null;

            if (ReadStream(
                    encoding, stream, ref dictionary,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return LoadForInterpreter(
                dictionary, cultureInfo, merge, expand,
                ref interpreterSettings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadForInterpreter(
            Encoding encoding,                           /* in: OPTIONAL */
            string fileName,                             /* in */
            CultureInfo cultureInfo,                     /* in: OPTIONAL */
            bool merge,                                  /* in */
            bool expand,                                 /* in */
            ref InterpreterSettings interpreterSettings, /* in, out */
            ref Result error                             /* out */
            )
        {
            StringDictionary dictionary = null;

            if (ReadFile(
                    encoding, fileName, ref dictionary,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return LoadForInterpreter(
                dictionary, cultureInfo, merge, expand,
                ref interpreterSettings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SaveForInterpreter(
            Encoding encoding,                       /* in: OPTIONAL */
            string fileName,                         /* in */
            bool expand,                             /* in */
            InterpreterSettings interpreterSettings, /* in: OPTIONAL */
            ref Result error                         /* out */
            )
        {
            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                MetaBindingFlags.InterpreterSettings, true);

            IEnumerable<PropertyInfo> propertyInfos = GetPropertyInfos(
                typeof(InterpreterSettings), bindingFlags, true,
                ref error);

            if (propertyInfos == null)
                return ReturnCode.Error;

            if (expand)
                InterpreterSettings.Expand(interpreterSettings);

            StringDictionary dictionary = null;

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                string name = propertyInfo.Name;

                if (String.IsNullOrEmpty(name))
                    continue;

                object value = null;
                Result localError = null;

                if (GetPropertyValue(propertyInfo,
                        interpreterSettings, ref value,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    return ReturnCode.Error;
                }

                if (dictionary == null)
                    dictionary = new StringDictionary();

                //
                // NOTE: This cannot use any opaque object handles
                //       (i.e. for arbitrary object types) due to
                //       the results being written out to the file
                //       system, for loading at a later time.
                //
                dictionary[name] = StringOps.GetStringFromObject(
                    value);
            }

            if ((dictionary != null) && WriteFile(
                    encoding, fileName, dictionary,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetHostFileName(
            Interpreter interpreter, /* in */
            string profile,          /* in */
            string typeName,         /* in */
            bool noColor             /* in */
            )
        {
            string packageName = GlobalState.GetPackageName();

            if (String.IsNullOrEmpty(packageName))
                return null;

            string suffix = String.Empty;

            if (!String.IsNullOrEmpty(profile))
                suffix = profile;

            return PathOps.Search(
                interpreter, packageName + typeName + suffix +
                    (noColor ? NoColorSuffix : String.Empty) +
                FileExtension.Profile, FileSearchFlags.Standard);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LoadForHost(
            Interpreter interpreter,   /* in */
            IDebugHost debugHost,      /* in */
            Type type,                 /* in */
            Encoding encoding,         /* in: OPTIONAL */
            string fileName,           /* in */
            CultureInfo cultureInfo,   /* in: OPTIONAL */
            BindingFlags bindingFlags, /* in */
            bool verbose               /* in */
            )
        {
            Result error = null;

            return LoadForHost(
                interpreter, debugHost, type, encoding,
                fileName, cultureInfo, bindingFlags,
                verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LoadForHost(
            Interpreter interpreter,   /* in */
            IDebugHost debugHost,      /* in */
            Type type,                 /* in */
            Encoding encoding,         /* in: OPTIONAL */
            string fileName,           /* in: OPTIONAL */
            Stream stream,             /* in */
            CultureInfo cultureInfo,   /* in: OPTIONAL */
            BindingFlags bindingFlags, /* in */
            bool verbose,              /* in */
            ref Result error           /* out */
            )
        {
            StringDictionary dictionary = null;

            if (ReadStream(
                    encoding, stream, ref dictionary,
                    ref error) != ReturnCode.Ok)
            {
                return false;
            }

            return LoadForHost(
                interpreter, debugHost, type, fileName,
                dictionary, cultureInfo, bindingFlags,
                verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LoadForHost(
            Interpreter interpreter,   /* in */
            IDebugHost debugHost,      /* in */
            Type type,                 /* in */
            Encoding encoding,         /* in: OPTIONAL */
            string fileName,           /* in */
            CultureInfo cultureInfo,   /* in: OPTIONAL */
            BindingFlags bindingFlags, /* in */
            bool verbose,              /* in */
            ref Result error           /* out */
            )
        {
            StringDictionary dictionary = null;

            if (ReadFile(
                    encoding, fileName, ref dictionary,
                    ref error) != ReturnCode.Ok)
            {
                return false;
            }

            return LoadForHost(
                interpreter, debugHost, type, fileName,
                dictionary, cultureInfo, bindingFlags,
                verbose, ref error);
        }
        #endregion
    }
}
