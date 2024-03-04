/*
 * PathClientData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("2515085c-2ab4-4a19-bf8c-cee04be8f32b")]
    internal sealed class PathClientData : ClientData, IIdentifier
    {
        #region Public Constructors
        public PathClientData()
        {
            this.kind = IdentifierKind.Path;
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

        public PathClientData(
            int sequence,
            int? index,
            string name,
            string group,
            string description,
            string path
            )
            : this(sequence, index, group)
        {
            this.path = path;

            ///////////////////////////////////////////////////////////////////

            SetName(name, index);
            SetDescription(name, description, index);
        }

        ///////////////////////////////////////////////////////////////////////

        public PathClientData(
            int sequence,
            int? index,
            string name,
            string group,
            Result error
            )
            : this(sequence, index, group)
        {
            this.error = error;

            ///////////////////////////////////////////////////////////////////

            SetName(name, index);
            SetDescription(name, null, index);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private PathClientData(
            int sequence,
            int? index,
            string group
            )
            : this()
        {
            this.sequence = sequence;
            this.index = index;
            this.group = group;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void SetName(
            string name,
            int? index
            )
        {
            this.name = (index != null) ?
                String.Format("{0}({1})", name, index) : name;
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetDescription(
            string name,
            string description,
            int? index
            )
        {
            this.description = (description != null) ? ((index != null) ?
                String.Format("{0} #{1}", description, index) : description) :
                name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private int sequence;
        public int Sequence
        {
            get { return sequence; }
            set { sequence = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int? index;
        public int? Index
        {
            get { return index; }
            set { index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Result error;
        public Result Error
        {
            get { return error; }
            set { error = value; }
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

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
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

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList(sequence, group, name, path);
        }
        #endregion
    }
}
