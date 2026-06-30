/*
 * PolicyWrapperDictionary.cs --
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

using PolicyWrapper = Eagle._Wrappers.Policy;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to instances
    /// of the policy wrapper type.  It extends the generic wrapper dictionary
    /// for use with policy wrappers.
    /// </summary>
    [ObjectId("a8c9a48b-28ec-4f8e-b9ba-9c508bee48ae")]
    internal sealed class PolicyWrapperDictionary :
            WrapperDictionary<string, PolicyWrapper>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PolicyWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public PolicyWrapperDictionary(
            IDictionary<string, PolicyWrapper> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
    }
}
