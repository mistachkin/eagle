/*
 * Encoding.cs --
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
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>encoding</c> command, which converts
    /// data between character encodings and the system encoding, retrieves the
    /// available encoding names, and reports the system encoding.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("428e30c3-2e24-4e9a-8f13-887d8dab6756")]
    /*
     * NOTE: We have no [encoding dirs] or [encoding system] that allows
     *       the system encoding to be changed, so this command is "safe".
     */
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class _Encoding : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>encoding</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _Encoding(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        /// <summary>
        /// The set of sub-commands supported by the <c>encoding</c> command,
        /// namely <c>convertfrom</c>, <c>convertto</c>, <c>getstring</c>,
        /// <c>names</c>, and <c>system</c>.
        /// </summary>
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] { 
            "convertfrom", "convertto", "getstring", "names", "system"
        });

        /// <summary>
        /// Gets the dictionary of sub-commands supported by the
        /// <c>encoding</c> command.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>encoding</c> command.  It dispatches to
        /// one of the supported sub-commands (<c>convertfrom</c>,
        /// <c>convertto</c>, <c>getstring</c>, <c>names</c>, or <c>system</c>)
        /// to convert data between encodings, decode a byte-array object into a
        /// string, enumerate the available encoding names, or report the
        /// system encoding.
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
        /// command name; element one is the sub-command name; the remaining
        /// elements are the arguments for that sub-command.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value produced by the selected
        /// sub-command (the converted data, decoded string, list of encoding
        /// names, or system encoding name).  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the sub-command is unknown, the interpreter is null,
        /// or the argument list is null, with details placed in
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
                                case "convertfrom":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            //
                                            // NOTE: (from Tcl encoding.n): Convert data to Unicode from the 
                                            //       specified encoding. The characters in data are treated 
                                            //       as binary data where the lower 8-bits of each character
                                            //       is taken as a single byte. The resulting sequence of 
                                            //       bytes is treated as a string in the specified encoding. 
                                            //       If encoding is not specified, the current system encoding
                                            //       is used.
                                            //
                                            int argumentIndex = 2;
                                            Encoding encoding = null;

                                            if (arguments.Count == 4)
                                            {
                                                code = interpreter.GetEncoding(
                                                    arguments[argumentIndex++], LookupFlags.Default,
                                                    ref encoding, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                string stringValue = null;

                                                code = StringOps.ConvertString(
                                                    null, encoding, EncodingType.Binary, EncodingType.System,
                                                    arguments[argumentIndex], ref stringValue, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = stringValue;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"encoding convertfrom ?encoding? data\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "convertto":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            //
                                            // NOTE: (from Tcl encoding.n): Convert string from Unicode to the
                                            //       specified encoding. The result is a sequence of bytes that
                                            //       represents the converted string. Each byte is stored in the
                                            //       lower 8-bits of a Unicode character. If encoding is not
                                            //       specified, the current system encoding is used.
                                            //
                                            int argumentIndex = 2;
                                            Encoding encoding = null;

                                            if (arguments.Count == 4)
                                            {
                                                code = interpreter.GetEncoding(
                                                    arguments[argumentIndex++], LookupFlags.Default,
                                                    ref encoding, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                string stringValue = null;

                                                code = StringOps.ConvertString(
                                                    encoding, null, EncodingType.System, EncodingType.Binary,
                                                    arguments[argumentIndex], ref stringValue, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = stringValue;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"encoding convertto ?encoding? data\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "getstring":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            IObject @object = null;

                                            code = interpreter.GetObject(
                                                arguments[2], LookupFlags.Default,
                                                ref @object, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Encoding encoding = null;

                                                if (arguments.Count == 4)
                                                {
                                                    code = interpreter.GetEncoding(
                                                        arguments[3], LookupFlags.Default,
                                                        ref encoding, ref result);
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Ok;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (@object.Value is byte[])
                                                    {
                                                        string stringValue = null;

                                                        code = StringOps.GetString(
                                                            encoding, (byte[])@object.Value, EncodingType.System,
                                                            ref stringValue, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = stringValue;
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "object \"{0}\" is not a byte array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"encoding getstring object ?encoding?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "names":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            bool system = true; /* COMPAT: Tcl. */

                                            if (arguments.Count >= 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref system,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                EncodingDictionary encodings = null;

                                                if (system)
                                                    StringOps.GetSystemEncodings(ref encodings);

                                                interpreter.GetEncodings(ref encodings);

                                                string pattern = null;

                                                if (arguments.Count == 4)
                                                    pattern = arguments[3];

                                                result = encodings.ToString(pattern, false);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"encoding names ?system? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "system":
                                    {
                                        //
                                        // NOTE: The system encoding in Eagle is always Unicode and cannot
                                        //       be changed.
                                        //
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                result = "not implemented";
                                                code = ReturnCode.Error;
                                            }
                                            else
                                            {
                                                result = StringOps.SystemEncodingWebName;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"encoding system ?encoding?\"";
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
                        result = "wrong # args: should be \"encoding option ?arg ...?\"";
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
