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
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>source</c> command, which reads a
    /// file and evaluates its contents as a script in the current interpreter.
    /// It supports options that control the text encoding, optional timing,
    /// library/package handling, and (when compiled with data support) the
    /// evaluation of encrypted script bundles.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("076f3c98-c556-4145-aba5-5aa440040581")]
    /*
     * POLICY: We allow files in the script library directory to be sourced.
     */
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("engine")]
    internal sealed class Source : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>source</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Source(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>source</c> command.  It parses any
        /// supplied options, opens the named file, and evaluates its contents
        /// as a script within a freshly pushed call frame, optionally honoring
        /// a specific text encoding, profiling the evaluation, entering the
        /// package level, and (when data support is compiled in) treating the
        /// file as an encrypted script bundle.
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
        /// command name; an optional run of leading elements supplies command
        /// options, and the final element supplies the name of the file to be
        /// sourced.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by evaluating the
        /// sourced file.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the file is sourced and evaluated
        /// successfully; otherwise, a non-Ok value (e.g.
        /// <see cref="ReturnCode.Error" />) when the interpreter is null, the
        /// argument list is null, the wrong number of arguments is supplied,
        /// option parsing fails, or evaluation of the file fails, with details
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
                result = "wrong # args: should be \"source ?options? fileName\"";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;

            OptionDictionary options =
                CommandOptions.GetCommandOptions(
                    CommandOptionType.Source);

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

            byte[] password = null; /* NOTE: For bundle use only. */

            if (options.IsPresent("-password", ref value))
                password = (byte[])value.Value;

            bool withInfo = false;

            if (options.IsPresent("-withinfo", ref value))
                withInfo = (bool)value.Value;

            bool time = false;

            if (options.IsPresent("-time", ref value))
                time = (bool)value.Value;

            bool library = false;

            if (options.IsPresent("-library", ref value))
                library = (bool)value.Value;

            bool package = false;

            if (options.IsPresent("-package", ref value))
                package = (bool)value.Value;

#if DATA
            bool? bundle = null;

            if (options.IsPresent("-bundle", ref value))
                bundle = (bool?)value.Value;

            BundleFlags bundleFlags = BundleFlags.Default;

            if (options.IsPresent("-bundleflags", ref value))
                bundleFlags = (BundleFlags)value.Value;
#endif

            if (code == ReturnCode.Ok)
            {
                string fileName = arguments[argumentIndex];
                string name = StringList.MakeList("source", fileName);

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
                                null, out savedInterpreterStateFlags);
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
                                ProcedureFlags savedProcedureFlags1 =
                                    ProcedureFlags.None;

                                if (library)
                                {
                                    interpreter.BeginLibraryProcedures(
                                        out savedProcedureFlags1);
                                }

                                try
                                {
                                    ProcedureFlags savedProcedureFlags2 =
                                        ProcedureFlags.None;

                                    if (package)
                                    {
                                        interpreter.BeginPackageProcedures(
                                            out savedProcedureFlags2);

                                        interpreter.EnterPackageLevel();
                                    }

                                    try
                                    {
                                        if (time)
                                        {
                                            profiler = ProfilerState.Create(
                                                interpreter, ref dispose);
                                        }

                                        if (profiler != null)
                                            profiler.Start();

#if DATA
                                        if (bundle == null)
                                            bundle = PathOps.MightBeBundleFile(fileName);

                                        if ((bool)bundle)
                                        {
                                            code = interpreter.EvaluateBundleFile(
                                                fileName, password, bundleFlags,
                                                ref clientData, ref result);
                                        }
                                        else
#endif
                                        {
                                            code = interpreter.EvaluateFile(
                                                encoding, fileName, ref result);
                                        }

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
                                        if (package)
                                        {
                                            interpreter.ExitPackageLevel();

                                            interpreter.EndPackageProcedures(
                                                ref savedProcedureFlags2);
                                        }
                                    }
                                }
                                finally
                                {
                                    if (library)
                                    {
                                        interpreter.EndLibraryProcedures(
                                            ref savedProcedureFlags1);
                                    }
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
