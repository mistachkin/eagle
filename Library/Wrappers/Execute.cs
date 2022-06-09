/*
 * Execute.cs --
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

namespace Eagle._Wrappers
{
    [ObjectId("c9d02a0d-a5dd-4c8f-9367-0cbcc738412f")]
    internal sealed class _Execute : Default, IExecute
    {
        #region Public Constructors
        public _Execute(
            long token,
            IExecute execute
            )
            : base(token)
        {
            this.execute = execute;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IExecute execute;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (execute != null)
                return execute.Execute(
                    interpreter, clientData, arguments, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return execute; }
        }
        #endregion
    }
}
