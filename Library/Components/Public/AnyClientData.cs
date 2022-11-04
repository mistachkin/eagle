/*
 * AnyClientData.cs --
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
using System.Reflection;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using AnyDictionary = System.Collections.Generic.Dictionary<string, object>;
using AnyDictionaryPair = System.Collections.Generic.KeyValuePair<string, object>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    [ObjectId("04cac3f9-049c-42b6-9446-084d2296c7da")]
    public class AnyClientData :
            ClientData, IHaveClientData, IHaveCultureInfo, IHaveInterpreter,
            IAnyClientData, ICloneable, IMaybeDisposed, IDisposable
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static bool DefaultOverwrite = true;
        private static bool DefaultCreate = true;
        private static bool DefaultToString = true;
        private static bool DefaultEmpty = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private object savedSyncRoot;
        private object syncRoot = new object();
        private IAnyClientData attached;
        private AnyDictionary dictionary;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        //
        // WARNING: For use by the Clone method only.
        //
        private AnyClientData(
            Interpreter interpreter,  /* in: OPTIONAL */
            IClientData clientData,   /* in */
            CultureInfo cultureInfo,  /* in: OPTIONAL */
            AnyDictionary dictionary, /* in */
            object data,              /* in */
            bool readOnly             /* in */
            )
            : base(data, readOnly)
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                this.interpreter = interpreter;
                this.clientData = clientData;
                this.cultureInfo = cultureInfo;
                this.dictionary = dictionary;
            }

            ///////////////////////////////////////////////////////////////////

            MaybeInitialize(null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public AnyClientData()
            : base()
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyClientData(
            object data /* in */
            )
            : base(data)
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyClientData(
            object data,  /* in */
            bool readOnly /* in */
            )
            : base(data, readOnly)
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyClientData(
            IClientData clientData, /* in */
            bool readOnly           /* in */
            )
            : base((clientData != null) ? clientData.Data : null, readOnly)
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyClientData(
            IAnyClientData anyClientData, /* in */
            bool readOnly                 /* in */
            )
            : base((anyClientData != null) ? anyClientData.Data : null, readOnly)
        {
            MaybeInitialize(anyClientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static AnyDictionary NewDictionary()
        {
            return new AnyDictionary();
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetStringFromObject(
            object @object /* in */
            )
        {
            return StringOps.GetStringFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsValid(
            IAnyClientData anyClientData /* in */
            )
        {
            if (anyClientData == null)
                return false;

            if (anyClientData.Disposed)
                return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private AnyDictionary GetDictionary()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return dictionary;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int MaybeResetData()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int count = 0;

                if (base.Data != null)
                {
                    count++;
                    base.Data = null;
                }

                return count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int MaybeClearAndResetDictionary()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int count = 0;

                if (dictionary != null)
                {
                    count += dictionary.Count;

                    dictionary.Clear();
                    dictionary = null;
                }

                return count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private AnyDictionary CopyOrNullDictionary()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return (dictionary != null) ?
                    new AnyDictionary(dictionary) : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private AnyDictionary CopyOrNewDictionary()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return (dictionary != null) ?
                    new AnyDictionary(dictionary) : NewDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybeInitialize(
            IAnyClientData anyClientData /* in: OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (dictionary != null)
                    return;

                AnyClientData localAnyClientData =
                    anyClientData as AnyClientData;

                dictionary = (localAnyClientData != null) ?
                    localAnyClientData.CopyOrNewDictionary() :
                    NewDictionary();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return clientData;
                }
            }
            set
            {
                CheckDisposed();
                CheckReadOnly();

                lock (syncRoot)
                {
                    clientData = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCultureInfo Members
        private CultureInfo cultureInfo;
        public CultureInfo CultureInfo
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return cultureInfo;
                }
            }
            set
            {
                CheckDisposed();
                CheckReadOnly();

                lock (syncRoot)
                {
                    cultureInfo = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return interpreter;
                }
            }
            set
            {
                CheckDisposed();
                CheckReadOnly();

                lock (syncRoot)
                {
                    interpreter = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        public object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void TryLock(
            ref bool locked /* out */
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        public void TryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(
                syncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void TryLock(
            int timeout,    /* in */
            ref bool locked /* out */
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void ExitLock(
            ref bool locked /* in, out */
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyDataBase Members
        public virtual bool TryResetAny(
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached != null)
                {
                    return attached.TryResetAny(
                        ref error);
                }

                AnyDictionary dictionary = GetDictionary();

                if (dictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                dictionary.Clear();
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryHasAny(
            string name,     /* in */
            ref bool hasAny, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached != null)
                {
                    return attached.TryHasAny(
                        name, ref hasAny, ref error);
                }

                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                AnyDictionary dictionary = GetDictionary();

                if (dictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                hasAny = dictionary.ContainsKey(name);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryListAny(
            string pattern,         /* in */
            bool noCase,            /* in */
            ref IList<string> list, /* out */
            ref Result error        /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached != null)
                {
                    return attached.TryListAny(
                        pattern, noCase, ref list, ref error);
                }

                AnyDictionary dictionary = GetDictionary();

                if (dictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                StringList localList = new StringList();

                if (GenericOps<string>.FilterList(
                        new StringList(dictionary.Keys), localList,
                        Index.Invalid, Index.Invalid, ToStringFlags.None,
                        pattern, noCase, ref error) != ReturnCode.Ok)
                {
                    return false;
                }

                list = localList;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetAny(
            string name,      /* in */
            out object value, /* out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached != null)
                {
                    return attached.TryGetAny(
                        name, out value, ref error);
                }

                value = null;

                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                AnyDictionary dictionary = GetDictionary();

                if (dictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                if (!dictionary.TryGetValue(name, out value))
                {
                    error = "datum not present";
                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TrySetAny(
            string name,     /* in */
            object value,    /* in */
            bool overwrite,  /* in */
            bool create,     /* in */
            bool toString,   /* in: NOT USED */
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached != null)
                {
                    return attached.TrySetAny(
                        name, value, overwrite, create,
                        toString, ref error);
                }

                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                AnyDictionary dictionary = GetDictionary();

                if (dictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                if (dictionary.ContainsKey(name))
                {
                    if (!overwrite)
                    {
                        error = "datum already present";
                        return false;
                    }
                }
                else
                {
                    if (!create)
                    {
                        error = "datum not present";
                        return false;
                    }
                }

                dictionary[name] = value;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryUnsetAny(
            string name,     /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached != null)
                {
                    return attached.TryUnsetAny(
                        name, ref error);
                }

                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                AnyDictionary dictionary = GetDictionary();

                if (dictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                if (!dictionary.Remove(name))
                {
                    error = "datum not removed";
                    return false;
                }

                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyData Members
        public bool TryResetAny()
        {
            CheckDisposed();
            CheckReadOnly();

            Result error = null;

            return TryResetAny(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasAny(
            string name /* in */
            )
        {
            CheckDisposed();

            bool hasAny = false;
            Result error = null;

            if (!TryHasAny(name, ref hasAny, ref error))
                return false;

            return hasAny;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryGetAny(
            string name,     /* in */
            out object value /* out */
            )
        {
            CheckDisposed();

            Result error = null;

            return TryGetAny(name, out value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TrySetAny(
            string name, /* in */
            object value /* in */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            Result error = null;

            return TrySetAny(
                name, value, DefaultOverwrite, DefaultCreate,
                DefaultToString, ref  error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryUnsetAny(
            string name /* in */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            Result error = null;

            return TryUnsetAny(name, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyValueTypeData Members
        public virtual bool TryGetBoolean(
            string name,     /* in */
            bool toString,   /* in */
            out bool value,  /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(bool);
                return false;
            }

            if (@object is bool)
            {
                value = (bool)@object;
                return true;
            }

            if (!toString)
            {
                value = default(bool);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(bool));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            bool localValue = default(bool);

            if (Value.GetBoolean2(stringValue,
                    ValueFlags.AnyBoolean, localCultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(bool);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetNullableBoolean(
            string name,     /* in */
            bool toString,   /* in */
            out bool? value, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is bool?)
            {
                value = (bool?)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(bool?));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            bool? localValue = null;

            if (Value.GetNullableBoolean2(stringValue,
                    ValueFlags.AnyBoolean, localCultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetSignedByte(
            string name,     /* in */
            bool toString,   /* in */
            out sbyte value, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(sbyte);
                return false;
            }

            if (@object is sbyte)
            {
                value = (sbyte)@object;
                return true;
            }

            if (!toString)
            {
                value = default(sbyte);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(sbyte));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            sbyte localValue = default(sbyte);

            if (Value.GetSignedByte2(stringValue,
                    ValueFlags.AnyByte | ValueFlags.Signed,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(sbyte);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetByte(
            string name,     /* in */
            bool toString,   /* in */
            out byte value,  /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(byte);
                return false;
            }

            if (@object is byte)
            {
                value = (byte)@object;
                return true;
            }

            if (!toString)
            {
                value = default(byte);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(byte));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            byte localValue = default(byte);

            if (Value.GetByte2(
                    stringValue, ValueFlags.AnyByte, localCultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(byte);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetNarrowInteger(
            string name,     /* in */
            bool toString,   /* in */
            out short value, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(short);
                return false;
            }

            if (@object is short)
            {
                value = (short)@object;
                return true;
            }

            if (!toString)
            {
                value = default(short);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(short));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            short localValue = default(short);

            if (Value.GetNarrowInteger2(stringValue,
                    ValueFlags.AnyNarrowInteger,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(short);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetUnsignedNarrowInteger(
            string name,      /* in */
            bool toString,    /* in */
            out ushort value, /* out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(ushort);
                return false;
            }

            if (@object is ushort)
            {
                value = (ushort)@object;
                return true;
            }

            if (!toString)
            {
                value = default(ushort);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(ushort));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            ushort localValue = default(ushort);

            if (Value.GetUnsignedNarrowInteger2(stringValue,
                    ValueFlags.AnyNarrowInteger | ValueFlags.Unsigned,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(ushort);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetCharacter(
            string name,     /* in */
            bool toString,   /* in */
            out char value,  /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(char);
                return false;
            }

            if (@object is char)
            {
                value = (char)@object;
                return true;
            }

            if (!toString)
            {
                value = default(char);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(char));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            char localValue = default(char);

            if (Value.GetCharacter2(stringValue,
                    ValueFlags.AnyCharacter,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(char);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetInteger(
            string name,     /* in */
            bool toString,   /* in */
            out int value,   /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(int);
                return false;
            }

            if (@object is int)
            {
                value = (int)@object;
                return true;
            }

            if (!toString)
            {
                value = default(int);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(int));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            int localValue = default(int);

            if (Value.GetInteger2(stringValue,
                    ValueFlags.AnyInteger,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(int);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetUnsignedInteger(
            string name,     /* in */
            bool toString,   /* in */
            out uint value,  /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(uint);
                return false;
            }

            if (@object is uint)
            {
                value = (uint)@object;
                return true;
            }

            if (!toString)
            {
                value = default(uint);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(uint));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            uint localValue = default(uint);

            if (Value.GetUnsignedInteger2(stringValue,
                    ValueFlags.AnyInteger | ValueFlags.Unsigned,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(uint);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetWideInteger(
            string name,     /* in */
            bool toString,   /* in */
            out long value,  /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(long);
                return false;
            }

            if (@object is long)
            {
                value = (long)@object;
                return true;
            }

            if (!toString)
            {
                value = default(long);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(long));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            long localValue = default(long);

            if (Value.GetWideInteger2(stringValue,
                    ValueFlags.AnyWideInteger,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(long);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetUnsignedWideInteger(
            string name,     /* in */
            bool toString,   /* in */
            out ulong value, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(ulong);
                return false;
            }

            if (@object is ulong)
            {
                value = (ulong)@object;
                return true;
            }

            if (!toString)
            {
                value = default(ulong);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(ulong));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            ulong localValue = default(ulong);

            if (Value.GetUnsignedWideInteger2(stringValue,
                    ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(ulong);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetDecimal(
            string name,       /* in */
            bool toString,     /* in */
            out decimal value, /* out */
            ref Result error   /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(decimal);
                return false;
            }

            if (@object is decimal)
            {
                value = (decimal)@object;
                return true;
            }

            if (!toString)
            {
                value = default(decimal);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(decimal));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            decimal localValue = default(decimal);

            if (Value.GetDecimal(stringValue,
                    ValueFlags.AnyDecimal,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(decimal);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetSingle(
            string name,     /* in */
            bool toString,   /* in */
            out float value, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(float);
                return false;
            }

            if (@object is float)
            {
                value = (float)@object;
                return true;
            }

            if (!toString)
            {
                value = default(float);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(float));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            float localValue = default(float);

            if (Value.GetSingle(stringValue,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(float);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetDouble(
            string name,      /* in */
            bool toString,    /* in */
            out double value, /* out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(double);
                return false;
            }

            if (@object is double)
            {
                value = (double)@object;
                return true;
            }

            if (!toString)
            {
                value = default(double);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(double));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            double localValue = default(double);

            if (Value.GetDouble(stringValue,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(float);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetDateTime(
            string name,           /* in */
            string format,         /* in */
            DateTimeKind kind,     /* in */
            DateTimeStyles styles, /* in */
            bool toString,         /* in */
            out DateTime value,    /* out */
            ref Result error       /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(DateTime);
                return false;
            }

            if (@object is DateTime)
            {
                value = (DateTime)@object;
                return true;
            }

            if (!toString)
            {
                value = default(DateTime);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(DateTime));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            DateTime localValue = default(DateTime);

            if (Value.GetDateTime2(stringValue,
                    format, ValueFlags.AnyDateTime, kind,
                    styles, localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(DateTime);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetTimeSpan(
            string name,        /* in */
            bool toString,      /* in */
            out TimeSpan value, /* out */
            ref Result error    /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(TimeSpan);
                return false;
            }

            if (@object is TimeSpan)
            {
                value = (TimeSpan)@object;
                return true;
            }

            if (!toString)
            {
                value = default(TimeSpan);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(TimeSpan));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            TimeSpan localValue = default(TimeSpan);

            if (Value.GetTimeSpan2(stringValue,
                    ValueFlags.AnyTimeSpan,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(TimeSpan);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetEnum(
            Interpreter interpreter, /* in: OPTIONAL */
            string name,             /* in */
            Type enumType,           /* in */
            bool toString,           /* in */
            out Enum value,          /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object == null)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(enumType));

                return false;
            }

            if (MarshalOps.IsSameType(@object.GetType(), enumType))
            {
                value = (Enum)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(enumType));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            object enumValue;

            if (EnumOps.IsFlags(enumType))
            {
                enumValue = EnumOps.TryParseFlags(
                    interpreter, enumType, null,
                    stringValue, localCultureInfo,
                    true, true, true, ref error);
            }
            else
            {
                enumValue = EnumOps.TryParse(
                    enumType, stringValue, true,
                    true, ref error);
            }

            if (!(enumValue is Enum))
            {
                value = null;
                return false;
            }

            value = (Enum)enumValue;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTypeData Members
        public virtual bool TryGetClientData(
            string name,           /* in */
            out IClientData value, /* out */
            ref Result error       /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (!(@object is IClientData))
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(IClientData));

                return false;
            }

            value = (IClientData)@object;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetString(
            string name,      /* in */
            bool toString,    /* in */
            out string value, /* out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is string)
            {
                value = (string)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(string));

                return false;
            }

            value = GetStringFromObject(@object);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetStringList(
            Interpreter interpreter, /* in: OPTIONAL */
            string name,             /* in */
            bool toString,           /* in */
            out StringList value,    /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is StringList)
            {
                value = (StringList)@object;
                return true;
            }

            string stringValue;

            if (@object is string)
            {
                stringValue = (string)@object;
            }
            else if (toString)
            {
                stringValue = GetStringFromObject(@object);
            }
            else
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(string));

                return false;
            }

            StringList listValue = null;

            if (ParserOps<string>.SplitList(
                    interpreter, stringValue, 0, Length.Invalid,
                    false, ref listValue, ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = listValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetGuid(
            string name,     /* in */
            bool toString,   /* in */
            out Guid value,  /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = default(Guid);
                return false;
            }

            if (@object is Guid)
            {
                value = (Guid)@object;
                return true;
            }

            if (!toString)
            {
                value = default(Guid);

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(Guid));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            Guid localValue = default(Guid);

            if (Value.GetGuid(stringValue,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(Guid);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetUri(
            string name,     /* in */
            UriKind uriKind, /* in */
            bool toString,   /* in */
            out Uri value,   /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is Uri)
            {
                value = (Uri)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(Uri));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            Uri localValue = null;

            if (Value.GetUri(stringValue,
                    uriKind, localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetVersion(
            string name,       /* in */
            bool toString,     /* in */
            out Version value, /* out */
            ref Result error   /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is Version)
            {
                value = (Version)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(Version));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            Version localValue = null;

            if (Value.GetVersion(stringValue,
                    localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetInterpreter(
            Interpreter interpreter, /* in: OPTIONAL */
            string name,             /* in */
            bool toString,           /* in */
            out Interpreter value,   /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is Interpreter)
            {
                value = (Interpreter)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(Interpreter));

                return false;
            }

            string stringValue = GetStringFromObject(@object);
            Interpreter localValue = null;

            if (Value.GetInterpreter(
                    interpreter, stringValue, InterpreterType.Default,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetPlugin(
            Interpreter interpreter, /* in */
            string name,             /* in */
            bool toString,           /* in */
            out IPlugin value,       /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is IPlugin)
            {
                value = (IPlugin)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(IPlugin));

                return false;
            }

            if (interpreter == null)
            {
                value = null;
                error = "invalid interpreter";

                return false;
            }

            string stringValue = GetStringFromObject(@object);
            AssemblyName assemblyName = null;

            try
            {
                assemblyName = new AssemblyName(stringValue); /* throw */
            }
            catch (Exception e)
            {
                value = null;
                error = e;

                return false;
            }

            IPlugin localValue = interpreter.FindPlugin(
                AppDomainOps.GetCurrent(), MatchMode.Exact,
                assemblyName.Name, assemblyName.Version,
                assemblyName.GetPublicKeyToken(), false,
                ref error);

            value = localValue;
            return (localValue != null);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetObject(
            Interpreter interpreter, /* in */
            string name,             /* in */
            bool toString,           /* in */
            out IObject value,       /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is IObject)
            {
                value = (IObject)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(IObject));

                return false;
            }

            if (interpreter == null)
            {
                value = null;
                error = "invalid interpreter";

                return false;
            }

            string stringValue = GetStringFromObject(@object);
            IObject localValue = null;

            if (interpreter.GetObject(
                    stringValue, LookupFlags.Default, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetEncoding(
            Interpreter interpreter, /* in: OPTIONAL */
            string name,             /* in */
            bool toString,           /* in */
            out Encoding value,      /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is Encoding)
            {
                value = (Encoding)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(Encoding));

                return false;
            }

            string stringValue = GetStringFromObject(@object);
            Encoding localValue = null;

            if (interpreter != null)
            {
                if (interpreter.GetEncoding(stringValue,
                        LookupFlags.Default, ref localValue,
                        ref error) != ReturnCode.Ok)
                {
                    value = null;
                    return false;
                }
            }
            else
            {
                localValue = StringOps.GetEncoding(
                    stringValue, ref error);

                if (localValue == null)
                {
                    value = null;
                    return false;
                }
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetByteArray(
            string name,      /* in */
            bool toString,    /* in */
            out byte[] value, /* out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is byte[])
            {
                value = (byte[])@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(byte[]));

                return false;
            }

            CultureInfo localCultureInfo;

            lock (syncRoot)
            {
                localCultureInfo = cultureInfo;
            }

            string stringValue = GetStringFromObject(@object);
            byte[] localValue = null;

            if (StringOps.GetBytesFromString(
                    stringValue, localCultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyClientData Members
        public IAnyClientData Attached
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return attached;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public IAnyClientData Root
        {
            get
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    IAnyClientData thisClientData = this;
                    IAnyClientData linkClientData = attached;

                    while (IsValid(linkClientData))
                    {
                        thisClientData = linkClientData;
                        linkClientData = linkClientData.Attached;
                    }

                    return thisClientData;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AttachTo(
            IAnyClientData anyClientData /* in */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached != null)
                    return false;

                if (anyClientData == null)
                    return false;

                if (AppDomainOps.IsTransparentProxy(anyClientData))
                    return false;

                if (Object.ReferenceEquals(anyClientData, this))
                    return false;

                savedSyncRoot = syncRoot;
                syncRoot = anyClientData.SyncRoot;
                attached = anyClientData;

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool DetachFrom(
            IAnyClientData anyClientData /* in */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (attached == null)
                    return false;

                if (anyClientData == null)
                    return false;

                if (AppDomainOps.IsTransparentProxy(anyClientData))
                    return false;

                if (!Object.ReferenceEquals(anyClientData, attached))
                    return false;

                syncRoot = savedSyncRoot;
                savedSyncRoot = null;
                attached = null;

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int ReplaceData(
            IAnyClientData anyClientData /* in */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            int count = 0;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (anyClientData != null)
                {
                    //
                    // HACK: *SANITY* Prevent people from replacing
                    //       with the same instance?
                    //
                    if (!Object.ReferenceEquals(anyClientData, this))
                    {
                        object localData = anyClientData.Data;

                        if (localData != null)
                        {
                            count += MaybeResetData();
                            base.Data = localData;
                        }

                        AnyClientData localAnyClientData =
                            anyClientData as AnyClientData;

                        if (localAnyClientData != null)
                        {
                            AnyDictionary localDictionary =
                                localAnyClientData.CopyOrNullDictionary();

                            if (localDictionary != null)
                            {
                                count += MaybeClearAndResetDictionary();
                                dictionary = localDictionary;
                            }
                        }
                    }
                }
                else
                {
                    count += MaybeResetData();
                    count += MaybeClearAndResetDictionary();
                }
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList()
        {
            CheckDisposed();

            return ToList(null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList(
            string pattern,
            bool noCase
            )
        {
            CheckDisposed();

            return ToList(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual IStringList ToList(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                IStringList result = new StringList();

                result.Add("BaseToString", base.ToString());

                if (dictionary != null)
                {
                    foreach (AnyDictionaryPair pair in dictionary)
                    {
                        string name = pair.Key;

                        if ((pattern != null) && !StringOps.Match(
                                null, MatchMode.Glob, name, pattern,
                                noCase))
                        {
                            continue;
                        }

                        string value = GetStringFromObject(pair.Value);

                        if (!empty && (value == null))
                            continue;

                        result.Add(name, value);
                    }
                }

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public virtual object Clone()
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                return new AnyClientData(
                    interpreter, clientData, cultureInfo,
                    (dictionary != null) ?
                        new AnyDictionary(dictionary) : null,
                    base.Data, base.ReadOnly);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(AnyClientData).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(
            bool disposing /* in */
            )
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        IAnyClientData localAttached;

                        lock (syncRoot)
                        {
                            localAttached = attached;
                        }

                        DetachFrom(localAttached);

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (dictionary != null)
                            {
                                dictionary.Clear();
                                dictionary = null;
                            }
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        clientData = null; /* NOT OWNED */
                        cultureInfo = null; /* NOT OWNED */
                        interpreter = null; /* NOT OWNED */
                    }
                }
            }
            finally
            {
                // base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~AnyClientData()
        {
            Dispose(false);
        }
        #endregion
    }
}
