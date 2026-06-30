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
    /// <summary>
    /// This class wraps an entropy provider so that it can be used safely
    /// across application domain boundaries.  It derives from
    /// <see cref="ScriptMarshalByRefObject" /> and forwards each
    /// <see cref="IProvideEntropy" /> request to the wrapped provider.
    /// </summary>
    [ObjectId("17169446-cb5f-4a14-b62d-58ec889c16d5")]
    public sealed class ProvideEntropyBridge :
        ScriptMarshalByRefObject, IProvideEntropy
    {
        #region Private Data
        /// <summary>
        /// The underlying entropy provider that requests are forwarded to.
        /// </summary>
        private IProvideEntropy provideEntropy;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified entropy provider.
        /// </summary>
        /// <param name="provideEntropy">
        /// The entropy provider to wrap.
        /// </param>
        private ProvideEntropyBridge(
            IProvideEntropy provideEntropy
            )
        {
            this.provideEntropy = provideEntropy;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new bridge that wraps the specified entropy
        /// provider.
        /// </summary>
        /// <param name="provideEntropy">
        /// The entropy provider to wrap.  This parameter may not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
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
        /// <summary>
        /// This method fills the specified array with entropy bytes by
        /// forwarding the request to the wrapped entropy provider.
        /// </summary>
        /// <param name="data">
        /// The array to fill with entropy bytes.
        /// </param>
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

        /// <summary>
        /// This method fills the specified array with non-zero entropy bytes by
        /// forwarding the request to the wrapped entropy provider.
        /// </summary>
        /// <param name="data">
        /// The array to fill with non-zero entropy bytes.
        /// </param>
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
        /// <summary>
        /// This method fills the specified array with entropy bytes by
        /// forwarding the request to the wrapped entropy provider.  The array is
        /// passed by reference to support cross-domain marshalling.
        /// </summary>
        /// <param name="data">
        /// The array to fill with entropy bytes.
        /// </param>
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
        /// <summary>
        /// This method fills the specified array with non-zero entropy bytes by
        /// forwarding the request to the wrapped entropy provider.  The array is
        /// passed by reference to support cross-domain marshalling.
        /// </summary>
        /// <param name="data">
        /// The array to fill with non-zero entropy bytes.
        /// </param>
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
