/*
 * Procedure.cs --
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
    /// This interface is implemented by every procedure that can be added to
    /// and dispatched by an Eagle interpreter.  It is an aggregate that
    /// composes call-level tracking (<see cref="ILevels" />), the procedure
    /// identity and definition (<see cref="IProcedureData" />),
    /// dynamic-execute callback support
    /// (<see cref="IDynamicExecuteCallback" />), the execution entry point
    /// (<see cref="IExecute" />), and usage tracking
    /// (<see cref="IUsageData" />).  See <c>core_language.md</c> for procedure
    /// semantics.
    /// </summary>
    [ObjectId("39baec06-3ddf-4e2e-9ba6-f09491556ef6")]
    public interface IProcedure : ILevels, IProcedureData, IDynamicExecuteCallback, IExecute, IUsageData
    {
        // nothing.
    }
}
