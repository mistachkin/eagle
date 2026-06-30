/*
 * Setup.cs --
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
    /// This interface is implemented by entities that require an explicit
    /// setup (i.e. one-time initialization) step to be performed before they
    /// can be used.
    /// </summary>
    [ObjectId("61a0ed57-55db-4ce2-94dd-a8a93a290da3")]
    public interface ISetup
    {
        /// <summary>
        /// Performs any one-time setup required by this entity before it can
        /// be used.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        //
        // WARNING: This method may not throw exceptions.
        //
        [Throw(false)]
        ReturnCode Setup(ref Result error);
    }
}
