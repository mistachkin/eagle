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
using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("c71b2e97-868d-453d-94f5-53e3f31e16ec")]
    public sealed class TypedInstance : IHaveObjectFlags, ITypedInstance
    {
        #region Public Constructors
        public TypedInstance(
            Type type,
            ObjectFlags objectFlags,
            string objectName,
            string fullObjectName,
            object @object,
            string[] extraParts
            )
        {
            this.type = type;
            this.objectFlags = objectFlags;
            this.objectName = objectName;
            this.fullObjectName = fullObjectName;
            this.@object = @object;
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

        private object @object;
        public object Object
        {
            get { return @object; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string[] extraParts;
        public string[] ExtraParts
        {
            get { return extraParts; }
        }
        #endregion
    }
}
