/*
 * ProvideEntropyBridge.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("17169446-cb5f-4a14-b62d-58ec889c16d5")]
    public sealed class ProvideEntropyBridge :
        ScriptMarshalByRefObject, IProvideEntropy
    {
        #region Private Data
        private IProvideEntropy provideEntropy;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ProvideEntropyBridge(
            IProvideEntropy provideEntropy
            )
        {
            this.provideEntropy = provideEntropy;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ProvideEntropyBridge Create(
            IProvideEntropy provideEntropy,
            ref Result error
            )
        {
            if (provideEntropy == null)
            {
                error = "invalid entropy provider";
                return null;
            }

            return new ProvideEntropyBridge(provideEntropy);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProvideEntropy Members
        public void GetBytes(byte[] data)
        {
            if (provideEntropy == null)
            {
                throw new InvalidOperationException(
                    "invalid entropy provider");
            }

            /* NO RESULT */
            provideEntropy.GetBytes(data); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        public void GetNonZeroBytes(byte[] data)
        {
            if (provideEntropy == null)
            {
                throw new InvalidOperationException(
                    "invalid entropy provider");
            }

            /* NO RESULT */
            provideEntropy.GetNonZeroBytes(data); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: The "bytes" parameter must be "ref"; otherwise,
        //         cross-domain marshalling does not work right.
        //
        public void GetBytes(ref byte[] data)
        {
            if (provideEntropy == null)
            {
                throw new InvalidOperationException(
                    "invalid entropy provider");
            }

            /* NO RESULT */
            provideEntropy.GetBytes(ref data);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: The "bytes" parameter must be "ref"; otherwise,
        //         cross-domain marshalling does not work right.
        //
        public void GetNonZeroBytes(ref byte[] data)
        {
            if (provideEntropy == null)
            {
                throw new InvalidOperationException(
                    "invalid entropy provider");
            }

            /* NO RESULT */
            provideEntropy.GetNonZeroBytes(ref data);
        }
        #endregion
    }
}
