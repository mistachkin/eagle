/*
 * PolicyData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the identity and metadata for a policy that
    /// can be added to and evaluated by an Eagle interpreter.  It composes
    /// the unique identity (<see cref="IIdentifier" />), the owning plugin
    /// (<see cref="IHavePlugin" />), the wrapper bookkeeping
    /// (<see cref="IWrapperData" />), and the type and name information
    /// (<see cref="ITypeAndName" />).  The remaining members describe the
    /// managed method that implements the policy and how it is bound and
    /// invoked.
    /// </summary>
    [ObjectId("30de3027-a7fc-4433-840e-d968ce4847e4")]
    public interface IPolicyData : IIdentifier, IHavePlugin, IWrapperData, ITypeAndName
    {
        //
        // NOTE: The name of the policy method.
        //
        /// <summary>
        /// Gets or sets the name of the method that implements this policy.
        /// </summary>
        string MethodName { get; set; }

        //
        // NOTE: The binding flags for the policy method.
        //
        /// <summary>
        /// Gets or sets the reflection binding flags used to locate the
        /// method that implements this policy.
        /// </summary>
        BindingFlags BindingFlags { get; set; }

        //
        // NOTE: The flags for the policy method.
        //
        /// <summary>
        /// Gets or sets the flags that control how the method implementing
        /// this policy is invoked.
        /// </summary>
        MethodFlags MethodFlags { get; set; }

        //
        // NOTE: The flags for the policy.
        //
        /// <summary>
        /// Gets or sets the flags that control the behavior of this policy.
        /// </summary>
        PolicyFlags PolicyFlags { get; set; }
    }
}
