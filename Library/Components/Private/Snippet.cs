/*
 * Snippet.cs --
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
    /// This class represents a named snippet of content -- a script, a digital
    /// signature, or other text, XML, or binary data -- together with its
    /// identity (name, group, and description), its origin path, and the flags
    /// that control how it may be used.  It implements <see cref="ISnippet" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4233850c-8652-49df-8489-d44e17a8e380")]
    internal sealed class Snippet : ISnippet
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a snippet from its name, grouping, description, origin
        /// path, content, client data, and flags.
        /// </summary>
        /// <param name="name">
        /// The name of this snippet.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group with which this snippet is associated.  This parameter may
        /// be null.
        /// </param>
        /// <param name="description">
        /// The human-readable description of this snippet.  This parameter may
        /// be null.
        /// </param>
        /// <param name="path">
        /// The path identifying where this snippet originated.  This parameter
        /// may be null.
        /// </param>
        /// <param name="bytes">
        /// The binary content of this snippet, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The textual content of this snippet, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="xml">
        /// The XML content of this snippet, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with this snippet, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="snippetFlags">
        /// The flags controlling how this snippet may be used.
        /// </param>
        public Snippet(
            string name,
            string group,
            string description,
            string path,
            byte[] bytes,
            string text,
            string xml,
            IClientData clientData,
            SnippetFlags snippetFlags
            )
        {
            this.kind = IdentifierKind.Snippet;

            if (!FlagOps.HasFlags(
                    snippetFlags, SnippetFlags.NoAttributes, true))
            {
                this.id = AttributeOps.GetObjectId(this);
                this.group = AttributeOps.GetObjectGroups(this);
            }

            EntityOps.MaybeSetGroup(this, group);

            this.name = name;
            this.description = description;
            this.path = path;
            this.bytes = bytes;
            this.text = text;
            this.xml = xml;
            this.clientData = clientData;
            this.snippetFlags = snippetFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method enforces that the existing name of this snippet has not
        /// already been set, throwing if it has.  It is used to guarantee that
        /// the name, once assigned, is immutable.
        /// </summary>
        private void OldNameMustBeImmutable()
        {
            if (name == null)
                return;

            throw new InvalidOperationException("old name is immutable");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this snippet currently has a name.
        /// </summary>
        /// <returns>
        /// True if this snippet has a name; otherwise, false.
        /// </returns>
        private bool PrivateHaveName()
        {
            return (name != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the name of this snippet without performing any
        /// validation or immutability checks.
        /// </summary>
        /// <param name="name">
        /// The name to assign to this snippet.  This parameter may be null.
        /// </param>
        private void PrivateSetName(
            string name /* in */
            )
        {
            this.name = name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method enforces that the supplied new name is valid (i.e. not
        /// null), throwing if it is not.
        /// </summary>
        /// <param name="name">
        /// The candidate new name to validate.
        /// </param>
        private static void NewNameMustBeValid(
            string name /* in */
            )
        {
            if (name != null)
                return;

            throw new InvalidOperationException("new name is invalid");
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method adjusts a set of snippet flags by selectively turning
        /// individual flags on or off and then masks the result to the
        /// per-instance flags.
        /// </summary>
        /// <param name="snippetFlags">
        /// The set of flags to adjust.
        /// </param>
        /// <param name="isScript">
        /// When non-null, controls the <see cref="SnippetFlags.MustBeScript" />
        /// flag: true sets it and false clears it.  This parameter may be null,
        /// leaving the flag unchanged.
        /// </param>
        /// <param name="isSignature">
        /// When non-null, controls the
        /// <see cref="SnippetFlags.MustBeSignature" /> flag: true sets it and
        /// false clears it.  This parameter may be null, leaving the flag
        /// unchanged.
        /// </param>
        /// <param name="forSecurity">
        /// When non-null, controls the
        /// <see cref="SnippetFlags.SecurityPackage" /> flag: true sets it and
        /// false clears it.  This parameter may be null, leaving the flag
        /// unchanged.
        /// </param>
        /// <param name="forInstance">
        /// When non-null, controls the <see cref="SnippetFlags.ForInstance" />
        /// flag: true sets it and false clears it.  This parameter may be null,
        /// leaving the flag unchanged.
        /// </param>
        /// <returns>
        /// The adjusted set of flags, masked to
        /// <see cref="SnippetFlags.InstanceMask" />.
        /// </returns>
        public static SnippetFlags MaskFlags(
            SnippetFlags snippetFlags, /* in */
            bool? isScript,            /* in */
            bool? isSignature,         /* in */
            bool? forSecurity,         /* in */
            bool? forInstance          /* in */
            )
        {
            SnippetFlags result = snippetFlags;

            if (isScript != null)
            {
                if ((bool)isScript)
                    result |= SnippetFlags.MustBeScript;
                else
                    result &= ~SnippetFlags.MustBeScript;
            }

            if (isSignature != null)
            {
                if ((bool)isSignature)
                    result |= SnippetFlags.MustBeSignature;
                else
                    result &= ~SnippetFlags.MustBeSignature;
            }

            if (forSecurity != null)
            {
                if ((bool)forSecurity)
                    result |= SnippetFlags.SecurityPackage;
                else
                    result &= ~SnippetFlags.SecurityPackage;
            }

            if (forInstance != null)
            {
                if ((bool)forInstance)
                    result |= SnippetFlags.ForInstance;
                else
                    result &= ~SnippetFlags.ForInstance;
            }

            return result & SnippetFlags.InstanceMask;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this snippet.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets the name of this snippet.  Setting this property is not
        /// supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this snippet.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets the identifier kind of this snippet.  Setting this property is
        /// not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this snippet.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets the globally unique identifier of this snippet.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the extra data associated with this snippet.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets the extra data associated with this snippet.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group with which this snippet is associated.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets the group with which this snippet is associated.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the human-readable description of this snippet.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets the human-readable description of this snippet.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISnippetData Members
        /// <summary>
        /// Stores the path identifying where this snippet originated.
        /// </summary>
        private string path;
        /// <summary>
        /// Gets the path identifying where this snippet originated.
        /// </summary>
        public string Path
        {
            get { return path; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the binary content of this snippet, if any.
        /// </summary>
        private byte[] bytes;
        /// <summary>
        /// Gets the binary content of this snippet, if any.
        /// </summary>
        public byte[] Bytes
        {
            get { return bytes; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the textual content of this snippet, if any.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets the textual content of this snippet, if any.
        /// </summary>
        public string Text
        {
            get { return text; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the XML content of this snippet, if any.
        /// </summary>
        private string xml;
        /// <summary>
        /// Gets the XML content of this snippet, if any.
        /// </summary>
        public string Xml
        {
            get { return xml; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling how this snippet may be used.
        /// </summary>
        private SnippetFlags snippetFlags;
        /// <summary>
        /// Gets the flags controlling how this snippet may be used.
        /// </summary>
        public SnippetFlags SnippetFlags
        {
            get { return snippetFlags; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISnippet Members
        /// <summary>
        /// This method determines whether this snippet currently has a name.
        /// </summary>
        /// <returns>
        /// True if this snippet has a name; otherwise, false.
        /// </returns>
        public bool HaveName()
        {
            return PrivateHaveName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the name of this snippet only if it does not
        /// already have one.
        /// </summary>
        /// <param name="name">
        /// The name to assign to this snippet.  This parameter may be null.
        /// </param>
        public void MaybeSetName(
            string name /* in */
            )
        {
            if (PrivateHaveName()) return;
            PrivateSetName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the name of this snippet, throwing if it already
        /// has a name or if the supplied name is invalid (i.e. null).
        /// </summary>
        /// <param name="name">
        /// The name to assign to this snippet.
        /// </param>
        public void SetName(
            string name /* in */
            )
        {
            OldNameMustBeImmutable(); /* throw */
            NewNameMustBeValid(name); /* throw */
            PrivateSetName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this snippet is hidden.
        /// </summary>
        /// <returns>
        /// True if this snippet is hidden; otherwise, false.
        /// </returns>
        public bool IsHidden()
        {
            return FlagOps.HasFlags(
                snippetFlags, SnippetFlags.Hidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this snippet as hidden.
        /// </summary>
        public void SetHidden()
        {
            snippetFlags |= SnippetFlags.Hidden;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this snippet is locked.
        /// </summary>
        /// <returns>
        /// True if this snippet is locked; otherwise, false.
        /// </returns>
        public bool IsLocked()
        {
            return FlagOps.HasFlags(
                snippetFlags, SnippetFlags.Locked, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this snippet as locked.
        /// </summary>
        public void SetLocked()
        {
            snippetFlags |= SnippetFlags.Locked;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this snippet is disabled.
        /// </summary>
        /// <returns>
        /// True if this snippet is disabled; otherwise, false.
        /// </returns>
        public bool IsDisabled()
        {
            return FlagOps.HasFlags(
                snippetFlags, SnippetFlags.Disabled, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this snippet as disabled.
        /// </summary>
        public void SetDisabled()
        {
            snippetFlags |= SnippetFlags.Disabled;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs describing the
        /// populated properties of this snippet.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs describing this snippet.
        /// </returns>
        public IStringList ToList()
        {
            IStringList list = new StringPairList();

            if (kind != IdentifierKind.None)
                list.Add("kind", kind.ToString());

            if (!id.Equals(Guid.Empty))
                list.Add("id", id.ToString());

            if (name != null)
                list.Add("name", name);

            if (group != null)
                list.Add("group", group);

            if (description != null)
                list.Add("description", description);

            if (path != null)
                list.Add("path", path);

            if (bytes != null)
                list.Add("bytes", Convert.ToBase64String(bytes));

            if (text != null)
                list.Add("text", text);

            if (xml != null)
                list.Add("xml", xml);

            if (snippetFlags != SnippetFlags.None)
                list.Add("flags", snippetFlags.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        //
        // WARNING: Any changes to this method could break backward
        //          compatibility with previously released versions.
        //
        /// <summary>
        /// Returns a string representation of this snippet, preferring its
        /// name, then its path, then its text, then its XML content.
        /// </summary>
        /// <returns>
        /// The string representation of this snippet, or the empty string if
        /// none of those properties are set.
        /// </returns>
        public override string ToString()
        {
            if (name != null)
                return name;

            if (path != null)
                return path;

            if (text != null)
                return text;

            if (xml != null)
                return xml;

            return String.Empty;
        }
        #endregion
    }
}
