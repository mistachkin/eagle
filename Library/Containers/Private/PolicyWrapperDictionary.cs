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
    [ObjectId("a8c9a48b-28ec-4f8e-b9ba-9c508bee48ae")]
    internal sealed class PolicyWrapperDictionary :
            WrapperDictionary<string, PolicyWrapper>
    {
        public PolicyWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PolicyWrapperDictionary(
            IDictionary<string, PolicyWrapper> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
    }
}
