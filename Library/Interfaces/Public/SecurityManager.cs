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
    /// <summary>
    /// This interface is implemented by the component responsible for
    /// managing the security posture of an Eagle interpreter, including the
    /// default and final policy decisions for commands, scripts, files, and
    /// streams, and the operations used to query and change whether the
    /// interpreter is safe, restricted, standard, or trusted.
    /// </summary>
    [ObjectId("948e4b2a-52a5-48d5-aa66-1555445cadc9")]
    public interface ISecurityManager
    {
        ///////////////////////////////////////////////////////////////////////
        // SECURITY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the default policy decision applied to commands before
        /// any policies have been consulted.
        /// </summary>
        PolicyDecision CommandInitialDecision { get; set; } /* NOTE: Default, before policies. */
        /// <summary>
        /// Gets or sets the default policy decision applied to scripts before any
        /// policies have been consulted.
        /// </summary>
        PolicyDecision ScriptInitialDecision { get; set; } /* NOTE: Default, before policies. */
        /// <summary>
        /// Gets or sets the default policy decision applied to files before any
        /// policies have been consulted.
        /// </summary>
        PolicyDecision FileInitialDecision { get; set; } /* NOTE: Default, before policies. */
        /// <summary>
        /// Gets or sets the default policy decision applied to streams before any
        /// policies have been consulted.
        /// </summary>
        PolicyDecision StreamInitialDecision { get; set; } /* NOTE: Default, before policies. */

        /// <summary>
        /// Gets or sets the policy decision applied to commands after all
        /// policies have been consulted.
        /// </summary>
        PolicyDecision CommandFinalDecision { get; set; } /* NOTE: Previous, after policies. */
        /// <summary>
        /// Gets or sets the policy decision applied to scripts after all policies
        /// have been consulted.
        /// </summary>
        PolicyDecision ScriptFinalDecision { get; set; } /* NOTE: Previous, after policies. */
        /// <summary>
        /// Gets or sets the policy decision applied to files after all policies
        /// have been consulted.
        /// </summary>
        PolicyDecision FileFinalDecision { get; set; } /* NOTE: Previous, after policies. */
        /// <summary>
        /// Gets or sets the policy decision applied to streams after all policies
        /// have been consulted.
        /// </summary>
        PolicyDecision StreamFinalDecision { get; set; } /* NOTE: Previous, after policies. */

        /// <summary>
        /// Determines whether this interpreter is operating in restricted
        /// mode.
        /// </summary>
        /// <returns>
        /// True if the interpreter is restricted; otherwise, false.
        /// </returns>
        bool IsRestricted();

        /// <summary>
        /// Determines whether this interpreter is operating as a security
        /// software development kit.
        /// </summary>
        /// <returns>
        /// True if the interpreter is operating as a security software
        /// development kit; otherwise, false.
        /// </returns>
        bool IsSecuritySdk();
        /// <summary>
        /// Determines whether this interpreter is operating as a license
        /// software development kit.
        /// </summary>
        /// <returns>
        /// True if the interpreter is operating as a license software
        /// development kit; otherwise, false.
        /// </returns>
        bool IsLicenseSdk();
        /// <summary>
        /// Determines whether this interpreter is operating as any kind of
        /// software development kit.
        /// </summary>
        /// <returns>
        /// True if the interpreter is operating as any kind of software
        /// development kit; otherwise, false.
        /// </returns>
        bool IsAnySdk();
        /// <summary>
        /// Determines whether this interpreter is operating as the specified
        /// kind(s) of software development kit.
        /// </summary>
        /// <param name="sdkType">
        /// The kind(s) of software development kit to check for.
        /// </param>
        /// <param name="all">
        /// True to require that all of the specified kinds be present;
        /// false to require only any of them.
        /// </param>
        /// <returns>
        /// True if the interpreter is operating as the specified kind(s)
        /// of software development kit; otherwise, false.
        /// </returns>
        bool IsSdk(SdkType sdkType, bool all);

        /// <summary>
        /// Determines whether commands and other entities marked unsafe are
        /// hidden in this interpreter.
        /// </summary>
        /// <returns>
        /// True if unsafe entities are hidden; otherwise, false.
        /// </returns>
        bool IsHideUnsafe();

        /// <summary>
        /// Determines whether this interpreter is operating in safe mode.
        /// </summary>
        /// <returns>
        /// True if the interpreter is safe; otherwise, false.
        /// </returns>
        bool IsSafe();
        /// <summary>
        /// Changes whether this interpreter is operating in safe mode.
        /// </summary>
        /// <param name="makeFlags">
        /// The flags used to control how the change is performed.
        /// </param>
        /// <param name="safe">
        /// True to make the interpreter safe; false to make it unsafe.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MakeSafe(MakeFlags makeFlags, bool safe, ref Result error);

        /// <summary>
        /// Marks this interpreter as trusted.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MarkTrusted(ref Result error); /* WARNING: Dangerous. */
        /// <summary>
        /// Marks this interpreter as safe.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MarkSafe(ref Result error); /* WARNING: Dangerous. */

        /// <summary>
        /// Locks this interpreter and then marks it as trusted.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode LockAndMarkTrusted(ref Result error);
        /// <summary>
        /// Marks this interpreter as safe and then unlocks it.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MarkSafeAndUnlock(ref Result error);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the recorded value indicating whether security was previously
        /// enabled for this interpreter, for later restoration.
        /// </summary>
        /// <param name="enabled">
        /// True if security was enabled, false if it was disabled, or
        /// null to clear the recorded state.
        /// </param>
        /// <returns>
        /// True if the recorded value was changed; otherwise, false.
        /// </returns>
        bool SetSecurityWasEnabled(bool? enabled);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this interpreter is operating in standard mode.
        /// </summary>
        /// <returns>
        /// True if the interpreter is standard; otherwise, false.
        /// </returns>
        bool IsStandard();

        /// <summary>
        /// Changes whether this interpreter is operating in standard mode.
        /// </summary>
        /// <param name="makeFlags">
        /// The flags used to control how the change is performed.
        /// </param>
        /// <param name="standard">
        /// True to make the interpreter standard; false otherwise.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MakeStandard(MakeFlags makeFlags, bool standard,
            ref Result error);
    }
}
