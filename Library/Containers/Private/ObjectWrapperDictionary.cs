/*
 * ObjectWrapperDictionary.cs --
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

using ObjectWrapper = Eagle._Wrappers._Object;

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary of opaque object wrappers, keyed by
    /// name.  It extends the generic wrapper dictionary with a type name
    /// suitable for use within Eagle.
    /// </summary>
    [ObjectId("4f7cd0e5-f1f7-4c6a-a30e-1948339621dd")]
    internal sealed class ObjectWrapperDictionary :
            WrapperDictionary<string, ObjectWrapper>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ObjectWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new dictionary.
        /// </param>
        public ObjectWrapperDictionary(
            IDictionary<string, ObjectWrapper> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion
    }
}
