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

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("fe0cd230-f1fc-42e9-87b2-25e765a2eafd")]
    public interface ITrustManager
    {
        ///////////////////////////////////////////////////////////////////////
        // TRUST MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        StringList TrustedPaths { get; } /* WARNING: Trusted by the default [source] policy. */
        UriDictionary<object> TrustedUris { get; } /* WARNING: Trusted by the default [source] policy. */
        ObjectDictionary TrustedTypes { get; } /* WARNING: Trusted by the default [object] policy. */
        StringList TrustedHashes { get; } /* WARNING: Trusted by the default [load] policy. */
    }
}
