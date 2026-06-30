/*
 * FormatValue.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents the options used when formatting a value as
    /// text.  It extends the culture-aware contract
    /// (<see cref="IHaveCultureInfo" />) with settings that control how
    /// binary, date/time, numeric, and null-like values are rendered.
    /// </summary>
    [ObjectId("0defea4e-72fb-4f4b-b491-31e49e4cd802")]
    public interface IFormatValue : IHaveCultureInfo
    {
#if DATA
        /// <summary>
        /// Gets or sets the behavior used when formatting binary large object
        /// (BLOB) values.
        /// </summary>
        BlobBehavior BlobBehavior { get; set; }
        /// <summary>
        /// Gets or sets the behavior used when formatting date and time
        /// values.
        /// </summary>
        DateTimeBehavior DateTimeBehavior { get; set; }
#endif

        /// <summary>
        /// Gets or sets the kind (for example, local or UTC) assumed when
        /// formatting date and time values.
        /// </summary>
        DateTimeKind DateTimeKind { get; set; }
        /// <summary>
        /// Gets or sets the format string used when formatting date and time
        /// values.
        /// </summary>
        string DateTimeFormat { get; set; }
        /// <summary>
        /// Gets or sets the format string used when formatting numeric values.
        /// </summary>
        string NumberFormat { get; set; }
        /// <summary>
        /// Gets or sets the text used to represent a null value.
        /// </summary>
        string NullValue { get; set; }
        /// <summary>
        /// Gets or sets the text used to represent a database null value.
        /// </summary>
        string DbNullValue { get; set; }
        /// <summary>
        /// Gets or sets the text used to represent a value that could not be
        /// formatted due to an error.
        /// </summary>
        string ErrorValue { get; set; }
    }
}
