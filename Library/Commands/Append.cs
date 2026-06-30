/*
 * Append.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>append</c> command, which appends
    /// one or more values to the string stored in a variable, creating the
    /// variable if necessary.  See <c>core_language.md</c> for the command
    /// syntax and semantics.
    /// </summary>
    [ObjectId("c507edec-2507-4632-963f-2a0ce5d6373d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Append : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>append</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Append(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>append</c> command.  It appends each of
        /// the supplied values, in order, to the string value of the named
        /// variable and returns the new value; when no values are supplied it
        /// behaves as a read of the variable and (for compatibility with Tcl)
        /// raises an error if the variable does not exist.
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
        /// command name; element one is the variable name; any remaining
        /// elements are the values to append.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the new string value of the variable.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the new variable
        /// value placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the variable cannot be read or written, the
        /// interpreter is null, or the argument list is null, with details
        /// placed in <paramref name="result" />.
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
                    if (arguments.Count >= 2)
                    {
                        string varName = arguments[1];

                        if (arguments.Count == 2)
                        {
                            //
                            // NOTE: *SPECIAL CASE* For compatibility with Tcl, we must generate 
                            //       an error if only two arguments are supplied and the variable 
                            //       does not exist.
                            //
                            code = interpreter.GetVariableValue(
                                VariableFlags.DirectGetValueMask, varName, ref result,
                                ref result);
                        }
                        else
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                IHaveStringBuilder haveStringBuilder;

                                if (interpreter.GetVariableValue(
                                        VariableFlags.DirectGetValueMask, varName,
                                        ref result) == ReturnCode.Ok)
                                {
                                    haveStringBuilder = StringOps.GetIHaveStringBuilderFromObject(
                                        result, true);
                                }
                                else
                                {
                                    haveStringBuilder = StringOps.NewIHaveStringBuilder();
                                }

                                StringBuilder builder = haveStringBuilder.BuilderForReadWrite;

                                for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                    builder.Append(arguments[argumentIndex]);

                                haveStringBuilder.DoneWithReadWrite();

                                code = interpreter.SetVariableValue2(
                                    VariableFlags.DirectSetValueMask, varName,
                                    haveStringBuilder, (TraceList)null, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    result = haveStringBuilder.BuilderForReadOnly;
                                    result.EngineData = haveStringBuilder;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"append varName ?value ...?\"";
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
