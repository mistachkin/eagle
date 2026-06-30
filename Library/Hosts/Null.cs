/*
 * Null.cs --
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
using System.Threading;
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
    /// This class implements a host that performs no real input or output,
    /// providing only do-nothing implementations of the <see cref="IHost" />
    /// contract.  It is useful as a placeholder, or starting point, when an
    /// interpreter must be created without any functional interactive host:
    /// every output operation silently fails (returning false), every reading
    /// or processing operation reports an error or supplies no data, and the
    /// configuration properties simply store and return their values.  Compare
    /// to the fully functional console host implementations.
    /// </summary>
    [ObjectId("acbef62d-d8c5-429c-87aa-9500d91e7a7c")]
    public sealed class Null :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IHost, IDisposable, IMaybeDisposed
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this host.  This private constructor performs
        /// the initialization common to the public constructors, setting the
        /// identifier kind and the unique identifier of this host.
        /// </summary>
        private Null()
        {
            kind = IdentifierKind.Host;
            id = AttributeOps.GetObjectId(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this host using the specified host data.  This
        /// constructor delegates to the private constructor and then copies the
        /// relevant settings from the supplied host data, when it is present.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize this host (for example, its name,
        /// group, description, client data, profile, and creation flags).  This
        /// parameter may be null.
        /// </param>
        public Null(
            IHostData hostData
            )
            : this()
        {
            if (hostData != null)
            {
                name = hostData.Name;
                group = hostData.Group;
                description = hostData.Description;
                clientData = hostData.ClientData;
                profile = hostData.Profile;
                hostCreateFlags = hostData.HostCreateFlags;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this host.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this host.
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
        /// Stores the identifier kind of this host.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this host.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this host.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this host.
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
        /// Stores the client data associated with this host.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this host.
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
        /// Stores the group of this host.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this host.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this host.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this host.
        /// </summary>
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        /// <summary>
        /// This method is called when interactive processing is about to begin at
        /// the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the start of processing; on
        /// output, the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode BeginProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called when interactive processing is about to end at the
        /// specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the end of processing; on
        /// output, the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode EndProcessing(
            int levels,
            ref string text,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called when interactive processing has completed at the
        /// specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode DoneProcessing(
            int levels,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the window or console title of this host.
        /// </summary>
        private string title;
        /// <summary>
        /// Gets or sets the current window or console title used by this host.
        /// </summary>
        public string Title
        {
            get { CheckDisposed(); return title; }
            set { CheckDisposed(); title = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the host's window or console title to reflect its
        /// current value.
        /// </summary>
        /// <returns>
        /// True if the title was refreshed; otherwise, false.
        /// </returns>
        public bool RefreshTitle()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host's interactive input has been
        /// redirected (for example, from a file or pipe).
        /// </summary>
        /// <returns>
        /// True if the input is redirected; otherwise, false.
        /// </returns>
        public bool IsInputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method displays a prompt of the specified type and reports the
        /// flags that resulted from displaying it.
        /// </summary>
        /// <param name="type">
        /// The type of prompt to display (for example, a normal or
        /// continuation prompt).
        /// </param>
        /// <param name="flags">
        /// On input, the flags that control how the prompt is displayed; on
        /// output, the flags that resulted from displaying it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host's interactive resources are
        /// currently open.
        /// </summary>
        /// <returns>
        /// True if the host is open; otherwise, false.
        /// </returns>
        public bool IsOpen()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pauses interactive processing, typically waiting for the
        /// user to acknowledge before continuing.
        /// </summary>
        /// <returns>
        /// True if the host was paused; otherwise, false.
        /// </returns>
        public bool Pause()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered host output.
        /// </summary>
        /// <returns>
        /// True if the output was flushed; otherwise, false.
        /// </returns>
        public bool Flush()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the flags that control which header sections the
        /// host displays.
        /// </summary>
        /// <returns>
        /// The current header flags for this host.
        /// </returns>
        public HeaderFlags GetHeaderFlags()
        {
            CheckDisposed();

            return HeaderFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the flags that control how much detail the host
        /// includes in its output.
        /// </summary>
        /// <returns>
        /// The current detail flags for this host.
        /// </returns>
        public DetailFlags GetDetailFlags()
        {
            CheckDisposed();

            return DetailFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the flags that describe the capabilities and
        /// configuration of this host.
        /// </summary>
        /// <returns>
        /// The current host flags for this host.
        /// </returns>
        public HostFlags GetHostFlags()
        {
            CheckDisposed();

            return HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of read operations in progress on this
        /// host.
        /// </summary>
        public int ReadLevels
        {
            get
            {
                CheckDisposed();

                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current nesting level of write operations in progress on this
        /// host.
        /// </summary>
        public int WriteLevels
        {
            get
            {
                CheckDisposed();

                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single line of interactive input from the host.
        /// </summary>
        /// <param name="value">
        /// Upon success, this is set to the line of input that was read.
        /// </param>
        /// <returns>
        /// True if a line was read; otherwise, false.
        /// </returns>
        public bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public bool Write(
            char value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool Write(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes an end-of-line to the host output.
        /// </summary>
        /// <returns>
        /// True if the end-of-line was written; otherwise, false.
        /// </returns>
        public bool WriteLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string followed by an end-of-line to the host
        /// output.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        public bool WriteLine(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted representation of a result, followed by
        /// an end-of-line, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        public bool WriteResultLine(
            ReturnCode code,
            Result result
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted representation of a result, including an
        /// error line number, followed by an end-of-line, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        public bool WriteResultLine(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        /// <summary>
        /// Stores the flags that control how this host opens and manages streams.
        /// </summary>
        private HostStreamFlags streamFlags = HostStreamFlags.Invalid;
        /// <summary>
        /// Gets or sets the flags that control how this host opens and manages
        /// streams.
        /// </summary>
        public HostStreamFlags StreamFlags
        {
            get { CheckDisposed(); return streamFlags; }
            set { CheckDisposed(); streamFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens a stream for the specified path on behalf of the
        /// engine.
        /// </summary>
        /// <param name="path">
        /// The path of the file or resource to open.
        /// </param>
        /// <param name="mode">
        /// The mode used when opening the stream (for example, create or
        /// open).
        /// </param>
        /// <param name="access">
        /// The access requested for the stream (for example, read or write).
        /// </param>
        /// <param name="share">
        /// The sharing mode permitted for the stream.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer to use for the stream.
        /// </param>
        /// <param name="options">
        /// The additional options used when opening the stream.
        /// </param>
        /// <param name="hostStreamFlags">
        /// On input, the flags that influence how the stream is opened; on
        /// output, the flags describing the stream that was opened.
        /// </param>
        /// <param name="fullPath">
        /// Upon return, this contains the fully qualified path of the stream
        /// that was opened.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        public ReturnCode GetStream(
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize,
            FileOptions options,
            ref HostStreamFlags hostStreamFlags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fetches the named data (for example, a script) on behalf of
        /// the engine.
        /// </summary>
        /// <param name="name">
        /// The name of the data to fetch.
        /// </param>
        /// <param name="dataFlags">
        /// The flags that control how the data is located and fetched.
        /// </param>
        /// <param name="scriptFlags">
        /// On input, the flags that influence how the data is fetched; on
        /// output, the flags describing the data that was fetched.
        /// </param>
        /// <param name="clientData">
        /// On input, the extra data supplied for the request, if any; on
        /// output, the extra data associated with the fetched data, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the fetched data.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        public ReturnCode GetData(
            string name,
            DataFlags dataFlags,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            )
        {
            CheckDisposed();

            result = "not implemented";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcessHost Members
        /// <summary>
        /// Stores a value indicating whether the hosting process is permitted to
        /// exit.
        /// </summary>
        private bool canExit;
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is permitted
        /// to exit.
        /// </summary>
        public bool CanExit
        {
            get { CheckDisposed(); return canExit; }
            set { CheckDisposed(); canExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the hosting process is permitted to be
        /// forcibly exited.
        /// </summary>
        private bool canForceExit;
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is permitted
        /// to be forcibly exited.
        /// </summary>
        public bool CanForceExit
        {
            get { CheckDisposed(); return canForceExit; }
            set { CheckDisposed(); canForceExit = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the hosting process is currently in
        /// the process of exiting.
        /// </summary>
        private bool exiting;
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is currently
        /// in the process of exiting.
        /// </summary>
        public bool Exiting
        {
            get { CheckDisposed(); return exiting; }
            set { CheckDisposed(); exiting = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        /// <summary>
        /// This method creates a new thread that uses a parameterless start
        /// delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should be
        /// configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack from
        /// the creating thread.
        /// </param>
        /// <param name="thread">
        /// Upon success, this will contain the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode CreateThread(
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new thread that uses a parameterized start
        /// delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new thread and
        /// accepts a single object argument.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should be
        /// configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack from
        /// the creating thread.
        /// </param>
        /// <param name="thread">
        /// Upon success, this will contain the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode CreateThread(
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues a parameterless callback for execution on a thread
        /// pool thread.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke on a thread pool thread.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode QueueWorkItem(
            ThreadStart callback,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues a callback that accepts a state object for execution
        /// on a thread pool thread.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke on a thread pool thread.
        /// </param>
        /// <param name="state">
        /// The state object to pass to the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method suspends the current thread for the specified amount of
        /// time.
        /// </summary>
        /// <param name="milliseconds">
        /// The amount of time to suspend the current thread, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread was successfully suspended; otherwise, false.
        /// </returns>
        public bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to yield execution to another
        /// thread that is ready to run on the current processor.
        /// </summary>
        /// <returns>
        /// True if the operating system switched execution to another thread;
        /// otherwise, false.
        /// </returns>
        public bool Yield()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        /// <summary>
        /// Gets the default input stream for this host.
        /// </summary>
        public Stream DefaultIn
        {
            get { CheckDisposed(); return input; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the default output stream for this host.
        /// </summary>
        public Stream DefaultOut
        {
            get { CheckDisposed(); return output; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the default error stream for this host.
        /// </summary>
        public Stream DefaultError
        {
            get { CheckDisposed(); return error; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the active input stream for this host.
        /// </summary>
        private Stream input;
        /// <summary>
        /// Gets or sets the active input stream for this host.
        /// </summary>
        public Stream In
        {
            get { CheckDisposed(); return input; }
            set { CheckDisposed(); input = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the active output stream for this host.
        /// </summary>
        private Stream output;
        /// <summary>
        /// Gets or sets the active output stream for this host.
        /// </summary>
        public Stream Out
        {
            get { CheckDisposed(); return output; }
            set { CheckDisposed(); output = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the active error stream for this host.
        /// </summary>
        private Stream error;
        /// <summary>
        /// Gets or sets the active error stream for this host.
        /// </summary>
        public Stream Error
        {
            get { CheckDisposed(); return error; }
            set { CheckDisposed(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the encoding used for the input stream.
        /// </summary>
        private Encoding inputEncoding;
        /// <summary>
        /// Gets or sets the encoding used for the input stream.
        /// </summary>
        public Encoding InputEncoding
        {
            get { CheckDisposed(); return inputEncoding; }
            set { CheckDisposed(); inputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the encoding used for the output stream.
        /// </summary>
        private Encoding outputEncoding;
        /// <summary>
        /// Gets or sets the encoding used for the output stream.
        /// </summary>
        public Encoding OutputEncoding
        {
            get { CheckDisposed(); return outputEncoding; }
            set { CheckDisposed(); outputEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the encoding used for the error stream.
        /// </summary>
        private Encoding errorEncoding;
        /// <summary>
        /// Gets or sets the encoding used for the error stream.
        /// </summary>
        public Encoding ErrorEncoding
        {
            get { CheckDisposed(); return errorEncoding; }
            set { CheckDisposed(); errorEncoding = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active input stream to its default.
        /// </summary>
        /// <returns>
        /// True if the input stream was reset; otherwise, false.
        /// </returns>
        public bool ResetIn()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active output stream to its default.
        /// </summary>
        /// <returns>
        /// True if the output stream was reset; otherwise, false.
        /// </returns>
        public bool ResetOut()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active error stream to its default.
        /// </summary>
        /// <returns>
        /// True if the error stream was reset; otherwise, false.
        /// </returns>
        public bool ResetError()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the output stream for this host has been
        /// redirected.
        /// </summary>
        /// <returns>
        /// True if the output stream has been redirected; otherwise, false.
        /// </returns>
        public bool IsOutputRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the error stream for this host has been
        /// redirected.
        /// </summary>
        /// <returns>
        /// True if the error stream has been redirected; otherwise, false.
        /// </returns>
        public bool IsErrorRedirected()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets up the input, output, and error channels for this
        /// host.
        /// </summary>
        /// <returns>
        /// True if the channels were set up successfully; otherwise, false.
        /// </returns>
        public bool SetupChannels()
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// This method creates a copy of this host.
        /// </summary>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        public IHost Clone()
        {
            CheckDisposed();

            return Clone(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this host for use with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the cloned host will be associated with.
        /// </param>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        public IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Null(new HostData(
                Name, Group, Description, ClientData, typeof(Null).Name,
                interpreter, null, Profile, HostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the flags that describe the testing capabilities of
        /// this host.
        /// </summary>
        /// <returns>
        /// The host test flags for this host.
        /// </returns>
        public HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            return HostTestFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests that the current script evaluation be canceled.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly cancel evaluation even if cancellation has
        /// been disabled or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode Cancel(
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
        /// This method requests that the interpreter exit.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly request the exit even if it has been disabled
        /// or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode Exit(
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
        /// This method writes a line terminator to the debug output of the host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebugLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string, followed by a line
        /// terminator, to the debug output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebugLine(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the debug output of the host.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebug(
            char value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the debug output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the character.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character a number of times to the
        /// debug output of the host, using the specified colors and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebug(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebug(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of the host, using
        /// the specified foreground color and optionally followed by a line
        /// terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the debug output of the host, using
        /// the specified colors and optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteDebug(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line terminator to the error output of the host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteErrorLine()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string, followed by a line
        /// terminator, to the error output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteErrorLine(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the error output of the host.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteError(
            char value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character to the error output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the character.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified character a number of times to the
        /// error output of the host, using the specified colors and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteError(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteError(
            string value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of the host, using
        /// the specified foreground color and optionally followed by a line
        /// terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string to the error output of the host, using
        /// the specified colors and optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteError(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified return code and result to the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteResult(
            ReturnCode code,
            Result result,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified return code and result to the host,
        /// optionally without additional formatting and optionally followed by a
        /// line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteResult(
            ReturnCode code,
            Result result,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified return code, result, and error line to
        /// the host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified return code, result, and error line to
        /// the host, optionally without additional formatting and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteResult(
            ReturnCode code,
            Result result,
            int errorLine,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified prefix, return code, result, and error
        /// line to the host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="prefix">
        /// The string to write before the result.
        /// </param>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified prefix, return code, result, and error
        /// line to the host, optionally without additional formatting and
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="prefix">
        /// The string to write before the result.
        /// </param>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        public bool WriteResult(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool raw,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        /// <summary>
        /// This method saves the host's current cursor position so that it can
        /// later be restored.
        /// </summary>
        /// <returns>
        /// True if the position was saved; otherwise, false.
        /// </returns>
        public bool SavePosition()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the host's cursor position previously saved with
        /// <see cref="SavePosition" />.
        /// </summary>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after restoring the
        /// position.
        /// </param>
        /// <returns>
        /// True if the position was restored; otherwise, false.
        /// </returns>
        public bool RestorePosition(
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes announcement information associated with a
        /// breakpoint to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the announcement is associated with.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteAnnouncementInfo(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes announcement information associated with a
        /// breakpoint to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the announcement is associated with.
        /// </param>
        /// <param name="value">
        /// The announcement text to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteAnnouncementInfo(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the arguments associated with a
        /// breakpoint to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the breakpoint.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the information is associated with.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint the information is associated with.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to write information about.
        /// </param>
        /// <param name="result">
        /// The result value associated with the breakpoint.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteArgumentInfo(
            Interpreter interpreter,
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            ArgumentList arguments,
            Result result,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the arguments associated with a
        /// breakpoint to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the breakpoint.
        /// </param>
        /// <param name="breakpointType">
        /// The type of breakpoint the information is associated with.
        /// </param>
        /// <param name="breakpointName">
        /// The name of the breakpoint the information is associated with.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to write information about.
        /// </param>
        /// <param name="result">
        /// The result value associated with the breakpoint.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteArgumentInfo(
            Interpreter interpreter,
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            ArgumentList arguments,
            Result result,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a representation of a single call frame to the host
        /// output, using the specified affixes and separator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
        /// </param>
        /// <param name="type">
        /// A string describing the kind of call frame being written.
        /// </param>
        /// <param name="prefix">
        /// The text to write before the call frame information.
        /// </param>
        /// <param name="suffix">
        /// The text to write after the call frame information.
        /// </param>
        /// <param name="separator">
        /// The character used to separate parts of the call frame information.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallFrame(
            Interpreter interpreter,
            ICallFrame frame,
            string type,
            string prefix,
            string suffix,
            char separator,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about a single call frame to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallFrameInfo(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about a single call frame to the host
        /// output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call frame is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallFrameInfo(
            Interpreter interpreter,
            ICallFrame frame,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a representation of the specified call stack to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a representation of the specified call stack to the
        /// host output, limited to a maximum number of frames.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallStack(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified call stack to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified call stack to the
        /// host output, limited to a maximum number of frames.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified call stack to the
        /// host output, limited to a maximum number of frames and using the
        /// specified colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the call stack is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to write information about.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCallStackInfo(
            Interpreter interpreter,
            CallStack callStack,
            int limit,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's script
        /// debugger to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteDebuggerInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's script
        /// debugger to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteDebuggerInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's various flag
        /// sets to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to write information about.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to write information about.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to write information about.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to write information about.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteFlagInfo(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's various flag
        /// sets to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to write information about.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to write information about.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to write information about.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to write information about.
        /// </param>
        /// <param name="headerFlags">
        /// The header flags to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteFlagInfo(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            HeaderFlags headerFlags,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified host to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteHostInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified host to the host
        /// output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteHostInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified interpreter to
        /// the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteInterpreterInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified interpreter to
        /// the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteInterpreterInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's execution
        /// engine to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteEngineInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's execution
        /// engine to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteEngineInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the entities defined in the
        /// interpreter to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteEntityInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the entities defined in the
        /// interpreter to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteEntityInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the native call stack to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteStackInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the native call stack to the
        /// host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteStackInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's control state
        /// to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteControlInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's control state
        /// to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteControlInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's test state to
        /// the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteTestInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's test state to
        /// the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteTestInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified token to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the token is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="token">
        /// The token to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteTokenInfo(
            Interpreter interpreter,
            IToken token,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified token to the host
        /// output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the token is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="token">
        /// The token to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteTokenInfo(
            Interpreter interpreter,
            IToken token,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified trace to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the trace is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified trace to the host
        /// output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the trace is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information to write.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the variable is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteVariableInfo(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified variable to the
        /// host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the variable is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="variable">
        /// The variable to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteVariableInfo(
            Interpreter interpreter,
            IVariable variable,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified object to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the object is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The object to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteObjectInfo(
            Interpreter interpreter,
            IObject @object,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the specified object to the host
        /// output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the object is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The object to write information about.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteObjectInfo(
            Interpreter interpreter,
            IObject @object,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the most recent complaint
        /// raised by the interpreter to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteComplaintInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the most recent complaint
        /// raised by the interpreter to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteComplaintInfo(
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

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        /// <summary>
        /// This method writes information about the interpreter's command
        /// execution history to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which history entries are written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteHistoryInfo(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about the interpreter's command
        /// execution history to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which history entries are written.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteHistoryInfo(
            Interpreter interpreter,
            IHistoryFilter historyFilter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes custom, host-specific information to the host
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes custom, host-specific information to the host
        /// output, using the specified
        /// colors.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the information is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteCustomInfo(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes complete information about a result, including the
        /// previous result, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="previousResult">
        /// The previous result value to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteAllResultInfo(
            ReturnCode code,
            Result result,
            int errorLine,
            Result previousResult,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes complete information about a result, including the
        /// previous result, to the host output, using the specified
        /// colors.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="previousResult">
        /// The previous result value to include in the output.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteAllResultInfo(
            ReturnCode code,
            Result result,
            int errorLine,
            Result previousResult,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about a named result to the host
        /// output.
        /// </summary>
        /// <param name="name">
        /// The name associated with the result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteResultInfo(
            string name,
            ReturnCode code,
            Result result,
            int errorLine,
            DetailFlags detailFlags,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes information about a named result to the host
        /// output, using the specified
        /// colors.
        /// </summary>
        /// <param name="name">
        /// The name associated with the result.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write information about.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a trailing end-of-line after the information.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written; otherwise, false.
        /// </returns>
        public bool WriteResultInfo(
            string name,
            ReturnCode code,
            Result result,
            int errorLine,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// This method writes the interactive loop header to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the header is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the header is associated
        /// with.
        /// </param>
        /// <param name="result">
        /// The result value to include in the header, if any.
        /// </param>
        public void WriteHeader(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the interactive loop footer to the host output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the footer is associated with.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop the footer is associated
        /// with.
        /// </param>
        /// <param name="result">
        /// The result value to include in the footer, if any.
        /// </param>
        public void WriteFooter(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            Result result
            )
        {
            CheckDisposed();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        /// <summary>
        /// This method begins rendering a box with the specified name and
        /// content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the content of the box.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully begun; otherwise, false.
        /// </returns>
        public bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends rendering a box with the specified name and content.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs that make up the content of the box.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the box was successfully ended; otherwise, false.
        /// </returns>
        public bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a box
        /// with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a box
        /// with the specified name, padding the content to a minimum width.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a box
        /// with the specified name, using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a box
        /// with the specified name, padding the content to a minimum width and
        /// using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a box
        /// with the specified name, using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified string value as the content of a box
        /// with the specified name, padding the content to a minimum width and
        /// using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="value">
        /// The string value to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            string value,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified list of name/value pairs as the content
        /// of a box with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified list of name/value pairs as the content
        /// of a box with the specified name, padding the content to a minimum
        /// width.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified list of name/value pairs as the content
        /// of a box with the specified name, using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified list of name/value pairs as the content
        /// of a box with the specified name, padding the content to a minimum width
        /// and using the specified content colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified list of name/value pairs as the content
        /// of a box with the specified name, using the specified content and box
        /// colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified list of name/value pairs as the content
        /// of a box with the specified name, padding the content to a minimum width
        /// and using the specified content and box colors.
        /// </summary>
        /// <param name="name">
        /// The name of the box.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to write as the content of the box.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="minimumLength">
        /// The minimum width, in characters, of the box content.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to emit a trailing line terminator after the box.
        /// </param>
        /// <param name="restore">
        /// Non-zero to restore the cursor position after writing the box.
        /// </param>
        /// <param name="left">
        /// On input, the column at which to begin writing; upon return,
        /// receives the resulting column.
        /// </param>
        /// <param name="top">
        /// On input, the row at which to begin writing; upon return, receives
        /// the resulting row.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use for the box content.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use for the box content.
        /// </param>
        /// <param name="boxForegroundColor">
        /// The foreground color to use for the box frame.
        /// </param>
        /// <param name="boxBackgroundColor">
        /// The background color to use for the box frame.
        /// </param>
        /// <returns>
        /// True if the box was successfully written; otherwise, false.
        /// </returns>
        public bool WriteBox(
            string name,
            StringPairList list,
            IClientData clientData,
            int minimumLength,
            bool newLine,
            bool restore,
            ref int left,
            ref int top,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor boxForegroundColor,
            ConsoleColor boxBackgroundColor
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        /// <summary>
        /// Stores a value indicating whether colorized output is disabled for this
        /// host.
        /// </summary>
        private bool noColor;
        /// <summary>
        /// Gets or sets a value indicating whether colorized output is disabled for
        /// this host.  When true, color operations have no visible effect.
        /// </summary>
        public bool NoColor
        {
            get { CheckDisposed(); return noColor; }
            set { CheckDisposed(); noColor = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the host foreground and background colors to their
        /// default values.
        /// </summary>
        /// <returns>
        /// True if the colors were reset; otherwise, false.
        /// </returns>
        public bool ResetColors()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the current foreground and background colors of the
        /// host.
        /// </summary>
        /// <param name="foregroundColor">
        /// Upon success, receives the current foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the current background color.
        /// </param>
        /// <returns>
        /// True if the colors were obtained; otherwise, false.
        /// </returns>
        public bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the specified foreground and background colors as
        /// necessary so that they are suitable for use by the host (e.g. to avoid
        /// an unreadable combination).
        /// </summary>
        /// <param name="foregroundColor">
        /// On input, the desired foreground color; on output, the adjusted
        /// foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the desired background color; on output, the adjusted
        /// background color.
        /// </param>
        /// <returns>
        /// True if the colors were adjusted; otherwise, false.
        /// </returns>
        public bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host foreground color.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to set.
        /// </param>
        /// <returns>
        /// True if the foreground color was set; otherwise, false.
        /// </returns>
        public bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host background color.
        /// </summary>
        /// <param name="backgroundColor">
        /// The background color to set.
        /// </param>
        /// <returns>
        /// True if the background color was set; otherwise, false.
        /// </returns>
        public bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host foreground and/or background colors.
        /// </summary>
        /// <param name="foreground">
        /// Non-zero to set the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to set the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to set, used only when
        /// <paramref name="foreground" /> is true.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to set, used only when
        /// <paramref name="background" /> is true.
        /// </param>
        /// <returns>
        /// True if the requested colors were set; otherwise, false.
        /// </returns>
        public bool SetColors(
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the foreground and/or background colors associated with
        /// a named entry within a color theme.
        /// </summary>
        /// <param name="theme">
        /// The name of the color theme to query, or null to use the active
        /// theme.
        /// </param>
        /// <param name="name">
        /// The name of the color entry within the theme.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to obtain the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to obtain the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// Upon success, receives the foreground color, when requested.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the background color, when requested.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode GetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the foreground and/or background colors associated with
        /// a named entry within a color theme.
        /// </summary>
        /// <param name="theme">
        /// The name of the color theme to modify, or null to use the active
        /// theme.
        /// </param>
        /// <param name="name">
        /// The name of the color entry within the theme.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to set the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to set the background color.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to set, used only when
        /// <paramref name="foreground" /> is true.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to set, used only when
        /// <paramref name="background" /> is true.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public ReturnCode SetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        /// <summary>
        /// This method resets the current position to its default value.
        /// </summary>
        /// <returns>
        /// True if the position was reset; otherwise, false.
        /// </returns>
        public bool ResetPosition()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the current position.
        /// </summary>
        /// <param name="left">
        /// Upon success, receives the zero-based column (horizontal)
        /// coordinate of the current position.
        /// </param>
        /// <param name="top">
        /// Upon success, receives the zero-based row (vertical) coordinate
        /// of the current position.
        /// </param>
        /// <returns>
        /// True if the position was obtained; otherwise, false.
        /// </returns>
        public bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the current position.
        /// </summary>
        /// <param name="left">
        /// The zero-based column (horizontal) coordinate to set as the
        /// current position.
        /// </param>
        /// <param name="top">
        /// The zero-based row (vertical) coordinate to set as the current
        /// position.
        /// </param>
        /// <returns>
        /// True if the position was set; otherwise, false.
        /// </returns>
        public bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default position.
        /// </summary>
        /// <param name="left">
        /// Upon success, receives the zero-based column (horizontal)
        /// coordinate of the default position.
        /// </param>
        /// <param name="top">
        /// Upon success, receives the zero-based row (vertical) coordinate
        /// of the default position.
        /// </param>
        /// <returns>
        /// True if the default position was obtained; otherwise, false.
        /// </returns>
        public bool GetDefaultPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the default position.
        /// </summary>
        /// <param name="left">
        /// The zero-based column (horizontal) coordinate to set as the
        /// default position.
        /// </param>
        /// <param name="top">
        /// The zero-based row (vertical) coordinate to set as the default
        /// position.
        /// </param>
        /// <returns>
        /// True if the default position was set; otherwise, false.
        /// </returns>
        public bool SetDefaultPosition(
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
        /// This method resets the size of the specified host buffer and/or window
        /// to its default.
        /// </summary>
        /// <param name="hostSizeType">
        /// <see cref="HostSizeType" /> value indicating which size should
        /// be reset.
        /// </param>
        /// <returns>
        /// True if the size was reset successfully; otherwise, false.
        /// </returns>
        public bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the size of the specified host buffer and/or
        /// window.
        /// </summary>
        /// <param name="hostSizeType">
        /// <see cref="HostSizeType" /> value indicating which size should
        /// be queried.
        /// </param>
        /// <param name="width">
        /// Upon success, this contains the width, in characters.
        /// </param>
        /// <param name="height">
        /// Upon success, this contains the height, in characters.
        /// </param>
        /// <returns>
        /// True if the size was queried successfully; otherwise, false.
        /// </returns>
        public bool GetSize(
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
        /// This method changes the size of the specified host buffer and/or
        /// window.
        /// </summary>
        /// <param name="hostSizeType">
        /// <see cref="HostSizeType" /> value indicating which size should
        /// be changed.
        /// </param>
        /// <param name="width">
        /// The new width, in characters.
        /// </param>
        /// <param name="height">
        /// The new height, in characters.
        /// </param>
        /// <returns>
        /// True if the size was changed successfully; otherwise, false.
        /// </returns>
        public bool SetSize(
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
        /// This method reads a single character from the host.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the character that was read, or a negative
        /// value if the end of the input was reached.
        /// </param>
        /// <returns>
        /// True if the character was read; otherwise, false.
        /// </returns>
        public bool Read(
            ref int value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed by
        /// the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
        public bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// This method reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed by
        /// the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
        [Obsolete()]
        public bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            )
        {
            CheckDisposed();

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        /// <summary>
        /// This method writes a single character to the host output, optionally
        /// followed by a newline.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output the specified
        /// number of times.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public bool Write(
            char value,
            int count
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output the specified
        /// number of times, optionally followed by a newline.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the characters.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public bool Write(
            char value,
            int count,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output the specified
        /// number of times, optionally followed by a newline, using the specified
        /// foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public bool Write(
            char value,
            int count,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();


            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single character to the host output using the
        /// specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        public bool Write(
            char value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output using the specified
        /// foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool Write(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output using the specified
        /// foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool Write(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally followed by a
        /// newline.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally followed by a
        /// newline, using the specified foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string to the host output, optionally followed by a
        /// newline, using the specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool Write(
            string value,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a formatted list of name and value pairs to the host
        /// output, optionally followed by a newline, using the specified foreground
        /// and background colors.
        /// </summary>
        /// <param name="list">
        /// The list of name and value pairs to write.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the formatted output.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the formatted output was written; otherwise, false.
        /// </returns>
        public bool WriteFormat(
            StringPairList list,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string followed by a newline to the host output
        /// using the specified foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool WriteLine(
            string value,
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a string followed by a newline to the host output
        /// using the specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        public bool WriteLine(
            string value,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        /// <summary>
        /// Stores the name of the profile used to load and persist this host's
        /// saved settings.
        /// </summary>
        private string profile;
        /// <summary>
        /// Gets or sets the name of the profile used to load and persist this
        /// host's saved settings.
        /// </summary>
        public string Profile
        {
            get { CheckDisposed(); return profile; }
            set { CheckDisposed(); profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags that were (or will be) used to create and configure
        /// this host.
        /// </summary>
        private HostCreateFlags hostCreateFlags;
        /// <summary>
        /// Gets or sets the flags that were (or will be) used to create and
        /// configure this host.
        /// </summary>
        public HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the default window or console title used by this host when no
        /// more specific title has been set.
        /// </summary>
        private string defaultTitle;
        /// <summary>
        /// Gets or sets the default window or console title used by this host when
        /// no more specific title has been set.
        /// </summary>
        public string DefaultTitle
        {
            get { CheckDisposed(); return defaultTitle; }
            set { CheckDisposed(); defaultTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this host should attach to an existing
        /// host environment rather than creating a new one.
        /// </summary>
        private bool useAttach;
        /// <summary>
        /// Gets or sets a value indicating whether this host should attach to an
        /// existing host environment (for example, an existing console) rather than
        /// creating a new one.
        /// </summary>
        public bool UseAttach
        {
            get { CheckDisposed(); return useAttach; }
            set { CheckDisposed(); useAttach = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether host operations that would normally be
        /// skipped or refused should instead be forced.
        /// </summary>
        private bool useForce;
        /// <summary>
        /// Gets or sets a value indicating whether host operations that would
        /// normally be skipped or refused should instead be forced.
        /// </summary>
        public bool UseForce
        {
            get { CheckDisposed(); return useForce; }
            set { CheckDisposed(); useForce = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this host should suppress changes to
        /// the window or console title.
        /// </summary>
        private bool noTitle;
        /// <summary>
        /// Gets or sets a value indicating whether this host should suppress
        /// changes to the window or console title.
        /// </summary>
        public bool NoTitle
        {
            get { CheckDisposed(); return noTitle; }
            set { CheckDisposed(); noTitle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this host should suppress changes to
        /// the window or console icon.
        /// </summary>
        private bool noIcon;
        /// <summary>
        /// Gets or sets a value indicating whether this host should suppress
        /// changes to the window or console icon.
        /// </summary>
        public bool NoIcon
        {
            get { CheckDisposed(); return noIcon; }
            set { CheckDisposed(); noIcon = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this host should skip loading and
        /// saving its profile-based settings.
        /// </summary>
        private bool noProfile;
        /// <summary>
        /// Gets or sets a value indicating whether this host should skip loading
        /// and saving its profile-based settings.
        /// </summary>
        public bool NoProfile
        {
            get { CheckDisposed(); return noProfile; }
            set { CheckDisposed(); noProfile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this host should disable interactive
        /// cancellation.
        /// </summary>
        private bool noCancel;
        /// <summary>
        /// Gets or sets a value indicating whether this host should disable
        /// interactive cancellation (for example, the cancel key handler).
        /// </summary>
        public bool NoCancel
        {
            get { CheckDisposed(); return noCancel; }
            set { CheckDisposed(); noCancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this host echoes the input it reads
        /// back to its output.
        /// </summary>
        private bool echo;
        /// <summary>
        /// Gets or sets a value indicating whether this host echoes the input it
        /// reads back to its output.
        /// </summary>
        public bool Echo
        {
            get { CheckDisposed(); return echo; }
            set { CheckDisposed(); echo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a snapshot of this host's current state, with the
        /// amount of detail controlled by the supplied flags.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags that select how much state detail is included in the
        /// result.
        /// </param>
        /// <returns>
        /// A list describing the requested host state.
        /// </returns>
        public StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits an audible tone through the host, when supported.
        /// </summary>
        /// <param name="frequency">
        /// The tone frequency, in hertz.
        /// </param>
        /// <param name="duration">
        /// The tone duration, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the tone was emitted; otherwise, false (for example, when
        /// the host does not support audible output).
        /// </returns>
        public bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the host currently has no pending
        /// interactive input or output activity.
        /// </summary>
        /// <returns>
        /// True if the host is idle; otherwise, false.
        /// </returns>
        public bool IsIdle()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's display area, when supported.
        /// </summary>
        /// <returns>
        /// True if the display was cleared; otherwise, false.
        /// </returns>
        public bool Clear()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host's configuration flags to their default
        /// values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public bool ResetHostFlags()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the host's interactive input history.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode ResetHistory(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current mode of one of the host's standard
        /// channels.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be retrieved (for example, input or
        /// output).
        /// </param>
        /// <param name="mode">
        /// Upon success, this is set to the current channel mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode GetMode(
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
        /// This method sets the mode of one of the host's standard channels.
        /// </summary>
        /// <param name="channelType">
        /// The channel whose mode is to be set (for example, input or
        /// output).
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
        public ReturnCode SetMode(
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
        /// This method opens, or re-opens, the host's underlying interactive
        /// resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the host's underlying interactive resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards any buffered host input and/or output without
        /// closing the host.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the host to its initial state, reinitializing its
        /// interactive resources.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a named output section, allowing the host to group or
        /// visually delimit related output.
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
        public bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a named output section previously begun with
        /// <see cref="BeginSection" />.
        /// </summary>
        /// <param name="name">
        /// The name of the section to end.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the section, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the section was ended; otherwise, false.
        /// </returns>
        public bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this host is currently in the process of
        /// being disposed; this property is not supported and always throws.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// Always thrown, because this host does not track whether it is currently
        /// being disposed.
        /// </exception>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                throw new NotImplementedException();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this host and suppresses
        /// finalization.
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
        /// Stores a value indicating whether this host has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this host has already been disposed.
        /// It is called at the start of most members to guard against use after
        /// disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this host has been disposed and the engine is configured to
        /// throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Null));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this host.  It implements the
        /// standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
        private /* protected virtual */ void Dispose(bool disposing)
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

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this host, releasing any resources that were not released by
        /// an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Null()
        {
            Dispose(false);
        }
        #endregion
    }
}
