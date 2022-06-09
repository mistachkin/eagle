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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("737f635e-db2a-44d2-848f-42ed3435f2ec")]
    public sealed class TclEncoding : CoreUtf8Encoding
    {
        #region Public Constants
        public static readonly Encoding Tcl = new TclEncoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        internal static readonly string webName = "Tcl";

        ///////////////////////////////////////////////////////////////////////

        private static readonly byte[] emptyByteArray = new byte[0];
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Text.Encoding Overrides
        public override byte[] GetPreamble()
        {
            return emptyByteArray;
        }

        ///////////////////////////////////////////////////////////////////////

        public override string WebName
        {
            get { return webName; }
        }
        #endregion
    }
}
