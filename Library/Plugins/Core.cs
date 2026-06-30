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
    /// <summary>
    /// This class implements the primary, system plugin for the Eagle core
    /// library.  It adds the built-in (core) command set into an interpreter
    /// and provides access to the core library assembly's embedded resources,
    /// framework information, and localized strings.
    /// </summary>
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
        /// <summary>
        /// Constructs an instance of the core plugin, merging the plugin flags
        /// declared via attributes on this type and its base type.
        /// </summary>
        /// <param name="pluginData">
        /// The data used to create and identify this plugin, such as its name
        /// and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method returns the package flags associated with this plugin.
        /// Since this is the core library and this class is sealed, the
        /// resulting flags always include <see cref="PackageFlags.Core" />.
        /// </summary>
        /// <returns>
        /// The package flags for this plugin.
        /// </returns>
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
        /// <summary>
        /// This method clears the core plugin token stored in the interpreter
        /// state, ensuring it is reset when this plugin is terminated.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose core plugin token is reset.  This
        /// parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method is called when the plugin is being terminated within
        /// the specified interpreter.  It resets the core plugin token before
        /// delegating to the base implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is being terminated in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied for this operation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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
        /// <summary>
        /// This method returns information about the framework associated with
        /// this plugin's assembly.
        /// </summary>
        /// <param name="id">
        /// The optional identifier used to select the framework information of
        /// interest.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the framework information is gathered
        /// and formatted.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the requested framework information.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method opens a stream over an embedded resource contained in
        /// this plugin's assembly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operation is being performed in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the embedded resource to open.  This parameter should
        /// not be null or empty.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to select the resource, which is not used by this
        /// implementation.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The opened stream, or null if the resource could not be found or
        /// opened, with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
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

        /// <summary>
        /// This method returns a localized string resource associated with
        /// this plugin, delegating to the interpreter to perform the lookup.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to look up the string.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the string resource to look up.  This parameter may be
        /// null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to select the string resource.  This parameter may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The requested string, or null if it could not be found, with
        /// details placed in the <paramref name="error" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method returns descriptive "about" information for this
        /// plugin, such as its name, version, and copyright.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operation is being performed in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the formatted "about" information.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = FormatOps.PluginAbout(this, false, null);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the list of compile-time options that this
        /// plugin (i.e. the core library) was built with.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operation is being performed in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the list of options.  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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
