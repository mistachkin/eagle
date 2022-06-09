/*
 * Class10.cs --
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
using TclSample.Forms;
using _Hosts = Eagle._Hosts;

namespace TclSample
{
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("a71265aa-7232-49ec-9b14-5fd0e8372155")]
    public class Class10 : _Hosts.Wrapper, IDisposable
    {
        #region Private Data
        /// <summary>
        /// This object is used to synchronize access to the private data.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The parent interpreter instance associated with this host.
        /// </summary>
        private Interpreter interpreter;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The form associated with this host.  This form is used to display
        /// output received from the associated interpreter.
        /// </summary>
        private HostForm hostForm;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Class10(
            IHostData hostData,
            IHost baseHost,
            bool baseHostOwned
            )
            : base(hostData, baseHost, baseHostOwned)
        {
            SetInterpreter((hostData != null) ? hostData.Interpreter : null);

            ///////////////////////////////////////////////////////////////////

            NewHostForm();
            ShowHostForm();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        protected virtual void SetInterpreter(
            Interpreter interpreter
            )
        {
            lock (syncRoot)
            {
                this.interpreter = interpreter;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool NewHostForm()
        {
            bool result = false;

            lock (syncRoot)
            {
                if (this.hostForm == null)
                {
                    this.hostForm = new HostForm(this.interpreter);
                    result = true;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool ShowHostForm()
        {
            bool result = false;
            HostForm hostForm;

            lock (syncRoot)
            {
                hostForm = this.hostForm;
            }

            if (hostForm != null)
            {
                hostForm.Show();
                result = true;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool SafeWriteHostForm(
            string value,
            bool newLine
            )
        {
            bool result = false;
            HostForm hostForm;

            lock (syncRoot)
            {
                hostForm = this.hostForm;
            }

            if (hostForm != null)
                result = hostForm.SafeAppendLog(value, newLine);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool SafeCloseHostForm()
        {
            bool result = false;
            HostForm hostForm;

            lock (syncRoot)
            {
                hostForm = this.hostForm;
            }

            if (hostForm != null)
            {
                hostForm.SafeClose();
                result = true;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        //
        // NOTE: This method is used by the HostForm class to notify this
        //       interpreter host that it is being disposed.
        //
        public virtual bool ResetHostForm()
        {
            bool result = false;

            lock (syncRoot)
            {
                if (hostForm != null)
                {
                    hostForm = null;
                    result = true;
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public override bool WriteDebugLine( /* NOTE: For DebugOps.Write. */
            string value
            )
        {
            bool result = false;

            try
            {
                result = SafeWriteHostForm(value, true);
            }
            finally
            {
                if (!result)
                    result = base.WriteDebugLine(value);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool WriteErrorLine( /* NOTE: For DebugOps.Complain. */
            string value
            )
        {
            bool result = false;

            try
            {
                result = SafeWriteHostForm(value, true);
            }
            finally
            {
                if (!result)
                    result = base.WriteErrorLine(value);
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(Class10));
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

                    SafeCloseHostForm();
                    SetInterpreter(null); /* NOT OWNED */
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
