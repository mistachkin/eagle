/*
 * CallbackQueueManager.cs --
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
    [ObjectId("a3a33f2f-c360-445b-bee6-02547eed0a0d")]
    public interface ICallbackQueueManager
    {
        ///////////////////////////////////////////////////////////////////////
        // CALLBACK QUEUE CHECKING
        ///////////////////////////////////////////////////////////////////////

        bool HasCallbackQueue(
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // CALLBACK QUEUE MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode ClearCallbackQueue(
            ref Result error
            );

        ReturnCode EnqueueCallback(
            ICallback callback,
            ref Result error
            );

        ReturnCode DequeueCallback(
            ref ICallback callback,
            ref Result error
            );
    }
}
