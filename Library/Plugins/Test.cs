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
    [ObjectId("f5813bfc-7fae-45bb-ab05-9e2d9f1ef49f")]
    [PluginFlags(
        PluginFlags.System | PluginFlags.Command |
        PluginFlags.Static | PluginFlags.MergeCommands |
        PluginFlags.Test)]
    internal sealed class Test : Default
    {
        #region Public Constructors
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
        private bool enableExecute;
        public bool EnableExecute
        {
            get { return enableExecute; }
            set { enableExecute = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool enableGetStream;
        public bool EnableGetStream
        {
            get { return enableGetStream; }
            set { enableGetStream = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool enableGetString;
        public bool EnableGetString
        {
            get { return enableGetString; }
            set { enableGetString = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PluginFlags extraFlags;
        public PluginFlags ExtraFlags
        {
            get { return extraFlags; }
            set { extraFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static PluginFlags GetDefaultExtraFlags()
        {
            return PluginFlags.NoCommands | PluginFlags.NoFunctions |
                   PluginFlags.NoPolicies | PluginFlags.NoTraces;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private PluginFlags GetConstructorPluginFlags()
        {
            return AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);
        }

        ///////////////////////////////////////////////////////////////////////

        private PluginFlags GetIStatePluginFlags()
        {
            return this.Flags | this.ExtraFlags;
        }

        ///////////////////////////////////////////////////////////////////////

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
