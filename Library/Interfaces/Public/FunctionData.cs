/*
 * FunctionData.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the metadata describing a function (i.e. an
    /// expression function) that can be added to and evaluated by an Eagle
    /// interpreter.  It composes the function identity
    /// (<see cref="IIdentifier" />), the owning plugin
    /// (<see cref="IHavePlugin" />), wrapper bookkeeping
    /// (<see cref="IWrapperData" />), and the type-and-name pairing
    /// (<see cref="ITypeAndName" />), and adds the argument count, allowed
    /// argument types, and function flags.
    /// </summary>
    [ObjectId("e499605b-9aab-4da2-b122-2de5be55726f")]
    public interface IFunctionData : IIdentifier, IHavePlugin, IWrapperData, ITypeAndName
    {
        /// <summary>
        /// Gets or sets the number of arguments accepted by this function.
        /// This value may be zero.
        /// </summary>
        //
        // NOTE: The number of arguments for this function, may be zero.
        //
        int Arguments { get; set; }

        /// <summary>
        /// Gets or sets the list of allowed argument types for this function.
        /// </summary>
        //
        // NOTE: The list of allowed argument types for this function.
        //
        TypeList Types { get; set; }

        /// <summary>
        /// Gets or sets the flags for this function.
        /// </summary>
        //
        // NOTE: The flags for this function.
        //
        FunctionFlags Flags { get; set; }
    }
}
