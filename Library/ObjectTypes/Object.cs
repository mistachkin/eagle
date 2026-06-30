/*
 * Object.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._ObjectTypes
{
    /// <summary>
    /// This class implements the object type used to represent opaque managed
    /// objects (i.e. arbitrary <see cref="System.Object" /> instances) within
    /// the Eagle engine.  It derives from <see cref="Default" />; its
    /// conversion methods are currently placeholders pending implementation.
    /// </summary>
    [ObjectId("53afee69-2fd1-4130-b477-cb9a0db0b39b")]
    internal sealed class Object : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the opaque managed object type.
        /// </summary>
        /// <param name="objectTypeData">
        /// The data used to create and identify this object type, such as its
        /// name and associated managed type.  This parameter may be null.
        /// </param>
        public Object(
            IObjectTypeData objectTypeData
            )
            : base(objectTypeData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectType Members
        /// <summary>
        /// Builds the internal representation of this object type from the
        /// supplied string value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// The string value to convert into the internal representation.
        /// </param>
        /// <param name="value">
        /// Upon success, receives a pointer to the newly built internal
        /// representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode SetFromAny(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Regenerates the string representation of this object type from its
        /// internal representation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the regenerated string representation.
        /// </param>
        /// <param name="value">
        /// A pointer to the internal representation to convert into a string.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode UpdateString(
            Interpreter interpreter,
            ref string text,
            IntPtr value,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a copy of the internal representation of this object type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="oldValue">
        /// A pointer to the existing internal representation to copy.
        /// </param>
        /// <param name="newValue">
        /// Upon success, receives a pointer to the newly created copy of the
        /// internal representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode Duplicate(
            Interpreter interpreter,
            IntPtr oldValue,
            ref IntPtr newValue,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts a value to this object type, replacing its internal
        /// representation in place (i.e. "shimmering" it).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// The string value associated with the value being shimmered.
        /// </param>
        /// <param name="value">
        /// On input, a pointer to the existing internal representation; upon
        /// success, receives a pointer to the new internal representation for
        /// this object type.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode Shimmer(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }
        #endregion
    }
}
