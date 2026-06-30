/*
 * Plugin.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class wraps an <see cref="IPlugin" /> instance, forwarding all
    /// member access to the wrapped target while gracefully handling the case
    /// where no target has been set (by returning default values or an error
    /// result).
    /// </summary>
    [ObjectId("ec624d41-7b2f-4fa6-81d0-2eaf70a7f29b")]
    internal sealed class Plugin : Default, IPlugin
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped plugin target.
        /// </summary>
        public Plugin()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped plugin instance that all members forward to.
        /// </summary>
        internal IPlugin plugin;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped plugin.
        /// </summary>
        public string Name
        {
            get { return (plugin != null) ? plugin.Name : null; }
            set { if (plugin != null) { plugin.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped plugin.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (plugin != null) ? plugin.Kind : IdentifierKind.None; }
            set { if (plugin != null) { plugin.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped plugin.
        /// </summary>
        public Guid Id
        {
            get { return (plugin != null) ? plugin.Id : Guid.Empty; }
            set { if (plugin != null) { plugin.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped plugin.
        /// </summary>
        public IClientData ClientData
        {
            get { return (plugin != null) ? plugin.ClientData : null; }
            set { if (plugin != null) { plugin.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped plugin.
        /// </summary>
        public string Group
        {
            get { return (plugin != null) ? plugin.Group : null; }
            set { if (plugin != null) { plugin.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped plugin.
        /// </summary>
        public string Description
        {
            get { return (plugin != null) ? plugin.Description : null; }
            set { if (plugin != null) { plugin.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// Gets or sets a value indicating whether the wrapped plugin has been
        /// initialized.
        /// </summary>
        public bool Initialized
        {
            get { return (plugin != null) ? plugin.Initialized : false; }
            set { if (plugin != null) { plugin.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the plugin is being initialized.
        /// </param>
        /// <param name="clientData">
        /// The client data supplied for the initialization.
        /// </param>
        /// <param name="result">
        /// Upon success, receives any result produced by the wrapped plugin.
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method terminates the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the plugin is being terminated.
        /// </param>
        /// <param name="clientData">
        /// The client data supplied for the termination.
        /// </param>
        /// <param name="result">
        /// Upon success, receives any result produced by the wrapped plugin.
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Gets or sets the type name of the wrapped plugin.
        /// </summary>
        public string TypeName
        {
            get { return (plugin != null) ? plugin.TypeName : null; }
            set { if (plugin != null) { plugin.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type of the wrapped plugin.
        /// </summary>
        public Type Type
        {
            get { return (plugin != null) ? plugin.Type : null; }
            set { if (plugin != null) { plugin.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPluginData Members
        /// <summary>
        /// Gets or sets the flags associated with the wrapped plugin.
        /// </summary>
        public PluginFlags Flags
        {
            get { return (plugin != null) ? plugin.Flags : PluginFlags.None; }
            set { if (plugin != null) { plugin.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the version of the wrapped plugin.
        /// </summary>
        public Version Version
        {
            get { return (plugin != null) ? plugin.Version : null; }
            set { if (plugin != null) { plugin.Version = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the URI of the wrapped plugin.
        /// </summary>
        public Uri Uri
        {
            get { return (plugin != null) ? plugin.Uri : null; }
            set { if (plugin != null) { plugin.Uri = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the update URI of the wrapped plugin.
        /// </summary>
        public Uri UpdateUri
        {
            get { return (plugin != null) ? plugin.UpdateUri : null; }
            set { if (plugin != null) { plugin.UpdateUri = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the application domain associated with the wrapped
        /// plugin.
        /// </summary>
        public AppDomain AppDomain
        {
            get { return (plugin != null) ? plugin.AppDomain : null; }
            set { if (plugin != null) { plugin.AppDomain = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the assembly associated with the wrapped plugin.
        /// </summary>
        public Assembly Assembly
        {
            get { return (plugin != null) ? plugin.Assembly : null; }
            set { if (plugin != null) { plugin.Assembly = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the assembly name of the wrapped plugin.
        /// </summary>
        public AssemblyName AssemblyName
        {
            get { return (plugin != null) ? plugin.AssemblyName : null; }
            set { if (plugin != null) { plugin.AssemblyName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the date and time associated with the wrapped plugin.
        /// </summary>
        public DateTime? DateTime
        {
            get { return (plugin != null) ? plugin.DateTime : null; }
            set { if (plugin != null) { plugin.DateTime = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the file name of the wrapped plugin.
        /// </summary>
        public string FileName
        {
            get { return (plugin != null) ? plugin.FileName : null; }
            set { if (plugin != null) { plugin.FileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the list of commands provided by the wrapped plugin.
        /// </summary>
        public CommandDataList Commands
        {
            get { return (plugin != null) ? plugin.Commands : null; }
            set { if (plugin != null) { plugin.Commands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the list of policies provided by the wrapped plugin.
        /// </summary>
        public PolicyDataList Policies
        {
            get { return (plugin != null) ? plugin.Policies : null; }
            set { if (plugin != null) { plugin.Policies = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the list of command tokens associated with the wrapped
        /// plugin.
        /// </summary>
        public LongList CommandTokens
        {
            get { return (plugin != null) ? plugin.CommandTokens : null; }
            set { if (plugin != null) { plugin.CommandTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the list of function tokens associated with the wrapped
        /// plugin.
        /// </summary>
        public LongList FunctionTokens
        {
            get { return (plugin != null) ? plugin.FunctionTokens : null; }
            set { if (plugin != null) { plugin.FunctionTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the list of policy tokens associated with the wrapped
        /// plugin.
        /// </summary>
        public LongList PolicyTokens
        {
            get { return (plugin != null) ? plugin.PolicyTokens : null; }
            set { if (plugin != null) { plugin.PolicyTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the list of trace tokens associated with the wrapped
        /// plugin.
        /// </summary>
        public LongList TraceTokens
        {
            get { return (plugin != null) ? plugin.TraceTokens : null; }
            set { if (plugin != null) { plugin.TraceTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the resource manager associated with the wrapped
        /// plugin.
        /// </summary>
        public ResourceManager ResourceManager
        {
            get { return (plugin != null) ? plugin.ResourceManager : null; }
            set { if (plugin != null) { plugin.ResourceManager = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the auxiliary data associated with the wrapped plugin.
        /// </summary>
        public ObjectDictionary AuxiliaryData
        {
            get { return (plugin != null) ? plugin.AuxiliaryData : null; }
            set { if (plugin != null) { plugin.AuxiliaryData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        #region INotify Members
        /// <summary>
        /// This method gets the notification types of interest to the wrapped
        /// plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the query.
        /// </param>
        /// <returns>
        /// The notification types handled by the wrapped plugin, or
        /// <see cref="NotifyType.None" /> if there is no wrapped plugin.
        /// </returns>
        public NotifyType GetTypes(
            Interpreter interpreter
            )
        {
            return (plugin != null) ?
                plugin.GetTypes(interpreter) : NotifyType.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the notification flags of interest to the wrapped
        /// plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the query.
        /// </param>
        /// <returns>
        /// The notification flags handled by the wrapped plugin, or
        /// <see cref="NotifyFlags.None" /> if there is no wrapped plugin.
        /// </returns>
        public NotifyFlags GetFlags(
            Interpreter interpreter
            )
        {
            return (plugin != null) ?
                plugin.GetFlags(interpreter) : NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method delivers a notification to the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the notification.
        /// </param>
        /// <param name="eventArgs">
        /// The event arguments describing the notification.
        /// </param>
        /// <param name="clientData">
        /// The client data supplied for the notification.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the notification.
        /// </param>
        /// <param name="result">
        /// Upon success, receives any result produced by the wrapped plugin.
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Notify(
            Interpreter interpreter,
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.Notify(
                interpreter, eventArgs, clientData, arguments, ref result);
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteRequest Members
        /// <summary>
        /// This method executes a request against the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="clientData">
        /// The client data supplied for the request.
        /// </param>
        /// <param name="request">
        /// The request object to be processed by the wrapped plugin.
        /// </param>
        /// <param name="response">
        /// Upon success, receives the response object produced by the wrapped
        /// plugin.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            object request,
            ref object response,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.Execute(
                interpreter, clientData, request, ref response, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        /// <summary>
        /// This method performs any post-initialization steps for the wrapped
        /// plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the plugin was initialized.
        /// </param>
        /// <param name="clientData">
        /// The client data supplied for the post-initialization.
        /// </param>
        public void PostInitialize(
            Interpreter interpreter,
            IClientData clientData
            )
        {
            if (plugin != null)
                plugin.PostInitialize(interpreter, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets framework information from the wrapped plugin.
        /// </summary>
        /// <param name="id">
        /// The optional identifier of the framework to query.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the framework information is gathered.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the framework information.  Upon failure,
        /// receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode GetFramework(
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.GetFramework(id, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named stream resource from the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="name">
        /// The name of the stream resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to select the resource.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested stream, or null if it cannot be retrieved.
        /// </returns>
        public Stream GetStream(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return null;
            }

            return plugin.GetStream(
                interpreter, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named string resource from the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="name">
        /// The name of the string resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to select the resource.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested string, or null if it cannot be retrieved.
        /// </returns>
        public string GetString(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return null;
            }

            return plugin.GetString(
                interpreter, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named URI resource from the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="name">
        /// The name of the URI resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to select the resource.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested URI, or null if it cannot be retrieved.
        /// </returns>
        public Uri GetUri(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return null;
            }

            return plugin.GetUri(
                interpreter, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the certificate file name for a named resource of
        /// the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose certificate file name is requested.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The certificate file name, or null if it cannot be retrieved.
        /// </returns>
        public string GetCertificateFileName(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return null;
            }

            return plugin.GetCertificateFileName(
                interpreter, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the certificate for a named resource of the wrapped
        /// plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose certificate is requested.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The certificate, or null if it cannot be retrieved.
        /// </returns>
        public IIdentifier GetCertificate(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return null;
            }

            return plugin.GetCertificate(interpreter, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the key pair for a named resource of the wrapped
        /// plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose key pair is requested.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The key pair, or null if it cannot be retrieved.
        /// </returns>
        public IIdentifier GetKeyPair(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return null;
            }

            return plugin.GetKeyPair(interpreter, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the key ring for a named resource of the wrapped
        /// plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="name">
        /// The name of the resource whose key ring is requested.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The key ring, or null if it cannot be retrieved.
        /// </returns>
        public IIdentifier GetKeyRing(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid wrapper target";
                return null;
            }

            return plugin.GetKeyRing(interpreter, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces the banner information for the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the banner information.  Upon failure,
        /// receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Banner(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.Banner(interpreter, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces the about information for the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the about information.  Upon failure,
        /// receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.About(interpreter, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces the option information for the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the option information.  Upon failure,
        /// receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Options(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.Options(interpreter, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces the status information for the wrapped plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the request.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the status information.  Upon failure,
        /// receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Status(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return plugin.Status(interpreter, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object is disposable;
        /// always false for this wrapper.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value must be an
        /// <see cref="IPlugin" />; otherwise, setting it throws.
        /// </summary>
        public override object Object
        {
            get { return plugin; }
            set { plugin = (IPlugin)value; } /* throw */
        }
        #endregion
    }
}
