/*
 * ExecuteRequest.cs --
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
    [ObjectId("06df1e97-84cd-47d1-8944-063c8172d831")]
    public interface IExecuteRequest
    {
        ReturnCode Execute(
            Interpreter interpreter, IClientData clientData, object request,
            ref object response, ref Result error);
    }
}
