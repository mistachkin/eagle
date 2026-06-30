/*
 * ExecuteRequest.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that service an arbitrary
    /// request object and produce a corresponding response object.  It defines
    /// the single entry point, <see cref="Execute" />, used to dispatch a
    /// request and obtain its response.
    /// </summary>
    [ObjectId("06df1e97-84cd-47d1-8944-063c8172d831")]
    public interface IExecuteRequest
    {
        /// <summary>
        /// This method is called to service a single request and produce its
        /// response.  It reports its outcome both through the returned
        /// <see cref="ReturnCode" /> and through the
        /// <paramref name="response" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this request is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the entity was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="request">
        /// The request object to be serviced.
        /// </param>
        /// <param name="response">
        /// Upon success, this contains the response object produced for the
        /// request.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode Execute(
            Interpreter interpreter, IClientData clientData, object request,
            ref object response, ref Result error);
    }
}
