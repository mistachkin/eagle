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
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents the definition of a single command option (i.e. a
    /// switch) that may be recognized while processing the arguments to a
    /// command, function, or sub-command.  It captures the option name, its
    /// expected value type and flags, the option group it belongs to, the
    /// index at which it was found, and the value (if any) that was supplied
    /// for it.  Instances are typically created via the static "factory"
    /// methods or parsed from their string form, and are then collected into an
    /// <see cref="OptionDictionary" /> for argument processing.
    /// </summary>
    [ObjectId("3081850b-bbde-4b8f-bc24-24513df11f2d")]
    public sealed class Option :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IOption
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default set of option categories assigned to a newly created
        /// option when none are explicitly specified.
        /// </summary>
        private static OptionCategory defaultCategories = OptionCategory.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character that introduces an option name (i.e. the leading
        /// minus sign).
        /// </summary>
        private static readonly char OptionCharacter = Characters.MinusSign;

        /// <summary>
        /// The string form of the character that introduces an option name,
        /// used as the prefix when formatting an option name.
        /// </summary>
        private static readonly string OptionPrefix = OptionCharacter.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        /// <summary>
        /// The well-known token that marks the end of the options for a command,
        /// after which all remaining arguments are treated as non-option
        /// arguments.
        /// </summary>
        public static readonly string EndOfOptions = "--";

        /// <summary>
        /// The well-known token that requests the list of supported options for
        /// a command.
        /// </summary>
        public static readonly string ListOfOptions = "---";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // NOTE: How many list elements are minimally required when creating
        //       a basic option from a string?
        //
        /// <summary>
        /// The minimum number of list elements that must be present when
        /// creating a basic option from its string form.
        /// </summary>
        private const int MinimumElementCount = 2;

        //
        // NOTE: How many list elements are required when creating an option
        //       entirely from a string?
        //
        /// <summary>
        /// The number of list elements that must be present when creating an
        /// option entirely from its string form.
        /// </summary>
        private const int StandardElementCount = 5;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new option using the default option categories.
        /// </summary>
        /// <param name="type">
        /// The expected managed type associated with this option, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how this option is processed.
        /// </param>
        /// <param name="groupIndex">
        /// The index of the logical option group this option belongs to, or an
        /// invalid index if it does not belong to a group.
        /// </param>
        /// <param name="index">
        /// The argument index at which this option was found, or an invalid
        /// index if it has not been found.
        /// </param>
        /// <param name="name">
        /// The name of this option.
        /// </param>
        /// <param name="value">
        /// The value supplied for this option, if any.  This parameter may be
        /// null.
        /// </param>
        public Option(
            Type type,         /* in */
            OptionFlags flags, /* in */
            int groupIndex,    /* in */
            int index,         /* in */
            string name,       /* in */
            IVariant value     /* in */
            ) : this(type, defaultCategories, flags,
                     groupIndex, index, name, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new option with the specified option categories.
        /// </summary>
        /// <param name="type">
        /// The expected managed type associated with this option, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="categories">
        /// The categories assigned to this option.
        /// </param>
        /// <param name="flags">
        /// The flags that control how this option is processed.
        /// </param>
        /// <param name="groupIndex">
        /// The index of the logical option group this option belongs to, or an
        /// invalid index if it does not belong to a group.
        /// </param>
        /// <param name="index">
        /// The argument index at which this option was found, or an invalid
        /// index if it has not been found.
        /// </param>
        /// <param name="name">
        /// The name of this option.
        /// </param>
        /// <param name="value">
        /// The value supplied for this option, if any.  This parameter may be
        /// null.
        /// </param>
        public Option(
            Type type,                 /* in */
            OptionCategory categories, /* in */
            OptionFlags flags,         /* in */
            int groupIndex,            /* in */
            int index,                 /* in */
            string name,               /* in */
            IVariant value             /* in */
            )
        {
            this.kind = IdentifierKind.Option;
            this.name = name;
            this.description = null;
            this.clientData = null;
            this.type = type;
            this.categories = categories;
            this.flags = flags;
            this.groupIndex = groupIndex;
            this.index = index;
            this.value = value;
            this.defaultValue = (value != null) ? new Variant(value) : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this option.
        /// </summary>
        /// <returns>
        /// The string representation of this option.
        /// </returns>
        public override string ToString()
        {
            return ToString(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this option.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name of this option.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this object.
        /// </summary>
        private IdentifierKind kind;

        /// <summary>
        /// Gets or sets the kind of identifier represented by this object.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier associated with this option.
        /// </summary>
        private Guid id;

        /// <summary>
        /// Gets or sets the unique identifier associated with this option.
        /// </summary>
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The extra data, if any, associated with this option by the
        /// application.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the extra data, if any, associated with this option by
        /// the application.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The name of the group, if any, that this option belongs to.
        /// </summary>
        private string group;

        /// <summary>
        /// Gets or sets the name of the group, if any, that this option belongs
        /// to.
        /// </summary>
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description, if any, of this option.
        /// </summary>
        private string description;

        /// <summary>
        /// Gets or sets the human-readable description, if any, of this option.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IOption Members
        /// <summary>
        /// The expected managed type, if any, associated with this option.
        /// </summary>
        private Type type;

        /// <summary>
        /// Gets or sets the expected managed type, if any, associated with this
        /// option.
        /// </summary>
        public Type Type
        {
            get { return type; }
            set { type = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The categories assigned to this option.
        /// </summary>
        private OptionCategory categories;

        /// <summary>
        /// Gets or sets the categories assigned to this option.
        /// </summary>
        public OptionCategory Categories
        {
            get { return categories; }
            set { categories = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control how this option is processed.
        /// </summary>
        private OptionFlags flags;

        /// <summary>
        /// Gets or sets the flags that control how this option is processed.
        /// </summary>
        public OptionFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Which logical option grouping does this belong to (e.g.
        //       [lsort] has string comparison types like "ascii",
        //       "dictionary", "integer", and "real", and ordering types like
        //       "ascending" / "descending").
        //
        /// <summary>
        /// The index of the logical option group this option belongs to, or an
        /// invalid index if it does not belong to a group.
        /// </summary>
        private int groupIndex;

        /// <summary>
        /// Gets or sets the index of the logical option group this option
        /// belongs to, or an invalid index if it does not belong to a group.
        /// </summary>
        public int GroupIndex
        {
            get { return groupIndex; }
            set { groupIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The argument index at which this option was found, or an invalid
        /// index if it has not been found.
        /// </summary>
        private int index;

        /// <summary>
        /// Gets or sets the argument index at which this option was found, or an
        /// invalid index if it has not been found.
        /// </summary>
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The value, if any, supplied for this option.
        /// </summary>
        private IVariant value;

        /// <summary>
        /// Gets or sets the value, if any, supplied for this option.
        /// </summary>
        public IVariant Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the underlying object value, if any, wrapped by the value
        /// supplied for this option.
        /// </summary>
        public object InnerValue
        {
            get { return (value != null) ? value.Value : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default value, if any, captured for this option when it was
        /// created.
        /// </summary>
        private IVariant defaultValue;

        /// <summary>
        /// Gets the default value, if any, captured for this option when it was
        /// created.
        /// </summary>
        public IVariant DefaultValue
        {
            get { return defaultValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the underlying object value, if any, wrapped by the default
        /// value captured for this option.
        /// </summary>
        public object DefaultInnerValue
        {
            get { return (defaultValue != null) ? defaultValue.Value : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option has the specified
        /// categories.
        /// </summary>
        /// <param name="categories">
        /// The categories to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified categories be present;
        /// zero to require that any of them be present.
        /// </param>
        /// <returns>
        /// True if the required categories are present; otherwise, false.
        /// </returns>
        public bool HasCategories(
            OptionCategory categories,
            bool all
            )
        {
            return FlagOps.HasFlags(this.categories, categories, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option has the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be present; zero
        /// to require that any of them be present.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
        public bool HasFlags(
            OptionFlags flags, /* in */
            bool all           /* in */
            )
        {
            return FlagOps.HasFlags(this.flags, flags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option is strict, meaning that
        /// an exact match is required.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option is strict; otherwise, false.
        /// </returns>
        public bool IsStrict(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Strict, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option is matched without regard
        /// to case.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option is matched without regard to case; otherwise,
        /// false.
        /// </returns>
        public bool IsNoCase(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.NoCase, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option is considered unsafe and
        /// is therefore disallowed in a safe interpreter.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option is unsafe; otherwise, false.
        /// </returns>
        public bool IsUnsafe(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Unsafe, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option is restricted.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option is restricted; otherwise, false.
        /// </returns>
        public bool IsRestricted(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Restricted, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option permits an integer value
        /// in addition to its normal value type.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option permits an integer value; otherwise, false.
        /// </returns>
        public bool IsAllowInteger(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.AllowInteger, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option is ignored during
        /// processing.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option is ignored; otherwise, false.
        /// </returns>
        public bool IsIgnored(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Ignored, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option requires a value to be
        /// supplied.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option requires a value; otherwise, false.
        /// </returns>
        public bool MustHaveValue(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.MustHaveValue, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option is permitted to be
        /// present, taking into account whether it is unsupported on the
        /// current platform or has been disabled.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if this option is permitted to be present; otherwise, false.
        /// </returns>
        public bool CanBePresent(
            OptionDictionary options, /* in: NOT USED */
            ref Result error          /* out */
            )
        {
            if (HasFlags(OptionFlags.Unsupported, true))
            {
                error = String.Format(
                    "option \"{0}\" not supported for this platform",
                    name);

                return false;
            }

            if (HasFlags(OptionFlags.Disabled, true))
            {
                error = String.Format(
                    "option \"{0}\" is disabled",
                    name);

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option was present among the
        /// arguments that were processed.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <returns>
        /// True if this option was present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Present, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option was present among the
        /// arguments that were processed and, if so, returns the argument
        /// indexes associated with its name and value.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <param name="nameIndex">
        /// Upon success, this contains the argument index at which the option
        /// name was found.
        /// </param>
        /// <param name="valueIndex">
        /// Upon success, this contains the argument index at which the option
        /// value was found, when this option must have a value.
        /// </param>
        /// <returns>
        /// True if this option was present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            OptionDictionary options, /* in: NOT USED */
            ref int nameIndex,        /* out */
            ref int valueIndex        /* out */
            )
        {
            if (HasFlags(OptionFlags.Present, true))
            {
                nameIndex = this.index;

                if (MustHaveValue(options))
                    valueIndex = nameIndex + 1;

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this option was present among the
        /// arguments that were processed and, if so, returns the value that was
        /// supplied for it.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed.  This parameter is not
        /// used.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the value supplied for this option, when
        /// this option must have a value.
        /// </param>
        /// <returns>
        /// True if this option was present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            OptionDictionary options, /* in: NOT USED */
            ref IVariant value        /* out */
            )
        {
            if (HasFlags(OptionFlags.Present, true))
            {
                if (MustHaveValue(options))
                    value = this.value;

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this option as present or not present and, when
        /// marking it present, records the argument index and value supplied
        /// for it.  When this option is marked present, all other options in
        /// the same option group are marked as not present.
        /// </summary>
        /// <param name="options">
        /// The collection of options being processed, used to locate the other
        /// options in this option group.  This parameter may be null.
        /// </param>
        /// <param name="present">
        /// Non-zero to mark this option as present; zero to mark it as not
        /// present.
        /// </param>
        /// <param name="index">
        /// The argument index at which this option was found.
        /// </param>
        /// <param name="value">
        /// The value supplied for this option, if any.  This parameter may be
        /// null.
        /// </param>
        public void SetPresent(
            OptionDictionary options, /* in */
            bool present,             /* in */
            int index,                /* in */
            IVariant value            /* in */
            )
        {
            if (present)
            {
                this.flags |= OptionFlags.Present;
                this.index = index;
                this.value = value;
            }
            else
            {
                this.flags &= ~OptionFlags.Present;
                this.index = _Constants.Index.Invalid;
                this.value = null;
            }

            //
            // NOTE: Now mark all the other options in this option group as
            //       "not present".
            //
            if ((options != null) && (options.Values != null))
            {
                foreach (IOption option in options.Values)
                {
                    if ((option != null) &&
                        !Object.ReferenceEquals(option, this) &&
                        (option.GroupIndex != _Constants.Index.Invalid) &&
                        (option.GroupIndex == this.groupIndex))
                    {
                        //
                        // NOTE: Only modify the flags since that is how we detect
                        //       whether the option is considered to be "present".
                        //
                        option.Flags &= ~OptionFlags.Present;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a human-readable description of the value type
        /// implied by the specified option flags (e.g. "integer", "boolean",
        /// "list", etc.).
        /// </summary>
        /// <param name="flags">
        /// The option flags to describe.
        /// </param>
        /// <returns>
        /// A human-readable description of the value type implied by the
        /// specified option flags.
        /// </returns>
        public string ToString(
            OptionFlags flags /* in */
            )
        {
            string result;

            if (FlagOps.HasFlags(flags, OptionFlags.MustHaveValue, true))
            {
                if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeRuleSet, true))
                {
                    result = "rule set";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeCallback, true))
                {
                    result = "callback";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeExecute, true))
                {
                    result = "execute";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBePlugin, true))
                {
                    result = "plugin";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeEncoding, true))
                {
                    result = "encoding";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeCultureInfo, true))
                {
                    result = "culture info";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeSecureString, true))
                {
                    result = "secure string";
                }
#if NATIVE && TCL
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeTclInterpreter, true))
                {
                    result = "tcl interpreter";
                }
#endif
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeRelativeNamespace, true))
                {
                    result = "relative namespace";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeAbsoluteNamespace, true))
                {
                    result = "absolute namespace";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeOption, true))
                {
                    result = "option";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeIdentifier, true))
                {
                    result = "identifier";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeAlias, true))
                {
                    result = "alias";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeReturnCodeList, true))
                {
                    result = "return code list";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeAbsoluteUri, true))
                {
                    result = "absolute uri";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeVersion, true))
                {
                    result = "version";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeTypeList, true))
                {
                    result = "type list";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeType, true))
                {
                    result = "type";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeInterpreter, true))
                {
                    result = "interpreter";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeObject, true))
                {
                    result = "object";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeValue, true))
                {
                    result = "numeric";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeMatchMode, true))
                {
                    result = "match mode";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeDictionary, true))
                {
                    result = "dictionary";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeList, true))
                {
                    result = "list";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeTimeSpan, true))
                {
                    result = "time-span";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeDateTime, true))
                {
                    result = "date-time";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeGuid, true))
                {
                    result = "guid";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeEnum, true))
                {
                    result = (type != null) ?
                        String.Format("{0} enumeration",
                            FormatOps.TypeNameOrFullName(type)) :
                        "enumeration";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeEnumList, true))
                {
                    result = (type != null) ?
                        String.Format("{0} enumeration list",
                            FormatOps.TypeNameOrFullName(type)) :
                        "enumeration list";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeReturnCode, true))
                {
                    result = "return code";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeLevel, true))
                {
                    result = "level";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeIndex, true))
                {
                    result = "index";
                }
#if NET_40
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeBigInteger, true))
                {
                    result = "big integer";
                }
#endif
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeUnsignedWideInteger, true))
                {
                    result = "unsigned wide integer";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeWideInteger, true))
                {
                    result = "wide integer";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeUnsignedInteger, true))
                {
                    result = "unsigned integer";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeInteger, true))
                {
                    result = "integer";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeUnsignedNarrowInteger, true))
                {
                    result = "unsigned narrow integer";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeNarrowInteger, true))
                {
                    result = "narrow integer";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeByte, true))
                {
                    result = "byte";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeSignedByte, true))
                {
                    result = "signed byte";
                }
                else if (FlagOps.HasFlags(
                        flags, OptionFlags.MustBeBoolean, true))
                {
                    result = "boolean";
                }
                else
                {
                    result = "string";
                }
            }
            else if (FlagOps.HasFlags(flags, OptionFlags.EndOfOptions, true))
            {
                result = "end of options";
            }
            else if (FlagOps.HasFlags(flags, OptionFlags.ListOfOptions, true))
            {
                result = "list of options";
            }
            else
            {
                result = "nothing";
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list containing the name and value of the
        /// specified option.
        /// </summary>
        /// <param name="option">
        /// The option to convert to a list.  This parameter may be null, in
        /// which case this option is used.
        /// </param>
        /// <returns>
        /// A list containing the name and value of the specified option.
        /// </returns>
        public StringList ToList(
            IOption option /* in */
            )
        {
            StringList list = new StringList();

            if (option == null)
                option = this;

            list.Add(option.Name);

            IVariant value = option.Value;

            list.Add((value != null) ? value.ToString() : null);

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a human-readable description of the value type
        /// implied by the flags of this option.
        /// </summary>
        /// <returns>
        /// A human-readable description of the value type implied by the flags
        /// of this option.
        /// </returns>
        public string FlagsToString()
        {
            return ToString(flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string representation of the specified
        /// option, consisting of its name followed by its value.
        /// </summary>
        /// <param name="option">
        /// The option to convert to a string.  This parameter may be null, in
        /// which case this option is used.
        /// </param>
        /// <returns>
        /// The string representation of the specified option.
        /// </returns>
        public string ToString(
            IOption option /* in */
            )
        {
            return ParserOps<string>.ListToString(
                ToList(option), _Constants.Index.Invalid,
                _Constants.Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new option with no associated option group or
        /// argument index.
        /// </summary>
        /// <param name="type">
        /// The expected managed type associated with the option, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the option is processed.
        /// </param>
        /// <param name="name">
        /// The name of the option.
        /// </param>
        /// <param name="value">
        /// The value supplied for the option, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created option.
        /// </returns>
        private static IOption Create(
            Type type,         /* in */
            OptionFlags flags, /* in */
            string name,       /* in */
            IVariant value     /* in */
            )
        {
            return new Option(
                type, flags, _Constants.Index.Invalid,
                _Constants.Index.Invalid, name, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates the special option that represents a request for
        /// the list of supported options.
        /// </summary>
        /// <returns>
        /// The newly created list-of-options option.
        /// </returns>
        internal static IOption CreateListOfOptions()
        {
            return Create(null,
                OptionFlags.System | OptionFlags.ListOfOptions,
                ListOfOptions, null);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new option with no associated managed type.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the option is processed.
        /// </param>
        /// <param name="name">
        /// The name of the option.
        /// </param>
        /// <param name="value">
        /// The value supplied for the option, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created option.
        /// </returns>
        public static IOption Create(
            OptionFlags flags, /* in */
            string name,       /* in */
            IVariant value     /* in */
            )
        {
            return Create(null, flags, name, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new, simple option that has no flags, no
        /// associated managed type, and no value.
        /// </summary>
        /// <param name="name">
        /// The name of the option.
        /// </param>
        /// <returns>
        /// The newly created option.
        /// </returns>
        public static IOption CreateSimple(
            string name /* in */
            )
        {
            return Create(null, OptionFlags.None, name, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new option that must have a string value.
        /// </summary>
        /// <param name="name">
        /// The name of the option.
        /// </param>
        /// <param name="value">
        /// The string value supplied for the option, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The newly created option.
        /// </returns>
        public static IOption CreateString(
            string name, /* in */
            string value /* in */
            )
        {
            return Create(null,
                OptionFlags.MustHaveValue, name, (value != null) ?
                new Variant(value) : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new option that must have an enumerated value
        /// (or a list of enumerated values) of the specified type.
        /// </summary>
        /// <param name="type">
        /// The enumerated type associated with the option.
        /// </param>
        /// <param name="name">
        /// The name of the option.
        /// </param>
        /// <param name="value">
        /// The enumerated value supplied for the option, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="list">
        /// Non-zero if the option accepts a list of enumerated values; zero if
        /// it accepts a single enumerated value.
        /// </param>
        /// <returns>
        /// The newly created option.
        /// </returns>
        public static IOption CreateEnum(
            Type type,   /* in */
            string name, /* in */
            Enum value,  /* in */
            bool list    /* in */
            )
        {
            return Create(
                type, list ? OptionFlags.MustHaveEnumListValue :
                OptionFlags.MustHaveEnumValue, name, (value != null) ?
                new Variant(value) : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates the special option that represents the end of
        /// the options for a command.
        /// </summary>
        /// <returns>
        /// The newly created end-of-options option.
        /// </returns>
        public static IOption CreateEndOfOptions()
        {
            return CreateSimple(EndOfOptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text is the end-of-
        /// options token.
        /// </summary>
        /// <param name="text">
        /// The text to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified text is the end-of-options token; otherwise,
        /// false.
        /// </returns>
        public static bool IsEndOfOptions(
            string text /* in */
            )
        {
            return !String.IsNullOrEmpty(text) &&
                !String.IsNullOrEmpty(EndOfOptions) &&
                (text.Length == EndOfOptions.Length) &&
                SharedStringOps.SystemEquals(text, EndOfOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text is the list-of-
        /// options token.
        /// </summary>
        /// <param name="text">
        /// The text to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified text is the list-of-options token; otherwise,
        /// false.
        /// </returns>
        public static bool IsListOfOptions(
            string text /* in */
            )
        {
            return !String.IsNullOrEmpty(text) &&
                !String.IsNullOrEmpty(ListOfOptions) &&
                (text.Length == ListOfOptions.Length) &&
                SharedStringOps.SystemEquals(text, ListOfOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text looks like an
        /// option name (i.e. whether it begins with the option character).
        /// </summary>
        /// <param name="text">
        /// The text to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified text looks like an option name; otherwise,
        /// false.
        /// </returns>
        public static bool LooksLikeOption(
            string text /* in */
            )
        {
            return !String.IsNullOrEmpty(text) &&
                (text[0] == OptionCharacter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified option name, optionally prefixing
        /// it with the option character.
        /// </summary>
        /// <param name="name">
        /// The option name to format.  This parameter may be null or empty, in
        /// which case it is returned unchanged.
        /// </param>
        /// <param name="prefix">
        /// Non-zero to prefix the option name with the option character; zero
        /// to omit the prefix.
        /// </param>
        /// <returns>
        /// The formatted option name.
        /// </returns>
        public static string FormatOption(
            string name, /* in */
            bool prefix  /* in */
            )
        {
            if (String.IsNullOrEmpty(name))
                return name;

            return String.Format("{0}{1}",
                prefix ? OptionPrefix : String.Empty, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an option from its full string form, which must
        /// contain the type name, flags, group index, argument index, name, and
        /// (optionally) value.  This overload accepts the individual type
        /// resolution flags as separate parameters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The string form of the option to parse.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to use when resolving the type name, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit integer values where a type or enumerated value
        /// is expected.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require strict matching when resolving types and parsing
        /// flags.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce verbose error messages when resolving the type
        /// name.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case when resolving types and parsing flags.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing values.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Upon success, the newly created option; otherwise, null.
        /// </returns>
        public static IOption FromString( /* COMPAT: Eagle beta. */
            Interpreter interpreter, /* in */
            string text,             /* in */
            AppDomain appDomain,     /* in */
            bool allowInteger,       /* in */
            bool strict,             /* in */
            bool verbose,            /* in */
            bool noCase,             /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            if (list.Count < StandardElementCount)
            {
                error = String.Format(
                    "cannot create option, only {0} of {1} " +
                    "required elements were specified", list.Count,
                    StandardElementCount);

                return null;
            }

            Type type = null;
            ResultList errors = null;

            if (_Public.Value.GetAnyType(
                    interpreter, list[0], null, appDomain,
                    _Public.Value.GetTypeValueFlags(
                        allowInteger, strict, verbose,
                        noCase), cultureInfo, ref type,
                    ref errors) != ReturnCode.Ok)
            {
                error = errors;
                return null;
            }

            object enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(OptionFlags), null,
                list[1], cultureInfo, allowInteger, strict,
                noCase, ref error);

            if (!(enumValue is OptionFlags))
                return null;

            OptionFlags optionFlags = (OptionFlags)enumValue;
            int groupIndex = 0;

            if (_Public.Value.GetInteger2(
                    list[2], ValueFlags.AnyInteger, cultureInfo,
                    ref groupIndex, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            int index = 0;

            if (_Public.Value.GetInteger2(
                    list[3], ValueFlags.AnyInteger, cultureInfo,
                    ref index, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            string name = list[4];

            if (name == null)
            {
                error = "invalid option name";
                return null;
            }

            int nextIndex = StandardElementCount;
            IVariant value = null;

            if (ScriptOps.GetOptionValue(
                    interpreter, list, type, optionFlags, true, allowInteger,
                    strict, noCase, cultureInfo, ref value, ref nextIndex,
                    ref error) != ReturnCode.Ok)
            {
                return null;
            }

            return new Option(
                type, optionFlags, groupIndex, index, name, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an option from its string form, which must
        /// contain at least the flags and name, and may optionally contain a
        /// type name, value, and group index.  This overload accepts the type
        /// resolution flags combined into a single value flags parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The string form of the option to parse.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to use when resolving the type name, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that control how types are resolved and values are
        /// parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing values.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Upon success, the newly created option; otherwise, null.
        /// </returns>
        public static IOption FromString(
            Interpreter interpreter, /* in */
            string text,             /* in */
            AppDomain appDomain,     /* in */
            ValueFlags valueFlags,   /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            if (list.Count < MinimumElementCount)
            {
                error = String.Format(
                    "cannot create option, only {0} of {1} " +
                    "required elements were specified", list.Count,
                    MinimumElementCount);

                return null;
            }

            bool allowInteger;
            bool strict;
            bool verbose;
            bool noCase;

            _Public.Value.ExtractTypeValueFlags(
                valueFlags, out allowInteger, out strict,
                out verbose, out noCase);

            object enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(OptionFlags), null,
                list[0], cultureInfo, allowInteger, strict,
                noCase, ref error);

            if (!(enumValue is OptionFlags))
                return null;

            OptionFlags optionFlags = (OptionFlags)enumValue;
            string name = list[1];

            if (name == null)
            {
                error = "invalid option name";
                return null;
            }

            int nextIndex = MinimumElementCount;
            Type type = null;

            if (FlagOps.HasFlags(
                    optionFlags, OptionFlags.MustBeEnumMask, false))
            {
                if (nextIndex >= list.Count)
                {
                    error = String.Format(
                        "option with {0} or {1} flags must have type name",
                        FormatOps.WrapOrNull(OptionFlags.MustBeEnum),
                        FormatOps.WrapOrNull(OptionFlags.MustBeEnumList));

                    return null;
                }

                ResultList errors = null;

                if (_Public.Value.GetAnyType(
                        interpreter, list[nextIndex], null, appDomain,
                        _Public.Value.GetTypeValueFlags(optionFlags),
                        cultureInfo, ref type, ref errors) != ReturnCode.Ok)
                {
                    error = errors;
                    return null;
                }

                nextIndex++;
            }

            IVariant value = null;

            if (ScriptOps.GetOptionValue(
                    interpreter, list, type, optionFlags, false, allowInteger,
                    strict, noCase, cultureInfo, ref value, ref nextIndex,
                    ref error) != ReturnCode.Ok)
            {
                return null;
            }

            int groupIndex = _Constants.Index.Invalid;

            if (nextIndex < list.Count)
            {
                if (_Public.Value.GetInteger2(
                        list[nextIndex], ValueFlags.AnyInteger, cultureInfo,
                        ref groupIndex, ref error) != ReturnCode.Ok)
                {
                    return null;
                }

                nextIndex++;
            }

            return new Option(
                type, optionFlags, groupIndex, _Constants.Index.Invalid,
                name, value);
        }
        #endregion
    }
}
