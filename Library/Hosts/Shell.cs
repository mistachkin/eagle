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
    /// <summary>
    /// This class is the abstract base class for interactive shell hosts.  It
    /// is derived from <see cref="Profile" /> and adds the support common to
    /// shell-style hosts, namely prompt generation (via the configured prompt
    /// variables), construction of the default window or console title from
    /// the package and assembly information, and reflection-based lookup and
    /// assignment of the named foreground and background colors used by the
    /// host.  Concrete hosts (for example, the console host) derive from this
    /// class to provide the actual interactive input and output.
    /// </summary>
    [ObjectId("7c8dc9cc-9e23-4a5d-a0c6-00fe61846e0d")]
    public abstract class Shell : Profile, IDisposable
    {
        #region Private Constants
        /// <summary>
        /// The default image runtime version string used when a more specific
        /// value is not available.
        /// </summary>
        private const string DefaultImageRuntimeVersion = "v4.0.30319";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The suffix appended to a color name to form the name of the property
        /// that holds the corresponding foreground color.
        /// </summary>
        private const string ForegroundColorSuffix = "ForegroundColor";
        /// <summary>
        /// The suffix appended to a color name to form the name of the property
        /// that holds the corresponding background color.
        /// </summary>
        private const string BackgroundColorSuffix = "BackgroundColor";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        /// <summary>
        /// Constructs a new instance of this host using the specified host
        /// data.
        /// </summary>
        /// <param name="hostData">
        /// The data used to initialize this host, including its name,
        /// associated interpreter, and creation flags.  This parameter may be
        /// null.
        /// </param>
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
        /// <summary>
        /// This method determines the name of the script variable that holds
        /// the prompt for the specified prompt type and flags.
        /// </summary>
        /// <param name="type">
        /// The type of prompt (for example, normal or continuation) whose
        /// variable name is to be returned.
        /// </param>
        /// <param name="flags">
        /// The flags that further select the prompt variable (for example,
        /// whether the debug or queue prompt is wanted).
        /// </param>
        /// <returns>
        /// The name of the script variable that holds the requested prompt.
        /// </returns>
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
        /// <summary>
        /// This method builds the default window or console title from the
        /// supplied package name and the version, release, date, configuration,
        /// and runtime information of the supplied assembly.
        /// </summary>
        /// <param name="packageName">
        /// The package name to include at the start of the title.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose version and related attributes are included in
        /// the title.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The constructed title string.
        /// </returns>
        protected virtual string BuildCoreTitle(
            string packageName,
            Assembly assembly
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            string release = FormatOps.ReleaseAttribute(
                SharedAttributeOps.GetAssemblyRelease(assembly));

            bool haveRelease = !String.IsNullOrEmpty(release);

            string text = RuntimeOps.GetAssemblyTextOrSuffix(assembly);

            string runtimeVersion = FormatOps.ShortImageOrRuntimeVersion(
                assembly, ShouldTreatAsMono(), ShouldTreatAsDotNetCore());

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

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        /// <summary>
        /// This method invalidates the cached host flags so that they will be
        /// recomputed the next time they are requested.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invalidates the cached host flags and then resets the
        /// remaining host flag state via the base class.
        /// </summary>
        /// <returns>
        /// True if the host flags were reset; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes and caches the flags describing the
        /// capabilities of this host, combining the shell-specific flags with
        /// those supplied by the base class.
        /// </summary>
        /// <returns>
        /// The flags describing the capabilities of this host.
        /// </returns>
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

        /// <summary>
        /// This method records whether the most recent read operation raised an
        /// exception and invalidates the cached host flags so that they reflect
        /// the change.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if the most recent read operation raised an exception.
        /// </param>
        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records whether the most recent write operation raised
        /// an exception and invalidates the cached host flags so that they
        /// reflect the change.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if the most recent write operation raised an exception.
        /// </param>
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
        /// <summary>
        /// This method displays the interactive prompt of the specified type.
        /// It first attempts to evaluate the configured prompt script; if no
        /// such script is configured (or it requests the default behavior), it
        /// falls back to writing the default prompt for the type.
        /// </summary>
        /// <param name="type">
        /// The type of prompt to display (for example, normal or
        /// continuation).
        /// </param>
        /// <param name="flags">
        /// On input, the flags controlling how the prompt is displayed; on
        /// return, this is updated to reflect what was done (for example,
        /// whether the prompt was completely or partially displayed).
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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
            // NOTE: Grab the count of the total interactive inputs for use
            //       in the prompt.
            //
            int count = localInterpreter.TotalInteractiveInputs;

            //
            // BUGFIX: Always bypass the interpreter readiness checks here;
            //         otherwise, we can get into very nasty situations (e.g.
            //         infinite recursion for [debug oncancel], etc).
            //
            ReturnCode code; /* IGNORED (?) */
            Result value = null;

            if (type != PromptType.None)
            {
                if (localInterpreter.GetVariableValue(
                        VariableFlags.ViaPrompt, GetPromptVariableName(
                        type, flags), ref value) == ReturnCode.Ok)
                {
                    Result result = null;
                    int errorLine = 0;

                    code = localInterpreter.EvaluatePromptScript(
                        value, ref result, ref errorLine);

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: The prompt script probably displayed some kind
                        //       of complete prompt; therefore, we are done.
                        //
                        flags |= PromptFlags.Done;
                    }
                    else if (code == ReturnCode.Return)
                    {
                        //
                        // NOTE: The prompt script probably displayed some kind
                        //       of partial prompt; therefore, we are done.
                        //
                        flags |= PromptFlags.Partial;
                    }
                    else if (code == ReturnCode.Continue)
                    {
                        //
                        // NOTE: Just do nothing.  This will result in the normal
                        //       shell prompt being displayed.
                        //
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
                        /* IGNORED */
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
                    // NOTE: There is no prompt script configured.  Use
                    //       the default (below).
                    //
                    code = ReturnCode.Ok;
                }
            }
            else
            {
                //
                // NOTE: Our caller requested a prompt type of "None"
                //       -OR- there is no prompt script configured.
                //       So far, this has been a complete success.
                //
                code = ReturnCode.Ok;
                flags |= PromptFlags.Done;
            }

            //
            // NOTE: If tracing is enabled, emit key information about the
            //       success -OR- failure to display the prompt.
            //
            if (FlagOps.HasFlags(flags, PromptFlags.Trace, true))
            {
                TraceOps.DebugTrace(String.Format(
                    "Prompt: code = {0}, flags = {1}, error = {2}",
                    code, FormatOps.WrapOrNull(flags),
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(Shell).Name, TracePriority.HostDebug);
            }

            //
            // NOTE: If we did not evaluate a prompt script -OR- if that script
            //       failed then we attempt to output the appropriate default
            //       prompt.
            //
            if (!FlagOps.HasFlags(flags, PromptFlags.Done, true))
            {
                //
                // NOTE: Now, we need to fallback to the default
                //       prompt.
                //
                string prompt = HostOps.GetDefaultPrompt(
                    localInterpreter, type, flags, id, count);

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

        /// <summary>
        /// Stores the cached flags describing the capabilities of this host, or
        /// <see cref="HostFlags.Invalid" /> when they have not yet been
        /// computed.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;
        /// <summary>
        /// This method returns the flags describing the capabilities of this
        /// host, computing them if necessary.
        /// </summary>
        /// <returns>
        /// The flags describing the capabilities of this host.
        /// </returns>
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        /// <summary>
        /// This method retrieves a named foreground and/or background color by
        /// reflecting over the color properties of this host.  Only the
        /// "default" theme (a null or empty theme name) is supported.
        /// </summary>
        /// <param name="theme">
        /// The name of the theme to query; only a null or empty value (the
        /// default theme) is supported.  This parameter is reserved.
        /// </param>
        /// <param name="name">
        /// The base name of the color to retrieve.  This parameter should not
        /// be null or empty.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to retrieve the foreground color associated with
        /// <paramref name="name" />.
        /// </param>
        /// <param name="background">
        /// Non-zero to retrieve the background color associated with
        /// <paramref name="name" />.
        /// </param>
        /// <param name="foregroundColor">
        /// Upon success, this contains the requested foreground color when
        /// <paramref name="foreground" /> is non-zero.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, this contains the requested background color when
        /// <paramref name="background" /> is non-zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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

        /// <summary>
        /// This method assigns a named foreground and/or background color by
        /// reflecting over the color properties of this host.  Only the
        /// "default" theme (a null or empty theme name) is supported.
        /// </summary>
        /// <param name="theme">
        /// The name of the theme to modify; only a null or empty value (the
        /// default theme) is supported.  This parameter is reserved.
        /// </param>
        /// <param name="name">
        /// The base name of the color to assign.  This parameter should not be
        /// null or empty.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to assign the foreground color associated with
        /// <paramref name="name" />.
        /// </param>
        /// <param name="background">
        /// Non-zero to assign the background color associated with
        /// <paramref name="name" />.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to assign when <paramref name="foreground" /> is
        /// non-zero.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to assign when <paramref name="background" /> is
        /// non-zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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
        /// <summary>
        /// Gets the default window or console title used by this host,
        /// constructing and caching it from the package name and assembly the
        /// first time it is requested.
        /// </summary>
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

        /// <summary>
        /// This method resets the configuration flags of this host to their
        /// default values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host to its initial state, also resetting
        /// its host flags.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this host has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this host has already been
        /// disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this host has been disposed and the engine is configured
        /// to throw on use of a disposed object.
        /// </exception>
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

        /// <summary>
        /// This method releases the resources held by this host.  It implements
        /// the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from <c>Dispose()</c>
        /// (i.e. deterministically); zero if it is being called from the
        /// finalizer.  When non-zero, managed resources are released.
        /// </param>
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
