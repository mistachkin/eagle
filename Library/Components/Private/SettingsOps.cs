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
    /// <summary>
    /// This class provides the private helper methods used to load and save
    /// settings, mapping between the name/value pairs stored in profile files
    /// (or streams) and the fields and properties of the associated objects
    /// (e.g. interpreter settings and the interpreter host) via reflection.
    /// </summary>
    [ObjectId("63931324-d1cc-43a0-8e19-083eb3cb21a0")]
    internal static class SettingsOps
    {
        #region Private Constants
        /// <summary>
        /// The suffix appended to a host settings file name to select the "no
        /// color" variant of the file.
        /// </summary>
        private const string NoColorSuffix = "NoColor";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to display a setting name only (i.e. when it
        /// has no associated value).
        /// </summary>
        private const string NameOnlyFormat = "SET {{{0}}}";

        /// <summary>
        /// The format string used to display a setting name and its value.
        /// </summary>
        private const string NameAndValueFormat = "SET {{{0}}} = {{{1}}}";

        /// <summary>
        /// The format string used to display a setting name and an associated
        /// error.
        /// </summary>
        private const string NameAndErrorFormat = "SET {{{0}}} --> {1}";

        /// <summary>
        /// The format string used to display a setting name, its value, and an
        /// associated error.
        /// </summary>
        private const string FullFormat = "SET {{{0}}} = {{{2}}} --> {1}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method parses the specified lines of "name = value" text,
        /// skipping blank lines and comment lines, and stores the resulting
        /// name/value pairs in the specified dictionary.
        /// </summary>
        /// <param name="lines">
        /// The lines of text to process.  This parameter cannot be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary that receives the parsed name/value pairs.  If this
        /// parameter is null, a new dictionary is created when the first pair
        /// is added.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method reads the entire specified stream and parses its
        /// content as lines of "name = value" text, storing the resulting
        /// name/value pairs in the specified dictionary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to read the stream.  This parameter may be null,
        /// in which case the default profile encoding is used.
        /// </param>
        /// <param name="stream">
        /// The stream to read.  This parameter cannot be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary that receives the parsed name/value pairs.  If this
        /// parameter is null, a new dictionary is created when the first pair
        /// is added.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method reads all lines of the specified file and parses them
        /// as "name = value" text, storing the resulting name/value pairs in
        /// the specified dictionary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to read the file.  This parameter may be null, in
        /// which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read.  This parameter cannot be null or an
        /// empty string and must refer to an existing file.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary that receives the parsed name/value pairs.  If this
        /// parameter is null, a new dictionary is created when the first pair
        /// is added.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method writes the name/value pairs from the specified
        /// dictionary to the specified file as lines of "name = value" text.
        /// The file must not already exist.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to write the file.  This parameter may be null,
        /// in which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to write.  This parameter cannot be null or an
        /// empty string and must not refer to an existing file.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary containing the name/value pairs to write.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method searches the specified type and its base types for a
        /// field with the specified name, using the specified binding flags.
        /// </summary>
        /// <param name="type">
        /// The type whose fields (and those of its base types) are searched.
        /// </param>
        /// <param name="name">
        /// The name of the field to find.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the field lookup.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The matching field information, or null if the field could not be
        /// found.
        /// </returns>
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

        /// <summary>
        /// This method searches the specified type and its base types for a
        /// property with the specified name and the specified read and write
        /// capabilities, using the specified binding flags.
        /// </summary>
        /// <param name="type">
        /// The type whose properties (and those of its base types) are
        /// searched.
        /// </param>
        /// <param name="name">
        /// The name of the property to find.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookup.
        /// </param>
        /// <param name="canRead">
        /// Non-zero to require that the matching property be readable.
        /// </param>
        /// <param name="canWrite">
        /// Non-zero to require that the matching property be writable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The matching property information, or null if the property could
        /// not be found.
        /// </returns>
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

        /// <summary>
        /// This method filters the specified properties, keeping only those
        /// whose type is one of the supported settings types and which have
        /// the required read and write capabilities.
        /// </summary>
        /// <param name="propertyInfos">
        /// The properties to filter.
        /// </param>
        /// <param name="canRead">
        /// Non-zero to require that a kept property be readable.
        /// </param>
        /// <param name="canWrite">
        /// Non-zero to require that a kept property be writable.
        /// </param>
        /// <returns>
        /// The list of properties that passed the filter, or null if none did.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the supported settings properties of the
        /// specified type, optionally requiring that they be writable.
        /// </summary>
        /// <param name="type">
        /// The type whose properties are retrieved.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookup.
        /// </param>
        /// <param name="canWrite">
        /// Non-zero to require that the retrieved properties be writable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The supported settings properties, or null if none could be found.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field from the
        /// specified object.
        /// </summary>
        /// <param name="fieldInfo">
        /// The field whose value is retrieved.  This parameter cannot be null.
        /// </param>
        /// <param name="object">
        /// The object from which to read the field value.  This parameter may
        /// be null for a static field.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the field value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method sets the value of the specified field on the specified
        /// object.
        /// </summary>
        /// <param name="fieldInfo">
        /// The field whose value is set.  This parameter cannot be null.
        /// </param>
        /// <param name="object">
        /// The object on which to set the field value.  This parameter may be
        /// null for a static field.
        /// </param>
        /// <param name="value">
        /// The value to set.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method converts the specified string into a value suitable for
        /// the specified property, based on the property type (which may be an
        /// enumeration, boolean, integer, character, string, GUID, string
        /// list, rule set, or encoding).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve certain value types (e.g.
        /// encodings).  This parameter may be null unless the property type
        /// requires it.
        /// </param>
        /// <param name="propertyInfo">
        /// The property for which the value is being converted.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="object">
        /// The object that owns the property; this is used when reading the
        /// existing value of a flags enumeration property.
        /// </param>
        /// <param name="newText">
        /// The string to convert into a property value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the string.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newValue">
        /// Upon success, receives the converted property value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified property from the
        /// specified object.
        /// </summary>
        /// <param name="propertyInfo">
        /// The property whose value is retrieved.  This parameter cannot be
        /// null and must be readable.
        /// </param>
        /// <param name="object">
        /// The object from which to read the property value.  This parameter
        /// may be null for a static property.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the property value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method sets the value of the specified property on the
        /// specified object.
        /// </summary>
        /// <param name="propertyInfo">
        /// The property whose value is set.  This parameter cannot be null and
        /// must be writable.
        /// </param>
        /// <param name="object">
        /// The object on which to set the property value.  This parameter may
        /// be null for a static property.
        /// </param>
        /// <param name="value">
        /// The value to set.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value is a console
        /// color equal to the "none" sentinel.
        /// </summary>
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// <returns>
        /// True if the value is the "none" console color; otherwise, false.
        /// </returns>
        private static bool IsConsoleColorNone(
            object value /* in */
            )
        {
            return (value is ConsoleColor) &&
                ((ConsoleColor)value == _ConsoleColor.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value is a console
        /// color equal to the "invalid" sentinel.
        /// </summary>
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// <returns>
        /// True if the value is the "invalid" console color; otherwise, false.
        /// </returns>
        private static bool IsConsoleColorInvalid(
            object value /* in */
            )
        {
            return (value is ConsoleColor) &&
                ((ConsoleColor)value == _ConsoleColor.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified property is of the
        /// console color type.
        /// </summary>
        /// <param name="propertyInfo">
        /// The property to test.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the property is of the console color type; otherwise,
        /// false.
        /// </returns>
        private static bool IsConsoleColorProperty(
            PropertyInfo propertyInfo /* in */
            )
        {
            if (propertyInfo == null)
                return false;

            if (propertyInfo.PropertyType != typeof(ConsoleColor))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted setting name and associated error
        /// (and value, when present) to the specified debug host.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to which the message is written.  This parameter may
        /// be null, in which case nothing is written.
        /// </param>
        /// <param name="name">
        /// The setting name.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The setting value.  This parameter may be null, in which case only
        /// the name and error are written.
        /// </param>
        /// <param name="error">
        /// The error to write.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the message was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a formatted setting name and associated
        /// exception (and value, when present) to the specified debug host.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to which the message is written.  This parameter may
        /// be null, in which case nothing is written.
        /// </param>
        /// <param name="name">
        /// The setting name.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The setting value.  This parameter may be null, in which case only
        /// the name and exception are written.
        /// </param>
        /// <param name="exception">
        /// The exception to write.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the message was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a formatted setting name (taken from the
        /// specified property) and associated error to the specified debug
        /// host.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to which the message is written.  This parameter may
        /// be null, in which case nothing is written.
        /// </param>
        /// <param name="propertyInfo">
        /// The property whose name is used.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The setting value.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// The error to write.
        /// </param>
        /// <returns>
        /// True if the message was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a formatted setting name (taken from the
        /// specified property) and associated exception to the specified debug
        /// host.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to which the message is written.  This parameter may
        /// be null, in which case nothing is written.
        /// </param>
        /// <param name="propertyInfo">
        /// The property whose name is used.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The setting value.  This parameter may be null.
        /// </param>
        /// <param name="exception">
        /// The exception to write.
        /// </param>
        /// <returns>
        /// True if the message was written; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method writes a formatted setting name (taken from the
        /// specified property) and its value to the specified interactive
        /// host, temporarily applying console colors when the value is a
        /// console color and the host supports colors.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to which the message is written.  This
        /// parameter may be null, in which case nothing is written.
        /// </param>
        /// <param name="propertyInfo">
        /// The property whose name is used.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The setting value to write.  This parameter may be null, in which
        /// case only the name is written.
        /// </param>
        /// <returns>
        /// True if the message was written; otherwise, false.
        /// </returns>
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

                    IColorHost colorHost = interactiveHost as IColorHost;

                    ConsoleColor? savedForegroundColor;
                    ConsoleColor? savedBackgroundColor;

                    MaybeSaveColors(colorHost,
                        out savedForegroundColor,
                        out savedBackgroundColor);

                    try
                    {
                        MaybeUseColors(
                            colorHost, propertyInfo, value);

                        if (!interactiveHost.Write(String.Format(
                                (value != null) ?
                                    NameAndValueFormat : NameOnlyFormat,
                                (propertyInfo != null) ?
                                    propertyInfo.Name : String.Empty,
                                value)))
                        {
                            return false;
                        }
                    }
                    finally
                    {
                        MaybeRestoreColors(colorHost,
                            ref savedForegroundColor,
                            ref savedBackgroundColor);
                    }

                    if (!interactiveHost.WriteLine())
                        return false;

                    return true;
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

        /// <summary>
        /// This method applies console colors to the specified color host when
        /// the specified property is a console color property and the value is
        /// a console color, using a high-contrast foreground color against the
        /// value as the background color.
        /// </summary>
        /// <param name="colorHost">
        /// The color host on which the colors are set.  This parameter may be
        /// null, in which case nothing is done.
        /// </param>
        /// <param name="propertyInfo">
        /// The property associated with the value.
        /// </param>
        /// <param name="value">
        /// The value used as the background color.
        /// </param>
        /// <returns>
        /// True if the colors were applied; otherwise, false.
        /// </returns>
        private static bool MaybeUseColors(
            IColorHost colorHost,      /* in */
            PropertyInfo propertyInfo, /* in */
            object value               /* in */
            )
        {
            if (colorHost == null)
                return false;

            if (!IsConsoleColorProperty(propertyInfo))
                return false;

            if (!(value is ConsoleColor))
                return false;

            ConsoleColor backgroundColor = (ConsoleColor)value;

            return colorHost.SetColors(true, true,
                HostOps.GetHighContrastColor(backgroundColor),
                backgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the current foreground and background colors of
        /// the specified color host so they can be restored later.
        /// </summary>
        /// <param name="colorHost">
        /// The color host whose colors are saved.  This parameter may be null,
        /// in which case nothing is saved.
        /// </param>
        /// <param name="savedForegroundColor">
        /// Upon success, receives the saved foreground color; otherwise, this
        /// is set to null.
        /// </param>
        /// <param name="savedBackgroundColor">
        /// Upon success, receives the saved background color; otherwise, this
        /// is set to null.
        /// </param>
        /// <returns>
        /// True if the colors were saved; otherwise, false.
        /// </returns>
        private static bool MaybeSaveColors(
            IColorHost colorHost,                   /* in */
            out ConsoleColor? savedForegroundColor, /* out */
            out ConsoleColor? savedBackgroundColor  /* out */
            )
        {
            savedForegroundColor = null;
            savedBackgroundColor = null;

            if (colorHost == null)
                return false;

            ConsoleColor foregroundColor = _ConsoleColor.None;
            ConsoleColor backgroundColor = _ConsoleColor.None;

            if (!colorHost.GetColors(
                    ref foregroundColor, ref backgroundColor))
            {
                return false;
            }

            savedForegroundColor = foregroundColor;
            savedBackgroundColor = backgroundColor;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the previously saved foreground and background
        /// colors to the specified color host, first resetting all terminal
        /// attributes to prevent background color bleed on non-Windows
        /// terminals.
        /// </summary>
        /// <param name="colorHost">
        /// The color host whose colors are restored.  This parameter may be
        /// null, in which case nothing is done.
        /// </param>
        /// <param name="savedForegroundColor">
        /// The saved foreground color to restore; cleared to null once
        /// restored.
        /// </param>
        /// <param name="savedBackgroundColor">
        /// The saved background color to restore; cleared to null once
        /// restored.
        /// </param>
        /// <returns>
        /// True if at least one color was restored; otherwise, false.
        /// </returns>
        private static bool MaybeRestoreColors(
            IColorHost colorHost,                   /* in */
            ref ConsoleColor? savedForegroundColor, /* in, out */
            ref ConsoleColor? savedBackgroundColor  /* in, out */
            )
        {
            if (colorHost == null)
                return false;

            //
            // BUGFIX: On non-Windows terminals, background
            //         colors extend to the right margin via
            //         ANSI escape codes.  Reset all terminal
            //         attributes before restoring saved colors
            //         to prevent background color bleed.
            //
            /* IGNORED */
            colorHost.ResetColors();

            int count = 0;

            if (savedForegroundColor != null)
            {
                if (savedBackgroundColor != null)
                {
                    if (colorHost.SetColors(true, true,
                            (ConsoleColor)savedForegroundColor,
                            (ConsoleColor)savedBackgroundColor))
                    {
                        savedForegroundColor = null;
                        savedBackgroundColor = null;

                        count += 2;
                    }
                }
                else
                {
                    if (colorHost.SetForegroundColor(
                            (ConsoleColor)savedForegroundColor))
                    {
                        savedForegroundColor = null;

                        count++;
                    }
                }
            }
            else
            {
                if (savedBackgroundColor != null)
                {
                    if (colorHost.SetForegroundColor(
                            (ConsoleColor)savedBackgroundColor))
                    {
                        savedBackgroundColor = null;

                        count++;
                    }
                }
            }

            return (count > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates a set of interpreter settings from the
        /// name/value pairs in the specified dictionary, optionally merging
        /// into existing settings and optionally expanding the resulting
        /// settings.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary containing the name/value pairs to load.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the values.  This parameter may be
        /// null.
        /// </param>
        /// <param name="merge">
        /// Non-zero to merge into the existing interpreter settings; zero to
        /// require that no valid interpreter settings already exist.
        /// </param>
        /// <param name="expand">
        /// Non-zero to expand the loaded interpreter settings.
        /// </param>
        /// <param name="interpreterSettings">
        /// On input, the existing interpreter settings (when merging); upon
        /// success, receives the resulting interpreter settings.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode LoadForInterpreter(
            StringDictionary dictionary,                  /* in */
            CultureInfo cultureInfo,                      /* in: OPTIONAL */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* in, out */
            ref Result error                              /* out */
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

            IInterpreterSettings profileInterpreterSettings =
                InterpreterSettings.Create();

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

            IInterpreterSettings newInterpreterSettings;

            if (merge && (interpreterSettings != null))
                newInterpreterSettings = interpreterSettings;
            else
                newInterpreterSettings = InterpreterSettings.Create();

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

        /// <summary>
        /// This method applies the name/value pairs in the specified
        /// dictionary to the writable properties of the specified host type,
        /// optionally writing verbose progress and error messages to the debug
        /// host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve certain value types.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host whose properties are set and to which verbose
        /// messages are written.  This parameter cannot be null.
        /// </param>
        /// <param name="type">
        /// The type whose properties are set.  This parameter cannot be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file the settings were loaded from, used for
        /// diagnostic tracing.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary containing the name/value pairs to apply.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the values.  This parameter may be
        /// null.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookups.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to write verbose progress and error messages to the debug
        /// host.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True on success; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method gets the value of the named field of the specified type
        /// from the specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the field.
        /// </param>
        /// <param name="name">
        /// The name of the field whose value is retrieved.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the field lookup.
        /// </param>
        /// <param name="object">
        /// The object from which to read the field value.  This parameter may
        /// be null for a static field.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the field value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method sets the value of the named field of the specified type
        /// on the specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the field.
        /// </param>
        /// <param name="name">
        /// The name of the field whose value is set.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the field lookup.
        /// </param>
        /// <param name="object">
        /// The object on which to set the field value.  This parameter may be
        /// null for a static field.
        /// </param>
        /// <param name="value">
        /// The value to set.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the named property of the specified
        /// type from the specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the property.
        /// </param>
        /// <param name="name">
        /// The name of the property whose value is retrieved.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookup.
        /// </param>
        /// <param name="object">
        /// The object from which to read the property value.  This parameter
        /// may be null for a static property.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the property value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method sets the value of the named property of the specified
        /// type on the specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the property.
        /// </param>
        /// <param name="name">
        /// The name of the property whose value is set.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookup.
        /// </param>
        /// <param name="object">
        /// The object on which to set the property value.  This parameter may
        /// be null for a static property.
        /// </param>
        /// <param name="value">
        /// The value to set.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path could refer to a
        /// settings profile document, based on its file extension.
        /// </summary>
        /// <param name="path">
        /// The path to test.  This parameter may be null or an empty string.
        /// </param>
        /// <returns>
        /// True if the path has the settings profile file extension;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method reads settings from the specified stream and populates
        /// a set of interpreter settings from them, optionally merging into
        /// existing settings and optionally expanding the result.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to read the stream.  This parameter may be null,
        /// in which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file associated with the stream, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="stream">
        /// The stream to read.  This parameter cannot be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the values.  This parameter may be
        /// null.
        /// </param>
        /// <param name="merge">
        /// Non-zero to merge into the existing interpreter settings; zero to
        /// require that no valid interpreter settings already exist.
        /// </param>
        /// <param name="expand">
        /// Non-zero to expand the loaded interpreter settings.
        /// </param>
        /// <param name="interpreterSettings">
        /// On input, the existing interpreter settings (when merging); upon
        /// success, receives the resulting interpreter settings.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode LoadForInterpreter(
            Encoding encoding,                            /* in: OPTIONAL */
            string fileName,                              /* in: OPTIONAL */
            Stream stream,                                /* in */
            CultureInfo cultureInfo,                      /* in: OPTIONAL */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* in, out */
            ref Result error                              /* out */
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

        /// <summary>
        /// This method reads settings from the specified file and populates a
        /// set of interpreter settings from them, optionally merging into
        /// existing settings and optionally expanding the result.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to read the file.  This parameter may be null, in
        /// which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read.  This parameter cannot be null or an
        /// empty string and must refer to an existing file.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the values.  This parameter may be
        /// null.
        /// </param>
        /// <param name="merge">
        /// Non-zero to merge into the existing interpreter settings; zero to
        /// require that no valid interpreter settings already exist.
        /// </param>
        /// <param name="expand">
        /// Non-zero to expand the loaded interpreter settings.
        /// </param>
        /// <param name="interpreterSettings">
        /// On input, the existing interpreter settings (when merging); upon
        /// success, receives the resulting interpreter settings.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode LoadForInterpreter(
            Encoding encoding,                            /* in: OPTIONAL */
            string fileName,                              /* in */
            CultureInfo cultureInfo,                      /* in: OPTIONAL */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* in, out */
            ref Result error                              /* out */
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

        /// <summary>
        /// This method writes the supported properties of the specified
        /// interpreter settings to the specified file as name/value pairs,
        /// optionally expanding the settings first.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to write the file.  This parameter may be null,
        /// in which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to write.  This parameter cannot refer to an
        /// existing file.
        /// </param>
        /// <param name="expand">
        /// Non-zero to expand the interpreter settings before saving.
        /// </param>
        /// <param name="interpreterSettings">
        /// The interpreter settings to save.  This parameter cannot be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SaveForInterpreter(
            Encoding encoding,                        /* in: OPTIONAL */
            string fileName,                          /* in */
            bool expand,                              /* in */
            IInterpreterSettings interpreterSettings, /* in: OPTIONAL */
            ref Result error                          /* out */
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

        /// <summary>
        /// This method builds the name of, and searches for, the host settings
        /// profile file matching the specified profile, type name, and color
        /// preference.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to perform the file search.
        /// </param>
        /// <param name="profile">
        /// The profile name used as part of the file name.  This parameter may
        /// be null or an empty string.
        /// </param>
        /// <param name="typeName">
        /// The host type name used as part of the file name.
        /// </param>
        /// <param name="noColor">
        /// Non-zero to select the "no color" variant of the file name.
        /// </param>
        /// <returns>
        /// The full path to the matching file, or null if it could not be
        /// found.
        /// </returns>
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

        /// <summary>
        /// This method reads host settings from the specified file and applies
        /// them to the writable properties of the specified host type,
        /// discarding any error information.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve certain value types.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host whose properties are set and to which verbose
        /// messages are written.  This parameter cannot be null.
        /// </param>
        /// <param name="type">
        /// The type whose properties are set.  This parameter cannot be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to read the file.  This parameter may be null, in
        /// which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the values.  This parameter may be
        /// null.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookups.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to write verbose progress and error messages to the debug
        /// host.
        /// </param>
        /// <returns>
        /// True on success; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method reads host settings from the specified stream and
        /// applies them to the writable properties of the specified host type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve certain value types.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host whose properties are set and to which verbose
        /// messages are written.  This parameter cannot be null.
        /// </param>
        /// <param name="type">
        /// The type whose properties are set.  This parameter cannot be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to read the stream.  This parameter may be null,
        /// in which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file associated with the stream, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="stream">
        /// The stream to read.  This parameter cannot be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the values.  This parameter may be
        /// null.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookups.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to write verbose progress and error messages to the debug
        /// host.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True on success; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method reads host settings from the specified file and applies
        /// them to the writable properties of the specified host type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve certain value types.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host whose properties are set and to which verbose
        /// messages are written.  This parameter cannot be null.
        /// </param>
        /// <param name="type">
        /// The type whose properties are set.  This parameter cannot be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to read the file.  This parameter may be null, in
        /// which case the default profile encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read.  This parameter cannot be null or an
        /// empty string and must refer to an existing file.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the values.  This parameter may be
        /// null.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookups.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to write verbose progress and error messages to the debug
        /// host.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True on success; otherwise, false.
        /// </returns>
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
