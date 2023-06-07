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
    [ObjectId("ec135739-c556-422f-b396-314d27a556a9")]
    public sealed class Variable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IVariable
    {
        #region Private Constants
        internal static readonly string DefaultValue = String.Empty;

        ///////////////////////////////////////////////////////////////////////

        private const string NamespaceTagName = "@namespace";
        private const string FrameTagName = "@frame";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private EventWaitHandle @event;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
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
        internal bool HasTracesWithSideEffects(
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
                    Interpreter.IsObjectTraceCallback(callback))
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

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
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

        private bool ResetMarks()
        {
            if ((tags == null) || (tags.Count > 0))
                return false;

            tags = null;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadLock Members
        //
        // NOTE: This property is really for external use only.  Also, it
        //       should not be used to actually set the associated value,
        //       except under a few very rare sets of circumstances.
        //
        private long? threadId;
        public long? ThreadId
        {
            get { return threadId; }
            set { threadId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
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
        public bool Unlock(
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
                    error = "variable already unlocked";
                    return false;
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

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        // TODO: In the future, perhaps add other sanity checks here, e.g. a
        //       disposed IVariable cannot be used?
        //
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

        #region IVariable Members
        private ICallFrame frame;
        public ICallFrame Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private VariableFlags flags;
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

        private ObjectDictionary tags;
        public ObjectDictionary Tags
        {
            get { return tags; }
            set { tags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string qualifiedName;
        public string QualifiedName
        {
            get { return qualifiedName; }
            set { qualifiedName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IVariable link;
        public IVariable Link
        {
            get { return link; }
            set { link = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string linkIndex;
        public string LinkIndex
        {
            get { return linkIndex; }
            set { linkIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object value;
        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ElementDictionary arrayValue;
        public ElementDictionary ArrayValue
        {
            get { return arrayValue; }
            set { arrayValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TraceList traces;
        public TraceList Traces
        {
            get { return traces; }
            set { traces = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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

            if (!Lock(ref error))
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
                success = Unlock(ref error);
            }

            return success ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

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

            IVariable variable = new Variable(
                withFrames ? frame : null, name, this.flags,
                withFrames ? qualifiedName : null,
                link, linkIndex, withValues ? value : null,
                withValues ? arrayValue : null, withTraces ?
                traces : null, withLocks ? threadId : null,
                (newEvent != null) ? newEvent : this.@event);

            if (fireTraces && (interpreter != null))
            {
                Result localResult = null;

                if (interpreter.FireCloneTraces(
                        BreakpointType.BeforeVariableSet,
                        this.flags, frame, name, null, null,
                        value, null, arrayValue, variable,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return null;
                }
            }

            return variable;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public bool HasFlags(
            VariableFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public bool HasTraces()
        {
            return ((traces != null) && (traces.Count > 0));
        }

        ///////////////////////////////////////////////////////////////////////

        public void ClearTraces()
        {
            /* IGNORED */
            ResetTraces(true, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public bool HasMark(
            string name
            )
        {
            object value = null;

            return HasMark(name, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public INamespace GetNamespaceMark()
        {
            INamespace @namespace = null;

            if (HasMark(NamespaceTagName, ref @namespace))
                return @namespace;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public ICallFrame GetFrameMark()
        {
            ICallFrame frame = null;

            if (HasMark(FrameTagName, ref frame))
                return frame;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public ReturnCode FireTraces(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (traces != null)
            {
                //
                // NOTE: Save the current variable flags.
                //
                VariableFlags savedFlags = flags;

                //
                // NOTE: Prevent endless trace recursion.
                //
                flags |= VariableFlags.NoTrace;

                try
                {
                    //
                    // NOTE: Process each trace (as long as they all continue
                    //       to succeed).
                    //
                    foreach (ITrace trace in traces)
                    {
                        if ((trace != null) && !EntityOps.IsDisabled(trace))
                        {
                            //
                            // NOTE: If possible, set the Trace property of the
                            //       TraceInfo to the one we are about to execute.
                            //
                            if (traceInfo != null)
                                traceInfo.Trace = trace;

                            //
                            // NOTE: Since variable traces can basically do anything
                            //       they want, we wrap them in a try block to prevent
                            //       exceptions from escaping.
                            //
                            interpreter.EnterTraceLevel();

                            try
                            {
                                code = trace.Execute(
                                    breakpointType, interpreter, traceInfo, ref result);
                            }
                            catch (Exception e)
                            {
                                //
                                // NOTE: Translate exceptions to a failure return.
                                //
                                result = String.Format(
                                    "caught exception while firing variable trace: {0}",
                                    e);

                                code = ReturnCode.Error;
                            }
                            finally
                            {
                                interpreter.ExitTraceLevel();
                            }

                            //
                            // NOTE: Check for exception results specially because we
                            //       treat "Break" different from other return codes.
                            //
                            if (code == ReturnCode.Break)
                            {
                                //
                                // NOTE: Success; however, skip processing further
                                //       traces for this variable operation.
                                //
                                code = ReturnCode.Ok;
                                break;
                            }
                            else if (code != ReturnCode.Ok)
                            {
                                //
                                // NOTE: Some type of failure (or exception), stop
                                //       processing for this variable operation.
                                //
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    //
                    // NOTE: Restore the saved variable flags.
                    //
                    flags = savedFlags;
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringOps.GetStringFromObject(
                value, DefaultValue, !(value is Variable));
        }
        #endregion
    }
}
