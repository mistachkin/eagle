/*
 * FormatDataValue.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents the options used when formatting a data value
    /// (for example, a value originating from a data reader).  It extends the
    /// general value-formatting options (<see cref="IFormatValue" />) with
    /// settings specific to structured and tabular data.
    /// </summary>
    [ObjectId("a795a441-63e9-4959-ae8c-fea686007dff")]
    public interface IFormatDataValue : IFormatValue
    {
        /// <summary>
        /// Gets or sets the maximum number of values to format.
        /// </summary>
        int Limit { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the formatted output should
        /// be nested.
        /// </summary>
        bool Nested { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether any existing output should
        /// be cleared before formatting.
        /// </summary>
        bool Clear { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether null values are permitted.
        /// </summary>
        bool AllowNull { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the values should be
        /// formatted as name/value pairs.
        /// </summary>
        bool Pairs { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the names (for example,
        /// column names) should be included in the formatted output.
        /// </summary>
        bool Names { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether post-formatting fixup of
        /// the values should be skipped.
        /// </summary>
        bool NoFixup { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the formatted output should
        /// be produced as an alias.
        /// </summary>
        bool Alias { get; set; }
    }
}
