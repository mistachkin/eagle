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
    /// <summary>
    /// This class implements a WiX (Windows Installer XML) preprocessor
    /// extension that exposes the Eagle scripting engine to the WiX
    /// preprocessor, allowing variable values to be queried, functions to be
    /// evaluated, and pragmas to be processed via an embedded Eagle
    /// interpreter.
    /// </summary>
    [ObjectId("dc8dd503-3594-4058-bf24-f9e5e4a084a5")]
    internal sealed class Preprocessor : PreprocessorExtension, IDisposable
    {
        #region Private Constants
        /// <summary>
        /// The variable and function name prefixes recognized by this
        /// extension, derived from the Eagle script file extension.
        /// </summary>
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
        /// <summary>
        /// The default assumption about whether a console-like host is
        /// available for diagnostic output.
        /// </summary>
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
        /// <summary>
        /// The default interpreter creation flags used when creating the
        /// embedded Eagle interpreter for this extension.
        /// </summary>
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
        /// <summary>
        /// The default host creation flags used when creating the embedded
        /// Eagle interpreter for this extension.
        /// </summary>
        private static readonly HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.EmbeddedUse;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default, consider all standard option sources.
        //
        /// <summary>
        /// The default set of option sources considered when processing the
        /// startup options for the embedded Eagle interpreter.
        /// </summary>
        private static readonly OptionOriginFlags DefaultOptionOriginFlags =
            OptionOriginFlags.Standard;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The trace listener installed by this extension to capture trace and
        /// debug output, if any.
        /// </summary>
        private TraceListener listener;

        /// <summary>
        /// The effective interpreter creation flags used to create the
        /// embedded Eagle interpreter.
        /// </summary>
        private CreateFlags createFlags;

        /// <summary>
        /// The effective host creation flags used to create the embedded Eagle
        /// interpreter.
        /// </summary>
        private HostCreateFlags hostCreateFlags;

        /// <summary>
        /// The embedded Eagle interpreter used to evaluate variables,
        /// functions, and pragmas for the WiX preprocessor.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// When true, a console-like host is assumed to be available for
        /// diagnostic output.
        /// </summary>
        private bool console;

        /// <summary>
        /// When true, "exceptional" (non-Ok) success return codes are treated
        /// as successful.
        /// </summary>
        private bool exceptions;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the WiX preprocessor extension, computing
        /// the effective interpreter and host creation flags from the
        /// environment and the configured defaults.
        /// </summary>
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
        /// <summary>
        /// This method attempts to determine whether a console-like host is
        /// available for diagnostic output, using the relevant environment
        /// variables to refine the supplied default assumption.
        /// </summary>
        /// <param name="default">
        /// The default assumption about console availability to use when the
        /// environment does not indicate otherwise.
        /// </param>
        /// <returns>
        /// True if a console-like host should be assumed available; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method adds or removes the trace listener used to capture
        /// trace and debug output, discarding any error message that may be
        /// produced.
        /// </summary>
        /// <param name="setup">
        /// Non-zero to add the trace listener; zero to remove and dispose it.
        /// </param>
        /// <param name="console">
        /// Non-zero if a console-like host is available, which influences the
        /// type of trace listener that is created.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a redundant setup or removal request as an error
        /// instead of a no-op.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method adds or removes the trace listener used to capture
        /// trace and debug output.
        /// </summary>
        /// <param name="setup">
        /// Non-zero to add the trace listener; zero to remove and dispose it.
        /// </param>
        /// <param name="console">
        /// Non-zero if a console-like host is available, which influences the
        /// type of trace listener that is created.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a redundant setup or removal request as an error
        /// instead of a no-op.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// Gets the variable and function name prefixes recognized by this
        /// preprocessor extension.
        /// </summary>
        public override string[] Prefixes
        {
            get
            {
                CheckDisposed();

                return (string[])prefixes.Clone();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes preprocessing by creating the embedded
        /// Eagle interpreter and processing its startup options.  It throws a
        /// <see cref="ScriptException" /> if the interpreter cannot be created
        /// or its startup options cannot be processed.
        /// </summary>
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
        /// <summary>
        /// This method finalizes preprocessing by disposing this extension and
        /// its embedded Eagle interpreter.
        /// </summary>
        public override void FinalizePreprocess()
        {
            CheckDisposed(); /* throw */
            Dispose(); /* throw */
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the value of the named preprocessor variable by
        /// evaluating it against the embedded Eagle interpreter.  It throws a
        /// <see cref="ScriptException" /> if the variable value cannot be
        /// obtained.
        /// </summary>
        /// <param name="prefix">
        /// The variable name prefix.  This parameter is ignored and is always
        /// assumed to be "eagle".
        /// </param>
        /// <param name="name">
        /// The name of the variable whose value is to be returned.
        /// </param>
        /// <returns>
        /// The string value of the named variable.
        /// </returns>
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

        /// <summary>
        /// This method evaluates the named preprocessor function by invoking it
        /// against the embedded Eagle interpreter.  It throws a
        /// <see cref="ScriptException" /> if the function cannot be invoked
        /// successfully.
        /// </summary>
        /// <param name="prefix">
        /// The function name prefix.  This parameter is ignored and is always
        /// assumed to be "eagle".
        /// </param>
        /// <param name="function">
        /// The name of the function (command or procedure) to invoke.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the function.
        /// </param>
        /// <returns>
        /// The string result produced by invoking the function.
        /// </returns>
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
        /// <summary>
        /// This method processes a preprocessor pragma by interpreting it as an
        /// <see cref="EngineMode" /> and evaluating or substituting the
        /// supplied argument text against the embedded Eagle interpreter,
        /// writing any result to the supplied writer.  It throws a
        /// <see cref="ScriptException" /> if the argument text cannot be
        /// processed successfully.
        /// </summary>
        /// <param name="sourceLineNumbers">
        /// The source line number information associated with the pragma.
        /// </param>
        /// <param name="prefix">
        /// The pragma name prefix.  This parameter is ignored and is always
        /// assumed to be "eagle".
        /// </param>
        /// <param name="pragma">
        /// The pragma name, interpreted as an <see cref="EngineMode" /> value
        /// that selects how the argument text is processed.
        /// </param>
        /// <param name="args">
        /// The argument text to be evaluated or substituted.
        /// </param>
        /// <param name="writer">
        /// The XML writer that any result is written to.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the pragma was recognized and processed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Non-zero if this object instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an <see cref="ObjectDisposedException" /> if this
        /// object instance has been disposed and the interpreter is configured
        /// to throw on access to disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(Preprocessor).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this object instance,
        /// including the embedded Eagle interpreter and any installed trace
        /// listeners.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
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
        /// <summary>
        /// This method releases all resources used by this object instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this object instance, releasing any resources that were
        /// not already released by an explicit call to the
        /// <see cref="Dispose()" /> method.
        /// </summary>
        ~Preprocessor()
        {
            Dispose(false);
        }
        #endregion
    }
}
