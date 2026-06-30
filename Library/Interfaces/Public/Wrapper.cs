/*
 * Wrapper.cs --
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
    /// This interface is implemented by entities that wrap an arbitrary object,
    /// tracking it by token (via <see cref="IWrapperData" />) and managing its
    /// lifetime.  When the wrapped object is disposable, disposing the wrapper
    /// disposes the wrapped object as well.
    /// </summary>
    [ObjectId("166e10d1-381d-434d-b3c3-34ad9372ffd5")]
    public interface IWrapper : IWrapperData, IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the wrapped object is disposable
        /// (i.e. implements <see cref="IDisposable" />).
        /// </summary>
        bool IsDisposable { get; }
        /// <summary>
        /// Gets or sets the object wrapped by this instance.  This value may be
        /// null.
        /// </summary>
        object Object { get; set; }
    }
}
