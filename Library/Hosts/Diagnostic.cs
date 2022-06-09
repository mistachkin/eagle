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
    [ObjectId("9372a55b-ebc4-4745-a4e0-ce73fdc1fe39")]
    public class Diagnostic : Core, IDisposable
    {
        #region Diagnostic Stream Class
        [ObjectId("6e9e17a2-da98-4ab5-87a9-0c4a0483494b")]
        private sealed class DiagnosticStream : Stream
        {
            #region Public Constructors
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
            private bool canRead;
            public override bool CanRead
            {
                get { CheckDisposed(); return canRead; }
            }

            ///////////////////////////////////////////////////////////////////

            public override bool CanSeek
            {
                get { CheckDisposed(); return false; }
            }

            ///////////////////////////////////////////////////////////////////

            private bool canWrite;
            public override bool CanWrite
            {
                get { CheckDisposed(); return canWrite; }
            }

            ///////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                DebugOps.TraceFlush();
            }

            ///////////////////////////////////////////////////////////////////

            public override long Length
            {
                get { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////

            public override long Position
            {
                get { CheckDisposed(); throw new NotSupportedException(); }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////

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

            public override long Seek(
                long offset,
                SeekOrigin origin
                )
            {
                CheckDisposed();

                throw new NotSupportedException();
            }

            ///////////////////////////////////////////////////////////////////

            public override void SetLength(
                long value
                )
            {
                CheckDisposed();

                throw new NotSupportedException();
            }

            ///////////////////////////////////////////////////////////////////

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

                for (int index = offset; count > 0; index++, count--)
                {
                    DebugOps.TraceWrite(
                        ConversionOps.ToChar(buffer[index])); /* EXEMPT */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
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
            ~DiagnosticStream()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override bool RefreshTitle()
        {
            CheckDisposed();

            //
            // NOTE: We have no title; therefore, just succeed.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public override bool IsOpen()
        {
            CheckDisposed();

            /* ALWAYS OPEN */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool Pause()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool Flush()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

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
        private Stream input;
        public override Stream In
        {
            get { CheckDisposed(); return input; }
            set { CheckDisposed(); input = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream output;
        public override Stream Out
        {
            get { CheckDisposed(); return output; }
            set { CheckDisposed(); output = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Stream error;
        public override Stream Error
        {
            get { CheckDisposed(); return error; }
            set { CheckDisposed(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override Encoding InputEncoding
        {
            get { CheckDisposed(); return null; }
            set { CheckDisposed(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public override Encoding OutputEncoding
        {
            get { CheckDisposed(); return null; }
            set { CheckDisposed(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public override Encoding ErrorEncoding
        {
            get { CheckDisposed(); return null; }
            set { CheckDisposed(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool ResetIn()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool ResetOut()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool ResetError()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool IsOutputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool IsErrorRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool SetupChannels()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
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

        private HostTestFlags hostTestFlags = HostTestFlags.Invalid;
        public override HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            if (hostTestFlags == HostTestFlags.Invalid)
                hostTestFlags = HostTestFlags.None;

            return hostTestFlags;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override bool ResetColors()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public override bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            /* IGNORED */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

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

        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWriteHost Members

        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool IsIdle()
        {
            CheckDisposed();

            //
            // STUB: We have no better idle detection.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool Clear()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

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

        public override ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////

        public override bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

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

        #region IDisposable "Pattern" Members
        private bool disposed;
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
