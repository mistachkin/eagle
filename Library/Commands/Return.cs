/*
 * Return.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>return</c> command, which returns a
    /// result from the enclosing procedure, source file, or interpreter and
    /// allows the return code and error information to be controlled via the
    /// <c>-code</c>, <c>-errorinfo</c>, and <c>-errorcode</c> options.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("a54f8720-ba76-476c-91b1-3f140d587c70")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("control")]
    internal sealed class Return : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>return</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Return(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>return</c> command.  It parses any
        /// supplied <c>-code</c>, <c>-errorinfo</c>, and <c>-errorcode</c>
        /// options, records the requested return code and error information on
        /// the interpreter, and returns <see cref="ReturnCode.Return" /> so
        /// that the enclosing procedure, file, or interpreter unwinds with the
        /// optional result string.
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
        /// command name; the remaining elements supply the optional
        /// <c>-code</c>, <c>-errorinfo</c>, and <c>-errorcode</c> options
        /// followed by an optional result string.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the optional result string (or an
        /// empty string when none was supplied).  Upon failure, this contains
        /// an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Return" /> when invoked correctly, signaling
        /// the enclosing scope to unwind with the requested return code;
        /// otherwise, <see cref="ReturnCode.Error" /> when option parsing
        /// fails, the wrong number of arguments is supplied, the interpreter
        /// is null, or the argument list is null, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 1)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Return);

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 1)
                            code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);
                        else
                            code = ReturnCode.Ok;
                        
                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                            {
                                //
                                // NOTE: Always reset these first.  They may be set to something
                                //       below.
                                //
                                interpreter.ErrorInfo = null;
                                interpreter.ErrorCode = null;

                                //
                                // NOTE: Initialize the variable that will hold the supplied option 
                                //       values, if any.
                                //
                                IVariant value = null;

                                //
                                // NOTE: Process the -code option.
                                //
                                if (options.IsPresent("-code", ref value))
                                    interpreter.ReturnCode = (ReturnCode)value.Value;
                                else
                                    interpreter.ReturnCode = ReturnCode.Ok;

                                //
                                // NOTE: If the -code option indicates an error then we also want to 
                                //       process the values for the -errorinfo and -errorcode options.
                                //
                                if (interpreter.ReturnCode == ReturnCode.Error)
                                {
                                    if (options.IsPresent("-errorcode", ref value))
                                    {
                                        interpreter.ErrorCode = value.ToString();
                                        Engine.SetErrorCodeSet(interpreter, true);
                                    }

                                    if (options.IsPresent("-errorinfo", ref value))
                                    {
                                        interpreter.ErrorInfo = value.ToString();
                                        Engine.SetErrorInProgress(interpreter, true);
                                    }
                                }

                                //
                                // NOTE: Is an actual string value being returned?
                                //
                                if (argumentIndex != Index.Invalid)
                                    result = arguments[argumentIndex];
                                else
                                    result = String.Empty;

                                code = ReturnCode.Return;
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(
                                        options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    result = "wrong # args: should be \"return ?-code code? ?-errorinfo info? ?-errorcode code? ?string?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"return ?-code code? ?-errorinfo info? ?-errorcode code? ?string?\"";
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
