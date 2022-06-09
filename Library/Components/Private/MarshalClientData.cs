/*
 * MarshalClientData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("bde54105-be0f-493e-b8d0-99e796dba4cd")]
    internal sealed class MarshalClientData : ClientData
    {
        #region Private Constructors
        private MarshalClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public MarshalClientData(
            object data,
            OptionDictionary options,
            MarshalFlags marshalFlags,
            ReturnCode returnCode,
            Result result
            )
            : this(data)
        {
            this.options = options;
            this.marshalFlags = marshalFlags;
            this.returnCode = returnCode;
            this.result = result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private OptionDictionary options;
        public OptionDictionary Options
        {
            get { return options; }
            set { options = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private MarshalFlags marshalFlags;
        public MarshalFlags MarshalFlags
        {
            get { return marshalFlags; }
            set { marshalFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode returnCode;
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Result result;
        public Result Result
        {
            get { return result; }
            set { result = value; }
        }
        #endregion
    }
}
