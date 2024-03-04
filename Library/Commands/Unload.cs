/*
 * Unload.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("c37b126c-c84e-4296-9931-4f0033645ff4")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Unload : Core
    {
        public Unload(
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
                            new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-clientdata", null),
                            new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-data", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-keeplibrary", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocomplain", null),
                            new Option(null, OptionFlags.MustHaveMatchModeValue, Index.Invalid, Index.Invalid, "-match",
                                new Variant(StringOps.DefaultUnloadMatchMode)),
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
                                        if (options.IsPresent("-data", ref value))
                                        {
                                            IObject @object = (IObject)value.Value;

                                            localClientData = _Public.ClientData.WrapOrReplace(
                                                localClientData, @object.Value);
                                        }

                                        MatchMode mode = StringOps.DefaultUnloadMatchMode;

                                        if (options.IsPresent("-match", ref value))
                                            mode = (MatchMode)value.Value;

                                        bool noCase = false;

                                        if (options.IsPresent("-nocase"))
                                            noCase = true;

                                        if (childInterpreter.HasPlugins(ref result))
                                        {
                                            string fileName = PathOps.ResolveFullPath(
                                                interpreter, arguments[argumentIndex]);

                                            if (!String.IsNullOrEmpty(fileName))
                                            {
                                                string typeName = null;

                                                if ((argumentIndex + 1) < arguments.Count)
                                                    typeName = arguments[argumentIndex + 1];

                                                //
                                                // NOTE: Grab the plugin flags to match from the target
                                                //       interpreter and add the Demand flag to them.
                                                //
                                                PluginFlags pluginFlags =
                                                    childInterpreter.PluginFlags | PluginFlags.Demand;

                                                //
                                                // FIXME: PRI 4: Threading.
                                                //
                                                bool unload = false;
                                                StringList list = childInterpreter.CopyPluginKeys();

                                                foreach (string name in list)
                                                {
                                                    IPluginData pluginData = childInterpreter.GetPluginData(name);

                                                    //
                                                    // NOTE: Check that this plugin represents a loaded
                                                    //       assembly.
                                                    //
                                                    if (pluginData != null)
                                                    {
                                                        if ((pluginData.FileName != null) &&
                                                            PathOps.IsSameFile(interpreter,
                                                                pluginData.FileName, fileName))
                                                        {
                                                            if (String.IsNullOrEmpty(typeName) ||
                                                                StringOps.Match(interpreter, mode,
                                                                    pluginData.TypeName, typeName, noCase) ||
                                                                StringOps.Match(interpreter, mode,
                                                                    pluginData.Name, typeName, noCase))
                                                            {
                                                                code = childInterpreter.UnloadPlugin(
                                                                    name, localClientData, pluginFlags,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    unload = true;

                                                                //
                                                                // NOTE: Stop as soon as we match and
                                                                //       attempt to unload a plugin,
                                                                //       whether or not we actually
                                                                //       unloaded it.  We always halt
                                                                //       on errors and since we only
                                                                //       support unloading a single
                                                                //       plugin at a time (even if
                                                                //       there are multiple plugins
                                                                //       contained in a particular
                                                                //       assembly file), we know it
                                                                //       is safe to stop now.
                                                                //
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }

                                                if ((code == ReturnCode.Ok) && !unload)
                                                {
                                                    if (typeName != null)
                                                        result = String.Format(
                                                            "type \"{0}\" and file \"{1}\" have never been loaded",
                                                            typeName, fileName);
                                                    else
                                                        result = String.Format(
                                                            "file \"{0}\" has never been loaded",
                                                            fileName);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "invalid file name";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            code = ReturnCode.Error;
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
                                    result = "wrong # args: should be \"unload ?options? fileName ?packageName? ?interp?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"unload ?options? fileName ?packageName? ?interp?\"";
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
