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
using System.Diagnostics;

#if SERIALIZATION
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

using Eagle._Attributes;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents an exception raised by the Eagle script engine
    /// itself while preparing to evaluate, or while evaluating, a script.  It
    /// specializes <see cref="ScriptException" /> for errors that originate
    /// within the engine rather than within the script being evaluated.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c8ebc544-e5dd-4c08-b37c-e58381352f34")]
    public class ScriptEngineException : ScriptException
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with a default error return
        /// code and no associated message.
        /// </summary>
        public ScriptEngineException()
            : base()
        {
            Breakpoint();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified error
        /// message and a default error return code.
        /// </summary>
        /// <param name="message">
        /// The error message that describes this exception.  This parameter
        /// may be null.
        /// </param>
        public ScriptEngineException(
            string message
            )
            : base(message)
        {
            Breakpoint();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified error
        /// message, inner exception, and a default error return code.
        /// </summary>
        /// <param name="message">
        /// The error message that describes this exception.  This parameter
        /// may be null.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the underlying cause of this exception.  This
        /// parameter may be null.
        /// </param>
        public ScriptEngineException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
            Breakpoint();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified return code
        /// and result.
        /// </summary>
        /// <param name="code">
        /// The return code associated with this exception.
        /// </param>
        /// <param name="result">
        /// The result associated with this exception, used as its error
        /// message.  This parameter may be null.
        /// </param>
        public ScriptEngineException(
            ReturnCode code,
            Result result
            )
            : base(code, result)
        {
            Breakpoint();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified return code,
        /// result, and inner exception.
        /// </summary>
        /// <param name="code">
        /// The return code associated with this exception.
        /// </param>
        /// <param name="result">
        /// The result associated with this exception, used as its error
        /// message.  This parameter may be null.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the underlying cause of this exception.  This
        /// parameter may be null.
        /// </param>
        public ScriptEngineException(
            ReturnCode code,
            Result result,
            Exception innerException
            )
            : base(code, result, innerException)
        {
            Breakpoint();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method provides a convenient location for setting a debugger
        /// breakpoint.  It does nothing and is only present in debug builds.
        /// </summary>
        [Conditional("DEBUG")]
        private void Breakpoint()
        {
            //
            // TODO: Set debugger breakpoints here.
            //
            return;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class with serialized data.  This
        /// constructor is used during deserialization to reconstitute the
        /// exception object transmitted over a stream.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination of the
        /// serialized data.
        /// </param>
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
        /// <summary>
        /// This method populates the specified serialization information with
        /// the data needed to serialize this exception.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination of the
        /// serialized data.
        /// </param>
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
