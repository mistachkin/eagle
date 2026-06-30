/*
 * Cd.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>cd</c> command, which changes the
    /// current working directory of the process to a specified directory, or
    /// to the home or profile directory of the user when no directory is
    /// given.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("295be8e8-f85f-4e5a-a937-cece2660e903")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("fileSystem")]
    internal sealed class Cd : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>cd</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Cd(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>cd</c> command.  It accepts an optional
        /// directory name and changes the current working directory of the
        /// process to that directory; when no directory name is supplied, the
        /// home or profile directory of the user is used instead.
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
        /// command name; an optional element one supplies the name of the
        /// directory to change to.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the current working directory is
        /// changed successfully; otherwise, <see cref="ReturnCode.Error" />
        /// when the wrong number of arguments is supplied, the interpreter is
        /// null, the argument list is null, the directory cannot be resolved,
        /// or changing the directory raises an exception, with details placed
        /// in <paramref name="result" />.
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
                    if ((arguments.Count == 1) || (arguments.Count == 2))
                    {
                        string directory;

                        if (arguments.Count == 2)
                        {
                            directory = arguments[1];
                        }
                        else
                        {
                            directory = PathOps.GetUserDirectory(true);

                            if (directory == null)
                            {
                                result = "failed to get home or profile directory for user";
                                code = ReturnCode.Error;
                            }
                        }

                        if (code == ReturnCode.Ok)
                        {
                            try
                            {
                                directory = PathOps.ResolveFullPath(interpreter, directory);

                                if (!String.IsNullOrEmpty(directory))
                                {
                                    Directory.SetCurrentDirectory(directory);
                                    result = String.Empty;
                                }
                                else
                                {
                                    result = "unrecognized path";
                                    code = ReturnCode.Error;
                                }
                            }
                            catch (Exception e)
                            {
                                Engine.SetExceptionErrorCode(interpreter, e);

                                result = e;
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"cd ?dirName?\"";
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
