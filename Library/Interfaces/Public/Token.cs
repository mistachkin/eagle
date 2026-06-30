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
    /// <summary>
    /// This interface represents a single token produced when a script is
    /// parsed, capturing its type, location within the source text, and the
    /// parse state it belongs to.  It also adds script-location
    /// (<see cref="IScriptLocation" />) and client-data
    /// (<see cref="IHaveClientData" />) support.
    /// </summary>
    [ObjectId("a0f5d9db-4d76-45c9-abd3-2ddf82f26b85")]
    public interface IToken : IScriptLocation, IHaveClientData
    {
        /// <summary>
        /// Gets or sets the parse state that this token belongs to.  This
        /// value may be null.
        /// </summary>
        IParseState ParseState { get; set; }
        /// <summary>
        /// Gets or sets the type of this token.
        /// </summary>
        TokenType Type { get; set; }
        /// <summary>
        /// Gets or sets the syntax type of this token, which classifies its
        /// role within the parsed script.
        /// </summary>
        TokenSyntaxType SyntaxType { get; set; }
        /// <summary>
        /// Gets or sets the flags associated with this token.
        /// </summary>
        TokenFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the zero-based character offset, within the source
        /// text, where this token begins.
        /// </summary>
        int Start { get; set; }
        /// <summary>
        /// Gets or sets the length, in characters, of this token within the
        /// source text.
        /// </summary>
        int Length { get; set; }
        /// <summary>
        /// Gets or sets the number of sub-tokens (components) that make up this
        /// token.
        /// </summary>
        int Components { get; set; }

        /// <summary>
        /// Gets the source text spanned by this token.  This value may be null.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Determines whether this token has been made immutable.
        /// </summary>
        /// <returns>
        /// True if this token is immutable; otherwise, false.
        /// </returns>
        bool IsImmutable();
        /// <summary>
        /// Marks this token as immutable, preventing further modification of
        /// its state.
        /// </summary>
        void MakeImmutable();

        /// <summary>
        /// Creates a snapshot of this token that can later be restored.
        /// </summary>
        /// <param name="token">
        /// Upon return, this will contain the saved copy of this token.
        /// </param>
        void Save(out IToken token);
        /// <summary>
        /// Creates a snapshot of this token, associating it with the specified
        /// parse state, that can later be restored.
        /// </summary>
        /// <param name="parseState">
        /// The parse state to associate with the saved token.  This parameter
        /// may be null.
        /// </param>
        /// <param name="token">
        /// Upon return, this will contain the saved copy of this token.
        /// </param>
        void Save(IParseState parseState, out IToken token);
        /// <summary>
        /// Restores the state of this token from a previously saved copy.
        /// </summary>
        /// <param name="token">
        /// The previously saved token whose state should be restored into this
        /// token.
        /// </param>
        /// <returns>
        /// True if the token state was restored; otherwise, false.
        /// </returns>
        bool Restore(ref IToken token);

        /// <summary>
        /// Builds a list of name/value pairs describing this token, resolving
        /// its text against the specified source text.
        /// </summary>
        /// <param name="text">
        /// The source text that this token refers to.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing this token.
        /// </returns>
        StringPairList ToList(string text);
        /// <summary>
        /// Builds a list of name/value pairs describing this token, resolving
        /// its text against the specified source text and optionally scrubbing
        /// potentially sensitive information.
        /// </summary>
        /// <param name="text">
        /// The source text that this token refers to.  This parameter may be
        /// null.
        /// </param>
        /// <param name="scrub">
        /// Non-zero to scrub potentially sensitive information from the
        /// resulting list.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing this token.
        /// </returns>
        StringPairList ToList(string text, bool scrub);

        /// <summary>
        /// Returns a string representation of this token, resolving its text
        /// against the specified source text.
        /// </summary>
        /// <param name="text">
        /// The source text that this token refers to.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// A string representation of this token.
        /// </returns>
        string ToString(string text);
    }
}
