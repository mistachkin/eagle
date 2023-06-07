/*
 * Napply.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("86d11eee-7c32-4b07-95fb-11536876ed67")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("procedure")]
    internal sealed class Napply : Core
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

        public Napply(
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
                                                    list2, ArgumentFlags.NameOnly | ArgumentFlags.WithName);

                                                ArgumentDictionary namedArguments = new ArgumentDictionary();

                                                foreach (Argument argument in formalArguments)
                                                {
                                                    if (argument == null)
                                                        continue;

                                                    string argumentName = argument.Name;

                                                    if (argumentName == null)
                                                        continue;

                                                    if (namedArguments.ContainsKey(argumentName))
                                                    {
                                                        result = String.Format(
                                                            "procedure \"{0}\" duplicate argument named \"{1}\"",
                                                            name, argumentName);

                                                        code = ReturnCode.Error;
                                                        break;
                                                    }

                                                    namedArguments.Add(argumentName, argument);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (namedArguments.IsGoodCount(argumentCount - 2, true))
                                                    {
                                                        ICallFrame frame = null;

                                                        try
                                                        {
                                                            bool hasArgs = namedArguments.IsVariadic(null, true);
                                                            int maximumId = namedArguments.GetMaximumId();

                                                            if (hasArgs)
                                                                maximumId--;

                                                            bool[] foundArguments = null;

                                                            if (maximumId > 0)
                                                                foundArguments = new bool[maximumId];

                                                            frame = interpreter.NewProcedureCallFrame(
                                                                name, CallFrameFlags.Procedure | CallFrameFlags.Lambda,
                                                                new ClientData(hashValue), this, arguments);

                                                            StringDictionary alreadySet = new StringDictionary();
                                                            ArgumentList argsArguments = hasArgs ? new ArgumentList() : null;
                                                            ArgumentList frameProcedureArguments = new ArgumentList();

                                                            frameProcedureArguments.Add(arguments[0]);
                                                            frame.ProcedureArguments = frameProcedureArguments;

                                                            int argumentIndex = 2;

                                                            for (; argumentIndex < argumentCount; argumentIndex += 2)
                                                            {
                                                                string varName = arguments[argumentIndex];

                                                                if ((argumentIndex + 1) >= argumentCount)
                                                                {
                                                                    if (!hasArgs)
                                                                    {
                                                                        result = String.Format(
                                                                            "procedure \"{0}\" missing value for argument named \"{1}\"",
                                                                            name, varName);

                                                                        code = ReturnCode.Error;
                                                                    }

                                                                    break;
                                                                }

                                                                if (hasArgs && namedArguments.IsVariadicName(varName))
                                                                {
                                                                    Argument argument = arguments[argumentIndex + 1];
                                                                    StringList list4 = null;

                                                                    code = ListOps.GetOrCopyOrSplitList(
                                                                        interpreter, argument, true, ref list4,
                                                                        ref result);

                                                                    if (code != ReturnCode.Ok)
                                                                        break;

                                                                    for (int index = 0; index < list4.Count; index++)
                                                                    {
                                                                        Argument argsArgument = Argument.GetOrCreate(
                                                                            interpreter, argument.Flags |
                                                                                ArgumentFlags.Named |
                                                                                ArgumentFlags.List,
                                                                            String.Format("{0}{1}{2}{3}{4}",
                                                                                namedArguments.GetVariadicName(),
                                                                                Characters.Space, argumentIndex,
                                                                                Characters.Space, index), list4[index],
                                                                            interpreter.HasNoCacheArgument());

                                                                        argsArguments.Add(argsArgument);
                                                                    }
                                                                }

                                                                IAnyPair<int, Argument> anyPair;

                                                                if (!namedArguments.TryGetValue(varName, out anyPair))
                                                                {
                                                                    if (!hasArgs)
                                                                    {
                                                                        //
                                                                        // NOTE: This is an error.  The named argument is not
                                                                        //       supported -AND- there was no "args" argument
                                                                        //       in the definition of the procedure.
                                                                        //
                                                                        result = String.Format(
                                                                            "procedure \"{0}\" unsupported argument named \"{1}\"",
                                                                            name, varName);

                                                                        code = ReturnCode.Error;
                                                                    }

                                                                    break;
                                                                }

                                                                if (!alreadySet.ContainsKey(varName))
                                                                {
                                                                    //
                                                                    // HACK: Set the found flag on this named argument.
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
                                                                    object varValue;
                                                                    Argument argument = arguments[argumentIndex + 1];

                                                                    varValue = Argument.GetOrCreate(
                                                                        interpreter, argument.Flags | ArgumentFlags.Named,
                                                                        varName, argument, interpreter.HasNoCacheArgument());

                                                                    code = interpreter.SetVariableValue2(
                                                                        VariableFlags.Argument, frame, varName, varValue,
                                                                        ref result);

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
                                                                            interpreter, ArgumentFlags.Named |
                                                                            ArgumentFlags.FrameOnly, varName, varValue,
                                                                            interpreter.HasNoCacheArgument()));
                                                                    }

                                                                    alreadySet.Add(varName, null);
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
                                                                            if (varValue is Argument)
                                                                            {
                                                                                frameProcedureArguments.Add((Argument)varValue);
                                                                            }
                                                                            else
                                                                            {
                                                                                frameProcedureArguments.Add(Argument.GetOrCreate(
                                                                                    interpreter, ArgumentFlags.Named |
                                                                                    ArgumentFlags.FrameOnly, varName, varValue,
                                                                                    interpreter.HasNoCacheArgument()));
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
                                                                                name, varName);

                                                                            code = ReturnCode.Error;
                                                                            break;
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (hasArgs)
                                                                {
                                                                    //
                                                                    // NOTE: Add to the list for the final argument value,
                                                                    //       which consists of all the remaining argument
                                                                    //       values.
                                                                    //
                                                                    for (; argumentIndex < argumentCount; argumentIndex++)
                                                                    {
                                                                        Argument argument = arguments[argumentIndex];

                                                                        Argument argsArgument = Argument.GetOrCreate(
                                                                            interpreter, argument.Flags |
                                                                                ArgumentFlags.Named |
                                                                                ArgumentFlags.List,
                                                                            String.Format("{0}{1}{2}",
                                                                                namedArguments.GetVariadicName(),
                                                                                Characters.Space,
                                                                                argumentIndex), argument,
                                                                            interpreter.HasNoCacheArgument());

                                                                        argsArguments.Add(argsArgument);
                                                                    }
                                                                }
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (argsArguments != null)
                                                                {
                                                                    code = interpreter.SetVariableValue2(
                                                                        VariableFlags.Argument, frame,
                                                                        namedArguments.GetVariadicName(),
                                                                        argsArguments, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        frameProcedureArguments.Add(Argument.GetOrCreate(
                                                                            interpreter, ArgumentFlags.Named |
                                                                            ArgumentFlags.FrameOnly | ArgumentFlags.List,
                                                                            namedArguments.GetVariadicName(), argsArguments,
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
                                                            "wrong # args: should be \"napply lambdaExpr {0}\"", /* SKIP */
                                                            formalArguments.ToRawString(ToStringFlags.Decorated,
                                                                Characters.Space.ToString()));

                                                        code = ReturnCode.Error;
                                                    }
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
                        result = "wrong # args: should be \"napply lambdaExpr ?arg1 arg2 ...?\"";
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
