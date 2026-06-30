/*
 * BundleData.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the contract for the metadata describing a script
    /// bundle.  It exposes read-only properties identifying the bundle (e.g.
    /// its language, vendor, path, and content) along with the isolation and
    /// security settings that govern its use, and provides a method to make the
    /// metadata immutable.
    /// </summary>
    [ObjectId("553d7eb3-28f8-42dc-8bd6-96e7fc678aba")]
    public interface IBundleData : IHaveInterpreter
    {
        /// <summary>
        /// Gets the name of the language associated with the bundle.
        /// </summary>
        string Language { get; }
        /// <summary>
        /// Gets the sequence number associated with the bundle.
        /// </summary>
        long Sequence { get; }
        /// <summary>
        /// Gets the name of the vendor associated with the bundle.
        /// </summary>
        string Vendor { get; }
        /// <summary>
        /// Gets the path associated with the bundle.
        /// </summary>
        string Path { get; }
        /// <summary>
        /// Gets the full name of the bundle.
        /// </summary>
        string FullName { get; }
        /// <summary>
        /// Gets the name of the hash algorithm associated with the bundle.
        /// </summary>
        string HashAlgorithmName { get; }
        /// <summary>
        /// Gets the raw bytes that make up the bundle file.
        /// </summary>
        byte[] FileBytes { get; }
        /// <summary>
        /// Gets the isolation level that governs how the bundle is evaluated.
        /// </summary>
        IsolationLevel IsolationLevel { get; }
        /// <summary>
        /// Gets the security level that governs how the bundle is evaluated.
        /// </summary>
        SecurityLevel SecurityLevel { get; }
        /// <summary>
        /// Gets the script security flags that govern how the bundle is
        /// evaluated.
        /// </summary>
        ScriptSecurityFlags SecurityFlags { get; }
        /// <summary>
        /// Gets the rule set that governs how the bundle is evaluated.
        /// </summary>
        IRuleSet RuleSet { get; }
        /// <summary>
        /// Makes this bundle metadata immutable, preventing any further
        /// modification.
        /// </summary>
        void MakeImmutable();
    }
}
