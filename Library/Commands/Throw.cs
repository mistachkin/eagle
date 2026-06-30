/*
 * Throw.cs --
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
    /// This class implements the Eagle <c>throw</c> command, which raises a
    /// script exception carrying a message, an optional return code, and an
    /// optional inner exception so that it propagates out of the current
    /// script context.  See <c>core_language.md</c> for the command syntax
    /// and semantics.
    /// </summary>
    [ObjectId("c51886a9-e2d6-4cb9-ab39-9258bc2baeb9")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("control")]
    internal sealed class Throw : Core
    {
        /// <summary>
        /// The error message used when this command is invoked with the wrong
        /// number of arguments.
        /// </summary>
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"throw message ?returnCode? ?innerException?\"";

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>throw</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Throw(
            ICommandData commandData /* in */
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>throw</c> command.  It accepts a
        /// required message, an optional return code, and an optional object
        /// handle naming an inner exception, then raises a
        /// <see cref="ScriptException" /> that becomes the script result.
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
        /// command name; element one is the required message; an optional
        /// element two supplies the return code; an optional element three
        /// supplies the handle of an object that must be an exception to use
        /// as the inner exception.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.  When the
        /// arguments are processed correctly, this method does not return
        /// normally; instead it throws a <see cref="ScriptException" /> whose
        /// message becomes the script result.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Error" /> when the interpreter is null, the
        /// argument list is null, the wrong number of arguments is supplied,
        /// the return code cannot be parsed, the inner exception object cannot
        /// be resolved, or the resolved object is not an exception, with
        /// details placed in <paramref name="result" />.  On success this
        /// method throws rather than returning a value.
        /// </returns>
        /// <exception cref="ScriptException">
        /// Thrown when all arguments are processed correctly, carrying the
        /// requested return code, message, and optional inner exception out of
        /// the script context.
        /// </exception>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            int argumentCount = arguments.Count;

            if ((argumentCount < 2) || (argumentCount > 4))
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            ReturnCode returnCode = ReturnCode.Error;

            if (argumentCount >= 3)
            {
                if (Value.GetReturnCode2(
                        arguments[2], ValueFlags.AnyReturnCode,
                        interpreter.InternalCultureInfo,
                        ref returnCode, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            Exception innerException = null;

            if (argumentCount >= 4)
            {
                IObject @object = null;

                if (interpreter.GetObject(
                        arguments[3], LookupFlags.Default, ref @object,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (@object != null)
                {
                    object objectValue = @object.Value;

                    if (objectValue != null)
                    {
                        innerException = objectValue as Exception;

                        if (innerException == null)
                        {
                            result = String.Format(
                                "object \"{0}\" is not an exception",
                                arguments[3]);

                            return ReturnCode.Error;
                        }
                    }
                }
            }

            //
            // NOTE: If we managed to process all arguments correctly, use
            //       requested error message and throw a script exception.
            //       This exception is guaranteed not to escape the script
            //       engine; however, it will be the script result.
            //
            Result message = arguments[1]; /* NOTE: Implicit conversion. */

            throw new ScriptException(returnCode, message, innerException);
        }
        #endregion
    }
}
