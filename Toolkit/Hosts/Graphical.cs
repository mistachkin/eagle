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
    /// <summary>
    /// This class implements a proof-of-concept graphical host based on the
    /// shared host <see cref="Core" /> base class.  It is not production
    /// ready; nearly every input, output, and control operation is a stub
    /// that does nothing (output methods return false and control methods
    /// report a "not implemented" error), and the active streams and
    /// encodings are simply stored and returned.  It is intended as a
    /// starting point for building a real windowed host rather than for
    /// actual use.
    /// </summary>
    [ObjectId("5a3db3bb-ae55-4a61-8772-040254dbb90c")]
    public class Graphical : Core, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this host using the specified host data.
        /// This constructor forwards the supplied host data to the
        /// <see cref="Core" /> base class.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize this host (for example, its name,
        /// group, description, client data, profile, and creation flags).  This
        /// parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method resets the cached host flags so that they will be
        /// recalculated on demand and then resets the base host flags.  It is
        /// the non-virtual implementation used by this host.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            hostFlags = HostFlags.Invalid;

            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the cached host flags from the base class
        /// the first time they are requested (i.e. while they are still
        /// invalid) and returns them.
        /// </summary>
        /// <returns>
        /// The flags that describe the capabilities and configuration of this
        /// host.
        /// </returns>
        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
                hostFlags = base.MaybeInitializeHostFlags();

            return hostFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        /// <summary>
        /// This method updates the host's window or console title to reflect its
        /// current value.  This host does not support titles and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host does not refresh a title.
        /// </returns>
        public override bool RefreshTitle()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host's interactive input has been
        /// redirected (for example, from a file or pipe).
        /// </summary>
        /// <returns>
        /// Always false, because this host never reads interactive input.
        /// </returns>
        public override bool IsInputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host's interactive resources are
        /// currently open.
        /// </summary>
        /// <returns>
        /// Always true for this host.
        /// </returns>
        public override bool IsOpen()
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pauses interactive processing, typically waiting for the
        /// user to acknowledge before continuing.  This host does not support
        /// pausing and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host does not pause.
        /// </returns>
        public override bool Pause()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered host output.  This host buffers no
        /// output and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host has nothing to flush.
        /// </returns>
        public override bool Flush()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cached flags that describe the capabilities and
        /// configuration of this host.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;
        /// <summary>
        /// This method returns the flags that describe the capabilities and
        /// configuration of this host, initializing them on first use.
        /// </summary>
        /// <returns>
        /// The flags that describe the capabilities and configuration of this
        /// host.
        /// </returns>
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of read operations in progress on this
        /// host.  This host never reads from the user, so this is always zero.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of write operations in progress on
        /// this host.  This host never writes to the user, so this is always
        /// zero.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single line of interactive input from the host.
        /// This host never reads input and always fails.
        /// </summary>
        /// <param name="value">
        /// Upon success, this would receive the line that was read; this host
        /// never modifies it.
        /// </param>
        /// <returns>
        /// Always false, because this host never reads interactive input.
        /// </returns>
        public override bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes an end-of-line to the host output.  This host
        /// produces no output and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host produces no output.
        /// </returns>
        public override bool WriteLine()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        /// <summary>
        /// Stores the active input stream for this host.
        /// </summary>
        private Stream input;
        /// <summary>
        /// Gets or sets the active input stream for this host.
        /// </summary>
        public override Stream In
        {
            get { CheckDisposed(); return input; }
            set { CheckDisposed(); input = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the active output stream for this host.
        /// </summary>
        private Stream output;
        /// <summary>
        /// Gets or sets the active output stream for this host.
        /// </summary>
        public override Stream Out
        {
            get { CheckDisposed(); return output; }
            set { CheckDisposed(); output = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the active error stream for this host.
        /// </summary>
        private Stream error;
        /// <summary>
        /// Gets or sets the active error stream for this host.
        /// </summary>
        public override Stream Error
        {
            get { CheckDisposed(); return error; }
            set { CheckDisposed(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the encoding used for the input stream.
        /// </summary>
        private Encoding inputEncoding;
        /// <summary>
        /// Gets or sets the encoding used for the input stream.
        /// </summary>
        public override Encoding InputEncoding
        {
            get { CheckDisposed(); return inputEncoding; }
            set { CheckDisposed(); inputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the encoding used for the output stream.
        /// </summary>
        private Encoding outputEncoding;
        /// <summary>
        /// Gets or sets the encoding used for the output stream.
        /// </summary>
        public override Encoding OutputEncoding
        {
            get { CheckDisposed(); return outputEncoding; }
            set { CheckDisposed(); outputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the encoding used for the error stream.
        /// </summary>
        private Encoding errorEncoding;
        /// <summary>
        /// Gets or sets the encoding used for the error stream.
        /// </summary>
        public override Encoding ErrorEncoding
        {
            get { CheckDisposed(); return errorEncoding; }
            set { CheckDisposed(); errorEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active input stream to its default.  This host
        /// does not support resetting and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host does not reset its input stream.
        /// </returns>
        public override bool ResetIn()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active output stream to its default.  This
        /// host does not support resetting and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host does not reset its output stream.
        /// </returns>
        public override bool ResetOut()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active error stream to its default.  This host
        /// does not support resetting and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host does not reset its error stream.
        /// </returns>
        public override bool ResetError()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the output stream for this host has
        /// been redirected.
        /// </summary>
        /// <returns>
        /// Always false, because this host never redirects its output.
        /// </returns>
        public override bool IsOutputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the error stream for this host has
        /// been redirected.
        /// </summary>
        /// <returns>
        /// Always false, because this host never redirects its error output.
        /// </returns>
        public override bool IsErrorRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets up the input, output, and error channels for this
        /// host.  This host does not set up channels and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host does not set up channels.
        /// </returns>
        public override bool SetupChannels()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// This method creates a copy of this host for use with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that the cloned host will be associated with.
        /// </param>
        /// <returns>
        /// A new host that is a copy of this host.
        /// </returns>
        public override IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Graphical(new HostData(
                Name, Group, Description, ClientData, typeof(Graphical).Name,
                interpreter, ResourceManager, Profile, Utility.GetHostCreateFlags(
                HostCreateFlags, UseAttach, UseForce, NoColor, NoTitle, NoIcon,
                NoProfile, NoCancel)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the flags that describe the testing capabilities of
        /// this host.  This host advertises no test capabilities.
        /// </summary>
        /// <returns>
        /// Always <see cref="HostTestFlags.Invalid" />, because this host has no
        /// test capabilities.
        /// </returns>
        public override HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            return HostTestFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the current script evaluation be canceled.
        /// This host does not support cancellation and always fails.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly cancel evaluation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host does not support
        /// cancellation.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the interpreter exit.  This host does not
        /// support exiting and always fails.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly exit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host does not support
        /// exiting.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line terminator to the debug output of the host.
        /// This host produces no output and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host produces no debug output.
        /// </returns>
        public override bool WriteDebugLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the debug output of the
        /// host, optionally followed by a line terminator.  This host produces
        /// no output and always fails.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to also write a line terminator after the character.
        /// </param>
        /// <returns>
        /// Always false, because this host produces no debug output.
        /// </returns>
        public override bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of the
        /// host, optionally followed by a line terminator.  This host produces
        /// no output and always fails.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to also write a line terminator after the string.
        /// </param>
        /// <returns>
        /// Always false, because this host produces no debug output.
        /// </returns>
        public override bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line terminator to the error output of the host.
        /// This host produces no output and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host produces no error output.
        /// </returns>
        public override bool WriteErrorLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the error output of the
        /// host, optionally followed by a line terminator.  This host produces
        /// no output and always fails.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to also write a line terminator after the character.
        /// </param>
        /// <returns>
        /// Always false, because this host produces no error output.
        /// </returns>
        public override bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of the
        /// host, optionally followed by a line terminator.  This host produces
        /// no output and always fails.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to also write a line terminator after the string.
        /// </param>
        /// <returns>
        /// Always false, because this host produces no error output.
        /// </returns>
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
        /// <summary>
        /// This method writes custom, host-specific information to the host
        /// output, using the specified colors.  This host produces no output and
        /// always fails.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose information is to be written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that select how much detail is included.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to also write a line terminator after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// Always false, because this host produces no output.
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

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        /// <summary>
        /// This method begins rendering a box with the specified name and
        /// content.  This host does not render boxes and always fails.
        /// </summary>
        /// <param name="name">
        /// The name of the box to begin.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the box content.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the box, if any.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// Always false, because this host does not render boxes.
        /// </returns>
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

        /// <summary>
        /// This method ends rendering a box with the specified name and content.
        /// This host does not render boxes and always fails.
        /// </summary>
        /// <param name="name">
        /// The name of the box to end.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the box content.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the box, if any.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// Always false, because this host does not render boxes.
        /// </returns>
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
        /// <summary>
        /// This method resets the host foreground and background colors to their
        /// default values.  This host does not support colors and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host does not support colors.
        /// </returns>
        public override bool ResetColors()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the current foreground and background colors of the
        /// host.  This host does not support colors and always fails.
        /// </summary>
        /// <param name="foregroundColor">
        /// Upon success, this would receive the current foreground color; this
        /// host never modifies it.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, this would receive the current background color; this
        /// host never modifies it.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support colors.
        /// </returns>
        public override bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the specified foreground and background colors as
        /// necessary so that they are suitable for use by the host.  This host
        /// does not support colors and always fails.
        /// </summary>
        /// <param name="foregroundColor">
        /// On input, the foreground color to adjust; this host never modifies
        /// it.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the background color to adjust; this host never modifies
        /// it.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support colors.
        /// </returns>
        public override bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host foreground color.  This host does not
        /// support colors and always fails.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to set.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support colors.
        /// </returns>
        public override bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host background color.  This host does not
        /// support colors and always fails.
        /// </summary>
        /// <param name="backgroundColor">
        /// The background color to set.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support colors.
        /// </returns>
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
        /// <summary>
        /// This method gets the current cursor position.  This host does not
        /// support cursor positioning and always fails.
        /// </summary>
        /// <param name="left">
        /// Upon success, this would receive the current column; this host never
        /// modifies it.
        /// </param>
        /// <param name="top">
        /// Upon success, this would receive the current row; this host never
        /// modifies it.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support cursor positioning.
        /// </returns>
        public override bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the current cursor position.  This host does not
        /// support cursor positioning and always fails.
        /// </summary>
        /// <param name="left">
        /// The column to move the cursor to.
        /// </param>
        /// <param name="top">
        /// The row to move the cursor to.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support cursor positioning.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        /// <summary>
        /// This method resets the size of the specified host buffer and/or
        /// window to its default.  This host does not support sizing and always
        /// fails.
        /// </summary>
        /// <param name="hostSizeType">
        /// The buffer and/or window whose size is to be reset.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support sizing.
        /// </returns>
        public override bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the size of the specified host buffer and/or
        /// window.  This host does not support sizing and always fails.
        /// </summary>
        /// <param name="hostSizeType">
        /// The buffer and/or window whose size is to be queried.
        /// </param>
        /// <param name="width">
        /// Upon success, this would receive the width; this host never modifies
        /// it.
        /// </param>
        /// <param name="height">
        /// Upon success, this would receive the height; this host never modifies
        /// it.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support sizing.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method changes the size of the specified host buffer and/or
        /// window.  This host does not support sizing and always fails.
        /// </summary>
        /// <param name="hostSizeType">
        /// The buffer and/or window whose size is to be changed.
        /// </param>
        /// <param name="width">
        /// The new width to apply.
        /// </param>
        /// <param name="height">
        /// The new height to apply.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support sizing.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        /// <summary>
        /// This method reads a single character from the host.  This host never
        /// reads input and always fails.
        /// </summary>
        /// <param name="value">
        /// Upon success, this would receive the character that was read; this
        /// host never modifies it.
        /// </param>
        /// <returns>
        /// Always false, because this host never reads input.
        /// </returns>
        public override bool Read(
            ref int value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single key press from the host.  This host never
        /// reads input and always fails.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed.
        /// </param>
        /// <param name="value">
        /// Upon success, this would receive data describing the key that was
        /// pressed; this host never modifies it.
        /// </param>
        /// <returns>
        /// Always false, because this host never reads input.
        /// </returns>
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
        /// <summary>
        /// This method reads a single key press from the host.  This host never
        /// reads input and always fails.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed.
        /// </param>
        /// <param name="value">
        /// Upon success, this would receive information about the key that was
        /// pressed; this host never modifies it.
        /// </param>
        /// <returns>
        /// Always false, because this host never reads input.
        /// </returns>
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
        /// <summary>
        /// This method writes a single character to the host output, optionally
        /// followed by a newline.  This host produces no output and always
        /// fails.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to also write a newline after the character.
        /// </param>
        /// <returns>
        /// Always false, because this host produces no output.
        /// </returns>
        public override bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally followed
        /// by a newline.  This host produces no output and always fails.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to also write a newline after the string.
        /// </param>
        /// <returns>
        /// Always false, because this host produces no output.
        /// </returns>
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
        /// <summary>
        /// This method returns a snapshot of this host's current state, with the
        /// amount of detail controlled by the supplied flags.  This host
        /// provides no state information.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that select how much state detail is included in the
        /// result.
        /// </param>
        /// <returns>
        /// Always null, because this host provides no state information.
        /// </returns>
        public override StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits an audible tone through the host.  This host does
        /// not support audible output and always fails.
        /// </summary>
        /// <param name="frequency">
        /// The tone frequency, in hertz.
        /// </param>
        /// <param name="duration">
        /// The tone duration, in milliseconds.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support audible output.
        /// </returns>
        public override bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host currently has no pending
        /// interactive input or output activity.  This host has no idle
        /// detection.
        /// </summary>
        /// <returns>
        /// Always false, because this host has no idle detection.
        /// </returns>
        public override bool IsIdle()
        {
            CheckDisposed();

            //
            // STUB: We have no idle detection.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's display area.  This host has no display
        /// area and always fails.
        /// </summary>
        /// <returns>
        /// Always false, because this host has no display to clear.
        /// </returns>
        public override bool Clear()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host's configuration flags to their default
        /// values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's interactive input history.  This host
        /// keeps no history and always fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host keeps no history.
        /// </returns>
        public override ReturnCode ResetHistory(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current mode of one of the host's standard
        /// channels.  This host does not support channel modes and always fails.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be retrieved (for example, input or
        /// output).
        /// </param>
        /// <param name="mode">
        /// Upon success, this would be set to the current channel mode; this
        /// host never modifies it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host does not support channel
        /// modes.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the mode of one of the host's standard channels.
        /// This host does not support channel modes and always fails.
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
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host does not support channel
        /// modes.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens, or re-opens, the host's underlying interactive
        /// resources.  This host does not support opening and always fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host does not support
        /// opening.
        /// </returns>
        public override ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the host's underlying interactive resources.  This
        /// host does not support closing and always fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host does not support
        /// closing.
        /// </returns>
        public override ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards any buffered host input and/or output without
        /// closing the host.  This host does not support discarding and always
        /// fails.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />, because this host does not support
        /// discarding.
        /// </returns>
        public override ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the host to its initial state, reinitializing its
        /// interactive resources.  It chains to the base implementation and then
        /// resets this host's flags.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a named output section, allowing the host to group
        /// or visually delimit related output.  This host does not support
        /// sections and always fails.
        /// </summary>
        /// <param name="name">
        /// The name of the section to begin.  This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support sections.
        /// </returns>
        public override bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a named output section previously begun with
        /// <see cref="BeginSection" />.  This host does not support sections and
        /// always fails.
        /// </summary>
        /// <param name="name">
        /// The name of the section to end.  This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// Always false, because this host does not support sections.
        /// </returns>
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
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this host has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this host has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this host has been disposed and the engine is configured
        /// to throw on use of a disposed object.
        /// </exception>
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

        /// <summary>
        /// This method releases the resources held by this host.  It implements
        /// the standard dispose pattern and chains to the base class.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called deterministically (for
        /// example, from an explicit call to dispose); zero if it is being
        /// called from the finalizer.  When non-zero, managed resources are
        /// released.
        /// </param>
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
