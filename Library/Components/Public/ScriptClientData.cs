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
    [ObjectId("02de061d-be8f-494a-89c1-5c80dc037c6e")]
    public class ScriptClientData :
            AnyClientData, IHaveStringDictionary, ICloneable, IDisposable
    {
        #region Private Data
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ScriptClientData(
            object data /* in */
            )
            : this(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptClientData(
            object data,  /* in */
            bool readOnly /* in */
            )
            : this(new StringDictionary(), data, readOnly)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        private StringDictionary dictionary;
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
        private bool disposed;
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
