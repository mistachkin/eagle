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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e0dbe036-00be-476f-817b-38ff751cbd97")]
    public sealed class TraceList : List<ITrace>
    {
        #region Public Constructors
        public TraceList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public TraceList(
            IEnumerable<ITrace> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public TraceList(
            IEnumerable<TraceCallback> collection
            )
        {
            AddRange(null, TraceFlags.None, null, collection);
        }

        ///////////////////////////////////////////////////////////////////////

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
        internal void AddRange(
            IDictionary<string, _Wrappers.Trace> dictionary
            )
        {
            foreach (KeyValuePair<string, _Wrappers.Trace> pair
                    in dictionary)
            {
                _Wrappers.Trace trace = pair.Value;

                if (trace == null)
                    continue;

                this.Add(trace);
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<ITrace>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
