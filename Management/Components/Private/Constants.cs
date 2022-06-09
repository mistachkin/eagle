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
    [ObjectId("dbace818-0369-4be6-90ff-6b4a0226dacc")]
    internal static class HelpMessage
    {
        public const string Text = "The string, expression, script, or file name to process.";
        public const string Args =
            "The list of 'command-line' arguments for the interpreter, if any.";
        public const string PreInitialize =
            "The script to evaluate during interpreter creation, if any.";
        public const string CreateFlags = "The flags for interpreter creation.";
        public const string HostCreateFlags = "The flags for interpreter host creation.";
        public const string InitializeFlags = "The flags for interpreter initialization.";
        public const string ScriptFlags = "The flags for script library behavior.";
        public const string InterpreterFlags = "The flags for interpreter behavior.";
        public const string EngineFlags = "The flags for modifying script engine behavior.";
        public const string SubstitutionFlags =
            "The flags for modifying string substitution behavior.";
        public const string EventFlags = "The flags for modifying event handling behavior.";
        public const string ExpressionFlags =
            "The flags for modifying string expression behavior.";
        public const string Unsafe =
            "Should be 'true' to allow 'unsafe' commands, 'false' otherwise.";
        public const string Standard =
            "Should be 'true' to allow only 'standard' commands, 'false' otherwise.";
        public const string Console =
            "Should be 'true' to allow console messages, 'false' otherwise.";
        public const string Force =
            "Should be 'true' to skip confirmation prompts, 'false' otherwise.";
        public const string Exceptions =
            "Should be 'true' to allow non-Ok return codes, 'false' otherwise.";
        public const string Policies =
            "Should be 'true' to use the 'cmdlet' command execution policies, 'false' otherwise.";
        public const string Deny =
            "Should be 'true' to deny command execution by default, 'false' otherwise.";
        public const string MetaCommand =
            "Should be 'true' to add the 'cmdlet' meta-command, 'false' otherwise.";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("d3b8a155-8881-45e6-8c30-5a8138979617")]
    internal static class Parameter
    {
        public const string Expression = "Expression";
        public const string Script = "Script";
        public const string String = "String";
        public const string File = "File";
        public const string FileName = "FileName";
        public const string PreInitialize = "PreInitialize";
        public const string CreateFlags = "CreateFlags";
        public const string HostCreateFlags = "HostCreateFlags";
        public const string InitializeFlags = "InitializeFlags";
        public const string ScriptFlags = "ScriptFlags";
        public const string InterpreterFlags = "InterpreterFlags";
        public const string EngineFlags = "EngineFlags";
        public const string SubstitutionFlags = "SubstitutionFlags";
        public const string EventFlags = "EventFlags";
        public const string Unsafe = "Unsafe";
        public const string Standard = "Standard";
        public const string Console = "Console";
        public const string Force = "Force";
        public const string Exceptions = "Exceptions";
        public const string Policies = "Policies";
        public const string Deny = "Deny";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public const string Evaluate = "Evaluate";
        public const string Substitute = "Substitute";
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("fdd326c3-0b3d-4d10-a641-c1da94f05da4")]
    internal static class Noun
    {
        private const string Prefix = "Eagle"; /* unique noun prefix */

        public const string Expression = Prefix + "Expression";
        public const string Text = Prefix + "Text";
        public const string Script = Prefix + "Script";
        public const string ScriptFile = Prefix + "ScriptFile";
        public const string TextFile = Prefix + "TextFile";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("1bad0a26-fd90-49e3-8299-4687928f8620")]
    internal static class Policy
    {
        public const string VerboseDescription = "Executing command: {0}";

        public static readonly string VerboseWarning =
            "Detected use of the command name \"{0}\", marked as 'unsafe', allow anyway?" +
            Environment.NewLine + "The full command is: {1}";

        public const string ProcessCaption = "Eagle Cmdlet Policy";
        public const string ContinueCaption = "Eagle Cmdlet Policy (Confirm)";

        public static readonly string Query = "{0}" + Environment.NewLine + "Are you really sure?";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("39c16643-46a1-4795-8629-b4562d8a0393")]
    internal static class Prefix
    {
        public const string CreateFlags = "Preparing to create interpreter, ";
        public const string HostCreateFlags = null;
        public const string InitializeFlags = null;
        public const string ScriptFlags = null;
        public const string InterpreterFlags = null;
        public const string EngineFlags = null;
        public const string SubstitutionFlags = null;
        public const string EventFlags = null;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("faf16de4-9310-4640-8f43-07f86da15471")]
    internal static class Verbose
    {
        public const string SafeMode = "Using 'safe' mode...";
        public const string UnsafeMode = "Using 'unsafe' mode...";
        public const string PoliciesEnabled =
            "Using 'cmdlet' and built-in command execution policies...";
        public const string PoliciesDisabled =
            "Using built-in command execution policies...";
        public const string PoliciesReset = "Policy settings reset.";
        public const string PreInitializeScript = "Pre-initialize script set to: {0}";
        public const string PreInitializeNone = "No pre-initialize script is set.";
        public const string InterpreterCreated = "Interpreter created.";
        public const string InterpreterSetup = "Interpreter setup.";
        public const string MetaCommandAdded = "Meta-command added.";
        public const string InterpreterDisposed = "Interpreter disposed.";
        public const string ProcessingStopped = "Processing has been stopped.";
        public const string Entered = "{0} entered.";
        public const string Exited = "{0} exited.";
        public const string PipelineStopping = "Pipeline is stopping, result is: {0}";
        public const string TraceListener = "Trace listener is: {0}";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("a01793d7-102a-40e0-aa58-6e5497095e39")]
    internal static class ErrorId
    {
        public const string CreateException = "CreateException";
        public const string DisposeException = "DisposeException";
        public const string CancelException = "CancelException";
        public const string CouldNotSetArguments = "CouldNotSetArguments";
        public const string CouldNotCreateInterpreter = "CouldNotCreateInterpreter";
        public const string CouldNotSetupInterpreter = "CouldNotSetupInterpreter";
        public const string CouldNotAddMetaCommand = "CouldNotAddMetaCommand";
        public const string AlreadyCreatedInterpreter = "AlreadyCreatedInterpreter";
        public const string CouldNotDisposeInterpreter = "CouldNotDisposeInterpreter";
        public const string CouldNotSetupTraceListeners = "CouldNotSetupTraceListeners";
        public const string InvalidInterpreter = "InvalidInterpreter";
        public const string DisposedInterpreter = "DisposedInterpreter";
        public const string InvalidScript = "InvalidScript";
        public const string ScriptError = "ScriptError";
        public const string CancelError = "CancelError";
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("c17aa344-0fa0-4794-a8e0-12230f2ae747")]
    internal static class SnapIn
    {
        public static readonly string Description = String.Format(
            "This PowerShell snap-in contains cmdlets to interact with " +
            "Eagle, the Tcl {0} compatible scripting language for the " +
            "Common Language Runtime (CLR).", Utility.GetTclVersion());

        public const string Name = "EagleCmdlets";
        public const string Vendor = "Eagle Development Team";
    }
}
