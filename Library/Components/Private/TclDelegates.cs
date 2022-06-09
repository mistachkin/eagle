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

    [ObjectId("35c49550-649e-4b41-873b-ceb88d6ab148")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    delegate ReturnCode Tcl_FindCallback(
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
