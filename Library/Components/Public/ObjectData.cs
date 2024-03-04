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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("ba45a840-b13c-4a30-9f55-ba5766d66a93")]
    public class ObjectData : IObjectData
    {
        #region Public Constructors
        public ObjectData()
        {
            this.kind = IdentifierKind.ObjectData;
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectData(
            string name,
            string group,
            string description,
            IClientData clientData,
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

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
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
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
