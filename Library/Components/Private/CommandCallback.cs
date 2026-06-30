/*
 * CommandCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _ClientData = Eagle._Components.Public.ClientData;
using CCW = Eagle._Components.Public.CommandCallbackWrapper;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class represents a script-based callback that bridges a managed
    /// delegate (or direct invocation) to an Eagle script.  When the associated
    /// delegate or event fires, this class evaluates the configured callback
    /// arguments as a script in the target interpreter, optionally marshaling
    /// the incoming arguments into opaque object handles, handling by-reference
    /// (output) parameters, and converting the script result back into a return
    /// value.  Instances may be reused for a given interpreter when the callback
    /// script matches.  It implements <see cref="IGetInterpreter" />,
    /// <see cref="ICallback" />, <see cref="IExecute" />, and is disposable.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("ee26fe9d-bbe4-42af-be2d-d52952acc9a6")]
    internal sealed class CommandCallback :
            IGetInterpreter, ICallback, IExecute, IDisposable
    {
        #region Private Static Data
        //
        // NOTE: This is the total number of times an instance of this class
        //       was reused for an interpreter in this AppDomain.
        //
        // NOTE: An instance can only be reused, for a particular interpreter,
        //       if the callback script matches.
        //
        /// <summary>
        /// The total number of times an instance of this class was reused for
        /// an interpreter in this application domain.
        /// </summary>
        private static long fetchedCount;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the total number of times an instance of this class
        //       was added to an interpreter in this AppDomain.
        //
        /// <summary>
        /// The total number of times an instance of this class was added to an
        /// interpreter in this application domain.
        /// </summary>
        private static long addedCount;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the total number of times a System.Delegate was able
        //       to be reused by the GetDelegate method before it checked the
        //       method signature.
        //
        /// <summary>
        /// The total number of times a delegate was able to be reused by the
        /// GetDelegate method before it checked the method signature.
        /// </summary>
        private static long reused1Count;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the total number of times a System.Delegate was able
        //       to be reused by the GetDelegate method after it checked the
        //       method signature.
        //
        /// <summary>
        /// The total number of times a delegate was able to be reused by the
        /// GetDelegate method after it checked the method signature.
        /// </summary>
        private static long reused2Count;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the total number of times a System.Delegate needed to
        //       be created by the GetDelegate method.
        //
        /// <summary>
        /// The total number of times a delegate needed to be created by the
        /// GetDelegate method.
        /// </summary>
        private static long createdCount;

        ///////////////////////////////////////////////////////////////////////

        #region Dynamic Delegate Support
        //
        // NOTE: This is used to synchronize access to the MethodInfo.
        //
        /// <summary>
        /// This object is used to synchronize access to the dynamic invoke
        /// method information (and the associated static data).
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *HACK* This is purposely not read-only; however, it would not
        //       make much sense to change it to another value (except perhaps
        //       null?) because it will be looked up relative to this class.
        //
        /// <summary>
        /// The name of the static method used as the "trampoline" target when
        /// servicing dynamic delegate invocations.
        /// </summary>
        private static string DynamicInvokeMethodName =
            "StaticFireDynamicInvokeCallback";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use by GetDynamicInvokeMethodInfo() only.
        //
        /// <summary>
        /// The cached method information for the dynamic invoke "trampoline"
        /// method; this is for use by the GetDynamicInvokeMethodInfo method
        /// only.
        /// </summary>
        private static MethodInfo dynamicInvokeMethodInfo;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a command callback from the specified identity, flags,
        /// interpreter, and callback arguments.
        /// </summary>
        /// <param name="name">
        /// The name of this callback.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this callback.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this callback.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this callback.  This parameter may
        /// be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control how arguments and return values are
        /// marshaled for this callback.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used to control the behavior of this callback.
        /// </param>
        /// <param name="objectFlags">
        /// The flags used when creating opaque object handles for the callback
        /// arguments.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// The flags used to control the handling of by-reference (output)
        /// arguments.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that will be used to evaluate the callback script.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments forming the callback script.  This parameter
        /// may be null.
        /// </param>
        private CommandCallback(
            string name,
            string group,
            string description,
            IClientData clientData,
            MarshalFlags marshalFlags,
            CallbackFlags callbackFlags,
            ObjectFlags objectFlags,
            ByRefArgumentFlags byRefArgumentFlags,
            Interpreter interpreter,
            StringList arguments
            )
        {
            this.kind = IdentifierKind.Callback;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.marshalFlags = marshalFlags;
            this.callbackFlags = callbackFlags;
            this.byRefArgumentFlags = byRefArgumentFlags;
            this.objectFlags = objectFlags;
            this.interpreter = interpreter;
            this.arguments = arguments;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new command callback or returns an existing,
        /// matching one for the specified interpreter.  When no matching
        /// callback exists, a new instance is created, added to the interpreter,
        /// and returned.
        /// </summary>
        /// <param name="marshalFlags">
        /// The flags used to control how arguments and return values are
        /// marshaled for the callback.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used to control the behavior of the callback.
        /// </param>
        /// <param name="objectFlags">
        /// The flags used when creating opaque object handles for the callback
        /// arguments.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// The flags used to control the handling of by-reference (output)
        /// arguments.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that will be used to evaluate the callback script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the callback.  This parameter may
        /// be null.
        /// </param>
        /// <param name="name">
        /// The name of the callback.  When null, the string form of
        /// <paramref name="arguments" /> is used.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments forming the callback script.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The created or fetched callback instance, or null if it could not be
        /// created.
        /// </returns>
        public static ICallback Create(
            MarshalFlags marshalFlags,
            CallbackFlags callbackFlags,
            ObjectFlags objectFlags,
            ByRefArgumentFlags byRefArgumentFlags,
            Interpreter interpreter,
            IClientData clientData,
            string name,
            StringList arguments,
            ref Result error
            )
        {
            ICallback callback = null;

            //
            // NOTE: If the interpreter cannot currently be used for script
            //       evaluation, bail out now.
            //
            if (CheckInterpreter(interpreter, ref error))
            {
                //
                // NOTE: The "name" of the callback is the full string
                //       representation of the argument list.  We normalize
                //       null to empty string here because the underlying
                //       dictionary of callbacks in the interpreter cannot
                //       handle a null key.
                //
                if (name == null)
                {
                    name = (arguments != null) ?
                        arguments.ToString() : String.Empty;
                }

                //
                // NOTE: Attempt to locate the matching callback object in
                //       the interpreter.  If it is not found, create a new
                //       one based on the specified arguments, add it to the
                //       interpreter, and return it.
                //
                if (interpreter.GetCallback(
                        name, LookupFlags.Exists,
                        ref callback) != ReturnCode.Ok)
                {
                    bool success = false;

                    try
                    {
                        callback = new CommandCallback(
                            name, null, null, clientData,
                            marshalFlags, callbackFlags,
                            objectFlags, byRefArgumentFlags,
                            interpreter, arguments);

                        if (interpreter.AddCallback(
                                callback, clientData,
                                ref error) == ReturnCode.Ok)
                        {
                            success = true;

                            /* IGNORED */
                            Interlocked.Increment(ref addedCount);
                        }
                    }
                    finally
                    {
                        if (!success && (callback != null))
                        {
                            ObjectOps.TryDisposeOrComplain<ICallback>(
                                interpreter, ref callback);

                            callback = null;
                        }
                    }
                }
                else if (callback == null)
                {
                    error = String.Format(
                        "invalid callback returned for {0}",
                        FormatOps.DisplayName(name));
                }
                else
                {
                    /* IGNORED */
                    Interlocked.Increment(ref fetchedCount);
                }
            }

            return callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds diagnostic information about this class (e.g. the
        /// various reuse and creation counts) to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to add the diagnostic information to.  If this parameter is
        /// null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included in the
        /// resulting information.
        /// </param>
        public static void AddInfo(
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

                if (empty || (fetchedCount > 0))
                    localList.Add("FetchedCount", fetchedCount.ToString());

                if (empty || (addedCount > 0))
                    localList.Add("AddedCount", addedCount.ToString());

                if (empty || (reused1Count > 0))
                    localList.Add("Reused1Count", reused1Count.ToString());

                if (empty || (reused2Count > 0))
                    localList.Add("Reused2Count", reused2Count.ToString());

                if (empty || (createdCount > 0))
                    localList.Add("CreatedCount", createdCount.ToString());

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Command Callback");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used by GetDynamicDelegate(), in some circumstances,
        //       as the method called to service the incoming delegate (i.e.
        //       EmitDelegateWrapperMethodBody emits a "Callvirt" -OR- "Call"
        //       MSIL instruction with this method as the destination).
        //
        /* [static --> this] System.Delegate.DynamicInvoke */
        /// <summary>
        /// This method is used as the static "trampoline" target for dynamic
        /// delegate invocations; it forwards the call to the instance method on
        /// the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The callback instance whose dynamic invoke handler should be fired.
        /// This parameter cannot be null.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the callback.  This parameter may also
        /// receive modified values for any by-reference (output) arguments.
        /// </param>
        /// <returns>
        /// The return value produced by the callback.
        /// </returns>
        public static object StaticFireDynamicInvokeCallback(
            ICallback callback, /* in */
            object[] args       /* in, out */
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            return callback.FireDynamicInvokeCallback(args);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        //
        // NOTE: This is for use by GetDynamicDelegate() only.
        //
        /// <summary>
        /// This method returns (and lazily resolves) the method information for
        /// the static dynamic invoke "trampoline" method on this class.
        /// </summary>
        /// <returns>
        /// The method information for the dynamic invoke method, or null if it
        /// could not be resolved.
        /// </returns>
        private static MethodInfo GetDynamicInvokeMethodInfo()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (dynamicInvokeMethodInfo == null)
                {
                    Type type = typeof(CommandCallback);

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

        /// <summary>
        /// This method determines the actual delegate type that should be used,
        /// based on the requested type and whether dynamic or delegate-style
        /// callbacks are in effect.
        /// </summary>
        /// <param name="delegateType">
        /// The delegate type originally requested by the caller.
        /// </param>
        /// <param name="useDynamicCallback">
        /// Non-zero if a dynamic callback is being used.
        /// </param>
        /// <param name="isDelegate">
        /// Non-zero if the requested type is the base delegate type itself.
        /// </param>
        /// <param name="newDelegateType">
        /// Upon return, receives the delegate type that should actually be used,
        /// which may be null.
        /// </param>
        private static void GetNewDelegateType(
            Type delegateType,       /* in */
            bool useDynamicCallback, /* in */
            bool isDelegate,         /* in */
            out Type newDelegateType /* out */
            )
        {
            if (useDynamicCallback)
            {
                if (isDelegate)
                    newDelegateType = null;
                else
                    newDelegateType = delegateType;
            }
            else
            {
                if (isDelegate)
                    newDelegateType = typeof(GenericCallback);
                else
                    newDelegateType = delegateType;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the specified interpreter is valid and may
        /// currently be used for script evaluation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the interpreter is valid and usable; otherwise, false.
        /// </returns>
        private static bool CheckInterpreter(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            if (Interpreter.IsDeletedOrDisposed(
                    interpreter, false, ref error))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the specified script thread is valid, not
        /// disposed, and has an interpreter that may currently be used for
        /// script evaluation.
        /// </summary>
        /// <param name="scriptThread">
        /// The script thread to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the script thread is valid and usable; otherwise, false.
        /// </returns>
        private static bool CheckScriptThread(
            IScriptThread scriptThread,
            ref Result error
            )
        {
            if (scriptThread == null)
            {
                error = "invalid script thread";
                return false;
            }

            if (scriptThread.IsDisposed)
            {
                error = "script thread is disposed";
                return false;
            }

            return CheckInterpreter(scriptThread.Interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the core scheduling-related boolean flags from
        /// the specified callback flags.
        /// </summary>
        /// <param name="callbackFlags">
        /// The callback flags to process.
        /// </param>
        /// <param name="useOwner">
        /// Upon return, non-zero if the owner of the target interpreter should
        /// handle the callback.
        /// </param>
        /// <param name="resetCancel">
        /// Upon return, non-zero if the script cancellation state should be
        /// reset prior to evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Upon return, non-zero if the script cancellation state must be fully
        /// reset prior to evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously when the target is busy.
        /// </param>
        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out bool useOwner,
            out bool resetCancel,
            out bool mustResetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy
            )
        {
            useOwner = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.UseOwner, true);

            resetCancel = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ResetCancel, true);

            mustResetCancel = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.MustResetCancel, true);

            asynchronous = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Asynchronous, true);

            asynchronousIfBusy = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AsynchronousIfBusy, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the core scheduling-related boolean flags as
        /// well as the post-evaluation behavior flags from the specified
        /// callback flags.
        /// </summary>
        /// <param name="callbackFlags">
        /// The callback flags to process.
        /// </param>
        /// <param name="useOwner">
        /// Upon return, non-zero if the owner of the target interpreter should
        /// handle the callback.
        /// </param>
        /// <param name="resetCancel">
        /// Upon return, non-zero if the script cancellation state should be
        /// reset prior to evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Upon return, non-zero if the script cancellation state must be fully
        /// reset prior to evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously when the target is busy.
        /// </param>
        /// <param name="fireAndForget">
        /// Upon return, non-zero if the callback should be removed from the
        /// interpreter after it is fired.
        /// </param>
        /// <param name="complain">
        /// Upon return, non-zero if failures should be reported via the complain
        /// mechanism.
        /// </param>
        /// <param name="disposeThread">
        /// Upon return, non-zero if thread-specific data should be disposed
        /// after the callback completes.
        /// </param>
        /// <param name="throwOnError">
        /// Upon return, non-zero if a script error should result in an exception
        /// being thrown.
        /// </param>
        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out bool useOwner,
            out bool resetCancel,
            out bool mustResetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy,
            out bool fireAndForget,
            out bool complain,
            out bool disposeThread,
            out bool throwOnError
            )
        {
            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out mustResetCancel, out asynchronous,
                out asynchronousIfBusy);

            ///////////////////////////////////////////////////////////////////

            fireAndForget = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.FireAndForget, true);

            complain = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Complain, true);

            disposeThread = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.DisposeThread, true);

            throwOnError = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ThrowOnError, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the argument-marshaling, scheduling, and
        /// post-evaluation behavior flags from the specified callback flags.
        /// This overload omits the by-reference and return-value related flags.
        /// </summary>
        /// <param name="callbackFlags">
        /// The callback flags to process.
        /// </param>
        /// <param name="objectOptionType">
        /// Upon return, receives the object option type used when creating
        /// opaque object handles for the callback arguments.
        /// </param>
        /// <param name="needArguments">
        /// Upon return, non-zero if opaque object handles should be created for
        /// the callback arguments.
        /// </param>
        /// <param name="create">
        /// Upon return, non-zero if opaque object handles should be created.
        /// </param>
        /// <param name="dispose">
        /// Upon return, non-zero if created objects should be disposed.
        /// </param>
        /// <param name="alias">
        /// Upon return, non-zero if command aliases should be created for the
        /// objects.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, non-zero if raw command aliases should be created.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, non-zero if aliases for all members should be created.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, non-zero if an object reference should be added for each
        /// created alias.
        /// </param>
        /// <param name="toString">
        /// Upon return, non-zero if the string form of each object should be
        /// used.
        /// </param>
        /// <param name="useOwner">
        /// Upon return, non-zero if the owner of the target interpreter should
        /// handle the callback.
        /// </param>
        /// <param name="resetCancel">
        /// Upon return, non-zero if the script cancellation state should be
        /// reset prior to evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Upon return, non-zero if the script cancellation state must be fully
        /// reset prior to evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously when the target is busy.
        /// </param>
        /// <param name="fireAndForget">
        /// Upon return, non-zero if the callback should be removed from the
        /// interpreter after it is fired.
        /// </param>
        /// <param name="complain">
        /// Upon return, non-zero if failures should be reported via the complain
        /// mechanism.
        /// </param>
        /// <param name="disposeThread">
        /// Upon return, non-zero if thread-specific data should be disposed
        /// after the callback completes.
        /// </param>
        /// <param name="throwOnError">
        /// Upon return, non-zero if a script error should result in an exception
        /// being thrown.
        /// </param>
        /// <param name="useParameterNames">
        /// Upon return, non-zero if parameter names should be used when adding
        /// arguments to the callback script.
        /// </param>
        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out ObjectOptionType objectOptionType,
            out bool needArguments,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString,
            out bool useOwner,
            out bool resetCancel,
            out bool mustResetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy,
            out bool fireAndForget,
            out bool complain,
            out bool disposeThread,
            out bool throwOnError,
            out bool useParameterNames
            )
        {
            bool byRefStrict;
            bool returnValue;
            bool defaultValue;
            bool addReference;
            bool removeReference;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out mustResetCancel,
                out asynchronous, out asynchronousIfBusy, out byRefStrict,
                out returnValue, out defaultValue, out addReference,
                out removeReference, out fireAndForget, out complain,
                out disposeThread, out throwOnError, out useParameterNames);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the full set of argument-marshaling, by-
        /// reference, return-value, scheduling, and post-evaluation behavior
        /// flags from the specified callback flags.
        /// </summary>
        /// <param name="callbackFlags">
        /// The callback flags to process.
        /// </param>
        /// <param name="objectOptionType">
        /// Upon return, receives the object option type used when creating
        /// opaque object handles for the callback arguments.
        /// </param>
        /// <param name="needArguments">
        /// Upon return, non-zero if opaque object handles should be created for
        /// the callback arguments.
        /// </param>
        /// <param name="create">
        /// Upon return, non-zero if opaque object handles should be created.
        /// </param>
        /// <param name="dispose">
        /// Upon return, non-zero if created objects should be disposed.
        /// </param>
        /// <param name="alias">
        /// Upon return, non-zero if command aliases should be created for the
        /// objects.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, non-zero if raw command aliases should be created.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, non-zero if aliases for all members should be created.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, non-zero if an object reference should be added for each
        /// created alias.
        /// </param>
        /// <param name="toString">
        /// Upon return, non-zero if the string form of each object should be
        /// used.
        /// </param>
        /// <param name="useOwner">
        /// Upon return, non-zero if the owner of the target interpreter should
        /// handle the callback.
        /// </param>
        /// <param name="resetCancel">
        /// Upon return, non-zero if the script cancellation state should be
        /// reset prior to evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Upon return, non-zero if the script cancellation state must be fully
        /// reset prior to evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Upon return, non-zero if the callback script should be evaluated
        /// asynchronously when the target is busy.
        /// </param>
        /// <param name="byRefStrict">
        /// Upon return, non-zero if strict type checking should be applied to
        /// by-reference (output) arguments.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, non-zero if the script result should be converted into a
        /// return value.
        /// </param>
        /// <param name="defaultValue">
        /// Upon return, non-zero if the default value for the return type should
        /// be used.
        /// </param>
        /// <param name="addReference">
        /// Upon return, non-zero if an object reference should be added for the
        /// return value.
        /// </param>
        /// <param name="removeReference">
        /// Upon return, non-zero if an object reference should be removed for the
        /// return value.
        /// </param>
        /// <param name="fireAndForget">
        /// Upon return, non-zero if the callback should be removed from the
        /// interpreter after it is fired.
        /// </param>
        /// <param name="complain">
        /// Upon return, non-zero if failures should be reported via the complain
        /// mechanism.
        /// </param>
        /// <param name="disposeThread">
        /// Upon return, non-zero if thread-specific data should be disposed
        /// after the callback completes.
        /// </param>
        /// <param name="throwOnError">
        /// Upon return, non-zero if a script error should result in an exception
        /// being thrown.
        /// </param>
        /// <param name="useParameterNames">
        /// Upon return, non-zero if parameter names should be used when adding
        /// arguments to the callback script.
        /// </param>
        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out ObjectOptionType objectOptionType,
            out bool needArguments,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString,
            out bool useOwner,
            out bool resetCancel,
            out bool mustResetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy,
            out bool byRefStrict,
            out bool returnValue,
            out bool defaultValue,
            out bool addReference,
            out bool removeReference,
            out bool fireAndForget,
            out bool complain,
            out bool disposeThread,
            out bool throwOnError,
            out bool useParameterNames
            )
        {
            needArguments = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Arguments, true);

            create = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Create, true);

            dispose = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Dispose, true);

            alias = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Alias, true);

            aliasRaw = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AliasRaw, true);

            aliasAll = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AliasAll, true);

            aliasReference = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AliasReference, true);

            toString = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ToString, true);

            ///////////////////////////////////////////////////////////////////

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out mustResetCancel, out asynchronous, out asynchronousIfBusy);

            ///////////////////////////////////////////////////////////////////

            byRefStrict = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ByRefStrict, true);

            returnValue = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ReturnValue, true);

            defaultValue = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.DefaultValue, true);

            addReference = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AddReference, true);

            removeReference = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.RemoveReference, true);

            fireAndForget = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.FireAndForget, true);

            complain = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Complain, true);

            disposeThread = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.DisposeThread, true);

            throwOnError = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ThrowOnError, true);

            useParameterNames = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.UseParameterNames, true);

            ///////////////////////////////////////////////////////////////////

            objectOptionType = ObjectOptionType.FireCallback |
                ObjectOps.GetOptionType(aliasRaw, aliasAll);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the various delegate-creation boolean flags from
        /// the specified marshal flags.
        /// </summary>
        /// <param name="marshalFlags">
        /// The marshal flags to process.
        /// </param>
        /// <param name="throwOnBindFailure">
        /// Upon return, non-zero if delegate binding failures should throw an
        /// exception.
        /// </param>
        /// <param name="forceNewCallback">
        /// Upon return, non-zero if a new delegate should always be created even
        /// when a matching one already exists.
        /// </param>
        /// <param name="useDelegateCallback">
        /// Upon return, non-zero if the base delegate type callback handling is
        /// permitted.
        /// </param>
        /// <param name="useGenericCallback">
        /// Upon return, non-zero if generic callback handling is permitted.
        /// </param>
        /// <param name="useDynamicCallback">
        /// Upon return, non-zero if dynamic callback handling should be used.
        /// </param>
        /// <param name="useCallbackParameterNames">
        /// Upon return, non-zero if the parameter names from the callback method
        /// should be used.
        /// </param>
        private static void ProcessMarshalFlags(
            MarshalFlags marshalFlags,
            out bool throwOnBindFailure,
            out bool forceNewCallback,
            out bool useDelegateCallback,
            out bool useGenericCallback,
            out bool useDynamicCallback,
            out bool useCallbackParameterNames
            )
        {
            throwOnBindFailure = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.ThrowOnBindFailure, true);

            forceNewCallback = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.ForceNewCallback, true);

            useDelegateCallback = !FlagOps.HasFlags(
                marshalFlags, MarshalFlags.NoDelegateCallback, true);

            useGenericCallback = !FlagOps.HasFlags(
                marshalFlags, MarshalFlags.NoGenericCallback, true);

            useDynamicCallback = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.DynamicCallback, true);

            useCallbackParameterNames = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.CallbackParameterNames, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether thread interrupt and abort exceptions
        /// should be caught based on the specified callback flags.
        /// </summary>
        /// <param name="callbackFlags">
        /// The callback flags to check.
        /// </param>
        /// <returns>
        /// True if thread interrupts should be caught; otherwise, false.
        /// </returns>
        private static bool ShouldCatchInterrupt(
            CallbackFlags callbackFlags
            )
        {
            return FlagOps.HasFlags(
                callbackFlags, CallbackFlags.CatchInterrupt, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends an argument (optionally preceded by its parameter
        /// name) to the specified local argument list, creating the list if
        /// necessary.
        /// </summary>
        /// <param name="name">
        /// The parameter name to add before the value, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="value">
        /// The argument value to add.
        /// </param>
        /// <param name="localArguments">
        /// The local argument list to add to; if null, a new list is created and
        /// returned via this parameter.
        /// </param>
        private static void AddArgument(
            string name,                  /* in */
            string value,                 /* in */
            ref StringList localArguments /* in, out */
            )
        {
            if (localArguments == null)
                localArguments = new StringList();

            if (name != null)
                localArguments.Add(name);

            localArguments.Add(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends all of the specified arguments to the specified
        /// local argument list.
        /// </summary>
        /// <param name="localArguments">
        /// The local argument list to add to.  If this parameter is null, this
        /// method does nothing.
        /// </param>
        /// <param name="arguments">
        /// The arguments to add.  If this parameter is null, this method does
        /// nothing.
        /// </param>
        private static void AddArguments(
            StringList localArguments, /* in, out */
            StringList arguments       /* in */
            )
        {
            if ((localArguments == null) || (arguments == null))
                return;

            foreach (string argument in arguments)
                localArguments.Add(argument);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends all of the specified arguments to the specified
        /// local argument list, creating the list if necessary.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to add.  If this parameter is null, this method does
        /// nothing.
        /// </param>
        /// <param name="localArguments">
        /// The local argument list to add to; if null, a new list is created and
        /// returned via this parameter.
        /// </param>
        private static void AddArguments(
            StringList arguments,         /* in */
            ref StringList localArguments /* in, out */
            )
        {
            if (arguments != null)
            {
                if (localArguments == null)
                    localArguments = new StringList();

                AddArguments(localArguments, arguments);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the owner object associated with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose owner should be returned.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The owner of the interpreter, or null if there is none (or the
        /// interpreter is null).
        /// </returns>
        private static object GetOwner(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.GetOwner();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the owner of the specified script
        /// thread is currently busy.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.  This parameter may be null.
        /// </param>
        /// <param name="scriptThread">
        /// The script thread whose owner busy state should be checked.
        /// </param>
        /// <returns>
        /// True if the owner is busy; otherwise, false.
        /// </returns>
        private static bool IsScriptThreadOwnerBusy(
            Interpreter interpreter,
            IScriptThread scriptThread
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.InternalIsOwnerBusy(scriptThread);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter is currently
        /// busy.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter is busy; otherwise, false.
        /// </returns>
        private static bool IsInterpreterBusy(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.InternalIsBusy;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the cancel flags used when resetting the script
        /// cancellation state for a callback.
        /// </summary>
        /// <param name="mustResetCancel">
        /// Non-zero if the script cancellation state must be completely and
        /// globally reset.
        /// </param>
        /// <returns>
        /// The cancel flags to use.
        /// </returns>
        private static CancelFlags GetCancelFlags(
            bool mustResetCancel
            )
        {
            CancelFlags result = CancelFlags.CommandCallback;

            //
            // HACK: On occasion, you must be absolutely positively sure
            //       that the script cancellation state is completely and
            //       totally reset in order to make totally 100% sure that
            //       you can actually evaluate a script associated with a
            //       callback method.
            //
            if (mustResetCancel)
            {
                result |= CancelFlags.Global;
                result |= CancelFlags.IgnorePending;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the callback script by sending or queuing it to
        /// the owner of the target interpreter, expressed as a script thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the callback.
        /// </param>
        /// <param name="scriptThread">
        /// The script thread to which the callback script should be sent or
        /// queued.
        /// </param>
        /// <param name="arguments">
        /// The arguments forming the callback script.
        /// </param>
        /// <param name="resetCancel">
        /// Non-zero if the script cancellation state should be reset prior to
        /// evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Non-zero if the script cancellation state must be fully reset prior to
        /// evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero if the callback script should be queued asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Non-zero if the callback script should be queued asynchronously when
        /// the owner is busy.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this parameter will be modified to contain the line
        /// number associated with any script error (always zero for this
        /// method).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode InvokeUsingScriptThread(
            Interpreter interpreter,    /* in */
            IScriptThread scriptThread, /* in */
            StringList arguments,       /* in */
            bool resetCancel,           /* in */
            bool mustResetCancel,          /* in */
            bool asynchronous,          /* in */
            bool asynchronousIfBusy,    /* in */
            ref Result result,          /* out */
            ref int errorLine           /* out */
            )
        {
            //
            // NOTE: If the interpreter cannot currently be used for
            //       script evaluation, bail out now.
            //
            if (!CheckInterpreter(interpreter, ref result))
                return ReturnCode.Error;

            //
            // NOTE: If the script thread cannot currently be used
            //       for script evaluation, bail out now.
            //
            if (!CheckScriptThread(scriptThread, ref result))
                return ReturnCode.Error;

            //
            // NOTE: If the appropriate flag is set, reset the script
            //       cancellation flag for the target interpreter now.
            //
            if (resetCancel && !scriptThread.ResetCancel(
                    GetCancelFlags(mustResetCancel), ref result))
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: No calls into the IScriptThread will supply
            //       error line information; therefore, reset it
            //       to a well-defined value (zero) now.
            //
            errorLine = 0;

            //
            // NOTE: Should the callback script be asynchronously
            //       queued to the IScriptThread rather than being
            //       sent synchronously?
            //
            if (asynchronous || (asynchronousIfBusy &&
                IsScriptThreadOwnerBusy(interpreter, scriptThread)))
            {
                //
                // NOTE: Queue the resulting command as a script
                //       (with proper list quoting), to the owner
                //       of the target interpreter, for evaluation
                //       asynchronously.
                //
                if (scriptThread.Queue(arguments.ToString()))
                {
                    result = null;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = "could not queue script to thread";
                    return ReturnCode.Error;
                }
            }
            else
            {
                //
                // NOTE: Send the resulting command as a script
                //       (with proper list quoting), to the owner
                //       of the target interpreter, for evaluation
                //       synchronously.
                //
                return scriptThread.Send(
                    arguments.ToString(), ref result);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the callback script directly using the
        /// specified interpreter, either synchronously or asynchronously.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for evaluating the callback script.
        /// </param>
        /// <param name="arguments">
        /// The arguments forming the callback script.
        /// </param>
        /// <param name="resetCancel">
        /// Non-zero if the script cancellation state should be reset prior to
        /// evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Non-zero if the script cancellation state must be fully reset prior to
        /// evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero if the callback script should be evaluated asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Non-zero if the callback script should be evaluated asynchronously
        /// when the interpreter is busy.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this parameter will be modified to contain the line
        /// number associated with any script error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode InvokeUsingInterpreter(
            Interpreter interpreter, /* in */
            StringList arguments,    /* in */
            bool resetCancel,        /* in */
            bool mustResetCancel,       /* in */
            bool asynchronous,       /* in */
            bool asynchronousIfBusy, /* in */
            ref Result result,       /* out */
            ref int errorLine        /* out */
            )
        {
            //
            // NOTE: If the interpreter cannot currently be used for
            //       script evaluation, bail out now.
            //
            if (!CheckInterpreter(interpreter, ref result))
                return ReturnCode.Error;

            //
            // NOTE: If the appropriate flag is set, reset the script
            //       cancellation flag for the target interpreter now.
            //
            if (resetCancel && Engine.ResetCancel(
                    interpreter, GetCancelFlags(mustResetCancel),
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (asynchronous ||
                (asynchronousIfBusy && IsInterpreterBusy(interpreter)))
            {
                //
                // NOTE: Asynchronous script evaluation means the
                //       error line information will be unavailable;
                //       therefore, reset it to a well-defined value
                //       (zero) now.
                //
                errorLine = 0;

                //
                // NOTE: Evaluate the resulting command as a script
                //       (with proper list quoting), asynchronously.
                //
                Result error = null;

                if (interpreter.EvaluateScript(
                        arguments.ToString(), null, null,
                        ref error) == ReturnCode.Ok)
                {
                    result = null;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = error;
                    return ReturnCode.Error;
                }
            }
            else
            {
                //
                // NOTE: Evaluate the resulting command as a script
                //       (with proper list quoting).
                //
                return interpreter.EvaluateScript(
                    arguments.ToString(), ref result,
                    ref errorLine); /* EXEMPT */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method returns the interpreter associated with this callback.
        /// </summary>
        /// <returns>
        /// The interpreter associated with this callback, which may be null.
        /// </returns>
        private Interpreter GetInterpreter()
        {
            return interpreter;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends an argument (optionally preceded by the parameter
        /// name at the specified index) to the specified local argument list.
        /// </summary>
        /// <param name="index">
        /// The index of the parameter name to add before the value, if any; a
        /// negative or out-of-range value suppresses the name.
        /// </param>
        /// <param name="value">
        /// The argument value to add.
        /// </param>
        /// <param name="localArguments">
        /// The local argument list to add to; if null, a new list is created and
        /// returned via this parameter.
        /// </param>
        private void AddArgument(
            int index,                    /* in */
            string value,                 /* in */
            ref StringList localArguments /* in, out */
            )
        {
            string name = null;

            if ((parameterNames != null) &&
                (index >= 0) && (index < parameterNames.Count))
            {
                name = parameterNames[index];
            }

            AddArgument(name, value, ref localArguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the instance (original) arguments of this
        /// callback to the specified local argument list.
        /// </summary>
        /// <param name="localArguments">
        /// The local argument list to add to.
        /// </param>
        private void AddArguments(
            StringList localArguments /* in */
            )
        {
            AddArguments(localArguments, this.arguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a new delegate needs to be created
        /// based on whether the currently cached delegate matches the specified
        /// type information.
        /// </summary>
        /// <param name="newDelegateType">
        /// The delegate type required.  This parameter may be null.
        /// </param>
        /// <param name="returnType">
        /// The return type required.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The parameter types required.  This parameter may be null.
        /// </param>
        /// <param name="useOriginalDelegateType">
        /// Non-zero to compare against the original delegate type; zero to
        /// compare against the modified delegate type.
        /// </param>
        /// <returns>
        /// True if a new delegate must be created; otherwise, false.
        /// </returns>
        private bool NeedToCreateDelegate(
            Type newDelegateType,        /* in: OPTIONAL */
            Type returnType,             /* in: OPTIONAL */
            TypeList parameterTypes,     /* in: OPTIONAL */
            bool useOriginalDelegateType /* in */
            )
        {
            if (@delegate == null)
                return true;

            Type oldDelegateType = useOriginalDelegateType ?
                this.originalDelegateType : this.modifiedDelegateType;

            if (oldDelegateType != newDelegateType)
                return true;

            if (this.returnType != returnType)
                return true;

            if (!TypeList.Equals(this.parameterTypes, parameterTypes))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines and returns the method information that
        /// matches the signature required to service the specified delegate
        /// type, lazily creating the appropriate per-instance delegate handler.
        /// </summary>
        /// <param name="delegateType">
        /// The delegate type that needs to be serviced.
        /// </param>
        /// <param name="useDelegateCallback">
        /// Non-zero if the base delegate type should be treated as a parameter-
        /// less, return-less callback.
        /// </param>
        /// <param name="useGenericCallback">
        /// Non-zero if generic callback handling is permitted.
        /// </param>
        /// <param name="useDynamicCallback">
        /// Non-zero if dynamic callback handling should be used.
        /// </param>
        /// <param name="isDelegate">
        /// Non-zero if the specified type is the base delegate type itself.
        /// </param>
        /// <returns>
        /// The method information matching the required signature, or null if the
        /// delegate type is not supported.
        /// </returns>
        private MethodInfo GetMethodInfo(
            Type delegateType,        /* in */
            bool useDelegateCallback, /* in */
            bool useGenericCallback,  /* in */
            bool useDynamicCallback,  /* in */
            bool isDelegate           /* in */
            )
        {
            //
            // WARNING: This method is tricky because of the ThreadStart and
            //          GenericCallback delegate types.  They will both match
            //          any delegate method signature that has no parameters
            //          and no return value.
            //
#if EMIT
            if (useDynamicCallback ||
                ConversionOps.LooksLikeDynamicInvokeCallback(delegateType))
            {
                if (dynamicInvokeCallback == null)
                {
                    dynamicInvokeCallback = new DynamicInvokeCallback(
                        FireDynamicInvokeCallback);
                }

                return DelegateOps.GetInvokeMethod(isDelegate ?
                    typeof(DynamicInvokeCallback) : delegateType);
            }
            else
#endif
            if (ConversionOps.LooksLikeAsyncCallback(delegateType))
            {
                if (asyncCallback == null)
                    asyncCallback = new AsyncCallback(FireAsyncCallback);

                return asyncCallback.Method;
            }
            else if (ConversionOps.LooksLikeEventHandler(delegateType))
            {
                if (eventHandler == null)
                    eventHandler = new EventHandler(FireEventHandler);

                return eventHandler.Method;
            }
            else if (ConversionOps.LooksLikeParameterizedThreadStart(
                    delegateType))
            {
                if (parameterizedThreadStart == null)
                {
                    parameterizedThreadStart = new ParameterizedThreadStart(
                        FireParameterizedThreadStart);
                }

                return parameterizedThreadStart.Method;
            }
            else if (ConversionOps.LooksLikeWaitCallback(delegateType))
            {
                if (waitCallback == null)
                    waitCallback = new WaitCallback(FireWaitCallback);

                return waitCallback.Method;
            }
            else if (!isDelegate &&
                ConversionOps.LooksLikeThreadStart(delegateType))
            {
                if (threadStart == null)
                    threadStart = new ThreadStart(FireThreadStart);

                return threadStart.Method;
            }
            //
            // HACK: Fake that "System.Delegate" really means they want a
            //       delegate with no parameters and no return value (i.e.
            //       the same signature as ThreadStart and GenericCallback).
            //
            else if ((useDelegateCallback && isDelegate) ||
                (useGenericCallback &&
                    ConversionOps.LooksLikeGenericCallback(delegateType)))
            {
                if (genericCallback == null)
                {
                    genericCallback = new GenericCallback(
                        FireGenericCallback);
                }

                return genericCallback.Method;
            }
            else
            {
                //
                // NOTE: We have no idea what kind of method signature is
                //       required.
                //
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the method information for the dynamic invoke
        /// "trampoline" method to use when emitting a delegate wrapper, choosing
        /// between this class and the public callback wrapper class based on
        /// whether a managed delegate type is supplied.
        /// </summary>
        /// <param name="delegateType">
        /// The delegate type being wrapped; when null, the public callback
        /// wrapper class trampoline is used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The trampoline method information, or null on failure.
        /// </returns>
        private static MethodInfo GetMethodInfo(
            Type delegateType, /* in */
            ref Result error   /* out */
            )
        {
            MethodInfo methodInfo;
            string typeName;

            if (delegateType != null)
            {
                //
                // NOTE: Use the StaticFireDynamicInvokeCallback static
                //       method from this class as the method to act as
                //       a "trampoline" for the generated IL method body.
                //       This works because the created DynamicMethod is
                //       logically also part of this class.
                //
                typeName = typeof(CommandCallback).Name;
                methodInfo = GetDynamicInvokeMethodInfo();
            }
            else
            {
                //
                // NOTE: Use the StaticFireDynamicInvokeCallback static
                //       method from the CommandCallbackWrapper class as
                //       the method to act as a "trampoline" for the
                //       generated IL method body.  This is necessary
                //       because the new Delegate type logically resides
                //       outside of this assembly and can only access
                //       public members in this assembly.  Furthermore,
                //       in order for the Delegate and MethodInfo
                //       signatures to match up correctly (i.e. not throw
                //       runtime exceptions), this cannot be an instance
                //       method (why not?).
                //
                typeName = typeof(CCW).Name;
                methodInfo = CCW.GetDynamicInvokeMethodInfo();
            }

            if (methodInfo == null)
            {
                error = String.Format(
                    "missing \"{0}.StaticFireDynamicInvokeCallback\" method.",
                    typeName);

                return null;
            }

            return methodInfo;
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// This method creates an instance of the specified (generated) type,
        /// optionally setting its "first argument" static field so the generated
        /// trampoline can locate this callback when invoked directly.
        /// </summary>
        /// <param name="type">
        /// The type to create an instance of.
        /// </param>
        /// <param name="useFieldInfo">
        /// Non-zero if the "first argument" static field on the created instance
        /// should be populated.
        /// </param>
        /// <returns>
        /// The newly created instance.
        /// </returns>
        private static object CreateInstance(
            Type type,        /* in */
            bool useFieldInfo /* in */
            )
        {
            //
            // HACK: Always create instance of the (new) type, i.e.
            //       even if the field (below) is not found or not
            //       needed.
            //
            object result = Activator.CreateInstance(type);

            //
            // BUGFIX: In order to support scenarios where the (new)
            //         target method could be invoked directly (i.e.
            //         without going through the delegate), some new
            //         code is required here.  Namely, we must have
            //         a (brand new) type with its own static field
            //         that can hold the right object value for the
            //         "firstArgument" parameter value to the static
            //         StaticFireDynamicInvokeCallback (trampoline)
            //         method in the CommandCallbackWrapper class.
            //
            if (useFieldInfo)
            {
                BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PrivateStatic, true);

                FieldInfo fieldInfo = type.GetField(
                    DelegateOps.FirstArgumentFieldName, bindingFlags);

                if (fieldInfo != null)
                    fieldInfo.SetValue(result, result);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a dynamic delegate that wraps this callback,
        /// either by emitting a dynamic method for a known delegate type or by
        /// creating a managed delegate type and wrapper method on the fly.
        /// </summary>
        /// <param name="name">
        /// The name to use for the dynamic method.  When null, a name is
        /// generated.
        /// </param>
        /// <param name="returnType">
        /// The return type of the delegate.  This parameter cannot be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The parameter types of the delegate.  This parameter cannot be null.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshal flags, if any.  This parameter may be null.
        /// </param>
        /// <param name="throwOnBindFailure">
        /// Non-zero if delegate binding failures should throw an exception.
        /// </param>
        /// <param name="delegateType">
        /// The delegate type to create; when null, a new managed delegate type
        /// is created and returned via this parameter.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The created delegate, or null on failure.
        /// </returns>
        private Delegate GetDynamicDelegate(
            string name,                            /* in */
            Type returnType,                        /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in: OPTIONAL */
            bool throwOnBindFailure,                /* in */
            ref Type delegateType,                  /* in, out */
            ref Result error                        /* out */
            )
        {
            if (returnType == null)
            {
                error = "invalid return type";
                return null;
            }

            if (parameterTypes == null)
            {
                error = "invalid parameter types";
                return null;
            }

            Type newDelegateType = delegateType;

            MethodInfo methodInfo = GetMethodInfo(
                newDelegateType, ref error);

            if (methodInfo == null)
                return null;

            Interpreter interpreter = GetInterpreter();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            try
            {
                if (newDelegateType != null)
                {
                    TypeList newParameterTypes = new TypeList();

                    newParameterTypes.Add(typeof(ICallback));
                    newParameterTypes.AddRange(parameterTypes);

                    if (name == null)
                        name = DelegateOps.MakeDelegateName(interpreter);

                    DynamicMethod dynamicMethod = new DynamicMethod(
                        name, returnType, newParameterTypes.ToArray(),
                        GetType(), true);

                    ILGenerator generator = dynamicMethod.GetILGenerator();

                    DelegateOps.EmitDelegateWrapperMethodBody(
                        generator, methodInfo, returnType, parameterTypes,
                        true);

                    Delegate newDelegate = dynamicMethod.CreateDelegate(
                        newDelegateType, this);

                    if (newDelegate != null)
                    {
                        if (delegateType == null)
                            delegateType = newDelegateType;

                        return newDelegate;
                    }
                    else
                    {
                        error = String.Format(
                            "bad delegate of type {0} for dynamic method {1}",
                            newDelegateType, dynamicMethod);
                    }
                }
                else
                {
                    Type newWrapperType = null;

                    if ((DelegateOps.CreateManagedDelegateType(
                            interpreter, null, null, null, null, returnType,
                            parameterTypes, ref newDelegateType,
                            ref error) == ReturnCode.Ok) &&
                        (DelegateOps.CreateDelegateWrapperMethod(
                            interpreter, null, null, null, null, methodInfo,
                            returnType, parameterTypes, ref newWrapperType,
                            ref error) == ReturnCode.Ok))
                    {
                        object newObject = CreateInstance(newWrapperType, false);

                        if (CCW.Create(
                                newObject, this, ref error) == ReturnCode.Ok)
                        {
                            MethodInfo newMethodInfo = newWrapperType.GetMethod(
                                DelegateOps.InvokeMethodName);

                            Delegate newDelegate = Delegate.CreateDelegate(
                                newDelegateType, newObject, newMethodInfo,
                                throwOnBindFailure);

                            if (newDelegate != null)
                            {
                                if (delegateType == null)
                                    delegateType = newDelegateType;

                                return newDelegate;
                            }
                            else
                            {
                                error = String.Format(
                                    "bad delegate of type {0} for method {1}",
                                    newDelegateType, newMethodInfo);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the original delegate type for this
        /// callback is one of the thread-oriented delegate types (i.e. a thread
        /// start, parameterized thread start, or wait callback).
        /// </summary>
        /// <returns>
        /// True if the original delegate type is thread-oriented; otherwise,
        /// false.
        /// </returns>
        private bool IsOriginalDelegateForThread()
        {
            Type delegateType = this.originalDelegateType;

            if (ConversionOps.IsThreadStart(delegateType))
                return true;

            if (ConversionOps.IsParameterizedThreadStart(delegateType))
                return true;

            if (ConversionOps.IsWaitCallback(delegateType))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// This method stores the original and generated method information for
        /// this callback.
        /// </summary>
        /// <param name="oldMethod">
        /// The original method being wrapped.
        /// </param>
        /// <param name="newMethod">
        /// The generated wrapper method.
        /// </param>
        private void SetMethods(
            MethodBase oldMethod, /* in */
            MethodBase newMethod  /* in */
            )
        {
            this.oldMethod = oldMethod;
            this.newMethod = newMethod;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the delegate type, parameter, and return type
        /// information associated with the most recently created delegate for
        /// this callback.
        /// </summary>
        /// <param name="oldDelegateType">
        /// The original (requested) delegate type.
        /// </param>
        /// <param name="newDelegateType">
        /// The actual (modified) delegate type that was created.
        /// </param>
        /// <param name="newParameterNames">
        /// The parameter names associated with the delegate.
        /// </param>
        /// <param name="newReturnType">
        /// The return type of the delegate.
        /// </param>
        /// <param name="newParameterTypes">
        /// The parameter types of the delegate.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshal flags, if any.  This parameter may be null.
        /// </param>
        private void SetDelegate(
            Type oldDelegateType,                  /* in */
            Type newDelegateType,                  /* in */
            StringList newParameterNames,          /* in */
            Type newReturnType,                    /* in */
            TypeList newParameterTypes,            /* in */
            MarshalFlagsList parameterMarshalFlags /* in: OPTIONAL */
            )
        {
            this.originalDelegateType = oldDelegateType;
            this.modifiedDelegateType = newDelegateType;
            this.parameterNames = newParameterNames;
            this.returnType = newReturnType;
            this.parameterTypes = newParameterTypes;
            this.parameterMarshalFlags = parameterMarshalFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines which callback parameters are by-reference
        /// (output) parameters, sets up their temporary variable names, and
        /// builds the supporting collections used during argument fixup.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the temporary by-reference variable names
        /// should be set up.
        /// </param>
        /// <param name="argumentInfoList">
        /// Upon return, this parameter will be modified to contain the list of
        /// by-reference argument information, or remain null if none are by-
        /// reference.
        /// </param>
        /// <param name="argumentInfoDictionary">
        /// Upon return, this parameter will be modified to contain a dictionary
        /// mapping parameter index to its by-reference argument information.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrepareByRefArguments(
            Interpreter interpreter,                              /* in */
            ref ArgumentInfoList argumentInfoList,                /* in, out */
            ref IntArgumentInfoDictionary argumentInfoDictionary, /* in, out */
            ref Result error                                      /* out */
            )
        {
            if (parameterTypes == null)
                return ReturnCode.Ok;

            ReturnCode code = MarshalOps.GetByRefArgumentInfo(
                new TypeList(parameterTypes), parameterMarshalFlags,
                marshalFlags, ref argumentInfoList, ref error);

            if (code != ReturnCode.Ok)
                return code;

            if (argumentInfoList == null)
                return ReturnCode.Ok; /* NOTE: None are ByRef. */

            MarshalOps.SetupTemporaryByRefVariableNames(
                interpreter, argumentInfoList);

            foreach (ArgumentInfo argumentInfo in argumentInfoList)
            {
                if (argumentInfo == null)
                    continue;

                if (argumentInfoDictionary == null)
                    argumentInfoDictionary = new IntArgumentInfoDictionary();

                argumentInfoDictionary[argumentInfo.Index] = argumentInfo;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the callback script using the specified
        /// interpreter, deriving the scheduling behavior from the configured
        /// callback flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for evaluating the callback script.
        /// </param>
        /// <param name="arguments">
        /// The arguments to append to the callback script.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            ref Result result
            )
        {
            bool useOwner;
            bool resetCancel;
            bool mustResetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out mustResetCancel, out asynchronous,
                out asynchronousIfBusy);

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                mustResetCancel, asynchronous, asynchronousIfBusy,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method invokes the callback, using the configured callback
        /// flags to determine its invocation behavior.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when invoking the callback.
        /// </param>
        /// <param name="arguments">
        /// The arguments to pass to the callback.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result produced by the callback; upon
        /// failure, receives an error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, receives the line number where the error occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            ref Result result,
            ref int errorLine
            )
        {
            bool useOwner;
            bool resetCancel;
            bool mustResetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out mustResetCancel, out asynchronous,
                out asynchronousIfBusy);

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                mustResetCancel, asynchronous, asynchronousIfBusy,
                ref result, ref errorLine);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the callback script using the specified
        /// interpreter and explicit scheduling behavior, discarding any error
        /// line information.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for evaluating the callback script.
        /// </param>
        /// <param name="arguments">
        /// The arguments to append to the callback script.
        /// </param>
        /// <param name="useOwner">
        /// Non-zero if the owner of the target interpreter should handle the
        /// callback.
        /// </param>
        /// <param name="resetCancel">
        /// Non-zero if the script cancellation state should be reset prior to
        /// evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Non-zero if the script cancellation state must be fully reset prior to
        /// evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero if the callback script should be evaluated asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Non-zero if the callback script should be evaluated asynchronously
        /// when the target is busy.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            bool useOwner,
            bool resetCancel,
            bool mustResetCancel,
            bool asynchronous,
            bool asynchronousIfBusy,
            ref Result result
            )
        {
            int errorLine = 0;

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                mustResetCancel, asynchronous, asynchronousIfBusy,
                ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the callback script using the specified
        /// interpreter and explicit scheduling behavior, dispatching either to
        /// the interpreter's owner (a script thread or another interpreter) or
        /// to the interpreter directly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for evaluating the callback script.
        /// </param>
        /// <param name="arguments">
        /// The arguments to append to the callback script.
        /// </param>
        /// <param name="useOwner">
        /// Non-zero if the owner of the target interpreter should handle the
        /// callback.
        /// </param>
        /// <param name="resetCancel">
        /// Non-zero if the script cancellation state should be reset prior to
        /// evaluation.
        /// </param>
        /// <param name="mustResetCancel">
        /// Non-zero if the script cancellation state must be fully reset prior to
        /// evaluation.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero if the callback script should be evaluated asynchronously.
        /// </param>
        /// <param name="asynchronousIfBusy">
        /// Non-zero if the callback script should be evaluated asynchronously
        /// when the target is busy.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this parameter will be modified to contain the line
        /// number associated with any script error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            bool useOwner,
            bool resetCancel,
            bool mustResetCancel,
            bool asynchronous,
            bool asynchronousIfBusy,
            ref Result result,
            ref int errorLine
            )
        {
            //
            // NOTE: Construct a new local argument list.
            //
            StringList localArguments = new StringList();

            //
            // NOTE: Start with the original (instance) arguments, if any.
            //
            AddArguments(localArguments);

            //
            // NOTE: Append the arguments for this invocation, if any.
            //
            AddArguments(localArguments, arguments);

            //
            // NOTE: Does the creator of this callback want the owner of the
            //       target interpreter to handle it, instead of simply using
            //       the target interpreter directly?
            //
            if (useOwner)
            {
                //
                // NOTE: Attempt to fetch the owner of the target interpreter.
                //       The owner must be an IScriptThread or an interpreter.
                //       If that is not the case (i.e. a null value or a value
                //       of an unsupported type is returned), the invocation
                //       of this callback will be considered a failure.
                //
                object owner = GetOwner(interpreter);

                if (owner is IScriptThread)
                {
                    return InvokeUsingScriptThread(
                        interpreter, (IScriptThread)owner, localArguments,
                        resetCancel, mustResetCancel, asynchronous,
                        asynchronousIfBusy, ref result, ref errorLine);
                }
                else if (owner is Interpreter)
                {
                    return InvokeUsingInterpreter(
                        (Interpreter)owner, localArguments, resetCancel,
                        mustResetCancel, asynchronous, asynchronousIfBusy,
                        ref result, ref errorLine);
                }
                else
                {
                    //
                    // NOTE: If the owner of the target interpreter is not
                    //       an IScriptThread, we do not (currently) know
                    //       how to deal with it.
                    //
                    result = String.Format(
                        "owner of callback interpreter not an {0} or {1}",
                        typeof(IScriptThread).Name, typeof(Interpreter).Name);

                    return ReturnCode.Error;
                }
            }
            else
            {
                return InvokeUsingInterpreter(
                    interpreter, localArguments, resetCancel,
                    mustResetCancel, asynchronous, asynchronousIfBusy,
                    ref result, ref errorLine);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the type of a by-reference (output)
        /// argument value matches the corresponding parameter type, when strict
        /// checking is in effect.
        /// </summary>
        /// <param name="parameterIndex">
        /// The index of the parameter being checked.
        /// </param>
        /// <param name="parameterName">
        /// The name of the parameter being checked.
        /// </param>
        /// <param name="parameterType">
        /// The declared type of the parameter.
        /// </param>
        /// <param name="argType">
        /// The actual type of the argument value.
        /// </param>
        /// <param name="byRefStrict">
        /// Non-zero if strict by-reference type checking should be applied.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the types are compatible (or strict
        /// checking is not in effect); otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode CheckByRefArgumentType(
            int parameterIndex,
            string parameterName,
            Type parameterType,
            Type argType,
            bool byRefStrict,
            ref Result error
            )
        {
            if (byRefStrict || FlagOps.HasFlags(
                    byRefArgumentFlags, ByRefArgumentFlags.Strict, true))
            {
                if (!MarshalOps.IsSameValueType(
                        parameterType, argType) &&
                    !MarshalOps.IsSameReferenceType(
                        parameterType, argType, marshalFlags))
                {
                    error = String.Format(
                        "output parameter {0} type {1} does not " +
                        "match argument value type {2}",
                        FormatOps.ArgumentName(
                            parameterIndex, parameterName),
                        MarshalOps.GetErrorTypeName(parameterType),
                        MarshalOps.GetErrorTypeName(argType));

                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the final values of any by-reference (output)
        /// arguments from their temporary interpreter variables back into the
        /// supplied argument array, then unsets those temporary variables.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter holding the temporary by-reference variables.
        /// </param>
        /// <param name="argumentInfoList">
        /// The list of by-reference argument information to process.
        /// </param>
        /// <param name="args">
        /// The argument array whose by-reference elements should be updated.
        /// </param>
        /// <param name="byRefStrict">
        /// Non-zero if strict by-reference type checking should be applied.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode FixupByRefArguments(
            Interpreter interpreter,
            ArgumentInfoList argumentInfoList,
            object[] args,
            bool byRefStrict,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (argumentInfoList == null)
            {
                error = "invalid argument info";
                return ReturnCode.Error;
            }

            if (args == null)
            {
                error = "invalid argument array";
                return ReturnCode.Error;
            }

            int argumentLength = args.Length;

            foreach (ArgumentInfo argumentInfo in argumentInfoList)
            {
                if (argumentInfo == null)
                    continue;

                int parameterIndex = argumentInfo.Index;
                string parameterName = argumentInfo.Name;

                if ((parameterIndex < 0) || (parameterIndex >= args.Length))
                {
                    error = String.Format(
                        "output parameter {0} out-of-bounds, index {1} " +
                        "must be between 0 and {2}", FormatOps.ArgumentName(
                        parameterIndex, parameterName), parameterIndex,
                        argumentLength);

                    return ReturnCode.Error;
                }

                Result variableValue = null;

                if (interpreter.GetVariableValue(
                        VariableFlags.None, parameterName,
                        ref variableValue, ref error) == ReturnCode.Ok)
                {
                    //
                    // BUGBUG: This call uses 'true' for the 'addReference'
                    //         parameter to DoesObjectExist because the call
                    //         to unset the variable (just below) will remove
                    //         a reference and we do not want to automatically
                    //         dispose the underlying object(s), if any.  The
                    //         "correct" solution here would be to somehow
                    //         modify the UnsetVariable code to be capable of
                    //         skipping object reference removal and/or object
                    //         disposal; however, that would be complicated by
                    //         the fact that the UnsetVariable code (purposely)
                    //         has no knowledge of any specific variable traces
                    //         (including the object reference counting trace)
                    //         that may be present on a particular variable.
                    //
                    object arg = null;

                    if (interpreter.InternalDoesObjectExist(variableValue,
                            true, false, ref arg) != ReturnCode.Ok)
                    {
                        arg = variableValue.Value;
                    }

                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, parameterName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, unsetCode, unsetError);

                    Type parameterType = argumentInfo.Type;

                    Type argType = (arg != null) ?
                        arg.GetType() : typeof(object);

                    if (CheckByRefArgumentType(
                            parameterIndex, parameterName, parameterType,
                            argType, byRefStrict, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    args[parameterIndex] = arg;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the thread-specific data for the specified
        /// interpreter (for the current thread) when requested, or when the
        /// original delegate is one of the thread-oriented delegate types.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose thread-specific data may be disposed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="disposeThread">
        /// Non-zero if the thread-specific data should be disposed regardless of
        /// the original delegate type.
        /// </param>
        private void MaybeDisposeThread(
            Interpreter interpreter, /* in */
            bool disposeThread       /* in */
            )
        {
            //
            // NOTE: Cleanup all the thread-specific data for the current
            //       interpreter (for this thread, which is now exiting)
            //       unless this is the primary thread for the interpreter
            //       -OR- it is actively in use elsewhere in this thread.
            //
            // HACK: This is necessary for dynamic delegate usage due to
            //       the (remote?) possibility that a dynamic delegate is
            //       used in place of a thread-start delegate.
            //
            if ((interpreter != null) &&
                (disposeThread || IsOriginalDelegateForThread()))
            {
                /* IGNORED */
                interpreter.MaybeDisposeThread();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// Stores the interpreter associated with this callback.  This object is
        /// not owned by this callback.
        /// </summary>
        private Interpreter interpreter; /* NOT OWNED */
        /// <summary>
        /// Gets the interpreter associated with this callback.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return GetInterpreter(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this callback.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this callback.
        /// </summary>
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this callback.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this callback.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this callback.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this callback.
        /// </summary>
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this callback.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this callback.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this callback.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this callback.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this callback.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this callback.
        /// </summary>
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        /// <summary>
        /// Stores the object flags used when creating opaque object handles for
        /// the callback arguments.
        /// </summary>
        private ObjectFlags objectFlags;
        /// <summary>
        /// Gets or sets the object flags used when creating opaque object handles
        /// for the callback arguments.
        /// </summary>
        public ObjectFlags ObjectFlags
        {
            get { CheckDisposed(); return objectFlags; }
            set { CheckDisposed(); objectFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICallbackData Members
        /// <summary>
        /// Stores the flags used to control how arguments and return values are
        /// marshaled for this callback.
        /// </summary>
        private MarshalFlags marshalFlags;
        /// <summary>
        /// Gets the flags used to control how arguments and return values are
        /// marshaled for this callback.
        /// </summary>
        public MarshalFlags MarshalFlags
        {
            get { CheckDisposed(); return marshalFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags used to control the behavior of this callback.
        /// </summary>
        private CallbackFlags callbackFlags;
        /// <summary>
        /// Gets or sets the flags used to control the behavior of this callback.
        /// </summary>
        public CallbackFlags CallbackFlags
        {
            get { CheckDisposed(); return callbackFlags; }
            set { CheckDisposed(); callbackFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags used to control the handling of by-reference
        /// (output) arguments.
        /// </summary>
        private ByRefArgumentFlags byRefArgumentFlags;
        /// <summary>
        /// Gets or sets the flags used to control the handling of by-reference
        /// (output) arguments.
        /// </summary>
        public ByRefArgumentFlags ByRefArgumentFlags
        {
            get { CheckDisposed(); return byRefArgumentFlags; }
            set { CheckDisposed(); byRefArgumentFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of arguments forming the callback script.
        /// </summary>
        private StringList arguments;
        /// <summary>
        /// Gets the list of arguments forming the callback script.
        /// </summary>
        public StringList Arguments
        {
            get { CheckDisposed(); return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// Stores the original method that is being wrapped by this callback.
        /// </summary>
        private MethodBase oldMethod;
        /// <summary>
        /// Gets the original method that is being wrapped by this callback.
        /// </summary>
        public MethodBase OldMethod
        {
            get { CheckDisposed(); return oldMethod; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the generated wrapper method for this callback.
        /// </summary>
        private MethodBase newMethod;
        /// <summary>
        /// Gets the generated wrapper method for this callback.
        /// </summary>
        public MethodBase NewMethod
        {
            get { CheckDisposed(); return newMethod; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the most recently created delegate that wraps this callback.
        /// </summary>
        private Delegate @delegate;
        /// <summary>
        /// Gets the most recently created delegate that wraps this callback.
        /// </summary>
        public Delegate Delegate
        {
            get { CheckDisposed(); return @delegate; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the original (requested) delegate type for this callback.
        /// </summary>
        private Type originalDelegateType;
        /// <summary>
        /// Gets the original (requested) delegate type for this callback.
        /// </summary>
        public Type OriginalDelegateType
        {
            get { CheckDisposed(); return originalDelegateType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the actual (modified) delegate type for this callback.
        /// </summary>
        private Type modifiedDelegateType;
        /// <summary>
        /// Gets the actual (modified) delegate type for this callback.
        /// </summary>
        public Type ModifiedDelegateType
        {
            get { CheckDisposed(); return modifiedDelegateType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the parameter names associated with the delegate for this
        /// callback.
        /// </summary>
        private StringList parameterNames;
        /// <summary>
        /// Gets the parameter names associated with the delegate for this
        /// callback.
        /// </summary>
        public StringList ParameterNames
        {
            get { CheckDisposed(); return parameterNames; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the return type of the delegate for this callback.
        /// </summary>
        private Type returnType;
        /// <summary>
        /// Gets the return type of the delegate for this callback.
        /// </summary>
        public Type ReturnType
        {
            get { CheckDisposed(); return returnType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the parameter types of the delegate for this callback.
        /// </summary>
        private TypeList parameterTypes;
        /// <summary>
        /// Gets the parameter types of the delegate for this callback.
        /// </summary>
        public TypeList ParameterTypes
        {
            get { CheckDisposed(); return parameterTypes; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the per-parameter marshal flags for the delegate of this
        /// callback.
        /// </summary>
        private MarshalFlagsList parameterMarshalFlags;
        /// <summary>
        /// Gets the per-parameter marshal flags for the delegate of this
        /// callback.
        /// </summary>
        public MarshalFlagsList ParameterMarshalFlags
        {
            get { CheckDisposed(); return parameterMarshalFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached async callback delegate for this callback.
        /// </summary>
        private AsyncCallback asyncCallback;
        /// <summary>
        /// Gets the cached async callback delegate for this callback.
        /// </summary>
        public AsyncCallback AsyncCallback
        {
            get { CheckDisposed(); return asyncCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached event handler delegate for this callback.
        /// </summary>
        private EventHandler eventHandler;
        /// <summary>
        /// Gets the cached event handler delegate for this callback.
        /// </summary>
        public EventHandler EventHandler
        {
            get { CheckDisposed(); return eventHandler; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached thread start delegate for this callback.
        /// </summary>
        private ThreadStart threadStart;
        /// <summary>
        /// Gets the cached thread start delegate for this callback.
        /// </summary>
        public ThreadStart ThreadStart
        {
            get { CheckDisposed(); return threadStart; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached parameterized thread start delegate for this
        /// callback.
        /// </summary>
        private ParameterizedThreadStart parameterizedThreadStart;
        /// <summary>
        /// Gets the cached parameterized thread start delegate for this
        /// callback.
        /// </summary>
        public ParameterizedThreadStart ParameterizedThreadStart
        {
            get { CheckDisposed(); return parameterizedThreadStart; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached wait callback delegate for this callback.
        /// </summary>
        private WaitCallback waitCallback;
        /// <summary>
        /// Gets the cached wait callback delegate for this callback.
        /// </summary>
        public WaitCallback WaitCallback
        {
            get { CheckDisposed(); return waitCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached generic callback delegate for this callback.
        /// </summary>
        private GenericCallback genericCallback;
        /// <summary>
        /// Gets the cached generic callback delegate for this callback.
        /// </summary>
        public GenericCallback GenericCallback
        {
            get { CheckDisposed(); return genericCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached dynamic invoke callback delegate for this callback.
        /// </summary>
        private DynamicInvokeCallback dynamicInvokeCallback;
        /// <summary>
        /// Gets the cached dynamic invoke callback delegate for this callback.
        /// </summary>
        public DynamicInvokeCallback DynamicInvokeCallback
        {
            get { CheckDisposed(); return dynamicInvokeCallback; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICallback Members
        /// <summary>
        /// This method returns (lazily creating if necessary) the async callback
        /// delegate that fires this callback.
        /// </summary>
        /// <returns>
        /// The async callback delegate for this callback.
        /// </returns>
        public AsyncCallback GetAsyncCallback()
        {
            CheckDisposed();

            if (asyncCallback == null)
                asyncCallback = new AsyncCallback(FireAsyncCallback);

            return asyncCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns (lazily creating if necessary) the event handler
        /// delegate that fires this callback.
        /// </summary>
        /// <returns>
        /// The event handler delegate for this callback.
        /// </returns>
        public EventHandler GetEventHandler()
        {
            CheckDisposed();

            if (eventHandler == null)
                eventHandler = new EventHandler(FireEventHandler);

            return eventHandler;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns (lazily creating if necessary) the thread start
        /// delegate that fires this callback.
        /// </summary>
        /// <returns>
        /// The thread start delegate for this callback.
        /// </returns>
        public ThreadStart GetThreadStart()
        {
            CheckDisposed();

            if (threadStart == null)
                threadStart = new ThreadStart(FireThreadStart);

            return threadStart;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns (lazily creating if necessary) the parameterized
        /// thread start delegate that fires this callback.
        /// </summary>
        /// <returns>
        /// The parameterized thread start delegate for this callback.
        /// </returns>
        public ParameterizedThreadStart GetParameterizedThreadStart()
        {
            CheckDisposed();

            if (parameterizedThreadStart == null)
            {
                parameterizedThreadStart = new ParameterizedThreadStart(
                    FireParameterizedThreadStart);
            }

            return parameterizedThreadStart;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns (lazily creating if necessary) the wait callback
        /// delegate that fires this callback.
        /// </summary>
        /// <returns>
        /// The wait callback delegate for this callback.
        /// </returns>
        public WaitCallback GetWaitCallback()
        {
            CheckDisposed();

            if (waitCallback == null)
                waitCallback = new WaitCallback(FireWaitCallback);

            return waitCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns (lazily creating if necessary) the generic
        /// callback delegate that fires this callback.
        /// </summary>
        /// <returns>
        /// The generic callback delegate for this callback.
        /// </returns>
        public GenericCallback GetGenericCallback()
        {
            CheckDisposed();

            if (genericCallback == null)
                genericCallback = new GenericCallback(FireGenericCallback);

            return genericCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns (lazily creating if necessary) the dynamic invoke
        /// callback delegate that fires this callback.
        /// </summary>
        /// <returns>
        /// The dynamic invoke callback delegate for this callback.
        /// </returns>
        public DynamicInvokeCallback GetDynamicInvokeCallback()
        {
            CheckDisposed();

            if (dynamicInvokeCallback == null)
            {
                dynamicInvokeCallback = new DynamicInvokeCallback(
                    FireDynamicInvokeCallback);
            }

            return dynamicInvokeCallback;
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// This method generates a wrapper method that forwards calls of the
        /// specified original method signature to this callback, and records the
        /// original and generated methods.
        /// </summary>
        /// <param name="oldMethod">
        /// The original method whose signature should be wrapped.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="returnType">
        /// The return type of the method.  This parameter cannot be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The parameter types of the method.  This parameter cannot be null.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshal flags, if any.  This parameter may be null.
        /// </param>
        /// <param name="firstArgument">
        /// The object to use as the first (implicit) argument; when null, the
        /// declaring type of <paramref name="oldMethod" /> is used.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control how arguments and return values are
        /// marshaled.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The generated wrapper method, or null on failure.
        /// </returns>
        public MethodBase GetMethod(
            MethodBase oldMethod,                   /* in */
            Type returnType,                        /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in: OPTIONAL */
            object firstArgument,                   /* in: OPTIONAL */
            MarshalFlags marshalFlags,              /* in */
            ref Result error                        /* out */
            )
        {
            CheckDisposed();

            if (oldMethod == null)
            {
                error = "invalid old method";
                return null;
            }

            if (returnType == null)
            {
                error = "invalid return type";
                return null;
            }

            if (parameterTypes == null)
            {
                error = "invalid parameter types";
                return null;
            }

            MethodInfo newMethodInfo = GetMethodInfo(null, ref error);

            if (newMethodInfo == null)
                return null;

            Interpreter interpreter = GetInterpreter();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            try
            {
                Type newWrapperType = null;

                if (DelegateOps.CreateWrapperMethod(
                        interpreter, null, null, null, null,
                        newMethodInfo, returnType, parameterTypes,
                        oldMethod.IsStatic, ref newWrapperType,
                        ref error) == ReturnCode.Ok)
                {
                    object newObject = CreateInstance(
                        newWrapperType, true);

                    object newFirstArgument = (firstArgument != null) ?
                        firstArgument : oldMethod.DeclaringType;

                    if ((CCW.Create(newObject, this,
                            ref error) == ReturnCode.Ok) &&
                        (CCW.Create(newFirstArgument, this,
                            ref error) == ReturnCode.Ok))
                    {
                        MethodBase newMethod = newWrapperType.GetMethod(
                            DelegateOps.InvokeMethodName);

                        SetMethods(oldMethod, newMethod);

                        return newMethod;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a delegate of the specified type that fires this
        /// callback, reusing a previously created delegate when the type
        /// information matches and creating a new one otherwise.
        /// </summary>
        /// <param name="delegateType">
        /// The delegate type required.
        /// </param>
        /// <param name="returnType">
        /// The desired return type, which may override the inferred return type.
        /// This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The desired parameter types, which may override the inferred parameter
        /// types.  This parameter may be null.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// The per-parameter marshal flags, if any.  This parameter may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control how the delegate is created and how
        /// arguments and return values are marshaled.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The created or reused delegate, or null on failure.
        /// </returns>
        public Delegate GetDelegate(
            Type delegateType,                      /* in */
            Type returnType,                        /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in: OPTIONAL */
            MarshalFlags marshalFlags,              /* in */
            ref Result error                        /* out */
            )
        {
            CheckDisposed();

            //
            // NOTE: Process configured marshal flags into the various boolean
            //       flags.
            //
            bool throwOnBindFailure;
            bool forceNewCallback;
            bool useDelegateCallback;
            bool useGenericCallback;
            bool useDynamicCallback;
            bool useCallbackParameterNames;

            ProcessMarshalFlags(
                marshalFlags, out throwOnBindFailure,
                out forceNewCallback, out useDelegateCallback,
                out useGenericCallback, out useDynamicCallback,
                out useCallbackParameterNames);

            if (forceNewCallback || NeedToCreateDelegate(
                    delegateType, returnType, parameterTypes, true))
            {
                //
                // NOTE: This is the method metadata that will contain the type
                //       signature for the method to invoke.
                //
                MethodInfo methodInfo = null;

                //
                // NOTE: Determine if the specified delegate type is just the
                //       System.Delegate type itself (this requires some special
                //       handling).
                //
                bool isDelegate = ConversionOps.IsDelegate(delegateType);

                //
                // NOTE: Attempt to figure out what kind of method signature is
                //       required to service the delegate.
                //
                methodInfo = GetMethodInfo(
                    delegateType, useDelegateCallback, useGenericCallback,
                    useDynamicCallback, isDelegate);

                if (methodInfo != null)
                {
                    Type newDelegateType;

                    GetNewDelegateType(
                        delegateType, useDynamicCallback, isDelegate,
                        out newDelegateType);

                    StringList newParameterNames;

                    MarshalOps.GetParameterNames(methodInfo,
                        useCallbackParameterNames, out newParameterNames);

                    Type newReturnType;
                    TypeList newParameterTypes;

                    MarshalOps.GetReturnAndParameterTypes(
                        methodInfo, out newReturnType, out newParameterTypes);

                    //
                    // BUGFIX: Do not re-create a new (dynamic?) delegate
                    //         if one of the correct type information has
                    //         already been created.
                    //
                    if (!forceNewCallback && !NeedToCreateDelegate(
                            newDelegateType, newReturnType, newParameterTypes,
                            false))
                    {
                        /* IGNORED */
                        Interlocked.Increment(ref reused2Count);

                        return @delegate;
                    }

                    if (useDynamicCallback)
                    {
#if EMIT
                        if (returnType != null)
                            newReturnType = returnType;

                        if (parameterTypes != null)
                            newParameterTypes = parameterTypes;

                        @delegate = GetDynamicDelegate(
                            null, newReturnType, newParameterTypes,
                            parameterMarshalFlags, throwOnBindFailure,
                            ref newDelegateType, ref error);

                        if (@delegate != null)
                        {
                            SetDelegate(
                                delegateType, newDelegateType,
                                newParameterNames, newReturnType,
                                newParameterTypes, parameterMarshalFlags);

                            /* IGNORED */
                            Interlocked.Increment(ref createdCount);
                        }
#else
                        error = "not implemented";
#endif
                    }
                    else
                    {
                        @delegate = Delegate.CreateDelegate(
                            newDelegateType, this, methodInfo,
                            throwOnBindFailure); /* throw */

                        if (@delegate != null)
                        {
                            if (returnType != null)
                                newReturnType = returnType;

                            if (parameterTypes != null)
                                newParameterTypes = parameterTypes;

                            SetDelegate(
                                delegateType, newDelegateType,
                                newParameterNames, newReturnType,
                                newParameterTypes, parameterMarshalFlags);

                            /* IGNORED */
                            Interlocked.Increment(ref createdCount);
                        }
                    }
                }
                else
                {
                    error = ScriptOps.BadValue(
                        "unsupported", "delegate type", delegateType.FullName,
                        new string[] {
                            useDelegateCallback ?
                                typeof(Delegate).FullName : null,
                            typeof(AsyncCallback).FullName,
                            typeof(EventHandler).FullName,
                            typeof(ThreadStart).FullName,
                            typeof(ParameterizedThreadStart).FullName,
                            useGenericCallback ?
                                typeof(GenericCallback).FullName : null,
                            useDynamicCallback ?
                                typeof(DynamicInvokeCallback).FullName : null
                        }, null, null);
                }
            }
            else
            {
                /* IGNORED */
                Interlocked.Increment(ref reused1Count);
            }

            return @delegate;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to an async callback,
        /// without supplying any extra arguments.
        /// </summary>
        /// <param name="ar">
        /// The asynchronous result associated with the operation.
        /// </param>
        public void FireAsyncCallback(
            IAsyncResult ar
            ) /* System.AsyncCallback */
        {
            CheckDisposed();

            /* NO RESULT */
            FireAsyncCallback(ar, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to an async callback,
        /// optionally creating an opaque object handle for the asynchronous
        /// result and appending the supplied extra arguments.
        /// </summary>
        /// <param name="ar">
        /// The asynchronous result associated with the operation.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        public void FireAsyncCallback(
            IAsyncResult ar,
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool mustResetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;
            bool useParameterNames;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out mustResetCancel,
                out asynchronous, out asynchronousIfBusy,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError, out useParameterNames);

            Interpreter interpreter = GetInterpreter();

            try
            {
                ReturnCode code;
                Result result = null;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                StringList localArguments = null;

                //
                // NOTE: Do we want to create opaque object handles for
                //       the event arguments?
                //
                if (needArguments)
                {
                    //
                    // NOTE: Add opaque object handle to the interpreter
                    //       for the sender of the event.
                    //
                    code = MarshalOps.FixupReturnValue(
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo, null, objectFlags,
                        null, ObjectOps.GetInvokeOptions(objectOptionType),
                        objectOptionType, null, null, ar, create,
                        dispose, alias, aliasReference, toString,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;

                    //
                    // NOTE: Add the "ar" argument to the list.
                    //
                    AddArgument(
                        useParameterNames ? "ar" : null, result,
                        ref localArguments);
                }

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    mustResetCancel, asynchronous, asynchronousIfBusy,
                    ref result);

            done:

                try
                {
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                MaybeDisposeThread(interpreter, disposeThread);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to an event, without
        /// supplying any extra arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data associated with the event.
        /// </param>
        public void FireEventHandler(
            object sender,
            EventArgs e
            ) /* System.EventHandler */
        {
            CheckDisposed();

            /* NO RESULT */
            FireEventHandler(sender, e, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to an event, optionally
        /// creating opaque object handles for the sender and event data and
        /// appending the supplied extra arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event data associated with the event.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        public void FireEventHandler(
            object sender,
            EventArgs e,
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool mustResetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;
            bool useParameterNames;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out mustResetCancel,
                out asynchronous, out asynchronousIfBusy,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError, out useParameterNames);

            Interpreter interpreter = GetInterpreter();

            try
            {
                ReturnCode code;
                Result result = null;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                StringList localArguments = null;

                //
                // NOTE: Do we want to create opaque object handles for
                //       the event arguments?
                //
                if (needArguments)
                {
                    //
                    // NOTE: Add opaque object handle to the interpreter
                    //       for the sender of the event.
                    //
                    code = MarshalOps.FixupReturnValue(
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo, null, objectFlags,
                        null, ObjectOps.GetInvokeOptions(objectOptionType),
                        objectOptionType, null, null, sender, create,
                        dispose, alias, aliasReference, toString,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;

                    //
                    // NOTE: Add the "sender" argument to the list.
                    //
                    AddArgument(
                        useParameterNames ? "sender" : null, result,
                        ref localArguments);

                    //
                    // NOTE: Add an opaque object handle to the interpreter
                    //       for the data of the event.
                    //
                    code = MarshalOps.FixupReturnValue(
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo, null, objectFlags,
                        null, ObjectOps.GetInvokeOptions(objectOptionType),
                        objectOptionType, null, null, e, create,
                        dispose, alias, aliasReference, toString,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;

                    //
                    // NOTE: Add the "e" argument to the list.
                    //
                    AddArgument(
                        useParameterNames ? "e" : null, result,
                        ref localArguments);
                }

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    mustResetCancel, asynchronous, asynchronousIfBusy,
                    ref result);

            done:

                try
                {
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                MaybeDisposeThread(interpreter, disposeThread);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a thread start,
        /// without supplying any extra arguments, catching thread interrupt and
        /// abort exceptions as configured.
        /// </summary>
        public void FireThreadStart() /* System.Threading.ThreadStart */
        {
            CheckDisposed();

            bool shouldCatchInterrupt = ShouldCatchInterrupt(callbackFlags);

            try
            {
                /* NO RESULT */
                FireThreadStart(null);
            }
            catch (ThreadAbortException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (shouldCatchInterrupt)
                    Thread.ResetAbort();
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (!shouldCatchInterrupt)
                    throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a thread start,
        /// appending the supplied extra arguments.
        /// </summary>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        public void FireThreadStart(
            StringList arguments
            )
        {
            CheckDisposed();

            /* NO RESULT */
            FireWithoutParameters(arguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a parameterized thread
        /// start, without supplying any extra arguments, catching thread
        /// interrupt and abort exceptions as configured.
        /// </summary>
        /// <param name="obj">
        /// The object passed to the parameterized thread start.
        /// </param>
        public void FireParameterizedThreadStart(
            object obj
            ) /* System.Threading.ParameterizedThreadStart */
        {
            CheckDisposed();

            bool shouldCatchInterrupt = ShouldCatchInterrupt(callbackFlags);

            try
            {
                /* NO RESULT */
                FireParameterizedThreadStart(obj, null);
            }
            catch (ThreadAbortException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (shouldCatchInterrupt)
                    Thread.ResetAbort();
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (!shouldCatchInterrupt)
                    throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a parameterized thread
        /// start, optionally creating an opaque object handle for the supplied
        /// object and appending the supplied extra arguments.
        /// </summary>
        /// <param name="obj">
        /// The object passed to the parameterized thread start.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        public void FireParameterizedThreadStart(
            object obj,
            StringList arguments
            )
        {
            CheckDisposed();

            /* NO RESULT */
            FireWithOneParameter(obj, arguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a wait callback,
        /// without supplying any extra arguments, catching thread interrupt and
        /// abort exceptions as configured.
        /// </summary>
        /// <param name="state">
        /// The state object passed to the wait callback.
        /// </param>
        public void FireWaitCallback(
            object state
            ) /* System.Threading.WaitCallback */
        {
            CheckDisposed();

            bool shouldCatchInterrupt = ShouldCatchInterrupt(callbackFlags);

            try
            {
                /* NO RESULT */
                FireWithOneParameter(state, null);
            }
            catch (ThreadAbortException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (shouldCatchInterrupt)
                    Thread.ResetAbort();
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (!shouldCatchInterrupt)
                    throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a wait callback,
        /// optionally creating an opaque object handle for the supplied state
        /// object and appending the supplied extra arguments.
        /// </summary>
        /// <param name="state">
        /// The state object passed to the wait callback.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        public void FireWaitCallback(
            object state,
            StringList arguments
            )
        {
            CheckDisposed();

            /* NO RESULT */
            FireWithOneParameter(state, arguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /* Eagle._Components.Public.Delegates.GenericCallback */
        /// <summary>
        /// This method fires this callback in response to a generic callback,
        /// without supplying any extra arguments, catching thread interrupt and
        /// abort exceptions as configured.
        /// </summary>
        public void FireGenericCallback()
        {
            CheckDisposed();

            bool shouldCatchInterrupt = ShouldCatchInterrupt(callbackFlags);

            try
            {
                /* NO RESULT */
                FireGenericCallback(null);
            }
            catch (ThreadAbortException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (shouldCatchInterrupt)
                    Thread.ResetAbort();
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError2);

                if (!shouldCatchInterrupt)
                    throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a generic callback,
        /// appending the supplied extra arguments.
        /// </summary>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        public void FireGenericCallback(
            StringList arguments
            )
        {
            CheckDisposed();

            /* NO RESULT */
            FireWithoutParameters(arguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /* System.Delegate.DynamicInvoke */
        /// <summary>
        /// This method fires this callback in response to a dynamic invoke,
        /// without supplying any extra arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments passed to the dynamic invoke.
        /// </param>
        /// <returns>
        /// The return value produced by the callback.
        /// </returns>
        public object FireDynamicInvokeCallback(
            params object[] args
            )
        {
            CheckDisposed();

            return FireDynamicInvokeCallback(args, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback in response to a dynamic invoke,
        /// optionally creating opaque object handles for the arguments, handling
        /// by-reference (output) parameters, and appending the supplied extra
        /// arguments.
        /// </summary>
        /// <param name="args">
        /// The arguments passed to the dynamic invoke; by-reference elements may
        /// be updated upon return.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The return value produced by the callback.
        /// </returns>
        public object FireDynamicInvokeCallback(
            object[] args,
            StringList arguments
            )
        {
            CheckDisposed();

            return FireWithParameters(args, arguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes this callback (i.e. evaluates the callback
        /// script) with the specified extra arguments, discarding any error line
        /// information.
        /// </summary>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode Invoke(
            StringList arguments,
            ref Result result
            )
        {
            CheckDisposed();

            int errorLine = 0;

            return Invoke(arguments, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes this callback (i.e. evaluates the callback
        /// script) with the specified extra arguments, deriving the scheduling
        /// behavior from the configured callback flags.
        /// </summary>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this parameter will be modified to contain the line
        /// number associated with any script error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode Invoke(
            StringList arguments,
            ref Result result,
            ref int errorLine
            )
        {
            CheckDisposed();

            bool useOwner;
            bool mustResetCancel;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out mustResetCancel, out asynchronous, out asynchronousIfBusy);

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                mustResetCancel, asynchronous, asynchronousIfBusy,
                ref result, ref errorLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes this callback as an executable entity, invoking
        /// the callback script with the specified arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for evaluating the callback script.
        /// </param>
        /// <param name="clientData">
        /// The client data supplied by the caller.  This parameter is not used.
        /// </param>
        /// <param name="arguments">
        /// The arguments to append to the callback script.  This parameter may
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the script
        /// result; upon failure, it will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData, /* NOT USED */
            ArgumentList arguments,
            ref Result result
            )
        {
            CheckDisposed();

            return Invoke(interpreter, (arguments != null) ?
                new StringList(arguments) : null, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method fires this callback without marshaling any incoming
        /// parameters, appending only the supplied extra arguments, then handles
        /// post-evaluation behavior such as complaining and fire-and-forget
        /// removal.
        /// </summary>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        private void FireWithoutParameters(
            StringList arguments
            )
        {
            //
            // NOTE: Process the configured callback flags to figure out if
            //       we should complain about failures that would otherwise
            //       be unreportable -AND- if we should attempt to dispose
            //       thread-specific data after completion of the specified
            //       script.
            //
            bool useOwner;
            bool resetCancel;
            bool mustResetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out mustResetCancel, out asynchronous, out asynchronousIfBusy,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError);

            Interpreter interpreter = GetInterpreter();

            try
            {
                ReturnCode code;
                Result result = null;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                StringList localArguments = null;

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    mustResetCancel, asynchronous, asynchronousIfBusy,
                    ref result);

            done:

                try
                {
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                MaybeDisposeThread(interpreter, disposeThread);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback with a single incoming parameter,
        /// optionally creating an opaque object handle for it and appending the
        /// supplied extra arguments, then handles post-evaluation behavior such
        /// as complaining and fire-and-forget removal.
        /// </summary>
        /// <param name="obj">
        /// The single incoming parameter value.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        private void FireWithOneParameter(
            object obj,
            StringList arguments
            )
        {
            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool mustResetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;
            bool useParameterNames;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out mustResetCancel,
                out asynchronous, out asynchronousIfBusy,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError, out useParameterNames);

            Interpreter interpreter = GetInterpreter();

            try
            {
                ReturnCode code;
                Result result = null;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                StringList localArguments = null;

                //
                // NOTE: Do we want to create opaque object handles for the
                //       event arguments?
                //
                if (needArguments)
                {
                    //
                    // NOTE: Add an opaque object handle to the interpreter
                    //       for the object parameter.
                    //
                    code = MarshalOps.FixupReturnValue(
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo, null, objectFlags,
                        null, ObjectOps.GetInvokeOptions(objectOptionType),
                        objectOptionType, null, null, obj, create,
                        dispose, alias, aliasReference, toString,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;

                    //
                    // NOTE: Add the "obj" argument to the list.
                    //
                    AddArgument(
                        useParameterNames ? 0 : Index.Invalid,
                        result, ref localArguments);
                }

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    mustResetCancel, asynchronous, asynchronousIfBusy,
                    ref result);

            done:

                try
                {
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                MaybeDisposeThread(interpreter, disposeThread);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires this callback with an array of incoming parameters,
        /// optionally creating opaque object handles for them, handling by-
        /// reference (output) parameters, appending the supplied extra
        /// arguments, and converting the script result into a return value, then
        /// handles post-evaluation behavior such as complaining and fire-and-
        /// forget removal.
        /// </summary>
        /// <param name="args">
        /// The array of incoming parameter values; by-reference elements may be
        /// updated upon return.
        /// </param>
        /// <param name="arguments">
        /// The extra arguments to append to the callback script.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The return value produced by the callback, which is either an object
        /// extracted from the script result or the formatted script result.
        /// </returns>
        private object FireWithParameters(
            object[] args,
            StringList arguments
            )
        {
            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool mustResetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool byRefStrict;
            bool returnValue;
            bool defaultValue;
            bool addReference;
            bool removeReference;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;
            bool useParameterNames;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out mustResetCancel,
                out asynchronous, out asynchronousIfBusy,
                out byRefStrict, out returnValue, out defaultValue,
                out addReference, out removeReference, out fireAndForget,
                out complain, out disposeThread, out throwOnError,
                out useParameterNames);

            Interpreter interpreter = GetInterpreter();

            try
            {
                ReturnCode code;
                Result result = null;
                int errorLine = 0;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                ArgumentInfoList argumentInfoList = null;
                StringList localArguments = null;

                OptionDictionary options = ObjectOps.GetInvokeOptions(
                    objectOptionType);

                //
                // NOTE: Do we want to create opaque object handles for the
                //       callback arguments?
                //
                if (needArguments && (args != null) && (args.Length > 0))
                {
                    IntArgumentInfoDictionary argumentInfoDictionary = null;

                    code = PrepareByRefArguments(interpreter,
                        ref argumentInfoList, ref argumentInfoDictionary,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;

                    for (int index = 0; index < args.Length; index++)
                    {
                        object arg = args[index];

                        code = MarshalOps.FixupReturnValue(
                            interpreter, interpreter.InternalBinder,
                            interpreter.InternalCultureInfo, null, objectFlags,
                            null, options, objectOptionType, null, null, arg,
                            create, dispose, alias, aliasReference,
                            toString, ref result);

                        if (code != ReturnCode.Ok)
                            goto done;

                        string argString = result;

                        if (argumentInfoDictionary != null)
                        {
                            ArgumentInfo argumentInfo;

                            if (argumentInfoDictionary.TryGetValue(
                                    index, out argumentInfo) &&
                                (argumentInfo != null))
                            {
                                string argName = argumentInfo.Name;

                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, argName, argString,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                argString = argName;
                            }
                        }

                        AddArgument(
                            useParameterNames ? index : Index.Invalid,
                            argString, ref localArguments);
                    }
                }

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    mustResetCancel, asynchronous, asynchronousIfBusy,
                    ref result, ref errorLine);

                if (code != ReturnCode.Ok)
                    goto done;

                //
                // NOTE: Handle any ByRef arguments that may be present.
                //
                if (argumentInfoList != null)
                {
                    code = FixupByRefArguments(
                        interpreter, argumentInfoList, args, byRefStrict,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;
                }

            done:

                try
                {
                    //
                    // NOTE: If necessary, complain prior to the next block,
                    //       because it needs to return (i.e. more than one
                    //       possible value).
                    //
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }

                    //
                    // NOTE: This does not apply for successful return codes
                    //       when the string result is a valid opaque object
                    //       handle.
                    //
                    if (returnValue && (code == ReturnCode.Ok))
                    {
                        if (defaultValue)
                        {
                            return MarshalOps.GetDefaultValue(returnType);
                        }
                        else
                        {
                            object value = null;

                            if (interpreter.InternalDoesObjectExist(
                                    result, addReference, removeReference,
                                    ref value) == ReturnCode.Ok)
                            {
                                return value;
                            }
                        }
                    }

                    //
                    // HACK: Just use the formatted result as the return value,
                    //       which will be just the script result itself when
                    //       successful.
                    //
                    return ResultOps.Format(code, result, errorLine);
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                MaybeDisposeThread(interpreter, disposeThread);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this callback, which
        /// is the string form of its argument list.
        /// </summary>
        /// <returns>
        /// The string form of the callback argument list, or an empty string if
        /// there are no arguments.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            StringList localArguments = this.arguments;

            if (localArguments == null)
                return String.Empty;

            return localArguments.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources used by this callback and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this callback has been disposed and is no longer usable.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this callback has been disposed
        /// and the interpreter is configured to throw on disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
            {
                throw new ObjectDisposedException(
                    typeof(CommandCallback).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this callback.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method (i.e. managed resources should be
        /// released); zero if it is being called from the finalizer.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: Make sure to remove any references to this
                    //       instance from the public callback wrapper.
                    //
                    /* IGNORED */
                    CCW.Cleanup(this);

                    //
                    // NOTE: The contained interpreter is NOT OWNED by
                    //       this object; therefore, DO NOT dispose it.
                    //
                    if (interpreter != null)
                        interpreter = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this callback, releasing any unmanaged resources.
        /// </summary>
        ~CommandCallback()
        {
            Dispose(false);
        }
        #endregion
    }
}
