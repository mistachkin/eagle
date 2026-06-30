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
    /// <summary>
    /// This class provides an enumerator implementation whose enumeration
    /// behavior (advancing, fetching the current element, and resetting) is
    /// driven entirely by evaluating user-supplied scripts within an
    /// interpreter.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements yielded by this enumerator.
    /// </typeparam>
    [ObjectId("d6ddf4d7-4033-4b08-ae65-78bf4ac3f1f2")]
    internal sealed class ScriptEnumerator<T> : IEnumerator<T>
    {
        #region Private Data
        /// <summary>
        /// The interpreter used to evaluate the scripts that implement the
        /// enumeration behavior.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// The script evaluated to advance the enumerator to the next element.
        /// </summary>
        private IScript moveNextScript;

        /// <summary>
        /// The script evaluated to obtain the element at the current position
        /// of the enumerator.
        /// </summary>
        private IScript currentScript;

        /// <summary>
        /// The script evaluated to reset the enumerator to its initial
        /// position.
        /// </summary>
        private IScript resetScript;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class that uses the specified scripts
        /// to implement its enumeration behavior.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate the supplied scripts.
        /// </param>
        /// <param name="moveNextScript">
        /// The script evaluated to advance the enumerator to the next element.
        /// </param>
        /// <param name="currentScript">
        /// The script evaluated to obtain the element at the current position.
        /// </param>
        /// <param name="resetScript">
        /// The script evaluated to reset the enumerator to its initial
        /// position.
        /// </param>
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
        /// <summary>
        /// Gets the element at the current position of the enumerator by
        /// evaluating the current-element script, resolving the resulting
        /// opaque object handle, and returning its underlying value.
        /// </summary>
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
        /// <summary>
        /// This method advances the enumerator to the next element by
        /// evaluating the move-next script and interpreting its result as a
        /// boolean value.
        /// </summary>
        /// <returns>
        /// True if the enumerator was successfully advanced to the next
        /// element; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Gets the element at the current position of the enumerator as a
        /// non-generic object reference.
        /// </summary>
        object IEnumerator.Current
        {
            get { CheckDisposed(); return ((IEnumerator<T>)this).Current; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the enumerator to its initial position, which is
        /// before the first element, by evaluating the reset script.
        /// </summary>
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
        /// <summary>
        /// Non-zero if this object instance has been disposed.
        /// </summary>
        private bool disposed;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method throws an exception if this object instance has been
        /// disposed and the interpreter is configured to throw in that case.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(ScriptEnumerator<T>).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases resources held by this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; otherwise, it is being called from
        /// the finalizer.
        /// </param>
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
        /// <summary>
        /// This method releases all resources held by this object instance and
        /// suppresses finalization.
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
        /// Finalizes this object instance, releasing any unmanaged resources it
        /// holds.
        /// </summary>
        ~ScriptEnumerator()
        {
            Dispose(false);
        }
        #endregion
    }
}
