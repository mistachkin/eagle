/*
 * Set.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>set</c> command, which reads or
    /// writes the value of a variable.  When given only a variable name it
    /// returns that variable's value; when given a name and a new value it
    /// assigns the value and returns it.  See <c>core_language.md</c> for the
    /// command syntax and semantics.
    /// </summary>
    [ObjectId("a183b0df-8f44-4e9a-955a-ebd79edcfd63")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Set : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>set</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Set(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>set</c> command.  With two arguments it
        /// retrieves and returns the value of the named variable; with three
        /// arguments it assigns the supplied value to the named variable and
        /// returns the assigned value.
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
        /// command name; element one is the variable name; an optional element
        /// two supplies the new value to assign.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value read from or assigned to the
        /// variable.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the variable value
        /// placed in <paramref name="result" />; otherwise,
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
                    int count = arguments.Count;

                    if ((count == 2) || (count == 3))
                    {
                        if (count == 2)
                        {
                            code = interpreter.GetVariableValue(
                                VariableFlags.DirectGetValueMask, arguments[1],
                                ref result, ref result);
                        }
                        else if (count == 3)
                        {
                            code = interpreter.SetVariableValue2(
                                VariableFlags.DirectSetValueMask, arguments[1],
                                arguments[2].Value, null, ref result);

                            if (code == ReturnCode.Ok)
                                result = arguments[2];
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"set varName ?newValue?\"";
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
