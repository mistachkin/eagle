/*
 * NewProcedureCallback.cs --
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
    /// This interface is implemented by an entity that is able to create a new
    /// procedure on demand.  It provides the single factory method,
    /// <see cref="NewProcedure" />, used to construct a procedure from its
    /// procedure data.
    /// </summary>
    [ObjectId("fbf69595-5dc2-4118-ad35-cf8664f16e2e")]
    public interface INewProcedureCallback
    {
        /// <summary>
        /// Creates a new procedure using the specified procedure data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the procedure will belong to.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="procedureData">
        /// The data used to configure the newly created procedure.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created procedure, or null if one could not be created.
        /// </returns>
        IProcedure NewProcedure(
            Interpreter interpreter,
            IProcedureData procedureData,
            ref Result error
        );
    }
}
