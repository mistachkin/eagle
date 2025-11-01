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
        private static OptionCategory defaultCategories = OptionCategory.None;

        ///////////////////////////////////////////////////////////////////////

        private static readonly char OptionCharacter = Characters.MinusSign;
        private static readonly string OptionPrefix = OptionCharacter.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly string EndOfOptions = "--";
        public static readonly string ListOfOptions = "---";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // NOTE: How many list elements are minimally required when creating
        //       a basic option from a string?
        //
        private const int MinimumElementCount = 2;

        //
        // NOTE: How many list elements are required when creating an option
        //       entirely from a string?
        //
        private const int StandardElementCount = 5;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        public override string ToString()
        {
            return ToString(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IOption Members
        private Type type;
        public Type Type
        {
            get { return type; }
            set { type = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private OptionCategory categories;
        public OptionCategory Categories
        {
            get { return categories; }
            set { categories = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private OptionFlags flags;
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
        private int groupIndex;
        public int GroupIndex
        {
            get { return groupIndex; }
            set { groupIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int index;
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IVariant value;
        public IVariant Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public object InnerValue
        {
            get { return (value != null) ? value.Value : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IVariant defaultValue;
        public IVariant DefaultValue
        {
            get { return defaultValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        public object DefaultInnerValue
        {
            get { return (defaultValue != null) ? defaultValue.Value : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasCategories(
            OptionCategory categories,
            bool all
            )
        {
            return FlagOps.HasFlags(this.categories, categories, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasFlags(
            OptionFlags flags, /* in */
            bool all           /* in */
            )
        {
            return FlagOps.HasFlags(this.flags, flags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsStrict(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Strict, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsNoCase(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.NoCase, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsUnsafe(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Unsafe, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsRestricted(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Restricted, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsAllowInteger(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.AllowInteger, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsIgnored(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Ignored, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MustHaveValue(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.MustHaveValue, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public bool IsPresent(
            OptionDictionary options /* in: NOT USED */
            )
        {
            return HasFlags(OptionFlags.Present, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public string FlagsToString()
        {
            return ToString(flags);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static IOption Create(
            OptionFlags flags, /* in */
            string name,       /* in */
            IVariant value     /* in */
            )
        {
            return Create(null, flags, name, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IOption CreateSimple(
            string name /* in */
            )
        {
            return Create(null, OptionFlags.None, name, null);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static IOption CreateEndOfOptions()
        {
            return CreateSimple(EndOfOptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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

        public static bool LooksLikeOption(
            string text /* in */
            )
        {
            return !String.IsNullOrEmpty(text) &&
                (text[0] == OptionCharacter);
        }

        ///////////////////////////////////////////////////////////////////////

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
