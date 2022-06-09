/*
 * SubstituteString.cs --
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
    [ObjectId("d876c133-6ce6-4f8f-a963-6d74df187689")]
    public sealed class SubstituteString : Script
    {
        #region Microsoft.Build.Utilities.Task Overrides
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
                                code = SubstituteString(
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
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new InterpreterDisposedException(
                    typeof(SubstituteString));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
