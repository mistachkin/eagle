/*
 * Option.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("d65817f4-92df-4ebb-a36c-d9483651d372")]
    public interface IOption : IIdentifier
    {
        Type Type { get; set; }
        OptionFlags Flags { get; set; }
        int GroupIndex { get; set; }
        int Index { get; set; }
        IVariant Value { get; set; }
        object InnerValue { get; }
        bool HasFlags(OptionFlags flags, bool all);
        bool IsStrict(OptionDictionary options);
        bool IsNoCase(OptionDictionary options);
        bool IsUnsafe(OptionDictionary options);
        bool IsAllowInteger(OptionDictionary options);
        bool IsIgnored(OptionDictionary options);
        bool MustHaveValue(OptionDictionary options);
        bool CanBePresent(OptionDictionary options, ref Result error);
        bool IsPresent(OptionDictionary options);
        bool IsPresent(OptionDictionary options, ref int nameIndex, ref int valueIndex);
        bool IsPresent(OptionDictionary options, ref IVariant value);
        void SetPresent(OptionDictionary options, bool present, int index, IVariant value);
        StringList ToList(IOption option);
        string FlagsToString();
        string ToString(IOption option);
        string ToString(OptionFlags flags);
    }
}
