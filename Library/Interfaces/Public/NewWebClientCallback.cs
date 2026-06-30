/*
 * NewWebClientCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Net;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by an entity that is able to create a new
    /// web client on demand.  It provides the single factory method,
    /// <see cref="NewWebClient" />, used to construct a
    /// <see cref="WebClient" /> for use by the interpreter.
    /// </summary>
    [ObjectId("2d265417-d31f-45e3-b755-b52fdb180830")]
    public interface INewWebClientCallback
    {
        /// <summary>
        /// Creates a new web client for use by the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the web client will be used by.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="argument">
        /// The extra, caller-specific argument associated with the request,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data associated with the request, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created web client, or null if one could not be created.
        /// </returns>
        WebClient NewWebClient(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
        );
    }
}
