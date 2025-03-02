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
    [ObjectId("6939e5d2-3952-4c30-a4f0-9fe618243e24")]
    public sealed class TypedMember :
            ITypedMember, IEqualityComparer<ITypedMember>
    {
        #region Public Constructors
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
        private ObjectFlags objectFlags;
        public ObjectFlags ObjectFlags
        {
            get { return objectFlags; }
            set { throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypedMember Members
        private Type type;
        public Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object @object;
        public object Object
        {
            get { return @object; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string memberName;
        public string MemberName
        {
            get { return memberName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fullMemberName;
        public string FullMemberName
        {
            get { return fullMemberName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private MemberInfo[] memberInfo;
        public MemberInfo[] MemberInfo
        {
            get { return memberInfo; }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private bool? shouldHaveObject;
        public bool ShouldHaveObject
        {
            get
            {
                return (shouldHaveObject != null) ?
                    (bool)shouldHaveObject : (@object != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
