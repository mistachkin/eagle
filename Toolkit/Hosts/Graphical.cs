/*
 * Graphical.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this code, it is a proof-of-concept only.  It is not production ready.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Hosts
{
    [ObjectId("5a3db3bb-ae55-4a61-8772-040254dbb90c")]
    public class Graphical : Core, IDisposable
    {
        #region Public Constructors
        public Graphical(
            IHostData hostData
            )
            : base(hostData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        private bool PrivateResetHostFlags()
        {
            hostFlags = HostFlags.Invalid;

            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
                hostFlags = base.MaybeInitializeHostFlags();

            return hostFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        public override bool RefreshTitle()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsInputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsOpen()
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Pause()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Flush()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int ReadLevels
        {
            get
            {
                CheckDisposed();

                /* NEVER READING FROM USER */
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override int WriteLevels
        {
            get
            {
                CheckDisposed();

                /* NEVER WRITING TO USER */
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteLine()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        private Stream input;
        public override Stream In
        {
            get { CheckDisposed(); return input; }
            set { CheckDisposed(); input = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Stream output;
        public override Stream Out
        {
            get { CheckDisposed(); return output; }
            set { CheckDisposed(); output = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Stream error;
        public override Stream Error
        {
            get { CheckDisposed(); return error; }
            set { CheckDisposed(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Encoding inputEncoding;
        public override Encoding InputEncoding
        {
            get { CheckDisposed(); return inputEncoding; }
            set { CheckDisposed(); inputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Encoding outputEncoding;
        public override Encoding OutputEncoding
        {
            get { CheckDisposed(); return outputEncoding; }
            set { CheckDisposed(); outputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Encoding errorEncoding;
        public override Encoding ErrorEncoding
        {
            get { CheckDisposed(); return errorEncoding; }
            set { CheckDisposed(); errorEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetIn()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetOut()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetError()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsOutputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsErrorRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetupChannels()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public override IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Graphical(new HostData(
                Name, Group, Description, ClientData, typeof(Graphical).Name,
                interpreter, ResourceManager, Profile, Utility.GetHostCreateFlags(
                HostCreateFlags, UseAttach, NoColor, NoTitle, NoIcon, NoProfile,
                NoCancel)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            return HostTestFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteDebugLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteErrorLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        public override bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        public override bool ResetColors()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        public override bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        public override bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        public override bool Read(
            ref int value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        [Obsolete()]
        public override bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            )
        {
            CheckDisposed();

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        public override bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsIdle()
        {
            CheckDisposed();

            //
            // STUB: We have no idle detection.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Clear()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    SafeGetInterpreter(), null))
            {
                throw new InterpreterDisposedException(typeof(Graphical));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

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
