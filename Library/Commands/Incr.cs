/*
 * Incr.cs --
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
    /// This class implements the Eagle <c>incr</c> command, which increments
    /// the integer value stored in a variable by an optional amount (one by
    /// default) and returns the new value.  See <c>core_language.md</c> for
    /// the command syntax and semantics.
    /// </summary>
    [ObjectId("e620e38f-fe66-42ca-8889-3bcb0db3d62c")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("expression")]
    internal sealed class Incr : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>incr</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Incr(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>incr</c> command.  It reads the integer
        /// value of the named variable, adds the increment (one by default, or
        /// the optional amount supplied), stores the resulting value back into
        /// the variable, and returns that new value.
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
        /// command name; element one is the name of the variable to increment;
        /// an optional element two supplies the integer increment amount.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the new integer value of the variable.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the new value of the
        /// variable placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the increment or current value cannot be converted to
        /// an integer, the variable cannot be read or written, the interpreter
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
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if ((arguments.Count == 2) || (arguments.Count == 3))
                    {
                        long increment = 1;

                        if (arguments.Count == 3)
                        {
                            code = Value.GetWideInteger2(
                                (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                interpreter.InternalCultureInfo, ref increment, ref result);

                            //
                            // NOTE: Replicate "odd" Tcl behavior regarding error reporting
                            //       for converting the increment value.
                            //
                            if (code == ReturnCode.Error)
                            {
                                /* IGNORED */
                                Engine.AddErrorInformation(interpreter, result,
                                    String.Format("{0}    (reading increment)",
                                        Environment.NewLine));
                            }
                        }

                        if (code == ReturnCode.Ok)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                string name = arguments[1];
                                long value = 0;

                                code = interpreter.GetIntegerVariableValue(VariableFlags.None,
                                    name, ref value, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    value += increment;

                                    code = interpreter.SetIntegerVariableValue(VariableFlags.None,
                                        name, value, ref result);

                                    if (code == ReturnCode.Ok)
                                        result = value;
                                }
                                else if (code == ReturnCode.Error)
                                {
                                    //
                                    // NOTE: Replicate "odd" Tcl behavior regarding error reporting for
                                    //       converting the value to be incremented.
                                    //
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (reading value of variable to increment)",
                                            Environment.NewLine));
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"incr varName ?increment?\"";
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
