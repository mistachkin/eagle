/*
 * IdentityEncoding.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Text;
using Eagle._Attributes;

namespace Eagle._Encodings
{
    /// <summary>
    /// This class represents the identity encoding, a one-to-one mapping
    /// between bytes and characters that performs no transformation.  It
    /// extends <see cref="OneByteEncoding" /> and differs only in its
    /// registered (IANA) name.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("439ec704-df12-4acf-86c9-7f0187a99744")]
    public sealed class IdentityEncoding : OneByteEncoding
    {
        #region Public Constants
        /// <summary>
        /// A shared, pre-built instance of this encoding.
        /// </summary>
        public static readonly Encoding Identity = new IdentityEncoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The registered (IANA) name reported for this encoding.
        /// </summary>
        internal static new readonly string webName = "Identity";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Text.Encoding Overrides
        /// <summary>
        /// Gets the registered (IANA) name for this encoding.
        /// </summary>
        public override string WebName
        {
            get { return webName; }
        }
        #endregion
    }
}
