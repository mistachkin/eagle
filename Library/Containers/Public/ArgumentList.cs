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
    /// <summary>
    /// This class represents an ordered, mutable collection of
    /// <see cref="Argument" /> objects.  It is the primary container used to
    /// hold the arguments passed to commands, procedures, and other callable
    /// entities within the Eagle interpreter.  It derives from the generic
    /// <see cref="List{T}" /> of <see cref="Argument" /> and adds support for
    /// constructing argument lists from a variety of source types, producing
    /// flattened string representations (optionally cached), querying variadic
    /// and optional argument semantics (COMPAT: Tcl), and cloning.  The list
    /// element accessors are overridden so that any mutation invalidates the
    /// cached string form.
    /// </summary>
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
        /// <summary>
        /// The per-list cache hit and miss counts, indexed by
        /// <see cref="CacheCountType" />, used to gather statistics about the
        /// cached string representation.
        /// </summary>
        private static long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf];
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The default separator string used between elements when producing
        /// the string representation of this argument list (a single space).
        /// </summary>
        private static readonly string DefaultSeparator = Characters.SpaceString;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if CACHE_ARGUMENTLIST_TOSTRING
        /// <summary>
        /// The cached, flattened string representation of this argument list,
        /// or null when no cached value is currently available.
        /// </summary>
        private string @string; /* CACHE */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty argument list with the default initial capacity.
        /// </summary>
        private ArgumentList()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an argument list from the specified objects.  Each object
        /// is added according to its type: an <see cref="ArgumentList" />,
        /// <see cref="StringList" />, string sequence, collection, or list has
        /// its elements added; an <see cref="Argument" /> is added directly;
        /// and any other value is converted to an argument via its string
        /// representation (a null object yielding an argument with no value).
        /// </summary>
        /// <param name="objects">
        /// The objects used to populate the new argument list.
        /// </param>
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
                        // NOTE: They supplied a collection or list [of some
                        //       kind] as a parameter.  Add all (supported?)
                        //       elements to this list.
                        //
                        // HACK: The check above cannot be for IEnumerable,
                        //       because System.String "passes" that check,
                        //       and we do not want that here.
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

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This is primarily for use by the [apply] and [proc]
        //          script command implementations and may require some
        //          breaking changes in the future.
        //
        /// <summary>
        /// Constructs an argument list from the specified list of name/value
        /// pairs, where each pair becomes an argument.  Null entries are
        /// skipped.  When the final pair name matches the variadic argument
        /// name (COMPAT: Tcl), that argument is marked as an argument list, and
        /// any pair with a non-null value is marked as having a default value.
        /// </summary>
        /// <param name="list">
        /// The list of name/value pairs used to populate the new argument list.
        /// This parameter may be null, in which case the list is left empty.
        /// </param>
        /// <param name="flags">
        /// The argument flags applied to each created argument; when these
        /// flags include <see cref="ArgumentFlags.NameOnly" />, each pair name
        /// is used as the argument name, otherwise it is used as the argument
        /// value.
        /// </param>
        public ArgumentList(
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

        #region Internal Constructors
        /// <summary>
        /// Constructs an empty argument list with the specified initial
        /// capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements the new argument list can initially store
        /// without resizing.
        /// </param>
        internal ArgumentList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument list containing the elements copied from the
        /// specified collection of arguments.
        /// </summary>
        /// <param name="collection">
        /// The collection of arguments whose elements are copied into the new
        /// argument list.
        /// </param>
        internal ArgumentList(
            IEnumerable<Argument> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument list by converting each string in the
        /// specified collection into an argument.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings used to populate the new argument list.
        /// </param>
        internal ArgumentList(
            IEnumerable<string> collection
            )
            : this()
        {
            AddRange(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument list by converting each string in the
        /// specified collection into an argument, applying the specified flags.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings used to populate the new argument list.
        /// </param>
        /// <param name="flags">
        /// The argument flags applied to each created argument; when these
        /// flags include <see cref="ArgumentFlags.NameOnly" />, each string is
        /// used as the argument name, otherwise it is used as the argument
        /// value.
        /// </param>
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Argument Handling Methods
        /// <summary>
        /// Returns the range of the specified argument list starting at the
        /// specified index, or null when the list is null, the index is out of
        /// range, or the range would consist of a single empty element.
        /// </summary>
        /// <param name="list">
        /// The argument list to examine.  This parameter may be null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <returns>
        /// The range of the list starting at the specified index, or null when
        /// the result would be empty.
        /// </returns>
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
        /// <summary>
        /// Returns a new argument list containing the elements of the specified
        /// list from the specified index through the end of the list.
        /// </summary>
        /// <param name="list">
        /// The argument list to copy elements from.  This parameter may be
        /// null, in which case null is returned.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <returns>
        /// A new argument list containing the requested range, or null when the
        /// specified list is null.
        /// </returns>
        public static ArgumentList GetRange(
            ArgumentList list,
            int firstIndex
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a new argument list containing the elements of the specified
        /// list from the specified index through the end of the list, with the
        /// option to return null when the range would be empty.
        /// </summary>
        /// <param name="list">
        /// The list to copy elements from.  This parameter may be null, in
        /// which case null is returned.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <param name="nullIfEmpty">
        /// When true, null is returned instead of an empty argument list when
        /// the requested range contains no elements.
        /// </param>
        /// <returns>
        /// A new argument list containing the requested range, or null when the
        /// specified list is null or the range is empty and null was requested.
        /// </returns>
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

        /// <summary>
        /// Returns a new argument list containing the elements of the specified
        /// list from the first index through the last index, inclusive.
        /// </summary>
        /// <param name="list">
        /// The list to copy elements from.  This parameter may be null, in
        /// which case null is returned.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as the final element.
        /// </param>
        /// <returns>
        /// A new argument list containing the requested range, or null when the
        /// specified list is null.
        /// </returns>
        public static ArgumentList GetRange(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            return GetRange(list, firstIndex, lastIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a new argument list containing the elements of the specified
        /// list from the first index through the last index, inclusive, with
        /// the option to return null when the range would be empty.  Each
        /// source element is converted to its string representation, and null
        /// elements are preserved as null.
        /// </summary>
        /// <param name="list">
        /// The list to copy elements from.  This parameter may be null, in
        /// which case null is returned.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as the final element.
        /// </param>
        /// <param name="nullIfEmpty">
        /// When true, null is returned instead of an empty argument list when
        /// the requested range contains no elements.
        /// </param>
        /// <returns>
        /// A new argument list containing the requested range, or null when the
        /// specified list is null or the range is empty and null was requested.
        /// </returns>
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

        /// <summary>
        /// Returns a new string list containing the elements of the specified
        /// argument list from the specified index through the end of the list.
        /// </summary>
        /// <param name="list">
        /// The argument list to copy elements from.  This parameter may be
        /// null, in which case null is returned.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <returns>
        /// A new string list containing the requested range, or null when the
        /// specified list is null.
        /// </returns>
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

        /// <summary>
        /// Returns a new string list containing the elements of the specified
        /// list from the first index through the last index, inclusive.
        /// </summary>
        /// <param name="list">
        /// The list to copy elements from.  This parameter may be null, in
        /// which case null is returned.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as the final element.
        /// </param>
        /// <returns>
        /// A new string list containing the requested range, or null when the
        /// specified list is null.
        /// </returns>
        public static StringList GetRangeAsStringList(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            return GetRangeAsStringList(list, firstIndex, lastIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a new string list containing the elements of the specified
        /// list from the first index through the last index, inclusive, with
        /// the option to strip surrounding quotation marks.  Each source
        /// element is converted to its string representation, and null elements
        /// are preserved as null.
        /// </summary>
        /// <param name="list">
        /// The list to copy elements from.  This parameter may be null, in
        /// which case null is returned.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as zero.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last element to include; a value of
        /// <see cref="Index.Invalid" /> is treated as the final element.
        /// </param>
        /// <param name="dequote">
        /// When true, any surrounding quotation marks are stripped from each
        /// element string.
        /// </param>
        /// <returns>
        /// A new string list containing the requested range, or null when the
        /// specified list is null.
        /// </returns>
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
        /// <summary>
        /// Adds the elements of the specified collection to the end of this
        /// argument list, converting each element into an argument according to
        /// the specified options.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are added to this argument list.
        /// </param>
        /// <param name="forceCopy">
        /// When true, each element that is already an argument is copied rather
        /// than referenced directly.
        /// </param>
        /// <param name="supportedOnly">
        /// When true, only elements of a supported type are converted into
        /// arguments.
        /// </param>
        /// <param name="toString">
        /// When true, unsupported elements are converted into arguments using
        /// their string representation.
        /// </param>
        /// <param name="allowNull">
        /// When true, a null argument resulting from a conversion is added to
        /// the list; otherwise, it is skipped.
        /// </param>
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

        /// <summary>
        /// Adds the strings in the specified collection to the end of this
        /// argument list, converting each string into an argument.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings added to this argument list.
        /// </param>
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

        /// <summary>
        /// Inserts the strings in the specified collection into this argument
        /// list at the specified index, converting each string into an
        /// argument.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the new arguments are inserted.
        /// </param>
        /// <param name="collection">
        /// The collection of strings inserted into this argument list.
        /// </param>
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

        /// <summary>
        /// Determines whether this argument list accepts a variable number of
        /// arguments (COMPAT: Tcl).  An argument list is considered variadic
        /// when its final argument is named with the variadic argument name.
        /// </summary>
        /// <param name="setFlags">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// True if this argument list is variadic; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Returns the number of trailing arguments in this list that are
        /// optional.  An argument is considered optional when it has a non-null
        /// default value and no non-optional argument occurs after it in the
        /// list (COMPAT: Tcl).  A trailing variadic argument is not counted.
        /// </summary>
        /// <returns>
        /// The number of optional arguments at the end of this argument list.
        /// </returns>
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

        /// <summary>
        /// Returns the concatenation of the string representations of all the
        /// arguments in this list, with no separator between them.
        /// </summary>
        /// <returns>
        /// The raw, concatenated string representation of this argument list.
        /// </returns>
        public string ToRawString()
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (Argument element in this)
                result.Append(element);

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the concatenation of the string representations of all the
        /// non-null arguments in this list, formatted using the specified flags
        /// and joined with the specified separator.
        /// </summary>
        /// <param name="toStringFlags">
        /// The flags that control how each argument is converted to a string.
        /// </param>
        /// <param name="separator">
        /// The string placed between adjacent arguments; when null, no
        /// separator is inserted.
        /// </param>
        /// <returns>
        /// The raw, separator-joined string representation of this argument
        /// list.
        /// </returns>
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

        /// <summary>
        /// Returns the string representation of this argument list, with each
        /// argument formatted using the specified flags and the elements joined
        /// using the default separator.
        /// </summary>
        /// <param name="toStringFlags">
        /// The flags that control how each argument is converted to a string.
        /// </param>
        /// <returns>
        /// The string representation of this argument list.
        /// </returns>
        public string ToString(
            ToStringFlags toStringFlags
            )
        {
            return ToString(toStringFlags, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the string representation of this argument list, with each
        /// argument formatted using the specified flags, the elements joined
        /// using the default separator, and the elements optionally filtered by
        /// a match pattern.  When caching is enabled and applicable, a cached
        /// result may be returned or stored.
        /// </summary>
        /// <param name="toStringFlags">
        /// The flags that control how each argument is converted to a string.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter which arguments are included;
        /// when null, all arguments are included.
        /// </param>
        /// <param name="noCase">
        /// When true, pattern matching is performed without regard to case.
        /// </param>
        /// <returns>
        /// The string representation of this argument list.
        /// </returns>
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

        /// <summary>
        /// Returns the string representation of this argument list, with the
        /// elements joined using the specified separator and optionally
        /// filtered by a match pattern.  When caching is enabled and
        /// applicable, a cached result may be returned or stored.
        /// </summary>
        /// <param name="separator">
        /// The string placed between adjacent arguments.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter which arguments are included;
        /// when null, all arguments are included.
        /// </param>
        /// <param name="noCase">
        /// When true, pattern matching is performed without regard to case.
        /// </param>
        /// <returns>
        /// The string representation of this argument list.
        /// </returns>
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
        /// <summary>
        /// Releases the read-only string builder, if any, associated with the
        /// engine data of each argument in this list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with this argument list.
        /// </param>
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
        /// <summary>
        /// Returns the name used to identify the final, variadic argument in an
        /// argument list (COMPAT: Tcl).
        /// </summary>
        /// <returns>
        /// The variadic argument name.
        /// </returns>
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
        /// <summary>
        /// Gets the value of this argument list, which is its flattened string
        /// representation.
        /// </summary>
        public object Value
        {
            get { return ToString(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length, in characters, of the flattened string
        /// representation of this argument list, or
        /// <see cref="_Constants.Length.Invalid" /> when that representation is
        /// null.
        /// </summary>
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

        /// <summary>
        /// Gets the flattened string representation of this argument list.
        /// </summary>
        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// Creates a new argument list that is a deep copy of this one; each
        /// non-null argument is itself cloned.
        /// </summary>
        /// <returns>
        /// A new argument list that is a copy of this instance.
        /// </returns>
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
        /// <summary>
        /// Creates a deep copy of this argument list and replaces the value of
        /// its first argument with the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to assign to the first argument of the cloned list.
        /// </param>
        /// <returns>
        /// The cloned argument list with its first value replaced, the cloned
        /// list unchanged when it is empty, or null when cloning fails.
        /// </returns>
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
        /// <summary>
        /// Returns the string representation of this argument list, with the
        /// elements joined using the default separator.
        /// </summary>
        /// <returns>
        /// The string representation of this argument list.
        /// </returns>
        public override string ToString()
        {
            return ToString(DefaultSeparator, null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached String Helper Methods
#if CACHE_ARGUMENTLIST_TOSTRING
        /// <summary>
        /// Discards the cached string representation of this argument list,
        /// optionally also discarding the cached string representation of each
        /// contained argument.
        /// </summary>
        /// <param name="children">
        /// When true, the cached string representation of each contained
        /// argument is also invalidated.
        /// </param>
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

        /// <summary>
        /// Determines whether the cached string representation may be used for
        /// the specified formatting parameters.  Caching is only applicable
        /// when no special formatting flags are requested, the separator is a
        /// list separator, and no filter pattern is supplied.
        /// </summary>
        /// <param name="toStringFlags">
        /// The flags that control how each argument is converted to a string.
        /// </param>
        /// <param name="separator">
        /// The string placed between adjacent arguments.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter which arguments are included.
        /// </param>
        /// <param name="noCase">
        /// When true, pattern matching is performed without regard to case.
        /// </param>
        /// <returns>
        /// True if the cached string representation may be used; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// Determines whether any cache hit or miss counts have been recorded
        /// for the cached string representation.
        /// </summary>
        /// <returns>
        /// True if any cache counts have been recorded; otherwise, false.
        /// </returns>
        public static bool HaveCacheCounts()
        {
            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a string describing the recorded cache hit and miss counts
        /// for the cached string representation.
        /// </summary>
        /// <param name="empty">
        /// When true, counts with a value of zero are included in the result.
        /// </param>
        /// <returns>
        /// A string describing the recorded cache counts.
        /// </returns>
        public static string CacheCountsToString(bool empty)
        {
            return FormatOps.CacheCounts(cacheCounts, empty);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Explicit ICollection<Argument> Overrides
        /// <summary>
        /// Adds the specified argument to the end of this list, invalidating
        /// the cached string representation.
        /// </summary>
        /// <param name="item">
        /// The argument to add.
        /// </param>
        void ICollection<Argument>.Add(
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes all arguments from this list, invalidating the cached string
        /// representation.
        /// </summary>
        void ICollection<Argument>.Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the first occurrence of the specified argument from this
        /// list, invalidating the cached string representation.
        /// </summary>
        /// <param name="item">
        /// The argument to remove.
        /// </param>
        /// <returns>
        /// True if an argument was removed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Adds the specified argument to the end of this list, invalidating
        /// the cached string representation.
        /// </summary>
        /// <param name="item">
        /// The argument to add.
        /// </param>
        public new void Add(
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes all arguments from this list, invalidating the cached string
        /// representation.
        /// </summary>
        public new void Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the first occurrence of the specified argument from this
        /// list, invalidating the cached string representation.
        /// </summary>
        /// <param name="item">
        /// The argument to remove.
        /// </param>
        /// <returns>
        /// True if an argument was removed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Inserts the specified argument into this list at the specified index,
        /// invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the argument is inserted.
        /// </param>
        /// <param name="item">
        /// The argument to insert.
        /// </param>
        void IList<Argument>.Insert(
            int index,
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the argument at the specified index from this list,
        /// invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the argument to remove.
        /// </param>
        void IList<Argument>.RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the argument at the specified index; setting an element
        /// invalidates the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the argument to get or set.
        /// </param>
        /// <returns>
        /// The argument at the specified index.
        /// </returns>
        Argument IList<Argument>.this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IList<Argument> Overrides
        /// <summary>
        /// Inserts the specified argument into this list at the specified index,
        /// invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the argument is inserted.
        /// </param>
        /// <param name="item">
        /// The argument to insert.
        /// </param>
        public new void Insert(
            int index,
            Argument item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the argument at the specified index from this list,
        /// invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the argument to remove.
        /// </param>
        public new void RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the argument at the specified index; setting an element
        /// invalidates the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the argument to get or set.
        /// </param>
        /// <returns>
        /// The argument at the specified index.
        /// </returns>
        public new Argument this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region List<Argument> Overrides
        /// <summary>
        /// Adds the arguments in the specified collection to the end of this
        /// list, invalidating the cached string representation.
        /// </summary>
        /// <param name="collection">
        /// The collection of arguments to add.
        /// </param>
        public new void AddRange(
            IEnumerable<Argument> collection
            )
        {
            InvalidateCachedString(false);

            base.AddRange(collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Inserts the arguments in the specified collection into this list at
        /// the specified index, invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the new arguments are inserted.
        /// </param>
        /// <param name="collection">
        /// The collection of arguments to insert.
        /// </param>
        public new void InsertRange(
            int index,
            IEnumerable<Argument> collection
            )
        {
            InvalidateCachedString(false);

            base.InsertRange(index, collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes all arguments from this list that match the conditions
        /// defined by the specified predicate, invalidating the cached string
        /// representation.
        /// </summary>
        /// <param name="match">
        /// The predicate that defines the conditions of the arguments to
        /// remove.
        /// </param>
        /// <returns>
        /// The number of arguments removed from this list.
        /// </returns>
        public new int RemoveAll(
            Predicate<Argument> match
            )
        {
            InvalidateCachedString(false);

            return base.RemoveAll(match); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the specified number of arguments from this list starting at
        /// the specified index, invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based starting index of the range of arguments to remove.
        /// </param>
        /// <param name="count">
        /// The number of arguments to remove.
        /// </param>
        public new void RemoveRange(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.RemoveRange(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reverses the order of the arguments in this list, invalidating the
        /// cached string representation.
        /// </summary>
        public new void Reverse()
        {
            InvalidateCachedString(false);

            base.Reverse(); /* O(N) */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reverses the order of the arguments in the specified range of this
        /// list, invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based starting index of the range to reverse.
        /// </param>
        /// <param name="count">
        /// The number of arguments in the range to reverse.
        /// </param>
        public new void Reverse(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.Reverse(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the arguments in this list using the default comparer,
        /// invalidating the cached string representation.
        /// </summary>
        public new void Sort()
        {
            InvalidateCachedString(false);

            base.Sort();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the arguments in this list using the specified comparison,
        /// invalidating the cached string representation.
        /// </summary>
        /// <param name="comparison">
        /// The comparison used when ordering the arguments.
        /// </param>
        public new void Sort(
            Comparison<Argument> comparison
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparison); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the arguments in this list using the specified comparer,
        /// invalidating the cached string representation.
        /// </summary>
        /// <param name="comparer">
        /// The comparer used when ordering the arguments, or null to use the
        /// default comparer.
        /// </param>
        public new void Sort(
            IComparer<Argument> comparer
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparer); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the arguments in the specified range of this list using the
        /// specified comparer, invalidating the cached string representation.
        /// </summary>
        /// <param name="index">
        /// The zero-based starting index of the range to sort.
        /// </param>
        /// <param name="count">
        /// The number of arguments in the range to sort.
        /// </param>
        /// <param name="comparer">
        /// The comparer used when ordering the arguments, or null to use the
        /// default comparer.
        /// </param>
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
