/*
 * ParserOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    #region ParserOpsData Class
    [ObjectId("ce052fdf-0d25-4fc5-9747-ea447a3c41d8")]
    internal static class ParserOpsData
    {
        #region Private Data
#if NATIVE && NATIVE_UTILITY
        //
        // HACK: These are purposely not read-only.
        //
        internal static bool UseNativeSplitList = false;
        internal static bool UseNativeJoinList = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        // TODO: Are these good defaults for performance?
        //
        internal static int NativeMinimumTextLength = 1048576;
        internal static int NativeMaximumTextLength = 0;

        internal static int NativeMinimumListCount = 10000;
        internal static int NativeMaximumListCount = 0;

        ///////////////////////////////////////////////////////////////////////

        internal static long nativeSplitCount;
        internal static long nativeJoinCount;

        ///////////////////////////////////////////////////////////////////////

        internal static bool NoComplain = true; // COMPAT: Eagle beta.
#endif

        ///////////////////////////////////////////////////////////////////////

        internal static long managedSplitCount;
        internal static long managedJoinCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.WriteEngineInfo method.
        //
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

#if NATIVE && NATIVE_UTILITY
            if (empty || UseNativeSplitList)
            {
                localList.Add("UseNativeSplitList",
                    UseNativeSplitList.ToString());
            }

            if (empty || UseNativeJoinList)
            {
                localList.Add("UseNativeJoinList",
                    UseNativeJoinList.ToString());
            }

            if (empty || (NativeMinimumTextLength > 0))
            {
                localList.Add("NativeMinimumTextLength",
                    NativeMinimumTextLength.ToString());
            }

            if (empty || (NativeMaximumTextLength > 0))
            {
                localList.Add("NativeMaximumTextLength",
                    NativeMaximumTextLength.ToString());
            }

            if (empty || (NativeMinimumListCount > 0))
            {
                localList.Add("NativeMinimumListCount",
                    NativeMinimumListCount.ToString());
            }

            if (empty || (NativeMaximumListCount > 0))
            {
                localList.Add("NativeMaximumListCount",
                    NativeMaximumListCount.ToString());
            }

            if (empty || NoComplain)
                localList.Add("NoComplain", NoComplain.ToString());
#endif

            long localCount = Interlocked.CompareExchange(
                ref managedSplitCount, 0, 0);

            if (empty || (localCount > 0))
                localList.Add("ManagedSplitCount", localCount.ToString());

            localCount = Interlocked.CompareExchange(
                ref managedJoinCount, 0, 0);

            if (empty || (localCount > 0))
                localList.Add("ManagedJoinCount", localCount.ToString());

#if NATIVE && NATIVE_UTILITY
            localCount = Interlocked.CompareExchange(
                ref nativeSplitCount, 0, 0);

            if (empty || (localCount > 0))
                localList.Add("NativeSplitCount", localCount.ToString());

            localCount = Interlocked.CompareExchange(
                ref nativeJoinCount, 0, 0);

            if (empty || (localCount > 0))
                localList.Add("NativeJoinCount", localCount.ToString());
#endif

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("List Splitting & Joining");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Integration Support Methods
#if NATIVE && NATIVE_UTILITY
        public static void EnableNative(
            bool enable
            )
        {
            UseNativeSplitList = enable;
            UseNativeJoinList = enable;
        }
#endif
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region ParserOps<T> Class
    [ObjectId("a4c1ccc4-4dd3-4ecd-8548-3309719ec9f9")]
    internal static class ParserOps<T>
    {
        #region Native List Splitting
#if NATIVE && NATIVE_UTILITY
        public static ReturnCode NativeSplitList(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            int startIndex, /* NOT USED */
            int length, /* NOT USED */
            bool readOnly,
            ref StringList list,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                Result localError; /* REUSED */

                //
                // BUGFIX: *DEADLOCK* Prevent deadlocks here by using
                //         the TryLock pattern.
                //
                NativeUtility.TryLock(ref locked); /* TRANSACTIONAL */

                if (locked && NativeUtility.IsAvailable(interpreter))
                {
                    //
                    // NOTE: Convert a null string into a null list
                    //       without actually calling into the native
                    //       utility library.
                    //
                    if (text == null)
                    {
                        list = null;
                        return ReturnCode.Ok;
                    }

                    StringList localList;

#if LIST_CACHE
                    bool wasNull;
#endif

                    if (list != null)
                    {
#if LIST_CACHE
                        localList = StringList.MaybeReadOnly(list, readOnly);
#else
                        localList = new StringList(list);
#endif

#if LIST_CACHE
                        wasNull = false;
#endif
                    }
                    else
                    {
#if LIST_CACHE
                        localList = StringList.MaybeReadOnly(readOnly);
#else
                        localList = new StringList();
#endif

#if LIST_CACHE
                        wasNull = true;
#endif
                    }

#if LIST_CACHE
                    bool useCache = wasNull && (interpreter != null);

                    if (useCache)
                    {
                        if (interpreter.GetCachedStringList(
                                text, ref localList))
                        {
                            if (!readOnly && (localList != null))
                            {
                                if (!interpreter.RemoveCachedStringList(
                                        localList.CacheKey))
                                {
                                    localList = new StringList(localList);
                                }
                            }

                            list = localList;
                            return ReturnCode.Ok;
                        }
                    }
#endif

                    localError = null;

                    if (NativeUtility.SplitList(
                            text, ref localList,
                            ref localError) != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "NativeSplitList: {0}", localError),
                            typeof(ParserOps<T>).Name,
                            TracePriority.NativeError);

                        error = localError;
                        return ReturnCode.Error;
                    }

