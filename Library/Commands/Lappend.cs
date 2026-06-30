/*
 * Lappend.cs --
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
    /// This class implements the Eagle <c>lappend</c> command, which appends
    /// one or more values as list elements to the variable named by its first
    /// argument, creating the variable as an empty list when it does not yet
    /// exist.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("1c359f9f-7a48-41e9-8897-f7a0464e8be0")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lappend : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lappend</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lappend(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lappend</c> command.  It appends each
        /// of the supplied values, in order, as list elements to the variable
        /// named by the first argument and returns the resulting list value.
        /// The variable is created as an empty list if it does not already
        /// exist.
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
        /// command name; element one is the name of the variable to append to;
        /// any remaining elements are the values appended as list elements.
        /// This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the new value of the variable, with all
        /// supplied values appended as list elements.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the resulting list
        /// value placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, or
        /// the variable value cannot be read or set, with details placed in
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
                    if (arguments.Count >= 2)
                    {
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            string name = arguments[1];

                            //
                            // BUGFIX: Do not use DoesVariableExist here, it does not currently
                            //         honor variable traces (primarily because there is currently
                            //         no such thing as a "does this exist" trace operation).
                            //
                            // BUGBUG: Potentially, if a trace callback denies us the ability to 
                            //         read the current value, we will try to set a new value instead 
                            //         of appending to the existing one; however, there is currently 
                            //         no nice way to solve this problem.  Trace callbacks should 
                            //         deny setting a new value if they deny reading the existing 
                            //         value.
                            //
                            StringList list = null;

                            code = interpreter.GetListVariableValue(
                                VariableFlags.None, name, true, false, false, false, ref list,
                                ref result);

                            if (code != ReturnCode.Ok)
                                return code;

                            //
                            // NOTE: Add all the list elements specified by the caller, in order.
                            //
                            for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                list.Add(arguments[argumentIndex]);

                            //
                            // NOTE: If the list was not parsed, that means we are using the existing
                            //       cached list representation.  Set the list value directly (i.e.
                            //       not the string representation of it).
                            //
                            code = interpreter.SetListVariableValue(
                                VariableFlags.None, name, list, null, ref result);

                            //
                            // NOTE: Return the resulting list value, with all new elements appended.
                            //
                            if (code == ReturnCode.Ok)
                                result = list;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lappend varName ?value ...?\"";
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
