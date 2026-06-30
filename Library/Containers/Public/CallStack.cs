/*
 * CallStack.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents the call stack of an Eagle interpreter as a stack
    /// of <see cref="ICallFrame" /> instances.  It extends the generic stack
    /// list container with the ability to free the call frames it contains and
    /// to format itself as a string.
    /// </summary>
    [ObjectId("71f3559d-68e7-47de-9356-916c0c147f63")]
    public sealed class CallStack : StackList<ICallFrame>, IMaybeDisposed, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty call stack with the default capacity.
        /// </summary>
        /// <param name="canFree">
        /// Non-zero if the call frames contained by this call stack may be
        /// freed when this call stack is disposed.
        /// </param>
        public CallStack(
            bool canFree
            )
            : base()
        {
            this.canFree = canFree;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a call stack that initially contains the call frames
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of call frames whose elements are copied into this
        /// call stack.  This parameter may be null.
        /// </param>
        /// <param name="canFree">
        /// Non-zero if the call frames contained by this call stack may be
        /// freed when this call stack is disposed.
        /// </param>
        public CallStack(
            IEnumerable<ICallFrame> collection,
            bool canFree
            )
            : base(collection)
        {
            this.canFree = canFree;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty call stack with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of call frames that this call stack is able to
        /// hold without resizing.
        /// </param>
        /// <param name="canFree">
        /// Non-zero if the call frames contained by this call stack may be
        /// freed when this call stack is disposed.
        /// </param>
        public CallStack(
            int capacity,
            bool canFree
            )
            : base(capacity)
        {
            this.canFree = canFree;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        /// <summary>
        /// Stores a value indicating whether the call frames contained by this
        /// call stack may be freed when this call stack is disposed.
        /// </summary>
        private bool canFree;
        /// <summary>
        /// Gets a value indicating whether the call frames contained by this
        /// call stack may be freed when this call stack is disposed.
        /// </summary>
        public bool CanFree
        {
            get { /* CheckDisposed(); */ return canFree; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method frees every call frame contained by this call stack and
        /// then removes all of them from this call stack.  The special free
        /// semantics are used (instead of disposal) so that the global call
        /// frame is handled correctly.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the global call frame is permitted to be freed; this
        /// should only be non-zero when the interpreter itself is being
        /// disposed.
        /// </param>
        public void Free(
            bool global
            )
        {
            //
            // HACK: *SPECIAL CASE* We cannot dispose the global call frame
            //       unless we are [also] disposing of the interpreter itself;
            //       therefore, use the special Free method here instead of the
            //       Dispose method.  The Free method is guaranteed to do the
            //       right thing with regard to the global call frame (assuming
            //       the "global" parameter is correct).
            //
            foreach (ICallFrame frame in this)
            {
                if (frame == null)
                    continue;

                frame.Free(global);
            }

            //
            // NOTE: Finally, clear all frames from the call stack.
            //
            Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method produces a string representation of this call stack,
        /// optionally limiting the included call frames to those matching the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter which call frames are included in the
        /// resulting string.  This parameter may be null, in which case all
        /// call frames are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of this call stack.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            // CheckDisposed();

            return ParserOps<ICallFrame>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string representation of this call stack that
        /// includes all of its call frames.
        /// </summary>
        /// <returns>
        /// The string representation of this call stack.
        /// </returns>
        public override string ToString()
        {
            // CheckDisposed();

            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this call stack has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this call stack is currently in the
        /// process of being disposed; this property always returns zero for
        /// this call stack.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this call stack has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this call stack has already been
        /// disposed and the engine is configured to throw on use of a disposed
        /// object.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this call stack has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(CallStack));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this call stack.  It
        /// implements the standard dispose pattern.  When freeing is permitted,
        /// the contained call frames are freed; otherwise, they are simply
        /// removed from this call stack.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
        private /* protected virtual */ void Dispose(
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

                    if (canFree)
                        Free(true);
                    else
                        Clear();
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
        /// This method releases all resources held by this call stack and
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
        /// Finalizes this call stack, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~CallStack()
        {
            Dispose(false);
        }
        #endregion
    }
}
