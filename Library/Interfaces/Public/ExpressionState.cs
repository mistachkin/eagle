/*
 * ExpressionState.cs --
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
    [ObjectId("972771dd-148c-4c27-a22c-7495c15967c3")]
    public interface IExpressionState
    {
        IParseState ParseState { get; set; }
        bool NotReady { get; set; }
        Lexeme Lexeme { get; set; }
        int Start { get; set; }
        int Length { get; set; }
        int Next { get; set; }
        int PreviousEnd { get; set; }
        int Original { get; set; }
        int Last { get; set; }

        bool IsImmutable();
        void MakeImmutable();

        void Save(out IExpressionState exprState);
        void Save(IParseState parseState, out IExpressionState exprState);
        bool Restore(ref IExpressionState exprState);

        StringPairList ToList(string text);
        string ToString(string text);
    }
}
