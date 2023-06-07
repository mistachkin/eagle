/*
 * Class14.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Net;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if TEST
using _Test = Eagle._Tests.Default;
#endif

namespace Sample
{
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("509108fc-537f-4a95-a126-c1105dcd6d70")]
    internal sealed class Class14
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        : ScriptMarshalByRefObject, INewWebClientCallback, IWebTransferCallback
#endif
    {
        #region WebClient Sample Class
        //
        // FIXME: Always change this GUID.
        //
        [ObjectId("f7837701-6dcd-4187-bd34-953a941f51bc")]
        private sealed class Class14WebClient : WebClient
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the configured plugin instance.  It will be used to
        //       query for an embedded resource string containing the sample
        //       package script.
        //
        private IPlugin plugin;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Class14(
            IPlugin plugin /* in */
            )
        {
            this.plugin = plugin;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static WebClient NewClass14WebClient(
            ref Result error
            )
        {
            try
            {
                return new Class14WebClient();
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INewWebClientCallback Members
        public WebClient NewWebClient(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            bool isolated = Utility.IsCrossAppDomain(interpreter, plugin);

            if (plugin != null)
            {
                ReturnCode code;
                object response = null;

                code = plugin.Execute(
                    interpreter, clientData, new StringList("addState",
                    isolated.ToString()), ref response, ref error);

                if (code != ReturnCode.Ok)
                {
                    Utility.DebugTrace(String.Format(
                        "NewWebClient: code = {0}, error = {1}", code,
                        Utility.FormatWrapOrNull(error)),
                        typeof(Class14).Name, TracePriority.Medium);
                }
            }

            if (isolated)
            {
                return NewClass14WebClient(ref error);
            }
            else
            {
#if TEST
                return _Test.TestScriptNewWebClientCallback(
                    interpreter, argument, clientData, ref error);
#else
                return NewClass14WebClient(ref error);
#endif
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWebTransferCallback Members
        public ReturnCode WebTransfer(
            Interpreter interpreter,
            WebFlags flags,
            IClientData clientData,
            ref Result error
            )
        {
            bool isolated = Utility.IsCrossAppDomain(interpreter, plugin);

            Utility.DebugTrace(String.Format(
                "WebTransfer: interpreter = {0}, flags = {1}, " +
                "clientData = {2}, isolated = {3}, error = {4}",
                Utility.FormatWrapOrNull(interpreter), flags,
                Utility.FormatWrapOrNull(clientData), isolated,
                Utility.FormatWrapOrNull(error)),
                typeof(Class14).Name, TracePriority.Medium);

            WebClientData webClientData = clientData as WebClientData;

            if (webClientData != null)
                webClientData.ViaClient = true;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Class14));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    plugin = null; /* NOT OWNED: DO NOT DISPOSE */
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Class14()
        {
            Dispose(false);
        }
        #endregion
    }
}
