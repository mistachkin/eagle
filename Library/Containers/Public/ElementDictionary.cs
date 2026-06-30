/*
 * ElementDictionary.cs --
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
using System.Reflection;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Security.Cryptography;

#if SERIALIZATION
using System.Security.Permissions;
#endif

using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

using VariableFlagsDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Public.VariableFlags>;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<string, object>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<string, object>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class implements a dictionary of array elements, keyed by element
    /// name, that also tracks the per-element variable flags and signals an
    /// associated event when those flags are changed.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("93a6e29b-64cb-418d-9454-928e1dc05245")]
    public sealed class ElementDictionary : SomeDictionary
    {
        #region Private Static Data
#if !MONO
        /// <summary>
        /// This is used to synchronize access to the cached field information
        /// used by the <see cref="GetCapacity" /> method.
        /// </summary>
        private static readonly object syncRoot = new object();
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the default initial capacity used when constructing a new
        /// instance of this class without an explicit capacity.
        /// </summary>
        private static int DefaultCapacity = 0;

        ///////////////////////////////////////////////////////////////////////

#if !MONO && !FAST_DICTIONARY
        /// <summary>
        /// This is the name of the private field, within the base dictionary
        /// class, that holds the hash bucket array used to determine the
        /// current capacity.
        /// </summary>
        private static readonly string BucketsFieldName = "buckets";

        /// <summary>
        /// This holds the cached reflection information for the private hash
        /// bucket field named by <see cref="BucketsFieldName" />.  It is
        /// populated on first use and may be null until then.
        /// </summary>
        private static FieldInfo BucketsFieldInfo = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the event handle to signal when a change in element
        //       flags is detected.
        //
        /// <summary>
        /// This is the event handle to signal when a change in element flags is
        /// detected.
        /// </summary>
        private EventWaitHandle variableEvent;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the per-element variable flags.  When changed, the
        //       associated event will be signaled.
        //
        /// <summary>
        /// These are the per-element variable flags.  When changed, the
        /// associated event will be signaled.
        /// </summary>
        private VariableFlagsDictionary elementFlags;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class using the default initial
        /// capacity.
        /// </summary>
        /// <param name="variableEvent">
        /// The event handle to signal when a change in element flags is
        /// detected.  This value may be null.
        /// </param>
        public ElementDictionary(
            EventWaitHandle variableEvent
            )
            : this(variableEvent, DefaultCapacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class using the specified initial
        /// capacity.
        /// </summary>
        /// <param name="variableEvent">
        /// The event handle to signal when a change in element flags is
        /// detected.  This value may be null.
        /// </param>
        /// <param name="capacity">
        /// The initial capacity for the underlying dictionary.
        /// </param>
        public ElementDictionary(
            EventWaitHandle variableEvent,
            int capacity
            )
            : base(capacity)
        {
            Initialize(variableEvent);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class, copying all entries from
        /// the specified dictionary.
        /// </summary>
        /// <param name="variableEvent">
        /// The event handle to signal when a change in element flags is
        /// detected.  This value may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary whose entries are copied into the new instance.
        /// </param>
        public ElementDictionary(
            EventWaitHandle variableEvent,
            IDictionary dictionary
            )
            : this(variableEvent, DefaultCapacity)
        {
            foreach (DictionaryEntry entry in dictionary)
                this.Add(entry.Key.ToString(), entry.Value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class, copying the entries from
        /// the specified dictionary that match the supplied pattern against
        /// their key and/or value.
        /// </summary>
        /// <param name="variableEvent">
        /// The event handle to signal when a change in element flags is
        /// detected.  This value may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary whose entries are considered for copying into the new
        /// instance.
        /// </param>
        /// <param name="mode">
        /// The matching mode used when comparing entries against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which entries are copied.  If this value
        /// is null, all entries are copied.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="matchKey">
        /// Non-zero if the entry key should be considered when matching against
        /// the pattern.
        /// </param>
        /// <param name="matchValue">
        /// Non-zero if the entry value should be considered when matching
        /// against the pattern.
        /// </param>
        internal ElementDictionary(
            EventWaitHandle variableEvent,
            IDictionary dictionary,
            MatchMode mode,
            string pattern,
            bool noCase,
            bool matchKey,
            bool matchValue
            )
            : this(variableEvent, DefaultCapacity)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                string key = entry.Key.ToString();
                object value = entry.Value;

                if (pattern == null)
                {
                    this.Add(key, value);
                    continue;
                }

                string text;

                if (matchKey)
                {
                    if (matchValue)
                        text = StringList.MakeList(key, value);
                    else
                        text = key;
                }
                else if (matchValue)
                {
                    text = StringOps.GetStringFromObject(value);
                }
                else
                {
                    //
                    // NOTE: Nothing to match against, just skip it.
                    //
                    continue;
                }

                if (StringOps.Match(null, mode, text, pattern, noCase))
                    this.Add(key, value);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a new instance of this class from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the new instance.
        /// </param>
        /// <param name="context">
        /// The streaming context associated with the serialized data.
        /// </param>
        private ElementDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            elementFlags = (VariableFlagsDictionary)info.GetValue(
                "elementFlags", typeof(VariableFlagsDictionary));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method initializes the per-instance state of this class,
        /// including the associated event handle and the per-element variable
        /// flags.
        /// </summary>
        /// <param name="variableEvent">
        /// The event handle to signal when a change in element flags is
        /// detected.  This value may be null.
        /// </param>
        private void Initialize(
            EventWaitHandle variableEvent
            )
        {
            this.variableEvent = variableEvent;
            elementFlags = new VariableFlagsDictionary();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the element with the specified key, optionally
        /// zeroing the underlying string storage associated with its value
        /// first.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine whether string zeroing is
        /// enabled.  This value may be null.
        /// </param>
        /// <param name="key">
        /// The key of the element to remove.  If this value is null, no element
        /// is removed.
        /// </param>
        /// <param name="zero">
        /// Non-zero to zero the underlying string storage associated with the
        /// removed value prior to removing it.
        /// </param>
        /// <returns>
        /// True if the element was removed; otherwise, false.
        /// </returns>
        internal bool ResetValue(
            Interpreter interpreter,
            string key,
            bool zero
            )
        {
#if !MONO && NATIVE && WINDOWS
            object value;

            if (zero && (key != null) && TryGetValue(key, out value))
            {
                if ((value is string) && (interpreter != null) &&
                    interpreter.HasZeroString())
                {
                    /* IGNORED */
                    StringOps.ZeroStringOrTrace((string)value);
                }
            }
