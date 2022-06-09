/*
 * ThrowAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

namespace Eagle._Attributes
{
    //
    // NOTE: This attribute is for declaring that certain methods on public
    //       "callback" interfaces are allowed to throw exceptions (for various
    //       definitions of "allowed").  Custom commands and plugins are
    //       officially discouraged from throwing exceptions (i.e. for normal
    //       communication of "exceptional" conditions failure ReturnCode
    //       should be used); however, in the event of an unanticipated
    //       exception or other catastrophic condition, the Interpreter and
    //       Engine components "guarantee" that exceptions will not propogate
    //       outward from methods that are officially allowed to throw
    //       exceptions (i.e. ones officially tagged with this attribute).
    //
    [AttributeUsage(ThrowAttribute.Targets, Inherited = false)]
    [ObjectId("1a4164d2-d5e6-4121-babb-5d2bff960493")]
    public sealed class ThrowAttribute : Attribute
    {
        public const AttributeTargets Targets =
            AttributeTargets.Constructor | AttributeTargets.Method |
            AttributeTargets.Property | AttributeTargets.Field |
            AttributeTargets.Event | AttributeTargets.Delegate;

        ///////////////////////////////////////////////////////////////////////

        public ThrowAttribute(bool @throw)
        {
            this.@throw = @throw;
        }

        ///////////////////////////////////////////////////////////////////////

        public ThrowAttribute(string value)
        {
            @throw = bool.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private bool @throw;
        public bool Throw
        {
            get { return @throw; }
        }
    }
}
