/*
 * WebClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Specialized;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Components.Public
{
    [ObjectId("291c4c2c-2827-426f-badc-254f3e1d6e2f")]
    public class WebClientData : AnyClientData, ICloneable
    {
        #region Private Constructors
        private WebClientData(
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            string fileName,             /* in */
            byte[] rawData,              /* in */
            NameValueCollection data,    /* in */
            int? timeout,                /* in */
            bool trusted,                /* in */
            byte[] bytes,                /* in */
            bool viaClient               /* in */
            )
            : base()
        {
            this.arguments = arguments;
            this.callbackFlags = callbackFlags;
            this.uri = uri;
            this.method = method;
            this.fileName = fileName;
            this.rawData = rawData;
            this.data = data;
            this.timeout = timeout;
            this.trusted = trusted;
            this.bytes = bytes;
            this.viaClient = viaClient;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public WebClientData()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private StringList arguments;
        public StringList Arguments
        {
            get { CheckDisposed(); return arguments; }
            set { CheckDisposed(); arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private CallbackFlags callbackFlags;
        public CallbackFlags CallbackFlags
        {
            get { CheckDisposed(); return callbackFlags; }
            set { CheckDisposed(); callbackFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri uri;
        public Uri Uri
        {
            get { CheckDisposed(); return uri; }
            set { CheckDisposed(); uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string method;
        public string Method
        {
            get { CheckDisposed(); return method; }
            set { CheckDisposed(); method = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fileName;
        public string FileName
        {
            get { CheckDisposed(); return fileName; }
            set { CheckDisposed(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] rawData;
        public byte[] RawData
        {
            get { CheckDisposed(); return rawData; }
            set { CheckDisposed(); rawData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private NameValueCollection data;
        public new NameValueCollection Data
        {
            get { CheckDisposed(); return data; }
            set { CheckDisposed(); data = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int? timeout;
        public int? Timeout
        {
            get { CheckDisposed(); return timeout; }
            set { CheckDisposed(); timeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool trusted;
        public bool Trusted
        {
            get { CheckDisposed(); return trusted; }
            set { CheckDisposed(); trusted = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] bytes;
        public byte[] Bytes
        {
            get { CheckDisposed(); return bytes; }
            set { CheckDisposed(); bytes = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool viaClient;
        public bool ViaClient
        {
            get { CheckDisposed(); return viaClient; }
            set { CheckDisposed(); viaClient = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public new object Clone()
        {
            CheckDisposed();

            return new WebClientData(
                arguments, callbackFlags, uri, method, fileName,
                rawData, data, timeout, trusted, bytes, viaClient);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(WebClientData).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void Dispose(
            bool disposing /* in */
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

                        arguments = null;
                        callbackFlags = CallbackFlags.None;
                        uri = null;
                        method = null;
                        fileName = null;
                        rawData = null;
                        data = null;
                        timeout = null;
                        trusted = false;
                        bytes = null;
                        viaClient = false;
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
