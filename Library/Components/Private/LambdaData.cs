/*
 * LambdaData.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class holds the data that defines an anonymous procedure (lambda),
    /// including its name, group, description, flags, arguments (raw, named,
    /// overwrite, and clean forms), body, script location, client data, and
    /// token.  It implements <see cref="ILambdaData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c2ef6406-fff9-4fc5-860c-3b16f5df8f37")]
    internal class LambdaData : ILambdaData
    {
        /// <summary>
        /// Constructs a lambda data object from the fully specified set of
        /// identity, argument, body, location, and token parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this lambda.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this lambda.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this lambda.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The procedure flags controlling this lambda's behavior.
        /// </param>
        /// <param name="arguments">
        /// The formal argument list of this lambda.  This parameter may be
        /// null.
        /// </param>
        /// <param name="namedArguments">
        /// The named arguments of this lambda.  This parameter may be null.
        /// </param>
        /// <param name="overwriteArguments">
        /// The arguments that overwrite existing variables when this lambda is
        /// invoked.  This parameter may be null.
        /// </param>
        /// <param name="cleanArguments">
        /// The arguments that are cleaned up after this lambda is invoked.
        /// This parameter may be null.
        /// </param>
        /// <param name="body">
        /// The body (script) of this lambda.  This parameter may be null.
        /// </param>
        /// <param name="location">
        /// The script location associated with this lambda.  This parameter may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this lambda.  This parameter may be
        /// null.
        /// </param>
        /// <param name="token">
        /// The token associated with this lambda.
        /// </param>
        public LambdaData(
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
            this.kind = IdentifierKind.LambdaData;
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
        /// Stores the name of this lambda.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this lambda.
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
        /// Stores the identifier kind of this lambda.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this lambda.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this lambda.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this lambda.
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
        /// Stores the client data associated with this lambda.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this lambda.
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
        /// Stores the group of this lambda.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this lambda.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this lambda.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this lambda.
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
        /// Stores the procedure flags controlling this lambda's behavior.
        /// </summary>
        private ProcedureFlags flags;
        /// <summary>
        /// Gets or sets the procedure flags controlling this lambda's behavior.
        /// </summary>
        public virtual ProcedureFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the formal argument list of this lambda.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the formal argument list of this lambda.
        /// </summary>
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the named arguments of this lambda.
        /// </summary>
        private ArgumentDictionary namedArguments;
        /// <summary>
        /// Gets or sets the named arguments of this lambda.
        /// </summary>
        public virtual ArgumentDictionary NamedArguments
        {
            get { return namedArguments; }
            set { namedArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the arguments that overwrite existing variables when this
        /// lambda is invoked.
        /// </summary>
        private ArgumentList overwriteArguments;
        /// <summary>
        /// Gets or sets the arguments that overwrite existing variables when
        /// this lambda is invoked.
        /// </summary>
        public virtual ArgumentList OverwriteArguments
        {
            get { return overwriteArguments; }
            set { overwriteArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the arguments that are cleaned up after this lambda is
        /// invoked.
        /// </summary>
        private ArgumentList cleanArguments;
        /// <summary>
        /// Gets or sets the arguments that are cleaned up after this lambda is
        /// invoked.
        /// </summary>
        public virtual ArgumentList CleanArguments
        {
            get { return cleanArguments; }
            set { cleanArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the body (script) of this lambda.
        /// </summary>
        private string body;
        /// <summary>
        /// Gets or sets the body (script) of this lambda.
        /// </summary>
        public virtual string Body
        {
            get { return body; }
            set { body = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the script location associated with this lambda.
        /// </summary>
        private IScriptLocation location;
        /// <summary>
        /// Gets or sets the script location associated with this lambda.
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
        /// Stores the token associated with this lambda.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token associated with this lambda.
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
        /// This method produces a string describing this lambda using its name
        /// only.
        /// </summary>
        /// <returns>
        /// A string containing the name of this lambda (or an empty string when
        /// it has no name).
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
