/*
 * ScriptException.cs --
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

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    //
    // HACK: When compiling for .NET Standard, this class derives from the
    //       System.Exception class instead of System.ApplicationException,
    //       thus allowing serialization without hitting an issue found on
    //       GitHub, here:
    //
    //       https://github.com/dotnet/corefx/issues/23584
    //
    /// <summary>
    /// This class represents an exception raised while evaluating Eagle
    /// scripts.  It carries the <see cref="ReturnCode" /> and <see cref="Result" />
    /// produced by the failing operation, along with the optional argument
    /// list associated with it, so that script-level error information can be
    /// surfaced through the normal managed exception mechanism.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("30409d09-40ec-488c-be4c-92d769d150a6")]
    public class ScriptException
#if NET_STANDARD_20
        : Exception
#else
        : ApplicationException
#endif
    {
        #region Private Static Data
        /// <summary>
        /// The total number of script exception instances that have been
        /// assigned an identifier since this class was first used.
        /// </summary>
        private static long count;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with a default error return
        /// code and no associated message.
        /// </summary>
        public ScriptException()
            : base()
        {
            this.returnCode = ReturnCode.Error;

            MaybeSetIdAndIncrementCount();
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
        public ScriptException(
            string message
            )
            : base(message)
        {
            this.returnCode = ReturnCode.Error;

            MaybeSetIdAndIncrementCount();
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
        public ScriptException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
            this.returnCode = ReturnCode.Error;

            MaybeSetIdAndIncrementCount();
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
        public ScriptException(
            ReturnCode code,
            Result result
            )
            : this(result)
        {
            this.returnCode = code;

            MaybeSetIdAndIncrementCount();
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
        public ScriptException(
            ReturnCode code,
            Result result,
            Exception innerException
            )
            : this(result, innerException)
        {
            this.returnCode = code;

            MaybeSetIdAndIncrementCount();
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

        #region Internal Constructors
        /// <summary>
        /// Constructs an instance of this class with the specified argument
        /// list, return code, result, and inner exception.
        /// </summary>
        /// <param name="arguments">
        /// The argument list associated with this exception.  This parameter
        /// may be null.
        /// </param>
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
        internal ScriptException(
            ArgumentList arguments,
            ReturnCode code,
            Result result,
            Exception innerException
            )
            : this(result, innerException)
        {
            this.arguments = arguments;
            this.returnCode = code;

            MaybeSetIdAndIncrementCount();
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
        protected ScriptException(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            arguments = info.GetValue(
                "arguments", typeof(ArgumentList)) as ArgumentList;

            returnCode = (ReturnCode)info.GetInt32("returnCode");

            /* IGNORED */
            Interlocked.CompareExchange(
                ref id, (long)info.GetInt64("id"), 0);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The argument list associated with this exception, if any.
        /// </summary>
        private ArgumentList arguments;

        /// <summary>
        /// Gets the argument list associated with this exception, if any.
        /// </summary>
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The return code associated with this exception.
        /// </summary>
        private ReturnCode returnCode;

        /// <summary>
        /// Gets the return code associated with this exception.
        /// </summary>
        public virtual ReturnCode ReturnCode
        {
            get { return returnCode; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier assigned to this exception, or zero if one has
        /// not yet been assigned.
        /// </summary>
        private long id;

        /// <summary>
        /// Gets the unique identifier assigned to this exception, or zero if
        /// one has not yet been assigned.
        /// </summary>
        public virtual long Id
        {
            get { return Interlocked.CompareExchange(ref id, 0, 0); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// This method assigns a unique identifier to this exception if it does
        /// not already have one, incrementing the total instance count when it
        /// does so.
        /// </summary>
        /// <returns>
        /// True if a new identifier was assigned to this exception; otherwise,
        /// false.
        /// </returns>
        protected bool MaybeSetIdAndIncrementCount()
        {
            if (Interlocked.CompareExchange(
                    ref id, GlobalState.NextId(), 0) == 0)
            {
                Interlocked.Increment(ref count);
                Breakpoint();
                return true;
            }

            return false;
        }
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
            info.AddValue("arguments", arguments);
            info.AddValue("returnCode", returnCode);
            info.AddValue("id", id);

            base.GetObjectData(info, context);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds rows describing the script exception subsystem to
        /// the specified list, primarily for introspection and diagnostic
        /// purposes.
        /// </summary>
        /// <param name="list">
        /// The list to add the descriptive rows to.  Upon success, the
        /// requested rows will have been added to this list.  This parameter
        /// may be null, in which case this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included in the added
        /// rows.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

            if (empty || (count != 0))
                localList.Add("Count", count.ToString());

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("Script Exception");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion
    }
}
