/*
 * MethodResult.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Services
{
    /// <summary>
    /// This class represents the result of a service method invocation, bundling
    /// together the return code, the result string, and the error line (if any)
    /// produced by the operation.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("5427de34-b9f6-4dfc-9a83-f86b06fec2e8")]
    public sealed class MethodResult
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class representing a successful result
        /// (i.e. with a return code of <see cref="ReturnCode.Ok" />).
        /// </summary>
        public MethodResult()
            : this(ReturnCode.Ok)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified return code
        /// and no result string.
        /// </summary>
        /// <param name="returnCode">
        /// The return code produced by the method invocation.
        /// </param>
        public MethodResult(
            ReturnCode returnCode
            )
            : this(returnCode, null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified return code
        /// and result string.
        /// </summary>
        /// <param name="returnCode">
        /// The return code produced by the method invocation.
        /// </param>
        /// <param name="result">
        /// The result or error string produced by the method invocation.
        /// </param>
        public MethodResult(
            ReturnCode returnCode,
            string result
            )
            : this(returnCode, result, 0)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified return code,
        /// result string, and error line number.
        /// </summary>
        /// <param name="returnCode">
        /// The return code produced by the method invocation.
        /// </param>
        /// <param name="result">
        /// The result or error string produced by the method invocation.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if there was no
        /// error.
        /// </param>
        public MethodResult(
            ReturnCode returnCode,
            string result,
            int errorLine
            )
        {
            this.returnCode = returnCode;
            this.result = result;
            this.errorLine = errorLine;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Stores the return code produced by the method invocation.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// Gets or sets the return code produced by the method invocation.
        /// </summary>
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the result or error string produced by the method invocation.
        /// </summary>
        private string result;
        /// <summary>
        /// Gets or sets the result or error string produced by the method
        /// invocation.
        /// </summary>
        public string Result
        {
            get { return result; }
            set { result = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the line number where an error occurred, or zero if there was
        /// no error.
        /// </summary>
        private int errorLine;
        /// <summary>
        /// Gets or sets the line number where an error occurred, or zero if there
        /// was no error.
        /// </summary>
        public int ErrorLine
        {
            get { return errorLine; }
            set { errorLine = value; }
        }
        #endregion
    }
}
