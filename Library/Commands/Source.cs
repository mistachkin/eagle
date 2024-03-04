/*
 * Source.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("076f3c98-c556-4145-aba5-5aa440040581")]
    /*
     * POLICY: We allow files in the script library directory to be sourced.
     */
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("engine")]
    internal sealed class Source : Core
    {
        public Source(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

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
                result = "wrong # args: should be \"source ?options? fileName\"";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;

            OptionDictionary options = new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-withinfo", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-time", null),
                Option.CreateEndOfOptions()
            });

            int argumentIndex = Index.Invalid;

            if (arguments.Count > 2)
            {
                if (interpreter.GetOptions(
                        options, arguments, 0, 1, Index.Invalid, false,
                        ref argumentIndex, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                argumentIndex = 1;
            }

            if ((argumentIndex == Index.Invalid) ||
                ((argumentIndex + 1) != arguments.Count))
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
                    result = "wrong # args: should be \"source ?options? fileName\"";
                }

                return ReturnCode.Error;
            }

            IVariant value = null;
            Encoding encoding = null;

            if (options.IsPresent("-encoding", ref value))
                encoding = (Encoding)value.Value;

            bool withInfo = false;

            if (options.IsPresent("-withinfo", ref value))
                withInfo = (bool)value.Value;

            bool time = false;

            if (options.IsPresent("-time", ref value))
                time = (bool)value.Value;

            if (code == ReturnCode.Ok)
            {
                string name = StringList.MakeList(
                    "source", arguments[argumentIndex]);

                ICallFrame frame = interpreter.NewTrackingCallFrame(
                    name, CallFrameFlags.Source);

                interpreter.PushAutomaticCallFrame(frame);

                try
                {
#if ARGUMENT_CACHE
                    CacheFlags savedCacheFlags;

                    if (withInfo)
                    {
                        interpreter.BeginNoArgumentCache(
                            out savedCacheFlags);
                    }
                    else
                    {
                        savedCacheFlags = CacheFlags.None;
                    }

                    try
                    {
#endif
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                        InterpreterStateFlags savedInterpreterStateFlags;

                        if (withInfo)
                        {
                            interpreter.BeginArgumentLocation(
                                out savedInterpreterStateFlags);
                        }
                        else
                        {
                            savedInterpreterStateFlags =
                                InterpreterStateFlags.None;
                        }

                        try
                        {
#endif
                            IProfilerState profiler = null;
                            bool dispose = true;

                            try
                            {
                                if (time)
                                {
                                    profiler = ProfilerState.Create(
                                        interpreter, ref dispose);
                                }

                                if (profiler != null)
                                    profiler.Start();

                                code = interpreter.EvaluateFile(
                                    encoding, arguments[argumentIndex],
                                    ref result);

                                if (profiler != null)
                                {
                                    profiler.Stop();

                                    TraceOps.DebugTrace(String.Format(
                                        "Execute: completed in {0}",
                                        FormatOps.MaybeNull(profiler)),
                                        typeof(Source).Name,
                                        TracePriority.Command);
                                }
                            }
                            finally
                            {
                                if (profiler != null)
                                {
                                    if (dispose)
                                    {
                                        ObjectOps.TryDisposeOrComplain<IProfilerState>(
                                            interpreter, ref profiler);
                                    }

                                    profiler = null;
                                }
                            }
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                        }
                        finally
                        {
                            if (withInfo)
                            {
                                interpreter.EndArgumentLocation(
                                    ref savedInterpreterStateFlags);
                            }
                        }
#endif
#if ARGUMENT_CACHE
                    }
                    finally
                    {
                        if (withInfo)
                        {
                            interpreter.EndNoArgumentCache(
                                ref savedCacheFlags);
                        }
                    }
#endif
                }
                finally
                {
                    //
                    // NOTE: Pop the original call frame that we pushed above
                    //       and any intervening scope call frames that may be
                    //       leftover (i.e. they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFramesAndOneMore();
                }
            }

            return code;
        }
        #endregion
    }
}
