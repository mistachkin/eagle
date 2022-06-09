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
    [ObjectId("8bde05f7-44aa-4d1c-a350-15c02319305a")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Read : Core
    {
        #region Private Constants
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"read ?options? channelId ?numChars?\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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

            OptionDictionary options = ObjectOps.GetReadOptions();

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

            IChannel channel = interpreter.GetChannel(
                channelId, ref result);

            if (channel == null)
                return ReturnCode.Error;

            Encoding encoding = channel.GetEncoding();

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

                    OptionDictionary readOptions =
                        ObjectOps.GetInvokeOptions(objectOptionType);

                    return MarshalOps.FixupReturnValue(
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo, returnType,
                        objectFlags, readOptions, objectOptionType,
                        objectName, interpName, buffer, create,
                        dispose, alias, aliasReference, toString,
                        ref result);
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
