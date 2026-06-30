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
using System.Collections.Generic;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a type member (or set of overloaded members)
    /// together with the type, object instance, and naming information used to
    /// resolve it.  It also provides value-based equality comparison for
    /// instances of <see cref="ITypedMember" />.
    /// </summary>
    [ObjectId("6939e5d2-3952-4c30-a4f0-9fe618243e24")]
    public sealed class TypedMember :
            ITypedMember, IEqualityComparer<ITypedMember>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class using the specified type,
        /// object flags, object value, naming information, member information,
        /// and object expectation.
        /// </summary>
        /// <param name="type">
        /// The type that declares the member.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags associated with the member.
        /// </param>
        /// <param name="object">
        /// The object instance associated with the member.  This parameter may
        /// be null.
        /// </param>
        /// <param name="memberName">
        /// The name used to refer to the member.
        /// </param>
        /// <param name="fullMemberName">
        /// The fully qualified name used to refer to the member.
        /// </param>
        /// <param name="memberInfo">
        /// The reflected member information for the member (or its overloads).
        /// This parameter may be null.
        /// </param>
        /// <param name="shouldHaveObject">
        /// Non-zero if the member is expected to have an associated object
        /// instance; null to infer this from the object value.
        /// </param>
        public TypedMember(
            Type type,
            ObjectFlags objectFlags,
            object @object,
            string memberName,
            string fullMemberName,
            MemberInfo[] memberInfo,
            bool? shouldHaveObject
            )
        {
            this.type = type;
            this.objectFlags = objectFlags;
            this.@object = @object;
            this.memberName = memberName;
            this.fullMemberName = fullMemberName;
            this.memberInfo = memberInfo;
            this.shouldHaveObject = shouldHaveObject;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        /// <summary>
        /// Stores the object flags associated with this typed member.
        /// </summary>
        private ObjectFlags objectFlags;
        /// <summary>
        /// Gets or sets the object flags associated with this typed member.
        /// Setting this property is not supported.
        /// </summary>
        public ObjectFlags ObjectFlags
        {
            get { return objectFlags; }
            set { throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypedMember Members
        /// <summary>
        /// Stores the type that declares this typed member.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets the type that declares this typed member.
        /// </summary>
        public Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the object instance associated with this typed member.
        /// </summary>
        private object @object;
        /// <summary>
        /// Gets the object instance associated with this typed member.
        /// </summary>
        public object Object
        {
            get { return @object; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name used to refer to this typed member.
        /// </summary>
        private string memberName;
        /// <summary>
        /// Gets the name used to refer to this typed member.
        /// </summary>
        public string MemberName
        {
            get { return memberName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the fully qualified name used to refer to this typed member.
        /// </summary>
        private string fullMemberName;
        /// <summary>
        /// Gets the fully qualified name used to refer to this typed member.
        /// </summary>
        public string FullMemberName
        {
            get { return fullMemberName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the reflected member information for this typed member (or its
        /// overloads).
        /// </summary>
        private MemberInfo[] memberInfo;
        /// <summary>
        /// Gets the reflected member information for this typed member (or its
        /// overloads).
        /// </summary>
        public MemberInfo[] MemberInfo
        {
            get { return memberInfo; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the first reflected member information entry for this typed
        /// member as a method, or null if there is none.
        /// </summary>
        public MethodInfo FirstMethodInfo
        {
            get
            {
                if ((memberInfo == null) || (memberInfo.Length < 1))
                    return null;

                return memberInfo[0] as MethodInfo;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores whether this typed member is expected to have an associated
        /// object instance, or null to infer this from the object value.
        /// </summary>
        private bool? shouldHaveObject;
        /// <summary>
        /// Gets a value indicating whether this typed member should have an
        /// associated object instance.  When the underlying expectation was not
        /// explicitly specified, this is inferred from whether the object value
        /// is non-null.
        /// </summary>
        public bool ShouldHaveObject
        {
            get
            {
                return (shouldHaveObject != null) ?
                    (bool)shouldHaveObject : (@object != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all of the state associated with this typed
        /// member to its default value.
        /// </summary>
        public void Reset()
        {
            objectFlags = ObjectFlags.None;
            type = null;
            @object = null;
            memberName = null;
            fullMemberName = null;
            memberInfo = null;
            shouldHaveObject = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<ITypedMember> Members
        /// <summary>
        /// This method determines whether two typed members are equal by
        /// comparing their object flags, types, object values, names, fully
        /// qualified names, and member information.
        /// </summary>
        /// <param name="left">
        /// The first typed member to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second typed member to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two typed members are equal; otherwise, false.
        /// </returns>
        public bool Equals(
            ITypedMember left,
            ITypedMember right
            )
        {
            if ((left == null) && (right == null))
            {
                return true;
            }
            else if ((left == null) || (right == null))
            {
                return false;
            }
            else
            {
                if (left.ObjectFlags != right.ObjectFlags)
                    return false;

                if (!Object.ReferenceEquals(left.Type, right.Type))
                    return false;

                if (!Object.ReferenceEquals(left.Object, right.Object))
                    return false;

                if (!SharedStringOps.SystemEquals(
                        left.MemberName, right.MemberName))
                {
                    return false;
                }

                if (!SharedStringOps.SystemEquals(
                        left.FullMemberName, right.FullMemberName))
                {
                    return false;
                }

                if (!Object.ReferenceEquals(
                        left.MemberInfo, right.MemberInfo))
                {
                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates a hash code for the specified typed member
        /// based on its object flags, type, object value, name, fully qualified
        /// name, and member information.
        /// </summary>
        /// <param name="value">
        /// The typed member for which a hash code is to be calculated.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The calculated hash code for the specified typed member.
        /// </returns>
        public int GetHashCode(
            ITypedMember value /* in */
            )
        {
            int result = 0;

            if (value != null)
            {
                foreach (object innerValue in new object[] {
                        value.ObjectFlags, value.Type,
                        value.Object, value.MemberName,
                        value.FullMemberName, value.MemberInfo
                    })
                {
                    if (innerValue == null)
                        continue;

                    result = CommonOps.HashCodes.Combine(
                        result, innerValue.GetHashCode());
                }
            }

            return result;
        }
        #endregion
    }
}
