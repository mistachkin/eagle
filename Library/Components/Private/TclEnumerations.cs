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
    /// <summary>
    /// This enumeration represents the release level of Tcl (e.g. alpha,
    /// beta, or final), as defined by tcl.h.
    /// </summary>
    [ObjectId("b72e2625-f92c-451a-9536-bb55ee686701")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    enum Tcl_ReleaseLevel /* tcl.h */
    {
        /// <summary>
        /// The release level is unknown.
        /// </summary>
        TCL_UNKNOWN_RELEASE = -1,

        /// <summary>
        /// The release is an alpha release.
        /// </summary>
        TCL_ALPHA_RELEASE = 0,

        /// <summary>
        /// The release is a beta release.
        /// </summary>
        TCL_BETA_RELEASE = 1,

        /// <summary>
        /// The release is a final release.
        /// </summary>
        TCL_FINAL_RELEASE = 2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration contains flags used to control variable lookup and
    /// access operations, as defined by tcl.h.
    /// </summary>
    [Flags()]
    [ObjectId("93f0f813-6f32-4291-8481-5d226c8e395f")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_VarFlags /* tcl.h */
    {
        /// <summary>
        /// No special variable flags are set.
        /// </summary>
        TCL_VAR_NONE = 0x0,

        /// <summary>
        /// The variable is to be looked up in the global namespace only.
        /// </summary>
        TCL_GLOBAL_ONLY = 0x1,

        /// <summary>
        /// The variable is to be looked up in the current namespace only.
        /// </summary>
        TCL_NAMESPACE_ONLY = 0x2,

        /// <summary>
        /// The value is to be appended to the existing value of the variable.
        /// </summary>
        TCL_APPEND_VALUE = 0x4,

        /// <summary>
        /// The value is to be appended as a list element to the existing value
        /// of the variable.
        /// </summary>
        TCL_LIST_ELEMENT = 0x8,

        /// <summary>
        /// An error message is to be left in the interpreter result upon
        /// failure.
        /// </summary>
        TCL_LEAVE_ERR_MSG = 0x200,

        /// <summary>
        /// The first part of the variable name is to be parsed for array
        /// element notation.
        /// </summary>
        TCL_PARSE_PART1 = 0x400
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration contains flags used when querying the script
    /// cancellation state of a Tcl interpreter (TIP #285), as defined by
    /// tcl.h.
    /// </summary>
    [Flags()]
    [ObjectId("803b80e9-ce6f-4f3f-a31f-5fc8110cb4f1")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_CanceledFlags /* tcl.h */
    {
        /// <summary>
        /// No script cancellation flags are set.
        /// </summary>
        TCL_CANCEL_NONE = 0x0,       /* TIP #285 */

        /// <summary>
        /// An error message is to be left in the interpreter result upon
        /// failure.
        /// </summary>
        TCL_LEAVE_ERR_MSG = 0x200,   /* TIP #285 */

        /// <summary>
        /// The script in progress is to be unwound completely as part of the
        /// cancellation.
        /// </summary>
        TCL_CANCEL_UNWIND = 0x100000 /* TIP #285 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration contains internal flags that reflect the current state
    /// of a Tcl interpreter, as defined by tclInt.h.
    /// </summary>
    [Flags()]
    [ObjectId("fb61388d-fd4d-4827-b82b-cdd77e60bc82")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    enum Tcl_InterpFlags /* tclInt.h */
    {
        /// <summary>
        /// The interpreter has been deleted.
        /// </summary>
        DELETED = 0x1,

        /// <summary>
        /// An error is currently in progress within the interpreter.
        /// </summary>
        ERR_IN_PROGRESS = 0x2,

        /// <summary>
        /// The error information has already been logged for the current
        /// error.
        /// </summary>
        ERR_ALREADY_LOGGED = 0x4,

        /// <summary>
        /// The error code has been set for the current error.
        /// </summary>
        ERROR_CODE_SET = 0x8,

        /// <summary>
        /// The expression subsystem has been initialized.
        /// </summary>
        EXPR_INITIALIZED = 0x10,

        /// <summary>
        /// Commands should not be compiled inline.
        /// </summary>
        DONT_COMPILE_CMDS_INLINE = 0x20,

        /// <summary>
        /// The random number seed has been initialized.
        /// </summary>
        RAND_SEED_INITIALIZED = 0x40,

        /// <summary>
        /// The interpreter is a safe interpreter.
        /// </summary>
        SAFE_INTERP = 0x80,

        /// <summary>
        /// Scripts should be evaluated directly, without being compiled.
        /// </summary>
        USE_EVAL_DIRECT = 0x100,

        /// <summary>
        /// An interpreter trace is currently in progress.
        /// </summary>
        INTERP_TRACE_IN_PROGRESS = 0x200,

        /// <summary>
        /// The alternate wrong-number-of-arguments message format is to be
        /// used.
        /// </summary>
        INTERP_ALTERNATE_WRONG_ARGS = 0x400,

        /// <summary>
        /// The legacy error information is to be copied for compatibility.
        /// </summary>
        ERR_LEGACY_COPY = 0x800,

        /// <summary>
        /// The script in progress has been canceled (TIP #285).
        /// </summary>
        CANCELED = 0x1000 /* TIP #285 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration contains flags used to control which kinds of events
    /// are processed by the Tcl event loop, as defined by tcl.h.
    /// </summary>
    [Flags()]
    [ObjectId("757aa35b-362e-42bb-8672-927d1841c1ff")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    enum Tcl_EventFlags /* tcl.h */
    {
        /// <summary>
        /// No events are to be processed.
        /// </summary>
        TCL_NO_EVENTS = 0,

        /// <summary>
        /// The event loop should not block while waiting for events.
        /// </summary>
        TCL_DONT_WAIT = (1 << 1),

        /// <summary>
        /// Windowing system events are to be processed.
        /// </summary>
        TCL_WINDOW_EVENTS = (1 << 2),

        /// <summary>
        /// File events are to be processed.
        /// </summary>
        TCL_FILE_EVENTS = (1 << 3),

        /// <summary>
        /// Timer events are to be processed.
        /// </summary>
        TCL_TIMER_EVENTS = (1 << 4),

        /// <summary>
        /// Idle events are to be processed.
        /// </summary>
        TCL_IDLE_EVENTS = (1 << 5),

        /// <summary>
        /// All kinds of events are to be processed.
        /// </summary>
        TCL_ALL_EVENTS = (~TCL_DONT_WAIT)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration contains flags used to control the evaluation of
    /// scripts and commands, as defined by tcl.h.
    /// </summary>
    [Flags()]
    [ObjectId("a64bf803-6725-459f-b5a4-10b0103da533")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_EvalFlags /* tcl.h */
    {
        /// <summary>
        /// No special evaluation flags are set.
        /// </summary>
        TCL_EVAL_NONE = 0x0,

        /// <summary>
        /// Evaluation is to terminate at a close bracket.
        /// </summary>
        TCL_BRACKET_TERM = 0x1,

        /// <summary>
        /// The script is being evaluated from a file.
        /// </summary>
        TCL_EVAL_FILE = 0x2,

        /// <summary>
        /// Exceptional return codes (other than error) are permitted during
        /// evaluation.
        /// </summary>
        TCL_ALLOW_EXCEPTIONS = 0x4,

        /// <summary>
        /// The script is to be evaluated using the supplied context.
        /// </summary>
        TCL_EVAL_CTX = 0x8,

        /// <summary>
        /// Source information is to be recorded in the call frame.
        /// </summary>
        TCL_EVAL_SOURCE_IN_FRAME = 0x10,

        /// <summary>
        /// Command and variable resolvers are to be bypassed during
        /// evaluation.
        /// </summary>
        TCL_EVAL_NORESOLVE = 0x20,

        /// <summary>
        /// The script is to be substituted but not evaluated.
        /// </summary>
        TCL_NO_EVAL = 0x10000,

        /// <summary>
        /// The script is to be evaluated in the global namespace.
        /// </summary>
        TCL_EVAL_GLOBAL = 0x20000,

        /// <summary>
        /// The script is to be evaluated directly, without being compiled.
        /// </summary>
        TCL_EVAL_DIRECT = 0x40000,

        /// <summary>
        /// The command is to be invoked directly, without further
        /// substitution.
        /// </summary>
        TCL_EVAL_INVOKE = 0x80000,

        /// <summary>
        /// The evaluation is to be unwound completely upon cancellation
        /// (TIP #285).
        /// </summary>
        TCL_CANCEL_UNWIND = 0x100000, /* TIP #285 */

        /// <summary>
        /// No error information is to be added to the interpreter upon
        /// failure.
        /// </summary>
        TCL_EVAL_NOERR = 0x200000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration contains flags used to control which kinds of
    /// substitutions are performed on a string, as defined by tcl.h.
    /// </summary>
    [Flags()]
    [ObjectId("4cacc717-9247-4e8e-afaa-e35becdb208c")]
    /* NOTE: Always public, used via Interpreter interface. */
    public enum Tcl_SubstFlags /* tcl.h */
    {
        /// <summary>
        /// No substitutions are to be performed.
        /// </summary>
        TCL_SUBST_NONE = 0x0,

        /// <summary>
        /// Command substitutions are to be performed.
        /// </summary>
        TCL_SUBST_COMMANDS = 0x1,

        /// <summary>
        /// Variable substitutions are to be performed.
        /// </summary>
        TCL_SUBST_VARIABLES = 0x2,

        /// <summary>
        /// Backslash substitutions are to be performed.
        /// </summary>
        TCL_SUBST_BACKSLASHES = 0x4,

        /// <summary>
        /// All kinds of substitutions are to be performed.
        /// </summary>
        TCL_SUBST_ALL = TCL_SUBST_COMMANDS |
                        TCL_SUBST_VARIABLES |
                        TCL_SUBST_BACKSLASHES
    }
}

