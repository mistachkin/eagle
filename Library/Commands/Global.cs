/*
 * Global.cs --
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
    [ObjectId("1cd4e351-10d3-4e53-bcfa-6d7e09e41184")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Global : Core
    {
        public Global(
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
                result = "wrong # args: should be \"global varName ?varName ...?\"";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                ICallFrame localFrame = null;

                if (interpreter.GetVariableFrameViaResolvers(
                        LookupFlags.Default, ref localFrame,
                        ref result) == ReturnCode.Ok)
                {
                    if ((localFrame != null) &&
                        !interpreter.IsGlobalCallFrame(localFrame))
                    {
                        bool useNamespaces = interpreter.AreNamespacesEnabled();

                        for (int argumentIndex = 1;
                                argumentIndex < arguments.Count;
                                argumentIndex++)
                        {
                            string varName = arguments[argumentIndex];
                            ICallFrame otherFrame = interpreter.CurrentGlobalFrame;

                            if (useNamespaces)
                            {
                                string qualifiers = null;
                                string tail = null;
                                NamespaceFlags namespaceFlags = NamespaceFlags.None;

                                if (NamespaceOps.SplitName(
                                        varName, ref qualifiers, ref tail,
                                        ref namespaceFlags,
                                        ref result) == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: For linking between call frames, use
                                    //       the simple variable name only.
                                    //
                                    varName = tail;
                                }
                                else
                                {
                                    return ReturnCode.Error;
                                }

                                if (FlagOps.HasFlags(namespaceFlags,
                                        NamespaceFlags.Qualified, true))
                                {
                                    INamespace @namespace = NamespaceOps.Lookup(
                                        interpreter, qualifiers, false, false,
                                        ref result);

                                    if (@namespace != null)
                                        otherFrame = @namespace.VariableFrame;
                                    else
                                        return ReturnCode.Error;
                                }
                            }

                            if (ScriptOps.LinkVariable(
                                    interpreter, localFrame, varName, otherFrame,
                                    varName, ref result) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }
                        }

                        result = String.Empty;
                    }
                    else
                    {
                        // already in global scope... this is a NOP.
                        result = String.Empty;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }
        }
        #endregion
    }
}
