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
    [ObjectId("5f2b9883-f5da-4d3c-85b8-cddb6b0de9f8")]
    internal sealed class Namespace :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IIdentifier, IMaybeDisposed, INamespace, IDisposable
    {
        #region Private Data
        private Dictionary<string, INamespace> children;
        private ObjectDictionary imports;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
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
        private string name;
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
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { return false; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter /* READ-ONLY */
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INamespaceData Members
        private INamespace parent;
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

        private IResolve resolve;
        public IResolve Resolve /* READ-ONLY */
        {
            get { CheckDisposed(); return resolve; }
            set { CheckDisposed(); throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame variableFrame;
        public ICallFrame VariableFrame /* READ-ONLY */
        {
            get { CheckDisposed(); return variableFrame; }
            set { CheckDisposed(); throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private string unknown;
        public string Unknown
        {
            get { CheckDisposed(); return unknown; }
            set { CheckDisposed(); unknown = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INamespace Members
        private string qualifiedName;
        public string QualifiedName
        {
            get
            {
                CheckDisposed();

                return GetQualifiedName();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int referenceCount;
        public int ReferenceCount
        {
            get { CheckDisposed(); return referenceCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool deleted;
        public bool Deleted
        {
            get { CheckDisposed(); return deleted; }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private StringDictionary exportNames;
        public StringDictionary ExportNames
        {
            get { CheckDisposed(); return exportNames; }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public IEnumerable<INamespace> GetAllChildren()
        {
            CheckDisposed();

            return (children != null) ?
                new List<INamespace>(children.Values) : null;
        }

        ///////////////////////////////////////////////////////////////////////

        public void ClearAllChildren()
        {
            CheckDisposed();

            if (children == null)
                return;

            children.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

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

        public void MarkDeleted()
        {
            CheckDisposed();

            deleted = true;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool IsGlobal()
        {
            return (parent == null);
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetQualifiedName()
        {
            if (qualifiedName == null)
                qualifiedName = NamespaceOps.GetQualifiedName(this, null);

            return qualifiedName;
        }

        ///////////////////////////////////////////////////////////////////////

        private void ResetQualifiedName()
        {
            qualifiedName = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetDisplayName()
        {
            return GetQualifiedName();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            CheckDisposed();

            return name;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(Namespace));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Namespace()
        {
            Dispose(false);
        }
        #endregion
    }
}
