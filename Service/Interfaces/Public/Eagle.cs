/*
 * Eagle.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.ObjectModel;
using System.Web.Services;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Services
{
    [WebServiceBinding("IEagle")]
    [ObjectId("3c5bcb95-95b8-4402-9713-f0a2497a40be")]
    public interface IEagle
    {
        [WebMethod()]
        MethodResult EvaluateExpression(string text);

        [WebMethod()]
        MethodResult EvaluateExpressionWithArgs(string text,
            Collection<string> args);

        [WebMethod()]
        MethodResult EvaluateScript(string text);

        [WebMethod()]
        MethodResult EvaluateScriptWithArgs(string text,
            Collection<string> args);

        [WebMethod()]
        MethodResult EvaluateFile(string fileName);

        [WebMethod()]
        MethodResult EvaluateFileWithArgs(string fileName,
            Collection<string> args);

        [WebMethod()]
        MethodResult SubstituteString(string text);

        [WebMethod()]
        MethodResult SubstituteStringWithArgs(string text,
            Collection<string> args);

        [WebMethod()]
        MethodResult SubstituteFile(string fileName);

        [WebMethod()]
        MethodResult SubstituteFileWithArgs(string fileName,
            Collection<string> args);

        [WebMethod()]
        bool IsSuccess(ReturnCode code, bool exceptions);

        [WebMethod()]
        string FormatResult(ReturnCode code, string result, int errorLine);

        [WebMethod()]
        string FormatMethodResult(MethodResult result);
    }
}
