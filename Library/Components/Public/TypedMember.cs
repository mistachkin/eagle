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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("6939e5d2-3952-4c30-a4f0-9fe618243e24")]
    public sealed class TypedMember : IHaveObjectFlags, ITypedMember
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
        #endregion
    }
}
