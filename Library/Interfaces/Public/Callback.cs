/*
 * Callback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("39dd45ed-3da6-48ee-83b1-1f2f5ec64500")]
    public interface ICallback : ICallbackData
    {
        Delegate GetDelegate(
            Type delegateType, Type returnType, TypeList parameterTypes,
            MarshalFlagsList parameterMarshalFlags, bool throwOnBindFailure,
            ref Result error);

        AsyncCallback GetAsyncCallback();
        EventHandler GetEventHandler();
        ThreadStart GetThreadStart();
        ParameterizedThreadStart GetParameterizedThreadStart();
        GenericCallback GetGenericCallback();
        DynamicInvokeCallback GetDynamicInvokeCallback();

        void FireAsyncCallback(IAsyncResult ar); /* System.AsyncCallback */
        void FireEventHandler(object sender, EventArgs e); /* System.EventHandler */
        void FireThreadStart(); /* System.Threading.ThreadStart */
        void FireParameterizedThreadStart(object obj); /* System.Threading.ParameterizedThreadStart */
        void FireGenericCallback(); /* Eagle._Components.Public.Delegates.GenericCallback */
        object FireDynamicInvokeCallback(params object[] args); /* System.Delegate.DynamicInvoke */

        void FireAsyncCallback(IAsyncResult ar, StringList arguments);
        void FireEventHandler(object sender, EventArgs e, StringList arguments);
        void FireThreadStart(StringList arguments);
        void FireParameterizedThreadStart(object obj, StringList arguments);
        void FireGenericCallback(StringList arguments);
        object FireDynamicInvokeCallback(object[] args, StringList arguments);

        ReturnCode Invoke(StringList arguments, ref Result result);
        ReturnCode Invoke(StringList arguments, ref Result result, ref int errorLine);
    }
}
