/*
 * EnsembleManager.cs --
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
    [ObjectId("23fa5163-9096-40cf-ba25-19494feaf08d")]
    public interface IEnsembleManager
    {
        ReturnCode AddOrUpdateSubCommand(
            Interpreter interpreter,
            string name,
            ISubCommand subCommand,
            IClientData clientData,
            SubCommandFlags flags,
            ref Result error
        );
    }
}
