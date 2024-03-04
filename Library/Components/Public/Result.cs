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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("9b092a26-fb6f-4487-ad6e-560ce24f249b")]
    public sealed class Result :
            IResult, IToString, IString, ICloneable
    {
        #region Public Constants
        public static readonly string NoValue = null;
        public static readonly IClientData NoClientData = null;

        ///////////////////////////////////////////////////////////////////////

        public static readonly Result Null = new Result((object)null);
        public static readonly Result Empty = FromString(String.Empty);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        private static bool UseEmptyForNull = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        // WARNING: Setting this to true could be very expensive.
        //
        private static bool? PopulateStackTrace = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        [DebuggerStepThrough()]
        private Result()
        {
            Reset(); /* NOTE: Well-known state. */

            if (ShouldPopulateStackTrace())
            {
                stackTrace = DebugOps.GetStackTraceString();
                SetFlags(ResultFlags.StackTrace);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support
        //       for conversions from other data types.
        //
        [DebuggerStepThrough()]
        private Result(
            object value
            )
            : this(ReturnCode.Ok, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Result(
            IGetValue value
            )
            : this(ReturnCode.Ok, (value != null) ? value.Value : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support
        //       for conversions from the Interpreter data type.
        //
        [DebuggerStepThrough()]
        private Result(
            Interpreter value
            )
            : this(ReturnCode.Ok, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support
        //       for conversions from the Argument data type.
        //
        [DebuggerStepThrough()]
        private Result(
            Argument value
            )
            : this(ReturnCode.Ok, (value != null) ? value.Value : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support
        //       for the Copy static factory method.
        //
        [DebuggerStepThrough()]
        private Result(
            Result result,
            ResultFlags flags
            )
            : this()
        {
            CopyFrom(result, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is primarily intended for success (Ok)
        //       results.
        //
        [DebuggerStepThrough()]
        private Result(
            ReturnCode returnCode,
            object value
            )
            : this()
        {
            SetValueOnly(returnCode, value);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is primarily intended for failure (Error)
        //       results.
        //
        [DebuggerStepThrough()]
        private Result( /* NOT USED */
            ReturnCode returnCode,
            object value,
            int errorLine,
            string errorCode,
            string errorInfo,
            Exception exception
            )
            : this(returnCode, value)
        {
            SetErrorOnly(errorLine, errorCode, errorInfo, exception);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        [DebuggerStepThrough()]
        private void SetFlags(
            ResultFlags flags
            )
        {
            this.flags |= flags;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private void UnsetFlags(
            ResultFlags flags
            )
        {
            this.flags &= ~flags;
        }

        ///////////////////////////////////////////////////////////////////////

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

        [DebuggerStepThrough()]
        private void Reset()
        {
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
            value = NoValue;
            clientData = NoClientData;

#if CACHE_RESULT_TOSTRING
            InvalidateCachedString(false);
#endif

            UnsetFlags(ResultFlags.String);
        }

        ///////////////////////////////////////////////////////////////////////

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
        }

        ///////////////////////////////////////////////////////////////////////

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
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Stack Trace Helpers
        [DebuggerStepThrough()]
        private static bool ShouldPopulateStackTrace()
        {
            if (PopulateStackTrace == null)
            {
                bool stackTrace;

                if (CommonOps.Environment.DoesVariableExist(
                        EnvVars.ResultStack))
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
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

        [DebuggerStepThrough()]
        public static Result Copy(
            Result result,
            ResultFlags flags
            )
        {
            return Copy(result, null, flags);
        }

        ///////////////////////////////////////////////////////////////////////

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

            if (left.flags != right.flags)
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static String Helpers
        #region Dead Code
#if DEAD_CODE
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

                    if (@string != null)
                        return @string.Length;
                    else
                        return @default;
                }
                else
#endif
                {
                    return value.ToString().Length;
                }
            }
            else
            {
                return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

                    return @string;
                }
                else
#endif
                {
                    return value.ToString();
                }
            }
            else
            {
                return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        [DebuggerStepThrough()]
        private static Result FromInterpreter(
            Interpreter value
            )
        {
            return new Result(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromArgument(
            Argument value
            )
        {
            return new Result(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDouble(
            double value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDecimal(
            decimal value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromEnum(
            Enum value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromException(
            Exception value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromVersion(
            Version value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromResultList(
            ResultList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromStringBuilder(
            StringBuilder value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromWideInteger(
            long value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromInteger(
            int value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromBoolean(
            bool value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromCharacter(
            char value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

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

        [DebuggerStepThrough()]
        private static Result FromDateTime(
            DateTime value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromTimeSpan(
            TimeSpan value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromGuid(
            Guid value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromUri(
            Uri value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromString(
            string value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromByte(
            byte value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromByteArray(
            byte[] value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromByteList(
            ByteList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromList(
            IStringList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDictionary(
            IDictionary value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

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
        [DebuggerStepThrough()]
        public static implicit operator string(
            Result result
            )
        {
            return ToString(result, UseEmptyForNull ? String.Empty : null);
        }

        ///////////////////////////////////////////////////////////////////////

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

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringPairList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            ClientDataDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            DateTime value
            )
        {
            return FromDateTime(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            TimeSpan value
            )
        {
            return FromTimeSpan(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Guid value
            )
        {
            return FromGuid(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Uri value
            )
        {
            return FromUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            string value
            )
        {
            return FromString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            byte value
            )
        {
            return FromByte(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            byte[] value
            )
        {
            return FromByteArray(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            ByteList value
            )
        {
            return FromByteList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            char value
            )
        {
            return FromCharacter(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            double value
            )
        {
            return FromDouble(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            decimal value
            )
        {
            return FromDecimal(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Enum value
            )
        {
            return FromEnum(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Exception value
            )
        {
            return FromException(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Version value
            )
        {
            return FromVersion(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            ResultList value
            )
        {
            return FromResultList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringBuilder value
            )
        {
            return FromStringBuilder(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            long value
            )
        {
            return FromWideInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            int value
            )
        {
            return FromInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

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
        [DebuggerStepThrough()]
        public int IndexOf(
            string value,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).IndexOf(value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

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

        [DebuggerStepThrough()]
        public string Substring(
            int startIndex
            )
        {
            return ToString(this, String.Empty).Substring(startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Substring(
            int startIndex,
            int length
            )
        {
            return ToString(this, String.Empty).Substring(startIndex, length);
        }

        ///////////////////////////////////////////////////////////////////////

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

        [DebuggerStepThrough()]
        public string Replace(
            string oldValue,
            string newValue
            )
        {
            return ToString(this, String.Empty).Replace(oldValue, newValue);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Trim()
        {
            return ToString(this, String.Empty).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Trim(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).Trim(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string TrimStart(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).TrimStart(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string TrimEnd(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).TrimEnd(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public char[] ToCharArray()
        {
            return ToString(this, String.Empty).ToCharArray();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        [DebuggerStepThrough()]
        public string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(
            ToStringFlags flags,
            string @default /* NOT USED */
            )
        {
            return ToString("{0}");
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(
            string format
            )
        {
            return String.Format(format, ToString(this, null));
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(string format, int limit, bool strict)
        {
            return FormatOps.Ellipsis(
                String.Format(format, ToString(this, null)), limit, strict);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        [DebuggerStepThrough()]
        public override string ToString()
        {
            return ToString(this, String.Empty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
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
        private IClientData valueData;
        public IClientData ValueData
        {
            [DebuggerStepThrough()]
            get { return valueData; }
            [DebuggerStepThrough()]
            set { valueData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData extraData;
        public IClientData ExtraData
        {
            [DebuggerStepThrough()]
            get { return extraData; }
            [DebuggerStepThrough()]
            set { extraData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame callFrame;
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
        private object value;
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
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            [DebuggerStepThrough()]
            get { return ToString(this, value, null); }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            [DebuggerStepThrough()]
            get { return GetLength(this, value, 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Private
#if CACHE_RESULT_TOSTRING
        [DebuggerStepThrough()]
        internal void InvalidateCachedString(
            bool children /* NOT USED */
            )
        {
            @string = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private string @string; /* CACHE */
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
        private ResultFlags flags;
        public ResultFlags Flags
        {
            [DebuggerStepThrough()]
            get { return flags; }
            [DebuggerStepThrough()]
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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

        [DebuggerStepThrough()]
        public IResult Copy(
            ResultFlags flags
            )
        {
            return new Result(this, flags);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private ReturnCode returnCode;
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

        private ReturnCode previousReturnCode;
        public ReturnCode PreviousReturnCode
        {
            [DebuggerStepThrough()]
            get { return previousReturnCode; }
            [DebuggerStepThrough()]
            set { previousReturnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int errorLine;
        public int ErrorLine
        {
            [DebuggerStepThrough()]
            get { return errorLine; }
            [DebuggerStepThrough()]
            set { errorLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string errorCode;
        public string ErrorCode
        {
            [DebuggerStepThrough()]
            get { return errorCode; }
            [DebuggerStepThrough()]
            set { errorCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string errorInfo;
        public string ErrorInfo
        {
            [DebuggerStepThrough()]
            get { return errorInfo; }
            [DebuggerStepThrough()]
            set { errorInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Exception exception;
        public Exception Exception
        {
            [DebuggerStepThrough()]
            get { return exception; }
            [DebuggerStepThrough()]
            set { exception = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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
        [DebuggerStepThrough()]
        public object Clone()
        {
            return new Result(this, ResultFlags.CopyAll);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Members
        private object engineData;
        internal object EngineData
        {
            [DebuggerStepThrough()]
            get { return engineData; }
            [DebuggerStepThrough()]
            set { engineData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string stackTrace;
        internal string StackTrace
        {
            [DebuggerStepThrough()]
            get { return stackTrace; }
        }
        #endregion
    }
}
