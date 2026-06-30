/*
 * AsynchronousCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the contract for a callback that is invoked when
    /// an asynchronous script evaluation completes.  It provides a single
    /// entry point, <see cref="Invoke" />, that the engine calls with the
    /// context describing the completed operation and its result.
    /// </summary>
    [ObjectId("9e7e939e-766e-44d3-83af-2d00f361a3da")]
    public interface IAsynchronousCallback
    {
        /// <summary>
        /// This method is called by the engine when an asynchronous operation
        /// has completed.
        /// </summary>
        /// <param name="context">
        /// The context describing the completed asynchronous operation,
        /// including its result.  This parameter should not be null.
        /// </param>
        void Invoke(IAsynchronousContext context);
    }
}
