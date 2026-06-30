/*
 * ObjectTypeData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the metadata associated with a custom object
    /// type.  It composes the unique identity (<see cref="IIdentifier" />)
    /// and the wrapper bookkeeping (<see cref="IWrapperData" />).
    /// </summary>
    [ObjectId("9685e986-0229-4bd7-947b-6a98bd03364b")]
    public interface IObjectTypeData : IIdentifier, IWrapperData
    {
        /// <summary>
        /// Gets or sets the underlying type represented by this object type.
        /// </summary>
        Type Type { get; set; }
    }
}
