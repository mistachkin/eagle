/*
 * Hash.cs --
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
using System.Security.Cryptography;
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
    [ObjectId("66a2a9aa-1024-4199-b6d9-097c2662acd7")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("string")]
    internal sealed class _Hash : Core
    {
        #region Public Constructors
        public _Hash(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "keyed", "list", "mac", "normal"
        });

        ///////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
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
                result = "wrong # args: should be \"hash option ?arg ...?\"";
                return ReturnCode.Error;
            }

            ReturnCode code;
            string subCommand = arguments[1];
            bool tried = false;

            code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, this, clientData, arguments, true,
                null, ref subCommand, ref tried, ref result);

            if ((code != ReturnCode.Ok) || tried)
                return code;

            //
            // NOTE: These algorithms are known to be supported by the full
            //       .NET Framework (Desktop).  Some may not be available on
            //       Mono and/or .NET Core.
            //
            //         HMAC: HMACMD5, HMACRIPEMD160, HMACSHA1, HMACSHA256,
            //               HMACSHA384, HMACSHA512
            //
            //        Keyed: MACTripleDES
            //
            //       Normal: MD5, RIPEMD160, SHA, SHA1, SHA256, SHA384, SHA512
            //
            switch (subCommand)
            {
                case "keyed":
                    {
                        if (arguments.Count >= 4)
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] {
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-object",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-raw",
                                    null),
                                new Option(null, OptionFlags.Unsafe,
                                    Index.Invalid, Index.Invalid, "-filename",
                                    null), /* COMPAT: Tcllib. */
                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                    Index.Invalid, Index.Invalid, "-encoding",
                                    null),
                                Option.CreateEndOfOptions()
                            });

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(
                                options, arguments, 0, 2, Index.Invalid, false,
                                ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    ((argumentIndex + 2) <= arguments.Count) &&
                                    ((argumentIndex + 3) >= arguments.Count))
                                {
                                    bool asObject = false;

                                    if (options.IsPresent("-object"))
                                        asObject = true;

                                    bool raw = false;

                                    if (options.IsPresent("-raw"))
                                        raw = true;

                                    bool isFileName = false;

                                    if (options.IsPresent("-filename"))
                                        isFileName = true;

                                    IVariant value = null;
                                    Encoding encoding = null;

                                    if (options.IsPresent("-encoding", ref value))
                                        encoding = (Encoding)value.Value;

                                    byte[] key = null;

                                    if ((argumentIndex + 3) == arguments.Count)
                                    {
                                        code = StringOps.GetBytes(
                                            encoding, arguments[argumentIndex + 2],
                                            EncodingType.Binary, true, ref key,
                                            ref result);
                                    }

                                    if (code == ReturnCode.Ok)
                                    {
                                        string stringValue = arguments[argumentIndex + 1];
                                        EncodingType? encodingType = null;

                                        if (asObject)
                                        {
                                            if (isFileName)
                                            {
                                                result = "cannot use -object with -filename";
                                                code = ReturnCode.Error;
                                            }
                                            else if (encoding != null)
                                            {
                                                result = "cannot use -object with -encoding";
                                                code = ReturnCode.Error;
                                            }
                                            else
                                            {
                                                IObject @object = null;

                                                code = interpreter.GetObject(
                                                    stringValue, LookupFlags.Default,
                                                    ref @object, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    byte[] bytes = (@object != null) ?
                                                        @object.Value as byte[] : null;

                                                    if (bytes != null)
                                                    {
                                                        //
                                                        // HACK: This is necessary to fully work with
                                                        //       internal calls to StringOps.GetBytes
                                                        //       method(s).
                                                        //
                                                        stringValue = Convert.ToBase64String(bytes,
                                                            Base64FormattingOptions.InsertLineBreaks);

                                                        encodingType = EncodingType.Null;
                                                    }
                                                    else
                                                    {
                                                        result = "object must be byte array";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            try
                                            {
                                                byte[] hashValue = HashOps.ComputeKeyed(
                                                    interpreter, arguments[argumentIndex],
                                                    key, stringValue, encoding, encodingType,
                                                    isFileName, ref result);

                                                if (hashValue != null)
                                                {
                                                    if (raw)
                                                        result = new ByteList(hashValue);
                                                    else
                                                        result = FormatOps.Hash(hashValue);
                                                }
                                                else
                                                {
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
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                            this.Name, subCommand);
                                    }

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                this.Name, subCommand);

                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "list":
                    {
                        if ((arguments.Count == 2) || (arguments.Count == 3))
                        {
                            string type = null;

                            if (arguments.Count == 3)
                                type = arguments[2];

                            switch (type)
                            {
                                case null:
                                case "all":
                                    {
                                        StringList list = null;

                                        HashOps.AddAlgorithmNames(true, true, true, true, ref list);

                                        result = list;
                                        break;
                                    }
                                case "default":
                                    {
                                        StringList list = null;

                                        HashOps.AddAlgorithmNames(true, false, false, false, ref list);

                                        result = list;
                                        break;
                                    }
                                case "keyed": /* SKIP */
                                    {
                                        StringList list = null;

                                        HashOps.AddAlgorithmNames(false, false, true, false, ref list);

                                        result = list;
                                        break;
                                    }
                                case "mac": /* SKIP */
                                    {
                                        StringList list = null;

                                        HashOps.AddAlgorithmNames(false, true, false, false, ref list);

                                        result = list;
                                        break;
                                    }
                                case "normal": /* SKIP */
                                    {
                                        StringList list = null;

                                        HashOps.AddAlgorithmNames(false, false, false, true, ref list);

                                        result = list;
                                        break;
                                    }
                                default:
                                    {
                                        result = "unknown algorithm list, must be: all, default, keyed, mac, or normal";
                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?type?\"",
                                this.Name, subCommand);

                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "mac":
                    {
                        if (arguments.Count >= 4)
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] {
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-object",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-raw",
                                    null),
                                new Option(null, OptionFlags.Unsafe,
                                    Index.Invalid, Index.Invalid, "-filename",
                                    null), /* COMPAT: Tcllib. */
                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                    Index.Invalid, Index.Invalid, "-encoding",
                                    null),
                                Option.CreateEndOfOptions()
                            });

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(
                                options, arguments, 0, 2, Index.Invalid, false,
                                ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    ((argumentIndex + 2) <= arguments.Count) &&
                                    ((argumentIndex + 3) >= arguments.Count))
                                {
                                    bool asObject = false;

                                    if (options.IsPresent("-object"))
                                        asObject = true;

                                    bool raw = false;

                                    if (options.IsPresent("-raw"))
                                        raw = true;

                                    bool isFileName = false;

                                    if (options.IsPresent("-filename"))
                                        isFileName = true;

                                    IVariant value = null;
                                    Encoding encoding = null;

                                    if (options.IsPresent("-encoding", ref value))
                                        encoding = (Encoding)value.Value;

                                    byte[] key = null;

                                    if ((argumentIndex + 3) == arguments.Count)
                                    {
                                        code = StringOps.GetBytes(
                                            encoding, arguments[argumentIndex + 2],
                                            EncodingType.Binary, true, ref key,
                                            ref result);
                                    }

                                    if (code == ReturnCode.Ok)
                                    {
                                        string stringValue = arguments[argumentIndex + 1];
                                        EncodingType? encodingType = null;

                                        if (asObject)
                                        {
                                            if (isFileName)
                                            {
                                                result = "cannot use -object with -filename";
                                                code = ReturnCode.Error;
                                            }
                                            else if (encoding != null)
                                            {
                                                result = "cannot use -object with -encoding";
                                                code = ReturnCode.Error;
                                            }
                                            else
                                            {
                                                IObject @object = null;

                                                code = interpreter.GetObject(
                                                    stringValue, LookupFlags.Default,
                                                    ref @object, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    byte[] bytes = (@object != null) ?
                                                        @object.Value as byte[] : null;

                                                    if (bytes != null)
                                                    {
                                                        //
                                                        // HACK: This is necessary to fully work with
                                                        //       internal calls to StringOps.GetBytes
                                                        //       method(s).
                                                        //
                                                        stringValue = Convert.ToBase64String(bytes,
                                                            Base64FormattingOptions.InsertLineBreaks);

                                                        encodingType = EncodingType.Null;
                                                    }
                                                    else
                                                    {
                                                        result = "object must be byte array";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            try
                                            {
                                                byte[] hashValue = HashOps.ComputeHMAC(
                                                    interpreter, arguments[argumentIndex],
                                                    key, stringValue, encoding, encodingType,
                                                    isFileName, ref result);

                                                if (hashValue != null)
                                                {
                                                    if (raw)
                                                        result = new ByteList(hashValue);
                                                    else
                                                        result = FormatOps.Hash(hashValue);
                                                }
                                                else
                                                {
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
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                            this.Name, subCommand);
                                    }

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                this.Name, subCommand);

                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "normal":
                    {
                        if (arguments.Count >= 4)
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] {
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-object",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-raw",
                                    null),
                                new Option(null, OptionFlags.Unsafe,
                                    Index.Invalid, Index.Invalid, "-filename",
                                    null), /* COMPAT: Tcllib. */
                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                    Index.Invalid, Index.Invalid, "-encoding",
                                    null),
                                Option.CreateEndOfOptions()
                            });

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(
                                options, arguments, 0, 2, Index.Invalid, false,
                                ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    ((argumentIndex + 2) == arguments.Count))
                                {
                                    bool asObject = false;

                                    if (options.IsPresent("-object"))
                                        asObject = true;

                                    bool raw = false;

                                    if (options.IsPresent("-raw"))
                                        raw = true;

                                    bool isFileName = false;

                                    if (options.IsPresent("-filename"))
                                        isFileName = true;

                                    IVariant value = null;
                                    Encoding encoding = null;

                                    if (options.IsPresent("-encoding", ref value))
                                        encoding = (Encoding)value.Value;

                                    if (code == ReturnCode.Ok) /* REDUNDANT */
                                    {
                                        string stringValue = arguments[argumentIndex + 1];
                                        EncodingType? encodingType = null;

                                        if (asObject)
                                        {
                                            if (isFileName)
                                            {
                                                result = "cannot use -object with -filename";
                                                code = ReturnCode.Error;
                                            }
                                            else if (encoding != null)
                                            {
                                                result = "cannot use -object with -encoding";
                                                code = ReturnCode.Error;
                                            }
                                            else
                                            {
                                                IObject @object = null;

                                                code = interpreter.GetObject(
                                                    stringValue, LookupFlags.Default,
                                                    ref @object, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    byte[] bytes = (@object != null) ?
                                                        @object.Value as byte[] : null;

                                                    if (bytes != null)
                                                    {
                                                        //
                                                        // HACK: This is necessary to fully work with
                                                        //       internal calls to StringOps.GetBytes
                                                        //       method(s).
                                                        //
                                                        stringValue = Convert.ToBase64String(bytes,
                                                            Base64FormattingOptions.InsertLineBreaks);

                                                        encodingType = EncodingType.Null;
                                                    }
                                                    else
                                                    {
                                                        result = "object must be byte array";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            try
                                            {
                                                byte[] hashValue = HashOps.Compute(
                                                    interpreter, arguments[argumentIndex],
                                                    stringValue, encoding, encodingType,
                                                    isFileName, ref result);

                                                if (hashValue != null)
                                                {
                                                    if (raw)
                                                        result = new ByteList(hashValue);
                                                    else
                                                        result = FormatOps.Hash(hashValue);
                                                }
                                                else
                                                {
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
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1} ?options? algorithm string\"",
                                            this.Name, subCommand);
                                    }

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?options? algorithm string\"",
                                this.Name, subCommand);

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

            return code;
        }
        #endregion
    }
}
