/*
 * ProcedureData.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents the metadata describing a script procedure defined
    /// within an interpreter, including its identity, flags, formal argument
    /// lists, body, source location, and the token used to identify it.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("84de62ac-7cea-432a-8f10-b5267cab6122")]
    public class ProcedureData : IProcedureData
    {
        /// <summary>
        /// Constructs a procedure data instance from the fully specified set of
        /// identity, flag, argument, body, location, and token parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this procedure.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this procedure.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this procedure.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling this procedure's behavior.
        /// </param>
        /// <param name="arguments">
        /// The formal argument list for this procedure.  This parameter may be
        /// null.
        /// </param>
        /// <param name="namedArguments">
        /// The collection of named formal arguments for this procedure.  This
        /// parameter may be null.
        /// </param>
        /// <param name="overwriteArguments">
        /// The argument list used to overwrite the call frame arguments for this
        /// procedure.  This parameter may be null.
        /// </param>
        /// <param name="cleanArguments">
        /// The cleaned argument list for this procedure.  This parameter may be
        /// null.
        /// </param>
        /// <param name="body">
        /// The script body of this procedure.  This parameter may be null.
        /// </param>
        /// <param name="location">
        /// The source location of this procedure's body.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this procedure, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this procedure within the interpreter.
        /// </param>
        public ProcedureData(
            string name,
            string group,
            string description,
            ProcedureFlags flags,
            ArgumentList arguments,
            ArgumentDictionary namedArguments,
            ArgumentList overwriteArguments,
            ArgumentList cleanArguments,
            string body,
            IScriptLocation location,
            IClientData clientData,
            long token
            )
        {
            this.kind = IdentifierKind.ProcedureData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.flags = flags;
            this.clientData = clientData;
            this.arguments = arguments;
            this.namedArguments = namedArguments;
            this.overwriteArguments = overwriteArguments;
            this.cleanArguments = cleanArguments;
            this.body = body;
            this.location = location;
            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this procedure.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this procedure.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this procedure.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this procedure.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this procedure.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this procedure.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this procedure.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this procedure.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this procedure.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this procedure.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this procedure.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this procedure.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        /// <summary>
        /// Stores the flags controlling this procedure's behavior.
        /// </summary>
        private ProcedureFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling this procedure's behavior.
        /// </summary>
        public virtual ProcedureFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the formal argument list for this procedure.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the formal argument list for this procedure.
        /// </summary>
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of named formal arguments for this procedure.
        /// </summary>
        private ArgumentDictionary namedArguments;
        /// <summary>
        /// Gets or sets the collection of named formal arguments for this
        /// procedure.
        /// </summary>
        public virtual ArgumentDictionary NamedArguments
        {
            get { return namedArguments; }
            set { namedArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the argument list used to overwrite the call frame arguments
        /// for this procedure.
        /// </summary>
        private ArgumentList overwriteArguments;
        /// <summary>
        /// Gets or sets the argument list used to overwrite the call frame
        /// arguments for this procedure.
        /// </summary>
        public virtual ArgumentList OverwriteArguments
        {
            get { return overwriteArguments; }
            set { overwriteArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cleaned argument list for this procedure.
        /// </summary>
        private ArgumentList cleanArguments;
        /// <summary>
        /// Gets or sets the cleaned argument list for this procedure.
        /// </summary>
        public virtual ArgumentList CleanArguments
        {
            get { return cleanArguments; }
            set { cleanArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the script body of this procedure.
        /// </summary>
        private string body;
        /// <summary>
        /// Gets or sets the script body of this procedure.
        /// </summary>
        public virtual string Body
        {
            get { return body; }
            set { body = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the source location of this procedure's body.
        /// </summary>
        private IScriptLocation location;
        /// <summary>
        /// Gets or sets the source location of this procedure's body.
        /// </summary>
        public virtual IScriptLocation Location
        {
            get { return location; }
            set { location = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this procedure within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this procedure within the
        /// interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the name of this procedure, or an empty string
        /// when it has no name.
        /// </summary>
        /// <returns>
        /// The name of this procedure.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
