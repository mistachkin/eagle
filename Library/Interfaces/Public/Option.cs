/*
 * Option.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that represent a single
    /// command option, including its type, categories, flags, ordering, and
    /// current and default values, together with the operations used to test
    /// for and record the option's presence within an
    /// <see cref="OptionDictionary" />.  It extends
    /// <see cref="IIdentifier" />.
    /// </summary>
    [ObjectId("d65817f4-92df-4ebb-a36c-d9483651d372")]
    public interface IOption : IIdentifier
    {
        /// <summary>
        /// Gets or sets the underlying value type of this option.
        /// </summary>
        Type Type { get; set; }
        /// <summary>
        /// Gets or sets the categories this option belongs to.
        /// </summary>
        OptionCategory Categories { get; set; }
        /// <summary>
        /// Gets or sets the flags that control the behavior of this option.
        /// </summary>
        OptionFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the index of the mutually-exclusive group this option
        /// belongs to.
        /// </summary>
        int GroupIndex { get; set; }
        /// <summary>
        /// Gets or sets the ordinal index of this option.
        /// </summary>
        int Index { get; set; }

        /// <summary>
        /// Gets or sets the current value of this option.
        /// </summary>
        IVariant Value { get; set; }
        /// <summary>
        /// Gets the underlying inner value of this option.
        /// </summary>
        object InnerValue { get; }

        /// <summary>
        /// Gets the default value of this option.
        /// </summary>
        IVariant DefaultValue { get; }
        /// <summary>
        /// Gets the underlying inner default value of this option.
        /// </summary>
        object DefaultInnerValue { get; }

        /// <summary>
        /// Determines whether this option belongs to the specified categories.
        /// </summary>
        /// <param name="categories">
        /// The categories to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero if this option must belong to all of the specified
        /// categories; otherwise, matching any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if this option matches the specified categories; otherwise,
        /// false.
        /// </returns>
        bool HasCategories(OptionCategory categories, bool all);
        /// <summary>
        /// Determines whether this option has the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero if this option must have all of the specified flags;
        /// otherwise, matching any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if this option matches the specified flags; otherwise, false.
        /// </returns>
        bool HasFlags(OptionFlags flags, bool all);

        /// <summary>
        /// Determines whether this option is configured for strict processing.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option is strict; otherwise, false.
        /// </returns>
        bool IsStrict(OptionDictionary options);
        /// <summary>
        /// Determines whether this option is processed without regard to case.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option is case-insensitive; otherwise, false.
        /// </returns>
        bool IsNoCase(OptionDictionary options);
        /// <summary>
        /// Determines whether this option is considered unsafe.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option is unsafe; otherwise, false.
        /// </returns>
        bool IsUnsafe(OptionDictionary options);
        /// <summary>
        /// Determines whether this option is restricted.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option is restricted; otherwise, false.
        /// </returns>
        bool IsRestricted(OptionDictionary options);
        /// <summary>
        /// Determines whether this option allows an integer value.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option allows an integer value; otherwise, false.
        /// </returns>
        bool IsAllowInteger(OptionDictionary options);
        /// <summary>
        /// Determines whether this option is ignored.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option is ignored; otherwise, false.
        /// </returns>
        bool IsIgnored(OptionDictionary options);
        /// <summary>
        /// Determines whether this option must be supplied with a value.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option must have a value; otherwise, false.
        /// </returns>
        bool MustHaveValue(OptionDictionary options);

        /// <summary>
        /// Determines whether this option is permitted to be present given the
        /// current option context.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if this option can be present; otherwise, false.
        /// </returns>
        bool CanBePresent(OptionDictionary options, ref Result error);
        /// <summary>
        /// Determines whether this option is present in the specified option
        /// context.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <returns>
        /// True if this option is present; otherwise, false.
        /// </returns>
        bool IsPresent(OptionDictionary options);
        /// <summary>
        /// Determines whether this option is present in the specified option
        /// context, also reporting the argument indexes at which it was found.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <param name="nameIndex">
        /// Upon return, receives the argument index of the option name, if it
        /// was present.
        /// </param>
        /// <param name="valueIndex">
        /// Upon return, receives the argument index of the option value, if
        /// any.
        /// </param>
        /// <returns>
        /// True if this option is present; otherwise, false.
        /// </returns>
        bool IsPresent(OptionDictionary options, ref int nameIndex, ref int valueIndex);
        /// <summary>
        /// Determines whether this option is present in the specified option
        /// context, also reporting its value.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this query.
        /// </param>
        /// <param name="value">
        /// Upon return, receives the value of the option, if it was present.
        /// </param>
        /// <returns>
        /// True if this option is present; otherwise, false.
        /// </returns>
        bool IsPresent(OptionDictionary options, ref IVariant value);
        /// <summary>
        /// Records whether this option is present, along with its argument
        /// index and value.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options providing the context for this operation.
        /// </param>
        /// <param name="present">
        /// Non-zero if this option is to be marked as present; otherwise, zero.
        /// </param>
        /// <param name="index">
        /// The argument index at which this option was found.
        /// </param>
        /// <param name="value">
        /// The value associated with this option, if any.
        /// </param>
        void SetPresent(OptionDictionary options, bool present, int index, IVariant value);

        /// <summary>
        /// Builds a list representation of the specified option.
        /// </summary>
        /// <param name="option">
        /// The option to convert into a list.
        /// </param>
        /// <returns>
        /// A <see cref="StringList" /> describing the specified option.
        /// </returns>
        StringList ToList(IOption option);

        /// <summary>
        /// Builds a string representation of the flags for this option.
        /// </summary>
        /// <returns>
        /// A string describing the flags for this option.
        /// </returns>
        string FlagsToString();
        /// <summary>
        /// Builds a string representation of the specified option.
        /// </summary>
        /// <param name="option">
        /// The option to convert into a string.
        /// </param>
        /// <returns>
        /// A string describing the specified option.
        /// </returns>
        string ToString(IOption option);
        /// <summary>
        /// Builds a string representation of the specified option flags.
        /// </summary>
        /// <param name="flags">
        /// The option flags to convert into a string.
        /// </param>
        /// <returns>
        /// A string describing the specified option flags.
        /// </returns>
        string ToString(OptionFlags flags);
    }
}
