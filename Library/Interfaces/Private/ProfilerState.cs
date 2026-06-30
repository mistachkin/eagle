/*
 * ProfilerState.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by entities that measure and report the
    /// elapsed time of an operation.  It composes the standard disposal
    /// contract (<see cref="IDisposable" />) and disposal-state reporting
    /// (<see cref="IMaybeDisposed" />), and exposes methods to start, stop, and
    /// query a profiling measurement.
    /// </summary>
    [ObjectId("9f2e1444-ac22-4a42-8b31-76489caab961")]
    internal interface IProfilerState : IDisposable, IMaybeDisposed
    {
        /// <summary>
        /// This method returns the most recently measured elapsed time, in
        /// milliseconds.
        /// </summary>
        /// <returns>
        /// The elapsed time, in milliseconds, or null if no measurement is
        /// available.
        /// </returns>
        long? GetMilliseconds();

        /// <summary>
        /// This method begins a new profiling measurement, recording the
        /// current starting time.
        /// </summary>
        void Start();

        /// <summary>
        /// This method ends the current profiling measurement and returns the
        /// elapsed time.
        /// </summary>
        /// <returns>
        /// The elapsed time, in milliseconds, since the matching call to
        /// <see cref="Start" />.
        /// </returns>
        double Stop();

        /// <summary>
        /// This method ends the current profiling measurement and returns the
        /// elapsed time, optionally obfuscating the reported value.
        /// </summary>
        /// <param name="obfuscate">
        /// Non-zero to obfuscate the returned elapsed time; otherwise, the
        /// precise elapsed time is returned.
        /// </param>
        /// <returns>
        /// The elapsed time, in milliseconds, since the matching call to
        /// <see cref="Start" />, possibly obfuscated.
        /// </returns>
        double Stop(bool obfuscate);

        /// <summary>
        /// This method returns the current profiling state as a list of
        /// strings suitable for display or diagnostics.
        /// </summary>
        /// <returns>
        /// A list of strings representing the current profiling state.
        /// </returns>
        IStringList ToList();
    }
}
