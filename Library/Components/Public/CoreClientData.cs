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
    [ObjectId("c699153d-e96e-46f0-9d94-09b6292e9332")]
    public class CoreClientData : ClientData, ICoreClientData
    {
        #region Public Constructors
        public CoreClientData()
            : base(null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public CoreClientData(
            object data /* in */
            )
            : base(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        public virtual Type MaybeGetDataType()
        {
            object data = base.DataNoThrow;

            if (data == null)
                return null;

            return AppDomainOps.MaybeGetType(data);
        }

        ///////////////////////////////////////////////////////////////////////

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
