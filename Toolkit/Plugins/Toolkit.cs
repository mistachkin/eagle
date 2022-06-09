/*
 * Toolkit.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this code, it is a proof-of-concept only.  It is not production ready.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////////////////////

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    [ObjectId("eb156d13-ddad-4a0a-88f3-d979553d22c1")]
    [PluginFlags(
        PluginFlags.Primary | PluginFlags.System |
        PluginFlags.Host | PluginFlags.Command |
        PluginFlags.Static | PluginFlags.MergeCommands |
        PluginFlags.UserInterface)]
    internal sealed class Toolkit : Default
    {
        #region Public Constructors
        public Toolkit(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= Utility.GetPluginFlags(GetType().BaseType) |
                Utility.GetPluginFlags(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = Utility.FormatPluginAbout(this, true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Options(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = new StringList(DefineConstants.OptionList, false);
            return ReturnCode.Ok;
        }
        #endregion
    }
}
