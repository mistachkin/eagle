/*
 * Event.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a single asynchronous event that has been
    /// queued for execution within an interpreter.  It exposes the event's
    /// identifying metadata (type, flags, priority, scheduled time, owning
    /// thread, and callback) and provides operations to obtain, reset, and set
    /// the result produced when the event is processed.
    /// </summary>
    [ObjectId("9c55849d-5347-4f5d-8c56-80d306d4f688")]
    public interface IEvent : ISynchronize, IIdentifier, IGetInterpreter
    {
        /// <summary>
        /// Gets the <see cref="System.Delegate" /> associated with this event,
        /// if any.  This value may be null.
        /// </summary>
        Delegate Delegate { get; }

        /// <summary>
        /// Gets the <see cref="EventType" /> that categorizes this event.
        /// </summary>
        EventType Type { get; }

        /// <summary>
        /// Gets the <see cref="EventFlags" /> that further describe this event
        /// and how it should be processed.
        /// </summary>
        EventFlags Flags { get; }

        /// <summary>
        /// Gets the <see cref="EventPriority" /> that determines this event's
        /// ordering relative to other queued events.
        /// </summary>
        EventPriority Priority { get; }

        /// <summary>
        /// Gets the <see cref="System.DateTime" /> at which this event is
        /// scheduled to be processed.
        /// </summary>
        DateTime DateTime { get; }

        /// <summary>
        /// Gets the <see cref="EventCallback" /> delegate that is invoked when
        /// this event is processed, if any.  This value may be null.
        /// </summary>
        EventCallback Callback { get; }

        /// <summary>
        /// Gets the identifier of the thread that this event is associated
        /// with, if any.  This value may be null.
        /// </summary>
        long? ThreadId { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Obtains the result produced by processing this event, optionally
        /// waiting for the event to be processed.
        /// </summary>
        /// <param name="wait">
        /// Non-zero to wait indefinitely for the event to be processed before
        /// returning its result; otherwise, return immediately.
        /// </param>
        /// <param name="returnCode">
        /// Upon success, this will receive the <see cref="ReturnCode" />
        /// produced when the event was processed.
        /// </param>
        /// <param name="result">
        /// Upon success, this will receive the result value produced when the
        /// event was processed.
        /// </param>
        /// <param name="errorLine">
        /// Upon success, this will receive the script line number associated
        /// with the result, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was obtained successfully; otherwise, false.
        /// </returns>
        bool GetResult(
            bool wait,
            ref ReturnCode returnCode,
            ref Result result,
            ref int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Obtains the result produced by processing this event, optionally
        /// waiting up to the specified timeout for the event to be processed.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the event
        /// to be processed.
        /// </param>
        /// <param name="wait">
        /// Non-zero to wait for the event to be processed before returning its
        /// result; otherwise, return immediately.
        /// </param>
        /// <param name="returnCode">
        /// Upon success, this will receive the <see cref="ReturnCode" />
        /// produced when the event was processed.
        /// </param>
        /// <param name="result">
        /// Upon success, this will receive the result value produced when the
        /// event was processed.
        /// </param>
        /// <param name="errorLine">
        /// Upon success, this will receive the script line number associated
        /// with the result, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was obtained successfully; otherwise, false.
        /// </returns>
        bool GetResult(
            int timeout,
            bool wait,
            ref ReturnCode returnCode,
            ref Result result,
            ref int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the result state associated with this event so that a new
        /// result may be produced.
        /// </summary>
        /// <param name="signal">
        /// Non-zero to signal any threads waiting on this event's result after
        /// the reset has been performed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result state was reset successfully; otherwise, false.
        /// </returns>
        bool ResetResult(
            bool signal,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the result state associated with this event so that a new
        /// result may be produced, optionally waiting up to the specified
        /// timeout to acquire any required synchronization.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait while
        /// performing the reset.
        /// </param>
        /// <param name="signal">
        /// Non-zero to signal any threads waiting on this event's result after
        /// the reset has been performed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result state was reset successfully; otherwise, false.
        /// </returns>
        bool ResetResult(
            int timeout,
            bool signal,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the result associated with this event, making it available to
        /// callers obtaining the result.
        /// </summary>
        /// <param name="signal">
        /// Non-zero to signal any threads waiting on this event's result after
        /// it has been set.
        /// </param>
        /// <param name="returnCode">
        /// The <see cref="ReturnCode" /> to associate with this event's
        /// result.
        /// </param>
        /// <param name="result">
        /// The result value to associate with this event.  This parameter may
        /// be null.
        /// </param>
        /// <param name="errorLine">
        /// The script line number to associate with this event's result, if
        /// any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was set successfully; otherwise, false.
        /// </returns>
        bool SetResult(
            bool signal,
            ReturnCode returnCode,
            Result result,
            int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the result associated with this event, making it available to
        /// callers obtaining the result, optionally waiting up to the
        /// specified timeout to acquire any required synchronization.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait while setting
        /// the result.
        /// </param>
        /// <param name="signal">
        /// Non-zero to signal any threads waiting on this event's result after
        /// it has been set.
        /// </param>
        /// <param name="returnCode">
        /// The <see cref="ReturnCode" /> to associate with this event's
        /// result.
        /// </param>
        /// <param name="result">
        /// The result value to associate with this event.  This parameter may
        /// be null.
        /// </param>
        /// <param name="errorLine">
        /// The script line number to associate with this event's result, if
        /// any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was set successfully; otherwise, false.
        /// </returns>
        bool SetResult(
            int timeout,
            bool signal,
            ReturnCode returnCode,
            Result result,
            int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds and returns a list of name/value pairs describing the
        /// current state of this event.
        /// </summary>
        /// <returns>
        /// A <see cref="StringPairList" /> describing this event.
        /// </returns>
        StringPairList ToList();
    }
}
