/*
 * InterpreterDisposedException.cs --
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
    /// <summary>
    /// This class represents the exception thrown when an operation is
    /// attempted on an interpreter that has already been disposed.  It derives
    /// from <see cref="ObjectDisposedException" /> and implements
    /// <see cref="IGetInterpreter" /> so that the disposed interpreter, if
    /// known, can be retrieved.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("59912fbe-86a1-4df9-bbf9-117bd0a8ff4d")]
    public class InterpreterDisposedException :
            ObjectDisposedException, IGetInterpreter
    {
        #region Private Static Data
        /// <summary>
        /// The total number of these exceptions that have been assigned an
        /// identifier since the process started.
        /// </summary>
        private static long count;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The unique identifier of this exception instance.
        /// </summary>
        private long id;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The disposed interpreter associated with this exception, if any.
        /// </summary>
#if SERIALIZATION
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an interpreter disposed exception with no associated
        /// object name.
        /// </summary>
        public InterpreterDisposedException()
            : this((string)null)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception for the specified
        /// object name.
        /// </summary>
        /// <param name="objectName">
        /// The name of the disposed object.
        /// </param>
        public InterpreterDisposedException(
            string objectName
            )
            : base(objectName)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception for the specified
        /// object name with a custom message.
        /// </summary>
        /// <param name="objectName">
        /// The name of the disposed object.
        /// </param>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        public InterpreterDisposedException(
            string objectName,
            string message
            )
            : base(objectName, message)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception with a custom message
        /// and an inner exception.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of this exception, if any.
        /// </param>
        public InterpreterDisposedException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception for the specified
        /// interpreter and object name with a custom message.
        /// </summary>
        /// <param name="interpreter">
        /// The disposed interpreter associated with this exception, if any.
        /// </param>
        /// <param name="objectName">
        /// The name of the disposed object.
        /// </param>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        public InterpreterDisposedException(
            Interpreter interpreter,
            string objectName,
            string message
            )
            : this(objectName, message)
        {
            MaybeSetIdAndIncrementCount();
            SetInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception for the specified
        /// interpreter with a custom message and an inner exception.
        /// </summary>
        /// <param name="interpreter">
        /// The disposed interpreter associated with this exception, if any.
        /// </param>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of this exception, if any.
        /// </param>
        public InterpreterDisposedException(
            Interpreter interpreter,
            string message,
            Exception innerException
            )
            : this(message, innerException)
        {
            MaybeSetIdAndIncrementCount();
            SetInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception, using the specified
        /// type to derive the disposed object name.
        /// </summary>
        /// <param name="type">
        /// The type of the disposed object; its name is used as the object
        /// name.  This parameter may be null.
        /// </param>
        public InterpreterDisposedException(
            Type type
            )
            : this(null, type)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception for the specified
        /// interpreter, using the specified type to derive the disposed object
        /// name.
        /// </summary>
        /// <param name="interpreter">
        /// The disposed interpreter associated with this exception, if any.
        /// </param>
        /// <param name="type">
        /// The type of the disposed object; its name is used as the object
        /// name.  This parameter may be null.
        /// </param>
        public InterpreterDisposedException(
            Interpreter interpreter,
            Type type
            )
            : this(interpreter, (type != null) ? type.Name : null, (string)null)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an interpreter disposed exception for the specified
        /// interpreter, using the specified type to derive the disposed object
        /// name, with a custom message.
        /// </summary>
        /// <param name="interpreter">
        /// The disposed interpreter associated with this exception, if any.
        /// </param>
        /// <param name="type">
        /// The type of the disposed object; its name is used as the object
        /// name.  This parameter may be null.
        /// </param>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        public InterpreterDisposedException(
            Interpreter interpreter,
            Type type,
            string message
            )
            : this(interpreter, (type != null) ? type.Name : null, message)
        {
            MaybeSetIdAndIncrementCount();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method provides a convenient, debug-only location at which to
        /// set a debugger breakpoint when one of these exceptions is created.
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
        /// Constructs an interpreter disposed exception from previously
        /// serialized data.  This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The serialization information that holds the serialized object
        /// data.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source and destination of
        /// the serialized stream.
        /// </param>
        protected InterpreterDisposedException(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            /* IGNORED */
            Interlocked.CompareExchange(
                ref id, (long)info.GetInt64("id"), 0);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// This method assigns a unique identifier to this exception if one
        /// has not already been assigned, incrementing the total count of
        /// these exceptions when it does so.
        /// </summary>
        /// <returns>
        /// True if an identifier was assigned by this call; otherwise, false.
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method associates a disposed interpreter with this exception.
        /// If the specified interpreter has been disposed, it is used;
        /// otherwise, the active interpreter is used if it has been disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The candidate interpreter to associate with this exception, if it
        /// has been disposed.  This parameter may be null.
        /// </param>
        protected void SetInterpreter(
            Interpreter interpreter
            )
        {
            //
            // NOTE: If the provided interpreter has been disposed, use it.
            //
            if ((interpreter != null) && interpreter.Disposed)
            {
                this.interpreter = interpreter;
                return;
            }

            //
            // NOTE: Otherwise, grab the active interpreter and check if it
            //       has been disposed.  If so, use it.
            //
            Interpreter activeInterpreter = Interpreter.GetActive();

            if ((activeInterpreter != null) && activeInterpreter.Disposed)
            {
                this.interpreter = activeInterpreter;
                return;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// Gets the disposed interpreter associated with this exception, if
        /// any.
        /// </summary>
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets the unique identifier of this exception instance.
        /// </summary>
        public virtual long Id
        {
            get { return Interlocked.CompareExchange(ref id, 0, 0); }
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
        /// The serialization information to populate with serialized object
        /// data.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source and destination of
        /// the serialized stream.
        /// </param>
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
            )
        {
            info.AddValue("id", Id);

            base.GetObjectData(info, context);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        /// <summary>
        /// This method adds diagnostic information about these exceptions (for
        /// example, the total number created) to the specified list.
        /// </summary>
        /// <param name="list">
        /// Upon return, the list to which the diagnostic information has been
        /// added.  This parameter may be null, in which case nothing is added.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included.
        /// </param>
        //
        // NOTE: Used by the _Hosts.Default.BuildInterpreterInfoList method.
        //
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
                list.Add("Interpreter Disposed Exception");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion
    }
}
