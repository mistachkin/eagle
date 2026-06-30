/*
 * RuntimeOptionManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the set of
    /// runtime options.  These options are reserved for use by the host
    /// application and/or scripts; the core library itself does not make use of
    /// them, except that the core script library may use any whose name is
    /// prefixed with <c>eagle</c>.
    /// </summary>
    //
    // NOTE: The configured "runtime options" are RESERVED for use by the host
    //       application and/or scripts.  The core library itself does not make
    //       use of them; however, the core script library is allowed to create
    //       and/or make use of any that have a name prefixed with "eagle".
    //
    [ObjectId("09f5da81-c862-4b38-8a18-4a0c3668b737")]
    public interface IRuntimeOptionManager
    {
        ///////////////////////////////////////////////////////////////////////
        // RUNTIME OPTION DATA
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the dictionary of runtime options.
        /// </summary>
        ClientDataDictionary RuntimeOptions { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // RUNTIME OPTION MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a runtime option with the specified
        /// name is present.
        /// </summary>
        /// <param name="name">
        /// The name of the runtime option to check for.
        /// </param>
        /// <returns>
        /// True if the runtime option is present; otherwise, false.
        /// </returns>
        bool HasRuntimeOption(string name);

        /// <summary>
        /// This method removes all runtime options.
        /// </summary>
        /// <returns>
        /// True if the runtime options were cleared; otherwise, false.
        /// </returns>
        bool ClearRuntimeOptions();

        /// <summary>
        /// This method adds a runtime option with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the runtime option to add.
        /// </param>
        /// <returns>
        /// True if the runtime option was added; otherwise, false.
        /// </returns>
        bool AddRuntimeOption(string name);

        /// <summary>
        /// This method removes the runtime option with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the runtime option to remove.
        /// </param>
        /// <returns>
        /// True if the runtime option was removed; otherwise, false.
        /// </returns>
        bool RemoveRuntimeOption(string name);

        /// <summary>
        /// This method changes the runtime options based on the leading
        /// operator character of the specified name: a leading <c>+</c> adds
        /// the named option, a leading <c>-</c> removes it, a leading <c>=</c>
        /// clears all options and then adds the named option, and a bare name
        /// adds the named option.
        /// </summary>
        /// <param name="name">
        /// The name of the runtime option to change, optionally prefixed with
        /// an operator character.
        /// </param>
        /// <returns>
        /// True if the runtime option was changed; otherwise, false.
        /// </returns>
        bool ChangeRuntimeOption(string name);
    }
}
