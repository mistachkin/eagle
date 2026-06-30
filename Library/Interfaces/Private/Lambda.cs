/*
 * Lambda.cs --
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
    /// This interface is implemented by lambda expressions, the anonymous
    /// procedures created and applied via the <c>apply</c> command.  It is an
    /// aggregate that composes the procedure behavior and state
    /// (<see cref="IProcedure" />) with the lambda-specific identity and
    /// metadata (<see cref="ILambdaData" />).  It adds no members of its own.
    /// </summary>
    [ObjectId("a0c49bba-7e1e-4830-8c21-698c9b2fa8a9")]
    internal interface ILambda : IProcedure, ILambdaData
    {
        // nothing.
    }
}
