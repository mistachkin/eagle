/*
 * TraceWrapperDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Containers.Private
{
    [ObjectId("54794da1-263d-471d-af62-acf1246d9b6c")]
    internal sealed class TraceWrapperDictionary :
            WrapperDictionary<string, _Wrappers.Trace>
    {
        public TraceWrapperDictionary()
            : base()
        {
            // do nothing.
        }
    }
}
