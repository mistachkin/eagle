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
    /// <summary>
    /// This interface is implemented by the wrapper around a loaded native Tcl
    /// library.  It exposes information about the loaded library (its build,
    /// file name, module handle, and stubs table), the flags used when loading
    /// and unloading it, and the managed delegates bound to the native Tcl API
    /// entry points.  It also provides helper methods for validating native
    /// pointers and for managing the process exit handler.
    /// </summary>
    [ObjectId("23d9d7e3-d7be-477a-9016-daf3d0e596a2")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    interface ITclApi : ISynchronize, IGetInterpreter
    {
        /// <summary>
        /// Gets the build information describing the loaded native Tcl library.
        /// </summary>
        TclBuild Build { get; }

        /// <summary>
        /// Gets the file name of the loaded native Tcl library.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the native module handle of the loaded Tcl library.
        /// </summary>
        IntPtr Module { get; }

        /// <summary>
        /// Gets the native pointer to the Tcl stubs table, if any.
        /// </summary>
        IntPtr Stubs { get; }

        /// <summary>
        /// Gets the flags that were used when loading the native Tcl library.
        /// </summary>
        LoadFlags LoadFlags { get; }

        /// <summary>
        /// Gets the flags that will be used when unloading the native Tcl
        /// library.
        /// </summary>
        UnloadFlags UnloadFlags { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether exceptions raised while
        /// invoking the native Tcl API should be propagated.
        /// </summary>
        bool Exceptions { get; set; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of name/value pairs describing the state
        /// of this object.
        /// </summary>
        /// <param name="all">
        /// Non-zero to include all available information; otherwise, zero to
        /// include only a summary.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing the state of this object.
        /// </returns>
        StringPairList ToList(bool all);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this object.
        /// </summary>
        /// <param name="tclApi">
        /// Upon success, this will contain the newly created copy of this
        /// object.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Copy(ref ITclApi tclApi, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the specified native interpreter pointer
        /// is valid for use with this object.
        /// </summary>
        /// <param name="interp">
        /// The native Tcl interpreter pointer to validate.
        /// </param>
        /// <returns>
        /// True if the pointer is valid; otherwise, false.
        /// </returns>
        bool CheckInterp(IntPtr interp);

        /// <summary>
        /// This method verifies that the specified native interpreter pointer
        /// is valid for use with this object.
        /// </summary>
        /// <param name="interp">
        /// The native Tcl interpreter pointer to validate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the pointer is valid; otherwise, false.
        /// </returns>
        bool CheckInterp(IntPtr interp, ref Result error);

        /// <summary>
        /// This method verifies that the specified native Tcl object pointer is
        /// valid for use with this object.
        /// </summary>
        /// <param name="objPtr">
        /// The native Tcl object pointer to validate.
        /// </param>
        /// <returns>
        /// True if the pointer is valid; otherwise, false.
        /// </returns>
        bool CheckObjPtr(IntPtr objPtr);

        /// <summary>
        /// This method verifies that the specified native Tcl object pointer is
        /// valid for use with this object.
        /// </summary>
        /// <param name="objPtr">
        /// The native Tcl object pointer to validate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the pointer is valid; otherwise, false.
        /// </returns>
        bool CheckObjPtr(IntPtr objPtr, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the previously installed process exit handler.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ClearExitHandler(ref Result error);

        /// <summary>
        /// This method installs the process exit handler.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetExitHandler(ref Result error);

        /// <summary>
        /// This method removes the previously installed process exit handler.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnsetExitHandler(ref Result error);

        ///////////////////////////////////////////////////////////////////////

        #region Tcl API Delegates (managed function pointers)
        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_GetVersion entry
        /// point.
        /// </summary>
        Tcl_GetVersion GetVersion { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_FindExecutable
        /// entry point.
        /// </summary>
        Tcl_FindExecutable FindExecutable { get; }

#if TCL_KITS
        /// <summary>
        /// Gets the managed delegate bound to the native TclKit_SetKitPath
        /// entry point.
        /// </summary>
        TclKit_SetKitPath Kit_SetKitPath { get; }
#endif

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_CreateInterp entry
        /// point.
        /// </summary>
        Tcl_CreateInterp CreateInterp { get; }

#if TCL_KITS
        /// <summary>
        /// Gets the managed delegate bound to the native TclKit_AppInit entry
        /// point.
        /// </summary>
        TclKit_AppInit Kit_AppInit { get; }
#endif

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_Preserve entry
        /// point.
        /// </summary>
        Tcl_Preserve Preserve { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_Release entry
        /// point.
        /// </summary>
        Tcl_Release Release { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_ObjGetVar2 entry
        /// point.
        /// </summary>
        Tcl_ObjGetVar2 ObjGetVar2 { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_ObjSetVar2 entry
        /// point.
        /// </summary>
        Tcl_ObjSetVar2 ObjSetVar2 { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_UnsetVar2 entry
        /// point.
        /// </summary>
        Tcl_UnsetVar2 UnsetVar2 { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_Init entry point.
        /// </summary>
        Tcl_Init Init { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_InitMemory entry
        /// point.
        /// </summary>
        Tcl_InitMemory InitMemory { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_MakeSafe entry
        /// point.
        /// </summary>
        Tcl_MakeSafe MakeSafe { get; }

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_RegisterObjType
        /// entry point.
        /// </summary>
        Tcl_RegisterObjType RegisterObjType { get; } /* NOT USED */
#endif
        #endregion

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_GetObjType entry
        /// point.
        /// </summary>
        Tcl_GetObjType GetObjType { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_AppendAllObjTypes
        /// entry point.
        /// </summary>
        Tcl_AppendAllObjTypes AppendAllObjTypes { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_ConvertToType
        /// entry point.
        /// </summary>
        Tcl_ConvertToType ConvertToType { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_CreateObjCommand
        /// entry point.
        /// </summary>
        Tcl_CreateObjCommand CreateObjCommand { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native
        /// Tcl_DeleteCommandFromToken entry point.
        /// </summary>
        Tcl_DeleteCommandFromToken DeleteCommandFromToken { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_DeleteInterp entry
        /// point.
        /// </summary>
        Tcl_DeleteInterp DeleteInterp { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_InterpDeleted
        /// entry point.
        /// </summary>
        Tcl_InterpDeleted InterpDeleted { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_InterpActive entry
        /// point.
        /// </summary>
        Tcl_InterpActive InterpActive { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_GetErrorLine entry
        /// point.
        /// </summary>
        Tcl_GetErrorLine GetErrorLine { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_SetErrorLine entry
        /// point.
        /// </summary>
        Tcl_SetErrorLine SetErrorLine { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_NewObj entry
        /// point.
        /// </summary>
        Tcl_NewObj NewObj { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_NewUnicodeObj
        /// entry point.
        /// </summary>
        Tcl_NewUnicodeObj NewUnicodeObj { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_NewStringObj entry
        /// point.
        /// </summary>
        Tcl_NewStringObj NewStringObj { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_NewByteArrayObj
        /// entry point.
        /// </summary>
        Tcl_NewByteArrayObj NewByteArrayObj { get; }

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_DuplicateObj entry
        /// point.
        /// </summary>
        Tcl_DuplicateObj DuplicateObj { get; } /* NOT USED */
#endif
        #endregion

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_DbIncrRefCount
        /// entry point.
        /// </summary>
        Tcl_DbIncrRefCount DbIncrRefCount { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_DbDecrRefCount
        /// entry point.
        /// </summary>
        Tcl_DbDecrRefCount DbDecrRefCount { get; }

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_DbIsShared entry
        /// point.
        /// </summary>
        Tcl_DbIsShared DbIsShared { get; } /* NOT USED */

        /// <summary>
        /// Gets the managed delegate bound to the native
        /// Tcl_InvalidateStringRep entry point.
        /// </summary>
        Tcl_InvalidateStringRep InvalidateStringRep { get; } /* NOT USED */
#endif
        #endregion

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_CommandComplete
        /// entry point.
        /// </summary>
        Tcl_CommandComplete CommandComplete { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_AllowExceptions
        /// entry point.
        /// </summary>
        Tcl_AllowExceptions AllowExceptions { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_EvalObjEx entry
        /// point.
        /// </summary>
        Tcl_EvalObjEx EvalObjEx { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_EvalFile entry
        /// point.
        /// </summary>
        Tcl_EvalFile EvalFile { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_RecordAndEvalObj
        /// entry point.
        /// </summary>
        Tcl_RecordAndEvalObj RecordAndEvalObj { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_ExprObj entry
        /// point.
        /// </summary>
        Tcl_ExprObj ExprObj { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_SubstObj entry
        /// point.
        /// </summary>
        Tcl_SubstObj SubstObj { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_CancelEval entry
        /// point.
        /// </summary>
        Tcl_CancelEval CancelEval { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_Canceled entry
        /// point.
        /// </summary>
        Tcl_Canceled Canceled { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native TclResetCancellation
        /// entry point.
        /// </summary>
        TclResetCancellation ResetCancellation { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native
        /// TclSetInterpCancelFlags entry point.
        /// </summary>
        TclSetInterpCancelFlags SetInterpCancelFlags { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_DoOneEvent entry
        /// point.
        /// </summary>
        Tcl_DoOneEvent DoOneEvent { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_ResetResult entry
        /// point.
        /// </summary>
        Tcl_ResetResult ResetResult { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_GetObjResult entry
        /// point.
        /// </summary>
        Tcl_GetObjResult GetObjResult { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_SetObjResult entry
        /// point.
        /// </summary>
        Tcl_SetObjResult SetObjResult { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_GetUnicodeFromObj
        /// entry point.
        /// </summary>
        Tcl_GetUnicodeFromObj GetUnicodeFromObj { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_GetStringFromObj
        /// entry point.
        /// </summary>
        Tcl_GetStringFromObj GetStringFromObj { get; }

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Gets the managed delegate bound to the native
        /// Tcl_GetByteArrayFromObj entry point.
        /// </summary>
        Tcl_GetByteArrayFromObj GetByteArrayFromObj { get; } /* NOT USED */
#endif
        #endregion

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_CreateExitHandler
        /// entry point.
        /// </summary>
        Tcl_CreateExitHandler CreateExitHandler { get; }

        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_DeleteExitHandler
        /// entry point.
        /// </summary>
        Tcl_DeleteExitHandler DeleteExitHandler { get; }

#if TCL_THREADS
        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_FinalizeThread
        /// entry point.
        /// </summary>
        Tcl_FinalizeThread FinalizeThread { get; }
#endif

        //
        // NOTE: Without the underscore it clashes with the destructor.
        //
        /// <summary>
        /// Gets the managed delegate bound to the native Tcl_Finalize entry
        /// point.
        /// </summary>
        Tcl_Finalize _Finalize { get; }
        #endregion
    }
}
