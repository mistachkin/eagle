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
    [ObjectId("8767e99b-f26f-4e32-8c1c-a6d70e7f13f5")]
    internal sealed class PluginClientData : ClientData
    {
        #region Public Constructors
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
        private bool isCore;
        public bool IsCore
        {
            get { return isCore; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useBuiltIn;
        public bool UseBuiltIn
        {
            get { return useBuiltIn; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
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
