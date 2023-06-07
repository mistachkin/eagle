/*
 * PolicyContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("94d0d340-c819-4054-abbf-bb6ccfed0ed3")]
    public interface IPolicyContext :
            IGetClientData, IHaveInterpreter, IHavePlugin, ITypeAndName
    {
        //
        // NOTE: *WARNING* This is the plugin that contains
        //       the policy currently being invoked (i.e. it
        //       can change with each callback).
        //
        // IPlugin Plugin { get; set; }

        PolicyFlags Flags { get; }

        AssemblyName AssemblyName { get; }

        IExecute Execute { get; }
        ArgumentList Arguments { get; }

        IScript Script { get; }
        string FileName { get; }
        byte[] Bytes { get; }
        string Text { get; }
        Encoding Encoding { get; }

        byte[] HashValue { get; }
        string HashAlgorithmName { get; }

        //
        // NOTE: *WARNING* For informational purposes only.
        //       Please DO NOT USE to make policy decisions.
        //
        Result Result { get; set; }

        PolicyDecision OriginalDecision { get; }
        PolicyDecision Decision { get; }
        Result Reason { get; } /* OPTIONAL: Reason for the decision. */

        bool IsUndecided();
        bool IsDenied();
        bool IsApproved();

        void Undecided();
        void Denied();
        void Approved();

        void Undecided(Result reason);
        void Denied(Result reason);
        void Approved(Result reason);

        [Obsolete()]
        void Trace(string category, TracePriority priority);

        void Trace(string category, TracePriority priority, bool full);
    }
}
