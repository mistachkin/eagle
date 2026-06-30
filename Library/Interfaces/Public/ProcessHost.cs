/*
 * ProcessHost.cs --
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
    /// This interface is implemented by interactive hosts that represent a
    /// hosting process and participate in its exit semantics.  It extends
    /// <see cref="IInteractiveHost" /> with the ability to query and control
    /// whether the process may exit, may be forcibly exited, and is currently
    /// in the process of exiting.
    /// </summary>
    [ObjectId("64ebb0dc-03a6-4860-8b85-350af13bd36d")]
    public interface IProcessHost : IInteractiveHost
    {
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// permitted to exit.
        /// </summary>
        bool CanExit { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// permitted to be forcibly exited.
        /// </summary>
        bool CanForceExit { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the hosting process is
        /// currently in the process of exiting.
        /// </summary>
        bool Exiting { get; set; }
    }
}
