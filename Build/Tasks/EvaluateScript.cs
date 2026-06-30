/*
 * EvaluateScript.cs --
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

namespace Eagle._Tasks
{
    /// <summary>
    /// This class implements an MSBuild task that creates an interpreter and
    /// evaluates a script within it, logging any errors that occur during the
    /// process.
    /// </summary>
    [ObjectId("939fefef-4352-4d02-b86f-01df06770435")]
    public sealed class EvaluateScript : Script
    {
        #region Microsoft.Build.Utilities.Task Overrides
        /// <summary>
        /// This method executes the task.  It creates an interpreter, evaluates
        /// the configured script within it, and logs any errors encountered.
        /// </summary>
        /// <returns>
        /// True if the task succeeded and no errors were logged; otherwise,
        /// false.
        /// </returns>
        public override bool Execute()
        {
            CheckDisposed();

            Result localResult = null;

            try
            {
                code = PreCreateInterpreter(ref localResult);

                if (code == ReturnCode.Ok)
                {
                    using (Interpreter interpreter = CreateInterpreter(
                            ref localResult))
                    {
                        if (interpreter != null)
                        {
                            code = PostCreateInterpreter(
                                interpreter, ref localResult);

                            if (code == ReturnCode.Ok)
                            {
                                code = EvaluateScript(
                                    interpreter, ref localResult);
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }

                        if (!IsSuccess(code))
                            MaybeLogError(code, localResult);
                    }
                }
            }
            catch (Exception e)
            {
                localResult = e;
                code = ReturnCode.Error;

                MaybeLogErrorFromInnerException(e);
                MaybeLogErrorFromException(e);
            }

            result = localResult;
            return IsSuccess(code) && !Log.HasLoggedErrors;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this task has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this task has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new InterpreterDisposedException(
                    typeof(EvaluateScript));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this task.  It implements
        /// the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from <see cref="IDisposable.Dispose" />
        /// (i.e. deterministically); zero if it is being called from the
        /// finalizer.  When non-zero, managed resources are released.
        /// </param>
        protected override void Dispose(
            bool disposing
            )
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
