/*
 * TclEnumerations.cs --
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

namespace Eagle._Components.Private.Tcl
{
    [ObjectId("b72e2625-f92c-451a-9536-bb55ee686701")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    enum Tcl_ReleaseLevel /* tcl.h */
    {
        TCL_UNKNOWN_RELEASE = -1,
        TCL_ALPHA_RELEASE = 0,
        TCL_BETA_RELEASE = 1,
        TCL_FINAL_RELEASE = 2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("93f0f813-6f32-4291-8481-5d226c8e395f")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_VarFlags /* tcl.h */
    {
        TCL_VAR_NONE = 0x0,
        TCL_GLOBAL_ONLY = 0x1,
        TCL_NAMESPACE_ONLY = 0x2,
        TCL_APPEND_VALUE = 0x4,
        TCL_LIST_ELEMENT = 0x8,
        TCL_LEAVE_ERR_MSG = 0x200,
        TCL_PARSE_PART1 = 0x400
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("803b80e9-ce6f-4f3f-a31f-5fc8110cb4f1")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_CanceledFlags /* tcl.h */
    {
        TCL_CANCEL_NONE = 0x0,       /* TIP #285 */
        TCL_LEAVE_ERR_MSG = 0x200,   /* TIP #285 */
        TCL_CANCEL_UNWIND = 0x100000 /* TIP #285 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("fb61388d-fd4d-4827-b82b-cdd77e60bc82")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    enum Tcl_InterpFlags /* tclInt.h */
    {
        DELETED = 0x1,
        ERR_IN_PROGRESS = 0x2,
        ERR_ALREADY_LOGGED = 0x4,
        ERROR_CODE_SET = 0x8,
        EXPR_INITIALIZED = 0x10,
        DONT_COMPILE_CMDS_INLINE = 0x20,
        RAND_SEED_INITIALIZED = 0x40,
        SAFE_INTERP = 0x80,
        USE_EVAL_DIRECT = 0x100,
        INTERP_TRACE_IN_PROGRESS = 0x200,
        INTERP_ALTERNATE_WRONG_ARGS = 0x400,
        ERR_LEGACY_COPY = 0x800,
        CANCELED = 0x1000 /* TIP #285 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("757aa35b-362e-42bb-8672-927d1841c1ff")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    enum Tcl_EventFlags /* tcl.h */
    {
        TCL_NO_EVENTS = 0,
        TCL_DONT_WAIT = (1 << 1),
        TCL_WINDOW_EVENTS = (1 << 2),
        TCL_FILE_EVENTS = (1 << 3),
        TCL_TIMER_EVENTS = (1 << 4),
        TCL_IDLE_EVENTS = (1 << 5),
        TCL_ALL_EVENTS = (~TCL_DONT_WAIT)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a64bf803-6725-459f-b5a4-10b0103da533")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_EvalFlags /* tcl.h */
    {
        TCL_EVAL_NONE = 0x0,
        TCL_BRACKET_TERM = 0x1,
        TCL_EVAL_FILE = 0x2,
        TCL_ALLOW_EXCEPTIONS = 0x4,
        TCL_EVAL_CTX = 0x8,
        TCL_EVAL_SOURCE_IN_FRAME = 0x10,
        TCL_EVAL_NORESOLVE = 0x20,
        TCL_NO_EVAL = 0x10000,
        TCL_EVAL_GLOBAL = 0x20000,
        TCL_EVAL_DIRECT = 0x40000,
        TCL_EVAL_INVOKE = 0x80000,
        TCL_CANCEL_UNWIND = 0x100000, /* TIP #285 */
        TCL_EVAL_NOERR = 0x200000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("4cacc717-9247-4e8e-afaa-e35becdb208c")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_SubstFlags /* tcl.h */
    {
        TCL_SUBST_NONE = 0x0,
        TCL_SUBST_COMMANDS = 0x1,
        TCL_SUBST_VARIABLES = 0x2,
        TCL_SUBST_BACKSLASHES = 0x4,
        TCL_SUBST_ALL = TCL_SUBST_COMMANDS |
                        TCL_SUBST_VARIABLES |
                        TCL_SUBST_BACKSLASHES
    }
}

