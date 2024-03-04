/*
 * Preprocessor.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Diagnostics;

#if WIX_35 || WIX_36 || WIX_37 || WIX_38 || WIX_39 || WIX_310 || WIX_311
using System.Xml;
#endif

using Microsoft.Tools.WindowsInstallerXml;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

namespace Eagle._Extensions
{
    [ObjectId("dc8dd503-3594-4058-bf24-f9e5e4a084a5")]
    internal sealed class Preprocessor : PreprocessorExtension, IDisposable
    {
        #region Private Constants
        private static readonly string[] prefixes = {
            //
            // NOTE: Get the file name extension for scripts and remove
            //       the leading dot.
            //
            FileExtension.Script.Substring(1)
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default, no console.
        //
        private static readonly bool DefaultConsole = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want to initialize the script library.
        //       2. We want to throw an exception if disposed objects
        //          are accessed (only if built for WiX 3.5 or higher).
        //       3. We want to throw an exception if interpreter
        //          creation fails.
        //       4. We want to have only directories that actually
        //          exist in the auto-path.
        //
        private static readonly CreateFlags DefaultCreateFlags = (
            CreateFlags.EmbeddedUse
#if !WIX_35 && !WIX_36 && !WIX_37 && !WIX_38 && !WIX_39 && !WIX_310 && !WIX_311
            & ~CreateFlags.ThrowOnDisposed
#endif
            );

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We do not want to change the console title.
        //       2. We do not want to change the console icon.
        //       3. We do not want to intercept the Ctrl-C keypress.
        //
        private static readonly HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.EmbeddedUse;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default, consider all standard option sources.
        //
        private static readonly OptionOriginFlags DefaultOptionOriginFlags =
            OptionOriginFlags.Standard;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private TraceListener listener;
        private CreateFlags createFlags;
        private HostCreateFlags hostCreateFlags;
        private Interpreter interpreter;
        private bool console;
        private bool exceptions;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Preprocessor()
        {
            //
            // NOTE: Can we assume that the console host is available?
            //
            console = NeedConsole(DefaultConsole);

            //
            // NOTE: Get the effective interpreter creation flags from the
            //       environment, etc.
            //
            createFlags = Interpreter.GetStartupCreateFlags(
                null, DefaultCreateFlags, DefaultOptionOriginFlags,
                console, true);

            hostCreateFlags = Interpreter.GetStartupHostCreateFlags(
                null, DefaultHostCreateFlags, DefaultOptionOriginFlags,
                console, true);

            //
            // NOTE: By default, we do not want to allow "exceptional"
            //       (non-Ok) success return codes.
            //
            exceptions = false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static bool NeedConsole(
            bool @default
            )
        {
            //
            // HACK: By default, assume that a console-based host is not
            //       available.  Then, attempt to check and see if the
            //       user believes that one is available.  We use this
            //       very clumsy method because WiX does not seem to
            //       expose an easy way for us to determine if we have a
            //       console-like host available to output diagnostic
            //       [and other] information to.
            //
            try
            {
                if (@default)
                {
                    if (Utility.GetEnvironmentVariable(
                            EnvVars.NoConsole, true, false) != null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (Utility.GetEnvironmentVariable(
                            EnvVars.Console, true, false) != null)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Trace Listener Helper Methods
        private ReturnCode SetupTraceListeners(
            bool setup,
            bool console,
            bool strict
            )
        {
            Result error = null;

            return SetupTraceListeners(setup, console, strict, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode SetupTraceListeners(
            bool setup,
            bool console,
            bool strict,
            ref Result error
            )
        {
            try
            {
                if (setup)
                {
                    //
                    // NOTE: Add our trace listener to the collections for
                    //       trace and debug output.
                    //
                    if (listener == null)
                    {
                        listener = Utility.NewTraceListener(
                            Utility.GetTraceListenerType(console), null,
                            ref error);

                        if (listener != null)
                        {
                            /* IGNORED */
                            Utility.AddTraceListener(listener, false);

                            /* IGNORED */
                            Utility.AddTraceListener(listener, true);

                            return ReturnCode.Ok; // NOTE: Success.
                        }
                    }
                    else if (strict)
                    {
                        error = "trace listeners already setup";
                    }
                    else
                    {
                        return ReturnCode.Ok; // NOTE: Fake success.
                    }
                }
                else
                {
                    //
                    // NOTE: Remove and dispose our trace listeners now.
                    //
                    if (listener != null)
                    {
                        /* IGNORED */
                        Utility.RemoveTraceListener(listener, true);

                        /* IGNORED */
                        Utility.RemoveTraceListener(listener, false);

                        listener.Dispose();
                        listener = null;

                        return ReturnCode.Ok; // NOTE: Success.
                    }
                    else if (strict)
                    {
                        error = "trace listeners not setup";
                    }
                    else
                    {
                        return ReturnCode.Ok; // NOTE: Fake success.
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region PreprocessorExtension Members
        public override string[] Prefixes
        {
            get
            {
                CheckDisposed();

                return (string[])prefixes.Clone();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public override void InitializePreprocess()
        {
            CheckDisposed();

            ReturnCode code;
            Result result = null;

            interpreter = Interpreter.Create(
                null, createFlags, hostCreateFlags, ref result); /* throw */

            if (interpreter != null)
            {
                code = Interpreter.ProcessStartupOptions(
                    interpreter, null, createFlags,
                    DefaultOptionOriginFlags, console, true,
                    ref result);
            }
            else
            {
                code = ReturnCode.Error;
            }

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

#if WIX_35 || WIX_36 || WIX_37 || WIX_38 || WIX_39 || WIX_310 || WIX_311
        public override void FinalizePreprocess()
        {
            CheckDisposed(); /* throw */
            Dispose(); /* throw */
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public override string GetVariableValue(
            string prefix, /* NOTE: IGNORED, always "eagle". */
            string name
            )
        {
            CheckDisposed();

            //
            // HACK: Use a workaround for how WiX parses references to
            //       extension variables and functions (i.e. it always
            //       assumes that internal parenthesis indicate a call
            //       to a function).
            //
            if (name != null)
            {
                name = name.Replace(
                    Characters.OpenBracket, Characters.OpenParenthesis);

                name = name.Replace(
                    Characters.CloseBracket, Characters.CloseParenthesis);
            }

            ReturnCode code;
            Result value = null;
            Result error = null;

            code = interpreter.GetVariableValue(
                VariableFlags.None, name, ref value, ref error);

            //
            // NOTE: Did we succeed in fetching the variable value?
            //
            if (Utility.IsSuccess(code, exceptions))
                return value;

            throw new ScriptException(code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        public override string EvaluateFunction(
            string prefix, /* NOTE: IGNORED, always "eagle". */
            string function,
            string[] args)
        {
            CheckDisposed();

            ReturnCode code;
            Result result = null;

            code = interpreter.Invoke(
                function, ClientData.Empty,
                new ArgumentList(function, new StringList(args)),
                ref result);

            //
            // NOTE: Did we succeed in executing the command/procedure/etc?
            //
            if (Utility.IsSuccess(code, exceptions))
                return result;

            throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

#if WIX_35 || WIX_36 || WIX_37 || WIX_38 || WIX_39 || WIX_310 || WIX_311
        public override bool ProcessPragma(
            SourceLineNumberCollection sourceLineNumbers,
            string prefix, /* NOTE: IGNORED, always "eagle". */
            string pragma, /* EngineMode */
            string args,
            XmlWriter writer
            )
        {
            CheckDisposed();

            ReturnCode code;
            Result result = null;

            if (interpreter != null)
            {
                object enumValue = Utility.TryParseEnum(
                    typeof(EngineMode), pragma, true, true);

                if (enumValue is EngineMode)
                {
                    EngineMode engineMode = (EngineMode)enumValue;

                    switch (engineMode)
                    {
                        case EngineMode.None:
                            {
                                //
                                // NOTE: Do nothing (null result).
                                //
                                code = ReturnCode.Ok;
                                break;
                            }
                        case EngineMode.EvaluateExpression:
                            {
                                args = Utility.NormalizeLineEndings(args);

                                code = interpreter.EvaluateExpression(
                                    args, ref result);

                                break;
                            }
                        case EngineMode.EvaluateScript:
                            {
                                args = Utility.NormalizeLineEndings(args);

                                code = interpreter.EvaluateScript(
                                    args, ref result);

                                break;
                            }
                        case EngineMode.EvaluateFile:
                            {
                                args = Utility.NormalizeLineEndings(args);

                                code = interpreter.EvaluateFile(
                                    args, ref result);

                                break;
                            }
                        case EngineMode.SubstituteString:
                            {
                                args = Utility.NormalizeLineEndings(args);

                                code = interpreter.SubstituteString(
                                    args, ref result);

                                break;
                            }
                        case EngineMode.SubstituteFile:
                            {
                                args = Utility.NormalizeLineEndings(args);

                                code = interpreter.SubstituteFile(
                                    args, ref result);

                                break;
                            }
                        default:
                            {
                                result = String.Format(
                                    "invalid engine mode {0}",
                                    engineMode);

                                code = ReturnCode.Error;
                                break;
                            }
                    }
                }
                else
                {
                    result = Utility.BadValue(
                        null, "engine mode", pragma,
                        Enum.GetNames(typeof(EngineMode)),
                        null, null);

                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            //
            // NOTE: Did we succeed in processing the argument text?
            //
            if (Utility.IsSuccess(code, exceptions))
            {
                if ((writer != null) &&
                    !String.IsNullOrEmpty(result))
                {
                    writer.WriteRaw(result);
                }

                return true;
            }

            throw new ScriptException(code, result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(Preprocessor).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (interpreter != null)
                    {
                        interpreter.Dispose(); /* throw */
                        interpreter = null;
                    }

                    SetupTraceListeners(false, console, false);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Preprocessor()
        {
            Dispose(false);
        }
        #endregion
    }
}
