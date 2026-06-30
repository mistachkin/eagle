/*
 * PluginClientData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a container for client data associated with a plugin,
    /// recording whether the plugin is part of the core library and whether its
    /// built-in commands should be used.
    /// </summary>
    [ObjectId("8767e99b-f26f-4e32-8c1c-a6d70e7f13f5")]
    internal sealed class PluginClientData : ClientData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with the specified plugin
        /// settings and opaque data payload.
        /// </summary>
        /// <param name="isCore">
        /// Non-zero if the associated plugin is part of the core library.
        /// </param>
        /// <param name="useBuiltIn">
        /// Non-zero if the built-in commands of the associated plugin should be
        /// used.
        /// </param>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        public PluginClientData(
            bool isCore,     /* in */
            bool useBuiltIn, /* in */
            object data      /* in */
            )
            : base(data)
        {
            this.isCore = isCore;
            this.useBuiltIn = useBuiltIn;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Stores a value indicating whether the associated plugin is part of
        /// the core library.
        /// </summary>
        private bool isCore;
        /// <summary>
        /// Gets a value indicating whether the associated plugin is part of the
        /// core library.
        /// </summary>
        public bool IsCore
        {
            get { return isCore; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the built-in commands of the
        /// associated plugin should be used.
        /// </summary>
        private bool useBuiltIn;
        /// <summary>
        /// Gets a value indicating whether the built-in commands of the
        /// associated plugin should be used.
        /// </summary>
        public bool UseBuiltIn
        {
            get { return useBuiltIn; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method determines whether the built-in commands should be used,
        /// based on the supplied client data.  This is true only when the client
        /// data is an instance of this class that represents a core plugin
        /// configured to use its built-in commands.
        /// </summary>
        /// <param name="clientData">
        /// The client data to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the built-in commands should be used; otherwise, false.
        /// </returns>
        public static bool ShouldUseBuiltIns(
            IClientData clientData /* in */
            )
        {
            PluginClientData pluginClientData =
                clientData as PluginClientData;

            if (pluginClientData == null)
                return false;

            if (!pluginClientData.IsCore)
                return false;

            if (!pluginClientData.UseBuiltIn)
                return false;

            return true;
        }
        #endregion
    }
}
