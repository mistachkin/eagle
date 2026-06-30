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
    /// <summary>
    /// This class wraps an <see cref="IShellCallback" /> instance and forwards
    /// the interactive shell callbacks to it.  It derives from
    /// <see cref="ScriptMarshalByRefObject" /> so that it may be used to bridge
    /// shell callbacks across application domain boundaries (for example, with
    /// isolated interpreters or plugins).
    /// </summary>
    [ObjectId("3cadd0c3-16d8-4988-96e9-0ff662ebd450")]
    public sealed class ShellCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The shell callback instance that the forwarding methods of this
        /// class delegate to.  This field may be null.
        /// </summary>
        private IShellCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class that forwards shell callbacks to
        /// the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The shell callback instance to forward calls to.  This parameter may
        /// be null.
        /// </param>
        private ShellCallbackBridge(
            IShellCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards a preview-argument shell callback to the wrapped
        /// callback instance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host associated with the callback.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to indicate that the callback should not actually perform any
        /// action, only report what it would do.
        /// </param>
        /// <param name="index">
        /// On input, the index of the argument being previewed; on output, this
        /// may be modified to indicate a new index.
        /// </param>
        /// <param name="arg">
        /// On input, the argument being previewed; on output, this may be
        /// modified.  This parameter may be null.
        /// </param>
        /// <param name="argv">
        /// On input, the list of arguments; on output, this may be modified.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
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

        /// <summary>
        /// This method forwards an unknown-argument shell callback to the
        /// wrapped callback instance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host associated with the callback.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="switchCount">
        /// The number of command line switches processed so far.
        /// </param>
        /// <param name="arg">
        /// The unknown argument being processed.  This parameter may be null.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to indicate that the callback should not actually perform any
        /// action, only report what it would do.
        /// </param>
        /// <param name="argv">
        /// On input, the list of arguments; on output, this may be modified.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode UnknownArgumentCallback(
            Interpreter interpreter,
            IInteractiveHost interactiveHost,
            IClientData clientData,
            int switchCount,
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
                interpreter, interactiveHost, clientData, switchCount, arg,
                whatIf, ref argv, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards an evaluate-script shell callback to the wrapped
        /// callback instance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The script text to evaluate.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the script evaluation; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this contains the line number where the error occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
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

        /// <summary>
        /// This method forwards an evaluate-file shell callback to the wrapped
        /// callback instance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the script to evaluate.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the script evaluation; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this contains the line number where the error occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
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

        /// <summary>
        /// This method forwards an evaluate-encoded-file shell callback to the
        /// wrapped callback instance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when reading the file.  This parameter
        /// may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the script to evaluate.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the script evaluation; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this contains the line number where the error occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
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
        /// <summary>
        /// This method creates a new instance of this class that forwards shell
        /// callbacks to the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The shell callback instance to forward calls to.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge instance, or null if it could not be
        /// created (in which case <paramref name="error" /> contains an
        /// appropriate error message).
        /// </returns>
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
