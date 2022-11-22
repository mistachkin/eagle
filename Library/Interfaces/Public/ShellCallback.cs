/*
 * ShellCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("e8744274-1efb-47c0-83c5-41e50eff3cf8")]
    public interface IShellCallback
    {
        ReturnCode PreviewArgument(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            IInteractiveHost interactiveHost,
            IClientData clientData,
            bool whatIf,
            ref int index,
            ref string arg,
            ref IList<string> argv,
            ref Result result
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode UnknownArgument(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            IInteractiveHost interactiveHost,
            IClientData clientData,
            int switchCount,
            string arg,
            bool whatIf,
            ref IList<string> argv,
            ref Result result
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateScript(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            string text,
            ref Result result,
            ref int errorLine
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateFile(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            string fileName,
            ref Result result,
            ref int errorLine
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode EvaluateEncodedFile(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            Encoding encoding,
            string fileName,
            ref Result result,
            ref int errorLine
        );
    }
}
