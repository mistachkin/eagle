/*
 * TclObjectType.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this code, it is a proof-of-concept only.  It is not production ready.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Eagle._Attributes;
using Eagle._Components.Private.Tcl.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private.Tcl
{
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    [ObjectId("f2fb7f27-df8e-41b4-9d4b-bdb6383a91fa")]
    internal sealed class TclObjectType
    {
        //
        // NOTE: Callers of this class are forbidden from attempting to register these
        //       object types with Tcl because they are considered "predefined" and
        //       should never normally need to be overridden.  This list will grow to
        //       include object types registered by other instances of this class.
        //
        private static StringList typeNames = new StringList();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode AddPredefindObjectTypes(
            Interpreter interpreter,
            IntPtr interp,
            ref Result error
            )
        {
            ReturnCode code;
            Result typeResult = null;

            //
            // NOTE: Get the object types registered in the Tcl interpreter.
            //
            code = TclWrapper.GetAllObjectTypes(
                interpreter.tclApi, interp, ref typeResult);

            if (code == ReturnCode.Ok)
            {
                StringList list = null;

                code = ParserOps<string>.SplitList(
                    interpreter, typeResult, 0, Length.Invalid,
                    false, ref list, ref error);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: For every object type currently registered in
                    //       the Tcl interpreter that we do not already know
                    //       about, add it to the list of object types that
                    //       cannot be registered.
                    //
                    foreach (string name in list)
                        if (!typeNames.Contains(name))
                            typeNames.Add(name);
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl Object Type Setup
        public static ReturnCode RegisterObjectType(
            Interpreter interpreter, /* in */
            string name,             /* in */
            IObjectType objectType,  /* in */
            ref Result error         /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (interpreter.tclApi != null)
                {
                    //
                    // NOTE: *WARNING* Empty Tcl object type names are allowed,
                    //       please do not change this to "!String.IsNullOrEmpty".
                    //
                    if (name != null)
                    {
                        //
                        // NOTE: Get the specified object type from Tcl.  If we are
                        //       registering this object type, it must not already
                        //       exist; otherwise, it must already exist.
                        //
                        IntPtr typePtr = interpreter.tclApi.GetObjType(name);

                        //
                        //
                        //
                        if (objectType != null)
                        {

                            if (typePtr == IntPtr.Zero)
                            {
                                Tcl_ObjType objType = null;

                                code = SetupObjectType(name, ref objType, ref error);

                                if (code == ReturnCode.Ok)
                                {




                                    interpreter.tclApi.RegisterObjType(ref objType);







                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "object type \"{0}\" already registered",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {

                            if (typePtr != IntPtr.Zero)
                            {

                                //
                                // NOTE: We want to "unregister" the object type.  Since
                                //       Tcl does not directly support this we will simply
                                //       make the specified object type a synonym for the
                                //       built-in "string" object type.
                                //
                                Tcl_ObjType objType = MarshalObjectType(typePtr);

                                IntPtr stringTypePtr = interpreter.tclApi.GetObjType("string");

                                if (stringTypePtr != IntPtr.Zero)
                                {
                                    Tcl_ObjType stringObjType = MarshalObjectType(stringTypePtr);





                                }
                                else
                                {

                                }


                            }
                            else
                            {
                                error = String.Format(
                                    "object type \"{0}\" is not registered",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {

                    }







                    code = ReturnCode.Ok;
                }
                else
                {
                    error = "invalid Tcl API object";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode SetupObjectType(
            string name,             /* in */
            ref Tcl_ObjType objType, /* in, out */
            ref Result error         /* out */
            )
        {
            try
            {
                if (objType == null)
                    objType = new Tcl_ObjType();

                if ((name != null) && (objType.name == IntPtr.Zero))
                    //
                    // NOTE: *WARNING* This string can NEVER be
                    //       freed while Tcl remains loaded.
                    //
                    objType.name =
                        Marshal.StringToCoTaskMemAnsi(name);

                if (objType.setFromAnyProc == null)
                    objType.setFromAnyProc =
                        new Tcl_SetFromAnyProc(SetFromAnyProc);

                if (objType.updateStringProc == null)
                    objType.updateStringProc =
                        new Tcl_UpdateStringProc(UpdateStringProc);

                if (objType.dupIntRepProc == null)
                    objType.dupIntRepProc =
                        new Tcl_DupInternalRepProc(DupInternalRepProc);

                if (objType.freeIntRepProc == null)
                    objType.freeIntRepProc =
                        new Tcl_FreeInternalRepProc(FreeInternalRepProc);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        #region Static Object Type Helpers
#if HAVE_SIZEOF
        public static Tcl_Obj MarshalObject(IntPtr objPtr)
        {
            Tcl_Obj result = null;

            try
            {
                if (objPtr != IntPtr.Zero)
                    result = (Tcl_Obj)Marshal.PtrToStructure(objPtr, typeof(Tcl_Obj));
            }
            catch
            {
                // do nothing.
            }

            return result;
        }
#endif
        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Tcl_ObjType MarshalObjectType(IntPtr typePtr)
        {
            Tcl_ObjType result = null;

            try
            {
                if (typePtr != IntPtr.Zero)
                    result = (Tcl_ObjType)Marshal.PtrToStructure(typePtr, typeof(Tcl_ObjType));
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if HAVE_SIZEOF
        public static Tcl_ObjType MarshalObjectType(Tcl_Obj obj)
        {
            Tcl_ObjType result = null;

            try
            {
                if (obj.typePtr != IntPtr.Zero)
                    result = MarshalObjectType(obj.typePtr);
            }
            catch
            {
                // do nothing.
            }

            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // **** WARNING ***** BEGIN CODE DIRECTLY CALLED BY THE NATIVE TCL RUNTIME ***** WARNING **** /
        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl Object Type Callbacks
        //
        // -- SetFromAnyProc --
        //
        // The setFromAnyProc member contains the address of a function called to create
        // a valid internal representation from an object's string representation. If an
        // internal representation can't be created from the string, it returns TCL_ERROR
        // and puts a message describing the error in the result object for interp unless
        // interp is NULL. If setFromAnyProc is successful, it stores the new internal
        // representation, sets objPtr's typePtr member to point to setFromAnyProc's
        // Tcl_ObjType, and returns TCL_OK. Before setting the new internal
        // representation, the setFromAnyProc must free any internal representation of
        // objPtr's old type; it does this by calling the old type's freeIntRepProc if it
        // is not NULL. As an example, the setFromAnyProc for the builtin Tcl integer
        // type gets an up-to-date string representation for objPtr by calling
        // Tcl_GetStringFromObj. It parses the string to obtain an integer and, if this
        // succeeds, stores the integer in objPtr's internal representation and sets
        // objPtr's typePtr member to point to the integer type's Tcl_ObjType structure.
        // Do not release objPtr's old internal representation unless you replace it with
        // a new one or reset the typePtr member to NULL.
        //
        private static ReturnCode SetFromAnyProc( /* Tcl_ObjType, REQUIRED */
            IntPtr interp, /* in */
            IntPtr objPtr  /* in, out */
            )
        {
            //ReturnCode code;

            //try
            //{
            //    Interpreter interpreter = Interpreter.GetActive();

            //    if (interpreter != null)
            //    {
            //        IntPtr value = IntPtr.Zero;
            //        IntPtr newObjTypePtr = IntPtr.Zero;
            //        Result error = null;

            //        Tcl_Obj obj = MarshalObject(objPtr);

            //        if (obj != null)
            //        {
            //            IntPtrList objTypePtrs =
            //                new IntPtrList(Interpreter.tclObjectTypes.Keys);

            //            if (NativeStack.FindNativeStackItem(objTypePtrs, ref newObjTypePtr))
            //            {
            //                IObjectType objectType =
            //                    Interpreter.tclObjectTypes[newObjTypePtr];

            //                if (objectType != null)
            //                {
            //                    string text =
            //                        TclWrapper.GetString(interpreter.tclApi, objPtr);

            //                    try
            //                    {
            //                        code = objectType.SetFromAny(
            //                            interpreter, text, ref value, ref error);
            //                    }
            //                    catch (Exception e)
            //                    {
            //                        error = e;
            //                        code = ReturnCode.Error;
            //                    }
            //                }
            //                else
            //                {
            //                    error = "invalid object type interface";
            //                    code = ReturnCode.Error;
            //                }
            //            }
            //            else
            //            {
            //                error = "cannot determine object type";
            //                code = ReturnCode.Error;
            //            }
            //        }
            //        else
            //        {
            //            error = "could not marshal object";
            //            code = ReturnCode.Error;
            //        }

            //        if (code == ReturnCode.Ok)
            //        {
            //            Tcl_ObjType oldObjType = MarshalObjectType(obj); /* may be null */

            //            //
            //            // NOTE: Free the old internal rep if necessary.  This must be done
            //            //       last because if we fail we leave it alone.
            //            //
            //            if ((oldObjType != null) && (oldObjType.freeIntRepProc != null))
            //                oldObjType.freeIntRepProc(objPtr);

            //            obj.otherValuePtr = value;
            //            obj.typePtr = newObjTypePtr;
            //        }
            //        else
            //        {
            //            //
            //            // NOTE: Set the Tcl interpreter result to the error.
            //            //
            //            if (!String.IsNullOrEmpty(error))
            //                TclWrapper.SetResult(interpreter.tclApi, interp,
            //                    TclWrapper.NewByteArray(interpreter.tclApi, error));
            //            else
            //                TclWrapper.ResetResult(interpreter.tclApi, interp);
            //        }
            //    }
            //    else
            //    {
            //        //
            //        // NOTE: Invalid interpreter, we cannot continue and there is no real
            //        //       way to report the error message to Tcl because we do not have
            //        //       a Tcl API object.
            //        //
            //        code = ReturnCode.Error;
            //    }
            //}
            //catch (Exception e)
            //{
            //    //
            //    // NOTE: Nothing we can do here except log the failure.
            //    //
            //    DebugOps.Trace(String.Format(
            //        "{0}: {1}{2}",
            //        GlobalState.GetCurrentThreadId(),
            //        e, Environment.NewLine),
            //        typeof(Tcl_SetFromAnyProc).Name);

            //    code = ReturnCode.Error;
            //}

            //return code;

            return ReturnCode.Ok; // FIXME: Stub.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        //
        // -- UpdateStringProc --
        //
        // The updateStringProc member contains the address of a function called to
        // create a valid string representation from an object's internal representation.
        // objPtr's bytes member is always NULL when it is called. It must always set
        // bytes non-NULL before returning. We require the string representation's byte
        // array to have a null after the last byte, at offset length; this allows string
        // representations that do not contain null bytes to be treated as conventional
        // null character-terminated C strings. Storage for the byte array must be
        // allocated in the heap by Tcl_Alloc or ckalloc. Note that updateStringProcs
        // must allocate enough storage for the string's bytes and the terminating null
        // byte. The updateStringProc for Tcl's builtin list type, for example, builds an
        // array of strings for each element object and then calls Tcl_Merge to construct
        // a string with proper Tcl list structure. It stores this string as the list
        // object's string representation.
        //
        private static void UpdateStringProc( /* Tcl_ObjType, REQUIRED */
            IntPtr objPtr /* in, out */
            )
        {
            return; // FIXME: Stub.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        //
        // -- DupInternalRepProc --
        //
        // The dupIntRepProc member contains the address of a function called to copy an
        // internal representation from one object to another. dupPtr's internal
        // representation is made a copy of srcPtr's internal representation. Before the
        // call, srcPtr's internal representation is valid and dupPtr's is not. srcPtr's
        // object type determines what copying its internal representation means. For
        // example, the dupIntRepProc for the Tcl integer type simply copies an integer.
        // The builtin list type's dupIntRepProc allocates a new array that points at the
        // original element objects; the elements are shared between the two lists (and
        // their reference counts are incremented to reflect the new references).
        //
        private static void DupInternalRepProc( /* Tcl_ObjType, OPTIONAL */
            IntPtr srcPtr, /* in */
            IntPtr dupPtr  /* out */
            )
        {
            return; // FIXME: Stub.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        //
        // -- FreeInternalRepProc --
        //
        // The freeIntRepProc member contains the address of a function that is called
        // when an object is freed. The freeIntRepProc function can deallocate the
        // storage for the object's internal representation and do other type-specific
        // processing necessary when an object is freed. For example, Tcl list objects
        // have an internalRep.otherValuePtr that points to an array of pointers to each
        // element in the list. The list type's freeIntRepProc decrements the reference
        // count for each element object (since the list will no longer refer to those
        // objects), then deallocates the storage for the array of pointers. The
        // freeIntRepProc member can be set to NULL to indicate that the internal
        // representation does not require freeing.
        //
        private static void FreeInternalRepProc( /* Tcl_ObjType, OPTIONAL */
            IntPtr objPtr /* in, out */
            )
        {
            return; // FIXME: Stub.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ***** WARNING ***** END CODE DIRECTLY CALLED BY THE NATIVE TCL RUNTIME ***** WARNING ***** /
        ///////////////////////////////////////////////////////////////////////////////////////////////
    }
}
