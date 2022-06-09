/*
 * ShellCallbackBridge.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("3cadd0c3-16d8-4988-96e9-0ff662ebd450")]
    public sealed class ShellCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IShellCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ShellCallbackBridge(
            IShellCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public ReturnCode PreviewArgumentCallback(
            Interpreter interpreter,
            IInteractiveHost interactiveHost,
            IClientData clientData,
            bool whatIf,
            ref int index,
            ref string arg,
            ref IList<string> argv,
            ref Result result
            )
        {
            if (callback == null)
            {
                result = "invalid shell callback";
                return ReturnCode.Error;
            }

            return callback.PreviewArgument(
                interpreter, interactiveHost, clientData, whatIf,
                ref index, ref arg, ref argv, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode UnknownArgumentCallback(
            Interpreter interpreter,
            IInteractiveHost interactiveHost,
            IClientData clientData,
            int count,
            string arg,
            bool whatIf,
            ref IList<string> argv,
            ref Result result
            )
        {
            if (callback == null)
            {
                result = "invalid shell callback";
                return ReturnCode.Error;
            }

            return callback.UnknownArgument(
                interpreter, interactiveHost, clientData, count, arg,
                whatIf, ref argv, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode EvaluateScriptCallback(
            Interpreter interpreter,
            string text,
            ref Result result,
            ref int errorLine
            )
        {
            if (callback == null)
            {
                result = "invalid shell callback";
                return ReturnCode.Error;
            }

            return callback.EvaluateScript(
                interpreter, text, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode EvaluateFileCallback(
            Interpreter interpreter,
            string fileName,
            ref Result result,
            ref int errorLine
            )
        {
            if (callback == null)
            {
                result = "invalid shell callback";
                return ReturnCode.Error;
            }

            return callback.EvaluateFile(
                interpreter, fileName, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode EvaluateEncodedFileCallback(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            ref Result result,
            ref int errorLine
            )
        {
            if (callback == null)
            {
                result = "invalid shell callback";
                return ReturnCode.Error;
            }

            return callback.EvaluateEncodedFile(
                interpreter, encoding, fileName, ref result, ref errorLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ShellCallbackBridge Create(
            IShellCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid shell callback";
                return null;
            }

            return new ShellCallbackBridge(callback);
        }
        #endregion
    }
}
