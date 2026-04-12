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
    [ObjectId("34822d82-6e90-4883-b469-e2680fe85b46")]
    [CommandFlags(
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.Automatic
    )]
    [ObjectGroup("delegate")]
    public class Automatic : Default
    {
        #region Private Constants
        private const string WrongNumArgsFormat =
            "wrong # args: should be \"{0} ?options? method ?arg ...?\"";

        private const string PermissionDeniedFormat =
            "permission denied: safe {0} cannot use {1}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private readonly object syncRoot = new object();
        private TypedInstance typedInstance;
        private IDelegateMapper mapper;
        private DelegateCache cache;
        private DelegateFlags delegateFlags;
        private bool? safe;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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

        private Type GetTargetType()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return MarshalOps.GetType(typedInstance);
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
