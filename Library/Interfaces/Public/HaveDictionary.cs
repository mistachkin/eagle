/*
 * HaveDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("bc1fd9b9-26bc-41fe-a663-076c4e38c051")]
    public interface IHaveDictionary<T>
    {
        T GetNamedValue(string name);
        void SetNamedValue(string name, T value);
    }
}
