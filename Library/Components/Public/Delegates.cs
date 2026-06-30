/*
 * Delegates.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;

#if NETWORK
using System.Net;
#endif

using System.Reflection;

#if SHELL
using System.Text;
#endif

using System.Threading;

#if WINFORMS
using System.Windows.Forms;
#endif

using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using FormEventResultTriplet = Eagle._Interfaces.Public.IAnyTriplet<
    bool?, bool?, Eagle._Components.Public.ReturnCode?>;

namespace Eagle._Components.Public.Delegates
{
    #region Multi-Purpose Delegates
    //
    // NOTE: Used by the ListOps class when working with IEnumerable values
    //       in a generic fashion.
    //
    /// <summary>
    /// This delegate represents a method used to compare two values of the same
    /// type, for example, when ordering the elements of a generic enumerable.
    /// </summary>
    /// <param name="value1">
    /// The first value to be compared.
    /// </param>
    /// <param name="value2">
    /// The second value to be compared.
    /// </param>
    /// <returns>
    /// A negative number if <paramref name="value1" /> is less than <paramref
    /// name="value2" />, a positive number if it is greater, or zero if they
    /// are equal.
    /// </returns>
    [ObjectId("9141f8a2-d3a2-4e1a-8720-14c8f7100524")]
    public delegate int CompareCallback<T>(T value1, T value2);

    /// <summary>
    /// This delegate represents a method used to compute a hash code for a
    /// value of the specified type.
    /// </summary>
    /// <param name="value">
    /// The value for which a hash code is to be computed.
    /// </param>
    /// <returns>
    /// The hash code computed for the specified value.
    /// </returns>
    [ObjectId("8f0f2247-a852-452a-bc59-a948ccb45010")]
    public delegate int GetHashCodeCallback<T>(T value);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the NativeOps class for its (optional) keyboard event
    //       handling.
    //
    /// <summary>
    /// This delegate represents a method used to check whether a string value
    /// is acceptable, for example, during native keyboard event handling.
    /// </summary>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="value">
    /// The string value to be checked.
    /// </param>
    /// <param name="index">
    /// The position of the value within its containing sequence, if any.  May
    /// be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message that describes
    /// why the value was rejected.
    /// </param>
    /// <returns>
    /// True if the value is acceptable; otherwise, false.
    /// </returns>
    [ObjectId("a798bf76-f1ab-46c4-a7a6-8e0cea69e3ab")]
    public delegate bool CheckStringCallback(
        IClientData clientData,
        string value,
        int? index,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to determine whether a candidate
    /// string is unique with respect to an existing collection (and optional
    /// dictionary) of strings.
    /// </summary>
    /// <param name="collection">
    /// The collection of existing strings to check against.
    /// </param>
    /// <param name="dictionary">
    /// An optional dictionary of existing strings to check against.  May be
    /// null.
    /// </param>
    /// <param name="keyItem">
    /// The candidate string to be checked for uniqueness.
    /// </param>
    /// <returns>
    /// True if the candidate string is unique, false if it is not, or null if
    /// uniqueness could not be determined.
    /// </returns>
    [ObjectId("97a5de4e-da1b-4ef7-9adc-5285a3e3e221")]
    public delegate bool? UniqueStringCallback<TValue>(
        ICollection<string> collection,         /* in */
        IDictionary<string, TValue> dictionary, /* in: OPTIONAL */
        string keyItem                          /* in */
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to check whether a pending
    /// operation has been canceled.
    /// </summary>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// True if the operation has been canceled; otherwise, false.
    /// </returns>
    [ObjectId("8064c19a-300e-4383-92a3-98154391ae86")]
    public delegate bool CheckCancelCallback(
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // WARNING: The ISynchronizeAll is actually the interpreter;
    //          however, great care must be taken to avoid using
    //          anything in the interpreter class that requires
    //          the interpreter lock (obviously?).  If this rule
    //          is violated, deadlocks WILL result.
    //
    /// <summary>
    /// This delegate represents a method to be executed while holding a lock on
    /// the specified synchronization object.
    /// </summary>
    /// <param name="synchronizeAll">
    /// The object whose lock is held while the callback executes.
    /// </param>
    /// <param name="retry">
    /// The number of times acquisition of the lock should be retried.
    /// </param>
    /// <param name="timeout">
    /// The maximum number of milliseconds to wait when attempting to acquire
    /// the lock.
    /// </param>
    /// <param name="no">
    /// Upon return, this parameter is set to non-zero to indicate that the
    /// callback chose not to perform any work.
    /// </param>
    /// <returns>
    /// True if the callback succeeded; otherwise, false.
    /// </returns>
    [ObjectId("d388608e-5933-43a2-8f5c-2248ec12ea42")]
    public delegate bool LockCallback(
        ISynchronizeAll synchronizeAll,
        int retry,
        int timeout,
        ref bool no
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the CommandCallback class (i.e. instead of always using
    //       ThreadStart).
    //
    /// <summary>
    /// This delegate represents a generic method that accepts no arguments and
    /// returns no value, for use in place of a thread start routine.
    /// </summary>
    [ObjectId("03384bc0-ed43-4c2f-8fae-d93e826afd6c")]
    public delegate void GenericCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the CommandCallback class for use with DynamicInvoke.
    //
    /// <summary>
    /// This delegate represents a method that is invoked dynamically with a
    /// variable number of arguments.
    /// </summary>
    /// <param name="args">
    /// The arguments to be passed to the method being invoked.
    /// </param>
    /// <returns>
    /// The value returned by the dynamically invoked method, if any.
    /// </returns>
    [ObjectId("294a5eca-79bb-40cd-8454-c184140c559c")]
    public delegate object DynamicInvokeCallback(params object[] args);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by clients of the library for free() style native functions.
    //
    /// <summary>
    /// This delegate represents a free() style method used to release a block
    /// of native memory.
    /// </summary>
    /// <param name="data">
    /// A pointer to the native memory to be released.
    /// </param>
    [ObjectId("a39742e2-79ea-4ba1-84f1-f8f0df2b9aca")]
    public delegate void FreeCallback(IntPtr data);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read a single byte or character
    //       from a stream.
    //
    /// <summary>
    /// This delegate represents a method used to read a single byte or
    /// character from a stream.
    /// </summary>
    /// <returns>
    /// The byte or character that was read, or a negative number if the end of
    /// the stream has been reached.
    /// </returns>
    [ObjectId("6f1e7f9d-9c20-438f-9baf-a841d68ee12f")]
    public delegate int ReadInt32Callback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read a single byte from a stream.
    //
    /// <summary>
    /// This delegate represents a method used to read a single byte from a
    /// stream.
    /// </summary>
    /// <returns>
    /// The byte that was read from the stream.
    /// </returns>
    [ObjectId("d7dcdc56-0c87-4a02-998d-d20c521bcf32")]
    public delegate byte ReadByteCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read bytes from a stream.
    //
    /// <summary>
    /// This delegate represents a method used to read a sequence of bytes from
    /// a stream.
    /// </summary>
    /// <param name="buffer">
    /// The buffer into which the bytes are read.
    /// </param>
    /// <param name="index">
    /// The offset within the buffer at which to begin storing the bytes that
    /// were read.
    /// </param>
    /// <param name="count">
    /// The maximum number of bytes to read.
    /// </param>
    /// <returns>
    /// The number of bytes actually read.
    /// </returns>
    [ObjectId("eec1a2a2-0bf3-40a3-90ad-28db171607d6")]
    public delegate int ReadBytesCallback(byte[] buffer, int index, int count);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read characters from a stream.
    //
    /// <summary>
    /// This delegate represents a method used to read a sequence of characters
    /// from a stream.
    /// </summary>
    /// <param name="buffer">
    /// The buffer into which the characters are read.
    /// </param>
    /// <param name="index">
    /// The offset within the buffer at which to begin storing the characters
    /// that were read.
    /// </param>
    /// <param name="count">
    /// The maximum number of characters to read.
    /// </param>
    /// <returns>
    /// The number of characters actually read.
    /// </returns>
    [ObjectId("6e9e4d16-58c4-4889-9db7-5ddff78b8e6c")]
    public delegate int ReadCharsCallback(char[] buffer, int index, int count);
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Interpreter Health Related Delegates
#if THREADING
    /// <summary>
    /// This delegate represents a method used to evaluate the overall health of
    /// an interpreter.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter whose health is being evaluated.
    /// </param>
    /// <param name="status">
    /// Upon return, this parameter receives the resulting health status.
    /// </param>
    /// <param name="errors">
    /// Upon failure, this parameter receives the list of errors that were
    /// encountered.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("6fedb042-0a7e-4c83-9bb6-1351f9fd0c55")]
    public delegate ReturnCode HealthCallback(
        Interpreter interpreter, /* in */
        ref CheckStatus status,  /* in, out */
        ref ResultList errors    /* in, out */
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region RuleSet Related Delegates
    /// <summary>
    /// This delegate represents a method invoked for each rule encountered
    /// while iterating over a rule set.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the rule set.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="rule">
    /// The rule currently being processed.
    /// </param>
    /// <param name="stopOnError">
    /// Upon return, indicates whether iteration should stop if an error is
    /// encountered.
    /// </param>
    /// <param name="errors">
    /// Upon failure, this parameter receives the list of errors that were
    /// encountered.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("fe3c35d6-98ed-442a-bca1-a3427f891c91")]
    public delegate ReturnCode RuleIterationCallback(
        Interpreter interpreter,
        IClientData clientData,
        IRule rule,
        ref bool stopOnError,
        ref ResultList errors
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to determine whether a given rule
    /// matches the specified text.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the rule set.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="kind">
    /// The kind of identifier represented by the text, if any.  May be null.
    /// </param>
    /// <param name="mode">
    /// The matching mode to be used.
    /// </param>
    /// <param name="text">
    /// The text to be matched against the rule.
    /// </param>
    /// <param name="rule">
    /// The rule to be matched.
    /// </param>
    /// <param name="match">
    /// Upon return, indicates whether the rule matched the text.  May be null
    /// if this could not be determined.
    /// </param>
    /// <param name="errors">
    /// Upon failure, this parameter receives the list of errors that were
    /// encountered.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("d2049902-11ff-4bf7-8137-6e035084b260")]
    public delegate ReturnCode RuleMatchCallback(
        Interpreter interpreter,
        IClientData clientData,
        IdentifierKind? kind,
        MatchMode mode,
        string text,
        IRule rule,
        ref bool? match,
        ref ResultList errors
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Command Related Delegates
    /// <summary>
    /// This delegate represents a method invoked to resolve a command that
    /// could not otherwise be found.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter in which the command is being resolved.
    /// </param>
    /// <param name="engineFlags">
    /// The engine flags in effect for the current operation.
    /// </param>
    /// <param name="name">
    /// The name of the command being resolved.
    /// </param>
    /// <param name="arguments">
    /// The arguments associated with the command being resolved.
    /// </param>
    /// <param name="lookupFlags">
    /// The flags used to control command lookup.
    /// </param>
    /// <param name="ambiguous">
    /// Upon return, indicates whether the command name was ambiguous.
    /// </param>
    /// <param name="execute">
    /// Upon success, this parameter receives the resolved executable entity.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("382028fd-1c67-4e67-8448-1ae4262bc2d6")]
    public delegate ReturnCode UnknownCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        EngineFlags engineFlags,
        string name,
        ArgumentList arguments,
        LookupFlags lookupFlags,
        ref bool ambiguous,
        ref IExecute execute,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Package Related Delegates
    /// <summary>
    /// This delegate represents a method invoked to handle the loading or
    /// provision of a package.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter in which the package is being handled.
    /// </param>
    /// <param name="name">
    /// The name of the package.
    /// </param>
    /// <param name="version">
    /// The version of the package.
    /// </param>
    /// <param name="text">
    /// The script or other text associated with the package.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the package is handled.
    /// </param>
    /// <param name="exact">
    /// Non-zero if an exact version match is required.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("76847f0d-5f35-4107-a4d4-eb0814dc1d80")]
    public delegate ReturnCode PackageCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string name,
        Version version,
        string text,
        PackageFlags flags,
        bool exact,
        ref Result result
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Interactive Debugger / Shell Related Delegates
#if DEBUGGER
    //
    // NOTE: This is used by the Debugger class when it needs to break into
    //       the interactive loop.
    //
    /// <summary>
    /// This delegate represents a method used to enter the interactive loop,
    /// for example, when the debugger needs to break in.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the interactive loop.
    /// </param>
    /// <param name="loopData">
    /// The data that describes the interactive loop to be entered.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("5d85f38d-bc7b-4e30-9fa3-34eb64e3ada2")]
    public delegate ReturnCode InteractiveLoopCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IInteractiveLoopData loopData,
        ref Result result
    );
#endif

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to determine whether a string
    /// matches a pattern.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="mode">
    /// The matching mode to be used.
    /// </param>
    /// <param name="text">
    /// The text to be matched against the pattern.
    /// </param>
    /// <param name="pattern">
    /// The pattern to be matched against the text.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="match">
    /// Upon return, indicates whether the text matched the pattern.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("456d86a0-f092-423c-bddb-da8d19355789")]
    public delegate ReturnCode MatchCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        MatchMode mode,
        string text,
        string pattern,
        IClientData clientData,
        ref bool match,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

#if SHELL
    /// <summary>
    /// This delegate represents a method used to evaluate a script.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter in which the script is evaluated.
    /// </param>
    /// <param name="text">
    /// The script to be evaluated.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result of evaluation; upon
    /// failure, it receives an error message.
    /// </param>
    /// <param name="errorLine">
    /// Upon failure, this parameter receives the line number where the error
    /// occurred.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("3d8f4e8b-6bcb-4117-9cf8-9c0264c8e0d6")]
    public delegate ReturnCode EvaluateScriptCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string text,
        ref Result result,
        ref int errorLine
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to evaluate the script contained
    /// in a file.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter in which the script is evaluated.
    /// </param>
    /// <param name="fileName">
    /// The name of the file containing the script to be evaluated.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result of evaluation; upon
    /// failure, it receives an error message.
    /// </param>
    /// <param name="errorLine">
    /// Upon failure, this parameter receives the line number where the error
    /// occurred.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("1cfdbf5e-c6f6-4304-899c-f4d81fd08b2d")]
    public delegate ReturnCode EvaluateFileCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string fileName,
        ref Result result,
        ref int errorLine
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to evaluate the script contained
    /// in a file using a specified character encoding.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter in which the script is evaluated.
    /// </param>
    /// <param name="encoding">
    /// The character encoding used to read the file.
    /// </param>
    /// <param name="fileName">
    /// The name of the file containing the script to be evaluated.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result of evaluation; upon
    /// failure, it receives an error message.
    /// </param>
    /// <param name="errorLine">
    /// Upon failure, this parameter receives the line number where the error
    /// occurred.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("fdfcc53e-2241-4d7d-bf0b-b9f6aa5aef6c")]
    public delegate ReturnCode EvaluateEncodedFileCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        Encoding encoding,
        string fileName,
        ref Result result,
        ref int errorLine
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This is used by the ShellMainCore method when it encounters an
    //       argument, before performing any other processong on it.
    //
    /// <summary>
    /// This delegate represents a method invoked to preview a command line
    /// argument before it is otherwise processed by the shell.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="interactiveHost">
    /// The interactive host associated with the shell.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="phase">
    /// The phase of argument processing currently in effect.
    /// </param>
    /// <param name="whatIf">
    /// Non-zero if the argument should be examined without actually being acted
    /// upon.
    /// </param>
    /// <param name="index">
    /// Upon return, this parameter may be modified to change which argument is
    /// processed next.
    /// </param>
    /// <param name="arg">
    /// Upon return, this parameter may be modified to change the argument being
    /// processed.
    /// </param>
    /// <param name="argv">
    /// Upon return, this parameter may be modified to change the list of
    /// remaining arguments.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("9c6079bc-abc1-4daa-8c80-a026849693b7")]
    public delegate ReturnCode PreviewArgumentCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IInteractiveHost interactiveHost,
        IClientData clientData,
        ArgumentPhase phase,
        bool whatIf,
        ref int index,
        ref string arg,
        ref IList<string> argv,
        ref Result result
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This is used by the ShellMainCore method when it encounters an
    //       argument it cannot handle.
    //
    /// <summary>
    /// This delegate represents a method invoked when the shell encounters a
    /// command line argument it cannot otherwise handle.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="interactiveHost">
    /// The interactive host associated with the shell.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="switchCount">
    /// The number of switches that have been processed so far.
    /// </param>
    /// <param name="arg">
    /// The argument that could not be handled.
    /// </param>
    /// <param name="whatIf">
    /// Non-zero if the argument should be examined without actually being acted
    /// upon.
    /// </param>
    /// <param name="argv">
    /// Upon return, this parameter may be modified to change the list of
    /// remaining arguments.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("dad9e62e-eeeb-4043-b474-4a21955f369a")]
    public delegate ReturnCode UnknownArgumentCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IInteractiveHost interactiveHost,
        IClientData clientData,
        int switchCount,
        string arg,
        bool whatIf,
        ref IList<string> argv,
        ref Result result
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Native Stack Related Delegates
#if NATIVE && (WINDOWS || UNIX || UNSAFE)
    /// <summary>
    /// This delegate represents a method used to determine whether the calling
    /// thread is the main thread.
    /// </summary>
    /// <returns>
    /// True if the calling thread is the main thread; otherwise, false.
    /// </returns>
    [ObjectId("4c8e5255-5783-47de-9336-5e9fdabfc7b1")]
    public delegate bool NativeIsMainThreadCallback();

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to obtain information about the
    /// native stack.
    /// </summary>
    /// <returns>
    /// A value that describes the native stack, for example, the current native
    /// stack pointer.
    /// </returns>
    [ObjectId("c7c5173e-501a-4398-b824-5f57436b855f")]
    public delegate UIntPtr NativeStackCallback();
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Script Binder Related Delegates
    //
    // NOTE: Used by the script binder and marshaller to convert a string to
    //       a System.Type object.
    //
    /// <summary>
    /// This delegate represents a method used to convert a string into a
    /// System.Type object.
    /// </summary>
    /// <param name="typeName">
    /// The name of the type to be located.
    /// </param>
    /// <returns>
    /// The type identified by the specified name, or null if it could not be
    /// found.
    /// </returns>
    [ObjectId("bd71b549-ff0c-4a56-8557-5d84e5a1cd5a")]
    public delegate Type GetTypeCallback1(
        string typeName
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the script binder and marshaller to convert a string to
    //       a System.Type object.
    //
    /// <summary>
    /// This delegate represents a method used to convert a string into a
    /// System.Type object.
    /// </summary>
    /// <param name="typeName">
    /// The name of the type to be located.
    /// </param>
    /// <param name="throwOnError">
    /// Non-zero to throw an exception if the type cannot be found.
    /// </param>
    /// <param name="ignoreCase">
    /// Non-zero to ignore case when matching the type name.
    /// </param>
    /// <returns>
    /// The type identified by the specified name, or null if it could not be
    /// found.
    /// </returns>
    [ObjectId("95efe170-9c6d-4325-84fa-6e86c6b42449")]
    public delegate Type GetTypeCallback3(
        string typeName,
        bool throwOnError,
        bool ignoreCase
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the script binder to implement dynamic type conversions
    //       from string (always in the context of an interpreter).
    //
    /// <summary>
    /// This delegate represents a method used to dynamically convert a string
    /// into a value of the specified type.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="type">
    /// The type to which the string is to be converted.
    /// </param>
    /// <param name="text">
    /// The string to be converted.
    /// </param>
    /// <param name="options">
    /// The options that control the conversion.
    /// </param>
    /// <param name="cultureInfo">
    /// The culture used during the conversion.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="marshalFlags">
    /// The flags that control marshalling.  This parameter may be modified by
    /// the callback.
    /// </param>
    /// <param name="value">
    /// Upon success, this parameter receives the converted value.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("c9a205cc-d60f-4e67-b5ef-309aebf1b46c")]
    public delegate ReturnCode ChangeTypeCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        Type type,
        string text,
        OptionDictionary options,
        CultureInfo cultureInfo,
        IClientData clientData,
        ref MarshalFlags marshalFlags,
        ref object value,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to dynamically convert a value of
    /// the specified type into its string representation.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="type">
    /// The type of the value to be converted.
    /// </param>
    /// <param name="value">
    /// The value to be converted into a string.
    /// </param>
    /// <param name="options">
    /// The options that control the conversion.
    /// </param>
    /// <param name="cultureInfo">
    /// The culture used during the conversion.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="marshalFlags">
    /// The flags that control marshalling.  This parameter may be modified by
    /// the callback.
    /// </param>
    /// <param name="text">
    /// Upon success, this parameter receives the string representation of the
    /// value.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("817fb51d-ac00-411d-b921-ab9688e68a1b")]
    public delegate ReturnCode ToStringCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        Type type,
        object value,
        OptionDictionary options,
        CultureInfo cultureInfo,
        IClientData clientData,
        ref MarshalFlags marshalFlags,
        ref string text,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region String Handling Related Delegates
    //
    // NOTE: This is used by the PathOps class in order to obtain values for
    //       temporary file names.
    //
    /// <summary>
    /// This delegate represents a method used to obtain a string value, for
    /// example, when constructing a temporary file name.
    /// </summary>
    /// <returns>
    /// The string value that was obtained.
    /// </returns>
    [ObjectId("fdd759c2-0255-4415-bede-05a86b83d3ba")]
    public delegate string GetStringValueCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This is used by the StringList class to perform a transform on
    //       each element added to the newly created list.
    //
    /// <summary>
    /// This delegate represents a method used to transform a string value, for
    /// example, as each element is added to a newly created list.
    /// </summary>
    /// <param name="value">
    /// The string value to be transformed.
    /// </param>
    /// <returns>
    /// The transformed string value.
    /// </returns>
    [ObjectId("eecb9222-2372-4b29-bb03-de84545e5181")]
    public delegate string StringTransformCallback(
        string value
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This is used by the SyntaxOps class to invoke a callback on
    //       each row of data found in a (new-line?) delimited data file.
    //
    /// <summary>
    /// This delegate represents a method invoked for each row of data found in
    /// a delimited data file.
    /// </summary>
    /// <param name="metadata">
    /// The metadata that describes the columns of the row.
    /// </param>
    /// <param name="row">
    /// The values that make up the current row.
    /// </param>
    /// <param name="clientData">
    /// Opaque, caller-defined data.  This parameter may be modified by the
    /// callback.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// True if processing should continue; otherwise, false.
    /// </returns>
    [ObjectId("cf4b5f5e-8a02-4e97-9f86-07c1af996f0a")]
    public delegate bool StringDataRowCallback(
        IEnumerable<IPair<string>> metadata,
        IEnumerable<string> row,
        ref IClientData clientData,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region List Handling Related Delegates
    /// <summary>
    /// This delegate represents a method used to transform a list of strings.
    /// </summary>
    /// <param name="value">
    /// The list of strings to be transformed.
    /// </param>
    /// <returns>
    /// True if the transformation succeeded; otherwise, false.
    /// </returns>
    [ObjectId("915a62b8-78df-4d98-80d9-2d10d14c3756")]
    public delegate bool ListTransformCallback(
        IList<string> value
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to select a single element from a
    /// sequence of strings.
    /// </summary>
    /// <param name="value">
    /// The sequence of strings from which an element is to be selected.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <returns>
    /// The selected element, or null if no element was selected.
    /// </returns>
    [ObjectId("65d3d8e6-0b48-4666-a64b-63c4b30ec686")]
    public delegate string ElementSelectionCallback(
        IEnumerable<string> value,
        IClientData clientData
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region CallFrame Related Delegates
    /// <summary>
    /// This delegate represents a method invoked for a call frame.
    /// </summary>
    /// <param name="frame">
    /// The call frame being processed.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("bcbaf233-14eb-4cd5-9406-ee4ae5202730")]
    public delegate ReturnCode CallFrameCallback(
        ICallFrame frame,
        IClientData clientData,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Namespace Related Delegates
    /// <summary>
    /// This delegate represents a method invoked for a namespace.
    /// </summary>
    /// <param name="namespace">
    /// The namespace being processed.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("b88795f7-2538-4a2c-9bc8-5b04a2f0046e")]
    public delegate ReturnCode NamespaceCallback(
        INamespace @namespace,
        IClientData clientData,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Network Related Delegates
#if NETWORK
    /// <summary>
    /// This delegate represents a method used to create a new network client
    /// object.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="argument">
    /// The argument used to configure the new network client.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// The newly created network client object, or null upon failure.
    /// </returns>
    [ObjectId("bb10bfb2-6c28-4bde-8ec8-893139747d2e")]
    public delegate object NewNetworkClientCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string argument,
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method invoked prior to the creation of a web
    /// client, allowing its parameters to be adjusted.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="argument">
    /// Upon return, this parameter may be modified to change the argument used
    /// to create the web client.
    /// </param>
    /// <param name="clientData">
    /// Upon return, this parameter may be modified to change the client data
    /// associated with the web client.
    /// </param>
    /// <param name="timeout">
    /// Upon return, this parameter may be modified to change the timeout, in
    /// milliseconds.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("c4655c83-55b2-4de9-a2e2-6c208194aa8e")]
    public delegate ReturnCode PreWebClientCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        ref string argument,
        ref IClientData clientData,
        ref int? timeout,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to create a new web client
    /// object.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="argument">
    /// The argument used to configure the new web client.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// The newly created web client object, or null upon failure.
    /// </returns>
    [ObjectId("96141ff9-099d-46ca-ab6d-cf46e20dadcb")]
    public delegate WebClient NewWebClientCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string argument,
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method invoked to handle a web data transfer.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="webFlags">
    /// The flags that control the web transfer.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("b925a02b-d50b-4831-ae46-4161a2e4b5eb")]
    public delegate ReturnCode WebTransferCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        WebFlags webFlags,
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method invoked when an error occurs during a
    /// web data transfer.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="uri">
    /// The URI associated with the failed transfer.
    /// </param>
    /// <param name="webFlags">
    /// The flags that control the web transfer.
    /// </param>
    /// <param name="retries">
    /// The number of times the transfer has been retried.
    /// </param>
    /// <param name="timeout">
    /// The timeout, in milliseconds, in effect for the transfer.  May be null.
    /// </param>
    /// <param name="maximumRetries">
    /// The maximum number of times the transfer may be retried.  May be null.
    /// </param>
    /// <param name="result">
    /// Upon return, this parameter may receive a result that supersedes the
    /// error.
    /// </param>
    /// <param name="errors">
    /// Upon failure, this parameter receives the list of errors that were
    /// encountered.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("9a591097-65bb-49da-97bd-8a85c43e3dfe")]
    public delegate ReturnCode WebErrorCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IClientData clientData,
        Uri uri,
        WebFlags webFlags,
        int retries,
        int? timeout,
        int? maximumRetries,
        ref object result,
        ref ResultList errors
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Plugin Related Delegates
    /// <summary>
    /// This delegate represents a method used to create a new static plugin
    /// object.
    /// </summary>
    /// <returns>
    /// The newly created plugin object.
    /// </returns>
    [ObjectId("8e9a3010-dfa0-45cd-bc58-37103446dae3")]
    public delegate IPlugin NewStaticPluginCallback();
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Interpreter Host Related Delegates
    /// <summary>
    /// This delegate represents a method used to create a new interpreter host
    /// object.
    /// </summary>
    /// <param name="hostData">
    /// The data used to configure the new host.
    /// </param>
    /// <returns>
    /// The newly created host object.
    /// </returns>
    [ObjectId("890b5534-11fe-42ab-8549-fd823257115b")]
    public delegate IHost NewHostCallback(
        IHostData hostData
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method invoked to report an unhandled or
    /// otherwise notable error condition.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="id">
    /// The unique identifier associated with this complaint.
    /// </param>
    /// <param name="code">
    /// The return code associated with the error.
    /// </param>
    /// <param name="result">
    /// The result or error message associated with the error.
    /// </param>
    /// <param name="stackTrace">
    /// The stack trace associated with the error.
    /// </param>
    /// <param name="quiet">
    /// Non-zero if the error should be reported quietly.
    /// </param>
    /// <param name="retry">
    /// The number of times reporting has been retried.
    /// </param>
    /// <param name="levels">
    /// The number of active complaint nesting levels.
    /// </param>
    [ObjectId("81f1a091-95b5-4add-952c-ec12ccf67cbe")]
    public delegate void ComplainCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        long id,
        ReturnCode code,
        Result result,
        string stackTrace,
        bool quiet,
        int retry,
        int levels
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to filter trace messages before
    /// they are emitted.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="message">
    /// Upon return, this parameter may be modified to change the trace message.
    /// </param>
    /// <param name="category">
    /// Upon return, this parameter may be modified to change the trace
    /// category.
    /// </param>
    /// <param name="priority">
    /// Upon return, this parameter may be modified to change the trace
    /// priority.
    /// </param>
    /// <returns>
    /// True if the trace message should be emitted; otherwise, false.
    /// </returns>
    [ObjectId("b2e36d0b-7929-41a3-923e-44f0fc4094d8")]
    public delegate bool TraceFilterCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        ref string message,
        ref string category,
        ref TracePriority priority
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to write an end-of-line sequence
    /// to an output destination.
    /// </summary>
    [ObjectId("60472558-0049-40ba-9c08-5e3ac01d5acb")]
    public delegate void WriteLineCallback();

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to write a single character to an
    /// output destination.
    /// </summary>
    /// <param name="value">
    /// The character to be written.
    /// </param>
    [ObjectId("d8feecac-4278-4070-8819-4bd5feb4dc79")]
    public delegate void WriteCharCallback(
        char value
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to write a string to an output
    /// destination.
    /// </summary>
    /// <param name="value">
    /// The string to be written.
    /// </param>
    [ObjectId("bdf5896b-81ee-48c4-93a1-b8a8c58d9027")]
    public delegate void WriteStringCallback(
        string value
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Script Evaluation Related Delegates
    //
    // NOTE: Generic script evaluation callback (see also IAsynchronousContext
    //       interface).
    //
    /// <summary>
    /// This delegate represents a generic method used to perform asynchronous
    /// script evaluation.
    /// </summary>
    /// <param name="context">
    /// The context that describes the asynchronous operation.
    /// </param>
    [ObjectId("96804ca1-fad9-4e5e-beab-0e4c78c29044")]
    public delegate void AsynchronousCallback(
        IAsynchronousContext context
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Event Manager Related Delegates
    //
    // NOTE: This is used by the EventManager to provide a means of fetching
    //       the current DateTime (which may be "virtual").
    //
    /// <summary>
    /// This delegate represents a method used to obtain the current date and
    /// time, which may be virtualized.
    /// </summary>
    /// <returns>
    /// The current date and time.
    /// </returns>
    [ObjectId("a495ae22-6f78-483c-a66a-39f084c76021")]
    public delegate DateTime DateTimeNowCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This is used with one of the IEventManager.ListEvents method
    //       overloads to determine if an event should be included in its
    //       result set.
    //
    /// <summary>
    /// This delegate represents a method used to determine whether an event
    /// should be included in a result set.
    /// </summary>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="event">
    /// The event to be evaluated.
    /// </param>
    /// <param name="match">
    /// Upon return, indicates whether the event should be included.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("186aa540-9c5c-41a7-bb18-38d2cb52e58e")]
    public delegate ReturnCode EventMatchCallback(
        IClientData clientData,
        IEvent @event,
        ref bool match,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Generic interpreter asynchronous event callback.
    //
    /// <summary>
    /// This delegate represents a generic interpreter asynchronous event
    /// callback.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("8e5cab46-9887-49e4-8746-39330df59c34")]
    public delegate ReturnCode EventCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IClientData clientData,
        ref Result result
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: When set, for use by the Interpreter.Ready method.
    //
    /// <summary>
    /// This delegate represents a method invoked to determine whether an
    /// interpreter is ready to continue processing.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="timeout">
    /// The timeout, in milliseconds, associated with the readiness check.
    /// </param>
    /// <param name="flags">
    /// The flags that control the readiness check.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("de8424a3-1b5a-472d-b5df-cd12cb01915d")]
    public delegate ReturnCode ReadyCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IClientData clientData,
        int timeout,
        ReadyFlags flags,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: When set, for use by the ThreadOps.GetTimeout method.
    //
    // TODO: Change to use the IInterpreter type.
    //
    /// <summary>
    /// This delegate represents a method used to obtain the timeout value of
    /// the specified kind.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="timeoutType">
    /// The kind of timeout to be obtained.
    /// </param>
    /// <param name="timeout">
    /// Upon success, this parameter receives the timeout, in milliseconds.  May
    /// be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("241a0de7-2be7-4117-be1e-cc72eb5f46cb")]
    public delegate ReturnCode GetTimeoutCallback(
        Interpreter interpreter, /* in */
        TimeoutType timeoutType, /* in */
        ref int? timeout,        /* in, out */
        ref Result error         /* out */
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to wait on a set of events or to
    /// sleep for a specified period of time.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="events">
    /// The events to be waited upon.
    /// </param>
    /// <param name="milliseconds">
    /// The maximum number of milliseconds to wait.
    /// </param>
    /// <param name="eventWaitFlags">
    /// The flags that control how the events are waited upon.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("bcd7e02e-06e0-4549-8499-8b0f4275d3a0")]
    public delegate ReturnCode SleepWaitCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        EventWaitHandle[] events,
        int milliseconds,
        EventWaitFlags eventWaitFlags,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Native Tcl Integration Related Delegates
#if NATIVE && TCL && TCL_THREADS
    //
    // NOTE: Generic callback with a return code and result.
    //
    /// <summary>
    /// This delegate represents a generic callback that conveys a return code
    /// and result.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData1">
    /// The first piece of optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="clientData2">
    /// The second piece of optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="data">
    /// The request data associated with the callback, for example, an event.
    /// </param>
    /// <param name="code">
    /// The return code associated with the result.
    /// </param>
    /// <param name="result">
    /// The result associated with the operation.
    /// </param>
    /// <param name="errorLine">
    /// The line number where the error occurred, if any.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("bc8f729a-3c6e-45c8-acb0-f2548fd44299")]
    public delegate ReturnCode ResultCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IClientData clientData1,
        IClientData clientData2,
        object data, /* NOTE: Request data, e.g. IEvent, etc. */
        ReturnCode code,
        Result result,
        int errorLine,
        ref Result error
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Command Related Delegates
    /// <summary>
    /// This delegate represents a method used to create a new command object.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="name">
    /// The name of the command to be created.
    /// </param>
    /// <param name="plugin">
    /// The plugin that owns the command.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// The newly created command object, or null upon failure.
    /// </returns>
    [ObjectId("1f5888b2-e64c-4d72-b314-2f958e94f342")]
    public delegate ICommand NewCommandCallback(
        Interpreter interpreter,
        IClientData clientData,
        string name,
        IPlugin plugin,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Sub-Command Related Delegates
    /// <summary>
    /// This delegate represents a method used to derive the name for a
    /// delegate, for example, when adding sub-commands.
    /// </summary>
    /// <param name="delegates">
    /// The dictionary of existing delegates.
    /// </param>
    /// <param name="methodInfo">
    /// The method for which a delegate name is to be derived.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <returns>
    /// The name to be used for the delegate.
    /// </returns>
    [ObjectId("af2c6fd7-0af9-4d71-9f5d-41c1fdf38c19")]
    public delegate string NewDelegateNameCallback(
        DelegateDictionary delegates,
        MethodInfo methodInfo,
        IClientData clientData
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Command Execution Related Delegates
    //
    // NOTE: Interpreter command execution callback (see also the
    //       IExecute interface).
    //
    /// <summary>
    /// This delegate represents an interpreter command execution callback.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="arguments">
    /// The arguments to the command being executed.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("f566bc2c-242c-4a0b-a556-605f6b4b3833")]
    public delegate ReturnCode ExecuteCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IClientData clientData,
        ArgumentList arguments,
        ref Result result
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Interpreter function execution callback (see also the
    //       IExecuteArgument interface).
    //
    /// <summary>
    /// This delegate represents an interpreter function execution callback.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="arguments">
    /// The arguments to the function being executed.
    /// </param>
    /// <param name="value">
    /// Upon success, this parameter receives the resulting argument value.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("6ad94277-45c8-4e89-b709-60a51f31af6a")]
    public delegate ReturnCode ExecuteArgumentCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IClientData clientData,
        ArgumentList arguments,
        ref Argument value,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Procedure Related Delegates
    /// <summary>
    /// This delegate represents a method used to create a new procedure object.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="procedureData">
    /// The data used to configure the new procedure.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// The newly created procedure object, or null upon failure.
    /// </returns>
    [ObjectId("c208442b-e3eb-41af-9aac-10e67df801d3")]
    public delegate IProcedure NewProcedureCallback(
        Interpreter interpreter,
        IProcedureData procedureData,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Object Disposal Related Delegates
    //
    // NOTE: The interpreter disposal callback (also see the Dispose method
    //       of the Interpreter class).
    //
    /// <summary>
    /// This delegate represents a method invoked to dispose of an object.
    /// </summary>
    /// <param name="object">
    /// The object to be disposed.
    /// </param>
    [ObjectId("5bb5400d-c094-4eb5-93da-a8ecdff29b07")]
    public delegate void DisposeCallback(
        object @object
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Variable Tracing Related Delegates
    //
    // NOTE: Interpreter variable trace callback.
    //
    /// <summary>
    /// This delegate represents an interpreter variable trace callback.
    /// </summary>
    /// <param name="breakpointType">
    /// The kind of breakpoint that triggered the trace.
    /// </param>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="traceInfo">
    /// The information that describes the variable operation being traced.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("87929871-aaea-44b9-b6e1-fb6fdc9afaf1")]
    public delegate ReturnCode TraceCallback(
        BreakpointType breakpointType,
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        ITraceInfo traceInfo,
        ref Result result
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Notification Related Delegates
#if NOTIFY || NOTIFY_OBJECT
    //
    // NOTE: Interpreter notification callback (see also INotify interface).
    //
    /// <summary>
    /// This delegate represents an interpreter notification callback.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="eventArgs">
    /// The arguments that describe the notification.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="arguments">
    /// The arguments associated with the notification.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result; upon failure, it
    /// receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("2b4f0432-8412-4dde-afcc-8e556c37f5c2")]
    public delegate ReturnCode NotifyCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IScriptEventArgs eventArgs,
        IClientData clientData,
        ArgumentList arguments,
        ref Result result
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Windows Forms Related Delegates
#if WINFORMS
    /// <summary>
    /// This delegate represents a method used to report status text, for
    /// example, to a status bar.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter associated with the operation.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="text">
    /// The status text to be reported.
    /// </param>
    /// <param name="clear">
    /// Non-zero if any existing status text should be cleared.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code that
    /// indicates the type of failure.
    /// </returns>
    [ObjectId("79ecb423-d799-4cb8-a7e4-a47c1678afb7")]
    public delegate ReturnCode StatusCallback(
        Interpreter interpreter, /* in */
        IClientData clientData,  /* in: OPTIONAL */
        string text,             /* in */
        bool clear,              /* in */
        ref Result error         /* out */
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method invoked to handle a Windows Forms
    /// event.
    /// </summary>
    /// <param name="eventType">
    /// The kind of event being handled.
    /// </param>
    /// <param name="sender">
    /// The object that raised the event.
    /// </param>
    /// <param name="e">
    /// The arguments associated with the event.
    /// </param>
    /// <returns>
    /// A triplet that conveys the result of handling the event.
    /// </returns>
    [ObjectId("7d59df01-40af-4f08-949b-750c7603c727")]
    public delegate FormEventResultTriplet FormEventCallback(
        EventType eventType, /* in */
        object sender,       /* in */
        EventArgs e          /* in, out */
    );
#endif
    #endregion
}
