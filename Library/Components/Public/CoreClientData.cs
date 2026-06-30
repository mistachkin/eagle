/*
 * CoreClientData.cs --
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
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class provides the core implementation of <see cref="ICoreClientData" />,
    /// wrapping an arbitrary client data object and exposing helpers for
    /// inspecting the runtime type of that object.
    /// </summary>
    [ObjectId("c699153d-e96e-46f0-9d94-09b6292e9332")]
    public class CoreClientData : ClientData, ICoreClientData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a core client data instance that wraps no data.
        /// </summary>
        public CoreClientData()
            : base(null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a core client data instance that wraps the specified
        /// data.  The wrapped data is not treated as read-only.
        /// </summary>
        /// <param name="data">
        /// The data to wrap.  This parameter may be null.
        /// </param>
        public CoreClientData(
            object data /* in */
            )
            : base(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a core client data instance that wraps the specified
        /// data, optionally treating that data as read-only.
        /// </summary>
        /// <param name="data">
        /// The data to wrap.  This parameter may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the wrapped data should be treated as read-only.
        /// </param>
        public CoreClientData(
            object data,  /* in */
            bool readOnly /* in */
            )
            : base(data, readOnly)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICoreClientData Members
        /// <summary>
        /// This method gets the runtime type of the wrapped client data, if
        /// any.
        /// </summary>
        /// <returns>
        /// The runtime type of the wrapped data, or null if there is no
        /// wrapped data.
        /// </returns>
        public virtual Type MaybeGetDataType()
        {
            object data = base.DataNoThrow;

            if (data == null)
                return null;

            return AppDomainOps.MaybeGetType(data);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the runtime type of the wrapped client
        /// data, if any.
        /// </summary>
        /// <returns>
        /// The name of the runtime type of the wrapped data, or null if there
        /// is no wrapped data.
        /// </returns>
        public virtual string GetDataTypeName()
        {
            object data = base.DataNoThrow;

            if (data == null)
                return null;

            return FormatOps.RawTypeName(data);
        }
        #endregion
    }
}
