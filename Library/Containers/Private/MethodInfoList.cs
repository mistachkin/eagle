/*
 * MethodInfoList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Reflection;
using Eagle._Attributes;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a list of reflected method descriptors.  It
    /// extends the standard generic list with a type name suitable for use
    /// within Eagle.
    /// </summary>
    [ObjectId("12ac5a24-b1ed-46ff-9c8b-7665d77c1935")]
    internal sealed class MethodInfoList : List<MethodInfo>
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public MethodInfoList()
            : base()
        {
            // do nothing.
        }
    }
}
