/*
 * PositionHost.cs --
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
    [ObjectId("30810314-8094-406d-a023-21c785b4681e")]
    public interface IPositionHost : IInteractiveHost
    {
        bool ResetPosition();

        bool GetPosition(ref int left, ref int top);
        bool SetPosition(int left, int top);

        bool GetDefaultPosition(ref int left, ref int top);
        bool SetDefaultPosition(int left, int top);
    }
}
