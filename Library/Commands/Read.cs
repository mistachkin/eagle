/*
 * Read.cs --
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
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>read</c> command, which reads data
    /// from a channel (for example a file or socket), optionally up to a
    /// specified number of characters, and returns it as a string or wrapped
    /// object.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("8bde05f7-44aa-4d1c-a350-15c02319305a")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Read : Core
    {
        #region Private Constants
        /// <summary>
        /// The error message used when the <c>read</c> command is invoked with
        /// the wrong number of arguments.
        /// </summary>
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"read ?options? channelId ?numChars?\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>read</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Read(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>read</c> command.  It parses any
        /// options, resolves the requested channel, reads the requested number
        /// of characters (or to end-of-file when no count is supplied), and
        /// returns the data either as a string or as a wrapped object.
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
        /// command name; the remaining elements supply any options, the
        /// channel identifier, and an optional character count.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the data read from the channel (as a
        /// string or wrapped object).  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the data read placed
        /// in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the arguments are invalid, the
        /// channel cannot be resolved, or an exception occurs, with details
        /// placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            OptionDictionary options = CommandOptions.GetCommandOptions(
                CommandOptionType.Read);

            int argumentIndex = Index.Invalid;

            if (interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid, false,
                    ref argumentIndex, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if ((argumentIndex == Index.Invalid) ||
                ((argumentIndex + 2) < arguments.Count))
            {
                if ((argumentIndex != Index.Invalid) &&
                    Option.LooksLikeOption(arguments[argumentIndex]))
                {
                    result = OptionDictionary.BadOption(
                        options, arguments[argumentIndex],
                        !interpreter.InternalIsSafe());
                }
                else
                {
                    result = WrongNumArgs;
                }

                return ReturnCode.Error;
            }

            IVariant value = null;
            Encoding encoding = null;

            if (options.IsPresent("-encoding", ref value))
                encoding = (Encoding)value.Value;

            bool newLine = true;

            if (options.IsPresent("-nonewline"))
                newLine = false;

            bool noBlock = false;

            if (options.IsPresent("-noblock"))
                noBlock = true;

            bool useObject = false;

            if (options.IsPresent("-useobject"))
                useObject = true;

            //
            // NOTE: If they do not specify a count we read until
            //       the end-of-file is encountered.
            //
            int count = Count.Invalid;

            if ((argumentIndex + 1) < arguments.Count)
            {
                if (Value.GetInteger2(
                        (IGetValue)arguments[argumentIndex + 1],
                        ValueFlags.AnyInteger,
                        interpreter.InternalCultureInfo,
                        ref count, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            string channelId = arguments[argumentIndex];

            IChannel channel = interpreter.InternalGetChannel(
                channelId, ref result);

            if (channel == null)
                return ReturnCode.Error;

            if (encoding == null)
                encoding = channel.GetEncoding();

            if (!channel.NullEncoding && (encoding == null))
            {
                result = String.Format(
                    "failed to get encoding for input channel {0}",
                    FormatOps.WrapOrNull(channelId));

                return ReturnCode.Error;
            }

            CharList endOfLine = channel.GetInputEndOfLine();

            try
            {
                ReturnCode code;
                ByteList buffer = null;

                if (noBlock)
                {
                    code = channel.ReadBuffer(
                        count, null, false, false, ref buffer,
                        ref result);
                }
                else
                {
                    code = channel.Read(
                        count, null, false, false, ref buffer,
                        ref result);
                }

                if (code != ReturnCode.Ok)
                    return code;

                //
                // BUGFIX: Remove trailing end-of-line character
                //         even when reading the entire stream.
                //
                if (!newLine)
                    channel.RemoveTrailingEndOfLine(buffer, endOfLine);

                if (useObject)
                {
                    Type returnType;
                    ObjectFlags objectFlags;
                    string objectName;
                    string interpName;
                    bool create;
                    bool dispose;
                    bool alias;
                    bool aliasRaw;
                    bool aliasAll;
                    bool aliasReference;
                    bool toString;

                    ObjectOps.ProcessFixupReturnValueOptions(
                        options, null, out returnType, out objectFlags,
                        out objectName, out interpName, out create,
                        out dispose, out alias, out aliasRaw,
                        out aliasAll, out aliasReference, out toString);

                    ObjectOptionType objectOptionType =
                        ObjectOptionType.Read | ObjectOps.GetOptionType(
                            aliasRaw, aliasAll);

                    return MarshalOps.FixupReturnValue(
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo,
                        returnType, objectFlags, options,
                        ObjectOps.GetInvokeOptions(objectOptionType),
                        objectOptionType, objectName, interpName,
                        buffer, create, dispose, alias,
                        aliasReference, toString, ref result);
                }
                else
                {
                    string stringValue = null;

                    code = StringOps.GetString(
                        encoding, ArrayOps.GetArray<byte>(buffer, true),
                        EncodingType.Binary, ref stringValue, ref result);

                    if (code != ReturnCode.Ok)
                        return code;

                    result = stringValue;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                result = e;
                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
