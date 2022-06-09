/*
 * Glob.cs --
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
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("92dc78d3-6dce-4f68-9459-9d3510b1ce7d")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("fileSystem")]
    internal sealed class Glob : Core
    {
        public Glob(
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
            ReturnCode code;

            if (arguments.Count >= 2)
            {
                OptionDictionary options = new OptionDictionary(
                    new IOption[] {
                    new Option(null, OptionFlags.MustHaveValue,
                        Index.Invalid, Index.Invalid, "-path", null),
                    new Option(null, OptionFlags.MustHaveValue,
                        Index.Invalid, Index.Invalid, "-directory", null),
                    new Option(null, OptionFlags.MustHaveListValue,
                        Index.Invalid, Index.Invalid, "-types", null),
                    new Option(null, OptionFlags.None, Index.Invalid,
                        Index.Invalid, "-join", null),
                    new Option(null, OptionFlags.None, Index.Invalid,
                        Index.Invalid, "-tails", null),
                    new Option(null, OptionFlags.None, Index.Invalid,
                        Index.Invalid, "-nocomplain", null),
                    new Option(null, OptionFlags.None, Index.Invalid,
                        Index.Invalid, "-noerror", null),
                    Option.CreateEndOfOptions()
                });

                int argumentIndex = Index.Invalid;

                code = interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid, false,
                    ref argumentIndex, ref result);

                if (code == ReturnCode.Ok)
                {
                    if (argumentIndex != Index.Invalid)
                    {
                        Variant value = null;
                        string pathPrefix = null;

                        if (options.IsPresent("-path", ref value))
                            pathPrefix = value.ToString();

                        string directory = null;

                        if (options.IsPresent("-directory", ref value))
                            directory = value.ToString();

                        IntDictionary types = new IntDictionary();

                        if (options.IsPresent("-types", ref value))
                            types = new IntDictionary((StringList)value.Value);

                        bool join = false;

                        if (options.IsPresent("-join"))
                            join = true;

                        bool tailOnly = false;

                        if (options.IsPresent("-tails"))
                            tailOnly = true;

                        bool errorOnNotFound = true;

                        if (options.IsPresent("-nocomplain"))
                            errorOnNotFound = false;

                        bool noError = false;

                        if (options.IsPresent("-noerror"))
                            noError = true;

                        if ((pathPrefix == null) || (directory == null))
                        {
                            if (!tailOnly ||
                                (pathPrefix != null) || (directory != null))
                            {
                                StringList patterns = new StringList(
                                    arguments, argumentIndex);

                                bool isWindows =
                                    PlatformOps.IsWindowsOperatingSystem();

                                StringList fileNames = FileOps.GlobFiles(
                                    interpreter, patterns, types, pathPrefix,
                                    directory, join, tailOnly, isWindows,
                                    isWindows, errorOnNotFound, ref result);

                                if (fileNames != null)
                                {
                                    result = fileNames;
                                    code = ReturnCode.Ok;
                                }
                                else if (!noError)
                                {
                                    code = ReturnCode.Error;
                                }
                                else
                                {
                                    result = String.Empty;
                                    code = ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                result = "\"-tails\" must be used with either \"-directory\" or \"-path\"";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "\"-path\" cannot be used with \"-directory\"";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"glob ?options? pattern ?pattern ...?\"";
                        code = ReturnCode.Error;
                    }
                }
            }
            else
            {
                result = "wrong # args: should be \"glob ?options? pattern ?pattern ...?\"";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
