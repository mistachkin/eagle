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

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by an Eagle namespace.  It builds on the
    /// identity and configuration exposed by <see cref="INamespaceData" /> to
    /// provide command and variable name resolution, import and export
    /// management, child namespace management, and reference-count based
    /// lifetime tracking.
    /// </summary>
    [ObjectId("6024fa55-6831-48db-b47b-6606e16c080a")]
    public interface INamespace : INamespaceData
    {
        /// <summary>
        /// Gets the fully qualified name of this namespace.
        /// </summary>
        string QualifiedName { get; }
        /// <summary>
        /// Gets the current reference count for this namespace, reflecting how
        /// many times it has been entered without a matching exit.
        /// </summary>
        int ReferenceCount { get; }
        /// <summary>
        /// Gets a value indicating whether this namespace has been marked as
        /// deleted.  True if the namespace has been deleted; otherwise, false.
        /// </summary>
        bool Deleted { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up the qualified export name that backs a given qualified
        /// import name within this namespace.
        /// </summary>
        /// <param name="qualifiedImportName">
        /// The fully qualified import name to look up.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="qualifiedExportName">
        /// Upon success, receives the fully qualified export name associated
        /// with the specified import name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetImport(
            string qualifiedImportName,
            ref string qualifiedExportName,
            ref Result error
            );

        /// <summary>
        /// Adds an import that maps a qualified import name in this namespace
        /// to a qualified export name in another namespace.
        /// </summary>
        /// <param name="targetNamespace">
        /// The namespace that exports the name being imported.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="qualifiedImportName">
        /// The fully qualified import name to create within this namespace.
        /// This parameter should not be null.
        /// </param>
        /// <param name="qualifiedExportName">
        /// The fully qualified export name, within the target namespace, that
        /// the import refers to.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddImport(
            INamespace targetNamespace,
            string qualifiedImportName,
            string qualifiedExportName,
            ref Result error
            );

        /// <summary>
        /// Renames an existing import within this namespace.
        /// </summary>
        /// <param name="qualifiedOldName">
        /// The fully qualified import name as it currently exists.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="qualifiedNewName">
        /// The fully qualified import name to rename it to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero if the absence of the existing import should be treated as
        /// an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RenameImport(
            string qualifiedOldName,
            string qualifiedNewName,
            bool strict,
            ref Result error
            );

        /// <summary>
        /// Removes a single import from this namespace.
        /// </summary>
        /// <param name="qualifiedImportName">
        /// The fully qualified import name to remove.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero if the absence of the import should be treated as an
        /// error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveImport(
            string qualifiedImportName,
            bool strict,
            ref Result error
            );

        /// <summary>
        /// Removes all imports from this namespace whose names match the
        /// specified pattern.
        /// </summary>
        /// <param name="qualifiedPattern">
        /// The fully qualified pattern used to match import names to remove.
        /// This parameter should not be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero if the absence of any matching import should be treated as
        /// an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveImports(
            string qualifiedPattern,
            bool strict,
            ref Result error
            );

        /// <summary>
        /// Removes every import from this namespace.
        /// </summary>
        /// <param name="strict">
        /// Non-zero if the absence of any imports should be treated as an
        /// error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveAllImports(
            bool strict,
            ref Result error
            );

        /// <summary>
        /// Returns the names of the imports in this namespace that match the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match import names.  A null value matches all
        /// names.
        /// </param>
        /// <param name="keys">
        /// Non-zero to return the import (key) names; otherwise, the
        /// associated export (value) names are returned.
        /// </param>
        /// <param name="tailOnly">
        /// Non-zero to return only the tail (unqualified) portion of each
        /// matched name.
        /// </param>
        /// <returns>
        /// The list of matching names.
        /// </returns>
        StringList GetImportNames(string pattern, bool keys, bool tailOnly);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the dictionary of export patterns currently defined for this
        /// namespace.
        /// </summary>
        StringDictionary ExportNames { get; }
        /// <summary>
        /// Returns the export names in this namespace that match the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match export names.  A null value matches all
        /// names.
        /// </param>
        /// <returns>
        /// The list of matching export names.
        /// </returns>
        StringList GetExportNames(string pattern);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Increments the reference count for this namespace, marking it as
        /// entered.
        /// </summary>
        /// <param name="all">
        /// Non-zero to also enter all ancestor namespaces of this namespace.
        /// </param>
        /// <returns>
        /// The updated reference count for this namespace.
        /// </returns>
        int Enter(bool all);
        /// <summary>
        /// Decrements the reference count for this namespace, marking it as
        /// exited.
        /// </summary>
        /// <param name="all">
        /// Non-zero to also exit all ancestor namespaces of this namespace.
        /// </param>
        /// <returns>
        /// The updated reference count for this namespace.
        /// </returns>
        int Exit(bool all);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Marks this namespace as deleted.
        /// </summary>
        void MarkDeleted();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns all immediate child namespaces of this namespace.
        /// </summary>
        /// <returns>
        /// An enumerable of the child namespaces.
        /// </returns>
        IEnumerable<INamespace> GetAllChildren();
        /// <summary>
        /// Removes all child namespaces from this namespace.
        /// </summary>
        void ClearAllChildren();
        /// <summary>
        /// Moves all child namespaces of this namespace into another
        /// namespace.
        /// </summary>
        /// <param name="namespace">
        /// The namespace to receive the child namespaces.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MoveAllChildren(INamespace @namespace, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the call frame holding the variables for this namespace and
        /// clears the reference to it from this namespace.
        /// </summary>
        /// <returns>
        /// The variable call frame for this namespace, if any; otherwise,
        /// null.
        /// </returns>
        ICallFrame GetAndClearVariableFrame();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the immediate child namespace with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the child namespace to return.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The matching child namespace, if found; otherwise, null.
        /// </returns>
        INamespace GetChild(string name, ref Result error);
        /// <summary>
        /// Adds a child namespace to this namespace.
        /// </summary>
        /// <param name="namespace">
        /// The child namespace to add.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddChild(INamespace @namespace, ref Result error);

        /// <summary>
        /// Renames an immediate child namespace of this namespace.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the child namespace.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="newName">
        /// The new name for the child namespace.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RenameChild(
            string oldName,
            string newName,
            ref Result error
            );

        /// <summary>
        /// Removes an immediate child namespace from this namespace.
        /// </summary>
        /// <param name="name">
        /// The name of the child namespace to remove.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveChild(string name, ref Result error);

        /// <summary>
        /// Traverses this namespace and its descendants, invoking the
        /// specified callback for each one.
        /// </summary>
        /// <param name="callback">
        /// The callback to invoke for each visited namespace.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data to pass to the callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Traverse(
            NamespaceCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Returns the immediate child namespaces of this namespace that match
        /// the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match child namespace names.  A null value
        /// matches all names.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to also include child namespaces that have been marked as
        /// deleted.
        /// </param>
        /// <returns>
        /// An enumerable of the matching child namespaces.
        /// </returns>
        IEnumerable<INamespace> GetChildren(string pattern, bool deleted);
        /// <summary>
        /// Returns all descendant namespaces of this namespace that match the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match descendant namespace names.  A null value
        /// matches all names.
        /// </param>
        /// <param name="deleted">
        /// Non-zero to also include descendant namespaces that have been
        /// marked as deleted.
        /// </param>
        /// <returns>
        /// An enumerable of the matching descendant namespaces.
        /// </returns>
        IEnumerable<INamespace> GetDescendants(string pattern, bool deleted);
    }
}
