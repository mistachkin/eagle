/*
 * CallbackQueue.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;

namespace Eagle._Containers.Private
{
    [ObjectId("376abeb8-61de-44d6-9a86-b4bdc8c8c48a")]
    internal sealed class CallbackQueue : Queue<CommandCallback>
    {
        public CallbackQueue()
            : base()
        {
            // do nothing.
        }
    }
}
