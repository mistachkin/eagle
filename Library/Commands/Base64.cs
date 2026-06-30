/*
 * Base64.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
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
    /// This class implements the Eagle <c>base64</c> command, which encodes a
    /// string to its Base64 representation and decodes a Base64 string back to
    /// its original value via the <c>encode</c> and <c>decode</c>
    /// sub-commands.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("b2cf12bb-e35a-4039-9736-3da91e590777")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("string")]
    internal sealed class Base64 : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>base64</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Base64(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        /// <summary>
        /// The set of sub-commands supported by this command, namely
        /// <c>decode</c> and <c>encode</c>.
        /// </summary>
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] { 
            "decode", "encode"
        });

        /// <summary>
        /// Gets the dictionary of sub-commands supported by this command,
        /// used by the engine to dispatch and validate ensemble invocations.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>base64</c> command.  It dispatches to
        /// the <c>encode</c> or <c>decode</c> sub-command, optionally honoring
        /// an <c>-encoding</c> option, to convert between a string and its
        /// Base64 representation.
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
        /// command name; element one is the sub-command name (<c>encode</c> or
        /// <c>decode</c>); the remaining elements supply any options and the
        /// string to convert.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the encoded or decoded string.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the converted value
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, an option is invalid, the conversion fails, the
        /// interpreter is null, or the argument list is null, with details
        /// placed in <paramref name="result" />.
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
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            null, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "decode":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options =
                                                CommandOptions.GetCommandOptions(
                                                    CommandOptionType.Base64_Decode);

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        try
                                                        {
                                                            string stringValue = null;

                                                            code = StringOps.GetString(encoding,
                                                                Convert.FromBase64String(arguments[argumentIndex]),
                                                                EncodingType.Binary, ref stringValue, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = stringValue;
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
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"base64 decode ?options? string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"base64 decode ?options? string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "encode":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options =
                                                CommandOptions.GetCommandOptions(
                                                    CommandOptionType.Base64_Encode);

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        try
                                                        {
                                                            byte[] bytes = null;

                                                            code = StringOps.GetBytes(
                                                                encoding, arguments[argumentIndex],
                                                                EncodingType.Binary, true, ref bytes,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                result = Convert.ToBase64String(bytes,
                                                                    Base64FormattingOptions.InsertLineBreaks);
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
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"base64 encode ?options? string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"base64 encode ?options? string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"base64 option ?arg ...?\"";
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
