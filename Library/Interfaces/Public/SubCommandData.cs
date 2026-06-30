/*
 * SubCommandData.cs --
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
    /// This interface defines the read-only and read-write metadata that
    /// describes a sub-command.  It composes the identity
    /// (<see cref="IIdentifier" />), the shared command base data
    /// (<see cref="ICommandBaseData" />), the owning command
    /// (<see cref="IHaveCommand" />), and the wrapper data
    /// (<see cref="IWrapperData" />), adding the sub-command name index and
    /// flags.
    /// </summary>
    [ObjectId("7f9e86b1-36c0-4699-97ee-5a86f3859a12")]
    public interface ISubCommandData : IIdentifier, ICommandBaseData, IHaveCommand, IWrapperData
    {
        /// <summary>
        /// Gets or sets the index, within the argument list, of the element
        /// that contains the sub-command name.
        /// </summary>
        int NameIndex { get; set; }
        /// <summary>
        /// Gets or sets the flags that control the behavior of this
        /// sub-command.
        /// </summary>
        SubCommandFlags Flags { get; set; }
    }
}
