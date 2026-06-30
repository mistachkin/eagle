/*
 * CoreClientData.cs --
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
    /// This interface is implemented by client data containers that carry an
    /// associated data type.  It extends <see cref="IClientData" /> and
    /// <see cref="IBaseClientData" /> with members used to query the type of
    /// the contained data.
    /// </summary>
    [ObjectId("ce2d0e84-99ba-4437-bb87-1529cdffc6c4")]
    public interface ICoreClientData : IClientData, IBaseClientData
    {
        /// <summary>
        /// Gets the type of the contained data, if it is available.
        /// </summary>
        /// <returns>
        /// The type of the contained data, or null if it is not available.
        /// </returns>
        Type MaybeGetDataType();

        /// <summary>
        /// Gets the name of the type of the contained data.
        /// </summary>
        /// <returns>
        /// The name of the type of the contained data, or null if it is not
        /// available.
        /// </returns>
        string GetDataTypeName();
    }
}
