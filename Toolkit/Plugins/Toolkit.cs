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
    /// <summary>
    /// This class implements a proof-of-concept Eagle plugin that demonstrates
    /// hosting toolkit commands and a user interface within an interpreter.
    /// </summary>
    [ObjectId("eb156d13-ddad-4a0a-88f3-d979553d22c1")]
    [PluginFlags(
        PluginFlags.Primary | PluginFlags.System |
        PluginFlags.Host | PluginFlags.Command |
        PluginFlags.Static | PluginFlags.MergeCommands |
        PluginFlags.UserInterface)]
    internal sealed class Toolkit : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data used to initialize the new instance.
        /// </param>
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
        /// <summary>
        /// This method returns descriptive information about this plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will contain the formatted information
        /// about this plugin.  Upon failure, it will contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = Utility.FormatPluginAbout(this, true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the set of conditional compilation symbols that
        /// were defined when this plugin was built.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will contain the list of conditional
        /// compilation symbols.  Upon failure, it will contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
