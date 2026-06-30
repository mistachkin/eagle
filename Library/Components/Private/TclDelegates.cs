/*
 * TclDelegates.cs --
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
using System.Runtime.InteropServices;
using System.Security;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private.Tcl;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private.Tcl.Delegates
{
    //
    // NOTE: This delegate is the same as "System.Threading.ThreadStart".
    //
    /// <summary>
    /// This delegate represents the entry point method for an isolated Tcl
    /// worker thread that accepts no parameter; it is equivalent to
    /// <see cref="System.Threading.ThreadStart" />.
    /// </summary>
    [ObjectId("61666bcd-2023-460a-bd0c-990e2e8c30e0")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_ThreadStart();

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // NOTE: This delegate is the same as "System.Threading.ParameterizedThreadStart".
    //
    /// <summary>
    /// This delegate represents the entry point method for an isolated Tcl
    /// worker thread that accepts a single object parameter; it is equivalent
    /// to <see cref="System.Threading.ParameterizedThreadStart" />.
    /// </summary>
    /// <param name="obj">
    /// Optional, opaque, caller-defined data passed to the thread entry point.
    /// May be null.
    /// </param>
    [ObjectId("dab42cf0-99cc-4a4c-88cb-a15f56eea090")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_ParameterizedThreadStart(
        object obj
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Used by the Tcl worker thread class.  This delegate is the same as
    //       "Eagle._Components.Public.Delegates.ApcCallback".
    //
    /// <summary>
    /// This delegate represents a callback, invoked by the Tcl worker thread,
    /// that receives a single native pointer argument; it is equivalent to
    /// <c>Eagle._Components.Public.Delegates.ApcCallback</c>.
    /// </summary>
    /// <param name="data">
    /// An opaque native pointer passed to the callback.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("aa66a40a-accf-44b5-996b-e6bd9853efe7")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_EventCallback(
        IntPtr data
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents a method used to locate available native Tcl
    /// builds, optionally recursing into nested searches via the supplied
    /// callback.
    /// </summary>
    /// <param name="tclManager">
    /// The Tcl manager that provides context for the find operation.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the current find operation is performed.
    /// </param>
    /// <param name="allFlags">
    /// The combined flags that apply across the entire (possibly recursive)
    /// find operation.
    /// </param>
    /// <param name="callback">
    /// The callback used to perform nested find operations, if any.
    /// </param>
    /// <param name="paths">
    /// The candidate file system paths to be searched.
    /// </param>
    /// <param name="minimumRequired">
    /// The minimum required Tcl version, or null for no minimum.
    /// </param>
    /// <param name="maximumRequired">
    /// The maximum required Tcl version, or null for no maximum.
    /// </param>
    /// <param name="unknown">
    /// The Tcl version to assume when one cannot be determined, or null.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data.  May be null.
    /// </param>
    /// <param name="builds">
    /// Upon success, this dictionary receives the discovered Tcl builds keyed
    /// by version.
    /// </param>
    /// <param name="errors">
    /// Upon failure, this list receives one or more error messages describing
    /// why the find operation failed.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [ObjectId("35c49550-649e-4b41-873b-ceb88d6ab148")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_FindCallback(
        ITclManager tclManager,
        FindFlags flags,
        FindFlags allFlags,
        Tcl_FindCallback callback,
        IEnumerable<string> paths,
        Version minimumRequired,
        Version maximumRequired,
        Version unknown,
        IClientData clientData,
        ref TclBuildDictionary builds,
        ref ResultList errors
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to retrieve the
    /// version of the loaded Tcl library.
    /// </summary>
    /// <param name="major">
    /// Upon return, this parameter receives the major version number.
    /// </param>
    /// <param name="minor">
    /// Upon return, this parameter receives the minor version number.
    /// </param>
    /// <param name="patchLevel">
    /// Upon return, this parameter receives the patch level number.
    /// </param>
    /// <param name="releaseLevel">
    /// Upon return, this parameter receives the release level (e.g. alpha,
    /// beta, or final).
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("7eaa9886-a841-409e-b992-64371b5c9d77")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_GetVersion(
        out int major,
        out int minor,
        out int patchLevel,
        out Tcl_ReleaseLevel releaseLevel
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to inform the Tcl
    /// library of the full path to the running executable, based on the value
    /// of its first command line argument.
    /// </summary>
    /// <param name="argv0">
    /// The first command line argument (i.e. the program name) used to locate
    /// the executable.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("b4164df3-a5f8-4fd0-ac55-28ce420696d8")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_FindExecutable(
        string argv0
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
    /// <summary>
    /// This delegate represents the native TclKit function used to set the path
    /// to the Tcl kit (i.e. the embedded archive) used by the loaded library.
    /// </summary>
    /// <param name="kitPath">
    /// The path to the Tcl kit to be used.
    /// </param>
    /// <returns>
    /// A native pointer that represents the previously configured kit path, if
    /// any.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("761504db-bc70-424c-9b6f-d21241376884")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr TclKit_SetKitPath(
        string kitPath
    );
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to create a new
    /// Tcl interpreter.
    /// </summary>
    /// <returns>
    /// A native pointer to the newly created Tcl interpreter.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("0540853c-2587-4742-9608-e3b63b718d19")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_CreateInterp();

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to preserve (i.e.
    /// increment the reference count of) a native object so that it is not
    /// freed while still in use.
    /// </summary>
    /// <param name="clientData">
    /// A native pointer to the object to be preserved.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("362d2cac-4ba5-42af-b54b-0cf2b13b6c67")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_Preserve(
        IntPtr clientData
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to release (i.e.
    /// decrement the reference count of) a native object previously preserved,
    /// freeing it when no references remain.
    /// </summary>
    /// <param name="clientData">
    /// A native pointer to the object to be released.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("5c56a471-d996-4d00-94bf-b1fbff782d93")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_Release(
        IntPtr clientData
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to read the value
    /// of a Tcl variable (or array element) identified by its two name parts.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that owns the variable.
    /// </param>
    /// <param name="part1Ptr">
    /// A native pointer to the first part of the variable name (i.e. the array
    /// or scalar name).
    /// </param>
    /// <param name="part2Ptr">
    /// A native pointer to the second part of the variable name (i.e. the array
    /// element name), or a null pointer for a scalar variable.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the variable is accessed.
    /// </param>
    /// <returns>
    /// A native pointer to the value of the variable, or a null pointer on
    /// failure.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("d00dae3d-4c23-46c2-952a-095290901e6b")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_ObjGetVar2(
        IntPtr interp,
        IntPtr part1Ptr,
        IntPtr part2Ptr,
        Tcl_VarFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to set the value
    /// of a Tcl variable (or array element) identified by its two name parts.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that owns the variable.
    /// </param>
    /// <param name="part1Ptr">
    /// A native pointer to the first part of the variable name (i.e. the array
    /// or scalar name).
    /// </param>
    /// <param name="part2Ptr">
    /// A native pointer to the second part of the variable name (i.e. the array
    /// element name), or a null pointer for a scalar variable.
    /// </param>
    /// <param name="newValuePtr">
    /// A native pointer to the new value to be stored in the variable.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the variable is accessed.
    /// </param>
    /// <returns>
    /// A native pointer to the new value of the variable, or a null pointer on
    /// failure.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("1c891070-8650-465e-8bdd-b284600a629c")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_ObjSetVar2(
        IntPtr interp,
        IntPtr part1Ptr,
        IntPtr part2Ptr,
        IntPtr newValuePtr,
        Tcl_VarFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to unset a Tcl
    /// variable (or array element) identified by its two name parts.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that owns the variable.
    /// </param>
    /// <param name="name1">
    /// The first part of the variable name (i.e. the array or scalar name).
    /// </param>
    /// <param name="name2">
    /// The second part of the variable name (i.e. the array element name), or
    /// null for a scalar variable.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the variable is accessed.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("2c119879-5acb-472f-bd42-7084e14a062d")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_UnsetVar2(
        IntPtr interp,
        string name1,
        string name2,
        Tcl_VarFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_KITS
    /// <summary>
    /// This delegate represents the native TclKit application initialization
    /// function, invoked to initialize a Tcl interpreter created from a kit.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be initialized.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("02db5df2-b878-41f3-b34d-171321eec5ff")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode TclKit_AppInit(
        IntPtr interp
    );
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to initialize the
    /// standard Tcl script library support for an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be initialized.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("4099a5ee-6f76-4573-b7f0-19c37c9e65b6")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_Init(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to initialize the
    /// Tcl memory debugging command for an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be initialized.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("69f61baa-e2ed-4606-9a9a-21037c626a20")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_InitMemory(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to mark an
    /// interpreter as safe, removing potentially dangerous commands.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be made safe.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("34a0ada6-9c0c-4231-8c32-1980518cfe9b")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_MakeSafe(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Dead Code
#if DEAD_CODE
    /// <summary>
    /// This delegate represents the native Tcl function used to register a
    /// new Tcl object type, making it available for use by the interpreter.
    /// </summary>
    /// <param name="typePtr">
    /// A reference to the Tcl object type structure to be registered.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("7da8e3da-6891-4df3-abb0-541b8a8f11bc")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_RegisterObjType(
        ref Tcl_ObjType typePtr
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to look up a
    /// registered Tcl object type by its name.
    /// </summary>
    /// <param name="typeName">
    /// The name of the Tcl object type to look up.
    /// </param>
    /// <returns>
    /// A native pointer to the matching Tcl object type, or a null pointer if
    /// no such type is registered.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("f60d9fa2-29bd-4260-a468-150ff295d0e0")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate /* Tcl_ObjType */ IntPtr Tcl_GetObjType(
        string typeName
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to append the
    /// names of all registered Tcl object types to a Tcl object.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that provides context for the
    /// operation.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object that receives the appended type
    /// names.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("55b58400-93da-477f-b3c5-d0418bbac783")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_AppendAllObjTypes(
        IntPtr interp,
        IntPtr objPtr
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to convert a Tcl
    /// object to the specified Tcl object type.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that provides context for the
    /// conversion.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object to be converted.
    /// </param>
    /// <param name="typePtr">
    /// A native pointer to the Tcl object type to convert to.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("d95d7850-961f-4584-a656-e0d3512c8e78")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_ConvertToType(
        IntPtr interp,
        IntPtr objPtr,
        IntPtr typePtr
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to create a new
    /// object-based Tcl command in an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter in which to create the command.
    /// </param>
    /// <param name="cmdName">
    /// The name of the Tcl command to be created.
    /// </param>
    /// <param name="proc">
    /// The callback to be invoked when the command is evaluated.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data passed to the command and delete
    /// callbacks.
    /// </param>
    /// <param name="deleteProc">
    /// The callback to be invoked when the command is deleted.
    /// </param>
    /// <returns>
    /// A native pointer to the token that identifies the newly created command.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("418ba20b-b02c-47fb-b291-5d1637e18226")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_CreateObjCommand(
        IntPtr interp,
        string cmdName,
        [MarshalAs(UnmanagedType.FunctionPtr)] Tcl_ObjCmdProc proc,
        IntPtr clientData,
        [MarshalAs(UnmanagedType.FunctionPtr)] Tcl_CmdDeleteProc deleteProc
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the object-based command callback invoked by
    /// the native Tcl runtime when a bridged Tcl command is evaluated.
    /// </summary>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data associated with the command.
    /// </param>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that is evaluating the command.
    /// </param>
    /// <param name="objc">
    /// The number of argument objects supplied to the command, including the
    /// command name itself.
    /// </param>
    /// <param name="objv">
    /// A native pointer to the array of argument objects supplied to the
    /// command.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /* [SuppressUnmanagedCodeSecurity()] */
    [ObjectId("c5daafcb-7edc-48fc-84ea-69e235117584")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_ObjCmdProc( /* Command */
        IntPtr clientData,
        IntPtr interp,
        int objc,
        IntPtr objv
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the command deletion callback invoked by the
    /// native Tcl runtime when a bridged Tcl command is deleted.
    /// </summary>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data associated with the command being
    /// deleted.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /* [SuppressUnmanagedCodeSecurity()] */
    [ObjectId("7955a2f6-6cbc-4048-8065-d66f0003b928")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_CmdDeleteProc( /* Command */
        IntPtr clientData
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to delete a Tcl
    /// command identified by its previously created token.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that owns the command.
    /// </param>
    /// <param name="token">
    /// A native pointer to the token that identifies the command to be deleted.
    /// </param>
    /// <returns>
    /// Zero if the command was deleted successfully; otherwise, a non-zero
    /// value.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("982a86db-6895-4498-a488-4a5a43ce02ba")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate int Tcl_DeleteCommandFromToken(
        IntPtr interp,
        IntPtr token
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to delete a Tcl
    /// interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be deleted.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("507161b5-04db-4203-ab37-649158032262")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_DeleteInterp(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to determine
    /// whether a Tcl interpreter has been deleted.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be checked.
    /// </param>
    /// <returns>
    /// A non-zero value if the interpreter has been deleted; otherwise, zero.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("402c50fc-d379-4297-96c8-6963f752d1b4")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate int Tcl_InterpDeleted(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to determine
    /// whether a Tcl interpreter is currently active (i.e. evaluating).
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be checked.
    /// </param>
    /// <returns>
    /// A non-zero value if the interpreter is active; otherwise, zero.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("3d2413fd-ca9d-48b6-be65-838f90c6c2c7")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate int Tcl_InterpActive(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to retrieve the
    /// line number associated with the most recent error in an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be queried.
    /// </param>
    /// <returns>
    /// The line number of the most recent error.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("8708d4e7-c708-4b06-8850-fe5758a68c77")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate int Tcl_GetErrorLine(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to set the line
    /// number associated with the most recent error in an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be modified.
    /// </param>
    /// <param name="line">
    /// The error line number to be set.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("8bf1e635-0fb9-4173-bfe9-4db02748af35")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_SetErrorLine(
        IntPtr interp,
        int line
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to create a new,
    /// empty Tcl object.
    /// </summary>
    /// <returns>
    /// A native pointer to the newly created Tcl object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("64131be5-8b9d-43d1-9ad5-ddc7209eec8c")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_NewObj();

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to create a new
    /// Tcl object from a Unicode string.
    /// </summary>
    /// <param name="unicode">
    /// The Unicode string used to initialize the new Tcl object.
    /// </param>
    /// <param name="numChars">
    /// The number of characters from the string to use, or a negative value to
    /// use the entire string.
    /// </param>
    /// <returns>
    /// A native pointer to the newly created Tcl object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("f102611f-cb71-4bfe-8355-0d73fb675874")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_NewUnicodeObj(
        string unicode,
        int numChars
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to create a new
    /// Tcl object from a byte array containing string data.
    /// </summary>
    /// <param name="bytes">
    /// The byte array containing the string data used to initialize the new
    /// Tcl object.
    /// </param>
    /// <param name="length">
    /// The number of bytes from the array to use, or a negative value to use
    /// the entire array.
    /// </param>
    /// <returns>
    /// A native pointer to the newly created Tcl object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("76becc7f-1853-4dc6-8a4f-88b97bccec8a")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_NewStringObj(
        [MarshalAs(UnmanagedType.LPArray)] byte[] bytes,
        int length
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to create a new
    /// Tcl byte array object from a byte array.
    /// </summary>
    /// <param name="bytes">
    /// The byte array used to initialize the new Tcl object.
    /// </param>
    /// <param name="length">
    /// The number of bytes from the array to use.
    /// </param>
    /// <returns>
    /// A native pointer to the newly created Tcl object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("1e7f38bf-231d-466a-b0b3-82e106f69e6d")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_NewByteArrayObj(
        [MarshalAs(UnmanagedType.LPArray)] byte[] bytes,
        int length
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Dead Code
#if DEAD_CODE
    /// <summary>
    /// This delegate represents the native Tcl function used to create a new
    /// Tcl object that is a duplicate of an existing Tcl object.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object to be duplicated.
    /// </param>
    /// <returns>
    /// A native pointer to the newly created duplicate Tcl object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("36165d35-2215-46aa-96b6-6698d3f2577b")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_DuplicateObj(
        IntPtr objPtr
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the debugging variant of the native Tcl
    /// function used to increment the reference count of a Tcl object.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose reference count is to be
    /// incremented.
    /// </param>
    /// <param name="fileName">
    /// The name of the source file from which the call originates, used for
    /// debugging.
    /// </param>
    /// <param name="line">
    /// The line number within the source file from which the call originates,
    /// used for debugging.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("77136398-aeeb-4efc-b80d-10acbaa18979")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_DbIncrRefCount(
        IntPtr objPtr,
        string fileName,
        int line
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the debugging variant of the native Tcl
    /// function used to decrement the reference count of a Tcl object, freeing
    /// it when no references remain.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose reference count is to be
    /// decremented.
    /// </param>
    /// <param name="fileName">
    /// The name of the source file from which the call originates, used for
    /// debugging.
    /// </param>
    /// <param name="line">
    /// The line number within the source file from which the call originates,
    /// used for debugging.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("4b74b434-c6d6-410e-9560-4cef3628eb6e")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_DbDecrRefCount(
        IntPtr objPtr,
        string fileName,
        int line
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Dead Code
#if DEAD_CODE
    /// <summary>
    /// This delegate represents the debugging variant of the native Tcl
    /// function used to determine whether a Tcl object is shared (i.e. has a
    /// reference count greater than one).
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object to be tested.
    /// </param>
    /// <param name="fileName">
    /// The name of the source file from which the call originates, used for
    /// debugging.
    /// </param>
    /// <param name="line">
    /// The line number within the source file from which the call originates,
    /// used for debugging.
    /// </param>
    /// <returns>
    /// A non-zero value if the Tcl object is shared; otherwise, zero.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("0cad1440-64cd-43dc-ba34-e586b40b4c72")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate int Tcl_DbIsShared(
        IntPtr objPtr,
        string fileName,
        int line
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to invalidate
    /// the string representation of a Tcl object, forcing it to be
    /// regenerated from the internal representation when next required.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose string representation is to
    /// be invalidated.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("a46fe667-eb5c-415f-95c3-ffbe784e736f")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_InvalidateStringRep(
        IntPtr objPtr
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to determine
    /// whether a command string is syntactically complete (i.e. has no unclosed
    /// braces, brackets, or quotes).
    /// </summary>
    /// <param name="cmd">
    /// The command string to be checked for completeness.
    /// </param>
    /// <returns>
    /// A non-zero value if the command is complete; otherwise, zero.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("40460f68-9fcb-4ada-ae0e-16bb5b89b3bb")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate int Tcl_CommandComplete(
        string cmd
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to permit
    /// exceptional return codes (e.g. break and continue) from the next script
    /// evaluation in an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be modified.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("dff11f2f-2cae-48d2-ab16-d73d4fe53c49")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_AllowExceptions(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to evaluate a Tcl
    /// object as a script.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter in which to evaluate the script.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object that contains the script to be
    /// evaluated.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the script is evaluated.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("b723e6be-f2bd-41d1-bfdc-35b62d8487e6")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_EvalObjEx(
        IntPtr interp,
        IntPtr objPtr,
        Tcl_EvalFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to read and
    /// evaluate the script contained in a file.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter in which to evaluate the script.
    /// </param>
    /// <param name="fileName">
    /// The name of the file that contains the script to be evaluated.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl,
        CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("ba110804-348f-4526-b713-0643fbeb3f3a")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_EvalFile(
        IntPtr interp,
        string fileName
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to record a script
    /// on the interpreter history list and then evaluate it.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter in which to evaluate the script.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object that contains the script to be
    /// recorded and evaluated.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the script is evaluated.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("46748f9e-39a7-4b27-b973-9f38375e31e0")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_RecordAndEvalObj(
        IntPtr interp,
        IntPtr objPtr,
        Tcl_EvalFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to evaluate a Tcl
    /// object as an expression.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter in which to evaluate the
    /// expression.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object that contains the expression to be
    /// evaluated.
    /// </param>
    /// <param name="resultPtr">
    /// Upon success, this parameter receives a native pointer to the Tcl object
    /// that contains the result of the expression.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("c66f58d1-8dce-426d-8ed4-110d839a203e")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_ExprObj(
        IntPtr interp,
        IntPtr objPtr,
        ref IntPtr resultPtr
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to perform
    /// substitutions (e.g. command, variable, and backslash substitution) on a
    /// Tcl object.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter that provides context for the
    /// substitution.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object on which to perform substitution.
    /// </param>
    /// <param name="flags">
    /// The flags that control which kinds of substitution are performed.
    /// </param>
    /// <returns>
    /// A native pointer to the Tcl object that contains the substituted result,
    /// or a null pointer on failure.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("7816b69a-04d9-446d-a807-6f2b3eccb1e0")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_SubstObj(
        IntPtr interp,
        IntPtr objPtr,
        Tcl_SubstFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to cancel the
    /// script evaluation that is currently in progress in an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter whose evaluation is to be
    /// canceled.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object that contains the optional
    /// cancellation result, or a null pointer.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data associated with the cancellation.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the evaluation is canceled.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("4c151fcb-2021-4696-b109-82740a3ac14a")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_CancelEval(
        IntPtr interp,
        IntPtr objPtr,
        IntPtr clientData,
        Tcl_EvalFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to determine
    /// whether script evaluation in an interpreter has been canceled.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter to be checked.
    /// </param>
    /// <param name="flags">
    /// The flags that control how the cancellation status is checked.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> if evaluation has not been canceled;
    /// otherwise, an appropriate error code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("4ccb610c-c706-4174-8845-40882a769177")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_Canceled(
        IntPtr interp,
        Tcl_CanceledFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to reset the script
    /// cancellation state of an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter whose cancellation state is to be
    /// reset.
    /// </param>
    /// <param name="force">
    /// A non-zero value to force the cancellation state to be reset even when an
    /// evaluation is still in progress; otherwise, zero.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
    /// code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("aa7e4ca0-137e-4fd6-b646-594ce1c43cfc")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode TclResetCancellation(
        IntPtr interp,
        int force
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // NOTE: Unfortunately, this exact terminology is needed to retain
    //       backward compatibility with native Tcl 8.6.0.  Please see:
    //
    //       https://urn.to/r/tcl_set_slave_cancel_flags
    //
    /// <summary>
    /// This delegate represents the native Tcl function used to set the script
    /// cancellation flags for an interpreter (e.g. a slave interpreter).
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter whose cancellation flags are to
    /// be set.
    /// </param>
    /// <param name="flags">
    /// The cancellation flags to be set.
    /// </param>
    /// <param name="force">
    /// A non-zero value to force the cancellation flags to be set; otherwise,
    /// zero.
    /// </param>
    [ObjectName("TclSetSlaveCancelFlags")]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("f670cfbd-cd3c-4361-a400-84d918b4e2b6")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void TclSetInterpCancelFlags(
        IntPtr interp,
        Tcl_EvalFlags flags,
        int force
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to process a single
    /// event from the Tcl event loop.
    /// </summary>
    /// <param name="flags">
    /// The flags that control which kinds of events are processed and whether
    /// the call blocks.
    /// </param>
    /// <returns>
    /// A non-zero value if an event was processed; otherwise, zero.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("e96819d8-9e94-4e12-9708-69ac8d6241f6")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate int Tcl_DoOneEvent(
        Tcl_EventFlags flags
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to reset the result
    /// of an interpreter to an empty string.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter whose result is to be reset.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("bcd291cc-88a3-49f8-b96f-7eccd8004b92")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_ResetResult(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to retrieve the
    /// result object of an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter whose result is to be retrieved.
    /// </param>
    /// <returns>
    /// A native pointer to the Tcl object that contains the interpreter result.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("7967b914-8506-462b-85b7-313e9696d9fe")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_GetObjResult(
        IntPtr interp
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to set the result
    /// object of an interpreter.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter whose result is to be set.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object to be used as the interpreter result.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("1be6de8f-eb23-436e-8eca-b46e1f175409")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_SetObjResult(
        IntPtr interp,
        IntPtr objPtr
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to retrieve the
    /// Unicode string representation of a Tcl object.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose Unicode string is to be
    /// retrieved.
    /// </param>
    /// <param name="length">
    /// Upon return, this parameter receives the length, in characters, of the
    /// returned string.
    /// </param>
    /// <returns>
    /// A native pointer to the Unicode string representation of the object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("45381916-0292-4a04-a796-39740ccbaa74")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_GetUnicodeFromObj(
        IntPtr objPtr,
        ref int length
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to retrieve the
    /// string representation of a Tcl object.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose string is to be retrieved.
    /// </param>
    /// <param name="length">
    /// Upon return, this parameter receives the length, in bytes, of the
    /// returned string.
    /// </param>
    /// <returns>
    /// A native pointer to the string representation of the object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("5869dee6-d6e0-4f18-b1c4-fa9d9529976f")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_GetStringFromObj(
        IntPtr objPtr,
        ref int length
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Dead Code
#if DEAD_CODE
    /// <summary>
    /// This delegate represents the native Tcl function used to obtain the
    /// byte array value, and its length, from a Tcl object.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object from which the byte array is to be
    /// obtained.
    /// </param>
    /// <param name="length">
    /// Upon success, receives the number of bytes in the returned byte array.
    /// </param>
    /// <returns>
    /// A native pointer to the first byte of the byte array value of the Tcl
    /// object.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("3c5bc4b9-9811-4661-8846-c8c4434fe595")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate IntPtr Tcl_GetByteArrayFromObj(
        IntPtr objPtr,
        ref int length
    );
#endif
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to register a
    /// callback to be invoked when the Tcl library is finalized (i.e. at exit).
    /// </summary>
    /// <param name="proc">
    /// The callback to be invoked at exit.
    /// </param>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data passed to the exit callback.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("373165e5-53e2-4010-a34d-bf0754e465be")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_CreateExitHandler(
        [MarshalAs(UnmanagedType.FunctionPtr)] Tcl_ExitProc proc,
        IntPtr clientData
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to unregister a
    /// previously registered exit callback.
    /// </summary>
    /// <param name="proc">
    /// The exit callback to be unregistered.
    /// </param>
    /// <param name="clientData">
    /// The opaque, caller-defined data that was supplied when the callback was
    /// registered.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("26e3d24c-ff29-42da-83ba-dda559fa919f")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_DeleteExitHandler(
        [MarshalAs(UnmanagedType.FunctionPtr)] Tcl_ExitProc proc,
        IntPtr clientData
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the exit callback invoked by the native Tcl
    /// runtime when the Tcl library is finalized (i.e. at exit).
    /// </summary>
    /// <param name="clientData">
    /// Optional, opaque, caller-defined data associated with the exit callback.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /* [SuppressUnmanagedCodeSecurity()] */
    [ObjectId("55665bde-0803-45f0-a9b4-f2ff64a76c73")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_ExitProc( /* ExitHandler */
        IntPtr clientData
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_THREADS
    /// <summary>
    /// This delegate represents the native Tcl function used to finalize the
    /// Tcl subsystem for the current thread.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("1b5d9342-3483-4d50-9698-d15eba34ebb1")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_FinalizeThread();
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the native Tcl function used to finalize the
    /// entire Tcl subsystem, releasing all of its resources.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity()]
    [ObjectId("db886667-832f-4c16-ad8d-1a96c85614fc")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_Finalize();

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Dead Code
#if DEAD_CODE
    /// <summary>
    /// This delegate represents the Tcl object type procedure used to set
    /// the internal representation of a Tcl object from any other type.
    /// </summary>
    /// <param name="interp">
    /// A native pointer to the Tcl interpreter, used for error reporting, or
    /// the invalid pointer if no error reporting is desired.
    /// </param>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose internal representation is to
    /// be set.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
    /// error code.
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /* [SuppressUnmanagedCodeSecurity()] */
    [ObjectId("6e3e4f6f-6971-40fd-8fc3-7cc4a51dba44")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_SetFromAnyProc( /* Tcl_ObjType */
        IntPtr interp,
        IntPtr objPtr
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the Tcl object type procedure used to
    /// regenerate the string representation of a Tcl object from its internal
    /// representation.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose string representation is to
    /// be updated.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /* [SuppressUnmanagedCodeSecurity()] */
    [ObjectId("f6b35209-2716-4212-8633-bee610169265")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_UpdateStringProc( /* Tcl_ObjType */
        IntPtr objPtr
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the Tcl object type procedure used to copy
    /// the internal representation from one Tcl object to another.
    /// </summary>
    /// <param name="srcPtr">
    /// A native pointer to the source Tcl object whose internal
    /// representation is to be copied.
    /// </param>
    /// <param name="dupPtr">
    /// A native pointer to the destination Tcl object that receives the
    /// copied internal representation.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /* [SuppressUnmanagedCodeSecurity()] */
    [ObjectId("a7813a7a-bda8-4cc6-b6fd-f95f0d2bc9da")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_DupInternalRepProc( /* Tcl_ObjType */
        IntPtr srcPtr,
        IntPtr dupPtr
    );

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This delegate represents the Tcl object type procedure used to free
    /// the internal representation of a Tcl object.
    /// </summary>
    /// <param name="objPtr">
    /// A native pointer to the Tcl object whose internal representation is to
    /// be freed.
    /// </param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    /* [SuppressUnmanagedCodeSecurity()] */
    [ObjectId("eec9d966-d58b-4255-9721-b0a62ae7a7bb")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate void Tcl_FreeInternalRepProc( /* Tcl_ObjType */
        IntPtr objPtr
    );
#endif
    #endregion
}
