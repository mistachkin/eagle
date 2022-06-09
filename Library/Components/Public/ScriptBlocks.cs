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
    [ObjectId("6fd6fe04-5798-4164-9360-04d4b45f56f6")]
    public sealed class ScriptBlocks : IDisposable
    {
        #region Private Constants
        private static readonly char[] WhiteSpaceChars =
            Characters.WhiteSpaceChars;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string OpenBlock = "<#";
        private static readonly string CloseBlock = "#>";

        ///////////////////////////////////////////////////////////////////////

        private static readonly int OpenLength = OpenBlock.Length;
        private static readonly int CloseLength = CloseBlock.Length;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private bool okOrReturnOnly;
        private bool allowExceptions;
        private bool trimSpace;
        private bool emitErrors;
        private bool stopOnError;
        private bool emitFailures;
        private bool stopOnFailure;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { CheckDisposed(); return text; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptBlockFlags scriptBlockFlags;
        public ScriptBlockFlags ScriptBlockFlags
        {
            get { CheckDisposed(); return scriptBlockFlags; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Output Properties (Read-Only)
        private int literalCount;
        public int LiteralCount
        {
            get { CheckDisposed(); return literalCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int blockCount;
        public int BlockCount
        {
            get { CheckDisposed(); return blockCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int evaluateCount;
        public int EvaluateCount
        {
            get { CheckDisposed(); return evaluateCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int substituteCount;
        public int SubstituteCount
        {
            get { CheckDisposed(); return substituteCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int variableCount;
        public int VariableCount
        {
            get { CheckDisposed(); return variableCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int failCount;
        public int FailCount
        {
            get { CheckDisposed(); return failCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int errorCount;
        public int ErrorCount
        {
            get { CheckDisposed(); return errorCount; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
        private static StringBuilder NewStringBuilder(
            int capacity /* in */
            )
        {
            return StringOps.NewStringBuilder(capacity);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsSubstituteChar(
            char character /* in */
            )
        {
            return (character == Characters.Comment);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsVariableChar(
            char character /* in */
            )
        {
            return (character == Characters.EqualSign);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
                throw new ObjectDisposedException(typeof(ScriptBlocks).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        ~ScriptBlocks()
        {
            Dispose(false);
        }
        #endregion
    }
}
