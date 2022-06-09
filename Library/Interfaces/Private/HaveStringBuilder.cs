/*
 * HaveStringBuilder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("5f4db407-e0e8-42f6-8b7f-8905c2a57eec")]
    internal interface IHaveStringBuilder
    {
        long Id { get; }

        int ReadWriteCount { get; }

        ArgumentList Arguments { get; set; }

        StringBuilder Builder { get; set; }

        StringBuilder BuilderForReadWrite { get; }
        StringBuilder BuilderForReadOnly { get; }

        void DoneWithReadWrite();
        void DoneWithReadOnly();
    }
}
