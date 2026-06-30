/*
 * Guid.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>guid</c> command, which creates,
    /// compares, and tests globally unique identifiers via the <c>compare</c>,
    /// <c>isnull</c>, <c>isvalid</c>, <c>new</c>, and <c>null</c>
    /// sub-commands.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("0214cfc3-e69e-4e85-8623-336b9ba8076d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("string")]
    internal sealed class _Guid : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>guid</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _Guid(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        /// <summary>
        /// The set of sub-commands supported by this command, namely
        /// <c>compare</c>, <c>isnull</c>, <c>isvalid</c>, <c>new</c>, and
        /// <c>null</c>.
        /// </summary>
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "compare", "isnull", "isvalid", "new", "null"
        });

        /// <summary>
        /// Gets the dictionary of sub-commands supported by this command,
        /// used by the engine to dispatch and validate ensemble invocations.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>guid</c> command.  It dispatches to the
        /// <c>compare</c>, <c>isnull</c>, <c>isvalid</c>, <c>new</c>, or
        /// <c>null</c> sub-command in order to create a new identifier, obtain
        /// the empty identifier, compare two identifiers, or test whether a
        /// value is empty or is a well-formed identifier.
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
        /// command name; element one is the sub-command name; the remaining
        /// elements are the arguments to that sub-command.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value produced by the selected
        /// sub-command (for example, a new or empty identifier, the result of
        /// a comparison, or a boolean test result).  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the sub-command is unknown, the
        /// wrong number of arguments is supplied, an argument cannot be parsed
        /// as an identifier, the interpreter is null, or the argument list is
        /// null, with details placed in <paramref name="result" />.
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
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            null, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "compare":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            Guid guid1 = Guid.Empty;

                                            code = Value.GetGuid(arguments[2], interpreter.InternalCultureInfo, ref guid1, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Guid guid2 = Guid.Empty;

                                                code = Value.GetGuid(arguments[3], interpreter.InternalCultureInfo, ref guid2, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = guid1.CompareTo(guid2);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid compare guid1 guid2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isnull":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            Guid guid = Guid.Empty;

                                            code = Value.GetGuid(arguments[2], interpreter.InternalCultureInfo, ref guid, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = ConversionOps.ToInt(guid.CompareTo(Guid.Empty) == 0);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid isnull guid\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isvalid":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            Guid guid = Guid.Empty;
                                            Result localResult = null;

                                            result = ConversionOps.ToInt(Value.GetGuid(arguments[2], interpreter.InternalCultureInfo, ref guid, ref localResult) == ReturnCode.Ok);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid isvalid guid\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "new":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = Guid.NewGuid();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid new\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "null":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = Guid.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid null\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"guid option ?arg ...?\"";
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
