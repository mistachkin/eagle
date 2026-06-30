/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Private;

namespace Eagle._Lambdas
{
    /// <summary>
    /// This class implements the core lambda term (an anonymous procedure)
    /// used by the Eagle engine.  It derives from <see cref="Default" /> and
    /// serves as the common base class for the specialized lambda variants
    /// that differ only in how their arguments are bound to the call frame.
    /// See <c>core_language.md</c> for procedure and lambda semantics.
    /// </summary>
    [ObjectId("65fd32e4-45e7-4250-b9eb-f26405157045")]
    internal class Core : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the core lambda term.
        /// </summary>
        /// <param name="lambdaData">
        /// The data used to create and identify this lambda term, such as its
        /// name, arguments, and body.  This parameter may be null.
        /// </param>
        public Core(
            ILambdaData lambdaData
            )
            : base(lambdaData)
        {
            // do nothing.
        }
        #endregion
    }
}
