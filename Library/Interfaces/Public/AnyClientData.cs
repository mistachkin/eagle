/*
 * AnyClientData.cs --
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
    [ObjectId("4669f1ff-0fb8-4628-bf4f-1ac1637ecc2f")]
    public interface IAnyClientData :
            IClientData, IAnyData, IAnyTypeData, IAnyValueTypeData,
            ISynchronize
    {
        bool AttachTo(IAnyClientData anyClientData);
        bool DetachFrom(IAnyClientData anyClientData);

        int ReplaceData(IAnyClientData anyClientData);
    }
}
