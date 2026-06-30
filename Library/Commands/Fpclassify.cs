/*
 * Fpclassify.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for inFpclassifyion on usage and redistribution of
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
    /// This class implements the Eagle <c>fpclassify</c> command, which
    /// categorizes a floating-point value into one of the standard
    /// IEEE 754 classes (for example zero, subnormal, normal, infinite,
    /// or not-a-number).  See <c>core_language.md</c> for the command syntax
    /// and semantics.
    /// </summary>
    [ObjectId("fcf8ffd4-41d5-4956-96f4-db61570d0c94")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("expression")]
    internal sealed class Fpclassify : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>fpclassify</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Fpclassify(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>fpclassify</c> command.  It interprets
        /// its single argument as a floating-point value and reports the
        /// IEEE 754 class of that value as a lowercase name.
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
        /// command name; element one is the value whose floating-point class
        /// is to be determined.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the lowercase name of the
        /// floating-point class of the supplied value.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the class name placed
        /// in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the value cannot be parsed as a double, the
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

            if (arguments.Count != 2)
            {
                result = "wrong # args: should be \"fpclassify value\"";
                return ReturnCode.Error;
            }

            double value = 0.0;

            if (Value.GetDouble(
                    (IGetValue)arguments[1],
                    interpreter.InternalCultureInfo,
                    ref value, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            result = MathOps.Classify(value).ToString().ToLowerInvariant();
            return ReturnCode.Ok;
        }
        #endregion
    }
}
