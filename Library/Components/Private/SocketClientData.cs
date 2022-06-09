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
    [ObjectId("9554f738-dde6-4fce-a9a2-a3e5df6394a3")]
    internal sealed class SocketClientData : ClientData, IHaveInterpreter
    {
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private SocketClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public SocketClientData(
            object data,
            EventWaitHandle @event,
            Interpreter interpreter,
            OptionDictionary options,
            string address,
            string port,
            AddressFamily addressFamily,
            StreamFlags streamFlags,
            int? availableTimeout,
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
            this.exclusive = exclusive;
            this.text = text;
        }

        ///////////////////////////////////////////////////////////////////////

        public object SyncRoot
        {
            get { return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
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

        private EventWaitHandle @event;
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

        private OptionDictionary options;
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

        private string address;
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

        private string port;
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

        private AddressFamily addressFamily;
        public AddressFamily AddressFamily
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

        private StreamFlags streamFlags;
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

        private int? availableTimeout;
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

        private bool exclusive;
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

        private string text;
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

        private ReturnCode returnCode;
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

        private Result result;
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
    }
}
