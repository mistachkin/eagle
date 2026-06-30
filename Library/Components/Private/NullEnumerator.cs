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
    /// <summary>
    /// This class implements an enumerator that contains no elements.  Its
    /// <see cref="MoveNext" /> method always returns false, so iteration never
    /// yields any items.  It is used as a placeholder where a non-null
    /// enumerator is required but there is nothing to enumerate.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements that would be enumerated.
    /// </typeparam>
    [ObjectId("005a09b4-f7b8-476f-9f87-aa409f868710")]
    internal sealed class NullEnumerator<T> : IEnumerator<T>
    {
        #region Private Data
        /// <summary>
        /// Non-zero if accessing the current element should throw an exception
        /// instead of returning the default value of the element type.
        /// </summary>
        private bool strict;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class in strict mode, delegating to
        /// the more general constructor.
        /// </summary>
        public NullEnumerator()
            : this(true)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="strict">
        /// Non-zero if accessing the current element should throw an exception
        /// instead of returning the default value of the element type.
        /// </param>
        public NullEnumerator(bool strict)
        {
            this.strict = strict;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator<T> Members
        /// <summary>
        /// Gets the element at the current position of the enumerator.  Since
        /// this enumerator is always positioned before the first element, in
        /// strict mode this property throws
        /// <see cref="InvalidOperationException" />; otherwise, it returns the
        /// default value of the element type.
        /// </summary>
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
        /// <summary>
        /// Gets the element at the current position of the enumerator, as a
        /// non-generic object.  This property has the same behavior as the
        /// generic <see cref="Current" /> property.
        /// </summary>
        object IEnumerator.Current
        {
            get { CheckDisposed(); return ((IEnumerator<T>)this).Current; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method advances the enumerator to the next element.  Since this
        /// enumerator contains no elements, it never advances.
        /// </summary>
        /// <returns>
        /// Always false, because there are no elements to enumerate.
        /// </returns>
        public bool MoveNext()
        {
            CheckDisposed();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the enumerator to its initial position, which is
        /// before the first element.
        /// </summary>
        public void Reset()
        {
            CheckDisposed();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws <see cref="ObjectDisposedException" /> if this
        /// object has been disposed and the interpreter is configured to throw
        /// on use of disposed objects.
        /// </summary>
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

        /// <summary>
        /// This method releases the resources used by this object.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
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
        /// This method releases all resources used by this object.
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
        /// Finalizes an instance of this class, releasing any unmanaged
        /// resources.
        /// </summary>
        ~NullEnumerator()
        {
            Dispose(false);
        }
        #endregion
    }
}