#if LIST_CACHE
                    if (useCache)
                    {
                        if (localList != null)
                            localList.CacheKey = text;

                        if (interpreter.AddCachedStringList(text, localList) &&
                            !readOnly && (localList != null))
                        {
                            localList = new StringList(localList);
                        }
                    }
#endif

                    list = localList;
                    return ReturnCode.Ok;
                }
                else if (!locked)
                {
                    localError = "unable to acquire native utility lock";

                    TraceOps.DebugTrace(String.Format(
                        "NativeSplitList: {0}", localError),
                        typeof(ParserOps<T>).Name,
                        TracePriority.LockWarning);

                    //
                    // WARNING: Setting this error message will
                    //          cause spurious complaints to be
                    //          seen on secondary threads.
                    //
                    error = localError;
                }
                else
                {
                    localError = "native utility not available";

#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "NativeSplitList: {0}", localError),
                        typeof(ParserOps<T>).Name,
                        TracePriority.NativeError3);
#endif

                    //
                    // HACK: This is not a real "error", per se.
                    //       It simply means the native utility
                    //       (library) is not available and the
                    //       managed fallback should be used.
                    //
                    error = localError;
                }

                return ReturnCode.Error;
            }
            finally
            {
                NativeUtility.ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Managed List Splitting
        private static ReturnCode ManagedSplitList(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            int startIndex,
            int length,
            bool readOnly,
            ref StringList list,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Interlocked.Increment(
                ref ParserOpsData.managedSplitCount);

            ReturnCode code;

            if (text == null)
            {
                error = "cannot split null string into list";
                return ReturnCode.Error;
            }

            StringList localList;

#if LIST_CACHE
            bool wasNull;
#endif

            if (list != null)
            {
#if LIST_CACHE
                localList = StringList.MaybeReadOnly(list, readOnly);
#else
                localList = new StringList(list);
#endif

#if LIST_CACHE
                wasNull = false;
#endif
            }
            else
            {
#if LIST_CACHE
                localList = StringList.MaybeReadOnly(readOnly);
#else
                localList = new StringList();
#endif

#if LIST_CACHE
                wasNull = true;
#endif
            }

            bool useTextLength = (length < 0);

#if LIST_CACHE
            bool useCache = wasNull && (interpreter != null) &&
                (startIndex == 0) && useTextLength;

            if (useCache)
            {
                if (interpreter.GetCachedStringList(
                        text, ref localList))
                {
                    if (!readOnly && (localList != null))
                    {
                        if (!interpreter.RemoveCachedStringList(
                                localList.CacheKey))
                        {
                            localList = new StringList(localList);
                        }
                    }

                    list = localList;
                    return ReturnCode.Ok;
                }
            }
#endif

            int textLength = text.Length;

            if (useTextLength)
                length = textLength;

            int capacity = 2 * length + 2;
            StringBuilder element = null;

            for (int index = startIndex; index < length; )
            {
                int elementIndex = 0;
                int elementLength = 0;
                bool braces = false;

                code = Parser.FindElement(
                    /* interpreter, */ text, index, length - index,
                    ref elementIndex, ref index, ref elementLength,
                    ref braces, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                if (elementIndex == length)
                    break;

                if (braces)
                {
                    //
                    // NOTE: If we are in braces, don't worry about
                    //       processing backslash escapes.
                    //
                    element = StringOps.NewStringBuilder(
                        element, text, elementIndex, elementLength,
                        capacity);
                }
                else
                {
                    element = StringOps.NewStringBuilder(
                        element, capacity);

                    for (char character = text[elementIndex];
                            elementLength > 0; elementLength--)
                    {
                        if (character == Characters.Backslash)
                        {
                            int read = 0;
                            char? character1 = null;
                            char? character2 = null;

                            Parser.ParseBackslash(
                                text, elementIndex, elementLength,
                                ref read, ref character1,
                                ref character2);

                            if (character1 != null)
                                element.Append(character1);

                            if (character2 != null)
                                element.Append(character2);

                            elementIndex += (read - 1);
                            elementLength -= (read - 1);
                        }
                        else
                        {
                            element.Append(character);
                        }

                        elementIndex++;

                        if (elementIndex < textLength)
                        {
                            character = text[elementIndex];
                        }
                        else if (elementLength > 1)
                        {
                            error = "hit end of string while copying list element";
                            return ReturnCode.Error;
                        }
                    }
                }

                localList.Add(element.ToString());
            }

#if LIST_CACHE
            if (useCache)
            {
                if (localList != null)
                    localList.CacheKey = text;

                if (interpreter.AddCachedStringList(text, localList) &&
                    !readOnly && (localList != null))
                {
                    localList = new StringList(localList);
                }
            }
#endif

            list = localList;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region List Splitting
#if NATIVE && NATIVE_UTILITY
        private static bool ShouldUseNativeSplitList(
            string text,
            int startIndex,
            int length
            )
        {
            if (!ParserOpsData.UseNativeSplitList)
                return false;

            if ((text == null) || (startIndex != 0) || (length >= 0))
                return false;

            int minimumLength = ParserOpsData.NativeMinimumTextLength;

            if ((minimumLength > 0) && (text.Length < minimumLength))
                return false;

            int maximumLength = ParserOpsData.NativeMaximumTextLength;

            if ((maximumLength > 0) && (text.Length > maximumLength))
                return false;

            return true;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SplitList(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            int startIndex,
            int length,
            bool readOnly,
            ref StringList list
            )
        {
            Result error = null;

            return SplitList(
                interpreter, text, startIndex, length, readOnly,
                ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SplitList(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            int startIndex,
            int length,
            bool readOnly,
            ref StringList list,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
#if NATIVE && NATIVE_UTILITY
            //
            // BUGFIX: The NativeSplitList method ignores length as it
            //         only ever splits the entire string; therefore,
            //         use the ManagedSplitList method when splitting
            //         any partial string.
            //
            if (ShouldUseNativeSplitList(text, startIndex, length))
            {
                ReturnCode code;
                StringList localList = null;
                Result localError = null;

                code = NativeSplitList(
                    interpreter, text, startIndex, length, readOnly,
                    ref localList, ref localError);

                if (code == ReturnCode.Ok)
                {
                    Interlocked.Increment(
                        ref ParserOpsData.nativeSplitCount);

                    list = localList;
                    return code;
                }

                if (!ParserOpsData.NoComplain && (localError != null))
                    DebugOps.Complain(code, localError);
            }
#endif

            return ManagedSplitList(
                interpreter, text, startIndex, length, readOnly,
                ref list, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native List Joining
#if NATIVE && NATIVE_UTILITY
        private static ReturnCode FilterList(
            IList<T> inputList,
            StringList outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string pattern,
            bool noCase,
            ref Result error
            )
        {
            if (inputList == null)
            {
                error = "invalid input list";
                return ReturnCode.Error;
            }

            if (outputList == null)
            {
                error = "invalid output list";
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Skip everything if the input list is empty.
            //
            int count = inputList.Count;

            if (count == 0)
                return ReturnCode.Ok;

            if (!ListOps.CheckStartAndStopIndex(
                    0, count - 1, ref startIndex, ref stopIndex,
                    ref error))
            {
                return ReturnCode.Error;
            }

            for (int index = startIndex; index <= stopIndex; index++)
            {
                T element = inputList[index];
                string elementString;

                if (element != null)
                {
                    if (toStringFlags != ToStringFlags.None)
                    {
                        IToString toString = element as IToString;

                        if (toString != null)
                        {
                            elementString = toString.ToString(
                                toStringFlags);
                        }
                        else
                        {
                            elementString = element.ToString();
                        }
                    }
                    else
                    {
                        elementString = element.ToString();
                    }
                }
                else
                {
                    elementString = String.Empty;
                }

                //
                // NOTE: Match the string representation and add
                //       the original element and not the string
                //       representation because this is a generic
                //       list, not a StringList.
                //
                if ((pattern == null) || StringOps.Match(
                        null, StringOps.DefaultMatchMode,
                        elementString, pattern, noCase))
                {
                    outputList.Add(elementString);
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode FilterList(
            IList<T> inputList,
            StringList outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string regExPattern,
            RegexOptions regExOptions,
            ref Result error
            )
        {
            if (inputList == null)
            {
                error = "invalid input list";
                return ReturnCode.Error;
            }

            if (outputList == null)
            {
                error = "invalid output list";
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Skip everything if the input list is empty.
            //
            int count = inputList.Count;

            if (count == 0)
                return ReturnCode.Ok;

            if (!ListOps.CheckStartAndStopIndex(
                    0, count - 1, ref startIndex, ref stopIndex,
                    ref error))
            {
                return ReturnCode.Error;
            }

            Regex regEx = null;

            try
            {
                if (regExPattern != null)
                {
                    regEx = RegExOps.Create(
                        regExPattern, regExOptions); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }

            if ((regExPattern != null) && (regEx == null))
            {
                error = "invalid regular expression";
                return ReturnCode.Error;
            }

            for (int index = startIndex; index <= stopIndex; index++)
            {
                T element = inputList[index];
                string elementString;

                if (element != null)
                {
                    if (toStringFlags != ToStringFlags.None)
                    {
                        IToString toString = element as IToString;

                        if (toString != null)
                        {
                            elementString = toString.ToString(
                                toStringFlags);
                        }
                        else
                        {
                            elementString = element.ToString();
                        }
                    }
                    else
                    {
                        elementString = element.ToString();
                    }
                }
                else
                {
                    elementString = String.Empty;
                }

                //
                // NOTE: Match the string representation and add
                //       the original element and not the string
                //       representation because this is a generic
                //       list, not a StringList.
                //
                if ((regEx == null) || regEx.IsMatch(elementString))
                    outputList.Add(elementString);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode NativeListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase,
            ref string text,
            ref Result error
            )
        {
            bool locked = false;

            try
            {
                //
                // BUGFIX: *DEADLOCK* Prevent deadlocks here by using
                //         the TryLock pattern.
                //
                Result localError; /* REUSED */

                NativeUtility.TryLock(ref locked); /* TRANSACTIONAL */

                if (locked && NativeUtility.IsAvailable(null))
                {
                    //
                    // NOTE: Convert a null list into an empty string
                    //       without actually calling into the native
                    //       utility library.
                    //
                    if (list == null)
                    {
                        text = String.Empty;
                        return ReturnCode.Ok;
                    }

                    StringList localList;

                    if ((startIndex >= 0) || (stopIndex >= 0) ||
                        (toStringFlags != ToStringFlags.None) ||
                        (pattern != null))
                    {
                        localList = new StringList(list.Count);
                        localError = null;

                        if (FilterList(
                                list, localList, startIndex, stopIndex,
                                toStringFlags, pattern, noCase,
                                ref localError) != ReturnCode.Ok)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "NativeListToString ({0}): {1}",
                                MatchMode.Glob, localError),
                                typeof(ParserOps<T>).Name,
                                TracePriority.NativeError2);

                            error = localError;
                            return ReturnCode.Error;
                        }
                    }
#if !MONO_BUILD
                    //
                    // HACK: *MONO* The Mono C# compiler cannot handle
                    //       this block of code.  It gives the following
                    //       warnings:
                    //
                    //       warning CS0184: The given expression is
                    //       never of the provided
                    //       (`Eagle._Containers.Public.StringList')
                    //       type
                    //
                    //       warning CS0162: Unreachable code detected
                    //
                    else if (list is StringList)
                    {
                        localList = list as StringList;
                    }
#endif
                    else
                    {
                        localList = new StringList(list);
                    }

                    string localText = null;

                    localError = null;

                    if (NativeUtility.JoinList(
                            localList, ref localText,
                            ref localError) == ReturnCode.Ok)
                    {
                        text = localText;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "NativeListToString ({0}): {1}",
                            MatchMode.Glob, localError),
                            typeof(ParserOps<T>).Name,
                            TracePriority.NativeError);

                        error = localError;
                        return ReturnCode.Error;
                    }
                }
                else if (!locked)
                {
                    localError = "unable to acquire native utility lock";

                    TraceOps.DebugTrace(String.Format(
                        "NativeListToString ({0}): {1}",
                        MatchMode.Glob, localError),
                        typeof(ParserOps<T>).Name,
                        TracePriority.LockWarning);

                    //
                    // WARNING: Setting this error message will
                    //          cause spurious complaints to be
                    //          seen on secondary threads.
                    //
                    error = localError;
                }
                else
                {
                    localError = "native utility not available";

#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "NativeListToString ({0}): {1}",
                        MatchMode.Glob, localError),
                        typeof(ParserOps<T>).Name,
                        TracePriority.NativeError3);
#endif

                    //
                    // HACK: This is not a real "error", per se.
                    //       It simply means the native utility
                    //       (library) is not available and the
                    //       managed fallback should be used.
                    //
                    error = localError;
                }

                return ReturnCode.Error;
            }
            finally
            {
                NativeUtility.ExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode NativeListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions,
            ref string text,
            ref Result error
            )
        {
            bool locked = false;

            try
            {
                //
                // BUGFIX: *DEADLOCK* Prevent deadlocks here by using
                //         the TryLock pattern.
                //
                Result localError; /* REUSED */

                NativeUtility.TryLock(ref locked); /* TRANSACTIONAL */

                if (locked && NativeUtility.IsAvailable(null))
                {
                    //
                    // NOTE: Convert a null list into an empty string
                    //       without actually calling into the native
                    //       utility library.
                    //
                    if (list == null)
                    {
                        text = String.Empty;
                        return ReturnCode.Ok;
                    }

                    StringList localList;

                    if ((startIndex >= 0) || (stopIndex >= 0) ||
                        (toStringFlags != ToStringFlags.None) ||
                        (regExPattern != null))
                    {
                        localList = new StringList(list.Count);
                        localError = null;

                        if (FilterList(
                                list, localList, startIndex, stopIndex,
                                toStringFlags, regExPattern, regExOptions,
                                ref localError) != ReturnCode.Ok)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "NativeListToString ({0}): {1}",
                                MatchMode.RegExp, localError),
                                typeof(ParserOps<T>).Name,
                                TracePriority.NativeError2);

                            error = localError;
                            return ReturnCode.Error;
                        }
                    }
#if !MONO_BUILD
                    //
                    // HACK: *MONO* The Mono C# compiler cannot handle
                    //       this block of code.  It gives the following
                    //       warnings:
                    //
                    //       warning CS0184: The given expression is
                    //       never of the provided
                    //       (`Eagle._Containers.Public.StringList')
                    //       type
                    //
                    //       warning CS0162: Unreachable code detected
                    //
                    else if (list is StringList)
                    {
                        localList = list as StringList;
                    }
#endif
                    else
                    {
                        localList = new StringList(list);
                    }

                    string localText = null;

                    localError = null;

                    if (NativeUtility.JoinList(
                            localList, ref localText,
                            ref localError) == ReturnCode.Ok)
                    {
                        text = localText;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "NativeListToString ({0}): {1}",
                            MatchMode.RegExp, localError),
                            typeof(ParserOps<T>).Name,
                            TracePriority.NativeError);

                        error = localError;
                        return ReturnCode.Error;
                    }
                }
                else if (!locked)
                {
                    localError = "unable to acquire native utility lock";

                    TraceOps.DebugTrace(String.Format(
                        "NativeListToString ({0}): {1}",
                        MatchMode.RegExp, localError),
                        typeof(ParserOps<T>).Name,
                        TracePriority.LockWarning);

                    //
                    // WARNING: Setting this error message will
                    //          cause spurious complaints to be
                    //          seen on secondary threads.
                    //
                    error = localError;
                }
                else
                {
                    localError = "native utility not available";

#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "NativeListToString ({0}): {1}",
                        MatchMode.RegExp, localError),
                        typeof(ParserOps<T>).Name,
                        TracePriority.NativeError3);
#endif

                    //
                    // HACK: This is not a real "error", per se.
                    //       It simply means the native utility
                    //       (library) is not available and the
                    //       managed fallback should be used.
                    //
                    error = localError;
                }

                return ReturnCode.Error;
            }
            finally
            {
                NativeUtility.ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Managed List Joining
        private static string ManagedListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            Interlocked.Increment(
                ref ParserOpsData.managedJoinCount);

            StringBuilder result = StringOps.NewStringBuilder();

            if (list != null)
            {
                int count = list.Count;

                if (ListOps.CheckStartAndStopIndex(
                        0, count - 1, ref startIndex, ref stopIndex))
                {
                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        T element = list[index];
                        string elementString;

                        if (element != null)
                        {
                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = element as IToString;

                                if (toString != null)
                                {
                                    elementString = toString.ToString(
                                        toStringFlags);
                                }
                                else
                                {
                                    elementString = element.ToString();
                                }
                            }
                            else
                            {
                                elementString = element.ToString();
                            }
                        }
                        else
                        {
                            elementString = String.Empty;
                        }

                        if ((pattern == null) || StringOps.Match(
                                null, StringOps.DefaultMatchMode,
                                elementString, pattern, noCase))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None :
                                ListElementFlags.DontQuoteHash;

                            int length = elementString.Length;

                            Parser.ScanElement(
                                /* null, */ elementString, 0,
                                length, ref flags);

                            if ((result.Length > 0) &&
                                !String.IsNullOrEmpty(separator))
                            {
                                result.Append(separator);
                            }

                            Parser.ConvertElement(
                                /* null, */ elementString, 0,
                                length, flags, ref result);
                        }
                    }
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        private static string ManagedListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
            Interlocked.Increment(
                ref ParserOpsData.managedJoinCount);

            StringBuilder result = StringOps.NewStringBuilder();

            if (list != null)
            {
                int count = list.Count;

                if (ListOps.CheckStartAndStopIndex(
                        0, count - 1, ref startIndex, ref stopIndex))
                {
                    Regex regEx = null;

                    if (regExPattern != null)
                    {
                        regEx = RegExOps.Create(
                            regExPattern, regExOptions); /* throw */
                    }

                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        T element = list[index];
                        string elementString;

                        if (element != null)
                        {
                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = element as IToString;

                                if (toString != null)
                                {
                                    elementString = toString.ToString(
                                        toStringFlags);
                                }
                                else
                                {
                                    elementString = element.ToString();
                                }
                            }
                            else
                            {
                                elementString = element.ToString();
                            }
                        }
                        else
                        {
                            elementString = String.Empty;
                        }

                        if ((regEx == null) || regEx.IsMatch(elementString))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None :
                                ListElementFlags.DontQuoteHash;

                            int length = elementString.Length;

                            Parser.ScanElement(
                                /* null, */ elementString, 0,
                                length, ref flags);

                            if ((result.Length > 0) &&
                                !String.IsNullOrEmpty(separator))
                            {
                                result.Append(separator);
                            }

                            Parser.ConvertElement(
                                /* null, */ elementString, 0,
                                length, flags, ref result);
                        }
                    }
                }
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region List Joining
#if NATIVE && NATIVE_UTILITY
        private static bool ShouldUseNativeJoinList(
            IList<T> list,
            string separator
            )
        {
            if (!ParserOpsData.UseNativeJoinList)
                return false;

            if ((list == null) || !Parser.IsListSeparator(separator))
                return false;

            int minimumCount = ParserOpsData.NativeMinimumListCount;

            if ((minimumCount > 0) && (list.Count < minimumCount))
                return false;

            int maximumCount = ParserOpsData.NativeMaximumListCount;

            if ((maximumCount > 0) && (list.Count > maximumCount))
                return false;

            return true;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
#if NATIVE && NATIVE_UTILITY
            if (ShouldUseNativeJoinList(list, separator))
            {
                ReturnCode code;
                string localText = null;
                Result localError = null;

                code = NativeListToString(
                    list, startIndex, stopIndex, toStringFlags,
                    separator, pattern, noCase, ref localText,
                    ref localError);

                if (code == ReturnCode.Ok)
                {
                    Interlocked.Increment(
                        ref ParserOpsData.nativeJoinCount);

                    return localText;
                }

                if (!ParserOpsData.NoComplain && (localError != null))
                    DebugOps.Complain(code, localError);
            }
#endif

            return ManagedListToString(
                list, startIndex, stopIndex, toStringFlags, separator,
                pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
#if NATIVE && NATIVE_UTILITY
            if (ShouldUseNativeJoinList(list, separator))
            {
                ReturnCode code;
                string localText = null;
                Result localError = null;

                code = NativeListToString(
                    list, startIndex, stopIndex, toStringFlags,
                    separator, regExPattern, regExOptions,
                    ref localText, ref localError);

                if (code == ReturnCode.Ok)
                {
                    Interlocked.Increment(
                        ref ParserOpsData.nativeJoinCount);

                    return localText;
                }

                if (!ParserOpsData.NoComplain && (localError != null))
                    DebugOps.Complain(code, localError);
            }
#endif

            return ManagedListToString(
                list, startIndex, stopIndex, toStringFlags, separator,
                regExPattern, regExOptions);
        }
        #endregion
    }
    #endregion
}
