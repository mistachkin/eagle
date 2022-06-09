/*
 * ResourceManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("09f9d4ac-6a76-4c38-91a6-ce81c9d5dfd4")]
    public interface IResourceManager
    {
        ///////////////////////////////////////////////////////////////////////
        // RESOURCE STRING HANDLING
        ///////////////////////////////////////////////////////////////////////

        string GetString(string name);

        string GetString(
            IPlugin plugin,
            string name
            );

        string GetString(
            IPlugin plugin,
            string name,
            ref Result error
            );

        string GetString(
            IPlugin plugin,
            string name,
            CultureInfo cultureInfo
            );

        string GetString(
            IPlugin plugin,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            );
    }
}
