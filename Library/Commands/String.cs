/*
 * String.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;

#if NETWORK
using System.Net;
#endif

#if NET_40
using System.Numerics;
#endif

using System.Text;
using System.Text.RegularExpressions;

#if XML
using System.Xml;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using IndexRangeList = System.Collections.Generic.List<
    Eagle._Components.Public.Pair<ulong>>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the <c>string</c> command, which provides a
    /// collection of sub-commands for inspecting and manipulating strings,
    /// such as comparison, searching, formatting, indexing, character class
    /// testing, case conversion, trimming, and related operations.  It is an
    /// ensemble whose sub-commands cover these string operations.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("eb32c33f-5454-4b8f-894a-af725b0df057")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize | CommandFlags.SecuritySdk)]
    [ObjectGroup("string")]
    internal sealed class _String : Core
    {
        #region Private Constants
        /// <summary>
        /// The error message used to report an incorrect number of arguments
        /// for the <c>string is</c> sub-command.
        /// </summary>
        private static readonly string IsWrongNumArgs =
            "wrong # args: should be \"string is ?not? class ?-strict? ?-failindex varName? string\"";

        /// <summary>
        /// The format string used to construct an error message reporting an
        /// incorrect number of arguments for the <c>string is</c> sub-command,
        /// parameterized by the command name, sub-command name, and class name.
        /// </summary>
        private static readonly string IsWrongNumArgsFormat =
            "wrong # args: should be \"{0} {1} ?not? {2} ?-strict? ?-failindex varName? string\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The mapping of character class names to their per-character test
        /// callbacks, used by the <c>string is</c> sub-command to validate each
        /// character of a string against a character class.
        /// </summary>
        private Dictionary<string, CharIsCallback> charIsCallbacks = null;
        /// <summary>
        /// The collection of character class names recognized by the
        /// <c>string is</c> sub-command.
        /// </summary>
        private EnsembleDictionary isSubCommands = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs one-time initialization of the character class
        /// callbacks and the associated character class names used by the
        /// <c>string is</c> sub-command.  This data is not a per-interpreter
        /// datum and never changes once it has been initialized.
        /// </summary>
        /// <param name="callbacks">
        /// Upon success, this is populated with the mapping of character class
        /// names to their per-character test callbacks, unless it was already
        /// populated.
        /// </param>
        /// <param name="subCommands">
        /// Upon success, this is populated with the collection of character
        /// class names, unless it was already populated.
        /// </param>
        private static void Initialize(
            ref Dictionary<string, CharIsCallback> callbacks, /* in, out */
            ref EnsembleDictionary subCommands                /* in, out */
            )
        {
            //
            // NOTE: One-time initialization, this is not a per-interpreter
            //       datum and it never changes.
            //
            if (callbacks == null)
            {
                callbacks = new Dictionary<string, CharIsCallback>();

                callbacks.Add("annotation", null);   // *SPECIAL CASE*, whole string only
                callbacks.Add("array", null);        // *SPECIAL CASE*, whole string only
                callbacks.Add("base64", null);       // *SPECIAL CASE*, whole string only
                callbacks.Add("boolean", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("byte", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("cidr", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("command", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("component", null);    // *SPECIAL CASE*, whole string only
                callbacks.Add("datetime", null);     // *SPECIAL CASE*, whole string only
                callbacks.Add("decimal", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("dict", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("directory", null);    // *SPECIAL CASE*, whole string only
                callbacks.Add("double", null);       // *SPECIAL CASE*, whole string only
                callbacks.Add("element", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("encoding", null);     // *SPECIAL CASE*, whole string only

#if NET_40
                callbacks.Add("entier", null);       // *SPECIAL CASE*, whole string only
#endif

                callbacks.Add("false", null);        // *SPECIAL CASE*, whole string only
                callbacks.Add("file", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("guid", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("identifier", null);   // *SPECIAL CASE*, whole string only
                callbacks.Add("idxranges", null);    // *SPECIAL CASE*, whole string only
                callbacks.Add("inetaddr", null);     // *SPECIAL CASE*, whole string only
                callbacks.Add("integer", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("interpreter", null);  // *SPECIAL CASE*, whole string only
                callbacks.Add("list", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("none", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("not", null);          // *SPECIAL CASE*, modifier only
                callbacks.Add("number", null);       // *SPECIAL CASE*, whole string only
                callbacks.Add("numeric", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("object", null);       // *SPECIAL CASE*, whole string only
                callbacks.Add("path", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("plugin", null);       // *SPECIAL CASE*, whole string only
                callbacks.Add("real", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("ruleset", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("scalar", null);       // *SPECIAL CASE*, whole string only
                callbacks.Add("single", null);       // *SPECIAL CASE*, whole string only
                callbacks.Add("timespan", null);     // *SPECIAL CASE*, whole string only
                callbacks.Add("true", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("type", null);         // *SPECIAL CASE*, whole string only
                callbacks.Add("uri", null);          // *SPECIAL CASE*, whole string only
                callbacks.Add("value", null);        // *SPECIAL CASE*, whole string only
                callbacks.Add("variant", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("version", null);      // *SPECIAL CASE*, whole string only
                callbacks.Add("versionrange", null); // *SPECIAL CASE*, whole string only
                callbacks.Add("xml", null);          // *SPECIAL CASE*, whole string only
                callbacks.Add("wideinteger", null);  // *SPECIAL CASE*, whole string only

                //
                // NOTE: Per-character callbacks for character class checking
                //       (used in a loop to check each character of the string
                //       against).
                //
                callbacks.Add("alnum", Char.IsLetterOrDigit);
                callbacks.Add("alpha", Char.IsLetter);
                callbacks.Add("ascii", StringOps.CharIsAscii);
                callbacks.Add("asciialnum", StringOps.CharIsAsciiAlphaOrDigit);
                callbacks.Add("asciialpha", StringOps.CharIsAsciiAlpha);
                callbacks.Add("asciidigit", StringOps.CharIsAsciiDigit);
                callbacks.Add("control", Char.IsControl);
                callbacks.Add("digit", Char.IsDigit);
                callbacks.Add("graph", StringOps.CharIsGraph);
                callbacks.Add("hexadecimal", StringOps.CharIsAsciiHexadecimal);
                callbacks.Add("lower", Char.IsLower);
                callbacks.Add("print", StringOps.CharIsPrint);
                callbacks.Add("punct", Char.IsPunctuation);
                callbacks.Add("reserved", StringOps.CharIsReserved);
                callbacks.Add("space", Char.IsWhiteSpace);
                callbacks.Add("upper", Char.IsUpper);
                callbacks.Add("wordchar", StringOps.CharIsWord);
                callbacks.Add("xdigit", Parser.IsHexadecimalDigit);
            }

            if (subCommands == null)
                subCommands = new EnsembleDictionary(callbacks.Keys);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the <c>string</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _String(
            ICommandData commandData
            )
            : base(commandData)
        {
            Initialize(ref charIsCallbacks, ref isSubCommands);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// The collection of sub-command names supported by this ensemble
        /// command, used to dispatch each invocation to the appropriate
        /// sub-command handler.
        /// </summary>
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "bytelength", "cat", "character", "classes", "compare", "ends",
            "equal", "first", "format", "index", "is", "last",
            "length", "map", "match", "ordinal", "range", "repeat",
            "replace", "reverse", "starts", "tolower", "totitle",
            "toupper", "trim", "trimleft", "trimright", "wordend",
            "wordstart"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the collection of sub-command names supported by this ensemble
        /// command.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>string</c> command.  It dispatches to
        /// the requested ensemble sub-command (for example <c>compare</c>,
        /// <c>first</c>, <c>format</c>, <c>index</c>, <c>is</c>,
        /// <c>length</c>, <c>map</c>, <c>match</c>, <c>range</c>,
        /// <c>replace</c>, or <c>trim</c>) in order to inspect or manipulate
        /// strings, honoring the recognized options for each sub-command.
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
        /// command name and element one is the sub-command name, followed by
        /// any sub-command-specific arguments.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the dispatched
        /// sub-command.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, or
        /// the dispatched sub-command fails, with details placed in
        /// <paramref name="result" />.
        /// </returns>
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
                    if (arguments.Count >= 2)
                    {
                        if (code == ReturnCode.Ok)
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
                                    case "bytelength":
                                        {
                                            if ((arguments.Count == 3) || (arguments.Count == 4))
                                            {
                                                if (arguments.Count == 4)
                                                {
                                                    Encoding encoding = null;

                                                    code = interpreter.GetEncoding(
                                                        arguments[3], LookupFlags.Default, ref encoding,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        int byteCount = 0;

                                                        code = StringOps.AddByteCount(
                                                            encoding, arguments[2], EncodingType.System,
                                                            ref byteCount, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = byteCount;
                                                    }
                                                }
                                                else
                                                {
                                                    result = (arguments[2].Length * sizeof(char));
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string bytelength string ?encoding?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "cat":
                                        {
                                            if (arguments.Count >= 2)
                                            {
                                                int capacity = 0;

                                                for (int argumentIndex = 2;
                                                        argumentIndex < arguments.Count;
                                                        argumentIndex++)
                                                {
                                                    capacity += arguments[argumentIndex].Length;
                                                }

                                                StringBuilder builder = StringBuilderFactory.Create(capacity);

                                                for (int argumentIndex = 2;
                                                        argumentIndex < arguments.Count;
                                                        argumentIndex++)
                                                {
                                                    builder.Append(arguments[argumentIndex]);
                                                }

                                                result = StringBuilderCache.GetStringAndRelease(ref builder);
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string cat ?arg ...?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "character":
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                int intValue = 0;

                                                code = Value.GetInteger2(
                                                    (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                                    interpreter.InternalCultureInfo, ref intValue, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = ConversionOps.ToChar(intValue);
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string character integer\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "classes":
                                        {
                                            if (arguments.Count == 2)
                                            {
                                                if (isSubCommands != null)
                                                {
                                                    result = new StringList(isSubCommands.Keys);
                                                }
                                                else
                                                {
                                                    result = "string classes unavailable";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string classes\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "compare":
                                    case "equal":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_Equal);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) == arguments.Count))
                                                    {
                                                        IVariant value = null;
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        CultureInfo cultureInfo = null;

                                                        if (options.IsPresent("-culture", ref value))
                                                            cultureInfo = (CultureInfo)value.Value;
#endif

                                                        bool noCase = false;

                                                        if (options.IsPresent("-nocase"))
                                                            noCase = true;

                                                        StringComparison comparisonType =
                                                            SharedStringOps.GetBinaryComparisonType(noCase);

                                                        if (options.IsPresent("-comparison", ref value))
                                                            comparisonType = (StringComparison)value.Value;

#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        CompareOptions compareOptions = noCase ?
                                                            CompareOptions.IgnoreCase : CompareOptions.None;

                                                        if (options.IsPresent("-options"))
                                                            compareOptions = (CompareOptions)value.Value;
#endif

                                                        int length = Length.Invalid;

                                                        if (options.IsPresent("-length", ref value))
                                                            length = (int)value.Value;

                                                        int maximumLength = (length < 0) ?
                                                            Math.Max(arguments[argumentIndex].Length,
                                                                arguments[argumentIndex + 1].Length) : 0;

                                                        int compare;

#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        if (cultureInfo != null)
                                                        {
                                                            if (length >= 0)
                                                            {
                                                                compare = String.Compare(
                                                                    arguments[argumentIndex], 0,
                                                                    arguments[argumentIndex + 1], 0,
                                                                    length, cultureInfo,
                                                                    compareOptions);
                                                            }
                                                            else
                                                            {
                                                                compare = String.Compare(
                                                                    arguments[argumentIndex], 0,
                                                                    arguments[argumentIndex + 1], 0,
                                                                    maximumLength, cultureInfo,
                                                                    compareOptions);
                                                            }
                                                        }
                                                        else
#endif
                                                        {
                                                            if (length >= 0)
                                                            {
                                                                compare = SharedStringOps.Compare(
                                                                    arguments[argumentIndex], 0,
                                                                    arguments[argumentIndex + 1], 0,
                                                                    length, comparisonType);
                                                            }
                                                            else
                                                            {
                                                                compare = SharedStringOps.Compare(
                                                                    arguments[argumentIndex], 0,
                                                                    arguments[argumentIndex + 1], 0,
                                                                    maximumLength, comparisonType);
                                                            }
                                                        }

                                                        if (SharedStringOps.SystemEquals(subCommand, "equal"))
                                                        {
                                                            result = (compare == 0);
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // BUGFIX: Apparently, the String.Compare
                                                            //         method can return values other
                                                            //         than -1, 0, and 1 (COMPAT: Tcl).
                                                            //
                                                            if (compare < 0)
                                                                result = -1;
                                                            else if (compare > 0)
                                                                result = 1;
                                                            else
                                                                result = compare;
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
                                                                "wrong # args: should be \"{0} {1} ?options? string1 string2\"",
                                                                this.Name, subCommand);
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "wrong # args: should be \"{0} {1} ?options? string1 string2\"",
                                                    this.Name, subCommand);

                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "ends":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_Ends);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) == arguments.Count))
                                                    {
                                                        IVariant value = null;
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        CultureInfo cultureInfo = null;

                                                        if (options.IsPresent("-culture", ref value))
                                                            cultureInfo = (CultureInfo)value.Value;
#endif

                                                        bool noCase = false;

                                                        if (options.IsPresent("-nocase"))
                                                            noCase = true;

                                                        StringComparison comparisonType =
                                                            SharedStringOps.GetBinaryComparisonType(noCase);

                                                        if (options.IsPresent("-comparison", ref value))
                                                            comparisonType = (StringComparison)value.Value;

#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        if (cultureInfo != null)
                                                        {
                                                            result = SharedStringOps.EndsWith(
                                                                arguments[argumentIndex + 1],
                                                                arguments[argumentIndex],
                                                                cultureInfo, noCase);
                                                        }
                                                        else
#endif
                                                        {
                                                            result = SharedStringOps.EndsWith(
                                                                arguments[argumentIndex + 1],
                                                                arguments[argumentIndex],
                                                                comparisonType);
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
                                                                "wrong # args: should be \"{0} {1} ?options? suffix string\"",
                                                                this.Name, subCommand);
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "wrong # args: should be \"{0} {1} ?options? suffix string\"",
                                                    this.Name, subCommand);

                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "first":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_First);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) <= arguments.Count) &&
                                                        ((argumentIndex + 3) >= arguments.Count))
                                                    {
                                                        bool noCase = false;

                                                        if (options.IsPresent("-nocase"))
                                                            noCase = true;

                                                        IVariant value = null;

                                                        StringComparison comparisonType =
                                                            SharedStringOps.GetBinaryComparisonType(noCase);

                                                        if (options.IsPresent("-comparison", ref value))
                                                            comparisonType = (StringComparison)value.Value;

                                                        Argument needle = arguments[argumentIndex];
                                                        Argument haystack = arguments[argumentIndex + 1];

                                                        int needleLength = needle.Length;
                                                        int haystackLength = haystack.Length;

                                                        if ((needleLength > 0) && (haystackLength > 0))
                                                        {
                                                            if ((argumentIndex + 3) == arguments.Count)
                                                            {
                                                                int startIndex = Index.Invalid;

                                                                code = Value.GetIndex(
                                                                    arguments[argumentIndex + 2],
                                                                    haystackLength, ValueFlags.AnyIndex,
                                                                    interpreter.InternalCultureInfo, ref startIndex,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (startIndex < 0)
                                                                    {
                                                                        result = haystack.IndexOf(
                                                                            needle, comparisonType);
                                                                    }
                                                                    else if (startIndex < haystackLength)
                                                                    {
                                                                        result = haystack.IndexOf(
                                                                            needle, startIndex, comparisonType);
                                                                    }
                                                                    else
                                                                    {
                                                                        result = Index.Invalid;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = haystack.IndexOf(needle, comparisonType);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = Index.Invalid; /* COMPAT: Tcl. */
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
                                                            result = "wrong # args: should be \"string first ?options? needleString haystackString ?startIndex?\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string first ?options? needleString haystackString ?startIndex?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "format":
                                        {
                                            if (arguments.Count >= 3)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_Format, interpreter);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        IVariant value = null;
                                                        string valueFormat = null;

                                                        if (options.IsPresent("-valueformat", ref value))
                                                            valueFormat = value.ToString();

                                                        DateTimeKind dateTimeKind = interpreter.DateTimeKind;

                                                        if (options.IsPresent("-datetimekind", ref value))
                                                            dateTimeKind = (DateTimeKind)value.Value;

                                                        DateTimeStyles dateTimeStyles = ObjectOps.GetDefaultDateTimeStyles();

                                                        if (options.IsPresent("-datetimestyles", ref value))
                                                            dateTimeStyles = (DateTimeStyles)value.Value;

                                                        CultureInfo cultureInfo = null;

                                                        if (options.IsPresent("-culture", ref value))
                                                            cultureInfo = (CultureInfo)value.Value;

                                                        bool verbatim = false;

                                                        if (options.IsPresent("-verbatim"))
                                                            verbatim = true;

                                                        ValueFlags valueFlags = ValueFlags.AnyNonCharacter;

                                                        if (options.IsPresent("-valueflags", ref value))
                                                            valueFlags = (ValueFlags)value.Value;

                                                        ObjectList args = new ObjectList();

                                                        //
                                                        // HACK: *MONO* On Mono, attempt to avoid using the String.Format
                                                        //       method overload that takes three (or more) arguments and
                                                        //       takes an IFormatProvider object as the first argument as
                                                        //       it always throws a System.MissingMethodException when
                                                        //       invoked via reflection.  However, if the culture option
                                                        //       was used, force the issue.
                                                        //
                                                        if ((cultureInfo != null) || !CommonOps.Runtime.IsMono())
                                                            args.Add(Value.GetNumberFormatProvider(cultureInfo));

                                                        args.Add(arguments[argumentIndex].String);

                                                        for (argumentIndex++; argumentIndex < arguments.Count; argumentIndex++)
                                                        {
                                                            object @object = null;

                                                            if (!verbatim &&
                                                                ((Value.GetObject(
                                                                    interpreter, arguments[argumentIndex],
                                                                    ref @object) == ReturnCode.Ok) ||
                                                                (Value.GetValue(
                                                                    arguments[argumentIndex], valueFormat,
                                                                    valueFlags, dateTimeKind, dateTimeStyles,
                                                                    cultureInfo, ref @object) == ReturnCode.Ok)))
                                                            {
                                                                args.Add(@object);
                                                            }
                                                            else
                                                            {
                                                                args.Add(arguments[argumentIndex].String);
                                                            }
                                                        }

                                                        result = (string)typeof(string).InvokeMember(
                                                            subCommand, ObjectOps.GetBindingFlags(
                                                            MetaBindingFlags.LooseMethod, true),
                                                            null, null, args.ToArray());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"string format format ?arg ...?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string format format ?arg ...?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "index":
                                        {
                                            if (arguments.Count == 4)
                                            {
                                                string value = arguments[2];
                                                int index = Index.Invalid;

                                                code = Value.GetIndex(
                                                    arguments[3], value.Length, ValueFlags.AnyIndex,
                                                    interpreter.InternalCultureInfo, ref index, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((index >= 0) && (index < value.Length))
                                                        result = value[index];
                                                    else
                                                        result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string index string charIndex\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "is":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                int optionIndex = 3;
                                                bool not = false;

                                            retry:

                                                string subSubCommand = arguments[optionIndex - 1];

                                                code = ScriptOps.SubCommandFromEnsemble(
                                                    interpreter, isSubCommands, "class", true,
                                                    false, ref subSubCommand, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (SharedStringOps.SystemEquals(subSubCommand, "not"))
                                                    {
                                                        if (arguments.Count > (optionIndex + 1))
                                                        {
                                                            optionIndex++;
                                                            not = !not;
                                                            goto retry;
                                                        }
                                                        else
                                                        {
                                                            result = IsWrongNumArgs;
                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        OptionDictionary options = CommandOptions.GetCommandOptions(
                                                            CommandOptionType.String_Is, interpreter, null, null,
                                                            null, null, null, not);

                                                        int argumentIndex = Index.Invalid;

                                                        code = interpreter.GetOptions(options, arguments, 0, optionIndex, Index.Invalid, false, ref argumentIndex, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if ((argumentIndex != Index.Invalid) &&
                                                                ((argumentIndex + 1) == arguments.Count))
                                                            {
                                                                bool strict = false;

                                                                if (options.IsPresent("-strict"))
                                                                    strict = true;

                                                                bool noComplain = false;

                                                                if (options.IsPresent("-nocomplain"))
                                                                    noComplain = true;

                                                                IVariant value = null;
                                                                bool treatAsVarName = false;

                                                                if (options.IsPresent("-via", ref value))
                                                                    treatAsVarName = (bool)value.Value;

                                                                string goodVarName = null;

                                                                if (options.IsPresent("-good", ref value))
                                                                    goodVarName = value.ToString();

                                                                string badVarName = null;

                                                                if (options.IsPresent("-bad", ref value))
                                                                    badVarName = value.ToString();

                                                                string failIndexVarName = null;

                                                                if (options.IsPresent("-failindex", ref value))
                                                                    failIndexVarName = value.ToString();

                                                                bool any = false;

                                                                if (options.IsPresent("-any", ref value))
                                                                    any = (bool)value.Value;

                                                                if (options.IsPresent("-not", ref value))
                                                                    not = (bool)value.Value;

                                                                bool noStrict = false;

                                                                if (SharedStringOps.SystemEquals(subSubCommand, "dict") ||
                                                                    SharedStringOps.SystemEquals(subSubCommand, "list"))
                                                                {
                                                                    noStrict = true;
                                                                }

                                                                int? count = null;

                                                                if (options.IsPresent("-count", ref value))
                                                                    count = (int)value.Value;

                                                                bool valid = !not;
                                                                int failIndex = Index.Invalid;

                                                                Argument argument = arguments[argumentIndex];
                                                                string @string = argument;

                                                                if (treatAsVarName)
                                                                {
                                                                    Result varValue = null;

                                                                    code = interpreter.GetVariableValue(
                                                                        @string, ref varValue, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        argument = varValue;
                                                                        @string = argument;
                                                                    }
                                                                    else if (noComplain)
                                                                    {
                                                                        result = not;
                                                                        code = ReturnCode.Ok;

                                                                        goto done;
                                                                    }
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (!String.IsNullOrEmpty(@string))
                                                                    {
                                                                        switch (subSubCommand)
                                                                        {
                                                                            case "annotation":
                                                                                {
                                                                                    if (Value.IsAnnotation(@string))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "array":
                                                                                {
                                                                                    VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                                                    IVariable variable = null;

                                                                                    if (interpreter.GetVariableViaResolversWithSplit(
                                                                                            @string, ref flags, ref variable) == ReturnCode.Ok)
                                                                                    {
                                                                                        if (EntityOps.IsLink(variable))
                                                                                            variable = EntityOps.FollowLinks(variable, flags);

                                                                                        if ((variable == null) ||
                                                                                            EntityOps.IsUndefined(variable) ||
                                                                                            !EntityOps.IsArray(variable))
                                                                                        {
                                                                                            valid = not;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            valid = !not;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "base64":
                                                                                {
                                                                                    if (StringOps.IsBase64(@string))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "boolean":
                                                                            case "false":
                                                                            case "true":
                                                                                {
                                                                                    bool boolValue = false;

                                                                                    if ((Value.GetBoolean5(@string,
                                                                                            ValueFlags.AnyBoolean | ValueFlags.NoCase,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref boolValue) == ReturnCode.Ok) &&
                                                                                        (SharedStringOps.SystemEquals(
                                                                                            subSubCommand, "boolean") ||
                                                                                        (SharedStringOps.SystemEquals(
                                                                                            subSubCommand, "true") && boolValue) ||
                                                                                        (SharedStringOps.SystemEquals(
                                                                                            subSubCommand, "false") && !boolValue)))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "byte":
                                                                                {
                                                                                    byte byteValue = 0; /* NOT USED */

                                                                                    if (Value.GetByte2(
                                                                                            @string, ValueFlags.AnyByte,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref byteValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "cidr":
                                                                                {
#if NETWORK
                                                                                    if (SocketOps.IsValidCIDR(
                                                                                            @string, IpFlags.Default))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
#endif
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "command":
                                                                                {
                                                                                    if (interpreter.InternalDoesIExecuteExistViaResolvers(
                                                                                            @string) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "component":
                                                                                {
                                                                                    if (PathOps.CheckForValid(
                                                                                            null, @string, true, false, true, false))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "datetime":
                                                                                {
                                                                                    DateTime dateTimeValue = DateTime.MinValue; /* NOT USED */

                                                                                    if (Value.GetDateTime(
                                                                                            @string, interpreter.DateTimeFormat,
                                                                                            interpreter.DateTimeKind,
                                                                                            interpreter.DateTimeStyles,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref dateTimeValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "decimal":
                                                                                {
                                                                                    decimal decimalValue = Decimal.Zero; /* NOT USED */

                                                                                    //
                                                                                    // FIXME: PRI 4: This is not 100% compatible
                                                                                    //        with the Tcl semantics.
                                                                                    //
                                                                                    if (Value.GetDecimal(
                                                                                            @string, ValueFlags.AnyDecimal,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref decimalValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "dict":
                                                                                {
                                                                                    //
                                                                                    // NOTE: Also see Tcl TIP #501.
                                                                                    //
                                                                                    StringList list = null;

                                                                                    if ((ListOps.GetOrCopyOrSplitList(
                                                                                            interpreter, argument, true,
                                                                                            ref list) == ReturnCode.Ok) &&
                                                                                        ((list.Count % 2) == 0))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "directory":
                                                                                {
                                                                                    if (PathOps.ValidatePathAsDirectory(
                                                                                            @string, true, true))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "double":
                                                                                {
                                                                                    double doubleValue = 0.0; /* NOT USED */

                                                                                    //
                                                                                    // FIXME: PRI 4: This is not 100% compatible
                                                                                    //        with the Tcl semantics.
                                                                                    //
                                                                                    if (Value.GetDouble(
                                                                                            @string, ValueFlags.AnyDouble,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref doubleValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "encoding":
                                                                                {
                                                                                    Encoding encoding = null;

                                                                                    if (interpreter.GetEncoding(
                                                                                            @string, LookupFlags.EncodingNoVerbose,
                                                                                            ref encoding) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "element":
                                                                                {
                                                                                    VariableFlags flags = VariableFlags.CommonCommandMask;
                                                                                    IVariable variable = null;

                                                                                    if (interpreter.GetVariableViaResolversWithSplit(
                                                                                            @string, ref flags, ref variable) == ReturnCode.Ok)
                                                                                    {
                                                                                        if (EntityOps.IsLink(variable))
                                                                                            variable = EntityOps.FollowLinks(variable, flags);

                                                                                        if ((variable == null) ||
                                                                                            EntityOps.IsUndefined(variable) ||
                                                                                            !EntityOps.IsArray(variable) ||
                                                                                            !FlagOps.HasFlags(flags, VariableFlags.WasElement, true))
                                                                                        {
                                                                                            valid = not;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            //
                                                                                            // HACK: To really validate that the provided string
                                                                                            //       is an array element, we need to attempt to
                                                                                            //       query its value.  This ends up calling into
                                                                                            //       the resolver again; however, this cannot
                                                                                            //       be avoided due to various trace-only arrays
                                                                                            //       like "::env".
                                                                                            //
                                                                                            Result localValue = null; /* NOT USED */

                                                                                            if (interpreter.GetVariableValue(
                                                                                                    VariableFlags.None, @string,
                                                                                                    ref localValue) == ReturnCode.Ok)
                                                                                            {
                                                                                                valid = !not;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                valid = not;
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "entier":
                                                                                {
#if NET_40
                                                                                    BigInteger bigIntegerValue = BigInteger.Zero;

                                                                                    if (Value.GetBigInteger(
                                                                                            @string, ValueFlags.AnyInteger,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref bigIntegerValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
#endif
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "file":
                                                                                {
                                                                                    if (PathOps.ValidatePathAsFile(
                                                                                            @string, true, true))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "guid":
                                                                                {
                                                                                    Guid guid = Guid.Empty; /* NOT USED */

                                                                                    if (Value.GetGuid(
                                                                                            @string, interpreter.InternalCultureInfo,
                                                                                            ref guid) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "identifier":
                                                                                {
                                                                                    valid = StringOps.StringIs(@string,
                                                                                        StringOps.CharIsIdentifierZero,
                                                                                        StringOps.CharIsIdentifierOnePlus,
                                                                                        not, any, !strict, ref failIndex);

                                                                                    break;
                                                                                }
                                                                            case "idxranges":
                                                                                {
                                                                                    IndexRangeList ranges = null; /* NOT USED */

                                                                                    if ((count != null) &&
                                                                                        (RuntimeOps.ParseIndexRanges(
                                                                                            @string, (int)count,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref ranges) == ReturnCode.Ok))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "inetaddr":
                                                                                {
                                                                                    //
                                                                                    // HACK: Any valid 32-bit integer, including
                                                                                    //       their signed or unsigned variations,
                                                                                    //       is technically a valid IP address
                                                                                    //
                                                                                    long longValue = 0; /* NOT USED */

                                                                                    if (Value.GetWideInteger2(
                                                                                            @string, ValueFlags.AnyInteger,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref longValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
#if NETWORK
                                                                                        IPAddress address;

                                                                                        if (IPAddress.TryParse(@string, out address))
                                                                                        {
                                                                                            valid = !not;
                                                                                        }
                                                                                        else
#endif
                                                                                        {
                                                                                            //
                                                                                            // HACK: Not valid -OR- not supported.
                                                                                            //
                                                                                            valid = not;
                                                                                        }
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "integer":
                                                                                {
                                                                                    int intValue = 0; /* NOT USED */

                                                                                    if (Value.GetInteger2(@string,
                                                                                            ValueFlags.AnyInteger |
                                                                                            ValueFlags.WidenToUnsigned,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref intValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "interpreter":
                                                                                {
                                                                                    Interpreter interpreterValue = null; /* NOT USED */

                                                                                    if (Value.GetInterpreter(
                                                                                            interpreter, @string, InterpreterType.Default,
                                                                                            ref interpreterValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "list":
                                                                                {
                                                                                    StringList list = null; /* NOT USED */

                                                                                    if (ListOps.GetOrCopyOrSplitList(
                                                                                            interpreter, argument, true,
                                                                                            ref list) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "none":
                                                                                {
                                                                                    int index = Index.Invalid;

                                                                                    if ((Value.GetIndex(
                                                                                            @string, Count.Invalid, ValueFlags.AnyIndex,
                                                                                            interpreter.InternalCultureInfo, ref index,
                                                                                            ref result) == ReturnCode.Ok) &&
                                                                                        (index == Index.Invalid))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "number":
                                                                                {
                                                                                    INumber number = null; /* NOT USED */

                                                                                    if (Value.GetNumber(
                                                                                            @string, ValueFlags.AnyNumberAnyRadix,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref number) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "numeric":
                                                                                {
                                                                                    object numericValue = null; /* NOT USED */

                                                                                    //
                                                                                    // HACK: The ValueFlags passed to this method
                                                                                    //       are only consulted when dealing with
                                                                                    //       integer radix handling.
                                                                                    //
                                                                                    if (Value.GetNumeric(interpreter,
                                                                                            @string, ValueFlags.AnyRadixAnySign,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref numericValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "object":
                                                                                {
                                                                                    object objectValue = null; /* NOT USED */

                                                                                    if (Value.GetObject(
                                                                                            interpreter, @string,
                                                                                            ref objectValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "path":
                                                                                {
                                                                                    if (PathOps.CheckForValid(
                                                                                            null, @string, false, false, true,
                                                                                            PlatformOps.IsWindowsOperatingSystem()))
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "plugin":
                                                                                {
                                                                                    IPlugin plugin = null; /* NOT USED */

                                                                                    if ((interpreter.GetPlugin(
                                                                                            @string, LookupFlags.NoVerbose,
                                                                                            ref plugin) == ReturnCode.Ok) ||
                                                                                        interpreter.InternalFindPlugin(
                                                                                            null, MatchMode.Glob, @string,
                                                                                            null, null, LookupFlags.NoVerbose,
                                                                                            false) != null)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "real":
                                                                                {
                                                                                    INumber number = null; /* NOT USED */

                                                                                    if (Value.GetNumber(
                                                                                            @string, ValueFlags.AnyRealAnyRadix,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref number) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "ruleset":
                                                                                {
                                                                                    IRuleSet ruleSet = RuleSet.Create(
                                                                                        @string, interpreter.InternalCultureInfo);

                                                                                    if (ruleSet != null)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "scalar":
                                                                                {
                                                                                    VariableFlags flags = VariableFlags.CommonCommandMask;
                                                                                    IVariable variable = null;

                                                                                    if (interpreter.GetVariableViaResolversWithSplit(
                                                                                            @string, ref flags, ref variable) == ReturnCode.Ok)
                                                                                    {
                                                                                        if (EntityOps.IsLink(variable))
                                                                                            variable = EntityOps.FollowLinks(variable, flags);

                                                                                        if ((variable == null) ||
                                                                                            EntityOps.IsUndefined(variable) ||
                                                                                            EntityOps.IsArray(variable) ||
                                                                                            FlagOps.HasFlags(flags, VariableFlags.WasElement, true))
                                                                                        {
                                                                                            valid = not;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            valid = !not;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "single":
                                                                                {
                                                                                    float floatValue = 0.0f; /* NOT USED */

                                                                                    if (Value.GetSingle(
                                                                                            @string, interpreter.InternalCultureInfo,
                                                                                            ref floatValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "timespan":
                                                                                {
                                                                                    TimeSpan timeSpanValue = TimeSpan.Zero; /* NOT USED */

                                                                                    if (Value.GetTimeSpan(
                                                                                            @string, interpreter.InternalCultureInfo,
                                                                                            ref timeSpanValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "type":
                                                                                {
                                                                                    Type type = null; /* NOT USED */

                                                                                    if (Value.GetAnyType(
                                                                                            interpreter, @string, null, null,
                                                                                            Value.GetTypeValueFlags(false, false, false),
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref type) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "uri":
                                                                                {
                                                                                    Uri uri = null; /* NOT USED */

                                                                                    if (Value.GetUri(
                                                                                            @string, UriKind.Absolute,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref uri) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "value":
                                                                                {
                                                                                    object objectValue = null; /* NOT USED */

                                                                                    if (Value.GetValue(
                                                                                            @string, interpreter.DateTimeFormat,
                                                                                            ValueFlags.AnyNonCharacter,
                                                                                            interpreter.DateTimeKind,
                                                                                            interpreter.DateTimeStyles,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref objectValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "variant":
                                                                                {
                                                                                    IVariant variant = null; /* NOT USED */

                                                                                    if (Value.GetVariant(
                                                                                            interpreter, @string,
                                                                                            interpreter.DateTimeFormat,
                                                                                            ValueFlags.AnyVariant,
                                                                                            interpreter.DateTimeKind,
                                                                                            interpreter.DateTimeStyles,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref variant) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "version":
                                                                                {
                                                                                    Version version = null; /* NOT USED */

                                                                                    if (Value.GetVersion(
                                                                                            @string,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref version) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "versionrange":
                                                                                {
                                                                                    Version version1 = null; /* NOT USED */
                                                                                    Version version2 = null; /* NOT USED */

                                                                                    if (Value.GetVersionRange(
                                                                                            @string, ValueFlags.AnyVersionRange,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref version1, ref version2) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "xml":
                                                                                {
#if XML
                                                                                    XmlDocument document = null; /* NOT USED */

                                                                                    if (XmlOps.LoadString(@string,
                                                                                            ref document) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
#endif
                                                                                    {
                                                                                        //
                                                                                        // HACK: Not valid -OR- not supported.
                                                                                        //
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            case "wideinteger":
                                                                                {
                                                                                    long longValue = 0; /* NOT USED */

                                                                                    if (Value.GetWideInteger2(@string,
                                                                                            ValueFlags.AnyWideInteger |
                                                                                            ValueFlags.AllowUnsigned,
                                                                                            interpreter.InternalCultureInfo,
                                                                                            ref longValue) == ReturnCode.Ok)
                                                                                    {
                                                                                        valid = !not;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        valid = not;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                            default:
                                                                                {
                                                                                    CharIsCallback callback;

                                                                                    if (charIsCallbacks.TryGetValue(
                                                                                            subSubCommand, out callback) &&
                                                                                        (callback != null))
                                                                                    {
                                                                                        valid = StringOps.StringIs(
                                                                                            @string, callback, not, any,
                                                                                            !strict, ref failIndex);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = ScriptOps.BadSubCommand(
                                                                                            interpreter, null, "class", subSubCommand,
                                                                                            isSubCommands, null, null);

                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                    break;
                                                                                }
                                                                        }
                                                                    }
                                                                    else if (strict && !noStrict)
                                                                    {
                                                                        valid = not;
                                                                    }
                                                                }

                                                                //
                                                                // NOTE: Handle the setting of the failure index.
                                                                //
                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if ((valid == not) && (failIndexVarName != null))
                                                                    {
                                                                        code = interpreter.SetVariableValue(
                                                                            VariableFlags.None, failIndexVarName,
                                                                            failIndex.ToString(), null, ref result);
                                                                    }
                                                                }

                                                                //
                                                                // NOTE: Handle the setting of the "good" -OR- "bad"
                                                                //       (effective) value that was actually checked.
                                                                //
                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (valid == not)
                                                                    {
                                                                        if (badVarName != null)
                                                                        {
                                                                            code = interpreter.SetVariableValue(
                                                                                VariableFlags.None, badVarName,
                                                                                @string, null, ref result);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (goodVarName != null)
                                                                        {
                                                                            code = interpreter.SetVariableValue(
                                                                                VariableFlags.None, goodVarName,
                                                                                @string, null, ref result);
                                                                        }
                                                                    }
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                    result = interpreter.BooleanToResult(valid);
                                                            }
                                                            else
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
                                                                    result = String.Format(
                                                                        IsWrongNumArgsFormat, this.Name, subCommand,
                                                                        subSubCommand);
                                                                }

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = IsWrongNumArgs;
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "last":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_Last);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) <= arguments.Count) &&
                                                        ((argumentIndex + 3) >= arguments.Count))
                                                    {
                                                        bool noCase = false;

                                                        if (options.IsPresent("-nocase"))
                                                            noCase = true;

                                                        IVariant value = null;

                                                        StringComparison comparisonType =
                                                            SharedStringOps.GetBinaryComparisonType(noCase);

                                                        if (options.IsPresent("-comparison", ref value))
                                                            comparisonType = (StringComparison)value.Value;

                                                        Argument needle = arguments[argumentIndex];
                                                        Argument haystack = arguments[argumentIndex + 1];

                                                        int needleLength = needle.Length;
                                                        int haystackLength = haystack.Length;

                                                        if ((needleLength > 0) && (haystackLength > 0))
                                                        {
                                                            if ((argumentIndex + 3) == arguments.Count)
                                                            {
                                                                int startIndex = Index.Invalid;

                                                                code = Value.GetIndex(
                                                                    arguments[argumentIndex + 2],
                                                                    haystackLength, ValueFlags.AnyIndex,
                                                                    interpreter.InternalCultureInfo, ref startIndex,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (startIndex < 0)
                                                                    {
                                                                        result = Index.Invalid;
                                                                    }
                                                                    else if (startIndex < haystackLength)
                                                                    {
                                                                        result = haystack.LastIndexOf(
                                                                            needle, startIndex, comparisonType);
                                                                    }
                                                                    else
                                                                    {
                                                                        result = haystack.LastIndexOf(
                                                                            needle, comparisonType);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = haystack.LastIndexOf(
                                                                    needle, comparisonType);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = Index.Invalid; /* COMPAT: Tcl. */
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
                                                            result = "wrong # args: should be \"string last ?options? needleString haystackString ?startIndex?\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string last ?options? needleString haystackString ?startIndex?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "length":
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                result = arguments[2].Length;
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string length string\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "map":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_Map);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) == arguments.Count))
                                                    {
                                                        StringList list = null;

                                                        code = ListOps.GetOrCopyOrSplitList(
                                                            interpreter, arguments[argumentIndex], true, ref list,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if ((list.Count % 2) == 0)
                                                            {
                                                                IVariant value = null;
                                                                string countVarName = null;

                                                                if (options.IsPresent("-countvar", ref value))
                                                                    countVarName = value.ToString();

                                                                int maximum = Count.Invalid;

                                                                if (options.IsPresent("-maximum", ref value))
                                                                    maximum = (int)value.Value;

                                                                bool multiPass = false;

                                                                if (options.IsPresent("-multipass"))
                                                                    multiPass = true;

                                                                MatchMode mode = StringOps.DefaultMapMatchMode;

                                                                if (options.IsPresent("-regexp"))
                                                                {
                                                                    mode &= ~MatchMode.SimpleModeMask;
                                                                    mode |= MatchMode.RegExp;
                                                                }

                                                                if (options.IsPresent("-eval"))
                                                                    mode |= MatchMode.Evaluate;

                                                                RegexOptions regExOptions = StringOps.DefaultRegExOptions;

                                                                if (options.IsPresent("-regexpoptions", ref value))
                                                                    regExOptions = (RegexOptions)value.Value;

                                                                bool noCase = false;

                                                                if (options.IsPresent("-nocase"))
                                                                {
                                                                    regExOptions |= RegexOptions.IgnoreCase;
                                                                    noCase = true;
                                                                }

                                                                bool subSpec = false;

                                                                if (options.IsPresent("-subspec"))
                                                                    subSpec = true;

                                                                StringComparison comparisonType =
                                                                    SharedStringOps.GetBinaryComparisonType(noCase);

                                                                if (options.IsPresent("-comparison", ref value))
                                                                    comparisonType = (StringComparison)value.Value;

                                                                StringPairList list2 = new StringPairList();

                                                                for (int index = 0; index < list.Count; index += 2)
                                                                    list2.Add(list[index], list[index + 1]);

                                                                int count = 0;
                                                                Result localResult;

                                                                if (multiPass)
                                                                {
                                                                    localResult = StringOps.StrMultiMap(
                                                                        interpreter, mode,
                                                                        arguments[argumentIndex + 1], 0,
                                                                        list2, comparisonType, regExOptions,
                                                                        maximum, subSpec, ref count);
                                                                }
                                                                else
                                                                {
                                                                    localResult = StringOps.StrMap(
                                                                        interpreter, mode,
                                                                        arguments[argumentIndex + 1], 0,
                                                                        list2, comparisonType, regExOptions,
                                                                        maximum, subSpec, ref count);
                                                                }

                                                                if (countVarName != null)
                                                                    code = interpreter.SetVariableValue(
                                                                        VariableFlags.None, countVarName,
                                                                        count.ToString(), null, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = localResult;
                                                            }
                                                            else
                                                            {
                                                                result = "char map list unbalanced";
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
                                                            result = "wrong # args: should be \"string map ?options? charMap string\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string map ?options? charMap string\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "match":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_Match);

                                                int argumentIndex = Index.Invalid;

                                                if (arguments.Count > 2)
                                                    code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                                else
                                                    code = ReturnCode.Ok;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    //
                                                    // NOTE: Get the index for the first non-option argument.
                                                    //
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) == arguments.Count))
                                                    {
                                                        IVariant value = null;
                                                        MatchMode mode = StringOps.DefaultMatchMode;

                                                        if (options.IsPresent("-mode", ref value))
                                                            mode = (MatchMode)value.Value;

                                                        bool noCase = false;

                                                        if (options.IsPresent("-nocase"))
                                                            noCase = true;

                                                        bool match = StringOps.Match(
                                                            interpreter, mode, arguments[argumentIndex + 1],
                                                            arguments[argumentIndex], noCase);

                                                        result = interpreter.BooleanToResult(match);
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
                                                            result = "wrong # args: should be \"string match ?options? pattern string\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string match ?options? pattern string\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "ordinal":
                                        {
                                            if (arguments.Count == 4)
                                            {
                                                string value = arguments[2];
                                                int index = Index.Invalid;

                                                code = Value.GetIndex(
                                                    arguments[3], value.Length, ValueFlags.AnyIndex,
                                                    interpreter.InternalCultureInfo, ref index, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((index >= 0) && (index < value.Length))
                                                        result = ConversionOps.ToInt(value[index]);
                                                    else
                                                        result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string ordinal string charIndex\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "range":
                                        {
                                            if (arguments.Count == 5)
                                            {
                                                int firstIndex = Index.Invalid;

                                                code = Value.GetIndex(
                                                    arguments[3], arguments[2].Length, ValueFlags.AnyIndex,
                                                    interpreter.InternalCultureInfo, ref firstIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    int lastIndex = Index.Invalid;

                                                    code = Value.GetIndex(
                                                        arguments[4], arguments[2].Length, ValueFlags.AnyIndex,
                                                        interpreter.InternalCultureInfo, ref lastIndex, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (firstIndex < 0)
                                                            firstIndex = 0;

                                                        if (lastIndex >= arguments[2].Length)
                                                            lastIndex = arguments[2].Length - 1;

                                                        if (firstIndex <= lastIndex)
                                                            result = arguments[2].Substring(
                                                                firstIndex, (lastIndex - firstIndex) + 1);
                                                        else
                                                            result = String.Empty;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string range string first last\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "repeat":
                                        {
                                            if (arguments.Count == 4)
                                            {
                                                string value = arguments[2];
                                                int count = 0;

                                                code = Value.GetInteger2(
                                                    (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                    interpreter.InternalCultureInfo, ref count, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
#if RESULT_LIMITS
                                                    /* NO RESULT */
                                                    Engine.CheckResultAgainstLimits(
                                                        interpreter, (value != null) ? value.Length : 0,
                                                        count, ref code, ref result);

                                                    if (code == ReturnCode.Ok)
#endif
                                                    {
                                                        result = StringOps.StrRepeat(count, value);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string repeat string count\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "replace":
                                        {
                                            if ((arguments.Count == 5) || (arguments.Count == 6))
                                            {
                                                int firstIndex = Index.Invalid;

                                                code = Value.GetIndex(
                                                    arguments[3], arguments[2].Length, ValueFlags.AnyIndex,
                                                    interpreter.InternalCultureInfo, ref firstIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    int lastIndex = Index.Invalid;

                                                    code = Value.GetIndex(
                                                        arguments[4], arguments[2].Length, ValueFlags.AnyIndex,
                                                        interpreter.InternalCultureInfo, ref lastIndex, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // BUGFIX: Per Tcl, the string is returned unchanged
                                                        //         when the (clamped) range selects nothing --
                                                        //         i.e. last < first, last < 0, or first is at
                                                        //         or past the end of the string (first >=
                                                        //         length).  Using ">" here would let a "first"
                                                        //         exactly equal to the length fall through and
                                                        //         wrongly append the replacement.
                                                        //
                                                        if ((lastIndex < firstIndex) ||
                                                            (lastIndex < 0) ||
                                                            (firstIndex >= arguments[2].Length))
                                                        {
                                                            result = arguments[2];
                                                        }
                                                        else
                                                        {
                                                            if (firstIndex < 0)
                                                                firstIndex = 0;

                                                            StringBuilder builder = StringBuilderFactory.Create();

                                                            builder.Append(arguments[2].Substring(0, firstIndex));

                                                            if (arguments.Count == 6)
                                                                builder.Append(arguments[5]);

                                                            if ((lastIndex + 1) < arguments[2].Length)
                                                                builder.Append(arguments[2].Substring(lastIndex + 1));

                                                            result = StringBuilderCache.GetStringAndRelease(ref builder);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string replace string first last ?string?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "reverse":
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                result = StringOps.StrReverse(arguments[2]);
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string reverse string\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "starts":
                                        {
                                            if (arguments.Count >= 4)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    CommandOptionType.String_Starts);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 2) == arguments.Count))
                                                    {
                                                        IVariant value = null;
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        CultureInfo cultureInfo = null;

                                                        if (options.IsPresent("-culture", ref value))
                                                            cultureInfo = (CultureInfo)value.Value;
#endif

                                                        bool noCase = false;

                                                        if (options.IsPresent("-nocase"))
                                                            noCase = true;

                                                        StringComparison comparisonType =
                                                            SharedStringOps.GetBinaryComparisonType(noCase);

                                                        if (options.IsPresent("-comparison", ref value))
                                                            comparisonType = (StringComparison)value.Value;

#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        if (cultureInfo != null)
                                                        {
                                                            result = SharedStringOps.StartsWith(
                                                                arguments[argumentIndex + 1],
                                                                arguments[argumentIndex],
                                                                cultureInfo, noCase);
                                                        }
                                                        else
#endif
                                                        {
                                                            result = SharedStringOps.StartsWith(
                                                                arguments[argumentIndex + 1],
                                                                arguments[argumentIndex],
                                                                comparisonType);
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
                                                                "wrong # args: should be \"{0} {1} ?options? prefix string\"",
                                                                this.Name, subCommand);
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "wrong # args: should be \"{0} {1} ?options? prefix string\"",
                                                    this.Name, subCommand);

                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "tolower":
                                    case "totitle":
                                    case "toupper":
                                        {
                                            if (arguments.Count >= 3)
                                            {
                                                OptionDictionary options = CommandOptions.GetCommandOptions(
                                                    this.Name, subCommand);

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 3) >= arguments.Count))
                                                    {
                                                        IVariant value = null;

#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                        CultureInfo cultureInfo = null;

                                                        if (options.IsPresent("-culture", ref value))
                                                            cultureInfo = (CultureInfo)value.Value;
#endif

                                                        bool? invariant = null;

                                                        if (options.IsPresent("-invariant", ref value))
                                                            invariant = (bool)value.Value;

                                                        string text = arguments[argumentIndex];

                                                        if ((argumentIndex + 1) == arguments.Count)
                                                        {
                                                            if (SharedStringOps.SystemEquals(
                                                                    subCommand, "totitle"))
                                                            {
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                                result = StringOps.ToTitle(
                                                                    text, cultureInfo, invariant);
#else
                                                                result = StringOps.ToTitle(
                                                                    text, null, invariant);
#endif
                                                            }
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                            else if (cultureInfo != null)
                                                            {
                                                                result = (string)typeof(string).InvokeMember(
                                                                    subCommand, ObjectOps.GetBindingFlags(
                                                                    MetaBindingFlags.LooseMethod, true),
                                                                    null, text, new object[] { cultureInfo });
                                                            }
#endif
                                                            else
                                                            {
                                                                StringOps.MaybeMutateCaseMethodName(
                                                                    ref subCommand, invariant);

                                                                result = (string)typeof(string).InvokeMember(
                                                                    subCommand, ObjectOps.GetBindingFlags(
                                                                    MetaBindingFlags.LooseMethod, true),
                                                                    null, text, null);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int firstIndex = Index.Invalid;

                                                            if ((code == ReturnCode.Ok) &&
                                                                ((argumentIndex + 1) < arguments.Count))
                                                            {
                                                                code = Value.GetIndex(
                                                                    arguments[argumentIndex + 1],
                                                                    text.Length, ValueFlags.AnyIndex,
                                                                    interpreter.InternalCultureInfo,
                                                                    ref firstIndex, ref result);
                                                            }

                                                            if (firstIndex < 0)
                                                                firstIndex = 0;

                                                            int lastIndex = firstIndex;

                                                            if ((code == ReturnCode.Ok) &&
                                                                ((argumentIndex + 2) < arguments.Count))
                                                            {
                                                                code = Value.GetIndex(
                                                                    arguments[argumentIndex + 2],
                                                                    text.Length, ValueFlags.AnyIndex,
                                                                    interpreter.InternalCultureInfo,
                                                                    ref lastIndex, ref result);
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (lastIndex >= text.Length)
                                                                    lastIndex = text.Length - 1;

                                                                if (lastIndex >= firstIndex)
                                                                {
                                                                    StringBuilder builder = StringBuilderFactory.Create();

                                                                    if (firstIndex > 0)
                                                                        builder.Append(text.Substring(0, firstIndex));

                                                                    if (SharedStringOps.SystemEquals(subCommand, "totitle"))
                                                                    {
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                                        builder.Append(StringOps.ToTitle(
                                                                            text.Substring(
                                                                                firstIndex, (lastIndex - firstIndex) + 1),
                                                                            cultureInfo, invariant));
#else
                                                                        builder.Append(StringOps.ToTitle(
                                                                            text.Substring(
                                                                                firstIndex, (lastIndex - firstIndex) + 1),
                                                                            null, invariant));
#endif
                                                                    }
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                                                                    else if (cultureInfo != null)
                                                                    {
                                                                        builder.Append((string)typeof(string).InvokeMember(
                                                                            subCommand, ObjectOps.GetBindingFlags(
                                                                            MetaBindingFlags.LooseMethod, true),
                                                                            null, text.Substring(firstIndex,
                                                                            (lastIndex - firstIndex) + 1),
                                                                            new object[] { cultureInfo }));
                                                                    }
#endif
                                                                    else
                                                                    {
                                                                        StringOps.MaybeMutateCaseMethodName(
                                                                            ref subCommand, invariant);

                                                                        builder.Append((string)typeof(string).InvokeMember(
                                                                            subCommand, ObjectOps.GetBindingFlags(
                                                                            MetaBindingFlags.LooseMethod, true),
                                                                            null, text.Substring(firstIndex,
                                                                            (lastIndex - firstIndex) + 1), null));
                                                                    }

                                                                    if ((lastIndex + 1) < text.Length)
                                                                    {
                                                                        builder.Append(text.Substring(
                                                                            lastIndex + 1, text.Length - (lastIndex + 1)));
                                                                    }

                                                                    result = StringBuilderCache.GetStringAndRelease(ref builder);
                                                                }
                                                                else
                                                                {
                                                                    result = text;
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
                                                                "wrong # args: should be \"{0} {1} ?options? string ?first? ?last?\"",
                                                                this.Name, subCommand);
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "wrong # args: should be \"{0} {1} ?options? string ?first? ?last?\"",
                                                    this.Name, subCommand);

                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "trim":
                                        {
                                            if ((arguments.Count == 3) || (arguments.Count == 4))
                                            {
                                                CharList characters = Characters.WhiteSpaceCharList;

                                                if (arguments.Count == 4)
                                                    characters = new CharList(arguments[3].ToCharArray());

                                                result = arguments[2].Trim(characters.ToArray());
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string trim string ?chars?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "trimleft":
                                        {
                                            if ((arguments.Count == 3) || (arguments.Count == 4))
                                            {
                                                CharList characters = Characters.WhiteSpaceCharList;

                                                if (arguments.Count == 4)
                                                    characters = new CharList(arguments[3].ToCharArray());

                                                result = arguments[2].TrimStart(characters.ToArray());
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string trimleft string ?chars?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "trimright":
                                        {
                                            if ((arguments.Count == 3) || (arguments.Count == 4))
                                            {
                                                CharList characters = Characters.WhiteSpaceCharList;

                                                if (arguments.Count == 4)
                                                    characters = new CharList(arguments[3].ToCharArray());

                                                result = arguments[2].TrimEnd(characters.ToArray());
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string trimright string ?chars?\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "wordend":
                                        {
                                            if (arguments.Count == 4)
                                            {
                                                string text = arguments[2];
                                                int charIndex = Index.Invalid;

                                                code = Value.GetIndex(
                                                    arguments[3], text.Length, ValueFlags.AnyIndex,
                                                    interpreter.InternalCultureInfo, ref charIndex,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (charIndex < 0)
                                                        charIndex = 0;

                                                    int index;

                                                    if (charIndex < text.Length)
                                                    {
                                                        for (index = charIndex; index < text.Length; index++)
                                                            if (!StringOps.CharIsWord(text[index]))
                                                                break;

                                                        if (index == charIndex)
                                                            index++;
                                                    }
                                                    else
                                                    {
                                                        index = text.Length;
                                                    }

                                                    result = index;
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string wordend string index\"";
                                                code = ReturnCode.Error;
                                            }
                                            break;
                                        }
                                    case "wordstart":
                                        {
                                            if (arguments.Count == 4)
                                            {
                                                string text = arguments[2];
                                                int charIndex = Index.Invalid;

                                                code = Value.GetIndex(
                                                    arguments[3], text.Length, ValueFlags.AnyIndex,
                                                    interpreter.InternalCultureInfo, ref charIndex,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (charIndex >= text.Length)
                                                        charIndex = text.Length - 1;

                                                    int index = 0;

                                                    if (charIndex > 0)
                                                    {
                                                        for (index = charIndex; index >= 0; index--)
                                                            if (!StringOps.CharIsWord(text[index]))
                                                                break;

                                                        if (index != charIndex)
                                                            index++;
                                                    }

                                                    result = index;
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"string wordstart string index\"";
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
                    }
                    else
                    {
                        result = "wrong # args: should be \"string option ?arg ...?\"";
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

        done:

            return code;
        }
        #endregion
    }
}
