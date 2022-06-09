/*
 * CommandCallbackWrapper.cs --
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
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    //
    // WARNING: This class must be public for it to work correctly; however,
    //          it cannot be created and is NOT designed for use outside of
    //          the Eagle core library itself.  In the future, it may change
    //          in completely incompatible ways.  You have been warned.
    //
    [ObjectId("a6ec2541-13ec-4f07-ab59-70d5d8fd52b4")]
    public sealed class CommandCallbackWrapper
    {
        #region Private Static Data
        //
        // NOTE: This is used to synchronize access to both the MethodInfo
        //       and the static callback lookup dictionary (both below).
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *HACK* This is purposely not read-only; however, it would not
        //       make much sense to change it to another value (except perhaps
        //       null?) because it will be looked up relative to this class.
        //
        private static string DynamicInvokeMethodName =
            "StaticFireDynamicInvokeCallback";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use by GetDynamicInvokeMethodInfo() only.
        //
        private static MethodInfo dynamicInvokeMethodInfo;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the static callback lookup dictionary.  It maps the
        //       CommandCallbackWrapper instances to their CommandCallback
        //       (as ICallback) instances.
        //
        private static readonly IDictionary<object, ICallback> callbacks =
            new Dictionary<object, ICallback>();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private CommandCallbackWrapper()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        internal static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (DynamicInvokeMethodName != null))
                {
                    localList.Add("DynamicInvokeMethodName",
                        FormatOps.DisplayString(DynamicInvokeMethodName));
                }

                if (empty || (dynamicInvokeMethodInfo != null))
                {
                    localList.Add("DynamicInvokeMethodInfo",
                        FormatOps.DelegateMethodName(
                            dynamicInvokeMethodInfo, true, true));
                }

                if (empty || ((callbacks != null) && (callbacks.Count > 0)))
                {
                    localList.Add("Callbacks", (callbacks != null) ?
                        callbacks.Count.ToString() : FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Command Callback Wrapper");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use by CommandCallback.GetDynamicDelegate()
        //       only.
        //
        internal static MethodInfo GetDynamicInvokeMethodInfo()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (dynamicInvokeMethodInfo == null)
                {
                    Type type = typeof(CommandCallbackWrapper);

                    if ((type != null) && (DynamicInvokeMethodName != null))
                    {
                        dynamicInvokeMethodInfo = type.GetMethod(
                            DynamicInvokeMethodName, ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PublicStaticMethod, true));
                    }
                }

                return dynamicInvokeMethodInfo;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use by CommandCallback.Dispose(bool) only.
        //
        internal static int Cleanup(
            ICallback callback
            )
        {
            IDictionary<object, ICallback> localCallbacks;

            lock (syncRoot)
            {
                if (callbacks == null)
                    return 0;

                localCallbacks = new Dictionary<object, ICallback>(callbacks);
            }

            int count = 0;

            foreach (KeyValuePair<object, ICallback> pair in localCallbacks)
            {
                if ((callback == null) ||
                    ObjectData.ReferenceEquals(pair.Value, callback))
                {
                    lock (syncRoot)
                    {
                        if ((callbacks != null) &&
                            callbacks.Remove(pair.Key))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        //
        // HACK: This is used by CommandCallback.GetDynamicDelegate(), in
        //       some circumstances, as the method called to service the
        //       incoming delegate (i.e. EmitDelegateWrapperMethodBody
        //       emits a "Callvirt" or "Call" MSIL instruction with this
        //       method as the destination).
        //
        public static object StaticFireDynamicInvokeCallback(
            object firstArgument,
            object[] args
            )
        {
            ICallback callback;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((firstArgument == null) || (callbacks == null) ||
                    !callbacks.TryGetValue(firstArgument, out callback))
                {
                    throw new ScriptException(String.Format(
                        "{0} for object {1} with hash code {2} not found",
                        typeof(ICallback), FormatOps.WrapOrNull(
                        firstArgument), FormatOps.WrapHashCode(
                        firstArgument)));
                }
            }

            TraceOps.DebugTrace(String.Format(
                "StaticFireDynamicInvokeCallback: " +
                "firstArgument = {0} ({1}), callback = {2}",
                FormatOps.WrapOrNull(firstArgument),
                FormatOps.WrapHashCode(firstArgument),
                FormatOps.WrapHashCode(callback)),
                typeof(CommandCallbackWrapper).Name,
                TracePriority.MarshalDebug2);

            //
            // NOTE: The "callback" variable could be null at this point.
            //
            return CommandCallback.StaticFireDynamicInvokeCallback(
                callback, args);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        //
        // NOTE: This is for use by CommandCallback.GetDynamicDelegate()
        //       only.
        //
        internal static ReturnCode Create(
            object value,       /* in */
            ICallback callback, /* in */
            ref Result error    /* out */
            )
        {
            if (value == null)
            {
                error = "invalid object instance";
                return ReturnCode.Error;
            }

            if (callback == null)
            {
                error = "invalid command callback";
                return ReturnCode.Error;
            }

            lock (syncRoot)
            {
                if (callbacks == null)
                {
                    error = "command callbacks not available";
                    return ReturnCode.Error;
                }

                callbacks[value] = callback;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
