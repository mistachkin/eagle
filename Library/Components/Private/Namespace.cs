/*
 * Namespace.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

using ObjectPair = System.Collections.Generic.KeyValuePair<
    string, object>;

using NamespacePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.INamespace>;

using ObjectIDictionary = System.Collections.Generic.IDictionary<
    string, object>;

using NamespaceDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.INamespace>;

using DescendantTriplet = Eagle._Components.Public.MutableAnyTriplet<
    System.Collections.Generic.Dictionary<string,
        Eagle._Interfaces.Public.INamespace>, string, bool>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class is the default implementation of the
    /// <see cref="INamespace" /> interface.  It represents a single Tcl-style
    /// namespace, tracking its name, parent, child namespaces, imported
    /// commands (as aliases), and exported command name patterns, as well as
    /// the reference count used by the namespace enter/exit mechanism.
    /// </summary>
    [ObjectId("5f2b9883-f5da-4d3c-85b8-cddb6b0de9f8")]
    internal sealed class Namespace :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IIdentifier, IMaybeDisposed, INamespace, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The child namespaces of this namespace, keyed by their simple
        /// (unqualified) name.
        /// </summary>
        private Dictionary<string, INamespace> children;

        /// <summary>
        /// The commands imported into this namespace, keyed by their qualified
        /// import name; each value is the alias that implements the import.
        /// </summary>
        private ObjectDictionary imports;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class, initializing its identifier
        /// kind and identifier and creating its empty child, import, and export
        /// collections.
        /// </summary>
        private Namespace()
        {
            kind = IdentifierKind.Namespace;
            id = AttributeOps.GetObjectId(this);
            children = new Dictionary<string, INamespace>();
            imports = new ObjectDictionary();
            exportNames = new StringDictionary();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class from the specified namespace
        /// data, copying its name, client data, interpreter, parent, resolver,
        /// variable frame, and unknown handler.
        /// </summary>
        /// <param name="namespaceData">
        /// The namespace data used to initialize this namespace, or null to
        /// leave the corresponding fields at their default values.
        /// </param>
        public Namespace(
            INamespaceData namespaceData
            )
            : this()
        {
            if (namespaceData != null)
            {
                name = namespaceData.Name;
                clientData = namespaceData.ClientData;
                interpreter = namespaceData.Interpreter;
                parent = namespaceData.Parent;
                resolve = namespaceData.Resolve;
                variableFrame = namespaceData.VariableFrame;
                unknown = namespaceData.Unknown;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The simple (unqualified) name of this namespace.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the simple (unqualified) name of this namespace.
        /// Changing the name recomputes the qualified names of this namespace
        /// and its children and notifies the parent namespace.
        /// </summary>
        public string Name
        {
            get { CheckDisposed(); return name; }
            set
            {
                CheckDisposed();

                string oldName;
                bool global;

                BeforeNameChange(out oldName, out global);

                name = value;

                AfterNameChange(oldName, global);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this object.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this namespace.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier associated with this namespace.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier associated with this namespace.
        /// </summary>
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this namespace.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this namespace.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The group associated with this namespace.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group associated with this namespace.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this namespace.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this namespace.
        /// </summary>
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this namespace has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this namespace is in the process of
        /// being disposed.  This implementation always returns false.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// The interpreter that this namespace belongs to.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that this namespace belongs to; Setting this
        /// property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public Interpreter Interpreter /* READ-ONLY */
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INamespaceData Members
        /// <summary>
        /// The parent namespace of this namespace, or null if this is the
        /// global namespace.
        /// </summary>
        private INamespace parent;
        /// <summary>
        /// Gets or sets the parent namespace of this namespace.  Changing the
        /// parent recomputes the qualified names of this namespace and its
        /// children and notifies the (former and new) parent namespaces.
        /// </summary>
        public INamespace Parent
        {
            get { CheckDisposed(); return parent; }
            set
            {
                CheckDisposed();

                string oldName;
                bool global;

                BeforeNameChange(out oldName, out global);

                parent = value;

                AfterNameChange(oldName, global);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The resolver associated with this namespace, if any.
        /// </summary>
        private IResolve resolve;
        /// <summary>
        /// Gets the resolver associated with this namespace; Setting this
        /// property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public IResolve Resolve /* READ-ONLY */
        {
            get { CheckDisposed(); return resolve; }
            set { CheckDisposed(); throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The call frame that holds the variables belonging to this namespace.
        /// </summary>
        private ICallFrame variableFrame;
        /// <summary>
        /// Gets the call frame that holds the variables belonging to this
        /// namespace; Setting this property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public ICallFrame VariableFrame /* READ-ONLY */
        {
            get { CheckDisposed(); return variableFrame; }
            set { CheckDisposed(); throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the command used to handle unknown commands within this
        /// namespace.
        /// </summary>
        private string unknown;
        /// <summary>
        /// Gets or sets the name of the command used to handle unknown commands
        /// within this namespace.
        /// </summary>
        public string Unknown
        {
            get { CheckDisposed(); return unknown; }
            set { CheckDisposed(); unknown = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INamespace Members
        /// <summary>
        /// The cached fully-qualified name of this namespace, or null if it has
        /// not yet been computed.
        /// </summary>
        private string qualifiedName;
        /// <summary>
        /// Gets the fully-qualified name of this namespace, computing and
        /// caching it on first access.
        /// </summary>
        public string QualifiedName
        {
            get
            {
                CheckDisposed();

                return GetQualifiedName();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of times this namespace has been entered without a
        /// matching exit.
        /// </summary>
        private int referenceCount;
        /// <summary>
        /// Gets the number of times this namespace has been entered without a
        /// matching exit.
        /// </summary>
        public int ReferenceCount
        {
            get { CheckDisposed(); return referenceCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, this namespace has been marked as deleted.
        /// </summary>
        private bool deleted;
        /// <summary>
        /// Gets a value indicating whether this namespace has been marked as
        /// deleted.
        /// </summary>
        public bool Deleted
        {
            get { CheckDisposed(); return deleted; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an imported command by its qualified import
        /// name and returns the qualified name of the command it was exported
        /// from.
        /// </summary>
        /// <param name="qualifiedImportName">
        /// The qualified name of the import to look up.
        /// </param>
        /// <param name="qualifiedExportName">
        /// Upon success, receives the qualified name of the exported command
        /// that the import refers to.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode GetImport(
            string qualifiedImportName,
            ref string qualifiedExportName,
            ref Result error
            )
        {
            CheckDisposed();

            if (qualifiedImportName == null)
            {
                error = "invalid import name";
                return ReturnCode.Error;
            }

            if (!NamespaceOps.IsQualifiedName(qualifiedImportName))
            {
                error = "import name must be qualified";
                return ReturnCode.Error;
            }

            if (imports == null)
            {
                error = String.Format(
                    "imports not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            object @object;

            if (imports.TryGetValue(qualifiedImportName, out @object))
            {
                IAlias alias = @object as IAlias;

                if (alias != null)
                {
                    qualifiedExportName = NamespaceOps.GetAliasName(alias);
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "import name {0} is not an alias",
                        FormatOps.WrapOrNull(qualifiedImportName));
                }
            }
            else
            {
                error = String.Format(
                    "import name {0} not found",
                    FormatOps.WrapOrNull(qualifiedImportName));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method imports a command into this namespace by creating an
        /// alias from the qualified import name to the qualified export name.
        /// </summary>
        /// <param name="targetNamespace">
        /// The namespace that the imported command is being exported from.
        /// </param>
        /// <param name="qualifiedImportName">
        /// The qualified name under which the command is imported into this
        /// namespace.
        /// </param>
        /// <param name="qualifiedExportName">
        /// The qualified name of the exported command being imported.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode AddImport(
            INamespace targetNamespace,
            string qualifiedImportName,
            string qualifiedExportName,
            ref Result error
            )
        {
            CheckDisposed();

            if (qualifiedImportName == null)
            {
                error = "invalid import name";
                return ReturnCode.Error;
            }

            if (qualifiedExportName == null)
            {
                error = "invalid export name";
                return ReturnCode.Error;
            }

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (imports == null)
            {
                error = String.Format(
                    "imports not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            IAlias alias = null;
            Result localResult = null;

            if (interpreter.AddAlias(
                    qualifiedImportName, CommandFlags.None,
                    AliasFlags.NamespaceImport, _Public.ClientData.Empty,
                    interpreter, null, new ArgumentList(qualifiedExportName),
                    null, 0, ref alias, ref localResult) == ReturnCode.Ok)
            {
                alias.SourceNamespace = this;
                alias.TargetNamespace = targetNamespace;
            }
            else
            {
                error = localResult;
                return ReturnCode.Error;
            }

            string nameToken = alias.NameToken;

            if (nameToken == null)
            {
                //
                // NOTE: This should not happen as the alias was successfully
                //       added to the interpreter and the name token cannot be
                //       null in that case.
                //
                error = "invalid alias name";
                return ReturnCode.Error;
            }

            if (imports.ContainsKey(qualifiedImportName))
            {
                Result localError = null;
                ResultList errors = new ResultList();

                errors.Add(String.Format(
                    "can't add import {0} in {1}: already exists",
                    FormatOps.WrapOrNull(nameToken),
                    FormatOps.WrapOrNull(GetDisplayName())));

                if (interpreter.RemoveAliasAndCommand(
                        nameToken, null, false,
                        ref localError) != ReturnCode.Ok)
                {
                    errors.Add(localError);
                }

                error = errors;
                return ReturnCode.Error;
            }

            imports.Add(qualifiedImportName, alias);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method renames an imported command (alias) from one qualified
        /// name to another.
        /// </summary>
        /// <param name="oldQualifiedName">
        /// The current qualified name of the import to rename.
        /// </param>
        /// <param name="newQualifiedName">
        /// The new qualified name for the import.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a non-matching or missing import as an error;
        /// otherwise, such cases are tolerated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RenameImport(
            string oldQualifiedName,
            string newQualifiedName,
            bool strict,
            ref Result error
            )
        {
            CheckDisposed();

            if (imports == null)
            {
                error = String.Format(
                    "imports not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            INamespace oldNamespace = NamespaceOps.LookupParent(
                interpreter, oldQualifiedName, false, true, false, ref error);

            if (oldNamespace == null)
                return ReturnCode.Error;

            INamespace newNamespace = NamespaceOps.LookupParent(
                interpreter, newQualifiedName, false, true, false, ref error);

            if (newNamespace == null)
                return ReturnCode.Error;

            int count = 0;

            ObjectDictionary localImports = new ObjectDictionary(
                (ObjectIDictionary)imports);

            foreach (ObjectPair pair in localImports)
            {
                IAlias alias = pair.Value as IAlias;

                if (alias == null)
                    continue;

                string aliasName = NamespaceOps.GetAliasName(alias);

                if (NamespaceOps.IsSame(
                        alias.TargetNamespace, oldNamespace) &&
                    StringOps.Match(interpreter, MatchMode.Glob, aliasName,
                        ScriptOps.MakeCommandName(oldQualifiedName), false))
                {
                    alias.TargetNamespace = newNamespace;
                    NamespaceOps.SetAliasName(alias, newQualifiedName);

                    return ReturnCode.Ok;
                }
                else if (strict)
                {
                    error = String.Format(
                        "import {0} is not an alias in namespace {1}",
                        FormatOps.WrapOrNull(oldQualifiedName),
                        FormatOps.WrapOrNull(GetDisplayName()));

                    return ReturnCode.Error;
                }
            }

            if (strict && (count == 0))
            {
                error = String.Format(
                    "no imports matched name {0} in namespace {1}",
                    FormatOps.WrapOrNull(oldQualifiedName),
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a single imported command (alias) from this
        /// namespace by its qualified import name.
        /// </summary>
        /// <param name="qualifiedImportName">
        /// The qualified name of the import to remove.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a missing import as an error; otherwise, a missing
        /// import is tolerated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RemoveImport(
            string qualifiedImportName,
            bool strict,
            ref Result error
            )
        {
            CheckDisposed();

            if (qualifiedImportName == null)
            {
                error = "invalid import name";
                return ReturnCode.Error;
            }

            if (imports == null)
            {
                error = String.Format(
                    "imports not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            object @object;

            if (imports.TryGetValue(qualifiedImportName, out @object))
            {
                IAlias alias = @object as IAlias;

                if (alias != null)
                {
                    if (interpreter != null)
                    {
                        string nameToken = alias.NameToken;

                        if (nameToken != null)
                        {
                            if (interpreter.RemoveAliasAndCommand(
                                    nameToken, null, false,
                                    ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }
                        }
                    }

                    if (imports.Remove(qualifiedImportName) || !strict)
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "import name {0} not removed",
                            FormatOps.WrapOrNull(qualifiedImportName));
                    }
                }
                else
                {
                    error = String.Format(
                        "import name {0} is not an alias",
                        FormatOps.WrapOrNull(qualifiedImportName));
                }
            }
            else if (!strict)
            {
                return ReturnCode.Ok;
            }
            else
            {
                error = String.Format(
                    "import name {0} not found",
                    FormatOps.WrapOrNull(qualifiedImportName));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all imported commands (aliases) from this
        /// namespace whose names match the specified pattern.
        /// </summary>
        /// <param name="qualifiedPattern">
        /// The qualified name pattern used to match imports to remove, or null
        /// to match all imports.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the case where no imports match as an error;
        /// otherwise, that case is tolerated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RemoveImports(
            string qualifiedPattern,
            bool strict,
            ref Result error
            )
        {
            CheckDisposed();

            if (imports == null)
            {
                error = String.Format(
                    "imports not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            INamespace patternNamespace = null;

            if ((qualifiedPattern != null) && (interpreter != null))
            {
                patternNamespace = NamespaceOps.LookupParent(
                    interpreter, qualifiedPattern, false, true, false,
                    ref error);

                if (patternNamespace == null)
                    return ReturnCode.Error;
            }

            int count = 0;

            ObjectDictionary localImports = new ObjectDictionary(
                (ObjectIDictionary)imports);

            foreach (ObjectPair pair in localImports)
            {
                IAlias alias = pair.Value as IAlias;

                if (alias == null)
                    continue;

                if (!MatchImportName(
                        qualifiedPattern, pair.Key, alias,
                        ref error))
                {
                    continue;
                }

                if (interpreter != null)
                {
                    string nameToken = alias.NameToken;

                    if ((nameToken != null) &&
                        interpreter.RemoveAliasAndCommand(
                            nameToken, null, false,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                count += imports.Remove(pair.Key) ? 1 : 0;
            }

            if (strict && (count == 0))
            {
                error = String.Format(
                    "no imports matched pattern {0} in namespace {1}",
                    FormatOps.WrapOrNull(qualifiedPattern),
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes every imported command (alias) from this
        /// namespace.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to treat the case where no imports were removed as an
        /// error; otherwise, that case is tolerated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RemoveAllImports(
            bool strict,
            ref Result error
            )
        {
            CheckDisposed();

            if (imports == null)
            {
                error = String.Format(
                    "imports not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            int count = 0;

            ObjectDictionary localImports = new ObjectDictionary(
                (ObjectIDictionary)imports);

            foreach (ObjectPair pair in localImports)
            {
                IAlias alias = pair.Value as IAlias;

                if (alias == null)
                    continue;

                string nameToken = alias.NameToken;

                if ((interpreter != null) && (nameToken != null))
                {
                    if (interpreter.RemoveAliasAndCommand(
                            nameToken, null, false,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                count += imports.Remove(pair.Key) ? 1 : 0;
            }

            if (strict && (count == 0))
            {
                error = String.Format(
                    "no imports removed from namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the names of the commands imported into this
        /// namespace, optionally filtered by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to filter the names, or null to return all
        /// names.
        /// </param>
        /// <param name="keys">
        /// Non-zero to return the qualified import names (the keys); zero to
        /// return the names of the exported commands the imports refer to.
        /// </param>
        /// <param name="tailOnly">
        /// Non-zero to return only the simple (tail) portion of each name; zero
        /// to return the full name.
        /// </param>
        /// <returns>
        /// A list of import names, which is never null but may be empty.
        /// </returns>
        public StringList GetImportNames(
            string pattern,
            bool keys,
            bool tailOnly
            ) /* CANNOT RETURN NULL */
        {
            CheckDisposed();

            StringList list = new StringList();

            if (imports == null)
                return list;

            if (pattern != null)
                pattern = ScriptOps.MakeCommandName(pattern);

            foreach (ObjectPair pair in imports)
            {
                IAlias alias = pair.Value as IAlias;

                if (alias == null)
                    continue;

                if (keys)
                {
                    string importName = pair.Key;

                    string importNameTailOnly = NamespaceOps.TailOnly(
                        importName);

                    if ((pattern == null) || StringOps.Match(
                            interpreter, MatchMode.Glob, importNameTailOnly,
                            pattern, false))
                    {
                        list.Add(tailOnly ? importNameTailOnly : importName);
                    }
                }
                else
                {
                    string aliasName = NamespaceOps.GetAliasName(alias);

                    string aliasNameTailOnly = NamespaceOps.TailOnly(
                        aliasName);

                    if ((pattern == null) || StringOps.Match(
                            interpreter, MatchMode.Glob, aliasNameTailOnly,
                            pattern, false))
                    {
                        list.Add(tailOnly ? aliasNameTailOnly : aliasName);
                    }
                }
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of command name patterns that this namespace exports.
        /// </summary>
        private StringDictionary exportNames;
        /// <summary>
        /// Gets the set of command name patterns that this namespace exports.
        /// </summary>
        public StringDictionary ExportNames
        {
            get { CheckDisposed(); return exportNames; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the command name patterns exported by this
        /// namespace, optionally filtered by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to filter the export names, or null to return
        /// all of them.
        /// </param>
        /// <returns>
        /// A list of exported command name patterns, which is never null but
        /// may be empty.
        /// </returns>
        public StringList GetExportNames(
            string pattern
            )
        {
            CheckDisposed();

            return (exportNames != null) ?
                new StringList(exportNames.Keys).ToList(pattern, false)
                as StringList : new StringList();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the reference count of this namespace,
        /// optionally also entering all of its ancestor namespaces.
        /// </summary>
        /// <param name="all">
        /// Non-zero to also enter every ancestor namespace; zero to enter only
        /// this namespace.
        /// </param>
        /// <returns>
        /// The sum of the reference counts affected by this operation.
        /// </returns>
        public int Enter(
            bool all
            )
        {
            CheckDisposed();

            int count = 0;

            if (all)
            {
                INamespace @namespace = parent;

                while (@namespace != null)
                {
                    count += @namespace.Enter(false);
                    @namespace = @namespace.Parent;
                }
            }

            count += Interlocked.Increment(ref referenceCount);
            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the reference count of this namespace,
        /// optionally also exiting all of its ancestor namespaces.
        /// </summary>
        /// <param name="all">
        /// Non-zero to also exit every ancestor namespace; zero to exit only
        /// this namespace.
        /// </param>
        /// <returns>
        /// The sum of the reference counts affected by this operation.
        /// </returns>
        public int Exit(
            bool all
            )
        {
            CheckDisposed();

            int count = 0;

            if (all)
            {
                INamespace @namespace = parent;

                while (@namespace != null)
                {
                    count += @namespace.Exit(false);
                    @namespace = @namespace.Parent;
                }
            }

            count += Interlocked.Decrement(ref referenceCount);
            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a snapshot of all immediate child namespaces of
        /// this namespace.
        /// </summary>
        /// <returns>
        /// A list of the immediate child namespaces, or null if children are
        /// not available.
        /// </returns>
        public IEnumerable<INamespace> GetAllChildren()
        {
            CheckDisposed();

            return (children != null) ?
                new List<INamespace>(children.Values) : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all immediate child namespaces from this
        /// namespace.
        /// </summary>
        public void ClearAllChildren()
        {
            CheckDisposed();

            if (children == null)
                return;

            children.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method moves all child namespaces from the specified source
        /// namespace into this namespace, reparenting them.  This namespace
        /// cannot become a child of itself, so it is re-added to the source if
        /// necessary.
        /// </summary>
        /// <param name="namespace">
        /// The source namespace whose children are to be moved into this
        /// namespace.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode MoveAllChildren(
            INamespace @namespace,
            ref Result error
            )
        {
            CheckDisposed();

            if (@namespace == null)
            {
                error = "invalid namespace";
                return ReturnCode.Error;
            }

            if (children == null)
            {
                error = String.Format(
                    "children not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            //
            // NOTE: Grab all the children [that need to be moved] from the
            //       existing namespace.
            //
            IEnumerable<INamespace> newChildren = @namespace.GetAllChildren();

            if (newChildren == null)
                return ReturnCode.Ok; /* NOTE: Ok, no children. */

            //
            // NOTE: Pass 1: Verify that no duplicate children exist.
            //
            foreach (INamespace child in newChildren)
            {
                if (child == null)
                    continue;

                if (children.ContainsKey(child.Name))
                {
                    error = String.Format(
                        "can't add {0}: namespace already exists",
                        FormatOps.WrapOrNull(child.Name));

                    return ReturnCode.Error;
                }
            }

            //
            // NOTE: Pass 2: Add all new children to our collection.
            //
            bool reAddThis = false;

            foreach (INamespace child in newChildren)
            {
                if (child == null)
                    continue;

                //
                // BUGFIX: We cannot be a child of ourself.  Therefore, we
                //         need to re-add ourself to the original namespace
                //         after clearing all the other children from it.
                //
                if (Object.ReferenceEquals(child, this))
                {
                    reAddThis = true;
                    continue;
                }

                children.Add(child.Name, child);
                child.Parent = this;
            }

            //
            // NOTE: Next, clear the children from the source namespace.
            //
            @namespace.ClearAllChildren();

            //
            // NOTE: Finally, if necessary, re-add this namespace to the
            //       original one.
            //
            if (reAddThis &&
                (@namespace.AddChild(this, ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the variable frame of this namespace and clears
        /// the stored reference to it in a single operation.
        /// </summary>
        /// <returns>
        /// The variable frame that was associated with this namespace, or null
        /// if there was none.
        /// </returns>
        public ICallFrame GetAndClearVariableFrame()
        {
            CheckDisposed();

            try
            {
                return variableFrame;
            }
            finally
            {
                variableFrame = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this namespace as deleted.
        /// </summary>
        public void MarkDeleted()
        {
            CheckDisposed();

            deleted = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an immediate child namespace by its simple
        /// name.
        /// </summary>
        /// <param name="name">
        /// The simple name of the child namespace to look up.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The matching child namespace, or null if it could not be found.
        /// </returns>
        public INamespace GetChild(
            string name,
            ref Result error
            )
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(name))
            {
                error = "cannot lookup child: invalid name";
                return null;
            }

            if (children == null)
            {
                error = String.Format(
                    "children not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return null;
            }

            INamespace child;

            if (children.TryGetValue(name, out child))
                return child;

            error = String.Format(
                "namespace {0} not found in {1}",
                FormatOps.WrapOrNull(name),
                FormatOps.WrapOrNull(GetDisplayName()));

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds an immediate child namespace to this namespace.
        /// </summary>
        /// <param name="namespace">
        /// The child namespace to add.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode AddChild(
            INamespace @namespace,
            ref Result error
            )
        {
            CheckDisposed();

            if (@namespace == null)
            {
                error = "cannot add child: invalid namespace";
                return ReturnCode.Error;
            }

            if (children == null)
            {
                error = String.Format(
                    "children not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            string name = @namespace.Name;

            if (String.IsNullOrEmpty(name))
            {
                error = "cannot add child: invalid name";
                return ReturnCode.Error;
            }

            if (children.ContainsKey(name))
            {
                error = String.Format(
                    "can't add {0}: namespace already exists",
                    FormatOps.WrapOrNull(name));

                return ReturnCode.Error;
            }

            children.Add(name, @namespace);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method renames an immediate child namespace within this
        /// namespace's child collection.
        /// </summary>
        /// <param name="oldName">
        /// The current simple name of the child namespace.
        /// </param>
        /// <param name="newName">
        /// The new simple name for the child namespace.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RenameChild(
            string oldName,
            string newName,
            ref Result error
            )
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(oldName))
            {
                error = "cannot rename child: invalid old name";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(newName))
            {
                error = "cannot rename child: invalid new name";
                return ReturnCode.Error;
            }

            if (children == null)
            {
                error = String.Format(
                    "children not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            INamespace child;

            if (!children.TryGetValue(oldName, out child))
            {
                error = String.Format(
                    "can't rename from {0}: namespace does not exist",
                    FormatOps.WrapOrNull(oldName));

                return ReturnCode.Error;
            }

            if (children.ContainsKey(newName))
            {
                error = String.Format(
                    "can't rename to {0}: namespace already exists",
                    FormatOps.WrapOrNull(newName));

                return ReturnCode.Error;
            }

            children.Add(newName, child);
            children.Remove(oldName);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes an immediate child namespace from this namespace
        /// by its simple name.
        /// </summary>
        /// <param name="name">
        /// The simple name of the child namespace to remove.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode RemoveChild(
            string name,
            ref Result error
            )
        {
            CheckDisposed();

            if (String.IsNullOrEmpty(name))
            {
                error = "cannot remove child: invalid name";
                return ReturnCode.Error;
            }

            if (children == null)
            {
                error = String.Format(
                    "children not available in namespace {0}",
                    FormatOps.WrapOrNull(GetDisplayName()));

                return ReturnCode.Error;
            }

            if (!children.Remove(name))
            {
                error = String.Format(
                    "can't remove {0}: namespace does not exist",
                    FormatOps.WrapOrNull(name));

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes a callback for this namespace and then,
        /// recursively, for every descendant namespace, stopping early if the
        /// callback returns a non-success code.
        /// </summary>
        /// <param name="callback">
        /// The callback to invoke for this namespace and each descendant.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if every callback succeeded; otherwise,
        /// the first non-success code returned by the callback.
        /// </returns>
        public ReturnCode Traverse(
            NamespaceCallback callback,
            IClientData clientData,
            ref Result error
            )
        {
            CheckDisposed();

            if (callback == null)
            {
                error = "invalid callback";
                return ReturnCode.Error;
            }

            ReturnCode code;

            code = callback(this, clientData, ref error);

            if (code != ReturnCode.Ok)
                return code;

            if (children == null)
                return code;

            if (children.Count == 0)
                return code;

            foreach (NamespacePair pair in children)
            {
                INamespace child = pair.Value;

                if (child == null)
                    continue;

                code = child.Traverse(callback, clientData, ref error);

                if (code != ReturnCode.Ok)
                    return code;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the immediate child namespaces of this namespace
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to match child namespaces, or null to match
        /// all of them.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to include children that have been marked as deleted; zero
        /// to exclude them.
        /// </param>
        /// <returns>
        /// A list of matching child namespaces, which is never null but may be
        /// empty.
        /// </returns>
        public IEnumerable<INamespace> GetChildren(
            string pattern,
            bool deleted
            ) /* CANNOT RETURN NULL */
        {
            CheckDisposed();

            return new List<INamespace>(
                PrivateGetChildren(pattern, deleted).Values);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns all descendant namespaces of this namespace
        /// (children, grandchildren, and so on) that match the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to match descendant namespaces, or null to
        /// match all of them.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to include descendants that have been marked as deleted;
        /// zero to exclude them.
        /// </param>
        /// <returns>
        /// A list of matching descendant namespaces.
        /// </returns>
        public IEnumerable<INamespace> GetDescendants(
            string pattern,
            bool deleted
            )
        {
            CheckDisposed();

            return new List<INamespace>(
                PrivateGetDescendants(pattern, deleted).Values);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method determines whether this namespace is the global
        /// namespace (i.e. it has no parent).
        /// </summary>
        /// <returns>
        /// True if this is the global namespace; otherwise, false.
        /// </returns>
        private bool IsGlobal()
        {
            return (parent == null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the fully-qualified name of this namespace,
        /// computing and caching it on first access.
        /// </summary>
        /// <returns>
        /// The fully-qualified name of this namespace.
        /// </returns>
        private string GetQualifiedName()
        {
            if (qualifiedName == null)
                qualifiedName = NamespaceOps.GetQualifiedName(this, null);

            return qualifiedName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the cached fully-qualified name so that it will
        /// be recomputed on next access.
        /// </summary>
        private void ResetQualifiedName()
        {
            qualifiedName = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a name suitable for displaying this namespace in
        /// diagnostic and error messages.
        /// </summary>
        /// <returns>
        /// The display name of this namespace.
        /// </returns>
        private string GetDisplayName()
        {
            return GetQualifiedName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the origin command name for the specified alias
        /// name within this namespace.
        /// </summary>
        /// <param name="aliasName">
        /// The alias name whose origin is to be resolved.
        /// </param>
        /// <returns>
        /// The origin command name, or null if it could not be resolved.
        /// </returns>
        private string GetOriginName(
            string aliasName
            )
        {
            if (aliasName == null)
                return null;

            Result result = null;

            if (NamespaceOps.Origin(interpreter, this,
                    NamespaceOps.MakeAbsoluteName(aliasName),
                    ref result) == ReturnCode.Ok)
            {
                return ScriptOps.MakeCommandName(result);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method captures the state needed before the name or parent of
        /// this namespace is changed, saving the original name and forcing the
        /// qualified name to be computed for the global namespace.
        /// </summary>
        /// <param name="oldName">
        /// Upon return, receives the original simple name of this namespace.
        /// </param>
        /// <param name="global">
        /// Upon return, indicates whether this is the global namespace.
        /// </param>
        private void BeforeNameChange(
            out string oldName,
            out bool global
            )
        {
            //
            // NOTE: Save the original name for later, we will need it.
            //
            oldName = name;

            //
            // NOTE: Is this the global namespace?
            //
            global = IsGlobal();

            //
            // HACK: Force figuring out the qualified name now, prior to
            //       the local name being changed, if necessary.
            //
            if (global)
                /* IGNORED */
                GetQualifiedName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the bookkeeping needed after the name or parent
        /// of this namespace has changed, recomputing qualified names and
        /// notifying the parent namespace of the rename.  The global namespace's
        /// qualified name cannot be changed.
        /// </summary>
        /// <param name="oldName">
        /// The original simple name of this namespace, as captured by
        /// <see cref="BeforeNameChange" />.
        /// </param>
        /// <param name="global">
        /// Non-zero if this is the global namespace, as captured by
        /// <see cref="BeforeNameChange" />.
        /// </param>
        private void AfterNameChange(
            string oldName,
            bool global
            )
        {
            //
            // HACK: The qualified name of the global namespace cannot be
            //       changed; however, we let the local name be changed.
            //
            if (!global)
            {
                ResetQualifiedName();

                //
                // NOTE: For the rest of the steps in here, we want to make
                //       sure that the name is actually different now.
                //
                if (!NamespaceOps.IsSame(oldName, name))
                {
                    //
                    // HACK: Next, force all child namespaces to recompute
                    //       their qualified names as well.
                    //
                    ResetChildNames(null, true);

                    //
                    // HACK: Finally, "notify" the parent namespace that our
                    //       name has been changed (i.e. so it can update its
                    //       list of children).
                    //
                    if (parent != null)
                    {
                        ReturnCode renameCode;
                        Result renameError = null;

                        renameCode = parent.RenameChild(
                            oldName, name, ref renameError);

                        if (renameCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, renameCode, renameError);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a dictionary of the immediate child namespaces of
        /// this namespace that match the specified pattern.  When the pattern is
        /// qualified, matching is performed against qualified names; otherwise,
        /// it is performed against simple names.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to match child namespaces, or null to match
        /// all of them.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to include children that have been marked as deleted; zero
        /// to exclude them.
        /// </param>
        /// <returns>
        /// A dictionary of matching child namespaces, which is never null but
        /// may be empty.
        /// </returns>
        private NamespaceDictionary PrivateGetChildren(
            string pattern,
            bool deleted
            ) /* CANNOT RETURN NULL */
        {
            NamespaceDictionary dictionary = new NamespaceDictionary();

            if (children != null)
            {
                if (pattern != null)
                {
                    bool qualified = NamespaceOps.IsQualifiedName(pattern);

                    foreach (NamespacePair pair in children)
                    {
                        INamespace child = pair.Value;

                        if (child == null)
                            continue;

                        if (!deleted && child.Deleted)
                            continue;

                        if (qualified)
                        {
                            if (!StringOps.Match(
                                    interpreter, MatchMode.Glob,
                                    child.QualifiedName, pattern, false))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!StringOps.Match(
                                    interpreter, MatchMode.Glob,
                                    pair.Key, pattern, false))
                            {
                                continue;
                            }
                        }

                        dictionary.Add(pair.Key, child);
                    }
                }
                else
                {
                    foreach (NamespacePair pair in children)
                    {
                        INamespace child = pair.Value;

                        if (child == null)
                            continue;

                        if (!deleted && child.Deleted)
                            continue;

                        dictionary.Add(pair.Key, child);
                    }
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a dictionary of all descendant namespaces of this
        /// namespace that match the specified pattern, by traversing the
        /// namespace tree.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to match descendant namespaces, or null to
        /// match all of them.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to include descendants that have been marked as deleted;
        /// zero to exclude them.
        /// </param>
        /// <returns>
        /// A dictionary of matching descendant namespaces, which is never null
        /// but may be empty.
        /// </returns>
        private NamespaceDictionary PrivateGetDescendants(
            string pattern,
            bool deleted
            ) /* CANNOT RETURN NULL */
        {
            NamespaceDictionary dictionary = new NamespaceDictionary();
            Result error = null;

            if (Traverse(
                    GetDescendantsCallback, new ClientData(
                        new DescendantTriplet(false, dictionary,
                        pattern, deleted)),
                    ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "PrivateGetDescendants: error = {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(Namespace).Name,
                    TracePriority.NamespaceError);
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /* Eagle._Components.Public.Delegates.NamespaceCallback */
        /// <summary>
        /// This method is the traversal callback used by
        /// <see cref="PrivateGetDescendants" /> to accumulate matching
        /// descendant namespaces into a dictionary carried in the client data.
        /// </summary>
        /// <param name="namespace">
        /// The namespace currently being visited by the traversal.
        /// </param>
        /// <param name="clientData">
        /// The client data carrying the descendant triplet (the accumulator
        /// dictionary, the match pattern, and the deleted flag).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private ReturnCode GetDescendantsCallback(
            INamespace @namespace,
            IClientData clientData,
            ref Result error
            )
        {
            if (@namespace == null)
            {
                error = "invalid namespace";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                error = "invalid clientData";
                return ReturnCode.Error;
            }

            DescendantTriplet anyTriplet =
                clientData.Data as DescendantTriplet;

            if (anyTriplet == null)
            {
                error = "invalid descendant triplet";
                return ReturnCode.Error;
            }

            NamespaceDictionary dictionary = anyTriplet.X;

            if (dictionary == null)
            {
                error = "invalid namespace dictionary";
                return ReturnCode.Error;
            }

            string pattern = anyTriplet.Y;
            string namespaceName;

            if ((pattern == null) ||
                NamespaceOps.IsQualifiedName(pattern))
            {
                namespaceName = @namespace.QualifiedName;
            }
            else
            {
                namespaceName = @namespace.Name;
            }

            if (namespaceName == null)
            {
                error = "invalid namespace name";
                return ReturnCode.Error;
            }

            bool deleted = anyTriplet.Z;

            if (deleted || !@namespace.Deleted)
            {
                if ((pattern == null) || StringOps.Match(
                        interpreter, MatchMode.Glob,
                        namespaceName, pattern, false))
                {
                    dictionary[namespaceName] = @namespace;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forces the qualified names of the matching child
        /// namespaces to be recomputed, which in turn cascades to their own
        /// children.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to match child namespaces, or null to match
        /// all of them.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to include children that have been marked as deleted; zero
        /// to exclude them.
        /// </param>
        private void ResetChildNames(
            string pattern,
            bool deleted
            )
        {
            foreach (NamespacePair pair in
                    PrivateGetChildren(pattern, deleted))
            {
                INamespace child = pair.Value;

                if (child == null)
                    continue;

                //
                // HACK: Force the qualified name to be recomputed.
                //
                child.Name = child.Name;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether an imported command matches the
        /// specified pattern, checking the import name, the alias name, and the
        /// origin command name in turn.
        /// </summary>
        /// <param name="qualifiedPattern">
        /// The qualified glob pattern to match against, or null to match any
        /// import.
        /// </param>
        /// <param name="importName">
        /// The qualified import name to test.
        /// </param>
        /// <param name="alias">
        /// The alias that implements the import.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the import matches the pattern; otherwise, false.
        /// </returns>
        private bool MatchImportName(
            string qualifiedPattern,
            string importName,
            IAlias alias,
            ref Result error
            )
        {
            if (qualifiedPattern == null)
                return true;

            //
            // TODO: This check is essential for commands that are removed
            //       from an interpreter via PrivateRemoveCommand, e.g. by
            //       the [rename] command, etc.
            //
            if (StringOps.Match(
                    interpreter, MatchMode.Glob, importName,
                    ScriptOps.MakeCommandName(qualifiedPattern), false))
            {
                return true;
            }

            //
            // TODO: This check is essential for commands that are removed
            //       from an interpreter via PrivateRemoveCommand, e.g. by
            //       the [rename] command, etc.  Also, it can be triggered
            //       by using the [namespace delete] or [namespace forget]
            //       sub-commands.
            //
            string aliasName = NamespaceOps.GetAliasName(alias);

            if (StringOps.Match(
                    interpreter, MatchMode.Glob, aliasName,
                    ScriptOps.MakeCommandName(qualifiedPattern), false))
            {
                return true;
            }

            //
            // TODO: This can be triggered by using the [namespace delete]
            //       or [namespace forget] sub-commands.
            //
            string originName = GetOriginName(aliasName);

            if (originName == null)
                return false;

            if (StringOps.Match(
                    interpreter, MatchMode.Glob, originName,
                    ScriptOps.MakeCommandName(qualifiedPattern), false))
            {
                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string that represents this namespace.
        /// </summary>
        /// <returns>
        /// The simple (unqualified) name of this namespace.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this namespace has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this namespace has already been
        /// disposed and the engine is configured to throw on use of a disposed
        /// object.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this namespace has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(Namespace));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this namespace.  It
        /// implements the standard dispose pattern, disposing child namespaces,
        /// clearing the export names, removing all imports, freeing the variable
        /// frame, and clearing the remaining state.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (children != null)
                    {
                        foreach (INamespace child in children.Values)
                        {
                            if (child == null)
                                continue;

                            IDisposable disposable = child as IDisposable;

                            if (disposable != null)
                            {
                                disposable.Dispose();
                                disposable = null;
                            }
                        }

                        children.Clear();
                        children = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (exportNames != null)
                    {
                        exportNames.Clear();
                        exportNames = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (imports != null)
                    {
                        ReturnCode removeCode;
                        Result removeError = null;

                        removeCode = RemoveAllImports(
                            false, ref removeError);

                        if (removeCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeError);
                        }

                        imports.Clear();
                        imports = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (interpreter != null)
                        interpreter = null; /* NOT OWNED */

                    ///////////////////////////////////////////////////////////

                    parent = null; /* NOT OWNED */
                    resolve = null; /* NOT OWNED */

                    ///////////////////////////////////////////////////////////

                    if (variableFrame != null)
                    {
                        variableFrame.Free(true);
                        variableFrame = null;
                    }

                    ///////////////////////////////////////////////////////////

                    unknown = null;

                    ///////////////////////////////////////////////////////////

                    qualifiedName = null;
                    referenceCount = 0;
                    deleted = false;

                    ///////////////////////////////////////////////////////////

                    kind = IdentifierKind.None;
                    id = Guid.Empty;
                    name = null;
                    group = null;
                    description = null;
                    clientData = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this namespace and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this namespace, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Namespace()
        {
            Dispose(false);
        }
        #endregion
    }
}
