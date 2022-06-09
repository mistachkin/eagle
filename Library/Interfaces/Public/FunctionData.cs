/*
 * FunctionData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("e499605b-9aab-4da2-b122-2de5be55726f")]
    public interface IFunctionData : IIdentifier, IHavePlugin, IWrapperData
    {
        //
        // NOTE: The fully qualified type name for this function (not
        //       including the assembly name).
        //
        string TypeName { get; set; }

        //
        // NOTE: The number of arguments for this function, may be zero.
        //
        int Arguments { get; set; }

        //
        // NOTE: The list of allowed argument types for this function.
        //
        TypeList Types { get; set; }

        //
        // NOTE: The flags for this function.
        //
        FunctionFlags Flags { get; set; }
    }
}
