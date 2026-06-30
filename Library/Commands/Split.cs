/*
 * Split.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>split</c> command, which breaks a
    /// string into a list of elements using a set of separator characters (or,
    /// optionally, a single separator string).  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("870fdad9-4698-4b0a-863d-8b9e0fe699ca")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Split : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>split</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Split(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>split</c> command.  It splits the input
        /// string into a list, treating each character of the supplied
        /// separators (or the entire separators argument as a single string
        /// when the <c>-string</c> option is present) as a delimiter.  When no
        /// separators are supplied, the default whitespace characters (tab,
        /// line feed, carriage return, and space) are used; when the
        /// separators are empty, the string is split into its individual
        /// characters.
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
        /// command name; element one is the string to split; an optional
        /// element two supplies the separator characters; and optional further
        /// elements supply command options (for example <c>-string</c>).  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the resulting list of split elements
        /// (or an empty string when the input string is empty).  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the split list placed
        /// in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, an option cannot be parsed, the interpreter is null, or
        /// the argument list is null, with details placed in
        /// <paramref name="result" />.
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
                    if ((arguments.Count >= 2) && (arguments.Count <= 4))
                    {
                        string value = arguments[1];
                        string separators;

                        if (arguments.Count >= 3)
                        {
                            separators = arguments[2];
                        }
                        else
                        {
                            separators = String.Format(
                                "{0}{1}{2}{3}", Characters.HorizontalTab,
                                Characters.LineFeed, Characters.CarriageReturn,
                                Characters.Space);
                        }

                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Split);

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 3)
                        {
                            code = interpreter.GetOptions(
                                options, arguments, 0, 3, Index.Invalid,
                                true, ref argumentIndex, ref result);
                        }
                        else
                        {
                            code = ReturnCode.Ok;
                        }

                        if (code == ReturnCode.Ok)
                        {
                            if (argumentIndex == Index.Invalid)
                            {
                                bool @string = false;

                                if (options.IsPresent("-string"))
                                    @string = true;

                                if (!String.IsNullOrEmpty(value))
                                {
                                    StringList list;

                                    if (@string)
                                    {
                                        if (!String.IsNullOrEmpty(separators))
                                        {
                                            list = new StringList(value.Split(
                                                new string[] { separators },
                                                StringSplitOptions.None));
                                        }
                                        else
                                        {
                                            list = new StringList(
                                                value.ToCharArray());
                                        }
                                    }
                                    else
                                    {
                                        if (!String.IsNullOrEmpty(separators))
                                        {
                                            list = new StringList(value.Split(
                                                separators.ToCharArray(),
                                                StringSplitOptions.None));
                                        }
                                        else
                                        {
                                            list = new StringList(
                                                value.ToCharArray());
                                        }
                                    }

                                    result = list;
                                }
                                else
                                {
                                    result = String.Empty; /* COMPAT: Tcl. */
                                }
                            }
                            else
                            {
                                result = "wrong # args: should be \"split string ?splitChars? ?options?\"";
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"split string ?splitChars? ?options?\"";
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
