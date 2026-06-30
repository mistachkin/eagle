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
    /// <summary>
    /// This class represents the client data used to carry the inputs and
    /// outputs of an object marshalling operation, including the associated
    /// options, marshalling flags, return code, and result.
    /// </summary>
    [ObjectId("bde54105-be0f-493e-b8d0-99e796dba4cd")]
    internal sealed class MarshalClientData : ClientData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class wrapping the specified data.
        /// </summary>
        /// <param name="data">
        /// The data to wrap.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class wrapping the specified data
        /// along with the options, marshalling flags, return code, and result
        /// associated with a marshalling operation.
        /// </summary>
        /// <param name="data">
        /// The data to wrap.  This parameter may be null.
        /// </param>
        /// <param name="options">
        /// The options associated with the marshalling operation.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags that control the behavior of the marshalling operation.
        /// </param>
        /// <param name="returnCode">
        /// The return code produced by the marshalling operation.
        /// </param>
        /// <param name="result">
        /// The result produced by the marshalling operation.
        /// </param>
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
        /// <summary>
        /// The options associated with the marshalling operation.
        /// </summary>
        private OptionDictionary options;
        /// <summary>
        /// Gets or sets the options associated with the marshalling operation.
        /// </summary>
        public OptionDictionary Options
        {
            get { return options; }
            set { options = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control the behavior of the marshalling operation.
        /// </summary>
        private MarshalFlags marshalFlags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of the marshalling
        /// operation.
        /// </summary>
        public MarshalFlags MarshalFlags
        {
            get { return marshalFlags; }
            set { marshalFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The return code produced by the marshalling operation.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// Gets or sets the return code produced by the marshalling operation.
        /// </summary>
        public ReturnCode ReturnCode
        {
            get { return returnCode; }
            set { returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The result produced by the marshalling operation.
        /// </summary>
        private Result result;
        /// <summary>
        /// Gets or sets the result produced by the marshalling operation.
        /// </summary>
        public Result Result
        {
            get { return result; }
            set { result = value; }
        }
        #endregion
    }
}
