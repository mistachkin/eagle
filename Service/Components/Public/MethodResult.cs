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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("5427de34-b9f6-4dfc-9a83-f86b06fec2e8")]
    public sealed class MethodResult
    {
        #region Public Constructors
        public MethodResult()
            : this(ReturnCode.Ok)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult(
            ReturnCode returnCode
            )
            : this(returnCode, null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult(
            ReturnCode returnCode,
            string result
            )
            : this(returnCode, result, 0)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        private ReturnCode returnCode;
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string result;
        public string Result
        {
            get { return result; }
            set { result = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int errorLine;
        public int ErrorLine
        {
            get { return errorLine; }
            set { errorLine = value; }
        }
        #endregion
    }
}
