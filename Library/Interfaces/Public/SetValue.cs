/*
 * SetValue.cs --
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
    /// <summary>
    /// This interface is implemented by entities that allow their associated
    /// value to be set after they have been created.
    /// </summary>
    [ObjectId("f6912624-bc36-4fd0-8287-13d523a353b5")]
    public interface ISetValue
    {
        /// <summary>
        /// Sets the value associated with this object.  This value may be
        /// null.
        /// </summary>
        object Value { set; }
    }
}
