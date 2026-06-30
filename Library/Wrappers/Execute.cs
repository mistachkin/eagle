/*
 * Execute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements the concrete wrapper for an
    /// <see cref="Eagle._Interfaces.Public.IExecute" /> object.  All of its behavior is inherited from
    /// the <see cref="Core" /> base class, which forwards the command-style
    /// execution interface to the wrapped instance.
    /// </summary>
    [ObjectId("c9d02a0d-a5dd-4c8f-9367-0cbcc738412f")]
    internal sealed class _Execute : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public _Execute()
            : base()
        {
            // do nothing.
        }
        #endregion
    }
}
