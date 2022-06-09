/*
 * Custom.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !CONSOLE
#error "This file cannot be compiled or used properly with console support disabled."
#endif

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

namespace Eagle._Hosts
{
    [ObjectId("fbce00e2-9408-42bf-ac13-06865c5928ad")]
    internal sealed class Custom : Eagle._Hosts.Console, IDisposable
    {
        #region Public Constructors
        public Custom(
            IHostData hostData
            )
            : base(hostData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public override IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Custom(new HostData(
                Name, Group, Description, ClientData, typeof(Custom).Name,
                interpreter, ResourceManager, Profile, HostCreateFlags));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    SafeGetInterpreter(), null))
            {
                throw new InterpreterDisposedException(typeof(Custom));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Custom()
        {
            Dispose(false);
        }
        #endregion
    }
}
