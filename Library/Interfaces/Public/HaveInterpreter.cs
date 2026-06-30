/*
 * HaveInterpreter.cs --
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
    /// This interface is implemented by entities that are associated with a
    /// particular interpreter context and that permit that context to be both
    /// queried and changed.  It combines the read-only access provided by
    /// <see cref="IGetInterpreter" /> with the mutating access provided by
    /// <see cref="ISetInterpreter" />.
    /// </summary>
    [ObjectId("9c0fb79d-c93e-4a93-b2ed-38625a4fc6a9")]
    public interface IHaveInterpreter : IGetInterpreter, ISetInterpreter
    {
        /// <summary>
        /// Gets or sets the interpreter context associated with this entity.
        /// This value may be null.
        /// </summary>
        //
        // TODO: Change this to use the IInterpreter type.
        //
        new Interpreter Interpreter { get; set; }
    }
}
