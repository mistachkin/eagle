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
    [ObjectId("7c153249-e3bb-4604-9a7d-19c57b85ffea")]
    public interface IError
    {
        ReturnCode ReturnCode { get; set; }
        ReturnCode PreviousReturnCode { get; set; }

        int ErrorLine { get; set; }
        string ErrorCode { get; set; }
        string ErrorInfo { get; set; }

        Exception Exception { get; set; }

        void Clear();
        bool Save(Interpreter interpreter);
        bool Restore(Interpreter interpreter);
    }
}
