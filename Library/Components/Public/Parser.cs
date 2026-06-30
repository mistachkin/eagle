/*
 * Parser.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NET_40
using System.Numerics;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    #region Parse Token Class
    /// <summary>
    /// This class represents a single token produced by the Eagle script
    /// parser, such as a word, a variable reference, a command substitution,
    /// or a backslash sequence.
    /// </summary>
    [ObjectId("36d66e11-af5d-45ab-8c0a-fc77b4e08153")]
    public class ParseToken :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IToken
    {
        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this token, reserved for application usage.
        /// </summary>
        private IClientData clientData; // RESERVED for application usage.
        /// <summary>
        /// Gets or sets the client data associated with this token.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { if (immutable) throw new InvalidOperationException(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region IScriptLocation Members
        /// <summary>
        /// The name of the file, if any, that this token was parsed from.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets or sets the name of the file, if any, that this token was parsed from.
        /// </summary>
        public virtual string FileName
        {
            get { return fileName; }
            set { if (immutable) throw new InvalidOperationException(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The line number where this token starts.
        /// </summary>
        private int startLine;
        /// <summary>
        /// Gets or sets the line number where this token starts.
        /// </summary>
        public virtual int StartLine
        {
            get { return startLine; }
            set { if (immutable) throw new InvalidOperationException(); startLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The line number where this token ends.
        /// </summary>
        private int endLine;
        /// <summary>
        /// Gets or sets the line number where this token ends.
        /// </summary>
        public virtual int EndLine
        {
            get { return endLine; }
            set { if (immutable) throw new InvalidOperationException(); endLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this token was parsed via the <c>source</c> command.
        /// </summary>
        private bool viaSource;
        /// <summary>
        /// Gets or sets a value indicating whether this token was parsed via the
        /// <c>source</c> command.
        /// </summary>
        public virtual bool ViaSource
        {
            get { return viaSource; }
            set { if (immutable) throw new InvalidOperationException(); viaSource = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// properties of this token, using its own text.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs representing this token.
        /// </returns>
        public virtual StringPairList ToList()
        {
            return ToList(GetText(), false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// properties of this token, using its own text.
        /// </summary>
        /// <param name="scrub">
        /// Non-zero to scrub the file name of any sensitive path information.
        /// </param>
        /// <returns>
        /// The list of name/value pairs representing this token.
        /// </returns>
        public virtual StringPairList ToList(bool scrub)
        {
            return ToList(GetText(), scrub);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region IToken Members
        /// <summary>
        /// The parser state that this token belongs to.
        /// </summary>
        private IParseState parseState;    // Parser state that this token belongs to.
        /// <summary>
        /// Gets or sets the parser state that this token belongs to.
        /// </summary>
        public virtual IParseState ParseState
        {
            get { return parseState; }
            set { if (immutable) throw new InvalidOperationException(); parseState = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type of this token, such as a word or a variable.
        /// </summary>
        private TokenType type; // Type of token, such as 'Command'.
        /// <summary>
        /// Gets or sets the type of this token, such as a word or a variable.
        /// </summary>
        public virtual TokenType Type
        {
            get { return type; }
            set { if (immutable) throw new InvalidOperationException(); type = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The syntax highlighting type associated with this token.
        /// </summary>
        private TokenSyntaxType syntaxType;
        /// <summary>
        /// Gets or sets the syntax highlighting type associated with this token.
        /// </summary>
        public virtual TokenSyntaxType SyntaxType
        {
            get { return syntaxType; }
            set { if (immutable) throw new InvalidOperationException(); syntaxType = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags associated with this token.
        /// </summary>
        private TokenFlags flags;
        /// <summary>
        /// Gets or sets the flags associated with this token.
        /// </summary>
        public virtual TokenFlags Flags
        {
            get { return flags; }
            set { if (immutable) throw new InvalidOperationException(); flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The starting character offset of this token within the script text.
        /// </summary>
        private int start;      // Starting offset.
        /// <summary>
        /// Gets or sets the starting character offset of this token within the
        /// script text.
        /// </summary>
        public virtual int Start
        {
            get { return start; }
            set { if (immutable) throw new InvalidOperationException(); start = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The length, in characters, of this token within the script text.
        /// </summary>
        private int length;     // Length in characters.
        /// <summary>
        /// Gets or sets the length, in characters, of this token within the
        /// script text.
        /// </summary>
        public virtual int Length
        {
            get { return length; }
            set { if (immutable) throw new InvalidOperationException(); length = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        // field tells how many of them there are (including
        // components of components, etc).  The component
        // tokens immediately follow this one.

        /// <summary>
        /// The number of component tokens that make up this token, if any.
        /// </summary>
        private int components; // if this token is composed of other tokens, this
        /// <summary>
        /// Gets or sets the number of component tokens that make up this token,
        /// if any.
        /// </summary>
        public virtual int Components
        {
            get { return components; }
            set { if (immutable) throw new InvalidOperationException(); components = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the sub-string of the associated script text that corresponds to
        /// this token.
        /// </summary>
        public virtual string Text
        {
            get
            {
                string text = GetText();

                //
                // NOTE: Grab the sub-string for this token based on the associated
                //       parse state.
                //
                return (text != null) ?
                    (length > 0) ?
                        text.Substring(start, length) :
                        text.Substring(start) :
                    null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this token has been made read-only (immutable).
        /// </summary>
        private bool immutable;
        /// <summary>
        /// This method determines whether this token has been made read-only
        /// (immutable).
        /// </summary>
        /// <returns>
        /// True if this token is immutable; otherwise, false.
        /// </returns>
        public virtual bool IsImmutable()
        {
            return immutable;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method makes this token read-only (immutable), preventing further
        /// changes to its properties.
        /// </summary>
        public virtual void MakeImmutable()
        {
            immutable = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this token, saving its current state.
        /// </summary>
        /// <param name="token">
        /// Upon success, receives the newly created copy of this token.
        /// </param>
        public virtual void Save(
            out IToken token
            )
        {
            Save(this.ParseState, out token);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this token, associated with the specified
        /// parser state, saving its current state.
        /// </summary>
        /// <param name="parseState">
        /// The parser state to associate with the newly created token.
        /// </param>
        /// <param name="token">
        /// Upon success, receives the newly created copy of this token.
        /// </param>
        public virtual void Save(
            IParseState parseState,
            out IToken token
            )
        {
            ParseToken parseToken = new ParseToken(parseState);

            parseToken.type = this.type;
            parseToken.syntaxType = this.syntaxType;
            parseToken.flags = this.flags;
            parseToken.start = this.start;
            parseToken.length = this.length;
            parseToken.components = this.components;
            parseToken.clientData = this.clientData;
            parseToken.immutable = this.immutable;

            token = parseToken;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the state of this token from a previously saved
        /// token.
        /// </summary>
        /// <param name="token">
        /// The previously saved token to restore from.  Upon success, this is
        /// reset to null.
        /// </param>
        /// <returns>
        /// True if the state was restored; otherwise, false.
        /// </returns>
        public virtual bool Restore(
            ref IToken token
            )
        {
            if (immutable)
                return false;

            if (token == null)
                return false;

            ParseToken parseToken = token as ParseToken;

            if (parseToken == null)
                return false;

            this.parseState = parseToken.parseState;
            this.type = parseToken.type;
            this.syntaxType = parseToken.syntaxType;
            this.flags = parseToken.flags;
            this.start = parseToken.start;
            this.length = parseToken.length;
            this.components = parseToken.components;
            this.clientData = parseToken.clientData;
            this.immutable = parseToken.immutable;

            token = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// properties of this token, using the specified script text.
        /// </summary>
        /// <param name="text">
        /// The script text used to extract the textual value of this token.
        /// </param>
        /// <returns>
        /// The list of name/value pairs representing this token.
        /// </returns>
        public virtual StringPairList ToList(
            string text
            )
        {
            return ToList(text, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// properties of this token, using the specified script text.
        /// </summary>
        /// <param name="text">
        /// The script text used to extract the textual value of this token.
        /// </param>
        /// <param name="scrub">
        /// Non-zero to scrub the file name of any sensitive path information.
        /// </param>
        /// <returns>
        /// The list of name/value pairs representing this token.
        /// </returns>
        public virtual StringPairList ToList(
            string text,
            bool scrub
            )
        {
            StringPairList list = new StringPairList();

            list.Add("IsImmutable", this.IsImmutable().ToString());
            list.Add("Type", this.Type.ToString());
            list.Add("SyntaxType", this.SyntaxType.ToString());
            list.Add("Flags", this.Flags.ToString());

            //
            // NOTE: Need to "cache" these so we call the virtual "FileName"
            //       property exactly once.
            //
            string fileName = this.FileName;

            if (scrub)
            {
                fileName = PathOps.ScrubPath(GlobalState.GetBasePath(),
                    fileName);
            }

            list.Add("FileName", (fileName != null) ?
                fileName : String.Empty);

            list.Add("StartLine", this.StartLine.ToString());
            list.Add("EndLine", this.EndLine.ToString());
            list.Add("ViaSource", this.ViaSource.ToString());

            //
            // NOTE: Need to "cache" these so we call the virtual "Start" and
            //       "Length" properties exactly once.
            //
            int start = this.Start;
            int length = this.Length;

            list.Add("Start", start.ToString());
            list.Add("Length", length.ToString());

            list.Add("Components", this.Components.ToString());

            list.Add("Text", (text != null) ?
                (length > 0) ?
                    text.Substring(start, length) :
                    text.Substring(start) :
                String.Empty);

            //
            // NOTE: Need to "cache" this so we call the virtual "ClientData"
            //       property exactly once.
            //
            IClientData clientData = this.ClientData;

            list.Add("ClientData", (clientData != null) ?
                clientData.ToString() : String.Empty);

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the properties of this token into a string, using
        /// the specified script text.
        /// </summary>
        /// <param name="text">
        /// The script text used to extract the textual value of this token.
        /// </param>
        /// <returns>
        /// The string representation of this token.
        /// </returns>
        public virtual string ToString(
            string text
            )
        {
            return ToList(text).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method gets the script text associated with the parser state that
        /// this token belongs to.
        /// </summary>
        /// <returns>
        /// The associated script text, or null if it is not available.
        /// </returns>
        private string GetText()
        {
            //
            // NOTE: Need to "cache" this so we call the virtual "State" property
            //       exactly once.
            //
            IParseState parseState = this.ParseState;

            if (parseState != null)
                return parseState.Text;

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        /// <summary>
        /// This method creates a new token associated with the specified parser
        /// state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="parseState">
        /// The parser state to associate with the new token.
        /// </param>
        /// <returns>
        /// The newly created token.
        /// </returns>
        public static IToken FromState(
            Interpreter interpreter, /* NOT USED */
            IParseState parseState
            )
        {
            return new ParseToken(parseState);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        /// <summary>
        /// Constructs an instance of this class, copying the properties from an
        /// existing token.
        /// </summary>
        /// <param name="token">
        /// The existing token whose properties should be copied, if any.
        /// </param>
        protected ParseToken(
            IToken token
            )
            : this((token != null) ? token.ParseState : null)
        {
            if (token != null)
            {
                this.Type = token.Type;
                this.SyntaxType = token.SyntaxType;
                this.Flags = token.Flags;
                this.FileName = token.FileName;
                this.StartLine = token.StartLine;
                this.EndLine = token.EndLine;
                this.ViaSource = token.ViaSource;
                this.Start = token.Start;
                this.Length = token.Length;
                this.Components = token.Components;
                this.ClientData = token.ClientData;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, associated with the specified
        /// parser state.
        /// </summary>
        /// <param name="parseState">
        /// The parser state to associate with this token, if any.
        /// </param>
        protected ParseToken(
            IParseState parseState
            )
        {
            this.ParseState = parseState;

            if (parseState != null)
            {
                this.FileName = parseState.FileName;
                this.StartLine = parseState.CurrentLine;

                //
                // NOTE: Copy default token flags from parse state.
                //
                this.Flags = parseState.TokenFlags;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this token.
        /// </summary>
        /// <returns>
        /// The string representation of this token.
        /// </returns>
        public override string ToString()
        {
            return ToString(GetText());
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Parse State Class
    /// <summary>
    /// This class represents the state maintained by the Eagle script parser
    /// while it parses script text into tokens.
    /// </summary>
    [ObjectId("28ff2466-3e33-4c83-a8fb-f37fcf192ec6")]
    public class ParseState :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IParseState
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified engine and
        /// substitution flags.
        /// </summary>
        /// <param name="engineFlags">
        /// The engine flags to use while parsing.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use while parsing.
        /// </param>
        internal ParseState(
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags
            )
            : this(engineFlags, substitutionFlags, null, Parser.StartLine)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified engine and
        /// substitution flags, file name, and current line number.
        /// </summary>
        /// <param name="engineFlags">
        /// The engine flags to use while parsing.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use while parsing.
        /// </param>
        /// <param name="fileName">
        /// The name of the file being parsed, if any.
        /// </param>
        /// <param name="currentLine">
        /// The current line number within the script text.
        /// </param>
        internal ParseState(
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            string fileName,
            int currentLine
            )
            : this(engineFlags, substitutionFlags, fileName, currentLine, TokenFlags.None)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified engine and
        /// substitution flags, file name, current line number, and token flags.
        /// </summary>
        /// <param name="engineFlags">
        /// The engine flags to use while parsing.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use while parsing.
        /// </param>
        /// <param name="fileName">
        /// The name of the file being parsed, if any.
        /// </param>
        /// <param name="currentLine">
        /// The current line number within the script text.
        /// </param>
        /// <param name="tokenFlags">
        /// The default flags to apply to newly created tokens.
        /// </param>
        private ParseState(
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            string fileName,
            int currentLine,
            TokenFlags tokenFlags
            )
        {
            this.EngineFlags = engineFlags;
            this.SubstitutionFlags = substitutionFlags;
            this.FileName = fileName;
            this.CurrentLine = currentLine;
            this.TokenFlags = tokenFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, copying the state from an existing
        /// parser state.
        /// </summary>
        /// <param name="parseState">
        /// The existing parser state to copy, if any.
        /// </param>
        private ParseState(
            IParseState parseState
            )
        {
            if (parseState != null)
            {
                this.NotReady = parseState.NotReady;
                this.SubstitutionFlags = parseState.SubstitutionFlags;
                this.FileName = parseState.FileName;
                this.CurrentLine = parseState.CurrentLine;
                this.CommentStart = parseState.CommentStart;
                this.CommentLength = parseState.CommentLength;
                this.CommandStart = parseState.CommandStart;
                this.CommandLength = parseState.CommandLength;
                this.CommandWords = parseState.CommandWords;
                this.TokenFlags = parseState.TokenFlags;
                this.Tokens = parseState.Tokens; /* shallow copy */
                this.ParseError = parseState.ParseError;
                this.Text = parseState.Text;
                this.Characters = parseState.Characters;
                this.Terminator = parseState.Terminator;
                this.Incomplete = parseState.Incomplete;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new, empty parser state.
        /// </summary>
        /// <returns>
        /// The newly created parser state.
        /// </returns>
        public static IParseState Create()
        {
            return new ParseState(
                EngineFlags.None, SubstitutionFlags.Default);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region IParseState Members
        /// <summary>
        /// Non-zero if the parser was not ready to continue.
        /// </summary>
        private bool notReady;
        /// <summary>
        /// Gets or sets a value indicating whether the parser was not ready to
        /// continue.
        /// </summary>
        public virtual bool NotReady
        {
            get { return notReady; }
            set { notReady = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The engine flags in effect while parsing.
        /// </summary>
        private EngineFlags engineFlags;
        /// <summary>
        /// Gets or sets the engine flags in effect while parsing.
        /// </summary>
        public virtual EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { if (immutable) throw new InvalidOperationException(); engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The substitution flags in effect while parsing.
        /// </summary>
        private SubstitutionFlags substitutionFlags;
        /// <summary>
        /// Gets or sets the substitution flags in effect while parsing.
        /// </summary>
        public virtual SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { if (immutable) throw new InvalidOperationException(); substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the file being parsed, if any.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets or sets the name of the file being parsed, if any.
        /// </summary>
        public virtual string FileName
        {
            get { return fileName; }
            set { if (immutable) throw new InvalidOperationException(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The current line number within the script text.
        /// </summary>
        private int currentLine;
        /// <summary>
        /// Gets or sets the current line number within the script text.
        /// </summary>
        public virtual int CurrentLine
        {
            get { return currentLine; }
            set { if (immutable) throw new InvalidOperationException(); currentLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character index where the current line begins.
        /// </summary>
        private int lineStart;
        /// <summary>
        /// Gets or sets the character index where the current line begins.
        /// </summary>
        public virtual int LineStart
        {
            get { return lineStart; }
            set { if (immutable) throw new InvalidOperationException(); lineStart = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character index where the current comment begins.
        /// </summary>
        private int commentStart;
        /// <summary>
        /// Gets or sets the character index where the current comment begins.
        /// </summary>
        public virtual int CommentStart
        {
            get { return commentStart; }
            set { if (immutable) throw new InvalidOperationException(); commentStart = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The length, in characters, of the current comment.
        /// </summary>
        private int commentLength;
        /// <summary>
        /// Gets or sets the length, in characters, of the current comment.
        /// </summary>
        public virtual int CommentLength
        {
            get { return commentLength; }
            set { if (immutable) throw new InvalidOperationException(); commentLength = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character index where the current command begins.
        /// </summary>
        private int commandStart;
        /// <summary>
        /// Gets or sets the character index where the current command begins.
        /// </summary>
        public virtual int CommandStart
        {
            get { return commandStart; }
            set { if (immutable) throw new InvalidOperationException(); commandStart = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The length, in characters, of the current command.
        /// </summary>
        private int commandLength;
        /// <summary>
        /// Gets or sets the length, in characters, of the current command.
        /// </summary>
        public virtual int CommandLength
        {
            get { return commandLength; }
            set { if (immutable) throw new InvalidOperationException(); commandLength = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of words in the current command.
        /// </summary>
        private int commandWords;
        /// <summary>
        /// Gets or sets the number of words in the current command.
        /// </summary>
        public virtual int CommandWords
        {
            get { return commandWords; }
            set { if (immutable) throw new InvalidOperationException(); commandWords = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags applied to newly created tokens.
        /// </summary>
        private TokenFlags tokenFlags;
        /// <summary>
        /// Gets or sets the default flags applied to newly created tokens.
        /// </summary>
        public virtual TokenFlags TokenFlags
        {
            get { return tokenFlags; }
            set { if (immutable) throw new InvalidOperationException(); tokenFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of tokens produced by parsing.
        /// </summary>
        private TokenList tokens;
        /// <summary>
        /// Gets or sets the list of tokens produced by parsing.
        /// </summary>
        public virtual TokenList Tokens
        {
            get { return tokens; }
            set { if (immutable) throw new InvalidOperationException(); tokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The parse error, if any, that occurred.
        /// </summary>
        private ParseError error;
        /// <summary>
        /// Gets or sets the parse error, if any, that occurred.
        /// </summary>
        public virtual ParseError ParseError
        {
            get { return error; }
            set { if (immutable) throw new InvalidOperationException(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script text being parsed.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets or sets the script text being parsed.
        /// </summary>
        public virtual string Text
        {
            get { return text; }
            set { if (immutable) throw new InvalidOperationException(); text = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The total number of characters available for parsing.
        /// </summary>
        private int characters;
        /// <summary>
        /// Gets or sets the total number of characters available for parsing.
        /// </summary>
        public virtual int Characters
        {
            get { return characters; }
            set { if (immutable) throw new InvalidOperationException(); characters = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character index of the terminator of the most recently parsed
        /// construct.
        /// </summary>
        private int terminator;
        /// <summary>
        /// Gets or sets the character index of the terminator of the most recently
        /// parsed construct.
        /// </summary>
        public virtual int Terminator
        {
            get { return terminator; }
            set { if (immutable) throw new InvalidOperationException(); terminator = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the most recently parsed construct was incomplete.
        /// </summary>
        private bool incomplete;
        /// <summary>
        /// Gets or sets a value indicating whether the most recently parsed
        /// construct was incomplete.
        /// </summary>
        public virtual bool Incomplete
        {
            get { return incomplete; }
            set { if (immutable) throw new InvalidOperationException(); incomplete = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this parser state has been made read-only (immutable).
        /// </summary>
        private bool immutable;
        /// <summary>
        /// This method determines whether this parser state has been made
        /// read-only (immutable).
        /// </summary>
        /// <returns>
        /// True if this parser state is immutable; otherwise, false.
        /// </returns>
        public virtual bool IsImmutable()
        {
            return immutable;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method makes this parser state, and all of its tokens, read-only
        /// (immutable).
        /// </summary>
        public virtual void MakeImmutable()
        {
            TokenList tokens = this.Tokens;

            if (tokens != null)
            {
                foreach (IToken token in tokens)
                {
                    if (token == null)
                        continue;

                    token.MakeImmutable();
                }
            }

            immutable = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this parser state, saving its current
        /// state.
        /// </summary>
        /// <param name="full">
        /// Non-zero to also create copies of the contained tokens.
        /// </param>
        /// <param name="parseState">
        /// Upon success, receives the newly created copy of this parser state.
        /// </param>
        public virtual void Save(
            bool full,
            out IParseState parseState
            )
        {
            ParseState localParseState = new ParseState(null);

            localParseState.notReady = this.notReady;
            localParseState.engineFlags = this.engineFlags;
            localParseState.substitutionFlags = this.substitutionFlags;
            localParseState.fileName = this.fileName;
            localParseState.currentLine = this.currentLine;
            localParseState.lineStart = this.lineStart;
            localParseState.commentStart = this.commentStart;
            localParseState.commentLength = this.commentLength;
            localParseState.commandStart = this.commandStart;
            localParseState.commandLength = this.commandLength;
            localParseState.commandWords = this.commandWords;
            localParseState.tokenFlags = this.tokenFlags;

            TokenList newTokens;

            if (full)
                CopyTokens(localParseState, out newTokens);
            else
                newTokens = this.tokens;

            localParseState.tokens = newTokens;
            localParseState.error = this.error;
            localParseState.text = this.text;
            localParseState.characters = this.characters;
            localParseState.terminator = this.terminator;
            localParseState.incomplete = this.incomplete;
            localParseState.immutable = this.immutable;

            parseState = localParseState;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the state of this parser state from a previously
        /// saved parser state.
        /// </summary>
        /// <param name="parseState">
        /// The previously saved parser state to restore from.  Upon success,
        /// this is reset to null.
        /// </param>
        /// <returns>
        /// True if the state was restored; otherwise, false.
        /// </returns>
        public virtual bool Restore(
            ref IParseState parseState
            )
        {
            if (immutable)
                return false;

            if (parseState == null)
                return false;

            ParseState localParseState = parseState as ParseState;

            if (localParseState == null)
                return false;

            this.notReady = localParseState.notReady;
            this.engineFlags = localParseState.engineFlags;
            this.substitutionFlags = localParseState.substitutionFlags;
            this.fileName = localParseState.fileName;
            this.currentLine = localParseState.currentLine;
            this.lineStart = localParseState.lineStart;
            this.commentStart = localParseState.commentStart;
            this.commentLength = localParseState.commentLength;
            this.commandStart = localParseState.commandStart;
            this.commandLength = localParseState.commandLength;
            this.commandWords = localParseState.commandWords;
            this.tokenFlags = localParseState.tokenFlags;
            this.tokens = localParseState.tokens;
            this.error = localParseState.error;
            this.text = localParseState.text;
            this.characters = localParseState.characters;
            this.terminator = localParseState.terminator;
            this.incomplete = localParseState.incomplete;
            this.immutable = localParseState.immutable;

            parseState = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs representing the
        /// properties of this parser state and its tokens.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs representing this parser state.
        /// </returns>
        public virtual StringPairList ToList()
        {
            StringPairList list = new StringPairList();

            list.Add("NotReady", this.NotReady.ToString());
            list.Add("IsImmutable", this.IsImmutable().ToString());
            list.Add("EngineFlags", this.EngineFlags.ToString());
            list.Add("SubstitutionFlags", this.SubstitutionFlags.ToString());
            list.Add("FileName", this.FileName);
            list.Add("CurrentLine", this.CurrentLine.ToString());
            list.Add("CommentStart", this.CommentStart.ToString());
            list.Add("CommentLength", this.CommentLength.ToString());
            list.Add("CommandStart", this.CommandStart.ToString());
            list.Add("CommandLength", this.CommandLength.ToString());
            list.Add("CommandWords", this.CommandWords.ToString());

            //
            // NOTE: Need to "cache" this so we call the virtual "Tokens"
            //       property exactly once.
            //
            TokenList tokens = this.Tokens;

            if (tokens != null)
            {
                list.Add("Tokens", tokens.Count.ToString());

                //
                // NOTE: Need to "cache" this so we call the virtual "Text"
                //       property exactly once.
                //
                string text = this.Text;

                foreach (IToken token in tokens)
                {
                    if (token == null)
                        continue;

                    list.Add(token.ToList(text));
                }
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this parser state.
        /// </summary>
        /// <returns>
        /// The string representation of this parser state.
        /// </returns>
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method creates a copy of the list of tokens, associating the
        /// copies with the specified parser state.
        /// </summary>
        /// <param name="parseState">
        /// The parser state to associate with the copied tokens.
        /// </param>
        /// <param name="newTokens">
        /// Upon success, receives the newly created list of tokens.
        /// </param>
        private void CopyTokens(
            IParseState parseState,
            out TokenList newTokens
            )
        {
            if (tokens != null)
            {
                newTokens = new TokenList(tokens.Count);

                foreach (IToken token in tokens)
                {
                    IToken newToken;

                    if (token != null)
                        token.Save(parseState, out newToken);
                    else
                        newToken = null;

                    newTokens.Add(newToken);
                }
            }
            else
            {
                newTokens = null;
            }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Parser Class
    /// <summary>
    /// This class provides the core static methods used to parse Eagle script
    /// text into tokens, to parse integers in various radixes, to quote and
    /// split list elements, and to perform glob-style string matching.
    /// </summary>
    [ObjectId("bead8508-f064-457d-8e30-39744966fd0d")]
    public static class Parser
    {
        #region Private Constants
        /// <summary>
        /// The initial capacity used when creating a list of tokens.
        /// </summary>
        private static int TokenCapacity = 100;

        /// <summary>
        /// Represents the absence of a line number.
        /// </summary>
        public static readonly int NoLine = -2;
        /// <summary>
        /// Represents any line number.
        /// </summary>
        public static readonly int AnyLine = -1;
        /// <summary>
        /// The line number where parsing starts (the first line).
        /// </summary>
        public static readonly int StartLine = 1;
        /// <summary>
        /// Represents an unknown line number.
        /// </summary>
        public static readonly int UnknownLine = 0;

        /// <summary>
        /// The radix (base) used for binary integers.
        /// </summary>
        internal const int BinaryRadix = 2;
        /// <summary>
        /// The radix (base) used for octal integers.
        /// </summary>
        internal const int OctalRadix = 8;
        /// <summary>
        /// The radix (base) used for decimal integers.
        /// </summary>
        internal const int DecimalRadix = 10;
        /// <summary>
        /// The radix (base) used for hexadecimal integers.
        /// </summary>
        internal const int HexadecimalRadix = 16;

        /// <summary>
        /// The radix value that indicates the radix should be detected
        /// automatically.
        /// </summary>
        private const int AutomaticRadix = 0;
        /// <summary>
        /// The minimum radix (base) supported when parsing integers.
        /// </summary>
        private const int MinimumRadix = BinaryRadix;
        /// <summary>
        /// The maximum radix (base) supported when parsing integers.
        /// </summary>
        private const int MaximumRadix = 36;

        /// <summary>
        /// The maximum number of characters to scan when building an error
        /// message about list element syntax.
        /// </summary>
        private const int ErrorScanLimit = 20;

        /// <summary>
        /// The absolute value of the minimum 32-bit signed integer, expressed as
        /// an unsigned integer.
        /// </summary>
        private const uint IntMinValue = unchecked((uint)(-int.MinValue));
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Parser Helper Methods
        #region Readiness Checking Methods
        /// <summary>
        /// This method checks whether the interpreter and parser are ready to
        /// continue parsing.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check, if any.
        /// </param>
        /// <param name="parseState">
        /// The parser state to check, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the parser is ready to continue;
        /// otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode Ready(
            Interpreter interpreter,
            IParseState parseState,
            ref Result error
            )
        {
            ReadyFlags readyFlags = ReadyFlags.None;

            if (parseState != null)
            {
                EngineFlags engineFlags = parseState.EngineFlags;

                if (EngineFlagOps.HasNoCancel(engineFlags))
                    readyFlags |= ReadyFlags.NoCancel;

                if (EngineFlagOps.HasNoGlobalCancel(engineFlags))
                    readyFlags |= ReadyFlags.NoGlobalCancel;

#if DEBUGGER
                if (EngineFlagOps.HasNoBreakpoint(engineFlags))
                    readyFlags |= ReadyFlags.NoBreakpoint;
#endif
            }

            return Interpreter.ParserReady(
                interpreter, null, readyFlags, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Substitution Flags Methods
        /// <summary>
        /// This method determines whether backslash substitution is enabled by
        /// the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The substitution flags to check.
        /// </param>
        /// <returns>
        /// True if backslash substitution is enabled; otherwise, false.
        /// </returns>
        private static bool HasBackslashes(
            SubstitutionFlags flags
            )
        {
            return ((flags & SubstitutionFlags.Backslashes) == SubstitutionFlags.Backslashes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether variable substitution is enabled by
        /// the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The substitution flags to check.
        /// </param>
        /// <returns>
        /// True if variable substitution is enabled; otherwise, false.
        /// </returns>
        private static bool HasVariables(
            SubstitutionFlags flags
            )
        {
            return ((flags & SubstitutionFlags.Variables) == SubstitutionFlags.Variables);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether command substitution is enabled by
        /// the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The substitution flags to check.
        /// </param>
        /// <returns>
        /// True if command substitution is enabled; otherwise, false.
        /// </returns>
        private static bool HasCommands(
            SubstitutionFlags flags
            )
        {
            return ((flags & SubstitutionFlags.Commands) == SubstitutionFlags.Commands);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Character Checking Methods
        /// <summary>
        /// This method determines whether the specified character is a horizontal
        /// tab or a space.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a horizontal tab or a space; otherwise, false.
        /// </returns>
        private static bool IsTabOrSpace(
            char character
            )
        {
            return ((character == Characters.HorizontalTab) ||
                    (character == Characters.Space));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is valid within
        /// an identifier (a letter, a digit, or an underscore).
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is valid within an identifier; otherwise, false.
        /// </returns>
        internal static bool IsIdentifier(
            char character
            )
        {
            return Char.IsLetterOrDigit(character) ||
                   (character == Characters.Underscore);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is a line
        /// terminator.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a line terminator; otherwise, false.
        /// </returns>
        internal static bool IsLineTerminator(
            char character
            )
        {
            return (character == Characters.LineFeed);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is considered
        /// whitespace by the parser.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is whitespace; otherwise, false.
        /// </returns>
        internal static bool IsWhiteSpace(
            char character
            )
        {
            //
            // NOTE: We purposely do not use the .NET Framework provided
            //       System.Char.IsWhiteSpace function here because all the
            //       delimiters in this language are currently limited to
            //       7-bit ASCII characters.
            //
            // return Char.IsWhiteSpace(character) ||
            //     Characters.WhiteSpaceCharDictionary.ContainsKey(character);
            //
            // HACK: Apparently, we cannot simply use this check to determine
            //       if we consider the character to be whitespace.  It has
            //       been reported that the Dictionary.ContainsKey method
            //       allocates 28 bytes for something each time it is called.
            //       Since this method is in the critical code path for script
            //       evaluation, this has the potential to negatively impact
            //       performance.
            //
            // return Characters.WhiteSpaceCharDictionary.ContainsKey(character);
            //
            switch (character)
            {
                case Characters.HorizontalTab:
                case Characters.LineFeed:
                case Characters.VerticalTab:
                case Characters.FormFeed:
                case Characters.CarriageReturn:
                case Characters.Space:
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character can be the first
        /// non-whitespace character of a boolean valued string.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character can begin a boolean value; otherwise, false.
        /// </returns>
        internal static bool IsBoolean(
            char character
            )
        {
            //
            // NOTE: This method returns non-zero if the specified character
            //       can be the first non-whitespace character of a boolean
            //       valued string.  The caller is responsible for checking
            //       the remainder of the string, e.g. via trying to convert
            //       it into a boolean value, etc.
            //
            switch (character)
            {
                case Characters.D: // Disable / Disabled
                case Characters.E: // Enable / Enabled
                case Characters.F: // False
                case Characters.N: // No
                case Characters.O: // Off / On
                case Characters.T: // True
                case Characters.Y: // Yes
                    return true;

                case Characters.d: // disable / disabled
                case Characters.e: // enable / enabled
                case Characters.f: // false
                case Characters.n: // no
                case Characters.o: // off / on
                case Characters.t: // true
                case Characters.y: // yes
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is valid within
        /// an integer value.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <param name="sign">
        /// Non-zero to also treat a leading plus or minus sign as valid.
        /// </param>
        /// <returns>
        /// True if the character is valid within an integer; otherwise, false.
        /// </returns>
        internal static bool IsInteger(
            char character,
            bool sign
            )
        {
            //
            // HACK: Apparently, we cannot simply use this check to determine
            //       if we consider the character to be valid for an integer.
            //       It has been reported that the Dictionary.ContainsKey method
            //       allocates 28 bytes for something each time it is called.
            //
            // if (sign && Characters.SignCharDictionary.ContainsKey(character))
            //     return true;
            //
            // return Characters.IntegerCharDictionary.ContainsKey(character);
            //
            switch (character)
            {
                case Characters.PlusSign:
                case Characters.MinusSign:
                    return sign;

                case Characters.Zero:
                case Characters.One:
                case Characters.Two:
                case Characters.Three:
                case Characters.Four:
                case Characters.Five:
                case Characters.Six:
                case Characters.Seven:
                case Characters.Eight:
                case Characters.Nine:
                    return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Character Type Methods
        /// <summary>
        /// This method determines the character type classification of the
        /// specified character.
        /// </summary>
        /// <param name="character">
        /// The character to classify.
        /// </param>
        /// <param name="nextLine">
        /// Upon return, non-zero if the character causes the current source line
        /// to advance.
        /// </param>
        /// <returns>
        /// The character type classification of the character.
        /// </returns>
        private static CharacterType GetCharacterType(
            char character,
            ref bool nextLine
            )
        {
            CharacterType characterType;

            //
            // NOTE: Most characters do not cause the current source line
            //       to advance.
            //
            nextLine = false;

            switch (character)
            {
                //
                // NOTE: Check for command terminator first because the
                //       IsWhiteSpace function also allows for line feeds
                //       and we do not want to classify them as whitespace
                //       for the purposes of this function.
                //
                case Characters.LineFeed:
                    {
                        characterType = CharacterType.CommandTerminator;

                        //
                        // NOTE: Also advance the current source line.
                        //
                        nextLine = true;
                        break;
                    }
                case Characters.SemiColon:
                    {
                        characterType = CharacterType.CommandTerminator;
                        break;
                    }
                case Characters.Null:
                case Characters.OpenBracket:
                case Characters.DollarSign:
                case Characters.Backslash:
                    {
                        characterType = CharacterType.Substitution;
                        break;
                    }
                case Characters.QuotationMark:
                    {
                        characterType = CharacterType.Quote;
                        break;
                    }
                case Characters.CloseParenthesis:
                    {
                        characterType = CharacterType.CloseParenthesis;
                        break;
                    }
                case Characters.CloseBracket:
                    {
                        characterType = CharacterType.CloseBracket;
                        break;
                    }
                case Characters.OpenBrace:
                case Characters.CloseBrace:
                    {
                        characterType = CharacterType.Brace;
                        break;
                    }
                //
                // HACK: *PERF* Previously, there was a "default"
                //       case here with a call to the IsWhiteSpace
                //       method here; however, that was too slow.
                //
                case Characters.HorizontalTab:
                case Characters.VerticalTab:
                case Characters.FormFeed:
                case Characters.CarriageReturn:
                case Characters.Space:
                    {
                        characterType = CharacterType.Space;
                        break;
                    }
                default:
                    {
                        characterType = CharacterType.None;
                        break;
                    }
            }

            return characterType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character exactly matches
        /// the specified character type.
        /// </summary>
        /// <param name="character">
        /// The character to classify.
        /// </param>
        /// <param name="characterType1">
        /// The character type to compare against.
        /// </param>
        /// <param name="characterType2">
        /// Upon return, receives the character type classification of the
        /// character.
        /// </param>
        /// <param name="nextLine">
        /// Upon return, non-zero if the character causes the current source line
        /// to advance.
        /// </param>
        /// <returns>
        /// True if the character type matches exactly; otherwise, false.
        /// </returns>
        private static bool IsCharacterType(
            char character,
            CharacterType characterType1,
            ref CharacterType characterType2,
            ref bool nextLine
            )
        {
            characterType2 = GetCharacterType(character, ref nextLine);
            return (characterType1 == characterType2); // exact match.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the type of the specified character
        /// matches any of the types in the specified mask.
        /// </summary>
        /// <param name="character">
        /// The character to classify.
        /// </param>
        /// <param name="mask">
        /// The mask of character types to check against.
        /// </param>
        /// <param name="characterType">
        /// Upon return, receives the character type classification of the
        /// character.
        /// </param>
        /// <param name="nextLine">
        /// Upon return, non-zero if the character causes the current source line
        /// to advance.
        /// </param>
        /// <returns>
        /// True if the character type matches the mask; otherwise, false.
        /// </returns>
        private static bool HasCharacterTypes(
            char character,
            CharacterType mask,
            ref CharacterType characterType,
            ref bool nextLine
            )
        {
            characterType = GetCharacterType(character, ref nextLine);
            return ((characterType & mask) != CharacterType.None); // matches type mask.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region String Checking Methods
        /// <summary>
        /// This method counts the number of lines in the specified string.
        /// </summary>
        /// <param name="text">
        /// The string to examine.
        /// </param>
        /// <returns>
        /// The number of lines in the string.
        /// </returns>
        public static int CountLines(
            string text
            )
        {
            int result = 0;

            if (text != null)
            {
                //
                // NOTE: For a non-null string, there is always at least
                //       one line.
                //
                result++;

                int index = 0;

                do
                {
                    //
                    // NOTE: Is there another line terminator character
                    //       starting where we last left off?
                    //
                    index = text.IndexOf(Characters.LineFeed, index);

                    if (index == Index.Invalid)
                        break;

                    index++;  /* NOTE: Skip line terminator next time. */
                    result++; /* NOTE: Another line was found. */
                } while (true);
            }

            return result;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Syntax Highlighting Methods
        /// <summary>
        /// This method determines the syntax highlighting type for the specified
        /// token.
        /// </summary>
        /// <param name="tokenIndex">
        /// The index of the token within its command.
        /// </param>
        /// <param name="token">
        /// The token to classify, if any.
        /// </param>
        /// <returns>
        /// The syntax highlighting type for the token.
        /// </returns>
        private static TokenSyntaxType GetTokenSyntaxType(
            int tokenIndex,
            IToken token
            )
        {
            if (token != null)
            {
                switch (token.Type)
                {
                    case TokenType.Word:
                    case TokenType.SimpleWord:
                        {
                            TokenSyntaxType syntaxType = TokenSyntaxType.None;

                            if (tokenIndex == 0)
                                syntaxType |= TokenSyntaxType.CommandName;
                            else
                                syntaxType |= TokenSyntaxType.Argument;

                            string text = token.Text;

                            if (!String.IsNullOrEmpty(text))
                            {
                                switch (text[0])
                                {
                                    case Characters.QuotationMark:
                                        {
                                            syntaxType |= TokenSyntaxType.StringLiteral;
                                            break;
                                        }
                                    case Characters.OpenBrace:
                                        {
                                            syntaxType |= TokenSyntaxType.Block;
                                            break;
                                        }
                                    case Characters.DollarSign:
                                        {
                                            syntaxType |= TokenSyntaxType.Variable;
                                            break;
                                        }
                                    case Characters.OpenBracket:
                                        {
                                            syntaxType |= TokenSyntaxType.Command;
                                            break;
                                        }
                                }
                            }

                            return syntaxType;
                        }
                    case TokenType.Backslash:
                        {
                            return TokenSyntaxType.Backslash;
                        }
                    case TokenType.Command:
                        {
                            return TokenSyntaxType.Command;
                        }
                    case TokenType.Variable:
                    case TokenType.VariableNameOnly:
                        {
                            return TokenSyntaxType.Variable;
                        }
                }
            }

            return TokenSyntaxType.None;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses the specified script text into a series of command
        /// tokens.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for parsing, if any.
        /// </param>
        /// <param name="fileName">
        /// The name of the file the script text originated from, if any.
        /// </param>
        /// <param name="currentLine">
        /// The current line number within the script text.
        /// </param>
        /// <param name="text">
        /// The script text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the script text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to parse, or a negative value to parse to
        /// the end of the text.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use while parsing.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use while parsing.
        /// </param>
        /// <param name="nested">
        /// Non-zero if the script text is nested within a command substitution.
        /// </param>
        /// <param name="noReady">
        /// Non-zero to skip the parser readiness check.
        /// </param>
        /// <param name="syntax">
        /// Non-zero to compute syntax highlighting information for each token.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return any parse error; otherwise, success is always
        /// returned.
        /// </param>
        /// <param name="parseState">
        /// The parser state to use, which is created when null.
        /// </param>
        /// <param name="tokens">
        /// The list of tokens to populate, which is created when null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ParseScript(
            Interpreter interpreter,             /* in */
            string fileName,                     /* in */
            int currentLine,                     /* in */
            string text,                         /* in */
            int startIndex,                      /* in */
            int characters,                      /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            bool nested,                         /* in */
            bool noReady,                        /* in */
            bool syntax,                         /* in */
            bool strict,                         /* in */
            ref IParseState parseState,          /* in, out */
            ref TokenList tokens,                /* in, out */
            ref Result error                     /* out */
            )
        {
            ReturnCode code;
            int length = (text != null) ? text.Length : 0;
            int index = startIndex;
            int count = 0;

            if (parseState == null)
            {
                parseState = new ParseState(
                    engineFlags, substitutionFlags, fileName,
                    currentLine);
            }

            while ((code = ParseCommand(
                    interpreter, text, index,
                    (characters < 0) ? length - index : characters - index,
                    nested, parseState, noReady, ref error)) == ReturnCode.Ok)
            {
                count++; /* NOTE: Number of commands parsed. */

                TokenList commandTokens = parseState.Tokens;

                if (commandTokens != null)
                {
                    if (syntax)
                    {
                        for (int tokenIndex = 0; tokenIndex < commandTokens.Count; tokenIndex++)
                        {
                            IToken commandToken = commandTokens[tokenIndex];

                            if (commandToken != null)
                                commandToken.SyntaxType |=
                                    GetTokenSyntaxType(tokenIndex, commandToken);
                        }
                    }

                    if (tokens == null)
                        tokens = new TokenList(TokenCapacity);

                    IToken token = ParseToken.FromState(interpreter, parseState);

                    token.Type = TokenType.Separator;
                    token.Start = parseState.CommandStart;
                    token.Length = parseState.CommandLength;
                    token.Components = count;

                    tokens.Add(token);
                    tokens.AddRange(commandTokens);
                }

                index = parseState.CommandStart + parseState.CommandLength;

                if (index >= characters)
                    break;
            }

            return strict ? code : ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Core Parser Methods
        #region Integer Parser Methods
        #region Generic Integer Parser
        /// <summary>
        /// This method parses an integer value from the specified text using the
        /// specified radix.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to parse, or a negative value to parse to
        /// the end of the text.
        /// </param>
        /// <param name="radix">
        /// The radix (base) to use, or zero to detect it automatically.
        /// </param>
        /// <param name="whiteSpace">
        /// Non-zero to skip leading whitespace.
        /// </param>
        /// <param name="greedy">
        /// Non-zero to continue consuming valid digits even after an overflow
        /// occurs.
        /// </param>
        /// <param name="unsigned">
        /// Non-zero to parse the value as unsigned.
        /// </param>
        /// <param name="legacyOctal">
        /// Non-zero to treat a leading zero as an octal radix prefix.
        /// </param>
        /// <param name="endIndex">
        /// Upon return, receives the index just after the last character
        /// consumed.
        /// </param>
        /// <returns>
        /// The parsed integer value.
        /// </returns>
        internal static int ParseInteger(
            string text,
            int startIndex,
            int characters,
            byte radix,
            bool whiteSpace,
            bool greedy,
            bool unsigned,
            bool legacyOctal,
            ref int endIndex
            )
        {
            endIndex = startIndex;

            if (!String.IsNullOrEmpty(text))
            {
                int index = startIndex;
                char character = Characters.Null;

                //
                // NOTE: If no max length was supplied, potentially
                //       consume the whole string.
                //
                if (characters < 0)
                    characters = text.Length;

                //
                // NOTE: Skip over the leading white space?
                //
                if (whiteSpace)
                {
                    while (index < characters)
                    {
                        character = text[index];

                        if (!Char.IsWhiteSpace(character))
                            break;

                        index++;
                    }
                }
                else if (index < characters)
                {
                    character = text[index];
                }

                bool negative = false;

                //
                // NOTE: Check for leading plus or minus sign.
                //
                if (character == Characters.MinusSign)
                {
                    negative = true;
                    index++;
                }
                else if (character == Characters.PlusSign)
                {
                    index++;
                }

                //
                // NOTE: Check that the radix is valid.
                //
                if ((radix == AutomaticRadix) ||
                    ((radix >= MinimumRadix) && (radix <= MaximumRadix)))
                {
                    //
                    // NOTE: Have we processed the radix yet?
                    //
                    bool haveRadix = false;

                    //
                    // NOTE: Have we read a digit yet?
                    //
                    bool haveDigit = false;

                    //
                    // NOTE: Have we overflowed a uint?
                    //
                    bool overflow = false;

                    //
                    // NOTE: Maximum value we can multiply by our radix.
                    //
                    uint multiplyValue = 0;

                    //
                    // NOTE: The current result value.
                    //
                    uint value = 0;

                    //
                    // NOTE: The primary digit processing loop.
                    //
                    while (true)
                    {
                        //
                        // NOTE: Are we out of characters to read?  If so,
                        //       we are done.
                        //
                        if (index >= characters)
                            break;

                        //
                        // NOTE: Get the character at the current position.
                        //
                        character = text[index];

                        if (!haveRadix)
                        {
                            if (radix == AutomaticRadix)
                            {
                                //
                                // NOTE: All supported radix prefixes start with "0<letter>"
                                //       or "0" (octal); otherwise, it must be the decimal
                                //       radix (10).
                                //
                                if (character != Characters.Zero)
                                {
                                    radix = DecimalRadix;
                                }
                                else
                                {
                                    //
                                    // NOTE: Are we out of characters to read when we
                                    //       still have not determined the radix yet?
                                    //       If so, we cannot continue.
                                    //
                                    if ((index + 1) >= characters)
                                        break;

                                    //
                                    // NOTE: Preview the next character.
                                    //
                                    char nextCharacter = text[index + 1];

                                    //
                                    // NOTE: Check for the radix prefixes we support.
                                    //
                                    if ((nextCharacter == Characters.B) ||
                                        (nextCharacter == Characters.b))
                                    {
                                        radix = BinaryRadix;
                                        index++;
                                    }
                                    else if ((nextCharacter == Characters.O) ||
                                        (nextCharacter == Characters.o))
                                    {
                                        radix = OctalRadix;
                                        index++;
                                    }
                                    else if ((nextCharacter == Characters.D) ||
                                        (nextCharacter == Characters.d))
                                    {
                                        radix = DecimalRadix;
                                        index++;
                                    }
                                    else if ((nextCharacter == Characters.X) ||
                                        (nextCharacter == Characters.x))
                                    {
                                        radix = HexadecimalRadix;
                                        index++;
                                    }
                                    else
                                    {
                                        if (legacyOctal)
                                            radix = OctalRadix;
                                        else
                                            radix = DecimalRadix;
                                    }

                                    index++;
                                }
                            }
                            else if ((character == Characters.Zero) && ((index + 1) < characters))
                            {
                                //
                                // NOTE: Preview the next character.
                                //
                                char nextCharacter = text[index + 1];

                                //
                                // NOTE: Check for (and remove) the prefix for the
                                //       selected radix, if it is present.
                                //
                                if (radix == BinaryRadix)
                                {
                                    if ((nextCharacter == Characters.B) ||
                                        (nextCharacter == Characters.b))
                                    {
                                        index++;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: The zero was not followed by a
                                        //       binary radix prefix; therefore,
                                        //       the zero we have seen is an
                                        //       actual digit, not part of a
                                        //       radix prefix.
                                        //
                                        haveDigit = true;
                                    }
                                }
                                else if (radix == OctalRadix)
                                {
                                    if (((nextCharacter == Characters.O) ||
                                        (nextCharacter == Characters.o)))
                                    {
                                        index++;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: The zero was not followed by a
                                        //       octal radix prefix; therefore,
                                        //       the zero we have seen is an
                                        //       actual digit, not part of a
                                        //       radix prefix.
                                        //
                                        haveDigit = true;
                                    }
                                }
                                else if (radix == DecimalRadix)
                                {
                                    if (((nextCharacter == Characters.D) ||
                                        (nextCharacter == Characters.d)))
                                    {
                                        index++;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: The zero was not followed by a
                                        //       decimal radix prefix; therefore,
                                        //       the zero we have seen is an
                                        //       actual digit, not part of a
                                        //       radix prefix.
                                        //
                                        haveDigit = true;
                                    }
                                }
                                else if (radix == HexadecimalRadix)
                                {
                                    if (((nextCharacter == Characters.X) ||
                                        (nextCharacter == Characters.x)))
                                    {
                                        index++;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: The zero was not followed by a
                                        //       hexadecimal radix prefix;
                                        //       therefore, the zero we have seen
                                        //       is an actual digit, not part of a
                                        //       radix prefix.
                                        //
                                        haveDigit = true;
                                    }
                                }
                                else
                                {
                                    //
                                    // NOTE: The radix specified by the caller is
                                    //       not one of the "well-known" ones;
                                    //       therefore, the zero we have seen is
                                    //       an actual digit, not part of a radix
                                    //       prefix.
                                    //
                                    haveDigit = true;
                                }

                                index++;
                            }

                            //
                            // NOTE: Setup the maximum value we can safely multiply by
                            //       the radix (now that we actually know the radix).
                            //
                            multiplyValue = uint.MaxValue / radix;

                            //
                            // NOTE: We now have a radix.
                            //
                            haveRadix = true;

                            //
                            // NOTE: Now, skip to the top of the loop to process the
                            //       characters after the radix prefix.
                            //
                            continue;
                        }

                        //
                        // NOTE: Calculate the digit value for the current character.
                        //       If the current character is not a valid digit, we are
                        //       done.
                        //
                        byte digitValue = 0;

                        if (StringOps.CharIsAsciiDigit(character))
                        {
                            digitValue = (byte)(character - Characters.Zero);
                        }
                        else if (StringOps.CharIsAsciiAlpha(character))
                        {
                            if (character >= Characters.a)
                                digitValue = (byte)(DecimalRadix + character - Characters.a);
                            else
                                digitValue = (byte)(DecimalRadix + character - Characters.A);
                        }
                        else
                        {
                            break;
                        }

                        //
                        // NOTE: Make sure the digit value is vaild for this radix.
                        //       If not, we are done.
                        //
                        if (digitValue >= radix)
                            break;

                        //
                        // NOTE: We have now read and processed a digit.
                        //
                        if (!haveDigit)
                            haveDigit = true;

                        //
                        // NOTE: Check if we would overflow the value.
                        //
                        if ((value < multiplyValue) ||
                            ((value == multiplyValue) && (digitValue <= (uint.MaxValue % radix))))
                        {
                            //
                            // NOTE: Check for useless multiply.
                            //
                            //       (Zero * Anything) == Zero
                            //
                            if (value > 0)
                                //
                                // NOTE: Shift old digits to the left.
                                //
                                value *= radix;

                            //
                            // NOTE: Add new digit value.
                            //
                            value += digitValue;
                        }
                        else
                        {
                            //
                            // NOTE: We cannot process the new digit; it would
                            //       have resulted in an overflow.  Signal this
                            //       condition for later.
                            //
                            if (!overflow)
                                overflow = true;

                            //
                            // NOTE: Do they want us to keep consuming valid digits
                            //       even though we cannot actually process them?
                            //
                            if (!greedy)
                                break;
                        }

                        //
                        // NOTE: Advance to the next character now.
                        //
                        index++;
                    }

                    //
                    // NOTE: Did we manage to read and process a digit?
                    //
                    if (!haveDigit)
                    {
                        index = 0;
                    }
                    else if (overflow ||
                        (!unsigned && ((negative && (value > IntMinValue)) ||
                        (!negative && (value > int.MaxValue)))))
                    {
                        //
                        // NOTE: We encountered an overflow of some kind.
                        //
                        if (unsigned)
                        {
                            //
                            // NOTE: Return the maximum value for overflow in
                            //       "unsigned" mode.
                            //
                            value = uint.MaxValue;
                        }
                        else if (negative)
                        {
                            //
                            // NOTE: Return the minimum value (for a signed long)
                            //       in "signed" mode for an overflow in the
                            //       negative direction.
                            //
                            value = IntMinValue;
                        }
                        else
                        {
                            //
                            // NOTE: Return the maximum value (for a signed long)
                            //       in "signed" mode for an overflow in the
                            //       positive direction.
                            //
                            value = int.MaxValue;
                        }
                    }

                    //
                    // NOTE: Negate the value, if necessary.
                    //
                    if (negative)
                        value = ConversionOps.Negate(value);

                    //
                    // NOTE: If the ending index value would differ from the
                    //       default of zero (set above) then set it now.
                    //
                    if (index > 0)
                        endIndex = index;

                    return ConversionOps.ToInt(value);
                }
                else
                {
                    //
                    // NOTE: Indicate that we failed just after processing
                    //       the leading minus or plus sign, if any.
                    //
                    endIndex = index;
                }
            }

            return 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base2 (Binary) Integer Parser
        /// <summary>
        /// This method determines whether the specified character is a valid
        /// binary digit.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a valid binary digit; otherwise, false.
        /// </returns>
        private static bool IsBinaryDigit(
            char character
            )
        {
            return ((character >= Characters.Zero) && (character <= Characters.One));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a binary (base-2) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseBinary(
            string text,
            int startIndex,
            int characters,
            ref long number
            )
        {
            int index = startIndex;
            long result = 0;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsBinaryDigit(digit))
                        break;

                    index++;

                    result <<= 1;
                    result |= (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method parses a binary (base-2) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseBinary(
            string text,
            int startIndex,
            int characters,
            ref BigInteger number
            )
        {
            int index = startIndex;
            BigInteger result = BigInteger.Zero;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsBinaryDigit(digit))
                        break;

                    index++;

                    result <<= 1;
                    result |= (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base8 (Octal) Integer Parser
        /// <summary>
        /// This method determines whether the specified character is a valid
        /// octal digit.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a valid octal digit; otherwise, false.
        /// </returns>
        private static bool IsOctalDigit(
            char character
            )
        {
            return ((character >= Characters.Zero) && (character <= Characters.Seven));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a octal (base-8) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseOctal(
            string text,
            int startIndex,
            int characters,
            ref long number
            )
        {
            int index = startIndex;
            long result = 0;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsOctalDigit(digit))
                        break;

                    index++;

                    result <<= 3;
                    result |= (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method parses a octal (base-8) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseOctal(
            string text,
            int startIndex,
            int characters,
            ref BigInteger number
            )
        {
            int index = startIndex;
            BigInteger result = BigInteger.Zero;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsOctalDigit(digit))
                        break;

                    index++;

                    result <<= 3;
                    result |= (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base10 (Decimal) Integer Parser
        /// <summary>
        /// This method determines whether the specified character is a valid
        /// decimal digit.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a valid decimal digit; otherwise, false.
        /// </returns>
        private static bool IsDecimalDigit(
            char character
            )
        {
            return ((character >= Characters.Zero) && (character <= Characters.Nine));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a decimal (base-10) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseDecimal(
            string text,
            int startIndex,
            int characters,
            ref long number
            )
        {
            int index = startIndex;
            long result = 0;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsDecimalDigit(digit))
                        break;

                    index++;

                    result *= 10;
                    result += (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method parses a decimal (base-10) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseDecimal(
            string text,
            int startIndex,
            int characters,
            ref BigInteger number
            )
        {
            int index = startIndex;
            BigInteger result = BigInteger.Zero;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsDecimalDigit(digit))
                        break;

                    index++;

                    result *= 10;
                    result += (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base16 (Hexadecimal) Integer Parser
        /// <summary>
        /// This method determines whether the specified character is a valid
        /// hexadecimal digit.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a valid hexadecimal digit; otherwise, false.
        /// </returns>
        internal static bool IsHexadecimalDigit(
            char character
            )
        {
            return ((IsDecimalDigit(character)) ||
                    ((character >= Characters.A) && (character <= Characters.F)) ||
                    ((character >= Characters.a) && (character <= Characters.f)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a hexadecimal (base-16) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseHexadecimal(
            string text,
            int startIndex,
            int characters,
            ref long number
            )
        {
            int index = startIndex;
            long result = 0;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsHexadecimalDigit(digit))
                        break;

                    index++;
                    result <<= 4;

                    if (digit >= Characters.a)
                        result |= (byte)(DecimalRadix + digit - Characters.a);
                    else if (digit >= Characters.A)
                        result |= (byte)(DecimalRadix + digit - Characters.A);
                    else
                        result |= (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method parses a hexadecimal (base-16) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseHexadecimal(
            string text,
            int startIndex,
            int characters,
            ref BigInteger number
            )
        {
            int index = startIndex;
            BigInteger result = BigInteger.Zero;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsHexadecimalDigit(digit))
                        break;

                    index++;
                    result <<= 4;

                    if (digit >= Characters.a)
                        result |= (byte)(DecimalRadix + digit - Characters.a);
                    else if (digit >= Characters.A)
                        result |= (byte)(DecimalRadix + digit - Characters.A);
                    else
                        result |= (byte)(digit - Characters.Zero);
                }
            }

            number = result;
            return (index - startIndex);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base26 (Hexavigesimal) Integer Parser
        /// <summary>
        /// This method determines whether the specified character is a valid
        /// hexavigesimal digit.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a valid hexavigesimal digit; otherwise, false.
        /// </returns>
        private static bool IsHexavigesimalDigit(
            char character
            )
        {
            return (((character >= Characters.A) && (character <= Characters.Z)) ||
                    ((character >= Characters.a) && (character <= Characters.z)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a hexavigesimal (base-26) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseHexavigesimal(
            string text,
            int startIndex,
            int characters,
            ref long number
            )
        {
            int index = startIndex;
            long result = 0;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsHexavigesimalDigit(digit))
                        break;

                    index++;
                    result *= 26;

                    if (digit >= Characters.a)
                        result += (byte)(digit - Characters.a);
                    else
                        result += (byte)(digit - Characters.A);
                }
            }

            number = result;
            return (index - startIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method parses a hexavigesimal (base-26) integer value from the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The maximum number of characters to parse.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseHexavigesimal(
            string text,
            int startIndex,
            int characters,
            ref BigInteger number
            )
        {
            int index = startIndex;
            BigInteger result = BigInteger.Zero;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;

                while ((index < length) && (characters-- > 0))
                {
                    char digit = text[index];

                    if (!IsHexavigesimalDigit(digit))
                        break;

                    index++;
                    result *= 26;

                    if (digit >= Characters.a)
                        result += (byte)(digit - Characters.a);
                    else
                        result += (byte)(digit - Characters.A);
                }
            }

            number = result;
            return (index - startIndex);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Backslash Parser
        /// <summary>
        /// This method parses a backslash substitution sequence from the
        /// specified text, producing the resulting character(s).
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where the backslash sequence begins.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse.
        /// </param>
        /// <param name="character1">
        /// Upon success, receives the first resulting character, if any.
        /// </param>
        /// <param name="character2">
        /// Upon success, receives the second resulting character, if any.
        /// </param>
        internal static void ParseBackslash(
            string text,
            int startIndex,
            int characters,
            ref char? character1,
            ref char? character2
            )
        {
            int read = 0;
            Result error = null;

            ParseBackslash(
                null, text, startIndex, characters,
                ref read, ref character1, ref character2,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a backslash substitution sequence from the
        /// specified text, reporting how many characters it consumed.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where the backslash sequence begins.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse.
        /// </param>
        /// <param name="read">
        /// Upon return, receives the number of characters consumed.
        /// </param>
        private static void ParseBackslash(
            string text,
            int startIndex,
            int characters,
            ref int read
            )
        {
            char? character1 = null;
            char? character2 = null;
            Result error = null;

            ParseBackslash(
                null, text, startIndex, characters,
                ref read, ref character1, ref character2,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a backslash substitution sequence from the
        /// specified text, producing the resulting character(s) and reporting
        /// how many characters it consumed.
        /// </summary>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where the backslash sequence begins.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse.
        /// </param>
        /// <param name="read">
        /// Upon return, receives the number of characters consumed.
        /// </param>
        /// <param name="character1">
        /// Upon success, receives the first resulting character, if any.
        /// </param>
        /// <param name="character2">
        /// Upon success, receives the second resulting character, if any.
        /// </param>
        internal static void ParseBackslash( /* For use by ParserOps only. */
            string text,
            int startIndex,
            int characters,
            ref int read,
            ref char? character1,
            ref char? character2
            )
        {
            Result error = null;

            ParseBackslash(
                null, text, startIndex, characters,
                ref read, ref character1, ref character2,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a backslash substitution sequence from the
        /// specified text, producing the resulting character(s) and reporting
        /// how many characters it consumed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where the backslash sequence begins.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse.
        /// </param>
        /// <param name="read">
        /// Upon return, receives the number of characters consumed.
        /// </param>
        /// <param name="character1">
        /// Upon success, receives the first resulting character, if any.
        /// </param>
        /// <param name="character2">
        /// Upon success, receives the second resulting character, if any.
        /// </param>
        /// <param name="error">
        /// The error message.  This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode ParseBackslash(
            Interpreter interpreter, /* NOT USED */
            string text,          /* in */
            int startIndex,       /* in */
            int characters,       /* in */
            ref int read,         /* out */
            ref char? character1, /* out */
            ref char? character2, /* out */
            ref Result error /* NOT USED */
            )
        {
            read = 0;

            if (!String.IsNullOrEmpty(text) && (characters > 0))
            {
                int index = startIndex + 1;
                int length = text.Length;

                if ((index >= length) || // TEST: Test this.
                    (characters == 1))
                {
                    character1 = Characters.Backslash;
                    read = 1;
                }
                else
                {
                    read = 2;

                    switch (text[index])
                    {
                        case Characters.Null:
                            {
                                character1 = Characters.Backslash;
                                read = 1;
                                break;
                            }
                        case Characters.a:
                            {
                                character1 = Characters.Bell;
                                break;
                            }
                        case Characters.b:
                            {
                                character1 = Characters.Backspace;
                                break;
                            }
                        case Characters.f:
                            {
                                character1 = Characters.FormFeed;
                                break;
                            }
                        case Characters.n:
                            {
                                character1 = Characters.LineFeed;
                                break;
                            }
                        case Characters.r:
                            {
                                character1 = Characters.CarriageReturn;
                                break;
                            }
                        case Characters.t:
                            {
                                character1 = Characters.HorizontalTab;
                                break;
                            }
                        case Characters.v:
                            {
                                character1 = Characters.VerticalTab;
                                break;
                            }
                        case Characters.Backslash:
                            {
                                character1 = Characters.Backslash;
                                break;
                            }
                        case Characters.B: /* custom */
                            {
                                long number = 0;

                                //
                                // NOTE: Uses up all remaining binary characters.
                                //
                                read += ParseBinary(text, index + 1, characters - 2, ref number);

                                if (read == 2)
                                    character1 = Characters.B;
                                else
                                    character1 = (char)ConversionOps.ToByte(number); // NOTE: Must be byte (spec).

                                break;
                            }
                        case Characters.o: /* custom */
                            {
                                long number = 0;

                                //
                                // NOTE: Uses up all remaining octal characters.
                                //
                                read += ParseOctal(text, index + 1, characters - 2, ref number);

                                if (read == 2)
                                    character1 = Characters.o;
                                else
                                    character1 = (char)ConversionOps.ToByte(number); // NOTE: Must be byte (spec).

                                break;
                            }
                        case Characters.d: /* custom */
                            {
                                long number = 0;

                                //
                                // NOTE: Uses up all remaining decimal characters.
                                //
                                read += ParseDecimal(text, index + 1, characters - 2, ref number);

                                if (read == 2)
                                    character1 = Characters.d;
                                else
                                    character1 = (char)ConversionOps.ToByte(number); // NOTE: Must be byte (spec).

                                break;
                            }
                        case Characters.x:
                            {
                                long number = 0;

                                //
                                // NOTE: Uses up all remaining hexadecimal characters.
                                //
                                read += ParseHexadecimal(text, index + 1, characters - 2, ref number);

                                if (read == 2)
                                    character1 = Characters.x; // no hex digits, just "\x"
                                else
                                    character1 = (char)ConversionOps.ToByte(number); // NOTE: Must be byte (spec).

                                break;
                            }
                        case Characters.X: /* custom */
                            {
                                long number = 0;

                                //
                                // NOTE: Uses up all remaining hexadecimal characters.
                                //
                                read += ParseHexadecimal(text, index + 1, characters - 2, ref number);

                                if (read == 2)
                                {
                                    character1 = Characters.X; // no hex digits, just "\X"
                                }
                                else
                                {
                                    ConversionOps.ToChars(number, ref character1, ref character2);

                                    //
                                    // HACK: Reset second character to null if no bits were set.
                                    //
                                    if (character2 == Characters.Null) /* COMPAT: Eagle beta. */
                                        character2 = null;
                                }

                                break;
                            }
                        case Characters.u:
                            {
                                long number = 0;

                                //
                                // NOTE: Uses up to Characters.HexChars or however many hex
                                //       characters remain, whichever is less.
                                //
                                int charCharacters = (characters > (Characters.HexChars + 1))
                                    ? Characters.HexChars : characters - 2;

                                read += ParseHexadecimal(text, index + 1, charCharacters, ref number);

                                if (read == 2)
                                    character1 = Characters.u;
                                else
                                    character1 = ConversionOps.ToChar(number);

                                break;
                            }
                        case Characters.U: /* COMPAT: Tcl 8.6+ */
                            {
                                long number = 0;

                                //
                                // NOTE: Uses up to Characters.TwoHexChars or however many hex
                                //       characters remain, whichever is less.
                                //
                                int charCharacters = (characters > (Characters.TwoHexChars + 1))
                                    ? Characters.TwoHexChars : characters - 2;

                                read += ParseHexadecimal(text, index + 1, charCharacters, ref number);

                                if (read == 2)
                                {
                                    character1 = Characters.U;
                                }
                                else
                                {
                                    ConversionOps.ToChars(number, ref character1, ref character2);

                                    //
                                    // HACK: Reset second character to null if no bits were set.
                                    //
                                    if (character2 == Characters.Null) /* COMPAT: Eagle beta. */
                                        character2 = null;
                                }

                                break;
                            }
                        case Characters.LineFeed:
                            {
                                read--;

                                do
                                {
                                    index++; read++;
                                } while ((index < length) && // TEST: Test this.
                                         (read < characters) &&
                                         IsTabOrSpace(text[index]));

                                character1 = Characters.Space;
                                break;
                            }
                        default:
                            {
                                if (IsDecimalDigit(text[index]) && (text[index] < Characters.Eight))
                                {
                                    character1 = ConversionOps.ToChar(text[index] - Characters.Zero);
                                    index++;

                                    if ((characters == 2) || !IsDecimalDigit(text[index]) || (text[index] >= Characters.Eight))
                                        break;

                                    read = 3; // "\xy"
                                    character1 = ConversionOps.ToChar(((char)character1 << 3) + (text[index] - Characters.Zero));
                                    index++;

                                    if ((characters == 3) || !IsDecimalDigit(text[index]) || (text[index] >= Characters.Eight))
                                        break;

                                    read = 4; // "\xyz"
                                    character1 = ConversionOps.ToChar(((char)character1 << 3) + (text[index] - Characters.Zero));
                                    break;
                                }

                                //
                                // UNICODE: We are not supporting construction of multi-byte UTF-8 characters
                                //          here because the .NET Framework can only represent a subset of them
                                //          using a single character (which is always 2 bytes).
                                //
                                character1 = text[index];
                                break;
                            }
                    }
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region White-Space Parser
        /// <summary>
        /// This method parses (skips over) whitespace within the script text of
        /// the specified parser state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the script text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse.
        /// </param>
        /// <param name="parseState">
        /// The parser state whose script text is being parsed.
        /// </param>
        /// <param name="characterType">
        /// Upon return, receives the character type that terminated the
        /// whitespace.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        internal static int ParseWhiteSpace(
            Interpreter interpreter,
            int startIndex,
            int characters,
            IParseState parseState,
            ref CharacterType characterType,
            ref Result error
            )
        {
            int used = 0;

            if (parseState != null)
            {
                string text = parseState.Text;

                if (text != null) // INTL: do not change to String.IsNullOrEmpty
                {
                    int length = text.Length;

                    if (length > 0)
                    {
                        int index = startIndex;
                        CharacterType characterType2 = CharacterType.None;
                        bool nextLine = false;

                        while (true)
                        {
                            while ((index < length) && // TEST: Test this.
                                   (characters > 0) &&
                                   IsCharacterType(text[index], CharacterType.Space, ref characterType2, ref nextLine))
                            {
                                if (nextLine && (index > parseState.LineStart))
                                {
                                    parseState.CurrentLine++;
                                    parseState.LineStart = index;
                                    nextLine = false;
                                }

                                characters--; index++;
                            }

                            if (nextLine && (index > parseState.LineStart))
                            {
                                parseState.CurrentLine++;
                                parseState.LineStart = index;
                                nextLine = false;
                            }

                            if ((index < length) && // TEST: Test this.
                                (characters > 0) &&
                                (characterType2 == CharacterType.Substitution))
                            {
                                if (text[index] != Characters.Backslash)
                                    break;

                                if (--characters == 0)
                                    break;

                                //
                                // TEST: Test this.
                                //
                                // NOTE: Should be OK as end-of-string was not originally
                                //       considered a line terminator.
                                //
                                if ((index + 1) >= length)
                                    break;

                                if (!IsLineTerminator(text[index + 1]))
                                    break;

                                if ((index + 1) > parseState.LineStart)
                                {
                                    parseState.CurrentLine++;
                                    parseState.LineStart = index + 1;
                                }

                                index += 2;

                                if (--characters == 0)
                                {
                                    parseState.Incomplete = true;
                                    break;
                                }
                                continue;
                            }
                            break;
                        }
                        characterType = characterType2;
                        used = (index - startIndex);
                    } // no else
                }
                else
                {
                    error = "cannot parse a null string";
                }
            }
            else
            {
                error = "invalid parser state";
            }

            return used;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Comment Parser
        /// <summary>
        /// This method parses (skips over) any comments within the script text of
        /// the specified parser state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the script text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse.
        /// </param>
        /// <param name="parseState">
        /// The parser state whose script text is being parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The number of characters consumed.
        /// </returns>
        private static int ParseComment(
            Interpreter interpreter,
            int startIndex,
            int characters,
            IParseState parseState,
            ref Result error
            )
        {
            int index = startIndex;

            if (parseState != null)
            {
                string text = parseState.Text;

                if (!String.IsNullOrEmpty(text))
                {
                    int length = text.Length;
                    CharacterType characterType = CharacterType.None;
                    int scanned;

                    while ((index < length) && // TEST: Test this.
                           (characters > 0))
                    {
                        do
                        {
                            scanned = ParseWhiteSpace(
                                interpreter, index, characters, parseState,
                                ref characterType, ref error);

                            if (scanned > 0)
                            {
                                index += scanned; characters -= scanned;
                            }
                        } while ((index < length) && // TEST: Test this.
                                 (characters > 0) &&
                                 IsLineTerminator(text[index]) &&
                                 (((int)LogicOps.Y(index++, characters--)) > 0));

                        if ((index >= length) || // TEST: Test this.
                            (characters == 0) ||
                            (text[index] != Characters.NumberSign))
                            break;

                        if (parseState.CommentStart == Index.Invalid)
                            parseState.CommentStart = index;

                        while ((index < length) && // TEST: Test this.
                               (characters > 0))
                        {
                            if (text[index] == Characters.Backslash)
                            {
                                scanned = ParseWhiteSpace(
                                    interpreter, index, characters, parseState,
                                    ref characterType, ref error);

                                if (scanned > 0)
                                {
                                    index += scanned; characters -= scanned;
                                }
                                else
                                {
                                    scanned = 0;

                                    ParseBackslash(
                                        text, index, characters, ref scanned);

                                    //
                                    // NOTE: The code commented out here is not
                                    //       needed because the ParseWhiteSpace
                                    //       method (above) will always handle
                                    //       this case.  Keeping this code here
                                    //       should serve as a reminder that it
                                    //       is actually not required.
                                    //
                                    //if ((scanned > 1) &&
                                    //    IsLineTerminator(text[index + 1]) &&
                                    //    ((index + 1) > parseState.LineStart))
                                    //{
                                    //    parseState.CurrentLine++;
                                    //    parseState.LineStart = index + 1;
                                    //}

                                    index += scanned; characters -= scanned;
                                }
                            }
                            else
                            {
                                index++; characters--;

                                if (IsLineTerminator(text[index - 1]))
                                {
                                    if ((index - 1) > parseState.LineStart)
                                    {
                                        parseState.CurrentLine++;
                                        parseState.LineStart = index - 1;
                                    }
                                    break;
                                }
                            }
                        }
                        parseState.CommentLength = index - parseState.CommentStart;
                    }
                } // no else
            }

            return (index - startIndex);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Brace Parser
        /// <summary>
        /// This method parses a brace-quoted word from the specified text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where the opening brace is located.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse, or a negative value
        /// to parse to the end of the text.
        /// </param>
        /// <param name="parseState">
        /// The parser state to populate.
        /// </param>
        /// <param name="append">
        /// Non-zero to append to the existing tokens; otherwise, the parser
        /// state is reset first.
        /// </param>
        /// <param name="noReady">
        /// Non-zero to skip the parser readiness check.
        /// </param>
        /// <param name="terminator">
        /// Upon success, receives the index just after the closing brace.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        internal static ReturnCode ParseBraces(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters,
            IParseState parseState,
            bool append,
            bool noReady,
            ref int terminator,
            ref Result error
            )
        {
            if (!noReady && (interpreter != null) &&
                (Ready(interpreter, parseState, ref error) != ReturnCode.Ok))
            {
                if (parseState != null)
                    parseState.NotReady = true;

                return ReturnCode.Error;
            }

            if (interpreter != null)
                interpreter.EnterParserLevel();

            try
            {
                if (parseState != null)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        int length = text.Length;

                        if (characters < 0)
                            characters = length;

                        if (characters > 0)
                        {
                            IToken token = ParseToken.FromState(interpreter, parseState);

                            if (!append)
                            {
                                parseState.CommandWords = 0;

                                if (parseState.Tokens == null)
                                    parseState.Tokens = new TokenList(TokenCapacity);
                                else
                                    parseState.Tokens.Clear();

                                parseState.Text = text;
                                parseState.Characters = startIndex + characters;
                                parseState.ParseError = ParseError.Success;
                                parseState.Incomplete = false;
                            }

                            int index = startIndex;

                            token.Type = TokenType.Text;
                            token.Start = index + 1;
                            token.Components = 0;

                            int oldTokens = parseState.Tokens.Count;
                            int level = 1;

                            while (true)
                            {
                                // while ((int)Logic.Y(++index, --characters) > 0)
                                while (LogicOps.And((++index < length),
                                        (--characters > 0))) // TEST: Test this.
                                {
                                    bool nextLine = false;

                                    if (GetCharacterType(text[index], ref nextLine) != CharacterType.None)
                                    {
                                        if (nextLine && (index > parseState.LineStart))
                                        {
                                            parseState.CurrentLine++;
                                            parseState.LineStart = index;
                                        }
                                        break;
                                    }
                                }

                                if ((index >= length) || // TEST: Test this.
                                    (characters == 0))
                                {
                                    bool openBrace = false;

                                    parseState.ParseError = ParseError.MissingBrace;
                                    parseState.Terminator = startIndex;
                                    parseState.Incomplete = true;

                                    error = "missing close-brace";

                                    while (--index > startIndex)
                                    {
                                        switch (text[index])
                                        {
                                            case Characters.OpenBrace:
                                                {
                                                    openBrace = true;
                                                    break;
                                                }
                                            case Characters.LineFeed:
                                            case Characters.CarriageReturn:
                                                {
                                                    openBrace = false;
                                                    break;
                                                }
                                            case Characters.NumberSign:
                                                {
                                                    if (openBrace && IsWhiteSpace(text[index - 1]))
                                                    {
                                                        error += ": possible unbalanced brace in comment";
                                                        goto error;
                                                    }
                                                    break;
                                                }
                                        }
                                    }

                                error:
                                    return ReturnCode.Error;
                                }

                                switch (text[index])
                                {
                                    case Characters.OpenBrace:
                                        {
                                            level++;
                                            break;
                                        }
                                    case Characters.CloseBrace:
                                        {
                                            if (--level == 0)
                                            {
                                                if ((index != token.Start) ||
                                                    (parseState.Tokens.Count == oldTokens))
                                                {
                                                    token.Length = (index - token.Start);

                                                    parseState.Tokens.Add(token, parseState);
                                                }
                                                terminator = index + 1;
                                                return ReturnCode.Ok;
                                            }
                                            break;
                                        }
                                    case Characters.Backslash:
                                        {
                                            int read = 0;

                                            ParseBackslash(
                                                text, index, characters, ref read);

                                            if ((read > 1) &&
                                                IsLineTerminator(text[index + 1]))
                                            {
                                                if (characters == 2)
                                                {
                                                    parseState.Incomplete = true;
                                                }

                                                token.Length = (index - token.Start);

                                                if (token.Length > 0)
                                                    parseState.Tokens.Add(token, parseState);

                                                token = ParseToken.FromState(interpreter, parseState);

                                                token.Type = TokenType.Backslash;
                                                token.Start = index;
                                                token.Length = read;
                                                token.Components = 0;

                                                //
                                                // NOTE: Only the text after the backslash token itself
                                                //       should be considered to be on the next line.
                                                //
                                                if ((index + 1) > parseState.LineStart)
                                                {
                                                    parseState.CurrentLine++;
                                                    parseState.LineStart = index + 1;
                                                }

                                                parseState.Tokens.Add(token, parseState);

                                                index += (read - 1);
                                                characters -= (read - 1);

                                                token = ParseToken.FromState(interpreter, parseState);

                                                token.Type = TokenType.Text;
                                                token.Start = index + 1;
                                                token.Components = 0;
                                            }
                                            else
                                            {
                                                index += (read - 1);
                                                characters -= (read - 1);
                                            }
                                            break;
                                        }
                                }
                            }
                        }
                        else
                        {
                            error = "cannot parse zero characters";
                        }
                    }
                    else
                    {
                        error = "cannot parse a null or empty string";
                    }
                }
                else
                {
                    error = "invalid parser state";
                }

                return ReturnCode.Error;
            }
            finally
            {
                if (interpreter != null)
                    interpreter.ExitParserLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Variable Name Parser
        /// <summary>
        /// This method determines whether the specified text is a simple scalar
        /// variable name.
        /// </summary>
        /// <param name="text">
        /// The variable name to check.
        /// </param>
        /// <param name="notSimpleError">
        /// The error to use when the name is not simple (i.e., qualified).
        /// </param>
        /// <param name="notScalarError">
        /// The error to use when the name is not scalar (i.e., an array
        /// element).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives the appropriate error.
        /// </param>
        /// <returns>
        /// True if the variable name is a simple scalar; otherwise, false.
        /// </returns>
        public static bool IsSimpleScalarVariableName(
            string text,
            Result notSimpleError,
            Result notScalarError,
            ref Result error
            )
        {
            int length;

            if (StringOps.IsNullOrEmpty(text, out length))
            {
                error = "variable name is empty or null";
                return false;
            }

            if (text.IndexOf(
                    Characters.OpenParenthesis) != Index.Invalid)
            {
                if (length > 1 &&
                    (text[length - 1] == Characters.CloseParenthesis))
                {
                    error = notScalarError;
                    return false;
                }
            }

            int index = text.IndexOf(Characters.Colon);

            if (index == Index.Invalid)
                return true;

            index++;

            if ((index < length) &&
                (text[index] == Characters.Colon))
            {
                error = notSimpleError;
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits a variable name into its variable name and array
        /// index components.
        /// </summary>
        /// <param name="name">
        /// The variable name to split.
        /// </param>
        /// <param name="varName">
        /// Upon success, receives the variable name component.
        /// </param>
        /// <param name="varIndex">
        /// Upon success, receives the array index component, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SplitVariableName(
            string name,
            ref string varName,
            ref string varIndex,
            ref Result error
            )
        {
            if (name == null)
            {
                error = "invalid variable name";
                return ReturnCode.Error;
            }

            int length = name.Length;

            if (length == 0)
            {
                varName = String.Empty;
                varIndex = null;

                return ReturnCode.Ok;
            }

            int openParenthesis = name.IndexOf(
                Characters.OpenParenthesis);

            if (openParenthesis == Index.Invalid)
            {
                varName = name;
                varIndex = null;

                return ReturnCode.Ok;
            }

            if ((length <= 1) ||
                (name[length - 1] != Characters.CloseParenthesis))
            {
                varName = name;
                varIndex = null;

                return ReturnCode.Ok;
            }

            int closeParenthesis = length - 1;

            varName = name.Substring(0, openParenthesis);

            openParenthesis++;

            varIndex = name.Substring(openParenthesis,
                closeParenthesis - openParenthesis);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a variable reference from the specified text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where the variable reference begins.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse, or a negative value
        /// to parse to the end of the text.
        /// </param>
        /// <param name="parseState">
        /// The parser state to populate.
        /// </param>
        /// <param name="append">
        /// Non-zero to append to the existing tokens; otherwise, the parser
        /// state is reset first.
        /// </param>
        /// <param name="noReady">
        /// Non-zero to skip the parser readiness check.
        /// </param>
        /// <param name="nameOnly">
        /// Non-zero to parse the variable name only, without producing a
        /// variable token.
        /// </param>
        /// <param name="noDollarSign">
        /// Non-zero if the variable reference does not begin with a dollar
        /// sign.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        internal static ReturnCode ParseVariableName(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters,
            IParseState parseState,
            bool append,
            bool noReady,
            bool nameOnly,
            bool noDollarSign,
            ref Result error
            )
        {
            //
            // NOTE: Special case this because various parts of the Engine cannot
            //       even set the last errorInfo information without this.
            //
            if (!noReady && (interpreter != null) &&
                (Ready(interpreter, parseState, ref error) != ReturnCode.Ok))
            {
                if (parseState != null)
                    parseState.NotReady = true;

                return ReturnCode.Error;
            }

            if (interpreter != null)
                interpreter.EnterParserLevel();

            try
            {
                IToken token;
                int index;
                int variableIndex;
                bool array;

                if (parseState != null)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        int length = text.Length;

                        if (characters < 0)
                            characters = length;

                        if (characters > 0)
                        {
                            if (!append)
                            {
                                parseState.CommandWords = 0;

                                if (parseState.Tokens == null)
                                    parseState.Tokens = new TokenList(TokenCapacity);
                                else
                                    parseState.Tokens.Clear();

                                parseState.Text = text;
                                parseState.Characters = startIndex + characters;
                                parseState.ParseError = ParseError.Success;
                                parseState.Incomplete = false;
                            }

                            index = startIndex;

                            token = ParseToken.FromState(interpreter, parseState);

                            token.Type = nameOnly ?
                                TokenType.VariableNameOnly : TokenType.Variable;

                            token.Start = index;

                            variableIndex = parseState.Tokens.Count;

                            parseState.Tokens.Add(token, parseState);

                            token = ParseToken.FromState(interpreter, parseState);

                            if(!noDollarSign)
                            {
                                index++; characters--;

                                if ((index >= length) || // TEST: Test this.
                                    (characters == 0))
                                    goto justADollarSign;
                            }

                            //
                            // ODD: Analysis reveals that these three statements (which Tcl also
                            //      performs) are redundant because both branches of the if
                            //      statement below setup these three fields of the struct with
                            //      the values that are identical to these values or supersede
                            //      these values.
                            //
                            //token.Type = TokenType.Text;
                            //token.Start = index;
                            //token.Components = 0;

                            if (text[index] == Characters.OpenBrace)
                            {
                                index++; characters--;

                                token.Type = TokenType.Text;
                                token.Start = index;
                                token.Components = 0;

                                while ((index < length) && // TEST: Test this.
                                       (characters > 0) &&
                                       (text[index] != Characters.CloseBrace))
                                {
                                    //
                                    // NOTE: Handle the case where there are
                                    //       embedded line terminators in the
                                    //       variable name as this should change
                                    //       the current line number.
                                    //
                                    if (IsLineTerminator(text[index]) &&
                                        (index > parseState.LineStart))
                                    {
                                        parseState.CurrentLine++;
                                        parseState.LineStart = index;
                                    }

                                    characters--; index++;
                                }

                                if ((index >= length) || // TEST: Test this.
                                    (characters == 0))
                                {
                                    error = "missing close-brace for variable name";
                                    parseState.ParseError = ParseError.MissingVariableBrace;
                                    parseState.Terminator = token.Start - 1;
                                    parseState.Incomplete = true;
                                    goto error;
                                }

                                token.Length = (index - token.Start);

                                parseState.Tokens[parseState.Tokens.Last].Length =
                                    (index - parseState.Tokens[parseState.Tokens.Last].Start);

                                parseState.Tokens.Add(token, parseState);

                                index++;
                            }
                            else
                            {
                                token.Type = TokenType.Text;
                                token.Start = index;
                                token.Components = 0;

                                while ((index < length) && // TEST: Test this.
                                       (characters > 0))
                                {
                                    char character = text[index];

                                    if (IsIdentifier(character))
                                    {
                                        index++;
                                        characters--;
                                        continue;
                                    }

                                    if ((character == Characters.Colon) &&
                                        ((index + 1) < length) && // TEST: Test this.
                                        (characters > 1) &&
                                        (text[index + 1] == Characters.Colon))
                                    {
                                        index += 2;
                                        characters -= 2;

                                        while ((index < length) && // TEST: Test this.
                                               (characters > 0) &&
                                               (text[index] == Characters.Colon))
                                        {
                                            index++; characters--;
                                        }

                                        continue;
                                    }

                                    break;
                                }

                                // array support...

                                array = ((index < length) && // TEST: Test this.
                                         (characters > 0) &&
                                         (text[index] == Characters.OpenParenthesis));

                                token.Length = (index - token.Start);

                                if (!noDollarSign && (token.Length == 0) && !array)
                                    goto justADollarSign;

                                parseState.Tokens.Add(token, parseState);

                                if (array)
                                {
                                    if (ParseTokens(
                                            interpreter, index + 1, characters - 1,
                                            CharacterType.CloseParenthesis, parseState,
                                            noReady, ref error) != ReturnCode.Ok)
                                    {
                                        goto error;
                                    }

                                    if ((parseState.Terminator >= length) || // TEST: Test this.
                                        (parseState.Terminator == (index + characters)) ||
                                        (text[parseState.Terminator] != Characters.CloseParenthesis))
                                    {
                                        error = "missing )";
                                        parseState.ParseError = ParseError.MissingParenthesis;
                                        parseState.Terminator = index;
                                        parseState.Incomplete = true;
                                        goto error;
                                    }

                                    index = parseState.Terminator + 1;
                                }
                            }

                            //
                            // NOTE: Fixup token size and nested components.
                            //
                            token = parseState.Tokens[variableIndex];
                            token.Length = (index - token.Start);
                            token.Components = parseState.Tokens.Count - (variableIndex + 1);

                            //
                            // BUGFIX: Variable name can span multiple lines.
                            //
                            token.EndLine = parseState.CurrentLine;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "cannot parse zero characters";
                        }
                    }
                    else
                    {
                        error = "cannot parse a null or empty string";
                    }
                }
                else
                {
                    error = "invalid parser state";
                }

                return ReturnCode.Error;

            justADollarSign:
                token = parseState.Tokens[variableIndex];
                token.Type = TokenType.Text;
                token.Length = 1;
                token.Components = 0;
                return ReturnCode.Ok;

            error:
                return ReturnCode.Error;
            }
            finally
            {
                if (interpreter != null)
                    interpreter.ExitParserLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Token Parser
        /// <summary>
        /// This method parses a series of tokens from the script text of the
        /// specified parser state, stopping at any character matching the
        /// specified mask.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the script text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse.
        /// </param>
        /// <param name="mask">
        /// The mask of character types that should terminate parsing.
        /// </param>
        /// <param name="parseState">
        /// The parser state to populate.
        /// </param>
        /// <param name="noReady">
        /// Non-zero to skip the parser readiness check.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        internal static ReturnCode ParseTokens(
            Interpreter interpreter,
            int startIndex,
            int characters,
            CharacterType mask,
            IParseState parseState,
            bool noReady,
            ref Result error
            )
        {
            if (!noReady && (interpreter != null) &&
                (Ready(interpreter, parseState, ref error) != ReturnCode.Ok))
            {
                if (parseState != null)
                    parseState.NotReady = true;

                return ReturnCode.Error;
            }

            if (interpreter != null)
                interpreter.EnterParserLevel();

            try
            {
                if (parseState != null)
                {
                    string text = parseState.Text;

                    if (text != null) // INTL: do not change to String.IsNullOrEmpty
                    {
                        int length = text.Length;
                        CharacterType characterType = CharacterType.None;
                        bool nextLine = false;

                        IToken token;
                        int index = startIndex;
                        int originalTokens = parseState.Tokens.Count;

                        while ((index < length) && // TEST: Test this.
                                (characters > 0) &&
                                !HasCharacterTypes(text[index], mask,
                                    ref characterType, ref nextLine))
                        {
                            if (nextLine && (index > parseState.LineStart))
                            {
                                parseState.CurrentLine++;
                                parseState.LineStart = index;
                                nextLine = false;
                            }

                            token = ParseToken.FromState(interpreter, parseState);

                            token.Start = index;
                            token.Components = 0;

                            if ((characterType & CharacterType.Substitution) == CharacterType.None)
                            {
                                CharacterType characterType2 = CharacterType.None;
                                bool nextLine2 = false;

                                while (LogicOps.And((++index < length),
                                        (--characters > 0)) && // TEST: Test this.
                                    !HasCharacterTypes(text[index],
                                        mask | CharacterType.Substitution,
                                        ref characterType2, ref nextLine2))
                                {
                                    if (nextLine2 && (index > parseState.LineStart))
                                    {
                                        parseState.CurrentLine++;
                                        parseState.LineStart = index;
                                        nextLine2 = false;
                                    }
                                }

                                if (nextLine2 && (index > parseState.LineStart))
                                {
                                    parseState.CurrentLine++;
                                    parseState.LineStart = index;
                                    nextLine2 = false;
                                }

                                token.Type = TokenType.Text;
                                token.Length = (index - token.Start);

                                parseState.Tokens.Add(token, parseState);
                            }
                            else if (text[index] == Characters.DollarSign)
                            {
                                if (!HasVariables(parseState.SubstitutionFlags))
                                {
                                    token.Type = TokenType.Text;
                                    token.Length = 1;

                                    parseState.Tokens.Add(token, parseState);

                                    index++;
                                    characters--;
                                    continue;
                                }

                                int varToken = parseState.Tokens.Count;

                                if (ParseVariableName(
                                        interpreter, text, index, characters,
                                        parseState, true, noReady, false,
                                        false, ref error) != ReturnCode.Ok)
                                {
                                    return ReturnCode.Error;
                                }

                                index += parseState.Tokens[varToken].Length;
                                characters -= parseState.Tokens[varToken].Length;
                            }
                            else if (text[index] == Characters.OpenBracket)
                            {
                                if (!HasCommands(parseState.SubstitutionFlags))
                                {
                                    token.Type = TokenType.Text;
                                    token.Length = 1;

                                    parseState.Tokens.Add(token, parseState);

                                    index++;
                                    characters--;
                                    continue;
                                }

                                index++; characters--;

                                IParseState nestedParseState = new ParseState(
                                    parseState.EngineFlags, parseState.SubstitutionFlags,
                                    parseState.FileName, parseState.CurrentLine);

                                while (true)
                                {
                                    if (ParseCommand(interpreter, text, index,
                                            characters, true, nestedParseState, noReady,
                                            ref error) != ReturnCode.Ok)
                                    {
                                        parseState.ParseError = nestedParseState.ParseError;
                                        parseState.Terminator = nestedParseState.Terminator;
                                        parseState.Incomplete = nestedParseState.Incomplete;
                                        return ReturnCode.Error;
                                    }

                                    index = nestedParseState.CommandStart + nestedParseState.CommandLength;
                                    characters = parseState.Characters - index;

                                    if ((nestedParseState.Terminator < nestedParseState.Text.Length) && // TEST: Test this.
                                        (nestedParseState.Terminator < parseState.Characters) &&
                                        (nestedParseState.Text[nestedParseState.Terminator] == Characters.CloseBracket) &&
                                        !nestedParseState.Incomplete)
                                    {
                                        break;
                                    }

                                    if ((index >= length) || // TEST: Test this.
                                        (characters == 0))
                                    {
                                        error = "missing close-bracket";
                                        parseState.ParseError = ParseError.MissingBracket;
                                        parseState.Terminator = token.Start;
                                        parseState.Incomplete = true;
                                        return ReturnCode.Error;
                                    }
                                }

                                token.Type = TokenType.Command;
                                token.Length = (index - token.Start);

                                parseState.Tokens.Add(token, parseState);
                            }
                            else if (text[index] == Characters.Backslash)
                            {
                                if (!HasBackslashes(parseState.SubstitutionFlags))
                                {
                                    token.Type = TokenType.Text;
                                    token.Length = 1;

                                    parseState.Tokens.Add(token, parseState);

                                    index++;
                                    characters--;
                                    continue;
                                }

                                int read = 0; /* token.Length; */ /* TODO: Why was this here? */

                                ParseBackslash(text, index, characters, ref read);

                                token.Length = read;

                                if (token.Length == 1)
                                {
                                    token.Type = TokenType.Text;

                                    parseState.Tokens.Add(token, parseState);

                                    index++; characters--;
                                    continue;
                                }

                                //
                                // BUGBUG: At this point, it should be impossible for length to have
                                //         any value other than 2.
                                //
                                //         1. There are more than zero characters left (loop condition).
                                //
                                //         2. ParseBackslash always returns 0, 1, or 2.
                                //
                                //         3. ParseBackslash cannot return zero if the text is not null nor
                                //            empty and there are more than zero characters to read.
                                //
                                //         4. The length == 1 case is handled above.
                                //
                                if ((index + 1) >= length)
                                {
                                    //
                                    // TODO: We should never get here.  Validate this fact and remove
                                    //       this block of code.
                                    //
                                    throw new ScriptException(ReturnCode.Error,
                                        "out of characters after ParseBackslash", null);
                                }

                                if (IsLineTerminator(text[index + 1]))
                                {
                                    if ((index + 1) > parseState.LineStart)
                                    {
                                        parseState.CurrentLine++;
                                        parseState.LineStart = index + 1;
                                    }

                                    if (characters == 2)
                                    {
                                        parseState.Incomplete = true;
                                    }

                                    if ((mask & CharacterType.Space) == CharacterType.Space)
                                    {
                                        if (parseState.Tokens.Count == originalTokens)
                                            goto finishToken;

                                        break;
                                    }
                                }

                                token.Type = TokenType.Backslash;

                                parseState.Tokens.Add(token, parseState);

                                index += token.Length;
                                characters -= token.Length;
                            }
                            else if (text[index] == Characters.Null)
                            {
                                token.Type = TokenType.Text;
                                token.Length = 1;

                                parseState.Tokens.Add(token, parseState);

                                index++; characters--;
                            }
                            else
                            {
                                error = "ParseTokens encountered unknown character";
                                return ReturnCode.Error;
                            }
                        }

                        if (nextLine && (index > parseState.LineStart))
                        {
                            parseState.CurrentLine++;
                            parseState.LineStart = index;
                            nextLine = false;
                        }

                        if (parseState.Tokens.Count == originalTokens)
                        {
                            token = ParseToken.FromState(interpreter, parseState);

                            token.Start = index;
                            token.Components = 0;

                            goto finishToken;
                        }

                        goto afterFinishToken;

                    finishToken:
                        token.Type = TokenType.Text;
                        token.Length = 0;

                        parseState.Tokens.Add(token, parseState);

                    afterFinishToken:
                        parseState.Terminator = index;
                    }
                    else
                    {
                        error = "cannot parse a null string";
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid parser state";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
            finally
            {
                if (interpreter != null)
                    interpreter.ExitParserLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Quoted String Parser
        /// <summary>
        /// This method parses a double-quoted word from the specified text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where the opening quotation mark is
        /// located.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to parse, or a negative value
        /// to parse to the end of the text.
        /// </param>
        /// <param name="parseState">
        /// The parser state to populate.
        /// </param>
        /// <param name="append">
        /// Non-zero to append to the existing tokens; otherwise, the parser
        /// state is reset first.
        /// </param>
        /// <param name="noReady">
        /// Non-zero to skip the parser readiness check.
        /// </param>
        /// <param name="terminator">
        /// Upon success, receives the index just after the closing quotation
        /// mark.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        internal static ReturnCode ParseQuotedString(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters,
            IParseState parseState,
            bool append,
            bool noReady,
            ref int terminator,
            ref Result error
            )
        {
            if (!noReady && (interpreter != null) &&
                (Ready(interpreter, parseState, ref error) != ReturnCode.Ok))
            {
                if (parseState != null)
                    parseState.NotReady = true;

                return ReturnCode.Error;
            }

            if (interpreter != null)
                interpreter.EnterParserLevel();

            try
            {
                if (parseState != null)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        int length = text.Length;

                        if (characters < 0)
                            characters = length;

                        if (characters > 0)
                        {
                            if (!append)
                            {
                                parseState.CommandWords = 0;

                                if (parseState.Tokens == null)
                                    parseState.Tokens = new TokenList(TokenCapacity);
                                else
                                    parseState.Tokens.Clear();

                                parseState.Text = text;
                                parseState.Characters = startIndex + characters;
                                parseState.ParseError = ParseError.Success;
                            }

                            if (ParseTokens(
                                    interpreter, startIndex + 1, characters - 1,
                                    CharacterType.Quote, parseState,
                                    noReady, ref error) != ReturnCode.Ok)
                            {
                                goto error;
                            }

                            if ((parseState.Terminator >= length) || // TEST: Test this.
                                (text[parseState.Terminator] != Characters.QuotationMark))
                            {
                                error = "missing \"";
                                parseState.ParseError = ParseError.MissingQuote;
                                parseState.Terminator = startIndex;
                                parseState.Incomplete = true;
                                goto error;
                            }

                            terminator = parseState.Terminator + 1;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "cannot parse zero characters";
                        }
                    }
                    else
                    {
                        error = "cannot parse a null or empty string";
                    }
                }
                else
                {
                    error = "invalid parser state";
                }

            error:
                return ReturnCode.Error;
            }
            finally
            {
                if (interpreter != null)
                    interpreter.ExitParserLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Command Parser
        /// <summary>
        /// This method determines whether the specified script text is a complete
        /// script (i.e., contains no incomplete commands).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="text">
        /// The script text to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the script is complete; otherwise, false.
        /// </returns>
        internal static bool IsComplete(
            Interpreter interpreter, /* in */
            string text,             /* in */
            ref Result error         /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            bool notReady = false; /* NOT USED */

            return IsComplete(
                interpreter, null, 0, text, Parser.StartLine,
                Length.Invalid, ref notReady, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified script text is a complete
        /// script (i.e., contains no incomplete commands).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="fileName">
        /// The name of the file the script text originated from, if any.
        /// </param>
        /// <param name="currentLine">
        /// The current line number within the script text.
        /// </param>
        /// <param name="text">
        /// The script text to check.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the script text, where checking should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to check, or a negative value to check to
        /// the end of the text.
        /// </param>
        /// <param name="notReady">
        /// Upon return, non-zero if the parser was not ready.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the script is complete; otherwise, false.
        /// </returns>
        public static bool IsComplete(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            int currentLine,         /* in */
            string text,             /* in */
            int startIndex,          /* in */
            int characters,          /* in */
            ref bool notReady,       /* in, out */
            ref Result error         /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            EngineFlags engineFlags;
            SubstitutionFlags substitutionFlags;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                engineFlags = interpreter.EngineFlagsNoLock;
                substitutionFlags = interpreter.SubstitutionFlagsNoLock;
            }

            return IsComplete(
                interpreter, fileName, currentLine, text, startIndex,
                characters, engineFlags, substitutionFlags, ref notReady,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified script text is a complete
        /// script (i.e., contains no incomplete commands).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="fileName">
        /// The name of the file the script text originated from, if any.
        /// </param>
        /// <param name="currentLine">
        /// The current line number within the script text.
        /// </param>
        /// <param name="text">
        /// The script text to check.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the script text, where checking should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to check, or a negative value to check to
        /// the end of the text.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use while parsing.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use while parsing.
        /// </param>
        /// <param name="notReady">
        /// Upon return, non-zero if the parser was not ready.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the script is complete; otherwise, false.
        /// </returns>
        public static bool IsComplete(
            Interpreter interpreter,             /* in */
            string fileName,                     /* in */
            int currentLine,                     /* in */
            string text,                         /* in */
            int startIndex,                      /* in */
            int characters,                      /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            ref bool notReady,                   /* in, out */
            ref Result error                     /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            IParseState parseState = new ParseState(
                engineFlags, substitutionFlags, fileName,
                currentLine);

            int index = startIndex;
            int length = (text != null) ? text.Length : 0;

            if (characters < 0)
                characters = length;

            while (ParseCommand(
                    interpreter, text, index,
                    characters - index, false, parseState,
                    notReady, ref error) == ReturnCode.Ok)
            {
                index = parseState.CommandStart + parseState.CommandLength;

                if ((index >= length) || (index >= characters))
                    break;
            }

            notReady = parseState.NotReady;
            return !parseState.Incomplete;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the specified parser state for parsing the
        /// specified script text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="fileName">
        /// The name of the file the script text originated from, if any.
        /// </param>
        /// <param name="currentLine">
        /// The current line number within the script text.
        /// </param>
        /// <param name="text">
        /// The script text to be parsed.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the script text, where parsing will begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to parse, or a negative value to parse to
        /// the end of the text.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use while parsing.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use while parsing.
        /// </param>
        /// <param name="parseState">
        /// The parser state to initialize, which is created when null.
        /// </param>
        /// <returns>
        /// True upon successful initialization.
        /// </returns>
        internal static bool Initialize(
            Interpreter interpreter,
            string fileName,
            int currentLine,
            string text,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            ref IParseState parseState
            )
        {
            if (parseState == null)
            {
                parseState = new ParseState(
                    engineFlags, substitutionFlags, fileName,
                    currentLine);
            }

            parseState.LineStart = Index.Invalid;
            parseState.CommentStart = Index.Invalid;
            parseState.CommentLength = 0;

            parseState.CommandStart = Index.Invalid;
            parseState.CommandLength = 0;
            parseState.CommandWords = 0;

            if (parseState.Tokens == null)
                parseState.Tokens = new TokenList(TokenCapacity);
            else
                parseState.Tokens.Clear();

            parseState.ParseError = ParseError.Success;

            parseState.Text = text;

            int length = (text != null) ? text.Length : 0;

            if (characters < 0)
                characters = length;

            parseState.Characters = startIndex + characters;
            parseState.Terminator = Index.Invalid;
            parseState.Incomplete = false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a single command from the specified text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to parse, or a negative value to parse to
        /// the end of the text.
        /// </param>
        /// <param name="nested">
        /// Non-zero if the command is nested within a command substitution.
        /// </param>
        /// <param name="parseState">
        /// The parser state to populate.
        /// </param>
        /// <param name="noReady">
        /// Non-zero to skip the parser readiness check.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ParseCommand(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters,
            bool nested,
            IParseState parseState,
            bool noReady,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            if (!noReady && (interpreter != null) &&
                (Ready(interpreter, parseState, ref error) != ReturnCode.Ok))
            {
                //
                // NOTE: At this point, we cannot really determine if the command
                //       is complete or not because we cannot continue; therefore,
                //       let us err on the side of caution.
                //
                if (parseState != null)
                    parseState.NotReady = true;

                return ReturnCode.Error;
            }

            if (interpreter != null)
                interpreter.EnterParserLevel();

            try
            {
                if (parseState != null)
                {
                    if ((text != null) || (characters >= 0))
                    {
                        int length = (text != null) ? text.Length : 0;

                        if (characters < 0)
                            characters = length;

                        parseState.LineStart = Index.Invalid;
                        parseState.CommentStart = Index.Invalid;
                        parseState.CommentLength = 0;
                        parseState.CommandStart = Index.Invalid;
                        parseState.CommandLength = 0;
                        parseState.CommandWords = 0;

                        if (parseState.Tokens == null)
                            parseState.Tokens = new TokenList(TokenCapacity);
                        else
                            parseState.Tokens.Clear();

                        parseState.Text = text;
                        parseState.Characters = startIndex + characters;
                        parseState.Terminator = parseState.Characters;
                        parseState.Incomplete = false;
                        parseState.ParseError = ParseError.Success;

                        CharacterType terminators;

                        if (nested)
                            terminators = CharacterType.CommandTerminator | CharacterType.CloseBracket;
                        else
                            terminators = CharacterType.CommandTerminator;

                        int scanned = ParseComment(
                            interpreter, startIndex, characters, parseState, ref error);

                        int index = (startIndex + scanned); characters -= scanned;

                        if ((text == null) || // TEST: Test this.
                            (index >= length) || // TEST: Test this.
                            (characters == 0))
                        {
                            if (nested)
                                parseState.Incomplete = nested;
                        }

                        parseState.CommandStart = index;

                        while (true)
                        {
                            int wordIndex = parseState.Tokens.Count;

                            IToken token = ParseToken.FromState(interpreter, parseState);

                            token.Type = TokenType.Word;

                            CharacterType characterType = CharacterType.None;

                            scanned = ParseWhiteSpace(
                                interpreter, index, characters, parseState,
                                ref characterType, ref error);

                            if (scanned > 0)
                            {
                                index += scanned; characters -= scanned;
                            }

                            if ((text == null) || // TEST: Test this.
                                (index >= length) || // TEST: Test this.
                                (characters == 0))
                            {
                                parseState.Terminator = index;
                                break;
                            }

                            if ((characterType & terminators) != CharacterType.None)
                            {
                                parseState.Terminator = index;
                                index++;
                                break;
                            }

                            token.Start = index;

                            parseState.Tokens.Add(token, parseState);

                            parseState.CommandWords++;

                            if (text[index] == Characters.QuotationMark)
                            {
                                int terminator = Index.Invalid;

                                if (ParseQuotedString(
                                        interpreter, text, index, characters,
                                        parseState, true, noReady, ref terminator,
                                        ref error) != ReturnCode.Ok)
                                {
                                    goto error;
                                }

                                index = terminator; characters = parseState.Characters - index;
                            }
                            else if (text[index] == Characters.OpenBrace)
                            {
                                int terminator = Index.Invalid;

                                if (ParseBraces(
                                        interpreter, text, index, characters,
                                        parseState, true, noReady, ref terminator,
                                        ref error) != ReturnCode.Ok)
                                {
                                    goto error;
                                }

                                index = terminator; characters = parseState.Characters - index;
                            }
                            else
                            {
                                if (ParseTokens(
                                        interpreter, index, characters,
                                        CharacterType.Space | terminators, parseState,
                                        noReady, ref error) != ReturnCode.Ok)
                                {
                                    goto error;
                                }

                                index = parseState.Terminator; characters = parseState.Characters - index;
                            }

                            token.Length = index - token.Start;
                            token.Components = parseState.Tokens.Count - (wordIndex + 1);

                            if ((token.Components == 1) &&
                                (parseState.Tokens[wordIndex + 1].Type == TokenType.Text))
                            {
                                token.Type = TokenType.SimpleWord;
                            }

                            scanned = ParseWhiteSpace(
                                interpreter, index, characters, parseState,
                                ref characterType, ref error);

                            if (scanned > 0)
                            {
                                index += scanned; characters -= scanned;
                                continue;
                            }

                            if ((index >= length) ||
                                (characters == 0))
                            {
                                parseState.Terminator = index;
                                break;
                            }

                            if ((characterType & terminators) != CharacterType.None)
                            {
                                parseState.Terminator = index;
                                index++;
                                break;
                            }

                            if (text[index - 1] == Characters.QuotationMark)
                            {
                                error = "extra characters after close-quote";
                                parseState.ParseError = ParseError.ExtraAfterCloseQuote;
                            }
                            else
                            {
                                error = "extra characters after close-brace";
                                parseState.ParseError = ParseError.ExtraAfterCloseBrace;
                            }
                            parseState.Terminator = index;
                            goto error;
                        }

                        parseState.CommandLength = index - parseState.CommandStart;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "cannot parse a null string";
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid parser state";
                    return ReturnCode.Error;
                }

            error:
                if (parseState.CommandStart == Index.Invalid)
                    parseState.CommandStart = 0;

                parseState.CommandLength = parseState.Characters - parseState.CommandStart;

                return ReturnCode.Error;
            }
            finally
            {
                if (interpreter != null)
                    interpreter.ExitParserLevel();
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region List Parser Methods
        /// <summary>
        /// This method quotes the specified string so that it may be used as a
        /// single list element.
        /// </summary>
        /// <param name="text">
        /// The string to quote.
        /// </param>
        /// <returns>
        /// The quoted string.
        /// </returns>
        public static string Quote(
            string text
            )
        {
            return Quote(text, ListElementFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method quotes the specified string so that it may be used as a
        /// single list element.
        /// </summary>
        /// <param name="text">
        /// The string to quote.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the string is quoted.
        /// </param>
        /// <returns>
        /// The quoted string.
        /// </returns>
        public static string Quote(
            string text,
            ListElementFlags flags
            )
        {
            StringBuilder result;
            int length = (text != null) ? text.Length : 0;

            if (FlagOps.HasFlags(flags, ListElementFlags.UseBackslashes, true))
            {
                result = StringBuilderFactory.Create(4 * length + 2);

                BackslashElement(text, 0, length, flags, ref result);
            }
            else
            {
                result = StringBuilderFactory.Create(2 * length + 2);

                ScanElement(/* null, */ text, 0, length, ref flags);
                ConvertElement(/* null, */ text, 0, length, flags, ref result);
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string requires quoting
        /// in order to be used as a single list element.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <returns>
        /// True if the string requires quoting; otherwise, false.
        /// </returns>
        public static bool NeedsQuoting(
            string text
            )
        {
            return NeedsQuoting(text, ListElementFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string requires quoting
        /// in order to be used as a single list element.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the string is quoted.
        /// </param>
        /// <returns>
        /// True if the string requires quoting; otherwise, false.
        /// </returns>
        public static bool NeedsQuoting(
            string text,
            ListElementFlags flags
            )
        {
            int length = (text != null) ? text.Length : 0;

            ScanElement(/* null, */ text, 0, length, ref flags);

            return FlagOps.HasFlags(flags, ListElementFlags.UseBraces, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans a string to determine how it must be quoted for use
        /// as a single list element.
        /// </summary>
        /// <param name="text">
        /// The string to scan.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the string, where scanning should begin.
        /// </param>
        /// <param name="length">
        /// The number of characters to scan, or a negative value to scan to
        /// the end of the string.
        /// </param>
        /// <param name="flags">
        /// Upon return, receives the flags describing how the element must be
        /// quoted.
        /// </param>
        /// <returns>
        /// The maximum number of characters required to represent the quoted
        /// element.
        /// </returns>
        internal static int ScanElement(
            /* Interpreter interpreter, */ /* NOT USED */
            string text,
            int startIndex,
            int length,
            ref ListElementFlags flags
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            if (text == null)
                text = String.Empty;

            if (length < 0)
                length = text.Length;

            int lastIndex = startIndex + length;
            int index = startIndex;
            char character; /* REUSED */

            if (index == lastIndex)
            {
                flags |= ListElementFlags.UseBraces;
            }
            else
            {
                character = text[index];

                if ((character == Characters.OpenBrace) ||
                    (character == Characters.QuotationMark))
                {
                    flags |= ListElementFlags.UseBraces;
                }
            }

            int nestingLevel = 0;

            for (; index < lastIndex; index++)
            {
                character = text[index];

                switch (character)
                {
                    case Characters.OpenBrace:
                        {
                            nestingLevel++;
                            break;
                        }
                    case Characters.CloseBrace:
                        {
                            nestingLevel--;

                            if (nestingLevel < 0)
                                flags |= ListElementFlags.NoBracesMask;

                            break;
                        }
                    case Characters.Backslash:
                        {
                            if (((index + 1) == lastIndex) ||
                                IsLineTerminator(text[index + 1]))
                            {
                                flags |= ListElementFlags.NoBracesMask;
                            }
                            else
                            {
                                int read = 0;

                                ParseBackslash(
                                    text, index, text.Length - index,
                                    ref read);

                                index += (read - 1);
                                flags |= ListElementFlags.UseBraces;
                            }
                            break;
                        }
                    case Characters.OpenBracket:   /* FALL-THROUGH */
                    case Characters.DollarSign:
                    case Characters.SemiColon:
                    //
                    // HACK: *PERF* Previously, there was a "default"
                    //       case here with a call to the IsWhiteSpace
                    //       method here; however, that was too slow.
                    //
                    case Characters.HorizontalTab: /* FALL-THROUGH */
                    case Characters.LineFeed:
                    case Characters.VerticalTab:
                    case Characters.FormFeed:
                    case Characters.CarriageReturn:
                    case Characters.Space:
                        {
                            flags |= ListElementFlags.UseBraces;
                            break;
                        }
                }
            }

            if (nestingLevel != 0)
            {
                flags |= ListElementFlags.DontUseBraces |
                    ListElementFlags.BracesUnmatched;
            }

            /*
             * Allow enough space to backslash every character plus
             * leave two spaces for braces.
             */

            return ((2 * (index - startIndex)) + 2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a string into a list element, appending the
        /// result to the specified string builder.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the string, where conversion should begin.
        /// </param>
        /// <param name="length">
        /// The number of characters to convert, or a negative value to convert
        /// to the end of the string.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the element is quoted.
        /// </param>
        /// <param name="element">
        /// The string builder to append the converted element to, which is
        /// created when null.
        /// </param>
        /// <returns>
        /// The number of characters appended.
        /// </returns>
        internal static int ConvertElement(
            /* Interpreter interpreter, */ /* NOT USED */
            string text,
            int startIndex,
            int length,
            ListElementFlags flags,
            ref StringBuilder element
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            if ((text != null) && (length < 0))
                length = text.Length;

            if (element == null)
                element = StringBuilderFactory.CreateNoCache(); /* EXEMPT */

            int elementStartLength = element.Length;

            if ((text == null) || (length == 0))
            {
                element.Append(Characters.OpenBrace_CloseBrace);

                return 2;
            }

            int index = startIndex;

            if ((text[index] == Characters.NumberSign) &&
                !FlagOps.HasFlags(flags, ListElementFlags.DontQuoteHash, true))
            {
                flags |= ListElementFlags.UseBraces;
            }

            int lastIndex = startIndex + length;

            if (FlagOps.HasFlags(flags, ListElementFlags.UseBraces, true) &&
                !FlagOps.HasFlags(flags, ListElementFlags.DontUseBraces, true))
            {
                //
                // BUGFIX: *PERF* Append the whole sub-string in one shot.
                //
                element.Append(Characters.OpenBrace);
                element.Append(text, index, lastIndex - index);
                element.Append(Characters.CloseBrace);
            }
            else
            {
                if (text[index] == Characters.OpenBrace)
                {
                    element.Append(Characters.Backslash_OpenBrace);

                    index++;
                    flags |= ListElementFlags.BracesUnmatched;
                }
                else if ((text[index] == Characters.NumberSign) &&
                         !FlagOps.HasFlags(flags, ListElementFlags.DontQuoteHash, true))
                {
                    element.Append(Characters.Backslash_NumberSign);

                    index++;
                }

                //
                // NOTE: *PERF* These flags do not change beyond this point.
                //       Therefore, check the "unmatched braces" flag once and
                //       place the result in a local boolean variable.  This
                //       may result in slightly faster code when checking for
                //       this condition inside the loop.
                //
                bool bracesUnmatched = FlagOps.HasFlags(
                    flags, ListElementFlags.BracesUnmatched, true);

                for (; index != lastIndex; index++)
                {
                    char character = text[index];

                    switch (character)
                    {
                        case Characters.OpenBracket:
                        case Characters.CloseBracket:
                        case Characters.DollarSign:
                        case Characters.SemiColon:
                        case Characters.Space:
                        case Characters.Backslash:
                        case Characters.QuotationMark:
                            {
                                element.Append(Characters.Backslash);
                                break;
                            }
                        case Characters.OpenBrace:
                        case Characters.CloseBrace:
                            {
                                if (bracesUnmatched)
                                    element.Append(Characters.Backslash);

                                break;
                            }
                        case Characters.HorizontalTab:
                            {
                                element.Append(Characters.Backslash_t);
                                continue;
                            }
                        case Characters.LineFeed:
                            {
                                element.Append(Characters.Backslash_n);
                                continue;
                            }
                        case Characters.VerticalTab:
                            {
                                element.Append(Characters.Backslash_v);
                                continue;
                            }
                        case Characters.FormFeed:
                            {
                                element.Append(Characters.Backslash_f);
                                continue;
                            }
                        case Characters.CarriageReturn:
                            {
                                element.Append(Characters.Backslash_r);
                                continue;
                            }
                    }

                    element.Append(character);
                }
            }

            return element.Length - elementStartLength;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a string into a list element using backslash
        /// escaping, appending the result to the specified string builder.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the string, where conversion should begin.
        /// </param>
        /// <param name="length">
        /// The number of characters to convert, or a negative value to convert
        /// to the end of the string.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the element is escaped.
        /// </param>
        /// <param name="element">
        /// The string builder to append the converted element to, which is
        /// created when null.
        /// </param>
        /// <returns>
        /// The number of characters appended.
        /// </returns>
        internal static int BackslashElement(
            /* Interpreter interpreter, */ /* NOT USED */
            string text,
            int startIndex,
            int length,
            ListElementFlags flags,
            ref StringBuilder element
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            if ((text != null) && (length < 0))
                length = text.Length;

            if (element == null)
                element = StringBuilderFactory.CreateNoCache(); /* EXEMPT */

            int elementStartLength = element.Length;

            if ((text == null) || (length == 0))
            {
                element.Append(Characters.OpenBrace_CloseBrace);
                return 2;
            }

            bool backslashTab = FlagOps.HasFlags(
                flags, ListElementFlags.BackslashTab, true);

            bool backslashLine = FlagOps.HasFlags(
                flags, ListElementFlags.BackslashLine, true);

            bool backslashForm = FlagOps.HasFlags(
                flags, ListElementFlags.BackslashForm, true);

            bool backslashSpace = FlagOps.HasFlags(
                flags, ListElementFlags.BackslashSpace, true);

            int lastIndex = startIndex + length;

            for (int index = startIndex; index != lastIndex; index++)
            {
                char character = text[index];

                switch (character)
                {
                    //
                    // TODO: Maybe make some (more) of these escapes
                    //       optional?
                    //
                    case Characters.OpenBracket:
                    case Characters.CloseBracket:
                    case Characters.DollarSign:
                    case Characters.SemiColon:
                    case Characters.Backslash:
                    case Characters.QuotationMark:
                    case Characters.OpenBrace:
                    case Characters.CloseBrace:
                        {
                            element.Append(Characters.Backslash);
                            element.AppendFormat("x{0:X2}", (int)character);

                            continue;
                        }
                    case Characters.Space:
                        {
                            if (backslashSpace)
                            {
                                element.Append(Characters.Backslash);
                                element.AppendFormat("x{0:X2}", (int)character);
                                continue;
                            }

                            break;
                        }
                    case Characters.HorizontalTab:
                        {
                            if (backslashTab)
                            {
                                element.Append(Characters.Backslash_t);
                                continue;
                            }

                            break;
                        }
                    case Characters.LineFeed:
                        {
                            if (backslashLine)
                            {
                                element.Append(Characters.Backslash_n);
                                continue;
                            }

                            break;
                        }
                    case Characters.VerticalTab:
                        {
                            if (backslashTab)
                            {
                                element.Append(Characters.Backslash_v);
                                continue;
                            }

                            break;
                        }
                    case Characters.FormFeed:
                        {
                            if (backslashForm)
                            {
                                element.Append(Characters.Backslash_f);
                                continue;
                            }

                            break;
                        }
                    case Characters.CarriageReturn:
                        {
                            if (backslashLine)
                            {
                                element.Append(Characters.Backslash_r);
                                continue;
                            }

                            break;
                        }
                }

                element.Append(character);
            }

            return element.Length - elementStartLength;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the next element within the specified list text.
        /// </summary>
        /// <param name="text">
        /// The list text to search.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the list text, where searching should begin.
        /// </param>
        /// <param name="length">
        /// The number of characters available to search.
        /// </param>
        /// <param name="elementIndex">
        /// Upon success, receives the index where the element begins.
        /// </param>
        /// <param name="nextIndex">
        /// Upon success, receives the index where the next element begins.
        /// </param>
        /// <param name="elementLength">
        /// Upon success, receives the length of the element.
        /// </param>
        /// <param name="braces">
        /// Upon success, non-zero if the element was enclosed in braces.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        internal static ReturnCode FindElement(
            /* Interpreter interpreter, */ /* NOT USED */
            string text,
            int startIndex,
            int length,
            ref int elementIndex,
            ref int nextIndex,
            ref int elementLength,
            ref bool braces,
            ref Result error
            )
        {
            int index = startIndex;
            int limit = (index + length);

            while ((index < limit) && IsWhiteSpace(text[index]))
                index++;

            int localLength = 0;
            bool localBraces = false;
            int elementStartIndex;

            if (index == limit)
            {
                elementStartIndex = limit;
                goto done;
            }

            int openBraces = 0;
            bool inQuotes = false;

            if (text[index] == Characters.OpenBrace)
            {
                openBraces++;
                index++;
            }
            else if (text[index] == Characters.QuotationMark)
            {
                inQuotes = true;
                index++;
            }

            elementStartIndex = index;
            localBraces = (openBraces != 0);

            while (index < limit)
            {
                switch (text[index])
                {
                    case Characters.OpenBrace:
                        {
                            if (openBraces != 0)
                            {
                                openBraces++;
                            }
                            break;
                        }
                    case Characters.CloseBrace:
                        {
                            if (openBraces > 1)
                            {
                                openBraces--;
                            }
                            else if (openBraces == 1)
                            {
                                localLength = (index - elementStartIndex);
                                index++;

                                if ((index >= limit) || IsWhiteSpace(text[index]))
                                    goto done;

                                int errorIndex = index;

                                while ((errorIndex < limit) &&
                                       !IsWhiteSpace(text[errorIndex]) &&
                                       (errorIndex < index + ErrorScanLimit))
                                {
                                    errorIndex++;
                                }

                                error = String.Format(
                                    "list element in braces followed by \"{0}\" instead of space",
                                    text.Substring(index, errorIndex - index));

                                return ReturnCode.Error;
                            }
                            break;
                        }
                    case Characters.Backslash:
                        {
                            int read = 0;

                            ParseBackslash(
                                text, index, limit - index, ref read);

                            index += (read - 1);
                            break;
                        }
                    case Characters.QuotationMark:
                        {
                            if (inQuotes)
                            {
                                localLength = (index - elementStartIndex);
                                index++;

                                if ((index >= limit) || IsWhiteSpace(text[index]))
                                    goto done;

                                int errorIndex = index;

                                while ((errorIndex < limit) &&
                                       !IsWhiteSpace(text[errorIndex]) &&
                                       (errorIndex < index + ErrorScanLimit))
                                {
                                    errorIndex++;
                                }

                                error = String.Format(
                                    "list element in quotes followed by \"{0}\" instead of space",
                                    text.Substring(index, errorIndex - index));

                                return ReturnCode.Error;
                            }
                            break;
                        }
                    //
                    // HACK: *PERF* Previously, there was a "default"
                    //       case here with a call to the IsWhiteSpace
                    //       method here; however, that was too slow.
                    //
                    case Characters.HorizontalTab:
                    case Characters.LineFeed:
                    case Characters.VerticalTab:
                    case Characters.FormFeed:
                    case Characters.CarriageReturn:
                    case Characters.Space:
                        {
                            if ((openBraces == 0) && !inQuotes)
                            {
                                localLength = (index - elementStartIndex);
                                goto done;
                            }
                            break;
                        }
                }
                index++;
            }

            if (index == limit)
            {
                if (openBraces != 0)
                {
                    error = "unmatched open brace in list";
                    return ReturnCode.Error;
                }
                else if (inQuotes)
                {
                    error = "unmatched open quote in list";
                    return ReturnCode.Error;
                }
                localLength = (index - elementStartIndex);
            }

        done:
            while ((index < limit) && IsWhiteSpace(text[index]))
                index++;

            elementIndex = elementStartIndex;
            nextIndex = index;
            elementLength = localLength;
            braces = localBraces;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

#if (NATIVE && NATIVE_UTILITY) || CACHE_ARGUMENTLIST_TOSTRING || CACHE_STRINGLIST_TOSTRING
        /// <summary>
        /// This method determines whether the specified string is a valid list
        /// element separator.
        /// </summary>
        /// <param name="separator">
        /// The string to check.
        /// </param>
        /// <returns>
        /// True if the string is a valid list separator; otherwise, false.
        /// </returns>
        internal static bool IsListSeparator(
            string separator
            )
        {
            if (String.IsNullOrEmpty(separator))
                return false;

            if (separator.Length > 1)
                return false;

            return (separator[0] == Characters.Space);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified list text into its individual
        /// elements.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.
        /// </param>
        /// <param name="text">
        /// The list text to split.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the list text, where splitting should begin.
        /// </param>
        /// <param name="length">
        /// The number of characters to split, or a negative value to split to
        /// the end of the text.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero to produce a read-only list.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of elements.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SplitList(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            int startIndex,
            int length,
            bool readOnly,
            ref StringList list,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            return ParserOps<string>.SplitList(
                interpreter, text, startIndex, length, readOnly,
                ref list, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region String Match (i.e. the "glob" / "like") Algorithm
        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified glob-style pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="text">
        /// The text to match.
        /// </param>
        /// <param name="textStartIndex">
        /// The index, within the text, where matching should begin.
        /// </param>
        /// <param name="pattern">
        /// The glob-style pattern to match against.
        /// </param>
        /// <param name="patternStartIndex">
        /// The index, within the pattern, where matching should begin.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match.
        /// </param>
        /// <returns>
        /// True if the text matches the pattern; otherwise, false.
        /// </returns>
        public static bool StringMatch(
            Interpreter interpreter, /* NOT USED */
            string text,
            int textStartIndex,
            string pattern,
            int patternStartIndex,
            bool noCase
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            bool fail = false;

            return StringMatch(
                interpreter, text, textStartIndex, pattern,
                patternStartIndex, noCase, ref fail);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified glob-style pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="text">
        /// The text to match.
        /// </param>
        /// <param name="textStartIndex">
        /// The index, within the text, where matching should begin.
        /// </param>
        /// <param name="pattern">
        /// The glob-style pattern to match against.
        /// </param>
        /// <param name="patternStartIndex">
        /// The index, within the pattern, where matching should begin.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match.
        /// </param>
        /// <param name="fail">
        /// Upon return, non-zero if matching failed due to an error, such as
        /// running out of stack space.
        /// </param>
        /// <returns>
        /// True if the text matches the pattern; otherwise, false.
        /// </returns>
        private static bool StringMatch(
            Interpreter interpreter, /* NOT USED */
            string text,
            int textStartIndex,
            string pattern,
            int patternStartIndex,
            bool noCase,
            ref bool fail
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            if ((text == null) || (pattern == null))
                return false;

#if NATIVE
            int levels = 0;

            if (interpreter != null)
                levels = interpreter.EnterParserLevel();

            try
            {
#endif
                InterpreterFlags interpreterFlags = InterpreterFlags.None;

                if (interpreter != null)
                {
                    bool locked = false;

                    try
                    {
                        //
                        // HACK: Since querying the interpreter flags is mostly
                        //       "optional" for the purposes of this method and
                        //       it is in a hot-path, do not use a "hard" lock
                        //       here.
                        //
                        interpreter.InternalSoftTryLock(ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            interpreterFlags = interpreter.InterpreterFlagsNoLock;
                        }
                        else
                        {
                            TraceOps.LockTrace(
                                "StringMatch",
                                typeof(Parser).Name, false,
                                TracePriority.LockWarning,
                                interpreter.MaybeWhoHasLock());
                        }
                    }
                    finally
                    {
                        interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                    }
                }

#if NATIVE
                /* EXEMPT */
                bool stringMatchStackChecking = FlagOps.HasFlags(
                    interpreterFlags, InterpreterFlags.StringMatchStackChecking, true);
#endif

                /* EXEMPT */
                bool fixFor219233 = FlagOps.HasFlags(
                    interpreterFlags, InterpreterFlags.FixFor219233, true);

                int textLength = text.Length;
                int patternLength = pattern.Length;
                int textIndex = textStartIndex;
                int patternIndex = patternStartIndex;

                while (true)
                {
                    if (patternIndex >= patternLength)
                        return (textIndex >= textLength);

                    char patternChar = pattern[patternIndex];

                    if ((textIndex >= textLength) &&
                        (patternChar != Characters.Asterisk))
                    {
                        return false;
                    }

                    char ch1; /* NOTE: Current text character (below). */
                    char ch2; /* NOTE: Current pattern character (below). */

                    if (patternChar == Characters.Asterisk)
                    {
                        while ((bool)LogicOps.Y(++patternIndex,
                                (patternIndex < patternLength) &&
                                (pattern[patternIndex] == Characters.Asterisk)))
                        {
                            /* NO BODY */
                        }

                        if (patternIndex >= patternLength)
                            return true;

                        patternChar = pattern[patternIndex];
                        ch2 = patternChar;

                        while (true)
                        {
                            if ((patternChar != Characters.OpenBracket) &&
                                (patternChar != Characters.QuestionMark) &&
                                (patternChar != Characters.Backslash))
                            {
                                while (textIndex < textLength)
                                {
                                    ch1 = text[textIndex];

                                    if ((ch2 == ch1) || (noCase &&
                                        (Char.ToLower(ch2) == Char.ToLower(ch1))))
                                    {
                                        break;
                                    }

                                    textIndex++;
                                }
                            }

#if NATIVE
                            if (stringMatchStackChecking)
                            {
                                if (RuntimeOps.MaybeCheckForParserStackSpace(
                                        interpreter, levels) != ReturnCode.Ok)
                                {
                                    fail = true;
                                    return false;
                                }
                            }
#endif

                            if (StringMatch(
                                    interpreter, text, textIndex, pattern,
                                    patternIndex, noCase, ref fail))
                            {
                                return true;
                            }

                            if (fail)
                                return false;

                            if (textIndex >= textLength)
                                return false;

                            textIndex++;
                        }
                    }

                    if (patternChar == Characters.QuestionMark)
                    {
                        patternIndex++; textIndex++;
                        continue;
                    }

                    if (patternChar == Characters.OpenBracket)
                    {
                        patternIndex++;

                        ch1 = noCase ?
                            Char.ToLower(text[textIndex]) :
                            text[textIndex];

                        textIndex++;

                        char startChar;
                        char endChar;

                        while (true)
                        {
                            if ((patternIndex >= patternLength) ||
                                (pattern[patternIndex] == Characters.CloseBracket))
                            {
                                return false;
                            }

                            startChar = noCase ?
                                Char.ToLower(pattern[patternIndex]) :
                                pattern[patternIndex];

                            patternIndex++;

                            if ((patternIndex < patternLength) &&
                                (pattern[patternIndex] == Characters.MinusSign))
                            {
                                patternIndex++;

                                if (patternIndex >= patternLength)
                                    return false;

                                endChar = noCase ?
                                    Char.ToLower(pattern[patternIndex]) :
                                    pattern[patternIndex];

                                patternIndex++;

                                if (fixFor219233 &&
                                    (patternIndex >= patternLength) &&
                                    (endChar == Characters.CloseBracket))
                                {
                                    return false;
                                }

                                if (((startChar <= ch1) && (ch1 <= endChar)) ||
                                    ((endChar <= ch1) && (ch1 <= startChar)))
                                {
                                    break;
                                }
                            }
                            else if (startChar == ch1)
                            {
                                break;
                            }
                        }

                        while ((patternIndex < patternLength) &&
                               (pattern[patternIndex] != Characters.CloseBracket))
                        {
                            patternIndex++;
                        }

                        if (patternIndex < patternLength)
                            patternIndex++;

                        continue;
                    }

                    if (patternChar == Characters.Backslash)
                    {
                        patternIndex++;

                        if (patternIndex >= patternLength)
                            return false;
                    }

                    ch1 = text[textIndex++];
                    ch2 = pattern[patternIndex++];

                    if ((!noCase && (ch1 != ch2)) || (noCase &&
                        (Char.ToLower(ch1) != Char.ToLower(ch2))))
                    {
                        return false;
                    }
                }
#if NATIVE
            }
            finally
            {
                if (interpreter != null)
                    interpreter.ExitParserLevel();
            }
#endif
        }
        #endregion
    }
    #endregion
}
