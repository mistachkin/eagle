/*
 * Diagnostic.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Hosts
{
    /// <summary>
    /// This class implements a non-interactive host whose input and output are
    /// directed to the diagnostic tracing and debugging subsystem instead of a
    /// console.  It is derived from <see cref="Core" /> and provides a minimal
    /// <see cref="IHost" /> implementation: it exposes no real console, reports
    /// no interactive input source, performs no colorization, sizing, or cursor
    /// positioning, and routes all written output (including debug and error
    /// output) through the debugging and tracing facilities.  It is primarily
    /// useful for capturing or discarding interpreter output during
    /// diagnostics and testing.
    /// </summary>
    [ObjectId("9372a55b-ebc4-4745-a4e0-ce73fdc1fe39")]
    public class Diagnostic : Core, IDisposable
    {
        #region Diagnostic Stream Class
        /// <summary>
        /// This class implements a write-mostly <see cref="Stream" /> whose
        /// data is forwarded to the diagnostic tracing subsystem.  It is used
        /// to back the standard input, output, and error streams of the
        /// containing <see cref="Diagnostic" /> host.  Reads always return no
        /// data and most stream capabilities (seeking, length, and position)
        /// are not supported.
        /// </summary>
        [ObjectId("6e9e17a2-da98-4ab5-87a9-0c4a0483494b")]
        private sealed class DiagnosticStream : Stream
        {
            #region Public Constructors
            /// <summary>
            /// Constructs a new instance of this stream, specifying whether
            /// reading and writing are permitted.
            /// </summary>
            /// <param name="canRead">
            /// Non-zero if this stream should report that it supports reading.
            /// </param>
            /// <param name="canWrite">
            /// Non-zero if this stream should report that it supports writing.
            /// </param>
            public DiagnosticStream(
                bool canRead,
                bool canWrite
                )
            {
                this.canRead = canRead;
                this.canWrite = canWrite;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Stream Members
            /// <summary>
            /// Stores a value indicating whether this stream supports reading.
            /// </summary>
            private bool canRead;
            /// <summary>
            /// Gets a value indicating whether this stream supports reading.
            /// </summary>
            public override bool CanRead
            {
                get { CheckDisposed(); return canRead; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Gets a value indicating whether this stream supports seeking;
            /// this stream never supports seeking, so this property always
            /// returns false.
            /// </summary>
            public override bool CanSeek
            {
                get { CheckDisposed(); return false; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Stores a value indicating whether this stream supports writing.
            /// </summary>
            private bool canWrite;
            /// <summary>
            /// Gets a value indicating whether this stream supports writing.
            /// </summary>
            public override bool CanWrite
            {
                get { CheckDisposed(); return canWrite; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method flushes any buffered diagnostic output to the
            /// tracing subsystem.
            /// </summary>
            public override void Flush()
            {
                CheckDisposed();

                DebugOps.TraceFlush();
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Gets the length, in bytes, of this stream; this operation is not
            /// supported and always throws
            /// <see cref="NotSupportedException" />.
            /// </summary>
            public override long Length
            {
                get { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Gets or sets the current position within this stream; this
            /// operation is not supported and always throws
            /// <see cref="NotSupportedException" />.
            /// </summary>
            public override long Position
            {
                get { CheckDisposed(); throw new NotSupportedException(); }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method reads a sequence of bytes from this stream; this
            /// stream has no input source, so it logs the request and always
            /// reads no bytes.
            /// </summary>
            /// <param name="buffer">
            /// The buffer into which data would be read.  This parameter should
            /// not be null.
            /// </param>
            /// <param name="offset">
            /// The zero-based offset within <paramref name="buffer" /> at which
            /// to begin storing data.
            /// </param>
            /// <param name="count">
            /// The maximum number of bytes to read.
            /// </param>
            /// <returns>
            /// The number of bytes read, which is always zero for this stream.
            /// </returns>
            public override int Read(
                byte[] buffer,
                int offset,
                int count
                )
            {
                CheckDisposed();

                if (!canRead)
                    throw new NotSupportedException();

                if (buffer == null)
                    throw new ArgumentNullException();

                if ((offset < 0) || (count < 0))
                    throw new ArgumentOutOfRangeException();

                int length = buffer.Length;

                if ((offset + count) > length)
                    throw new ArgumentException();

                //
                // NOTE: Log the read request because it should be somewhat
                //       unusual.
                //
                DebugOps.TraceWriteLineFormatted(String.Format(
                    "Read: request for {0} bytes starting at offset {1}",
                    count, offset), typeof(DiagnosticStream).Name); /* EXEMPT */

                return 0;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the current position within this stream; this
            /// operation is not supported and always throws
            /// <see cref="NotSupportedException" />.
            /// </summary>
            /// <param name="offset">
            /// The offset, relative to <paramref name="origin" />, of the new
            /// position.
            /// </param>
            /// <param name="origin">
            /// The reference point used to obtain the new position.
            /// </param>
            /// <returns>
            /// The new position within this stream; this method never returns
            /// normally.
            /// </returns>
            public override long Seek(
                long offset,
                SeekOrigin origin
                )
            {
                CheckDisposed();

                throw new NotSupportedException();
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the length of this stream; this operation is
            /// not supported and always throws
            /// <see cref="NotSupportedException" />.
            /// </summary>
            /// <param name="value">
            /// The desired length, in bytes, of this stream.
            /// </param>
            public override void SetLength(
                long value
                )
            {
                CheckDisposed();

                throw new NotSupportedException();
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method writes a sequence of bytes to this stream by
            /// converting them to characters and forwarding them to the
            /// diagnostic tracing subsystem.
            /// </summary>
            /// <param name="buffer">
            /// The buffer containing the data to write.  This parameter should
            /// not be null.
            /// </param>
            /// <param name="offset">
            /// The zero-based offset within <paramref name="buffer" /> at which
            /// the data to write begins.
            /// </param>
            /// <param name="count">
            /// The number of bytes to write.
            /// </param>
            public override void Write(
                byte[] buffer,
                int offset,
                int count
                )
            {
                CheckDisposed();

                if (!canWrite)
                    throw new NotSupportedException();

                if (buffer == null)
                    throw new ArgumentNullException();

                if ((offset < 0) || (count < 0))
                    throw new ArgumentOutOfRangeException();

                int length = buffer.Length;

                if ((offset + count) > length)
                    throw new ArgumentException();

                StringBuilder builder = StringBuilderFactory.Create(count);

                for (int index = offset; count > 0; index++, count--)
                    builder.Append(ConversionOps.ToChar(buffer[index]));

                DebugOps.TraceWrite(StringBuilderCache.GetStringAndRelease(
                    ref builder)); /* EXEMPT */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            /// <summary>
            /// Stores a value indicating whether this stream has been disposed.
            /// </summary>
            private bool disposed;
            /// <summary>
            /// This method throws an exception if this stream has already been
            /// disposed.  It is called at the start of most members to guard
            /// against use after disposal.
            /// </summary>
            /// <exception cref="InterpreterDisposedException">
            /// Thrown when this stream has been disposed and the engine is
            /// configured to throw on use of a disposed object.
            /// </exception>
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed &&
                    _Engine.IsThrowOnDisposed(null, false))
                {
                    throw new InterpreterDisposedException(
                        typeof(DiagnosticStream));
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method releases the resources held by this stream.  It
            /// implements the standard dispose pattern.
            /// </summary>
            /// <param name="disposing">
            /// Non-zero if this method is being called from
            /// <see cref="Stream.Dispose()" /> (i.e. deterministically); zero
            /// if it is being called from the finalizer.  When non-zero,
            /// managed resources are released.
            /// </param>
            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////
                    }
                }
                finally
                {
                    base.Dispose(disposing);

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Destructor
            /// <summary>
            /// Finalizes this stream, releasing any resources that were not
            /// released by an explicit call to <see cref="Stream.Dispose()" />.
            /// </summary>
            ~DiagnosticStream()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this host using the specified host
        /// data and sets up its diagnostic input, output, and error streams.
        /// </summary>
        /// <param name="hostData">
        /// The data used to initialize this host, including its name,
        /// associated interpreter, and creation flags.  This parameter may be
        /// null.
        /// </param>
        public Diagnostic(
            IHostData hostData
            )
            : base(hostData)
        {
            //
            // NOTE: Setup the output and error streams.
            //
            input = new DiagnosticStream(true, false);
            output = new DiagnosticStream(false, true);
            error = new DiagnosticStream(false, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        /// <summary>
        /// This method invalidates the cached host flags so that they will be
        /// recomputed the next time they are requested.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invalidates the cached host flags and then resets the
        /// remaining host flag state via the base class.
        /// </summary>
        /// <returns>
        /// True if the host flags were reset; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes and caches the flags describing the
        /// capabilities of this host, combining the diagnostic-specific flags
        /// with those supplied by the base class.
        /// </summary>
        /// <returns>
        /// The flags describing the capabilities of this host.
        /// </returns>
        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support no colors (i.e. monochrome) and
                //       unlimited text output.
                //
                hostFlags = HostFlags.Monochrome | HostFlags.Text |
                            HostFlags.UnlimitedSize |
                            base.MaybeInitializeHostFlags();
            }

            return hostFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        /// <summary>
        /// This method refreshes the window or console title of this host;
        /// this host has no title, so this method does nothing and succeeds.
        /// </summary>
        /// <returns>
        /// True if the title was refreshed; otherwise, false.
        /// </returns>
        public override bool RefreshTitle()
        {
            CheckDisposed();

            //
            // NOTE: We have no title; therefore, just succeed.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the interactive input of this host
        /// has been redirected.  This host has no interactive input source, so
        /// its input is always considered redirected.
        /// </summary>
        /// <returns>
        /// True if input is redirected; otherwise, false.
        /// </returns>
        public override bool IsInputRedirected()
        {
            CheckDisposed();

            //
            // NOTE: We have no input stream; therefore, the input
            //       must come from somewhere else (i.e. a file).
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this host is currently open; this
        /// host is always considered open.
        /// </summary>
        /// <returns>
        /// True if this host is open; otherwise, false.
        /// </returns>
        public override bool IsOpen()
        {
            CheckDisposed();

            /* ALWAYS OPEN */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pauses interactive input or output for this host; this
        /// host does not support pausing, so this method does nothing and
        /// fails.
        /// </summary>
        /// <returns>
        /// True if the host was paused; otherwise, false.
        /// </returns>
        public override bool Pause()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered interactive output for this host;
        /// this host buffers no such output, so this method does nothing and
        /// fails.
        /// </summary>
        /// <returns>
        /// True if the output was flushed; otherwise, false.
        /// </returns>
        public override bool Flush()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached flags describing the capabilities of this host, or
        /// <see cref="HostFlags.Invalid" /> when they have not yet been
        /// computed.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;
        /// <summary>
        /// This method returns the flags describing the capabilities of this
        /// host, computing them if necessary.
        /// </summary>
        /// <returns>
        /// The flags describing the capabilities of this host.
        /// </returns>
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of nested read operations currently in progress for
        /// this host; this host never reads from the user, so this property
        /// always returns zero.
        /// </summary>
        public override int ReadLevels
        {
            get
            {
                CheckDisposed();

                /* NEVER READING FROM USER */
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of nested write operations currently in progress for
        /// this host; this host never writes to the user, so this property
        /// always returns zero.
        /// </summary>
        public override int WriteLevels
        {
            get
            {
                CheckDisposed();

                /* NEVER WRITING TO USER */
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a line of interactive input from this host; this
        /// host has no input source, so it returns a null line and succeeds.
        /// </summary>
        /// <param name="value">
        /// Upon return, this is set to the line that was read, which is always
        /// null for this host.
        /// </param>
        /// <returns>
        /// True if the read succeeded; otherwise, false.
        /// </returns>
        public override bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            //
            // NOTE: We have no input source; indicate this to the caller.
            //
            value = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes an end-of-line to the diagnostic tracing
        /// subsystem.
        /// </summary>
        /// <returns>
        /// True if the end-of-line was written; otherwise, false.
        /// </returns>
        public override bool WriteLine()
        {
            CheckDisposed();

            try
            {
                DebugOps.TraceWriteLine(null); /* EXEMPT */

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        /// <summary>
        /// Stores the stream used as the standard input of this host.
        /// </summary>
        private Stream input;
        /// <summary>
        /// Gets or sets the stream used as the standard input of this host.
        /// </summary>
        public override Stream In
        {
            get { CheckDisposed(); return input; }
            set { CheckDisposed(); input = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the stream used as the standard output of this host.
        /// </summary>
        private Stream output;
        /// <summary>
        /// Gets or sets the stream used as the standard output of this host.
        /// </summary>
        public override Stream Out
        {
            get { CheckDisposed(); return output; }
            set { CheckDisposed(); output = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the stream used as the standard error of this host.
        /// </summary>
        private Stream error;
        /// <summary>
        /// Gets or sets the stream used as the standard error of this host.
        /// </summary>
        public override Stream Error
        {
            get { CheckDisposed(); return error; }
            set { CheckDisposed(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the standard input of this host;
        /// this host uses no encoding, so the value is always null and setting
        /// it has no effect.
        /// </summary>
        public override Encoding InputEncoding
        {
            get { CheckDisposed(); return null; }
            set { CheckDisposed(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the standard output of this host;
        /// this host uses no encoding, so the value is always null and setting
        /// it has no effect.
        /// </summary>
        public override Encoding OutputEncoding
        {
            get { CheckDisposed(); return null; }
            set { CheckDisposed(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used for the standard error of this host;
        /// this host uses no encoding, so the value is always null and setting
        /// it has no effect.
        /// </summary>
        public override Encoding ErrorEncoding
        {
            get { CheckDisposed(); return null; }
            set { CheckDisposed(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the standard input stream of this host; this host
        /// does not support resetting its input, so this method does nothing and
        /// fails.
        /// </summary>
        /// <returns>
        /// True if the input was reset; otherwise, false.
        /// </returns>
        public override bool ResetIn()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the standard output stream of this host; this
        /// host does not support resetting its output, so this method does
        /// nothing and fails.
        /// </summary>
        /// <returns>
        /// True if the output was reset; otherwise, false.
        /// </returns>
        public override bool ResetOut()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the standard error stream of this host; this host
        /// does not support resetting its error stream, so this method does
        /// nothing and fails.
        /// </summary>
        /// <returns>
        /// True if the error stream was reset; otherwise, false.
        /// </returns>
        public override bool ResetError()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the standard output of this host has
        /// been redirected; this host does not consider its output redirected.
        /// </summary>
        /// <returns>
        /// True if output is redirected; otherwise, false.
        /// </returns>
        public override bool IsOutputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the standard error of this host has
        /// been redirected; this host does not consider its error stream
        /// redirected.
        /// </summary>
        /// <returns>
        /// True if the error stream is redirected; otherwise, false.
        /// </returns>
        public override bool IsErrorRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets up the standard channels of this host; this host
        /// does not set up any channels, so this method does nothing and fails.
        /// </summary>
        /// <returns>
        /// True if the channels were set up; otherwise, false.
        /// </returns>
        public override bool SetupChannels()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// This method creates a copy of this host that is associated with the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter with which the cloned host should be associated.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created host.
        /// </returns>
        public override IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Diagnostic(new HostData(
                Name, Group, Description, ClientData, TypeName,
                interpreter, ResourceManager, Profile, HostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached flags controlling the testing behavior of this
        /// host, or <see cref="HostTestFlags.Invalid" /> when they have not yet
        /// been computed.
        /// </summary>
        private HostTestFlags hostTestFlags = HostTestFlags.Invalid;
        /// <summary>
        /// This method returns the flags controlling the testing behavior of
        /// this host, computing them if necessary.
        /// </summary>
        /// <returns>
        /// The flags controlling the testing behavior of this host.
        /// </returns>
        public override HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            if (hostTestFlags == HostTestFlags.Invalid)
                hostTestFlags = HostTestFlags.None;

            return hostTestFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests cancellation of the script currently being
        /// evaluated; this host does not implement cancellation and always
        /// fails.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force the cancellation even when it would normally be
        /// refused.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the interactive loop or process exit; this
        /// host does not implement exiting and always fails.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force the exit even when it would normally be refused.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes an end-of-line to the debugging output.
        /// </summary>
        /// <returns>
        /// True if the end-of-line was written; otherwise, false.
        /// </returns>
        public override bool WriteDebugLine()
        {
            CheckDisposed();

            try
            {
                DebugOps.DebugWriteLine(null);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the debugging output,
        /// optionally followed by an end-of-line.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write an end-of-line after the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public override bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            try
            {
                if (newLine)
                    DebugOps.DebugWriteLine(value);
                else
                    DebugOps.DebugWrite(value);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the debugging output, optionally
        /// followed by an end-of-line.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write an end-of-line after the string.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public override bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            try
            {
                if (newLine)
                    DebugOps.DebugWriteLine(value);
                else
                    DebugOps.DebugWrite(value);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes an end-of-line to the error (tracing) output.
        /// </summary>
        /// <returns>
        /// True if the end-of-line was written; otherwise, false.
        /// </returns>
        public override bool WriteErrorLine()
        {
            CheckDisposed();

            try
            {
                DebugOps.TraceWriteLine(null); /* EXEMPT */

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the error (tracing)
        /// output, optionally followed by an end-of-line.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write an end-of-line after the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public override bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            try
            {
                if (newLine)
                    DebugOps.TraceWriteLine(value); /* EXEMPT */
                else
                    DebugOps.TraceWrite(value); /* EXEMPT */

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the error (tracing) output,
        /// optionally followed by an end-of-line.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write an end-of-line after the string.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public override bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            try
            {
                if (newLine)
                    DebugOps.TraceWriteLine(value); /* EXEMPT */
                else
                    DebugOps.TraceWrite(value); /* EXEMPT */

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        /// <summary>
        /// This method writes host-specific custom information; this host emits
        /// no custom information, so this method does nothing and succeeds.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose information would be written.  This parameter
        /// may be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that select how much detail would be written.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write an end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color that would be used.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color that would be used.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public override bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        /// <summary>
        /// This method begins a visual box used to group related output; this
        /// host draws no boxes, so this method does nothing and succeeds.
        /// </summary>
        /// <param name="name">
        /// The name of the box to begin.  This parameter should not be null.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs to display in the box, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the box, if any.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// True if the box was begun; otherwise, false.
        /// </returns>
        public override bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a visual box previously begun with
        /// <see cref="BeginBox" />; this host draws no boxes, so this method
        /// does nothing and succeeds.
        /// </summary>
        /// <param name="name">
        /// The name of the box to end.  This parameter should not be null.
        /// </param>
        /// <param name="list">
        /// The list of name and value pairs to display in the box, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the box, if any.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// True if the box was ended; otherwise, false.
        /// </returns>
        public override bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        /// <summary>
        /// This method resets the foreground and background colors of this host
        /// to their defaults; this host has no colors, so this method does
        /// nothing and fails.
        /// </summary>
        /// <returns>
        /// True if the colors were reset; otherwise, false.
        /// </returns>
        public override bool ResetColors()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current foreground and background colors
        /// of this host; this host has no colors, so the supplied values are
        /// ignored and left unchanged.
        /// </summary>
        /// <param name="foregroundColor">
        /// Upon return, this would contain the current foreground color; it is
        /// left unchanged by this host.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon return, this would contain the current background color; it is
        /// left unchanged by this host.
        /// </param>
        /// <returns>
        /// True if the colors were retrieved; otherwise, false.
        /// </returns>
        public override bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            /* IGNORED */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the supplied foreground and background colors as
        /// appropriate for this host; this host has no colors, so the supplied
        /// values are ignored and left unchanged.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to adjust; it is left unchanged by this host.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to adjust; it is left unchanged by this host.
        /// </param>
        /// <returns>
        /// True if the colors were adjusted; otherwise, false.
        /// </returns>
        public override bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            /* IGNORED */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the foreground color of this host; this host has no
        /// colors, so the supplied value is ignored.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to set; it is ignored by this host.
        /// </param>
        /// <returns>
        /// True if the foreground color was set; otherwise, false.
        /// </returns>
        public override bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            /* IGNORED */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the background color of this host; this host has no
        /// colors, so the supplied value is ignored.
        /// </summary>
        /// <param name="backgroundColor">
        /// The background color to set; it is ignored by this host.
        /// </param>
        /// <returns>
        /// True if the background color was set; otherwise, false.
        /// </returns>
        public override bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            /* IGNORED */
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        /// <summary>
        /// This method retrieves the current cursor position of this host; this
        /// host has no cursor, so this method does nothing and fails.
        /// </summary>
        /// <param name="left">
        /// Upon success, this would contain the zero-based column of the cursor.
        /// </param>
        /// <param name="top">
        /// Upon success, this would contain the zero-based row of the cursor.
        /// </param>
        /// <returns>
        /// True if the position was retrieved; otherwise, false.
        /// </returns>
        public override bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the current cursor position of this host; this host
        /// has no cursor, so this method does nothing and fails.
        /// </summary>
        /// <param name="left">
        /// The zero-based column to which the cursor would be moved.
        /// </param>
        /// <param name="top">
        /// The zero-based row to which the cursor would be moved.
        /// </param>
        /// <returns>
        /// True if the position was set; otherwise, false.
        /// </returns>
        public override bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        /// <summary>
        /// This method resets the size of the specified host dimension to its
        /// default; this host has no size, so this method does nothing and
        /// fails.
        /// </summary>
        /// <param name="hostSizeType">
        /// The type of size to reset (for example, the buffer or the window).
        /// </param>
        /// <returns>
        /// True if the size was reset; otherwise, false.
        /// </returns>
        public override bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the size of the specified host dimension; this
        /// host has no size, so this method does nothing and fails.
        /// </summary>
        /// <param name="hostSizeType">
        /// The type of size to retrieve (for example, the buffer or the
        /// window).
        /// </param>
        /// <param name="width">
        /// Upon success, this would contain the width, in columns.
        /// </param>
        /// <param name="height">
        /// Upon success, this would contain the height, in rows.
        /// </param>
        /// <returns>
        /// True if the size was retrieved; otherwise, false.
        /// </returns>
        public override bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the size of the specified host dimension; this host
        /// has no size, so this method does nothing and fails.
        /// </summary>
        /// <param name="hostSizeType">
        /// The type of size to set (for example, the buffer or the window).
        /// </param>
        /// <param name="width">
        /// The desired width, in columns.
        /// </param>
        /// <param name="height">
        /// The desired height, in rows.
        /// </param>
        /// <returns>
        /// True if the size was set; otherwise, false.
        /// </returns>
        public override bool SetSize(
            HostSizeType hostSizeType,
            int width,
            int height
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        /// <summary>
        /// This method reads a single character from this host; this host has
        /// no input source, so it returns end-of-file and succeeds.
        /// </summary>
        /// <param name="value">
        /// Upon return, this is set to the character that was read, which is
        /// always end-of-file for this host.
        /// </param>
        /// <returns>
        /// True if the read succeeded; otherwise, false.
        /// </returns>
        public override bool Read(
            ref int value
            )
        {
            CheckDisposed();

            //
            // NOTE: We have no input source; indicate this to the caller.
            //
            value = ChannelStream.EndOfFile;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single key press from this host; this host has
        /// no input source, so it returns a null key and succeeds.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key so that it is not echoed.
        /// </param>
        /// <param name="value">
        /// Upon return, this is set to the data describing the key that was
        /// read, which is always null for this host.
        /// </param>
        /// <returns>
        /// True if the read succeeded; otherwise, false.
        /// </returns>
        public override bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();

            //
            // NOTE: We have no input source; indicate this to the caller.
            //
            value = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// This method reads a single key press from this host, returning it as
        /// a <see cref="ConsoleKeyInfo" />; this host has no input source, so it
        /// returns a default key and succeeds.  This overload is obsolete.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key so that it is not echoed.
        /// </param>
        /// <param name="value">
        /// Upon return, this is set to the key that was read, which is always
        /// the default value for this host.
        /// </param>
        /// <returns>
        /// True if the read succeeded; otherwise, false.
        /// </returns>
        [Obsolete()]
        public override bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            )
        {
            CheckDisposed();

            //
            // NOTE: We have no input source; indicate this to the caller.
            //
            value = default(ConsoleKeyInfo);
            return true;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        /// <summary>
        /// This method writes a single character to the diagnostic tracing
        /// output, optionally followed by an end-of-line.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write an end-of-line after the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public override bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            try
            {
                if (newLine)
                    DebugOps.TraceWriteLine(value); /* EXEMPT */
                else
                    DebugOps.TraceWrite(value); /* EXEMPT */

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the diagnostic tracing output,
        /// optionally followed by an end-of-line.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write an end-of-line after the string.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public override bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            try
            {
                if (newLine)
                    DebugOps.TraceWriteLine(value); /* EXEMPT */
                else
                    DebugOps.TraceWrite(value); /* EXEMPT */

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        /// <summary>
        /// This method returns a snapshot of this host's current state; this
        /// host exposes no state, so this method returns null.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that select how much state detail is included in the
        /// result.
        /// </param>
        /// <returns>
        /// A list describing the requested host state, or null when no state is
        /// available.
        /// </returns>
        public override StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits an audible tone through this host; this host does
        /// not support audible output, so this method does nothing and fails.
        /// </summary>
        /// <param name="frequency">
        /// The tone frequency, in hertz.
        /// </param>
        /// <param name="duration">
        /// The tone duration, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the tone was emitted; otherwise, false.
        /// </returns>
        public override bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this host currently has no pending
        /// interactive activity; this host has no better idle detection, so it
        /// always reports that it is idle.
        /// </summary>
        /// <returns>
        /// True if this host is idle; otherwise, false.
        /// </returns>
        public override bool IsIdle()
        {
            CheckDisposed();

            //
            // STUB: We have no better idle detection.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the display area of this host; this host has no
        /// display, so this method does nothing and fails.
        /// </summary>
        /// <returns>
        /// True if the display was cleared; otherwise, false.
        /// </returns>
        public override bool Clear()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the configuration flags of this host to their
        /// default values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the interactive input history of this host; this
        /// host does not implement history and always fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode ResetHistory(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current mode of one of this host's
        /// standard channels; this host does not implement channel modes and
        /// always fails.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be retrieved (for example, input or
        /// output).
        /// </param>
        /// <param name="mode">
        /// Upon success, this would be set to the current channel mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the mode of one of this host's standard channels;
        /// this host does not implement channel modes and always fails.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be set (for example, input or output).
        /// </param>
        /// <param name="mode">
        /// The new channel mode to apply.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens, or re-opens, this host's underlying interactive
        /// resources; this host does not implement opening and always fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes this host's underlying interactive resources;
        /// this host does not implement closing and always fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards any buffered input and/or output of this host
        /// without closing it; this host does not implement discarding and
        /// always fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host to its initial state, also resetting
        /// its host flags.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if (base.Reset(ref error) == ReturnCode.Ok)
            {
                if (!PrivateResetHostFlags()) /* NON-VIRTUAL */
                {
                    error = "failed to reset flags";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a named output section; this host does not
        /// delimit sections, so this method does nothing and succeeds.
        /// </summary>
        /// <param name="name">
        /// The name of the section to begin.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the section was begun; otherwise, false.
        /// </returns>
        public override bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a named output section previously begun with
        /// <see cref="BeginSection" />; this host does not delimit sections, so
        /// this method does nothing and succeeds.
        /// </summary>
        /// <param name="name">
        /// The name of the section to end.  This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the section was ended; otherwise, false.
        /// </returns>
        public override bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this host has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this host has already been
        /// disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this host has been disposed and the engine is configured
        /// to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    InternalSafeGetInterpreter(false), null))
            {
                throw new InterpreterDisposedException(typeof(Diagnostic));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this host.  It implements
        /// the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from <c>Dispose()</c>
        /// (i.e. deterministically); zero if it is being called from the
        /// finalizer.  When non-zero, managed resources are released.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        if (error != null)
                        {
                            error.Dispose();
                            error = null;
                        }

                        if (output != null)
                        {
                            output.Dispose();
                            output = null;
                        }

                        if (input != null)
                        {
                            input.Dispose();
                            input = null;
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
