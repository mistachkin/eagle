/*
 * Automatic.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using DelegateList = System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>;

using DelegateTriplet = Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>;

using DelegateCache = System.Collections.Generic.Dictionary<
    System.Reflection.MethodBase, System.Delegate>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements an Eagle automatic command, which is a
    /// dynamically created ensemble that exposes the methods of a managed
    /// object (or type) as sub-commands, dispatching each one through a
    /// delegate.  It maps sub-command names to method overloads via an
    /// <see cref="IDelegateMapper" />, lazily creates and caches the
    /// associated delegates, and enforces "safe" interpreter permissions on
    /// the methods it exposes.  See <c>core_language.md</c> for the command
    /// syntax and semantics.
    /// </summary>
    [ObjectId("34822d82-6e90-4883-b469-e2680fe85b46")]
    [CommandFlags(
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.Automatic
    )]
    [ObjectGroup("delegate")]
    public class Automatic : Default
    {
        #region Private Constants
        /// <summary>
        /// The format string used to build the "wrong # args" error message
        /// when this command is invoked without enough arguments.  The single
        /// format placeholder is filled with the command name.
        /// </summary>
        private const string WrongNumArgsFormat =
            "wrong # args: should be \"{0} ?options? method ?arg ...?\"";

        /// <summary>
        /// The format string used to build the "permission denied" error
        /// message when a "safe" interpreter attempts to use a method or
        /// sub-command that is not marked as safe.  The first placeholder is
        /// the context type and the second is the denied entity description.
        /// </summary>
        private const string PermissionDeniedFormat =
            "permission denied: safe {0} cannot use {1}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// command instance across threads.
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// The typed instance (i.e. the target type together with its
        /// optional object instance) whose methods are exposed as the
        /// sub-commands of this ensemble.
        /// </summary>
        private TypedInstance typedInstance;

        /// <summary>
        /// The delegate mapper used to look up method overloads for a given
        /// sub-command name and to enumerate the available sub-commands.
        /// </summary>
        private IDelegateMapper mapper;

        /// <summary>
        /// The cache of delegates that have already been created, keyed by the
        /// method they invoke, used to avoid repeatedly recreating them.
        /// </summary>
        private DelegateCache cache;

        /// <summary>
        /// The flags that control how the target delegates are created,
        /// looked up, and invoked by this command.
        /// </summary>
        private DelegateFlags delegateFlags;

        /// <summary>
        /// The optional per-command override of the "safe" treatment for this
        /// command.  When null, the containing interpreter's safe state is
        /// used instead.
        /// </summary>
        private bool? safe;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the automatic command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Automatic(
            ICommandData commandData /* in */
            )
            : base(commandData)
        {
            //
            // NOTE: This is not a strictly vanilla "command", it is a
            //       wrapped ensemble with per sub-command delegates.
            //
            this.Kind |= IdentifierKind.Ensemble | IdentifierKind.Automatic;

            //
            // NOTE: Normally, this flags assignment is performed by
            //       _Commands.Core for all commands residing in the core
            //       library; however, this class does not inherit from
            //       _Commands.Core.
            //
            if ((commandData == null) || !FlagOps.HasFlags(
                    commandData.Flags, CommandFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetCommandFlags(GetType().BaseType) |
                    AttributeOps.GetCommandFlags(this);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the automatic command and initializes it
        /// with the target instance, delegate mapper, delegate flags, and
        /// safe override that drive its sub-command dispatching.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        /// <param name="typedInstance">
        /// The typed instance whose methods are exposed as the sub-commands of
        /// this ensemble.
        /// </param>
        /// <param name="mapper">
        /// The delegate mapper used to look up method overloads for the
        /// exposed sub-commands.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags that control how the target delegates are created,
        /// looked up, and invoked.
        /// </param>
        /// <param name="safe">
        /// The optional per-command override of the "safe" treatment.  When
        /// null, the containing interpreter's safe state is used instead.
        /// </param>
        public Automatic(
            ICommandData commandData,    /* in */
            TypedInstance typedInstance, /* in */
            IDelegateMapper mapper,      /* in */
            DelegateFlags delegateFlags, /* in */
            bool? safe                   /* in */
            )
            : this(commandData)
        {
            Initialize(typedInstance, mapper, delegateFlags, safe);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// Gets the dictionary of sub-commands exposed by this ensemble,
        /// computed from the methods of the target type as reported by the
        /// delegate mapper, honoring the current "safe" treatment.  Returns
        /// null when no delegate mapper is set or when the sub-commands cannot
        /// be enumerated.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (mapper == null)
                        return null;

                    Interpreter interpreter = Interpreter.GetActive();
                    ContextType contextType;

                    bool treatAsSafe = ShouldTreatAsSafe(
                        interpreter, out contextType);

                    EnsembleDictionary subCommands = null;
                    Result error = null;

                    if (mapper.ToList(
                            interpreter, GetTargetType(), null,
                            null, StringOps.DefaultMatchMode,
                            MarshalFlags.Default |
                                MarshalFlags.NamesOnly |
                                MarshalFlags.UnqualifiedNames,
                            false, treatAsSafe, ref subCommands,
                            ref error) != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "SubCommands: Error {0} {1} {2}",
                            contextType,
                            FormatOps.InterpreterNoThrow(
                                interpreter),
                            FormatOps.WrapOrNull(error)),
                            typeof(Automatic).Name,
                            TracePriority.ScriptError);

                        return null;
                    }

                    return subCommands;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method initializes the mutable state of this command with the
        /// supplied target instance, delegate mapper, delegate flags, and safe
        /// override, and creates a fresh delegate cache.
        /// </summary>
        /// <param name="typedInstance">
        /// The typed instance whose methods are exposed as the sub-commands of
        /// this ensemble.
        /// </param>
        /// <param name="mapper">
        /// The delegate mapper used to look up method overloads for the
        /// exposed sub-commands.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags that control how the target delegates are created,
        /// looked up, and invoked.
        /// </param>
        /// <param name="safe">
        /// The optional per-command override of the "safe" treatment.  When
        /// null, the containing interpreter's safe state is used instead.
        /// </param>
        private void Initialize(
            TypedInstance typedInstance, /* in */
            IDelegateMapper mapper,      /* in */
            DelegateFlags delegateFlags, /* in */
            bool? safe                   /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                this.typedInstance = typedInstance;
                this.mapper = mapper;
                this.delegateFlags = delegateFlags;
                this.safe = safe;
                this.cache = new DelegateCache();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the delegate cache and, optionally, discards it
        /// entirely.
        /// </summary>
        /// <param name="reset">
        /// Non-zero to discard the cache (setting it to null) after clearing
        /// it; zero to clear it but keep it available for reuse.
        /// </param>
        /// <returns>
        /// The number of entries that were present in the cache before it was
        /// cleared; otherwise, <see cref="Count.Invalid" /> when there was no
        /// cache.
        /// </returns>
        private int ClearAndMaybeResetCache(
            bool reset /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (cache != null)
                {
                    int result = cache.Count;

                    cache.Clear();

                    if (reset)
                        cache = null;

                    return result;
                }
                else
                {
                    return Count.Invalid;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this command, resetting
        /// the safe override and delegate flags, discarding the delegate
        /// cache, disposing of the delegate mapper, and resetting the typed
        /// instance.  Any exception thrown during cleanup is traced and
        /// suppressed.
        /// </summary>
        private void Terminate()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    safe = null;
                    delegateFlags = DelegateFlags.None;

                    ClearAndMaybeResetCache(true);

                    if (mapper != null)
                    {
                        mapper.Dispose();
                        mapper = null;
                    }

                    if (typedInstance != null)
                    {
                        typedInstance.Reset();
                        typedInstance = null;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Automatic).Name,
                        TracePriority.CleanupError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the target type whose methods are exposed as
        /// the sub-commands of this ensemble, as derived from the typed
        /// instance.
        /// </summary>
        /// <returns>
        /// The target type, or null when it cannot be determined.
        /// </returns>
        private Type GetTargetType()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return MarshalOps.GetType(typedInstance);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the target object instance on which the
        /// supplied method should be invoked.
        /// </summary>
        /// <param name="method">
        /// The method that is about to be invoked.  When this method is static
        /// (or null), no target object is required.
        /// </param>
        /// <returns>
        /// The target object instance from the typed instance, or null when
        /// there is no typed instance or when <paramref name="method" /> is
        /// static.
        /// </returns>
        private object GetTargetObject(
            MethodBase method /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (typedInstance == null)
                    return null;

                if ((method != null) && method.IsStatic)
                    return null;

                return typedInstance.Object;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the typed instance has an object instance
        /// of its target type, creating one (via its public parameterless
        /// constructor) when necessary.
        /// </summary>
        /// <param name="count">
        /// On input, the running count of affected items; this is incremented
        /// by one when a new object instance is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero when a new object instance was created; false when the
        /// typed instance already had an object instance; or null when an
        /// error occurred, with details placed in <paramref name="error" />.
        /// </returns>
        private bool? MaybeCreateTargetObject(
            ref int count,   /* in, out */
            ref Result error /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (typedInstance == null)
                {
                    error = "invalid typed instance";
                    return null;
                }

                if (typedInstance.Object != null)
                    return false;

                Type type = typedInstance.Type;

                if (type == null)
                {
                    error = "invalid object type";
                    return null;
                }

                try
                {
                    typedInstance = new TypedInstance(
                        type, typedInstance.ObjectFlags,
                        Activator.CreateInstance(type),
                        typedInstance.ObjectName,
                        typedInstance.FullObjectName,
                        typedInstance.ExtraParts
                    );

                    count++;
                    return true;
                }
                catch (Exception e)
                {
                    error = e;
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps a sub-command name to the name of the method that
        /// implements it.  Currently the sub-command name is returned verbatim.
        /// </summary>
        /// <param name="subCommandName">
        /// The name of the sub-command being dispatched.
        /// </param>
        /// <returns>
        /// The name of the method that implements the sub-command.
        /// </returns>
        private string GetMethodName(
            string subCommandName /* in */
            )
        {
            //
            // HACK: For now, just return the sub-command name verbatim;
            //       in the future, this may be different, e.g. using a
            //       lowercase name, etc.
            //
            return subCommandName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether method and sub-command access should
        /// be treated as "safe", preferring the per-command safe override when
        /// one is set and otherwise falling back to the interpreter's safe
        /// state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose safe state is consulted when no
        /// per-command override is set.  May be null.
        /// </param>
        /// <param name="contextType">
        /// Upon return, this indicates whether the safe decision came from the
        /// command (<see cref="ContextType.command" />) or the interpreter
        /// (<see cref="ContextType.interpreter" />).
        /// </param>
        /// <returns>
        /// Non-zero when access should be treated as safe; otherwise, zero.
        /// </returns>
        private bool ShouldTreatAsSafe(
            Interpreter interpreter,    /* in */
            out ContextType contextType /* out */
            )
        {
            if (safe != null)
            {
                contextType = ContextType.command;
                return (bool)safe;
            }

            contextType = ContextType.interpreter;

            if (interpreter == null)
                return false;

            return interpreter.InternalIsSafe();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve a previously cached delegate for
        /// the supplied method.
        /// </summary>
        /// <param name="method">
        /// The method whose cached delegate is being requested.
        /// </param>
        /// <param name="delegate">
        /// Upon success, this contains the cached delegate; otherwise, it is
        /// set to null.
        /// </param>
        /// <returns>
        /// Non-zero when a cached delegate was found; otherwise, zero.
        /// </returns>
        private bool TryGetFromCache(
            MethodBase method,     /* in */
            out Delegate @delegate /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((method == null) || (cache == null))
                {
                    @delegate = null;
                    return false;
                }

                return cache.TryGetValue(method, out @delegate);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to store a delegate in the cache, keyed by the
        /// method it invokes.
        /// </summary>
        /// <param name="method">
        /// The method that serves as the cache key.
        /// </param>
        /// <param name="delegate">
        /// The delegate to cache for the supplied method.
        /// </param>
        /// <returns>
        /// Non-zero when the delegate was cached; otherwise, zero (for
        /// example, when the method, delegate, or cache is null).
        /// </returns>
        private bool TryPutInCache(
            MethodBase method, /* in */
            Delegate @delegate /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((method == null) || (@delegate == null) ||
                    (cache == null))
                {
                    return false;
                }

                cache[method] = @delegate;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the dynamically created delegates held by the
        /// delegate mapper and the local delegate cache, so they can be
        /// (re-)created on demand.
        /// </summary>
        /// <param name="delegatesOnly">
        /// Non-zero to clear only the delegates; zero to also clear the
        /// underlying mapped types.
        /// </param>
        /// <param name="count">
        /// On input, the running count of affected items; this is increased by
        /// the number of delegates that were cleared.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        private ReturnCode ClearDelegates(
            bool delegatesOnly, /* in */
            ref int count,      /* in, out */
            ref Result error    /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((mapper != null) && mapper.Clear(
                        delegatesOnly, ref count,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            count += ClearAndMaybeResetCache(false);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates that a method overload may be used (enforcing
        /// "safe" permissions) and ensures a delegate exists for it, either by
        /// reusing a cached delegate, reusing one already stored on the
        /// triplet, or creating a new one and caching it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.
        /// </param>
        /// <param name="outerDelegate">
        /// The triplet describing the candidate method overload and its
        /// associated delegate; its delegate may be created and stored back
        /// into it.
        /// </param>
        /// <param name="objectType">
        /// The target type that declares the method, used when formatting
        /// error messages.
        /// </param>
        /// <param name="methodName">
        /// The name of the method, used when formatting error messages.
        /// </param>
        /// <param name="parameterCount">
        /// The number of formal parameters expected, used when formatting
        /// error messages.
        /// </param>
        /// <param name="index">
        /// The optional method overload index, used when formatting error
        /// messages and consulting the cached safe state.
        /// </param>
        /// <param name="treatAsSafe">
        /// Non-zero when the call is being made on behalf of a "safe"
        /// interpreter, which restricts access to methods marked as safe.
        /// </param>
        /// <param name="contextType">
        /// The context (command or interpreter) from which the safe decision
        /// originated, used when formatting error messages.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero when a usable delegate is available; otherwise, zero with
        /// details placed in <paramref name="error" />.
        /// </returns>
        private bool CheckOrCreateDelegate(
            Interpreter interpreter,       /* in */
            DelegateTriplet outerDelegate, /* in, out */
            Type objectType,               /* in */
            string methodName,             /* in */
            int parameterCount,            /* in */
            int? index,                    /* in */
            bool treatAsSafe,              /* in */
            ContextType contextType,       /* in */
            ref Result error               /* out */
            )
        {
            if (outerDelegate == null)
            {
                error = "invalid method outer delegate";
                return false;
            }

            MethodBase method = outerDelegate.X;

            if (method == null)
            {
                error = "invalid method base";
                return false;
            }

            if (treatAsSafe && !AttributeOps.IsSafe(method) &&
                !AttributeOps.IsCachedSafe(interpreter, index, method))
            {
                error = String.Format(
                    PermissionDeniedFormat, contextType,
                    String.Format("method overload {0}",
                    DelegateMapper.FormatErrorMessage(
                    objectType, methodName, parameterCount,
                    index)));

                return false;
            }

            //
            // HACK: Do not move this check above the "safe"
            //       interpreter checking (above), just for
            //       the case where the interpreter changes
            //       its "safe" state after the method being
            //       checked was added to the cache.
            //
            Delegate localDelegate;

            if (TryGetFromCache(method, out localDelegate))
                return true;

            Type delegateType = null;

            if (!DelegateOps.CreateDelegateType(
                    interpreter, method, ref delegateType,
                    ref error))
            {
                return false;
            }

            localDelegate = outerDelegate.Y;

            if (localDelegate == null)
            {
                try
                {
                    localDelegate = Delegate.CreateDelegate(
                        delegateType, GetTargetObject(method),
                        method as MethodInfo, true);

                    if (localDelegate != null)
                    {
                        outerDelegate.Y = localDelegate; /* SAVE */
                    }
                    else
                    {
                        error = "failed delegate creation";
                        return false;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    return false;
                }
            }

            /* IGNORED */
            TryPutInCache(method, localDelegate);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a sub-command name to the list of delegates
        /// that may handle it, looking up the matching method overloads,
        /// enforcing "safe" permissions, and creating delegates as needed.
        /// When a specific overload index is supplied, only that overload is
        /// resolved; otherwise, all matching overloads are resolved.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.
        /// </param>
        /// <param name="subCommandName">
        /// The name of the sub-command being dispatched.
        /// </param>
        /// <param name="parameterCount">
        /// The number of formal method parameters expected.
        /// </param>
        /// <param name="limit">
        /// The optional maximum number of method overloads to consider.
        /// </param>
        /// <param name="index">
        /// The optional index selecting a single method overload; when null,
        /// all matching overloads are considered.
        /// </param>
        /// <param name="delegates">
        /// Upon success, this contains the list of resolved delegate triplets;
        /// upon failure, it may be an empty or partial list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero when at least one usable delegate was resolved; otherwise,
        /// zero with details placed in <paramref name="error" />.
        /// </returns>
        private bool TryGetDelegate(
            Interpreter interpreter,    /* in */
            string subCommandName,      /* in */
            int parameterCount,         /* in */
            int? limit,                 /* in */
            int? index,                 /* in */
            out DelegateList delegates, /* out */
            ref Result error            /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                delegates = new DelegateList();

                ContextType contextType;

                bool treatAsSafe = ShouldTreatAsSafe(
                    interpreter, out contextType);

                Result localError = null;

                if (subCommandName == null)
                {
                    localError = "invalid sub-command name";
                    goto error;
                }

                if (mapper == null)
                {
                    localError = "invalid delegate mapper";
                    goto error;
                }

                Type objectType = GetTargetType();

                if (objectType == null)
                {
                    localError = "invalid object type";
                    goto error;
                }

                string methodName = GetMethodName(subCommandName);
                DelegateList localDelegates = null;

                localError = null;

                if ((mapper.Lookup(
                        objectType, methodName, parameterCount,
                        limit, index, ref localDelegates,
                        ref localError) != ReturnCode.Ok) ||
                    (localDelegates == null))
                {
                    if (localError == null)
                        localError = "method list is invalid or not found";

                    goto error;
                }

                int localCount = localDelegates.Count;
                int localIndex;
                DelegateTriplet localDelegate;

                if (index != null)
                {
                    localIndex = (int)index;

                    if ((localIndex < 0) || (localIndex >= localCount))
                    {
                        if (localCount > 0)
                        {
                            localError = String.Format(
                                "method overload index must be {0}",
                                FormatOps.BetweenOrExact(0, localCount - 1));
                        }
                        else
                        {
                            localError = "method overload list is empty";
                        }

                        goto error;
                    }

                    localDelegate = localDelegates[localIndex];
                    localError = null;

                    if (!CheckOrCreateDelegate(
                            interpreter, localDelegate, objectType,
                            methodName, parameterCount, index,
                            treatAsSafe, contextType, ref localError))
                    {
                        goto error;
                    }

                    delegates.Add(localDelegate);
                }
                else
                {
                    int added = 0;

                    for (localIndex = 0; localIndex < localCount; localIndex++)
                    {
                        localDelegate = localDelegates[localIndex];
                        localError = null;

                        if (!CheckOrCreateDelegate(
                                interpreter, localDelegate, objectType,
                                methodName, parameterCount, index,
                                treatAsSafe, contextType, ref localError))
                        {
                            goto error;
                        }

                        delegates.Add(localDelegate);
                        added++;
                    }

                    if (added == 0)
                    {
                        localError = "filtered delegate list is empty";
                        goto error;
                    }
                }

                return true;

            error:

                ResultList errors = null;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                if (!treatAsSafe)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(ScriptOps.BadSubCommand(
                        interpreter, null, null, subCommandName,
                        CreateEnsembleDictionary(parameterCount),
                        null, null));
                }
                else if (errors == null)
                {
                    errors = new ResultList();

                    errors.Add(String.Format(
                        PermissionDeniedFormat, contextType,
                        String.Format("sub-command {0}",
                        FormatOps.MaybeNull(subCommandName))));
                }

                error = errors;
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a dictionary of the sub-commands available for
        /// the target type, filtered by the supplied argument count, via the
        /// delegate mapper.  It is used, for example, when generating a "bad
        /// sub-command" error message.
        /// </summary>
        /// <param name="argumentCount">
        /// The argument count used to filter the candidate sub-commands.
        /// </param>
        /// <returns>
        /// The dictionary of matching sub-commands, or null when the target
        /// type or delegate mapper is unavailable.
        /// </returns>
        private EnsembleDictionary CreateEnsembleDictionary(
            int argumentCount /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Type objectType = GetTargetType();

                if (objectType == null)
                    return null;

                if (mapper == null)
                    return null;

                return mapper.CreateEnsemble(objectType, argumentCount);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// This method terminates this command, releasing the resources it
        /// holds (delegates, cache, mapper, and target instance) before
        /// delegating to the base class implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Terminate(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            Terminate();

            return base.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the automatic command.  It parses any leading
        /// options (such as <c>-autocreate</c>, <c>-autoflush</c>,
        /// <c>-autostatus</c>, <c>-autolimit</c>, and <c>-autoindex</c>),
        /// optionally performs the requested maintenance action, and otherwise
        /// resolves the named sub-command to a method delegate and invokes it
        /// with the remaining arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name, optionally followed by options, the sub-command
        /// (method) name, and its arguments.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value produced by the invoked
        /// method (or the count of affected items for a maintenance action).
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" />) with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            int nameCount = 2;
            int argumentCount = arguments.Count;

            if (argumentCount < nameCount)
            {
                result = String.Format(
                    WrongNumArgsFormat, this.Name);

                return ReturnCode.Error;
            }

            //
            // HACK: Remove command and sub-command names from the
            //       raw argument count, which should leave us with
            //       with the final formal method parameter count.
            //
            int parameterCount = argumentCount - nameCount;
            int nameIndex = 1; /* NOTE: Skip command prefix. */
            bool allowOptions = false;
            int argumentIndex;
            int? limit = null; /* a.k.a. methodOverloads.Count. */
            int? index = null; /* a.k.a. methodOverloads[index] */
            bool? autoCreate = null;
            bool? autoStatus = null;
            bool? autoFlush = null; /* a.k.a. drain the swamp.. */

            if (argumentCount > nameCount)
            {
                OptionDictionary options = CommandOptions.GetCommandOptions(
                    CommandOptionType.Library_Call);

                argumentIndex = Index.Invalid;

                if (interpreter.GetOptions(
                        options, arguments, 0, nameIndex,
                        Index.Invalid, false, ref argumentIndex,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                int nameNeeded = nameCount - nameIndex;

                if ((argumentIndex == Index.Invalid) ||
                    ((argumentIndex + nameNeeded) > argumentCount))
                {
                    result = String.Format(
                        WrongNumArgsFormat, this.Name);

                    return ReturnCode.Error;
                }

                parameterCount -= (argumentIndex - nameIndex);
                allowOptions = true;

                ///////////////////////////////////////////////////////////////

                if (options != null)
                {
                    IVariant value = null;

                    if (options.IsPresent("-autolimit", ref value))
                        limit = (int)value.Value;

                    if (options.IsPresent("-autoindex", ref value))
                        index = (int)value.Value;

                    if (options.IsPresent("-autocreate", ref value))
                        autoCreate = (bool)value.Value;

                    if (options.IsPresent("-autostatus", ref value))
                        autoStatus = (bool)value.Value;

                    if (options.IsPresent("-autoflush", ref value))
                        autoFlush = (bool)value.Value;
                }
            }
            else
            {
                argumentIndex = 1;
            }

            //
            // HACK: If the "-autocreate" option is used, attempt
            //       to make sure the delegate target object (i.e.
            //       within our typed instance) is created, based
            //       on its delegate target type (i.e. also within
            //       our typed instance), before proceeding with
            //       sub-command dispatching.
            //
            int count = 0;

            if (autoCreate != null)
            {
                //
                // NOTE: If necessary, attempt to create an object
                //       of the delegate target type now.  It must
                //       have a public parameterless constructor.
                //       This method will return null to indicate
                //       an error.  It will return non-zero if the
                //       typed instance was modified to include an
                //       object instance of the target type.
                //
                bool? created = MaybeCreateTargetObject(
                    ref count, ref result);

                if (created == null)
                    return ReturnCode.Error;

                //
                // HACK: Next, make sure any "stale" delegates that
                //       have been created are removed, so they can
                //       be (re-)created based on the newly created
                //       object instance.
                //
                if ((bool)created)
                {
                    if (ClearDelegates(true, ref count,
                            ref result) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                //
                // HACK: If the boolean value for the "-autocreate"
                //       option is false, proceed with sub-command
                //       dispatch, which requires null-ing out the
                //       autoCreate local variable, which prevents
                //       bailing out below.
                //
                if (!(bool)autoCreate)
                    autoCreate = null;
            }

            //
            // HACK: If the "-autoflush" option is used, clear out
            //       all dynamically created delegates and their
            //       underlying types to allow for their temporary
            //       assemblies to be unloaded from this AppDomain.
            //
            if (autoFlush != null)
            {
                if (ClearDelegates(
                        (bool)autoFlush, ref count,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            //
            // HACK: If the "-autostatus" option is used, report
            //       the total number of mapped types / delegates.
            //
            if (autoStatus != null)
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if ((mapper != null) && mapper.Count(
                            (bool)autoStatus, ref count,
                            ref result) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            if ((autoCreate != null) ||
                (autoFlush != null) || (autoStatus != null))
            {
                result = count;
                return ReturnCode.Ok;
            }

            string subCommandName = arguments[argumentIndex];
            DelegateList delegates;

            if (!TryGetDelegate(
                    interpreter, subCommandName, parameterCount,
                    limit, index, out delegates, ref result))
            {
                return ReturnCode.Error;
            }

            if (delegates == null)
            {
                result = String.Format(
                    "invalid sub-command delegates for {0}",
                    FormatOps.WrapOrNull(subCommandName));

                return ReturnCode.Error;
            }

            ArgumentList newArguments;

            if (FlagOps.HasFlags(delegateFlags,
                    DelegateFlags.LookupObjects, true))
            {
                ScriptOps.LookupObjectsInArguments(
                    interpreter, arguments, out newArguments);
            }
            else
            {
                newArguments = arguments;
            }

            ReturnCode code;
            Delegate @delegate = null;
            Result returnValue = null;

            code = ScriptOps.ExecuteOrInvokeDelegate(
                interpreter, delegates, newArguments, allowOptions,
                nameCount /* cmd ?options? method ... */, nameIndex,
                delegateFlags, ref @delegate, ref returnValue);

            if (code != ReturnCode.Ok)
            {
                result = returnValue;
                return code;
            }

            return ScriptOps.HandleDelegateResult(
                interpreter, @delegate, delegateFlags, returnValue,
                ref result);
        }
        #endregion
    }
}
