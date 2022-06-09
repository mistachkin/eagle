/*
 * NullEnumerator.cs --
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

namespace Eagle._Components.Private
{
    [ObjectId("005a09b4-f7b8-476f-9f87-aa409f868710")]
    internal sealed class NullEnumerator<T> : IEnumerator<T>
    {
        #region Private Data
        private bool strict;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public NullEnumerator()
            : this(true)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public NullEnumerator(bool strict)
        {
            this.strict = strict;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator<T> Members
        public T Current
        {
            get
            {
                CheckDisposed();

                //
                // NOTE: Technically, we are before the first element (i.e.
                //       our MoveNext does nothing and always returns false).
                //       Therefore, in "strict" mode, we throw an exception
                //       here; otherwise, we simply return null.
                //
                if (strict)
                    throw new InvalidOperationException();
                else
                    return default(T);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator Members
        object IEnumerator.Current
        {
            get { CheckDisposed(); return ((IEnumerator<T>)this).Current; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MoveNext()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public void Reset()
        {
            CheckDisposed();
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
                throw new ObjectDisposedException(
                    typeof(NullEnumerator<T>).Name);
            }
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
        ~NullEnumerator()
        {
            Dispose(false);
        }
        #endregion
    }
}
