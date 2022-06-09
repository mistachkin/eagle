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
    [ObjectId("e6aca5bb-218f-47a3-9174-62e39dcebf6c")]
    public abstract class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IWrapper, IDisposable
    {
        #region Protected Constructors
        protected Default(
            long token
            )
        {
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
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
        public abstract bool IsDisposable { get; }

        ///////////////////////////////////////////////////////////////////////
        //
        // NOTE: The default wrapper, in order to provide any functionality,
        //       requires access to the wrapped object.
        //
        public abstract object Object { get; }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(object obj)
        {
            CheckDisposed();

            return (this.Object != null) ?
                this.Object.Equals(obj) : base.Equals(obj);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            CheckDisposed();

            return (this.Object != null) ?
                this.Object.GetHashCode() : base.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            CheckDisposed();

            return (this.Object != null) ?
                this.Object.ToString() : base.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Default()
        {
            Dispose(false);
        }
        #endregion
    }
}
