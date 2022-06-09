/*
 * Core.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    [ObjectId("416b7692-6f4d-472b-be6c-f2da391bee87")]
    [PluginFlags(
#if NATIVE
        PluginFlags.Primary | PluginFlags.System |
        PluginFlags.Host | PluginFlags.Debugger |
        PluginFlags.Command | PluginFlags.Function |
        PluginFlags.Trace | PluginFlags.Policy |
        PluginFlags.Resolver | PluginFlags.Static |
        PluginFlags.NativeCode | PluginFlags.MergeCommands |
        PluginFlags.NoPolicies | PluginFlags.NoTraces
#else
        PluginFlags.Primary | PluginFlags.System |
        PluginFlags.Host | PluginFlags.Debugger |
        PluginFlags.Command | PluginFlags.Function |
        PluginFlags.Trace | PluginFlags.Policy |
        PluginFlags.Resolver | PluginFlags.Static |
        PluginFlags.MergeCommands | PluginFlags.NoPolicies |
        PluginFlags.NoTraces
#endif
    )]
    internal sealed class Core : Default
    {
        #region Public Constructors
        public Core(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            //
            // NOTE: This plugin adds the "core" command set into the specified
            //       interpreter.  These commands will typically always be
            //       available in a given interpreter; however, this is not
            //       absolutely guaranteed as they can be explicitly unloaded.
            //
            this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        protected override PackageFlags GetPackageFlags()
        {
            //
            // NOTE: We know the package is a core package because this is
            //       the core library and this class is sealed.
            //
            return PackageFlags.Core | base.GetPackageFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void ResetToken(
            Interpreter interpreter
            )
        {
            //
            // HACK: Cleanup the core plugin token in the interpreter
            //       state because this is the only place where we can
            //       be 100% sure it will get done.
            //
            if (interpreter == null)
                return;

            interpreter.InternalCorePluginToken = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public override ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            ResetToken(interpreter);

            return base.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        public override ReturnCode GetFramework(
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            return RuntimeOps.GetFramework(
                this.Assembly, id, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public override Stream GetStream(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo, /* NOT USED */
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(name))
            {
                error = "invalid stream name";
                return null;
            }

            Assembly assembly = this.Assembly;

            if (assembly == null)
            {
                error = "plugin assembly not available";
                return null;
            }

            try
            {
                Stream stream = assembly.GetManifestResourceStream(
                    PathOps.MakeRelativePath(name, true));

                if (stream != null)
                    return stream;
                else
                    error = "stream not found";
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public override string GetString(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                return interpreter.GetString(
                    this, name, cultureInfo, ref error);
            }
            else
            {
                error = "invalid interpreter";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = FormatOps.PluginAbout(this, false, null);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Options(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = new StringList(DefineConstants.OptionList, false);
            return ReturnCode.Ok;
        }
        #endregion
    }
}
