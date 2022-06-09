/*
 * TclApi.cs --
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
using Eagle._Components.Private.Tcl;
using Eagle._Components.Private.Tcl.Delegates;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private.Tcl
{
    [ObjectId("23d9d7e3-d7be-477a-9016-daf3d0e596a2")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    interface ITclApi : ISynchronize, IGetInterpreter
    {
        TclBuild Build { get; }
        string FileName { get; }
        IntPtr Module { get; }
        IntPtr Stubs { get; }
        LoadFlags LoadFlags { get; }
        UnloadFlags UnloadFlags { get; }

        ///////////////////////////////////////////////////////////////////////

        bool Exceptions { get; set; }

        ///////////////////////////////////////////////////////////////////////

        StringPairList ToList(bool all);

        ///////////////////////////////////////////////////////////////////////

        ReturnCode Copy(ref ITclApi tclApi, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        bool CheckInterp(IntPtr interp);
        bool CheckInterp(IntPtr interp, ref Result error);

        bool CheckObjPtr(IntPtr objPtr);
        bool CheckObjPtr(IntPtr objPtr, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        ReturnCode ClearExitHandler(ref Result error);
        ReturnCode SetExitHandler(ref Result error);
        ReturnCode UnsetExitHandler(ref Result error);

        ///////////////////////////////////////////////////////////////////////

        #region Tcl API Delegates (managed function pointers)
        Tcl_GetVersion GetVersion { get; }
        Tcl_FindExecutable FindExecutable { get; }

#if TCL_KITS
        TclKit_SetKitPath Kit_SetKitPath { get; }
#endif

        Tcl_CreateInterp CreateInterp { get; }

#if TCL_KITS
        TclKit_AppInit Kit_AppInit { get; }
#endif

        Tcl_Preserve Preserve { get; }
        Tcl_Release Release { get; }
        Tcl_ObjGetVar2 ObjGetVar2 { get; }
        Tcl_ObjSetVar2 ObjSetVar2 { get; }
        Tcl_UnsetVar2 UnsetVar2 { get; }
        Tcl_Init Init { get; }
        Tcl_InitMemory InitMemory { get; }
        Tcl_MakeSafe MakeSafe { get; }

        #region Dead Code
#if DEAD_CODE
        Tcl_RegisterObjType RegisterObjType { get; } /* NOT USED */
#endif
        #endregion

        Tcl_GetObjType GetObjType { get; }
        Tcl_AppendAllObjTypes AppendAllObjTypes { get; }
        Tcl_ConvertToType ConvertToType { get; }
        Tcl_CreateObjCommand CreateObjCommand { get; }
        Tcl_DeleteCommandFromToken DeleteCommandFromToken { get; }
        Tcl_DeleteInterp DeleteInterp { get; }
        Tcl_InterpDeleted InterpDeleted { get; }
        Tcl_InterpActive InterpActive { get; }
        Tcl_GetErrorLine GetErrorLine { get; }
        Tcl_SetErrorLine SetErrorLine { get; }
        Tcl_NewObj NewObj { get; }
        Tcl_NewUnicodeObj NewUnicodeObj { get; }
        Tcl_NewStringObj NewStringObj { get; }
        Tcl_NewByteArrayObj NewByteArrayObj { get; }

        #region Dead Code
#if DEAD_CODE
        Tcl_DuplicateObj DuplicateObj { get; } /* NOT USED */
#endif
        #endregion

        Tcl_DbIncrRefCount DbIncrRefCount { get; }
        Tcl_DbDecrRefCount DbDecrRefCount { get; }

        #region Dead Code
#if DEAD_CODE
        Tcl_DbIsShared DbIsShared { get; } /* NOT USED */
        Tcl_InvalidateStringRep InvalidateStringRep { get; } /* NOT USED */
#endif
        #endregion

        Tcl_CommandComplete CommandComplete { get; }
        Tcl_AllowExceptions AllowExceptions { get; }
        Tcl_EvalObjEx EvalObjEx { get; }
        Tcl_EvalFile EvalFile { get; }
        Tcl_RecordAndEvalObj RecordAndEvalObj { get; }
        Tcl_ExprObj ExprObj { get; }
        Tcl_SubstObj SubstObj { get; }
        Tcl_CancelEval CancelEval { get; }
        Tcl_Canceled Canceled { get; }
        TclResetCancellation ResetCancellation { get; }
        TclSetInterpCancelFlags SetInterpCancelFlags { get; }
        Tcl_DoOneEvent DoOneEvent { get; }
        Tcl_ResetResult ResetResult { get; }
        Tcl_GetObjResult GetObjResult { get; }
        Tcl_SetObjResult SetObjResult { get; }
        Tcl_GetUnicodeFromObj GetUnicodeFromObj { get; }
        Tcl_GetStringFromObj GetStringFromObj { get; }

        #region Dead Code
#if DEAD_CODE
        Tcl_GetByteArrayFromObj GetByteArrayFromObj { get; } /* NOT USED */
#endif
        #endregion

        Tcl_CreateExitHandler CreateExitHandler { get; }
        Tcl_DeleteExitHandler DeleteExitHandler { get; }

#if TCL_THREADS
        Tcl_FinalizeThread FinalizeThread { get; }
#endif

        //
        // NOTE: Without the underscore it clashes with the destructor.
        //
        Tcl_Finalize _Finalize { get; }
        #endregion
    }
}
