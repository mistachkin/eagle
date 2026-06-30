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
using Eagle._Interfaces.Public;

namespace Eagle._Procedures
{
    /// <summary>
    /// This class implements the core procedure type used by the Eagle
    /// library.  It derives from <see cref="Default" /> and serves as the base
    /// for the concrete procedure argument-handling strategies (see
    /// <see cref="PositionalArguments" /> and <see cref="NamedArguments" />).
    /// </summary>
    [ObjectId("5765ee79-add6-444a-a4e5-d6f80d501125")]
    public class Core : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the core procedure using the specified
        /// procedure metadata.
        /// </summary>
        /// <param name="procedureData">
        /// The data used to create and identify this procedure, such as its
        /// name, arguments, and body.  This parameter may be null.
        /// </param>
        public Core(
            IProcedureData procedureData
            )
            : base(procedureData)
        {
            // do nothing.
        }
        #endregion
    }
}
