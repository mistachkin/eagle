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
    /// <summary>
    /// This delegate represents a method used to create and populate an array
    /// of callbacks of the specified type, for use by the XML handling
    /// machinery.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the callback elements contained in the array.
    /// </typeparam>
    /// <param name="callbacks">
    /// Upon return, this parameter receives the newly created array of
    /// callbacks.
    /// </param>
    [ObjectId("a0c4c328-49e5-44d2-ae4e-390d60266691")]
    internal delegate void XmlInitializeArrayCallback<T>(
        out T[] callbacks
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to retrieve the value of a named
    /// attribute from an XML element.
    /// </summary>
    /// <param name="element">
    /// The XML element from which to retrieve the attribute value.
    /// </param>
    /// <param name="attributeName">
    /// The name of the attribute whose value is to be retrieved.
    /// </param>
    /// <param name="required">
    /// Non-zero if the attribute must be present; otherwise, a missing
    /// attribute is permitted.
    /// </param>
    /// <param name="attributeValue">
    /// Upon return, this parameter receives the retrieved attribute value.
    /// </param>
    /// <returns>
    /// True if the attribute value was successfully retrieved; otherwise,
    /// false.
    /// </returns>
    [ObjectId("37ed3fee-32f2-455c-9489-b6a5af0b9013")]
    internal delegate bool XmlGetAttributeCallback(
        XmlElement element,
        string attributeName,
        bool required,
        out object attributeValue
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to set the value of a named
    /// attribute on an XML element.
    /// </summary>
    /// <param name="element">
    /// The XML element on which to set the attribute value.
    /// </param>
    /// <param name="attributeName">
    /// The name of the attribute whose value is to be set.
    /// </param>
    /// <param name="required">
    /// Non-zero if the attribute must be present; otherwise, a missing
    /// attribute is permitted.
    /// </param>
    /// <param name="attributeValue">
    /// The value to be assigned to the attribute.
    /// </param>
    /// <returns>
    /// True if the attribute value was successfully set; otherwise, false.
    /// </returns>
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
    /// <summary>
    /// This delegate represents a method used to check whether a character is a
    /// member of some subset of Unicode categories.
    /// </summary>
    /// <param name="character">
    /// The character to be checked.
    /// </param>
    /// <returns>
    /// True if the character is a member of the subset; otherwise, false.
    /// </returns>
    [ObjectId("45d162bb-0243-40c4-ba13-66ed4ae6c3a9")]
    internal delegate bool CharIsCallback(
        char character
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to determine whether an index
    /// falls within a given range of values.
    /// </summary>
    /// <param name="range">
    /// The pair of values describing the inclusive bounds of the range.
    /// </param>
    /// <param name="index">
    /// The index to be tested against the range.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <returns>
    /// True if the index falls within the range, false if it does not, or null
    /// if the determination could not be made.
    /// </returns>
    [ObjectId("57960c5c-3baa-4507-ad02-03ccd9364a62")]
    internal delegate bool? IndexRangeCallback(
        Pair<ulong> range,
        ulong index,
        IClientData clientData
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to create and return a new
    /// instance of some object.
    /// </summary>
    /// <returns>
    /// The newly created object instance.
    /// </returns>
    [ObjectId("3fd07a42-53af-4bad-924c-715c4ccdbeef")]
    internal delegate object FactoryCallback();
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Clock Formatting Related Delegates
    //
    // NOTE: This is used by the [clock] command machinery to handle Tcl
    //       format string compatibility.
    //
    /// <summary>
    /// This delegate represents a method used by the <c>clock</c> command
    /// machinery to transform clock data into a string, for example, when
    /// handling Tcl format string compatibility.
    /// </summary>
    /// <param name="clockData">
    /// The clock data to be transformed.
    /// </param>
    /// <returns>
    /// The string representation produced from the clock data.
    /// </returns>
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
    /// <summary>
    /// This delegate represents a method used by the <c>file</c> command to get
    /// the created, modified, or last access time for a given file.
    /// </summary>
    /// <param name="path">
    /// The path of the file whose date and time is to be retrieved.
    /// </param>
    /// <returns>
    /// The date and time value retrieved for the specified file.
    /// </returns>
    [ObjectId("a968cc80-afb9-4d19-90a0-66c55b5ee2c1")]
    internal delegate DateTime GetDateTimeCallback(
        string path
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used by the <c>file</c> command to set
    /// the created, modified, or last access time for a given file.
    /// </summary>
    /// <param name="path">
    /// The path of the file whose date and time is to be set.
    /// </param>
    /// <param name="dateTime">
    /// The date and time value to be assigned to the specified file.
    /// </param>
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
    /// <summary>
    /// This delegate represents a method used by the package index search
    /// routine to notify the caller of a newly found package index.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter for which the package index search is being performed.
    /// </param>
    /// <param name="path">
    /// The directory path where the package index was found.
    /// </param>
    /// <param name="fileName">
    /// The file name of the package index that was found.
    /// </param>
    /// <param name="tag">
    /// An optional tag used to identify or categorize the package index.  May
    /// be null.
    /// </param>
    /// <param name="type">
    /// The type of package associated with the found package index.
    /// </param>
    /// <param name="flags">
    /// The flags that control package index handling.  This parameter may be
    /// modified by the callback.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  This parameter may be modified
    /// by the callback.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
    /// error code.
    /// </returns>
    [ObjectId("d5f45c92-d910-4e51-8415-01842a78b57d")]
    internal delegate ReturnCode PackageIndexCallback(
        Interpreter interpreter, // TODO: Change to use the IInterpreter type.
        string path,
        string fileName,
        string tag,
        PackageType type,
        ref PackageIndexFlags flags,
        ref IClientData clientData,
        ref Result error
    );
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Script Evaluation Related Delegates
    /// <summary>
    /// This delegate represents a method used to locate and evaluate a named
    /// script from the script library.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter in which the library script is to be evaluated.
    /// </param>
    /// <param name="fileSystemHost">
    /// The file system host used to locate and read the library script.
    /// </param>
    /// <param name="name">
    /// The name of the library script to be located and evaluated.
    /// </param>
    /// <param name="direct">
    /// Non-zero to evaluate the script directly; otherwise, zero.
    /// </param>
    /// <param name="scriptFlags">
    /// The flags that control how the library script is located and handled.
    /// This parameter may be modified by the callback.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  This parameter may be modified
    /// by the callback.  May be null.
    /// </param>
    /// <param name="result">
    /// Upon success, this parameter receives the result of evaluating the
    /// script; upon failure, it receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
    /// error code.
    /// </returns>
    [ObjectId("39b0cf79-f06e-48a3-8a33-545c2179d5b3")]
    internal delegate ReturnCode ScriptLibraryCallback(
        Interpreter interpreter,
        IFileSystemHost fileSystemHost,
        string name,
        bool direct,
        ref ScriptFlags scriptFlags,
        ref IClientData clientData,
        ref Result result
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Somehow, script evaluation in the interpreter was interrupted
    //       (e.g. canceled, unwound, halted, deleted, etc).  This callback
    //       type is used to notify external callers of this condition.
    //
    /// <summary>
    /// This delegate represents a method used to notify external callers that
    /// script evaluation in the interpreter has been interrupted (for example,
    /// canceled, unwound, halted, or deleted).
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter whose script evaluation was interrupted.
    /// </param>
    /// <param name="interruptType">
    /// The type of interruption that occurred.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
    /// error code.
    /// </returns>
    [ObjectId("06442494-d5cd-4d25-9538-adeff4e518d9")]
    internal delegate ReturnCode InterruptCallback(
        Interpreter interpreter,
        InterruptType interruptType,
        IClientData clientData,
        ref Result error
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to filter the sub-commands of an
    /// ensemble, returning only those that should be visible to the caller.
    /// </summary>
    /// <param name="interpreter">
    /// The interpreter in which the ensemble sub-commands are being filtered.
    /// </param>
    /// <param name="ensemble">
    /// The ensemble whose sub-commands are being filtered.
    /// </param>
    /// <param name="subCommands">
    /// The collection of candidate sub-commands to be filtered.
    /// </param>
    /// <param name="error">
    /// Upon failure, this parameter receives an error message.
    /// </param>
    /// <returns>
    /// The filtered collection of sub-commands, or null if filtering failed.
    /// </returns>
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
    /// <summary>
    /// This delegate represents the native Unix <c>dlopen</c> function, used by
    /// the dynamic loader to open a shared library.
    /// </summary>
    /// <param name="fileName">
    /// The file name of the shared library to be opened.
    /// </param>
    /// <param name="mode">
    /// The flags controlling how the shared library is opened.
    /// </param>
    /// <returns>
    /// A handle to the opened shared library, or <see cref="IntPtr.Zero" /> on
    /// failure.
    /// </returns>
    [ObjectId("ae933b48-15c2-4d3f-a0a0-79f2504f7c02")]
    internal delegate IntPtr dlopen(
        string fileName,
        int mode
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Unix <c>dlclose</c> function, used
    /// by the dynamic loader to close a previously opened shared library.
    /// </summary>
    /// <param name="module">
    /// The handle to the shared library to be closed.
    /// </param>
    /// <returns>
    /// Zero on success; otherwise, a non-zero value.
    /// </returns>
    [ObjectId("9fc24143-1481-439b-ab73-d96cd6d83d90")]
    internal delegate int dlclose(
        IntPtr module
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Unix <c>dlsym</c> function, used by
    /// the dynamic loader to look up the address of a symbol within a shared
    /// library.
    /// </summary>
    /// <param name="module">
    /// The handle to the shared library to be searched.
    /// </param>
    /// <param name="name">
    /// The name of the symbol whose address is to be looked up.
    /// </param>
    /// <returns>
    /// The address of the named symbol, or <see cref="IntPtr.Zero" /> if it
    /// could not be found.
    /// </returns>
    [ObjectId("f36604da-d689-4fa7-91d1-f44ba4e2e69a")]
    internal delegate IntPtr dlsym(
        IntPtr module,
        string name
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Unix <c>dladdr</c> function, used by
    /// the dynamic loader to obtain information about the shared library
    /// containing a given address.
    /// </summary>
    /// <param name="address">
    /// The address for which information is to be obtained.
    /// </param>
    /// <param name="info">
    /// Upon return, this parameter receives information about the shared
    /// library containing the address.
    /// </param>
    /// <returns>
    /// A non-zero value on success; otherwise, zero.
    /// </returns>
    [ObjectId("4a54273f-bce8-4a97-aa37-51041adb31fe")]
    internal delegate int dladdr(
        IntPtr address,
        ref NativeOps.UnsafeNativeMethods.Dl_info_t info
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Unix <c>dlerror</c> function, used
    /// by the dynamic loader to retrieve a human-readable string describing the
    /// most recent error.
    /// </summary>
    /// <returns>
    /// A pointer to a string describing the most recent error, or
    /// <see cref="IntPtr.Zero" /> if no error has occurred.
    /// </returns>
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
    /// <summary>
    /// This delegate represents a method used by the Windows native exception
    /// handling code to filter a top-level exception.
    /// </summary>
    /// <param name="exception">
    /// The exception to be filtered.
    /// </param>
    /// <returns>
    /// A value indicating how the exception should be handled.
    /// </returns>
    [ObjectId("2f60ccbb-4060-4b8f-b320-baa8ba030684")]
    internal delegate int TopLevelExceptionFilterCallback(
        Exception exception
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Windows native stack checking code.
    //
    /// <summary>
    /// This delegate represents the native Windows <c>NtCurrentTeb</c>
    /// function, used by the Windows native stack checking code to obtain the
    /// thread environment block for the current thread.
    /// </summary>
    /// <returns>
    /// A pointer to the thread environment block for the current thread.
    /// </returns>
    [ObjectId("20a6b621-590a-41f6-ad37-83dd56b2238c")]
    internal delegate IntPtr NtCurrentTeb();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the WindowOps.GetNativeWindow code.
    //
    /// <summary>
    /// This delegate represents a method used by the
    /// <c>WindowOps.GetNativeWindow</c> code to obtain a handle to the native
    /// window.
    /// </summary>
    /// <returns>
    /// A handle to the native window, or <see cref="IntPtr.Zero" /> if none is
    /// available.
    /// </returns>
    [ObjectId("a65c5973-4c3f-4922-bca0-a48068f49237")]
    internal delegate IntPtr GetNativeWindowCallback();

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Windows native QueueUserAPC function.
    //
    /// <summary>
    /// This delegate represents a method used as the asynchronous procedure
    /// call routine for the native Windows <c>QueueUserAPC</c> function.
    /// </summary>
    /// <param name="data">
    /// The opaque, caller-defined data passed to the asynchronous procedure
    /// call routine.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [ObjectId("3c45a7d9-3b66-4a4e-af94-5260b51bccdd")]
    internal delegate void ApcCallback(
        IntPtr data
    );

    ///////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Windows native EnumWindows function.
    //
    /// <summary>
    /// This delegate represents the enumeration callback routine used by the
    /// native Windows <c>EnumWindows</c> function; it is invoked once for each
    /// top-level window.
    /// </summary>
    /// <param name="hWnd">
    /// The handle to the top-level window being enumerated.
    /// </param>
    /// <param name="lParam">
    /// The opaque, caller-defined data passed to the enumeration routine.
    /// </param>
    /// <returns>
    /// True to continue the enumeration; false to stop it.
    /// </returns>
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
    /// <summary>
    /// This delegate represents the native utility library function used to
    /// obtain the version string of that library.
    /// </summary>
    /// <returns>
    /// A pointer to the version string of the native utility library.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("52ddb2fb-4b4b-4e2f-be2f-7a751aaf9f89")]
    internal delegate IntPtr Eagle_GetVersion();

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native utility library function used to
    /// free a version string previously obtained from that library.
    /// </summary>
    /// <param name="pVersion">
    /// A pointer to the version string to be freed.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("47d130c9-5831-4b69-bdec-0a3bc913c482")]
    internal delegate void Eagle_FreeVersion(
        IntPtr pVersion
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native utility library function used to
    /// allocate a block of memory.
    /// </summary>
    /// <param name="size">
    /// The size, in bytes, of the block of memory to be allocated.
    /// </param>
    /// <returns>
    /// A pointer to the newly allocated block of memory, or
    /// <see cref="IntPtr.Zero" /> on failure.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("ad5185f6-f1f1-4736-b1ec-2e7d9d329763")]
    internal delegate IntPtr Eagle_AllocateMemory(
        int size
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native utility library function used to
    /// free a block of memory previously allocated by that library.
    /// </summary>
    /// <param name="pMemory">
    /// A pointer to the block of memory to be freed.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("c14691dc-6921-4dc9-8364-5e9028818bf8")]
    internal delegate void Eagle_FreeMemory(
        IntPtr pMemory
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native utility library function used to
    /// free an array of list elements previously allocated by that library.
    /// </summary>
    /// <param name="elementCount">
    /// The number of elements in the array to be freed.
    /// </param>
    /// <param name="ppElements">
    /// A pointer to the array of elements to be freed.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("8a107c25-71af-43c1-bd0a-40b458dabab2")]
    internal delegate void Eagle_FreeElements(
        int elementCount,
        IntPtr ppElements
    );

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native utility library function used to
    /// split a string into its constituent list elements.
    /// </summary>
    /// <param name="length">
    /// The length, in characters, of the string to be split.
    /// </param>
    /// <param name="text">
    /// The string to be split into list elements.
    /// </param>
    /// <param name="elementCount">
    /// Upon return, this parameter receives the number of elements produced by
    /// the split.
    /// </param>
    /// <param name="pElementLengths">
    /// Upon return, this parameter receives a pointer to the array of element
    /// lengths.
    /// </param>
    /// <param name="ppElements">
    /// Upon return, this parameter receives a pointer to the array of split
    /// elements.
    /// </param>
    /// <param name="pError">
    /// Upon failure, this parameter receives a pointer to an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
    /// error code.
    /// </returns>
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

    /// <summary>
    /// This delegate represents the native utility library function used to
    /// join an array of list elements into a single string.
    /// </summary>
    /// <param name="elementCount">
    /// The number of elements in the array to be joined.
    /// </param>
    /// <param name="elementLengths">
    /// The array of element lengths, in characters.
    /// </param>
    /// <param name="elements">
    /// The array of elements to be joined.
    /// </param>
    /// <param name="length">
    /// Upon return, this parameter receives the length, in characters, of the
    /// joined string.
    /// </param>
    /// <param name="pText">
    /// Upon return, this parameter receives a pointer to the joined string.
    /// </param>
    /// <param name="pError">
    /// Upon failure, this parameter receives a pointer to an error message.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
    /// error code.
    /// </returns>
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

    /// <summary>
    /// This delegate represents the native utility library function used to set
    /// the memory heap that library uses for its allocations.
    /// </summary>
    /// <param name="newHeap">
    /// A handle to the new memory heap to be used.
    /// </param>
    /// <returns>
    /// A handle to the previously configured memory heap.
    /// </returns>
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
    /// <summary>
    /// This delegate represents a method used to report an error encountered by
    /// the Windows Forms integration code, for example, by writing it to a
    /// trace listener.
    /// </summary>
    /// <param name="error">
    /// The error to be reported.
    /// </param>
    [ObjectId("4b291012-5998-4945-a166-90ea70a0c811")]
    internal delegate void TraceErrorCallback(
        Result error
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Error Transformation Delegates
    /// <summary>
    /// This delegate represents a method used to transform an error into
    /// another error, for example, to wrap or rewrite its message.
    /// </summary>
    /// <param name="error">
    /// The error to be transformed.
    /// </param>
    /// <returns>
    /// The transformed error.
    /// </returns>
    [ObjectId("2e168d44-0cf4-40b3-8140-6f11b2c515b0")]
    internal delegate Result ErrorCallback(Result error);

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to transform a list of errors
    /// into another list of errors, for example, to wrap or rewrite its
    /// entries.
    /// </summary>
    /// <param name="error">
    /// The list of errors to be transformed.
    /// </param>
    /// <returns>
    /// The transformed list of errors.
    /// </returns>
    [ObjectId("4a243c7e-1cd3-4fa2-bd56-9b71502dbe8b")]
    internal delegate ResultList ErrorListCallback(ResultList error);
    #endregion
}
