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

using VariableFlagsDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Public.VariableFlags>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("93a6e29b-64cb-418d-9454-928e1dc05245")]
    public sealed class ElementDictionary : Dictionary<string, object>
    {
        #region Private Static Data
#if !MONO
        private static readonly object syncRoot = new object();
#endif

        ///////////////////////////////////////////////////////////////////////

        private static int DefaultCapacity = 0;

        ///////////////////////////////////////////////////////////////////////

#if !MONO
        private static readonly string BucketsFieldName = "buckets";
        private static FieldInfo BucketsFieldInfo = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the event handle to signal when a change in element
        //       flags is detected.
        //
        private EventWaitHandle variableEvent;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the per-element variable flags.  When changed, the
        //       associated event will be signaled.
        //
        private VariableFlagsDictionary elementFlags;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ElementDictionary(
            EventWaitHandle variableEvent
            )
            : this(variableEvent, DefaultCapacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ElementDictionary(
            EventWaitHandle variableEvent,
            int capacity
            )
            : base(capacity)
        {
            Initialize(variableEvent);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void Initialize(
            EventWaitHandle variableEvent
            )
        {
            this.variableEvent = variableEvent;
            elementFlags = new VariableFlagsDictionary();
        }

        ///////////////////////////////////////////////////////////////////////

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
                    ReturnCode zeroCode;
                    bool zeroNoComplain = false;
                    Result zeroError = null;

                    zeroCode = StringOps.ZeroString(
                        (string)value, ref zeroNoComplain, ref zeroError);

                    if (!zeroNoComplain && (zeroCode != ReturnCode.Ok))
                        DebugOps.Complain(interpreter, zeroCode, zeroError);
                }
            }
#endif

            if (key == null)
                return false;

            return Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

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
                        ReturnCode zeroCode;
                        bool zeroNoComplain = false;
                        Result zeroError = null;

                        zeroCode = StringOps.ZeroString(
                            (string)value, ref zeroNoComplain, ref zeroError);

                        if (!zeroNoComplain && (zeroCode != ReturnCode.Ok))
                            DebugOps.Complain(interpreter, zeroCode, zeroError);
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
        private object defaultValue = null;
        public object DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        public void Add(
            IDictionary<string, object> dictionary
            )
        {
            foreach (KeyValuePair<string, object> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetRandom(
            RandomNumberGenerator rng, /* in */
            ref Result error           /* out */
            )
        {
            if (rng == null)
            {
                error = "random number generator not available";
                return null;
            }

            if (this.Count == 0)
            {
                error = "no elements in array";
                return null;
            }

            byte[] bytes = new byte[sizeof(int)];

            rng.GetBytes(bytes);

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
        public int GetCapacity()
        {
#if !MONO
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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public string KeysToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Overrides
#if SERIALIZATION
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
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
