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
using _Count = Eagle._Constants.Count;

using ArgumentPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IAnyPair<
        int, Eagle._Components.Public.Argument>>;

using VariablePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IVariable>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private helper methods used to query, mutate,
    /// traverse, and manage the call frames that make up an Eagle interpreter's
    /// call stack.  It centralizes the logic for testing call frame flags,
    /// creating and cloning scope call frames, locating call frames by level,
    /// pushing and popping the call stack, and cleaning up the variables owned
    /// by a call frame.  All members are static; the class is never
    /// instantiated.
    /// </summary>
    [ObjectId("72edb8df-b8c4-47ca-b8c9-6a0f463ee97a")]
    internal static class CallFrameOps
    {
        #region Private Constants
        /// <summary>
        /// The name of the <c>[info level]</c> sub-command, used when
        /// formatting and matching call frame level information.
        /// </summary>
        internal static readonly string InfoLevelSubCommand = "level";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The prefix used when generating an automatic name for a scope call
        /// frame.
        /// </summary>
        private static readonly string ScopePrefix = "scope";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The prefix used when generating an automatic name for a procedure
        /// scope call frame.
        /// </summary>
        private static readonly string ProcedureScope = "procedureScope";

        /// <summary>
        /// The prefix used when generating an automatic name for a lambda scope
        /// call frame.
        /// </summary>
        private static readonly string LambdaScope = "lambdaScope";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Checking Methods
        /// <summary>
        /// This method gets the absolute level of the specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The level of the call frame, or <see cref="Index.Invalid" /> if the
        /// call frame is null.
        /// </returns>
        public static long GetLevel(
            ICallFrame frame
            )
        {
            return (frame != null) ? frame.Level : Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame has the
        /// specified flags set.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be set; zero to
        /// require only any of them.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and has the specified flags;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method computes the call frame flags appropriate for the
        /// specified engine mode, starting from an existing set of flags.
        /// </summary>
        /// <param name="oldFlags">
        /// The existing call frame flags to start from.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that determines which evaluation or substitution
        /// flags to add.
        /// </param>
        /// <param name="useNamespaces">
        /// Non-zero to also add the namespace usage flag.
        /// </param>
        /// <returns>
        /// The computed call frame flags.
        /// </returns>
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

        /// <summary>
        /// This method computes the variable flags that should be used when
        /// creating new variables within the specified call frame, based on the
        /// call frame's flags.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The variable flags to use for new variables, or
        /// <see cref="VariableFlags.None" /> if the call frame is null.
        /// </returns>
        public static VariableFlags GetNewVariableFlags(
            ICallFrame frame
            )
        {
            VariableFlags variableFlags = VariableFlags.None;

            if (frame == null)
                return variableFlags;

            CallFrameFlags callFrameFlags = frame.Flags;

            if (FlagOps.HasFlags(
                    callFrameFlags, CallFrameFlags.Library, true))
            {
                variableFlags |= VariableFlags.FastTraceMask;
            }

            if (FlagOps.HasFlags(
                    callFrameFlags, CallFrameFlags.Fast, true))
            {
                variableFlags |= VariableFlags.FastMask;
            }

            return variableFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is an alias
        /// call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is an alias call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsAlias(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Alias, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is the
        /// global call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is the global call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsGlobal(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Global, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a global
        /// scope call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is a global scope call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsGlobalScope(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.GlobalScope, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a lambda
        /// call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is a lambda call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsLambda(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Lambda, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a local
        /// call frame (that is, a procedure or scope call frame).
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is a procedure or scope call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsLocal(
            ICallFrame frame
            )
        {
            return IsProcedure(frame) || IsScope(frame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a
        /// procedure call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is a procedure call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsProcedure(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Procedure, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a scope
        /// call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is a scope call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsScope(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Scope, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a
        /// tracking call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is a tracking call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsTracking(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Tracking, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is an engine
        /// call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is an engine call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsEngine(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Engine, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame has
        /// namespace usage enabled.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and has namespace usage enabled;
        /// otherwise, false.
        /// </returns>
        public static bool IsUseNamespace(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.UseNamespace, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame has been
        /// disposed or is flagged as undefined.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and has been disposed or is
        /// undefined; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified call frame is a
        /// downlevel call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is a downlevel call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsDownlevel(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Downlevel, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is an
        /// uplevel call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and is an uplevel call frame;
        /// otherwise, false.
        /// </returns>
        public static bool IsUplevel(
            ICallFrame frame
            )
        {
            return (frame != null) ?
                FlagOps.HasFlags(frame.Flags,
                    CallFrameFlags.Uplevel, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a
        /// namespace call frame that also supports variables.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is a namespace call frame supporting
        /// variables; otherwise, false.
        /// </returns>
        public static bool IsNamespace(
            ICallFrame frame
            )
        {
            return IsNamespace(frame, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame is a
        /// namespace call frame, optionally also requiring that it support
        /// variables.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <param name="variables">
        /// Non-zero to also require that the call frame support variables.
        /// </param>
        /// <returns>
        /// True if the call frame is a namespace call frame (and, when
        /// requested, supports variables); otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified call frame supports
        /// variables.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null and supports variables;
        /// otherwise, false.
        /// </returns>
        public static bool IsVariable(
            ICallFrame frame
            )
        {
            if (frame == null)
                return false;

            return frame.IsVariable;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified call frame supports
        /// variables and is not the global call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame is non-null, supports variables, and is not
        /// the global call frame; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified call frame is eligible
        /// for variable cleanup.  A call frame is eligible unless it is the
        /// global call frame, a namespace call frame, or a scope call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <param name="variables">
        /// Non-zero to require that the call frame support variables in order to
        /// be eligible.
        /// </param>
        /// <returns>
        /// True if the call frame is eligible for cleanup; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method gets the name of the executable entity associated with
        /// the specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// Upon success, this contains the name of the executable entity
        /// associated with the call frame.
        /// </param>
        /// <returns>
        /// True if the name was successfully obtained; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method sets or clears the fast flag on the specified call
        /// frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to modify.  This parameter may be null, in which case
        /// nothing is done.
        /// </param>
        /// <param name="fast">
        /// Non-zero to set the fast flag; zero to clear it.
        /// </param>
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
        /// <summary>
        /// This method determines whether two call frame references refer to the
        /// same call frame instance.
        /// </summary>
        /// <param name="frame1">
        /// The first call frame to compare.  This parameter may be null.
        /// </param>
        /// <param name="frame2">
        /// The second call frame to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if both references refer to the same instance; otherwise,
        /// false.
        /// </returns>
        public static bool IsSame(
            ICallFrame frame1,
            ICallFrame frame2
            )
        {
            return Object.ReferenceEquals(frame1, frame2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two call frames are the same instance
        /// and have matching names.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.  This parameter may be null.
        /// </param>
        /// <param name="frame1">
        /// The first call frame to compare.  This parameter may be null.
        /// </param>
        /// <param name="frame2">
        /// The second call frame to compare.  This parameter may be null.
        /// </param>
        /// <param name="name1">
        /// The first name to compare.  This parameter may be null.
        /// </param>
        /// <param name="name2">
        /// The second name to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if both call frames refer to the same instance and the two
        /// names are equal; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method gets the hash value stored in the auxiliary data of the
        /// specified call frame.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The hash value bytes, or null if the call frame is null or has no
        /// hash value.
        /// </returns>
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

        /// <summary>
        /// This method gets the hash value stored in the auxiliary data of the
        /// specified call frame, formatted as a string.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted hash string, or null if the call frame is null or has
        /// no hash value.
        /// </returns>
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

        /// <summary>
        /// This method generates an automatic, unique name for a scope call
        /// frame using the interpreter's next identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the next unique identifier.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The generated scope name, or null if the interpreter is null.
        /// </returns>
        public static string GetAutomaticScopeName(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return FormatOps.Id(ScopePrefix, null, interpreter.NextId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates an automatic name for a procedure or lambda
        /// scope call frame associated with the specified call frame.  When the
        /// scope is shared, the name omits a per-thread identifier so that the
        /// scope is shared by all threads.
        /// </summary>
        /// <param name="frame">
        /// The call frame whose name and flags determine the generated scope
        /// name.  This parameter may be null.
        /// </param>
        /// <param name="shared">
        /// Non-zero to generate a name that is shared across all threads (i.e.
        /// with no per-thread identifier).
        /// </param>
        /// <returns>
        /// The generated scope name, or null if the call frame is null.
        /// </returns>
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

        /// <summary>
        /// This method computes the call frame flags needed to convert a global
        /// scope call frame into a normal (non-global) scope call frame.
        /// </summary>
        /// <param name="frame">
        /// The global scope call frame to query.  This parameter may be null.
        /// </param>
        /// <param name="newFlags">
        /// Upon success, this contains the flags for the non-global scope.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the new flags were computed successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method marks a normal scope call frame as a global scope call
        /// frame.
        /// </summary>
        /// <param name="frame">
        /// The normal scope call frame to mark.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the call frame was marked as a global scope; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method creates a new engine scope call frame for the specified
        /// interpreter, with an automatically generated name and an empty set of
        /// variables and arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which to create the engine scope call frame.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The new engine scope call frame, or null if the interpreter is null.
        /// </returns>
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

        /// <summary>
        /// This method stores the specified call frame into the supplied value
        /// data, when the value data is available.
        /// </summary>
        /// <param name="frame">
        /// The call frame to store.  This parameter may be null.
        /// </param>
        /// <param name="valueData">
        /// The value data to store the call frame into.  This parameter may be
        /// null, in which case nothing is done.
        /// </param>
        /// <returns>
        /// True if the call frame was stored; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method stores the specified call frame into the supplied result,
        /// optionally creating an empty result when one is not already present.
        /// </summary>
        /// <param name="frame">
        /// The call frame to store.  This parameter may be null.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an empty result when none is provided.
        /// </param>
        /// <param name="result">
        /// The result to store the call frame into.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call frame was stored; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method clones the variables of the current (or global) variable
        /// call frame into a brand new scope call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the call frames.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The name to assign to the new scope call frame.
        /// </param>
        /// <param name="global">
        /// Non-zero to clone from the current global call frame; zero to clone
        /// from the current call frame.
        /// </param>
        /// <param name="byRef">
        /// Non-zero to share the existing variable instances by reference; zero
        /// to create independent copies re-parented to the new scope.
        /// </param>
        /// <param name="newVariables">
        /// Upon success, this contains the new variable collection.
        /// </param>
        /// <param name="newFrame">
        /// Upon success, this contains the new scope call frame, when one was
        /// created.
        /// </param>
        /// <param name="created">
        /// Upon success, this is non-zero if a new scope call frame was
        /// created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method clones the variables of the current (or global) variable
        /// call frame into an existing scope call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the call frames.  This parameter may be
        /// null.
        /// </param>
        /// <param name="targetFrame">
        /// The existing scope call frame to clone the variables into.  This
        /// parameter may be null.
        /// </param>
        /// <param name="global">
        /// Non-zero to clone from the current global call frame; zero to clone
        /// from the variable call frame of the target.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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
        /// <summary>
        /// This method counts the call frames between the current call frame and
        /// the bottom of the call stack that match the specified flag criteria.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame from which to start counting.  This parameter
        /// may be null, indicating the global scope (a count of zero).
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to be counted, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to be counted, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <param name="count">
        /// Upon return, this is incremented by the number of matching call
        /// frames found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a call frame matching the specified
        /// level and flag criteria exists on the call stack, discarding the
        /// located call frame.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame used as the starting point for relative level
        /// searches.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to interpret <paramref name="level" /> as an absolute level
        /// (measured inward from the global call frame); zero to interpret it as
        /// relative to the current call frame.
        /// </param>
        /// <param name="level">
        /// The level of the call frame to find.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a matching call frame was found;
        /// otherwise, an appropriate error return code.
        /// </returns>
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

        /// <summary>
        /// This method locates the call frame matching the specified level and
        /// flag criteria on the call stack, discarding any error message.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame used as the starting point for relative level
        /// searches.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to interpret <paramref name="level" /> as an absolute level
        /// (measured inward from the global call frame); zero to interpret it as
        /// relative to the current call frame.
        /// </param>
        /// <param name="level">
        /// The level of the call frame to find.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <param name="frame">
        /// Upon success, this contains the located call frame.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a matching call frame was found;
        /// otherwise, an appropriate error return code.
        /// </returns>
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

        /// <summary>
        /// This method locates the call frame matching the specified level and
        /// flag criteria on the call stack.  An absolute level of zero selects
        /// the outermost matching call frame, a relative level of zero selects
        /// the innermost matching call frame, and positive levels select the Nth
        /// matching call frame inward or outward, respectively; negative levels
        /// are always an error.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame used as the starting point for relative level
        /// searches.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to interpret <paramref name="level" /> as an absolute level
        /// (measured inward from the global call frame); zero to interpret it as
        /// relative to the current call frame.
        /// </param>
        /// <param name="level">
        /// The level of the call frame to find.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <param name="frame">
        /// Upon success, this contains the located call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a matching call frame was found;
        /// otherwise, an appropriate error return code.
        /// </returns>
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
        /// <summary>
        /// This method follows the chain of previous call frames, returning the
        /// earliest one.
        /// </summary>
        /// <param name="frame">
        /// The call frame from which to begin following the chain.
        /// </param>
        /// <returns>
        /// The earliest call frame in the chain, or null if the specified call
        /// frame is null.
        /// </returns>
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

        /// <summary>
        /// This method follows the chain of next call frames starting from the
        /// specified call frame and returns the last one in the chain.
        /// </summary>
        /// <param name="frame">
        /// The call frame at which to start following.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The last call frame in the next chain, or null if the supplied call
        /// frame is null.
        /// </returns>
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

        /// <summary>
        /// This method locates the index, within the call stack, of the call
        /// frame matching the specified level and flag criteria.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to interpret <paramref name="level" /> as an absolute level
        /// (measured inward from the global call frame); zero to interpret it as
        /// relative to the current call frame.
        /// </param>
        /// <param name="level">
        /// The level of the call frame to find.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <param name="index">
        /// Upon success, this contains the index of the located call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a matching call frame was found;
        /// otherwise, an appropriate error return code.
        /// </returns>
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

        /// <summary>
        /// This method locates the index, within the call stack, of the
        /// specified call frame.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The call frame whose index is to be found.  This parameter may be
        /// null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to start searching at the outermost call frame; zero to
        /// start at the innermost call frame.
        /// </param>
        /// <param name="level">
        /// The level parameter (currently unused by this overload).
        /// </param>
        /// <param name="index">
        /// Upon success, this contains the index of the located call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the call frame was found; otherwise,
        /// an appropriate error return code.
        /// </returns>
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

        /// <summary>
        /// This method gets the global call frame for absolute level zero, or
        /// otherwise locates the call frame matching the specified level and
        /// flag criteria, discarding any error message.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="globalFrame">
        /// The actual outermost global call frame.  This parameter may be null.
        /// </param>
        /// <param name="currentGlobalFrame">
        /// The current global call frame, which may be a scope call frame.  This
        /// parameter may be null.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame used as the starting point for relative level
        /// searches.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to interpret <paramref name="level" /> as an absolute level;
        /// zero to interpret it as relative to the current call frame.
        /// </param>
        /// <param name="super">
        /// Non-zero, for absolute level zero, to return the actual outermost
        /// global call frame instead of the current global call frame.
        /// </param>
        /// <param name="level">
        /// The level of the call frame to find.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <param name="frame">
        /// Upon success, this contains the resulting call frame.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a call frame was found; otherwise, an
        /// appropriate error return code.
        /// </returns>
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

        /// <summary>
        /// This method gets the global call frame for absolute level zero, or
        /// otherwise locates the call frame matching the specified level and
        /// flag criteria.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="globalFrame">
        /// The actual outermost global call frame.  This parameter may be null.
        /// </param>
        /// <param name="currentGlobalFrame">
        /// The current global call frame, which may be a scope call frame.  This
        /// parameter may be null.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame used as the starting point for relative level
        /// searches.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to interpret <paramref name="level" /> as an absolute level;
        /// zero to interpret it as relative to the current call frame.
        /// </param>
        /// <param name="super">
        /// Non-zero, for absolute level zero, to return the actual outermost
        /// global call frame instead of the current global call frame.
        /// </param>
        /// <param name="level">
        /// The level of the call frame to find.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to match, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <param name="frame">
        /// Upon success, this contains the resulting call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a call frame was found; otherwise, an
        /// appropriate error return code.
        /// </returns>
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

        /// <summary>
        /// This method marks (or unmarks) the call frames matching the specified
        /// level and flag criteria, recording the marking round using the unique
        /// identifier of the current call frame.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame used as the starting point and to identify the
        /// marking round.  This parameter may be null.
        /// </param>
        /// <param name="absolute">
        /// Non-zero to interpret <paramref name="level" /> as an absolute level;
        /// zero to interpret it as relative to the current call frame.
        /// </param>
        /// <param name="level">
        /// The level of the call frame at which to stop marking.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a call frame must have to be marked, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a call frame must not have to be marked, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="markFlags">
        /// The flags to apply to each matching call frame's mark.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set
        /// for the call frame to be excluded.
        /// </param>
        /// <param name="mark">
        /// Non-zero to add the mark; zero to remove it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        /// <summary>
        /// This method builds a new call stack by traversing the supplied call
        /// stack, either copying all call frames or only those that participate
        /// in the <c>[info level]</c> view, subject to an optional limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the call stack.  This parameter may be
        /// null.
        /// </param>
        /// <param name="callStack">
        /// The call stack to traverse.  This parameter may be null.
        /// </param>
        /// <param name="skipFrame">
        /// The call frame to skip during traversal (subject to legacy host
        /// behavior).  This parameter may be null.
        /// </param>
        /// <param name="limit">
        /// The maximum number of call frames to include, or
        /// <see cref="Limits.Unlimited" /> for no limit.
        /// </param>
        /// <param name="all">
        /// Non-zero to include all call frames; zero to include only those
        /// visible to the <c>[info level]</c> view.
        /// </param>
        /// <param name="newCallStack">
        /// Upon success, this contains the newly constructed call stack.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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
        /// <summary>
        /// This method gets the flags of the specified call frame without
        /// allowing any exception to propagate.
        /// </summary>
        /// <param name="frame">
        /// The call frame to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The call frame flags, or null if the call frame is null or its flags
        /// could not be obtained.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified set of call frame flags
        /// satisfies the supplied match criteria.
        /// </summary>
        /// <param name="flags">
        /// The call frame flags to test.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that must be present, or <see cref="CallFrameFlags.None" />
        /// for no requirement.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that are checked for the exclusion test, or
        /// <see cref="CallFrameFlags.None" /> for no requirement.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of <paramref name="hasFlags" /> to be set.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of <paramref name="notHasFlags" /> to be set.
        /// </param>
        /// <returns>
        /// True if the flags satisfy the match criteria; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified call stack is empty.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to query.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The value to return when the call stack is null.
        /// </param>
        /// <returns>
        /// True if the call stack is empty; otherwise, false.  When the call
        /// stack is null, <paramref name="default" /> is returned.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a call frame can be pushed onto the
        /// specified call stack.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call stack is non-null; otherwise, false.
        /// </returns>
        public static bool CanPush(
            CallStack callStack
            )
        {
            return (callStack != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a call frame can be peeked at or
        /// popped from the specified call stack.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the call stack is non-null and non-empty; otherwise, false.
        /// </returns>
        public static bool CanPeekOrPop(
            CallStack callStack
            )
        {
            return ((callStack != null) && (callStack.Count > 0));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a call frame can be peeked at or
        /// popped from the specified call stack and, when possible, returns the
        /// call frame at the top of the stack.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to query.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// Upon success, this contains the call frame at the top of the stack.
        /// </param>
        /// <returns>
        /// True if the call stack is non-null and non-empty; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the call frame at the specified index
        /// can be peeked at or popped from the call stack and, when possible,
        /// returns that call frame.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to query.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The index of the call frame, relative to the top of the stack.
        /// </param>
        /// <param name="frame">
        /// Upon success, this contains the call frame at the specified index.
        /// </param>
        /// <returns>
        /// True if the call stack is non-null and contains a call frame at the
        /// specified index; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method peeks at the call frame at the top of the specified call
        /// stack without removing it.  The caller must ensure the call stack is
        /// not null.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to peek at.
        /// </param>
        /// <returns>
        /// The call frame at the top of the call stack.
        /// </returns>
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
        /// <summary>
        /// This method pops the call frame at the top of the specified call
        /// stack and updates the current call frame to refer to the new top of
        /// the stack.  The caller must ensure the call stack is not null.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to pop from.
        /// </param>
        /// <param name="currentFrame">
        /// Upon return, this contains the new top of the call stack, or null if
        /// the call stack is now empty.
        /// </param>
        /// <returns>
        /// The call frame that was popped.
        /// </returns>
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
        /// <summary>
        /// This method pushes a new call frame onto the specified call stack and
        /// updates the current call frame to refer to it.  The caller must
        /// ensure the call stack is not null.
        /// </summary>
        /// <param name="callStack">
        /// The call stack to push onto.
        /// </param>
        /// <param name="newFrame">
        /// The new call frame to push.
        /// </param>
        /// <param name="currentFrame">
        /// Upon return, this contains the new top of the call stack (i.e. the
        /// call frame that was just pushed).
        /// </param>
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
        /// <summary>
        /// This method cleans up the variables owned by the specified call
        /// frame, removing variables that are null, undefined, or going out of
        /// scope, and also removing the now-undefined targets of any variable
        /// links.
        /// </summary>
        /// <param name="currentFrame">
        /// The current call frame, whose level determines which variables are
        /// considered to be going out of scope.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to clean up.  This parameter may be null.
        /// </param>
        /// <param name="undefined">
        /// Non-zero to additionally flag the call frame as undefined after
        /// cleanup.
        /// </param>
        /// <returns>
        /// The number of variables removed, or <c>Count.Invalid</c> if the call
        /// frame is null or has no variables.
        /// </returns>
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

        /// <summary>
        /// This method purges the undefined variables from the current variable
        /// call frame of the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose current variable call frame is to be purged.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains a message describing how many variables
        /// were purged; upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method moves (or copies) named variables from one variable
        /// collection to another, optionally restricted to (or excluding) a set
        /// of named arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when cloning variables.  This parameter
        /// may be null.
        /// </param>
        /// <param name="sourceVariables">
        /// The source variable collection.  This parameter may be null.
        /// </param>
        /// <param name="targetVariables">
        /// The target variable collection.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The optional set of named arguments that restricts (or, with
        /// <paramref name="excludeNames" />, excludes) which variables are
        /// moved.  This parameter may be null, meaning all source variables.
        /// </param>
        /// <param name="excludeNames">
        /// Non-zero to treat <paramref name="arguments" /> as a set of names to
        /// exclude rather than include.
        /// </param>
        /// <param name="skipExisting">
        /// Non-zero to skip variables that already exist in the target
        /// collection.
        /// </param>
        /// <param name="copyExisting">
        /// Non-zero to deep-clone each variable before storing it in the target
        /// collection.
        /// </param>
        /// <param name="keepExisting">
        /// Non-zero to keep the variable in the source collection (i.e. copy
        /// rather than move).
        /// </param>
        /// <param name="count">
        /// Upon return, this is incremented by the number of move, copy, and
        /// removal operations performed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error return code.
        /// </returns>
        public static ReturnCode MoveNamedVariables(
            Interpreter interpreter,            /* in */
            VariableDictionary sourceVariables, /* in, out */
            VariableDictionary targetVariables, /* in, out */
            ArgumentDictionary arguments,       /* in: OPTIONAL */
            bool excludeNames,                  /* in */
            bool skipExisting,                  /* in */
            bool copyExisting,                  /* in */
            bool keepExisting,                  /* in */
            ref int count,                      /* in, out */
            ref Result error                    /* out */
            )
        {
            if (sourceVariables == null)
            {
                error = "invalid source variables";
                return ReturnCode.Error;
            }

            if (targetVariables == null)
            {
                error = "invalid target variables";
                return ReturnCode.Error;
            }

            ArgumentDictionary newArguments;

            if (arguments == null)
            {
                newArguments = new ArgumentDictionary(
                    sourceVariables.Keys);
            }
            else if (excludeNames)
            {
                newArguments = new ArgumentDictionary();

                foreach (string name in sourceVariables.Keys)
                {
                    if (name == null) /* IMPOSSIBLE */
                        continue;

                    if (arguments.ContainsKey(name))
                        continue;

                    newArguments.Add(name, (Argument)null);
                }
            }
            else
            {
                newArguments = arguments;
            }

            foreach (ArgumentPair pair in newArguments)
            {
                string name = pair.Key;

                if (name == null)
                    continue;

                IVariable value;

                if (!sourceVariables.TryGetValue(name, out value))
                    continue;

                if (skipExisting && targetVariables.ContainsKey(name))
                    continue;

                if (copyExisting)
                {
                    value = value.Clone(
                        interpreter, CloneFlags.DeepMask, ref error);

                    if (value == null)
                        return ReturnCode.Error;

                    count++;
                }

                targetVariables[name] = value;
                count++;

                if (!keepExisting && sourceVariables.Remove(name))
                    count++;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
