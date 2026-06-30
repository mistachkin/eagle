/*
 * Result.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Diagnostics;

#if NET_40
using System.Numerics;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents the result of an Eagle operation -- the value
    /// produced by a successful command, function, expression, or script, or
    /// the error message produced by a failing one.  It is the companion to
    /// <see cref="ReturnCode" />: most APIs return a <see cref="ReturnCode" />
    /// and communicate their value or error through a <c>ref Result</c>
    /// parameter.  For failures, a result also carries optional error
    /// metadata (error line, error code, and error information), and it
    /// supports implicit conversion to and from the common value types so it
    /// can be assigned naturally.  See <c>error_handling.md</c> for usage
    /// patterns.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("9b092a26-fb6f-4487-ad6e-560ce24f249b")]
    public sealed class Result :
            IResult, IToString, IString, ICloneable
    {
        #region Public Constants
        /// <summary>
        /// The sentinel used to represent the absence of a string value (a
        /// null string).
        /// </summary>
        public static readonly string NoValue = null;

        /// <summary>
        /// The sentinel used to represent the absence of a cached full-string
        /// representation (a null string).
        /// </summary>
        public static readonly string NoFullString = null;

        /// <summary>
        /// The sentinel used to represent the absence of client data (a null
        /// reference).
        /// </summary>
        public static readonly IClientData NoClientData = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A shared, pre-built result instance whose value is null.
        /// </summary>
        public static readonly Result Null = new Result((object)null);

        /// <summary>
        /// A shared, pre-built result instance whose value is the empty
        /// string.
        /// </summary>
        public static readonly Result Empty = FromString(String.Empty);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, a null value is treated as the empty string when
        /// producing the string form of a result.
        /// </summary>
        private static bool UseEmptyForNull = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, a stack trace is captured each time any return code
        /// is recorded on a result.
        /// </summary>
        private static bool AnyReturnCodeStackTrace = false;

        /// <summary>
        /// When non-zero, a stack trace is captured each time an error line is
        /// recorded on a result.
        /// </summary>
        private static bool AnyErrorLineStackTrace = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        // WARNING: Setting these to true could be very expensive.
        //
        /// <summary>
        /// When non-null, this overrides whether a stack trace is captured
        /// when a result is created.
        /// </summary>
        private static bool? PopulateStackTrace = null;

        /// <summary>
        /// When non-null, this overrides whether a captured stack trace is
        /// included in the string form of a result.
        /// </summary>
        private static bool? IncludeStackTrace = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty result in a well-known, reset state.
        /// </summary>
        [DebuggerStepThrough()]
        private Result()
        {
            Reset(); /* NOTE: Well-known state. */
            MaybePopulateStackTrace();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a successful result wrapping the specified value.  This
        /// constructor provides internal support for conversions from other
        /// data types.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Result(
            object value
            )
            : this(ReturnCode.Ok, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a successful result wrapping the value obtained from the
        /// specified value container.
        /// </summary>
        /// <param name="value">
        /// The value container whose value is wrapped.  This parameter may be
        /// null, in which case a null value is wrapped.
        /// </param>
        [DebuggerStepThrough()]
        private Result(
            IGetValue value
            )
            : this(ReturnCode.Ok, (value != null) ? value.Value : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a successful result wrapping the specified interpreter.
        /// This constructor provides internal support for conversions from the
        /// interpreter data type.
        /// </summary>
        /// <param name="value">
        /// The interpreter to wrap.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Result(
            Interpreter value
            )
            : this(ReturnCode.Ok, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a successful result wrapping the value of the specified
        /// argument.  This constructor provides internal support for
        /// conversions from the argument data type.
        /// </summary>
        /// <param name="value">
        /// The argument whose value is wrapped.  This parameter may be null,
        /// in which case a null value is wrapped.
        /// </param>
        [DebuggerStepThrough()]
        private Result(
            Argument value
            )
            : this(ReturnCode.Ok, (value != null) ? value.Value : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a result by copying the specified result, honoring the
        /// given flags.  This constructor provides internal support for the
        /// <c>Copy</c> static factory method.
        /// </summary>
        /// <param name="result">
        /// The result to copy from.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which portions of the source result are
        /// copied.
        /// </param>
        [DebuggerStepThrough()]
        private Result(
            Result result,    /* in */
            ResultFlags flags /* in */
            )
            : this()
        {
            CopyFrom(result, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a result with the specified return code and value.  This
        /// constructor is primarily intended for success (<see cref="ReturnCode.Ok" />)
        /// results.
        /// </summary>
        /// <param name="returnCode">
        /// The return code to record on the result.
        /// </param>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Result(
            ReturnCode returnCode, /* in */
            object value           /* in */
            )
            : this()
        {
            SetValueOnly(returnCode, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a result with the specified return code, value, and
        /// error metadata.  This constructor is primarily intended for failure
        /// (<see cref="ReturnCode.Error" />) results.
        /// </summary>
        /// <param name="returnCode">
        /// The return code to record on the result.
        /// </param>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with the error, or zero if none.
        /// </param>
        /// <param name="errorCode">
        /// The error code associated with the error, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="errorInfo">
        /// The error information (stack trace) associated with the error, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="exception">
        /// The exception associated with the error, if any.  This parameter
        /// may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Result( /* NOT USED */
            ReturnCode returnCode, /* in */
            object value,          /* in */
            int errorLine,         /* in */
            string errorCode,      /* in */
            string errorInfo,      /* in */
            Exception exception    /* in */
            )
            : this(returnCode, value)
        {
            SetErrorOnly(errorLine, errorCode, errorInfo, exception);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        /// <summary>
        /// This method sets the specified flags on this result, leaving any
        /// other flags unchanged.
        /// </summary>
        /// <param name="flags">
        /// The flags to set on this result.
        /// </param>
        [DebuggerStepThrough()]
        private void SetFlags(
            ResultFlags flags
            )
        {
            this.flags |= flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the specified flags on this result, leaving any
        /// other flags unchanged.
        /// </summary>
        /// <param name="flags">
        /// The flags to clear on this result.
        /// </param>
        [DebuggerStepThrough()]
        private void UnsetFlags(
            ResultFlags flags
            )
        {
            this.flags &= ~flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the mask of flags that should be excluded when
        /// copying from another result, based on the supplied copy flags and
        /// the current state of this result.
        /// </summary>
        /// <param name="flags">
        /// The copy flags that control which portions of a source result are
        /// being copied.
        /// </param>
        /// <returns>
        /// The mask of flags to exclude during the copy operation.
        /// </returns>
        [DebuggerStepThrough()]
        private ResultFlags MaskCopyFromFlags(
            ResultFlags flags
            )
        {
            ResultFlags mask;

            if (FlagOps.HasFlags(flags, ResultFlags.Error, true))
                mask = ResultFlags.InternalMask;
            else
                mask = ResultFlags.AllMask;

            if (stackTrace != null)
                mask &= ~ResultFlags.StackTrace;

            return mask;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the wrapped value of this result to null,
        /// optionally zeroing any sensitive string data beforehand and
        /// invalidating the cached string representation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine whether string data
        /// should be zeroed.  This parameter may be null.
        /// </param>
        /// <param name="zero">
        /// Non-zero to zero out any sensitive string data before it is
        /// released.
        /// </param>
        [DebuggerStepThrough()]
        internal void ResetValue(
            Interpreter interpreter,
            bool zero
            )
        {
#if !MONO && NATIVE && WINDOWS
            if (zero && (value is string) && (interpreter != null) &&
                interpreter.HasZeroString())
            {
                /* IGNORED */
                StringOps.ZeroStringOrTrace((string)value);
            }
#endif

            value = null;

#if CACHE_RESULT_TOSTRING
#if !MONO && NATIVE && WINDOWS
            if (zero && (@string != null) && (interpreter != null) &&
                interpreter.HasZeroString())
            {
                /* IGNORED */
                StringOps.ZeroStringOrTrace(@string);
            }
#endif

            InvalidateCachedString(false);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the value, cached string, full string, and
        /// client data of this result to their well-known, empty (null)
        /// state.
        /// </summary>
        [DebuggerStepThrough()]
        private void Reset()
        {
            ///////////////////////////////////////////////////////////////////
            //
            // NOTE: For this object, we always null out the fields (i.e.
            //       the NoValue and NoClientData constants are defined
            //       to be null) because:
            //
            //       1. Typical usage of this method would be to recycle
            //          this object for use in an object pool, which
            //          really requires totally cleaned out (null) field
            //          values.
            //
            //       2. The existing semantics of this object do not offer
            //          any kind of guarantee that uninitialized instances
            //          will convert to an empty string (i.e. unlike the
            //          Argument object).
            //
            ///////////////////////////////////////////////////////////////////

            value = NoValue;

#if CACHE_RESULT_TOSTRING
            InvalidateCachedString(false);
#endif

            UnsetFlags(ResultFlags.String);

            ///////////////////////////////////////////////////////////////////

            fullString = NoFullString;
            UnsetFlags(ResultFlags.FullString);

            ///////////////////////////////////////////////////////////////////

            clientData = NoClientData;
            UnsetFlags(ResultFlags.ClientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally clears individual portions of this result
        /// (e.g. client data, value data, extra data, call frame, engine data,
        /// stack trace, and full string), based on the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags indicating which portions of this result should be
        /// cleared.
        /// </param>
        [DebuggerStepThrough()]
        private void MaybeClear(
            ResultFlags flags
            )
        {
            if (FlagOps.HasFlags(flags, ResultFlags.ClientData, true))
            {
                clientData = null;
                UnsetFlags(ResultFlags.ClientData);
            }

            if (FlagOps.HasFlags(flags, ResultFlags.ValueData, true))
            {
                valueData = null;
                UnsetFlags(ResultFlags.ValueData);
            }

            if (FlagOps.HasFlags(flags, ResultFlags.ExtraData, true))
            {
                extraData = null;
                UnsetFlags(ResultFlags.ExtraData);
            }

            if (FlagOps.HasFlags(flags, ResultFlags.CallFrame, true))
            {
                callFrame = null;
                UnsetFlags(ResultFlags.CallFrame);
            }

            if (FlagOps.HasFlags(flags, ResultFlags.EngineData, true))
            {
                engineData = null;
                UnsetFlags(ResultFlags.EngineData);
            }

            if (FlagOps.HasFlags(flags, ResultFlags.StackTrace, true))
            {
                stackTrace = null;
                UnsetFlags(ResultFlags.StackTrace);
            }

            if (FlagOps.HasFlags(flags, ResultFlags.FullString, true))
            {
                fullString = null;
                UnsetFlags(ResultFlags.FullString);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the return code and wrapped value of this result,
        /// updating the associated flags and cached string representation as
        /// appropriate for the type of the value.
        /// </summary>
        /// <param name="returnCode">
        /// The return code to record on this result.
        /// </param>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private void SetValueOnly(
            ReturnCode returnCode,
            object value
            )
        {
            this.returnCode = returnCode;
            this.previousReturnCode = returnCode;
            this.value = value;

            if (this.value is string)
            {
#if CACHE_RESULT_TOSTRING
                //
                // NOTE: We now have a cached string representation.
                //
                this.@string = (string)this.value;
#endif

                //
                // NOTE: We now have a string result.
                //
                SetFlags(ResultFlags.String);

                //
                // NOTE: If necessary, include the stack trace(s).
                //
                MaybeIncludeFullString(this);
            }
            else if (this.value is Exception)
            {
                //
                // NOTE: Save the value as the exception property as
                //       well.
                //
                this.exception = (Exception)this.value;

                //
                // NOTE: We now have an exception result.
                //
                SetFlags(ResultFlags.Exception);

                //
                // NOTE: If necessary, reset the stack trace(s).
                //
                ResetFullString();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the error metadata of this result and marks it as
        /// containing error information.
        /// </summary>
        /// <param name="errorLine">
        /// The script line number associated with the error, or zero if none.
        /// </param>
        /// <param name="errorCode">
        /// The error code associated with the error, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="errorInfo">
        /// The error information (stack trace) associated with the error, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="exception">
        /// The exception associated with the error, if any.  This parameter
        /// may be null.
        /// </param>
        [DebuggerStepThrough()]
        private void SetErrorOnly(
            int errorLine,
            string errorCode,
            string errorInfo,
            Exception exception
            )
        {
            this.errorLine = errorLine;
            this.errorCode = errorCode;
            this.errorInfo = errorInfo;
            this.exception = exception;

            //
            // NOTE: We now have error info.
            //
            SetFlags(ResultFlags.Error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the selected portions of the specified source
        /// result into this result, honoring the supplied copy flags.
        /// </summary>
        /// <param name="result">
        /// The result to copy from.  This parameter may be null, in which case
        /// nothing is copied.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which portions of the source result are
        /// copied.
        /// </param>
        [DebuggerStepThrough()]
        private void CopyFrom(
            Result result,
            ResultFlags flags
            )
        {
            if (result != null)
            {
                if (FlagOps.HasFlags(flags, ResultFlags.String, true))
                {
                    //
                    // NOTE: Either this is a string (and we know how to make
                    //       a deep copy of a string) -OR- we do not know how
                    //       to make a deep copy of it; therefore, just refer
                    //       to it.
                    //
                    /* System.String: Immutable, Deep Copy */
                    /* <other>: Shallow Copy */
                    this.value = result.value;

#if CACHE_RESULT_TOSTRING
                    /* Immutable, Deep Copy */
                    this.@string = result.@string;
#endif
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.Error, true))
                {
                    /* ValueType, Deep Copy */
                    this.returnCode = result.returnCode;

                    /* ValueType, Deep Copy */
                    this.previousReturnCode = result.previousReturnCode;

                    /* ValueType, Deep Copy */
                    this.errorLine = result.errorLine;

                    /* Immutable, Deep Copy */
                    this.errorCode = result.errorCode;

                    /* Immutable, Deep Copy */
                    this.errorInfo = result.errorInfo;

                    /* Immutable (?), Shallow Copy */
                    this.exception = result.exception;
                }

                ///////////////////////////////////////////////////////////////

                /* ValueType, Deep Copy */
                this.flags = (result.flags & ~MaskCopyFromFlags(flags));

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.ClientData, true))
                {
                    this.clientData = result.clientData;
                    SetFlags(ResultFlags.ClientData);
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.ValueData, true))
                {
                    this.valueData = result.valueData;
                    SetFlags(ResultFlags.ValueData);
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.ExtraData, true))
                {
                    this.extraData = result.extraData;
                    SetFlags(ResultFlags.ExtraData);
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.CallFrame, true))
                {
                    this.callFrame = result.callFrame;
                    SetFlags(ResultFlags.CallFrame);
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.EngineData, true))
                {
                    this.engineData = result.engineData;
                    SetFlags(ResultFlags.EngineData);
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.StackTrace, true))
                {
                    this.stackTrace = result.stackTrace;
                    SetFlags(ResultFlags.StackTrace);
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(flags, ResultFlags.FullString, true))
                {
                    this.fullString = result.fullString;
                    SetFlags(ResultFlags.FullString);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method gets the wrapped value of the specified result.
        /// </summary>
        /// <param name="result">
        /// The result whose value is returned.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped value of the specified result, or null if the result is
        /// null.
        /// </returns>
        [DebuggerStepThrough()]
        public static object GetValue(
            Result result
            )
        {
            if (result == null)
                return null;

            return result.Value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the wrapped value of the specified result,
        /// optionally creating a new result when the supplied result is null.
        /// </summary>
        /// <param name="result">
        /// The result whose value is set.  When null and
        /// <paramref name="create" /> is non-zero, a new result is created and
        /// stored here.
        /// </param>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <param name="create">
        /// Non-zero to create a new result when the supplied result is null.
        /// </param>
        /// <returns>
        /// Non-zero if the value was set; otherwise, zero (e.g. when the
        /// result is null and creation was not requested).
        /// </returns>
        [DebuggerStepThrough()]
        public static bool SetValue(
            ref Result result,
            object value,
            bool create
            )
        {
            if (result == null)
            {
                if (!create)
                    return false;

                result = new Result(); /* EXEMPT */
            }

            result.Value = value;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a multi-line, human-readable string describing
        /// the specified result, including its contained value, cached string,
        /// return code, error metadata, managed stack trace, and any chained
        /// exceptions.
        /// </summary>
        /// <param name="result">
        /// The result to describe.  This parameter may be null.
        /// </param>
        /// <param name="anyReturnCode">
        /// Non-zero to include the return code even when it is
        /// <see cref="ReturnCode.Ok" />.
        /// </param>
        /// <param name="anyErrorLine">
        /// Non-zero to include the error line even when it is zero.
        /// </param>
        /// <returns>
        /// A string describing the specified result, or null if the result is
        /// null.
        /// </returns>
        [DebuggerStepThrough()]
        public static string WithStackTraces(
            Result result,
            bool anyReturnCode,
            bool anyErrorLine
            )
        {
            if (result == null)
                return null;

            StringBuilder builder = StringBuilderFactory.Create();
            object value = result.value;

            if (value != null) /* NOTE: Maybe "System.String"? */
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine("CONTAINED VALUE:");
                builder.AppendLine();
                builder.Append(FormatOps.DisplayString(value.ToString()));
            }

#if CACHE_RESULT_TOSTRING
            string @string = result.@string;

            if (@string != null)
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine("CACHED STRING:");
                builder.AppendLine();
                builder.Append(FormatOps.DisplayString(@string));
            }
#endif

            ReturnCode returnCode = result.returnCode;

            if (anyReturnCode || (returnCode != ReturnCode.Ok))
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine("RETURN CODE:");
                builder.AppendLine();
                builder.Append(returnCode);
            }

            int errorLine = result.errorLine;

            if (anyErrorLine || (errorLine != 0))
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine("ERROR LINE:");
                builder.AppendLine();
                builder.Append(errorLine);
            }

            string errorCode = result.errorCode;

            if (errorCode != null)
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine("SCRIPT ERROR CODE:");
                builder.AppendLine();
                builder.Append(FormatOps.DisplayString(errorCode));
            }

            string errorInfo = result.errorInfo;

            if (errorInfo != null)
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine("SCRIPT STACK TRACE:");
                builder.AppendLine();
                builder.Append(FormatOps.DisplayString(errorInfo));
            }

            string stackTrace = result.stackTrace;

            if (stackTrace != null)
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine("MANAGED STACK TRACE:");
                builder.AppendLine();

                //
                // HACK: Normalize all directory separators to
                //       be forward slashes.
                //
                builder.Append(
                    FormatOps.DisplayString(PathOps.GetUnixPath(
                    stackTrace)));
            }

            Exception exception = result.exception;
            int exceptionCount = 0;

            while (exception != null)
            {
                MaybeAddLinesTo(builder, true);

                builder.AppendLine(String.Format(
                    "MANAGED EXCEPTION (level {0}):", exceptionCount));

                builder.AppendLine();

                //
                // HACK: Normalize all directory separators to
                //       be forward slashes.
                //
                builder.Append(
                    FormatOps.DisplayString(PathOps.GetUnixPath(
                    exception.ToString())));

                exception = exception.InnerException;
                exceptionCount++;
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Stack Trace Helpers
        /// <summary>
        /// This method conditionally populates the cached full-string form of
        /// the specified result (including its stack traces) when stack trace
        /// inclusion is enabled; otherwise, it clears that cached form.
        /// </summary>
        /// <param name="result">
        /// The result whose full-string form is updated.  This parameter may
        /// be null.
        /// </param>
        [DebuggerStepThrough()]
        private static void MaybeIncludeFullString(
            Result result
            )
        {
            if (result != null)
            {
                if (ShouldIncludeStackTrace(result))
                {
                    result.fullString = WithStackTraces(result);
                    result.SetFlags(ResultFlags.FullString);
                }
                else
                {
                    result.fullString = null;
                    result.UnsetFlags(ResultFlags.FullString);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a stack trace should be captured
        /// when a result is created, caching the answer after the first call.
        /// </summary>
        /// <returns>
        /// Non-zero if a stack trace should be captured when a result is
        /// created.
        /// </returns>
        [DebuggerStepThrough()]
        private static bool ShouldPopulateStackTrace()
        {
            if (PopulateStackTrace == null)
            {
                bool stackTrace;

                if (CommonOps.Environment.DoesVariableExist(
                        EnvVars.PopulateResultStack) ||
                    CommonOps.Environment.DoesVariableExist(
                        String.Format(
                            "{0}_{1}", EnvVars.PopulateResultStack,
                            GlobalState.GetCurrentThreadId())))
                {
                    stackTrace = true;
                }
                else
                {
                    stackTrace = false;
                }

                PopulateStackTrace = stackTrace;
            }

            return (bool)PopulateStackTrace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a captured stack trace should be
        /// included in the string form of a result, caching the answer after
        /// the first call.
        /// </summary>
        /// <returns>
        /// Non-zero if a captured stack trace should be included in the string
        /// form of a result.
        /// </returns>
        [DebuggerStepThrough()]
        private static bool ShouldIncludeStackTrace()
        {
            if (IncludeStackTrace == null)
            {
                bool stackTrace;

                if (CommonOps.Environment.DoesVariableExist(
                        EnvVars.IncludeResultStack) ||
                    CommonOps.Environment.DoesVariableExist(
                        String.Format(
                            "{0}_{1}", EnvVars.IncludeResultStack,
                            GlobalState.GetCurrentThreadId())))
                {
                    stackTrace = true;
                }
                else
                {
                    stackTrace = false;
                }

                IncludeStackTrace = stackTrace;
            }

            return (bool)IncludeStackTrace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a captured stack trace should be
        /// included in the string form of the specified result, based on its
        /// return code and whether it has a captured stack trace.
        /// </summary>
        /// <param name="result">
        /// The result to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if a captured stack trace should be included in the string
        /// form of the specified result.
        /// </returns>
        [DebuggerStepThrough()]
        private static bool ShouldIncludeStackTrace(
            Result result
            )
        {
            if (result == null)
                return false;

            if (!AnyReturnCodeStackTrace &&
                (result.returnCode != ReturnCode.Error))
            {
                return false;
            }

            if (result.stackTrace == null)
                return false;

            return ShouldIncludeStackTrace();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends one or more line terminators to the specified
        /// string builder to visually separate sections, when the builder
        /// already contains content.
        /// </summary>
        /// <param name="builder">
        /// The string builder to append to.  This parameter may be null.
        /// </param>
        /// <param name="maybeExtraLine">
        /// Non-zero to append an extra line terminator when the builder does
        /// not already end with one.
        /// </param>
        [DebuggerStepThrough()]
        private static void MaybeAddLinesTo(
            StringBuilder builder,
            bool maybeExtraLine
            )
        {
            if (builder != null)
            {
                int length = builder.Length;

                if (length > 0)
                {
                    if (maybeExtraLine &&
                        !Parser.IsLineTerminator(builder[length - 1]))
                    {
                        builder.AppendLine();
                    }

                    builder.AppendLine();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the cached decision about whether a stack trace
        /// should be captured when a result is created, so that it will be
        /// recomputed on next use.
        /// </summary>
        [DebuggerStepThrough()]
        internal static void ResetPopulateStackTrace()
        {
            PopulateStackTrace = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the cached decision about whether a captured
        /// stack trace should be included in the string form of a result, so
        /// that it will be recomputed on next use.
        /// </summary>
        [DebuggerStepThrough()]
        internal static void ResetIncludeStackTrace()
        {
            IncludeStackTrace = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally overrides, and then returns, whether a stack
        /// trace should be captured when a result is created.
        /// </summary>
        /// <param name="enable">
        /// When non-null, the new value controlling whether a stack trace is
        /// captured when a result is created.  When null, the current value is
        /// left unchanged.
        /// </param>
        /// <returns>
        /// The effective value controlling whether a stack trace is captured
        /// when a result is created, which may itself be null when no override
        /// is in effect.
        /// </returns>
        [DebuggerStepThrough()]
        internal static bool? EnablePopulateStackTrace(
            bool? enable
            )
        {
            if (enable != null)
                PopulateStackTrace = enable;

            return PopulateStackTrace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally overrides, and then returns, whether a
        /// captured stack trace should be included in the string form of a
        /// result.
        /// </summary>
        /// <param name="enable">
        /// When non-null, the new value controlling whether a captured stack
        /// trace is included in the string form of a result.  When null, the
        /// current value is left unchanged.
        /// </param>
        /// <returns>
        /// The effective value controlling whether a captured stack trace is
        /// included in the string form of a result, which may itself be null
        /// when no override is in effect.
        /// </returns>
        [DebuggerStepThrough()]
        internal static bool? EnableIncludeStackTrace(
            bool? enable
            )
        {
            if (enable != null)
                IncludeStackTrace = enable;

            return IncludeStackTrace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a multi-line, human-readable string describing
        /// the specified result, using the configured defaults for whether the
        /// return code and error line are always included.
        /// </summary>
        /// <param name="result">
        /// The result to describe.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A string describing the specified result, or null if the result is
        /// null.
        /// </returns>
        [DebuggerStepThrough()]
        internal static string WithStackTraces(
            Result result
            )
        {
            return WithStackTraces(result,
                AnyReturnCodeStackTrace, AnyErrorLineStackTrace);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stack Trace Helpers
        /// <summary>
        /// This method captures the current managed stack trace into this
        /// result and marks it as having a stack trace.
        /// </summary>
        [DebuggerStepThrough()]
        private void PrivatePopulateStackTrace()
        {
            stackTrace = DebugOps.GetStackTraceString();
            SetFlags(ResultFlags.StackTrace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears any captured managed stack trace from this
        /// result and marks it as no longer having a stack trace.
        /// </summary>
        [DebuggerStepThrough()]
        private void PrivateResetStackTrace()
        {
            stackTrace = null;
            UnsetFlags(ResultFlags.StackTrace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method captures the current managed stack trace into this
        /// result when stack trace population is enabled; otherwise, it clears
        /// any captured stack trace.
        /// </summary>
        [DebuggerStepThrough()]
        private void MaybePopulateStackTrace()
        {
            if (ShouldPopulateStackTrace())
                PrivatePopulateStackTrace();
            else
                PrivateResetStackTrace();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the cached full-string form of this result and
        /// marks it as no longer having one.
        /// </summary>
        [DebuggerStepThrough()]
        private void ResetFullString()
        {
            fullString = null;
            UnsetFlags(ResultFlags.FullString);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        /// <summary>
        /// This method combines two results into a single result containing a
        /// list of the non-null results.
        /// </summary>
        /// <param name="result1">
        /// The first result to combine.  This parameter may be null, in which
        /// case it is omitted.
        /// </param>
        /// <param name="result2">
        /// The second result to combine.  This parameter may be null, in which
        /// case it is omitted.
        /// </param>
        /// <returns>
        /// A result wrapping the list of non-null results, or null if both
        /// supplied results are null.
        /// </returns>
        [DebuggerStepThrough()]
        public static Result Combine(
            Result result1,
            Result result2
            )
        {
            ResultList results = null;

            if (result1 != null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add(result1);
            }

            if (result2 != null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add(result2);
            }

            return results;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of the specified result, honoring the
        /// given copy flags.
        /// </summary>
        /// <param name="result">
        /// The result to copy.  This parameter may be null, in which case null
        /// is returned.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which portions of the result are copied.
        /// </param>
        /// <returns>
        /// A copy of the specified result, or null if it is null.
        /// </returns>
        [DebuggerStepThrough()]
        public static Result Copy(
            Result result,
            ResultFlags flags
            )
        {
            return Copy(result, null, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of the specified result, honoring the
        /// given copy flags and optionally overriding its return code.
        /// </summary>
        /// <param name="result">
        /// The result to copy.  This parameter may be null, in which case null
        /// is returned.
        /// </param>
        /// <param name="newReturnCode">
        /// When non-null, the return code to record on the copied result.
        /// When null, the return code of the source result is preserved.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which portions of the result are copied.
        /// </param>
        /// <returns>
        /// A copy of the specified result, or null if it is null.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result Copy(
            Result result,
            ReturnCode? newReturnCode,
            ResultFlags flags
            )
        {
            Result localResult = null;

            if (result != null) /* garbage in, garbage out */
            {
                localResult = (Result)result.Copy(flags);

                if (newReturnCode != null)
                    localResult.returnCode = (ReturnCode)newReturnCode;
            }

            return localResult;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of the specified result, honoring the
        /// given copy flags and optionally overriding its wrapped value.
        /// </summary>
        /// <param name="result">
        /// The result to copy.  This parameter may be null, in which case null
        /// is returned.
        /// </param>
        /// <param name="newValue">
        /// The new value to wrap in the copied result, subject to type
        /// compatibility rules.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which portions of the result are copied.
        /// </param>
        /// <returns>
        /// A copy of the specified result, or null if it is null.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result Copy(
            Result result,
            object newValue,
            ResultFlags flags
            )
        {
            return Copy(result, null, newValue, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of the specified result, honoring the
        /// given copy flags and optionally overriding both its return code and
        /// its wrapped value.
        /// </summary>
        /// <param name="result">
        /// The result to copy.  This parameter may be null, in which case null
        /// is returned.
        /// </param>
        /// <param name="newReturnCode">
        /// When non-null, the return code to record on the copied result.
        /// When null, the return code of the source result is preserved.
        /// </param>
        /// <param name="newValue">
        /// The new value to wrap in the copied result, subject to type
        /// compatibility rules.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which portions of the result are copied.
        /// </param>
        /// <returns>
        /// A copy of the specified result, or null if it is null.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result Copy(
            Result result,
            ReturnCode? newReturnCode,
            object newValue,
            ResultFlags flags
            )
        {
            Result localResult = null;

            if (result != null) /* garbage in, garbage out */
            {
                localResult = (Result)result.Copy(flags);

                if (newReturnCode != null)
                    localResult.returnCode = (ReturnCode)newReturnCode;

                if (FlagOps.HasFlags(flags, ResultFlags.IgnoreType, true) ||
                    MarshalOps.IsSameObjectType(result.value, newValue))
                {
                    localResult.value = newValue;
                }
            }

            return localResult;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Equals Helpers
        /// <summary>
        /// This method determines whether two results are equal by comparing
        /// their wrapped values, flags, return codes, and error metadata.
        /// </summary>
        /// <param name="left">
        /// The first result to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second result to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the two results are considered equal.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static bool Equals(
            Result left,
            Result right
            )
        {
            if (Object.ReferenceEquals(left, right))
                return true;

            if ((left == null) || (right == null))
                return false;

            if (!ValueEquals(left.value, right.value))
                return false;

            if (!FlagsEquals(left.flags, right.flags))
                return false;

            if (left.returnCode != right.returnCode)
                return false;

            if (left.previousReturnCode != right.previousReturnCode)
                return false;

            if (left.errorLine != right.errorLine)
                return false;

            if (!SharedStringOps.SystemEquals(left.errorCode, right.errorCode))
                return false;

            if (!SharedStringOps.SystemEquals(left.errorInfo, right.errorInfo))
                return false;

            if (!Object.ReferenceEquals(left.exception, right.exception))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two wrapped values are equal, using
        /// ordinal string comparison when both are strings.
        /// </summary>
        /// <param name="left">
        /// The first value to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second value to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the two values are considered equal.
        /// </returns>
        [DebuggerStepThrough()]
        private static bool ValueEquals(
            object left,
            object right
            )
        {
            //
            // BUGBUG: This method should probably just use Object.Equals
            //         and nothing else.
            //
            if ((left is string) && (right is string))
            {
                return SharedStringOps.SystemEquals(
                    (string)left, (string)right);
            }
            else
            {
                return Object.Equals(left, right);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two sets of result flags are equal,
        /// ignoring any internal-only flags.
        /// </summary>
        /// <param name="left">
        /// The first set of flags to compare.
        /// </param>
        /// <param name="right">
        /// The second set of flags to compare.
        /// </param>
        /// <returns>
        /// Non-zero if the two sets of flags are considered equal.
        /// </returns>
        [DebuggerStepThrough()]
        private static bool FlagsEquals(
            ResultFlags left,
            ResultFlags right
            )
        {
            left &= ~ResultFlags.InternalMask;
            right &= ~ResultFlags.InternalMask;

            return (left == right);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static String Helpers
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method compares the string forms of two results, using the
        /// specified comparison type.
        /// </summary>
        /// <param name="result1">
        /// The first result to compare.  This parameter may be null.
        /// </param>
        /// <param name="result2">
        /// The second result to compare.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number indicating whether
        /// <paramref name="result1" /> sorts before, at the same position as,
        /// or after <paramref name="result2" />.
        /// </returns>
        [DebuggerStepThrough()]
        private static int Compare(
            Result result1,
            Result result2,
            StringComparison comparisonType
            )
        {
            return SharedStringOps.Compare(
                ToString(result1, null), ToString(result2, null),
                comparisonType);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        /// <summary>
        /// This method computes the length of the string form of the specified
        /// value, caching the computed string on the result when possible.
        /// </summary>
        /// <param name="result">
        /// The result associated with the value, used for caching its string
        /// form.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value whose string length is computed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The length to return when the value is null.
        /// </param>
        /// <returns>
        /// The length of the string form of the value, or
        /// <paramref name="default" /> when the value is null.
        /// </returns>
        [DebuggerStepThrough()]
        private static int GetLength(
            Result result,
            object value,
            int @default
            )
        {
            if (value is string)
            {
                return ((string)value).Length;
            }
            else if (value != null)
            {
#if CACHE_RESULT_TOSTRING
                if (result != null)
                {
                    string @string = result.@string;

                    if (@string != null)
                        return @string.Length;

                    @string = value.ToString();
                    result.@string = @string;

                    MaybeIncludeFullString(result);

                    if (@string != null)
                        return @string.Length;
                    else
                        return @default;
                }
                else
#endif
                {
                    MaybeIncludeFullString(value as Result);

                    return value.ToString().Length;
                }
            }
            else
            {
                return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the string form of the specified value,
        /// caching the computed string on the result when possible.
        /// </summary>
        /// <param name="result">
        /// The result associated with the value, used for caching its string
        /// form.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value whose string form is computed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The string to return when the value is null.
        /// </param>
        /// <returns>
        /// The string form of the value, or <paramref name="default" /> when
        /// the value is null.
        /// </returns>
        [DebuggerStepThrough()]
        private static string ToString(
            Result result,
            object value,
            string @default
            )
        {
            if (value is string)
            {
                return (string)value;
            }
            else if (value != null)
            {
#if CACHE_RESULT_TOSTRING
                if (result != null)
                {
                    string @string = result.@string;

                    if (@string != null)
                        return @string;

                    @string = value.ToString();
                    result.@string = @string;

                    MaybeIncludeFullString(result);

                    return @string;
                }
                else
#endif
                {
                    MaybeIncludeFullString(value as Result);

                    return value.ToString();
                }
            }
            else
            {
                return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is directly
        /// supported as a wrapped result value.
        /// </summary>
        /// <param name="type">
        /// The type to check for support.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the specified type is supported as a wrapped result
        /// value.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static bool IsSupported(
            Type type
            )
        {
            if (type == null)
                return false;

            if (type == typeof(bool))
            {
                return true;
            }
            else if (type == typeof(byte))
            {
                return true;
            }
            else if (type == typeof(byte[]))
            {
                return true;
            }
            else if (type == typeof(char))
            {
                return true;
            }
            else if (type == typeof(int))
            {
                return true;
            }
            else if (type == typeof(long))
            {
                return true;
            }
#if NET_40
            else if (type == typeof(BigInteger))
            {
                return true;
            }
#endif
            else if (type == typeof(double))
            {
                return true;
            }
            else if (type == typeof(decimal))
            {
                return true;
            }
            else if (type == typeof(string))
            {
                return true;
            }
            else if (type == typeof(DateTime))
            {
                return true;
            }
            else if (type == typeof(TimeSpan))
            {
                return true;
            }
            else if (type == typeof(Guid))
            {
                return true;
            }
            else if (type == typeof(Uri))
            {
                return true;
            }
            else if (type == typeof(Version))
            {
                return true;
            }
            else if (type == typeof(StringBuilder))
            {
                return true;
            }
            else if (type == typeof(CommandBuilder))
            {
                return true;
            }
            else if (type == typeof(Interpreter))
            {
                return true;
            }
            else if (type == typeof(Argument))
            {
                return true;
            }
            else if (type == typeof(ByteList))
            {
                return true;
            }
            else if (type == typeof(ResultList))
            {
                return true;
            }
            else if (type == typeof(ObjectDictionary))
            {
                return true;
            }
            else if (type.IsEnum)
            {
                return true;
            }
            else if (RuntimeOps.DoesClassTypeSupportInterface(
                    type, typeof(IStringList)))
            {
                return true;
            }
            else if (RuntimeOps.IsClassTypeEqualOrSubClass(
                    type, typeof(Exception), true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a result from the specified object, optionally
        /// forcing a copy, restricting wrapping to supported types, and falling
        /// back to a string representation.
        /// </summary>
        /// <param name="value">
        /// The object to wrap or convert.  This parameter may be null, in which
        /// case null is returned.
        /// </param>
        /// <param name="forceCopy">
        /// Non-zero to force a copy when the supplied object is already a
        /// result.
        /// </param>
        /// <param name="supportedOnly">
        /// Non-zero to wrap the object directly only when its type is
        /// supported.
        /// </param>
        /// <param name="toString">
        /// Non-zero to fall back to a string representation of the object when
        /// it cannot be wrapped directly.
        /// </param>
        /// <returns>
        /// A result wrapping or representing the specified object, or null if
        /// the object is null or cannot be represented.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromObject(
            object value,
            bool forceCopy,
            bool supportedOnly,
            bool toString
            )
        {
            if (value == null)
                return null;

            Result result = value as Result;

            if (result != null)
            {
                //
                // NOTE: Otherwise, use the existing reference.
                //
                if (forceCopy)
                {
                    result = new Result(
                        result, ResultFlags.CopyObject); /* COPY */
                }
            }
            else if (!supportedOnly ||
                IsSupported(AppDomainOps.MaybeGetType(value)))
            {
                result = new Result(value); /* WRAP */
            }
            else if (toString)
            {
                result = StringOps.GetResultFromObject(value); /* String */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method creates a new result that wraps the specified opaque
        /// object value.
        /// </summary>
        /// <param name="value">
        /// The opaque object value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created result wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromIObject(
            IObject value
            )
        {
            return new Result(value);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>Interpreter</c>.
        /// </summary>
        /// <param name="value">
        /// The interpreter to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified interpreter.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromInterpreter(
            Interpreter value
            )
        {
            return new Result(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the value of
        /// the specified <c>Argument</c>.
        /// </summary>
        /// <param name="value">
        /// The argument whose value is wrapped.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified argument value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromArgument(
            Argument value
            )
        {
            return new Result(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>double</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromDouble(
            double value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>decimal</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromDecimal(
            decimal value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>Enum</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromEnum(
            Enum value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>Exception</c>.
        /// </summary>
        /// <param name="value">
        /// The exception to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified exception.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromException(
            Exception value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>Version</c>.
        /// </summary>
        /// <param name="value">
        /// The version to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified version.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromVersion(
            Version value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>ResultList</c>.
        /// </summary>
        /// <param name="value">
        /// The result list to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified result list.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromResultList(
            ResultList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>ObjectDictionary</c>.
        /// </summary>
        /// <param name="value">
        /// The dictionary to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromObjectDictionary(
            ObjectDictionary value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>StringBuilder</c>.
        /// </summary>
        /// <param name="value">
        /// The string builder to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified string builder.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromStringBuilder(
            StringBuilder value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>BigInteger</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromBigInteger(
            BigInteger value
            )
        {
            return new Result((object)value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// wide integer (<c>long</c>) value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromWideInteger(
            long value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>int</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromInteger(
            int value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>bool</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromBoolean(
            bool value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>char</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromCharacter(
            char value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> whose value is the
        /// concatenation of the specified characters.
        /// </summary>
        /// <param name="value1">
        /// The first character.  This parameter may be null, in which case it
        /// contributes nothing.
        /// </param>
        /// <param name="value2">
        /// The second character.  This parameter may be null, in which case it
        /// contributes nothing.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> containing the concatenated characters.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromCharacters(
            char? value1,
            char? value2
            )
        {
            return new Result((object)String.Format("{0}{1}",
                (value1 != null) ? value1.ToString() : null,
                (value2 != null) ? value2.ToString() : null));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>DateTime</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromDateTime(
            DateTime value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>TimeSpan</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromTimeSpan(
            TimeSpan value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>Guid</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromGuid(
            Guid value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>Uri</c>.
        /// </summary>
        /// <param name="value">
        /// The URI to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified URI.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromUri(
            Uri value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>string</c>.
        /// </summary>
        /// <param name="value">
        /// The string to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified string.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromString(
            string value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>byte</c> value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromByte(
            byte value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>byte[]</c> array.
        /// </summary>
        /// <param name="value">
        /// The byte array to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified byte array.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromByteArray(
            byte[] value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// <c>ByteList</c>.
        /// </summary>
        /// <param name="value">
        /// The byte list to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified byte list.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromByteList(
            ByteList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// string list.
        /// </summary>
        /// <param name="value">
        /// The string list to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified list.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromList(
            IStringList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> wrapping the specified
        /// dictionary.
        /// </summary>
        /// <param name="value">
        /// The dictionary to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromDictionary(
            IDictionary value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="Result" /> from the result produced
        /// by the specified command builder.
        /// </summary>
        /// <param name="value">
        /// The command builder whose result is wrapped.  This parameter may be
        /// null, in which case null is returned.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> wrapping the command builder result, or
        /// null if the command builder is null.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromCommandBuilder(
            CommandBuilder value
            )
        {
            if (value == null)
                return null;

            return new Result(value.GetResult());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the string form of the specified result.
        /// </summary>
        /// <param name="result">
        /// The result whose string form is computed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The string to return when the result is null.
        /// </param>
        /// <returns>
        /// The string form of the result, or <paramref name="default" /> when
        /// the result is null.
        /// </returns>
        [DebuggerStepThrough()]
        private static string ToString(
            Result result,
            string @default
            )
        {
            if (result == null)
                return @default;

            return ToString(result, result.Value, @default);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        /// <summary>
        /// This operator implicitly converts the specified
        /// <see cref="Result" /> into a <c>string</c>.
        /// </summary>
        /// <param name="result">
        /// The result to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The string form of the specified result.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator string(
            Result result
            )
        {
            return ToString(result, UseEmptyForNull ? String.Empty : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Interpreter</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The interpreter to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified interpreter,
        /// or null if it is null.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            Interpreter value
            )
        {
            if (value != null)
                return FromInterpreter(value);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Argument</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The argument to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified argument, or
        /// null if it is null.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            Argument value
            )
        {
            if (value != null)
                return FromArgument(value);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>StringList</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The string list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified
        /// <c>StringPairList</c> into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The string pair list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringPairList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified
        /// <c>StringDictionary</c> into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The dictionary to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified
        /// <c>ClientDataDictionary</c> into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The dictionary to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            ClientDataDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>DateTime</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            DateTime value
            )
        {
            return FromDateTime(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>TimeSpan</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            TimeSpan value
            )
        {
            return FromTimeSpan(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Guid</c> into a
        /// <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            Guid value
            )
        {
            return FromGuid(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Uri</c> into a
        /// <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The URI to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified URI.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            Uri value
            )
        {
            return FromUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>string</c> into
        /// a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The string to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified string.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            string value
            )
        {
            return FromString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>byte</c> into a
        /// <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            byte value
            )
        {
            return FromByte(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>byte[]</c> array
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The byte array to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified byte array.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            byte[] value
            )
        {
            return FromByteArray(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>ByteList</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The byte list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified byte list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            ByteList value
            )
        {
            return FromByteList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>char</c> into a
        /// <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            char value
            )
        {
            return FromCharacter(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>double</c> into
        /// a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            double value
            )
        {
            return FromDouble(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>decimal</c> into
        /// a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            decimal value
            )
        {
            return FromDecimal(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Enum</c> value
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            Enum value
            )
        {
            return FromEnum(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Exception</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The exception to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified exception.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            Exception value
            )
        {
            return FromException(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Version</c> into
        /// a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The version to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified version.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            Version value
            )
        {
            return FromVersion(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>ResultList</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The result list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified result list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            ResultList value
            )
        {
            return FromResultList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified
        /// <c>ObjectDictionary</c> into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The dictionary to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            ObjectDictionary value
            )
        {
            return FromObjectDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>StringBuilder</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The string builder to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified string
        /// builder.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringBuilder value
            )
        {
            return FromStringBuilder(value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This operator implicitly converts the specified <c>BigInteger</c>
        /// into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            BigInteger value
            )
        {
            return FromBigInteger(value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified wide integer
        /// (<c>long</c>) into a <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            long value
            )
        {
            return FromWideInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>int</c> into a
        /// <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            int value
            )
        {
            return FromInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>bool</c> into a
        /// <see cref="Result" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Result" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Result(
            bool value
            )
        {
            return FromBoolean(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IString Members
        /// <summary>
        /// This method returns the index of the first occurrence of the
        /// specified substring within the string form of this result, using
        /// the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The substring to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of
        /// <paramref name="value" />, or -1 if it is not found.
        /// </returns>
        [DebuggerStepThrough()]
        public int IndexOf(
            string value,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).IndexOf(value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the index of the first occurrence of the
        /// specified substring within the string form of this result, starting
        /// at the specified index and using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The substring to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to start the search.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of
        /// <paramref name="value" />, or -1 if it is not found.
        /// </returns>
        [DebuggerStepThrough()]
        public int IndexOf(
            string value,
            int startIndex,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).IndexOf(
                value, startIndex, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the index of the last occurrence of the
        /// specified substring within the string form of this result, using
        /// the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The substring to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of
        /// <paramref name="value" />, or -1 if it is not found.
        /// </returns>
        [DebuggerStepThrough()]
        public int LastIndexOf(
            string value,
            StringComparison comparisonType
            )
        {
            return StringOps.LastIndexOf(
                ToString(this, String.Empty), value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the index of the last occurrence of the
        /// specified substring within the string form of this result, starting
        /// at the specified index and using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The substring to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to start the backward search.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of
        /// <paramref name="value" />, or -1 if it is not found.
        /// </returns>
        [DebuggerStepThrough()]
        public int LastIndexOf(
            string value,
            int startIndex,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).LastIndexOf(
                value, startIndex, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the string form of this result
        /// starts with the specified substring, using the specified comparison
        /// type.
        /// </summary>
        /// <param name="value">
        /// The substring to look for at the start.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// Non-zero if the string form of this result starts with
        /// <paramref name="value" />.
        /// </returns>
        [DebuggerStepThrough()]
        public bool StartsWith(
            string value,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).StartsWith(
                value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the string form of this result ends
        /// with the specified substring, using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The substring to look for at the end.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// Non-zero if the string form of this result ends with
        /// <paramref name="value" />.
        /// </returns>
        [DebuggerStepThrough()]
        public bool EndsWith(
            string value,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).EndsWith(
                value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the substring of the string form of this result
        /// that begins at the specified index.
        /// </summary>
        /// <param name="startIndex">
        /// The zero-based starting index of the substring.
        /// </param>
        /// <returns>
        /// The substring beginning at <paramref name="startIndex" />.
        /// </returns>
        [DebuggerStepThrough()]
        public string Substring(
            int startIndex
            )
        {
            return ToString(this, String.Empty).Substring(startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the substring of the string form of this result
        /// that begins at the specified index and has the specified length.
        /// </summary>
        /// <param name="startIndex">
        /// The zero-based starting index of the substring.
        /// </param>
        /// <param name="length">
        /// The number of characters in the substring.
        /// </param>
        /// <returns>
        /// The substring beginning at <paramref name="startIndex" /> with the
        /// specified length.
        /// </returns>
        [DebuggerStepThrough()]
        public string Substring(
            int startIndex,
            int length
            )
        {
            return ToString(this, String.Empty).Substring(startIndex, length);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares the string form of this result with the
        /// specified string, using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to compare against.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number indicating whether
        /// this result sorts before, at the same position as, or after
        /// <paramref name="value" />.
        /// </returns>
        [DebuggerStepThrough()]
        public int Compare(
            string value,
            StringComparison comparisonType
            )
        {
            return SharedStringOps.Compare(
                ToString(this, null), value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares the string form of this result with the string
        /// form of the specified result, using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The result to compare against.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number indicating whether
        /// this result sorts before, at the same position as, or after
        /// <paramref name="value" />.
        /// </returns>
        [DebuggerStepThrough()]
        public int Compare(
            Result value,
            StringComparison comparisonType
            )
        {
            return SharedStringOps.Compare(
                ToString(this, null), ToString(value, null),
                comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the string form of this result
        /// contains the specified substring, using the specified comparison
        /// type.
        /// </summary>
        /// <param name="value">
        /// The substring to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing characters.
        /// </param>
        /// <returns>
        /// Non-zero if the string form of this result contains
        /// <paramref name="value" />.
        /// </returns>
        [DebuggerStepThrough()]
        public bool Contains(
            string value,
            StringComparison comparisonType
            )
        {
            return (ToString(this, String.Empty).IndexOf(
                value, comparisonType) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a copy of the string form of this result in
        /// which all occurrences of the specified old value are replaced with
        /// the specified new value.
        /// </summary>
        /// <param name="oldValue">
        /// The substring to be replaced.
        /// </param>
        /// <param name="newValue">
        /// The substring to replace all occurrences of
        /// <paramref name="oldValue" />.
        /// </param>
        /// <returns>
        /// The string form of this result with the replacements applied.
        /// </returns>
        [DebuggerStepThrough()]
        public string Replace(
            string oldValue,
            string newValue
            )
        {
            return ToString(this, String.Empty).Replace(oldValue, newValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this result with all leading
        /// and trailing white-space characters removed.
        /// </summary>
        /// <returns>
        /// The trimmed string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string Trim()
        {
            return ToString(this, String.Empty).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this result with all leading
        /// and trailing occurrences of the specified characters removed.
        /// </summary>
        /// <param name="trimChars">
        /// The characters to remove, or null to remove white-space characters.
        /// </param>
        /// <returns>
        /// The trimmed string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string Trim(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).Trim(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this result with all leading
        /// occurrences of the specified characters removed.
        /// </summary>
        /// <param name="trimChars">
        /// The characters to remove, or null to remove white-space characters.
        /// </param>
        /// <returns>
        /// The trimmed string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string TrimStart(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).TrimStart(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this result with all
        /// trailing occurrences of the specified characters removed.
        /// </summary>
        /// <param name="trimChars">
        /// The characters to remove, or null to remove white-space characters.
        /// </param>
        /// <returns>
        /// The trimmed string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string TrimEnd(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).TrimEnd(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the characters of the string form of this
        /// result as an array.
        /// </summary>
        /// <returns>
        /// An array containing the characters of the string form of this
        /// result.
        /// </returns>
        [DebuggerStepThrough()]
        public char[] ToCharArray()
        {
            return ToString(this, String.Empty).ToCharArray();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        /// <summary>
        /// This method returns the string form of this result, honoring the
        /// specified formatting flags.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling how the result is formatted.
        /// </param>
        /// <returns>
        /// The formatted string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this result, honoring the
        /// specified formatting flags.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling how the result is formatted.
        /// </param>
        /// <param name="default">
        /// The default string to use; this parameter is currently unused.
        /// </param>
        /// <returns>
        /// The formatted string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string ToString(
            ToStringFlags flags,
            string @default /* NOT USED */
            )
        {
            return ToString("{0}");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this result formatted using
        /// the specified composite format string.
        /// </summary>
        /// <param name="format">
        /// The composite format string, in which <c>{0}</c> is replaced with
        /// the string form of this result.
        /// </param>
        /// <returns>
        /// The formatted string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string ToString(
            string format
            )
        {
            return String.Format(format, ToString(this, null));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this result formatted using
        /// the specified composite format string, truncated with an ellipsis
        /// to the specified length.
        /// </summary>
        /// <param name="format">
        /// The composite format string, in which <c>{0}</c> is replaced with
        /// the string form of this result.
        /// </param>
        /// <param name="limit">
        /// The maximum length of the returned string before truncation.
        /// </param>
        /// <param name="strict">
        /// Non-zero to strictly enforce the length limit.
        /// </param>
        /// <returns>
        /// The formatted, possibly truncated, string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public string ToString(string format, int limit, bool strict)
        {
            return FormatOps.Ellipsis(
                String.Format(format, ToString(this, null)), limit, strict);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string form of this result, or an empty
        /// string when it has no value.
        /// </summary>
        /// <returns>
        /// The string form of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public override string ToString()
        {
            return ToString(this, String.Empty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this result, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this result.
        /// </summary>
        public IClientData ClientData
        {
            [DebuggerStepThrough()]
            get { return clientData; }
            [DebuggerStepThrough()]
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IValueData Members
        /// <summary>
        /// The value data associated with this result, if any.
        /// </summary>
        private IClientData valueData;
        /// <summary>
        /// Gets or sets the value data associated with this result.
        /// </summary>
        public IClientData ValueData
        {
            [DebuggerStepThrough()]
            get { return valueData; }
            [DebuggerStepThrough()]
            set { valueData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The extra data associated with this result, if any.
        /// </summary>
        private IClientData extraData;
        /// <summary>
        /// Gets or sets the extra data associated with this result.
        /// </summary>
        public IClientData ExtraData
        {
            [DebuggerStepThrough()]
            get { return extraData; }
            [DebuggerStepThrough()]
            set { extraData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The call frame associated with this result, if any.
        /// </summary>
        private ICallFrame callFrame;
        /// <summary>
        /// Gets or sets the call frame associated with this result.
        /// </summary>
        public ICallFrame CallFrame
        {
            [DebuggerStepThrough()]
            get { return callFrame; }
            [DebuggerStepThrough()]
            set { callFrame = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        /// <summary>
        /// The value wrapped by this result, if any.
        /// </summary>
        private object value;
        /// <summary>
        /// Gets or sets the value wrapped by this result.  Setting this
        /// property updates the associated flags and cached string
        /// representation as appropriate for the type of the value.
        /// </summary>
        public object Value
        {
            [DebuggerStepThrough()]
            get { return value; }
            [DebuggerStepThrough()]
            set
            {
                this.value = value;

                if (this.value is string)
                {
#if CACHE_RESULT_TOSTRING
                    //
                    // NOTE: We now have a cached string representation.
                    //
                    this.@string = (string)this.value;
#endif

                    //
                    // NOTE: We now have a string result.
                    //
                    SetFlags(ResultFlags.String);

                    //
                    // NOTE: If necessary, include the stack trace(s).
                    //
                    MaybeIncludeFullString(this);
                }
                else
                {
#if CACHE_RESULT_TOSTRING
                    //
                    // NOTE: We no longer have a cached string representation.
                    //
                    InvalidateCachedString(false);
#endif

                    //
                    // NOTE: We no longer have a string result.
                    //
                    UnsetFlags(ResultFlags.String);

                    //
                    // NOTE: If necessary, reset the stack trace(s).
                    //
                    ResetFullString();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the string form of the value wrapped by this result.
        /// </summary>
        public string String
        {
            [DebuggerStepThrough()]
            get { return ToString(this, value, null); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length of the string form of the value wrapped by this
        /// result.
        /// </summary>
        public int Length
        {
            [DebuggerStepThrough()]
            get { return GetLength(this, value, 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Private
#if CACHE_RESULT_TOSTRING
        /// <summary>
        /// This method invalidates the cached string representation of this
        /// result.
        /// </summary>
        /// <param name="children">
        /// Non-zero to also invalidate cached strings of any child results;
        /// this parameter is currently unused.
        /// </param>
        [DebuggerStepThrough()]
        internal void InvalidateCachedString(
            bool children /* NOT USED */
            )
        {
            @string = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached string representation of this result, if any.
        /// </summary>
        private string @string; /* CACHE */
        /// <summary>
        /// Gets the cached string representation of this result.
        /// </summary>
        internal string CachedString
        {
            [DebuggerStepThrough()]
            get { return @string; }
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResult Members
        /// <summary>
        /// The flags describing the state and contents of this result.
        /// </summary>
        private ResultFlags flags;
        /// <summary>
        /// Gets or sets the flags describing the state and contents of this
        /// result.
        /// </summary>
        public ResultFlags Flags
        {
            [DebuggerStepThrough()]
            get { return flags; }
            [DebuggerStepThrough()]
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the selected portions of this result, based on
        /// the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags indicating which portions of this result should be reset.
        /// </param>
        [DebuggerStepThrough()]
        public void Reset(
            ResultFlags flags
            )
        {
            if (FlagOps.HasFlags(flags, ResultFlags.String, true))
                Reset();

            if (FlagOps.HasFlags(flags, ResultFlags.Error, true))
                Clear();

            MaybeClear(flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of this result, honoring the specified
        /// copy flags.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling which portions of this result are copied.
        /// </param>
        /// <returns>
        /// A new <see cref="IResult" /> that is a copy of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public IResult Copy(
            ResultFlags flags
            )
        {
            return new Result(this, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this result has the specified flags
        /// set.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to test for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all the specified flags are set; otherwise,
        /// any one of them being set is sufficient.
        /// </param>
        /// <returns>
        /// Non-zero if this result has the specified flags set.
        /// </returns>
        [DebuggerStepThrough()]
        public bool HasFlags(
            ResultFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IError Members
        /// <summary>
        /// The return code recorded on this result.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// Gets or sets the return code recorded on this result.  Setting this
        /// property to a different value preserves the prior value as the
        /// previous return code.
        /// </summary>
        public ReturnCode ReturnCode
        {
            [DebuggerStepThrough()]
            get { return returnCode; }
            [DebuggerStepThrough()]
            set
            {
                //
                // NOTE: Is the return code actually changing?
                //
                if (returnCode != value)
                {
                    //
                    // NOTE: Save the previous return code.
                    //
                    previousReturnCode = returnCode;

                    //
                    // NOTE: Set the new return code.
                    //
                    returnCode = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The previous return code recorded on this result.
        /// </summary>
        private ReturnCode previousReturnCode;
        /// <summary>
        /// Gets or sets the previous return code recorded on this result.
        /// </summary>
        public ReturnCode PreviousReturnCode
        {
            [DebuggerStepThrough()]
            get { return previousReturnCode; }
            [DebuggerStepThrough()]
            set { previousReturnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script line number associated with the error, or zero if none.
        /// </summary>
        private int errorLine;
        /// <summary>
        /// Gets or sets the script line number associated with the error.
        /// </summary>
        public int ErrorLine
        {
            [DebuggerStepThrough()]
            get { return errorLine; }
            [DebuggerStepThrough()]
            set { errorLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The error code associated with the error, if any.
        /// </summary>
        private string errorCode;
        /// <summary>
        /// Gets or sets the error code associated with the error.
        /// </summary>
        public string ErrorCode
        {
            [DebuggerStepThrough()]
            get { return errorCode; }
            [DebuggerStepThrough()]
            set { errorCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The error information (stack trace) associated with the error, if
        /// any.
        /// </summary>
        private string errorInfo;
        /// <summary>
        /// Gets or sets the error information (stack trace) associated with the
        /// error.
        /// </summary>
        public string ErrorInfo
        {
            [DebuggerStepThrough()]
            get { return errorInfo; }
            [DebuggerStepThrough()]
            set { errorInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The exception associated with the error, if any.
        /// </summary>
        private Exception exception;
        /// <summary>
        /// Gets or sets the exception associated with the error.
        /// </summary>
        public Exception Exception
        {
            [DebuggerStepThrough()]
            get { return exception; }
            [DebuggerStepThrough()]
            set { exception = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the error information recorded on this result,
        /// resetting its return code and error metadata.
        /// </summary>
        [DebuggerStepThrough()]
        public void Clear()
        {
            //
            // NOTE: Clear the error information only.
            //
            returnCode = ReturnCode.Ok;
            previousReturnCode = ReturnCode.Ok;

            errorLine = 0;
            errorCode = null;
            errorInfo = null;

            exception = null;

            UnsetFlags(ResultFlags.Error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the error information from the specified
        /// interpreter into this result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose error information is saved.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the error information was saved; otherwise, zero (e.g.
        /// when the interpreter is null).
        /// </returns>
        [DebuggerStepThrough()]
        public bool Save(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                returnCode = interpreter.ReturnCode;
                previousReturnCode = returnCode;

                errorLine = interpreter.ErrorLine; /* EXEMPT */
                errorCode = interpreter.ErrorCode;
                errorInfo = interpreter.ErrorInfo;

                exception = interpreter.Exception;

                SetFlags(ResultFlags.Error);

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the error information from this result into the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose error information is restored.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the error information was restored; otherwise, zero
        /// (e.g. when the interpreter is null).
        /// </returns>
        [DebuggerStepThrough()]
        public bool Restore(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                interpreter.ReturnCode = returnCode;

                interpreter.ErrorLine = errorLine;
                interpreter.ErrorCode = errorCode;
                interpreter.ErrorInfo = errorInfo;

                interpreter.Exception = exception;

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a copy of this result, including all of its
        /// state.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this result.
        /// </returns>
        [DebuggerStepThrough()]
        public object Clone()
        {
            return new Result(this, ResultFlags.CopyAll);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Members
        /// <summary>
        /// The engine-specific data associated with this result, if any.
        /// </summary>
        private object engineData;
        /// <summary>
        /// Gets or sets the engine-specific data associated with this result.
        /// </summary>
        internal object EngineData
        {
            [DebuggerStepThrough()]
            get { return engineData; }
            [DebuggerStepThrough()]
            set { engineData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The managed stack trace captured for this result, if any.
        /// </summary>
        private string stackTrace;
        /// <summary>
        /// Gets the managed stack trace captured for this result.
        /// </summary>
        internal string StackTrace
        {
            [DebuggerStepThrough()]
            get { return stackTrace; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached full-string form of this result (including stack
        /// traces), if any.
        /// </summary>
        private string fullString;
        /// <summary>
        /// Gets the cached full-string form of this result.
        /// </summary>
        internal string FullString
        {
            [DebuggerStepThrough()]
            get { return fullString; }
        }
        #endregion
    }
}
