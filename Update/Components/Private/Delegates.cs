/*
 * Delegates.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Runtime.InteropServices;

namespace Eagle._Components.Private.Delegates
{
    #region Delegates
    [Guid("569a9612-a295-4419-8471-486ab6f89d21")]
    internal delegate void DelegateWithNoArgs();

    ///////////////////////////////////////////////////////////////////////////

    [Guid("3936a634-2dff-4cd9-a462-b9c671712fba")]
    internal delegate void TraceCallback(string message, string category);
    #endregion
}
