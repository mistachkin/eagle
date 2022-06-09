/*
 * HaveClientData.cs --
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
    [ObjectId("ac1c43de-7488-4af8-92ef-7c5c7b82625f")]
    public interface IHaveClientData : IGetClientData, ISetClientData
    {
        new IClientData ClientData { get; set; }
    }
}
