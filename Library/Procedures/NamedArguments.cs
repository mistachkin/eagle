/*
 * NamedArguments.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Procedures
{
    [ObjectId("edfcb0d3-9444-4799-bbe4-5912d970cbd6")]
    public class NamedArguments : Core
    {
        #region Public Constructors
        public NamedArguments(
            IProcedureData procedureData
            )
            : base(procedureData)
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
                            ArgumentDictionary namedArguments = this.NamedArguments;

                            if (namedArguments != null)
                            {
                                int argumentCount = arguments.Count;

                                if (((argumentCount - 1) % 2) == 0)
                                {
                                    {
                                        ICallFrame frame = null;

                                        try
                                        {
                                            bool hasArgs = namedArguments.IsVariadic(true);
                                            int maximumId = namedArguments.GetMaximumId();

                                            if (hasArgs)
                                                maximumId--;

                                            bool[] foundArguments = null;

                                            if (maximumId > 0)
                                                foundArguments = new bool[maximumId];

                                            frame = interpreter.NewProcedureCallFrame(
                                                procedureName, CallFrameFlags.Procedure,
                                                null, this, arguments);

                                            VariableDictionary variables = frame.Variables;
                                            ArgumentList argsArguments = hasArgs ? new ArgumentList() : null;

                                            frame.ProcedureArguments = new ArgumentList(arguments[0]);

                                            for (int argumentIndex = 1; argumentIndex < argumentCount; argumentIndex += 2)
                                            {
                                                string varName = arguments[argumentIndex];

                                                if (!variables.ContainsKey(varName))
                                                {
                                                    ArgumentFlags flags = ArgumentFlags.None;
                                                    object varValue;

                                                    Argument argument = arguments[argumentIndex + 1];
                                                    IAnyPair<int, Argument> anyPair;

                                                    if (namedArguments.TryGetValue(varName, out anyPair))
                                                    {
                                                        //
                                                        // NOTE: The named argument exists and has been specified.
                                                        //
                                                        if ((anyPair != null) && (foundArguments != null))
                                                        {
                                                            int id = anyPair.X;

                                                            if ((id >= 0) && (id < foundArguments.Length))
                                                                foundArguments[id] = true;
                                                        }

                                                        //
                                                        // NOTE: Sync up the argument name for use when debugging
                                                        //       (below) and use the value supplied by the caller.
                                                        //
                                                        varValue = Argument.GetOrCreate(
                                                            interpreter, argument.Flags | flags, varName,
                                                            argument, interpreter.HasNoCacheArgument());
                                                    }
                                                    else if (hasArgs)
                                                    {
                                                        //
                                                        // NOTE: This argument is part of an argument list.
                                                        //
                                                        flags |= ArgumentFlags.ArgumentList;

                                                        //
                                                        // NOTE: Add to the list for the final argument value, which
                                                        //       consists of all the remaining named argument values.
                                                        //
                                                        Argument argsArgument = Argument.GetOrCreate(
                                                            interpreter, argument.Flags | flags,
                                                            String.Format("{0}{1}{2}", varName, Characters.Space,
                                                            argsArguments.Count), argument,
                                                            interpreter.HasNoCacheArgument());

                                                        argsArguments.Add(varName);
                                                        argsArguments.Add(argsArgument);

                                                        //
                                                        // NOTE: Setting the "args" variable is done after this loop.
                                                        //
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: This is an error.  The named argument is not supported
                                                        //       -AND- there was no "args" argument in the definition of
                                                        //       the procedure.
                                                        //
                                                        result = String.Format(
                                                            "procedure \"{0}\" unsupported argument named \"{1}\"",
                                                            procedureName, varName);

                                                        code = ReturnCode.Error;
                                                        break;
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
                                                    frame.ProcedureArguments.Add(varName);

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

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // NOTE: Next, verify that all named arguments that do not
                                                //       have a default value have been specified.
                                                //
                                                if (foundArguments != null)
                                                {
                                                    foreach (KeyValuePair<string, IAnyPair<int, Argument>> pair
                                                            in namedArguments)
                                                    {
                                                        IAnyPair<int, Argument> anyPair = pair.Value;

                                                        if (anyPair == null)
                                                            continue; /* TODO: Error? */

                                                        int id = anyPair.X;

                                                        if ((id < 0) || (id >= foundArguments.Length))
                                                            continue; /* TODO: Error? */

                                                        if (foundArguments[id])
                                                            continue;

                                                        Argument argument = anyPair.Y;

                                                        if (argument == null)
                                                            continue;

                                                        string varName = pair.Key;

                                                        if (argument.HasFlags(ArgumentFlags.HasDefault, true))
                                                        {
                                                            object @default = argument.Default;

                                                            object varValue = (@default != null) ?
                                                                @default : Argument.NoValue;

                                                            code = interpreter.SetVariableValue2(
                                                                VariableFlags.Argument, frame, varName, varValue,
                                                                ref result);

                                                            if (code != ReturnCode.Ok)
                                                                break;

                                                            //
                                                            // BUGFIX: Now, also keep track of this argument in
                                                            //         the procedure arguments list.  Primarily
                                                            //         because we do not want to have to redo
                                                            //         this logic later (i.e. for [scope]).
                                                            //
                                                            frame.ProcedureArguments.Add(varName);

                                                            if (varValue is Argument)
                                                            {
                                                                frame.ProcedureArguments.Add((Argument)varValue);
                                                            }
                                                            else
                                                            {
                                                                frame.ProcedureArguments.Add(Argument.GetOrCreate(
                                                                    interpreter, ArgumentFlags.None, varName,
                                                                    varValue, interpreter.HasNoCacheArgument()));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: This is an error.  A required named argument is
                                                            //       missing.  This means it was not specified -AND-
                                                            //       it has no default value.
                                                            //
                                                            result = String.Format(
                                                                "procedure \"{0}\" missing argument named \"{1}\"",
                                                                procedureName, varName);

                                                            code = ReturnCode.Error;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argsArguments != null)
                                                {
                                                    code = interpreter.SetVariableValue2(
                                                        VariableFlags.Argument, frame,
                                                        TclVars.Core.Arguments,
                                                        argsArguments, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        frame.ProcedureArguments.Add(TclVars.Core.Arguments);
                                                        frame.ProcedureArguments.Add(Argument.GetOrCreate(
                                                            interpreter, ArgumentFlags.ArgumentList,
                                                            TclVars.Core.Arguments, argsArguments,
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
                                                            EngineFlags.None, BreakpointType.BeforeProcedureBody))
                                                    {
                                                        code = interpreter.CheckBreakpoints(
                                                            code, BreakpointType.BeforeProcedureBody, procedureName,
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
                                                                EngineFlags.None, BreakpointType.AfterProcedureBody))
                                                        {
                                                            code = interpreter.CheckBreakpoints(
                                                                code, BreakpointType.AfterProcedureBody, procedureName,
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
                                                                String.Format("{0}    (procedure \"{1}\" line {2})",
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
                                }
                                else
                                {
                                    if (namedArguments.Count > 0)
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1}\"",
                                            Parser.Quote(procedureName),
                                            namedArguments.ToRawString(ToStringFlags.Decorated,
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
