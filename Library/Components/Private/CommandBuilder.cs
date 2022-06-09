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
    [ObjectId("cc10a3eb-0433-4f1d-abdf-ea46ed7cccb0")]
    internal sealed class CommandBuilder
    {
        #region Private Constants
        //
        // NOTE: This is the maximum possible capacity for a command string.
        //       Using a zero here means there is no limit except the ones
        //       imposed by the .NET Framework itself.
        //
        private static int MaximumCapacity = 0; /* READ-WRITE */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This must be the same value that the Flatten method would
        //       return for an empty result list.
        //
        private static readonly object EmptyValue = String.Empty;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private ResultList results;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
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
        public static CommandBuilder Create()
        {
            return new CommandBuilder();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
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

        private string Flatten()
        {
            if (results == null)
                return null;

            long capacity = EstimateCapacity();
            Result error = null;

            if (!IsCapacityOk(capacity, MaximumCapacity, ref error))
                throw new ScriptEngineException(error);

            StringBuilder builder = StringOps.NewStringBuilder((int)capacity);

            foreach (Result result in results)
            {
                if (result == null) continue;
                builder.Append(result);
            }

            return builder.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Result Limit Methods
#if RESULT_LIMITS
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
        public void Clear()
        {
            if (results == null)
                return;

            results.Clear();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            Result result /* in */
            )
        {
            results.Add(result); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string text,    /* in */
            int startIndex, /* in */
            int length      /* in */
            )
        {
            results.Add(text.Substring(startIndex, length)); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

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
