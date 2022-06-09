/*
 * Unset.cs --
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
    [ObjectId("fcafc062-3490-4cf9-83d7-7ddb2c1e8838")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Unset : Core
    {
        public Unset(
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
            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 1)
                    {
                        if (arguments.Count > 1)
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] {
                                new Option(null, OptionFlags.None, Index.Invalid,
                                    Index.Invalid, "-nocomplain", null),
                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                    Index.Invalid, "-unlinkonly", null),
                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                    Index.Invalid, "-remove", null),
                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                    Index.Invalid, "-notrace", null),
                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                    Index.Invalid, "-purge", null),
#if !MONO && NATIVE && WINDOWS
                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                    Index.Invalid, "-zerostring", null),
                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                    Index.Invalid, "-maybezerostring", null),
#else
                                new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported,
                                    Index.Invalid, Index.Invalid, "-zerostring", null),
                                new Option(null, OptionFlags.Unsafe | OptionFlags.Ignored,
                                    Index.Invalid, Index.Invalid, "-maybezerostring", null),
#endif
                                Option.CreateEndOfOptions()
                            });

                            int argumentIndex = Index.Invalid;

                            if (interpreter.GetOptions(
                                    options, arguments, 0, 1, Index.Invalid, true,
                                    ref argumentIndex, ref result) == ReturnCode.Ok)
                            {
                                if (argumentIndex != Index.Invalid)
                                {
                                    //
                                    // TODO: Is this really needed to be Tcl compliant?
                                    //
                                    VariableFlags flags = VariableFlags.NoRemove;

                                    if (options.IsPresent("-unlinkonly"))
                                        flags |= VariableFlags.NoFollowLink;

                                    if (options.IsPresent("-nocomplain"))
                                        flags |= VariableFlags.NoComplain;

                                    if (options.IsPresent("-remove"))
                                        flags &= ~VariableFlags.NoRemove;

                                    if (options.IsPresent("-notrace"))
                                        flags |= VariableFlags.NoTrace;

                                    if (options.IsPresent("-purge"))
                                        flags |= VariableFlags.Purge;

                                    ///////////////////////////////////////////////////////////////////

#if !MONO && NATIVE && WINDOWS
                                    if (options.IsPresent("-zerostring"))
                                    {
                                        flags |= VariableFlags.ZeroStringMask;
                                    }
                                    else if (!CommonOps.Runtime.IsMono() &&
                                        options.IsPresent("-maybezerostring"))
                                    {
                                        flags |= VariableFlags.ZeroStringMask;
                                    }
#endif

                                    ///////////////////////////////////////////////////////////////////

                                    for (; argumentIndex < arguments.Count; argumentIndex++)
                                    {
                                        if (interpreter.UnsetVariable(
                                                flags, arguments[argumentIndex],
                                                ref result) != ReturnCode.Ok)
                                        {
                                            return ReturnCode.Error;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                return ReturnCode.Error;
                            }
                        }

                        /*
                         * Do nothing if no arguments supplied, so as to match
                         * command documentation.
                         */
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        result = "wrong # args: should be \"unset ?options? ?varName varName ...?\"";
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
