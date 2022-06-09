/*
 * Script.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;

#if CAS_POLICY
using System.Security.Cryptography;
using System.Security.Policy;
#endif

#if XML
using System.Xml;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _ClientData = Eagle._Components.Public.ClientData;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    [ObjectId("b2975958-ed3b-4d1d-8540-0ff4c297110d")]
    public sealed class Script :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IScript, /* THREAD-SAFE */
        ICloneable
    {
        #region Public Static Data
        public static readonly IScript Empty = new Script(
            ScriptSecurityFlags.AnyMask);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
#if XML
        //
        // HACK: These are purposely not read-only.
        //
        private static XmlAttributeListType xmlAttributeListType =
            XmlAttributeListType.All;

        private static bool overwriteExtraXmlAttributes = true;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Code Access Security Constants
#if CAS_POLICY
        private static readonly Evidence DefaultEvidence = null;
        private static readonly byte[] DefaultHashValue = null;
        private static readonly HashAlgorithm DefaultHashAlgorithm = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private ScriptSecurityFlags securityFlags;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Script(
            ScriptSecurityFlags securityFlags
            )
        {
            this.securityFlags = securityFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        private Script(
            Guid id,
            string name,
            string group,
            string description,
            string type,
            string text,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
#if XML
            XmlBlockType blockType,
            DateTime timeStamp,
            string publicKeyToken,
            byte[] signature,
#endif
#if CAS_POLICY
            Evidence evidence,
            byte[] hashValue,
            HashAlgorithm hashAlgorithm,
#endif
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData,
            ScriptSecurityFlags securityFlags
            )
            : this(securityFlags)
        {
            this.kind = IdentifierKind.Script;
            this.id = id;
            this.name = name;
            this.group = group;
            this.description = description;
            this.type = type;
            this.text = text;
            this.fileName = fileName;
            this.startLine = startLine;
            this.endLine = endLine;
            this.viaSource = viaSource;

#if XML
            this.blockType = blockType;
            this.timeStamp = timeStamp;
            this.publicKeyToken = publicKeyToken;
            this.signature = signature;
#endif

#if CAS_POLICY
            this.evidence = evidence;
            this.hashValue = hashValue;
            this.hashAlgorithm = hashAlgorithm;
#endif

            this.engineMode = engineMode;
            this.scriptFlags = scriptFlags;
            this.engineFlags = engineFlags;
            this.substitutionFlags = substitutionFlags;
            this.expressionFlags = expressionFlags;
            this.eventFlags = eventFlags;
            this.clientData = clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool IsImmutable()
        {
            return FlagOps.HasFlags(
                securityFlags, ScriptSecurityFlags.Immutable, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool HasAnyRestrictions()
        {
            return FlagOps.HasFlags(
                securityFlags, ScriptSecurityFlags.AnyMask, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private void CheckIsImmutable()
        {
            if (!IsImmutable())
                return;

            throw new ScriptException(
                "permission denied: script is immutable");
        }

        ///////////////////////////////////////////////////////////////////////

        private void CheckHasAnyRestrictions()
        {
            if (!HasAnyRestrictions())
                return;

            throw new ScriptException(
                "permission denied: script is read-only and/or immutable");
        }

        ///////////////////////////////////////////////////////////////////////

        private ObjectDictionary PrivateGetExtra()
        {
            object data = null;

            /* IGNORED */
            _ClientData.UnwrapOrReturn(clientData, ref data);

            return data as ObjectDictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        private ObjectDictionary PrivateGetOrCopyExtra()
        {
            //
            // NOTE: Does this instance have extra attributes?
            //
            ObjectDictionary extra = PrivateGetExtra();

            if (extra == null)
                return null;

            if (!HasAnyRestrictions())
                return extra;

            return new ObjectDictionary(
                (IDictionary<string, object>)extra);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        public static IScript Create(
            string text
            )
        {
            return Create(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string text,
            IClientData clientData
            )
        {
            return Create(
                ScriptTypes.Invalid, text, TimeOps.GetUtcNow(),
                clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string type,
            string text,
            DateTime timeStamp,
            IClientData clientData
            )
        {
            return Create(
                null, null, null, type, text, timeStamp,
                EngineMode.EvaluateScript, ScriptFlags.None,
                EngineFlags.None, SubstitutionFlags.Default,
                EventFlags.None, ExpressionFlags.Default,
                clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string name,
            string group,
            string description,
            string type,
            string text,
            DateTime timeStamp,
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData
            )
        {
            return Create(
                name, group, description, type, text, null,
                Parser.UnknownLine, Parser.UnknownLine, false,
                timeStamp, engineMode, scriptFlags, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string name,
            string group,
            string description,
            string type,
            string text,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
            DateTime timeStamp,
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData
            )
        {
            return PrivateCreate(
                Guid.Empty, name, group, description, type, text,
                fileName, startLine, endLine, viaSource,
#if XML
                XmlBlockType.None, timeStamp, null, null,
#endif
                engineMode, scriptFlags, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, clientData,
                ScriptSecurityFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            IScript script,
            ref Result error
            )
        {
            if (script == null)
            {
                error = "invalid script";
                return null;
            }

            ICloneable cloneable = script as ICloneable;

            if (cloneable == null)
            {
                error = "script is not cloneable";
                return null;
            }

            IScript localScript = cloneable.Clone() as IScript;

            if (localScript == null)
            {
                error = "script could not be cloned";
                return null;
            }

            return localScript;
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        internal static IScript CreateForAfter(
            string name,
            string group,
            string description,
            string type,
            string text,
            DateTime timeStamp,
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData
            )
        {
            return Create(
                name, group, description, type, text, null,
                Parser.UnknownLine, Parser.UnknownLine, false,
                timeStamp, engineMode, scriptFlags, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        internal static IScript CreateForPolicy(
            string name,
            string type,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result error /* NOT USED */
            )
        {
            return Create(
                name, null, null, type, text, TimeOps.GetUtcNow(),
                EngineMode.EvaluateScript, ScriptFlags.None,
                engineFlags, substitutionFlags, eventFlags,
                expressionFlags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static IScript CreateForPolicy(
            IScript script,  /* in */
            string text,     /* in: originalText */
            ref Result error /* out */
            )
        {
            if (script == null)
            {
                error = "invalid script";
                return null;
            }

            ICloneable cloneable = script as ICloneable;

            if (cloneable == null)
            {
                error = "cannot clone script";
                return null;
            }

            object clone = cloneable.Clone();

            if (clone == null)
            {
                error = "could not clone script";
                return null;
            }

            Script cloneScript = clone as Script;

            if (cloneScript == null)
            {
                error = "cloned script type mismatch";
                return null;
            }

            cloneScript.Text = text;
            return cloneScript;
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        /* INTERNAL STATIC OK */
        internal static IScript CreateFromXmlNode( /* NOTE: Engine use only. */
            string type,
            XmlNode node,
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData,
            ref Result error
            )
        {
            //
            // NOTE: Try to create the IScript using values extracted
            //       from the specified XML node.
            //
            Guid id;
            XmlBlockType blockType;
            string text;

            string name;
            string group;
            string description;

            DateTime timeStamp;
            string publicKeyToken;
            byte[] signature;

            ObjectDictionary extra;

            if (!ScriptXmlOps.TryGetAttributeValues(
                    node, xmlAttributeListType,
                    overwriteExtraXmlAttributes, out id,
                    out blockType, out text, out name,
                    out group, out description,
                    out timeStamp, out publicKeyToken,
                    out signature, out extra, ref error))
            {
                return null;
            }

            IClientData localClientData;

            if (extra != null)
            {
                localClientData = _ClientData.WrapOrReplace(
                    clientData, extra);
            }
            else
            {
                localClientData = clientData;
            }

            return PrivateCreate(
                id, name, group, description, type, text, null,
                Parser.UnknownLine, Parser.UnknownLine, false,
                blockType, timeStamp, publicKeyToken, signature,
                engineMode, scriptFlags, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                localClientData, ScriptSecurityFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SaveToXmlNode(
            XmlNode node,
            IScript script,
            ref Result error
            )
        {
            if (script == null)
            {
                error = "invalid script";
                return ReturnCode.Error;
            }

            if (!ScriptXmlOps.TrySetAttributeValues(
                    node, xmlAttributeListType,
                    overwriteExtraXmlAttributes, script.Id,
                    script.BlockType, script.Text, script.Name,
                    script.Group, script.Description,
                    script.TimeStamp, script.PublicKeyToken,
                    script.Signature, script.MaybeGetExtra(),
                    ref error))
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Private
        private static IScript PrivateCreate(
            Guid id,
            string name,
            string group,
            string description,
            string type,
            string text,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
#if XML
            XmlBlockType blockType,
            DateTime timeStamp,
            string publicKeyToken,
            byte[] signature,
#endif
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData,
            ScriptSecurityFlags securityFlags
            )
        {
            return new Script(
                id, name, group, description, type, text,
                fileName, startLine, endLine, viaSource,
#if XML
                blockType, timeStamp, publicKeyToken,
                signature,
#endif
#if CAS_POLICY
                DefaultEvidence,
                DefaultHashValue,
                DefaultHashAlgorithm,
#endif
                engineMode, scriptFlags, engineFlags,
                substitutionFlags, eventFlags,
                expressionFlags, clientData, securityFlags);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator Class
        [ObjectId("10883ee1-ca0c-44d0-89f9-2cdf26517ca1")]
        private sealed class ScriptEnumerator : IEnumerator
        {
            #region Private Data
            private IScript script;
            private int position;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ScriptEnumerator(
                IScript script
                )
            {
                if (script == null)
                    throw new ArgumentNullException("script");

                this.script = script;

                Reset();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IEnumerator Members
            public object Current
            {
                get
                {
                    if (script != null)
                    {
                        lock (script.SyncRoot)
                        {
                            //
                            // TODO: If we ever support scripts with
                            //       multiple parts, change this to
                            //       do proper indexing.
                            //
                            if (position < script.Count)
                            {
                                /* Immutable, Deep Copy */
                                return script.Text;
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            public bool MoveNext()
            {
                position++;

                if (script != null)
                {
                    lock (script.SyncRoot)
                    {
                        return position < script.Count;
                    }
                }
                else
                {
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public void Reset()
            {
                position = Index.Invalid;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerable Members
        public IEnumerator GetEnumerator()
        {
            return new ScriptEnumerator(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection Members
        public int Count
        {
            //
            // TODO: If we ever support scripts with multiple
            //       parts, change this to return the proper
            //       count.
            //
            get { return 1; } // A collection of one.
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSynchronized
        {
            get { return false; } // must lock manually.
        }

        ///////////////////////////////////////////////////////////////////////

        private readonly object syncRoot = new object();
        public object SyncRoot
        {
            get { return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void CopyTo(
            Array array,
            int index
            )
        {
            if (array == null)
                throw new ArgumentNullException();

            if (index < 0)
                throw new ArgumentOutOfRangeException();

            if (array.Rank != 1)
                throw new ArgumentException();

            int length = array.Length;

            if (index >= length)
                throw new ArgumentException();

            int count = this.Count;

            if ((index + count) > length)
                throw new ArgumentException();

            array.SetValue(text, index);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { return name; }
            set { CheckHasAnyRestrictions(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
            set { CheckHasAnyRestrictions(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { CheckHasAnyRestrictions(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { return group; }
            set { CheckHasAnyRestrictions(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { return description; }
            set { CheckHasAnyRestrictions(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckIsImmutable(); return clientData; }
            set { CheckHasAnyRestrictions(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptLocation Members
        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set { CheckHasAnyRestrictions(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int startLine;
        public int StartLine
        {
            get { return startLine; }
            set { CheckHasAnyRestrictions(); startLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int endLine;
        public int EndLine
        {
            get { return endLine; }
            set { CheckHasAnyRestrictions(); endLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool viaSource;
        public bool ViaSource
        {
            get { return viaSource; }
            set { CheckHasAnyRestrictions(); viaSource = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList ToList()
        {
            return ToList(false);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList ToList(
            bool scrub
            )
        {
            StringPairList list = new StringPairList();

            list.Add("type", type);
            list.Add("text", text);

            list.Add("fileName", scrub ? PathOps.ScrubPath(
                GlobalState.GetBasePath(), fileName) : fileName);

            list.Add("startLine", startLine.ToString());
            list.Add("endLine", endLine.ToString());
            list.Add("viaSource", viaSource.ToString());

            list.Add("engineMode", engineMode.ToString());
            list.Add("scriptFlags", scriptFlags.ToString());
            list.Add("engineFlags", engineFlags.ToString());
            list.Add("substitutionFlags", substitutionFlags.ToString());
            list.Add("eventFlags", eventFlags.ToString());
            list.Add("expressionFlags", expressionFlags.ToString());

#if XML
            list.Add("blockType", blockType.ToString());
            list.Add("timeStamp", timeStamp.ToString());
            list.Add("publicKeyToken", publicKeyToken);
            list.Add("signature", ArrayOps.ToHexadecimalString(signature));
#endif

#if CAS_POLICY
            if (!scrub)
            {
                list.Add("evidence", (evidence != null) ?
                    evidence.ToString() : null);

                list.Add("hashValue",
                    ArrayOps.ToHexadecimalString(hashValue));

                list.Add("hashAlgorithm", (hashAlgorithm != null) ?
                    hashAlgorithm.ToString() : null);
            }
#endif

            if (!scrub)
            {
                ObjectDictionary extra = PrivateGetExtra();

                if (extra != null)
                    list.Add("extra", extra.ToString());
            }

            list.Add("securityFlags", securityFlags.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptData Members
        private string type;
        public string Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public IList Parts
        {
            get
            {
                //
                // TODO: If this property is modified in the future
                //       to return an actual list of script parts,
                //       make sure it returns a deep copy if this
                //       IScript instance is read-only or immutable.
                //
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            //
            // TODO: If we ever support scripts with multiple parts,
            //       change this to combine all the parts into one
            //       piece of text?
            //
            get { return text; }
            private set { text = value; } // WARNING: CreateForPolicy ONLY.
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        private XmlBlockType blockType;
        public XmlBlockType BlockType
        {
            get { return blockType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime timeStamp;
        public DateTime TimeStamp
        {
            get { return timeStamp; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string publicKeyToken;
        public string PublicKeyToken
        {
            get { return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] signature;
        public byte[] Signature
        {
            get
            {
                if (HasAnyRestrictions())
                    return ArrayOps.Copy(signature);

                return signature;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        private Evidence evidence;
        public Evidence Evidence
        {
            get { return evidence; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] hashValue;
        public byte[] HashValue
        {
            get
            {
                if (HasAnyRestrictions())
                    return ArrayOps.Copy(hashValue);

                return hashValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private HashAlgorithm hashAlgorithm;
        public HashAlgorithm HashAlgorithm
        {
            get { return hashAlgorithm; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptFlags Members
        private EngineMode engineMode;
        public EngineMode EngineMode
        {
            get { return engineMode; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptFlags scriptFlags;
        public ScriptFlags ScriptFlags
        {
            get { return scriptFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EngineFlags engineFlags;
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SubstitutionFlags substitutionFlags;
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventFlags eventFlags;
        public EventFlags EventFlags
        {
            get { return eventFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ExpressionFlags expressionFlags;
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScript Members
        public ObjectDictionary MaybeGetExtra()
        {
            if (IsImmutable())
                return null;

            return PrivateGetOrCopyExtra();
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary GetExtra()
        {
            //
            // HACK: Since we do not know what type(s) of objects
            //       are contained in the "extra" attributes that
            //       may be present, do not allow them to be used
            //       as part of the dictionary return value if an
            //       instance is immutable, i.e. avoid returning
            //       a dictionary at all by throwing an exception
            //       here.
            //
            CheckIsImmutable();

            return PrivateGetOrCopyExtra();
        }

        ///////////////////////////////////////////////////////////////////////

        public void MakeImmutable()
        {
            //
            // WARNING: Once this method is called, it cannot be undone from
            //          external to this class.  This is by design, for the
            //          sake of security (e.g. for IScript objects passed to
            //          the policy engine).  Further, there is no way for an
            //          external caller to determine if an IScript instance
            //          is read-only or immutable (i.e. via an introspection
            //          property) without causing an exception to be thrown.
            //          This restriction may be relaxed in the future.
            //
            securityFlags |= ScriptSecurityFlags.Immutable;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return PrivateCreate(
                id, name, group, description, type, text, fileName,
                startLine, endLine, viaSource,
#if XML
                blockType, timeStamp, publicKeyToken, signature,
#endif
                engineMode, scriptFlags, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, clientData, securityFlags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList(text, type);
        }
        #endregion
    }
}
