/*
 * ScriptData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if XML
using System;
#endif

using System.Collections;

#if CAS_POLICY
using System.Security.Cryptography;
using System.Security.Policy;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface describes a unit of script data, including its type,
    /// constituent parts, and text, together with optional metadata such as
    /// XML block information, digital signature data, and code access
    /// security evidence.
    /// </summary>
    [ObjectId("16affcc8-1ef3-48cf-ae63-8ecd49271d95")]
    public interface IScriptData
    {
        /// <summary>
        /// Gets the type name associated with this script data.
        /// </summary>
        string Type { get; }
        /// <summary>
        /// Gets the list of constituent parts that make up this script data.
        /// </summary>
        IList Parts { get; }
        /// <summary>
        /// Gets the text of this script data.
        /// </summary>
        string Text { get; }

#if XML
        /// <summary>
        /// Gets the kind of XML block this script data was loaded from.
        /// </summary>
        XmlBlockType BlockType { get; }
        /// <summary>
        /// Gets the time stamp associated with this script data.
        /// </summary>
        DateTime TimeStamp { get; }
        /// <summary>
        /// Gets the public key token associated with the digital signature of
        /// this script data, if any.
        /// </summary>
        string PublicKeyToken { get; }
        /// <summary>
        /// Gets the digital signature of this script data, if any, as an array
        /// of bytes.
        /// </summary>
        byte[] Signature { get; }
#endif

#if CAS_POLICY
        /// <summary>
        /// Gets the code access security evidence associated with this script
        /// data, if any.
        /// </summary>
        Evidence Evidence { get; }
        /// <summary>
        /// Gets the hash value computed for this script data, if any, as an
        /// array of bytes.
        /// </summary>
        byte[] HashValue { get; }
        /// <summary>
        /// Gets the hash algorithm used to compute the hash value for this script
        /// data, if any.
        /// </summary>
        HashAlgorithm HashAlgorithm { get; }
#endif

        /// <summary>
        /// Gets the bundle data associated with this script data, if any.
        /// </summary>
        IBundleData BundleData { get; }
    }
}
