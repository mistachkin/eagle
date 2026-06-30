/*
 * ScriptClientData.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a unit of opaque, caller-supplied client data that
    /// also carries an associated dictionary of named string values.  It builds
    /// upon <see cref="AnyClientData" /> to add thread-safe, optionally
    /// read-only access to the dictionary and supports cloning.
    /// </summary>
    [ObjectId("02de061d-be8f-494a-89c1-5c80dc037c6e")]
    public class ScriptClientData :
            AnyClientData, IHaveStringDictionary, ICloneable
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the instance data of this
        /// object.
        /// </summary>
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with the specified client data
        /// and a new, empty dictionary, allowing the dictionary to be modified.
        /// </summary>
        /// <param name="data">
        /// The opaque, caller-supplied client data to wrap.  This parameter may
        /// be null.
        /// </param>
        public ScriptClientData(
            object data /* in */
            )
            : this(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified client data
        /// and a new, empty dictionary.
        /// </summary>
        /// <param name="data">
        /// The opaque, caller-supplied client data to wrap.  This parameter may
        /// be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the resulting instance should be read-only, preventing
        /// subsequent modification of its data.
        /// </param>
        public ScriptClientData(
            object data,  /* in */
            bool readOnly /* in */
            )
            : this(new StringDictionary(), data, readOnly)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified dictionary
        /// and client data.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary of named string values to associate with this
        /// instance.  This parameter may be null.
        /// </param>
        /// <param name="data">
        /// The opaque, caller-supplied client data to wrap.  This parameter may
        /// be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the resulting instance should be read-only, preventing
        /// subsequent modification of its data.
        /// </param>
        public ScriptClientData(
            StringDictionary dictionary, /* in */
            object data,                 /* in */
            bool readOnly                /* in */
            )
            : base(data, readOnly)
        {
            lock (syncRoot)
            {
                this.dictionary = dictionary;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified dictionary,
        /// using the wrapped value of the specified client data container.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary of named string values to associate with this
        /// instance.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data container whose wrapped value is used.  This
        /// parameter may be null, in which case a null value is used.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the resulting instance should be read-only, preventing
        /// subsequent modification of its data.
        /// </param>
        public ScriptClientData(
            StringDictionary dictionary, /* in */
            IClientData clientData,      /* in */
            bool readOnly                /* in */
            )
            : base((clientData != null) ? clientData.Data : null, readOnly)
        {
            lock (syncRoot)
            {
                this.dictionary = dictionary;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified dictionary,
        /// copying its state from the specified extended client data container.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary of named string values to associate with this
        /// instance.  This parameter may be null.
        /// </param>
        /// <param name="anyClientData">
        /// The extended client data container from which to copy state.  This
        /// parameter may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the resulting instance should be read-only, preventing
        /// subsequent modification of its data.
        /// </param>
        public ScriptClientData(
            StringDictionary dictionary,  /* in */
            IAnyClientData anyClientData, /* in */
            bool readOnly                 /* in */
            )
            : base(anyClientData, readOnly)
        {
            lock (syncRoot)
            {
                this.dictionary = dictionary;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveStringDictionary Members
        /// <summary>
        /// The dictionary of named string values associated with this instance.
        /// </summary>
        private StringDictionary dictionary;

        /// <summary>
        /// Gets or sets the dictionary of named string values associated with
        /// this instance.
        /// </summary>
        public virtual StringDictionary Dictionary
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return dictionary;
                }
            }
            set
            {
                CheckDisposed();
                CheckReadOnly();

                lock (syncRoot)
                {
                    dictionary = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyDataBase Overrides
        /// <summary>
        /// This method removes all entries from the associated dictionary,
        /// resetting it to an empty state.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the dictionary was reset successfully; otherwise, false.
        /// </returns>
        public override bool TryResetAny(
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringDictionary localDictionary = Dictionary; /* PROPERTY */

                if (localDictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                localDictionary.Clear();
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the associated dictionary contains an
        /// entry with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the entry to check for.
        /// </param>
        /// <param name="hasAny">
        /// Upon success, this parameter will be set to non-zero if an entry
        /// with the specified name is present; otherwise, it will be set to
        /// zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the check was performed successfully; otherwise, false.
        /// </returns>
        public override bool TryHasAny(
            string name,     /* in */
            ref bool hasAny, /* out */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                StringDictionary localDictionary = Dictionary; /* PROPERTY */

                if (localDictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                hasAny = localDictionary.ContainsKey(name);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the list of entry names in the associated
        /// dictionary, optionally filtered by a matching pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the entry names.  This parameter may be
        /// null, in which case all entry names are returned.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <param name="list">
        /// Upon success, this parameter will be set to the list of matching
        /// entry names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the list was obtained successfully; otherwise, false.
        /// </returns>
        public override bool TryListAny(
            string pattern,         /* in */
            bool noCase,            /* in */
            ref IList<string> list, /* out */
            ref Result error        /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringDictionary localDictionary = Dictionary; /* PROPERTY */

                if (localDictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                StringList localList = new StringList();

                if (GenericOps<string>.FilterList(
                        new StringList(localDictionary.Keys), localList,
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

        /// <summary>
        /// This method obtains the value of the entry with the specified name
        /// from the associated dictionary.
        /// </summary>
        /// <param name="name">
        /// The name of the entry whose value is to be obtained.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be set to the value of the
        /// specified entry.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the value was obtained successfully; otherwise, false.
        /// </returns>
        public override bool TryGetAny(
            string name,      /* in */
            out object value, /* out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                value = null;

                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                StringDictionary localDictionary = Dictionary; /* PROPERTY */

                if (localDictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                string stringValue;

                if (!localDictionary.TryGetValue(name, out stringValue))
                {
                    error = "datum not present";
                    return false;
                }

                value = stringValue;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the value of the entry with the specified name in
        /// the associated dictionary, optionally creating it or overwriting any
        /// existing value.
        /// </summary>
        /// <param name="name">
        /// The name of the entry to set.
        /// </param>
        /// <param name="value">
        /// The value to assign to the entry.  This parameter may be null.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero if an existing entry with the specified name may be
        /// overwritten.
        /// </param>
        /// <param name="create">
        /// Non-zero if a new entry may be created when one with the specified
        /// name does not already exist.
        /// </param>
        /// <param name="toString">
        /// Non-zero if a non-string value should be converted to its string
        /// representation before being stored.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the entry was set successfully; otherwise, false.
        /// </returns>
        public override bool TrySetAny(
            string name,     /* in */
            object value,    /* in */
            bool overwrite,  /* in */
            bool create,     /* in */
            bool toString,   /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                StringDictionary localDictionary = Dictionary; /* PROPERTY */

                if (localDictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                if (localDictionary.ContainsKey(name))
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

                if (value is string)
                {
                    localDictionary[name] = (string)value;
                    return true;
                }

                if (!toString)
                {
                    error = String.Format(
                        "value {0} is not {1}", FormatOps.WrapOrNull(name),
                        typeof(string));

                    return false;
                }

                localDictionary[name] = StringOps.GetStringFromObject(value);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the entry with the specified name from the
        /// associated dictionary.
        /// </summary>
        /// <param name="name">
        /// The name of the entry to remove.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the entry was removed successfully; otherwise, false.
        /// </returns>
        public override bool TryUnsetAny(
            string name,     /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (name == null)
                {
                    error = "invalid name";
                    return false;
                }

                StringDictionary localDictionary = Dictionary; /* PROPERTY */

                if (localDictionary == null)
                {
                    error = "data unavailable";
                    return false;
                }

                if (!localDictionary.Remove(name))
                {
                    error = "datum not removed";
                    return false;
                }

                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new instance of this class that is a copy of
        /// the current instance, including a copy of the associated dictionary.
        /// </summary>
        /// <returns>
        /// The newly created copy of this instance.
        /// </returns>
        public override object Clone()
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                return new ScriptClientData(
                    new StringDictionary(
                        (IDictionary<string, string>)dictionary),
                    base.Data, base.ReadOnly);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object has been disposed of and may no longer be
        /// used.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this object has been disposed of
        /// and the engine is configured to throw on use of disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(ScriptClientData).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this object, optionally
        /// disposing of any managed resources.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose" /> method (as opposed to the
        /// finalizer); when non-zero, managed resources are also released.
        /// </param>
        protected override void Dispose(
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
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
