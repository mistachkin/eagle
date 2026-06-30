/*
 * ProcessStringBuilderDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Diagnostics;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps process instances to the
    /// <see cref="StringBuilder" /> instances used to accumulate their output
    /// data.  It extends the generic process dictionary with helpers for
    /// creating, appending to, querying, and removing the accumulated data for
    /// a process.
    /// </summary>
    [ObjectId("8fd6cd25-318b-44a6-8d2e-a4466e23617f")]
    internal sealed class ProcessStringBuilderDictionary :
            ProcessDictionary<StringBuilder>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ProcessStringBuilderDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method attempts to obtain the string builder associated with
        /// the specified process.
        /// </summary>
        /// <param name="process">
        /// The process whose associated string builder is sought.
        /// </param>
        /// <param name="nullOk">
        /// Non-zero if a null string builder value is acceptable; otherwise, a
        /// null value is treated as a failure.
        /// </param>
        /// <param name="builder">
        /// Upon success, this will contain the string builder associated with
        /// the specified process; otherwise, this will contain null.
        /// </param>
        /// <returns>
        /// True if the string builder was successfully obtained; otherwise,
        /// false.
        /// </returns>
        private bool TryGetBuilder(
            Process process,
            bool nullOk,
            out StringBuilder builder
            )
        {
            if (process == null)
            {
                builder = null;
                return false;
            }

            if (!TryGetValue(process, out builder))
                return false;

            if (!nullOk && (builder == null))
                return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method begins accumulating output data for the specified
        /// process by creating a new string builder for it, optionally with the
        /// specified initial capacity.  If a string builder already exists for
        /// the process, no action is taken.
        /// </summary>
        /// <param name="process">
        /// The process for which a new string builder should be created.
        /// </param>
        /// <param name="capacity">
        /// The initial capacity of the new string builder.  This parameter may
        /// be null, in which case a default capacity is used.
        /// </param>
        /// <returns>
        /// True if a new string builder was created; otherwise, false.
        /// </returns>
        public bool NewData(
            Process process,
            int? capacity
            )
        {
            StringBuilder builder;

            if (TryGetBuilder(process, true, out builder))
                return false;

            /* NO RESULT */
            Add(process, (capacity != null) ?
                StringBuilderFactory.Create((int)capacity) :
                StringBuilderFactory.Create());

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a line of output data to the string builder
        /// associated with the specified process.
        /// </summary>
        /// <param name="process">
        /// The process whose accumulated data should be appended to.
        /// </param>
        /// <param name="data">
        /// The data to append, as a new line, to the accumulated data.
        /// </param>
        /// <returns>
        /// True if the data was successfully appended; otherwise, false.
        /// </returns>
        public bool AppendData(
            Process process,
            string data
            )
        {
            StringBuilder builder;

            if (!TryGetBuilder(process, false, out builder))
                return false;

            builder.AppendLine(data);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the accumulated output data for the specified
        /// process.
        /// </summary>
        /// <param name="process">
        /// The process whose accumulated data should be retrieved.
        /// </param>
        /// <returns>
        /// The accumulated data for the process, or null if no data is
        /// available.
        /// </returns>
        public string GetData(
            Process process
            )
        {
            StringBuilder builder;

            if (!TryGetBuilder(process, false, out builder))
                return null;

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stops accumulating output data for the specified
        /// process, releasing the associated string builder and removing the
        /// entry from the dictionary.
        /// </summary>
        /// <param name="process">
        /// The process whose accumulated data should be removed.
        /// </param>
        /// <returns>
        /// True if the entry was successfully removed; otherwise, false.
        /// </returns>
        public bool RemoveData(
            Process process
            )
        {
            StringBuilder builder;

            if (!TryGetBuilder(process, true, out builder))
                return false;

            StringBuilderCache.Release(ref builder);

            return Remove(process);
        }
        #endregion
    }
}
