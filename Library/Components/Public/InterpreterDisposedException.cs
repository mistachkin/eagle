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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("59912fbe-86a1-4df9-bbf9-117bd0a8ff4d")]
    public class InterpreterDisposedException :
            ObjectDisposedException, IGetInterpreter
    {
        #region Private Static Data
        private static long count;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private long id;

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public InterpreterDisposedException()
            : this((string)null)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            string objectName
            )
            : base(objectName)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            string objectName,
            string message
            )
            : base(objectName, message)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

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

        public InterpreterDisposedException(
            Type type
            )
            : this(null, type)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            Interpreter interpreter,
            Type type
            )
            : this(interpreter, (type != null) ? type.Name : null, (string)null)
        {
            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        public virtual long Id
        {
            get { return Interlocked.CompareExchange(ref id, 0, 0); }
        }
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
            info.AddValue("id", Id);

            base.GetObjectData(info, context);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
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
