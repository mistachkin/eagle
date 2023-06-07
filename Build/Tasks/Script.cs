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
        private static readonly HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.EmbeddedUse;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The object flags to use when calling FixupReturnValue on
        //       the various method parameters passed required by the script
        //       being evaluated to handle formal interface methods.
        //
        private static readonly ObjectFlags DefaultObjectFlags =
            ObjectFlags.Default | ObjectFlags.NoBinder |
            ObjectFlags.NoDispose | ObjectFlags.AddReference;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The object option type to use when calling FixupReturnValue
        //       on the various method parameters passed required by the
        //       script being evaluated to handle formal interface methods.
        //
        private static readonly ObjectOptionType DefaultObjectOptionType =
            ObjectOptionType.Default;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the opaque object handle name that will refer to the
        //       task instance associated with the interpreter being used by
        //       that same task instance.
        //
        public static readonly string DefaultObjectName = "__task";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string codeResultErrorLineAndInfoFormat =
            "{0}, line {2}: {1}" + Environment.NewLine + "{3}";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private string text;
        private IEnumerable<string> args;
        private CreateFlags createFlags;
        private HostCreateFlags hostCreateFlags;
        private EngineFlags engineFlags;
        private SubstitutionFlags substitutionFlags;
        private EventFlags eventFlags;
        private ExpressionFlags expressionFlags;
        private bool exceptions;
        private bool showStackTrace;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Data
        protected ReturnCode code;
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
        [Required()]
        public string Text
        {
            get { CheckDisposed(); return text; }
            set { CheckDisposed(); text = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        /* [Optional()] */
        public CreateFlags CreateFlags
        {
            get { CheckDisposed(); return createFlags; }
            set { CheckDisposed(); createFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public EngineFlags EngineFlags
        {
            get { CheckDisposed(); return engineFlags; }
            set { CheckDisposed(); engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public SubstitutionFlags SubstitutionFlags
        {
            get { CheckDisposed(); return substitutionFlags; }
            set { CheckDisposed(); substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public EventFlags EventFlags 
        {
            get { CheckDisposed(); return eventFlags; }
            set { CheckDisposed(); eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public ExpressionFlags ExpressionFlags
        {
            get { CheckDisposed(); return expressionFlags; }
            set { CheckDisposed(); expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public bool Exceptions
        {
            get { CheckDisposed(); return exceptions; }
            set { CheckDisposed(); exceptions = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public bool ShowStackTrace
        {
            get { CheckDisposed(); return showStackTrace; }
            set { CheckDisposed(); showStackTrace = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Output()]
        public ReturnCode Code
        {
            get { CheckDisposed(); return code; }
            set { CheckDisposed(); code = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        protected virtual Interpreter CreateInterpreter(
            ref Result result
            )
        {
            return Interpreter.Create(
                args, createFlags, hostCreateFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        protected virtual bool IsSuccess(
            ReturnCode code
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Logging Helper Methods (Execute)
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

        protected virtual void MaybeLogErrorFromException(
            Exception exception
            )
        {
            if (exception == null)
                return;

            Log.LogErrorFromException(exception, showStackTrace);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        protected virtual ReturnCode EvaluateExpression(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateExpression(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateScript(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateScript(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateFile(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateFile(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SubstituteString(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.SubstituteString(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(Script).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Script()
        {
            Dispose(false);
        }
        #endregion
    }
}
