/*
 * Rename.cs --
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
    /// This class implements the Eagle <c>rename</c> command, which renames or
    /// deletes a command (or, with the appropriate options, another kind of
    /// identifier such as a function or an object) within the interpreter.
    /// Renaming to an empty new name deletes the identifier.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("fe410cf6-1f44-47d5-a9dc-613770302383")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.SecuritySdk)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Rename : Core
    {
        #region Private Constants
        /// <summary>
        /// The error message used to report that the <c>rename</c> command was
        /// invoked with the wrong number of arguments.
        /// </summary>
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"rename ?options? oldName newName\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>rename</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Rename(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>rename</c> command.  It parses any
        /// supported options, then renames the identifier named by
        /// <c>oldName</c> to <c>newName</c> (deleting it when <c>newName</c> is
        /// empty), dispatching to the appropriate interpreter operation based
        /// on the requested identifier kind.
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
        /// command name; following any options are the old name and new name
        /// of the identifier to rename.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the identifier is renamed (or
        /// deleted) successfully; otherwise, <see cref="ReturnCode.Error" />
        /// when the interpreter is null, the argument list is null, the wrong
        /// number of arguments is supplied, an option is invalid, or the
        /// rename operation fails, with details placed in
        /// <paramref name="result" />.
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

            if (arguments.Count < 3)
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            OptionDictionary options =
                CommandOptions.GetCommandOptions(
                    CommandOptionType.Rename);

            int argumentIndex = Index.Invalid;

            if (interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid, false,
                    ref argumentIndex, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if ((argumentIndex == Index.Invalid) ||
                ((argumentIndex + 1) >= arguments.Count) ||
                ((argumentIndex + 2) < arguments.Count))
            {
                if ((argumentIndex != Index.Invalid) &&
                    Option.LooksLikeOption(arguments[argumentIndex]))
                {
                    result = OptionDictionary.BadOption(
                        options, arguments[argumentIndex],
                        !interpreter.InternalIsSafe());
                }
                else
                {
                    result = WrongNumArgs;
                }

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            IVariant value = null;
            IdentifierKind kind = IdentifierKind.None;

            if (options.IsPresent("-kind", ref value))
                kind = (IdentifierKind)value.Value;

            bool delete = true;

            if (options.IsPresent("-nodelete"))
                delete = false;

            bool hidden = false;

            if (options.IsPresent("-hidden"))
                hidden = true;

            bool hiddenOnly = false;

            if (options.IsPresent("-hiddenonly"))
                hiddenOnly = true;

            string varName = null;

            if (options.IsPresent("-newnamevar", ref value))
                varName = value.ToString();

            ///////////////////////////////////////////////////////////////////

            string oldName = arguments[argumentIndex];
            string newName = arguments[argumentIndex + 1];
            Result localResult = null;

            if (kind == IdentifierKind.Object)
            {
                if (interpreter.RenameObject(
                        oldName, newName, false, false, false,
                        ref localResult) == ReturnCode.Ok)
                {
                    result = String.Empty;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = localResult;
                    return ReturnCode.Error;
                }
            }
            else if (kind == IdentifierKind.Function)
            {
                if (interpreter.RenameFunction(
                        oldName, newName, delete,
                        ref localResult) == ReturnCode.Ok)
                {
                    result = String.Empty;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = localResult;
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (interpreter.RenameAnyIExecute(oldName,
                        newName, varName, kind, false,
                        delete, false, hidden, hiddenOnly,
                        ref localResult) == ReturnCode.Ok)
                {
                    result = String.Empty;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = localResult;
                    return ReturnCode.Error;
                }
            }
        }
        #endregion
    }
}
