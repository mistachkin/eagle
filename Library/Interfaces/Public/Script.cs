/*
 * Script.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a script, i.e. a unit of code that can be
    /// evaluated by the Eagle engine.  It composes the script data
    /// (<see cref="IScriptData" />), its script flags
    /// (<see cref="IHaveScriptFlags" />), its source location
    /// (<see cref="IScriptLocation" />), the collection of its parts
    /// (<see cref="ICollection" />), and its identity
    /// (<see cref="IIdentifier" />), adding operations to query and finalize
    /// it.
    /// </summary>
    [ObjectId("15da79ef-3b2a-42fc-97bc-da5b0384e23e")]
    public interface IScript :
            IScriptData, IHaveScriptFlags, IScriptLocation,
            ICollection, IIdentifier
    {
        /// <summary>
        /// This method determines whether this script should be treated as a
        /// file, returning the associated file name and file bytes when it
        /// should.
        /// </summary>
        /// <param name="fileName">
        /// Upon return, this will contain the associated file name when this
        /// script should be treated as a file; otherwise, null.
        /// </param>
        /// <param name="fileBytes">
        /// Upon return, this will contain the associated file bytes when this
        /// script should be treated as a file; otherwise, null.
        /// </param>
        /// <returns>
        /// True if this script should be treated as a file; otherwise, false.
        /// </returns>
        bool ShouldTreatAsFile(
            out string fileName, out byte[] fileBytes
        );

#if XML
        /// <summary>
        /// This method gets the block type of this script as a string.
        /// </summary>
        /// <returns>
        /// The block type of this script, as a string.
        /// </returns>
        string GetBlockTypeString();
#endif

        /// <summary>
        /// This method gets the extra attributes associated with this script,
        /// if this script is not immutable.
        /// </summary>
        /// <returns>
        /// The dictionary of extra attributes, or null if this script is
        /// immutable.
        /// </returns>
        ObjectDictionary MaybeGetExtra();

        /// <summary>
        /// This method gets the extra attributes associated with this script.
        /// </summary>
        /// <returns>
        /// The dictionary of extra attributes associated with this script.
        /// </returns>
        ObjectDictionary GetExtra();

        /// <summary>
        /// This method marks this script as immutable, preventing further
        /// changes to it.
        /// </summary>
        void MakeImmutable();
    }
}
