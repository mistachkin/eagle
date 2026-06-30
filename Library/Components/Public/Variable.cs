/*
 * Variable.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a variable within the Eagle interpreter.  It
    /// holds the variable's name and qualified name, its flags, its scalar
    /// value and/or array element values, any associated traces, the call
    /// frame that owns it, an optional link to another variable (e.g. via
    /// [upvar] or [global]), and its thread-locking state.  It implements the
    /// <see cref="IVariable" /> interface.
    /// </summary>
    [ObjectId("ec135739-c556-422f-b396-314d27a556a9")]
    public sealed class Variable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IVariable
    {
        #region Private Constants
        /// <summary>
        /// The default value used for the string form of a variable when its
        /// value is null; this is the empty string.
        /// </summary>
        internal static readonly string DefaultValue = String.Empty;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the mark (tag) used to associate a namespace with a
        /// variable.
        /// </summary>
        private const string NamespaceTagName = "@namespace";
        /// <summary>
        /// The name of the mark (tag) used to associate a call frame with a
        /// variable.
        /// </summary>
        private const string FrameTagName = "@frame";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The event used to signal interested parties when the flags of this
        /// variable change.  This member may be null.
        /// </summary>
        private EventWaitHandle @event;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
        /// <summary>
        /// Constructs a variable that is linked to another variable.  This
        /// constructor delegates to a private constructor, using no variable
        /// flags.
        /// </summary>
        /// <param name="frame">
        /// The call frame that owns this variable.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of this variable.  This parameter may be null.
        /// </param>
        /// <param name="qualifiedName">
        /// The fully qualified name of this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="link">
        /// The variable that this variable is linked to, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="linkIndex">
        /// The array element index within the linked variable, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="event">
        /// The event used to signal changes to this variable, if any.  This
        /// parameter may be null.
        /// </param>
        internal Variable(
            ICallFrame frame,
            string name,
            string qualifiedName,
            IVariable link,
            string linkIndex,
            EventWaitHandle @event
            )
            : this(frame, name, VariableFlags.None, qualifiedName,
                   link, linkIndex, @event)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a variable with the specified flags and traces.  This
        /// constructor delegates to a private constructor.
        /// </summary>
        /// <param name="frame">
        /// The call frame that owns this variable.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of this variable.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control the behavior of this variable.
        /// </param>
        /// <param name="qualifiedName">
        /// The fully qualified name of this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="traces">
        /// The list of traces associated with this variable, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="event">
        /// The event used to signal changes to this variable, if any.  This
        /// parameter may be null.
        /// </param>
        internal Variable(
            ICallFrame frame,
            string name,
            VariableFlags flags,
            string qualifiedName,
            TraceList traces,
            EventWaitHandle @event
            )
            : this(frame, name, flags, qualifiedName, (string)null, @event)
        {
            #region IVariable Metadata Members
            this.traces = traces;
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a variable with the specified flags that is linked to
        /// another variable.  This constructor delegates to a private
        /// constructor.
        /// </summary>
        /// <param name="frame">
        /// The call frame that owns this variable.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of this variable.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control the behavior of this variable.
        /// </param>
        /// <param name="qualifiedName">
        /// The fully qualified name of this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="link">
        /// The variable that this variable is linked to, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="linkIndex">
        /// The array element index within the linked variable, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="event">
        /// The event used to signal changes to this variable, if any.  This
        /// parameter may be null.
        /// </param>
        private Variable(
            ICallFrame frame,
            string name,
            VariableFlags flags,
            string qualifiedName,
            IVariable link,
            string linkIndex,
            EventWaitHandle @event
            )
            : this(frame, name, flags, qualifiedName, (string)null, @event)
        {
            #region IVariable Metadata Members
            this.link = link;
            this.linkIndex = linkIndex;
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a variable with the specified flags and scalar value.
        /// This is the primary private constructor that establishes the
        /// well-known initial state of a variable.
        /// </summary>
        /// <param name="frame">
        /// The call frame that owns this variable.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of this variable.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control the behavior of this variable; the
        /// non-instance flags are masked off.
        /// </param>
        /// <param name="qualifiedName">
        /// The fully qualified name of this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The initial scalar value of this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="event">
        /// The event used to signal changes to this variable, if any.  This
        /// parameter may be null.
        /// </param>
        private Variable(
            ICallFrame frame,
            string name,
            VariableFlags flags,
            string qualifiedName,
            object value,
            EventWaitHandle @event
            )
        {
            #region IIdentifier Members
            this.kind = IdentifierKind.Variable;
            this.id = Guid.Empty;
            this.name = name;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IVariable Metadata Members
            this.frame = frame;
            this.flags = flags & ~VariableFlags.NonInstanceMask;
            this.tags = null;
            this.qualifiedName = qualifiedName;
            this.link = null;
            this.linkIndex = null;
            this.traces = null;
            this.threadId = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IVariable Data Members
            this.value = value;
            this.arrayValue = null; // TODO: For arrays, create this?
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Non-IVariable Members
            this.@event = @event;
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the Clone() method only.
        //
        /// <summary>
        /// Constructs a fully specified copy of a variable, including its
        /// flags, link target, scalar value, array element values, traces, and
        /// thread-locking state.  This constructor is for use by the
        /// <c>Clone</c> method only.
        /// </summary>
        /// <param name="frame">
        /// The call frame that owns this variable.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of this variable.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control the behavior of this variable; the
        /// non-instance flags are masked off.
        /// </param>
        /// <param name="qualifiedName">
        /// The fully qualified name of this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="link">
        /// The variable that this variable is linked to, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="linkIndex">
        /// The array element index within the linked variable, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// The initial scalar value of this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="arrayValue">
        /// The collection of array element values for this variable, if it is
        /// an array.  This parameter may be null.
        /// </param>
        /// <param name="traces">
        /// The list of traces associated with this variable, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread that holds the lock on this variable,
        /// or null when it is not locked.
        /// </param>
        /// <param name="event">
        /// The event used to signal changes to this variable, if any.  This
        /// parameter may be null.
        /// </param>
        private Variable(
            ICallFrame frame,
            string name,
            VariableFlags flags,
            string qualifiedName,
            IVariable link,
            string linkIndex,
            object value,
            ElementDictionary arrayValue,
            TraceList traces,
            long? threadId,
            EventWaitHandle @event
            )
        {
            #region IIdentifier Members
            this.kind = IdentifierKind.Variable;
            this.id = Guid.Empty;
            this.name = name;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IVariable Metadata Members
            this.frame = frame;
            this.flags = flags & ~VariableFlags.NonInstanceMask;
            this.tags = null;
            this.qualifiedName = qualifiedName;
            this.link = link;
            this.linkIndex = linkIndex;
            this.traces = traces;
            this.threadId = threadId;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IVariable Data Members
            this.value = value;
            this.arrayValue = arrayValue;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Non-IVariable Members
            this.@event = @event;
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        //
        // TODO: Why does this method exist?
        //
        /// <summary>
        /// This method determines whether this variable has any traces that may
        /// produce side-effects when they are fired.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to check whether any gathered trace values
        /// refer to existing opaque object handles.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// Non-zero if this variable has one or more traces that may produce
        /// side-effects; otherwise, zero.
        /// </returns>
        internal bool HasTracesWithSideEffects( /* NOT USED */
            Interpreter interpreter
            )
        {
            //
            // NOTE: If a variable is undefined, any traces it
            //       may have are invalid, will not be invoked,
            //       and therefore cannot have any side-effects.
            //
            if (EntityOps.IsUndefined(this))
                return false;

            //
            // NOTE: If a variable has no list of traces, they
            //       (obviously?) cannot have side-effects.
            //
            if (traces == null)
                return false;

            bool checkObjects = false;

            foreach (ITrace trace in traces)
            {
                //
                // NOTE: No trace?  No problem.
                //
                if (trace == null)
                    continue;

                //
                // TODO: Cannot deal with cross-AppDomain stuff
                //       here.  Is something actually required
                //       here for this case?
                //
                if (AppDomainOps.IsTransparentProxy(trace))
                    continue;

                //
                // NOTE: No trace callback?  There is nothing to
                //       create a side-effect.
                //
                TraceCallback callback = trace.Callback;

                if (callback == null)
                    continue;

                //
                // BUGBUG: Do not allow the object trace callback
                //         to be present more than once?
                //
                if (!checkObjects &&
                    (Interpreter.IsObjectTraceCallback(callback)
#if DATA
                        || Interpreter.IsDbTraceCallback(callback)
#endif
                    ))
                {
                    //
                    // NOTE: This variable may have traces with
                    //       side-effects IF it actually refers
                    //       to an existing opaque object handle.
                    //       That will be checked later.
                    //
                    checkObjects = true;
                    continue;
                }

                //
                // NOTE: Any other trace callback of any kind,
                //       including those internal and external
                //       to the core library, are considered
                //       to have side-effects.
                //
                return true;
            }

            if (checkObjects && (interpreter != null))
            {
                StringList localValues = null;

                ScriptOps.GatherTraceValues(
                    name, null, value, arrayValue, ref localValues);

                if (localValues != null)
                {
                    foreach (string localValue in localValues)
                    {
                        if (localValue == null)
                            continue;

                        if (interpreter.DoesObjectExist(
                                localValue) == ReturnCode.Ok)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the scalar value and array element values of this
        /// variable, optionally zeroing any string storage first.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to determine whether string storage should be
        /// zeroed.  This parameter may be null.
        /// </param>
        /// <param name="zero">
        /// Non-zero to zero any string storage held by the value before
        /// releasing it.
        /// </param>
        internal void ResetValue(
            Interpreter interpreter,
            bool zero
            )
        {
#if !MONO && NATIVE && WINDOWS
            if (zero && (interpreter != null) && interpreter.HasZeroString())
            {
                if (value is string)
                {
                    /* IGNORED */
                    StringOps.ZeroStringOrTrace((string)value);
                }
                else if (value is Argument)
                {
                    ((Argument)value).ResetValue(interpreter, zero);
                }
                else if (value is Result)
                {
                    ((Result)value).ResetValue(interpreter, zero);
                }
            }
#endif

            value = null;

            if (arrayValue != null)
            {
                arrayValue.ResetValue(interpreter, zero);
                arrayValue = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally initializes and/or clears the list of traces
        /// associated with this variable.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to create an empty list of traces when none currently
        /// exists.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the existing list of traces, returning a copy of
        /// the traces that were removed.
        /// </param>
        /// <returns>
        /// A copy of the list of traces that were cleared, or null when nothing
        /// was cleared.
        /// </returns>
        private TraceList ResetTraces(
            bool initialize,
            bool clear
            )
        {
            if (traces != null)
            {
                if (clear)
                {
                    TraceList oldTraces = new TraceList(traces);

                    traces.Clear();

                    return oldTraces;
                }
            }
            else if (initialize)
            {
                traces = new TraceList();
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the collection of marks (tags) for this
        /// variable when it exists but is empty.
        /// </summary>
        /// <returns>
        /// Non-zero if the empty collection of marks was released; otherwise,
        /// zero.
        /// </returns>
        private bool ResetMarks()
        {
            if ((tags == null) || (tags.Count > 0))
                return false;

            tags = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method determines whether this variable is currently locked by
        /// the calling thread.  The interpreter lock must already be held by the
        /// caller.
        /// </summary>
        /// <returns>
        /// Non-zero if this variable is locked by the current thread;
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
        /// This method determines whether this variable is currently locked by
        /// a thread other than the calling thread.  The interpreter lock must
        /// already be held by the caller.
        /// </summary>
        /// <param name="threadId">
        /// Upon return, this contains the identifier of the thread that has
        /// locked this variable, or null if it is not locked.
        /// </param>
        /// <returns>
        /// Non-zero if this variable is locked by a thread other than the
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
        /// This method attempts to unlock this variable on behalf of the
        /// calling thread.
        /// </summary>
        /// <param name="errorOnUnlocked">
        /// Non-zero to treat an already-unlocked variable as an error; zero to
        /// treat it as success.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this variable was unlocked (or was already unlocked and
        /// <paramref name="errorOnUnlocked" /> is zero); otherwise, zero.
        /// </returns>
        private bool PrivateUnlock(
            bool errorOnUnlocked,
            ref Result error
            )
        {
            //
            // HACK: This method does care about the undefined flag.
            //       If a variable is undefined, unlocking it cannot
            //       fail when it is already unlocked.
            //
            long? localMaybeThreadId = threadId;

            if (localMaybeThreadId == null)
            {
                if (HasFlags(VariableFlags.Undefined, true))
                {
                    //
                    // HACK: The variable is now (?) dead;
                    //       therefore, permit unlocking.
                    //
                    return true;
                }
                else
                {
                    //
                    // NOTE: It is possible that another
                    //       thread [unset] the variable
                    //       and then recreated it (i.e.
                    //       it is actually a different
                    //       variable now, technically).
                    //
                    if (errorOnUnlocked)
                    {
                        error = "variable already unlocked";
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
                    "variable locked by other thread {0}",
                    FormatOps.WrapOrNull(localThreadId));

                return false;
            }

            threadId = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current number of active trace-firing levels for this
        /// variable.
        /// </summary>
        private long PrivateLevels
        {
            get { return Interlocked.CompareExchange(ref levels, 0, 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the count of active trace-firing levels for
        /// this variable.
        /// </summary>
        /// <returns>
        /// The new number of active trace-firing levels after the increment.
        /// </returns>
        private long PrivateEnterLevel()
        {
            return Interlocked.Increment(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the count of active trace-firing levels for
        /// this variable.
        /// </summary>
        /// <returns>
        /// The new number of active trace-firing levels after the decrement.
        /// </returns>
        private long PrivateExitLevel()
        {
            return Interlocked.Decrement(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a region in which traces for this variable are
        /// suppressed, saving the current variable flags so they can be
        /// restored later.
        /// </summary>
        /// <param name="savedFlags">
        /// Upon return, this contains the variable flags as they were before
        /// trace suppression was enabled.
        /// </param>
        private void BeginNoTrace(
            out VariableFlags savedFlags
            )
        {
            savedFlags = flags;
            flags |= VariableFlags.NoTrace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a region in which traces for this variable were
        /// suppressed, restoring the previously saved variable flags.
        /// </summary>
        /// <param name="savedFlags">
        /// The variable flags to restore, as previously saved by the
        /// <c>BeginNoTrace</c> method; upon return, this is reset to no flags.
        /// </param>
        private void EndNoTrace(
            ref VariableFlags savedFlags
            )
        {
            flags = savedFlags;
            savedFlags = VariableFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this variable.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this variable.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this variable.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this variable.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this variable.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this variable.
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this variable.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this variable.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this variable.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this variable.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this variable.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this variable.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadLock Members
        /// <summary>
        /// Stores the identifier of the thread that currently holds the lock on
        /// this variable, or null when it is not locked.
        /// </summary>
        //
        // NOTE: This property is really for external use only.  Also, it
        //       should not be used to actually set the associated value,
        //       except under a few very rare sets of circumstances.
        //
        private long? threadId;
        /// <summary>
        /// Gets or sets the identifier of the thread that currently holds the
        /// lock on this variable, or null when it is not locked.  This property
        /// is really for external use only; it should not be used to actually
        /// set the associated value, except under a few very rare sets of
        /// circumstances.
        /// </summary>
        public long? ThreadId
        {
            get { return threadId; }
            set { threadId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method determines whether this variable is currently locked by
        /// the calling thread.  The interpreter lock must already be held by the
        /// caller.
        /// </summary>
        /// <returns>
        /// Non-zero if this variable is locked by the current thread;
        /// otherwise, zero.
        /// </returns>
        public bool IsLocked()
        {
            return IsLockedByThisThread();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method attempts to lock this variable for exclusive use by the
        /// calling thread.  The interpreter lock must already be held by the
        /// caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this variable was successfully locked by the current
        /// thread; otherwise, zero (for example, if it is already locked).
        /// </returns>
        public bool Lock(
            ref Result error
            )
        {
            //
            // HACK: This method purposely does not care about the
            //       undefined flag.  Generally, a variable cannot
            //       be locked while undefined; however, we do not
            //       enforce that here.
            //
            long? localMaybeThreadId = threadId;

            if (localMaybeThreadId != null)
            {
                error = String.Format(
                    "variable already locked by thread {0}",
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
        /// This method attempts to unlock this variable, treating an
        /// already-unlocked variable as an error.  The interpreter lock must
        /// already be held by the caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this variable was successfully unlocked; otherwise, zero.
        /// </returns>
        public bool Unlock(
            ref Result error
            )
        {
            return PrivateUnlock(true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        /// <summary>
        /// This method attempts to unlock this variable, treating an
        /// already-unlocked variable as success rather than an error.  The
        /// interpreter lock must already be held by the caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this variable was unlocked (or was already unlocked);
        /// otherwise, zero.
        /// </returns>
        public bool MaybeUnlock(
            ref Result error
            )
        {
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
        /// This method determines whether this variable is currently usable by
        /// the calling thread.  The interpreter lock must already be held by the
        /// caller.
        /// </summary>
        /// <returns>
        /// Non-zero if this variable is usable by the current thread (i.e. it is
        /// not locked by another thread); otherwise, zero.
        /// </returns>
        public bool IsUsable()
        {
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
        /// This method determines whether this variable is currently usable by
        /// the calling thread, reporting why it is not when applicable.  The
        /// interpreter lock must already be held by the caller.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if this variable is usable by the current thread (i.e. it is
        /// not locked by another thread); otherwise, zero.
        /// </returns>
        public bool IsUsable(
            ref Result error
            )
        {
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
                "variable locked by other thread {0}",
                FormatOps.WrapOrNull(localMaybeThreadId));

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveLevels Members
        /// <summary>
        /// Stores the current number of active trace-firing levels for this
        /// variable.
        /// </summary>
        private long levels;
        /// <summary>
        /// Gets the current number of active trace-firing levels for this
        /// variable.
        /// </summary>
        public long Levels
        {
            get { return PrivateLevels; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the count of active trace-firing levels for
        /// this variable.
        /// </summary>
        /// <returns>
        /// The new number of active trace-firing levels after the increment.
        /// </returns>
        public long EnterLevel()
        {
            return PrivateEnterLevel();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the count of active trace-firing levels for
        /// this variable.
        /// </summary>
        /// <returns>
        /// The new number of active trace-firing levels after the decrement.
        /// </returns>
        public long ExitLevel()
        {
            return PrivateExitLevel();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IVariable Members
        /// <summary>
        /// Stores the call frame that owns this variable.
        /// </summary>
        private ICallFrame frame;
        /// <summary>
        /// Gets or sets the call frame that owns this variable.
        /// </summary>
        public ICallFrame Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags that control the behavior of this variable.
        /// </summary>
        private VariableFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this variable.
        /// Setting this property fires the internal flags-changed event handler.
        /// </summary>
        public VariableFlags Flags
        {
            get { return flags; }
            set
            {
                //
                // NOTE: Save the old variable flags.
                //
                VariableFlags oldFlags = flags;

                //
                // NOTE: Set the new variable flags.
                //
                flags = value;

                //
                // NOTE: Call our internal event handler,
                //       passing the old and new flags.
                //
                /* IGNORED */
                EntityOps.OnFlagsChanged(@event, oldFlags, flags);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of marks (tags) associated with this variable.
        /// </summary>
        private ObjectDictionary tags;
        /// <summary>
        /// Gets or sets the collection of marks (tags) associated with this
        /// variable.
        /// </summary>
        public ObjectDictionary Tags
        {
            get { return tags; }
            set { tags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the fully qualified name of this variable.
        /// </summary>
        private string qualifiedName;
        /// <summary>
        /// Gets or sets the fully qualified name of this variable.
        /// </summary>
        public string QualifiedName
        {
            get { return qualifiedName; }
            set { qualifiedName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the variable that this variable is linked to, if any.
        /// </summary>
        private IVariable link;
        /// <summary>
        /// Gets or sets the variable that this variable is linked to, if any.
        /// </summary>
        public IVariable Link
        {
            get { return link; }
            set { link = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the array element index within the linked variable, if any.
        /// </summary>
        private string linkIndex;
        /// <summary>
        /// Gets or sets the array element index within the linked variable, if
        /// any.
        /// </summary>
        public string LinkIndex
        {
            get { return linkIndex; }
            set { linkIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the scalar value of this variable.
        /// </summary>
        private object value;
        /// <summary>
        /// Gets or sets the scalar value of this variable.
        /// </summary>
        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of array element values for this variable, if
        /// it is an array.
        /// </summary>
        private ElementDictionary arrayValue;
        /// <summary>
        /// Gets or sets the collection of array element values for this
        /// variable, if it is an array.
        /// </summary>
        public ElementDictionary ArrayValue
        {
            get { return arrayValue; }
            set { arrayValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of traces associated with this variable.
        /// </summary>
        private TraceList traces;
        /// <summary>
        /// Gets or sets the list of traces associated with this variable.
        /// </summary>
        public TraceList Traces
        {
            get { return traces; }
            set { traces = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the call frame that owns this variable and clears
        /// its cached qualified name, optionally recomputing it via the
        /// specified interpreter.
        /// </summary>
        /// <param name="frame">
        /// The new call frame that owns this variable.  This parameter may be
        /// null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to recompute the qualified name of this
        /// variable.  This parameter may be null, in which case the qualified
        /// name is not recomputed.
        /// </param>
        public void ResetFrame(
            ICallFrame frame,
            Interpreter interpreter
            )
        {
            this.frame = frame;
            this.qualifiedName = null;

            if (interpreter != null)
                interpreter.MaybeSetQualifiedName(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this variable as undefined or defined.  Making a
        /// variable undefined also releases any thread lock held on it.
        /// </summary>
        /// <param name="undefined">
        /// Non-zero to mark this variable as undefined; zero to mark it as
        /// defined.
        /// </param>
        public void MakeUndefined(
            bool undefined
            )
        {
            //
            // HACK: Obviously (?), if a thread uses [unset] on a
            //       variable, it gives up any lock it has on it.
            //
            if (undefined)
            {
                flags |= VariableFlags.Undefined;
                threadId = null;
            }
            else
            {
                flags &= ~VariableFlags.Undefined;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this variable as global or non-global.  The global
        /// and local designations are mutually exclusive.
        /// </summary>
        /// <param name="global">
        /// Non-zero to mark this variable as global; zero to clear the global
        /// designation.
        /// </param>
        public void MakeGlobal(
            bool global
            )
        {
            if (global)
            {
                //
                // NOTE: Mutually exclusive with the local flag.
                //
                flags &= ~VariableFlags.Local;
                flags |= VariableFlags.Global;
            }
            else
            {
                flags &= ~VariableFlags.Global;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this variable as local or non-local.  The local
        /// and global designations are mutually exclusive.
        /// </summary>
        /// <param name="local">
        /// Non-zero to mark this variable as local; zero to clear the local
        /// designation.
        /// </param>
        public void MakeLocal(
            bool local
            )
        {
            if (local)
            {
                //
                // NOTE: Mutually exclusive with the global flag.
                //
                flags &= ~VariableFlags.Global;
                flags |= VariableFlags.Local;
            }
            else
            {
                flags &= ~VariableFlags.Local;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this variable to its well-known initial state,
        /// clearing its flags, marks, qualified name, link, value, array
        /// values, traces, and thread lock, and assigning the specified event.
        /// </summary>
        /// <param name="event">
        /// The event used to signal changes to this variable, if any.  This
        /// parameter may be null.
        /// </param>
        public void Reset(
            EventWaitHandle @event
            )
        {
            flags = VariableFlags.None;
            tags = null;
            qualifiedName = null;
            link = null;
            linkIndex = null;
            value = null;
            arrayValue = null;
            traces = null; // BUGBUG: Is this correct (i.e. does Tcl do this)?
            threadId = null;

            this.@event = @event;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the scalar and array values from another variable
        /// into this variable, optionally firing clone traces.  Linked
        /// variables and special variables are handled according to the
        /// specified clone flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to check for special variables and to fire clone
        /// traces.  This parameter may be null.
        /// </param>
        /// <param name="variable">
        /// The variable to copy the scalar and array values from.  This
        /// parameter may be null, in which case null values are copied.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the values are copied (e.g. whether
        /// special variables are allowed and whether traces are fired).
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Continue" /> if this variable is linked and
        /// nothing was copied; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode CopyValueFrom(
            Interpreter interpreter,
            IVariable variable,
            CloneFlags flags,
            ref Result error
            )
        {
            if ((interpreter != null) && !FlagOps.HasFlags(
                    flags, CloneFlags.AllowSpecial, true))
            {
                if (interpreter.IsSpecialVariable(variable))
                {
                    error = "cannot copy from special variable";
                    return ReturnCode.Error;
                }

                if (interpreter.IsSpecialVariable(this))
                {
                    error = "cannot copy to special variable";
                    return ReturnCode.Error;
                }
            }

            if ((link != null) || (linkIndex != null))
                return ReturnCode.Continue;

            object newValue;
            ElementDictionary newValues;

            if (variable != null)
            {
                newValue = variable.Value;
                newValues = variable.ArrayValue;
            }
            else
            {
                newValue = null;
                newValues = null;
            }

            if (!IsLocked() && !Lock(ref error))
                return ReturnCode.Error;

            bool success = false;

            try
            {
                if ((interpreter != null) && FlagOps.HasFlags(
                        flags, CloneFlags.FireTraces, true))
                {
                    Result localResult = null;

                    if (interpreter.FireCloneTraces(
                            BreakpointType.BeforeVariableSet,
                            this.flags, frame, name, null,
                            value, newValue, arrayValue,
                            newValues, variable,
                            ref localResult) != ReturnCode.Ok)
                    {
                        error = localResult;
                        return ReturnCode.Error;
                    }
                }

                value = newValue;
                arrayValue = newValues;
            }
            finally
            {
                success = MaybeUnlock(ref error);
            }

            return success ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this variable, selectively including
        /// its call frame, values, array values, traces, and lock state
        /// according to the specified clone flags, and optionally firing clone
        /// traces.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to check for special variables, obtain the
        /// variable event, and fire clone traces.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control which aspects of this variable are included in
        /// the clone and whether traces are fired.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created variable upon success; otherwise, null.
        /// </returns>
        public IVariable Clone(
            Interpreter interpreter,
            CloneFlags flags,
            ref Result error
            )
        {
            bool withFrames = FlagOps.HasFlags(
                flags, CloneFlags.WithFrames, true);

            bool withValues = FlagOps.HasFlags(
                flags, CloneFlags.WithValues, true);

            bool withArrayValues = FlagOps.HasFlags(
                flags, CloneFlags.WithArrayValues, true);

            bool withTraces = FlagOps.HasFlags(
                flags, CloneFlags.WithTraces, true);

            bool withLocks = FlagOps.HasFlags(
                flags, CloneFlags.WithLocks, true);

            bool fireTraces = FlagOps.HasFlags(
                flags, CloneFlags.FireTraces, true);

            bool allowSpecial = FlagOps.HasFlags(
                flags, CloneFlags.AllowSpecial, true);

            if (!allowSpecial && (interpreter != null) &&
                interpreter.IsSpecialVariable(this))
            {
                error = "cannot clone special variable";
                return null;
            }

            EventWaitHandle newEvent = (interpreter != null) ?
                interpreter.VariableEvent : null;

            ElementDictionary newArrayValue ;

            if (withValues && withArrayValues && (arrayValue != null))
            {
                newArrayValue = new ElementDictionary(
                    @event, arrayValue);
            }
            else
            {
                newArrayValue = null;
            }

            IVariable variable = new Variable(
                withFrames ? frame : null, name, this.flags, withFrames ?
                qualifiedName : null, link, linkIndex, withValues ? value :
                null, newArrayValue, withTraces ? traces : null, withLocks ?
                threadId : null, (newEvent != null) ? newEvent : @event);

            if (fireTraces && (interpreter != null))
            {
                Result localResult = null;

                if (interpreter.FireCloneTraces(
                        BreakpointType.BeforeVariableSet, this.flags,
                        frame, name, null, null, withValues ? value :
                        null, null, newArrayValue, variable,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return null;
                }
            }

            return variable;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method configures this variable as either a scalar or an array,
        /// establishing the appropriate scalar value and/or array element
        /// dictionary and optionally updating the array flag.
        /// </summary>
        /// <param name="newValue">
        /// The new scalar value to assign.  This parameter may be null, in which
        /// case the existing scalar value is left unchanged (unless cleared).
        /// </param>
        /// <param name="union">
        /// Non-zero to clear any value that is incompatible with the requested
        /// scalar or array configuration.
        /// </param>
        /// <param name="array">
        /// Non-zero to configure this variable as an array; zero to configure it
        /// as a scalar.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the existing scalar value or array element
        /// dictionary.
        /// </param>
        /// <param name="flag">
        /// Non-zero to set or unset the array flag to match the requested
        /// configuration.
        /// </param>
        public void SetupValue(
            object newValue,
            bool union,
            bool array,
            bool clear,
            bool flag
            )
        {
            if (array)
            {
                //
                // NOTE: An array variable cannot have a scalar value unless
                //       the caller specifically asks for one.
                //
                if (union && (value != null))
                    value = null;
                else if (newValue != null)
                    value = newValue;

                //
                // NOTE: An array variable must have an array value (element
                //       dictionary).  Only clear it if requested by the
                //       caller.
                //
                if (arrayValue == null)
                    arrayValue = new ElementDictionary(@event);
                else if (clear)
                    arrayValue.Clear();

                //
                // NOTE: Set the array flag?
                //
                if (flag)
                    flags |= VariableFlags.Array;
            }
            else
            {
                //
                // NOTE: A scalar variable cannot have an array value (element
                //       dictionary).
                //
                if (union && (arrayValue != null))
                    arrayValue = null;

                //
                // NOTE: A scalar variable can have a scalar value.  Only clear
                //       it if requested by the caller.  Otherwise, set the new
                //       value if requested by the caller.
                //
                if (clear)
                    value = null;
                else if (newValue != null)
                    value = newValue;

                //
                // NOTE: Unset the array flag?
                //
                if (flag)
                    flags &= ~VariableFlags.Array;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has the specified flags
        /// set.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to test for on this variable.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are set; zero to
        /// require that any of the specified flags is set.
        /// </param>
        /// <returns>
        /// Non-zero if this variable has the specified flags set, subject to
        /// <paramref name="all" />; otherwise, zero.
        /// </returns>
        public bool HasFlags(
            VariableFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or unsets the specified flags on this variable.
        /// </summary>
        /// <param name="flags">
        /// The flags to set or unset on this variable.
        /// </param>
        /// <param name="set">
        /// Non-zero to set the specified flags; zero to unset them.
        /// </param>
        /// <returns>
        /// The resulting flags of this variable after the change.
        /// </returns>
        public VariableFlags SetFlags(
            VariableFlags flags,
            bool set
            )
        {
            if (set)
                return (this.flags |= flags);
            else
                return (this.flags &= ~flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has any traces
        /// associated with it.
        /// </summary>
        /// <returns>
        /// Non-zero if this variable has one or more traces; otherwise, zero.
        /// </returns>
        public bool HasTraces()
        {
            return ((traces != null) && (traces.Count > 0));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears all of the traces associated with this variable.
        /// </summary>
        public void ClearTraces()
        {
            /* IGNORED */
            ResetTraces(true, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified traces to the list of traces
        /// associated with this variable, creating the list when necessary.
        /// </summary>
        /// <param name="traces">
        /// The list of traces to add to this variable.  Null entries within the
        /// list are skipped.
        /// </param>
        /// <returns>
        /// The number of traces that were added.
        /// </returns>
        public int AddTraces(
            TraceList traces
            )
        {
            int result = 0;

            /* IGNORED */
            ResetTraces(true, false);

            if (this.traces != null)
            {
                foreach (ITrace trace in traces)
                {
                    if (trace == null)
                        continue;

                    this.traces.Add(trace);

                    result++;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the collection of marks (tags) for this
        /// variable, creating an empty collection when none exists.
        /// </summary>
        /// <returns>
        /// Non-zero if the collection of marks was created; zero if it already
        /// existed.
        /// </returns>
        public bool InitializeMarks()
        {
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
        /// This method clears all of the marks (tags) from this variable.
        /// </summary>
        /// <returns>
        /// Non-zero if any marks were cleared; otherwise, zero.
        /// </returns>
        public bool ClearMarks()
        {
            if ((tags != null) && (tags.Count > 0))
            {
                tags.Clear();
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has the named mark
        /// (tag).
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.  This parameter should not be null
        /// or an empty string.
        /// </param>
        /// <returns>
        /// Non-zero if this variable has the named mark; otherwise, zero.
        /// </returns>
        public bool HasMark(
            string name
            )
        {
            object value = null;

            return HasMark(name, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has the named mark
        /// (tag) and, when present, interprets its value as a namespace.
        /// </summary>
        /// <param name="name">
        /// The name of the mark to test for.  This parameter should not be null
        /// or an empty string.
        /// </param>
        /// <param name="namespace">
        /// Upon return, when the mark is present, this contains its value
        /// interpreted as an <see cref="INamespace" />, or null when the value
        /// is not a namespace.
        /// </param>
        /// <returns>
        /// Non-zero if this variable has the named mark; otherwise, zero.
        /// </returns>
        public bool HasMark(
            string name,
            ref INamespace @namespace
            )
        {
            object value = null;

            if (HasMark(name, ref value))
            {
                @namespace = value as INamespace;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has the named mark
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
        /// Non-zero if this variable has the named mark; otherwise, zero.
        /// </returns>
        public bool HasMark(
            string name,
            ref ICallFrame frame
            )
        {
            object value = null;

            if (HasMark(name, ref value))
            {
                frame = value as ICallFrame;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has the named mark
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
        /// Non-zero if this variable has the named mark; otherwise, zero.
        /// </returns>
        public bool HasMark(
            string name,
            ref object value
            )
        {
            if (!String.IsNullOrEmpty(name))
            {
                if (tags != null)
                {
                    if (tags.TryGetValue(name, out value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or unsets the named mark (tag) on this variable.
        /// </summary>
        /// <param name="mark">
        /// Non-zero to set the named mark; zero to unset it.
        /// </param>
        /// <param name="name">
        /// The name of the mark to set or unset.  This parameter should not be
        /// null or an empty string.
        /// </param>
        /// <param name="value">
        /// The value to associate with the mark when setting it.  This parameter
        /// may be null.
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
            if (!String.IsNullOrEmpty(name))
            {
                if (tags != null)
                {
                    if (mark && !tags.ContainsKey(name))
                    {
                        tags.Add(name, value);
                        return true;
                    }
                    else if (!mark && tags.ContainsKey(name))
                    {
                        tags.Remove(name);
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the namespace associated with this variable via
        /// its namespace mark (tag).
        /// </summary>
        /// <returns>
        /// The namespace associated with this variable, or null when none is
        /// present.
        /// </returns>
        public INamespace GetNamespaceMark()
        {
            INamespace @namespace = null;

            if (HasMark(NamespaceTagName, ref @namespace))
                return @namespace;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has a namespace mark
        /// (tag) and, optionally, whether it refers to the specified namespace.
        /// </summary>
        /// <param name="namespace">
        /// The namespace to compare against the variable's namespace mark.  This
        /// parameter may be null, in which case the method only checks for the
        /// presence of a namespace mark.
        /// </param>
        /// <returns>
        /// Non-zero if this variable has a namespace mark that matches the
        /// specified namespace (or any namespace when it is null); otherwise,
        /// zero.
        /// </returns>
        public bool HasNamespaceMark(
            INamespace @namespace
            )
        {
            INamespace localNamespace = GetNamespaceMark();

            if (localNamespace != null)
            {
                if (@namespace == null)
                    return true;

                return NamespaceOps.IsSame(localNamespace, @namespace);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method associates the specified namespace with this variable by
        /// setting its namespace mark (tag).
        /// </summary>
        /// <param name="namespace">
        /// The namespace to associate with this variable.  This parameter may be
        /// null, in which case no mark is set.
        /// </param>
        /// <returns>
        /// Non-zero if the namespace mark was set; otherwise, zero.
        /// </returns>
        public bool SetNamespaceMark(
            INamespace @namespace
            )
        {
            if (@namespace == null)
                return false;

            /* IGNORED */
            InitializeMarks();

            return SetMark(true, NamespaceTagName, @namespace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the namespace mark (tag) from this variable,
        /// releasing the collection of marks when it becomes empty.
        /// </summary>
        /// <returns>
        /// Non-zero if the namespace mark was removed; otherwise, zero.
        /// </returns>
        public bool UnsetNamespaceMark()
        {
            try
            {
                return SetMark(false, NamespaceTagName, null);
            }
            finally
            {
                /* IGNORED */
                ResetMarks();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the call frame associated with this variable
        /// via its frame mark (tag).
        /// </summary>
        /// <returns>
        /// The call frame associated with this variable, or null when none is
        /// present.
        /// </returns>
        public ICallFrame GetFrameMark()
        {
            ICallFrame frame = null;

            if (HasMark(FrameTagName, ref frame))
                return frame;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this variable has a frame mark (tag)
        /// and, optionally, whether it refers to the specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to compare against the variable's frame mark.  This
        /// parameter may be null, in which case the method only checks for the
        /// presence of a frame mark.
        /// </param>
        /// <returns>
        /// Non-zero if this variable has a frame mark that matches the specified
        /// call frame (or any call frame when it is null); otherwise, zero.
        /// </returns>
        public bool HasFrameMark(
            ICallFrame frame
            )
        {
            ICallFrame localFrame = GetFrameMark();

            if (localFrame != null)
            {
                if (frame == null)
                    return true;

                return CallFrameOps.IsSame(localFrame, frame);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method associates the specified call frame with this variable by
        /// setting its frame mark (tag).
        /// </summary>
        /// <param name="frame">
        /// The call frame to associate with this variable.  This parameter may
        /// be null, in which case no mark is set.
        /// </param>
        /// <returns>
        /// Non-zero if the frame mark was set; otherwise, zero.
        /// </returns>
        public bool SetFrameMark(
            ICallFrame frame
            )
        {
            if (frame == null)
                return false;

            /* IGNORED */
            InitializeMarks();

            return SetMark(true, FrameTagName, frame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the frame mark (tag) from this variable,
        /// releasing the collection of marks when it becomes empty.
        /// </summary>
        /// <returns>
        /// Non-zero if the frame mark was removed; otherwise, zero.
        /// </returns>
        public bool UnsetFrameMark()
        {
            try
            {
                return SetMark(false, FrameTagName, null);
            }
            finally
            {
                /* IGNORED */
                ResetMarks();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fires the traces associated with this variable for the
        /// specified breakpoint type, executing each enabled trace in turn.
        /// Re-entrant invocation is detected and reported as an error.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation that triggered the traces.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter in whose context the traces are executed.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information passed to each trace callback.  This parameter
        /// may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message; it may also
        /// be set by an individual trace callback.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if all traces completed successfully (or
        /// a trace requested a break); otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode FireTraces(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            if (traces == null)
                return ReturnCode.Ok;

            long levels = PrivateEnterLevel();

            try
            {
                if (levels == 1)
                {
                    VariableFlags savedFlags;

                    BeginNoTrace(out savedFlags);

                    try
                    {
                        foreach (ITrace trace in traces)
                        {
                            if (trace == null)
                                continue;

                            if (EntityOps.IsDisabled(trace))
                                continue;

                            if (traceInfo != null)
                                traceInfo.Trace = trace;

                            ReturnCode code;

                            /* IGNORED */
                            interpreter.EnterTraceLevel();

                            try
                            {
                                code = trace.Execute(
                                    breakpointType, interpreter,
                                    traceInfo, ref result);
                            }
                            catch (Exception e)
                            {
                                result = String.Format(
                                    "caught exception while " +
                                    "firing variable trace: {0}",
                                    e);

                                code = ReturnCode.Error;
                            }
                            finally
                            {
                                /* IGNORED */
                                interpreter.ExitTraceLevel();
                            }

                            if (code == ReturnCode.Break)
                                return ReturnCode.Ok;
                            else if (code != ReturnCode.Ok)
                                return ReturnCode.Error;
                        }

                        return ReturnCode.Ok;
                    }
                    finally
                    {
                        EndNoTrace(ref savedFlags);
                    }
                }
                else
                {
                    result = String.Format(
                        "cannot fire traces, variable {0} is busy",
                        FormatOps.VariableName(name, null));

                    return ReturnCode.Error;
                }
            }
            finally
            {
                PrivateExitLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this variable, based
        /// on its scalar value.
        /// </summary>
        /// <returns>
        /// The string representation of this variable's value, or the default
        /// value when its value is null.
        /// </returns>
        public override string ToString()
        {
            return StringOps.GetStringFromObject(
                value, DefaultValue, !(value is Variable));
        }
        #endregion
    }
}
