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
    [ObjectId("30de3027-a7fc-4433-840e-d968ce4847e4")]
    public interface IPolicyData : IIdentifier, IHavePlugin, IWrapperData
    {
        //
        // NOTE: The fully qualified type name for this policy (not including
        //       the assembly name).
        //
        string TypeName { get; set; }

        //
        // NOTE: The name of the policy method.
        //
        string MethodName { get; set; }

        //
        // NOTE: The binding flags for the policy method.
        //
        BindingFlags BindingFlags { get; set; }

        //
        // NOTE: The flags for the policy method.
        //
        MethodFlags MethodFlags { get; set; }

        //
        // NOTE: The flags for the policy.
        //
        PolicyFlags PolicyFlags { get; set; }
    }
}
