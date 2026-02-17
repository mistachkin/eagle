/*
 * DelegateMapper.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using TypePair = System.Collections.Generic.KeyValuePair<
    System.Type, System.Collections.Generic.Dictionary<
    string, System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>>>>;

using TypeDictionary = System.Collections.Generic.Dictionary<
    System.Type, System.Collections.Generic.Dictionary<
    string, System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>>>>;

using MethodNamePair = System.Collections.Generic.KeyValuePair<
    string, System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>>>;

using MethodNameDictionary = System.Collections.Generic.Dictionary<
    string, System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>>>;

using ParameterCountPair = System.Collections.Generic.KeyValuePair<
    int, System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>>;

using ParameterCountDictionary = System.Collections.Generic.Dictionary<
    int, System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>>;

using DelegateList = System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>;

using DelegateTriplet = Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>;

using _Count = Eagle._Constants.Count;

namespace Eagle._Components.Public
{
    [ObjectId("d93db8c3-baa8-4aee-840a-051c14d9b7e4")]
    internal sealed class DelegateMapper :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDelegateMapper
    {
        #region Private Data
        private readonly object syncRoot = new object();
        private TypeDictionary types;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public DelegateMapper()
        {
            Initialize(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static BindingFlags GetBindingFlags(
            BindingFlags? bindingFlags
            )
        {
            if (bindingFlags != null)
                return (BindingFlags)bindingFlags;

            return ObjectOps.GetBindingFlags(
                MetaBindingFlags.PublicInstance, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static MethodBase[] GetMethodBases(
            DelegateList delegates /* in */
            )
        {
            if (delegates == null)
                return null;

            int count = delegates.Count;
            MethodBase[] methodBases = new MethodBase[count];

            for (int index = 0; index < count; index++)
            {
                DelegateTriplet outerDelegate = delegates[index];

                if (outerDelegate == null)
                    continue;

                methodBases[index] = outerDelegate.X;
            }

            return methodBases;
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybeSortDelegates(
            DelegateList delegates,     /* in */
            MarshalFlags marshalFlags,  /* in */
            DelegateFlags delegateFlags /* in */
            )
        {
            if (delegates != null)
            {
                MethodBase[] methodBases = GetMethodBases(delegates);

                MarshalOps.MaybeSortMethods(
                    methodBases, (MarshalFlags)marshalFlags);

                delegates.Clear();

                foreach (MethodBase methodBase in methodBases)
                {
                    delegates.Add(new DelegateTriplet(
                        true, methodBase, null, delegateFlags));
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static string FormatErrorMessage(
            Type objectType,     /* in: OPTIONAL */
            string methodName,   /* in: OPTIONAL */
            int? parameterCount, /* in: OPTIONAL */
            int? index           /* in: OPTIONAL */
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

            builder.AppendFormat(
                "{0}{1}{2}", FormatOps.MaybeNull(objectType),
                Type.Delimiter, FormatOps.MaybeNull(methodName));

            if (parameterCount != null)
                builder.AppendFormat("(..{0}..)", (int)parameterCount);

            if (index != null)
                builder.AppendFormat("@{0}", (int)index);

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void Initialize(
            bool force /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (force || (types == null))
                    types = new TypeDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int ClearAndMaybeReset(
            bool reset /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types != null)
                {
                    int result = types.Count;

                    types.Clear();

                    if (reset)
                        types = null;

                    return result;
                }
                else
                {
                    return _Count.Invalid;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int CountOrClearDelegates(
            bool clear /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int count = 0;

                if (types != null)
                {
                    foreach (TypePair typePair in types)
                    {
                        MethodNameDictionary methods = typePair.Value;

                        if (methods == null)
                            continue;

                        foreach (MethodNamePair methodPair in methods)
                        {
                            ParameterCountDictionary parameterCounts =
                                methodPair.Value;

                            if (parameterCounts == null)
                                continue;

                            foreach (ParameterCountPair parameterPair
                                    in parameterCounts)
                            {
                                DelegateList delegates = parameterPair.Value;

                                if (delegates == null)
                                    continue;

                                foreach (DelegateTriplet outerDelegate
                                        in delegates)
                                {
                                    if (outerDelegate == null)
                                        continue;

                                    count += (outerDelegate.Y != null) ? 1 : 0;

                                    if (clear)
                                        outerDelegate.Y = null;
                                }
                            }
                        }
                    }
                }

                return count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode Add(
            Type objectType,              /* in */
            MethodBase method,            /* in */
            MarshalFlags? marshalFlags,   /* in */
            DelegateFlags? delegateFlags, /* in */
            bool clear,                   /* in */
            ref int count,                /* in, out */
            ref Result error              /* out */
            )
        {
            if (objectType == null)
            {
                error = "invalid object type";
                return ReturnCode.Error;
            }

            if (method == null)
            {
                error = "invalid object method";
                return ReturnCode.Error;
            }

            string methodName = method.Name;

            if (methodName == null)
            {
                error = "invalid method name";
                return ReturnCode.Error;
            }

            ParameterInfo returnInfo;
            ParameterInfo[] parameterInfos;

            MarshalOps.GetParameterInfos(method,
                out returnInfo, out parameterInfos);

            if (parameterInfos == null)
            {
                error = "invalid method parameters";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types == null)
                {
                    error = "type mappings unavailable";
                    return ReturnCode.Error;
                }

                MethodNameDictionary methodNames;

                if (!types.TryGetValue(
                        objectType, out methodNames) ||
                    (methodNames == null))
                {
                    methodNames = new MethodNameDictionary(
                        StringComparer.OrdinalIgnoreCase);

                    types[objectType] = methodNames;
                    count++;
                }

                ParameterCountDictionary parameterCounts;

                if (!methodNames.TryGetValue(
                        methodName, out parameterCounts) ||
                    (parameterCounts == null))
                {
                    parameterCounts = new ParameterCountDictionary();
                    methodNames[methodName] = parameterCounts;
                    count++;
                }

                int parameterCount = parameterInfos.Length;
                DelegateList delegates;

                if (!parameterCounts.TryGetValue(
                        parameterCount, out delegates) ||
                    (delegates == null))
                {
                    delegates = new DelegateList();
                    parameterCounts[parameterCount] = delegates;
                    count++;
                }

                if (clear)
                {
                    delegates.Clear();
                    count++;
                }

                DelegateFlags localDelegateFlags = (delegateFlags != null) ?
                    (DelegateFlags)delegateFlags : DelegateFlags.Default;

                delegates.Add(new DelegateTriplet(true,
                    method, null, localDelegateFlags));

                if (marshalFlags != null)
                {
                    MaybeSortDelegates(
                        delegates, (MarshalFlags)marshalFlags,
                        localDelegateFlags);
                }

                count++;

                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegateMapper Members
        public ReturnCode Count(
            bool delegatesOnly, /* in */
            ref int count,      /* in, out */
            ref Result error    /* out: NOT USED */
            )
        {
            CheckDisposed();

            if (delegatesOnly)
            {
                count += CountOrClearDelegates(false);
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (types != null)
                        count += types.Count;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Clear(
            bool delegatesOnly, /* in */
            ref int count,      /* in, out */
            ref Result error    /* out: NOT USED */
            )
        {
            CheckDisposed();

            if (delegatesOnly)
                count += CountOrClearDelegates(true);
            else
                count += ClearAndMaybeReset(false);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Load(
            Type objectType,              /* in */
            BindingFlags? bindingFlags,   /* in */
            MarshalFlags? marshalFlags,   /* in */
            DelegateFlags? delegateFlags, /* in */
            bool clear,                   /* in */
            ref int count,                /* in, out */
            ref Result error              /* out */
            )
        {
            CheckDisposed();

            if (objectType == null)
            {
                error = "invalid object type";
                return ReturnCode.Error;
            }

            BindingFlags localBindingFlags = GetBindingFlags(
                bindingFlags);

            MethodInfo[] methods = objectType.GetMethods(
                localBindingFlags);

            int localCount = 0;

            if (methods != null)
            {
                bool localClear = clear;

                foreach (MethodInfo method in methods)
                {
                    if (method == null)
                        continue;

                    if (Add(
                            objectType, method,
                            marshalFlags, delegateFlags,
                            localClear, ref localCount,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (localClear)
                        localClear = false;
                }
            }

            if (localCount > 0)
            {
                count += localCount;
                return ReturnCode.Ok;
            }
            else
            {
                error = String.Format(
                    "no {0} methods matching {1}",
                    FormatOps.TypeName(objectType),
                    localBindingFlags);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public EnsembleDictionary CreateEnsemble(
            Type objectType,   /* in */
            int parameterCount /* in */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types == null)
                    return null;

                MethodNameDictionary methodNames;

                if (!types.TryGetValue(objectType, out methodNames) ||
                    (methodNames == null))
                {
                    return null;
                }

                StringDictionary subCommandNames = new StringDictionary();

                foreach (MethodNamePair pair in methodNames)
                {
                    ParameterCountDictionary parameterCounts = pair.Value;

                    if (parameterCounts == null)
                        continue;

                    DelegateList delegates;

                    if (!parameterCounts.TryGetValue(
                            parameterCount, out delegates) ||
                        (delegates == null) || (delegates.Count == 0))
                    {
                        continue;
                    }

                    subCommandNames[pair.Key] = null;
                }

                return new EnsembleDictionary(subCommandNames.Keys);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Lookup(
            Type objectType,            /* in */
            string methodName,          /* in */
            int parameterCount,         /* in */
            int? limit,                 /* in */
            int? index,                 /* in */
            ref DelegateList delegates, /* in */
            ref Result error            /* out */
            )
        {
            CheckDisposed();

            if (objectType == null)
            {
                error = "invalid object type";
                return ReturnCode.Error;
            }

            if (methodName == null)
            {
                error = "invalid method name";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types == null)
                {
                    error = "type mappings unavailable";
                    return ReturnCode.Error;
                }

                MethodNameDictionary methodNames;

                if (!types.TryGetValue(
                        objectType, out methodNames) ||
                    (methodNames == null))
                {
                    error = String.Format(
                        "type mapping {0} not found",
                        FormatOps.MaybeNull(objectType));

                    return ReturnCode.Error;
                }

                ParameterCountDictionary parameterCounts;

                if (!methodNames.TryGetValue(
                        methodName, out parameterCounts) ||
                    (parameterCounts == null))
                {
                    error = String.Format(
                        "method {0} not found",
                        FormatErrorMessage(
                            objectType, methodName,
                            null, null));

                    return ReturnCode.Error;
                }

                DelegateList localDelegates;

                if (!parameterCounts.TryGetValue(
                        parameterCount, out localDelegates) ||
                    (localDelegates == null))
                {
                    error = String.Format(
                        "method {0} not found",
                        FormatErrorMessage(
                            objectType, methodName,
                            parameterCount, null));

                    return ReturnCode.Error;
                }

                int localCount = localDelegates.Count;
                int localIndex; /* REUSED */

                if (index != null)
                {
                    localIndex = (int)index;

                    if ((localIndex < 0) ||
                        (localIndex >= localCount))
                    {
                        error = String.Format(
                            "method {0} not found",
                            FormatErrorMessage(
                                objectType, methodName,
                                parameterCount, localIndex));

                        return ReturnCode.Error;
                    }
                }

                if (limit != null)
                {
                    int localLimit = (int)limit;

                    if ((localLimit < 0) ||
                        (localLimit > localCount))
                    {
                        error = String.Format(
                            "bad method limit {0} versus count {1}",
                            localLimit, localCount);

                        return ReturnCode.Error;
                    }

                    localDelegates = new DelegateList(
                        localDelegates);

                    localDelegates.RemoveRange(
                        localLimit, localCount - localLimit);
                }

                delegates = localDelegates;
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ToList(
            Interpreter interpreter,            /* in */
            Type objectType,                    /* in */
            string methodName,                  /* in */
            int? parameterCount,                /* in */
            MatchMode mode,                     /* in */
            MarshalFlags marshalFlags,          /* in */
            bool noCase,                        /* in */
            bool? safe,                         /* in */
            ref EnsembleDictionary subCommands, /* out */
            ref Result error                    /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (types == null)
                {
                    error = "type mappings unavailable";
                    return ReturnCode.Error;
                }

                StringList localList = new StringList();

                foreach (TypePair typePair in types)
                {
                    if ((objectType != null) &&
                        !Object.ReferenceEquals(objectType, typePair.Key))
                    {
                        continue;
                    }

                    MethodNameDictionary methods = typePair.Value;

                    if (methods == null)
                        continue;

                    foreach (MethodNamePair methodPair in methods)
                    {
                        if ((methodName != null) && !StringOps.Match(
                                null, mode, methodPair.Key, methodName, noCase))
                        {
                            continue;
                        }

                        ParameterCountDictionary parameterCounts = methodPair.Value;

                        if (parameterCounts == null)
                            continue;

                        foreach (ParameterCountPair parameterPair in parameterCounts)
                        {
                            if ((parameterCount != null) &&
                                ((int)parameterCount != parameterPair.Key))
                            {
                                continue;
                            }

                            DelegateList delegates = parameterPair.Value;

                            if (delegates == null)
                                continue;

                            int localCount = delegates.Count;

                            for (int localIndex = 0; localIndex < localCount; localIndex++)
                            {
                                DelegateTriplet outerDelegate = delegates[localIndex];

                                if (outerDelegate == null)
                                    continue;

                                MethodBase method = outerDelegate.X;

                                if (method == null)
                                    continue;

                                if ((safe != null) &&
                                    ((bool)safe != AttributeOps.IsSafe(method)) &&
                                    ((bool)safe != AttributeOps.IsCachedSafe(
                                        interpreter, localIndex, method)))
                                {
                                    continue;
                                }

                                ParameterInfo returnInfo;
                                ParameterInfo[] parameterInfos;

                                MarshalOps.GetParameterInfos(method,
                                    out returnInfo, out parameterInfos);

                                localList.Add(FormatOps.MethodOverload(
                                    localIndex, FormatOps.TypeName(objectType),
                                    method.Name, returnInfo, parameterInfos,
                                    marshalFlags));
                            }
                        }
                    }
                }

                if (localList.Count > 0)
                {
                    if (subCommands == null)
                        subCommands = new EnsembleDictionary();

                    foreach (string element in localList)
                    {
                        if (element == null)
                            continue;

                        subCommands[element] = null;
                    }
                }

                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            ReturnCode code;
            EnsembleDictionary subCommands = null;
            Result error = null;

            code = ToList(Interpreter.GetActive(),
                null, null, null, StringOps.DefaultMatchMode,
                MarshalFlags.Default, false, null, ref subCommands,
                ref error);

            if (code != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "ToString: {0}: {1}", code,
                    FormatOps.WrapOrNull(error)),
                    typeof(IDelegateMapper).Name,
                    TracePriority.ScriptError);

                return null;
            }

            return (subCommands != null) ? subCommands.ToString() : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed &&
                Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(DelegateMapper).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(DelegateMapper).Name,
                TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    /* IGNORED */
                    ClearAndMaybeReset(true);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~DelegateMapper()
        {
            Dispose(false);
        }
        #endregion
    }
}
