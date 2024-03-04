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
    [ObjectId("d37cf7c6-37db-41f9-b0b5-33cd5a9c43d8")]
    public class ExpressionToken : ParseToken, IExpressionToken
    {
        #region Private Constructors
        private ExpressionToken(
            IToken token
            )
            : this(token, Lexeme.Unknown, null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        private ExpressionToken(
            IParseState parseState
            )
            : base(parseState)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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
        public override void Save(
            out IToken token
            )
        {
            Save(base.ParseState, out token);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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
        private Lexeme lexeme;
        public virtual Lexeme Lexeme
        {
            get { return lexeme; }
            set { if (IsImmutable()) throw new InvalidOperationException(); lexeme = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private IVariant variant;
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
    [ObjectId("a6d90ec7-f14c-4038-a8d4-5872c5de6fbb")]
    public class ExpressionState :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IExpressionState
    {
        #region Private Constructors
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
        private IParseState parseState;
        public virtual IParseState ParseState
        {
            get { return parseState; }
            set { if (immutable) throw new InvalidOperationException(); parseState = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private bool notReady;
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

        private Lexeme lexeme;
        public virtual Lexeme Lexeme
        {
            get { return lexeme; }
            set { if (immutable) throw new InvalidOperationException(); lexeme = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private int start;
        public virtual int Start
        {
            get { return start; }
            set { if (immutable) throw new InvalidOperationException(); start = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private int length;
        public virtual int Length
        {
            get { return length; }
            set { if (immutable) throw new InvalidOperationException(); length = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private int next;
        public virtual int Next
        {
            get { return next; }
            set { if (immutable) throw new InvalidOperationException(); next = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private int previousEnd;
        public virtual int PreviousEnd
        {
            get { return previousEnd; }
            set { if (immutable) throw new InvalidOperationException(); previousEnd = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private int original;
        public virtual int Original
        {
            get { return original; }
            set { if (immutable) throw new InvalidOperationException(); original = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private int last;
        public virtual int Last
        {
            get { return last; }
            set { if (immutable) throw new InvalidOperationException(); last = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private bool immutable;
        public virtual bool IsImmutable()
        {
            return immutable;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public virtual void MakeImmutable()
        {
            IParseState parseState = this.ParseState;

            if (parseState != null)
                parseState.MakeImmutable();

            immutable = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public virtual void Save(
            out IExpressionState exprState
            )
        {
            Save(this.ParseState, out exprState);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        public virtual string ToString(
            string text
            )
        {
            return ToList(text).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
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
    [ObjectId("034801c3-eaaf-4f5d-bc57-6d9fc83e94ab")]
    public static class ExpressionParser
    {
        #region Private Constants
        private static int TokenCapacity = 100;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        private static int ParseInteger(
            string text,
            int startIndex,
            int characters
            )
        {
            int index = startIndex;

            if ((characters > 1) &&
                (text[index] == Characters.Zero) &&
                ((text[index + 1] == Characters.x) || (text[index + 1] == Characters.X)))
            {
                long number = 0;

                index += 2; characters -= 2;

                int scanned = Parser.ParseHexadecimal(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

                return 1;
            }
            else if ((characters > 1) &&
                     (text[index] == Characters.Zero) &&
                     ((text[index + 1] == Characters.d) || (text[index + 1] == Characters.D)))
            {
                long number = 0;

                index += 2; characters -= 2;

                int scanned = Parser.ParseDecimal(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

                return 1;
            }
            else if ((characters > 1) &&
                     (text[index] == Characters.Zero) &&
                     ((text[index + 1] == Characters.o) || (text[index + 1] == Characters.O)))
            {
                long number = 0;

                index += 2; characters -= 2;

                int scanned = Parser.ParseOctal(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

                return 1;
            }
            else if ((characters > 1) &&
                (text[index] == Characters.Zero) &&
                     ((text[index + 1] == Characters.b) || (text[index + 1] == Characters.B)))
            {
                long number = 0;

                index += 2; characters -= 2;

                int scanned = Parser.ParseBinary(text, index, characters, ref number);

                if (scanned > 0)
                    return scanned + 2;

                return 1;
            }

            while ((characters > 0) && Parser.IsInteger(text[index], false))
            {
                characters--; index++;
            }

            if (characters == 0)
                return (index - startIndex);
            else if ((text[index] != Characters.Period) &&
                     (text[index] != Characters.e) &&
                     (text[index] != Characters.E))
                return (index - startIndex);
            else
                return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

            while (exprState.Lexeme == Lexeme.Exponent)
            {
                int operatorIndex = exprState.Start;

                code = GetLexeme(interpreter, exprState, noReady, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                code = ParseUnary(interpreter, exprState, noReady, ref error);

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
                case Lexeme.Literal: /* int, long, or double */
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
                            interpreter, parseState.Text, dollarIndex,
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
                            interpreter, parseState.Text, exprState.Start,
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
                                    interpreter, parseState.Text, index,
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
                                (parseState.Text[nestedParseState.Terminator] == Characters.CloseBracket) &&
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
                            interpreter, parseState.Text, exprState.Start,
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
                            string value = parseState.Text.Substring(
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
                    Parser.IsLineTerminator(parseState.Text[index]) &&
                    ((int)LogicOps.Y(index++, characters--) > 0));

            parseState.Terminator = index;

            if (characters == 0)
            {
                exprState.Lexeme = Lexeme.End;
                exprState.Next = index;
                return ReturnCode.Ok;
            }

            if ((parseState.Text[index] != Characters.PlusSign) &&
                (parseState.Text[index] != Characters.MinusSign))
            {
                CultureInfo cultureInfo = (interpreter != null) ?
                    interpreter.InternalCultureInfo : null;

                bool noInteger = false;
                int end = exprState.Last;

            retryNumber:

                if (!noInteger &&
                    ((length = ParseInteger(parseState.Text, index, end - index)) > 0))
                {
                    string value = parseState.Text.Substring(index, length);

                    //
                    // NOTE: See if we can parse and interpret the string
                    //       as some kind of integer value.
                    //
                    ulong ulongValue = 0;
                    Result localError = null;

                    if (Value.GetUnsignedWideInteger2(
                            value, ValueFlags.AnyWideInteger |
                            ValueFlags.Unsigned, cultureInfo, ref ulongValue,
                            ref localError) == ReturnCode.Error)
                    {
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
                else if ((length = ParseMaxDoubleLength(parseState.Text, index, end)) > 0)
                {
                    string value = parseState.Text.Substring(index, length);

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

            switch (parseState.Text[index])
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
                            (parseState.Text[index + 1] == Characters.Asterisk))
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
                            (parseState.Text[index + 1] == Characters.GreaterThanSign))
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
                            (parseState.Text[index + 1] == Characters.EqualSign))
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
                            switch (parseState.Text[index + 1])
                            {
                                case Characters.LessThanSign:
                                    {
                                        exprState.Lexeme = Lexeme.LeftShift;
                                        exprState.Length = 2;
                                        exprState.Next = index + 2;

                                        if ((exprState.Last - index) > 2)
                                        {
                                            switch (parseState.Text[index + 2])
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
                                            switch (parseState.Text[index + 2])
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
                                            switch (parseState.Text[index + 2])
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
                            switch (parseState.Text[index + 1])
                            {
                                case Characters.GreaterThanSign:
                                    {
                                        exprState.Lexeme = Lexeme.RightShift;
                                        exprState.Length = 2;
                                        exprState.Next = index + 2;

                                        if ((exprState.Last - index) > 2)
                                        {
                                            switch (parseState.Text[index + 2])
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
                            (parseState.Text[index + 1] == Characters.EqualSign))
                        {
                            exprState.Lexeme = Lexeme.Equal;
                            exprState.Length = 2;
                            exprState.Next = index + 2;
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (parseState.Text[index + 1] == Characters.GreaterThanSign))
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
                            (parseState.Text[index + 1] == Characters.EqualSign))
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
                            (parseState.Text[index + 1] == Characters.Ampersand))
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
                            (parseState.Text[index + 1] == Characters.Caret))
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
                            (parseState.Text[index + 1] == Characters.Pipe))
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
                            (parseState.Text[index + 1] == Characters.q))
                        {
                            //
                            // BUGFIX: Fix "eq*()" functions being detected as the
                            //         "eq" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
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
                            (parseState.Text[index + 1] == Characters.e))
                        {
                            //
                            // NOTE: Fix "ge*()" functions being detected as the
                            //       "ge" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringGreaterThanOrEqualTo;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (parseState.Text[index + 1] == Characters.t))
                        {
                            //
                            // NOTE: Fix "gt*()" functions being detected as the
                            //       "gt" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
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
                            (parseState.Text[index + 1] == Characters.n))
                        {
                            //
                            // BUGFIX: Fix "in*()" functions being detected as the
                            //         "in" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
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
                            (parseState.Text[index + 1] == Characters.e))
                        {
                            //
                            // NOTE: Fix "le*()" functions being detected as the
                            //       "le" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringLessThanOrEqualTo;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (parseState.Text[index + 1] == Characters.t))
                        {
                            //
                            // NOTE: Fix "lt*()" functions being detected as the
                            //       "lt" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
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
                            (parseState.Text[index + 1] == Characters.e))
                        {
                            //
                            // BUGFIX: Fix "ne*()" functions being detected as the
                            //         "ne" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
                            {
                                exprState.Lexeme = Lexeme.StringNotEqual;
                                exprState.Length = 2;
                                exprState.Next = index + 2;

                                parseState.Terminator = exprState.Next;

                                return ReturnCode.Ok;
                            }
                        }
                        else if (((exprState.Last - index) > 1) &&
                            (parseState.Text[index + 1] == Characters.i))
                        {
                            //
                            // BUGFIX: Fix "ni*()" functions being detected as the
                            //         "ni" operator.
                            //
                            if (((exprState.Last - index) <= 2) ||
                                !Parser.IsIdentifier(parseState.Text[index + 2]))
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
                        char character = parseState.Text[index];

                        if (Char.IsLetter(character))
                        {
                            length = (exprState.Last - index);
                            exprState.Lexeme = Lexeme.IdentifierName;

                            while ((length > 0) &&
                                   Parser.IsIdentifier(parseState.Text[index]))
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
    [ObjectId("2a8a47c7-d933-4de1-ae6a-e46eaf5debfd")]
    internal static class ExpressionEvaluator
    {
        #region Private Methods
        #region Expression Flags Methods
#if EXPRESSION_FLAGS
        private static bool HasBackslashes(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Backslashes) == ExpressionFlags.Backslashes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool HasVariables(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Variables) == ExpressionFlags.Variables);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool HasCommands(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Commands) == ExpressionFlags.Commands);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool HasFunctions(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Functions) == ExpressionFlags.Functions);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool HasOperators(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.Operators) == ExpressionFlags.Operators);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        private static bool HasBooleanToInteger(
            ExpressionFlags flags
            )
        {
            return ((flags & ExpressionFlags.BooleanToInteger) == ExpressionFlags.BooleanToInteger);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

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
                        value = Argument.FromString(parseState.Text.Substring(
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
                            parseState.Text, token.Start, token.Length,
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
                            interpreter, parseState.Text, token.Start + 1,
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
                        string name = parseState.Text.Substring(
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
