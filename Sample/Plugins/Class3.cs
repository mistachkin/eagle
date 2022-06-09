/*
 * Class3.cs --
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
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Plugins = Eagle._Plugins;

namespace Sample
{
    /// <summary>
    /// Declare a "custom plugin" class that inherits default functionality and
    /// implements the appropriate interface(s).  This is the "primary" plugin
    /// for this assembly.  Only one plugin per assembly can be marked as the
    /// "primary" one.
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("c5e02acc-2297-4004-99e6-fbca0e6c812e")]
    [PluginFlags(
        PluginFlags.Primary | PluginFlags.User |
        PluginFlags.Command | PluginFlags.Function |
        PluginFlags.Trace | PluginFlags.Policy)]
    internal sealed class Class3 : _Plugins.Default
    {
        #region Private Constants
        /// <summary>
        /// This constant contains the name of an environment variable, known
        /// to (and used by) only this plugin.  When set, it should contain
        /// the fully qualified path to an extra directory where packages may
        /// be located.
        /// </summary>
        private static readonly string ExtraPackageDirectoryEnvVarName =
            "EAGLE_EXTRA_PACKAGE_DIRECTORY";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This constant contains the name of an environment variable, known
        /// to (and used by) only this plugin.  When set, it should contain
        /// the name of the dynamic command to create in response to calls to
        /// the <see cref="UnknownCallback" /> delegate.
        /// </summary>
        private static readonly string DynamicCommandNameEnvVarName =
            "EAGLE_DYNAMIC_COMMAND_NAME";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// This field is used to store the token returned by the core library
        /// that represents the plugin instance loaded into the interpreter.
        /// </summary>
        private long functionToken;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This field is used to store extra state in the form of a Tcl list,
        /// primarily for use during testing.
        /// </summary>
        private StringList extraState;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructor (Required)
        /// <summary>
        /// Constructs an instance of this custom plugin class.
        /// </summary>
        /// <param name="pluginData">
        /// An instance of the plugin data class containing the properties
        /// used to initialize the new instance of this custom plugin class.
        /// Typically, the only thing done with this parameter is to pass it
        /// along to the base class constructor.
        /// </param>
        public Class3(
            IPluginData pluginData /* in */
            )
            : base(pluginData)
        {
            //
            // NOTE: Typically, nothing much is done here.  Any non-trivial
            //       initialization should be done in IState.Initialize and
            //       cleaned up in IState.Terminate.
            //
            this.Flags |= Utility.GetPluginFlags(GetType().BaseType) |
                Utility.GetPluginFlags(this); /* HIGHLY RECOMMENDED */

            //
            // HACK: For now, skip adding policies if we are being loaded into
            //       an isolated application domain.
            //
            // if (Utility.HasFlags(this.Flags, PluginFlags.Isolated, true))
            //     this.Flags |= PluginFlags.NoPolicies;

            //
            // NOTE: Make sure the extra state is created now, if needed.
            //
            if (extraState == null)
                extraState = new StringList();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// Determines the fully qualified path where extra packages may be
        /// located.
        /// </summary>
        /// <returns>
        /// The fully qualified path where extra packages may be located -OR-
        /// null if it cannot be determined.
        /// </returns>
        private static string GetExtraPackageDirectory()
        {
            //
            // TODO: Do something else here?  Maybe read this value from the
            //       application configuration?
            //
            return Utility.GetEnvironmentVariable(
                ExtraPackageDirectoryEnvVarName, true, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines the name of the dynamic command to return in response
        /// to calls to the <see cref="UnknownCallback" /> delegate.
        /// </summary>
        /// <returns>
        /// The name of the dynamic command to return in response to calls to
        /// the <see cref="UnknownCallback" /> delegate -OR- null if it cannot
        /// be determined.
        /// </returns>
        private static string GetDynamicCommandName()
        {
            //
            // TODO: Do something else here?  Maybe read this value from the
            //       application configuration?
            //
            return Utility.GetEnvironmentVariable(
                DynamicCommandNameEnvVarName, true, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an instance of a class (<see cref="Class13" />) that
        /// can handle the <see cref="PackageCallback" /> delegate.
        /// </summary>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="extraPackageDirectory">
        /// The fully qualified path to a directory that may contain extra
        /// packages.
        /// </param>
        /// <returns>
        /// The newly created class that handles the
        /// <see cref="PackageCallback" /> delegate -OR- null if it cannot
        /// be created.
        /// </returns>
        private static Class13 CreatePackageCallbackClass(
            IPlugin plugin,              /* in */
            string extraPackageDirectory /* in */
            )
        {
            return new Class13(
                plugin, Class13.GetDefaultPackageName(true),
                Class13.GetDefaultPackageName(false),
                extraPackageDirectory);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an instance of a class (<see cref="Class15" />) that can
        /// handle the <see cref="UnknownCallback" /> delegate.
        /// </summary>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="name">
        /// The name of the new dynamic command.
        /// </param>
        /// <returns>
        /// The newly created class that handles the
        /// <see cref="UnknownCallback" /> delegate -OR- null if it cannot
        /// be created.
        /// </returns>
        private static Class15 CreateUnknownCallbackClass(
            IPlugin plugin, /* in */
            string name     /* in */
            )
        {
            return new Class15(plugin, name);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK && WEB
        /// <summary>
        /// Creates an instance of a class (<see cref="Class14" />) that
        /// can handle the <see cref="NewWebClientCallback" /> delegate.
        /// </summary>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <returns>
        /// The newly created class that handles the
        /// <see cref="NewWebClientCallback" /> delegate -OR- null if it
        /// cannot be created.
        /// </returns>
        private static Class14 CreateNewWebClientCallbackClass(
            IPlugin plugin /* in */
            )
        {
            return new Class14(plugin);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// Creates an instance of a class (<see cref="Class13" />) that
        /// implements the <see cref="IPackageCallback" /> interface.
        /// </summary>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="extraPackageDirectory">
        /// The fully qualified path to a directory that may contain extra
        /// packages.
        /// </param>
        /// <returns>
        /// The newly created class that implements the
        /// <see cref="IPackageCallback" /> interface -OR- null if it cannot
        /// be created.
        /// </returns>
        private static IPackageCallback CreatePackageCallback(
            IPlugin plugin,              /* in */
            string extraPackageDirectory /* in */
            )
        {
            return CreatePackageCallbackClass(plugin, extraPackageDirectory);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an instance of a class (<see cref="Class15" />) that
        /// implements the <see cref="IUnknownCallback" /> interface.
        /// </summary>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="name">
        /// The name of the new dynamic command.
        /// </param>
        /// <returns>
        /// The newly created class that implements the
        /// <see cref="IUnknownCallback" /> interface -OR- null if it cannot
        /// be created.
        /// </returns>
        private static IUnknownCallback CreateUnknownCallback(
            IPlugin plugin, /* in */
            string name     /* in */
            )
        {
            return CreateUnknownCallbackClass(plugin, name);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK && WEB
        /// <summary>
        /// Creates an instance of a class (<see cref="Class14" />) that
        /// implements the <see cref="INewWebClientCallback" /> interface.
        /// </summary>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <returns>
        /// The newly created class that implements the
        /// <see cref="INewWebClientCallback" /> interface -OR- null if it
        /// cannot be created.
        /// </returns>
        private static INewWebClientCallback CreateNewWebClientCallback(
            IPlugin plugin /* in */
            )
        {
            return CreateNewWebClientCallbackClass(plugin);
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is used to install -OR- uninstall the per-interpreter
        /// callbacks used for the package subsystem.  It is designed to work
        /// correctly even when the plugin has been loaded into an isolated
        /// application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="extraPackageDirectory">
        /// The fully qualified path to a directory that may contain extra
        /// packages.
        /// </param>
        /// <param name="install">
        /// Non-zero is used to install the callbacks and zero is used to
        /// uninstall them.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        private static ReturnCode InstallPackageCallbacks(
            Interpreter interpreter,      /* in */
            IPlugin plugin,               /* in */
            string extraPackageDirectory, /* in */
            bool install,                 /* in */
            ref Result error              /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            PackageCallbackBridge callbackBridge = null;
#endif

            if (install && Utility.IsCrossAppDomain(interpreter, plugin))
            {
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                callbackBridge = PackageCallbackBridge.Create(
                    CreatePackageCallback(plugin, extraPackageDirectory),
                    ref error);

                if (callbackBridge == null)
                    return ReturnCode.Error;
#else
                error = "cannot set delegates with plugin isolated";
                return ReturnCode.Error;
#endif
            }

            bool locked = false;

            try
            {
                interpreter.TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (install)
                    {
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                        if (callbackBridge != null)
                        {
                            interpreter.PackageFallback =
                                new PackageCallback(
                                    callbackBridge.PackageFallbackCallback);
                        }
                        else
#endif
                        {
                            interpreter.PackageFallback =
                                CreatePackageCallbackClass(plugin,
                                    extraPackageDirectory).PackageFallback;
                        }
                    }
                    else
                    {
                        interpreter.PackageFallback = null;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "interpreter is locked";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                interpreter.ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is used to install -OR- uninstall the per-interpreter
        /// callbacks used for the unknown subsystem.  It is designed to work
        /// correctly even when the plugin has been loaded into an isolated
        /// application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="name">
        /// The name of the new dynamic command.
        /// </param>
        /// <param name="install">
        /// Non-zero is used to install the callbacks and zero is used to
        /// uninstall them.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        private static ReturnCode InstallUnknownCallbacks(
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            string name,             /* in */
            bool install,            /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            UnknownCallbackBridge callbackBridge = null;
#endif

            if (install && Utility.IsCrossAppDomain(interpreter, plugin))
            {
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                callbackBridge = UnknownCallbackBridge.Create(
                    CreateUnknownCallback(plugin, name), ref error);

                if (callbackBridge == null)
                    return ReturnCode.Error;
#else
                error = "cannot set delegates with plugin isolated";
                return ReturnCode.Error;
#endif
            }

            bool locked = false;

            try
            {
                interpreter.TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (install)
                    {
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                        if (callbackBridge != null)
                        {
                            interpreter.UnknownCallback =
                                new UnknownCallback(
                                    callbackBridge.Unknown);
                        }
                        else
#endif
                        {
                            interpreter.UnknownCallback =
                                CreateUnknownCallbackClass(
                                    plugin, name).Unknown;
                        }
                    }
                    else
                    {
                        interpreter.UnknownCallback = null;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "interpreter is locked";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                interpreter.ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK && WEB
        /// <summary>
        /// This method is used to install -OR- uninstall the per-interpreter
        /// callbacks used for the <see cref="System.Net.WebClient" />
        /// subsystem.  It is designed to work correctly even when the plugin
        /// has been loaded into an isolated application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="plugin">
        /// The plugin context we are executing in.
        /// </param>
        /// <param name="install">
        /// Non-zero is used to install the callbacks and zero is used to
        /// uninstall them.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        private static ReturnCode InstallNewWebClientCallbacks(
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            bool install,            /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            NewWebClientCallbackBridge callbackBridge = null;
#endif

            if (install && Utility.IsCrossAppDomain(interpreter, plugin))
            {
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                callbackBridge = NewWebClientCallbackBridge.Create(
                    CreateNewWebClientCallback(plugin), ref error);

                if (callbackBridge == null)
                    return ReturnCode.Error;
#else
                error = "cannot set delegates with plugin isolated";
                return ReturnCode.Error;
#endif
            }

            bool locked = false;

            try
            {
                interpreter.TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (install)
                    {
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                        if (callbackBridge != null)
                        {
                            interpreter.NewWebClientCallback =
                                new NewWebClientCallback(
                                    callbackBridge.NewWebClientCallback);
                        }
                        else
#endif
                        {
                            interpreter.NewWebClientCallback =
                                CreateNewWebClientCallbackClass(
                                    plugin).NewWebClient;
                        }
                    }
                    else
                    {
                        interpreter.NewWebClientCallback = null;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "interpreter is locked";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                interpreter.ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members (Optional)
        /// <summary>
        /// Initialize the plugin and/or setup any needed state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this plugin was initially created, if
        /// any.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Initialize(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most plugins
                //       will want to do this because it is a fairly standard
                //       safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

#if NETWORK && WEB
            if (InstallNewWebClientCallbacks(
                    interpreter, this, true, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

            string extraPackageDirectory = GetExtraPackageDirectory();

            if (InstallPackageCallbacks(
                    interpreter, this, extraPackageDirectory, true,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            string dynamicCommandName = GetDynamicCommandName();

            if (InstallUnknownCallbacks(
                    interpreter, this, dynamicCommandName, true,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            IFunction function = new Class8(new FunctionData(
                typeof(Class8).Name, null, null, clientData, null,
                1, null, Utility.GetFunctionFlags(typeof(Class8)),
                this, functionToken));

            if (interpreter.AddFunction(function, clientData,
                    ref functionToken, ref result) == ReturnCode.Ok)
            {
                return base.Initialize(interpreter, clientData, ref result);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Terminate the plugin and/or cleanup any state we setup during
        /// Initialize.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this plugin was initially created, if
        /// any.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Terminate(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most plugins
                //       will want to do this because it is a fairly standard
                //       safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            string extraPackageDirectory = GetExtraPackageDirectory();

            if (InstallPackageCallbacks(
                    interpreter, this, extraPackageDirectory, false,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            string dynamicCommandName = GetDynamicCommandName();

            if (InstallUnknownCallbacks(
                    interpreter, this, dynamicCommandName, false,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

#if NETWORK && WEB
            if (InstallNewWebClientCallbacks(
                    interpreter, this, false, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

            if ((interpreter == null) || (functionToken == 0) ||
                interpreter.RemoveFunction(functionToken, clientData,
                    ref result) == ReturnCode.Ok)
            {
                functionToken = 0;

                return base.Terminate(interpreter, clientData, ref result);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteRequest Members (Optional)
        /// <summary>
        /// This optional method is designed to handle arbitrary execution
        /// requests from other plugins and/or the interpreter itself.  It
        /// is legal to return success without performing any action.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.  This parameter may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to be used when servicing the execution request,
        /// if any.  This parameter must be treated as strictly optional when
        /// servicing the execution request.  Any execution request that
        /// would succeed when this parameter is non-null must also succeed
        /// when this parameter is null.
        /// </param>
        /// <param name="request">
        /// The object must contain the data required to service the
        /// execution request, if any.  If the execution request can be
        /// properly serviced without any data, this parameter may be null.
        /// </param>
        /// <param name="response">
        /// This object must be modified to contain the result of the
        /// execution request, if any.  If the execution request does not
        /// require data to be included in the response, this parameter
        /// may be modified to contain null.
        /// </param>
        /// <param name="error">
        /// Upon success, the value of this parameter is undefined.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            object request,
            ref object response,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            StringList requestList = request as StringList;

            if (requestList == null)
            {
                string[] requestArray = request as string[];

                if (requestArray != null)
                    requestList = new StringList(requestArray);
            }

            if (requestList == null)
            {
                string requestString = request as string;

                if (Parser.SplitList(
                        interpreter, requestString, 0, Length.Invalid,
                        true, ref requestList, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            string requestCommand = null;

            if (requestList.Count > 0)
                requestCommand = requestList[0];

            if (Utility.SystemStringEquals(requestCommand, "copyState"))
            {
                response = (extraState != null) ?
                    new StringList(extraState) : null; /* Deep Copy */

                return ReturnCode.Ok;
            }
            else if (Utility.SystemStringEquals(requestCommand, "getState"))
            {
                response = extraState; /* Shallow Copy */
                return ReturnCode.Ok;
            }
            else if (Utility.SystemStringEquals(requestCommand, "addState"))
            {
                if (extraState != null)
                {
                    if (requestList.Count > 1)
                        extraState.Add(requestList[1]);
                    else
                        extraState.Add((string)null);

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid extra state";
                    return ReturnCode.Error;
                }
            }
            else if (Utility.SystemStringEquals(requestCommand, "clearState"))
            {
                if (extraState != null)
                {
                    extraState.Clear();
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid extra state";
                    return ReturnCode.Error;
                }
            }

            error = String.Format(
                "unsupported plugin request command {0}",
                Utility.FormatWrapOrNull(requestCommand));

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members (Optional)
        /// <summary>
        /// This method is used to obtain a <see cref="Type" /> or instance of
        /// a type within the plugin.
        /// </summary>
        /// <param name="id">
        /// The <see cref="Guid" /> value associated with the
        /// <see cref="ObjectId" /> attribute for the target type.
        /// </param>
        /// <param name="flags">
        /// These flags determine the semantics of the lookup process used to
        /// locate the target type.
        /// </param>
        /// <param name="result">
        /// Upon success, the <see cref="Result.Value" /> property will be the
        /// target type iself (<see cref="Type" />) or an instance of it.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode GetFramework(
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            return Utility.GetFramework(Assembly, id, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to lookup the specified (resource) stream,
        /// based on its name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="name">
        /// The name of the (resource) stream to lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The information about the selected culture.  This parameter may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The stream upon success -OR- null if the stream cannot be found.
        /// </returns>
        public override Stream GetStream(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return Utility.GetStream(Assembly, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to lookup the specified (resource) string,
        /// based on its name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="name">
        /// The name of the (resource) string to lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The information about the selected culture.  This parameter may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The string upon success -OR- null if the string cannot be found.
        /// </returns>
        public override string GetString(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return Utility.GetAnyString(
                interpreter, this, ResourceManager, name, cultureInfo,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns information about the loaded plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode About(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            )
        {
            result = Utility.FormatPluginAbout(this, true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the compilation options used when compiling the loaded
        /// plugin as a list of strings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain a list of strings consisting of the
        /// compilation options used when compiling the loaded plugin.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Options(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            )
        {
            result = new StringList(DefineConstants.OptionList, false);
            return ReturnCode.Ok;
        }
        #endregion
    }
}
