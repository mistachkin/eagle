/*
 * Token.cs --
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
    [ObjectId("a0f5d9db-4d76-45c9-abd3-2ddf82f26b85")]
    public interface IToken : IScriptLocation, IHaveClientData
    {
        IParseState ParseState { get; set; }
        TokenType Type { get; set; }
        TokenSyntaxType SyntaxType { get; set; }
        TokenFlags Flags { get; set; }
        int Start { get; set; }
        int Length { get; set; }
        int Components { get; set; }

        string Text { get; }

        bool IsImmutable();
        void MakeImmutable();

        void Save(out IToken token);
        void Save(IParseState parseState, out IToken token);
        bool Restore(ref IToken token);

        StringPairList ToList(string text);
        StringPairList ToList(string text, bool scrub);

        string ToString(string text);
    }
}
