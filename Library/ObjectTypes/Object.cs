/*
 * Object.cs --
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
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._ObjectTypes
{
    [ObjectId("53afee69-2fd1-4130-b477-cb9a0db0b39b")]
    internal sealed class Object : Default
    {
        #region Public Constructors
        public Object(
            IObjectTypeData objectTypeData
            )
            : base(objectTypeData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectType Members
        public override ReturnCode SetFromAny(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode UpdateString(
            Interpreter interpreter,
            ref string text,
            IntPtr value,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Duplicate(
            Interpreter interpreter,
            IntPtr oldValue,
            ref IntPtr newValue,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Shimmer(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            //
            // TODO: Implement me.
            //
            return ReturnCode.Ok;
        }
        #endregion
    }
}
