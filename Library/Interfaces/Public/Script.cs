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

using System.Collections;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("15da79ef-3b2a-42fc-97bc-da5b0384e23e")]
    public interface IScript :
            IScriptData, IScriptFlags, IScriptLocation,
            ICollection, IIdentifier
    {
        ObjectDictionary MaybeGetExtra();
        ObjectDictionary GetExtra();

        void MakeImmutable();
    }
}
