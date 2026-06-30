/*
 * WebErrorCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that wish to be notified when
    /// an error occurs during a web transfer operation, giving them the
    /// opportunity to inspect the error and optionally influence whether the
    /// operation is retried.
    /// </summary>
    [ObjectId("44c7aa4e-42eb-4ed8-ad78-335bfb7bcec4")]
    public interface IWebErrorCallback
    {
        /// <summary>
        /// This method is called when an error occurs during a web transfer
        /// operation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the web transfer is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data supplied when the callback was
        /// registered, if any.  This parameter may be null.
        /// </param>
        /// <param name="uri">
        /// The URI involved in the web transfer that failed.  This parameter
        /// may be null.
        /// </param>
        /// <param name="webFlags">
        /// The flags that control how the web transfer is performed.
        /// </param>
        /// <param name="retries">
        /// The number of times the web transfer has already been retried.
        /// </param>
        /// <param name="timeout">
        /// The timeout for the web transfer, in milliseconds, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times the web transfer may be retried, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon entry, this may contain the result associated with the failed
        /// web transfer.  Upon return, this may be modified to reflect the
        /// outcome of handling the error.
        /// </param>
        /// <param name="errors">
        /// The list of errors that have been encountered during the web
        /// transfer; additional error details may be appended to it.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode WebError(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            WebFlags webFlags,
            int retries,
            int? timeout,
            int? maximumRetries,
            ref object result,
            ref ResultList errors
        );
    }
}
