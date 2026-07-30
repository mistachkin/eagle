/*
 * Expression.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;

#if NET_40
using System.Numerics;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using _ParseState = Eagle._Components.Public.ParseState;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    #region Expression Token Class
    /// <summary>
    /// This class represents a single token produced while parsing an Eagle
    /// expression.  It extends the general-purpose <see cref="ParseToken" />
    /// with expression-specific information, namely the <see cref="Lexeme" />
    /// that classifies the token (operator, literal, function name, etc.) and
    /// an optional <see cref="IVariant" /> value associated with it.  It
    /// implements <see cref="IExpressionToken" />.
    /// </summary>
    [ObjectId("d37cf7c6-37db-41f9-b0b5-33cd5a9c43d8")]
    public class ExpressionToken : ParseToken, IExpressionToken
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an expression token by copying the supplied token, using
        /// an unknown lexeme and no variant value.
        /// </summary>
        /// <param name="token">
        /// The token to copy.
        /// </param>
        private ExpressionToken(
            IToken token
            )
            : this(token, Lexeme.Unknown, null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an expression token by copying the supplied token and
        /// setting its lexeme and variant value.
        /// </summary>
        /// <param name="token">
        /// The token to copy.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme that classifies this token.
        /// </param>
        /// <param name="variant">
        /// The variant value to associate with this token, if any.  This
        /// parameter may be null.
        /// </param>
        private ExpressionToken(
            IToken token,
            Lexeme lexeme,
            IVariant variant
            )
            : base(token)
        {
            this.Lexeme = lexeme;
            this.Variant = variant;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an expression token associated with the supplied parse
        /// state, using an unknown lexeme and no variant value.
        /// </summary>
        /// <param name="parseState">
        /// The parse state this token belongs to.
        /// </param>
        private ExpressionToken(
            IParseState parseState
            )
            : base(parseState)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an expression token associated with the supplied parse
        /// state and sets its lexeme and variant value.
        /// </summary>
        /// <param name="parseState">
        /// The parse state this token belongs to.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme that classifies this token.
        /// </param>
        /// <param name="variant">
        /// The variant value to associate with this token, if any.  This
        /// parameter may be null.
        /// </param>
        private ExpressionToken(
            IParseState parseState,
            Lexeme lexeme,
            IVariant variant
            )
            : this(parseState)
        {
            this.Lexeme = lexeme;
            this.Variant = variant;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        /// <summary>
        /// This method creates (or reuses) an expression token from an existing
        /// token.  If the supplied token is already an expression token it is
        /// returned unchanged; if it is a parse token a new expression token is
        /// created from it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="token">
        /// The token to convert into (or interpret as) an expression token.
        /// </param>
        /// <returns>
        /// The resulting expression token, or null if the supplied token cannot
        /// be converted.
        /// </returns>
        public static IExpressionToken FromToken(
            Interpreter interpreter, /* NOT USED */
            IToken token
            )
        {
            if (token is IExpressionToken)
                return (IExpressionToken)token;
            else if (token is ParseToken)
                return new ExpressionToken(token);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new expression token from the supplied parse
        /// and expression states, using an unknown lexeme.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="parseState">
        /// The parse state the new token belongs to.
        /// </param>
        /// <param name="exprState">
        /// The expression state used to initialize the new token.
        /// </param>
        /// <returns>
        /// The newly created expression token.
        /// </returns>
        public static IExpressionToken FromState(
            Interpreter interpreter,
            IParseState parseState,
            IExpressionState exprState
            )
        {
            return FromState(
                interpreter, parseState, exprState, Lexeme.Unknown);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new expression token from the supplied parse
        /// and expression states, using the specified lexeme.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="parseState">
        /// The parse state the new token belongs to.
        /// </param>
        /// <param name="exprState">
        /// The expression state used to initialize the new token.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme that classifies the new token.
        /// </param>
        /// <returns>
        /// The newly created expression token.
        /// </returns>
        public static IExpressionToken FromState(
            Interpreter interpreter,
            IParseState parseState,
            IExpressionState exprState,
            Lexeme lexeme
            )
        {
            return FromState(
                interpreter, parseState, exprState, null, lexeme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new expression token from the supplied parse
        /// state, variant value, and lexeme.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="parseState">
        /// The parse state the new token belongs to.
        /// </param>
        /// <param name="exprState">
        /// The expression state.  This parameter is not used.
        /// </param>
        /// <param name="variant">
        /// The variant value to associate with the new token, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme that classifies the new token.
        /// </param>
        /// <returns>
        /// The newly created expression token.
        /// </returns>
        public static IExpressionToken FromState(
            Interpreter interpreter,    /* NOT USED */
            IParseState parseState,
            IExpressionState exprState, /* NOT USED */
            IVariant variant,
            Lexeme lexeme
            )
        {
            return new ExpressionToken(parseState, lexeme, variant);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Eagle._Interfaces.Public.IToken Overrides
        /// <summary>
        /// This method saves a snapshot of this token, using its own parse
        /// state.
        /// </summary>
        /// <param name="token">
        /// Upon success, receives the saved token snapshot.
        /// </param>
        public override void Save(
            out IToken token
            )
        {
            Save(base.ParseState, out token);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves a snapshot of this token, associating the snapshot
        /// with the supplied parse state and preserving this token's lexeme and
        /// variant value.
        /// </summary>
        /// <param name="parseState">
        /// The parse state to associate with the saved token snapshot.
        /// </param>
        /// <param name="token">
        /// Upon success, receives the saved token snapshot.
        /// </param>
        public override void Save(
            IParseState parseState,
            out IToken token
            )
        {
            IToken localToken;

            base.Save(parseState, out localToken);

            ExpressionToken exprToken = new ExpressionToken(
                localToken);

            exprToken.lexeme = this.lexeme;
            exprToken.variant = this.variant;

            token = localToken;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores this token's state from a previously saved
        /// token snapshot, including its lexeme and variant value.
        /// </summary>
        /// <param name="token">
        /// The previously saved token snapshot to restore from.  Upon success,
        /// this is set to null.
        /// </param>
        /// <returns>
        /// True if the token state was restored; otherwise, false.
        /// </returns>
        public override bool Restore(
            ref IToken token
            )
        {
            if (IsImmutable())
                return false;

            if (token == null)
                return false;

            ExpressionToken exprToken = token as ExpressionToken;

            if (exprToken == null)
                return false;

            IToken localToken = token;

            if (base.Restore(ref localToken))
            {
                this.lexeme = exprToken.lexeme;
                this.variant = exprToken.variant;

                token = null;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of name/value pairs describing this
        /// token, including its lexeme, optional variant value, and the
        /// information from the base token.
        /// </summary>
        /// <param name="text">
        /// The source text the token refers to, used to extract the token's
        /// textual representation.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing this token.
        /// </returns>
        public override StringPairList ToList(
            string text
            )
        {
            StringPairList list = new StringPairList();

            list.Add("Lexeme", this.Lexeme.ToString());

            IVariant variant = this.Variant;

            if (variant != null)
                list.Add("Variant", variant.ToString());

            list.Add(base.ToList(text));

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region IExpressionToken Members
        /// <summary>
        /// The lexeme that classifies this token.
        /// </summary>
        private Lexeme lexeme;
        /// <summary>
        /// Gets or sets the lexeme that classifies this token.
        /// </summary>
        public virtual Lexeme Lexeme
        {
            get { return lexeme; }
            set { if (IsImmutable()) throw new InvalidOperationException(); lexeme = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The variant value associated with this token, if any.
        /// </summary>
        private IVariant variant;
        /// <summary>
        /// Gets or sets the variant value associated with this token, if any.
        /// </summary>
        public virtual IVariant Variant
        {
            get { return variant; }
            set { if (IsImmutable()) throw new InvalidOperationException(); variant = value; }
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////

    #region Expression State Class
    /// <summary>
    /// This class holds the mutable state used while parsing an Eagle
    /// expression.  It tracks the current lexeme, the position information
    /// (start, length, and the various indexes into the source text), and a
    /// reference to the associated <see cref="IParseState" />.  Once parsing is
    /// complete the state can be made immutable, and it supports saving and
    /// restoring snapshots of itself.  It implements
    /// <see cref="IExpressionState" />.
    /// </summary>
    [ObjectId("a6d90ec7-f14c-4038-a8d4-5872c5de6fbb")]
    public class ExpressionState :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IExpressionState
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an expression state associated with the supplied parse
        /// state, optionally copying the position and lexeme information from
        /// another expression state.
        /// </summary>
        /// <param name="parseState">
        /// The parse state to associate with this expression state.
        /// </param>
        /// <param name="state">
        /// The expression state to copy position and lexeme information from, if
        /// any.  This parameter may be null.
        /// </param>
        internal ExpressionState(
            IParseState parseState,
            IExpressionState state
            )
        {
            this.ParseState = parseState;

            if (state != null)
            {
                this.NotReady = state.NotReady;
                this.Lexeme = state.Lexeme;
                this.Start = state.Start;
                this.Length = state.Length;
                this.Next = state.Next;
                this.PreviousEnd = state.PreviousEnd;
                this.Original = state.Original;
                this.Last = state.Last;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region IExpressionState Members
        /// <summary>
        /// The parse state associated with this expression state.
        /// </summary>
        private IParseState parseState;
        /// <summary>
        /// Gets or sets the parse state associated with this expression state.
        /// </summary>
        public virtual IParseState ParseState
        {
            get { return parseState; }
            set { if (immutable) throw new InvalidOperationException(); parseState = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When set, indicates the interpreter is not ready for expression
        /// processing.
        /// </summary>
        private bool notReady;
        /// <summary>
        /// Gets or sets a value indicating whether the interpreter is not ready
        /// for expression processing.  When an associated parse state is
        /// present, that parse state's value is used instead.
        /// </summary>
        public virtual bool NotReady
        {
            get
            {
                //
                // NOTE: Need to "cache" this so we call the virtual "Parse"
                //       property exactly once.
                //
                IParseState parseState = this.ParseState;

                return (parseState != null) ? parseState.NotReady : notReady;
            }
            set
            {
                //
                // NOTE: Need to "cache" this so we call the virtual "Parse"
                //       property exactly once.
                //
                IParseState parseState = this.ParseState;

                if (parseState != null)
                    parseState.NotReady = value;
                else
                    notReady = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The lexeme of the token most recently scanned.
        /// </summary>
        private Lexeme lexeme;
        /// <summary>
        /// Gets or sets the lexeme of the token most recently scanned.
        /// </summary>
        public virtual Lexeme Lexeme
        {
            get { return lexeme; }
            set { if (immutable) throw new InvalidOperationException(); lexeme = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The starting index, within the source text, of the token most
        /// recently scanned.
        /// </summary>
        private int start;
        /// <summary>
        /// Gets or sets the starting index, within the source text, of the
        /// token most recently scanned.
        /// </summary>
        public virtual int Start
        {
            get { return start; }
            set { if (immutable) throw new InvalidOperationException(); start = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The length, in characters, of the token most recently scanned.
        /// </summary>
        private int length;
        /// <summary>
        /// Gets or sets the length, in characters, of the token most recently
        /// scanned.
        /// </summary>
        public virtual int Length
        {
            get { return length; }
            set { if (immutable) throw new InvalidOperationException(); length = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The index, within the source text, where the next token begins.
        /// </summary>
        private int next;
        /// <summary>
        /// Gets or sets the index, within the source text, where the next token
        /// begins.
        /// </summary>
        public virtual int Next
        {
            get { return next; }
            set { if (immutable) throw new InvalidOperationException(); next = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The index, within the source text, marking the end of the previously
        /// scanned token.
        /// </summary>
        private int previousEnd;
        /// <summary>
        /// Gets or sets the index, within the source text, marking the end of
        /// the previously scanned token.
        /// </summary>
        public virtual int PreviousEnd
        {
            get { return previousEnd; }
            set { if (immutable) throw new InvalidOperationException(); previousEnd = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The original starting index, within the source text, of the
        /// expression being parsed.
        /// </summary>
        private int original;
        /// <summary>
        /// Gets or sets the original starting index, within the source text, of
        /// the expression being parsed.
        /// </summary>
        public virtual int Original
        {
            get { return original; }
            set { if (immutable) throw new InvalidOperationException(); original = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The index, within the source text, just past the end of the
        /// expression being parsed.
        /// </summary>
        private int last;
        /// <summary>
        /// Gets or sets the index, within the source text, just past the end of
        /// the expression being parsed.
        /// </summary>
        public virtual int Last
        {
            get { return last; }
            set { if (immutable) throw new InvalidOperationException(); last = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When set, indicates this expression state has been made immutable
        /// and can no longer be modified.
        /// </summary>
        private bool immutable;
        /// <summary>
        /// This method indicates whether this expression state has been made
        /// immutable.
        /// </summary>
        /// <returns>
        /// True if this expression state is immutable; otherwise, false.
        /// </returns>
        public virtual bool IsImmutable()
        {
            return immutable;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method makes this expression state (and its associated parse
        /// state, if any) immutable, preventing further modification.
        /// </summary>
        public virtual void MakeImmutable()
        {
            IParseState parseState = this.ParseState;

            if (parseState != null)
                parseState.MakeImmutable();

            immutable = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves a snapshot of this expression state, using its own
        /// parse state.
        /// </summary>
        /// <param name="exprState">
        /// Upon success, receives the saved expression state snapshot.
        /// </param>
        public virtual void Save(
            out IExpressionState exprState
            )
        {
            Save(this.ParseState, out exprState);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves a snapshot of this expression state, associating
        /// the snapshot with the supplied parse state.
        /// </summary>
        /// <param name="parseState">
        /// The parse state to associate with the saved snapshot.
        /// </param>
        /// <param name="exprState">
        /// Upon success, receives the saved expression state snapshot.
        /// </param>
        public virtual void Save(
            IParseState parseState,
            out IExpressionState exprState
            )
        {
            ExpressionState localExprState = new ExpressionState(
                parseState, null);

            localExprState.notReady = this.notReady;
            localExprState.lexeme = this.lexeme;
            localExprState.start = this.start;
            localExprState.length = this.length;
            localExprState.next = this.next;
            localExprState.previousEnd = this.previousEnd;
            localExprState.original = this.original;
            localExprState.last = this.last;
            localExprState.immutable = this.immutable;

            exprState = localExprState;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores this expression state from a previously saved
        /// snapshot.
        /// </summary>
        /// <param name="exprState">
        /// The previously saved expression state snapshot to restore from.
        /// Upon success, this is set to null.
        /// </param>
        /// <returns>
        /// True if the expression state was restored; otherwise, false.
        /// </returns>
        public virtual bool Restore(
            ref IExpressionState exprState
            )
        {
            if (immutable)
                return false;

            if (parseState == null)
                return false;

            ExpressionState localExprState = exprState as ExpressionState;

            if (localExprState == null)
                return false;

            this.parseState = localExprState.parseState;
            this.notReady = localExprState.notReady;
            this.lexeme = localExprState.lexeme;
            this.start = localExprState.start;
            this.length = localExprState.length;
            this.next = localExprState.next;
            this.previousEnd = localExprState.previousEnd;
            this.original = localExprState.original;
            this.last = localExprState.last;
            this.immutable = localExprState.immutable;

            exprState = null;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of name/value pairs describing this
        /// expression state, including its immutability, lexeme, position
        /// information, the corresponding text fragment, and the associated
        /// parse state.
        /// </summary>
        /// <param name="text">
        /// The source text the expression state refers to, used to extract the
        /// text fragment.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing this expression state.
        /// </returns>
        public virtual StringPairList ToList(
            string text
            )
        {
            StringPairList list = new StringPairList();

            list.Add("IsImmutable", this.IsImmutable().ToString());
            list.Add("Lexeme", this.Lexeme.ToString());

            //
            // NOTE: Need to "cache" these so we call the virtual "Start" and
            //       "Length" properties exactly once.
            //
            int start = this.Start;
            int length = this.Length;

            list.Add("Start", start.ToString());
            list.Add("Length", length.ToString());

            list.Add("Next", this.Next.ToString());
            list.Add("PreviousEnd", this.PreviousEnd.ToString());
            list.Add("Original", this.Original.ToString());
            list.Add("Last", this.Last.ToString());

            list.Add("Text", (text != null) ?
                ((length > 0) ?
                    text.Substring(start, length) :
                    text.Substring(start)) :
                String.Empty);

            //
            // NOTE: Need to "cache" this so we call the virtual "Parse" property
            //       exactly once.
            //
            IParseState parseState = this.ParseState;

            if (parseState != null)
                list.Add(parseState.ToList());

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string representation of this expression
        /// state.
        /// </summary>
        /// <param name="text">
        /// The source text the expression state refers to, used to extract the
        /// text fragment.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A string representation of this expression state.
        /// </returns>
        public virtual string ToString(
            string text
            )
        {
            return ToList(text).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string representation of this expression
        /// state, using the source text from its associated parse state if one
        /// is present.
        /// </summary>
        /// <returns>
        /// A string representation of this expression state.
        /// </returns>
        public override string ToString()
        {
            //
            // NOTE: Need to "cache" this so we call the virtual "Parse" property
            //       exactly once.
            //
            IParseState parseState = this.ParseState;

            return (parseState != null) ? ToString(parseState.Text) : ToString(null);
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////

    #region Expression Parser Class
    /// <summary>
    /// This class implements the recursive-descent parser for Eagle
    /// expressions.  It transforms the text of an expression into a flat list
    /// of <see cref="ExpressionToken" /> instances (held in a parse state) that
    /// can later be evaluated.  The parser honors standard operator precedence
    /// and associativity and is designed to be compatible with the Tcl 8.4
    /// expression grammar.  All members are static.
    /// </summary>
    [ObjectId("034801c3-eaaf-4f5d-bc57-6d9fc83e94ab")]
    public static class ExpressionParser
    {
        #region Private Constants
        /// <summary>
        /// The initial capacity used when creating the token list for a parsed
        /// expression.
        /// </summary>
        private static int TokenCapacity = 100;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method parses an Eagle expression contained within the supplied
        /// text, populating the supplied parse state with the resulting tokens.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="text">
        /// The source text containing the expression to parse.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the source text, where parsing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to parse.  If this value is less than zero,
        /// the remainder of the text is used.
        /// </param>
        /// <param name="parseState">
        /// The parse state to populate with the resulting tokens.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        public static ReturnCode ParseExpression(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters,
            IParseState parseState,
            bool noReady,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Parser.Ready(interpreter, parseState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (parseState != null)
                    parseState.NotReady = true;

                return code;
            }

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            if (characters < 0)
                characters = (text != null) ? text.Length : 0;

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
            parseState.Terminator = 0;
            parseState.Incomplete = false;

            IExpressionState exprState = new ExpressionState(
                parseState, null);

            exprState.Lexeme = Lexeme.Unknown;
            exprState.Start = Index.Invalid;
            exprState.Length = 0;
            exprState.Next = startIndex;
            exprState.PreviousEnd = startIndex;
            exprState.Original = startIndex;
            exprState.Last = startIndex + characters;

            code = GetLexeme(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                goto error;

            code = ParseConditional(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                goto error;

            if (exprState.Lexeme != Lexeme.End)
            {
                LogSyntaxError(exprState,
                    "extra tokens at end of expression", ref error);

                goto error;
            }

            return ReturnCode.Ok;

        error:
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied value is exactly the
        /// name of a supported expression operator.
        /// </summary>
        /// <param name="value">
        /// The value to check.
        /// </param>
        /// <returns>
        /// True if the value is the name of a supported expression operator;
        /// otherwise, false.
        /// </returns>
        public static bool IsOperatorNameOnly(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            switch (value)
            {
                case Operators.Exponent:
                case Operators.Multiply:
                case Operators.Divide:
                case Operators.Modulus:
                case Operators.Plus:
                case Operators.Minus:
                case Operators.LeftShift:
                case Operators.RightShift:
                case Operators.LeftRotate:
                case Operators.RightRotate:
                case Operators.LessThan:
                case Operators.GreaterThan:
                case Operators.LessThanOrEqualTo:
                case Operators.GreaterThanOrEqualTo:
                case Operators.Equal:
                case Operators.NotEqual:
                case Operators.BitwiseAnd:
                case Operators.BitwiseXor:
                case Operators.BitwiseOr:
                case Operators.BitwiseEqv:
                case Operators.BitwiseImp:
                case Operators.LogicalAnd:
                case Operators.LogicalXor:
                case Operators.LogicalOr:
                case Operators.LogicalEqv:
                case Operators.LogicalImp:
                case Operators.Question:
                case Operators.LogicalNot:
                case Operators.BitwiseNot:
                case Operators.StringEqual:
                case Operators.StringGreaterThan:
                case Operators.StringGreaterThanOrEqualTo:
                case Operators.StringLessThan:
                case Operators.StringLessThanOrEqualTo:
                case Operators.StringNotEqual:
                case Operators.ListIn:
                case Operators.ListNotIn:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method checks whether the interpreter is ready to continue
        /// expression processing (e.g. not over the recursion limit and not
        /// canceled).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="exprState">
        /// The expression state whose parse state is used for the readiness
        /// check.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok if the interpreter is ready; otherwise, an error
        /// return code.
        /// </returns>
        private static ReturnCode Ready(
            Interpreter interpreter,
            IExpressionState exprState,
            ref Result error
            )
        {
            return Parser.Ready(interpreter, (exprState != null) ?
                exprState.ParseState : null, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans the supplied text, starting at the specified
        /// index, to determine the length of a leading integer literal.  It
        /// recognizes the hexadecimal, decimal, octal, and binary radix
        /// prefixes, and (when supported) big-integer values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, used to determine whether big integers are
        /// permitted.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The source text to scan.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the source text, where scanning should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters available to scan.
        /// </param>
        /// <returns>
        /// The number of characters comprising the integer literal, or zero if
        /// the text does not begin with a valid integer literal.
        /// </returns>
        private static int ParseInteger(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters
            )
        {
            int index = startIndex;

            long number; /* REUSED */

#if NET_40
            BigInteger bigNumber; /* REUSED */
#endif

            int scanned; /* REUSED */

            if ((characters > 1) &&
                (text[index] == Characters.Zero) &&
                ((text[index + 1] == Characters.x) || (text[index + 1] == Characters.X)))
            {
                index += 2; characters -= 2;

                number = 0;
                scanned = Parser.ParseHexadecimal(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

#if NET_40
                if (ScriptOps.HasFlags(
                        interpreter, InterpreterFlags.AllowBigIntegers, true))
                {
                    bigNumber = BigInteger.Zero;
                    scanned = Parser.ParseHexadecimal(text, index, characters, ref bigNumber);

                    if (scanned > 0)
                        return scanned + 2;
                }
#endif

                return 1;
            }
            else if ((characters > 1) &&
                     (text[index] == Characters.Zero) &&
                     ((text[index + 1] == Characters.d) || (text[index + 1] == Characters.D)))
            {
                index += 2; characters -= 2;

                number = 0;
                scanned = Parser.ParseDecimal(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

#if NET_40
                if (ScriptOps.HasFlags(
                        interpreter, InterpreterFlags.AllowBigIntegers, true))
                {
                    bigNumber = BigInteger.Zero;
                    scanned = Parser.ParseDecimal(text, index, characters, ref bigNumber);

                    if (scanned > 0)
                        return scanned + 2;
                }
#endif

                return 1;
            }
            else if ((characters > 1) &&
                     (text[index] == Characters.Zero) &&
                     ((text[index + 1] == Characters.o) || (text[index + 1] == Characters.O)))
            {
                index += 2; characters -= 2;

                number = 0;
                scanned = Parser.ParseOctal(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

#if NET_40
                if (ScriptOps.HasFlags(
                        interpreter, InterpreterFlags.AllowBigIntegers, true))
                {
                    bigNumber = BigInteger.Zero;
                    scanned = Parser.ParseOctal(text, index, characters, ref bigNumber);

                    if (scanned > 0)
                        return scanned + 2;
                }
#endif

                return 1;
            }
            else if ((characters > 1) &&
                (text[index] == Characters.Zero) &&
                     ((text[index + 1] == Characters.b) || (text[index + 1] == Characters.B)))
            {
                index += 2; characters -= 2;

                number = 0;
                scanned = Parser.ParseBinary(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

#if NET_40
                if (ScriptOps.HasFlags(
                        interpreter, InterpreterFlags.AllowBigIntegers, true))
                {
                    bigNumber = BigInteger.Zero;
                    scanned = Parser.ParseBinary(text, index, characters, ref bigNumber);

                    if (scanned > 0)
                        return scanned + 2;
                }
#endif

                return 1;
            }

            while ((characters > 0) && Parser.IsInteger(text[index], false))
            {
                characters--; index++;
            }

            if (characters == 0)
            {
                return (index - startIndex);
            }
            else if ((text[index] != Characters.Period) &&
                     (text[index] != Characters.e) &&
                     (text[index] != Characters.E))
            {
                return (index - startIndex);
            }
            else
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans the supplied text, starting at the specified
        /// index, to determine the maximum length of characters that could
        /// comprise a floating-point literal (including digits, radix and
        /// exponent characters, signs, the radix point, and the characters used
        /// to spell infinity and not-a-number).
        /// </summary>
        /// <param name="text">
        /// The source text to scan.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the source text, where scanning should begin.
        /// </param>
        /// <param name="end">
        /// The index, within the source text, just past the last character that
        /// may be scanned.
        /// </param>
        /// <returns>
        /// The number of leading characters that could form a floating-point
        /// literal.
        /// </returns>
        private static int ParseMaxDoubleLength(
            string text,
            int startIndex,
            int end
            )
        {
            int index = startIndex;

            while (index < end)
            {
                switch (text[index])
                {
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
                    case Characters.A:
                    case Characters.B:
                    case Characters.C:
                    case Characters.D:
                    case Characters.E:
                    case Characters.F:
                    case Characters.I:
                    case Characters.N:
                    case Characters.P:
                    case Characters.X:
                    case Characters.a:
                    case Characters.b:
                    case Characters.c:
                    case Characters.d:
                    case Characters.e:
                    case Characters.f:
                    case Characters.i:
                    case Characters.n:
                    case Characters.p:
                    case Characters.t: // NOTE: Custom "Infinity".
                    case Characters.x:
                    case Characters.y: // NOTE: Custom "Infinity".
                    case Characters.Period:
                    case Characters.PlusSign:
                    case Characters.MinusSign:
                    case Characters.Infinity: // NOTE: Custom "InfinitySymbol".
                    case Characters.GreekSmallLetterPi:
                    case Characters.EulerConstant:
                        {
                            index++;
                            break;
                        }
                    default:
                        {
                            goto done;
                        }
                }
            }

        done:
            return (index - startIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a variable-assignment expression (the
        /// <c>:=</c> operator), which has the lowest precedence above a primary
        /// expression.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseVariableAssignment(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParsePrimary(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.VariableAssignment)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseConditional(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.VariableAssignment, operatorIndex,
                    Operators.VariableAssignment.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a conditional expression (the ternary
        /// <c>?:</c> operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseConditional(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseLogicalOr(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            if (exprState.Lexeme == Lexeme.Question)
            {
                IExpressionToken subToken = ExpressionToken.FromState(
                    interpreter, parseState, exprState);

                subToken.Type = TokenType.SubExpression;
                subToken.Start = sourceStart;

                IExpressionToken operatorToken = ExpressionToken.FromState(
                    interpreter, parseState, exprState, Lexeme.Question);

                operatorToken.Type = TokenType.Operator;
                operatorToken.Start = exprState.Start;
                operatorToken.Length = 1;
                operatorToken.Components = 0;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseConditional(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                if (exprState.Lexeme != Lexeme.Colon)
                {
                    LogSyntaxError(exprState,
                        "missing colon from ternary conditional", ref error);

                    return ReturnCode.Error;
                }

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseConditional(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                subToken.Length = (exprState.PreviousEnd - sourceStart);

                //
                // BUGFIX: must include the ones we have not added yet (below).
                //
                subToken.Components = (parseState.Tokens.Count + 2) - (firstIndex + 1);

                parseState.Tokens.InsertRange(firstIndex,
                    new IToken[] { subToken, operatorToken }, parseState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a logical-or expression (the <c>||</c>
        /// operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseLogicalOr(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseLogicalXor(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.LogicalOr)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseLogicalXor(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.LogicalOr, operatorIndex,
                    Operators.LogicalOr.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a logical-exclusive-or expression (the
        /// <c>^^</c> operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseLogicalXor(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseLogicalAnd(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.LogicalXor)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseLogicalAnd(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.LogicalXor, operatorIndex,
                    Operators.LogicalXor.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a logical-and expression (the <c>&amp;&amp;</c>
        /// operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseLogicalAnd(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseLogicalImp(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.LogicalAnd)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseLogicalImp(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.LogicalAnd, operatorIndex,
                    Operators.LogicalAnd.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a logical-implication expression (the
        /// <c>=&gt;</c> operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseLogicalImp(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseLogicalEqv(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.LogicalImp)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseLogicalEqv(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.LogicalImp, operatorIndex,
                    Operators.LogicalImp.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a logical-equivalence expression (the
        /// <c>&lt;=&gt;</c> operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseLogicalEqv(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseBitwiseOr(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.LogicalEqv)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseBitwiseOr(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.LogicalEqv, operatorIndex,
                    Operators.LogicalEqv.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a bitwise-or expression (the <c>|</c>
        /// operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseBitwiseOr(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseBitwiseXor(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.BitwiseOr)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseBitwiseXor(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.BitwiseOr, operatorIndex,
                    Operators.BitwiseOr.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a bitwise-exclusive-or expression (the
        /// <c>^</c> operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseBitwiseXor(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseBitwiseAnd(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.BitwiseXor)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseBitwiseAnd(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.BitwiseXor, operatorIndex,
                    Operators.BitwiseXor.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a bitwise-and expression (the <c>&amp;</c>
        /// operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseBitwiseAnd(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseBitwiseImp(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.BitwiseAnd)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseBitwiseImp(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.BitwiseAnd, operatorIndex,
                    Operators.BitwiseAnd.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a bitwise-implication expression (the
        /// <c>-&gt;</c> operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseBitwiseImp(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseBitwiseEqv(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.BitwiseImp)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseBitwiseEqv(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.BitwiseImp, operatorIndex,
                    Operators.BitwiseImp.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a bitwise-equivalence expression (the
        /// <c>&lt;-&gt;</c> operator).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseBitwiseEqv(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseMembership(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            while (exprState.Lexeme == Lexeme.BitwiseEqv)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseMembership(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.BitwiseEqv, operatorIndex,
                    Operators.BitwiseEqv.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a list-membership expression (the <c>in</c>
        /// and <c>ni</c> operators).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseMembership(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseEquality(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            Lexeme lexeme = exprState.Lexeme;

            while ((lexeme == Lexeme.ListIn) ||
                   (lexeme == Lexeme.ListNotIn))
            {
                int operatorIndex = exprState.Start;
                int operatorLength;

                if (lexeme == Lexeme.ListIn)
                    operatorLength = Operators.ListIn.Length;
                else
                    operatorLength = Operators.ListNotIn.Length;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseEquality(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, lexeme, operatorIndex,
                    operatorLength, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);

                lexeme = exprState.Lexeme;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses an equality expression (the <c>==</c>,
        /// <c>!=</c>, <c>eq</c>, and <c>ne</c> operators).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseEquality(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseRelational(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            Lexeme lexeme = exprState.Lexeme;

            while ((lexeme == Lexeme.Equal) ||
                   (lexeme == Lexeme.NotEqual) ||
                   (lexeme == Lexeme.StringEqual) ||
                   (lexeme == Lexeme.StringNotEqual))
            {
                int operatorIndex = exprState.Start;
                int operatorLength;

                if (lexeme == Lexeme.Equal)
                    operatorLength = Operators.Equal.Length;
                else if (lexeme == Lexeme.NotEqual)
                    operatorLength = Operators.NotEqual.Length;
                else if (lexeme == Lexeme.StringEqual)
                    operatorLength = Operators.StringEqual.Length;
                else
                    operatorLength = Operators.StringNotEqual.Length;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseRelational(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, lexeme, operatorIndex,
                    operatorLength, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);

                lexeme = exprState.Lexeme;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a relational expression (the <c>&lt;</c>,
        /// <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c>, <c>lt</c>, <c>gt</c>,
        /// <c>le</c>, and <c>ge</c> operators).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseRelational(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseShiftRotate(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            Lexeme lexeme = exprState.Lexeme;

            while ((lexeme == Lexeme.LessThan) ||
                   (lexeme == Lexeme.GreaterThan) ||
                   (lexeme == Lexeme.LessThanOrEqualTo) ||
                   (lexeme == Lexeme.GreaterThanOrEqualTo) ||
                   (lexeme == Lexeme.StringLessThan) ||
                   (lexeme == Lexeme.StringGreaterThan) ||
                   (lexeme == Lexeme.StringLessThanOrEqualTo) ||
                   (lexeme == Lexeme.StringGreaterThanOrEqualTo))
            {
                int operatorIndex = exprState.Start;
                int operatorLength;

                if (lexeme == Lexeme.LessThan)
                    operatorLength = Operators.LessThan.Length;
                else if (lexeme == Lexeme.GreaterThan)
                    operatorLength = Operators.GreaterThan.Length;
                else if (lexeme == Lexeme.LessThanOrEqualTo)
                    operatorLength = Operators.LessThanOrEqualTo.Length;
                else if (lexeme == Lexeme.GreaterThanOrEqualTo)
                    operatorLength = Operators.GreaterThanOrEqualTo.Length;
                else if (lexeme == Lexeme.StringLessThan)
                    operatorLength = Operators.StringLessThan.Length;
                else if (lexeme == Lexeme.StringGreaterThan)
                    operatorLength = Operators.StringGreaterThan.Length;
                else if (lexeme == Lexeme.StringLessThanOrEqualTo)
                    operatorLength = Operators.StringLessThanOrEqualTo.Length;
                else
                    operatorLength = Operators.StringGreaterThanOrEqualTo.Length;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseShiftRotate(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, lexeme, operatorIndex,
                    operatorLength, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);

                lexeme = exprState.Lexeme;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a shift or rotate expression (the <c>&lt;&lt;</c>,
        /// <c>&gt;&gt;</c>, <c>&lt;&lt;&lt;</c>, and <c>&gt;&gt;&gt;</c>
        /// operators).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseShiftRotate(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseAdd(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            Lexeme lexeme = exprState.Lexeme;

            while ((lexeme == Lexeme.LeftShift) ||
                   (lexeme == Lexeme.RightShift) ||
                   (lexeme == Lexeme.LeftRotate) ||
                   (lexeme == Lexeme.RightRotate))
            {
                int operatorIndex = exprState.Start;
                int operatorLength;

                if (lexeme == Lexeme.LeftShift)
                    operatorLength = Operators.LeftShift.Length;
                else if (lexeme == Lexeme.RightShift)
                    operatorLength = Operators.RightShift.Length;
                else if (lexeme == Lexeme.LeftRotate)
                    operatorLength = Operators.LeftRotate.Length;
                else
                    operatorLength = Operators.RightRotate.Length;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseAdd(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, lexeme, operatorIndex,
                    operatorLength, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);

                lexeme = exprState.Lexeme;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses an additive expression (the binary <c>+</c>
        /// and <c>-</c> operators).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseAdd(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseMultiply(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            Lexeme lexeme = exprState.Lexeme;

            while ((lexeme == Lexeme.Plus) ||
                   (lexeme == Lexeme.Minus))
            {
                int operatorIndex = exprState.Start;
                int operatorLength;

                if (lexeme == Lexeme.Plus)
                    operatorLength = Operators.Plus.Length;
                else
                    operatorLength = Operators.Minus.Length;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseMultiply(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, lexeme, operatorIndex,
                    operatorLength, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);

                lexeme = exprState.Lexeme;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a multiplicative expression (the <c>*</c>,
        /// <c>/</c>, and <c>%</c> operators).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseMultiply(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseExponent(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            Lexeme lexeme = exprState.Lexeme;

            while ((lexeme == Lexeme.Multiply) ||
                   (lexeme == Lexeme.Divide) ||
                   (lexeme == Lexeme.Modulus))
            {
                int operatorIndex = exprState.Start;
                int operatorLength;

                if (lexeme == Lexeme.Multiply)
                    operatorLength = Operators.Multiply.Length;
                else if (lexeme == Lexeme.Divide)
                    operatorLength = Operators.Divide.Length;
                else
                    operatorLength = Operators.Modulus.Length;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseExponent(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, lexeme, operatorIndex,
                    operatorLength, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);

                lexeme = exprState.Lexeme;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses an exponentiation expression (the <c>**</c>
        /// operator).  This operator is right-associative.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseExponent(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            code = ParseUnary(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            //
            // NOTE: The exponentiation operator is RIGHT-associative -- i.e.
            //       "a ** b ** c" means "a ** (b ** c)" -- matching standard
            //       mathematics and Tcl (the "**" operator was introduced in
            //       Tcl 8.5 as right-associative; it did not exist in 8.4).
            //       Parse the right-hand operand by RECURSING into this method
            //       (so a chain groups to the right) rather than looping over
            //       the right operands (which would group to the left).
            //
            if (exprState.Lexeme == Lexeme.Exponent)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseExponent(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, Lexeme.Exponent, operatorIndex,
                    Operators.Exponent.Length, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a unary expression (the unary <c>+</c>,
        /// <c>-</c>, <c>~</c>, and <c>!</c> operators); if no unary operator is
        /// present it falls through to a variable-assignment expression.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParseUnary(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            int sourceStart = exprState.Start;
            int firstIndex = parseState.Tokens.Count;

            Lexeme lexeme = exprState.Lexeme;

            if ((lexeme == Lexeme.Plus) ||
                (lexeme == Lexeme.Minus) ||
                (lexeme == Lexeme.BitwiseNot) ||
                (lexeme == Lexeme.LogicalNot))
            {
                int operatorIndex = exprState.Start;
                int operatorLength;

                if (lexeme == Lexeme.Plus)
                    operatorLength = Operators.Plus.Length;
                else if (lexeme == Lexeme.Minus)
                    operatorLength = Operators.Minus.Length;
                else if (lexeme == Lexeme.BitwiseNot)
                    operatorLength = Operators.BitwiseNot.Length;
                else
                    operatorLength = Operators.LogicalNot.Length;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseUnary(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                InsertSubExpressionTokens(
                    interpreter, lexeme, operatorIndex,
                    operatorLength, parseState.Text, sourceStart,
                    (exprState.PreviousEnd - sourceStart), firstIndex,
                    exprState);
            }
            else
            {
                code = ParseVariableAssignment(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a primary expression -- the highest-precedence
        /// grammar production -- comprising parenthesized sub-expressions,
        /// numeric and string literals, variable references, bracketed command
        /// substitutions, braced strings, and function calls.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode ParsePrimary(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            ReturnCode code;

            if (noReady || (interpreter == null))
                code = ReturnCode.Ok;
            else
                code = Ready(interpreter, exprState, ref error);

            if (code != ReturnCode.Ok)
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return code;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            Lexeme lexeme = exprState.Lexeme;

            if (lexeme == Lexeme.OpenParenthesis)
            {
                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseConditional(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                if (exprState.Lexeme != Lexeme.CloseParenthesis)
                {
                    LogSyntaxError(exprState,
                        "looking for close parenthesis", ref error);

                    return ReturnCode.Error;
                }

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                return ReturnCode.Ok;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            string text = parseState.Text;
            int exprIndex = parseState.Tokens.Count;

            IExpressionToken exprToken = ExpressionToken.FromState(
                interpreter, parseState, exprState);

            exprToken.Type = TokenType.SubExpression;
            exprToken.Start = exprState.Start;

            parseState.Tokens.Add(exprToken, parseState);

            int firstIndex = parseState.Tokens.Count;
            int terminator = Index.Invalid;

            IExpressionToken token;

            switch (lexeme)
            {
                case Lexeme.Literal: /* int, long, BigInteger, or double */
#if !MONO_BUILD
                //
                // HACK: Part of workaround for a bug in the Mono 2.10 C#
                //       compiler.
                //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                //
                tokenizeLiteral:
#endif
                    {
                        token = ExpressionToken.FromState(
                            interpreter, parseState, exprState, lexeme);

                        token.Type = TokenType.Text;
                        token.Start = exprState.Start;
                        token.Length = exprState.Length;
                        token.Components = 0;

                        parseState.Tokens.Add(token, parseState);

                        exprToken = ExpressionToken.FromToken(
                            interpreter, parseState.Tokens[exprIndex]);

                        exprToken.Length = exprState.Length;
                        exprToken.Components = 1;
                        break;
                    }
                case Lexeme.DollarSign: /* variable reference */
                    {
                        int dollarIndex = (exprState.Next - 1);

                        code = Parser.ParseVariableName(
                            interpreter, text, dollarIndex,
                            (exprState.Last - dollarIndex), parseState,
                            true, noReady, false, false, ref error);

                        if (code != ReturnCode.Ok)
                            return code;

                        exprState.Next = dollarIndex + parseState.Tokens[firstIndex].Length;

                        exprToken = ExpressionToken.FromToken(
                            interpreter, parseState.Tokens[exprIndex]);

                        exprToken.Length = parseState.Tokens[firstIndex].Length;
                        exprToken.Components = (parseState.Tokens[firstIndex].Components + 1);
                        break;
                    }
                case Lexeme.QuotationMark: /* quoted string */
                    {
                        int stringIndex = exprState.Next;

                        code = Parser.ParseQuotedString(
                            interpreter, text, exprState.Start,
                            (exprState.Last - stringIndex), parseState,
                            true, noReady, ref terminator, ref error);

                        if (code != ReturnCode.Ok)
                            return code;

                        exprState.Next = terminator;

                        exprToken = ExpressionToken.FromToken(
                            interpreter, parseState.Tokens[exprIndex]);

                        exprToken.Length = (terminator - exprToken.Start);
                        exprToken.Components = parseState.Tokens.Count - firstIndex;

                        if (exprToken.Components > 1)
                        {
                            exprToken = ExpressionToken.FromToken(
                                interpreter, parseState.Tokens[exprIndex]);

                            exprToken.Components++;

                            token = ExpressionToken.FromState(
                                interpreter, parseState, exprState);

                            token.Type = TokenType.Word;
                            token.Start = exprToken.Start;
                            token.Length = exprToken.Length;
                            token.Components = (exprToken.Components - 1);

                            parseState.Tokens.Insert(firstIndex, token, parseState);
                        }
                        break;
                    }
                case Lexeme.OpenBracket:
                    {
                        token = ExpressionToken.FromState(
                            interpreter, parseState, exprState);

                        token.Type = TokenType.Command;
                        token.Start = exprState.Start;
                        token.Components = 0;

                        parseState.Tokens.Add(token, parseState);

                        int index = exprState.Next;

                        while (true)
                        {
                            IParseState nestedParseState = new ParseState(
                                parseState.EngineFlags, parseState.SubstitutionFlags,
                                parseState.FileName, parseState.CurrentLine);

                            if (Parser.ParseCommand(
                                    interpreter, text, index,
                                    parseState.Characters - index, true, nestedParseState,
                                    noReady, ref error) != ReturnCode.Ok)
                            {
                                parseState.Terminator = nestedParseState.Terminator;
                                parseState.ParseError = nestedParseState.ParseError;
                                parseState.Incomplete = nestedParseState.Incomplete;
                                return ReturnCode.Error;
                            }

                            index = (nestedParseState.CommandStart + nestedParseState.CommandLength);

                            if ((nestedParseState.Terminator < parseState.Characters) &&
                                (text[nestedParseState.Terminator] == Characters.CloseBracket) &&
                                !nestedParseState.Incomplete)
                            {
                                break;
                            }

                            if (index == parseState.Characters)
                            {
                                error = "missing close-bracket";
                                parseState.Terminator = token.Start;
                                parseState.ParseError = ParseError.MissingBracket;
                                parseState.Incomplete = true;

                                return ReturnCode.Error;
                            }
                        }

                        token.Length = (index - token.Start);
                        exprState.Next = index;

                        exprToken = ExpressionToken.FromToken(
                            interpreter, parseState.Tokens[exprIndex]);

                        exprToken.Length = (index - token.Start);
                        exprToken.Components = 1;
                        break;
                    }
                case Lexeme.OpenBrace:
                    {
                        code = Parser.ParseBraces(
                            interpreter, text, exprState.Start,
                            (exprState.Last - exprState.Start), parseState,
                            true, noReady, ref terminator, ref error);

                        if (code != ReturnCode.Ok)
                            return code;

                        exprState.Next = terminator;

                        exprToken = ExpressionToken.FromToken(
                            interpreter, parseState.Tokens[exprIndex]);

                        exprToken.Length = (terminator - exprState.Start);
                        exprToken.Components = (parseState.Tokens.Count - firstIndex);

                        if (exprToken.Components > 1)
                        {
                            exprToken = ExpressionToken.FromToken(
                                interpreter, parseState.Tokens[exprIndex]);

                            exprToken.Components++;

                            token = ExpressionToken.FromState(
                                interpreter, parseState, exprState);

                            token.Type = TokenType.Word;
                            token.Start = exprToken.Start;
                            token.Length = exprToken.Length;
                            token.Components = (exprToken.Components - 1);

                            parseState.Tokens.Insert(firstIndex, token, parseState);
                        }
                        break;
                    }

                /*
                 * Disable attempt to support functions named "eq" or "ne".  This is
                 * unworkable with the Tcl 8.4.* compatible expression parser (per
                 * Don Porter).  See Tcl bugs 1971879 and 1201589.
                 *
                case Lexemes.StringEqual:
                case Lexemes.StringNotEqual:
                 */

                case Lexeme.ListIn:
                case Lexeme.ListNotIn:
                case Lexeme.IdentifierName:
                    {
                        IExpressionState savedExprState1;

                        exprState.Save(out savedExprState1);

                        code = GetLexeme(interpreter, exprState, noReady, ref error);

                        if (code != ReturnCode.Ok)
                            return code;

                        if (exprState.Lexeme != Lexeme.OpenParenthesis)
                        {
                            string value = text.Substring(
                                savedExprState1.Start, savedExprState1.Length);

                            CultureInfo cultureInfo = (interpreter != null) ?
                                interpreter.InternalCultureInfo : null;

                            //
                            // NOTE: If we can interpret the value as a boolean,
                            //       then it cannot be a function name.
                            //
                            bool boolValue = false;

                            if (Value.GetBoolean2(
                                    value, ValueFlags.AnyBoolean, cultureInfo,
                                    ref boolValue) == ReturnCode.Ok)
                            {
                                exprState.Restore(ref savedExprState1);

#if MONO_BUILD
                                //
                                // HACK: Part of workaround for a bug in the Mono 2.10 C#
                                //       compiler.
                                //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                                //
                                goto case Lexeme.Literal;
#else
                                goto tokenizeLiteral;
#endif
                            }

                            if (interpreter.DoesFunctionExist(value) == ReturnCode.Ok)
                            {
                                LogSyntaxError(savedExprState1,
                                    "expected parenthesis enclosing function arguments", ref error);
                            }
                            else
                            {
                                LogSyntaxError(savedExprState1,
                                    "variable references require preceding $", ref error);
                            }

                            return ReturnCode.Error;
                        }

                        token = ExpressionToken.FromState(
                            interpreter, parseState, exprState, lexeme);

                        token.Type = TokenType.Function;
                        token.Start = savedExprState1.Start;
                        token.Length = savedExprState1.Length;
                        token.Components = 0;

                        parseState.Tokens.Add(token, parseState);

                        code = GetLexeme(interpreter, exprState, noReady, ref error);

                        if (code != ReturnCode.Ok)
                            return code;

                        while (exprState.Lexeme != Lexeme.CloseParenthesis)
                        {
                            code = ParseConditional(interpreter, exprState, noReady, ref error);

                            if (code != ReturnCode.Ok)
                                return code;

                            if (exprState.Lexeme == Lexeme.Comma)
                            {
                                code = GetLexeme(interpreter, exprState, noReady, ref error);

                                if (code != ReturnCode.Ok)
                                    return code;
                            }
                            else if (exprState.Lexeme != Lexeme.CloseParenthesis)
                            {
                                LogSyntaxError(exprState,
                                    "missing close parenthesis at end of function call", ref error);

                                return ReturnCode.Error;
                            }
                        }

                        exprToken = ExpressionToken.FromToken(
                            interpreter, parseState.Tokens[exprIndex]);

                        exprToken.Length = (exprState.Next - exprToken.Start);
                        exprToken.Components = (parseState.Tokens.Count - firstIndex);
                        break;
                    }
                case Lexeme.Comma:
                    {
                        LogSyntaxError(exprState,
                            "commas can only separate function arguments", ref error);

                        return ReturnCode.Error;
                    }
                case Lexeme.End:
                    {
                        LogSyntaxError(exprState,
                            "premature end of expression", ref error);

                        return ReturnCode.Error;
                    }
                case Lexeme.Unknown:
                    {
                        LogSyntaxError(exprState,
                            "single equality character not legal in expressions", ref error);

                        return ReturnCode.Error;
                    }
                case Lexeme.UnknownCharacter:
                    {
                        LogSyntaxError(exprState,
                            "character not legal in expressions", ref error);

                        return ReturnCode.Error;
                    }
                case Lexeme.Question:
                    {
                        LogSyntaxError(exprState,
                            "unexpected ternary 'then' separator", ref error);

                        return ReturnCode.Error;
                    }
                case Lexeme.Colon:
                    {
                        LogSyntaxError(exprState,
                            "unexpected ternary 'else' separator", ref error);

                        return ReturnCode.Error;
                    }
                case Lexeme.CloseParenthesis:
                    {
                        LogSyntaxError(exprState,
                            "unexpected close parenthesis", ref error);

                        return ReturnCode.Error;
                    }
                default:
                    {
                        LogSyntaxError(exprState,
                            String.Format("unexpected operator {0}", lexeme), ref error);

                        return ReturnCode.Error;
                    }
            }

            code = GetLexeme(interpreter, exprState, noReady, ref error);

            if (code != ReturnCode.Ok)
                return code;

            parseState.Terminator = exprState.Next;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: More cleanup in here.  Revise operator lookup so that it is
        //       actually 100% based on the list of supported operators and
        //       does not assume the length of any given operator token.
        //
        /// <summary>
        /// This method scans the source text for the next lexeme (operator,
        /// literal, identifier, etc.), skipping any leading whitespace, and
        /// updates the expression state accordingly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null when
        /// <paramref name="noReady" /> is true.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and current lexeme;
        /// it is updated to reflect the lexeme that was scanned.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        private static ReturnCode GetLexeme(
            Interpreter interpreter,
            IExpressionState exprState,
            bool noReady,
            ref Result error
            )
        {
            if (!noReady && (interpreter != null) &&
                (Ready(interpreter, exprState, ref error) != ReturnCode.Ok))
            {
                if (exprState != null)
                    exprState.NotReady = true;

                return ReturnCode.Error;
            }

            if (exprState == null)
            {
                error = "invalid expression state";
                return ReturnCode.Error;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            string text = parseState.Text;
            int index;
            int length;
            int characters;

            exprState.PreviousEnd = exprState.Next;
            index = exprState.Next;
            characters = parseState.Characters - index;

            do
            {
                CharacterType characterType = CharacterType.None;

                int scanned = Parser.ParseWhiteSpace(interpreter, index,
                    characters, parseState, ref characterType, ref error);

                index += scanned; characters -= scanned;
            } while ((characters > 0) &&
                    Parser.IsLineTerminator(text[index]) &&
                    ((int)LogicOps.Y(index++, characters--) > 0));

            parseState.Terminator = index;

            if (characters == 0)
            {
                exprState.Lexeme = Lexeme.End;
                exprState.Next = index;
                return ReturnCode.Ok;
            }

            if ((text[index] != Characters.PlusSign) &&
                (text[index] != Characters.MinusSign))
            {
                CultureInfo cultureInfo = (interpreter != null) ?
                    interpreter.InternalCultureInfo : null;

                bool noInteger = false;
                int end = exprState.Last;

            retryNumber:

                if (!noInteger && ((length = ParseInteger(
                        interpreter, text, index, end - index)) > 0))
                {
                    string value = text.Substring(index, length);

                    //
                    // NOTE: See if we can parse and interpret the string as
                    //       "some kind" of integer value.
                    //
                    ulong ulongValue = 0;
                    Result localError = null;

                    if (Value.GetUnsignedWideInteger2(
                            value, ValueFlags.AnyWideInteger |
                            ValueFlags.Unsigned, cultureInfo, ref ulongValue,
                            ref localError) == ReturnCode.Error)
                    {
#if NET_40
                        bool noBigInteger = false;

                    retryBigInteger:

                        if (!noBigInteger && ScriptOps.HasFlags(interpreter,
                                InterpreterFlags.AllowBigIntegers, true))
                        {
                            BigInteger bigIntegerValue = BigInteger.Zero;
                            int stopIndex = Index.Invalid;

                            if ((Value.GetBigInteger2(
                                    value, ValueFlags.AnyInteger, cultureInfo,
                                    ref bigIntegerValue, ref stopIndex,
                                    ref localError) == ReturnCode.Error) &&
                                (stopIndex != Index.Invalid))
                            {
                                noBigInteger = true;
                                goto retryBigInteger;
                            }
                            else if (stopIndex != Index.Invalid)
                            {
                                exprState.Lexeme = Lexeme.Literal;
                                exprState.Start = index;

                                stopIndex += index;

                                if ((stopIndex - index) > length)
                                    exprState.Length = length;
                                else
                                    exprState.Length = (stopIndex - index);

                                exprState.Next = index + exprState.Length;
                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }
#endif

                        if (ScriptOps.HasFlags(interpreter,
                                InterpreterFlags.StrictExpressionInteger, true))
                        {
                            parseState.ParseError = ParseError.BadNumber;
                            error = localError;
                            return ReturnCode.Error;
                        }

                        noInteger = true;
                        goto retryNumber;
                    }

                    exprState.Lexeme = Lexeme.Literal;
                    exprState.Start = index;
                    exprState.Length = length;
                    exprState.Next = index + length;
                    parseState.Terminator = exprState.Next;

                    return ReturnCode.Ok;
                }
                else if ((length = ParseMaxDoubleLength(text, index, end)) > 0)
                {
                    string value = text.Substring(index, length);

                    //
                    // NOTE: See if we can parse and interpret the string
                    //       as some kind of floating-point value.
                    //
                    double doubleValue = 0.0;
                    int stopIndex = Index.Invalid;
                    Result localError = null;

                    if ((Value.GetDouble2(
                            value, ValueFlags.AnyDouble, cultureInfo,
                            ref doubleValue, ref stopIndex,
                            ref localError) == ReturnCode.Error) &&
                        (stopIndex != Index.Invalid))
                    {
                        parseState.ParseError = ParseError.BadNumber;
                        error = localError;
                        return ReturnCode.Error;
                    }
                    else if (stopIndex != Index.Invalid)
                    {
                        exprState.Lexeme = Lexeme.Literal;
                        exprState.Start = index;

                        stopIndex += index;

                        if ((stopIndex - index) > length)
                            exprState.Length = length;
                        else
                            exprState.Length = (stopIndex - index);

                        exprState.Next = index + exprState.Length;
                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                }
            }

            exprState.Start = index;
            exprState.Length = 1;
            exprState.Next = index + 1;
            parseState.Terminator = exprState.Next;

            switch (text[index])
            {
                case Characters.OpenBracket:
                    {
                        exprState.Lexeme = Lexeme.OpenBracket;

                        return ReturnCode.Ok;
                    }
                case Characters.OpenBrace:
                    {
                        exprState.Lexeme = Lexeme.OpenBrace;

                        return ReturnCode.Ok;
                    }
                case Characters.OpenParenthesis:
                    {
                        exprState.Lexeme = Lexeme.OpenParenthesis;

                        return ReturnCode.Ok;
                    }
                case Characters.CloseParenthesis:
                    {
                        exprState.Lexeme = Lexeme.CloseParenthesis;

                        return ReturnCode.Ok;
                    }
                case Characters.DollarSign:
                    {
                        exprState.Lexeme = Lexeme.DollarSign;

                        return ReturnCode.Ok;
                    }
                case Characters.QuotationMark:
                    {
                        exprState.Lexeme = Lexeme.QuotationMark;

                        return ReturnCode.Ok;
                    }
                case Characters.Comma:
                    {
                        exprState.Lexeme = Lexeme.Comma;

                        return ReturnCode.Ok;
                    }
                case Characters.Asterisk:
                    {
                        exprState.Lexeme = Lexeme.Multiply;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.Asterisk))
                        {
                            exprState.Lexeme = Lexeme.Exponent;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.Slash:
                    {
                        exprState.Lexeme = Lexeme.Divide;

                        return ReturnCode.Ok;
                    }
                case Characters.PercentSign:
                    {
                        exprState.Lexeme = Lexeme.Modulus;

                        return ReturnCode.Ok;
                    }
                case Characters.PlusSign:
                    {
                        exprState.Lexeme = Lexeme.Plus;

                        return ReturnCode.Ok;
                    }
                case Characters.MinusSign:
                    {
                        exprState.Lexeme = Lexeme.Minus;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.GreaterThanSign))
                        {
                            exprState.Lexeme = Lexeme.BitwiseImp;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.QuestionMark:
                    {
                        exprState.Lexeme = Lexeme.Question;

                        return ReturnCode.Ok;
                    }
                case Characters.Colon:
                    {
                        exprState.Lexeme = Lexeme.Colon;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.EqualSign))
                        {
                            exprState.Lexeme = Lexeme.VariableAssignment;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.LessThanSign:
                    {
                        exprState.Lexeme = Lexeme.LessThan;

                        if ((exprState.Last - index) > 1)
                        {
                            switch (text[index + 1])
                            {
                                case Characters.LessThanSign:
                                    {
                                        exprState.Lexeme = Lexeme.LeftShift;
                                        exprState.Length = 2;
                                        exprState.Next = index + 2;

                                        if ((exprState.Last - index) > 2)
                                        {
                                            switch (text[index + 2])
                                            {
                                                case Characters.LessThanSign:
                                                    {
                                                        exprState.Lexeme = Lexeme.LeftRotate;
                                                        exprState.Length = 3;
                                                        exprState.Next = index + 3;
                                                        break;
                                                    }
                                            }
                                        }
                                        break;
                                    }
                                case Characters.MinusSign:
                                    {
                                        if ((exprState.Last - index) > 2)
                                        {
                                            switch (text[index + 2])
                                            {
                                                case Characters.GreaterThanSign:
                                                    {
                                                        exprState.Lexeme = Lexeme.BitwiseEqv;
                                                        exprState.Length = 3;
                                                        exprState.Next = index + 3;
                                                        break;
                                                    }
                                            }
                                        }
                                        break;
                                    }
                                case Characters.EqualSign:
                                    {
                                        exprState.Lexeme = Lexeme.LessThanOrEqualTo;
                                        exprState.Length = 2;
                                        exprState.Next = index + 2;

                                        if ((exprState.Last - index) > 2)
                                        {
                                            switch (text[index + 2])
                                            {
                                                case Characters.GreaterThanSign:
                                                    {
                                                        exprState.Lexeme = Lexeme.LogicalEqv;
                                                        exprState.Length = 3;
                                                        exprState.Next = index + 3;
                                                        break;
                                                    }
                                            }
                                        }
                                        break;
                                    }
                            }
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.GreaterThanSign:
                    {
                        exprState.Lexeme = Lexeme.GreaterThan;

                        if ((exprState.Last - index) > 1)
                        {
                            switch (text[index + 1])
                            {
                                case Characters.GreaterThanSign:
                                    {
                                        exprState.Lexeme = Lexeme.RightShift;
                                        exprState.Length = 2;
                                        exprState.Next = index + 2;

                                        if ((exprState.Last - index) > 2)
                                        {
                                            switch (text[index + 2])
                                            {
                                                case Characters.GreaterThanSign:
                                                    {
                                                        exprState.Lexeme = Lexeme.RightRotate;
                                                        exprState.Length = 3;
                                                        exprState.Next = index + 3;
                                                        break;
                                                    }
                                            }
                                        }
                                        break;
                                    }
                                case Characters.EqualSign:
                                    {
                                        exprState.Lexeme = Lexeme.GreaterThanOrEqualTo;
                                        exprState.Length = 2;
                                        exprState.Next = index + 2;
                                        break;
                                    }
                            }
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.EqualSign:
                    {
                        exprState.Lexeme = Lexeme.Unknown;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.EqualSign))
                        {
                            exprState.Lexeme = Lexeme.Equal;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.GreaterThanSign))
                        {
                            exprState.Lexeme = Lexeme.LogicalImp;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.ExclamationMark:
                    {
                        exprState.Lexeme = Lexeme.LogicalNot;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.EqualSign))
                        {
                            exprState.Lexeme = Lexeme.NotEqual;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.Ampersand:
                    {
                        exprState.Lexeme = Lexeme.BitwiseAnd;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.Ampersand))
                        {
                            exprState.Lexeme = Lexeme.LogicalAnd;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.Caret:
                    {
                        exprState.Lexeme = Lexeme.BitwiseXor;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.Caret))
                        {
                            exprState.Lexeme = Lexeme.LogicalXor;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.Pipe:
                    {
                        exprState.Lexeme = Lexeme.BitwiseOr;

                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.Pipe))
                        {
                            exprState.Lexeme = Lexeme.LogicalOr;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }

                        parseState.Terminator = exprState.Next;

                        return ReturnCode.Ok;
                    }
                case Characters.Tilde:
                    {
                        exprState.Lexeme = Lexeme.BitwiseNot;

                        return ReturnCode.Ok;
                    }
                case Characters.e:
                    {
                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.q))
                        {
                            //
                            // BUGFIX: Fix "eq*()" functions being detected as the
                            //         "eq" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringEqual;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }

#if MONO_BUILD
                        //
                        // HACK: Part of workaround for a bug in the Mono 2.10 C#
                        //       compiler.
                        //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                        //
                        goto default;
#else
                        goto checkIdentifierName;
#endif
                    }
                case Characters.g:
                    {
                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.e))
                        {
                            //
                            // NOTE: Fix "ge*()" functions being detected as the
                            //       "ge" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringGreaterThanOrEqualTo;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.t))
                        {
                            //
                            // NOTE: Fix "gt*()" functions being detected as the
                            //       "gt" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringGreaterThan;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }

#if MONO_BUILD
                        //
                        // HACK: Part of workaround for a bug in the Mono 2.10 C#
                        //       compiler.
                        //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                        //
                        goto default;
#else
                        goto checkIdentifierName;
#endif
                    }
                case Characters.i:
                    {
                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.n))
                        {
                            //
                            // BUGFIX: Fix "in*()" functions being detected as the
                            //         "in" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.ListIn;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }

#if MONO_BUILD
                        //
                        // HACK: Part of workaround for a bug in the Mono 2.10 C#
                        //       compiler.
                        //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                        //
                        goto default;
#else
                        goto checkIdentifierName;
#endif
                    }
                case Characters.l:
                    {
                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.e))
                        {
                            //
                            // NOTE: Fix "le*()" functions being detected as the
                            //       "le" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringLessThanOrEqualTo;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.t))
                        {
                            //
                            // NOTE: Fix "lt*()" functions being detected as the
                            //       "lt" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringLessThan;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }

#if MONO_BUILD
                        //
                        // HACK: Part of workaround for a bug in the Mono 2.10 C#
                        //       compiler.
                        //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                        //
                        goto default;
#else
                        goto checkIdentifierName;
#endif
                    }
                case Characters.n:
                    {
                        if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.e))
                        {
                            //
                            // BUGFIX: Fix "ne*()" functions being detected as the
                            //         "ne" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringNotEqual;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (text[index + 1] == Characters.i))
                        {
                            //
                            // BUGFIX: Fix "ni*()" functions being detected as the
                            //         "ni" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.ListNotIn;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }

#if MONO_BUILD
                        //
                        // HACK: Part of workaround for a bug in the Mono 2.10 C#
                        //       compiler.
                        //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                        //
                        goto default;
#else
                        goto checkIdentifierName;
#endif
                    }
                default:
#if !MONO_BUILD
                //
                // HACK: Part of workaround for a bug in the Mono 2.10 C#
                //       compiler.
                //       https://bugzilla.novell.com/show_bug.cgi?id=671488
                //
                checkIdentifierName:
#endif
                    {
                        char character = text[index];

                        if (Char.IsLetter(character))
                        {
                            length = (exprState.Last - index);
                            exprState.Lexeme = Lexeme.IdentifierName;

                            while ((length > 0) &&
                                   Parser.IsIdentifier(text[index]))
                            {
                                index++; length--;
                            }

                            exprState.Length = (index - exprState.Start);
                            exprState.Next = index;

                            parseState.Terminator = exprState.Next;

                            return ReturnCode.Ok;
                        }

                        exprState.Lexeme = Lexeme.UnknownCharacter;

                        return ReturnCode.Ok;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method inserts the sub-expression and operator tokens for a
        /// binary (or similar) operator into the token list at the appropriate
        /// position, wrapping the already-parsed operands.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, used when reporting internal errors.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme that classifies the operator token.
        /// </param>
        /// <param name="operatorIndex">
        /// The starting index, within the source text, of the operator.
        /// </param>
        /// <param name="operatorCharacters">
        /// The length, in characters, of the operator.
        /// </param>
        /// <param name="text">
        /// The source text being parsed.
        /// </param>
        /// <param name="startIndex">
        /// The starting index, within the source text, of the entire
        /// sub-expression.
        /// </param>
        /// <param name="characters">
        /// The length, in characters, of the entire sub-expression.
        /// </param>
        /// <param name="firstIndex">
        /// The index, within the token list, where the new tokens are inserted.
        /// </param>
        /// <param name="exprState">
        /// The expression state tracking the parse position and providing the
        /// parse state whose token list is modified.
        /// </param>
        private static void InsertSubExpressionTokens(
            Interpreter interpreter,
            Lexeme lexeme,
            int operatorIndex,
            int operatorCharacters,
            string text,
            int startIndex,
            int characters,
            int firstIndex,
            IExpressionState exprState
            )
        {
            if (exprState == null)
            {
                //
                // NOTE: This should never happen, emit a complaint about it.
                //
                DebugOps.Complain(interpreter,
                    ReturnCode.Error, "invalid expression state");

                return;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                //
                // NOTE: This should never happen, emit a complaint about it.
                //
                DebugOps.Complain(interpreter,
                    ReturnCode.Error, "invalid parse state");

                return;
            }

            IExpressionToken subToken = ExpressionToken.FromState(
                interpreter, parseState, exprState);

            subToken.Type = TokenType.SubExpression;
            subToken.Start = startIndex;
            subToken.Length = characters;

            //
            // BUGFIX: must include the ones we have not added yet (below).
            //
            subToken.Components = (parseState.Tokens.Count + 2) - (firstIndex + 1);

            IExpressionToken operatorToken = ExpressionToken.FromState(
                interpreter, parseState, exprState, lexeme);

            operatorToken.Type = TokenType.Operator;
            operatorToken.Start = operatorIndex;
            operatorToken.Length = operatorCharacters;
            operatorToken.Components = 0;

            parseState.Tokens.InsertRange(firstIndex,
                new IToken[] { subToken, operatorToken }, parseState);

            return;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats and records a descriptive syntax-error message
        /// for the expression being parsed, including the original expression
        /// text and the location near where the error occurred.
        /// </summary>
        /// <param name="exprState">
        /// The expression state tracking the parse position and providing the
        /// parse state used to report the error.
        /// </param>
        /// <param name="extraInfo">
        /// Additional information describing the specific syntax error.
        /// </param>
        /// <param name="error">
        /// Receives the formatted syntax-error message.
        /// </param>
        private static void LogSyntaxError(
            IExpressionState exprState,
            string extraInfo,
            ref Result error
            )
        {
            if (exprState == null)
            {
                error = "invalid expression state";
                return;
            }

            IParseState parseState = exprState.ParseState;

            if (parseState == null)
            {
                error = "invalid parse state";
                return;
            }

            string text = parseState.Text;

            int originalIndex = exprState.Original;
            int nearIndex = exprState.Next - 1;

            string original = null;
            string near = null;

            if ((text != null) &&
                (originalIndex >= 0) && (originalIndex < text.Length))
            {
                original = text.Substring(originalIndex);

                if ((nearIndex > originalIndex) && (nearIndex < text.Length))
                    near = text.Substring(nearIndex);
            }

            error = String.Format(
                "syntax error in expression{0}{1}: {2}",
                (original != null) ? String.Format(
                    (originalIndex > 0) ? " \"{0}\" at index {1}" : " \"{0}\"",
                    original, originalIndex) : String.Empty,
                (near != null) ? String.Format(
                    (nearIndex > 0) ? " near \"{0}\" at index {1}" : " near \"{0}\"",
                    near, nearIndex) : String.Empty,
                extraInfo);

            parseState.ParseError = ParseError.Syntax;
            parseState.Terminator = exprState.Start;
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////

    #region Expression Evaluator Class
    /// <summary>
    /// This class evaluates the token list produced by the
    /// <see cref="ExpressionParser" />, recursively computing the value of each
    /// sub-expression.  It handles literals, variable and command
    /// substitutions, operators (including the short-circuiting logical
    /// operators and the ternary conditional), and math function calls, and it
    /// performs final-result fixups such as precision adjustment.  All members
    /// are static.
    /// </summary>
    [ObjectId("2a8a47c7-d933-4de1-ae6a-e46eaf5debfd")]
    internal static class ExpressionEvaluator
    {
        #region Private Methods
        #region Expression Flags Methods
#if EXPRESSION_FLAGS
        /// <summary>
        /// This method determines whether the supplied expression flags permit
        /// backslash substitution.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <returns>
        /// True if backslash substitution is permitted; otherwise, false.
        /// </returns>
        private static bool HasBackslashes(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Backslashes) == ExpressionFlags.Backslashes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied expression flags permit
        /// variable substitution.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <returns>
        /// True if variable substitution is permitted; otherwise, false.
        /// </returns>
        private static bool HasVariables(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Variables) == ExpressionFlags.Variables);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied expression flags permit
        /// command substitution.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <returns>
        /// True if command substitution is permitted; otherwise, false.
        /// </returns>
        private static bool HasCommands(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Commands) == ExpressionFlags.Commands);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied expression flags permit
        /// function calls.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <returns>
        /// True if function calls are permitted; otherwise, false.
        /// </returns>
        private static bool HasFunctions(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Functions) == ExpressionFlags.Functions);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied expression flags permit
        /// operators.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <returns>
        /// True if operators are permitted; otherwise, false.
        /// </returns>
        private static bool HasOperators(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Operators) == ExpressionFlags.Operators);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied expression flags permit
        /// substitutions, either requiring all substitution types or any of
        /// them.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <param name="all">
        /// When true, requires every substitution type to be present; when
        /// false, requires at least one substitution type to be present.
        /// </param>
        /// <returns>
        /// True if the required substitutions are permitted; otherwise, false.
        /// </returns>
        private static bool HasSubstitutions(
            ExpressionFlags flags,
            bool all
            )
        {
            if (all)
                return ((flags & ExpressionFlags.Substitutions) == ExpressionFlags.Substitutions);
            else
                return ((flags & ExpressionFlags.Substitutions) != ExpressionFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied expression flags request
        /// that a final boolean result be converted to an integer.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <returns>
        /// True if a final boolean result should be converted to an integer;
        /// otherwise, false.
        /// </returns>
        public static bool HasBooleanToInteger(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.BooleanToInteger) == ExpressionFlags.BooleanToInteger);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the supplied expression flags request
        /// that a final string result be converted to an integer when possible.
        /// </summary>
        /// <param name="flags">
        /// The expression flags to check.
        /// </param>
        /// <returns>
        /// True if a final string result should be converted to an integer;
        /// otherwise, false.
        /// </returns>
        public static bool HasStringToInteger(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.StringToInteger) == ExpressionFlags.StringToInteger);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a short-circuiting logical operator
        /// can produce its result from the value of its first operand alone,
        /// and if so computes that result.
        /// </summary>
        /// <param name="lexeme">
        /// The lexeme of the logical operator being evaluated (logical and, or,
        /// or implication).
        /// </param>
        /// <param name="inValue">
        /// The boolean value of the operator's first operand.
        /// </param>
        /// <param name="outValue">
        /// Upon success, when short-circuiting applies, receives the result of
        /// the operator.
        /// </param>
        /// <returns>
        /// True if the operator can be short-circuited (and
        /// <paramref name="outValue" /> was set); otherwise, false.
        /// </returns>
        private static bool CheckShortCircuit(
            Lexeme lexeme,
            bool inValue,
            ref bool outValue
            )
        {
            bool result = false;

            switch (lexeme)
            {
                case Lexeme.LogicalAnd:
                    {
                        result = !inValue;

                        if (result)
                            outValue = inValue;

                        break;
                    }
                case Lexeme.LogicalOr:
                    {
                        result = inValue;

                        if (result)
                            outValue = inValue;

                        break;
                    }
                case Lexeme.LogicalImp:
                    {
                        result = !inValue;

                        if (result)
                            outValue = !inValue;

                        break;
                    }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method evaluates a single sub-expression, identified by its
        /// token index within the parse state, recursively evaluating its
        /// operands and applying the appropriate operator or function.  It is
        /// the core of expression evaluation and may be called re-entrantly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="parseState">
        /// The parse state containing the token list to evaluate.
        /// </param>
        /// <param name="tokenIndex">
        /// The index, within the token list, of the sub-expression token to
        /// evaluate.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags controlling evaluation.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags controlling variable, command, and backslash
        /// substitution.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags controlling event processing during evaluation.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags controlling which constructs are permitted and
        /// how the final result is fixed up.
        /// </param>
        /// <param name="executeResultLimit">
        /// The maximum size permitted for a command-execution result.
        /// </param>
        /// <param name="nestedResultLimit">
        /// The maximum size permitted for a nested-evaluation result.
        /// </param>
        /// <param name="noReady">
        /// When true, the interpreter readiness check is skipped.
        /// </param>
        /// <param name="sameAppDomain">
        /// When true, the evaluation is known to occur within the same
        /// application domain.
        /// </param>
        /// <param name="argumentLocation">
        /// When true, argument location tracking is enabled for the debugger.
        /// </param>
        /// <param name="usable">
        /// Upon return, indicates whether the interpreter is still usable.
        /// </param>
        /// <param name="exception">
        /// Upon return, indicates whether an exception occurred during
        /// evaluation.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the computed value of the sub-expression.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
        public static ReturnCode EvaluateSubExpression(
            Interpreter interpreter,
            IParseState parseState,
            int tokenIndex,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool noReady,
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref bool usable,
            ref bool exception,
            ref Argument value,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (parseState == null)
            {
                error = "invalid parse state";
                return ReturnCode.Error;
            }

            string text = parseState.Text;
            TokenList tokens = parseState.Tokens;

            if (tokens == null)
            {
                error = "invalid token list";
                return ReturnCode.Error;
            }

            int count = tokens.Count;
            int index = tokenIndex;

            if ((index < 0) || (index >= count))
            {
                error = String.Format(
                    "initial token index {0} is out of bounds, have {1} " +
                    "tokens", index, count);

                return ReturnCode.Error;
            }

            IExpressionToken firstToken = ExpressionToken.FromToken(
                interpreter, tokens[index]);

            IExpressionToken token = firstToken;

            if (token.Type != TokenType.SubExpression)
            {
                error = String.Format(
                    "initial token type {0} is not {1}", token.Type,
                    TokenType.SubExpression);

                return ReturnCode.Error;
            }

            ReturnCode code = noReady ? ReturnCode.Ok :
                Parser.Ready(interpreter, parseState, ref error);

            if (code != ReturnCode.Ok)
            {
                parseState.NotReady = true;
                return code;
            }

            interpreter.EnterExpressionLevel();

            index++; // skip initial sub-expression.

            token = ExpressionToken.FromToken(
                interpreter, tokens[index]);

            switch (token.Type)
            {
                case TokenType.Word:
                    {
#if EXPRESSION_FLAGS
                        //
                        // BUGBUG: For now, we must insist on making sure
                        //         all the substitution types are present
                        //         before calling into the engine for
                        //         tokens to be processed.  We have to do
                        //         this because the engine does not
                        //         currently support evaluating only
                        //         certain token types.
                        //
                        if (!HasSubstitutions(expressionFlags, true))
                        {
                            error = String.Format(
                                "expression token type \"{0}\" forbidden: {1}",
                                token.Type, token.Text);

                            code = ReturnCode.Error;
                            goto done;
                        }
#endif

                        index++;

                        Result result = value;

                        code = Engine.EvaluateTokens(
                            interpreter, parseState, index,
#if RESULT_LIMITS
                            executeResultLimit,
                            nestedResultLimit,
#endif
                            token.Components, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            argumentLocation,
#endif
                            ref result);

                        if (code == ReturnCode.Ok)
                        {
                            value = result;
                        }
                        else
                        {
                            error = result;
                            goto done;
                        }

                        index += (token.Components + 1);
                        break;
                    }
                case TokenType.Text:
                    {
                        value = Argument.FromString(text.Substring(
                            token.Start, token.Length));

                        index++;
                        break;
                    }
                case TokenType.Backslash:
                    {
#if EXPRESSION_FLAGS
                        if (!HasBackslashes(expressionFlags))
                        {
                            error = String.Format(
                                "expression token type \"{0}\" forbidden: {1}",
                                token.Type, token.Text);

                            code = ReturnCode.Error;
                            goto done;
                        }
#endif

                        char? character1 = null;
                        char? character2 = null;

                        Parser.ParseBackslash(
                            text, token.Start, token.Length,
                            ref character1, ref character2);

                        value = Argument.FromCharacters(character1, character2);

                        index++;
                        break;
                    }
                case TokenType.Command:
                    {
#if EXPRESSION_FLAGS
                        if (!HasCommands(expressionFlags))
                        {
                            error = String.Format(
                                "expression token type \"{0}\" forbidden: {1}",
                                token.Type, token.Text);

                            code = ReturnCode.Error;
                            goto done;
                        }
#endif

                        Result result = value;

                        code = Engine.EvaluateScript(
                            interpreter, text, token.Start + 1,
                            token.Length - 2, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                            executeResultLimit, nestedResultLimit,
#endif
                            sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            argumentLocation,
#endif
                            ref result);

                        if (code == ReturnCode.Ok)
                        {
                            value = result;
                        }
                        else
                        {
                            error = result;
                            goto done;
                        }

                        index++;
                        break;
                    }
                case TokenType.Variable:
                case TokenType.VariableNameOnly:
                    {
#if EXPRESSION_FLAGS
                        if (!HasVariables(expressionFlags))
                        {
                            error = String.Format(
                                "expression token type \"{0}\" forbidden: {1}",
                                token.Type, token.Text);

                            code = ReturnCode.Error;
                            goto done;
                        }
#endif

                        Result result = value;

                        code = Engine.EvaluateTokens(
                            interpreter, parseState, index,
#if RESULT_LIMITS
                            executeResultLimit,
                            nestedResultLimit,
#endif
                            1, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags,
                            sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            argumentLocation,
#endif
                            ref result);

                        if (code == ReturnCode.Ok)
                        {
                            value = result;
                        }
                        else
                        {
                            error = result;
                            goto done;
                        }

                        index += (token.Components + 1);
                        break;
                    }
                case TokenType.SubExpression:
                    {
                        code = EvaluateSubExpression(
                            interpreter, parseState, index, engineFlags,
                            substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                            executeResultLimit, nestedResultLimit,
#endif
                            noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            argumentLocation,
#endif
                            ref usable, ref exception, ref value,
                            ref error);

                        if (code != ReturnCode.Ok)
                            goto done;
                        else if (!usable)
                            goto done;

                        index += (token.Components + 1);
                        break;
                    }
                case TokenType.Operator:
                case TokenType.Function:
                    {
                        string name = text.Substring(
                            token.Start, token.Length);

                        IOperator @operator = null;

                        if (interpreter.GetExpressionOperator(
                                token.Lexeme, name, ref @operator) != ReturnCode.Ok)
                        {
                            IFunction function = null;
                            Result localError = null;

                            if ((code = interpreter.GetExpressionFunction(
                                    name, ref function, ref localError)) == ReturnCode.Ok)
                            {
#if EXPRESSION_FLAGS
                                //
                                // NOTE: Yes, this is somewhat odd.  Why would
                                //       the caller forbid using functions in
                                //       an expression?  I suppose there could
                                //       be custom functions in the future that
                                //       have side-effects.
                                //
                                if (!HasFunctions(expressionFlags))
                                {
                                    error = String.Format(
                                        "expression token type \"{0}\" " +
                                        "forbidden: {1}", token.Type,
                                        token.Text);

                                    code = ReturnCode.Error;
                                    goto done;
                                }
#endif

                                index = tokenIndex;
                                token = firstToken;

                                int afterIndex = index + token.Components + 1;

                                index += 2; // skip func name and open paren.

                                ArgumentList arguments = new ArgumentList(
                                    function.Name);

                                if (function.Arguments != 0)
                                {
                                    //
                                    // NOTE: Function accepts a variable number
                                    //       of arguments?
                                    //
                                    bool hasArgs = (function.Arguments < 0);

                                    //
                                    // NOTE: Keep going until we process all
                                    //       the formal arguments for this
                                    //       function call OR if the function
                                    //       takes a variable number of
                                    //       arguments, until all the
                                    //       sub-components for this function
                                    //       token are exhausted.
                                    //
                                    for (int argumentIndex = 0;
                                            (argumentIndex < function.Arguments) ||
                                            (hasArgs && (index < afterIndex));
                                            argumentIndex++)
                                    {
                                        //
                                        // NOTE: Are there too few arguments
                                        //       for the function?  There may
                                        //       not be a hard-limit.
                                        //
                                        if ((index == afterIndex) &&
                                            (function.Arguments > 0))
                                        {
                                            error = String.Format(
                                                "too few arguments for math " +
                                                "function \"{0}\"", name);

                                            code = ReturnCode.Error;
                                            goto done;
                                        }

                                        Argument argument = null;

                                        code = EvaluateSubExpression(
                                            interpreter, parseState, index,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit, nestedResultLimit,
#endif
                                            noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            argumentLocation,
#endif
                                            ref usable, ref exception, ref argument,
                                            ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        arguments.Add(argument);

                                        token = ExpressionToken.FromToken(
                                            interpreter, tokens[index]);

                                        index += (token.Components + 1);
                                    }

                                    //
                                    // NOTE: Are there too many arguments for
                                    //       the function?  There may not be a
                                    //       hard-limit.
                                    //
                                    if ((index != afterIndex) &&
                                        (function.Arguments > 0))
                                    {
                                        error = String.Format(
                                            "too many arguments for math " +
                                            "function \"{0}\"", name);

                                        code = ReturnCode.Error;
                                        goto done;
                                    }
                                }
                                else if (index != afterIndex)
                                {
                                    error = String.Format(
                                        "too many arguments for math " +
                                        "function \"{0}\"", name);

                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                //
                                // NOTE: Perform function...
                                //
                                code = Engine.ExecuteFunction(
                                    function, interpreter, function.ClientData,
                                    arguments, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags,
#if RESULT_LIMITS
                                    executeResultLimit,
#endif
                                    ref usable, ref exception, ref value,
                                    ref error);

                                if (code != ReturnCode.Ok)
                                    goto done;
                                else if (!usable)
                                    goto done;

                                break;
                            }
                            else
                            {
                                error = localError;
                                goto done;
                            }
                        }
#if EXPRESSION_FLAGS
                        //
                        // NOTE: Yes, this is a bit odd.  Why would the caller
                        //       forbid using operators in an expression?  I
                        //       suppose there could be custom operators in the
                        //       future that have side-effects.
                        //
                        else if (!HasOperators(expressionFlags))
                        {
                            error = String.Format(
                                "expression token type \"{0}\" forbidden: {1}",
                                token.Type, token.Text);

                            code = ReturnCode.Error;
                            goto done;
                        }
#endif

                        if (!FlagOps.HasFlags(
                                @operator.Flags, OperatorFlags.Special, true))
                        {
                            index++;

                            token = ExpressionToken.FromToken(
                                interpreter, tokens[index]);

                            ArgumentList arguments = new ArgumentList(
                                @operator.Name);

                            Argument operand1 = null;

                            code = EvaluateSubExpression(
                                interpreter, parseState, index, engineFlags,
                                substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                                executeResultLimit, nestedResultLimit,
#endif
                                noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                argumentLocation,
#endif
                                ref usable, ref exception, ref operand1,
                                ref error);

                            if (code != ReturnCode.Ok)
                                goto done;
                            else if (!usable)
                                goto done;

                            arguments.Add(operand1);

                            index += (token.Components + 1);

                            if (@operator.Operands == 2)
                            {
                                Argument operand2 = null;

                                code = EvaluateSubExpression(
                                    interpreter, parseState, index,
                                    engineFlags, substitutionFlags, eventFlags,
                                    expressionFlags,
#if RESULT_LIMITS
                                    executeResultLimit, nestedResultLimit,
#endif
                                    noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                    argumentLocation,
#endif
                                    ref usable, ref exception, ref operand2,
                                    ref error);

                                if (code != ReturnCode.Ok)
                                    goto done;
                                else if (!usable)
                                    goto done;

                                arguments.Add(operand2);

                                token = ExpressionToken.FromToken(
                                    interpreter, tokens[index]);

                                index += (token.Components + 1);
                            }

                            //
                            // NOTE: Perform normal operator...
                            //
                            code = Engine.ExecuteOperator(
                                @operator, interpreter, @operator.ClientData,
                                arguments, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags,
#if RESULT_LIMITS
                                executeResultLimit,
#endif
                                ref usable, ref exception, ref value,
                                ref error);

                            if (code != ReturnCode.Ok)
                                goto done;
                            else if (!usable)
                                goto done;

                            break;
                        }

                        //
                        // NOTE: Handle the special case operators...
                        //
                        {
                            ArgumentList arguments = new ArgumentList(
                                @operator.Name);

                            switch (@operator.Lexeme)
                            {
                                case Lexeme.Plus:
                                case Lexeme.Minus:
                                    {
                                        index++;

                                        token = ExpressionToken.FromToken(
                                            interpreter, tokens[index]);

                                        Argument operand1 = null;

                                        code = EvaluateSubExpression(
                                            interpreter, parseState, index,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit, nestedResultLimit,
#endif
                                            noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            argumentLocation,
#endif
                                            ref usable, ref exception, ref operand1,
                                            ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        arguments.Add(operand1);

                                        index += (token.Components + 1);

                                        if (index == (tokenIndex +
                                                firstToken.Components + 1))
                                        {
                                            //
                                            // NOTE: Perform special operator
                                            //       (unary plus/minus)...
                                            //
                                            code = Engine.ExecuteOperator(
                                                @operator, interpreter,
                                                @operator.ClientData,
                                                arguments, engineFlags,
                                                substitutionFlags, eventFlags,
                                                expressionFlags,
#if RESULT_LIMITS
                                                executeResultLimit,
#endif
                                                ref usable, ref exception,
                                                ref value, ref error);

                                            if (code != ReturnCode.Ok)
                                                goto done;
                                            else if (!usable)
                                                goto done;

                                            break;
                                        }

                                        //
                                        // binary plus or minus...
                                        //
                                        Argument operand2 = null;

                                        code = EvaluateSubExpression(
                                            interpreter, parseState, index,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit, nestedResultLimit,
#endif
                                            noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            argumentLocation,
#endif
                                            ref usable, ref exception, ref operand2,
                                            ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        arguments.Add(operand2);

                                        token = ExpressionToken.FromToken(
                                            interpreter, tokens[index]);

                                        index += (token.Components + 1);

                                        //
                                        // NOTE: Perform special operator
                                        //       (binary plus/minus)...
                                        //
                                        code = Engine.ExecuteOperator(
                                            @operator, interpreter,
                                            @operator.ClientData, arguments,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit,
#endif
                                            ref usable, ref exception,
                                            ref value, ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        break;
                                    }
                                case Lexeme.LogicalAnd: // short circuit (if operand1 is FALSE)
                                case Lexeme.LogicalOr:  // short circuit (if operand1 is TRUE)
                                case Lexeme.LogicalImp: // short circuit (if operand1 is FALSE)
                                    {
                                        index = tokenIndex + 2;

                                        //
                                        // NOTE: Evaluate first operand.
                                        //
                                        Argument operand1 = null;

                                        code = EvaluateSubExpression(
                                            interpreter, parseState, index,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit, nestedResultLimit,
#endif
                                            noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            argumentLocation,
#endif
                                            ref usable, ref exception, ref operand1,
                                            ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        //
                                        // NOTE: Convert first operand value to
                                        //       boolean and check...
                                        //
                                        bool boolInValue = false;

                                        code = Engine.ToBoolean(
                                            operand1, interpreter.InternalCultureInfo,
                                            ref boolInValue, ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;

                                        bool boolOutValue = false;

                                        if (CheckShortCircuit(@operator.Lexeme,
                                                boolInValue, ref boolOutValue))
                                        {
                                            value = Argument.FromBoolean(
                                                boolOutValue);

                                            break;
                                        }

                                        arguments.Add(operand1);

                                        token = ExpressionToken.FromToken(
                                            interpreter, tokens[index]);

                                        index += (token.Components + 1);

                                        //
                                        // NOTE: Evaluate second operand.
                                        //       SHORT CIRCUIT FIXUP HERE.
                                        //
                                        Argument operand2 = null;

                                        code = EvaluateSubExpression(
                                            interpreter, parseState, index,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit, nestedResultLimit,
#endif
                                            noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            argumentLocation,
#endif
                                            ref usable, ref exception, ref operand2,
                                            ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        arguments.Add(operand2);

                                        token = ExpressionToken.FromToken(
                                            interpreter, tokens[index]);

                                        index += (token.Components + 1);

                                        //
                                        // NOTE: Perform special operator
                                        //       (logical and/or/imp)...
                                        //
                                        code = Engine.ExecuteOperator(
                                            @operator, interpreter,
                                            @operator.ClientData, arguments,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit,
#endif
                                            ref usable, ref exception,
                                            ref value, ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        break;
                                    }
                                case Lexeme.Question: // if-then semantics...
                                    {                 // evaluate matching
                                                      // expression.
                                        index = tokenIndex + 2;

                                        token = ExpressionToken.FromToken(
                                            interpreter, tokens[index]);

                                        ///////////////////////////////////////
                                        // EVALUATE LOGICAL EXPRESSION
                                        ///////////////////////////////////////

                                        Argument operand1 = null;

                                        code = EvaluateSubExpression( // if
                                            interpreter, parseState, index,
                                            engineFlags, substitutionFlags,
                                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit, nestedResultLimit,
#endif
                                            noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            argumentLocation,
#endif
                                            ref usable, ref exception, ref operand1,
                                            ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;
                                        else if (!usable)
                                            goto done;

                                        index += (token.Components + 1);

                                        //
                                        // NOTE: Convert first operand value to
                                        //       boolean and check...
                                        //
                                        bool boolInValue = false;

                                        code = Engine.ToBoolean(
                                            operand1, interpreter.InternalCultureInfo,
                                            ref boolInValue, ref error);

                                        if (code != ReturnCode.Ok)
                                            goto done;

                                        ///////////////////////////////////////
                                        // EVALUATE TRUE BRANCH
                                        ///////////////////////////////////////

                                        if (boolInValue) // TRUE PART
                                        {
                                            code = EvaluateSubExpression( // then
                                                interpreter, parseState, index,
                                                engineFlags, substitutionFlags,
                                                eventFlags, expressionFlags,
#if RESULT_LIMITS
                                                executeResultLimit, nestedResultLimit,
#endif
                                                noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                                argumentLocation,
#endif
                                                ref usable, ref exception, ref value,
                                                ref error);

                                            if (code != ReturnCode.Ok)
                                                goto done;
                                            else if (!usable)
                                                goto done;
                                            else
                                                break;
                                        }

                                        ///////////////////////////////////////

                                        token = ExpressionToken.FromToken(
                                            interpreter, tokens[index]);

                                        index += (token.Components + 1);

                                        ///////////////////////////////////////
                                        // EVALUATE FALSE BRANCH
                                        ///////////////////////////////////////

                                        if (!boolInValue) // FALSE PART
                                        {
                                            code = EvaluateSubExpression( // else
                                                interpreter, parseState, index,
                                                engineFlags, substitutionFlags,
                                                eventFlags, expressionFlags,
#if RESULT_LIMITS
                                                executeResultLimit, nestedResultLimit,
#endif
                                                noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                                argumentLocation,
#endif
                                                ref usable, ref exception, ref value,
                                                ref error);

                                            if (code != ReturnCode.Ok)
                                                goto done;
                                            else if (!usable)
                                                goto done;
                                            else
                                                break;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        error = String.Format(
                                            "unexpected operator {0} " +
                                            "requiring special treatment",
                                            @operator.Lexeme);

                                        break;
                                    }
                            }
                        }
                        break;
                    }
                default:
                    {
                        error = String.Format(
                            "unexpected token type {0} for sub-expression",
                            token.Type);

                        code = ReturnCode.Error;
                        break;
                    }
            }

        done:

            if (usable)
            {
                //
                // NOTE: If this is going to be the final result of the whole
                //       expression, fixup the precision to produce the actual
                //       final result.
                //
                if (interpreter.IsOuterSubExpression()) /* SIDE-EFFECT */
                {
                    if ((code == ReturnCode.Ok) && (value != null))
                    {
                        try
                        {
                            object innerValue = value.Value;

                            if (innerValue is decimal)
                            {
                                value = interpreter.FixFinalPrecision(
                                    (decimal)innerValue); /* throw */
                            }
                            else if (innerValue is double)
                            {
                                value = interpreter.FixFinalPrecision(
                                    (double)innerValue); /* throw */
                            }
                            //
                            // NOTE: If the final result of the expression is
                            //       a boolean value and the BooleanToInteger
                            //       flag is set, then automatically convert
                            //       the final result to an integer instead
                            //       (COMPAT: Tcl).
                            //
                            else if (innerValue is bool)
                            {
                                if (HasBooleanToInteger(expressionFlags))
                                {
                                    value = ConversionOps.ToInt(
                                        (bool)innerValue);
                                }
                            }
                            else if (innerValue is string)
                            {
                                if (HasStringToInteger(expressionFlags))
                                {
                                    long longValue = 0;

                                    if (Value.GetWideInteger2(
                                            (string)innerValue,
                                            ValueFlags.AnyWideInteger,
                                            null, ref longValue) == ReturnCode.Ok)
                                    {
                                        value = longValue;
                                    }
                                }
                            }
#if DEBUG && VERBOSE
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "EvaluateSubExpression: skipped " +
                                    "fixup, unsupported type {0}, " +
                                    "value = {1}",
                                    FormatOps.TypeName(innerValue),
                                    FormatOps.WrapOrNull(innerValue)),
                                    typeof(ExpressionEvaluator).Name,
                                    TracePriority.ValueDebug);
                            }
#endif
                        }
                        catch (Exception e)
                        {
                            error = e;
                            code = ReturnCode.Error;
                        }
                    }
                }

                //
                // BUGBUG: Check for general syntax error...
                //
                //         This does not work due to our inline handling of special
                //         operators requiring recursion and/or SHORT-CIRCUITING.
                //
                // if (index != (tokenIndex + firstToken.Components + 1))
                // {
                //     ExpressionParser.LogSyntaxError(exprState, null, ref error);
                //     code = ReturnCode.Error;
                // }
            }
            else
            {
                //
                // NOTE: The interpreter is no longer usable (i.e. it may have
                //       been disposed, deleted, etc).  Return an error code.
                //       The result should already contain an appropriate error
                //       message.
                //
                error = Result.Copy(
                    Engine.InterpreterUnusableError, ResultFlags.CopyValue);

                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
    #endregion
}
