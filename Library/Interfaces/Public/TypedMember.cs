/*
 * TypedMember.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("de49741d-ea05-4c95-9323-cdbc8eeaf73d")]
    public interface ITypedMember
    {
        Type Type { get; }
        object Object { get; }
        string MemberName { get; }
        string FullMemberName { get; }
        MemberInfo[] MemberInfo { get; }
        MethodInfo FirstMethodInfo { get; }
        bool ShouldHaveObject { get; }
    }
}
