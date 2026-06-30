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
    /// <summary>
    /// This interface represents the mutable state used by the engine while
    /// parsing a script into commands and tokens.  It tracks the input text,
    /// the current parsing position, the boundaries of the current comment
    /// and command, the collected tokens, and any parse error.  It is the
    /// managed counterpart to the parse state structure used by the Tcl
    /// parser.
    /// </summary>
    [ObjectId("9eb09ada-108e-4568-bcdf-009312791e57")]
    public interface IParseState
    {
        /// <summary>
        /// Gets or sets a value indicating that this parse state is not yet
        /// ready for use.
        /// </summary>
        bool NotReady { get; set; }
        /// <summary>
        /// Gets or sets the engine flags in effect for this parse.
        /// </summary>
        EngineFlags EngineFlags { get; set; }
        /// <summary>
        /// Gets or sets the substitution flags in effect for this parse.
        /// </summary>
        SubstitutionFlags SubstitutionFlags { get; set; }
        /// <summary>
        /// Gets or sets the name of the file the script being parsed
        /// originated from, if any.
        /// </summary>
        string FileName { get; set; }
        /// <summary>
        /// Gets or sets the one-based line number currently being parsed.
        /// </summary>
        int CurrentLine { get; set; }
        /// <summary>
        /// Gets or sets the character index, within the text, where the
        /// current line starts.
        /// </summary>
        int LineStart { get; set; }
        /// <summary>
        /// Gets or sets the character index, within the text, where the
        /// current comment starts.
        /// </summary>
        int CommentStart { get; set; }
        /// <summary>
        /// Gets or sets the length, in characters, of the current comment.
        /// </summary>
        int CommentLength { get; set; }
        /// <summary>
        /// Gets or sets the character index, within the text, where the
        /// current command starts.
        /// </summary>
        int CommandStart { get; set; }
        /// <summary>
        /// Gets or sets the length, in characters, of the current command.
        /// </summary>
        int CommandLength { get; set; }
        /// <summary>
        /// Gets or sets the number of words parsed so far for the current
        /// command.
        /// </summary>
        int CommandWords { get; set; }
        /// <summary>
        /// Gets or sets the token flags in effect for this parse.
        /// </summary>
        TokenFlags TokenFlags { get; set; }
        /// <summary>
        /// Gets or sets the list of tokens collected during this parse.
        /// </summary>
        TokenList Tokens { get; set; }
        /// <summary>
        /// Gets or sets the parse error, if any, encountered during this
        /// parse.
        /// </summary>
        ParseError ParseError { get; set; }
        /// <summary>
        /// Gets or sets the full text being parsed.
        /// </summary>
        string Text { get; set; }
        /// <summary>
        /// Gets or sets the number of characters, within the text, that are
        /// available to be parsed.
        /// </summary>
        int Characters { get; set; }
        /// <summary>
        /// Gets or sets the character index, within the text, of the
        /// terminator for the current command.
        /// </summary>
        int Terminator { get; set; }
        /// <summary>
        /// Gets or sets a value indicating that the parsed command is
        /// incomplete (e.g. it has unbalanced braces or brackets).
        /// </summary>
        bool Incomplete { get; set; }

        /// <summary>
        /// Determines whether this parse state has been made immutable.
        /// </summary>
        /// <returns>
        /// True if this parse state is immutable; otherwise, false.
        /// </returns>
        bool IsImmutable();
        /// <summary>
        /// Marks this parse state as immutable, preventing further changes
        /// to its members.
        /// </summary>
        void MakeImmutable();

        /// <summary>
        /// Creates a copy of this parse state suitable for later
        /// restoration.
        /// </summary>
        /// <param name="full">
        /// Non-zero to save a full (deep) copy of this parse state;
        /// otherwise, a shallow copy is saved.
        /// </param>
        /// <param name="parseState">
        /// Upon return, receives the saved copy of this parse state.
        /// </param>
        void Save(bool full, out IParseState parseState);
        /// <summary>
        /// Restores this parse state from a previously saved copy.
        /// </summary>
        /// <param name="parseState">
        /// The previously saved parse state to restore from.  Upon return,
        /// this may be reset.
        /// </param>
        /// <returns>
        /// True if this parse state was restored; otherwise, false.
        /// </returns>
        bool Restore(ref IParseState parseState);

        /// <summary>
        /// Converts this parse state into a list of name/value pairs, one
        /// per member, primarily for diagnostic purposes.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs representing this parse state.
        /// </returns>
        StringPairList ToList();
    }
}
