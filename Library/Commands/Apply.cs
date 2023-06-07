/*
 * Apply.cs --
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
    [ObjectId("2bf60b1f-86bd-4271-952a-847b72b613c4")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.SecuritySdk)]
    [ObjectGroup("procedure")]
    internal sealed class Apply : Core
    {
        #region Private Static Methods
        private string NextName(
            Interpreter interpreter,
            INamespace @namespace
            )
        {
            //
            // NOTE: Create and return a per-interpreter unique name in
            //       the specified namespace (which may be global).
            //
            return NamespaceOps.MakeAbsoluteName(
                NamespaceOps.MakeQualifiedName(interpreter, @namespace,
                StringList.MakeList(this.Name, GlobalState.NextId(
                interpreter))));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Apply(
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
                    int argumentCount = arguments.Count;

                    if (argumentCount >= 2)
                    {
                        //
                        // NOTE: lambdaExpr must be a two element list {args body} or a three element
                        //       list {args body namespace}.
                        //
                        StringList lambdaExpr = null;

                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], true, ref lambdaExpr, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((lambdaExpr.Count == 2) || (lambdaExpr.Count == 3))
                            {
                                byte[] hashValue = arguments[1].GetHashValue(ref result);

                                if (hashValue != null)
                                {
                                    INamespace @namespace = null;

                                    if (lambdaExpr.Count == 3)
                                    {
                                        @namespace = NamespaceOps.Lookup(
                                            interpreter, lambdaExpr[2], true, false,
                                            ref result);

                                        if (@namespace == null)
                                            code = ReturnCode.Error;
                                    }

                                    if (code == ReturnCode.Ok)
                                    {
                                        //
                                        // NOTE: Parse the arguments into a list and make sure there are enough
                                        //       supplied to satisfy the request.
                                        //
                                        StringList list = null;

                                        code = ParserOps<string>.SplitList(
                                            interpreter, lambdaExpr[0], 0,
                                            Length.Invalid, true, ref list,
                                            ref result);

                                        if (code == ReturnCode.Ok)
                                        {
                                            StringPairList list2 = new StringPairList();

                                            for (int argumentIndex = 0; argumentIndex < list.Count; argumentIndex++)
                                            {
                                                StringList list3 = null;

                                                code = ParserOps<string>.SplitList(
                                                    interpreter, list[argumentIndex], 0,
                                                    Length.Invalid, true, ref list3,
                                                    ref result);

                                                if (code != ReturnCode.Ok)
                                                    break;

                                                if (list3.Count > 2)
                                                {
                                                    result = String.Format(
                                                        "too many fields in argument specifier \"{0}\"",
                                                        list[argumentIndex]);

                                                    code = ReturnCode.Error;
                                                    break;
                                                }
                                                else if ((list3.Count == 0) || String.IsNullOrEmpty(list3[0]))
                                                {
                                                    result = "argument with no name";
                                                    code = ReturnCode.Error;
                                                    break;
                                                }
                                                else if (!Parser.IsSimpleScalarVariableName(list3[0],
                                                        String.Format(Interpreter.ArgumentNotSimpleError, list3[0]),
                                                        String.Format(Interpreter.ArgumentNotScalarError, list3[0]), ref result))
                                                {
                                                    code = ReturnCode.Error;
                                                    break;
                                                }

                                                string argName = list3[0];
                                                string argDefault = (list3.Count >= 2) ? list3[1] : null;

                                                list2.Add(new StringPair(argName, argDefault));
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // HACK: This name is needed for several error messages, see below.
                                                //
                                                string name = NextName(interpreter, @namespace);

                                                //
                                                // NOTE: We *MUST* have the formal arguments in an actual ArgumentList
                                                //       container now.  The variadic and optional argument semantics
                                                //       depend on it.
                                                //
                                                ArgumentList formalArguments = new ArgumentList(
                                                    list2, ArgumentFlags.NameOnly);

                                                //
                                                // NOTE: Compare lambda argument count with the total outer argument
                                                //       count minus the "apply" and "lambdaExpr" arguments.
                                                //
                                                bool hasArgs = formalArguments.IsVariadic(true);
                                                int totalArgs = hasArgs ? formalArguments.Count - 1 : formalArguments.Count;
                                                int optionalArgs = formalArguments.GetOptionalCount();

                                                if ((((argumentCount - 2) >= (totalArgs - optionalArgs)) &&
                                                     ((argumentCount - 2) <= totalArgs)) ||
                                                    (hasArgs && ((argumentCount - 2) >= (totalArgs - optionalArgs))))
                                                {
                                                    ICallFrame frame = null;

                                                    try
                                                    {
                                                        frame = interpreter.NewProcedureCallFrame(
                                                            name, CallFrameFlags.Procedure | CallFrameFlags.Lambda,
                                                            new ClientData(hashValue), this, arguments);

                                                        StringDictionary alreadySet = new StringDictionary();
                                                        ArgumentList frameProcedureArguments = new ArgumentList();

                                                        frameProcedureArguments.Add(arguments[0]);
                                                        frame.ProcedureArguments = frameProcedureArguments;

                                                        for (int argumentIndex = 0; argumentIndex < formalArguments.Count; argumentIndex++)
                                                        {
                                                            string varName = formalArguments[argumentIndex].Name;

                                                            if (!alreadySet.ContainsKey(varName))
                                                            {
                                                                ArgumentFlags flags = ArgumentFlags.None;
                                                                object varValue;

                                                                if (hasArgs && (argumentIndex == (formalArguments.Count - 1)))
                                                                {
                                                                    //
                                                                    // NOTE: This argument is part of an argument list.
                                                                    //
                                                                    flags |= ArgumentFlags.List;

                                                                    //
                                                                    // NOTE: Build the list for the final formal argument value,
                                                                    //       which consists of all the remaining argument values.
                                                                    //
                                                                    ArgumentList argsArguments = new ArgumentList();

                                                                    for (int argsArgumentIndex = argumentIndex + 2;
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
                                                                    if ((argumentIndex + 2) < argumentCount)
                                                                    {
                                                                        //
                                                                        // NOTE: Sync up the argument name for use when
                                                                        //       debugging (below) and use the value
                                                                        //       supplied by the caller.
                                                                        //
                                                                        varValue = Argument.GetOrCreate(interpreter,
                                                                            arguments[argumentIndex + 2].Flags | flags,
                                                                            varName, arguments[argumentIndex + 2],
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
                                                                        object @default = formalArguments[argumentIndex].Default;
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
                                                                    frameProcedureArguments.Add((Argument)varValue);
                                                                }
                                                                else
                                                                {
                                                                    frameProcedureArguments.Add(Argument.GetOrCreate(
                                                                        interpreter, flags, varName, varValue,
                                                                        interpreter.HasNoCacheArgument()));
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
                                                            interpreter.PushProcedureCallFrame(frame, true, ref savedFrame);

                                                            try
                                                            {
#if DEBUGGER && DEBUGGER_EXECUTE
                                                                if (DebuggerOps.CanHitBreakpoints(interpreter,
                                                                        EngineFlags.None, BreakpointType.BeforeLambdaBody))
                                                                {
                                                                    code = interpreter.CheckBreakpoints(
                                                                        code, BreakpointType.BeforeLambdaBody, this.Name,
                                                                        null, null, this, null, clientData, arguments,
                                                                        ref result);
                                                                }
#endif

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    interpreter.ReturnCode = ReturnCode.Ok;

                                                                    code = interpreter.EvaluateScript(
                                                                        lambdaExpr[1], (IScriptLocation)arguments[1], ref result);

#if DEBUGGER && DEBUGGER_EXECUTE
                                                                    if (DebuggerOps.CanHitBreakpoints(interpreter,
                                                                            EngineFlags.None, BreakpointType.AfterLambdaBody))
                                                                    {
                                                                        code = interpreter.CheckBreakpoints(
                                                                            code, BreakpointType.AfterLambdaBody, this.Name,
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
                                                                                Environment.NewLine, FormatOps.Ellipsis(arguments[1]),
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
                                                    result = String.Format(
                                                        "wrong # args: should be \"apply lambdaExpr {0}\"", /* SKIP */
                                                        formalArguments.ToRawString(ToStringFlags.Decorated,
                                                            Characters.Space.ToString()));

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result =  String.Format(
                                    "can't interpret \"{0}\" as a lambda expression",
                                    arguments[1]);

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"apply lambdaExpr ?arg1 arg2 ...?\"";
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
