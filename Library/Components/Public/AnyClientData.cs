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
    /// <summary>
    /// This class provides a general-purpose, thread-safe container for client
    /// data.  In addition to the single value provided by its base class
    /// <see cref="ClientData" />, it stores an arbitrary collection of named
    /// values and exposes strongly-typed accessors for retrieving them as
    /// common value and reference types.  It also supports an associated
    /// interpreter and culture, attaching to (and detaching from) another
    /// container so that the two share locking state, cloning, and the standard
    /// disposal pattern.
    /// </summary>
    [ObjectId("04cac3f9-049c-42b6-9446-084d2296c7da")]
    public class AnyClientData :
            ClientData, IHaveClientData, IHaveCultureInfo, IHaveInterpreter,
            IAnyClientData, ICloneable, IMaybeDisposed, IDisposable
    {
        #region Private Constants
        //
        // NOTE: The number of milliseconds to sleep before retrying for
        //       the instance lock (i.e. syncRoot).
        //
        /// <summary>
        /// The number of milliseconds to sleep before retrying to acquire the
        /// instance lock.
        /// </summary>
        private static int SleepMilliseconds = 50;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is the maximum number of milliseconds that we keep
        //       retrying for the instance lock (i.e. syncRoot).
        //
        /// <summary>
        /// The maximum number of milliseconds to keep retrying to acquire the
        /// instance lock before giving up.  A negative value means there is no
        /// limit.
        /// </summary>
        private static double MaximumSyncRootTimeout = 4000;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default value indicating whether an existing named value may be
        /// overwritten when setting a value.
        /// </summary>
        private static bool DefaultOverwrite = true;

        /// <summary>
        /// The default value indicating whether a named value may be created
        /// when it does not already exist while setting a value.
        /// </summary>
        private static bool DefaultCreate = true;

        /// <summary>
        /// The default value indicating whether a stored value may be coerced
        /// to its string form when a typed accessor is used.
        /// </summary>
        private static bool DefaultToString = true;

        /// <summary>
        /// The default value indicating whether named values with a null value
        /// are included when producing a list representation.
        /// </summary>
        private static bool DefaultEmpty = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The original synchronization root, saved while this instance is
        /// attached to another instance so it can be restored upon detaching.
        /// </summary>
        private object savedSyncRoot;

        /// <summary>
        /// The object used to synchronize access to this instance.
        /// </summary>
        private object syncRoot = new object();

        /// <summary>
        /// The instance to which this instance is attached, if any.  When set,
        /// operations are delegated to it.
        /// </summary>
        private IAnyClientData attached;

        /// <summary>
        /// The backing dictionary that stores the named values held by this
        /// instance.
        /// </summary>
        private AnyDictionary dictionary;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        //
        // WARNING: For use by the Clone method only.
        //
        /// <summary>
        /// Constructs an instance from the supplied component state.  This
        /// constructor is for use by the <c>Clone</c> method only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with this instance.  This parameter may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this instance.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to associate with this instance.  This parameter may be
        /// null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary of named values to use for this instance.
        /// </param>
        /// <param name="data">
        /// The single client data value to wrap.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if this instance should be read-only.
        /// </param>
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
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                this.interpreter = interpreter;
                this.clientData = clientData;
                this.cultureInfo = cultureInfo;
                this.dictionary = dictionary;
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }

            ///////////////////////////////////////////////////////////////////

            MaybeInitialize(null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance with no wrapped value.
        /// </summary>
        public AnyClientData()
            : base()
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance wrapping the specified value.
        /// </summary>
        /// <param name="data">
        /// The single client data value to wrap.  This parameter may be null.
        /// </param>
        public AnyClientData(
            object data /* in */
            )
            : base(data)
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance wrapping the specified value, optionally
        /// making it read-only.
        /// </summary>
        /// <param name="data">
        /// The single client data value to wrap.  This parameter may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if this instance should be read-only.
        /// </param>
        public AnyClientData(
            object data,  /* in */
            bool readOnly /* in */
            )
            : base(data, readOnly)
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance wrapping the value obtained from the specified
        /// client data, optionally making it read-only.
        /// </summary>
        /// <param name="clientData">
        /// The client data whose value is wrapped.  This parameter may be null,
        /// in which case a null value is wrapped.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if this instance should be read-only.
        /// </param>
        public AnyClientData(
            IClientData clientData, /* in */
            bool readOnly           /* in */
            )
            : base((clientData != null) ? clientData.Data : null, readOnly)
        {
            MaybeInitialize(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance wrapping the value obtained from the specified
        /// instance and seeding its named values from that instance, optionally
        /// making it read-only.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance whose value and named values are copied.  This parameter
        /// may be null, in which case a null value is wrapped and no named values
        /// are copied.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if this instance should be read-only.
        /// </param>
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
        /// <summary>
        /// This method creates a new, empty dictionary for holding named
        /// values.
        /// </summary>
        /// <returns>
        /// The newly created dictionary.
        /// </returns>
        private static AnyDictionary NewDictionary()
        {
            return new AnyDictionary();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces the string representation of the specified
        /// object.
        /// </summary>
        /// <param name="object">
        /// The object to convert to a string.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The string representation of the object.
        /// </returns>
        private static string GetStringFromObject(
            object @object /* in */
            )
        {
            return StringOps.GetStringFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified instance is valid for
        /// use (i.e. non-null and not disposed).
        /// </summary>
        /// <param name="anyClientData">
        /// The instance to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the instance is non-null and not disposed; otherwise, false.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Locking Helper Methods
        /// <summary>
        /// This method attempts to acquire the specified lock without waiting.
        /// </summary>
        /// <param name="syncRoot">
        /// The object to lock.  This parameter may be null, in which case no
        /// attempt is made.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// acquired.
        /// </param>
        private static void PrivateTryLock(
            object syncRoot, /* in */
            ref bool locked  /* out */
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the specified lock, waiting up to the
        /// specified timeout.
        /// </summary>
        /// <param name="syncRoot">
        /// The object to lock.  This parameter may be null, in which case no
        /// attempt is made.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait for the lock.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// acquired.
        /// </param>
        private static void PrivateTryLock(
            object syncRoot, /* in */
            int timeout,     /* in */
            ref bool locked  /* out */
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the specified lock, waiting up to the
        /// configured wait-lock timeout.
        /// </summary>
        /// <param name="syncRoot">
        /// The object to lock.  This parameter may be null, in which case no
        /// attempt is made.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// acquired.
        /// </param>
        private static void PrivateTryLockWithWait(
            object syncRoot, /* in */
            ref bool locked  /* out */
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(
                syncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the specified lock if it is currently held.
        /// </summary>
        /// <param name="syncRoot">
        /// The object to unlock.  This parameter may be null, in which case
        /// nothing is done.
        /// </param>
        /// <param name="locked">
        /// On input, non-zero if the lock is held.  Upon return, this parameter
        /// will be false if the lock was released.
        /// </param>
        private static void PrivateExitLock(
            object syncRoot, /* in */
            ref bool locked  /* in, out */
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to sleep before retrying
        /// to acquire the instance lock.
        /// </summary>
        /// <returns>
        /// The configured sleep time, in milliseconds.
        /// </returns>
        private static int GetSleepMilliseconds()
        {
            return Interlocked.CompareExchange(
                ref SleepMilliseconds, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically sets the number of milliseconds to sleep
        /// before retrying to acquire the instance lock, provided the current
        /// value matches the expected one.
        /// </summary>
        /// <param name="oldSleepMilliseconds">
        /// The expected current sleep time, in milliseconds.
        /// </param>
        /// <param name="newSleepMilliseconds">
        /// The new sleep time to set, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the value was changed; otherwise, false.
        /// </returns>
        private static bool SetSleepMilliseconds(
            int oldSleepMilliseconds, /* in */
            int newSleepMilliseconds  /* in */
            )
        {
            return Interlocked.CompareExchange(
                ref SleepMilliseconds, newSleepMilliseconds,
                oldSleepMilliseconds) == oldSleepMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the maximum number of milliseconds to keep retrying
        /// to acquire the instance lock before giving up.
        /// </summary>
        /// <returns>
        /// The configured maximum timeout, in milliseconds.
        /// </returns>
        private static double GetMaximumTimeout()
        {
            return Interlocked.CompareExchange(
                ref MaximumSyncRootTimeout, 0.0, 0.0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically sets the maximum number of milliseconds to
        /// keep retrying to acquire the instance lock, provided the current
        /// value matches the expected one.
        /// </summary>
        /// <param name="oldMaximumTimeout">
        /// The expected current maximum timeout, in milliseconds.
        /// </param>
        /// <param name="newMaximumTimeout">
        /// The new maximum timeout to set, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the value was changed; otherwise, false.
        /// </returns>
        private static bool SetMaximumTimeout(
            double oldMaximumTimeout, /* in */
            double newMaximumTimeout  /* in */
            )
        {
            return Interlocked.CompareExchange(
                ref MaximumSyncRootTimeout, newMaximumTimeout,
                oldMaximumTimeout) == oldMaximumTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the maximum lock-retry timeout has
        /// elapsed since the specified start time.
        /// </summary>
        /// <param name="start">
        /// The time, in UTC, at which the lock-retry attempts began.
        /// </param>
        /// <returns>
        /// True if the timeout has elapsed; otherwise, false.  When there is no
        /// timeout limit, this method always returns false.
        /// </returns>
        private static bool HasTimeoutElapsed(
            DateTime start /* in */
            )
        {
            double maximumTimeout = GetMaximumTimeout();

            if (maximumTimeout < 0)
                return false;

            DateTime now = TimeOps.GetUtcNow();

            if (now < start) // NOTE: Time travel, eh?
                return true;

            double timeout = now.Subtract(start).TotalMilliseconds;

            if (timeout < maximumTimeout)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sleeps for the configured interval, if any, and always
        /// yields the current thread so that it does not simply fast-spin while
        /// retrying to acquire the instance lock.
        /// </summary>
        private static void MaybeSleepAndOrYield()
        {
            int milliseconds = GetSleepMilliseconds();

            if (milliseconds > 0)
                HostOps.ThreadSleep(milliseconds);

            //
            // HACK: *FAIL-SAFE* Always "yield" somehow so
            //       this thread does not simply fast-spin.
            //
            HostOps.ThreadYield();
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method gets the synchronization root currently in use by this
        /// instance.
        /// </summary>
        /// <returns>
        /// The current synchronization root.
        /// </returns>
        private object GetSyncRoot()
        {
            return Interlocked.CompareExchange(
                ref syncRoot, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically sets the synchronization root used by this
        /// instance, provided the current value matches the expected one.
        /// </summary>
        /// <param name="oldSyncRoot">
        /// The expected current synchronization root.
        /// </param>
        /// <param name="newSyncRoot">
        /// The new synchronization root to set.
        /// </param>
        /// <returns>
        /// True if the synchronization root was changed; otherwise, false.
        /// </returns>
        private bool MaybeSetSyncRoot(
            object oldSyncRoot, /* in */
            object newSyncRoot  /* in */
            )
        {
            return Interlocked.CompareExchange(
                ref syncRoot, newSyncRoot,
                oldSyncRoot) == oldSyncRoot;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method acquires the instance lock, retrying as necessary while
        /// the synchronization root remains stable and the maximum retry timeout
        /// has not elapsed.
        /// </summary>
        /// <param name="oldSyncRoot">
        /// Upon success, this parameter will receive the synchronization root
        /// that was locked.
        /// </param>
        /// <param name="locked">
        /// Upon success, this parameter will be non-zero, indicating the lock
        /// was acquired.
        /// </param>
        /// <returns>
        /// True if the lock was acquired before the timeout elapsed; otherwise,
        /// false.
        /// </returns>
        private bool MaybeEnterSyncRoot(
            ref object oldSyncRoot, /* out */
            ref bool locked         /* out */
            )
        {
            DateTime start = TimeOps.GetUtcNow();

            while (true)
            {
                oldSyncRoot = GetSyncRoot();

                if (oldSyncRoot == null)
                {
                    if (HasTimeoutElapsed(start))
                        return false;

                    MaybeSleepAndOrYield();
                    continue;
                }

                //
                // NOTE: This may (technically) wait longer than
                //       the overall timeout budget; and that is
                //       fine.
                //
                PrivateTryLockWithWait(oldSyncRoot, ref locked);

                if (!locked)
                {
                    if (HasTimeoutElapsed(start))
                        return false;

                    MaybeSleepAndOrYield();
                    continue;
                }

                object newSyncRoot = GetSyncRoot();

                if (!Object.ReferenceEquals(oldSyncRoot, newSyncRoot))
                {
                    PrivateExitLock(oldSyncRoot, ref locked);

                    if (HasTimeoutElapsed(start))
                        return false;

                    MaybeSleepAndOrYield();
                    continue;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the instance lock previously acquired via
        /// <c>MaybeEnterSyncRoot</c>.
        /// </summary>
        /// <param name="syncRoot">
        /// On input, the synchronization root that was locked.  Upon success,
        /// this parameter will be set to null.
        /// </param>
        /// <param name="locked">
        /// On input, non-zero if the lock is held.  Upon return, this parameter
        /// will be false if the lock was released.
        /// </param>
        /// <returns>
        /// True if the lock was released; otherwise, false.
        /// </returns>
        private bool MaybeExitSyncRoot(
            ref object syncRoot, /* in, out */
            ref bool locked      /* out */
            )
        {
            PrivateExitLock(syncRoot, ref locked);

            if (locked)
                return false;

            syncRoot = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method throws an exception indicating that the instance lock
        /// could not be acquired within the maximum retry timeout.
        /// </summary>
        private void ThrowLockError()
        {
            double maximumTimeout = GetMaximumTimeout();

            throw new ScriptException(String.Format(
                "locking retry timeout after {0} milliseconds",
                (maximumTimeout < 0) ? FormatOps.DisplayInfinite :
                maximumTimeout.ToString()));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the culture currently associated with this instance.
        /// </summary>
        /// <returns>
        /// The associated culture, or null if there is none.
        /// </returns>
        private CultureInfo GetCultureInfo()
        {
            return Interlocked.CompareExchange(
                ref cultureInfo, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically sets the culture associated with this
        /// instance, provided the current value matches the expected one.
        /// </summary>
        /// <param name="oldCultureInfo">
        /// The expected current culture.
        /// </param>
        /// <param name="newCultureInfo">
        /// The new culture to set.
        /// </param>
        /// <returns>
        /// True if the culture was changed; otherwise, false.
        /// </returns>
        private bool MaybeSetCultureInfo(
            CultureInfo oldCultureInfo,
            CultureInfo newCultureInfo
            )
        {
            return Interlocked.CompareExchange(
                ref cultureInfo, newCultureInfo,
                oldCultureInfo) == oldCultureInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the dictionary of named values held by this
        /// instance, under the protection of the instance lock.
        /// </summary>
        /// <returns>
        /// The dictionary of named values, or null if there is none.
        /// </returns>
        private AnyDictionary GetDictionary()
        {
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                return dictionary;
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the single wrapped value held by the base class,
        /// if any.
        /// </summary>
        /// <returns>
        /// The number of values that were cleared (zero or one).
        /// </returns>
        private int MaybeResetData()
        {
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                int count = 0;

                if (base.Data != null)
                {
                    count++;
                    base.Data = null;
                }

                return count;
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and discards the dictionary of named values held
        /// by this instance, if any.
        /// </summary>
        /// <returns>
        /// The number of named values that were present before the dictionary
        /// was cleared.
        /// </returns>
        private int MaybeClearAndResetDictionary()
        {
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                int count = 0;

                if (dictionary != null)
                {
                    count += dictionary.Count;

                    dictionary.Clear();
                    dictionary = null;
                }

                return count;
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a copy of the dictionary of named values held by
        /// this instance, or null if there is none.
        /// </summary>
        /// <returns>
        /// A new dictionary containing a copy of the named values, or null if
        /// this instance has no dictionary.
        /// </returns>
        private AnyDictionary CopyOrNullDictionary()
        {
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                return (dictionary != null) ?
                    new AnyDictionary(dictionary) : null;
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a copy of the dictionary of named values held by
        /// this instance, or a new, empty dictionary if there is none.
        /// </summary>
        /// <returns>
        /// A new dictionary containing a copy of the named values, or a new,
        /// empty dictionary if this instance has no dictionary.
        /// </returns>
        private AnyDictionary CopyOrNewDictionary()
        {
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                return (dictionary != null) ?
                    new AnyDictionary(dictionary) : NewDictionary();
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures the dictionary of named values exists, creating
        /// it if necessary.  When an instance is supplied, its named values are
        /// used to seed the new dictionary.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance whose named values are used to seed the dictionary.
        /// This parameter may be null.
        /// </param>
        private void MaybeInitialize(
            IAnyClientData anyClientData /* in: OPTIONAL */
            )
        {
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                if (dictionary != null)
                    return;

                AnyClientData localAnyClientData =
                    anyClientData as AnyClientData;

                dictionary = (localAnyClientData != null) ?
                    localAnyClientData.CopyOrNewDictionary() :
                    NewDictionary();
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this instance.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the client data associated with this instance.
        /// </summary>
        public IClientData ClientData
        {
            get
            {
                CheckDisposed();

                bool locked = false;
                object syncRoot = null;

                try
                {
                    if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                        ThrowLockError();

                    return clientData;
                }
                finally
                {
                    MaybeExitSyncRoot(ref syncRoot, ref locked);
                }
            }
            set
            {
                CheckDisposed();
                CheckReadOnly();

                bool locked = false;
                object syncRoot = null;

                try
                {
                    if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                        ThrowLockError();

                    clientData = value;
                }
                finally
                {
                    MaybeExitSyncRoot(ref syncRoot, ref locked);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCultureInfo Members
        /// <summary>
        /// The culture associated with this instance.
        /// </summary>
        private CultureInfo cultureInfo;

        /// <summary>
        /// Gets or sets the culture associated with this instance.
        /// </summary>
        public CultureInfo CultureInfo
        {
            get
            {
                CheckDisposed();

                return GetCultureInfo();
            }
            set
            {
                CheckDisposed();
                CheckReadOnly();

                CultureInfo oldCultureInfo = GetCultureInfo();
                CultureInfo newCultureInfo = value;

                if (!MaybeSetCultureInfo(
                        oldCultureInfo, newCultureInfo))
                {
                    throw new ScriptException(
                        "could not change culture");
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// The interpreter associated with this instance.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Gets or sets the interpreter associated with this instance.
        /// </summary>
        public Interpreter Interpreter
        {
            get
            {
                CheckDisposed();

                bool locked = false;
                object syncRoot = null;

                try
                {
                    if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                        ThrowLockError();

                    return interpreter;
                }
                finally
                {
                    MaybeExitSyncRoot(ref syncRoot, ref locked);
                }
            }
            set
            {
                CheckDisposed();
                CheckReadOnly();

                bool locked = false;
                object syncRoot = null;

                try
                {
                    if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                        ThrowLockError();

                    interpreter = value;
                }
                finally
                {
                    MaybeExitSyncRoot(ref syncRoot, ref locked);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronizeBase Members
        /// <summary>
        /// Gets the object used to synchronize access to this instance.
        /// </summary>
        public object SyncRoot
        {
            get { CheckDisposed(); return GetSyncRoot(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        /// <summary>
        /// This method attempts to acquire the instance lock without waiting.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// acquired.
        /// </param>
        public virtual void TryLock(
            ref bool locked /* out */
            )
        {
            CheckDisposed();

            PrivateTryLock(GetSyncRoot(), ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the instance lock, waiting up to the
        /// configured wait-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// acquired.
        /// </param>
        public void TryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateTryLockWithWait(GetSyncRoot(), ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the instance lock without waiting and
        /// without checking whether this instance has been disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// acquired.
        /// </param>
        public void TryLockNoThrow(
            ref bool locked
            )
        {
            // CheckDisposed(); /* EXEMPT */

            PrivateTryLock(GetSyncRoot(), ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the instance lock, waiting up to the
        /// specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait for the lock.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// acquired.
        /// </param>
        public virtual void TryLock(
            int timeout,    /* in */
            ref bool locked /* out */
            )
        {
            CheckDisposed();

            PrivateTryLock(GetSyncRoot(), timeout, ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the instance lock if it is currently held.
        /// </summary>
        /// <param name="locked">
        /// On input, non-zero if the lock is held.  Upon return, this parameter
        /// will be false if the lock was released.
        /// </param>
        public virtual void ExitLock(
            ref bool locked /* in, out */
            )
        {
            CheckDisposed();

            PrivateExitLock(GetSyncRoot(), ref locked);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyDataBase Members
        /// <summary>
        /// This method removes all named values held by this instance.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the named values were reset; otherwise, false.
        /// </returns>
        public virtual bool TryResetAny(
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a named value is present.
        /// </summary>
        /// <param name="name">
        /// The name of the value to check for.
        /// </param>
        /// <param name="hasAny">
        /// Upon success, this parameter will be non-zero if the named value is
        /// present.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the presence of the named value was determined; otherwise,
        /// false.
        /// </returns>
        public virtual bool TryHasAny(
            string name,     /* in */
            ref bool hasAny, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method lists the names of the values held by this instance,
        /// optionally filtered by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to filter the names.  This parameter may be
        /// null to match all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, this parameter will receive the list of matching names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the names were listed; otherwise, false.
        /// </returns>
        public virtual bool TryListAny(
            string pattern,         /* in */
            bool noCase,            /* in */
            ref IList<string> list, /* out */
            ref Result error        /* out */
            )
        {
            CheckDisposed();

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as an object.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the value.  Upon failure,
        /// it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
        public virtual bool TryGetAny(
            string name,      /* in */
            out object value, /* out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets a named value, honoring the supplied overwrite and
        /// create options.
        /// </summary>
        /// <param name="name">
        /// The name of the value to set.
        /// </param>
        /// <param name="value">
        /// The value to set.  This parameter may be null.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero if an existing value with the same name may be overwritten.
        /// </param>
        /// <param name="create">
        /// Non-zero if the value may be created when it does not already exist.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the value should be coerced to its string form.  This
        /// parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
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

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a named value.
        /// </summary>
        /// <param name="name">
        /// The name of the value to remove.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was removed; otherwise, false.
        /// </returns>
        public virtual bool TryUnsetAny(
            string name,     /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();


            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyData Members
        /// <summary>
        /// This method removes all named values held by this instance.
        /// </summary>
        /// <returns>
        /// True if the named values were reset; otherwise, false.
        /// </returns>
        public bool TryResetAny()
        {
            CheckDisposed();
            CheckReadOnly();

            Result error = null;

            return TryResetAny(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a named value is present.
        /// </summary>
        /// <param name="name">
        /// The name of the value to check for.
        /// </param>
        /// <returns>
        /// True if the named value is present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as an object.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the value.  Upon failure,
        /// it will be null.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method sets a named value using the default overwrite, create,
        /// and string-coercion options.
        /// </summary>
        /// <param name="name">
        /// The name of the value to set.
        /// </param>
        /// <param name="value">
        /// The value to set.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method removes a named value.
        /// </summary>
        /// <param name="name">
        /// The name of the value to remove.
        /// </param>
        /// <returns>
        /// True if the value was removed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method gets a named value as a boolean.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a boolean.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the boolean value.  Upon
        /// failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            bool localValue = default(bool);

            if (Value.GetBoolean2(
                    stringValue, ValueFlags.AnyBoolean, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(bool);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a nullable boolean.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a nullable boolean.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the nullable boolean value.
        /// Upon failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            bool? localValue = null;

            if (Value.GetNullableBoolean2(
                    stringValue, ValueFlags.AnyBoolean, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a signed byte.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a signed byte.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the signed byte value.
        /// Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            sbyte localValue = default(sbyte);

            if (Value.GetSignedByte2(
                    stringValue, ValueFlags.AnyByte | ValueFlags.Signed,
                    cultureInfo, ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(sbyte);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a byte.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a byte.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the byte value.  Upon
        /// failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            byte localValue = default(byte);

            if (Value.GetByte2(
                    stringValue, ValueFlags.AnyByte, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(byte);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a narrow (16-bit) integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a narrow integer.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the narrow integer value.
        /// Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            short localValue = default(short);

            if (Value.GetNarrowInteger2(
                    stringValue, ValueFlags.AnyNarrowInteger, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(short);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as an unsigned narrow (16-bit)
        /// integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already an unsigned narrow integer.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the unsigned narrow integer
        /// value.  Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            ushort localValue = default(ushort);

            if (Value.GetUnsignedNarrowInteger2(
                    stringValue, ValueFlags.AnyNarrowInteger |
                    ValueFlags.Unsigned, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(ushort);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a character.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a character.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the character value.  Upon
        /// failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            char localValue = default(char);

            if (Value.GetCharacter2(
                    stringValue, ValueFlags.AnyCharacter, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(char);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a 32-bit integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already an integer.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the integer value.  Upon
        /// failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            int localValue = default(int);

            if (Value.GetInteger2(
                    stringValue, ValueFlags.AnyInteger, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(int);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as an unsigned 32-bit integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already an unsigned integer.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the unsigned integer value.
        /// Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            uint localValue = default(uint);

            if (Value.GetUnsignedInteger2(
                    stringValue, ValueFlags.AnyInteger | ValueFlags.Unsigned,
                    cultureInfo, ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(uint);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a wide (64-bit) integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a wide integer.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the wide integer value.
        /// Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            long localValue = default(long);

            if (Value.GetWideInteger2(
                    stringValue, ValueFlags.AnyWideInteger, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(long);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as an unsigned wide (64-bit) integer.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already an unsigned wide integer.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the unsigned wide integer
        /// value.  Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            ulong localValue = default(ulong);

            if (Value.GetUnsignedWideInteger2(
                    stringValue, ValueFlags.AnyWideInteger |
                    ValueFlags.Unsigned, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(ulong);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a decimal.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a decimal.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the decimal value.  Upon
        /// failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            decimal localValue = default(decimal);

            if (Value.GetDecimal(
                    stringValue, ValueFlags.AnyDecimal, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(decimal);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a single-precision floating-point
        /// number.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a single-precision floating-point number.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the single-precision value.
        /// Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            float localValue = default(float);

            if (Value.GetSingle(
                    stringValue, cultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(float);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a double-precision floating-point
        /// number.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a double-precision floating-point number.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the double-precision value.
        /// Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            double localValue = default(double);

            if (Value.GetDouble(
                    stringValue, cultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(float);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a date and time.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="format">
        /// The format used to parse the value from its string form.  This
        /// parameter may be null.
        /// </param>
        /// <param name="kind">
        /// The kind of date and time to assume when parsing the value.
        /// </param>
        /// <param name="styles">
        /// The styles used to control parsing of the value.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a date and time.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the date and time value.
        /// Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            DateTime localValue = default(DateTime);

            if (Value.GetDateTime2(
                    stringValue, format, ValueFlags.AnyDateTime,
                    kind, styles, cultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(DateTime);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a time span.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a time span.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the time span value.  Upon
        /// failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            TimeSpan localValue = default(TimeSpan);

            if (Value.GetTimeSpan2(
                    stringValue, ValueFlags.AnyTimeSpan, cultureInfo,
                    ref localValue, ref error) != ReturnCode.Ok)
            {
                value = default(TimeSpan);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as an enumerated value of the
        /// specified type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when parsing flags-style enumerated values.
        /// This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the value should be returned as.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already of the requested enumerated type.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the enumerated value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            object enumValue;

            if (EnumOps.IsFlags(enumType))
            {
                enumValue = EnumOps.TryParseFlags(
                    interpreter, enumType, null,
                    stringValue, cultureInfo,
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
        /// <summary>
        /// This method gets a named value as client data.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the client data value.
        /// Upon failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as a string.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced to its string form when
        /// it is not already a string.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the string value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as a string list, parsing it as a list
        /// when necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when parsing the value as a list.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced to its string form, and
        /// then parsed as a list, when it is not already a string list or a
        /// string.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the string list value.
        /// Upon failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as a globally unique identifier.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a globally unique identifier.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the globally unique
        /// identifier value.  Upon failure, it will be the default value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            Guid localValue = default(Guid);

            if (Value.GetGuid(
                    stringValue, cultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = default(Guid);
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a uniform resource identifier.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="uriKind">
        /// The kind of uniform resource identifier to require when parsing the
        /// value.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a uniform resource identifier.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the uniform resource
        /// identifier value.  Upon failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            Uri localValue = null;

            if (Value.GetUri(
                    stringValue, uriKind, cultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as a version.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a version.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the version value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            Version localValue = null;

            if (Value.GetVersion(
                    stringValue, cultureInfo, ref localValue,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = localValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as an interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when resolving the value.  This parameter may
        /// be null.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already an interpreter.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the interpreter value.
        /// Upon failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as a plugin, resolving it by assembly
        /// name within the specified interpreter when necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the plugin.  This parameter is
        /// required when the value must be resolved from its string form.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a plugin.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the plugin value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as a rule set, creating it from its
        /// string form when necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the rule set.  This parameter is
        /// required when the value must be created from its string form.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a rule set.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the rule set value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
        public virtual bool TryGetRuleSet(
            Interpreter interpreter,
            string name,
            bool toString,
            out IRuleSet value,
            ref Result error
            )
        {
            CheckDisposed();

            object @object;

            if (!TryGetAny(name, out @object, ref error))
            {
                value = null;
                return false;
            }

            if (@object is IRuleSet)
            {
                value = (IRuleSet)@object;
                return true;
            }

            if (!toString)
            {
                value = null;

                error = String.Format(
                    "value {0} is not {1}", FormatOps.WrapOrNull(name),
                    typeof(IRuleSet));

                return false;
            }

            if (interpreter == null)
            {
                value = null;
                error = "invalid interpreter";

                return false;
            }

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);

            IRuleSet localValue = RuleSet.Create(
                stringValue, cultureInfo, ref error);

            value = localValue;
            return (localValue != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a named value as an opaque object handle, resolving
        /// it within the specified interpreter when necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the object.  This parameter is
        /// required when the value must be resolved from its string form.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already an object handle.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the object value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as an encoding, resolving it within
        /// the specified interpreter, or globally, when necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the encoding.  This parameter may be
        /// null, in which case the encoding is resolved globally.
        /// </param>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already an encoding.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the encoding value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets a named value as a byte array.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.
        /// </param>
        /// <param name="toString">
        /// Non-zero if the stored value may be coerced from its string form when
        /// it is not already a byte array.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will receive the byte array value.  Upon
        /// failure, it will be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message.
        /// </param>
        /// <returns>
        /// True if the value was retrieved; otherwise, false.
        /// </returns>
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

            CultureInfo cultureInfo = GetCultureInfo();
            string stringValue = GetStringFromObject(@object);
            byte[] localValue = null;

            if (StringOps.GetBytesFromString(
                    stringValue, cultureInfo, ref localValue,
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
        /// <summary>
        /// Gets the instance to which this instance is currently attached, or
        /// null if it is not attached to another instance.
        /// </summary>
        public IAnyClientData Attached
        {
            get
            {
                CheckDisposed();

                bool locked = false;
                object syncRoot = null;

                try
                {
                    if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                        ThrowLockError();

                    return attached;
                }
                finally
                {
                    MaybeExitSyncRoot(ref syncRoot, ref locked);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the root instance reached by following the chain of attached
        /// instances, or this instance if it is not attached to another
        /// instance.
        /// </summary>
        public IAnyClientData Root
        {
            get
            {
                CheckDisposed();

                bool locked = false;
                object syncRoot = null;

                try
                {
                    if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                        ThrowLockError();

                    IAnyClientData thisClientData = this;
                    IAnyClientData linkClientData = attached;

                    while (IsValid(linkClientData))
                    {
                        thisClientData = linkClientData;
                        linkClientData = linkClientData.Attached;
                    }

                    return thisClientData;
                }
                finally
                {
                    MaybeExitSyncRoot(ref syncRoot, ref locked);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attaches this instance to the specified instance so that
        /// the two share the same synchronization root and operations are
        /// delegated to the attached instance.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance to attach to.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if this instance was attached; otherwise, false.
        /// </returns>
        public bool AttachTo(
            IAnyClientData anyClientData /* in */
            )
        {
            CheckDisposed();

            object oldSyncRoot = GetSyncRoot();

            if (oldSyncRoot == null)
                return false;

            bool locked1 = false;

            try
            {
                PrivateTryLock(oldSyncRoot, ref locked1);

                if (!locked1)
                    return false;

                if (attached != null)
                    return false;

                if (anyClientData == null)
                    return false;

                if (AppDomainOps.IsTransparentProxy(anyClientData))
                    return false;

                if (Object.ReferenceEquals(anyClientData, this))
                    return false;

                object newSyncRoot = anyClientData.SyncRoot;

                if (newSyncRoot == null)
                    return false;

                bool locked2 = false;

                try
                {
                    PrivateTryLock(newSyncRoot, ref locked2);

                    if (!locked2)
                        return false;

                    if (!MaybeSetSyncRoot(oldSyncRoot, newSyncRoot))
                        return false;

                    savedSyncRoot = oldSyncRoot;
                    attached = anyClientData;

                    return true;
                }
                finally
                {
                    PrivateExitLock(newSyncRoot, ref locked2);
                }
            }
            finally
            {
                PrivateExitLock(oldSyncRoot, ref locked1);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method detaches this instance from the specified instance,
        /// restoring its original synchronization root.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance to detach from, which must be the instance currently
        /// attached.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if this instance was detached; otherwise, false.
        /// </returns>
        public bool DetachFrom(
            IAnyClientData anyClientData /* in */
            )
        {
            CheckDisposed();

            object oldSyncRoot = GetSyncRoot();

            if (oldSyncRoot == null)
                return false;

            bool locked1 = false;

            try
            {
                PrivateTryLock(oldSyncRoot, ref locked1);

                if (!locked1)
                    return false;

                if (attached == null)
                    return false;

                if (anyClientData == null)
                    return false;

                if (AppDomainOps.IsTransparentProxy(anyClientData))
                    return false;

                if (!Object.ReferenceEquals(anyClientData, attached))
                    return false;

                object newSyncRoot = savedSyncRoot;

                if (newSyncRoot == null)
                    return false;

                bool locked2 = false;

                try
                {
                    PrivateTryLock(newSyncRoot, ref locked2);

                    if (!locked2)
                        return false;

                    if (!MaybeSetSyncRoot(oldSyncRoot, newSyncRoot))
                        return false;

                    savedSyncRoot = null;
                    attached = null;

                    return true;
                }
                finally
                {
                    PrivateExitLock(newSyncRoot, ref locked2);
                }
            }
            finally
            {
                PrivateExitLock(oldSyncRoot, ref locked1);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces the wrapped value and named values of this
        /// instance with those of the specified instance.  When a null instance
        /// is supplied, the wrapped value and named values are cleared.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance whose value and named values are copied.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The number of values that were replaced or cleared.
        /// </returns>
        public int ReplaceData(
            IAnyClientData anyClientData /* in */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                int count = 0;

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

                return count;
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list representation of the named values held
        /// by this instance.
        /// </summary>
        /// <returns>
        /// A list of name and value pairs.
        /// </returns>
        public IStringList ToList()
        {
            CheckDisposed();

            return ToList(null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list representation of the named values held
        /// by this instance, optionally filtered by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to filter the names.  This parameter may be
        /// null to match all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A list of name and value pairs.
        /// </returns>
        public IStringList ToList(
            string pattern,
            bool noCase
            )
        {
            CheckDisposed();

            return ToList(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list representation of the named values held
        /// by this instance, optionally filtered by a pattern and optionally
        /// including values that are null.
        /// </summary>
        /// <param name="pattern">
        /// The glob pattern used to filter the names.  This parameter may be
        /// null to match all names.
        /// </param>
        /// <param name="empty">
        /// Non-zero if named values whose value is null should be included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A list of name and value pairs.
        /// </returns>
        public virtual IStringList ToList(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            CheckDisposed();

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this instance, based
        /// on the list of its named values.
        /// </summary>
        /// <returns>
        /// The string representation of this instance.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new instance that is a copy of this instance,
        /// including its interpreter, client data, culture, named values, and
        /// wrapped value.
        /// </summary>
        /// <returns>
        /// The newly created copy of this instance.
        /// </returns>
        public virtual object Clone()
        {
            CheckDisposed();

            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

                return new AnyClientData(
                    interpreter, clientData, cultureInfo,
                    (dictionary != null) ?
                        new AnyDictionary(dictionary) : null,
                    base.Data, base.ReadOnly);
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this instance is in the process of
        /// being disposed.  This member is not supported by this class.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                throw new NotSupportedException();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this instance has been disposed
        /// and the interpreter is configured to throw on disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && /* NO-LOCK */
                Engine.IsThrowOnDisposed(interpreter, null))
            {
                throw new ObjectDisposedException(
                    typeof(AnyClientData).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of the resources used by this instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the public
        /// <c>Dispose</c> method (rather than from the finalizer), in which case
        /// managed resources may also be released.
        /// </param>
        protected virtual void Dispose(
            bool disposing /* in */
            )
        {
            bool locked = false;
            object syncRoot = null;

            try
            {
                if (!MaybeEnterSyncRoot(ref syncRoot, ref locked))
                    ThrowLockError();

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

                            localAttached = attached;

                            DetachFrom(localAttached);

                            if (dictionary != null)
                            {
                                dictionary.Clear();
                                dictionary = null;
                            }
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////

                        clientData = null; /* NOT OWNED */
                        cultureInfo = null; /* NOT OWNED */
                        interpreter = null; /* NOT OWNED */
                    }
                }
                finally
                {
                    //
                    // NOTE: The base class (ClientData) does not
                    //       implement IDisposable.  If this ever
                    //       changes, uncomment this.
                    //
                    // base.Dispose(disposing);

                    disposed = true;
                }
            }
            finally
            {
                MaybeExitSyncRoot(ref syncRoot, ref locked);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method disposes of the resources used by this instance and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this instance, releasing any unmanaged resources.
        /// </summary>
        ~AnyClientData()
        {
            Dispose(false);
        }
        #endregion
    }
}
