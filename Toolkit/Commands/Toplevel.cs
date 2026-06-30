/*
 * Toplevel.cs --
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
using Eagle._Containers.Public;
using Eagle._Forms;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the <c>toplevel</c> command, which creates a new
    /// top-level window on its own thread.  It is a proof-of-concept only and
    /// is not production ready.
    /// </summary>
    [ObjectId("c7aff90a-1599-4694-9034-667717b1bdcf")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("managedEnvironment")]
#if INTERNALS_VISIBLE_TO
    internal sealed class _Toplevel : Core
#else
    internal sealed class _Toplevel : Default
#endif
    {
        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="commandData">
        /// An object instance containing the data necessary to create this
        /// command.
        /// </param>
        public _Toplevel(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the command, creating a new top-level window on
        /// its own thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the command execution.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this command, if any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to the command.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the name of the newly created top-level
        /// window; upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                    if (arguments.Count == 1)
                    {
                        try
                        {
                            string name = Utility.FormatId(
                                Characters.Period.ToString(), this.Name,
                                interpreter.NextId());

                            IMutableAnyPair<Thread, Toplevel> anyPair =
                                new MutableAnyPair<Thread, Toplevel>(true);

                            anyPair.X = new Thread(delegate()
                            {
                                anyPair.Y = new Toplevel(interpreter, name);
                                anyPair.Y.ShowDialog();
                            });

                            anyPair.X.Start();

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
                        result = "wrong # args: should be \"toplevel\"";
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
