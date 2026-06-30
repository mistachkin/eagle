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
    /// This class represents a single argument to an Eagle command, procedure,
    /// or function.  It wraps an underlying value of (almost) any type and
    /// carries optional metadata such as its name, default value, script
    /// location, and caching and hashing information.  It supports implicit
    /// conversion to and from the common value types so it can be assigned
    /// naturally, and it provides string-oriented helper methods via the
    /// <see cref="IString" /> interface.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("3db192d7-76fa-485f-949c-a75bd929e66a")]
    public sealed class Argument :
            IArgument, IScriptLocation, IToString, IString,
            ICacheValue, ICanHashValue, ICloneable
    {
        #region Private Constants
        #region System.Object Overrides Support Constants
#if ARGUMENT_CACHE
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The seed value used when computing the hash code for an argument.
        /// </summary>
        private static int HashCodeSeed = 0x23f910c2;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The sentinel used to represent the absence of argument flags.
        /// </summary>
        public static readonly ArgumentFlags NoFlags = ArgumentFlags.None;
        /// <summary>
        /// The sentinel used to represent the absence of an argument name (the
        /// empty string).
        /// </summary>
        public static readonly string NoName = String.Empty;
        /// <summary>
        /// The sentinel used to represent the absence of a value (the empty
        /// string).
        /// </summary>
        public static readonly string NoValue = String.Empty;
        /// <summary>
        /// The sentinel used to represent the absence of a cached string
        /// representation (a null string).
        /// </summary>
        public static readonly string NoString = null;
        /// <summary>
        /// The sentinel used to represent the absence of a default value (a
        /// null reference).
        /// </summary>
        public static readonly string NoDefault = null;
        /// <summary>
        /// The sentinel used to represent the absence of a script file name (a
        /// null string).
        /// </summary>
        public static readonly string NoFileName = null;
        /// <summary>
        /// The sentinel used to represent the absence of a known script line
        /// number.
        /// </summary>
        public static readonly int NoLine = Parser.UnknownLine;
        /// <summary>
        /// The sentinel used to represent that an argument did not originate
        /// from the [source] command.
        /// </summary>
        public static readonly bool NoViaSource = false;
        /// <summary>
        /// The sentinel used to represent the absence of a cached value (a null
        /// reference).
        /// </summary>
        public static readonly object NoCacheValue = null;
        /// <summary>
        /// The sentinel used to represent the absence of a cache generation
        /// number.
        /// </summary>
        public static readonly long NoCacheGeneration = 0;
        /// <summary>
        /// The sentinel used to represent the absence of a hash value (a null
        /// reference).
        /// </summary>
        public static readonly byte[] NoHashValue = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A shared, pre-built argument instance whose value is null.
        /// </summary>
        public static readonly Argument Null = InternalCreate();
        /// <summary>
        /// A shared, pre-built argument instance whose value is the empty
        /// string.
        /// </summary>
        public static readonly Argument Empty = InternalCreate(String.Empty);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, a null value is treated as the empty string when
        /// producing the string form of an argument.
        /// </summary>
        private static bool UseEmptyForNull = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
#if CACHE_ARGUMENT_TOSTRING
        /// <summary>
        /// Constructs an argument with the specified flags, name, value,
        /// default value, script location, and caching metadata.  This
        /// overload delegates to the primary constructor, supplying no cached
        /// string representation.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value wrapped by the argument.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The default value of the argument.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the script file the argument originated from.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The starting script line number of the argument.
        /// </param>
        /// <param name="endLine">
        /// The ending script line number of the argument.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the argument originated via the [source] command.
        /// </param>
        /// <param name="cacheValue">
        /// The opaque cached value associated with the argument.  This
        /// parameter may be null.
        /// </param>
        /// <param name="cacheGeneration">
        /// The cache generation number associated with the cached value.
        /// </param>
        /// <param name="hashValue">
        /// The pre-computed hash value of the argument.  This parameter may be
        /// null.
        /// </param>
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
            object cacheValue,
            long cacheGeneration,
            byte[] hashValue
            )
            : this(flags, name, value, NoString, @default, fileName,
                   startLine, endLine, viaSource, cacheValue, cacheGeneration,
                   hashValue)
        {
            // do nothing.
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument with the specified flags, name, value, cached
        /// string representation, default value, script location, and caching
        /// metadata.  This is the primary constructor.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value wrapped by the argument.  This parameter may be null.
        /// </param>
        /// <param name="string">
        /// The cached string representation of the value.  This parameter may
        /// be null.
        /// </param>
        /// <param name="default">
        /// The default value of the argument.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the script file the argument originated from.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The starting script line number of the argument.
        /// </param>
        /// <param name="endLine">
        /// The ending script line number of the argument.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the argument originated via the [source] command.
        /// </param>
        /// <param name="cacheValue">
        /// The opaque cached value associated with the argument.  This
        /// parameter may be null.
        /// </param>
        /// <param name="cacheGeneration">
        /// The cache generation number associated with the cached value.
        /// </param>
        /// <param name="hashValue">
        /// The pre-computed hash value of the argument.  This parameter may be
        /// null.
        /// </param>
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
            object cacheValue,
            long cacheGeneration,
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
            this.cacheValue = cacheValue;
            this.cacheGeneration = cacheGeneration;
            this.hashValue = hashValue;
            this.engineData = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument with the specified flags, name, value, and
        /// default value.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value wrapped by the argument.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The default value of the argument.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name,
            object value,
            object @default
            )
            : this(flags, name, value, @default, NoFileName, NoLine, NoLine,
                   NoViaSource, NoCacheValue, NoCacheGeneration, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument with the specified flags, name, and value.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value wrapped by the argument.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name,
            object value
            )
            : this(flags, name, value, NoDefault, NoFileName, NoLine, NoLine,
                   NoViaSource, NoCacheValue, NoCacheGeneration, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument with the specified flags and name.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Argument(
            ArgumentFlags flags,
            string name
            )
            : this(flags, name, NoValue, NoDefault, NoFileName, NoLine, NoLine,
                   NoViaSource, NoCacheValue, NoCacheGeneration, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the StringOps.GetArgumentFromObject method and
        //       this class only.
        //
        /// <summary>
        /// Constructs an argument wrapping the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Argument(
            object value
            )
            : this(NoFlags, NoName, value, NoDefault, NoFileName, NoLine,
                   NoLine, NoViaSource, NoCacheValue, NoCacheGeneration,
                   NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument by copying the specified argument.
        /// </summary>
        /// <param name="value">
        /// The argument to copy.  This parameter may be null, in which case the
        /// well-known sentinel values are used.
        /// </param>
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
                   (value != null) ? value.CacheValue : NoCacheValue,
                   (value != null) ? value.CacheGeneration : NoCacheGeneration,
                   (value != null) ? value.HashValue : NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument wrapping the specified interpreter.
        /// </summary>
        /// <param name="value">
        /// The interpreter to wrap.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        private Argument(
            Interpreter value
            )
            : this(NoFlags, NoName, value, NoDefault, NoFileName, NoLine,
                   NoLine, NoViaSource, NoCacheValue, NoCacheGeneration,
                   NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument wrapping the value obtained from the
        /// specified value container.
        /// </summary>
        /// <param name="value">
        /// The value container whose value is wrapped.  This parameter may be
        /// null, in which case a null value is wrapped.
        /// </param>
        [DebuggerStepThrough()]
        private Argument(
            IGetValue value
            )
            : this(NoFlags, NoName, (value != null) ? value.Value : null,
                   NoDefault, NoFileName, NoLine, NoLine, NoViaSource,
                   NoCacheValue, NoCacheGeneration, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument wrapping the value obtained from the
        /// specified result.
        /// </summary>
        /// <param name="value">
        /// The result whose value is wrapped.  This parameter may be null, in
        /// which case a null value is wrapped.
        /// </param>
        [DebuggerStepThrough()]
        private Argument(
            Result value
            )
            : this(NoFlags, NoName, (value != null) ? value.Value : null,
                   NoDefault, NoFileName, NoLine, NoLine, NoViaSource,
                   NoCacheValue, NoCacheGeneration, NoHashValue)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an argument wrapping the value obtained from the
        /// specified result, together with the specified script location.
        /// </summary>
        /// <param name="value">
        /// The result whose value is wrapped.  This parameter may be null, in
        /// which case a null value is wrapped.
        /// </param>
        /// <param name="fileName">
        /// The name of the script file the argument originated from.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The starting script line number of the argument.
        /// </param>
        /// <param name="endLine">
        /// The ending script line number of the argument.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the argument originated via the [source] command.
        /// </param>
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
                   NoCacheValue, NoCacheGeneration, NoHashValue)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method adds the specified flags to the flags of this argument.
        /// </summary>
        /// <param name="flags">
        /// The flags to add.
        /// </param>
        [DebuggerStepThrough()]
        private void SetFlags(
            ArgumentFlags flags
            )
        {
            this.flags |= flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified flags from the flags of this
        /// argument.
        /// </summary>
        /// <param name="flags">
        /// The flags to remove.
        /// </param>
        [DebuggerStepThrough()]
        private void UnsetFlags(
            ArgumentFlags flags
            )
        {
            this.flags &= ~flags;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reset Helper Methods
        /// <summary>
        /// This method resets the wrapped value of this argument, optionally
        /// zeroing any sensitive string data beforehand.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to determine whether string data should be
        /// zeroed.  This parameter may be null.
        /// </param>
        /// <param name="zero">
        /// Non-zero to zero any sensitive string data before discarding it.
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

#if CACHE_ARGUMENT_TOSTRING
#if !MONO && NATIVE && WINDOWS
            if (zero && (@string != null) && (interpreter != null) &&
                interpreter.HasZeroString())
            {
                /* IGNORED */
                StringOps.ZeroStringOrTrace(@string);
            }
#endif

            InvalidateCachedString(true);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this argument to a well-known state, then
        /// optionally wraps the specified value.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling how the argument is reset.
        /// </param>
        /// <param name="value">
        /// The value to wrap after the reset.  This parameter may be null.
        /// </param>
        [DebuggerStepThrough()]
        internal void Reset(
            ArgumentFlags flags,
            object value
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

                this.cacheValue = null;
                this.cacheGeneration = 0;
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

                this.cacheValue = NoCacheValue;
                this.cacheGeneration = NoCacheGeneration;
                this.hashValue = NoHashValue;
            }

            this.engineData = null;

            if (value != null)
                this.value = GetValue(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static String Helpers
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method compares the string forms of two arguments using the
        /// specified comparison type.
        /// </summary>
        /// <param name="argument1">
        /// The first argument to compare.  This parameter may be null.
        /// </param>
        /// <param name="argument2">
        /// The second argument to compare.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// Zero if the arguments are equal, a negative number if the first
        /// sorts before the second, or a positive number otherwise.
        /// </returns>
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
        /// <summary>
        /// This method gets the wrapped value of the specified argument.
        /// </summary>
        /// <param name="argument">
        /// The argument whose value is returned.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped value of the specified argument, or null if the argument
        /// is null.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified value should be treated
        /// as read-only, unwrapping any value containers as necessary.
        /// </summary>
        /// <param name="value">
        /// The value to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the value should be treated as read-only; otherwise,
        /// zero.
        /// </returns>
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

        /// <summary>
        /// This method gets the underlying value of the specified object,
        /// unwrapping it when it is a value container.
        /// </summary>
        /// <param name="value">
        /// The value to unwrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The unwrapped value, or the original value when it is not a value
        /// container.
        /// </returns>
        [DebuggerStepThrough()]
        private static object GetValue(
            object value
            )
        {
            IGetValue getValue = value as IGetValue;

            if (getValue != null)
                return getValue.Value;

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
        //          Interpreter.ClearArgumentCache
        //          Interpreter.GetOrCreateCacheArgument
        //
        /// <summary>
        /// This method creates a new, empty argument wrapping a null value.
        /// </summary>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate()
        {
            return new Argument((object)null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new argument wrapping the specified argument
        /// list.
        /// </summary>
        /// <param name="value">
        /// The argument list to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            ArgumentList value
            )
        {
            return new Argument((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new argument wrapping the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument PrivateCreate(
            object value
            )
        {
            return new Argument(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new argument by copying the specified
        /// argument.
        /// </summary>
        /// <param name="value">
        /// The argument to copy.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument PrivateCreate(
            Argument value
            )
        {
            return new Argument(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new argument wrapping the specified
        /// interpreter.
        /// </summary>
        /// <param name="value">
        /// The interpreter to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument PrivateCreate(
            Interpreter value
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
        /// <summary>
        /// This method creates a new argument wrapping the value obtained from
        /// the specified value container.
        /// </summary>
        /// <param name="value">
        /// The value container whose value is wrapped.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            IGetValue value
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
        /// <summary>
        /// This method creates a new argument wrapping the value obtained from
        /// the specified result.
        /// </summary>
        /// <param name="value">
        /// The result whose value is wrapped.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument InternalCreate(
            Result value
            )
        {
            return new Argument(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new argument with the specified flags and
        /// name.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
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

        /// <summary>
        /// This method creates a new argument with the specified flags, name,
        /// and value.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value wrapped by the argument.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
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

        /// <summary>
        /// This method creates a new argument with the specified flags, name,
        /// value, and default value.
        /// </summary>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value wrapped by the argument.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The default value of the argument.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
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

#if DEBUGGER && DEBUGGER_BREAKPOINTS
        /// <summary>
        /// This method creates a new argument wrapping the value obtained from
        /// the specified result, together with the specified script location.
        /// </summary>
        /// <param name="value">
        /// The result whose value is wrapped.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the script file the argument originated from.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The starting script line number of the argument.
        /// </param>
        /// <param name="endLine">
        /// The ending script line number of the argument.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the argument originated via the [source] command.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
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
        /// <summary>
        /// This method returns a cached argument matching the specified value
        /// container when possible, creating and caching a new argument
        /// otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter providing the argument cache.  This parameter may be
        /// null.
        /// </param>
        /// <param name="getValue">
        /// The value container whose value is wrapped.  This parameter may be
        /// null.
        /// </param>
        /// <param name="createOnly">
        /// Non-zero to bypass the cache and always create a new argument.
        /// </param>
        /// <returns>
        /// The cached or newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument GetOrCreate(
            Interpreter interpreter,
            IGetValue getValue,
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
                    argument.Reset(ArgumentFlags.ResetWithDefault, getValue);

                    if (interpreter.GetCachedArgument(ref argument))
                        return argument;

#if LIST_CACHE
                    if (IsReadOnly(getValue))
#endif
                    {
                        argument = InternalCreate(getValue);

                        interpreter.AddCachedArgument(argument);

                        return argument;
                    }
                }
            }
#endif

            return InternalCreate(getValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a cached argument matching the specified result
        /// when possible, creating and caching a new argument otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter providing the argument cache.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The result whose value is wrapped.  This parameter may be null.
        /// </param>
        /// <param name="createOnly">
        /// Non-zero to bypass the cache and always create a new argument.
        /// </param>
        /// <returns>
        /// The cached or newly created argument.
        /// </returns>
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
                    argument.Reset(ArgumentFlags.ResetWithDefault, value);

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

        /// <summary>
        /// This method returns a cached argument matching the specified flags,
        /// name, and value when possible, creating and caching a new argument
        /// otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter providing the argument cache.  This parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags describing the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value wrapped by the argument.  This parameter may be null.
        /// </param>
        /// <param name="createOnly">
        /// Non-zero to bypass the cache and always create a new argument.
        /// </param>
        /// <returns>
        /// The cached or newly created argument.
        /// </returns>
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
                    argument.Reset(ArgumentFlags.ResetWithDefault, value);
                    argument.flags = flags;
                    argument.name = name;

                    object localValue = argument.value;

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

#if DEBUGGER && DEBUGGER_BREAKPOINTS
        /// <summary>
        /// This method returns a cached argument matching the specified result
        /// and script location when possible, creating and caching a new
        /// argument otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter providing the argument cache.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The result whose value is wrapped.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the script file the argument originated from.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startLine">
        /// The starting script line number of the argument.
        /// </param>
        /// <param name="endLine">
        /// The ending script line number of the argument.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero if the argument originated via the [source] command.
        /// </param>
        /// <param name="createOnly">
        /// Non-zero to bypass the cache and always create a new argument.
        /// </param>
        /// <returns>
        /// The cached or newly created argument.
        /// </returns>
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
                    argument.Reset(ArgumentFlags.ResetWithDefault, value);
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
        /// <summary>
        /// This method computes the length of the string form of the specified
        /// value, optionally using the cached string of the specified argument.
        /// </summary>
        /// <param name="argument">
        /// The argument whose cached string may be used.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The value whose string length is computed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The length to return when the value is null.
        /// </param>
        /// <returns>
        /// The length of the string form of the value, or the specified default
        /// when the value is null.
        /// </returns>
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

        /// <summary>
        /// This method computes the string form of the specified value,
        /// optionally using the cached string of the specified argument.
        /// </summary>
        /// <param name="argument">
        /// The argument whose cached string may be used.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The value whose string form is computed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The string to return when the value is null.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The string form of the value, or the specified default when the
        /// value is null.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified type is directly
        /// supported for wrapping by an argument.
        /// </summary>
        /// <param name="type">
        /// The type to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if the type is supported; otherwise, zero.
        /// </returns>
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
        /// This method creates an argument from the specified object,
        /// optionally copying an existing argument, restricting to supported
        /// types, and falling back to a string representation.
        /// </summary>
        /// <param name="value">
        /// The object to wrap.  This parameter may be null, in which case null
        /// is returned.
        /// </param>
        /// <param name="forceCopy">
        /// Non-zero to force a copy when the value is already an argument.
        /// </param>
        /// <param name="supportedOnly">
        /// Non-zero to wrap the value only when its type is supported.
        /// </param>
        /// <param name="toString">
        /// Non-zero to fall back to a string representation when the value is
        /// not otherwise supported.
        /// </param>
        /// <returns>
        /// The newly created or existing argument, or null if one could not be
        /// produced.
        /// </returns>
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

        /// <summary>
        /// This method creates an argument wrapping the specified object value.
        /// </summary>
        /// <param name="value">
        /// The object value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromIObject(
            IObject value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified interpreter.
        /// </summary>
        /// <param name="value">
        /// The interpreter to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromInterpreter(
            Interpreter value
            )
        {
            return PrivateCreate(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the value obtained from the
        /// specified result.
        /// </summary>
        /// <param name="value">
        /// The result whose value is wrapped.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromResult(
            Result value
            )
        {
            return InternalCreate(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified double value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromDouble(
            double value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified decimal value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromDecimal(
            decimal value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified enumerated value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromEnum(
            Enum value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified exception.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromException(
            Exception value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified version.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromVersion(
            Version value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified result list.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromResultList(
            ResultList value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified object dictionary.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromObjectDictionary(
            ObjectDictionary value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified string builder.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromStringBuilder(
            StringBuilder value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method creates an argument wrapping the specified big integer value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromBigInteger(
            BigInteger value
            )
        {
            return PrivateCreate((object)value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified wide integer value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromWideInteger(
            long value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified integer value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromInteger(
            int value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified boolean value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromBoolean(
            bool value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified character value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromCharacter(
            char value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the string formed by
        /// concatenating the specified characters.
        /// </summary>
        /// <param name="value1">
        /// The first character.  This parameter may be null.
        /// </param>
        /// <param name="value2">
        /// The second character.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
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

        /// <summary>
        /// This method creates an argument wrapping the specified date and time value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromDateTime(
            DateTime value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified time span value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromTimeSpan(
            TimeSpan value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified globally unique identifier value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromGuid(
            Guid value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified URI.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromUri(
            Uri value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified string value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromString(
            string value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified byte value.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromByte(
            byte value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified byte array.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromByteArray(
            byte[] value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified byte list.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        [DebuggerStepThrough()]
        private static Argument FromByteList(
            ByteList value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the specified string list.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created argument.
        /// </returns>
        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Argument FromList(
            IStringList value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a result wrapping the specified dictionary.
        /// </summary>
        /// <param name="value">
        /// The dictionary to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A result wrapping the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        private static Result FromDictionary(
            IDictionary value
            )
        {
            return PrivateCreate((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an argument wrapping the result obtained from
        /// the specified command builder.
        /// </summary>
        /// <param name="value">
        /// The command builder whose result is wrapped.  This parameter may be
        /// null, in which case null is returned.
        /// </param>
        /// <returns>
        /// The newly created argument, or null if the command builder is null.
        /// </returns>
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

        /// <summary>
        /// This method computes the string form of the specified argument,
        /// honoring its flags.
        /// </summary>
        /// <param name="argument">
        /// The argument whose string form is computed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The string to return when the argument is null.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The string form of the argument, or the specified default when the
        /// argument is null.
        /// </returns>
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
        /// <summary>
        /// This operator implicitly converts the specified
        /// <see cref="Argument" /> into a <c>string</c>.
        /// </summary>
        /// <param name="argument">
        /// The argument to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The string form of the specified argument.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator string(
            Argument argument
            )
        {
            return ToString(argument, UseEmptyForNull ? String.Empty : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Interpreter</c>
        /// into an <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The interpreter to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified interpreter,
        /// or null if it is null.
        /// </returns>
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

        /// <summary>
        /// This operator implicitly converts the specified <c>Result</c> into
        /// an <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The result to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified result, or
        /// null if it is null.
        /// </returns>
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

        /// <summary>
        /// This operator implicitly converts the specified <c>StringList</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The string list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>StringPairList</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The string pair list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringPairList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>StringDictionary</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The dictionary to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>ClientDataDictionary</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The dictionary to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            ClientDataDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>DateTime</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The date and time to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified date and time.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            DateTime value
            )
        {
            return FromDateTime(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>TimeSpan</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The time span to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified time span.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            TimeSpan value
            )
        {
            return FromTimeSpan(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Guid</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The identifier to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified identifier.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Guid value
            )
        {
            return FromGuid(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Uri</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The URI to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified URI.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Uri value
            )
        {
            return FromUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>string</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The string to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified string.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            string value
            )
        {
            return FromString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>byte</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            byte value
            )
        {
            return FromByte(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>byte[]</c> array into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The array to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified array.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            byte[] value
            )
        {
            return FromByteArray(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>ByteList</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            ByteList value
            )
        {
            return FromByteList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>char</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The character to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified character.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            char value
            )
        {
            return FromCharacter(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>double</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            double value
            )
        {
            return FromDouble(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>decimal</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            decimal value
            )
        {
            return FromDecimal(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Enum</c> value into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Enum value
            )
        {
            return FromEnum(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Exception</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The exception to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified exception.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Exception value
            )
        {
            return FromException(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>Version</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The version to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified version.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            Version value
            )
        {
            return FromVersion(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>ResultList</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The list to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified list.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            ResultList value
            )
        {
            return FromResultList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>ObjectDictionary</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The dictionary to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified dictionary.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            ObjectDictionary value
            )
        {
            return FromObjectDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>StringBuilder</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            StringBuilder value
            )
        {
            return FromStringBuilder(value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This operator implicitly converts the specified <c>BigInteger</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            BigInteger value
            )
        {
            return FromBigInteger(value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified wide integer (a
        /// <c>long</c>) into an <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            long value
            )
        {
            return FromWideInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>int</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
        [DebuggerStepThrough()]
        public static implicit operator Argument(
            int value
            )
        {
            return FromInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified <c>bool</c> into an
        /// <see cref="Argument" />.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// A new <see cref="Argument" /> representing the specified value.
        /// </returns>
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
        /// <summary>
        /// This method returns the index of the first occurrence of the
        /// specified substring within the string form of this argument.
        /// </summary>
        /// <param name="value">
        /// The substring to locate.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence, or -1 if not found.
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
        /// specified substring within the string form of this argument,
        /// starting at the specified index.
        /// </summary>
        /// <param name="value">
        /// The substring to locate.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin the search.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence, or -1 if not found.
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
        /// specified substring within the string form of this argument.
        /// </summary>
        /// <param name="value">
        /// The substring to locate.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence, or -1 if not found.
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
        /// specified substring within the string form of this argument,
        /// starting at the specified index.
        /// </summary>
        /// <param name="value">
        /// The substring to locate.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin the backward search.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence, or -1 if not found.
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
        /// This method determines whether the string form of this argument
        /// begins with the specified substring.
        /// </summary>
        /// <param name="value">
        /// The substring to look for.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// True if the string form begins with the substring; otherwise,
        /// false.
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
        /// This method determines whether the string form of this argument
        /// ends with the specified substring.
        /// </summary>
        /// <param name="value">
        /// The substring to look for.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// True if the string form ends with the substring; otherwise, false.
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
        /// This method returns the substring of the string form of this
        /// argument beginning at the specified index.
        /// </summary>
        /// <param name="startIndex">
        /// The zero-based starting index of the substring.
        /// </param>
        /// <returns>
        /// The requested substring.
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
        /// This method returns the substring of the string form of this
        /// argument beginning at the specified index and having the specified
        /// length.
        /// </summary>
        /// <param name="startIndex">
        /// The zero-based starting index of the substring.
        /// </param>
        /// <param name="length">
        /// The number of characters in the substring.
        /// </param>
        /// <returns>
        /// The requested substring.
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
        /// This method compares the string form of this argument with the
        /// specified string using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to compare against.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// Zero if the values are equal, a negative number if this argument
        /// sorts before the value, or a positive number otherwise.
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
        /// This method compares the string form of this argument with the
        /// string form of the specified argument using the specified
        /// comparison type.
        /// </summary>
        /// <param name="value">
        /// The argument to compare against.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// Zero if the values are equal, a negative number if this argument
        /// sorts before the value, or a positive number otherwise.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the string form of this argument
        /// contains the specified substring.
        /// </summary>
        /// <param name="value">
        /// The substring to look for.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to perform.
        /// </param>
        /// <returns>
        /// True if the substring is found; otherwise, false.
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
        /// This method returns a copy of the string form of this argument with
        /// all occurrences of one substring replaced by another.
        /// </summary>
        /// <param name="oldValue">
        /// The substring to be replaced.
        /// </param>
        /// <param name="newValue">
        /// The substring to substitute for each occurrence.
        /// </param>
        /// <returns>
        /// The resulting string after the replacements.
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
        /// This method removes all leading and trailing white-space characters
        /// from the string form of this argument.
        /// </summary>
        /// <returns>
        /// The trimmed string.
        /// </returns>
        [DebuggerStepThrough()]
        public string Trim()
        {
            return ToString(this, String.Empty).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all leading and trailing occurrences of the
        /// specified characters from the string form of this argument.
        /// </summary>
        /// <param name="trimChars">
        /// The characters to remove.  This parameter may be null to trim
        /// white-space.
        /// </param>
        /// <returns>
        /// The trimmed string.
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
        /// This method removes all leading occurrences of the specified
        /// characters from the string form of this argument.
        /// </summary>
        /// <param name="trimChars">
        /// The characters to remove.  This parameter may be null to trim
        /// white-space.
        /// </param>
        /// <returns>
        /// The trimmed string.
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
        /// This method removes all trailing occurrences of the specified
        /// characters from the string form of this argument.
        /// </summary>
        /// <param name="trimChars">
        /// The characters to remove.  This parameter may be null to trim
        /// white-space.
        /// </param>
        /// <returns>
        /// The trimmed string.
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
        /// This method copies the characters of the string form of this
        /// argument into a character array.
        /// </summary>
        /// <returns>
        /// The character array containing the characters.
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
        /// This method returns the string form of this argument, honoring the
        /// specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling how the string form is produced.
        /// </param>
        /// <returns>
        /// The string form of this argument.
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
        /// This method returns the string form of this argument, honoring the
        /// specified flags and falling back to the specified default when no
        /// value is available.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling how the string form is produced.
        /// </param>
        /// <param name="default">
        /// The string to return when no value is available.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The string form of this argument, or the specified default.
        /// </returns>
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
                        argumentFlags, ArgumentFlags.List, true))
                {
                    if (FlagOps.HasFlags(
                            argumentFlags, ArgumentFlags.WithName,
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
                            argumentFlags, ArgumentFlags.WithName,
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
                    argumentFlags, ArgumentFlags.WithName, true))
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

        /// <summary>
        /// This method returns the string form of this argument formatted using
        /// the specified composite format string, with the name and value as
        /// arguments.
        /// </summary>
        /// <param name="format">
        /// The composite format string.
        /// </param>
        /// <returns>
        /// The formatted string.
        /// </returns>
        [DebuggerStepThrough()]
        public string ToString(
            string format
            )
        {
            return String.Format(format, name, ToString(this, null));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this argument formatted using
        /// the specified composite format string, with the name and value as
        /// arguments, then truncated to the specified length.
        /// </summary>
        /// <param name="format">
        /// The composite format string.
        /// </param>
        /// <param name="limit">
        /// The maximum length of the resulting string, or a non-positive value
        /// for no limit.
        /// </param>
        /// <param name="strict">
        /// Non-zero to enforce the length limit strictly.
        /// </param>
        /// <returns>
        /// The formatted, possibly truncated string.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified object is an argument
        /// equal to this argument, comparing flags, script location, value,
        /// default value, name, and file name.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this argument.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the specified object is an equal argument; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method computes a hash code for this argument based on its
        /// flags, script location, value, default value, name, and file name.
        /// </summary>
        /// <returns>
        /// The computed hash code.
        /// </returns>
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

        /// <summary>
        /// This method returns the default string form of this argument.
        /// </summary>
        /// <returns>
        /// The string form of this argument.
        /// </returns>
        [DebuggerStepThrough()]
        public override string ToString()
        {
            return ToString(ToStringFlags.None, String.Empty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this argument, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this argument.
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
        /// The value data associated with this argument, if any.
        /// </summary>
        private IClientData valueData;
        /// <summary>
        /// Gets or sets the value data associated with this argument.
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
        /// The extra data associated with this argument, if any.
        /// </summary>
        private IClientData extraData;
        /// <summary>
        /// Gets or sets the extra data associated with this argument.
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
        /// The call frame associated with this argument, if any.
        /// </summary>
        private ICallFrame callFrame;
        /// <summary>
        /// Gets or sets the call frame associated with this argument.
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

        #region IGetValue Members
        /// <summary>
        /// The value wrapped by this argument, if any.
        /// </summary>
        private object value;
        /// <summary>
        /// Gets the value wrapped by this argument.
        /// </summary>
        public object Value
        {
            [DebuggerStepThrough()]
            get { return value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the string form of the value wrapped by this argument.
        /// </summary>
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

        /// <summary>
        /// Gets the length of the string form of the value wrapped by this
        /// argument.
        /// </summary>
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
        /// <summary>
        /// This method invalidates the cached string representation of this
        /// argument.
        /// </summary>
        /// <param name="zero">
        /// Non-zero to clear the cached string to null; otherwise, the
        /// no-string sentinel is used.
        /// </param>
        [DebuggerStepThrough()]
        internal void InvalidateCachedString(
            bool zero
            )
        {
            @string = zero ? null : NoString;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached string representation of the value, if any.
        /// </summary>
        private string @string; /* CACHE */
        /// <summary>
        /// Gets the cached string representation of this argument.
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

        #region IArgument Members
        /// <summary>
        /// The name of this argument, if any.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets the name of this argument.  Setting this property is not
        /// supported and always throws <see cref="NotSupportedException" />.
        /// </summary>
        public string Name
        {
            [DebuggerStepThrough()]
            get { return name; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags describing this argument.
        /// </summary>
        private ArgumentFlags flags;
        /// <summary>
        /// Gets the flags describing this argument.  Setting this property is
        /// not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public ArgumentFlags Flags
        {
            [DebuggerStepThrough()]
            get { return flags; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default value of this argument, if any.
        /// </summary>
        private object @default;
        /// <summary>
        /// Gets the default value of this argument.  Setting this property is
        /// not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public object Default
        {
            [DebuggerStepThrough()]
            get { return @default; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this argument to a well-known state.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling how the argument is reset.
        /// </param>
        [DebuggerStepThrough()]
        public void Reset(
            ArgumentFlags flags
            )
        {
            Reset(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this argument has the specified
        /// flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to look for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require all of the specified flags to be present;
        /// otherwise, any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// The name of the script file this argument originated from, if any.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets the name of the script file this argument originated from.
        /// Setting this property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public string FileName
        {
            [DebuggerStepThrough()]
            get { return fileName; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The starting script line number of this argument.
        /// </summary>
        private int startLine;
        /// <summary>
        /// Gets the starting script line number of this argument.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public int StartLine
        {
            [DebuggerStepThrough()]
            get { return startLine; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The ending script line number of this argument.
        /// </summary>
        private int endLine;
        /// <summary>
        /// Gets the ending script line number of this argument.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public int EndLine
        {
            [DebuggerStepThrough()]
            get { return endLine; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if this argument originated via the [source] command.
        /// </summary>
        private bool viaSource;
        /// <summary>
        /// Gets a value indicating whether this argument originated via the
        /// [source] command.  Setting this property is not supported and always
        /// throws <see cref="NotSupportedException" />.
        /// </summary>
        public bool ViaSource
        {
            [DebuggerStepThrough()]
            get { return viaSource; }
            [DebuggerStepThrough()]
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs describing this
        /// argument.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs describing this argument.
        /// </returns>
        [DebuggerStepThrough()]
        public StringPairList ToList()
        {
            return ToList(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs describing this
        /// argument, optionally scrubbing the file name of any base path.
        /// </summary>
        /// <param name="scrub">
        /// Non-zero to scrub the base path from the file name.
        /// </param>
        /// <returns>
        /// The list of name/value pairs describing this argument.
        /// </returns>
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

        #region ICacheValue Members
        //
        // WARNING: This property is for private and/or diagnostic use only.
        //
        /// <summary>
        /// The opaque cached value associated with this argument, if any.
        /// </summary>
        private object cacheValue;
        /// <summary>
        /// Gets the opaque cached value associated with this argument.
        /// </summary>
        public object CacheValue
        {
            [DebuggerStepThrough()]
            get { return cacheValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This property is for private and/or diagnostic use only.
        //
        /// <summary>
        /// The cache generation number associated with the cached value.
        /// </summary>
        private long cacheGeneration;
        /// <summary>
        /// Gets the cache generation number associated with the cached value.
        /// </summary>
        public long CacheGeneration
        {
            [DebuggerStepThrough()]
            get { return cacheGeneration; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached value associated with this argument,
        /// optionally validating the cache generation against the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose cache generation is checked.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noGeneration">
        /// Non-zero to skip validation of the cache generation.
        /// </param>
        /// <returns>
        /// The cached value, or null if it is unavailable or the generation
        /// does not match.
        /// </returns>
        [DebuggerStepThrough()]
        public object GetCacheValue(
            Interpreter interpreter,
            bool noGeneration
            )
        {
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
            if (!noGeneration && ((interpreter == null) ||
                !interpreter.MatchCacheGeneration(true, ref cacheGeneration)))
            {
                return null;
            }
#endif

            return cacheValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the cached value associated with this argument,
        /// optionally validating the cache generation against the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose cache generation is checked.  This parameter
        /// may be null.
        /// </param>
        /// <param name="value">
        /// The value to cache.  This parameter may be null.
        /// </param>
        /// <param name="noGeneration">
        /// Non-zero to skip validation of the cache generation.
        /// </param>
        /// <returns>
        /// True if the value was cached; otherwise, false.
        /// </returns>
        [DebuggerStepThrough()]
        public bool SetCacheValue(
            Interpreter interpreter,
            object value,
            bool noGeneration
            )
        {
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
            if (!noGeneration && ((interpreter == null) ||
                !interpreter.MatchCacheGeneration(false, ref cacheGeneration)))
            {
                return false;
            }
#endif

            cacheValue = value;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICanHashValue Members
        /// <summary>
        /// This method gets the hash value of this argument, computing and
        /// caching it on first use.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered while
        /// computing the hash value.
        /// </param>
        /// <returns>
        /// The hash value of this argument, or null on failure.
        /// </returns>
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

        /// <summary>
        /// The cached hash value of this argument, if any.
        /// </summary>
        private byte[] hashValue;
        /// <summary>
        /// Gets or sets the cached hash value of this argument.
        /// </summary>
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
        /// <summary>
        /// This method creates a copy of this argument.
        /// </summary>
        /// <returns>
        /// A new argument that is a copy of this argument.
        /// </returns>
        [DebuggerStepThrough()]
        public object Clone()
        {
            return PrivateCreate(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Members
        /// <summary>
        /// The engine-specific data associated with this argument, if any.
        /// </summary>
        private object engineData;
        /// <summary>
        /// Gets the engine-specific data associated with this argument.
        /// Setting this property is not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        internal object EngineData
        {
            [DebuggerStepThrough()]
            get { return engineData; }
            [DebuggerStepThrough()]
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method associates the specified arguments with the specified
        /// engine data when it provides a string builder.
        /// </summary>
        /// <param name="engineData">
        /// The engine data that may provide a string builder.  This parameter
        /// may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments to associate with the engine data.  This parameter may
        /// be null.
        /// </param>
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
        /// <summary>
        /// This method resets the wrapped value of this argument to the
        /// specified string builder.
        /// </summary>
        /// <param name="builder">
        /// The string builder to wrap.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method sets the wrapped value of this argument.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
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
