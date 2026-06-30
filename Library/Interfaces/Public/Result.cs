/*
 * Result.cs --
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
    /// This interface represents a script result, i.e. the value produced by
    /// (or the error message resulting from) executing an entity.  It composes
    /// the underlying scalar value (<see cref="IValue" />), the associated
    /// value metadata (<see cref="IValueData" />), and the error information
    /// (<see cref="IError" />) into a single object, adding flags that describe
    /// the result and operations to reset, copy, and query it.
    /// </summary>
    [ObjectId("760b31ee-4fac-47b1-a331-852c67d80102")]
    public interface IResult : IValue, IValueData, IError
    {
        /// <summary>
        /// Gets or sets the flags that describe this result.
        /// </summary>
        ResultFlags Flags { get; set; }

        /// <summary>
        /// This method resets the selected portions of this result, based on
        /// the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags indicating which portions of this result should be reset.
        /// </param>
        void Reset(ResultFlags flags);

        /// <summary>
        /// This method creates a copy of this result, honoring the specified
        /// copy flags.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling which portions of this result are copied.
        /// </param>
        /// <returns>
        /// A new <see cref="IResult" /> that is a copy of this result.
        /// </returns>
        IResult Copy(ResultFlags flags);

        /// <summary>
        /// This method determines whether this result has the specified flags
        /// set.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to test for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all the specified flags are set; otherwise,
        /// any one of them being set is sufficient.
        /// </param>
        /// <returns>
        /// True if this result has the specified flags set; otherwise, false.
        /// </returns>
        bool HasFlags(ResultFlags hasFlags, bool all);
    }
}
