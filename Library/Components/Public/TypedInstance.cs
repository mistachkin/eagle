/*
 * TypedInstance.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Public
{
    [ObjectId("c71b2e97-868d-453d-94f5-53e3f31e16ec")]
    public sealed class TypedInstance :
            ITypedInstance, IEqualityComparer<ITypedInstance>
    {
        #region Public Constructors
        public TypedInstance(
            Type type,
            ObjectFlags objectFlags,
            object @object,
            string objectName,
            string fullObjectName,
            string[] extraParts
            )
        {
            this.type = type;
            this.objectFlags = objectFlags;
            this.@object = @object;
            this.objectName = objectName;
            this.fullObjectName = fullObjectName;
            this.extraParts = extraParts;
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

        #region ITypedInstance Members
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

        private string objectName;
        public string ObjectName
        {
            get { return objectName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fullObjectName;
        public string FullObjectName
        {
            get { return fullObjectName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string[] extraParts;
        public string[] ExtraParts
        {
            get { return extraParts; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Reset()
        {
            objectFlags = ObjectFlags.None;
            type = null;
            objectName = null;
            fullObjectName = null;
            @object = null;
            extraParts = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<ITypedInstance> Members
        public bool Equals(
            ITypedInstance left,
            ITypedInstance right
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
                        left.ObjectName, right.ObjectName))
                {
                    return false;
                }

                if (!SharedStringOps.SystemEquals(
                        left.FullObjectName, right.FullObjectName))
                {
                    return false;
                }

                if (!Object.ReferenceEquals(
                        left.ExtraParts, right.ExtraParts))
                {
                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            ITypedInstance value /* in */
            )
        {
            int result = 0;

            if (value != null)
            {
                foreach (object innerValue in new object[] {
                        value.ObjectFlags, value.Type,
                        value.Object, value.ObjectName,
                        value.FullObjectName, value.ExtraParts
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
