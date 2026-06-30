/*
 * AliasData.cs --
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
    /// This class holds the data describing a single command alias within an
    /// Eagle interpreter -- the mapping of a command name to an executable
    /// entity, together with the source and target interpreters and
    /// namespaces, any pre-bound arguments and options, the alias flags, and
    /// the associated token.  It implements <see cref="IAliasData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e64c8423-691d-4dca-9831-7c2aa66c94f5")]
    public class AliasData : IAliasData
    {
        /// <summary>
        /// Constructs an alias data object from the specified identity,
        /// scoping, target, argument, and option parameters.
        /// </summary>
        /// <param name="nameToken">
        /// The name token identifying this alias.  This parameter may be null.
        /// </param>
        /// <param name="sourceInterpreter">
        /// The interpreter in which this alias is defined.  This parameter may
        /// be null.
        /// </param>
        /// <param name="targetInterpreter">
        /// The interpreter in which the target of this alias is executed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="sourceNamespace">
        /// The namespace in which this alias is defined.  This parameter may be
        /// null.
        /// </param>
        /// <param name="targetNamespace">
        /// The namespace in which the target of this alias resides.  This
        /// parameter may be null.
        /// </param>
        /// <param name="target">
        /// The executable entity invoked by this alias.  This parameter may be
        /// null.
        /// </param>
        /// <param name="arguments">
        /// The arguments to be prepended when this alias is invoked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="options">
        /// The options associated with this alias.  This parameter may be null.
        /// </param>
        /// <param name="aliasFlags">
        /// The flags controlling this alias's behavior.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first argument to be passed through to the target
        /// when this alias is invoked.
        /// </param>
        /// <param name="token">
        /// The token associated with this alias.
        /// </param>
        public AliasData(
            string nameToken,
            Interpreter sourceInterpreter,
            Interpreter targetInterpreter,
            INamespace sourceNamespace,
            INamespace targetNamespace,
            IExecute target,
            ArgumentList arguments,
            OptionDictionary options,
            AliasFlags aliasFlags,
            int startIndex,
            long token
            )
        {
            this.kind = IdentifierKind.AliasData;
            this.id = AttributeOps.GetObjectId(this);
            this.nameToken = nameToken;
            this.sourceInterpreter = sourceInterpreter;
            this.targetInterpreter = targetInterpreter;
            this.sourceNamespace = sourceNamespace;
            this.targetNamespace = targetNamespace;
            this.target = target;
            this.arguments = arguments;
            this.options = options;
            this.aliasFlags = aliasFlags;
            this.startIndex = startIndex;
            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this alias.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this alias.
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
        /// The identifier kind of this alias.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this alias.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The globally unique identifier of this alias.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this alias.
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
        /// The client data associated with this alias, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this alias.
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
        /// The group of this alias, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this alias.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of this alias, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this alias.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAliasData Members
        /// <summary>
        /// The name token identifying this alias.
        /// </summary>
        private string nameToken;
        /// <summary>
        /// Gets or sets the name token identifying this alias.
        /// </summary>
        public virtual string NameToken
        {
            get { return nameToken; }
            set { nameToken = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter in which this alias is defined.
        /// </summary>
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter sourceInterpreter;
        /// <summary>
        /// Gets or sets the interpreter in which this alias is defined.
        /// </summary>
        public virtual Interpreter SourceInterpreter
        {
            get { return sourceInterpreter; }
            set { sourceInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter in which the target of this alias is executed.
        /// </summary>
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter targetInterpreter;
        /// <summary>
        /// Gets or sets the interpreter in which the target of this alias is
        /// executed.
        /// </summary>
        public virtual Interpreter TargetInterpreter
        {
            get { return targetInterpreter; }
            set { targetInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The namespace in which this alias is defined.
        /// </summary>
        private INamespace sourceNamespace;
        /// <summary>
        /// Gets or sets the namespace in which this alias is defined.
        /// </summary>
        public virtual INamespace SourceNamespace
        {
            get { return sourceNamespace; }
            set { sourceNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The namespace in which the target of this alias resides.
        /// </summary>
        private INamespace targetNamespace;
        /// <summary>
        /// Gets or sets the namespace in which the target of this alias
        /// resides.
        /// </summary>
        public virtual INamespace TargetNamespace
        {
            get { return targetNamespace; }
            set { targetNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The executable entity invoked by this alias.
        /// </summary>
        private IExecute target;
        /// <summary>
        /// Gets or sets the executable entity invoked by this alias.
        /// </summary>
        public virtual IExecute Target
        {
            get { return target; }
            set { target = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The arguments to be prepended when this alias is invoked.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the arguments to be prepended when this alias is
        /// invoked.
        /// </summary>
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The options associated with this alias.
        /// </summary>
        private OptionDictionary options;
        /// <summary>
        /// Gets or sets the options associated with this alias.
        /// </summary>
        public virtual OptionDictionary Options
        {
            get { return options; }
            set { options = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags controlling this alias's behavior.
        /// </summary>
        private AliasFlags aliasFlags;
        /// <summary>
        /// Gets or sets the flags controlling this alias's behavior.
        /// </summary>
        public virtual AliasFlags AliasFlags
        {
            get { return aliasFlags; }
            set { aliasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The index of the first argument to be passed through to the target
        /// when this alias is invoked.
        /// </summary>
        private int startIndex;
        /// <summary>
        /// Gets or sets the index of the first argument to be passed through to
        /// the target when this alias is invoked.
        /// </summary>
        public virtual int StartIndex
        {
            get { return startIndex; }
            set { startIndex = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The token associated with this alias.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token associated with this alias.
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
        /// This method returns the name of this alias, or an empty string when
        /// it has no name.
        /// </summary>
        /// <returns>
        /// The name of this alias, or an empty string.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
