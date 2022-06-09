/*
 * StreamHost.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.IO;
using System.Text;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("9180bb9e-b41a-4d17-be3a-4e8f75313020")]
    public interface IStreamHost : IInteractiveHost
    {
        Stream DefaultIn { get; }
        Stream DefaultOut { get; }
        Stream DefaultError { get; }

        Stream In { get; set; }
        Stream Out { get; set; }
        Stream Error { get; set; }

        Encoding InputEncoding { get; set; }
        Encoding OutputEncoding { get; set; }
        Encoding ErrorEncoding { get; set; }

        bool ResetIn();
        bool ResetOut();
        bool ResetError();

        bool IsOutputRedirected();
        bool IsErrorRedirected();

        bool SetupChannels();
    }
}
