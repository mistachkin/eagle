/*
 * ObjectType.cs --
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
    /// This interface is implemented by entities that define a custom object
    /// type, providing the operations used to convert between the string and
    /// internal representations of a value.  It extends
    /// <see cref="IObjectTypeData" /> with the conversion, update,
    /// duplication, and shimmering entry points modeled on the Tcl object
    /// type system.
    /// </summary>
    [ObjectId("8388e998-f8d7-4deb-b664-9aa88aefa54a")]
    public interface IObjectType : IObjectTypeData
    {
        /// <summary>
        /// Sets the internal representation of a value from its string
        /// representation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// The string representation to convert into the internal
        /// representation.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting internal representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // TODO: Change these to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode SetFromAny(Interpreter interpreter, string text,
            ref IntPtr value, ref Result error);

        /// <summary>
        /// Updates the string representation of a value from its internal
        /// representation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the resulting string representation.
        /// </param>
        /// <param name="value">
        /// The internal representation to convert into the string
        /// representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode UpdateString(Interpreter interpreter, ref string text,
            IntPtr value, ref Result error);

        /// <summary>
        /// Duplicates the internal representation of a value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="oldValue">
        /// The existing internal representation to duplicate.
        /// </param>
        /// <param name="newValue">
        /// Upon success, receives the duplicated internal representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode Duplicate(Interpreter interpreter, IntPtr oldValue,
            ref IntPtr newValue, ref Result error);

        /// <summary>
        /// Frees the internal representation of a value, converting it away
        /// from this object type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// The string representation of the value being shimmered.
        /// </param>
        /// <param name="value">
        /// The internal representation to be freed; upon success, this may be
        /// reset.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode Shimmer(Interpreter interpreter, string text,
            ref IntPtr value, ref Result error); // a.k.a. FreeInternalRep
    }
}
