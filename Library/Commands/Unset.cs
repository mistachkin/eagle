/*
 * Unset.cs --
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
    /// This class implements the Eagle <c>unset</c> command, which removes one
    /// or more variables (scalars, arrays, or array elements) from the current
    /// scope.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("fcafc062-3490-4cf9-83d7-7ddb2c1e8838")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Unset : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>unset</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Unset(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>unset</c> command.  It parses any
        /// supplied options into a set of <see cref="VariableFlags" /> and then
        /// removes each named variable from the interpreter, optionally
        /// suppressing complaints about missing variables and controlling link,
        /// trace, removal, and purge behavior.
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
        /// command name; the remaining elements are an optional set of options
        /// followed by the names of the variables to unset.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when all named variables are removed
        /// (or when no variable names are supplied); otherwise,
        /// <see cref="ReturnCode.Error" /> when option parsing fails, a
        /// variable cannot be removed, the wrong number of arguments is
        /// supplied, the interpreter is null, or the argument list is null,
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 1)
                    {
                        if (arguments.Count > 1)
                        {
                            OptionDictionary options =
                                CommandOptions.GetCommandOptions(
                                    CommandOptionType.Unset);

                            int argumentIndex = Index.Invalid;

                            if (interpreter.GetOptions(
                                    options, arguments, 0, 1, Index.Invalid, true,
                                    ref argumentIndex, ref result) == ReturnCode.Ok)
                            {
                                if (argumentIndex != Index.Invalid)
                                {
                                    //
                                    // TODO: Is this really needed to be Tcl compliant?
                                    //
                                    VariableFlags flags = VariableFlags.NoRemove;

                                    if (options.IsPresent("-unlinkonly"))
                                        flags |= VariableFlags.NoFollowLink;

                                    if (options.IsPresent("-nocomplain"))
                                        flags |= VariableFlags.NoComplain;

                                    if (options.IsPresent("-remove"))
                                        flags &= ~VariableFlags.NoRemove;

                                    if (options.IsPresent("-notrace"))
                                        flags |= VariableFlags.NoTrace;

                                    if (options.IsPresent("-purge"))
                                        flags |= VariableFlags.Purge;

                                    ///////////////////////////////////////////////////////////////////

#if !MONO && NATIVE && WINDOWS
                                    if (options.IsPresent("-zerostring"))
                                    {
                                        flags |= VariableFlags.ZeroStringMask;
                                    }
                                    else if (!CommonOps.Runtime.IsMono() &&
                                        options.IsPresent("-maybezerostring"))
                                    {
                                        flags |= VariableFlags.ZeroStringMask;
                                    }
#endif

                                    ///////////////////////////////////////////////////////////////////

                                    for (; argumentIndex < arguments.Count; argumentIndex++)
                                    {
                                        if (interpreter.UnsetVariable(
                                                flags, arguments[argumentIndex],
                                                ref result) != ReturnCode.Ok)
                                        {
                                            return ReturnCode.Error;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                return ReturnCode.Error;
                            }
                        }

                        /*
                         * Do nothing if no arguments supplied, so as to match
                         * command documentation.
                         */
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        result = "wrong # args: should be \"unset ?options? ?varName varName ...?\"";
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
