/*
 * ChangeTypeData.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface carries the input, output, and status of a single type
    /// conversion performed by the Eagle marshaller.  It composes an
    /// associated culture (<see cref="IHaveCultureInfo" />) and exposes the
    /// requested target type, the original value, the converted value, and
    /// the flags and outcome of the conversion attempt.
    /// </summary>
    [ObjectId("0026a039-64df-417b-9d36-506ecf04d904")]
    public interface IChangeTypeData : IHaveCultureInfo
    {
        /// <summary>
        /// Gets the name of the caller that requested the type conversion.
        /// </summary>
        string Caller { get; }
        /// <summary>
        /// Gets the target type that the value is being converted to.
        /// </summary>
        Type Type { get; }
        /// <summary>
        /// Gets the original value that is being converted.
        /// </summary>
        object OldValue { get; }
        /// <summary>
        /// Gets the dictionary of options that influence how the conversion
        /// is performed.
        /// </summary>
        OptionDictionary Options { get; }
        /// <summary>
        /// Gets the extra, caller-specific data associated with the
        /// conversion, if any.  This value may be null.
        /// </summary>
        IClientData ClientData { get; }

        /// <summary>
        /// Gets or sets the flags that control how the marshaller performs
        /// the conversion.
        /// </summary>
        MarshalFlags MarshalFlags { get; set; }
        /// <summary>
        /// Gets or sets the converted value produced by the conversion.
        /// </summary>
        object NewValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether opaque object handle
        /// lookup should be skipped when performing the conversion.
        /// </summary>
        bool NoHandle { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the original value was an
        /// opaque object (or object handle).
        /// </summary>
        bool WasObject { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether a conversion was
        /// attempted.
        /// </summary>
        bool Attempted { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the conversion was
        /// successfully performed.
        /// </summary>
        bool Converted { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the original value already
        /// matched the target type.
        /// </summary>
        bool DoesMatch { get; set; }
    }
}
