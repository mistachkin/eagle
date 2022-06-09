/*
 * PolicyList.cs --
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
    [ObjectId("ff141be4-aa17-48da-ac23-936bb052c448")]
    public sealed class PolicyList : List<IPolicy>
    {
        #region Public Constructors
        public PolicyList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PolicyList(
            IEnumerable<IPolicy> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PolicyList(
            IEnumerable<ExecuteCallback> collection
            )
        {
            AddRange(null, PolicyFlags.None, null, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public PolicyList(
            IClientData clientData,
            PolicyFlags policyFlags,
            IPlugin plugin,
            IEnumerable<ExecuteCallback> collection
            )
        {
            AddRange(clientData, policyFlags, plugin, collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void AddRange(
            IClientData clientData,
            PolicyFlags policyFlags,
            IPlugin plugin,
            IEnumerable<ExecuteCallback> collection
            )
        {
            foreach (ExecuteCallback item in collection)
            {
                if (item != null)
                {
                    Result error = null;

                    IPolicy policy = PolicyOps.NewCore(
                        item, clientData, policyFlags, plugin, ref error);

                    if (policy == null)
                    {
                        DebugOps.Complain(ReturnCode.Error, error);
                        continue;
                    }

                    this.Add(policy);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<IPolicy>.ListToString(
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
