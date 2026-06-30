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
    /// <summary>
    /// This class provides a simple, in-memory implementation of the
    /// <see cref="IDataRecord" /> interface backed by parallel arrays of field
    /// names, values, type names, and field types.  It exposes a single record
    /// (row) of tabular data so its fields can be read by name or by ordinal
    /// index using the standard ADO.NET data record accessors.
    /// </summary>
    [ObjectId("fe62507b-4064-497d-8a98-c8dffdfedc10")]
    internal sealed class DataRecord : IDataRecord
    {
        #region Private Data
        /// <summary>
        /// The array of field names, indexed by ordinal position.
        /// </summary>
        private string[] names;

        /// <summary>
        /// The array of field values, indexed by ordinal position.
        /// </summary>
        private object[] values;

        /// <summary>
        /// The array of field type names, indexed by ordinal position.
        /// </summary>
        private string[] typeNames;

        /// <summary>
        /// The array of field types, indexed by ordinal position.
        /// </summary>
        private Type[] types;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of fields in this record (the common length of the
        /// backing arrays).
        /// </summary>
        private int length;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class with empty (reset) backing
        /// arrays.
        /// </summary>
        private DataRecord()
        {
            ResetArrays();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class from the specified collections
        /// of field names, values, type names, and field types.
        /// </summary>
        /// <param name="names">
        /// The collection of field names.  This parameter may be null.
        /// </param>
        /// <param name="values">
        /// The collection of field values.  This parameter may be null.
        /// </param>
        /// <param name="typeNames">
        /// The collection of field type names.  This parameter may be null.
        /// </param>
        /// <param name="types">
        /// The collection of field types.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method resets all of the backing arrays to null and sets the
        /// field count to its invalid sentinel value.
        /// </summary>
        private void ResetArrays()
        {
            names = null;
            values = null;
            typeNames = null;
            types = null;

            length = Length.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the backing arrays from the specified
        /// collections and then normalizes their lengths so that all arrays
        /// share a common length.
        /// </summary>
        /// <param name="names">
        /// The collection of field names.  This parameter may be null.
        /// </param>
        /// <param name="values">
        /// The collection of field values.  This parameter may be null.
        /// </param>
        /// <param name="typeNames">
        /// The collection of field type names.  This parameter may be null.
        /// </param>
        /// <param name="types">
        /// The collection of field types.  This parameter may be null.
        /// </param>
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

        /// <summary>
        /// This method ensures that all of the backing arrays have the same
        /// length, growing or allocating each array as needed so that every
        /// array matches the largest array length, and records that length as
        /// the field count.
        /// </summary>
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

        /// <summary>
        /// This method validates that the backing arrays are present and have
        /// consistent lengths and, optionally, that the specified ordinal index
        /// is within range, throwing an exception when any check fails.
        /// </summary>
        /// <param name="i">
        /// The ordinal index to range-check, or null to skip the index check.
        /// </param>
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

        /// <summary>
        /// This method searches the field names for the specified name using an
        /// ordinal (case-sensitive) comparison and returns its ordinal index.
        /// </summary>
        /// <param name="name">
        /// The field name to search for.
        /// </param>
        /// <returns>
        /// The ordinal index of the matching field, or an invalid index when no
        /// field with the specified name is found.
        /// </returns>
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
        /// <summary>
        /// Gets the number of fields in the current record.
        /// </summary>
        public int FieldCount
        {
            get { CheckArrays(null); return length; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the specified field as a boolean.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The boolean value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a byte.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The byte value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method reads a stream of bytes from the specified field into
        /// the supplied buffer, starting at the given offsets.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <param name="fieldOffset">
        /// The offset within the field value at which to begin reading.
        /// </param>
        /// <param name="buffer">
        /// The buffer into which the bytes are copied.
        /// </param>
        /// <param name="bufferOffset">
        /// The offset within <paramref name="buffer" /> at which to begin
        /// writing.
        /// </param>
        /// <param name="length">
        /// The number of bytes to copy.
        /// </param>
        /// <returns>
        /// The number of bytes copied.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a character.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The character value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method reads a stream of characters from the specified field
        /// into the supplied buffer, starting at the given offsets.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <param name="fieldOffset">
        /// The offset within the field value at which to begin reading.
        /// </param>
        /// <param name="buffer">
        /// The buffer into which the characters are copied.
        /// </param>
        /// <param name="bufferOffset">
        /// The offset within <paramref name="buffer" /> at which to begin
        /// writing.
        /// </param>
        /// <param name="length">
        /// The number of characters to copy.
        /// </param>
        /// <returns>
        /// The number of characters copied.
        /// </returns>
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

        /// <summary>
        /// This method gets a data reader for the specified field.  This
        /// implementation does not provide nested data readers.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// Always returns null, as there is no associated data reader.
        /// </returns>
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

        /// <summary>
        /// This method gets the data type name of the specified field.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The data type name of the specified field.
        /// </returns>
        public string GetDataTypeName(
            int i /* in */
            )
        {
            CheckArrays(i);

            return typeNames[i];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the specified field as a
        /// <see cref="DateTime" />.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The <see cref="DateTime" /> value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a decimal.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The decimal value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a double.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The double value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the <see cref="Type" /> of the specified field.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The <see cref="Type" /> of the specified field.
        /// </returns>
        public Type GetFieldType(
            int i /* in */
            )
        {
            CheckArrays(i);

            return types[i];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the specified field as a
        /// single-precision floating-point number.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The single-precision floating-point value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a
        /// <see cref="Guid" />.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The <see cref="Guid" /> value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a 16-bit signed
        /// integer.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The 16-bit signed integer value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a 32-bit signed
        /// integer.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The 32-bit signed integer value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as a 64-bit signed
        /// integer.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The 64-bit signed integer value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the name of the specified field.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The name of the specified field.
        /// </returns>
        public string GetName(
            int i /* in */
            )
        {
            CheckArrays(i);

            return names[i];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the ordinal index of the field with the specified
        /// name.
        /// </summary>
        /// <param name="name">
        /// The name of the field to locate.
        /// </param>
        /// <returns>
        /// The ordinal index of the named field, or an invalid index when no
        /// field with the specified name is found.
        /// </returns>
        public int GetOrdinal(
            string name /* in */
            )
        {
            CheckArrays(null);

            return FindName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the specified field as a string.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The string value of the specified field.
        /// </returns>
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

        /// <summary>
        /// This method gets the value of the specified field as an object.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// The value of the specified field.
        /// </returns>
        public object GetValue(
            int i /* in */
            )
        {
            CheckArrays(i);

            return values[i];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the field values of the current record into the
        /// supplied array.
        /// </summary>
        /// <param name="values">
        /// The array into which the field values are copied.
        /// </param>
        /// <returns>
        /// The number of field values copied into <paramref name="values" />.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified field is set to the
        /// database null value.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field.
        /// </param>
        /// <returns>
        /// True if the specified field is set to the database null value;
        /// otherwise, false.
        /// </returns>
        public bool IsDBNull(
            int i /* in */
            )
        {
            CheckArrays(i);

            return (values[i] == DBNull.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the value of the field with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the field whose value is to be retrieved.
        /// </param>
        /// <returns>
        /// The value of the field with the specified name.
        /// </returns>
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

        /// <summary>
        /// Gets the value of the field at the specified ordinal index.
        /// </summary>
        /// <param name="i">
        /// The ordinal index of the field whose value is to be retrieved.
        /// </param>
        /// <returns>
        /// The value of the field at the specified ordinal index.
        /// </returns>
        public object this[int i]
        {
            get { CheckArrays(i); return values[i]; }
        }
        #endregion
    }
}
