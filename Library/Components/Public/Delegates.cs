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
    [ObjectId("9141f8a2-d3a2-4e1a-8720-14c8f7100524")]
    public delegate int CompareCallback<T>(T value1, T value2);

    [ObjectId("8f0f2247-a852-452a-bc59-a948ccb45010")]
    public delegate int GetHashCodeCallback<T>(T value);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the CommandCallback class (i.e. instead of always using
    //       ThreadStart).
    //
    [ObjectId("03384bc0-ed43-4c2f-8fae-d93e826afd6c")]
    public delegate void GenericCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the CommandCallback class for use with DynamicInvoke.
    //
    [ObjectId("294a5eca-79bb-40cd-8454-c184140c559c")]
    public delegate object DynamicInvokeCallback(params object[] args);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by clients of the library for free() style native functions.
    //
    [ObjectId("a39742e2-79ea-4ba1-84f1-f8f0df2b9aca")]
    public delegate void FreeCallback(IntPtr data);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read a single byte or character
    //       from a stream.
    //
    [ObjectId("6f1e7f9d-9c20-438f-9baf-a841d68ee12f")]
    public delegate int ReadInt32Callback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read a single byte from a stream.
    //
    [ObjectId("d7dcdc56-0c87-4a02-998d-d20c521bcf32")]
    public delegate byte ReadByteCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read bytes from a stream.
    //
    [ObjectId("eec1a2a2-0bf3-40a3-90ad-28db171607d6")]
    public delegate int ReadBytesCallback(byte[] buffer, int index, int count);

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Engine class to read characters from a stream.
    //
    [ObjectId("6e9e4d16-58c4-4889-9db7-5ddff78b8e6c")]
    public delegate int ReadCharsCallback(char[] buffer, int index, int count);
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region RuleSet Related Delegates
    [ObjectId("fe3c35d6-98ed-442a-bca1-a3427f891c91")]
    public delegate ReturnCode RuleIterationCallback(
        Interpreter interpreter,
        IRule rule,
        ref bool stopOnError,
        ref ResultList errors
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("d2049902-11ff-4bf7-8137-6e035084b260")]
    public delegate ReturnCode RuleMatchCallback(
        Interpreter interpreter,
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
    [ObjectId("5d85f38d-bc7b-4e30-9fa3-34eb64e3ada2")]
    public delegate ReturnCode InteractiveLoopCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IInteractiveLoopData loopData,
        ref Result result
    );
#endif

    ///////////////////////////////////////////////////////////////////////////

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
    [ObjectId("3d8f4e8b-6bcb-4117-9cf8-9c0264c8e0d6")]
    public delegate ReturnCode EvaluateScriptCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string text,
        ref Result result,
        ref int errorLine
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("1cfdbf5e-c6f6-4304-899c-f4d81fd08b2d")]
    public delegate ReturnCode EvaluateFileCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string fileName,
        ref Result result,
        ref int errorLine
    );

    ///////////////////////////////////////////////////////////////////////////

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
    [ObjectId("dad9e62e-eeeb-4043-b474-4a21955f369a")]
    public delegate ReturnCode UnknownArgumentCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IInteractiveHost interactiveHost,
        IClientData clientData,
        int count,
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
    [ObjectId("4c8e5255-5783-47de-9336-5e9fdabfc7b1")]
    public delegate bool NativeIsMainThreadCallback();

    ///////////////////////////////////////////////////////////////////////////

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
    [ObjectId("bd71b549-ff0c-4a56-8557-5d84e5a1cd5a")]
    public delegate Type GetTypeCallback1(
        string typeName
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the script binder and marshaller to convert a string to
    //       a System.Type object.
    //
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
    [ObjectId("fdd759c2-0255-4415-bede-05a86b83d3ba")]
    public delegate string GetStringValueCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This is used by the StringList class to perform a transform on
    //       each element added to the newly created list.
    //
    [ObjectId("eecb9222-2372-4b29-bb03-de84545e5181")]
    public delegate string StringTransformCallback(
        string value
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region List Handling Related Delegates
    [ObjectId("915a62b8-78df-4d98-80d9-2d10d14c3756")]
    public delegate bool ListTransformCallback(
        IList<string> value
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("65d3d8e6-0b48-4666-a64b-63c4b30ec686")]
    public delegate string ElementSelectionCallback(
        IEnumerable<string> value,
        IClientData clientData
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region CallFrame Related Delegates
    [ObjectId("bcbaf233-14eb-4cd5-9406-ee4ae5202730")]
    public delegate ReturnCode CallFrameCallback(
        ICallFrame frame,
        IClientData clientData,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Namespace Related Delegates
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
    [ObjectId("bb10bfb2-6c28-4bde-8ec8-893139747d2e")]
    public delegate object NewNetworkClientCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string argument,
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("c4655c83-55b2-4de9-a2e2-6c208194aa8e")]
    public delegate ReturnCode PreWebClientCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string argument,
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("96141ff9-099d-46ca-ab6d-cf46e20dadcb")]
    public delegate WebClient NewWebClientCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string argument,
        IClientData clientData,
        ref Result error
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Plugin Related Delegates
    [ObjectId("8e9a3010-dfa0-45cd-bc58-37103446dae3")]
    public delegate IPlugin NewStaticPluginCallback();
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Interpreter Host Related Delegates
    [ObjectId("890b5534-11fe-42ab-8549-fd823257115b")]
    public delegate IHost NewHostCallback(
        IHostData hostData
    );

    ///////////////////////////////////////////////////////////////////////////

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

    [ObjectId("b2e36d0b-7929-41a3-923e-44f0fc4094d8")]
    public delegate bool TraceFilterCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string message,
        string category,
        TracePriority priority
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("60472558-0049-40ba-9c08-5e3ac01d5acb")]
    public delegate void WriteLineCallback();

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("d8feecac-4278-4070-8819-4bd5feb4dc79")]
    public delegate void WriteCharCallback(
        char value
    );

    ///////////////////////////////////////////////////////////////////////////

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
    [ObjectId("a495ae22-6f78-483c-a66a-39f084c76021")]
    public delegate DateTime DateTimeNowCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This is used with one of the IEventManager.ListEvents method
    //       overloads to determine if an event should be included in its
    //       result set.
    //
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
    [ObjectId("241a0de7-2be7-4117-be1e-cc72eb5f46cb")]
    public delegate ReturnCode GetTimeoutCallback(
        Interpreter interpreter, /* in */
        TimeoutType timeoutType, /* in */
        ref int? timeout,        /* in, out */
        ref Result error         /* out */
    );

    ///////////////////////////////////////////////////////////////////////////

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
    // NOTE: Interpreter command execution callback (see also IExecute
    //       interface).
    //
    [ObjectId("f566bc2c-242c-4a0b-a556-605f6b4b3833")]
    public delegate ReturnCode ExecuteCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        IClientData clientData,
        ArgumentList arguments,
        ref Result result
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Procedure Related Delegates
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
    [ObjectId("79ecb423-d799-4cb8-a7e4-a47c1678afb7")]
    public delegate ReturnCode StatusCallback(
        Interpreter interpreter, /* in */
        IClientData clientData,  /* in: OPTIONAL */
        string text,             /* in */
        bool clear,              /* in */
        ref Result error         /* out */
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("7d59df01-40af-4f08-949b-750c7603c727")]
    public delegate FormEventResultTriplet FormEventCallback(
        EventType eventType, /* in */
        object sender,       /* in */
        EventArgs e          /* in, out */
    );
#endif
    #endregion
}
