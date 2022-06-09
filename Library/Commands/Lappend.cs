/*
 * Lappend.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("1c359f9f-7a48-41e9-8897-f7a0464e8be0")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lappend : Core
    {
        public Lappend(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

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
                    if (arguments.Count >= 2)
                    {
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            string name = arguments[1];

                            //
                            // BUGFIX: Do not use DoesVariableExist here, it does not currently
                            //         honor variable traces (primarily because there is currently
                            //         no such thing as a "does this exist" trace operation).
                            //
                            // BUGBUG: Potentially, if a trace callback denies us the ability to 
                            //         read the current value, we will try to set a new value instead 
                            //         of appending to the existing one; however, there is currently 
                            //         no nice way to solve this problem.  Trace callbacks should 
                            //         deny setting a new value if they deny reading the existing 
                            //         value.
                            //
                            StringList list = null;

                            code = interpreter.GetListVariableValue(
                                VariableFlags.None, name, true, false, false, false, ref list,
                                ref result);

                            if (code != ReturnCode.Ok)
                                return code;

                            //
                            // NOTE: Add all the list elements specified by the caller, in order.
                            //
                            for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                list.Add(arguments[argumentIndex]);

                            //
                            // NOTE: If the list was not parsed, that means we are using the existing
                            //       cached list representation.  Set the list value directly (i.e.
                            //       not the string representation of it).
                            //
                            code = interpreter.SetListVariableValue(
                                VariableFlags.None, name, list, null, ref result);

                            //
                            // NOTE: Return the resulting list value, with all new elements appended.
                            //
                            if (code == ReturnCode.Ok)
                                result = list;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lappend varName ?value ...?\"";
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
