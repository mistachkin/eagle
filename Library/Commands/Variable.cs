/*
 * Variable.cs --
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
    [ObjectId("8f887079-44e3-405a-a0d3-0b446ce2fa15")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class _Variable : Core
    {
        public _Variable(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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

            if (arguments.Count < 2)
            {
                result = "wrong # args: should be \"variable ?name value...? name ?value?\"";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                ICallFrame localFrame = null;

                if (interpreter.GetVariableFrameViaResolvers(
                        LookupFlags.Default, ref localFrame,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (localFrame == null)
                {
                    result = "local call frame is invalid";
                    return ReturnCode.Error;
                }

                if (!localFrame.IsVariable)
                {
                    result = "local call frame does not support variables";
                    return ReturnCode.Error;
                }

                bool useNamespaces = interpreter.AreNamespacesEnabled();
                INamespace currentNamespace = null;

                if (useNamespaces &&
                    interpreter.GetCurrentNamespaceViaResolvers(
                        null, LookupFlags.Default, ref currentNamespace,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                ICallFrame otherFrame = null;

                if ((currentNamespace != null) &&
                    !interpreter.IsGlobalNamespace(currentNamespace))
                {
                    otherFrame = currentNamespace.VariableFrame;
                }
                else
                {
                    otherFrame = interpreter.CurrentGlobalFrame;
                }

                for (int argumentIndex = 1;
                        argumentIndex < arguments.Count;
                        argumentIndex += 2)
                {
                    string varName = arguments[argumentIndex];

                    VariableFlags flags = VariableFlags.NoElement;

                    if (!useNamespaces)
                        flags |= VariableFlags.GlobalOnly;

                    IVariable otherVariable = null;
                    Result error = null;

                    if (interpreter.GetVariableViaResolversWithSplit(
                            varName, ref flags, ref otherVariable,
                            ref error) != ReturnCode.Ok)
                    {
                        if (FlagOps.HasFlags(
                                flags, VariableFlags.NotFound, true))
                        {
                            error = null;

                            if (interpreter.AddVariable2(
                                    VariableFlags.Undefined | flags,
                                    varName, null, true, ref otherVariable,
                                    ref error) != ReturnCode.Ok)
                            {
                                result = error;
                                return ReturnCode.Error;
                            }
                        }
                        else
                        {
                            //
                            // NOTE: We did not search for the variable, let
                            //       the caller know why.
                            //
                            result = error;
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: Create the variable link between the local frame
                    //       (i.e. a procedure, etc) and the other frame (i.e.
                    //       namespace or global).
                    //
                    if (CallFrameOps.IsLocal(localFrame))
                    {
                        error = null;

                        if (ScriptOps.LinkVariable(
                                interpreter, localFrame, varName, otherFrame,
                                varName, ref error) != ReturnCode.Ok)
                        {
                            result = error;
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: If they provided a value, set it now.
                    //
                    // BUGFIX: This must be done after setting up the link
                    //         and not before; otherwise, the LinkVariable
                    //         method will detect a defined variable with
                    //         the same name in the local call frame and
                    //         refuse to overwrite it (by design).
                    //
                    if ((argumentIndex + 1) < arguments.Count)
                    {
                        error = null;

                        if (interpreter.SetVariableValue2(
                                VariableFlags.None, otherFrame, varName,
                                null, arguments[argumentIndex + 1].Value, null,
                                ref otherVariable, ref error) != ReturnCode.Ok)
                        {
                            result = error;
                            return ReturnCode.Error;
                        }
                    }
                }
            }

            result = String.Empty;
            return ReturnCode.Ok;
        }
        #endregion
    }
}
