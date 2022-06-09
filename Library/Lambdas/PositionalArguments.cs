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
    [ObjectId("ba0e86a4-4e29-4760-8ff6-4ad92eed6a91")]
    internal class PositionalArguments : Core
    {
        #region Public Constructors
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
                ReturnCode code = ReturnCode.Ok;

                if (interpreter != null)
                {
                    if (arguments != null)
                    {
                        IScriptLocation location = null;

                        code = ScriptOps.GetAndCheckProcedureLocation(
                            interpreter, this, ref location, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            string procedureName = this.Name;
                            ArgumentList procedureArguments = this.Arguments;

                            if (procedureArguments != null)
                            {
                                int argumentCount = arguments.Count;
                                bool hasArgs = procedureArguments.IsVariadic(true);
                                int totalArgs = hasArgs ? procedureArguments.Count - 1 : procedureArguments.Count;
                                int optionalArgs = procedureArguments.GetOptionalCount();

                                if ((argumentCount > 0) &&
                                    ((((argumentCount - 1) >= (totalArgs - optionalArgs)) &&
                                    ((argumentCount - 1) <= totalArgs)) ||
                                    (hasArgs && ((argumentCount - 1) >= (totalArgs - optionalArgs)))))
                                {
                                    ICallFrame frame = null;

                                    try
                                    {
                                        frame = interpreter.NewProcedureCallFrame(
                                            procedureName, CallFrameFlags.Procedure | CallFrameFlags.Lambda,
                                            null, this, arguments);

                                        VariableDictionary variables = frame.Variables;

                                        frame.ProcedureArguments = new ArgumentList(arguments[0]);

                                        for (int argumentIndex = 0; argumentIndex < procedureArguments.Count; argumentIndex++)
                                        {
                                            string varName = procedureArguments[argumentIndex].Name;

                                            if (!variables.ContainsKey(varName))
                                            {
                                                ArgumentFlags flags = ArgumentFlags.None;
                                                object varValue;

                                                if (hasArgs && (argumentIndex == (procedureArguments.Count - 1)))
                                                {
                                                    //
                                                    // NOTE: This argument is part of an argument list.
                                                    //
                                                    flags |= ArgumentFlags.ArgumentList;

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
                                                            interpreter, arguments[argsArgumentIndex].Flags | flags,
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
                                                            arguments[argumentIndex + 1].Flags | flags,
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

                                                code = interpreter.SetVariableValue2(VariableFlags.Argument, frame,
                                                    varName, varValue, ref result);

                                                if (code != ReturnCode.Ok)
                                                    break;

                                                //
                                                // BUGFIX: Now, also keep track of this argument in the procedure
                                                //         arguments list.  Primarily because we do not want to
                                                //         have to redo this logic later (i.e. for [scope]).
                                                //
                                                if (varValue is Argument)
                                                {
                                                    frame.ProcedureArguments.Add((Argument)varValue);
                                                }
                                                else
                                                {
                                                    frame.ProcedureArguments.Add(Argument.GetOrCreate(
                                                        interpreter, flags, varName, varValue,
                                                        interpreter.HasNoCacheArgument()));
                                                }
                                            }
                                        }

                                        //
                                        // NOTE: Make sure we succeeded in creating the call frame.
                                        //
                                        if (code == ReturnCode.Ok)
                                        {
                                            ICallFrame savedFrame = null;
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
                                                        bool atomic = EntityOps.IsAtomic(this);

                                                        if (atomic)
                                                            interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                                                        if (!atomic || locked)
                                                        {
#if ARGUMENT_CACHE || PARSE_CACHE
                                                            EngineFlags savedEngineFlags = EngineFlags.None;
                                                            bool nonCaching = EntityOps.IsNonCaching(this);

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
                                                        code = Engine.UpdateReturnInformation(interpreter);
                                                    else if (code == ReturnCode.Error)
                                                        Engine.AddErrorInformation(interpreter, result,
                                                            String.Format("{0}    (lambda term \"{1}\" line {2})",
                                                                Environment.NewLine, FormatOps.Ellipsis(procedureName),
                                                                Interpreter.GetErrorLine(interpreter)));
                                                }
                                            }
                                            finally
                                            {
                                                /* IGNORED */
                                                interpreter.PopProcedureCallFrame(frame, ref savedFrame);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (frame != null)
                                        {
                                            IDisposable disposable = frame as IDisposable;

                                            if (disposable != null)
                                            {
                                                disposable.Dispose();
                                                disposable = null;
                                            }

                                            frame = null;
                                        }
                                    }
                                }
                                else
                                {
                                    if (procedureArguments.Count > 0)
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1}\"",
                                            Parser.Quote(procedureName),
                                            procedureArguments.ToRawString(ToStringFlags.Decorated,
                                                Characters.Space.ToString()));
                                    else
                                        result = String.Format(
                                            "wrong # args: should be \"{0}\"",
                                            Parser.Quote(procedureName));

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = "invalid procedure argument list";
                                code = ReturnCode.Error;
                            }
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
            finally
            {
                ExitLevel();
            }
        }
        #endregion
    }
}
