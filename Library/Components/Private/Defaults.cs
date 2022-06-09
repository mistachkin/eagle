/*
 * Defaults.cs --
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

namespace Eagle._Components.Private
{
    [ObjectId("ae7a1c2c-0e8d-4830-8004-d59af53f3aff")]
    internal static class Defaults
    {
        ///////////////////////////////////////////////////////////////////////
        //    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*    //
        ///////////////////////////////////////////////////////////////////////
        //
        // HACK: These are all purposely not read-only.  They are used to
        //       abstract away hard-coded default flag values.  Please do
        //       not change these values unless you know exactly how they
        //       are used.
        //
        ///////////////////////////////////////////////////////////////////////
        //    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*    //
        ///////////////////////////////////////////////////////////////////////

        #region Public Static Data
        public static CreationFlagTypes CreationFlagTypes =
            CreationFlagTypes.Default;

        ///////////////////////////////////////////////////////////////////////

        public static CreateFlags CreateFlags = CreateFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        public static HostCreateFlags HostCreateFlags =
            HostCreateFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        public static InitializeFlags InitializeFlags =
            InitializeFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        public static ScriptFlags ScriptFlags = ScriptFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        public static InterpreterFlags InterpreterFlags =
            InterpreterFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        public static InterpreterStateFlags InterpreterStateFlags =
            InterpreterStateFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags PluginFlags = PluginFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        public static PackageIndexFlags PackageIndexFlags =
            PackageIndexFlags.Default;
        #endregion
    }
}
