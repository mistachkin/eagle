/*
 * ClientData.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("149c6f50-7596-4f71-861c-aa1ac700aed7")]
    public class ClientData :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IClientData, IBaseClientData
    {
        #region Public Constants
        public static readonly IClientData Empty = new ClientData(null, true);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ClientData()
            : this(null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ClientData(
            object data /* in */
            )
            : this(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ClientData(
            object data,  /* in */
            bool readOnly /* in */
            )
        {
            this.data = data;
            this.readOnly = readOnly;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        protected virtual void CheckReadOnly()
        {
            if (readOnly)
                throw new ScriptException("data is read-only");
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual bool IsEmpty()
        {
            return Object.ReferenceEquals(this, Empty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IClientData Members
        public virtual object Data
        {
            get { return DataNoThrow; }
            set { CheckReadOnly(); DataNoThrow = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBaseClientData Members
        private object data;
        public virtual object DataNoThrow
        {
            get { return data; }
            set { if (!readOnly) data = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool readOnly;
        public virtual bool ReadOnly
        {
            get { return readOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData log;
        public virtual IClientData Log
        {
            get { return log; }
            set { log = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static IClientData Pack(
            bool readOnly,       /* in */
            params object[] args /* in */
            )
        {
            return new ClientData(args, readOnly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IClientData Pack<T>(
            bool readOnly,  /* in */
            params T[] args /* in */
            )
        {
            return new ClientData(args, readOnly);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static bool IsReadOnly(
            IClientData clientData /* in */
            )
        {
            //
            // HACK: We only know about the base ClientData class as far as
            //       detecting the read-only property goes (since it is not
            //       part of the formal IClientData interface).
            //
            IBaseClientData baseClientData = clientData as IBaseClientData;

            if (baseClientData == null)
                return false; /* NOTE: It cannot be read-only if null. */

            //
            // NOTE: Return the value of the read-only property for the
            //       IClientData instance.
            //
            return baseClientData.ReadOnly;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryGet(
            object @object,
            bool validate,
            out IClientData clientData
            )
        {
            IGetClientData getClientData = @object as IGetClientData;

            if (getClientData != null)
            {
                clientData = getClientData.ClientData;

                if (!validate || (clientData != null))
                    return true;
            }
            else
            {
                clientData = null;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryUnpack<T>(
            IClientData clientData, /* in */
            bool strictType,        /* in */
            out T[] args            /* out */
            )
        {
            object data = null;

            if (!HasData(clientData, ref data))
            {
                args = null;
                return false;
            }

            IList<T> genericList = data as IList<T>;

            if (genericList != null)
            {
                args = new List<T>(genericList).ToArray();
                return true;
            }

            IList list = data as IList;

            if (list != null)
            {
                int count = list.Count;
                T[] localArgs = new T[count];

                for (int index = 0; index < count; index++)
                {
                    object element = list[index];

                    if (MarshalOps.DoesValueMatchType(
                            typeof(T), element))
                    {
                        localArgs[index] = (T)element;
                    }
                    else if (strictType)
                    {
                        args = null;
                        return false;
                    }
                }

                args = localArgs;
                return true;
            }

            IMutableAnyTriplet anyTriplet = data as IMutableAnyTriplet;

            if (anyTriplet != null)
            {
                T[] localArgs = new T[3];

                if (!ExtractorOps.TryExtractXYZ(
                        anyTriplet, out localArgs[0], out localArgs[1],
                        out localArgs[2]))
                {
                    args = null;
                    return false;
                }

                args = localArgs;
                return true;
            }

            IMutableAnyPair anyPair = data as IMutableAnyPair;

            if (anyPair != null)
            {
                T[] localArgs = new T[2];

                if (!ExtractorOps.TryExtractXY(
                        anyPair, out localArgs[0], out localArgs[1]))
                {
                    args = null;
                    return false;
                }

                args = localArgs;
                return true;
            }

            args = null;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryReplace<T>(
            IClientData clientData, /* in, out */
            int index,              /* in */
            T value,                /* in */
            bool ignoreReadOnly,    /* in */
            bool strictType         /* in */
            )
        {
            if (!ignoreReadOnly && IsReadOnly(clientData))
                return false;

            object data = null;

            if (!HasData(clientData, ref data))
                return false;

            int localIndex = (int)index;
            IList<T> genericList = data as IList<T>;

            if (genericList != null)
            {
                if ((localIndex < 0) ||
                    (localIndex >= genericList.Count))
                {
                    return false;
                }

                genericList[localIndex] = value;
                return true;
            }

            IList list = data as IList; /* Array? */

            if (list != null)
            {
                if ((localIndex < 0) ||
                    (localIndex >= list.Count))
                {
                    return false;
                }

                if (strictType)
                {
                    object element = list[localIndex];

                    if (!MarshalOps.DoesValueMatchType(
                            typeof(T), element))
                    {
                        return false;
                    }
                }

                list[localIndex] = value;
                return true;
            }

            IMutableAnyTriplet anyTriplet = data as IMutableAnyTriplet;

            if (anyTriplet != null)
            {
                if (!ignoreReadOnly && !anyTriplet.Mutable)
                    return false;

                if ((localIndex != 0) &&
                    (localIndex != 1) &&
                    (localIndex != 2))
                {
                    return false;
                }

                if (localIndex == 0)
                    return anyTriplet.TrySetX(value);
                else if (localIndex == 1)
                    return anyTriplet.TrySetY(value);
                else
                    return anyTriplet.TrySetZ(value);
            }

            IMutableAnyPair anyPair = data as IMutableAnyPair;

            if (anyPair != null)
            {
                if (!ignoreReadOnly && !anyPair.Mutable)
                    return false;

                if ((localIndex != 0) &&
                    (localIndex != 1))
                {
                    return false;
                }

                if (localIndex == 0)
                    return anyPair.TrySetX(value);
                else
                    return anyPair.TrySetY(value);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryExtract<T>(
            IClientData clientData, /* in */
            int index,              /* in */
            out T value             /* out */
            )
        {
            object data = null;

            if (!HasData(clientData, ref data))
            {
                value = default(T);
                return false;
            }

            int localIndex = (int)index;
            IList<T> genericList = data as IList<T>;

            if (genericList != null)
            {
                if ((localIndex < 0) ||
                    (localIndex >= genericList.Count))
                {
                    value = default(T);
                    return false;
                }

                value = genericList[localIndex];
                return true;
            }

            IList list = data as IList; /* Array? */

            if (list != null)
            {
                if ((localIndex < 0) ||
                    (localIndex >= list.Count))
                {
                    value = default(T);
                    return false;
                }

                object element = list[localIndex];

                if (!MarshalOps.DoesValueMatchType(
                        typeof(T), element))
                {
                    value = default(T);
                    return false;
                }

                value = (T)element;
                return true;
            }

            IAnyTriplet anyTriplet = data as IAnyTriplet;

            if (anyTriplet != null)
            {
                if ((localIndex != 0) &&
                    (localIndex != 1) &&
                    (localIndex != 2))
                {
                    value = default(T);
                    return false;
                }

                T localValue;

                if (localIndex == 0)
                {
                    if (!ExtractorOps.TryExtractX<T>(
                            anyTriplet, out localValue))
                    {
                        value = default(T);
                        return false;
                    }
                }
                else if (localIndex == 1)
                {
                    if (!ExtractorOps.TryExtractY<T>(
                            anyTriplet, out localValue))
                    {
                        value = default(T);
                        return false;
                    }
                }
                else
                {
                    if (!ExtractorOps.TryExtractZ<T>(
                            anyTriplet, out localValue))
                    {
                        value = default(T);
                        return false;
                    }
                }

                value = localValue;
                return true;
            }

            IAnyPair anyPair = data as IAnyPair;

            if (anyPair != null)
            {
                if ((localIndex != 0) &&
                    (localIndex != 1))
                {
                    value = default(T);
                    return false;
                }

                T localValue;

                if (localIndex == 0)
                {
                    if (!ExtractorOps.TryExtractX<T>(
                            anyPair, out localValue))
                    {
                        value = default(T);
                        return false;
                    }
                }
                else
                {
                    if (!ExtractorOps.TryExtractY<T>(
                            anyPair, out localValue))
                    {
                        value = default(T);
                        return false;
                    }
                }

                value = localValue;
                return true;
            }

            value = default(T);
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IClientData MaybeCreate(
            IClientData clientData /* in */
            )
        {
            return IsNullOrEmpty(clientData) ?
                new ClientData(null) : clientData;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IClientData WrapOrReplace(
            IClientData clientData, /* in */
            object data             /* in */
            )
        {
            //
            // NOTE: If the original IClientData instance contains any data,
            //       wrap it, along with the new data, in an outer instance.
            //       Otherwise, simply create and return a new IClientData
            //       instance with the new data.
            //
            if (HasData(clientData))
            {
                return new ClientData(new AnyPair<IClientData, object>(
                    clientData, data), IsReadOnly(clientData));
            }
            else
            {
                return new ClientData(data, IsReadOnly(clientData));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static IClientData UnwrapOrReturn(
            IClientData clientData, /* in */
            ref object data         /* out */
            )
        {
            object localData = null;

            //
            // NOTE: Does the IClientData instance have any data at all?
            //
            if (HasData(clientData, ref localData))
            {
                //
                // NOTE: Is it wrapping another IClientData instance?
                //
                IAnyPair<IClientData, object> anyPair =
                    localData as IAnyPair<IClientData, object>;

                if (anyPair != null)
                {
                    //
                    // NOTE: Return the wrapped data.  In this case, the
                    //       original data can still be used by the caller
                    //       if they extract it from the original (outer)
                    //       IClientData instance.
                    //
                    data = anyPair.Y;

                    //
                    // NOTE: Return the wrapped (inner) IClientData instance
                    //       to the caller.
                    //
                    return anyPair.X;
                }

                //
                // NOTE: Return the original contained data.
                //
                data = localData;
            }

            return clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static bool HasData(
            IClientData clientData /* in */
            )
        {
            object data = null;

            return HasData(clientData, ref data);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasData(
            IClientData clientData, /* in */
            ref object data         /* out */
            )
        {
            //
            // NOTE: If IClientData instance is null -OR- equals our reserved
            //       "empty" instance, then it contains no actual data.
            //
            if (IsNullOrEmpty(clientData))
                return false;

            //
            // NOTE: If this a "plain old" IClientData instance of the default
            //       type and it contains null data, we know there is no actual
            //       data in it.
            //
            Type localType = AppDomainOps.MaybeGetType(clientData);
            object localData = clientData.Data;

            if ((localType == typeof(ClientData)) && (localData == null))
                return false;

            //
            // NOTE: Otherwise, we must assume it contains actual data.
            //
            data = localData;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsNullOrEmpty(
            IClientData clientData
            )
        {
            if (clientData == null)
                return true;

            if (Object.ReferenceEquals(clientData, Empty))
                return true;

#if REMOTING
            if (AppDomainOps.IsTransparentProxy(clientData))
            {
                ClientData remoteClientData =
                    clientData as ClientData;

                if ((remoteClientData != null) &&
                    remoteClientData.IsEmpty())
                {
                    return true;
                }
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static string GetDataTypeName(
            IClientData clientData, /* in */
            string nullTypeName,    /* in */
            string proxyTypeName,   /* in */
            bool wrap               /* in */
            )
        {
            if (clientData == null)
                return nullTypeName;

            return FormatOps.TypeName(
                clientData.Data, nullTypeName, proxyTypeName, wrap);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        private string cachedToString;
        public override string ToString()
        {
            if (cachedToString == null)
            {
                cachedToString = String.Format("{0} {1} {2}",
                    this.ReadOnly ? "read-only" : "read-write",
                    GetType(), FormatOps.WrapHashCode(this));
            }

            return cachedToString;
        }
        #endregion
    }
}
