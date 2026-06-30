/*
 * Utility.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this class from within the Eagle core library itself.
// Instead, the various internal methods used by this class should be called
// directly.  This class is intended only for use by third-party plugins and
// applications.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;

#if NETWORK
using System.Collections.Specialized;
#endif

#if DATA
using System.Data;
#endif

using System.Diagnostics;
using System.Globalization;
using System.IO;

#if NETWORK
using System.Net;
using System.Net.Sockets;
#endif

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

#if !NET_STANDARD_20
using System.Security.AccessControl;
#endif

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

#if WEB
using System.Web;

#if NET_STANDARD_20 && NET_CORE_REFERENCES
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
#endif
#endif

#if WINFORMS
using System.Windows.Forms;
#endif

#if XML
using System.Xml;
#endif

#if XML && SERIALIZATION
using System.Xml.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _RuntimeOps = Eagle._Components.Private.RuntimeOps;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _StringDictionary = Eagle._Containers.Public.StringDictionary;

#if NETWORK
using CidrDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;
#endif

using SyntaxData = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;

using ActiveInterpreterPair = Eagle._Interfaces.Public.IAnyPair<
    Eagle._Components.Public.Interpreter, Eagle._Interfaces.Public.IClientData>;

using AssemblyFilePluginNames = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;

using DelegateList = System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class exposes a large collection of utility methods intended for
    /// use by third-party plugins and applications. Each method forwards to
    /// the corresponding internal Eagle implementation; this class should not
    /// be used from within the Eagle core library itself.
    /// </summary>
    [ObjectId("702cb2b3-5e60-4f90-b5af-df09c236ef51")]
    public static class Utility /* FOR EXTERNAL USE ONLY */
    {
        #region External Use Only Helper Methods
        /// <summary>
        /// Determines the command option type implied by the specified
        /// argument list and returns the associated option dictionary.
        /// </summary>
        /// <param name="arguments">
        /// The list of arguments used to derive the command option type; may
        /// be null.
        /// </param>
        /// <returns>
        /// The option dictionary associated with the derived command option
        /// type, or null if it cannot be determined.
        /// </returns>
        public static OptionDictionary GetCommandOptions(
            ArgumentList arguments /* in */
            )
        {
            return CommandOptions.GetCommandOptions(arguments);
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// Constructs a unique name for a database connection object based on
        /// its type and the associated interpreter.
        /// </summary>
        /// <param name="object">
        /// The database connection object for which a name is needed; may be
        /// null.
        /// </param>
        /// <param name="dbConnectionType">
        /// The type of the database connection.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to obtain a unique identifier, or null to use
        /// the global state.
        /// </param>
        /// <returns>
        /// The constructed unique name for the database connection object, or
        /// null if it is not available.
        /// </returns>
        public static string FormatDatabaseConnectionName(
            object @object,                    /* in */
            DbConnectionType dbConnectionType, /* in */
            Interpreter interpreter            /* in */
            )
        {
            return FormatOps.DatabaseConnectionName(
                @object, dbConnectionType, interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a unique name for a database transaction object based on
        /// the associated interpreter.
        /// </summary>
        /// <param name="object">
        /// The database transaction object for which a name is needed; may be
        /// null.
        /// </param>
        /// <param name="dbConnectionType">
        /// The type of the database connection; this parameter is not used.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to obtain a unique identifier, or null to use
        /// the global state.
        /// </param>
        /// <returns>
        /// The constructed unique name for the database transaction object, or
        /// null if it is not available.
        /// </returns>
        public static string FormatDatabaseTransactionName(
            object @object,                    /* in */
            DbConnectionType dbConnectionType, /* in: NOT USED */
            Interpreter interpreter            /* in */
            )
        {
            return FormatOps.DatabaseTransactionName(
                @object, interpreter);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the file name of the managed executable for the current
        /// process, preferring the entry assembly location and falling back to
        /// the main module file name of the current process.
        /// </summary>
        /// <returns>
        /// The managed executable file name, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetManagedExecutableName()
        {
            return PathOps.GetManagedExecutableName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the global static lock, waiting up to the
        /// specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock
        /// to be acquired.
        /// </param>
        /// <param name="locked">
        /// On input, the current locked state; upon return, non-zero if the
        /// static lock is held by the calling thread.
        /// </param>
        [Obsolete()]
        public static void TryGlobalLock( /* Trust me, you don't need this. */
            int timeout,
            ref bool locked
            )
        {
            GlobalState.TryLock(timeout, ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Releases the global static lock, if it is currently held by the
        /// calling thread.
        /// </summary>
        /// <param name="locked">
        /// On input, non-zero if the static lock is held by the calling
        /// thread; upon return, this parameter is set to zero.
        /// </param>
        [Obsolete()]
        public static void ExitGlobalLock( /* You don't need this either. */
            ref bool locked
            )
        {
            GlobalState.ExitLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the total number of active interpreters across all threads.
        /// </summary>
        /// <returns>
        /// The total active interpreter count.
        /// </returns>
        public static long GetTotalActiveCount()
        {
            return GlobalState.GetTotalActiveCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Pushes the specified interpreter onto the active interpreter stack.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to push onto the active interpreter stack; may be
        /// null.
        /// </param>
        public static void PushActiveInterpreter(
            Interpreter interpreter
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Pushes the specified interpreter, together with the specified
        /// client data, onto the active interpreter stack.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to push onto the active interpreter stack; may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the pushed interpreter; may be
        /// null.
        /// </param>
        public static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData
            )
        {
            GlobalState.PushActiveInterpreter(interpreter, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Associates the specified log client data with the topmost active
        /// interpreter pair, pushing a new active interpreter if necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to push if there is no active interpreter pair; may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The log client data to associate with the active interpreter; if
        /// null, this method does nothing.
        /// </param>
        /// <param name="pushed">
        /// On input, the current pushed count; upon return, it is incremented
        /// when an active interpreter is pushed.
        /// </param>
        public static void MaybePushActiveLogClientData(
            Interpreter interpreter,
            IClientData clientData,
            ref int pushed
            )
        {
            GlobalState.MaybePushActiveLogClientData(
                interpreter, clientData, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the log client data associated with the topmost active
        /// interpreter pair, optionally popping the active interpreter.
        /// </summary>
        /// <param name="pushed">
        /// On input, a counter tracking the number of pushed active
        /// interpreters; upon return, it is updated to reflect any pop
        /// performed.
        /// </param>
        /// <returns>
        /// The affected active interpreter pair, or null if there was no
        /// active interpreter.
        /// </returns>
        public static ActiveInterpreterPair MaybePopActiveLogClientData(
            ref int pushed
            ) /* THREAD-SAFE */
        {
            return GlobalState.MaybePopActiveLogClientData(null, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the topmost active interpreter pair without removing it
        /// from the active interpreter stack.
        /// </summary>
        /// <returns>
        /// The topmost active interpreter pair, or null if there is no active
        /// interpreter.
        /// </returns>
        public static ActiveInterpreterPair PeekActiveInterpreter()
        {
            return GlobalState.PeekActiveInterpreter();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the topmost active interpreter pair from the active
        /// interpreter stack and discards it.
        /// </summary>
        public static void PopActiveInterpreter()
        {
            GlobalState.PopActiveInterpreter();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified value to a string using the to-string
        /// callback of the specified script binder.
        /// </summary>
        /// <param name="scriptBinder">
        /// The script binder that provides the to-string callback used for the
        /// conversion.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when performing the conversion; may be null.
        /// </param>
        /// <param name="value">
        /// The value to convert to a string.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the resulting string value; upon failure,
        /// receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ConvertValueToString(
            IScriptBinder scriptBinder,
            CultureInfo cultureInfo,
            object value,
            ref Result result
            )
        {
            return MarshalOps.ConvertValueToString(
                scriptBinder, cultureInfo, value, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the two specified types are considered the same,
        /// using the default marshalling flags.
        /// </summary>
        /// <param name="type1">
        /// The first type to compare.
        /// </param>
        /// <param name="type2">
        /// The second type to compare.
        /// </param>
        /// <returns>
        /// True if the two types are considered the same; otherwise, false.
        /// </returns>
        public static bool IsSameType(
            Type type1,
            Type type2
            )
        {
            return MarshalOps.IsSameType(type1, type2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified member name into its corresponding core
        /// entity name.
        /// </summary>
        /// <param name="name">
        /// The member name to convert.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to convert the entire name to lowercase; otherwise, only
        /// the first letter is made lowercase.
        /// </param>
        /// <returns>
        /// The core entity name, or null if the specified name is null.
        /// </returns>
        public static string MemberNameToEntityName(
            string name,
            bool noCase
            )
        {
            return ScriptOps.MemberNameToEntityName(name, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Splits the specified long integer value into its four constituent
        /// 16-bit parts.
        /// </summary>
        /// <param name="value">
        /// The long integer value to split.
        /// </param>
        /// <param name="highWord">
        /// On input, ignored; upon return, receives the highest-order 16 bits
        /// of the value.
        /// </param>
        /// <param name="highMidWord">
        /// On input, ignored; upon return, receives the upper-middle 16 bits
        /// of the value.
        /// </param>
        /// <param name="lowMidWord">
        /// On input, ignored; upon return, receives the lower-middle 16 bits
        /// of the value.
        /// </param>
        /// <param name="lowWord">
        /// On input, ignored; upon return, receives the lowest-order 16 bits
        /// of the value.
        /// </param>
        public static void ExtractWords(
            long value,
            ref long highWord,
            ref long highMidWord,
            ref long lowMidWord,
            ref long lowWord
            )
        {
            ConversionOps.UnmakeLong(
                value, ref highWord, ref highMidWord, ref lowMidWord,
                ref lowWord);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Splits the specified long integer value into its two 32-bit halves.
        /// </summary>
        /// <param name="value">
        /// The long integer value to split.
        /// </param>
        /// <param name="highWord">
        /// On input, ignored; upon return, receives the low-order 32 bits of
        /// the value.
        /// </param>
        /// <param name="lowWord">
        /// On input, ignored; upon return, receives the high-order 32 bits of
        /// the value.
        /// </param>
        public static void ExtractWords(
            long value,
            ref int highWord,
            ref int lowWord
            )
        {
            ConversionOps.ToInts(
                value, ref highWord, ref lowWord);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new event wait handle with the specified name and reset
        /// behavior, using a named event when configured to do so, and
        /// indicating whether the event was newly created.
        /// </summary>
        /// <param name="initialState">
        /// Non-zero if the event should be set initially; otherwise, the event
        /// is initially reset.
        /// </param>
        /// <param name="mode">
        /// The reset behavior (automatic or manual) for the event.
        /// </param>
        /// <param name="name">
        /// The name of the event to create.
        /// </param>
        /// <param name="createdNew">
        /// Upon return, non-zero if the event was created by this call;
        /// otherwise, an existing event was opened.
        /// </param>
        /// <returns>
        /// The newly created or opened event wait handle.
        /// </returns>
        public static EventWaitHandle CreateNamedEvent(
            bool initialState,
            EventResetMode mode,
            string name,
            out bool createdNew
            )
        {
            return ThreadOps.CreateEvent(
                initialState, mode, name, out createdNew);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        /// <summary>
        /// Creates a new event wait handle with the specified name, reset
        /// behavior, and access control security, using a named event when
        /// configured to do so, and indicating whether the event was newly
        /// created.
        /// </summary>
        /// <param name="initialState">
        /// Non-zero if the event should be set initially; otherwise, the event
        /// is initially reset.
        /// </param>
        /// <param name="mode">
        /// The reset behavior (automatic or manual) for the event.
        /// </param>
        /// <param name="name">
        /// The name of the event to create.
        /// </param>
        /// <param name="createdNew">
        /// Upon return, non-zero if the event was created by this call;
        /// otherwise, an existing event was opened.
        /// </param>
        /// <param name="eventSecurity">
        /// The access control security to apply to the event.
        /// </param>
        /// <returns>
        /// The newly created or opened event wait handle.
        /// </returns>
        public static EventWaitHandle CreateNamedEvent(
            bool initialState,
            EventResetMode mode,
            string name,
            out bool createdNew,
            EventWaitHandleSecurity eventSecurity
            )
        {
            return ThreadOps.CreateEvent(
                initialState, mode, name, out createdNew,
                eventSecurity);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Opens an existing event wait handle with the specified name, using
        /// a named event when configured to do so.
        /// </summary>
        /// <param name="name">
        /// The name of the event to open.
        /// </param>
        /// <returns>
        /// The opened event wait handle, or null if it could not be opened.
        /// </returns>
        public static EventWaitHandle OpenNamedEvent(
            string name
            )
        {
            return ThreadOps.OpenEvent(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Closes the specified event wait handle, releasing a reference to a
        /// named event when applicable.
        /// </summary>
        /// <param name="event">
        /// On input, the event wait handle to close; upon return, it is set to
        /// null when the event has been closed.
        /// </param>
        public static void CloseNamedEvent(
            ref EventWaitHandle @event
            )
        {
            ThreadOps.CloseEvent(ref @event);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Searches the specified type and its base types for a field with the
        /// specified name, using the specified binding flags.
        /// </summary>
        /// <param name="type">
        /// The type whose fields (and those of its base types) are searched.
        /// </param>
        /// <param name="name">
        /// The name of the field to find.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the field lookup.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The matching field information, or null if the field could not be
        /// found.
        /// </returns>
        public static FieldInfo GetFieldInfo(
            Type type,
            string name,
            BindingFlags bindingFlags,
            ref Result error
            )
        {
            return SettingsOps.GetFieldInfo(
                type, name, bindingFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Searches the specified type and its base types for a property with
        /// the specified name and the specified read and write capabilities,
        /// using the specified binding flags.
        /// </summary>
        /// <param name="type">
        /// The type whose properties (and those of its base types) are
        /// searched.
        /// </param>
        /// <param name="name">
        /// The name of the property to find.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookup.
        /// </param>
        /// <param name="canRead">
        /// Non-zero to require that the matching property be readable.
        /// </param>
        /// <param name="canWrite">
        /// Non-zero to require that the matching property be writable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The matching property information, or null if the property could
        /// not be found.
        /// </returns>
        public static PropertyInfo GetPropertyInfo(
            Type type,
            string name,
            BindingFlags bindingFlags,
            bool canRead,
            bool canWrite,
            ref Result error
            )
        {
            return SettingsOps.GetPropertyInfo(
                type, name, bindingFlags, canRead, canWrite, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the value of the named field of the specified type from the
        /// specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the field.
        /// </param>
        /// <param name="name">
        /// The name of the field whose value is retrieved.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the field lookup.
        /// </param>
        /// <param name="object">
        /// The object from which to read the field value; may be null for a
        /// static field.
        /// </param>
        /// <param name="value">
        /// On input, ignored; upon success, receives the field value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetFieldValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            ref object value,
            ref Result error
            )
        {
            return SettingsOps.GetFieldValue(
                type, name, bindingFlags, @object, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the value of the named field of the specified type on the
        /// specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the field.
        /// </param>
        /// <param name="name">
        /// The name of the field whose value is set.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the field lookup.
        /// </param>
        /// <param name="object">
        /// The object on which to set the field value; may be null for a
        /// static field.
        /// </param>
        /// <param name="value">
        /// The value to set; may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetFieldValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            object value,
            ref Result error
            )
        {
            return SettingsOps.SetFieldValue(
                type, name, bindingFlags, @object, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the value of the named property of the specified type from the
        /// specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the property.
        /// </param>
        /// <param name="name">
        /// The name of the property whose value is retrieved.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookup.
        /// </param>
        /// <param name="object">
        /// The object from which to read the property value; may be null for a
        /// static property.
        /// </param>
        /// <param name="value">
        /// On input, ignored; upon success, receives the property value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetPropertyValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            ref object value,
            ref Result error
            )
        {
            return SettingsOps.GetPropertyValue(
                type, name, bindingFlags, @object, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the value of the named property of the specified type on the
        /// specified object.
        /// </summary>
        /// <param name="type">
        /// The type that declares (or inherits) the property.
        /// </param>
        /// <param name="name">
        /// The name of the property whose value is set.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to control the property lookup.
        /// </param>
        /// <param name="object">
        /// The object on which to set the property value; may be null for a
        /// static property.
        /// </param>
        /// <param name="value">
        /// The value to set; may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetPropertyValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            object value,
            ref Result error
            )
        {
            return SettingsOps.SetPropertyValue(
                type, name, bindingFlags, @object, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current performance count. On Windows the high-resolution
        /// performance counter may be used; on other platforms the result is
        /// the current number of elapsed microseconds.
        /// </summary>
        /// <returns>
        /// The current performance count.
        /// </returns>
        public static long GetPerformanceCount()
        {
            return PerformanceOps.GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calculates the number of microseconds that elapsed between the
        /// specified starting and stopping performance counts.
        /// </summary>
        /// <param name="startCount">
        /// The performance count captured at the start of the measured
        /// interval.
        /// </param>
        /// <param name="stopCount">
        /// The performance count captured at the end of the measured interval.
        /// </param>
        /// <returns>
        /// The number of microseconds that elapsed during the measured
        /// interval.
        /// </returns>
        public static double GetPerformanceMicroseconds(
            long startCount,
            long stopCount
            )
        {
            return PerformanceOps.GetMicrosecondsFromCount(
                startCount, stopCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes the runtime hash code for the specified object, based on
        /// its identity rather than any overridden hash code.
        /// </summary>
        /// <param name="value">
        /// The object for which to compute the runtime hash code.
        /// </param>
        /// <returns>
        /// The identity-based hash code for the specified object.
        /// </returns>
        public static int GetHashCode(
            object value
            )
        {
            return _RuntimeOps.GetHashCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// Obtains the underlying socket associated with the specified network
        /// stream, using reflection to access the non-public property when
        /// necessary.
        /// </summary>
        /// <param name="stream">
        /// The network stream whose underlying socket is to be obtained.
        /// </param>
        /// <returns>
        /// The underlying socket, or null if it could not be obtained.
        /// </returns>
        public static Socket GetSocket(
            NetworkStream stream
            )
        {
            return SocketOps.GetSocket(stream);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified socket has been cleaned up
        /// (disposed), using reflection to access the non-public property.
        /// </summary>
        /// <param name="socket">
        /// The socket to query.
        /// </param>
        /// <param name="default">
        /// The value to return if the cleaned-up state cannot be determined.
        /// </param>
        /// <returns>
        /// True if the socket has been cleaned up, false if it has not, or the
        /// value of <paramref name="default" /> if the state could not be
        /// determined.
        /// </returns>
        public static bool IsSocketCleanedUp(
            Socket socket,
            bool @default
            )
        {
            return SocketOps.IsCleanedUp(socket, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified TCP listener is active, using
        /// reflection to access the non-public property.
        /// </summary>
        /// <param name="listener">
        /// The TCP listener to query.
        /// </param>
        /// <param name="default">
        /// The value to return if the active state cannot be determined.
        /// </param>
        /// <returns>
        /// True if the listener is active, false if it is not, or the value of
        /// <paramref name="default" /> if the state could not be determined.
        /// </returns>
        public static bool IsTcpListenerActive(
            TcpListener listener,
            bool @default
            )
        {
            return SocketOps.IsListenerActive(listener, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Downloads a zip archive from a well-known auxiliary resource and
        /// extracts its contents into the specified directory.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the download and extraction; may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to use for the download and extraction; may be
        /// null.
        /// </param>
        /// <param name="extractDirectory">
        /// The directory into which the zip archive is extracted.
        /// </param>
        /// <param name="resourceName">
        /// The name of the auxiliary resource to download.
        /// </param>
        /// <param name="useFallback">
        /// Non-zero to force use of the command line tool fallback, zero to
        /// use only the managed extraction, or null to use the managed
        /// extraction and fall back to the command line tool if necessary; may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode DownloadAndExtractZipFile(
            Interpreter interpreter,
            IClientData clientData,
            string extractDirectory,
            string resourceName,
            bool? useFallback,
            ref Result error
            )
        {
            return ScriptOps.DownloadAndExtractZipFile(
                interpreter, clientData, extractDirectory,
                resourceName, useFallback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the contents of a zip archive into the specified
        /// directory, optionally falling back to the external "unzip" command
        /// line tool when the managed extraction is unavailable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the context for the extraction; may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data for the extraction; may be null.
        /// </param>
        /// <param name="downloadDirectory">
        /// The directory used to download the command line tool, when running
        /// on Windows; may be null on non-Windows operating systems.
        /// </param>
        /// <param name="downloadFileName">
        /// The file name of the zip archive to extract.
        /// </param>
        /// <param name="extractDirectory">
        /// The directory into which the zip archive is extracted.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use when executing the command line tool, or
        /// null to use the default event flags; may be null.
        /// </param>
        /// <param name="useFallback">
        /// Non-zero to force use of the command line tool fallback, zero to
        /// use only the managed extraction, or null to use the managed
        /// extraction and fall back to the command line tool if necessary; may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExtractZipFileToDirectory(
            Interpreter interpreter,
            IClientData clientData,
            string downloadDirectory,
            string downloadFileName,
            string extractDirectory,
            EventFlags? eventFlags,
            bool? useFallback,
            ref Result error
            )
        {
            return ScriptOps.ExtractZipFileToDirectory(
                interpreter, clientData, downloadDirectory,
                downloadFileName, extractDirectory, eventFlags,
                useFallback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// Determines whether the web subsystem is currently in offline mode,
        /// in which case the creation of web clients is prevented.
        /// </summary>
        /// <returns>
        /// True if the web subsystem is in offline mode; otherwise, false.
        /// </returns>
        public static bool InOfflineMode()
        {
            return WebOps.InOfflineMode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables offline mode by incrementing or decrementing
        /// the offline level count.
        /// </summary>
        /// <param name="offline">
        /// Non-zero to enter offline mode (increment the level); zero to leave
        /// offline mode (decrement the level).
        /// </param>
        public static void SetOfflineMode(
            bool offline
            )
        {
            WebOps.SetOfflineMode(offline);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the configured default maximum number of times a web
        /// request may be retried.
        /// </summary>
        /// <returns>
        /// The configured maximum number of retries.
        /// </returns>
        public static int GetWebMaximumRetries()
        {
            return WebOps.GetMaximumRetries();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the configured default maximum number of times a web request
        /// may be retried.
        /// </summary>
        /// <param name="retries">
        /// The new maximum number of retries.
        /// </param>
        /// <returns>
        /// The previous maximum number of retries.
        /// </returns>
        public static int SetWebMaximumRetries(
            int retries
            )
        {
            return WebOps.SetMaximumRetries(retries);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sleeps for the amount of time appropriate to the specified retry
        /// attempt, using the interpreter event subsystem when an interpreter
        /// is available, or a plain thread sleep otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null to use a plain thread
        /// sleep.
        /// </param>
        /// <param name="event">
        /// The event that, when signaled, can interrupt the wait, or null for
        /// none.
        /// </param>
        /// <param name="retries">
        /// The current retry attempt number, used to scale the sleep time.
        /// </param>
        public static void SleepForWebRetry(
            Interpreter interpreter,
            EventWaitHandle @event,
            int retries
            )
        {
            WebOps.SleepForRetry(interpreter, @event, retries);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new web client for the specified interpreter, using the
        /// configured web client tag environment variable value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context, or null for none.
        /// </param>
        /// <param name="argument">
        /// A description of the operation requesting the web client, used for
        /// diagnostic purposes.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any; may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to apply to each outgoing request, or
        /// null to apply no explicit timeout; may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The created web client upon success, or null upon failure.
        /// </returns>
        public static WebClient CreateWebClient(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            int? timeout,
            ref Result error
            )
        {
            return WebOps.CreateClient(
                interpreter, argument, clientData, timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new web client for the specified interpreter, consulting
        /// any pre-create and new-client callbacks and honoring offline mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context, or null for none.
        /// </param>
        /// <param name="argument">
        /// A description of the operation requesting the web client, used for
        /// diagnostic purposes.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any; may be null.
        /// </param>
        /// <param name="tag">
        /// The tag to add to each outgoing request, via custom request
        /// headers, or null to add no tag; may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to apply to each outgoing request, or
        /// null to apply no explicit timeout; may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The created web client upon success, or null upon failure.
        /// </returns>
        public static WebClient CreateWebClient(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            string tag,
            int? timeout,
            ref Result error
            )
        {
            return WebOps.CreateClient(
                interpreter, argument, clientData, tag, timeout,
                ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// Configures the security protocol used for subsequent web requests,
        /// without forcing reconfiguration when it appears to have already
        /// been set up.
        /// </summary>
        /// <param name="obsolete">
        /// Non-zero to permit obsolete security protocols to be included.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetWebSecurityProtocol(
            bool obsolete,
            ref Result error
            )
        {
            return WebOps.SetSecurityProtocol(false, obsolete, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Configures the security protocol used for subsequent web requests.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force the security protocol to be reconfigured even if
        /// it appears to have already been set up.
        /// </param>
        /// <param name="obsolete">
        /// Non-zero to permit obsolete security protocols to be included.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetWebSecurityProtocol(
            bool force,
            bool obsolete,
            ref Result error
            )
        {
            return WebOps.SetSecurityProtocol(force, obsolete, ref error);
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// Serves as the cross-application-domain entry point used to start
        /// the Eagle shell using the command line arguments for the current
        /// process.
        /// </summary>
        public static void StartupShellMain() /* System.CrossAppDomainDelegate */
        {
            ShellOps.StartupShellMain();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Serves as the cross-application-domain entry point used to create
        /// an interpreter and enter its interactive loop using the command
        /// line arguments for the current process.
        /// </summary>
        public static void StartupInteractiveLoop() /* System.CrossAppDomainDelegate */
        {
            ShellOps.StartupInteractiveLoop();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// Obtains the default application domain by hosting the CLR runtime
        /// via native COM interfaces. On platforms or runtimes where this is
        /// not supported, it falls back to returning the current application
        /// domain.
        /// </summary>
        /// <returns>
        /// The default application domain, or null if it could not be
        /// obtained.
        /// </returns>
        public static object GetDefaultAppDomain()
        {
            return AppDomainOps.GetDefault();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// This method obtains the running totals of application domains
        /// created and unloaded, either limited to the counts tracked by
        /// this library or drawn from the process-wide reference counts.
        /// </summary>
        /// <param name="localOnly">
        /// Non-zero to report only the counts tracked by this library; zero
        /// to report the process-wide counts.
        /// </param>
        /// <param name="createCount">
        /// Upon return, this parameter receives the total number of
        /// application domains created.
        /// </param>
        /// <param name="unloadCount">
        /// Upon return, this parameter receives the total number of
        /// application domains unloaded.
        /// </param>
        public static void GetAppDomainCounts(
            bool localOnly,
            ref long createCount,
            ref long unloadCount
            )
        {
            AppDomainOps.GetCounts(
                localOnly, ref createCount, ref unloadCount);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the cached file system location of the core
        /// Eagle assembly.
        /// </summary>
        /// <returns>
        /// The file system location of this assembly, or null if it is not
        /// available.
        /// </returns>
        public static string GetAssemblyLocation()
        {
            return GlobalState.GetAssemblyLocation();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the strong name signature of the
        /// assembly file with the specified name is verified, forcing
        /// verification even if it has been previously disabled for the
        /// assembly.
        /// </summary>
        /// <param name="fileName">
        /// The name of the assembly file whose strong name signature should
        /// be verified.
        /// </param>
        /// <returns>
        /// True if the strong name signature is verified; otherwise, false.
        /// </returns>
        public static bool IsFileStrongNameVerified(
            string fileName
            )
        {
            return _RuntimeOps.IsStrongNameVerified(fileName, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the trust of the specified file
        /// can be verified, using the default trust checking options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any; this parameter may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to check for trust.
        /// </param>
        /// <returns>
        /// True if the trust of the file could be verified; otherwise,
        /// false.
        /// </returns>
        public static bool IsFileTrusted(
            Interpreter interpreter,
            string fileName
            )
        {
            return _RuntimeOps.IsFileTrusted(
                interpreter, null, fileName, IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether interpreter creation is currently
        /// disabled, consulting global state and including transient
        /// (non-persistent) disable requests.
        /// </summary>
        /// <param name="error">
        /// If creation is disabled, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// True if interpreter creation is disabled; otherwise, false.
        /// </returns>
        public static bool IsInterpreterCreationDisabled(
            ref Result error
            )
        {
            return Interpreter.IsCreationDisabled(false, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When flags are supplied, this method re-enables interpreter
        /// creation by decrementing the outstanding disable count; when null,
        /// it instead queries the current disable count without changing it.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how creation is re-enabled, or null to
        /// merely query the outstanding disable count; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// Null when flags were supplied (and creation was re-enabled), or
        /// the current count of outstanding requests to disable interpreter
        /// creation when flags is null.
        /// </returns>
        public static int? EnableInterpreterCreation(
            DisableFlags? flags
            )
        {
            if (flags != null)
            {
                Interpreter.EnableCreation((DisableFlags)flags);
                return null;
            }
            else
            {
                return Interpreter.GetDisableCreationCount();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When flags are supplied, this method disables interpreter creation
        /// by incrementing the outstanding disable count (optionally
        /// persisting the change); when null, it instead queries the current
        /// disable count without changing it.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how creation is disabled, including whether
        /// the change is persistent, or null to merely query the outstanding
        /// disable count; this parameter may be null.
        /// </param>
        /// <returns>
        /// Null when flags were supplied (and creation was disabled), or the
        /// current count of outstanding requests to disable interpreter
        /// creation when flags is null.
        /// </returns>
        public static int? DisableInterpreterCreation(
            DisableFlags? flags
            )
        {
            if (flags != null)
            {
                Interpreter.DisableCreation((DisableFlags)flags);
                return null;
            }
            else
            {
                return Interpreter.GetDisableCreationCount();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN || MAYBE_ENTERPRISE_LOCKDOWN
        /// <summary>
        /// This method determines whether enterprise lockdown mode is
        /// enabled.  When compiled with ENTERPRISE_LOCKDOWN, this mode is
        /// always enabled; otherwise, it reflects whether the stub assembly
        /// is loaded.
        /// </summary>
        /// <returns>
        /// True if enterprise lockdown mode is enabled; otherwise, false.
        /// </returns>
        public static bool IsEnterpriseLockdownEnabled()
        {
            return Interpreter.IsEnterpriseLockdownEnabled();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables enterprise lockdown mode.  When compiled with
        /// ENTERPRISE_LOCKDOWN, this mode is always enabled and the call
        /// succeeds immediately; otherwise, it attempts to load the stub
        /// assembly.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode EnableEnterpriseLockdown(
            ref Result error
            )
        {
            return Interpreter.EnableEnterpriseLockdown(ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the count of outstanding requests to enable
        /// the stub assembly, after verifying that interpreter creation is not
        /// persistently disabled.
        /// </summary>
        /// <param name="flags">
        /// The flags that control the behavior of this method.
        /// </param>
        public static void EnableStubAssembly(
            DisableFlags flags
            )
        {
            Interpreter.EnableStubAssembly(flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the count of outstanding requests to enable
        /// the stub assembly, after verifying that interpreter creation is not
        /// persistently disabled.
        /// </summary>
        /// <param name="flags">
        /// The flags that control the behavior of this method.
        /// </param>
        public static void DisableStubAssembly(
            DisableFlags flags
            )
        {
            Interpreter.DisableStubAssembly(flags);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        /// <summary>
        /// This method attempts to obtain a serial number that uniquely
        /// identifies the file or directory at the specified path, dispatching
        /// to the platform-specific implementation for the current operating
        /// system.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to obtain a serial number for.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the serial number is calculated.
        /// </param>
        /// <param name="serialNumber">
        /// Upon success, this parameter receives the calculated serial number
        /// string.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// True if the serial number was obtained successfully; otherwise,
        /// false.
        /// </returns>
        public static bool TryGetPathSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
            return PathOps.TryGetSerialNumber(
                path, flags, ref serialNumber, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the elements of the specified list into a
        /// single path string, using the directory separator indicated by the
        /// caller or, when none is specified, the first separator found within
        /// the list.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator, zero to use the
        /// Windows directory separator, or null to detect the separator from
        /// the list itself; this parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list of path components to combine; this parameter may be null.
        /// </param>
        /// <returns>
        /// The combined path string, or null if it is not available.
        /// </returns>
        public static string CombinePath(
            bool? unix,
            IList list
            )
        {
            return PathOps.CombinePath(unix, list);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the leading global namespace separator from
        /// the specified name, if present.
        /// </summary>
        /// <param name="name">
        /// The name to trim; this parameter may be null.
        /// </param>
        /// <returns>
        /// The name with any leading global namespace separator removed, or
        /// null if the input was null.
        /// </returns>
        public static string TrimLeadingNamespacePrefix(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified path to use the directory
        /// separator style indicated by the translation type.
        /// </summary>
        /// <param name="path">
        /// The path to translate.
        /// </param>
        /// <param name="translationType">
        /// The kind of path translation to perform.
        /// </param>
        /// <returns>
        /// The translated path, or the original path if no translation
        /// applies.
        /// </returns>
        public static string TranslatePath(
            string path,
            PathTranslationType translationType
            )
        {
            return PathOps.TranslatePath(path, translationType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the temporary directory to use, consulting
        /// the configured temporary path callback when present or otherwise
        /// the relevant environment variables, falling back to the system
        /// temporary directory; any configured temporary sub-path is applied.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The temporary directory path, or null if one is not available.
        /// </returns>
        public static string GetTempPath(
            Interpreter interpreter
            )
        {
            return PathOps.GetTempPath(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the full path to a temporary file, using the
        /// configured temporary file name callback when present or otherwise
        /// combining a random file name with the temporary directory.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to use when generating the random file name; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The full path to a temporary file, or null if it is not available.
        /// </returns>
        public static string GetTempFileName(
            string prefix
            )
        {
            return PathOps.GetTempFileName(null, prefix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the full path to a temporary file, using the
        /// configured temporary file name callback when present or otherwise
        /// combining a random file name with the temporary directory.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any; this parameter may be
        /// null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to use when generating the random file name; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The full path to a temporary file, or null if it is not available.
        /// </returns>
        public static string GetTempFileName(
            Interpreter interpreter,
            string prefix
            )
        {
            return PathOps.GetTempFileName(interpreter, prefix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the path specified via
        /// <paramref name="path1" /> is contained within (i.e. is under) the
        /// path specified via <paramref name="path2" />.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any; this parameter may be
        /// null.
        /// </param>
        /// <param name="path1">
        /// The candidate child path to check.
        /// </param>
        /// <param name="path2">
        /// The candidate parent path.
        /// </param>
        /// <returns>
        /// True if <paramref name="path1" /> is under
        /// <paramref name="path2" />; otherwise, false.
        /// </returns>
        public static bool IsUnderPath(
            Interpreter interpreter,
            string path1,
            string path2
            )
        {
            return PathOps.IsUnderPath(interpreter, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the local user name, machine name, and domain
        /// name, using either the built-in runtime values or the corresponding
        /// environment variables.
        /// </summary>
        /// <param name="perUser">
        /// Non-zero to consider the per-user environment variables when
        /// deciding whether the built-in values should be used.
        /// </param>
        /// <param name="forceBuiltIn">
        /// Non-zero to force the use of the built-in runtime values, zero to
        /// force the use of the environment variables, or null to decide
        /// automatically; this parameter may be null.
        /// </param>
        /// <param name="userName">
        /// Upon return, this parameter receives the local user name.
        /// </param>
        /// <param name="machineName">
        /// Upon return, this parameter receives the local machine name.
        /// </param>
        /// <param name="domainName">
        /// Upon return, this parameter receives the local domain name.
        /// </param>
        /// <returns>
        /// Non-zero if the built-in runtime values were used, zero if the
        /// environment variables were used, or null if no local names could be
        /// obtained.
        /// </returns>
        public static bool? GetLocalNames(
            bool perUser,
            bool? forceBuiltIn,
            out string userName,
            out string machineName,
            out string domainName
            )
        {
            return PathOps.GetLocalNames(
                perUser, forceBuiltIn, out userName, out machineName,
                out domainName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates a unique file or directory path within the
        /// specified base directory by combining the prefix and suffix with a
        /// randomly generated hexadecimal identifier, retrying until an unused
        /// path is found.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any; this parameter may be
        /// null.
        /// </param>
        /// <param name="directory">
        /// The base directory in which to generate the unique path; if null or
        /// an empty string, the temporary directory is used instead.
        /// </param>
        /// <param name="prefix">
        /// The prefix to prepend to the generated identifier, if any; this
        /// parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to append to the generated identifier, if any; this
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The generated unique path, or null if a unique path could not be
        /// generated.
        /// </returns>
        public static string GetUniquePath(
            Interpreter interpreter,
            string directory,
            string prefix,
            string suffix,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.GetUniquePath(
                interpreter, directory, prefix, suffix, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the greatest legal key size and the least
        /// legal block size supported by the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">
        /// The symmetric algorithm to query; this parameter may be null.
        /// </param>
        /// <param name="keySize">
        /// Upon success, this parameter receives the greatest legal key size,
        /// in bits, supported by the algorithm.
        /// </param>
        /// <param name="blockSize">
        /// Upon success, this parameter receives the least legal block size,
        /// in bits, supported by the algorithm.
        /// </param>
        /// <returns>
        /// An array of two boolean values; the first element is true if the
        /// key size was determined and the second is true if the block size
        /// was determined.
        /// </returns>
        public static bool[] GetGreatestMaxKeySizeAndLeastMinBlockSize(
            SymmetricAlgorithm algorithm,
            ref int keySize,
            ref int blockSize
            )
        {
            return _RuntimeOps.GetGreatestMaxKeySizeAndLeastMinBlockSize(
                algorithm, ref keySize, ref blockSize);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets framework version information for the specified
        /// assembly and/or type, according to the specified framework flags.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query for framework information; if null, an error
        /// is reported.
        /// </param>
        /// <param name="id">
        /// The optional object identifier of the type within the assembly to
        /// query, or null to query the assembly itself; this parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The framework flags that control which sources of framework
        /// information are consulted and how the result is formatted.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the framework information;
        /// upon failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetFramework(
            Assembly assembly,
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            return _RuntimeOps.GetFramework(assembly, id, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: The returned DateTime value may be virtualized (i.e. it may
        //          not reflect the actual current date and time).
        //
        /// <summary>
        /// This method returns the current local date and time, or the fake
        /// local date and time if one has been set.  The returned value may be
        /// virtualized and may not reflect the actual current date and time.
        /// </summary>
        /// <returns>
        /// The current (possibly virtualized) local date and time.
        /// </returns>
        public static DateTime GetNow()
        {
            return TimeOps.GetNow();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: The returned DateTime value may be virtualized (i.e. it may
        //          not reflect the actual current date and time).
        //
        /// <summary>
        /// This method returns the current UTC date and time, or the fake UTC
        /// date and time if one has been set.  The returned value may be
        /// virtualized and may not reflect the actual current date and time.
        /// </summary>
        /// <returns>
        /// The current (possibly virtualized) UTC date and time.
        /// </returns>
        public static DateTime GetUtcNow()
        {
            return TimeOps.GetUtcNow();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of ticks corresponding to the
        /// current UTC date and time (or the fake UTC date and time, if one
        /// has been set).
        /// </summary>
        /// <returns>
        /// The number of ticks corresponding to the current UTC date and
        /// time.
        /// </returns>
        public static long GetUtcNowTicks()
        {
            return TimeOps.GetUtcNowTicks();
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// This method determines whether the specified text looks like the
        /// start of an XML document.
        /// </summary>
        /// <param name="text">
        /// The text to examine; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the text begins with the XML document start prefix;
        /// otherwise, false.
        /// </returns>
        public static bool LooksLikeXmlDocument(
            string text
            )
        {
            return XmlOps.LooksLikeDocument(text);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns an unsigned random number obtained from the
        /// shared random number generator.
        /// </summary>
        /// <returns>
        /// A randomly generated unsigned integer value.
        /// </returns>
        public static ulong GetRandomNumber()
        {
            return _RuntimeOps.GetRandomNumber();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fills the specified byte array with random data, using
        /// the specified interpreter as the entropy source when one is
        /// provided and otherwise the shared random number generator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose entropy source is used; if null, the shared
        /// random number generator is used instead; this parameter may be
        /// null.
        /// </param>
        /// <param name="bytes">
        /// On input, supplies a byte array whose length determines the number
        /// of random bytes to produce; upon success, receives the random data.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetRandomBytes(
            Interpreter interpreter,
            ref byte[] bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return _RuntimeOps.GetRandomBytes(
                interpreter, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a random element from a one-dimensional array,
        /// using the interpreter random number generator when available and
        /// otherwise the global runtime random number generator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose random number generator is used; when null,
        /// the global runtime random number generator is used instead; this
        /// parameter may be null.
        /// </param>
        /// <param name="array">
        /// The one-dimensional, non-empty array from which to select an
        /// element.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter receives the randomly selected element
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SelectRandomArrayValue(
            Interpreter interpreter,
            Array array,
            ref object value,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ArrayOps.SelectRandomValue(
                interpreter, array, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes blank lines and comment lines from the
        /// specified string value, normalizing the remaining lines to use
        /// carriage-return/line-feed line endings.
        /// </summary>
        /// <param name="trimAll">
        /// Non-zero to trim leading and trailing whitespace from every
        /// retained line; otherwise, the original lines are retained verbatim.
        /// </param>
        /// <param name="value">
        /// On input, the string value to process; upon success, receives the
        /// resulting string value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode RemoveBlanksAndComments(
            bool trimAll,
            ref string value,
            ref Result error
            )
        {
            return StringOps.RemoveBlanksAndComments(
                trimAll, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the data contained within the comment lines of
        /// the specified string value, discarding all non-comment lines and
        /// the leading comment character of each comment line, normalizing the
        /// result to use carriage-return/line-feed line endings.
        /// </summary>
        /// <param name="value">
        /// On input, the string value to process; upon success, receives the
        /// resulting string value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ExtractDataFromComments(
            ref string value,
            ref Result error
            )
        {
            return StringOps.ExtractDataFromComments(ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the file system path of the current script,
        /// optionally returning only its containing directory.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the context for resolving the script path.
        /// </param>
        /// <param name="directoryOnly">
        /// Non-zero to return only the directory portion of the script path.
        /// </param>
        /// <param name="path">
        /// Upon success, this parameter receives the resulting file system
        /// path.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetScriptPath(
            Interpreter interpreter,
            bool directoryOnly,
            ref string path,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.GetScriptPath(
                interpreter, directoryOnly, ref path, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches for a file using the specified candidate path
        /// and search behavior flags, resolving it against the relevant search
        /// locations.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any; this parameter may be
        /// null.
        /// </param>
        /// <param name="path">
        /// The (possibly qualified) file name to search for.
        /// </param>
        /// <param name="fileSearchFlags">
        /// The flags that control how the search is performed.
        /// </param>
        /// <returns>
        /// The resolved file name if it was found; otherwise, either null or
        /// the original input path, depending on the search flags.
        /// </returns>
        public static string SearchForPath(
            Interpreter interpreter,
            string path,
            FileSearchFlags fileSearchFlags
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.Search(interpreter, path, fileSearchFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the specified directory and each of its parent
        /// directories for files matching the specified search patterns.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context; this parameter is not used.
        /// </param>
        /// <param name="directory">
        /// The directory at which to begin the upward search.
        /// </param>
        /// <param name="subParts">
        /// The sub-directory parts to append to each directory before
        /// searching; this parameter may be null.
        /// </param>
        /// <param name="searchPatterns">
        /// The list of file name search patterns to match.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching paths to collect, or a negative
        /// value for no limit.
        /// </param>
        /// <param name="unix">
        /// Non-zero to use Unix-style path combining, zero to use the native
        /// style, or null to use the default; this parameter may be null.
        /// </param>
        /// <param name="paths">
        /// On input, the list of matching paths to add to (created if null);
        /// upon return, contains any newly matched paths.
        /// </param>
        /// <returns>
        /// The number of matching paths that were found.
        /// </returns>
        public static int SearchParentsForPath(
            Interpreter interpreter,
            string directory,
            StringList subParts,
            StringList searchPatterns,
            int limit,
            bool? unix,
            ref StringList paths
            )
        {
            return PathOps.SearchParents(
                interpreter, directory, subParts, searchPatterns, limit,
                unix, ref paths);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// This method returns the names of the interactive commands whose
        /// help topics match the specified pattern, using the default text
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive command help is consulted.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter command names, or null to return all of
        /// them; this parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform pattern matching without regard to case;
        /// otherwise, matching is case-sensitive.
        /// </param>
        /// <returns>
        /// A list of matching interactive command names, or null when no help
        /// data is available.
        /// </returns>
        public static StringList GetInteractiveCommandNames(
            Interpreter interpreter,
            string pattern,
            bool noCase
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HelpOps.GetInteractiveCommandNames(interpreter,
                pattern, noCase, HelpOps.GetDefaultTextFlags());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a copy of the syntax and description pair
        /// associated with the named interactive command, using the default
        /// text flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive command help is consulted.
        /// </param>
        /// <param name="name">
        /// The name of the interactive command whose help item is requested.
        /// </param>
        /// <returns>
        /// A copy of the syntax and description pair for the named command, or
        /// null when it is not found.
        /// </returns>
        public static StringPair GetInteractiveCommandHelpItem(
            Interpreter interpreter,
            string name
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HelpOps.GetInteractiveCommandHelpItem(
                interpreter, name, HelpOps.GetDefaultTextFlags());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the names of the interactive commands whose
        /// help topics match the specified pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive command help is consulted.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter command names, or null to return all of
        /// them; this parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform pattern matching without regard to case;
        /// otherwise, matching is case-sensitive.
        /// </param>
        /// <param name="textFlags">
        /// The flags used to control how the help data is produced.
        /// </param>
        /// <returns>
        /// A list of matching interactive command names, or null when no help
        /// data is available.
        /// </returns>
        public static StringList GetInteractiveCommandNames(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            TextFlags textFlags
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HelpOps.GetInteractiveCommandNames(
                interpreter, pattern, noCase, textFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a copy of the syntax and description pair
        /// associated with the named interactive command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive command help is consulted.
        /// </param>
        /// <param name="name">
        /// The name of the interactive command whose help item is requested.
        /// </param>
        /// <param name="textFlags">
        /// The flags used to control how the help data is produced.
        /// </param>
        /// <returns>
        /// A copy of the syntax and description pair for the named command, or
        /// null when it is not found.
        /// </returns>
        public static StringPair GetInteractiveCommandHelpItem(
            Interpreter interpreter,
            string name,
            TextFlags textFlags
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HelpOps.GetInteractiveCommandHelpItem(
                interpreter, name, textFlags);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// This method creates, and optionally starts, a thread that runs the
        /// Eagle shell using the specified command line arguments.
        /// </summary>
        /// <param name="args">
        /// The command line arguments to pass to the shell.
        /// </param>
        /// <param name="start">
        /// Non-zero to start the thread before returning; otherwise, the
        /// thread is created but not started.
        /// </param>
        /// <returns>
        /// The created thread, or null if it could not be created.
        /// </returns>
        public static Thread CreateShellMainThread(
            IEnumerable<string> args,
            bool start
            )
        {
            return ShellOps.CreateShellMainThread(args, start);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates, and optionally starts, a thread that runs an
        /// interactive loop for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to run the interactive loop for.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop to run.
        /// </param>
        /// <param name="start">
        /// Non-zero to start the thread before returning; otherwise, the
        /// thread is created but not started.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The created thread, or null if it could not be created.
        /// </returns>
        public static Thread CreateInteractiveLoopThread(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            bool start,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ShellOps.CreateInteractiveLoopThread(
                interpreter, loopData, start, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stops the specified interactive loop thread by
        /// signaling its done event, canceling its host input, and waiting for
        /// it to exit.
        /// </summary>
        /// <param name="thread">
        /// The interactive loop thread to stop.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that owns the interactive loop.
        /// </param>
        /// <param name="force">
        /// Non-zero to forcibly cancel any pending host input.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode StopInteractiveLoopThread(
            Thread thread,
            Interpreter interpreter,
            bool force,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ShellOps.StopInteractiveLoopThread(
                thread, interpreter, force, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a single command-line string from the specified
        /// collection of arguments, optionally quoting every argument.
        /// </summary>
        /// <param name="args">
        /// The collection of arguments to be included in the resulting command
        /// line.
        /// </param>
        /// <param name="quoteAll">
        /// Non-zero to force every argument to be quoted; otherwise, only
        /// those requiring it are quoted.
        /// </param>
        /// <returns>
        /// The constructed command-line string, or null if an error was
        /// encountered.
        /// </returns>
        public static string BuildCommandLine(
            IEnumerable<string> args,
            bool quoteAll
            )
        {
            bool done = false; /* NOT USED */
            Result error = null; /* NOT USED */

            return _RuntimeOps.BuildCommandLine(
                null, args, null, quoteAll, false,
                false, ref done, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the first element of the specified
        /// argument list, replacing the list with a smaller one (or null when
        /// it becomes empty).
        /// </summary>
        /// <param name="args">
        /// On input, the argument list to pop the first element from; upon
        /// return, refers to the remaining elements, or null if none remain.
        /// </param>
        /// <returns>
        /// The first element of the list, or null if the list is null or
        /// empty.
        /// </returns>
        public static string PopFirstArgument(
            ref IList<string> args
            )
        {
            return GenericOps<string>.PopFirstArgument(ref args);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the last element of the specified
        /// argument list, replacing the list with a smaller one (or null when
        /// it becomes empty).
        /// </summary>
        /// <param name="args">
        /// On input, the argument list to pop the last element from; upon
        /// return, refers to the remaining elements, or null if none remain.
        /// </param>
        /// <returns>
        /// The last element of the list, or null if the list is null or empty.
        /// </returns>
        public static string PopLastArgument(
            ref IList<string> args
            )
        {
            return GenericOps<string>.PopLastArgument(ref args);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified switch, comparing only the leading portion of the switch
        /// up to the length of the text, using a case-insensitive comparison.
        /// </summary>
        /// <param name="text">
        /// The text to compare against the switch.
        /// </param>
        /// <param name="switch">
        /// The switch to be matched.
        /// </param>
        /// <returns>
        /// True if the text matches the leading portion of the switch;
        /// otherwise, false.
        /// </returns>
        public static bool MatchSwitch(
            string text,
            string @switch
            )
        {
            return StringOps.MatchSwitch(text, @switch);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified flags value contains a
        /// particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the value supplied via
        /// <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in the value supplied via
        /// <paramref name="hasFlags" /> must be present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            OptionFlags flags,
            OptionFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified flags value contains a
        /// particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the value supplied via
        /// <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in the value supplied via
        /// <paramref name="hasFlags" /> must be present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            TracePriority flags,
            TracePriority hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified flags value contains a
        /// particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the value supplied via
        /// <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in the value supplied via
        /// <paramref name="hasFlags" /> must be present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            PackageIfNeededFlags flags,
            PackageIfNeededFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            RuleSetType flags,
            RuleSetType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            BreakpointType flags,
            BreakpointType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            ProcedureFlags flags,
            ProcedureFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if THREADING
        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            CheckStatus flags,
            CheckStatus hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            CommandFlags flags,
            CommandFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            CreateFlags flags,
            CreateFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            CreateStateFlags flags,
            CreateStateFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            ExecutionPolicy flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            ExecutionPolicy? flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            HostFlags flags,
            HostFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            NotifyFlags flags,
            NotifyFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            NotifyType flags,
            NotifyType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            OperatorFlags flags,
            OperatorFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            PathFlags flags,
            PathFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            PluginFlags flags,
            PluginFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            PolicyFlags flags,
            PolicyFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            ScriptFlags flags,
            ScriptFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            ScriptSecurityFlags flags,
            ScriptSecurityFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            SecretDataFlags flags,
            SecretDataFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            SecretDataFlags? flags,
            SecretDataFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            SwapFlags flags,
            SwapFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            UriFlags flags,
            UriFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// Determines whether the given flags value contains the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that every flag in
        /// <paramref name="hasFlags" /> is present; otherwise, the
        /// presence of any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            WebFlags flags,
            WebFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes a hash of the specified string value using the
        /// default hash algorithm associated with text encoding.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="value">
        /// The string value to hash.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when converting the value to bytes
        /// prior to hashing.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null on failure.
        /// </returns>
        public static byte[] HashString(
            Interpreter interpreter,
            string value,
            EncodingType encodingType,
            ref Result error
            )
        {
            return HashOps.Compute(interpreter,
                HashOps.GetAlgorithmName(EncodingType.Text),
                value, StringOps.GetEncoding(encodingType),
                null, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes a hash of the specified string value using the
        /// named hash algorithm.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to compute.
        /// </param>
        /// <param name="value">
        /// The string value to hash.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when converting the value to bytes
        /// prior to hashing.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null on failure.
        /// </returns>
        public static byte[] HashString(
            Interpreter interpreter,
            string hashAlgorithmName,
            string value,
            EncodingType encodingType,
            ref Result error
            )
        {
            return HashOps.Compute(
                interpreter, hashAlgorithmName, value,
                StringOps.GetEncoding(encodingType), null,
                false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes a hash of the specified text using the named hash
        /// algorithm and the given character encoding.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to compute.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the text to
        /// bytes; this parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text to hash.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null on failure.
        /// </returns>
        public static byte[] HashString(
            string hashAlgorithmName,
            Encoding encoding,
            string text,
            ref Result error
            )
        {
            return HashOps.HashString(
                hashAlgorithmName, encoding, text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes a hash of the specified bytes using the named hash
        /// algorithm.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to compute.
        /// </param>
        /// <param name="bytes">
        /// The array of bytes to hash.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null on failure.
        /// </returns>
        public static byte[] HashBytes(
            string hashAlgorithmName,
            byte[] bytes,
            ref Result error
            )
        {
            return HashOps.HashBytes(
                hashAlgorithmName, bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes a hash of the contents of the specified file using
        /// the named hash algorithm.  Remote URI file names are not
        /// supported.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to use the
        /// default hash algorithm.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose contents are to be hashed.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used to read the file and convert its
        /// contents to bytes; this parameter may be null to hash the
        /// raw bytes of the file.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null on failure.
        /// </returns>
        public static byte[] HashFile(
            string hashAlgorithmName,
            string fileName,
            Encoding encoding,
            ref Result error
            )
        {
            return _RuntimeOps.HashFile(
                hashAlgorithmName, fileName, encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes a hash over the concatenation of an optional string
        /// value and an optional byte array value, using the default
        /// hash algorithm associated with text encoding.
        /// </summary>
        /// <param name="value1">
        /// The string value to include in the hash; this parameter may
        /// be null.
        /// </param>
        /// <param name="value2">
        /// The byte array value to include in the hash; this parameter
        /// may be null.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when converting the string value to
        /// bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null on failure
        /// (including when both values are null).
        /// </returns>
        public static byte[] HashStringAndOrBytes(
            string value1,
            byte[] value2,
            EncodingType encodingType,
            ref Result error
            )
        {
            return HashOps.Compute(HashOps.GetAlgorithmName(
                EncodingType.Text), value1, value2, StringOps.GetEncoding(
                encodingType), ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads the contents of the specified script file and computes
        /// the hash of its original text using the default hash
        /// algorithm.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to read the script file and to
        /// obtain the flags that control how it is read.
        /// </param>
        /// <param name="fileName">
        /// The name of the script file whose original text is to be
        /// hashed.
        /// </param>
        /// <param name="noRemote">
        /// Non-zero to prevent the script file from being read from a
        /// remote location.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null on failure.
        /// </returns>
        public static byte[] HashScriptFile(
            Interpreter interpreter,
            string fileName,
            bool noRemote,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return _RuntimeOps.HashScriptFile(
                interpreter, fileName, noRemote, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new trace listener of the specified type and adds
        /// it to the normal and/or debug trace listener collections.
        /// </summary>
        /// <param name="listenerType">
        /// The type of trace listener to create and add.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data used during creation; this
        /// parameter may be null.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener
        /// collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to treat an existing listener of the same type as a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="listener">
        /// On input, this parameter is ignored; upon success, it
        /// receives the trace listener that was created and added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SetupTraceListeners(
            TraceListenerType listenerType,
            IClientData clientData,
            bool trace,
            bool debug,
            bool console,
            bool verbose,
            bool typeOnly,
            ref TraceListener listener,
            ref Result error
            )
        {
            return DebugOps.SetupTraceListeners(
                listenerType, clientData, trace,
                debug, console, verbose, typeOnly,
                ref listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the log file names associated with the trace
        /// listeners in the selected listener collection.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to operate on the debug trace listener collection;
        /// otherwise, the normal trace listener collection is used.
        /// </param>
        /// <param name="fileNames">
        /// On input, supplies an optional existing list; upon success,
        /// it receives the list of extracted log file names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// True if at least one log file name was extracted; otherwise,
        /// false.
        /// </returns>
        public static bool ExtractTraceLogFileNames(
            bool debug,
            ref StringList fileNames,
            ref Result error
            )
        {
            return DebugOps.ExtractTraceLogFileNames(
                debug, ref fileNames, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Queries the current status of the tracing subsystem,
        /// appending a set of name and value pairs describing it to the
        /// specified list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="list">
        /// On input, supplies the list to which the status information
        /// is appended; a new list is allocated when this parameter is
        /// null.  Upon success, it receives the status information.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode QueryTraceStatus(
            Interpreter interpreter,
            ref StringPairList list,
            ref Result error
            )
        {
            return TraceOps.QueryStatus(
                interpreter, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Obtains the character encoding associated with the specified
        /// encoding name.
        /// </summary>
        /// <param name="name">
        /// The name of the encoding to obtain.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The encoding associated with the specified name, or null if
        /// it cannot be obtained.
        /// </returns>
        public static Encoding GetEncoding(
            string name,
            ref Result error
            )
        {
            return StringOps.GetEncoding(name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// Creates a new log file trace listener and adds it to the
        /// normal and/or debug trace listener collections.
        /// </summary>
        /// <param name="name">
        /// An optional name to assign to the new trace listener; this
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the log file to write to.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the log file; this parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control log client-data handling; this parameter may be null.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener
        /// collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to treat an existing listener of the same type as a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="listener">
        /// On input, this parameter is ignored; upon success, it
        /// receives the trace listener that was created and added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SetupTraceLogFile(
            string name,
            string fileName,
            Encoding encoding,
            LogFlags? flags,
            bool trace,
            bool debug,
            bool console,
            bool verbose,
            bool typeOnly,
            ref TraceListener listener,
            ref Result error
            )
        {
            return DebugOps.SetupTraceLogFile(
                name, fileName, encoding, flags, trace, debug, console,
                verbose, typeOnly, ref listener, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Safely determines whether trace output should be sent to the
        /// host of the specified interpreter, falling back to an
        /// environment variable and then to false when the setting
        /// cannot otherwise be determined.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose setting is queried; this parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// True if trace output should be sent to the host; otherwise,
        /// false.
        /// </returns>
        public static bool ShouldTraceToHost(
            Interpreter interpreter
            ) /* SAFE-ON-DISPOSE */
        {
            return DebugOps.SafeGetTraceToHost(interpreter, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Wraps a raw object value as an opaque object handle,
        /// registering it with the specified interpreter so it can be
        /// passed where a handle string is expected.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to look up and create the
        /// opaque object handle; this parameter may be null.
        /// </param>
        /// <param name="value">
        /// The object value to wrap as an opaque object handle.
        /// </param>
        /// <returns>
        /// The string name of the opaque object handle for the value,
        /// the value itself when it is already a string, or null if it
        /// could not be wrapped.
        /// </returns>
        public static string WrapHandle(
            Interpreter interpreter,
            object value
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HandleOps.Wrap(interpreter, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the specified argument unchanged.  This is used so
        /// that the return-value fixup handling can be applied to any
        /// object.
        /// </summary>
        /// <param name="arg">
        /// The object to return unchanged.
        /// </param>
        /// <returns>
        /// The supplied <paramref name="arg" /> value, exactly as
        /// provided.
        /// </returns>
        public static object Identity(
            object arg
            )
        {
            return HandleOps.Identity(arg);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the global static lock and then
        /// immediately releases it, reporting whether the lock could be
        /// acquired.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time, in milliseconds, to wait for the lock to
        /// be acquired, or null to use the default soft-lock timeout.
        /// </param>
        /// <returns>
        /// True if the static lock was acquired; otherwise, false.
        /// </returns>
        public static bool TryLockAndExit(
            int? timeout
            )
        {
            return GlobalState.TryLockAndExit(timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the specified type argument unchanged.  This is used
        /// so that the return-value fixup handling can be applied to
        /// any type object.
        /// </summary>
        /// <param name="arg">
        /// The type to return unchanged.
        /// </param>
        /// <returns>
        /// The supplied <paramref name="arg" /> value, exactly as
        /// provided.
        /// </returns>
        public static Type TypeIdentity(
            Type arg
            )
        {
            return HandleOps.TypeIdentity(arg);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an assembly name object from its string
        /// representation.
        /// </summary>
        /// <param name="assemblyName">
        /// The string representation of the assembly name to parse.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The parsed assembly name, or null on failure.
        /// </returns>
        public static AssemblyName GetAssemblyName(
            string assemblyName,
            ref Result error
            )
        {
            return AssemblyOps.GetName(assemblyName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Finds the first assembly in the specified application domain
        /// whose full name matches the given pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used during pattern matching; this parameter
        /// may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search; when null, the current
        /// application domain is used.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare each assembly full name to
        /// the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against each assembly full name, or null
        /// to match any assembly.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform the pattern match without regard to case.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first assembly to consider, or null to
        /// start at the beginning.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindAssemblyInAppDomain(
            Interpreter interpreter,
            AppDomain appDomain,
            MatchMode mode,
            string pattern,
            bool noCase,
            int? startIndex,
            ref Result error
            )
        {
            return AssemblyOps.FindInAppDomain(
                interpreter, appDomain, mode, pattern, noCase,
                startIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Finds the first assembly in the specified application domain
        /// that matches the given simple name, version, and public key
        /// token.  Each criterion that is null is ignored.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to search; when null, the current
        /// application domain is used.
        /// </param>
        /// <param name="name">
        /// The simple name to require, or null to ignore the name.
        /// </param>
        /// <param name="version">
        /// The version to require, or null to ignore the version.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token to require, or null to ignore the
        /// public key token.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindAssemblyInAppDomain(
            AppDomain appDomain,
            string name,
            Version version,
            byte[] publicKeyToken,
            ref Result error
            )
        {
            return AssemblyOps.FindInAppDomain(
                appDomain, name, version, publicKeyToken, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Finds the first assembly in the specified application domain
        /// whose location matches the given file path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search; when null, the current
        /// application domain is used.
        /// </param>
        /// <param name="path">
        /// The file path to match against each assembly location.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first assembly to consider, or null to
        /// start at the beginning.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindAssemblyInAppDomain(
            Interpreter interpreter,
            IClientData clientData,
            AppDomain appDomain,
            string path,
            int? startIndex,
            ref Result error
            )
        {
            return AssemblyOps.FindInAppDomain(
                interpreter, clientData, appDomain, path,
                startIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether two assembly names refer to the same
        /// assembly, comparing them by reference and by full name.
        /// </summary>
        /// <param name="assemblyName1">
        /// The first assembly name to compare; this parameter may be
        /// null.
        /// </param>
        /// <param name="assemblyName2">
        /// The second assembly name to compare; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the assembly names are considered the same, including
        /// when both are null; otherwise, false.
        /// </returns>
        public static bool IsSameAssemblyName(
            AssemblyName assemblyName1,
            AssemblyName assemblyName2
            )
        {
            return AssemblyOps.IsSameAssemblyName(
                assemblyName1, assemblyName2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a deep copy of the master collection of all
        /// interpreters.
        /// </summary>
        /// <returns>
        /// A deep copy of the interpreter collection, or null if it
        /// could not be produced.
        /// </returns>
        public static InterpreterDictionary GetInterpreters()
        {
            return GlobalState.CloneInterpreterPairs();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of all tracked interpreters whose string
        /// representation matches the specified pattern.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used to compare each interpreter against
        /// the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which interpreters are disposed,
        /// or null to select all interpreters.
        /// </param>
        /// <param name="cancelFlags">
        /// The flags that control whether pending evaluations are
        /// canceled and whether global busy status is honored.
        /// </param>
        /// <returns>
        /// The number of interpreters that were disposed.
        /// </returns>
        public static int DisposeInterpreters(
            MatchMode mode,
            string pattern,
            CancelFlags cancelFlags
            )
        {
            return GlobalState.DisposeInterpreters(
                mode, pattern, cancelFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the cached assembly name of the Eagle core library
        /// assembly.
        /// </summary>
        /// <returns>
        /// The cached assembly name.
        /// </returns>
        public static AssemblyName GetPackageAssemblyName()
        {
            return GlobalState.GetAssemblyName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the package name for the specified package type.
        /// </summary>
        /// <param name="packageType">
        /// The package type whose name is requested.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to return the package name in lower case.
        /// </param>
        /// <returns>
        /// The package name; this value is never null.
        /// </returns>
        public static string GetPackageName(
            PackageType packageType,
            bool noCase
            )
        {
            return GlobalState.GetPackageName(packageType, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package name for the specified package
        /// type, optionally surrounding it with the given prefix and
        /// suffix.
        /// </summary>
        /// <param name="packageType">
        /// The package type whose package name is returned.
        /// </param>
        /// <param name="prefix">
        /// An optional string to prepend to the package name; this
        /// parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// An optional string to append to the package name; this
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to return the package name in lower case; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// The package name for the specified package type, surrounded by
        /// the prefix and suffix when provided.  This method does not
        /// return null.
        /// </returns>
        public static string GetPackageName(
            PackageType packageType,
            string prefix,
            string suffix,
            bool noCase
            )
        {
            return GlobalState.GetPackageName(
                packageType, prefix, suffix, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the version number of the Eagle core library
        /// package.
        /// </summary>
        /// <returns>
        /// The package version, or null if it cannot be determined.
        /// </returns>
        public static Version GetPackageVersion()
        {
            return GlobalState.GetPackageVersion(null);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The "mappings" dictionary passed here must contain mappings
        //       between (unqualified) assembly file names (e.g. "Harpy.dll",
        //       "Badge.dll", etc) and their contained (plugin) type names,
        //       e.g. "Licensing.Core", "Security.Core", "Badge.Enterprise",
        //       etc.
        //
        /// <summary>
        /// This method creates and evaluates the "package ifneeded"
        /// scripts for each assembly-to-plugin-names mapping, using the
        /// directories derived from the specified path and flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the scripts are created and
        /// evaluated.
        /// </param>
        /// <param name="mappings">
        /// The dictionary that maps unqualified assembly file names (e.g.
        /// "Harpy.dll") to their contained plugin type names (e.g.
        /// "Licensing.Core").
        /// </param>
        /// <param name="path">
        /// The primary path from which the candidate package directories
        /// are derived; this parameter may be null.
        /// </param>
        /// <param name="version">
        /// The package version string to match; this parameter may be null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token that candidate assemblies must have; this
        /// parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when extracting public key tokens; this
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the scripts are created and
        /// evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the list of scripts that
        /// were created; upon failure, it receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CreateAndEvaluatePackageIfNeededScripts(
            Interpreter interpreter,
            AssemblyFilePluginNames mappings,
            string path,
            Version version,
            byte[] publicKeyToken,
            CultureInfo cultureInfo,
            PackageIfNeededFlags flags,
            ref Result result
            )
        {
            return PackageOps.CreateAndEvaluateIfNeededScripts(
                interpreter, mappings, path, version, publicKeyToken,
                cultureInfo, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the text of a "package scan" command that
        /// can be used to rescan the specified paths for new package
        /// index files.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine whether plugin
        /// probing is enabled; this parameter may be null.
        /// </param>
        /// <param name="commandName">
        /// The command name to emit in place of the default "package"
        /// command name; this parameter may be null.
        /// </param>
        /// <param name="paths">
        /// The paths to be scanned; this parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The text of the constructed "package scan" command, or null if
        /// it is not available.
        /// </returns>
        public static string GetPackageScanCommand(
            Interpreter interpreter,
            string commandName,
            IEnumerable<string> paths,
            ref Result error
            )
        {
            return PackageOps.GetScanCommand(
                interpreter, commandName, paths, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares two version numbers, treating a null
        /// version as less than any non-null version.
        /// </summary>
        /// <param name="version1">
        /// The first version to compare; this parameter may be null.
        /// </param>
        /// <param name="version2">
        /// The second version to compare; this parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the versions are equal, a negative number if
        /// <paramref name="version1"/> is less than
        /// <paramref name="version2"/>, or a positive number if
        /// <paramref name="version1"/> is greater than
        /// <paramref name="version2"/>.
        /// </returns>
        public static int VersionCompare(
            Version version1,
            Version version2
            )
        {
            return PackageOps.VersionCompare(version1, version2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package path relative to the specified
        /// assembly, optionally appending the package name and version
        /// components.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used as the basis for the package path; this
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The package name component to append; this parameter may be
        /// null.
        /// </param>
        /// <param name="version">
        /// The package version component to append; this parameter may be
        /// null.
        /// </param>
        /// <param name="pathFlags">
        /// The flags that control how the package path is constructed.
        /// </param>
        /// <returns>
        /// The resulting package path, or null if no suitable library
        /// path is found.
        /// </returns>
        public static string GetPackagePath(
            Assembly assembly,
            string name,
            Version version,
            PathFlags pathFlags
            )
        {
            return GlobalState.GetPackagePath(
                assembly, name, version, pathFlags);
        }

        ///////////////////////////////////////////////////////////////////////

#if POLICY_TRACE
        /// <summary>
        /// This method enables or disables diagnostic tracing of policy
        /// decisions.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable policy decision tracing; otherwise, zero.
        /// </param>
        public static void SetPolicyTrace(
            bool enable
            )
        {
            GlobalState.PolicyTrace = enable;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the trusted hashes associated with the
        /// specified interpreter into the global list of trusted hashes,
        /// optionally clearing the global list first.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose trusted hashes are copied; this
        /// parameter may be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the global list of trusted hashes before
        /// copying; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CopyTrustedHashes(
            Interpreter interpreter,
            bool clear,
            ref Result error
            )
        {
            return GlobalState.CopyTrustedHashes(
                interpreter, clear, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the trusted hashes from the source
        /// interpreter into the target interpreter.
        /// </summary>
        /// <param name="sourceInterpreter">
        /// The interpreter whose trusted hashes are copied; this
        /// parameter may be null.
        /// </param>
        /// <param name="targetInterpreter">
        /// The interpreter that receives the copied trusted hashes; this
        /// parameter may be null.
        /// </param>
        public static void CopyTrustedHashes(
            Interpreter sourceInterpreter,
            Interpreter targetInterpreter
            )
        {
            PolicyOps.CopyTrustedHashes(sourceInterpreter, targetInterpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified hashes to the global list of
        /// trusted hashes, optionally clearing the global list first.
        /// </summary>
        /// <param name="hashes">
        /// The hashes to add to the global list of trusted hashes; this
        /// parameter may be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the global list before adding; otherwise,
        /// zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode AddTrustedHashes(
            IEnumerable<string> hashes,
            bool clear,
            ref Result error
            )
        {
            return GlobalState.AddTrustedHashes(hashes, clear, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the base path used by the library.
        /// </summary>
        /// <param name="basePath">
        /// The replacement base path to install.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh the paths derived from the base path so
        /// the change takes effect; otherwise, zero.
        /// </param>
        public static void SetBasePath(
            string basePath,
            bool refresh
            )
        {
            GlobalState.SetBasePath(basePath, refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the shared library path used by the library.
        /// </summary>
        /// <param name="libraryPath">
        /// The replacement library path to install; this parameter may be null.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh the dependent package paths so the change
        /// takes effect; otherwise, zero.
        /// </param>
        public static void SetLibraryPath(
            string libraryPath,
            bool refresh
            )
        {
            GlobalState.SetLibraryPath(libraryPath, refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified trace priority as a string,
        /// optionally including its non-base flag bits in hexadecimal.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to format.
        /// </param>
        /// <param name="baseOnly">
        /// Non-zero to return only the base priority name; zero to also
        /// append the remaining flag bits in hexadecimal.
        /// </param>
        /// <param name="shortName">
        /// Non-zero to use the abbreviated name for the base priority;
        /// otherwise, zero.
        /// </param>
        /// <returns>
        /// The formatted trace priority string, which always contains at
        /// least the base priority name.
        /// </returns>
        public static string FormatTracePriority(
            TracePriority priority,
            bool baseOnly,
            bool shortName
            )
        {
            return FormatOps.TracePriority(priority, baseOnly, shortName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a full platform name describing the
        /// runtime, configuration, platform, process bits, and machine
        /// associated with the Eagle core library assembly.
        /// </summary>
        /// <returns>
        /// The full platform name string; unavailable components are
        /// represented by a placeholder.
        /// </returns>
        public static string GetFullPlatformName()
        {
            return FormatOps.FullPlatformName(GlobalState.GetAssembly());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the file name, optionally including a
        /// library path fragment, associated with the specified script
        /// type.
        /// </summary>
        /// <param name="type">
        /// The script type to convert to a file name; this parameter may
        /// be null.
        /// </param>
        /// <param name="packageType">
        /// The package type that selects the library path fragment
        /// prefix.
        /// </param>
        /// <param name="fileNameOnly">
        /// Non-zero to omit the library path fragment; zero to include
        /// the appropriate path prefix.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return null when <paramref name="type"/> is null
        /// or empty; zero to return the value unchanged.
        /// </param>
        /// <returns>
        /// The resulting file name, or null if strict semantics are used
        /// and <paramref name="type"/> is null or empty.
        /// </returns>
        public static string ScriptTypeToFileName(
            string type,
            PackageType packageType,
            bool fileNameOnly,
            bool strict
            )
        {
            return FormatOps.ScriptTypeToFileName(
                type, packageType, fileNameOnly, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified variable as undefined or
        /// defined.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify; this parameter may be null.
        /// </param>
        /// <param name="undefined">
        /// Non-zero to mark the variable as undefined; zero to mark it as
        /// defined.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and was modified; otherwise,
        /// false.
        /// </returns>
        public static bool SetVariableUndefined(
            IVariable variable,
            bool undefined
            )
        {
            return EntityOps.SetUndefined(variable, undefined);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the dirty flag on the specified
        /// variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify; this parameter may be null.
        /// </param>
        /// <param name="dirty">
        /// Non-zero to set the dirty flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were modified;
        /// otherwise, false.
        /// </returns>
        public static bool SetVariableDirty(
            IVariable variable,
            bool dirty
            )
        {
            return EntityOps.SetDirty(variable, dirty);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified variable, and optionally one
        /// of its array elements, as dirty.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify; this parameter may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to mark as dirty, or null to mark only
        /// the variable itself; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were updated;
        /// otherwise, false.
        /// </returns>
        public static bool SignalVariableDirty(
            IVariable variable,
            string index
            )
        {
            return EntityOps.SignalDirty(variable, index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the verb (e.g. "verify", "read", "set",
        /// or "unset") associated with the specified breakpoint type.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type to translate.
        /// </param>
        /// <returns>
        /// The verb associated with the breakpoint type, or the empty
        /// string if there is no matching verb.
        /// </returns>
        public static string FormatBreakpoint(
            BreakpointType breakpointType
            )
        {
            return FormatOps.Breakpoint(breakpointType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a formatted complaint message from the
        /// specified identifier, return code, result, and stack trace.
        /// </summary>
        /// <param name="id">
        /// The identifier associated with the complaint.
        /// </param>
        /// <param name="code">
        /// The return code indicating the type of error.
        /// </param>
        /// <param name="result">
        /// The result containing the error details; this parameter may be
        /// null.
        /// </param>
        /// <param name="stackTrace">
        /// The caller-provided stack trace text; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted complaint message string.
        /// </returns>
        public static string FormatComplaint(
            long id,
            ReturnCode code,
            Result result,
            string stackTrace
            )
        {
            return FormatOps.Complaint(id, code, result, stackTrace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the error display name for a variable with
        /// the specified name and array element index.
        /// </summary>
        /// <param name="varName">
        /// The variable name.
        /// </param>
        /// <param name="varIndex">
        /// The array element index, or null to format only the variable
        /// name; this parameter may be null.
        /// </param>
        /// <returns>
        /// A name of the form "varName(varIndex)" when both are provided,
        /// or just the variable name when <paramref name="varIndex"/> is
        /// null; null if <paramref name="varIndex"/> is provided but
        /// <paramref name="varName"/> is null.
        /// </returns>
        public static string FormatErrorVariableName(
            string varName,
            string varIndex
            )
        {
            return FormatOps.ErrorVariableName(varName, varIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the base path used by the library.
        /// </summary>
        /// <returns>
        /// The base path, or null if it could not be determined.
        /// </returns>
        public static string GetBasePath()
        {
            return GlobalState.GetBasePath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the externals path used by the library.
        /// </summary>
        /// <returns>
        /// The externals path, or null if it could not be determined.
        /// </returns>
        public static string GetExternalsPath()
        {
            return GlobalState.GetExternalsPath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the binary base path used by the library,
        /// initializing it first if necessary.
        /// </summary>
        /// <returns>
        /// The binary base path, or null if it could not be obtained.
        /// </returns>
        public static string GetBinaryPath()
        {
            //
            // HACK: We do not know if the external caller already has an
            //       interpreter; therefore, make sure the binary path is
            //       initialized here.
            //
            return GlobalState.InitializeOrGetBinaryPath(true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally sets the binary base path used by
        /// the library.
        /// </summary>
        /// <param name="binaryPath">
        /// The replacement binary base path to install.
        /// </param>
        /// <param name="force">
        /// Non-zero to overwrite an existing binary base path; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the binary base path was set; otherwise, false.
        /// </returns>
        public static bool MaybeSetBinaryPath(
            string binaryPath,
            bool force
            )
        {
            return GlobalState.MaybeSetBinaryPath(binaryPath, force);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the per-user directory that should be
        /// used for storing document files, falling back to the
        /// configured home directories when necessary.
        /// </summary>
        /// <returns>
        /// The full path of the document directory, or null if no
        /// suitable directory could be determined.
        /// </returns>
        public static string GetDocumentDirectory()
        {
            return PathOps.GetDocumentDirectory(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the fully qualified file name of the
        /// executable file for the current process.
        /// </summary>
        /// <returns>
        /// The fully qualified executable file name, or null if it could
        /// not be determined.
        /// </returns>
        public static string GetExecutableName()
        {
            return PathOps.GetExecutableName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the public key token of the specified
        /// assembly name, formatted as a hexadecimal string.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose public key token is returned; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The public key token as a hexadecimal string, or null if it
        /// cannot be determined.
        /// </returns>
        public static string GetAssemblyPublicKeyToken(
            AssemblyName assemblyName
            )
        {
            return AssemblyOps.GetPublicKeyToken(assemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the X.509 version 2 certificate associated
        /// with the specified signed file, optionally consulting and
        /// updating the certificate cache.
        /// </summary>
        /// <param name="fileName">
        /// The name of the signed file whose certificate is loaded; this
        /// parameter may be null.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to bypass the certificate cache and load directly
        /// from the file; otherwise, zero.
        /// </param>
        /// <param name="certificate2">
        /// The input value of this parameter is ignored; upon success, it
        /// receives the loaded certificate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetAssemblyCertificate2(
            string fileName,
            bool noCache,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            return CertificateOps.GetCertificate2(
                fileName, noCache, ref certificate2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the X.509 version 2 certificate used to sign
        /// the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose signer certificate is returned; this
        /// parameter may be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of a certificate as a failure;
        /// zero to return success with a null certificate when none
        /// exists.
        /// </param>
        /// <param name="certificate2">
        /// The input value of this parameter is ignored; upon success, it
        /// receives the signer certificate, which may be null when
        /// <paramref name="strict"/> is zero and no certificate exists.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetAssemblyCertificate2(
            Assembly assembly,
            bool strict,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            return AssemblyOps.GetCertificate2(
                assembly, strict, ref certificate2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifier declared by the
        /// object identifier attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose object identifier attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The declared object identifier, or Guid.Empty if the member is
        /// null, has no such attribute, or an error occurs.
        /// </returns>
        public static Guid GetObjectId(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetObjectId(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifier declared by the
        /// object identifier attribute of the runtime type of the
        /// specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type is queried for its object
        /// identifier attribute; this parameter may be null.
        /// </param>
        /// <returns>
        /// The declared object identifier, or Guid.Empty if the object is
        /// null or an error occurs.
        /// </returns>
        public static Guid GetObjectId(
            object @object
            )
        {
            return AttributeOps.GetObjectId(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the command flags declared by the command
        /// flags attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose command flags attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The declared command flags, or CommandFlags.None if the member
        /// is null, has no such attribute, or an error occurs.
        /// </returns>
        public static CommandFlags GetCommandFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetCommandFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the command flags declared by the command
        /// flags attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type is queried for its command flags
        /// attribute; this parameter may be null.
        /// </param>
        /// <returns>
        /// The declared command flags, or CommandFlags.None if the object
        /// is null or an error occurs.
        /// </returns>
        public static CommandFlags GetCommandFlags(
            object @object
            )
        {
            return AttributeOps.GetCommandFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the function flags declared by the
        /// function flags attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose function flags attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The declared function flags, or FunctionFlags.None if the
        /// member is null, has no such attribute, or an error occurs.
        /// </returns>
        public static FunctionFlags GetFunctionFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetFunctionFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the function flags declared by the
        /// function flags attribute of the runtime type of the specified
        /// object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type is queried for its function
        /// flags attribute; this parameter may be null.
        /// </param>
        /// <returns>
        /// The declared function flags, or FunctionFlags.None if the
        /// object is null or an error occurs.
        /// </returns>
        public static FunctionFlags GetFunctionFlags(
            object @object
            )
        {
            return AttributeOps.GetFunctionFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the processor architecture detected for the
        /// current process.
        /// </summary>
        /// <returns>
        /// The detected processor architecture, or
        /// ProcessorArchitecture.Unknown if it could not be determined.
        /// </returns>
        public static ProcessorArchitecture GetProcessorArchitecture()
        {
            return PlatformOps.GetProcessorArchitecture();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a plugin data object describing a plugin
        /// with the specified attributes.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain, if any, associated with the plugin;
        /// this parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly, if any, containing the plugin; this parameter
        /// may be null.
        /// </param>
        /// <param name="assemblyName">
        /// The name of the assembly containing the plugin.
        /// </param>
        /// <param name="dateTime">
        /// The date and time, if any, to associate with the plugin; this
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name, if any, of the assembly containing the plugin;
        /// this parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type.
        /// </param>
        /// <param name="uri">
        /// The uri, if any, to associate with the plugin; this parameter
        /// may be null.
        /// </param>
        /// <param name="updateUri">
        /// The update uri, if any, to associate with the plugin; this
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data, if any, to associate with the
        /// plugin; this parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags to associate with the plugin.
        /// </param>
        /// <returns>
        /// The created plugin data, or null if the assembly name is null.
        /// </returns>
        public static IPluginData CreatePluginData(
            AppDomain appDomain,
            Assembly assembly,
            AssemblyName assemblyName,
            DateTime? dateTime,
            string fileName,
            string typeName,
            Uri uri,
            Uri updateUri,
            IClientData clientData,
            PluginFlags flags
            )
        {
            return Interpreter.CreatePluginData(
                appDomain, assembly, assemblyName, dateTime,
                fileName, typeName, uri, updateUri, clientData,
                flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the portion of an absolute file name that
        /// is relative to one of the well-known package paths for the
        /// current application domain.
        /// </summary>
        /// <param name="fileName">
        /// The absolute file name to make relative to a package path.
        /// </param>
        /// <param name="keepLib">
        /// Non-zero to retain the trailing library directory component;
        /// otherwise, zero.
        /// </param>
        /// <param name="verbatim">
        /// Non-zero to return the computed name exactly; zero to remove
        /// the intermediate platform directory component if present.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The file name relative to the matched package path, or null if
        /// it is not relative to any package path or an error occurs.
        /// </returns>
        public static string GetPackageRelativeFileName(
            string fileName,
            bool keepLib,
            bool verbatim,
            ref Result error
            )
        {
            return PathOps.GetPackageRelativeFileName(
                fileName, keepLib, verbatim, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines a relative file name with the directory
        /// that contains the file for the specified plugin.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose containing directory is used as the base;
        /// this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data associated with the operation; this
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The relative file name to combine with the plugin directory;
        /// this parameter may be null.
        /// </param>
        /// <returns>
        /// The combined file name, or null if the plugin or file name is
        /// invalid, the file name is rooted, the plugin file name cannot
        /// be determined, or an error occurs.
        /// </returns>
        public static string GetPluginRelativeFileName(
            IPlugin plugin,
            IClientData clientData,
            string fileName
            )
        {
            return PathOps.GetPluginRelativeFileName(
                plugin, clientData, fileName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the plugin flags declared by the plugin
        /// flags attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose plugin flags attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The declared plugin flags, or PluginFlags.None if the member
        /// is null, has no such attribute, or an error occurs.
        /// </returns>
        public static PluginFlags GetPluginFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetPluginFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the plugin flags declared by the plugin
        /// flags attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type is queried for its plugin flags
        /// attribute; this parameter may be null.
        /// </param>
        /// <returns>
        /// The declared plugin flags, or PluginFlags.None if the object
        /// is null or an error occurs.
        /// </returns>
        public static PluginFlags GetPluginFlags(
            object @object
            )
        {
            return AttributeOps.GetPluginFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the notify flags declared by the notify
        /// flags attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose notify flags attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The declared notify flags, or NotifyFlags.None if the member
        /// is null, has no such attribute, or an error occurs.
        /// </returns>
        public static NotifyFlags GetNotifyFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetNotifyFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the notify types declared by the notify
        /// types attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose notify types attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The declared notify types, or NotifyType.None if the member is
        /// null, has no such attribute, or an error occurs.
        /// </returns>
        public static NotifyType GetNotifyTypes(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetNotifyTypes(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the build date and time associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query; this parameter may be null.
        /// </param>
        /// <returns>
        /// The build date and time, or DateTime.MinValue if it cannot be
        /// determined.
        /// </returns>
        public static DateTime GetAssemblyDateTime(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyDateTime(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the configuration string declared by the
        /// assembly configuration attribute of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose configuration attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The configuration string, or null if the assembly is null,
        /// has no such attribute, or an error occurs.
        /// </returns>
        public static string GetAssemblyConfiguration(
            Assembly assembly
            )
        {
            return AttributeOps.GetAssemblyConfiguration(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the tag associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query; this parameter may be null.
        /// </param>
        /// <returns>
        /// The assembly tag, or null if it cannot be determined.
        /// </returns>
        public static string GetAssemblyTag(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyTag(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the descriptive text associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query; this parameter may be null.
        /// </param>
        /// <returns>
        /// The descriptive text, or null if it cannot be determined.
        /// </returns>
        public static string GetAssemblyText(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyText(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the descriptive text associated with the
        /// specified assembly, falling back to the assembly base suffix
        /// when no descriptive text is available.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose text or base suffix is returned; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The descriptive text if available; otherwise, the assembly
        /// base suffix, or null if neither is available.
        /// </returns>
        public static string GetAssemblyTextOrSuffix(
            Assembly assembly
            )
        {
            return _RuntimeOps.GetAssemblyTextOrSuffix(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the title declared by the assembly title
        /// attribute of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose title attribute is queried; this parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The title string, or null if the assembly is null, has no title
        /// attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblyTitle(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyTitle(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the description declared by the assembly
        /// description attribute of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose description attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The description string, or null if the assembly is null, has no
        /// description attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblyDescription(
            Assembly assembly
            )
        {
            return AttributeOps.GetAssemblyDescription(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default URI associated with the
        /// specified assembly, taken from its assembly URI attribute.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query; this parameter may be null.
        /// </param>
        /// <returns>
        /// The default URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyUri(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyUri(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the XML schema URI associated with the
        /// specified assembly, taken from the matching assembly URI
        /// attribute.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query; this parameter may be null.
        /// </param>
        /// <returns>
        /// The XML schema URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyXmlSchemaUri(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyXmlSchemaUri(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the URI with the specified name that is
        /// associated with the specified assembly, taken from its assembly
        /// URI attributes.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query; this parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name identifying which URI to retrieve; this parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The matching URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyUri(
            Assembly assembly,
            string name
            )
        {
            return SharedAttributeOps.GetAssemblyUri(assembly, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the version of the specified assembly,
        /// taken from its assembly name.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose version is returned; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The version of the assembly, or null if it cannot be
        /// determined.
        /// </returns>
        public static Version GetAssemblyVersion(
            Assembly assembly
            )
        {
            return AssemblyOps.GetVersion(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the cached version of the Eagle core
        /// library assembly.
        /// </summary>
        /// <returns>
        /// The Eagle core library assembly version.
        /// </returns>
        public static Version GetEagleVersion()
        {
            return GlobalState.GetAssemblyVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the source identifier declared by the
        /// assembly source-id attribute of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose source-id attribute is queried; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The source identifier string, or null if the assembly is null,
        /// has no source-id attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblySourceId(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblySourceId(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the source time stamp declared by the
        /// assembly source-time-stamp attribute of the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose source-time-stamp attribute is queried;
        /// this parameter may be null.
        /// </param>
        /// <returns>
        /// The source time stamp string, or null if the assembly is null,
        /// has no such attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblySourceTimeStamp(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblySourceTimeStamp(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this build of Eagle was compiled
        /// with threading support enabled.
        /// </summary>
        /// <param name="interpreter">
        /// An interpreter context; this parameter may be null and is not
        /// actually used.
        /// </param>
        /// <returns>
        /// True if threading support is available; otherwise, false.
        /// </returns>
        public static bool HaveEagleThreading(
            Interpreter interpreter
            )
        {
            return _RuntimeOps.HaveThreading(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this build of Eagle was compiled
        /// with native code support enabled.
        /// </summary>
        /// <param name="interpreter">
        /// An interpreter context; this parameter may be null and is not
        /// actually used.
        /// </param>
        /// <returns>
        /// True if native code support is available; otherwise, false.
        /// </returns>
        public static bool HaveEagleNative(
            Interpreter interpreter
            )
        {
            return _RuntimeOps.HaveNative(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified compile-time
        /// define constant was present when this build of Eagle was
        /// compiled.
        /// </summary>
        /// <param name="name">
        /// The name of the define constant to check for.
        /// </param>
        /// <returns>
        /// True if the define constant is present; otherwise, false
        /// (including when the name is null or empty).
        /// </returns>
        public static bool HaveEagleDefineConstant(
            string name
            )
        {
            return _RuntimeOps.HaveDefineConstant(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the Eagle core library assembly.
        /// </summary>
        /// <returns>
        /// The Eagle core library assembly.
        /// </returns>
        public static Assembly GetAssembly()
        {
            return GlobalState.GetAssembly();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the source identifier declared by the
        /// source-id attribute of the Eagle core library assembly.
        /// </summary>
        /// <returns>
        /// The Eagle source identifier string, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetEagleSourceId()
        {
            return SharedAttributeOps.GetAssemblySourceId(
                GlobalState.GetAssembly());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the list of compile-time define constants
        /// that were present when this build of Eagle was compiled.
        /// </summary>
        /// <returns>
        /// The list of define constant names.
        /// </returns>
        public static StringList GetEagleDefineConstants()
        {
            return Eagle._Constants.DefineConstants.OptionList;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether an interpreter associated with
        /// the specified token currently exists and still matches that
        /// token.
        /// </summary>
        /// <param name="token">
        /// The interpreter token to look up; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if a matching interpreter exists; otherwise, false
        /// (including when the token is null).
        /// </returns>
        public static bool DoesTokenInterpreterExist(
            ulong? token
            )
        {
            if (token == null)
                return false;

            ulong localToken = (ulong)token;
            Interpreter interpreter;
            Result error = null;

            interpreter = GlobalState.GetTokenInterpreter(
                localToken, ref error);

            if (interpreter == null)
                return false;

            return interpreter.MatchToken(localToken);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified environment
        /// variable exists.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to check.
        /// </param>
        /// <returns>
        /// True if the environment variable exists; otherwise, false
        /// (including when an exception is caught).
        /// </returns>
        public static bool DoesEnvironmentVariableExist(
            string variable
            )
        {
            return CommonOps.Environment.DoesVariableExist(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified environment
        /// variable exists and, if so, captures its value.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to check.
        /// </param>
        /// <param name="value">
        /// Upon return, this receives the value of the environment
        /// variable, or null if it does not exist; the value on input is
        /// ignored.
        /// </param>
        /// <returns>
        /// True if the environment variable exists; otherwise, false
        /// (including when an exception is caught).
        /// </returns>
        public static bool DoesEnvironmentVariableExist(
            string variable,
            ref string value
            )
        {
            return CommonOps.Environment.DoesVariableExist(
                variable, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified environment
        /// variable exists and, if it does, captures its value and then
        /// removes it.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to check and remove.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the value of the environment
        /// variable; otherwise, it is set to null; the value on input is
        /// ignored.
        /// </param>
        /// <returns>
        /// True if the environment variable existed (and was removed);
        /// otherwise, false (including when an exception is caught).
        /// </returns>
        public static bool DoesEnvironmentVariableExistOnce(
            string variable,
            ref string value
            )
        {
            return CommonOps.Environment.DoesVariableExistOnce(
                variable, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the value of the specified environment
        /// variable.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to query.
        /// </param>
        /// <returns>
        /// The value of the environment variable, or a fallback value if
        /// it does not exist or an exception is caught.
        /// </returns>
        public static string GetEnvironmentVariable(
            string variable
            )
        {
            return CommonOps.Environment.GetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the value of the specified global
        /// configuration variable (such as an environment variable),
        /// optionally applying the default name prefix and expanding any
        /// environment variable references in the result.
        /// </summary>
        /// <param name="variable">
        /// The name of the configuration variable to query.
        /// </param>
        /// <param name="prefixed">
        /// Non-zero to apply the default name prefix to the variable name
        /// before looking it up; otherwise, zero.
        /// </param>
        /// <param name="expand">
        /// Non-zero to expand any environment variable references
        /// contained in the resulting value; otherwise, zero.
        /// </param>
        /// <returns>
        /// The configuration value, or null if it does not exist.
        /// </returns>
        public static string GetEnvironmentVariable(
            string variable,
            bool prefixed,
            bool expand
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            if (expand)
                flags |= ConfigurationFlags.Expand;

            return GlobalConfiguration.GetValue(variable, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the value of the specified environment
        /// variable and then removes it.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to query and remove.
        /// </param>
        /// <returns>
        /// The original value of the environment variable, or a fallback
        /// value if it did not exist or an exception is caught.
        /// </returns>
        public static string GetAndUnsetEnvironmentVariable(
            string variable
            )
        {
            return CommonOps.Environment.GetAndUnsetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the specified environment variable to the
        /// specified value.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to set.
        /// </param>
        /// <param name="value">
        /// The value to assign to the environment variable.
        /// </param>
        /// <returns>
        /// True if the environment variable was set successfully;
        /// otherwise, false.
        /// </returns>
        public static bool SetEnvironmentVariable(
            string variable,
            string value
            )
        {
            return CommonOps.Environment.SetVariable(variable, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the specified global configuration variable
        /// (such as an environment variable) to the specified value,
        /// optionally applying the default name prefix to the variable
        /// name.
        /// </summary>
        /// <param name="variable">
        /// The name of the configuration variable to set.
        /// </param>
        /// <param name="value">
        /// The value to assign to the configuration variable.
        /// </param>
        /// <param name="prefixed">
        /// Non-zero to apply the default name prefix to the variable name
        /// before setting it; otherwise, zero.
        /// </param>
        public static void SetEnvironmentVariable(
            string variable,
            string value,
            bool prefixed
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            GlobalConfiguration.SetValue(variable, value, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified environment variable.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to remove.
        /// </param>
        /// <returns>
        /// True if the environment variable was removed successfully;
        /// otherwise, false.
        /// </returns>
        public static bool UnsetEnvironmentVariable(
            string variable
            )
        {
            return CommonOps.Environment.UnsetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the specified environment variable to a new
        /// value and returns its previous value.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to change.
        /// </param>
        /// <param name="value">
        /// The new value to assign to the environment variable.
        /// </param>
        /// <returns>
        /// The previous value of the environment variable, or a fallback
        /// value if an exception is caught.
        /// </returns>
        public static string ChangeEnvironmentVariable(
            string variable,
            string value
            )
        {
            return CommonOps.Environment.ChangeVariable(variable, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the specified environment variable to a new
        /// value only when its current value matches the specified old
        /// value, and returns the value it had prior to any change.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to change.
        /// </param>
        /// <param name="oldValue">
        /// The value the environment variable must currently have in
        /// order for the change to take place.
        /// </param>
        /// <param name="newValue">
        /// The new value to assign when the current value matches
        /// <paramref name="oldValue"/>.
        /// </param>
        /// <returns>
        /// The value of the environment variable prior to any change, or
        /// a fallback value if an exception is caught.
        /// </returns>
        public static string MaybeChangeEnvironmentVariable(
            string variable,
            string oldValue,
            string newValue
            )
        {
            return CommonOps.Environment.MaybeChangeVariable(
                variable, oldValue, newValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified global configuration variable
        /// (such as an environment variable), optionally applying the
        /// default name prefix to the variable name.
        /// </summary>
        /// <param name="variable">
        /// The name of the configuration variable to remove.
        /// </param>
        /// <param name="prefixed">
        /// Non-zero to apply the default name prefix to the variable name
        /// before removing it; otherwise, zero.
        /// </param>
        public static void UnsetEnvironmentVariable(
            string variable,
            bool prefixed
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            GlobalConfiguration.UnsetValue(variable, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method expands any environment variable references
        /// contained within the specified string.
        /// </summary>
        /// <param name="name">
        /// The string that may contain environment variable references to
        /// expand.
        /// </param>
        /// <returns>
        /// The string with any environment variable references expanded,
        /// or a fallback value if an exception is caught.
        /// </returns>
        public static string ExpandEnvironmentVariables(
            string name
            )
        {
            return CommonOps.Environment.ExpandVariables(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the current values of the specified
        /// environment variables into a client data object so they may be
        /// restored later.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to save.
        /// </param>
        /// <param name="clientData">
        /// Upon success, this receives the client data object holding the
        /// saved environment variable values; the value on input is
        /// ignored.
        /// </param>
        /// <returns>
        /// True if the environment variables were saved successfully;
        /// otherwise, false.
        /// </returns>
        public static bool SaveEnvironmentVariables(
            IEnumerable<string> names,
            ref IClientData clientData
            )
        {
            return CommonOps.Environment.SaveVariables(
                names, ref clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method applies the saved values of the specified
        /// environment variables from the specified client data object.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to set.
        /// </param>
        /// <param name="clientData">
        /// The client data object holding the saved environment variable
        /// values, as produced by SaveEnvironmentVariables.
        /// </param>
        /// <returns>
        /// True if the environment variables were set successfully;
        /// otherwise, false (including when the client data is not of the
        /// expected type).
        /// </returns>
        public static bool SetEnvironmentVariables(
            IEnumerable<string> names,
            IClientData clientData
            )
        {
            return CommonOps.Environment.SetVariables(
                names, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the saved values of the specified
        /// environment variables from the specified client data object.
        /// </summary>
        /// <param name="names">
        /// The names of the environment variables to restore.
        /// </param>
        /// <param name="clientData">
        /// The client data object holding the saved environment variable
        /// values, as produced by SaveEnvironmentVariables.
        /// </param>
        /// <returns>
        /// True if the environment variables were restored successfully;
        /// otherwise, false (including when the client data is not of the
        /// expected type).
        /// </returns>
        public static bool RestoreEnvironmentVariables(
            IEnumerable<string> names,
            IClientData clientData
            )
        {
            return CommonOps.Environment.RestoreVariables(
                names, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a string representation from the specified
        /// object, handling the string-like types supported by the engine
        /// and falling back to its string representation when necessary.
        /// </summary>
        /// <param name="object">
        /// The object to obtain a string representation from; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The string representation of the object, or null if one cannot
        /// be obtained.
        /// </returns>
        public static string GetStringFromObject(
            object @object
            )
        {
            return StringOps.GetStringFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains an argument from the specified object,
        /// converting the various string-like and enumerable types
        /// supported by the engine as necessary.
        /// </summary>
        /// <param name="object">
        /// The object to obtain an argument from; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The argument for the object, or null if one cannot be
        /// obtained.
        /// </returns>
        public static Argument GetArgumentFromObject(
            object @object
            )
        {
            return StringOps.GetArgumentFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a result from the specified object,
        /// converting the various string-like and enumerable types
        /// supported by the engine as necessary.
        /// </summary>
        /// <param name="object">
        /// The object to obtain a result from; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The result for the object, or null if one cannot be obtained.
        /// </returns>
        public static Result GetResultFromObject(
            object @object
            )
        {
            return StringOps.GetResultFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps a nullable console preference to the
        /// corresponding trace listener type.
        /// </summary>
        /// <param name="console">
        /// Non-zero to request the console listener type; zero to request
        /// the default listener type; null to request automatic
        /// detection.
        /// </param>
        /// <returns>
        /// The trace listener type that corresponds to the specified
        /// preference.
        /// </returns>
        public static TraceListenerType GetTraceListenerType(
            bool? console
            )
        {
            return DebugOps.GetTraceListenerType(console);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new trace listener of the specified
        /// type.
        /// </summary>
        /// <param name="listenerType">
        /// The type of trace listener to create.
        /// </param>
        /// <param name="clienData">
        /// The optional client data used during creation; this parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// The newly created trace listener, or null on failure.
        /// </returns>
        public static TraceListener NewTraceListener(
            TraceListenerType listenerType,
            IClientData clienData,
            ref Result error
            )
        {
            return DebugOps.NewTraceListener(
                listenerType, clienData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified trace listener to the selected
        /// listener collection.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to add; this parameter may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection;
        /// otherwise, the trace listener collection is used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            return DebugOps.AddTraceListener(listener, debug);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified trace listener from the
        /// selected listener collection.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to remove; this parameter may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection;
        /// otherwise, the trace listener collection is used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            return DebugOps.RemoveTraceListener(listener, debug);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new core procedure instance of the
        /// appropriate type based on the flags contained in the specified
        /// procedure data.
        /// </summary>
        /// <param name="procedureData">
        /// The data describing the procedure to create, including its
        /// flags.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// The newly created procedure, or null if it could not be
        /// created.
        /// </returns>
        public static IProcedure NewCoreProcedure(
            IProcedureData procedureData,
            ref Result error
            )
        {
            return _RuntimeOps.NewCoreProcedure(procedureData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified URI is a
        /// supported web URI, based on the schemes permitted by the
        /// supplied flags.  This overload does not return the host name.
        /// </summary>
        /// <param name="uri">
        /// The URI to check; this cannot be null and must be absolute.
        /// </param>
        /// <param name="flags">
        /// On input, the flags specifying which schemes are allowed; on
        /// output, the "was" scheme bits are updated to reflect the
        /// detected scheme.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// True if the URI uses one of the allowed schemes; otherwise,
        /// false.
        /// </returns>
        public static bool IsWebUri(
            Uri uri,
            ref UriFlags flags,
            ref Result error
            )
        {
            return PathOps.IsWebUri(uri, ref flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified URI is a
        /// supported web URI, based on the schemes permitted by the
        /// supplied flags, and optionally returns its host name.
        /// </summary>
        /// <param name="uri">
        /// The URI to check; this cannot be null and must be absolute.
        /// </param>
        /// <param name="flags">
        /// On input, the flags specifying which schemes are allowed (and
        /// whether the host is required); on output, the "was" scheme
        /// bits are updated to reflect the detected scheme.
        /// </param>
        /// <param name="host">
        /// Upon success, and unless the no-host flag is set, this receives
        /// the DNS-safe host name of the URI; the value on input is
        /// ignored.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// True if the URI uses one of the allowed schemes; otherwise,
        /// false.
        /// </returns>
        public static bool IsWebUri(
            Uri uri,
            ref UriFlags flags,
            ref string host,
            ref Result error
            )
        {
            return PathOps.IsWebUri(uri, ref flags, ref host, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is a
        /// remote (non-file) absolute URI.  This overload does not return
        /// the parsed URI and does not treat existing local files as a
        /// match.
        /// </summary>
        /// <param name="value">
        /// The string value to check.
        /// </param>
        /// <returns>
        /// True if the value is an absolute URI that does not use the file
        /// scheme; otherwise, false.
        /// </returns>
        public static bool IsRemoteUri(
            string value
            )
        {
            return PathOps.IsRemoteUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is a
        /// remote (non-file) absolute URI, returning the parsed URI.
        /// This overload does not treat existing local files as a match.
        /// </summary>
        /// <param name="value">
        /// The string value to check.
        /// </param>
        /// <param name="uri">
        /// Upon return, this receives the parsed absolute URI, or null if
        /// the value could not be parsed; the value on input is ignored.
        /// </param>
        /// <returns>
        /// True if the value is an absolute URI that does not use the file
        /// scheme; otherwise, false.
        /// </returns>
        public static bool IsRemoteUri(
            string value,
            ref Uri uri
            )
        {
            return PathOps.IsRemoteUri(value, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a leading or trailing "None" or
        /// "Default" name (and its associated separator) from the string
        /// representation of an enumerated value.
        /// </summary>
        /// <param name="value">
        /// The enumeration string value to fix up.
        /// </param>
        /// <returns>
        /// The fixed-up enumeration string value (the original value when
        /// it is null or empty).
        /// </returns>
        public static string FixupEnumString(
            string value
            )
        {
            return EnumOps.FixupEnumString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the value of the specified
        /// enumerated type that corresponds to the specified underlying
        /// value.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to obtain the value as.
        /// </param>
        /// <param name="value">
        /// The underlying value to convert into an enumerated value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryGetEnum(
            Type enumType,
            object value,
            ref Result error
            )
        {
            return EnumOps.TryGet(enumType, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the
        /// specified enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer;
        /// otherwise, zero.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names case-insensitively;
        /// otherwise, matching is case-sensitive.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase
            )
        {
            return EnumOps.TryParse(enumType, value, allowInteger, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the
        /// specified enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer;
        /// otherwise, zero.
        /// </param>
        /// <param name="ignoreLeading">
        /// Non-zero to ignore a leading non-identifier character when
        /// matching enumeration names; otherwise, zero.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error;
        /// otherwise, zero.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names case-insensitively;
        /// otherwise, matching is case-sensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool ignoreLeading,
            bool errorOnNotFound,
            bool noCase,
            ref Result error
            )
        {
            return EnumOps.TryParse(
                enumType, value, allowInteger, ignoreLeading, errorOnNotFound,
                noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse a string into a value of the
        /// specified enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type to parse the value as.
        /// </param>
        /// <param name="value">
        /// The string value to parse.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit the value to be expressed as an integer;
        /// otherwise, zero.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match enumeration names case-insensitively;
        /// otherwise, matching is case-sensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// The enumerated value, or null if it could not be obtained.
        /// </returns>
        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase,
            ref Result error
            )
        {
            return EnumOps.TryParse(
                enumType, value, allowInteger, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses a flags enumeration value by applying the operators and
        /// names in the new value string to the old value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The flags enumerated type to parse against.
        /// </param>
        /// <param name="oldValue">
        /// The initial enumeration value, as a string; this parameter may
        /// be null or empty.
        /// </param>
        /// <param name="newValue">
        /// The string of operators and enumeration names to apply to the
        /// initial value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used while parsing; this parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit raw integer values in addition to enumeration
        /// names.
        /// </param>
        /// <param name="errorOnNop">
        /// Non-zero to treat an empty new value as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case while matching names; otherwise, matching
        /// is case-sensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The resulting boxed enumeration value, or null if parsing fails.
        /// </returns>
        public static object TryParseFlagsEnum(
            Interpreter interpreter,
            Type enumType,
            string oldValue,
            string newValue,
            CultureInfo cultureInfo,
            bool allowInteger,
            bool errorOnNop,
            bool noCase,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return EnumOps.TryParseFlags(
                interpreter, enumType, oldValue, newValue, cultureInfo,
                allowInteger, errorOnNop, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses a flags enumeration value by applying the operators and
        /// names in the new value string to the old value, honoring an
        /// optional set of mask values and mask operators.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The flags enumerated type to parse against.
        /// </param>
        /// <param name="oldValue">
        /// The initial enumeration value, as a string; this parameter may
        /// be null or empty.
        /// </param>
        /// <param name="newValue">
        /// The string of operators and enumeration names to apply to the
        /// initial value.
        /// </param>
        /// <param name="maskValues">
        /// The set of flag values that may be used, constraining the parse;
        /// this parameter may be null.
        /// </param>
        /// <param name="maskOperators">
        /// The set of operators that may be used, constraining the parse;
        /// this parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used while parsing; this parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit raw integer values in addition to enumeration
        /// names.
        /// </param>
        /// <param name="errorOnNop">
        /// Non-zero to treat an empty new value as an error.
        /// </param>
        /// <param name="errorOnMask">
        /// Non-zero to treat use of a masked value or operator as an error
        /// instead of silently skipping it.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case while matching names; otherwise, matching
        /// is case-sensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The resulting boxed enumeration value, or null if parsing fails.
        /// </returns>
        public static object TryParseFlagsEnum(
            Interpreter interpreter,
            Type enumType,
            string oldValue,
            string newValue,
            string maskValues,
            string maskOperators,
            CultureInfo cultureInfo,
            bool allowInteger,
            bool errorOnNop,
            bool errorOnMask,
            bool noCase,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return EnumOps.TryParseFlags(
                interpreter, enumType, oldValue, newValue, maskValues,
                maskOperators, cultureInfo, allowInteger, errorOnNop,
                errorOnMask, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes the combined unsigned integer value for each parameter
        /// table and stores it into the corresponding element of the
        /// parameter values array.
        /// </summary>
        /// <param name="tables">
        /// The parameter tables whose values are combined.
        /// </param>
        /// <param name="parameterValues">
        /// The array that receives the combined value for each table.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use while converting values; this parameter may
        /// be null.
        /// </param>
        /// <param name="errorOnBadValue">
        /// Non-zero to treat a value that cannot be converted as an error
        /// instead of silently skipping it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SetParameterValuesFromTablesEnum(
            ObjectDictionary[] tables,
            ulong[] parameterValues,
            CultureInfo cultureInfo,
            bool errorOnBadValue,
            ref Result error
            )
        {
            return EnumOps.SetParameterValuesFromTables(
                tables, parameterValues, cultureInfo, errorOnBadValue,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Fills the parameter tables with the enumeration names and values
        /// from the specified type, grouped according to its parameter index
        /// attributes.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type whose names and values are used.
        /// </param>
        /// <param name="tables">
        /// This parameter supplies the parameter tables on input, which may
        /// be null, and, upon success, receives the filled tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode FillTablesEnum(
            Type enumType,
            ref ObjectDictionary[] tables,
            ref Result error
            )
        {
            return EnumOps.FillTables(enumType, ref tables, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses a string of operators and names into a set of parameter
        /// tables for the specified enumerated type, grouped according to the
        /// parameter index attributes applied to the type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type to parse against.
        /// </param>
        /// <param name="value">
        /// The string of operators and names to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used while parsing; this parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case while matching names; otherwise, matching
        /// is case-sensitive.
        /// </param>
        /// <param name="errorOnEmptyList">
        /// Non-zero to treat an empty modifiers list as an error.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat a name that cannot be found as an error.
        /// </param>
        /// <param name="tables">
        /// This parameter supplies the parameter tables on input, which may
        /// be null, and, upon success, receives the resulting tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode TryParseTablesEnum(
            Interpreter interpreter,
            Type enumType,
            string value,
            CultureInfo cultureInfo,
            bool noCase,
            bool errorOnEmptyList,
            bool errorOnNotFound,
            ref ObjectDictionary[] tables,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return EnumOps.TryParseTables(
                interpreter, enumType, value, cultureInfo,
                noCase, errorOnEmptyList, errorOnNotFound,
                ref tables, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Obtains the lists of names and underlying values for the specified
        /// enumerated type.
        /// </summary>
        /// <param name="enumType">
        /// The enumerated type whose names and values are obtained.
        /// </param>
        /// <param name="enumNames">
        /// This parameter may be null on input and, upon success, receives
        /// the enumeration names.
        /// </param>
        /// <param name="enumValues">
        /// This parameter may be null on input and, upon success, receives
        /// the corresponding enumeration values.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetEnumNamesAndValues(
            Type enumType,
            ref StringList enumNames,
            ref UlongList enumValues,
            ref Result error
            )
        {
            return EnumOps.GetNamesAndValues(
                enumType, ref enumNames, ref enumValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the list of flag names that are set within the specified
        /// enumerated value, using the supplied candidate names and values.
        /// </summary>
        /// <param name="enumValue">
        /// The enumerated value to examine.
        /// </param>
        /// <param name="enumNames">
        /// The candidate enumeration names; this parameter may be null.
        /// </param>
        /// <param name="enumValues">
        /// The enumeration values corresponding to the candidate names; this
        /// parameter may be null.
        /// </param>
        /// <param name="skipEnumType">
        /// Non-zero to skip deriving the candidate names and values from the
        /// type of the value.
        /// </param>
        /// <param name="skipNameless">
        /// Non-zero to skip enumeration values that have no associated name.
        /// </param>
        /// <param name="keepZeros">
        /// Non-zero to keep names whose enumeration value is zero.
        /// </param>
        /// <param name="uniqueValues">
        /// Non-zero to require that each contributing flag value be unique.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The list of flag names that are set, or null if the operation
        /// fails.
        /// </returns>
        public static StringList ToFlagsEnumList(
            Enum enumValue,
            StringList enumNames,
            UlongList enumValues,
            bool skipEnumType,
            bool skipNameless,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            return FormatOps.FlagsEnumV2(
                enumValue, enumNames, enumValues, skipEnumType,
                skipNameless, keepZeros, uniqueValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified type is an enumerated type that
        /// has the flags attribute applied to it.
        /// </summary>
        /// <param name="enumType">
        /// The type to check; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the type is a flags enumeration; otherwise, false.
        /// </returns>
        public static bool IsFlagsEnum(Type enumType)
        {
            return EnumOps.IsFlags(enumType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Produces an English-style enumeration of the elements of the
        /// specified list, joining them with the given separator, prefix,
        /// suffix, and per-item value prefix and suffix.  Null and empty
        /// elements are skipped.
        /// </summary>
        /// <param name="list">
        /// The list of strings to format; this parameter may be null.
        /// </param>
        /// <param name="separator">
        /// The text placed between consecutive items; this parameter may be
        /// null.
        /// </param>
        /// <param name="prefix">
        /// The text placed before the list of items; this parameter may be
        /// null.
        /// </param>
        /// <param name="suffix">
        /// The text placed after the list of items; this parameter may be
        /// null.
        /// </param>
        /// <param name="valuePrefix">
        /// The text prepended to each individual item; this parameter may be
        /// null.
        /// </param>
        /// <param name="valueSuffix">
        /// The text appended to each individual item; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The resulting English-style string, or null if it is not
        /// available.
        /// </returns>
        public static string ListToEnglish(
            IList<string> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            return GenericOps<string>.ListToEnglish(
                list, separator, prefix, suffix, valuePrefix, valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Produces an English-style enumeration of the elements of the
        /// specified list, joining them with the given separator, prefix,
        /// suffix, and per-item value prefix and suffix.  Null and empty
        /// elements are skipped.
        /// </summary>
        /// <param name="list">
        /// The list of URIs to format; this parameter may be null.
        /// </param>
        /// <param name="separator">
        /// The text placed between consecutive items; this parameter may be
        /// null.
        /// </param>
        /// <param name="prefix">
        /// The text placed before the list of items; this parameter may be
        /// null.
        /// </param>
        /// <param name="suffix">
        /// The text placed after the list of items; this parameter may be
        /// null.
        /// </param>
        /// <param name="valuePrefix">
        /// The text prepended to each individual item; this parameter may be
        /// null.
        /// </param>
        /// <param name="valueSuffix">
        /// The text appended to each individual item; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The resulting English-style string, or null if it is not
        /// available.
        /// </returns>
        public static string ListToEnglish(
            IList<Uri> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            return GenericOps<Uri>.ListToEnglish(
                list, separator, prefix, suffix, valuePrefix, valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes the combined length of the string representations of the
        /// elements of the specified list, beginning at the given start index
        /// and counting only those that meet the specified minimum length.
        /// </summary>
        /// <param name="list">
        /// The list whose element string lengths are summed; this parameter
        /// may be null.
        /// </param>
        /// <param name="format">
        /// The format string used when converting each element to a string;
        /// this parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index at which to begin summing element lengths.
        /// </param>
        /// <param name="minimum">
        /// The minimum element length required for an element to be counted.
        /// </param>
        /// <returns>
        /// The combined length, or zero if the list is null.
        /// </returns>
        public static int GetTotalLength<T>(
            IList<T> list,
            string format,
            int startIndex,
            int minimum
            )
        {
            return ListOps.GetTotalLength<T>(
                list, format, startIndex, minimum);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether two collections contain equal elements in the
        /// same order, using the specified comparison callback or the
        /// elements' own equality.
        /// </summary>
        /// <param name="collection1">
        /// The first collection to compare; this parameter may be null.
        /// </param>
        /// <param name="collection2">
        /// The second collection to compare; this parameter may be null.
        /// </param>
        /// <param name="callback">
        /// The callback used to compare elements; this parameter may be null,
        /// in which case the elements' own equality is used.
        /// </param>
        /// <returns>
        /// True if both collections are null or contain equal elements in the
        /// same order; otherwise, false.
        /// </returns>
        public static bool IEnumerableEquals<T>(
            IEnumerable<T> collection1,
            IEnumerable<T> collection2,
            CompareCallback<T> callback
            )
        {
            return ListOps.IEnumerableEquals<T>(
                collection1, collection2, callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes an order-independent hash code for the specified
        /// collection by combining the hash codes of its elements.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are hashed; this parameter may be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The callback used to obtain each element's hash code; this
        /// parameter may be null, in which case the elements' own hash code
        /// is used.
        /// </param>
        /// <returns>
        /// The combined hash code, or zero if the collection is null.
        /// </returns>
        public static int IEnumerableHashCode<T>(
            IEnumerable<T> collection,
            GetHashCodeCallback<T> callback
            )
        {
            return ListOps.IEnumerableHashCode<T>(collection, callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified object appears to have been
        /// disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="object">
        /// The object to check; this parameter may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the check even when it would otherwise be
        /// skipped.
        /// </param>
        /// <param name="cannotCheck">
        /// The value returned when the disposal state cannot be determined;
        /// this parameter may be null.
        /// </param>
        /// <param name="caughtException">
        /// The value returned when the check throws an object-disposed
        /// exception; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the object appears disposed, false if it does not, or null
        /// when the state cannot be determined.
        /// </returns>
        public static bool? IsDisposed(
            Interpreter interpreter,
            object @object,
            bool force,
            bool? cannotCheck,
            bool? caughtException
            )
        {
            return ObjectOps.IsDisposed(
                interpreter, @object, force, cannotCheck, caughtException);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to dispose the specified object, skipping objects that
        /// are already disposed or not disposable, and emits a diagnostic
        /// trace message if disposal fails.
        /// </summary>
        /// <param name="object">
        /// This parameter supplies the object on input, which may be null,
        /// and, upon return, receives its default value.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode TryDisposeObjectOrTrace<T>(
            ref T @object
            )
        {
            return ObjectOps.TryDisposeOrTrace<T>(ref @object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to dispose the specified object and reports a complaint
        /// through the interpreter if disposal fails.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="object">
        /// This parameter supplies the object on input, which may be null,
        /// and, upon return, receives its default value.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode TryDisposeObjectOrComplain<T>(
            Interpreter interpreter,
            ref T @object
            )
        {
            return ObjectOps.TryDisposeOrComplain<T>(
                interpreter, ref @object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to dispose the specified object using the default
        /// disposal behavior.
        /// </summary>
        /// <param name="object">
        /// This parameter supplies the object on input, which may be null,
        /// and, upon return, receives its default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode TryDisposeObject<T>(
            ref T @object,
            ref Result error
            )
        {
            return ObjectOps.TryDispose<T>(ref @object, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds an identifier string from the specified prefix and integer
        /// identifier.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to include; this parameter may be null.
        /// </param>
        /// <param name="id">
        /// The integer identifier to include; a value of zero is omitted.
        /// </param>
        /// <returns>
        /// The resulting identifier string, or null if it is not available.
        /// </returns>
        public static string MakeStringId(
            string prefix,
            long id
            )
        {
            return FormatOps.Id(prefix, null, id);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether two file names are equal, after converting each
        /// one to its native form, using filesystem-appropriate string
        /// comparison.
        /// </summary>
        /// <param name="path1">
        /// The first path to compare; this parameter may be null.
        /// </param>
        /// <param name="path2">
        /// The second path to compare; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the file names are equal; otherwise, false.
        /// </returns>
        public static bool IsEqualFileName(
            string path1,
            string path2
            )
        {
            return PathOps.IsEqualFileName(path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the drive-letter prefix from the specified path, producing
        /// a relative path.
        /// </summary>
        /// <param name="path">
        /// The path to convert; this parameter may be null.
        /// </param>
        /// <param name="separator">
        /// Non-zero to also remove the leading directory separator.
        /// </param>
        /// <returns>
        /// The relative path, or the original path if it is null, empty, too
        /// short, or does not begin with a drive-letter prefix.
        /// </returns>
        public static string MakeRelativePath(
            string path,
            bool separator
            )
        {
            return PathOps.MakeRelativePath(path, separator);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses the specified text as a hexavigesimal (base-26) wide
        /// integer value.
        /// </summary>
        /// <param name="text">
        /// The text to parse; this parameter may be null or empty.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter receives the parsed wide integer
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation; success requires that the entire text be a valid
        /// hexavigesimal integer.
        /// </returns>
        public static ReturnCode ParseHexavigesimal(
            string text,
            ref long value,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(text) &&
                (Parser.ParseHexavigesimal(text, 0, text.Length,
                    ref value) == text.Length))
            {
                return ReturnCode.Ok;
            }

            error = String.Format(
                "expected hexavigesimal wide integer but got \"{0}\"",
                text);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified unsigned integer value as a hexavigesimal
        /// (base-26) string, padded to a minimum width.
        /// </summary>
        /// <param name="value">
        /// The value to format.
        /// </param>
        /// <param name="width">
        /// The minimum width of the result; a shorter result is padded.
        /// </param>
        /// <returns>
        /// The resulting hexavigesimal string, or null if it is not
        /// available.
        /// </returns>
        public static string FormatHexavigesimal(
            ulong value,
            byte width
            )
        {
            return FormatOps.Hexavigesimal(value, width);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified text appears to be a base-26
        /// encoded value.
        /// </summary>
        /// <param name="text">
        /// The text to examine; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the text appears to be base-26 encoded; otherwise, false.
        /// </returns>
        public static bool IsBase26(
            string text
            )
        {
            return StringOps.IsBase26(text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decodes the specified hexavigesimal (base-26) string into an array
        /// of bytes, ignoring any white-space characters.
        /// </summary>
        /// <param name="value">
        /// The hexavigesimal (base-26) string to decode; this parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The resulting array of bytes, or null if the string is null,
        /// empty, or cannot be decoded.
        /// </returns>
        public static byte[] FromBase26String(
            string value
            )
        {
            return StringOps.FromBase26String(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Encodes the specified array of bytes into a hexavigesimal
        /// (base-26) string, optionally inserting line breaks and spaces.
        /// </summary>
        /// <param name="array">
        /// The array of bytes to encode; this parameter may be null.
        /// </param>
        /// <param name="options">
        /// The formatting options controlling the insertion of line breaks
        /// and spaces.
        /// </param>
        /// <returns>
        /// The resulting hexavigesimal string, or null if the array is null.
        /// </returns>
        public static string ToBase26String(
            byte[] array,
            Base26FormattingOption options
            )
        {
            return StringOps.ToBase26String(array, options);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified text appears to be a base-64
        /// encoded value.
        /// </summary>
        /// <param name="text">
        /// The text to examine; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the text appears to be base-64 encoded; otherwise, false.
        /// </returns>
        public static bool IsBase64(
            string text
            )
        {
            return StringOps.IsBase64(text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the qualified name of the method that backs the specified
        /// delegate.
        /// </summary>
        /// <param name="delegate">
        /// The delegate whose backing method name is formatted; this
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// Non-zero to include the containing assembly information.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display-friendly placeholder when the
        /// delegate is null.
        /// </param>
        /// <returns>
        /// The resulting method name, or null if the delegate is null and
        /// <paramref name="display" /> is zero.
        /// </returns>
        public static string FormatDelegateMethodName(
            Delegate @delegate,
            bool assembly,
            bool display
            )
        {
            return FormatOps.DelegateMethodName(@delegate, assembly, display);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the directory name used for a package of the specified
        /// name and version.
        /// </summary>
        /// <param name="name">
        /// The package name.
        /// </param>
        /// <param name="version">
        /// The package version.
        /// </param>
        /// <param name="full">
        /// Non-zero to prepend the standard library directory prefix.
        /// </param>
        /// <returns>
        /// The resulting directory name string, or null if it is not
        /// available.
        /// </returns>
        public static string FormatPackageDirectory(
            string name,
            Version version,
            bool full
            )
        {
            return FormatOps.PackageDirectory(name, version, full);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds an assembly-qualified plugin name from the specified
        /// assembly name and type name.
        /// </summary>
        /// <param name="assemblyName">
        /// The name of the assembly containing the plugin; this parameter
        /// may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type; this parameter may be null.
        /// </param>
        /// <returns>
        /// The resulting assembly-qualified plugin name, or null if it is not
        /// available.
        /// </returns>
        public static string FormatPluginName(
            string assemblyName,
            string typeName
            )
        {
            return FormatOps.PluginName(assemblyName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Produces a human-readable "about" description for the specified
        /// plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to describe; this parameter may be null.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the full type name; otherwise, the simple type
        /// name is used.
        /// </param>
        /// <returns>
        /// The resulting "about" description, or null if the plugin data is
        /// null.
        /// </returns>
        public static string FormatPluginAbout(
            IPluginData pluginData,
            bool full
            )
        {
            return FormatOps.PluginAbout(pluginData, full, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Produces a human-readable "about" description for the specified
        /// plugin, appending the specified extra text.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to describe; this parameter may be null.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the full type name; otherwise, the simple type
        /// name is used.
        /// </param>
        /// <param name="extra">
        /// The extra text to append to the description; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The resulting "about" description, or null if the plugin data is
        /// null.
        /// </returns>
        public static string FormatPluginAbout(
            IPluginData pluginData,
            bool full,
            string extra
            )
        {
            return FormatOps.PluginAbout(pluginData, full, extra);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds an identifier string from the specified prefix, name, and
        /// integer identifier.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to include; this parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to include; this parameter may be null.
        /// </param>
        /// <param name="id">
        /// The integer identifier to include; a value of zero is omitted.
        /// </param>
        /// <returns>
        /// The resulting identifier string, or null if it is not available.
        /// </returns>
        public static string FormatId(
            string prefix,
            string name,
            long id
            )
        {
            return FormatOps.Id(prefix, name, id);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Combines the specified results into a single result list, skipping
        /// any that are null.
        /// </summary>
        /// <param name="results">
        /// The results to combine; this parameter may be null.
        /// </param>
        /// <returns>
        /// A result containing the non-null results, or null if the supplied
        /// array is null.
        /// </returns>
        public static Result MaybeCombineResults(
            params Result[] results
            )
        {
            return ResultOps.MaybeCombine(results);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified return code and result value into a single
        /// string.
        /// </summary>
        /// <param name="code">
        /// The return code to format.
        /// </param>
        /// <param name="result">
        /// The result value to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted string, or null if it is not available.
        /// </returns>
        public static string FormatResult(
            ReturnCode code,
            Result result
            )
        {
            return ResultOps.Format(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified return code, result value, and error line
        /// number into a single string.
        /// </summary>
        /// <param name="code">
        /// The return code to format.
        /// </param>
        /// <param name="result">
        /// The result value to format; this parameter may be null.
        /// </param>
        /// <param name="errorLine">
        /// The error line number to format, or zero if none.
        /// </param>
        /// <returns>
        /// The formatted string, or null if it is not available.
        /// </returns>
        public static string FormatResult(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            return ResultOps.Format(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified return code represents success.
        /// </summary>
        /// <param name="code">
        /// The return code to check.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero to treat any code other than an error code as success;
        /// zero to treat only the success code as success.
        /// </param>
        /// <returns>
        /// True if the return code represents success; otherwise, false.
        /// </returns>
        public static bool IsSuccess(
            ReturnCode code,
            bool exceptions
            )
        {
            return ResultOps.IsSuccess(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a custom success return code by combining the specified
        /// value with the custom-success flag.
        /// </summary>
        /// <param name="value">
        /// The custom value to combine with the success flag.
        /// </param>
        /// <returns>
        /// The resulting custom success return code.
        /// </returns>
        public static ReturnCode CustomOkCode(
            uint value
            )
        {
            return ResultOps.CustomOkCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a custom failure return code by combining the specified
        /// value with the custom-failure flag.
        /// </summary>
        /// <param name="value">
        /// The custom value to combine with the failure flag.
        /// </param>
        /// <returns>
        /// The resulting custom failure return code.
        /// </returns>
        public static ReturnCode CustomErrorCode(
            uint value
            )
        {
            return ResultOps.CustomErrorCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the exit code that represents successful completion.
        /// </summary>
        /// <returns>
        /// The success exit code.
        /// </returns>
        public static ExitCode SuccessExitCode()
        {
            return ResultOps.SuccessExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the exit code that represents failure.
        /// </summary>
        /// <returns>
        /// The failure exit code.
        /// </returns>
        public static ExitCode FailureExitCode()
        {
            return ResultOps.FailureExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the exit code that represents termination due to an
        /// exception.
        /// </summary>
        /// <returns>
        /// The exception exit code.
        /// </returns>
        public static ExitCode ExceptionExitCode()
        {
            return ResultOps.ExceptionExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Translates the specified exit code into the corresponding return
        /// code.
        /// </summary>
        /// <param name="exitCode">
        /// The exit code to translate.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the exit code represents success;
        /// otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExitCodeToReturnCode(
            ExitCode exitCode
            )
        {
            return ResultOps.ExitCodeToReturnCode(exitCode);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Translates the specified return code into the corresponding exit
        /// code.
        /// </summary>
        /// <param name="code">
        /// The return code to translate.
        /// </param>
        /// <returns>
        /// The success exit code if the return code represents success;
        /// otherwise, the failure exit code.
        /// </returns>
        public static ExitCode ReturnCodeToExitCode(
            ReturnCode code
            )
        {
            return ResultOps.ReturnCodeToExitCode(code, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Translates the specified return code into the corresponding exit
        /// code.
        /// </summary>
        /// <param name="code">
        /// The return code to translate.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero to use exception-aware success semantics when determining
        /// whether the return code represents success.
        /// </param>
        /// <returns>
        /// The success exit code if the return code represents success;
        /// otherwise, the failure exit code.
        /// </returns>
        public static ExitCode ReturnCodeToExitCode(
            ReturnCode code,
            bool exceptions
            )
        {
            return ResultOps.ReturnCodeToExitCode(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reverses the byte order (endianness) of the specified unsigned
        /// integer value.
        /// </summary>
        /// <param name="X">
        /// The value whose byte order is reversed.
        /// </param>
        /// <returns>
        /// The value with its bytes in reversed order.
        /// </returns>
        public static uint FlipEndian(
            uint X
            )
        {
            return ConversionOps.FlipEndian(X);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reverses the byte order (endianness) of the specified unsigned
        /// long integer value.
        /// </summary>
        /// <param name="X">
        /// The value whose byte order is reversed.
        /// </param>
        /// <returns>
        /// The value with its bytes in reversed order.
        /// </returns>
        public static ulong FlipEndian(
            ulong X
            )
        {
            return ConversionOps.FlipEndian(X);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified string value into an array of bytes,
        /// automatically detecting the encoding format (delimited
        /// hexadecimal bytes, base-16, base-64, or a GUID).
        /// </summary>
        /// <param name="value">
        /// The string value to convert; this parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used while parsing; this parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// This parameter supplies the byte array on input and, upon success,
        /// receives the converted bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetBytesFromString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            return StringOps.GetBytesFromString(
                value, cultureInfo, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Encodes the specified string value into an array of bytes using
        /// the specified encoding, or the encoding associated with the
        /// specified encoding type.
        /// </summary>
        /// <param name="encoding">
        /// The character encoding to use; this parameter may be null, in
        /// which case the encoding associated with the type is used.
        /// </param>
        /// <param name="value">
        /// The string value to encode; this parameter may be null when
        /// <paramref name="errorOnNull" /> is zero.
        /// </param>
        /// <param name="type">
        /// The encoding type used when no explicit encoding is supplied.
        /// </param>
        /// <param name="errorOnNull">
        /// Non-zero to treat a null value as an error.
        /// </param>
        /// <param name="bytes">
        /// This parameter supplies the byte array on input and, upon success,
        /// receives the resulting bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetBytes(
            Encoding encoding,
            string value,
            EncodingType type,
            bool errorOnNull,
            ref byte[] bytes,
            ref Result error
            )
        {
            return StringOps.GetBytes(
                encoding, value, type, errorOnNull, ref bytes,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a list containing the unique, non-empty elements of the
        /// specified list, preserving their original order.
        /// </summary>
        /// <param name="list">
        /// The list to filter; this parameter may be null.
        /// </param>
        /// <returns>
        /// A new list of unique elements, or the original list if it is null
        /// or empty.
        /// </returns>
        public static StringList GetUniqueElements(
            StringList list
            )
        {
            return ListOps.GetUniqueElements(list);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a new list containing the unique elements of the specified
        /// list, using the supplied callback to decide which elements are
        /// treated as duplicates.
        /// </summary>
        /// <param name="list">
        /// The list whose unique elements are gathered; this parameter may be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The callback consulted for each element to decide whether it is a
        /// duplicate; this parameter may be null, in which case elements that
        /// are null, empty, or already seen are skipped.
        /// </param>
        /// <returns>
        /// A new list of the unique elements, or the original list when it is
        /// null, empty, or no callback is supplied.
        /// </returns>
        public static StringList GetUniqueElements(
            StringList list,
            UniqueStringCallback<string> callback
            )
        {
            return ListOps.GetUniqueElements(list, callback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the specified byte array into its hexadecimal string
        /// representation, using lowercase digits.
        /// </summary>
        /// <param name="array">
        /// The bytes to convert; this parameter may be null.
        /// </param>
        /// <returns>
        /// The hexadecimal string representation of the bytes, or null when
        /// the array is null.
        /// </returns>
        public static string ToHexadecimalString(
            byte[] array
            )
        {
            return ArrayOps.ToHexadecimalString(array);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        /// <summary>
        /// Serializes the specified object, treated as the specified type, as
        /// XML to the specified writer.
        /// </summary>
        /// <param name="object">
        /// The object to serialize; this parameter cannot be null.
        /// </param>
        /// <param name="type">
        /// The type the object is serialized as; this parameter cannot be
        /// null.
        /// </param>
        /// <param name="writer">
        /// The XML writer that receives the serialized output; this parameter
        /// cannot be null.
        /// </param>
        /// <param name="serializerNamespaces">
        /// The XML namespaces to emit during serialization; this parameter may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode Serialize(
            object @object,
            Type type,
            XmlWriter writer,
            XmlSerializerNamespaces serializerNamespaces,
            ref Result error
            )
        {
            return XmlOps.Serialize(
                @object, type, writer, serializerNamespaces, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deserializes an object of the specified type from the specified XML
        /// reader.
        /// </summary>
        /// <param name="type">
        /// The type of object to deserialize; this parameter cannot be null.
        /// </param>
        /// <param name="reader">
        /// The XML reader supplying the serialized data; this parameter cannot
        /// be null.
        /// </param>
        /// <param name="object">
        /// This parameter must be null on input; upon success, it receives the
        /// deserialized object.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode Deserialize(
            Type type,
            XmlReader reader,
            ref object @object,
            ref Result error
            )
        {
            return XmlOps.Deserialize(type, reader, ref @object, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// Validates the specified XML document against the schema loaded from
        /// the named manifest resource of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly containing the schema resource; this parameter may be
        /// null to use the default assembly.
        /// </param>
        /// <param name="resourceName">
        /// The name of the manifest resource containing the schema; this
        /// parameter may be null to use the default schema resource name.
        /// </param>
        /// <param name="document">
        /// The XML document to validate; this parameter cannot be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode Validate(
            Assembly assembly,
            string resourceName,
            XmlDocument document,
            ref Result error
            )
        {
            return XmlOps.Validate(
                assembly, resourceName, document, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Selects the object option type to use for an object invocation,
        /// based on whether raw and/or all-overload semantics are requested.
        /// </summary>
        /// <param name="raw">
        /// Non-zero to select the "raw" invocation option type; otherwise,
        /// zero.
        /// </param>
        /// <param name="all">
        /// Non-zero to select the "all overloads" invocation option type,
        /// which takes precedence over the raw option type; otherwise, zero.
        /// </param>
        /// <returns>
        /// The selected object option type.
        /// </returns>
        public static ObjectOptionType GetOptionType(
            bool raw,
            bool all
            )
        {
            return ObjectOps.GetOptionType(raw, all);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Returns the option dictionary appropriate for the specified
        /// invoke-related object option type, for use when fixing up return
        /// values and by-reference arguments.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type whose invoke-related options are requested.
        /// </param>
        /// <returns>
        /// The option dictionary for the requested invoke option type, or null
        /// if the option type does not denote a single invoke option type.
        /// </returns>
        public static OptionDictionary GetInvokeOptions(
            ObjectOptionType objectOptionType
            )
        {
            return ObjectOps.GetInvokeOptions(objectOptionType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Loads an assembly from the specified stream of raw assembly bytes.
        /// </summary>
        /// <param name="stream">
        /// The readable, seekable stream containing the assembly image; this
        /// parameter cannot be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The loaded assembly, or null on failure.
        /// </returns>
        public static Assembly LoadAssemblyFromStream(
            Stream stream,
            ref Result error
            )
        {
            return AssemblyOps.LoadFromStream(stream, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Verifies that the specified assembly file exists, is trusted, and,
        /// when a public key token is supplied, carries a matching strong-name
        /// public key token.
        /// </summary>
        /// <param name="fileName">
        /// The name of the assembly file to verify; this parameter cannot be
        /// null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected strong-name public key token; this parameter may be
        /// null to skip the public key token check.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode VerifyAssemblyFromFile(
            string fileName,
            byte[] publicKeyToken,
            IClientData clientData,
            ref Result error
            )
        {
            return AssemblyOps.VerifyFromFile(
                fileName, publicKeyToken, clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an interpreter suitable for use with the settings
        /// subsystem, configured according to the specified script data flags.
        /// </summary>
        /// <param name="interpreter">
        /// The parent interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The script data flags that control how the interpreter is created.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result; upon failure, it
        /// receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The newly created interpreter, or null on failure.
        /// </returns>
        public static Interpreter CreateInterpreterForSettings(
            Interpreter interpreter,
            IClientData clientData,
            ScriptDataFlags flags,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.CreateInterpreterForSettings(
                interpreter, clientData, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the cached interpreter used by the settings subsystem, if
        /// any.
        /// </summary>
        public static void ClearInterpreterForSettings()
        {
            ScriptOps.ClearInterpreterCache();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified interpreter is currently
        /// evaluating one or more package index files.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter is evaluating a package index file;
        /// otherwise, false.
        /// </returns>
        public static bool IsScriptFileForPackageIndexPending(
            Interpreter interpreter
            )
        {
            return ScriptOps.IsFileForPackageIndexPending(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified interpreter is currently
        /// evaluating one or more settings files.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter is evaluating a settings file; otherwise,
        /// false.
        /// </returns>
        public static bool IsScriptFileForSettingsPending(
            Interpreter interpreter
            )
        {
            return ScriptOps.IsFileForSettingsPending(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Loads settings by evaluating the specified script file, optionally
        /// using a newly created, cached, or isolated interpreter, and
        /// extracts the resulting settings from its global call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to create or host the evaluating
        /// interpreter; this parameter may be null.
        /// </param>
        /// <param name="pushClientData">
        /// The caller-specific data associated with pushing the evaluation
        /// context; this parameter may be null.
        /// </param>
        /// <param name="callbackClientData">
        /// The caller-specific data passed to any callbacks; this parameter
        /// may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the settings script file to evaluate.
        /// </param>
        /// <param name="flags">
        /// On input, this parameter supplies the script data flags that
        /// control interpreter creation and settings extraction; upon success,
        /// it receives the updated flags.
        /// </param>
        /// <param name="settings">
        /// On input, this parameter optionally supplies an existing settings
        /// dictionary; upon success, it receives the loaded settings.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode LoadSettingsViaScriptFile(
            Interpreter interpreter,
            IClientData pushClientData,
            IClientData callbackClientData,
            string fileName,
            ref ScriptDataFlags flags,
            ref _StringDictionary settings,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.LoadSettingsViaFile(
                interpreter, pushClientData, callbackClientData,
                fileName, ref flags, ref settings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes the specified text to a newly created temporary script file,
        /// optionally using the specified character encoding; the file is
        /// removed if creation fails.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text to write to the temporary file.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when writing the text; this parameter
        /// may be null.
        /// </param>
        /// <param name="fileName">
        /// On input, this parameter may supply a preferred file name; upon
        /// success, it receives the name of the created temporary file.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CreateTemporaryScriptFile(
            Interpreter interpreter,
            string text,
            Encoding encoding,
            ref string fileName,
            ref Result error
            )
        {
            return ScriptOps.CreateTemporaryFile(
                interpreter, text, encoding, ref fileName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to convert the specified string into a value of the
        /// specified target type, honoring the supplied value and date/time
        /// parsing options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="type">
        /// The target type to convert the string into.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="valueFlags">
        /// The flags controlling how the value is parsed; this parameter may
        /// be null.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format string used when parsing date and time values; this
        /// parameter may be null.
        /// </param>
        /// <param name="dateTimeKind">
        /// The kind assumed when parsing date and time values.
        /// </param>
        /// <param name="dateTimeStyles">
        /// The styles applied when parsing date and time values.
        /// </param>
        /// <param name="value">
        /// On input, this parameter is ignored; upon success, it receives the
        /// converted value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode TryGetValueOfType(
            Interpreter interpreter,
            IClientData clientData,
            Type type,
            string text,
            ValueFlags? valueFlags,
            string dateTimeFormat,
            DateTimeKind dateTimeKind,
            DateTimeStyles dateTimeStyles,
            ref object value,
            ref Result error
            )
        {
            return MarshalOps.TryGetValueOfType(
                interpreter, clientData, type, text, valueFlags,
                dateTimeFormat, dateTimeKind, dateTimeStyles,
                ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Extracts the reflection-related member types, binding flags, and
        /// value flags from the specified options, falling back to the
        /// supplied defaults wherever an option is absent.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain reflection-related options;
        /// this parameter may be null.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type identifying the sub-command being processed.
        /// </param>
        /// <param name="defaultMemberTypes">
        /// The member types to use when none are present in the options; this
        /// parameter may be null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The binding flags to use when none are present in the options; this
        /// parameter may be null to use the built-in default.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The object value flags to use when none are present in the options;
        /// this parameter may be null to use the built-in default.
        /// </param>
        /// <param name="defaultMemberValueFlags">
        /// The member value flags to use when none are present in the options;
        /// this parameter may be null to use the built-in default.
        /// </param>
        /// <param name="memberTypes">
        /// Upon return, this parameter receives the resolved member types.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, this parameter receives the resolved binding flags.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, this parameter receives the resolved object value
        /// flags.
        /// </param>
        /// <param name="memberValueFlags">
        /// Upon return, this parameter receives the resolved member value
        /// flags.
        /// </param>
        public static void ProcessReflectionOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags
            )
        {
            ObjectOps.ProcessReflectionOptions(
                options, objectOptionType, defaultMemberTypes,
                defaultBindingFlags, defaultObjectValueFlags,
                defaultMemberValueFlags, out memberTypes,
                out bindingFlags, out objectValueFlags,
                out memberValueFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Extracts the object flags, object name, interpreter name, and
        /// alias-related settings used when fixing up an object return value
        /// from the specified options.
        /// </summary>
        /// <param name="options">
        /// The option dictionary to read the fixup options from; this
        /// parameter may be null.
        /// </param>
        /// <param name="defaultObjectFlags">
        /// The object flags to use when none are present in the options; this
        /// parameter may be null to use the built-in default.
        /// </param>
        /// <param name="objectFlags">
        /// Upon return, this parameter receives the resolved object flags.
        /// </param>
        /// <param name="objectName">
        /// Upon return, this parameter receives the requested object handle
        /// name.
        /// </param>
        /// <param name="interpName">
        /// Upon return, this parameter receives the target interpreter name.
        /// </param>
        /// <param name="alias">
        /// Upon return, this parameter indicates whether a command alias
        /// should be created.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, this parameter indicates whether the alias should use
        /// "raw" semantics.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, this parameter indicates whether the alias should
        /// cover all overloads.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, this parameter indicates whether the alias should add
        /// a reference to the object.
        /// </param>
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, out objectFlags,
                out objectName, out interpName, out alias,
                out aliasRaw, out aliasAll, out aliasReference);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Extracts the return type, object flags, object name, interpreter
        /// name, and the create, dispose, alias, and to-string settings used
        /// when fixing up an object return value from the specified options.
        /// </summary>
        /// <param name="options">
        /// The option dictionary to read the fixup options from; this
        /// parameter may be null.
        /// </param>
        /// <param name="defaultObjectFlags">
        /// The object flags to use when none are present in the options; this
        /// parameter may be null to use the built-in default.
        /// </param>
        /// <param name="returnType">
        /// Upon return, this parameter receives the resolved return type.
        /// </param>
        /// <param name="objectFlags">
        /// Upon return, this parameter receives the resolved object flags.
        /// </param>
        /// <param name="objectName">
        /// Upon return, this parameter receives the requested object handle
        /// name.
        /// </param>
        /// <param name="interpName">
        /// Upon return, this parameter receives the target interpreter name.
        /// </param>
        /// <param name="create">
        /// Upon return, this parameter indicates whether an opaque object
        /// handle should be created.
        /// </param>
        /// <param name="dispose">
        /// Upon return, this parameter indicates whether the object may be
        /// disposed when it cannot be added to the interpreter.
        /// </param>
        /// <param name="alias">
        /// Upon return, this parameter indicates whether a command alias
        /// should be created.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, this parameter indicates whether the alias should use
        /// "raw" semantics.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, this parameter indicates whether the alias should
        /// cover all overloads.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, this parameter indicates whether the alias should add
        /// a reference to the object.
        /// </param>
        /// <param name="toString">
        /// Upon return, this parameter indicates whether the string
        /// representation of the value should be returned instead of an opaque
        /// object handle.
        /// </param>
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, out returnType,
                out objectFlags, out objectName, out interpName,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Extracts the return type, object flags, by-reference object flags,
        /// object name, interpreter name, and the create, dispose, alias, and
        /// to-string settings used when fixing up an object return value from
        /// the specified options.
        /// </summary>
        /// <param name="options">
        /// The option dictionary to read the fixup options from; this
        /// parameter may be null.
        /// </param>
        /// <param name="defaultObjectFlags">
        /// The object flags to use when none are present in the options; this
        /// parameter may be null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefObjectFlags">
        /// The object flags to use for by-reference values when none are
        /// present in the options; this parameter may be null to use the
        /// built-in default.
        /// </param>
        /// <param name="returnType">
        /// Upon return, this parameter receives the resolved return type.
        /// </param>
        /// <param name="objectFlags">
        /// Upon return, this parameter receives the resolved object flags.
        /// </param>
        /// <param name="byRefObjectFlags">
        /// Upon return, this parameter receives the resolved by-reference
        /// object flags.
        /// </param>
        /// <param name="objectName">
        /// Upon return, this parameter receives the requested object handle
        /// name.
        /// </param>
        /// <param name="interpName">
        /// Upon return, this parameter receives the target interpreter name.
        /// </param>
        /// <param name="create">
        /// Upon return, this parameter indicates whether an opaque object
        /// handle should be created.
        /// </param>
        /// <param name="dispose">
        /// Upon return, this parameter indicates whether the object may be
        /// disposed when it cannot be added to the interpreter.
        /// </param>
        /// <param name="alias">
        /// Upon return, this parameter indicates whether a command alias
        /// should be created.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, this parameter indicates whether the alias should use
        /// "raw" semantics.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, this parameter indicates whether the alias should
        /// cover all overloads.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, this parameter indicates whether the alias should add
        /// a reference to the object.
        /// </param>
        /// <param name="toString">
        /// Upon return, this parameter indicates whether the string
        /// representation of the value should be returned instead of an opaque
        /// object handle.
        /// </param>
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            ObjectFlags? defaultByRefObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out ObjectFlags byRefObjectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, defaultByRefObjectFlags,
                out returnType, out objectFlags, out byRefObjectFlags,
                out objectName, out interpName, out create, out dispose,
                out alias, out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Returns the default reflection binding flags used by the object
        /// sub-command options.
        /// </summary>
        /// <returns>
        /// The default binding flags.
        /// </returns>
        public static BindingFlags GetDefaultBindingFlags()
        {
            return ObjectOps.GetDefaultBindingFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Returns the option dictionary that defines the options accepted
        /// when fixing up an object return value.
        /// </summary>
        /// <returns>
        /// The option dictionary for the object fixup-return-value command
        /// options.
        /// </returns>
        public static OptionDictionary GetFixupReturnValueOptions()
        {
            return CommandOptions.GetCommandOptions(
                CommandOptionType.Object_FixupReturnValue);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Translates the specified object value into an interpreter result,
        /// optionally creating an opaque object handle and an associated
        /// command alias.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type associated with the opaque object handle; this parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The object flags used when creating the opaque object handle.
        /// </param>
        /// <param name="options">
        /// The options currently in effect for the calling command; this
        /// parameter may be null.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that selects the appropriate set of options.
        /// </param>
        /// <param name="objectName">
        /// The requested name for the opaque object handle; this parameter may
        /// be null to generate one automatically.
        /// </param>
        /// <param name="value">
        /// The object value to translate into a result; this parameter may be
        /// null.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the object; otherwise, zero.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero for the created alias to add a reference to the object;
        /// otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result; upon failure, it
        /// receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        [Obsolete()]
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            Type type,
            ObjectFlags flags,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            string objectName,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, type, flags, null, options,
                objectOptionType, objectName, value, true,
                alias, aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Translates the specified object value into an interpreter result,
        /// optionally creating an opaque object handle and an associated
        /// command alias with its own options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type associated with the opaque object handle; this parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The object flags used when creating the opaque object handle.
        /// </param>
        /// <param name="currentOptions">
        /// The options currently in effect for the calling command; this
        /// parameter may be null.
        /// </param>
        /// <param name="aliasOptions">
        /// The options to associate with any command alias that is created;
        /// this parameter may be null.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that selects the appropriate set of options.
        /// </param>
        /// <param name="objectName">
        /// The requested name for the opaque object handle; this parameter may
        /// be null to generate one automatically.
        /// </param>
        /// <param name="value">
        /// The object value to translate into a result; this parameter may be
        /// null.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the object; otherwise, zero.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero for the created alias to add a reference to the object;
        /// otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result; upon failure, it
        /// receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            Type type,
            ObjectFlags flags,
            OptionDictionary currentOptions,
            OptionDictionary aliasOptions,
            ObjectOptionType objectOptionType,
            string objectName,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, type, flags, currentOptions, aliasOptions,
                objectOptionType, objectName, value, true,
                alias, aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Translates the specified object value into an interpreter result,
        /// possibly creating an opaque object handle, a command alias, and a
        /// bridged command, using the specified binder and culture for value
        /// conversion.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="binder">
        /// The binder used to convert values to and from their string
        /// representations.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion; this parameter may be
        /// null.
        /// </param>
        /// <param name="type">
        /// The type associated with the opaque object handle; this parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The object flags used when creating the opaque object handle.
        /// </param>
        /// <param name="options">
        /// The options currently in effect for the calling command; this
        /// parameter may be null.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that selects the appropriate set of options.
        /// </param>
        /// <param name="objectName">
        /// The requested name for the opaque object handle; this parameter may
        /// be null to generate one automatically.
        /// </param>
        /// <param name="interpName">
        /// The name of the target interpreter for a bridged command; this
        /// parameter may be null to skip creating one.
        /// </param>
        /// <param name="value">
        /// The object value to translate into a result; this parameter may be
        /// null.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an opaque object handle; otherwise, zero.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to permit disposing the object when it cannot be added to
        /// the interpreter; otherwise, zero.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the object; otherwise, zero.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero for the created alias to add a reference to the object;
        /// otherwise, zero.
        /// </param>
        /// <param name="toString">
        /// Non-zero to return the string representation of the value instead
        /// of an opaque object handle; otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result; upon failure, it
        /// receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        [Obsolete()]
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            IBinder binder,
            CultureInfo cultureInfo,
            Type type,
            ObjectFlags flags,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            string objectName,
            string interpName,
            object value,
            bool create,
            bool dispose,
            bool alias,
            bool aliasReference,
            bool toString,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, binder, cultureInfo, type, flags,
                null, options, objectOptionType,
                objectName, interpName, value, create, dispose,
                alias, aliasReference, toString, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        /// <summary>
        /// Translates the specified object value into an interpreter result,
        /// possibly creating an opaque object handle, a command alias with its
        /// own options, and a bridged command, using the specified binder and
        /// culture for value conversion.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="binder">
        /// The binder used to convert values to and from their string
        /// representations.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion; this parameter may be
        /// null.
        /// </param>
        /// <param name="type">
        /// The type associated with the opaque object handle; this parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The object flags used when creating the opaque object handle.
        /// </param>
        /// <param name="currentOptions">
        /// The options currently in effect for the calling command; this
        /// parameter may be null.
        /// </param>
        /// <param name="aliasOptions">
        /// The options to associate with any command alias that is created;
        /// this parameter may be null.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that selects the appropriate set of options.
        /// </param>
        /// <param name="objectName">
        /// The requested name for the opaque object handle; this parameter may
        /// be null to generate one automatically.
        /// </param>
        /// <param name="interpName">
        /// The name of the target interpreter for a bridged command; this
        /// parameter may be null to skip creating one.
        /// </param>
        /// <param name="value">
        /// The object value to translate into a result; this parameter may be
        /// null.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an opaque object handle; otherwise, zero.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to permit disposing the object when it cannot be added to
        /// the interpreter; otherwise, zero.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the object; otherwise, zero.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero for the created alias to add a reference to the object;
        /// otherwise, zero.
        /// </param>
        /// <param name="toString">
        /// Non-zero to return the string representation of the value instead
        /// of an opaque object handle; otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result; upon failure, it
        /// receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            IBinder binder,
            CultureInfo cultureInfo,
            Type type,
            ObjectFlags flags,
            OptionDictionary currentOptions,
            OptionDictionary aliasOptions,
            ObjectOptionType objectOptionType,
            string objectName,
            string interpName,
            object value,
            bool create,
            bool dispose,
            bool alias,
            bool aliasReference,
            bool toString,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, binder, cultureInfo, type, flags,
                currentOptions, aliasOptions, objectOptionType,
                objectName, interpName, value, create, dispose,
                alias, aliasReference, toString, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Throws a script exception indicating that the specified feature is
        /// not supported by the specified plugin, subject to the active
        /// interpreter (or global) configuration.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin that does not support the feature; this parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The name of the unsupported feature; this parameter may be null.
        /// </param>
        public static void ThrowFeatureNotSupported(
            IPluginData pluginData,
            string name
            )
        {
            _RuntimeOps.ThrowFeatureNotSupported(pluginData, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs a policy check that approves or denies a command based on
        /// whether its sub-command appears in the specified set of allowed (or
        /// disallowed) sub-command names.
        /// </summary>
        /// <param name="flags">
        /// The policy flags that control the behavior of this check.
        /// </param>
        /// <param name="commandType">
        /// The type of the command being checked.
        /// </param>
        /// <param name="commandToken">
        /// The token identifying the command being checked.
        /// </param>
        /// <param name="subCommandNames">
        /// The set of sub-command names to match against.
        /// </param>
        /// <param name="allowed">
        /// Non-zero to treat the named sub-commands as the allowed set; zero
        /// to treat them as the denied set.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to the command being checked.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the policy decision; upon
        /// failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SubCommandPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            _StringDictionary subCommandNames,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaSubCommand(
                flags, commandType, commandToken, subCommandNames,
                allowed, interpreter, clientData, arguments,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs a policy check that approves or denies a command based on
        /// whether the directory of the specified file appears in the
        /// specified set of allowed (or disallowed) directories.
        /// </summary>
        /// <param name="flags">
        /// The policy flags that control the behavior of this check.
        /// </param>
        /// <param name="commandType">
        /// The type of the command being checked.
        /// </param>
        /// <param name="commandToken">
        /// The token identifying the command being checked.
        /// </param>
        /// <param name="fileName">
        /// The file whose containing directory is checked.
        /// </param>
        /// <param name="directories">
        /// The set of directories to match against; this parameter may be
        /// null.
        /// </param>
        /// <param name="allowed">
        /// Non-zero to treat the directories as the allowed set; zero to treat
        /// them as the denied set.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to the command being checked.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the policy decision; upon
        /// failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode DirectoryPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            string fileName,
            PathDictionary<object> directories,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaDirectory(
                flags, commandType, commandToken, fileName,
                directories, allowed, interpreter, clientData,
                arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs a policy check that approves or denies a command based on
        /// whether the specified URI appears in the specified set of allowed
        /// (or disallowed) URIs.
        /// </summary>
        /// <param name="flags">
        /// The policy flags that control the behavior of this check.
        /// </param>
        /// <param name="commandType">
        /// The type of the command being checked.
        /// </param>
        /// <param name="commandToken">
        /// The token identifying the command being checked.
        /// </param>
        /// <param name="uri">
        /// The URI to match against the set.
        /// </param>
        /// <param name="uris">
        /// The set of URIs to match against; this parameter may be null.
        /// </param>
        /// <param name="allowed">
        /// Non-zero to treat the URIs as the allowed set; zero to treat them
        /// as the denied set.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to the command being checked.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the policy decision; upon
        /// failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode UriPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Uri uri,
            UriDictionary<object> uris,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaUri(
                flags, commandType, commandToken, uri, uris,
                allowed, interpreter, clientData, arguments,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs a policy check that approves or denies a command based on
        /// the outcome of invoking the specified user-supplied callback.
        /// </summary>
        /// <param name="flags">
        /// The policy flags that control the behavior of this check.
        /// </param>
        /// <param name="commandType">
        /// The type of the command being checked.
        /// </param>
        /// <param name="commandToken">
        /// The token identifying the command being checked.
        /// </param>
        /// <param name="callback">
        /// The callback consulted to make the policy decision; this parameter
        /// may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to the command being checked.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the policy decision; upon
        /// failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CallbackPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            ICallback callback,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaCallback(
                flags, commandType, commandToken, callback,
                interpreter, clientData, arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs a policy check that approves or denies a command based on
        /// the outcome of evaluating the specified policy script.
        /// </summary>
        /// <param name="flags">
        /// The policy flags that control the behavior of this check.
        /// </param>
        /// <param name="commandType">
        /// The type of the command being checked.
        /// </param>
        /// <param name="commandToken">
        /// The token identifying the command being checked.
        /// </param>
        /// <param name="policyInterpreter">
        /// The interpreter used to evaluate the policy script; this parameter
        /// may be null.
        /// </param>
        /// <param name="text">
        /// The policy script to evaluate; this parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context whose command is being checked; this
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to the command being checked.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the policy decision; upon
        /// failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ScriptPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Interpreter policyInterpreter,
            string text,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaScript(
                flags, commandType, commandToken, policyInterpreter,
                text, interpreter, clientData, arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs a policy check that approves or denies a command based on
        /// whether the specified object type appears in the specified set of
        /// allowed (or disallowed) types.
        /// </summary>
        /// <param name="flags">
        /// The policy flags that control the behavior of this check.
        /// </param>
        /// <param name="commandType">
        /// The type of the command being checked.
        /// </param>
        /// <param name="commandToken">
        /// The token identifying the command being checked.
        /// </param>
        /// <param name="objectType">
        /// The type to match against the set.
        /// </param>
        /// <param name="types">
        /// The set of types to match against; this parameter may be null.
        /// </param>
        /// <param name="allowed">
        /// Non-zero to treat the types as the allowed set; zero to treat them
        /// as the denied set.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to the command being checked.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the policy decision; upon
        /// failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode TypePolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Type objectType,
            TypeList types,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaType(
                flags, commandType, commandToken, objectType,
                types, allowed, interpreter, clientData,
                arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether any path in the specified list refers to the
        /// same underlying file or directory as the specified second path.
        /// </summary>
        /// <param name="paths">
        /// The list of candidate paths to compare.
        /// </param>
        /// <param name="path2">
        /// The path to compare each candidate against.
        /// </param>
        /// <returns>
        /// True if any path in the list refers to the same file as path2;
        /// otherwise, false.
        /// </returns>
        public static bool IsSameFile(
            StringList paths,
            string path2
            )
        {
            return IsSameFile(null, paths, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether any path in the specified list refers to the
        /// same underlying file or directory as the specified second path,
        /// using the specified interpreter context.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="paths">
        /// The list of candidate paths to compare.
        /// </param>
        /// <param name="path2">
        /// The path to compare each candidate against.
        /// </param>
        /// <returns>
        /// True if any path in the list refers to the same file as path2;
        /// otherwise, false.
        /// </returns>
        public static bool IsSameFile(
            Interpreter interpreter,
            StringList paths,
            string path2
            ) /* DEADLOCK-ON-DISPOSE */
        {
            foreach (string path1 in paths)
                if (IsSameFile(interpreter, path1, path2))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the two specified paths refer to the same
        /// underlying file or directory.
        /// </summary>
        /// <param name="path1">
        /// The first path to compare.
        /// </param>
        /// <param name="path2">
        /// The second path to compare.
        /// </param>
        /// <returns>
        /// True if the two paths refer to the same file; otherwise, false.
        /// </returns>
        public static bool IsSameFile(
            string path1,
            string path2
            )
        {
            return IsSameFile(null, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the two specified paths refer to the same
        /// underlying file or directory, using the specified interpreter
        /// context and preferring native platform file information when
        /// available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="path1">
        /// The first path to compare.
        /// </param>
        /// <param name="path2">
        /// The second path to compare.
        /// </param>
        /// <returns>
        /// True if the two paths refer to the same file; otherwise, false.
        /// </returns>
        public static bool IsSameFile(
            Interpreter interpreter,
            string path1,
            string path2
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.IsSameFile(interpreter, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Normalizes the specified path, returning the path unchanged if it
        /// is invalid or cannot be normalized.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="path">
        /// The path to normalize.
        /// </param>
        /// <returns>
        /// The normalized path, or the original path when it is invalid or
        /// cannot be normalized.
        /// </returns>
        public static string RobustNormalizePath(
            Interpreter interpreter,
            string path
            )
        {
            return PathOps.RobustNormalizePath(interpreter, path);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the specified path to a fully qualified path, performing
        /// environment variable and leading tilde substitution.
        /// </summary>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <returns>
        /// The resolved path, or null if it cannot be resolved.
        /// </returns>
        public static string NormalizePath(
            string path
            )
        {
            return NormalizePath(null, path);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the specified path to a fully qualified path, performing
        /// environment variable and leading tilde substitution and normalizing
        /// its directory separators.
        /// </summary>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <param name="unix">
        /// Non-zero to normalize directory separators to forward slashes, zero
        /// to normalize them to backslashes, or null to leave them unchanged;
        /// this parameter may be null.
        /// </param>
        /// <returns>
        /// The resolved path, or null if it cannot be resolved.
        /// </returns>
        public static string NormalizePath(
            string path,
            bool? unix
            )
        {
            return NormalizePath(null, path, unix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the specified path to a fully qualified path, performing
        /// environment variable and leading tilde substitution, using the
        /// specified interpreter context.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <returns>
        /// The resolved path, or null if it cannot be resolved.
        /// </returns>
        public static string NormalizePath(
            Interpreter interpreter,
            string path
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.ResolvePath(interpreter, path);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the specified path to a fully qualified path, performing
        /// environment variable and leading tilde substitution and normalizing
        /// its directory separators, using the specified interpreter context.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <param name="unix">
        /// Non-zero to normalize directory separators to forward slashes, zero
        /// to normalize them to backslashes, or null to leave them unchanged;
        /// this parameter may be null.
        /// </param>
        /// <returns>
        /// The resolved path, or null if it cannot be resolved.
        /// </returns>
        public static string NormalizePath(
            Interpreter interpreter, /* in */
            string path,             /* in */
            bool? unix               /* in */
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.ResolvePath(interpreter, path, unix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the first directory separator character that appears in the
        /// specified path, falling back to the native directory separator when
        /// none is present.
        /// </summary>
        /// <param name="path">
        /// The path to scan for a directory separator; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The first forward- or backward-slash found in the path, or the
        /// native directory separator character when none is present.
        /// </returns>
        public static char GetFirstDirectorySeparator(
            string path
            )
        {
            return PathOps.GetFirstDirectorySeparator(path);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current process is running with
        /// administrative privileges.
        /// </summary>
        /// <returns>
        /// True if the current process has administrative privileges;
        /// otherwise, false.
        /// </returns>
        public static bool IsAdministrator()
        {
            return _RuntimeOps.IsAdministrator();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Verifies that the specified set of compile-time define constants is
        /// consistent with the managed runtime that is currently executing.
        /// </summary>
        /// <param name="defines">
        /// The set of compile-time define constants to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CheckDefineConstants(
            StringList defines,
            ref Result error
            )
        {
            return CommonOps.Runtime.CheckDefineConstants(defines, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current managed runtime is version 2.x of
        /// the .NET Framework.
        /// </summary>
        /// <returns>
        /// True if running on the .NET Framework 2.x; otherwise, false.
        /// </returns>
        public static bool IsFramework20()
        {
            return CommonOps.Runtime.IsFramework20();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current managed runtime is version 4.x of
        /// the .NET Framework.
        /// </summary>
        /// <returns>
        /// True if running on the .NET Framework 4.x; otherwise, false.
        /// </returns>
        public static bool IsFramework40()
        {
            return CommonOps.Runtime.IsFramework40();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current managed runtime is the Mono runtime.
        /// </summary>
        /// <returns>
        /// True if running under the Mono runtime; otherwise, false.
        /// </returns>
        public static bool IsMono()
        {
            return CommonOps.Runtime.IsMono();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current managed runtime is .NET Core (or
        /// .NET 5.0 or higher).
        /// </summary>
        /// <returns>
        /// True if running under .NET Core or higher; otherwise, false.
        /// </returns>
        public static bool IsDotNetCore()
        {
            return CommonOps.Runtime.IsDotNetCore();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current managed runtime appears to be the
        /// .NET 5.0 runtime or higher.
        /// </summary>
        /// <returns>
        /// True if the runtime appears to be .NET 5.0 or higher; otherwise,
        /// false.
        /// </returns>
        public static bool IsDotNetCore5xOrHigher()
        {
            return CommonOps.Runtime.IsDotNetCore5xOrHigher();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current managed runtime appears to be the
        /// .NET 7.0 runtime or higher.
        /// </summary>
        /// <returns>
        /// True if the runtime appears to be .NET 7.0 or higher; otherwise,
        /// false.
        /// </returns>
        public static bool IsDotNetCore7xOrHigher()
        {
            return CommonOps.Runtime.IsDotNetCore7xOrHigher();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the user interface is currently interactive,
        /// based on environment variables, the operating system
        /// user-interactive state, and interpreter state.
        /// </summary>
        /// <returns>
        /// True if the user interface is interactive; otherwise, false.
        /// </returns>
        public static bool IsInteractive()
        {
            return WindowOps.IsInteractive();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deletes the files matching the specified patterns within the
        /// specified directory and then removes the resulting empty
        /// subdirectories and the directory itself.
        /// </summary>
        /// <param name="directory">
        /// The directory to clean up and remove.
        /// </param>
        /// <param name="patterns">
        /// The file name patterns selecting which files to delete; may be
        /// null, in which case no files are deleted and only the (now empty)
        /// subdirectories and directory are removed.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to process all subdirectories recursively; otherwise, only
        /// the specified directory is processed.
        /// </param>
        /// <returns>
        /// True if the directory was cleaned up and removed successfully;
        /// otherwise, false.
        /// </returns>
        public static bool CleanupDirectory(
            string directory,
            IEnumerable<string> patterns,
            bool recursive
            )
        {
            return FileOps.CleanupDirectory(directory, patterns, recursive);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified path is valid for use as a file or
        /// directory, optionally requiring it to be rooted and/or to exist.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <param name="asDirectory">
        /// Non-zero to validate the path as a directory; otherwise, the path
        /// is validated as a file.
        /// </param>
        /// <param name="rooted">
        /// Non-zero to require the path to be rooted, zero to require that it
        /// not be rooted, or null to skip this check.
        /// </param>
        /// <param name="exists">
        /// Non-zero to require the path to exist, zero to require that it not
        /// exist, or null to skip this check.
        /// </param>
        /// <returns>
        /// True if the path satisfies all of the requested constraints;
        /// otherwise, false.
        /// </returns>
        public static bool ValidatePath(
            string path,
            bool asDirectory,
            bool? rooted,
            bool? exists
            )
        {
            return asDirectory ?
                PathOps.ValidatePathAsDirectory(path, rooted, exists) :
                PathOps.ValidatePathAsFile(path, rooted, exists);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        /// <summary>
        /// Verifies that the specified path satisfies the requested
        /// permissions, taking into account whether it exists, whether it is a
        /// file or directory, whether it is read-only, and the access rights
        /// of the current user.
        /// </summary>
        /// <param name="path">
        /// The path to verify; may be null or empty.
        /// </param>
        /// <param name="permissions">
        /// The set of file permissions that the path is required to satisfy.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode VerifyPath(
            string path,
            FilePermission permissions,
            ref Result error
            )
        {
            return FileOps.VerifyPath(path, permissions, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Compares two path parts using the string comparison appropriate for
        /// file names on the current operating system.
        /// </summary>
        /// <param name="part1">
        /// The first path part to compare; may be null.
        /// </param>
        /// <param name="part2">
        /// The second path part to compare; may be null.
        /// </param>
        /// <returns>
        /// Zero if the two path parts are equal, a negative number if
        /// <paramref name="part1"/> sorts before <paramref name="part2"/>, or
        /// a positive number if it sorts after.
        /// </returns>
        public static int ComparePathParts(
            string part1,
            string part2
            )
        {
            return PathOps.CompareParts(part1, part2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Compares two file names, after converting each one to its native
        /// form, using the string comparison appropriate for file names on the
        /// current operating system.
        /// </summary>
        /// <param name="path1">
        /// The first file name to compare; may be null.
        /// </param>
        /// <param name="path2">
        /// The second file name to compare; may be null.
        /// </param>
        /// <returns>
        /// Zero if the two file names are equal, a negative number if
        /// <paramref name="path1"/> sorts before <paramref name="path2"/>, or
        /// a positive number if it sorts after.
        /// </returns>
        public static int CompareFileNames(
            string path1,
            string path2
            )
        {
            return PathOps.CompareFileNames(path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the <see cref="PathType" /> of the specified path,
        /// distinguishing absolute, relative, and (on Windows) volume-relative
        /// paths.
        /// </summary>
        /// <param name="path">
        /// The path to examine; may be null.
        /// </param>
        /// <returns>
        /// The <see cref="PathType" /> classifying the specified path.
        /// </returns>
        public static PathType GetPathType(
            string path
            )
        {
            return PathOps.GetPathType(path);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the string comparison type appropriate for comparing file
        /// names on the current operating system.
        /// </summary>
        /// <returns>
        /// A case-insensitive comparison type on Windows, where file names are
        /// not case-sensitive; otherwise, a case-sensitive comparison type.
        /// </returns>
        public static StringComparison GetPathComparisonType()
        {
            return PathOps.GetComparisonType();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the ordinal string comparison type used for internal,
        /// system-level string comparisons.
        /// </summary>
        /// <param name="noCase">
        /// Non-zero to return a case-insensitive ordinal comparison type;
        /// otherwise, a case-sensitive ordinal comparison type.
        /// </param>
        /// <returns>
        /// StringComparison.OrdinalIgnoreCase if <paramref name="noCase"/> is
        /// non-zero; otherwise, StringComparison.Ordinal.
        /// </returns>
        public static StringComparison GetSystemComparisonType(
            bool noCase
            )
        {
            return SharedStringOps.GetSystemComparisonType(noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether two strings are equal using the specified string
        /// comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare; may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare; may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison type governing the equality test.
        /// </param>
        /// <returns>
        /// True if the two strings are equal under the specified comparison;
        /// otherwise, false.
        /// </returns>
        public static bool StringEquals(
            string left,
            string right,
            StringComparison comparisonType
            )
        {
            return SharedStringOps.Equals(left, right, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether two strings are equal using the case-sensitive
        /// ordinal comparison used for internal, system-level string
        /// comparisons.
        /// </summary>
        /// <param name="left">
        /// The first string to compare; may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare; may be null.
        /// </param>
        /// <returns>
        /// True if the two strings are equal under the system ordinal
        /// comparison; otherwise, false.
        /// </returns>
        public static bool SystemStringEquals(
            string left,
            string right
            )
        {
            return SharedStringOps.SystemEquals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether two strings are equal using the ordinal
        /// comparison used for internal, system-level string comparisons,
        /// optionally ignoring case.
        /// </summary>
        /// <param name="left">
        /// The first string to compare; may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare; may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case while comparing; otherwise, the comparison
        /// is case-sensitive.
        /// </param>
        /// <returns>
        /// True if the two strings are equal under the selected system ordinal
        /// comparison; otherwise, false.
        /// </returns>
        public static bool SystemStringEquals(
            string left,
            string right,
            bool noCase
            )
        {
            return SharedStringOps.Equals(left, right,
                SharedStringOps.GetSystemComparisonType(noCase));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Normalizes all line endings in the specified text to the line-feed
        /// convention required by the script evaluation engine, converting
        /// carriage-return and carriage-return/line-feed sequences.
        /// </summary>
        /// <param name="text">
        /// The text whose line endings are to be normalized; may be null.
        /// </param>
        /// <returns>
        /// The text with normalized line endings, or the original null or
        /// empty value when it has no content.
        /// </returns>
        public static string NormalizeLineEndings(
            string text
            )
        {
            return StringOps.NormalizeLineEndings(text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the default attribute flags key, which identifies flags
        /// that are not associated with any explicit key.
        /// </summary>
        /// <returns>
        /// The default attribute flags key, which is zero.
        /// </returns>
        public static long DefaultAttributeFlagsKey()
        {
            return AttributeFlags.DefaultKey;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses a textual attribute flags specification into a dictionary
        /// mapping each key to its set of flag characters.
        /// </summary>
        /// <param name="text">
        /// The textual attribute flags specification to parse.
        /// </param>
        /// <param name="complex">
        /// Non-zero to permit complex, keyed flags enclosed in braces;
        /// otherwise, only flags for the default key are recognized.
        /// </param>
        /// <param name="space">
        /// Non-zero to ignore whitespace characters within the specification;
        /// otherwise, whitespace is significant.
        /// </param>
        /// <param name="sort">
        /// Non-zero to sort the flags for each key in the result; otherwise,
        /// their original order is preserved.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A dictionary mapping each key to its flag characters, or null if
        /// the specification could not be parsed.
        /// </returns>
        public static IDictionary<long, string> ParseAttributeFlags(
            string text,
            bool complex,
            bool space,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Parse(
                text, complex, space, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders a dictionary of per-key attribute flags back into its
        /// textual attribute flags representation.
        /// </summary>
        /// <param name="flags">
        /// The dictionary mapping each key to its flag characters.
        /// </param>
        /// <param name="legacy">
        /// Non-zero to render keyed flags using the legacy fixed-width
        /// hexadecimal format; otherwise, the variable-width, colon-separated
        /// format is used.
        /// </param>
        /// <param name="compact">
        /// Non-zero to collapse duplicate flag characters before rendering;
        /// otherwise, they are rendered as-is.
        /// </param>
        /// <param name="space">
        /// Non-zero to insert a space between successive keyed groups;
        /// otherwise, no separating space is added.
        /// </param>
        /// <param name="sort">
        /// Non-zero to sort both the keys and the flags within each key;
        /// otherwise, their original order is preserved.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The textual attribute flags representation, or null if the
        /// dictionary could not be formatted.
        /// </returns>
        public static string FormatAttributeFlags(
            IDictionary<long, string> flags,
            bool legacy,
            bool compact,
            bool space,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Format(
                flags, legacy, compact, space, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Verifies that the specified text is a well-formed attribute flags
        /// specification by attempting to parse it.
        /// </summary>
        /// <param name="text">
        /// The textual attribute flags specification to verify.
        /// </param>
        /// <param name="complex">
        /// Non-zero to permit complex, keyed flags enclosed in braces;
        /// otherwise, only flags for the default key are recognized.
        /// </param>
        /// <param name="space">
        /// Non-zero to ignore whitespace characters within the specification;
        /// otherwise, whitespace is significant.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// True if the specification is well-formed; otherwise, false.
        /// </returns>
        public static bool VerifyAttributeFlags(
            string text,
            bool complex,
            bool space,
            ref Result error
            )
        {
            return AttributeFlags.Verify(text, complex, space, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the flags associated with the specified key
        /// include some or all of the specified flag characters.
        /// </summary>
        /// <param name="flags">
        /// The dictionary mapping each key to its flag characters.
        /// </param>
        /// <param name="key">
        /// The key whose associated flags are tested.
        /// </param>
        /// <param name="haveFlags">
        /// The flag characters to test for; may be null, in which case the
        /// test always succeeds, or empty, in which case the test succeeds
        /// only when the key has no flags.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are present;
        /// otherwise, the presence of any one of them is sufficient.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat invalid flag characters as a failure; otherwise,
        /// they are ignored.
        /// </param>
        /// <returns>
        /// True if the key's flags satisfy the test; otherwise, false.
        /// </returns>
        public static bool HaveAttributeFlags(
            IDictionary<long, string> flags,
            long key,
            string haveFlags,
            bool all,
            bool strict
            )
        {
            return AttributeFlags.Have(flags, key, haveFlags, all, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Applies a sequence of add, remove, and set operations to the flags
        /// associated with the specified key, returning a new dictionary with
        /// the modified flags.
        /// </summary>
        /// <param name="flags">
        /// The dictionary mapping each key to its flag characters.
        /// </param>
        /// <param name="key">
        /// The key whose associated flags are changed.
        /// </param>
        /// <param name="changeFlags">
        /// The change specification to apply, which may contain add, remove,
        /// and set meta-characters as well as meta-characters that expand to
        /// groups of flag characters.
        /// </param>
        /// <param name="sort">
        /// Non-zero to sort the resulting flags for the key; otherwise, their
        /// order is preserved.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A new dictionary reflecting the requested changes, or null if the
        /// changes could not be applied.
        /// </returns>
        public static IDictionary<long, string> ChangeAttributeFlags(
            IDictionary<long, string> flags,
            long key,
            string changeFlags,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Change(
                flags, key, changeFlags, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a standard "bad value" error message, optionally listing the
        /// set of acceptable values.
        /// </summary>
        /// <param name="adjective">
        /// The adjective describing the problem with the value; may be null to
        /// use a default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the value; may be null to use a
        /// default.
        /// </param>
        /// <param name="value">
        /// The offending value.
        /// </param>
        /// <param name="values">
        /// The set of acceptable values to list; may be null.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable value; may be null.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable values; may be null.
        /// </param>
        /// <returns>
        /// A <see cref="Result" /> containing the formatted error message.
        /// </returns>
        public static Result BadValue(
            string adjective,
            string type,
            string value,
            IEnumerable<string> values,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadValue(
                adjective, type, value, values, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a standard "bad sub-command" error message, listing the
        /// sub-commands supported by the specified ensemble.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null. This parameter is not
        /// used.
        /// </param>
        /// <param name="adjective">
        /// The adjective describing the problem with the sub-command; may be
        /// null to use a default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the sub-command; may be null to
        /// use a default.
        /// </param>
        /// <param name="subCommand">
        /// The offending sub-command name.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble whose sub-commands are acceptable.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable sub-command; may be null.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable sub-commands; may be
        /// null.
        /// </param>
        /// <returns>
        /// A <see cref="Result" /> containing the formatted error message.
        /// </returns>
        public static Result BadSubCommand(
            Interpreter interpreter, /* NOT USED */
            string adjective,
            string type,
            string subCommand,
            IEnsemble ensemble,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadSubCommand(
                interpreter, adjective, type, subCommand, ensemble,
                prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a standard "wrong # args" error message from the specified
        /// arguments.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier associated with the command; may be null. This
        /// parameter is not used.
        /// </param>
        /// <param name="count">
        /// The number of leading arguments to include in the message.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments used to construct the message; may be null.
        /// </param>
        /// <param name="suffix">
        /// The text to append after the argument summary; may be null.
        /// </param>
        /// <returns>
        /// A <see cref="Result" /> containing the formatted error message.
        /// </returns>
        public static Result WrongNumberOfArguments(
            IIdentifierName identifierName,
            int count,
            ArgumentList arguments,
            string suffix
            )
        {
            return ScriptOps.WrongNumberOfArguments(
                identifierName, count, arguments, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a standard "bad sub-command" error message, listing the
        /// supported sub-commands and distinguishing an unsupported
        /// sub-command from an entirely unknown one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null. This parameter is not
        /// used.
        /// </param>
        /// <param name="adjective">
        /// The adjective describing the problem with the sub-command; may be
        /// null to use a default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the sub-command; may be null to
        /// use a default.
        /// </param>
        /// <param name="subCommand">
        /// The offending sub-command name.
        /// </param>
        /// <param name="subCommands">
        /// The dictionary of acceptable sub-commands.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable sub-command; may be null.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable sub-commands; may be
        /// null.
        /// </param>
        /// <returns>
        /// A <see cref="Result" /> containing the formatted error message.
        /// </returns>
        public static Result BadSubCommand(
            Interpreter interpreter, /* NOT USED */
            string adjective,
            string type,
            string subCommand,
            EnsembleDictionary subCommands,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadSubCommand(
                interpreter, adjective, type, subCommand, subCommands,
                prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves a sub-command name against the specified dictionary of
        /// sub-commands, allowing an unambiguous prefix to match.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="subCommands">
        /// The dictionary of sub-commands to search.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found;
        /// otherwise, the absence of a match is not an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match case-insensitively, zero to match
        /// case-sensitively, or null to use the default; may be null.
        /// </param>
        /// <param name="name">
        /// On input, the sub-command name to resolve; upon a successful match,
        /// receives the resolved sub-command name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,
            EnsembleDictionary subCommands,
            string type,
            bool strict,
            bool? noCase,
            ref string name,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.SubCommandFromEnsemble(
                interpreter, subCommands, type, strict, noCase, ref name,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves a sub-command name from the specified ensemble and, if
        /// found, executes it in a single step.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to search; may be null.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data to pass to the sub-command; may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to pass to the sub-command; may be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found;
        /// otherwise, the absence of a match is not an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match case-insensitively, zero to match
        /// case-sensitively, or null to use the default; may be null.
        /// </param>
        /// <param name="name">
        /// On input, the sub-command name to resolve; upon a successful match,
        /// receives the resolved sub-command name.
        /// </param>
        /// <param name="tried">
        /// Upon return, receives non-zero if the sub-command was actually
        /// dispatched; otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result of executing the
        /// sub-command; upon failure, it receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter,
            IEnsemble ensemble,
            IClientData clientData,
            ArgumentList arguments,
            bool strict,
            bool? noCase,
            ref string name,
            ref bool tried,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, ensemble, clientData, arguments, strict,
                noCase, ref name, ref tried, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Populates the command and policy metadata for the specified plugin
        /// by discovering the appropriate types and filtering them according
        /// to the supplied rule set and command flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when applying the rule set; may be
        /// null.
        /// </param>
        /// <param name="plugin">
        /// The plugin to receive the discovered command and policy metadata.
        /// </param>
        /// <param name="types">
        /// The candidate types to consider; may be null, in which case the
        /// types are queried from the plugin as needed.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to filter the discovered entities; may be null.
        /// </param>
        /// <param name="commandFlags">
        /// If not null, only commands having all of these flags are included.
        /// </param>
        /// <param name="notCommandFlags">
        /// If not null, commands having any of these flags are excluded.
        /// </param>
        /// <param name="noCommands">
        /// Non-zero to skip populating command metadata.
        /// </param>
        /// <param name="noPolicies">
        /// Non-zero to skip populating policy metadata.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce more detailed error information.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode PopulatePluginEntities(
            Interpreter interpreter,
            IPlugin plugin,
            TypeList types,
            IRuleSet ruleSet,
            CommandFlags? commandFlags,
            CommandFlags? notCommandFlags,
            bool noCommands,
            bool noPolicies,
            bool verbose,
            ref Result error
            )
        {
            return _RuntimeOps.PopulatePluginEntities(
                interpreter, plugin, types, ruleSet, commandFlags,
                notCommandFlags, false, noCommands, noPolicies,
                verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the policy context from the specified policy callback
        /// client data and determines whether the executable object it
        /// contains matches the specified command type and token.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the policy request.
        /// </param>
        /// <param name="clientData">
        /// The policy callback client data from which the policy context is
        /// extracted.
        /// </param>
        /// <param name="commandType">
        /// The command type to match against; may be null to skip type
        /// matching.
        /// </param>
        /// <param name="commandToken">
        /// The command token to match against, or zero to match by type
        /// instead.
        /// </param>
        /// <param name="policyContext">
        /// On input, the policy context to use, or null to obtain it from
        /// <paramref name="clientData"/>; upon success, receives the policy
        /// context.
        /// </param>
        /// <param name="match">
        /// Upon success, receives non-zero if the executable object matched
        /// the specified type or token.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ExtractPolicyContextAndCommand(
            Interpreter interpreter,
            IClientData clientData,
            Type commandType,
            long commandToken,
            ref IPolicyContext policyContext,
            ref bool match,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the policy context and the plugin it references from the
        /// specified policy callback client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the policy request; this
        /// parameter is not used.
        /// </param>
        /// <param name="clientData">
        /// The policy callback client data from which the policy context is
        /// extracted.
        /// </param>
        /// <param name="policyContext">
        /// On input, the policy context to use, or null to obtain it from
        /// <paramref name="clientData"/>; upon success, receives the policy
        /// context.
        /// </param>
        /// <param name="plugin">
        /// Upon success, receives the plugin referenced by the policy context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ExtractPolicyContextAndPlugin(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref IPlugin plugin,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndPlugin(
                interpreter, clientData, ref policyContext, ref plugin,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the policy context, encoding, script, and timeout from the
        /// specified policy callback client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the policy request; this
        /// parameter is not used.
        /// </param>
        /// <param name="clientData">
        /// The policy callback client data from which the policy context is
        /// extracted.
        /// </param>
        /// <param name="policyContext">
        /// On input, the policy context to use, or null to obtain it from
        /// <paramref name="clientData"/>; upon success, receives the policy
        /// context.
        /// </param>
        /// <param name="encoding">
        /// Upon success, receives the encoding from the policy context; may be
        /// null.
        /// </param>
        /// <param name="script">
        /// Upon success, receives the script from the policy context.
        /// </param>
        /// <param name="timeout">
        /// Upon success, receives the timeout from the policy context, or null
        /// if none is specified.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ExtractPolicyContextAndScript(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref Encoding encoding,
            ref IScript script,
            ref int? timeout,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndScript(
                interpreter, clientData, ref policyContext, ref encoding,
                ref script, ref timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the policy context, file name, and timeout from the
        /// specified policy callback client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the policy request; this
        /// parameter is not used.
        /// </param>
        /// <param name="clientData">
        /// The policy callback client data from which the policy context is
        /// extracted.
        /// </param>
        /// <param name="policyContext">
        /// On input, the policy context to use, or null to obtain it from
        /// <paramref name="clientData"/>; upon success, receives the policy
        /// context.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the file name from the policy context.
        /// </param>
        /// <param name="timeout">
        /// Upon success, receives the timeout from the policy context, or null
        /// if none is specified.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ExtractPolicyContextAndFileName(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string fileName,
            ref int? timeout,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndFileName(
                interpreter, clientData, ref policyContext, ref fileName,
                ref timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the policy context and text from the specified policy
        /// callback client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the policy request; this
        /// parameter is not used.
        /// </param>
        /// <param name="clientData">
        /// The policy callback client data from which the policy context is
        /// extracted.
        /// </param>
        /// <param name="policyContext">
        /// On input, the policy context to use, or null to obtain it from
        /// <paramref name="clientData"/>; upon success, receives the policy
        /// context.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the text from the policy context; may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ExtractPolicyContextAndText(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string text,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndText(
                interpreter, clientData, ref policyContext, ref text,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts the policy context, encoding, text, hash value, and script
        /// bytes from the specified policy callback client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the policy request.
        /// </param>
        /// <param name="clientData">
        /// The policy callback client data from which the policy context is
        /// extracted.
        /// </param>
        /// <param name="policyContext">
        /// On input, the policy context to use, or null to obtain it from
        /// <paramref name="clientData"/>; upon success, receives the policy
        /// context.
        /// </param>
        /// <param name="encoding">
        /// Upon success, receives the encoding from the policy context; may be
        /// null.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the text from the policy context; may be
        /// null.
        /// </param>
        /// <param name="hashValue">
        /// Upon success, receives the hash value from the policy context; may
        /// be null.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives a copy of the script bytes from the policy
        /// context, or null if none are available.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ExtractPolicyContextAndTextAndBytes(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref Encoding encoding,
            ref string text,
            ref byte[] hashValue,
            ref ByteList bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndTextAndBytes(
                interpreter, clientData, ref policyContext, ref encoding,
                ref text, ref hashValue, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Detects and sets the script library path using the specified
        /// assembly, according to the requested detection methods.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used as the starting point for detection.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="detectFlags">
        /// The flags controlling which detection methods, such as assembly
        /// location, environment variables, and registry settings, are
        /// attempted.
        /// </param>
        /// <returns>
        /// True if a suitable script library path was detected; otherwise,
        /// false.
        /// </returns>
        public static bool DetectLibraryPath(
            Assembly assembly,
            IClientData clientData,
            DetectFlags detectFlags
            )
        {
            return GlobalState.DetectLibraryPath(
                assembly, clientData, detectFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Detects and sets the script library path using the specified
        /// assembly name and assembly, according to the requested detection
        /// methods.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name used as the starting point for detection.
        /// </param>
        /// <param name="assembly">
        /// The assembly associated with the assembly name.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="detectFlags">
        /// The flags controlling which detection methods, such as assembly
        /// location, environment variables, and registry settings, are
        /// attempted.
        /// </param>
        /// <returns>
        /// True if a suitable script library path was detected; otherwise,
        /// false.
        /// </returns>
        public static bool DetectLibraryPath(
            AssemblyName assemblyName,
            Assembly assembly,
            IClientData clientData,
            DetectFlags detectFlags
            )
        {
            return GlobalState.DetectLibraryPath(
                assemblyName, assembly, clientData, detectFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a result that can be used to deliver an operation result
        /// across threads, backed by a newly created event wait handle stored
        /// in its client data.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the underlying event wait handle; may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created synchronized result.
        /// </returns>
        public static Result CreateSynchronizedResult(
            string name
            )
        {
            return ResultOps.CreateSynchronized(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cleans up a synchronized result by closing the underlying event
        /// wait handle stored in its client data, if any.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result to clean up; may be null.
        /// </param>
        public static void CleanupSynchronizedResult(
            Result synchronizedResult
            )
        {
            ResultOps.CleanupSynchronized(synchronizedResult);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Waits for the specified event to be signaled, for up to the
        /// requested amount of time, optionally processing events and honoring
        /// script cancellation while it waits.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="event">
        /// The event to wait on, or null to simply wait for the requested
        /// amount of time.
        /// </param>
        /// <param name="waitMicroseconds">
        /// The total amount of time, in microseconds, to wait, or null to use
        /// the default.
        /// </param>
        /// <param name="readyMicroseconds">
        /// The amount of time, in microseconds, to wait for interpreter
        /// readiness, or null to use the default.
        /// </param>
        /// <param name="eventWaitFlags">
        /// The flags controlling the wait behavior, such as whether a timeout
        /// applies and whether cancellation is honored.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode Wait(
            Interpreter interpreter,
            EventWaitHandle @event,
            long? waitMicroseconds,
            long? readyMicroseconds,
            EventWaitFlags eventWaitFlags,
            ref Result error
            ) /* SAFE-ON-DISPOSE */
        {
            return EventOps.Wait(interpreter,
                @event, waitMicroseconds, readyMicroseconds,
                !FlagOps.HasFlags(
                    eventWaitFlags, EventWaitFlags.NoTimeout, true),
                FlagOps.HasFlags(
                    eventWaitFlags, EventWaitFlags.NoWindows, true),
                FlagOps.HasFlags(
                    eventWaitFlags, EventWaitFlags.NoCancel, true),
                FlagOps.HasFlags(
                    eventWaitFlags, EventWaitFlags.NoGlobalCancel, true),
                FlagOps.HasFlags(
                    eventWaitFlags, EventWaitFlags.Trace, true),
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Waits indefinitely for the event wait handle associated with the
        /// specified synchronized result to be signaled.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result whose event wait handle is awaited; may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the event was signaled; otherwise, false.
        /// </returns>
        public static bool WaitSynchronizedResult(
            Result synchronizedResult
            )
        {
            return ResultOps.WaitSynchronized(synchronizedResult);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Waits up to the specified timeout for the event wait handle
        /// associated with the specified synchronized result to be signaled.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result whose event wait handle is awaited; may be
        /// null.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait.
        /// </param>
        /// <returns>
        /// True if the event was signaled before the timeout elapsed;
        /// otherwise, false.
        /// </returns>
        public static bool WaitSynchronizedResult(
            Result synchronizedResult,
            int timeout
            )
        {
            return ResultOps.WaitSynchronized(
                synchronizedResult, timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the specified return code and result into the synchronized
        /// result and signals its event wait handle, if any, to notify a
        /// waiting thread that the result is available.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result to populate and signal; may be null.
        /// </param>
        /// <param name="code">
        /// The return code to store.
        /// </param>
        /// <param name="result">
        /// The result value to store; may be null.
        /// </param>
        public static void SetSynchronizedResult(
            Result synchronizedResult,
            ReturnCode code,
            Result result
            )
        {
            ResultOps.SetSynchronized(synchronizedResult, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Retrieves the return code and result value previously stored into
        /// the specified synchronized result.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result to read from; may be null.
        /// </param>
        /// <param name="code">
        /// Upon success, receives the stored return code.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the stored result value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetSynchronizedResult(
            Result synchronizedResult,
            ref ReturnCode code,
            ref Result result,
            ref Result error
            )
        {
            return ResultOps.GetSynchronized(
                synchronizedResult, ref code, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// Determines whether exclusive mode for software updates, which
        /// restricts validation to trusted update keys, is currently enabled.
        /// </summary>
        /// <returns>
        /// Non-zero if exclusive mode is enabled, zero if it is disabled, or
        /// null if the status could not be determined.
        /// </returns>
        public static bool? IsSoftwareUpdateExclusive()
        {
            return UpdateOps.IsExclusive();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables exclusive mode for software updates, which
        /// restricts validation to trusted update keys.
        /// </summary>
        /// <param name="exclusive">
        /// Non-zero to enable exclusive mode, zero to disable it, or null to
        /// leave the current setting unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SetSoftwareUpdateExclusive(
            bool? exclusive,
            ref Result error
            )
        {
            if (exclusive == null) return ReturnCode.Ok;
            return UpdateOps.SetExclusive((bool)exclusive, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the software update trusted status, which
        /// governs certificate validation for update connections, is currently
        /// enabled.
        /// </summary>
        /// <returns>
        /// Non-zero if the trusted status is enabled, zero if it is disabled,
        /// or null if the status could not be determined.
        /// </returns>
        public static bool? IsSoftwareUpdateTrusted()
        {
            return UpdateOps.IsTrusted();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables the software update trusted status, which
        /// governs certificate validation for update connections.
        /// </summary>
        /// <param name="trusted">
        /// Non-zero to enable the trusted status, zero to disable it, or null
        /// to leave the current setting unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SetSoftwareUpdateTrusted(
            bool? trusted,
            ref Result error
            )
        {
            if (trusted == null) return ReturnCode.Ok;
            return UpdateOps.SetTrusted((bool)trusted, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the current application domain or process
        /// appears to be shutting down, by checking for process shutdown, a
        /// pending domain unload, and domain finalization.
        /// </summary>
        /// <returns>
        /// True if the application domain or process is stopping soon;
        /// otherwise, false.
        /// </returns>
        public static bool AppDomainIsStoppingSoon()
        {
            return AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the identifier of the specified application domain into a
        /// string, optionally producing a display form prefixed for
        /// presentation.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain whose identifier is formatted; may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to produce a display form, prefixed for presentation;
        /// otherwise, only the bare identifier is produced.
        /// </param>
        /// <returns>
        /// The formatted application domain identifier string, or null if it
        /// is not available.
        /// </returns>
        public static string FormatAppDomainId(
            AppDomain appDomain,
            bool display
            )
        {
            return AppDomainOps.FormatIdString(appDomain, false, display);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified application domain is
        /// the current application domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to compare against the current one; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified application domain is the current one;
        /// otherwise, false.
        /// </returns>
        public static bool IsCurrentAppDomain(
            AppDomain appDomain
            )
        {
            return AppDomainOps.IsCurrent(appDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current application domain is
        /// the default application domain.
        /// </summary>
        /// <returns>
        /// True if the current application domain is the default application
        /// domain; otherwise, false.
        /// </returns>
        public static bool IsDefaultAppDomain()
        {
            return AppDomainOps.IsCurrentDefault();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one, treating an
        /// isolated plugin as cross-domain.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain or is
        /// isolated; otherwise, false.
        /// </returns>
        public static bool IsCrossAppDomain(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsCross(
                pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one, treating an
        /// isolated plugin as cross-domain.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when an application domain involved in the
        /// comparison is null; when this parameter is itself null, the
        /// comparison proceeds normally.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain or is
        /// isolated; otherwise, false.
        /// </returns>
        public static bool IsCrossAppDomain(
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
            return AppDomainOps.IsCross(
                pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one, without treating
        /// an isolated plugin as automatically cross-domain.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain; otherwise,
        /// false.
        /// </returns>
        public static bool IsCrossAppDomainNoIsolated(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsCrossNoIsolated(
                pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one, without treating
        /// an isolated plugin as automatically cross-domain.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when an application domain involved in the
        /// comparison is null; when this parameter is itself null, the
        /// comparison proceeds normally.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain; otherwise,
        /// false.
        /// </returns>
        public static bool IsCrossAppDomainNoIsolated(
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
            return AppDomainOps.IsCrossNoIsolated(
                pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains, treating an isolated plugin
        /// as cross-domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains, or the plugin is isolated; otherwise, false.
        /// </returns>
        public static bool IsCrossAppDomain(
            Interpreter interpreter,
            IPluginData pluginData
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCross(
                interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains, treating an isolated plugin
        /// as cross-domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when an application domain involved in the
        /// comparison is null; when this parameter is itself null, the
        /// comparison proceeds normally.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains, or the plugin is isolated; otherwise, false.
        /// </returns>
        public static bool IsCrossAppDomain(
            Interpreter interpreter,
            IPluginData pluginData,
            bool? resultOnNull
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCross(
                interpreter, pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains, without treating an
        /// isolated plugin as automatically cross-domain.  A non-orphan
        /// interpreter running in a non-default application domain is always
        /// treated as cross-domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains; otherwise, false.
        /// </returns>
        public static bool IsCrossAppDomainNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCrossNoIsolated(
                interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains, without treating an
        /// isolated plugin as automatically cross-domain.  A non-orphan
        /// interpreter running in a non-default application domain is always
        /// treated as cross-domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin to test; this parameter may be null.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when an application domain involved in the
        /// comparison is null; when this parameter is itself null, the
        /// comparison proceeds normally.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains; otherwise, false.
        /// </returns>
        public static bool IsCrossAppDomainNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData,
            bool? resultOnNull
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCrossNoIsolated(
                interpreter, pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the application domain associated
        /// with the specified interpreter is the same as the current
        /// application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter is in the current application domain;
        /// otherwise, false.
        /// </returns>
        public static bool IsSameAppDomain(
            Interpreter interpreter
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsSame(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two plugins reside in the same
        /// application domain, comparing them by application domain identifier.
        /// </summary>
        /// <param name="pluginData1">
        /// The first plugin to compare; this parameter may be null.
        /// </param>
        /// <param name="pluginData2">
        /// The second plugin to compare; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if both plugins are in the same application domain; otherwise,
        /// false.
        /// </returns>
        public static bool IsSameAppDomain(
            IPluginData pluginData1,
            IPluginData pluginData2
            )
        {
            return AppDomainOps.IsSame(pluginData1, pluginData2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the directory containing the current on-disk
        /// location of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose location directory is needed; this parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The directory of the assembly location, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetCurrentPath(
            Assembly assembly
            )
        {
            return AssemblyOps.GetCurrentPath(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the original local file path derived from the code
        /// base of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose original local path is needed; this parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The original local file path, or null if it cannot be determined.
        /// </returns>
        public static string GetOriginalLocalPath(
            Assembly assembly
            )
        {
            return AssemblyOps.GetOriginalLocalPath(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the original local file path derived from the code
        /// base of the specified assembly name.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose original local path is needed; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The original local file path, or null if it cannot be determined.
        /// </returns>
        public static string GetOriginalLocalPath(
            AssemblyName assemblyName
            )
        {
            return AssemblyOps.GetOriginalLocalPath(assemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the current application
        /// domain.
        /// </summary>
        /// <returns>
        /// The identifier of the current application domain.
        /// </returns>
        public static long GetCurrentAppDomainId()
        {
            return AppDomainOps.GetCurrentId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the current (this) process.
        /// </summary>
        /// <returns>
        /// The identifier of the current process, or zero if it cannot be
        /// obtained.
        /// </returns>
        public static long GetCurrentProcessId()
        {
            return ProcessOps.GetId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the main module file name of the current (this)
        /// process.
        /// </summary>
        /// <returns>
        /// The main module file name of the current process, or null if it
        /// cannot be obtained.
        /// </returns>
        public static string GetCurrentProcessFileName()
        {
            return ProcessOps.GetFileName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the current thread.
        /// </summary>
        /// <returns>
        /// The identifier of the current thread.
        /// </returns>
        public static long GetCurrentThreadId()
        {
            return GlobalState.GetCurrentThreadId(); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the parent of the current
        /// (this) process, when native support is available.
        /// </summary>
        /// <returns>
        /// The identifier of the parent process, or zero if it cannot be
        /// obtained.
        /// </returns>
        public static long GetParentProcessId()
        {
            return ProcessOps.GetParentId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes one of the methods represented by the specified
        /// list of delegates, optionally processing leading options and
        /// converting the supplied arguments as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="delegates">
        /// The list of delegates representing the candidate methods.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to be processed and passed to the invoked
        /// method.
        /// </param>
        /// <param name="allowOptions">
        /// Non-zero if leading options are permitted within the argument list.
        /// </param>
        /// <param name="nameCount">
        /// The number of leading arguments that make up the method name.
        /// </param>
        /// <param name="nameIndex">
        /// The argument index at which the method name begins.
        /// </param>
        /// <param name="delegate">
        /// Upon success, this parameter receives the delegate that was
        /// actually invoked.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result of the invocation;
        /// upon failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode InvokeDelegate(
            Interpreter interpreter,
            DelegateList delegates,
            ArgumentList arguments,
            bool allowOptions,
            int nameCount,
            int nameIndex,
            ref Delegate @delegate,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ObjectOps.InvokeDelegate(
                interpreter, delegates, arguments, allowOptions,
                nameCount, nameIndex, ref @delegate, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// This method records the specified prompt text as the most recently
        /// written prompt and then writes it to the console.
        /// </summary>
        /// <param name="value">
        /// The prompt text to be recorded and written to the console; this
        /// parameter may be null.
        /// </param>
        public static void WritePromptViaConsole(
            string value
            )
        {
            ConsoleOps.WritePrompt(value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// This method writes the supplied list of compile-time options to the
        /// interactive host in a right-aligned, columnar layout.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host the options are written to; this parameter may
        /// be null.
        /// </param>
        /// <param name="options">
        /// The options to write; this parameter may be null.
        /// </param>
        /// <param name="perLine">
        /// The number of options to emit per line, or a non-positive value to
        /// use a computed default.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing line terminator after the options.
        /// </param>
        /// <returns>
        /// True if any options were written, false if none were written, or
        /// null if the host or the options sequence is null.
        /// </returns>
        public static bool? WriteOptions(
            IInteractiveHost interactiveHost, /* in */
            IEnumerable<string> options,      /* in */
            int perLine,                      /* in */
            bool newLine                      /* in */
            )
        {
            return HelpOps.WriteOptions(
                interactiveHost, options, perLine, newLine);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to a channel by locating and
        /// executing the appropriate command via the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="commandName">
        /// The name of the command to use for writing, or null to use the
        /// default [puts] command.
        /// </param>
        /// <param name="channelId">
        /// The identifier of the channel to write to, or null to use the
        /// standard output channel.
        /// </param>
        /// <param name="value">
        /// The value to be written; this parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the result of the write;
        /// upon failure, it receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode WriteViaIExecute(
            Interpreter interpreter,
            string commandName, /* NOTE: Almost always null, for [puts]. */
            string channelId,   /* NOTE: Almost always null, for "stdout". */
            string value,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.WriteViaIExecute(
                interpreter, commandName, channelId, value, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified date and time using one of the
        /// trace date and time formats.
        /// </summary>
        /// <param name="value">
        /// The date and time value to format.
        /// </param>
        /// <param name="interactive">
        /// Non-zero to use the interactive trace format; otherwise, the
        /// standard trace format is used.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
        public static string FormatTraceDateTime(
            DateTime value,
            bool interactive
            )
        {
            return FormatOps.TraceDateTime(value, interactive);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified value for inclusion in script log
        /// output, substituting display placeholders for null, empty, or
        /// whitespace-only values.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to collapse internal whitespace before formatting.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the result with an ellipsis when it is too
        /// long.
        /// </param>
        /// <param name="value">
        /// The value to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The resulting display string, which may be a placeholder such as
        /// &lt;null&gt;, &lt;empty&gt;, or &lt;space&gt; for the corresponding
        /// values.
        /// </returns>
        public static string FormatScriptForLog(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return FormatOps.ScriptForLog(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified exception as a string for use in
        /// trace output, including the current stack trace.
        /// </summary>
        /// <param name="exception">
        /// The exception to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted exception string, or null if it is not available.
        /// </returns>
        [Obsolete()]
        public static string FormatTraceException(
            Exception exception
            )
        {
            return FormatOps.TraceException(exception,
                TracePriority.ForException | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the name of the specified
        /// plugin, optionally wrapping it.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin whose name is needed; this parameter may be null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting plugin name.
        /// </param>
        /// <returns>
        /// The plugin name, optionally wrapped, or a placeholder such as
        /// &lt;unavailable&gt; when the name cannot be determined.
        /// </returns>
        public static string FormatPluginName(
            IPluginData pluginData,
            bool wrap
            )
        {
            return FormatOps.PluginName(pluginData, wrap);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a collection of defined constant names into a
        /// single space-separated string.
        /// </summary>
        /// <param name="collection">
        /// The collection of constant names to format; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The space-separated constant names, or a placeholder such as
        /// &lt;nullList&gt; or &lt;emptyList&gt; for a null or empty
        /// collection.
        /// </returns>
        public static string FormatDefineConstants(
            IEnumerable<string> collection
            )
        {
            return FormatOps.DefineConstants(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a formatted command log entry describing a
        /// command invocation and, optionally, its result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin associated with the command; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments of the command invocation; this parameter may be
        /// null.
        /// </param>
        /// <param name="returnCode">
        /// The return code of the command invocation; this parameter may be
        /// null.
        /// </param>
        /// <param name="result">
        /// The result of the command invocation; this parameter may be null.
        /// </param>
        /// <param name="indentSpaces">
        /// The number of spaces by which to indent the entry.
        /// </param>
        /// <param name="allowNewLines">
        /// Non-zero to permit new lines within the formatted entry.
        /// </param>
        /// <param name="entryId">
        /// On input, the entry identifier to use; when zero, a new identifier
        /// is allocated and returned via this parameter.
        /// </param>
        /// <returns>
        /// The formatted command log entry string, or null if it is not
        /// available.
        /// </returns>
        public static string FormatCommandLogEntry(
            Interpreter interpreter,
            IPluginData pluginData,
            IClientData clientData,
            ArgumentList arguments,
            ReturnCode? returnCode,
            Result result,
            int indentSpaces,
            bool allowNewLines,
            ref long entryId
            )
        {
            return FormatOps.CommandLogEntry(
                interpreter, pluginData, clientData, arguments,
                returnCode, result, indentSpaces, allowNewLines,
                ref entryId);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the specified string value,
        /// substituting placeholders for empty or null values.
        /// </summary>
        /// <param name="value">
        /// The string value to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The value itself, or a placeholder such as &lt;empty&gt; or
        /// &lt;null&gt; when the value is empty or null.
        /// </returns>
        public static string FormatDisplayString(
            string value
            )
        {
            return FormatOps.DisplayString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the specified value unchanged, or a display
        /// placeholder when it is null.
        /// </summary>
        /// <param name="value">
        /// The value to inspect; this parameter may be null.
        /// </param>
        /// <returns>
        /// The value itself when it is non-null; otherwise, the literal
        /// placeholder &lt;null&gt;.
        /// </returns>
        public static object FormatMaybeNull(
            object value
            )
        {
            return FormatOps.MaybeNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the specified value unchanged, or a display
        /// placeholder when it is null or an empty string.
        /// </summary>
        /// <param name="value">
        /// The value to inspect; this parameter may be null.
        /// </param>
        /// <returns>
        /// The value itself when it is non-null and not an empty string;
        /// otherwise, the placeholder &lt;null&gt; or &lt;empty&gt;.
        /// </returns>
        public static object FormatMaybeNullOrEmpty(
            object value
            )
        {
            return FormatOps.MaybeNullOrEmpty(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the type, hash code, and wrapped value of the
        /// specified object into a single descriptive string.
        /// </summary>
        /// <param name="value">
        /// The object to describe; this parameter may be null.
        /// </param>
        /// <returns>
        /// A descriptive string containing the object type, hash code, and its
        /// wrapped value; the value portion is shown as &lt;null&gt; when the
        /// object is null.
        /// </returns>
        public static string FormatTypeAndWrapOrNull(
            object value
            )
        {
            return FormatOps.TypeAndWrapOrNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified collection
        /// of strings, wrapping the value when it is not null.
        /// </summary>
        /// <param name="value">
        /// The collection of strings to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped, space-delimited collection, or the placeholder
        /// &lt;null&gt; when the collection is null.
        /// </returns>
        public static string FormatWrapOrNull(
            IEnumerable<string> value
            )
        {
            return FormatOps.WrapOrNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified object,
        /// wrapping the value when it is not null.
        /// </summary>
        /// <param name="value">
        /// The object to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped string form of the object, or the placeholder
        /// &lt;null&gt; when the object is null.
        /// </returns>
        public static string FormatWrapOrNull(
            object value
            )
        {
            return FormatOps.WrapOrNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified collection
        /// of strings, wrapping the value when it is not null, with optional
        /// whitespace normalization and ellipsis truncation.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to collapse internal whitespace before wrapping.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the result with an ellipsis when it is too
        /// long.
        /// </param>
        /// <param name="value">
        /// The collection of strings to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped, space-delimited collection, or the placeholder
        /// &lt;null&gt; when the collection is null.
        /// </returns>
        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            IEnumerable<string> value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified object,
        /// wrapping the value when it is not null, with optional whitespace
        /// normalization and ellipsis truncation.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to collapse internal whitespace before wrapping.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the result with an ellipsis when it is too
        /// long.
        /// </param>
        /// <param name="value">
        /// The object to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped string form of the object, or the placeholder
        /// &lt;null&gt; when the object is null.
        /// </returns>
        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified collection
        /// of strings, wrapping the value when it is not null, with optional
        /// whitespace normalization, ellipsis truncation, and placeholder
        /// substitution.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to collapse internal whitespace before wrapping.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the result with an ellipsis when it is too
        /// long.
        /// </param>
        /// <param name="display">
        /// Non-zero to substitute the placeholders &lt;null&gt; or
        /// &lt;empty&gt; for null or empty values instead of wrapping them.
        /// </param>
        /// <param name="value">
        /// The collection of strings to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped, space-delimited collection, or the placeholder
        /// &lt;null&gt; when the collection is null.
        /// </returns>
        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            IEnumerable<string> value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, display, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified object,
        /// wrapping the value when it is not null, with optional whitespace
        /// normalization, ellipsis truncation, and placeholder substitution.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to collapse internal whitespace before wrapping.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the result with an ellipsis when it is too
        /// long.
        /// </param>
        /// <param name="display">
        /// Non-zero to substitute the placeholders &lt;null&gt; or
        /// &lt;empty&gt; for null or empty values instead of wrapping them.
        /// </param>
        /// <param name="value">
        /// The object to format; this parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped string form of the object, or the placeholder
        /// &lt;null&gt; when the object is null.
        /// </returns>
        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            object value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, display, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two byte arrays contain the same
        /// sequence of bytes over their full lengths.
        /// </summary>
        /// <param name="array1">
        /// The first byte array to compare; this parameter may be null.
        /// </param>
        /// <param name="array2">
        /// The second byte array to compare; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if both arrays have equal length and identical bytes;
        /// otherwise, false.
        /// </returns>
        public static bool ArrayEquals(
            byte[] array1,
            byte[] array2
            )
        {
            return ArrayOps.Equals(array1, array2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two byte arrays contain the same
        /// sequence of bytes over the specified number of leading bytes.
        /// </summary>
        /// <param name="array1">
        /// The first byte array to compare; this parameter may be null.
        /// </param>
        /// <param name="array2">
        /// The second byte array to compare; this parameter may be null.
        /// </param>
        /// <param name="length">
        /// The number of leading bytes to compare, or an invalid length to
        /// compare the arrays over their full lengths.
        /// </param>
        /// <returns>
        /// True if the arrays are equal over the specified bytes; otherwise,
        /// false.
        /// </returns>
        public static bool ArrayEquals(
            byte[] array1,
            byte[] array2,
            int length
            )
        {
            return ArrayOps.Equals(array1, array2, length);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two byte arrays contain the same
        /// sequence of bytes over the specified range.
        /// </summary>
        /// <param name="array1">
        /// The first byte array to compare; this parameter may be null.
        /// </param>
        /// <param name="array2">
        /// The second byte array to compare; this parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first byte to compare in each array.
        /// </param>
        /// <param name="length">
        /// The number of bytes to compare, or an invalid length to compare
        /// through the end of the arrays.
        /// </param>
        /// <returns>
        /// True if the arrays are equal over the specified range; otherwise,
        /// false.
        /// </returns>
        public static bool ArrayEquals(
            byte[] array1,
            byte[] array2,
            int startIndex,
            int length
            )
        {
            return ArrayOps.Equals(array1, array2, startIndex, length);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the index of the first occurrence of one byte
        /// array within another byte array.
        /// </summary>
        /// <param name="array1">
        /// The byte array to search within; this parameter may be null.
        /// </param>
        /// <param name="array2">
        /// The contiguous sequence of bytes to search for; this parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref
        /// name="array2" /> within <paramref name="array1" />, or a negative
        /// value if it is not found.
        /// </returns>
        public static int ArrayIndexOf(
            byte[] array1,
            byte[] array2
            )
        {
            return ArrayOps.IndexOf(array1, array2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally enables or disables the trace limiting
        /// subsystem by adjusting its disable count, or simply queries it when
        /// no adjustment is requested.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the subsystem (decrementing its disable count),
        /// zero to disable it (incrementing its disable count), or null to
        /// leave it unchanged and only query the current state.
        /// </param>
        /// <returns>
        /// True if the subsystem is disabled after the operation; otherwise,
        /// false.
        /// </returns>
        public static bool MaybeAdjustTraceLimits(
            bool? enable
            )
        {
            return TraceLimits.MaybeAdjustEnabled(enable);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default trace priority, which is used for
        /// trace messages that do not specify one explicitly.
        /// </summary>
        /// <returns>
        /// The default <see cref="TracePriority" /> value.
        /// </returns>
        public static TracePriority GetTracePriority()
        {
            return TraceOps.GetTracePriority();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes the specified trace client data, applying its
        /// requested changes to the tracing subsystem (for example, resetting
        /// state and configuring listeners, categories, priorities, and
        /// format).
        /// </summary>
        /// <param name="traceClientData">
        /// The trace client data describing the requested changes; this
        /// parameter may not be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ProcessTraceClientData(
            TraceClientData traceClientData,
            ref Result result
            )
        {
            return TraceOps.ProcessClientData(traceClientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// This method emits the specified message to the native debug output,
        /// appending a line terminator.
        /// </summary>
        /// <param name="message">
        /// The message to emit; this parameter may be null.
        /// </param>
        public static void OutputDebugString(
            string message
            )
        {
            DebugOps.Output(message, DebugPriority.FromExternal);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified message to the native debug output
        /// at the specified debug priority, appending a line terminator.
        /// </summary>
        /// <param name="message">
        /// The message to emit; this parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The debug priority associated with the message, or null to use the
        /// default external priority.
        /// </param>
        public static void OutputDebugString(
            string message,
            DebugPriority? priority
            )
        {
            DebugPriority localPriority = DebugPriority.FromExternal;

            if (priority != null)
                localPriority |= (DebugPriority)priority;

            DebugOps.Output(message, localPriority);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes the specified trace message at the
        /// current default trace priority, flagged as originating from
        /// outside the library.  This overload is obsolete because it
        /// does not accept an explicit trace priority.
        /// </summary>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        [Obsolete()] /* NOTE: Lack of priority. */
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            string message,
            string category
            )
        {
            TraceOps.DebugTraceAlways(message, category,
                TraceOps.GetTracePriority() | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes the specified trace message, attributed
        /// to the specified thread, at the current default trace priority,
        /// flagged as originating from outside the library.  This overload
        /// is obsolete because it does not accept an explicit trace
        /// priority.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to attribute the trace message to.
        /// </param>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        [Obsolete()] /* NOTE: Lack of priority. */
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            long threadId,
            string message,
            string category
            )
        {
            TraceOps.DebugTraceAlways(threadId, message, category,
                TraceOps.GetTracePriority() | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes a trace message describing the named
        /// method together with a set of alternating name/value parameter
        /// pairs, using the specified trace priority and flagged as
        /// originating from outside the library.
        /// </summary>
        /// <param name="methodName">
        /// The name of the method that originated the trace message; this
        /// parameter may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to write; this parameter may be null.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message; this
        /// parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate long parameter values with an ellipsis.
        /// </param>
        /// <param name="parameters">
        /// The array of alternating parameter names and values to include
        /// in the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            string methodName,
            string message,
            string category,
            TracePriority priority,
            bool ellipsis,
            params object[] parameters
            )
        {
            TraceOps.DebugTraceAlways(methodName, message, category,
                priority | TracePriority.External, 1, ellipsis, parameters);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes the specified trace message, associated
        /// with the specified interpreter, using the specified trace
        /// priority and flagged as originating from outside the library.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message; this
        /// parameter may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of additional call frames to skip when capturing a
        /// stack trace.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            Interpreter interpreter,
            string message,
            string category,
            TracePriority priority,
            int skipFrames
            )
        {
            TraceOps.DebugTraceAlways(interpreter, message, category,
                priority | TracePriority.External, skipFrames);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes the specified trace message using the
        /// specified trace priority and flagged as originating from
        /// outside the library.
        /// </summary>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            string message,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTraceAlways(message, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes a trace message describing the specified
        /// exception, using the specified trace priority and flagged as
        /// originating from outside the library.
        /// </summary>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            Exception exception,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTraceAlways(exception, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes a trace message describing the specified
        /// exception, associated with the specified interpreter, using the
        /// specified trace priority and flagged as originating from
        /// outside the library.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message; this
        /// parameter may be null.
        /// </param>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of additional call frames to skip when capturing a
        /// stack trace.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            Interpreter interpreter,
            Exception exception,
            string category,
            TracePriority priority,
            int skipFrames
            )
        {
            TraceOps.DebugTraceAlways(interpreter, exception, category,
                priority | TracePriority.External, skipFrames);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unconditionally writes the specified trace message, attributed
        /// to the specified thread, using the specified trace priority and
        /// flagged as originating from outside the library.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to attribute the trace message to.
        /// </param>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            long threadId,
            string message,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTraceAlways(threadId, message, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Dumps the recorded complaints, either to a file or via the
        /// failsafe output mechanism, optionally filtering by interpreter
        /// and optionally clearing the complaints that are written.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose complaints should be dumped, or null to
        /// dump the complaints from all interpreters; this parameter may
        /// be null.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when writing complaints to a
        /// file, or null to use the default encoding; this parameter may
        /// be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to write the complaints to, or null to use
        /// the failsafe output mechanism; this parameter may be null.
        /// </param>
        /// <param name="message">
        /// An optional message to write before the complaints; this
        /// parameter may be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to remove each complaint that is successfully written.
        /// </param>
        /// <returns>
        /// The number of complaints written, or an invalid count if the
        /// operation could not be performed.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int DumpComplaints(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            string message,
            bool clear
            )
        {
            return DebugOps.DumpComplaints(
                interpreter, encoding, fileName, message, clear);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Records a complaint for the specified interpreter, capturing a
        /// stack trace and method name as appropriate and routing the
        /// complaint to the configured output sinks.  This method never
        /// throws an exception.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the complaint; this parameter
        /// may be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the complaint.
        /// </param>
        /// <param name="result">
        /// The result (message) associated with the complaint; this
        /// parameter may be null.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Complain(
            Interpreter interpreter,
            ReturnCode code,
            Result result
            ) /* SAFE-ON-DISPOSE */
        {
            DebugOps.Complain(interpreter, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes the specified value using the failsafe output mechanism,
        /// always emitting it to the native debug output and optionally
        /// emitting it to the supplied debug host.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to write to, if enabled; this parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="viaHost">
        /// Non-zero to also emit the value to the supplied debug host.
        /// </param>
        public static void WriteWithoutFail(
            IDebugHost debugHost,
            string value,
            bool viaHost
            )
        {
            DebugOps.WriteWithoutFail(debugHost, value, true, viaHost);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Generates the next available global integer identifier,
        /// suitable for use with script-visible entities (e.g. channel
        /// names).
        /// </summary>
        /// <returns>
        /// The next available global integer identifier.
        /// </returns>
        public static long NextId()
        {
            return GlobalState.NextId();
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// Dynamically loads the native library with the specified file
        /// name.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native library to load.
        /// </param>
        /// <param name="lastError">
        /// Upon failure, receives the native error code; otherwise, it is
        /// set to zero.
        /// </param>
        /// <returns>
        /// The native handle of the loaded library, or
        /// <see cref="IntPtr.Zero" /> on failure.
        /// </returns>
        public static IntPtr LoadLibrary(
            string fileName,
            out int lastError
            )
        {
            return NativeOps.LoadLibrary(fileName, out lastError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the native address of the exported function or variable
        /// with the specified name from the specified module.
        /// </summary>
        /// <param name="module">
        /// The native handle of the module to query.
        /// </param>
        /// <param name="name">
        /// The name of the exported function or variable to locate.
        /// </param>
        /// <param name="lastError">
        /// Upon failure, receives the native error code; otherwise, it is
        /// set to zero.
        /// </param>
        /// <returns>
        /// The native address of the exported function or variable, or
        /// <see cref="IntPtr.Zero" /> on failure.
        /// </returns>
        public static IntPtr GetProcAddress(
            IntPtr module,
            string name,
            out int lastError
            )
        {
            return NativeOps.GetProcAddress(module, name, out lastError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unloads the previously loaded native library identified by the
        /// specified module handle.
        /// </summary>
        /// <param name="module">
        /// The native handle of the library to unload.
        /// </param>
        /// <param name="lastError">
        /// Upon failure, receives the native error code; otherwise, it is
        /// set to zero.
        /// </param>
        /// <returns>
        /// True if the library was unloaded; otherwise, false.
        /// </returns>
        public static bool FreeLibrary(
            IntPtr module,
            out int lastError
            )
        {
            return NativeOps.FreeLibrary(module, out lastError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the error message associated with the last native error
        /// for the calling thread, if any.
        /// </summary>
        /// <returns>
        /// The error message associated with the last native error, or
        /// null if there is no last error.
        /// </returns>
        public static string MaybeGetErrorMessage()
        {
            return NativeOps.MaybeGetErrorMessage();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the error message associated with the last native error
        /// for the calling thread.
        /// </summary>
        /// <returns>
        /// The error message associated with the last native error, or
        /// null if it cannot be determined.
        /// </returns>
        public static string GetErrorMessage()
        {
            return NativeOps.GetErrorMessage();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the error message associated with the specified native
        /// error code.
        /// </summary>
        /// <param name="error">
        /// The native error code to translate into a message.
        /// </param>
        /// <returns>
        /// The error message associated with the specified error code, or
        /// null if it cannot be determined.
        /// </returns>
        public static string GetErrorMessage(
            int error
            )
        {
            return NativeOps.GetErrorMessage(error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Opens the manifest resource stream with the specified name from
        /// the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly from which the manifest resource stream should be
        /// opened.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource stream to open.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The opened stream, or null if the stream could not be opened.
        /// </returns>
        public static Stream GetStream(
            Assembly assembly,
            string name,
            ref Result error
            )
        {
            return _RuntimeOps.GetStream(assembly, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Searches for a manifest resource stream with the specified
        /// name, first within the specified assembly and then within all
        /// assemblies currently loaded into the application domain.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to search first; this parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource stream to search for.
        /// </param>
        /// <param name="verbose">
        /// Non-zero if errors encountered while searching individual
        /// assemblies should be accumulated into the error message.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The assembly that contains the named manifest resource stream,
        /// or null if no such assembly was found.
        /// </returns>
        public static Assembly FindStream(
            Assembly assembly,
            string name,
            bool verbose,
            ref Result error
            )
        {
            return _RuntimeOps.FindStream(
                assembly, name, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Opens a stream for the resource with the specified name using
        /// the specified resource manager and culture.
        /// </summary>
        /// <param name="resourceManager">
        /// The resource manager from which the stream should be opened.
        /// </param>
        /// <param name="name">
        /// The name of the resource to open.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource should be opened; this
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The opened stream, or null if the stream could not be opened.
        /// </returns>
        public static Stream GetStream(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return _RuntimeOps.GetStream(
                resourceManager, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Retrieves the string value of the resource with the specified
        /// name using the specified resource manager and culture.
        /// </summary>
        /// <param name="resourceManager">
        /// The resource manager from which the string value should be
        /// retrieved.
        /// </param>
        /// <param name="name">
        /// The name of the resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource should be retrieved; this
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The string value of the resource, or null if it could not be
        /// retrieved.
        /// </returns>
        public static string GetString(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return _RuntimeOps.GetString(
                resourceManager, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Retrieves the string value of the resource with the specified
        /// name, first using the specified resource manager and then using
        /// the manifest resources of the specified plugin assembly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the request; this
        /// parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin whose assembly manifest resources should be queried.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager from which the string value should first
        /// be retrieved.
        /// </param>
        /// <param name="name">
        /// The name of the resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource should be retrieved; this
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The string value of the resource, or null if it could not be
        /// retrieved.
        /// </returns>
        public static string GetAnyString(
            Interpreter interpreter,
            IPlugin plugin,
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return _RuntimeOps.GetAnyString(
                interpreter, plugin, resourceManager, name, cultureInfo,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Retrieves the names of all resources available via the
        /// specified resource manager and the resource manager associated
        /// with the specified plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin whose associated resource manager should be queried;
        /// this parameter may be null.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager that should be queried; this parameter may
        /// be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource names should be retrieved;
        /// this parameter may be null.
        /// </param>
        /// <param name="list">
        /// Supplies the list on input and, upon success, receives the
        /// retrieved resource names; the list is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode GetResourceNames(
            IPluginData pluginData,
            ResourceManager resourceManager,
            CultureInfo cultureInfo,
            ref StringList list,
            ref Result error
            )
        {
            return _RuntimeOps.GetResourceNames(
                pluginData, resourceManager, cultureInfo, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the Tcl patch level string that this library reports for
        /// Tcl compatibility purposes.
        /// </summary>
        /// <returns>
        /// The Tcl patch level string, or null if it is not available.
        /// </returns>
        public static string GetTclPatchLevel()
        {
            return TclVars.Package.PatchLevelValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the Tcl version string that this library reports for Tcl
        /// compatibility purposes.
        /// </summary>
        /// <returns>
        /// The Tcl version string, or null if it is not available.
        /// </returns>
        public static string GetTclVersion()
        {
            return TclVars.Package.VersionValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified object is a transparent proxy
        /// (i.e. a proxy to an object in another application domain).
        /// </summary>
        /// <param name="proxy">
        /// The object to check.
        /// </param>
        /// <returns>
        /// True if the object is a transparent proxy; otherwise, false.
        /// </returns>
        public static bool IsTransparentProxy(
            object proxy
            )
        {
            return AppDomainOps.IsTransparentProxy(proxy);
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        /// <summary>
        /// Determines whether the specified plugin is isolated (i.e.
        /// loaded into a separate application domain), based on its flags.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin to check.
        /// </param>
        /// <returns>
        /// True if the plugin is isolated; otherwise, false.
        /// </returns>
        [Obsolete()]
        public static bool IsPluginIsolated(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsIsolated(pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Fixes up the enumerated-type options that refer to types
        /// defined within an isolated plugin assembly, converting them so
        /// that they can be used from the primary application domain.  For
        /// flags enumerations a placeholder type is substituted; for
        /// ordinary enumerations the option is converted to its integral
        /// type.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data whose options should be fixed up.
        /// </param>
        /// <param name="options">
        /// The option dictionary to fix up.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat missing plugin data or options as an error;
        /// zero to treat them as a successful no-op.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode FixupOptions(
            IPluginData pluginData,
            OptionDictionary options,
            bool strict,
            ref Result error
            )
        {
            return AppDomainOps.FixupOptions(
                pluginData, options, strict, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Temporarily prepends the specified path to the auto-path
        /// environment variable and refreshes the auto-path list, saving
        /// the previous value for later restoration.
        /// </summary>
        /// <param name="path">
        /// The path to prepend to the auto-path; this parameter may be
        /// null.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output during the
        /// refresh.
        /// </param>
        /// <param name="savedLibPath">
        /// Upon return, receives the previous value of the auto-path
        /// environment variable, for later restoration.
        /// </param>
        public static void BeginWithAutoPath(
            string path,
            bool verbose,
            ref string savedLibPath
            )
        {
            GlobalState.BeginWithAutoPath(path, verbose, ref savedLibPath);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Restores the previously saved auto-path environment variable
        /// value and refreshes the auto-path list.
        /// </summary>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output during the
        /// refresh.
        /// </param>
        /// <param name="savedLibPath">
        /// Supplies the previously saved auto-path environment variable
        /// value to restore on input, and is reset to null upon return.
        /// </param>
        public static void EndWithAutoPath(
            bool verbose,
            ref string savedLibPath
            )
        {
            GlobalState.EndWithAutoPath(verbose, ref savedLibPath);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Refreshes the cached entry assembly information, using the
        /// specified assembly or the automatically detected entry
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to use as the entry assembly, or null to detect it
        /// automatically; this parameter may be null.
        /// </param>
        /// <returns>
        /// Always returns true.
        /// </returns>
        public static bool RefreshEntryAssembly(
            Assembly assembly
            )
        {
            return GlobalState.RefreshEntryAssembly(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method needs to be public because external applications
        //       and plugins may set the environment variables we care about;
        //       however, there is no other way to notify this library about
        //       those changes (other than this method, that is).
        //
        /// <summary>
        /// Re-queries all auto-path related environment variables, also
        /// resetting the shared auto-path list so that it is reinitialized
        /// on the next request.
        /// </summary>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output.
        /// </param>
        public static void RefreshAutoPathList(
            bool verbose
            )
        {
            GlobalState.RefreshAutoPathList(verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to combine a base <see cref="Uri" /> with a relative
        /// <see cref="Uri" /> string, producing a new absolute
        /// <see cref="Uri" /> from the selected components according to the
        /// specified flags and formatting.
        /// </summary>
        /// <param name="baseUri">
        /// The absolute base <see cref="Uri" /> to combine; this cannot be
        /// null and must be absolute.
        /// </param>
        /// <param name="relativeUri">
        /// The relative <see cref="Uri" /> string to combine with the base
        /// <see cref="Uri" />; if null or empty, the base
        /// <see cref="Uri" /> is returned unchanged.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when combining query name/value
        /// pairs, used only when compiled with web support enabled; this
        /// parameter may be null.
        /// </param>
        /// <param name="components">
        /// The <see cref="UriComponents" /> to include from the source
        /// URIs when building the combined <see cref="Uri" />.
        /// </param>
        /// <param name="format">
        /// The <see cref="UriFormat" /> used when extracting components,
        /// which may be replaced with the default format unless the
        /// appropriate flag is set.
        /// </param>
        /// <param name="flags">
        /// The <see cref="UriFlags" /> that control how the URIs are
        /// combined (e.g. path separator handling, normalization, and any
        /// scheme constraints).
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The combined absolute <see cref="Uri" />, or null if the URIs
        /// could not be combined.
        /// </returns>
        public static Uri TryCombineUris(
            Uri baseUri,
            string relativeUri,
            Encoding encoding,
            UriComponents components,
            UriFormat format,
            UriFlags flags,
            ref Result error
            )
        {
            return PathOps.TryCombineUris(
                baseUri, relativeUri, encoding, components, format,
                flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// Returns the web client tag value by checking the thread,
        /// process, parent process, and global context environment
        /// variables, in that order, returning the first non-empty value
        /// found.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <returns>
        /// The first non-empty web client tag value found, or null if none
        /// was found.
        /// </returns>
        public static string GetWebClientTagEnvVarValue(
            Interpreter interpreter
            )
        {
            return WebOps.GetTagEnvVarValue(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the web client tag value stored in the environment
        /// variable for the specified context identifier type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which environment
        /// variable to query.
        /// </param>
        /// <returns>
        /// The web client tag value, or null if there is none.
        /// </returns>
        public static string GetWebClientTagEnvVarValue(
            Interpreter interpreter,
            ContextIdType type
            )
        {
            return WebOps.GetTagEnvVarValue(interpreter, type);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the environment variable used to store the web client tag
        /// for the specified context identifier type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which environment
        /// variable to set.
        /// </param>
        /// <param name="tag">
        /// The web client tag value to store, or null to clear it; this
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the environment variable was set; otherwise, false.
        /// </returns>
        public static bool SetWebClientTagEnvVarValue(
            Interpreter interpreter,
            ContextIdType type,
            string tag
            )
        {
            return WebOps.SetTagEnvVarValue(interpreter, type, tag);
        }

        ///////////////////////////////////////////////////////////////////////

#if WEB
        /// <summary>
        /// Attempts to set the web client tag environment variable for the
        /// specified context identifier type from the tag request header
        /// of the specified HTTP request, optionally unsetting it when no
        /// tag is available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="request">
        /// The HTTP request whose tag header is used; when null, the
        /// variable may be unset.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which environment
        /// variable to set or unset.
        /// </param>
        /// <returns>
        /// True if the environment variable was set or unset; otherwise,
        /// false.
        /// </returns>
        public static bool TrySetWebClientTagEnvVarValue(
            Interpreter interpreter,
            HttpRequest request,
            ContextIdType type
            )
        {
            return WebOps.TrySetTagEnvVarValue(
                interpreter, request, type);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Downloads the data at the specified <see cref="Uri" />,
        /// retrying the request and consulting any configured web error
        /// callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data associated with this request; this
        /// parameter may be null.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the data to download.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to
        /// use the configured default; this parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the
        /// configured default; this parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged; this
        /// parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// Supplies a byte buffer on input and, upon success, receives the
        /// downloaded data.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode DownloadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            int? maximumRetries,
            int? timeout,
            bool? trusted,
            ref byte[] bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return WebOps.DownloadData(
                interpreter, clientData, uri, maximumRetries, timeout,
                trusted, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Downloads the resource at the specified <see cref="Uri" /> to a
        /// local file, retrying the request and consulting any configured
        /// web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data associated with this request; this
        /// parameter may be null.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the resource to download.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to write the downloaded resource to.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to
        /// use the configured default; this parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the
        /// configured default; this parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged; this
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode DownloadFile(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string fileName,
            int? maximumRetries,
            int? timeout,
            bool? trusted,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return WebOps.DownloadFile(
                interpreter, clientData, uri, fileName, maximumRetries,
                timeout, trusted, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Uploads the specified raw data to the specified
        /// <see cref="Uri" />, retrying the request and consulting any
        /// configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data associated with this request; this
        /// parameter may be null.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the data to.
        /// </param>
        /// <param name="method">
        /// The HTTP method (verb) used for the upload.
        /// </param>
        /// <param name="rawData">
        /// The raw data to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to
        /// use the configured default; this parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the
        /// configured default; this parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged; this
        /// parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// Supplies a byte buffer on input and, upon success, receives the
        /// response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode UploadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            byte[] rawData,
            int? maximumRetries,
            int? timeout,
            bool? trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            return WebOps.UploadData(
                interpreter, clientData, uri, method, rawData,
                maximumRetries, timeout, trusted, ref bytes,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Uploads the specified name/value collection to the specified
        /// <see cref="Uri" />, retrying the request and consulting any
        /// configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data associated with this request; this
        /// parameter may be null.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the values to.
        /// </param>
        /// <param name="method">
        /// The HTTP method (verb) used for the upload.
        /// </param>
        /// <param name="collection">
        /// The name/value collection to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to
        /// use the configured default; this parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the
        /// configured default; this parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged; this
        /// parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// Supplies a byte buffer on input and, upon success, receives the
        /// response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode UploadValues(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            NameValueCollection collection,
            int? maximumRetries,
            int? timeout,
            bool? trusted,
            ref byte[] bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return WebOps.UploadValues(
                interpreter, clientData, uri, method, collection,
                maximumRetries, timeout, trusted, ref bytes,
                ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a command callback that evaluates a script in the
        /// specified interpreter, or returns an existing matching callback
        /// when one is already registered.
        /// </summary>
        /// <param name="marshalFlags">
        /// The flags used to control how arguments and return values are
        /// marshaled for the callback.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used to control the behavior of the callback.
        /// </param>
        /// <param name="objectFlags">
        /// The flags used when creating opaque object handles for the
        /// callback arguments.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// The flags used to control the handling of by-reference (output)
        /// arguments.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that will be used to evaluate the callback
        /// script.
        /// </param>
        /// <param name="clientData">
        /// The caller-specific data to associate with the callback; this
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the callback; when null, the string form of
        /// <paramref name="arguments" /> is used.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments forming the callback script; this
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// The created or fetched callback instance, or null if it could
        /// not be created.
        /// </returns>
        public static ICallback CreateCommandCallback(
            MarshalFlags marshalFlags,
            CallbackFlags callbackFlags,
            ObjectFlags objectFlags,
            ByRefArgumentFlags byRefArgumentFlags,
            Interpreter interpreter,
            IClientData clientData,
            string name,
            StringList arguments,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return CommandCallback.Create(
                marshalFlags, callbackFlags | CallbackFlags.External,
                objectFlags, byRefArgumentFlags, interpreter,
                clientData, name, arguments, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// Creates a database connection from the specified connection
        /// parameters, trying each configured connection type in turn and
        /// reporting which one succeeded.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used during type resolution; this
        /// parameter may be null.
        /// </param>
        /// <param name="dbConnectionParameters">
        /// The database connection parameters describing the connection
        /// type(s), public key token(s), connection string, and assembly
        /// or type information to use; this cannot be null.
        /// </param>
        /// <param name="connection">
        /// Supplies the connection on input and, upon success, receives
        /// the created database connection.
        /// </param>
        /// <param name="dbConnectionType">
        /// Supplies a value on input and, upon success, receives the
        /// connection type that was successfully created.
        /// </param>
        /// <param name="publicKeyToken">
        /// Supplies a value on input and, upon success, receives the
        /// public key token associated with the connection type that was
        /// successfully created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,
            IDbConnectionParameters dbConnectionParameters,
            ref IDbConnection connection,
            ref DbConnectionType dbConnectionType,
            ref byte[] publicKeyToken,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            if (dbConnectionParameters == null)
            {
                error = "invalid database connection parameters";
                return ReturnCode.Error;
            }

            return DataOps.CreateDbConnection(interpreter,
                dbConnectionParameters.DbConnectionType1,
                dbConnectionParameters.DbConnectionType2,
                dbConnectionParameters.PublicKeyToken1,
                dbConnectionParameters.PublicKeyToken2,
                dbConnectionParameters.ConnectionString,
                dbConnectionParameters.AssemblyFileName,
                dbConnectionParameters.TypeFullName,
                dbConnectionParameters.TypeName,
                dbConnectionParameters.Type,
                dbConnectionParameters.ValueFlags,
                ref connection, ref dbConnectionType,
                ref publicKeyToken, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// Determines whether a debugger is attached to the current
        /// process.  This is only supported on the Windows operating
        /// system.
        /// </summary>
        /// <returns>
        /// True if a debugger is present; otherwise, false.  Also returns
        /// false on non-Windows platforms or if the check fails.
        /// </returns>
        public static bool IsDebuggerPresent()
        {
            try
            {
                if (PlatformOps.IsWindowsOperatingSystem())
                    return NativeOps.SafeNativeMethods.IsDebuggerPresent();
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Utility).Name, TracePriority.External |
                    TracePriority.NativeError);
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the host creation flags from the specified base flags
        /// and option toggles.
        /// </summary>
        /// <param name="hostCreateFlags">
        /// The base host creation flags supplied by the caller.
        /// </param>
        /// <param name="useAttach">
        /// Non-zero to attach to an existing console.
        /// </param>
        /// <param name="useForce">
        /// Non-zero to force console creation.
        /// </param>
        /// <param name="noColor">
        /// Non-zero to disable host color support.
        /// </param>
        /// <param name="noTitle">
        /// Non-zero to disable setting the host title.
        /// </param>
        /// <param name="noIcon">
        /// Non-zero to disable setting the host icon.
        /// </param>
        /// <param name="noProfile">
        /// Non-zero to disable loading the host profile.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to disable cancellation support.
        /// </param>
        /// <returns>
        /// The resulting host creation flags.
        /// </returns>
        public static HostCreateFlags GetHostCreateFlags(
            HostCreateFlags hostCreateFlags,
            bool useAttach,
            bool useForce,
            bool noColor,
            bool noTitle,
            bool noIcon,
            bool noProfile,
            bool noCancel
            )
        {
            return HostOps.GetCreateFlags(
                hostCreateFlags, useAttach, useForce, noColor,
                noTitle, noIcon, noProfile, noCancel);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new host of the specified type that copies and wraps
        /// the current interpreter host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose host is copied and wrapped.
        /// </param>
        /// <param name="type">
        /// The type of host to create.
        /// </param>
        /// <param name="host">
        /// Supplies the host on input and, upon success, receives the
        /// newly created host.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode CopyAndWrapHost(
            Interpreter interpreter,
            Type type,
            ref IHost host,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HostOps.CopyAndWrap(
                interpreter, type, ref host, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unwraps and disposes of the interpreter wrapper host, restoring
        /// its base host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose wrapper host is unwrapped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message
        /// describing the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or
        /// failure of the operation.
        /// </returns>
        public static ReturnCode UnwrapAndDisposeHost(
            Interpreter interpreter,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HostOps.UnwrapAndDispose(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a human-readable string describing the operating system
        /// name, version, and related platform information for the current
        /// process.
        /// </summary>
        /// <returns>
        /// A string describing the operating system and platform, or null
        /// if it is not available.
        /// </returns>
        public static string GetOperatingSystemNameAndVersion()
        {
            return PlatformOps.GetOperatingSystemNameAndVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a combined string containing the name and
        /// version of the current managed runtime.
        /// </summary>
        /// <returns>
        /// A string describing the current runtime name and version, or
        /// null if it is not available.
        /// </returns>
        public static string GetRuntimeNameAndVersion()
        {
            return CommonOps.Runtime.GetRuntimeNameAndVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a string describing the version, including
        /// the pieces of version information appropriate for setup use.
        /// </summary>
        /// <returns>
        /// The version string, or null if it could not be built.
        /// </returns>
        public static string GetVersion()
        {
            return _RuntimeOps.GetVersion(VersionFlags.Setup);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// a Macintosh (Mac OS) operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Macintosh; otherwise,
        /// false.
        /// </returns>
        public static bool IsMacintoshOperatingSystem()
        {
            return PlatformOps.IsMacintoshOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// a Unix operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Unix; otherwise, false.
        /// </returns>
        public static bool IsUnixOperatingSystem()
        {
            return PlatformOps.IsUnixOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// a Linux operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Linux; otherwise,
        /// false.
        /// </returns>
        public static bool IsLinuxOperatingSystem()
        {
            return PlatformOps.IsLinuxOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// a Windows operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows; otherwise,
        /// false.
        /// </returns>
        public static bool IsWindowsOperatingSystem()
        {
            return PlatformOps.IsWindowsOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST || !WINFORMS
        /// <summary>
        /// This method determines an automated answer to a prompt by
        /// matching the prompt text against the configured dialog
        /// automation data, returning a boolean answer.
        /// </summary>
        /// <param name="text">
        /// The prompt text to match against the dialog automation data.
        /// </param>
        /// <param name="caption">
        /// The caption associated with the prompt.  This parameter is not
        /// used.
        /// </param>
        /// <param name="default">
        /// The answer to return when no match is found; this parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The automated boolean answer, or the supplied default (which may
        /// be null) when no match is found.
        /// </returns>
        public static bool? GetPromptResultForAutomation(
            string text,
            string caption,
            bool? @default
            )
        {
            return WindowOps.GetPromptResultForAutomation(
                text, caption, @default);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if WINFORMS
        /// <summary>
        /// This method determines an automated answer to a prompt by
        /// matching the prompt text against the configured dialog
        /// automation data, returning either a boolean or an enumerated
        /// value depending on the requested type.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the automated answer; typically a boolean or
        /// an enumerated dialog result type.
        /// </typeparam>
        /// <param name="text">
        /// The prompt text to match against the dialog automation data.
        /// </param>
        /// <param name="caption">
        /// The caption associated with the prompt.  This parameter is not
        /// used.
        /// </param>
        /// <param name="default">
        /// The answer to return when no match is found; this parameter may
        /// be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing matched values; this parameter may
        /// be null.
        /// </param>
        /// <param name="automationFlags">
        /// The flags controlling how the dialog automation data is parsed
        /// and matched.
        /// </param>
        /// <returns>
        /// The automated answer, or the supplied default (which may be
        /// null) when no match is found.
        /// </returns>
        public static T? GetPromptResultForAutomation<T>(
            string text,
            string caption,
            T? @default,
            CultureInfo cultureInfo,
            AutomationFlags automationFlags
            ) where T : struct /* e.g. System.Windows.Forms.DialogResult */
        {
            return WindowOps.GetPromptResultForAutomation<T>(
                text, caption, @default, cultureInfo, automationFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the native window handle (hWnd) associated
        /// with the specified control, without forcing the handle to be
        /// created.
        /// </summary>
        /// <param name="control">
        /// The control whose native window handle is to be queried.
        /// </param>
        /// <param name="handle">
        /// On input, this parameter is ignored; upon success, it receives
        /// the native window handle associated with the control.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetControlHandle( /* hWnd */
            Control control,
            ref IntPtr handle,
            ref Result error
            )
        {
            return FormOps.GetHandle(control, ref handle, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the native menu handle (hMenu) associated
        /// with the specified menu, without forcing the handle to be
        /// created.
        /// </summary>
        /// <param name="menu">
        /// The menu whose native menu handle is to be queried.
        /// </param>
        /// <param name="handle">
        /// On input, this parameter is ignored; upon success, it receives
        /// the native menu handle associated with the menu.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetMenuHandle( /* hMenu */
            Menu menu,
            ref IntPtr handle,
            ref Result error
            )
        {
            return FormOps.GetHandle(menu, ref handle, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        /// <summary>
        /// This method securely overwrites a region of unmanaged memory
        /// with zero bytes, using a non-elidable platform zeroing
        /// primitive.
        /// </summary>
        /// <param name="pMemory">
        /// A pointer to the start of the unmanaged memory region to zero.
        /// </param>
        /// <param name="size">
        /// The number of bytes, starting at the supplied pointer, to
        /// overwrite with zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ZeroMemory(
            IntPtr pMemory,
            uint size,
            ref Result error
            )
        {
            return NativeOps.ZeroMemory(pMemory, size, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Securely zero a managed byte array. When native support is
        //       available this uses a non-elidable platform zeroing primitive
        //       (RtlZeroMemory on Windows, memset on Unix); otherwise it falls
        //       back to Array.Clear, which the JIT may treat as a dead store.
        //       The platform conditional is kept inside this method so callers
        //       need not be platform-aware.
        //
        /// <summary>
        /// This method securely overwrites the contents of the specified
        /// managed byte array with zero bytes.  When native support is
        /// available a non-elidable platform zeroing primitive is used;
        /// otherwise it falls back to Array.Clear.
        /// </summary>
        /// <param name="array">
        /// The byte array whose contents are to be zeroed; an error is
        /// returned when this is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ZeroMemory(
            byte[] array,
            ref Result error
            )
        {
            if (array == null)
            {
                error = "invalid array";
                return ReturnCode.Error;
            }

#if NATIVE && (WINDOWS || UNIX)
            return NativeOps.ZeroMemory(array, ref error);
#else
            if (array.Length > 0)
                Array.Clear(array, 0, array.Length);

            return ReturnCode.Ok;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        /// <summary>
        /// This method computes a hash code for the specified byte array
        /// over its full length.
        /// </summary>
        /// <param name="array">
        /// The byte array to hash.
        /// </param>
        /// <returns>
        /// The computed hash code for the byte array.
        /// </returns>
        public static int GetHashCode(
            byte[] array
            )
        {
            return ArrayOps.GetHashCode(array);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the timeout, in milliseconds, to use for the
        /// specified type of thread operation, preferring the supplied
        /// timeout, then the timeout configured for the interpreter, and
        /// finally the default timeout for that operation type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose configured timeout is consulted;
        /// this parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The specific timeout, in milliseconds, to prefer, or null to
        /// fall back to the interpreter or default timeout.
        /// </param>
        /// <param name="timeoutType">
        /// The type of thread operation the timeout applies to.
        /// </param>
        /// <returns>
        /// The resolved timeout, in milliseconds.
        /// </returns>
        public static int GetThreadTimeout(
            Interpreter interpreter,
            int? timeout,
            TimeoutType timeoutType
            ) /* SAFE-ON-DISPOSE */
        {
            return ThreadOps.GetTimeout(
                interpreter, timeout, timeoutType | TimeoutType.External);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and/or starts a thread to run the specified
        /// start delegate, optionally queuing a work item to the thread
        /// pool instead of creating a dedicated thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to associate with the thread; this
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to assign to the thread, or null to derive a name from
        /// the start delegate.
        /// </param>
        /// <param name="start">
        /// The delegate that the thread or work item will execute.
        /// </param>
        /// <param name="parameter">
        /// The parameter to pass to the start delegate; this parameter may
        /// be null.
        /// </param>
        /// <param name="useThreadPool">
        /// Non-zero to queue a work item to the thread pool instead of
        /// creating a dedicated thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for a newly created
        /// thread.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the thread will be used for user-interface purposes.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the active call stack should be associated with the
        /// thread.
        /// </param>
        /// <param name="thread">
        /// Upon return, this parameter receives the thread that was created;
        /// it must be null on entry when a dedicated thread is being
        /// created.
        /// </param>
        public static void CreateAndOrStartThread(
            Interpreter interpreter,
            string name,
            ParameterizedThreadStart start,
            object parameter,
            bool useThreadPool,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread
            ) /* SAFE-ON-DISPOSE */
        {
            ThreadOps.CreateAndOrStart(
                interpreter, name, start, parameter, useThreadPool,
                maxStackSize, userInterface, isBackground, useActiveStack,
                ref thread);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to shut down the specified thread,
        /// optionally waiting for it, interrupting it, and aborting it,
        /// according to the specified flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to use when waiting for the thread
        /// to join, or null to use the default timeout.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the thread is shut down.
        /// </param>
        /// <param name="thread">
        /// The thread to shut down; upon return, this parameter is set to
        /// null unless the flags request that it be preserved.
        /// </param>
        public static void MaybeShutdownThread(
            Interpreter interpreter,
            int? timeout,
            ShutdownFlags flags,
            ref Thread thread
            ) /* SAFE-ON-DISPOSE */
        {
            ThreadOps.MaybeShutdown(
                interpreter, timeout, flags | ShutdownFlags.External,
                ref thread);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified callback delegate to the thread
        /// pool, without waiting for the work item to start.
        /// </summary>
        /// <param name="callBack">
        /// The callback delegate to queue to the thread pool.
        /// </param>
        /// <returns>
        /// True if the work item was successfully queued; otherwise, false.
        /// </returns>
        public static bool QueueUserWorkItem(
            WaitCallback callBack
            )
        {
            return ThreadOps.QueueUserWorkItem(callBack, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified callback delegate to the thread
        /// pool, optionally waiting for the work item to start as directed
        /// by the supplied flags.
        /// </summary>
        /// <param name="callBack">
        /// The callback delegate to queue to the thread pool.
        /// </param>
        /// <param name="flags">
        /// The flags that control the behavior of this method; the
        /// wait-for-start flag determines whether this method waits for the
        /// queued work item to start before returning.
        /// </param>
        /// <returns>
        /// True if the work item was successfully queued; otherwise, false.
        /// </returns>
        public static bool QueueUserWorkItem(
            WaitCallback callBack,
            QueueFlags flags
            )
        {
            return ThreadOps.QueueUserWorkItem(
                callBack, FlagOps.HasFlags(flags,
                QueueFlags.WaitForStart, true));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified callback delegate and state
        /// object to the thread pool, without waiting for the work item to
        /// start.
        /// </summary>
        /// <param name="callBack">
        /// The callback delegate to queue to the thread pool.
        /// </param>
        /// <param name="state">
        /// The state object to pass to the callback delegate.
        /// </param>
        /// <returns>
        /// True if the work item was successfully queued; otherwise, false.
        /// </returns>
        public static bool QueueUserWorkItem(
            WaitCallback callBack,
            object state
            )
        {
            return ThreadOps.QueueUserWorkItem(callBack, state, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified callback delegate and state
        /// object to the thread pool, optionally waiting for the work item
        /// to start as directed by the supplied flags.
        /// </summary>
        /// <param name="callBack">
        /// The callback delegate to queue to the thread pool.
        /// </param>
        /// <param name="state">
        /// The state object to pass to the callback delegate.
        /// </param>
        /// <param name="flags">
        /// The flags that control the behavior of this method; the
        /// wait-for-start flag determines whether this method waits for the
        /// queued work item to start before returning.
        /// </param>
        /// <returns>
        /// True if the work item was successfully queued; otherwise, false.
        /// </returns>
        public static bool QueueUserWorkItem(
            WaitCallback callBack,
            object state,
            QueueFlags flags
            )
        {
            return ThreadOps.QueueUserWorkItem(
                callBack, state, FlagOps.HasFlags(flags,
                QueueFlags.WaitForStart, true));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an instance of the hash algorithm with the
        /// specified name.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to create, or null to use the
        /// default hash algorithm.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The created hash algorithm instance, or null if it could not be
        /// created.
        /// </returns>
        public static HashAlgorithm CreateHashAlgorithm(
            string hashAlgorithmName,
            ref Result error
            )
        {
            return HashOps.CreateAlgorithm(hashAlgorithmName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the qualified full type name from the
        /// specified namespace, type name, and containing assembly.
        /// </summary>
        /// <param name="namespaceName">
        /// The namespace portion of the type name.
        /// </param>
        /// <param name="typeName">
        /// The simple name of the type.
        /// </param>
        /// <param name="assembly">
        /// The assembly used to qualify the resulting type name.
        /// </param>
        /// <returns>
        /// The qualified full type name, or null if it could not be built.
        /// </returns>
        public static string GetFactoryTypeName(
            string namespaceName,
            string typeName,
            Assembly assembly
            )
        {
            return FormatOps.GetQualifiedTypeFullName(
                namespaceName, typeName, assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up a type by its name, using a
        /// case-insensitive comparison and optionally retrying after
        /// removing any dashes present in the name.
        /// </summary>
        /// <param name="typeName">
        /// The name of the type to look up.
        /// </param>
        /// <param name="allowFallback">
        /// Non-zero to retry the lookup with any dashes removed from the
        /// type name when the initial lookup fails.
        /// </param>
        /// <returns>
        /// The resolved type, or null if no matching type could be found.
        /// </returns>
        public static Type LookupFactoryType(
            string typeName,
            bool allowFallback
            )
        {
            return FactoryOps.LookupType(typeName, allowFallback);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an instance of the specified type using its
        /// registered factory callback, or via its default public
        /// constructor when no callback is registered.
        /// </summary>
        /// <param name="type">
        /// The type to create an instance of.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The newly created instance, or null if it could not be created.
        /// </returns>
        public static object CreateViaFactory(
            Type type,
            ref Result error
            )
        {
            return FactoryOps.Create(type, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the simple name or full name of the type of
        /// the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose type name is to be formatted; this parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The formatted type name, or null if it is not available.
        /// </returns>
        public static string FormatTypeNameOrFullName(
            object @object
            )
        {
            return FormatOps.TypeNameOrFullName(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the entire tracing subsystem back to its
        /// default state, including the trace filter callback, flags,
        /// limits, priorities, categories, format, and indicators.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="overrideEnvironment">
        /// Non-zero to override any values that would normally be read from
        /// environment variables.
        /// </param>
        public static void ResetTraceStatus(
            Interpreter interpreter,
            bool overrideEnvironment
            )
        {
            TraceOps.ResetStatus(interpreter, overrideEnvironment);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forcibly enables or disables one or more aspects of
        /// the tracing subsystem, as selected by the specified state type
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="stateType">
        /// The flags selecting which aspects of the tracing subsystem to
        /// modify and how (for example, reset, enable, or disable).
        /// </param>
        /// <param name="enabled">
        /// Non-zero to enable the selected aspects; zero to disable them.
        /// </param>
        /// <returns>
        /// The flags indicating which aspects of the tracing subsystem were
        /// actually modified.
        /// </returns>
        public static TraceStateType ForceTraceEnabledOrDisabled(
            Interpreter interpreter,
            TraceStateType stateType,
            bool enabled
            )
        {
            return TraceOps.ForceEnabledOrDisabled(
                interpreter, stateType, enabled);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the specified trace priority, in place, by a
        /// number of priority levels.
        /// </summary>
        /// <param name="priority">
        /// On input, the trace priority to adjust; on output, it receives
        /// the adjusted trace priority.
        /// </param>
        /// <param name="adjustment">
        /// The number of priority levels to adjust by; positive values
        /// increase the priority and negative values decrease it.
        /// </param>
        public static void AdjustTracePriority(
            ref TracePriority priority,
            int adjustment
            )
        {
            TraceOps.ExternalAdjustTracePriority(ref priority, adjustment);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method masks the specified trace priority down to only its
        /// priority-level bits.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to mask.
        /// </param>
        /// <returns>
        /// The masked trace priority, containing only its priority-level
        /// bits.
        /// </returns>
        public static TracePriority MaskTracePriority(
            TracePriority priority
            )
        {
            return TraceOps.MaskTracePriority(priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces the priority-level bits of the specified
        /// trace priority with those of the specified base priority, when
        /// the base priority contains any priority-level bits.
        /// </summary>
        /// <param name="priority">
        /// On input, the trace priority to modify; on output, it receives
        /// the modified trace priority.
        /// </param>
        /// <param name="basePriority">
        /// The base trace priority whose priority-level bits are used.
        /// </param>
        public static void ChangeBaseTracePriority(
            ref TracePriority priority,
            TracePriority basePriority
            )
        {
            TraceOps.ChangeBaseTracePriority(
                ref priority, basePriority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the configured trace priority mask, which
        /// controls the set of trace priority flags that are currently
        /// allowed.
        /// </summary>
        /// <returns>
        /// The configured trace priority mask.
        /// </returns>
        public static TracePriority GetTracePriorities()
        {
            return TraceOps.GetTracePriorities();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the configured trace priority mask, which
        /// controls the set of trace priority flags that are currently
        /// allowed.
        /// </summary>
        /// <param name="priorities">
        /// The new trace priority mask.
        /// </param>
        public static void SetTracePriorities(
            TracePriority priorities
            )
        {
            TraceOps.SetTracePriorities(priorities);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds or removes the specified trace priority flags
        /// from the configured trace priority mask.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags to add or remove.
        /// </param>
        /// <param name="enabled">
        /// Non-zero to add the specified flags; zero to remove them.
        /// </param>
        public static void AdjustTracePriorities(
            TracePriority priority,
            bool enabled
            )
        {
            TraceOps.AdjustTracePriorities(priority, enabled);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default setting that controls whether
        /// managed objects are disposed by default.
        /// </summary>
        /// <returns>
        /// True if managed objects are disposed by default; otherwise,
        /// false.
        /// </returns>
        public static bool GetObjectDefaultDispose()
        {
            return ObjectOps.GetDefaultDispose();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default setting that controls whether
        /// managed object operations are performed synchronously by
        /// default.
        /// </summary>
        /// <returns>
        /// True if managed object operations are performed synchronously by
        /// default; otherwise, false.
        /// </returns>
        public static bool GetObjectDefaultSynchronous()
        {
            return ObjectOps.GetDefaultSynchronous();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified object and complains, via the
        /// debugging subsystem, if disposal fails.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="interpreter">
        /// The interpreter context used when complaining about a disposal
        /// failure; this parameter may be null.
        /// </param>
        /// <param name="object">
        /// On input, the object to be disposed; upon return, it is reset to
        /// its default value.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode DisposeOrComplain<T>(
            Interpreter interpreter,
            ref T @object
            )
        {
            return ObjectOps.DisposeOrComplain<T>(interpreter, ref @object);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: On .NET Core, calling AppDomain.GetAssemblies via reflection
        //       does not appear to work.  Calling this method via reflection
        //       does work.
        //
        /// <summary>
        /// This method gets the assemblies currently loaded into the
        /// application domain of the calling code.
        /// </summary>
        /// <returns>
        /// The loaded assemblies, or null if there is no current
        /// application domain.
        /// </returns>
        public static IEnumerable<Assembly> GetAssemblies()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain == null)
                return null;

            return appDomain.GetAssemblies();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the named application
        /// configuration setting.
        /// </summary>
        /// <param name="name">
        /// The name of the application configuration setting to obtain.
        /// </param>
        /// <returns>
        /// The configured setting value, or null if it is not available.
        /// </returns>
        public static string GetAppSetting(
            string name
            )
        {
            return ConfigurationOps.GetAppSetting(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the data of the named embedded resource stream
        /// from the specified assembly, returning either the raw bytes or
        /// the text decoded using a default encoding.
        /// </summary>
        /// <param name="assembly">
        /// The assembly containing the embedded resource.
        /// </param>
        /// <param name="name">
        /// The name of the embedded resource to read.
        /// </param>
        /// <param name="raw">
        /// Non-zero to return the raw bytes; otherwise, the decoded text is
        /// returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The byte array or string read from the resource, or null on
        /// failure.
        /// </returns>
        public static object GetResourceStreamData(
            Assembly assembly,
            string name,
            bool raw,
            ref Result error
            )
        {
            return AssemblyOps.GetResourceStreamData(
                assembly, name, null, raw, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the data of the named embedded resource stream
        /// from the specified assembly, returning either the raw bytes or
        /// the text decoded using the specified encoding.
        /// </summary>
        /// <param name="assembly">
        /// The assembly containing the embedded resource.
        /// </param>
        /// <param name="name">
        /// The name of the embedded resource to read.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to decode the resource as text, or null to use
        /// a default encoding; this value is ignored when <paramref name="raw" />
        /// is non-zero.
        /// </param>
        /// <param name="raw">
        /// Non-zero to return the raw bytes; otherwise, the decoded text is
        /// returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The byte array or string read from the resource, or null on
        /// failure.
        /// </returns>
        public static object GetResourceStreamData(
            Assembly assembly,
            string name,
            Encoding encoding,
            bool raw,
            ref Result error
            )
        {
            return AssemblyOps.GetResourceStreamData(
                assembly, name, encoding, raw, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the string token used to represent a null
        /// object value.
        /// </summary>
        /// <returns>
        /// The string token used to represent a null object value.
        /// </returns>
        public static string NullObjectName()
        {
            return _Object.Null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the enumerated value of a named field or
        /// property of an object, which is expected to be a flags
        /// enumeration.
        /// </summary>
        /// <param name="object">
        /// The object whose field or property value is to be obtained; this
        /// parameter may be null.
        /// </param>
        /// <param name="memberName">
        /// The name of the field or property to obtain.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match the member name in a case-insensitive manner;
        /// otherwise, matching is case-sensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The enumerated value of the named member, or null if it could
        /// not be obtained.
        /// </returns>
        public static Enum GetInstanceFlags(
            object @object,
            string memberName,
            bool noCase,
            ref Result error
            )
        {
            return EnumOps.GetFlags(
                @object, memberName, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the process reference count from its
        /// environment variable and, if requested, increments or decrements
        /// it, persisting the updated value (or removing the variable when
        /// the count drops to zero or below).  The resulting count is
        /// discarded by this overload.
        /// </summary>
        /// <param name="prefix">
        /// The environment variable name prefix to use, or null to use the
        /// default prefix.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the integer reference count value.
        /// </param>
        /// <param name="increment">
        /// Non-zero to increment the reference count, zero to decrement it,
        /// or null to leave it unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CheckAndMaybeModifyProcessReferenceCount(
            string prefix,
            CultureInfo cultureInfo,
            bool? increment,
            ref Result error
            )
        {
            long referenceCount; /* NOT USED */

            return ProcessOps.CheckAndMaybeModifyReferenceCount(
                prefix, cultureInfo, increment, out referenceCount,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the process reference count from its
        /// environment variable and, if requested, increments or decrements
        /// it, persisting the updated value (or removing the variable when
        /// the count drops to zero or below).
        /// </summary>
        /// <param name="prefix">
        /// The environment variable name prefix to use, or null to use the
        /// default prefix.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the integer reference count value.
        /// </param>
        /// <param name="increment">
        /// Non-zero to increment the reference count, zero to decrement it,
        /// or null to leave it unchanged.
        /// </param>
        /// <param name="referenceCount">
        /// Upon return, this parameter receives the resulting reference
        /// count value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CheckAndMaybeModifyProcessReferenceCount(
            string prefix,
            CultureInfo cultureInfo,
            bool? increment,
            out long referenceCount,
            ref Result error
            )
        {
            return ProcessOps.CheckAndMaybeModifyReferenceCount(
                prefix, cultureInfo, increment, out referenceCount,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a list-valued environment variable and, if
        /// requested, clears it and/or appends an element to it, persisting
        /// the updated list (or removing the variable when the resulting
        /// list is empty).
        /// </summary>
        /// <param name="prefix">
        /// The environment variable name prefix to use, or null to use the
        /// default prefix.
        /// </param>
        /// <param name="element">
        /// The element to append to the list, or null to append nothing.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the existing list prior to appending.
        /// </param>
        /// <param name="list">
        /// Upon return, this parameter receives the resulting list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CheckAndMaybeAppendProcessElement(
            string prefix,
            string element,
            bool clear,
            out StringList list,
            ref Result error
            )
        {
            return ProcessOps.CheckAndMaybeAppendElement(
                prefix, element, clear, out list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX || UNSAFE)
        /// <summary>
        /// This method atomically replaces the native stack callback
        /// delegate of the specified type with the supplied delegate,
        /// returning the previous delegate.
        /// </summary>
        /// <param name="callbackType">
        /// The type of native stack callback to be changed.
        /// </param>
        /// <param name="delegate">
        /// On input, the new delegate to install; upon success, it receives
        /// the previously installed delegate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ChangeNativeCallback(
            NativeCallbackType callbackType,
            ref Delegate @delegate,
            ref Result error
            )
        {
            return NativeStack.ChangeCallback(
                callbackType, ref @delegate, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if WEB
        /// <summary>
        /// This method appends the name and value pairs from the specified
        /// dictionary to a URI query string, URL-encoding each name and
        /// value and separating successive pairs with ampersands.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary of name and value pairs to append; nothing is
        /// appended when this is null.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when URL-encoding the names and values, or
        /// null to use the default encoding.
        /// </param>
        /// <param name="builder">
        /// On input, the string builder that receives the query string;
        /// when null, a new string builder is created and returned via this
        /// parameter.
        /// </param>
        public static void QueryFromDictionary(
            _StringDictionary dictionary,
            Encoding encoding,
            ref StringBuilder builder
            )
        {
            PathOps.QueryFromDictionary(
                dictionary, encoding, ref builder);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// This method converts an arbitrary data value into its string
        /// representation, using the formatting options carried by the
        /// specified data value format.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="value">
        /// The data value to convert; this parameter may be null.
        /// </param>
        /// <param name="formatDataValue">
        /// The formatting options used to convert the value; null causes
        /// null to be returned.
        /// </param>
        /// <returns>
        /// The string representation of the value, or null when no
        /// formatting options were supplied.
        /// </returns>
        public static string FixupDataValue(
            Interpreter interpreter,
            object value,
            IFormatDataValue formatDataValue
            ) /* throw */
        {
            if (formatDataValue == null)
                return null;

            return MarshalOps.FixupDataValue(
                interpreter, value,
                formatDataValue.CultureInfo,
                formatDataValue.BlobBehavior,
                formatDataValue.DateTimeBehavior,
                formatDataValue.DateTimeKind,
                formatDataValue.DateTimeFormat,
                formatDataValue.NumberFormat,
                formatDataValue.NullValue,
                formatDataValue.DbNullValue,
                formatDataValue.ErrorValue,
                formatDataValue.Alias);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the records read from the specified data
        /// reader into a string list, using the formatting options carried
        /// by the specified data value format.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="reader">
        /// The data reader whose records are to be converted.
        /// </param>
        /// <param name="formatDataValue">
        /// The formatting options used to convert the field values; an
        /// error is returned when this is null.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the converted records are
        /// appended.
        /// </param>
        /// <param name="count">
        /// On input and output, the running count of records converted,
        /// which is incremented by this method.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode DataReaderToList(
            Interpreter interpreter,
            IDataReader reader,
            IFormatDataValue formatDataValue,
            ref StringList list,
            ref int count,
            ref Result error
            )
        {
            if (formatDataValue == null)
            {
                error = "invalid data value format parameters";
                return ReturnCode.Error;
            }

            return DataOps.DataReaderToList(
                interpreter, reader,
                formatDataValue.CultureInfo,
                formatDataValue.BlobBehavior,
                formatDataValue.DateTimeBehavior,
                formatDataValue.DateTimeKind,
                formatDataValue.DateTimeFormat,
                formatDataValue.NumberFormat,
                formatDataValue.NullValue,
                formatDataValue.DbNullValue,
                formatDataValue.ErrorValue,
                formatDataValue.Limit,
                formatDataValue.Nested,
                formatDataValue.Clear,
                formatDataValue.AllowNull,
                formatDataValue.Pairs,
                formatDataValue.Names,
                formatDataValue.NoFixup,
                formatDataValue.Alias,
                ref list, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// Reads every row from a data reader and builds an in-memory data
        /// table, applying the value conversion and formatting settings
        /// carried by the supplied format object.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="reader">
        /// The data reader whose rows are loaded into the table.
        /// </param>
        /// <param name="formatDataValue">
        /// The object whose properties supply the culture, BLOB, date-time,
        /// number, and null/error value conversion settings used to build the
        /// table; this parameter must not be null.
        /// </param>
        /// <param name="dataTable">
        /// Upon success, receives the data table built from the rows of the
        /// reader; it is set to null on failure.  Any value passed in is
        /// ignored.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode DataReaderToDataTable(
            Interpreter interpreter,
            IDataReader reader,
            IFormatDataValue formatDataValue,
            ref IDataTable dataTable,
            ref Result error
            )
        {
            if (formatDataValue == null)
            {
                error = "invalid data value format parameters";
                return ReturnCode.Error;
            }

            Result localResult = null;

            dataTable = DataOps.CreateDataTable(
                reader, interpreter,
                formatDataValue.CultureInfo,
                formatDataValue.BlobBehavior,
                formatDataValue.DateTimeBehavior,
                formatDataValue.DateTimeKind,
                formatDataValue.DateTimeFormat,
                formatDataValue.NumberFormat,
                formatDataValue.NullValue,
                formatDataValue.DbNullValue,
                formatDataValue.ErrorValue,
                ref localResult);

            if (dataTable == null)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads rows from a data reader into the elements of a Tcl array
        /// variable, storing the column-name list, one element per row, and
        /// the final row count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose array variable is populated; this
        /// parameter must not be null.
        /// </param>
        /// <param name="reader">
        /// The data reader whose rows are read.
        /// </param>
        /// <param name="varName">
        /// The name of the array variable to populate; this parameter may be
        /// null, in which case the rows are read and counted but no variable
        /// is written.
        /// </param>
        /// <param name="formatDataValue">
        /// The object whose properties supply the value conversion,
        /// formatting, and per-row handling settings; this parameter must not
        /// be null.
        /// </param>
        /// <param name="count">
        /// On input, supplies an initial count to which the number of rows
        /// read is added; upon success, receives that updated total.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode DataReaderToArray(
            Interpreter interpreter,
            IDataReader reader,
            string varName,
            IFormatDataValue formatDataValue,
            ref int count,
            ref Result error
            )
        {
            if (formatDataValue == null)
            {
                error = "invalid data value format parameters";
                return ReturnCode.Error;
            }

            return DataOps.DataReaderToArray(
                interpreter, reader, varName,
                formatDataValue.CultureInfo,
                formatDataValue.BlobBehavior,
                formatDataValue.DateTimeBehavior,
                formatDataValue.DateTimeKind,
                formatDataValue.DateTimeFormat,
                formatDataValue.NumberFormat,
                formatDataValue.NullValue,
                formatDataValue.DbNullValue,
                formatDataValue.ErrorValue,
                formatDataValue.Limit,
                formatDataValue.Clear,
                formatDataValue.AllowNull,
                formatDataValue.Pairs,
                formatDataValue.Names,
                formatDataValue.NoFixup,
                formatDataValue.Alias,
                ref count, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// Simulates typing a string of keystrokes into the native console
        /// window after ensuring it has keyboard focus.
        /// </summary>
        /// <param name="stringCallback">
        /// A callback invoked to validate the string as it is typed; this
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="value">
        /// The string of characters to type into the console.
        /// </param>
        /// <param name="milliseconds">
        /// The delay, in milliseconds, between simulated keystrokes.
        /// </param>
        /// <param name="flags">
        /// The flags that control the keystroke simulation behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ConsoleKeyboardString(
            CheckStringCallback stringCallback,
            IClientData clientData,
            string value,
            int milliseconds,
            SimulatedKeyFlags flags,
            ref Result error
            )
        {
            return NativeConsole.SimulateKeyboardString(
                stringCallback, clientData, value,
                milliseconds, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Simulates typing a string of keystrokes into the native console
        /// window after ensuring it has keyboard focus, allowing the
        /// operation to be canceled.
        /// </summary>
        /// <param name="cancelCallback">
        /// A callback invoked to determine whether the simulation should be
        /// canceled; this parameter may be null, in which case a default
        /// focus check is used.
        /// </param>
        /// <param name="stringCallback">
        /// A callback invoked to validate the string as it is typed; this
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="value">
        /// The string of characters to type into the console.
        /// </param>
        /// <param name="milliseconds">
        /// The delay, in milliseconds, between simulated keystrokes.
        /// </param>
        /// <param name="flags">
        /// The flags that control the keystroke simulation behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ConsoleKeyboardString(
            CheckCancelCallback cancelCallback,
            CheckStringCallback stringCallback,
            IClientData clientData,
            string value,
            int milliseconds,
            SimulatedKeyFlags flags,
            ref Result error
            )
        {
            return NativeConsole.SimulateKeyboardString(
                cancelCallback, stringCallback, clientData,
                value, milliseconds, flags, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts a name to absolute form, prepending the global namespace
        /// separator ("::") when it is not already absolute.
        /// </summary>
        /// <param name="name">
        /// The name to convert; this parameter may be null.
        /// </param>
        /// <returns>
        /// The absolute name; the global namespace name ("::") if
        /// <paramref name="name"/> is empty, or null if it is null.
        /// </returns>
        public static string MakeAbsoluteName(
            string name
            )
        {
            return NamespaceOps.MakeAbsoluteName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Produces a command name by stripping any leading global namespace
        /// separator ("::") from the specified name.
        /// </summary>
        /// <param name="name">
        /// The name to convert; this parameter may be null.
        /// </param>
        /// <returns>
        /// The name with any leading "::" removed, or null if
        /// <paramref name="name"/> is null.
        /// </returns>
        public static string MakeCommandName(
            string name
            )
        {
            return ScriptOps.MakeCommandName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads the PE link timestamp from the header of the specified
        /// executable file and converts it to a date-time value.
        /// </summary>
        /// <param name="fileName">
        /// The path of the PE (portable executable) file to read.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, receives the link timestamp read from the file; it
        /// is left unchanged on failure.
        /// </param>
        /// <returns>
        /// True if the timestamp was read and converted successfully;
        /// otherwise, false.
        /// </returns>
        public static bool GetPeFileDateTime(
            string fileName,
            ref DateTime dateTime
            )
        {
            return FileOps.GetPeFileDateTime(fileName, ref dateTime);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new, empty delegate mapper instance.
        /// </summary>
        /// <returns>
        /// The newly created delegate mapper.
        /// </returns>
        public static IDelegateMapper CreateDelegateMapper()
        {
            return new DelegateMapper();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string key used to cache attribute-derived metadata
        /// for the specified method overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based overload index of the method, embedded in the key.
        /// </param>
        /// <param name="method">
        /// The method to build the key for; this parameter may be null.
        /// </param>
        /// <returns>
        /// The cache key string, or null if <paramref name="method"/> is
        /// null.
        /// </returns>
        public static string GetMethodDataName(
            Interpreter interpreter,
            int index,
            MethodBase method
            )
        {
            return AttributeOps.GetMethodDataName(
                interpreter, index, method, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Retrieves the command flags previously cached for the specified
        /// method overload in the given application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to parse a string-form cached value;
        /// this parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose data slots are searched; this
        /// parameter may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based overload index of the method.
        /// </param>
        /// <param name="method">
        /// The method whose cached flags are wanted; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The cached command flags, or null if none are cached or they
        /// cannot be retrieved.
        /// </returns>
        public static CommandFlags? GetCachedCommandFlags(
            Interpreter interpreter,
            AppDomain appDomain,
            int index,
            MethodBase method
            )
        {
            return AttributeOps.GetCachedCommandFlags(
                interpreter, appDomain, index, method);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the specified command flags for a method overload in the
        /// given application domain's data slot.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to build the cache key; this
        /// parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose data slot is written; this parameter
        /// may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based overload index of the method.
        /// </param>
        /// <param name="method">
        /// The method to key on; this parameter may be null.
        /// </param>
        /// <param name="commandFlags">
        /// The command flags to store; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the flags were stored successfully; otherwise, false.
        /// </returns>
        public static bool SetCachedCommandFlags(
            Interpreter interpreter,
            AppDomain appDomain,
            int index,
            MethodBase method,
            CommandFlags? commandFlags
            )
        {
            return AttributeOps.SetCachedCommandFlags(
                interpreter, appDomain, index, method,
                commandFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified value is exactly a power of two.
        /// </summary>
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// <returns>
        /// True if <paramref name="value"/> is a power of two (including
        /// one); otherwise, false.
        /// </returns>
        public static bool IsPowerOfTwo(
            ulong value
            )
        {
            return MathOps.IsPowerOfTwo(value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// Reads a file of CIDR patterns (one per line, skipping blank and
        /// comment lines), extracts an address prefix from each, and groups
        /// the original lines under their prefix in a dictionary.
        /// </summary>
        /// <param name="fileName">
        /// The path of the file to read.
        /// </param>
        /// <param name="prefixLength">
        /// The number of address parts to use as the extracted prefix; this
        /// parameter may be null to use the default prefix length.
        /// </param>
        /// <param name="ipFlags">
        /// The flags controlling parsing, error handling, and the
        /// empty-result behavior.
        /// </param>
        /// <param name="wildcard">
        /// Non-zero to append a trailing wildcard component to each prefix,
        /// zero to suppress it, or null to use the default behavior.
        /// </param>
        /// <param name="dictionary">
        /// On input, supplies an existing dictionary to add to (a new one is
        /// created if it is null); upon success, receives the grouped
        /// patterns.
        /// </param>
        /// <param name="count">
        /// On input, supplies an initial count to which the number of
        /// patterns added is added; upon success, receives the updated total.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode LoadForCIDR(
            string fileName,
            byte? prefixLength,
            IpFlags ipFlags,
            bool? wildcard,
            ref CidrDictionary dictionary,
            ref int count,
            ref Result error
            )
        {
            return SocketOps.LoadForCIDR(
                fileName, prefixLength, ipFlags, wildcard,
                ref dictionary, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores each entry of a CIDR dictionary as an element (prefix
        /// mapped to its list of patterns) of an existing array variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose array variable is updated; this
        /// parameter must not be null.
        /// </param>
        /// <param name="varName">
        /// The name of the existing array variable to update; this parameter
        /// must not be null.  The variable is not created if it does not
        /// already exist.
        /// </param>
        /// <param name="dictionary">
        /// The CIDR dictionary whose entries become array elements; this
        /// parameter must not be null.
        /// </param>
        /// <param name="ipFlags">
        /// This parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode UpdateVariableWithCIDR(
            Interpreter interpreter,
            string varName,
            CidrDictionary dictionary,
            IpFlags ipFlags,
            ref Result error
            )
        {
            return SocketOps.UpdateVariableWithCIDR(
                interpreter, varName, dictionary, ipFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Tests whether a single host name or IP address falls within a
        /// single CIDR pattern.
        /// </summary>
        /// <param name="address">
        /// The host name or IP address to test.
        /// </param>
        /// <param name="pattern">
        /// The CIDR pattern, in "address/prefix-length" form, to match
        /// against.
        /// </param>
        /// <param name="ipFlags">
        /// The flags controlling validation, address resolution, and
        /// matching.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// True if the address matches, false if it does not, or null if
        /// the match could not be performed.
        /// </returns>
        public static bool? MatchViaCIDR(
            string address,
            string pattern,
            IpFlags ipFlags,
            ref Result error
            )
        {
            return SocketOps.MatchViaCIDR(
                address, pattern, ipFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Tests whether a host name or IP address matches any of the
        /// specified CIDR patterns, complaining if the match cannot be
        /// performed.
        /// </summary>
        /// <param name="address">
        /// The host name or IP address to test.
        /// </param>
        /// <param name="patterns">
        /// The collection of CIDR patterns to match against.
        /// </param>
        /// <param name="ipFlags">
        /// The flags controlling validation, address resolution, and
        /// matching.
        /// </param>
        /// <returns>
        /// True if any pattern matches, false if none match, or null if the
        /// match could not be performed.
        /// </returns>
        public static bool? MatchViaCIDR(
            string address,
            IEnumerable<string> patterns,
            IpFlags ipFlags
            )
        {
            bool? match;
            Result error = null;

            match = SocketOps.MatchViaCIDR(
                address, patterns, ipFlags, ref error);

            if (match == null)
                DebugOps.Complain(ReturnCode.Error, error);

            return match;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Tests whether a host name or IP address matches any of the
        /// specified CIDR patterns.
        /// </summary>
        /// <param name="address">
        /// The host name or IP address to test.
        /// </param>
        /// <param name="patterns">
        /// The collection of CIDR patterns to match against.
        /// </param>
        /// <param name="ipFlags">
        /// The flags controlling validation, address resolution, and
        /// matching.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// True if any pattern matches, false if none match, or null if the
        /// match could not be performed.
        /// </returns>
        public static bool? MatchViaCIDR(
            string address,
            IEnumerable<string> patterns,
            IpFlags ipFlags,
            ref Result error
            )
        {
            return SocketOps.MatchViaCIDR(
                address, patterns, ipFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Tests whether a host name or IP address matches any of the
        /// specified CIDR patterns, reporting the index of the first
        /// matching pattern.
        /// </summary>
        /// <param name="address">
        /// The host name or IP address to test.
        /// </param>
        /// <param name="patterns">
        /// The collection of CIDR patterns to match against.
        /// </param>
        /// <param name="ipFlags">
        /// The flags controlling validation, address resolution, and
        /// matching.
        /// </param>
        /// <param name="index">
        /// Upon a successful match, receives the zero-based index of the
        /// first matching pattern; it is set to null when no pattern matches
        /// or the match cannot be performed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// True if any pattern matches, false if none match, or null if the
        /// match could not be performed.
        /// </returns>
        public static bool? MatchViaCIDR(
            string address,
            IEnumerable<string> patterns,
            IpFlags ipFlags,
            out int? index,
            ref Result error
            )
        {
            return SocketOps.MatchViaCIDR(
                address, patterns, ipFlags, out index, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Collapses a set of IPv4 CIDR patterns into the minimal equivalent
        /// set by merging overlapping and adjacent address ranges.
        /// </summary>
        /// <param name="patterns">
        /// The IPv4 CIDR patterns to collapse.
        /// </param>
        /// <param name="ipFlags">
        /// The flags controlling validation and whether the result is
        /// sorted.
        /// </param>
        /// <param name="merged">
        /// On input, supplies an existing list to append to (a new one is
        /// created if it is null); upon success, receives the collapsed
        /// patterns.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode Collapse_IPv4_CIDR(
            IEnumerable<string> patterns,
            IpFlags ipFlags,
            ref StringList merged,
            ref Result error
            )
        {
            return SocketOps.Collapse_IPv4_CIDR(
                patterns, ipFlags, ref merged, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// Collapses a set of IPv6 CIDR patterns into the minimal equivalent
        /// set by merging overlapping and adjacent address ranges.
        /// </summary>
        /// <param name="patterns">
        /// The IPv6 CIDR patterns to collapse.
        /// </param>
        /// <param name="ipFlags">
        /// The flags controlling validation and whether the result is
        /// sorted.
        /// </param>
        /// <param name="merged">
        /// On input, supplies an existing list to append to (a new one is
        /// created if it is null); upon success, receives the collapsed
        /// patterns.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode Collapse_IPv6_CIDR(
            IEnumerable<string> patterns,
            IpFlags ipFlags,
            ref StringList merged,
            ref Result error
            )
        {
            return SocketOps.Collapse_IPv6_CIDR(
                patterns, ipFlags, ref merged, ref error);
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified date-time value using the full ISO-8601
        /// pattern, including seven-digit fractional seconds and a time-zone
        /// designator.
        /// </summary>
        /// <param name="value">
        /// The date-time value to format.
        /// </param>
        /// <returns>
        /// The formatted ISO-8601 date-time string.
        /// </returns>
        public static string FormatIso8601FullDateTime(
            DateTime value
            )
        {
            return FormatOps.Iso8601FullDateTime(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Optionally truncates a date-time value down to the specified
        /// granularity in seconds, flooring it to the nearest multiple
        /// within the day (or to the start of the day).
        /// </summary>
        /// <param name="value">
        /// The date-time value to truncate; this parameter may be null to
        /// use the current UTC time.
        /// </param>
        /// <param name="seconds">
        /// The truncation granularity, in seconds; a value of zero or less
        /// leaves the value unchanged.
        /// </param>
        /// <returns>
        /// The truncated date-time value.
        /// </returns>
        public static DateTime MaybeTruncateDateTime(
            DateTime? value,
            long seconds
            )
        {
            return TimeOps.MaybeTruncate(value, seconds);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the log file name saved in the current application domain.
        /// </summary>
        /// <returns>
        /// The saved log file name, or null if none is set or it cannot be
        /// retrieved.
        /// </returns>
        public static string GetSavedLogFileName()
        {
            return AppDomainOps.GetSavedLogFileName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Saves a log file name in the current application domain.
        /// </summary>
        /// <param name="fileName">
        /// The log file name to save; this parameter may be null to clear
        /// the saved name.
        /// </param>
        /// <returns>
        /// True if the name was saved successfully; otherwise, false.
        /// </returns>
        public static bool SetSavedLogFileName(
            string fileName
            )
        {
            return AppDomainOps.SetSavedLogFileName(fileName);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          command syntax management subsystem is completed.
        //
        /// <summary>
        /// Clears the shared in-memory cache of command syntax data.
        /// </summary>
        /// <returns>
        /// The number of entries that were removed from the cache.
        /// </returns>
        public static int ClearCachedSyntaxData()
        {
            return SyntaxOps.ClearCache();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          command syntax management subsystem is completed.
        //
        /// <summary>
        /// Parses command syntax data from the specified text and merges it
        /// into the shared in-memory cache.
        /// </summary>
        /// <param name="text">
        /// The syntax data text to parse.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is parsed and merged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode LoadAndCacheSyntaxData(
            string text,
            SyntaxDataFlags flags,
            ref Result error
            )
        {
            return SyntaxOps.LoadAndCacheData(
                text, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          command syntax management subsystem is completed.
        //
        /// <summary>
        /// Parses command syntax data from the specified text and merges it
        /// into the supplied syntax data.
        /// </summary>
        /// <param name="text">
        /// The syntax data text to parse.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is parsed and merged.
        /// </param>
        /// <param name="data">
        /// On input, supplies existing syntax data to merge into; this
        /// parameter may be null.  Upon success, receives the merged data.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode LoadSyntaxData(
            string text,
            SyntaxDataFlags flags,
            ref SyntaxData data,
            ref Result error
            )
        {
            return SyntaxOps.LoadData(
                text, flags, ref data, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          command syntax management subsystem is completed.
        //
        /// <summary>
        /// Reads command syntax data from the specified file and merges it
        /// into the supplied syntax data.
        /// </summary>
        /// <param name="fileName">
        /// The path of the file to read.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when reading the file; this
        /// parameter may be null to use the default encoding.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the data is parsed and merged.
        /// </param>
        /// <param name="data">
        /// On input, supplies existing syntax data to merge into; this
        /// parameter may be null.  Upon success, receives the merged data.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode LoadSyntaxDataFrom(
            string fileName,
            Encoding encoding,
            SyntaxDataFlags flags,
            ref SyntaxData data,
            ref Result error
            )
        {
            return SyntaxOps.LoadDataFrom(
                fileName, encoding, flags, ref data, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          command syntax management subsystem is completed.
        //
        /// <summary>
        /// Reads command syntax data from every matching file in the
        /// specified directory and merges it into the supplied syntax data.
        /// </summary>
        /// <param name="directory">
        /// The directory to search for syntax data files; recursion is
        /// controlled by the supplied flags.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when reading the files; this
        /// parameter may be null to use the default encoding.
        /// </param>
        /// <param name="flags">
        /// The flags controlling recursion and how the data is parsed and
        /// merged.
        /// </param>
        /// <param name="data">
        /// On input, supplies existing syntax data to merge into; this
        /// parameter may be null.  Upon success, receives the merged data.
        /// </param>
        /// <param name="errors">
        /// On input, supplies an existing error list to append to (one is
        /// created as needed); receives any per-file errors encountered.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode LoadSyntaxDataFrom(
            string directory,
            Encoding encoding,
            SyntaxDataFlags flags,
            ref SyntaxData data,
            ref ResultList errors
            )
        {
            return SyntaxOps.LoadDataFrom(
                directory, encoding, flags, ref data, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Retrieves clones of the character sets used when loading and
        /// parsing command syntax data.
        /// </summary>
        /// <param name="commentChars">
        /// Upon success, receives a clone of the comment character set.
        /// </param>
        /// <param name="lineChars">
        /// Upon success, receives a clone of the line-separator character
        /// set.
        /// </param>
        /// <param name="fieldChars">
        /// Upon success, receives a clone of the field-separator character
        /// set.
        /// </param>
        /// <param name="wrapChars">
        /// Upon success, receives a clone of the quote (wrap) character set.
        /// </param>
        /// <param name="escapeChars">
        /// Upon success, receives a clone of the escape character set.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// True if all character sets were valid and assigned; otherwise,
        /// false.
        /// </returns>
        public static bool GetSyntaxLoadChars(
            ref char[] commentChars,
            ref char[] lineChars,
            ref char[] fieldChars,
            ref char[] wrapChars,
            ref char[] escapeChars,
            ref Result error
            )
        {
            return SyntaxOps.GetLoadChars(
                ref commentChars, ref lineChars, ref fieldChars,
                ref wrapChars, ref escapeChars, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses delimited text into rows and fields, invoking a callback
        /// for each data row and skipping blank and comment lines.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="callback">
        /// The callback invoked once per parsed data row.
        /// </param>
        /// <param name="commentChars">
        /// The characters that introduce a comment line.
        /// </param>
        /// <param name="lineChars">
        /// The characters that separate lines.
        /// </param>
        /// <param name="fieldChars">
        /// The characters that separate fields within a line.
        /// </param>
        /// <param name="wrapChars">
        /// The characters that quote (wrap) a field value.
        /// </param>
        /// <param name="escapeChars">
        /// The characters that escape the following character.
        /// </param>
        /// <param name="flags">
        /// The flags controlling parsing behavior.
        /// </param>
        /// <param name="clientData">
        /// On input, supplies caller-specific data passed to the callback,
        /// which may replace it; this parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode ParseSyntaxData(
            string text,
            StringDataRowCallback callback,
            char[] commentChars,
            char[] lineChars,
            char[] fieldChars,
            char[] wrapChars,
            char[] escapeChars,
            SyntaxDataFlags flags,
            ref IClientData clientData,
            ref Result error
            )
        {
            return SyntaxOps.ParseData(
                text, callback, commentChars, lineChars,
                fieldChars, wrapChars, escapeChars, flags,
                ref clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          procedure management subsystem is completed.
        //
        /// <summary>
        /// Parses a list of argument specifiers (each a "{name ?default?}"
        /// sub-list) into a flat list of name/default pairs, validating that
        /// each name is a simple scalar variable name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to parse the list; this parameter
        /// may be null.
        /// </param>
        /// <param name="list1">
        /// The list of argument specifier strings to parse.
        /// </param>
        /// <param name="list2">
        /// On input, supplies an existing pair list to append to (a new one
        /// is created if it is null); upon success, receives the parsed
        /// name/default pairs.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetFormalArgumentNamesAndDefaults(
            Interpreter interpreter,
            StringList list1,
            ref StringPairList list2,
            ref Result error
            )
        {
            return _RuntimeOps.GetFormalArgumentNamesAndDefaults(
                interpreter, list1, ref list2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          procedure management subsystem is completed.
        //
        /// <summary>
        /// Builds a formal argument list and a name-keyed argument dictionary
        /// from a list of name/default pairs, rejecting duplicate names.
        /// </summary>
        /// <param name="procedureName">
        /// The procedure name used when formatting error messages; this
        /// parameter may be null.
        /// </param>
        /// <param name="list2">
        /// The list of name/default pairs to convert.
        /// </param>
        /// <param name="formalArguments">
        /// Upon success, receives the formal argument list built from the
        /// pairs.
        /// </param>
        /// <param name="namedArguments">
        /// Upon success, receives the argument dictionary keyed by argument
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode GetFormalAndNamedArguments(
            string procedureName,
            StringPairList list2,
            ref ArgumentList formalArguments,
            ref ArgumentDictionary namedArguments,
            ref Result error
            )
        {
            return _RuntimeOps.GetFormalAndNamedArguments(
                procedureName, list2, ref formalArguments,
                ref namedArguments, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          procedure management subsystem is completed.
        //
        /// <summary>
        /// Creates a new procedure object from the specified name, body,
        /// arguments, and flags, using the interpreter's procedure-creation
        /// callback when one is registered.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that will own the procedure; this parameter may
        /// be null.
        /// </param>
        /// <param name="name">
        /// The name of the procedure.
        /// </param>
        /// <param name="group">
        /// The group the procedure belongs to; this parameter may be null.
        /// </param>
        /// <param name="description">
        /// A description of the procedure; this parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the kind of procedure created.
        /// </param>
        /// <param name="arguments">
        /// The list of formal arguments.
        /// </param>
        /// <param name="namedArguments">
        /// The named arguments; this parameter may be null.
        /// </param>
        /// <param name="overwriteArguments">
        /// The arguments used to overwrite existing argument values; this
        /// parameter may be null.
        /// </param>
        /// <param name="cleanArguments">
        /// The arguments used for clean-up handling; this parameter may be
        /// null.
        /// </param>
        /// <param name="body">
        /// The script body of the procedure.
        /// </param>
        /// <param name="location">
        /// The script location associated with the procedure; this parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The opaque, caller-defined data to associate with the operation; this parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// The newly created procedure, or null on failure.
        /// </returns>
        public static IProcedure NewProcedure(
            Interpreter interpreter,
            string name,
            string group,
            string description,
            ProcedureFlags flags,
            ArgumentList arguments,
            ArgumentDictionary namedArguments,
            ArgumentList overwriteArguments,
            ArgumentList cleanArguments,
            string body,
            IScriptLocation location,
            IClientData clientData,
            ref Result error
            )
        {
            return _RuntimeOps.NewProcedure(
                interpreter, name, group, description, flags,
                arguments, namedArguments, overwriteArguments,
                cleanArguments, body, location, clientData,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          procedure management subsystem is completed.
        //
        /// <summary>
        /// Examines a procedure body's annotation comments to determine which
        /// procedure flags it should carry and extracts any overwrite and
        /// clean argument lists.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used during parsing; this parameter may
        /// be null.
        /// </param>
        /// <param name="name">
        /// The name of the procedure.
        /// </param>
        /// <param name="text">
        /// The procedure body text whose annotations are scanned.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to interpret annotation values; this parameter
        /// may be null.
        /// </param>
        /// <param name="isLibrary">
        /// Receives whether the procedure should be treated as a library
        /// procedure.
        /// </param>
        /// <param name="isPrivate">
        /// Receives whether the procedure should be private.
        /// </param>
        /// <param name="isFast">
        /// Receives whether the procedure should be fast.
        /// </param>
        /// <param name="isAtomic">
        /// Receives whether the procedure should be atomic.
        /// </param>
        /// <param name="isInline">
        /// Receives whether the procedure should be inline.
        /// </param>
        /// <param name="isNonCaching">
        /// Receives whether the procedure should disable caching.
        /// </param>
        /// <param name="isMatchTypes">
        /// Receives whether the procedure should match argument types.
        /// </param>
        /// <param name="overwriteArguments">
        /// Receives the overwrite argument list, or null if none was
        /// specified.
        /// </param>
        /// <param name="cleanArguments">
        /// Receives the clean argument list, or null if none was specified.
        /// </param>
        public static void ShouldProcedureHaveFlags(
            Interpreter interpreter,
            string name,
            string text,
            CultureInfo cultureInfo,
            out bool isLibrary,
            out bool isPrivate,
            out bool isFast,
            out bool isAtomic,
            out bool isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
            out bool isNonCaching,
#endif
            out bool isMatchTypes,
            out ArgumentList overwriteArguments,
            out ArgumentList cleanArguments
            )
        {
            ScriptOps.ShouldProcedureHaveFlags(
                interpreter, name, text, cultureInfo, out isLibrary,
                out isPrivate, out isFast, out isAtomic, out isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
                out isNonCaching,
#endif
                out isMatchTypes, out overwriteArguments, out cleanArguments);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          procedure management subsystem is completed.
        //
        /// <summary>
        /// Validates the requested combination of procedure attributes and,
        /// when valid, merges the corresponding flag bits into the supplied
        /// procedure flags.
        /// </summary>
        /// <param name="isLibrary">
        /// Non-zero to request the library flag; otherwise, zero.
        /// </param>
        /// <param name="isPrivate">
        /// Non-zero to request the private flag; otherwise, zero.
        /// </param>
        /// <param name="isFast">
        /// Non-zero to request the fast flag; otherwise, zero.
        /// </param>
        /// <param name="isAtomic">
        /// Non-zero to request the atomic flag; otherwise, zero.
        /// </param>
        /// <param name="isInline">
        /// Non-zero to request the inline flag; otherwise, zero.
        /// </param>
        /// <param name="isNonCaching">
        /// Non-zero to request the non-caching flag; otherwise, zero.
        /// </param>
        /// <param name="isMatchTypes">
        /// Non-zero to request the match-types flag; otherwise, zero.
        /// </param>
        /// <param name="procedureFlags">
        /// On input, supplies the existing procedure flags; upon success,
        /// receives those flags with the requested bits merged in.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode SanityCheckAndModifyProcedureFlags(
            bool isLibrary,
            bool isPrivate,
            bool isFast,
            bool isAtomic,
            bool isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
            bool isNonCaching,
#endif
            bool isMatchTypes,
            ref ProcedureFlags procedureFlags,
            ref Result error
            )
        {
            return ScriptOps.SanityCheckAndModifyProcedureFlags(
                isLibrary, isPrivate, isFast, isAtomic,
                isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
                isNonCaching,
#endif
                isMatchTypes, ref procedureFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          namespace management subsystem is completed.
        //
        /// <summary>
        /// Qualifies a name relative to the interpreter's current namespace,
        /// optionally forcing the result to be absolute.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose current namespace is used; this parameter
        /// must not be null.
        /// </param>
        /// <param name="name">
        /// The name to qualify; this parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to force the qualified name to be absolute; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// The qualified name, or null if <paramref name="name"/> is null.
        /// </returns>
        public static string MakeQualifiedName(
            Interpreter interpreter,
            string name,
            bool absolute
            )
        {
            return NamespaceOps.MakeQualifiedName(
                interpreter, name, absolute);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          namespace management subsystem is completed.
        //
        /// <summary>
        /// Returns only the tail (last component) of a qualified name,
        /// discarding any namespace qualifiers.
        /// </summary>
        /// <param name="name">
        /// The qualified name whose tail is wanted; this parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The tail component of the name, or null if
        /// <paramref name="name"/> is null.
        /// </returns>
        public static string TailOnly(
            string name
            )
        {
            return NamespaceOps.TailOnly(name);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// Attaches to the parent process's console or, failing that,
        /// allocates a new native console window.
        /// </summary>
        /// <param name="force">
        /// Non-zero to attach or allocate even when a console already
        /// appears to be open; otherwise, zero.
        /// </param>
        /// <param name="attach">
        /// Non-zero to first attempt attaching to the parent process's
        /// console; otherwise, zero.
        /// </param>
        /// <param name="attached">
        /// Upon success, receives true if the parent console was attached or
        /// false if a new console was allocated; it may be left unchanged
        /// when a console is already open.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode AttachOrOpenNativeConsole(
            bool force,
            bool attach,
            ref bool? attached,
            ref Result error
            )
        {
            return NativeConsole.AttachOrOpen(
                force, attach, ref attached, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Frees the calling process's native console and resets the
        /// associated handles and screen buffers.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing
        /// the problem.
        /// </param>
        /// <returns>
        /// A <see cref="ReturnCode" /> value indicating the success or failure
        /// of the operation.
        /// </returns>
        public static ReturnCode CloseNativeConsole(
            ref Result error
            )
        {
            return NativeConsole.Close(ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        #region .NET Core Wrapper Methods
        //
        // HACK: These wrapper methods are primarily for use by the test
        //       suite due to a bug in the .NET Core runtime, see:
        //
        //       https://github.com/dotnet/coreclr/issues/15662
        //
        /// <summary>
        /// Resolves an assembly-qualified type name to a type using the
        /// standard runtime type resolution.
        /// </summary>
        /// <param name="typeName">
        /// The assembly-qualified name of the type to resolve.
        /// </param>
        /// <returns>
        /// The resolved type, or null if it cannot be found.
        /// </returns>
        public static Type GetType(
            string typeName
            )
        {
            return Type.GetType(typeName);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These wrapper methods are primarily for use by the test
        //       suite due to a bug in the .NET Core runtime, see:
        //
        //       https://github.com/dotnet/coreclr/issues/15662
        //
        /// <summary>
        /// Resolves an assembly-qualified type name to a type, with control
        /// over error throwing and case sensitivity.
        /// </summary>
        /// <param name="typeName">
        /// The assembly-qualified name of the type to resolve.
        /// </param>
        /// <param name="throwOnError">
        /// Non-zero to throw an exception when the type cannot be found;
        /// otherwise, zero.
        /// </param>
        /// <param name="ignoreCase">
        /// Non-zero to perform a case-insensitive search; otherwise, zero.
        /// </param>
        /// <returns>
        /// The resolved type, or null if it cannot be found and
        /// <paramref name="throwOnError"/> is zero.
        /// </returns>
        public static Type GetType(
            string typeName,
            bool throwOnError,
            bool ignoreCase
            )
        {
            return Type.GetType(typeName, throwOnError, ignoreCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves a type name to a type, searching the interpreter's
        /// loaded assemblies and applying the default value flags; any
        /// failure is reported via the trace mechanism.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The type name to resolve.
        /// </param>
        /// <param name="objectTypes">
        /// The candidate object types to consider; this parameter may be
        /// null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags controlling how the type name is parsed; this
        /// parameter may be null to use the default flags.
        /// </param>
        /// <returns>
        /// The resolved type, or null if it cannot be found.
        /// </returns>
        public static Type GetAnyType(
            Interpreter interpreter, /* in: OPTIONAL */
            string typeName,         /* in */
            TypeList objectTypes,    /* in: OPTIONAL */
            ValueFlags? valueFlags   /* in: OPTIONAL */
            )
        {
            ResultList errors = null;

            return GetAnyType(
                interpreter, typeName, objectTypes,
                valueFlags, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves a type name to a type, searching the interpreter's
        /// loaded assemblies and applying the default value flags,
        /// accumulating any errors encountered.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that supplies the execution context; this parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The type name to resolve.
        /// </param>
        /// <param name="objectTypes">
        /// The candidate object types to consider; this parameter may be
        /// null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags controlling how the type name is parsed; this
        /// parameter may be null to use the default flags.
        /// </param>
        /// <param name="errors">
        /// On input, supplies an existing error list to append to (one is
        /// created as needed); upon failure, receives the accumulated
        /// errors.
        /// </param>
        /// <returns>
        /// The resolved type, or null if it cannot be found.
        /// </returns>
        public static Type GetAnyType(
            Interpreter interpreter, /* in: OPTIONAL */
            string typeName,         /* in */
            TypeList objectTypes,    /* in: OPTIONAL */
            ValueFlags? valueFlags,  /* in: OPTIONAL */
            ref ResultList errors    /* in, out */
            )
        {
            if (interpreter == null)
                interpreter = Interpreter.GetAny();

            AppDomain appDomain;
            CultureInfo cultureInfo;

            if (interpreter != null)
            {
                appDomain = interpreter.GetAppDomain();
                cultureInfo = interpreter.InternalCultureInfo;
            }
            else
            {
                appDomain = AppDomainOps.GetCurrent();
                cultureInfo = Value.GetDefaultCulture();
            }

            //
            // HACK: Make sure that value of Defaults.ValueFlags
            //       gets used for the final value flags.
            //
            ValueFlags localValueFlags = Value.GetTypeValueFlags(
                (valueFlags != null) ? (ValueFlags)valueFlags :
                ValueFlags.None, false, false, false, false);

            Type type = null;

            if (Value.GetAnyType(
                    interpreter, typeName, objectTypes,
                    appDomain, localValueFlags, cultureInfo,
                    ref type, ref errors) == ReturnCode.Ok)
            {
                return type;
            }
            else
            {
                TraceOps.DebugTrace(
                    (Result)errors, typeof(Utility).Name,
                    TracePriority.MarshalError);

                return null;
            }
        }
        #endregion
        #endregion
    }
}
