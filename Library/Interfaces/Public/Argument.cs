/*
 * Argument.cs --
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
    /// This interface defines the contract for a single argument, such as a
    /// procedure parameter or a named value parsed from a script.  In
    /// addition to the value-related members it inherits from
    /// <see cref="IGetValue" /> and <see cref="IValueData" />, it exposes the
    /// argument name, its associated flags, and an optional default value.
    /// </summary>
    [ObjectId("dda1842c-b846-431b-8692-e87cea73d555")]
    public interface IArgument : IGetValue, IValueData
    {
        /// <summary>
        /// Gets or sets the name of this argument.
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Gets or sets the flags that describe this argument.
        /// </summary>
        ArgumentFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the default value for this argument, which is used
        /// when no explicit value is supplied.  This value may be null.
        /// </summary>
        object Default { get; set; }
        /// <summary>
        /// Resets this argument to its initial state, applying the specified
        /// flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to assign to this argument as part of the reset.
        /// </param>
        void Reset(ArgumentFlags flags);

        /// <summary>
        /// Determines whether this argument has the specified flags set.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be set;
        /// otherwise, only one of the specified flags need be set.
        /// </param>
        /// <returns>
        /// True if the requested flags are set; otherwise, false.
        /// </returns>
        bool HasFlags(ArgumentFlags hasFlags, bool all);
    }
}
