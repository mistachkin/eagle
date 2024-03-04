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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e64c8423-691d-4dca-9831-7c2aa66c94f5")]
    public class AliasData : IAliasData
    {
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

        #region IAliasData Members
        private string nameToken;
        public virtual string NameToken
        {
            get { return nameToken; }
            set { nameToken = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter sourceInterpreter;
        public virtual Interpreter SourceInterpreter
        {
            get { return sourceInterpreter; }
            set { sourceInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter targetInterpreter;
        public virtual Interpreter TargetInterpreter
        {
            get { return targetInterpreter; }
            set { targetInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private INamespace sourceNamespace;
        public virtual INamespace SourceNamespace
        {
            get { return sourceNamespace; }
            set { sourceNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private INamespace targetNamespace;
        public virtual INamespace TargetNamespace
        {
            get { return targetNamespace; }
            set { targetNamespace = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IExecute target;
        public virtual IExecute Target
        {
            get { return target; }
            set { target = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private OptionDictionary options;
        public virtual OptionDictionary Options
        {
            get { return options; }
            set { options = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private AliasFlags aliasFlags;
        public virtual AliasFlags AliasFlags
        {
            get { return aliasFlags; }
            set { aliasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int startIndex;
        public virtual int StartIndex
        {
            get { return startIndex; }
            set { startIndex = value; }
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
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
