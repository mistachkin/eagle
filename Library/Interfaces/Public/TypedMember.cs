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
    /// <summary>
    /// This interface represents one or more reflected members of a type,
    /// together with the object instance they belong to and the names by
    /// which the member is known.  It extends <see cref="IHaveObjectFlags" />
    /// with the underlying object, its type, the member name and fully
    /// qualified member name, the reflected member information, and the first
    /// method overload (if any).
    /// </summary>
    [ObjectId("de49741d-ea05-4c95-9323-cdbc8eeaf73d")]
    public interface ITypedMember : IHaveObjectFlags
    {
        /// <summary>
        /// Gets the type that declares the member.
        /// </summary>
        Type Type { get; }
        /// <summary>
        /// Gets the underlying object instance the member belongs to.
        /// </summary>
        object Object { get; }
        /// <summary>
        /// Gets the short name of the member.
        /// </summary>
        string MemberName { get; }
        /// <summary>
        /// Gets the fully qualified name of the member.
        /// </summary>
        string FullMemberName { get; }
        /// <summary>
        /// Gets the reflected member information for the member.
        /// </summary>
        MemberInfo[] MemberInfo { get; }
        /// <summary>
        /// Gets the reflected method information for the first method overload,
        /// if any.
        /// </summary>
        MethodInfo FirstMethodInfo { get; }
        /// <summary>
        /// Gets a value indicating whether an object instance is required to
        /// access the member.
        /// </summary>
        bool ShouldHaveObject { get; }
        /// <summary>
        /// Resets this typed member to its default state.
        /// </summary>
        void Reset();
    }
}
