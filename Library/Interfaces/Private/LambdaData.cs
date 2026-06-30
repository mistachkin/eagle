/*
 * LambdaData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by entities that carry the identity and
    /// metadata describing a lambda expression.  It is an aggregate that, for
    /// now, contributes nothing beyond the procedure metadata it composes
    /// (<see cref="IProcedureData" />).
    /// </summary>
    [ObjectId("7ba4a7f5-07c3-4c40-aa30-4193111b1370")]
    internal interface ILambdaData : IProcedureData
    {
        // nothing.
    }
}
