/*
 * Constants.cs --
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
using Eagle._Components.Public;

namespace Eagle._Constants
{
    /// <summary>
    /// This class contains the help message text used to describe the cmdlet
    /// parameters exposed by the Eagle PowerShell integration.
    /// </summary>
    [ObjectId("dbace818-0369-4be6-90ff-6b4a0226dacc")]
    internal static class HelpMessage
    {
        /// <summary>
        /// The help text for the input string, expression, script, or file
        /// name to process.
        /// </summary>
        public const string Text = "The string, expression, script, or file name to process.";

        /// <summary>
        /// The help text for the interpreter command-line arguments parameter.
        /// </summary>
        public const string Args =
            "The list of 'command-line' arguments for the interpreter, if any.";

        /// <summary>
        /// The help text for the pre-initialization script parameter.
        /// </summary>
        public const string PreInitialize =
            "The script to evaluate during interpreter creation, if any.";

        /// <summary>
        /// The help text for the interpreter creation flags parameter.
        /// </summary>
        public const string CreateFlags = "The flags for interpreter creation.";

        /// <summary>
        /// The help text for the interpreter host creation flags parameter.
        /// </summary>
        public const string HostCreateFlags = "The flags for interpreter host creation.";

        /// <summary>
        /// The help text for the interpreter initialization flags parameter.
        /// </summary>
        public const string InitializeFlags = "The flags for interpreter initialization.";

        /// <summary>
        /// The help text for the script library behavior flags parameter.
        /// </summary>
        public const string ScriptFlags = "The flags for script library behavior.";

        /// <summary>
        /// The help text for the interpreter behavior flags parameter.
        /// </summary>
        public const string InterpreterFlags = "The flags for interpreter behavior.";

        /// <summary>
        /// The help text for the script engine behavior flags parameter.
        /// </summary>
        public const string EngineFlags = "The flags for modifying script engine behavior.";

        /// <summary>
        /// The help text for the string substitution behavior flags parameter.
        /// </summary>
        public const string SubstitutionFlags =
            "The flags for modifying string substitution behavior.";

        /// <summary>
        /// The help text for the event handling behavior flags parameter.
        /// </summary>
        public const string EventFlags = "The flags for modifying event handling behavior.";

        /// <summary>
        /// The help text for the string expression behavior flags parameter.
        /// </summary>
        public const string ExpressionFlags =
            "The flags for modifying string expression behavior.";

        /// <summary>
        /// The help text for the parameter controlling whether unsafe commands
        /// are allowed.
        /// </summary>
        public const string Unsafe =
            "Should be 'true' to allow 'unsafe' commands, 'false' otherwise.";

        /// <summary>
        /// The help text for the parameter controlling whether only standard
        /// commands are allowed.
        /// </summary>
        public const string Standard =
            "Should be 'true' to allow only 'standard' commands, 'false' otherwise.";

        /// <summary>
        /// The help text for the parameter controlling whether console
        /// messages are allowed.
        /// </summary>
        public const string Console =
            "Should be 'true' to allow console messages, 'false' otherwise.";

        /// <summary>
        /// The help text for the parameter controlling whether confirmation
        /// prompts are skipped.
        /// </summary>
        public const string Force =
            "Should be 'true' to skip confirmation prompts, 'false' otherwise.";

        /// <summary>
        /// The help text for the parameter controlling whether non-Ok return
        /// codes are allowed.
        /// </summary>
        public const string Exceptions =
            "Should be 'true' to allow non-Ok return codes, 'false' otherwise.";

        /// <summary>
        /// The help text for the parameter controlling whether the cmdlet
        /// command execution policies are used.
        /// </summary>
        public const string Policies =
            "Should be 'true' to use the 'cmdlet' command execution policies, 'false' otherwise.";

        /// <summary>
        /// The help text for the parameter controlling whether command
        /// execution is denied by default.
        /// </summary>
        public const string Deny =
            "Should be 'true' to deny command execution by default, 'false' otherwise.";

        /// <summary>
        /// The help text for the parameter controlling whether the cmdlet
        /// meta-command is added.
        /// </summary>
        public const string MetaCommand =
            "Should be 'true' to add the 'cmdlet' meta-command, 'false' otherwise.";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the canonical names of the parameters exposed by
    /// the Eagle PowerShell cmdlets.
    /// </summary>
    [ObjectId("d3b8a155-8881-45e6-8c30-5a8138979617")]
    internal static class Parameter
    {
        /// <summary>
        /// The name of the expression parameter.
        /// </summary>
        public const string Expression = "Expression";

        /// <summary>
        /// The name of the script parameter.
        /// </summary>
        public const string Script = "Script";

        /// <summary>
        /// The name of the string parameter.
        /// </summary>
        public const string String = "String";

        /// <summary>
        /// The name of the file parameter.
        /// </summary>
        public const string File = "File";

        /// <summary>
        /// The name of the file name parameter.
        /// </summary>
        public const string FileName = "FileName";

        /// <summary>
        /// The name of the pre-initialization script parameter.
        /// </summary>
        public const string PreInitialize = "PreInitialize";

        /// <summary>
        /// The name of the interpreter creation flags parameter.
        /// </summary>
        public const string CreateFlags = "CreateFlags";

        /// <summary>
        /// The name of the interpreter host creation flags parameter.
        /// </summary>
        public const string HostCreateFlags = "HostCreateFlags";

        /// <summary>
        /// The name of the interpreter initialization flags parameter.
        /// </summary>
        public const string InitializeFlags = "InitializeFlags";

        /// <summary>
        /// The name of the script library behavior flags parameter.
        /// </summary>
        public const string ScriptFlags = "ScriptFlags";

        /// <summary>
        /// The name of the interpreter behavior flags parameter.
        /// </summary>
        public const string InterpreterFlags = "InterpreterFlags";

        /// <summary>
        /// The name of the script engine behavior flags parameter.
        /// </summary>
        public const string EngineFlags = "EngineFlags";

        /// <summary>
        /// The name of the string substitution behavior flags parameter.
        /// </summary>
        public const string SubstitutionFlags = "SubstitutionFlags";

        /// <summary>
        /// The name of the event handling behavior flags parameter.
        /// </summary>
        public const string EventFlags = "EventFlags";

        /// <summary>
        /// The name of the parameter controlling whether unsafe commands are
        /// allowed.
        /// </summary>
        public const string Unsafe = "Unsafe";

        /// <summary>
        /// The name of the parameter controlling whether only standard
        /// commands are allowed.
        /// </summary>
        public const string Standard = "Standard";

        /// <summary>
        /// The name of the parameter controlling whether console messages are
        /// allowed.
        /// </summary>
        public const string Console = "Console";

        /// <summary>
        /// The name of the parameter controlling whether confirmation prompts
        /// are skipped.
        /// </summary>
        public const string Force = "Force";

        /// <summary>
        /// The name of the parameter controlling whether non-Ok return codes
        /// are allowed.
        /// </summary>
        public const string Exceptions = "Exceptions";

        /// <summary>
        /// The name of the parameter controlling whether the cmdlet command
        /// execution policies are used.
        /// </summary>
        public const string Policies = "Policies";

        /// <summary>
        /// The name of the parameter controlling whether command execution is
        /// denied by default.
        /// </summary>
        public const string Deny = "Deny";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the PowerShell verb names used by the Eagle cmdlets.
    /// </summary>
    [ObjectId("b367c4dc-8226-4368-aac2-6535fe1f982b")]
    internal static class Verb
    {
        //
        // NOTE: If you define the APPROVED_VERBS compile-time constant,
        //       we will use the "Invoke" and "Resolve" verbs from the
        //       list of "approved verbs" for PowerShell.  Otherwise,
        //       we will use the "Evaluate" and "Substitute" verbs,
        //       which are more consistent with the terminology of the
        //       Tcl and Eagle scripting languages themselves.  Judging
        //       from the list of "approved verbs" for PowerShell, it
        //       would seem that the PowerShell team did not really
        //       anticipate people integrating other complete scripting
        //       languages with their product via cmdlets (probably
        //       because PowerShell is a powerful scripting language in
        //       its own right).  Also, see CodePlex bug #8009.
        //
#if APPROVED_VERBS
        //
        // NOTE: This verb is used for the cmdlets that evaluate a
        //       script (or file) containing zero or more commands.
        //       This verb seems to fit fairly well and it is available
        //       in PowerShell 1.0 and higher.
        //
        //       From MSDN:
        //
        //       VerbsLifecycle: Defines the lifecycle verbs, such as
        //                       Enable, Disable, Start, and Stop, that
        //                       can be used to name cmdlets.
        //
        //       Invoke: Performs an action, such as running a command
        //               or a method.
        //
        /// <summary>
        /// The verb used by the cmdlets that evaluate a script (or file).
        /// </summary>
        public const string Evaluate = "Invoke";

        //
        // NOTE: This verb is used for the cmdlets that process all the
        //       variable, command, and backslash substitutions
        //       contained within a string of text (or file).  Strictly
        //       speaking, this verb does not fit very well.
        //       Unfortunately, it is the best choice we have from the
        //       list of "approved verbs" for PowerShell 1.0.  The
        //       "Format" verb might be a better choice here; however,
        //       it is not available in PowerShell 1.0 (i.e. it is new
        //       to PowerShell 2.0).
        //
        //       From MSDN:
        //
        //       VerbsDiagnostic: Defines the diagnostic verb names
        //                        that can be used to specify the
        //                        action of a cmdlet, such as Debug,
        //                        Ping, and Trace.
        //
        //       Resolve: Maps a shorthand representation of a resource
        //                to a more complete representation.
        //
        /// <summary>
        /// The verb used by the cmdlets that perform string substitution on a
        /// string of text (or file).
        /// </summary>
        public const string Substitute = "Resolve";
#else
        //
        // NOTE: For those people familiar with Tcl and/or Eagle,
        //       these verbs are self-explanatory as they correspond
        //       exactly with the [eval] and [subst] commands available
        //       in those languages.  Since the sole purpose of these
        //       cmdlets is to integrate with Eagle and/or Tcl (i.e.
        //       they are not "general-purpose" cmdlets), consistency
        //       with those languages trumps consistency with the list
        //       of "approved verbs" for PowerShell itself.  Also, see
        //       CodePlex bug #8009.
        //
        /// <summary>
        /// The verb used by the cmdlets that evaluate a script (or file).
        /// </summary>
        public const string Evaluate = "Evaluate";

        /// <summary>
        /// The verb used by the cmdlets that perform string substitution on a
        /// string of text (or file).
        /// </summary>
        public const string Substitute = "Substitute";
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the PowerShell noun names used by the Eagle cmdlets.
    /// </summary>
    [ObjectId("fdd326c3-0b3d-4d10-a641-c1da94f05da4")]
    internal static class Noun
    {
        /// <summary>
        /// The prefix prepended to each noun to keep it unique.
        /// </summary>
        private const string Prefix = "Eagle"; /* unique noun prefix */

        /// <summary>
        /// The noun used for the cmdlet that evaluates an expression.
        /// </summary>
        public const string Expression = Prefix + "Expression";

        /// <summary>
        /// The noun used for the cmdlet that processes a string of text.
        /// </summary>
        public const string Text = Prefix + "Text";

        /// <summary>
        /// The noun used for the cmdlet that evaluates a script.
        /// </summary>
        public const string Script = Prefix + "Script";

        /// <summary>
        /// The noun used for the cmdlet that evaluates a script file.
        /// </summary>
        public const string ScriptFile = Prefix + "ScriptFile";

        /// <summary>
        /// The noun used for the cmdlet that processes a text file.
        /// </summary>
        public const string TextFile = Prefix + "TextFile";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the message and caption text used by the Eagle
    /// cmdlet command execution policy.
    /// </summary>
    [ObjectId("1bad0a26-fd90-49e3-8299-4687928f8620")]
    internal static class Policy
    {
        /// <summary>
        /// The verbose description format string emitted when a command is
        /// executed.
        /// </summary>
        public const string VerboseDescription = "Executing command: {0}";

        /// <summary>
        /// The verbose warning format string emitted when a command marked as
        /// unsafe is about to be executed.
        /// </summary>
        public static readonly string VerboseWarning =
            "Detected use of the command name \"{0}\", marked as 'unsafe', allow anyway?" +
            Environment.NewLine + "The full command is: {1}";

        /// <summary>
        /// The caption text used when prompting about command execution.
        /// </summary>
        public const string ProcessCaption = "Eagle Cmdlet Policy";

        /// <summary>
        /// The caption text used when confirming command execution.
        /// </summary>
        public const string ContinueCaption = "Eagle Cmdlet Policy (Confirm)";

        /// <summary>
        /// The confirmation query format string presented to the user.
        /// </summary>
        public static readonly string Query = "{0}" + Environment.NewLine + "Are you really sure?";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the message prefixes used when emitting verbose
    /// diagnostics for the various interpreter flag parameters.
    /// </summary>
    [ObjectId("39c16643-46a1-4795-8629-b4562d8a0393")]
    internal static class Prefix
    {
        /// <summary>
        /// The message prefix used for the interpreter creation flags.
        /// </summary>
        public const string CreateFlags = "Preparing to create interpreter, ";

        /// <summary>
        /// The message prefix used for the interpreter host creation flags.
        /// </summary>
        public const string HostCreateFlags = null;

        /// <summary>
        /// The message prefix used for the interpreter initialization flags.
        /// </summary>
        public const string InitializeFlags = null;

        /// <summary>
        /// The message prefix used for the script library behavior flags.
        /// </summary>
        public const string ScriptFlags = null;

        /// <summary>
        /// The message prefix used for the interpreter behavior flags.
        /// </summary>
        public const string InterpreterFlags = null;

        /// <summary>
        /// The message prefix used for the script engine behavior flags.
        /// </summary>
        public const string EngineFlags = null;

        /// <summary>
        /// The message prefix used for the string substitution behavior flags.
        /// </summary>
        public const string SubstitutionFlags = null;

        /// <summary>
        /// The message prefix used for the event handling behavior flags.
        /// </summary>
        public const string EventFlags = null;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the verbose diagnostic message text emitted by the
    /// Eagle cmdlets.
    /// </summary>
    [ObjectId("faf16de4-9310-4640-8f43-07f86da15471")]
    internal static class Verbose
    {
        /// <summary>
        /// The message emitted when the interpreter is using safe mode.
        /// </summary>
        public const string SafeMode = "Using 'safe' mode...";

        /// <summary>
        /// The message emitted when the interpreter is using unsafe mode.
        /// </summary>
        public const string UnsafeMode = "Using 'unsafe' mode...";

        /// <summary>
        /// The message emitted when both the cmdlet and built-in command
        /// execution policies are enabled.
        /// </summary>
        public const string PoliciesEnabled =
            "Using 'cmdlet' and built-in command execution policies...";

        /// <summary>
        /// The message emitted when only the built-in command execution
        /// policies are enabled.
        /// </summary>
        public const string PoliciesDisabled =
            "Using built-in command execution policies...";

        /// <summary>
        /// The message emitted when the policy settings have been reset.
        /// </summary>
        public const string PoliciesReset = "Policy settings reset.";

        /// <summary>
        /// The message format string emitted when a pre-initialization script
        /// has been set.
        /// </summary>
        public const string PreInitializeScript = "Pre-initialize script set to: {0}";

        /// <summary>
        /// The message emitted when no pre-initialization script has been set.
        /// </summary>
        public const string PreInitializeNone = "No pre-initialize script is set.";

        /// <summary>
        /// The message emitted when the interpreter has been created.
        /// </summary>
        public const string InterpreterCreated = "Interpreter created.";

        /// <summary>
        /// The message emitted when the interpreter has been set up.
        /// </summary>
        public const string InterpreterSetup = "Interpreter setup.";

        /// <summary>
        /// The message emitted when the meta-command has been added.
        /// </summary>
        public const string MetaCommandAdded = "Meta-command added.";

        /// <summary>
        /// The message emitted when the interpreter has been disposed.
        /// </summary>
        public const string InterpreterDisposed = "Interpreter disposed.";

        /// <summary>
        /// The message emitted when processing has been stopped.
        /// </summary>
        public const string ProcessingStopped = "Processing has been stopped.";

        /// <summary>
        /// The message format string emitted when a processing stage is
        /// entered.
        /// </summary>
        public const string Entered = "{0} entered.";

        /// <summary>
        /// The message format string emitted when a processing stage is
        /// exited.
        /// </summary>
        public const string Exited = "{0} exited.";

        /// <summary>
        /// The message format string emitted when the pipeline is stopping.
        /// </summary>
        public const string PipelineStopping = "Pipeline is stopping, result is: {0}";

        /// <summary>
        /// The message format string emitted to report the active trace
        /// listener.
        /// </summary>
        public const string TraceListener = "Trace listener is: {0}";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the error identifier strings used when reporting
    /// errors from the Eagle cmdlets.
    /// </summary>
    [ObjectId("a01793d7-102a-40e0-aa58-6e5497095e39")]
    internal static class ErrorId
    {
        /// <summary>
        /// The error identifier used when an exception is thrown while creating
        /// the interpreter.
        /// </summary>
        public const string CreateException = "CreateException";

        /// <summary>
        /// The error identifier used when an exception is thrown while
        /// disposing the interpreter.
        /// </summary>
        public const string DisposeException = "DisposeException";

        /// <summary>
        /// The error identifier used when an exception is thrown while
        /// cancelling.
        /// </summary>
        public const string CancelException = "CancelException";

        /// <summary>
        /// The error identifier used when the interpreter arguments could not
        /// be set.
        /// </summary>
        public const string CouldNotSetArguments = "CouldNotSetArguments";

        /// <summary>
        /// The error identifier used when the interpreter could not be created.
        /// </summary>
        public const string CouldNotCreateInterpreter = "CouldNotCreateInterpreter";

        /// <summary>
        /// The error identifier used when the interpreter could not be set up.
        /// </summary>
        public const string CouldNotSetupInterpreter = "CouldNotSetupInterpreter";

        /// <summary>
        /// The error identifier used when the meta-command could not be added.
        /// </summary>
        public const string CouldNotAddMetaCommand = "CouldNotAddMetaCommand";

        /// <summary>
        /// The error identifier used when the interpreter has already been
        /// created.
        /// </summary>
        public const string AlreadyCreatedInterpreter = "AlreadyCreatedInterpreter";

        /// <summary>
        /// The error identifier used when the interpreter could not be
        /// disposed.
        /// </summary>
        public const string CouldNotDisposeInterpreter = "CouldNotDisposeInterpreter";

        /// <summary>
        /// The error identifier used when the trace listeners could not be set
        /// up.
        /// </summary>
        public const string CouldNotSetupTraceListeners = "CouldNotSetupTraceListeners";

        /// <summary>
        /// The error identifier used when the interpreter is invalid.
        /// </summary>
        public const string InvalidInterpreter = "InvalidInterpreter";

        /// <summary>
        /// The error identifier used when the interpreter has been disposed.
        /// </summary>
        public const string DisposedInterpreter = "DisposedInterpreter";

        /// <summary>
        /// The error identifier used when the script is invalid.
        /// </summary>
        public const string InvalidScript = "InvalidScript";

        /// <summary>
        /// The error identifier used when a script error occurs.
        /// </summary>
        public const string ScriptError = "ScriptError";

        /// <summary>
        /// The error identifier used when a cancellation error occurs.
        /// </summary>
        public const string CancelError = "CancelError";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the metadata describing the Eagle PowerShell
    /// snap-in.
    /// </summary>
    [ObjectId("c17aa344-0fa0-4794-a8e0-12230f2ae747")]
    internal static class SnapIn
    {
        /// <summary>
        /// The human-readable description of the snap-in.
        /// </summary>
        public static readonly string Description = String.Format(
            "This PowerShell snap-in contains cmdlets to interact with " +
            "Eagle, the Tcl {0} compatible scripting language for the " +
            "Common Language Runtime (CLR).", Utility.GetTclVersion());

        /// <summary>
        /// The name of the snap-in.
        /// </summary>
        public const string Name = "EagleCmdlets";

        /// <summary>
        /// The vendor of the snap-in.
        /// </summary>
        public const string Vendor = "Eagle Development Team";
    }
}
