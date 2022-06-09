/*
 * CallFrame.cs --
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
    [ObjectId("5c1396df-5c74-4ffa-bafa-13f2b795fab1")]
    public interface ICallFrame : IIdentifier, IMaybeDisposed, IThreadLock
    {
        long FrameId { get; set; }
        long FrameLevel { get; set; }
        CallFrameFlags Flags { get; set; }
        ObjectDictionary Tags { get; set; }
        long Index { get; set; }
        long Level { get; set; }
        IExecute Execute { get; set; }
        ArgumentList Arguments { get; set; }
        bool OwnArguments { get; set; }
        ArgumentList ProcedureArguments { get; set; }
        VariableDictionary Variables { get; set; }
        ICallFrame Other { get; set; }
        ICallFrame Previous { get; set; }
        ICallFrame Next { get; set; }

        //
        // NOTE: *RESERVED* For future use by the core library only.
        //
        IClientData EngineData { get; set; }

        //
        // NOTE: *RESERVED* For future use by the core library only.
        //
        IClientData AuxiliaryData { get; set; }

        //
        // NOTE: *RESERVED* For use by custom resolvers only.
        //
        IClientData ResolveData { get; set; }

        //
        // NOTE: *RESERVED* For use by third-party applications and
        //       plugins.  The core library will never use this.
        //
        IClientData ExtraData { get; set; }

        //
        // NOTE: Non-zero if the call frame actually owns variables.
        //
        bool IsVariable { get; }

        StringPairList ToList(DetailFlags detailFlags);
        string ToString(DetailFlags detailFlags);

        bool HasFlags(CallFrameFlags hasFlags, bool all);
        CallFrameFlags SetFlags(CallFrameFlags flags, bool set);

        bool ClearMarks();
        bool HasMark(string name);
        bool HasMark(string name, ref ICallFrame frame);
        bool HasMark(string name, ref object value);
        bool SetMark(bool mark, string name, object value);
        bool SetMark(bool mark, CallFrameFlags flags, string name, object value);

        void Free(bool global);
    }
}
