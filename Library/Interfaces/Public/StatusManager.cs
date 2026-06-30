/*
 * StatusManager.cs --
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
    /// This interface is implemented by entities that manage and report
    /// status information.  It defines methods to check, start, stop, clear,
    /// and report the current status.
    /// </summary>
    [ObjectId("f3eb371a-5949-4615-a9fb-b262503a539b")]
    public interface IStatusManager
    {
        /// <summary>
        /// Checks the current status.
        /// </summary>
        /// <param name="clientData">
        /// The extra, entity-specific data for this operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait, or null to use the
        /// default timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CheckStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Starts tracking of the status.
        /// </summary>
        /// <param name="clientData">
        /// The extra, entity-specific data for this operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait, or null to use the
        /// default timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode StartStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stops tracking of the status.
        /// </summary>
        /// <param name="clientData">
        /// The extra, entity-specific data for this operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait, or null to use the
        /// default timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode StopStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears any accumulated status information.
        /// </summary>
        /// <param name="clientData">
        /// The extra, entity-specific data for this operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait, or null to use the
        /// default timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ClearStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reports the specified status text.
        /// </summary>
        /// <param name="clientData">
        /// The extra, entity-specific data for this operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="text">
        /// The status text to be reported.  This parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait, or null to use the
        /// default timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ReportStatus(
            IClientData clientData,
            string text,
            int? timeout,
            ref Result error
        );
    }
}
