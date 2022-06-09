/*
 * ReadHost.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if CONSOLE
using System;
#endif

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("fc97d189-df06-4f49-800d-cca84ae0bf32")]
    public interface IReadHost : IInteractiveHost
    {
        bool Read(ref int value); /* RECOMMENDED */
        bool ReadKey(bool intercept, ref IClientData value); /* RECOMMENDED */

#if CONSOLE
        [Obsolete()]
        bool ReadKey(bool intercept, ref ConsoleKeyInfo value); /* DEPRECATED */
#endif
    }
}
