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
    [ObjectId("295be8e8-f85f-4e5a-a937-cece2660e903")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("fileSystem")]
    internal sealed class Cd : Core
    {
        public Cd(
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
