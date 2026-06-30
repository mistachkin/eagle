/*
 * WebTransferCallback.cs --
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
    /// This interface is implemented by entities that perform or participate
    /// in a web transfer operation, providing a single entry point that is
    /// invoked to carry out the transfer.
    /// </summary>
    [ObjectId("c381f330-a5db-4130-855c-ad33bd99dc5d")]
    public interface IWebTransferCallback
    {
        /// <summary>
        /// This method is called to perform a web transfer operation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the web transfer is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="webFlags">
        /// The flags that control how the web transfer is performed.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data supplied when the callback was
        /// registered, if any.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode WebTransfer(
            Interpreter interpreter,
            WebFlags webFlags,
            IClientData clientData,
            ref Result error
        );
    }
}
