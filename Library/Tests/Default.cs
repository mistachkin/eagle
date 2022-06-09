/*
 * Default.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;

#if DATA
using System.Data;
#endif

using System.Diagnostics;
using System.Globalization;
using System.IO;

#if NET_35 || NET_40 || NET_STANDARD_20
using System.IO.Pipes;
#endif

#if NETWORK
using System.Net;
#endif

using System.Reflection;
using System.Runtime.InteropServices;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

#if REMOTING
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
#endif

#if CAS_POLICY
using System.Security.Policy;
#endif

#if SERIALIZATION
using System.Security.Permissions;
#endif

#if NETWORK && REMOTING
using System.Security.Principal;
#endif

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#if WINFORMS
using System.Windows.Forms;
#endif

#if XML
using System.Xml;
#endif

using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

using SecurityProtocolType = Eagle._Components.Public.SecurityProtocolType;
using _RuntimeOps = Eagle._Components.Private.RuntimeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _Public = Eagle._Components.Public;

using MessageCountDictionary = System.Collections.Generic.Dictionary<string, long>;
using IsolationLevel = System.Data.IsolationLevel;
using CommandTriplet = Eagle._Components.Public.MutableAnyTriplet<string, System.Type, long>;

using TimeoutTriplet = Eagle._Components.Public.AnyTriplet<
    Eagle._Components.Private.ScriptTimeoutClientData, System.Threading.EventWaitHandle, long>;

#if XML
using XmlGetAttributeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Private.Delegates.XmlGetAttributeCallback>;

using XmlSetAttributeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Private.Delegates.XmlSetAttributeCallback>;
#endif

using NameValueCollection = System.Collections.Specialized.NameValueCollection;

using CommandDataPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.ICommandData>;

using CommandDataDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.ICommandData>;

using FunctionDataPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IFunctionData>;

using FunctionDataDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IFunctionData>;

using OperatorDataPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Private.IOperatorData>;

using OperatorDataDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Private.IOperatorData>;

#if NETWORK
using Int32ObjectDictionary = System.Collections.Generic.Dictionary<int, object>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Tests
{
    [ObjectId("e1257294-a012-4164-b0ea-3763dd06eec2")]
    public class Default : IMaybeDisposed, IDisposable
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        private static string PkgInstallLogCommandName = "pkgInstallLog"; /* COMPAT: Eagle. */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static Regex GoodPkgNameRegEx = RegExOps.Create(
            "^[A-Z][0-9A-Z\\._]*$", RegexOptions.IgnoreCase);

        private static Regex BadPkgNameRegEx = RegExOps.Create(
            "\\.\\.|\\.$");

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if REMOTING
        private static readonly string RemotingChannelName = String.Empty;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string TestCustomInfoBoxName = "TestCustomInfo";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string ObjectWrongNumArgs =
            "wrong # args: should be \"object ?options? member ?arg ...?\"";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
#if DEBUGGER
        private static string DebugEmergencyScript = "debug emergency now";
        private static string DebugBreakScript = "debug break";
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        private static readonly string EmptyArgument = "##empty##";
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK
        //
        // HACK: These are purposely not read-only.
        //
        private static SecurityProtocolType? SavedSecurityProtocol = null;
        private static SecurityProtocolType? BestSecurityProtocol = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Enumerations
        [Flags()]
        [ObjectId("c3e9879b-bc21-4b6f-9d53-0c6928841431")]
        public enum TestEnumSByte : sbyte
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = unchecked((sbyte)0x80)
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Flags()]
        [ObjectId("e85efd15-5813-41e2-b1e5-e1dc799c572b")]
        public enum TestEnumByte : byte
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = 0x80
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Flags()]
        [ObjectId("a5c7d144-fb22-4a37-95c5-b2e130f2827b")]
        public enum TestEnumInt16 : short
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = 0x80,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Nine = 0x100,
            Ten = 0x200,
            Eleven = 0x400,
            Twelve = 0x800,
            Thirteen = 0x1000,
            Fourteen = 0x2000,
            Fifteen = 0x4000,
            Sixteen = unchecked((short)0x8000)
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Flags()]
        [ObjectId("e58f5280-47c6-455f-aff9-60c39c27ae5e")]
        public enum TestEnumUInt16 : ushort
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = 0x80,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Nine = 0x100,
            Ten = 0x200,
            Eleven = 0x400,
            Twelve = 0x800,
            Thirteen = 0x1000,
            Fourteen = 0x2000,
            Fifteen = 0x4000,
            Sixteen = 0x8000
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Flags()]
        [ObjectId("bfce4ea1-6c08-454c-833e-5b78578c3e89")]
        public enum TestEnumInt32 : int
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = 0x80,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Nine = 0x100,
            Ten = 0x200,
            Eleven = 0x400,
            Twelve = 0x800,
            Thirteen = 0x1000,
            Fourteen = 0x2000,
            Fifteen = 0x4000,
            Sixteen = 0x8000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Seventeen = 0x10000,
            Eighteen = 0x20000,
            Nineteen = 0x40000,
            Twenty = 0x80000,
            TwentyOne = 0x100000,
            TwentyTwo = 0x200000,
            TwentyThree = 0x400000,
            TwentyFour = 0x800000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            TwentyFive = 0x1000000,
            TwentySix = 0x2000000,
            TwentySeven = 0x4000000,
            TwentyEight = 0x8000000,
            TwentyNine = 0x10000000,
            Thirty = 0x20000000,
            ThirtyOne = 0x40000000,
            ThirtyTwo = unchecked((int)0x80000000)
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Flags()]
        [ObjectId("8c447530-b33e-46ad-a66e-d713b9564be0")]
        public enum TestEnumUInt32 : uint
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = 0x80,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Nine = 0x100,
            Ten = 0x200,
            Eleven = 0x400,
            Twelve = 0x800,
            Thirteen = 0x1000,
            Fourteen = 0x2000,
            Fifteen = 0x4000,
            Sixteen = 0x8000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Seventeen = 0x10000,
            Eighteen = 0x20000,
            Nineteen = 0x40000,
            Twenty = 0x80000,
            TwentyOne = 0x100000,
            TwentyTwo = 0x200000,
            TwentyThree = 0x400000,
            TwentyFour = 0x800000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            TwentyFive = 0x1000000,
            TwentySix = 0x2000000,
            TwentySeven = 0x4000000,
            TwentyEight = 0x8000000,
            TwentyNine = 0x10000000,
            Thirty = 0x20000000,
            ThirtyOne = 0x40000000,
            ThirtyTwo = 0x80000000
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Flags()]
        [ObjectId("9775dfdf-10dd-44f2-8bb1-10c7a25e7a74")]
        public enum TestEnumInt64 : long
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = 0x80,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Nine = 0x100,
            Ten = 0x200,
            Eleven = 0x400,
            Twelve = 0x800,
            Thirteen = 0x1000,
            Fourteen = 0x2000,
            Fifteen = 0x4000,
            Sixteen = 0x8000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Seventeen = 0x10000,
            Eighteen = 0x20000,
            Nineteen = 0x40000,
            Twenty = 0x80000,
            TwentyOne = 0x100000,
            TwentyTwo = 0x200000,
            TwentyThree = 0x400000,
            TwentyFour = 0x800000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            TwentyFive = 0x1000000,
            TwentySix = 0x2000000,
            TwentySeven = 0x4000000,
            TwentyEight = 0x8000000,
            TwentyNine = 0x10000000,
            Thirty = 0x20000000,
            ThirtyOne = 0x40000000,
            ThirtyTwo = 0x80000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            ThirtyThree = 0x100000000,
            ThirtyFour = 0x200000000,
            ThirtyFive = 0x400000000,
            ThirtySix = 0x800000000,
            ThirtySeven = 0x1000000000,
            ThirtyEight = 0x2000000000,
            ThirtyNine = 0x4000000000,
            Forty = 0x8000000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FortyOne = 0x10000000000,
            FortyTwo = 0x20000000000,
            FortyThree = 0x40000000000,
            FortyFour = 0x80000000000,
            FortyFive = 0x100000000000,
            FortySix = 0x200000000000,
            FortySeven = 0x400000000000,
            FortyEight = 0x800000000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FortyNine = 0x1000000000000,
            Fifty = 0x2000000000000,
            FiftyOne = 0x4000000000000,
            FiftyTwo = 0x8000000000000,
            FiftyThree = 0x10000000000000,
            FiftyFour = 0x20000000000000,
            FiftyFive = 0x40000000000000,
            FiftySix = 0x80000000000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FiftySeven = 0x100000000000000,
            FiftyEight = 0x200000000000000,
            FiftyNine = 0x400000000000000,
            Sixty = 0x800000000000000,
            SixtyOne = 0x1000000000000000,
            SixtyTwo = 0x2000000000000000,
            SixtyThree = 0x4000000000000000,
            SixtyFour = unchecked((long)0x8000000000000000)
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Flags()]
        [ObjectId("ec1f4e5e-5f71-42de-898d-c7a944d73480")]
        public enum TestEnumUInt64 : ulong
        {
            None = 0x0,
            One = 0x1,
            Two = 0x2,
            Three = 0x4,
            Four = 0x8,
            Five = 0x10,
            Six = 0x20,
            Seven = 0x40,
            Eight = 0x80,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Nine = 0x100,
            Ten = 0x200,
            Eleven = 0x400,
            Twelve = 0x800,
            Thirteen = 0x1000,
            Fourteen = 0x2000,
            Fifteen = 0x4000,
            Sixteen = 0x8000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            Seventeen = 0x10000,
            Eighteen = 0x20000,
            Nineteen = 0x40000,
            Twenty = 0x80000,
            TwentyOne = 0x100000,
            TwentyTwo = 0x200000,
            TwentyThree = 0x400000,
            TwentyFour = 0x800000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            TwentyFive = 0x1000000,
            TwentySix = 0x2000000,
            TwentySeven = 0x4000000,
            TwentyEight = 0x8000000,
            TwentyNine = 0x10000000,
            Thirty = 0x20000000,
            ThirtyOne = 0x40000000,
            ThirtyTwo = 0x80000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            ThirtyThree = 0x100000000,
            ThirtyFour = 0x200000000,
            ThirtyFive = 0x400000000,
            ThirtySix = 0x800000000,
            ThirtySeven = 0x1000000000,
            ThirtyEight = 0x2000000000,
            ThirtyNine = 0x4000000000,
            Forty = 0x8000000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FortyOne = 0x10000000000,
            FortyTwo = 0x20000000000,
            FortyThree = 0x40000000000,
            FortyFour = 0x80000000000,
            FortyFive = 0x100000000000,
            FortySix = 0x200000000000,
            FortySeven = 0x400000000000,
            FortyEight = 0x800000000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FortyNine = 0x1000000000000,
            Fifty = 0x2000000000000,
            FiftyOne = 0x4000000000000,
            FiftyTwo = 0x8000000000000,
            FiftyThree = 0x10000000000000,
            FiftyFour = 0x20000000000000,
            FiftyFive = 0x40000000000000,
            FiftySix = 0x80000000000000,

            ///////////////////////////////////////////////////////////////////////////////////////////

            FiftySeven = 0x100000000000000,
            FiftyEight = 0x200000000000000,
            FiftyNine = 0x400000000000000,
            Sixty = 0x800000000000000,
            SixtyOne = 0x1000000000000000,
            SixtyTwo = 0x2000000000000000,
            SixtyThree = 0x4000000000000000,
            SixtyFour = 0x8000000000000000
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Delegates
        [ObjectId("13daa524-57d2-470e-b67d-23490b21e0d0")]
        private delegate void VoidWithStringCallback(string value);

        [ObjectId("ad67563c-a83c-4cb4-a91b-aaed767f57a0")]
        private delegate long LongWithDateTimeCallback(DateTime dateTime);

        [ObjectId("c3cb55e9-3e8f-4e0d-95ec-d2b324f36715")]
        private delegate IEnumerable IEnumerableWithICommandCallback(ICommand command);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Delegates
        [ObjectId("d75fb203-a5eb-4688-83aa-487dff0119c7")]
        public delegate int TwoArgsDelegate(string param1, string param2);

        [ObjectId("55a64fc1-79e0-4236-a5af-d3a31b261591")]
        public delegate void ThreeArgsDelegate(object[] args, int value, ref object data);

        [ObjectId("e45e0f8b-65cf-4249-996e-cd973c22ae16")]
        public delegate int? OneArgNullableIntDelegate(int? value);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [ObjectId("d79b0f58-a2a6-4281-9987-97b2e613b181")]
        public delegate ReturnCode RefreshStreamsCallback(
            Interpreter interpreter, /* in */
            ChannelType channelType, /* in */
            bool force,              /* in */
            bool strict,             /* in */
            ref Stream inputStream,  /* in, out */
            ref Stream outputStream, /* in, out */
            ref Stream errorStream,  /* in, out */
            ref Result error         /* out */
        );
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Default()
        {
            id = GlobalState.NextId();
            @event = ThreadOps.CreateEvent(false);
            intArrayField = new int[10];
            privateField = this.ToString();
            intPtrArrayField = Array.CreateInstance(typeof(IntPtr), new int[] { 2, 3 });
            objectArrayField = Array.CreateInstance(typeof(Default), new int[] { 1 });
            idToString = false;
            throwOnDispose = defaultThrowOnDisposed;
            miscellaneousData = new object[] { null };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default(sbyte value)
            : this()
        {
            internalField = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default(string value)
            : this()
        {
            privateField = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default(sbyte value1, bool value2)
            : this(value1)
        {
            uniqueToString = value2;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static readonly object staticSyncRoot = new object();
        private static Type staticTypeField;
        private static object staticObjectField;
        private static StringPairList customInfoList;
        private static ObjectWrapperDictionary savedObjects;
        private static DateTime now;
        private static DateTimeNowCallback nowCallback;
        private static long nowIncrement;
        private static bool staticDynamicInvoke;
        private static bool defaultThrowOnDisposed;
        private static int traceFilterStubSetting;
        private static string traceFilterPattern;
        private static MatchMode traceFilterMatchMode;
        private static string traceErrorPattern;
        private static int shouldTraceError;
        private static int? staticTimeout = null;
        private static StringDictionary strings;
        private static StringBuilder calledMethods;
        private static string ruleCallbackText = null;

#if WINFORMS
        private static int shouldStatusCallback;
#endif

        private static object[] staticMiscellaneousData = new object[] { null };

#if DEBUGGER
        private static InteractiveLoopCallback savedInteractiveLoopCallback;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Data
        public int[] intArrayField;
        public bool boolField;
        public byte byteField;
        public short shortField;
        public int intField;
        public long longField;
        public decimal decimalField;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private long id;
        internal sbyte internalField;
        private EventWaitHandle @event;
        private Result asyncResult;
        private string privateField;
        private Type typeField;
        private object objectField;
        private Array intPtrArrayField;
        private Array objectArrayField;
        private Interpreter callbackInterpreter;
        private string newInterpreterText;
        private string useInterpreterText;
        private string freeInterpreterText;
        private string sleepWaitCallbackText;
        private string newCommandText;
        private string complainCommandName;
        private bool complainWithThrow;
        private ReturnCode complainCode;
        private Result complainResult;
        private int complainErrorLine;
        private bool dynamicInvoke;
        private string unknownCallbackText;
        private string packageFallbackText;
        private bool idToString;
        private bool uniqueToString;
        private bool throwOnDispose;
        private object[] miscellaneousData;
        private string tempPath;
        private bool tempException;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: When this is non-zero, any exceptions that are encountered
        //       by this class will be reported in detail.
        //
        private static bool VerboseExceptions = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Methods for Reflection
        //
        // HACK: This method is a placeholder for the ExecuteCallback
        //       delegate type.  It does not do anything.  It should
        //       only be used by the TestAddExecuteCallbacks method,
        //       below.
        //
        public static ReturnCode TestNopExecuteCallback(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in, out */
            ArgumentList arguments,  /* in */
            ref Result result        /* in, out */
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddExecuteCallbacks(
            Interpreter interpreter, /* in */
            Assembly assembly,       /* in */
            IPlugin plugin,          /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            CommandFlags flags,      /* in */
            string includePattern,   /* in: OPTIONAL */
            string excludePattern,   /* in: OPTIONAL */
            bool stopOnError,        /* in */
            ref LongList tokens,     /* in, out */
            ref ResultList errors    /* out */
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            if (assembly == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid assembly");
                return ReturnCode.Error;
            }

            BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                MetaBindingFlags.TransferHelper, true); /* STATIC */

            MethodInfo matchMethod = typeof(Default).GetMethod(
                "TestNopExecuteCallback", bindingFlags); /* SELF */

            if (matchMethod == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid match method");
                return ReturnCode.Error;
            }

            Type matchType = typeof(ExecuteCallback);

            try
            {
                Type[] types = assembly.GetTypes(); /* throw */

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    if (!type.IsClass)
                        continue;

                    string typeName = type.FullName;

                    if ((includePattern != null) && !StringOps.Match(
                            interpreter, MatchMode.Glob, typeName,
                            includePattern, false))
                    {
                        continue;
                    }

                    if ((excludePattern != null) && StringOps.Match(
                            interpreter, MatchMode.Glob, typeName,
                            excludePattern, false))
                    {
                        continue;
                    }

                    MethodInfo[] methodInfos = type.GetMethods(
                        bindingFlags);

                    if (methodInfos == null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "could not query type {0} methods",
                            MarshalOps.GetErrorTypeName(type)));

                        if (stopOnError)
                            return ReturnCode.Error;
                        else
                            continue;
                    }

                    foreach (MethodInfo methodInfo in methodInfos)
                    {
                        if (methodInfo == null)
                            continue;

                        if (!MarshalOps.MatchReturnType(
                                methodInfo.ReturnType,
                                matchMethod.ReturnType,
                                false, true))
                        {
                            continue;
                        }

                        if (!MarshalOps.MatchParameterTypes(
                                methodInfo.GetParameters(),
                                matchMethod.GetParameters(),
                                false, true))
                        {
                            continue;
                        }

                        Delegate @delegate = null;

                        try
                        {
                            @delegate = Delegate.CreateDelegate(
                                matchType, methodInfo, true);
                        }
                        catch (Exception e)
                        {
                            /* IGNORED */
                            RuntimeOps.MaybeGrabExceptions(
                                e, VerboseExceptions, ref errors);

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }

                        if (@delegate == null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "expected to create {0} delegate of {1}",
                                MarshalOps.GetErrorTypeName(matchType),
                                MarshalOps.GetErrorMemberName(methodInfo)));

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }

                        if (!(@delegate is ExecuteCallback))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "created delegate {0} not actually an {1}",
                                FormatOps.WrapOrNull(@delegate),
                                MarshalOps.GetErrorTypeName(matchType)));

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }

                        long token = 0;
                        Result result = null;

                        if (interpreter.AddExecuteCallback(
                                ScriptOps.MemberNameToEntityName(
                                    methodInfo.Name, false),
                                (ExecuteCallback)@delegate, clientData,
                                ref token, ref result) == ReturnCode.Ok)
                        {
                            if (tokens == null)
                                tokens = new LongList();

                            tokens.Add(token);
                        }
                        else
                        {
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                /* IGNORED */
                RuntimeOps.MaybeGrabExceptions(
                    e, VerboseExceptions, ref errors);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddCommands(
            Interpreter interpreter, /* in */
            Assembly assembly,       /* in */
            IPlugin plugin,          /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            CommandFlags flags,      /* in */
            string includePattern,   /* in: OPTIONAL */
            string excludePattern,   /* in: OPTIONAL */
            bool stopOnError,        /* in */
            ref LongList tokens,     /* in, out */
            ref ResultList errors    /* out */
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            if (assembly == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid assembly");
                return ReturnCode.Error;
            }

            Type matchType = typeof(ICommand);
            string matchTypeName = matchType.FullName;

            try
            {
                Type[] types = assembly.GetTypes(); /* throw */

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    if (!type.IsClass)
                        continue;

                    if (type.GetInterface(
                            matchTypeName) == null)
                    {
                        continue;
                    }

                    string typeName = type.FullName;

                    if ((includePattern != null) && !StringOps.Match(
                            interpreter, MatchMode.Glob, typeName,
                            includePattern, false))
                    {
                        continue;
                    }

                    if ((excludePattern != null) && StringOps.Match(
                            interpreter, MatchMode.Glob, typeName,
                            excludePattern, false))
                    {
                        continue;
                    }

                    bool added = false;
                    object @object = null;

                    try
                    {
                        long token = 0;

                        ICommandData commandData = new CommandData(
                            ScriptOps.TypeNameToEntityName(type, false),
                            null, null, clientData, typeName, flags,
                            plugin, token);

                        try
                        {
                            @object = Activator.CreateInstance(
                                type, commandData); /* throw */
                        }
                        catch (Exception e)
                        {
                            /* IGNORED */
                            RuntimeOps.MaybeGrabExceptions(
                                e, VerboseExceptions, ref errors);

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }

                        if (@object == null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "expected to create {0} object of {1}",
                                MarshalOps.GetErrorTypeName(matchType),
                                MarshalOps.GetErrorTypeName(type)));

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }

                        ICommand command = @object as ICommand;

                        if (command == null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "created object {0} not actually an {1}",
                                MarshalOps.GetErrorTypeName(type),
                                MarshalOps.GetErrorTypeName(matchType)));

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }

                        Result result = null;

                        if (interpreter.AddCommand(
                                command, clientData, ref token,
                                ref result) == ReturnCode.Ok)
                        {
                            added = true;

                            if (tokens == null)
                                tokens = new LongList();

                            tokens.Add(token);
                        }
                        else
                        {
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            if (stopOnError)
                                return ReturnCode.Error;
                            else
                                continue;
                        }
                    }
                    finally
                    {
                        if (!added && (@object != null))
                        {
                            ObjectOps.TryDisposeOrComplain<object>(
                                interpreter, ref @object);

                            @object = null;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                /* IGNORED */
                RuntimeOps.MaybeGrabExceptions(
                    e, VerboseExceptions, ref errors);

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for TraceOps.DebugTrace
        public static bool TestHasTraceListener(
            bool debug,
            TraceListenerType? listenerType,
            IClientData clientData
            )
        {
            return DebugOps.HasTraceListener(debug, listenerType, clientData);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetTraceFilterCallback(
            Interpreter interpreter,
            MatchMode? mode,
            string pattern,
            int index,
            bool setup,
            ref Result error
            )
        {
            TraceFilterCallback callback = null;

            switch (index)
            {
                case 0:
                    {
                        callback = new TraceFilterCallback(
                            TestTraceFilterStubCallback);

                        break;
                    }
                case 1:
                    {
                        callback = new TraceFilterCallback(
                            TestTraceFilterMessageCallback);

                        break;
                    }
                case 2:
                    {
                        callback = new TraceFilterCallback(
                            TestTraceFilterCategoryCallback);

                        break;
                    }
            }

            if (callback == null)
            {
                error = String.Format(
                    "unsupported trace filter callback #{0}",
                    index);

                return ReturnCode.Error;
            }

            if (mode != null)
            {
                lock (staticSyncRoot) /* TRANSACTIONAL */
                {
                    traceFilterMatchMode = (MatchMode)mode;
                    traceFilterPattern = pattern;
                }
            }

            if (interpreter != null)
            {
                if (setup)
                    interpreter.InternalTraceFilterCallback = callback;
                else
                    interpreter.InternalTraceFilterCallback = null;
            }
            else
            {
                if (setup)
                    TraceOps.SetTraceFilterCallback(callback);
                else
                    TraceOps.SetTraceFilterCallback(null);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestBumpTraceFilterStubSetting(
            bool filter
            )
        {
            if (filter)
                Interlocked.Increment(ref traceFilterStubSetting);
            else
                Interlocked.Decrement(ref traceFilterStubSetting);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for StringOps.MatchCore
        public static void TestSetMatchCallback(
            Interpreter interpreter,
            bool setup
            )
        {
            if (interpreter == null)
                return;

            if (setup)
                interpreter.MatchCallback = TestMatchCallback;
            else
                interpreter.MatchCallback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ThreadOps.GetTimeout
        public static void TestSetGetTimeoutCallback(
            Interpreter interpreter,
            bool setup
            )
        {
            if (interpreter == null)
                return;

            if (setup)
                interpreter.GetTimeoutCallback = TestGetTimeoutCallback;
            else
                interpreter.GetTimeoutCallback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        public static void TestSetBreakOrFailOnComplain(
            Interpreter interpreter, /* NOT USED */
            bool setup,
            bool useFail
            )
        {
            ComplainCallback callback = useFail ?
                (ComplainCallback)TestComplainCallbackFail :
                TestComplainCallbackBreak;

            Interpreter.ComplainCallback = setup ? callback : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetComplainCallback(
            Interpreter interpreter, /* NOT USED */
            bool setup,
            bool withThrow
            )
        {
            if (setup)
            {
                Interpreter.ComplainCallback = withThrow ?
                    (ComplainCallback)TestComplainCallbackThrow :
                    TestComplainCallbackNoThrow;
            }
            else
            {
                Interpreter.ComplainCallback = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetScriptOnComplain(
            Interpreter interpreter, /* NOT USED */
            string text,
            EngineFlags engineFlags,
            bool setup
            )
        {
            if (setup)
            {
                ScriptComplain scriptComplain = new ScriptComplain(
                    text, engineFlags);

                Interpreter.ComplainCallback = new ComplainCallback(
                    scriptComplain.Complain);
            }
            else
            {
                try
                {
                    ComplainCallback callback = Interpreter.ComplainCallback;

                    if (callback != null)
                    {
                        ScriptComplain scriptComplain = callback.Target as ScriptComplain;

                        if (scriptComplain != null)
                        {
                            scriptComplain.Dispose(); /* throw */
                            scriptComplain = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Default).Name,
                        TracePriority.CleanupError);
                }
                finally
                {
                    Interpreter.ComplainCallback = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for RuleIterationCallback
        public static RuleIterationCallback TestGetRuleIterationCallback()
        {
            return new RuleIterationCallback(TestRuleIterationCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRuleIterationCallback(
            Interpreter interpreter, /* in */
            IRule rule,              /* in */
            ref bool stopOnError,    /* in, out */
            ref ResultList errors    /* in, out */
            )
        {
            if (!String.IsNullOrEmpty(ruleCallbackText))
            {
                ScriptBooleanValue stopOnErrorValue = new ScriptBooleanValue(
                    stopOnError);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("interpreter", interpreter);
                objects.Add("rule", rule);
                objects.Add("stopOnError", stopOnErrorValue);

                ReturnCode code;
                Result result = null;

                code = Helpers.EvaluateScript(
                    interpreter, ruleCallbackText, objects, ref result);

                stopOnErrorValue.MaybeGetValue(ref stopOnError);

                if (result != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(result);
                }

                return code;
            }
            else
            {
                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for RuleMatchCallback
        public static RuleMatchCallback TestGetRuleMatchCallback()
        {
            return new RuleMatchCallback(TestRuleMatchCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRuleMatchCallback(
            Interpreter interpreter, /* in */
            IdentifierKind? kind,    /* in */
            MatchMode mode,          /* in */
            string text,             /* in */
            IRule rule,              /* in */
            ref bool? match,         /* in, out */
            ref ResultList errors    /* in, out */
            )
        {
            if (!String.IsNullOrEmpty(ruleCallbackText))
            {
                ScriptBooleanValue matchValue = new ScriptBooleanValue(
                    match);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("interpreter", interpreter);
                objects.Add("kind", kind);
                objects.Add("mode", mode);
                objects.Add("text", text);
                objects.Add("rule", rule);
                objects.Add("match", matchValue);

                ReturnCode code;
                Result result = null;

                code = Helpers.EvaluateScript(
                    interpreter, ruleCallbackText, objects, ref result);

                matchValue.MaybeGetNullableValue(ref match);

                if (result != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(result);
                }

                return code;
            }
            else
            {
                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for StatusCallback
#if WINFORMS
        public static ReturnCode TestSetStatusCallback(
            Interpreter interpreter,
            bool setup,
            ref Result error
            )
        {
            return TestSetStatusCallback(interpreter,
                setup ? (StatusCallback)TestStatusCallback : null,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetStatusCallback(
            Interpreter interpreter,
            StatusCallback callback,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                lock (interpreter.InternalSyncRoot)
                {
                    interpreter.StatusCallback = callback;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for SleepWaitCallback
        public static ReturnCode TestGetSleepWaitCallback(
            Interpreter interpreter,
            ref SleepWaitCallback callback,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                lock (interpreter.InternalSyncRoot)
                {
                    callback = interpreter.SleepWaitCallback;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetSleepWaitCallback(
            Interpreter interpreter,
            SleepWaitCallback callback,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                lock (interpreter.InternalSyncRoot)
                {
                    interpreter.SleepWaitCallback = callback;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for PathCallbackType
        public static ReturnCode TestGetPathCallback(
            PathCallbackType callbackType,
            ref Delegate @delegate,
            ref Result error
            )
        {
            return PathOps.GetCallback(
                callbackType, ref @delegate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestChangePathCallback(
            PathCallbackType callbackType,
            Delegate @delegate,
            ref Result error
            )
        {
            return PathOps.ChangeCallback(
                callbackType, @delegate, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for KeyEventMap
#if WINFORMS
        public static object TestGetKeyEventMap()
        {
            return StatusFormOps.GetKeyEventMap();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetKeyEventMap(
            object keyEventMap
            )
        {
            StatusFormOps.SetKeyEventMap(keyEventMap as KeyOps.KeyEventMap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSaveKeyEventMap(
            bool reset,
            ref object savedKeyEventMap
            )
        {
            StatusFormOps.SaveKeyEventMap(reset, ref savedKeyEventMap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestRestoreKeyEventMap(
            ref object savedKeyEventMap
            )
        {
            StatusFormOps.RestoreKeyEventMap(ref savedKeyEventMap);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for TestStreamHost
#if CONSOLE && WINFORMS
        public static ReturnCode TestRefreshTextBoxStreams(
            Interpreter interpreter, /* in */
            ChannelType channelType, /* in */
            bool force,              /* in */
            bool strict,             /* in */
            ref Stream inputStream,  /* in, out */
            ref Stream outputStream, /* in, out */
            ref Stream errorStream,  /* in, out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            TextBox textBox = StatusFormOps.GetTextBox(interpreter);

            if (textBox == null)
            {
                error = "invalid text box";
                return ReturnCode.Error;
            }

            ResultList errors = null;

            if (FlagOps.HasFlags(channelType, ChannelType.Input, true))
            {
                if (force || (inputStream == null))
                {
                    try
                    {
                        if (inputStream != null)
                        {
                            inputStream.Dispose(); /* throw */
                            inputStream = null;
                        }

                        inputStream = new TextBoxStream(
                            textBox, true, false); /* throw */
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (strict)
                        {
                            error = errors;
                            return ReturnCode.Error;
                        }
                    }
                }
            }

            if (FlagOps.HasFlags(channelType, ChannelType.Output, true))
            {
                if (force || (outputStream == null))
                {
                    try
                    {
                        if (outputStream != null)
                        {
                            outputStream.Dispose(); /* throw */
                            outputStream = null;
                        }

                        outputStream = new TextBoxStream(
                            textBox, false, true); /* throw */
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (strict)
                        {
                            error = errors;
                            return ReturnCode.Error;
                        }
                    }
                }
            }

            if (FlagOps.HasFlags(channelType, ChannelType.Error, true))
            {
                if (force || (errorStream == null))
                {
                    try
                    {
                        if (errorStream != null)
                        {
                            errorStream.Dispose(); /* throw */
                            errorStream = null;
                        }

                        errorStream = new TextBoxStream(
                            textBox, false, true); /* throw */
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (strict)
                        {
                            error = errors;
                            return ReturnCode.Error;
                        }
                    }
                }
            }

            return (errors == null) ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetupTextBoxStreamHost(
            Interpreter interpreter,     /* in */
            bool setup,                  /* in */
            ref ChannelType channelType, /* in, out */
            ref IStreamHost streamHost,  /* in, out */
            ref Result error             /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ChannelType newChannelType = channelType;

            if (setup)
            {
                newChannelType &= ~ChannelType.EndContext;
                newChannelType &= ~ChannelType.UseHost;
                newChannelType |= ChannelType.ErrorOnNull;
                newChannelType |= ChannelType.BeginContext;
                newChannelType |= ChannelType.AllowExist;

                if (!FlagOps.HasFlags(
                        newChannelType, ChannelType.StandardChannels,
                        false))
                {
                    error = "no channels selected for redirection";
                    return ReturnCode.Error;
                }

                IStreamHost newStreamHost = new TestStreamHost(
                    HostOps.NewData(typeof(TestStreamHost).Name,
                        interpreter, Defaults.HostCreateFlags),
                    new RefreshStreamsCallback(
                        TestRefreshTextBoxStreams));

                if (interpreter.ModifyStandardChannels(
                        newStreamHost, null, newChannelType,
                        ref error) == ReturnCode.Ok)
                {
                    channelType = newChannelType;
                    streamHost = newStreamHost;

                    return ReturnCode.Ok;
                }

                return ReturnCode.Error;
            }
            else
            {
                newChannelType &= ~ChannelType.BeginContext;
                newChannelType &= ~ChannelType.UseHost;
                newChannelType |= ChannelType.AllowExist;
                newChannelType |= ChannelType.EndContext;
                newChannelType |= ChannelType.SkipGetStream;

                return interpreter.ModifyStandardChannels(
                    streamHost, null, newChannelType, ref error);
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for TextBoxStream
#if WINFORMS
        public static ReturnCode TestCreateStatusFormTextBoxStream(
            Interpreter interpreter,
            bool canRead,
            bool canWrite,
            ref Stream stream,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error="invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                stream = new TextBoxStream(
                    StatusFormOps.GetTextBox(interpreter), canRead,
                    canWrite); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for SecurityProtocolType
#if NETWORK
        public static string TestSecurityProtocolToString(
            SecurityProtocolType protocol,
            CultureInfo cultureInfo,
            bool system
            )
        {
            try
            {
                SecurityProtocolType[] values = Enum.GetValues(
                    typeof(SecurityProtocolType)) as SecurityProtocolType[];

                if (values == null)
                    return null;

                StringList list = new StringList();

                list.Separator = String.Format(
                    "{0}{1}", Characters.Comma, Characters.Space);

                System.Net.SecurityProtocolType altProtocol =
                    (System.Net.SecurityProtocolType)protocol;

                Int32ObjectDictionary protocols = new Int32ObjectDictionary();

                foreach (SecurityProtocolType value in values)
                {
                    if (value == (SecurityProtocolType)0)
                        continue;

                    if (protocols.ContainsKey((int)value))
                        continue;

                    string valueString = value.ToString().Replace(
                        "SystemDefault, ", String.Empty);

                    if (system)
                    {
                        System.Net.SecurityProtocolType altValue =
                            (System.Net.SecurityProtocolType)value;

                        if ((altProtocol & altValue) != 0)
                        {
                            string altString = altValue.ToString();
                            int altInteger = 0;

                            if (Value.GetInteger2(altString,
                                    ValueFlags.AnyInteger, cultureInfo,
                                    ref altInteger) == ReturnCode.Ok)
                            {
                                list.Add(StringList.MakeList(
                                    valueString, FormatOps.Hexadecimal(
                                    altInteger, true)));
                            }
                            else
                            {
                                list.Add(StringList.MakeList(
                                    altString, FormatOps.Hexadecimal(
                                    altValue, true)));
                            }
                        }
                    }
                    else if ((protocol & value) != 0)
                    {
                        list.Add(StringList.MakeList(
                            valueString, FormatOps.Hexadecimal(
                            value, true)));
                    }

                    protocols.Add((int)value, null);
                }

                return list.ToString();
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Default).Name,
                    TracePriority.NetworkError);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static SecurityProtocolType? TestProbeSecurityProtocol(
            ref Result error /* out */
            )
        {
            try
            {
                SecurityProtocolType[] values = Enum.GetValues(
                    typeof(SecurityProtocolType)) as SecurityProtocolType[];

                if (values == null)
                {
                    error = String.Format(
                        "no enumeration values for {0}",
                        MarshalOps.GetErrorTypeName(
                            typeof(SecurityProtocolType)));

                    return null;
                }

                SecurityProtocolType result = SecurityProtocolType.None;

                SecurityProtocolType savedValue =
                    (SecurityProtocolType)ServicePointManager.SecurityProtocol;

                try
                {
                    foreach (SecurityProtocolType value in values)
                    {
                        if (value == (SecurityProtocolType)0)
                            continue;

                        try
                        {
                            //
                            // HACK: Check if this is a legal value by trying to
                            //       actually use it.  All known versions of the
                            //       .NET Framework will throw an exception if
                            //       this is not a legal value.
                            //
                            ServicePointManager.SecurityProtocol =
                                (System.Net.SecurityProtocolType)value; /* throw */

                            result |= value;
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }
                }
                finally
                {
                    ServicePointManager.SecurityProtocol =
                        (System.Net.SecurityProtocolType)savedValue;
                }

                return result;
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetupSecurityProtocol(
            bool force,            /* in */
            bool noObsolete,       /* in */
            ref ResultList results /* in, out */
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                Result localResult; /* REUSED */

                if (force || (BestSecurityProtocol == null))
                {
                    SecurityProtocolType? probeProtocols;

                    localResult = null;

                    probeProtocols = TestProbeSecurityProtocol(
                        ref localResult);

                    if (probeProtocols == null)
                    {
                        if (localResult != null)
                        {
                            if (results == null)
                                results = new ResultList();

                            results.Add(localResult);
                        }

                        return ReturnCode.Error;
                    }

                    SecurityProtocolType allProtocols =
                        (SecurityProtocolType)probeProtocols;

                    if (noObsolete)
                    {
                        allProtocols &= ~SecurityProtocolType.Ssl2;
                        allProtocols &= ~SecurityProtocolType.Ssl3;
                    }

                    BestSecurityProtocol = allProtocols;

                    localResult = String.Format(
                        "best security protocol set to {0}",
                        FormatOps.WrapOrNull(BestSecurityProtocol));
                }
                else
                {
                    localResult = "best security protocol already setup";
                }

                if (localResult != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(localResult);
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestResetSecurityProtocol(
            bool force /* in */
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (force || (BestSecurityProtocol != null))
                {
                    BestSecurityProtocol = null;
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestGetSecurityProtocol(
            ref ResultList results /* in, out */
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                SecurityProtocolType? protocol = BestSecurityProtocol;

                if (results == null)
                    results = new ResultList();

                if (protocol != null)
                {
                    results.Add(protocol);
                    return ReturnCode.Ok;
                }
                else
                {
                    results.Add("best security protocol unavailable");
                    return ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetSecurityProtocol(
            ref ResultList results /* in, out */
            )
        {
            try
            {
                lock (staticSyncRoot) /* TRANSACTIONAL */
                {
                    Result localResult; /* REUSED */
                    SecurityProtocolType? newProtocol = BestSecurityProtocol;

                    if (newProtocol != null)
                    {
                        SecurityProtocolType oldProtocol =
                            (SecurityProtocolType)ServicePointManager.SecurityProtocol;

                        if ((SecurityProtocolType)newProtocol != oldProtocol)
                        {
                            ServicePointManager.SecurityProtocol =
                                (System.Net.SecurityProtocolType)newProtocol;

                            localResult = String.Format(
                                "security protocol changed from {0} to {1} (set)",
                                FormatOps.WrapOrNull(oldProtocol),
                                FormatOps.WrapOrNull(newProtocol));
                        }
                        else
                        {
                            localResult = String.Format(
                                "security protocol unchanged from {0} (set)",
                                FormatOps.WrapOrNull(oldProtocol));
                        }
                    }
                    else
                    {
                        localResult = "cannot change security protocol (set)";
                    }

                    if (localResult != null)
                    {
                        if (results == null)
                            results = new ResultList();

                        results.Add(localResult);
                    }

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                if (results == null)
                    results = new ResultList();

                results.Add(e);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestPushSecurityProtocol(
            Interpreter interpreter, /* in: NOT USED */
            ref Result result        /* out */
            )
        {
            try
            {
                lock (staticSyncRoot) /* TRANSACTIONAL */
                {
                    if (SavedSecurityProtocol != null)
                    {
                        result = "already have saved security protocol";
                        return ReturnCode.Error;
                    }

                    SecurityProtocolType oldProtocol =
                        (SecurityProtocolType)ServicePointManager.SecurityProtocol;

                    SecurityProtocolType? newProtocol = BestSecurityProtocol;

                    if (newProtocol != null)
                    {
                        //
                        // NOTE: Save existing network security protocol for
                        //       possible future use (e.g. restoration).
                        //
                        SavedSecurityProtocol = oldProtocol;

                        //
                        // HACK: For use of the TLS 1.2+ security protocol
                        //       because some web servers fail without it.
                        //       In order to support the .NET Framework 2.0+
                        //       at compilation time, must use its integer
                        //       constant here.
                        //
                        ServicePointManager.SecurityProtocol =
                            (System.Net.SecurityProtocolType)newProtocol;

                        //
                        // NOTE: If we get to this point, just assume success.
                        //
                        result = String.Format(
                            "security protocol changed from {0} to {1} (push)",
                            FormatOps.WrapOrNull(oldProtocol),
                            FormatOps.WrapOrNull(newProtocol));
                    }
                    else
                    {
                        result = String.Format(
                            "security protocol unchanged from {0} (push)",
                            FormatOps.WrapOrNull(oldProtocol));
                    }

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                result = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestPopSecurityProtocol(
            Interpreter interpreter, /* in: NOT USED */
            ref Result result        /* out */
            )
        {
            try
            {
                lock (staticSyncRoot) /* TRANSACTIONAL */
                {
                    SecurityProtocolType? newProtocol = SavedSecurityProtocol;

                    if (newProtocol == null)
                    {
                        result = "missing saved security protocol";
                        return ReturnCode.Error;
                    }

                    SecurityProtocolType oldProtocol =
                        (SecurityProtocolType)ServicePointManager.SecurityProtocol;

                    ServicePointManager.SecurityProtocol =
                        (System.Net.SecurityProtocolType)newProtocol;

                    SavedSecurityProtocol = null;

                    result = String.Format(
                        "security protocol changed from {0} to {1} (pop)",
                        FormatOps.WrapOrNull(oldProtocol),
                        FormatOps.WrapOrNull(newProtocol));

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                result = e;
                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ScriptWebClient
#if NETWORK
        public static bool TestHasOkPreWebClientCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return (interpreter.PreWebClientCallback == TestOkPreWebClientCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestHasErrorPreWebClientCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return (interpreter.PreWebClientCallback == TestErrorPreWebClientCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestOkPreWebClientCallback(
            Interpreter interpreter, /* NOT USED */
            string argument,
            IClientData clientData,
            ref Result error /* NOT USED */
            )
        {
            ReturnCode code;
            Result result = null;

            code = TestPushSecurityProtocol(interpreter, ref result);

            TraceOps.DebugTrace(String.Format(
                "TestOkPreWebClientCallback: code = {0}, result = {1}",
                FormatOps.WrapOrNull(code), FormatOps.WrapOrNull(result)),
                typeof(Default).Name, TracePriority.NetworkDebug);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestErrorPreWebClientCallback(
            Interpreter interpreter, /* NOT USED */
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            error = "use of web client has been forbidden";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetPreWebClientCallback(
            Interpreter interpreter,
            bool enable,
            bool success,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (enable)
            {
                interpreter.PreWebClientCallback = success ?
                    (PreWebClientCallback)TestOkPreWebClientCallback :
                    TestErrorPreWebClientCallback;
            }
            else
            {
                interpreter.PreWebClientCallback = null;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestHasScriptNewWebClientCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return (interpreter.NewWebClientCallback == TestScriptNewWebClientCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestHasErrorNewWebClientCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return (interpreter.NewWebClientCallback == TestErrorNewWebClientCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static WebClient TestScriptNewWebClientCallback(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            string text = null;

            if (clientData != null)
            {
                object data = null;

                /* IGNORED */
                clientData = ClientData.UnwrapOrReturn(
                    clientData, ref data);

                text = data as string;
            }

            if (text != null)
            {
                return ScriptWebClient.Create(
                    interpreter, text, argument, ref error);
            }
            else
            {
                return WebOps.CreateClient(
                    argument, WebOps.GetTimeout(interpreter),
                    ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetScriptNewWebClientCallback(
            Interpreter interpreter,
            bool enable,
            bool success,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (enable)
            {
                interpreter.NewWebClientCallback = success ?
                    (NewWebClientCallback)TestScriptNewWebClientCallback :
                    TestErrorNewWebClientCallback;
            }
            else
            {
                interpreter.NewWebClientCallback = null;
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ErrorCallback / ErrorListCallback
        public static bool TestGetErrorCallbacks(
            Interpreter interpreter,       /* in: NOT USED */
            ref Delegate errorDelegate,    /* out: OPTIONAL */
            ref Delegate errorListDelegate /* out: OPTIONAL */
            )
        {
            errorDelegate = Value.GetErrorCallback();
            errorListDelegate = Value.GetErrorListCallback();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestSetErrorCallbacks(
            Interpreter interpreter,    /* in: NOT USED */
            Delegate errorDelegate,     /* in: OPTIONAL */
            Delegate errorListDelegate, /* in: OPTIONAL */
            bool setup                  /* in */
            )
        {
            if ((errorDelegate == null) && (errorListDelegate == null))
            {
                if (setup)
                {
                    Value.SetErrorCallback(
                        new ErrorCallback(TestErrorCallback));

                    Value.SetErrorListCallback(
                        new ErrorListCallback(TestErrorListCallback));
                }
                else
                {
                    Value.SetErrorCallback(null);
                    Value.SetErrorListCallback(null);
                }

                return true;
            }

            if (errorDelegate != null)
            {
                ErrorCallback callback =
                    errorDelegate as ErrorCallback;

                if (callback != null)
                    Value.SetErrorCallback(callback);
                else
                    return false;
            }

            if (errorListDelegate != null)
            {
                ErrorListCallback callback =
                    errorListDelegate as ErrorListCallback;

                if (callback != null)
                    Value.SetErrorListCallback(callback);
                else
                    return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestEnableErrorCallbacks(
            Interpreter interpreter, /* in: NOT USED */
            bool enable,             /* in */
            string pattern           /* in: OPTIONAL */
            )
        {
            Interlocked.Exchange(
                ref traceErrorPattern, pattern);

            if (enable)
            {
                return Interlocked.Increment(
                    ref shouldTraceError) > 0;
            }
            else
            {
                return Interlocked.Decrement(
                    ref shouldTraceError) <= 0;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for AddExecuteCallback
        public static ReturnCode TestExecuteCallback1(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                //
                // HACK: Just default the command name to "eval" here
                //       when there are zero arguments.
                //
                result = String.Format(
                    "wrong # args: should be \"{0} arg ?arg ...?\"",
                    (arguments.Count > 0) ? (string)arguments[0] : "command");

                return ReturnCode.Error;
            }

            return interpreter.EvaluateScript(arguments, 1, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestExecuteCallback2(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 3)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} {1} arg ?arg ...?\"",
                    (arguments.Count > 0) ? (string)arguments[0] : "command",
                    (arguments.Count > 1) ? (string)arguments[1] : "subcommand");

                return ReturnCode.Error;
            }

            return interpreter.EvaluateScript(arguments, 2, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddExecuteCallback(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddExecuteCallback(
                name, TestExecuteCallback1, clientData, ref token,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddExecuteCallback(
            Interpreter interpreter,
            string name,
            ICommand command,
            IClientData clientData,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddExecuteCallback(
                name, command, TestExecuteCallback2, clientData,
                ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddExecuteCallbacks(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            bool ignoreNull,
            bool stopOnError,
            ref IEnumerable<IExecuteCallbackData> collection,
            ref int errorCount,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            IPlugin plugin =
#if TEST_PLUGIN || DEBUG
                    interpreter.GetTestPlugin(ref result);
#else
                    interpreter.GetCorePlugin(ref result);
#endif

            if (plugin == null)
                return ReturnCode.Error;

            collection = new IExecuteCallbackData[] {
                new ExecuteCallbackData(
                    String.Format("{0}_one", name),
                    TestExecuteCallback1, null, 0
                ),
                null, /* NOTE: Purposely invalid. */
                new ExecuteCallbackData(
                    String.Format("{0}_two", name),
                    null, null, 1001 /* NOTE: Bad token. */
                ),
                new ExecuteCallbackData(
                    String.Format("{0}_two", name),
                    null, null, 0
                ),
                new ExecuteCallbackData(
                    String.Format("{0}_tri", name),
                    TestExecuteCallback2, null, 0
                )
            };

            return interpreter.AddExecuteCallbacks(
                collection, plugin, clientData, ignoreNull,
                stopOnError, ref errorCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRemoveExecuteCallbacks(
            Interpreter interpreter,
            IEnumerable<IExecuteCallbackData> collection,
            IClientData clientData,
            bool ignoreNull,
            bool stopOnError,
            ref int errorCount,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.RemoveExecuteCallbacks(
                collection, clientData, ignoreNull,
                stopOnError, ref errorCount, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for AddSubCommands
        public static string TestNewDelegateNameCallback(
            DelegateDictionary delegates,
            MethodInfo methodInfo,
            IClientData clientData
            ) /* NewDelegateNameCallback */
        {
            if (methodInfo == null)
                return null;

            string methodName = methodInfo.Name;
            StringBuilder builder = StringOps.NewStringBuilder();

            builder.Append(methodName);

            if ((delegates != null) &&
                delegates.ContainsKey(builder.ToString()))
            {
                ParameterInfo[] parameterInfo =
                    methodInfo.GetParameters();

                if (parameterInfo != null)
                {
                    builder.AppendFormat("{0}{1}",
                        Characters.Underscore,
                        parameterInfo.Length);
                }
            }

            if ((delegates != null) &&
                delegates.ContainsKey(builder.ToString()))
            {
                byte[] hashValue = HashOps.HashString(
                    null, (string)null, methodInfo.ToString());

                if (hashValue != null)
                {
                    builder.AppendFormat("{0}{1}",
                        Characters.Underscore,
                        ArrayOps.ToHexadecimalString(hashValue));
                }
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddStaticSubCommands(
            Interpreter interpreter,
            string name,
            DelegateFlags delegateFlags,
            ref long token,
            ref Result result
            )
        {
            return TestAddStaticSubCommands(
                interpreter, name, typeof(Default),
                delegateFlags, ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddStaticSubCommands(
            Interpreter interpreter,
            string name,
            Type type,
            DelegateFlags delegateFlags,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            IPlugin plugin =
#if TEST_PLUGIN || DEBUG
                    interpreter.GetTestPlugin(ref result);
#else
                    interpreter.GetCorePlugin(ref result);
#endif

            if (plugin == null)
                return ReturnCode.Error;

            return interpreter.AddSubCommands(
                name, type, null, plugin, ClientData.Empty,
                TestNewDelegateNameCallback, delegateFlags |
                DelegateFlags.PublicStaticMask, ref token,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewHostCallback
        public static IHost TestNewHostNullCallback(
            IHostData hostData
            )
        {
            return null; /* FAILURE */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for TestContext
        public static ReturnCode TestGetComparer(
            Interpreter interpreter,
            ref IComparer<string> comparer,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                comparer = interpreter.TestComparer;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetComparer(
            Interpreter interpreter,
            IComparer<string> comparer,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                interpreter.TestComparer = comparer;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for Reparse Points
        public static ReturnCode TestProcessReparseData(
            byte[] bytes,
            Encoding encoding,
            ref Result result
            )
        {
            if (bytes == null)
            {
                result = "invalid bytes";
                return ReturnCode.Error;
            }

            int length = bytes.Length;

            if (length == 0)
            {
                result = "must have more than zero bytes";
                return ReturnCode.Error;
            }

            if ((length % 2) != 0)
            {
                result = "must have an even number of bytes";
                return ReturnCode.Error;
            }

            //
            // NOTE: Match with "\??\X:\some\path".
            //
            char[] scanCharacters1 = new char[] {
                Characters.Backslash,
                Characters.QuestionMark,
                Characters.QuestionMark,
                Characters.Backslash,
                Characters.X, /* NOTE: Any letter. */
                Characters.Colon,
                Characters.Backslash
            };

            //
            // NOTE: Match with "X:\some\path".
            //
            char[] scanCharacters2 = new char[] {
                Characters.X, /* NOTE: Any letter. */
                Characters.Colon,
                Characters.Backslash
            };

            bool found = false;
            int startIndex = 0;

            for (; startIndex < length; startIndex += 2)
            {
                if (TestScanForCharacters(
                        bytes, scanCharacters1, startIndex))
                {
                    found = true;
                    break;
                }

                if (TestScanForCharacters(
                        bytes, scanCharacters2, startIndex))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                result = "no path found";
                return ReturnCode.Error;
            }

            if (encoding == null)
                encoding = Encoding.Unicode;

            try
            {
                string value = encoding.GetString(
                    bytes, startIndex, length - startIndex);

                if (!String.IsNullOrEmpty(value))
                {
                    int lastIndex = value.IndexOf(Characters.Null);

                    if (lastIndex != Index.Invalid)
                        value = value.Substring(0, lastIndex);
                }

                result = value;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for Easy Integration
        public static Interpreter TestCreateInterpreterWithCommands(
            InterpreterSettings interpreterSettings, /* in: OPTIONAL */
            IClientData clientData,                  /* in: OPTIONAL */
            IPlugin plugin,                          /* in: OPTIONAL */
            IEnumerable<Type> commandTypes,          /* in: OPTIONAL */
            ref Result error                         /* out */
            )
        {
            ICollection<CommandTriplet> commands =
                new List<CommandTriplet>();

            if (commandTypes != null)
            {
                foreach (Type commandType in commandTypes)
                {
                    commands.Add(new CommandTriplet(
                        true, null, commandType, 0));
                }
            }
            else
            {
                Type commandType = typeof(_Commands.Nop);

                commands.Add(new CommandTriplet(
                    true, "nop_demo1", commandType, 0));

                commands.Add(new CommandTriplet(
                    true, "nop_demo2", commandType, 0));

                commands.Add(new CommandTriplet(
                    true, "nop_demo3", commandType, 0));
            }

            return Helpers.CreateInterpreterWithCommands(
                interpreterSettings, commands, clientData,
                plugin, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        public static ReturnCode TestEvaluateScriptWithConsoleRedirection(
            Interpreter interpreter, /* in */
            IScript script,          /* in */
            string input,            /* in: OPTIONAL */
            StringBuilder output,    /* in: OPTIONAL */
            StringBuilder error,     /* in: OPTIONAL */
            ref Result result,       /* out */
            ref int errorLine        /* out */
            )
        {
            TextReader inputReader = null;
            TextWriter outputWriter = null;
            TextWriter errorWriter = null;

            try
            {
                if (input != null)
                    inputReader = new StringReader(input);

                if (output != null)
                    outputWriter = new StringWriter(output);

                if (error != null)
                    errorWriter = new StringWriter(error);

                return Helpers.EvaluateScriptWithConsoleRedirection(
                    interpreter, script, inputReader, outputWriter,
                    errorWriter, ref result, ref errorLine);
            }
            finally
            {
                if (errorWriter != null)
                {
                    errorWriter.Close();
                    errorWriter = null;
                }

                if (outputWriter != null)
                {
                    outputWriter.Close();
                    outputWriter = null;
                }

                if (inputReader != null)
                {
                    inputReader.Close();
                    inputReader = null;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for IScript
        public static IScript TestCreateScriptForPolicy(
            string name,
            string type,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result error
            )
        {
            return Script.CreateForPolicy(
                name, type, text, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ScriptXmlOps
#if XML
        #region XmlGetAttributeCallback / XmlSetAttributeCallback Helper Methods
        public static IScript TestCreateScriptFromXmlNode(
            string type,                         /* in */
            XmlNode node,                        /* in */
            EngineMode engineMode,               /* in */
            ScriptFlags scriptFlags,             /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            IClientData clientData,              /* in */
            ref Result error                     /* out */
            )
        {
            return Script.CreateFromXmlNode(
                type, node, engineMode, scriptFlags, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestXmlTryGetAttributeValue(
            XmlElement element,           /* in */
            string attributeName,         /* in */
            object defaultAttributeValue, /* in */
            out object attributeValue     /* out */
            )
        {
            return ScriptXmlOps.TryGetAttributeValue(
                element, attributeName, defaultAttributeValue,
                out attributeValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestXmlTrySetAttributeValue(
            XmlElement element,   /* in */
            string attributeName, /* in */
            object attributeValue /* in */
            )
        {
            return ScriptXmlOps.TrySetAttributeValue(
                element, attributeName, attributeValue);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region XmlGetAttributeCallback Methods
        public static bool TestNullXmlGetAttributeCallback(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            bool required,            /* in */
            out object attributeValue /* out */
            )
        {
            attributeValue = null;
            return !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestStringXmlGetAttributeCallback(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            bool required,            /* in */
            out object attributeValue /* out */
            )
        {
            return TestXmlTryGetAttributeValue(
                element, attributeName, null,
                out attributeValue) || !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestBooleanXmlGetAttributeCallback(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            bool required,            /* in */
            out object attributeValue /* out */
            )
        {
            if (!TestXmlTryGetAttributeValue(
                    element, attributeName, null,
                    out attributeValue))
            {
                return !required;
            }

            Interpreter interpreter = Interpreter.GetActive();
            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            bool boolValue = false;
            Result error = null;

            if (Value.GetBoolean2(
                    (string)attributeValue,
                    ValueFlags.AnyBoolean,
                    cultureInfo, ref boolValue,
                    ref error) != ReturnCode.Ok)
            {
                throw new ScriptException(error);
            }

            attributeValue = boolValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestInt64XmlGetAttributeCallback(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            bool required,            /* in */
            out object attributeValue /* out */
            )
        {
            if (!TestXmlTryGetAttributeValue(
                    element, attributeName, null,
                    out attributeValue))
            {
                return !required;
            }

            Interpreter interpreter = Interpreter.GetActive();
            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            long longValue = 0;
            Result error = null;

            if (Value.GetWideInteger2(
                    (string)attributeValue,
                    ValueFlags.AnyWideInteger,
                    cultureInfo, ref longValue,
                    ref error) != ReturnCode.Ok)
            {
                throw new ScriptException(error);
            }

            attributeValue = longValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByteArrayXmlGetAttributeCallback(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            bool required,            /* in */
            out object attributeValue /* out */
            )
        {
            if (!TestXmlTryGetAttributeValue(
                    element, attributeName, null,
                    out attributeValue))
            {
                return !required;
            }

            Interpreter interpreter = Interpreter.GetActive();
            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            byte[] bytesValue = null;
            Result error = null;

            if (ArrayOps.GetBytesFromString(
                    (string)attributeValue,
                    cultureInfo, ref bytesValue,
                    ref error) != ReturnCode.Ok)
            {
                throw new ScriptException(error);
            }

            attributeValue = bytesValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestListXmlGetAttributeCallback(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            bool required,            /* in */
            out object attributeValue /* out */
            )
        {
            if (!TestXmlTryGetAttributeValue(
                    element, attributeName, null,
                    out attributeValue))
            {
                return !required;
            }

            Interpreter interpreter = Interpreter.GetActive();
            StringList list = null;
            Result error = null;

            if (ParserOps<string>.SplitList(
                    interpreter, (string)attributeValue,
                    0, Length.Invalid, false, ref list,
                    ref error) != ReturnCode.Ok)
            {
                throw new ScriptException(error);
            }

            attributeValue = list;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestErrorXmlGetAttributeCallback(
            XmlElement element,       /* in */
            string attributeName,     /* in */
            bool required,            /* in */
            out object attributeValue /* out */
            )
        {
            throw new ScriptException(String.Format(
                "unsupported {0}xml attribute {1}",
                required ? "required " : String.Empty,
                FormatOps.WrapOrNull(attributeName)));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region XmlSetAttributeCallback Methods
        public static bool TestNullXmlSetAttributeCallback(
            XmlElement element,   /* in */
            string attributeName, /* in */
            bool required,        /* in */
            object attributeValue /* in */
            )
        {
            return !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestStringXmlSetAttributeCallback(
            XmlElement element,   /* in */
            string attributeName, /* in */
            bool required,        /* in */
            object attributeValue /* in */
            )
        {
            return TestXmlTrySetAttributeValue(
                element, attributeName,
                attributeValue) || !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestBooleanXmlSetAttributeCallback(
            XmlElement element,   /* in */
            string attributeName, /* in */
            bool required,        /* in */
            object attributeValue /* in */
            )
        {
            if (attributeValue is string)
            {
                Interpreter interpreter = Interpreter.GetActive();
                CultureInfo cultureInfo = null;

                if (interpreter != null)
                    cultureInfo = interpreter.InternalCultureInfo;

                bool boolValue = false;
                Result error = null;

                if (Value.GetBoolean2(
                        (string)attributeValue,
                        ValueFlags.AnyBoolean,
                        cultureInfo, ref boolValue,
                        ref error) != ReturnCode.Ok)
                {
                    throw new ScriptException(error);
                }

                attributeValue = boolValue;
            }

            return TestXmlTrySetAttributeValue(
                element, attributeName,
                attributeValue) || !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestInt64XmlSetAttributeCallback(
            XmlElement element,   /* in */
            string attributeName, /* in */
            bool required,        /* in */
            object attributeValue /* in */
            )
        {
            if (attributeValue is string)
            {
                Interpreter interpreter = Interpreter.GetActive();
                CultureInfo cultureInfo = null;

                if (interpreter != null)
                    cultureInfo = interpreter.InternalCultureInfo;

                long longValue = 0;
                Result error = null;

                if (Value.GetWideInteger2(
                        (string)attributeValue,
                        ValueFlags.AnyWideInteger,
                        cultureInfo, ref longValue,
                        ref error) != ReturnCode.Ok)
                {
                    throw new ScriptException(error);
                }

                attributeValue = longValue;
            }

            return TestXmlTrySetAttributeValue(
                element, attributeName,
                attributeValue) || !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByteArrayXmlSetAttributeCallback(
            XmlElement element,   /* in */
            string attributeName, /* in */
            bool required,        /* in */
            object attributeValue /* in */
            )
        {
            if (attributeValue is string)
            {
                Interpreter interpreter = Interpreter.GetActive();
                CultureInfo cultureInfo = null;

                if (interpreter != null)
                    cultureInfo = interpreter.InternalCultureInfo;

                byte[] bytesValue = null;
                Result error = null;

                if (ArrayOps.GetBytesFromString(
                        (string)attributeValue,
                        cultureInfo, ref bytesValue,
                        ref error) != ReturnCode.Ok)
                {
                    throw new ScriptException(error);
                }

                attributeValue = bytesValue;
            }

            return TestXmlTrySetAttributeValue(
                element, attributeName,
                attributeValue) || !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestListXmlSetAttributeCallback(
            XmlElement element,   /* in */
            string attributeName, /* in */
            bool required,        /* in */
            object attributeValue /* in */
            )
        {
            if (attributeValue is string)
            {
                Interpreter interpreter = Interpreter.GetActive();
                StringList list = null;
                Result error = null;

                if (ParserOps<string>.SplitList(
                        interpreter, (string)attributeValue,
                        0, Length.Invalid, true, ref list,
                        ref error) != ReturnCode.Ok)
                {
                    throw new ScriptException(error);
                }

                attributeValue = list;
            }

            return TestXmlTrySetAttributeValue(
                element, attributeName,
                attributeValue) || !required;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestErrorXmlSetAttributeCallback(
            XmlElement element,   /* in */
            string attributeName, /* in */
            bool required,        /* in */
            object attributeValue /* in */
            )
        {
            throw new ScriptException(String.Format(
                "unsupported {0}xml attribute {1}",
                required ? "required " : String.Empty,
                FormatOps.WrapOrNull(attributeName)));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region XmlGetAttributeCallback Support Methods
        public static ReturnCode TestGetScriptXmlAttributeGetter(
            string name,            /* in */
            ref Delegate @delegate, /* out */
            ref Result error        /* out */
            )
        {
            lock (ScriptXmlOps.GetSyncRoot()) /* TRANSACTIONAL */
            {
                XmlGetAttributeDictionary attributeGetters =
                    ScriptXmlOps.GetAttributeGetters();

                if (attributeGetters == null)
                {
                    error = "xml attribute getters not available";
                    return ReturnCode.Error;
                }

                if (name == null)
                {
                    error = "invalid xml attribute name";
                    return ReturnCode.Error;
                }

                XmlGetAttributeCallback callback;

                if (!attributeGetters.TryGetValue(name, out callback))
                {
                    error = "xml attribute callback does not exist";
                    return ReturnCode.Error;
                }

                @delegate = callback;
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestChangeScriptXmlAttributeGetter(
            string name,        /* in */
            Delegate @delegate, /* in: OPTIONAL */
            bool overwrite,     /* in */
            ref int changed,    /* in, out */
            ref Result error    /* out */
            )
        {
            lock (ScriptXmlOps.GetSyncRoot()) /* TRANSACTIONAL */
            {
                XmlGetAttributeDictionary attributeGetters =
                    ScriptXmlOps.GetAttributeGetters();

                if (attributeGetters == null)
                {
                    error = "xml attribute getters not available";
                    return ReturnCode.Error;
                }

                if (name == null)
                {
                    error = "invalid xml attribute name";
                    return ReturnCode.Error;
                }

                if (@delegate != null)
                {
                    XmlGetAttributeCallback callback =
                        @delegate as XmlGetAttributeCallback;

                    if (callback == null)
                    {
                        error = "invalid xml attribute callback";
                        return ReturnCode.Error;
                    }

                    if (attributeGetters.ContainsKey(name))
                    {
                        if (overwrite)
                        {
                            changed++;
                        }
                        else
                        {
                            error = "xml attribute callback already exists";
                            return ReturnCode.Error;
                        }
                    }

                    attributeGetters[name] = callback;
                    changed++;
                }
                else if (attributeGetters.Remove(name))
                {
                    changed++;
                }
                else
                {
                    error = "xml attribute callback does not exist";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddScriptXmlAttributeGetter(
            string name,     /* in */
            Type type,       /* in */
            bool overwrite,  /* in */
            ref int changed, /* in, out */
            ref Result error /* out */
            )
        {
            Delegate @delegate;

            if (type == null)
            {
                @delegate = new XmlGetAttributeCallback(
                    TestNullXmlGetAttributeCallback);
            }
            else if (type == typeof(bool))
            {
                @delegate = new XmlGetAttributeCallback(
                    TestBooleanXmlGetAttributeCallback);
            }
            else if (type == typeof(long))
            {
                @delegate = new XmlGetAttributeCallback(
                    TestInt64XmlGetAttributeCallback);
            }
            else if (type == typeof(string))
            {
                @delegate = new XmlGetAttributeCallback(
                    TestStringXmlGetAttributeCallback);
            }
            else if (type == typeof(byte[]))
            {
                @delegate = new XmlGetAttributeCallback(
                    TestByteArrayXmlGetAttributeCallback);
            }
            else if (type == typeof(StringList))
            {
                @delegate = new XmlGetAttributeCallback(
                    TestListXmlGetAttributeCallback);
            }
            else if (type == typeof(ScriptException))
            {
                @delegate = new XmlGetAttributeCallback(
                    TestErrorXmlGetAttributeCallback);
            }
            else
            {
                error = String.Format(
                    "unsupported type {0} for {1}",
                    MarshalOps.GetErrorTypeName(type),
                    MarshalOps.GetErrorTypeName(
                        typeof(XmlGetAttributeCallback)));

                return ReturnCode.Error;
            }

            return TestChangeScriptXmlAttributeGetter(
                name, @delegate, overwrite, ref changed, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region XmlSetAttributeCallback Support Methods
        public static ReturnCode TestGetScriptXmlAttributeSetter(
            string name,            /* in */
            ref Delegate @delegate, /* out */
            ref Result error        /* out */
            )
        {
            lock (ScriptXmlOps.GetSyncRoot()) /* TRANSACTIONAL */
            {
                XmlSetAttributeDictionary attributeSetters =
                    ScriptXmlOps.GetAttributeSetters();

                if (attributeSetters == null)
                {
                    error = "xml attribute setters not available";
                    return ReturnCode.Error;
                }

                if (name == null)
                {
                    error = "invalid xml attribute name";
                    return ReturnCode.Error;
                }

                XmlSetAttributeCallback callback;

                if (!attributeSetters.TryGetValue(name, out callback))
                {
                    error = "xml attribute callback does not exist";
                    return ReturnCode.Error;
                }

                @delegate = callback;
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestChangeScriptXmlAttributeSetter(
            string name,        /* in */
            Delegate @delegate, /* in: OPTIONAL */
            bool overwrite,     /* in */
            ref int changed,    /* in, out */
            ref Result error    /* out */
            )
        {
            lock (ScriptXmlOps.GetSyncRoot()) /* TRANSACTIONAL */
            {
                XmlSetAttributeDictionary attributeSetters =
                    ScriptXmlOps.GetAttributeSetters();

                if (attributeSetters == null)
                {
                    error = "xml attribute setters not available";
                    return ReturnCode.Error;
                }

                if (name == null)
                {
                    error = "invalid xml attribute name";
                    return ReturnCode.Error;
                }

                if (@delegate != null)
                {
                    XmlSetAttributeCallback callback =
                        @delegate as XmlSetAttributeCallback;

                    if (callback == null)
                    {
                        error = "invalid xml attribute callback";
                        return ReturnCode.Error;
                    }

                    if (attributeSetters.ContainsKey(name))
                    {
                        if (overwrite)
                        {
                            changed++;
                        }
                        else
                        {
                            error = "xml attribute callback already exists";
                            return ReturnCode.Error;
                        }
                    }

                    attributeSetters[name] = callback;
                    changed++;
                }
                else if (attributeSetters.Remove(name))
                {
                    changed++;
                }
                else
                {
                    error = "xml attribute callback does not exist";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddScriptXmlAttributeSetter(
            string name,     /* in */
            Type type,       /* in */
            bool overwrite,  /* in */
            ref int changed, /* in, out */
            ref Result error /* out */
            )
        {
            Delegate @delegate;

            if (type == null)
            {
                @delegate = new XmlSetAttributeCallback(
                    TestNullXmlSetAttributeCallback);
            }
            else if (type == typeof(bool))
            {
                @delegate = new XmlSetAttributeCallback(
                    TestBooleanXmlSetAttributeCallback);
            }
            else if (type == typeof(long))
            {
                @delegate = new XmlSetAttributeCallback(
                    TestInt64XmlSetAttributeCallback);
            }
            else if (type == typeof(string))
            {
                @delegate = new XmlSetAttributeCallback(
                    TestStringXmlSetAttributeCallback);
            }
            else if (type == typeof(byte[]))
            {
                @delegate = new XmlSetAttributeCallback(
                    TestByteArrayXmlSetAttributeCallback);
            }
            else if (type == typeof(StringList))
            {
                @delegate = new XmlSetAttributeCallback(
                    TestListXmlSetAttributeCallback);
            }
            else if (type == typeof(ScriptException))
            {
                @delegate = new XmlSetAttributeCallback(
                    TestErrorXmlSetAttributeCallback);
            }
            else
            {
                error = String.Format(
                    "unsupported type {0} for {1}",
                    MarshalOps.GetErrorTypeName(type),
                    MarshalOps.GetErrorTypeName(
                        typeof(XmlSetAttributeCallback)));

                return ReturnCode.Error;
            }

            return TestChangeScriptXmlAttributeSetter(
                name, @delegate, overwrite, ref changed, ref error);
        }
        #endregion
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewInterpreterCallback
        public static ReturnCode TestPublicStaticNewInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            string formatted = String.Format(
                "TestPublicStaticNewInterpreterCallback: interpreter = {0}",
                FormatOps.InterpreterNoThrow(interpreter));

            if (calledMethods == null)
                calledMethods = StringOps.NewStringBuilder();

            calledMethods.AppendLine(formatted);

            TraceOps.DebugTrace(
                formatted, typeof(Default).Name, TracePriority.Highest);

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for [pkgInstallLog]
        public static ReturnCode TestPkgInstallLogCallback(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            try
            {
                bool? install;
                PkgInstallType? type;
                string name;
                string directory;

                code = TestValidatePkgInstallArguments(
                    interpreter, arguments, out install, out type,
                    out name, out directory, ref result);

                if (code != ReturnCode.Ok)
                    return code;

                string action = FormatOps.DisplayUnknown;

                if (install != null)
                    action = (bool)install ? "INSTALL" : "UNINSTALL";

                //
                // TODO: Maybe (?) send this package install information
                //       somewhere else as well, e.g. a SQLite database,
                //       etc.
                //
                TraceOps.DebugTrace(String.Format(
                    "{0}: {1} {2} AS {3} TO {4}",
                    PkgInstallLogCommandName, action,
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(type),
                    FormatOps.WrapOrNull(directory)),
                    typeof(Default).Name,
                    TracePriority.PackageDebug5);
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                TraceOps.DebugTrace(String.Format(
                    "TestPkgInstallLogCallback: interpreter = {0}, " +
                    "arguments = {1}, code = {2}, result = {3}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(arguments), code,
                    FormatOps.WrapOrNull(result)),
                    typeof(Default).Name, TracePriority.PackageDebug4);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddPkgInstallLogCommand(
            Interpreter interpreter,
            ref Result result
            )
        {
            long token = 0;

            return TestAddPkgInstallLogCommand(
                interpreter, PkgInstallLogCommandName, null, ref token,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddPkgInstallLogCommand(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddExecuteCallback(
                name, TestPkgInstallLogCallback, clientData, ref token,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Command Callback Methods
        public static ReturnCode TestAddCommands(
            Interpreter interpreter,
            IClientData clientData,
            ref ResultList results
            )
        {
            if (interpreter == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            if (interpreter.IsSafe())
            {
                if (results == null)
                    results = new ResultList();

                results.Add("permission denied: safe interpreter");
                return ReturnCode.Error;
            }

            int errorCount = 0;
            long token; /* REUSED */
            Result result; /* REUSED */

            token = 0;
            result = null;

            if (interpreter.AddExecuteCallback("vadd",
                    TestAddVariableCommandCallback, clientData,
                    ref token, ref result) == ReturnCode.Ok)
            {
                if (token != 0)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(token);
                }
            }
            else
            {
                if (result != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(result);
                }

                errorCount++;
            }

            token = 0;
            result = null;

            if (interpreter.AddExecuteCallback("vusable",
                    TestUsableVariableCommandCallback, clientData,
                    ref token, ref result) == ReturnCode.Ok)
            {
                if (token != 0)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(token);
                }
            }
            else
            {
                if (result != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(result);
                }

                errorCount++;
            }

            token = 0;
            result = null;

            if (interpreter.AddExecuteCallback("vislocked",
                    TestIsLockedVariableCommandCallback, clientData,
                    ref token, ref result) == ReturnCode.Ok)
            {
                if (token != 0)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(token);
                }
            }
            else
            {
                if (result != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(result);
                }

                errorCount++;
            }

            token = 0;
            result = null;

            if (interpreter.AddExecuteCallback("vlock",
                    TestLockVariableCommandCallback, clientData,
                    ref token, ref result) == ReturnCode.Ok)
            {
                if (token != 0)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(token);
                }
            }
            else
            {
                if (result != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(result);
                }

                errorCount++;
            }

            token = 0;
            result = null;

            if (interpreter.AddExecuteCallback("vunlock",
                    TestUnlockVariableCommandCallback, clientData,
                    ref token, ref result) == ReturnCode.Ok)
            {
                if (token != 0)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(token);
                }
            }
            else
            {
                if (result != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(result);
                }

                errorCount++;
            }

            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for CrossAppDomainHelper
#if !NET_STANDARD_20
        public static ReturnCode TestEvaluateInAppDomain(
            AppDomain appDomain, /* in */
            string text,         /* in */
            bool viaCross,       /* in */
            bool noTrace         /* in */
            )
        {
            ReturnCode code;
            Result result = null;

            code = TestEvaluateInAppDomain(
                appDomain, text, viaCross, ref result);

            if (!noTrace && (code != ReturnCode.Ok))
            {
                TraceOps.DebugTrace(String.Format(
                    "TestEvaluateInAppDomain: appDomain = {0}, " +
                    "code = {1}, result = {2}", (appDomain != null) ?
                        AppDomainOps.GetId(appDomain).ToString() :
                        FormatOps.DisplayNull,
                    code, FormatOps.WrapOrNull(result)),
                    typeof(Default).Name, TracePriority.Highest);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestEvaluateInAppDomain(
            AppDomain appDomain, /* in */
            string text,         /* in */
            bool viaCross,       /* in */
            ref Result result    /* out */
            )
        {
            if (viaCross)
            {
                return TestEvaluateInAppDomainViaCross(
                    appDomain, text, ref result);
            }
            else
            {
                return TestEvaluateInAppDomainViaCreate(
                    appDomain, text, ref result);
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IStringList TestToStringFormat(
            IDictionary<long, double> dictionary,
            bool pairs,
            bool keys,
            bool values,
            MatchMode mode,
            string keyPattern,
            string valuePattern,
            string keyFormat,
            string valueFormat,
            IFormatProvider formatProvider,
            bool noCase
            )
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<long, double>();

                dictionary.Add(long.MinValue, double.MinValue);
                dictionary.Add(-1, -1.0);
                dictionary.Add(0, 0.0);
                dictionary.Add(1, 1.0);
                dictionary.Add(long.MaxValue, double.MaxValue);
            }

            return GenericOps<long, double>.KeysAndValues(
                dictionary, pairs, keys, values, mode,
                keyPattern, valuePattern, keyFormat,
                valueFormat, formatProvider, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestCreateMissingVariable(
            Interpreter interpreter,
            ICallFrame variableFrame,
            string varName,
            object varValue,
            TraceList traces,
            bool dirty,
            ref VariableFlags variableFlags,
            ref IVariable variable,
            ref Result error
            )
        {
            variableFlags &= ~VariableFlags.ErrorMask; // GetVariable

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            IVariable localVariable;

            interpreter.CreateMissingVariable(
                variableFrame, varName, varValue, traces,
                dirty, ref variableFlags, out localVariable);

            variable = localVariable;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestCreateThreadVariable(
            Interpreter interpreter,
            VariableFlags variableFlags,
            string name,
            ref object variable,
            ref Result error
            )
        {
            bool success = false;
            ThreadVariable threadVariable = null;

            try
            {
                threadVariable = ThreadVariable.Create();

                if (threadVariable == null)
                {
                    error = String.Format(
                        "could not create thread variable {0}",
                        FormatOps.ErrorVariableName(name));

                    return ReturnCode.Error;
                }

                if (threadVariable.AddVariable(
                        interpreter, variableFlags, name,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                variable = threadVariable;
                success = true;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                if (!success && (threadVariable != null))
                {
                    threadVariable.Dispose();
                    threadVariable = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NativeCallbackType
#if NATIVE && (WINDOWS || UNIX || UNSAFE)
        public static ReturnCode TestChangeNativeCallback(
            NativeCallbackType callbackType, /* in */
            bool automatic,                  /* in */
            ref Delegate @delegate,          /* in, out */
            ref Result error                 /* out */
            )
        {
            if (automatic && (@delegate == null))
            {
                switch (callbackType)
                {
                    case NativeCallbackType.IsMainThread:
                        {
                            @delegate = new NativeIsMainThreadCallback(
                                TestNativeIsMainThreadCallback);

                            break;
                        }
                    case NativeCallbackType.GetStackPointer:
                    case NativeCallbackType.GetStackAllocated:
                    case NativeCallbackType.GetStackMaximum:
                    case NativeCallbackType.UnixGetStackMaximum:
                        {
                            @delegate = new NativeStackCallback(
                                TestNativeStackCallback);

                            break;
                        }
                }
            }

            return NativeStack.ChangeCallback(
                callbackType, ref @delegate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestChangeNativeCallback( /* REDUNDANT */
            NativeCallbackType callbackType, /* in */
            ref Delegate @delegate,          /* in, out */
            ref Result error                 /* out */
            )
        {
            return NativeStack.ChangeCallback(
                callbackType, ref @delegate, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static NameValueCollection TestGetAppSettings()
        {
            return ConfigurationOps.GetAppSettings();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetAppSettings(
            NameValueCollection appSettings
            )
        {
            ConfigurationOps.SetAppSettings(appSettings);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        public static bool TestSetDebugInteractiveLoopCallback(
            Interpreter interpreter,
            bool? setup
            )
        {
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (Interpreter.IsDeletedOrDisposed(interpreter, false))
                    return false;

                bool result = false;

                if (setup != null)
                {
                    if ((bool)setup)
                    {
                        if (!TestIsDebugInteractiveLoopCallback(
                                interpreter.InteractiveLoopCallback))
                        {
                            TestEnableDebugInteractiveLoopCallback(
                                interpreter);

                            result = true;
                        }
                    }
                    else
                    {
                        if (!TestIsSavedInteractiveLoopCallback(
                                interpreter.InteractiveLoopCallback))
                        {
                            TestDisableDebugInteractiveLoopCallback(
                                interpreter);

                            result = true;
                        }
                    }

                    TraceOps.DebugTrace(String.Format(
                        "TestSetDebugInteractiveLoopCallback: HOOK {0}{1}.",
                        result ? String.Empty : "already ", (bool)setup ?
                        "installed" : "uninstalled"), typeof(Default).Name,
                        TracePriority.ScriptDebug2);
                }
                else
                {
                    result = TestIsDebugInteractiveLoopCallback(
                        interpreter.InteractiveLoopCallback);
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestThreadPoolDebugEmergencyBreak(
            Interpreter interpreter,
            ref Result error
            )
        {
            try
            {
                if (ThreadOps.QueueUserWorkItem(
                        TestDebugEmergencyBreakWaitCallback, interpreter))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "failed to queue work item to thread pool";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestGetOptions(
            Interpreter interpreter,
            IdentifierKind kind,
            string name,
            IEnumerable<IOption> options,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            bool endOfOptions,
            ref int argumentIndex,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            IIdentifier identifier = null;

            if (interpreter.GetIdentifier(
                    kind, name, arguments, lookupFlags, ref identifier,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            OptionDictionary localOptions = null;

            if (options != null)
                localOptions = new OptionDictionary(options);

            if (endOfOptions)
            {
                if (localOptions == null)
                    localOptions = new OptionDictionary();

                localOptions.Add(Option.CreateEndOfOptions());
            }

            return interpreter.GetOptions(
                identifier, localOptions, arguments, ref argumentIndex,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestGetOptions(
            Interpreter interpreter,
            ArgumentList arguments,
            IEnumerable<IOption> options,
            int listCount,
            int startIndex,
            int stopIndex,
            OptionBehaviorFlags behaviorFlags,
            bool endOfOptions,
            bool noCase,
            bool noValue,
            bool noSet,
            ref int nextIndex,
            ref int endIndex,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            OptionDictionary localOptions = null;

            if (options != null)
                localOptions = new OptionDictionary(options);

            if (endOfOptions)
            {
                if (localOptions == null)
                    localOptions = new OptionDictionary();

                localOptions.Add(Option.CreateEndOfOptions());
            }

            if (interpreter.GetOptions(
                    localOptions, arguments, listCount,
                    startIndex, stopIndex, behaviorFlags,
                    noCase, noValue, noSet, ref nextIndex,
                    ref endIndex, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        public static string TestCorePluginAbout(
            Interpreter interpreter,
            bool showCertificate,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            IPlugin plugin = interpreter.GetCorePlugin(ref error);

            if (plugin == null)
                return null;

            Result localResult = null;

            if (HelpOps.GetPluginAbout(
                    interpreter, plugin, showCertificate,
                    ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return null;
            }

            return localResult;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestSecurityPluginAbout(
            Interpreter interpreter,
            bool alternate,
            ref Result error
            )
        {
            IPlugin plugin = ScriptOps.FindSecurityPlugin(
                interpreter, Priority.Highest, alternate, ref error);

            if (plugin == null)
                return null;

            Result localResult = null;

            if (plugin.About(
                    interpreter, ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return null;
            }

            return localResult;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestResizeStaticMiscellaneousData(
            int newSize
            )
        {
            Array.Resize(ref staticMiscellaneousData, newSize);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ValueType TestStaticValueType(
            ValueType value
            )
        {
            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ValueType TestStaticByRefValueType(
            ref ValueType value
            )
        {
            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestStaticGeneric<T>(
            T value,
            bool typeOnly
            )
        {
            if (typeOnly)
            {
                return (value != null) ? value.GetType() : typeof(object);
            }
            else
            {
                return Result.FromObject(value, true, false, false);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestStaticObjectIdentity(
            object value
            )
        {
            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetResourceString(
            Interpreter interpreter,
            string name,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            if (name == null)
            {
                error = "invalid name";
                return null;
            }

            try
            {
                IFileSystemHost fileSystemHost = interpreter.Host;

                if (fileSystemHost == null)
                {
                    error = "interpreter host not available";
                    return null;
                }

                scriptFlags |= ScriptFlags.CoreAssemblyOnly;

                Result result = null;

                if (fileSystemHost.GetData(
                        name, DataFlags.Script, ref scriptFlags,
                        ref clientData, ref result) == ReturnCode.Ok)
                {
                    return result;
                }
                else
                {
                    error = result;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestArePluginsIsolated(
            Interpreter interpreter
            )
        {
#if ISOLATED_PLUGINS
            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (Interpreter.IsDeletedOrDisposed(interpreter, false))
                        return false;

                    return FlagOps.HasFlags(
                        interpreter.PluginFlags, PluginFlags.Isolated, true);
                }
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestMethod(
            string argument /* This is the value of the "pwzArgument" argument
                             * as it was passed to native CLR API method
                             * ICLRRuntimeHost.ExecuteInDefaultAppDomain. */
            )
        {
            int value = 0;

            if (Value.GetInteger2(
                    argument, ValueFlags.AnyInteger, null,
                    ref value) == ReturnCode.Ok)
            {
                return value;
            }

            return -1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool? TestGetQuiet(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.InternalQuiet;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool? TestSetQuiet()
        {
            return TestSetQuiet(Interpreter.GetActive(), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool? TestUnsetQuiet()
        {
            return TestSetQuiet(Interpreter.GetActive(), false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool? TestSetQuiet(
            Interpreter interpreter,
            bool quiet
            )
        {
            if (interpreter == null)
                return null;

            bool oldQuiet;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                oldQuiet = interpreter.InternalQuietNoLock;
                interpreter.InternalQuietNoLock = quiet;

                //
                // HACK: Also set (or clear) interpreter flags that
                //       are "associated" with quiet mode (i.e. as
                //       far as the Eagle test suite is concerned).
                //
                TestSetQuietMask(interpreter, quiet);
            }

            TraceOps.DebugTrace(String.Format(
                "TestSetQuiet: Quiet mode was {0}, now {1}.",
                oldQuiet ? "enabled" : "disabled",
                quiet ? "enabled" : "disabled"),
                typeof(Default).Name, TracePriority.TestDebug2);

            return oldQuiet;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetNoBackgroundError(
            Interpreter interpreter,
            bool noBackgroundError
            )
        {
            if (interpreter == null)
                return;

            interpreter.SetNoBackgroundError(noBackgroundError);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestSetComplain(
            Interpreter interpreter,
            bool complain
            )
        {
            if (interpreter == null)
                return false;

            IInteractiveHost interactiveHost = interpreter.GetInteractiveHost();

            if (interactiveHost == null)
                return false;

            FieldInfo fieldInfo = interactiveHost.GetType().GetField(
                "hostFlags", ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PrivateInstanceGetField, true));

            if (fieldInfo == null)
                return false;

            HostFlags hostFlags = (HostFlags)fieldInfo.GetValue(
                interactiveHost);

            if (complain)
                hostFlags |= HostFlags.Complain;
            else
                hostFlags &= ~HostFlags.Complain;

            fieldInfo.SetValue(interactiveHost, hostFlags);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestDisposeInterpreter(
            Interpreter interpreter,
            int timeout,
            bool asynchronous,
            ref Result error
            )
        {
            if (asynchronous)
            {
                if (Engine.QueueWorkItem(
                        new WaitCallback(TestDisposeInterpreterWaitCallback),
                        new AnyPair<int, Interpreter>(timeout, interpreter)))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "could not queue work item";
                }
            }
            else if (TestDisposeInterpreter(
                    timeout, ref interpreter, ref error))
            {
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestDisposeInterpreterViaGc(
            string text,
            int timeout,
            ref Result error
            )
        {
            try
            {
                Interpreter interpreter;
                Result result = null; /* REUSED */

                interpreter = Interpreter.Create(ref result);

                if (interpreter == null)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                if (text != null)
                {
                    ReturnCode code;

                    result = null;

                    code = interpreter.EvaluateScript(text, ref result);

                    if (code != ReturnCode.Ok)
                    {
                        error = result;
                        return code;
                    }
                }

                DateTime oldNow = TimeOps.GetUtcNow();
                DateTime stopNow = oldNow.AddMilliseconds(timeout);

                int oldCount = Interpreter.GlobalDisposeCount;

                interpreter.ClearReferences();
                interpreter = null; /* NOTE: No more references. */

                while (true) /* NOTE: Must timeout or dispose. */
                {
                    ObjectOps.CollectGarbage(); /* NOTE: Dispose now? */

                    int newCount = Interpreter.GlobalDisposeCount;

                    if (newCount > oldCount)
                        return ReturnCode.Ok;

                    DateTime newNow = TimeOps.GetUtcNow();

                    if (newNow >= stopNow)
                        break;
                }

                error = String.Format(
                    "interpreter was not disposed in {0} milliseconds",
                    timeout);

                return ReturnCode.Error;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
#if INTERACTIVE_COMMANDS
        public static bool TestDisposedWriteHeader(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (Interpreter.IsDeletedOrDisposed(interpreter, false))
                    return false;

                IInteractiveHost interactiveHost = interpreter.GetInteractiveHost();

                interpreter.SetDisposed(true);

                try
                {
                    IInteractiveLoopData loopData = new InteractiveLoopData(
                        null, ReturnCode.Ok, null, null, HeaderFlags.AllForTest);

                    bool show = false;

                    InteractiveOps.Commands.show(
                        interpreter, interactiveHost, new ArgumentList(),
                        loopData, null, loopData.HeaderFlags, ReturnCode.Ok,
                        null, ref show);

                    return true;
                }
                catch (Exception e)
                {
                    DebugOps.Complain(interpreter, ReturnCode.Error, e);

                    return false;
                }
                finally
                {
                    interpreter.SetDisposed(false);
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestMayEnterInteractiveLoop(
            Interpreter interpreter,
            ref bool result,
            ref Result error
            )
        {
            StringList arguments = null;

            if ((interpreter != null) && (interpreter.GetArguments(
                    ref arguments, ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            return TestMayEnterInteractiveLoop(
                interpreter, arguments, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestMayEnterInteractiveLoop(
            Interpreter interpreter,
            IEnumerable<string> args,
            ref bool result,
            ref Result error
            )
        {
            return Interpreter.MayEnterInteractiveLoop(
                interpreter, args, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ExitCode TestShellMainCore(
            Interpreter interpreter,
            IEnumerable<string> args,
            bool initialize,
            bool loop,
            ref Result result
            )
        {
            IShellCallbackData callbackData = ShellCallbackData.Create();

            if (callbackData == null)
            {
                result = "could not create shell callback data";
                return Utility.FailureExitCode();
            }

            callbackData.PreviewArgumentCallback = TestShellFixEmptyPreviewArgumentCallback;
            callbackData.UnknownArgumentCallback = TestShellUnknownArgumentCallback;
            callbackData.EvaluateScriptCallback = TestShellEvaluateScriptCallback;

#if DEBUGGER
            callbackData.InteractiveLoopCallback = TestShellInteractiveLoopCallback;
#endif

            return Interpreter.ShellMainCore(
                interpreter, callbackData, null, args, initialize, loop,
                ref result);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddLoadPluginPolicy(
            Interpreter interpreter,
            IClientData clientData,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddPolicy(
                TestLoadPluginPolicy, null, clientData, ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestLoadPluginViaBytes(
            Interpreter interpreter,
            string assemblyFileName,
#if CAS_POLICY
            Evidence evidence,
#endif
            string typeName,
            IClientData clientData,
            PluginFlags pluginFlags,
            ref IPlugin plugin,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(assemblyFileName))
            {
                result = "invalid assembly file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(assemblyFileName))
            {
                result = String.Format(
                    "cannot load plugin: assembly file {0} does not exist",
                    FormatOps.WrapOrNull(assemblyFileName));

                return ReturnCode.Error;
            }

            if (plugin != null)
            {
                result = "cannot overwrite valid plugin";
                return ReturnCode.Error;
            }

            if (token != 0)
            {
                result = "cannot overwrite valid plugin token";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;

            try
            {
                //
                // NOTE: Figure out what the debug symbols file for the
                //       assembly would be if it actually existed.
                //
                string symbolFileName = PathOps.GetNativePath(
                    PathOps.CombinePath(null, Path.GetDirectoryName(
                    assemblyFileName), Path.GetFileNameWithoutExtension(
                    assemblyFileName) + FileExtension.Symbols)); /* throw */

                byte[] assemblyBytes = File.ReadAllBytes(
                    assemblyFileName); /* throw */

                byte[] symbolBytes = File.Exists(symbolFileName) ?
                    File.ReadAllBytes(symbolFileName) : null; /* throw */

                code = interpreter.LoadPlugin(assemblyBytes, symbolBytes,
#if CAS_POLICY
                    evidence,
#endif
                    typeName, clientData, pluginFlags, ref plugin, ref result);

                if (code == ReturnCode.Ok)
                {
                    code = interpreter.AddPlugin(
                        plugin, clientData, ref token, ref result);
                }

                return code;
            }
            catch (Exception e)
            {
                result = e;
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (plugin != null))
                {
                    ReturnCode unloadCode;
                    Result unloadResult = null;

                    unloadCode = interpreter.UnloadPlugin(
                        plugin, clientData, pluginFlags, ref unloadResult);

                    if (unloadCode != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, unloadCode, unloadResult);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRenameNamespace(
            Interpreter interpreter,
            string oldName,
            string newName,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(newName))
            {
                error = "invalid or empty new namespace name";
                return ReturnCode.Error;
            }

            if (NamespaceOps.IsQualifiedName(newName))
            {
                error = "new namespace name must not be qualified";
                return ReturnCode.Error;
            }

            INamespace @namespace = NamespaceOps.Lookup(
                interpreter, oldName, false, false, ref error);

            if (@namespace == null)
                return ReturnCode.Error;

            try
            {
                @namespace.Name = newName;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSaveObjects(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            interpreter.SaveObjects(ref savedObjects);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestRestoreObjects(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            interpreter.RestoreObjects(ref savedObjects);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if EMIT
        public static void TestExecuteDelegateCommands(
            Interpreter interpreter,
            ArgumentList arguments,
            bool dynamic,
            ref ReturnCodeList returnCodes,
            ref ResultList results
            )
        {
            if (returnCodes == null)
                returnCodes = new ReturnCodeList();

            if (results == null)
                results = new ResultList();

            long[] tokens = { 0, 0, 0 };

            try
            {
                ReturnCode code;
                Result result = null;

                if (interpreter == null)
                {
                    returnCodes.Add(ReturnCode.Error);
                    results.Add("invalid interpreter");

                    return;
                }

                if (arguments == null)
                {
                    returnCodes.Add(ReturnCode.Error);
                    results.Add("invalid argument list");

                    return;
                }

                IPlugin plugin =
#if TEST_PLUGIN || DEBUG
                    interpreter.GetTestPlugin(ref result);
#else
                    interpreter.GetCorePlugin(ref result);
#endif

                if (plugin == null)
                {
                    returnCodes.Add(ReturnCode.Error);
                    results.Add(result);

                    return;
                }

                Delegate[] delegates = { null, null, null };

                if (dynamic)
                {
                    BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStatic, true);

                    MethodInfo[] methodInfo = { null, null, null };

                    methodInfo[0] = typeof(Default).GetMethod(
                        "TestVoidMethod", bindingFlags);

                    if (methodInfo[0] == null)
                    {
                        returnCodes.Add(ReturnCode.Error);
                        results.Add("method TestVoidMethod not found");

                        return;
                    }

                    methodInfo[1] = typeof(Default).GetMethod(
                        "TestLongMethod", bindingFlags);

                    if (methodInfo[1] == null)
                    {
                        returnCodes.Add(ReturnCode.Error);
                        results.Add("method TestLongMethod not found");

                        return;
                    }

                    methodInfo[2] = typeof(Default).GetMethod(
                        "TestIEnumerableMethod", bindingFlags);

                    if (methodInfo[2] == null)
                    {
                        returnCodes.Add(ReturnCode.Error);
                        results.Add("method TestIEnumerableMethod not found");

                        return;
                    }

                    Type[] types = { null, null, null };

                    code = DelegateOps.CreateManagedDelegateType(
                        interpreter, null, null, null, null,
                        methodInfo[0].ReturnType,
                        TestGetParameterTypeList(methodInfo[0]), ref types[0],
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        returnCodes.Add(code);
                        results.Add(result);

                        return;
                    }

                    code = DelegateOps.CreateManagedDelegateType(
                        interpreter, null, null, null, null,
                        methodInfo[1].ReturnType,
                        TestGetParameterTypeList(methodInfo[1]), ref types[1],
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        returnCodes.Add(code);
                        results.Add(result);

                        return;
                    }

                    code = DelegateOps.CreateManagedDelegateType(
                        interpreter, null, null, null, null,
                        methodInfo[2].ReturnType,
                        TestGetParameterTypeList(methodInfo[2]), ref types[2],
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        returnCodes.Add(code);
                        results.Add(result);

                        return;
                    }

                    delegates[0] = Delegate.CreateDelegate(types[0],
                        methodInfo[0], false);

                    delegates[1] = Delegate.CreateDelegate(types[1],
                        methodInfo[1], false);

                    delegates[2] = Delegate.CreateDelegate(types[2],
                        methodInfo[2], false);
                }
                else
                {
                    delegates[0] = new VoidWithStringCallback(
                        TestVoidMethod);

                    delegates[1] = new LongWithDateTimeCallback(
                        TestLongMethod);

                    delegates[2] = new IEnumerableWithICommandCallback(
                        TestIEnumerableMethod);
                }

                string typeName = typeof(_Commands._Delegate).FullName;
                ICommand[] commands = { null, null, null };

                commands[0] = new _Commands._Delegate(
                    new CommandData("voidDelegate",
                    null, null, ClientData.Empty, typeName,
                    CommandFlags.None, plugin, 0),
                    new DelegateData(delegates[0],
                    DelegateFlags.LegacyMask, 0));

                commands[1] = new _Commands._Delegate(
                    new CommandData("longDelegate",
                    null, null, ClientData.Empty, typeName,
                    CommandFlags.None, plugin, 0),
                    new DelegateData(delegates[1],
                    DelegateFlags.LegacyMask, 0));

                commands[2] = new _Commands._Delegate(
                    new CommandData("enumerableDelegate",
                    null, null, ClientData.Empty, typeName,
                    CommandFlags.None, plugin, 0),
                    new DelegateData(delegates[2],
                    DelegateFlags.LegacyMask, 0));

                code = interpreter.AddCommand(
                    commands[0], ClientData.Empty,
                    ref tokens[0], ref result);

                if (code != ReturnCode.Ok)
                {
                    returnCodes.Add(code);
                    results.Add(result);

                    return;
                }

                code = interpreter.AddCommand(
                    commands[1], ClientData.Empty,
                    ref tokens[1], ref result);

                if (code != ReturnCode.Ok)
                {
                    returnCodes.Add(code);
                    results.Add(result);

                    return;
                }

                code = interpreter.AddCommand(
                    commands[2], ClientData.Empty,
                    ref tokens[2], ref result);

                if (code != ReturnCode.Ok)
                {
                    returnCodes.Add(code);
                    results.Add(result);

                    return;
                }

                arguments.Insert(0, commands[0].Name);

                code = Engine.EvaluateScript(
                    interpreter, arguments.ToString(),
                    ref result);

                returnCodes.Add(code);
                results.Add(result);

                arguments[0] = commands[1].Name;

                code = Engine.EvaluateScript(
                    interpreter, arguments.ToString(),
                    ref result);

                returnCodes.Add(code);
                results.Add(result);

                arguments[0] = commands[2].Name;

                code = Engine.EvaluateScript(
                    interpreter, arguments.ToString(),
                    ref result);

                returnCodes.Add(code);
                results.Add(result);
            }
            finally
            {
                if (interpreter != null)
                {
                    ReturnCode removeCode;
                    Result removeError = null;

                    if (tokens[2] != 0)
                    {
                        removeCode = interpreter.RemoveCommand(
                            tokens[2], null, ref removeError);

                        if (removeCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeError);
                        }
                    }

                    if (tokens[1] != 0)
                    {
                        removeCode = interpreter.RemoveCommand(
                            tokens[1], null, ref removeError);

                        if (removeCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeError);
                        }
                    }

                    if (tokens[0] != 0)
                    {
                        removeCode = interpreter.RemoveCommand(
                            tokens[0], null, ref removeError);

                        if (removeCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeError);
                        }
                    }
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestExecuteStaticDelegates(
            ArgumentList arguments,
            ref ReturnCodeList returnCodes,
            ref ResultList results
            )
        {
            if (returnCodes == null)
                returnCodes = new ReturnCodeList();

            if (results == null)
                results = new ResultList();

            ReturnCode code;
            Result result = null;

            code = Engine.ExecuteDelegate(
                new VoidWithStringCallback(TestVoidMethod),
                arguments, ref result);

            returnCodes.Add(code);
            results.Add(result);

            code = Engine.ExecuteDelegate(
                new LongWithDateTimeCallback(TestLongMethod),
                arguments, ref result);

            returnCodes.Add(code);
            results.Add(result);

            code = Engine.ExecuteDelegate(
                new IEnumerableWithICommandCallback(TestIEnumerableMethod),
                arguments, ref result);

            returnCodes.Add(code);
            results.Add(result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestToIntPtr(
            Interpreter interpreter, /* NOT USED */
            Type type, /* NOT USED */
            string text,
            OptionDictionary options, /* NOT USED */
            CultureInfo cultureInfo,
            IClientData clientData, /* NOT USED */
            ref MarshalFlags marshalFlags, /* NOT USED */
            ref object value,
            ref Result error
            )
        {
            long longValue = 0;

            if (Value.GetWideInteger2(
                    text, ValueFlags.AnyWideInteger, cultureInfo,
                    ref longValue, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // HACK: Maybe truncate 64-bit pointer value to 32-bit.
            //
            if (PlatformOps.Is64BitProcess())
                value = new IntPtr(longValue);
            else
                value = new IntPtr(ConversionOps.ToInt(longValue));

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestIntPtrChangeTypeCallback(
            Interpreter interpreter,
            bool install,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            IScriptBinder scriptBinder = interpreter.InternalBinder as IScriptBinder;

            if (scriptBinder == null)
            {
                error = "invalid script binder";
                return ReturnCode.Error;
            }

            if (install)
            {
                if (scriptBinder.AddChangeTypeCallback(
                        typeof(IntPtr), TestToIntPtr,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (scriptBinder.RemoveChangeTypeCallback(
                        typeof(IntPtr), ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestCallStaticDynamicCallback0(
            Delegate callback,
            params object[] args
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (TestHasOnlyObjectArrayParameter(callback))
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback.DynamicInvoke(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestCallStaticDynamicCallback1(
            DynamicInvokeCallback callback,
            params object[] args
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (staticDynamicInvoke)
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestCallStaticDynamicCallback2(
            TwoArgsDelegate callback,
            string param1,
            string param2
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (staticDynamicInvoke)
                return (int)callback.DynamicInvoke(new object[] { param1, param2 });
            else
                return callback(param1, param2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestCallStaticDynamicCallback3(
            ThreeArgsDelegate callback,
            object[] args,
            int value,
            ref object data
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (staticDynamicInvoke)
            {
                object[] newArgs = new object[] { args, value, data };

                callback.DynamicInvoke(newArgs);
                data = newArgs[newArgs.Length - 1];
            }
            else
            {
                callback(args, value, ref data);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int? TestCallStaticDynamicCallback4(
            OneArgNullableIntDelegate callback,
            int? value
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (staticDynamicInvoke)
            {
                object[] newArgs = new object[] { value };

                return (int?)callback.DynamicInvoke(newArgs);
            }
            else
            {
                return (int?)callback(value);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IEnumerable<Delegate> TestGetStaticDynamicCallbacks()
        {
            return new Delegate[] {
                new DynamicInvokeCallback(TestDynamicStaticCallback0),
                new DynamicInvokeCallback(TestDynamicStaticCallback1),
                new TwoArgsDelegate(TestDynamicStaticCallback2),
                new ThreeArgsDelegate(TestDynamicStaticCallback3),
                new OneArgNullableIntDelegate(TestDynamicStaticCallback4)
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestDynamicStaticCallback0(
            params object[] args
            )
        {
            return String.Format("static, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestDynamicStaticCallback1(
            object[] args
            )
        {
            return String.Format("static, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestDynamicStaticCallback2(
            string param1,
            string param2
            )
        {
            return SharedStringOps.Compare(param1, param2,
                SharedStringOps.GetBinaryComparisonType(true));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestDynamicStaticCallback3(
            object[] args,
            int value,
            ref object data
            )
        {
            data = String.Format("static, {0}, {1}, {2}",
                TestFormatArgs(args), value, FormatOps.WrapOrNull(data));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int? TestDynamicStaticCallback4(
            int? value
            )
        {
            return (value != null) ? (int)value * 2 : (int?)null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestToHexadecimalString(
            byte[] array
            )
        {
            return ArrayOps.ToHexadecimalString(array);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static MethodInfo TestMethodInfo()
        {
            return typeof(Default).GetMethod("TestMethodInfo");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ChangeTypeCallback TestReturnChangeTypeCallback()
        {
            return TestChangeTypeCallback;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ToStringCallback TestReturnToStringCallback()
        {
            return TestToStringCallback;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestChangeTypeCallback(
            Interpreter interpreter,
            Type type,
            string text,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            ref MarshalFlags marshalFlags,
            ref object value,
            ref Result error
            )
        {
            value = text;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestToStringCallback(
            Interpreter interpreter,
            Type type,
            object value,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            ref MarshalFlags marshalFlags,
            ref string text,
            ref Result error
            )
        {
            text = (value != null) ? value.ToString() : String.Empty;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void StaticMethodWithCallback(
            GetTypeCallback3 callback
            )
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestIdentity(
            object arg
            )
        {
            return HandleOps.Identity(arg);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestTypeIdentity(
            Type arg
            )
        {
            return HandleOps.TypeIdentity(arg);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestStringArray(
            string [] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestArray(
            int[] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestNullArray(
            int[] array
            )
        {
            if (array == null)
                return -1;

            int count = 0;

            foreach (int element in array)
                count += element;

            return count;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestObjectAsArray(
            object input,
            ref object output
            )
        {
            output = new object[] { input, output };
            return (input != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefArray(
            bool create,
            ref int[] array
            )
        {
            if (create)
                array = new int[] { 1, 2, 3 };

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefArray(
            bool create,
            ref string[] array
            )
        {
            if (create)
                array = new string[] { "one", "two", "three" };

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefArray(
            Interpreter interpreter,
            VariableFlags variableFlags,
            string varName,
            string varIndex,
            object varValue,
            bool create,
            ref string[] array
            )
        {
            if ((interpreter != null) && (varName != null))
            {
                ReturnCode code;
                IVariable variable = null; /* NOT USED */
                Result error = null;

                code = interpreter.SetVariableValue2(
                    variableFlags, null, varName, varIndex,
                    varValue, null, ref variable, ref error);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, code, error);
            }

            return TestByRefArray(create, ref array);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestOutArray(
            bool create,
            out int[] array
            )
        {
            if (create)
                array = new int[] { 1, 2, 3 };
            else
                array = null;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestOutArray(
            bool create,
            out string[] array
            )
        {
            if (create)
                array = new string[] { "one", "two", "three" };
            else
                array = null;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestMulti2Array(
            int[,] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestMulti3Array(
            int[, ,] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestNestedArray(
            int[][] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static sbyte TestSByte(
            byte X
            )
        {
            return ConversionOps.ToSByte(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestInt(
            uint X
            )
        {
            return ConversionOps.ToInt(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long TestLong(
            ulong X
            )
        {
            return ConversionOps.ToLong(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ulong TestULong(
            long X
            )
        {
            return ConversionOps.ToULong(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestIntParams(
            params int[] args
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestEnum(
            ReturnCode x
            )
        {
            return (x == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefEnum(
            ref ReturnCode x
            )
        {
            if (x == ReturnCode.Error)
                x = ReturnCode.Break;

            return (x == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestNullableEnum(
            ReturnCode? x
            )
        {
            return (x != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefNullableEnum(
            ref ReturnCode? x
            )
        {
            if ((x != null) && (x == ReturnCode.Error))
                x = ReturnCode.Break;

            return (x != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ByteList TestByteList()
        {
            return new ByteList(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntList TestIntList()
        {
            return new IntList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static LongList TestLongList()
        {
            return new LongList(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DerivedList TestDerivedList()
        {
            return new DerivedList();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringDictionary TestStringDictionary(
            bool enumerable,
            bool keys,
            bool values,
            params string[] args
            )
        {
            if (enumerable)
                return new StringDictionary((IEnumerable<string>)args, keys, values);
            else
                return new StringDictionary(new StringList(args), keys, values);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringPairList TestStringPairList(
            Interpreter interpreter
            )
        {
            Result error = null;

            return AttributeOps.GetObjectIds(
                (interpreter != null) ? interpreter.GetAppDomain() : null, true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringPairList[] TestArrayOfStringPairList()
        {
            return new StringPairList[] {
                new StringPairList(
                    new StringPair(),
                    new StringPair("this is a test #1."),
                    new StringPair("1", "2"),
                    new StringPair("3", "4"),
                    new StringPair("5", "6"),
                    new StringPair("7", "8"),
                    new StringPair("9", "10")),
                new StringPairList(
                    new StringPair(),
                    new StringPair("this is a test #2."),
                    new StringPair("11", "12"),
                    new StringPair("13", "14"),
                    new StringPair("15", "16"),
                    new StringPair("17", "18"),
                    new StringPair("19", "20"))
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Guid TestObjectId(
            Interpreter interpreter
            )
        {
            // ICommand command = null;
            // Result error = null;

            // interpreter.GetCommand("apply", false, true, ref command, ref error);
            // interpreter.GetCommand("apply", true, true, ref command, ref error);

            Guid id = AttributeOps.GetObjectId(typeof(ObjectIdAttribute));

            IInteractiveHost interactiveHost =
                (interpreter != null) ? interpreter.GetInteractiveHost() : null;

            if (interactiveHost != null)
                interactiveHost.Write(id.ToString());

            return id;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestEnsureTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            bool typeOnly,
            ref Result error
            )
        {
            return DebugOps.EnsureTraceListener(
                listeners, listener, typeOnly, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReplaceTraceListener(
            TraceListenerCollection listeners,
            TraceListener oldListener,
            TraceListener newListener,
            bool typeOnly,
            bool dispose,
            ref Result error
            )
        {
            return DebugOps.ReplaceTraceListener(
                listeners, oldListener, newListener, typeOnly,
                dispose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestSetDateTimeNowCallback(
            Interpreter interpreter,
            DateTime dateTime,
            long increment,
            bool enable
            )
        {
            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (!EventOps.ManagerIsOk(eventManager))
                return false;

            if (enable)
            {
                now = dateTime;
                nowCallback = eventManager.NowCallback; /* save */
                nowIncrement = increment;
                eventManager.NowCallback = TestDateTimeNow; /* set */
            }
            else
            {
                now = DateTime.MinValue;
                eventManager.NowCallback = nowCallback; /* restore */
                nowCallback = null; /* clear */
                nowIncrement = 0;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList TestStringListFromString(
            string value
            )
        {
            return StringList.FromString(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool TestPeInformation(
            string fileName,
            ref uint timeStamp,
            ref UIntPtr reserve,
            ref UIntPtr commit
            )
        {
            return FileOps.GetPeFileTimeStamp(fileName, ref timeStamp) &&
                FileOps.GetPeFileStackReserveAndCommit(fileName, ref reserve, ref commit);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetPluginIsolation(
            Interpreter interpreter,
            bool enable
            )
        {
            ReturnCode code;
            Result result = null;

            code = TestSetPluginIsolation(interpreter, enable, ref result);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetPluginIsolation(
            Interpreter interpreter,
            bool enable,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

#if ISOLATED_PLUGINS
            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (Interpreter.IsDeletedOrDisposed(
                        interpreter, false, ref result))
                {
                    return ReturnCode.Error;
                }

                ResultList results = new ResultList();
                PluginFlags pluginFlags = interpreter.PluginFlags;

                results.Add(String.Format(
                    "plugin isolation was {0}", FlagOps.HasFlags(
                    pluginFlags, PluginFlags.Isolated, true) ?
                    "enabled" : "disabled"));

                if (enable)
                    interpreter.EnablePluginIsolation();
                else
                    interpreter.DisablePluginIsolation();

                pluginFlags = interpreter.PluginFlags;

                results.Add(String.Format(
                    "plugin isolation is {0}", FlagOps.HasFlags(
                    pluginFlags, PluginFlags.Isolated, true) ?
                    "enabled" : "disabled"));

                result = results;
                return ReturnCode.Ok;
            }
#else
            result = "plugin isolation is not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestResource(
            Interpreter interpreter
            )
        {
            return ResourceOps.GetString(
                interpreter, ResourceId.Test, 1, TimeOps.GetUtcNow(), "test");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestScriptStream(
            Interpreter interpreter,
            string name,
            string value,
            int length,
            ref Result result
            )
        {
            if (interpreter == null)
                return;

#if false
            IInteractiveHost interactiveHost = interpreter.Host;
#endif

            if (value == null) /* NOTE: Default? */
            {
                if (name == null) name = "TestScriptStream";

                value = "set x 1" + Characters.DosNewLine +
                    Characters.EndOfFile + "abc";
            }

            ReturnCode[] code = { ReturnCode.Ok, ReturnCode.Ok };
            Result[] localResult = { null, null };
            string[] extra = { null, null };

            using (StringReader stringReader = new StringReader(value))
            {
                EngineFlags engineFlags = EngineFlags.ForceSoftEof;
                string text = null;
                bool canRetry = false; /* NOT USED */

                code[0] = Engine.ReadScriptStream(
                    interpreter, name, stringReader, 0,
                    length, ref engineFlags, ref text,
                    ref canRetry, ref localResult[0]);

                if (code[0] == ReturnCode.Ok)
                    localResult[0] = text;

                extra[0] = stringReader.ReadToEnd();
            }

#if false
            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[0], localResult[0]);

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[0], extra[0]);

            using (StringReader stringReader = new StringReader(value))
            {
                EngineFlags engineFlags = EngineFlags.ForceSoftEof;
                string text = null;

                code[1] = Engine.ReadScriptStream(
                    interpreter, name, stringReader, 0,
                    Count.Invalid, ref engineFlags, ref text,
                    ref localResult[1]);

                if (code[1] == ReturnCode.Ok)
                    localResult[1] = text;

                extra[1] = stringReader.ReadToEnd();
            }

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[1], localResult[1]);

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[1], extra[1]);
#endif

            result = StringList.MakeList(code[0], code[1], localResult[0],
                localResult[1], extra[0], extra[1]);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadScriptFile(
            Interpreter interpreter,                 /* in */
            Encoding encoding,                       /* in */
            string fileName,                         /* in */
            EngineFlags engineFlags,                 /* in */
            ref IClientData clientData,              /* out */
            ref Result error                         /* out */
            )
        {
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags |= interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            return TestReadScriptFile(
                interpreter, encoding, fileName, ref engineFlags,
                ref substitutionFlags, ref eventFlags, ref expressionFlags,
                ref clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadScriptFile(
            Interpreter interpreter,                 /* in */
            Encoding encoding,                       /* in */
            string fileName,                         /* in */
            ref EngineFlags engineFlags,             /* in, out */
            ref SubstitutionFlags substitutionFlags, /* in, out */
            ref EventFlags eventFlags,               /* in, out */
            ref ExpressionFlags expressionFlags,     /* in, out */
            ref IClientData clientData,              /* out */
            ref Result error                         /* out */
            )
        {
            ReadScriptClientData readScriptClientData = null;
            bool canRetry = false; /* NOT USED */

            if (Engine.ReadScriptFile(
                    interpreter, encoding, fileName, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref readScriptClientData,
                    ref canRetry, ref error) == ReturnCode.Ok)
            {
                clientData = readScriptClientData;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadPostScriptBytes(
            Interpreter interpreter, /* NOT USED */
            Stream stream,
            long streamLength,
            bool seekSoftEof,
            ref ByteList bytes,
            ref Result error
            )
        {
            try
            {
                Engine.ReadPostScriptBytes(
                    stream.ReadByte, stream.Read, streamLength,
                    seekSoftEof, ref bytes);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSubstituteFile(
            Interpreter interpreter,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            )
        {
            ReturnCode code = Engine.SubstituteFile(
                interpreter, fileName, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ref result);

            IInteractiveHost interactiveHost = interpreter.GetInteractiveHost();

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code, result);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestMatchComparer(
            MatchMode mode,
            bool noCase,
            RegexOptions regExOptions,
            StringList list,
            string value,
            ref bool match,
            ref Result error
            )
        {
            if (list != null)
            {
                if (value != null)
                {
                    try
                    {
                        PathDictionary<int> paths = new PathDictionary<int>(
                            new _Comparers.Match(mode, noCase, regExOptions));

                        paths.Add(list);
                        match = paths.ContainsKey(value);

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid value";
                }
            }
            else
            {
                error = "invalid list";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestExpr( /* LOCAL-ONLY */
            Interpreter interpreter,
            string text,
            ref bool usable,
            ref bool exception,
            ref Result result,
            out Result result2
            )
        {
            Result error; /* REUSED */

            if (interpreter == null)
            {
                error = "invalid interpreter";

                if (result == null)
                    result = String.Empty;

                result.Value = error;
                result2 = error;

                return ReturnCode.Error;
            }

            IParseState parseState = new ParseState(
                interpreter.EngineFlags, interpreter.SubstitutionFlags);

            error = null;

            if (ExpressionParser.ParseExpression(interpreter,
                    text, 0, (text != null) ? text.Length : 0,
                    parseState, true, ref error) != ReturnCode.Ok)
            {
                if (result == null)
                    result = String.Empty;

                result.Value = error;
                result2 = error;

                return ReturnCode.Error;
            }

            int savedExpressionLevels = 0;
            Argument value = null;

            interpreter.PushSubExpression(ref savedExpressionLevels);

            try
            {
                error = null;

                if (ExpressionEvaluator.EvaluateSubExpression(
                        interpreter, parseState, 0,
                        interpreter.EngineFlags,
                        interpreter.SubstitutionFlags,
                        interpreter.EngineEventFlags,
                        interpreter.ExpressionFlags,
#if RESULT_LIMITS
                        interpreter.ExecuteResultLimit,
                        interpreter.NestedResultLimit,
#endif
                        true, AppDomainOps.IsSame(interpreter),
#if DEBUGGER && BREAKPOINTS
                        Engine.HasArgumentLocation(interpreter),
#endif
                        ref usable, ref exception, ref value,
                        ref error) == ReturnCode.Ok)
                {
                    if (result == null)
                        result = String.Empty;

                    result.Value = value;
                    result2 = value;

                    return ReturnCode.Ok;
                }
                else
                {
                    if (result == null)
                        result = String.Empty;

                    result.Value = error;
                    result2 = error;

                    return ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.PopSubExpression(ref savedExpressionLevels);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList TestParseInteger(
            string text,
            int startIndex,
            int characters,
            byte radix,
            bool whiteSpace,
            bool greedy,
            bool unsigned,
            bool legacyOctal
            )
        {
            int endIndex = 0;

            int intValue = Parser.ParseInteger(
                text, startIndex, characters, radix,
                whiteSpace, greedy, unsigned, legacyOctal,
                ref endIndex);

            return new StringList(intValue.ToString(), endIndex.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddNullObject(
            Interpreter interpreter, /* in */
            string name,             /* in: OPTIONAL */
            ref Result result        /* out */
            )
        {
            if (name == null)
                name = "TestAddNullObject";

            return TestAddObject(
                interpreter, name, null, ClientData.Empty,
                ObjectOps.GetDefaultObjectFlags(), 1, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddObject(
            Interpreter interpreter, /* in */
            string name,             /* in: OPTIONAL */
            object value,            /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            ObjectFlags? flags,      /* in */
            int referenceCount,      /* in */
            ref Result result        /* out */
            )
        {
            long token = 0;

            return TestAddObject(
                interpreter, name, value, clientData, flags,
                referenceCount, ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddObject(
            Interpreter interpreter,  /* in */
            string name,              /* in: OPTIONAL */
            object value,             /* in: OPTIONAL */
            IClientData clientData,   /* in: OPTIONAL */
            ObjectFlags? objectFlags, /* in: OPTIONAL */
            int referenceCount,       /* in */
            ref long token,           /* out */
            ref Result result         /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (clientData == null)
                clientData = ClientData.Empty;

            if (objectFlags == null)
                objectFlags = ObjectOps.GetDefaultObjectFlags();

#if DEBUGGER && DEBUGGER_ARGUMENTS
            ArgumentList executeArguments =
                Engine.GetDebuggerExecuteArguments(
                    interpreter);
#endif

            return interpreter.AddObject(
                name, null, (ObjectFlags)objectFlags, clientData,
                referenceCount,
#if NATIVE && TCL
                null,
#endif
#if DEBUGGER && DEBUGGER_ARGUMENTS
                executeArguments,
#endif
                value, ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ResultList TestAddFunction(
            Interpreter interpreter
            )
        {
            ResultList results = new ResultList();
            long token = 0;

            ReturnCode code;
            Result result = null;

            code = interpreter.AddFunction(
                typeof(_Tests.Default), "foo", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.AddFunction(
                typeof(_Functions.Min), "bar", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.AddFunction(
                typeof(_Functions.Min), "eq", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.AddFunction(
                typeof(_Functions.Min), "eqq", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            return results;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddNamedFunction(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref Result result
            )
        {
            long token = 0;

            return interpreter.AddFunction(
                typeof(_Tests.Default.Function), name, (int)Arity.None,
                null, FunctionFlags.ForTestUse, null, clientData, true,
                ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddNamedFunction2(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref Result result
            )
        {
            long token = 0;

            return interpreter.AddFunction(
                typeof(_Tests.Default.Function2), name, (int)Arity.Automatic,
                null, FunctionFlags.ForTestUse, null, clientData, true,
                ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddNamedFunction3(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref Result result
            )
        {
            long token = 0;

            return interpreter.AddFunction(
                typeof(_Tests.Default.Function3), name, (int)Arity.Automatic,
                null, FunctionFlags.ForTestUse, null, clientData, true,
                ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ResultList TestRemoveFunction(
            Interpreter interpreter
            )
        {
            ResultList results = new ResultList();

            ReturnCode code;
            Result result = null;

            code = interpreter.RemoveFunction("foo", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.RemoveFunction("bar", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.RemoveFunction("eq", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.RemoveFunction("eqq", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            return results;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRemoveNamedFunction(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            Result result
            )
        {
            return interpreter.RemoveFunction(name, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestWriteBox(
            Interpreter interpreter,
            string value,
            bool multiple,
            bool newLine,
            bool restore,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                IDisplayHost displayHost = interpreter.Host;

                if (displayHost != null)
                {
                    try
                    {
                        bool positioning = FlagOps.HasFlags(
                            displayHost.GetHostFlags(), HostFlags.Positioning,
                            true);

                        int left = 0;
                        int top = 0;

                        if (!positioning ||
                            displayHost.GetPosition(ref left, ref top))
                        {
                            ConsoleColor foregroundColor = _ConsoleColor.None;
                            ConsoleColor backgroundColor = _ConsoleColor.None;

                            if (displayHost.GetColors(
                                    null, "TestInfo", true, true, ref foregroundColor,
                                    ref backgroundColor, ref error) == ReturnCode.Ok)
                            {
                                if (multiple)
                                {
                                    StringList list = null;

                                    if (ParserOps<string>.SplitList(
                                            interpreter, value, 0, Length.Invalid, true,
                                            ref list, ref error) == ReturnCode.Ok)
                                    {
                                        if (displayHost.WriteBox(
                                                null, new StringPairList(list), null,
                                                newLine, restore, ref left, ref top,
                                                foregroundColor, backgroundColor))
                                        {
                                            return true;
                                        }
                                        else
                                        {
                                            error = "could not write box to interpreter host";
                                        }
                                    }
                                }
                                else
                                {
                                    if (displayHost.WriteBox(
                                            null, value, null, newLine, restore,
                                            ref left, ref top, foregroundColor,
                                            backgroundColor))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        error = "could not write box to interpreter host";
                                    }
                                }
                            }
                        }
                        else
                        {
                            error = "could not get interpreter host position";
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "interpreter host not available";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool EnableWriteCustomInfo(
            Interpreter interpreter,
            bool enable,
            StringPairList list
            )
        {
            if (interpreter == null)
                return false;

#if CONSOLE
            _Hosts.Console consoleHost = interpreter.Host as _Hosts.Console;

            if (consoleHost == null)
                return false;

            consoleHost.EnableTests(enable);
#endif

            if (enable)
            {
                if (list != null)
                {
                    customInfoList = new StringPairList(list);
                }
                else
                {
                    customInfoList = new StringPairList(
                        new StringPair("Custom"), null,
                        new StringPair("name0", null),
                        new StringPair("name1", String.Empty),
                        new StringPair("name2", "value1"),
                        new StringPair("name3", TimeOps.GetUtcNow().ToString()),
                        new StringPair("name4", (interpreter.Random != null) ?
                            interpreter.Random.Next().ToString() : "0"));
                }
            }
            else
            {
                customInfoList = null;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestWriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            if (interpreter == null)
                return false;

            _Hosts.Default defaultHost = interpreter.Host as _Hosts.Default;

            if (defaultHost == null)
                return false;

            int hostLeft = 0;
            int hostTop = 0;

            if (!defaultHost.GetDefaultPosition(ref hostLeft, ref hostTop))
                return false;

            try
            {
                OutputStyle outputStyle = defaultHost.OutputStyle;
                StringPairList list = new StringPairList(customInfoList);

                if (defaultHost.IsBoxedOutputStyle(outputStyle))
                {
                    return defaultHost.WriteBox(
                        TestCustomInfoBoxName, list, null, false, true,
                        ref hostLeft, ref hostTop, foregroundColor,
                        backgroundColor);
                }
                else if (defaultHost.IsFormattedOutputStyle(outputStyle))
                {
                    return defaultHost.WriteFormat(
                        list, newLine, foregroundColor, backgroundColor);
                }
                else if (defaultHost.IsNoneOutputStyle(outputStyle))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                defaultHost.SetDefaultPosition(hostLeft, hostTop);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestCheckCopyErrorInformation(
            Interpreter interpreter,
            ReturnCode code,
            bool strict,
            ref Result errorCode,
            ref Result errorInfo
            )
        {
            if ((interpreter != null) && (code == ReturnCode.Error))
            {
                //
                // BUGFIX: Now, prevent blocking forever when copying the
                //         error information by using the TryLock method.
                //         If it succeeds, any internal locking performed
                //         by the CopyErrorInformation method is harmless
                //         -OR- if it fails, we just avoid blocking.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        ResultList errors = null;

                        if (interpreter.InternalCopyErrorInformation(
                                VariableFlags.None, strict, ref errorCode,
                                ref errorInfo, ref errors) == ReturnCode.Ok)
                        {
                            return true;
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "Failed to copy error information: {0}",
                                errors), typeof(Default).Name,
                                TracePriority.EngineError2);
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "Could not lock interpreter to copy error information",
                            typeof(Default).Name, TracePriority.EngineError2);
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestVerifyBuiltInCommands(
            Interpreter interpreter, /* in */
            ref ResultList results   /* in, out */
            )
        {
            if (interpreter == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            IPlugin plugin = interpreter.NewCorePlugin();

            if (plugin == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("could not create core plugin");
                return ReturnCode.Error;
            }

            Result error; /* REUSED */
            IRuleSet ruleSet = interpreter.GetRuleSet();
            CommandDataList list1 = new CommandDataList();

            plugin.Commands = list1;
            error = null;

            if (_RuntimeOps.PopulatePluginEntities(
                    interpreter, plugin, null, ruleSet,
                    PluginFlags.None, null, false,
                    false, true, ref error) != ReturnCode.Ok)
            {
                if (error != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(error);
                }

                return ReturnCode.Error;
            }

            CommandDataList list2 = new CommandDataList();

            plugin.Commands = list2;
            error = null;

            if (_RuntimeOps.PopulatePluginEntities(
                    interpreter, plugin, null, ruleSet,
                    PluginFlags.None, null, true,
                    false, true, ref error) != ReturnCode.Ok)
            {
                if (error != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(error);
                }

                return ReturnCode.Error;
            }

            return TestCompareCommandDataLists(list1, list2, ref results);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestVerifyBuiltInFunctions(
            Interpreter interpreter, /* in */
            ref ResultList results   /* in, out */
            )
        {
            if (interpreter == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            bool standard;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                standard = interpreter.InternalIsStandard();
            }

            IPlugin plugin = interpreter.NewCorePlugin();

            if (plugin == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("could not create core plugin");
                return ReturnCode.Error;
            }

            Result error; /* REUSED */
            List<IFunctionData> list1 = null;

            error = null;

            if (_RuntimeOps.GetPluginFunctions(
                    plugin, null, PluginFlags.None, standard,
                    ref list1, ref error) != ReturnCode.Ok)
            {
                if (error != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(error);
                }

                return ReturnCode.Error;
            }

            List<IFunctionData> list2 = null;

            error = null;

            if (_RuntimeOps.GetBuiltInFunctions(
                    plugin, PluginFlags.None, standard,
                    ref list2, ref error) != ReturnCode.Ok)
            {
                if (error != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(error);
                }

                return ReturnCode.Error;
            }

            return TestCompareFunctionDataLists(list1, list2, ref results);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestVerifyBuiltInOperators(
            Interpreter interpreter, /* in */
            ref ResultList results   /* in, out */
            )
        {
            if (interpreter == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            InterpreterFlags interpreterFlags;
            bool standard;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                interpreterFlags = interpreter.InterpreterFlagsNoLock;
                standard = interpreter.InternalIsStandard();
            }

            IPlugin plugin = interpreter.NewCorePlugin();

            if (plugin == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("could not create core plugin");
                return ReturnCode.Error;
            }

            Result error; /* REUSED */
            List<IOperatorData> list1 = null;

            error = null;

            if (_RuntimeOps.GetPluginOperators(
                    plugin, null, StringOps.GetComparisonType(
                    interpreterFlags, false), PluginFlags.None,
                    standard, ref list1, ref error) != ReturnCode.Ok)
            {
                if (error != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(error);
                }

                return ReturnCode.Error;
            }

            List<IOperatorData> list2 = null;

            error = null;

            if (_RuntimeOps.GetBuiltInOperators(
                    plugin, StringOps.GetComparisonType(
                    interpreterFlags, false), PluginFlags.None,
                    standard, ref list2, ref error) != ReturnCode.Ok)
            {
                if (error != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(error);
                }

                return ReturnCode.Error;
            }

            return TestCompareOperatorDataLists(list1, list2, ref results);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Requires C# 4.0
#if NET_40
        public ReturnCode TestOptionalParameter0(
            ref Result result,
            string one = null
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter0_1", one);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter0(
            ref Result result,
            string one = null,
            int two = 0
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter0_2", one, two);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter1(
            ref Result result,
            string one,
            string two = null
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter1_1", one, two);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter1(
            ref Result result,
            string one,
            string two = null,
            int three = 0
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter1_2", one, two, three);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter2(
            ref Result result,
            string one,
            string two = "two1"
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter2_1", one, two);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter2(
            ref Result result,
            string one,
            string two = "two2",
            int three = int.MaxValue
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter2_2", one, two, three);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter2(
            ref Result result,
            string one,
            string two = "two3",
            int three = int.MaxValue - 1,
            params string[] more
            )
        {
            CheckDisposed();

            StringList list = new StringList(
                "TestOptionalParameter2_3", one, two,
                three.ToString());

            if (more != null)
                foreach (string item in more)
                    list.Add(item);

            result = list;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameterZ(
            ref Result result,
            Guid guid = default(Guid)
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameterZ", guid);

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        public void TestSetScriptComplainCallback(
            Interpreter interpreter,
            string commandName,
            bool setup,
            bool withThrow
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            complainCommandName = commandName;
            complainWithThrow = withThrow;

            Interpreter.ComplainCallback = setup ?
                (ComplainCallback)TestScriptComplainCallback : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for SleepWaitCallback
        public ReturnCode TestSetSleepWaitCallback(
            Interpreter interpreter,
            string text,
            bool setup,
            ref Result error
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            sleepWaitCallbackText = text;

            return TestSetSleepWaitCallback(interpreter,
                setup ? (SleepWaitCallback)TestSleepWaitCallback : null,
                ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for GetTempFileNameCallback
        public ReturnCode TestSetupGetTempFileNameCallback(
            bool setup,
            bool? exception,
            ref Result error
            )
        {
            CheckDisposed();

            if (exception != null)
                tempException = (bool)exception;

            return TestChangePathCallback(
                PathCallbackType.GetTempFileName, setup ?
                    (GetStringValueCallback)TestGetTempFileNameCallback :
                    null, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for GetTempPathCallback
        public ReturnCode TestSetupGetTempPathCallback(
            bool setup,
            bool? exception,
            ref Result error
            )
        {
            CheckDisposed();

            if (exception != null)
                tempException = (bool)exception;

            return TestChangePathCallback(
                PathCallbackType.GetTempPath, setup ?
                    (GetStringValueCallback)TestGetTempPathCallback :
                    null, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewInterpreterCallback
        public void TestSetNewInterpreterCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            newInterpreterText = text;

            Interpreter.NewInterpreterCallback = setup ?
                (EventCallback)TestNewInterpreterCallback : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestPublicInstanceNewInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            CheckDisposed();

            string formatted = String.Format(
                "TestPublicInstanceNewInterpreterCallback: interpreter = {0}",
                FormatOps.InterpreterNoThrow(interpreter));

            if (calledMethods == null)
                calledMethods = StringOps.NewStringBuilder();

            calledMethods.AppendLine(formatted);

            TraceOps.DebugTrace(
                formatted, typeof(Default).Name, TracePriority.Highest);

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for UseInterpreterCallback
        public void TestSetUseInterpreterCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            useInterpreterText = text;

            Interpreter.UseInterpreterCallback = setup ?
                (EventCallback)TestUseInterpreterCallback : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for FreeInterpreterCallback
        public void TestSetFreeInterpreterCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            freeInterpreterText = text;

            Interpreter.FreeInterpreterCallback = setup ?
                (EventCallback)TestFreeInterpreterCallback : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewCommandCallback
        public void TestSetNewCommandCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            newCommandText = text;

            interpreter.NewCommandCallback = setup ?
                (NewCommandCallback)TestNewCommandCallback : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for UnknownCallback
        public void TestSetUnknownSpyCallback(
            Interpreter interpreter,
            bool setup
            )
        {
            CheckDisposed();

            if (interpreter == null)
                return;

            interpreter.UnknownCallback = setup ?
                (UnknownCallback)TestUnknownSpyCallback :
                null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestSetUnknownScriptObjectCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            if (interpreter == null)
                return;

            interpreter.UnknownCallback = setup ?
                (UnknownCallback)TestUnknownScriptObjectCallback :
                null;

            unknownCallbackText = text;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestSetUnknownScriptCommandCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            if (interpreter == null)
                return;

            interpreter.UnknownCallback = setup ?
                (UnknownCallback)TestUnknownScriptCommandCallback :
                null;

            unknownCallbackText = text;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestSetUnknownObjectInvokeCallback(
            Interpreter interpreter,
            bool setup
            )
        {
            CheckDisposed();

            if (interpreter == null)
                return;

            interpreter.UnknownCallback = setup ?
                (UnknownCallback)TestUnknownObjectInvokeCallback :
                null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for PackageCallback
        public void TestSetPackageFallbackCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            if (interpreter == null)
                return;

            packageFallbackText = text;

            if (setup)
                interpreter.PackageFallback = TestPackageFallbackCallback;
            else
                interpreter.PackageFallback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for AddSubCommands
        public ReturnCode TestAddInstanceSubCommands(
            Interpreter interpreter,
            string name,
            DelegateFlags delegateFlags,
            ref long token,
            ref Result result
            )
        {
            CheckDisposed();

            return TestAddInstanceSubCommands(
                interpreter, name, typeof(Default), this,
                delegateFlags, ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestAddInstanceSubCommands(
            Interpreter interpreter,
            string name,
            Type type,
            object @object,
            DelegateFlags delegateFlags,
            ref long token,
            ref Result result
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            IPlugin plugin =
#if TEST_PLUGIN || DEBUG
                    interpreter.GetTestPlugin(ref result);
#else
                    interpreter.GetCorePlugin(ref result);
#endif

            if (plugin == null)
                return ReturnCode.Error;

            return interpreter.AddSubCommands(
                name, type, @object, plugin, ClientData.Empty,
                TestNewDelegateNameCallback, delegateFlags |
                DelegateFlags.PublicInstanceMask, ref token,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            if (uniqueToString)
                return GlobalState.NextId().ToString();

            if (idToString)
                return id.ToString();

            return base.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestResizeMiscellaneousData(
            int newSize
            )
        {
            Array.Resize(ref miscellaneousData, newSize);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestGeneric<T>(
            T value,
            bool typeOnly
            )
        {
            CheckDisposed();

            if (typeOnly)
            {
                return (value != null) ? value.GetType() : typeof(object);
            }
            else
            {
                return Result.FromObject(value, true, false, false);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestObjectIdentity(
            object value
            )
        {
            CheckDisposed();

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default TestReturnOfSelf(
            bool useNull
            )
        {
            CheckDisposed();

            return useNull ? null : this;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void MethodWithCallback(
            GetTypeCallback3 callback
            )
        {
            CheckDisposed();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int[] TestIntArrayReturnValue()
        {
            CheckDisposed();

            return new int[] { 1, 2, 3, 4, 5 };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string[] TestStringArrayReturnValue()
        {
            CheckDisposed();

            return new string[] { "1", "2", "joe", "jim", "tom" };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringList TestStringListReturnValue()
        {
            CheckDisposed();

            return new StringList(TestStringArrayReturnValue());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IList<string> TestStringIListReturnValue(
            bool useCustom,
            params string[] strings
            )
        {
            CheckDisposed();

            return useCustom ?
                new StringList(strings) : new List<string>(strings);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IList<IList<IList<string>>> TestStringIListIListIListReturnValue(
            bool useCustom,
            params string[] strings
            )
        {
            CheckDisposed();

            if (useCustom)
            {
                IList<string> list = new StringList(strings);

                IList<IList<string>> list2 = (IList<IList<string>>)
                    new GenericList<IList<string>>(list, list, list);

                IList<IList<IList<string>>> list3 = (IList<IList<IList<string>>>)
                    new GenericList<IList<IList<string>>>(list2, list2, list2);

                return list3;
            }
            else
            {
                IList<string> list = new List<string>(strings);

                IList<IList<string>> list2 = (IList<IList<string>>)
                    new List<IList<string>>(new IList<string>[] { list, list, list });

                IList<IList<IList<string>>> list3 = (IList<IList<IList<string>>>)
                    new List<IList<IList<string>>>(new IList<IList<string>>[] { list2, list2, list2 });

                return list3;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringList[] TestStringListArrayReturnValue()
        {
            CheckDisposed();

            return new StringList[] {
                TestStringListReturnValue(), new StringList("hello world"),
                new StringList(";"), new StringList("\\"), new StringList("{")
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IDictionary<string, string> TestStringIDictionaryReturnValue(
            bool useCustom,
            params string[] strings
            )
        {
            CheckDisposed();

            if (useCustom)
            {
                IList<string> list = new StringList(strings);

                return new GenericDictionary<string, string>(list);
            }
            else
            {
                IList<string> list = new List<string>(strings);

                return new Dictionary<string, string>(
                    new GenericDictionary<string, string>(list));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefByteArray(
            int size,
            ref byte[] byteArray
            )
        {
            CheckDisposed();

            byteArray = new byte[size];

            for (int index = 0; index < size; index++)
                byteArray[index] = (byte)(index & byte.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestTwoByteArrays(
            Interpreter interpreter,
            bool randomize,
            byte[] inByteArray,
            ref byte[] outByteArray,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (inByteArray == null)
            {
                error = "invalid input byte array";
                return ReturnCode.Error;
            }

            if (randomize)
            {
                byte[] randomByteArray = new byte[inByteArray.Length];
                Random random = interpreter.Random;

                if (random != null)
                    random.NextBytes(randomByteArray);

                outByteArray = new byte[inByteArray.Length];

                for (int index = 0; index < outByteArray.Length; index++)
                {
                    outByteArray[index] = ConversionOps.ToByte(
                        inByteArray[index] ^ randomByteArray[index]);
                }
            }
            else
            {
                outByteArray = new byte[inByteArray.Length];

                for (int index = 0; index < outByteArray.Length; index++)
                    outByteArray[index] = inByteArray[index];
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public byte[] TestReturnByteArray(
            byte[] array
            )
        {
            CheckDisposed();

            //
            // WARNING: DO NOT REMOVE.  This is used by the unit tests to
            //          convert a Tcl array into a Tcl list via the Eagle
            //          marshaller.
            //
            return array;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefStringListArray(
            ref StringList[] list
            )
        {
            CheckDisposed();

            list = TestStringListArrayReturnValue();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default TestComplexMethod(
            sbyte w,
            int x,
            bool y,
            double z,
            ref int[] t1,
            ref int[,] t2,
            ref string[] t3,
            out string t4,
            out string[] t5
            )
        {
            CheckDisposed();

            int[][] t6 = null;

            return TestComplexMethod(
                w, x, y, z, ref t1, ref t2, ref t3, out t4, out t5, out t6);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefNullableValueTypeMethod(
            ref int? x
            )
        {
            CheckDisposed();

            if (x != null) x++;
            else x = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestNullableValueTypeMethod(
            int? x
            )
        {
            CheckDisposed();

            return x;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefValueTypeMethod(
            ref int x
            )
        {
            CheckDisposed();

            x++;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int TestRanges(
            bool a,
            sbyte b,
            byte c,
            char d,
            short e,
            ushort f,
            int g,
            uint h,
            long i,
            ulong j,
            decimal k,
            float l,
            double m
            )
        {
            CheckDisposed();

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public char TestCharacterMethod(
            char x
            )
        {
            CheckDisposed();

            return x;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefCharacterArrayMethod(
            ref char[] x
            )
        {
            CheckDisposed();

            x = new char[] { 'f', 'o', 'o' };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string TestGetPrivateField()
        {
            CheckDisposed();

            return privateField;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default TestComplexMethod(
            sbyte w,
            int x,
            bool y,
            double z,
            ref int[] t1,
            ref int[,] t2,
            ref string[] t3,
            out string t4,
            out string[] t5,
            out int[][] t6
            )
        {
            CheckDisposed();

            if (y)
                t1[0]++;

            if (z > 1)
            {
#if !MONO_BUILD
                t2[-1, -2] += 20;
#endif

                t2[0, 0]++;
                t2[0, 1] *= 2;
                t2[1, 0]--;
                t2[1, 1] /= 2;
                t2[2, 1] += 21;
            }

            if (x > 0)
                //
                // BUGFIX: We do not want to complicate the test case to account
                //         for negative numbers here; therefore, just use the
                //         absolute value.
                //
                t3[0] = Math.Abs(Environment.TickCount).ToString();

            //
            // BUGFIX: Cannot be locale-specific here.
            //
            t4 = FormatOps.Iso8601DateTime(TimeOps.GetUtcNow(), true);

            t5 = new string[] {
                w.ToString(), x.ToString(), y.ToString(), z.ToString()
            };

            t6 = new int[][] {
                new int[] { 0, 1, 2 }, new int[] { 2, 4, 6 }, new int[] { 8, 16, 32 }
            };

            return new Default(w);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !MONO_BUILD
        public int[,] TestMoreComplexMethod(
            bool y,
            ref int[,] t7
            )
        {
            CheckDisposed();

            if (y)
            {
                t7 = (int[,])Array.CreateInstance(
                    typeof(int), new int[] { 4, 5 }, new int[] { -6, 6 });
            }
            else
            {
                t7[-6, 6] += -6 * 6;
                t7[-6, 7] += -6 * 7;
                t7[-6, 8] += -6 * 8;
                t7[-6, 9] += -6 * 9;
                t7[-6, 10] += -6 * 10;

                t7[-5, 6] += -5 * 6;
                t7[-5, 7] += -5 * 7;
                t7[-5, 8] += -5 * 8;
                t7[-5, 9] += -5 * 9;
                t7[-5, 10] += -5 * 10;

                t7[-4, 6] += -4 * 6;
                t7[-4, 7] += -4 * 7;
                t7[-4, 8] += -4 * 8;
                t7[-4, 9] += -4 * 9;
                t7[-4, 10] += -4 * 10;

                t7[-3, 6] += -3 * 6;
                t7[-3, 7] += -3 * 7;
                t7[-3, 8] += -3 * 8;
                t7[-3, 9] += -3 * 9;
                t7[-3, 10] += -3 * 10;
            }

            return t7;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestSetupIntArray(
            bool reverse
            )
        {
            CheckDisposed();

            if (intArrayField == null)
                throw new InvalidOperationException();

            int length = intArrayField.Length;

            for (int index = 0; index < length; index++)
            {
                intArrayField[index] = reverse ?
                    ((length - 1) - index) : index;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestSetVariableEnumerable(
            Interpreter interpreter,
            string name,
            bool autoReset,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;
                ReturnCode setCode;
                Result setError = null;

                setCode = interpreter.SetVariableEnumerable(
                    VariableFlags.None, name, intArrayField, autoReset,
                    ref setError);

                if (setCode != ReturnCode.Ok)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(setError);

                    if (!quiet)
                        DebugOps.Complain(interpreter, setCode, setError);
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestUnsetVariableEnumerable(
            Interpreter interpreter,
            string name,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;
                ReturnCode unsetCode;
                Result unsetError = null;

                unsetCode = interpreter.UnsetVariable(
                    VariableFlags.None, name, ref unsetError);

                if (unsetCode != ReturnCode.Ok)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(unsetError);

                    if (!quiet)
                        DebugOps.Complain(interpreter, unsetCode, unsetError);
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestSetVariableLinks(
            Interpreter interpreter,
            string stringName,
            string objectName,
            string integerName,
            string propertyName,
            bool useString,
            bool useObject,
            bool useInteger,
            bool useProperty,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                BindingFlags bindingFlags; /* REUSED */

                bindingFlags = ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PrivateInstanceGetField,
                    true);

                ResultList errors = null;

                if (useString)
                {
                    FieldInfo fieldInfo = null;

                    try
                    {
                        fieldInfo = GetType().GetField(
                            "privateField", bindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (fieldInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, stringName, fieldInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (useObject)
                {
                    FieldInfo fieldInfo = null;

                    try
                    {
                        fieldInfo = GetType().GetField(
                            "objectField", bindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (fieldInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, objectName, fieldInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (useInteger)
                {
                    FieldInfo fieldInfo = null;

                    try
                    {
                        bindingFlags = ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PublicInstanceGetField,
                            true);

                        fieldInfo = GetType().GetField(
                            "intField", bindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (fieldInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, integerName, fieldInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (useProperty)
                {
                    PropertyInfo propertyInfo = null;

                    try
                    {
                        bindingFlags = ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PublicInstanceGetProperty,
                            true);

                        propertyInfo = GetType().GetProperty(
                            "SimpleIntProperty", bindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (propertyInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, propertyName, propertyInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestUnsetVariableLinks(
            Interpreter interpreter,
            string stringName,
            string objectName,
            string integerName,
            string propertyName,
            bool useString,
            bool useObject,
            bool useInteger,
            bool useProperty,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;

                if (useString)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, stringName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useObject)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, objectName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useInteger)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, integerName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useProperty)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, propertyName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestSetVariableSystemArray(
            Interpreter interpreter,
            string intPtrName,
            string objectName,
            bool useIntPtr,
            bool useObject,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;

                if (useIntPtr)
                {
                    ReturnCode setCode;
                    Result setError = null;

                    setCode = interpreter.SetVariableSystemArray(
                        VariableFlags.None, intPtrName, intPtrArrayField,
                        ref error);

                    if (setCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(setError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, setCode, setError);
                    }
                }

                if (useObject)
                {
                    ReturnCode setCode;
                    Result setError = null;

                    setCode = interpreter.SetVariableSystemArray(
                        VariableFlags.None, objectName, objectArrayField,
                        ref error);

                    if (setCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(setError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, setCode, setError);
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestUnsetVariableSystemArray(
            Interpreter interpreter,
            string intPtrName,
            string objectName,
            bool useIntPtr,
            bool useObject,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;

                if (useIntPtr)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, intPtrName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useObject)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, objectName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestSetVariableWithTypedValue(
            Interpreter interpreter,
            VariableFlags flags,
            string varName,
            object varValue,
            Type type,
            bool nonPublic,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (varValue != null)
            {
                return interpreter.SetVariableValue2(
                    flags, varName, varValue, null,
                    ref error);
            }
            else if (type != null)
            {
                try
                {
                    varValue = TestCreateInstance(
                        type, nonPublic);

                    return interpreter.SetVariableValue2(
                        flags, varName, varValue, null,
                        ref error);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "must specify valid value or type";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestTakeEventHandler(
            EventHandler eventHandler,
            object sender,
            EventArgs e
            )
        {
            CheckDisposed();

            if (eventHandler != null)
                eventHandler(sender, e);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestTakeGenericEventHandler<TEventArgs>(
            EventHandler<TEventArgs> eventHandler,
            object sender,
            TEventArgs e
            ) where TEventArgs : EventArgs
        {
            CheckDisposed();

            if (eventHandler != null)
                eventHandler(sender, e);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestTakeResolveEventHandler(
            EventHandler<ResolveEventArgs> eventHandler,
            object sender,
            ResolveEventArgs e
            )
        {
            CheckDisposed();

            if (eventHandler != null)
                eventHandler(sender, e);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestEvaluateAsyncCallback(
            IAsynchronousContext context
            )
        {
            CheckDisposed();

            if (context != null)
                //
                // NOTE: Capture async result.
                //
                asyncResult = ResultOps.Format(
                    context.ReturnCode, context.Result, context.ErrorLine);

            if (@event != null)
                ThreadOps.SetEvent(@event);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestEvaluateHelper(
            Interpreter interpreter,
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ObjectFlags objectFlags,
            bool synchronous,
            bool dispose,
            ref Result result,
            params object[] args
            )
        {
            CheckDisposed();

            ObjectDictionary objects = null;

            if (args != null)
            {
                int length = args.Length;

                objects = new ObjectDictionary(length);

                for (int index = 0; index < length; index++)
                {
                    objects.Add(String.Format(
                        "parameter{0}", index + 1), args[index]);
                }
            }

            if (callback != null) /* NOTE: Asynchronous? */
            {
                ReturnCode code;
                Result error = null;

                code = Helpers.EvaluateScript(
                    interpreter, text, callback, clientData,
                    objects, objectFlags, synchronous, dispose,
                    ref error);

                if (code != ReturnCode.Ok)
                    result = error;

                return code;
            }
            else
            {
                return Helpers.EvaluateScript(
                    interpreter, text, objects, objectFlags,
                    synchronous, dispose, ref result);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestEvaluateAsync(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            int timeout,
            ref Result result
            )
        {
            CheckDisposed();

            int preDisposeContextCount = 0;
            int postDisposeContextCount = 0;

            return TestEvaluateAsync(
                interpreter, text, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, timeout, ref preDisposeContextCount,
                ref postDisposeContextCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestEvaluateAsync(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            int timeout,
            ref int preDisposeContextCount,
            ref int postDisposeContextCount,
            ref Result result
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                if (Engine.EvaluateScript(
                        interpreter, text, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, TestEvaluateAsyncCallback,
                        null, ref result) == ReturnCode.Ok)
                {
                    if (@event != null)
                    {
                        if (timeout == _Timeout.Infinite)
                            timeout = interpreter.InternalTimeout;

                        if (ThreadOps.WaitEvent(@event, timeout))
                        {
                            //
                            // HACK: Wait a bit for the EngineThreadStart
                            //       method to invoke MaybeDisposeThread
                            //       on the interpreter.  It should not be
                            //       too long as the callback has already
                            //       completed.
                            //
                            if (timeout > 0)
                            {
                                HostOps.ThreadSleepOrMaybeComplain(
                                    timeout, false);
                            }

#if THREADING
                            //
                            // NOTE: Return the context counts that should
                            //       have been last updated when the context
                            //       manager was purged from the thread-pool
                            //       thread.
                            //
                            interpreter.QueryDisposeContextCounts(
                                ref preDisposeContextCount,
                                ref postDisposeContextCount);
#endif

                            //
                            // NOTE: Return async result.
                            //
                            result = asyncResult;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            result = String.Format(
                                "waited {0} milliseconds for script to complete",
                                timeout);
                        }

                        //
                        // NOTE: Reset event for next time.
                        //
                        ThreadOps.ResetEvent(@event);
                    }
                    else
                    {
                        //
                        // NOTE: No event is setup, skip waiting.
                        //
                        return ReturnCode.Ok;
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IAnyPair<int, long> TestPermute(
            IList<string> list,
            ListTransformCallback callback
            )
        {
            CheckDisposed();

            if (callback == null)
                callback = TestListTransformCallback;

            intField = 0;
            longField = 0;

            ListOps.Permute(list, callback);

            return new AnyPair<int, long>(intField, longField);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestCallDynamicCallback0(
            Delegate callback,
            params object[] args
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (TestHasOnlyObjectArrayParameter(callback))
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback.DynamicInvoke(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestCallDynamicCallback1(
            DynamicInvokeCallback callback,
            params object[] args
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (dynamicInvoke)
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int TestCallDynamicCallback2(
            TwoArgsDelegate callback,
            string param1,
            string param2
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (dynamicInvoke)
                return (int)callback.DynamicInvoke(new object[] { param1, param2 });
            else
                return callback(param1, param2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestCallDynamicCallback3(
            ThreeArgsDelegate callback,
            object[] args,
            int value,
            ref object data
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (dynamicInvoke)
            {
                object[] newArgs = new object[] { args, value, data };

                callback.DynamicInvoke(newArgs);
                data = newArgs[newArgs.Length - 1];
            }
            else
            {
                callback(args, value, ref data);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int? TestCallDynamicCallback4(
            OneArgNullableIntDelegate callback,
            int? value
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (dynamicInvoke)
            {
                object[] newArgs = new object[] { value };

                return (int?)callback.DynamicInvoke(newArgs);
            }
            else
            {
                return (int?)callback(value);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IEnumerable<Delegate> TestGetDynamicCallbacks()
        {
            CheckDisposed();

            return new Delegate[] {
                new DynamicInvokeCallback(TestDynamicCallback0),
                new DynamicInvokeCallback(TestDynamicCallback1),
                new TwoArgsDelegate(TestDynamicCallback2),
                new ThreeArgsDelegate(TestDynamicCallback3),
                new OneArgNullableIntDelegate(TestDynamicCallback4)
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestDynamicCallback0(
            params object[] args
            )
        {
            CheckDisposed();

            return String.Format("instance, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestDynamicCallback1(
            object[] args
            )
        {
            CheckDisposed();

            return String.Format("instance, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int TestDynamicCallback2(
            string param1,
            string param2
            )
        {
            CheckDisposed();

            return SharedStringOps.Compare(param1, param2,
                SharedStringOps.GetBinaryComparisonType(true));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestDynamicCallback3(
            object[] args,  /* in */
            int value,      /* in */
            ref object data /* in, out */
            )
        {
            CheckDisposed();

            data = String.Format("instance, {0}, {1}, {2}",
                TestFormatArgs(args), value, FormatOps.WrapOrNull(data));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int? TestDynamicCallback4(
            int? value
            )
        {
            CheckDisposed();

            return (value != null) ? (int)value * 2 : (int?)null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool TestListTransformCallback(
            IList<string> list
            )
        {
            // CheckDisposed();

            if (list == null)
                return true;

            string value = list.ToString();

            longField ^= value.GetHashCode();
            intField++;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        private void TestScriptComplainCallback(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            // CheckDisposed();

            if ((callbackInterpreter != null) &&
                !String.IsNullOrEmpty(complainCommandName))
            {
                StringList list = new StringList(complainCommandName,
                    FormatOps.Complaint(id, code, result, stackTrace));

                complainCode = callbackInterpreter.EvaluateScript(
                    list.ToString(), ref complainResult,
                    ref complainErrorLine);
            }

            if (complainWithThrow)
                throw new ScriptException(code, result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for StatusCallback
#if WINFORMS
        private static ReturnCode TestStatusCallback(
            Interpreter interpreter,
            IClientData clientData,
            string text,
            bool clear,
            ref Result error
            )
        {
            int localShouldStatusCallback = Interlocked.CompareExchange(
                ref shouldStatusCallback, 0, 0);

            if (localShouldStatusCallback == 1)
            {
                error = "status callback error";
                return ReturnCode.Error;
            }

            if (localShouldStatusCallback == 2)
            {
                error = "status callback return";
                return ReturnCode.Return;
            }

            if (localShouldStatusCallback == 3)
            {
                error = "status callback break";
                return ReturnCode.Break;
            }

            if (localShouldStatusCallback == 4)
            {
                error = "status callback continue";
                return ReturnCode.Continue;
            }

            if (localShouldStatusCallback == 5)
                throw new ScriptException("status callback exception");

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for SleepWaitCallback
        private ReturnCode TestSleepWaitCallback(
            Interpreter interpreter,
            EventWaitHandle[] events,
            int milliseconds,
            EventWaitFlags eventWaitFlags,
            ref Result error
            )
        {
            // CheckDisposed();

            ObjectDictionary objects = new ObjectDictionary();

            objects.Add("interpreter", interpreter);
            objects.Add("events", events);
            objects.Add("milliseconds", milliseconds);
            objects.Add("eventWaitFlags", eventWaitFlags);

            Result result = null;

            if (Helpers.EvaluateScript(
                    callbackInterpreter, sleepWaitCallbackText,
                    objects, ref result) != ReturnCode.Ok)
            {
                error = result;
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for GetTempFileNameCallback
        private string TestGetTempFileNameCallback()
        {
            // CheckDisposed();

            if (tempException)
            {
                throw new ScriptException(
                    "get temporary file name failure (requested)");
            }

            string path = tempPath;

            if (path != null)
            {
                return Path.Combine(path, String.Format(
                    "tmp{0:X5}.tmp", GlobalState.NextId()));
            }
            else
            {
                return Path.GetTempFileName(); /* EXEMPT */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for GetTempPathCallback
        private string TestGetTempPathCallback()
        {
            // CheckDisposed();

            if (tempException)
            {
                throw new ScriptException(
                    "get temporary path failure (requested)");
            }

            return tempPath;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewInterpreterCallback
        private ReturnCode TestNewInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            // CheckDisposed();

            if ((callbackInterpreter != null) &&
                !String.IsNullOrEmpty(newInterpreterText))
            {
                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("interpreter", interpreter);
                objects.Add("clientData", clientData);

                return Helpers.EvaluateScript(
                    callbackInterpreter, newInterpreterText,
                    objects, ref result);
            }

            result = null;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode TestPrivateInstanceNewInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            string formatted = String.Format(
                "TestPrivateInstanceNewInterpreterCallback: interpreter = {0}",
                FormatOps.InterpreterNoThrow(interpreter));

            if (calledMethods == null)
                calledMethods = StringOps.NewStringBuilder();

            calledMethods.AppendLine(formatted);

            TraceOps.DebugTrace(
                formatted, typeof(Default).Name, TracePriority.Highest);

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for UseInterpreterCallback
        private ReturnCode TestUseInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            // CheckDisposed();

            if ((callbackInterpreter != null) &&
                !String.IsNullOrEmpty(useInterpreterText))
            {
                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("interpreter", interpreter);
                objects.Add("clientData", clientData);

                return Helpers.EvaluateScript(
                    callbackInterpreter, useInterpreterText,
                    objects, ref result);
            }

            result = null;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for FreeInterpreterCallback
        private ReturnCode TestFreeInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            // CheckDisposed();

            if ((callbackInterpreter != null) &&
                !String.IsNullOrEmpty(freeInterpreterText))
            {
                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("interpreter", interpreter);
                objects.Add("clientData", clientData);

                return Helpers.EvaluateScript(
                    callbackInterpreter, freeInterpreterText,
                    objects, ref result);
            }

            result = null;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewCommandCallback
        private ICommand TestNewCommandCallback(
            Interpreter interpreter,
            IClientData clientData,
            string name,
            IPlugin plugin,
            ref Result error
            )
        {
            // CheckDisposed();

            if ((callbackInterpreter != null) &&
                !String.IsNullOrEmpty(newCommandText))
            {
                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("interpreter", interpreter);
                objects.Add("clientData", clientData);
                objects.Add("name", name);
                objects.Add("plugin", plugin);

                ReturnCode code;
                Result result = null;

                code = Helpers.EvaluateScript(
                    callbackInterpreter, newCommandText,
                    objects, ref result);

                if (code == ReturnCode.Ok)
                {
                    string objectName = result;
                    IObject @object = null;

                    code = interpreter.GetObject(
                        objectName, LookupFlags.Default, ref @object,
                        ref result);

                    if (code == ReturnCode.Ok)
                    {
                        ICommand command = @object.Value as ICommand;

                        if (command == null)
                            error = "returned object is not a command";

                        return command;
                    }
                    else
                    {
                        error = result;
                    }
                }
                else
                {
                    error = result;
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for UnknownCallback
        private void TraceForUnknownCallback(
            string prefix,
            Interpreter interpreter,
            EngineFlags engineFlags,
            string name,
            ArgumentList oldArguments,
            ArgumentList newArguments,
            LookupFlags lookupFlags,
            bool ambiguous,
            IExecute execute,
            Result error,
            ReturnCode code
            )
        {
            TraceOps.DebugTrace(String.Format(
                "{0}: interpreter = {1}, engineFlags = {2}, name = {3}, " +
                "oldArguments = {4}, newArguments = {5}, lookupFlags = {6}, " +
                "ambiguous = {7}, execute = {8}, error = {9}, code = {10}",
                (prefix != null) ? prefix : "TraceForUnknownCallback",
                FormatOps.InterpreterNoThrow(interpreter), FormatOps.WrapOrNull(
                engineFlags), FormatOps.WrapOrNull(name), FormatOps.WrapOrNull(
                oldArguments), FormatOps.WrapOrNull(newArguments),
                FormatOps.WrapOrNull(lookupFlags), FormatOps.WrapOrNull(
                ambiguous), FormatOps.WrapOrNull(execute), FormatOps.WrapOrNull(
                true, true, error), FormatOps.WrapOrNull(code)),
                typeof(Default).Name, TracePriority.Command);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode TestUnknownScriptObjectCallback(
            Interpreter interpreter,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref IExecute execute,
            ref Result error
            )
        {
            // CheckDisposed();

            ReturnCode code = ReturnCode.Ok;

            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    code = ReturnCode.Error;

                    return code;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("methodName", "UnknownCallback");
                objects.Add("engineFlags", engineFlags);
                objects.Add("name", name);
                objects.Add("arguments", arguments);
                objects.Add("lookupFlags", lookupFlags);
                objects.Add("ambiguous", ambiguous);
                objects.Add("execute", execute);

                Result localResult = null; /* REUSED */

                code = Helpers.EvaluateScript(
                    interpreter, unknownCallbackText,
                    objects, ref localResult);

                if (!ResultOps.IsOkOrReturn(code))
                {
                    error = localResult;
                    return code;
                }

                string objectName = localResult;
                IObject @object = null;

                localResult = null;

                code = interpreter.GetObject(
                    objectName, LookupFlags.Default, ref @object,
                    ref localResult);

                if (code != ReturnCode.Ok)
                {
                    error = localResult;
                    return code;
                }

                IExecute localExecute = @object.Value as IExecute;

                if (localExecute == null)
                {
                    error = "returned object is not an execute";
                    code = ReturnCode.Error;

                    return code;
                }

                execute = localExecute;

                return code;
            }
            finally
            {
                TraceForUnknownCallback("TestUnknownScriptObjectCallback",
                    interpreter, engineFlags, name, arguments, null,
                    lookupFlags, ambiguous, execute, error, code);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode TestUnknownScriptCommandCallback(
            Interpreter interpreter,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref IExecute execute,
            ref Result error
            )
        {
            // CheckDisposed();

            ReturnCode code = ReturnCode.Ok;

            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    code = ReturnCode.Error;

                    return code;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("methodName", "UnknownCallback");
                objects.Add("engineFlags", engineFlags);
                objects.Add("name", name);
                objects.Add("arguments", arguments);
                objects.Add("lookupFlags", lookupFlags);
                objects.Add("ambiguous", ambiguous);
                objects.Add("execute", execute);

                Result localResult = null; /* REUSED */

                code = Helpers.EvaluateScript(
                    interpreter, unknownCallbackText,
                    objects, ref localResult);

                if (!ResultOps.IsOkOrReturn(code))
                {
                    error = localResult;
                    return code;
                }

                string commandName = localResult;
                IExecute localExecute = null;

                localResult = null;

                code = interpreter.GetIExecuteViaResolvers(
                    engineFlags, commandName, arguments, lookupFlags,
                    ref localExecute, ref localResult);

                if (code != ReturnCode.Ok)
                {
                    error = localResult;
                    return code;
                }

                execute = localExecute;
                return code;
            }
            finally
            {
                TraceForUnknownCallback("TestUnknownScriptCommandCallback",
                    interpreter, engineFlags, name, arguments, null,
                    lookupFlags, ambiguous, execute, error, code);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode TestUnknownSpyCallback(
            Interpreter interpreter,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref IExecute execute,
            ref Result error
            )
        {
            // CheckDisposed();

            ReturnCode code = ReturnCode.Error;

            TraceForUnknownCallback("TestUnknownSpyCallback",
                interpreter, engineFlags, name, arguments, null,
                lookupFlags, ambiguous, execute, error,
                code);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode TestUnknownObjectInvokeCallback(
            Interpreter interpreter,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref IExecute execute,
            ref Result error
            )
        {
            // CheckDisposed();

            ReturnCode code = ReturnCode.Ok;
            ArgumentList savedArguments;

            if (arguments != null)
                savedArguments = new ArgumentList(arguments);
            else
                savedArguments = null;

            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    code = ReturnCode.Error;

                    return code;
                }

                if (arguments == null)
                {
                    error = "invalid arguments";
                    code = ReturnCode.Error;

                    return code;
                }

                if (arguments.Count < 2)
                {
                    error = ObjectWrongNumArgs;
                    code = ReturnCode.Error;

                    return code;
                }

                OptionDictionary options = ObjectOps.GetInvokeOptions();
                int argumentIndex = Index.Invalid;
                Result localError = null; /* REUSED */

                if (interpreter.GetOptions(
                        options, arguments, 0, 1, Index.Invalid, false,
                        ref argumentIndex, ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    code = ReturnCode.Error;

                    return code;
                }

                if ((argumentIndex == Index.Invalid) ||
                    (argumentIndex >= arguments.Count))
                {
                    if ((argumentIndex != Index.Invalid) &&
                        Option.LooksLikeOption(arguments[argumentIndex]))
                    {
                        localError = OptionDictionary.BadOption(
                            options, arguments[argumentIndex],
                            !interpreter.InternalIsSafe());
                    }
                    else
                    {
                        localError = ObjectWrongNumArgs;
                    }

                    error = localError;
                    code = ReturnCode.Error;

                    return code;
                }

                MemberTypes memberTypes;
                BindingFlags bindingFlags;
                ValueFlags objectValueFlags;
                ValueFlags memberValueFlags;

                ObjectOps.ProcessReflectionOptions(
                    options, ObjectOptionType.Invoke,
                    null, null, null, null, out memberTypes,
                    out bindingFlags, out objectValueFlags,
                    out memberValueFlags);

                string objectName = name;

                if (objectName == null)
                    objectName = arguments[0]; /* HACK: Command name? */

                AppDomain appDomain = interpreter.GetAppDomain();
                CultureInfo cultureInfo = interpreter.InternalCultureInfo;
                ITypedInstance typedInstance = null;

                localError = null;

                if (Value.GetNestedObject(
                        interpreter, objectName, null, appDomain,
                        bindingFlags, null, null, objectValueFlags,
                        cultureInfo, ref typedInstance,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    code = ReturnCode.Error;

                    return code;
                }

                string memberName = arguments[argumentIndex];
                ArgumentList localArguments = null;

                localError = null;

                if (interpreter.MergeArguments(options,
                        new ArgumentList("object", "invoke"), arguments,
                        2, 1, false, false, false, ref localArguments,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    code = ReturnCode.Error;

                    return code;
                }

                if ((localArguments == null) || (localArguments.Count == 0))
                {
                    error = "invalid arguments after merge";
                    code = ReturnCode.Error;

                    return code;
                }

                ITypedMember typedMember = null;

                localError = null;

                if (Value.GetNestedMember(
                        interpreter, MarshalOps.MaybeUseExtraParts(
                            typedInstance, memberName), typedInstance,
                        memberTypes, bindingFlags, memberValueFlags,
                        cultureInfo, ref typedMember,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    code = ReturnCode.Error;

                    return code;
                }

                string executeName = localArguments[0]; /* HACK: Command name? */
                IExecute localExecute = null;

                localError = null;

                if (interpreter.GetIExecuteViaResolvers(
                        engineFlags | EngineFlags.ToExecute,
                        executeName, localArguments,
                        LookupFlags.EngineDefault,
                        ref ambiguous, ref localExecute,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    code = ReturnCode.Error;

                    return code;
                }

                //
                // HACK: Override the original arguments.  This is needed to
                //       force the Engine class to execute the [object invoke]
                //       sub-command correctly (i.e. with the correct options
                //       and member name).
                //
                arguments.Clear();
                arguments.AddRange(localArguments);

                //
                // NOTE: This will be the IExecute instance that corresponds
                //       to the [object] command.
                //
                execute = localExecute;

                return code;
            }
            finally
            {
                TraceForUnknownCallback("TestUnknownObjectInvokeCallback",
                    interpreter, engineFlags, name, savedArguments,
                    arguments, lookupFlags, ambiguous, execute, error,
                    code);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for PackageCallback
        private ReturnCode TestPackageFallbackCallback(
            Interpreter interpreter,
            string name,
            Version version,
            string text,
            PackageFlags flags,
            bool exact,
            ref Result result
            )
        {
            // CheckDisposed();

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (name == null)
            {
                result = "invalid package name";
                return ReturnCode.Error;
            }

            if (version == null)
            {
                result = "invalid package version";
                return ReturnCode.Error;
            }

            if (packageFallbackText != null)
            {
                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("methodName", "PackageFallback");
                objects.Add("name", name);
                objects.Add("version", version);
                objects.Add("text", text);
                objects.Add("flags", flags);
                objects.Add("exact", exact);

                Result localResult = null; /* REUSED */

                if (!ResultOps.IsOkOrReturn(Helpers.EvaluateScript(
                        interpreter, packageFallbackText, objects,
                        ref localResult)))
                {
                    result = localResult;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        interpreter, localResult, ref value,
                        ref localResult) != ReturnCode.Ok)
                {
                    result = localResult;
                    return ReturnCode.Error;
                }

                if (value)
                {
                    localResult = null;

                    if (interpreter.PkgProvide(
                            name, version, ClientData.Empty, flags,
                            ref localResult) == ReturnCode.Ok)
                    {
                        result = localResult;
                    }
                    else
                    {
                        result = localResult;
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    result = String.Empty;
                }
            }
            else
            {
                result = String.Empty;
            }

            return ReturnCode.Ok;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        #region Methods for Ad-Hoc Commands
        private static void TestVoidMethod(
            string value
            )
        {
            //
            // NOTE: Write a string to the console as long as it is not null.
            //
            if (value == null)
                throw new ArgumentNullException("value");

            Interpreter interpreter = Interpreter.GetActive();

            if (interpreter == null)
                throw new ScriptException("invalid interpreter");

            ReturnCode code;
            Result result = null;

            code = ScriptOps.WriteViaIExecute(
                interpreter, null, null, value, ref result);

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long TestLongMethod(
            DateTime dateTime
            )
        {
            //
            // NOTE: Just return the number of ticks.
            //
            return dateTime.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IEnumerable TestIEnumerableMethod(
            ICommand command
            )
        {
            //
            // NOTE: Return the sub-commands for the command.
            //
            if (command != null)
            {
                EnsembleDictionary subCommands = PolicyOps.GetSubCommandsUnsafe(
                    command); /* TEST NAME LIST USE ONLY */

                if (subCommands != null)
                {
                    StringList result = new StringList(subCommands.Keys);
                    result.Sort(); return result;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TestHasOnlyObjectArrayParameter(
            Delegate callback
            )
        {
            if (callback == null)
                return false;

            MethodInfo methodInfo = callback.Method;

            if (methodInfo == null)
                return false;

            ParameterInfo[] parameterInfo = methodInfo.GetParameters();

            if ((parameterInfo == null) || (parameterInfo.Length != 1))
                return false;

            ParameterInfo firstParameterInfo = parameterInfo[0];

            if (firstParameterInfo == null)
                return false;

            return (firstParameterInfo.ParameterType == typeof(object[]));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ScriptWebClient
#if NETWORK
        private static WebClient TestErrorNewWebClientCallback(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            string text = null;

            if (clientData != null)
            {
                object data = null;

                /* IGNORED */
                clientData = ClientData.UnwrapOrReturn(
                    clientData, ref data);

                text = data as string;
            }

            if (text != null)
                error = text;
            else
                error = "creation of web client forbidden";

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ErrorCallback / ErrorListCallback
        private static Result TestErrorCallback(
            Result error
            )
        {
            if (Interlocked.CompareExchange(
                    ref shouldTraceError, 0, 0) > 0)
            {
                string pattern = Interlocked.CompareExchange(
                    ref traceErrorPattern, null, null);

                if ((pattern == null) || Parser.StringMatch(
                        null, error, 0, pattern, 0, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "TestErrorCallback: error = {0}",
                        FormatOps.WrapOrNull(true, false,
                        error)), typeof(Default).Name,
                        TracePriority.ConversionError);
                }
            }

            return error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ResultList TestErrorListCallback(
            ResultList errors
            )
        {
            if (Interlocked.CompareExchange(
                    ref shouldTraceError, 0, 0) > 0)
            {
                string pattern = Interlocked.CompareExchange(
                    ref traceErrorPattern, null, null);

                if ((pattern == null) || Parser.StringMatch(
                        null, (Result)errors, 0, pattern, 0, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "TestErrorListCallback: errors = {0}",
                        FormatOps.WrapOrNull(true, false,
                        errors)), typeof(Default).Name,
                        TracePriority.ConversionError);
                }
            }

            return errors;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for TraceOps.DebugTrace
        private static bool TestTraceFilterStubCallback(
            Interpreter interpreter,
            string message,
            string category,
            TracePriority priority
            )
        {
            int localTraceFilterStubSetting = Interlocked.CompareExchange(
                ref traceFilterStubSetting, 0, 0);

            if (localTraceFilterStubSetting == 1)
                return true;

            if (localTraceFilterStubSetting == 2)
                throw new ScriptException("trace filter callback error");

            if (localTraceFilterStubSetting == 3)
            {
                TraceOps.DebugTrace(String.Format(
                    "TestTraceFilterStubCallback: traceFilterStubSetting = {0}",
                    localTraceFilterStubSetting), typeof(Default).Name,
                    TracePriority.TestError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TestTraceFilterMessageCallback(
            Interpreter interpreter,
            string message,
            string category,
            TracePriority priority
            )
        {
            MatchMode mode;
            string pattern;

            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                mode = traceFilterMatchMode;
                pattern = traceFilterPattern;
            }

            if ((pattern != null) && !StringOps.Match(
                    interpreter, mode, message, pattern, false))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TestTraceFilterCategoryCallback(
            Interpreter interpreter,
            string message,
            string category,
            TracePriority priority
            )
        {
            MatchMode mode;
            string pattern;

            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                mode = traceFilterMatchMode;
                pattern = traceFilterPattern;
            }

            if ((pattern != null) && !StringOps.Match(
                    interpreter, mode, category, pattern, false))
            {
                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for StringOps.MatchCore
        private static ReturnCode TestMatchCallback(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            IClientData clientData,
            ref bool match,
            ref Result error
            )
        {
            bool noCase = false;
            IComparer<string> comparer = null;
            RegexOptions regExOptions = RegexOptions.None;

            if (clientData != null)
            {
                ObjectList objectList = clientData.Data as ObjectList;

                if ((objectList != null) && (objectList.Count >= 3))
                {
                    if (objectList[0] is bool)
                        noCase = (bool)objectList[0];

                    if (objectList[1] is IComparer<string>)
                        comparer = (IComparer<string>)objectList[1];

                    if (objectList[2] is RegexOptions)
                        regExOptions = (RegexOptions)objectList[2];
                }
            }

            if (FlagOps.HasFlags(mode, MatchMode.Callback, true))
            {
                MatchMode[] modes = Enum.GetValues(
                    typeof(MatchMode)) as MatchMode[];

                if (modes != null)
                {
                    foreach (MatchMode localMode in modes)
                    {
                        if ((localMode == MatchMode.None) ||
                            (localMode == MatchMode.Invalid) ||
                            (localMode == MatchMode.Callback) ||
                            !FlagOps.HasFlags(localMode,
                                MatchMode.SimpleModeMask, false) ||
                            FlagOps.HasFlags(localMode,
                                MatchMode.SimpleModeMask, true))
                        {
                            continue;
                        }

                        ReturnCode code;
                        bool localMatch = false;
                        Result localError = null;

                        code = StringOps.Match(
                            interpreter, localMode, text,
                            pattern, noCase, comparer,
                            regExOptions, ref localMatch,
                            ref localError);

                        if (code != ReturnCode.Ok)
                        {
                            error = localError;
                            return code;
                        }

                        if (localMatch)
                        {
                            match = localMatch;
                            return ReturnCode.Ok;
                        }
                    }
                }

                match = false;
                return ReturnCode.Ok;
            }
            else
            {
                return StringOps.Match(
                    interpreter, mode, text, pattern,
                    noCase, comparer, regExOptions,
                    ref match, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ThreadOps.GetTimeout
        private static ReturnCode TestGetTimeoutCallback(
            Interpreter interpreter, /* in */
            TimeoutType timeoutType, /* in */
            ref int? timeout,        /* in, out */
            ref Result error         /* out */
            )
        {
            if (staticTimeout != null)
            {
                timeout = staticTimeout;
                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid static timeout";
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        private static void TestComplainCallbackFail(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            string formatted = FormatOps.Complaint(
                id, code, result, stackTrace);

#if CONSOLE
            ConsoleOps.WriteComplaint(formatted);
#endif

            DebugOps.Fail(typeof(Default).FullName, formatted);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackBreak(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
#if CONSOLE
            ConsoleOps.WriteComplaint(FormatOps.Complaint(
                id, code, result, stackTrace));
#endif

            DebugOps.Break();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackThrow(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            TestComplainCallbackSetVariable(
                "test_complain_throw", interpreter, id, code, result,
                stackTrace, quiet, retry, levels);

            throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackNoThrow(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            TestComplainCallbackSetVariable(
                "test_complain_no_throw", interpreter, id, code, result,
                stackTrace, quiet, retry, levels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackSetVariable(
            string varName,
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            if (interpreter != null)
            {
                ReturnCode setCode;
                Result setError = null;

                setCode = interpreter.SetVariableValue(
                    VariableFlags.None, varName, StringList.MakeList(
                    "retry", retry, "levels", levels, "formatted",
                    FormatOps.Complaint(id, code, result, stackTrace)),
                    ref setError);

                if ((setCode != ReturnCode.Ok) && (levels == 1))
                {
                    DebugOps.Complain(
                        interpreter, setCode, setError); /* RECURSIVE */
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Policy Callback Methods
        [MethodFlags(
            MethodFlags.PluginPolicy | MethodFlags.System |
            MethodFlags.NoAdd)]
        private static ReturnCode TestLoadPluginPolicy( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments, /* NOT USED */
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                result = "invalid policy clientData";
                return ReturnCode.Error;
            }

            IPolicyContext policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                result = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            string fileName = policyContext.FileName;

            if (String.IsNullOrEmpty(fileName))
            {
                policyContext.Denied("no plugin file name was supplied");
                return ReturnCode.Ok;
            }

            string typeName = policyContext.TypeName;

            if (StringOps.Match(
                    interpreter, MatchMode.Glob, typeName, "*.Class4", true))
            {
                policyContext.Denied("access to plugin Class4 is denied");
                return ReturnCode.Ok;
            }

            if (StringOps.Match(
                    interpreter, MatchMode.Glob, typeName, "*.Class3", true))
            {
                policyContext.Approved("access to plugin Class3 is granted");
                return ReturnCode.Ok;
            }

            policyContext.Undecided("plugin type name is unknown");
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Command Callback Methods
        private static ReturnCode TestAddVariableCommandCallback(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count != 2)
            {
                result = "wrong # args: should be \"vadd name\"";
                return ReturnCode.Error;
            }

            return interpreter.AddVariable(
                VariableFlags.None, arguments[1], null, true,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestUsableVariableCommandCallback(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count != 2)
            {
                result = "wrong # args: should be \"vusable name\"";
                return ReturnCode.Error;
            }

            Result error; /* REUSED */
            VariableFlags variableFlags = VariableFlags.NoUsable;
            IVariable variable = null;

            error = null;

            if (interpreter.GetVariableViaResolversWithSplit(
                    arguments[1], ref variableFlags, ref variable,
                    ref error) != ReturnCode.Ok)
            {
                result = error;
                return ReturnCode.Error;
            }

            error = null;

            if (variable.IsUsable(ref error))
            {
                result = String.Empty;
                return ReturnCode.Ok;
            }
            else
            {
                result = error;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestIsLockedVariableCommandCallback(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count != 2)
            {
                result = "wrong # args: should be \"vislocked name\"";
                return ReturnCode.Error;
            }

            VariableFlags variableFlags = VariableFlags.NoUsable;
            IVariable variable = null;
            Result error = null;

            if (interpreter.GetVariableViaResolversWithSplit(
                    arguments[1], ref variableFlags, ref variable,
                    ref error) != ReturnCode.Ok)
            {
                result = error;
                return ReturnCode.Error;
            }

            Variable localVariable = variable as Variable;

            if (localVariable == null)
            {
                result = "unsupported variable type";
                return ReturnCode.Error;
            }

            long? threadId = null;

            /* IGNORED */
            localVariable.IsLockedByOtherThread(ref threadId);

            result = threadId;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestLockVariableCommandCallback(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if ((arguments.Count < 2) || (arguments.Count > 3))
            {
                result = "wrong # args: should be \"vlock name ?timeout?\"";
                return ReturnCode.Error;
            }

            int timeout = 0;

            if (arguments.Count == 3)
            {
                if (Value.GetInteger2(
                        (IGetValue)arguments[2], ValueFlags.AnyInteger,
                        interpreter.InternalCultureInfo, ref timeout,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            EventWaitFlags eventWaitFlags = interpreter.EventWaitFlags;
            VariableFlags variableFlags = interpreter.EventVariableFlags;

            ScriptOps.MaybeModifyEventWaitFlags(ref eventWaitFlags);

            return interpreter.LockVariable(
                eventWaitFlags, variableFlags, arguments[1], timeout, null,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestUnlockVariableCommandCallback(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count != 2)
            {
                result = "wrong # args: should be \"vunlock name\"";
                return ReturnCode.Error;
            }

            return interpreter.UnlockVariable(
                VariableFlags.None, arguments[1], ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NativeStack
#if NATIVE && (WINDOWS || UNIX || UNSAFE)
        private static bool TestNativeIsMainThreadCallback()
        {
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr TestNativeStackCallback()
        {
            return UIntPtr.Zero;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for Reparse Points
        private static bool TestScanForCharacters(
            byte[] dataBytes,
            char[] scanCharacters,
            int dataStartIndex
            )
        {
            if (dataBytes == null)
                return false;

            if (scanCharacters == null)
                return false;

            if (dataStartIndex < 0)
                return false;

            if ((dataStartIndex % 2) != 0)
                return false;

            int dataLength = dataBytes.Length;

            if (dataLength == 0)
                return false;

            if ((dataLength % 2) != 0)
                return false;

            int scanLength = scanCharacters.Length;

            if (scanLength == 0)
                return true; // NOTE: Match nothing?  Ok.

            int scanIndex = 0;

            for (int dataIndex = dataStartIndex;
                    dataIndex < dataLength; dataIndex += 2)
            {
                char dataCharacter = ConversionOps.ToChar(
                    dataBytes[dataIndex], dataBytes[dataIndex + 1]);

                char scanCharacter = scanCharacters[scanIndex];

                if (scanCharacter == Characters.X)
                {
                    if (Char.IsLetter(dataCharacter))
                    {
                        scanIndex++;

                        if (scanIndex >= scanLength)
                            return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (dataCharacter == scanCharacter)
                    {
                        scanIndex++;

                        if (scanIndex >= scanLength)
                            return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for Interpreter Disposal
        private static bool TestDisposeInterpreter(
            int timeout,                 /* in */
            ref Interpreter interpreter, /* in, out */
            ref Result error             /* out */
            )
        {
            try
            {
                if (timeout > 0)
                    Thread.Sleep(timeout);

                if (interpreter != null)
                {
                    interpreter.Dispose(); /* throw */
                    interpreter = null;
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestDisposeInterpreterWaitCallback(
            object state
            ) /* System.Threading.WaitCallback */
        {
            IAnyPair<int, Interpreter> anyPair =
                state as IAnyPair<int, Interpreter>;

            if (anyPair == null)
                return;

            int timeout = anyPair.X;
            Interpreter interpreter = anyPair.Y;
            Result error = null;

            if (!TestDisposeInterpreter(
                    timeout, ref interpreter, ref error))
            {
                TraceOps.DebugTrace(String.Format(
                    "TestDisposeInterpreterWaitCallback: error = {0}",
                    FormatOps.WrapOrNull(error)), typeof(Default).Name,
                    TracePriority.CleanupError);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewInterpreterCallback
        private static ReturnCode TestPrivateStaticNewInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            string formatted = String.Format(
                "TestPrivateStaticNewInterpreterCallback: interpreter = {0}",
                FormatOps.InterpreterNoThrow(interpreter));

            if (calledMethods == null)
                calledMethods = StringOps.NewStringBuilder();

            calledMethods.AppendLine(formatted);

            TraceOps.DebugTrace(
                formatted, typeof(Default).Name, TracePriority.Highest);

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for [pkgInstallLog]
        private static ReturnCode TestValidatePkgInstallArguments(
            Interpreter interpreter,
            ArgumentList arguments,
            out bool? install,
            out PkgInstallType? type,
            out string name,
            out string directory,
            ref Result error
            )
        {
            install = null;
            type = null;
            name = null;
            directory = null;

            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 4)
            {
                error = String.Format(
                    "wrong # args: should be \"{0} type name directory ?arg ...?\"",
                    PkgInstallLogCommandName);

                return ReturnCode.Error;
            }

            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            object enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(PkgInstallType),
                PkgInstallType.Default.ToString(),
                arguments[1], cultureInfo, true, true,
                true, ref error);

            if (!(enumValue is PkgInstallType))
                return ReturnCode.Error;

            PkgInstallType localType = (PkgInstallType)enumValue;
            bool? localInstall = null;

            if (FlagOps.HasFlags(
                    localType, PkgInstallType.Install, true))
            {
                localInstall = true;
            }
            else if (FlagOps.HasFlags(
                    localType, PkgInstallType.Uninstall, true))
            {
                localInstall = false;
            }

            localType &= ~PkgInstallType.ActionMask;
            localType &= ~PkgInstallType.ForDefault;

            if ((localType != PkgInstallType.Temporary) &&
                (localType != PkgInstallType.Persistent))
            {
                error = String.Format(
                    "unsupported package install type, must be {0} or {1}",
                    PkgInstallType.Temporary, PkgInstallType.Persistent);

                return ReturnCode.Error;
            }

            string localName = arguments[2];

            if ((GoodPkgNameRegEx != null) &&
                !GoodPkgNameRegEx.Match(localName).Success)
            {
                error = "this is not a good package name";
                return ReturnCode.Error;
            }

            if ((BadPkgNameRegEx != null) &&
                BadPkgNameRegEx.Match(localName).Success)
            {
                error = "this is a bad package name";
                return ReturnCode.Error;
            }

            string localDirectory = arguments[3];

            if (String.IsNullOrEmpty(localDirectory) ||
                !Directory.Exists(localDirectory))
            {
                error = "bad package directory";
                return ReturnCode.Error;
            }

            install = localInstall;
            type = localType;
            name = localName;
            directory = localDirectory;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for CrossAppDomainHelper
#if !NET_STANDARD_20
        private static ReturnCode TestEvaluateInAppDomainViaCross(
            AppDomain appDomain, /* in */
            string text,         /* in */
            ref Result result    /* out */
            )
        {
            if (appDomain == null)
            {
                result = "invalid application domain";
                return ReturnCode.Error;
            }

            TestCrossAppDomainHelper helper = null;

            try
            {
                helper = new TestCrossAppDomainHelper();
                helper.Text = text; /* in */

                //
                // TODO: Why can this call never succeed
                //       when the AppDomain cannot probe
                //       for the core library assembly,
                //       even if it is already loaded in
                //       that AppDomain?
                //
                appDomain.DoCallBack(helper.Evaluate); /* throw */

                result = helper.Result; /* out */
                return helper.ReturnCode; /* out */
            }
            catch (Exception e)
            {
                result = e;
                return ReturnCode.Error;
            }
            finally
            {
                ObjectOps.DisposeOrTrace<TestCrossAppDomainHelper>(
                    null, ref helper);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestEvaluateInAppDomainViaCreate(
            AppDomain appDomain, /* in */
            string text,         /* in */
            ref Result result    /* out */
            )
        {
            if (appDomain == null)
            {
                result = "invalid application domain";
                return ReturnCode.Error;
            }

            string location = GlobalState.GetAssemblyLocation();

            if (String.IsNullOrEmpty(location))
            {
                result = "invalid assembly location";
                return ReturnCode.Error;
            }

            string typeName = TestCrossAppDomainHelper.GetTypeName();

            if (String.IsNullOrEmpty(typeName))
            {
                result = "invalid type name";
                return ReturnCode.Error;
            }

            TestCrossAppDomainHelper helper = null;

            try
            {
                helper = appDomain.CreateInstanceFromAndUnwrap(
                    location, typeName) as TestCrossAppDomainHelper;

                if (helper != null)
                {
                    helper.Text = text; /* in */

                    helper.Evaluate(); /* throw */

                    result = helper.Result; /* out */
                    return helper.ReturnCode; /* out */
                }
                else
                {
                    result = String.Format(
                        "could not create {0} from {1}",
                        FormatOps.WrapOrNull(typeName),
                        FormatOps.WrapOrNull(location));

                    return ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                return ReturnCode.Error;
            }
            finally
            {
                ObjectOps.DisposeOrTrace<TestCrossAppDomainHelper>(
                    null, ref helper);
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static InterpreterFlags? TestSetQuietMask(
            Interpreter interpreter,
            bool quiet
            )
        {
            if (interpreter == null)
                return null;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                InterpreterFlags oldFlags = interpreter.InterpreterFlagsNoLock;
                InterpreterFlags unsetFlags = InterpreterFlags.QuietUnsetMask;
                InterpreterFlags setFlags = InterpreterFlags.QuietSetMask;

                if (quiet)
                {
                    interpreter.InterpreterFlagsNoLock &= ~unsetFlags;
                    interpreter.InterpreterFlagsNoLock |= setFlags;
                }
                else
                {
                    interpreter.InterpreterFlagsNoLock &= ~setFlags;
                    interpreter.InterpreterFlagsNoLock |= unsetFlags;
                }

                return oldFlags;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
#if CONSOLE
        private static HostCreateFlags TestMaskFlags(
            HostCreateFlags hostCreateFlags
            )
        {
            HostCreateFlags newHostCreateFlags = hostCreateFlags;

            newHostCreateFlags &= ~HostCreateFlags.CloseConsole;
            newHostCreateFlags |= HostCreateFlags.OpenConsole;
            newHostCreateFlags |= HostCreateFlags.QuietConsole;

            return newHostCreateFlags;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestDebugEmergencyBreakWaitCallback(
            object state
            ) /* System.Threading.WaitCallback */
        {
            Interpreter interpreter = null;
            ReturnCode code = ReturnCode.Error; /* REDUNDANT */
            Result result = null;

            try
            {
                interpreter = state as Interpreter;

                if (interpreter != null)
                {
                    code = interpreter.EvaluateScript(
                        DebugEmergencyScript, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        code = interpreter.EvaluateScript(
                            DebugBreakScript, ref result);
                    }
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (code != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, code, result);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TestIsDebugInteractiveLoopCallback(
            InteractiveLoopCallback interactiveLoopCallback
            )
        {
            return interactiveLoopCallback == TestDebugInteractiveLoopCallback;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TestIsSavedInteractiveLoopCallback(
            InteractiveLoopCallback interactiveLoopCallback
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                return interactiveLoopCallback == savedInteractiveLoopCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestEnableDebugInteractiveLoopCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                savedInteractiveLoopCallback = interpreter.InteractiveLoopCallback;
                interpreter.InteractiveLoopCallback = TestDebugInteractiveLoopCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestDisableDebugInteractiveLoopCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                interpreter.InteractiveLoopCallback = savedInteractiveLoopCallback;
                savedInteractiveLoopCallback = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestDebugInteractiveLoopCallback(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            ref Result result
            )
        {
#if CONSOLE
            //
            // NOTE: If the interpreter is not disposed and is using the
            //       console host, make sure the native console window
            //       is open and ready for use by the interactive use.
            //
            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalHardTryLock(ref locked);

                    if (locked)
                    {
                        if (!interpreter.InternalDisposed)
                        {
                            //
                            // HACK: Check for an actual System.Console based host.
                            //
                            _Hosts.Console consoleHost = interpreter.GetInteractiveHost(
                                typeof(_Hosts.Console)) as _Hosts.Console;

                            if (consoleHost != null)
                            {
                                HostCreateFlags hostCreateFlags = TestMaskFlags(
                                    interpreter.HostCreateFlags);

                                HostOps.SetupNativeConsole(
                                    interpreter, hostCreateFlags);
                            }
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "TestDebugInteractiveLoopCallback: interpreter {0} host type mismatch.",
                                    FormatOps.InterpreterNoThrow(interpreter)),
                                    typeof(Default).Name, TracePriority.ScriptDebug2);
                            }
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TestDebugInteractiveLoopCallback: interpreter {0} is disposed.",
                                FormatOps.InterpreterNoThrow(interpreter)),
                                typeof(Default).Name, TracePriority.ScriptError2);
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "TestDebugInteractiveLoopCallback: interpreter {0} already locked.",
                            FormatOps.InterpreterNoThrow(interpreter)),
                            typeof(Default).Name, TracePriority.LockWarning);
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked);
                }
            }
            else
            {
                TraceOps.DebugTrace(
                    "TestDebugInteractiveLoopCallback: interpreter is invalid.",
                    typeof(Default).Name, TracePriority.ScriptError2);
            }
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            InteractiveLoopCallback interactiveLoopCallback;

            lock (staticSyncRoot)
            {
                interactiveLoopCallback = savedInteractiveLoopCallback;
            }

            if (interactiveLoopCallback != null)
            {
                return interactiveLoopCallback(
                    interpreter, loopData, ref result);
            }
            else
            {
#if SHELL
                return Interpreter.InteractiveLoop(
                    interpreter, loopData, ref result);
#else
                result = "interactive loop is not implemented";
                return ReturnCode.Error;
#endif
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object TestCreateInstance(
            Type type,
            bool nonPublic
            )
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (type == typeof(string))
            {
                return Activator.CreateInstance(
                    type, Guid.NewGuid().ToString().ToCharArray());
            }
            else
            {
                return Activator.CreateInstance(type, nonPublic);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TestFormatArgs(
            object[] args
            )
        {
            if (args == null)
                return FormatOps.DisplayNull;

            int length = args.Length;

            if (length == 0)
                return FormatOps.DisplayEmpty;

            return String.Format("object[{0}]", length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        public static ReturnCode TestShellFixEmptyPreviewArgumentCallback(
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            IClientData clientData,           /* in */
            ArgumentPhase phase,              /* in */
            bool whatIf,                      /* in */
            ref int index,                    /* in, out */
            ref string arg,                   /* in, out */
            ref IList<string> argv,           /* in, out */
            ref Result result                 /* out */
            )
        {
            if (SharedStringOps.SystemEquals(arg, EmptyArgument))
                arg = String.Empty;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestShellErrorEmptyPreviewArgumentCallback(
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            IClientData clientData,           /* in */
            ArgumentPhase phase,              /* in */
            bool whatIf,                      /* in */
            ref int index,                    /* in, out */
            ref string arg,                   /* in, out */
            ref IList<string> argv,           /* in, out */
            ref Result result                 /* out */
            )
        {
            if (String.IsNullOrEmpty(arg))
            {
                result = "empty arguments are not allowed";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestShellUnknownArgumentCallback(
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            IClientData clientData,           /* in */
            int count,                        /* in */
            string arg,                       /* in */
            bool whatIf,                      /* in */
            ref IList<string> argv,           /* in, out */
            ref Result result                 /* out */
            )
        {
            int argc = (argv != null) ? argv.Count : 0;

            if ((count > 0) &&
                StringOps.MatchSwitch(arg, "one"))
            {
                GenericOps<string>.PopFirstArgument(ref argv);

                result += String.Format(
                    "argument one OK{0}",
                    Characters.Pipe);

                return ReturnCode.Ok;
            }
            else if ((count > 0) &&
                StringOps.MatchSwitch(arg, "two"))
            {
                if (argc >= 2)
                {
                    string value = argv[1];

                    GenericOps<string>.PopFirstArgument(ref argv);
                    GenericOps<string>.PopFirstArgument(ref argv);

                    result += String.Format(
                        "argument two {0} OK{1}",
                        FormatOps.WrapOrNull(value),
                        Characters.Pipe);

                    return ReturnCode.Ok;
                }
                else
                {
                    result += String.Format(
                        "wrong # args: should be \"-two <value>\"{0}",
                        Characters.Pipe);
                }
            }
            else if ((count > 0) &&
                StringOps.MatchSwitch(arg, "three"))
            {
                GenericOps<string>.PopFirstArgument(ref argv);

                result += String.Format(
                    "argument three ERROR{0}",
                    Characters.Pipe);
            }
            else
            {
                result += String.Format(
                    "invalid test argument {0}{1}",
                    FormatOps.WrapOrNull(arg),
                    Characters.Pipe);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestShellEvaluateScriptCallback(
            Interpreter interpreter, /* in */
            string text,             /* in */
            ref Result result,       /* out */
            ref int errorLine        /* out */
            )
        {
            IInteractiveHost interactiveHost = (interpreter != null) ?
                interpreter.GetInteractiveHost() : null;

            ShellOps.WritePrompt(interactiveHost, String.Format(
                "Begin evaluation of shell script {0}{1}{2}...",
                Characters.OpenBrace, Parser.Quote(text),
                Characters.CloseBrace));

            try
            {
                if (interpreter != null)
                {
                    return interpreter.EvaluateScript(
                        text, ref result, ref errorLine);
                }
                else
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }
            }
            finally
            {
                ShellOps.WritePrompt(interactiveHost, String.Format(
                    "End evaluation of shell script {0}{1}{2}.",
                    Characters.OpenBrace, Parser.Quote(text),
                    Characters.CloseBrace));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private static ReturnCode TestShellInteractiveLoopCallback(
            Interpreter interpreter,       /* in */
            IInteractiveLoopData loopData, /* in */
            ref Result result              /* out */
            )
        {
            IInteractiveHost interactiveHost = (interpreter != null) ?
                interpreter.GetInteractiveHost() : null;

            ShellOps.WritePrompt(
                interactiveHost, "Entering the interactive loop...");

            try
            {
                return Interpreter.InteractiveLoop(
                    interpreter, loopData, ref result);
            }
            finally
            {
                ShellOps.WritePrompt(
                    interactiveHost, "Exiting the interactive loop...");
            }
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static DateTime TestDateTimeNow()
        {
            if (nowIncrement != 0)
                now = now.AddTicks(nowIncrement);

            return now;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static TypeList TestGetParameterTypeList(
            MethodInfo methodInfo
            )
        {
            TypeList result = new TypeList();

            if (methodInfo != null)
                foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
                    result.Add(parameterInfo.ParameterType);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        private static IntPtrList TestIntPtrList()
        {
            return new IntPtrList(new IntPtr[] {
                new IntPtr(-1), IntPtr.Zero, new IntPtr(1)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static CharList TestCharList()
        {
            return new CharList(Characters.ListReservedCharList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static CommandDataDictionary TestMakeCommandDictionary(
            CommandDataList commands /* in */
            )
        {
            if (commands == null)
                return null;

            CommandDataDictionary result = new CommandDataDictionary();

            foreach (ICommandData command in commands)
            {
                if (command == null)
                    continue;

                string name = command.Name;

                if (name == null)
                    continue;

                result[name] = command;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static FunctionDataDictionary TestMakeFunctionDictionary(
            IEnumerable<IFunctionData> functions /* in */
            )
        {
            if (functions == null)
                return null;

            FunctionDataDictionary result = new FunctionDataDictionary();

            foreach (IFunctionData function in functions)
            {
                if (function == null)
                    continue;

                string name = function.Name;

                if (name == null)
                    continue;

                result[name] = function;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static OperatorDataDictionary TestMakeOperatorDictionary(
            IEnumerable<IOperatorData> operators /* in */
            )
        {
            if (operators == null)
                return null;

            OperatorDataDictionary result = new OperatorDataDictionary();

            foreach (IOperatorData @operator in operators)
            {
                if (@operator == null)
                    continue;

                string name = @operator.Name;

                if (name == null)
                    continue;

                result[name] = @operator;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TestCompareTypeLists(
            TypeList list1, /* in */
            TypeList list2  /* in */
            )
        {
            if ((list1 == null) || (list2 == null))
            {
                if ((list1 == null) && (list2 == null))
                    return true;

                return false;
            }

            int length = list1.Count;

            if (length != list2.Count)
                return false;

            for (int index = 0; index < length; index++)
                if (!MarshalOps.IsSameType(list1[index], list2[index]))
                    return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestCompareCommandDataLists(
            CommandDataList list1, /* in */
            CommandDataList list2, /* in */
            ref ResultList results /* in, out */
            )
        {
            if ((list1 == null) || (list2 == null))
            {
                if ((list1 == null) && (list2 == null))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid lists");
                    return ReturnCode.Ok;
                }

                if (list1 == null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid first list");
                }

                if (list2 == null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid second list");
                }

                return ReturnCode.Error;
            }

            CommandDataDictionary dictionary1 =
                TestMakeCommandDictionary(list1);

            if (dictionary1 == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid first dictionary");
                return ReturnCode.Error;
            }

            CommandDataDictionary dictionary2 =
                TestMakeCommandDictionary(list2);

            if (dictionary2 == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid second dictionary");
                return ReturnCode.Error;
            }

            int errorCount = 0;

            if (dictionary1.Count != dictionary2.Count)
            {
                errorCount++;

                if (results == null)
                    results = new ResultList();

                results.Add(String.Format(
                    "first dictionary count {0} does not " +
                    "match second dictionary count {1}",
                    dictionary1.Count, dictionary2.Count));
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            foreach (CommandDataPair pair1 in dictionary1)
            {
                string key1 = pair1.Key;
                ICommandData data2;

                if (!dictionary2.TryGetValue(key1, out data2))
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "second dictionary missing {0}",
                        FormatOps.WrapOrNull(key1)));

                    continue;
                }

                ICommandData data1 = pair1.Value;

                if ((data1 == null) || (data2 == null))
                {
                    if ((data1 != null) || (data2 != null))
                    {
                        if (data1 == null)
                        {
                            errorCount++;

                            if (results == null)
                                results = new ResultList();

                            results.Add(String.Format(
                                "invalid first object for {0}",
                                FormatOps.WrapOrNull(key1)));
                        }

                        if (data2 == null)
                        {
                            errorCount++;

                            if (results == null)
                                results = new ResultList();

                            results.Add(String.Format(
                                "invalid second object for {0}",
                                FormatOps.WrapOrNull(key1)));
                        }
                    }

                    continue;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IIdentifier Members
                if (data1.Kind == data2.Kind)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "kind for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Kind)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "kind mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Kind),
                        FormatOps.WrapOrNull(data2.Kind)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Id.Equals(data2.Id))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "id for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Id)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "id mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Id),
                        FormatOps.WrapOrNull(data2.Id)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Name, data2.Name))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "name for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Name)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "name mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Name),
                        FormatOps.WrapOrNull(data2.Name)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Group, data2.Group))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "group for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Group)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "group mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Group),
                        FormatOps.WrapOrNull(data2.Group)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Description, data2.Description))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "description for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Description)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "description mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Description),
                        FormatOps.WrapOrNull(data2.Description)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (Object.ReferenceEquals(
                        data1.ClientData, data2.ClientData))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "clientData for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ClientData)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "clientData mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ClientData),
                        FormatOps.WrapOrNull(data2.ClientData)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region ICommandBaseData Members
                if (SharedStringOps.SystemEquals(
                        data1.TypeName, data2.TypeName))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "typeName for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.TypeName)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "typeName mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.TypeName),
                        FormatOps.WrapOrNull(data2.TypeName)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.CommandFlags == data2.CommandFlags)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "commandFlags for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.CommandFlags)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "commandFlags mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.CommandFlags),
                        FormatOps.WrapOrNull(data2.CommandFlags)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IHavePlugin Members
                if (Object.ReferenceEquals(
                        data1.Plugin, data2.Plugin))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "plugin for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Plugin)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "plugin mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Plugin),
                        FormatOps.WrapOrNull(data2.Plugin)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IWrapperData Members
                if (data1.Token == data2.Token)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "token for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Token)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "token mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Token),
                        FormatOps.WrapOrNull(data2.Token)));
                }
                #endregion
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            foreach (CommandDataPair pair2 in dictionary2)
            {
                string key2 = pair2.Key;
                ICommandData data1;

                if (!dictionary1.TryGetValue(key2, out data1))
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "first dictionary missing {0}",
                        FormatOps.WrapOrNull(key2)));
                }
            }

            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestCompareFunctionDataLists(
            IEnumerable<IFunctionData> list1, /* in */
            IEnumerable<IFunctionData> list2, /* in */
            ref ResultList results            /* in, out */
            )
        {
            if ((list1 == null) || (list2 == null))
            {
                if ((list1 == null) && (list2 == null))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid lists");
                    return ReturnCode.Ok;
                }

                if (list1 == null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid first list");
                }

                if (list2 == null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid second list");
                }

                return ReturnCode.Error;
            }

            FunctionDataDictionary dictionary1 =
                TestMakeFunctionDictionary(list1);

            if (dictionary1 == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid first dictionary");
                return ReturnCode.Error;
            }

            FunctionDataDictionary dictionary2 =
                TestMakeFunctionDictionary(list2);

            if (dictionary2 == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid second dictionary");
                return ReturnCode.Error;
            }

            int errorCount = 0;

            if (dictionary1.Count != dictionary2.Count)
            {
                errorCount++;

                if (results == null)
                    results = new ResultList();

                results.Add(String.Format(
                    "first dictionary count {0} does not " +
                    "match second dictionary count {1}",
                    dictionary1.Count, dictionary2.Count));
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            foreach (FunctionDataPair pair1 in dictionary1)
            {
                string key1 = pair1.Key;
                IFunctionData data2;

                if (!dictionary2.TryGetValue(key1, out data2))
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "second dictionary missing {0}",
                        FormatOps.WrapOrNull(key1)));

                    continue;
                }

                IFunctionData data1 = pair1.Value;

                if ((data1 == null) || (data2 == null))
                {
                    if ((data1 != null) || (data2 != null))
                    {
                        if (data1 == null)
                        {
                            errorCount++;

                            if (results == null)
                                results = new ResultList();

                            results.Add(String.Format(
                                "invalid first object for {0}",
                                FormatOps.WrapOrNull(key1)));
                        }

                        if (data2 == null)
                        {
                            errorCount++;

                            if (results == null)
                                results = new ResultList();

                            results.Add(String.Format(
                                "invalid second object for {0}",
                                FormatOps.WrapOrNull(key1)));
                        }
                    }

                    continue;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IIdentifier Members
                if (data1.Kind == data2.Kind)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "kind for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Kind)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "kind mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Kind),
                        FormatOps.WrapOrNull(data2.Kind)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Id.Equals(data2.Id))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "id for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Id)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "id mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Id),
                        FormatOps.WrapOrNull(data2.Id)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Name, data2.Name))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "name for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Name)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "name mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Name),
                        FormatOps.WrapOrNull(data2.Name)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Group, data2.Group))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "group for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Group)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "group mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Group),
                        FormatOps.WrapOrNull(data2.Group)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Description, data2.Description))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "description for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Description)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "description mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Description),
                        FormatOps.WrapOrNull(data2.Description)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (Object.ReferenceEquals(
                        data1.ClientData, data2.ClientData))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "clientData for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ClientData)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "clientData mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ClientData),
                        FormatOps.WrapOrNull(data2.ClientData)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IFunctionData Members
                if (SharedStringOps.SystemEquals(
                        data1.TypeName, data2.TypeName))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "typeName for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.TypeName)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "typeName mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.TypeName),
                        FormatOps.WrapOrNull(data2.TypeName)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Arguments == data2.Arguments)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "arguments for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Arguments)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "arguments mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Arguments),
                        FormatOps.WrapOrNull(data2.Arguments)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (TestCompareTypeLists(
                        data1.Types, data2.Types))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "types for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Types)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "types mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Types),
                        FormatOps.WrapOrNull(data2.Types)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Flags == data2.Flags)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "flags for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Flags)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "flags mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Flags),
                        FormatOps.WrapOrNull(data2.Flags)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IHavePlugin Members
                if (Object.ReferenceEquals(
                        data1.Plugin, data2.Plugin))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "plugin for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Plugin)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "plugin mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Plugin),
                        FormatOps.WrapOrNull(data2.Plugin)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IWrapperData Members
                if (data1.Token == data2.Token)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "token for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Token)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "token mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Token),
                        FormatOps.WrapOrNull(data2.Token)));
                }
                #endregion
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            foreach (FunctionDataPair pair2 in dictionary2)
            {
                string key2 = pair2.Key;
                IFunctionData data1;

                if (!dictionary1.TryGetValue(key2, out data1))
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "first dictionary missing {0}",
                        FormatOps.WrapOrNull(key2)));
                }
            }

            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestCompareOperatorDataLists(
            IEnumerable<IOperatorData> list1, /* in */
            IEnumerable<IOperatorData> list2, /* in */
            ref ResultList results            /* in, out */
            )
        {
            if ((list1 == null) || (list2 == null))
            {
                if ((list1 == null) && (list2 == null))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid lists");
                    return ReturnCode.Ok;
                }

                if (list1 == null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid first list");
                }

                if (list2 == null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add("invalid second list");
                }

                return ReturnCode.Error;
            }

            OperatorDataDictionary dictionary1 =
                TestMakeOperatorDictionary(list1);

            if (dictionary1 == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid first dictionary");
                return ReturnCode.Error;
            }

            OperatorDataDictionary dictionary2 =
                TestMakeOperatorDictionary(list2);

            if (dictionary2 == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add("invalid second dictionary");
                return ReturnCode.Error;
            }

            int errorCount = 0;

            if (dictionary1.Count != dictionary2.Count)
            {
                errorCount++;

                if (results == null)
                    results = new ResultList();

                results.Add(String.Format(
                    "first dictionary count {0} does not " +
                    "match second dictionary count {1}",
                    dictionary1.Count, dictionary2.Count));
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            foreach (OperatorDataPair pair1 in dictionary1)
            {
                string key1 = pair1.Key;
                IOperatorData data2;

                if (!dictionary2.TryGetValue(key1, out data2))
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "second dictionary missing {0}",
                        FormatOps.WrapOrNull(key1)));

                    continue;
                }

                IOperatorData data1 = pair1.Value;

                if ((data1 == null) || (data2 == null))
                {
                    if ((data1 != null) || (data2 != null))
                    {
                        if (data1 == null)
                        {
                            errorCount++;

                            if (results == null)
                                results = new ResultList();

                            results.Add(String.Format(
                                "invalid first object for {0}",
                                FormatOps.WrapOrNull(key1)));
                        }

                        if (data2 == null)
                        {
                            errorCount++;

                            if (results == null)
                                results = new ResultList();

                            results.Add(String.Format(
                                "invalid second object for {0}",
                                FormatOps.WrapOrNull(key1)));
                        }
                    }

                    continue;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IIdentifier Members
                if (data1.Kind == data2.Kind)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "kind for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Kind)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "kind mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Kind),
                        FormatOps.WrapOrNull(data2.Kind)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Id.Equals(data2.Id))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "id for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Id)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "id mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Id),
                        FormatOps.WrapOrNull(data2.Id)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Name, data2.Name))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "name for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Name)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "name mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Name),
                        FormatOps.WrapOrNull(data2.Name)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Group, data2.Group))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "group for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Group)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "group mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Group),
                        FormatOps.WrapOrNull(data2.Group)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (SharedStringOps.SystemEquals(
                        data1.Description, data2.Description))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "description for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Description)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "description mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Description),
                        FormatOps.WrapOrNull(data2.Description)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (Object.ReferenceEquals(
                        data1.ClientData, data2.ClientData))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "clientData for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ClientData)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "clientData mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ClientData),
                        FormatOps.WrapOrNull(data2.ClientData)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IOperatorData Members
                if (SharedStringOps.SystemEquals(
                        data1.TypeName, data2.TypeName))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "typeName for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.TypeName)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "typeName mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.TypeName),
                        FormatOps.WrapOrNull(data2.TypeName)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Lexeme == data2.Lexeme)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "lexeme for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Lexeme)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "lexeme mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Lexeme),
                        FormatOps.WrapOrNull(data2.Lexeme)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Operands == data2.Operands)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "operands for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Operands)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "operands mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Operands),
                        FormatOps.WrapOrNull(data2.Operands)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (TestCompareTypeLists(
                        data1.Types, data2.Types))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "types for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Types)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "types mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Types),
                        FormatOps.WrapOrNull(data2.Types)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.Flags == data2.Flags)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "flags for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Flags)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "flags mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Flags),
                        FormatOps.WrapOrNull(data2.Flags)));
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                if (data1.ComparisonType == data2.ComparisonType)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "comparisonType for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ComparisonType)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "comparisonType mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.ComparisonType),
                        FormatOps.WrapOrNull(data2.ComparisonType)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IHavePlugin Members
                if (Object.ReferenceEquals(
                        data1.Plugin, data2.Plugin))
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "plugin for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Plugin)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "plugin mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Plugin),
                        FormatOps.WrapOrNull(data2.Plugin)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IWrapperData Members
                if (data1.Token == data2.Token)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "token for {0} matched {1}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Token)));
                }
                else
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "token mismatch for {0}, {1} versus {2}",
                        FormatOps.WrapOrNull(key1),
                        FormatOps.WrapOrNull(data1.Token),
                        FormatOps.WrapOrNull(data2.Token)));
                }
                #endregion
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            foreach (OperatorDataPair pair2 in dictionary2)
            {
                string key2 = pair2.Key;
                IOperatorData data1;

                if (!dictionary1.TryGetValue(key2, out data1))
                {
                    errorCount++;

                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "first dictionary missing {0}",
                        FormatOps.WrapOrNull(key2)));
                }
            }

            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Static Properties
        public static Type StaticTypeProperty
        {
            get { return staticTypeField; }
            set { staticTypeField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object StaticObjectProperty
        {
            get { return staticObjectField; }
            set { staticObjectField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool StaticDynamicInvoke
        {
            get { return staticDynamicInvoke; }
            set { staticDynamicInvoke = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool DefaultThrowOnDisposed
        {
            get { return defaultThrowOnDisposed; }
            set { defaultThrowOnDisposed = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Properties
        public Type TypeProperty
        {
            get { CheckDisposed(); return typeField; }
            set { CheckDisposed(); typeField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object ObjectProperty
        {
            get { CheckDisposed(); return objectField; }
            set { CheckDisposed(); objectField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool SimpleBoolProperty
        {
            get { CheckDisposed(); return boolField; }
            set { CheckDisposed(); boolField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public byte SimpleByteProperty
        {
            get { CheckDisposed(); return byteField; }
            set { CheckDisposed(); byteField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public short SimpleShortProperty
        {
            get { CheckDisposed(); return shortField; }
            set { CheckDisposed(); shortField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int SimpleIntProperty
        {
            get { CheckDisposed(); return intField; }
            set { CheckDisposed(); intField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public long SimpleLongProperty
        {
            get { CheckDisposed(); return longField; }
            set { CheckDisposed(); longField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public decimal SimpleDecimalProperty
        {
            get { CheckDisposed(); return decimalField; }
            set { CheckDisposed(); decimalField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int ReadOnlyProperty
        {
            get { CheckDisposed(); return intField; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool DynamicInvoke
        {
            get { CheckDisposed(); return dynamicInvoke; }
            set { CheckDisposed(); dynamicInvoke = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool ThrowOnDispose
        {
            get { CheckDisposed(); return throwOnDispose; }
            set { CheckDisposed(); throwOnDispose = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string TempPath
        {
            get { CheckDisposed(); return tempPath; }
            set { CheckDisposed(); tempPath = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool TempException
        {
            get { CheckDisposed(); return tempException; }
            set { CheckDisposed(); tempException = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Indexer Properties
        public int this[int index]
        {
            get { CheckDisposed(); return intArrayField[index]; }
            set { CheckDisposed(); intArrayField[index] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int this[int index, string more]
        {
            get { CheckDisposed(); return intArrayField[index]; }
            set { CheckDisposed(); intArrayField[index] = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Complain Properties
        public ReturnCode ComplainCode
        {
            get { CheckDisposed(); return complainCode; }
            set { CheckDisposed(); complainCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Result ComplainResult
        {
            get { CheckDisposed(); return complainResult; }
            set { CheckDisposed(); complainResult = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int ComplainErrorLine
        {
            get { CheckDisposed(); return complainErrorLine; }
            set { CheckDisposed(); complainErrorLine = value; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Properties
        private static object StaticMiscellaneousData
        {
            get { return staticMiscellaneousData; }
            set { staticMiscellaneousData = (object[])value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object StaticMiscellaneousData0
        {
            get { return staticMiscellaneousData[0]; }
            set { staticMiscellaneousData[0] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object StaticMiscellaneousData1
        {
            get { return staticMiscellaneousData[1]; }
            set { staticMiscellaneousData[1] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object StaticMiscellaneousData2
        {
            get { return staticMiscellaneousData[2]; }
            set { staticMiscellaneousData[2] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object StaticMiscellaneousData3
        {
            get { return staticMiscellaneousData[3]; }
            set { staticMiscellaneousData[3] = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Properties
        private object MiscellaneousData
        {
            get { return miscellaneousData; }
            set { miscellaneousData = (object[])value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private object MiscellaneousData0
        {
            get { return miscellaneousData[0]; }
            set { miscellaneousData[0] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private object MiscellaneousData1
        {
            get { return miscellaneousData[1]; }
            set { miscellaneousData[1] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private object MiscellaneousData2
        {
            get { return miscellaneousData[2]; }
            set { miscellaneousData[2] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private object MiscellaneousData3
        {
            get { return miscellaneousData[3]; }
            set { miscellaneousData[3] = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ResultList Command Result Helper Class
        [ObjectId("f9c4c632-726d-4600-9c6e-58b9d48be208")]
        public sealed class ReturnAsList : IExecute
        {
            #region Public Constructors
            public ReturnAsList()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode AddToInterpreter(
                Interpreter interpreter,
                string name,
                IClientData clientData,
                ref long token,
                ref Result error
                )
            {
                return Helpers.AddToInterpreter(
                    interpreter, name, this, clientData, ref token,
                    ref error);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecute Members
            public ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }

                int argumentCount = arguments.Count;

                if (argumentCount < 2)
                {
                    result = String.Format(
                        "wrong # args: should be \"{0} flags ?arg ...?\"",
                        null);

                    return ReturnCode.Error;
                }

                string value = arguments[1];
                ResultFlags flags = ResultFlags.DefaultListMask;

                if (!String.IsNullOrEmpty(value))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(ResultFlags), flags.ToString(),
                        value, interpreter.InternalCultureInfo, true, true,
                        true, ref result);

                    if (!(enumValue is ResultFlags))
                        return ReturnCode.Error;

                    flags = (ResultFlags)enumValue;
                }

                ResultList results = new ResultList(flags);

                for (int index = 2; index < argumentCount; index++)
                    results.Add(arguments[index]);

                result = results;
                return ReturnCode.Ok;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region CrossAppDomainHelper Helper Class
#if !NET_STANDARD_20
        [ObjectId("40db6d0e-c718-4fe6-9b1f-9fb108e8be68")]
        public sealed class TestCrossAppDomainHelper :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            ScriptMarshalByRefObject,
#endif
            IDisposable
        {
            #region Private Data
            private readonly object syncRoot = new object();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Static Methods
            public static string GetTypeName()
            {
                return typeof(TestCrossAppDomainHelper).FullName;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public TestCrossAppDomainHelper()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private string text;
            public string Text /* in */
            {
                get { CheckDisposed(); lock (syncRoot) { return text; } }
                set { CheckDisposed(); lock (syncRoot) { text = value; } }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ReturnCode returnCode;
            public ReturnCode ReturnCode /* out */
            {
                get { CheckDisposed(); lock (syncRoot) { return returnCode; } }
                set { CheckDisposed(); lock (syncRoot) { returnCode = value; } }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Result result;
            public Result Result /* out */
            {
                get { CheckDisposed(); lock (syncRoot) { return result; } }
                set { CheckDisposed(); lock (syncRoot) { result = value; } }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public void Evaluate() /* System.CrossAppDomainDelegate */
            {
                CheckDisposed();

                string localText;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localText = text;
                }

                ReturnCode localCode = ReturnCode.Ok; /* REUSED */
                Result localResult = null; /* REUSED */

                try
                {
                    Interpreter interpreter = Interpreter.GetFirst();

                    if (interpreter != null)
                    {
                        localResult = null;

                        localCode = interpreter.EvaluateScript(
                            localText, ref localResult);
                    }
                    else
                    {
                        localResult = null;

                        using (interpreter = Interpreter.Create(
                                ref localResult))
                        {
                            if (interpreter != null)
                            {
                                localResult = null;

                                localCode = interpreter.EvaluateScript(
                                    localText, ref localResult);
                            }
                            else
                            {
                                localCode = ReturnCode.Error;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    localResult = e;
                    localCode = ReturnCode.Error;
                }
                finally
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        result = localResult;
                        returnCode = localCode;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(TestCrossAppDomainHelper).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            text = null;
                            returnCode = ReturnCode.Ok;
                            result = null;
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////

                        disposed = true;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~TestCrossAppDomainHelper()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Helpers Class
        [ObjectId("0ea7f7bb-bf64-4a11-b11c-20ff8be94024")]
        public static class Helpers
        {
            #region AsynchronousCallbackClientData Helper Class
            [ObjectId("e93df7af-ad99-4fc5-b3f5-f38b12c6115b")]
            public sealed class AsynchronousCallbackClientData : ClientData, IGetClientData
            {
                #region Private Constructors
                internal AsynchronousCallbackClientData(
                    AsynchronousCallback callback, /* in */
                    IClientData clientData,        /* in */
                    StringList objectNames,        /* in */
                    long instanceId,               /* in */
                    bool synchronous,              /* in */
                    bool dispose                   /* in */
                    )
                    : base(null, true)
                {
                    this.callback = callback;
                    this.clientData = clientData;
                    this.objectNames = objectNames;
                    this.instanceId = instanceId;
                    this.synchronous = synchronous;
                    this.dispose = dispose;
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region IGetClientData Members
                private IClientData clientData;
                public IClientData ClientData
                {
                    get { return clientData; }
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Public Properties
                private AsynchronousCallback callback;
                public AsynchronousCallback Callback
                {
                    get { return callback; }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                private StringList objectNames;
                public StringList ObjectNames
                {
                    get { return objectNames; }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                private long instanceId;
                public long InstanceId
                {
                    get { return instanceId; }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                private bool synchronous;
                public bool Synchronous
                {
                    get { return synchronous; }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                private bool dispose;
                public bool Dispose
                {
                    get { return dispose; }
                }
                #endregion
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constants
            //
            // NOTE: The object flags to use when calling FixupReturnValue on the
            //       various method parameters passed required by the script being
            //       evaluated to handle formal interface methods.
            //
            private static readonly ObjectFlags DefaultObjectFlags =
                ObjectFlags.Default | ObjectFlags.NoBinder |
                ObjectFlags.NoDispose | ObjectFlags.AddReference;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The object option type to use when calling FixupReturnValue
            //       on the various method parameters passed required by the
            //       script being evaluated to handle formal interface methods.
            //
            private static readonly ObjectOptionType DefaultObjectOptionType =
                ObjectOptionType.Default;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private static ReturnCode SaveOrRestoreObjects(
                Interpreter interpreter, /* in */
                StringList objectNames,  /* in */
                long instanceId,         /* in */
                bool restore,            /* in */
                ref Result error         /* out */
                )
            {
                int count = 0;

                return SaveOrRestoreObjects(
                    interpreter, objectNames, instanceId, restore,
                    ref count, ref error);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static ReturnCode SaveOrRestoreObjects(
                Interpreter interpreter, /* in */
                StringList objectNames,  /* in */
                long instanceId,         /* in */
                bool restore,            /* in */
                ref int count,           /* out */
                ref Result error         /* out */
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (objectNames == null)
                {
                    error = "invalid object names";
                    return ReturnCode.Error;
                }

                int localCount = 0;

                foreach (string oldName in objectNames)
                {
                    if (oldName == null)
                        continue;

                    string newName = String.Format(
                        "{0}_{1}", oldName, instanceId);

                    Result localResult = null;

                    if (restore)
                    {
                        if (interpreter.DoesObjectExist(
                                newName) != ReturnCode.Ok)
                        {
                            continue;
                        }

                        if (interpreter.RenameObject(
                                newName, oldName, false, false, false,
                                ref localResult) != ReturnCode.Ok)
                        {
                            error = localResult;
                            return ReturnCode.Error;
                        }

                        localCount++;
                    }
                    else
                    {
                        if (interpreter.DoesObjectExist(
                                oldName) != ReturnCode.Ok)
                        {
                            continue;
                        }

                        if (interpreter.RenameObject(
                                oldName, newName, false, false, false,
                                ref localResult) != ReturnCode.Ok)
                        {
                            error = localResult;
                            return ReturnCode.Error;
                        }

                        localCount++;
                    }
                }

                count += localCount;
                return ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static ReturnCode FixupReturnValue(
                Interpreter interpreter, /* in */
                string objectName,       /* in */
                object value,            /* in */
                ObjectFlags objectFlags, /* in */
                ref Result result        /* out */
                )
            {
                return MarshalOps.FixupReturnValue(
                    interpreter, null, objectFlags | DefaultObjectFlags,
                    null, DefaultObjectOptionType, objectName, value, true,
                    true, false, ref result);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static ReturnCode RemoveObject(
                Interpreter interpreter, /* in */
                string objectName,       /* in */
                bool synchronous,        /* in */
                bool dispose,            /* in */
                ref Result result        /* out */
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                return interpreter.RemoveObject(
                    objectName, _Public.ClientData.Empty, synchronous,
                    ref dispose, ref result);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static IAsynchronousContext CopyAsynchronousContext(
                IAsynchronousContext context,  /* in */
                AsynchronousCallback callback, /* in */
                IClientData clientData         /* in */
                )
            {
                if (context == null)
                    return null;

                return new AsynchronousContext(
                    context.ThreadId, context.EngineMode,
                    context.Interpreter, context.Text,
                    context.EngineFlags, context.SubstitutionFlags,
                    context.EventFlags, context.ExpressionFlags,
                    callback, clientData);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static ReturnCode BeginChannelRedirection(
                Interpreter interpreter,     /* in */
                Stream inputStream,          /* in: OPTIONAL */
                Stream outputStream,         /* in: OPTIONAL */
                Stream errorStream,          /* in: OPTIONAL */
                HostFlags hostFlags,         /* in */
                ref ChannelType channelType, /* in, out */
                ref IStreamHost streamHost,  /* out */
                ref Result error             /* out */
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                IStreamHost newStreamHost = new ScriptStreamHost(
                    inputStream, outputStream, errorStream, null,
                    null, null, Defaults.HostCreateFlags, hostFlags);

                ChannelType newChannelType = channelType;

                newChannelType &= ~ChannelType.StandardChannels;

                if (inputStream != null)
                    newChannelType |= ChannelType.Input;

                if (outputStream != null)
                    newChannelType |= ChannelType.Output;

                if (errorStream != null)
                    newChannelType |= ChannelType.Error;

                if (!FlagOps.HasFlags(
                        newChannelType, ChannelType.StandardChannels,
                        false))
                {
                    error = "no channels selected for redirection";
                    return ReturnCode.Error;
                }

                newChannelType &= ~ChannelType.EndContext;
                newChannelType &= ~ChannelType.UseHost;
                newChannelType |= ChannelType.ErrorOnNull;
                newChannelType |= ChannelType.BeginContext;
                newChannelType |= ChannelType.AllowExist;

                if (interpreter.ModifyStandardChannels(
                        newStreamHost, null, newChannelType,
                        ref error) == ReturnCode.Ok)
                {
                    channelType = newChannelType;
                    streamHost = newStreamHost;

                    return ReturnCode.Ok;
                }

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static ReturnCode EndChannelRedirection(
                Interpreter interpreter, /* in */
                ChannelType channelType, /* in */
                IStreamHost streamHost,  /* in */
                ref Result error         /* out */
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                ChannelType newChannelType = channelType;

                newChannelType &= ~ChannelType.BeginContext;
                newChannelType &= ~ChannelType.UseHost;
                newChannelType |= ChannelType.AllowExist;
                newChannelType |= ChannelType.EndContext;
                newChannelType |= ChannelType.SkipGetStream;

                return interpreter.ModifyStandardChannels(
                    streamHost, null, newChannelType, ref error);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
            private static void BeginConsoleRedirection(
                TextReader inputReader,           /* in */
                TextWriter outputReader,          /* in */
                TextWriter errorReader,           /* in */
                out TextReader savedInputReader,  /* out */
                out TextWriter savedOutputReader, /* out */
                out TextWriter savedErrorReader   /* out */
                )
            {
                savedInputReader = (inputReader != null) ?
                    Console.In : null;

                savedOutputReader = (outputReader != null) ?
                    Console.Out : null;

                savedErrorReader = (errorReader != null) ?
                    Console.Error : null;

                if (inputReader != null)
                    Console.SetIn(inputReader);

                if (outputReader != null)
                    Console.SetOut(outputReader);

                if (errorReader != null)
                    Console.SetError(errorReader);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static void EndConsoleRedirection(
                ref TextReader savedInputReader,  /* in, out */
                ref TextWriter savedOutputReader, /* in, out */
                ref TextWriter savedErrorReader   /* in, out */
                )
            {
                if (savedInputReader != null)
                {
                    Console.SetIn(savedInputReader);
                    savedInputReader = null;
                }

                if (savedOutputReader != null)
                {
                    Console.SetOut(savedOutputReader);
                    savedOutputReader = null;
                }

                if (savedErrorReader != null)
                {
                    Console.SetError(savedErrorReader);
                    savedErrorReader = null;
                }
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Eagle._Components.Public.Delegates.AsynchronousCallback
            private static void AsynchronousCallbackWrapper(
                IAsynchronousContext context /* in */
                )
            {
                if (context == null)
                    return;

                AsynchronousCallbackClientData clientData =
                    context.ClientData as AsynchronousCallbackClientData;

                if (clientData == null)
                    return;

                try
                {
                    AsynchronousCallback callback = clientData.Callback;

                    if (callback != null)
                    {
                        callback(CopyAsynchronousContext(
                            context, clientData.Callback,
                            clientData.ClientData));
                    }
                }
                finally
                {
                    /* NO RESULT */
                    RemoveObjects(
                        context.Interpreter, clientData.ObjectNames,
                        clientData.InstanceId, clientData.Synchronous,
                        clientData.Dispose);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public static ReturnCode AddToInterpreter(
                Interpreter interpreter,
                string name,
                IExecute execute,
                IClientData clientData,
                ref long token,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                Result result = null;

                if (interpreter.AddIExecute(
                        name, execute, clientData, ref token,
                        ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode RemoveFromInterpreter(
                Interpreter interpreter,
                long token,
                IClientData clientData,
                IdentifierKind kind,
                ref bool dispose,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (token == 0)
                {
                    error = "invalid token";
                    return ReturnCode.Error;
                }

                Result result = null;

                switch (kind)
                {
                    case IdentifierKind.Interpreter:
                        {
                            //
                            // HACK: This is not technically correct;
                            //       however, since the interpreters
                            //       are using integer identifiers at
                            //       the moment, it will work.
                            //
                            if (interpreter.RemoveChildInterpreter(
                                    token.ToString(), clientData,
                                    ref error) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Policy:
                        {
                            if (interpreter.RemovePolicy(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Trace:
                        {
                            if (interpreter.RemoveTrace(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Command:
                        {
                            if (interpreter.RemoveCommand(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.HiddenCommand:
                        {
                            if (interpreter.RemoveHiddenCommand(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Procedure:
                        {
                            if (interpreter.RemoveProcedure(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.HiddenProcedure:
                        {
                            if (interpreter.RemoveHiddenProcedure(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.IExecute:
                        {
                            if (interpreter.RemoveIExecute(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.HiddenIExecute:
                        {
                            if (interpreter.RemoveHiddenIExecute(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Function:
                        {
                            if (interpreter.RemoveFunction(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Package:
                        {
                            if (interpreter.RemovePackage(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Plugin:
                        {
                            if (interpreter.RemovePlugin(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.Object:
                        {
                            if (interpreter.RemoveObject(
                                    token, clientData, ref dispose,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
#if EMIT && NATIVE && LIBRARY
                    case IdentifierKind.NativeModule:
                        {
                            if (interpreter.RemoveModule(
                                    token, clientData, ref dispose,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.NativeDelegate:
                        {
                            if (interpreter.RemoveDelegate(
                                    token, clientData, ref dispose,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
#endif
                    case IdentifierKind.Alias:
                        {
                            if (interpreter.RemoveAlias(
                                    token, clientData,
                                    ref result) != ReturnCode.Ok)
                            {
                                error = result;
                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case IdentifierKind.AnyIExecute:
                    case IdentifierKind.Ensemble:
                    case IdentifierKind.Lambda:
                    case IdentifierKind.Operator:
                    case IdentifierKind.Variable:
                    case IdentifierKind.CallFrame:
                    case IdentifierKind.Host:
                    case IdentifierKind.Callback:
                    case IdentifierKind.Resolve:
                    case IdentifierKind.Namespace:
                    case IdentifierKind.SubCommand:
                    case IdentifierKind.InteractiveLoopData:
                    case IdentifierKind.ShellCallbackData:
                    case IdentifierKind.UpdateData:
                    case IdentifierKind.Certificate:
                    case IdentifierKind.KeyPair:
                    case IdentifierKind.KeyRing:
                    case IdentifierKind.Channel:
                    case IdentifierKind.None:
                    case IdentifierKind.PolicyData:
                    case IdentifierKind.TraceData:
                    case IdentifierKind.CommandData:
                    case IdentifierKind.SubCommandData:
                    case IdentifierKind.ProcedureData:
                    case IdentifierKind.LambdaData:
                    case IdentifierKind.OperatorData:
                    case IdentifierKind.FunctionData:
                    case IdentifierKind.EnsembleData:
                    case IdentifierKind.PackageData:
                    case IdentifierKind.PluginData:
                    case IdentifierKind.ObjectData:
                    case IdentifierKind.ObjectTypeData:
                    case IdentifierKind.ObjectType:
                    case IdentifierKind.Option:
                    case IdentifierKind.HostData:
                    case IdentifierKind.AliasData:
                    case IdentifierKind.DelegateData:
                    case IdentifierKind.Delegate:
                    case IdentifierKind.SubDelegate:
                    case IdentifierKind.ResolveData:
                    case IdentifierKind.ClockData:
                    case IdentifierKind.Script:
                    case IdentifierKind.ScriptBuilder:
                    case IdentifierKind.NamespaceData:
                    case IdentifierKind.Path:
                        {
                            error = "identifier kind not supported";
                            return ReturnCode.Error;
                        }
                    default:
                        {
                            error = "unknown identifier kind";
                            return ReturnCode.Error;
                        }
                }

                return ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static Interpreter CreateInterpreterWithCommands(
                InterpreterSettings interpreterSettings, /* in: OPTIONAL */
                IEnumerable<CommandTriplet> commands,    /* in: OPTIONAL */
                IClientData clientData,                  /* in: OPTIONAL */
                IPlugin plugin,                          /* in: OPTIONAL */
                ref Result error                         /* out */
                )
            {
                Interpreter interpreter;
                Result result = null; /* REUSED */

                interpreter = Interpreter.Create(
                    interpreterSettings, false, ref result);

                if (interpreter == null)
                {
                    error = result;
                    return null;
                }

                if (commands != null)
                {
                    foreach (CommandTriplet anyTriplet in commands)
                    {
                        if (anyTriplet == null)
                            continue;

                        Type type = anyTriplet.Y;

                        if (type == null)
                            continue;

                        string name = anyTriplet.X;

                        if (name == null)
                            name = type.Name;

                        ICommandData commandData = new CommandData(
                            name, null, null, clientData, type.FullName,
                            CommandFlags.None, plugin, 0);

                        try
                        {
                            ICommand command = Activator.CreateInstance(
                                type, commandData) as ICommand; /* throw */

                            if (command == null)
                            {
                                interpreter.Dispose(); /* throw */
                                interpreter = null;

                                error = String.Format(
                                    "cannot create instance of type {0}",
                                    MarshalOps.GetErrorTypeName(type));

                                break;
                            }

                            long token = 0;

                            result = null;

                            if (interpreter.AddCommand(
                                    command, null, ref token,
                                    ref result) == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Save new command token to its
                                //       associated command descriptor
                                //       for use by the caller.  This
                                //       can be done if the triplet is
                                //       actually mutable.
                                //
                                if (anyTriplet.Mutable)
                                    anyTriplet.Z = token;
                            }
                            else
                            {
                                interpreter.Dispose(); /* throw */
                                interpreter = null;

                                error = result;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                if (interpreter != null)
                                {
                                    interpreter.Dispose(); /* throw */
                                    interpreter = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                TraceOps.DebugTrace(
                                    ex, typeof(Helpers).Name,
                                    TracePriority.CleanupError);
                            }

                            error = e;
                            break;
                        }
                    }
                }

                return interpreter;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode FixupReturnValues(
                Interpreter interpreter,    /* in */
                ObjectDictionary objects,   /* in */
                ObjectFlags objectFlags,    /* in */
                long instanceId,            /* in */
                ref StringList objectNames, /* in, out */
                ref Result error            /* out */
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (objects != null)
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        if (Interpreter.IsDeletedOrDisposed(
                                interpreter, false, ref error))
                        {
                            return ReturnCode.Error;
                        }

                        StringList localObjectNames = new StringList(objects.Keys);

                        if (SaveOrRestoreObjects(
                                interpreter, localObjectNames, instanceId, false,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        foreach (KeyValuePair<string, object> pair in objects)
                        {
                            string objectName = pair.Key;

                            if (String.IsNullOrEmpty(objectName))
                                objectName = null; /* AUTOMATIC */

                            Result localResult = null;

                            if (FixupReturnValue(interpreter,
                                    objectName, pair.Value, objectFlags,
                                    ref localResult) != ReturnCode.Ok)
                            {
                                error = localResult;
                                return ReturnCode.Error;
                            }

                            if (localResult != null)
                            {
                                if (objectNames == null)
                                    objectNames = new StringList();

                                objectNames.Add(localResult);
                            }
                        }
                    }
                }

                return ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static void RemoveObjects(
                Interpreter interpreter, /* in */
                StringList objectNames,  /* in */
                long instanceId,         /* in */
                bool synchronous,        /* in */
                bool dispose             /* in */
                )
            {
                if ((interpreter != null) && (objectNames != null))
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        if (Interpreter.IsDeletedOrDisposed(interpreter, false))
                            return;

                        foreach (string objectName in objectNames)
                        {
                            if (objectName == null)
                                continue;

                            ReturnCode removeCode;
                            Result removeResult = null;

                            removeCode = RemoveObject(
                                interpreter, objectName, synchronous,
                                dispose, ref removeResult);

                            if (removeCode != ReturnCode.Ok)
                            {
                                DebugOps.Complain(
                                    interpreter, removeCode, removeResult);
                            }
                        }

                        ReturnCode restoreCode;
                        Result restoreError = null;

                        restoreCode = SaveOrRestoreObjects(
                            interpreter, objectNames, instanceId, true,
                            ref restoreError);

                        if (restoreCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, restoreCode, restoreError);
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode EvaluateScript(
                Interpreter interpreter,  /* in */
                string text,              /* in */
                ObjectDictionary objects, /* in */
                ref Result result         /* out */
                )
            {
                return EvaluateScript(
                    interpreter, text, objects, ObjectFlags.None,
                    ObjectOps.GetDefaultSynchronous(),
                    ObjectOps.GetDefaultDispose(), ref result);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode EvaluateScript(
                Interpreter interpreter,  /* in */
                string text,              /* in */
                ObjectDictionary objects, /* in */
                ObjectFlags objectFlags,  /* in */
                bool synchronous,         /* in */
                bool dispose,             /* in */
                ref Result result         /* out */
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                long instanceId = interpreter.NextId();
                StringList objectNames = null;

                try
                {
                    Result error = null;

                    if (FixupReturnValues(
                            interpreter, objects, objectFlags,
                            instanceId, ref objectNames,
                            ref error) != ReturnCode.Ok)
                    {
                        result = error;
                        return ReturnCode.Error;
                    }

                    return interpreter.EvaluateScript(
                        text, ref result);
                }
                finally
                {
                    /* NO RESULT */
                    RemoveObjects(
                        interpreter, objectNames, instanceId,
                        synchronous, dispose);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode EvaluateScript(
                Interpreter interpreter,       /* in */
                string text,                   /* in */
                AsynchronousCallback callback, /* in */
                IClientData clientData,        /* in */
                ObjectDictionary objects,      /* in */
                ObjectFlags objectFlags,       /* in */
                bool synchronous,              /* in */
                bool dispose,                  /* in */
                ref Result error               /* out */
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                long instanceId = interpreter.NextId();
                StringList objectNames = null;

                if (FixupReturnValues(
                        interpreter, objects, objectFlags,
                        instanceId, ref objectNames,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                return interpreter.EvaluateScript(
                    text, AsynchronousCallbackWrapper,
                    new AsynchronousCallbackClientData(
                        callback, clientData, objectNames,
                        instanceId, synchronous, dispose),
                    ref error);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode EvaluateScriptWithChannelRedirection(
                Interpreter interpreter, /* in */
                IScript script,          /* in */
                Stream inputStream,      /* in: OPTIONAL */
                Stream outputStream,     /* in: OPTIONAL */
                Stream errorStream,      /* in: OPTIONAL */
                HostFlags hostFlags,     /* in */
                ChannelType channelType, /* in */
                ref Result result,       /* out */
                ref int errorLine        /* out */
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                ChannelType newChannelType = channelType;
                IStreamHost streamHost = null;
                Result error; /* REUSED */

                error = null;

                if (BeginChannelRedirection(
                        interpreter, inputStream, outputStream,
                        errorStream, hostFlags, ref newChannelType,
                        ref streamHost, ref error) != ReturnCode.Ok)
                {
                    result = error;
                    return ReturnCode.Error;
                }

                try
                {
                    return interpreter.EvaluateScript(
                        script, ref result, ref errorLine);
                }
                finally
                {
                    error = null;

                    if (EndChannelRedirection(
                            interpreter, newChannelType, streamHost,
                            ref error) != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "EvaluateScriptWithChannelRedirection: " +
                            "EndChannelRedirection, error = {0}",
                            FormatOps.WrapOrNull(error)),
                            typeof(Helpers).Name,
                            TracePriority.CleanupError);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
            public static ReturnCode EvaluateScriptWithConsoleRedirection(
                Interpreter interpreter, /* in */
                IScript script,          /* in */
                TextReader inputReader,  /* in: OPTIONAL */
                TextWriter outputReader, /* in: OPTIONAL */
                TextWriter errorReader,  /* in: OPTIONAL */
                ref Result result,       /* out */
                ref int errorLine        /* out */
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                TextReader savedInputReader;
                TextWriter savedOutputReader;
                TextWriter savedErrorReader;

                BeginConsoleRedirection(
                    inputReader, outputReader, errorReader,
                    out savedInputReader, out savedOutputReader,
                    out savedErrorReader);

                try
                {
                    return interpreter.EvaluateScript(
                        script, ref result, ref errorLine);
                }
                finally
                {
                    EndConsoleRedirection(
                        ref savedInputReader, ref savedOutputReader,
                        ref savedErrorReader);
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static void WriteString(
                Stream stream,     /* in */
                Encoding encoding, /* in */
                string value,      /* in */
                bool newLine,      /* in */
                bool flush         /* in */
                )
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");

                if (value == null)
                    throw new ArgumentNullException("value");

                Encoding localEncoding = (encoding != null) ?
                    encoding : Encoding.UTF8;

                byte[] bytes; /* REUSED */

                bytes = localEncoding.GetBytes(value);

                if (bytes == null)
                    throw new InvalidOperationException();

                stream.Write(bytes, 0, bytes.Length);

                if (newLine)
                {
                    bytes = localEncoding.GetBytes(Characters.DosNewLine);

                    if (bytes == null)
                        throw new InvalidOperationException();

                    stream.Write(bytes, 0, bytes.Length);
                }

                if (flush)
                    stream.Flush();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode ToBoolean(
                Interpreter interpreter, /* in */
                Result result,           /* in */
                ref bool value,          /* out */
                ref Result error         /* out */
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                return Value.GetBoolean3(
                    result, ValueFlags.AnyBoolean, interpreter.InternalCultureInfo,
                    ref value, ref error);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode GetResultFromDictionary(
                Interpreter interpreter, /* in: OPTIONAL */
                string value,            /* in */
                ref Result result        /* out */
                )
            {
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        interpreter, value, 0, Length.Invalid, true,
                        ref list, ref result) == ReturnCode.Ok)
                {
                    try
                    {
                        StringDictionary dictionary = new StringDictionary(
                            list, true, true); /* throw */

                        string element; /* REUSED */
                        ReturnCode? returnCode = null;

                        if (dictionary.TryGetValue("returnCode", out element))
                        {
                            object enumValue = EnumOps.TryParse(
                                typeof(ReturnCode), element, true, true,
                                ref result);

                            if (enumValue == null)
                                return ReturnCode.Error;

                            returnCode = (ReturnCode)enumValue;
                        }

                        string stringValue = null;

                        if (dictionary.TryGetValue("result", out element))
                            stringValue = element;

                        int? errorLine = null;

                        if (dictionary.TryGetValue("errorLine", out element))
                        {
                            CultureInfo cultureInfo = null;

                            if (interpreter != null)
                                cultureInfo = interpreter.InternalCultureInfo;

                            int intValue = 0;

                            if (Value.GetInteger2(
                                    element, ValueFlags.AnyInteger, cultureInfo,
                                    ref intValue, ref result) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            errorLine = intValue;
                        }

                        result = (stringValue != null) ?
                            stringValue : String.Empty;

                        if (returnCode != null)
                            result.ReturnCode = (ReturnCode)returnCode;

                        if (errorLine != null)
                            result.ErrorLine = (int)errorLine;

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        result = e;
                    }
                }

                return ReturnCode.Error;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptStreamHost Test Class
        [ObjectId("fadcc915-e31e-4004-aeb4-4218392c9fff")]
        public class ScriptStreamHost : _Hosts.Fake
        {
            #region Private Constants
            private static readonly string typeName = typeof(ScriptStreamHost).Name;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ScriptStreamHost(
                IHostData hostData,
                HostFlags hostFlags
                )
                : base(hostData)
            {
                this.hostFlags = hostFlags;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ScriptStreamHost(
                Stream input,
                Stream output,
                Stream error,
                Encoding inputEncoding,
                Encoding outputEncoding,
                Encoding errorEncoding,
                HostCreateFlags hostCreateFlags,
                HostFlags hostFlags
                )
                : this(HostOps.NewData(typeName, hostCreateFlags), hostFlags)
            {
                base.In = input;
                base.Out = output;
                base.Error = error;
                base.InputEncoding = inputEncoding;
                base.OutputEncoding = outputEncoding;
                base.ErrorEncoding = errorEncoding;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IInteractiveHost Members
            private HostFlags hostFlags;
            public override HostFlags GetHostFlags()
            {
                return hostFlags;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IStreamHost Members
            public override Stream DefaultIn
            {
                get { return base.In; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override Stream DefaultOut
            {
                get { return base.Out; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override Stream DefaultError
            {
                get { return base.Error; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool SetupChannels()
            {
                return false;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptClient Test Class
#if NET_35 || NET_40 || NET_STANDARD_20
        [ObjectId("d7624c68-da17-478d-9fc7-9efd94bdc6b8")]
        public class ScriptClient : IDisposable
        {
            #region Private Constants
            internal static readonly string LocalHost = Characters.Period.ToString();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Interpreter interpreter;
            private string serverName;
            private string pipeName;
            private bool trace;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ScriptClient(
                Interpreter interpreter,
                string serverName,
                string pipeName,
                bool trace
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    this.interpreter = interpreter;
                    this.serverName = serverName;
                    this.pipeName = pipeName;
                    this.trace = trace;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public virtual ReturnCode Send(
                string text,
                int timeout,
                ref Result result
                )
            {
                CheckDisposed();

                Interpreter localInterpreter;
                string localServerName;
                string localPipeName;
                bool localTrace;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localInterpreter = interpreter;
                    localServerName = serverName;
                    localPipeName = pipeName;
                    localTrace = trace;
                }

                if (localInterpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (localServerName == null)
                {
                    result = "invalid server name";
                    return ReturnCode.Error;
                }

                if (localPipeName == null)
                {
                    result = "invalid pipe name";
                    return ReturnCode.Error;
                }

                using (NamedPipeClientStream stream = new NamedPipeClientStream(
                        localServerName, localPipeName))
                {
                    if (localTrace)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Send: connecting, server {0}, pipe {1}",
                            FormatOps.WrapOrNull(localServerName),
                            FormatOps.WrapOrNull(localPipeName)),
                            typeof(ScriptClient).Name,
                            TracePriority.NetworkDebug);
                    }

                    stream.Connect(timeout);

                    if (localTrace)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Send: connected, server {0}, pipe {1}",
                            FormatOps.WrapOrNull(localServerName),
                            FormatOps.WrapOrNull(localPipeName)),
                            typeof(ScriptClient).Name,
                            TracePriority.NetworkDebug);
                    }

                    if (text != null)
                    {
                        using (StreamReader streamReader = new StreamReader(
                                stream))
                        {
                            Helpers.WriteString(
                                stream, null, text, true, true);

                            Helpers.WriteString(
                                stream, null, ScriptServer.EndOfData, true,
                                true);

                            if (localTrace)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "Send: sent script/end-of-data: {0}",
                                    FormatOps.ScriptForLog(true, false, text)),
                                    typeof(ScriptClient).Name,
                                    TracePriority.NetworkDebug2);
                            }

                            if (Helpers.GetResultFromDictionary(
                                    localInterpreter, streamReader.ReadToEnd(),
                                    ref result) == ReturnCode.Ok)
                            {
                                if (localTrace)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "Send: received/extracted result: {0}",
                                        FormatOps.ScriptForLog(true, false, result)),
                                        typeof(ScriptClient).Name,
                                        TracePriority.NetworkDebug2);
                                }

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                if (localTrace)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "Send: failed to extract result {0}",
                                        FormatOps.WrapOrNull(result)),
                                        typeof(ScriptClient).Name,
                                        TracePriority.NetworkDebug2);
                                }

                                return ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptClient).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(
                bool disposing
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (trace)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Dispose: called, disposing = {0}, disposed = {1}",
                            disposing, disposed), typeof(ScriptClient).Name,
                            TracePriority.CleanupDebug);
                    }

                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            interpreter = null; /* NOT OWNED */
                            serverName = null;
                            pipeName = null;
                            trace = false;
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////

                        disposed = true;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptClient()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptServer Test Class
#if NET_35 || NET_40 || NET_STANDARD_20
        [ObjectId("ff4501a0-dce2-4f48-80ec-7f07f24cceb6")]
        public class ScriptServer : IDisposable
        {
            #region Private Constants
            internal const string EndOfData = "### END-OF-DATA ###";
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Interpreter interpreter;
            private string pipeName;
            private bool autoStop;
            private bool trace;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private DisposeCallback preDisposeCallback;
            private int startCount;
            private int stopCount;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ScriptServer(
                Interpreter interpreter,
                string pipeName,
                bool autoStop,
                bool trace
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    this.interpreter = interpreter;
                    this.pipeName = pipeName;
                    this.autoStop = autoStop;
                    this.trace = trace;
                }

                if (autoStop)
                    SetupAutoStop();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public virtual ReturnCode Start(
                ref Result error
                )
            {
                CheckDisposed();

                Interpreter localInterpreter;
                string localPipeName;
                bool localTrace;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localInterpreter = interpreter;
                    localPipeName = pipeName;
                    localTrace = trace;
                }

                if (localInterpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (localPipeName == null)
                {
                    error = "invalid pipe name";
                    return ReturnCode.Error;
                }

                int localStartCount = Interlocked.Increment(ref startCount);

                try
                {
                    if (localStartCount == 1)
                    {
                        using (NamedPipeServerStream stream = new NamedPipeServerStream(
                                localPipeName))
                        {
                            using (StreamReader streamReader = new StreamReader(stream))
                            {
                                /* IGNORED */
                                Interlocked.Exchange(ref stopCount, 0);

                                while (true)
                                {
                                    if (Interlocked.CompareExchange(ref stopCount, 0, 0) > 0)
                                    {
                                        if (localTrace)
                                        {
                                            TraceOps.DebugTrace(
                                                "Start: stopped (1)", typeof(ScriptServer).Name,
                                                TracePriority.NetworkDebug);
                                        }

                                        break;
                                    }

                                    try
                                    {
                                        if (localTrace)
                                        {
                                            TraceOps.DebugTrace(String.Format(
                                                "Start: waiting, interpreter {0}, pipe {1}",
                                                FormatOps.InterpreterNoThrow(localInterpreter),
                                                FormatOps.WrapOrNull(localPipeName)),
                                                typeof(ScriptServer).Name,
                                                TracePriority.NetworkDebug);
                                        }

                                        stream.WaitForConnection();

                                        if (localTrace)
                                        {
                                            TraceOps.DebugTrace(String.Format(
                                                "Start: connected, interpreter {0}, pipe {1}",
                                                FormatOps.InterpreterNoThrow(localInterpreter),
                                                FormatOps.WrapOrNull(localPipeName)),
                                                typeof(ScriptServer).Name,
                                                TracePriority.NetworkDebug);
                                        }

                                        if (Interlocked.CompareExchange(ref stopCount, 0, 0) > 0)
                                        {
                                            if (localTrace)
                                            {
                                                TraceOps.DebugTrace(
                                                    "Start: stopped (2)", typeof(ScriptServer).Name,
                                                    TracePriority.NetworkDebug);
                                            }

                                            break;
                                        }

                                        StringBuilder builder = StringOps.NewStringBuilder();

                                        while (true)
                                        {
                                            string line = streamReader.ReadLine();

                                            if (line == null)
                                                break;

                                            if (SharedStringOps.SystemEquals(line, EndOfData))
                                                break;

                                            builder.AppendLine(line);
                                        }

                                        string text = builder.ToString();

                                        builder.Length = 0;

                                        if (localTrace)
                                        {
                                            TraceOps.DebugTrace(String.Format(
                                                "Start: received script: {0}",
                                                FormatOps.ScriptForLog(true, false, text)),
                                                typeof(ScriptServer).Name,
                                                TracePriority.NetworkDebug2);
                                        }

                                        ReturnCode code;
                                        Result result = null;
                                        int errorLine = 0;

                                        code = localInterpreter.EvaluateScript(
                                            text, ref result, ref errorLine);

                                        text = StringList.MakeList(
                                            "returnCode", code, "result", result,
                                            "errorLine", errorLine);

                                        Helpers.WriteString(
                                            stream, null, text, false, true);

                                        if (localTrace)
                                        {
                                            TraceOps.DebugTrace(String.Format(
                                                "Start: sent response: {0}",
                                                FormatOps.ScriptForLog(true, false, text)),
                                                typeof(ScriptServer).Name,
                                                TracePriority.NetworkDebug2);
                                        }
                                    }
                                    finally
                                    {
                                        if (stream != null)
                                        {
                                            try
                                            {
                                                stream.WaitForPipeDrain();
                                            }
                                            catch (Exception e)
                                            {
                                                if (localTrace)
                                                {
                                                    TraceOps.DebugTrace(
                                                        e, typeof(ScriptServer).Name,
                                                        TracePriority.NetworkError);
                                                }
                                            }

                                            try
                                            {
                                                stream.Disconnect();
                                            }
                                            catch (Exception e)
                                            {
                                                if (localTrace)
                                                {
                                                    TraceOps.DebugTrace(
                                                        e, typeof(ScriptServer).Name,
                                                        TracePriority.NetworkError);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "server already started";
                        return ReturnCode.Error;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref startCount);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public virtual ReturnCode Stop(
                bool force,
                ref Result error
                )
            {
                CheckDisposed();

                Interpreter localInterpreter;
                string localPipeName;
                bool localTrace;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localInterpreter = interpreter;
                    localPipeName = pipeName;
                    localTrace = trace;
                }

                if (Interlocked.Increment(ref stopCount) > 0)
                {
                    if (force &&
                        (Interlocked.CompareExchange(ref startCount, 0, 0) > 0))
                    {
                        using (ScriptClient scriptClient = new ScriptClient(
                                localInterpreter, ScriptClient.LocalHost,
                                localPipeName, localTrace))
                        {
                            int timeout = ThreadOps.GetTimeout(
                                localInterpreter, null, TimeoutType.Join);

                            return scriptClient.Send(null, timeout, ref error);
                        }
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    error = "unable to make stop counter positive";
                    return ReturnCode.Error;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void PreInterpreterDisposed(
                object @object
                ) /* Eagle._Components.Public.Delegates.DisposeCallback */
            {
                Interpreter localInterpreter = @object as Interpreter;

                try
                {
                    ReturnCode stopCode;
                    Result stopError = null;

                    stopCode = Stop(true, ref stopError);

                    if (stopCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            localInterpreter, stopCode, stopError);
                    }
                }
                catch (Exception e)
                {
                    DebugOps.Complain(
                        localInterpreter, ReturnCode.Error, e);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void SetupAutoStop()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (interpreter == null)
                        return;

                    if (preDisposeCallback != null)
                        return;

                    DisposeCallback localPreDisposeCallback = new DisposeCallback(
                        PreInterpreterDisposed);

                    interpreter.PreInterpreterDisposed += localPreDisposeCallback;
                    preDisposeCallback = localPreDisposeCallback;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptServer).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(
                bool disposing
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (trace)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Dispose: called, disposing = {0}, disposed = {1}",
                            disposing, disposed), typeof(ScriptServer).Name,
                            TracePriority.CleanupDebug);
                    }

                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            interpreter = null; /* NOT OWNED */
                            pipeName = null;
                            autoStop = false;
                            trace = false;
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////

                        disposed = true;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptServer()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Win32Window Test Class
#if NATIVE && WINDOWS && WINFORMS
        [ObjectId("e0c7aa41-b73b-44b4-afcc-4b91dde0bfff")]
        public class Win32Window : IWin32Window, IDisposable
        {
            #region Public Constructors
            public Win32Window()
            {
                handle = IntPtr.Zero;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Win32Window(
                IntPtr handle
                )
                : this()
            {
                this.handle = handle;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IWin32Window Members
            private IntPtr handle;
            public virtual IntPtr Handle
            {
                get { CheckDisposed(); return handle; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(Win32Window).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        handle = IntPtr.Zero; /* NOT OWNED */
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~Win32Window()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region TestStreamHost Test Class
        [ObjectId("599f24b4-d297-4e3d-a7a9-ca9d32d0ffd3")]
        public class TestStreamHost : _Hosts.Fake
        {
            #region Private Data
            private Interpreter interpreter;
            private RefreshStreamsCallback callback;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public TestStreamHost(
                IHostData hostData,
                RefreshStreamsCallback callback
                )
                : base(hostData)
            {
                if (hostData != null)
                {
                    //
                    // NOTE: Keep track of the interpreter that we are
                    //       provided, if any.
                    //
                    interpreter = hostData.Interpreter;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                this.callback = callback;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void RefreshStreams(
                ChannelType channelType,
                bool force
                )
            {
                ReturnCode refreshCode;
                Result refreshError = null;

                try
                {
                    refreshCode = callback(
                        interpreter, channelType, force, false,
                        ref input, ref output, ref error,
                        ref refreshError); /* throw */
                }
                catch (Exception e)
                {
                    refreshError = e;
                    refreshCode = ReturnCode.Error;
                }

                if (refreshCode != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "RefreshStreams: error = {0}",
                        FormatOps.WrapOrNull(refreshError)),
                        typeof(TestStreamHost).Name,
                        TracePriority.IoError);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IInteractiveHost Members
            public override bool IsInputRedirected()
            {
                CheckDisposed();

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsOpen()
            {
                CheckDisposed();

                return (input != null) || (output != null) || (error != null);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override HostFlags GetHostFlags()
            {
                CheckDisposed();

                return HostFlags.Stream;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IStreamHost Members
            public override Stream DefaultIn
            {
                get
                {
                    CheckDisposed();

                    RefreshStreams(ChannelType.Input, false);
                    return input;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override Stream DefaultOut
            {
                get
                {
                    CheckDisposed();

                    RefreshStreams(ChannelType.Output, false);
                    return output;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override Stream DefaultError
            {
                get
                {
                    CheckDisposed();

                    RefreshStreams(ChannelType.Error, false);
                    return error;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Stream input;
            public override Stream In
            {
                get { CheckDisposed(); return input; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Stream output;
            public override Stream Out
            {
                get { CheckDisposed(); return output; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Stream error;
            public override Stream Error
            {
                get { CheckDisposed(); return error; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override Encoding InputEncoding
            {
                get { CheckDisposed(); return Encoding.Unicode; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override Encoding OutputEncoding
            {
                get { CheckDisposed(); return Encoding.Unicode; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override Encoding ErrorEncoding
            {
                get { CheckDisposed(); return Encoding.Unicode; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool ResetIn()
            {
                CheckDisposed();

                RefreshStreams(ChannelType.Input, true);
                return (input != null);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool ResetOut()
            {
                CheckDisposed();

                RefreshStreams(ChannelType.Output, true);
                return (output != null);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool ResetError()
            {
                CheckDisposed();

                RefreshStreams(ChannelType.Error, true);
                return (error != null);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsOutputRedirected()
            {
                CheckDisposed();

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsErrorRedirected()
            {
                CheckDisposed();

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool SetupChannels()
            {
                CheckDisposed();

                return false; /* NOT IMPLEMENTED */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                    throw new InterpreterDisposedException(typeof(TestStreamHost));
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

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

                        interpreter = null; /* NOT OWNED, DO NOT DISPOSE. */
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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region TextBoxStream Test Class
#if WINFORMS
        [ObjectId("81f27390-0ffa-4cd8-9e38-35fff48a623e")]
        public class TextBoxStream : Stream
        {
            #region Private Data
            private TextBox textBox;
            private bool? wasSelected;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public TextBoxStream(TextBox textBox, bool canRead, bool canWrite)
            {
                this.textBox = textBox;
                this.canRead = canRead;
                this.canWrite = canWrite;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private string GetText(out Result error)
            {
                error = null;

                if (textBox == null)
                {
                    error = "invalid text box";
                    return null;
                }

                string text;
                bool selected;

                text = FormOps.GetText(textBox, out selected);

                if (text == null)
                {
                    error = "could not get text";
                    return null;
                }

                wasSelected = selected;
                return text;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool SetText(string text, out Result error)
            {
                error = null;

                if (textBox == null)
                {
                    error = "invalid text box";
                    return false;
                }

                if (wasSelected == null)
                {
                    error = "selection flag not set";
                    return false;
                }

                if (!FormOps.SetText(textBox, text, (bool)wasSelected, false))
                {
                    error = "could not set text";
                    return false;
                }

                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Stream Members
            private bool canRead;
            public override bool CanRead
            {
                get { CheckDisposed(); return canRead; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool CanSeek
            {
                get { CheckDisposed(); return canRead; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool canWrite;
            public override bool CanWrite
            {
                get { CheckDisposed(); return canWrite; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                if (!canWrite)
                    throw new IOException("stream is read-only");

                if (textBox == null)
                    throw new IOException("invalid text box");

                if (wasSelected == null)
                    throw new IOException("selection flag not set");
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override long Length
            {
                get
                {
                    CheckDisposed();

                    if (!canRead)
                        throw new NotSupportedException();

                    string text;
                    Result error;

                    text = GetText(out error);

                    if (text == null)
                        throw new InvalidOperationException(error);

                    return text.Length;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int position = _Position.Invalid;
            public override long Position
            {
                get
                {
                    CheckDisposed();

                    if (!canRead)
                        throw new NotSupportedException();

                    return position;
                }
                set
                {
                    CheckDisposed();

                    if (!canRead)
                        throw new NotSupportedException();

                    if ((value < int.MinValue) || (value > int.MaxValue))
                        throw new IOException();

                    position = (int)value;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override int Read(byte[] buffer, int offset, int count)
            {
                CheckDisposed();

                if (!canRead)
                    throw new NotSupportedException();

                if (buffer == null)
                    throw new ArgumentNullException();

                if ((offset < 0) || (count < 0))
                    throw new ArgumentOutOfRangeException();

                int length = buffer.Length;

                if ((offset + count) > length)
                    throw new ArgumentException();

                string text;
                Result error;

                text = GetText(out error);

                if (text == null)
                    throw new InvalidOperationException(error);

                if (position < 0)
                    position = 0;

                int index;

                for (index = offset; count > 0; index++, count--)
                {
                    if (position >= text.Length)
                        break;

                    buffer[index] = (byte)(text[position++] & (char)byte.MaxValue);
                }

                return (index - offset);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override long Seek(long offset, SeekOrigin origin)
            {
                CheckDisposed();

                if (!canRead)
                    throw new NotSupportedException();

                string text;
                Result error;

                text = GetText(out error);

                if (text == null)
                    throw new InvalidOperationException(error);

                //
                // HACK: Only allow seeking to the start of the stream.
                //
                if ((offset != 0) || (origin != SeekOrigin.Begin))
                    throw new IOException();

                position = _Position.Invalid;

                return position;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void SetLength(long value)
            {
                CheckDisposed();

                throw new NotSupportedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Write(byte[] buffer, int offset, int count)
            {
                CheckDisposed();

                if (!canWrite)
                    throw new NotSupportedException();

                if (buffer == null)
                    throw new ArgumentNullException();

                if ((offset < 0) || (count < 0))
                    throw new ArgumentOutOfRangeException();

                int length = buffer.Length;

                if ((offset + count) > length)
                    throw new ArgumentException();

                string text;
                Result error;

                text = GetText(out error);

                if (text == null)
                    throw new InvalidOperationException(error);

                StringBuilder builder = StringOps.NewStringBuilder(text);

                for (int index = offset; count > 0; index++, count--)
                    builder.Append((char)buffer[index]);

                if (!SetText(builder.ToString(), out error))
                    throw new IOException(error);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(TextBoxStream).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            textBox = null; /* NOT OWNED */
                        }

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

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~TextBoxStream()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptComplain Test Class
        [ObjectId("6b919457-a9c8-4008-bd97-a2986f57d857")]
        public class ScriptComplain : IDisposable
        {
            #region Private Data
            private int complainLevels = 0;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Internal Constructors
            internal ScriptComplain(
                string text,
                EngineFlags engineFlags
                )
            {
                this.text = text;
                this.engineFlags = engineFlags;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private string text;
            public virtual string Text
            {
                get { CheckDisposed(); return text; }
                set { CheckDisposed(); text = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private EngineFlags engineFlags;
            public virtual EngineFlags EngineFlags
            {
                get { CheckDisposed(); return engineFlags; }
                set { CheckDisposed(); engineFlags = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public virtual void Complain(
                Interpreter interpreter,
                long id,
                ReturnCode code,
                Result result,
                string stackTrace,
                bool quiet,
                int retry,
                int levels
                )
            {
                CheckDisposed();

                int localLevels = Interlocked.Increment(ref complainLevels);

                try
                {
                    ReturnCode localCode;
                    Result localResult = null;

                    if (interpreter != null)
                    {
                        if (localLevels == 1)
                        {
                            EngineFlags engineFlags = this.EngineFlags;
                            ObjectDictionary objects = new ObjectDictionary();

                            objects.Add("engineFlags", engineFlags);
                            objects.Add("methodName", "Complain");
                            objects.Add("interpreter", interpreter);
                            objects.Add("id", id);
                            objects.Add("code", code);
                            objects.Add("result", result);
                            objects.Add("stackTrace", stackTrace);
                            objects.Add("quiet", quiet);
                            objects.Add("retry", retry);
                            objects.Add("levels", levels);

                            EngineFlags savedEngineFlags = interpreter.ContextEngineFlags;
                            interpreter.ContextEngineFlags |= engineFlags;

                            try
                            {
                                localCode = Helpers.EvaluateScript(
                                    interpreter, this.Text, objects,
                                    ref localResult);
                            }
                            finally
                            {
                                interpreter.ContextEngineFlags = savedEngineFlags;
                                savedEngineFlags = EngineFlags.None;
                            }
                        }
                        else
                        {
                            localResult = "cannot handle complaint, already pending";
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = "invalid interpreter";
                        localCode = ReturnCode.Error;
                    }

                    if (localCode != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "complain script failed: {0}",
                            ResultOps.Format(localCode, localResult)),
                            typeof(ScriptComplain).Name,
                            TracePriority.ComplainError);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref complainLevels);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptComplain).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(
                bool disposing
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptComplain).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        text = null;
                        engineFlags = EngineFlags.None;
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptComplain()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptNotifyPlugin Test Class
#if NOTIFY || NOTIFY_OBJECT
        [PluginFlags(
            PluginFlags.System | PluginFlags.Notify |
            PluginFlags.Static | PluginFlags.NoCommands |
            PluginFlags.NoFunctions | PluginFlags.NoPolicies |
            PluginFlags.NoTraces | PluginFlags.Test)]
        [ObjectId("0de67637-1915-4560-ac45-e512d09314cf")]
        public class ScriptNotifyPlugin : _Plugins.Notify, IDisposable
        {
            #region Private Data
            private static int notifyLevels = 0;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Internal Constructors
            internal ScriptNotifyPlugin(
                IPluginData pluginData,
                string text,
                NotifyType notifyTypes,
                NotifyFlags notifyFlags,
                EngineFlags engineFlags
                )
                : base(pluginData)
            {
                this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                    AttributeOps.GetPluginFlags(this);

                ///////////////////////////////////////////////////////////////////////////////////////

                this.text = text;
                this.notifyTypes = notifyTypes;
                this.notifyFlags = notifyFlags;
                this.engineFlags = engineFlags;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            //
            // NOTE: This is the script to evaluate in response to the methods
            //       overridden methods from the base class (_Plugins.Notify).
            //
            private string text;
            public virtual string Text
            {
                get { CheckDisposed(); return text; }
                set { CheckDisposed(); text = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: These are the notification types that will be handled by
            //       this plugin.
            //
            private NotifyType notifyTypes;
            public virtual NotifyType NotifyTypes
            {
                get { CheckDisposed(); return notifyTypes; }
                set { CheckDisposed(); notifyTypes = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: These are the notification flags that will be handled by
            //       this plugin.
            //
            private NotifyFlags notifyFlags;
            public virtual NotifyFlags NotifyFlags
            {
                get { CheckDisposed(); return notifyFlags; }
                set { CheckDisposed(); notifyFlags = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: These are the extra per-thread engine flags used while
            //       evaluating the configured notification script.
            //
            private EngineFlags engineFlags;
            public virtual EngineFlags EngineFlags
            {
                get { CheckDisposed(); return engineFlags; }
                set { CheckDisposed(); engineFlags = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            protected override PackageFlags GetPackageFlags()
            {
                //
                // NOTE: We know the package is primarily a core package
                //       because this is the core library.  Of course, a
                //       derived plugin can override this method and can
                //       alter this value.
                //
                return PackageFlags.Core | base.GetPackageFlags();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void ResetToken(
                Interpreter interpreter
                )
            {
                //
                // HACK: Cleanup the script plugin token in the interpreter
                //       state because this is the only place where we can
                //       be 100% sure it will get done.
                //
                if (interpreter == null)
                    return;

                interpreter.InternalScriptNotifyPluginToken = 0;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IState Members
            public override ReturnCode Initialize(
                Interpreter interpreter,
                IClientData clientData,
                ref Result result
                )
            {
                CheckDisposed();

                return base.Initialize(interpreter, clientData, ref result);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override ReturnCode Terminate(
                Interpreter interpreter,
                IClientData clientData,
                ref Result result
                )
            {
                CheckDisposed();

                ResetToken(interpreter);

                return base.Terminate(interpreter, clientData, ref result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region INotify Members
            public override NotifyType GetTypes(
                Interpreter interpreter
                )
            {
                CheckDisposed();

                return notifyTypes | base.GetTypes(interpreter);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override NotifyFlags GetFlags(
                Interpreter interpreter
                )
            {
                CheckDisposed();

                return notifyFlags | base.GetFlags(interpreter);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override ReturnCode Notify(
                Interpreter interpreter,
                IScriptEventArgs eventArgs,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                CheckDisposed();

                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                int levels = Interlocked.Increment(ref notifyLevels);

                try
                {
                    if (levels == 1)
                    {
                        EngineFlags engineFlags = this.EngineFlags;
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("notifyTypes", this.NotifyTypes);
                        objects.Add("notifyFlags", this.NotifyFlags);
                        objects.Add("engineFlags", engineFlags);
                        objects.Add("methodName", "Notify");
                        objects.Add("interpreter", interpreter);
                        objects.Add("eventArgs", eventArgs);
                        objects.Add("clientData", clientData);
                        objects.Add("arguments", arguments);

                        EngineFlags savedEngineFlags = interpreter.ContextEngineFlags;
                        interpreter.ContextEngineFlags |= engineFlags;

                        try
                        {
                            ReturnCode code;
                            Result localResult = null;

                            code = Helpers.EvaluateScript(interpreter,
                                this.Text, objects, ref localResult);

                            if (code != ReturnCode.Ok)
                                result = localResult;

                            return code;
                        }
                        finally
                        {
                            interpreter.ContextEngineFlags = savedEngineFlags;
                            savedEngineFlags = EngineFlags.None;
                        }
                    }
                    else
                    {
                        result = "cannot handle notification, already pending";
                        return ReturnCode.Error;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref notifyLevels);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptNotifyPlugin).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(
                bool disposing
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptNotifyPlugin).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        text = null;
                        notifyTypes = NotifyType.None;
                        notifyFlags = NotifyFlags.None;
                        engineFlags = EngineFlags.None;
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptNotifyPlugin()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptGetStringPlugin Test Class
        [ObjectId("818b9b5d-1c96-4e33-b450-cc4fa4fdcbcf")]
        [PluginFlags(
            PluginFlags.System | PluginFlags.Static |
            PluginFlags.NoCommands | PluginFlags.NoFunctions |
            PluginFlags.NoPolicies | PluginFlags.NoTraces |
            PluginFlags.Test)]
        public class ScriptGetStringPlugin : _Plugins.Default, IDisposable
        {
            #region Private Data
            private static int getStringLevels = 0;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Internal Constructors
            internal ScriptGetStringPlugin(
                IPluginData pluginData, /* in */
                string text,            /* in */
                EngineFlags engineFlags /* in */
                )
                : base(pluginData)
            {
                this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                    AttributeOps.GetPluginFlags(this);

                ///////////////////////////////////////////////////////////////////////////////////////

                this.text = text;
                this.engineFlags = engineFlags;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            //
            // NOTE: This is the script to evaluate in response to the methods
            //       overridden methods from the base class (_Plugins.Default).
            //
            private string text;
            public virtual string Text
            {
                get { CheckDisposed(); return text; }
                set { CheckDisposed(); text = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: These are the extra per-thread engine flags used while
            //       evaluating the configured notification script.
            //
            private EngineFlags engineFlags;
            public virtual EngineFlags EngineFlags
            {
                get { CheckDisposed(); return engineFlags; }
                set { CheckDisposed(); engineFlags = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IPlugin Members
            public override string GetString(
                Interpreter interpreter, /* in */
                string name,             /* in */
                CultureInfo cultureInfo, /* in */
                ref Result error         /* out */
                )
            {
                CheckDisposed();

                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return null;
                }

                int levels = Interlocked.Increment(ref getStringLevels);

                try
                {
                    if (levels == 1)
                    {
                        EngineFlags engineFlags = this.EngineFlags;
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("engineFlags", engineFlags);
                        objects.Add("methodName", "GetString");
                        objects.Add("interpreter", interpreter);
                        objects.Add("name", name);
                        objects.Add("cultureInfo", cultureInfo);

                        EngineFlags savedEngineFlags = interpreter.ContextEngineFlags;
                        interpreter.ContextEngineFlags |= engineFlags;

                        try
                        {
                            Result localResult = null;

                            if (ResultOps.IsOkOrReturn(Helpers.EvaluateScript(
                                    interpreter, this.Text, objects, ref localResult)))
                            {
                                return localResult;
                            }
                            else
                            {
                                error = localResult;
                                return null;
                            }
                        }
                        finally
                        {
                            interpreter.ContextEngineFlags = savedEngineFlags;
                            savedEngineFlags = EngineFlags.None;
                        }
                    }
                    else
                    {
                        error = "cannot get string, already pending";
                        return null;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref getStringLevels);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptGetStringPlugin).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(
                bool disposing /* in */
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptGetStringPlugin).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        text = null;
                        engineFlags = EngineFlags.None;
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptGetStringPlugin()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptWebClient Test Class
#if NETWORK
        [ObjectId("a772f475-0016-4016-a444-310b1be9ba58")]
        public class ScriptWebClient : WebClient, IHaveInterpreter /* NOT SEALED */
        {
            #region Private Constructors
            private ScriptWebClient(
                Interpreter interpreter,
                string text,
                string argument
                )
            {
                this.interpreter = interpreter;
                this.text = text;
                this.argument = argument;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static WebClient Create(
                Interpreter interpreter,
                string text,
                string argument,
                ref Result error /* NOT USED */
                )
            {
                return new ScriptWebClient(interpreter, text, argument);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            //
            // NOTE: This is the script to evaluate in response to the methods
            //       overridden methods from the base class (WebClient).
            //
            private string text;
            public virtual string Text
            {
                get { CheckDisposed(); return text; }
                set { CheckDisposed(); text = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the string argument (e.g. method name) passed to
            //       the web client creation callback that was responsible for
            //       creating this web client instance.
            //
            private string argument;
            public virtual string Argument
            {
                get { CheckDisposed(); return argument; }
                set { CheckDisposed(); argument = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter / ISetInterpreter Members
            //
            // NOTE: This is the interpreter context that the script will be
            //       evaluated in.
            //
            private Interpreter interpreter;
            public virtual Interpreter Interpreter
            {
                get { CheckDisposed(); return interpreter; }
                set { CheckDisposed(); interpreter = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            protected virtual void Complain(
                Interpreter interpreter,
                ReturnCode code,
                Result result
                )
            {
                DebugOps.Complain(interpreter, code, result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Net.WebClient Overrides
            protected override WebRequest GetWebRequest(
                Uri address
                )
            {
                WebRequest webRequest = base.GetWebRequest(address);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("argument", this.Argument);
                objects.Add("methodName", "GetWebRequest");
                objects.Add("address", address);
                objects.Add("webRequest", webRequest);

                ReturnCode localCode;
                Result localResult = null;

                localCode = Helpers.EvaluateScript(
                    this.Interpreter, this.Text, objects,
                    ref localResult);

                if (localCode != ReturnCode.Ok)
                    Complain(this.Interpreter, localCode, localResult);

                return webRequest;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override WebResponse GetWebResponse(
                WebRequest request
                )
            {
                WebResponse webResponse = base.GetWebResponse(request);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("argument", this.Argument);
                objects.Add("methodName", "GetWebResponse");
                objects.Add("webRequest", request);
                objects.Add("webResponse", webResponse);

                ReturnCode localCode;
                Result localResult = null;

                localCode = Helpers.EvaluateScript(
                    this.Interpreter, this.Text, objects,
                    ref localResult);

                if (localCode != ReturnCode.Ok)
                    Complain(this.Interpreter, localCode, localResult);

                return webResponse;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override WebResponse GetWebResponse(
                WebRequest request,
                IAsyncResult result
                )
            {
                WebResponse webResponse = base.GetWebResponse(request);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("argument", this.Argument);
                objects.Add("methodName", "GetWebResponse");
                objects.Add("webRequest", request);
                objects.Add("asyncResult", result);
                objects.Add("webResponse", webResponse);

                ReturnCode localCode;
                Result localResult = null;

                localCode = Helpers.EvaluateScript(
                    this.Interpreter, this.Text, objects,
                    ref localResult);

                if (localCode != ReturnCode.Ok)
                    Complain(this.Interpreter, localCode, localResult);

                return webResponse;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptWebClient).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            interpreter = null; /* NOT OWNED */
                            text = null;
                            argument = null;
                        }

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

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptWebClient()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptLimiter Test Class
        [ObjectId("79fcf88b-9b2c-4673-a055-95705af31216")]
        public sealed class ScriptLimiter : IDisposable
        {
            #region Private Constants
            private static readonly int Unlimited = Count.Invalid;
            private readonly object syncRoot = new object();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private ScriptLimiter()
            {
                Reset();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ScriptLimiter(
                ReadyCallback savedReadyCallback,
                bool disabled,
                long commandLimit,
                long operationLimit,
                TimeSpan? timeLimit,
                DateTime? started,
                bool autoReset
                )
                : this()
            {
                this.savedReadyCallback = savedReadyCallback;
                this.disabled = disabled;
                this.commandLimit = commandLimit;
                this.operationLimit = operationLimit;
                this.timeLimit = timeLimit;
                this.started = started;
                this.autoReset = autoReset;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static ScriptLimiter Create()
            {
                return new ScriptLimiter(
                    null, false, Unlimited, Unlimited, null, null, false);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private ReadyCallback savedReadyCallback;
            public ReadyCallback SavedReadyCallback
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return savedReadyCallback;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
                set
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            savedReadyCallback = value;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool disabled;
            public bool Disabled
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return disabled;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
                set
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            disabled = value;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private long commandLimit;
            public long CommandLimit
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return commandLimit;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
                set
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            commandLimit = value;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private long operationLimit;
            public long OperationLimit
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return operationLimit;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
                set
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            operationLimit = value;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private TimeSpan? timeLimit;
            public TimeSpan? TimeLimit
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return timeLimit;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
                set
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            timeLimit = value;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private DateTime? started;
            public DateTime? Started
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return started;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
                set
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            started = value;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private DateTime? stopped;
            public DateTime? Stopped
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return stopped;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool autoReset;
            public bool AutoReset
            {
                get
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            return autoReset;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
                set
                {
                    CheckDisposed();

                    bool locked = false;

                    try
                    {
                        PrivateTryLock(ref locked);

                        if (locked)
                        {
                            autoReset = value;
                        }
                        else
                        {
                            throw new ScriptException(
                                "unable to acquire lock");
                        }
                    }
                    finally
                    {
                        PrivateExitLock(ref locked);
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode Install(
                Interpreter interpreter,
                bool waitForLock,
                ref Result error
                )
            {
                CheckDisposed();

                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                bool locked1 = false;

                try
                {
                    if (waitForLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked1); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalSoftTryLock(
                            ref locked1); /* TRANSACTIONAL */
                    }

                    if (locked1)
                    {
                        bool locked2 = false;

                        try
                        {
                            PrivateTryLock(ref locked2); /* TRANSACTIONAL */

                            if (locked2)
                            {
                                savedReadyCallback = interpreter.InternalReadyCallback;
                                interpreter.InternalReadyCallback = ReadyCallback;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "unable to acquire lock";
                                return ReturnCode.Error;
                            }
                        }
                        finally
                        {
                            PrivateExitLock(ref locked2); /* TRANSACTIONAL */
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "unable to acquire interpreter {0} lock",
                            FormatOps.InterpreterNoThrow(interpreter));

                        return ReturnCode.Error;
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked1); /* TRANSACTIONAL */
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode Uninstall(
                Interpreter interpreter,
                bool waitForLock,
                ref Result error
                )
            {
                CheckDisposed();

                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                bool locked1 = false;

                try
                {
                    if (waitForLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked1); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalSoftTryLock(
                            ref locked1); /* TRANSACTIONAL */
                    }

                    if (locked1)
                    {
                        bool locked2 = false;

                        try
                        {
                            PrivateTryLock(ref locked2); /* TRANSACTIONAL */

                            if (locked2)
                            {
                                interpreter.InternalReadyCallback = savedReadyCallback;
                                savedReadyCallback = null;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "unable to acquire lock";
                                return ReturnCode.Error;
                            }
                        }
                        finally
                        {
                            PrivateExitLock(ref locked2); /* TRANSACTIONAL */
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "unable to acquire interpreter {0} lock",
                            FormatOps.InterpreterNoThrow(interpreter));

                        return ReturnCode.Error;
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked1); /* TRANSACTIONAL */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void Reset()
            {
                savedReadyCallback = null;
                disabled = false;
                commandLimit = Unlimited;
                operationLimit = Unlimited;
                timeLimit = null;
                started = null;
                stopped = null;
                autoReset = false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateTryLock(
                ref bool locked
                )
            {
                if (syncRoot == null)
                    return;

                locked = Monitor.TryEnter(syncRoot);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateExitLock(
                ref bool locked
                )
            {
                if (syncRoot == null)
                    return;

                if (locked)
                {
                    Monitor.Exit(syncRoot);
                    locked = false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Eagle._Components.Public.Delegates.ReadyCallback
            private ReturnCode ReadyCallback(
                Interpreter interpreter,
                IClientData clientData,
                int timeout,
                ReadyFlags flags,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                bool locked = false;

                try
                {
                    PrivateTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        //
                        // NOTE: If readiness checking for this instance is
                        //       disabled, skip all of our readiness checks;
                        //       however, the saved readiness callback will
                        //       still be called, if it exists (and it does
                        //       not refer back to this instance).
                        //
                        if (disabled)
                            goto skip;

                        //
                        // NOTE: First, check the command count against our
                        //       limit, if it is set.  Upon exceeding this
                        //       limit, process the auto-reset flag and then
                        //       return an error.  This is a fairly accurate
                        //       count and should be favored over operation
                        //       count in almost all cases.
                        //
                        if (commandLimit != Unlimited)
                        {
                            long commandCount = interpreter.CommandCountNoLock;

                            if (commandCount >= commandLimit)
                            {
                                if (autoReset)
                                    interpreter.CommandCountNoLock = 0;

                                error = String.Format(
                                    "command limit exceeded: {0} versus {1}",
                                    commandCount, commandLimit);

                                return ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Next, check the operation count against our
                        //       limit, if it is set.  Upon exceeding this
                        //       limit, process the auto-reset flag and then
                        //       return an error.  The operation count is not
                        //       quite as accurate (or useful) as the command
                        //       count and it should not normally be used.
                        //
                        if (operationLimit != Unlimited)
                        {
                            long operationCount = interpreter.OperationCountNoLock;

                            if (operationCount >= operationLimit)
                            {
                                if (autoReset)
                                    interpreter.OperationCountNoLock = 0;

                                error = String.Format(
                                    "operation limit exceeded: {0} versus {1}",
                                    operationCount, operationLimit);

                                return ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Next, check the time limit against our time
                        //       limit and start date, if they are set.  Upon
                        //       exceeding this limit, process the auto-reset
                        //       flag and then return an error.  This differs
                        //       significantly from the other two limit checks
                        //       because it is entirely synthetic (i.e. the
                        //       interpreter itself does not track the start
                        //       date, we do).  This relies upon this method
                        //       being called often and at regular intervals.
                        //       When the auto-reset flag is processed, the
                        //       start date is simply reset to "now".  When
                        //       the limit is exceeded, the stopped date is
                        //       set to "now", for accounting purposes.
                        //
                        if (timeLimit != null)
                        {
                            DateTime now = TimeOps.GetUtcNow();

                            if (started != null)
                            {
                                TimeSpan elapsed = now.Subtract(
                                    (DateTime)started);

                                if (elapsed > timeLimit)
                                {
                                    if (autoReset)
                                        started = now;

                                    stopped = now;

                                    error = String.Format(
                                        "time limit exceeded: {0} versus {1}",
                                        elapsed, timeLimit);

                                    return ReturnCode.Error;
                                }
                            }
                            else
                            {
                                started = now;
                            }

                            stopped = null;
                        }

                    skip:

                        //
                        // NOTE: If there is a saved readiness checking
                        //       callback (and it does not refer to this
                        //       instanced callback), attempt to call it
                        //       now.  This allows readiness checking to
                        //       be chained.
                        //
                        if ((savedReadyCallback != null) &&
                            (savedReadyCallback != ReadyCallback))
                        {
                            ReturnCode savedCode;
                            Result savedError = null;

                            savedCode = savedReadyCallback(
                                interpreter, clientData, timeout, flags,
                                ref savedError);

                            if (savedCode != ReturnCode.Ok)
                            {
                                error = savedError;
                                return savedCode;
                            }
                        }

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "unable to acquire lock";
                        return ReturnCode.Error;
                    }
                }
                finally
                {
                    PrivateExitLock(ref locked); /* TRANSACTIONAL */
                }
            }
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptLimiter).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptLimiter).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        Reset();
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptLimiter()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptEventState Test Class
        [ObjectId("aea45f5e-1a29-48eb-ab8f-66bb56cbf32f")]
        public sealed class ScriptEventState : IGetInterpreter, IDisposable
        {
            #region Private Constants
            private static int MinimumSleepTime = EventManager.MinimumSleepTime;
            private static bool UseThreadPoolForTimeout = true;
            private static bool NoStaThread = false;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private static readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string text;
            private ReturnCode returnCode;
            private Result result;
            private int errorLine;
            private Result errorCode;
            private Result errorInfo;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private EventWaitHandle doneEvent;   /* index = 0 */
            private EventWaitHandle scriptEvent; /* index = 1 */
            private EventWaitHandle resultEvent; /* index = 2 */
            private EventWaitHandle exitEvent;   /* index = 3 */

            ///////////////////////////////////////////////////////////////////////////////////////////

            private EventWaitHandle[] waitHandles;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private ScriptEventState()
            {
                InitializeEvents();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ScriptEventState(
                Interpreter interpreter,
                string text,
                int? timeout,
                bool useTimeout
                )
                : this()
            {
                PrepareForEvent(interpreter, text, timeout, useTimeout);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static ScriptEventState Create(
                Interpreter interpreter,
                string text,
                int? timeout,
                bool useTimeout
                )
            {
                return new ScriptEventState(
                    interpreter, text, timeout, useTimeout);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void InitializeEvents()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (doneEvent == null)
                        doneEvent = ThreadOps.CreateEvent(false);

                    if (scriptEvent == null)
                        scriptEvent = ThreadOps.CreateEvent(false);

                    if (resultEvent == null)
                        resultEvent = ThreadOps.CreateEvent(false);

                    if (exitEvent == null)
                        exitEvent = ThreadOps.CreateEvent(false);

                    if (waitHandles == null)
                    {
                        //
                        // NOTE: The result event does not actually go into
                        //       this array, since the script event thread
                        //       never waits on it.
                        //
                        waitHandles = new EventWaitHandle[] {
                            doneEvent, scriptEvent
                        };
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateClearResult()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    returnCode = ReturnCode.Ok;
                    result = null;
                    errorLine = 0;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateSetResult(
                ReturnCode returnCode,
                Result result,
                int errorLine
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    this.returnCode = returnCode;
                    this.result = result;
                    this.errorLine = errorLine;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateSetResult(
                ReturnCode returnCode,
                Result result,
                int errorLine,
                bool errorInfo
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    PrivateSetResult(returnCode, result, errorLine);

                    if (errorInfo)
                    {
                        /* IGNORED */
                        PrivateCheckPopulateErrorInformation();
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateClearErrorInformation()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    errorCode = null;
                    errorInfo = null;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool PrivateCheckPopulateErrorInformation()
            {
                Interpreter localInterpreter;
                ReturnCode localReturnCode;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localInterpreter = interpreter;
                    localReturnCode = returnCode;
                }

                Result localErrorCode = null;
                Result localErrorInfo = null;

                bool result = TestCheckCopyErrorInformation(
                    localInterpreter, localReturnCode, true,
                    ref localErrorCode, ref localErrorInfo);

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    errorCode = localErrorCode;
                    errorInfo = localErrorInfo;
                }

                return result;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateUnSignalScript()
            {
                EventWaitHandle localScriptEvent;

                lock (syncRoot)
                {
                    localScriptEvent = scriptEvent;
                }

                ThreadOps.ResetEvent(localScriptEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateUnSignalResult()
            {
                EventWaitHandle localResultEvent;

                lock (syncRoot)
                {
                    localResultEvent = resultEvent;
                }

                ThreadOps.ResetEvent(localResultEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateSignalDone()
            {
                EventWaitHandle localDoneEvent;

                lock (syncRoot)
                {
                    localDoneEvent = doneEvent;
                }

                ThreadOps.SetEvent(localDoneEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateSignalScript()
            {
                EventWaitHandle localScriptEvent;

                lock (syncRoot)
                {
                    localScriptEvent = scriptEvent;
                }

                ThreadOps.SetEvent(localScriptEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateSignalResult()
            {
                EventWaitHandle localResultEvent;

                lock (syncRoot)
                {
                    localResultEvent = resultEvent;
                }

                ThreadOps.SetEvent(localResultEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void PrivateSignalExit()
            {
                EventWaitHandle localExitEvent;

                lock (syncRoot)
                {
                    localExitEvent = exitEvent;
                }

                ThreadOps.SetEvent(localExitEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void ClearAndSignalResult()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    PrivateClearResult();
                    PrivateClearErrorInformation();
                    PrivateSignalResult();
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool WaitExitForDispose()
            {
                return WaitExit(
                    ThreadOps.GetDefaultTimeout(TimeoutType.Dispose));
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter Members
            private Interpreter interpreter;
            public Interpreter Interpreter
            {
                get { CheckDisposed(); lock (syncRoot) { return interpreter; } }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private int? timeout;
            public int? Timeout
            {
                get { CheckDisposed(); lock (syncRoot) { return timeout; } }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool useTimeout;
            public bool UseTimeout
            {
                get { CheckDisposed(); lock (syncRoot) { return useTimeout; } }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public void PrepareForEvent(
                Interpreter interpreter,
                string text,
                int? timeout,
                bool useTimeout
                )
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    this.interpreter = interpreter;
                    this.text = text;
                    this.timeout = timeout;
                    this.useTimeout = useTimeout;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool ResetCancel()
            {
                CheckDisposed();

                Interpreter interpreter;

                lock (syncRoot)
                {
                    interpreter = this.interpreter;
                }

                return (Engine.ResetCancel(interpreter,
                    CancelFlags.ScriptEvent) == ReturnCode.Ok);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public int GetMinimumSleepTime()
            {
                CheckDisposed();

                Interpreter localInterpreter;

                lock (syncRoot)
                {
                    localInterpreter = interpreter;
                }

                if (localInterpreter == null)
                    return MinimumSleepTime;

                IEventManager eventManager;

                lock (localInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (localInterpreter.Disposed)
                        return MinimumSleepTime;

                    eventManager = localInterpreter.EventManager;

                    if (eventManager == null)
                        return MinimumSleepTime;
                }

                lock (eventManager.SyncRoot) /* TRANSACTIONAL */
                {
                    if (!EventOps.ManagerIsOk(eventManager))
                        return MinimumSleepTime;

                    return eventManager.GetMinimumSleepTime(
                        SleepType.Script);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public int WaitScriptOrDone()
            {
                CheckDisposed();

                return WaitScriptOrDone(GetMinimumSleepTime());
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public int WaitScriptOrDone(
                int timeout
                )
            {
                CheckDisposed();

                EventWaitHandle[] localWaitHandles;

                lock (syncRoot)
                {
                    localWaitHandles = waitHandles;
                }

                return ThreadOps.WaitAnyEvent(localWaitHandles, timeout);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool WaitResult()
            {
                CheckDisposed();

                EventWaitHandle localResultEvent;

                lock (syncRoot)
                {
                    localResultEvent = resultEvent;
                }

                return ThreadOps.WaitEvent(localResultEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool WaitResult(
                int timeout
                )
            {
                CheckDisposed();

                EventWaitHandle localResultEvent;

                lock (syncRoot)
                {
                    localResultEvent = resultEvent;
                }

                return ThreadOps.WaitEvent(localResultEvent, timeout);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool WaitExit()
            {
                CheckDisposed();

                EventWaitHandle localExitEvent;

                lock (syncRoot)
                {
                    localExitEvent = exitEvent;
                }

                return ThreadOps.WaitEvent(localExitEvent);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool WaitExit(
                int timeout
                )
            {
                CheckDisposed();

                EventWaitHandle localExitEvent;

                lock (syncRoot)
                {
                    localExitEvent = exitEvent;
                }

                return ThreadOps.WaitEvent(localExitEvent, timeout);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool UnSignalScriptAndResult()
            {
                CheckDisposed();

                EventWaitHandle localScriptEvent;
                EventWaitHandle localResultEvent;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localScriptEvent = scriptEvent;
                    localResultEvent = resultEvent;
                }

                bool result = true;

                if (!ThreadOps.ResetEvent(localScriptEvent))
                    result = false;

                if (!ThreadOps.ResetEvent(localResultEvent))
                    result = false;

                return result;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool SignalScriptAndWaitResult(
                int timeout
                )
            {
                CheckDisposed();

                EventWaitHandle localScriptEvent;
                EventWaitHandle localResultEvent;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localScriptEvent = scriptEvent;
                    localResultEvent = resultEvent;
                }

                return ThreadOps.SignalAndWaitEvents(
                    localScriptEvent, localResultEvent, timeout,
                    NoStaThread);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public string TakeText()
            {
                CheckDisposed();

                return Interlocked.Exchange(ref text, null);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void ClearResult()
            {
                CheckDisposed();

                PrivateClearResult();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void GetResult(
                ref ReturnCode returnCode,
                ref Result result,
                ref int errorLine
                )
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    returnCode = this.returnCode;
                    result = this.result;
                    errorLine = this.errorLine;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SetResult(
                ReturnCode returnCode,
                Result result,
                int errorLine
                )
            {
                CheckDisposed();

                PrivateSetResult(returnCode, result, errorLine);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SetResult(
                ReturnCode returnCode,
                Result result,
                int errorLine,
                bool errorInfo
                )
            {
                CheckDisposed();

                PrivateSetResult(returnCode, result, errorLine, errorInfo);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SetAndSignalResult(
                ReturnCode returnCode,
                Result result,
                int errorLine
                )
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    PrivateSetResult(returnCode, result, errorLine);
                    PrivateSignalResult();
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SetAndSignalResult(
                ReturnCode returnCode,
                Result result,
                int errorLine,
                bool errorInfo
                )
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    PrivateSetResult(returnCode, result, errorLine, errorInfo);
                    PrivateSignalResult();
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void ClearErrorInformation()
            {
                CheckDisposed();

                PrivateClearErrorInformation();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool CheckPopulateErrorInformation()
            {
                CheckDisposed();

                return PrivateCheckPopulateErrorInformation();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void GetErrorInformation(
                ref Result errorCode,
                ref Result errorInfo
                )
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    errorCode = this.errorCode;
                    errorInfo = this.errorInfo;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void ClearResultAndErrorInformation()
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    PrivateClearResult();
                    PrivateClearErrorInformation();
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void UnSignalScript()
            {
                CheckDisposed();

                PrivateUnSignalScript();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void UnSignalResult()
            {
                CheckDisposed();

                PrivateUnSignalResult();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SignalDone()
            {
                CheckDisposed();

                PrivateSignalDone();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SignalScript()
            {
                CheckDisposed();

                PrivateSignalScript();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SignalResult()
            {
                CheckDisposed();

                PrivateSignalResult();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void SignalExit()
            {
                CheckDisposed();

                PrivateSignalExit();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void StartTimeout(
                ref ScriptTimeoutThread scriptTimeoutThread
                )
            {
                ScriptThreadClientData scriptThreadClientData =
                    ScriptThreadClientData.Create(
                        null, UseThreadPoolForTimeout);

                StartTimeout(
                    scriptThreadClientData, ref scriptTimeoutThread);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void StartTimeout(
                ScriptThreadClientData scriptThreadClientData,
                ref ScriptTimeoutThread scriptTimeoutThread
                )
            {
                CheckDisposed();

                Interpreter localInterpreter;
                int? localTimeout;
                bool localUseTimeout;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    localInterpreter = interpreter;
                    localTimeout = timeout;
                    localUseTimeout = useTimeout;
                }

                if (localUseTimeout)
                {
                    scriptTimeoutThread = ScriptTimeoutThread.Create(
                        localInterpreter, localTimeout,
                        scriptThreadClientData);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptEventState).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptEventState).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        //
                        // NOTE: First, attempt to unblock any threads that may
                        //       be waiting.
                        //
                        PrivateSignalDone(); /* trigger event thread to exit... */
                        ClearAndSignalResult(); /* trigger other thread to unblock... */
                        WaitExitForDispose(); /* wait for event thread to exit... */

                        //
                        // NOTE: Next, dispose of the event wait handle objects.
                        //
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            /* NO RESULT */
                            ThreadOps.CloseEvent(ref doneEvent);

                            /* NO RESULT */
                            ThreadOps.CloseEvent(ref scriptEvent);

                            /* NO RESULT */
                            ThreadOps.CloseEvent(ref resultEvent);

                            /* NO RESULT */
                            ThreadOps.CloseEvent(ref exitEvent);

                            interpreter = null; /* NOT OWNED, DO NOT DISPOSE. */
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptEventState()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptThreadClientData Test Class
        [ObjectId("d3b5006d-4b6b-406c-b498-a5203037b7b3")]
        public sealed class ScriptThreadClientData : ClientData
        {
            #region Private Constants
            private static bool DefaultUseThreadPool = false;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static int DefaultMaxStackSize = 0;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool DefaultUserInterface = false;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool DefaultIsBackground = true;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool DefaultUseActiveStack = true;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool DefaultAutoStart = true;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool DefaultWaitOnStop = false;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool DefaultWaitOnInterrupt = false;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool DefaultNoAbort = true;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private ScriptThreadClientData(
                object data
                )
                : base(data)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ScriptThreadClientData(
                object data,
                bool useThreadPool,
                int maxStackSize,
                bool userInterface,
                bool isBackground,
                bool useActiveStack,
                bool autoStart,
                bool waitOnStop,
                bool waitOnInterrupt,
                bool noAbort
                )
                : this(data)
            {
                this.useThreadPool = useThreadPool;
                this.maxStackSize = maxStackSize;
                this.userInterface = userInterface;
                this.isBackground = isBackground;
                this.useActiveStack = useActiveStack;
                this.autoStart = autoStart;
                this.waitOnStop = waitOnStop;
                this.waitOnInterrupt = waitOnInterrupt;
                this.noAbort = noAbort;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static ScriptThreadClientData Create(
                object data,
                bool useThreadPool
                )
            {
                return new ScriptThreadClientData(
                    data, useThreadPool, DefaultMaxStackSize,
                    DefaultUserInterface, DefaultIsBackground,
                    DefaultUseActiveStack, DefaultAutoStart,
                    DefaultWaitOnStop, DefaultWaitOnInterrupt,
                    DefaultNoAbort);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ScriptThreadClientData Create( /* RARE */
                object data,
                bool useThreadPool,
                int maxStackSize,
                bool userInterface,
                bool isBackground,
                bool useActiveStack,
                bool autoStart,
                bool waitOnStop,
                bool waitOnInterrupt,
                bool noAbort
                )
            {
                return new ScriptThreadClientData(
                    data, useThreadPool, maxStackSize, userInterface,
                    isBackground, useActiveStack, autoStart, waitOnStop,
                    waitOnInterrupt, noAbort);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private bool useThreadPool;
            public bool UseThreadPool
            {
                get { return useThreadPool; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int maxStackSize;
            public int MaxStackSize
            {
                get { return maxStackSize; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool userInterface;
            public bool UserInterface
            {
                get { return userInterface; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool isBackground;
            public bool IsBackground
            {
                get { return isBackground; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool useActiveStack;
            public bool UseActiveStack
            {
                get { return useActiveStack; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool autoStart;
            public bool AutoStart
            {
                get { return autoStart; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool waitOnStop;
            public bool WaitOnStop
            {
                get { return waitOnStop; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool waitOnInterrupt;
            public bool WaitOnInterrupt
            {
                get { return waitOnInterrupt; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool noAbort;
            public bool NoAbort
            {
                get { return noAbort; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Static Methods
            internal static void GetStartParameters(
                ScriptThreadClientData scriptThreadClientData,
                out bool useThreadPool,
                out int maxStackSize,
                out bool userInterface,
                out bool isBackground,
                out bool useActiveStack
                )
            {
                if (scriptThreadClientData != null)
                {
                    useThreadPool = scriptThreadClientData.useThreadPool;
                    maxStackSize = scriptThreadClientData.maxStackSize;
                    userInterface = scriptThreadClientData.userInterface;
                    isBackground = scriptThreadClientData.isBackground;
                    useActiveStack = scriptThreadClientData.useActiveStack;
                }
                else
                {
                    useThreadPool = DefaultUseThreadPool;
                    maxStackSize = DefaultMaxStackSize;
                    userInterface = DefaultUserInterface;
                    isBackground = DefaultIsBackground;
                    useActiveStack = DefaultUseActiveStack;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            internal static void GetStopParameters(
                ScriptThreadClientData scriptThreadClientData,
                out bool waitOnStop,
                out bool waitOnInterrupt,
                out bool noAbort
                )
            {
                if (scriptThreadClientData != null)
                {
                    waitOnStop = scriptThreadClientData.WaitOnStop;
                    waitOnInterrupt = scriptThreadClientData.WaitOnInterrupt;
                    noAbort = scriptThreadClientData.NoAbort;
                }
                else
                {
                    waitOnStop = DefaultWaitOnStop;
                    waitOnInterrupt = DefaultWaitOnInterrupt;
                    noAbort = DefaultNoAbort;
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptEventThread Test Class
        [ObjectId("791aa07b-7c96-409c-8d8e-7205f07bb7c7")]
        public sealed class ScriptEventThread : IGetInterpreter, IDisposable
        {
            #region Private Data
            private static readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ScriptEventState scriptEventState;
            private ScriptThreadClientData scriptThreadClientData;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Thread thread;
            private long threadId;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private ScriptEventThread(
                ScriptEventState scriptEventState,
                ScriptThreadClientData scriptThreadClientData
                )
            {
                this.scriptEventState = scriptEventState;
                this.scriptThreadClientData = scriptThreadClientData;

                ///////////////////////////////////////////////////////////////////////////////////////

                MaybeAutoStart();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static ScriptEventThread Create(
                ScriptEventState scriptEventState,
                ScriptThreadClientData scriptThreadClientData
                )
            {
                return new ScriptEventThread(
                    scriptEventState, scriptThreadClientData);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            //
            // NOTE: This method assumes the lock is already held -OR-
            //       is being called from a context where locks should
            //       not generally be obtained.
            //
            private Interpreter GetInterpreter()
            {
                return (scriptEventState != null) ?
                    scriptEventState.Interpreter : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool IsAutoStart()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (scriptThreadClientData == null)
                        return false;

                    return scriptThreadClientData.AutoStart;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void MaybeAutoStart()
            {
                if (IsAutoStart())
                    Start();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void EvaluateScript(
                ScriptEventState scriptEventState
                )
            {
                if (scriptEventState == null)
                    return;

                ScriptTimeoutThread scriptTimeoutThread = null;

                try
                {
                    Interpreter interpreter;

                    lock (syncRoot)
                    {
                        interpreter = GetInterpreter();
                    }

                    if (interpreter == null)
                        return;

                    string text = scriptEventState.TakeText();

                    if (text == null)
                        return;

                    scriptEventState.StartTimeout(
                        ref scriptTimeoutThread);

                    ReturnCode code;
                    Result result = null;
                    int errorLine = 0;

                    code = interpreter.EvaluateScript(
                        text, ref result, ref errorLine);

                    scriptEventState.SetResult(
                        code, result, errorLine, true);
                }
                finally
                {
                    scriptEventState.SignalResult();

                    if (scriptTimeoutThread != null)
                    {
                        scriptTimeoutThread.Stop();
                        scriptTimeoutThread = null;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ThreadStart Method
            private void ThreadStart(
                object obj /* NOT USED */
                ) /* System.Threading.ParameterizedThreadStart */
            {
                ScriptEventState localScriptEventState = null;

                try
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        //
                        // NOTE: Set the thread Id for this instance to the
                        //       current thread we are executing on (which
                        //       may be from the thread pool).  This can be
                        //       used by our owner to figure out what our
                        //       actual thread is.  It's not used elsewhere
                        //       in this class.
                        //
                        threadId = GlobalState.GetCurrentSystemThreadId();

                        //
                        // NOTE: Grab the script event state instance, which
                        //       is used to communicate with our creator.
                        //
                        localScriptEventState = scriptEventState;
                    }

                    if (localScriptEventState == null)
                        return;

                    while (true)
                    {
                        //
                        // NOTE: Wait for something to happen.  Either we will
                        //       exit our loop, evaluate a script, or maybe do
                        //       nothing (wait timeout).
                        //
                        int index = localScriptEventState.WaitScriptOrDone();

                        //
                        // NOTE: Check the result of the wait operation, using
                        //       roughly an order of priority.
                        //
                        if (index == WaitHandle.WaitTimeout)
                        {
                            //
                            // NOTE: This is the normal case, there is nothing
                            //       to do yet.  Keep waiting.
                            //
                            continue;
                        }
                        else if (ThreadOps.WasAnyWaitFailed(index))
                        {
                            //
                            // NOTE: There was some kind of error (invalid wait
                            //       handle, etc?) during the wait operation.
                            //       Normally, there is not much point in doing
                            //       any more waiting, since there is no way it
                            //       could succeed unless the wait handles were
                            //       somehow magically made valid again.
                            //
                            // TODO: In the future, perhaps this should be more
                            //       flexible, e.g. by adding a property to the
                            //       ScriptEventState class.
                            //
                            break;
                        }
                        else if (index == 0)
                        {
                            //
                            // NOTE: The "done" event was signaled, bail out
                            //       now.
                            //
                            break;
                        }
                        else if (index == 1)
                        {
                            //
                            // NOTE: Make sure that the script related event
                            //       is no longer signaled.
                            //
                            localScriptEventState.UnSignalScript();

                            //
                            // NOTE: The "script" event was signaled; so do
                            //       the evaluation now.  The script text to
                            //       evaluate should be contained within the
                            //       mutable script triplet passed when this
                            //       thread was created.  The triplet itself
                            //       will be updated before the script event
                            //       is signaled.  The interpreter will also
                            //       be refreshed before being used.
                            //
                            EvaluateScript(localScriptEventState);
                        }
                        else
                        {
                            //
                            // NOTE: Something odd happened, log it?
                            //
                            TraceOps.DebugTrace(String.Format(
                                "unhandled event wait result: {0}",
                                index), typeof(ScriptEventThread).Name,
                                TracePriority.ThreadError);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                catch (ThreadInterruptedException)
                {
                    // do nothing.
                }
                catch (InterpreterDisposedException)
                {
                    // do nothing.
                }
                catch (Exception e)
                {
                    //
                    // NOTE: Since a user-defined script is being evaluated,
                    //       we catch and log all types of exceptions.
                    //
                    TraceOps.DebugTrace(
                        e, typeof(ScriptEventThread).Name,
                        TracePriority.ThreadError);
                }
                finally
                {
                    //
                    // NOTE: Indicate to any waiting thread that this thread
                    //       is now exiting.
                    //
                    if (localScriptEventState != null)
                    {
                        try
                        {
                            localScriptEventState.SignalExit();
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptEventThread).Name,
                                TracePriority.ThreadError);
                        }

                        localScriptEventState = null;
                    }
                }
            }
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter Members
            public Interpreter Interpreter
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return GetInterpreter();
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public void Start() /* throw */
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (thread != null)
                    {
                        throw new InvalidOperationException(
                            "script thread already started");
                    }

                    bool useThreadPool;
                    int maxStackSize;
                    bool userInterface;
                    bool isBackground;
                    bool useActiveStack;

                    ScriptThreadClientData.GetStartParameters(
                        scriptThreadClientData, out useThreadPool,
                        out maxStackSize, out userInterface,
                        out isBackground, out useActiveStack);

                    Interpreter localInterpreter = GetInterpreter();

                    ThreadOps.CreateAndOrStart(
                        localInterpreter, "scriptEvent", ThreadStart,
                        null, useThreadPool, maxStackSize, userInterface,
                        isBackground, useActiveStack, ref thread);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void Stop() /* throw */
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    bool waitOnStop;
                    bool waitOnInterrupt;
                    bool noAbort;

                    ScriptThreadClientData.GetStopParameters(
                        scriptThreadClientData, out waitOnStop,
                        out waitOnInterrupt, out noAbort);

                    Interpreter localInterpreter = GetInterpreter();

                    ShutdownFlags shutdownFlags = ShutdownFlags.ScriptEvent;

                    if (waitOnStop)
                        shutdownFlags |= ShutdownFlags.WaitBefore;

                    if (waitOnInterrupt)
                        shutdownFlags |= ShutdownFlags.WaitAfter;

                    if (noAbort)
                        shutdownFlags |= ShutdownFlags.NoAbort;

                    ThreadOps.MaybeShutdown(
                        localInterpreter, null, shutdownFlags, ref thread);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                CheckDisposed();

                long localThreadId;

                lock (syncRoot)
                {
                    localThreadId = threadId;
                }

                return localThreadId.ToString();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                Interpreter localInterpreter;

                lock (syncRoot)
                {
                    localInterpreter = GetInterpreter();
                }

                if (disposed &&
                    Engine.IsThrowOnDisposed(localInterpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptEventThread).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptEventThread).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (scriptEventState != null)
                            {
                                scriptEventState.Dispose();
                                scriptEventState = null;
                            }
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptEventThread()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptTimeoutThread Test Class
        [ObjectId("02cf4a67-9859-4f08-99b8-6285dcb57620")]
        public sealed class ScriptTimeoutThread : IGetInterpreter, IDisposable
        {
            #region Private Constants
            private static readonly string RequestIdRuntimeOption =
                typeof(ScriptTimeoutThread).Name + "RequestId";
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Static Data
            private static long nextRequestId;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private static readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////////////////////////////

            EventWaitHandle timeoutEvent;
            private int? timeout;
            private long requestId;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ScriptThreadClientData scriptThreadClientData;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private Thread thread;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private ScriptTimeoutThread(
                Interpreter interpreter,
                int? timeout,
                ScriptThreadClientData scriptThreadClientData
                )
                : this(interpreter, timeout, GetRequestId(),
                       scriptThreadClientData)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ScriptTimeoutThread(
                Interpreter interpreter,
                int? timeout,
                long requestId,
                ScriptThreadClientData scriptThreadClientData
                )
            {
                this.interpreter = interpreter;
                this.timeoutEvent = null;
                this.timeout = timeout;
                this.requestId = requestId;
                this.scriptThreadClientData = scriptThreadClientData;

                ///////////////////////////////////////////////////////////////////////////////////////

                MaybeAutoStart();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static ScriptTimeoutThread Create(
                Interpreter interpreter,
                int? timeout,
                ScriptThreadClientData scriptThreadClientData
                )
            {
                return new ScriptTimeoutThread(
                    interpreter, timeout, scriptThreadClientData);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ScriptTimeoutThread Create(
                Interpreter interpreter,
                int? timeout,
                long requestId,
                ScriptThreadClientData scriptThreadClientData
                )
            {
                return new ScriptTimeoutThread(
                    interpreter, timeout, requestId,
                    scriptThreadClientData);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void MaybeCloseEvent()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (timeoutEvent == null)
                        return;

                    ThreadOps.CloseEvent(ref timeoutEvent);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void MaybeCreateEvent()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (timeoutEvent != null)
                        return;

                    timeoutEvent = ThreadOps.CreateEvent(false);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void MaybeSignalEvent()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (timeoutEvent == null)
                        return;

                    ThreadOps.SetEvent(timeoutEvent);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool IsAutoStart()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (scriptThreadClientData == null)
                        return false;

                    return scriptThreadClientData.AutoStart;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void MaybeAutoStart()
            {
                if (IsAutoStart())
                    Start();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Static Methods
            private static long GetRequestId()
            {
                return Interlocked.Increment(ref nextRequestId);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static long GetRequestId(
                Interpreter interpreter
                )
            {
                if (interpreter != null)
                {
                    try
                    {
                        IClientData clientData = null;

                        if (interpreter.GetRuntimeOption(
                                RequestIdRuntimeOption,
                                ref clientData))
                        {
                            if (clientData != null)
                            {
                                object data = clientData.Data;

                                if (data is long)
                                    return (long)data;
                            }
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                return 0;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static bool SetRequestId(
                Interpreter interpreter,
                long requestId
                )
            {
                if (interpreter != null)
                {
                    try
                    {
                        if (interpreter.SetRuntimeOption(
                                RequestIdRuntimeOption,
                                new ClientData(requestId)))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ParameterizedThreadStart Method
            private static void ThreadStart(
                object obj
                ) /* System.Threading.ParameterizedThreadStart */
            {
                try
                {
                    //
                    // NOTE: If this thread was not passed the correct type of
                    //       object(s), just do nothing and then return.
                    //
                    TimeoutTriplet anyTriplet = obj as TimeoutTriplet;

                    if (anyTriplet == null)
                        return;

                    ScriptTimeoutClientData scriptTimeoutClientData = anyTriplet.X;

                    if (scriptTimeoutClientData == null)
                        return;

                    //
                    // NOTE: If the interpreter or timeout is not valid, just
                    //       do nothing and then return.  It should be noted
                    //       that the EventWaitHandle here is optional.  When
                    //       missing, a simple sleep will be used instead.
                    //
                    Interpreter interpreter = scriptTimeoutClientData.Interpreter;
                    int timeout = scriptTimeoutClientData.Timeout;

                    if ((interpreter == null) || (timeout <= 0))
                        return;

                    //
                    // HACK: Cannot use the wrapper methods here because they
                    //       catch ThreadInterruptedException, et al.
                    //
                    EventWaitHandle timeoutEvent = anyTriplet.Y;

                    if (timeoutEvent != null)
                    {
#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40 || NET_STANDARD_20)
                        timeoutEvent.WaitOne(timeout); /* throw */
#else
                        timeoutEvent.WaitOne(timeout, false); /* throw */
#endif
                    }
                    else
                    {
                        Thread.Sleep(timeout); /* throw */
                    }

                    //
                    // HACK: If the interpreter is now disposed, there is not
                    //       much else we can do now except log that fact.
                    //
                    long oldRequestId = anyTriplet.Z;

                    if (interpreter.InternalDisposed)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "skipping script timeout handling for " +
                            "interpreter {0} old request {1}: disposed " +
                            "after approximately {2} milliseconds",
                            FormatOps.InterpreterNoThrow(interpreter),
                            oldRequestId, timeout),
                            typeof(ScriptTimeoutThread).Name,
                            TracePriority.ScriptError);

                        return;
                    }

                    //
                    // NOTE: If another timeout thread has been started for
                    //       this interpreter since we started waiting, do
                    //       nothing.
                    //
                    if (oldRequestId != 0)
                    {
                        long newRequestId = GetRequestId(interpreter);

                        if ((newRequestId != 0) &&
                            (newRequestId != oldRequestId))
                        {
                            TraceOps.DebugTrace(String.Format(
                                "skipping script timeout handling for " +
                                "interpreter {0}, new request {1} does " +
                                "not match old request {2} after " +
                                "approximately {3} milliseconds",
                                FormatOps.InterpreterNoThrow(interpreter),
                                newRequestId, oldRequestId, timeout),
                                typeof(ScriptTimeoutThread).Name,
                                TracePriority.LowThreadDebug);

                            return;
                        }
                    }

                    //
                    // NOTE: If the interpreter is not busy (on any thread),
                    //       don't bother to do anything else.  In this case,
                    //       the script may have simply run to completion,
                    //       which means there is nothing to cancel.
                    //
                    if (!interpreter.InternalIsGlobalBusy)
                        return;

                    //
                    // NOTE: Create a result for the error message to be used
                    //       when the script is canceled.
                    //
                    Result cancelResult = String.Format(
                        Interpreter.timeoutCancelResultFormat, timeout);

                    //
                    // NOTE: Cancel any scripts in progress now.  This may
                    //       impact more than one user since the interpreter
                    //       is shared; however, that is seen as a necessary
                    //       evil.
                    //
                    // NOTE: Actually, this should now cancel the script on
                    //       the original (parent) thread only, i.e. if the
                    //       engine context is correctly set.
                    //
                    ReturnCode code;
                    Result error = null;

                    code = interpreter.InternalCancelAnyEvaluate(
#if THREADING
                        scriptTimeoutClientData.EngineContext,
#endif
                        cancelResult, CancelFlags.ScriptTimeout,
                        ref error);

                    if (code != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "script cancellation for interpreter {0} " +
                            "failed: {1}", FormatOps.InterpreterNoThrow(
                            interpreter), ResultOps.Format(code, error)),
                            typeof(ScriptTimeoutThread).Name,
                            TracePriority.EngineError);
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                catch (ThreadInterruptedException)
                {
                    // do nothing.
                }
                catch (InterpreterDisposedException)
                {
                    // do nothing.
                }
            }
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter Members
            private Interpreter interpreter;
            public Interpreter Interpreter
            {
                get { CheckDisposed(); lock (syncRoot) { return interpreter; } }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public void Start() /* throw */
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (thread != null)
                    {
                        throw new InvalidOperationException(
                            "timeout thread already started");
                    }

                    int localTimeout = ThreadOps.GetTimeout(
                        interpreter, timeout, TimeoutType.Script);

                    if (localTimeout > 0)
                    {
                        if ((requestId == 0) ||
                            SetRequestId(interpreter, requestId))
                        {
                            bool useThreadPool;
                            int maxStackSize;
                            bool userInterface;
                            bool isBackground;
                            bool useActiveStack;

                            ScriptThreadClientData.GetStartParameters(
                                scriptThreadClientData, out useThreadPool,
                                out maxStackSize, out userInterface,
                                out isBackground, out useActiveStack);

                            MaybeSignalEvent();
                            MaybeCloseEvent();
                            MaybeCreateEvent();

                            ScriptTimeoutClientData scriptTimeoutClientData =
                                interpreter.CreateScriptTimeoutClientData(
                                    null, TimeoutFlags.Timeout, null,
                                    localTimeout);

                            ThreadOps.CreateAndOrStart(
                                interpreter, "scriptTimeout", ThreadStart,
                                new TimeoutTriplet(
                                    scriptTimeoutClientData, timeoutEvent,
                                    requestId),
                                useThreadPool, maxStackSize, userInterface,
                                isBackground, useActiveStack, ref thread);
                        }
                        else
                        {
                            throw new ScriptException(
                                "failed to set timeout request Id");
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void Stop() /* throw */
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: If there is no thread instance, we must have used
                    //       a thread-pool thread and the MaybeShutdown method
                    //       is totally useless.  Therefore, signal associated
                    //       interrupt event instead.
                    //
                    if (thread != null)
                    {
                        bool waitOnStop;
                        bool waitOnInterrupt;
                        bool noAbort;

                        ScriptThreadClientData.GetStopParameters(
                            scriptThreadClientData, out waitOnStop,
                            out waitOnInterrupt, out noAbort);

                        ShutdownFlags shutdownFlags = ShutdownFlags.ScriptEvent;

                        if (waitOnStop)
                            shutdownFlags |= ShutdownFlags.WaitBefore;

                        if (waitOnInterrupt)
                            shutdownFlags |= ShutdownFlags.WaitAfter;

                        if (noAbort)
                            shutdownFlags |= ShutdownFlags.NoAbort;

                        ThreadOps.MaybeShutdown(
                            interpreter, null, shutdownFlags, ref thread);
                    }
                    else
                    {
                        //
                        // NOTE: Attempt to wake up thread-pool thread from
                        //       its script timeout wait state.
                        //
                        MaybeSignalEvent();
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptTimeoutThread).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptTimeoutThread).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (thread != null)
                            {
                                try
                                {
                                    Stop(); /* throw */
                                }
                                catch (Exception e)
                                {
                                    TraceOps.DebugTrace(
                                        e, typeof(ScriptTimeoutThread).Name,
                                        TracePriority.CleanupError);
                                }

                                thread = null;
                            }

                            ////////////////////////////////

                            MaybeCloseEvent();

                            ////////////////////////////////

                            interpreter = null; /* NOT OWNED, DO NOT DISPOSE. */
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptTimeoutThread()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptMutableValue Test Class
        [ObjectId("4f837494-d654-4c08-ba84-e00057772972")]
        public class ScriptMutableValue : IDisposable
        {
            #region Public Constructors
            public ScriptMutableValue(
                object value, /* in */
                bool owned    /* in */
                )
            {
                this.value = value;
                this.owned = owned;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private object value;
            public virtual object Value
            {
                get { CheckDisposed(); return this.value; }
                set { CheckDisposed(); this.value = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool owned;
            public virtual bool Owned
            {
                get { CheckDisposed(); return this.owned; }
                set { CheckDisposed(); this.owned = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                CheckDisposed();

                return StringList.MakeList(
                    FormatOps.WrapOrNull(this.Value), this.Owned);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptMutableValue).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(
                bool disposing /* in */
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(ScriptMutableValue).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        if (owned)
                        {
                            ObjectOps.TryDisposeOrComplain<object>(
                                null, ref value);

                            value = null;
                            owned = false;
                        }
                        else
                        {
                            value = null; /* NOT OWNED */
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptMutableValue()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptBooleanValue Test Class
        [ObjectId("73601e17-4e60-41fd-92dd-7ee2d9f875a7")]
        public sealed class ScriptBooleanValue : ScriptMutableValue
        {
            #region Public Constructors
            public ScriptBooleanValue(
                bool? value /* in */
                )
                : base(value, false)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public void MaybeGetValue(
                ref bool value /* in, out */
                )
            {
                CheckDisposed();

                object localValue = base.Value;

                if (localValue is bool)
                    value = (bool)localValue;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public void MaybeGetNullableValue(
                ref bool? value /* in, out */
                )
            {
                CheckDisposed();

                object localValue = base.Value;

                if (localValue is bool?)
                    value = (bool?)localValue;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptBooleanValue).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing /* in */
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////
                        }

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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region StringComparer Test Class
        [ObjectId("80a09504-c74c-4104-a2bd-ea1c419d0e50")]
        public sealed class StringComparer : IComparer<string>
        {
            #region Private Data
            private IComparer<string> comparer = null;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public StringComparer()
            {
                comparer = Comparer<string>.Default;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            public IComparer<string> Comparer
            {
                get { return comparer; }
                set { comparer = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IComparer<string> Members
            public int Compare(
                string x,
                string y
                )
            {
                if (comparer == null)
                    throw new ScriptException("invalid string comparer");

                return comparer.Compare(x, y);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IResolve Test Class
        [ObjectId("c420fccf-69f8-463a-b97b-629d7f7fcd9f")]
        public sealed class Resolve :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            ScriptMarshalByRefObject,
#endif
            IResolve
        {
            #region Private Data
            //
            // NOTE: The interpreter where the script should be evaluated in.
            //
            private Interpreter sourceInterpreter;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The interpreter where the variable frame, current namespace,
            //           execute, or variable is being resolved.
            //
            private Interpreter targetInterpreter;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The script to evaluate when this resolver instance is called.
            //
            private string text;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The call frame to return from the GetVariableFrame method
            //       if the script returns non-zero.
            //
            private ICallFrame frame;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The namespace to return from the GetCurrentNamespace method
            //       if the script returns non-zero.
            //
            private INamespace @namespace;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The command to return from the GetIExecute method if the
            //       script returns non-zero.
            //
            private IExecute execute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The variable to return from the GetVariable method if the
            //       script returns non-zero.
            //
            private IVariable variable;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: These are flags that control the behavior of the various
            //       IResolve methods of this class.
            //
            private TestResolveFlags testResolveFlags;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Keeps track of the number of times each IResolve method
            //       has been called.
            //
            private readonly int[] methodInvokeCounts = {
                0, /* GetVariableFrame */
                0, /* GetCurrentNamespace */
                0, /* GetIExecute */
                0  /* GetVariable */
            };
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public Resolve(
                Interpreter sourceInterpreter,
                Interpreter targetInterpreter,
                string text,
                ICallFrame frame,
                INamespace @namespace,
                IExecute execute,
                IVariable variable,
                TestResolveFlags testResolveFlags
                )
            {
                this.sourceInterpreter = sourceInterpreter;
                this.targetInterpreter = targetInterpreter;
                this.text = text;
                this.frame = frame;
                this.@namespace = @namespace;
                this.execute = execute;
                this.variable = variable;
                this.testResolveFlags = testResolveFlags;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierName Members
            public string Name
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierBase Members
            public IdentifierKind Kind
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Guid Id
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifier Members
            public string Group
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public string Description
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter / ISetInterpreter Members
            public Interpreter Interpreter
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetClientData / ISetClientData Members
            public IClientData ClientData
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IWrapperData Members
#if MONO_BUILD
#pragma warning disable 414
#endif
            private long token;
            public long Token
            {
                get { throw new NotImplementedException(); }
                set { token = value; }
            }
#if MONO_BUILD
#pragma warning restore 414
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IResolve Members
            public ReturnCode GetVariableFrame(
                ref ICallFrame frame,
                ref string varName,
                ref VariableFlags flags,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[0]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetVariableFrame: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, varName = {4}, flags = {5}, error = {6}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(varName), FormatOps.WrapOrNull(flags),
                        FormatOps.WrapOrNull(true, true, error)), typeof(Resolve).Name,
                        TracePriority.TestDebug);
                }

                if (!FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.HandleGlobalOnly, true) &&
                    FlagOps.HasFlags(flags, VariableFlags.GlobalOnly, true))
                {
                    if (targetInterpreter != null)
                    {
                        frame = targetInterpreter.CurrentGlobalFrame;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "invalid target interpreter";
                        return ReturnCode.Error;
                    }
                }

                if (!FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.HandleAbsolute, true) &&
                    NamespaceOps.IsAbsoluteName(varName))
                {
                    return NamespaceOps.GetVariableFrame(
                        targetInterpreter, ref frame, ref varName, ref flags,
                        ref error);
                }

                if (!FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.HandleQualified, true) &&
                    NamespaceOps.IsQualifiedName(varName))
                {
                    return NamespaceOps.GetVariableFrame(
                        targetInterpreter, ref frame, ref varName, ref flags,
                        ref error);
                }

                ICallFrame variableFrame = GetVariableFrame();

                if (variableFrame == null)
                {
                    error = "variable frame not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetVariableFrame");
                objects.Add("frame", frame);
                objects.Add("varName", varName);
                objects.Add("flags", flags);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    frame = variableFrame;
                    return ReturnCode.Ok;
                }

                error = "variable frame not found";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode GetCurrentNamespace(
                ICallFrame frame,
                ref INamespace @namespace,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[1]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetCurrentNamespace: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, namespace = {4}, error = {5}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(@namespace), FormatOps.WrapOrNull(true, true, error)),
                        typeof(Resolve).Name, TracePriority.TestDebug);
                }

                INamespace currentNamespace = GetCurrentNamespace();

                if (currentNamespace == null)
                {
                    error = "current namespace not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetCurrentNamespace");
                objects.Add("frame", frame);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    @namespace = currentNamespace;
                    testResolveFlags |= TestResolveFlags.NextUseNamespaceFrame;

                    return ReturnCode.Ok;
                }

                error = "current namespace not found";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode GetIExecute(
                ICallFrame frame,
                EngineFlags engineFlags,
                string name,
                ArgumentList arguments,
                LookupFlags lookupFlags,
                ref bool ambiguous,
                ref long token,
                ref IExecute execute,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[2]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetIExecute: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, engineFlags = {4}, name = {5}, " +
                        "arguments = {6}, lookupFlags = {7}, ambiguous = {8}, token = {9}, " +
                        "execute = {10}, error = {11}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(engineFlags), FormatOps.WrapOrNull(name),
                        FormatOps.WrapOrNull(arguments), FormatOps.WrapOrNull(lookupFlags),
                        FormatOps.WrapOrNull(ambiguous), FormatOps.WrapOrNull(token),
                        FormatOps.WrapOrNull(execute), FormatOps.WrapOrNull(true, true, error)),
                        typeof(Resolve).Name, TracePriority.TestDebug);
                }

                if (this.execute == null)
                {
                    error = "execute not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetIExecute");
                objects.Add("frame", frame);
                objects.Add("engineFlags", engineFlags);
                objects.Add("name", name);
                objects.Add("arguments", arguments);
                objects.Add("lookupFlags", lookupFlags);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    execute = this.execute;
                    return ReturnCode.Ok;
                }

                error = "execute not found";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode GetVariable(
                ICallFrame frame,
                string varName,
                string varIndex,
                ref VariableFlags flags,
                ref IVariable variable,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[3]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetVariable: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, varName = {4}, varIndex = {5}, flags = {6}, " +
                        "variable = {7}, error = {8}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(varName), FormatOps.WrapOrNull(varIndex),
                        FormatOps.WrapOrNull(flags), FormatOps.WrapOrNull(variable),
                        FormatOps.WrapOrNull(true, true, error)), typeof(Resolve).Name,
                        TracePriority.TestDebug);
                }

                if (this.variable == null)
                {
                    error = "variable not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetVariable");
                objects.Add("frame", frame);
                objects.Add("varName", varName);
                objects.Add("varIndex", varIndex);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    variable = this.variable;
                    return ReturnCode.Ok;
                }

                error = "variable not found";
                return ReturnCode.Error;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private ICallFrame GetVariableFrame()
            {
                if (frame != null)
                    return frame;

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.AlwaysUseNamespaceFrame,
                        true))
                {
                    if (@namespace != null)
                        return @namespace.VariableFrame;
                }
                else if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.NextUseNamespaceFrame,
                        true))
                {
                    /* ONE SHOT */
                    testResolveFlags &= ~TestResolveFlags.NextUseNamespaceFrame;

                    if (@namespace != null)
                        return @namespace.VariableFrame;
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: For use by the IResolve.GetCurrentNamespace method only.
            //
            private INamespace GetCurrentNamespace()
            {
                testResolveFlags &= ~TestResolveFlags.NextUseNamespaceFrame;

                return @namespace;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                StringList list = new StringList();

                for (int index = 0; index < methodInvokeCounts.Length; index++)
                    list.Add(methodInvokeCounts[index].ToString());

                return list.ToString();
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICommand Test Class
        [CommandFlags(CommandFlags.NoPopulate | CommandFlags.NoAdd)]
        [ObjectId("36efb842-527f-4ffa-8978-adc3c8fdce99")]
        public sealed class Command : ICommand
        {
            #region Private Data
            //
            // NOTE: The script command to evaluate when this command instance
            //       is executed (this only applies if the "useIExecute" flag
            //       is false).
            //
            private StringList scriptCommand;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The IExecute to execute when this command instance is
            //       executed (this only applies if the "useIExecute" flag
            //       is true).
            //
            private IExecute execute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the IExecute will be used instead
            //       of the script command.
            //
            private bool useIExecute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the arguments to the command
            //       will be appended to the script command to be evaluated
            //       (this only applies if the "useIExecute" flag is false).
            //
            private bool useExecuteArguments;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the arguments containing the name
            //       of the command will not be added to the final list of
            //       arguments to the script command.
            //
            private bool skipNameArguments;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, there can be no extra arguments to
            //       the command beyond the name of the command.
            //
            private bool strictNoArguments;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This keeps track of the number of times the Execute method
            //       of this class handles a command.  This value starts at zero,
            //       is always incremented, and never reset.
            //
            private int executeCount;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public Command(
                string name,
                IPlugin plugin,
                ExecuteCallback callback,
                IClientData clientData,
                CommandFlags commandFlags,
                StringList scriptCommand,
                IExecute execute,
                bool useIExecute,
                bool strictNoArguments,
                bool useExecuteArguments,
                bool skipNameArguments
                )
            {
                this.name = name;
                this.plugin = plugin;
                this.callback = callback;
                this.clientData = clientData;
                this.commandFlags = commandFlags;
                this.scriptCommand = scriptCommand;
                this.execute = execute;
                this.useIExecute = useIExecute;
                this.strictNoArguments = strictNoArguments;
                this.useExecuteArguments = useExecuteArguments;
                this.skipNameArguments = skipNameArguments;
                this.executeCount = 0;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private string GetCommandName()
            {
                return this.Name;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool AllowedToUseArguments(
                ArgumentList arguments,
                ref Result error
                )
            {
                if (!strictNoArguments)
                    return true;

                int nameIndex = 0; /* NOTE: Compat: Tcl */
                int nextIndex = nameIndex + 1;

                if ((arguments == null) || (arguments.Count <= nextIndex))
                    return true;

                error = String.Format(
                    "wrong # args: should be \"{0}\"", GetCommandName());

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ArgumentList GetArgumentsForExecute(
                ArgumentList arguments
                )
            {
                return useExecuteArguments ? arguments : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int GetStartIndexForArguments(
                ArgumentList arguments
                )
            {
                if (!skipNameArguments)
                    return 0;

                int nameIndex = 0; /* NOTE: Compat: Tcl */
                int nextIndex = nameIndex + 1;

                if ((arguments == null) || (arguments.Count < nextIndex))
                    return 0;

                return nextIndex;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetClientData / ISetClientData Members
            private IClientData clientData;
            public IClientData ClientData
            {
                get { return clientData; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierName Members
            private string name;
            public string Name
            {
                get { return name; }
                set { name = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierBase Members
            public IdentifierKind Kind
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Guid Id
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifier Members
            public string Group
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public string Description
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ICommandBaseData Members
            public string TypeName
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private CommandFlags commandFlags;
            public CommandFlags CommandFlags
            {
                get { return commandFlags; }
                set { commandFlags = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IHavePlugin Members
            private IPlugin plugin;
            public IPlugin Plugin
            {
                get { return plugin; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IWrapperData Members
            private long token;
            public long Token
            {
                get { throw new NotImplementedException(); }
                set { token = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ICommandData Members
            public CommandFlags Flags
            {
                get { return commandFlags; }
                set { commandFlags = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IState Members
            public bool Initialized
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode Initialize(
                Interpreter interpreter,
                IClientData clientData,
                ref Result result
                )
            {
                return ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode Terminate(
                Interpreter interpreter,
                IClientData clientData,
                ref Result result
                )
            {
                return ReturnCode.Ok;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDynamicExecuteCallback Members
            private ExecuteCallback callback;
            public ExecuteCallback Callback
            {
                get { return callback; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IEnsemble Members
            public EnsembleDictionary SubCommands
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IPolicyEnsemble Members
            public EnsembleDictionary AllowedSubCommands
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public EnsembleDictionary DisallowedSubCommands
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ISyntax Members
            public string Syntax
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IUsageData Members
            public bool ResetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool GetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool SetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool AddUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool CountUsage(
                ref long count
                )
            {
                //
                // NOTE: This is a stub required by the Engine class.
                //       Do nothing.
                //
                return true;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool ProfileUsage(
                ref long microseconds
                )
            {
                //
                // NOTE: This is a stub required by the Engine class.
                //       Do nothing.
                //
                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecute Members
            public ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }

                int nameIndex = 0; /* NOTE: Compat: Tcl */
                int nextIndex = nameIndex + 1;

                if (arguments.Count < nextIndex)
                {
                    result = String.Format(
                        "wrong # args: should be \"{0} ?arg ...?\"",
                        GetCommandName());

                    return ReturnCode.Error;
                }

                //
                // NOTE: Does this command accept arguments beyond the
                //       name of the command?
                //
                if (!AllowedToUseArguments(arguments, ref result))
                    return ReturnCode.Error;

                Interlocked.Increment(ref executeCount);

                if (useIExecute)
                {
                    //
                    // NOTE: Re-dispatch to the configured IExecute
                    //       instance and return its results verbatim.
                    //
                    string commandName = EntityOps.GetName(
                        execute as IIdentifierName);

                    if (commandName == null)
                        commandName = arguments[nameIndex];

                    return interpreter.Execute(
                        commandName, execute, clientData, arguments,
                        ref result);
                }
                else
                {
                    //
                    // NOTE: Evaluate the configured script command, maybe
                    //       adding all the local arguments, and return the
                    //       results verbatim.
                    //
                    string name = StringList.MakeList(GetCommandName());

                    ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                        CallFrameFlags.Evaluate | CallFrameFlags.Test |
                        CallFrameFlags.Command);

                    interpreter.PushAutomaticCallFrame(frame);

                    ReturnCode code = interpreter.EvaluateScript(
                        ScriptOps.GetArgumentsForExecute(this,
                        scriptCommand, GetArgumentsForExecute(arguments),
                        GetStartIndexForArguments(arguments)), 0, ref result);

                    if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format("{0}    (\"{1}\" body line {2})",
                                Environment.NewLine, GetCommandName(),
                                Interpreter.GetErrorLine(interpreter)));
                    }

                    //
                    // NOTE: Pop the original call frame that we pushed above and
                    //       any intervening scope call frames that may be leftover
                    //       (i.e. they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFramesAndOneMore();
                    return code;
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Test Class
        [ObjectId("4e325bd3-23d5-404c-bd9f-6b76fa86cddb")]
        public sealed class Execute : IExecute
        {
            #region Public Constructors
            public Execute()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode AddToInterpreter(
                Interpreter interpreter,
                string name,
                IClientData clientData,
                ref long token,
                ref Result error
                )
            {
                return Helpers.AddToInterpreter(
                    interpreter, name, this, clientData, ref token,
                    ref error);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecute Members
            ReturnCode IExecute.Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISyntax Test Class
        [ObjectId("a24cbf79-9244-4d2e-9303-6e7b8dc7d448")]
        public sealed class Syntax : IExecute, ISyntax
        {
            #region Public Constructors
            public Syntax()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Syntax(
                string syntax
                )
            {
                this.syntax = syntax;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode AddToInterpreter(
                Interpreter interpreter,
                string name,
                IClientData clientData,
                ref long token,
                ref Result error
                )
            {
                return Helpers.AddToInterpreter(
                    interpreter, name, this, clientData, ref token,
                    ref error);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ISyntax Members
            private string syntax;
            string ISyntax.Syntax
            {
                get { return syntax; }
                set { syntax = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecute Members
            public ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Test Class
        [ObjectId("bd7f00ed-3621-4fde-985c-e55d09ece914")]
        public sealed class Ensemble : IExecute, IEnsemble
        {
            #region Public Constructors
            public Ensemble()
            {
                this.subCommands = new EnsembleDictionary();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Ensemble(
                IEnumerable<string> collection
                )
            {
                AddSubCommands(collection);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Ensemble(
                params string[] args
                )
            {
                AddSubCommands(args);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private int AddSubCommands(
                IEnumerable<string> collection
                )
            {
                if (collection != null)
                {
                    if (subCommands == null)
                        subCommands = new EnsembleDictionary();

                    int oldCount = subCommands.Count;

                    foreach (string item in collection)
                    {
                        if (item == null)
                            continue;

                        subCommands[item] = null;
                    }

                    return subCommands.Count - oldCount;
                }

                return 0;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode AddToInterpreter(
                Interpreter interpreter,
                string name,
                IClientData clientData,
                ref long token,
                ref Result error
                )
            {
                return Helpers.AddToInterpreter(
                    interpreter, name, this, clientData, ref token,
                    ref error);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IEnsemble Members
            private EnsembleDictionary subCommands;
            public EnsembleDictionary SubCommands
            {
                get { return subCommands; }
                set { subCommands = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecute Members
            public ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISubCommand Test Class
        [ObjectId("b9d6a0c9-8bca-4558-8b55-68334118978f")]
        public sealed class SubCommand : ISubCommand
        {
            #region Private Data
            //
            // NOTE: The script command to evaluate when this sub-command
            //       instance is executed (this only applies if the
            //       "useIExecute" flag is false).
            //
            private StringList scriptCommand;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The IExecute to execute when this sub-command instance
            //       is executed (this only applies if the "useIExecute" flag
            //       is true).
            //
            private IExecute execute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the index into the argument list where the name
            //       of the sub-command is.
            //
            private int nameIndex;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the IExecute will be used instead
            //       of the script command.
            //
            private bool useIExecute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the arguments to the sub-command
            //       will be appended to the script command to be evaluated
            //       (this only applies if the "useIExecute" flag is false).
            //
            private bool useExecuteArguments;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the arguments containing names
            //       of the command and sub-command will not be added to the
            //       final list of arguments to the script command.
            //
            private bool skipNameArguments;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, there can be no extra arguments
            //       to the sub-command beyond the names of the command and
            //       sub-command.
            //
            private bool strictNoArguments;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This keeps track of the number of times the Execute
            //       method of this class handles a sub-command.  This value
            //       starts at zero, is always incremented, and never reset.
            //
            private int executeCount;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public SubCommand(
                string name,
                ICommand command,
                ExecuteCallback callback,
                IClientData clientData,
                CommandFlags commandFlags,
                StringList scriptCommand,
                IExecute execute,
                int nameIndex,
                bool useIExecute,
                bool strictNoArguments,
                bool useExecuteArguments,
                bool skipNameArguments
                )
            {
                this.name = name;
                this.command = command;
                this.callback = callback;
                this.clientData = clientData;
                this.commandFlags = commandFlags;
                this.scriptCommand = scriptCommand;
                this.execute = execute;
                this.nameIndex = nameIndex;
                this.useIExecute = useIExecute;
                this.strictNoArguments = strictNoArguments;
                this.useExecuteArguments = useExecuteArguments;
                this.skipNameArguments = skipNameArguments;
                this.executeCount = 0;

                ///////////////////////////////////////////////////////////////////////////////////////

                SetupSubCommands();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private string GetCommandName()
            {
                return ScriptOps.GetNameForExecute(null, this);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void SetupSubCommands()
            {
                //
                // NOTE: We only handle one sub-command at a time and it is
                //       always handled locally (i.e. using null ISubCommand
                //       instance).
                //
                subCommands = new EnsembleDictionary();
                subCommands.Add(this.Name, null);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool AllowedToUseArguments(
                ArgumentList arguments,
                ref Result error
                )
            {
                if (!strictNoArguments)
                    return true;

                int nameIndex = this.NameIndex;
                int nextIndex = nameIndex + 1;

                if ((arguments == null) || (arguments.Count <= nextIndex))
                    return true;

                error = String.Format(
                    "wrong # args: should be \"{0}\"", GetCommandName());

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ArgumentList GetArgumentsForExecute(
                ArgumentList arguments
                )
            {
                return useExecuteArguments ? arguments : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int GetStartIndexForArguments(
                ArgumentList arguments
                )
            {
                if (!skipNameArguments)
                    return 0;

                int nameIndex = this.NameIndex;
                int nextIndex = nameIndex + 1;

                if ((arguments == null) || (arguments.Count < nextIndex))
                    return 0;

                return nextIndex;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetClientData / ISetClientData Members
            private IClientData clientData;
            public IClientData ClientData
            {
                get { return clientData; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierName Members
            private string name;
            public string Name
            {
                get { return name; }
                set { name = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierBase Members
            public IdentifierKind Kind
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Guid Id
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifier Members
            public string Group
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public string Description
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ICommandBaseData Members
            public string TypeName
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private CommandFlags commandFlags;
            public CommandFlags CommandFlags
            {
                get { return commandFlags; }
                set { commandFlags = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IWrapperData Members
            public long Token
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IHaveCommand Members
            private ICommand command;
            public ICommand Command
            {
                get { return command; }
                set { command = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ISubCommandData Members
            public int NameIndex
            {
                get { return nameIndex; }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public SubCommandFlags Flags
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDynamicExecuteCallback Members
            private ExecuteCallback callback;
            public ExecuteCallback Callback
            {
                get { return callback; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDynamicExecuteDelegate Members
            public Delegate Delegate
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDelegateData Members
            public DelegateFlags DelegateFlags
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IEnsemble Members
            private EnsembleDictionary subCommands;
            public EnsembleDictionary SubCommands
            {
                get { return subCommands; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IPolicyEnsemble Members
            public EnsembleDictionary AllowedSubCommands
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public EnsembleDictionary DisallowedSubCommands
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ISyntax Members
            public string Syntax
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IUsageData Members
            public bool ResetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool GetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool SetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool AddUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool CountUsage(
                ref long count
                )
            {
                //
                // NOTE: This is a stub required by the Engine class.
                //       Do nothing.
                //
                return true;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool ProfileUsage(
                ref long microseconds
                )
            {
                //
                // NOTE: This is a stub required by the Engine class.
                //       Do nothing.
                //
                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecute Members
            public ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }

                int nameIndex = this.NameIndex;
                int nextIndex = nameIndex + 1;

                if (arguments.Count < nextIndex)
                {
                    result = String.Format(
                        "wrong # args: should be \"{0} ?arg ...?\"",
                        GetCommandName());

                    return ReturnCode.Error;
                }

                string subCommand = arguments[nameIndex];

                if (!StringOps.SubCommandEquals(subCommand, this.Name))
                {
                    result = ScriptOps.BadSubCommand(
                        interpreter, null, null, subCommand, this, null,
                        null);

                    return ReturnCode.Error;
                }

                //
                // NOTE: Does this sub-command accept arguments beyond
                //       the names of the command and sub-command?
                //
                if (!AllowedToUseArguments(arguments, ref result))
                    return ReturnCode.Error;

                Interlocked.Increment(ref executeCount);

                if (useIExecute)
                {
                    //
                    // NOTE: Re-dispatch to the configured IExecute
                    //       instance and return its results verbatim.
                    //
                    string commandName = EntityOps.GetName(
                        execute as IIdentifierName);

                    if (commandName == null)
                        commandName = arguments[nameIndex - 1];

                    return interpreter.Execute(
                        commandName, execute, clientData, arguments,
                        ref result);
                }
                else
                {
                    //
                    // NOTE: Evaluate the configured script command, maybe
                    //       adding all the local arguments, and return the
                    //       results verbatim.
                    //
                    string name = StringList.MakeList(GetCommandName());

                    ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                        CallFrameFlags.Evaluate | CallFrameFlags.Test |
                        CallFrameFlags.SubCommand);

                    interpreter.PushAutomaticCallFrame(frame);

                    ReturnCode code = interpreter.EvaluateScript(
                        ScriptOps.GetArgumentsForExecute(this,
                        scriptCommand, GetArgumentsForExecute(arguments),
                        GetStartIndexForArguments(arguments)), 0, ref result);

                    if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format("{0}    (\"{1}\" body line {2})",
                                Environment.NewLine, GetCommandName(),
                                Interpreter.GetErrorLine(interpreter)));
                    }

                    //
                    // NOTE: Pop the original call frame that we pushed above and
                    //       any intervening scope call frames that may be leftover
                    //       (i.e. they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFramesAndOneMore();
                    return code;
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region GetStringPlugin Test Class
        [ObjectId("62bc8b71-3214-4ef2-b783-c41a93761f31")]
        [PluginFlags(
            PluginFlags.System | PluginFlags.Static |
            PluginFlags.NoCommands | PluginFlags.NoFunctions |
            PluginFlags.NoPolicies | PluginFlags.NoTraces |
            PluginFlags.Test)]
        public class GetStringPlugin : _Plugins.Default, IDisposable
        {
            #region Private Constants
            //
            // HACK: These are purposely not read-only.
            //
            private static bool DefaultUseStatic = true;
            private static bool DefaultUseInstance = true;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////////////////////////////

            private StringDictionary strings;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool useStatic; /* NO-LOCK */
            private bool useInstance; /* NO-LOCK */
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public GetStringPlugin(
                IPluginData pluginData /* in */
                )
                : base(pluginData)
            {
                this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                    AttributeOps.GetPluginFlags(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void Initialize()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (strings == null)
                        strings = new StringDictionary();
                }

                useStatic = DefaultUseStatic;
                useInstance = DefaultUseInstance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void Terminate()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (strings != null)
                    {
                        strings.Clear();
                        strings = null;
                    }
                }

                useStatic = false;
                useInstance = false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ReturnCode Count(
                ref object response, /* out */
                ref Result error     /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (strings != null)
                    {
                        response = strings.Count;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "strings not available";
                        return ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ReturnCode List(
                ref object response, /* out */
                ref Result error     /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (strings != null)
                    {
                        response = strings.KeysToString(null, false);
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "strings not available";
                        return ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ReturnCode Clear(
                ref object response, /* out */
                ref Result error     /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (strings != null)
                    {
                        response = strings.Count;
                        strings.Clear();

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "strings not available";
                        return ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ReturnCode Add(
                string name,         /* in */
                string value,        /* in */
                ref object response, /* out */
                ref Result error     /* out */
                )
            {
                if (name != null)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (strings != null)
                        {
                            strings.Add(name, value);
                            response = null;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "strings not available";
                            return ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    error = "invalid string name";
                    return ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ReturnCode Change(
                string name,         /* in */
                string value,        /* in */
                ref object response, /* out */
                ref Result error     /* out */
                )
            {
                if (name != null)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (strings != null)
                        {
                            strings[name] = value;
                            response = null;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "strings not available";
                            return ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    error = "invalid string name";
                    return ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ReturnCode Remove(
                string name,         /* in */
                ref object response, /* out */
                ref Result error     /* out */
                )
            {
                if (name != null)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (strings != null)
                        {
                            response = strings.Remove(name);
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "strings not available";
                            return ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    error = "invalid string name";
                    return ReturnCode.Error;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IState Members
            public override ReturnCode Initialize(
                Interpreter interpreter, /* in */
                IClientData clientData,  /* in */
                ref Result result        /* out */
                )
            {
                CheckDisposed();

                Initialize();

                return base.Initialize(interpreter, clientData, ref result);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override ReturnCode Terminate(
                Interpreter interpreter, /* in */
                IClientData clientData,  /* in */
                ref Result result        /* out */
                )
            {
                CheckDisposed();

                Terminate();

                return base.Terminate(interpreter, clientData, ref result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecuteRequest Members
            public override ReturnCode Execute(
                Interpreter interpreter, /* in */
                IClientData clientData,  /* in */
                object request,          /* in */
                ref object response,     /* out */
                ref Result error         /* out */
                )
            {
                CheckDisposed();

                //
                // NOTE: This method is not supposed to raise an error under
                //       normal conditions when faced with an unrecognized
                //       request.  It simply does nothing and lets the base
                //       plugin handle it.
                //
                if (request is string[])
                {
                    string[] stringRequest = (string[])request;

                    ArgumentList arguments = new ArgumentList(
                        (IEnumerable<string>)stringRequest);

                    int argumentCount; /* REUSED */

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (_RuntimeOps.MatchRequestName(
                            arguments, "countStrings", out argumentCount) &&
                        (argumentCount == 1))
                    {
                        return Count(ref response, ref error);
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (_RuntimeOps.MatchRequestName(
                            arguments, "listStrings", out argumentCount) &&
                        (argumentCount == 1))
                    {
                        return List(ref response, ref error);
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (_RuntimeOps.MatchRequestName(
                            arguments, "clearStrings", out argumentCount) &&
                        (argumentCount == 1))
                    {
                        return Clear(ref response, ref error);
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (_RuntimeOps.MatchRequestName(
                            arguments, "addString", out argumentCount) &&
                        (argumentCount == 3))
                    {
                        return Add(arguments[1], arguments[2], ref response, ref error);
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (_RuntimeOps.MatchRequestName(
                            arguments, "changeString", out argumentCount) &&
                        (argumentCount == 3))
                    {
                        return Change(arguments[1], arguments[2], ref response, ref error);
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (_RuntimeOps.MatchRequestName(
                            arguments, "removeString", out argumentCount) &&
                        (argumentCount == 2))
                    {
                        return Remove(arguments[1], ref response, ref error);
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: If this point is reached the request was not handled.
                //       Call our base plugin and let it attempt to handle the
                //       request.
                //
                return base.Execute(
                    interpreter, clientData, request, ref response, ref error);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IPlugin Members
            public override string GetString(
                Interpreter interpreter, /* in */
                string name,             /* in */
                CultureInfo cultureInfo, /* in */
                ref Result error         /* out */
                )
            {
                CheckDisposed();

                string value; /* REUSED */
                Result localError; /* REUSED */
                ResultList errors = null;

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Instance (Private) Strings
                if (useInstance)
                {
                    if (name != null)
                    {
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if ((strings != null) &&
                                strings.TryGetValue(name, out value))
                            {
                                return value; /* MAY BE NULL */
                            }
                        }
                    }

                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "instance {0} has no string named {1}",
                        FormatOps.WrapHashCode(this),
                        FormatOps.WrapOrNull(name)));
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Static (Shared) Strings
                if (useStatic)
                {
                    localError = null;

                    value = TestGetString(
                        interpreter, name, cultureInfo, ref localError);

                    if (value != null)
                        return value;

                    if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Base Class Strings (None?)
                localError = null;

                value = base.GetString(
                    interpreter, name, cultureInfo, ref localError);

                if (value != null)
                    return value;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                if (errors != null)
                    error = errors;

                return null;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(GetStringPlugin).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Dispose(
                bool disposing /* in */
                )
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: called, disposing = {0}, disposed = {1}",
                    disposing, disposed), typeof(GetStringPlugin).Name,
                    TracePriority.CleanupDebug);

                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        Terminate();
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~GetStringPlugin()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region NativeTraceListener Test Class
#if NATIVE
        [ObjectId("ead726b6-0a9a-467c-ab1f-b6ddd939ab8a")]
        public class NativeTraceListener : TraceListener
        {
            #region Public Constructors
            public NativeTraceListener()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public NativeTraceListener(
                string name
                )
                : base(name)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();

                base.Close();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                base.Flush();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message /* in */
                )
            {
                CheckDisposed();

                /* IGNORED */
                NativeOps.OutputDebugMessage(message); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message /* in */
                )
            {
                CheckDisposed();

                /* IGNORED */
                NativeOps.OutputDebugMessage(String.Format(
                    "{0}{1}", message, Environment.NewLine)); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsThreadSafe
            {
                get { CheckDisposed(); return true; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(NativeTraceListener).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int disposeLevels;
            protected override void Dispose(
                bool disposing /* in */
                )
            {
                int levels = Interlocked.Increment(ref disposeLevels);

                try
                {
                    if (levels == 1)
                    {
                        try
                        {
                            if (!disposed)
                            {
                                if (disposing)
                                {
                                    ////////////////////////////////////
                                    // dispose managed resources here...
                                    ////////////////////////////////////

                                    /* NO RESULT */
                                    DebugOps.RemoveTraceListener(this);

                                    ////////////////////////////////////

                                    Close();
                                }
                            }
                        }
                        finally
                        {
                            base.Dispose(disposing);

                            disposed = true;
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref disposeLevels);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~NativeTraceListener()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region BufferedTraceListener Test Class
        [ObjectId("437d590d-843b-4f96-a7a5-4a89957ce154")]
        public class BufferedTraceListener : TraceListener, IBufferedTraceListener
        {
            #region Private Constants
            //
            // HACK: This is purposely not read-only.
            //
            private static int DefaultInitialCapacity = 10;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static readonly string NoTraceListenerError = "inner trace listener missing";
            private static readonly string BadTraceListenerError = "bad inner trace listener";
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////////////////////////////

            private long nextId;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private IList<IAnyTriplet<long, string, bool>> messages;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private BufferedTraceListener()
            {
                InitializeMessages();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private BufferedTraceListener(
                TraceListener listener,
                BufferedTraceFlags flags,
                int initialCapacity,
                int maximumCapacity
                )
                : this()
            {
                this.traceListener = listener;
                this.flags = flags;
                this.initialCapacity = initialCapacity;
                this.maximumCapacity = maximumCapacity;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static TraceListener Create(
                TraceListener listener,
                BufferedTraceFlags flags,
                int initialCapacity,
                int maximumCapacity,
                ref Result error /* NOT USED */
                )
            {
                return new BufferedTraceListener(
                    listener, flags, initialCapacity, maximumCapacity);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode Install(
                BufferedTraceListener listener,
                Type type,
                TraceListenerType listenerType,
                IClientData clientData,
                bool debug,
                ref Result error
                )
            {
                if (listener == null)
                {
                    error = "invalid buffered trace listener";
                    return ReturnCode.Error;
                }

                TraceListenerCollection listeners = DebugOps.GetListeners(
                    debug);

                if (listeners == null)
                {
                    error = "invalid trace listener collection";
                    return ReturnCode.Error;
                }

                int count = listeners.Count;

                if (count > 0)
                {
                    bool found = false;
                    Result localError = null;

                    for (int index = 0; index < count; index++)
                    {
                        TraceListener innerListener = listeners[index];

                        if (innerListener == null)
                            continue;

                        if (innerListener is BufferedTraceListener)
                        {
                            localError = "trace listener already installed";
                            continue;
                        }

                        if ((type == null) || Object.ReferenceEquals(
                                innerListener.GetType(), type))
                        {
                            listener.TraceListener = innerListener;

                            listeners.RemoveAt(index);
                            listeners.Insert(index, listener);

                            found = true;
                            break;
                        }
                    }

                    if (found)
                        return ReturnCode.Ok;

                    if (localError != null)
                        error = localError;
                    else
                        error = "trace listener is not installed";

                    return ReturnCode.Error;
                }
                else if (type == null)
                {
                    TraceListener innerListener = listener.CreateTraceListener(
                        listenerType, clientData, ref error);

                    if (innerListener == null)
                        return ReturnCode.Error;

                    //
                    // HACK: This if statement cannot be hit unless
                    //       our CreateTraceListener method ends up
                    //       being overridden in a derived class.
                    //
                    if (innerListener is BufferedTraceListener)
                    {
                        innerListener.Dispose();
                        innerListener = null;

                        error = "trace listener already installed";
                        return ReturnCode.Error;
                    }

                    listener.TraceListener = innerListener;
                    listeners.Add(listener);

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "no trace listeners are installed";
                    return ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode Uninstall(
                bool debug,
                ref Result error
                )
            {
                TraceListenerCollection listeners = DebugOps.GetListeners(
                    debug);

                if (listeners == null)
                {
                    error = "invalid trace listener collection";
                    return ReturnCode.Error;
                }

                int count = listeners.Count;

                if (count == 0)
                {
                    error = "no trace listeners are installed";
                    return ReturnCode.Error;
                }

                bool found = false;

                for (int index = count - 1; index >= 0; index--)
                {
                    BufferedTraceListener listener =
                        listeners[index] as BufferedTraceListener;

                    if (listener != null)
                    {
                        listeners.RemoveAt(index);

                        TraceListener innerListener = null;

                        if (!listener.IsTakeOwnership())
                            innerListener = listener.TraceListener;

                        listener.Dispose();
                        listener = null;

                        if (innerListener != null)
                            listeners.Insert(index, innerListener);

                        found = true;
                        break;
                    }
                }

                if (found)
                    return ReturnCode.Ok;

                error = "trace listener is not installed";
                return ReturnCode.Error;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private TraceListener traceListener;
            public virtual TraceListener TraceListener
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return traceListener;
                    }
                }
                set
                {
                    CheckDisposed();

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        DisposeTraceListener(false);

                        traceListener = value;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private BufferedTraceFlags flags;
            public virtual BufferedTraceFlags Flags
            {
                get { CheckDisposed(); lock (syncRoot) { return flags; } }
                set { CheckDisposed(); lock (syncRoot) { flags = value; } }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int initialCapacity;
            public virtual int InitialCapacity
            {
                get { CheckDisposed(); lock (syncRoot) { return initialCapacity; } }
                set { CheckDisposed(); lock (syncRoot) { initialCapacity = value; } }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int maximumCapacity;
            public virtual int MaximumCapacity
            {
                get { CheckDisposed(); lock (syncRoot) { return maximumCapacity; } }
                set { CheckDisposed(); lock (syncRoot) { maximumCapacity = value; } }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Static Methods
            private static StreamWriter GetStreamWriter(
                string fileName,
                Encoding encoding,
                bool append
                )
            {
                return (encoding != null) ?
                    new StreamWriter(fileName, append, encoding) :
                    new StreamWriter(fileName, append);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private string GetString(
                IAnyTriplet<long, string, bool> anyTriplet
                )
            {
                return GetString(anyTriplet, true);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void GetTraceListener(
                out TraceListener listener
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    listener = this.TraceListener; /* throw */
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void WriteTraceListener(
                string message,
                bool newLine
                )
            {
                if (newLine)
                    WriteLineTraceListener(message); /* throw */
                else
                    WriteTraceListener(message); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void CheckMessagesAndFlushTraceListener()
            {
                if (!IsBufferingDisabled() && IsEmptyOnFlush())
                {
                    /* IGNORED */
                    EmptyMessages();
                }

                FlushTraceListener();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            #region TraceListener Wraper Methods
            protected virtual TraceListener CreateTraceListener(
                TraceListenerType listenerType,
                IClientData clientData,
                ref Result error
                )
            {
                return DebugOps.NewTraceListener(
                    listenerType, clientData, ref error);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void CloseTraceListener()
            {
                TraceListener listener;

                GetTraceListener(out listener);

                if (listener == null)
                    throw new InvalidOperationException(NoTraceListenerError);

                if (Object.ReferenceEquals(listener, this))
                    throw new InvalidOperationException(BadTraceListenerError);

                listener.Close(); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void FlushTraceListener()
            {
                TraceListener listener;

                GetTraceListener(out listener);

                if (listener == null)
                    throw new InvalidOperationException(NoTraceListenerError);

                if (Object.ReferenceEquals(listener, this))
                    throw new InvalidOperationException(BadTraceListenerError);

                listener.Flush(); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool MaybeFlushTraceListenerForClose()
            {
                if (IsNeverFlush() || !IsFlushOnClose())
                    return false;

                FlushTraceListener(); /* throw */
                return true;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool MaybeFlushTraceListenerForEmpty()
            {
                if (IsNeverFlush() || !IsFlushOnEmpty())
                    return false;

                FlushTraceListener(); /* throw */
                return true;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool MaybeFlushTraceListener(
                bool close
                )
            {
                return close ?
                    MaybeFlushTraceListenerForClose() :
                    MaybeFlushTraceListenerForEmpty();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void WriteTraceListener(
                string message
                )
            {
                TraceListener listener;

                GetTraceListener(out listener);

                if (listener == null)
                    throw new InvalidOperationException(NoTraceListenerError);

                if (Object.ReferenceEquals(listener, this))
                    throw new InvalidOperationException(BadTraceListenerError);

                listener.Write(message); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void WriteLineTraceListener(
                string message
                )
            {
                TraceListener listener;

                GetTraceListener(out listener);

                if (listener == null)
                    throw new InvalidOperationException(NoTraceListenerError);

                if (Object.ReferenceEquals(listener, this))
                    throw new InvalidOperationException(BadTraceListenerError);

                listener.WriteLine(message); /* throw */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Message Formatting Methods
            protected virtual long GetNextId()
            {
                return Interlocked.Increment(ref nextId);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual string GetString(
                IAnyTriplet<long, string, bool> anyTriplet,
                bool useNewLine
                )
            {
                if (anyTriplet == null)
                    return null;

                string formatted = GetString(
                    anyTriplet.Y, useNewLine && anyTriplet.Z);

                if (!FlagOps.HasFlags(
                        flags, BufferedTraceFlags.FormatWithId, true))
                {
                    return formatted;
                }

                return String.Format("{0}: {1}", anyTriplet.X, formatted);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual string GetString(
                string message,
                bool newLine
                )
            {
                if (message != null)
                {
                    if (!newLine)
                        return message;

                    return String.Format(
                        "{0}{1}", message, Environment.NewLine);
                }
                else if (newLine)
                {
                    return Environment.NewLine;
                }
                else
                {
                    return null;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Flag Helper Methods
            protected virtual bool IsTakeOwnership()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.TakeOwnership, true);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void DisableTakeOwnership()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    flags &= ~BufferedTraceFlags.TakeOwnership;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsEmptyOnClose()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.EmptyOnClose, true);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsIgnoreEmptyOnClose()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.IgnoreEmptyOnClose, true);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsNeverFlush()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.NeverFlush, true);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsFlushOnClose()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.FlushOnClose, true);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsEmptyOnFlush()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.EmptyOnFlush, true);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsFlushOnEmpty()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.FlushOnEmpty, true);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsBufferingDisabled()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return FlagOps.HasFlags(
                        flags, BufferedTraceFlags.BufferingDisabled, true);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Message Buffer Methods
            protected virtual void InitializeMessages()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    messages = new List<IAnyTriplet<long, string, bool>>(
                        (initialCapacity < 0) ? DefaultInitialCapacity :
                        initialCapacity);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void CountMessages(
                ref MessageCountDictionary messageCounts
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (messages == null)
                        return;

                    bool coalesceDuplicates = FlagOps.HasFlags(
                        flags, BufferedTraceFlags.CoalesceDuplicates, true);

                    if (!coalesceDuplicates)
                        return;

                    bool consecutiveDuplicates = FlagOps.HasFlags(
                        flags, BufferedTraceFlags.ConsecutiveDuplicates, true);

                    if (messageCounts == null)
                        messageCounts = new MessageCountDictionary();

                    int messageCount = messages.Count;

                    for (int index = 0; index < messageCount; index++)
                    {
                        IAnyTriplet<long, string, bool> anyTriplet1 =
                            messages[index];

                        if (anyTriplet1 == null)
                            continue;

                        string formatted1 = GetString(anyTriplet1);

                        if (formatted1 == null)
                            continue;

                        if (consecutiveDuplicates)
                        {
                            if (index <= 0)
                                continue;

                            IAnyTriplet<long, string, bool> anyTriplet2 =
                                messages[index - 1];

                            if (anyTriplet2 == null)
                                continue;

                            string formatted2 = GetString(anyTriplet2);

                            if (formatted2 == null)
                                continue;

                            if (!SharedStringOps.SystemEquals(
                                    formatted1, formatted2))
                            {
                                continue;
                            }
                        }

                        long duplicateCount;

                        if (messageCounts.TryGetValue(
                                formatted1, out duplicateCount))
                        {
                            messageCounts[formatted1] = ++duplicateCount;
                        }
                        else
                        {
                            duplicateCount = 1;
                            messageCounts.Add(formatted1, duplicateCount);
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool EmptyMessages()
            {
                MessageCountDictionary messageCounts = null;

                CountMessages(ref messageCounts);

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (messages == null)
                        return false;

                    bool wrote = false;
                    string formatString;

                    if (FlagOps.HasFlags(
                            flags, BufferedTraceFlags.NoRepeatedCount, true))
                    {
                        formatString = "{0}";
                    }
                    else
                    {
                        formatString = "(repeated {1} {2}) {0}";
                    }

                    int messageCount = messages.Count;

                    for (int index = 0; index < messageCount; index++)
                    {
                        IAnyTriplet<long, string, bool> anyTriplet =
                            messages[index];

                        if (anyTriplet == null)
                            continue;

                        string formatted = GetString(anyTriplet);

                        if (formatted == null)
                            continue;

                        string message = anyTriplet.Y;
                        bool newLine = anyTriplet.Z;
                        long duplicateCount;

                        if ((messageCounts != null) && messageCounts.TryGetValue(
                                formatted, out duplicateCount))
                        {
                            if (duplicateCount == Count.Invalid)
                                continue;

                            if (duplicateCount > 1)
                            {
                                duplicateCount--;

                                WriteTraceListener(String.Format(
                                    formatString, message, duplicateCount,
                                    duplicateCount > 1 ? "times" : "time"),
                                    newLine); /* throw */

                                messageCounts[formatted] = Count.Invalid;
                                wrote = true;
                            }
                            else
                            {
                                WriteTraceListener(message, newLine); /* throw */
                                wrote = true;
                            }
                        }
                        else
                        {
                            WriteTraceListener(message, newLine); /* throw */
                            wrote = true;
                        }
                    }

                    messages.Clear();

                    return wrote;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool MaybeEmptyMessages()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (messages == null)
                        return false;

                    if (maximumCapacity < 0)
                        return false;

                    if (messages.Count < maximumCapacity)
                        return false;
                }

                return EmptyMessages();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void AddMessage(
                string message,
                bool newLine
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (messages == null)
                        return;

                    messages.Add(new AnyTriplet<long, string, bool>(
                        GetNextId(), message, newLine));
                }
            }
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IBufferedTraceListener Members
            public virtual bool MaybeFlushBuffers()
            {
                CheckDisposed();

                /* NO RESULT */
                CheckMessagesAndFlushTraceListener();

                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();

                if (!IsBufferingDisabled() && IsEmptyOnClose())
                {
                    if (EmptyMessages() || IsIgnoreEmptyOnClose())
                    {
                        /* IGNORED */
                        MaybeFlushTraceListener(false);
                    }
                }

                /* IGNORED */
                MaybeFlushTraceListener(true);

                CloseTraceListener();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                /* NO RESULT */
                CheckMessagesAndFlushTraceListener();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message
                )
            {
                CheckDisposed();

                if (IsBufferingDisabled())
                {
                    WriteTraceListener(message);
                }
                else
                {
                    AddMessage(message, false);

                    if (MaybeEmptyMessages())
                    {
                        /* IGNORED */
                        MaybeFlushTraceListener(false);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message
                )
            {
                CheckDisposed();

                if (IsBufferingDisabled())
                {
                    WriteLineTraceListener(message);
                }
                else
                {
                    AddMessage(message, true);

                    if (MaybeEmptyMessages())
                    {
                        /* IGNORED */
                        MaybeFlushTraceListener(false);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsThreadSafe
            {
                get { CheckDisposed(); return true; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode Dump(
                string fileName,
                Encoding encoding,
                bool append,
                bool clear,
                ref Result error
                )
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (messages == null)
                    {
                        error = "no messages available";
                        return ReturnCode.Error;
                    }

                    try
                    {
                        using (StreamWriter streamWriter = GetStreamWriter(
                                fileName, encoding, append))
                        {
                            foreach (IAnyTriplet<long, string, bool> anyTriplet
                                    in messages)
                            {
                                if (anyTriplet == null)
                                    continue;

                                string messageString = GetString(
                                    anyTriplet, false);

                                if (messageString == null)
                                    continue;

                                if (anyTriplet.Z)
                                    streamWriter.WriteLine(messageString);
                                else
                                    streamWriter.Write(messageString);
                            }

                            streamWriter.Flush();
                        }

                        if (clear)
                            messages.Clear();

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }

                return ReturnCode.Error;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(BufferedTraceListener).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void DisposeMessages()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (messages != null)
                    {
                        messages.Clear();
                        messages = null;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void DisposeTraceListener(
                bool resetTakeOwnership
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (traceListener != null)
                    {
                        if (IsTakeOwnership())
                        {
                            traceListener.Dispose();

                            if (resetTakeOwnership)
                                DisableTakeOwnership();
                        }

                        traceListener = null;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int disposeLevels;
            protected override void Dispose(
                bool disposing
                )
            {
                int levels = Interlocked.Increment(ref disposeLevels);

                try
                {
                    if (levels == 1)
                    {
                        try
                        {
                            if (!disposed)
                            {
                                if (disposing)
                                {
                                    ////////////////////////////////////
                                    // dispose managed resources here...
                                    ////////////////////////////////////

                                    /* NO RESULT */
                                    DebugOps.RemoveTraceListener(this);

                                    ////////////////////////////////////

                                    DisposeMessages();
                                    DisposeTraceListener(true);
                                }
                            }
                        }
                        finally
                        {
                            base.Dispose(disposing);

                            disposed = true;
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref disposeLevels);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~BufferedTraceListener()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptTraceListener Test Class
        [ObjectId("91730fa7-dbe4-42cd-b175-9ccb66cae405")]
        public class ScriptTraceListener : TraceListener
        {
            #region Private Data
            private int traceLevels = 0;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private ScriptTraceListener(
                Interpreter interpreter,
                string text,
                string argument
                )
            {
                this.interpreter = interpreter;
                this.text = text;
                this.argument = argument;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static TraceListener Create(
                Interpreter interpreter,
                string text,
                string argument,
                ref Result error /* NOT USED */
                )
            {
                return new ScriptTraceListener(interpreter, text, argument);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private bool disabled;
            public virtual bool Disabled
            {
                get { CheckDisposed(); return disabled; }
                set { CheckDisposed(); disabled = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the script to evaluate in response to the methods
            //       overridden methods from the base class (TraceListener).
            //
            private string text;
            public virtual string Text
            {
                get { CheckDisposed(); return text; }
                set { CheckDisposed(); text = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the string argument (e.g. method name) passed to
            //       the trace listener creation callback that was responsible
            //       for creating this trace listener instance.
            //
            private string argument;
            public virtual string Argument
            {
                get { CheckDisposed(); return argument; }
                set { CheckDisposed(); argument = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter / ISetInterpreter Members
            //
            // NOTE: This is the interpreter context that the script will be
            //       evaluated in.
            //
            private Interpreter interpreter;
            public virtual Interpreter Interpreter
            {
                get { CheckDisposed(); return interpreter; }
                set { CheckDisposed(); interpreter = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            protected virtual void CheckDisabled()
            {
                if (this.Disabled)
                {
                    throw new TraceException(String.Format(
                        "instance {0} of {1} is disabled",
                        FormatOps.WrapHashCode(this),
                        typeof(ScriptTraceListener)));
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual bool IsUsable()
            {
                //
                // NOTE: Since this class uses the complaint subsystem,
                //       both directly and indirectly, it is not usable
                //       if there is a complaint pending.
                //
                return !DebugOps.IsComplainPending();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Complain(
                Interpreter interpreter,
                ReturnCode code,
                Result result
                )
            {
                //
                // NOTE: This should be legal even though the complaint
                //       subsystem (potentially) calls into this trace
                //       listener (e.g. via the Trace.Write, which uses
                //       the Trace.Listeners collection) because all of
                //       the TraceListener method overrides avoid doing
                //       any processing when a complaint is pending.
                //
                DebugOps.Complain(interpreter, code, result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();
                CheckDisabled();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("listener", this);
                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "Close");

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();
                CheckDisabled();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("listener", this);
                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "Flush");

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message
                )
            {
                CheckDisposed();
                CheckDisabled();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("listener", this);
                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "Write");
                        objects.Add("message", message);

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message
                )
            {
                CheckDisposed();
                CheckDisabled();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("listener", this);
                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "WriteLine");
                        objects.Add("message", message);

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsThreadSafe
            {
                get { CheckDisposed(); CheckDisabled(); return true; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptTraceListener).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            interpreter = null; /* NOT OWNED */
                            text = null;
                            argument = null;
                        }

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

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptTraceListener()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region TraceException Test Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("265edb17-053e-4cb6-ac68-ccb15b126bb2")]
        public class TraceException : ScriptException
        {
            #region Private Static Data
            private static long count;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public TraceException()
                : base()
            {
                IncrementCount();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public TraceException(
                string message
                )
                : base(message)
            {
                IncrementCount();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public TraceException(
                string message,
                Exception innerException
                )
                : base(message, innerException)
            {
                IncrementCount();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public TraceException(
                ReturnCode code,
                Result result
                )
                : base(code, result)
            {
                IncrementCount();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public TraceException(
                ReturnCode code,
                Result result,
                Exception innerException
                )
                : base(code, result, innerException)
            {
                IncrementCount();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Constructors
#if SERIALIZATION
            protected TraceException(
                SerializationInfo info,
                StreamingContext context
                )
                : base(info, context)
            {
                // do nothing.
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            protected void IncrementCount()
            {
                Interlocked.Increment(ref count);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Introspection Support Methods
            //
            // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
            //
            public static new void AddInfo(
                StringPairList list,    /* in, out */
                DetailFlags detailFlags /* in */
                )
            {
                if (list == null)
                    return;

                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (count != 0))
                    localList.Add("Count", count.ToString());

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Trace Exception");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ThrowTraceListener Test Class
        [ObjectId("a786ad1f-2797-4f8e-a86a-fd9b8e675036")]
        public class ThrowTraceListener : TraceListener
        {
            #region Private Constructors
            private ThrowTraceListener()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private bool throwOnClose;
            public virtual bool ThrowOnClose
            {
                get { CheckDisposed(); return throwOnClose; }
                set { CheckDisposed(); throwOnClose = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool throwOnFlush;
            public virtual bool ThrowOnFlush
            {
                get { CheckDisposed(); return throwOnFlush; }
                set { CheckDisposed(); throwOnFlush = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool throwOnWrite;
            public virtual bool ThrowOnWrite
            {
                get { CheckDisposed(); return throwOnWrite; }
                set { CheckDisposed(); throwOnWrite = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool throwOnWriteLine;
            public virtual bool ThrowOnWriteLine
            {
                get { CheckDisposed(); return throwOnWriteLine; }
                set { CheckDisposed(); throwOnWriteLine = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static TraceListener Create(
                ref Result error /* NOT USED */
                )
            {
                return new ThrowTraceListener();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();

                if (ThrowOnClose)
                    throw new TraceException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                if (ThrowOnFlush)
                    throw new TraceException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message
                )
            {
                CheckDisposed();

                if (ThrowOnWrite)
                    throw new TraceException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message
                )
            {
                CheckDisposed();

                if (ThrowOnWriteLine)
                    throw new TraceException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsThreadSafe
            {
                get { CheckDisposed(); return true; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(ThrowTraceListener).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            throwOnClose = false;
                            throwOnFlush = false;
                            throwOnWrite = false;
                            throwOnWriteLine = false;

                            ////////////////////////////////////

                            /* NO RESULT */
                            DebugOps.RemoveTraceListener(this);
                        }

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

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ThrowTraceListener()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IBufferedTraceListener
        [ObjectId("f6ed23fe-6efa-4d07-b48a-ab215dc288b0")]
        public interface IBufferedTraceListener
        {
            bool MaybeFlushBuffers();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region DatabaseTraceListener Test Class
        [ObjectId("513e5392-29d9-4a84-a7c2-2407122b5eac")]
        public class DatabaseTraceListener : TraceListener, IBufferedTraceListener
        {
            #region Private Constants
            //
            // NOTE: This is the initial buffer capacity for trace message
            //       output.  For optimal performance, it should be large
            //       enough for at least one entire fully detailed logical
            //       line of output.
            //
            private static readonly int BufferCapacity = 20480; /* 20KB */

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Transaction Isolation Level (System.Data)
            //
            // NOTE: This is the isolation level when starting a database
            //       transaction to be used for inserting trace events in
            //       the target database.
            //
            private static readonly IsolationLevel DefaultIsolationLevel =
                IsolationLevel.Unspecified;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region SQL DML Statements
            //
            // NOTE: This is used to insert a single row with two columns,
            //       one for the new trace message and one for the new
            //       trace category.  It must work with any SQL database.
            //
            private static readonly string InsertCommandText =
                "INSERT INTO {0} ({1}, {2}) VALUES ({3}, {4});";
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDbDataParameter Names
            private static readonly string MessageParameterName = "@message";
            private static readonly string CategoryParameterName = "@category";
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private readonly object syncRoot = new object();
            private int traceLevels = 0;
            private IDbConnection connection;
            private IDbTransaction transaction;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private StringBuilder buffer;
            private string bufferCategory;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private DatabaseTraceListener()
            {
                buffer = StringOps.NewStringBuilder(BufferCapacity);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private DatabaseTraceListener(
                Interpreter interpreter,
                DbConnectionType dbConnectionType,
                string assemblyFileName,
                string typeName,
                string connectionString,
                string tableName,
                string messageColumnName,
                string categoryColumnName,
                bool autoFlush,
                bool autoCommit
                )
                : this()
            {
                this.interpreter = interpreter;
                this.dbConnectionType = dbConnectionType;
                this.assemblyFileName = assemblyFileName;
                this.typeName = typeName;
                this.connectionString = connectionString;
                this.tableName = tableName;
                this.messageColumnName = messageColumnName;
                this.categoryColumnName = categoryColumnName;
                this.autoFlush = autoFlush;
                this.autoCommit = autoCommit;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static TraceListener Create(
                Interpreter interpreter,
                DbConnectionType dbConnectionType,
                string assemblyFileName,
                string typeName,
                string connectionString,
                string tableName,
                string messageColumnName,
                string categoryColumnName,
                bool autoFlush,
                bool autoCommit
                )
            {
                return new DatabaseTraceListener(interpreter,
                    dbConnectionType, assemblyFileName, typeName,
                    connectionString, tableName, messageColumnName,
                    categoryColumnName, autoFlush, autoCommit);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private IDbConnection CreateDbConnection(
                ref Result error
                )
            {
                IDbConnection connection = null;

                if (DataOps.CreateDbConnection(
                        interpreter, dbConnectionType, connectionString,
                        assemblyFileName, typeName, typeName,
                        ObjectOps.GetDefaultObjectValueFlags(),
                        ref connection, ref error) == ReturnCode.Ok)
                {
                    return connection;
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool OpenDbConnection()
            {
                Result error = null;

                if (OpenDbConnection(ref error))
                    return true;
                else
                    Complain(ReturnCode.Error, error);

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool OpenDbConnection(
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (connection != null)
                        return true;

                    IDbConnection localConnection = CreateDbConnection(
                        ref error);

                    if (localConnection == null)
                        return false;

                    try
                    {
                        localConnection.Open(); /* throw */

                        connection = localConnection;
                        return true;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool BeginDbTransaction()
            {
                Result error = null;

                if (BeginDbTransaction(ref error))
                    return true;
                else
                    Complain(ReturnCode.Error, error);

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool BeginDbTransaction(
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (transaction != null)
                        return true;

                    if (connection == null)
                    {
                        error = "invalid connection";
                        return false;
                    }

                    IDbTransaction localTransaction = null;

                    try
                    {
                        localTransaction = connection.BeginTransaction(
                            DefaultIsolationLevel); /* throw */

                        if (localTransaction != null)
                        {
                            transaction = localTransaction;
                            return true;
                        }
                        else
                        {
                            error = "unable to begin transaction";
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool InsertTraceRowIntoDb(
                string message,
                string category
                )
            {
                Result error = null;

                if (InsertTraceRowIntoDb(
                        message, category, ref error))
                {
                    return true;
                }
                else
                {
                    Complain(ReturnCode.Error, error);
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool InsertTraceRowIntoDb(
                string message,
                string category,
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (connection == null)
                    {
                        error = "invalid connection";
                        return false;
                    }

                    try
                    {
                        using (IDbCommand command = connection.CreateCommand())
                        {
                            if (command == null)
                            {
                                error = "could not create command";
                                return false;
                            }

                            DataOps.CheckIdentifier("TableName", tableName);

                            DataOps.CheckIdentifier(
                                "MessageColumnName", messageColumnName);

                            DataOps.CheckIdentifier(
                                "CategoryColumnName", categoryColumnName);

                            command.CommandText = DataOps.FormatCommandText(
                                InsertCommandText, 2, tableName,
                                messageColumnName, categoryColumnName,
                                MessageParameterName, CategoryParameterName);

                            IDbDataParameter messageParameter = command.CreateParameter();

                            if (messageParameter == null)
                            {
                                error = "could not create message parameter";
                                return false;
                            }

                            messageParameter.ParameterName = MessageParameterName;
                            messageParameter.Value = message;

                            IDbDataParameter categoryParameter = command.CreateParameter();

                            if (categoryParameter == null)
                            {
                                error = "could not create category parameter";
                                return false;
                            }

                            categoryParameter.ParameterName = CategoryParameterName;
                            categoryParameter.Value = category;

                            command.Parameters.Add(messageParameter);
                            command.Parameters.Add(categoryParameter);

                            if (command.ExecuteNonQuery() <= 0) /* Did we do anything? */
                            {
                                error = "trace event was not inserted";
                                return false;
                            }
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool CommitDbTransaction()
            {
                Result error = null;

                if (CommitDbTransaction(ref error))
                    return true;
                else
                    Complain(ReturnCode.Error, error);

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool CommitDbTransaction(
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (transaction != null)
                    {
                        try
                        {
                            transaction.Commit(); /* throw */
                            transaction = null;

                            return true;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                    }
                    else
                    {
                        error = "invalid transaction";
                    }

                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool RollbackDbTransaction()
            {
                Result error = null;

                if (RollbackDbTransaction(ref error))
                    return true;
                else
                    Complain(ReturnCode.Error, error);

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool RollbackDbTransaction(
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (transaction != null)
                    {
                        try
                        {
                            transaction.Rollback(); /* throw */
                            transaction = null;

                            return true;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                    }
                    else
                    {
                        error = "invalid transaction";
                    }

                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool CloseDbConnection()
            {
                Result error = null;

                if (CloseDbConnection(ref error))
                    return true;
                else
                    Complain(ReturnCode.Error, error);

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool CloseDbConnection(
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (connection != null)
                    {
                        try
                        {
                            connection.Close(); /* throw */
                            connection = null;

                            return true;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                    }
                    else
                    {
                        error = "invalid connection";
                    }

                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool CloseDbTransaction()
            {
                Result error = null;

                if (CloseDbTransaction(ref error))
                    return true;
                else
                    Complain(ReturnCode.Error, error);

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool CloseDbTransaction(
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return autoCommit ?
                        CommitDbTransaction(ref error) :
                        RollbackDbTransaction(ref error);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool MaybeUpdateMessageBuffer(
                ref string message, /* in, out */
                string category,    /* in */
                bool newLine,       /* in */
                ref Result error    /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (buffer == null)
                    {
                        if (newLine)
                        {
                            return true; /* LEGAL */
                        }
                        else
                        {
                            error = "buffer not available";
                            return false; /* ILLEGAL */
                        }
                    }

                    if (!newLine || (buffer.Length > 0))
                    {
                        if (!CheckCategory(category, ref error))
                            return false; /* ILLEGAL */

                        if (newLine)
                        {
                            if (message != null)
                                buffer.AppendLine(message);

                            message = buffer.ToString();
                        }
                        else if (message != null)
                        {
                            buffer.Append(message);
                        }
                    }

                    return true; /* LEGAL */
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void MaybeResetMessageBuffer()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (buffer != null)
                        buffer.Length = 0;

                    if (bufferCategory != null)
                        bufferCategory = null;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool CheckCategory(
                string category,
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if ((category != null) && (bufferCategory != null) &&
                        !SharedStringOps.SystemEquals(category, bufferCategory))
                    {
                        error = String.Format(
                            "buffer category mismatch got {0}, expected {1}",
                            FormatOps.WrapOrNull(category), FormatOps.WrapOrNull(
                            bufferCategory));

                        return false;
                    }

                    return true;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool MaybeWriteToDb(
                string message,
                string category,
                bool newLine
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return MaybeWriteToDb(
                        message, category, newLine, autoFlush);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool MaybeWriteToDb(
                string message,
                string category,
                bool newLine,
                bool flush
                )
            {
                Result error = null;

                if (MaybeWriteToDb(
                        message, category, newLine, flush, ref error))
                {
                    return true;
                }
                else
                {
                    Complain(ReturnCode.Error, error);
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool MaybeWriteToDb(
                string message,
                string category,
                bool newLine,
                bool flush,
                ref Result error
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (newLine)
                    {
                        if (!OpenDbConnection(ref error))
                            return false;

                        if (!BeginDbTransaction(ref error))
                            return false;

                        string localMessage = message;

                        if (!MaybeUpdateMessageBuffer(
                                ref localMessage, category, newLine,
                                ref error))
                        {
                            return false;
                        }

                        if (!InsertTraceRowIntoDb(
                                localMessage, category, ref error))
                        {
                            return false;
                        }

                        if (flush && !CommitDbTransaction(ref error))
                            return false;

                        /* NO RESULT */
                        MaybeResetMessageBuffer();
                    }
                    else
                    {
                        string localMessage = message;

                        if (!MaybeUpdateMessageBuffer(
                                ref localMessage, category, newLine,
                                ref error))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private DbConnectionType dbConnectionType;
            public virtual DbConnectionType DbConnectionType
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return dbConnectionType;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string assemblyFileName;
            public virtual string AssemblyFileName
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return assemblyFileName;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string typeName;
            public virtual string TypeName
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return typeName;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string connectionString;
            public virtual string ConnectionString
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return connectionString;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string tableName;
            public virtual string TableName
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return tableName;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string messageColumnName;
            public virtual string MessageColumnName
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return messageColumnName;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string categoryColumnName;
            public virtual string CategoryColumnName
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return categoryColumnName;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool autoFlush;
            public virtual bool AutoFlush
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return autoFlush;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool autoCommit;
            public virtual bool AutoCommit
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return autoCommit;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter / ISetInterpreter Members
            //
            // NOTE: This is the interpreter context that the database
            //       connections will be created in.
            //
            private Interpreter interpreter;
            public virtual Interpreter Interpreter
            {
                get
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        return interpreter;
                    }
                }
                set
                {
                    CheckDisposed();

                    lock (syncRoot)
                    {
                        interpreter = value;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            protected virtual bool IsUsable()
            {
                //
                // NOTE: Since this class uses the complaint subsystem,
                //       both directly and indirectly, it is not usable
                //       if there is a complaint pending.
                //
                return !DebugOps.IsComplainPending();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Complain(
                ReturnCode code,
                Result result
                )
            {
                Interpreter localInterpreter;

                lock (syncRoot)
                {
                    localInterpreter = interpreter;
                }

                Complain(localInterpreter, code, result);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Complain(
                Interpreter interpreter,
                ReturnCode code,
                Result result
                )
            {
                //
                // NOTE: This should be legal even though the complaint
                //       subsystem (potentially) calls into this trace
                //       listener (e.g. via the Trace.Write, which uses
                //       the Trace.Listeners collection) because all of
                //       the TraceListener method overrides avoid doing
                //       any processing when a complaint is pending.
                //
                DebugOps.Complain(interpreter, code, result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IBufferedTraceListener Members
            public virtual bool MaybeFlushBuffers()
            {
                CheckDisposed();

                if (!IsUsable())
                    return false;

                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                        return MaybeWriteToDb(null, null, true);
                    else
                        return false;
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        /* IGNORED */
                        CloseDbConnection();
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        /* IGNORED */
                        CommitDbTransaction();
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message
                )
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        /* IGNORED */
                        MaybeWriteToDb(message, null, false);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Write(
                string message,
                string category
                )
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        /* IGNORED */
                        MaybeWriteToDb(message, category, false);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message
                )
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        /* IGNORED */
                        MaybeWriteToDb(message, null, true);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void WriteLine(
                string message,
                string category
                )
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        /* IGNORED */
                        MaybeWriteToDb(message, category, true);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override bool IsThreadSafe
            {
                get { CheckDisposed(); return true; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(
                        Interpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(DatabaseTraceListener).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            /* NO RESULT */
                            DebugOps.RemoveTraceListener(this);

                            ////////////////////////////////////

                            /* IGNORED */
                            CloseDbTransaction();

                            /* IGNORED */
                            CloseDbConnection();

                            ////////////////////////////////////

                            lock (syncRoot)
                            {
                                /* NOT OWNED */
                                interpreter = null;
                            }
                        }

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

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~DatabaseTraceListener()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region TraceListener Test Class
        [ObjectId("73634227-a942-4f0f-be47-1d395e1a9750")]
        public sealed class Listener : TraceListener
        {
            #region Private Constants
            //
            // HACK: These are purposely not marked as read-only.
            //
            private static Encoding DefaultEncoding = Encoding.UTF8;
            private static int DefaultBufferSize = 1024;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private Stream stream;
            private Encoding encoding;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private byte[] buffer;
            private bool expandBuffer;
            private bool zeroBuffer;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            internal Listener()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            internal Listener(
                string name /* in: may be NULL. */
                )
                : base(name)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public Listener(
                string name,       /* in: may be NULL. */
                string path,       /* in */
                Encoding encoding, /* in: may be NULL. */
                int bufferSize,    /* in: may be zero. */
                bool expandBuffer, /* in */
                bool zeroBuffer    /* in */
                )
                : this(name)
            {
                SetupStream(path); /* throw */
                SetupEncoding(encoding);

                ///////////////////////////////////////////////////////////////////////////////////////

                /* IGNORED */
                MaybeCreateOrZeroBuffer(bufferSize);

                ///////////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: Should the allocated buffer automatically expand
                //       to fit a request size?  If this is false, a brand
                //       new buffer will be allocated each time a request
                //       size cannot be satisfied by the existing buffer.
                //
                this.expandBuffer = expandBuffer;

                //
                // NOTE: Should the existing buffer always be zeroed before
                //       being returned?
                //
                this.zeroBuffer = zeroBuffer;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void SetupStream(
                string path
                )
            {
                CloseStream();

                stream = new FileStream(
                    path, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void FlushStream()
            {
                if (stream != null)
                    stream.Flush();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void CloseStream()
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void SetupEncoding(
                Encoding encoding
                )
            {
                if (encoding == null)
                    encoding = DefaultEncoding;

                this.encoding = encoding;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This method uses "strategy #1", use the existing buffer
            //       verbatim if it's already large enough; otherwise, replace
            //       the existing buffer with one of the larger size and then
            //       return it.
            //
            private byte[] MaybeCreateOrZeroBuffer(
                int bufferSize
                )
            {
                if (bufferSize <= 0)
                    bufferSize = DefaultBufferSize;

                if ((buffer == null) || (buffer.Length < bufferSize))
                    buffer = new byte[bufferSize];
                else if (zeroBuffer)
                    Array.Clear(buffer, 0, buffer.Length);

                return buffer;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This method uses "strategy #2", use the existing buffer
            //       verbatim if it's already large enough; otherwise, create
            //       a new buffer of the requested size and return it.
            //
            private byte[] GetOrCreateBuffer(
                int bufferSize
                )
            {
                if (bufferSize <= 0)
                    bufferSize = DefaultBufferSize;

                if ((buffer != null) && (bufferSize <= buffer.Length))
                {
                    if (zeroBuffer)
                        Array.Clear(buffer, 0, buffer.Length);

                    return buffer;
                }

                return new byte[bufferSize];
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Figure out which buffer management strategy to use and
            //       then return the appropriate buffer.
            //
            private byte[] GetBuffer(
                int bufferSize
                )
            {
                return expandBuffer ?
                    MaybeCreateOrZeroBuffer(bufferSize) :
                    GetOrCreateBuffer(bufferSize);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void GetWriteParameters(
                string message,
                ref byte[] buffer,
                ref int offset,
                ref int count
                )
            {
                if (encoding == null)
                    throw new InvalidOperationException();

                if (message == null)
                {
                    buffer = null; offset = 0; count = 0;
                    return;
                }

                int byteCount = encoding.GetByteCount(message);
                byte[] localBuffer = GetBuffer(byteCount);

                encoding.GetBytes(
                    message, 0, message.Length, localBuffer, 0);

                buffer = localBuffer; offset = 0; count = byteCount;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();

                CloseStream();
                base.Close();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                FlushStream();
                base.Flush();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message
                )
            {
                CheckDisposed();

                if (stream == null)
                    throw new InvalidOperationException();

                byte[] buffer = null; int offset = 0; int count = 0;

                GetWriteParameters(
                    message, ref buffer, ref offset, ref count);

                if ((buffer == null) || (count == 0))
                    return;

                stream.Write(buffer, offset, count);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message
                )
            {
                CheckDisposed();

                Write(message);
                Write(Environment.NewLine);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(Listener).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            /* NO RESULT */
                            DebugOps.RemoveTraceListener(this);

                            ////////////////////////////////////

                            Close();
                        }

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

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~Listener()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Test Class
        [ObjectId("4b0d4629-b42f-4cfa-adb6-5943e098961c")]
        public sealed class Disposable : IMaybeDisposed, IDisposable
        {
            #region Public Constructors
            public Disposable()
            {
                id = GlobalState.NextId(); /* EXEMPT */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IMaybeDisposed Members
            public bool Disposed
            {
                //
                // NOTE: *WARNING* Do not uncomment the CheckDisposed call here
                //       as that would defeat the purpose of this method and
                //       may interfere with the associated test cases.
                //
                get { /* CheckDisposed(); */ return disposed; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool Disposing
            {
                //
                // NOTE: *WARNING* Do not uncomment the CheckDisposed call here
                //       as that would defeat the purpose of this method and
                //       may interfere with the associated test cases.
                //
                get { /* CheckDisposed(); */ return disposing; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private long id;
            public long Id
            {
                get { CheckDisposed(); return id; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string name;
            public string Name
            {
                get { CheckDisposed(); return name; }
                set { CheckDisposed(); name = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                //
                // NOTE: *WARNING* Do not uncomment the CheckDisposed call here
                //       as that would defeat the purpose of this method and
                //       may interfere with the associated test cases.
                //
                // CheckDisposed();

                return String.Format(
                    "id = {0}, disposing = {1}, disposed = {2}",
                    id, disposing, disposed);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(Disposable).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool disposing;
            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                if (!disposed)
                {
                    //
                    // NOTE: Keep track of whether we were disposed via the
                    //       destructor (i.e. most likely via the GC) or
                    //       explicitly via the public Dispose method.
                    //
                    this.disposing = disposing;

                    //
                    // NOTE: This object is now disposed.  The test cases may
                    //       query the property associated with this field to
                    //       discover this fact.
                    //
                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~Disposable()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFunction Test Classes
        #region IFunction Test Class #1
        [ObjectId("a61370f5-215a-41a3-af8f-196fcf8f3cc4")]
        [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NoPopulate)]
        [Arguments(Arity.Unary)]
        [ObjectGroup("test")]
        public sealed class Function : _Functions.Default
        {
            #region Public Constructors
            public Function(
                IFunctionData functionData
                )
                : base(functionData)
            {
                this.Flags |= AttributeOps.GetFunctionFlags(GetType().BaseType) |
                    AttributeOps.GetFunctionFlags(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecuteArgument Members
            public override ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Argument value,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    error = "invalid argument list";
                    return ReturnCode.Error;
                }

                ReturnCode code;

                if (arguments.Count == 2)
                {
                    string text = arguments[1];
                    Result result = null;

                    code = Engine.EvaluateExpression(
                        interpreter, text, ref result);

                    if (code == ReturnCode.Ok)
                        value = StringList.MakeList(text, result);
                    else
                        error = result;
                }
                else
                {
                    error = ScriptOps.WrongNumberOfArguments(
                        this, 1, arguments, "expr");

                    code = ReturnCode.Error;
                }

                return code;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFunction Test Class #2
        [ObjectId("f7051cc9-57b1-4307-b4b6-1216811b9d39")]
        [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.NoPopulate)]
        [Arguments(Arity.Binary)]
        [ObjectGroup("test")]
        public sealed class Function2 : _Functions.Default
        {
            #region Public Constructors
            public Function2(
                IFunctionData functionData
                )
                : base(functionData)
            {
                this.Flags |= AttributeOps.GetFunctionFlags(GetType().BaseType) |
                    AttributeOps.GetFunctionFlags(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecuteArgument Members
            public override ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Argument value,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    error = "invalid argument list";
                    return ReturnCode.Error;
                }

                if (arguments.Count != (this.Arguments + 1)) /* 3 */
                {
                    error = ScriptOps.WrongNumberOfArguments(
                        this, 1, arguments, "bool expr");

                    return ReturnCode.Error;
                }

                ReturnCode code = ReturnCode.Ok;

                bool boolValue = false;

                if (Engine.ToBoolean(
                        arguments[1], interpreter.InternalCultureInfo,
                        ref boolValue, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (boolValue)
                {
                    try
                    {
                        //
                        // NOTE: This is being done purely for expression
                        //       engine testing purposes only.  Normally,
                        //       the containing interpreter should NOT be
                        //       disposed from inside of a custom command
                        //       or function.
                        //
                        if (interpreter != null)
                        {
                            interpreter.Dispose();
                            interpreter = null;
                        }

                        code = ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }

                string text = arguments[2];

                if ((code == ReturnCode.Ok) && !String.IsNullOrEmpty(text))
                {
                    Result result = null;

                    code = Engine.EvaluateExpression(
                        interpreter, text, ref result);

                    if (code == ReturnCode.Ok)
                        value = StringList.MakeList(text, result);
                    else
                        error = result;
                }

                return code;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFunction Test Class #3
        [ObjectId("c06da159-fe1f-4482-85ec-1b4c3bf4d0d0")]
        [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.NoPopulate)]
        [Arguments(Arity.None)]
        [ObjectGroup("test")]
        public sealed class Function3 : _Functions.Default
        {
            #region Public Constructors
            public Function3(
                IFunctionData functionData
                )
                : base(functionData)
            {
                this.Flags |= AttributeOps.GetFunctionFlags(GetType().BaseType) |
                    AttributeOps.GetFunctionFlags(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecuteArgument Members
            public override ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Argument value,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    error = "invalid argument list";
                    return ReturnCode.Error;
                }

                if (arguments.Count < 3)
                {
                    error = ScriptOps.WrongNumberOfArguments(
                        this, 1, arguments, "bool arg ?arg ...?");

                    return ReturnCode.Error;
                }

                bool boolValue = false;

                if (Engine.ToBoolean(
                        arguments[1], interpreter.InternalCultureInfo,
                        ref boolValue, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                ReturnCode code;

                if (boolValue)
                {
                    Result result = null;

                    code = interpreter.Invoke(arguments[2], clientData,
                        ArgumentList.GetRange(arguments, 2), ref result);

                    if (code == ReturnCode.Ok)
                        value = result;
                    else
                        error = result;
                }
                else
                {
                    value = ArgumentList.GetRange(
                        arguments, 1, Index.Invalid, false).ToString();

                    code = ReturnCode.Ok;
                }

                return code;
            }
            #endregion
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Generic List Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("df724c7b-bf7a-4292-8223-c2350b6f3dc2")]
        public sealed class GenericList<T> : List<T>
        {
            #region Public Constructors
            public GenericList()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericList(
                IEnumerable<T> collection
                )
                : base(collection)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericList(
                params T[] elements
                )
                : base(elements)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ToString Methods
            public string ToString(
                string pattern,
                bool noCase
                )
            {
                return ParserOps<T>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), pattern, noCase);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return ToString(null, false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Generic Dictionary Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("51e22e50-4079-48f6-83eb-e48cb49c7ea3")]
        public sealed class GenericDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            #region Public Constructors
            public GenericDictionary()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericDictionary(
                IEnumerable<TKey> collection
                )
                : this(collection, null)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericDictionary(
                params TKey[] elements
                )
                : this((IEnumerable<TKey>)elements)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericDictionary(
                IEnumerable<TKey> keys,
                IEnumerable<TValue> values
                )
                : this()
            {
                Add(keys, values);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Constructors
#if SERIALIZATION
            private GenericDictionary(
                SerializationInfo info,
                StreamingContext context
                )
                : base(info, context)
            {
                // do nothing.
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Add Methods
            public void Add(
                IEnumerable<TKey> keys,
                IEnumerable<TValue> values
                )
            {
                if (keys == null)
                    return;

                IEnumerator<TKey> keyEnumerator = keys.GetEnumerator();

                IEnumerator<TValue> valueEnumerator = (values != null) ?
                    values.GetEnumerator() : null;

                bool moreValues = (valueEnumerator != null);

                while (keyEnumerator.MoveNext())
                {
                    TKey key = keyEnumerator.Current;
                    TValue value = default(TValue);

                    if (moreValues)
                    {
                        if (valueEnumerator.MoveNext())
                            value = valueEnumerator.Current;
                        else
                            moreValues = false;
                    }

                    this.Add(key, value);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ToString Methods
            public string ToString(
                string pattern,
                bool noCase
                )
            {
                return GenericOps<TKey, TValue>.DictionaryToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), pattern, noCase);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return ToString(null, false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Derived List Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("99cda455-3f92-4ec7-bf6e-a406c76ddbf5")]
        public sealed class DerivedList : List<DerivedList>
        {
            #region Public Constructors
            public DerivedList()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public DerivedList(
                IEnumerable<DerivedList> collection
                )
                : base(collection)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public DerivedList(
                params DerivedList[] elements
                )
                : base(elements)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ToString Methods
            public string ToString(
                string pattern,
                bool noCase
                )
            {
                return ParserOps<DerivedList>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), pattern, noCase);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return ToString(null, false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Remote Authorizer Class (Conditional)
#if NETWORK && REMOTING
        [ObjectId("008a9889-79c4-4a75-af68-1bbb56a349b3")]
        public sealed class RemoteAuthorizer : IAuthorizeRemotingConnection
        {
            #region IAuthorizeRemotingConnection Members
            public bool IsConnectingEndPointAuthorized(EndPoint endPoint)
            {
                IPEndPoint ipEndPoint = endPoint as IPEndPoint;

                if ((ipEndPoint != null) &&
                    (ipEndPoint.Address != null) &&
                    (ipEndPoint.Address.Equals(IPAddress.Loopback)))
                {
                    return true;
                }

                return false;
            }
            ///////////////////////////////////////////////////////////////////////////////////////////////

            public bool IsConnectingIdentityAuthorized(
                IIdentity identity
                )
            {
                return true;
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Remote Object Class
        [ObjectId("2d6c5daa-884a-4ee5-a654-20b70cee463f")]
        public sealed class RemoteObject : MarshalByRefObject, IDisposable
        {
            #region Public Methods (Remotely Accessible)
            public DateTime Now()
            {
                CheckDisposed();

                return TimeOps.GetUtcNow();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool Exit()
            {
                CheckDisposed();

#if REMOTING
                try
                {
                    TcpServerChannel channel = ChannelServices.GetChannel(
                        RemotingChannelName) as TcpServerChannel;

                    if (channel != null)
                    {
                        if (RemotingServices.Disconnect(this))
                        {
                            channel.StopListening(null);
                            ChannelServices.UnregisterChannel(channel);

                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RemoteObject).Name,
                        TracePriority.RemotingError);
                }
#endif

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode Evaluate(
                string text,
                EngineFlags engineFlags,
                SubstitutionFlags substitutionFlags,
                EventFlags eventFlags,
                ExpressionFlags expressionFlags,
                ref Result result
                )
            {
                CheckDisposed();

                return Engine.EvaluateScript(
                    Interpreter.GetAny(), text,
                    engineFlags, substitutionFlags,
                    eventFlags, expressionFlags,
                    ref result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            //
            // HACK: We can never unset the variable that contains the transparent
            //       proxy reference unless this class implements IDisposable because
            //       the transparent proxy "pretends" to implement it, thereby fooling
            //       our TryDisposeObject function.
            //
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(RemoteObject).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(bool disposing)
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

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~RemoteObject()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptStringTransformCallback Test Class
        [ObjectId("0a94e612-1327-4c50-b217-151c779b4f5c")]
        public sealed class ScriptStringTransformCallback
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            : ScriptMarshalByRefObject, IStringTransformCallback
#endif
        {
            #region Private Data
            private StringTransformCallback callback;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ScriptStringTransformCallback(
                StringTransformCallback callback /* in */
                )
            {
                this.callback = callback;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IStringTransformCallback Members
            public string StringTransform(
                string value /* in */
                )
            {
                CheckDisposed();

                if (callback == null)
                    throw new ArgumentNullException("callback");

                return callback(value);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new InterpreterDisposedException(
                        typeof(ScriptStringTransformCallback));
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(
                bool disposing /* in */
                )
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptStringTransformCallback()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInterpreter Support Methods
#if THREADING
        public static object TestGetEngineContext(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.GetEngineContextNoCreate();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IClientData TestGetContextClientData(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.ContextClientData;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetContextClientData(
            Interpreter interpreter, /* in */
            IClientData clientData   /* in */
            )
        {
            if (interpreter == null)
                return;

            interpreter.ContextClientData = clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPlugin Support Methods
        public static void TestInitializeStrings(
            bool force /* in */
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (strings != null)
                {
                    if (!force)
                        return;
                }
                else
                {
                    strings = new StringDictionary();
                }

                ///////////////////////////////////////////////////////////////////////////////////////

#if XML
                string key = TestGetScriptStreamXmlDataHash();

                if (key != null)
                {
                    strings[String.Format("0x{0}", key)] =
                        TestGetScriptStreamXmlData();

                    strings[String.Format("0x{0}.harpy", key)] =
                        TestGetScriptStreamXmlSignature();

                    strings[String.Format("0x{0}_block1_signature", key)] =
                        TestGetScriptStreamXmlBlock1Signature();

                    strings[String.Format("0x{0}_block2_signature", key)] =
                        TestGetScriptStreamXmlBlock2Signature();
                }
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestResetStrings()
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (strings != null)
                {
                    strings.Clear();
                    strings = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestChangeString(
            string name,
            string value,
            ref Result result
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (strings == null)
                {
                    result = "named strings unavailable";
                    return ReturnCode.Error;
                }

                if (name == null)
                {
                    result = "invalid string name";
                    return ReturnCode.Error;
                }

                if (strings.ContainsKey(name))
                {
                    strings[name] = value;
                    result = "changed";
                }
                else
                {
                    strings.Add(name, value);
                    result = "added";
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRemoveString(
            string name,
            ref Result result
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (strings == null)
                {
                    result = "named strings unavailable";
                    return ReturnCode.Error;
                }

                if (name == null)
                {
                    result = "invalid string name";
                    return ReturnCode.Error;
                }

                if (strings.Remove(name))
                {
                    result = "removed";
                    return ReturnCode.Ok;
                }
                else
                {
                    result = String.Format(
                        "missing named string {0}",
                        FormatOps.WrapOrNull(name));

                    return ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetString(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (name != null)
            {
                lock (staticSyncRoot) /* TRANSACTIONAL */
                {
                    string value;

                    if ((strings != null) &&
                        strings.TryGetValue(name, out value))
                    {
                        return value; /* MAY BE NULL */
                    }
                }
            }

            error = String.Format(
                "type {0} has no string named {1}",
                FormatOps.TypeName(typeof(Default)),
                FormatOps.WrapOrNull(name));

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Conditional Public Static Methods
#if XML
        public static ReturnCode TestReadScriptXml(
            Interpreter interpreter,          /* in */
            Encoding encoding,                /* in */
            string xml,                       /* in */
            ref IClientData clientData,       /* in, out */
            ref IEnumerable<IScript> scripts, /* out */
            ref string text,                  /* out */
            ref Result error                  /* out */
            )
        {
            XmlErrorTypes retryXml = XmlErrorTypes.None;
            bool validateXml = false;
            bool relaxedXml = false;
            bool allXml = false;

            if (interpreter != null)
            {
                interpreter.QueryXmlProperties(
                    ref retryXml, ref validateXml,
                    ref relaxedXml, ref allXml);
            }

            bool canRetry = false; /* NOT USED */

            return TestReadScriptXml(
                interpreter, encoding, xml, retryXml, validateXml,
                relaxedXml, allXml, ref clientData, ref scripts,
                ref text, ref canRetry, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadScriptXml(
            Interpreter interpreter,          /* in */
            Encoding encoding,                /* in */
            string xml,                       /* in */
            XmlErrorTypes retryTypes,         /* in */
            bool validate,                    /* in */
            bool relaxed,                     /* in */
            bool all,                         /* in */
            ref IClientData clientData,       /* in, out */
            ref IEnumerable<IScript> scripts, /* out */
            ref string text,                  /* out */
            ref bool canRetry,                /* out */
            ref Result error                  /* out */
            )
        {
            EngineFlags engineFlags = EngineFlags.None;
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags = interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            return Engine.ReadScriptXml(
                interpreter, encoding, xml, retryTypes, validate, relaxed,
                all, ref engineFlags, ref substitutionFlags, ref eventFlags,
                ref expressionFlags, ref clientData, ref scripts, ref text,
                ref canRetry, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadFromXmlFile(
            string fileName,                     /* in */
            ref NameValueCollection appSettings, /* in, out */
            ref Result error                     /* out */
            )
        {
            NameValueCollection localAppSettings;
            Result localError = null;

            localAppSettings = ConfigurationOps.ReadFromXmlFile(
                fileName, ref localError);

            if (localAppSettings == null)
            {
                error = localError;
                return ReturnCode.Error;
            }

            if (appSettings != null)
            {
                ConfigurationOps.MergeAppSettings(
                    ref appSettings, localAppSettings, true, false);
            }
            else
            {
                appSettings = localAppSettings;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestWriteSettingsToXmlFile(
            string fileName,                 /* in */
            NameValueCollection appSettings, /* in */
            ref Result error                 /* out */
            )
        {
            return ConfigurationOps.WriteToXmlFile(
                fileName, appSettings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetScriptStreamXmlBlock1Signature() /* block1.eagle.harpy */
        {
            return String.Format(
                "HunSSBBQ38IuKr7knDLzdXDYruAjoURdsjF/axLGqjiEnSHk7FIdUSeIwYMx/qFsLAuEZ/lbJfw9{0}" +
                "gZ5IbqEyN+uZAK1RZf9Kb+25aLVmpJ15cFKtV0D19NZNXo+GEW+I0gXI5TkH6kgRs9oT2XE0Iwl5{0}" +
                "o97fHmSI6KtpFQqA+GV3XaUVdLw/wu8dmRNY0pFCxrr03/vfmCkqgjBtM5qipVKWc2fID14BxKOg{0}" +
                "e41YFTwW9/cSxejb4kd8gdFAwHDVma0zD1rzU45xtBlGC0n/hZkkFwWBemvID3V3yNe4tEX3Lrq4{0}" +
                "lHn+lf3/mLDaC8iqaR99F1YHyv2dH0iGLUYFDGaqR3R26peL1AVzJfT/iWm1lH+ohuzZw/0NvL7Z{0}" +
                "qaSVrkcnQ1eC6mZCOVVxOJzKJWUiv+K44wvOHeFFBWE5XIX62XNBVWA62sXtCLxdZOL5foJiXCsr{0}" +
                "0j94KNyjlsHvghqJ1f7UyDU6L8QGj4XBC0GywVU7VI5dfRGSyVjP3xpG01b9Svlwt5fj/9pzLp2Q{0}" +
                "c7DKBIF6JpbUUHaYpuauzYFDJ7spM5R8SdngvOMZILMQqwPIdgzBAghJtt7EJvKf0ubYOnhopYwJ{0}" +
                "lR6vWFpbkSsrQB5xDkquRauVjOeDug2bab3ZoQAJ5JcvnziL+e0eGxbxbdcNC7FDHxjWX1oybjnf{0}" +
                "AnwlJfwxd8YcRVfe2ZpFIMXzvqaIZRhkTdN/45WkxYCtJ8XMid51XPneOfbCWvIQLDI918TqS/QC{0}" +
                "hvzPhfbknJ2V0UwB6gOr3V9tZBJeeQu/8d2MqXS2JJD+GsuMSfngyIh6t853vDLHjQdkDGqo+uM9{0}" +
                "wBde/NmxHvvqmpJoA454WViafNrWGMhNuZbaNLYFa6zF23UTEAP0L2agke24XF9uo+6+4kJ+vB8i{0}" +
                "qfmsuFvw2kaSUHqYlaY7YvtiEOYY0ML+wyrKeLTMh5FXDvSnsHaLdGB9G+Ykmh8rQe8EBBzIxhDV{0}" +
                "qDLGg6orpMBQDoh0uK0kc/Xfj6fji9d10NQRMY7+9qypr6WKjM1orHjHJCi+rX2YdcCAhrMyqv3T{0}" +
                "47Hg1iR6zXRGrcWdh8RLK/vXF37Sd14mZb04ntv/ZM+vYyqhbrjG/ipO8t1B+t1ELiOiKdJbuBfw{0}" +
                "HJM0Kc04MskWD/wkufBdcAQ8gPhkrKzNp4sTQNh/xttMbqqNx1zyW6z+eCWBOOKc1MEzCVIvdKB4{0}" +
                "kTARKFU/VWuzb28O4Fpk3MVyxjAJYFnMWLYdwDKYuw9ZbvyuHM4iHj8CcndYq9L9GeqjFgHkkkWL{0}" +
                "iaeu+sWlgOtNWzg9JLJXkV3cbeywJ/3Z/P0qdfU1sNN/kSoxhI6zqaJ9fGIiU1VmdqxsGLRhycIC{0}" +
                "XunUDt+lE5FzLK/o7bgTrnZ2zP2qPsQ1o2ZikfIpPUGpdscraL3VqQr2LSwTNJfzvYLgmC+BqwI+{0}" +
                "EaDRyiNv2p+06fKjXN0+lfxZ2hK8FiHzzgaBkOcgXVryIt9MF5H2RQFVDaOHzsgE88CFgwfWU9Bi{0}" +
                "moepDeVGA+d+gdZtg71NcTyqwLSqRxxWuz+5JiPsU0J0xr8L0EHSWqbRqhcBNNhhPTVb/X4SnkV3{0}" +
                "61igPu+QxrOqZ5Oatv/DstWP4Qo8rZx73Et7f8XyQO1fCwuDe7krpfWIGN2YU8q4WvYIP3qsX3zb{0}" +
                "7skQvJTkeA+u0y4sxwDR9f/YOsgRp2p/g7hefvpNCahaLuEmj+dt0rLkXt1N/gLcyEGAH1khkBlO{0}" +
                "DHB0I4uTRAV3o4lxJocvSNvMTdfdk2XMeWn9R4a+BhsdlJzCPKn+UH+ziwj8HoGB41hsld6EhphW{0}" +
                "mV84WiFJ5m1G8xtoNe9Hj9rWtfiVKwxDhi48dJENgoYhJImILeGLDTr1ZBjGHJNozfp+pGZCDoRZ{0}" +
                "mX/CWWtvFiUFeH/bja3mJAyiCNtHNlC9499lXW4laHHQf8LV2RJsMbiWQnwBhaGzzzhVDLLXoIh9{0}" +
                "o+8thTZazUKxL/I96GDnSdCvU4GkpBxjMH6VodWsbz9Txss1YxNYM+w5Em10fJmC5N69QE1SbvIq{0}" +
                "GalDgpD3Ywt66gCnnfG0aldoKvxlJF1WiSOT/k2MYg+NWpgAndwO1KDlD0AepNlvgOCYpnlxebmU{0}" +
                "AUm0LxdKfXEz9XxmCNh7MInuFwU2ewRvFDLyZ7CVaw16bl9y0b2BzlusbICqLVkWAhtot375AmxV{0}" +
                "aQJ6UjsvSRoBIBJV9y18GAcT653HMZ+8yQSf52qgMIDvf2boeiqe9R91wVl4RClxo7RQm7kYKGru{0}" +
                "BE4BHxjW1hw247dtFn+YsWnw0Fm/+/i9ekNX3xW2L53zIM93foV+0BtEij2424ZoYaWKVFvphl4u{0}" +
                "KiaxUIJnQM08f4qhtYH0AUjwTrZ8St9ibEzM9R+/LfHWlCFHduMEEi8q9rtDXW04f1y2BQefrFjO{0}" +
                "Xe7SB4CWXda5V0L3RtpnE7dTXzwU3+/LU6dDkuAmc3DUTcX/70mlAjlx81qDs1Ay0/yvvBgR3RCQ{0}" +
                "m/ZqBgF4HrEidDnRCrxqljsv2V1Gu14LdSFvuNSfokXuutdR/ZQFahmU0bjk0p2t9yQZaj1kdcMN{0}" +
                "ZJk17738aLUtGEg2GsKwegEIAc47VsJRU3av2sraLOZi46wZa+SIFnfAdXH7cjHouAunA8AFuhZ/{0}" +
                "+VsxxRatNRBiSQvv8Pk1llchzwfgk04Yynxn7dlnnyKUWnOz+af6DVN8eX6dVzqHYD+fRlU={0}",
                Characters.DosNewLine
            );
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetScriptStreamXmlBlock2Signature() /* block2.eagle.harpy */
        {
            return String.Format(
                "ikuyqUlmyy9N6B3eP+mRyzlGoivcSvU4G79o2fkVApwsfg2gSlG/maGwrXPNEbBcznJVIzcZeYSH{0}" +
                "3hC+jtMKVWbKhfRZ9sN583g0XMVjgcbmAIdizyS2eKosQ9S31fBh6ib+ZdRptnfn6G8IJttmDDlH{0}" +
                "hqSdQO5tP7qwMj1QExqYCAkxO3ejFs1w6lwojXw7K57TgqmgF1l8G9DYH0A+uHZgIKXvjPqlSi36{0}" +
                "3LLbU37O8mGMIH6NBMR19BaoaMJTI8zb/Od6yVFii8wsYrtZs8occggXihtkezLRGOFN2KqrBPu4{0}" +
                "6pzj8PhKScsW/c9HAPObslyuPd1jm2o4g6tSBC+zl4DckEJZQkZiKe3ZgNubixqwCCQ3G6xivQ0y{0}" +
                "ZbLuhwzmZJGZ7ll3RdnrDuZfUioy96wRp3Cie4+xJtKyXTiYH3R5cL6Xgyz2YnFlZfXY1FkXzyVt{0}" +
                "BlFmF+9iXq3mGuOcu3WqeuuSbvyHcje7t0wSrYwRcAYpq7RpcrxhT+LD11FNdzG5fYc3DrR9+FZ+{0}" +
                "y6xoGKAxEk7LDWRc7ZRU+2QOSRKW9GKcw1ZEtfDchX1qCt2NeGD4ZmIVuaQJkfWGCtNNJvTIxpo3{0}" +
                "gdK3JQvOM4+ZyiyqoSQAMsXCeAXVdhfVTgsNAdOwnBV/dZcxEo5DVqFIa3MkyGaKZ+BnA7SKx4kf{0}" +
                "yMoxdb9YVf3T4JFKp11Hdp2/mcyOl8wRwtbFWecQ/ROrCTVeWXQ1QfROWynjOWST/Vt66JCRizsN{0}" +
                "AAR8KEQD0epmvPDy2mqzo+VP49jVpXdw5bxHjKNCV/25owInvdHx+Xfw101sovM2yHB5NVJbMfbf{0}" +
                "Ro1udHzEX6Vmyi3RnR8rAsL2C1ennSpGJwwESMBGk9xTFyI512fl/5lRG0HCZNpUdn+LJhnj4RZ6{0}" +
                "mgb9nB3AcenjOSr/nrA+vLZp8oEk8D6ykP9XJzsjMc2Dgj0WNlVuF3DXXby1Y9eG00/KXzF0DZwp{0}" +
                "3fgEqKMgFMqvOc0NVpSXd2G99SUjnXzVLyYuRsPKaAXtIm/je5fH1MEa2eCHlybKrlLwHsHkl1c+{0}" +
                "aTCT4TQnd3ElHXtp3ogv2dOrL0KIW8i/O4rUW5E7hD337W9wQI/QCTKe1TszIR+AY0NXSNizDbWT{0}" +
                "Vudt8nZj5+NI/cJMcISpGHOIfOc2h1mSMVhcr8gcnl0ezdkywsrPG1BcS64x9br747MIKs9dKxZu{0}" +
                "HGs/KTZu2FQGwOT9shgEfbUAK68X3Z9E7dc3MHgoDieCfEPshA47JMyxEAPlZ7VINagmjire5L72{0}" +
                "ky1E4lUmm9mwqc9QCWwHN6X3ZEmj0nMDXi5hnMTSWN/W35l0i9E5nLgy+teHyqHJ+UZSGA+UO4K1{0}" +
                "kD0YHhu1RYrpzZVO7PMF1X3tzrJMyMMAk+MrRKdExPke+TBNSV6uD2VGYAqPjODxTukW2F2EXeGH{0}" +
                "zyJ65wtH8+9ke7+J2TkLQMNI4dD/xGlZz1/DoKCCUh0AU25Tv7/bjDkiN7AzXYgOla7USQGRx/mq{0}" +
                "AglU4QWPzJJlYlga0OJ98eVp5diGex2dXlKBd2Z0TxBlchU2ll0PmZf7+0IDdoLKy0AMW4do/IVl{0}" +
                "GnhxJfKDJkXDDWhfjL+7ehxJq7tXz4oZ5P8FKLmeaCsp77Tuo+RB2BM2a+W167VPXB4WKyiwWMQx{0}" +
                "Bx4Scb/44CZt3VEcw+wTCAs1gw0dX3fa0ddtJjNZeP1O9125cezxsKEdEwbMmkOMR33XfHbPKfzL{0}" +
                "U+ULRTnmBWz3usXeMGmw8mTRyx6hHFOp43VzxKl2+bdjL4PraTFhlMqL4PDMTMbcpJ5i5JohWdeq{0}" +
                "krvPKU6VxqaNn5a6uylV3IlY0l/LYYvn8PuttejtIc63ro6+ajSqeinBS0D8e/eK9bb+kHEQP4nU{0}" +
                "JDYRTc5p7fHWp7L5PzTzR1aB2j3G73mzBC3WOwUKhG9G6zOBDu4kh9CNhCLZJYMHFwz9mlsYcFjS{0}" +
                "RbXUY6eXOFyAv362o+H3sJUENNilGXo3YsEleEp5blahTcfhqiUf+G27BmOncJ6V5F8pBOCxC/3b{0}" +
                "5XH+aivDO0wTk9b8yB6Bulm2vTrvbuu4dU9BSn5t1wW1wQL6WvQl75Dr/QI2yhv8UAOrtUd203Ca{0}" +
                "5BhBHUsoGhR2Qa0fuB7LO2f12P8zVGGJFZAg0C8iZ2WMmX2aawh6yTHKMZvI2DhucbDmE/H4D6NJ{0}" +
                "QWeyx2VNUc/F4i3OgPPctd5Qgo7uEMveahr33MOnXIHXaJ2QISKpK2cJmrcI9kM/m76gd+BccxCq{0}" +
                "Y/Ms43jFKZSBVM+4WvHpkDbmKCA/qhlmrT7+5MLXXfu9ujqfXLbYhnW0vD+SC1OF99BriXIz5yuS{0}" +
                "rNRoAYjSczKXxh4mfh3OgIjnOABSb3hUPS7wU3uhw1CXF5GvbkP7GMrJJdlPNoTPkJ/tmx8JEvwr{0}" +
                "tK2zIuP2lS5qU+d8AYiSaPb5ufvR3rFIr7MBFHMeyRQMIGjcVyDyz8RvC9NjLaegkA+gdnj0sOiR{0}" +
                "Dm6CCwJFZVQYyO1vN3byL50zDBENoFvlRAURKt8QFszq4pvzZ+PQryqvpi3V/4cQTeIiz3DegHSu{0}" +
                "tYDALnv/FaP5f/0ayZR2k/OGCkURAxv5CXegX+QfWz9f+gQOjLC9i47vHstsLqO6JdccbM7mKXII{0}" +
                "hBsOrrlyDsV/1OYtNgegolW84pbrKprmk55pkfJWZcsX5d2L0K98wQs9/vqk/YMpdBUbdOk={0}",
                Characters.DosNewLine
            );
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetScriptStreamXmlDataHash() /* 546217657d203f9e263ad37618c1509b304a03a8 */
        {
            return ArrayOps.ToHexadecimalString(
                HashOps.HashString("SHA1", (string)null, TestGetScriptStreamXmlData()));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetScriptStreamXmlData() /* TestScriptStreamXml.xml */
        {
            //
            // WARNING: For the signature in TestGetScriptStreamXmlSignature
            //          (below) to match, this content must have an SHA1 hash
            //          matching return value of TestGetScriptStreamXmlHash.
            //
            return String.Format(
                "<?xml version=\"1.0\" encoding=\"utf-8\" ?>{0}" +
                "<blocks xmlns=\"{3}\">{0}" +
                "  <block id=\"11b0bd2a-bc99-4639-9727-dc38efacaca1\" " +
                "type=\"automatic\" name=\"block1\" publicKeyToken=\"{6}\" " +
                "signature=\"{4}\">{0}" +
                "    <![CDATA[{0}" +
                "      set seconds [clock seconds]{0}" +
                "      lappend seconds \"clock time = $seconds\"{0}" +
                "      lappend seconds [expr {1}$seconds > 0 ? $seconds : \"negative\"{2}]{0}" +
                "    ]]>{0}" +
                "  </block>{0}" +
                "  <block id=\"07a3570a-49ab-45d7-9e1a-893b2c61edff\" " +
                "type=\"automatic\" name=\"block2\" publicKeyToken=\"{6}\" " +
                "signature=\"{5}\">{0}" +
                "    return [list {1}this is a test.{2} $seconds [clock seconds]]{0}" +
                "  </block>{0}" +
                "</blocks>{0}" + Characters.EndOfFile + "this is not valid XML",
                Characters.DosNewLine, Characters.OpenBrace, Characters.CloseBrace,
                Xml.ScriptNamespaceUri, TestGetScriptStreamXmlBlock1Signature(),
                TestGetScriptStreamXmlBlock2Signature(), PublicKeyToken.Class0
            );
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetScriptStreamXmlSignature() /* TestScriptStreamXml.xml.harpy */
        {
            return String.Format(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>{0}" +
                "<Certificate xmlns=\"{1}\"{0}" +
                "             xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"{0}" +
                "             xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">{0}" +
                "  <Protocol>None</Protocol>{0}" +
                "  <Vendor>Mistachkin Systems</Vendor>{0}" +
                "  <Id>7ef95cfe-37ee-41dd-a30c-bc342683d96d</Id>{0}" +
                "  <HashAlgorithm>SHA512</HashAlgorithm>{0}" +
                "  <EntityType>Script</EntityType>{0}" +
                "  <TimeStamp>2020-11-20T02:16:24.9461003Z</TimeStamp>{0}" +
                "  <Duration>-1.00:00:00</Duration>{0}" +
                "  <Key>0x{2}</Key>{0}" +
                "  <Signature>{0}" +
                "    ng1bWj3KmBPctC93cPGs3LfWB1jSSP8snIsrvCb0PVTe1Dyvr5DPrWbZN285cYEUSwmw7o4qJf1P{0}" +
                "    kpJPpHTwA5J1Jduyys/lb+AG2IxTpaDIJAwCwFl3FZdmNJkCMERv4ddQiyhIVXhKv3k2BNwgtI1A{0}" +
                "    XLXfuO6pOwbuP0OmUPIR202JIPJg2gr8GpTEz6Du8l8+4HOtfg3KO4fOpM08U8T/idTyLDBxaNJ/{0}" +
                "    ipCEn47/kNkZtykyC56Jf/BZIysFs4Hksu/hmxAw1dBY37eC2cjGvepw4YtYuF474qsHkpUVRhk3{0}" +
                "    HQXwMaJeHOCEHd/L/rMoIQWlpyoKCz0WEO3xh9AqIwM4jZX8cl5v5gvYYAUX8BSVdKPMwrGi+pbd{0}" +
                "    5O6zHVvJszthM+t/WW5sKtpOBaIrYaxYlpsUQbqaCRWookYpg7GOshJ41+KFvto+bWr3q1+Q0E2v{0}" +
                "    jjripJOmvj4znVlUZKWsvyPwBaJW8bfw0bObhmKiVcimT2+QsOpNPz1vbYJHwp4UiXOh7tyROEQx{0}" +
                "    PUmJF8rMtUa6qvYHM9wb3y8dPeV2AN1OgGNTv1RgLzlPTJIcrSD4/r9+MnIhnrmrvlrs9dvzOFXL{0}" +
                "    ZzBCH6hYoO6lVMBBKd5fP/0Vy/IcMGGx6HBOMEnJ/Tp7NFxLsWobmYwiWTTww6u8haoluoX+7BkR{0}" +
                "    b9OjjwItYU4FyXgu1bdivC3JfqTj+PwNygbIUdCXDdbFiZJRa+zTRZNInKQ3AyAwQebWprrtSz0I{0}" +
                "    L9ZbfpgcaT2WXoa8GcSwnE6Q1Q9ljVn23w6VCsVzYfUh0qkMsr/AT/NrV3Pmo+/KOxvHNt6s7YCw{0}" +
                "    o+YmXdpHtnLXfQ5s02fgULSwDRvNBCIZsy7yWFiBOhajsDnrfAnNremK1sQLBUvhrlbazi67jXlR{0}" +
                "    BI6NVc25yQa5VJxWZsQ/zn2KZp0ZRtuDG3FHcBtt43WRKDVRiNcsoOrk3n2Wc/+Tn5YJDudJctZj{0}" +
                "    4d/Wu/LHMircF69ABA2mN8E3Jc3Q0a5OAsKBbgg1Jzk9H5GwMlCK9GARD0uL0jVoQBTrSXwpSdst{0}" +
                "    gY1h4VAjYsXiwdkrfczoTIBKz1xoVJbCbTHA20hDuqSiD9NjZvMD1x2er5m3OoUtPgssZob9ukKX{0}" +
                "    4JILQwO3qVzuxRusqUJlMD32Zz5Sr0h0DThbFdWuRZmveuCiOo6gNtwfXCq5v1U/XeAfjzwtP9WF{0}" +
                "    SbCZMpGj7P+PvJF0LGCMdeh6enl9UVFRGcl/LEoA+3CyJGad+d2Ff+FfrY2xyrY/wkjPLjxNXH0y{0}" +
                "    3gnkSMyjYSZcHbaSlb73eLmh8HdZ4P5lNENh7Hoi7hmh0KRJWKLvwKCtYaJmRmMwg9xhmg//NlwB{0}" +
                "    0sZxUNYOVrbmYlSyLf+RfmfijWdB68k+yay54BENMSeYAPIJwvlS4rb5zCZKlh2CU2aFPM1Q0bu5{0}" +
                "    NyGoevEwtDzUHABhaJNgcztZM68vbfewnneYlpiMq7cm5LKks/7Egu7kPWQLwMWGDQpoI4q0VqdK{0}" +
                "    CVRh3kq3zgPtmSPTORwpMOAbwfHoA8YWQRV6gXIjyH7LptJ8DmwA3ARp8vlfQqSJQoo9iAjLVwgl{0}" +
                "    2MFqcW3UiLY7YJHyGDxBrQSMdSLig3YbMP8ixGTQO/cONnXjfHiAE/mSx/G/DM/3nWah3NUOl7Lz{0}" +
                "    +yd+m1UrdJom1J59l6l8UjLA87/erODmywQsJUbMW56wgYU9auFEJGqT5WZ8N6RGemmUaOwukUE/{0}" +
                "    hyo3WqvrPLMh/TzBxKfL6ucRMzPMI3agm1dCCI1YffTgzwqxMPs+X6Y/kY9gHE6PdEe4l9qSrUie{0}" +
                "    3Essr2a+jVC7bBx4lWoLnh8BcI4mpI2b0W+sK2lPi2aw4Qh/rMvC8RWQ5HNfJum882lYTfI4Mqp6{0}" +
                "    mRAeJiKsWspR3cdAWhxd1VD/MGbMMqGMqFgecu1g9R/+d24ZaRad9JfvyS4VNyv1zBo9swIDi5fh{0}" +
                "    3Qxit1Yl1NNPjITB5jANZaKKs9Aha42s8Nu9TYNQJoTiDXb78ENe55Yzj8nUcyk6cgHip9bHKE6Y{0}" +
                "    DSpmVcCJQOjrl8XEQ91IZAT7IQBZ2Nr8MYFzOuVu5sE+VRWtLgGuEI2PZ4VSYtem9rob394rIgAF{0}" +
                "    7h8LGvdV0aGLpobf+w5c1D2s2aj7hNbMYNgU/UoD6E45dK1u7gcOCK2/fFjh+cn96MYu2hChHxmD{0}" +
                "    fHZfw3+nfuzCag0xnDIdZ2YhC8FPlTgeF5vXdaQ9DYpXJVGEPhTdXwUOMhcARq6LotIuHr5xC0le{0}" +
                "    PIxD0dWezGvnLY8TDzpbTAP4v7+STPpBnqm6PsMVe7QaigGq/IJwsBplZf/bMoSWZh0hG3IzPNb4{0}" +
                "    vxCdg5yG0P8fxM4GP+OmuP5rwnZeKXjH1S8j4aSy562GkKrWr0kRZjbt+d4t10WzQpIYlF6ztsqy{0}" +
                "    PK/3gsDk7745s7ZpmAiiw8tdubGSxTPZ7WoGqjsNrdDutWBWKQs55yqn5/WaBu2PNBwJbxKBAGM+{0}" +
                "    pNrt1/ymiaxn+vZlEMX+Fxx+ahO6cRS4yJ7xHB49MUP7on4XED0xFaALtMZtjZktF+r33rjwPKsi{0}" +
                "    w64D7tUk13oxUKLh707mgE2Yu0lLTVvvMjzYBe+19WtM6IK9FS6mLL3OKfSacZP4O3qfW9iVTrK3{0}" +
                "    N7N0l/KQbsFA+JmjhxNnrteOMeDY9uPDS1aSyiEjE8yiOALDzb30cdXmliWQaDhfVYxi63w={0}" +
                "  </Signature>{0}" +
                "</Certificate>{0}", Characters.DosNewLine, Xml.SignatureNamespaceUri,
                PublicKeyToken.Class0
            );
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestScriptStreamXml(
            Interpreter interpreter,
            bool fault,
            bool write,
            ref Result result
            )
        {
            string value = TestGetScriptStreamXmlData();

            if (fault && (value != null))
            {
                //
                // HACK: Purposely corrupt the returned XML script, so
                //       it will fail signature checking.
                //
                value = value.Replace(
                    "11b0bd2a-bc99-4639-9727-dc38efacaca1",
                    Guid.Empty.ToString());
            }

            ReturnCode code;
            ResultList results = null;
            Result localResult; /* REUSED */
            string extra = null;

            using (StringReader stringReader = new StringReader(value))
            {
                EngineFlags engineFlags = EngineFlags.ForceSoftEof;
                string text = null;
                bool canRetry = false; /* NOT USED */

                localResult = null;

                code = Engine.ReadScriptStream(
                    interpreter, null, stringReader, 0,
                    Count.Invalid, ref engineFlags, ref text,
                    ref canRetry, ref localResult);

                if (code == ReturnCode.Ok)
                    localResult = text;

                if (localResult != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(localResult);
                }

                extra = stringReader.ReadToEnd();

                if (extra != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(extra);
                }
            }

            IInteractiveHost interactiveHost = write ?
                interpreter.GetInteractiveHost() : null;

            if (!write || (interactiveHost != null))
            {
                if (write)
                {
                    interactiveHost.WriteResultLine(code, localResult);
                    interactiveHost.WriteLine();

                    interactiveHost.WriteResultLine(code, extra);
                    interactiveHost.WriteLine();
                }

                if (code == ReturnCode.Ok)
                {
                    string text = localResult;

                    localResult = null;

                    code = Engine.EvaluateScript(
                        interpreter, text, ref localResult);

                    if (localResult != null)
                    {
                        if (results == null)
                            results = new ResultList();

                        results.Add(localResult);
                    }
                }

                if (write)
                    interactiveHost.WriteResultLine(code, localResult);
            }

            result = results;
            return code;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This will *NOT* build using Mono/XBuild.  Additionally,
        //       this method will not execute correctly on Mono due to a
        //       missing constructor for TcpServerChannel and incomplete
        //       support for .NET Remoting in general.
        //
#if !MONO && !MONO_BUILD && NETWORK && REMOTING
        public static bool TestRemotingHaveChannel()
        {
            TcpServerChannel channel;
            Result error;

            if (TestRemotingTryGetChannel(true, out channel, out error))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestRemotingTryGetChannel(
            bool trace,
            out TcpServerChannel channel,
            out Result error
            )
        {
            channel = null;
            error = null;

            try
            {
                channel = ChannelServices.GetChannel(
                    RemotingChannelName) as TcpServerChannel;

                return (channel != null);
            }
            catch (Exception e)
            {
                if (trace)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RemoteObject).Name,
                        TracePriority.RemotingError);
                }

                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRemoting(
            Interpreter interpreter,
            int port,
            string uri,
            WellKnownObjectMode mode,
            bool trace,
            ref Result error
            )
        {
            try
            {
                IDictionary sinkProperties = new Hashtable();

                //
                // NOTE: Allow all types to be used.
                //
                sinkProperties.Add("typeFilterLevel", "Full");

                BinaryServerFormatterSinkProvider sinkProvider =
                    new BinaryServerFormatterSinkProvider(
                        sinkProperties, null);

                IDictionary channelProperties = new Hashtable();

                channelProperties.Add("name", RemotingChannelName);
                channelProperties.Add("port", port);

                //
                // NOTE: Default value of "true" causes client to
                //       fail on Windows XP after the first test.
                //
                // BUGBUG: This may not actually work on Windows XP.
                //
                // channelProperties.Add("exclusiveAddressUse", false);

                //
                // NOTE: Value of "true" causes client hang when
                //       no config is used to setup the channel.
                //
                channelProperties.Add("secure", false);

                TcpServerChannel channel = new TcpServerChannel(
                    channelProperties, sinkProvider, new RemoteAuthorizer());

                ChannelServices.RegisterChannel(channel, false);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(RemoteObject), uri, mode);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (trace)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RemoteObject).Name,
                        TracePriority.RemotingError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if EMIT
        public static ReturnCode TestManagedDelegate(
            Interpreter interpreter,
            ref Type type,
            ref Result error
            )
        {
            return DelegateOps.CreateManagedDelegateType(
                interpreter, null, null, null, null, typeof(void),
                new TypeList(new Type[] { typeof(string) }), ref type,
                ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if EMIT && NATIVE && LIBRARY
        private static ReturnCode TestNativeDelegate(
            Interpreter interpreter,
            ref IDelegate @delegate,
            ref Result error
            )
        {
            return DelegateOps.CreateNativeDelegateType(
                interpreter, null, null, null, null, CallingConvention.Cdecl,
                true, (CharSet)0, false, false, typeof(ReturnCode),
                new TypeList(new Type[] {
                    typeof(int), typeof(IntPtr).MakeByRefType(),
                    typeof(Result).MakeByRefType(), typeof(bool)
                }), null, null, null, IntPtr.Zero, ref @delegate, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public virtual bool Disposed
        {
            get
            {
                //
                // NOTE: Obviously, this would defeat the point of this
                //       property.
                //
                // CheckDisposed();

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Disposing
        {
            get
            {
                //
                // NOTE: Obviously, this would defeat the point of this
                //       property.
                //
                // CheckDisposed();

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            #region Dead Code
#if DEAD_CODE
            if (intField == 999)
                throw new ScriptException(ReturnCode.Error, "dispose failure test");
#endif
            #endregion

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(Default).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (ThrowOnDispose)
                {
                    throw new ScriptException(
                        "throw on dispose is enabled");
                }

                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: Not owned by us.  Do not
                    //       dispose.
                    //
                    TestSetScriptComplainCallback(
                        null, null, false, false);

                    ////////////////////////////////////

                    ThreadOps.CloseEvent(ref @event);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Default()
        {
            Dispose(false);
        }
        #endregion
    }
}
