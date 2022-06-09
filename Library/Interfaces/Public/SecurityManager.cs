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

        StringList TrustedPaths { get; } /* WARNING: Trusted by the default [source] policy. */
        UriDictionary<object> TrustedUris { get; } /* WARNING: Trusted by the default [source] policy. */
        ObjectDictionary TrustedTypes { get; } /* WARNING: Trusted by the default [object] policy. */

        PolicyDecision CommandDecision { get; set; } /* NOTE: Default, before command policies. */
        PolicyDecision ScriptDecision { get; set; } /* NOTE: Default, before script policies. */
        PolicyDecision FileDecision { get; set; } /* NOTE: Default, before file policies. */
        PolicyDecision StreamDecision { get; set; } /* NOTE: Default, before/after stream policies. */

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
