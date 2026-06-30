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
using System.IO;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class encapsulates the client data associated with a web operation,
    /// including the arguments, callback flags, target uniform resource
    /// identifier, request method, file name, raw data, name/value data,
    /// timeout, trust setting, stream, byte payload, and whether the operation
    /// is performed via the web client.  It can be cloned and supports the
    /// standard disposal pattern.
    /// </summary>
    [ObjectId("291c4c2c-2827-426f-badc-254f3e1d6e2f")]
    public class WebClientData : AnyClientData, ICloneable
    {
        #region Private Constructors
        /// <summary>
        /// Constructs web client data from the fully specified set of web
        /// operation parameters.
        /// </summary>
        /// <param name="arguments">
        /// The arguments associated with the web operation.  This parameter may
        /// be null.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags controlling callback behavior for the web operation.
        /// </param>
        /// <param name="uri">
        /// The target uniform resource identifier for the web operation.  This
        /// parameter may be null.
        /// </param>
        /// <param name="method">
        /// The request method for the web operation.  This parameter may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The file name associated with the web operation.  This parameter may
        /// be null.
        /// </param>
        /// <param name="rawData">
        /// The raw data associated with the web operation.  This parameter may
        /// be null.
        /// </param>
        /// <param name="data">
        /// The name/value data associated with the web operation.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, for the web operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero if the web operation is trusted, if specified.  This
        /// parameter may be null.
        /// </param>
        /// <param name="stream">
        /// The stream associated with the web operation.  This parameter may be
        /// null.
        /// </param>
        /// <param name="bytes">
        /// The byte payload associated with the web operation.  This parameter
        /// may be null.
        /// </param>
        /// <param name="viaClient">
        /// Non-zero if the web operation is performed via the web client.
        /// </param>
        private WebClientData(
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            string fileName,             /* in */
            byte[] rawData,              /* in */
            NameValueCollection data,    /* in */
            int? timeout,                /* in */
            bool? trusted,               /* in */
            Stream stream,               /* in */
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
            this.stream = stream;
            this.bytes = bytes;
            this.viaClient = viaClient;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs empty web client data with default values.
        /// </summary>
        public WebClientData()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The arguments associated with the web operation, if any.
        /// </summary>
        private StringList arguments;
        /// <summary>
        /// Gets or sets the arguments associated with the web operation.
        /// </summary>
        public StringList Arguments
        {
            get { CheckDisposed(); return arguments; }
            set { CheckDisposed(); arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags controlling callback behavior for the web operation.
        /// </summary>
        private CallbackFlags callbackFlags;
        /// <summary>
        /// Gets or sets the flags controlling callback behavior for the web
        /// operation.
        /// </summary>
        public CallbackFlags CallbackFlags
        {
            get { CheckDisposed(); return callbackFlags; }
            set { CheckDisposed(); callbackFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The target uniform resource identifier for the web operation, if
        /// any.
        /// </summary>
        private Uri uri;
        /// <summary>
        /// Gets or sets the target uniform resource identifier for the web
        /// operation.
        /// </summary>
        public Uri Uri
        {
            get { CheckDisposed(); return uri; }
            set { CheckDisposed(); uri = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The request method for the web operation, if any.
        /// </summary>
        private string method;
        /// <summary>
        /// Gets or sets the request method for the web operation.
        /// </summary>
        public string Method
        {
            get { CheckDisposed(); return method; }
            set { CheckDisposed(); method = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name associated with the web operation, if any.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets or sets the file name associated with the web operation.
        /// </summary>
        public string FileName
        {
            get { CheckDisposed(); return fileName; }
            set { CheckDisposed(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The raw data associated with the web operation, if any.
        /// </summary>
        private byte[] rawData;
        /// <summary>
        /// Gets or sets the raw data associated with the web operation.
        /// </summary>
        public byte[] RawData
        {
            get { CheckDisposed(); return rawData; }
            set { CheckDisposed(); rawData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name/value data associated with the web operation, if any.
        /// </summary>
        private NameValueCollection data;
        /// <summary>
        /// Gets or sets the name/value data associated with the web operation.
        /// </summary>
        public new NameValueCollection Data
        {
            get { CheckDisposed(); return data; }
            set { CheckDisposed(); data = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The timeout, in milliseconds, for the web operation, if any.
        /// </summary>
        private int? timeout;
        /// <summary>
        /// Gets or sets the timeout, in milliseconds, for the web operation.
        /// </summary>
        public int? Timeout
        {
            get { CheckDisposed(); return timeout; }
            set { CheckDisposed(); timeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-null, non-zero if the web operation is trusted.
        /// </summary>
        private bool? trusted;
        /// <summary>
        /// Gets or sets whether the web operation is trusted.
        /// </summary>
        public bool? Trusted
        {
            get { CheckDisposed(); return trusted; }
            set { CheckDisposed(); trusted = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The stream associated with the web operation, if any.
        /// </summary>
        private Stream stream;
        /// <summary>
        /// Gets or sets the stream associated with the web operation.
        /// </summary>
        public Stream Stream
        {
            get { CheckDisposed(); return stream; }
            set { CheckDisposed(); stream = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The byte payload associated with the web operation, if any.
        /// </summary>
        private byte[] bytes;
        /// <summary>
        /// Gets or sets the byte payload associated with the web operation.
        /// </summary>
        public byte[] Bytes
        {
            get { CheckDisposed(); return bytes; }
            set { CheckDisposed(); bytes = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the web operation is performed via the web client.
        /// </summary>
        private bool viaClient;
        /// <summary>
        /// Gets or sets whether the web operation is performed via the web
        /// client.
        /// </summary>
        public bool ViaClient
        {
            get { CheckDisposed(); return viaClient; }
            set { CheckDisposed(); viaClient = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new instance of the web client data that is a
        /// copy of this instance.
        /// </summary>
        /// <returns>
        /// A new web client data instance that is a copy of this instance.
        /// </returns>
        public new object Clone()
        {
            CheckDisposed();

            return new WebClientData(
                arguments, callbackFlags, uri, method, fileName,
                rawData, data, timeout, trusted, stream, bytes,
                viaClient);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object instance has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this object instance has been
        /// disposed and disposed-object checking is enabled.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed &&
                Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(WebClientData).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose" /> method; zero if it is being
        /// called from the finalizer.
        /// </param>
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
                        stream = null;
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
