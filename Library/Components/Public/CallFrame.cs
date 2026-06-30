/*
 * CallFrame.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using VariablePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IVariable>;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a single call frame on an Eagle interpreter's
    /// call stack -- the execution context for a procedure body, a script
    /// scope, or the global level.  A call frame holds the frame's identity
    /// and stack level, the variables visible at that level, the executable
    /// entity and arguments being evaluated, and assorted engine, resolver,
    /// and client data.  Eagle's variable scoping (as seen by <c>upvar</c>,
    /// <c>uplevel</c>, <c>global</c>, and <c>variable</c>) is expressed in
    /// terms of call frames.  It implements <see cref="ICallFrame" /> and is
    /// disposable; disposing a frame releases the variables it owns.  See
    /// <c>core_language.md</c> for scoping semantics.
    /// </summary>
    [ObjectId("af168784-9b42-40cd-87a6-18eb4c3a663f")]
    public sealed class CallFrame :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ICallFrame, IDisposable /* optional */
    {
        #region Private Constructors
        /// <summary>
        /// Constructs a call frame from the fully specified set of identity,
        /// scoping, data, and variable parameters.  This is the most general
        /// constructor; the other constructor overloads delegate to it.
        /// </summary>
        /// <param name="frameId">
        /// The unique identifier of this call frame.
        /// </param>
        /// <param name="frameLevel">
        /// The absolute level of this call frame within the call stack.
        /// </param>
        /// <param name="name">
        /// The name of this call frame.  This parameter may be null.
        /// </param>
        /// <param name="tags">
        /// The optional collection of tags to associate with this call frame;
        /// the collection is copied.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The index of this call frame.
        /// </param>
        /// <param name="level">
        /// The relative level of this call frame.
        /// </param>
        /// <param name="flags">
        /// The flags controlling this call frame's behavior.
        /// </param>
        /// <param name="engineData">
        /// The engine-specific client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="auxiliaryData">
        /// The auxiliary client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="resolveData">
        /// The resolver client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="extraData">
        /// The extra client data for this call frame, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="variables">
        /// The variable collection to use or copy for this call frame, subject
        /// to <paramref name="newVariables" />.  This parameter may be null.
        /// </param>
        /// <param name="execute">
        /// The executable entity associated with this call frame (for example,
        /// the procedure being invoked), if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list associated with this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ownArguments">
        /// Non-zero if this call frame takes ownership of
        /// <paramref name="arguments" /> (and is responsible for disposing it).
        /// </param>
        /// <param name="newVariables">
        /// Non-zero to allocate a new variable collection for this call frame
        /// (copying <paramref name="variables" /> when supplied); zero to use
        /// the supplied collection directly.
        /// </param>
        internal CallFrame(
            long frameId,                 /* in */
            long frameLevel,              /* in */
            string name,                  /* in */
            ObjectDictionary tags,        /* in */
            long index,                   /* in */
            long level,                   /* in */
            CallFrameFlags flags,         /* in */
            IClientData engineData,       /* in */
            IClientData auxiliaryData,    /* in */
            IClientData resolveData,      /* in */
            IClientData extraData,        /* in */
            VariableDictionary variables, /* in */
            IExecute execute,             /* in */
            ArgumentList arguments,       /* in */
            bool ownArguments,            /* in */
            bool newVariables             /* in */
            )
        {
            this.kind = IdentifierKind.CallFrame;
            this.id = Guid.Empty;
            this.frameId = frameId;
            this.frameLevel = frameLevel;
            this.name = name;
            this.index = index;
            this.level = level;
            this.flags = flags;
            this.other = null;
            this.previous = null;
            this.next = null;
            this.execute = execute;
            this.arguments = arguments;
            this.ownArguments = ownArguments;
            this.procedureArguments = null;
            this.engineData = engineData;
            this.auxiliaryData = auxiliaryData;
            this.resolveData = resolveData;
            this.extraData = extraData;
            this.threadId = null;

            //
            // NOTE: Copy the list of tags specified by the caller -OR- use no
            //       tags for now.
            //
            this.tags = (tags != null) ?
                new ObjectDictionary((IDictionary<string, object>)tags) :
                null;

            //
            // NOTE: If they requested variables for this call frame, allocate
            //       a new collection for them now; otherwise, use the provided
            //       variable collection, if any.
            //
            if (newVariables)
            {
                this.variables = (variables != null) ?
                    new VariableDictionary(variables) :
                    new VariableDictionary();
            }
            else
            {
                this.variables = variables;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a call frame, allocating a new variable collection for
        /// it.  This constructor delegates to the primary constructor.
        /// </summary>
        /// <param name="frameId">
        /// The unique identifier of this call frame.
        /// </param>
        /// <param name="frameLevel">
        /// The absolute level of this call frame within the call stack.
        /// </param>
        /// <param name="name">
        /// The name of this call frame.  This parameter may be null.
        /// </param>
        /// <param name="tags">
        /// The optional collection of tags to associate with this call frame;
        /// the collection is copied.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The index of this call frame.
        /// </param>
        /// <param name="level">
        /// The relative level of this call frame.
        /// </param>
        /// <param name="flags">
        /// The flags controlling this call frame's behavior.
        /// </param>
        /// <param name="auxiliaryData">
        /// The auxiliary client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="resolveData">
        /// The resolver client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="extraData">
        /// The extra client data for this call frame, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="execute">
        /// The executable entity associated with this call frame (for example,
        /// the procedure being invoked), if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list associated with this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ownArguments">
        /// Non-zero if this call frame takes ownership of
        /// <paramref name="arguments" /> (and is responsible for disposing it).
        /// </param>
        internal CallFrame(
            long frameId,
            long frameLevel,
            string name,
            ObjectDictionary tags,
            int index,
            int level,
            CallFrameFlags flags,
            IClientData auxiliaryData,
            IClientData resolveData,
            IClientData extraData,
            IExecute execute,
            ArgumentList arguments,
            bool ownArguments
            )
            : this(frameId, frameLevel, name, tags, index, level, flags,
                   null, auxiliaryData, resolveData, null, null, execute,
                   arguments, ownArguments, true)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a call frame that uses the supplied variable collection
        /// directly.  This constructor delegates to the primary constructor.
        /// </summary>
        /// <param name="frameId">
        /// The unique identifier of this call frame.
        /// </param>
        /// <param name="frameLevel">
        /// The absolute level of this call frame within the call stack.
        /// </param>
        /// <param name="name">
        /// The name of this call frame.  This parameter may be null.
        /// </param>
        /// <param name="tags">
        /// The optional collection of tags to associate with this call frame;
        /// the collection is copied.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The index of this call frame.
        /// </param>
        /// <param name="level">
        /// The relative level of this call frame.
        /// </param>
        /// <param name="flags">
        /// The flags controlling this call frame's behavior.
        /// </param>
        /// <param name="auxiliaryData">
        /// The auxiliary client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="resolveData">
        /// The resolver client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="extraData">
        /// The extra client data for this call frame, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="variables">
        /// The variable collection to use directly for this call frame.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list associated with this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ownArguments">
        /// Non-zero if this call frame takes ownership of
        /// <paramref name="arguments" /> (and is responsible for disposing it).
        /// </param>
        internal CallFrame(
            long frameId,
            long frameLevel,
            string name,
            ObjectDictionary tags,
            int index,
            int level,
            CallFrameFlags flags,
            IClientData auxiliaryData,
            IClientData resolveData,
            IClientData extraData,
            VariableDictionary variables,
            ArgumentList arguments,
            bool ownArguments
            )
            : this(frameId, frameLevel, name, tags, index, level, flags,
                   null, auxiliaryData, resolveData, null, variables, null,
                   arguments, ownArguments, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a call frame that shares the variables of another call
        /// frame and is linked to the specified related frames.  This
        /// constructor delegates to the primary constructor.
        /// </summary>
        /// <param name="frameId">
        /// The unique identifier of this call frame.
        /// </param>
        /// <param name="frameLevel">
        /// The absolute level of this call frame within the call stack.
        /// </param>
        /// <param name="name">
        /// The name of this call frame.  This parameter may be null.
        /// </param>
        /// <param name="tags">
        /// The optional collection of tags to associate with this call frame;
        /// the collection is copied.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The index of this call frame.
        /// </param>
        /// <param name="level">
        /// The relative level of this call frame.
        /// </param>
        /// <param name="flags">
        /// The flags controlling this call frame's behavior.
        /// </param>
        /// <param name="auxiliaryData">
        /// The auxiliary client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="resolveData">
        /// The resolver client data for this call frame, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="extraData">
        /// The extra client data for this call frame, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="other">
        /// The other call frame whose variables are shared with this call
        /// frame.  This parameter may be null.
        /// </param>
        /// <param name="previous">
        /// The previous call frame linked to this call frame.  This parameter
        /// may be null.
        /// </param>
        /// <param name="next">
        /// The next call frame linked to this call frame.  This parameter may
        /// be null.
        /// </param>
        internal CallFrame(
            long frameId,
            long frameLevel,
            string name,
            ObjectDictionary tags,
            int index,
            int level,
            CallFrameFlags flags,
            IClientData auxiliaryData,
            IClientData resolveData,
            IClientData extraData,
            ICallFrame other,
            ICallFrame previous,
            ICallFrame next
            )
            : this(frameId, frameLevel, name, tags, index, level, flags,
                   null, auxiliaryData, resolveData, null, null, null,
                   null, false, false)
        {
            //
            // NOTE: Share the variables of this call frame with the original
            //       one.
            //
            this.other = other;
            this.previous = previous;
            this.next = next;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method determines whether this call frame is currently locked
        /// by the calling thread.  The interpreter lock must already be held
        /// by the caller.
        /// </summary>
        /// <returns>
        /// Non-zero if this call frame is locked by the current thread;
        /// otherwise, zero.
        /// </returns>
        private bool IsLockedByThisThread()
        {
            //
            // HACK: This method purposely does not care about the
            //       undefined flag.  Generally, a variable cannot
            //       be locked while undefined; however, we do not
            //       enforce that here.
            //
            long? localMaybeThreadId = this.threadId;

            if (localMaybeThreadId == null)
                return false;

            long localThreadId = (long)localMaybeThreadId;

            if (localThreadId != GlobalState.GetCurrentSystemThreadId())
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method determines whether this call frame is currently locked
        /// by a thread other than the calling thread.  The interpreter lock
        /// must already be held by the caller.
        /// </summary>
        /// <param name="threadId">
        /// Upon return, this contains the identifier of the thread that has
        /// locked this call frame, or null if it is not locked.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame is locked by a thread other than the
        /// current thread; otherwise, zero.
        /// </returns>
        internal bool IsLockedByOtherThread(
            ref long? threadId
            )
        {
            //
            // HACK: This method purposely does not care about the
            //       undefined flag.  Generally, a variable cannot
            //       be locked while undefined; however, we do not
            //       enforce that here.
            //
            long? localMaybeThreadId = this.threadId;

            if (localMaybeThreadId == null)
            {
                threadId = null;
                return false;
            }

            long localThreadId = (long)localMaybeThreadId;

            if (localThreadId == GlobalState.GetCurrentSystemThreadId())
            {
                threadId = localThreadId;
                return false;
            }

            threadId = localThreadId;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to unlock this call frame on behalf of the
        /// calling thread.  The interpreter lock must already be held by the
        /// caller.
        /// </summary>
        /// <param name="errorOnUnlocked">
        /// Non-zero to treat an already-unlocked call frame as an error; zero
        /// to treat it as success.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame was unlocked (or was already unlocked
        /// and <paramref name="errorOnUnlocked" /> is zero); otherwise, zero.
        /// </returns>
        private bool PrivateUnlock(
            bool errorOnUnlocked,
            ref Result error
            )
        {
            //
            // HACK: This method does care about the undefined flag.
            //       If a call frame is undefined, unlocking it cannot
            //       fail when it is already unlocked.
            //
            long? localMaybeThreadId = threadId;

            if (localMaybeThreadId == null)
            {
                if (HasFlags(CallFrameFlags.Undefined, true))
                {
                    //
                    // HACK: The call frame is now (?) dead;
                    //       therefore, permit unlocking.
                    //
                    return true;
                }
                else
                {
                    //
                    // NOTE: It is possible that another
                    //       thread destroyed the call
                    //       frame and then recreated it
                    //       (i.e. it is actually a different
                    //       call frame now, technically).
                    //
                    if (errorOnUnlocked)
                    {
                        error = "call frame already unlocked";
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            long localThreadId = (long)localMaybeThreadId;

            if (localThreadId != GlobalState.GetCurrentSystemThreadId())
            {
                error = String.Format(
                    "call frame locked by other thread {0}",
                    FormatOps.WrapOrNull(localThreadId));

                return false;
            }

            threadId = null;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this call frame.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this call frame.
        /// </summary>
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this call frame.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this call frame.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this call frame.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this call frame.
        /// </summary>
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this call frame.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this call frame.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this call frame.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this call frame.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this call frame.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this call frame.
        /// </summary>
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this call frame has been disposed.
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
        /// Gets a value indicating whether this call frame is currently in the
        /// process of being disposed; this property always returns zero for
        /// this call frame.
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

        #region IThreadLock Members
        /// <summary>
        /// Stores the identifier of the thread that currently holds the lock on
        /// this call frame, or null when it is not locked.
        /// </summary>
        private long? threadId;
        /// <summary>
        /// Gets or sets the identifier of the thread that currently holds the
        /// lock on this call frame, or null when it is not locked.  This
        /// property is really for external use only; it should not be used to
        /// actually set the associated value, except under a few very rare sets
        /// of circumstances.
        /// </summary>
        public long? ThreadId
        {
            get { CheckDisposed(); return threadId; }
            set { CheckDisposed(); threadId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method determines whether this call frame is currently locked
        /// by the calling thread.  The interpreter lock must already be held by
        /// the caller.
        /// </summary>
        /// <returns>
        /// Non-zero if this call frame is locked by the current thread;
        /// otherwise, zero.
        /// </returns>
        public bool IsLocked()
        {
            CheckDisposed();

            return IsLockedByThisThread();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method attempts to lock this call frame for exclusive use by
        /// the calling thread.  The interpreter lock must already be held by
        /// the caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame was successfully locked by the current
        /// thread; otherwise, zero (for example, if it is already locked).
        /// </returns>
        public bool Lock(
            ref Result error
            )
        {
            CheckDisposed();

            //
            // HACK: This method purposely does not care about the
            //       undefined flag.  Generally, a call frame cannot
            //       be locked while undefined; however, we do not
            //       enforce that here.
            //
            long? localMaybeThreadId = threadId;

            if (localMaybeThreadId != null)
            {
                error = String.Format(
                    "call frame already locked by thread {0}",
                    FormatOps.WrapOrNull(localMaybeThreadId));

                return false;
            }

            threadId = GlobalState.GetCurrentSystemThreadId();
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method attempts to unlock this call frame, treating an
        /// already-unlocked call frame as an error.  The interpreter lock must
        /// already be held by the caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame was successfully unlocked; otherwise,
        /// zero.
        /// </returns>
        public bool Unlock(
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateUnlock(true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method attempts to unlock this call frame, treating an
        /// already-unlocked call frame as success rather than an error.  The
        /// interpreter lock must already be held by the caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame was unlocked (or was already unlocked);
        /// otherwise, zero.
        /// </returns>
        public bool MaybeUnlock(
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateUnlock(false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        // TODO: In the future, perhaps add other sanity checks here, e.g. a
        //       disposed IVariable cannot be used?
        //
        /// <summary>
        /// This method determines whether this call frame is currently usable
        /// by the calling thread.  The interpreter lock must already be held by
        /// the caller.
        /// </summary>
        /// <returns>
        /// Non-zero if this call frame is usable by the current thread (i.e. it
        /// is not locked by another thread); otherwise, zero.
        /// </returns>
        public bool IsUsable()
        {
            CheckDisposed();

            Result error = null;

            return IsUsable(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        // TODO: In the future, perhaps add other sanity checks here, e.g. a
        //       disposed IVariable cannot be used?
        //
        /// <summary>
        /// This method determines whether this call frame is currently usable
        /// by the calling thread, reporting why it is not when applicable.  The
        /// interpreter lock must already be held by the caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame is usable by the current thread (i.e. it
        /// is not locked by another thread); otherwise, zero.
        /// </returns>
        public bool IsUsable(
            ref Result error
            )
        {
            CheckDisposed();

            //
            // HACK: This method purposely does not care about the
            //       undefined flag.  Generally, a variable cannot
            //       be locked while undefined; however, we do not
            //       enforce that here.
            //
            long? localMaybeThreadId = null;

            if (!IsLockedByOtherThread(ref localMaybeThreadId))
                return true;

            error = String.Format(
                "call frame locked by other thread {0}",
                FormatOps.WrapOrNull(localMaybeThreadId));

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICallFrame Members
        /// <summary>
        /// Stores the unique identifier of this call frame.
        /// </summary>
        private long frameId;
        /// <summary>
        /// Gets or sets the unique identifier of this call frame.
        /// </summary>
        public long FrameId
        {
            get { CheckDisposed(); return frameId; }
            set { CheckDisposed(); frameId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the absolute level of this call frame within the call stack.
        /// </summary>
        private long frameLevel;
        /// <summary>
        /// Gets or sets the absolute level of this call frame within the call
        /// stack.
        /// </summary>
        public long FrameLevel
        {
            get { CheckDisposed(); return frameLevel; }
            set { CheckDisposed(); frameLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling this call frame's behavior.
        /// </summary>
        private CallFrameFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling this call frame's behavior.
        /// </summary>
        public CallFrameFlags Flags
        {
            get { CheckDisposed(); return flags; }
            set { CheckDisposed(); flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of tags associated with this call frame.
        /// </summary>
        private ObjectDictionary tags;
        /// <summary>
        /// Gets or sets the collection of tags associated with this call frame.
        /// </summary>
        public ObjectDictionary Tags
        {
            get { CheckDisposed(); return tags; }
            set { CheckDisposed(); tags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the index of this call frame.
        /// </summary>
        private long index;
        /// <summary>
        /// Gets or sets the index of this call frame.
        /// </summary>
        public long Index
        {
            get { CheckDisposed(); return index; }
            set { CheckDisposed(); index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the relative level of this call frame.
        /// </summary>
        private long level;
        /// <summary>
        /// Gets or sets the relative level of this call frame.
        /// </summary>
        public long Level
        {
            get { CheckDisposed(); return level; }
            set { CheckDisposed(); level = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the executable entity associated with this call frame.
        /// </summary>
        private IExecute execute;
        /// <summary>
        /// Gets or sets the executable entity associated with this call frame.
        /// </summary>
        public IExecute Execute
        {
            get { CheckDisposed(); return execute; }
            set { CheckDisposed(); execute = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the argument list associated with this call frame.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the argument list associated with this call frame.
        /// When this call frame is linked to a next call frame, the value is
        /// obtained from or stored on that frame.
        /// </summary>
        public ArgumentList Arguments
        {
            get
            {
                CheckDisposed();

                return (next != null) ?
                    next.Arguments : arguments;
            }
            set
            {
                CheckDisposed();

                if (next != null)
                    next.Arguments = value;
                else
                    arguments = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this call frame owns its argument
        /// list.
        /// </summary>
        private bool ownArguments;
        /// <summary>
        /// Gets or sets a value indicating whether this call frame owns (and is
        /// responsible for disposing) its argument list.  When this call frame
        /// is linked to a next call frame, the value is obtained from or stored
        /// on that frame.
        /// </summary>
        public bool OwnArguments
        {
            get
            {
                CheckDisposed();

                return (next != null) ?
                    next.OwnArguments : ownArguments;
            }
            set
            {
                CheckDisposed();

                if (next != null)
                    next.OwnArguments = value;
                else
                    ownArguments = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the procedure argument list associated with this call frame.
        /// </summary>
        private ArgumentList procedureArguments;
        /// <summary>
        /// Gets or sets the procedure argument list associated with this call
        /// frame.  When this call frame is linked to a next call frame, the
        /// value is obtained from or stored on that frame.
        /// </summary>
        public ArgumentList ProcedureArguments
        {
            get
            {
                CheckDisposed();

                return (next != null) ?
                    next.ProcedureArguments : procedureArguments;
            }
            set
            {
                CheckDisposed();

                if (next != null)
                    next.ProcedureArguments = value;
                else
                    procedureArguments = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the variable collection owned by this call frame.
        /// </summary>
        private VariableDictionary variables;
        /// <summary>
        /// Gets or sets the variable collection for this call frame.  When this
        /// call frame is linked to a next call frame, the value is obtained
        /// from or stored on that frame.
        /// </summary>
        public VariableDictionary Variables
        {
            get
            {
                CheckDisposed();

                return (next != null) ?
                    next.Variables : variables;
            }
            set
            {
                CheckDisposed();

                if (next != null)
                    next.Variables = value;
                else
                    variables = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the other call frame whose variables are shared with this
        /// call frame.
        /// </summary>
        private ICallFrame other;
        /// <summary>
        /// Gets or sets the other call frame whose variables are shared with
        /// this call frame.
        /// </summary>
        public ICallFrame Other
        {
            get { CheckDisposed(); return other; }
            set { CheckDisposed(); other = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the previous call frame linked to this call frame.
        /// </summary>
        private ICallFrame previous;
        /// <summary>
        /// Gets or sets the previous call frame linked to this call frame.
        /// </summary>
        public ICallFrame Previous
        {
            get { CheckDisposed(); return previous; }
            set { CheckDisposed(); previous = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the next call frame linked to this call frame.
        /// </summary>
        private ICallFrame next;
        /// <summary>
        /// Gets or sets the next call frame linked to this call frame.
        /// </summary>
        public ICallFrame Next
        {
            get { CheckDisposed(); return next; }
            set { CheckDisposed(); next = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the engine-specific client data for this call frame.
        /// </summary>
        private IClientData engineData;
        /// <summary>
        /// Gets or sets the engine-specific client data for this call frame.
        /// </summary>
        public IClientData EngineData
        {
            get { CheckDisposed(); return engineData; }
            set { CheckDisposed(); engineData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the auxiliary client data for this call frame.
        /// </summary>
        private IClientData auxiliaryData;
        /// <summary>
        /// Gets or sets the auxiliary client data for this call frame.
        /// </summary>
        public IClientData AuxiliaryData
        {
            get { CheckDisposed(); return auxiliaryData; }
            set { CheckDisposed(); auxiliaryData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the resolver client data for this call frame.
        /// </summary>
        private IClientData resolveData;
        /// <summary>
        /// Gets or sets the resolver client data for this call frame.
        /// </summary>
        public IClientData ResolveData
        {
            get { CheckDisposed(); return resolveData; }
            set { CheckDisposed(); resolveData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the extra client data for this call frame.
        /// </summary>
        private IClientData extraData;
        /// <summary>
        /// Gets or sets the extra client data for this call frame.
        /// </summary>
        public IClientData ExtraData
        {
            get { CheckDisposed(); return extraData; }
            set { CheckDisposed(); extraData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this call frame is a variable call
        /// frame (i.e. one that maintains a variable collection).
        /// </summary>
        public bool IsVariable
        {
            get
            {
                CheckDisposed();

                if (!HasFlags(CallFrameFlags.Variables, false))
                    return false;

                if (HasFlags(CallFrameFlags.NoVariables, false))
                    return false;

                return (variables != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of name/value pairs describing this call
        /// frame, suitable for diagnostic display.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags controlling which details are included in the resulting
        /// list.
        /// </param>
        /// <returns>
        /// A <see cref="StringPairList" /> containing the requested details of
        /// this call frame.
        /// </returns>
        public StringPairList ToList(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            bool all = FlagOps.HasFlags(
                detailFlags, DetailFlags.ICallFrameToListAll, true);

            list.Add("flags", flags.ToString());

            if (all)
            {
                list.Add("frameId", frameId.ToString());
                list.Add("frameLevel", frameLevel.ToString());
            }

            if (name != null)
            {
                list.Add("name", (name != null) /* REDUNDANT */ ?
                    name : _String.Null);
            }

            if (all)
            {
                if ((tags != null) && (tags.Count > 0))
                {
                    list.Add("tags", (tags != null) /* REDUNDANT */ ?
                        tags.ToString() : _String.Null);
                }
            }

            if ((variables != null) && (variables.Count > 0))
            {
                list.Add("vars", (variables != null) /* REDUNDANT */ ?
                    variables.Count.ToString() : _String.Null);
            }

            if ((arguments != null) && (arguments.Count > 0))
            {
                list.Add("args", (arguments != null) /* REDUNDANT */ ?
                    arguments.Count.ToString() : _String.Null);
            }

            if (all)
                list.Add("ownArgs", ownArguments.ToString());

            if ((procedureArguments != null) && (procedureArguments.Count > 0))
            {
                list.Add("procArgs", (procedureArguments != null) /* REDUNDANT */ ?
                    procedureArguments.Count.ToString() : _String.Null);
            }

            if (all)
                list.Add("index", index.ToString());

            list.Add("level", level.ToString());

            if (all)
            {
                if (threadId != null)
                {
                    list.Add("threadId", (threadId != null) /* REDUNDANT */ ?
                        ((long)threadId).ToString() : _String.Null);
                }
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string describing this call frame, honoring
        /// the specified detail flags.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags controlling how much detail is included in the resulting
        /// string.
        /// </param>
        /// <returns>
        /// A string describing this call frame.  When the flags request a
        /// name-only rendering, this is the call frame name (or an empty string
        /// when it has no name).
        /// </returns>
        public string ToString(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            if (FlagOps.HasFlags(
                    detailFlags, DetailFlags.ICallFrameNameOnly, true))
            {
                return (name != null) ? name : String.Empty;
            }
            else
            {
                return ToList(detailFlags).ToString();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this call frame has the specified
        /// flags set.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to test for on this call frame.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are set; zero to
        /// require that any of the specified flags is set.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame has the specified flags set, subject to
        /// <paramref name="all" />; otherwise, zero.
        /// </returns>
        public bool HasFlags(
            CallFrameFlags hasFlags,
            bool all
            )
        {
            CheckDisposed();

            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CallFrameFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or unsets the specified flags on this call frame.
        /// </summary>
        /// <param name="flags">
        /// The flags to set or unset on this call frame.
        /// </param>
        /// <param name="set">
        /// Non-zero to set the specified flags; zero to unset them.
        /// </param>
        /// <returns>
        /// The resulting flags of this call frame after the change.
        /// </returns>
        public CallFrameFlags SetFlags(
            CallFrameFlags flags,
            bool set
            )
        {
            CheckDisposed();

            if (set)
                return (this.flags |= flags);
            else
                return (this.flags &= ~flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the collection of marks (tags) for this call
        /// frame, creating an empty collection when none exists.
        /// </summary>
        /// <returns>
        /// Non-zero if the collection of marks was created; zero if it already
        /// existed.
        /// </returns>
        public bool InitializeMarks()
        {
            CheckDisposed();

            if (tags != null)
            {
                return false;
            }
            else
            {
                tags = new ObjectDictionary();
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears all of the marks (tags) from this call frame.
        /// </summary>
        /// <returns>
        /// Non-zero if any marks were cleared; otherwise, zero.
        /// </returns>
        public bool ClearMarks()
        {
            CheckDisposed();

            if ((tags != null) && (tags.Count > 0))
            {
                tags.Clear();

                //
                // NOTE: Yes, we cleared some tags.
                //
                return true;
            }

            //
            // NOTE: For whatever reason, we did not clear the tags.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this call frame has the named mark
        /// (tag).
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.  This parameter should not be null
        /// or an empty string.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame has the named mark; otherwise, zero.
        /// </returns>
        public bool HasMark(
            string name
            )
        {
            CheckDisposed();

            object value = null;

            return HasMark(name, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this call frame has the named mark
        /// (tag) and, when present, interprets its value as a call frame.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.  This parameter should not be null
        /// or an empty string.
        /// </param>
        /// <param name="frame">
        /// Upon return, when the mark is present, this contains its value
        /// interpreted as an <see cref="ICallFrame" />, or null when the value
        /// is not a call frame.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame has the named mark; otherwise, zero.
        /// </returns>
        public bool HasMark(
            string name,
            ref ICallFrame frame
            )
        {
            CheckDisposed();

            object value = null;

            if (HasMark(name, ref value))
            {
                //
                // NOTE: Attempt to interpret the tag value as another
                //       call frame.
                //
                frame = value as ICallFrame;

                //
                // NOTE: Yes, the tag is present.
                //
                return true;
            }

            //
            // NOTE: For whatever reason, the tag is not present.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this call frame has the named mark
        /// (tag) and, when present, retrieves its value.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.  This parameter should not be null
        /// or an empty string.
        /// </param>
        /// <param name="value">
        /// Upon return, when the mark is present, this contains its associated
        /// value.
        /// </param>
        /// <returns>
        /// Non-zero if this call frame has the named mark; otherwise, zero.
        /// </returns>
        public bool HasMark(
            string name,
            ref object value
            )
        {
            CheckDisposed();

            //
            // NOTE: Null and/or empty string tag names are not allowed.
            //
            if (!String.IsNullOrEmpty(name))
            {
                if (tags != null)
                {
                    if (tags.TryGetValue(name, out value))
                    {
                        //
                        // NOTE: Yes, the tag is present.
                        //
                        return true;
                    }
                }
            }

            //
            // NOTE: For whatever reason, the tag is not present.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or unsets the named mark (tag) on this call frame.
        /// </summary>
        /// <param name="mark">
        /// Non-zero to set the named mark; zero to unset it.
        /// </param>
        /// <param name="name">
        /// The name of the mark to set or unset.  This parameter should not be
        /// null or an empty string.
        /// </param>
        /// <param name="value">
        /// The value to associate with the mark when setting it.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the mark was added or removed; otherwise, zero.
        /// </returns>
        public bool SetMark(
            bool mark,
            string name,
            object value
            )
        {
            CheckDisposed();

            //
            // NOTE: Null and/or empty string tag names are not allowed.
            //
            if (!String.IsNullOrEmpty(name))
            {
                if (tags != null)
                {
                    if (mark && !tags.ContainsKey(name))
                    {
                        //
                        // NOTE: Try to add the tag.
                        //
                        tags.Add(name, value);

                        //
                        // NOTE: Yes, we added the tag.
                        //
                        return true;
                    }
                    else if (!mark && tags.ContainsKey(name))
                    {
                        //
                        // NOTE: Try to remove the tag.
                        //
                        tags.Remove(name);

                        //
                        // NOTE: Yes, we removed the tag.
                        //
                        return true;
                    }
                }
            }

            //
            // NOTE: For whatever reason, we did not add/remove the tag.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or unsets the named mark (tag) on this call frame
        /// and, correspondingly, sets or unsets the specified flags.
        /// </summary>
        /// <param name="mark">
        /// Non-zero to set the named mark and flags; zero to unset them.
        /// </param>
        /// <param name="flags">
        /// The flags to set or unset on this call frame along with the mark.
        /// </param>
        /// <param name="name">
        /// The name of the mark to set or unset.  This parameter may be null,
        /// in which case only the flags are changed.
        /// </param>
        /// <param name="value">
        /// The value to associate with the mark when setting it.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the mark and flags were changed; otherwise, zero.
        /// </returns>
        public bool SetMark(
            bool mark,
            CallFrameFlags flags,
            string name,
            object value
            )
        {
            CheckDisposed();

            if ((name == null) || SetMark(mark, name, value))
            {
                if (mark)
                    this.flags |= flags;
                else
                    this.flags &= ~flags;

                //
                // NOTE: Yes, we set or unset the mark.
                //
                return true;
            }

            //
            // NOTE: For whatever reason, we did not set the mark.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the named variables of this call frame into a new
        /// dictionary, removing them from the call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional list of variable names to save; when null, all
        /// variables are saved.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// Upon return, this contains the newly created dictionary of saved
        /// variables.
        /// </param>
        /// <param name="count">
        /// On input, the running count of variables processed; on output, this
        /// is increased by the number of variables saved.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public ReturnCode Save(
            Interpreter interpreter,               /* in */
            ArgumentList arguments,                /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
            )
        {
            CheckDisposed();

            return Save(
                interpreter, new ArgumentDictionary(arguments),
                ref savedVariables, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the named variables of this call frame into a new
        /// dictionary, removing them from the call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional collection of variable names to save; when null, all
        /// variables are saved.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// Upon return, this contains the newly created dictionary of saved
        /// variables.
        /// </param>
        /// <param name="count">
        /// On input, the running count of variables processed; on output, this
        /// is increased by the number of variables saved.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public ReturnCode Save(
            Interpreter interpreter,               /* in */
            ArgumentDictionary arguments,          /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
            )
        {
            CheckDisposed();

            savedVariables = new VariableDictionary();

            return CallFrameOps.MoveNamedVariables(
                interpreter, variables, savedVariables, arguments,
                false, false, false, false, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores previously saved variables back into this call
        /// frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional list of variable names to restore; when null, all saved
        /// variables are restored.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// The dictionary of previously saved variables to restore; the
        /// restored variables are moved out of it.
        /// </param>
        /// <param name="count">
        /// On input, the running count of variables processed; on output, this
        /// is increased by the number of variables restored.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public ReturnCode Restore(
            Interpreter interpreter,               /* in */
            ArgumentList arguments,                /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* in, out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
            )
        {
            CheckDisposed();

            return Restore(
                interpreter, new ArgumentDictionary(arguments),
                ref savedVariables, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores previously saved variables back into this call
        /// frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this call frame belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="arguments">
        /// The optional collection of variable names to restore; when null, all
        /// saved variables are restored.  This parameter may be null.
        /// </param>
        /// <param name="savedVariables">
        /// The dictionary of previously saved variables to restore; the
        /// restored variables are moved out of it.
        /// </param>
        /// <param name="count">
        /// On input, the running count of variables processed; on output, this
        /// is increased by the number of variables restored.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public ReturnCode Restore(
            Interpreter interpreter,               /* in */
            ArgumentDictionary arguments,          /* in: OPTIONAL */
            ref VariableDictionary savedVariables, /* in, out */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
            )
        {
            CheckDisposed();

            return CallFrameOps.MoveNamedVariables(
                interpreter, savedVariables, variables, arguments,
                false, false, false, false, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this call frame,
        /// resetting it to an empty state.  The global call frame is only freed
        /// when the interpreter itself is being disposed.
        /// </summary>
        /// <param name="global">
        /// Non-zero to force this call frame to be freed even when it would
        /// otherwise be protected from being freed (for example, the global
        /// call frame).
        /// </param>
        public void Free(
            bool global
            )
        {
            //
            // HACK: *SPECIAL CASE* We cannot dispose the global call frame
            //       unless we are [also] disposing of the interpreter itself.
            //
            if (global ||
                !FlagOps.HasFlags(flags, CallFrameFlags.NoFree, true))
            {
                kind = IdentifierKind.None;
                id = Guid.Empty;
                name = null;
                group = null;
                description = null;
                clientData = null;
                frameId = 0;
                frameLevel = 0;
                flags = CallFrameFlags.None;
                threadId = null;

                ///////////////////////////////////////////////////////////////

                if (tags != null)
                {
                    tags.Clear();
                    tags = null;
                }

                ///////////////////////////////////////////////////////////////

                index = 0;
                level = 0;

                ///////////////////////////////////////////////////////////////

                if (arguments != null)
                {
                    //
                    // BUGFIX: We can only mutate argument lists that we own.
                    //
                    if (ownArguments)
                        arguments.Clear();

                    arguments = null;
                }

                ///////////////////////////////////////////////////////////////

                ownArguments = false;

                ///////////////////////////////////////////////////////////////

                if (procedureArguments != null)
                {
                    procedureArguments.Clear();
                    procedureArguments = null;
                }

                ///////////////////////////////////////////////////////////////

                if (variables != null)
                {
                    variables.Clear();
                    variables = null;
                }

                ///////////////////////////////////////////////////////////////

                other = null;    /* NOTE: Not owned, do not dispose. */
                previous = null; /* NOTE: Not owned, do not dispose. */
                next = null;     /* NOTE: Not owned, do not dispose. */

                ///////////////////////////////////////////////////////////////

                engineData = null;    /* NOTE: Not owned, do not dispose. */
                auxiliaryData = null; /* NOTE: Not owned, do not dispose. */
                resolveData = null;   /* NOTE: Not owned, do not dispose. */
                extraData = null;     /* NOTE: Not owned, do not dispose. */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string describing this call frame using its
        /// name only.
        /// </summary>
        /// <returns>
        /// A string containing the name of this call frame (or an empty string
        /// when it has no name).
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToString(DetailFlags.ICallFrameNameOnly);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this call frame has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this call frame has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this call frame has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(CallFrame));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this call frame.  It
        /// implements the standard dispose pattern.
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

                    Free(true);
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
        /// This method releases all resources held by this call frame and
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
        /// Finalizes this call frame, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~CallFrame()
        {
            Dispose(false);
        }
        #endregion
    }
}
