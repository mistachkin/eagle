/*
 * Upvar.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>upvar</c> command, which links one
    /// or more local variables to variables residing in another call frame
    /// (for example a calling procedure or the global frame), so that
    /// references to the local names operate on the other-frame variables.
    /// See <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("399949c6-da0d-4061-bf14-04fcbc8a8c65")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Upvar : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>upvar</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Upvar(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>upvar</c> command.  It resolves the
        /// target call frame from the optional level argument and then, for
        /// each <c>otherVar</c>/<c>localVar</c> pair, links the local name in
        /// the current variable frame to the named variable in that target
        /// frame.
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
        /// command name; an optional level specifier follows, after which the
        /// remaining elements form one or more <c>otherVar</c>/<c>localVar</c>
        /// name pairs.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this is left empty.  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if all requested links were
        /// established; otherwise, <see cref="ReturnCode.Error" /> when the
        /// call frame cannot be resolved, the wrong number of arguments is
        /// supplied, a variable cannot be linked, the interpreter is null, or
        /// the argument list is null, with details placed in
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
                    if (arguments.Count >= 3)
                    {
                        ICallFrame otherFrame = null;

                        FrameResult frameResult = interpreter.GetCallFrame(
                            arguments[1], ref otherFrame, ref result);

                        if (frameResult != FrameResult.Invalid)
                        {
                            int count = arguments.Count - ((int)frameResult + 1);

                            if ((count & 1) == 0)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    ICallFrame localFrame = null;

                                    code = interpreter.GetVariableFrameViaResolvers(
                                        LookupFlags.Default, ref localFrame, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        int argumentIndex = ((int)frameResult + 1); // skip "upvar ?level?"

                                        for (; count > 0; count -= 2, argumentIndex += 2)
                                        {
                                            string otherName = arguments[argumentIndex];
                                            string localName = arguments[argumentIndex + 1];

                                            code = ScriptOps.LinkVariable(
                                                interpreter, localFrame, localName,
                                                otherFrame, otherName, ref result);

                                            if (code != ReturnCode.Ok)
                                                break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                result = "wrong # args: should be \"upvar ?level? otherVar localVar ?otherVar localVar ...?\"";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"upvar ?level? otherVar localVar ?otherVar localVar ...?\"";
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
