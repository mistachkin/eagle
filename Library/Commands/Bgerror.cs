/*
 * Bgerror.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>bgerror</c> command, which is invoked
    /// to report an error that occurred in the background (i.e. outside of any
    /// active script context), typically by writing the error message to the
    /// interpreter host.  See <c>core_language.md</c> for the command syntax
    /// and semantics.
    /// </summary>
    [ObjectId("ed419ba9-aec5-4c49-a9a7-b41ed6b6eda0")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Bgerror : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>bgerror</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Bgerror(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>bgerror</c> command.  It reports the
        /// supplied background error message to the interpreter host (prefixed
        /// with the command name) unless background error handling has been
        /// disabled for the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name and element one supplies the background error message
        /// to report.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the error message is successfully
        /// reported; otherwise, <see cref="ReturnCode.Error" /> when the wrong
        /// number of arguments is supplied, background error handling is
        /// disabled, the interpreter host is not available, the interpreter is
        /// null, or the argument list is null, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == 2)
                    {
                        if (!interpreter.HasNoBackgroundError())
                        {
                            IInteractiveHost interactiveHost = interpreter.InternalHost;

                            if (interactiveHost != null)
                            {
                                string message = arguments[1];

                                message = !String.IsNullOrEmpty(message) ?
                                    String.Format("{0}: {1}", this.Name, message) :
                                    this.Name;

                                interactiveHost.WriteResultLine(ReturnCode.Error,
                                    message, Interpreter.GetErrorLine(interpreter));

                                result = String.Empty;
                            }
                            else
                            {
                                result = "interpreter host not available";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "background error handling disabled";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"bgerror message\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
