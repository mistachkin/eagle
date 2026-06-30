/*
 * TraceList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Interfaces.Public;

using TraceWrapper = Eagle._Wrappers.Trace;

using TracePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Trace>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a list of variable traces (<see cref="ITrace" />),
    /// each describing a callback invoked when a traced variable is read,
    /// written, or unset.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e0dbe036-00be-476f-817b-38ff751cbd97")]
    public sealed class TraceList : List<ITrace>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty list of traces.
        /// </summary>
        public TraceList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of traces that contains the traces copied from the
        /// specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of traces whose elements are copied into the new
        /// list.
        /// </param>
        public TraceList(
            IEnumerable<ITrace> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of traces by wrapping each trace callback in the
        /// specified collection in a new core trace.
        /// </summary>
        /// <param name="collection">
        /// The collection of trace callbacks to wrap and add to the new list.
        /// </param>
        public TraceList(
            IEnumerable<TraceCallback> collection
            )
        {
            AddRange(null, TraceFlags.None, null, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of traces by wrapping each trace callback in the
        /// specified collection in a new core trace, using the supplied client
        /// data, trace flags, and plugin.
        /// </summary>
        /// <param name="clientData">
        /// The client data to associate with each created trace.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceFlags">
        /// The trace flags to associate with each created trace.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with each created trace.  This parameter may
        /// be null.
        /// </param>
        /// <param name="collection">
        /// The collection of trace callbacks to wrap and add to the new list.
        /// </param>
        public TraceList(
            IClientData clientData,
            TraceFlags traceFlags,
            IPlugin plugin,
            IEnumerable<TraceCallback> collection
            )
        {
            AddRange(clientData, traceFlags, plugin, collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method wraps each trace callback in the specified collection in
        /// a new core trace and adds it to this list, using the supplied client
        /// data, trace flags, and plugin.  Callbacks that cannot be wrapped are
        /// reported and skipped.
        /// </summary>
        /// <param name="clientData">
        /// The client data to associate with each created trace.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceFlags">
        /// The trace flags to associate with each created trace.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with each created trace.  This parameter may
        /// be null.
        /// </param>
        /// <param name="collection">
        /// The collection of trace callbacks to wrap and add to this list.
        /// </param>
        private void AddRange(
            IClientData clientData,
            TraceFlags traceFlags,
            IPlugin plugin,
            IEnumerable<TraceCallback> collection
            )
        {
            foreach (TraceCallback item in collection)
            {
                if (item != null)
                {
                    Result error = null;

                    ITrace trace = ScriptOps.NewCoreTrace(
                        item, clientData, traceFlags, plugin, ref error);

                    if (trace == null)
                    {
                        DebugOps.Complain(ReturnCode.Error, error);
                        continue;
                    }

                    this.Add(trace);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods (Interpreter Class Only)
        /// <summary>
        /// This method adds the non-null trace wrappers from the specified
        /// dictionary to this list.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose trace wrapper values are added to this list.
        /// </param>
        internal void AddRange(
            IDictionary<string, TraceWrapper> dictionary
            )
        {
            foreach (TracePair pair in dictionary)
            {
                TraceWrapper trace = pair.Value;

                if (trace == null)
                    continue;

                this.Add(trace);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this list contains a trace whose
        /// callback is equal to the specified callback, performing a linear
        /// search.  Traces that are transparent proxies are skipped.
        /// </summary>
        /// <param name="item">
        /// The trace callback to search for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if a trace with a matching callback is found; otherwise, false.
        /// </returns>
        internal bool Contains( /* O(N) */
            TraceCallback item
            )
        {
            if (item != null)
            {
                foreach (ITrace trace in this)
                {
                    if (trace == null)
                        continue;

                    if (AppDomainOps.IsTransparentProxy(trace))
                        continue;

                    TraceCallback callback = trace.Callback;

                    if (callback == null)
                        continue;

                    if (callback.Equals(item))
                        return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method returns a string representation of this list, with the
        /// traces separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the traces included in the
        /// result.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<ITrace>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this list, with the
        /// traces separated by spaces.
        /// </summary>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
