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
    [ObjectId("1be83337-6bbb-4ed6-bde4-dba92bda3909")]
    public interface IVariant : INumber
    {
        bool IsNumber();
        bool IsDateTime();
        bool IsTimeSpan();
        bool IsGuid();
        bool IsString();
        bool IsList();
        bool IsDictionary();
        bool IsObject();
        bool IsCallFrame();
        bool IsInterpreter();
        bool IsType();
        bool IsTypeList();
        bool IsEnumList();
        bool IsUri();
        bool IsVersion();
        bool IsReturnCodeList();
        bool IsAlias();
        bool IsOption();
        bool IsNamespace();
        bool IsSecureString();
        bool IsEncoding();
        bool IsCultureInfo();
        bool IsPlugin();
        bool IsExecute();
        bool IsCallback();
        bool IsRuleSet();
        bool IsIdentifier();
        bool IsByteArray();

        bool ToDateTime(ref DateTime value);
        bool ToTimeSpan(ref TimeSpan value);
        bool ToGuid(ref Guid value);
        bool ToString(ref string value);
        bool ToList(ref StringList value);
        bool ToDictionary(ref StringDictionary value);
        bool ToObject(ref IObject value);
        bool ToCallFrame(ref ICallFrame value);
        bool ToInterpreter(ref Interpreter value);
        bool ToType(ref Type value);
        bool ToTypeList(ref TypeList value);
        bool ToEnumList(ref EnumList value);
        bool ToUri(ref Uri value);
        bool ToVersion(ref Version value);
        bool ToReturnCodeList(ref ReturnCodeList value);
        bool ToAlias(ref IAlias value);
        bool ToOption(ref IOption value);
        bool ToNamespace(ref INamespace value);
        bool ToSecureString(ref SecureString value);
        bool ToEncoding(ref Encoding value);
        bool ToCultureInfo(ref CultureInfo value);
        bool ToPlugin(ref IPlugin value);
        bool ToExecute(ref IExecute value);
        bool ToCallback(ref ICallback value);
        bool ToRuleSet(ref IRuleSet value);
        bool ToIdentifier(ref IIdentifier value);
        bool ToByteArray(ref byte[] value);
    }
}
