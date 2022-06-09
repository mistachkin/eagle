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
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("870fdad9-4698-4b0a-863d-8b9e0fe699ca")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Split : Core
    {
        public Split(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
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
                    if ((arguments.Count >= 2) && (arguments.Count <= 4))
                    {
                        string value = arguments[1];
                        string separators = Characters.Space.ToString();

                        if (arguments.Count >= 3)
                            separators = arguments[2];

                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-string", null),
                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 3)
                            code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                        else
                            code = ReturnCode.Ok;

                        if (code == ReturnCode.Ok)
                        {
                            if (argumentIndex == Index.Invalid)
                            {
                                bool @string = false;

                                if (options.IsPresent("-string"))
                                    @string = true;

                                StringList list;

                                if (@string)
                                {
                                    if (!String.IsNullOrEmpty(separators))
                                        list = new StringList(value.Split(
                                            new string[] { separators }, StringSplitOptions.None));
                                    else
                                        list = new StringList(value.ToCharArray());
                                }
                                else
                                {
                                    if (!String.IsNullOrEmpty(separators))
                                        list = new StringList(value.Split(
                                            separators.ToCharArray(), StringSplitOptions.None));
                                    else
                                        list = new StringList(value.ToCharArray());
                                }

                                result = list;
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
