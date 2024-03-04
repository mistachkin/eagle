/*
 * Load.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if CAS_POLICY
using System.Configuration.Assemblies;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _ClientData = Eagle._Components.Public.ClientData;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("eba460e1-048f-409a-a18c-70c5dc6aad6b")]
    [CommandFlags(
        CommandFlags.Unsafe | CommandFlags.Standard |
        CommandFlags.SecuritySdk | CommandFlags.LicenseSdk)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Load : Core
    {
        public Load(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.MustHaveRuleSetValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-ruleset", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-needclientdata", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-anythread", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nocommands", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nofunctions", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nopolicies", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-notraces", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noprovide", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noresources", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-verifiedonly", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-trustedonly", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-publickeytoken", null),
#if ISOLATED_PLUGINS
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-isolated", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noisolated", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-preview", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nopreview", null),
#else
                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid,
                                "-isolated", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid,
                                "-noisolated", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid,
                                "-preview", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid,
                                "-nopreview", null),
#endif
#if ISOLATED_PLUGINS && SHELL
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-update", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noupdate", null),
#else
                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid,
                                "-update", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid,
                                "-noupdate", null),
#endif
                            new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-clientdata", null),
                            new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-data", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-viaresource", null),
                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: There should be a minimum of one and a maximum
                            //       of three arguments after the final option.
                            //
                            if ((argumentIndex != Index.Invalid) &&
                                ((argumentIndex + 3) >= arguments.Count))
                            {
                                string path = ((argumentIndex + 2) < arguments.Count) ?
                                    (string)arguments[argumentIndex + 2] : String.Empty;

                                Interpreter childInterpreter = null;

                                code = interpreter.GetNestedChildInterpreter(
                                    path, LookupFlags.Interpreter, false,
                                    ref childInterpreter, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    IVariant value = null;
                                    IRuleSet ruleSet = null;

                                    if (options.IsPresent("-ruleset", ref value))
                                        ruleSet = (IRuleSet)value.Value;

                                    IClientData localClientData = clientData;

                                    if (options.IsPresent("-clientdata", ref value))
                                    {
                                        IObject @object = (IObject)value.Value;

                                        if ((@object.Value == null) ||
                                            (@object.Value is IClientData))
                                        {
                                            localClientData = (IClientData)@object.Value;
                                        }
                                        else
                                        {
                                            result = "option value has invalid clientData";
                                            code = ReturnCode.Error;
                                        }
                                    }

                                    if (code == ReturnCode.Ok)
                                    {
                                        if (options.IsPresent("-needclientdata"))
                                        {
                                            localClientData = _ClientData.MaybeCreate(
                                                localClientData);
                                        }

                                        if (options.IsPresent("-data", ref value))
                                        {
                                            IObject @object = (IObject)value.Value;

                                            if (@object != null)
                                            {
                                                localClientData = _ClientData.WrapOrReplace(
                                                    localClientData, @object.Value);
                                            }
                                            else
                                            {
                                                result = "option value has invalid data";
                                                code = ReturnCode.Error;
                                            }
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            //
                                            // NOTE: All plugins loaded by this command are considered
                                            //       as having been loaded "on demand".
                                            //
                                            PluginFlags pluginFlags = PluginFlags.Demand;

                                            //
                                            // NOTE: Add the plugin flags for the target interpreter.
                                            //
                                            pluginFlags |= childInterpreter.PluginFlags;

#if ISOLATED_PLUGINS
                                            //
                                            // NOTE: Enable loading this plugin into an isolated
                                            //       application domain?
                                            //
                                            if (options.IsPresent("-isolated"))
                                                pluginFlags |= PluginFlags.Isolated;

                                            //
                                            // NOTE: Disable loading this plugin into an isolated
                                            //       application domain (i.e. load it into the default
                                            //       application domain for the target interpreter)?
                                            //
                                            if (options.IsPresent("-noisolated"))
                                                pluginFlags &= ~PluginFlags.Isolated;

                                            //
                                            // HACK: By default, see if the script security subsystem
                                            //       wants to preview the plugin metadata in order to
                                            //       perform a plugin update check.  This flag can be
                                            //       overridden below via -preview and/or -nopreview
                                            //       options.
                                            //
                                            if (interpreter.HasSecurityLevel() &&
                                                !ScriptOps.ShouldCheckForSecurityUpdate(interpreter, true))
                                            {
                                                pluginFlags |= PluginFlags.NoPreview;
                                            }

                                            //
                                            // NOTE: Enable the plugin "preview" subsystem?
                                            //
                                            if (options.IsPresent("-preview"))
                                                pluginFlags &= ~PluginFlags.NoPreview;

                                            //
                                            // NOTE: Disable the plugin "preview" subsystem?
                                            //
                                            if (options.IsPresent("-nopreview"))
                                                pluginFlags |= PluginFlags.NoPreview;

#if SHELL
                                            //
                                            // HACK: By default, see if the script security subsystem
                                            //       wants to perform a plugin update check.  This
                                            //       flag can be overridden below via -update and/or
                                            //       -noupdate options.
                                            //
                                            if (interpreter.HasSecurityLevel() &&
                                                ScriptOps.ShouldCheckForSecurityUpdate(interpreter, true))
                                            {
                                                pluginFlags |= PluginFlags.UpdateCheck;
                                            }

                                            //
                                            // NOTE: Enable checking for an updated version of this
                                            //       plugin prior to loading it?
                                            //
                                            if (options.IsPresent("-update"))
                                                pluginFlags |= PluginFlags.UpdateCheck;

                                            //
                                            // NOTE: Disable checking for an updated version of this
                                            //       plugin prior to loading it?
                                            //
                                            if (options.IsPresent("-noupdate"))
                                                pluginFlags &= ~PluginFlags.UpdateCheck;
#endif
#endif

                                            if (options.IsPresent("-anythread"))
                                                pluginFlags |= PluginFlags.LoadOnAnyThread;

                                            if (options.IsPresent("-nocommands"))
                                                pluginFlags |= PluginFlags.NoCommands;

                                            if (options.IsPresent("-nofunctions"))
                                                pluginFlags |= PluginFlags.NoFunctions;

                                            if (options.IsPresent("-nopolicies"))
                                                pluginFlags |= PluginFlags.NoPolicies;

                                            if (options.IsPresent("-notraces"))
                                                pluginFlags |= PluginFlags.NoTraces;

                                            if (options.IsPresent("-noprovide"))
                                                pluginFlags |= PluginFlags.NoProvide;

                                            if (options.IsPresent("-noresources"))
                                                pluginFlags |= PluginFlags.NoResources;

                                            if (options.IsPresent("-verifiedonly"))
                                                pluginFlags |= PluginFlags.VerifiedOnly;

                                            if (options.IsPresent("-trustedonly"))
                                                pluginFlags |= PluginFlags.TrustedOnly;

                                            bool viaResource = false;

                                            if (options.IsPresent("-viaresource"))
                                                viaResource = true;

                                            byte[] publicKeyToken = null;

                                            if (options.IsPresent("-publickeytoken", ref value))
                                            {
                                                code = RuntimeOps.GetPublicKeyToken(
                                                    value.ToString(), interpreter.InternalCultureInfo,
                                                    ref publicKeyToken, ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                string maybeFileNameOnly = arguments[argumentIndex];

                                                string fileName = !viaResource ?
                                                    PathOps.ResolveFullPath(interpreter, maybeFileNameOnly) :
                                                    null;

                                                if ((viaResource && !String.IsNullOrEmpty(maybeFileNameOnly)) ||
                                                    (!viaResource && !String.IsNullOrEmpty(fileName)))
                                                {
                                                    if ((publicKeyToken == null) ||
                                                        RuntimeOps.CheckPublicKeyToken(
                                                            fileName, publicKeyToken, ref result))
                                                    {
                                                        string typeName = null;

                                                        if ((argumentIndex + 1) < arguments.Count)
                                                            typeName = arguments[argumentIndex + 1];

                                                        IPlugin plugin = null;
                                                        long token = 0;

                                                        try
                                                        {
                                                            if (viaResource)
                                                            {
                                                                code = RuntimeOps.LoadPlugin(
                                                                    childInterpreter, ruleSet, maybeFileNameOnly,
#if CAS_POLICY
                                                                    null,
#endif
                                                                    typeName, localClientData, pluginFlags,
                                                                    ref plugin, ref result);
                                                            }
                                                            else
                                                            {
                                                                code = childInterpreter.LoadPlugin(ruleSet,
                                                                    fileName,
#if CAS_POLICY
                                                                    null, null, AssemblyHashAlgorithm.None,
#endif
                                                                    typeName, localClientData, pluginFlags,
                                                                    ref plugin, ref result);
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                code = childInterpreter.AddPlugin(
                                                                    plugin, localClientData, ref token,
                                                                    ref result);
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            if (code != ReturnCode.Ok)
                                                            {
                                                                if (token != 0)
                                                                {
                                                                    //
                                                                    // NOTE: Terminate and remove the plugin now.
                                                                    //       This does not unload the associated
                                                                    //       AppDomain, if any.
                                                                    //
                                                                    ReturnCode removeCode;
                                                                    Result removeResult = null;

                                                                    removeCode = childInterpreter.RemovePlugin(
                                                                        token, localClientData, ref removeResult);

                                                                    if (removeCode != ReturnCode.Ok)
                                                                    {
                                                                        DebugOps.Complain(
                                                                            childInterpreter, removeCode,
                                                                            removeResult);
                                                                    }
                                                                }

                                                                if (plugin != null)
                                                                {
                                                                    //
                                                                    // NOTE: Unload the plugin.  This basically does
                                                                    //       "nothing" unless the plugin was isolated.
                                                                    //       In that case, it unloads the associated
                                                                    //       AppDomain.
                                                                    //
                                                                    ReturnCode unloadCode;
                                                                    Result unloadResult = null;

                                                                    unloadCode = childInterpreter.UnloadPlugin(
                                                                        plugin, localClientData, pluginFlags |
                                                                        PluginFlags.SkipTerminate, ref unloadResult);

                                                                    if (unloadCode != ReturnCode.Ok)
                                                                    {
                                                                        DebugOps.Complain(
                                                                            childInterpreter, unloadCode,
                                                                            unloadResult);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid file name";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(
                                        options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    result = "wrong # args: should be \"load ?options? fileName ?packageName? ?interp?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"load ?options? fileName ?packageName? ?interp?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
