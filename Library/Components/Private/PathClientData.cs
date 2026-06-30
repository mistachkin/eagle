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
    /// <summary>
    /// This class represents the client data associated with a path entry,
    /// carrying its sequence number, optional index, name, group, description,
    /// path, and any error encountered while processing it.
    /// </summary>
    [ObjectId("2515085c-2ab4-4a19-bf8c-cee04be8f32b")]
    internal sealed class PathClientData : ClientData, IIdentifier
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with its identifier kind set to
        /// path and its identifier set to the object identifier of this type.
        /// </summary>
        public PathClientData()
        {
            this.kind = IdentifierKind.Path;
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified sequence
        /// number, optional index, name, group, description, and path.
        /// </summary>
        /// <param name="sequence">
        /// The sequence number of the path entry.
        /// </param>
        /// <param name="index">
        /// The optional index of the path entry.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the path entry.
        /// </param>
        /// <param name="group">
        /// The group that the path entry belongs to.
        /// </param>
        /// <param name="description">
        /// The description of the path entry.
        /// </param>
        /// <param name="path">
        /// The path value of the path entry.
        /// </param>
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

        /// <summary>
        /// Constructs an instance of this class with the specified sequence
        /// number, optional index, name, group, and error.
        /// </summary>
        /// <param name="sequence">
        /// The sequence number of the path entry.
        /// </param>
        /// <param name="index">
        /// The optional index of the path entry.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the path entry.
        /// </param>
        /// <param name="group">
        /// The group that the path entry belongs to.
        /// </param>
        /// <param name="error">
        /// The error encountered while processing the path entry.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class with the specified sequence
        /// number, optional index, and group.
        /// </summary>
        /// <param name="sequence">
        /// The sequence number of the path entry.
        /// </param>
        /// <param name="index">
        /// The optional index of the path entry.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group that the path entry belongs to.
        /// </param>
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
        /// <summary>
        /// This method sets the name of the path entry, appending the index in
        /// parentheses when an index is present.
        /// </summary>
        /// <param name="name">
        /// The base name of the path entry.
        /// </param>
        /// <param name="index">
        /// The optional index of the path entry.  When non-null, it is appended
        /// to the name.
        /// </param>
        private void SetName(
            string name,
            int? index
            )
        {
            this.name = (index != null) ?
                String.Format("{0}({1})", name, index) : name;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the description of the path entry, appending the
        /// index when an index is present, or falling back to the name when no
        /// description is provided.
        /// </summary>
        /// <param name="name">
        /// The name of the path entry, used as the description when no
        /// description is provided.
        /// </param>
        /// <param name="description">
        /// The base description of the path entry.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The optional index of the path entry.  When non-null, it is appended
        /// to the description.
        /// </param>
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
        /// <summary>
        /// The sequence number of the path entry.
        /// </summary>
        private int sequence;
        /// <summary>
        /// Gets or sets the sequence number of the path entry.
        /// </summary>
        public int Sequence
        {
            get { return sequence; }
            set { sequence = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The optional index of the path entry.
        /// </summary>
        private int? index;
        /// <summary>
        /// Gets or sets the optional index of the path entry.
        /// </summary>
        public int? Index
        {
            get { return index; }
            set { index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The path value of the path entry.
        /// </summary>
        private string path;
        /// <summary>
        /// Gets or sets the path value of the path entry.
        /// </summary>
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The error encountered while processing the path entry.
        /// </summary>
        private Result error;
        /// <summary>
        /// Gets or sets the error encountered while processing the path entry.
        /// </summary>
        public Result Error
        {
            get { return error; }
            set { error = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of the path entry.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the path entry.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The identifier kind of the path entry.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of the path entry.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier of the path entry.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier of the path entry.
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with the path entry.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the path entry.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The group that the path entry belongs to.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group that the path entry belongs to.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of the path entry.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of the path entry.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of the path entry,
        /// composed of its sequence number, group, name, and path.
        /// </summary>
        /// <returns>
        /// The string representation of the path entry.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(sequence, group, name, path);
        }
        #endregion
    }
}
