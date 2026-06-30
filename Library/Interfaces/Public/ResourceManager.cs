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
    /// <summary>
    /// This interface defines the management surface for retrieving localized
    /// resource strings, optionally from a specific plugin and/or for a
    /// specific culture.
    /// </summary>
    [ObjectId("09f9d4ac-6a76-4c38-91a6-ce81c9d5dfd4")]
    public interface IResourceManager
    {
        ///////////////////////////////////////////////////////////////////////
        // RESOURCE STRING HANDLING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the resource string with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the resource string to retrieve.
        /// </param>
        /// <returns>
        /// The resource string with the specified name, or null if it could not
        /// be found.
        /// </returns>
        string GetString(string name);

        /// <summary>
        /// Gets the resource string with the specified name from the specified
        /// plugin.
        /// </summary>
        /// <param name="plugin">
        /// The plugin from which to retrieve the resource string, or null to
        /// use the default source.
        /// </param>
        /// <param name="name">
        /// The name of the resource string to retrieve.
        /// </param>
        /// <returns>
        /// The resource string with the specified name, or null if it could not
        /// be found.
        /// </returns>
        string GetString(
            IPlugin plugin,
            string name
            );

        /// <summary>
        /// Gets the resource string with the specified name from the specified
        /// plugin.
        /// </summary>
        /// <param name="plugin">
        /// The plugin from which to retrieve the resource string, or null to
        /// use the default source.
        /// </param>
        /// <param name="name">
        /// The name of the resource string to retrieve.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The resource string with the specified name, or null if it could not
        /// be found.
        /// </returns>
        string GetString(
            IPlugin plugin,
            string name,
            ref Result error
            );

        /// <summary>
        /// Gets the resource string with the specified name from the specified
        /// plugin, for the specified culture.
        /// </summary>
        /// <param name="plugin">
        /// The plugin from which to retrieve the resource string, or null to
        /// use the default source.
        /// </param>
        /// <param name="name">
        /// The name of the resource string to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which to retrieve the resource string, or null to
        /// use the default culture.
        /// </param>
        /// <returns>
        /// The resource string with the specified name, or null if it could not
        /// be found.
        /// </returns>
        string GetString(
            IPlugin plugin,
            string name,
            CultureInfo cultureInfo
            );

        /// <summary>
        /// Gets the resource string with the specified name from the specified
        /// plugin, for the specified culture.
        /// </summary>
        /// <param name="plugin">
        /// The plugin from which to retrieve the resource string, or null to
        /// use the default source.
        /// </param>
        /// <param name="name">
        /// The name of the resource string to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which to retrieve the resource string, or null to
        /// use the default culture.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The resource string with the specified name, or null if it could not
        /// be found.
        /// </returns>
        string GetString(
            IPlugin plugin,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            );
    }
}
