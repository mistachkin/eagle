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
    /// <summary>
    /// This class represents an object instance together with its type and the
    /// naming information used to resolve it.  It also provides value-based
    /// equality comparison for instances of <see cref="ITypedInstance" />.
    /// </summary>
    [ObjectId("c71b2e97-868d-453d-94f5-53e3f31e16ec")]
    public sealed class TypedInstance :
            ITypedInstance, IEqualityComparer<ITypedInstance>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class using the specified type,
        /// object flags, object value, and naming information.
        /// </summary>
        /// <param name="type">
        /// The type of the object instance.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags associated with the object instance.
        /// </param>
        /// <param name="object">
        /// The object instance itself.  This parameter may be null.
        /// </param>
        /// <param name="objectName">
        /// The name used to refer to the object instance.
        /// </param>
        /// <param name="fullObjectName">
        /// The fully qualified name used to refer to the object instance.
        /// </param>
        /// <param name="extraParts">
        /// The extra name parts associated with the object instance, if any.
        /// This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Stores the object flags associated with this typed instance.
        /// </summary>
        private ObjectFlags objectFlags;
        /// <summary>
        /// Gets or sets the object flags associated with this typed instance.
        /// Setting this property is not supported.
        /// </summary>
        public ObjectFlags ObjectFlags
        {
            get { return objectFlags; }
            set { throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypedInstance Members
        /// <summary>
        /// Stores the type of this typed instance.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets the type of this typed instance.
        /// </summary>
        public Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the object instance represented by this typed instance.
        /// </summary>
        private object @object;
        /// <summary>
        /// Gets the object instance represented by this typed instance.
        /// </summary>
        public object Object
        {
            get { return @object; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name used to refer to this typed instance.
        /// </summary>
        private string objectName;
        /// <summary>
        /// Gets the name used to refer to this typed instance.
        /// </summary>
        public string ObjectName
        {
            get { return objectName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the fully qualified name used to refer to this typed
        /// instance.
        /// </summary>
        private string fullObjectName;
        /// <summary>
        /// Gets the fully qualified name used to refer to this typed instance.
        /// </summary>
        public string FullObjectName
        {
            get { return fullObjectName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the extra name parts associated with this typed instance.
        /// </summary>
        private string[] extraParts;
        /// <summary>
        /// Gets the extra name parts associated with this typed instance.
        /// </summary>
        public string[] ExtraParts
        {
            get { return extraParts; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all of the state associated with this typed
        /// instance to its default value.
        /// </summary>
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
        /// <summary>
        /// This method determines whether two typed instances are equal by
        /// comparing their object flags, types, object values, names, fully
        /// qualified names, and extra name parts.
        /// </summary>
        /// <param name="left">
        /// The first typed instance to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second typed instance to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two typed instances are equal; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method calculates a hash code for the specified typed instance
        /// based on its object flags, type, object value, name, fully qualified
        /// name, and extra name parts.
        /// </summary>
        /// <param name="value">
        /// The typed instance for which a hash code is to be calculated.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The calculated hash code for the specified typed instance.
        /// </returns>
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
