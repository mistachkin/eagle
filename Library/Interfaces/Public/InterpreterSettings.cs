/*
 * InterpreterSettings.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface extends <see cref="IInterpreterSettingsData" /> with the
    /// operations used to manipulate a set of interpreter settings, such as
    /// making them safe or standard, enabling or disabling various subsystems,
    /// and importing flags and objects from an existing interpreter.
    /// </summary>
    [ObjectId("66ba02d6-f6b8-4142-96f7-14fc76813b5e")]
    public interface IInterpreterSettings : IInterpreterSettingsData
    {
        /// <summary>
        /// Adjusts these settings so that the resulting interpreter will be a
        /// safe interpreter.
        /// </summary>
        void MakeSafe();

        /// <summary>
        /// Adjusts these settings so that the resulting interpreter will be a
        /// standard interpreter.
        /// </summary>
        void MakeStandard();

        /// <summary>
        /// Adjusts these settings so that automatic initialization of the
        /// resulting interpreter is disabled.
        /// </summary>
        void DisableInitialize();

        /// <summary>
        /// Adjusts these settings so that namespace support is enabled for the
        /// resulting interpreter.
        /// </summary>
        void EnableNamespaces();

        /// <summary>
        /// Adjusts these settings so that namespace support is disabled for the
        /// resulting interpreter.
        /// </summary>
        void DisableNamespaces();

        /// <summary>
        /// Adjusts these settings so that the plugin loader is disabled for the
        /// resulting interpreter.
        /// </summary>
        void DisableLoader();

        /// <summary>
        /// Adjusts these settings so that initialization of the resulting
        /// interpreter is disabled.
        /// </summary>
        void DisableInitialization();

        /// <summary>
        /// Adjusts these settings so that setting of the auto-path is disabled
        /// for the resulting interpreter.
        /// </summary>
        void DisableSetAutoPath();

        /// <summary>
        /// Removes any options from these settings that are considered unsafe.
        /// </summary>
        void RemoveUnsafeOptions();

        /// <summary>
        /// Removes any test-related options from these settings that are
        /// considered unsafe.
        /// </summary>
        void RemoveUnsafeTestOptions();

        /// <summary>
        /// Adjusts these settings so that security is enabled for the resulting
        /// interpreter.
        /// </summary>
        void EnableSecurity();

        /// <summary>
        /// Adjusts these settings so that security is disabled for the
        /// resulting interpreter.
        /// </summary>
        void DisableSecurity();

        /// <summary>
        /// Resets all of these settings back to their default values.
        /// </summary>
        void ResetEverything();

        /// <summary>
        /// Resets the various flag-related settings back to their default
        /// values.
        /// </summary>
        void UseDefaultsForFlags();

        /// <summary>
        /// Copies the various flag-related settings from the specified
        /// interpreter into these settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to copy the flag-related settings from.  This
        /// parameter should not be null.
        /// </param>
        void UseFlagsFromInterpreter(Interpreter interpreter);

        /// <summary>
        /// Copies the various object-related settings from the specified
        /// interpreter into these settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to copy the object-related settings from.  This
        /// parameter should not be null.
        /// </param>
        void UseObjectsFromInterpreter(Interpreter interpreter);

        /// <summary>
        /// Sets the rule set for these settings, if it is permitted to do so.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set to associate with these settings.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MaybeSetRuleSet(IRuleSet ruleSet, ref Result error);
    }
}
