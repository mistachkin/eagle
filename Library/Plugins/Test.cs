/*
 * Test.cs --
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
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _ClientData = Eagle._Components.Public.ClientData;

namespace Eagle._Plugins
{
    /// <summary>
    /// This class implements a plugin used for testing the Eagle plugin
    /// subsystem.  It deliberately provides "non-standard" behavior -- such as
    /// managing its own command set and offering switchable implementations of
    /// the request, stream, and string lookup operations -- so that custom
    /// plugin handling can be exercised without relying on the default plugin.
    /// </summary>
    [ObjectId("f5813bfc-7fae-45bb-ab05-9e2d9f1ef49f")]
    [PluginFlags(
        PluginFlags.System | PluginFlags.Command |
        PluginFlags.Static | PluginFlags.MergeCommands |
        PluginFlags.Test)]
    internal sealed class Test : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the test plugin, merging the plugin flags
        /// declared via attributes and initializing the extra flags to their
        /// defaults.
        /// </summary>
        /// <param name="pluginData">
        /// The data used to create and identify this plugin, such as its name
        /// and flags.  This parameter may be null.
        /// </param>
        public Test(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= GetConstructorPluginFlags();
            this.ExtraFlags = GetDefaultExtraFlags();
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

        #region Public Properties
        /// <summary>
        /// The backing field for the <see cref="EnableExecute" /> property.
        /// </summary>
        private bool enableExecute;

        /// <summary>
        /// Gets or sets a value indicating whether the custom request-handling
        /// behavior of this plugin is enabled instead of the base behavior.
        /// </summary>
        public bool EnableExecute
        {
            get { return enableExecute; }
            set { enableExecute = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EnableGetStream" /> property.
        /// </summary>
        private bool enableGetStream;

        /// <summary>
        /// Gets or sets a value indicating whether the custom stream-lookup
        /// behavior of this plugin is enabled instead of the base behavior.
        /// </summary>
        public bool EnableGetStream
        {
            get { return enableGetStream; }
            set { enableGetStream = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="EnableGetString" /> property.
        /// </summary>
        private bool enableGetString;

        /// <summary>
        /// Gets or sets a value indicating whether the custom string-lookup
        /// behavior of this plugin is enabled instead of the base behavior.
        /// </summary>
        public bool EnableGetString
        {
            get { return enableGetString; }
            set { enableGetString = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="ExtraFlags" /> property.
        /// </summary>
        private PluginFlags extraFlags;

        /// <summary>
        /// Gets or sets the extra plugin flags that are combined with the
        /// normal flags when this plugin's state is initialized or terminated.
        /// </summary>
        public PluginFlags ExtraFlags
        {
            get { return extraFlags; }
            set { extraFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method returns the default set of extra plugin flags used by
        /// this plugin.
        /// </summary>
        /// <returns>
        /// The default extra plugin flags.
        /// </returns>
        private static PluginFlags GetDefaultExtraFlags()
        {
            return PluginFlags.NoCommands | PluginFlags.NoFunctions |
                   PluginFlags.NoPolicies | PluginFlags.NoTraces;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method computes the plugin flags to apply during construction,
        /// based on the attributes of this type and its base type.
        /// </summary>
        /// <returns>
        /// The plugin flags to apply during construction.
        /// </returns>
        private PluginFlags GetConstructorPluginFlags()
        {
            return AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the plugin flags to apply when this plugin's
        /// state is initialized or terminated, combining the normal flags with
        /// the extra flags.
        /// </summary>
        /// <returns>
        /// The combined plugin flags.
        /// </returns>
        private PluginFlags GetIStatePluginFlags()
        {
            return this.Flags | this.ExtraFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates the test command owned by this plugin.
        /// </summary>
        /// <param name="clientData">
        /// The extra, command-specific data associated with the new command,
        /// if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created command.
        /// </returns>
        private ICommand CreateCommand(
            IClientData clientData
            )
        {
            return new _Commands.Nop(new CommandData(
                FormatOps.PluginCommand(this.Assembly, this.Name,
                typeof(_Commands.Nop), null), null, null, clientData,
                typeof(_Commands.Nop).FullName, CommandFlags.None,
                this, 0));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the test plugin token stored in the interpreter
        /// state, ensuring it is reset when this plugin is terminated.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose test plugin token is reset.  This
        /// parameter may be null.
        /// </param>
        private void ResetToken(
            Interpreter interpreter
            )
        {
            //
            // HACK: Cleanup the test plugin token in the interpreter
            //       state because this is the only place where we can
            //       be 100% sure it will get done.
            //
            if (interpreter == null)
                return;

            interpreter.InternalTestPluginToken = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// This method is called when the plugin is being initialized within
        /// the specified interpreter.  Unlike the default plugin, it creates
        /// and adds its own test command (subject to the interpreter's
        /// creation flags and rule set) before delegating to the base
        /// implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is being initialized in.  This
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
        public override ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            //
            // NOTE: This method cannot rely on automatic command handling
            //       provided by the default plugin because it does not own
            //       the core command set.  This is very useful for testing
            //       "custom" plugin handling that does not involve relying
            //       on the default plugin.
            //
            // NOTE: *UPDATE* Honor the "NoCommands" creation flag here.
            //
            if ((interpreter != null) && !FlagOps.HasFlags(
                    interpreter.CreateFlags, CreateFlags.NoCommands, true))
            {
                //
                // NOTE: The test plugin command is "non-standard".  Create
                //       and add it only if the interpreter matches.
                //
                ICommand command = CreateCommand(clientData);

                if (!interpreter.IsStandard() && interpreter.ApplyRuleSet(
                        IdentifierKind.Command, MatchMode.IncludeRuleSetMask,
                        ScriptOps.MakeCommandName(command.Name)))
                {
                    if (interpreter.AddCommand(
                            command, null, ref result) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            this.Flags = GetIStatePluginFlags();

            return base.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called when the plugin is being terminated within
        /// the specified interpreter.  Unlike the default plugin, it removes
        /// the commands it owns and withdraws its package before delegating to
        /// the base implementation.
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
            //
            // NOTE: This method cannot rely on automatic command handling
            //       provided by the default plugin because it does not own
            //       the core command set.  This is very useful for testing
            //       "custom" plugin handling that does not involve relying
            //       on the default plugin.
            //
            if (interpreter != null)
            {
                //
                // NOTE: Attempt to remove all commands owned by this plugin
                //       now.  This is harmless if no commands are found to
                //       be owned by this plugin.
                //
                ReturnCode code = interpreter.RemoveCommands(
                    this, clientData, CommandFlags.None, ref result);

                if (code == ReturnCode.Ok)
                {
                    Version version = this.Version;

                    code = interpreter.WithdrawPackage(
                        this.GetType().FullName, version, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        ResetToken(interpreter);

                        result = StringList.MakeList(this.Name, version);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            this.Flags = GetIStatePluginFlags();

            return base.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteRequest Members
        /// <summary>
        /// This method handles a request directed at this plugin.  When the
        /// custom behavior is enabled, it echoes the interpreter, client data,
        /// and request back as the response; otherwise, it delegates to the
        /// base implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this request is being performed in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied for this request, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="request">
        /// The request to handle.  This parameter may be null.
        /// </param>
        /// <param name="response">
        /// Upon success, this contains the response produced for the request.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            object request,
            ref object response,
            ref Result error
            )
        {
            if (!enableExecute)
            {
                return base.Execute(
                    interpreter, clientData, request, ref response,
                    ref error);
            }

            if (clientData != null)
            {
                response = new object[] {
                    interpreter, clientData.Data, request
                };

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid clientData";
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        /// <summary>
        /// This method opens a stream over an embedded resource contained in
        /// this plugin's assembly.  When the custom behavior is enabled, it
        /// tries several name variations before reporting failure; otherwise,
        /// it delegates to the base implementation.
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
        /// The culture used to select the resource.  This parameter may be
        /// null.
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
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (!enableGetStream)
            {
                return base.GetStream(
                    interpreter, name, cultureInfo, ref error);
            }

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

            Stream stream; /* REUSED */
            Result localError; /* REUSED */
            ResultList errors = null;

            localError = null;

            stream = RuntimeOps.GetStream(
                assembly, name, ref localError);

            if (stream != null)
            {
                return stream;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            localError = null;

            stream = RuntimeOps.GetStream(
                assembly, PathOps.MakeRelativePath(name, true),
                ref localError);

            if (stream != null)
            {
                return stream;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            string prefix = GlobalState.GetBasePath();

            if (!String.IsNullOrEmpty(prefix) &&
                name.StartsWith(prefix, PathOps.ComparisonType))
            {
                localError = null;

                stream = RuntimeOps.GetStream(
                    assembly, name.Substring(prefix.Length),
                    ref localError);

                if (stream != null)
                {
                    return stream;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            localError = null;

            stream = RuntimeOps.GetStream(
                assembly, Path.GetFileName(name), ref localError);

            if (stream != null)
            {
                return stream;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            if (errors == null)
            {
                errors = new ResultList();
                errors.Add("stream not found");
            }

            error = errors;
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string resource associated with this plugin.
        /// When the custom behavior is enabled, it returns a diagnostic string
        /// for the recognized test name; otherwise, it delegates to the base
        /// implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operation is being performed in.  This
        /// parameter may be null.
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
            if (!enableGetString)
            {
                return base.GetString(
                    interpreter, name, cultureInfo, ref error);
            }

            ResultList errors = null;

#if TEST
            string testValue;
            Result testError = null;

            testValue = _Tests.Default.TestGetString(
                interpreter, name, cultureInfo, ref testError);

            if (testValue != null)
            {
                return testValue;
            }
            else if (testError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(testError);
            }
#endif

            if (SharedStringOps.SystemEquals(name, typeof(Test).Name))
            {
                return String.Format(
                    "interpreter: {0}, name: {1}, cultureInfo: {2}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(cultureInfo));
            }
            else
            {
                if (errors == null)
                    errors = new ResultList();

                if (name != null)
                    errors.Add("unrecognized string name");
                else
                    errors.Add("invalid string name");
            }

            error = errors;
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
