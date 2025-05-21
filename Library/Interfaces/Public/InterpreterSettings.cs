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
    [ObjectId("66ba02d6-f6b8-4142-96f7-14fc76813b5e")]
    public interface IInterpreterSettings : IInterpreterSettingsData
    {
        void MakeSafe();
        void MakeStandard();
        void DisableInitialize();
        void EnableNamespaces();
        void DisableNamespaces();
        void DisableLoader();
        void DisableInitialization();
        void DisableSetAutoPath();
        void RemoveUnsafeOptions();
        void RemoveUnsafeTestOptions();
        void EnableSecurity();
        void DisableSecurity();
        void ResetEverything();
        void UseDefaultsForFlags();
        void UseFlagsFromInterpreter(Interpreter interpreter);
        void UseObjectsFromInterpreter(Interpreter interpreter);
        ReturnCode MaybeSetRuleSet(IRuleSet ruleSet, ref Result error);
    }
}
