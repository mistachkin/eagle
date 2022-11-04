/*
 * Shell.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

using SharedStringOps = Eagle._Components.Shared.StringOps;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;

namespace Eagle._Hosts
{
    [ObjectId("7c8dc9cc-9e23-4a5d-a0c6-00fe61846e0d")]
    public abstract class Shell : Profile, IDisposable
    {
        #region Private Constants
        private const string DefaultImageRuntimeVersion = "v4.0.30319";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const string ForegroundColorSuffix = "ForegroundColor";
        private const string BackgroundColorSuffix = "BackgroundColor";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        protected Shell(
            IHostData hostData
            )
            : base(hostData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Prompt Support
        protected virtual string GetPromptVariableName(
            PromptType type,
            PromptFlags flags
            )
        {
            bool debug = FlagOps.HasFlags(flags, PromptFlags.Debug, true);
            bool queue = FlagOps.HasFlags(flags, PromptFlags.Queue, true);

            if (debug)
            {
                if (queue)
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Core.Prompt8 : TclVars.Core.Prompt7;
                }
                else
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Core.Prompt4 : TclVars.Core.Prompt3;
                }
            }
            else
            {
                if (queue)
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Core.Prompt6 : TclVars.Core.Prompt5;
                }
                else
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Core.Prompt2 : TclVars.Core.Prompt1;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Title Support
        protected virtual string BuildCoreTitle(
            string packageName,
            Assembly assembly
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            string release = FormatOps.ReleaseAttribute(
                SharedAttributeOps.GetAssemblyRelease(assembly));

            bool haveRelease = !String.IsNullOrEmpty(release);

            string text = RuntimeOps.GetAssemblyTextOrSuffix(assembly);

            //
            // HACK: The image runtime version is largely useless for
            //       Mono and .NET Core as it will (basically) always
            //       be "v4.0.30319" for backward compatibility with
            //       the .NET Framework 4.x.
            //
            string runtimeVersion;

            string imageRuntimeVersion =
                AssemblyOps.GetImageRuntimeVersion(assembly);

            if (SharedStringOps.SystemEquals(
                    imageRuntimeVersion,
                    DefaultImageRuntimeVersion) &&
                (ShouldTreatAsMono() ||
                ShouldTreatAsDotNetCore()))
            {
                runtimeVersion = FormatOps.ShortRuntimeVersion(
                    CommonOps.Runtime.GetRuntimeVersion());
            }
            else
            {
                runtimeVersion = FormatOps.ShortImageRuntimeVersion(
                    imageRuntimeVersion);
            }

            string configuration = AttributeOps.GetAssemblyConfiguration(
                assembly);

            string[] values = {
                packageName, FormatOps.MajorMinor(
                    AssemblyOps.GetVersion(assembly),
                    Characters.v.ToString(), null),
                haveRelease ? release :
                    SharedAttributeOps.GetAssemblyTag(assembly),
                haveRelease ? null :
                    FormatOps.PackageDateTime(
                        SharedAttributeOps.GetAssemblyDateTime(
                            assembly)),
                FormatOps.AssemblyTextAndConfiguration(
                    text, runtimeVersion, configuration,
                    Characters.OpenParenthesis.ToString(),
                    Characters.CloseParenthesis.ToString())
            };

            foreach (string value in values)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (result.Length > 0)
                        result.Append(Characters.Space);

                    result.Append(value);
                }
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support the "Prompt" method and the
                //       title subsystem.
                //
                hostFlags = HostFlags.Prompt | HostFlags.Title |
                    base.MaybeInitializeHostFlags();
            }

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetWriteException(
            bool exception
            )
        {
            base.SetWriteException(exception);
            PrivateResetHostFlagsOnly();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        public override ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            flags &= ~PromptFlags.Done;

            Interpreter localInterpreter = InternalSafeGetInterpreter(
                false);

            if (localInterpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Grab the integer identifier for the interpreter as this
            //       will help the end users to identity which interpreter is
            //       emitting the prompt.
            //
            long id = localInterpreter.IdNoThrow;

            if (id > 1) /* HACK: Omit Id for primary. */
                flags |= PromptFlags.Interpreter;

            //
            // BUGFIX: Always bypass the interpreter readiness checks here;
            //         otherwise, we can get into very nasty situations (e.g.
            //         infinite recursion for [debug oncancel], etc).
            //
            ReturnCode code;
            Result value = null;

            if ((type != PromptType.None) &&
                (localInterpreter.GetVariableValue(
                    VariableFlags.ViaPrompt, GetPromptVariableName(
                        type, flags), ref value) == ReturnCode.Ok))
            {
                Result result = null;
                int errorLine = 0;

                code = localInterpreter.EvaluatePromptScript(
                    value, ref result, ref errorLine);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: The prompt script probably displayed some kind
                    //       of prompt; therefore, we are done.
                    //
                    flags |= PromptFlags.Done;
                }
                else
                {
                    //
                    // NOTE: Attempt to show the error from the prompt script.
                    //
                    /* IGNORED */
                    WriteResultLine(code, result, errorLine);

                    //
                    // NOTE: Add error information to the interpreter.
                    //
                    _Engine.AddErrorInformation(
                        localInterpreter, result, String.Format(
                            "{0}    (script that generates prompt, line {1})",
                            Environment.NewLine, errorLine));

                    //
                    // NOTE: Now, transfer the prompt script evaluation error
                    //       to the caller.
                    //
                    error = result;
                }
            }
            else
            {
                //
                // NOTE: Either our caller requested a prompt type of "None"
                //       -OR- there is no prompt script configured.  So far,
                //       this has been a complete success.
                //
                code = ReturnCode.Ok;
            }

            //
            // NOTE: If we did not evaluate a prompt script -OR- if that script
            //       failed then we attempt to output the appropriate default
            //       prompt.
            //
            if ((value == null) || (code != ReturnCode.Ok))
            {
                //
                // NOTE: Now, we need to fallback to the default
                //       prompt.
                //
                string prompt = HostOps.GetDefaultPrompt(type, flags, id);

                //
                // NOTE: If we got a valid default prompt for this
                //       type, attempt to write it now.
                //
                if ((prompt != null) && Write(prompt))
                {
                    //
                    // NOTE: We displayed the debug prompt for this
                    //       type.
                    //
                    flags |= PromptFlags.Done;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        public override ReturnCode GetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: Only the "default" theme (i.e. using null or an empty
            //       string for the configuration) is supported for now.
            //
            if (String.IsNullOrEmpty(theme))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    try
                    {
                        ConsoleColor localForegroundColor = DefaultForegroundColor;
                        ConsoleColor localBackgroundColor = DefaultBackgroundColor;

                        //
                        // NOTE: Did they request the foreground color?
                        //
                        if ((code == ReturnCode.Ok) && foreground)
                        {
                            PropertyInfo propertyInfo = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, ForegroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (propertyInfo != null)
                            {
                                if (propertyInfo.CanRead)
                                {
                                    localForegroundColor = (ConsoleColor)propertyInfo.GetValue(
                                        this, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for foreground color \"{0}\" cannot be read",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for foreground color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Did they request the background color?
                        //
                        if ((code == ReturnCode.Ok) && background)
                        {
                            PropertyInfo propertyInfo = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, BackgroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (propertyInfo != null)
                            {
                                if (propertyInfo.CanRead)
                                {
                                    localBackgroundColor = (ConsoleColor)propertyInfo.GetValue(
                                        this, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for background color \"{0}\" cannot be read",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for background color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: If we succeeded in looking up the requested colors,
                        //       return them now.
                        //
                        if (code == ReturnCode.Ok)
                        {
                            foregroundColor = localForegroundColor;
                            backgroundColor = localBackgroundColor;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid color name";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "unsupported theme name";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode SetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: Only the "default" theme (i.e. using null or an empty
            //       string for the configuration) is supported for now.
            //
            if (String.IsNullOrEmpty(theme))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    try
                    {
                        //
                        // NOTE: Did they request the foreground color?
                        //
                        if ((code == ReturnCode.Ok) && foreground)
                        {
                            PropertyInfo propertyInfo = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, ForegroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (propertyInfo != null)
                            {
                                if (propertyInfo.CanWrite)
                                {
                                    propertyInfo.SetValue(this, foregroundColor, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for foreground color \"{0}\" cannot be written",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for foreground color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Did they request the background color?
                        //
                        if ((code == ReturnCode.Ok) && background)
                        {
                            PropertyInfo propertyInfo = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, BackgroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (propertyInfo != null)
                            {
                                if (propertyInfo.CanWrite)
                                {
                                    propertyInfo.SetValue(this, backgroundColor, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for background color \"{0}\" cannot be written",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for background color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid color name";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "unsupported theme name";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override string DefaultTitle
        {
            get
            {
                CheckDisposed();

                try
                {
                    if (base.DefaultTitle == null)
                    {
                        string packageName = GlobalState.GetPackageName();

                        if (!String.IsNullOrEmpty(packageName))
                        {
                            Assembly assembly = GlobalState.GetAssembly();

                            base.DefaultTitle = BuildCoreTitle(
                                packageName, assembly);
                        }
                    }

                    return base.DefaultTitle;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Shell).Name,
                        TracePriority.HostError);
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if (base.Reset(ref error) == ReturnCode.Ok)
            {
                if (!PrivateResetHostFlags()) /* NON-VIRTUAL */
                {
                    error = "failed to reset flags";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    InternalSafeGetInterpreter(false), null))
            {
                throw new InterpreterDisposedException(typeof(Shell));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
