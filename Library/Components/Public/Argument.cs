/*
 * Argument.cs --
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

#if ARGUMENT_CACHE
using System.Collections.Generic;
#endif

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
    [ObjectId("3db192d7-76fa-485f-949c-a75bd929e66a")]
    public sealed class Argument :
            IArgument, IScriptLocation, IToString, IString,
            ICanHashValue, ICloneable
    {
        #region Private Constants
        #region System.Object Overrides Support Constants
#if ARGUMENT_CACHE
        //
        // HACK: This is purposely not read-only.
        //
        private static int HashCodeSeed = 0x23f910c2;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static readonly ArgumentFlags NoFlags = ArgumentFlags.None;
        public static readonly string NoName = String.Empty;
        public static readonly string NoValue = String.Empty;
        public static readonly string NoString = null;
        public static readonly string NoDefault = null;
        public static readonly string NoFileName = null;
        public static readonly int NoLine = Parser.UnknownLine;
        public static readonly bool NoViaSource = false;
        public static readonly byte[] NoHashValue = null;

        ///////////////////////////////////////////////////////////////////////

        public static readonly Argument Null = InternalCreate();
        public static readonly Argument Empty = InternalCreate(String.Empty);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        private static bool UseEmptyForNull = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
#if CACHE_ARGUMENT_TOSTRING
        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name,
            object value,
            object @default,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
            byte[] hashValue
            )
            : this(flags, name, value, NoString, @default, fileName,
                   startLine, endLine, viaSource, hashValue)
        {
            // do nothing.
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name,
            object value,
#if CACHE_ARGUMENT_TOSTRING
            string @string,
#endif
            object @default,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
            byte[] hashValue
            )
        {
            this.flags = flags;
            this.name = name;
            this.value = value;

#if CACHE_ARGUMENT_TOSTRING
            this.@string = @string;
#endif

            this.@default = @default;
            this.fileName = fileName;
            this.startLine = startLine;
            this.endLine = endLine;
            this.viaSource = viaSource;
            this.hashValue = hashValue;
            this.engineData = null;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name,
            object value,
            object @default
            )
            : this(flags, name, value, @default, NoFileName, NoLine, NoLine,
                   NoViaSource, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name,
            object value
            )
            : this(flags, name, value, NoDefault, NoFileName, NoLine, NoLine,
                   NoViaSource, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name
            )
            : this(flags, name, NoValue, NoDefault, NoFileName, NoLine, NoLine,
                   NoViaSource, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the StringOps.GetArgumentFromObject method and
        //       this class only.
        //
        [DebuggerStepThrough()]
        private Argument(
            object value
            )
            : this(NoFlags, NoName, value, NoDefault, NoFileName, NoLine,
                   NoLine, NoViaSource, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            Argument value
            )
            : this((value != null) ? value.Flags : NoFlags,
                   (value != null) ? value.Name : NoName,
                   (value != null) ? value.Value : NoValue,
#if CACHE_ARGUMENT_TOSTRING
                   (value != null) ? value.String : NoString,
#endif
                   (value != null) ? value.Default : NoDefault,
                   (value != null) ? value.FileName : NoFileName,
                   (value != null) ? value.StartLine : NoLine,
                   (value != null) ? value.EndLine : NoLine,
                   (value != null) ? value.ViaSource : NoViaSource,
                   (value != null) ? value.HashValue : NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        [DebuggerStepThrough()]
        private Argument(
            Number value
            )
            : this(NoFlags, NoName, (value != null) ? value.Value : null,
                   NoDefault, NoFileName, NoLine, NoLine, NoViaSource,
                   NoHashValue)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            Interpreter value
            )
            : this(NoFlags, NoName, value, NoDefault, NoFileName, NoLine,
                   NoLine, NoViaSource, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        private Argument(
            Variant value
            )
            : this(NoFlags, NoName, (value != null) ? value.Value : null,
                   NoDefault, NoFileName, NoLine, NoLine, NoViaSource,
                   NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            Result value
            )
            : this(NoFlags, NoName, (value != null) ? value.Value : null,
                   NoDefault, NoFileName, NoLine, NoLine, NoViaSource,
                   NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Argument(
            Result value,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource
            )
            : this(NoFlags, NoName, (value != null) ? value.Value : null,
#if CACHE_ARGUMENT_TOSTRING
#if CACHE_RESULT_TOSTRING
                   (value != null) ? value.CachedString : null,
#else
                   null,
#endif
#endif
                   NoDefault, fileName, startLine, endLine, viaSource,
                   NoHashValue)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reset Helper Methods
        internal void ResetValue(
            Interpreter interpreter,
            bool zero
            )
        {
#if !MONO && NATIVE && WINDOWS
            if (zero && (value is string) && (interpreter != null) &&
                interpreter.HasZeroString())
            {
                ReturnCode zeroCode;
                bool zeroNoComplain = false;
                Result zeroError = null;

                zeroCode = StringOps.ZeroString(
                    (string)value, ref zeroNoComplain, ref zeroError);

                if (!zeroNoComplain && (zeroCode != ReturnCode.Ok))
                    DebugOps.Complain(interpreter, zeroCode, zeroError);
            }
#endif

            value = null;

#if CACHE_ARGUMENT_TOSTRING
#if !MONO && NATIVE && WINDOWS
            if (zero && (@string != null) && (interpreter != null) &&
                interpreter.HasZeroString())
            {
                ReturnCode zeroCode;
                bool zeroNoComplain = false;
                Result zeroError = null;

                zeroCode = StringOps.ZeroString(
                    @string, ref zeroNoComplain, ref zeroError);

                if (!zeroNoComplain && (zeroCode != ReturnCode.Ok))
                    DebugOps.Complain(interpreter, zeroCode, zeroError);
            }
#endif

            InvalidateCachedString(true);
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static String Helpers
        #region Dead Code
#if DEAD_CODE
        [DebuggerStepThrough()]
        private static int Compare(
            Argument argument1,
            Argument argument2,
            StringComparison comparisonType
            )
        {
            return SharedStringOps.Compare(
                ToString(argument1, null), ToString(argument2, null),
                comparisonType);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        [DebuggerStepThrough()]
        public static object GetValue(
            Argument argument
            )
        {
            if (argument == null)
                return null;

            return argument.Value;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        #region Argument Creation Support
#if LIST_CACHE
        [DebuggerStepThrough()]
        private static bool IsReadOnly(
            object value
            )
        {
            object localValue = value;

        retry:

            IReadOnly readOnly = localValue as IReadOnly;

            if (readOnly != null)
                return readOnly.IsReadOnly;

            IGetValue getValue = localValue as IGetValue;

            if (getValue != null)
            {
                localValue = getValue.Value;
                goto retry;
            }

            //
            // HACK: For our purposes, assume anything that does not
            //       implement the IReadOnly interface is read-only.
            //
            return true;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static object GetValue(
            object value
            )
        {
            if (value is IGetValue)
                return ((IGetValue)value).Value;

            return value;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Direct Argument Creation
        //
        // WARNING: This method is for use by this class and the following
        //          external methods only:
        //
        //          EngineContext (constructor)
        //          StringOps.GetArgumentFromObject
        //
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate()
        {
            return new Argument((object)null);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            ArgumentList value
            )
        {
            return new Argument((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument PrivateCreate(
            object value
            )
        {
            return new Argument(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument PrivateCreate(
            Argument value
            )
        {
            return new Argument(value);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is for use by this class and the following
        //          external methods only:
        //
        //          OptionDictionary.ToArgumentList
        //
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            Variant value
            )
        {
            return new Argument(value);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is for use by this class and the following
        //          external methods only:
        //
        //          ArgumentList (constructor)
        //          StringOps.GetArgumentFromObject
        //          OptionDictionary.ToArgumentList
        //
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            Result value
            )
        {
            return new Argument(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            ArgumentFlags flags,
            string name
            )
        {
            return new Argument(flags, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            ArgumentFlags flags,
            string name,
            object value
            )
        {
            return new Argument(flags, name, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            ArgumentFlags flags,
            string name,
            object value,
            object @default
            )
        {
            return new Argument(flags, name, value, @default);
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && BREAKPOINTS
        [DebuggerStepThrough()]
        private static Argument PrivateCreate(
            Result value,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource
            )
        {
            return new Argument(
                value, fileName, startLine, endLine, viaSource);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached Argument Creation
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument GetOrCreate(
            Interpreter interpreter,
            Variant value,
            bool createOnly
            )
        {
#if ARGUMENT_CACHE
            if (!createOnly)
            {
                Argument argument = null;

                if ((interpreter != null) && interpreter.CanUseArgumentCache(
                        CacheFlags.ForVariant, ref argument))
                {
                    argument.Reset(ArgumentFlags.ResetWithDefault);
                    argument.value = (value != null) ? value.Value : null;

                    if (interpreter.GetCachedArgument(ref argument))
                        return argument;

#if LIST_CACHE
                    if (IsReadOnly(value))
#endif
                    {
                        argument = InternalCreate(value);

                        interpreter.AddCachedArgument(argument);

                        return argument;
                    }
                }
            }
#endif

            return InternalCreate(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument GetOrCreate(
            Interpreter interpreter,
            Result value,
            bool createOnly
            )
        {
#if ARGUMENT_CACHE
            if (!createOnly)
            {
                Argument argument = null;

                if ((interpreter != null) && interpreter.CanUseArgumentCache(
                        CacheFlags.ForResult, ref argument))
                {
                    argument.Reset(ArgumentFlags.ResetWithDefault);
                    argument.value = (value != null) ? value.Value : null;

                    if (interpreter.GetCachedArgument(ref argument))
                        return argument;

#if LIST_CACHE
                    if (IsReadOnly(value))
#endif
                    {
                        argument = InternalCreate(value);

                        interpreter.AddCachedArgument(argument);

                        return argument;
                    }
                }
            }
#endif

            return InternalCreate(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument GetOrCreate(
            Interpreter interpreter,
            ArgumentFlags flags,
            string name,
            object value,
            bool createOnly
            )
        {
#if ARGUMENT_CACHE
            if (!createOnly)
            {
                Argument argument = null;

                if ((interpreter != null) && interpreter.CanUseArgumentCache(
                        CacheFlags.ForProcedure, ref argument))
                {
                    argument.Reset(ArgumentFlags.ResetWithDefault);
                    argument.flags = flags;
                    argument.name = name;

                    object localValue = GetValue(value);
                    argument.value = localValue;

                    if (interpreter.GetCachedArgument(ref argument))
                        return argument;

#if LIST_CACHE
                    if (IsReadOnly(localValue))
#endif
                    {
                        argument = InternalCreate(flags, name, localValue);

                        interpreter.AddCachedArgument(argument);

                        return argument;
                    }
                }
            }
#endif

            return InternalCreate(flags, name, value);
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && BREAKPOINTS
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument GetOrCreate(
            Interpreter interpreter,
            Result value,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
            bool createOnly
            )
        {
#if ARGUMENT_CACHE
            if (!createOnly)
            {
                Argument argument = null;

                if ((interpreter != null) && interpreter.CanUseArgumentCache(
                        CacheFlags.ForResultWithLocation, ref argument))
                {
                    argument.Reset(ArgumentFlags.ResetWithDefault);
                    argument.value = (value != null) ? value.Value : null;
                    argument.fileName = fileName;
                    argument.startLine = startLine;
                    argument.endLine = endLine;
                    argument.viaSource = viaSource;

                    if (interpreter.GetCachedArgument(ref argument))
                        return argument;

#if LIST_CACHE
                    if (IsReadOnly(value))
#endif
                    {
                        argument = PrivateCreate(
                            value, fileName, startLine, endLine, viaSource);

                        interpreter.AddCachedArgument(argument);

                        return argument;
                    }
                }
            }
#endif

            return PrivateCreate(
                value, fileName, startLine, endLine, viaSource);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        [DebuggerStepThrough()]
        private static int GetLength(
            Argument argument,
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
#if CACHE_ARGUMENT_TOSTRING
                if (argument != null)
                {
                    string @string = argument.@string;

                    if (@string != null)
                        return @string.Length;

                    @string = value.ToString();
                    argument.@string = @string;

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
            Argument argument,
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
#if CACHE_ARGUMENT_TOSTRING
                if (argument != null)
                {
                    string @string = argument.@string;

                    if (@string != null)
                        return @string;

                    @string = value.ToString();
                    argument.@string = @string;

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

        [DebuggerStepThrough()]
        private static bool IsSupported(
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
            else if (type == typeof(Result))
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
        internal static Argument FromObject(
            object value,
            bool forceCopy,
            bool supportedOnly,
            bool toString
            )
        {
            if (value == null)
                return null;

            Argument argument = value as Argument;

            if (argument != null)
            {
                //
                // NOTE: Otherwise, use the existing reference.
                //
                if (forceCopy)
                    argument = PrivateCreate(argument); /* COPY */
            }
            else if (!supportedOnly ||
                IsSupported(AppDomainOps.MaybeGetType(value)))
            {
                argument = PrivateCreate(value); /* WRAP */
            }
            else if (toString)
            {
                argument = StringOps.GetArgumentFromObject(value); /* String */
            }

            return argument;
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromIObject(
            IObject value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromInterpreter(
            Interpreter value
            )
        {
            return InternalCreate(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromResult(
            Result value
            )
        {
            return InternalCreate(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromVariant(
            Variant value
            )
        {
            return InternalCreate(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromDouble(
            double value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromDecimal(
            decimal value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromEnum(
            Enum value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromException(
            Exception value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromVersion(
            Version value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromResultList(
            ResultList value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromStringBuilder(
            StringBuilder value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromWideInteger(
            long value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromInteger(
            int value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromBoolean(
            bool value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromCharacter(
            char value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromCharacters(
            char? value1,
            char? value2
            )
        {
            return PrivateCreate((object)String.Format("{0}{1}",
                (value1 != null) ? value1.ToString() : null,
                (value2 != null) ? value2.ToString() : null));
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromDateTime(
            DateTime value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromTimeSpan(
            TimeSpan value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromGuid(
            Guid value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromUri(
            Uri value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromString(
            string value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromByte(
            byte value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromByteArray(
            byte[] value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Argument FromByteList(
            ByteList value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromList(
            IStringList value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDictionary(
            IDictionary value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromCommandBuilder(
            CommandBuilder value
            )
        {
            if (value == null)
                return null;

            return PrivateCreate(value.GetResult());
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static string ToString(
            Argument argument,
            string @default
            )
        {
            if (argument == null)
                return @default;

            if (!argument.HasFlags(ArgumentFlags.ToStringMask, false))
                return ToString(argument, argument.Value, @default);

            return argument.ToString(ToStringFlags.None, @default);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        [DebuggerStepThrough()]
        public static implicit operator string(
            Argument argument
            )
        {
            return ToString(argument, UseEmptyForNull ? String.Empty : null);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
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
        public static implicit operator Argument(
            Result value
            )
        {
            if (value != null)
                return FromResult(value);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Variant value
            )
        {
            if (value != null)
                return FromVariant(value);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringPairList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            ClientDataDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            DateTime value
            )
        {
            return FromDateTime(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            TimeSpan value
            )
        {
            return FromTimeSpan(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Guid value
            )
        {
            return FromGuid(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Uri value
            )
        {
            return FromUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            string value
            )
        {
            return FromString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            byte value
            )
        {
            return FromByte(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            byte[] value
            )
        {
            return FromByteArray(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            ByteList value
            )
        {
            return FromByteList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            char value
            )
        {
            return FromCharacter(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            double value
            )
        {
            return FromDouble(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            decimal value
            )
        {
            return FromDecimal(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Enum value
            )
        {
            return FromEnum(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Exception value
            )
        {
            return FromException(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Version value
            )
        {
            return FromVersion(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            ResultList value
            )
        {
            return FromResultList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringBuilder value
            )
        {
            return FromStringBuilder(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            long value
            )
        {
            return FromWideInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
            int value
            )
        {
            return FromInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Argument(
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
            Argument value,
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
            string @default
            )
        {
            ArgumentFlags argumentFlags = this.flags;

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(flags, ToStringFlags.NameAndValue, true) ||
                FlagOps.HasFlags(argumentFlags, ArgumentFlags.Debug, true))
            {
                IStringList list = new StringList();

                if (!String.IsNullOrEmpty(name))
                    list.Add(name);

                list.Add(ToString(this, value, @default));

                return list.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(flags, ToStringFlags.NameAndDefault, true))
            {
                IStringList list = new StringList();

                if (!String.IsNullOrEmpty(name))
                    list.Add(name);

                object localDefault = this.@default;

                if (localDefault != null)
                    list.Add(ToString(this, localDefault, @default));

                return list.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            if (!FlagOps.HasFlags(
                    argumentFlags, ArgumentFlags.NameOnly, true))
            {
                return ToString(this, value, @default);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(flags, ToStringFlags.Decorated, true))
            {
                if (FlagOps.HasFlags(
                        argumentFlags, ArgumentFlags.ArgumentList, true))
                {
                    if (FlagOps.HasFlags(
                            argumentFlags, ArgumentFlags.NamedArgument,
                            true))
                    {
                        return "?argName argValue ...?";
                    }
                    else
                    {
                        return "?arg ...?";
                    }
                }
                else if (FlagOps.HasFlags(
                        argumentFlags, ArgumentFlags.HasDefault, true))
                {
                    if (FlagOps.HasFlags(
                            argumentFlags, ArgumentFlags.NamedArgument,
                            true))
                    {
                        return (name != null) ?
                            String.Format("?{0} value?", name) : @default;
                    }
                    else
                    {
                        return (name != null) ?
                            String.Format("?{0}?", name) : @default;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    argumentFlags, ArgumentFlags.NamedArgument, true))
            {
                return String.Format(
                    "{0} value", (name != null) ? name : @default);
            }
            else
            {
                return (name != null) ? name : @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(
            string format
            )
        {
            return String.Format(format, name, ToString(this, null));
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(string format, int limit, bool strict)
        {
            return FormatOps.Ellipsis(
                String.Format(format, name, ToString(this, null)), limit,
                strict);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
#if ARGUMENT_CACHE
        [DebuggerStepThrough()]
        public override bool Equals(
            object obj
            )
        {
            Argument argument = obj as Argument;

            if (argument == null)
                return false;

            ///////////////////////////////////////////////////////////////////

            if (argument.flags != flags)
                return false;

            if (argument.startLine != startLine)
                return false;

            if (argument.endLine != endLine)
                return false;

            if (argument.viaSource != viaSource)
                return false;

            ///////////////////////////////////////////////////////////////////

            if (!StringOps.StringOrObjectEquals(argument.value, value))
                return false;

            ///////////////////////////////////////////////////////////////////

            if (!StringOps.StringOrObjectEquals(argument.@default, @default))
                return false;

            ///////////////////////////////////////////////////////////////////

            if (!StringOps.StringEquals(argument.name, name))
                return false;

            if (!StringOps.StringEquals(argument.fileName, fileName))
                return false;

            ///////////////////////////////////////////////////////////////////

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public override int GetHashCode()
        {
            int result = HashCodeSeed;

            ///////////////////////////////////////////////////////////////////

            if (flags != NoFlags)
                result ^= (flags.GetHashCode() << 1);

            if (startLine != NoLine)
                result ^= (startLine.GetHashCode() << 2);

            if (endLine != NoLine)
                result ^= (endLine.GetHashCode() << 3);

            if (viaSource)
                result ^= (viaSource.GetHashCode() << 4);

            ///////////////////////////////////////////////////////////////////

            if (value != null)
                result ^= value.GetHashCode();

            ///////////////////////////////////////////////////////////////////

            if (@default != null)
                result ^= @default.GetHashCode();

            ///////////////////////////////////////////////////////////////////

            if (name != null)
                result ^= name.GetHashCode();

            if (fileName != null)
                result ^= fileName.GetHashCode();

            ///////////////////////////////////////////////////////////////////

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public override string ToString()
        {
            return ToString(ToStringFlags.None, String.Empty);
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

        #region IGetValue Members
        private object value;
        public object Value
        {
            [DebuggerStepThrough()]
            get { return value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            [DebuggerStepThrough()]
            get
            {
                if (HasFlags(ArgumentFlags.ToStringMask, false))
                    return ToString(ToStringFlags.None, null);
                else
                    return ToString(this, value, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            [DebuggerStepThrough()]
            get
            {
                if (HasFlags(ArgumentFlags.ToStringMask, false))
                {
                    string result = ToString(ToStringFlags.None, null);
                    return (result != null) ? result.Length : 0;
                }
                else
                {
                    return GetLength(this, value, 0);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Private
#if CACHE_ARGUMENT_TOSTRING
        [DebuggerStepThrough()]
        internal void InvalidateCachedString(
            bool zero
            )
        {
            @string = zero ? null : NoString;
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

        #region IArgument Members
        private string name;
        public string Name
        {
            [DebuggerStepThrough()]
            get { return name; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentFlags flags;
        public ArgumentFlags Flags
        {
            [DebuggerStepThrough()]
            get { return flags; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private object @default;
        public object Default
        {
            [DebuggerStepThrough()]
            get { return @default; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public void Reset(
            ArgumentFlags flags
            )
        {
            if (FlagOps.HasFlags(flags, ArgumentFlags.Zero, true))
            {
                this.flags = ArgumentFlags.None;
                this.name = null;
                this.value = null;

#if CACHE_ARGUMENT_TOSTRING
                InvalidateCachedString(true);
#endif

                this.@default = null;
                this.fileName = null;
                this.startLine = 0;
                this.endLine = 0;
                this.viaSource = false;

                this.hashValue = null;
            }
            else
            {
                this.flags = NoFlags;
                this.name = NoName;
                this.value = NoValue;

#if CACHE_ARGUMENT_TOSTRING
                InvalidateCachedString(false);
#endif

                this.@default = NoDefault;
                this.fileName = NoFileName;
                this.startLine = NoLine;
                this.endLine = NoLine;
                this.viaSource = NoViaSource;

                this.hashValue = NoHashValue;
            }

            this.engineData = null;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public bool HasFlags(
            ArgumentFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptLocation Members
        private string fileName;
        public string FileName
        {
            [DebuggerStepThrough()]
            get { return fileName; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private int startLine;
        public int StartLine
        {
            [DebuggerStepThrough()]
            get { return startLine; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private int endLine;
        public int EndLine
        {
            [DebuggerStepThrough()]
            get { return endLine; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool viaSource;
        public bool ViaSource
        {
            [DebuggerStepThrough()]
            get { return viaSource; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public StringPairList ToList()
        {
            return ToList(false);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public StringPairList ToList(bool scrub)
        {
            StringPairList list = new StringPairList();

            list.Add("Flags", this.Flags.ToString());
            list.Add("Name", this.Name);
            list.Add("Value", ToString(this, this.Value, null));
            list.Add("Default", StringOps.GetStringFromObject(this.Default));

            list.Add("FileName", scrub ? PathOps.ScrubPath(
                GlobalState.GetBasePath(), this.FileName) : this.FileName);

            list.Add("StartLine", this.StartLine.ToString());
            list.Add("EndLine", this.EndLine.ToString());
            list.Add("ViaSource", this.ViaSource.ToString());

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICanHashValue Members
        [DebuggerStepThrough()]
        public byte[] GetHashValue(
            ref Result error
            )
        {
            if (hashValue == null)
            {
                hashValue = RuntimeOps.HashArgument(
                    null, this, null, ref error);
            }

            return hashValue;
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] hashValue;
        public byte[] HashValue
        {
            [DebuggerStepThrough()]
            get { return hashValue; }
            [DebuggerStepThrough()]
            set { hashValue = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        [DebuggerStepThrough()]
        public object Clone()
        {
            return PrivateCreate(this);
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
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        internal void SetEngineDataForIHaveStringBuilder(
            object engineData,
            ArgumentList arguments
            )
        {
            IHaveStringBuilder haveStringBuilder =
                engineData as IHaveStringBuilder;

            if (haveStringBuilder == null)
                return;

            haveStringBuilder.Arguments = arguments;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the StringBuilderWrapper class only.
        //
        [DebuggerStepThrough()]
        internal void ResetValue(
            StringBuilder builder
            )
        {
#if CACHE_ARGUMENT_TOSTRING
            InvalidateCachedString(true);
#endif

            value = builder;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the ArgumentList.CloneWithNewFirstValue
        //          method only.
        //
        [DebuggerStepThrough()]
        internal void SetValue(
            object value
            )
        {
#if CACHE_ARGUMENT_TOSTRING
            InvalidateCachedString(true);
#endif

            this.value = value;
        }
        #endregion
    }
}
