/*
 * OptionDictionary.cs --
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
using System.Globalization;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using OptionPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IOption>;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Interfaces.Public.IOption>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IOption>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    //
    // TODO: Centralize ALL options using ArgumentListOptionDictionary
    //       from a ranged ArgumentList to OptionDictionary.
    //
    /// <summary>
    /// This class represents an ordered collection of command options keyed by
    /// name.  It extends the dictionary used to map option names to their
    /// <see cref="IOption" /> definitions and provides the higher-level support
    /// used when parsing, resolving, querying, and formatting the options
    /// accepted by Eagle commands.  In addition to the usual add and lookup
    /// methods, it offers prefix-based (possibly case-insensitive) option
    /// resolution, presence and value tracking, category-based filtering, and
    /// the construction of human-readable error messages for bad or ambiguous
    /// options.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("755e6bb2-b7e3-42df-bc4e-81610901e093")]
    public sealed class OptionDictionary : SomeDictionary
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default value indicating whether verbose error messages (i.e. those
        /// that list the available options) should be produced.
        /// </summary>
        private static bool DefaultVerbose = false;
        /// <summary>
        /// The default value indicating whether option name matching should be
        /// case-insensitive.
        /// </summary>
        private static bool DefaultNoCase = false;
        /// <summary>
        /// The default value indicating whether a missing or unknown option should
        /// be treated as an error.
        /// </summary>
        private static bool DefaultStrict = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class.
        /// </summary>
        /// <param name="system">
        /// Non-zero to add the built-in (system) options to the newly created
        /// dictionary.
        /// </param>
        private OptionDictionary(
            bool system
            )
            : base()
        {
            if (system)
                AddSystemOptions(false, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class,
        /// copying the options from an existing dictionary.
        /// </summary>
        /// <param name="options">
        /// The existing dictionary whose options are to be copied into the newly
        /// created dictionary.
        /// </param>
        /// <param name="system">
        /// Non-zero to add the built-in (system) options to the newly created
        /// dictionary.
        /// </param>
        private OptionDictionary(
            OptionDictionary options,
            bool system
            )
            : base(options)
        {
            if (system)
                AddSystemOptions(false, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class,
        /// including the built-in (system) options.
        /// </summary>
        public OptionDictionary()
            : this(true)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class,
        /// adding the options from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of options to add to the newly created dictionary.
        /// </param>
        public OptionDictionary(
            IEnumerable<IOption> collection
            )
            : this()
        {
            foreach (IOption item in collection)
                MaybeAdd(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class,
        /// adding the options from the two specified collections.
        /// </summary>
        /// <param name="collection1">
        /// The first collection of options to add to the newly created dictionary.
        /// </param>
        /// <param name="collection2">
        /// The second collection of options to add to the newly created dictionary.
        /// </param>
        public OptionDictionary(
            IEnumerable<IOption> collection1,
            IEnumerable<IOption> collection2
            )
            : this(collection1)
        {
            foreach (IOption item in collection2)
                MaybeAdd(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class,
        /// adding the options from the specified collection of options followed by
        /// those from the specified collection of name and option pairs.
        /// </summary>
        /// <param name="collection1">
        /// The collection of options to add to the newly created dictionary.
        /// </param>
        /// <param name="collection2">
        /// The collection of name and option pairs to add to the newly created
        /// dictionary.
        /// </param>
        internal OptionDictionary(
            IEnumerable<IOption> collection1,
            IEnumerable<KeyValuePair<string, IOption>> collection2
            )
            : this(collection1)
        {
            foreach (KeyValuePair<string, IOption> pair in collection2)
                MaybeAdd(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class,
        /// including the built-in (system) options and adding the options from the
        /// two specified collections of name and option pairs.
        /// </summary>
        /// <param name="collection1">
        /// The first collection of name and option pairs to add to the newly created
        /// dictionary.
        /// </param>
        /// <param name="collection2">
        /// The second collection of name and option pairs to add to the newly
        /// created dictionary.
        /// </param>
        internal OptionDictionary(
            IEnumerable<KeyValuePair<string, IOption>> collection1,
            IEnumerable<KeyValuePair<string, IOption>> collection2
            )
            : this()
        {
            foreach (KeyValuePair<string, IOption> pair in collection1)
                MaybeAdd(pair.Key, pair.Value);

            foreach (KeyValuePair<string, IOption> pair in collection2)
                MaybeAdd(pair.Key, pair.Value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a new instance of the <see cref="OptionDictionary" /> class
        /// from previously serialized data (i.e. via the .NET Framework
        /// serialization subsystem).
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized data.
        /// </param>
        private OptionDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// Creates the set of built-in (system) options that should be present in
        /// every option dictionary.
        /// </summary>
        /// <returns>
        /// The collection of newly created built-in (system) options.
        /// </returns>
        private static IEnumerable<IOption> CreateSystemOptions(
            )
        {
            return new IOption[] { Option.CreateListOfOptions() };
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new option dictionary by parsing the specified string as a
        /// list, interpreting each element as an option definition.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to parse, in list form, where each element describes an option
        /// to be added to the new dictionary.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to use when resolving types referenced by the
        /// parsed options, if any.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to permit integer values to be used where an enumerated value is
        /// otherwise expected.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat any parsing or validation problem as an error.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce verbose error messages.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform string comparisons in a case-insensitive manner.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for parsing and comparisons, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// The newly created option dictionary, or null if it could not be created.
        /// </returns>
        public static OptionDictionary FromString(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            bool allowInteger,
            bool strict,
            bool verbose,
            bool noCase,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                OptionDictionary options = new OptionDictionary();

                foreach (string element in list)
                {
                    IOption option = Option.FromString(
                        interpreter, element, appDomain,
                        allowInteger, strict, verbose,
                        noCase, cultureInfo, ref error);

                    if (option == null)
                        return null;

                    if (options.Has(option))
                    {
                        error = String.Format(
                            "duplicate option name {0}",
                            FormatOps.WrapOrNull(option.Name));

                        return null;
                    }

                    options.Add(option);
                }

                return options;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new option dictionary by parsing the specified string as a
        /// list, interpreting each element as an option definition.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to parse, in list form, where each element describes an option
        /// to be added to the new dictionary.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to use when resolving types referenced by the
        /// parsed options, if any.
        /// </param>
        /// <param name="valueFlags">
        /// The flags used to control how the option values are parsed and
        /// interpreted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for parsing and comparisons, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// The newly created option dictionary, or null if it could not be created.
        /// </returns>
        public static OptionDictionary FromString(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                OptionDictionary options = new OptionDictionary();

                foreach (string element in list)
                {
                    IOption option = Option.FromString(
                        interpreter, element, appDomain,
                        valueFlags, cultureInfo, ref error);

                    if (option == null)
                        return null;

                    if (options.Has(option))
                    {
                        error = String.Format(
                            "duplicate option name {0}",
                            FormatOps.WrapOrNull(option.Name));

                        return null;
                    }

                    options.Add(option);
                }

                return options;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Add Methods
        /// <summary>
        /// Adds the specified option to the dictionary, using its name as the key.
        /// </summary>
        /// <param name="item">
        /// The option to add to the dictionary.
        /// </param>
        public void Add(
            IOption item
            )
        {
            this.Add(item.Name, item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified option to the dictionary unless an option with the
        /// same name is already present.
        /// </summary>
        /// <param name="item">
        /// The option to add to the dictionary.
        /// </param>
        public void MaybeAdd(
            IOption item
            )
        {
            if (!Has(item)) Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified option to the dictionary, using the specified key,
        /// unless an option with the same name is already present.
        /// </summary>
        /// <param name="name">
        /// The key (option name) to associate with the option.
        /// </param>
        /// <param name="item">
        /// The option to add to the dictionary.
        /// </param>
        public void MaybeAdd(
            string name,
            IOption item
            )
        {
            if (!Has(name)) Add(name, item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the built-in (system) options to the dictionary.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly replace any existing option that has the same name
        /// as a built-in (system) option.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that each built-in (system) option be added without
        /// already being present; otherwise, an existing option with the same name
        /// is left unchanged.
        /// </param>
        private void AddSystemOptions(
            bool force,
            bool strict
            )
        {
            IEnumerable<IOption> collection = CreateSystemOptions();

            if (collection != null)
            {
                foreach (IOption item in collection)
                {
                    if (item == null)
                        continue;

                    if (force)
                        Replace(item);
                    else if (strict)
                        Add(item);
                    else
                        MaybeAdd(item);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Replace Methods
        /// <summary>
        /// Adds the specified option to the dictionary, replacing any existing
        /// option that has the same name.
        /// </summary>
        /// <param name="item">
        /// The option to add to (or replace within) the dictionary.
        /// </param>
        public void Replace(
            IOption item
            )
        {
            this[item.Name] = item;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Has (Is Available) Methods
        /// <summary>
        /// Determines whether an option with the same name as the specified
        /// identifier is present in the dictionary.
        /// </summary>
        /// <param name="item">
        /// The identifier whose name is to be looked up.
        /// </param>
        /// <returns>
        /// True if a matching option is present; otherwise, false.
        /// </returns>
        public bool Has(
            IIdentifierBase item
            )
        {
            return (item != null) ? Has(item.Name) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether an option with the specified name is present in the
        /// dictionary.
        /// </summary>
        /// <param name="name">
        /// The name of the option to look up.
        /// </param>
        /// <returns>
        /// True if a matching option is present; otherwise, false.
        /// </returns>
        public bool Has(
            string name
            )
        {
            return Has(this, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether an option with the specified name is present in the
        /// dictionary, returning the matching option.
        /// </summary>
        /// <param name="name">
        /// The name of the option to look up.
        /// </param>
        /// <param name="option">
        /// Upon success, this parameter will be modified to contain the matching
        /// option.
        /// </param>
        /// <returns>
        /// True if a matching option is present; otherwise, false.
        /// </returns>
        public bool Has(
            string name,
            ref IOption option
            )
        {
            return Has(this, name, ref option);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether an option with the specified name is present in the
        /// specified dictionary.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to look up.
        /// </param>
        /// <returns>
        /// True if a matching option is present; otherwise, false.
        /// </returns>
        public static bool Has(
            OptionDictionary options,
            string name
            )
        {
            IOption option = null;

            return Has(options, name, ref option);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether an option with the specified name is present in the
        /// specified dictionary, returning the matching option.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to look up.
        /// </param>
        /// <param name="option">
        /// Upon success, this parameter will be modified to contain the matching
        /// option.
        /// </param>
        /// <returns>
        /// True if a matching option is present; otherwise, false.
        /// </returns>
        public static bool Has(
            OptionDictionary options,
            string name,
            ref IOption option
            )
        {
            Result error = null;

            return TryResolveSimple(
                options, name, DefaultVerbose, ref option, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option CanBePresent (Is Usable) Methods
        /// <summary>
        /// Determines whether the option with the specified name is available and
        /// may legally be present (e.g. it is not mutually exclusive with another
        /// option that is already present).
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// True if the option may legally be present; otherwise, false.
        /// </returns>
        public bool CanBePresent(
            string name,
            ref Result error
            )
        {
            return CanBePresent(this, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name, in the specified
        /// dictionary, is available and may legally be present (e.g. it is not
        /// mutually exclusive with another option that is already present).
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// True if the option may legally be present; otherwise, false.
        /// </returns>
        public static bool CanBePresent(
            OptionDictionary options,
            string name,
            ref Result error
            )
        {
            IOption option = null;

            if (!TryResolveSimple(
                    options, name, true, ref option, ref error))
            {
                return false;
            }

            return option.CanBePresent(options, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option IsPresent (Is Set) Methods
        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set).
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            string name
            )
        {
            IVariant value = null;
            int index = Index.Invalid;

            return IsPresent(
                this, name, false, DefaultNoCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set), returning its associated value.
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            string name,
            ref IVariant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(
                this, name, true, DefaultNoCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set), returning its associated value and argument index.
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <param name="index">
        /// Upon success, this parameter will be modified to contain the argument
        /// index at which the option was seen.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            string name,
            ref IVariant value,
            ref int index
            )
        {
            return IsPresent(
                this, name, true, DefaultNoCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set).
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            string name,
            bool noCase
            )
        {
            IVariant value = null;
            int index = Index.Invalid;

            return IsPresent(
                this, name, false, noCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set), returning its associated value.
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            string name,
            bool noCase,
            ref IVariant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(
                this, name, true, noCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set), returning its associated value and argument index.
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <param name="index">
        /// Upon success, this parameter will be modified to contain the argument
        /// index at which the option was seen.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public bool IsPresent(
            string name,
            bool noCase,
            ref IVariant value,
            ref int index
            )
        {
            return IsPresent(
                this, name, true, noCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name, in the specified
        /// dictionary, is present (i.e. has been set).
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public static bool IsPresent(
            OptionDictionary options,
            string name,
            bool noCase
            )
        {
            IVariant value = null;
            int index = Index.Invalid;

            return IsPresent(
                options, name, false, noCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name, in the specified
        /// dictionary, is present (i.e. has been set), returning its associated
        /// value.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public static bool IsPresent(
            OptionDictionary options,
            string name,
            bool noCase,
            ref IVariant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(
                options, name, true, noCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name, in the specified
        /// dictionary, is present (i.e. has been set), returning its associated
        /// value and argument index.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <param name="index">
        /// Upon success, this parameter will be modified to contain the argument
        /// index at which the option was seen.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        public static bool IsPresent(
            OptionDictionary options,
            string name,
            bool noCase,
            ref IVariant value,
            ref int index
            )
        {
            return IsPresent(
                options, name, true, noCase, DefaultStrict,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Internal
        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set), without treating a missing option as an error.
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        internal bool CheckPresent(
            string name
            )
        {
            IVariant value = null;
            int index = Index.Invalid;

            return IsPresent(
                this, name, false, DefaultNoCase, false,
                ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the option with the specified name is present (i.e.
        /// has been set), without treating a missing option as an error, returning
        /// its associated value.
        /// </summary>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        internal bool CheckPresent(
            string name,
            ref IVariant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(
                this, name, true, DefaultNoCase, false,
                ref value, ref index);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private
        /// <summary>
        /// Determines whether the option with the specified name, in the specified
        /// dictionary, is present (i.e. has been set), returning its associated
        /// value and argument index.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to check.
        /// </param>
        /// <param name="withValue">
        /// Non-zero if the caller intends to use the value associated with the
        /// option; otherwise, a diagnostic trace is emitted when a value would be
        /// discarded.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <param name="strict">
        /// Non-zero to emit a diagnostic trace when the named option cannot be
        /// found.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the value
        /// associated with the option.
        /// </param>
        /// <param name="index">
        /// Upon success, this parameter will be modified to contain the argument
        /// index at which the option was seen.
        /// </param>
        /// <returns>
        /// True if the option is present; otherwise, false.
        /// </returns>
        private static bool IsPresent(
            OptionDictionary options,
            string name,
            bool withValue,
            bool noCase,
            bool strict,
            ref IVariant value,
            ref int index
            )
        {
            if (options != null)
            {
                if (name != null)
                {
                    if (noCase)
                    {
                        //
                        // HACK: Perform a linear search of the options.  We
                        //       should not need to do this since the options
                        //       are in a dictionary; however, we want to
                        //       preserve the "case-sensitive" semantics unless
                        //       otherwise requested by the caller.
                        //
                        bool found = false;

                        foreach (KeyValuePair<string, IOption> pair in options)
                        {
                            if (SharedStringOps.SystemNoCaseEquals(
                                    pair.Key, 0, name, 0, name.Length))
                            {
                                found = true;

                                IOption option = pair.Value;

                                if ((option != null) &&
                                    option.IsPresent(options, ref value))
                                {
                                    if (!withValue && (value != null))
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "IsPresent: option {0} value " +
                                            "will be discarded by caller",
                                            FormatOps.WrapOrNull(name)),
                                            typeof(OptionDictionary).Name,
                                            TracePriority.CommandDebug2);
                                    }

                                    index = option.Index;
                                    return true;
                                }
                            }
                        }

                        if (strict && !found)
                        {
                            //
                            // NOTE: This should not really happen, issue a
                            //       debug trace.
                            //
                            TraceOps.DebugTrace(String.Format(
                                "IsPresent: {0}",
                                BadOption(options, name, true)),
                                typeof(OptionDictionary).Name,
                                TracePriority.CommandError);
                        }
                    }
                    else
                    {
                        IOption option;

                        if (options.TryGetValue(name, out option))
                        {
                            if ((option != null) &&
                                option.IsPresent(options, ref value))
                            {
                                if (!withValue && (value != null))
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "IsPresent: option {0} value " +
                                        "will be discarded by caller",
                                        FormatOps.WrapOrNull(name)),
                                        typeof(OptionDictionary).Name,
                                        TracePriority.CommandDebug2);
                                }

                                index = option.Index;
                                return true;
                            }
                        }
                        else if (strict)
                        {
                            //
                            // NOTE: This should not really happen, issue a
                            //       debug trace.
                            //
                            TraceOps.DebugTrace(String.Format(
                                "IsPresent: {0}",
                                BadOption(options, name, true)),
                                typeof(OptionDictionary).Name,
                                TracePriority.CommandError);
                        }
                    }
                }
            }

            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Setting Methods
        /// <summary>
        /// Sets the presence, argument index, and value of the option with the
        /// specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the option to modify.
        /// </param>
        /// <param name="present">
        /// Non-zero if the option should be marked as present (i.e. set).
        /// </param>
        /// <param name="index">
        /// The argument index to associate with the option.
        /// </param>
        /// <param name="value">
        /// The value to associate with the option.
        /// </param>
        /// <returns>
        /// True if the option was found and modified; otherwise, false.
        /// </returns>
        public bool SetPresent(
            string name,
            bool present,
            int index,
            IVariant value
            )
        {
            return SetPresent(this, name, present, index, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the presence, argument index, and value of the option with the
        /// specified name, in the specified dictionary.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name of the option to modify.
        /// </param>
        /// <param name="present">
        /// Non-zero if the option should be marked as present (i.e. set).
        /// </param>
        /// <param name="index">
        /// The argument index to associate with the option.
        /// </param>
        /// <param name="value">
        /// The value to associate with the option.
        /// </param>
        /// <returns>
        /// True if the option was found and modified; otherwise, false.
        /// </returns>
        public static bool SetPresent(
            OptionDictionary options,
            string name,
            bool present,
            int index,
            IVariant value
            )
        {
            if (options != null)
            {
                if (name != null)
                {
                    IOption option;

                    if (options.TryGetValue(name, out option))
                    {
                        if (option != null)
                        {
                            option.SetPresent(options, present, index, value);

                            return true;
                        }
                    }
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Lookup Methods
        /// <summary>
        /// Attempts to resolve an option by its exact name within the specified
        /// dictionary.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The exact name of the option to resolve.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include the list of available options in any error message
        /// that is produced.
        /// </param>
        /// <param name="option">
        /// Upon success, this parameter will be modified to contain the resolved
        /// option.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// True if the option was resolved; otherwise, false.
        /// </returns>
        private static bool TryResolveSimple(
            OptionDictionary options,
            string name,
            bool verbose,
            ref IOption option,
            ref Result error
            )
        {
            if (options == null)
            {
                error = "invalid options";
                return false;
            }

            if (name == null)
            {
                error = "invalid option name";
                return false;
            }

            if (!options.TryGetValue(name, out option))
            {
                error = BadOption(verbose ? options : null, name, true);
                return false;
            }

            if (option == null)
            {
                error = String.Format("invalid option \"{0}\"", name);
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to resolve an option by name, supporting unambiguous prefix
        /// matching.
        /// </summary>
        /// <param name="name">
        /// The name (or unambiguous prefix) of the option to resolve.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a non-existent option as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <param name="allowUnsafe">
        /// Non-zero to include options marked as unsafe when building any error
        /// message that is produced.
        /// </param>
        /// <param name="ambiguous">
        /// Upon failure, this parameter will be modified to non-zero if the
        /// specified name matched more than one option.
        /// </param>
        /// <param name="option">
        /// Upon success, this parameter will be modified to contain the resolved
        /// option.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
        /// code.
        /// </returns>
        public ReturnCode TryResolve(
            string name,
            bool strict,
            bool noCase,
            bool allowUnsafe,
            ref bool ambiguous,
            ref IOption option,
            ref Result error
            )
        {
            return TryResolve(
                this, name, strict, noCase, allowUnsafe, ref ambiguous,
                ref option, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to resolve an option by name, within the specified dictionary,
        /// supporting unambiguous prefix matching.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to search.
        /// </param>
        /// <param name="name">
        /// The name (or unambiguous prefix) of the option to resolve.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a non-existent option as an error.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform option name matching in a case-insensitive manner.
        /// </param>
        /// <param name="allowUnsafe">
        /// Non-zero to include options marked as unsafe when building any error
        /// message that is produced.
        /// </param>
        /// <param name="ambiguous">
        /// Upon failure, this parameter will be modified to non-zero if the
        /// specified name matched more than one option.
        /// </param>
        /// <param name="option">
        /// Upon success, this parameter will be modified to contain the resolved
        /// option.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
        /// code.
        /// </returns>
        public static ReturnCode TryResolve(
            OptionDictionary options,
            string name,
            bool strict,
            bool noCase,
            bool allowUnsafe,
            ref bool ambiguous,
            ref IOption option,
            ref Result error
            )
        {
            if (options != null)
            {
                if (name != null)
                {
                    string exactName = null;
                    StringList list = new StringList();

                    foreach (KeyValuePair<string, IOption> pair in options)
                    {
                        string key = pair.Key;
                        IOption value = pair.Value;
                        bool match;

                        if (noCase ||
                            ((value != null) && value.IsNoCase(options)))
                        {
                            match = SharedStringOps.SystemNoCaseEquals(
                                key, 0, name, 0, name.Length);
                        }
                        else
                        {
                            match = SharedStringOps.SystemEquals(
                                key, 0, name, 0, name.Length);
                        }

                        if (match)
                        {
                            //
                            // NOTE: Was the key valid (this should always
                            //       succeed).
                            //
                            if (key != null)
                            {
                                //
                                // NOTE: It was a match; however, was it an
                                //       exact match?
                                //
                                bool exactMatch = (key.Length == name.Length);

                                if (exactMatch)
                                {
                                    //
                                    // NOTE: Preserve match, it may differ
                                    //       in case.
                                    //
                                    exactName = key;
                                }

                                //
                                // NOTE: Was it an exact match or did we
                                //       match at least one character in
                                //       a partial match?
                                //
                                if (exactMatch || (name.Length > 0))
                                {
                                    //
                                    // NOTE: Store exact or partial match
                                    //       in the results dictionary.
                                    //
                                    list.Add(key);
                                }
                            }
                        }
                    }

                    //
                    // NOTE: If there was an exact match, just use it.
                    //
                    if (exactName != null)
                    {
                        //
                        // NOTE: Normal case, an exact option match was
                        //       found.
                        //
                        option = options[exactName];

                        return ReturnCode.Ok;
                    }
                    else if (list.Count == 1)
                    {
                        //
                        // NOTE: Normal case, exactly one option partially
                        //       matched.
                        //
                        option = options[list[0]];

                        return ReturnCode.Ok;
                    }
                    else if (list.Count > 1)
                    {
                        //
                        // NOTE: They specified an ambiguous option.
                        //
                        ambiguous = true;

                        error = AmbiguousOption(
                            options, name, list, allowUnsafe);
                    }
                    else if (strict)
                    {
                        //
                        // NOTE: They specified a non-existent option.
                        //
                        error = BadOption(options, name, allowUnsafe);
                    }
                    else
                    {
                        //
                        // NOTE: Non-strict mode, leave the original option
                        //       value unchanged and let the caller deal
                        //       with it.
                        //
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    error = "invalid option name";
                }
            }
            else
            {
                error = "invalid options";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new option dictionary containing only the options that belong
        /// to the specified categories.
        /// </summary>
        /// <param name="categories">
        /// The categories to include, or null to include all options.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that an option belong to all of the specified
        /// categories; otherwise, belonging to any one of them is sufficient.
        /// </param>
        /// <param name="system">
        /// Non-zero to include the built-in (system) options in the resulting
        /// dictionary.
        /// </param>
        /// <returns>
        /// The newly created, filtered option dictionary.
        /// </returns>
        public OptionDictionary Filter(
            OptionCategory? categories,
            bool all,
            bool system
            )
        {
            return Filter(this, categories, all, system);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new option dictionary containing only the options, from the
        /// specified dictionary, that belong to the specified categories.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to filter.
        /// </param>
        /// <param name="categories">
        /// The categories to include, or null to include all options.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that an option belong to all of the specified
        /// categories; otherwise, belonging to any one of them is sufficient.
        /// </param>
        /// <param name="system">
        /// Non-zero to include the built-in (system) options in the resulting
        /// dictionary.
        /// </param>
        /// <returns>
        /// The newly created, filtered option dictionary, or null if the specified
        /// dictionary was null.
        /// </returns>
        public static OptionDictionary Filter(
            OptionDictionary options,
            OptionCategory? categories,
            bool all,
            bool system
            )
        {
            if (options == null) // NOTE: Garbage in, garbage out.
                return null;

            if (categories == null) // NOTE: Ok, unfiltered.
                return options;

            OptionDictionary result = new OptionDictionary(system);
            OptionCategory localCategories = (OptionCategory)categories;

            foreach (OptionPair pair in options)
            {
                IOption option = pair.Value;

                if (option == null)
                    continue;

                if (!option.HasCategories(localCategories, all))
                    continue;

                result.Add(pair.Key, pair.Value);
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ArgumentList Building Methods
        /// <summary>
        /// Builds an argument list that represents the options, and their values,
        /// that are currently present (i.e. set).
        /// </summary>
        /// <param name="arguments">
        /// Upon success, this parameter will be modified to contain (or have
        /// appended to it) the arguments representing the present options.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
        /// code.
        /// </returns>
        public ReturnCode ToArgumentList(
            ref ArgumentList arguments,
            ref Result error
            )
        {
            return ToArgumentList(this, ref arguments, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds an argument list that represents the options, and their values,
        /// from the specified dictionary that are currently present (i.e. set).
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to process.
        /// </param>
        /// <param name="arguments">
        /// Upon success, this parameter will be modified to contain (or have
        /// appended to it) the arguments representing the present options.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
        /// code.
        /// </returns>
        public static ReturnCode ToArgumentList(
            OptionDictionary options,
            ref ArgumentList arguments,
            ref Result error
            )
        {
            if (options != null)
            {
                if (options.Count > 0)
                {
                    if (arguments == null)
                        arguments = new ArgumentList();

                    IOption endOption = null;

                    foreach (KeyValuePair<string, IOption> pair in options)
                    {
                        IOption option = pair.Value;

                        if (option == null)
                            continue;

                        if (option.IsIgnored(options))
                            continue;

                        //
                        // TODO: Is this a good idea (i.e. simply ignoring
                        //       the list-of-options flag instead of raising
                        //       an error)?
                        //
                        if (option.HasFlags(OptionFlags.ListOfOptions, true))
                            continue;

                        if (!option.HasFlags(OptionFlags.EndOfOptions, true))
                        {
                            IVariant value = null;

                            if (!option.IsPresent(options, ref value))
                                continue;

                            if (!option.CanBePresent(options, ref error))
                                return ReturnCode.Error;

                            arguments.Add(Argument.InternalCreate(option.Name));

                            if (option.MustHaveValue(options))
                                arguments.Add(Argument.InternalCreate(value));
                        }
                        else
                        {
                            //
                            // NOTE: This option must be processed last; however,
                            //       we still need to keep track of it now until
                            //       that time.
                            //
                            endOption = option;
                        }
                    }

                    if ((endOption != null) && !endOption.IsIgnored(options))
                    {
                        IVariant value = null;

                        if (endOption.IsPresent(options, ref value))
                        {
                            if (!endOption.CanBePresent(options, ref error))
                                return ReturnCode.Error;

                            arguments.Add(Argument.InternalCreate(endOption.Name));

                            if (endOption.MustHaveValue(options))
                                arguments.Add(Argument.InternalCreate(value));
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid options";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Message Methods
        /// <summary>
        /// Builds a sorted list of the option names in the specified dictionary,
        /// optionally excluding those marked as unsafe.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to process.
        /// </param>
        /// <param name="allowUnsafe">
        /// Non-zero to include options marked as unsafe.
        /// </param>
        /// <returns>
        /// The sorted list of option names, or null if the specified dictionary was
        /// null.
        /// </returns>
        private static StringSortedList FilterOptions(
            OptionDictionary options,
            bool allowUnsafe
            )
        {
            if (options == null)
                return null;

            StringSortedList dictionary;

            if (allowUnsafe)
            {
                dictionary = new StringSortedList(options.Keys);
            }
            else
            {
                dictionary = new StringSortedList();

                foreach (KeyValuePair<string, IOption> pair in options)
                {
                    string key = pair.Key;

                    if (key == null)
                        continue;

                    IOption value = pair.Value;

                    if ((value == null) || value.IsUnsafe(options))
                        continue;

                    dictionary.Add(key, null);
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a sorted list from the specified collection of option names,
        /// optionally excluding those that name an option marked as unsafe in the
        /// specified dictionary.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options used to validate the option names.
        /// </param>
        /// <param name="collection">
        /// The collection of option names to process.
        /// </param>
        /// <param name="allowUnsafe">
        /// Non-zero to include options marked as unsafe.
        /// </param>
        /// <returns>
        /// The sorted list of option names, or null if the specified dictionary or
        /// collection was null.
        /// </returns>
        private static StringSortedList FilterOptions(
            OptionDictionary options,
            IEnumerable<string> collection,
            bool allowUnsafe
            )
        {
            if ((options == null) || (collection == null))
                return null;

            StringSortedList dictionary;

            if (allowUnsafe)
            {
                dictionary = new StringSortedList(collection);
            }
            else
            {
                dictionary = new StringSortedList();

                foreach (string item in collection)
                {
                    if (item == null)
                        continue;

                    IOption value;

                    if (!options.TryGetValue(item, out value))
                        continue;

                    if ((value == null) || value.IsUnsafe(options))
                        continue;

                    dictionary.Add(item, null);
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a human-readable, English representation of the keys in the
        /// specified dictionary (e.g. a comma-separated list terminated with the
        /// word "or").
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys are to be formatted.
        /// </param>
        /// <returns>
        /// The human-readable, English representation of the dictionary keys.
        /// </returns>
        public static string ToEnglish(
            IDictionary<string, string> dictionary
            )
        {
            return GenericOps<string>.DictionaryToEnglish(
                dictionary, ", ", Characters.SpaceString, "or ");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a message that lists the available options in the specified
        /// dictionary.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options to list.
        /// </param>
        /// <param name="allowUnsafe">
        /// Non-zero to include options marked as unsafe.
        /// </param>
        /// <returns>
        /// The message listing the available options.
        /// </returns>
        public static Result ListOptions(
            OptionDictionary options,
            bool allowUnsafe
            )
        {
            if (options == null)
                return "there are no available options";

            IDictionary<string, string> dictionary = FilterOptions(
                options, allowUnsafe);

            return String.Format(
                "available options are {0}", ToEnglish(dictionary));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds an error message indicating that the specified option name was
        /// ambiguous, listing the options it matched.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options being searched.
        /// </param>
        /// <param name="name">
        /// The ambiguous option name.
        /// </param>
        /// <param name="list">
        /// The list of option names that the specified name matched.
        /// </param>
        /// <param name="allowUnsafe">
        /// Non-zero to include options marked as unsafe.
        /// </param>
        /// <returns>
        /// The error message describing the ambiguous option.
        /// </returns>
        public static Result AmbiguousOption(
            OptionDictionary options,
            string name,
            StringList list,
            bool allowUnsafe
            )
        {
            if ((options == null) || (list == null))
            {
                return String.Format(
                    "ambiguous option \"{0}\"", name); // FIXME: Fallback here?
            }

            IDictionary<string, string> dictionary = FilterOptions(
                options, list, allowUnsafe);

            return String.Format(
                "ambiguous option \"{0}\": must be {1}", name,
                ToEnglish(dictionary));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds an error message indicating that the specified option name was
        /// invalid, listing the available options.
        /// </summary>
        /// <param name="options">
        /// The dictionary of options being searched.
        /// </param>
        /// <param name="name">
        /// The invalid option name.
        /// </param>
        /// <param name="allowUnsafe">
        /// Non-zero to include options marked as unsafe.
        /// </param>
        /// <returns>
        /// The error message describing the invalid option.
        /// </returns>
        public static Result BadOption(
            OptionDictionary options,
            string name,
            bool allowUnsafe
            )
        {
            if (options == null)
            {
                return String.Format(
                    "bad option \"{0}\"", name); // FIXME: Fallback here?
            }

            IDictionary<string, string> dictionary = FilterOptions(
                options, allowUnsafe);

            return String.Format(
                "bad option \"{0}\": must be {1}", name,
                ToEnglish(dictionary));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// Builds a string representation of the option names in the dictionary that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each option name must match in order to be included, or
        /// null to include all option names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform pattern matching in a case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of the matching option names.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Builds a string representation of all the option names in the dictionary.
        /// </summary>
        /// <returns>
        /// The string representation of the option names.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
