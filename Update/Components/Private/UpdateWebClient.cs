/*
 * UpdateWebClient.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class extends the standard web client to set a custom user agent
    /// string on the outgoing HTTP requests it issues on behalf of the
    /// updater.
    /// </summary>
    [Guid("8cf49e8e-7096-4c8a-b814-621790fef991")]
    internal sealed class UpdateWebClient : WebClient
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="userAgent">
        /// The user agent string to set on outgoing HTTP requests, if any.
        /// This parameter may be null.
        /// </param>
        public UpdateWebClient(
            string userAgent
            )
        {
            this.userAgent = userAgent;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Net.WebClient Overrides
        /// <summary>
        /// This method creates the web request for the specified address,
        /// applying the configured user agent string when the request is an
        /// HTTP request.
        /// </summary>
        /// <param name="address">
        /// The address for which to create the web request.
        /// </param>
        /// <returns>
        /// The web request for the specified address.
        /// </returns>
        protected override WebRequest GetWebRequest(
            Uri address
            )
        {
            WebRequest webRequest = base.GetWebRequest(address);

            if ((webRequest is HttpWebRequest) && (userAgent != null))
                ((HttpWebRequest)webRequest).UserAgent = userAgent;

            return webRequest;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The user agent string applied to outgoing HTTP requests, if any.
        /// </summary>
        private string userAgent;

        /// <summary>
        /// Gets or sets the user agent string applied to outgoing HTTP
        /// requests.
        /// </summary>
        public string UserAgent
        {
            get { return userAgent; }
            set { userAgent = value; }
        }
        #endregion
    }
}
