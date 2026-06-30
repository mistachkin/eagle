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
    /// <summary>
    /// This interface represents the mutable state used while parsing and
    /// evaluating an expression.  It tracks the underlying parse state, the
    /// current lexeme, and the various positional offsets within the source
    /// text, and supports making the state immutable as well as saving and
    /// restoring snapshots of it.
    /// </summary>
    [ObjectId("972771dd-148c-4c27-a22c-7495c15967c3")]
    public interface IExpressionState
    {
        /// <summary>
        /// Gets or sets the parse state underlying this expression state.
        /// </summary>
        IParseState ParseState { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this expression state is
        /// not yet ready for use.
        /// </summary>
        bool NotReady { get; set; }
        /// <summary>
        /// Gets or sets the lexeme most recently recognized in the expression.
        /// </summary>
        Lexeme Lexeme { get; set; }
        /// <summary>
        /// Gets or sets the starting character offset of the current token
        /// within the source text.
        /// </summary>
        int Start { get; set; }
        /// <summary>
        /// Gets or sets the length, in characters, of the current token.
        /// </summary>
        int Length { get; set; }
        /// <summary>
        /// Gets or sets the character offset of the next character to be
        /// parsed within the source text.
        /// </summary>
        int Next { get; set; }
        /// <summary>
        /// Gets or sets the character offset just past the end of the previous
        /// token within the source text.
        /// </summary>
        int PreviousEnd { get; set; }
        /// <summary>
        /// Gets or sets the original starting character offset of the
        /// expression within the source text.
        /// </summary>
        int Original { get; set; }
        /// <summary>
        /// Gets or sets the character offset of the last character of the
        /// expression within the source text.
        /// </summary>
        int Last { get; set; }

        /// <summary>
        /// This method indicates whether this expression state has been made
        /// immutable.
        /// </summary>
        /// <returns>
        /// True if this expression state is immutable; otherwise, false.
        /// </returns>
        bool IsImmutable();
        /// <summary>
        /// This method makes this expression state immutable, preventing
        /// further changes to it.
        /// </summary>
        void MakeImmutable();

        /// <summary>
        /// This method saves a snapshot of this expression state.
        /// </summary>
        /// <param name="exprState">
        /// Upon return, this contains the saved copy of this expression state.
        /// </param>
        void Save(out IExpressionState exprState);
        /// <summary>
        /// This method saves a snapshot of this expression state, using the
        /// specified parse state.
        /// </summary>
        /// <param name="parseState">
        /// The parse state to associate with the saved expression state.
        /// </param>
        /// <param name="exprState">
        /// Upon return, this contains the saved copy of this expression state.
        /// </param>
        void Save(IParseState parseState, out IExpressionState exprState);
        /// <summary>
        /// This method restores this expression state from a previously saved
        /// snapshot.
        /// </summary>
        /// <param name="exprState">
        /// The previously saved expression state to restore from.
        /// </param>
        /// <returns>
        /// True if the expression state was restored; otherwise, false.
        /// </returns>
        bool Restore(ref IExpressionState exprState);

        /// <summary>
        /// This method formats this expression state as a list of name/value
        /// pairs.
        /// </summary>
        /// <param name="text">
        /// The source text the offsets in this expression state refer to, if
        /// any.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing this expression state.
        /// </returns>
        StringPairList ToList(string text);
        /// <summary>
        /// This method formats this expression state as a string.
        /// </summary>
        /// <param name="text">
        /// The source text the offsets in this expression state refer to, if
        /// any.
        /// </param>
        /// <returns>
        /// A string describing this expression state.
        /// </returns>
        string ToString(string text);
    }
}
