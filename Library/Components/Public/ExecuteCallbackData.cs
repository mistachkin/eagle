/*
 * ExecuteCallbackData.cs --
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
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class holds the metadata describing a named execute callback
    /// exposed to an interpreter, including the callback delegate, its client
    /// data, and the token used to identify it.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0396cf4b-a233-487f-a86e-714f10c210fa")]
    public class ExecuteCallbackData : IExecuteCallbackData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an execute callback data instance wrapping the specified
        /// name, callback, client data, and token.
        /// </summary>
        /// <param name="name">
        /// The name of this execute callback.
        /// </param>
        /// <param name="callback">
        /// The execute callback delegate to be invoked.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this execute callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this execute callback within the
        /// interpreter.
        /// </param>
        public ExecuteCallbackData(
            string name,
            ExecuteCallback callback,
            IClientData clientData,
            long token
            )
        {
            this.name = name;
            this.callback = callback;
            this.clientData = clientData;
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this execute callback data.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this execute callback data.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this execute callback data.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this execute callback
        /// data.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// Stores the execute callback delegate wrapped by this execute
        /// callback data.
        /// </summary>
        private ExecuteCallback callback;
        /// <summary>
        /// Gets or sets the execute callback delegate wrapped by this execute
        /// callback data.
        /// </summary>
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this execute callback data within
        /// the interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this execute callback data
        /// within the interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string representation of this execute
        /// callback data using its name only.
        /// </summary>
        /// <returns>
        /// The name of this execute callback data, or an empty string when it
        /// has no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
