/*
 * StringTransformCallback.cs --
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
    /// This interface is implemented by entities that can transform one
    /// string value into another.  It defines the single entry point,
    /// <see cref="StringTransform" />, used to apply the transformation.
    /// </summary>
    [ObjectId("2fde7f63-d35f-4592-8c43-dbee8bb3cb0f")]
    public interface IStringTransformCallback
    {
        /// <summary>
        /// Transforms the specified string value and returns the result.
        /// </summary>
        /// <param name="value">
        /// The string value to be transformed.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The transformed string value.
        /// </returns>
        string StringTransform(
            string value
        );
    }
}
