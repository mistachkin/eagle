/*
 * ScriptEnumerator.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("d6ddf4d7-4033-4b08-ae65-78bf4ac3f1f2")]
    internal sealed class ScriptEnumerator<T> : IEnumerator<T>
    {
        #region Private Data
        private Interpreter interpreter;
        private IScript moveNextScript;
        private IScript currentScript;
        private IScript resetScript;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ScriptEnumerator(
            Interpreter interpreter,
            IScript moveNextScript,
            IScript currentScript,
            IScript resetScript
            )
        {
            this.interpreter = interpreter;
            this.moveNextScript = moveNextScript;
            this.currentScript = currentScript;
            this.resetScript = resetScript;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator<T> Members
        public T Current
        {
            get
            {
                CheckDisposed();

                if (interpreter == null)
                    throw new ScriptException("invalid interpreter");

                ReturnCode code;
                Result result = null;

                code = interpreter.EvaluateScript(currentScript, ref result);

                if (!ResultOps.IsOkOrReturn(code))
                    throw new ScriptException(code, result);

                IObject @object = null;

                code = interpreter.GetObject(
                    result, LookupFlags.Default, ref @object, ref result);

                if (code != ReturnCode.Ok)
                    throw new ScriptException(code, result);

                return (T)@object.Value;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator Members
        public bool MoveNext()
        {
            CheckDisposed();

            if (interpreter == null)
                throw new ScriptException("invalid interpreter");

            ReturnCode code;
            Result result = null;

            code = interpreter.EvaluateScript(moveNextScript, ref result);

            if (!ResultOps.IsOkOrReturn(code))
                throw new ScriptException(code, result);

            bool value = false;

            code = Value.GetBoolean2(
                result, ValueFlags.AnyBoolean, interpreter.InternalCultureInfo,
                ref value, ref result);

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, result);

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        object IEnumerator.Current
        {
            get { CheckDisposed(); return ((IEnumerator<T>)this).Current; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Reset()
        {
            CheckDisposed();

            if (interpreter == null)
                throw new ScriptException("invalid interpreter");

            ReturnCode code;
            Result result = null;

            code = interpreter.EvaluateScript(resetScript, ref result);

            if (!ResultOps.IsOkOrReturn(code))
                throw new ScriptException(code, result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(ScriptEnumerator<T>).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //
                //}

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
        ~ScriptEnumerator()
        {
            Dispose(false);
        }
        #endregion
    }
}
