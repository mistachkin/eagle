/*
 * DataRecord.cs --
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
using System.Data;
using Eagle._Attributes;
using Eagle._Constants;
using Eagle._Containers.Private;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("fe62507b-4064-497d-8a98-c8dffdfedc10")]
    internal sealed class DataRecord : IDataRecord
    {
        #region Private Data
        private string[] names;
        private object[] values;
        private string[] typeNames;
        private Type[] types;

        ///////////////////////////////////////////////////////////////////////

        private int length;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private DataRecord()
        {
            ResetArrays();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public DataRecord(
            IEnumerable<string> names,     /* in */
            IEnumerable<object> values,    /* in */
            IEnumerable<string> typeNames, /* in */
            IEnumerable<Type> types        /* in */
            )
            : this()
        {
            InitializeArrays(names, values, typeNames, types);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void ResetArrays()
        {
            names = null;
            values = null;
            typeNames = null;
            types = null;

            length = Length.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        private void InitializeArrays(
            IEnumerable<string> names,     /* in */
            IEnumerable<object> values,    /* in */
            IEnumerable<string> typeNames, /* in */
            IEnumerable<Type> types        /* in */
            )
        {
            ArrayOps.Initialize<string>(__makeref(this.names), names);
            ArrayOps.Initialize<object>(__makeref(this.values), values);
            ArrayOps.Initialize<string>(__makeref(this.typeNames), typeNames);
            ArrayOps.Initialize<Type>(__makeref(this.types), types);

            MaybeAdjustArrayLengths();
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybeAdjustArrayLengths()
        {
            int nameLength = (names != null) ? names.Length : 0;
            int valueLength = (values != null) ? values.Length : 0;
            int typeNameLength = (typeNames != null) ? typeNames.Length : 0;
            int typeLength = (types != null) ? types.Length : 0;

            int maximumCount = (int)MathOps.Max(
                nameLength, valueLength, typeNameLength, typeLength);

            if ((names == null) || (nameLength != maximumCount))
            {
                if (names != null)
                {
                    if (nameLength < maximumCount)
                    {
                        Array.Resize(ref names, maximumCount);
                        nameLength = maximumCount;
                    }
                }
                else
                {
                    names = new string[maximumCount];
                    nameLength = maximumCount;
                }
            }

            if ((values == null) || (valueLength != maximumCount))
            {
                if (values != null)
                {
                    if (valueLength < maximumCount)
                    {
                        Array.Resize(ref values, maximumCount);
                        valueLength = maximumCount;
                    }
                }
                else
                {
                    values = new object[maximumCount];
                    valueLength = maximumCount;
                }
            }

            if ((typeNames == null) || (typeNameLength != maximumCount))
            {
                if (typeNames != null)
                {
                    if (typeNameLength < maximumCount)
                    {
                        Array.Resize(ref typeNames, maximumCount);
                        typeNameLength = maximumCount;
                    }
                }
                else
                {
                    typeNames = new string[maximumCount];
                    typeNameLength = maximumCount;
                }
            }

            if ((types == null) || (typeLength != maximumCount))
            {
                if (types != null)
                {
                    if (typeLength < maximumCount)
                    {
                        Array.Resize(ref types, maximumCount);
                        typeLength = maximumCount;
                    }
                }
                else
                {
                    types = new Type[maximumCount];
                    typeLength = maximumCount;
                }
            }

            length = maximumCount;
        }

        ///////////////////////////////////////////////////////////////////////

        private void CheckArrays(
            int? i /* in */
            )
        {
            if (names == null)
                throw new NullReferenceException("invalid names");

            if (values == null)
                throw new NullReferenceException("invalid values");

            if (typeNames == null)
                throw new NullReferenceException("invalid type names");

            if (types == null)
                throw new NullReferenceException("invalid types");

            int nameLength = names.Length;
            int valueLength = values.Length;
            int typeNameLength = typeNames.Length;
            int typeLength = types.Length;

            if ((length != nameLength) ||
                (length != valueLength) ||
                (length != typeNameLength) ||
                (length != typeLength))
            {
                throw new InvalidOperationException(
                    "arrays cannot have different lengths");
            }

            if (i != null)
            {
                if (i < 0)
                {
                    throw new IndexOutOfRangeException(
                        "index cannot be negative");
                }

                if (i >= length)
                {
                    throw new IndexOutOfRangeException(
                        "index cannot exceed field count");
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int FindName(
            string name /* in */
            )
        {
            if (names != null)
            {
                for (int index = 0; index < length; index++)
                {
                    if (SharedStringOps.Equals(
                            name, names[index],
                            StringComparison.Ordinal))
                    {
                        return index;
                    }
                }
            }

            return Index.Invalid;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDataRecord Members
        public int FieldCount
        {
            get { CheckArrays(null); return length; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetBoolean(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is bool)
                return (bool)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public byte GetByte(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is byte)
                return (byte)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public long GetBytes(
            int i,            /* in */
            long fieldOffset, /* in */
            byte[] buffer,    /* in, out */
            int bufferOffset, /* in */
            int length        /* in */
            )
        {
            CheckArrays(i);

            byte[] value = values[i] as byte[];

            if (value == null)
                throw new InvalidCastException();

            Array.Copy(value, fieldOffset, buffer, bufferOffset, length);

            return length;
        }

        ///////////////////////////////////////////////////////////////////////

        public char GetChar(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is char)
                return (char)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public long GetChars(
            int i,            /* in */
            long fieldOffset, /* in */
            char[] buffer,    /* in, out */
            int bufferOffset, /* in */
            int length        /* in */
            )
        {
            CheckArrays(i);

            char[] value = values[i] as char[];

            if (value == null)
                throw new InvalidCastException();

            Array.Copy(value, fieldOffset, buffer, bufferOffset, length);

            return length;
        }

        ///////////////////////////////////////////////////////////////////////

        public IDataReader GetData(
            int i /* in */
            )
        {
            CheckArrays(i);

            //
            // NOTE: There is no associated data reader.
            //
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetDataTypeName(
            int i /* in */
            )
        {
            CheckArrays(i);

            return typeNames[i];
        }

        ///////////////////////////////////////////////////////////////////////

        public DateTime GetDateTime(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is DateTime)
                return (DateTime)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public decimal GetDecimal(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is decimal)
                return (decimal)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public double GetDouble(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is double)
                return (double)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public Type GetFieldType(
            int i /* in */
            )
        {
            CheckArrays(i);

            return types[i];
        }

        ///////////////////////////////////////////////////////////////////////

        public float GetFloat(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is float)
                return (float)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid GetGuid(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is Guid)
                return (Guid)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public short GetInt16(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is short)
                return (short)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetInt32(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is int)
                return (int)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public long GetInt64(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is long)
                return (long)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetName(
            int i /* in */
            )
        {
            CheckArrays(i);

            return names[i];
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetOrdinal(
            string name /* in */
            )
        {
            CheckArrays(null);

            return FindName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetString(
            int i /* in */
            )
        {
            CheckArrays(i);

            object value = values[i];

            if (value is string)
                return (string)value;

            throw new InvalidCastException();
        }

        ///////////////////////////////////////////////////////////////////////

        public object GetValue(
            int i /* in */
            )
        {
            CheckArrays(i);

            return values[i];
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetValues(
            object[] values /* in, out */
            )
        {
            CheckArrays(null);

            int minimumLength = Math.Min(length, values.Length);

            Array.Copy(this.values, values, minimumLength);

            return minimumLength;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDBNull(
            int i /* in */
            )
        {
            CheckArrays(i);

            return (values[i] == DBNull.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public object this[string name]
        {
            get
            {
                CheckArrays(null);

                int i = FindName(name);

                if (i < 0)
                    throw new KeyNotFoundException();

                return values[i];
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public object this[int i]
        {
            get { CheckArrays(i); return values[i]; }
        }
        #endregion
    }
}
