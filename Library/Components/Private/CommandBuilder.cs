/*
 * CommandBuilder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class accumulates result fragments and flattens them into a single
    /// command result, optionally enforcing a maximum capacity.  When the
    /// accumulated result consists of a single value, that value may be
    /// returned directly (preserving its type) rather than being converted to a
    /// string.
    /// </summary>
    [ObjectId("cc10a3eb-0433-4f1d-abdf-ea46ed7cccb0")]
    internal sealed class CommandBuilder
    {
        #region Private Constants
        //
        // NOTE: This is the maximum possible capacity for a command string.
        //       Using a zero here means there is no limit except the ones
        //       imposed by the .NET Framework itself.
        //
        /// <summary>
        /// The maximum possible capacity, in characters, for a command string.
        /// A value of zero means there is no limit except those imposed by the
        /// .NET Framework itself.
        /// </summary>
        private static int MaximumCapacity = 0; /* READ-WRITE */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This must be the same value that the Flatten method would
        //       return for an empty result list.
        //
        /// <summary>
        /// The value returned when there is nothing to flatten.  This must be
        /// the same value that the <c>Flatten</c> method would return for an
        /// empty result list.
        /// </summary>
        private static readonly object EmptyValue = String.Empty;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The list of accumulated result fragments that will be flattened into
        /// the final command result.
        /// </summary>
        private ResultList results;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty command builder with its internal result list
        /// configured to add nested result lists as single items.
        /// </summary>
        private CommandBuilder()
        {
            //
            // BUGFIX: When adding items into this internal list, do not
            //         add new (ResultList) items as a range of values,
            //         add them as a single item.
            //
            results = new ResultList(
                ResultFlags.DefaultListMask | ResultFlags.NoAddRange);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new, empty command builder instance.
        /// </summary>
        /// <returns>
        /// The newly created command builder instance.
        /// </returns>
        public static CommandBuilder Create()
        {
            return new CommandBuilder();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method checks whether a requested capacity is valid, that is,
        /// non-negative, within the optional maximum, and within the range of a
        /// 32-bit integer.
        /// </summary>
        /// <param name="capacity">
        /// The requested capacity, in characters, to be validated.
        /// </param>
        /// <param name="maximumCapacity">
        /// The maximum allowed capacity, in characters, or zero for no limit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing why
        /// the capacity is invalid.
        /// </param>
        /// <returns>
        /// True if the requested capacity is valid; otherwise, false.
        /// </returns>
        private static bool IsCapacityOk(
            long capacity,        /* in */
            long maximumCapacity, /* in */
            ref Result error      /* out */
            )
        {
            if ((capacity < 0) ||
                ((maximumCapacity != 0) && (capacity > maximumCapacity)))
            {
                error = String.Format(
                    "maximum command length of {0} characters exceeded ({1})",
                    maximumCapacity, capacity);

                return false;
            }

            if (capacity > Int32.MaxValue)
            {
                error = String.Format(
                    "maximum {0} length of {1} characters exceeded ({1})",
                    typeof(Int32), Int32.MaxValue, capacity);

                return false;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method checks whether adding the specified result would keep the
        /// accumulated length within the specified maximum capacity.
        /// </summary>
        /// <param name="maximumCapacity">
        /// The maximum allowed capacity, in characters, or zero for no limit.
        /// </param>
        /// <param name="result">
        /// The result whose length is to be checked.  This parameter may be
        /// null, in which case there is always enough capacity.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing why
        /// there is not enough capacity.
        /// </param>
        /// <returns>
        /// True if there is enough capacity; otherwise, false.
        /// </returns>
        public static bool StaticHaveEnoughCapacity(
            int maximumCapacity, /* in */
            Result result,       /* in */
            ref Result error     /* out */
            )
        {
            if (maximumCapacity == 0) /* NOTE: No limit. */
                return true;

            if (result == null) /* NOTE: You can always add nothing. */
                return true;

            long capacity = result.Length;

            if (!IsCapacityOk(capacity, maximumCapacity, ref error))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether adding the specified number of characters
        /// would keep the accumulated length within the specified maximum
        /// capacity.
        /// </summary>
        /// <param name="maximumCapacity">
        /// The maximum allowed capacity, in characters, or zero for no limit.
        /// </param>
        /// <param name="length">
        /// The number of characters to be added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing why
        /// there is not enough capacity.
        /// </param>
        /// <returns>
        /// True if there is enough capacity; otherwise, false.
        /// </returns>
        public static bool StaticHaveEnoughCapacity(
            int maximumCapacity, /* in */
            int length,          /* in */
            ref Result error     /* out */
            )
        {
            if (maximumCapacity == 0) /* NOTE: No limit. */
                return true;

            long capacity = length;

            if (!IsCapacityOk(capacity, maximumCapacity, ref error))
                return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method estimates the total capacity, in characters, required to
        /// flatten all of the accumulated result fragments.
        /// </summary>
        /// <returns>
        /// The estimated capacity, in characters.
        /// </returns>
        private long EstimateCapacity()
        {
            long capacity = 0;

            if (results == null)
                return capacity;

            foreach (Result result in results)
            {
                if (result == null) continue;
                capacity += result.Length;
            }

            return capacity;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method concatenates all of the accumulated result fragments into
        /// a single string, enforcing the maximum capacity, if any.
        /// </summary>
        /// <returns>
        /// The flattened string, or null if there are no accumulated result
        /// fragments.
        /// </returns>
        private string Flatten()
        {
            if (results == null)
                return null;

            long capacity = EstimateCapacity();
            Result error = null;

            if (!IsCapacityOk(capacity, MaximumCapacity, ref error))
                throw new ScriptEngineException(error);

            StringBuilder builder = StringBuilderFactory.Create((int)capacity);

            foreach (Result result in results)
            {
                if (result == null) continue;
                builder.Append(result);
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Result Limit Methods
#if RESULT_LIMITS
        /// <summary>
        /// This method checks whether adding the specified result to the already
        /// accumulated result fragments would keep the total length within the
        /// specified maximum capacity.
        /// </summary>
        /// <param name="maximumCapacity">
        /// The maximum allowed capacity, in characters, or zero for no limit.
        /// </param>
        /// <param name="result">
        /// The result whose length is to be checked.  This parameter may be
        /// null, in which case there is always enough capacity.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing why
        /// there is not enough capacity.
        /// </param>
        /// <returns>
        /// True if there is enough capacity; otherwise, false.
        /// </returns>
        public bool HaveEnoughCapacity(
            int maximumCapacity, /* in */
            Result result,       /* in */
            ref Result error     /* out */
            )
        {
            if (maximumCapacity == 0) /* NOTE: No limit. */
                return true;

            if (result == null) /* NOTE: You can always add nothing. */
                return true;

            long capacity = EstimateCapacity();

            capacity += result.Length;

            if (!IsCapacityOk(capacity, maximumCapacity, ref error))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether adding the specified number of characters
        /// to the already accumulated result fragments would keep the total
        /// length within the specified maximum capacity.
        /// </summary>
        /// <param name="maximumCapacity">
        /// The maximum allowed capacity, in characters, or zero for no limit.
        /// </param>
        /// <param name="length">
        /// The number of characters to be added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message describing why
        /// there is not enough capacity.
        /// </param>
        /// <returns>
        /// True if there is enough capacity; otherwise, false.
        /// </returns>
        public bool HaveEnoughCapacity(
            int maximumCapacity, /* in */
            int length,          /* in */
            ref Result error     /* out */
            )
        {
            if (maximumCapacity == 0) /* NOTE: No limit. */
                return true;

            long capacity = EstimateCapacity();

            capacity += length;

            if (!IsCapacityOk(capacity, maximumCapacity, ref error))
                return false;

            return true;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method discards all of the accumulated result fragments,
        /// resetting the command builder to its empty state.
        /// </summary>
        public void Clear()
        {
            if (results == null)
                return;

            results.Clear();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified result to the accumulated result
        /// fragments.
        /// </summary>
        /// <param name="result">
        /// The result to be appended.
        /// </param>
        public void Add(
            Result result /* in */
            )
        {
            results.Add(result); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a substring of the specified text to the
        /// accumulated result fragments.
        /// </summary>
        /// <param name="text">
        /// The text containing the substring to be appended.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based starting character position of the substring within
        /// <paramref name="text" />.
        /// </param>
        /// <param name="length">
        /// The number of characters in the substring.
        /// </param>
        public void Add(
            string text,    /* in */
            int startIndex, /* in */
            int length      /* in */
            )
        {
            results.Add(text.Substring(startIndex, length)); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces the final command result from the accumulated
        /// result fragments.  When the accumulated result consists of a single
        /// value, that value may be returned directly (preserving its type)
        /// rather than being flattened to a string.
        /// </summary>
        /// <returns>
        /// The final command result, which may be a single typed value or a
        /// flattened string.
        /// </returns>
        public object GetResult()
        {
            ResultList localResults = results;

        retryList:

            if (localResults == null)
                return null;

            if (localResults.Count == 0)
                return EmptyValue;

            if (localResults.Count == 1)
            {
                Result result = localResults[0];

                if (result != null)
                {
                    object value = result.Value;

                retryValue:

                    ///////////////////////////////////////////////////////////
                    // TIER #0: These types are very trivial.
                    ///////////////////////////////////////////////////////////

                    if (value == null)
                        return EmptyValue;

                    ///////////////////////////////////////////////////////////
                    // TIER #1: These types are very common.
                    ///////////////////////////////////////////////////////////

                    if (value is ValueType)
                        return value;

                    if (value is string)
                        return value;

                    if (value is StringList)
                        return value;

                    if (value is ObjectDictionary)
                        return value;

                    ///////////////////////////////////////////////////////////
                    // TIER #2: These types are common.
                    ///////////////////////////////////////////////////////////

                    if (value is StringPairList)
                        return value;

                    if (value is StringBuilder)
                        return ((StringBuilder)value).ToString(); /* FLATTEN */

                    ///////////////////////////////////////////////////////////
                    // TIER #3: These types are uncommon.
                    ///////////////////////////////////////////////////////////

                    if (value is ByteList)
                        return value;

                    if (value is Exception)
                        return value;

                    if (value is Uri)
                        return value;

                    if (value is Version)
                        return value;

                    ///////////////////////////////////////////////////////////
                    // TIER #4: These types are wrapped.
                    ///////////////////////////////////////////////////////////

                    if (value is Argument)
                    {
                        value = ((Argument)value).Value; /* UNWRAP */
                        goto retryValue;
                    }

                    if (value is Result)
                    {
                        value = ((Result)value).Value; /* UNWRAP */
                        goto retryValue;
                    }

                    ///////////////////////////////////////////////////////////
                    // TIER #5: These types are composite.
                    ///////////////////////////////////////////////////////////

                    if (value is ResultList)
                    {
                        localResults = (ResultList)value; /* NESTED */
                        goto retryList;
                    }
                }
            }

            return Flatten();
        }
        #endregion
    }
}
