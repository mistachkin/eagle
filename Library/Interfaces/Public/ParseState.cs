/*
 * ParseState.cs --
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
    [ObjectId("9eb09ada-108e-4568-bcdf-009312791e57")]
    public interface IParseState
    {
        bool NotReady { get; set; }
        EngineFlags EngineFlags { get; set; }
        SubstitutionFlags SubstitutionFlags { get; set; }
        string FileName { get; set; }
        int CurrentLine { get; set; }
        int LineStart { get; set; }
        int CommentStart { get; set; }
        int CommentLength { get; set; }
        int CommandStart { get; set; }
        int CommandLength { get; set; }
        int CommandWords { get; set; }
        TokenFlags TokenFlags { get; set; }
        TokenList Tokens { get; set; }
        ParseError ParseError { get; set; }
        string Text { get; set; }
        int Characters { get; set; }
        int Terminator { get; set; }
        bool Incomplete { get; set; }

        bool IsImmutable();
        void MakeImmutable();

        void Save(bool full, out IParseState parseState);
        bool Restore(ref IParseState parseState);

        StringPairList ToList();
    }
}
