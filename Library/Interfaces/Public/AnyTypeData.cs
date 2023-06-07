/*
 * AnyTypeData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("c05b8893-ac64-44c9-a45f-8740dd41cc9e")]
    public interface IAnyTypeData
    {
        bool TryGetClientData(
            string name,
            out IClientData value,
            ref Result error
            );

        bool TryGetString(
            string name,
            bool toString,
            out string value,
            ref Result error
            );

        bool TryGetStringList(
            Interpreter interpreter,
            string name,
            bool toString,
            out StringList value,
            ref Result error
            );

        bool TryGetGuid(
            string name,
            bool toString,
            out Guid value,
            ref Result error
            );

        bool TryGetUri(
            string name,
            UriKind uriKind,
            bool toString,
            out Uri value,
            ref Result error
            );

        bool TryGetVersion(
            string name,
            bool toString,
            out Version value,
            ref Result error
            );

        bool TryGetInterpreter(
            Interpreter interpreter,
            string name,
            bool toString,
            out Interpreter value,
            ref Result error
            );

        bool TryGetPlugin(
            Interpreter interpreter,
            string name,
            bool toString,
            out IPlugin value,
            ref Result error
            );

        bool TryGetRuleSet(
            Interpreter interpreter,
            string name,
            bool toString,
            out IRuleSet value,
            ref Result error
            );

        bool TryGetObject(
            Interpreter interpreter,
            string name,
            bool toString,
            out IObject value,
            ref Result error
            );

        bool TryGetEncoding(
            Interpreter interpreter,
            string name,
            bool toString,
            out Encoding value,
            ref Result error
            );

        bool TryGetByteArray(
            string name,
            bool toString,
            out byte[] value,
            ref Result error
            );
    }
}
