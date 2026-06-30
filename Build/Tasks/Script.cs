/*
 * Script.cs --
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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Tasks
{
    /// <summary>
    /// This class is the abstract base class for the MSBuild tasks provided by
    /// Eagle.  It encapsulates the common state and helper methods needed to
    /// create an interpreter and then evaluate, substitute, or otherwise
    /// process a script, expression, or file as part of an MSBuild build.
    /// </summary>
    [ObjectId("557579fe-436f-461c-9d82-4cec1bc09e97")]
    public abstract class Script : Task, IDisposable
    {
        #region Private Constants
        //
        // NOTE: By default:
        //
        //       1. We want to initialize the script library.
        //       2. We want to throw an exception if disposed objects
        //          are accessed.
        //       3. We want to throw an exception if interpreter
        //          creation fails.
        //       4. We want to have only directories that actually
        //          exist in the auto-path.
        //
        /// <summary>
        /// The default interpreter creation flags used when creating an
        /// interpreter for use by this task.
        /// </summary>
        private static readonly CreateFlags DefaultCreateFlags =
            CreateFlags.EmbeddedUse;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We do not want to change the console title.
        //       2. We do not want to change the console icon.
        //       3. We do not want to intercept the Ctrl-C keypress.
        //
        /// <summary>
        /// The default host creation flags used when creating an interpreter
        /// for use by this task.
        /// </summary>
        private static readonly HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.EmbeddedUse;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The object flags to use when calling FixupReturnValue on
        //       the various method parameters passed required by the script
        //       being evaluated to handle formal interface methods.
        //
        /// <summary>
        /// The object flags used when exposing the various method parameters
        /// required by the script being evaluated, in order to handle formal
        /// interface methods.
        /// </summary>
        private static readonly ObjectFlags DefaultObjectFlags =
            ObjectFlags.Default | ObjectFlags.NoBinder |
            ObjectFlags.NoDispose | ObjectFlags.AddReference;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The object option type to use when calling FixupReturnValue
        //       on the various method parameters passed required by the
        //       script being evaluated to handle formal interface methods.
        //
        /// <summary>
        /// The object option type used when exposing the various method
        /// parameters required by the script being evaluated, in order to
        /// handle formal interface methods.
        /// </summary>
        private static readonly ObjectOptionType DefaultObjectOptionType =
            ObjectOptionType.Default;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the opaque object handle name that will refer to the
        //       task instance associated with the interpreter being used by
        //       that same task instance.
        //
        /// <summary>
        /// The opaque object handle name that refers to the task instance
        /// within the interpreter being used by that same task instance.
        /// </summary>
        public static readonly string DefaultObjectName = "__task";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the error message logged for a
        /// failed script result, incorporating the return code, the result,
        /// the error line, and the error information.
        /// </summary>
        private static readonly string codeResultErrorLineAndInfoFormat =
            "{0}, line {2}: {1}" + Environment.NewLine + "{3}";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The script text, expression, or file name to be evaluated,
        /// substituted, or otherwise processed by this task.
        /// </summary>
        private string text;

        /// <summary>
        /// The collection of arguments made available to the interpreter used
        /// by this task.
        /// </summary>
        private IEnumerable<string> args;

        /// <summary>
        /// The interpreter creation flags used when creating the interpreter
        /// for this task.
        /// </summary>
        private CreateFlags createFlags;

        /// <summary>
        /// The host creation flags used when creating the interpreter for this
        /// task.
        /// </summary>
        private HostCreateFlags hostCreateFlags;

        /// <summary>
        /// The engine flags used when evaluating, substituting, or otherwise
        /// processing the script.
        /// </summary>
        private EngineFlags engineFlags;

        /// <summary>
        /// The substitution flags used when evaluating, substituting, or
        /// otherwise processing the script.
        /// </summary>
        private SubstitutionFlags substitutionFlags;

        /// <summary>
        /// The event flags used when evaluating, substituting, or otherwise
        /// processing the script.
        /// </summary>
        private EventFlags eventFlags;

        /// <summary>
        /// The expression flags used when evaluating, substituting, or
        /// otherwise processing the script.
        /// </summary>
        private ExpressionFlags expressionFlags;

        /// <summary>
        /// When non-zero, "exceptional" (non-Ok) success return codes are
        /// allowed and treated as success.
        /// </summary>
        private bool exceptions;

        /// <summary>
        /// When non-zero, the exception stack trace is included when logging
        /// errors from an exception.
        /// </summary>
        private bool showStackTrace;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Data
        /// <summary>
        /// The return code produced by the most recent operation performed by
        /// this task.
        /// </summary>
        protected ReturnCode code;

        /// <summary>
        /// The result or error message produced by the most recent operation
        /// performed by this task.
        /// </summary>
        protected string result;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        //
        // NOTE: Sets up default values for the properties we use.  The MSBuild 
        //       documentation is not entirely clear about whether or not having 
        //       constructors is allowed; however, it does not appear to forbid 
        //       them.
        //
        /// <summary>
        /// Constructs an instance of this class, setting up the default values
        /// for the properties used by this task.
        /// </summary>
        protected Script()
        {
            //
            // NOTE: Get the effective interpreter creation flags from the
            //       environment, etc.
            //
            createFlags = Interpreter.GetStartupCreateFlags(null,
                DefaultCreateFlags, OptionOriginFlags.Standard, true, true);

            hostCreateFlags = Interpreter.GetStartupHostCreateFlags(null,
                DefaultHostCreateFlags, OptionOriginFlags.Standard, true, true);

            //
            // NOTE: By default, we do not want any special evaluation flags.
            //
            engineFlags = EngineFlags.None;

            //
            // NOTE: By default, we want all the substitution flags.
            //
            substitutionFlags = SubstitutionFlags.Default;

            //
            // NOTE: By default, we want to handle events targeted at the
            //       engine.
            //
            eventFlags = EventFlags.Default;

            //
            // NOTE: By default, we want all the expression flags.
            //
            expressionFlags = ExpressionFlags.Default;

            //
            // NOTE: By default, we do not want to allow "exceptional" (non-Ok) 
            //       success return codes.
            //
            exceptions = false;

            //
            // NOTE: By default, we want to show the exception stack trace.
            //
            showStackTrace = true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Task Parameters
        /// <summary>
        /// Gets or sets the script text, expression, or file name to be
        /// evaluated, substituted, or otherwise processed by this task.
        /// </summary>
        [Required()]
        public string Text
        {
            get { CheckDisposed(); return text; }
            set { CheckDisposed(); text = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the arguments made available to the interpreter used by
        /// this task, in string list form.
        /// </summary>
        /* [Optional()] */
        public string Args
        {
            get
            {
                CheckDisposed();

                if (args == null)
                    return null;

                return new StringList(args).ToString();
            }
            set
            {
                CheckDisposed();

                StringList list;
                Result error = null;

                list = StringList.FromString(value, ref error);

                if (list == null)
                    throw new ScriptException(error);

                args = list;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the interpreter creation flags used when creating the
        /// interpreter for this task.
        /// </summary>
        /* [Optional()] */
        public CreateFlags CreateFlags
        {
            get { CheckDisposed(); return createFlags; }
            set { CheckDisposed(); createFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the interpreter creation flags used when creating the
        /// interpreter for this task, in their string form.
        /// </summary>
        /* [Optional()] */
        public string CreateFlagsString
        {
            get { CheckDisposed(); return createFlags.ToString(); }
            set
            {
                CheckDisposed();

                object enumValue;
                Result error = null;

                enumValue = Utility.TryParseFlagsEnum(
                    null, typeof(CreateFlags),
                    createFlags.ToString(), value,
                    null, true, true, true, ref error);

                if (enumValue == null)
                    throw new ScriptException(error);

                createFlags = (CreateFlags)enumValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the host creation flags used when creating the
        /// interpreter for this task.
        /// </summary>
        /* [Optional()] */
        public HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the host creation flags used when creating the
        /// interpreter for this task, in their string form.
        /// </summary>
        /* [Optional()] */
        public string HostCreateFlagsString
        {
            get { CheckDisposed(); return hostCreateFlags.ToString(); }
            set
            {
                CheckDisposed();

                object enumValue;
                Result error = null;

                enumValue = Utility.TryParseFlagsEnum(
                    null, typeof(HostCreateFlags),
                    hostCreateFlags.ToString(), value,
                    null, true, true, true, ref error);

                if (enumValue == null)
                    throw new ScriptException(error);

                hostCreateFlags = (HostCreateFlags)enumValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the engine flags used when evaluating, substituting, or
        /// otherwise processing the script.
        /// </summary>
        /* [Optional()] */
        public EngineFlags EngineFlags
        {
            get { CheckDisposed(); return engineFlags; }
            set { CheckDisposed(); engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the engine flags used when evaluating, substituting, or
        /// otherwise processing the script, in their string form.
        /// </summary>
        /* [Optional()] */
        public string EngineFlagsString
        {
            get { CheckDisposed(); return engineFlags.ToString(); }
            set
            {
                CheckDisposed();

                object enumValue;
                Result error = null;

                enumValue = Utility.TryParseFlagsEnum(
                    null, typeof(EngineFlags),
                    engineFlags.ToString(), value,
                    null, true, true, true, ref error);

                if (enumValue == null)
                    throw new ScriptException(error);

                engineFlags = (EngineFlags)enumValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the substitution flags used when evaluating,
        /// substituting, or otherwise processing the script.
        /// </summary>
        /* [Optional()] */
        public SubstitutionFlags SubstitutionFlags
        {
            get { CheckDisposed(); return substitutionFlags; }
            set { CheckDisposed(); substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the substitution flags used when evaluating,
        /// substituting, or otherwise processing the script, in their string
        /// form.
        /// </summary>
        /* [Optional()] */
        public string SubstitutionFlagsString
        {
            get { CheckDisposed(); return substitutionFlags.ToString(); }
            set
            {
                CheckDisposed();

                object enumValue;
                Result error = null;

                enumValue = Utility.TryParseFlagsEnum(
                    null, typeof(SubstitutionFlags),
                    substitutionFlags.ToString(), value,
                    null, true, true, true, ref error);

                if (enumValue == null)
                    throw new ScriptException(error);

                substitutionFlags = (SubstitutionFlags)enumValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the event flags used when evaluating, substituting, or
        /// otherwise processing the script.
        /// </summary>
        /* [Optional()] */
        public EventFlags EventFlags 
        {
            get { CheckDisposed(); return eventFlags; }
            set { CheckDisposed(); eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the event flags used when evaluating, substituting, or
        /// otherwise processing the script, in their string form.
        /// </summary>
        /* [Optional()] */
        public string EventFlagsString
        {
            get { CheckDisposed(); return eventFlags.ToString(); }
            set
            {
                CheckDisposed();

                object enumValue;
                Result error = null;

                enumValue = Utility.TryParseFlagsEnum(
                    null, typeof(EventFlags),
                    eventFlags.ToString(), value,
                    null, true, true, true, ref error);

                if (enumValue == null)
                    throw new ScriptException(error);

                eventFlags = (EventFlags)enumValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the expression flags used when evaluating,
        /// substituting, or otherwise processing the script.
        /// </summary>
        /* [Optional()] */
        public ExpressionFlags ExpressionFlags
        {
            get { CheckDisposed(); return expressionFlags; }
            set { CheckDisposed(); expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the expression flags used when evaluating,
        /// substituting, or otherwise processing the script, in their string
        /// form.
        /// </summary>
        /* [Optional()] */
        public string ExpressionFlagsString
        {
            get { CheckDisposed(); return expressionFlags.ToString(); }
            set
            {
                CheckDisposed();

                object enumValue;
                Result error = null;

                enumValue = Utility.TryParseFlagsEnum(
                    null, typeof(ExpressionFlags),
                    expressionFlags.ToString(), value,
                    null, true, true, true, ref error);

                if (enumValue == null)
                    throw new ScriptException(error);

                expressionFlags = (ExpressionFlags)enumValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether "exceptional" (non-Ok)
        /// success return codes are allowed and treated as success.
        /// </summary>
        /* [Optional()] */
        public bool Exceptions
        {
            get { CheckDisposed(); return exceptions; }
            set { CheckDisposed(); exceptions = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the exception stack trace is
        /// included when logging errors from an exception.
        /// </summary>
        /* [Optional()] */
        public bool ShowStackTrace
        {
            get { CheckDisposed(); return showStackTrace; }
            set { CheckDisposed(); showStackTrace = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the return code produced by the most recent operation
        /// performed by this task.
        /// </summary>
        [Output()]
        public ReturnCode Code
        {
            get { CheckDisposed(); return code; }
            set { CheckDisposed(); code = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the result or error message produced by the most recent
        /// operation performed by this task.
        /// </summary>
        [Output()]
        public string Result
        {
            get { CheckDisposed(); return result; }
            set { CheckDisposed(); result = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Helper Methods
        #region Interpreter Creation Helper Methods (Execute)
        /// <summary>
        /// This method creates the interpreter to be used by this task.
        /// </summary>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created interpreter, or null if it could not be created.
        /// </returns>
        protected virtual Interpreter CreateInterpreter(
            ref Result result
            )
        {
            return Interpreter.Create(
                args, createFlags, hostCreateFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the processing required before the interpreter
        /// is created, refreshing the interpreter and host creation flags.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        protected virtual ReturnCode PreCreateInterpreter(
            ref Result error
            )
        {
            try
            {
                createFlags = Interpreter.GetStartupCreateFlags(
                    args, createFlags, OptionOriginFlags.Default,
                    true, true);

                hostCreateFlags = Interpreter.GetStartupHostCreateFlags(
                    args, hostCreateFlags, OptionOriginFlags.Default,
                    true, true);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the processing required after the interpreter
        /// is created, processing the startup options and exposing this task
        /// instance to the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that was created for use by this task.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        protected virtual ReturnCode PostCreateInterpreter(
            Interpreter interpreter,
            ref Result error
            )
        {
            ReturnCode code;
            Result result = null;

            code = Interpreter.ProcessStartupOptions(
                interpreter, args, createFlags,
                OptionOriginFlags.Standard, true, true,
                ref result);

            if (code != ReturnCode.Ok)
            {
                error = result;
                return code;
            }

            code = Utility.FixupReturnValue(
                interpreter, null, DefaultObjectFlags, null,
                null, DefaultObjectOptionType, DefaultObjectName,
                this, true, false, ref result);

            if (code != ReturnCode.Ok)
            {
                error = result;
                return code;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Postcondition Helper Methods (Execute)
        /// <summary>
        /// This method determines whether the specified return code represents
        /// success for this task.
        /// </summary>
        /// <param name="code">
        /// The return code to check.
        /// </param>
        /// <returns>
        /// True if the specified return code represents success; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsSuccess(
            ReturnCode code
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Logging Helper Methods (Execute)
        /// <summary>
        /// This method logs an error for the specified return code and result,
        /// if there is a result to log.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the error.
        /// </param>
        /// <param name="result">
        /// The result containing the error message and metadata to log.  This
        /// parameter may be null, in which case nothing is logged.
        /// </param>
        protected virtual void MaybeLogError(
            ReturnCode code,
            Result result
            )
        {
            if (result == null)
                return;

            Log.LogError(null, result.ErrorCode, null, null, result.ErrorLine,
                0, result.ErrorLine, 0, codeResultErrorLineAndInfoFormat, code,
                result, result.ErrorLine, result.ErrorInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method logs an error based on the specified exception, if there
        /// is an exception to log.
        /// </summary>
        /// <param name="exception">
        /// The exception to log.  This parameter may be null, in which case
        /// nothing is logged.
        /// </param>
        protected virtual void MaybeLogErrorFromException(
            Exception exception
            )
        {
            if (exception == null)
                return;

            Log.LogErrorFromException(exception, showStackTrace);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method logs an error based on the inner exception of the
        /// specified exception, if there is an inner exception to log.
        /// </summary>
        /// <param name="exception">
        /// The exception whose inner exception is to be logged.  This parameter
        /// may be null, as may its inner exception, in which case nothing is
        /// logged.
        /// </param>
        protected virtual void MaybeLogErrorFromInnerException(
            Exception exception
            )
        {
            if (exception == null)
                return;

            Exception innerException = exception.InnerException;

            if (innerException == null)
                return;

            Log.LogErrorFromException(innerException, showStackTrace);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Engine Helper Methods (Execute)
        /// <summary>
        /// This method evaluates the configured text as an expression using the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when evaluating the expression.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value of the expression; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        protected virtual ReturnCode EvaluateExpression(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateExpression(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the configured text as a script using the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when evaluating the script.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the script; upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        protected virtual ReturnCode EvaluateScript(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateScript(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the script contained in the file named by the
        /// configured text using the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when evaluating the file.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the script; upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        protected virtual ReturnCode EvaluateFile(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateFile(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs substitutions on the configured text using the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when performing the substitutions.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the substituted string; upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        protected virtual ReturnCode SubstituteString(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.SubstituteString(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs substitutions on the contents of the file named
        /// by the configured text using the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when performing the substitutions.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the substituted string; upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        protected virtual ReturnCode SubstituteFile(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.SubstituteFile(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// When non-zero, this task instance has been disposed and most of its
        /// members may no longer be used.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this task instance has been
        /// disposed and the engine is configured to throw on access to disposed
        /// objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(Script).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases resources held by this task instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                 //if (disposing)
                 //{
                 //   //////////////////////////////////
                 //    dispose managed resources here...
                 //   //////////////////////////////////
                 //}

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this task instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes an instance of this class, releasing any unmanaged
        /// resources held by it.
        /// </summary>
        ~Script()
        {
            Dispose(false);
        }
        #endregion
    }
}
