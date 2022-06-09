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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;

#if DEBUGGER && DEBUGGER_ARGUMENTS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Objects
{
    [ObjectId("51d9a798-e19c-479f-b5c3-98459cb21415")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IObject
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static int retryDelay = 50; // delay 50 milliseconds
        private static int retryLimit = 100; // total 5000 milliseconds
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Default(
            IObjectData objectData,
            object value,
            IClientData valueData
            )
        {
            kind = IdentifierKind.Object;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            if (objectData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, objectData.Group);

                name = objectData.Name;
                description = objectData.Description;
                clientData = objectData.ClientData;
                type = objectData.Type;
                alias = objectData.Alias;
                objectFlags = objectData.ObjectFlags;
                referenceCount = objectData.ReferenceCount;
                temporaryReferenceCount = objectData.TemporaryReferenceCount;

#if NATIVE && TCL
                interpName = objectData.InterpName;
#endif

#if DEBUGGER && DEBUGGER_ARGUMENTS
                executeArguments = objectData.ExecuteArguments;
#endif

                token = objectData.Token;
            }

            this.value = value;
            this.valueData = valueData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private int GetAndResetTemporaryReferences()
        {
            int oldCount = Interlocked.CompareExchange(
                ref temporaryReferenceCount, 0, 0);

            if (oldCount > 0)
            {
                return Interlocked.CompareExchange(
                    ref temporaryReferenceCount, 0, oldCount);
            }
            else
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool TryRemoveReferences(
            int removeCount,
            ref int finalCount
            )
        {
            int oldCount = Interlocked.CompareExchange(
                ref referenceCount, 0, 0);

            int newCount = oldCount - removeCount;
            if (newCount < 0) newCount = 0;

            if (Interlocked.CompareExchange(
                    ref referenceCount, newCount,
                    oldCount) == oldCount)
            {
                finalCount = newCount;
                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IValueData Members
        private IClientData valueData;
        public virtual IClientData ValueData
        {
            get { return valueData; }
            set { valueData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData extraData;
        public virtual IClientData ExtraData
        {
            get { return extraData; }
            set { extraData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame callFrame;
        public virtual ICallFrame CallFrame
        {
            get { return callFrame; }
            set { callFrame = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        private object value;
        public virtual object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string String
        {
            get { return (value != null) ? value.ToString() : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int Length
        {
            get { return (value != null) ? value.ToString().Length : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        private ObjectFlags objectFlags;
        public virtual ObjectFlags ObjectFlags
        {
            get { return objectFlags; }
            set { objectFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectData Members
        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IAlias alias;
        public virtual IAlias Alias
        {
            get { return alias; }
            set { alias = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int referenceCount;
        public virtual int ReferenceCount
        {
            get { return referenceCount; }
            set { referenceCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int temporaryReferenceCount;
        public virtual int TemporaryReferenceCount
        {
            get { return temporaryReferenceCount; }
            set { temporaryReferenceCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        private string interpName;
        public virtual string InterpName
        {
            get { return interpName; }
            set { interpName = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_ARGUMENTS
        private ArgumentList executeArguments;
        public virtual ArgumentList ExecuteArguments
        {
            get { return executeArguments; }
            set { executeArguments = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObject Members
        public virtual int AddReference()
        {
            return Interlocked.Increment(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int RemoveReference()
        {
            return Interlocked.Decrement(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int AddTemporaryReference()
        {
            return Interlocked.Increment(ref temporaryReferenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int RemoveTemporaryReference()
        {
            return Interlocked.Decrement(ref temporaryReferenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool RemoveTemporaryReferences(
            Interpreter interpreter,
            string name,
            ref int finalCount
            )
        {
            int removeCount = GetAndResetTemporaryReferences();

            if (removeCount > 0)
            {
                objectFlags &= ~ObjectFlags.TemporaryReturnReference;

                if (!FlagOps.HasFlags(
                        objectFlags, ObjectFlags.Locked, true))
                {
                    int tries = 0;
                    Result error = null;

                    while ((tries == 0) || (Interpreter.EngineReady(
                            interpreter, ReadyFlags.ViaObject,
                            ref error) == ReturnCode.Ok))
                    {
                        if (TryRemoveReferences(
                                removeCount, ref finalCount))
                        {
                            return true;
                        }

                        //
                        // HACK: In general, this should "never" happen;
                        //       however, if it does (i.e. under serious
                        //       multi-threaded stress), deal with it.
                        //
                        tries++;

                        if ((retryLimit >= 0) && /* <0 FOREVER */
                            (tries > retryLimit))
                        {
                            break;
                        }

                        if (retryDelay >= 0) /* <0 NO-DELAY */
                            Thread.Sleep(retryDelay);
                    }

                    //
                    // HACK: Undo the changes already made by this method;
                    //       i.e. restore all removed temporary references.
                    //
                    while (removeCount-- > 0)
                    {
                        /* IGNORED */
                        Interlocked.Increment(ref temporaryReferenceCount);
                    }

                    //
                    // HACK: This code is even less likely to be hit than
                    //       the above retry mechanism; therefore, emit a
                    //       large trace message about it.
                    //
                    TraceOps.DebugTrace(String.Format(
                        "RemoveTemporaryReferences: failed to remove {0} " +
                        "references from interpreter {1} object {2} {3}: {4}",
                        removeCount, FormatOps.InterpreterNoThrow(interpreter),
                        FormatOps.WrapOrNull(name), FormatOps.Tries(tries,
                        retryDelay, retryLimit), FormatOps.WrapOrNull(error)),
                        typeof(Default).Name, TracePriority.CleanupError);
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return name;
        }
        #endregion
    }
}