#endif

            if (key == null)
                return false;

            return Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all elements from this dictionary, optionally
        /// zeroing the underlying string storage associated with their values
        /// first.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine whether string zeroing is
        /// enabled.  This value may be null.
        /// </param>
        /// <param name="zero">
        /// Non-zero to zero the underlying string storage associated with each
        /// value prior to clearing the dictionary.
        /// </param>
        internal void ResetValue(
            Interpreter interpreter,
            bool zero
            )
        {
#if !MONO && NATIVE && WINDOWS
            if (zero && (interpreter != null) && interpreter.HasZeroString())
            {
                foreach (KeyValuePair<string, object> pair in this)
                {
                    object value = pair.Value;

                    if (value is string)
                    {
                        /* IGNORED */
                        StringOps.ZeroStringOrTrace((string)value);
                    }
                    else if (value is Argument)
                    {
                        ((Argument)value).ResetValue(interpreter, zero);
                    }
                    else if (value is Result)
                    {
                        ((Result)value).ResetValue(interpreter, zero);
                    }
                }
            }
#endif

            Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        //
        // NOTE: This is the default value for this array, per TIP #508.  It
        //       is managed via the [array default] sub-command and consulted
        //       when attempting to read any nonexistent elements.
        //
        /// <summary>
        /// This is the default value for this array, per TIP #508.  It is
        /// managed via the <c>[array default]</c> sub-command and consulted
        /// when attempting to read any nonexistent elements.
        /// </summary>
        private object defaultValue = null;

        /// <summary>
        /// Gets or sets the default value for this array, per TIP #508.  This
        /// value is consulted when attempting to read any nonexistent elements.
        /// </summary>
        public object DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether an element with the specified key is
        /// present, returning the configured default value when it is not.
        /// </summary>
        /// <param name="key">
        /// The key to locate.
        /// </param>
        /// <param name="defaultValue">
        /// Upon success, this is set to null.  Upon failure, this is set to the
        /// configured default value for this array.
        /// </param>
        /// <returns>
        /// True if an element with the specified key is present; otherwise,
        /// false.
        /// </returns>
        public bool ContainsKey(
            string key,
            out object defaultValue
            )
        {
            if (this.ContainsKey(key))
            {
                defaultValue = null;
                return true;
            }
            else
            {
                defaultValue = this.defaultValue;
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the variable flags associated with the element
        /// having the specified key.
        /// </summary>
        /// <param name="key">
        /// The key of the element whose variable flags are queried.
        /// </param>
        /// <param name="default">
        /// The variable flags to return when the specified key is null or has
        /// no associated flags.
        /// </param>
        /// <returns>
        /// The variable flags associated with the specified key, or the
        /// supplied default value when no flags are associated with it.
        /// </returns>
        public VariableFlags GetFlags(
            string key,
            VariableFlags @default
            )
        {
            if ((key != null) && (elementFlags != null))
            {
                VariableFlags value;

                if (elementFlags.TryGetValue(key, out value))
                    return value;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the element having the specified key
        /// has the specified variable flags set.
        /// </summary>
        /// <param name="key">
        /// The key of the element whose variable flags are queried.
        /// </param>
        /// <param name="default">
        /// The variable flags to assume when the specified key is null or has
        /// no associated flags.
        /// </param>
        /// <param name="hasFlags">
        /// The variable flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be set; zero to
        /// require that any of them be set.
        /// </param>
        /// <returns>
        /// True if the specified flags are set according to the value of the
        /// <paramref name="all" /> parameter; otherwise, false.
        /// </returns>
        public bool HasFlags(
            string key,
            VariableFlags @default,
            VariableFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(
                GetFlags(key, @default), hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds or removes the specified variable flags for the
        /// element having the specified key, optionally creating an entry for
        /// the key when one does not already exist, and optionally signaling
        /// the associated event when the flags are changed.
        /// </summary>
        /// <param name="key">
        /// The key of the element whose variable flags are changed.
        /// </param>
        /// <param name="initialValue">
        /// The initial variable flags to use when creating a new entry for the
        /// specified key.
        /// </param>
        /// <param name="changeValue">
        /// The variable flags to add or remove.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an entry for the specified key when one does not
        /// already exist.
        /// </param>
        /// <param name="add">
        /// Non-zero to add the specified flags; zero to remove them.
        /// </param>
        /// <param name="notify">
        /// Non-zero on input to signal the associated event when the flags are
        /// changed.  Upon return, this is set to false if the event was
        /// signaled.
        /// </param>
        /// <returns>
        /// True if the variable flags were changed; otherwise, false.
        /// </returns>
        public bool ChangeFlags(
            string key,
            VariableFlags initialValue,
            VariableFlags changeValue,
            bool create,
            bool add,
            ref bool notify
            )
        {
            if ((key != null) && (elementFlags != null))
            {
                VariableFlags oldValue;
                VariableFlags newValue;

                if (elementFlags.TryGetValue(key, out oldValue))
                {
                    if (add)
                    {
                        newValue = oldValue | changeValue;

                        elementFlags[key] = newValue;

                        if (notify && EntityOps.OnFlagsChanged(
                                variableEvent, oldValue, newValue))
                        {
                            notify = false;
                        }

                        return true;
                    }
                    else
                    {
                        newValue = oldValue & ~changeValue;

                        if (newValue != VariableFlags.None)
                        {
                            elementFlags[key] = newValue;

                            if (notify && EntityOps.OnFlagsChanged(
                                    variableEvent, oldValue, newValue))
                            {
                                notify = false;
                            }

                            return true;
                        }

                        if (notify && EntityOps.OnFlagsChanged(
                                variableEvent, oldValue, newValue))
                        {
                            notify = false;
                        }

                        return elementFlags.Remove(key);
                    }
                }
                else if (create)
                {
                    newValue = add ? (initialValue | changeValue) :
                        VariableFlags.None;

                    elementFlags.Add(key, newValue);

                    if (notify && EntityOps.OnFlagsChanged(
                            variableEvent, oldValue, newValue))
                    {
                        notify = false;
                    }

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all entries from the specified dictionary to this
        /// dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries are added.
        /// </param>
        public void Add(
            IDictionary<string, object> dictionary
            )
        {
            foreach (KeyValuePair<string, object> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects and returns the key of a random element from
        /// this dictionary, using the supplied source of entropy.
        /// </summary>
        /// <param name="provideEntropy">
        /// The source of entropy to use when selecting a random element.  If
        /// this value is null, the random number generator is used instead.
        /// </param>
        /// <param name="randomNumberGenerator">
        /// The random number generator to use when selecting a random element.
        /// This value is used only when the entropy source is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this is set to an error message that describes why a
        /// random element could not be selected.
        /// </param>
        /// <returns>
        /// The key of the randomly selected element, or null if a random
        /// element could not be selected.
        /// </returns>
        public string GetRandom(
            IProvideEntropy provideEntropy,              /* in */
            RandomNumberGenerator randomNumberGenerator, /* in */
            ref Result error                             /* out */
            )
        {
            if (this.Count == 0)
            {
                error = "no elements in array";
                return null;
            }

            byte[] bytes;

            if (provideEntropy != null)
            {
                bytes = new byte[sizeof(int)];

                /* NO RESULT */
                provideEntropy.GetBytes(ref bytes);
            }
            else if (randomNumberGenerator != null)
            {
                bytes = new byte[sizeof(int)];

                /* NO RESULT */
                randomNumberGenerator.GetBytes(bytes);
            }
            else
            {
                error = "random number generator not available";
                return null;
            }

            int index = BitConverter.ToInt32(bytes, 0);

            //
            // FIXME: *PERF* This is really bad for performance.
            //
            StringList keys = new StringList(this.Keys);

            return keys[Math.Abs(index) % this.Count];
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For interactive introspection use via the default host only.
        //       Do not rely on this method in other code.
        //
        /// <summary>
        /// This method returns the current capacity of the underlying
        /// dictionary.  It is intended for interactive introspection use via
        /// the default host only and should not be relied upon in other code.
        /// </summary>
        /// <returns>
        /// The current capacity of the underlying dictionary, or the default
        /// capacity when it cannot be determined.
        /// </returns>
        public int GetCapacity()
        {
#if !MONO && !FAST_DICTIONARY
            if (!CommonOps.Runtime.IsMono())
            {
                lock (syncRoot)
                {
                    if (BucketsFieldInfo == null)
                    {
                        BucketsFieldInfo = typeof(Dictionary<string, object>).
                            GetField(BucketsFieldName, ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PrivateInstanceGetField, true));
                    }

                    if (BucketsFieldInfo != null)
                    {
                        int[] buckets = BucketsFieldInfo.GetValue(this) as int[];

                        if (buckets != null)
                            return buckets.Length;
                    }
                }
            }
#endif

            return DefaultCapacity;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method returns a space-separated string containing the keys of
        /// the elements whose keys match the specified pattern.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used when comparing keys against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is
        /// regular expression based.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching keys.
        /// </returns>
        public string KeysToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, false, mode, pattern, null, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string containing all of the element keys,
        /// joined by the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The separator used between adjacent keys.
        /// </param>
        /// <returns>
        /// A string containing the element keys joined by the specified
        /// separator.
        /// </returns>
        public string KeysToString(
            string separator
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the element
        /// keys that match the specified glob pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to select which keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching keys.
        /// </returns>
        public string KeysToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the element
        /// keys that match the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which keys are
        /// included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching keys.
        /// </returns>
        public string KeysToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the values
        /// of the elements whose values match the specified pattern.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used when comparing values against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which values are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is
        /// regular expression based.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching values.
        /// </returns>
        public string ValuesToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, false, true, mode, null, pattern, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the element
        /// values that match the specified glob pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to select which values are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching values.
        /// </returns>
        public string ValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the element
        /// values that match the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which values are
        /// included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching values.
        /// </returns>
        public string ValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the keys and
        /// values of the elements that match the specified glob pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to select which elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching keys and values.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the keys and
        /// values of the elements that match the specified regular expression
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which elements are
        /// included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching keys and values.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated string containing the element
        /// keys that match the specified glob pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to select which keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A space-separated string containing the matching keys.
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

        #region System.Runtime.Serialization.ISerializable Overrides
#if SERIALIZATION
        /// <summary>
        /// This method populates the specified serialization information with
        /// the data needed to serialize this instance, including the
        /// per-element variable flags.
        /// </summary>
        /// <param name="info">
        /// The object that receives the serialized data for this instance.
        /// </param>
        /// <param name="context">
        /// The streaming context associated with the serialized data.
        /// </param>
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
            )
        {
            info.AddValue("elementFlags", elementFlags);

            base.GetObjectData(info, context);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a space-separated string containing all of the
        /// element keys.
        /// </summary>
        /// <returns>
        /// A space-separated string containing all of the element keys.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
