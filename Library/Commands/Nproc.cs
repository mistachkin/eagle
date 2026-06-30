/*
 * Nproc.cs --
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

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>nproc</c> command, which creates a
    /// new procedure whose arguments are passed by name rather than by
    /// position.  It behaves like the <c>proc</c> command except that the
    /// resulting procedure uses named arguments.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("2039903a-b5fc-4afa-a43e-2d20d31c2f61")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("procedure")]
    internal sealed class Nproc : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>nproc</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Nproc(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>nproc</c> command.  It parses the
        /// procedure name, formal argument list, and body, derives the named
        /// argument metadata, and adds (or replaces) the resulting procedure
        /// in the interpreter.
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
        /// command name; element one is the procedure name, element two is the
        /// formal argument list, and element three is the procedure body.
        /// This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the procedure is created
        /// successfully; otherwise, <see cref="ReturnCode.Error" /> when the
        /// wrong number of arguments is supplied, the interpreter is null, the
        /// argument list is null, or the procedure cannot be created, with
        /// details placed in <paramref name="result" />.
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
                    if (arguments.Count == 4)
                    {
                        string name = arguments[1];
                        IScriptLocation body = arguments[3];
                        StringList list1 = null;

                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[2], true, ref list1, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            ArgumentList overwriteArguments = null;
                            ArgumentList cleanArguments = null;
                            ProcedureFlags procedureFlags = ProcedureFlags.None;

                            if (!interpreter.InternalIsSafe())
                            {
                                bool isLibrary;
                                bool isPrivate;
                                bool isFast;
                                bool isAtomic;
                                bool isInline;

#if ARGUMENT_CACHE || PARSE_CACHE
                                bool isNonCaching;
#endif

                                bool isMatchTypes;

                                ScriptOps.ShouldProcedureHaveFlags(
                                    interpreter, name, (Argument)body,
                                    interpreter.InternalCultureInfo,
                                    out isLibrary, out isPrivate,
                                    out isFast, out isAtomic,
                                    out isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
                                    out isNonCaching,
#endif
                                    out isMatchTypes, out overwriteArguments,
                                    out cleanArguments);

                                code = ScriptOps.SanityCheckAndModifyProcedureFlags(
                                    isLibrary, isPrivate, isFast, isAtomic, isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
                                    isNonCaching,
#endif
                                    isMatchTypes, ref procedureFlags, ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }

                            StringPairList list2 = null;

                            code = RuntimeOps.GetFormalArgumentNamesAndDefaults(
                                interpreter, list1, ref list2, ref result);

                            ArgumentList formalArguments = null;
                            ArgumentDictionary namedArguments = null;

                            if (code == ReturnCode.Ok)
                            {
                                code = RuntimeOps.GetFormalAndNamedArguments(
                                    name, list2, ref formalArguments, ref namedArguments,
                                    ref result);
                            }

                            if (code == ReturnCode.Ok)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    procedureFlags |= interpreter.ProcedureFlags;
                                    procedureFlags &= ~ProcedureFlags.PositionalArguments;
                                    procedureFlags |= ProcedureFlags.NamedArguments;

                                    IProcedure procedure;
                                    Result error = null;

                                    procedure = RuntimeOps.NewProcedure(
                                        interpreter, interpreter.InternalAreNamespacesEnabled() ?
                                        NamespaceOps.MakeQualifiedName(interpreter, name) :
                                        ScriptOps.MakeCommandName(name), null, null,
                                        procedureFlags, formalArguments, namedArguments,
                                        overwriteArguments, cleanArguments, (Argument)body,
                                        ScriptLocation.Create(body), clientData, ref error);

                                    if (procedure != null)
                                    {
                                        code = interpreter.AddOrUpdateProcedureWithReplace(
                                            procedure, clientData, ref result);

                                        if (code == ReturnCode.Ok)
                                            result = String.Empty;
                                    }
                                    else
                                    {
                                        result = error;
                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                        }

                        if (code == ReturnCode.Error)
                        {
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (creating nproc \"{1}\")",
                                    Environment.NewLine, name));
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"nproc name args body\"";
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

        done:

            return code;
        }
        #endregion
    }
}
