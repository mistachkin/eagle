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
    /// <summary>
    /// This class implements the Eagle <c>global</c> command, which links one
    /// or more variables in the current procedure call frame to variables of
    /// the same name in the global (or specified namespace) call frame.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("1cd4e351-10d3-4e53-bcfa-6d7e09e41184")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Global : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>global</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Global(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>global</c> command.  For each named
        /// variable supplied, it creates a link in the current call frame to
        /// the variable of the same name in the global call frame (or, when
        /// namespaces are enabled and a qualified name is given, the variable
        /// frame of the resolved namespace).  When invoked at global scope it
        /// is a no-op.
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
        /// command name; the remaining elements are the names of the variables
        /// to link into the current call frame.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if all variables were linked
        /// successfully (or the command was a no-op at global scope);
        /// otherwise, <see cref="ReturnCode.Error" /> when the wrong number of
        /// arguments is supplied, the interpreter is null, the argument list is
        /// null, or a variable could not be resolved or linked, with details
        /// placed in <paramref name="result" />.
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
                        bool useNamespaces = interpreter.InternalAreNamespacesEnabled();

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
