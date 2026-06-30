/*
 * Default.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class provides the default base implementation for the
    /// <see cref="IWrapper" /> interface, which is used to wrap an underlying
    /// object so it can participate in the interpreter as an identifiable,
    /// token-bearing entity.  Concrete sub-classes supply the wrapped object
    /// and indicate whether it represents a disposable resource.
    /// </summary>
    [ObjectId("e6aca5bb-218f-47a3-9174-62e39dcebf6c")]
    public abstract class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IWrapper
    {
        #region Protected Constructors
        /// <summary>
        /// Constructs an instance of this wrapper base class.
        /// </summary>
        protected Default()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The unique token that identifies this wrapper within the
        /// interpreter.
        /// </summary>
        private long token;

        /// <summary>
        /// Gets or sets the unique token that identifies this wrapper within
        /// the interpreter.
        /// </summary>
        public virtual long Token
        {
            get { CheckDisposed(); return token; }
            set { CheckDisposed(); token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        //
        // NOTE: The default wrapper, in order to provide proper support for
        //       the IDisposable interface, needs to know if the wrapped object
        //       (in the sub-class) actually represents an IDisposable resource.
        //       By default, we could try to implement this member by checking
        //       if the wrapped object can be cast to an IDisposable; however,
        //       that would require using the virtual Object property, which
        //       could throw an ObjectDisposedException, which would defeat the
        //       ability to call this method from within the CheckDisposed
        //       method.
        //
        /// <summary>
        /// Gets a value indicating whether the object wrapped by this instance
        /// represents a resource that requires disposal.
        /// </summary>
        public abstract bool IsDisposable { get; }

        ///////////////////////////////////////////////////////////////////////
        //
        // NOTE: The default wrapper, in order to provide any functionality,
        //       requires access to the wrapped object.
        //
        /// <summary>
        /// Gets or sets the underlying object wrapped by this instance.
        /// </summary>
        public abstract object Object { get; set; }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the wrapped object is equal to the
        /// specified object.  When there is no wrapped object, the base class
        /// implementation is used instead.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the wrapped object.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// True if the wrapped object is equal to <paramref name="obj" />;
        /// otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            CheckDisposed();

            return (this.Object != null) ?
                this.Object.Equals(obj) : base.Equals(obj);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for the wrapped object.  When there
        /// is no wrapped object, the base class implementation is used instead.
        /// </summary>
        /// <returns>
        /// A hash code for the wrapped object.
        /// </returns>
        public override int GetHashCode()
        {
            CheckDisposed();

            return (this.Object != null) ?
                this.Object.GetHashCode() : base.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of the wrapped object.
        /// When there is no wrapped object, the base class implementation is
        /// used instead.
        /// </summary>
        /// <returns>
        /// A string representation of the wrapped object.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return (this.Object != null) ?
                this.Object.ToString() : base.ToString();
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
        /// and the wrapped object actually represents a disposable resource;
        /// otherwise, it does nothing.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            //
            // NOTE: *SPECIAL* This is a special case.  This class implements
            //       the IDisposable interface just in case the wrapped object
            //       (within the sub-class) requires its associated semantics;
            //       therefore, if the wrapped object itself does not actually
            //       represent an IDisposable resource (i.e. according to the
            //       sub-class, which should know better than us), skip raising
            //       any exceptions here if this instance has already been
            //       disposed, since in that case it's a useless "empty shell".
            //
            if (disposed && this.IsDisposable &&
                Engine.IsThrowOnDisposed(null, false))
            {
                throw new InterpreterDisposedException(typeof(Default));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this instance.  When the
        /// wrapped object implements <see cref="IDisposable" />, it is disposed
        /// as well.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        protected virtual void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    object @object = this.Object;

                    if (@object != null)
                    {
                        IDisposable disposable = @object as IDisposable;

                        if (disposable != null)
                        {
                            disposable.Dispose(); /* throw */
                            disposable = null;
                        }

                        @object = null;
                    }
                }

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
        /// This method releases all resources used by this instance and
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
        /// Finalizes an instance of this wrapper class, releasing any
        /// unmanaged resources.
        /// </summary>
        ~Default()
        {
            Dispose(false);
        }
        #endregion
    }
}
