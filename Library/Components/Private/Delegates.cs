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

#if NATIVE && (WINDOWS || NATIVE_UTILITY)
using System.Runtime.InteropServices;
#endif

#if NATIVE && NATIVE_UTILITY
using System.Security;
#endif

#if XML
using System.Xml;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private.Delegates
{
    #region Xml Handling Related Delegates
#if XML
    [ObjectId("a0c4c328-49e5-44d2-ae4e-390d60266691")]
    internal delegate void XmlInitializeArrayCallback<T>(
        out T[] callbacks
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("37ed3fee-32f2-455c-9489-b6a5af0b9013")]
    internal delegate bool XmlGetAttributeCallback(
        XmlElement element,
        string attributeName,
        bool required,
        out object attributeValue
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("6993ede3-81a8-4f27-8be8-a63e9d832975")]
    internal delegate bool XmlSetAttributeCallback(
        XmlElement element,
        string attributeName,
        bool required,
        object attributeValue
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region String Handling Related Delegates
    //
    // NOTE: This is used to check if a character is a member of some subset
    //       of Unicode categories.
    //
    [ObjectId("45d162bb-0243-40c4-ba13-66ed4ae6c3a9")]
    internal delegate bool CharIsCallback(
        char character
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("3fd07a42-53af-4bad-924c-715c4ccdbeef")]
    internal delegate object FactoryCallback();
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Clock Formatting Related Delegates
    //
    // NOTE: This is used by the [clock] command machinery to handle Tcl
    //       format string compatibility.
    //
    [ObjectId("4b36c510-9db2-4177-86f5-3b990b59f299")]
    internal delegate string ClockTransformCallback(
        IClockData clockData
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region File DateTime Handling Related Delegates
    //
    // NOTE: These are used by the file command to get or set the
    //       created, modified, or last access time for a given file.
    //
    [ObjectId("a968cc80-afb9-4d19-90a0-66c55b5ee2c1")]
    internal delegate DateTime GetDateTimeCallback(
        string path
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("4ad448b1-59be-4ac1-afad-53bf9b8fbbb2")]
    internal delegate void SetDateTimeCallback(
        string path,
        DateTime dateTime
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Package Handling Related Delegates
    //
    // NOTE: This is used by the package index search routine to notify the
    //       caller of a newly found package index.
    //
    [ObjectId("d5f45c92-d910-4e51-8415-01842a78b57d")]
    internal delegate ReturnCode PackageIndexCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string path,
        string fileName,
        ref PackageIndexFlags flags,
        ref IClientData clientData,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Script Evaluation Related Delegates
    //
    // NOTE: Somehow, script evaluation in the interpreter was interrupted
    //       (e.g. canceled, unwound, halted, deleted, etc).  This callback
    //       type is used to notify external callers of this condition.
    //
    [ObjectId("06442494-d5cd-4d25-9538-adeff4e518d9")]
    internal delegate ReturnCode InterruptCallback(
        Interpreter interpreter,
        InterruptType interruptType,
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("3c161b95-d8d6-48a6-a6fc-4581ffd0c6ec")]
    internal delegate IEnumerable<KeyValuePair<string, ISubCommand>>
            SubCommandFilterCallback(
        Interpreter interpreter,
        IEnsemble ensemble,
        IEnumerable<KeyValuePair<string, ISubCommand>> subCommands,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Native Unix Integration Related Delegates
#if NATIVE && UNIX
    //
    // NOTE: Used by the dynamic loader on Unix.
    //
    [ObjectId("ae933b48-15c2-4d3f-a0a0-79f2504f7c02")]
    internal delegate IntPtr dlopen(
        string fileName,
        int mode
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("9fc24143-1481-439b-ab73-d96cd6d83d90")]
    internal delegate int dlclose(
        IntPtr module
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("f36604da-d689-4fa7-91d1-f44ba4e2e69a")]
    internal delegate IntPtr dlsym(
        IntPtr module,
        string name
    );

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("ec1c7506-9c6e-4630-80ed-8ce2633fc4bb")]
    internal delegate IntPtr dlerror();
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Native Windows Integration Related Delegates
#if NATIVE && WINDOWS
    //
    // NOTE: Used by the Windows native exception handling code.
    //
    [ObjectId("2f60ccbb-4060-4b8f-b320-baa8ba030684")]
    internal delegate int TopLevelExceptionFilterCallback(
        Exception exception
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Windows native stack checking code.
    //
    [ObjectId("20a6b621-590a-41f6-ad37-83dd56b2238c")]
    internal delegate IntPtr NtCurrentTeb();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the WindowOps.GetNativeWindow code.
    //
    [ObjectId("a65c5973-4c3f-4922-bca0-a48068f49237")]
    internal delegate IntPtr GetNativeWindowCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Windows native QueueUserAPC function.
    //
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [ObjectId("3c45a7d9-3b66-4a4e-af94-5260b51bccdd")]
    internal delegate void ApcCallback(
        IntPtr data
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Windows native EnumWindows function.
    //
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [ObjectId("3b2d7140-039d-4d82-aca7-fecc8f52b311")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool EnumWindowCallback(
        IntPtr hWnd,
        IntPtr lParam
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Native Utility Integration Related Delegates
#if NATIVE && NATIVE_UTILITY
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("52ddb2fb-4b4b-4e2f-be2f-7a751aaf9f89")]
    internal delegate IntPtr Eagle_GetVersion();

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("47d130c9-5831-4b69-bdec-0a3bc913c482")]
    internal delegate void Eagle_FreeVersion(
        IntPtr pVersion
    );

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("ad5185f6-f1f1-4736-b1ec-2e7d9d329763")]
    internal delegate IntPtr Eagle_AllocateMemory(
        int size
    );

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("c14691dc-6921-4dc9-8364-5e9028818bf8")]
    internal delegate void Eagle_FreeMemory(
        IntPtr pMemory
    );

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("8a107c25-71af-43c1-bd0a-40b458dabab2")]
    internal delegate void Eagle_FreeElements(
        int elementCount,
        IntPtr ppElements
    );

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("4d0ffa2a-7968-48ee-b7a0-c6801120ea3f")]
    internal delegate ReturnCode Eagle_SplitList(
        int length,
        string text,
        ref int elementCount,
        ref IntPtr pElementLengths,
        ref IntPtr ppElements,
        ref IntPtr pError
    );

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("f3d46d80-6b00-44a9-b344-5216b03ac460")]
    internal delegate ReturnCode Eagle_JoinList(
        int elementCount,
        int[] elementLengths,
#if NATIVE_UTILITY_BSTR
        [MarshalAs(UnmanagedType.LPArray,
            ArraySubType = UnmanagedType.BStr)]
#endif
        string[] elements,
        ref int length,
        ref IntPtr pText,
        ref IntPtr pError
    );

    ///////////////////////////////////////////////////////////////////////////

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("1ec7e25c-1458-4f39-9271-495826c34689")]
    internal delegate IntPtr Eagle_SetMemoryHeap(
        IntPtr newHeap
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Windows Forms Related Delegates
#if WINFORMS
    [ObjectId("4b291012-5998-4945-a166-90ea70a0c811")]
    internal delegate void TraceErrorCallback(
        Result error
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Error Transformation Delegates
    [ObjectId("2e168d44-0cf4-40b3-8140-6f11b2c515b0")]
    internal delegate Result ErrorCallback(Result error);

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("4a243c7e-1cd3-4fa2-bd56-9b71502dbe8b")]
    internal delegate ResultList ErrorListCallback(ResultList error);
    #endregion
}
