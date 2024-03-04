/*
 * CallFrameOps.cs --
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
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using VariablePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IVariable>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("72edb8df-b8c4-47ca-b8c9-6a0f463ee97a")]
    internal static class CallFrameOps
    {
        #region Private Constants
        internal static readonly string InfoLevelSubCommand = "level";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string ScopePrefix = "scope";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string ProcedureScope = "procedureScope";
        private static readonly string LambdaScope = "lambdaScope";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Checking Methods
        public static long GetLevel(
            ICallFrame frame
            )
        {
            return (frame != null) ? frame.Level : Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ICallFrame frame,
            CallFrameFlags flags,
            bool all
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags, flags, all) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static CallFrameFlags GetFlags(
            CallFrameFlags oldFlags,
            EngineMode engineMode,
            bool useNamespaces
            )
        {
            CallFrameFlags newFlags = oldFlags;

            switch (engineMode)
            {
                case EngineMode.EvaluateExpression:
                    newFlags |= CallFrameFlags.Expression;
                    goto case EngineMode.EvaluateScript; /* FALL-THROUGH */
                case EngineMode.EvaluateScript:
                case EngineMode.EvaluateFile:
                    newFlags |= CallFrameFlags.Evaluate;
                    break;
                case EngineMode.SubstituteString:
                case EngineMode.SubstituteFile:
                    newFlags |= CallFrameFlags.Substitute;
                    break;
            }

            if (useNamespaces)
                newFlags |= CallFrameFlags.UseNamespace;

            return newFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        public static VariableFlags GetNewVariableFlags(
            ICallFrame frame
            )
        {
            return (frame != null) &&
                FlagOps.HasFlags(frame.Flags, CallFrameFlags.Fast, true) ?
                VariableFlags.FastMask : VariableFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAlias(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Alias, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsGlobal(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Global, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsGlobalScope(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.GlobalScope, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsLambda(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Lambda, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsLocal(
            ICallFrame frame
            )
        {
            return IsProcedure(frame) || IsScope(frame);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsProcedure(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Procedure, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsScope(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Scope, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTracking(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Tracking, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsEngine(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Engine, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUseNamespace(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.UseNamespace, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisposedOrUndefined(
            ICallFrame frame
            )
        {
            if (frame == null)
                return false;

            if (frame.Disposed)
                return true;

            return FlagOps.HasFlags(
                frame.Flags, CallFrameFlags.Undefined, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDownlevel(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Downlevel, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUplevel(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Uplevel, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNamespace(
            ICallFrame frame
            )
        {
            return IsNamespace(frame, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsNamespace(
            ICallFrame frame,
            bool variables
            )
        {
            if (frame == null)
                return false;

            if (!HasFlags(frame, CallFrameFlags.Namespace, true))
                return false;

            if (variables && !IsVariable(frame))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsVariable(
            ICallFrame frame
            )
        {
            if (frame == null)
                return false;

            return frame.IsVariable;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNonGlobalVariable(
            ICallFrame frame
            )
        {
            if (frame == null)
                return false;

            if (!frame.IsVariable)
                return false;

            return !FlagOps.HasFlags(
                frame.Flags, CallFrameFlags.Global, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsCleanup(
            ICallFrame frame,
            bool variables
            )
        {
            if (variables && !IsVariable(frame))
                return false;

            if (IsGlobal(frame))
                return false;

            if (IsNamespace(frame))
                return false;

            if (IsScope(frame))
                return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Methods
        public static bool GetIExecuteName(
            ICallFrame frame, /* in */
            ref string name   /* out */
            )
        {
            if (frame == null)
                return false;

            IIdentifierName identifierName = frame.Execute as IIdentifierName;

            if (identifierName == null)
                return false;

            string localName = identifierName.Name;

            if (localName == null)
                return false;

            name = localName;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Mutator Methods
        public static void SetFast(
            ICallFrame frame,
            bool fast
            )
        {
            if (frame == null)
                return;

            if (fast)
                frame.Flags |= CallFrameFlags.Fast;
            else
                frame.Flags &= ~CallFrameFlags.Fast;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Equality Methods
        public static bool IsSame(
            ICallFrame frame1,
            ICallFrame frame2
            )
        {
            return Object.ReferenceEquals(frame1, frame2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSame(
            Interpreter interpreter,
            ICallFrame frame1,
            ICallFrame frame2,
            string name1,
            string name2
            )
        {
            if (!IsSame(frame1, frame2))
                return false;

            return SharedStringOps.SystemEquals(name1, name2);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Scope Support Methods
        private static byte[] GetHashValue(
            ICallFrame frame
            )
        {
            if (frame == null)
                return null;

            IClientData auxiliaryData = frame.AuxiliaryData;

            if (auxiliaryData == null)
                return null;

            return auxiliaryData.Data as byte[];
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetHashString(
            ICallFrame frame
            )
        {
            byte[] hashValue = GetHashValue(frame);

            if (hashValue == null)
                return null;

            return FormatOps.Hash(hashValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAutomaticScopeName(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return FormatOps.Id(ScopePrefix, null, interpreter.NextId());
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAutomaticScopeName(
            ICallFrame frame,
            bool shared
            )
        {
            //
            // NOTE: *WARNING* It is important that this type of
            //       scope always has the same name when the
            //       -shared option is enabled (i.e. it will be
            //       shared by all threads); therefore, no Id
            //       (zero) is used in that case.
            //
            // HACK: For lambdas, this is a very ugly hack that
            //       effectively limits the use of a "-procedure"
            //       scope to one-at-a-time per-interpreter (i.e.
            //       because lambdas are transient and have a
            //       unique Id embedded in their procedure names).
            //
            if (frame == null)
                return null;

            bool isLambda = FlagOps.HasFlags(
                frame.Flags, CallFrameFlags.Lambda, true);

            long contextId = shared ?
                0 : GlobalState.GetCurrentSystemThreadId();

            return FormatOps.Id(
                isLambda ? LambdaScope : ProcedureScope,
                isLambda ? GetHashString(frame) : frame.Name,
                contextId);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetNonGlobalScopeFlags(
            ICallFrame frame,
            ref CallFrameFlags newFlags,
            ref Result error
            )
        {
            if (frame == null)
            {
                error = "invalid call frame";
                return false;
            }

            CallFrameFlags oldFlags = frame.Flags;

            if (!FlagOps.HasFlags(oldFlags, CallFrameFlags.GlobalScope, true))
            {
                error = "call frame is not a global scope";
                return false;
            }

            newFlags = (oldFlags & ~CallFrameFlags.GlobalScopeMask) |
                CallFrameFlags.Scope;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MarkGlobalScope(
            ICallFrame frame,
            ref Result error
            )
        {
            if (frame == null)
            {
                error = "cannot mark as global scope: invalid call frame";
                return false;
            }

            CallFrameFlags flags = frame.Flags;

            if (!FlagOps.HasFlags(flags, CallFrameFlags.Scope, true))
            {
                error = "cannot mark as global scope: not a normal scope";
                return false;
            }

            flags &= ~CallFrameFlags.Scope;
            flags |= CallFrameFlags.GlobalScopeMask;

            frame.Flags = flags;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ICallFrame NewEngineScope(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.NewScopeCallFrame(GetAutomaticScopeName(
                interpreter), CallFrameFlags.Scope | CallFrameFlags.Engine,
                new VariableDictionary(), new ArgumentList());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeStoreInto(
            ICallFrame frame,    /* in: OPTIONAL */
            IValueData valueData /* in: OPTIONAL */
            )
        {
            if (valueData == null)
                return false;

            valueData.CallFrame = frame;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeStoreInto(
            ICallFrame frame, /* in: OPTIONAL */
            bool create,      /* in */
            ref Result result /* in, out: OPTIONAL */
            )
        {
            if (result == null)
            {
                if (!create)
                    return false;

                result = String.Empty;
                result.Value = null;
            }

            return MaybeStoreInto(frame, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CloneToNewScope(
            Interpreter interpreter,             /* in */
            string name,                         /* in */
            bool global,                         /* in */
            bool byRef,                          /* in */
            ref VariableDictionary newVariables, /* out */
            ref ICallFrame newFrame,             /* out */
            ref bool created,                    /* out */
            ref Result error                     /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // BUGFIX: Grab the actual current variable frame.
                //
                ICallFrame sourceFrame = global ?
                    interpreter.CurrentGlobalFrame :
                    interpreter.CurrentFrame;

                Result localResult = null;

                if (interpreter.GetVariableFrameViaResolvers(
                        LookupFlags.Default, ref sourceFrame,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }

                if (sourceFrame == null)
                {
                    error = "invalid source call frame";
                    return ReturnCode.Error;
                }

                VariableDictionary sourceVariables = sourceFrame.Variables;

                if (sourceVariables == null)
                {
                    error = "source call frame does not support variables";
                    return ReturnCode.Error;
                }

                //
                // BUGFIX: Since VariableDictionary creation can result
                //         in a call to the MaybeCopyFrom method, which
                //         can then fire variable traces (with arbitrary
                //         side-effects), a copy must be made here.
                //
                sourceVariables = new VariableDictionary(sourceVariables);

                //
                // NOTE: Create a new collection of variables from the
                //       current variable call frame.
                //
                // BUGFIX: When creating the new variable collection,
                //         make sure that we actually get new instances
                //         of the Variable class, while cloning all of
                //         its existing properties, except the locking
                //         status.
                //
                // BUGBUG: This may be wrong and may need to be broken
                //         for backwards compatibility in the future;
                //         however, scripts can use the -byref option
                //         to retain the old (and broken) behavior.
                //
                if (byRef)
                {
                    newVariables = new VariableDictionary(sourceVariables);
                }
                else
                {
                    newVariables = VariableDictionary.Create(
                        interpreter, sourceVariables, CloneFlags.ScopeMask,
                        ref error);

                    if (newVariables == null)
                        return ReturnCode.Error;
                }

                //
                // HACK: Also, skip doing this when the "-byref" option is
                //       used, because we never want to the change frames
                //       of existing variable instances that may happen to
                //       reside in a namespace, etc.
                //
                if (!byRef)
                {
                    //
                    // BUGFIX: *HACK* Re-parent all the variables to be in
                    //         the scope call frame.
                    //
                    newFrame = interpreter.NewScopeCallFrame(
                        name, CallFrameFlags.Scope, newVariables,
                        new ArgumentList());

                    created = true;

                    foreach (VariablePair pair in newVariables)
                    {
                        IVariable variable = pair.Value;

                        if (interpreter.IsSpecialVariable(variable))
                            continue;

                        EntityOps.ResetCallFrame(
                            interpreter, variable, newFrame);
                    }
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CloneToExistingScope(
            Interpreter interpreter, /* in */
            ICallFrame targetFrame,  /* in */
            bool global,             /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (targetFrame == null)
            {
                error = "invalid target call frame";
                return ReturnCode.Error;
            }

            if (!IsScope(targetFrame))
            {
                error = "target call frame must be scope";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                VariableDictionary targetVariables = targetFrame.Variables;

                if (targetVariables == null)
                {
                    error = "target call frame does not support variables";
                    return ReturnCode.Error;
                }

                ICallFrame sourceFrame;

                if (global)
                {
                    sourceFrame = interpreter.CurrentGlobalFrame;
                }
                else
                {
                    sourceFrame = interpreter.GetVariableCallFrame(
                        targetFrame);
                }

                if (sourceFrame == null)
                {
                    error = "invalid source call frame before resolve";
                    return ReturnCode.Error;
                }

                Result localResult = null;

                if (interpreter.GetVariableFrameViaResolvers(
                        LookupFlags.Default, ref sourceFrame,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }

                if (sourceFrame == null)
                {
                    error = "invalid source call frame after resolve";
                    return ReturnCode.Error;
                }

                if (IsSame(sourceFrame, targetFrame))
                {
                    error = "cannot clone to same call frame";
                    return ReturnCode.Error;
                }

                VariableDictionary sourceVariables = sourceFrame.Variables;

                if (sourceVariables == null)
                {
                    error = "source call frame does not support variables";
                    return ReturnCode.Error;
                }

                foreach (VariablePair pair in sourceVariables)
                {
                    IVariable variable = pair.Value;

                    if (interpreter.IsSpecialVariable(variable))
                        continue;

                    if (targetVariables.AddOrUpdate(
                            interpreter, pair.Key, variable,
                            targetFrame, CloneFlags.ScopeMask,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Traversal Methods
        public static ReturnCode Count(
            CallStack callStack,
            ICallFrame currentFrame,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref int count,
            ref Result error
            )
        {
            //
            // NOTE: If the current call frame is null then we are basically
            //       in the global scope; therefore, there would be zero call
            //       frames of any type between the current scope and down
            //       toward the bottom of the call stack.
            //
            if (currentFrame != null)
            {
                //
                // NOTE: Figure out where we should stop searching.
                //
                ICallFrame stopFrame = currentFrame;
                int frameCount = callStack.Count;

                for (int index = 0; index < frameCount; index++)
                {
                    ICallFrame thisFrame = callStack[index];

                    if (((hasFlags == CallFrameFlags.None) ||
                            HasFlags(thisFrame, hasFlags, hasAll)) &&
                        ((notHasFlags == CallFrameFlags.None) ||
                            !HasFlags(thisFrame, notHasFlags, notHasAll)))
                    {
                        count++;
                    }

                    //
                    // BUGFIX: If there is a next frame -AND- it is the same
                    //         as the stop frame, then keep going until that
                    //         is not the case.  This makes it possible for
                    //         [info level] to return an accurate result when
                    //         there are multiple instances of the same open
                    //         [scope] on the call stack.
                    //
                    if (!IsSame(thisFrame, stopFrame))
                        continue;

                    ICallFrame nextFrame = null;
                    int nextIndex = index + 1;

                    if (nextIndex < frameCount)
                        nextFrame = callStack[nextIndex];

                    if (!IsSame(nextFrame, stopFrame))
                        return ReturnCode.Ok;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Find(
            CallStack callStack,
            ICallFrame currentFrame,
            bool absolute,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll
            )
        {
            ICallFrame frame = null;

            return Find(callStack, currentFrame, absolute, level,
                hasFlags, notHasFlags, hasAll, notHasAll, ref frame);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Find(
            CallStack callStack,
            ICallFrame currentFrame,
            bool absolute,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref ICallFrame frame
            )
        {
            Result error = null;

            return Find(callStack, currentFrame, absolute, level, hasFlags,
                notHasFlags, hasAll, notHasAll, ref frame, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Find(
            CallStack callStack,
            ICallFrame currentFrame,
            bool absolute,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref ICallFrame frame,
            ref Result error
            )
        {
            //
            // NOTE: Call frame determination logic:
            //
            //       1. absolute level = 0: Always the outermost call frame that
            //                              match the specified type(s).
            //
            //       2. relative level = 0: Always the innermost call frame that
            //                              match the specified type(s).
            //
            //       3. absolute level > 0: Always the Nth call frame inward from
            //                              the global call frame that match the
            //                              specified type(s).
            //
            //       4. relative level > 0: Always the Nth call frame outward from
            //                              the current call frame that match the
            //                              specified type(s).
            //
            //       5. absolute level < 0: Always an error.
            //
            //       6. relative level < 0: Always an error.
            //
            // NOTE: There must be at least one call frame to continue.  Actually,
            //       there must be at least two for the code below to make sense;
            //       however, we do not enforce that rule here.
            //
            if (callStack != null)
            {
                int frameCount = callStack.Count;

                if (frameCount > 0)
                {
                    int startIndex = 0;

                    if (absolute || FindIndex(callStack, currentFrame, absolute, level,
                            ref startIndex, ref error) == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Starting at the previously determined current call
                        //       frame index, traverse through the call stack N times
                        //       (where only call frames that match against the
                        //       specified type(s) count against N).
                        //
                        int count = 0;

                        for (int index = startIndex;
                            CommonOps.ForCondition(absolute, index, 0, frameCount - 1);
                            CommonOps.ForLoop(absolute, ref index))
                        {
                            ICallFrame thisFrame = callStack[index];

                            if (((hasFlags == CallFrameFlags.None) ||
                                    HasFlags(thisFrame, hasFlags, hasAll)) &&
                                ((notHasFlags == CallFrameFlags.None) ||
                                    !HasFlags(thisFrame, notHasFlags, notHasAll)))
                            {
                                if (count++ == level)
                                {
                                    frame = thisFrame;
                                    return ReturnCode.Ok;
                                }
                            }
                        }

                        error = "call frame not found";
                    }
                }
                else
                {
                    error = "empty call stack";
                }
            }
            else
            {
                error = "invalid call stack";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static ICallFrame FollowPrevious(
            ICallFrame frame
            )
        {
            if (frame != null)
                while (frame.Previous != null)
                    frame = frame.Previous;

            return frame;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static ICallFrame FollowNext(
            ICallFrame frame
            )
        {
            if (frame != null)
                while (frame.Next != null)
                    frame = frame.Next;

            return frame;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FindIndex(
            CallStack callStack,
            bool absolute,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref int index,
            ref Result error
            )
        {
            if (callStack != null)
            {
                int frameCount = callStack.Count;

                if (frameCount > 0)
                {
                    //
                    // NOTE: If we are doing handling an absolute level, start at the
                    //       outermost call frame; otherwise, work outwards from the
                    //       current call frame until we find its index.
                    //
                    int startIndex;

                    if (absolute)
                        startIndex = 0;
                    else
                        startIndex = frameCount - 1;

                    //
                    // NOTE: Starting at the previously determined starting call frame
                    //       index, traverse through the call stack N times (where only
                    //       call frames that match against the specified type(s) count
                    //       against N).
                    //
                    int count = 0;

                    for (int thisIndex = startIndex;
                        CommonOps.ForCondition(absolute, thisIndex, 0, frameCount - 1);
                        CommonOps.ForLoop(absolute, ref thisIndex))
                    {
                        ICallFrame thisFrame = callStack[thisIndex];

                        if (((hasFlags == CallFrameFlags.None) ||
                                HasFlags(thisFrame, hasFlags, hasAll)) &&
                            ((notHasFlags == CallFrameFlags.None) ||
                                !HasFlags(thisFrame, notHasFlags, notHasAll)))
                        {
                            if (count++ == level)
                            {
                                index = thisIndex;
                                return ReturnCode.Ok;
                            }
                        }
                    }

                    error = "call frame not found";
                }
                else
                {
                    error = "empty call stack";
                }
            }
            else
            {
                error = "invalid call stack";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FindIndex(
            CallStack callStack,
            ICallFrame frame,
            bool absolute,
            int level,
            ref int index,
            ref Result error
            )
        {
            if (callStack != null)
            {
                int frameCount = callStack.Count;

                if (frameCount > 0)
                {
                    if (frame != null)
                    {
                        //
                        // NOTE: If we are doing handling an absolute level, start
                        //       at the outermost call frame; otherwise, work
                        //       outwards from the current call frame until we find
                        //       its index.
                        //
                        int startIndex;

                        if (absolute)
                            startIndex = 0;
                        else
                            startIndex = frameCount - 1;

                        //
                        // NOTE: Starting at the previously determined starting call
                        //       frame index, traverse through the call stack N times
                        //       (where only call frames that match against the
                        //       specified type(s) count against N).
                        //
                        for (int thisIndex = startIndex;
                            CommonOps.ForCondition(absolute, thisIndex, 0, frameCount - 1);
                            CommonOps.ForLoop(absolute, ref thisIndex))
                        {
                            ICallFrame thisFrame = callStack[thisIndex];

                            if (IsSame(thisFrame, frame))
                            {
                                index = thisIndex;
                                return ReturnCode.Ok;
                            }
                        }

                        error = "call frame not found";
                    }
                    else
                    {
                        error = "invalid call frame";
                    }
                }
                else
                {
                    error = "empty call stack";
                }
            }
            else
            {
                error = "invalid call stack";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetOrFind(
            CallStack callStack,
            ICallFrame globalFrame,
            ICallFrame currentGlobalFrame,
            ICallFrame currentFrame,
            bool absolute,
            bool super,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref ICallFrame frame
            )
        {
            Result error = null;

            return GetOrFind(
                callStack, globalFrame, currentGlobalFrame, currentFrame,
                absolute, super, level, hasFlags, notHasFlags, hasAll,
                notHasAll, ref frame, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetOrFind(
            CallStack callStack,
            ICallFrame globalFrame,
            ICallFrame currentGlobalFrame,
            ICallFrame currentFrame,
            bool absolute,
            bool super,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref ICallFrame frame,
            ref Result error
            )
        {
            //
            // NOTE: Are they asking for "Absolute Zero"?
            //
            if (absolute && (level == 0))
            {
                //
                // NOTE: Absolute level 0 is the global call frame.
                //
                if (super)
                {
                    //
                    // NOTE: In this case, the caller is requesting
                    //       the actual, outermost, non-scope global
                    //       frame (i.e. not just the current global
                    //       frame).
                    //
                    frame = globalFrame;
                    return ReturnCode.Ok;
                }
                else
                {
                    //
                    // NOTE: In this case, using the current global
                    //       frame will be fine.  This may not be
                    //       the actual global frame.  It may be a
                    //       scope frame and that is OK.
                    //
                    frame = currentGlobalFrame;
                    return ReturnCode.Ok;
                }
            }
            else
            {
                //
                // NOTE: Relative level 0 is the current procedure call
                //       frame.  If there is no current procedure call,
                //       this is an error.
                //
                return Find(
                    callStack, currentFrame, absolute, level, hasFlags,
                    notHasFlags, hasAll, notHasAll, ref frame, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode MarkMatching(
            CallStack callStack,
            ICallFrame currentFrame,
            bool absolute,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            CallFrameFlags markFlags,
            bool hasAll,
            bool notHasAll,
            bool mark,
            ref Result error
            )
        {
            if (callStack != null)
            {
                int frameCount = callStack.Count;

                if (frameCount > 0)
                {
                    if (currentFrame != null)
                    {
                        //
                        // NOTE: If we are doing handling an absolute level, start
                        //       at the outermost call frame; otherwise, work
                        //       outwards from the current call frame until we find
                        //       its index.
                        //
                        int startIndex = 0;

                        if ((absolute && FindIndex(callStack, absolute, level,
                                hasFlags, notHasFlags, hasAll, notHasAll,
                                ref startIndex, ref error) == ReturnCode.Ok) ||
                            (!absolute && FindIndex(callStack, currentFrame, absolute,
                                level, ref startIndex, ref error) == ReturnCode.Ok))
                        {
                            //
                            // NOTE: If we are in absolute mode, we need to start
                            //       just after where the current frame index is.
                            //
                            if (absolute)
                                startIndex++;

                            //
                            // NOTE: Starting at the previously determined current
                            //       call frame index, traverse through the call
                            //       stack N times (where only call frames that
                            //       match against the specified type(s) count
                            //       against N).
                            //
                            int count = 0;

                            //
                            // NOTE: Use the unique Id of the starting call frame
                            //       to keep track of which frames were marked in
                            //       this "round".
                            //
                            string markName = StringList.MakeList(
                                currentFrame.FrameId, currentFrame.Name);

                            for (int index = startIndex;
                                CommonOps.ForCondition(absolute, index, 0, frameCount - 1);
                                CommonOps.ForLoop(absolute, ref index))
                            {
                                ICallFrame thisFrame = callStack[index];

                                if (((hasFlags == CallFrameFlags.None) ||
                                        HasFlags(thisFrame, hasFlags, hasAll)) &&
                                    ((notHasFlags == CallFrameFlags.None) ||
                                        !HasFlags(thisFrame, notHasFlags, notHasAll)))
                                {
                                    ICallFrame markFrame = null;

                                    if (mark || (thisFrame.HasMark(markName, ref markFrame) &&
                                            IsSame(markFrame, currentFrame)))
                                    {
                                        if (!absolute && (count++ == level))
                                            return ReturnCode.Ok;

                                        thisFrame.InitializeMarks();

                                        thisFrame.SetMark(mark, markFlags, markName,
                                            currentFrame);

                                        if (absolute &&
                                            IsSame(thisFrame, currentFrame))
                                        {
                                            return ReturnCode.Ok;
                                        }
                                    }
                                }
                            }

                            return ReturnCode.Ok;
                        }
                    }
                }
                else
                {
                    error = "empty call stack";
                }
            }
            else
            {
                error = "invalid call stack";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Traverse(
            Interpreter interpreter,
            CallStack callStack,
            ICallFrame skipFrame,
            int limit,
            bool all,
            ref CallStack newCallStack,
            ref Result error
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (Interpreter.IsDeletedOrDisposed(interpreter, false, ref error))
                    {
                        code = ReturnCode.Error;
                    }
                    else
                    {
                        if (callStack != null)
                        {
                            if (all)
                            {
                                //
                                // NOTE: Create the new call stack for the caller now
                                //       because this method guarantees it will be valid
                                //       if the method itself returns without error and
                                //       no errors can be generated beyond this point.
                                //
                                if (newCallStack == null)
                                    newCallStack = new CallStack(false);

                                int frameCount = callStack.Count;

                                for (int index = 0; index < frameCount; index++)
                                {
                                    //
                                    // NOTE: Index the call frames backwards.
                                    //
                                    ICallFrame frame = callStack[((frameCount - 1) - index)];

                                    //
                                    // HACK: Preserve this wacky logic from the default host.
                                    //       Skip over the specified call frame unless we
                                    //       have already processed the first call frame.
                                    //
                                    if ((skipFrame == null) ||
                                        (index > 0) ||
                                        !IsSame(frame, skipFrame))
                                    {
                                        if ((limit == Limits.Unlimited) ||
                                            (newCallStack.Count < limit))
                                        {
                                            newCallStack.Add(frame);
                                        }
                                    }
                                }

                                code = ReturnCode.Ok;
                            }
                            else
                            {
                                CallFrameFlags notHasFlags = CallFrameFlags.None;
                                ICallFrame currentFrame = null;

                                interpreter.GetInfoLevelFlagsAndFrame(
                                    null, ref notHasFlags, ref currentFrame);

                                int count = 0;

                                code = Count(callStack, currentFrame,
                                    interpreter.GetInfoLevelCallFrameFlags(),
                                    notHasFlags, false, false, ref count, ref error);

                                if (code == ReturnCode.Ok)
                                {
                                    ICallFrame globalFrame = interpreter.GlobalFrame; /* EXEMPT */
                                    ICallFrame currentGlobalFrame = interpreter.CurrentGlobalFrame;

                                    //
                                    // NOTE: Increase the calculated count because it
                                    //       does not include the global call frame.
                                    //
                                    count++;

                                    for (int index = 0; index < count; index++)
                                    {
                                        ICallFrame frame = null;

                                        code = GetOrFind(
                                            callStack, globalFrame, currentGlobalFrame,
                                            currentFrame, false, false, index,
                                            CallFrameFlags.Variables, notHasFlags,
                                            false, false, ref frame, ref error);

                                        if (code != ReturnCode.Ok)
                                            break;

                                        if (newCallStack == null)
                                            newCallStack = new CallStack(false);

                                        if ((limit == Limits.Unlimited) ||
                                            (newCallStack.Count < limit))
                                        {
                                            newCallStack.Add(frame);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            error = "invalid call stack";
                            code = ReturnCode.Error;
                        }
                    }
                }
            }
            else
            {
                error = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Push / Peek / Pop Methods
        public static CallFrameFlags? GetFlagsNoThrow(
            ICallFrame frame
            )
        {
            if (frame != null)
            {
                try
                {
                    return frame.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MatchFlags(
            CallFrameFlags flags,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll
            )
        {
            if (((hasFlags == CallFrameFlags.None) ||
                    FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                ((notHasFlags == CallFrameFlags.None) ||
                    FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsEmpty(
            CallStack callStack,
            bool @default
            )
        {
            if (callStack == null)
                return @default;

            return (callStack.Count == 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanPush(
            CallStack callStack
            )
        {
            return (callStack != null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanPeekOrPop(
            CallStack callStack
            )
        {
            return ((callStack != null) && (callStack.Count > 0));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanPeekOrPop(
            CallStack callStack,
            ref ICallFrame frame
            )
        {
            if (callStack == null)
                return false;

            if (callStack.Count == 0)
                return false;

            frame = callStack.Peek();
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanPeekOrPop(
            CallStack callStack,
            int index,
            ref ICallFrame frame
            )
        {
            if (callStack == null)
                return false;

            if (callStack.Count <= index)
                return false;

            frame = callStack.Peek(index);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This assumes that the call stack is not null.
        //
        public static ICallFrame Peek(
            CallStack callStack
            )
        {
            return callStack.Peek();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This assumes that the call stack is not null.
        //
        public static ICallFrame Pop(
            CallStack callStack,
            ref ICallFrame currentFrame
            )
        {
            ICallFrame newFrame = callStack.Pop(); // pop current call frame.

            //
            // NOTE: Did we pop the last call frame?  Normally, that should
            //       be impossible because the global call frame should not
            //       be popped.  That being said, this method is not allowed
            //       to throw an exception if the last call frame is popped.
            //
            currentFrame = (callStack.Count > 0) ?
                callStack.Peek() : null; // current frame is now stack top.

            return newFrame; // return popped call frame.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This assumes that the call stack is not null.
        //
        public static void Push(
            CallStack callStack,
            ICallFrame newFrame,
            ref ICallFrame currentFrame
            )
        {
            callStack.Push(newFrame); // push new call frame.
            currentFrame = callStack.Peek(); // current frame is now stack top.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Creation / Cleanup Methods
        public static int Cleanup(
            ICallFrame currentFrame,
            ICallFrame frame,
            bool undefined
            )
        {
            int count = _Constants.Count.Invalid;

            if (frame != null)
            {
                VariableDictionary variables = frame.Variables;

                if (variables != null)
                {
                    StringList list = new StringList(variables.Keys);
                    long currentLevel = GetLevel(currentFrame);

                    //
                    // NOTE: Ok, we got this far, reset the count of how many
                    //       variables have been removed.  Caller should check
                    //       if the result is greater than or equal to zero for
                    //       "success".
                    //
                    count = 0;

                    foreach (string name in list)
                    {
                        //
                        // NOTE: Grab the variable, by name, from the call frame
                        //       being "cleaned up".
                        //
                        IVariable variable = variables[name];

                        //
                        // NOTE: We only want to remove variables that match
                        //       certain criteria (below).
                        //
                        bool remove = false;

                        //
                        // NOTE: Is the variable actually valid (i.e. not null)?
                        //
                        if (variable != null)
                        {
                            //
                            // NOTE: Is the variable or link flagged as undefined?
                            //
                            if (EntityOps.IsUndefined(variable))
                            {
                                //
                                // NOTE: Always remove undefined variables for the
                                //       call frame we are "cleaning up".
                                //
                                remove = true;
                            }
                            else if (GetLevel(variable.Frame) > currentLevel)
                            {
                                //
                                // NOTE: Remove variables for the call frame we are
                                //       "cleaning up" only if they are going out of
                                //       scope.  If these frames are equal, we are
                                //       simply cleaning up undefined variables in
                                //       the current call frame without touching any
                                //       of the other variables.
                                //
                                remove = true;
                            }

                            //
                            // BUGFIX: We need to remove the targets of any links that
                            //         are now undefined due to unset or something
                            //         similar.  The link itself will also be removed
                            //         (however, not by this block of code) if it also
                            //         happens to be undefined or going out of scope.
                            //
                            if (EntityOps.IsLink(variable))
                            {
                                //
                                // NOTE: Follow the linked variable(s).
                                //
                                while (variable.Link != null)
                                {
                                    //
                                    // NOTE: Save the link itself because we may need
                                    //       to mark it as undefined (below).
                                    //
                                    IVariable savedVariable = variable; // TEST: Test.

                                    //
                                    // NOTE: Follow the linked variable.
                                    //
                                    variable = variable.Link;

                                    //
                                    // NOTE: Remove undefined variable in the other call
                                    //       frame via the local linked variable we may
                                    //       also be removing (below).
                                    //
                                    if (EntityOps.IsUndefined(variable))
                                    {
                                        //
                                        // NOTE: Unlink the local link variable and the
                                        //       variable in the other call frame
                                        //       (prevents having dangling references
                                        //       to "deleted" variables).
                                        //
                                        savedVariable.Link = null;
                                        savedVariable.LinkIndex = null;

                                        //
                                        // NOTE: Grab the name of the linked variable.
                                        //       If it is null, skip removing this
                                        //       variable (i.e. the variables dictionary
                                        //       for this frame cannot have a null key).
                                        //
                                        string linkName = variable.Name;

                                        if (linkName == null)
                                            continue;

                                        //
                                        // BUGFIX: We cannot remove the linked variable
                                        //         from the linked call frame if it is
                                        //         null or the same as the call frame
                                        //         being cleaned up; otherwise, an
                                        //         exception may be thrown at the top
                                        //         of the loop when fetching variables
                                        //         from the frame being cleaned up if
                                        //         the name of the linked variable
                                        //         occurs later in the list than the
                                        //         name of the link to it.
                                        //
                                        ICallFrame linkFrame = variable.Frame;

                                        if ((linkFrame == null) ||
                                            IsSame(linkFrame, frame))
                                        {
                                            continue;
                                        }

                                        //
                                        // BUGFIX: If the other end of the link is the
                                        //         global call frame or a namespace call
                                        //         frame, we cannot remove the variable
                                        //         because it may have been declared via
                                        //         [variable] and just not set yet (e.g.
                                        //         some script procedure declares the
                                        //         variable and then returns non-zero if
                                        //         it exists).
                                        //
                                        if (!IsCleanup(linkFrame, false))
                                            continue;

                                        //
                                        // NOTE: Grab the variables dictionary from the
                                        //       linked call frame.  If it is null we
                                        //       cannot do anything.  Otherwise, since
                                        //       we are traversing, use the frame
                                        //       reference stored within the variable
                                        //       itself to remove the variable from the
                                        //       other call frame.
                                        //
                                        VariableDictionary linkVariables =
                                            linkFrame.Variables;

                                        if (linkVariables == null)
                                            continue;

                                        linkVariables.Remove(linkName);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Always remove null variables for the call frame
                            //       we are "cleaning up".
                            //
                            remove = true;
                        }

                        if (remove && variables.Remove(name))
                            count++;
                    }
                }

                if (undefined)
                    frame.Flags |= CallFrameFlags.Undefined;
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Purge(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (Interpreter.IsDeletedOrDisposed(
                        interpreter, false, ref result))
                {
                    return ReturnCode.Error;
                }

                ICallFrame variableFrame = interpreter.CurrentFrame;

                if (interpreter.GetVariableFrameViaResolvers(
                        LookupFlags.Default, ref variableFrame,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (variableFrame != null)
                {
                    int purged = Cleanup(
                        interpreter.CurrentFrame, variableFrame, false);

                    if (purged >= 0)
                    {
                        result = String.Format(
                            "purged {0} undefined variables for call frame \"{1}\"",
                            purged, variableFrame);

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        result = "failed to cleanup call frame";
                    }
                }
                else
                {
                    result = "invalid variable call frame";
                }
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
