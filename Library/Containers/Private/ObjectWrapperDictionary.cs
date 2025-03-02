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
    [ObjectId("4f7cd0e5-f1f7-4c6a-a30e-1948339621dd")]
    internal sealed class ObjectWrapperDictionary :
            WrapperDictionary<string, ObjectWrapper>
    {
        #region Public Constructors
        public ObjectWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
