/*
 * Host.cs --
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
    [ObjectId("0b056d8f-c52e-4cb9-a92d-ddc478a00be7")]
    public interface IHost :
            IDisplayHost, IInteractiveHost, IFileSystemHost,
            IThreadHost, IProcessHost, IStreamHost, IDebugHost,
            IReadHost, IWriteHost, IInformationHost
    {
        string Profile { get; set; }
        string DefaultTitle { get; set; }

        HostCreateFlags HostCreateFlags { get; set; }

        bool UseAttach { get; set; }
        bool NoTitle { get; set; }
        bool NoIcon { get; set; }
        bool NoProfile { get; set; }
        bool NoCancel { get; set; }
        bool Echo { get; set; }

        StringList QueryState(DetailFlags detailFlags); /* [host query] */

        bool Beep(int frequency, int duration);

        bool IsIdle();
        bool Clear();

        bool ResetHostFlags();

        ReturnCode ResetHistory(ref Result error);

        ReturnCode GetMode(ChannelType channelType, ref uint mode,
            ref Result error);
        ReturnCode SetMode(ChannelType channelType, uint mode,
            ref Result error);

        ReturnCode Open(ref Result error);
        ReturnCode Close(ref Result error);
        ReturnCode Discard(ref Result error);
        ReturnCode Reset(ref Result error);

        bool BeginSection(string name, IClientData clientData);
        bool EndSection(string name, IClientData clientData);
    }
}
