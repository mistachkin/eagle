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
    //       Engine components "guarantee" that exceptions will not propagate
    //       outward from methods that are officially allowed to throw
    //       exceptions (i.e. ones officially tagged with this attribute).
    //
    /// <summary>
    /// This class implements an attribute used to declare whether the member
    /// it marks is officially allowed to throw exceptions.  It is applied to
    /// methods and other members on public callback interfaces to indicate
    /// that the Interpreter and Engine components will not guarantee
    /// suppression of exceptions originating from them.
    /// </summary>
    [AttributeUsage(ThrowAttribute.Targets, Inherited = false)]
    [ObjectId("1a4164d2-d5e6-4121-babb-5d2bff960493")]
    public sealed class ThrowAttribute : Attribute
    {
        /// <summary>
        /// The set of attribute targets to which this attribute may be
        /// applied.
        /// </summary>
        public const AttributeTargets Targets =
            AttributeTargets.Constructor | AttributeTargets.Method |
            AttributeTargets.Property | AttributeTargets.Field |
            AttributeTargets.Event | AttributeTargets.Delegate;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified flag
        /// indicating whether the marked member is allowed to throw
        /// exceptions.
        /// </summary>
        /// <param name="throw">
        /// Non-zero if the marked member is officially allowed to throw
        /// exceptions.
        /// </param>
        public ThrowAttribute(bool @throw)
        {
            this.@throw = @throw;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified flag,
        /// in its string form, indicating whether the marked member is allowed
        /// to throw exceptions.
        /// </summary>
        /// <param name="value">
        /// The string representation of the flag indicating whether the marked
        /// member is allowed to throw exceptions.  An exception is thrown if
        /// this value cannot be parsed as a boolean.
        /// </param>
        public ThrowAttribute(string value)
        {
            @throw = bool.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the marked member is officially allowed to throw
        /// exceptions.
        /// </summary>
        private bool @throw;
        /// <summary>
        /// Gets a value indicating whether the marked member is officially
        /// allowed to throw exceptions.
        /// </summary>
        public bool Throw
        {
            get { return @throw; }
        }
    }
}
