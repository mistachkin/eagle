/*
 * CommandBaseData.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("842aa70d-18ff-496c-a862-9ecb67af7552")]
    public interface ICommandBaseData
    {
        //
        // NOTE: The fully qualified type name for this command
        //       -OR- sub-command (not including the assembly
        //       name).
        //
        string TypeName { get; set; }

        //
        // NOTE: The flags for this command -OR- sub-command.
        //
        CommandFlags CommandFlags { get; set; }
    }
}
