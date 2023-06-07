/*
 * ArgumentList.cs --
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
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("65ed4894-90ef-49bf-8744-2a7c3406af55")]
    public sealed class ArgumentList :
            List<Argument>, IList<Argument>, ICollection<Argument>,
            IGetValue, ICloneable
    {
        #region Private Static Data
#if CACHE_ARGUMENTLIST_TOSTRING && CACHE_STATISTICS
        private static long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf];
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        private static readonly string DefaultSeparator =
            Characters.Space.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if CACHE_ARGUMENTLIST_TOSTRING
        private string @string; /* CACHE */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ArgumentList()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ArgumentList(
            params object[] objects
            )
            : base()
        {
            int length = objects.Length;

            if (length > 0)
            {
                for (int index = 0; index < length; index++)
                {
                    object @object = objects[index];

                    if (@object is ArgumentList)
                    {
                        //
                        // NOTE: They supplied an argument list as a parameter.
                        //       Add the entire list to this list.
                        //
                        this.AddRange((ArgumentList)@object);
                    }
                    else if (@object is StringList)
                    {
                        //
                        // NOTE: They supplied a string list as a parameter.
                        //       Add the entire list to this list.
                        //
                        this.AddRange((StringList)@object);
                    }
                    else if (@object is IEnumerable<string>)
                    {
                        //
                        // NOTE: They supplied a string array as a parameter.
                        //       Add all the elements to this list.
                        //
                        this.AddRange((IEnumerable<string>)@object);
                    }
                    else if ((@object is ICollection) || (@object is IList))
                    {
                        //
                        // NOTE: They supplied an collection or list [of some
                        //       kind] as a parameter.  Add all supported
                        //       elements to this list.
                        //
                        this.AddRange(
                            (IEnumerable)@object, true, true, false, false);
                    }
                    else
                    {
                        Argument argument;

                        if (@object is Argument)
                        {
                            argument = (Argument)@object;
                        }
                        else if (@object is string)
                        {
                            argument = Argument.InternalCreate(
                                (string)@object);
                        }
                        else if (@object != null)
                        {
                            argument = Argument.InternalCreate(
                                @object.ToString());
                        }
                        else
                        {
                            argument = Argument.InternalCreate(
                                Argument.NoValue);
                        }

                        this.Add(argument);
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
        internal ArgumentList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        internal ArgumentList(
            IEnumerable<Argument> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        internal ArgumentList(
            IEnumerable<string> collection
            )
            : this()
        {
            AddRange(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        internal ArgumentList(
            IEnumerable<string> collection,
            ArgumentFlags flags
            )
            : this()
        {
            foreach (string item in collection)
            {
                Argument argument;

                if (FlagOps.HasFlags(flags, ArgumentFlags.NameOnly, true))
                {
                    argument = Argument.InternalCreate(flags, item);
                }
                else
                {
                    argument = Argument.InternalCreate(
                        flags, Argument.NoName, item);
                }

                this.Add(argument);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal ArgumentList( /* NOTE: For [apply] and [proc] use only. */
            StringPairList list,
            ArgumentFlags flags
            )
            : this()
        {
            if (list != null)
            {
                string variadicName = GetVariadicName();
                int count = list.Count;

                for (int index = 0; index < count; index++)
                {
                    IPair<string> element = list[index];

                    //
                    // HACK: Skip over any null entries, thus ignoring
                    //       them.
                    //
                    if (element == null)
                        continue;

                    //
                    // NOTE: Does this argument list accept a variable
                    //       numbers of arguments (COMPAT: Tcl)?  If so,
                    //       add a flag to the final argument to mark it
                    //       as an "argument list".
                    //
                    ArgumentFlags nameFlags = ArgumentFlags.None;

                    if ((variadicName != null) &&
                        SharedStringOps.SystemEquals(
                            element.X, variadicName) &&
                        (index == (count - 1)))
                    {
                        nameFlags |= ArgumentFlags.List;
                    }

                    ArgumentFlags valueFlags = (element.Y != null) ?
                        ArgumentFlags.HasDefault : ArgumentFlags.None;

                    Argument argument;

                    if (FlagOps.HasFlags(flags, ArgumentFlags.NameOnly, true))
                    {
                        argument = Argument.InternalCreate(
                            flags | nameFlags | valueFlags, element.X,
                            Argument.NoValue, element.Y);
                    }
                    else
                    {
                        argument = Argument.InternalCreate(
                            flags | nameFlags | valueFlags, Argument.NoName,
                            element.X, element.Y);
                    }

                    this.Add(argument);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Argument Handling Methods
        public static ArgumentList NullIfEmpty(
            ArgumentList list,
            int firstIndex
            )
        {
            if (list == null)
                return null;

            if (firstIndex == Index.Invalid)
                firstIndex = 0;

            if ((firstIndex < 0) || (firstIndex >= list.Count))
                return null;

            //
            // NOTE: If there are elements beyond the first index or the
            //       element at the first index is not empty, then return
            //       the range starting from the first index; otherwise,
            //       return null.
            //
            if (((firstIndex + 1) < list.Count) ||
                !String.IsNullOrEmpty(list[firstIndex]))
            {
                return GetRange(list, firstIndex);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Range Methods
        public static ArgumentList GetRange(
            ArgumentList list,
            int firstIndex
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ArgumentList GetRange(
            IList list,
            int firstIndex,
            bool nullIfEmpty
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid,
                nullIfEmpty);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ArgumentList GetRange(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            return GetRange(list, firstIndex, lastIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ArgumentList GetRange(
            IList list,
            int firstIndex,
            int lastIndex,
            bool nullIfEmpty
            )
        {
            if (list == null)
                return null;

            ArgumentList range = null;

            if (firstIndex == Index.Invalid)
                firstIndex = 0;

            if (lastIndex == Index.Invalid)
                lastIndex = list.Count - 1;

            if ((!nullIfEmpty ||
                ((list.Count > 0) && ((lastIndex - firstIndex) > 0))))
            {
                range = new ArgumentList();

                for (int index = firstIndex; index <= lastIndex; index++)
                {
                    if (list[index] != null)
                        range.Add(list[index].ToString());
                    else
                        range.Add(null);
                }
            }

            return range;
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetRangeAsStringList(
            ArgumentList list,
            int firstIndex
            )
        {
            return GetRangeAsStringList(list,
                firstIndex, (list != null) ? (list.Count - 1) : Index.Invalid,
                false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetRangeAsStringList(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            return GetRangeAsStringList(list, firstIndex, lastIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetRangeAsStringList(
            IList list,
            int firstIndex,
            int lastIndex,
            bool dequote
            )
        {
            StringList range = null;

            if (list != null)
            {
                range = new StringList();

                if (firstIndex == Index.Invalid)
                    firstIndex = 0;

                if (lastIndex == Index.Invalid)
                    lastIndex = list.Count - 1;

                for (int index = firstIndex; index <= lastIndex; index++)
                {
                    object item = list[index];

                    if (item == null)
                    {
                        range.Add((string)null);
                        continue;
                    }

                    string @string = item.ToString();

                    if (dequote)
                    {
                        @string = FormatOps.StripOuter(
                            @string, Characters.QuotationMark);
                    }

                    range.Add(@string);
                }
            }

            return range;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void AddRange(
            IEnumerable collection,
            bool forceCopy,
            bool supportedOnly,
            bool toString,
            bool allowNull
            )
        {
#if CACHE_ARGUMENTLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (object item in collection)
            {
                Argument argument = Argument.FromObject(
                    item, forceCopy, supportedOnly, toString);

                if (!allowNull && (argument == null))
                    continue;

                this.Add(argument);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddRange(
            IEnumerable<string> collection
            )
        {
#if CACHE_ARGUMENTLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (string item in collection)
                this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void InsertRange(
            int index,
            IEnumerable<string> collection
            )
        {
#if CACHE_ARGUMENTLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            this.InsertRange(index, new ArgumentList(collection));
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsVariadic(
            bool setFlags /* NOT USED */
            )
        {
            //
            // NOTE: Grab the count as we need to use it several times in
            //       this method.
            //
            int count = this.Count;

            //
            // NOTE: Does this argument list accept a variable numbers of
            //       arguments (COMPAT: Tcl)?  For native Tcl (and Eagle),
            //       this is determined by checking if the last argument
            //       is named "args".
            //
            if (count == 0)
                return false;

            Argument argument = this[count - 1];

            if (argument == null)
                return false;

            string variadicName = GetVariadicName();

            return (variadicName != null) &&
                SharedStringOps.SystemEquals(argument.Name, variadicName);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetOptionalCount()
        {
            //
            // NOTE: Return the total number of arguments in the list
            //       that are optional.  In order for an argument to
            //       be considered optional, it must meet the following
            //       criteria:
            //
            //       1. It must have a default value that is not null.
            //
            //       2. No non-optional arguments may occur after it
            //          in the argument list (COMPAT: Tcl).
            //
            int result = 0;

            //
            // NOTE: Grab the count as we need to use it several times in
            //       this method.
            //
            int count = this.Count;

            //
            // NOTE: Count all the arguments starting from the end of the
            //       list going backward that have a default value.
            //
            int index = IsVariadic(false) ? count - 2 : count - 1;

            for (; index >= 0; index--)
            {
                Argument argument = this[index];

                if ((argument != null) && FlagOps.HasFlags(
                        argument.Flags, ArgumentFlags.HasDefault, true))
                {
                    result++;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString()
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (Argument element in this)
                result.Append(element);

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString(
            ToStringFlags toStringFlags,
            string separator
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (Argument element in this)
            {
                if (element != null)
                {
                    if ((separator != null) && (result.Length > 0))
                        result.Append(separator);

                    result.Append(element.ToString(toStringFlags));
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            ToStringFlags toStringFlags
            )
        {
            return ToString(toStringFlags, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            ToStringFlags toStringFlags,
            string pattern,
            bool noCase
            )
        {
#if CACHE_ARGUMENTLIST_TOSTRING
            bool canUseCachedString = CanUseCachedString(
                toStringFlags, DefaultSeparator, pattern, noCase);

            if (canUseCachedString && (@string != null))
            {
#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Hit]);
#endif

                return @string;
            }

#if CACHE_STATISTICS
            Interlocked.Increment(
                ref cacheCounts[(int)CacheCountType.Miss]);
#endif
#endif

            string result = ParserOps<Argument>.ListToString(
                this, Index.Invalid, Index.Invalid, toStringFlags,
                DefaultSeparator, pattern, noCase);

#if CACHE_ARGUMENTLIST_TOSTRING
            if (canUseCachedString)
                @string = result;
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
#if CACHE_ARGUMENTLIST_TOSTRING
            bool canUseCachedString = CanUseCachedString(
                ToStringFlags.None, separator, pattern, noCase);

            if (canUseCachedString && (@string != null))
            {
#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Hit]);
#endif

                return @string;
            }

#if CACHE_STATISTICS
            Interlocked.Increment(
                ref cacheCounts[(int)CacheCountType.Miss]);
#endif
#endif

            string result = ParserOps<Argument>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, pattern, noCase);

#if CACHE_ARGUMENTLIST_TOSTRING
            if (canUseCachedString)
                @string = result;
#endif

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        #region Dead Code
#if DEAD_CODE
        internal void ResetForIHaveStringBuilder(
            Interpreter interpreter
            )
        {
            foreach (Argument element in this)
            {
                if (element == null)
                    continue;

                IHaveStringBuilder haveStringBuilder =
                    element.EngineData as IHaveStringBuilder;

                if (haveStringBuilder == null)
                    continue;

                haveStringBuilder.DoneWithReadOnly();
            }
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private string GetVariadicName()
        {
            return TclVars.Core.Arguments;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue Members
        //
        // NOTE: This must call ToString to provide a "flattened" value
        //       because this is a mutable class.
        //
        public object Value
        {
            get { return ToString(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            get
            {
                string stringValue = ToString();

                return (stringValue != null) ?
                    stringValue.Length : _Constants.Length.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            ArgumentList list = new ArgumentList(this.Capacity);

            foreach (Argument element in this)
            {
                list.Add((element != null) ?
                    element.Clone() as Argument : null);
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [info level] Helper Methods
        //
        // WARNING: For use by the Interpreter.GetInfoLevelArguments
        //          method only.
        //
        internal ArgumentList CloneWithNewFirstValue(
            object value
            )
        {
            ArgumentList list = Clone() as ArgumentList;

            if (list == null)
                return null;

            if (list.Count == 0)
                return list;

            Argument element = list[0];

            if (element != null)
                element.SetValue(value);

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(DefaultSeparator, null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached String Helper Methods
#if CACHE_ARGUMENTLIST_TOSTRING
        internal void InvalidateCachedString(
            bool children
            )
        {
            @string = null;

            if (children)
            {
                foreach (Argument argument in this)
                {
                    if (argument == null)
                        continue;

                    argument.InvalidateCachedString(children);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CanUseCachedString(
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            if (toStringFlags != ToStringFlags.None)
                return false;

            if (!Parser.IsListSeparator(separator))
                return false;

            if (pattern != null)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        public static bool HaveCacheCounts()
        {
            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string CacheCountsToString(bool empty)
        {
            return FormatOps.CacheCounts(cacheCounts, empty);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Explicit ICollection<Argument> Overrides
        void ICollection<Argument>.Add(
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        void ICollection<Argument>.Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        bool ICollection<Argument>.Remove(
            Argument item
            )
        {
            InvalidateCachedString(false);

            return base.Remove(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<Argument> Overrides
        public new void Add(
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool Remove(
            Argument item
            )
        {
            InvalidateCachedString(false);

            return base.Remove(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IList<Argument> Overrides
        void IList<Argument>.Insert(
            int index,
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        void IList<Argument>.RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        Argument IList<Argument>.this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IList<Argument> Overrides
        public new void Insert(
            int index,
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new Argument this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region List<Argument> Overrides
        public new void AddRange(
            IEnumerable<Argument> collection
            )
        {
            InvalidateCachedString(false);

            base.AddRange(collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void InsertRange(
            int index,
            IEnumerable<Argument> collection
            )
        {
            InvalidateCachedString(false);

            base.InsertRange(index, collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new int RemoveAll(
            Predicate<Argument> match
            )
        {
            InvalidateCachedString(false);

            return base.RemoveAll(match); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void RemoveRange(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.RemoveRange(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Reverse()
        {
            InvalidateCachedString(false);

            base.Reverse(); /* O(N) */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Reverse(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.Reverse(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort()
        {
            InvalidateCachedString(false);

            base.Sort();
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort(
            Comparison<Argument> comparison
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparison); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort(
            IComparer<Argument> comparer
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparer); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Sort(
            int index,
            int count,
            IComparer<Argument> comparer)
        {
            InvalidateCachedString(false);

            base.Sort(index, count, comparer); /* throw */
        }
        #endregion
#endif
        #endregion
    }
}
