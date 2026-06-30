/*
 * ResolveData.cs --
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
    /// This interface defines the identity and metadata for a custom resolver.
    /// It composes the unique identity (<see cref="IIdentifier" />), the owning
    /// interpreter (<see cref="IHaveInterpreter" />), and the wrapper
    /// bookkeeping (<see cref="IWrapperData" />), and adds the resolver flags.
    /// </summary>
    [ObjectId("79e8f73c-d287-4715-824b-d5cc3cdc240f")]
    public interface IResolveData : IIdentifier, IHaveInterpreter, IWrapperData
    {
        //
        // NOTE: The flags for this resolver.
        //
        /// <summary>
        /// Gets or sets the flags that control the behavior of this resolver.
        /// </summary>
        ResolveFlags Flags { get; set; }
    }
}
