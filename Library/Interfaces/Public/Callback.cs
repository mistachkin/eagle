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

#if EMIT
using System.Reflection;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a managed callback that bridges the Eagle
    /// engine to a native delegate.  It extends <see cref="ICallbackData" />
    /// with methods that build the underlying method or delegate, obtain the
    /// callback as a variety of well-known delegate types, fire those
    /// delegates directly, and invoke the callback as a script.
    /// </summary>
    [ObjectId("39dd45ed-3da6-48ee-83b1-1f2f5ec64500")]
    public interface ICallback : ICallbackData
    {
#if EMIT
        /// <summary>
        /// Builds (or rebuilds) the method that the callback invokes, based
        /// on the supplied return type, parameter types, and marshalling
        /// configuration.
        /// </summary>
        /// <param name="oldMethod">
        /// The original method to base the new method on, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="returnType">
        /// The return type of the method to build.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the method to build.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshalling flags for the method to build.
        /// </param>
        /// <param name="firstArgument">
        /// The first argument to bind to the method, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control how arguments and the return value are
        /// marshalled.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The built method, or null if it could not be built.
        /// </returns>
        MethodBase GetMethod(
            MethodBase oldMethod,
            Type returnType,
            TypeList parameterTypes,
            MarshalFlagsList parameterMarshalFlags,
            object firstArgument,
            MarshalFlags marshalFlags,
            ref Result error
        );
#endif

        /// <summary>
        /// Builds the delegate that the callback invokes, based on the
        /// supplied delegate type, return type, parameter types, and
        /// marshalling configuration.
        /// </summary>
        /// <param name="delegateType">
        /// The type of the delegate to build.
        /// </param>
        /// <param name="returnType">
        /// The return type of the delegate to build.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the delegate to build.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshalling flags for the delegate to build.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control how arguments and the return value are
        /// marshalled.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The built delegate, or null if it could not be built.
        /// </returns>
        Delegate GetDelegate(
            Type delegateType,
            Type returnType,
            TypeList parameterTypes,
            MarshalFlagsList parameterMarshalFlags,
            MarshalFlags marshalFlags,
            ref Result error
        );

        /// <summary>
        /// Gets the callback as a <see cref="System.AsyncCallback" />
        /// delegate.
        /// </summary>
        /// <returns>
        /// The callback as a <see cref="System.AsyncCallback" /> delegate.
        /// </returns>
        AsyncCallback GetAsyncCallback();
        /// <summary>
        /// Gets the callback as an <see cref="System.EventHandler" />
        /// delegate.
        /// </summary>
        /// <returns>
        /// The callback as an <see cref="System.EventHandler" /> delegate.
        /// </returns>
        EventHandler GetEventHandler();

        /// <summary>
        /// Gets the callback as a <see cref="System.Threading.ThreadStart" />
        /// delegate.
        /// </summary>
        /// <returns>
        /// The callback as a <see cref="System.Threading.ThreadStart" />
        /// delegate.
        /// </returns>
        ThreadStart GetThreadStart();
        /// <summary>
        /// Gets the callback as a
        /// <see cref="System.Threading.ParameterizedThreadStart" /> delegate.
        /// </summary>
        /// <returns>
        /// The callback as a
        /// <see cref="System.Threading.ParameterizedThreadStart" /> delegate.
        /// </returns>
        ParameterizedThreadStart GetParameterizedThreadStart();
        /// <summary>
        /// Gets the callback as a
        /// <see cref="System.Threading.WaitCallback" /> delegate.
        /// </summary>
        /// <returns>
        /// The callback as a <see cref="System.Threading.WaitCallback" />
        /// delegate.
        /// </returns>
        WaitCallback GetWaitCallback();

        /// <summary>
        /// Gets the callback as a <see cref="GenericCallback" /> delegate.
        /// </summary>
        /// <returns>
        /// The callback as a <see cref="GenericCallback" /> delegate.
        /// </returns>
        GenericCallback GetGenericCallback();
        /// <summary>
        /// Gets the callback as a <see cref="DynamicInvokeCallback" />
        /// delegate.
        /// </summary>
        /// <returns>
        /// The callback as a <see cref="DynamicInvokeCallback" /> delegate.
        /// </returns>
        DynamicInvokeCallback GetDynamicInvokeCallback();

        /// <summary>
        /// Fires the callback using the <see cref="System.AsyncCallback" />
        /// delegate signature.
        /// </summary>
        /// <param name="ar">
        /// The asynchronous operation result passed to the callback.
        /// </param>
        void FireAsyncCallback(
            IAsyncResult ar
        ); /* System.AsyncCallback */

        /// <summary>
        /// Fires the callback using the <see cref="System.EventHandler" />
        /// delegate signature.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the event.
        /// </param>
        void FireEventHandler(
            object sender,
            EventArgs e
        ); /* System.EventHandler */

        /// <summary>
        /// Fires the callback using the
        /// <see cref="System.Threading.ThreadStart" /> delegate signature.
        /// </summary>
        void FireThreadStart(); /* System.Threading.ThreadStart */
        /// <summary>
        /// Fires the callback using the
        /// <see cref="System.Threading.ParameterizedThreadStart" /> delegate
        /// signature.
        /// </summary>
        /// <param name="obj">
        /// The object passed to the callback when the thread starts.
        /// </param>
        void FireParameterizedThreadStart(
            object obj
        ); /* System.Threading.ParameterizedThreadStart */

        /// <summary>
        /// Fires the callback using the
        /// <see cref="System.Threading.WaitCallback" /> delegate signature.
        /// </summary>
        /// <param name="state">
        /// The state object passed to the callback.
        /// </param>
        void FireWaitCallback(
            object state
        ); /* System.Threading.WaitCallback */

        /// <summary>
        /// Fires the callback using the <see cref="GenericCallback" />
        /// delegate signature.
        /// </summary>
        void FireGenericCallback(); /* Eagle._Components.Public.Delegates.GenericCallback */

        /// <summary>
        /// Fires the callback using the dynamic invoke delegate signature.
        /// </summary>
        /// <param name="args">
        /// The arguments passed to the callback.
        /// </param>
        /// <returns>
        /// The value returned by the callback, if any.
        /// </returns>
        object FireDynamicInvokeCallback(
            params object[] args
        ); /* System.Delegate.DynamicInvoke */

        /// <summary>
        /// Fires the callback using the <see cref="System.AsyncCallback" />
        /// delegate signature, with extra script arguments.
        /// </summary>
        /// <param name="ar">
        /// The asynchronous operation result passed to the callback.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments passed to the callback.
        /// </param>
        void FireAsyncCallback(
            IAsyncResult ar,
            StringList arguments
        );

        /// <summary>
        /// Fires the callback using the <see cref="System.EventHandler" />
        /// delegate signature, with extra script arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the event.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments passed to the callback.
        /// </param>
        void FireEventHandler(
            object sender,
            EventArgs e,
            StringList arguments
        );

        /// <summary>
        /// Fires the callback using the
        /// <see cref="System.Threading.ThreadStart" /> delegate signature,
        /// with extra script arguments.
        /// </summary>
        /// <param name="arguments">
        /// The extra arguments passed to the callback.
        /// </param>
        void FireThreadStart(
            StringList arguments
        );

        /// <summary>
        /// Fires the callback using the
        /// <see cref="System.Threading.ParameterizedThreadStart" /> delegate
        /// signature, with extra script arguments.
        /// </summary>
        /// <param name="obj">
        /// The object passed to the callback when the thread starts.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments passed to the callback.
        /// </param>
        void FireParameterizedThreadStart(
            object obj,
            StringList arguments
        );

        /// <summary>
        /// Fires the callback using the
        /// <see cref="System.Threading.WaitCallback" /> delegate signature,
        /// with extra script arguments.
        /// </summary>
        /// <param name="state">
        /// The state object passed to the callback.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments passed to the callback.
        /// </param>
        void FireWaitCallback(
            object state,
            StringList arguments
        );

        /// <summary>
        /// Fires the callback using the <see cref="GenericCallback" />
        /// delegate signature, with extra script arguments.
        /// </summary>
        /// <param name="arguments">
        /// The extra arguments passed to the callback.
        /// </param>
        void FireGenericCallback(
            StringList arguments
        );

        /// <summary>
        /// Fires the callback using the dynamic invoke delegate signature,
        /// with extra script arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments passed to the callback.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments passed to the callback.
        /// </param>
        /// <returns>
        /// The value returned by the callback, if any.
        /// </returns>
        object FireDynamicInvokeCallback(
            object[] args,
            StringList arguments
        );

        /// <summary>
        /// Invokes the callback as a script using the supplied arguments.
        /// </summary>
        /// <param name="arguments">
        /// The arguments passed to the callback.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// callback.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Invoke(
            StringList arguments,
            ref Result result
        );

        /// <summary>
        /// Invokes the callback as a script using the supplied arguments,
        /// also reporting the error line number on failure.
        /// </summary>
        /// <param name="arguments">
        /// The arguments passed to the callback.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// callback.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, receives the line number where the error occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Invoke(
            StringList arguments,
            ref Result result,
            ref int errorLine
        );
    }
}
