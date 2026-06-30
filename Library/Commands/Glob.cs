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
    /// <summary>
    /// This class implements the Eagle <c>glob</c> command, which returns the
    /// list of file names that match one or more patterns, optionally
    /// restricted by directory, path prefix, and file type.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("92dc78d3-6dce-4f68-9459-9d3510b1ce7d")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("fileSystem")]
    internal sealed class Glob : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>glob</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Glob(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>glob</c> command.  It parses the
        /// supplied options and patterns, then collects the matching file
        /// names from the file system, honoring the <c>-directory</c>,
        /// <c>-path</c>, <c>-types</c>, <c>-join</c>, <c>-tails</c>,
        /// <c>-nocomplain</c>, and <c>-noerror</c> options.
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
        /// command name; it is followed by any options and then one or more
        /// patterns to match.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the list of matching file names (or an
        /// empty string when none matched and <c>-noerror</c> was specified).
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, an option is invalid, a conflicting combination of
        /// options is used, or no matching files are found while error
        /// reporting is enabled, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;

            if (arguments.Count >= 2)
            {
                OptionDictionary options =
                    CommandOptions.GetCommandOptions(
                        CommandOptionType.Glob);

                int argumentIndex = Index.Invalid;

                code = interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid, false,
                    ref argumentIndex, ref result);

                if (code == ReturnCode.Ok)
                {
                    if (argumentIndex != Index.Invalid)
                    {
                        IVariant value = null;
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
