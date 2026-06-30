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
using System.Globalization;
using System.Security;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that can hold a value of any
    /// one of a wide variety of supported types (a "variant").  It extends
    /// <see cref="INumber" /> with type-test (Is*) members that report which
    /// kind of value is currently held and conversion (To*) members that
    /// attempt to extract the held value as a specific type.
    /// </summary>
    [ObjectId("1be83337-6bbb-4ed6-bde4-dba92bda3909")]
    public interface IVariant : INumber
    {
        /// <summary>
        /// Determines whether the wrapped value is a number.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a number; otherwise, false.
        /// </returns>
        bool IsNumber();
        /// <summary>
        /// Determines whether the wrapped value is a date and time.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a date and time; otherwise, false.
        /// </returns>
        bool IsDateTime();
        /// <summary>
        /// Determines whether the wrapped value is a time span.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a time span; otherwise, false.
        /// </returns>
        bool IsTimeSpan();
        /// <summary>
        /// Determines whether the wrapped value is a globally unique
        /// identifier (GUID).
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a GUID; otherwise, false.
        /// </returns>
        bool IsGuid();
        /// <summary>
        /// Determines whether the wrapped value is a string.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a string; otherwise, false.
        /// </returns>
        bool IsString();
        /// <summary>
        /// Determines whether the wrapped value is a list.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a list; otherwise, false.
        /// </returns>
        bool IsList();
        /// <summary>
        /// Determines whether the wrapped value is a dictionary.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a dictionary; otherwise, false.
        /// </returns>
        bool IsDictionary();
        /// <summary>
        /// Determines whether the wrapped value is an opaque object.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an opaque object; otherwise, false.
        /// </returns>
        bool IsObject();
        /// <summary>
        /// Determines whether the wrapped value is a call frame.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a call frame; otherwise, false.
        /// </returns>
        bool IsCallFrame();
        /// <summary>
        /// Determines whether the wrapped value is an interpreter.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an interpreter; otherwise, false.
        /// </returns>
        bool IsInterpreter();
        /// <summary>
        /// Determines whether the wrapped value is a type.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a type; otherwise, false.
        /// </returns>
        bool IsType();
        /// <summary>
        /// Determines whether the wrapped value is a list of types.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a list of types; otherwise, false.
        /// </returns>
        bool IsTypeList();
        /// <summary>
        /// Determines whether the wrapped value is a list of enumerated
        /// values.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a list of enumerated values;
        /// otherwise, false.
        /// </returns>
        bool IsEnumList();
        /// <summary>
        /// Determines whether the wrapped value is a uniform resource
        /// identifier (URI).
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a URI; otherwise, false.
        /// </returns>
        bool IsUri();
        /// <summary>
        /// Determines whether the wrapped value is a version.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a version; otherwise, false.
        /// </returns>
        bool IsVersion();
        /// <summary>
        /// Determines whether the wrapped value is a list of return codes.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a list of return codes; otherwise,
        /// false.
        /// </returns>
        bool IsReturnCodeList();
        /// <summary>
        /// Determines whether the wrapped value is an alias.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an alias; otherwise, false.
        /// </returns>
        bool IsAlias();
        /// <summary>
        /// Determines whether the wrapped value is an option.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an option; otherwise, false.
        /// </returns>
        bool IsOption();
        /// <summary>
        /// Determines whether the wrapped value is a namespace.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a namespace; otherwise, false.
        /// </returns>
        bool IsNamespace();
        /// <summary>
        /// Determines whether the wrapped value is a secure string.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a secure string; otherwise, false.
        /// </returns>
        bool IsSecureString();
        /// <summary>
        /// Determines whether the wrapped value is a character encoding.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a character encoding; otherwise,
        /// false.
        /// </returns>
        bool IsEncoding();
        /// <summary>
        /// Determines whether the wrapped value is a culture.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a culture; otherwise, false.
        /// </returns>
        bool IsCultureInfo();
        /// <summary>
        /// Determines whether the wrapped value is a plugin.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a plugin; otherwise, false.
        /// </returns>
        bool IsPlugin();
        /// <summary>
        /// Determines whether the wrapped value is an executable entity.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an executable entity; otherwise,
        /// false.
        /// </returns>
        bool IsExecute();
        /// <summary>
        /// Determines whether the wrapped value is a callback.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a callback; otherwise, false.
        /// </returns>
        bool IsCallback();
        /// <summary>
        /// Determines whether the wrapped value is a rule set.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is a rule set; otherwise, false.
        /// </returns>
        bool IsRuleSet();
        /// <summary>
        /// Determines whether the wrapped value is an identifier.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an identifier; otherwise, false.
        /// </returns>
        bool IsIdentifier();
        /// <summary>
        /// Determines whether the wrapped value is an array of bytes.
        /// </summary>
        /// <returns>
        /// True if the wrapped value is an array of bytes; otherwise, false.
        /// </returns>
        bool IsByteArray();

        /// <summary>
        /// Converts the wrapped value to a date and time.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted date and time value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToDateTime(ref DateTime value);
        /// <summary>
        /// Converts the wrapped value to a time span.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted time span value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToTimeSpan(ref TimeSpan value);
        /// <summary>
        /// Converts the wrapped value to a globally unique identifier (GUID).
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted GUID value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToGuid(ref Guid value);
        /// <summary>
        /// Converts the wrapped value to a string.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted string value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToString(ref string value);
        /// <summary>
        /// Converts the wrapped value to a list.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToList(ref StringList value);
        /// <summary>
        /// Converts the wrapped value to a dictionary.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted dictionary value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToDictionary(ref StringDictionary value);
        /// <summary>
        /// Converts the wrapped value to an opaque object.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted object value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToObject(ref IObject value);
        /// <summary>
        /// Converts the wrapped value to a call frame.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted call frame value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToCallFrame(ref ICallFrame value);
        /// <summary>
        /// Converts the wrapped value to an interpreter.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted interpreter value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToInterpreter(ref Interpreter value);
        /// <summary>
        /// Converts the wrapped value to a type.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted type value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToType(ref Type value);
        /// <summary>
        /// Converts the wrapped value to a list of types.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list of types.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToTypeList(ref TypeList value);
        /// <summary>
        /// Converts the wrapped value to a list of enumerated values.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list of enumerated values.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToEnumList(ref EnumList value);
        /// <summary>
        /// Converts the wrapped value to a uniform resource identifier (URI).
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted URI value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToUri(ref Uri value);
        /// <summary>
        /// Converts the wrapped value to a version.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted version value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToVersion(ref Version value);
        /// <summary>
        /// Converts the wrapped value to a list of return codes.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted list of return codes.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToReturnCodeList(ref ReturnCodeList value);
        /// <summary>
        /// Converts the wrapped value to an alias.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted alias value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToAlias(ref IAlias value);
        /// <summary>
        /// Converts the wrapped value to an option.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted option value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToOption(ref IOption value);
        /// <summary>
        /// Converts the wrapped value to a namespace.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted namespace value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToNamespace(ref INamespace value);
        /// <summary>
        /// Converts the wrapped value to a secure string.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted secure string value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToSecureString(ref SecureString value);
        /// <summary>
        /// Converts the wrapped value to a character encoding.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted character encoding value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToEncoding(ref Encoding value);
        /// <summary>
        /// Converts the wrapped value to a culture.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted culture value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToCultureInfo(ref CultureInfo value);
        /// <summary>
        /// Converts the wrapped value to a plugin.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted plugin value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToPlugin(ref IPlugin value);
        /// <summary>
        /// Converts the wrapped value to an executable entity.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted executable entity value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToExecute(ref IExecute value);
        /// <summary>
        /// Converts the wrapped value to a callback.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted callback value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToCallback(ref ICallback value);
        /// <summary>
        /// Converts the wrapped value to a rule set.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted rule set value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToRuleSet(ref IRuleSet value);
        /// <summary>
        /// Converts the wrapped value to an identifier.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted identifier value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToIdentifier(ref IIdentifier value);
        /// <summary>
        /// Converts the wrapped value to an array of bytes.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the converted array of bytes.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
        bool ToByteArray(ref byte[] value);
    }
}
