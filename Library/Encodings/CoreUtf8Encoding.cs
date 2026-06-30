/*
 * CoreUtf8Encoding.cs --
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
    /// This class provides the UTF-8 encoding used internally by Eagle.  It
    /// extends <see cref="UTF8Encoding" /> so that it never emits a
    /// byte-order-mark and never throws exceptions for invalid bytes.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("45e991e7-df20-4597-a1a0-a94537a5b3c6")]
    public class CoreUtf8Encoding : UTF8Encoding
    {
        #region Public Constants
        /// <summary>
        /// A shared, pre-built instance of this encoding.
        /// </summary>
        public static readonly Encoding CoreUtf8 = new CoreUtf8Encoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The registered (IANA) name reported for this encoding.
        /// </summary>
        private static readonly string webName = "CoreUtf8";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        //
        // HACK: We never want the byte-order-mark and we never
        //       want to throw exceptions for invalid bytes.
        //
        /// <summary>
        /// Constructs an instance of this UTF-8 encoding that emits no
        /// byte-order-mark and does not throw on invalid bytes.
        /// </summary>
        public CoreUtf8Encoding()
            : base(false, false)
        {
            // do nothing.
        }
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
