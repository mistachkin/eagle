/*
 * SubstituteFile.cs --
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
    /// This class implements an MSBuild task that performs Eagle command
    /// substitution on the contents of a file, creating and configuring an
    /// interpreter as needed to do so.
    /// </summary>
    [ObjectId("2cd9b48d-a031-4928-a463-89cd800aef25")]
    public sealed class SubstituteFile : Script
    {
        #region Microsoft.Build.Utilities.Task Overrides
        /// <summary>
        /// This method is called by MSBuild to execute the task.  It creates
        /// and configures an interpreter, performs command substitution on the
        /// configured file, and records the outcome.
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
                                code = SubstituteFile(
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
        /// Non-zero if this instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this instance has been disposed
        /// and the interpreter is configured to throw on disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new InterpreterDisposedException(
                    typeof(SubstituteFile));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of the resources used by this instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the public
        /// <c>Dispose</c> method (rather than from the finalizer), in which case
        /// managed resources may also be released.
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
