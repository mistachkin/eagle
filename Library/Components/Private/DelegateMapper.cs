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
    /// <summary>
    /// This class maintains a thread-safe, nested mapping from object types to
    /// method names to parameter counts to the delegate triplets (method,
    /// delegate, and flags) registered for them.  It implements
    /// <see cref="IDelegateMapper" /> and is used to load, look up, enumerate,
    /// and clear the delegates associated with the methods of a type.
    /// </summary>
    [ObjectId("d93db8c3-baa8-4aee-840a-051c14d9b7e4")]
    internal sealed class DelegateMapper :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDelegateMapper
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the type mapping data of this
        /// delegate mapper.
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// The nested dictionary mapping object types to method names to
        /// parameter counts to lists of delegate triplets.
        /// </summary>
        private TypeDictionary types;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty delegate mapper, initializing its type mapping
        /// data.
        /// </summary>
        public DelegateMapper()
        {
            Initialize(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method resolves the binding flags to use when reflecting over the
        /// methods of a type, falling back to the default public instance flags
        /// when none are supplied.
        /// </summary>
        /// <param name="bindingFlags">
        /// The binding flags to use, or null to use the default public instance
        /// flags.
        /// </param>
        /// <returns>
        /// The resolved binding flags.
        /// </returns>
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

        /// <summary>
        /// This method extracts the method bases from a list of delegate
        /// triplets, preserving their order.
        /// </summary>
        /// <param name="delegates">
        /// The list of delegate triplets to extract method bases from.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// An array of method bases parallel to the supplied list, or null when
        /// the list is null.
        /// </returns>
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

        /// <summary>
        /// This method optionally sorts the delegate triplets in a list, in
        /// place, according to the specified marshal flags, rebuilding the list
        /// from the sorted method bases.
        /// </summary>
        /// <param name="delegates">
        /// The list of delegate triplets to sort.  This parameter may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshal flags controlling how the methods are sorted.
        /// </param>
        /// <param name="delegateFlags">
        /// The delegate flags to assign to each rebuilt delegate triplet.
        /// </param>
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
        /// <summary>
        /// This method formats a human-readable identifier for a mapped method,
        /// combining the object type, method name, parameter count, and index
        /// into a single string for use in error messages.
        /// </summary>
        /// <param name="objectType">
        /// The object type to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="methodName">
        /// The method name to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="parameterCount">
        /// The parameter count to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The overload index to include, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted error message fragment.
        /// </returns>
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
        /// <summary>
        /// This method initializes the type mapping data of this delegate mapper,
        /// allocating it when it is missing or when reinitialization is forced.
        /// </summary>
        /// <param name="force">
        /// Non-zero to allocate a fresh type mapping even when one already
        /// exists; zero to allocate one only when it is missing.
        /// </param>
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

        /// <summary>
        /// This method clears the type mapping data and, optionally, releases it
        /// entirely.
        /// </summary>
        /// <param name="reset">
        /// Non-zero to release the type mapping after clearing it; zero to retain
        /// the (now empty) mapping.
        /// </param>
        /// <returns>
        /// The number of type mappings that were present before clearing, or an
        /// invalid count when the mapping was unavailable.
        /// </returns>
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

        /// <summary>
        /// This method counts the bound delegates across all type mappings and,
        /// optionally, clears the bound delegate from each triplet.
        /// </summary>
        /// <param name="clear">
        /// Non-zero to clear the bound delegate from each triplet while counting;
        /// zero to only count them.
        /// </param>
        /// <returns>
        /// The number of triplets that had a bound delegate.
        /// </returns>
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

        /// <summary>
        /// This method adds a single method to the type mapping, creating any
        /// missing intermediate dictionaries (by type, method name, and parameter
        /// count) and registering a delegate triplet for the method.
        /// </summary>
        /// <param name="objectType">
        /// The object type that owns the method being added.
        /// </param>
        /// <param name="method">
        /// The method to add to the mapping.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshal flags used to optionally sort the resulting delegates, or
        /// null to leave them unsorted.
        /// </param>
        /// <param name="delegateFlags">
        /// The delegate flags to assign to the new triplet, or null to use the
        /// default flags.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear any existing delegates for the same parameter count
        /// before adding the method.
        /// </param>
        /// <param name="count">
        /// On input and output, a running count that is incremented for each
        /// dictionary or entry created or modified.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method counts either the bound delegates or the type mappings
        /// held by this delegate mapper.
        /// </summary>
        /// <param name="delegatesOnly">
        /// Non-zero to count only the bound delegates; zero to count the type
        /// mappings.
        /// </param>
        /// <param name="count">
        /// On input and output, a running count to which the computed count is
        /// added.
        /// </param>
        /// <param name="error">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method clears either the bound delegates or the entire set of
        /// type mappings held by this delegate mapper.
        /// </summary>
        /// <param name="delegatesOnly">
        /// Non-zero to clear only the bound delegates; zero to clear the type
        /// mappings.
        /// </param>
        /// <param name="count">
        /// On input and output, a running count to which the number of cleared
        /// items is added.
        /// </param>
        /// <param name="error">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method loads all of the methods of the specified type that match
        /// the given binding flags into the type mapping, adding a delegate
        /// triplet for each.
        /// </summary>
        /// <param name="objectType">
        /// The object type whose methods are loaded.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to select the methods, or null to use the
        /// default public instance flags.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshal flags used to optionally sort the resulting delegates, or
        /// null to leave them unsorted.
        /// </param>
        /// <param name="delegateFlags">
        /// The delegate flags to assign to each new triplet, or null to use the
        /// default flags.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear any existing delegates before loading the first
        /// matching method.
        /// </param>
        /// <param name="count">
        /// On input and output, a running count to which the number of created or
        /// modified entries is added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method builds an ensemble of sub-command names from the mapped
        /// method names of the specified type that have at least one delegate
        /// registered for the given parameter count.
        /// </summary>
        /// <param name="objectType">
        /// The object type whose mapped methods are queried.
        /// </param>
        /// <param name="parameterCount">
        /// The parameter count that a method must have delegates for in order to
        /// be included.
        /// </param>
        /// <returns>
        /// An ensemble dictionary of matching sub-command names, or null when the
        /// type is not mapped.
        /// </returns>
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

        /// <summary>
        /// This method looks up the list of delegate triplets registered for a
        /// specific type, method name, and parameter count, optionally validating
        /// an index and limiting the number of results returned.
        /// </summary>
        /// <param name="objectType">
        /// The object type to look up.
        /// </param>
        /// <param name="methodName">
        /// The method name to look up.
        /// </param>
        /// <param name="parameterCount">
        /// The parameter count to look up.
        /// </param>
        /// <param name="limit">
        /// The maximum number of delegates to return, or null for no limit.  When
        /// supplied, a copy of the list truncated to this length is returned.
        /// </param>
        /// <param name="index">
        /// An overload index to validate against the matched list, or null to
        /// skip validation.
        /// </param>
        /// <param name="delegates">
        /// Upon success, this receives the matched list of delegate triplets.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method enumerates the mapped method overloads, optionally
        /// filtered by type, method name, parameter count, and safety, and adds a
        /// formatted overload description for each match to the supplied ensemble.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate cached method safety.  This parameter
        /// may be null.
        /// </param>
        /// <param name="objectType">
        /// The object type to restrict the enumeration to, or null for all mapped
        /// types.
        /// </param>
        /// <param name="methodName">
        /// The method name pattern to match, or null for all method names.
        /// </param>
        /// <param name="parameterCount">
        /// The parameter count to restrict the enumeration to, or null for all
        /// parameter counts.
        /// </param>
        /// <param name="mode">
        /// The match mode used when matching the method name pattern.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshal flags used when formatting each method overload.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to match the method name pattern without regard to case.
        /// </param>
        /// <param name="safe">
        /// When non-null, restricts the enumeration to methods whose safety
        /// matches this value; null to include methods regardless of safety.
        /// </param>
        /// <param name="subCommands">
        /// On input and output, the ensemble to which the formatted method
        /// overloads are added; it is allocated when null and matches exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method produces a string describing all of the mapped method
        /// overloads held by this delegate mapper.
        /// </summary>
        /// <returns>
        /// A string listing the mapped method overloads, or null when the
        /// enumeration fails.
        /// </returns>
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
        /// <summary>
        /// This method releases all resources held by this delegate mapper and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this delegate mapper has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this delegate mapper has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this delegate mapper has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
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

        /// <summary>
        /// This method releases the resources held by this delegate mapper.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// Finalizes this delegate mapper, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~DelegateMapper()
        {
            Dispose(false);
        }
        #endregion
    }
}
