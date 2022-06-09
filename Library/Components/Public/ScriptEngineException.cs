/*
 * ScriptEngineException.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if SERIALIZATION
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

using Eagle._Attributes;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c8ebc544-e5dd-4c08-b37c-e58381352f34")]
    public class ScriptEngineException : ScriptException
    {
        #region Public Constructors
        public ScriptEngineException()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptEngineException(
            string message
            )
            : base(message)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptEngineException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptEngineException(
            ReturnCode code,
            Result result
            )
            : base(code, result)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptEngineException(
            ReturnCode code,
            Result result,
            Exception innerException
            )
            : base(code, result, innerException)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        protected ScriptEngineException(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
            )
        {
            base.GetObjectData(info, context);
        }
#endif
        #endregion
    }
}
