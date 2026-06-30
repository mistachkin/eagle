/*
 * TclStructs.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Private.Tcl.Delegates;

namespace Eagle._Components.Private.Tcl
{
    #region Tcl Object Type
    /// <summary>
    /// This class represents the native Tcl_ObjType structure, which describes
    /// an object type known to the Tcl library.  It holds the type name along
    /// with the set of function pointers used to manage the internal
    /// representation of objects of that type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [ObjectId("acd575c7-a753-4bdf-bedb-73307ee1f6af")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class Tcl_ObjType
    {
        //
        // The name member describes the name of the type, e.g. int. Extension writers
        // can look up an object type using its name with the Tcl_GetObjType procedure.
        //
        /// <summary>
        /// The name of the type, e.g. int.  Extension writers can look up an
        /// object type using its name with the Tcl_GetObjType procedure.
        /// </summary>
        public IntPtr name;

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl_FreeInternalRepProc
        //
        // -- Tcl_FreeInternalRepProc --
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
        /// <summary>
        /// The address of a function that is called when an object is freed, in
        /// order to deallocate the storage for the object's internal
        /// representation and perform any other type-specific processing; it
        /// may be null to indicate that the internal representation does not
        /// require freeing.
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public Tcl_FreeInternalRepProc freeIntRepProc;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl_DupInternalRepProc
        //
        // -- Tcl_DupInternalRepProc --
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
        /// <summary>
        /// The address of a function called to copy an internal representation
        /// from one object to another.  What copying the internal
        /// representation means is determined by the object type.
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public Tcl_DupInternalRepProc dupIntRepProc;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl_UpdateStringProc
        //
        // -- Tcl_UpdateStringProc --
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
        /// <summary>
        /// The address of a function called to create a valid string
        /// representation from an object's internal representation.  It must
        /// always set the object's bytes member non-null, terminated by a null
        /// byte, before returning.
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public Tcl_UpdateStringProc updateStringProc;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl_SetFromAnyProc
        //
        // -- Tcl_SetFromAnyProc --
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
        /// <summary>
        /// The address of a function called to create a valid internal
        /// representation from an object's string representation.  On success
        /// it stores the new internal representation and points the object's
        /// typePtr member at this type; on failure it reports an error.
        /// </summary>
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public Tcl_SetFromAnyProc setFromAnyProc;
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Tcl Object /* NON-PORTABLE */
#if HAVE_SIZEOF
    /// <summary>
    /// This class represents the native Tcl_Obj structure, the in-memory form
    /// of a Tcl value.  It carries a reference count, an optional string
    /// representation, and a type-specific internal representation laid out as
    /// an explicit union.  This layout is non-portable and is only available
    /// when the size of native types can be computed.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [ObjectId("6cbb5288-adbd-4f3e-8360-0235a336e3c3")]
    internal sealed class Tcl_Obj
    {
        /// <summary>
        /// The reference count of the object; when it reaches zero the object
        /// will be freed.
        /// </summary>
        [FieldOffset(0)]
        public int refCount;            /* When 0 the object will be freed. */
        /// <summary>
        /// Points to the first byte of the object's string representation, or
        /// null when the string representation is invalid and must be
        /// regenerated from the internal representation.
        /// </summary>
        [FieldOffset(sizeof(int))]
        public IntPtr bytes;            /* This points to the first byte of the
                                         * object's string representation. The array
                                         * must be followed by a null byte (i.e., at
                                         * offset length) but may also contain
                                         * embedded null characters. The array's
                                         * storage is allocated by ckalloc. NULL
                                         * means the string rep is invalid and must
                                         * be regenerated from the internal rep.
                                         * Clients should use Tcl_GetStringFromObj
                                         * or Tcl_GetString to get a pointer to the
                                         * byte array as a readonly value. */
        /// <summary>
        /// The number of bytes at the location referenced by the bytes member,
        /// not including the terminating null.
        /// </summary>
        [FieldOffset(sizeof(int) + Build.SizeOfIntPtr)]
        public int length;              /* The number of bytes at *bytes, not
                                         * including the terminating null. */
        /// <summary>
        /// Denotes the object's type; corresponds to the type of the object's
        /// internal representation, or null when the object has no internal
        /// representation (has no type).
        /// </summary>
        [FieldOffset((sizeof(int) * 2) + Build.SizeOfIntPtr)]
        public IntPtr typePtr;          /* Denotes the object's type. Always
                                         * corresponds to the type of the object's
                                         * internal rep. NULL indicates the object
                                         * has no internal rep (has no type). */
        /* union {                       * The internal representation: */
        /// <summary>
        /// The internal representation as a long integer value (at least
        /// 32 bits wide).
        /// </summary>
        [FieldOffset((sizeof(int) * 2) + (Build.SizeOfIntPtr * 2))]
        public int longValue;           /* - an long integer value (>= 32-bits)*/
        /// <summary>
        /// The internal representation as a double-precision floating point
        /// value.
        /// </summary>
        [FieldOffset((sizeof(int) * 2) + (Build.SizeOfIntPtr * 2))]
        public double doubleValue;      /* - a double-precision floating value */
        /// <summary>
        /// The internal representation as another, type-specific value.
        /// </summary>
        [FieldOffset((sizeof(int) * 2) + (Build.SizeOfIntPtr * 2))]
        public IntPtr otherValuePtr;    /* - another, type-specific value */
        /// <summary>
        /// The internal representation as a long long value (at least 64 bits
        /// wide).
        /// </summary>
        [FieldOffset((sizeof(int) * 2) + (Build.SizeOfIntPtr * 2))]
        public long wideValue;          /* - a long long value (>= 64-bits) */
        /// <summary>
        /// The first of two pointers comprising the internal representation.
        /// </summary>
        [FieldOffset((sizeof(int) * 2) + (Build.SizeOfIntPtr * 2))]
        IntPtr ptr1;                    /* - internal rep as two pointers */
        /// <summary>
        /// The second of two pointers comprising the internal representation.
        /// </summary>
        [FieldOffset((sizeof(int) * 2) + (Build.SizeOfIntPtr * 2) + sizeof(long))]
        IntPtr ptr2;
        /* } internalRep;               /* End of internal representation. */
    }
#endif
    #endregion
}
