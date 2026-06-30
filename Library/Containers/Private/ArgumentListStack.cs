/*
 * ArgumentListStack.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a stack of argument lists.  It extends the
    /// standard generic stack with the ability to be converted to a string.
    /// </summary>
    [ObjectId("e1b84568-804a-4fba-8b23-c5ce5b7bf878")]
    internal sealed class ArgumentListStack : Stack<ArgumentList>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ArgumentListStack()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// Converts this stack to a string containing its elements.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each element must match in order to be included in
        /// the resulting string.  This parameter may be null, in which case all
        /// elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this stack.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return GenericOps<ArgumentList>.EnumerableToString(
                this, ToStringFlags.None, Characters.SpaceString,
                null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts this stack to a string containing its elements.
        /// </summary>
        /// <returns>
        /// The string representation of this stack.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
