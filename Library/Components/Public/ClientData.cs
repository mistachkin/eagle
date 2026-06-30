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
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="IClientData" /> interface, used to associate an arbitrary
    /// opaque data object with an entity managed by the Eagle library.  It
    /// also supports an optional read-only mode along with a number of static
    /// helper methods for packing, unpacking, wrapping, and querying the
    /// contained data.
    /// </summary>
    [ObjectId("149c6f50-7596-4f71-861c-aa1ac700aed7")]
    public class ClientData :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IClientData, IBaseClientData
    {
        #region Public Constants
        /// <summary>
        /// A shared, read-only instance that represents the absence of any
        /// client data.
        /// </summary>
        public static readonly IClientData Empty = new ClientData(null, true);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no contained data.
        /// </summary>
        public ClientData()
            : this(null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified contained
        /// data.
        /// </summary>
        /// <param name="data">
        /// The opaque data object to be contained by this instance.
        /// </param>
        public ClientData(
            object data /* in */
            )
            : this(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified contained
        /// data and read-only state.
        /// </summary>
        /// <param name="data">
        /// The opaque data object to be contained by this instance.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the contained data should be read-only, preventing any
        /// subsequent modification.
        /// </param>
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
        /// <summary>
        /// This method throws an exception if this instance is read-only.  It
        /// is used to guard operations that would otherwise modify the
        /// contained data.
        /// </summary>
        protected virtual void CheckReadOnly()
        {
            if (readOnly)
                throw new ScriptException("data is read-only");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this instance is the reserved
        /// "empty" instance.
        /// </summary>
        /// <returns>
        /// True if this instance is the reserved "empty" instance; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsEmpty()
        {
            return Object.ReferenceEquals(this, Empty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IClientData Members
        /// <summary>
        /// Gets or sets the opaque data object contained by this instance.  An
        /// attempt to set this property when this instance is read-only will
        /// cause an exception to be thrown.
        /// </summary>
        public virtual object Data
        {
            get { return DataNoThrow; }
            set { CheckReadOnly(); DataNoThrow = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBaseClientData Members
        /// <summary>
        /// The opaque data object contained by this instance.
        /// </summary>
        private object data;

        /// <summary>
        /// Gets or sets the opaque data object contained by this instance.
        /// Unlike <see cref="Data" />, attempting to set this property when
        /// this instance is read-only is silently ignored instead of throwing
        /// an exception.
        /// </summary>
        public virtual object DataNoThrow
        {
            get { return data; }
            set { if (!readOnly) data = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the contained data is read-only and may not be
        /// modified.
        /// </summary>
        private bool readOnly;

        /// <summary>
        /// Gets a value indicating whether the contained data is read-only.
        /// </summary>
        public virtual bool ReadOnly
        {
            get { return readOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The optional client data instance used for logging purposes.
        /// </summary>
        private IClientData log;

        /// <summary>
        /// Gets or sets the optional client data instance used for logging
        /// purposes.
        /// </summary>
        public virtual IClientData Log
        {
            get { return log; }
            set { log = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new <see cref="IClientData" /> instance that
        /// contains the specified arguments as its data.
        /// </summary>
        /// <param name="readOnly">
        /// Non-zero if the resulting instance should be read-only.
        /// </param>
        /// <param name="args">
        /// The arguments to be contained by the new instance.
        /// </param>
        /// <returns>
        /// The newly created <see cref="IClientData" /> instance.
        /// </returns>
        public static IClientData Pack(
            bool readOnly,       /* in */
            params object[] args /* in */
            )
        {
            return new ClientData(args, readOnly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new <see cref="IClientData" /> instance that
        /// contains the specified strongly typed arguments as its data.
        /// </summary>
        /// <param name="readOnly">
        /// Non-zero if the resulting instance should be read-only.
        /// </param>
        /// <param name="args">
        /// The strongly typed arguments to be contained by the new instance.
        /// </param>
        /// <returns>
        /// The newly created <see cref="IClientData" /> instance.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified client data instance
        /// is read-only.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance to check.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the specified client data instance is read-only; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to obtain the client data associated with the
        /// specified object.
        /// </summary>
        /// <param name="object">
        /// The object from which to obtain the associated client data.
        /// </param>
        /// <param name="validate">
        /// Non-zero if the obtained client data must be non-null in order for
        /// this method to succeed.
        /// </param>
        /// <param name="clientData">
        /// Upon success, receives the client data associated with the
        /// specified object.  Upon failure, this value is null.
        /// </param>
        /// <returns>
        /// True if the client data was successfully obtained; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to extract all the elements contained by the
        /// specified client data instance into a strongly typed array.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance whose contained data should be unpacked.
        /// </param>
        /// <param name="strictType">
        /// Non-zero if every element must match the requested type in order
        /// for this method to succeed.
        /// </param>
        /// <param name="args">
        /// Upon success, receives the array of extracted elements.  Upon
        /// failure, this value is null.
        /// </param>
        /// <returns>
        /// True if the elements were successfully extracted; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to replace the element at the specified index
        /// within the data contained by the specified client data instance.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance whose contained data should be modified.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the element to replace.
        /// </param>
        /// <param name="value">
        /// The new value to store at the specified index.
        /// </param>
        /// <param name="ignoreReadOnly">
        /// Non-zero to permit modification even when the contained data is
        /// marked read-only.
        /// </param>
        /// <param name="strictType">
        /// Non-zero if the existing element at the specified index must match
        /// the requested type in order for this method to succeed.
        /// </param>
        /// <returns>
        /// True if the element was successfully replaced; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to extract the element at the specified index
        /// from the data contained by the specified client data instance.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance whose contained data should be queried.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the element to extract.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the extracted element.  Upon failure, this
        /// value is the default value for its type.
        /// </param>
        /// <returns>
        /// True if the element was successfully extracted; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns a non-null client data instance, creating a new
        /// empty one if the specified instance is null or empty.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance to return if it is neither null nor empty.
        /// </param>
        /// <returns>
        /// The specified client data instance, or a newly created instance if
        /// the specified one was null or empty.
        /// </returns>
        public static IClientData MaybeCreate(
            IClientData clientData /* in */
            )
        {
            return IsNullOrEmpty(clientData) ?
                new ClientData(null) : clientData;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method wraps the specified data within a new client data
        /// instance.  If the specified client data instance already contains
        /// data, both it and the new data are wrapped together; otherwise, a
        /// new instance containing only the new data is returned.
        /// </summary>
        /// <param name="clientData">
        /// The existing client data instance, if any, to be wrapped along with
        /// the new data.
        /// </param>
        /// <param name="data">
        /// The new data to be wrapped.
        /// </param>
        /// <returns>
        /// The newly created client data instance.
        /// </returns>
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

        /// <summary>
        /// This method unwraps a client data instance previously created by
        /// <see cref="WrapOrReplace" />, returning the inner client data
        /// instance and its associated data.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance to unwrap.
        /// </param>
        /// <param name="data">
        /// Upon return, receives the data associated with the unwrapped
        /// instance, or the contained data if the instance was not wrapping
        /// anything.
        /// </param>
        /// <returns>
        /// The inner (wrapped) client data instance, or the original instance
        /// if it was not wrapping anything.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified client data instance
        /// contains any actual data.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance to check.
        /// </param>
        /// <returns>
        /// True if the specified client data instance contains actual data;
        /// otherwise, false.
        /// </returns>
        private static bool HasData(
            IClientData clientData /* in */
            )
        {
            object data = null;

            return HasData(clientData, ref data);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified client data instance
        /// contains any actual data and, if so, returns that data.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance to check.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the data contained by the specified client
        /// data instance.  Upon failure, this value is unchanged.
        /// </param>
        /// <returns>
        /// True if the specified client data instance contains actual data;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified client data instance
        /// is null or represents the reserved "empty" instance.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance to check.
        /// </param>
        /// <returns>
        /// True if the specified client data instance is null or empty;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the name of the type of the data contained by
        /// the specified client data instance.
        /// </summary>
        /// <param name="clientData">
        /// The client data instance whose contained data type name is
        /// requested.
        /// </param>
        /// <param name="nullTypeName">
        /// The type name to return when the client data instance is null or
        /// contains null data.
        /// </param>
        /// <param name="proxyTypeName">
        /// The type name to return when the contained data is a transparent
        /// proxy.
        /// </param>
        /// <param name="wrap">
        /// Non-zero if the returned type name should be wrapped for display.
        /// </param>
        /// <returns>
        /// The name of the type of the contained data.
        /// </returns>
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
        /// <summary>
        /// The cached string representation of this instance, or null if it
        /// has not yet been computed.
        /// </summary>
        private string cachedToString;

        /// <summary>
        /// This method returns a string representation of this instance,
        /// including its read-only state, its type, and its hash code.
        /// </summary>
        /// <returns>
        /// A string representation of this instance.
        /// </returns>
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
