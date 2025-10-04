/*
 * Proc.cs --
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
    [ObjectId("4fdd1172-4105-4b45-864e-30ca1b70e6c6")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("procedure")]
    internal sealed class Proc : Core
    {
        public Proc(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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
                            bool isLibrary = false;
                            bool isPrivate = false;
                            bool isFast = false;
                            bool isAtomic = false;
                            bool isInline = false;

#if ARGUMENT_CACHE || PARSE_CACHE
                            bool isNonCaching = false;
#endif

                            bool isMatchTypes = false;
                            ArgumentList overwriteArguments = null;
                            ArgumentList cleanArguments = null;

                            if (!interpreter.InternalIsSafe())
                            {
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

                                if (isInline && (isFast || isMatchTypes))
                                {
                                    result = String.Format(
                                        "cannot use the procedure annotations {0} or {1} " +
                                        "with the {2} procedure annotation.",
                                        FormatOps.WrapOrNull(
                                            ScriptOps.FormatAnnotation(Annotations.Fast)),
                                        FormatOps.WrapOrNull(
                                            ScriptOps.FormatAnnotation(Annotations.MatchTypes)),
                                        FormatOps.WrapOrNull(
                                            ScriptOps.FormatAnnotation(Annotations.Inline)));

                                    code = ReturnCode.Error;
                                    goto done;
                                }
                            }

                            StringPairList list2 = null;

                            code = RuntimeOps.GetFormalArgumentNamesAndDefaults(
                                interpreter, list1, ref list2, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                ArgumentList formalArguments = new ArgumentList(
                                    list2, ArgumentFlags.NameOnly);

                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    ProcedureFlags procedureFlags = interpreter.ProcedureFlags;

                                    procedureFlags &= ~ProcedureFlags.NamedArguments;
                                    procedureFlags |= ProcedureFlags.PositionalArguments;

                                    if (isPrivate)
                                        procedureFlags |= ProcedureFlags.Private;

                                    if (!isInline && isLibrary)
                                        procedureFlags |= ProcedureFlags.Library;

                                    if (isFast)
                                        procedureFlags |= ProcedureFlags.Fast;

                                    if (isAtomic)
                                        procedureFlags |= ProcedureFlags.Atomic;

                                    if (isInline)
                                        procedureFlags |= ProcedureFlags.NoPushFrame;

#if ARGUMENT_CACHE || PARSE_CACHE
                                    if (isLibrary || isNonCaching)
                                        procedureFlags |= ProcedureFlags.NonCaching;
#endif

                                    if (isMatchTypes)
                                        procedureFlags |= ProcedureFlags.MatchTypes;

                                    IProcedure procedure;
                                    Result error = null;

                                    procedure = RuntimeOps.NewProcedure(
                                        interpreter, interpreter.AreNamespacesEnabled() ?
                                        NamespaceOps.MakeQualifiedName(interpreter, name) :
                                        ScriptOps.MakeCommandName(name), null, null,
                                        procedureFlags, formalArguments, null, overwriteArguments,
                                        cleanArguments, (Argument)body, ScriptLocation.Create(body),
                                        clientData, ref error);

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
                                String.Format("{0}    (creating proc \"{1}\")",
                                    Environment.NewLine, name));
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"proc name args body\"";
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
