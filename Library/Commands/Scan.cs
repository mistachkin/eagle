/*
 * Scan.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using Index = Eagle._Constants.Index;
using Width = Eagle._Constants.Width;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>scan</c> command, which parses the
    /// characters of a string according to a conversion specifier format
    /// (in the style of the C <c>sscanf</c> function), either assigning the
    /// extracted values to the named variables or, in its inline form,
    /// returning them as a list.  See <c>core_language.md</c> for the command
    /// syntax and semantics.
    /// </summary>
    [ObjectId("c17cfc38-46ab-4958-9670-29f4b29e5989")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Scan : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>scan</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Scan(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>scan</c> command.  It scans the input
        /// string using the supplied format string and, depending on the
        /// number of arguments, either assigns the converted values to the
        /// named variables or returns them as a list (the inline form, when
        /// no variable names are supplied).
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
        /// command name; element one is the string to scan; element two is
        /// the format string; any remaining elements are the names of the
        /// variables to receive the converted values.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains either the number of conversions
        /// performed (when variable names are supplied) or, for the inline
        /// form, the list of converted values.  Upon failure, this contains
        /// an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, or
        /// the scan operation fails, with details placed in
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
                        //
                        // NOTE: The "inline" form (no variable name arguments)
                        //       returns the scanned values as a list instead of
                        //       assigning them to variables.
                        //
                        bool inline = (arguments.Count == 3);

                        code = StringOps.DoScan(
                            interpreter, arguments[1], arguments[2],
                            arguments, 3, inline, ref result);
                    }
                    else
                    {
                        result = "wrong # args: should be \"scan string format ?varName varName ...?\"";
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
