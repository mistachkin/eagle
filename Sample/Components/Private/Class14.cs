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
    /// <summary>
    /// This class is a sample that demonstrates how to implement the web client
    /// callback interfaces, allowing a plugin to customize web client creation
    /// and to observe web transfer and web error events.
    /// </summary>
    [ObjectId("509108fc-537f-4a95-a126-c1105dcd6d70")]
    internal sealed class Class14
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        : ScriptMarshalByRefObject, INewWebClientCallback,
          IWebErrorCallback, IWebTransferCallback
#endif
    {
        #region WebClient Sample Class
        //
        // FIXME: Always change this GUID.
        //
        /// <summary>
        /// This class is a sample <see cref="WebClient" /> subclass returned by
        /// the new web client callback.
        /// </summary>
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
        /// <summary>
        /// The configured plugin instance associated with this object, used
        /// when reporting state to the plugin.  This object is not owned by
        /// this instance and is not disposed by it.
        /// </summary>
        private IPlugin plugin;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this sample web client callback class.
        /// </summary>
        /// <param name="plugin">
        /// The plugin instance to associate with this object.
        /// </param>
        public Class14(
            IPlugin plugin /* in */
            )
        {
            this.plugin = plugin;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method creates a new instance of the sample
        /// <see cref="WebClient" /> subclass, catching any exception that may
        /// occur.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created web client, or null if it could not be created.
        /// </returns>
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
        /// <summary>
        /// This method is invoked to create a new web client instance,
        /// optionally reporting state to the associated plugin first.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this callback is executing in.
        /// </param>
        /// <param name="argument">
        /// The extra argument associated with the request, if any.
        /// </param>
        /// <param name="clientData">
        /// The extra, callback-specific data supplied to this callback, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created web client, or null if it could not be created.
        /// </returns>
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
                        typeof(Class14).Name, TracePriority.Medium |
                            TracePriority.FromPlugin);
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

        #region IWebErrorCallback Members
        /// <summary>
        /// This method is invoked when a web error occurs.  This sample
        /// implementation traces the error details and indicates that the
        /// operation should continue.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this callback is executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra, callback-specific data supplied to this callback, if any.
        /// </param>
        /// <param name="uri">
        /// The URI associated with the web operation that failed.
        /// </param>
        /// <param name="webFlags">
        /// The flags describing the web operation.
        /// </param>
        /// <param name="retries">
        /// The number of retries that have been attempted so far.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, associated with the operation, if any.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of retries permitted, if any.
        /// </param>
        /// <param name="result">
        /// On input and output, the result object associated with the
        /// operation, if any.
        /// </param>
        /// <param name="errors">
        /// On input and output, the list of errors accumulated for the
        /// operation, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Continue" /> to indicate that the operation
        /// should continue.
        /// </returns>
        public ReturnCode WebError(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            WebFlags webFlags,
            int retries,
            int? timeout,
            int? maximumRetries,
            ref object result,
            ref ResultList errors
            )
        {
            bool isolated = Utility.IsCrossAppDomain(interpreter, plugin);

            Utility.DebugTrace(String.Format(
                "WebError: interpreter = {0}, webFlags = {1}, " +
                "clientData = {2}, isolated = {3}, uri = {4}, " +
                "retries = {5}, timeout = {6}, maximumRetries = {7}, " +
                "result = {8}, errors = {9}",
                Utility.FormatWrapOrNull(interpreter), webFlags,
                Utility.FormatWrapOrNull(clientData), isolated,
                Utility.FormatWrapOrNull(uri), retries,
                Utility.FormatWrapOrNull(timeout),
                Utility.FormatWrapOrNull(maximumRetries),
                Utility.FormatWrapOrNull(result),
                Utility.FormatWrapOrNull(errors)),
                typeof(Class14).Name, TracePriority.Medium |
                    TracePriority.FromPlugin);

            return ReturnCode.Continue; /* NOTE: Do nothing / keep going. */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWebTransferCallback Members
        /// <summary>
        /// This method is invoked when a web transfer occurs.  This sample
        /// implementation traces the transfer details and marks the associated
        /// web client data as having been transferred via the client.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this callback is executing in.
        /// </param>
        /// <param name="webFlags">
        /// The flags describing the web operation.
        /// </param>
        /// <param name="clientData">
        /// The extra, callback-specific data supplied to this callback, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode WebTransfer(
            Interpreter interpreter,
            WebFlags webFlags,
            IClientData clientData,
            ref Result error
            )
        {
            bool isolated = Utility.IsCrossAppDomain(interpreter, plugin);

            Utility.DebugTrace(String.Format(
                "WebTransfer: interpreter = {0}, webFlags = {1}, " +
                "clientData = {2}, isolated = {3}, error = {4}",
                Utility.FormatWrapOrNull(interpreter), webFlags,
                Utility.FormatWrapOrNull(clientData), isolated,
                Utility.FormatWrapOrNull(error)),
                typeof(Class14).Name, TracePriority.Medium |
                    TracePriority.FromPlugin);

            WebClientData webClientData = clientData as WebClientData;

            if (webClientData != null)
                webClientData.ViaClient = true;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an <see cref="InterpreterDisposedException" /> if
        /// this object instance has been disposed and the interpreter is
        /// configured to throw on access to disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Class14));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this object instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
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
        /// <summary>
        /// This method releases all resources used by this object instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this object instance, releasing any resources that were
        /// not already released by an explicit call to the
        /// <see cref="Dispose()" /> method.
        /// </summary>
        ~Class14()
        {
            Dispose(false);
        }
        #endregion
    }
}
