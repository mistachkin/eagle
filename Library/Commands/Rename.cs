/*
 * Rename.cs --
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
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("fe410cf6-1f44-47d5-a9dc-613770302383")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.SecuritySdk)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Rename : Core
    {
        #region Private Constants
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"rename ?options? oldName newName\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Rename(
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

            if (arguments.Count < 3)
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            OptionDictionary options = new OptionDictionary(new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nodelete", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-hidden", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-hiddenonly", null),
                new Option(typeof(IdentifierKind),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-kind",
                    new Variant(IdentifierKind.None)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-newnamevar", null),
                Option.CreateEndOfOptions()
            });

            int argumentIndex = Index.Invalid;

            if (interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid, false,
                    ref argumentIndex, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if ((argumentIndex == Index.Invalid) ||
                ((argumentIndex + 1) >= arguments.Count) ||
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

            ///////////////////////////////////////////////////////////////////

            Variant value = null;
            IdentifierKind kind = IdentifierKind.None;

            if (options.IsPresent("-kind", ref value))
                kind = (IdentifierKind)value.Value;

            bool delete = true;

            if (options.IsPresent("-nodelete"))
                delete = false;

            bool hidden = false;

            if (options.IsPresent("-hidden"))
                hidden = true;

            bool hiddenOnly = false;

            if (options.IsPresent("-hiddenonly"))
                hiddenOnly = true;

            string varName = null;

            if (options.IsPresent("-newnamevar", ref value))
                varName = value.ToString();

            ///////////////////////////////////////////////////////////////////

            string oldName = arguments[argumentIndex];
            string newName = arguments[argumentIndex + 1];
            Result localResult = null;

            if (kind == IdentifierKind.Object)
            {
                if (interpreter.RenameObject(
                        oldName, newName, false, false, false,
                        ref localResult) == ReturnCode.Ok)
                {
                    result = String.Empty;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = localResult;
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (interpreter.RenameAnyIExecute(oldName,
                        newName, varName, kind, false,
                        delete, false, hidden, hiddenOnly,
                        ref localResult) == ReturnCode.Ok)
                {
                    result = String.Empty;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = localResult;
                    return ReturnCode.Error;
                }
            }
        }
        #endregion
    }
}
