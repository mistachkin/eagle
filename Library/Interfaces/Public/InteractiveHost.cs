/*
 * InteractiveHost.cs --
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
    //
    // NOTE: This interface represents the absolute mimimum requirements for a
    //       custom host to be usable by the interactive loop.  Nothing should
    //       be added to this interface.
    //
    [ObjectId("8eba5a3b-6a51-465b-b0a4-32cdfd568970")]
    public interface IInteractiveHost : IIdentifier
    {
        ReturnCode BeginProcessing(
            int levels, ref string text, ref Result error);

        ReturnCode EndProcessing(
            int levels, ref string text, ref Result error);

        ReturnCode DoneProcessing(int levels, ref Result error);

        string Title { get; set; }

        bool RefreshTitle();
        bool IsInputRedirected();

        ReturnCode Prompt(
            PromptType type, ref PromptFlags flags, ref Result error);

        bool IsOpen();
        bool Pause();
        bool Flush();

        HeaderFlags GetHeaderFlags();
        HostFlags GetHostFlags();

        int ReadLevels { get; }
        int WriteLevels { get; }

        bool ReadLine(ref string value);

        bool Write(char value);
        bool Write(string value);

        bool WriteLine();
        bool WriteLine(string value);

        bool WriteResultLine(ReturnCode code, Result result);
        bool WriteResultLine(ReturnCode code, Result result, int errorLine);
    }
}
