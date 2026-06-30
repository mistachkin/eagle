/*
 * CommandBaseData.cs --
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
    /// This interface is the common base for the metadata shared by both
    /// commands and sub-commands.  It composes type-and-name identity
    /// (<see cref="ITypeAndName" />) and exposes the behavioral flags that
    /// apply to the command or sub-command.
    /// </summary>
    [ObjectId("842aa70d-18ff-496c-a862-9ecb67af7552")]
    public interface ICommandBaseData : ITypeAndName
    {
        //
        // NOTE: The flags for this command -OR- sub-command.
        //
        /// <summary>
        /// Gets or sets the flags that control the behavior of this command
        /// or sub-command.
        /// </summary>
        CommandFlags CommandFlags { get; set; }
    }
}
