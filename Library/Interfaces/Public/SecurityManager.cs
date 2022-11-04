/*
 * SecurityManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("948e4b2a-52a5-48d5-aa66-1555445cadc9")]
    public interface ISecurityManager
    {
        ///////////////////////////////////////////////////////////////////////
        // SECURITY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        PolicyDecision CommandInitialDecision { get; set; } /* NOTE: Default, before policies. */
        PolicyDecision ScriptInitialDecision { get; set; } /* NOTE: Default, before policies. */
        PolicyDecision FileInitialDecision { get; set; } /* NOTE: Default, before policies. */
        PolicyDecision StreamInitialDecision { get; set; } /* NOTE: Default, before policies. */

        PolicyDecision CommandFinalDecision { get; set; } /* NOTE: Previous, after policies. */
        PolicyDecision ScriptFinalDecision { get; set; } /* NOTE: Previous, after policies. */
        PolicyDecision FileFinalDecision { get; set; } /* NOTE: Previous, after policies. */
        PolicyDecision StreamFinalDecision { get; set; } /* NOTE: Previous, after policies. */

        bool IsRestricted();

        bool IsSecuritySdk();
        bool IsLicenseSdk();
        bool IsAnySdk();
        bool IsSdk(SdkType sdkType, bool all);

        bool IsSafe();
        ReturnCode MakeSafe(MakeFlags makeFlags, bool safe, ref Result error);

        ReturnCode MarkTrusted(ref Result error); /* WARNING: Dangerous. */
        ReturnCode MarkSafe(ref Result error); /* WARNING: Dangerous. */

        ReturnCode LockAndMarkTrusted(ref Result error);
        ReturnCode MarkSafeAndUnlock(ref Result error);

        ///////////////////////////////////////////////////////////////////////

        bool SetSecurityWasEnabled(bool? enabled);

        ///////////////////////////////////////////////////////////////////////

        bool IsStandard();

        ReturnCode MakeStandard(MakeFlags makeFlags, bool standard,
            ref Result error);
    }
}
