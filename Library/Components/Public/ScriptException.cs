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
        private static long count;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ScriptException()
            : base()
        {
            this.returnCode = ReturnCode.Error;

            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptException(
            string message
            )
            : base(message)
        {
            this.returnCode = ReturnCode.Error;

            MaybeSetIdAndIncrementCount();
        }

        ///////////////////////////////////////////////////////////////////////

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
        private ArgumentList arguments;
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode returnCode;
        public virtual ReturnCode ReturnCode
        {
            get { return returnCode; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long id;
        public virtual long Id
        {
            get { return Interlocked.CompareExchange(ref id, 0, 0); }
        }
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
