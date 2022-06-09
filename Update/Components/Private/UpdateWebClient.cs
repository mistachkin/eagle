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
    [Guid("8cf49e8e-7096-4c8a-b814-621790fef991")]
    internal sealed class UpdateWebClient : WebClient
    {
        #region Public Constructors
        public UpdateWebClient(
            string userAgent
            )
        {
            this.userAgent = userAgent;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Net.WebClient Overrides
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
        private string userAgent;
        public string UserAgent
        {
            get { return userAgent; }
            set { userAgent = value; }
        }
        #endregion
    }
}
