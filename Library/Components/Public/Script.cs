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
    /// <summary>
    /// This class represents a unit of script text together with the metadata
    /// needed to evaluate it, such as its name, type, source location, engine
    /// and substitution flags, and (optionally) cryptographic and bundle
    /// information.  It implements <see cref="IScript" /> and supports both
    /// enumeration of its parts and cloning.  Instances may be made read-only
    /// or immutable, in which case attempts to modify them are rejected.
    /// </summary>
    [ObjectId("b2975958-ed3b-4d1d-8540-0ff4c297110d")]
    public sealed class Script :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IScript, /* THREAD-SAFE */
        ICloneable
    {
        #region Public Static Data
        /// <summary>
        /// A shared, pre-built empty script instance carrying no text and no
        /// security restrictions.
        /// </summary>
        public static readonly IScript Empty = new Script(new BundleData(
            null, 0, null, null, null, null, null, IsolationLevel.None,
            SecurityLevel.None, ScriptSecurityFlags.AnyMask, null));
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// A shared, pre-built bundle data instance carrying no security
        /// restrictions, used as the template when creating new scripts.
        /// </summary>
        private static readonly IBundleData EmptyBundleData = new BundleData(
            null, 0, null, null, null, null, null, IsolationLevel.None,
            SecurityLevel.None, ScriptSecurityFlags.None, null);

        ///////////////////////////////////////////////////////////////////////

#if XML
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The set of XML attributes considered when reading script metadata
        /// from, or writing it to, an XML node.
        /// </summary>
        private static XmlAttributeListType xmlAttributeListType =
            XmlAttributeListType.All;

        /// <summary>
        /// When non-zero, extra (unrecognized) XML attributes are overwritten
        /// when transferring script metadata to or from an XML node.
        /// </summary>
        private static bool overwriteExtraXmlAttributes = true;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Code Access Security Constants
#if CAS_POLICY
        /// <summary>
        /// The default Code Access Security evidence associated with a newly
        /// created script (a null reference).
        /// </summary>
        private static readonly Evidence DefaultEvidence = null;

        /// <summary>
        /// The default hash value associated with a newly created script (a
        /// null reference).
        /// </summary>
        private static readonly byte[] DefaultHashValue = null;

        /// <summary>
        /// The default hash algorithm associated with a newly created script (a
        /// null reference).
        /// </summary>
        private static readonly HashAlgorithm DefaultHashAlgorithm = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a script associated only with the specified bundle data,
        /// leaving all other metadata unset.
        /// </summary>
        /// <param name="bundleData">
        /// The bundle data, including any security restrictions, to associate
        /// with this script.  This parameter may be null.
        /// </param>
        private Script(
            IBundleData bundleData
            )
        {
            this.id = Guid.Empty;
            this.bundleData = bundleData;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a fully populated script from the specified metadata,
        /// text, source location, and flags.
        /// </summary>
        /// <param name="id">
        /// The globally unique identifier for this script.
        /// </param>
        /// <param name="name">
        /// The name of this script.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this script.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this script.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type of this script.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text itself.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file this script originated from, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The line number where this script begins, or an invalid line number
        /// if it is unknown.
        /// </param>
        /// <param name="endLine">
        /// The line number where this script ends, or an invalid line number
        /// if it is unknown.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if this script was obtained via the [source] command.
        /// </param>
        /// <param name="blockType">
        /// The type of XML block this script was extracted from.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp associated with this script.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token associated with this script.  This parameter
        /// may be null.
        /// </param>
        /// <param name="signature">
        /// The cryptographic signature associated with this script.  This
        /// parameter may be null.
        /// </param>
        /// <param name="evidence">
        /// The Code Access Security evidence associated with this script.  This
        /// parameter may be null.
        /// </param>
        /// <param name="hashValue">
        /// The hash value associated with this script.  This parameter may be
        /// null.
        /// </param>
        /// <param name="hashAlgorithm">
        /// The hash algorithm associated with this script.  This parameter may
        /// be null.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that should be used when evaluating this script.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags that should be used when evaluating this script.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that should be used when evaluating this script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags that should be used when evaluating this
        /// script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that should be used when evaluating this script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags that should be used when evaluating this
        /// script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this script.  This parameter may
        /// be null.
        /// </param>
        /// <param name="bundleData">
        /// The bundle data, including any security restrictions, to associate
        /// with this script.  This parameter may be null.
        /// </param>
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
            IBundleData bundleData
            )
            : this(bundleData)
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
        /// <summary>
        /// This method determines whether this script is immutable, based on
        /// its associated bundle data security flags.
        /// </summary>
        /// <returns>
        /// True if this script is immutable; otherwise, false.
        /// </returns>
        private bool IsImmutable()
        {
            if (bundleData == null)
                return false;

            return FlagOps.HasFlags(
                bundleData.SecurityFlags, ScriptSecurityFlags.Immutable, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this script has any security
        /// restrictions, based on its associated bundle data security flags.
        /// </summary>
        /// <returns>
        /// True if this script has any security restrictions; otherwise, false.
        /// </returns>
        private bool HasAnyRestrictions()
        {
            if (bundleData == null)
                return false;

            return FlagOps.HasFlags(
                bundleData.SecurityFlags, ScriptSecurityFlags.AnyMask, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method throws an exception if this script is immutable;
        /// otherwise, it does nothing.
        /// </summary>
        private void CheckIsImmutable()
        {
            if (!IsImmutable())
                return;

            throw new ScriptException(
                "permission denied: script is immutable");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method throws an exception if this script has any security
        /// restrictions; otherwise, it does nothing.
        /// </summary>
        private void CheckHasAnyRestrictions()
        {
            if (!HasAnyRestrictions())
                return;

            throw new ScriptException(
                "permission denied: script is read-only and/or immutable");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the dictionary of extra attributes associated
        /// with this script, by unwrapping it from the client data.
        /// </summary>
        /// <returns>
        /// The dictionary of extra attributes, or null if there are none.
        /// </returns>
        private ObjectDictionary PrivateGetExtra()
        {
            object data = null;

            /* IGNORED */
            _ClientData.UnwrapOrReturn(clientData, ref data);

            return data as ObjectDictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the dictionary of extra attributes associated
        /// with this script, returning a defensive copy when this script has
        /// any security restrictions.
        /// </summary>
        /// <returns>
        /// The dictionary of extra attributes (possibly a copy), or null if
        /// there are none.
        /// </returns>
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
        /// <summary>
        /// This method creates a script from the text and metadata of the
        /// specified snippet.
        /// </summary>
        /// <param name="snippet">
        /// The snippet whose text and metadata are used to create the script.
        /// This parameter may not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The newly created script, or null if it could not be created.
        /// </returns>
        public static IScript Create(
            ISnippet snippet,
            ref Result error
            )
        {
            if (snippet == null)
            {
                error = "invalid script";
                return null;
            }

            string text = null;

            if (SnippetOps.GetText(
                    snippet, ref text, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            Guid id = snippet.Id;

            /* IGNORED */
            ScriptOps.ExtractId(text, ref id);

            IScript script = InternalCreate(id,
                snippet.Name, snippet.Group, snippet.Description,
                ScriptTypes.Snippet, text, snippet.Path,
                Parser.UnknownLine, Parser.UnknownLine, false,
#if XML
                XmlBlockType.None, TimeOps.GetUtcNow(), null,
                null,
#endif
                EngineMode.EvaluateScript, ScriptFlags.None,
                EngineFlags.None, SubstitutionFlags.Default,
                EventFlags.None, ExpressionFlags.Default,
                snippet.ClientData, new BundleData(EmptyBundleData));

            if (script == null)
            {
                error = "failed to create script";
                return null;
            }

            return script;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script from the specified text.
        /// </summary>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
        public static IScript Create(
            string text
            )
        {
            return Create(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script from the specified text and client
        /// data.
        /// </summary>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the script.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
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

        /// <summary>
        /// This method creates a script of the specified type from the
        /// specified text, time stamp, and client data.
        /// </summary>
        /// <param name="type">
        /// The type of the script.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp to associate with the script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the script.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
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

        /// <summary>
        /// This method creates a script from the specified metadata, text,
        /// time stamp, flags, and client data.
        /// </summary>
        /// <param name="name">
        /// The name of the script.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the script.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the script.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type of the script.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp to associate with the script.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that should be used when evaluating the script.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags that should be used when evaluating the script.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that should be used when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags that should be used when evaluating the
        /// script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that should be used when evaluating the script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags that should be used when evaluating the script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the script.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
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

        /// <summary>
        /// This method creates a script from the specified metadata, text,
        /// source location, time stamp, flags, and client data.
        /// </summary>
        /// <param name="name">
        /// The name of the script.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the script.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the script.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type of the script.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file the script originated from, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The line number where the script begins, or an invalid line number
        /// if it is unknown.
        /// </param>
        /// <param name="endLine">
        /// The line number where the script ends, or an invalid line number if
        /// it is unknown.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the script was obtained via the [source] command.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp to associate with the script.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that should be used when evaluating the script.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags that should be used when evaluating the script.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that should be used when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags that should be used when evaluating the
        /// script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that should be used when evaluating the script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags that should be used when evaluating the script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the script.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
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
            Guid id = Guid.Empty;

            /* IGNORED */
            ScriptOps.ExtractId(text, ref id);

            return InternalCreate(
                id, name, group, description, type, text,
                fileName, startLine, endLine, viaSource,
#if XML
                XmlBlockType.None, timeStamp, null, null,
#endif
                engineMode, scriptFlags, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                clientData, new BundleData(EmptyBundleData));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script by cloning the specified existing
        /// script.
        /// </summary>
        /// <param name="script">
        /// The script to clone.  This parameter may not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The newly cloned script, or null if it could not be cloned.
        /// </returns>
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

        /// <summary>
        /// This method creates a script intended for use with the [after]
        /// command, from the specified metadata, text, time stamp, flags, and
        /// client data.
        /// </summary>
        /// <param name="name">
        /// The name of the script.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the script.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the script.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type of the script.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp to associate with the script.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that should be used when evaluating the script.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags that should be used when evaluating the script.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that should be used when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags that should be used when evaluating the
        /// script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that should be used when evaluating the script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags that should be used when evaluating the script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the script.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
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

        /// <summary>
        /// This method creates a script intended for use during policy
        /// evaluation, from the specified metadata, text, and flags.
        /// </summary>
        /// <param name="name">
        /// The name of the script.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type of the script.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that should be used when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags that should be used when evaluating the
        /// script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that should be used when evaluating the script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags that should be used when evaluating the script.
        /// </param>
        /// <param name="error">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
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

        /// <summary>
        /// This method creates a script intended for use during policy
        /// evaluation by cloning the specified existing script and replacing
        /// its text with the specified original text.
        /// </summary>
        /// <param name="script">
        /// The script to clone.  This parameter may not be null.
        /// </param>
        /// <param name="text">
        /// The original text to use for the cloned script.  This parameter may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The newly cloned script, or null if it could not be cloned.
        /// </returns>
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
        /// <summary>
        /// This method creates a script of the specified type by extracting its
        /// metadata and text from the specified XML node.
        /// </summary>
        /// <param name="type">
        /// The type of the script.  This parameter may be null.
        /// </param>
        /// <param name="node">
        /// The XML node containing the script metadata and text.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that should be used when evaluating the script.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags that should be used when evaluating the script.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that should be used when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags that should be used when evaluating the
        /// script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that should be used when evaluating the script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags that should be used when evaluating the script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the script.  This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The newly created script, or null if it could not be created.
        /// </returns>
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

            return InternalCreate(
                id, name, group, description, type, text, null,
                Parser.UnknownLine, Parser.UnknownLine, false,
                blockType, timeStamp, publicKeyToken, signature,
                engineMode, scriptFlags, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                localClientData, new BundleData(EmptyBundleData));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the metadata and text of the specified script to
        /// the specified XML node.
        /// </summary>
        /// <param name="node">
        /// The XML node to populate with the script metadata and text.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="script">
        /// The script whose metadata and text are saved.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, ReturnCode.Error.
        /// </returns>
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
        /// <summary>
        /// This method creates a script from the fully specified metadata,
        /// text, source location, flags, client data, and bundle data.  It is
        /// the common implementation used by the public and internal factory
        /// methods.
        /// </summary>
        /// <param name="id">
        /// The globally unique identifier for the script.
        /// </param>
        /// <param name="name">
        /// The name of the script.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the script.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the script.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type of the script.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file the script originated from, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The line number where the script begins, or an invalid line number
        /// if it is unknown.
        /// </param>
        /// <param name="endLine">
        /// The line number where the script ends, or an invalid line number if
        /// it is unknown.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the script was obtained via the [source] command.
        /// </param>
        /// <param name="blockType">
        /// The type of XML block the script was extracted from.
        /// </param>
        /// <param name="timeStamp">
        /// The time stamp to associate with the script.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token to associate with the script.  This parameter
        /// may be null.
        /// </param>
        /// <param name="signature">
        /// The cryptographic signature to associate with the script.  This
        /// parameter may be null.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that should be used when evaluating the script.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags that should be used when evaluating the script.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that should be used when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags that should be used when evaluating the
        /// script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that should be used when evaluating the script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags that should be used when evaluating the script.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the script.  This parameter may be
        /// null.
        /// </param>
        /// <param name="bundleData">
        /// The bundle data, including any security restrictions, to associate
        /// with the script.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created script.
        /// </returns>
        internal static IScript InternalCreate(
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
            IBundleData bundleData
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
                expressionFlags, clientData, bundleData);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator Class
        /// <summary>
        /// This class provides an enumerator over the parts of a script.
        /// Currently, a script always consists of a single part (its text).
        /// </summary>
        [ObjectId("10883ee1-ca0c-44d0-89f9-2cdf26517ca1")]
        private sealed class ScriptEnumerator : IEnumerator
        {
            #region Private Data
            /// <summary>
            /// The script being enumerated.
            /// </summary>
            private IScript script;

            /// <summary>
            /// The current zero-based position within the script being
            /// enumerated.
            /// </summary>
            private int position;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            /// <summary>
            /// Constructs an enumerator over the parts of the specified script.
            /// </summary>
            /// <param name="script">
            /// The script to enumerate.  This parameter may not be null.
            /// </param>
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
            /// <summary>
            /// Gets the current script part at the enumerator's position.
            /// </summary>
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

            /// <summary>
            /// This method advances the enumerator to the next part of the
            /// script.
            /// </summary>
            /// <returns>
            /// True if the enumerator was successfully advanced to the next
            /// part; otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method resets the enumerator to its initial position,
            /// before the first part of the script.
            /// </summary>
            public void Reset()
            {
                position = Index.Invalid;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerable Members
        /// <summary>
        /// This method returns an enumerator that iterates over the parts of
        /// this script.
        /// </summary>
        /// <returns>
        /// An enumerator over the parts of this script.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return new ScriptEnumerator(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection Members
        /// <summary>
        /// Gets the number of parts in this script.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether access to this script is
        /// synchronized (thread-safe).
        /// </summary>
        public bool IsSynchronized
        {
            get { return false; } // must lock manually.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The object used to synchronize access to this script.
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// Gets an object that can be used to synchronize access to this
        /// script.
        /// </summary>
        public object SyncRoot
        {
            get { return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the parts of this script to the specified array,
        /// starting at the specified index.
        /// </summary>
        /// <param name="array">
        /// The destination array.  This parameter may not be null and must be
        /// one-dimensional.
        /// </param>
        /// <param name="index">
        /// The zero-based index in the destination array at which copying
        /// begins.
        /// </param>
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
        /// <summary>
        /// The name of this script.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name of this script.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { CheckHasAnyRestrictions(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The identifier kind of this script.
        /// </summary>
        private IdentifierKind kind;

        /// <summary>
        /// Gets or sets the identifier kind of this script.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { CheckHasAnyRestrictions(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The globally unique identifier of this script.
        /// </summary>
        private Guid id;

        /// <summary>
        /// Gets or sets the globally unique identifier of this script.
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { CheckHasAnyRestrictions(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this script.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the client data associated with this script.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckIsImmutable(); return clientData; }
            set { CheckHasAnyRestrictions(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The group of this script.
        /// </summary>
        private string group;

        /// <summary>
        /// Gets or sets the group of this script.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { CheckHasAnyRestrictions(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of this script.
        /// </summary>
        private string description;

        /// <summary>
        /// Gets or sets the description of this script.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { CheckHasAnyRestrictions(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptLocation Members
        /// <summary>
        /// The name of the file this script originated from, if any.
        /// </summary>
        private string fileName;

        /// <summary>
        /// Gets or sets the name of the file this script originated from, if
        /// any.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
            set { CheckHasAnyRestrictions(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The line number where this script begins.
        /// </summary>
        private int startLine;

        /// <summary>
        /// Gets or sets the line number where this script begins.
        /// </summary>
        public int StartLine
        {
            get { return startLine; }
            set { CheckHasAnyRestrictions(); startLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The line number where this script ends.
        /// </summary>
        private int endLine;

        /// <summary>
        /// Gets or sets the line number where this script ends.
        /// </summary>
        public int EndLine
        {
            get { return endLine; }
            set { CheckHasAnyRestrictions(); endLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this script was obtained via the [source] command.
        /// </summary>
        private bool viaSource;

        /// <summary>
        /// Gets or sets a value indicating whether this script was obtained via
        /// the [source] command.
        /// </summary>
        public bool ViaSource
        {
            get { return viaSource; }
            set { CheckHasAnyRestrictions(); viaSource = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a list of name/value pairs representing the
        /// metadata and content of this script.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs representing this script.
        /// </returns>
        public StringPairList ToList()
        {
            return ToList(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a list of name/value pairs representing the
        /// metadata and content of this script, optionally scrubbing
        /// potentially sensitive information.
        /// </summary>
        /// <param name="scrub">
        /// Non-zero to omit or sanitize potentially sensitive information,
        /// such as file paths and security details.
        /// </param>
        /// <returns>
        /// A list of name/value pairs representing this script.
        /// </returns>
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

            list.Add("timeStamp",
                FormatOps.Iso8601FullDateTime(timeStamp));

            list.Add("publicKeyToken", publicKeyToken);

            if (signature != null)
            {
                list.Add("signature", Convert.ToBase64String(
                    signature, Base64FormattingOptions.InsertLineBreaks));
            }
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

            if (!scrub && (bundleData != null))
            {
                string language = bundleData.Language;

                if (language != null)
                    list.Add("language", language);

                list.Add("sequence",
                    bundleData.Sequence.ToString());

                string vendor = bundleData.Vendor;

                if (vendor != null)
                    list.Add("vendor", vendor);

                string path = bundleData.Path;

                if (path != null)
                    list.Add("path", path);

                string fullName = bundleData.FullName;

                if (fullName != null)
                    list.Add("fullName", fullName);

                string hashAlgorithmName = bundleData.HashAlgorithmName;

                if (hashAlgorithmName != null)
                    list.Add("hashAlgorithmName", hashAlgorithmName);

                byte[] fileBytes = bundleData.FileBytes;

                if (fileBytes != null)
                {
                    list.Add("fileBytes",
                        Convert.ToBase64String(fileBytes,
                        Base64FormattingOptions.InsertLineBreaks));
                }

                list.Add("isolationLevel",
                    bundleData.IsolationLevel.ToString());

                list.Add("securityLevel",
                    bundleData.SecurityLevel.ToString());

                IRuleSet ruleSet = bundleData.RuleSet;

                if (ruleSet != null)
                    list.Add("ruleSet", ruleSet.ToString());

                list.Add("securityFlags",
                    bundleData.SecurityFlags.ToString());
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptData Members
        /// <summary>
        /// The type of this script.
        /// </summary>
        private string type;

        /// <summary>
        /// Gets the type of this script.
        /// </summary>
        public string Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the list of parts that make up this script.  This property is
        /// obsolete and currently always returns null.
        /// </summary>
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

        /// <summary>
        /// The text of this script.
        /// </summary>
        private string text;

        /// <summary>
        /// Gets or sets the text of this script.  The set accessor is private
        /// and intended for use by CreateForPolicy only.
        /// </summary>
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
        /// <summary>
        /// The type of XML block this script was extracted from.
        /// </summary>
        private XmlBlockType blockType;

        /// <summary>
        /// Gets the type of XML block this script was extracted from.
        /// </summary>
        public XmlBlockType BlockType
        {
            get { return blockType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The time stamp associated with this script.
        /// </summary>
        private DateTime timeStamp;

        /// <summary>
        /// Gets the time stamp associated with this script.
        /// </summary>
        public DateTime TimeStamp
        {
            get { return timeStamp; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The public key token associated with this script.
        /// </summary>
        private string publicKeyToken;

        /// <summary>
        /// Gets the public key token associated with this script.
        /// </summary>
        public string PublicKeyToken
        {
            get { return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cryptographic signature associated with this script.
        /// </summary>
        private byte[] signature;

        /// <summary>
        /// Gets the cryptographic signature associated with this script.  When
        /// this script has any security restrictions, a defensive copy is
        /// returned.
        /// </summary>
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
        /// <summary>
        /// The Code Access Security evidence associated with this script.
        /// </summary>
        private Evidence evidence;

        /// <summary>
        /// Gets the Code Access Security evidence associated with this script.
        /// </summary>
        public Evidence Evidence
        {
            get { return evidence; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The hash value associated with this script.
        /// </summary>
        private byte[] hashValue;

        /// <summary>
        /// Gets the hash value associated with this script.  When this script
        /// has any security restrictions, a defensive copy is returned.
        /// </summary>
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

        /// <summary>
        /// The hash algorithm associated with this script.
        /// </summary>
        private HashAlgorithm hashAlgorithm;

        /// <summary>
        /// Gets the hash algorithm associated with this script.
        /// </summary>
        public HashAlgorithm HashAlgorithm
        {
            get { return hashAlgorithm; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The bundle data, including any security restrictions, associated
        /// with this script.
        /// </summary>
        private IBundleData bundleData;

        /// <summary>
        /// Gets the bundle data, including any security restrictions,
        /// associated with this script.
        /// </summary>
        public IBundleData BundleData
        {
            get { return bundleData; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveScriptFlags Members
        /// <summary>
        /// The engine mode that should be used when evaluating this script.
        /// </summary>
        private EngineMode engineMode;

        /// <summary>
        /// Gets or sets the engine mode that should be used when evaluating
        /// this script.  The set accessor is not implemented.
        /// </summary>
        public EngineMode EngineMode
        {
            get { return engineMode; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script flags that should be used when evaluating this script.
        /// </summary>
        private ScriptFlags scriptFlags;

        /// <summary>
        /// Gets or sets the script flags that should be used when evaluating
        /// this script.  The set accessor is not implemented.
        /// </summary>
        public ScriptFlags ScriptFlags
        {
            get { return scriptFlags; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The engine flags that should be used when evaluating this script.
        /// </summary>
        private EngineFlags engineFlags;

        /// <summary>
        /// Gets or sets the engine flags that should be used when evaluating
        /// this script.  The set accessor is not implemented.
        /// </summary>
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The substitution flags that should be used when evaluating this
        /// script.
        /// </summary>
        private SubstitutionFlags substitutionFlags;

        /// <summary>
        /// Gets or sets the substitution flags that should be used when
        /// evaluating this script.  The set accessor is not implemented.
        /// </summary>
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The event flags that should be used when evaluating this script.
        /// </summary>
        private EventFlags eventFlags;

        /// <summary>
        /// Gets or sets the event flags that should be used when evaluating
        /// this script.  The set accessor is not implemented.
        /// </summary>
        public EventFlags EventFlags
        {
            get { return eventFlags; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The expression flags that should be used when evaluating this
        /// script.
        /// </summary>
        private ExpressionFlags expressionFlags;

        /// <summary>
        /// Gets or sets the expression flags that should be used when
        /// evaluating this script.  The set accessor is not implemented.
        /// </summary>
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// Gets or sets the bundle flags associated with this script.  Neither
        /// accessor is implemented.
        /// </summary>
        public BundleFlags BundleFlags
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScript Members
        /// <summary>
        /// This method determines whether this script should be treated as a
        /// file rather than as inline text, based on its security flags.
        /// </summary>
        /// <param name="fileName">
        /// Upon success, this parameter will be set to the name of the file to
        /// treat this script as; otherwise, it will be set to null.
        /// </param>
        /// <param name="fileBytes">
        /// Upon success, this parameter will be set to the raw bytes of the
        /// file to treat this script as; otherwise, it will be set to null.
        /// </param>
        /// <returns>
        /// True if this script should be treated as a file; otherwise, false.
        /// </returns>
        public bool ShouldTreatAsFile(
            out string fileName,
            out byte[] fileBytes
            )
        {
            if ((bundleData != null) && FlagOps.HasFlags(
                    bundleData.SecurityFlags,
                    ScriptSecurityFlags.TreatAsFile, true))
            {
                fileName = bundleData.FullName;
                fileBytes = bundleData.FileBytes;

                return true;
            }
            else
            {
                fileName = null;
                fileBytes = null;

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// This method returns the type of XML block this script was extracted
        /// from, as a lower-case string.
        /// </summary>
        /// <returns>
        /// The lower-case string representation of the XML block type.
        /// </returns>
        public string GetBlockTypeString()
        {
            return blockType.ToString().ToLowerInvariant();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the dictionary of extra attributes associated
        /// with this script, returning null if this script is immutable.
        /// </summary>
        /// <returns>
        /// The dictionary of extra attributes, or null if this script is
        /// immutable or there are none.
        /// </returns>
        public ObjectDictionary MaybeGetExtra()
        {
            if (IsImmutable())
                return null;

            return PrivateGetOrCopyExtra();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the dictionary of extra attributes associated
        /// with this script, throwing an exception if this script is immutable.
        /// </summary>
        /// <returns>
        /// The dictionary of extra attributes, or null if there are none.
        /// </returns>
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

        /// <summary>
        /// This method makes this script immutable so that subsequent attempts
        /// to modify it are rejected.
        /// </summary>
        public void MakeImmutable()
        {
            if (bundleData == null)
            {
                throw new ScriptException(String.Format(
                    "cannot make immutable, missing {0}",
                    typeof(IBundleData)));
            }

            bundleData.MakeImmutable();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new script that is a copy of this script.
        /// </summary>
        /// <returns>
        /// A new script that is a copy of this script.
        /// </returns>
        public object Clone()
        {
            return InternalCreate(
                id, name, group, description, type, text, fileName,
                startLine, endLine, viaSource,
#if XML
                blockType, timeStamp, publicKeyToken, signature,
#endif
                engineMode, scriptFlags, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, clientData, bundleData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this script.
        /// </summary>
        /// <returns>
        /// A string representation of this script, consisting of its text and
        /// type formatted as a list.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(text, type);
        }
        #endregion
    }
}
