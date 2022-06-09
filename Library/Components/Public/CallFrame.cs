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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("af168784-9b42-40cd-87a6-18eb4c3a663f")]
    public sealed class CallFrame :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ICallFrame, IDisposable /* optional */
    {
        #region Private Constructors
        internal CallFrame(
            long frameId,
            long frameLevel,
            string name,
            ObjectDictionary tags,
            long index,
            long level,
            CallFrameFlags flags,
            IClientData engineData,
            IClientData auxiliaryData,
            IClientData resolveData,
            IClientData extraData,
            VariableDictionary variables,
            IExecute execute,
            ArgumentList arguments,
            bool ownArguments,
            bool newVariables
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
            // NOTE: For now, we always require a valid tags list to be present
            //       (i.e. we always create one if the caller did not supply a
            //       valid one).
            //
            if (this.tags != null)
                this.tags = tags;
            else
                this.tags = new ObjectDictionary();

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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { return false; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
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
            get { CheckDisposed(); return threadId; }
            set { CheckDisposed(); threadId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
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
        public bool Unlock(
            ref Result error
            )
        {
            CheckDisposed();

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
                    error = "call frame already unlocked";
                    return false;
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

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the interpreter lock is already held.
        //
        // TODO: In the future, perhaps add other sanity checks here, e.g. a
        //       disposed IVariable cannot be used?
        //
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
        private long frameId;
        public long FrameId
        {
            get { CheckDisposed(); return frameId; }
            set { CheckDisposed(); frameId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long frameLevel;
        public long FrameLevel
        {
            get { CheckDisposed(); return frameLevel; }
            set { CheckDisposed(); frameLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private CallFrameFlags flags;
        public CallFrameFlags Flags
        {
            get { CheckDisposed(); return flags; }
            set { CheckDisposed(); flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ObjectDictionary tags;
        public ObjectDictionary Tags
        {
            get { CheckDisposed(); return tags; }
            set { CheckDisposed(); tags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long index;
        public long Index
        {
            get { CheckDisposed(); return index; }
            set { CheckDisposed(); index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long level;
        public long Level
        {
            get { CheckDisposed(); return level; }
            set { CheckDisposed(); level = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IExecute execute;
        public IExecute Execute
        {
            get { CheckDisposed(); return execute; }
            set { CheckDisposed(); execute = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
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

        private bool ownArguments;
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

        private ArgumentList procedureArguments;
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

        private VariableDictionary variables;
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

        private ICallFrame other;
        public ICallFrame Other
        {
            get { CheckDisposed(); return other; }
            set { CheckDisposed(); other = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame previous;
        public ICallFrame Previous
        {
            get { CheckDisposed(); return previous; }
            set { CheckDisposed(); previous = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame next;
        public ICallFrame Next
        {
            get { CheckDisposed(); return next; }
            set { CheckDisposed(); next = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData engineData;
        public IClientData EngineData
        {
            get { CheckDisposed(); return engineData; }
            set { CheckDisposed(); engineData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData auxiliaryData;
        public IClientData AuxiliaryData
        {
            get { CheckDisposed(); return auxiliaryData; }
            set { CheckDisposed(); auxiliaryData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData resolveData;
        public IClientData ResolveData
        {
            get { CheckDisposed(); return resolveData; }
            set { CheckDisposed(); resolveData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData extraData;
        public IClientData ExtraData
        {
            get { CheckDisposed(); return extraData; }
            set { CheckDisposed(); extraData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public bool HasMark(
            string name
            )
        {
            CheckDisposed();

            object value = null;

            return HasMark(name, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            CheckDisposed();

            return ToString(DetailFlags.ICallFrameNameOnly);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(CallFrame));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~CallFrame()
        {
            Dispose(false);
        }
        #endregion
    }
}
