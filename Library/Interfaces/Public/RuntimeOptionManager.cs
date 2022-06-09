/*
 * RuntimeOptionManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    //
    // NOTE: The configured "runtime options" are RESERVED for use by the host
    //       application and/or scripts.  The core library itself does not make
    //       use of them; however, the core script library is allowed to create
    //       and/or make use of any that have a name prefixed with "eagle".
    //
    [ObjectId("09f5da81-c862-4b38-8a18-4a0c3668b737")]
    public interface IRuntimeOptionManager
    {
        ///////////////////////////////////////////////////////////////////////
        // RUNTIME OPTION DATA
        ///////////////////////////////////////////////////////////////////////

        ClientDataDictionary RuntimeOptions { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // RUNTIME OPTION MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        bool HasRuntimeOption(string name);
        bool ClearRuntimeOptions();
        bool AddRuntimeOption(string name);
        bool RemoveRuntimeOption(string name);
        bool ChangeRuntimeOption(string name);
    }
}
