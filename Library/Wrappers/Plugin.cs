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
    [ObjectId("ec624d41-7b2f-4fa6-81d0-2eaf70a7f29b")]
    internal sealed class Plugin : Default, IPlugin
    {
        #region Public Constructors
        public Plugin(
            long token,
            IPlugin plugin
            )
            : base(token)
        {
            this.plugin = plugin;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IPlugin plugin;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (plugin != null) ? plugin.Name : null; }
            set { if (plugin != null) { plugin.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (plugin != null) ? plugin.Kind : IdentifierKind.None; }
            set { if (plugin != null) { plugin.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (plugin != null) ? plugin.Id : Guid.Empty; }
            set { if (plugin != null) { plugin.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (plugin != null) ? plugin.ClientData : null; }
            set { if (plugin != null) { plugin.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (plugin != null) ? plugin.Group : null; }
            set { if (plugin != null) { plugin.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (plugin != null) ? plugin.Description : null; }
            set { if (plugin != null) { plugin.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public bool Initialized
        {
            get { return (plugin != null) ? plugin.Initialized : false; }
            set { if (plugin != null) { plugin.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (plugin != null)
                return plugin.Initialize(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (plugin != null)
                return plugin.Terminate(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        public string TypeName
        {
            get { return (plugin != null) ? plugin.TypeName : null; }
            set { if (plugin != null) { plugin.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Type Type
        {
            get { return (plugin != null) ? plugin.Type : null; }
            set { if (plugin != null) { plugin.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPluginData Members
        public PluginFlags Flags
        {
            get { return (plugin != null) ? plugin.Flags : PluginFlags.None; }
            set { if (plugin != null) { plugin.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Version Version
        {
            get { return (plugin != null) ? plugin.Version : null; }
            set { if (plugin != null) { plugin.Version = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Uri Uri
        {
            get { return (plugin != null) ? plugin.Uri : null; }
            set { if (plugin != null) { plugin.Uri = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Uri UpdateUri
        {
            get { return (plugin != null) ? plugin.UpdateUri : null; }
            set { if (plugin != null) { plugin.UpdateUri = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public AppDomain AppDomain
        {
            get { return (plugin != null) ? plugin.AppDomain : null; }
            set { if (plugin != null) { plugin.AppDomain = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Assembly Assembly
        {
            get { return (plugin != null) ? plugin.Assembly : null; }
            set { if (plugin != null) { plugin.Assembly = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public AssemblyName AssemblyName
        {
            get { return (plugin != null) ? plugin.AssemblyName : null; }
            set { if (plugin != null) { plugin.AssemblyName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public DateTime? DateTime
        {
            get { return (plugin != null) ? plugin.DateTime : null; }
            set { if (plugin != null) { plugin.DateTime = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string FileName
        {
            get { return (plugin != null) ? plugin.FileName : null; }
            set { if (plugin != null) { plugin.FileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public CommandDataList Commands
        {
            get { return (plugin != null) ? plugin.Commands : null; }
            set { if (plugin != null) { plugin.Commands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public PolicyDataList Policies
        {
            get { return (plugin != null) ? plugin.Policies : null; }
            set { if (plugin != null) { plugin.Policies = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public LongList CommandTokens
        {
            get { return (plugin != null) ? plugin.CommandTokens : null; }
            set { if (plugin != null) { plugin.CommandTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public LongList FunctionTokens
        {
            get { return (plugin != null) ? plugin.FunctionTokens : null; }
            set { if (plugin != null) { plugin.FunctionTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public LongList PolicyTokens
        {
            get { return (plugin != null) ? plugin.PolicyTokens : null; }
            set { if (plugin != null) { plugin.PolicyTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public LongList TraceTokens
        {
            get { return (plugin != null) ? plugin.TraceTokens : null; }
            set { if (plugin != null) { plugin.TraceTokens = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ResourceManager ResourceManager
        {
            get { return (plugin != null) ? plugin.ResourceManager : null; }
            set { if (plugin != null) { plugin.ResourceManager = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary AuxiliaryData
        {
            get { return (plugin != null) ? plugin.AuxiliaryData : null; }
            set { if (plugin != null) { plugin.AuxiliaryData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        #region INotify Members
        public NotifyType GetTypes(
            Interpreter interpreter
            )
        {
            if (plugin != null)
                return plugin.GetTypes(interpreter);
            else
                return NotifyType.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public NotifyFlags GetFlags(
            Interpreter interpreter
            )
        {
            if (plugin != null)
                return plugin.GetFlags(interpreter);
            else
                return NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Notify(
            Interpreter interpreter,
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (plugin != null)
            {
                return plugin.Notify(
                    interpreter, eventArgs, clientData, arguments,
                    ref result);
            }
            else
            {
                return ReturnCode.Error;
            }
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteRequest Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            object request,
            ref object response,
            ref Result error
            )
        {
            if (plugin != null)
            {
                return plugin.Execute(
                    interpreter, clientData, request, ref response,
                    ref error);
            }
            else
            {
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        public void PostInitialize(
            Interpreter interpreter,
            IClientData clientData
            )
        {
            if (plugin != null)
                plugin.PostInitialize(interpreter, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode GetFramework(
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            if (plugin != null)
                return plugin.GetFramework(id, flags, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public Stream GetStream(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (plugin != null)
            {
                return plugin.GetStream(
                    interpreter, name, cultureInfo, ref error);
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetString(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (plugin != null)
            {
                return plugin.GetString(
                    interpreter, name, cultureInfo, ref error);
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public Uri GetUri(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (plugin != null)
            {
                return plugin.GetUri(
                    interpreter, name, cultureInfo, ref error);
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetCertificateFileName(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin != null)
            {
                return plugin.GetCertificateFileName(
                    interpreter, name, ref error);
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public IIdentifier GetCertificate(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin != null)
                return plugin.GetCertificate(interpreter, name, ref error);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public IIdentifier GetKeyPair(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin != null)
                return plugin.GetKeyPair(interpreter, name, ref error);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public IIdentifier GetKeyRing(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (plugin != null)
                return plugin.GetKeyRing(interpreter, name, ref error);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Banner(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin != null)
                return plugin.Banner(interpreter, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin != null)
                return plugin.About(interpreter, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Options(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin != null)
                return plugin.Options(interpreter, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Status(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (plugin != null)
                return plugin.Status(interpreter, ref result);
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
            get { return plugin; }
        }
        #endregion
    }
}
