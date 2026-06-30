/*
 * TrustManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface manages the sets of trusted entities used by the
    /// default policies, including trusted file system paths, trusted URIs,
    /// trusted object types, and trusted file hashes.  It allows these sets
    /// to be queried and merged with additional trusted entities.
    /// </summary>
    [ObjectId("fe0cd230-f1fc-42e9-87b2-25e765a2eafd")]
    public interface ITrustManager
    {
        ///////////////////////////////////////////////////////////////////////
        // TRUST MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the list of trusted file system paths.
        /// </summary>
        StringList TrustedPaths { get; } /* WARNING: Trusted by the default [source] policy. */
        /// <summary>
        /// Gets the dictionary of trusted URIs.
        /// </summary>
        UriDictionary<object> TrustedUris { get; } /* WARNING: Trusted by the default [source] policy. */
        /// <summary>
        /// Gets the dictionary of trusted object types.
        /// </summary>
        ObjectDictionary TrustedTypes { get; } /* WARNING: Trusted by the default [object] policy. */
        /// <summary>
        /// Gets the list of trusted file hashes.
        /// </summary>
        StringList TrustedHashes { get; } /* WARNING: Trusted by the default [load] policy. */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Merges the specified trusted paths into the set of trusted paths.
        /// </summary>
        /// <param name="trustedPaths">
        /// The trusted paths to merge.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The trusted paths that were added by this operation.
        /// </returns>
        IEnumerable<string> MergeTrustedPaths(StringList trustedPaths);
        /// <summary>
        /// Merges the specified trusted URIs into the set of trusted URIs.
        /// </summary>
        /// <param name="trustedUris">
        /// The trusted URIs to merge.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The trusted URIs that were added by this operation.
        /// </returns>
        IEnumerable<string> MergeTrustedUris(UriDictionary<object> trustedUris);
        /// <summary>
        /// Merges the specified trusted types into the set of trusted types.
        /// </summary>
        /// <param name="trustedTypes">
        /// The trusted types to merge.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The trusted types that were added by this operation.
        /// </returns>
        IEnumerable<string> MergeTrustedTypes(ObjectDictionary trustedTypes);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the set of trusted hashes.
        /// </summary>
        /// <returns>
        /// The number of trusted hashes that were removed.
        /// </returns>
        int ClearTrustedHashes();
        /// <summary>
        /// Adds the specified hash to the set of trusted hashes.
        /// </summary>
        /// <param name="trustedHash">
        /// The trusted hash to add.  This parameter should not be null.
        /// </param>
        /// <returns>
        /// True if the hash was added; otherwise, false.
        /// </returns>
        bool AddTrustedHash(string trustedHash);
        /// <summary>
        /// Merges the specified trusted hashes into the set of trusted hashes.
        /// </summary>
        /// <param name="trustedHashes">
        /// The trusted hashes to merge.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The trusted hashes that were added by this operation.
        /// </returns>
        IEnumerable<string> MergeTrustedHashes(StringList trustedHashes);
    }
}
