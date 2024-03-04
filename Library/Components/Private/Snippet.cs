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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4233850c-8652-49df-8489-d44e17a8e380")]
    internal sealed class Snippet : ISnippet
    {
        #region Public Constructors
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
        private void OldNameMustBeImmutable()
        {
            if (name == null)
                return;

            throw new InvalidOperationException("old name is immutable");
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateHaveName()
        {
            return (name != null);
        }

        ///////////////////////////////////////////////////////////////////////

        private void PrivateSetName(
            string name /* in */
            )
        {
            this.name = name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
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
        private string name;
        public string Name
        {
            get { return name; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { return group; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { return description; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISnippetData Members
        private string path;
        public string Path
        {
            get { return path; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] bytes;
        public byte[] Bytes
        {
            get { return bytes; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string xml;
        public string Xml
        {
            get { return xml; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SnippetFlags snippetFlags;
        public SnippetFlags SnippetFlags
        {
            get { return snippetFlags; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISnippet Members
        public bool HaveName()
        {
            return PrivateHaveName();
        }

        ///////////////////////////////////////////////////////////////////////

        public void MaybeSetName(
            string name /* in */
            )
        {
            if (PrivateHaveName()) return;
            PrivateSetName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetName(
            string name /* in */
            )
        {
            OldNameMustBeImmutable(); /* throw */
            NewNameMustBeValid(name); /* throw */
            PrivateSetName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsHidden()
        {
            return FlagOps.HasFlags(
                snippetFlags, SnippetFlags.Hidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetHidden()
        {
            snippetFlags |= SnippetFlags.Hidden;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsLocked()
        {
            return FlagOps.HasFlags(
                snippetFlags, SnippetFlags.Locked, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetLocked()
        {
            snippetFlags |= SnippetFlags.Locked;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDisabled()
        {
            return FlagOps.HasFlags(
                snippetFlags, SnippetFlags.Disabled, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetDisabled()
        {
            snippetFlags |= SnippetFlags.Disabled;
        }

        ///////////////////////////////////////////////////////////////////////

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
