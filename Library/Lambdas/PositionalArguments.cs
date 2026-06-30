/*
 * PositionalArguments.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Lambdas
{
    /// <summary>
    /// This class implements a lambda term whose arguments are bound by
    /// position.  When executed, each caller-supplied value is matched to a
    /// formal argument by its ordinal position, default values are applied to
    /// any omitted trailing arguments, an optional final variadic argument
    /// collects any remaining values, and the lambda body is then evaluated.
    /// It derives from <see cref="Core" />.  See <c>core_language.md</c> for
    /// procedure and lambda semantics.
    /// </summary>
    [ObjectId("ba0e86a4-4e29-4760-8ff6-4ad92eed6a91")]
    internal class PositionalArguments : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the positional-argument lambda term.
        /// </summary>
        /// <param name="lambdaData">
        /// The data used to create and identify this lambda term, such as its
        /// name, arguments, and body.  This parameter may be null.
        /// </param>
        public PositionalArguments(
            ILambdaData lambdaData
            )
            : base(lambdaData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// Executes this lambda term, binding the caller-supplied arguments to
        /// the formal arguments by position, applying default values for any
        /// omitted trailing arguments, and then evaluating the lambda body.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this lambda term is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data supplied for this invocation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation, consisting of the lambda
        /// name followed by the positional argument values.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// lambda body.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            EnterLevel();

            try
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

                ProcedureFlags procedureFlags = ProcedureFlags.None;

                if (ScriptOps.MaybeCheckProcedureCaller(
                        interpreter, this, ref procedureFlags,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                IScriptLocation location = null;

                if (ScriptOps.GetAndCheckProcedureLocation(
                        interpreter, this, procedureFlags,
                        ref location, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                ArgumentList procedureArguments = this.Arguments;

                if (procedureArguments == null)
                {
                    result = "invalid procedure argument list";
                    return ReturnCode.Error;
                }

                int argumentCount = arguments.Count;
                string procedureName = this.Name;

                bool hasArgs = procedureArguments.IsVariadic(true);

                int totalArgs = hasArgs ?
                    procedureArguments.Count - 1 : procedureArguments.Count;

                int optionalArgs = procedureArguments.GetOptionalCount();

                if ((argumentCount <= 0) ||
                    ((((argumentCount - 1) < (totalArgs - optionalArgs)) ||
                    ((argumentCount - 1) > totalArgs)) && (!hasArgs ||
                    ((argumentCount - 1) < (totalArgs - optionalArgs)))))
                {
                    if (procedureArguments.Count > 0)
                    {
                        result = String.Format(
                            "wrong # args: should be \"{0} {1}\"",
                            Parser.Quote(procedureName),
                            procedureArguments.ToRawString(
                                ToStringFlags.Decorated,
                                Characters.SpaceString));
                    }
                    else
                    {
                        result = String.Format(
                            "wrong # args: should be \"{0}\"",
                            Parser.Quote(procedureName));
                    }

                    return ReturnCode.Error;
                }

                int saveCount = 0;
                int restoreCount = 0;

                bool noPushFrame = FlagOps.HasFlags(
                    procedureFlags, ProcedureFlags.NoPushFrame, true);

                ICallFrame frame = null;
                VariableDictionary savedVariables = null;

                VariableFlags variableFlags = noPushFrame ?
                    VariableFlags.None : VariableFlags.Argument;

                try
                {
                    ReturnCode code;

                    if (noPushFrame)
                    {
                        code = interpreter.GetVariableFrameViaResolvers(
                            LookupFlags.Default, ref frame, ref result);

                        if (code != ReturnCode.Ok)
                            goto done;

                        ArgumentList finalArguments;

                        ScriptOps.GetFinalArguments(
                            procedureArguments, this.OverwriteArguments,
                            out finalArguments);

                        code = frame.Save(
                            interpreter, finalArguments, ref savedVariables,
                            ref saveCount, ref result);

                        if (code != ReturnCode.Ok)
                            goto done;
                    }
                    else
                    {
                        CallFrameFlags callFrameFlags =
                            CallFrameFlags.Procedure | CallFrameFlags.Lambda;

                        frame = interpreter.NewProcedureCallFrame(
                            procedureName, callFrameFlags, null, this, arguments);

                        if (frame != null)
                        {
                            code = ReturnCode.Ok;
                        }
                        else
                        {
                            result = "could not create new procedure frame";
                            code = ReturnCode.Error;
                            goto done;
                        }
                    }

                    StringDictionary alreadySet = new StringDictionary();
                    ArgumentList frameProcedureArguments = noPushFrame ? null : new ArgumentList();

                    if (!noPushFrame)
                    {
                        frameProcedureArguments.Add(arguments[0]);
                        frame.ProcedureArguments = frameProcedureArguments;
                    }

                    for (int argumentIndex = 0; argumentIndex < procedureArguments.Count; argumentIndex++)
                    {
                        string varName = procedureArguments[argumentIndex].Name;

                        if (!alreadySet.ContainsKey(varName))
                        {
                            ArgumentFlags argumentFlags = ArgumentFlags.None;
                            object varValue;

                            if (hasArgs && (argumentIndex == (procedureArguments.Count - 1)))
                            {
                                //
                                // NOTE: This argument is part of an argument list.
                                //
                                argumentFlags |= ArgumentFlags.List;

                                //
                                // NOTE: Build the list for the final formal argument value,
                                //       which consists of all the remaining argument values.
                                //
                                ArgumentList argsArguments = new ArgumentList();

                                for (int argsArgumentIndex = argumentIndex + 1;
                                    argsArgumentIndex < argumentCount; argsArgumentIndex++)
                                {
                                    //
                                    // NOTE: Sync up the argument name and flags for use when
                                    //       debugging (below).
                                    //
                                    Argument argsArgument = Argument.GetOrCreate(
                                        interpreter, arguments[argsArgumentIndex].Flags | argumentFlags,
                                        String.Format("{0}{1}{2}", varName, Characters.Space,
                                        argsArguments.Count), arguments[argsArgumentIndex],
                                        interpreter.HasNoCacheArgument());

                                    argsArguments.Add(argsArgument);
                                }

                                varValue = argsArguments;
                            }
                            else
                            {
                                if ((argumentIndex + 1) < argumentCount)
                                {
                                    //
                                    // NOTE: Sync up the argument name for use when
                                    //       debugging (below) and use the value
                                    //       supplied by the caller.
                                    //
                                    varValue = Argument.GetOrCreate(interpreter,
                                        arguments[argumentIndex + 1].Flags | argumentFlags,
                                        varName, arguments[argumentIndex + 1],
                                        interpreter.HasNoCacheArgument());
                                }
                                else
                                {
                                    //
                                    // NOTE: We cannot sync up the argument name here
                                    //       because we are out-of-bounds on that list
                                    //       and it cannot be extended (i.e. it would
                                    //       break [info level]); therefore, we punt
                                    //       on that for now.  Use the default value
                                    //       for this argument, if any; otherwise, use
                                    //       an empty string.
                                    //
                                    object @default = procedureArguments[argumentIndex].Default;
                                    varValue = (@default != null) ? @default : Argument.NoValue;
                                }
                            }

                            code = interpreter.SetVariableValue2(
                                variableFlags, frame, varName, varValue, ref result);

                            if (code != ReturnCode.Ok)
                                break;

                            //
                            // BUGFIX: Now, also keep track of this argument in the procedure
                            //         arguments list.  Primarily because we do not want to
                            //         have to redo this logic later (i.e. for [scope]).
                            //
                            if (!noPushFrame)
                            {
                                if (varValue is Argument)
                                {
                                    frameProcedureArguments.Add((Argument)varValue);
                                }
                                else
                                {
                                    frameProcedureArguments.Add(Argument.GetOrCreate(
                                        interpreter, argumentFlags, varName, varValue,
                                        interpreter.HasNoCacheArgument()));
                                }
                            }

                            alreadySet.Add(varName, null);
                        }
                    }

                    //
                    // NOTE: Make sure we succeeded in creating the call frame.
                    //
                    if (code == ReturnCode.Ok)
                    {
                        ICallFrame savedFrame = null;

                        if (!noPushFrame)
                            interpreter.PushProcedureCallFrame(frame, true, ref savedFrame);

                        try
                        {
#if DEBUGGER && DEBUGGER_EXECUTE
                            if (DebuggerOps.CanHitBreakpoints(interpreter,
                                    EngineFlags.None, BreakpointType.BeforeLambdaBody))
                            {
                                code = interpreter.CheckBreakpoints(
                                    code, BreakpointType.BeforeLambdaBody, procedureName,
                                    null, null, this, null, clientData, arguments,
                                    ref result);
                            }
#endif

                            if (code == ReturnCode.Ok)
                            {
                                bool locked = false;

                                try
                                {
                                    bool atomic = FlagOps.HasFlags(
                                        procedureFlags, ProcedureFlags.Atomic, true);

                                    if (atomic)
                                        interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                                    if (!atomic || locked)
                                    {
#if ARGUMENT_CACHE || PARSE_CACHE
                                        EngineFlags savedEngineFlags = EngineFlags.None;

                                        bool nonCaching = FlagOps.HasFlags(
                                            procedureFlags, ProcedureFlags.NonCaching, true);

                                        if (nonCaching)
                                        {
                                            interpreter.BeginProcedureBodyNoCaching(
                                                ref savedEngineFlags);
                                        }
#endif

                                        try
                                        {
                                            string body = this.Body;

                                            interpreter.ReturnCode = ReturnCode.Ok;

                                            code = interpreter.EvaluateScript(
                                                body, location, ref result);
                                        }
                                        catch (Exception e)
                                        {
                                            result = e;
                                            code = ReturnCode.Error;
                                        }
#if ARGUMENT_CACHE || PARSE_CACHE
                                        finally
                                        {
                                            if (nonCaching)
                                            {
                                                interpreter.EndProcedureBodyNoCaching(
                                                    ref savedEngineFlags);
                                            }
                                        }
#endif
                                    }
                                    else
                                    {
                                        TraceOps.LockTrace(
                                            "Execute",
                                            typeof(PositionalArguments).Name, false,
                                            TracePriority.LockError,
                                            interpreter.MaybeWhoHasLock());

                                        result = "could not lock interpreter";
                                        code = ReturnCode.Error;
                                    }
                                }
                                finally
                                {
                                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                                }

#if DEBUGGER && DEBUGGER_EXECUTE
                                if (DebuggerOps.CanHitBreakpoints(interpreter,
                                        EngineFlags.None, BreakpointType.AfterLambdaBody))
                                {
                                    code = interpreter.CheckBreakpoints(
                                        code, BreakpointType.AfterLambdaBody, procedureName,
                                        null, null, this, null, clientData, arguments,
                                        ref result);
                                }
#endif

                                //
                                // BUGFIX: If an opaque object handle is being returned, add
                                //         a reference to it now.
                                //
                                if (ResultOps.IsOkOrReturn(code))
                                {
                                    code = interpreter.AddObjectReference(
                                        code, result, ObjectReferenceType.Return,
                                        ref result);
                                }

                                if (code == ReturnCode.Return)
                                {
                                    code = Engine.UpdateReturnInformation(interpreter);
                                }
                                else if (code == ReturnCode.Error)
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (lambda term \"{1}\" line {2})",
                                            Environment.NewLine, FormatOps.Ellipsis(procedureName),
                                            Interpreter.GetErrorLine(interpreter)));
                                }
                            }
                        }
                        finally
                        {
                            if (!noPushFrame)
                            {
                                /* IGNORED */
                                interpreter.PopProcedureCallFrame(frame, ref savedFrame);
                            }
                        }
                    }

                done:

                    return code;
                }
                finally
                {
                    if (frame != null)
                    {
                        if (noPushFrame)
                        {
                            ScriptOps.UnsetArgumentsOrComplain(
                                interpreter, frame, procedureArguments,
                                this.CleanArguments);

                            ReturnCode restoreCode;
                            Result restoreError = null;

                            restoreCode = frame.Restore(interpreter,
                                procedureArguments, ref savedVariables,
                                ref restoreCount, ref restoreError);

                            if ((restoreCode == ReturnCode.Ok) &&
                                (restoreCount != saveCount))
                            {
                                restoreError = String.Format(
                                    "failed to properly restore call frame {0} for " +
                                    "procedure {1}: restored {2} versus saved {3}",
                                    frame.Name, procedureName, restoreCount,
                                    saveCount);

                                restoreCode = ReturnCode.Error;
                            }

                            if (restoreCode != ReturnCode.Ok)
                            {
                                DebugOps.Complain(
                                    interpreter, restoreCode,
                                    restoreError);
                            }
                        }
                        else
                        {
                            IDisposable disposable = frame as IDisposable;

                            if (disposable != null)
                            {
                                disposable.Dispose();
                                disposable = null;
                            }
                        }

                        frame = null;
                    }
                }
            }
            finally
            {
                ExitLevel();
            }
        }
        #endregion
    }
}
