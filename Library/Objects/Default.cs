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
    /// <summary>
    /// This class is the default implementation of <see cref="IObject" />, the
    /// metadata wrapper used by Eagle to represent an opaque object handle.  An
    /// instance bundles the wrapped value together with its identity, type,
    /// alias, flags, client data, and reference counts so that the interpreter
    /// can track and manage the lifetime of native objects exposed to scripts.
    /// It is the type normally created when a value is added to an interpreter's
    /// object collection.
    /// </summary>
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
        /// <summary>
        /// The number of milliseconds to sleep between successive attempts to
        /// remove temporary references when contention is encountered.
        /// </summary>
        private static int retryDelay = 50; // delay 50 milliseconds

        /// <summary>
        /// The maximum number of times to retry removing temporary references
        /// before giving up; a value less than zero means retry forever.
        /// </summary>
        private static int retryLimit = 100; // total 5000 milliseconds
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Non-zero if this object is currently in the process of being
        /// disposed.
        /// </summary>
        private bool disposing;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an object wrapper, copying its identity, type, flags, and
        /// other metadata from the supplied object data and associating it with
        /// the specified wrapped value and value client data.
        /// </summary>
        /// <param name="objectData">
        /// The object data used to initialize this object's metadata.  This
        /// parameter may be null, in which case default metadata is used.
        /// </param>
        /// <param name="value">
        /// The value to be wrapped by this object.  This parameter may be null.
        /// </param>
        /// <param name="valueData">
        /// The client data to associate with the wrapped value.  This parameter
        /// may be null.
        /// </param>
        public Default(
            IObjectData objectData,
            object value,
            IClientData valueData
            )
        {
            kind = IdentifierKind.Object;

            if ((objectData == null) ||
                !FlagOps.HasFlags(objectData.ObjectFlags,
                    ObjectFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (objectData != null)
            {
                id = objectData.Id;

                EntityOps.MaybeSetupId(this);

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
        /// <summary>
        /// This method atomically reads the current temporary reference count
        /// and, if it is positive, resets it to zero.
        /// </summary>
        /// <returns>
        /// The temporary reference count that was in effect before it was reset,
        /// or zero if the count was not positive.
        /// </returns>
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

        /// <summary>
        /// This method attempts to atomically subtract the specified number of
        /// references from this object's reference count, clamping the result to
        /// a minimum of zero.  The operation fails (without modifying the count)
        /// if another thread changes the count concurrently.
        /// </summary>
        /// <param name="removeCount">
        /// The number of references to remove from the reference count.
        /// </param>
        /// <param name="finalCount">
        /// Upon success, receives the resulting reference count after the
        /// references were removed.
        /// </param>
        /// <returns>
        /// True if the references were removed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// The name of this object.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name of this object.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The identifier kind of this object.
        /// </summary>
        private IdentifierKind kind;

        /// <summary>
        /// Gets or sets the identifier kind of this object.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The globally unique identifier of this object.
        /// </summary>
        private Guid id;

        /// <summary>
        /// Gets or sets the globally unique identifier of this object.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this object.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the client data associated with this object.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The group of this object.
        /// </summary>
        private string group;

        /// <summary>
        /// Gets or sets the group of this object.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of this object.
        /// </summary>
        private string description;

        /// <summary>
        /// Gets or sets the description of this object.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IValueData Members
        /// <summary>
        /// The client data associated with the wrapped value of this object.
        /// </summary>
        private IClientData valueData;

        /// <summary>
        /// Gets or sets the client data associated with the wrapped value of
        /// this object.
        /// </summary>
        public virtual IClientData ValueData
        {
            get { return valueData; }
            set { valueData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The extra client data associated with this object.
        /// </summary>
        private IClientData extraData;

        /// <summary>
        /// Gets or sets the extra client data associated with this object.
        /// </summary>
        public virtual IClientData ExtraData
        {
            get { return extraData; }
            set { extraData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The call frame associated with this object, if any.
        /// </summary>
        private ICallFrame callFrame;

        /// <summary>
        /// Gets or sets the call frame associated with this object.
        /// </summary>
        public virtual ICallFrame CallFrame
        {
            get { return callFrame; }
            set { callFrame = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        /// <summary>
        /// The value wrapped by this object.
        /// </summary>
        private object value;

        /// <summary>
        /// Gets or sets the value wrapped by this object.
        /// </summary>
        public virtual object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the string representation of the wrapped value, or null if the
        /// wrapped value is null.
        /// </summary>
        public virtual string String
        {
            get { return (value != null) ? value.ToString() : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length of the string representation of the wrapped value, or
        /// zero if the wrapped value is null.
        /// </summary>
        public virtual int Length
        {
            get { return (value != null) ? value.ToString().Length : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        /// <summary>
        /// The flags that control the behavior of this object.
        /// </summary>
        private ObjectFlags objectFlags;

        /// <summary>
        /// Gets or sets the flags that control the behavior of this object.
        /// </summary>
        public virtual ObjectFlags ObjectFlags
        {
            get { return objectFlags; }
            set { objectFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectData Members
        /// <summary>
        /// The managed type of the wrapped value of this object.
        /// </summary>
        private Type type;

        /// <summary>
        /// Gets or sets the managed type of the wrapped value of this object.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The command alias associated with this object, if any.
        /// </summary>
        private IAlias alias;

        /// <summary>
        /// Gets or sets the command alias associated with this object.
        /// </summary>
        public virtual IAlias Alias
        {
            get { return alias; }
            set { alias = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of outstanding references to this object.
        /// </summary>
        private int referenceCount;

        /// <summary>
        /// Gets or sets the number of outstanding references to this object.
        /// </summary>
        public virtual int ReferenceCount
        {
            get { return referenceCount; }
            set { referenceCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of outstanding temporary references to this object.
        /// </summary>
        private int temporaryReferenceCount;

        /// <summary>
        /// Gets or sets the number of outstanding temporary references to this
        /// object.
        /// </summary>
        public virtual int TemporaryReferenceCount
        {
            get { return temporaryReferenceCount; }
            set { temporaryReferenceCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// The name of the native Tcl interpreter associated with this object,
        /// if any.
        /// </summary>
        private string interpName;

        /// <summary>
        /// Gets or sets the name of the native Tcl interpreter associated with
        /// this object.
        /// </summary>
        public virtual string InterpName
        {
            get { return interpName; }
            set { interpName = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_ARGUMENTS
        /// <summary>
        /// The arguments captured for use by the script debugger when this
        /// object is executed, if any.
        /// </summary>
        private ArgumentList executeArguments;

        /// <summary>
        /// Gets or sets the arguments captured for use by the script debugger
        /// when this object is executed.
        /// </summary>
        public virtual ArgumentList ExecuteArguments
        {
            get { return executeArguments; }
            set { executeArguments = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObject Members
        /// <summary>
        /// This method atomically increments the reference count of this object.
        /// </summary>
        /// <returns>
        /// The resulting reference count after it was incremented.
        /// </returns>
        public virtual int AddReference()
        {
            return Interlocked.Increment(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically decrements the reference count of this object.
        /// </summary>
        /// <returns>
        /// The resulting reference count after it was decremented.
        /// </returns>
        public virtual int RemoveReference()
        {
            return Interlocked.Decrement(ref referenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically increments the temporary reference count of
        /// this object.
        /// </summary>
        /// <returns>
        /// The resulting temporary reference count after it was incremented.
        /// </returns>
        public virtual int AddTemporaryReference()
        {
            return Interlocked.Increment(ref temporaryReferenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically decrements the temporary reference count of
        /// this object.
        /// </summary>
        /// <returns>
        /// The resulting temporary reference count after it was decremented.
        /// </returns>
        public virtual int RemoveTemporaryReference()
        {
            return Interlocked.Decrement(ref temporaryReferenceCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all outstanding temporary references from this
        /// object, retrying as necessary to cope with concurrent modification,
        /// and restoring the removed references if the operation cannot be
        /// completed.  References are not removed while the object is locked.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to check engine readiness between retries.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the object, used only for diagnostic trace output.  This
        /// parameter may be null.
        /// </param>
        /// <param name="finalCount">
        /// Upon success, receives the resulting reference count after the
        /// temporary references were removed.
        /// </param>
        /// <returns>
        /// True if the temporary references were removed; otherwise, false.
        /// </returns>
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
                            interpreter, null, ReadyFlags.ViaObject,
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
                        {
                            try
                            {
                                HostOps.ThreadSleep(retryDelay); /* throw */
                            }
                            catch (ThreadAbortException e)
                            {
                                Thread.ResetAbort();

                                TraceOps.DebugTrace(
                                    e, typeof(Default).Name,
                                    TracePriority.ThreadError2);
                            }
                            catch (ThreadInterruptedException e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(Default).Name,
                                    TracePriority.ThreadError2);
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(Default).Name,
                                    TracePriority.ThreadError);
                            }
                        }
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

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets or sets a value indicating whether this object has been
        /// disposed.
        /// </summary>
        public virtual bool Disposed
        {
            get { return disposed; }
            set { disposed = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this object is currently in
        /// the process of being disposed.
        /// </summary>
        public virtual bool Disposing
        {
            get { return disposing; }
            set { disposing = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The token that uniquely identifies this object within its containing
        /// collection.
        /// </summary>
        private long token;

        /// <summary>
        /// Gets or sets the token that uniquely identifies this object within
        /// its containing collection.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this object.
        /// </summary>
        /// <returns>
        /// The name of this object.
        /// </returns>
        public override string ToString()
        {
            return name;
        }
        #endregion
    }
}
