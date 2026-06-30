/*
 * ObjectData.cs --
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

#if DEBUGGER && DEBUGGER_ARGUMENTS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class holds the data that describes an opaque object handle managed
    /// by an Eagle interpreter, including its identity, the wrapped managed
    /// object's type, its alias, its flags, its reference counts, and its
    /// token.  It implements <see cref="IObjectData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("ba45a840-b13c-4a30-9f55-ba5766d66a93")]
    public class ObjectData : IObjectData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty object data instance, assigning its identifier
        /// kind and globally unique identifier.
        /// </summary>
        public ObjectData()
        {
            this.kind = IdentifierKind.ObjectData;
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an object data instance from the fully specified set of
        /// identity, disposal, type, alias, flag, reference count, and token
        /// parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this object.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this object.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this object.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this object, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="disposed">
        /// Non-zero if the wrapped object has been disposed.
        /// </param>
        /// <param name="disposing">
        /// Non-zero if the wrapped object is currently being disposed.
        /// </param>
        /// <param name="type">
        /// The type of the wrapped managed object.  This parameter may be null.
        /// </param>
        /// <param name="alias">
        /// The alias used to invoke members of the wrapped object, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="objectFlags">
        /// The flags controlling this object's behavior.
        /// </param>
        /// <param name="referenceCount">
        /// The number of outstanding references to this object.
        /// </param>
        /// <param name="temporaryReferenceCount">
        /// The number of outstanding temporary references to this object.
        /// </param>
        /// <param name="interpName">
        /// The name of the associated native Tcl interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="executeArguments">
        /// The script arguments used when the object was created or shared.
        /// This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this object within the interpreter.
        /// </param>
        public ObjectData(
            string name,
            string group,
            string description,
            IClientData clientData,
            bool disposed,
            bool disposing,
            Type type,
            IAlias alias,
            ObjectFlags objectFlags,
            int referenceCount,
            int temporaryReferenceCount,
#if NATIVE && TCL
            string interpName,
#endif
#if DEBUGGER && DEBUGGER_ARGUMENTS
            ArgumentList executeArguments,
#endif
            long token
            )
            : this()
        {
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.disposed = disposed;
            this.disposing = disposing;
            this.type = type;
            this.alias = alias;
            this.objectFlags = objectFlags;
            this.referenceCount = referenceCount;
            this.temporaryReferenceCount = temporaryReferenceCount;

#if NATIVE && TCL
            this.interpName = interpName;
#endif

#if DEBUGGER && DEBUGGER_ARGUMENTS
            this.executeArguments = executeArguments;
#endif

            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an object data instance by copying the values from the
        /// specified object data.
        /// </summary>
        /// <param name="objectData">
        /// The object data whose values are copied into the new instance.  This
        /// parameter may be null.
        /// </param>
        public ObjectData(
            IObjectData objectData
            )
            : this()
        {
            if (objectData != null)
            {
                name = objectData.Name;
                group = objectData.Group;
                description = objectData.Description;
                clientData = objectData.ClientData;
                disposed = objectData.Disposed;
                disposing = objectData.Disposing;
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
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a copy of the specified object data suitable for
        /// sharing between two interpreters, resetting the per-interpreter state
        /// (e.g. type information across application domains, reference counts,
        /// and the native Tcl interpreter name) and flagging the result as a
        /// shared object.
        /// </summary>
        /// <param name="interpreter1">
        /// The first interpreter involved in the sharing operation.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter2">
        /// The second interpreter involved in the sharing operation.  This
        /// parameter may be null.
        /// </param>
        /// <param name="objectData">
        /// The object data to copy for sharing.  This parameter may be null.
        /// </param>
        /// <param name="executeArguments">
        /// The script arguments used when the object was shared.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The newly created object data suitable for sharing.
        /// </returns>
        internal static IObjectData CreateForSharing(
            Interpreter interpreter1,
            Interpreter interpreter2,
            IObjectData objectData
#if DEBUGGER && DEBUGGER_ARGUMENTS
            , ArgumentList executeArguments
#endif
            )
        {
            IObjectData result = new ObjectData(objectData);

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: If the interpreters are in different application domains,
            //       it may be impossible to share the type information.
            //
            if (AppDomainOps.IsCross(interpreter1, interpreter2))
                result.Type = null;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This object will be shared between interpreters.  Flag it
            //       as such.
            //
            result.ObjectFlags |= ObjectFlags.SharedObject;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The reference counts for a given opaque object handle are
            //       always per-interpreter.  Therefore, this one will start
            //       with counts of zero.
            //
            result.ReferenceCount = 0;
            result.TemporaryReferenceCount = 0;

            ///////////////////////////////////////////////////////////////////

#if NATIVE && TCL
            //
            // NOTE: The associated native Tcl interpreter name is also dealt
            //       with on a per-interpreter basis.
            //
            result.InterpName = null;
#endif

            ///////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_ARGUMENTS
            //
            // NOTE: This should record the script arguments used when the
            //       object was shared (e.g. via the [interp shareobject]
            //       sub-command).
            //
            result.ExecuteArguments = executeArguments;
#endif

            ///////////////////////////////////////////////////////////////////

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this object.
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
        /// Stores the identifier kind of this object.
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
        /// Stores the globally unique identifier of this object.
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
        /// Stores the client data associated with this object.
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
        /// Stores the group of this object.
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
        /// Stores the description of this object.
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

        #region IHaveObjectFlags Members
        /// <summary>
        /// Stores the flags controlling this object's behavior.
        /// </summary>
        private ObjectFlags objectFlags;
        /// <summary>
        /// Gets or sets the flags controlling this object's behavior.
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
        /// Stores a value indicating whether the wrapped object has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// Gets or sets a value indicating whether the wrapped object has been
        /// disposed.
        /// </summary>
        public virtual bool Disposed
        {
            get { return disposed; }
            set { disposed = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the wrapped object is currently
        /// being disposed.
        /// </summary>
        private bool disposing;
        /// <summary>
        /// Gets or sets a value indicating whether the wrapped object is
        /// currently being disposed.
        /// </summary>
        public virtual bool Disposing
        {
            get { return disposing; }
            set { disposing = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the type of the wrapped managed object.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type of the wrapped managed object.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the alias used to invoke members of the wrapped object.
        /// </summary>
        private IAlias alias;
        /// <summary>
        /// Gets or sets the alias used to invoke members of the wrapped object.
        /// </summary>
        public virtual IAlias Alias
        {
            get { return alias; }
            set { alias = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of outstanding references to this object.
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
        /// Stores the number of outstanding temporary references to this object.
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
        /// Stores the name of the associated native Tcl interpreter.
        /// </summary>
        private string interpName;
        /// <summary>
        /// Gets or sets the name of the associated native Tcl interpreter.
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
        /// Stores the script arguments used when the object was created or
        /// shared.
        /// </summary>
        private ArgumentList executeArguments;
        /// <summary>
        /// Gets or sets the script arguments used when the object was created or
        /// shared.
        /// </summary>
        public virtual ArgumentList ExecuteArguments
        {
            get { return executeArguments; }
            set { executeArguments = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this object within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this object within the
        /// interpreter.
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
        /// This method returns the name of this object, or an empty string when
        /// it has no name.
        /// </summary>
        /// <returns>
        /// The name of this object, or an empty string when it has no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
