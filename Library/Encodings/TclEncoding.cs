/*
 * TclEncoding.cs --
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
    /// This class represents the UTF-8 encoding used for Tcl interoperability.
    /// It extends <see cref="CoreUtf8Encoding" /> and always reports an empty
    /// preamble so that no byte-order-mark is emitted.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("737f635e-db2a-44d2-848f-42ed3435f2ec")]
    public sealed class TclEncoding : CoreUtf8Encoding
    {
        #region Public Constants
        /// <summary>
        /// A shared, pre-built instance of this encoding.
        /// </summary>
        public static readonly Encoding Tcl = new TclEncoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The registered (IANA) name reported for this encoding.
        /// </summary>
        internal static readonly string webName = "Tcl";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A shared, empty byte array returned as the preamble for this
        /// encoding.
        /// </summary>
        private static readonly byte[] emptyByteArray = new byte[0];
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Text.Encoding Overrides
        /// <summary>
        /// Gets the preamble (byte-order-mark) for this encoding.
        /// </summary>
        /// <returns>
        /// An empty byte array, since this encoding never emits a
        /// byte-order-mark.
        /// </returns>
        public override byte[] GetPreamble()
        {
            return emptyByteArray;
        }

        ///////////////////////////////////////////////////////////////////////

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
