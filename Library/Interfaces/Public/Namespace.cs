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
    [ObjectId("6024fa55-6831-48db-b47b-6606e16c080a")]
    public interface INamespace : INamespaceData
    {
        string QualifiedName { get; }
        int ReferenceCount { get; }
        bool Deleted { get; }

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetImport(
            string qualifiedImportName,
            ref string qualifiedExportName,
            ref Result error
            );

        ReturnCode AddImport(
            INamespace targetNamespace,
            string qualifiedImportName,
            string qualifiedExportName,
            ref Result error
            );

        ReturnCode RenameImport(
            string qualifiedOldName,
            string qualifiedNewName,
            bool strict,
            ref Result error
            );

        ReturnCode RemoveImport(
            string qualifiedImportName,
            bool strict,
            ref Result error
            );

        ReturnCode RemoveImports(
            string qualifiedPattern,
            bool strict,
            ref Result error
            );

        ReturnCode RemoveAllImports(
            bool strict,
            ref Result error
            );

        StringList GetImportNames(string pattern, bool keys, bool tailOnly);

        ///////////////////////////////////////////////////////////////////////

        StringDictionary ExportNames { get; }
        StringList GetExportNames(string pattern);

        ///////////////////////////////////////////////////////////////////////

        int Enter(bool all);
        int Exit(bool all);

        ///////////////////////////////////////////////////////////////////////

        void MarkDeleted();

        ///////////////////////////////////////////////////////////////////////

        IEnumerable<INamespace> GetAllChildren();
        void ClearAllChildren();
        ReturnCode MoveAllChildren(INamespace @namespace, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        ICallFrame GetAndClearVariableFrame();

        ///////////////////////////////////////////////////////////////////////

        INamespace GetChild(string name, ref Result error);
        ReturnCode AddChild(INamespace @namespace, ref Result error);

        ReturnCode RenameChild(
            string oldName,
            string newName,
            ref Result error
            );

        ReturnCode RemoveChild(string name, ref Result error);

        ReturnCode Traverse(
            NamespaceCallback callback,
            IClientData clientData,
            ref Result error
            );

        IEnumerable<INamespace> GetChildren(string pattern, bool deleted);
        IEnumerable<INamespace> GetDescendants(string pattern, bool deleted);
    }
}
