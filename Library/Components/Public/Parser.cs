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
    [ObjectId("36d66e11-af5d-45ab-8c0a-fc77b4e08153")]
    public class ParseToken :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IToken
    {
        #region IGetClientData / ISetClientData Members
        private IClientData clientData; // RESERVED for application usage.
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { if (immutable) throw new InvalidOperationException(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region IScriptLocation Members
        private string fileName;
        public virtual string FileName
        {
            get { return fileName; }
            set { if (immutable) throw new InvalidOperationException(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int startLine;
        public virtual int StartLine
        {
            get { return startLine; }
            set { if (immutable) throw new InvalidOperationException(); startLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int endLine;
        public virtual int EndLine
        {
            get { return endLine; }
            set { if (immutable) throw new InvalidOperationException(); endLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private bool viaSource;
        public virtual bool ViaSource
        {
            get { return viaSource; }
            set { if (immutable) throw new InvalidOperationException(); viaSource = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public virtual StringPairList ToList()
        {
            return ToList(GetText(), false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public virtual StringPairList ToList(bool scrub)
        {
            return ToList(GetText(), scrub);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region IToken Members
        private IParseState parseState;    // Parser state that this token belongs to.
        public virtual IParseState ParseState
        {
            get { return parseState; }
            set { if (immutable) throw new InvalidOperationException(); parseState = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private TokenType type; // Type of token, such as 'Command'.
        public virtual TokenType Type
        {
            get { return type; }
            set { if (immutable) throw new InvalidOperationException(); type = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private TokenSyntaxType syntaxType;
        public virtual TokenSyntaxType SyntaxType
        {
            get { return syntaxType; }
            set { if (immutable) throw new InvalidOperationException(); syntaxType = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private TokenFlags flags;
        public virtual TokenFlags Flags
        {
            get { return flags; }
            set { if (immutable) throw new InvalidOperationException(); flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int start;      // Starting offset.
        public virtual int Start
        {
            get { return start; }
            set { if (immutable) throw new InvalidOperationException(); start = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int length;     // Length in characters.
        public virtual int Length
        {
            get { return length; }
            set { if (immutable) throw new InvalidOperationException(); length = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        // field tells how many of them there are (including
        // components of components, etc).  The component
        // tokens immediately follow this one.

        private int components; // if this token is composed of other tokens, this
        public virtual int Components
        {
            get { return components; }
            set { if (immutable) throw new InvalidOperationException(); components = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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

        private bool immutable;
        public virtual bool IsImmutable()
        {
            return immutable;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public virtual void MakeImmutable()
        {
            immutable = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public virtual void Save(
            out IToken token
            )
        {
            Save(this.ParseState, out token);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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

        public virtual StringPairList ToList(
            string text
            )
        {
            return ToList(text, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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

        public virtual string ToString(
            string text
            )
        {
            return ToList(text).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
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
        public override string ToString()
        {
            return ToString(GetText());
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Parse State Class
    [ObjectId("28ff2466-3e33-4c83-a8fb-f37fcf192ec6")]
    public class ParseState :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IParseState
    {
        #region Private Constructors
        internal ParseState(
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags
            )
            : this(engineFlags, substitutionFlags, null, Parser.StartLine)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
        public static IParseState Create()
        {
            return new ParseState(
                EngineFlags.None, SubstitutionFlags.Default);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region IParseState Members
        private bool notReady;
        public virtual bool NotReady
        {
            get { return notReady; }
            set { notReady = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private EngineFlags engineFlags;
        public virtual EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { if (immutable) throw new InvalidOperationException(); engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private SubstitutionFlags substitutionFlags;
        public virtual SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { if (immutable) throw new InvalidOperationException(); substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private string fileName;
        public virtual string FileName
        {
            get { return fileName; }
            set { if (immutable) throw new InvalidOperationException(); fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int currentLine;
        public virtual int CurrentLine
        {
            get { return currentLine; }
            set { if (immutable) throw new InvalidOperationException(); currentLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int lineStart;
        public virtual int LineStart
        {
            get { return lineStart; }
            set { if (immutable) throw new InvalidOperationException(); lineStart = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int commentStart;
        public virtual int CommentStart
        {
            get { return commentStart; }
            set { if (immutable) throw new InvalidOperationException(); commentStart = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int commentLength;
        public virtual int CommentLength
        {
            get { return commentLength; }
            set { if (immutable) throw new InvalidOperationException(); commentLength = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int commandStart;
        public virtual int CommandStart
        {
            get { return commandStart; }
            set { if (immutable) throw new InvalidOperationException(); commandStart = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int commandLength;
        public virtual int CommandLength
        {
            get { return commandLength; }
            set { if (immutable) throw new InvalidOperationException(); commandLength = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int commandWords;
        public virtual int CommandWords
        {
            get { return commandWords; }
            set { if (immutable) throw new InvalidOperationException(); commandWords = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private TokenFlags tokenFlags;
        public virtual TokenFlags TokenFlags
        {
            get { return tokenFlags; }
            set { if (immutable) throw new InvalidOperationException(); tokenFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private TokenList tokens;
        public virtual TokenList Tokens
        {
            get { return tokens; }
            set { if (immutable) throw new InvalidOperationException(); tokens = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private ParseError error;
        public virtual ParseError ParseError
        {
            get { return error; }
            set { if (immutable) throw new InvalidOperationException(); error = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private string text;
        public virtual string Text
        {
            get { return text; }
            set { if (immutable) throw new InvalidOperationException(); text = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int characters;
        public virtual int Characters
        {
            get { return characters; }
            set { if (immutable) throw new InvalidOperationException(); characters = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private int terminator;
        public virtual int Terminator
        {
            get { return terminator; }
            set { if (immutable) throw new InvalidOperationException(); terminator = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private bool incomplete;
        public virtual bool Incomplete
        {
            get { return incomplete; }
            set { if (immutable) throw new InvalidOperationException(); incomplete = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private bool immutable;
        public virtual bool IsImmutable()
        {
            return immutable;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
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
    [ObjectId("bead8508-f064-457d-8e30-39744966fd0d")]
    public static class Parser
    {
        #region Private Constants
        private static int TokenCapacity = 100;

        public static readonly int NoLine = -2;
        public static readonly int AnyLine = -1;
        public static readonly int StartLine = 1;
        public static readonly int UnknownLine = 0;

        internal const int BinaryRadix = 2;
        internal const int OctalRadix = 8;
        internal const int DecimalRadix = 10;
        internal const int HexadecimalRadix = 16;

        private const int AutomaticRadix = 0;
        private const int MinimumRadix = BinaryRadix;
        private const int MaximumRadix = 36;

        private const int ErrorScanLimit = 20;

        private const uint IntMinValue = unchecked((uint)(-int.MinValue));
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Parser Helper Methods
        #region Readiness Checking Methods
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
                interpreter, readyFlags, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Substitution Flags Methods
        private static bool HasBackslashes(
            SubstitutionFlags flags
            )
        {
            return ((flags & SubstitutionFlags.Backslashes) == SubstitutionFlags.Backslashes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private static bool HasVariables(
            SubstitutionFlags flags
            )
        {
            return ((flags & SubstitutionFlags.Variables) == SubstitutionFlags.Variables);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private static bool HasCommands(
            SubstitutionFlags flags
            )
        {
            return ((flags & SubstitutionFlags.Commands) == SubstitutionFlags.Commands);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Character Checking Methods
        private static bool IsTabOrSpace(
            char character
            )
        {
            return ((character == Characters.HorizontalTab) ||
                    (character == Characters.Space));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        internal static bool IsIdentifier(
            char character
            )
        {
            return Char.IsLetterOrDigit(character) ||
                   (character == Characters.Underscore);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        internal static bool IsLineTerminator(
            char character
            )
        {
            return (character == Characters.LineFeed);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
                default:
                    {
                        if (IsWhiteSpace(character))
                            characterType = CharacterType.Space;
                        else
                            characterType = CharacterType.None;
                    }
                    break;
            }

            return characterType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
        private static bool IsBinaryDigit(
            char character
            )
        {
            return ((character >= Characters.Zero) && (character <= Characters.One));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
                while ((index < text.Length) && (characters-- > 0))
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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base8 (Octal) Integer Parser
        private static bool IsOctalDigit(
            char character
            )
        {
            return ((character >= Characters.Zero) && (character <= Characters.Seven));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
                while ((index < text.Length) && (characters-- > 0))
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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base10 (Decimal) Integer Parser
        private static bool IsDecimalDigit(
            char character
            )
        {
            return ((character >= Characters.Zero) && (character <= Characters.Nine));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
                while ((index < text.Length) && (characters-- > 0))
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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base16 (Hexadecimal) Integer Parser
        internal static bool IsHexadecimalDigit(
            char character
            )
        {
            return ((IsDecimalDigit(character)) ||
                    ((character >= Characters.A) && (character <= Characters.F)) ||
                    ((character >= Characters.a) && (character <= Characters.f)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
                while ((index < text.Length) && (characters-- > 0))
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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Base26 (Hexavigesimal) Integer Parser
        private static bool IsHexavigesimalDigit(
            char character
            )
        {
            return (((character >= Characters.A) && (character <= Characters.Z)) ||
                    ((character >= Characters.a) && (character <= Characters.z)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
                while ((index < text.Length) && (characters-- > 0))
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
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Backslash Parser
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

                if ((index >= text.Length) || // TEST: Test this.
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
                        case Characters.Backslash: /* custom */
                            {
                                character1 = Characters.Backslash;
                                break;
                            }
                        case Characters.B:
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
                        case Characters.o:
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
                        case Characters.d:
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
                        case Characters.X:
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
                                } while ((index < text.Length) && // TEST: Test this.
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
        public static string Quote(
            string text
            )
        {
            return Quote(text, ListElementFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        internal static string Quote(
            string text,
            ListElementFlags flags
            )
        {
            int length = (text != null) ? text.Length : 0;
            StringBuilder result = StringBuilderFactory.Create(2 * length + 2);

            ScanElement(/* null, */ text, 0, length, ref flags);
            ConvertElement(/* null, */ text, 0, length, flags, ref result);

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public static bool NeedsQuoting(
            string text
            )
        {
            return NeedsQuoting(text, ListElementFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        private static bool NeedsQuoting(
            string text,
            ListElementFlags flags
            )
        {
            int length = (text != null) ? text.Length : 0;

            ScanElement(/* null, */ text, 0, length, ref flags);

            return ((flags & ListElementFlags.UseBraces) == ListElementFlags.UseBraces);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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

            if ((index == lastIndex) ||
                (text[index] == Characters.OpenBrace) ||
                (text[index] == Characters.QuotationMark))
            {
                flags |= ListElementFlags.UseBraces;
            }

            int nestingLevel = 0;

            for (; index < lastIndex; index++)
            {
                char character = text[index];

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
                                flags |= ListElementFlags.DontUseBraces |
                                    ListElementFlags.BracesUnmatched;

                            break;
                        }
                    case Characters.Backslash:
                        {
                            if (((index + 1) == lastIndex) ||
                                IsLineTerminator(text[index + 1]))
                            {
                                flags |= ListElementFlags.DontUseBraces |
                                    ListElementFlags.BracesUnmatched;
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
                    case Characters.OpenBracket:
                    case Characters.DollarSign:
                    case Characters.SemiColon:
                        {
                            flags |= ListElementFlags.UseBraces;
                            break;
                        }
                    default:
                        {
                            if (IsWhiteSpace(character))
                                flags |= ListElementFlags.UseBraces;

                            break;
                        }
                }
            }

            if (nestingLevel != 0)
                flags |= ListElementFlags.DontUseBraces |
                    ListElementFlags.BracesUnmatched;

            /*
             * Allow enough space to backslash every character plus leave two spaces
             * for braces.
             */

            return ((2 * (index - startIndex)) + 2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

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
                ((flags & ListElementFlags.DontQuoteHash) != ListElementFlags.DontQuoteHash))
            {
                flags |= ListElementFlags.UseBraces;
            }

            int lastIndex = startIndex + length;

            if (((flags & ListElementFlags.UseBraces) == ListElementFlags.UseBraces) &&
                ((flags & ListElementFlags.DontUseBraces) != ListElementFlags.DontUseBraces))
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
                         ((flags & ListElementFlags.DontQuoteHash) != ListElementFlags.DontQuoteHash))
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
                bool bracesUnmatched =
                    (flags & ListElementFlags.BracesUnmatched) == ListElementFlags.BracesUnmatched;

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
                                openBraces++;
                            break;
                        }
                    case Characters.CloseBrace:
                        {
                            if (openBraces > 1)
                                openBraces--;
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
                    default:
                        {
                            if (IsWhiteSpace(text[index]))
                            {
                                if ((openBraces == 0) && !inQuotes)
                                {
                                    localLength = (index - elementStartIndex);
                                    goto done;
                                }
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
                            TraceOps.DebugTrace(
                                "StringMatch: could not lock interpreter",
                                typeof(Parser).Name, TracePriority.LockWarning);
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
