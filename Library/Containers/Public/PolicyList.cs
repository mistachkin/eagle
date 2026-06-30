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
    /// <summary>
    /// This class represents an ordered collection of policy instances.  It
    /// provides constructors for building the collection from existing policy
    /// instances or by creating new policies from execution callbacks, as well
    /// as methods for producing a string representation of the collection.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("ff141be4-aa17-48da-ac23-936bb052c448")]
    public sealed class PolicyList : List<IPolicy>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty policy collection.
        /// </summary>
        public PolicyList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a policy collection containing the policy instances
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of policy instances whose elements are copied into
        /// the new collection.
        /// </param>
        public PolicyList(
            IEnumerable<IPolicy> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a policy collection by creating new policy instances from
        /// the specified collection of execution callbacks, using default
        /// policy settings.
        /// </summary>
        /// <param name="collection">
        /// The collection of execution callbacks used to create the policy
        /// instances added to the new collection.
        /// </param>
        public PolicyList(
            IEnumerable<ExecuteCallback> collection
            )
        {
            AddRange(null, PolicyFlags.None, null, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a policy collection by creating new policy instances from
        /// the specified collection of execution callbacks, using the supplied
        /// client data, policy flags, and plugin.
        /// </summary>
        /// <param name="clientData">
        /// The client data to associate with each created policy.  This
        /// parameter may be null.
        /// </param>
        /// <param name="policyFlags">
        /// The policy flags to associate with each created policy.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with each created policy.  This parameter may
        /// be null.
        /// </param>
        /// <param name="collection">
        /// The collection of execution callbacks used to create the policy
        /// instances added to the new collection.
        /// </param>
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
        /// <summary>
        /// This method creates a new policy instance from each execution
        /// callback in the specified collection and adds it to this collection.
        /// Callbacks that are null are skipped, and any failure to create a
        /// policy is reported via the debugging subsystem before continuing
        /// with the remaining callbacks.
        /// </summary>
        /// <param name="clientData">
        /// The client data to associate with each created policy.  This
        /// parameter may be null.
        /// </param>
        /// <param name="policyFlags">
        /// The policy flags to associate with each created policy.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with each created policy.  This parameter may
        /// be null.
        /// </param>
        /// <param name="collection">
        /// The collection of execution callbacks used to create the policy
        /// instances added to this collection.
        /// </param>
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
        /// <summary>
        /// This method produces a string representation of this collection,
        /// optionally filtering the included elements using the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match the elements to include, or null to
        /// include every element.
        /// </param>
        /// <param name="noCase">
        /// When non-zero, pattern matching is performed in a case-insensitive
        /// manner.
        /// </param>
        /// <returns>
        /// The string representation of the matching elements in this
        /// collection.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<IPolicy>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string representation of this collection that
        /// includes every element.
        /// </summary>
        /// <returns>
        /// The string representation of all the elements in this collection.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
