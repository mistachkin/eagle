/*
 * SocketClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Net.Sockets;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class carries the client data needed to establish and configure a
    /// client socket connection, including the target address and port, the
    /// associated interpreter and options, the various timeout and connection
    /// settings, and the eventual result of the connection attempt.
    /// </summary>
    [ObjectId("9554f738-dde6-4fce-a9a2-a3e5df6394a3")]
    internal sealed class SocketClientData : ClientData, IHaveInterpreter
    {
        #region Private Constants
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// instance.
        /// </summary>
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance with only the base client data value,
        /// leaving all other state at its default.  The public constructor
        /// delegates to this one.
        /// </summary>
        /// <param name="data">
        /// The client data value to associate with this instance.  This
        /// parameter may be null.
        /// </param>
        private SocketClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a fully specified instance describing a client socket
        /// connection and its desired configuration.
        /// </summary>
        /// <param name="data">
        /// The client data value to associate with this instance.  This
        /// parameter may be null.
        /// </param>
        /// <param name="event">
        /// The event used to signal completion of the connection attempt.
        /// This parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with this connection.  This parameter
        /// may be null.
        /// </param>
        /// <param name="options">
        /// The collection of options associated with this connection.  This
        /// parameter may be null.
        /// </param>
        /// <param name="address">
        /// The target host name or address to connect to.  This parameter may
        /// be null.
        /// </param>
        /// <param name="port">
        /// The target port to connect to.  This parameter may be null.
        /// </param>
        /// <param name="addressFamily">
        /// The address family to use for the connection, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="streamFlags">
        /// The flags controlling the behavior of the underlying stream.
        /// </param>
        /// <param name="availableTimeout">
        /// The timeout, in milliseconds, used when checking for available
        /// data, if any.  This parameter may be null.
        /// </param>
        /// <param name="readTimeout">
        /// The read timeout, in milliseconds, for the underlying stream, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="writeTimeout">
        /// The write timeout, in milliseconds, for the underlying stream, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="keepAlive">
        /// Non-zero if the keep-alive option should be enabled on the socket,
        /// if specified.  This parameter may be null.
        /// </param>
        /// <param name="exclusive">
        /// Non-zero if the socket should be bound for exclusive use.
        /// </param>
        /// <param name="text">
        /// The command text associated with this connection.  This parameter
        /// may be null.
        /// </param>
        public SocketClientData(
            object data,
            EventWaitHandle @event,
            Interpreter interpreter,
            OptionDictionary options,
            string address,
            string port,
            AddressFamily? addressFamily,
            StreamFlags streamFlags,
            int? availableTimeout,
            int? readTimeout,
            int? writeTimeout,
            bool? keepAlive,
            bool exclusive,
            string text /* command */
            )
            : this(data)
        {
            this.@event = @event;
            this.interpreter = interpreter;
            this.options = options;
            this.address = address;
            this.port = port;
            this.addressFamily = addressFamily;
            this.streamFlags = streamFlags;
            this.availableTimeout = availableTimeout;
            this.readTimeout = readTimeout;
            this.writeTimeout = writeTimeout;
            this.keepAlive = keepAlive;
            this.exclusive = exclusive;
            this.text = text;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// The interpreter associated with this connection.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter associated with this connection.
        /// </summary>
        public Interpreter Interpreter
        {
            get
            {
                lock (syncRoot)
                {
                    return interpreter;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    interpreter = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets the object used to synchronize access to the mutable state of
        /// this instance.
        /// </summary>
        public object SyncRoot
        {
            get { return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The event used to signal completion of the connection attempt.
        /// </summary>
        private EventWaitHandle @event;
        /// <summary>
        /// Gets or sets the event used to signal completion of the connection
        /// attempt.
        /// </summary>
        public EventWaitHandle Event
        {
            get
            {
                lock (syncRoot)
                {
                    return @event;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    @event = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The collection of options associated with this connection.
        /// </summary>
        private OptionDictionary options;
        /// <summary>
        /// Gets or sets the collection of options associated with this
        /// connection.
        /// </summary>
        public OptionDictionary Options
        {
            get
            {
                lock (syncRoot)
                {
                    return options;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    options = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The target host name or address to connect to.
        /// </summary>
        private string address;
        /// <summary>
        /// Gets or sets the target host name or address to connect to.
        /// </summary>
        public string Address
        {
            get
            {
                lock (syncRoot)
                {
                    return address;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    address = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The target port to connect to.
        /// </summary>
        private string port;
        /// <summary>
        /// Gets or sets the target port to connect to.
        /// </summary>
        public string Port
        {
            get
            {
                lock (syncRoot)
                {
                    return port;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    port = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The address family to use for the connection, if any.
        /// </summary>
        private AddressFamily? addressFamily;
        /// <summary>
        /// Gets or sets the address family to use for the connection, if any.
        /// </summary>
        public AddressFamily? AddressFamily
        {
            get
            {
                lock (syncRoot)
                {
                    return addressFamily;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    addressFamily = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags controlling the behavior of the underlying stream.
        /// </summary>
        private StreamFlags streamFlags;
        /// <summary>
        /// Gets or sets the flags controlling the behavior of the underlying
        /// stream.
        /// </summary>
        public StreamFlags StreamFlags
        {
            get
            {
                lock (syncRoot)
                {
                    return streamFlags;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    streamFlags = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The timeout, in milliseconds, used when checking for available
        /// data, if any.
        /// </summary>
        private int? availableTimeout;
        /// <summary>
        /// Gets or sets the timeout, in milliseconds, used when checking for
        /// available data, if any.
        /// </summary>
        public int? AvailableTimeout
        {
            get
            {
                lock (syncRoot)
                {
                    return availableTimeout;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    availableTimeout = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The read timeout, in milliseconds, for the underlying stream, if
        /// any.
        /// </summary>
        private int? readTimeout;
        /// <summary>
        /// Gets or sets the read timeout, in milliseconds, for the underlying
        /// stream, if any.
        /// </summary>
        public int? ReadTimeout
        {
            get
            {
                lock (syncRoot)
                {
                    return readTimeout;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    readTimeout = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The write timeout, in milliseconds, for the underlying stream, if
        /// any.
        /// </summary>
        private int? writeTimeout;
        /// <summary>
        /// Gets or sets the write timeout, in milliseconds, for the underlying
        /// stream, if any.
        /// </summary>
        public int? WriteTimeout
        {
            get
            {
                lock (syncRoot)
                {
                    return writeTimeout;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    writeTimeout = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the keep-alive option should be enabled on the socket,
        /// if specified.
        /// </summary>
        private bool? keepAlive;
        /// <summary>
        /// Gets or sets a value indicating whether the keep-alive option should
        /// be enabled on the socket, if specified.
        /// </summary>
        public bool? KeepAlive
        {
            get
            {
                lock (syncRoot)
                {
                    return keepAlive;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    keepAlive = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the socket should be bound for exclusive use.
        /// </summary>
        private bool exclusive;
        /// <summary>
        /// Gets or sets a value indicating whether the socket should be bound
        /// for exclusive use.
        /// </summary>
        public bool Exclusive
        {
            get
            {
                lock (syncRoot)
                {
                    return exclusive;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    exclusive = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The command text associated with this connection.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets or sets the command text associated with this connection.
        /// </summary>
        public string Text
        {
            get
            {
                lock (syncRoot)
                {
                    return text;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    text = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The return code resulting from the connection attempt.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// Gets or sets the return code resulting from the connection attempt.
        /// </summary>
        public ReturnCode ReturnCode
        {
            get
            {
                lock (syncRoot)
                {
                    return returnCode;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    returnCode = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The result, or error, produced by the connection attempt.
        /// </summary>
        private Result result;
        /// <summary>
        /// Gets or sets the result, or error, produced by the connection
        /// attempt.
        /// </summary>
        public Result Result
        {
            get
            {
                lock (syncRoot)
                {
                    return result;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    result = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Applies the configured read and write timeouts, if any, to the
        /// specified network stream.
        /// </summary>
        /// <param name="stream">
        /// The network stream to configure.  This parameter may be null, in
        /// which case this method does nothing.
        /// </param>
        public void MaybeSetTimeouts(
            NetworkStream stream
            )
        {
            if (stream == null)
                return;

            int? localReadTimeout;
            int? localWriteTimeout;

            lock (syncRoot)
            {
                localReadTimeout = readTimeout;
                localWriteTimeout = writeTimeout;
            }

            if (localReadTimeout != null)
                stream.ReadTimeout = (int)localReadTimeout;

            if (localWriteTimeout != null)
                stream.WriteTimeout = (int)localWriteTimeout;
        }
        #endregion
    }
}
