/*
 * ScriptBlocks.cs --
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
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class processes a block of text containing embedded Eagle script
    /// blocks, replacing each block with the result of evaluating it (or with
    /// a substituted or variable value), and emitting the literal text between
    /// blocks verbatim.  Script blocks are delimited by the opening tag
    /// <c>&lt;#</c> and the closing tag <c>#&gt;</c>; a block whose first
    /// character is the comment character has its contents substituted rather
    /// than evaluated, and a block whose first character is the equal sign is
    /// replaced with the value of the named variable.  The behavior of the
    /// processor is controlled by a set of <see cref="ScriptBlockFlags" /> and
    /// it tracks running counts of the literals, blocks, evaluations,
    /// substitutions, variable replacements, failures, and errors it
    /// encounters.  It can be used as a reusable instance or via its static
    /// helper methods, and it is disposable.
    /// </summary>
    [ObjectId("6fd6fe04-5798-4164-9360-04d4b45f56f6")]
    public sealed class ScriptBlocks : IDisposable
    {
        #region Private Constants
        /// <summary>
        /// The set of characters considered to be whitespace, used when
        /// trimming the formatted result of a block.
        /// </summary>
        private static readonly char[] WhiteSpaceChars =
            Characters.WhiteSpaceChars;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The opening tag that marks the start of an embedded script block.
        /// </summary>
        private static readonly string OpenBlock = "<#";

        /// <summary>
        /// The closing tag that marks the end of an embedded script block.
        /// </summary>
        private static readonly string CloseBlock = "#>";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The length, in characters, of the opening block tag.
        /// </summary>
        private static readonly int OpenLength = OpenBlock.Length;

        /// <summary>
        /// The length, in characters, of the closing block tag.
        /// </summary>
        private static readonly int CloseLength = CloseBlock.Length;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// When non-zero, only the Ok and Return return codes are treated as
        /// successful when evaluating a block.
        /// </summary>
        private bool okOrReturnOnly;

        /// <summary>
        /// When non-zero, an exception return code is treated as successful
        /// when evaluating a block.
        /// </summary>
        private bool allowExceptions;

        /// <summary>
        /// When non-zero, leading and trailing whitespace is trimmed from the
        /// formatted result of a successfully processed block.
        /// </summary>
        private bool trimSpace;

        /// <summary>
        /// When non-zero, block evaluation errors are emitted into the output
        /// text in addition to being recorded.
        /// </summary>
        private bool emitErrors;

        /// <summary>
        /// When non-zero, processing stops upon the first block evaluation
        /// error.
        /// </summary>
        private bool stopOnError;

        /// <summary>
        /// When non-zero, block parsing failures (such as unmatched tags) are
        /// emitted into the output text in addition to being recorded.
        /// </summary>
        private bool emitFailures;

        /// <summary>
        /// When non-zero, processing stops upon the first block parsing
        /// failure.
        /// </summary>
        private bool stopOnFailure;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a script block processor for the specified interpreter
        /// and text, configured by the specified flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate, substitute, and resolve the
        /// embedded script blocks.
        /// </param>
        /// <param name="text">
        /// The text containing the embedded script blocks to be processed.
        /// </param>
        /// <param name="scriptBlockFlags">
        /// The flags controlling how the embedded script blocks are processed.
        /// </param>
        public ScriptBlocks(
            Interpreter interpreter,          /* in */
            string text,                      /* in */
            ScriptBlockFlags scriptBlockFlags /* in */
            )
        {
            this.interpreter = interpreter;
            this.text = text;
            this.scriptBlockFlags = scriptBlockFlags;

            ///////////////////////////////////////////////////////////////////

            this.okOrReturnOnly = HasFlags(
                ScriptBlockFlags.OkOrReturnOnly, true);

            this.allowExceptions = HasFlags(
                ScriptBlockFlags.AllowExceptions, true);

            this.trimSpace = HasFlags(ScriptBlockFlags.TrimSpace, true);
            this.emitErrors = HasFlags(ScriptBlockFlags.EmitErrors, true);
            this.stopOnError = HasFlags(ScriptBlockFlags.StopOnError, true);
            this.emitFailures = HasFlags(ScriptBlockFlags.EmitFailures, true);

            this.stopOnFailure = HasFlags(
                ScriptBlockFlags.StopOnFailure, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        #region Input Properties (Read-Only)
        /// <summary>
        /// The interpreter used to evaluate, substitute, and resolve the
        /// embedded script blocks.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Gets the interpreter used to evaluate, substitute, and resolve the
        /// embedded script blocks.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The text containing the embedded script blocks to be processed.
        /// </summary>
        private string text;

        /// <summary>
        /// Gets the text containing the embedded script blocks to be processed.
        /// </summary>
        public string Text
        {
            get { CheckDisposed(); return text; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags controlling how the embedded script blocks are processed.
        /// </summary>
        private ScriptBlockFlags scriptBlockFlags;

        /// <summary>
        /// Gets the flags controlling how the embedded script blocks are
        /// processed.
        /// </summary>
        public ScriptBlockFlags ScriptBlockFlags
        {
            get { CheckDisposed(); return scriptBlockFlags; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Output Properties (Read-Only)
        /// <summary>
        /// The running count of literal text spans emitted between blocks.
        /// </summary>
        private int literalCount;

        /// <summary>
        /// Gets the running count of literal text spans emitted between blocks.
        /// </summary>
        public int LiteralCount
        {
            get { CheckDisposed(); return literalCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The running count of embedded script blocks that were found.
        /// </summary>
        private int blockCount;

        /// <summary>
        /// Gets the running count of embedded script blocks that were found.
        /// </summary>
        public int BlockCount
        {
            get { CheckDisposed(); return blockCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The running count of blocks that were evaluated as scripts.
        /// </summary>
        private int evaluateCount;

        /// <summary>
        /// Gets the running count of blocks that were evaluated as scripts.
        /// </summary>
        public int EvaluateCount
        {
            get { CheckDisposed(); return evaluateCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The running count of blocks that had their contents substituted.
        /// </summary>
        private int substituteCount;

        /// <summary>
        /// Gets the running count of blocks that had their contents
        /// substituted.
        /// </summary>
        public int SubstituteCount
        {
            get { CheckDisposed(); return substituteCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The running count of blocks that were replaced with a variable
        /// value.
        /// </summary>
        private int variableCount;

        /// <summary>
        /// Gets the running count of blocks that were replaced with a variable
        /// value.
        /// </summary>
        public int VariableCount
        {
            get { CheckDisposed(); return variableCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The running count of block parsing failures (such as unmatched
        /// tags).
        /// </summary>
        private int failCount;

        /// <summary>
        /// Gets the running count of block parsing failures (such as unmatched
        /// tags).
        /// </summary>
        public int FailCount
        {
            get { CheckDisposed(); return failCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The running count of block evaluation or substitution errors.
        /// </summary>
        private int errorCount;

        /// <summary>
        /// Gets the running count of block evaluation or substitution errors.
        /// </summary>
        public int ErrorCount
        {
            get { CheckDisposed(); return errorCount; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method processes the configured text, appending the literal
        /// spans and the results of the embedded script blocks to the output
        /// and accumulating the per-category counts on this instance.
        /// </summary>
        /// <param name="output">
        /// The string builder receiving the processed text.  Upon success or
        /// failure, this parameter is populated with the literal text and the
        /// results of the embedded script blocks; it is created if null when
        /// any output needs to be appended.
        /// </param>
        /// <param name="errors">
        /// The list receiving any error or failure messages.  Upon failure,
        /// this parameter is populated with the details; it is created if null
        /// when any error needs to be recorded.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok if no failures or errors were encountered; otherwise,
        /// ReturnCode.Error.
        /// </returns>
        public ReturnCode Process(
            ref StringBuilder output, /* in, out */
            ref ResultList errors     /* in, out */
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            CheckDisposed();

            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            if (text == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid block text");
                return ReturnCode.Error;
            }

            int localLiteralCount = 0;
            int localBlockCount = 0;
            int localEvaluateCount = 0;
            int localSubstituteCount = 0;
            int localVariableCount = 0;
            int localFailCount = 0;
            int localErrorCount = 0;
            int length = text.Length;
            int index = 0;

            while (index < length)
            {
                int remaining = length - index;

                int openIndex = text.IndexOf(OpenBlock, index, remaining,
                    SharedStringOps.SystemComparisonType);

                int closeIndex = text.IndexOf(CloseBlock, index, remaining,
                    SharedStringOps.SystemComparisonType);

                if (openIndex != Index.Invalid)
                {
                    int literalLength = openIndex - index;

                    if (literalLength > 0)
                    {
                        if (output == null)
                            output = NewStringBuilder(remaining);

                        output.Append(text, index, literalLength);
                        localLiteralCount++;
                    }

                    int savedOpenIndex = openIndex;

                    openIndex += OpenLength;

                    closeIndex = text.IndexOf(
                        CloseBlock, openIndex, length - openIndex,
                        SharedStringOps.SystemComparisonType);

                    if (closeIndex != Index.Invalid)
                    {
                        //
                        // NOTE: We found another block to process.
                        //
                        localBlockCount++;

                        //
                        // NOTE: Evaluate the block we just found and insert
                        //       the resulting text.  If a script error is
                        //       raised, either stop (in strict mode) -OR-
                        //       record it and continue (in non-strict mode).
                        //
                        int blockLength = closeIndex - openIndex;

                        //
                        // NOTE: If the block is "empty" (i.e. it contains no
                        //       characters whatsoever, not even whitespace,
                        //       then skip extracting the script and evaluate
                        //       an empty string.  We evaluate an empty string
                        //       just in case the script engine needs to
                        //       perform some "background" tasks at this point,
                        //       such as the processing of asynchronous events.
                        //
                        bool isSubstitute;
                        bool isVariable;
                        string blockText;

                        if (blockLength > 0)
                        {
                            //
                            // HACK: Sneak a peek at the first character of the
                            //       block.
                            //
                            char firstCharacter = text[openIndex];

                            //
                            // HACK: If it is the Tcl comment character (i.e.
                            //       '#'), we need to skip that character when
                            //       extracting the block to actually operate
                            //       on.
                            //
                            isSubstitute = IsSubstituteChar(firstCharacter);

                            int blockIndex = openIndex;

                            if (isSubstitute)
                            {
                                isVariable = false;
                                blockIndex += 1; blockLength -= 1;
                            }
                            else
                            {
                                isVariable = IsVariableChar(firstCharacter);

                                if (isVariable)
                                {
                                    blockIndex += 1; blockLength -= 1;
                                }
                            }

                            blockText = (blockLength > 0) ?
                                text.Substring(blockIndex, blockLength) :
                                String.Empty;
                        }
                        else
                        {
                            isSubstitute = false;
                            isVariable = false;
                            blockText = String.Empty;
                        }

                        ReturnCode localCode;
                        Result localResult = null;
                        int localErrorLine = 0;

                        //
                        // HACK: If the *VERY* first character of the block is
                        //       the comment character ("#") or the equal sign
                        //       ("="), then do not evaluate the block; rather,
                        //       just perform any textual substitutions inside
                        //       it -OR- replace it with the variable value.
                        //
                        if (isSubstitute)
                        {
                            //
                            // NOTE: We found another substitution to perform.
                            //
                            localSubstituteCount++;

                            localCode = interpreter.SubstituteString(blockText,
                                ref localResult);
                        }
                        else if (isVariable)
                        {
                            //
                            // NOTE: We found another variable to replace.
                            //
                            localVariableCount++;

                            Result localValue = null;
                            Result localError = null;

                            localCode = interpreter.GetVariableValue(
                                VariableFlags.None, blockText, ref localValue,
                                ref localError);

                            if (localCode == ReturnCode.Ok)
                                localResult = localValue;
                            else
                                localResult = localError;
                        }
                        else
                        {
                            //
                            // NOTE: We found another evaluation to perform.
                            //
                            localEvaluateCount++;

                            localCode = interpreter.EvaluateScript(blockText,
                                ref localResult, ref localErrorLine);
                        }

                        //
                        // NOTE: Was the block was processed successfully?
                        //
                        if (IsSuccess(
                                localCode, okOrReturnOnly, allowExceptions))
                        {
                            if (output == null)
                                output = NewStringBuilder(remaining);

                            if ((localResult != null) &&
                                (localResult.Length > 0))
                            {
                                string formatted = trimSpace ?
                                    (string)localResult.Trim(WhiteSpaceChars) :
                                    (string)localResult;

                                output.Append(formatted);
                            }
                        }
                        else
                        {
                            string formatted = ResultOps.Format(
                                    localCode, localResult, localErrorLine);

                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "block from absolute index {0} to absolute " +
                                "index {1} had {2} error: {3}", savedOpenIndex,
                                closeIndex, isSubstitute ? "substitution" :
                                "evaluation", formatted));

                            if (emitErrors)
                            {
                                if (output == null)
                                    output = NewStringBuilder(remaining);

                                output.AppendFormat(
                                    "{0}{0}BLOCK ERROR: {1}{0}{0}",
                                    Environment.NewLine, formatted);
                            }

                            localErrorCount++;

                            if (stopOnError)
                                break;
                        }

                        //
                        // NOTE: The very next thing to process is just after
                        //       the closing block tag.
                        //
                        index = closeIndex + CloseLength;
                    }
                    else
                    {
                        //
                        // NOTE: The open tag has no matching close tag.
                        //
                        if (errors == null)
                            errors = new ResultList();

                        Result localError = String.Format(
                            "found opening tag \"{0}\" at absolute index " +
                            "{1} and expected closing tag \"{2}\", which " +
                            "was not found", OpenBlock, savedOpenIndex,
                            CloseBlock);

                        errors.Add(localError);

                        if (emitFailures)
                        {
                            if (output == null)
                                output = NewStringBuilder(remaining);

                            output.AppendFormat(
                                "{0}{0}PARSE ERROR: {1}{0}{0}",
                                Environment.NewLine, localError);
                        }

                        localFailCount++;

                        if (stopOnFailure)
                            break;

                        //
                        // NOTE: The very next thing to process is just
                        //       after the opening block tag.
                        //
                        index = openIndex + OpenLength;
                    }
                }
                else if (closeIndex != Index.Invalid)
                {
                    //
                    // NOTE: The close tag has no matching open tag.
                    //
                    if (errors == null)
                        errors = new ResultList();

                    Result localError = String.Format(
                        "found closing tag \"{0}\" at absolute index " +
                        "{1} and expected opening tag \"{2}\", which " +
                        "was not found", CloseBlock, closeIndex,
                        OpenBlock);

                    errors.Add(localError);

                    if (emitFailures)
                    {
                        if (output == null)
                            output = NewStringBuilder(remaining);

                        output.AppendFormat(
                            "{0}{0}PARSE ERROR: {1}{0}{0}",
                            Environment.NewLine, localError);
                    }

                    localFailCount++;

                    if (stopOnFailure)
                        break;

                    //
                    // NOTE: The very next thing to process is just
                    //       after the closing block tag.
                    //
                    index = closeIndex + CloseLength;
                }
                else
                {
                    if (output == null)
                        output = NewStringBuilder(remaining);

                    output.Append(text, index, remaining);
                    localLiteralCount++;

                    index += remaining;
                }
            }

            literalCount += localLiteralCount;
            blockCount += localBlockCount;
            evaluateCount += localEvaluateCount;
            substituteCount += localSubstituteCount;
            variableCount += localVariableCount;
            failCount += localFailCount;
            errorCount += localErrorCount;

            return (localFailCount == 0) && (localErrorCount == 0) ?
                ReturnCode.Ok : ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method creates a temporary script block processor for the
        /// specified interpreter and text, processes the text, and discards
        /// the resulting per-category counts.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate, substitute, and resolve the
        /// embedded script blocks.
        /// </param>
        /// <param name="text">
        /// The text containing the embedded script blocks to be processed.
        /// </param>
        /// <param name="scriptBlockFlags">
        /// The flags controlling how the embedded script blocks are processed.
        /// </param>
        /// <param name="output">
        /// The string builder receiving the processed text.  Upon success or
        /// failure, this parameter is populated with the literal text and the
        /// results of the embedded script blocks; it is created if null when
        /// any output needs to be appended.
        /// </param>
        /// <param name="errors">
        /// The list receiving any error or failure messages.  Upon failure,
        /// this parameter is populated with the details; it is created if null
        /// when any error needs to be recorded.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok if no failures or errors were encountered; otherwise,
        /// ReturnCode.Error.
        /// </returns>
        public static ReturnCode Process(
            Interpreter interpreter,           /* in */
            string text,                       /* in */
            ScriptBlockFlags scriptBlockFlags, /* in */
            ref StringBuilder output,          /* in, out */
            ref ResultList errors              /* in, out */
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            int literalCount = 0;
            int blockCount = 0;
            int evaluateCount = 0;
            int substituteCount = 0;
            int variableCount = 0;
            int failCount = 0;
            int errorCount = 0;

            return Process(
                interpreter, text, scriptBlockFlags, ref literalCount,
                ref blockCount, ref evaluateCount, ref substituteCount,
                ref variableCount, ref failCount, ref errorCount,
                ref output, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a temporary script block processor for the
        /// specified interpreter and text, processes the text, and adds the
        /// resulting per-category counts to the supplied totals.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate, substitute, and resolve the
        /// embedded script blocks.
        /// </param>
        /// <param name="text">
        /// The text containing the embedded script blocks to be processed.
        /// </param>
        /// <param name="scriptBlockFlags">
        /// The flags controlling how the embedded script blocks are processed.
        /// </param>
        /// <param name="literalCount">
        /// On input, a running total; on output, increased by the count of
        /// literal text spans emitted between blocks.
        /// </param>
        /// <param name="blockCount">
        /// On input, a running total; on output, increased by the count of
        /// embedded script blocks that were found.
        /// </param>
        /// <param name="evaluateCount">
        /// On input, a running total; on output, increased by the count of
        /// blocks that were evaluated as scripts.
        /// </param>
        /// <param name="substituteCount">
        /// On input, a running total; on output, increased by the count of
        /// blocks that had their contents substituted.
        /// </param>
        /// <param name="variableCount">
        /// On input, a running total; on output, increased by the count of
        /// blocks that were replaced with a variable value.
        /// </param>
        /// <param name="failCount">
        /// On input, a running total; on output, increased by the count of
        /// block parsing failures (such as unmatched tags).
        /// </param>
        /// <param name="errorCount">
        /// On input, a running total; on output, increased by the count of
        /// block evaluation or substitution errors.
        /// </param>
        /// <param name="output">
        /// The string builder receiving the processed text.  Upon success or
        /// failure, this parameter is populated with the literal text and the
        /// results of the embedded script blocks; it is created if null when
        /// any output needs to be appended.
        /// </param>
        /// <param name="errors">
        /// The list receiving any error or failure messages.  Upon failure,
        /// this parameter is populated with the details; it is created if null
        /// when any error needs to be recorded.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok if no failures or errors were encountered; otherwise,
        /// ReturnCode.Error.
        /// </returns>
        public static ReturnCode Process(
            Interpreter interpreter,           /* in */
            string text,                       /* in */
            ScriptBlockFlags scriptBlockFlags, /* in */
            ref int literalCount,              /* in, out */
            ref int blockCount,                /* in, out */
            ref int evaluateCount,             /* in, out */
            ref int substituteCount,           /* in, out */
            ref int variableCount,             /* in, out */
            ref int failCount,                 /* in, out */
            ref int errorCount,                /* in, out */
            ref StringBuilder output,          /* in, out */
            ref ResultList errors              /* in, out */
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            using (ScriptBlocks scriptBlocks = new ScriptBlocks(
                    interpreter, text, scriptBlockFlags))
            {
                ReturnCode returnCode = scriptBlocks.Process(
                    ref output, ref errors);

                literalCount += scriptBlocks.LiteralCount;
                blockCount += scriptBlocks.BlockCount;
                evaluateCount += scriptBlocks.EvaluateCount;
                substituteCount += scriptBlocks.SubstituteCount;
                variableCount += scriptBlocks.VariableCount;
                failCount += scriptBlocks.FailCount;
                errorCount += scriptBlocks.ErrorCount;

                return returnCode;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method creates a new, non-cached string builder with the
        /// specified initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The suggested initial capacity, in characters, of the new string
        /// builder.
        /// </param>
        /// <returns>
        /// The newly created string builder.
        /// </returns>
        private static StringBuilder NewStringBuilder(
            int capacity /* in */
            )
        {
            return StringBuilderFactory.CreateNoCache(capacity); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character indicates
        /// that a block should have its contents substituted (the comment
        /// character) rather than evaluated.
        /// </summary>
        /// <param name="character">
        /// The first character of the block contents to test.
        /// </param>
        /// <returns>
        /// True if the character is the substitution (comment) character;
        /// otherwise, false.
        /// </returns>
        private static bool IsSubstituteChar(
            char character /* in */
            )
        {
            return (character == Characters.Comment);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character indicates
        /// that a block should be replaced with the value of a named variable
        /// (the equal sign) rather than evaluated.
        /// </summary>
        /// <param name="character">
        /// The first character of the block contents to test.
        /// </param>
        /// <returns>
        /// True if the character is the variable (equal sign) character;
        /// otherwise, false.
        /// </returns>
        private static bool IsVariableChar(
            char character /* in */
            )
        {
            return (character == Characters.EqualSign);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified return code represents
        /// a successful block evaluation, subject to the specified success
        /// criteria.
        /// </summary>
        /// <param name="code">
        /// The return code produced by evaluating, substituting, or resolving
        /// a block.
        /// </param>
        /// <param name="okOrReturnOnly">
        /// Non-zero to treat only the Ok and Return return codes as
        /// successful.
        /// </param>
        /// <param name="allowExceptions">
        /// Non-zero to treat an exception return code as successful; only used
        /// when <paramref name="okOrReturnOnly" /> is zero.
        /// </param>
        /// <returns>
        /// True if the return code represents success per the specified
        /// criteria; otherwise, false.
        /// </returns>
        private static bool IsSuccess(
            ReturnCode code,     /* in */
            bool okOrReturnOnly, /* in */
            bool allowExceptions /* in */
            )
        {
            if (okOrReturnOnly)
                return ResultOps.IsOkOrReturn(code);

            return ResultOps.IsSuccess(code, allowExceptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method determines whether the configured script block flags
        /// include the specified flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to test for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be present; zero
        /// to require that any of them be present.
        /// </param>
        /// <returns>
        /// True if the configured flags include the specified flags per the
        /// <paramref name="all" /> criterion; otherwise, false.
        /// </returns>
        private bool HasFlags(
            ScriptBlockFlags hasFlags, /* in */
            bool all                   /* in */
            )
        {
            return FlagOps.HasFlags(scriptBlockFlags, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this object has been disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
                throw new ObjectDisposedException(typeof(ScriptBlocks).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the managed and unmanaged resources used by
        /// this object.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (text != null)
                        text = null;

                    if (interpreter != null)
                        interpreter = null; /* NOT OWNED */
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this object, releasing any unmanaged resources it holds.
        /// </summary>
        ~ScriptBlocks()
        {
            Dispose(false);
        }
        #endregion
    }
}
