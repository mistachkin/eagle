/*
 * Button.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* 
//
// Please do not use this code, it is a proof-of-concept only.  It is not production ready.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* 
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Forms;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("dfee4f39-39f8-4711-9027-a5081c060eff")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("managedEnvironment")]
#if INTERNALS_VISIBLE_TO
    internal sealed class _Button : Core
#else
    internal sealed class _Button : Default
#endif
    {
        public _Button(
            ICommandData commandData
            )
            : base(commandData)
        {
            this.Flags |= Utility.GetCommandFlags(GetType().BaseType) |
                Utility.GetCommandFlags(this);
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
                    if (arguments.Count == 2)
                    {
                        IObject @object = null;

                        code = interpreter.GetObject(
                            Toplevel.CollectionName, LookupFlags.Default,
                            ref @object, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            ToplevelDictionary toplevels = @object.Value as ToplevelDictionary;

                            if (toplevels != null)
                            {
                                IAnyPair<Thread, Toplevel> anyPair = null;

                                if (toplevels.TryGetValue(arguments[1], out anyPair))
                                {
                                    if (anyPair != null)
                                    {
                                        Toplevel toplevel = anyPair.Y;

                                        if (toplevel != null)
                                        {
                                            try
                                            {
                                                string name = ".fixme";

                                                if (toplevel.InvokeRequired)
                                                    toplevel.BeginInvoke(
                                                        new Toplevel.AddButtonDelegate(toplevel.AddButton),
                                                        new object[] { name, "test", 0, 0, new EventHandler(button_Click) });
                                                else
                                                    toplevel.AddButton(name, "test", 0, 0, new EventHandler(button_Click));

                                                result = name;
                                            }
                                            catch (Exception e)
                                            {
                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "toplevel \"{0}\" window is invalid",
                                                arguments[1]);

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "toplevel \"{0}\" pair is invalid",
                                            arguments[1]);

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = String.Format(
                                        "toplevel \"{0}\" not found",
                                        arguments[1]);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "object \"{0}\" is not a toplevel collection", 
                                    Toplevel.CollectionName);

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"button toplevel\"";
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private void button_Click(
            object sender,
            EventArgs e
            )
        {
            throw new ScriptException("The method or operation is not implemented.");
        }
        #endregion
    }
}
