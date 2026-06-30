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
    /// <summary>
    /// This class implements the Eagle <c>unload</c> command, which unloads a
    /// previously loaded plugin assembly from an interpreter, identified by its
    /// file name and an optional type or package name.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("c37b126c-c84e-4296-9931-4f0033645ff4")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Critical |
        CommandFlags.Standard)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Unload : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>unload</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Unload(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>unload</c> command.  It parses any
        /// options, resolves the target (possibly nested) child interpreter,
        /// resolves the supplied file name to a full path, and unloads the
        /// matching loaded plugin assembly, optionally filtered by a type or
        /// package name and honoring the match-mode and case options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null and may be overridden
        /// by the <c>-clientdata</c> and <c>-data</c> options.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name, followed by any options and then the file name and
        /// the optional package name and child interpreter path.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of unloading the plugin.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, an option is invalid, the target interpreter cannot be
        /// found, the file name is invalid, the matching plugin was never
        /// loaded, the interpreter is null, or the argument list is null, with
        /// details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Unload);

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
