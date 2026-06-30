/*
 * EvaluateExpression.cs --
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
    /// This class implements an MSBuild task that creates an interpreter,
    /// evaluates a configured Eagle expression within it, and reports the
    /// result.
    /// </summary>
    [ObjectId("b3641ebe-8528-4658-be70-aa0e86bee1f4")]
    public sealed class EvaluateExpression : Script
    {
        #region Microsoft.Build.Utilities.Task Overrides
        /// <summary>
        /// This method executes the task.  It creates an interpreter,
        /// evaluates the configured Eagle expression within it, stores the
        /// result, and logs any error that is encountered.
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
                                code = EvaluateExpression(
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
        /// Stores a value indicating whether this object instance has been
        /// disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this object instance has already
        /// been disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new InterpreterDisposedException(
                    typeof(EvaluateExpression));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this object instance.
        /// It implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose()" /> method (i.e.
        /// deterministically); zero if it is being called from the finalizer.
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
