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

using CallbackDictionary = System.Collections.Generic.Dictionary<
    object, Eagle._Interfaces.Public.ICallback>;

namespace Eagle._Components.Public
{
    //
    // WARNING: This class must be public for it to work correctly; however,
    //          it cannot be created and is NOT designed for use outside of
    //          the Eagle core library itself.  In the future, it may change
    //          in completely incompatible ways.  You have been warned.
    //
    /// <summary>
    /// This class provides the static plumbing used to dispatch dynamically
    /// generated delegates back to their associated <see cref="ICallback" />
    /// instances.  It maintains a lookup that maps the first argument of a
    /// dynamic invocation (the object or type the delegate was created for) to
    /// the command callback that should service it, and exposes the well-known
    /// static method that emitted delegate wrappers call into.  This class
    /// cannot be instantiated and is intended for use by the Eagle core library
    /// only.
    /// </summary>
    [ObjectId("a6ec2541-13ec-4f07-ab59-70d5d8fd52b4")]
    public sealed class CommandCallbackWrapper
    {
        #region Private Static Data
        //
        // NOTE: This is used to synchronize access to both the MethodInfo
        //       and the static callback lookup dictionary (both below).
        //
        /// <summary>
        /// This object is used to synchronize access to the method information
        /// and the static callback lookup dictionary maintained by this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *HACK* This is purposely not read-only; however, it would not
        //       make much sense to change it to another value (except perhaps
        //       null?) because it will be looked up relative to this class.
        //
        /// <summary>
        /// The name of the static method on this class that emitted delegate
        /// wrappers call into in order to dispatch a dynamic invocation.
        /// </summary>
        private static string DynamicInvokeMethodName =
            "StaticFireDynamicInvokeCallback";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use by GetDynamicInvokeMethodInfo() only.
        //
        /// <summary>
        /// The cached reflected method information for the static method named
        /// by <see cref="DynamicInvokeMethodName" />.
        /// </summary>
        private static MethodInfo dynamicInvokeMethodInfo;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the static callback lookup dictionary.  It maps the
        //       CommandCallbackWrapper instances to their CommandCallback
        //       (as ICallback) instances.
        //
        /// <summary>
        /// The static callback lookup dictionary.  It maps each first argument
        /// (object or type) to its associated command callback, represented as
        /// an <see cref="ICallback" /> instance.
        /// </summary>
        private static readonly CallbackDictionary callbacks =
            new CallbackDictionary();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class.  This constructor is private
        /// because the class is not designed to be instantiated.
        /// </summary>
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
        /// <summary>
        /// This method adds diagnostic information about the state of this
        /// class (the dynamic invoke method name and information, plus the
        /// number of registered callbacks) to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to add the diagnostic information to.  If this parameter is
        /// null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control how much detail is included and whether
        /// empty values are emitted.
        /// </param>
        internal static void AddInfo(
            StringPairList list,    /* in */
            DetailFlags detailFlags /* in */
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

        /// <summary>
        /// This method attempts to locate the command callback associated with
        /// the specified first argument.  The first argument itself is checked
        /// first, followed by a lookup of the argument and then its type within
        /// the static callback lookup dictionary.
        /// </summary>
        /// <param name="firstArgument">
        /// The object or type to find the associated command callback for.
        /// </param>
        /// <param name="callback">
        /// Upon success, receives the command callback associated with the
        /// first argument; otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if a command callback was found; otherwise, false.
        /// </returns>
        private static bool TryGetCallback(
            object firstArgument,  /* in */
            out ICallback callback /* out */
            )
        {
            callback = firstArgument as ICallback;

            if (callback != null)
                return true;

            if (firstArgument == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (callbacks == null)
                    return false;

                if (callbacks.TryGetValue(firstArgument, out callback))
                    return true;

                //
                // TODO: Contemplate putting this lookup into a loop where
                //       we traverse through the base type up to the root,
                //       i.e. typeof(object).
                //
                Type firstArgumentType = firstArgument.GetType();

                if (callbacks.TryGetValue(firstArgumentType, out callback))
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use by CommandCallback.GetDynamicDelegate()
        //       only.
        //
        /// <summary>
        /// This method returns the reflected method information for the static
        /// dynamic invoke method on this class, looking it up and caching it on
        /// first use.
        /// </summary>
        /// <returns>
        /// The reflected method information for the static dynamic invoke
        /// method, or null if it could not be resolved.
        /// </returns>
        internal static MethodInfo GetDynamicInvokeMethodInfo()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (dynamicInvokeMethodInfo == null)
                {
                    Type type = typeof(CommandCallbackWrapper);

                    if ((type != null) &&
                        (DynamicInvokeMethodName != null))
                    {
                        dynamicInvokeMethodInfo = type.GetMethod(
                            DynamicInvokeMethodName,
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PublicStaticMethod,
                                true));
                    }
                }

                return dynamicInvokeMethodInfo;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use by CommandCallback.Dispose(bool) only.
        //
        /// <summary>
        /// This method removes registered callbacks from the static callback
        /// lookup dictionary.  When a specific callback is supplied, only the
        /// entries referring to it are removed; otherwise, all entries are
        /// removed.
        /// </summary>
        /// <param name="callback">
        /// The command callback whose entries should be removed.  If this
        /// parameter is null, all registered callbacks are removed.
        /// </param>
        /// <returns>
        /// The number of entries that were removed from the dictionary.
        /// </returns>
        internal static int Cleanup(
            ICallback callback /* in */
            )
        {
            CallbackDictionary localCallbacks;

            lock (syncRoot)
            {
                if (callbacks == null)
                    return 0;

                localCallbacks = new CallbackDictionary(callbacks);
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
        // HACK: This is used by CommandCallback.GetDynamicDelegate and
        //       CommandCallback.GetMethod, in some circumstances, as
        //       methods called to service the incoming delegate (e.g.
        //       EmitDelegateWrapperMethodBody) emits a "Callvirt" or
        //       "Call" MSIL instruction with this method as the
        //       destination).  Quite similar handling also applies to
        //       the CommandCallback.GetMethod method.
        //
        /// <summary>
        /// This method is the well-known entry point that emitted delegate
        /// wrappers call into.  It locates the command callback associated with
        /// the specified first argument and fires it using the supplied
        /// arguments.
        /// </summary>
        /// <param name="firstArgument">
        /// The object or type used to locate the command callback to fire.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the located command callback.
        /// </param>
        /// <returns>
        /// The value returned by the fired command callback.
        /// </returns>
        public static object StaticFireDynamicInvokeCallback(
            object firstArgument, /* in */
            object[] args         /* in */
            )
        {
            ICallback callback;

            if (!TryGetCallback(firstArgument, out callback))
            {
                throw new ScriptException(String.Format(
                    "{0} for object {1} with hash code {2} not found",
                    typeof(ICallback), FormatOps.WrapOrNull(
                    firstArgument), FormatOps.WrapHashCode(
                    firstArgument)));
            }

#if false
            TraceOps.DebugTrace(String.Format(
                "StaticFireDynamicInvokeCallback: " +
                "firstArgument = {0} ({1}), callback = {2}",
                FormatOps.WrapOrNull(firstArgument),
                FormatOps.WrapHashCode(firstArgument),
                FormatOps.WrapHashCode(callback)),
                typeof(CommandCallbackWrapper).Name,
                TracePriority.MarshalDebug2);
#endif

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
        // NOTE: This is for use by the CommandCallback.GetDynamicDelegate
        //       and CommandCallback.GetMethod methods only.
        //
        /// <summary>
        /// This method registers a command callback in the static callback
        /// lookup dictionary, associating it with the specified first argument.
        /// </summary>
        /// <param name="firstArgument">
        /// The object or type to associate with the command callback.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="callback">
        /// The command callback to register.  This parameter cannot be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// callback could not be registered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode Create(
            object firstArgument, /* in */
            ICallback callback,   /* in */
            ref Result error      /* out */
            )
        {
            if (firstArgument == null)
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

                callbacks[firstArgument] = callback;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
