/*
 * Error.cs --
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
    /// This interface represents a snapshot of the error-related state of an
    /// interpreter, including its return codes, error line, error code, error
    /// information, and any associated exception.  It supports clearing this
    /// state as well as saving it from and restoring it to an interpreter.
    /// </summary>
    [ObjectId("7c153249-e3bb-4604-9a7d-19c57b85ffea")]
    public interface IError
    {
        /// <summary>
        /// Gets or sets the <see cref="ReturnCode" /> associated with this
        /// error state.
        /// </summary>
        ReturnCode ReturnCode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ReturnCode" /> that was in effect prior
        /// to the one captured by this error state.
        /// </summary>
        ReturnCode PreviousReturnCode { get; set; }

        /// <summary>
        /// Gets or sets the script line number where the error occurred.
        /// </summary>
        int ErrorLine { get; set; }

        /// <summary>
        /// Gets or sets the error code (i.e. the value of the global
        /// <c>errorCode</c> variable) associated with this error state.  This
        /// value may be null.
        /// </summary>
        string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error information (i.e. the value of the global
        /// <c>errorInfo</c> variable, including the stack trace) associated
        /// with this error state.  This value may be null.
        /// </summary>
        string ErrorInfo { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Exception" /> associated with
        /// this error state, if any.  This value may be null.
        /// </summary>
        Exception Exception { get; set; }

        /// <summary>
        /// Resets this error state to its default, empty values.
        /// </summary>
        void Clear();

        /// <summary>
        /// Captures the current error-related state from the specified
        /// interpreter into this object.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose error state should be saved.  This parameter
        /// should not be null.
        /// </param>
        /// <returns>
        /// True if the error state was saved successfully; otherwise, false.
        /// </returns>
        bool Save(Interpreter interpreter);

        /// <summary>
        /// Restores the error-related state captured by this object back into
        /// the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose error state should be restored.  This
        /// parameter should not be null.
        /// </param>
        /// <returns>
        /// True if the error state was restored successfully; otherwise,
        /// false.
        /// </returns>
        bool Restore(Interpreter interpreter);
    }
}
