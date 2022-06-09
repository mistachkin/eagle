/*
 * NativePackage.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Private.Tcl;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private.Tcl;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Public
{
    [ObjectId("2e8eae65-3e12-4eb9-8695-c871cc23a57a")]
    public static class NativePackage
    {
        #region Private Constants
        //
        // NOTE: This is the name of the native command in the Tcl interpreter
        //       that will be bridged to the Eagle interpreter (i.e. the native
        //       endpoint).
        //
        private static readonly string nativeCommandName =
            GlobalState.GetPackageNameNoCase();

        //
        // NOTE: This is the name of the managed command in the Eagle
        //       interpreter that will be bridged to the Tcl interpreter
        //       (i.e. the managed endpoint).
        //
        private static readonly string managedCommandName =
            ScriptOps.TypeNameToEntityName(typeof(_Commands.Eval));

        //
        // NOTE: These name prefixes are used when building the script-visible
        //       names for the Tcl interpreters used by this class.  Also, see
        //       the constants tclParentInterpPrefix, tclSafeInterpPrefix, and
        //       tclInterpPrefix in the Interpreter class.
        //
        private const string tclNativeParentInterpPrefix = "nativeParentInterp";
        private const string tclNativeSafeParentInterpPrefix = "nativeSafeParentInterp";
        private const string tclNativeInterpPrefix = "nativeInterp";
        private const string tclNativeSafeInterpPrefix = "nativeSafeInterp";

        //
        // NOTE: In the argument string passed from native code, we need at
        //       least the module handle, the Tcl interpreter pointer, and the
        //       Tcl interpreter safety indicator.
        //
        private const int MinimumArgumentCountV1R1 = 4;
        private const int MinimumArgumentCountV1R2 = MinimumArgumentCountV1R1 + 2;

        //
        // NOTE: The possible protocol identifier strings that we should expect
        //       in the argument string passed from native code.
        //
        private const string ProtocolIdV1R0 = "Garuda_v1.0"; /* LEGACY */
        private const string ProtocolIdV1R1 = "Garuda_v1.0_r1.0";
        private const string ProtocolIdV1R2 = "Garuda_v1.0_r2.0";

        //
        // NOTE: These are the error messages returned when the string argument
        //       cannot be parsed properly according to the selected protocol
        //       version (i.e. not a list, not enough sub-arguments, etc).
        //
        private const string ParseArgumentErrorV1R1 = "could not parse " +
            "argument string, expected at least [{0} <IntPtr> <IntPtr> " +
            "<Boolean>]: {1}";

        private const string ParseArgumentErrorV1R2 = "could not parse " +
            "argument string, expected at least [{0} <IntPtr> <IntPtr> " +
            "<IntPtr> <Boolean> <Boolean>]: {1}";

        //
        // NOTE: This is the error message returned when the "safe" mode of the
        //       Eagle interpreter is unsuitable for the "safe" mode of the Tcl
        //       interpreter.
        //
        private const string SafeUnsafeError = "cannot use safe Tcl " +
            "interpreter {0} with unsafe Eagle interpreter {1}";

        //
        // NOTE: This is the error message returned when the Tcl interpreter
        //       has already been attached to the bridge.
        //
        private const string AlreadyAttachedError = "cannot attach Tcl " +
            "interpreter {0} to Eagle interpreter {1}, already attached";

        //
        // NOTE: This is the error message returned when the Tcl interpreter
        //       cannot be detached from the bridge.
        //
        private const string CouldNotDetachError = "could not detach Tcl " +
            "interpreter {0} from Eagle interpreter {1}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This object is used with the lock statement to protect
        //       access to the other static fields.
        //
        private static readonly object syncRoot = new object();

        //
        // NOTE: This is the number of outstanding method calls into methods
        //       that are called from native code (e.g. "Startup", "Control",
        //       "Detach", and "Shutdown").  All of these methods are public.
        //
        private static int activeCount;

        //
        // NOTE: The Eagle interpreters holding the Tcl integration components
        //       created and used by this class.
        //
        private static IntPtrInterpreterDictionary interpreters;

        //
        // NOTE: The list of Tcl interpreters we know originated outside the
        //       direct control of Eagle (i.e. we should not try to delete
        //       them).
        //
        private static IntPtrDictionary tclInterps = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Non-Public Methods
        #region Private Tcl Interpreter Management Methods
        private static string GetTclInterpreterPrefix(
            bool parent,
            bool safe
            )
        {
            if (parent)
            {
                return safe ? tclNativeSafeParentInterpPrefix :
                    tclNativeParentInterpPrefix;
            }
            else
            {
                return safe ? tclNativeSafeInterpPrefix :
                    tclNativeInterpPrefix;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetTclInterpreterName(
            bool isolated,
            bool safe
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                string prefix = GetTclInterpreterPrefix(
                    isolated || (tclInterps.Count == 0), safe);

                return FormatOps.Id(prefix, null, GlobalState.NextId());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetTclBridgeName(
            string interpName,
            string commandName
            )
        {
            return FormatOps.TclBridgeName(interpName, commandName);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasTclInterpreters(
            bool validate
            )
        {
            Result error = null;

            return HasTclInterpreters(validate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasTclInterpreters(
            bool validate,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool result = (tclInterps != null);

                if (!result)
                    error = "no Tcl interpreters available";

                if (result && validate)
                {
                    int count = 0;

                    foreach (KeyValuePair<string, IntPtr> pair in
                            tclInterps) /* O(N) */
                    {
                        //
                        // TODO: Also check if deleted here?
                        //
                        if (pair.Value != IntPtr.Zero)
                            count++;
                    }

                    result = (count > 0);

                    if (!result)
                        error = "no valid Tcl interpreters available";
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasInterpreters(
            bool validate
            )
        {
            Result error = null;

            return HasInterpreters(validate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasInterpreters(
            bool validate,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool result = (interpreters != null);

                if (!result)
                    error = "no Eagle interpreters available";

                if (result && validate)
                {
                    int count = 0;

                    foreach (KeyValuePair<IntPtr, Interpreter> pair in
                            interpreters) /* O(N) */
                    {
                        //
                        // TODO: Also check if disposed here?
                        //
                        if (pair.Value != null)
                            count++;
                    }

                    result = (count > 0);

                    if (!result)
                        error = "no valid Eagle interpreters available";
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the DebugTclInterpreters method only.
        //
        private static ReturnCode GetInterpreter(
            IntPtrInterpreterDictionary interpreters,
            IntPtr interp,
            ref IntPtr parentInterp,
            ref Interpreter interpreter
            )
        {
            if (interpreters == null)
                return ReturnCode.Error;

            ///////////////////////////////////////////////////////////////

            Interpreter localInterpreter = null;
            IntPtr localParentInterp = IntPtr.Zero;
            bool found = false;

            foreach (KeyValuePair<IntPtr, Interpreter> pair in
                    interpreters) /* O(N) */
            {
                localInterpreter = pair.Value;

                if (TclApi.HasInterp(localInterpreter, null, interp))
                {
                    localParentInterp = pair.Key;
                    found = true;

                    break;
                }
            }

            if (found)
            {
                parentInterp = localParentInterp;
                interpreter = localInterpreter;

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the DebugTclInterpreters method only.
        //
        private static void SnapshotAllInterpreters(
            out IntPtrDictionary tclInterps,
            out IntPtrInterpreterDictionary interpreters
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                tclInterps = null;

                if (NativePackage.tclInterps != null)
                {
                    tclInterps = new IntPtrDictionary(
                        NativePackage.tclInterps);
                }

                interpreters = null;

                if (NativePackage.interpreters != null)
                {
                    interpreters = new IntPtrInterpreterDictionary(
                        NativePackage.interpreters);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Promote to RuntimeOps?
        //
        private static string GetCountString(
            ICollection collection,
            string prefix,
            string @default
            )
        {
            if (collection == null)
                return @default;

            if (String.IsNullOrEmpty(prefix))
                return collection.Count.ToString();

            return prefix + collection.Count.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Promote to RuntimeOps?
        //
        private static void DebugWriteOrTrace(
            Interpreter interpreter,
            string message,
            string category,
            TracePriority priority,
            bool write
            )
        {
            if (write)
                DebugOps.WriteTo(interpreter, message, true);
            else
                TraceOps.DebugTrace(message, category, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugTclInterpreters(
            Interpreter interpreter,
            string prefix,
            bool write
            )
        {
            //
            // NOTE: When tracing from the entry point methods, make sure
            //       the appropriate method name prefixes are added.  All
            //       the entry points pass non-null for the prefix string;
            //       therefore, use that as an indicator.
            //
            if (!String.IsNullOrEmpty(prefix))
                prefix = "DebugTclInterpreters: " + prefix + ", ";

            ///////////////////////////////////////////////////////////////////

            IntPtrDictionary tclInterps;
            IntPtrInterpreterDictionary interpreters;

            SnapshotAllInterpreters(out tclInterps, out interpreters);

            ///////////////////////////////////////////////////////////////////

            if ((tclInterps == null) || (tclInterps.Count == 0))
            {
                DebugWriteOrTrace(interpreter, String.Format(
                    "{0}{1} Tcl interpreters, {2} Eagle interpreters",
                    prefix, GetCountString(tclInterps, "have ", "no"),
                    GetCountString(interpreters, "have ", "no")),
                    typeof(NativePackage).Name, TracePriority.NativeDebug,
                    write);

                return;
            }

            if ((interpreters == null) || (interpreters.Count == 0))
            {
                DebugWriteOrTrace(interpreter, String.Format(
                    "{0}{1} Eagle interpreters, {2} Tcl interpreters",
                    prefix, GetCountString(interpreters, "have ", "no"),
                    GetCountString(tclInterps, "have ", "no")),
                    typeof(NativePackage).Name, TracePriority.NativeDebug,
                    write);

                return;
            }

            ///////////////////////////////////////////////////////////////////

            foreach (KeyValuePair<string, IntPtr> pair in
                    tclInterps) /* O(N) */
            {
                IntPtr interp = pair.Value;
                IntPtr parentInterp = IntPtr.Zero;
                Interpreter localInterpreter = null;

                if (GetInterpreter(
                        interpreters, interp, ref parentInterp,
                        ref localInterpreter) == ReturnCode.Ok)
                {
                    DebugWriteOrTrace(interpreter, String.Format(
                        "{0}Tcl interpreter {1} with name {2} held " +
                        "by Eagle interpreter {3} via parent Tcl " +
                        "interpreter {4}", prefix, interp,
                        FormatOps.WrapOrNull(pair.Key),
                        FormatOps.InterpreterNoThrow(localInterpreter),
                        parentInterp), typeof(NativePackage).Name,
                        TracePriority.NativeDebug, write);
                }
                else
                {
                    DebugWriteOrTrace(interpreter, String.Format(
                        "{0}Tcl interpreter {1} with name {2} is " +
                        "not held by any Eagle interpreter", prefix,
                        interp, FormatOps.WrapOrNull(pair.Key)),
                        typeof(NativePackage).Name,
                        TracePriority.NativeDebug, write);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Tcl Interpreter Management Methods
        //
        // NOTE: Used by the CheckInterp method of the TclApi class.
        //
        internal static bool FindTclInterpreterThreadId(
            IntPtr interp,
            ref long threadId
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if ((tclInterps != null) &&
                        tclInterps.ContainsValue(interp) /* O(N) */)
                    {
                        //
                        // NOTE: GetInterpreter() call OK, local value is
                        //       not used elsewhere.
                        //
                        // NOTE: GetPrimaryInterpreter() call OK, local
                        //       value is not used elsewhere.
                        //
                        Interpreter interpreter = GetInterpreter(interp);

                        if (interpreter == null)
                            interpreter = GetPrimaryInterpreter();

                        if (interpreter != null)
                        {
                            long localThreadId = interpreter.GetTclThreadId();

                            if (localThreadId != 0)
                            {
                                threadId = localThreadId;
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the DoOneEvent method of the TclWrapper class and by
        //       the DisposeTcl method of the Interpreter class.
        //
        internal static bool IsTclInterpreterActive()
        {
            //
            // BUGFIX: If there are any active public method calls into this
            //         class, some native Tcl interpreter must be active.
            //
            if (Interlocked.CompareExchange(ref activeCount, 0, 0) > 0)
                return true;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                return HasInterpreters(true) && HasTclInterpreters(true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used indirectly by the Execute method of the _Commands.Tcl
        //       class to help implement the [tcl primary] sub-command.
        //
        internal static ReturnCode GetParentTclInterpreter(
            Interpreter interpreter,
            ref string name,
            ref IntPtr interp,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HasTclInterpreters(false, ref error))
                {
                    //
                    // HACK: Use the convention that we always prefix the
                    //       parent Tcl interpreter with a specially formatted
                    //       name in order to find it (we cannot simply use the
                    //       Tcl interpreter at index zero because the Tcl
                    //       interpreters are maintained in a dictionary).
                    //
                    string pattern = FormatOps.Id(
                        tclNativeParentInterpPrefix, null,
                        Characters.Asterisk.ToString());

                    string safePattern = FormatOps.Id(
                        tclNativeSafeParentInterpPrefix, null,
                        Characters.Asterisk.ToString());

                    //
                    // NOTE: Search all the Tcl interpreters in this interpreter
                    //       for the parent Tcl interp stopping as soon as we
                    //       find a valid one (there should only be one valid
                    //       parent Tcl interpreter per interpreter at any given
                    //       time).
                    //
                    string key = null;
                    IntPtr value = IntPtr.Zero;

                    foreach (KeyValuePair<string, IntPtr> pair in
                            tclInterps) /* O(N) */
                    {
                        //
                        // NOTE: First, make sure the native Tcl interpreter
                        //       belongs to the specified Eagle interpreter,
                        //       if any.
                        //
                        // NOTE: GetInterpreter() call OK, local value is
                        //       not used elsewhere.
                        //
                        // NOTE: GetPrimaryInterpreter() call OK, local
                        //       value is not used elsewhere.
                        //
                        if ((interpreter != null) &&
                            !Object.ReferenceEquals(
                                GetInterpreter(pair.Value), interpreter) &&
                            !Object.ReferenceEquals(
                                GetPrimaryInterpreter(), interpreter))
                        {
                            continue;
                        }

                        //
                        // NOTE: Second, make sure it is a parent (or "safe"
                        //       parent) Tcl interpreter.  Hard-coded match
                        //       mode is OK here.
                        //
                        if ((StringOps.Match(
                                interpreter, MatchMode.Glob, pair.Key,
                                pattern, false) ||
                            StringOps.Match(
                                interpreter, MatchMode.Glob, pair.Key,
                                safePattern, false)) &&
                            (pair.Value != IntPtr.Zero))
                        {
                            key = pair.Key;
                            value = pair.Value;

                            break;
                        }
                    }

                    //
                    // NOTE: Did we find the handle for the parent Tcl
                    //       interpreter?
                    //
                    if (key != null)
                    {
                        name = key;
                        interp = value;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "Tcl parent interpreter is not available";
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the DeleteTclInterpreter and DisposeTcl methods of
        //       the Interpreter class.
        //
        internal static bool ShouldDeleteTclInterpreter(
            string interpName,
            IntPtr interp
            )
        {
            //
            // NOTE: Return true if we are not aware of the Tcl interpreter;
            //       otherwise, we must return false because it is not
            //       actually owned by Eagle and we should not delete it.
            //
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return (tclInterps == null) ||
                    !tclInterps.ContainsValue(interp); /* O(N) */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Eagle Interpreter Management Methods
        //
        // WARNING: Normally, this method should not be called directly.
        //          All exceptions to this rule should be marked in the
        //          neighboring source code comments.
        //
        private static Interpreter GetInterpreter(
            IntPtr interp
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Interpreter interpreter = null;

                if ((interpreters != null) &&
                    interpreters.TryGetValue(interp, out interpreter))
                {
                    return interpreter;
                }

                TraceOps.DebugTrace(String.Format(
                    "GetInterpreter: failed, interp = {0}, " +
                    "interpreter = {1}, {2}",
                    interp, FormatOps.InterpreterNoThrow(interpreter),
                    (interpreters != null) ? "not found" : "unavailable"),
                    typeof(NativePackage).Name,
                    TracePriority.NativeError);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by GetPrimaryOrIsolatedInterpreter() only.
        //          All exceptions to this rule *MUST* be marked in the
        //          neighboring source code comments.
        //
        private static Interpreter GetPrimaryInterpreter()
        {
            //
            // NOTE: GetInterpreter() call OK, the GetPrimaryInterpreter()
            //       method is trusted implicitly and should be used with
            //       great care.
            //
            return GetInterpreter(IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        private static Interpreter GetPrimaryOrIsolatedInterpreter(
            IntPtr interp,
            bool isolated
            )
        {
            //
            // NOTE: GetInterpreter() call OK, the purpose of this method
            //       is to return it, if necessary.
            //
            // NOTE: GetPrimaryInterpreter() call OK, the purpose of this
            //       method is to return it, if necessary.
            //
            return isolated ?
                GetInterpreter(interp) : GetPrimaryInterpreter();
        }

        ///////////////////////////////////////////////////////////////////////

        private static Interpreter CreateInterpreter(
            IEnumerable<string> args,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            string text,
            string libraryPath,
            ref Result result
            )
        {
            if (text == null)
            {
                text = GlobalConfiguration.GetValue(
                    EnvVars.NativePackagePreInitialize,
                    ConfigurationFlags.NativePackage);
            }

            return Interpreter.Create(
                args, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, text, libraryPath, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool AddInterpreter(
            IntPtr interp,
            Interpreter interpreter
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (interpreters == null)
                    interpreters = new IntPtrInterpreterDictionary();

                if (interpreters.ContainsKey(interp))
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddInterpreter: failed, interp = {0}, " +
                        "interpreter = {1}, already exists",
                        interp, FormatOps.InterpreterNoThrow(interpreter)),
                        typeof(NativePackage).Name,
                        TracePriority.NativeError);

                    return false;
                }

                interpreters.Add(interp, interpreter);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RemoveInterpreter(
            IntPtr interp,
            Interpreter interpreter
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (interpreters == null)
                    interpreters = new IntPtrInterpreterDictionary();

#if DEBUG || FORCE_TRACE
                Interpreter localInterpreter;

                if (!interpreters.TryGetValue(interp, out localInterpreter))
                {
                    TraceOps.DebugTrace(String.Format(
                        "RemoveInterpreter: failed, interp = {0}, " +
                        "interpreter = {1}, does not exist",
                        interp, FormatOps.InterpreterNoThrow(interpreter)),
                        typeof(NativePackage).Name,
                        TracePriority.NativeError);

                    return false;
                }

                if (!Object.ReferenceEquals(localInterpreter, interpreter))
                {
                    TraceOps.DebugTrace(String.Format(
                        "RemoveInterpreter: failed, interp = {0}, " +
                        "interpreter = {1}, localInterpreter = {2}, " +
                        "mismatched", interp, FormatOps.InterpreterNoThrow(
                        interpreter), FormatOps.InterpreterNoThrow(
                        localInterpreter)), typeof(NativePackage).Name,
                        TracePriority.NativeError);

                    return false;
                }
#endif

                return interpreters.Remove(interp);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool DisposeInterpreter(
            IntPtr interp,
            Interpreter interpreter
            )
        {
            bool result = false;

            TraceOps.DebugTrace(String.Format(
                "DisposeInterpreter: entered, interp = {0}, " +
                "interpreter = {1}", interp,
                FormatOps.InterpreterNoThrow(interpreter)),
                typeof(NativePackage).Name,
                TracePriority.NativeDebug);

            if (interpreter != null)
            {
                //
                // NOTE: Attempt to dispose of the Eagle interpreter
                //       now.  In theory, this may throw exceptions;
                //       however, since it should not attempt to
                //       unload Tcl, so that is unlikely.
                //
                interpreter.Dispose(); /* throw */

                //
                // NOTE: Next, make sure the interpreter is removed
                //       from the static dictionary for this class.
                //
                result = RemoveInterpreter(interp, interpreter);

                //
                // NOTE: Finally, null it out for good measure.
                //
                interpreter = null;
            }

            TraceOps.DebugTrace(String.Format(
                "DisposeInterpreter: exited, interp = {0}, " +
                "interpreter = {1}, result = {2}", interp,
                FormatOps.InterpreterNoThrow(interpreter), result),
                typeof(NativePackage).Name, TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int DisposeInterpreters(
            IntPtr interp
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                TraceOps.DebugTrace(String.Format(
                    "DisposeInterpreters: entered, interp = {0}",
                    interp), typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                int[] count = { 0, 0 };

                if (interpreters != null)
                {
                    IntPtrInterpreterDictionary localInterpreters =
                        interpreters.Clone() as IntPtrInterpreterDictionary;

                    if (localInterpreters != null)
                    {
                        count[1] = localInterpreters.Count;

                        foreach (KeyValuePair<IntPtr, Interpreter> pair
                                in localInterpreters) /* O(N) */
                        {
                            Interpreter interpreter = pair.Value;

                            try
                            {
                                if (DisposeInterpreter(pair.Key, interpreter))
                                    count[0]++;
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(NativePackage).Name,
                                    TracePriority.NativeError);
                            }
                        }

                        localInterpreters.Clear();
                        localInterpreters = null;
                    }
                }

                TraceOps.DebugTrace(String.Format(
                    "DisposeInterpreters: exited, interp = {0}, " +
                    "disposed = {1}/{2}", interp, count[0], count[1]),
                    typeof(NativePackage).Name, TracePriority.NativeDebug);

                return count[0];
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal State Introspection Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        internal static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty ||
                    ((tclInterps != null) && (tclInterps.Count > 0)))
                {
                    localList.Add("TclInterps", (tclInterps != null) ?
                        tclInterps.Count.ToString() : FormatOps.DisplayNull);
                }

                if (empty ||
                    ((interpreters != null) && (interpreters.Count > 0)))
                {
                    localList.Add("Interpreters", (interpreters != null) ?
                        interpreters.Count.ToString() : FormatOps.DisplayNull);
                }

                //
                // NOTE: GetPrimaryInterpreter() call OK, local value is not
                //       used elsewhere.
                //
                Interpreter interpreter = GetPrimaryInterpreter();

                HostOps.BuildInterpreterInfoList(
                    interpreter, "Primary Native Package Interpreter",
                    detailFlags, ref localList);

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Native Package");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ExitLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void Complain(
            IntPtr interp,
            bool isolated,
            ReturnCode code,
            Result result
            )
        {
            DebugOps.Complain(
                GetPrimaryOrIsolatedInterpreter(interp, isolated),
                code, result);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Argument Handling Methods
        private static ReturnCode CheckProtocolId(
            string protocolId,
            ref bool haveExtra,
            ref Result error
            )
        {
            if (SharedStringOps.SystemEquals(protocolId, ProtocolIdV1R0) ||
                SharedStringOps.SystemEquals(protocolId, ProtocolIdV1R1))
            {
                haveExtra = false;
                return ReturnCode.Ok;
            }

            if (SharedStringOps.SystemEquals(protocolId, ProtocolIdV1R2))
            {
                haveExtra = true;
                return ReturnCode.Ok;
            }

            error = String.Format(
                "protocol mismatch, have \"{0}\", need \"{1}\", \"{2}\", " +
                "or \"{3}\"", protocolId, ProtocolIdV1R0, ProtocolIdV1R1,
                ProtocolIdV1R2);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr StringToIntPtr(
            string text,
            bool nonZero,
            ref Result error
            )
        {
            long value = 0;

            if (Value.GetWideInteger2(
                    text, ValueFlags.AnyInteger, null, ref value,
                    ref error) == ReturnCode.Ok)
            {
                if (nonZero && (value == 0))
                {
                    error = String.Format(
                        "expected non-zero wide integer but got \"{0}\"",
                        text);

                    return IntPtr.Zero;
                }

                return new IntPtr(value);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ParseArgument(
            string arg,
            ref string protocolId,
            ref IntPtr module,
            ref IntPtr stubs,
            ref IntPtr interp,
            ref bool isolated,
            ref bool safe,
            ref StringList list,
            ref Result error
            )
        {
            StringList localList = null;
            Result localError = null;
            bool haveExtra = false;

            //
            // NOTE: Attempt to parse the argument as a Tcl/Eagle list.  If
            //       this fails, we cannot continue.
            //
            if (ParserOps<string>.SplitList(
                    null, arg, 0, Length.Invalid, false, ref localList,
                    ref localError) != ReturnCode.Ok)
            {
                goto done;
            }

            //
            // NOTE: Make sure the parsed list has at least the minimum number
            //       of arguments we need (i.e. the Tcl library module handle,
            //       the Tcl interpreter pointer, etc).
            //
            if (localList.Count < MinimumArgumentCountV1R1)
            {
                localError = String.Format(
                    "not enough arguments for \"{0}\", have {1}, need {2}",
                    ProtocolIdV1R1, localList.Count, MinimumArgumentCountV1R1);

                goto done;
            }

            //
            // NOTE: Initially, start at the first element of the parsed list,
            //       which is always the protocol identifier.
            //
            int index = 0;

            //
            // NOTE: The first argument must be the protocol identifier and it
            //       must represent a supported protocol version.  This check
            //       will also tell us if we should expect an additional
            //       argument containing the Tcl C API stubs structure pointer.
            //
            string localProtocolId = localList[index];

            if (CheckProtocolId(localProtocolId,
                    ref haveExtra, ref localError) != ReturnCode.Ok)
            {
                goto done;
            }

            //
            // NOTE: Make sure the parsed list has at least the minimum number
            //       of arguments we need, taking into account the Tcl C API
            //       stubs structure pointer argument, if applicable.
            //
            if (haveExtra && (localList.Count < MinimumArgumentCountV1R2))
            {
                localError = String.Format(
                    "not enough arguments for \"{0}\", have {1}, need {2}",
                    ProtocolIdV1R2, localList.Count, MinimumArgumentCountV1R2);

                goto done;
            }

            //
            // NOTE: Advance to the next element, which is always the Tcl
            //       library module handle.
            //
            index++;

            //
            // NOTE: Assume the second argument of the list parsed from the
            //       argument actually represents the Tcl library module
            //       handle.
            //
            IntPtr localModule = StringToIntPtr(localList[index], true,
                ref localError);

            if (localModule == IntPtr.Zero)
                goto done;

            //
            // NOTE: Advance to the next element, which is either the pointer to
            //       the Tcl C API stubs structure -OR- the pointer to the Tcl
            //       interpreter.
            //
            index++;

            IntPtr localStubs = IntPtr.Zero;

            if (haveExtra)
            {
                //
                // NOTE: Assume the third argument of the list parsed from the
                //       argument actually represents the pointer to the Tcl C
                //       API stubs structure.
                //
                localStubs = StringToIntPtr(localList[index], true,
                    ref localError);

                if (localStubs == IntPtr.Zero)
                    goto done;

                //
                // NOTE: Advance to the next element, which is the pointer to
                //       the Tcl interpreter.
                //
                index++;
            }

            //
            // NOTE: Assume the third (or fourth) argument of the list parsed
            //       from the argument actually represents the pointer to the
            //       Tcl interpreter.
            //
            IntPtr localInterp = StringToIntPtr(localList[index], true,
                ref localError);

            if (localInterp == IntPtr.Zero)
                goto done;

            //
            // NOTE: Advance to the next element, which is either the Tcl
            //       interpreter safety indicator -OR- the Eagle isolated
            //       interpreter indicator.
            //
            index++;

            bool localIsolated = false;

            if (haveExtra)
            {
                //
                // NOTE: The fourth (or fifth) argument is the Tcl interpreter
                //       safety indicator.  Non-zero means the Tcl interpreter
                //       is "safe" and the Eagle interpreter MUST be as well.
                //
                if (Value.GetBoolean2(
                        localList[index], ValueFlags.AnyBoolean, null,
                        ref localIsolated, ref localError) != ReturnCode.Ok)
                {
                    goto done;
                }

                //
                // NOTE: Advance to the next element, which is always the Tcl
                //       interpreter safety indicator.
                //
                index++;
            }

            //
            // NOTE: The fifth (or sixth) argument is the Tcl interpreter
            //       safety indicator.  Non-zero means the Tcl interpreter is
            //       "safe" and the Eagle interpreter MUST be as well.
            //
            bool localSafe = false;

            if (Value.GetBoolean2(localList[index], ValueFlags.AnyBoolean,
                    null, ref localSafe, ref localError) != ReturnCode.Ok)
            {
                goto done;
            }

            //
            // NOTE: Advance to the next element, which is always the start
            //       of the remaining (optional) arguments, if any.
            //
            index++;

            //
            // NOTE: Ok, everything was successful.  Commit changes to the
            //       parameters provided by the caller starting with the
            //       argument list just in case the called method throws an
            //       exception.
            //
            list = StringList.GetRange(localList, index, true);

            protocolId = localProtocolId;
            interp = localInterp;
            module = localModule;
            stubs = localStubs;
            isolated = localIsolated;
            safe = localSafe;

            return ReturnCode.Ok;

        done:

            error = haveExtra ?
                String.Format(
                    ParseArgumentErrorV1R2, ProtocolIdV1R2, localError) :
                String.Format(
                    ParseArgumentErrorV1R1, ProtocolIdV1R1, localError);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static PackageControlType? GetControlType(
            StringList list,
            ref Result error
            )
        {
            //
            // HACK: It is NOT an error if there are no arguments to the
            //       "control" sub-command.  In that case, just clear out
            //       the error and return null.  Our caller will interpret
            //       this result to mean "do nothing and return success".
            //
            if ((list == null) || (list.Count < 1))
            {
                error = null;
                return null;
            }

            //
            // NOTE: Otherwise, if there is at least one argument to the
            //       "control" sub-command, it must represent one of the
            //       valid package control types.
            //
            object enumValue = EnumOps.TryParse(
                typeof(PackageControlType), list[0],
                true, true, ref error);

            if (!(enumValue is PackageControlType))
                return null;

            return (PackageControlType)enumValue;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetControlArgs(
            StringList list,
            ref Interpreter interpreter,
            ref string packageName,
            ref Version version,
            ref Result error
            )
        {
            Interpreter localInterpreter = GetPrimaryInterpreter();

            if (localInterpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            CultureInfo cultureInfo = localInterpreter.InternalCultureInfo;

            if ((list == null) || (list.Count < 2))
            {
                error = "missing package name";
                return ReturnCode.Error;
            }

            string localPackageName = list[1];
            Version localVersion = null;

            if (list.Count >= 3)
            {
                if (Value.GetVersion(
                        list[2], cultureInfo, ref localVersion,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            interpreter = localInterpreter;
            packageName = localPackageName;
            version = localVersion;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public CLR Native API Integration Methods
        //
        // WARNING: This method is used to integrate with native code via the
        //          native CLR API.
        //
        public static int Startup(
            string argument /* This is the value of the "pwzArgument" argument
                             * as it was passed to native CLR API method
                             * ICLRRuntimeHost.ExecuteInDefaultAppDomain. */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Interlocked.Increment(ref activeCount);

            try
            {
                ReturnCode code;
                string protocolId = null;
                IntPtr module = IntPtr.Zero;
                IntPtr stubs = IntPtr.Zero;
                IntPtr interp = IntPtr.Zero;
                bool isolated = false;
                bool safe = false;
                StringList list = null;
                Result result = null;

                TraceOps.DebugTrace(String.Format(
                    "Startup: entered, argument = {0}",
                    FormatOps.WrapOrNull(true, true, argument)),
                    typeof(NativePackage).Name, TracePriority.NativeDebug);

                DebugTclInterpreters(null, "Startup entered", false);

                code = ParseArgument(
                    argument, ref protocolId, ref module, ref stubs,
                    ref interp, ref isolated, ref safe, ref list,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: These are boolean flags used for communication
                    //       between the secondary try and finally blocks in
                    //       this method (below).  If a flag is true, the
                    //       associated resource is [still] owned by this
                    //       method and must be cleaned up by the secondary
                    //       finally block; otherwise, it is no longer owned
                    //       by this method and should be left alone.  These
                    //       flags should only be consulted in the event of
                    //       a failure.
                    //
                    bool[] created = new bool[] {
                        false, /* NOTE: [0], Eagle interpreter owned? */
                        false, /* NOTE: [1], Tcl API object owned? */
                        false, /* NOTE: [2], TclBridge object owned? */
                        false  /* NOTE: [3], Tcl read-only flag owned? */
                    };

                    //
                    // NOTE: These local variables are used to track the Eagle
                    //       interpreter, Tcl API object, and TclBridge object
                    //       that we [may] create in this method.  In the event
                    //       of a failure, they should be cleaned up within the
                    //       secondary finally block.
                    //
                    Interpreter interpreter = null;
                    ITclApi tclApi = null;
                    TclBridge tclBridge = null;

                    try
                    {
                        //
                        // NOTE: Lock access to the static data contained in
                        //       this class (e.g. the dictionaries of native
                        //       Tcl and Eagle interpreters).
                        //
                        // BUGFIX: Lock Reform: Part #1, prevent deadlock by
                        //         simply removing the outer lock here.  This
                        //         should still be safe because the lock will
                        //         be held for access to our own local static
                        //         data (i.e. but not while accessing any of
                        //         the Interpreter methods).
                        //
                        // NOTE: We will need at least one Eagle interpreter
                        //       to host the necessary native Tcl integration
                        //       components; therefore, attempt to fetch or
                        //       create one now.  When operating in isolated
                        //       mode, each native Tcl interpreter will use a
                        //       different Eagle interpreter.  When operating
                        //       in non-isolated mode, all native Tcl
                        //       interpreters will share the "primary" Eagle
                        //       interpreter.  These two modes can coexist.
                        //       The Eagle interpreters MUST be kept around
                        //       for the entire lifetime of their associated
                        //       native Tcl interpreters unless they are
                        //       manually detached or shutdown.  Furthermore,
                        //       they MUST be visible to the CLR GC (i.e. to
                        //       prevent them from being garbage collected);
                        //       therefore, we store them in a static field
                        //       of this class.  If a native Tcl interpreter
                        //       is deleted, that will now trigger a call to
                        //       Detach(), which will then dispose of the
                        //       associated Eagle interpreter.
                        //
                        interpreter = GetPrimaryOrIsolatedInterpreter(
                            interp, isolated);

                        if (interpreter == null)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "Startup: appDomain = {0}",
                                FormatOps.DisplayAppDomain()),
                                typeof(NativePackage).Name,
                                TracePriority.NativeDebug);

                            //
                            // NOTE: Starting with the mandatory creation
                            //       flags then calculate out the exact
                            //       creation flags necessary to properly
                            //       create a suitable Eagle interpreter.
                            //       Typically, this will end up with a
                            //       value something like:
                            //
                            //       Verbose | Initialize | Debugger |
                            //       NoIsolated | NoTitle | NoIcon |
                            //       NoCancel | StrictAutoPath |
                            //       SetArguments | UseNamespaces |
                            //       NoMonitorPlugin
                            //
                            CreateFlags createFlags =
                                Interpreter.GetStartupCreateFlags(
                                    list, CreateFlags.NativeUse,
                                    OptionOriginFlags.NativePackage,
                                    true, true);

                            //
                            // NOTE: Starting with the mandatory host
                            //       creation flags then calculate out
                            //       the exact host creation flags
                            //       necessary to properly create a
                            //       suitable Eagle interpreter host.
                            //       Typically, this will end up with a
                            //       value something like:
                            //
                            //       NoTitle | NoIcon | NoCancel
                            //
                            HostCreateFlags hostCreateFlags =
                                Interpreter.GetStartupHostCreateFlags(
                                    list, HostCreateFlags.NativeUse,
                                    OptionOriginFlags.NativePackage,
                                    true, true);

                            //
                            // NOTE: Starting with the default initialize
                            //       flags then calculate the effective
                            //       initialize flags.  These semantics
                            //       may have to change at some point.
                            //
                            InitializeFlags initializeFlags =
                                Interpreter.GetStartupInitializeFlags(
                                    list, Defaults.InitializeFlags,
                                    OptionOriginFlags.NativePackage,
                                    true, true);

                            //
                            // NOTE: Starting with the default script
                            //       flags then calculate the effective
                            //       script flags.  These semantics may
                            //       have to change at some point.
                            //
                            ScriptFlags scriptFlags =
                                Interpreter.GetStartupScriptFlags(
                                    list, Defaults.ScriptFlags,
                                    OptionOriginFlags.NativePackage,
                                    true, true);

                            //
                            // HACK: Always forbid changes to the native
                            //       Tcl integration subsystem while it
                            //       is being actively modified by this
                            //       method (i.e. during any script
                            //       evaluation that may take place from
                            //       within the Interpreter.Create
                            //       method).
                            //
                            createFlags |= CreateFlags.TclReadOnly;

                            //
                            // NOTE: Create the Eagle interpreter as
                            //       "safe"?  Normally, this is only done
                            //       for "safe" Tcl interpreters; however,
                            //       it can also be specified manually.
                            //
                            if (safe)
                                createFlags |= CreateFlags.SafeAndHideUnsafe;

                            //
                            // NOTE: Fetch the pre-initialize script for
                            //       the Eagle interpreter to be created.
                            //       This will almost always be null (i.e.
                            //       none).
                            //
                            string text = null;

                            code = Interpreter.GetStartupPreInitializeText(list,
                                createFlags, OptionOriginFlags.NativePackage,
                                true, true, ref text, ref result);

                            string libraryPath = null;

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Fetch the script library path for the
                                //       Eagle interpreter to be created.  This
                                //       will almost always be null (i.e. use
                                //       the default).
                                //
                                code = Interpreter.GetStartupLibraryPath(list,
                                    createFlags, OptionOriginFlags.NativePackage,
                                    true, true, ref libraryPath, ref result);
                            }

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Attempt to create an Eagle interpreter
                                //       now.  This can fail for any number of
                                //       reasons (e.g. no script library found,
                                //       etc).
                                //
                                interpreter = CreateInterpreter(
                                    safe ? null : list, createFlags,
                                    hostCreateFlags, initializeFlags,
                                    scriptFlags, text, libraryPath,
                                    ref result);

                                created[0] = true; /* NOTE: Owned. */

                                if (interpreter != null)
                                {
                                    //
                                    // NOTE: Ok, the Eagle interpreter was
                                    //       created.  Process the "startup
                                    //       options" for it now (e.g. set
                                    //       flags, enable tracing, etc).
                                    //
                                    code = Interpreter.ProcessStartupOptions(
                                        interpreter, list, createFlags,
                                        OptionOriginFlags.NativePackage,
                                        true, true, ref result);
                                }
                            }

                            //
                            // NOTE: Show an interpreter was just created and
                            //       the information associated with it.
                            //
                            TraceOps.DebugTrace(String.Format(
                                "Startup: interpreter {0}, " +
                                "interpreter = {1}, args = {2}, " +
                                "createFlags = {3}, hostCreateFlags = {4}, " +
                                "libraryPath = {5}, code = {6}, result = {7}",
                                (interpreter != null) ?
                                    "created" : "not created",
                                FormatOps.InterpreterNoThrow(interpreter),
                                FormatOps.WrapOrNull(true, true, list),
                                FormatOps.WrapOrNull(createFlags),
                                FormatOps.WrapOrNull(hostCreateFlags),
                                FormatOps.WrapOrNull(libraryPath), code,
                                FormatOps.WrapOrNull(true, true, result)),
                                typeof(NativePackage).Name,
                                TracePriority.NativeDebug);
                        }

                        //
                        // NOTE: Do we have a valid Eagle interpreter context
                        //       (i.e. either pre-existing or just created)?
                        //
                        if ((code == ReturnCode.Ok) &&
                            (interpreter != null))
                        {
                            //
                            // NOTE: Verify that the Eagle interpreter is safe
                            //       if the Tcl interpreter is safe (i.e. just
                            //       in case the Eagle interpreter was created
                            //       previously with an unsafe Tcl interpreter).
                            //
                            if (!safe || interpreter.InternalIsSafe())
                            {
                                //
                                // NOTE: Lookup the Eagle [eval] command by
                                //       name and grab the IExecute object
                                //       for it.  This command will be the
                                //       destination for the [eagle] command
                                //       in Tcl added by this package.
                                //
                                IExecute execute = null;

                                code = interpreter.GetIExecuteViaResolvers(
                                    interpreter.GetResolveEngineFlagsNoLock(true),
                                    managedCommandName,
                                    new ArgumentList(managedCommandName),
                                    LookupFlags.Default, ref execute,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Check if the Eagle interpreter
                                    //       already existed.
                                    //
                                    if (!created[0])
                                    {
                                        //
                                        // HACK: Always forbid changes to the native
                                        //       Tcl integration subsystem while it is
                                        //       being actively modified by this method
                                        //       (i.e. during any script evaluation
                                        //       that may take place in this method).
                                        //
                                        interpreter.MakeTclReadOnly(true);

                                        //
                                        // HACK: Indicate to the finally block that we
                                        //       did indeed set the Tcl read-only flag.
                                        //
                                        created[3] = true; /* NOTE: Owned. */
                                    }

                                    //
                                    // NOTE: Create the TclApi object based on the
                                    //       provided Tcl library module handle.  If
                                    //       the supplied module handle is invaild in
                                    //       some way, this is where the failure will
                                    //       likely be [noticed first].
                                    //
                                    lock (interpreter.TclSyncRoot) /* TRANSACTIONAL */
                                    {
                                        tclApi = TclApi.GetTclApi(interpreter);

                                        if (tclApi == null)
                                        {
                                            tclApi = TclApi.Create(
                                                interpreter, null, null,
                                                module, stubs, LoadFlags.Default,
                                                ref result);

                                            created[1] = true; /* NOTE: Owned. */

                                            TraceOps.DebugTrace(String.Format(
                                                "Startup: tclApi {0}, " +
                                                "interpreter = {1}, " +
                                                "module = {2}, stubs = {3}, " +
                                                "code = {4}, result = {5}",
                                                (tclApi != null) ?
                                                    "created" : "not created",
                                                FormatOps.InterpreterNoThrow(
                                                    interpreter), module, stubs,
                                                code, FormatOps.WrapOrNull(
                                                    true, true, result)),
                                                typeof(NativePackage).Name,
                                                TracePriority.NativeDebug);

                                            TclApi.SetTclApi(interpreter, tclApi);

                                            created[1] = false; /* NOTE: Disowned. */
                                        }
                                    }

                                    if (tclApi != null)
                                    {
                                        //
                                        // NOTE: Make sure the list of Tcl interpreters
                                        //       tracked by this class, that should not
                                        //       be deleted, is initialized.
                                        //
                                        string interpName = null;

                                        //
                                        // BUGFIX: Lock Reform: Part #2, obtain and hold
                                        //         the lock while modifying our own static
                                        //         data (i.e. the dictionary of native Tcl
                                        //         interpreters).
                                        //
                                        lock (syncRoot) /* TRANSACTIONAL */
                                        {
                                            if (tclInterps == null)
                                                tclInterps = new IntPtrDictionary();

                                            //
                                            // HACK: Yes, this check is "useless" (i.e.
                                            //       always true) if the dictionary was
                                            //       just created (above).
                                            //
                                            if (!tclInterps.ContainsValue(
                                                    interp)) /* O(N) */
                                            {
                                                //
                                                // NOTE: All isolated Tcl interpreters
                                                //       are a "parent" Tcl interpreter.
                                                //       However, only the very first
                                                //       non-isolated one is.
                                                //
                                                interpName = GetTclInterpreterName(
                                                    isolated, safe);

                                                tclInterps.Add(interpName, interp);
                                            }
                                        }

                                        //
                                        // NOTE: Make sure this Tcl interpreter is not
                                        //       already attached.
                                        //
                                        if (interpName != null)
                                        {
                                            //
                                            // NOTE: Associate the Tcl interpreter
                                            //       with the name determined for it
                                            //       above.  There can be NO race
                                            //       condition here because the Tcl
                                            //       interpreter name is determined
                                            //       within the locked region above
                                            //       and this class is designed to
                                            //       permit the same Tcl interpreter
                                            //       to be added multiple times, as
                                            //       long as each one has a distinct
                                            //       name.
                                            //
                                            TclApi.AddInterp(
                                                interpreter, interpName, interp);

                                            //
                                            // NOTE: Create the TclBridge object to
                                            //       translate calls from the [eagle]
                                            //       Tcl command to the [eval] Eagle
                                            //       command.
                                            //
                                            tclBridge = TclBridge.Create(
                                                interpreter, execute, ClientData.Empty,
                                                interp, nativeCommandName, false, true,
                                                true, ref result);

                                            created[2] = true; /* NOTE: Owned. */

                                            TraceOps.DebugTrace(String.Format(
                                                "Startup: tclBridge {0}, " +
                                                "interpreter = {1}, execute = {2}, " +
                                                "interp = {3}, name = {4}, " +
                                                "code = {5}, result = {6}",
                                                (tclBridge != null) ?
                                                    "created" : "not created",
                                                FormatOps.InterpreterNoThrow(interpreter),
                                                FormatOps.WrapOrNull(execute), interp,
                                                FormatOps.WrapOrNull(nativeCommandName),
                                                code, FormatOps.WrapOrNull(true, true,
                                                result)), typeof(NativePackage).Name,
                                                TracePriority.NativeDebug);

                                            if (tclBridge != null)
                                            {
                                                string bridgeName = GetTclBridgeName(
                                                    interpName, nativeCommandName);

                                                interpreter.AddTclBridge(
                                                    bridgeName, tclBridge);

                                                created[2] = false; /* NOTE: Disowned. */

                                                if (AddInterpreter(
                                                        isolated ? interp : IntPtr.Zero,
                                                        interpreter))
                                                {
                                                    created[0] = false; /* NOTE: Disowned. */
                                                }

                                                //
                                                // HACK: Finally, permit changes to the
                                                //       native Tcl integration subsystem.
                                                //
                                                interpreter.MakeTclReadOnly(false);

                                                //
                                                // HACK: Indicate to the finally block
                                                //       that the Tcl read-only flag has
                                                //       now been unset.
                                                //
                                                created[3] = false;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                AlreadyAttachedError, interp,
                                                FormatOps.InterpreterNoThrow(
                                                interpreter));

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    SafeUnsafeError, interp,
                                    FormatOps.InterpreterNoThrow(
                                    interpreter));

                                code = ReturnCode.Error;
                            }
                        }
                        else if (code != ReturnCode.Error)
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    finally
                    {
                        //
                        // NOTE: If we fail for some reason, cleanup any
                        //       resources we allocated during this method
                        //       call.
                        //
                        if (code != ReturnCode.Ok)
                        {
                            if (created[2] && (tclBridge != null))
                            {
                                tclBridge.Dispose();
                                tclBridge = null;
                            }

                            //
                            // NOTE: Only dispose of the Tcl API object if we
                            //       created it during this method call.
                            //
                            if (created[1] && (tclApi != null))
                            {
                                IDisposable disposable = tclApi as IDisposable;

                                if (disposable != null)
                                {
                                    disposable.Dispose();
                                    disposable = null;
                                }

                                tclApi = null;
                            }

                            //
                            // NOTE: Only dispose of the Eagle interpreter
                            //       if we created it during this method call;
                            //       otherwise, it may already be in use.
                            //
                            if (created[0] && (interpreter != null))
                            {
                                interpreter.Dispose();
                                interpreter = null;
                            }
                            else if (created[3] && (interpreter != null))
                            {
                                //
                                // NOTE: If the Eagle interpreter was not
                                //       created by us, make sure the Tcl
                                //       read-only flag is unset now (as
                                //       we may have set it above).
                                //
                                interpreter.MakeTclReadOnly(false);
                            }
                        }
                    }
                }

                //
                // NOTE: We have no way of passing the result string back to
                //       native code; therefore, just "complain" about it
                //       (e.g. to the console).
                //
                if (code != ReturnCode.Ok)
                    Complain(interp, isolated, code, result);

                DebugTclInterpreters(null, "Startup exited", false);

                TraceOps.DebugTrace(String.Format(
                    "Startup: exited, protocolId = {0}, module = {1}, " +
                    "stubs = {2}, interp = {3}, isolated = {4}, safe = {5}, " +
                    "list = {6}, code = {7}, result = {8}",
                    FormatOps.WrapOrNull(true, true, protocolId), module,
                    stubs, interp, isolated, safe, FormatOps.WrapOrNull(
                    true, true, list), code, FormatOps.WrapOrNull(true,
                    true, result)), typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                return (int)code;
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used to integrate with native code via the
        //          native CLR API.
        //
        public static int Control(
            string argument /* This is the value of the "pwzArgument" argument
                             * as it was passed to native CLR API method
                             * ICLRRuntimeHost.ExecuteInDefaultAppDomain. */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Interlocked.Increment(ref activeCount);

            try
            {
                ReturnCode code;
                string protocolId = null;
                IntPtr module = IntPtr.Zero;
                IntPtr stubs = IntPtr.Zero;
                IntPtr interp = IntPtr.Zero;
                bool isolated = false;
                bool safe = false;
                StringList list = null;
                Result result = null;

                TraceOps.DebugTrace(String.Format(
                    "Control: entered, argument = {0}",
                    FormatOps.WrapOrNull(true, true, argument)),
                    typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                DebugTclInterpreters(null, "Control entered", false);

                code = ParseArgument(
                    argument, ref protocolId, ref module, ref stubs,
                    ref interp, ref isolated, ref safe, ref list,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    PackageControlType? controlType;
                    Result error = null;

                    controlType = GetControlType(list, ref error);

                    if (controlType != null)
                    {
                        switch ((PackageControlType)controlType)
                        {
                            case PackageControlType.Require:
                                {
                                    Interpreter interpreter = null;
                                    string packageName = null;
                                    Version version = null;

                                    code = GetControlArgs(
                                        list, ref interpreter,
                                        ref packageName, ref version,
                                        ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        code = interpreter.PkgRequire(
                                            packageName, version,
                                            ClientData.Empty,
                                            PackageFlags.None,
                                            false, ref result);
                                    }
                                    break;
                                }
                            default:
                                {
                                    result = String.Format(
                                        "unsupported control type: {0}",
                                        controlType);

                                    code = ReturnCode.Error;
                                    break;
                                }
                        }
                    }
                    else if (error != null)
                    {
                        result = error;
                        code = ReturnCode.Error;
                    }
                    else
                    {
                        // do nothing.
                    }
                }

                //
                // NOTE: We have no way of passing the result string back to
                //       native code; therefore, just "complain" about it
                //       (e.g. to the console).
                //
                if (code != ReturnCode.Ok)
                    Complain(interp, isolated, code, result);

                DebugTclInterpreters(null, "Control exited", false);

                TraceOps.DebugTrace(String.Format(
                    "Control: exited, protocolId = {0}, module = {1}, " +
                    "stubs = {2}, interp = {3}, isolated = {4}, safe = {5}, " +
                    "list = {6}, code = {7}, result = {8}",
                    FormatOps.WrapOrNull(true, true, protocolId), module,
                    stubs, interp, isolated, safe, FormatOps.WrapOrNull(
                    true, true, list), code, FormatOps.WrapOrNull(true,
                    true, result)), typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                return (int)code;
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used to integrate with native code via the
        //          native CLR API.
        //
        public static int Detach(
            string argument /* This is the value of the "pwzArgument" argument
                             * as it was passed to native CLR API method
                             * ICLRRuntimeHost.ExecuteInDefaultAppDomain. */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Interlocked.Increment(ref activeCount);

            try
            {
                ReturnCode code;
                string protocolId = null;
                IntPtr module = IntPtr.Zero;
                IntPtr stubs = IntPtr.Zero;
                IntPtr interp = IntPtr.Zero;
                bool isolated = false;
                bool safe = false;
                StringList list = null;
                Result result = null;

                TraceOps.DebugTrace(String.Format(
                    "Detach: entered, argument = {0}",
                    FormatOps.WrapOrNull(true, true, argument)),
                    typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                DebugTclInterpreters(null, "Detach entered", false);

                code = ParseArgument(
                    argument, ref protocolId, ref module, ref stubs,
                    ref interp, ref isolated, ref safe, ref list,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    //
                    // BUGFIX: Lock Reform: Part #1, prevent deadlock by simply
                    //         removing the outer lock here.  This should still
                    //         be safe because the lock will be held for access
                    //         to our own local static data (i.e. but not while
                    //         accessing any of the Interpreter methods).
                    //
                    try
                    {
                        Interpreter interpreter =
                            GetPrimaryOrIsolatedInterpreter(
                                interp, isolated);

                        //
                        // NOTE: For an isolated Eagle interpreter, it
                        //       should be completely disposed.  For a
                        //       shared Eagle interpreter, its bridged
                        //       Tcl commands should be disposed.
                        //
                        if (isolated)
                        {
                            //
                            // NOTE: The package is being unloaded from
                            //       this Tcl interpreter; therefore,
                            //       dispose of this isolated Eagle
                            //       interpreter now.
                            //
                            if (DisposeInterpreter(interp, interpreter))
                            {
                                code = ReturnCode.Ok;
                            }
                            else
                            {
                                result = String.Format(
                                    CouldNotDetachError, interp,
                                    FormatOps.InterpreterNoThrow(
                                    interpreter));

                                code = ReturnCode.Error;
                            }
                        }
                        else if (interpreter != null)
                        {
                            //
                            // NOTE: The package is being unloaded from
                            //       this Tcl interpreter.  Therefore,
                            //       remove any bridged Tcl commands from
                            //       this shared Eagle interpreter that
                            //       are associated with the target Tcl
                            //       interpreter.
                            //
                            code = interpreter.DisposeTclBridges(
                                interp, null, null, false, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                int count = TclApi.RemoveInterp(
                                    interpreter, interp);

                                if (count != 1)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "Detach: expected to remove 1 " +
                                        "Tcl interpreter from Eagle " +
                                        "interpreter {0} matching {1}, " +
                                        "actually removed {2}",
                                        FormatOps.InterpreterNoThrow(
                                        interpreter), interp, count),
                                        typeof(NativePackage).Name,
                                        TracePriority.NativeError);
                                }
                            }
                        }

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // BUGFIX: Lock Reform: Part #2, obtain and hold
                            //         the lock while modifying our own static
                            //         data (i.e. the dictionary of native Tcl
                            //         interpreters).
                            //
                            lock (syncRoot) /* TRANSACTIONAL */
                            {
                                //
                                // NOTE: Now that we are sure that either the
                                //       isolated Eagle interpreter has been
                                //       completely disposed -OR- the matching
                                //       bridged Tcl commands within the shared
                                //       Eagle interpreter have been disposed,
                                //       remove this Tcl interpreter from the
                                //       list [that we cannot delete] because
                                //       this is only an issue during disposal
                                //       of Eagle interpreters.
                                //
                                if (tclInterps != null)
                                {
                                    int count = tclInterps.RemoveAll(
                                        interp, 0); /* O(N) */

                                    if (count != 1)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "Detach: expected to remove 1 " +
                                            "Tcl interpreter matching {0}, " +
                                            "actually removed {1}",
                                            interp, count),
                                            typeof(NativePackage).Name,
                                            TracePriority.NativeError);
                                    }

                                    if (tclInterps.Count == 0)
                                        tclInterps = null;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        result = e;
                        code = ReturnCode.Error;
                    }
                }

                //
                // NOTE: We have no way of passing the result string back to
                //       native code; therefore, just "complain" about it
                //       (e.g. to the console).
                //
                if (code != ReturnCode.Ok)
                    Complain(interp, isolated, code, result);

                DebugTclInterpreters(null, "Detach exited", false);

                TraceOps.DebugTrace(String.Format(
                    "Detach: exited, protocolId = {0}, module = {1}, " +
                    "stubs = {2}, interp = {3}, isolated = {4}, safe = {5}, " +
                    "list = {6}, code = {7}, result = {8}",
                    FormatOps.WrapOrNull(true, true, protocolId), module,
                    stubs, interp, isolated, safe, FormatOps.WrapOrNull(
                    true, true, list), code, FormatOps.WrapOrNull(true,
                    true, result)), typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                return (int)code;
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used to integrate with native code via the
        //          native CLR API.
        //
        public static int Shutdown(
            string argument /* This is the value of the "pwzArgument" argument
                             * as it was passed to native CLR API method
                             * ICLRRuntimeHost.ExecuteInDefaultAppDomain. */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Interlocked.Increment(ref activeCount);

            try
            {
                ReturnCode code;
                string protocolId = null;
                IntPtr module = IntPtr.Zero;
                IntPtr stubs = IntPtr.Zero;
                IntPtr interp = IntPtr.Zero;
                bool isolated = false;
                bool safe = false;
                StringList list = null;
                Result result = null;

                TraceOps.DebugTrace(String.Format(
                    "Shutdown: entered, argument = {0}",
                    FormatOps.WrapOrNull(true, true, argument)),
                    typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                DebugTclInterpreters(null, "Shutdown entered", false);

                code = ParseArgument(
                    argument, ref protocolId, ref module, ref stubs,
                    ref interp, ref isolated, ref safe, ref list,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        try
                        {
                            //
                            // NOTE: The native package is being unloaded from
                            //       the entire process, dispose all the Eagle
                            //       interpreters now.
                            //
                            DisposeInterpreters(interp);

                            //
                            // NOTE: Now that we are sure the Eagle interpreters
                            //       have all been disposed, remove all the Tcl
                            //       interpreters from the list [that we should
                            //       not delete] because this is only an issue
                            //       during disposal of the Eagle interpreters.
                            //
                            if (tclInterps != null)
                            {
                                tclInterps.Clear();
                                tclInterps = null;
                            }
                        }
                        catch (Exception e)
                        {
                            result = e;
                            code = ReturnCode.Error;
                        }
                    }
                }

                //
                // NOTE: We have no way of passing the result string back to
                //       native code; therefore, just "complain" about it
                //       (e.g. to the console).
                //
                if (code != ReturnCode.Ok)
                    Complain(interp, isolated, code, result);

                DebugTclInterpreters(null, "Shutdown exited", false);

                TraceOps.DebugTrace(String.Format(
                    "Shutdown: exited, protocolId = {0}, module = {1}, " +
                    "stubs = {2}, interp = {3}, isolated = {4}, safe = {5}, " +
                    "list = {6}, code = {7}, result = {8}",
                    FormatOps.WrapOrNull(true, true, protocolId), module,
                    stubs, interp, isolated, safe, FormatOps.WrapOrNull(
                    true, true, list), code, FormatOps.WrapOrNull(true,
                    true, result)), typeof(NativePackage).Name,
                    TracePriority.NativeDebug);

                return (int)code;
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }
        #endregion
    }
}
