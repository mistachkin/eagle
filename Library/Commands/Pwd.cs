/*
 * Pwd.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.IO;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>pwd</c> command, which returns the
    /// absolute path name of the current working directory.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("f73d9296-ae5b-4faf-8ebb-12305f43a4f5")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("fileSystem")]
    internal sealed class Pwd : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>pwd</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Pwd(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>pwd</c> command.  It accepts no
        /// arguments and returns the absolute path name of the current
        /// working directory, normalized to use Unix-style path separators.
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
        /// command name; no further arguments are accepted.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the absolute path name of the current
        /// working directory.  Upon failure, this contains an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the current working
        /// directory placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, or the argument list is
        /// null, with details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == 1)
                    {
                        result = PathOps.GetUnixPath(
                            PathOps.GetCurrentDirectory());
                    }
                    else
                    {
                        result = "wrong # args: should be \"pwd\"";
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
