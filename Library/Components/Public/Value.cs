/*
 * Value.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

#if NET_40
using System.Numerics;
#endif

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using StringLongPair = Eagle._Interfaces.Public.IAnyPair<string, long>;
using CharBoolPair = Eagle._Components.Public.AnyPair<char?, bool>;

#if NET_40
using BigIntegerDictionary = System.Collections.Generic.Dictionary<
    string, System.Numerics.BigInteger>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    /// <summary>
    /// This static class provides Eagle's core value parsing, conversion, and
    /// inspection services.  It converts between strings and the numeric,
    /// boolean, date/time, list, index, and object value types that Eagle
    /// scripts manipulate -- for example, parsing a string into an integer,
    /// double, wide integer, big integer, boolean, or list, resolving an index
    /// expression (such as <c>end</c> or <c>end-2</c>), and mapping between
    /// .NET types and Eagle's type model.  These services back expression
    /// evaluation, command argument processing, and result handling throughout
    /// the engine, and they honor the configured culture and Eagle's numeric
    /// conventions.  Most conversion methods follow the
    /// <see cref="ReturnCode" /> plus <c>ref</c>-output pattern: they return
    /// <see cref="ReturnCode.Ok" /> and assign the parsed value on success, or
    /// <see cref="ReturnCode.Error" /> and assign an error message on failure.
    /// See <c>core_language.md</c> for the value and expression syntax.
    /// </summary>
    /* INTERNAL STATIC OK */
    [ObjectId("cd8749dc-8483-45e9-ab43-a8daef79df64")]
    public static class Value
    {
        #region Private Constants
        /// <summary>
        /// The cached string representation of the integer zero.
        /// </summary>
        internal static readonly string ZeroString = 0.ToString();
        /// <summary>
        /// The cached string representation of the integer one.
        /// </summary>
        internal static readonly string OneString = 1.ToString();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The decimal separator character used when no culture-specific
        /// separator is available.
        /// </summary>
        private static readonly string DefaultDecimalSeparator = Characters.Period.ToString();

        //
        // HACK: These characters may be present in floating point values;
        //       however, that (clearly?) does not apply to hexadecimal in
        //       the case of 'E' / 'e'.  Also, these do not appear to vary
        //       based on the current culture.
        //
        /// <summary>
        /// The set of characters whose presence in a string may indicate a
        /// floating point value (i.e. the exponent characters).
        /// </summary>
        private static readonly char[] MaybeFloatingPointChars = {
            Characters.E, Characters.e
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The error message used when an index falls outside the permitted
        /// bounds.
        /// </summary>
        private const string badIndexBoundsError = "bad index bounds";
        /// <summary>
        /// The error message format used when an index expression contains an
        /// unsupported arithmetic operator.
        /// </summary>
        private const string badIndexOperatorError = "bad index operator {0}, must be one of: +-*/%";

        /// <summary>
        /// The error message format used when a string cannot be parsed as a
        /// simple index.
        /// </summary>
        private const string badIndexError1 =
            "bad index {0}: must be start|end|count|integer";

        /// <summary>
        /// The error message format used when a string cannot be parsed as a
        /// compound (operator-based) index.
        /// </summary>
        private const string badIndexError2 =
            "bad index {0}: must be start|end|count|integer?[+-*/%]start|end|count|integer?";

        /// <summary>
        /// The reserved name representing the "none" index keyword.
        /// </summary>
        private const string noneName = "none";
        /// <summary>
        /// The reserved name representing the "start" index keyword.
        /// </summary>
        private const string startName = "start";
        /// <summary>
        /// The reserved name representing the "end" index keyword.
        /// </summary>
        private const string endName = "end";
        /// <summary>
        /// The reserved name representing the "count" index keyword.
        /// </summary>
        private const string countName = "count";

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The regular expression used to recognize a compound index
        /// expression composed of two index keywords (or integers) joined by
        /// an arithmetic operator.
        /// </summary>
        private static Regex startEndPlusMinusIndexRegEx = RegExOps.Create(
            "^(" + noneName + "|" + startName + "|" + endName + "|" +
            countName + "|\\d+){1}([\\+\\-\\*\\/\\%]{1})(" +
            noneName + "|" + startName + "|" + endName + "|" +
            countName + "|\\d+)$", RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The regular expression pattern used to match a simple embedded
        /// annotation marker within script text.
        /// </summary>
        private static string annotationsPattern1 = "#\\s+<<(\\w+)>>\\s+";
        /// <summary>
        /// The regular expression pattern used to match a name/value embedded
        /// annotation marker within script text.
        /// </summary>
        private static string annotationsPattern2 = "#\\s+<<(\\w+):(\\w+(?: \\w+)*)>>\\s+";

        /// <summary>
        /// The regular expression pattern used to match a simple annotation
        /// name.
        /// </summary>
        private static string annotationsPattern3 = "^(\\w+)$";
        /// <summary>
        /// The regular expression pattern used to match a name/value
        /// annotation.
        /// </summary>
        private static string annotationsPattern4 = "^(\\w+):(\\w+(?: \\w+)*)$";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The regular expression used to recognize a dotted version string.
        /// </summary>
        private static Regex versionRegEx = RegExOps.Create(
            "^\\d+\\.\\d+(?:\\.\\d+){0,2}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The regular expression used to recognize a globally unique
        /// identifier in any of its supported textual formats.
        /// </summary>
        private static Regex guidRegEx = RegExOps.Create(
            "^(?:[0-9A-F]{32}|[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}" +
            "|\\{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\\}|" +
            "\\([0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\\))$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The regular expression used to recognize a version range (a pair of
        /// optional versions separated by a hyphen).
        /// </summary>
        private static Regex versionRangeRegEx = RegExOps.Create(
            "^(\\d+\\.\\d+(?:\\.\\d+(?:\\.\\d+)?)?)?-(\\d+\\.\\d+(?:\\.\\d+(?:\\.\\d+)?)?)?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        // NOTE: These are the *default* number styles that are used by the
        //       associated .NET Framework classes for the method overloads
        //       that do not accept an explicit number styles parameter.
        //
        /// <summary>
        /// The default number styles used when parsing a byte value.
        /// </summary>
        private static NumberStyles byteStyles = NumberStyles.Integer;

        /// <summary>
        /// The default number styles used when parsing a narrow integer value.
        /// </summary>
        private static NumberStyles narrowIntegerStyles = NumberStyles.Integer;

        /// <summary>
        /// The default number styles used when parsing an integer value.
        /// </summary>
        private static NumberStyles integerStyles = NumberStyles.Integer;

        /// <summary>
        /// The default number styles used when parsing a wide integer value.
        /// </summary>
        private static NumberStyles wideIntegerStyles = NumberStyles.Integer;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        // NOTE: This is the *default* DateTime styles that are used by the
        //       DateTime class for the method overloads that do not accept
        //       an explicit DateTime styles parameter.
        //
        /// <summary>
        /// The default date/time styles used when parsing a date/time value.
        /// </summary>
        private static DateTimeStyles dateTimeStyles = DateTimeStyles.None;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        // BUGBUG: Apparently, the AllowThousands flag is very broken.  Using
        //         it will allow things like "1,234,5,55" and "1,234,,555" to
        //         validate as decimal / double / single.
        //
        // NOTE: These are the *default* number styles that are used by the
        //       associated .NET Framework classes for the method overloads
        //       that do not accept an explicit number styles parameter.
        //
        /// <summary>
        /// The default number styles used when parsing a fixed-point decimal
        /// value.
        /// </summary>
        private static NumberStyles decimalStyles = NumberStyles.Number;

        /// <summary>
        /// The default number styles used when parsing a single-precision
        /// floating point value.
        /// </summary>
        private static NumberStyles singleStyles =
            NumberStyles.Float | NumberStyles.AllowThousands;

        /// <summary>
        /// The default number styles used when parsing a double-precision
        /// floating point value.
        /// </summary>
        private static NumberStyles doubleStyles =
            NumberStyles.Float | NumberStyles.AllowThousands;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// The default number styles used when parsing an arbitrary-precision
        /// big integer value.
        /// </summary>
        private static NumberStyles bigIntegerStyles = NumberStyles.Integer;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the static data of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region CultureInfo / IFormatProvider Data
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The culture used by default for value parsing and formatting.
        /// </summary>
        private static CultureInfo DefaultCulture = null;
        /// <summary>
        /// The format provider used by default for numeric value parsing and
        /// formatting.
        /// </summary>
        private static IFormatProvider NumberFormatProvider = null;
        /// <summary>
        /// The format provider used by default for date/time value parsing and
        /// formatting.
        /// </summary>
        private static IFormatProvider DateTimeFormatProvider = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Named Numeric Values
        /// <summary>
        /// The lookup table mapping recognized names (such as infinity and
        /// NaN) to their single-precision floating point values.
        /// </summary>
        private static SingleDictionary namedSingles = null; // Inf, NaN, etc (float)
        /// <summary>
        /// The lookup table mapping recognized names (such as infinity and
        /// NaN) to their double-precision floating point values.
        /// </summary>
        private static DoubleDictionary namedDoubles = null; // Inf, NaN, etc (double)

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// The lookup table mapping recognized names to their
        /// arbitrary-precision big integer values.
        /// </summary>
        private static BigIntegerDictionary namedBigIntegers = null; // MinusOne, Zero, One, etc (BigInteger)
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Supported "Value" Types
        /// <summary>
        /// The list of types treated as integer-valued.
        /// </summary>
        private static TypeList integerTypes = null;
        /// <summary>
        /// The list of types treated as floating point-valued.
        /// </summary>
        private static TypeList floatTypes = null;
        /// <summary>
        /// The list of types treated as string-valued.
        /// </summary>
        private static TypeList stringTypes = null;
        /// <summary>
        /// The list of all types treated as numeric.
        /// </summary>
        private static TypeList numberTypes = null;
        /// <summary>
        /// The list of types treated as integral (whole-number) numeric.
        /// </summary>
        private static TypeList integralTypes = null;
        /// <summary>
        /// The list of types treated as non-integral numeric.
        /// </summary>
        private static TypeList nonIntegralTypes = null;
        /// <summary>
        /// The list of non-numeric value types supported by this class.
        /// </summary>
        private static TypeList otherTypes = null;
        /// <summary>
        /// The list of all value types supported by this class.
        /// </summary>
        private static TypeList allTypes = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Error Callbacks
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The callback invoked, when present, to transform a single error
        /// result before it is returned.
        /// </summary>
        private static ErrorCallback errorCallback;
        /// <summary>
        /// The callback invoked, when present, to transform a list of error
        /// results before it is returned.
        /// </summary>
        private static ErrorListCallback errorListCallback;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        /// <summary>
        /// This static constructor initializes the supported type lists and the
        /// static state used by the value services of this class.
        /// </summary>
        static Value()
        {
            NumberOps.InitializeTypes();
            VariantOps.InitializeTypes();

            Initialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Initialization Methods
        /// <summary>
        /// This method initializes the culture, supported type lists, and named
        /// numeric lookup tables used by this class.
        /// </summary>
        internal static void Initialize()
        {
            InitializeCulture();
            InitializeTypes();
            InitializeNamedNumerics();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the default culture and the associated
        /// numeric and date/time format providers, if they have not already
        /// been set.
        /// </summary>
        private static void InitializeCulture()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (DefaultCulture == null)
                    DefaultCulture = CultureInfo.InvariantCulture;

                if (DefaultCulture != null)
                {
                    if (NumberFormatProvider == null)
                        NumberFormatProvider = DefaultCulture.NumberFormat;

                    if (DateTimeFormatProvider == null)
                        DateTimeFormatProvider = DefaultCulture.DateTimeFormat;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the static type lists used when adding
        /// operators and functions, if they have not already been set.
        /// </summary>
        private static void InitializeTypes()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Initialize the static type lists used when adding
                //       operators and functions.
                //
                if (integerTypes == null)
                    integerTypes = new TypeList(new Type[] { typeof(int) });

                ///////////////////////////////////////////////////////////////

                if (floatTypes == null)
                    floatTypes = new TypeList(new Type[] { typeof(double) });

                ///////////////////////////////////////////////////////////////

                if (stringTypes == null)
                    stringTypes = new TypeList(new Type[] { typeof(string) });

                ///////////////////////////////////////////////////////////////

                if (numberTypes == null)
                {
                    numberTypes = new TypeList();

                    NumberOps.AddTypes(ref numberTypes);
                }

                ///////////////////////////////////////////////////////////////

                if (integralTypes == null)
                {
                    integralTypes = new TypeList();

                    NumberOps.AddTypes(ref integralTypes);

                    integralTypes.Remove(typeof(decimal));
                    integralTypes.Remove(typeof(float));
                    integralTypes.Remove(typeof(double));
                }

                ///////////////////////////////////////////////////////////////

                if (nonIntegralTypes == null)
                {
                    nonIntegralTypes = new TypeList();

                    NumberOps.AddTypes(ref nonIntegralTypes);

                    nonIntegralTypes.Remove(typeof(bool));
                    nonIntegralTypes.Remove(typeof(sbyte));
                    nonIntegralTypes.Remove(typeof(byte));
                    nonIntegralTypes.Remove(typeof(short));
                    nonIntegralTypes.Remove(typeof(ushort));
                    nonIntegralTypes.Remove(typeof(char));
                    nonIntegralTypes.Remove(typeof(int));
                    nonIntegralTypes.Remove(typeof(uint));
                    nonIntegralTypes.Remove(typeof(long));
                    nonIntegralTypes.Remove(typeof(ulong));

#if NET_40
                    nonIntegralTypes.Remove(typeof(BigInteger));
#endif

                    nonIntegralTypes.Remove(typeof(Enum));
                    nonIntegralTypes.Remove(typeof(ReturnCode));
                    nonIntegralTypes.Remove(typeof(MatchMode));
                    nonIntegralTypes.Remove(typeof(MidpointRounding));
                }

                ///////////////////////////////////////////////////////////////

                if (otherTypes == null)
                {
                    otherTypes = new TypeList();

                    VariantOps.AddTypes(ref otherTypes);

                    otherTypes.Remove(typeof(INumber));
                }

                ///////////////////////////////////////////////////////////////

                if (allTypes == null)
                {
                    allTypes = new TypeList(numberTypes);

                    if (otherTypes != null)
                        allTypes.AddRange(otherTypes);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the lookup tables of named single, double,
        /// and big integer values understood by the expression parser, if they
        /// have not already been set.
        /// </summary>
        private static void InitializeNamedNumerics()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Initialize the lookup tables of named single and
                //       double values understood by the expression parser
                //       ("+Inf", "-Inf", "NaN", etc).
                //
                if (namedSingles == null)
                {
                    namedSingles = new SingleDictionary(
                        new _Comparers.StringCustom(
                            SharedStringOps.GetSystemComparisonType(true)));

                    namedSingles.Add(
                        TclVars.Expression.Infinity, float.PositiveInfinity);

                    namedSingles.Add(
                        Characters.PlusSign + TclVars.Expression.Infinity,
                        float.PositiveInfinity);

                    namedSingles.Add(
                        Characters.MinusSign + TclVars.Expression.Infinity,
                        float.NegativeInfinity);

                    namedSingles.Add(float.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        float.PositiveInfinity);

                    namedSingles.Add(float.NegativeInfinity.ToString(
                            GetDefaultCulture()),
                        float.NegativeInfinity);

                    namedSingles.Add(Characters.PlusSign +
                        float.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        float.PositiveInfinity);

                    namedSingles[Vars.Expression.Infinity] =
                        float.PositiveInfinity;

                    namedSingles[Characters.PlusSign +
                        Vars.Expression.Infinity] = float.PositiveInfinity;

                    namedSingles[Characters.MinusSign +
                        Vars.Expression.Infinity] = float.NegativeInfinity;

                    namedSingles.Add(TclVars.Expression.NaN, float.NaN);
                    namedSingles.Add(Vars.Expression.Pi, (float)Math.PI);
                    namedSingles.Add(Vars.Expression.Euler, (float)Math.E);
                }

                ///////////////////////////////////////////////////////////////

                if (namedDoubles == null)
                {
                    namedDoubles = new DoubleDictionary(
                        new _Comparers.StringCustom(
                            SharedStringOps.GetSystemComparisonType(true)));

                    namedDoubles.Add(
                        TclVars.Expression.Infinity, double.PositiveInfinity);

                    namedDoubles.Add(
                        Characters.PlusSign + TclVars.Expression.Infinity,
                        double.PositiveInfinity);

                    namedDoubles.Add(
                        Characters.MinusSign + TclVars.Expression.Infinity,
                        double.NegativeInfinity);

                    namedDoubles.Add(double.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        double.PositiveInfinity);

                    namedDoubles.Add(double.NegativeInfinity.ToString(
                            GetDefaultCulture()),
                        double.NegativeInfinity);

                    namedDoubles.Add(Characters.PlusSign +
                        double.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        double.PositiveInfinity);

                    namedDoubles[Vars.Expression.Infinity] =
                        double.PositiveInfinity;

                    namedDoubles[Characters.PlusSign +
                        Vars.Expression.Infinity] = double.PositiveInfinity;

                    namedDoubles[Characters.MinusSign +
                        Vars.Expression.Infinity] = double.NegativeInfinity;

                    namedDoubles.Add(TclVars.Expression.NaN, double.NaN);
                    namedDoubles.Add(Vars.Expression.Pi, Math.PI);
                    namedDoubles.Add(Vars.Expression.Euler, Math.E);
                }

                ///////////////////////////////////////////////////////////////

#if NET_40
                if (namedBigIntegers == null)
                {
                    namedBigIntegers = new BigIntegerDictionary(
                        new _Comparers.StringCustom(
                            SharedStringOps.GetSystemComparisonType(true)));

                    namedBigIntegers.Add("BigMinusOne", BigInteger.MinusOne);
                    namedBigIntegers.Add("BigZero", BigInteger.Zero);
                    namedBigIntegers.Add("BigOne", BigInteger.One);
                }
#endif
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Error Callback Helper Methods
        /// <summary>
        /// This method invokes the configured single-error callback, if any, to
        /// transform the specified error result.
        /// </summary>
        /// <param name="error">
        /// The error result to transform.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The transformed error result, or the original error result when no
        /// callback is configured.
        /// </returns>
        private static Result MaybeInvokeErrorCallback(
            Result error /* in */
            )
        {
            ErrorCallback callback;

            lock (syncRoot)
            {
                callback = errorCallback;
            }

            return (callback != null) ? callback(error) : error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the configured error-list callback, if any, to
        /// transform the specified list of error results.
        /// </summary>
        /// <param name="errors">
        /// The list of error results to transform.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The transformed list of error results, or the original list when no
        /// callback is configured.
        /// </returns>
        private static ResultList MaybeInvokeErrorCallback(
            ResultList errors /* in */
            )
        {
            ErrorListCallback callback;

            lock (syncRoot)
            {
                callback = errorListCallback;
            }

            return (callback != null) ? callback(errors) : errors;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the currently configured single-error callback.
        /// </summary>
        /// <returns>
        /// The currently configured single-error callback, or null if none is
        /// configured.
        /// </returns>
        internal static ErrorCallback GetErrorCallback()
        {
            lock (syncRoot)
            {
                return errorCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the currently configured error-list callback.
        /// </summary>
        /// <returns>
        /// The currently configured error-list callback, or null if none is
        /// configured.
        /// </returns>
        internal static ErrorListCallback GetErrorListCallback()
        {
            lock (syncRoot)
            {
                return errorListCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the single-error callback used to transform error
        /// results.
        /// </summary>
        /// <param name="callback">
        /// The single-error callback to use, or null to remove any existing
        /// callback.
        /// </param>
        internal static void SetErrorCallback(
            ErrorCallback callback /* in */
            )
        {
            lock (syncRoot)
            {
                errorCallback = callback;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the error-list callback used to transform lists of
        /// error results.
        /// </summary>
        /// <param name="callback">
        /// The error-list callback to use, or null to remove any existing
        /// callback.
        /// </param>
        internal static void SetErrorListCallback(
            ErrorListCallback callback /* in */
            )
        {
            lock (syncRoot)
            {
                errorListCallback = callback;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Culture Helper Methods
        /// <summary>
        /// This method returns the default culture used for value parsing and
        /// formatting, initializing it if necessary.
        /// </summary>
        /// <returns>
        /// The default culture, which is never null.
        /// </returns>
        internal static CultureInfo GetDefaultCulture() /* CANNOT RETURN NULL */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeCulture();

                if (DefaultCulture != null)
                    return DefaultCulture;

                return CultureInfo.InvariantCulture;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default format provider used for numeric
        /// value parsing and formatting, initializing the culture if necessary.
        /// </summary>
        /// <returns>
        /// The default numeric format provider, which may be null.
        /// </returns>
        private static IFormatProvider GetNumberFormatProvider() /* MAY RETURN NULL */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeCulture();

                return NumberFormatProvider;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the numeric format provider associated with the
        /// specified culture, falling back to the default numeric format
        /// provider.
        /// </summary>
        /// <param name="cultureInfo">
        /// The culture whose numeric format provider is returned.  This
        /// parameter may be null, in which case the default numeric format
        /// provider is used.
        /// </param>
        /// <returns>
        /// The numeric format provider for the specified culture, which may be
        /// null.
        /// </returns>
        internal static IFormatProvider GetNumberFormatProvider( /* MAY RETURN NULL */
            CultureInfo cultureInfo
            )
        {
            return (cultureInfo != null) ?
                cultureInfo.NumberFormat : GetNumberFormatProvider();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default format provider used for date/time
        /// value parsing and formatting, initializing the culture if necessary.
        /// </summary>
        /// <returns>
        /// The default date/time format provider, which may be null.
        /// </returns>
        internal static IFormatProvider GetDateTimeFormatProvider() /* MAY RETURN NULL */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeCulture();

                return DateTimeFormatProvider;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the date/time format provider associated with
        /// the specified culture, falling back to the default date/time format
        /// provider.
        /// </summary>
        /// <param name="cultureInfo">
        /// The culture whose date/time format provider is returned.  This
        /// parameter may be null, in which case the default date/time format
        /// provider is used.
        /// </param>
        /// <returns>
        /// The date/time format provider for the specified culture, which may
        /// be null.
        /// </returns>
        internal static IFormatProvider GetDateTimeFormatProvider( /* MAY RETURN NULL */
            CultureInfo cultureInfo
            )
        {
#if MONO || MONO_HACKS
            //
            // HACK: Sometimes Mono 6.12 will throw a NullReferenceException
            //       from DateTime.TryParseExact when it is used from within
            //       a non-default AppDomain and non-null IFormatProvider.
            //
            if (CommonOps.Runtime.IsMono() && !AppDomainOps.IsCurrentDefault())
                return null;
#endif

            return (cultureInfo != null) ?
                cultureInfo.DateTimeFormat : GetDateTimeFormatProvider();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the decimal separator string associated with the
        /// specified format provider, falling back to the default decimal
        /// separator.
        /// </summary>
        /// <param name="formatProvider">
        /// The format provider whose decimal separator is returned.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The decimal separator string for the specified format provider, or
        /// the default decimal separator when one is not available.
        /// </returns>
        private static string GetNumberDecimalSeparator( /* MAY RETURN NULL */
            IFormatProvider formatProvider
            )
        {
            NumberFormatInfo numberFormatInfo =
                formatProvider as NumberFormatInfo;

            if (numberFormatInfo != null)
                return numberFormatInfo.NumberDecimalSeparator;

            return DefaultDecimalSeparator;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Number / DateTime Styles Helper Methods
        /// <summary>
        /// This method retrieves the default number styles used when parsing a
        /// byte value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing a byte
        /// value.
        /// </param>
        private static void GetByteStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = byteStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the default number styles used when parsing a
        /// narrow integer value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing a narrow
        /// integer value.
        /// </param>
        private static void GetNarrowIntegerStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = narrowIntegerStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the default number styles used when parsing an
        /// integer value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing an integer
        /// value.
        /// </param>
        private static void GetIntegerStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = integerStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the default number styles used when parsing a
        /// wide integer value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing a wide
        /// integer value.
        /// </param>
        private static void GetWideIntegerStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = wideIntegerStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the default number styles used when parsing a
        /// fixed-point decimal value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing a
        /// fixed-point decimal value.
        /// </param>
        private static void GetDecimalStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = decimalStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by public entry points of this class only.
        //
        /// <summary>
        /// This method retrieves the default date/time styles used when parsing
        /// a date/time value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the date/time styles used when parsing a
        /// date/time value.
        /// </param>
        private static void GetDateTimeStyles(
            out DateTimeStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = dateTimeStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the default number styles used when parsing a
        /// single-precision floating point value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing a
        /// single-precision floating point value.
        /// </param>
        private static void GetSingleStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = singleStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the default number styles used when parsing a
        /// double-precision floating point value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing a
        /// double-precision floating point value.
        /// </param>
        private static void GetDoubleStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = doubleStyles;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method retrieves the default number styles used when parsing an
        /// arbitrary-precision big integer value.
        /// </summary>
        /// <param name="styles">
        /// Upon return, receives the number styles used when parsing an
        /// arbitrary-precision big integer value.
        /// </param>
        private static void GetBigIntegerStyles(
            out NumberStyles styles
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                styles = bigIntegerStyles;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Type Mapping Helper Methods
        /// <summary>
        /// This method copies each non-null type from the specified type list
        /// into the specified dictionary, creating the dictionary if necessary.
        /// </summary>
        /// <param name="list">
        /// The list of types to copy.  This parameter may be null, in which
        /// case no types are copied.
        /// </param>
        /// <param name="dictionary">
        /// A dictionary that, upon return, contains an entry for each copied
        /// type.  When null, a new dictionary is created.
        /// </param>
        private static void CopyTypes(
            TypeList list,
            ref Dictionary<Type, object> dictionary
            )
        {
            if (list == null)
                return;

            if (dictionary == null)
                dictionary = new Dictionary<Type, object>();

            foreach (Type type in list)
            {
                if (type == null)
                    continue;

                dictionary[type] = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the supported types selected by the specified
        /// flags to the specified type list, creating the list if necessary.
        /// </summary>
        /// <param name="flags">
        /// The flags selecting which categories of supported types to include.
        /// </param>
        /// <param name="types">
        /// A type list that, upon return, has the selected types appended to
        /// it.  When null, a new list is created.
        /// </param>
        internal static void GetTypes(
            TypeListFlags flags,
            ref TypeList types
            )
        {
            Dictionary<Type, object> dictionary = null;

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.IntegerTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(integerTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.FloatTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(floatTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.StringTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(stringTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.NumberTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(numberTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.IntegralTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(integralTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.NonIntegralTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(nonIntegralTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.OtherTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(otherTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(
                    flags, TypeListFlags.AllTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(allTypes, ref dictionary);
                }
            }

            if (dictionary != null)
            {
                if (types == null)
                    types = new TypeList();

                types.AddRange(dictionary.Keys);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Special Value Helper Methods
        /// <summary>
        /// This method parses tab-separated key/value mapping lines from the
        /// specified text, honoring the specified automation flags.  Upon
        /// success, the parsed mappings are stored in the <paramref name="list" />
        /// parameter; upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The text containing the tab-separated mapping lines to parse.  This
        /// parameter cannot be null.
        /// </param>
        /// <param name="automationFlags">
        /// The flags controlling parsing behavior, such as whether null values
        /// and duplicate keys are permitted.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of parsed key/value mappings.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExtractMappings(
            string text,                     /* in */
            AutomationFlags automationFlags, /* in */
            ref StringPairList list,         /* in, out */
            ref Result error                 /* out */
            )
        {
            if (text == null)
            {
                error = "invalid text";
                return ReturnCode.Error;
            }

            text = StringOps.NormalizeLineEndings(text);

            if (text == null)
            {
                error = "could not normalize text";
                return ReturnCode.Error;
            }

            string[] lines = text.Split(Characters.LineFeed);

            if (lines == null)
            {
                error = "could not split text";
                return ReturnCode.Error;
            }

            bool allowNullValue = FlagOps.HasFlags(
                automationFlags, AutomationFlags.AllowNullValue, true);

            bool uniqueKeys = FlagOps.HasFlags(
                automationFlags, AutomationFlags.UniqueKeys, true);

            bool ignoreDuplicateKey = FlagOps.HasFlags(
                automationFlags, AutomationFlags.IgnoreDuplicateKey, true);

            StringPairList localList = new StringPairList();
            int lineNumber = 0;

            foreach (string line in lines)
            {
                lineNumber++;

                int length;

                if (StringOps.IsNullOrEmpty(line, out length))
                    continue;

                if ((length >= 3) && /* PREFIX: ";#;" */
                    (line[0] == Characters.SemiColon) &&
                    (line[1] == Characters.NumberSign) &&
                    (line[2] == Characters.SemiColon))
                {
                    //
                    // NOTE: This is a comment line, skip it.
                    //
                    continue;
                }

                //
                // NOTE: Search for a literal horizontal tab
                //       character.  If a regular expression
                //       needs to contain a horizontal tab
                //       character, please use "\t" (as this
                //       is normal for regular expressions).
                //
                int index = line.IndexOf(Characters.HorizontalTab);

                if (index == Index.Invalid)
                {
                    error = String.Format(
                        "missing tab, line {0}", lineNumber);

                    return ReturnCode.Error;
                }

                //
                // NOTE: This conditional will be true if there
                //       is more than one horizontal tab character
                //       in the line.
                //
                if (index != line.LastIndexOf(Characters.HorizontalTab))
                {
                    error = String.Format(
                        "extra tab, line {0}", lineNumber);

                    return ReturnCode.Error;
                }

                //
                // HACK: Even though the underlying data structure
                //       being returned (i.e. List<T>) supports it,
                //       do not allow null keys here just in case
                //       we wish to switch to using some kind of
                //       dictionary later.
                //
                string key = line.Substring(0, index);

                if (String.IsNullOrEmpty(key))
                {
                    error = String.Format(
                        "invalid key, line {0}", lineNumber);

                    return ReturnCode.Error;
                }

                string value = line.Substring(index + 1);

                if (String.IsNullOrEmpty(value))
                {
                    if (allowNullValue)
                    {
                        //
                        // HACK: Convert emtpy to null.
                        //
                        value = null;
                    }
                    else
                    {
                        error = String.Format(
                            "invalid value, line {0}",
                            lineNumber);

                        return ReturnCode.Error;
                    }
                }

                if (uniqueKeys && localList.ContainsKey(
                        key, StringComparison.Ordinal))
                {
                    if (ignoreDuplicateKey)
                    {
                        continue;
                    }
                    else
                    {
                        error = String.Format(
                            "duplicate key, line {0}",
                            lineNumber);

                        return ReturnCode.Error;
                    }
                }

                localList.Add(key, value);
            }

            list = localList;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the specified script file and extracts its text,
        /// embedded signature, and trailing data.  Upon success, the extracted
        /// portions are stored in the corresponding parameters; upon failure, an
        /// error message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="fileName">
        /// The name of the script file to read.  This parameter cannot be null
        /// or an empty string.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to interpret the script text.  This parameter may
        /// be null, in which case a default binary encoding is used.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the script text.
        /// </param>
        /// <param name="signature">
        /// Upon success, receives the embedded signature bytes.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the trailing data bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExtractScript(
            string fileName,      /* in */
            Encoding encoding,    /* in: OPTIONAL */
            ref string text,      /* out */
            ref byte[] signature, /* out */
            ref byte[] data,      /* out */
            ref Result error      /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "script file {0} does not exist",
                    FormatOps.WrapOrNull(fileName));

                return ReturnCode.Error;
            }

            try
            {
                return ExtractScript(
                    File.ReadAllBytes(fileName), /* throw */
                    encoding, ref text, ref signature, ref data,
                    ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the script text, embedded signature, and
        /// trailing data from the specified bytes.  Upon success, the extracted
        /// portions are stored in the corresponding parameters; upon failure, an
        /// error message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="bytes">
        /// The raw bytes from which to extract the script.  This parameter
        /// cannot be null or empty.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to interpret the script text.  This parameter may
        /// be null, in which case a default binary encoding is used.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the script text.
        /// </param>
        /// <param name="signature">
        /// Upon success, receives the embedded signature bytes.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the trailing data bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExtractScript(
            byte[] bytes,         /* in */
            Encoding encoding,    /* in: OPTIONAL */
            ref string text,      /* out */
            ref byte[] signature, /* out */
            ref byte[] data,      /* out */
            ref Result error      /* out */
            )
        {
            if (bytes == null)
            {
                error = "invalid bytes";
                return ReturnCode.Error;
            }

            if (bytes.Length == 0)
            {
                error = "no bytes";
                return ReturnCode.Error;
            }

            if (encoding == null)
                encoding = StringOps.GetEncoding(EncodingType.Binary);

            byte[] localTextData;
            byte[] localOtherData;

            if (!ArrayOps.SplitOnOne<byte>(bytes,
                    ConversionOps.ToByte(Characters.EndOfFile),
                    out localTextData, out localOtherData))
            {
                error = "invalid or missing soft end-of-file";
                return ReturnCode.Error;
            }

            string localText = encoding.GetString(localTextData);
            int localTextLength = localText.Length;

            string annotation = ScriptOps.FormatAnnotation(
                Annotations.Signature);

            if (annotation == null)
            {
                error = "invalid signature annotation";
                return ReturnCode.Error;
            }

            int signatureIndex = localText.IndexOf(annotation, 0);

            if (signatureIndex == Index.Invalid)
            {
                error = "missing signature";
                return ReturnCode.Error;
            }

            if ((signatureIndex < 0) ||
                (signatureIndex >= localTextLength))
            {
                error = "malformed signature block";
                return ReturnCode.Error;
            }

            int annotationLength = annotation.Length;

            string localSignatureText = localText.Substring(
                signatureIndex + annotationLength);

            localText = localText.Substring(0, signatureIndex);

            if (!StringOps.IsBase64(localSignatureText))
            {
                error = "signature not base64";
                return ReturnCode.Error;
            }

            //
            // HACK: In theory, this call can throw; however,
            //       it should not since the signature block
            //       was just checked above using a regular
            //       expression (which is still cheaper than
            //       throwing an exception?).
            //
            byte[] localSignature = Convert.FromBase64String(
                localSignatureText); /* throw */

            text = localText;
            signature = localSignature;
            data = localOtherData;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified dictionary with the set of known
        /// annotation markers.
        /// </summary>
        /// <param name="annotations">
        /// A dictionary that, upon return, contains an entry for each known
        /// annotation marker.  When null, a new dictionary is created.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetAnnotations(
            ref StringDictionary annotations /* in, out */
            )
        {
            Result error = null;

            return GetAnnotations(ref annotations, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified dictionary with the set of known
        /// annotation markers.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="annotations">
        /// A dictionary that, upon return, contains an entry for each known
        /// annotation marker.  When null, a new dictionary is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetAnnotations(
            ref StringDictionary annotations, /* in, out */
            ref Result error                  /* out */
            )
        {
            try
            {
                FieldInfo[] fieldInfos = typeof(Annotations).GetFields();

                if (fieldInfos == null)
                {
                    error = "annotations unavailable";
                    return ReturnCode.Error;
                }

                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if (fieldInfo == null)
                        continue;

                    if (annotations == null)
                        annotations = new StringDictionary();

                    object fieldValue = fieldInfo.GetValue(null);

                    if (!(fieldValue is string))
                        continue;

                    string annotation = (string)fieldValue;

                    if (String.IsNullOrEmpty(annotation))
                        continue;

                    if (annotations.ContainsKey(annotation))
                        continue;

                    annotations[annotation] = null;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches one of the
        /// recognized annotation patterns.
        /// </summary>
        /// <param name="text">
        /// The text to test.
        /// </param>
        /// <returns>
        /// Non-zero if the text matches a recognized annotation pattern;
        /// otherwise, zero.
        /// </returns>
        public static bool IsAnnotation(
            string text /* in */
            )
        {
            foreach (string pattern in new string[] {
                    annotationsPattern3, annotationsPattern4
                })
            {
                if (pattern == null)
                    continue;

                if (Regex.IsMatch(text, pattern, RegexOptions.None))
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts embedded annotations from the specified text and
        /// adds them to the specified dictionary.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The text from which to extract annotations.  This parameter cannot
        /// be null.
        /// </param>
        /// <param name="annotations">
        /// A dictionary that, upon return, contains an entry for each extracted
        /// annotation.  When null, a new dictionary is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExtractAnnotations(
            string text,                      /* in */
            ref StringDictionary annotations, /* in, out */
            ref Result error                  /* out */
            )
        {
            if (text == null)
            {
                error = "invalid text";
                return ReturnCode.Error;
            }

            foreach (string pattern in new string[] {
                    annotationsPattern1, annotationsPattern2
                })
            {
                if (pattern == null)
                    continue;

                MatchCollection matches = Regex.Matches(
                    text, pattern, RegexOptions.IgnoreCase);

                if ((matches == null) || (matches.Count == 0))
                    continue;

                foreach (Match match in matches)
                {
                    string name = RegExOps.GetMatchValue(match, 1);
                    string value = RegExOps.GetMatchValue(match, 2);

                    if (name == null)
                        continue;

                    if (annotations == null)
                        annotations = new StringDictionary();

                    annotations[name] = value;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to look up the single-precision floating point
        /// value associated with the specified name.  Upon success, the value is
        /// stored in the <paramref name="value" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The name to look up.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating point value
        /// associated with the name.
        /// </param>
        /// <returns>
        /// Non-zero if the name was found; otherwise, zero.
        /// </returns>
        private static bool TryLookupNamedSingle(
            string text,
            ref float value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((text != null) && (namedSingles != null) &&
                    namedSingles.TryGetValue(text, out value))
                {
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to look up the double-precision floating point
        /// value associated with the specified name.  Upon success, the value is
        /// stored in the <paramref name="value" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The name to look up.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating point value
        /// associated with the name.
        /// </param>
        /// <returns>
        /// Non-zero if the name was found; otherwise, zero.
        /// </returns>
        private static bool TryLookupNamedDouble(
            string text,
            ref double value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((text != null) && (namedDoubles != null) &&
                    namedDoubles.TryGetValue(text, out value))
                {
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method attempts to look up the arbitrary-precision big integer
        /// value associated with the specified name.  Upon success, the value is
        /// stored in the <paramref name="value" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The name to look up.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the arbitrary-precision big integer value
        /// associated with the name.
        /// </param>
        /// <returns>
        /// Non-zero if the name was found; otherwise, zero.
        /// </returns>
        private static bool TryLookupNamedBigInteger(
            string text,
            ref BigInteger value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((text != null) && (namedBigIntegers != null) &&
                    namedBigIntegers.TryGetValue(text, out value))
                {
                    return true;
                }

                return false;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method examines the specified text for a radix prefix (such as
        /// the binary, octal, decimal, or hexadecimal prefix) and, when one is
        /// present, records the corresponding radix flags and the remaining
        /// text.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The text to examine for a radix prefix.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture associated with the text.  This parameter is not used.
        /// </param>
        /// <param name="prefixFlags">
        /// On input, the value flags controlling prefix handling; on return,
        /// updated with the detected radix flags.
        /// </param>
        /// <param name="newText">
        /// When a prefix is present, receives the text with the prefix removed.
        /// </param>
        /// <param name="negative">
        /// When a prefix is present, receives a value indicating whether the
        /// text carried a leading minus sign.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// Non-zero if no error occurred; otherwise, zero.
        /// </returns>
        private static bool CheckRadixPrefix(
            string text,                /* in */
            CultureInfo cultureInfo,    /* in: NOT USED */
            ref ValueFlags prefixFlags, /* in, out */
            ref string newText,         /* out */
            ref bool negative,          /* out */
            ref Result error            /* out */
            )
        {
            if (text == null)
                return true;

            int length = text.Length;

            if (length == 0)
                return true;

            int startIndex = 0;
            char firstCharacter = text[startIndex];
            bool haveSign = false;
            bool haveNegative = false;

            if ((firstCharacter == Characters.PlusSign) ||
                (firstCharacter == Characters.MinusSign))
            {
                haveSign = true;
                startIndex++;

                if (startIndex >= length)
                    return true;

                if (firstCharacter == Characters.MinusSign)
                    haveNegative = true;
            }

            if ((startIndex + 1) >= length)
                return true;

            char nextCharacter = text[startIndex];

            //
            // NOTE: All valid prefixes start with zero.
            //
            if (nextCharacter != Characters.Zero)
                return true;

            nextCharacter = text[startIndex + 1];

            int prefixLength = 0;

            prefixFlags &= ~ValueFlags.AnyRadix;

            if ((nextCharacter == Characters.B) ||
                (nextCharacter == Characters.b)) // binary?
            {
                prefixFlags |= ValueFlags.BinaryRadix;
                prefixLength = 2;
            }
            else if ((nextCharacter == Characters.O) ||
                (nextCharacter == Characters.o)) // octal?
            {
                prefixFlags |= ValueFlags.OctalRadix;
                prefixLength = 2;
            }
            else if ((nextCharacter == Characters.D) ||
                (nextCharacter == Characters.d)) // decimal?
            {
                prefixFlags |= ValueFlags.DecimalRadix;
                prefixLength = 2;
            }
            else if ((nextCharacter == Characters.X) ||
                (nextCharacter == Characters.x)) // hexadecimal?
            {
                prefixFlags |= ValueFlags.HexadecimalRadix;
                prefixLength = 2;
            }

            if (prefixLength > 0)
            {
                if (haveSign && !FlagOps.HasFlags(
                        prefixFlags, ValueFlags.AllowRadixSign, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "leading plus/minus sign not supported");

                    return false;
                }

                newText = text.Substring(startIndex + prefixLength);
                negative = haveNegative;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the specified value boxed as a narrow integer
        /// when it fits within the narrow integer range, or as a wide integer
        /// otherwise.
        /// </summary>
        /// <param name="value">
        /// The wide integer value to box.
        /// </param>
        /// <returns>
        /// The value boxed as a narrow integer when it fits within the narrow
        /// integer range; otherwise, the value boxed as a wide integer.
        /// </returns>
        private static object GetIntegerOrWideInteger(
            long value
            )
        {
            if ((value >= int.MinValue) && (value <= int.MaxValue))
                return ConversionOps.ToInt(value);
            else
                return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses the specified text into the most appropriate
        /// numeric value type.  Upon success, the parsed value is stored in the
        /// <paramref name="value" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine parsing behavior.  This
        /// parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the text is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the text.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the parsed numeric value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetNumeric(
            Interpreter interpreter,
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref object value
            )
        {
            Result error = null;

            return GetNumeric(
                interpreter, text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses the specified text into the most appropriate
        /// numeric value type.  Upon success, the parsed value is stored in the
        /// <paramref name="value" /> parameter; upon failure, an error message
        /// is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine parsing behavior.  This
        /// parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text to parse.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the text is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the text.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the parsed numeric value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNumeric(
            Interpreter interpreter,
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref object value,
            ref Result error
            )
        {
            if (text == null)
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "expected numeric value but got {0}",
                    FormatOps.WrapOrNull(text)));

                return ReturnCode.Error;
            }

            int length = text.Length;

            if (length == 0)
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "expected numeric value but got {0}",
                    FormatOps.WrapOrNull(text)));

                return ReturnCode.Error;
            }

            bool boolValue = false;
            bool wasInteger = false;

            if (TryParseBooleanOnly( /* FAST */
                    text, SharedStringOps.SystemNoCaseComparisonType,
                    ref boolValue, ref wasInteger))
            {
                if (wasInteger)
                    value = boolValue ? 1 : 0;
                else
                    value = boolValue;

                return ReturnCode.Ok;
            }

            double doubleValue = 0.0;

            if (TryLookupNamedDouble(text, ref doubleValue))
            {
                value = doubleValue;
                return ReturnCode.Ok;
            }

            NumberStyles styles; /* REUSED */

            IFormatProvider formatProvider = GetNumberFormatProvider(
                cultureInfo);

            string decimalSeparator = GetNumberDecimalSeparator(
                formatProvider);

            decimal decimalValue = Decimal.Zero;

            if ((decimalSeparator != null) &&
                (text.IndexOf(decimalSeparator) != Index.Invalid))
            {
                GetDecimalStyles(out styles);

                if (decimal.TryParse(
                        text, styles, formatProvider, out decimalValue))
                {
                    value = decimalValue;
                    return ReturnCode.Ok;
                }

                GetDoubleStyles(out styles);

                doubleValue = 0.0;

                if (double.TryParse(
                        text, styles, formatProvider, out doubleValue))
                {
                    value = doubleValue;
                    return ReturnCode.Ok;
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected fixed/floating point value but got {0}",
                    FormatOps.WrapOrNull(text)));

                return ReturnCode.Error;
            }

            bool triedDouble = false;

            if ((MaybeFloatingPointChars != null) &&
                (text.IndexOfAny(MaybeFloatingPointChars) != Index.Invalid))
            {
                GetDoubleStyles(out styles);

                doubleValue = 0.0;

                if (double.TryParse(
                        text, styles, formatProvider, out doubleValue))
                {
                    value = doubleValue;
                    return ReturnCode.Ok;
                }

                triedDouble = true;
            }

            bool done; /* REUSED */
            long longValue = 0;

            done = false;

            if (ParseWideIntegerWithRadixPrefix(
                    text, flags, cultureInfo, ref done, ref longValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (done)
            {
                value = GetIntegerOrWideInteger(longValue);
                return ReturnCode.Ok;
            }

            char firstCharacter = text[0];

            if ((firstCharacter == Characters.PlusSign) ||
                (firstCharacter == Characters.MinusSign))
            {
                GetWideIntegerStyles(out styles);

                longValue = 0;

                if (long.TryParse(
                        text, styles, formatProvider, out longValue))
                {
                    value = GetIntegerOrWideInteger(longValue);
                    return ReturnCode.Ok;
                }
            }

#if NET_40
            bool allowBigIntegers = ScriptOps.HasFlags(
                interpreter, InterpreterFlags.AllowBigIntegers, true);
#endif

            GetWideIntegerStyles(out styles);

            ulong ulongValue = 0;

            if (ulong.TryParse(
                    text, styles, formatProvider, out ulongValue))
            {
#if NET_40
                if (!allowBigIntegers || (ulongValue <= long.MaxValue))
#endif
                {
                    value = GetIntegerOrWideInteger(ConversionOps.ToLong(
                        ulongValue));

                    return ReturnCode.Ok;
                }
            }

#if NET_40
            if (allowBigIntegers)
            {
                BigInteger bigIntegerValue = BigInteger.Zero; /* REUSED */

                done = false;

                if (ParseBigIntegerWithRadixPrefix(
                        text, flags, cultureInfo, ref done,
                        ref bigIntegerValue, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (done)
                {
                    value = bigIntegerValue;
                    return ReturnCode.Ok;
                }

                GetBigIntegerStyles(out styles);

                bigIntegerValue = BigInteger.Zero;

                if (BigInteger.TryParse(
                        text, styles, formatProvider, out bigIntegerValue))
                {
                    value = bigIntegerValue;
                    return ReturnCode.Ok;
                }
            }
#endif

            GetDecimalStyles(out styles);

            decimalValue = Decimal.Zero;

            if (decimal.TryParse(
                    text, styles, formatProvider, out decimalValue))
            {
                value = decimalValue;
                return ReturnCode.Ok;
            }

            if (!triedDouble)
            {
                GetDoubleStyles(out styles);

                doubleValue = 0.0;

                if (double.TryParse(
                        text, styles, formatProvider, out doubleValue))
                {
                    value = doubleValue;
                    return ReturnCode.Ok;
                }

                triedDouble = true; /* REDUNDANT */
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected numeric value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string comparison type indicated by the
        /// specified value flags.
        /// </summary>
        /// <param name="flags">
        /// The value flags that determine whether a case-insensitive comparison
        /// is requested.
        /// </param>
        /// <returns>
        /// The string comparison type indicated by the flags.
        /// </returns>
        private static StringComparison GetComparisonType(
            ValueFlags flags
            )
        {
            if (FlagOps.HasFlags(flags, ValueFlags.NoCase, true))
                return SharedStringOps.SystemNoCaseComparisonType;

            return SharedStringOps.SystemComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Boolean Options To ValueFlags Methods
        /// <summary>
        /// This method builds the value flags used for type conversion from the
        /// specified boolean options.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to require a strict conversion.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce verbose error messages.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified options.
        /// </returns>
        internal static ValueFlags GetTypeValueFlags(
            bool strict,
            bool verbose,
            bool noCase
            )
        {
            return GetTypeValueFlags(
                false, strict, verbose, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the value flags used for type conversion from the
        /// specified boolean options.
        /// </summary>
        /// <param name="allowInteger">
        /// Non-zero to allow an integer value to be accepted.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require a strict conversion.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce verbose error messages.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified options.
        /// </returns>
        internal static ValueFlags GetTypeValueFlags(
            bool allowInteger,
            bool strict,
            bool verbose,
            bool noCase
            )
        {
            return GetTypeValueFlags(
                ValueFlags.None, allowInteger, strict, verbose, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the value flags used for type conversion by
        /// combining the specified base flags with the specified boolean
        /// options.
        /// </summary>
        /// <param name="flags">
        /// The base value flags to combine with the specified options.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to allow an integer value to be accepted.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require a strict conversion.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce verbose error messages.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified base flags and
        /// options.
        /// </returns>
        internal static ValueFlags GetTypeValueFlags(
            ValueFlags flags,
            bool allowInteger,
            bool strict,
            bool verbose,
            bool noCase
            )
        {
            ValueFlags result = flags | Defaults.ValueFlags;

            if (allowInteger)
                result |= ValueFlags.AllowInteger;

            if (strict)
                result |= ValueFlags.Strict;

            if (verbose)
                result |= ValueFlags.Verbose;

            if (noCase)
                result |= ValueFlags.NoCase;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the value flags used for type conversion from the
        /// specified option flags.
        /// </summary>
        /// <param name="flags">
        /// The option flags from which the value flags are derived.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified option flags.
        /// </returns>
        internal static ValueFlags GetTypeValueFlags(
            OptionFlags flags
            )
        {
            ValueFlags result = Defaults.ValueFlags;

            if (FlagOps.HasFlags(flags, OptionFlags.AllowInteger, true))
                result |= ValueFlags.AllowInteger;

            if (FlagOps.HasFlags(flags, OptionFlags.Strict, true))
                result |= ValueFlags.Strict;

            if (FlagOps.HasFlags(flags, OptionFlags.Verbose, true))
                result |= ValueFlags.Verbose;

            if (FlagOps.HasFlags(flags, OptionFlags.NoCase, true))
                result |= ValueFlags.NoCase;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the value flags used for object conversion by
        /// combining the specified base flags with the specified boolean
        /// options.
        /// </summary>
        /// <param name="flags">
        /// The base value flags to combine with the specified options.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require a strict conversion.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce verbose error messages.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified base flags and
        /// options.
        /// </returns>
        internal static ValueFlags GetObjectValueFlags(
            ValueFlags flags,
            bool strict,
            bool verbose,
            bool noCase
            )
        {
            return GetObjectValueFlags(
                flags, strict, verbose, noCase, false, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the value flags used for object conversion by
        /// combining the specified base flags with the specified boolean
        /// options.
        /// </summary>
        /// <param name="flags">
        /// The base value flags to combine with the specified options.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require a strict conversion.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce verbose error messages.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <param name="noNested">
        /// Non-zero to disallow nested object resolution.
        /// </param>
        /// <param name="noComObject">
        /// Non-zero to disallow COM object resolution.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified base flags and
        /// options.
        /// </returns>
        internal static ValueFlags GetObjectValueFlags(
            ValueFlags flags,
            bool strict,
            bool verbose,
            bool noCase,
            bool noNested,
            bool noComObject
            )
        {
            ValueFlags result = flags | Defaults.ValueFlags;

            if (strict)
                result |= ValueFlags.Strict;

            if (verbose)
                result |= ValueFlags.Verbose;

            if (noCase)
                result |= ValueFlags.NoCase;

            if (noNested)
                result |= ValueFlags.NoNested;

            if (noComObject)
                result |= ValueFlags.NoComObject;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the value flags used for member resolution by
        /// combining the specified base flags with the specified boolean
        /// options.
        /// </summary>
        /// <param name="flags">
        /// The base value flags to combine with the specified options.
        /// </param>
        /// <param name="noNested">
        /// Non-zero to disallow nested object resolution.
        /// </param>
        /// <param name="noComObject">
        /// Non-zero to disallow COM object resolution.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified base flags and
        /// options.
        /// </returns>
        internal static ValueFlags GetMemberValueFlags(
            ValueFlags flags,
            bool noNested,
            bool noComObject
            )
        {
            ValueFlags result = flags | Defaults.ValueFlags;

            if (noNested)
                result |= ValueFlags.NoNested;

            if (noComObject)
                result |= ValueFlags.NoComObject;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the value flags used for call frame resolution
        /// from the specified boolean option.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to require a strict conversion.
        /// </param>
        /// <returns>
        /// The value flags corresponding to the specified option.
        /// </returns>
        internal static ValueFlags GetCallFrameValueFlags(
            bool strict
            )
        {
            ValueFlags result = Defaults.ValueFlags;

            if (strict)
                result |= ValueFlags.Strict;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the individual boolean type-conversion options
        /// from the specified value flags.
        /// </summary>
        /// <param name="flags">
        /// The value flags from which to extract the boolean options.
        /// </param>
        /// <param name="allowInteger">
        /// Upon return, non-zero if an integer value is allowed.
        /// </param>
        /// <param name="strict">
        /// Upon return, non-zero if a strict conversion is required.
        /// </param>
        /// <param name="verbose">
        /// Upon return, non-zero if verbose error messages are produced.
        /// </param>
        /// <param name="noCase">
        /// Upon return, non-zero if matching is case-insensitive.
        /// </param>
        internal static void ExtractTypeValueFlags(
            ValueFlags flags,
            out bool allowInteger,
            out bool strict,
            out bool verbose,
            out bool noCase
            )
        {
            allowInteger = FlagOps.HasFlags(
                flags, ValueFlags.AllowInteger, true);

            strict = FlagOps.HasFlags(
                flags, ValueFlags.Strict, true);

            verbose = FlagOps.HasFlags(
                flags, ValueFlags.Verbose, true);

            noCase = FlagOps.HasFlags(
                flags, ValueFlags.NoCase, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string appears to be a
        /// version value, based on a simple pattern match.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <returns>
        /// Non-zero if the specified string looks like a version value;
        /// otherwise, zero.
        /// </returns>
        private static bool LooksLikeVersion(
            string text
            )
        {
            if (String.IsNullOrEmpty(text))
                return false;

            Regex regEx = versionRegEx;

            if (regEx == null)
                return false;

            Match match = regEx.Match(text);

            if (match == null)
                return false;

            return match.Success;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a version value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the version value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetVersion(
            string text,
            CultureInfo cultureInfo,
            ref Version value
            )
        {
            Result error = null;

            return GetVersion(text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a version value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the version value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetVersion(
            string text,
            CultureInfo cultureInfo,
            ref Version value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetVersion(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a version value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the version value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetVersion(
            string text,
            CultureInfo cultureInfo,
            ref Version value,
            ref Result error,
            ref Exception exception
            )
        {
            try
            {
                if (LooksLikeVersion(text))
                {
                    //
                    // FIXME: *COMPAT* This is not 100% Tcl
                    //        compatible.
                    //
                    // TODO: No TryParse, eh?
                    //
                    value = new Version(text);
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected version value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a version range, i.e. a
        /// pair of version values.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value1">
        /// Upon success, receives the first version of the range, if any.
        /// </param>
        /// <param name="value2">
        /// Upon success, receives the second version of the range, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetVersionRange(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Version value1,
            ref Version value2
            )
        {
            Result error = null;

            return GetVersionRange(
                text, flags, cultureInfo, ref value1, ref value2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a version range, i.e. a
        /// pair of version values.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value1">
        /// Upon success, receives the first version of the range, if any.
        /// </param>
        /// <param name="value2">
        /// Upon success, receives the second version of the range, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetVersionRange(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Version value1,
            ref Version value2,
            ref Result error
            )
        {
            Exception exception = null;

            return GetVersionRange(
                text, flags, cultureInfo, ref value1, ref value2, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a version range, i.e. a
        /// pair of version values.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value1">
        /// Upon success, receives the first version of the range, if any.
        /// </param>
        /// <param name="value2">
        /// Upon success, receives the second version of the range, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetVersionRange(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Version value1,
            ref Version value2,
            ref Result error,
            ref Exception exception
            )
        {
            try
            {
                if (text != null)
                {
                    if (versionRangeRegEx != null)
                    {
                        Match match = versionRangeRegEx.Match(text);

                        if (match.Success)
                        {
                            string matchValue1 = RegExOps.GetMatchValue(match, 1);
                            string matchValue2 = RegExOps.GetMatchValue(match, 2);

                            Version localValue1 = !String.IsNullOrEmpty(matchValue1) ?
                                new Version(matchValue1) : null;

                            Version localValue2 = !String.IsNullOrEmpty(matchValue2) ?
                                new Version(matchValue2) : null;

                            if ((localValue1 == null) && (localValue2 == null))
                            {
                                if (FlagOps.HasFlags(
                                        flags, ValueFlags.AllowEmpty, true))
                                {
                                    goto goodVersion;
                                }
                                else
                                {
                                    goto badVersion;
                                }
                            }

                            if ((localValue1 == null) || (localValue2 == null))
                            {
                                if (FlagOps.HasFlags(
                                        flags, ValueFlags.AllowOpen, true))
                                {
                                    goto goodVersion;
                                }
                                else
                                {
                                    goto badVersion;
                                }
                            }

                            /* IGNORED */
                            PackageOps.MaybeSwapVersion(
                                ref localValue1, ref localValue2);

                        goodVersion:

                            value1 = localValue1;
                            value2 = localValue2;

                            return ReturnCode.Ok;
                        }
                    }
                }
                else if (FlagOps.HasFlags(flags, ValueFlags.AllowNull, true))
                {
                    value1 = null;
                    value2 = null;

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

        badVersion:

            error = MaybeInvokeErrorCallback(String.Format(
                "expected version range value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a uri value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="uriKind">
        /// The kind of uri (e.g. absolute or relative) that is permitted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the uri value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetUri(
            string text,
            UriKind uriKind,
            CultureInfo cultureInfo,
            ref Uri value
            )
        {
            Result error = null;

            return GetUri(
                text, uriKind, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a uri value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="uriKind">
        /// The kind of uri (e.g. absolute or relative) that is permitted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the uri value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetUri(
            string text,
            UriKind uriKind,
            CultureInfo cultureInfo,
            ref Uri value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUri(
                text, uriKind, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a uri value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="uriKind">
        /// The kind of uri (e.g. absolute or relative) that is permitted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the uri value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetUri(
            string text,
            UriKind uriKind,
            CultureInfo cultureInfo,
            ref Uri value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text) &&
                Uri.IsWellFormedUriString(text, uriKind) &&
                Uri.TryCreate(text, uriKind, out value))
            {
                return ReturnCode.Ok;
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected uri value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reverses, in place, the byte order of the dash-delimited
        /// sections of a serialized guid that require byte-swapping on
        /// little-endian platforms.
        /// </summary>
        /// <param name="bytes">
        /// The array of guid bytes to reverse in place.  Upon success, this
        /// array contains the byte-swapped guid.
        /// </param>
        /// <returns>
        /// Non-zero if the bytes were reversed successfully; otherwise, zero.
        /// </returns>
        public static bool ReverseGuidBytes(
            byte[] bytes /* in, out */
            )
        {
            if (bytes == null)
                return false;

            if (bytes.Length != Marshal.SizeOf(typeof(Guid)))
                return false;

            //
            // NOTE: The normal string format for a GUID is:
            //
            //       00112233-4455-6677-8899-101112131415
            //
            //       The first three dash-delimited sections must be byte-swapped;
            //       The last two dash-delimited sections are stored as a sequence
            //       of bytes, which should never require any swapping.
            //
            if (BitConverter.IsLittleEndian)
            {
                byte swap;

                swap = bytes[0]; bytes[0] = bytes[3]; bytes[3] = swap; // 00XXXX33
                swap = bytes[1]; bytes[1] = bytes[2]; bytes[2] = swap; // XX1122XX
                swap = bytes[4]; bytes[4] = bytes[5]; bytes[5] = swap; // 4455
                swap = bytes[6]; bytes[6] = bytes[7]; bytes[7] = swap; // 6677
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string appears to be a
        /// guid value, based on a simple pattern match.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <returns>
        /// Non-zero if the specified string looks like a guid value; otherwise,
        /// zero.
        /// </returns>
        private static bool LooksLikeGuid(
            string text
            )
        {
            if (String.IsNullOrEmpty(text))
                return false;

            Regex regEx = guidRegEx;

            if (regEx == null)
                return false;

            Match match = regEx.Match(text);

            if (match == null)
                return false;

            return match.Success;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a guid value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the guid value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid value
            )
        {
            Result error = null;

            return GetGuid(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a guid value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the guid value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetGuid(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a guid value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the guid value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid value,
            ref Result error,
            ref Exception exception
            )
        {
            try
            {
                if (LooksLikeGuid(text))
                {
                    //
                    // TODO: No TryParse, eh?
                    //
                    value = new Guid(text);
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected guid value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable guid value.
        /// An empty string is converted to a null value.  Upon failure, an
        /// error message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the guid value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid? value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNullableGuid(
                text, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable guid value.
        /// An empty string is converted to a null value.  Upon failure, an
        /// error message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the guid value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNullableGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid? value,
            ref Result error,
            ref Exception exception
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                value = null;
                return ReturnCode.Ok;
            }

            Guid guid = Guid.Empty;

            if (GetGuid(
                    text, cultureInfo, ref guid, ref error,
                    ref exception) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            value = guid;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method installs the default type-resolution callbacks when the
        /// caller did not supply them, unless their use has been disabled via
        /// the value flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="callback1">
        /// The one-parameter type-resolution callback.  When null, it may be
        /// set to the default callback.
        /// </param>
        /// <param name="callback3">
        /// The three-parameter type-resolution callback.  When null, it may be
        /// set to the default callback.
        /// </param>
        private static void MaybeUseDefaultGetTypeCallbacks(
            ValueFlags flags,
            ref GetTypeCallback1 callback1,
            ref GetTypeCallback3 callback3
            )
        {
            //
            // NOTE: If they did not specify a type resolution
            //       callback, we use the default.
            //
            if (!FlagOps.HasFlags(
                    flags, ValueFlags.NoDefaultGetType, true))
            {
                if (callback1 == null)
                    callback1 = Type.GetType;

                if (callback3 == null)
                    callback3 = Type.GetType;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method merges the errors and exception accumulated during a
        /// local type-resolution attempt into the overall list of errors and
        /// exception provided by the caller.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="localErrors">
        /// The list of errors accumulated during the local type-resolution
        /// attempt, if any.
        /// </param>
        /// <param name="localException">
        /// The exception caught during the local type-resolution attempt, if
        /// any.
        /// </param>
        /// <param name="errors">
        /// Upon return, contains the merged list of errors.  When null, a new
        /// list may be created.
        /// </param>
        /// <param name="exception">
        /// Upon return, receives the exception that was caught, if any.
        /// </param>
        private static void HandleGetAnyTypeErrors(
            ValueFlags flags,
            ResultList localErrors,
            Exception localException,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if ((errors == null) || (errors.Count == 0) ||
                FlagOps.HasFlags(
                    flags, ValueFlags.AllGetTypeErrors, true))
            {
                if (localErrors != null)
                {
                    foreach (Result localError in localErrors)
                    {
                        if (localError == null)
                            continue;

                        if ((errors == null) ||
                            (errors.Find(localError) == Index.Invalid))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }
                    }
                }
            }

            if (localException != null)
                exception = localException;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to resolve the specified type name by searching
        /// each of the specified assemblies in turn.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The type name to resolve.
        /// </param>
        /// <param name="types">
        /// The list of type arguments to apply for a generic type, if any.
        /// </param>
        /// <param name="assemblies">
        /// The assemblies to search for the type.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved type.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetAnyTypeViaAnyAssembly(
            Interpreter interpreter,
            string text,
            TypeList types,
            IEnumerable<Assembly> assemblies,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            ResultList localErrors = null;
            Exception localException = null;

            if (assemblies != null)
            {
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly == null)
                        continue;

                    GetTypeCallback1 callback1 = null;
                    GetTypeCallback3 callback3 = null;

                    try
                    {
                        callback1 = assembly.GetType; /* throw */
                        callback3 = assembly.GetType; /* throw */
                    }
                    catch (Exception e)
                    {
                        if (!FlagOps.HasFlags(
                                flags, ValueFlags.NoException,
                                true))
                        {
                            if (localErrors == null)
                                localErrors = new ResultList();

                            localErrors.Add(e);
                        }
                    }

                    if (GetAnyTypeViaCallback(
                            interpreter, text, types, assembly, callback1,
                            callback3, flags | ValueFlags.NoDefaultGetType,
                            cultureInfo, ref value, ref localErrors,
                            ref localException) == ReturnCode.Ok)
                    {
                        HandleGetAnyTypeErrors(
                            flags, localErrors, localException,
                            ref errors, ref exception);

                        return ReturnCode.Ok;
                    }
                }
            }
            else
            {
                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add("invalid assemblies");
            }

            HandleGetAnyTypeErrors(
                flags, localErrors, localException,
                ref errors, ref exception);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the specified type name to a type, optionally
        /// searching the assemblies loaded into the specified application
        /// domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The type name to resolve.
        /// </param>
        /// <param name="types">
        /// The list of type arguments to apply for a generic type, if any.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose loaded assemblies may be searched, if
        /// any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved type.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetAnyType(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value
            )
        {
            ResultList errors = null;

            return GetAnyType(
                interpreter, text, types, appDomain, flags, cultureInfo,
                ref value, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the specified type name to a type, optionally
        /// searching the assemblies loaded into the specified application
        /// domain.  Upon failure, the errors encountered are stored in the
        /// <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The type name to resolve.
        /// </param>
        /// <param name="types">
        /// The list of type arguments to apply for a generic type, if any.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose loaded assemblies may be searched, if
        /// any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved type.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetAnyType(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value,
            ref ResultList errors
            )
        {
            ReturnCode code;
            Exception exception = null;

            code = GetAnyType(
                interpreter, text, types, appDomain,
                flags, cultureInfo, ref value, ref errors,
                ref exception);

            if (code != ReturnCode.Ok)
                errors = MaybeInvokeErrorCallback(errors);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the specified type name to a type, optionally
        /// searching the assemblies loaded into the specified application
        /// domain.  Upon failure, the errors encountered are stored in the
        /// <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The type name to resolve.
        /// </param>
        /// <param name="types">
        /// The list of type arguments to apply for a generic type, if any.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose loaded assemblies may be searched, if
        /// any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved type.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetAnyType(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            //
            // NOTE: This static method is used to fetch a System.Type
            //       object based on a type string that may be qualified
            //       with a namespace and/or an assembly name.  If the
            //       type string is not qualified with a namespace, we may
            //       try prepending various type prefixes to it during the
            //       resolution process.  If the type string is not
            //       qualified with an assembly name, we may try searching
            //       through all the assemblies loaded into the current
            //       AppDomain during the resolution process.  All errors
            //       that we encounter during the type name resolution
            //       process will be recorded in the list of errors
            //       provided by the caller, even if the overall result of
            //       the entire operation turns out to be successful.
            //
            ResultList localErrors = null;
            Exception localException = null;

            if (!FlagOps.HasFlags(
                    flags, ValueFlags.SkipTypeGetType, true) &&
                GetAnyTypeViaCallback(
                    interpreter, text, types, null, null, null,
                    flags, cultureInfo, ref value, ref localErrors,
                    ref localException) == ReturnCode.Ok)
            {
                HandleGetAnyTypeErrors(
                    flags, localErrors, localException,
                    ref errors, ref exception);

                return ReturnCode.Ok;
            }

            if ((appDomain != null) &&
                !FlagOps.HasFlags(
                    flags, ValueFlags.Strict, true) &&
                !FlagOps.HasFlags(
                    flags, ValueFlags.NoAssembly, true) &&
                !MarshalOps.IsAssemblyQualifiedTypeName(text))
            {
                Assembly[] assemblies = appDomain.GetAssemblies();

                if ((assemblies != null) && (assemblies.Length > 0))
                {
                    if (GetAnyTypeViaAnyAssembly(
                            interpreter, text, types,
                            assemblies, flags, cultureInfo,
                            ref value, ref localErrors,
                            ref localException) == ReturnCode.Ok)
                    {
                        HandleGetAnyTypeErrors(
                            flags, localErrors, localException,
                            ref errors, ref exception);

                        return ReturnCode.Ok;
                    }
                }
            }

            HandleGetAnyTypeErrors(
                flags, localErrors, localException,
                ref errors, ref exception);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to resolve the specified type name via the
        /// specified type-resolution callbacks, optionally constrained to a
        /// single assembly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The type name to resolve.
        /// </param>
        /// <param name="types">
        /// The list of type arguments to apply for a generic type, if any.
        /// </param>
        /// <param name="assembly">
        /// The assembly to which type resolution is constrained, if any.
        /// </param>
        /// <param name="callback1">
        /// The one-parameter type-resolution callback to use, if any.
        /// </param>
        /// <param name="callback3">
        /// The three-parameter type-resolution callback to use, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// This parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved type.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetAnyTypeViaCallback(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            TypeList types,
            Assembly assembly,
            GetTypeCallback1 callback1,
            GetTypeCallback3 callback3,
            ValueFlags flags,
            CultureInfo cultureInfo, /* NOT USED */
            ref Type value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            bool locked = false;

            try
            {
                if (interpreter != null)
                    interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked || (interpreter == null))
                {
                    int errorCount = (errors != null) ? errors.Count : 0;
                    AssemblyName assemblyName = null;

                    if (assembly != null)
                    {
                        try
                        {
                            assemblyName = assembly.GetName();
                        }
                        catch (Exception e)
                        {
                            if (!FlagOps.HasFlags(
                                    flags, ValueFlags.NoException, true))
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(e);
                            }

                            exception = e;
                        }
                    }

                    //
                    // NOTE: *WARNING* Empty opaque object handle names are allowed,
                    //       please do not change this to "!String.IsNullOrEmpty".
                    //
                    if (text != null)
                    {
                        //
                        // NOTE: Try to obtain a Type object from the interpreter
                        //       based on an opaque object handle.
                        //
                        object @object = null;

                        if ((interpreter != null) &&
                            (GetObject(interpreter, text, ref @object) == ReturnCode.Ok))
                        {
                            //
                            // NOTE: The null value may be used here to indicate
                            //       that the caller does not care about a particular
                            //       [parameter] type.
                            //
                            if ((@object == null) || (@object is Type))
                            {
                                value = MarshalOps.MaybeGenericType(
                                    (Type)@object, types, flags, ref errors);

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                Result localError = String.Format(
                                    "object {0} type mismatch, type {1} is " +
                                    "not compatible with type {2}",
                                    FormatOps.WrapOrNull(text),
                                    MarshalOps.GetErrorValueTypeName(@object),
                                    MarshalOps.GetErrorTypeName(typeof(Type)));

                                if ((errors == null) ||
                                    (errors.Find(localError) == Index.Invalid))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Add(localError);
                                }
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Check the type name in the type name lookup
                            //       table for the interpreter (i.e. typeDefs).
                            //
                            StringDictionary objectTypes = (interpreter != null) ?
                                interpreter.ObjectTypes : null;

                            if (objectTypes != null)
                            {
                                string newText;

                                if (objectTypes.TryGetValue(text, out newText))
                                    text = newText;
                            }

#if TYPE_CACHE
                            //
                            // NOTE: Check the type name in the type name lookup
                            //       cache for the interpreter.
                            //
                            Type localValue = null;

                            if ((interpreter != null) &&
                                interpreter.GetCachedType(text, ref localValue))
                            {
                                value = MarshalOps.MaybeGenericType(
                                    localValue, types, flags, ref errors);

                                return ReturnCode.Ok;
                            }
#endif

                            Type type = null;

                            try
                            {
                                MaybeUseDefaultGetTypeCallbacks(
                                    flags, ref callback1, ref callback3);

                                if (FlagOps.HasFlags(
                                        flags, ValueFlags.OneParameterGetType,
                                        true))
                                {
                                    if (callback1 != null)
                                    {
                                        type = callback1(text); /* throw */
                                    }
                                    else if (FlagOps.HasFlags(
                                            flags, ValueFlags.Verbose, true))
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        errors.Add(
                                            "no one parameter type callback available");
                                    }
                                }
                                else
                                {
                                    if (callback3 != null)
                                    {
                                        type = callback3(text,
                                            FlagOps.HasFlags(
                                                flags, ValueFlags.Verbose, true),
                                            FlagOps.HasFlags(
                                                flags, ValueFlags.NoCase, true)); /* throw */
                                    }
                                    else if (FlagOps.HasFlags(
                                            flags, ValueFlags.Verbose, true))
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        errors.Add(
                                            "no three parameter type callback available");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                if (!FlagOps.HasFlags(
                                        flags, ValueFlags.NoException, true))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Add(e);
                                }

                                exception = e;
                            }

                            //
                            // NOTE: Did we find the type they specified (which
                            //       may or may not have been qualified)?
                            //
                            if (type != null)
                            {
#if TYPE_CACHE
                                if (interpreter != null)
                                    interpreter.AddCachedType(text, type);
#endif

                                value = MarshalOps.MaybeGenericType(
                                    type, types, flags, ref errors);

                                return ReturnCode.Ok;
                            }
                            else if (!FlagOps.HasFlags(flags, ValueFlags.Strict, true) &&
                                !FlagOps.HasFlags(flags, ValueFlags.NoNamespace, true) &&
                                !MarshalOps.IsNamespaceQualifiedTypeName(text))
                            {
                                if (FlagOps.HasFlags(flags, ValueFlags.ShowName, true))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Add(String.Format("type {0} not found",
                                        FormatOps.WrapOrNull(FormatOps.QualifiedName(
                                            assemblyName, text, FlagOps.HasFlags(flags,
                                                ValueFlags.FullName, true)))));
                                }

                                StringLongPairStringDictionary namespaces =
                                    (interpreter != null) ?
                                        interpreter.ObjectNamespaces : null;

                                if (namespaces != null)
                                {
                                    //
                                    // HACK: Allow them to specify a partially-
                                    //       qualified type name as long as it
                                    //       resides in what we consider to be
                                    //       one of our well-known namespaces
                                    //       (i.e. "System", etc).
                                    //
                                    foreach (KeyValuePair<StringLongPair, string> pair in namespaces)
                                    {
                                        StringLongPair namespacePair = pair.Key;

                                        if (namespacePair == null)
                                            continue;

                                        string namespaceName = namespacePair.X;

                                        if (namespaceName == null)
                                            continue;

                                        string newText = FormatOps.QualifiedName(
                                            namespaceName, text);

#if TYPE_CACHE
                                        localValue = null;

                                        if ((interpreter != null) &&
                                            interpreter.GetCachedType(newText, ref localValue))
                                        {
                                            value = MarshalOps.MaybeGenericType(
                                                localValue, types, flags, ref errors);

                                            return ReturnCode.Ok;
                                        }
#endif

                                        try
                                        {
                                            MaybeUseDefaultGetTypeCallbacks(
                                                flags, ref callback1, ref callback3);

                                            if (FlagOps.HasFlags(
                                                    flags, ValueFlags.OneParameterGetType,
                                                    true))
                                            {
                                                if (callback1 != null)
                                                {
                                                    type = callback1(newText); /* throw */
                                                }
                                                else if (FlagOps.HasFlags(
                                                        flags, ValueFlags.Verbose, true))
                                                {
                                                    if (errors == null)
                                                        errors = new ResultList();

                                                    errors.Add(
                                                        "no one parameter type callback available");
                                                }
                                            }
                                            else
                                            {
                                                if (callback3 != null)
                                                {
                                                    type = callback3(newText,
                                                        FlagOps.HasFlags(flags,
                                                            ValueFlags.Verbose, true),
                                                        FlagOps.HasFlags(flags,
                                                            ValueFlags.NoCase, true)); /* throw */
                                                }
                                                else if (FlagOps.HasFlags(
                                                        flags, ValueFlags.Verbose, true))
                                                {
                                                    if (errors == null)
                                                        errors = new ResultList();

                                                    errors.Add(
                                                        "no three parameter type callback available");
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            if (!FlagOps.HasFlags(
                                                    flags, ValueFlags.NoException, true))
                                            {
                                                if (errors == null)
                                                    errors = new ResultList();

                                                errors.Add(e);
                                            }

                                            exception = e;
                                        }

                                        //
                                        // NOTE: Did we find the type they specified
                                        //       (which may or may not have been
                                        //       qualified) modified with the current
                                        //       type prefix?
                                        //
                                        if (type != null)
                                        {
#if TYPE_CACHE
                                            if (interpreter != null)
                                                interpreter.AddCachedType(newText, type);
#endif

                                            value = MarshalOps.MaybeGenericType(
                                                type, types, flags, ref errors);

                                            return ReturnCode.Ok;
                                        }
                                        else if (FlagOps.HasFlags(flags, ValueFlags.ShowName, true))
                                        {
                                            if (errors == null)
                                                errors = new ResultList();

                                            errors.Add(String.Format("type {0} not found",
                                                FormatOps.WrapOrNull(FormatOps.QualifiedName(
                                                    assemblyName, newText, FlagOps.HasFlags(
                                                        flags, ValueFlags.FullName, true)))));
                                        }
                                    }
                                }
                            }
                            else if (FlagOps.HasFlags(flags, ValueFlags.ShowName, true))
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(String.Format("type {0} not found",
                                    FormatOps.WrapOrNull(FormatOps.QualifiedName(
                                        assemblyName, text, FlagOps.HasFlags(
                                            flags, ValueFlags.FullName, true)))));
                            }
                        }
                    }

                    //
                    // NOTE: If this method did not add any error messages
                    //       before this point, add one now.
                    //
                    if ((errors == null) || (errors.Count == errorCount))
                    {
                        Result localError = String.Format(
                            "expected type value but got {0}",
                            FormatOps.WrapOrNull(text));

                        if ((errors == null) ||
                            (errors.Find(localError) == Index.Invalid))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Insert(0, localError);
                        }
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetAnyTypeViaCallback",
                        typeof(Value).Name, false,
                        TracePriority.LockError,
                        interpreter.MaybeWhoHasLock());

                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("could not lock interpreter");
                }
            }
            finally
            {
                if (interpreter != null)
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of types.  Upon
        /// failure, the errors encountered are stored in the
        /// <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose loaded assemblies may be searched, if
        /// any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of types parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors
            )
        {
            ReturnCode code;
            Exception exception = null;

            code = GetTypeList(
                interpreter, text, appDomain, flags,
                cultureInfo, ref value, ref errors,
                ref exception);

            if (code != ReturnCode.Ok)
                errors = MaybeInvokeErrorCallback(errors);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of types.  Upon
        /// failure, the errors encountered are stored in the
        /// <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose loaded assemblies may be searched, if
        /// any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of types parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if (text == null)
                goto error;

            StringList list = null;
            Result localError = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref localError) == ReturnCode.Ok)
            {
                TypeList typeList = new TypeList();

                foreach (string element in list)
                {
                    Type type = null;

                    if (GetAnyType(
                            interpreter, element, null, appDomain,
                            flags, cultureInfo, ref type, ref errors,
                            ref exception) == ReturnCode.Ok)
                    {
                        typeList.Add(type);
                    }
                    else
                    {
                        goto error;
                    }
                }

                //
                // NOTE: If we get this far, all elements in the list were
                //       successfully interpreted as a System.Type object.
                //
                value = typeList;

                return ReturnCode.Ok;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

        error:

            if (errors == null)
                errors = new ResultList();

            errors.Insert(0, String.Format(
                "expected type list value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of types, using
        /// the specified type-resolution callbacks.  Upon failure, the errors
        /// encountered are stored in the <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="assembly">
        /// The assembly to which type resolution is constrained, if any.
        /// </param>
        /// <param name="callback1">
        /// The one-parameter type-resolution callback to use, if any.
        /// </param>
        /// <param name="callback3">
        /// The three-parameter type-resolution callback to use, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of types parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            Assembly assembly,
            GetTypeCallback1 callback1,
            GetTypeCallback3 callback3,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors
            )
        {
            ReturnCode code;
            Exception exception = null;

            code = GetTypeList(
                interpreter, text, assembly, callback1,
                callback3, flags, cultureInfo, ref value,
                ref errors, ref exception);

            if (code != ReturnCode.Ok)
                errors = MaybeInvokeErrorCallback(errors);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of types, using
        /// the specified type-resolution callbacks.  Upon failure, the errors
        /// encountered are stored in the <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="assembly">
        /// The assembly to which type resolution is constrained, if any.
        /// </param>
        /// <param name="callback1">
        /// The one-parameter type-resolution callback to use, if any.
        /// </param>
        /// <param name="callback3">
        /// The three-parameter type-resolution callback to use, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of types parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during type
        /// resolution.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            Assembly assembly,
            GetTypeCallback1 callback1,
            GetTypeCallback3 callback3,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if (text == null)
                goto error;

            StringList list = null;
            Result localError = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref localError) == ReturnCode.Ok)
            {
                TypeList typeList = new TypeList();

                foreach (string element in list)
                {
                    Type type = null;

                    if (GetAnyTypeViaCallback(
                            interpreter, element, null, assembly, callback1,
                            callback3, flags, cultureInfo, ref type,
                            ref errors, ref exception) == ReturnCode.Ok)
                    {
                        typeList.Add(type);
                    }
                    else
                    {
                        goto error;
                    }
                }

                //
                // NOTE: If we get this far, all elements in the list were
                //       successfully interpreted as a System.Type object.
                //
                value = typeList;

                return ReturnCode.Ok;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

        error:

            if (errors == null)
                errors = new ResultList();

            errors.Insert(0, String.Format(
                "expected type list value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of return codes.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of return codes parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetReturnCodeList(
            string text,
            CultureInfo cultureInfo,
            ref ReturnCodeList value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetReturnCodeList(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of return codes.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of return codes parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetReturnCodeList(
            string text,
            CultureInfo cultureInfo,
            ref ReturnCodeList value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        null, text, 0, Length.Invalid, true,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    ReturnCodeList returnCodeList = new ReturnCodeList();

                    foreach (string element in list)
                    {
                        ReturnCode returnCode = ReturnCode.Ok;

                        if (GetReturnCode2(
                                element, ValueFlags.AnyReturnCode,
                                cultureInfo, ref returnCode, ref error,
                                ref exception) == ReturnCode.Ok)
                        {
                            returnCodeList.Add(returnCode);
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: If we get this far, all elements in the list were
                    //       successfully interpreted as a ReturnCode object.
                    //
                    value = returnCodeList;

                    return ReturnCode.Ok;
                }
                else
                {
                    error = MaybeInvokeErrorCallback(error);
                    return ReturnCode.Error;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected return code list value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of enumerated
        /// values of the specified type.  Upon failure, the errors encountered
        /// are stored in the <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="enumType">
        /// The enumeration type that each element must conform to.
        /// </param>
        /// <param name="oldValue">
        /// The previous value used as the basis when parsing flag
        /// enumerations, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of enumerated values parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during the
        /// conversion.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetEnumList(
            Interpreter interpreter,
            string text,
            Type enumType,
            string oldValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref EnumList value,
            ref ResultList errors
            )
        {
            ReturnCode code;
            Exception exception = null;

            code = GetEnumList(
                interpreter, text, enumType, oldValue,
                flags, cultureInfo, ref value, ref errors,
                ref exception);

            if (code != ReturnCode.Ok)
                errors = MaybeInvokeErrorCallback(errors);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of enumerated
        /// values of the specified type.  Upon failure, the errors encountered
        /// are stored in the <paramref name="errors" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="enumType">
        /// The enumeration type that each element must conform to.
        /// </param>
        /// <param name="oldValue">
        /// The previous value used as the basis when parsing flag
        /// enumerations, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of enumerated values parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during the
        /// conversion.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetEnumList(
            Interpreter interpreter,
            string text,
            Type enumType,
            string oldValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref EnumList value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if (text == null)
                goto error;

            if (enumType == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid type");

                goto error;
            }

            if (!enumType.IsEnum)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "type {0} is not an enumeration",
                    MarshalOps.GetErrorTypeName(enumType)));

                goto error;
            }

            bool allowInteger = FlagOps.HasFlags(
                flags, ValueFlags.AllowInteger, true);

            bool strict = FlagOps.HasFlags(
                flags, ValueFlags.Strict, true);

            bool noCase = FlagOps.HasFlags(
                flags, ValueFlags.NoCase, true);

            StringList list = null;
            Result localError; /* REUSED */

            localError = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref localError) == ReturnCode.Ok)
            {
                EnumList enumList = new EnumList();
                bool isFlags = EnumOps.IsFlags(enumType);

                foreach (string element in list)
                {
                    object enumValue;

                    if (isFlags)
                    {
                        localError = null;

                        enumValue = EnumOps.TryParseFlags(
                            interpreter, enumType, oldValue,
                            element, cultureInfo, allowInteger,
                            strict, noCase, ref localError);
                    }
                    else
                    {
                        localError = null;

                        enumValue = EnumOps.TryParse(
                            enumType, element, allowInteger,
                            noCase, ref localError);
                    }

                    if (enumValue != null)
                    {
                        enumList.Add((Enum)enumValue);
                    }
                    else
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        goto error;
                    }
                }

                //
                // NOTE: If we get this far, all elements in the list were
                //       successfully interpreted as a System.Enum object.
                //
                value = enumList;

                return ReturnCode.Ok;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

        error:

            if (errors == null)
                errors = new ResultList();

            errors.Insert(0, String.Format(
                "expected {0} enumeration list value but got {1}",
                MarshalOps.GetErrorTypeName(enumType),
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string to a date/time
        /// value.
        /// </summary>
        /// <param name="value">
        /// The string to convert.
        /// </param>
        /// <param name="useKind">
        /// Non-zero to apply the configured date/time kind to the parsed value.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="value" />.
        /// </param>
        /// <returns>
        /// Non-zero if the conversion succeeded; otherwise, zero.
        /// </returns>
        internal static bool TryParseDateTime( /* NOTE: FOR USE BY THE Variant CLASS ONLY */
            string value,
            bool useKind,
            out DateTime dateTime
            )
        {
            return TryParseDateTime(
                Interpreter.GetActive(), value, useKind, out dateTime);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string to a date/time
        /// value, using the date/time parameters associated with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="value">
        /// The string to convert.
        /// </param>
        /// <param name="useKind">
        /// Non-zero to apply the configured date/time kind to the parsed value.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="value" />.
        /// </param>
        /// <returns>
        /// Non-zero if the conversion succeeded; otherwise, zero.
        /// </returns>
        private static bool TryParseDateTime(
            Interpreter interpreter,
            string value,
            bool useKind,
            out DateTime dateTime
            )
        {
            DateTimeKind kind;
            DateTimeStyles styles;
            IFormatProvider provider;

            MaybeGetDateTimeParameters(
                interpreter, out kind, out styles, out provider);

            return TryParseDateTime(
                value, kind, styles, provider, useKind, out dateTime);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string to a date/time
        /// value, using the specified date/time kind, styles, and format
        /// provider.
        /// </summary>
        /// <param name="value">
        /// The string to convert.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value, if requested.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="provider">
        /// The format provider used when parsing the value, if any.
        /// </param>
        /// <param name="useKind">
        /// Non-zero to apply the specified date/time kind to the parsed value.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="value" />.
        /// </param>
        /// <returns>
        /// Non-zero if the conversion succeeded; otherwise, zero.
        /// </returns>
        private static bool TryParseDateTime(
            string value,
            DateTimeKind kind,
            DateTimeStyles styles,
            IFormatProvider provider,
            bool useKind,
            out DateTime dateTime
            )
        {
            DateTime dateTimeValue = DateTime.MinValue;

            if (DateTime.TryParse(
                    value, provider, styles, out dateTimeValue))
            {
                dateTime = useKind ?
                    DateTime.SpecifyKind(dateTimeValue, kind) :
                    dateTimeValue;

                return true;
            }

            dateTime = DateTime.MinValue;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// optionally using the specified format.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetDateTime(
            string text,
            string format,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime value
            )
        {
            Result error = null;

            return GetDateTime(
                text, format, kind, styles, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// optionally using the specified format.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDateTime(
            string text,
            string format,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDateTime(
                text, format, kind, styles, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// optionally using the specified format.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDateTime(
            string text,
            string format,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                DateTime dateTime;

                if (format != null)
                {
                    if (DateTime.TryParseExact(text, format,
                            GetDateTimeFormatProvider(cultureInfo),
                            styles, out dateTime))
                    {
                        value = DateTime.SpecifyKind(dateTime, kind);
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    if (DateTime.TryParse(text,
                            GetDateTimeFormatProvider(cultureInfo),
                            styles, out dateTime))
                    {
                        value = DateTime.SpecifyKind(dateTime, kind);
                        return ReturnCode.Ok;
                    }
                }
            }

            if (format != null)
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "unable to convert date-time string {0} using format {1}",
                    FormatOps.WrapOrNull(text), FormatOps.WrapOrNull(format)));
            }
            else
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "unable to convert date-time string {0}",
                    FormatOps.WrapOrNull(text)));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// optionally falling back to interpreting it as an integer tick count.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime value
            )
        {
            Result error = null;

            return GetDateTime2(
                text, format, flags, kind, styles, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: For external use only.  May be removed in the future.
        //
        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// optionally falling back to interpreting it as an integer tick count.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        [Obsolete()]
        public static ReturnCode GetDateTime2( /* COMPAT: Eagle beta. */
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error
            )
        {
            DateTimeStyles styles;

            GetDateTimeStyles(out styles);

            Exception exception = null;

            return GetDateTime2(
                text, format, flags, kind, styles, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// optionally falling back to interpreting it as an integer tick count.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDateTime2(
                text, format, flags, kind, styles, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// optionally falling back to interpreting it as an integer tick count.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error,
            ref Exception exception
            )
        {
            //
            // NOTE: Do we want to translate the DateTime formats recognized
            //       by Tcl to those recognized by the .NET Framework?
            //
            if (FlagOps.HasFlags(flags, ValueFlags.DateTimeFormat, true))
            {
                format = FormatOps.TranslateDateTimeFormats(
                    cultureInfo, TimeZone.CurrentTimeZone, format,
                    DateTime.MinValue, TimeOps.UnixEpoch, true, false);
            }

            //
            // NOTE: First, try to parse the text as a DateTime value,
            //       possibly using the format supplied by the caller.
            //
            if (FlagOps.HasFlags(flags, ValueFlags.DateTime, true) &&
                (GetDateTime(text, format, kind, styles, cultureInfo,
                    ref value) == ReturnCode.Ok))
            {
                return ReturnCode.Ok;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.Strict, true))
            {
                //
                // NOTE: Fallback to attempting to parse the text as an
                //       integer and then using that as the ticks value
                //       for the DateTime constructor.
                //
                long longValue = 0;

                if (FlagOps.HasFlags(
                        flags, ValueFlags.WideInteger, true) &&
                    (GetWideInteger2(
                        text, flags, cultureInfo,
                        ref longValue) == ReturnCode.Ok))
                {
                    try
                    {
                        value = DateTime.SpecifyKind(
                            new DateTime(longValue), kind);

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }

            if (format != null)
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "unable to convert date-time string {0} using format {1}",
                    FormatOps.WrapOrNull(text), FormatOps.WrapOrNull(format)));
            }
            else
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "unable to convert date-time string {0}",
                    FormatOps.WrapOrNull(text)));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by SetupOps ONLY.
        //
        /// <summary>
        /// This method converts the specified string to a date/time value,
        /// using the default date/time format and culture.  Upon failure, an
        /// error message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetDateTime3(
            string text,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            ref DateTime value,
            ref Result error
            )
        {
            return GetDateTime2(
                text, ObjectOps.GetDefaultDateTimeFormat(), flags,
                kind, styles, GetDefaultCulture(), ref value,
                ref error);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable date/time
        /// value.  An empty string is converted to a null value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime? value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNullableDateTime2(
                text, format, flags, kind, styles, cultureInfo,
                ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable date/time
        /// value.  An empty string is converted to a null value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="format">
        /// The exact format string to use when parsing, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to apply to the parsed value.
        /// </param>
        /// <param name="styles">
        /// The date/time styles that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the date/time value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref DateTime? value,
            ref Result error,
            ref Exception exception
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                value = null;
                return ReturnCode.Ok;
            }

            DateTime dateTime = DateTime.MinValue;

            if (GetDateTime2(text, format,
                    flags, kind, styles, cultureInfo, ref dateTime,
                    ref error, ref exception) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            value = dateTime;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a time-span value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetTimeSpan(
            string text,
            CultureInfo cultureInfo,
            ref TimeSpan value
            )
        {
            Result error = null;

            return GetTimeSpan(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a time-span value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetTimeSpan(
            string text,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetTimeSpan(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a time-span value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetTimeSpan(
            string text,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
                if (TimeSpan.TryParse(text, out value))
                    return ReturnCode.Ok;

            error = MaybeInvokeErrorCallback(String.Format(
                "unable to convert time-span string {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a time-span value,
        /// optionally falling back to interpreting it as an integer tick count.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan value
            )
        {
            Result error = null;

            return GetTimeSpan2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a time-span value,
        /// optionally falling back to interpreting it as an integer tick count.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetTimeSpan2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a time-span value,
        /// optionally falling back to interpreting it as an integer tick count.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error,
            ref Exception exception
            )
        {
            if (FlagOps.HasFlags(
                    flags, ValueFlags.TimeSpan, true) &&
                (GetTimeSpan(
                    text, cultureInfo, ref value) == ReturnCode.Ok))
            {
                return ReturnCode.Ok;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.Strict, true))
            {
                long longValue = 0;

                if (FlagOps.HasFlags(
                        flags, ValueFlags.WideInteger, true) &&
                    (GetWideInteger2(
                        text, flags, cultureInfo,
                        ref longValue) == ReturnCode.Ok))
                {
                    try
                    {
                        value = new TimeSpan(longValue);

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "unable to convert time-span string {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable time-span
        /// value.  An empty string is converted to a null value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan? value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNullableTimeSpan2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable time-span
        /// value.  An empty string is converted to a null value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the time-span value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan? value,
            ref Result error,
            ref Exception exception
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                value = null;
                return ReturnCode.Ok;
            }

            TimeSpan timeSpan = TimeSpan.Zero;

            if (GetTimeSpan2(
                    text, flags, cultureInfo, ref timeSpan,
                    ref error, ref exception) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            value = timeSpan;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is the reserved
        /// name that represents an invalid (none) index.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <returns>
        /// Non-zero if the specified string is the reserved none name;
        /// otherwise, zero.
        /// </returns>
        private static bool IsNoneName(
            string text
            )
        {
            return SharedStringOps.Equals(
                text, noneName, SharedStringOps.SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is the reserved
        /// name that represents the starting index.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <returns>
        /// Non-zero if the specified string is the reserved start name;
        /// otherwise, zero.
        /// </returns>
        private static bool IsStartName(
            string text
            )
        {
            return SharedStringOps.Equals(
                text, startName, SharedStringOps.SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is the reserved
        /// name that represents the ending index.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <returns>
        /// Non-zero if the specified string is the reserved end name;
        /// otherwise, zero.
        /// </returns>
        private static bool IsEndName(
            string text
            )
        {
            return SharedStringOps.Equals(
                text, endName, SharedStringOps.SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is the reserved
        /// name that represents the count of elements.
        /// </summary>
        /// <param name="text">
        /// The string to check.
        /// </param>
        /// <returns>
        /// Non-zero if the specified string is the reserved count name;
        /// otherwise, zero.
        /// </returns>
        private static bool IsCountName(
            string text
            )
        {
            return SharedStringOps.Equals(
                text, countName, SharedStringOps.SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the specified index falls within the
        /// specified bounds, when strict bounds checking is enabled.
        /// </summary>
        /// <param name="index">
        /// The index value to check.
        /// </param>
        /// <param name="firstIndex">
        /// The lowest valid index, or an invalid index if there are none.
        /// </param>
        /// <param name="lastIndex">
        /// The highest valid index, or an invalid index if there are none.
        /// </param>
        /// <param name="strict">
        /// Non-zero to enforce bounds checking.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode CheckIndex(
            int index,
            int firstIndex,
            int lastIndex,
            bool strict,
            ref Result error
            )
        {
            if (!strict)
                return ReturnCode.Ok;

            if ((firstIndex == Index.Invalid) || (lastIndex == Index.Invalid))
            {
                error = MaybeInvokeErrorCallback(badIndexBoundsError);
                return ReturnCode.Error;
            }

            if ((index < firstIndex) || (index > lastIndex))
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "index {0} out-of-bounds, must be between {1} and {2}",
                    index, firstIndex, lastIndex));

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the most appropriate error result to report for
        /// a failed index conversion.
        /// </summary>
        /// <param name="text">
        /// The index string that could not be converted.
        /// </param>
        /// <param name="errors">
        /// The list of errors accumulated during the conversion, if any.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <returns>
        /// The error result to report.
        /// </returns>
        private static Result GetIndexError(
            string text,
            ResultList errors,
            ValueFlags flags
            )
        {
            if ((errors == null) ||
                FlagOps.HasFlags(flags, ValueFlags.Verbose, true))
            {
                return errors;
            }

            if (errors.Count > 0)
                return errors[errors.Count - 1];

            return String.Format(
                badIndexError2, FormatOps.WrapOrNull(text));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a contextual message, along with the specified
        /// error, to the list of index conversion errors.
        /// </summary>
        /// <param name="partIndex">
        /// The one-based position of the index string part being processed, or
        /// zero for the entire string.
        /// </param>
        /// <param name="error">
        /// The error to add, if any.
        /// </param>
        /// <param name="errors">
        /// The list of errors to append to.  When null, a new list may be
        /// created.
        /// </param>
        private static void AddIndexError(
            int partIndex,
            Result error,
            ref ResultList errors
            )
        {
            if (error != null)
            {
                if (errors == null)
                    errors = new ResultList();

                if (partIndex > 0)
                {
                    errors.Add(String.Format(
                        "while processing index string part #{0}",
                        partIndex));
                }
                else
                {
                    errors.Add("while processing index string");
                }

                errors.Add(error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a single index string part to an integer index,
        /// optionally recognizing named indexes and offsets.
        /// </summary>
        /// <param name="text">
        /// The index string part to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="count">
        /// The number of elements in the associated list.
        /// </param>
        /// <param name="partIndex">
        /// The one-based position of the index string part being processed, or
        /// zero for the entire string.
        /// </param>
        /// <param name="firstIndex">
        /// The lowest valid index, or an invalid index if there are none.
        /// </param>
        /// <param name="lastIndex">
        /// The highest valid index, or an invalid index if there are none.
        /// </param>
        /// <param name="strict">
        /// Non-zero to enforce bounds checking.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting index.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the list of errors encountered during the
        /// conversion.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetIndex(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            int count,
            int partIndex,
            int firstIndex,
            int lastIndex,
            bool strict,
            ref int value,
            ref ResultList errors
            )
        {
            if (FlagOps.HasFlags(flags, ValueFlags.NamedIndex, true))
            {
                if (IsNoneName(text))
                {
                    value = Index.Invalid;
                    return ReturnCode.Ok;
                }

                if (IsStartName(text))
                {
                    if (!strict || (firstIndex != Index.Invalid))
                    {
                        value = firstIndex;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, badIndexBoundsError,
                            ref errors);
                    }
                }

                if (IsEndName(text))
                {
                    if (!strict || (lastIndex != Index.Invalid))
                    {
                        value = lastIndex;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, badIndexBoundsError,
                            ref errors);
                    }
                }

                if (IsCountName(text))
                {
                    value = count;
                    return ReturnCode.Ok;
                }
            }

            int intValue;
            Result localError; /* REUSED */

            if (partIndex > 0)
            {
                if (FlagOps.HasFlags(flags, ValueFlags.WithOffset, true))
                {
                    intValue = 0;
                    localError = null;

                    if (GetInteger2(
                            text, flags, cultureInfo, ref intValue,
                            ref localError) == ReturnCode.Ok)
                    {
                        localError = null;

                        if (CheckIndex(
                                intValue, firstIndex, lastIndex, strict,
                                ref localError) == ReturnCode.Ok)
                        {
                            value = intValue;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            AddIndexError(
                                partIndex, localError, ref errors);
                        }
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(
                        partIndex, "unsupported index offset",
                        ref errors);
                }
            }
            else
            {
                intValue = 0;
                localError = null;

                if (FlagOps.HasFlags(flags, ValueFlags.Integer, true) &&
                    GetInteger2(
                        text, flags, cultureInfo, ref intValue,
                        ref localError) == ReturnCode.Ok)
                {
                    localError = null;

                    if (CheckIndex(
                            intValue, firstIndex, lastIndex, strict,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = intValue;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(
                        partIndex, localError, ref errors);

                    bool boolValue = false;

                    localError = null;

                    if (FlagOps.HasFlags(flags, ValueFlags.Boolean, true) &&
                        GetBoolean2(
                            text, flags, cultureInfo, ref boolValue,
                            ref localError) == ReturnCode.Ok)
                    {
                        intValue = ConversionOps.ToInt(boolValue);
                        localError = null;

                        if (CheckIndex(
                                intValue, firstIndex, lastIndex, strict,
                                ref localError) == ReturnCode.Ok)
                        {
                            value = intValue;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            AddIndexError(
                                partIndex, localError, ref errors);
                        }
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, localError, ref errors);
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an integer index,
        /// recognizing named indexes and the special "index[+-]offset" syntax.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The index string to convert.
        /// </param>
        /// <param name="count">
        /// The number of elements in the associated list.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting index.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetIndex(
            string text,
            int count,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error
            )
        {
            //
            // NOTE: This method [later] assumes that this string is not null;
            //       therefore, disallow it up-front.
            //
            if (String.IsNullOrEmpty(text))
            {
                error = MaybeInvokeErrorCallback("invalid index string");
                return ReturnCode.Error;
            }

            //
            // NOTE: Before doing anything else, figure out the first and last
            //       valid indexes [for the list].
            //
            int firstIndex;
            int lastIndex;

            if (count > 0)
            {
                firstIndex = 0;
                lastIndex = (count - 1);
            }
            else
            {
                firstIndex = Index.Invalid;
                lastIndex = Index.Invalid;
            }

            //
            // NOTE: Is strict bounds checking enabled for all index
            //       values?
            //
            bool strict = FlagOps.HasFlags(flags, ValueFlags.Strict, true);

            //
            // NOTE: First, try to interpret the entire string as one
            //       index value.
            //
            int intValue0 = 0;
            ResultList errors = null;

            if (GetIndex(
                    text, flags, cultureInfo, count, 0,
                    firstIndex, lastIndex, strict,
                    ref intValue0, ref errors) == ReturnCode.Ok)
            {
                value = intValue0;
                return ReturnCode.Ok;
            }

            //
            // NOTE: Next, try to match the regular expression pattern
            //       used for the special "index[+-]offset" syntax.
            //
            if (startEndPlusMinusIndexRegEx == null)
            {
                AddIndexError(
                    0, String.Format(badIndexError1,
                    FormatOps.WrapOrNull(text)),
                    ref errors);

                error = MaybeInvokeErrorCallback(
                    GetIndexError(text, errors, flags));

                return ReturnCode.Error;
            }

            Match match = startEndPlusMinusIndexRegEx.Match(text);

            if ((match == null) || !match.Success)
            {
                AddIndexError(
                    0, String.Format(badIndexError2,
                    FormatOps.WrapOrNull(text)),
                    ref errors);

                error = MaybeInvokeErrorCallback(
                    GetIndexError(text, errors, flags));

                return ReturnCode.Error;
            }

            //
            // NOTE: Attempt to figure out the first value.
            //
            string matchValue1 = RegExOps.GetMatchValue(match, 1);
            int intValue1 = 0;

            if (GetIndex(
                    matchValue1, flags, cultureInfo, count, 1,
                    firstIndex, lastIndex, strict, ref intValue1,
                    ref errors) != ReturnCode.Ok)
            {
                error = MaybeInvokeErrorCallback(
                    GetIndexError(matchValue1, errors, flags));

                return ReturnCode.Error;
            }

            //
            // NOTE: Attempt to figure out the second value.
            //
            string matchValue2 = RegExOps.GetMatchValue(match, 3);
            int intValue2 = 0;

            if (GetIndex(
                    matchValue2, flags, cultureInfo, count, 2,
                    firstIndex, lastIndex, strict, ref intValue2,
                    ref errors) != ReturnCode.Ok)
            {
                error = MaybeInvokeErrorCallback(
                    GetIndexError(matchValue2, errors, flags));

                return ReturnCode.Error;
            }

            //
            // NOTE: Do we need to add or subtract the first and second
            //       values?
            //
            string matchValue3 = RegExOps.GetMatchValue(match, 2);

            if (String.IsNullOrEmpty(matchValue3))
            {
                AddIndexError(
                    3, String.Format(badIndexError2,
                    FormatOps.WrapOrNull(text)),
                    ref errors);

                error = MaybeInvokeErrorCallback(
                    GetIndexError(matchValue3, errors, flags));

                return ReturnCode.Error;
            }

            char @operator = matchValue3[0];
            int intValue3;
            Result localError; /* REUSED */

            if (@operator == Characters.PlusSign)
            {
                intValue3 = intValue1 + intValue2;
                localError = null;

                if (CheckIndex(
                        intValue3, firstIndex, lastIndex, strict,
                        ref localError) == ReturnCode.Ok)
                {
                    value = intValue3;
                    return ReturnCode.Ok;
                }
                else
                {
                    AddIndexError(3, localError, ref errors);
                }
            }
            else if (@operator == Characters.MinusSign)
            {
                intValue3 = intValue1 - intValue2;
                localError = null;

                if (CheckIndex(
                        intValue3, firstIndex, lastIndex, strict,
                        ref localError) == ReturnCode.Ok)
                {
                    value = intValue3;
                    return ReturnCode.Ok;
                }
                else
                {
                    AddIndexError(3, localError, ref errors);
                }
            }
            else if (@operator == Characters.Asterisk)
            {
                intValue3 = intValue1 * intValue2;
                localError = null;

                if (CheckIndex(
                        intValue3, firstIndex, lastIndex, strict,
                        ref localError) == ReturnCode.Ok)
                {
                    value = intValue3;
                    return ReturnCode.Ok;
                }
                else
                {
                    AddIndexError(3, localError, ref errors);
                }
            }
            else if (@operator == Characters.Slash)
            {
                if (intValue2 != 0)
                {
                    intValue3 = intValue1 / intValue2;
                    localError = null;

                    if (CheckIndex(
                            intValue3, firstIndex, lastIndex, strict,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = intValue3;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(3, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(3, String.Format(
                        "cannot divide {1} by zero (via {0}) for index",
                        FormatOps.WrapOrNull(Characters.Slash),
                        intValue1), ref errors);
                }
            }
            else if (@operator == Characters.PercentSign)
            {
                if (intValue2 != 0)
                {
                    intValue3 = intValue1 % intValue2;
                    localError = null;

                    if (CheckIndex(
                            intValue3, firstIndex, lastIndex, strict,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = intValue3;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(3, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(3, String.Format(
                        "cannot divide {1} by zero (via {0}) for index",
                        FormatOps.WrapOrNull(Characters.PercentSign),
                        intValue1), ref errors);
                }
            }
            else
            {
                AddIndexError(3,
                    String.Format(badIndexOperatorError,
                    FormatOps.WrapOrNull(matchValue3)),
                    ref errors);
            }

            error = MaybeInvokeErrorCallback(
                GetIndexError(text, errors, flags));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of integer
        /// indexes.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="count">
        /// The number of elements in the associated list.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of indexes parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetIndexList(
            Interpreter interpreter,
            string text,
            int count,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IntList value
            )
        {
            Result error = null;

            return GetIndexList(
                interpreter, text, count, flags, cultureInfo,
                ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a list of integer
        /// indexes.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="count">
        /// The number of elements in the associated list.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the list of indexes parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetIndexList(
            Interpreter interpreter,
            string text,
            int count,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IntList value,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        interpreter, text, 0, Length.Invalid, true,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    IntList intList = new IntList();

                    foreach (string element in list)
                    {
                        int index = Index.Invalid;

                        if (GetIndex(
                                element, count, flags, cultureInfo,
                                ref index, ref error) == ReturnCode.Ok)
                        {
                            intList.Add(index);
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: If we get this far, all elements in the list were
                    //       successfully interpreted as indexes.
                    //
                    value = intList;

                    return ReturnCode.Ok;
                }
                else
                {
                    error = MaybeInvokeErrorCallback(error);
                    return ReturnCode.Error;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected index list value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a call frame from the specified level
        /// specification.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The level specification to resolve.
        /// </param>
        /// <param name="levelFlags">
        /// The flags that control which kinds of level specifications are
        /// permitted.
        /// </param>
        /// <param name="callStack">
        /// The call stack to search.
        /// </param>
        /// <param name="globalFrame">
        /// The global call frame.
        /// </param>
        /// <param name="currentGlobalFrame">
        /// The current global call frame.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame.
        /// </param>
        /// <param name="hasFlags">
        /// The call frame flags that a matching frame must have.
        /// </param>
        /// <param name="notHasFlags">
        /// The call frame flags that a matching frame must not have.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that a matching frame have all of the flags in
        /// <paramref name="hasFlags" />.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that a matching frame lack all of the flags in
        /// <paramref name="notHasFlags" />.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how the level specification is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="frame">
        /// Upon success, receives the resolved call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// A <see cref="FrameResult" /> value describing the outcome of the
        /// call frame search.
        /// </returns>
        internal static FrameResult GetCallFrame(
            string text,
            LevelFlags levelFlags,
            CallStack callStack,
            ICallFrame globalFrame,
            ICallFrame currentGlobalFrame,
            ICallFrame currentFrame,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ICallFrame frame,
            ref Result error
            )
        {
            bool mark = false;
            bool absolute = false;
            bool super = false;
            int level = 0;

            return GetCallFrame(
                text, levelFlags, callStack, globalFrame,
                currentGlobalFrame, currentFrame, hasFlags,
                notHasFlags, hasAll, notHasAll, valueFlags,
                cultureInfo, ref mark, ref absolute,
                ref super, ref level, ref frame, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a call frame from the specified level
        /// specification, also reporting details about the search that was
        /// performed.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The level specification to resolve.
        /// </param>
        /// <param name="levelFlags">
        /// The flags that control which kinds of level specifications are
        /// permitted.
        /// </param>
        /// <param name="callStack">
        /// The call stack to search.
        /// </param>
        /// <param name="globalFrame">
        /// The global call frame.
        /// </param>
        /// <param name="currentGlobalFrame">
        /// The current global call frame.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame.
        /// </param>
        /// <param name="hasFlags">
        /// The call frame flags that a matching frame must have.
        /// </param>
        /// <param name="notHasFlags">
        /// The call frame flags that a matching frame must not have.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that a matching frame have all of the flags in
        /// <paramref name="hasFlags" />.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that a matching frame lack all of the flags in
        /// <paramref name="notHasFlags" />.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how the level specification is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="mark">
        /// Upon success, non-zero if the resolved call frames should be marked.
        /// </param>
        /// <param name="absolute">
        /// Upon success, non-zero if an absolute, rather than relative, search
        /// was performed.
        /// </param>
        /// <param name="super">
        /// Upon success, non-zero if the outer global frame was used.
        /// </param>
        /// <param name="level">
        /// Upon success, receives the resolved level number.
        /// </param>
        /// <param name="frame">
        /// Upon success, receives the resolved call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// A <see cref="FrameResult" /> value describing the outcome of the
        /// call frame search.
        /// </returns>
        internal static FrameResult GetCallFrame(
            string text,
            LevelFlags levelFlags,
            CallStack callStack,
            ICallFrame globalFrame,
            ICallFrame currentGlobalFrame,
            ICallFrame currentFrame,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref bool mark,
            ref bool absolute,
            ref bool super,
            ref int level,
            ref ICallFrame frame,
            ref Result error
            )
        {
            FrameResult frameResult = FrameResult.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                string localText = text;
                bool localMark = false;
                bool localAbsolute = false;
                bool localSuper = false;
                int localLevel = 0;

                if (localText[0] == Characters.NumberSign)
                {
                    //
                    // HACK: Allow, through the usage of "##<integer>" notation,
                    //       traversal of all call frames, including invisible
                    //       ones.  This feature is not supported by Tcl.
                    //
                    if ((localText.Length >= 2) &&
                        (localText[1] == Characters.NumberSign))
                    {
                        if (FlagOps.HasFlags(
                                levelFlags, LevelFlags.Absolute |
                                LevelFlags.Invisible, true))
                        {
                            //
                            // NOTE: Skip the leading "##" in the level
                            //       specification.
                            //
                            localText = localText.Substring(2);

                            //
                            // NOTE: No, do not mark any call frames.
                            //
                            localMark = false;

                            //
                            // NOTE: We are going to perform an "absolute"
                            //       call frame search.
                            //
                            localAbsolute = true;

                            //
                            // NOTE: Skip using the current global frame
                            //       and use the outer global frame.
                            //
                            localSuper = true;

                            //
                            // NOTE: Make sure we include invisible call
                            //       frames (this is not Tcl compatible).
                            //
                            notHasFlags &= ~CallFrameFlags.Invisible;

                            //
                            // NOTE: Indicate to the caller that a level
                            //       specification was parsed.
                            //
                            frameResult = FrameResult.Specific;
                        }
                        else
                        {
                            goto badLevel;
                        }
                    }
                    else
                    {
                        if (FlagOps.HasFlags(
                                levelFlags, LevelFlags.Absolute, true))
                        {
                            //
                            // NOTE: Skip the leading "#" in the level
                            //       specification.
                            //
                            localText = localText.Substring(1);

                            //
                            // NOTE: Yes, we need to mark the call
                            //       frames we use.
                            //
                            localMark = true;

                            //
                            // NOTE: We are going to perform an "absolute"
                            //       call frame search.
                            //
                            localAbsolute = true;

                            //
                            // NOTE: Make use of the current global frame.
                            //
                            localSuper = false;

                            //
                            // NOTE: Indicate to the caller that a level
                            //       specification was parsed.
                            //
                            frameResult = FrameResult.Specific;
                        }
                        else
                        {
                            goto badLevel;
                        }
                    }
                }
                else if (Parser.IsInteger(localText[0], false))
                {
                    if (FlagOps.HasFlags(
                            levelFlags, LevelFlags.Relative, true))
                    {
                        //
                        // NOTE: Yes, we need to mark the call frames
                        //       we use.
                        //
                        localMark = true;

                        //
                        // NOTE: We are going to perform an "relative"
                        //       call frame search.
                        //
                        localAbsolute = false;

                        //
                        // NOTE: Make use of the current global frame.
                        //
                        localSuper = false;

                        //
                        // NOTE: Indicate to the caller that a level
                        //       specification was parsed.
                        //
                        frameResult = FrameResult.Specific;
                    }
                    else
                    {
                        goto badLevel;
                    }
                }
                else if (localText[0] == Characters.MinusSign)
                {
                    //
                    // NOTE: Indicate to the caller that a level
                    //       specification was parsed.
                    //
                    frameResult = FrameResult.Specific;

                    //
                    // NOTE: Negative levels are not supported.
                    //
                    goto badLevel;
                }
                else if (!FlagOps.HasFlags(
                        valueFlags, ValueFlags.Strict, true))
                {
                    //
                    // NOTE: Yes, we need to mark the call frames
                    //       we use.
                    //
                    localMark = true;

                    //
                    // NOTE: We are going to perform an "relative"
                    //       call frame search.
                    //
                    localAbsolute = false;

                    //
                    // NOTE: Make use of the current global frame.
                    //
                    localSuper = false;

                    //
                    // NOTE: Use the call frame of the caller if
                    //       one is not specified.
                    //
                    localLevel = 1; // upvar OR uplevel <default>

                    //
                    // NOTE: Indicate to the caller that we are
                    //       using the default level specification.
                    //
                    frameResult = FrameResult.Default;
                }
                else
                {
                    goto badLevel;
                }

                //
                // NOTE: Do we need to parse a level specification
                //       integer?
                //
                if ((frameResult != FrameResult.Default) &&
                    (GetInteger2(
                        localText, ValueFlags.AnyInteger,
                        cultureInfo, ref localLevel,
                        ref error) != ReturnCode.Ok))
                {
                    return FrameResult.Invalid;
                }

                //
                // NOTE: Now perform the actual call frame search.
                //
                if (CallFrameOps.GetOrFind(
                        callStack, globalFrame, currentGlobalFrame,
                        currentFrame, localAbsolute, localSuper,
                        localLevel, hasFlags, notHasFlags, hasAll,
                        notHasAll, ref frame) == ReturnCode.Ok)
                {
                    //
                    // NOTE: Let the caller know what kind of call
                    //       frame search we just performed.
                    //
                    mark = localMark;
                    absolute = localAbsolute;
                    super = localSuper;
                    level = localLevel;
                }
                else
                {
                    goto badLevel;
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback("invalid level");
            }

            return frameResult;

        badLevel:

            error = MaybeInvokeErrorCallback(String.Format(
                "bad level {0}",
                (frameResult == FrameResult.Default) ?
                    FormatOps.WrapOrNull(FormatOps.Level(absolute, 1)) :
                    FormatOps.WrapOrNull(text)));

            return FrameResult.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a boolean value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value
            )
        {
            Result error = null;

            return GetBoolean2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a boolean value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetBoolean2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a boolean value.  Upon failure, an error message is stored
        /// in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from the value
        /// source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetBoolean6(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error
            )
        {
            return GetBoolean3(
                getValue, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable boolean
        /// value.  An empty string is converted to a null value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool? value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNullableBoolean2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the [string is boolean] sub-command only.
        //
        /// <summary>
        /// This method determines whether the specified string is a recognized
        /// boolean keyword.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetBoolean5(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value
            )
        {
            Result error = null;
            Exception exception = null;

            return GetBoolean5(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the [string is boolean] sub-command only.
        //
        /// <summary>
        /// This method determines whether the specified string is a recognized
        /// boolean keyword.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// This parameter is not used.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetBoolean5(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo, /* NOT USED */
            ref bool value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (TryParseBooleanOnly(text, flags, ref value))
            {
                return ReturnCode.Ok;
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    ScriptOps.BadValue(null, "boolean", text,
                    Enum.GetNames(typeof(Boolean)), null, null));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string to a boolean
        /// value, recognizing only the known boolean keywords.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// Non-zero if the conversion succeeded; otherwise, zero.
        /// </returns>
        internal static bool TryParseBooleanOnly(
            string text,
            ValueFlags flags,
            ref bool value
            )
        {
            bool wasInteger = false;

            return TryParseBooleanOnly(
                text, GetComparisonType(flags), ref value, ref wasInteger);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string to a boolean
        /// value, recognizing only the known boolean keywords.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison type to use when matching keywords.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// Non-zero if the conversion succeeded; otherwise, zero.
        /// </returns>
        private static bool TryParseBooleanOnly(
            string text,
            StringComparison comparisonType,
            ref bool value
            )
        {
            bool wasInteger = false;

            return TryParseBooleanOnly(
                text, comparisonType, ref value, ref wasInteger);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string to a boolean
        /// value, recognizing only the known boolean keywords, and also reports
        /// whether the value was expressed as an integer.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison type to use when matching keywords.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="wasInteger">
        /// Upon success, non-zero if the recognized value was expressed as an
        /// integer (i.e. zero or one).
        /// </param>
        /// <returns>
        /// Non-zero if the conversion succeeded; otherwise, zero.
        /// </returns>
        private static bool TryParseBooleanOnly(
            string text,
            StringComparison comparisonType,
            ref bool value,
            ref bool wasInteger
            )
        {
            int length;

            if (!StringOps.IsNullOrEmpty(text, out length))
            {
                #region Tcl and Eagle (Part 1)
                if (SharedStringOps.Equals(
                        text, ZeroString, comparisonType))
                {
                    value = false;
                    wasInteger = true;

                    return true;
                }

                if (SharedStringOps.Equals(
                        text, OneString, comparisonType))
                {
                    value = true;
                    wasInteger = true;

                    return true;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                /* MINIMUM: "true" */
                /* MAXIMUM: "false" */
                if ((length >= 4) && (length <= 5))
                {
                    if (SharedStringOps.Equals(
                            text, "true", comparisonType))
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (SharedStringOps.Equals(
                            text, "false", comparisonType))
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                /* MINIMUM: "no" */
                /* MAXIMUM: "yes" */
                if ((length >= 2) && (length <= 3))
                {
                    if (SharedStringOps.Equals(
                            text, "yes", comparisonType))
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (SharedStringOps.Equals(
                            text, "no", comparisonType))
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Eagle Only (Part 1)
                /* MINIMUM: "enable" */
                /* MAXIMUM: "disabled" */
                if ((length >= 6) && (length <= 8))
                {
                    if (SharedStringOps.Equals(
                            text, "enable", comparisonType))
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (SharedStringOps.Equals(
                            text, "disable", comparisonType))
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (SharedStringOps.Equals(
                            text, "enabled", comparisonType))
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (SharedStringOps.Equals(
                            text, "disabled", comparisonType))
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Tcl and Eagle (Part 2)
                /* MINIMUM: "on" (less would be ambiguous) */
                /* MAXIMUM: "off" */
                if ((length >= 2) && (length <= 3))
                {
                    if (SharedStringOps.Equals(
                            text, "on", comparisonType))
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (SharedStringOps.Equals(
                            text, "off", comparisonType))
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }
                #endregion
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a boolean value.  Upon
        /// failure, an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            bool allowBooleanString = FlagOps.HasFlags(
                flags, ValueFlags.AllowBooleanString, true);

            if (allowBooleanString)
            {
                //
                // NOTE *HACK* The case-insensitive comparison type is used
                //      here for Tcl compatibility.
                //
                if (TryParseBooleanOnly( /* FAST */
                        text, SharedStringOps.SystemNoCaseComparisonType,
                        ref value))
                {
                    return ReturnCode.Ok;
                }

                if (!FlagOps.HasFlags(flags, ValueFlags.Fast, true))
                {
                    object enumValue = EnumOps.TryParse(
                        typeof(Boolean), text, true, true);

                    if (enumValue is Boolean)
                    {
                        value = ConversionOps.ToBool((Boolean)enumValue);

                        return ReturnCode.Ok;
                    }
                }

                if (FlagOps.HasFlags(flags, ValueFlags.Strict, true))
                {
                    error = MaybeInvokeErrorCallback(
                        ScriptOps.BadValue(null, "boolean", text,
                        Enum.GetNames(typeof(Boolean)), null, null));

                    return ReturnCode.Error;
                }
            }

            long longValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetWideInteger2(
                    text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok))
            {
                value = ConversionOps.ToBool(longValue);

                return ReturnCode.Ok;
            }
            else if (allowBooleanString)
            {
                error = MaybeInvokeErrorCallback(
                    ScriptOps.BadValue(null, "boolean", text,
                    Enum.GetNames(typeof(Boolean)), null,
                    ", or an integer"));
            }
            else
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "expected wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable boolean
        /// value.  An empty string is converted to a null value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />, or null when the string is empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNullableBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool? value,
            ref Result error,
            ref Exception exception
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                value = null;
                return ReturnCode.Ok;
            }

            bool boolValue = false;

            if (GetBoolean2(
                    text, flags, cultureInfo, ref boolValue,
                    ref error, ref exception) == ReturnCode.Ok)
            {
                value = boolValue;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by Engine.StringToBoolean ONLY.
        //
        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a boolean value.  Upon failure, an error message is stored
        /// in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from the value
        /// source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetBoolean3(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error
            )
        {
            if (getValue == null)
            {
                error = MaybeInvokeErrorCallback(
                    ScriptOps.BadValue(null, "boolean", null,
                    Enum.GetNames(typeof(Boolean)), null, null));

                return ReturnCode.Error;
            }

            object innerValue = getValue.Value;

            if (innerValue is ValueType)
            {
                try
                {
                    value = ConversionOps.ToBool(innerValue); /* throw */

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = MaybeInvokeErrorCallback(e);
                    return ReturnCode.Error;
                }
            }

            string text = getValue.String;

            //
            // NOTE *HACK* The case-insensitive comparison type is used
            //      here for Tcl compatibility.
            //
            if (TryParseBooleanOnly( /* FAST */
                    text, SharedStringOps.SystemNoCaseComparisonType,
                    ref value))
            {
                return ReturnCode.Ok;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.Fast, true))
            {
                object enumValue = EnumOps.TryParse(
                    typeof(Boolean), text, true, true);

                if (enumValue is Boolean)
                {
                    value = ConversionOps.ToBool((Boolean)enumValue);

                    return ReturnCode.Ok;
                }
            }

            if (FlagOps.HasFlags(flags, ValueFlags.Strict, true))
            {
                error = MaybeInvokeErrorCallback(
                    ScriptOps.BadValue(null, "boolean", text,
                    Enum.GetNames(typeof(Boolean)), null, null));

                return ReturnCode.Error;
            }

            long longValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok))
            {
                value = ConversionOps.ToBool(longValue);

                return ReturnCode.Ok;
            }
            else
            {
                double doubleValue = 0.0;

                if (FlagOps.HasFlags(
                        flags, ValueFlags.Double, true) &&
                    (GetDouble(text, flags, cultureInfo,
                        ref doubleValue) == ReturnCode.Ok))
                {
                    value = ConversionOps.ToBool(doubleValue);

                    return ReturnCode.Ok;
                }
                else
                {
                    error = MaybeInvokeErrorCallback(
                        ScriptOps.BadValue(null, "boolean", text,
                        Enum.GetNames(typeof(Boolean)), null,
                        ", or a number"));

                    return ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by SetupOps ONLY.
        //
        /// <summary>
        /// This method converts the specified string to a boolean value, using
        /// the default culture.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the boolean value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetBoolean4(
            string text,
            ValueFlags flags,
            ref bool value,
            ref Result error
            )
        {
            return GetBoolean2(
                text, flags, GetDefaultCulture(), ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a single-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSingle2(
            string text,
            CultureInfo cultureInfo,
            ref float value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            ReturnCode code = ReturnCode.Error;

            stopIndex = Index.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;
                Result localError = null; /* REUSED */

                while (length > 0)
                {
                    localError = null;

                    code = GetSingle(
                        text.Substring(0, length), cultureInfo,
                        ref value, ref localError, ref exception);

                    if (code == ReturnCode.Ok)
                        break;
                    else
                        length--;
                }

                if (length > 0)
                    //
                    // NOTE: One beyond the character we actually
                    //       succeeded at.
                    //
                    stopIndex = length;
                else if (code != ReturnCode.Ok)
                    error = MaybeInvokeErrorCallback(localError);
                else
                    error = localError; /* EXEMPT */
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a single-precision floating-point value.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating-point value
        /// parsed from the value source.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSingle(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref float value
            )
        {
            Result error = null;

            return GetSingle(getValue, cultureInfo, ref value, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a single-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating-point value
        /// parsed from the value source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetSingle(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSingle(
                getValue, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a single-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating-point value
        /// parsed from the value source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSingle(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is float)
                {
                    value = (float)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.ToSingle(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(String.Format(
                            "could not convert {0} to single",
                            FormatOps.WrapOrNull(innerValue)));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetSingle(
                        getValue.String, cultureInfo, ref value,
                        ref error, ref exception);
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected single value but got null");

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a single-precision
        /// floating-point value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetSingle(
            string text,
            CultureInfo cultureInfo,
            ref float value
            )
        {
            Result error = null;

            return GetSingle(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a single-precision
        /// floating-point value.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetSingle(
            string text,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSingle(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a single-precision
        /// floating-point value.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the single-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSingle(
            string text,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (TryLookupNamedSingle(text, ref value))
                    return ReturnCode.Ok;

                NumberStyles styles;

                GetSingleStyles(out styles);

                if (float.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected floating-point number but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a double-precision floating-point value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDouble2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value,
            ref int stopIndex
            )
        {
            Result error = null;

            return GetDouble2(
                text, flags, cultureInfo, ref value,
                ref stopIndex, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // NOTE: Used by the Option Parser.
        //
        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a big integer value.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the big integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetBigInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref BigInteger value,
            ref Result error
            )
        {
            int stopIndex = Index.Invalid;

            return GetBigInteger2(
                text, flags, cultureInfo, ref value,
                ref stopIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the Expression parser.
        //
        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a big integer value.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the big integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetBigInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref BigInteger value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetBigInteger2(
                text, flags, cultureInfo, ref value,
                ref stopIndex, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a big integer value.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the big integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetBigInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref BigInteger value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            ReturnCode code = ReturnCode.Error;

            stopIndex = Index.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;
                Result localError = null; /* REUSED */

                while (length > 0)
                {
                    localError = null;

                    code = GetBigInteger(
                        text.Substring(0, length), flags, cultureInfo,
                        ref value, ref localError, ref exception);

                    if (code == ReturnCode.Ok)
                        break;
                    else
                        length--;
                }

                if (length > 0)
                    //
                    // NOTE: One beyond the character we actually
                    //       succeeded at.
                    //
                    stopIndex = length;
                else if (code != ReturnCode.Ok)
                    error = MaybeInvokeErrorCallback(localError);
                else
                    error = localError; /* EXEMPT */
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a big integer value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the big integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetBigInteger(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref BigInteger value
            )
        {
            Result error = null;

            return GetBigInteger(
                text, flags, cultureInfo, ref value,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a big integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the big integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetBigInteger(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref BigInteger value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetBigInteger(
                text, flags, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a big integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the big integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetBigInteger(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref BigInteger value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (TryLookupNamedBigInteger(text, ref value))
                    return ReturnCode.Ok;

                if (FlagOps.HasFlags(flags, ValueFlags.AnyRadix, false))
                {
                    bool done = false;

                    if ((ParseBigIntegerWithRadixPrefix(
                            text, flags, cultureInfo, ref done,
                            ref value) == ReturnCode.Ok) && done)
                    {
                        return ReturnCode.Ok;
                    }
                }

                NumberStyles styles;

                GetBigIntegerStyles(out styles);

                if (BigInteger.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected big integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the Expression parser.
        //
        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a double-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetDouble2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble2(
                text, flags, cultureInfo, ref value,
                ref stopIndex, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a double-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDouble2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            ReturnCode code = ReturnCode.Error;

            stopIndex = Index.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;
                Result localError = null; /* REUSED */

                while (length > 0)
                {
                    localError = null;

                    code = GetDouble(
                        text.Substring(0, length), flags, cultureInfo,
                        ref value, ref localError, ref exception);

                    if (code == ReturnCode.Ok)
                        break;
                    else
                        length--;
                }

                if (length > 0)
                    //
                    // NOTE: One beyond the character we actually
                    //       succeeded at.
                    //
                    stopIndex = length;
                else if (code != ReturnCode.Ok)
                    error = MaybeInvokeErrorCallback(localError);
                else
                    error = localError; /* EXEMPT */
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a double-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from the value source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        [Obsolete()]
        public static ReturnCode GetDouble(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble(
                getValue, ValueFlags.AnyDouble, cultureInfo,
                ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a double-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from the value source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetDouble(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble(
                getValue, flags, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a double-precision floating-point value.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from the value source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDouble(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is double)
                {
                    value = (double)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.ToDouble(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(String.Format(
                            "could not convert {0} to double",
                            FormatOps.WrapOrNull(innerValue)));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetDouble(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected double value but got null");

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a double-precision
        /// floating-point value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetDouble(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value
            )
        {
            Result error = null;

            return GetDouble(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a double-precision
        /// floating-point value.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetDouble(
            string text,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble(
                text, ValueFlags.AnyDouble, cultureInfo,
                ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a double-precision
        /// floating-point value.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetDouble(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble(
                text, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a double-precision
        /// floating-point value.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the double-precision floating-point value
        /// parsed from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDouble(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (TryLookupNamedDouble(text, ref value))
                    return ReturnCode.Ok;

                if (FlagOps.HasFlags(flags, ValueFlags.AnyRadix, false))
                {
                    bool done = false;
                    long longValue = 0;

                    if ((ParseWideIntegerWithRadixPrefix(
                            text, flags, cultureInfo, ref done,
                            ref longValue) == ReturnCode.Ok) && done)
                    {
                        value = longValue;
                        return ReturnCode.Ok;
                    }
                }

                NumberStyles styles;

                GetDoubleStyles(out styles);

                if (double.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected floating-point number but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a decimal value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the decimal value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDecimal2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref decimal value,
            ref int stopIndex
            )
        {
            Result error = null;

            return GetDecimal2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a decimal value.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the decimal value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDecimal2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref decimal value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDecimal2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the longest valid prefix of the specified
        /// string to a decimal value.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the decimal value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDecimal2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref decimal value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            ReturnCode code = ReturnCode.Error;

            stopIndex = Index.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;
                Result localError = null; /* REUSED */

                while (length > 0)
                {
                    localError = null;

                    code = GetDecimal(
                        text.Substring(0, length), flags, cultureInfo,
                        ref value, ref localError, ref exception);

                    if (code == ReturnCode.Ok)
                        break;
                    else
                        length--;
                }

                if (length > 0)
                    //
                    // NOTE: One beyond the character we actually
                    //       succeeded at.
                    //
                    stopIndex = length;
                else if (code != ReturnCode.Ok)
                    error = MaybeInvokeErrorCallback(localError);
                else
                    error = localError; /* EXEMPT */
            }

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a fixed-point decimal
        /// value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the fixed-point decimal value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetDecimal(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref decimal value
            )
        {
            Result error = null;

            return GetDecimal(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a fixed-point decimal
        /// value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the fixed-point decimal value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetDecimal(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref decimal value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDecimal(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a fixed-point decimal
        /// value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the fixed-point decimal value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetDecimal(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref decimal value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (FlagOps.HasFlags(flags, ValueFlags.AnyRadix, false))
                {
                    bool done = false;
                    long longValue = 0;

                    if ((ParseWideIntegerWithRadixPrefix(
                            text, flags, cultureInfo, ref done,
                            ref longValue) == ReturnCode.Ok) && done)
                    {
                        value = longValue;
                        return ReturnCode.Ok;
                    }
                }

                NumberStyles styles;

                GetDecimalStyles(out styles);

                if (decimal.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected fixed-point number but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned wide
        /// integer value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetUnsignedWideInteger(
            string text,
            CultureInfo cultureInfo,
            ref ulong value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                NumberStyles styles;

                GetWideIntegerStyles(out styles);

                if (ulong.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected unsigned wide integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a wide integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetWideInteger(
            string text,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                NumberStyles styles;

                GetWideIntegerStyles(out styles);

                if (long.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected wide integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable wide
        /// integer value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from
        /// <paramref name="text" />, or null when <paramref name="text" /> is
        /// null or an empty string.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long? value,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                value = null;
                return ReturnCode.Ok;
            }

            long longValue = 0;

            if (GetWideInteger2(
                    text, flags, cultureInfo, ref longValue,
                    ref error) == ReturnCode.Ok)
            {
                value = longValue;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a wide integer value.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from the value
        /// source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetWideInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetWideInteger2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified value
        /// source to a wide integer value.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value source to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from the value
        /// source.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetWideInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is int)
                {
                    value = ConversionOps.ToLong((int)innerValue);

                    return ReturnCode.Ok;
                }
                else if (innerValue is long)
                {
                    value = (long)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.IsIntegral())
                    {
                        if (number.ToWideInteger(ref value))
                        {
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = MaybeInvokeErrorCallback(String.Format(
                                "could not convert {0} to wide integer",
                                FormatOps.WrapOrNull(innerValue)));

                            return ReturnCode.Error;
                        }
                    }
                }

                //
                // NOTE: Defer to normal string processing.
                //
                return GetWideInteger2(
                    getValue.String, flags, cultureInfo,
                    ref value, ref error, ref exception);
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected wide integer value but got null");

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a wide integer value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value
            )
        {
            Result error = null;

            return GetWideInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a wide integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetWideInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned wide
        /// integer value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetUnsignedWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ulong value
            )
        {
            Result error = null;

            return GetUnsignedWideInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned wide
        /// integer value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetUnsignedWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ulong value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUnsignedWideInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse the specified string as a wide
        /// integer that begins with a radix prefix, such as hexadecimal,
        /// decimal, octal, or binary.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="done">
        /// Upon return, set to true if a recognized radix prefix was found and
        /// the value was parsed from it; otherwise, set to false.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ParseWideIntegerWithRadixPrefix(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool done,
            ref long value
            )
        {
            Result error = null;

            return ParseWideIntegerWithRadixPrefix(
                text, flags, cultureInfo, ref done, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse the specified string as a wide
        /// integer that begins with a radix prefix, such as hexadecimal,
        /// decimal, octal, or binary.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="done">
        /// Upon return, set to true if a recognized radix prefix was found and
        /// the value was parsed from it; otherwise, set to false.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ParseWideIntegerWithRadixPrefix(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool done,
            ref long value,
            ref Result error
            )
        {
            string newText = text;

            if (String.IsNullOrEmpty(newText))
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "expected wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));

                return ReturnCode.Error;
            }

            ValueFlags prefixFlags = flags & ~ValueFlags.AnyRadix;
            bool negative = false;

            if (!CheckRadixPrefix(
                    newText, cultureInfo, ref prefixFlags,
                    ref newText, ref negative, ref error))
            {
                return ReturnCode.Error;
            }

            int newLength = newText.Length;
            long newValue = value;

            if (FlagOps.HasFlags(prefixFlags, ValueFlags.HexadecimalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.HexadecimalRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "hexadecimal wide integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseHexadecimal(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected hexadecimal wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.DecimalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.DecimalRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "decimal wide integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseDecimal(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected decimal wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.OctalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.OctalRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "octal wide integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseOctal(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected octal wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.BinaryRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.BinaryRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "binary wide integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseBinary(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected binary wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else
            {
                done = false;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method attempts to parse the specified string as a
        /// <see cref="BigInteger" /> that begins with a radix prefix, such as
        /// hexadecimal, decimal, octal, or binary.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="done">
        /// Upon return, set to true if a recognized radix prefix was found and
        /// the value was parsed from it; otherwise, set to false.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the <see cref="BigInteger" /> value parsed
        /// from <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ParseBigIntegerWithRadixPrefix(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool done,
            ref BigInteger value
            )
        {
            Result error = null;

            return ParseBigIntegerWithRadixPrefix(
                text, flags, cultureInfo, ref done, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse the specified string as a
        /// <see cref="BigInteger" /> that begins with a radix prefix, such as
        /// hexadecimal, decimal, octal, or binary.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="done">
        /// Upon return, set to true if a recognized radix prefix was found and
        /// the value was parsed from it; otherwise, set to false.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the <see cref="BigInteger" /> value parsed
        /// from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ParseBigIntegerWithRadixPrefix(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool done,
            ref BigInteger value,
            ref Result error
            )
        {
            string newText = text;

            if (String.IsNullOrEmpty(newText))
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "expected big integer but got {0}",
                    FormatOps.WrapOrNull(text)));

                return ReturnCode.Error;
            }

            ValueFlags prefixFlags = flags & ~ValueFlags.AnyRadix;
            bool negative = false;

            if (!CheckRadixPrefix(
                    newText, cultureInfo, ref prefixFlags,
                    ref newText, ref negative, ref error))
            {
                return ReturnCode.Error;
            }

            int newLength = newText.Length;
            BigInteger newValue = value;

            if (FlagOps.HasFlags(prefixFlags, ValueFlags.HexadecimalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.HexadecimalRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "hexadecimal big integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseHexadecimal(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected hexadecimal big integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.DecimalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.DecimalRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "decimal big integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseDecimal(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected decimal big integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.OctalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.OctalRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "octal big integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseOctal(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected octal big integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.BinaryRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.BinaryRadix, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "binary big integer format not supported");

                    return ReturnCode.Error;
                }

                if (Parser.ParseBinary(
                        newText, 0, newLength, ref newValue) == newLength)
                {
                    if (!negative || (newValue != long.MinValue))
                    {
                        value = negative ? -newValue : newValue;
                        done = true;
                        return ReturnCode.Ok;
                    }
                }

                error = MaybeInvokeErrorCallback(String.Format(
                    "expected binary big integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }
            else
            {
                done = false;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a wide integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            bool done = false;

            if (ParseWideIntegerWithRadixPrefix(
                    text, flags, cultureInfo, ref done, ref value,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (done)
                return ReturnCode.Ok;

            if (!FlagOps.HasFlags(flags, ValueFlags.DecimalRadix, true))
            {
                error = MaybeInvokeErrorCallback(
                    "decimal wide integer format not supported");

                return ReturnCode.Error;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.SignednessMask, false))
            {
                error = MaybeInvokeErrorCallback(
                    "no signedness is supported");

                return ReturnCode.Error;
            }

            ReturnCode code;
            Result localError; /* REUSED */
            ResultList errors = null;

            if (FlagOps.HasFlags(flags, ValueFlags.DefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowSigned, true))
            {
                localError = null;

                code = GetWideInteger(
                    text, cultureInfo, ref value, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                    return code;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (FlagOps.HasFlags(flags, ValueFlags.NonDefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowUnsigned, true))
            {
                ulong ulongValue = 0;

                localError = null;

                code = GetUnsignedWideInteger(
                    text, cultureInfo, ref ulongValue, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                {
                    value = ConversionOps.ToLong(ulongValue);
                    return code;
                }

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (errors != null)
            {
                error = MaybeInvokeErrorCallback(errors);
            }
            else
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "expected wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned wide
        /// integer value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned wide integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetUnsignedWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ulong value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue; /* REUSED */
            bool done = false;

            longValue = 0;

            if (ParseWideIntegerWithRadixPrefix(
                    text, flags, cultureInfo, ref done, ref longValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (done)
            {
                value = ConversionOps.ToULong(longValue);
                return ReturnCode.Ok;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.DecimalRadix, true))
            {
                error = MaybeInvokeErrorCallback(
                    "decimal wide integer format not supported");

                return ReturnCode.Error;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.SignednessMask, false))
            {
                error = MaybeInvokeErrorCallback(
                    "no signedness is supported");

                return ReturnCode.Error;
            }

            ReturnCode code;
            Result localError; /* REUSED */
            ResultList errors = null;

            if (FlagOps.HasFlags(flags, ValueFlags.DefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowUnsigned, true))
            {
                localError = null;

                code = GetUnsignedWideInteger(
                    text, cultureInfo, ref value, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                    return code;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (FlagOps.HasFlags(flags, ValueFlags.NonDefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowSigned, true))
            {
                longValue = 0;
                localError = null;

                code = GetWideInteger(
                    text, cultureInfo, ref longValue, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                {
                    value = ConversionOps.ToULong(longValue);
                    return code;
                }

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (errors != null)
            {
                error = MaybeInvokeErrorCallback(errors);
            }
            else
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "expected unsigned wide integer but got {0}",
                    FormatOps.WrapOrNull(text)));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an integral value,
        /// represented as an <see cref="INumber" /> using the narrowest
        /// integer or wide integer type able to hold the parsed value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="number">
        /// Upon success, receives the parsed value as an
        /// <see cref="INumber" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetIntegerOrWideInteger(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber number
            )
        {
            Result error = null;

            return GetIntegerOrWideInteger(
                text, flags, cultureInfo, ref number, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an integral value,
        /// represented as an <see cref="INumber" /> using the narrowest
        /// integer or wide integer type able to hold the parsed value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the parsed value as an
        /// <see cref="INumber" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetIntegerOrWideInteger(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber value,
            ref Result error
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue, ref error) == ReturnCode.Ok)
            {
                if (FlagOps.HasFlags(
                        flags, ValueFlags.WidenToUnsigned, true))
                {
                    if ((longValue >= uint.MinValue) &&
                        (longValue <= uint.MaxValue))
                    {
                        value = new Variant(ConversionOps.ToUInt(longValue));
                    }
                    else
                    {
                        value = new Variant(ConversionOps.ToULong(longValue));
                    }
                }
                else
                {
                    if ((longValue >= int.MinValue) &&
                        (longValue <= int.MaxValue))
                    {
                        value = new Variant(ConversionOps.ToInt(longValue));
                    }
                    else
                    {
                        value = new Variant(longValue);
                    }
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified strings to a match mode value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine parsing behavior.  This
        /// parameter may be null.
        /// </param>
        /// <param name="oldText">
        /// The string containing the existing flags value, if any.
        /// </param>
        /// <param name="newText">
        /// The string containing the flags value to parse and combine with
        /// the existing value.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the match mode value resulting from
        /// combining <paramref name="oldText" /> and <paramref name="newText" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetMatchMode2(
            Interpreter interpreter,
            string oldText,
            string newText,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref MatchMode value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetMatchMode2(
                interpreter, oldText, newText, flags, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified strings to a match mode value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to determine parsing behavior.  This
        /// parameter may be null.
        /// </param>
        /// <param name="oldText">
        /// The string containing the existing flags value, if any.
        /// </param>
        /// <param name="newText">
        /// The string containing the flags value to parse and combine with
        /// the existing value.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the match mode value resulting from
        /// combining <paramref name="oldText" /> and <paramref name="newText" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetMatchMode2(
            Interpreter interpreter,
            string oldText,
            string newText,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref MatchMode value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            object enumValue = EnumOps.TryParseFlags(interpreter,
                typeof(MatchMode), oldText, newText, cultureInfo, true,
                FlagOps.HasFlags(flags, ValueFlags.Strict, true), true,
                ref error);

            if (enumValue is MatchMode)
            {
                value = (MatchMode)enumValue;

                return ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: We cannot nicely fallback to using long (wide) integer
                //       value processing here for a couple reasons:
                //
                //       1.  We would have to duplicate a lot of code from
                //           the EnumOps.TryParseFlags method that deals with
                //           combining the old and new values in one of several
                //           ways.
                //
                //       2.  Long (wide) integers are already handled by the
                //           EnumOps.TryParseFlags method itself.
                //
                error = MaybeInvokeErrorCallback(error);
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a completion code
        /// value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the completion code value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetReturnCode2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ReturnCode value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetReturnCode2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a completion code
        /// value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the completion code value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetReturnCode2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ReturnCode value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            object enumValue = EnumOps.TryParse(
                typeof(ReturnCode), text, true, true);

            if (enumValue is ReturnCode)
            {
                value = (ReturnCode)enumValue;

                return ReturnCode.Ok;
            }

            long longValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetWideInteger2(
                    text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok))
            {
                value = (ReturnCode)longValue;

                return ReturnCode.Ok;
            }

            error = MaybeInvokeErrorCallback(
                ScriptOps.BadValue(null, "completion code",
                text, Enum.GetNames(typeof(ReturnCode)),
                null, ", or an integer"));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a public key token.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="value">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, receives the public key token bytes parsed from
        /// <paramref name="value" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetPublicKeyToken(
            string value,
            CultureInfo cultureInfo,
            ref byte[] publicKeyToken,
            ref Result error
            )
        {
            return RuntimeOps.GetPublicKeyToken(
                value, cultureInfo, ref publicKeyToken, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified string to a byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetByte( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetByte(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetByte(
            string text,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                NumberStyles styles;

                GetByteStyles(out styles);

                if (byte.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected byte but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a signed byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the signed byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSignedByte( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSignedByte(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a signed byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the signed byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSignedByte(
            string text,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                NumberStyles styles;

                GetByteStyles(out styles);

                if (sbyte.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected signed byte but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a byte value.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value
            )
        {
            Result error = null;

            return GetByte2(
                getValue, flags, cultureInfo, ref value, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetByte2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is byte)
                {
                    value = (byte)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.ToByte(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(String.Format(
                            "could not convert {0} to byte",
                            FormatOps.WrapOrNull(innerValue)));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetByte2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected byte value but got null");

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a byte value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value
            )
        {
            Result error = null;

            return GetByte2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetByte2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= byte.MinValue) &&
                    (longValue <= byte.MaxValue))
                {
                    value = ConversionOps.ToByte(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected byte but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified value to a signed byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the signed byte value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSignedByte2( /* NOT USED */
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSignedByte2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a signed byte value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the signed byte value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSignedByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is sbyte)
                {
                    value = (sbyte)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.ToSignedByte(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(String.Format(
                            "could not convert {0} to signed byte",
                            FormatOps.WrapOrNull(innerValue)));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetSignedByte2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected signed byte value but got null");

                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a signed byte value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the signed byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetSignedByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSignedByte2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a signed byte value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the signed byte value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetSignedByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if (FlagOps.HasFlags(
                        flags, ValueFlags.WidenToUnsigned, true))
                {
                    if ((longValue >= byte.MinValue) &&
                        (longValue <= byte.MaxValue))
                    {
                        value = ConversionOps.ToSByte(longValue);

                        return ReturnCode.Ok;
                    }
                }

                if ((longValue >= sbyte.MinValue) &&
                    (longValue <= sbyte.MaxValue))
                {
                    value = ConversionOps.ToSByte(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected signed byte but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified string to a narrow integer value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the narrow integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNarrowInteger( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                NumberStyles styles;

                GetNarrowIntegerStyles(out styles);

                if (short.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected narrow integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a narrow integer value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the narrow integer value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNarrowInteger2( /* NOT USED */
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNarrowInteger2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a narrow integer value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the narrow integer value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNarrowInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is short)
                {
                    value = (short)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.ToNarrowInteger(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(String.Format(
                            "could not convert {0} to narrow integer",
                            FormatOps.WrapOrNull(innerValue)));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetNarrowInteger2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected narrow integer value but got null");

                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a narrow integer
        /// value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the narrow integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value
            )
        {
            Result error = null;

            return GetNarrowInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a narrow integer
        /// value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the narrow integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNarrowInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a narrow integer
        /// value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the narrow integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if (FlagOps.HasFlags(
                        flags, ValueFlags.WidenToUnsigned, true))
                {
                    if ((longValue >= ushort.MinValue) &&
                        (longValue <= ushort.MaxValue))
                    {
                        value = ConversionOps.ToShort(longValue);

                        return ReturnCode.Ok;
                    }
                }

                if ((longValue >= short.MinValue) &&
                    (longValue <= short.MaxValue))
                {
                    value = ConversionOps.ToShort(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected narrow integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned narrow
        /// integer value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned narrow integer value parsed
        /// from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetUnsignedNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ushort value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUnsignedNarrowInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned narrow
        /// integer value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned narrow integer value parsed
        /// from <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetUnsignedNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ushort value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= ushort.MinValue) &&
                    (longValue <= ushort.MaxValue))
                {
                    value = ConversionOps.ToUShort(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected unsigned narrow integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified string to a character value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the character value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetCharacter( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                //
                // NOTE: *SPECIAL CASE* Try to convert a string
                //       containing a numeric value into a single
                //       character.
                //
                NumberStyles styles;

                GetWideIntegerStyles(out styles);

                long longValue = 0;

                if (long.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out longValue))
                {
                    if ((longValue >= char.MinValue) &&
                        (longValue <= char.MaxValue))
                    {
                        value = ConversionOps.ToChar(longValue);

                        return ReturnCode.Ok;
                    }
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected character but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a character value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the character value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetCharacter2( /* NOT USED */
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetCharacter2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to a character value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the character value obtained from
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetCharacter2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is char)
                {
                    value = (char)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.ToCharacter(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(String.Format(
                            "could not convert {0} to character",
                            FormatOps.WrapOrNull(innerValue)));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetCharacter2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected character value but got null");

                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a character value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the character value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetCharacter2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value
            )
        {
            Result error = null;

            return GetCharacter2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a character value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the character value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetCharacter2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetCharacter2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a character value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the character value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetCharacter2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= char.MinValue) &&
                    (longValue <= char.MaxValue))
                {
                    value = ConversionOps.ToChar(longValue);

                    return ReturnCode.Ok;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified string to a integer value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetInteger(
            string text,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                NumberStyles styles;

                GetIntegerStyles(out styles);

                if (int.TryParse(text, styles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to a nullable integer
        /// value.  When the string is null or empty, a null value is produced.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the integer value parsed from
        /// <paramref name="text" />, or null when <paramref name="text" /> is
        /// null or empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNullableInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int? value,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                value = null;
                return ReturnCode.Ok;
            }

            int intValue = 0;

            if (GetInteger2(
                    text, flags, cultureInfo, ref intValue,
                    ref error) == ReturnCode.Ok)
            {
                value = intValue;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified
        /// <see cref="IGetValue" /> object to an integer value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The object that provides the value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the integer value provided by
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetInteger2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified
        /// <see cref="IGetValue" /> object to an integer value.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="getValue">
        /// The object that provides the value to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the integer value provided by
        /// <paramref name="getValue" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is int)
                {
                    value = (int)innerValue;

                    return ReturnCode.Ok;
                }
                else if (NumberOps.HaveType(innerValue))
                {
                    INumber number = new Variant(innerValue);

                    if (number.ToInteger(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(String.Format(
                            "could not convert {0} to integer",
                            FormatOps.WrapOrNull(innerValue)));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetInteger2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "expected integer value but got null");

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an integer value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value
            )
        {
            Result error = null;

            return GetInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if (FlagOps.HasFlags(
                        flags, ValueFlags.WidenToUnsigned, true))
                {
                    if ((longValue >= uint.MinValue) &&
                        (longValue <= uint.MaxValue))
                    {
                        value = ConversionOps.ToInt(longValue);

                        return ReturnCode.Ok;
                    }
                }

                if ((longValue >= int.MinValue) &&
                    (longValue <= int.MaxValue))
                {
                    value = ConversionOps.ToInt(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned integer value.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetUnsignedInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref uint value
            )
        {
            Result error = null;

            return GetUnsignedInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetUnsignedInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref uint value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUnsignedInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string to an unsigned integer value.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to convert.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the value is parsed.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the unsigned integer value parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetUnsignedInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref uint value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= uint.MinValue) &&
                    (longValue <= uint.MaxValue))
                {
                    value = ConversionOps.ToUInt(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected unsigned integer but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an interpreter by name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the interpreter to look up.
        /// </param>
        /// <param name="type">
        /// The flags that specify which categories of interpreter to consider.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the interpreter identified by
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetInterpreter(
            Interpreter interpreter,
            string text,
            InterpreterType type,
            ref Interpreter value
            )
        {
            Result error = null;

            return GetInterpreter(
                interpreter, text, type, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an interpreter by name.  Upon failure,
        /// an error message is stored in the <paramref name="error" />
        /// parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the interpreter to look up.
        /// </param>
        /// <param name="type">
        /// The flags that specify which categories of interpreter to consider.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the interpreter identified by
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetInterpreter(
            Interpreter interpreter,
            string text,
            InterpreterType type,
            ref Interpreter value,
            ref Result error
            )
        {
            if (text == null)
            {
                error = MaybeInvokeErrorCallback(
                    "invalid interpreter name");

                return ReturnCode.Error;
            }

            if (text.Length == 0)
            {
                value = interpreter;
                return ReturnCode.Ok;
            }

            Result localError; /* REUSED */
            ResultList errors = null;

            if (FlagOps.HasFlags(type,
                    InterpreterType.Eagle | InterpreterType.Parent, true))
            {
                localError = null;

                if (GlobalState.GetInterpreter(
                        text, LookupFlags.Interpreter, null, ref value,
                        ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (FlagOps.HasFlags(type,
                    InterpreterType.Eagle | InterpreterType.Child, true))
            {
                if (interpreter != null)
                {
                    localError = null;

                    if (interpreter.GetChildInterpreter(
                            text, LookupFlags.Interpreter, FlagOps.HasFlags(
                            type, InterpreterType.Nested, true), false,
                            ref value, ref localError) == ReturnCode.Ok)
                    {
                        return ReturnCode.Ok;
                    }
                    else if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid interpreter");
                }
            }

            if (FlagOps.HasFlags(type,
                    InterpreterType.Eagle | InterpreterType.Token, true))
            {
                CultureInfo cultureInfo = null;

                if (interpreter != null)
                    cultureInfo = interpreter.InternalCultureInfo;

                ulong token = 0;

                localError = null;

                if (GetUnsignedWideInteger2(
                        text, ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                        cultureInfo, ref token, ref localError) == ReturnCode.Ok)
                {
                    localError = null;

                    value = GlobalState.GetTokenInterpreter(token,
                        ref localError);

                    if (value != null)
                    {
                        return ReturnCode.Ok;
                    }
                    else if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            error = MaybeInvokeErrorCallback(errors);
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a nested member of an object by delegating the
        /// lookup to the specified script binder.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="scriptBinder">
        /// The script binder used to perform the member lookup.
        /// </param>
        /// <param name="text">
        /// The name of the member to look up.
        /// </param>
        /// <param name="typedInstance">
        /// The typed instance whose member is being looked up.
        /// </param>
        /// <param name="memberTypes">
        /// The flags that specify which kinds of members to consider.
        /// </param>
        /// <param name="bindingFlags">
        /// The flags that control how the member lookup is performed.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted during the
        /// lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for any culture-sensitive operations.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNestedMemberViaBinder(
            IScriptBinder scriptBinder,
            string text,
            ITypedInstance typedInstance,
            MemberTypes memberTypes,
            BindingFlags bindingFlags,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedMember value,
            ref Result error
            )
        {
            if (scriptBinder == null)
            {
                error = MaybeInvokeErrorCallback(
                    "invalid script binder");

                return ReturnCode.Error;
            }

            ReturnCode code = scriptBinder.GetMember(
                text, typedInstance, memberTypes, bindingFlags,
                valueFlags, cultureInfo, ref value, ref error);

            if (code != ReturnCode.Ok)
                error = MaybeInvokeErrorCallback(error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a nested member of an object using the binder
        /// belonging to the specified interpreter.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the member to look up.
        /// </param>
        /// <param name="typedInstance">
        /// The typed instance whose member is being looked up.
        /// </param>
        /// <param name="memberTypes">
        /// The flags that specify which kinds of members to consider.
        /// </param>
        /// <param name="bindingFlags">
        /// The flags that control how the member lookup is performed.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted during the
        /// lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for any culture-sensitive operations.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNestedMember(
            Interpreter interpreter,
            string text,
            ITypedInstance typedInstance,
            MemberTypes memberTypes,
            BindingFlags bindingFlags,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedMember value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNestedMember(
                interpreter, text, typedInstance, memberTypes, bindingFlags,
                valueFlags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the resolution of a nested member of an object.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter and any caught exception is
        /// stored in the <paramref name="exception" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="text">
        /// The name of the member to look up.
        /// </param>
        /// <param name="typedInstance">
        /// The typed instance whose member is being looked up.
        /// </param>
        /// <param name="memberTypes">
        /// The flags that specify which kinds of members to consider.
        /// </param>
        /// <param name="bindingFlags">
        /// The flags that control how the member lookup is performed.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted during the
        /// lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for any culture-sensitive operations.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives any exception that was caught during the
        /// lookup.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNestedMember(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            ITypedInstance typedInstance,
            MemberTypes memberTypes,
            BindingFlags bindingFlags,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedMember value,
            ref Result error,
            ref Exception exception
            )
        {
            bool locked; /* REUSED */
            IBinder binder = null;

            if (interpreter != null)
            {
                locked = false;

                try
                {
                    interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        //
                        // NOTE: Grab the binder for the interpreter now, as we
                        //       will need it multiple times (in the loop, etc).
                        //
                        binder = interpreter.InternalBinder;
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "GetNestedMember",
                            typeof(Value).Name, false,
                            TracePriority.LockWarning,
                            interpreter.MaybeWhoHasLock());
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            if (binder != null)
            {
                ReturnCode code = GetNestedMemberViaBinder(
                    binder as IScriptBinder, text, typedInstance,
                    memberTypes, bindingFlags, valueFlags, cultureInfo,
                    ref value, ref error);

                if (code == ReturnCode.Break)
                    code = ReturnCode.Ok; // NOTE: For our caller.

                if ((code == ReturnCode.Ok) || (code == ReturnCode.Error))
                    return code;
            }

            locked = false;

            try
            {
                if (interpreter != null)
                    interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked || (interpreter == null))
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        //
                        // NOTE: We require a valid typed object instance.
                        //
                        if (typedInstance != null)
                        {
                            //
                            // NOTE: Grab the initial type from the typed instance.  This
                            //       must be valid to continue; however, the contained
                            //       object instance may be null because we do not need it
                            //       if the initial member is static.
                            //
                            Type type = typedInstance.Type;
                            object @object = typedInstance.Object;
                            IHaveObjectFlags haveObjectFlags = typedInstance as IHaveObjectFlags;

                            ObjectFlags objectFlags = (haveObjectFlags != null) ?
                                haveObjectFlags.ObjectFlags : ObjectFlags.None;

                            if (type != null)
                            {
                                if (FlagOps.HasFlags(valueFlags, ValueFlags.NoNested, true))
                                {
                                    //
                                    // NOTE: Nested member resolution has been forbidden by the
                                    //       caller.  Perform simple member name resolution.
                                    //
                                    try
                                    {
                                        //
                                        // NOTE: Construct the typed member object for use by the
                                        //       caller.
                                        //
                                        value = new TypedMember(
                                            type, ObjectFlags.None, @object, text, text,
                                            type.GetMember(text, memberTypes, bindingFlags),
                                            null);

                                        return ReturnCode.Ok;
                                    }
                                    catch (Exception e)
                                    {
                                        error = MaybeInvokeErrorCallback(e);

                                        exception = e;
                                    }
                                }
                                else
                                {
                                    //
                                    // NOTE: Finally, split apart their object or type reference
                                    //       into pieces and attempt to traverse down the actual
                                    //       object they want.
                                    //
                                    string[] parts = MarshalOps.SplitTypeName(text);

                                    if (parts != null)
                                    {
                                        //
                                        // NOTE: How many parts are there?  There must be at least
                                        //       one.
                                        //
                                        int length = parts.Length;

                                        if (length > 1)
                                        {
                                            //
                                            // NOTE: This is a little tricky.  We traverse the parts in
                                            //       the array and attempt to use each one to lookup the
                                            //       next one via Type.InvokeMember; however, the last
                                            //       time through the loop is special because we use it
                                            //       to lookup the final list of members matching the
                                            //       specified name, member types, and binding flags.
                                            //
                                            for (int index = 0; index < length; index++)
                                            {
                                                //
                                                // NOTE: Grab the parts that we may need in the body.
                                                //
                                                string lastPart = (index > 0) ? parts[index - 1] : null;
                                                string part = parts[index];

                                                //
                                                // NOTE: Are we processing anything other than the last
                                                //       part?
                                                //
                                                if ((index + 1) < length)
                                                {
                                                    //
                                                    // NOTE: At this point, the part must be valid; otherwise,
                                                    //       we cannot lookup the remaining parts that were
                                                    //       specified.
                                                    //
                                                    if (type != null)
                                                    {
                                                        try
                                                        {
                                                            //
                                                            // NOTE: Try fetching the next part as a method with zero
                                                            //       arguments, a field, or a property of the current
                                                            //       part.  If this does not work (i.e. it throws an
                                                            //       exception), we are done.  This is allowed to
                                                            //       return null unless more parts remain to be looked
                                                            //       up.
                                                            //
                                                            @object = type.InvokeMember(
                                                                part, bindingFlags | ObjectOps.GetBindingFlags(
                                                                    MetaBindingFlags.NestedObject, true),
                                                                binder as Binder, @object, null, cultureInfo);

                                                            //
                                                            // NOTE: Normally, we cannot obtain any type information
                                                            //       from a transparent proxy.
                                                            //
                                                            if (ShouldUseObjectGetType(@object, valueFlags, objectFlags))
                                                            {
                                                                //
                                                                // NOTE: Now, get the type of the object we just fetched.
                                                                //       If the object instance is invalid here then so is
                                                                //       the type.
                                                                //
                                                                type = (@object != null) ? @object.GetType() : null;
                                                            }
                                                            else
                                                            {
                                                                error = MaybeInvokeErrorCallback(String.Format(
                                                                    "could not process member part {0}, transparent proxy",
                                                                    FormatOps.WrapOrNull(part)));

                                                                return ReturnCode.Error;
                                                            }

                                                            //
                                                            // HACK: Make COM Interop objects work [slightly better].
                                                            //
                                                            if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                                                MarshalOps.IsSystemComObjectType(type))
                                                            {
                                                                TypePairDictionary<string, long> objectInterfaces =
                                                                    (interpreter != null) ?
                                                                        interpreter.ObjectInterfaces : null;

                                                                type = MarshalOps.GetTypeFromComObject(
                                                                    interpreter, text, part, @object,
                                                                    objectInterfaces, binder, cultureInfo,
                                                                    objectFlags, ref error);

                                                                if (type == null)
                                                                {
                                                                    error = MaybeInvokeErrorCallback(error);
                                                                    return ReturnCode.Error;
                                                                }
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            //
                                                            // NOTE: Failure, we are done.  Give the caller our error
                                                            //       information.
                                                            //
                                                            error = MaybeInvokeErrorCallback(e);

                                                            exception = e;

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: We cannot lookup the next part because the current
                                                        //       part is null.  This is considered a failure unless
                                                        //       the right flag is set.
                                                        //
                                                        if (FlagOps.HasFlags(
                                                                valueFlags, ValueFlags.StopOnNullMember, true))
                                                        {
                                                            value = new TypedMember(
                                                                null, ObjectFlags.None, null, lastPart,
                                                                MarshalOps.JoinTypeName(parts, index - 1),
                                                                null, null);

                                                            return ReturnCode.Continue;
                                                        }
                                                        else
                                                        {
                                                            error = MaybeInvokeErrorCallback(String.Format(
                                                                "cannot process member part {0}, " +
                                                                "previous part {1}was null",
                                                                FormatOps.WrapOrNull(part),
                                                                (lastPart != null) ? String.Format(
                                                                "part {0} ", FormatOps.WrapOrNull(
                                                                lastPart)) : String.Empty));

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: At this point, the part must be valid; otherwise,
                                                    //       we cannot lookup the remaining part that was
                                                    //       specified.
                                                    //
                                                    if (type != null)
                                                    {
                                                        //
                                                        // NOTE: This is the last pass through the loop.  The caller
                                                        //       expects an array of zero or more member info objects
                                                        //       matching the specified name, member types, and binding
                                                        //       flags to perform overload resolution on.
                                                        //
                                                        try
                                                        {
                                                            //
                                                            // NOTE: Construct the typed member object for use by the
                                                            //       caller.
                                                            //
                                                            value = new TypedMember(
                                                                type, ObjectFlags.None, @object, part,
                                                                MarshalOps.JoinTypeName(parts, index),
                                                                type.GetMember(part, memberTypes,
                                                                bindingFlags), null);

                                                            return ReturnCode.Ok;
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            error = MaybeInvokeErrorCallback(e);

                                                            exception = e;

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: We cannot lookup the next part because the current
                                                        //       part is null.  This is considered a failure unless
                                                        //       the right flag is set.
                                                        //
                                                        if (FlagOps.HasFlags(
                                                                valueFlags, ValueFlags.StopOnNullMember, true))
                                                        {
                                                            value = new TypedMember(
                                                                null, ObjectFlags.None, null, lastPart,
                                                                MarshalOps.JoinTypeName(parts, index - 1),
                                                                null, null);

                                                            return ReturnCode.Continue;
                                                        }
                                                        else
                                                        {
                                                            error = MaybeInvokeErrorCallback(String.Format(
                                                                "cannot process member part {0}, " +
                                                                "previous part {1}was null",
                                                                FormatOps.WrapOrNull(part),
                                                                (lastPart != null) ? String.Format(
                                                                "part {0} ", FormatOps.WrapOrNull(
                                                                lastPart)) : String.Empty));

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: There is only one member part.  Perform simple member
                                            //       name resolution.
                                            //
                                            try
                                            {
                                                //
                                                // NOTE: Construct the typed member object for use by the
                                                //       caller.
                                                //
                                                value = new TypedMember(
                                                    type, ObjectFlags.None, @object, text, text,
                                                    type.GetMember(text, memberTypes, bindingFlags),
                                                    null);

                                                return ReturnCode.Ok;
                                            }
                                            catch (Exception e)
                                            {
                                                error = MaybeInvokeErrorCallback(e);

                                                exception = e;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        error = MaybeInvokeErrorCallback(
                                            "could not parse member name");
                                    }
                                }
                            }
                            else
                            {
                                error = MaybeInvokeErrorCallback(
                                    "invalid type");
                            }
                        }
                        else
                        {
                            error = MaybeInvokeErrorCallback(
                                "invalid typed instance");
                        }
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(
                            "invalid member name");
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetNestedMember",
                        typeof(Value).Name, false,
                        TracePriority.LockError,
                        interpreter.MaybeWhoHasLock());

                    error = MaybeInvokeErrorCallback(
                        "could not lock interpreter");
                }
            }
            finally
            {
                if (interpreter != null)
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a nested object by delegating the lookup to the
        /// specified script binder.  Upon failure, an error message is stored
        /// in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="scriptBinder">
        /// The script binder used to perform the object lookup.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider during the lookup.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to resolve the object.
        /// </param>
        /// <param name="bindingFlags">
        /// The flags that control how the object lookup is performed.
        /// </param>
        /// <param name="objectType">
        /// The type of object to create, if any.
        /// </param>
        /// <param name="proxyType">
        /// The proxy type to use, if any.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted during the
        /// lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for any culture-sensitive operations.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved object.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNestedObjectViaBinder(
            IScriptBinder scriptBinder,
            string text,
            TypeList types,
            AppDomain appDomain,
            BindingFlags bindingFlags,
            Type objectType,
            Type proxyType,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedInstance value,
            ref Result error
            )
        {
            if (scriptBinder == null)
            {
                error = MaybeInvokeErrorCallback(
                    "invalid script binder");

                return ReturnCode.Error;
            }

            ReturnCode code = scriptBinder.GetObject(
                text, types, appDomain, bindingFlags, objectType,
                proxyType, valueFlags, cultureInfo, ref value,
                ref error);

            if (code != ReturnCode.Ok)
                error = MaybeInvokeErrorCallback(error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the runtime type of the specified
        /// object should be obtained by calling its <c>GetType</c> method.
        /// </summary>
        /// <param name="object">
        /// The object whose type is being considered.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted.
        /// </param>
        /// <param name="objectFlags">
        /// The flags associated with the object.
        /// </param>
        /// <returns>
        /// True if the <c>GetType</c> method of the object should be used to
        /// obtain its type; otherwise, false.
        /// </returns>
        private static bool ShouldUseObjectGetType(
            object @object,
            ValueFlags valueFlags,
            ObjectFlags objectFlags
            )
        {
            if (!AppDomainOps.IsTransparentProxy(@object))
                return true;

            if (FlagOps.HasFlags(
                    valueFlags, ValueFlags.AllowProxyGetType, true) ||
                FlagOps.HasFlags(
                    objectFlags, ObjectFlags.AllowProxyGetTypeMask, false))
            {
                if (FlagOps.HasFlags(
                        valueFlags, ValueFlags.ForceProxyGetType, true) ||
                    FlagOps.HasFlags(
                        objectFlags, ObjectFlags.ForceProxyGetTypeMask, false) ||
                    AppDomainOps.IsTypePresent(@object))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the type of a transparent proxy
        /// object should be determined manually.
        /// </summary>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted.
        /// </param>
        /// <param name="objectFlags">
        /// The flags associated with the object.
        /// </param>
        /// <returns>
        /// True if the proxy type should be determined manually; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldUseManualType(
            ValueFlags valueFlags,
            ObjectFlags objectFlags
            )
        {
            if (FlagOps.HasFlags(
                    valueFlags, ValueFlags.ManualProxyGetType, true) ||
                FlagOps.HasFlags(
                    objectFlags, ObjectFlags.ManualProxyGetType, false))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a nested object using the binder belonging to
        /// the specified interpreter.  Upon failure, an error message is stored
        /// in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider during the lookup.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to resolve the object.
        /// </param>
        /// <param name="bindingFlags">
        /// The flags that control how the object lookup is performed.
        /// </param>
        /// <param name="objectType">
        /// The type of object to create, if any.
        /// </param>
        /// <param name="proxyType">
        /// The proxy type to use, if any.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted during the
        /// lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for any culture-sensitive operations.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved object.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNestedObject(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            BindingFlags bindingFlags,
            Type objectType,
            Type proxyType,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedInstance value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNestedObject(
                interpreter, text, types, appDomain, bindingFlags, objectType,
                proxyType, valueFlags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the resolution of a nested object.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter and any caught exception is
        /// stored in the <paramref name="exception" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider during the lookup.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to resolve the object.
        /// </param>
        /// <param name="bindingFlags">
        /// The flags that control how the object lookup is performed.
        /// </param>
        /// <param name="objectType">
        /// The type of object to create, if any.
        /// </param>
        /// <param name="proxyType">
        /// The proxy type to use, if any.
        /// </param>
        /// <param name="valueFlags">
        /// The flags that control how values are interpreted during the
        /// lookup.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for any culture-sensitive operations.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved object.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives any exception that was caught during the
        /// lookup.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNestedObject(
            Interpreter interpreter, /* OPTIONAL */
            string text,
            TypeList types,
            AppDomain appDomain,
            BindingFlags bindingFlags,
            Type objectType,
            Type proxyType,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ITypedInstance value,
            ref Result error,
            ref Exception exception
            )
        {
            bool locked; /* REUSED */
            IBinder binder = null;

            if (interpreter != null)
            {
                locked = false;

                try
                {
                    interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        //
                        // NOTE: Grab the binder for the interpreter now, as we
                        //       will need it multiple times (in the loop, etc).
                        //
                        binder = interpreter.InternalBinder;
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "GetNestedObject",
                            typeof(Value).Name, false,
                            TracePriority.LockWarning,
                            interpreter.MaybeWhoHasLock());
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            string[] extraParts = MarshalOps.MaybePreSplitTypeName(ref text);

            if (binder != null)
            {
                ReturnCode code = GetNestedObjectViaBinder(
                    binder as IScriptBinder, text, types, appDomain,
                    bindingFlags, objectType, proxyType, valueFlags,
                    cultureInfo, ref value, ref error);

                if (code == ReturnCode.Break)
                    code = ReturnCode.Ok; // NOTE: For our caller.

                if ((code == ReturnCode.Ok) || (code == ReturnCode.Error))
                    return code;
            }

            locked = false;

            try
            {
                if (interpreter != null)
                    interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked || (interpreter == null))
                {
                    //
                    // NOTE: *WARNING* Empty opaque object handle names are allowed,
                    //       please do not change this to "!String.IsNullOrEmpty".
                    //
                    if (text != null)
                    {
                        LookupFlags lookupFlags = LookupFlags.NoVerbose;

                        if (FlagOps.HasFlags(
                                valueFlags, ValueFlags.NullForProxyType, true))
                        {
                            lookupFlags |= LookupFlags.NullForProxyType;
                        }

                        Type localObjectType = null;
                        ObjectFlags objectFlags = ObjectFlags.None;
                        object @object = null;

                        //
                        // NOTE: First, check for a verbatim object handle.
                        //
                        if ((interpreter != null) && (GetObject(
                                interpreter, text, lookupFlags, ref localObjectType,
                                ref objectFlags, ref @object) == ReturnCode.Ok))
                        {
                            //
                            // HACK: Now, if applicable, check if this object can be used
                            //       in a "safe" interpreter.
                            //
                            if ((interpreter != null) && interpreter.InternalIsSafe() &&
                                !PolicyOps.IsTrustedObject(
                                    interpreter, text, objectFlags, @object, ref error))
                            {
                                error = MaybeInvokeErrorCallback(error);
                                return ReturnCode.Error;
                            }

                            //
                            // NOTE: Get the type of the underlying object instance.  If the
                            //       object instance is invalid here then so is the type.
                            //
                            // HACK: For any types that reside in the core library assembly,
                            //       we should be OK to use them even when they represent as
                            //       a transparent proxy.  Also, allow for objects that are
                            //       specially marked.
                            //
                            if (localObjectType == null)
                            {
                                if (ShouldUseObjectGetType(@object, valueFlags, objectFlags))
                                {
                                    //
                                    // NOTE: Now, get the type of the object we just fetched.
                                    //       If the object instance is invalid here then so is
                                    //       the type.
                                    //
                                    localObjectType = (@object != null) ? @object.GetType() : null;
                                }
                                else if (ShouldUseManualType(valueFlags, objectFlags))
                                {
                                    //
                                    // NOTE: Use the type specified by the caller as fallback.
                                    //
                                    localObjectType = proxyType;
                                }
                            }

                            //
                            // HACK: Make COM Interop objects work [slightly better].
                            //
                            if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                MarshalOps.IsSystemComObjectType(localObjectType))
                            {
                                TypePairDictionary<string, long> objectInterfaces =
                                    (interpreter != null) ?
                                        interpreter.ObjectInterfaces : null;

                                localObjectType = MarshalOps.GetTypeFromComObject(
                                    interpreter, text, null, @object,
                                    objectInterfaces, binder, cultureInfo,
                                    objectFlags, ref error);

                                if (localObjectType == null)
                                {
                                    error = MaybeInvokeErrorCallback(error);
                                    return ReturnCode.Error;
                                }
                            }

                            //
                            // NOTE: Construct the typed instance object for the caller.
                            //
                            value = new TypedInstance(
                                (objectType != null) ? objectType : localObjectType,
                                objectFlags, @object, text, text, extraParts);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            //
                            // NOTE: Next, check for a verbatim [qualified] type name.
                            //
                            ResultList errors = null;

                            if (GetAnyType(
                                    interpreter, text, types, appDomain, valueFlags,
                                    cultureInfo, ref localObjectType, ref errors,
                                    ref exception) == ReturnCode.Ok)
                            {
                                //
                                // HACK: Now, if applicable, check if this type can be used
                                //       in a "safe" interpreter.
                                //
                                if ((interpreter != null) && interpreter.InternalIsSafe() &&
                                    !PolicyOps.IsTrustedType(
                                        interpreter, text, (objectType != null) ? objectType :
                                        localObjectType, valueFlags, ref error))
                                {
                                    error = MaybeInvokeErrorCallback(error);
                                    return ReturnCode.Error;
                                }

                                //
                                // NOTE: Construct the typed instance object for the caller.
                                //
                                value = new TypedInstance(
                                    (objectType != null) ? objectType : localObjectType,
                                    ObjectFlags.None, null, text, text, extraParts);

                                return ReturnCode.Ok;
                            }
                            else if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoNested, true))
                            {
                                //
                                // NOTE: Finally, split apart their object or type reference
                                //       into pieces and attempt to traverse down the actual
                                //       object they want.
                                //
                                string[] parts = MarshalOps.SplitTypeName(text);

                                if (parts != null)
                                {
                                    //
                                    // NOTE: How many parts are there?  There must be at least
                                    //       one.
                                    //
                                    int length = parts.Length;

                                    if (length > 0)
                                    {
                                        //
                                        // NOTE: This is a little tricky.  We traverse the parts in
                                        //       the array and attempt to use each one to lookup the
                                        //       next one via Type.InvokeMember; however, the first
                                        //       time through the loop is special because we use it
                                        //       to setup the anchor point for our search and we must
                                        //       consider the possibility that the caller specified a
                                        //       fully qualified type name (which we would have just
                                        //       split into pieces above).  Therefore, if the first
                                        //       part is not an opaque object handle, this forces us
                                        //       to use longest match semantics when searching for a
                                        //       type name associated with the first "logical" part.
                                        //       In summary, we cannot assume that the first logical
                                        //       part is the same as the first physical part, due to
                                        //       fully qualified type names (please refer to the
                                        //       comments below for more details).
                                        //
                                        Result localError; /* REUSED */

                                        for (int index = 0; index < length; index++)
                                        {
                                            //
                                            // NOTE: Grab the parts that we may need in the body.
                                            //
                                            string lastPart = (index > 0) ? parts[index - 1] : null;
                                            string part = parts[index];

                                            //
                                            // NOTE: Have we already tried processing the first part?
                                            //
                                            if (index > 0)
                                            {
                                                //
                                                // NOTE: At this point, the part must be valid; otherwise,
                                                //       we cannot lookup the remaining parts that were
                                                //       specified.
                                                //
                                                if (localObjectType != null)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try fetching the next part as a method with zero
                                                        //       arguments, a field, or a property of the current
                                                        //       part.  If this does not work (i.e. it throws an
                                                        //       exception), we are done.  This is allowed to
                                                        //       return null unless more parts remain to be looked
                                                        //       up.
                                                        //
                                                        @object = localObjectType.InvokeMember(
                                                            part, bindingFlags | ObjectOps.GetBindingFlags(
                                                                MetaBindingFlags.NestedObject, true),
                                                            binder as Binder, @object, null, cultureInfo);

                                                        //
                                                        // NOTE: Normally, we cannot obtain any type information
                                                        //       from a transparent proxy.
                                                        //
                                                        if (ShouldUseObjectGetType(@object, valueFlags, objectFlags))
                                                        {
                                                            //
                                                            // NOTE: Now, get the type of the object we just fetched.
                                                            //       If the object instance is invalid here then so is
                                                            //       the type.
                                                            //
                                                            localObjectType = (@object != null) ? @object.GetType() : null;
                                                        }
                                                        else if (ShouldUseManualType(valueFlags, objectFlags))
                                                        {
                                                            //
                                                            // NOTE: Use the type specified by the caller as fallback.
                                                            //
                                                            localObjectType = proxyType;
                                                        }
                                                        else
                                                        {
                                                            if (errors == null)
                                                                errors = new ResultList();

                                                            errors.Insert(0, String.Format(
                                                                "could not process object part {0}, transparent proxy",
                                                                FormatOps.WrapOrNull(part)));

                                                            error = MaybeInvokeErrorCallback(errors);

                                                            return ReturnCode.Error;
                                                        }

                                                        //
                                                        // NOTE: Reset the object flags because nested objects
                                                        //       cannot have object flags (i.e. there may be no
                                                        //       wrapper).
                                                        //
                                                        objectFlags = ObjectFlags.None;

                                                        //
                                                        // HACK: Make COM Interop objects work [slightly better].
                                                        //
                                                        if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                                            MarshalOps.IsSystemComObjectType(localObjectType))
                                                        {
                                                            TypePairDictionary<string, long> objectInterfaces =
                                                                (interpreter != null) ?
                                                                    interpreter.ObjectInterfaces : null;

                                                            localError = null;

                                                            localObjectType = MarshalOps.GetTypeFromComObject(
                                                                interpreter, text, part, @object,
                                                                objectInterfaces, binder, cultureInfo,
                                                                objectFlags, ref localError);

                                                            if (localObjectType == null)
                                                            {
                                                                if (localError != null)
                                                                {
                                                                    if (errors == null)
                                                                        errors = new ResultList();

                                                                    errors.Insert(0, localError);
                                                                }

                                                                error = MaybeInvokeErrorCallback(errors);

                                                                return ReturnCode.Error;
                                                            }
                                                        }

                                                        //
                                                        // HACK: Now, if applicable, check if this type can be used
                                                        //       in a "safe" interpreter.
                                                        //
                                                        localError = null;

                                                        if ((interpreter != null) && interpreter.InternalIsSafe() &&
                                                            !PolicyOps.IsTrustedType(
                                                                interpreter, text, localObjectType, valueFlags,
                                                                ref localError))
                                                        {
                                                            if (localError != null)
                                                            {
                                                                if (errors == null)
                                                                    errors = new ResultList();

                                                                errors.Insert(0, localError);
                                                            }

                                                            error = MaybeInvokeErrorCallback(errors);

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        //
                                                        // NOTE: Failure, we are done.  Give the caller our error
                                                        //       information.
                                                        //
                                                        if (errors == null)
                                                            errors = new ResultList();

                                                        errors.Insert(0, e);

                                                        error = MaybeInvokeErrorCallback(errors);

                                                        exception = e;

                                                        return ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: We cannot lookup the next part because the current
                                                    //       part is null.  This is considered a failure unless
                                                    //       the right flag is set.
                                                    //
                                                    if (FlagOps.HasFlags(
                                                            valueFlags, ValueFlags.StopOnNullObject, true))
                                                    {
                                                        value = new TypedInstance(
                                                            null, ObjectFlags.None, lastPart, text, null,
                                                            extraParts);

                                                        return ReturnCode.Continue;
                                                    }
                                                    else
                                                    {
                                                        if (errors == null)
                                                            errors = new ResultList();

                                                        errors.Insert(0, String.Format(
                                                            "cannot process object part {0}, " +
                                                            "previous part {1}was null",
                                                            FormatOps.WrapOrNull(part),
                                                            (lastPart != null) ? String.Format(
                                                            "part {0} ", FormatOps.WrapOrNull(
                                                            lastPart)) : String.Empty));

                                                        error = MaybeInvokeErrorCallback(errors);

                                                        return ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: First, try to lookup an object reference in the supplied
                                                //       interpreter.  We know that we do not have to perform
                                                //       longest match semantics on this value when looking it up
                                                //       as an object handle because we cheat in the marshalling
                                                //       code (i.e. we do not permit the type delimiter in opaque
                                                //       object handle values).
                                                //
                                                if ((interpreter != null) && (GetObject(
                                                        interpreter, part, lookupFlags, ref localObjectType,
                                                        ref objectFlags, ref @object) == ReturnCode.Ok))
                                                {
                                                    //
                                                    // HACK: Now, if applicable, check if this object can be used
                                                    //       in a "safe" interpreter.
                                                    //
                                                    localError = null;

                                                    if ((interpreter != null) && interpreter.InternalIsSafe() &&
                                                        !PolicyOps.IsTrustedObject(
                                                            interpreter, text, objectFlags, @object, ref localError))
                                                    {
                                                        if (localError != null)
                                                        {
                                                            if (errors == null)
                                                                errors = new ResultList();

                                                            errors.Insert(0, localError);
                                                        }

                                                        error = MaybeInvokeErrorCallback(errors);

                                                        return ReturnCode.Error;
                                                    }

                                                    //
                                                    // NOTE: Now, get the type of the object we just fetched.
                                                    //       If the object instance is invalid here then so is
                                                    //       the type.
                                                    //
                                                    // HACK: For any types that reside in the core library assembly,
                                                    //       we should be OK to use them even when they represent as
                                                    //       a transparent proxy.  Also, allow for objects that are
                                                    //       specially marked.
                                                    //
                                                    if (localObjectType == null)
                                                    {
                                                        if (ShouldUseObjectGetType(@object, valueFlags, objectFlags))
                                                        {
                                                            //
                                                            // NOTE: Now, get the type of the object we just fetched.
                                                            //       If the object instance is invalid here then so is
                                                            //       the type.
                                                            //
                                                            localObjectType = (@object != null) ? @object.GetType() : null;
                                                        }
                                                        else if (ShouldUseManualType(valueFlags, objectFlags))
                                                        {
                                                            //
                                                            // NOTE: Use the type specified by the caller as fallback.
                                                            //
                                                            localObjectType = proxyType;
                                                        }
                                                    }

                                                    //
                                                    // HACK: Make COM Interop objects work [slightly better].
                                                    //
                                                    if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                                        MarshalOps.IsSystemComObjectType(localObjectType))
                                                    {
                                                        TypePairDictionary<string, long> objectInterfaces =
                                                            (interpreter != null) ?
                                                                interpreter.ObjectInterfaces : null;

                                                        localError = null;

                                                        localObjectType = MarshalOps.GetTypeFromComObject(
                                                            interpreter, text, part, @object,
                                                            objectInterfaces, binder, cultureInfo,
                                                            objectFlags, ref localError);

                                                        if (localObjectType == null)
                                                        {
                                                            if (localError != null)
                                                            {
                                                                if (errors == null)
                                                                    errors = new ResultList();

                                                                errors.Insert(0, localError);
                                                            }

                                                            error = MaybeInvokeErrorCallback(errors);

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: Next, try to lookup a type based on one or more of
                                                    //       the parts of the object reference.  We always try
                                                    //       for the longest possible match here.
                                                    //
                                                    bool found = false;

                                                    //
                                                    // NOTE: Longest match semantics.  Start the last index for
                                                    //       this type search at the last part index and work
                                                    //       our way backwards until we find a match.  If no
                                                    //       match is found at this point, the whole operation
                                                    //       is considered a failure and we are done.
                                                    //
                                                    for (int lastIndex = length - 1; lastIndex >= index; lastIndex--)
                                                    {
                                                        string typeName = MarshalOps.JoinTypeName(
                                                            parts, index, (lastIndex - index));

                                                        if (GetAnyType(
                                                                interpreter, typeName, types, appDomain, valueFlags,
                                                                cultureInfo, ref localObjectType, ref errors,
                                                                ref exception) == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // HACK: Now, if applicable, check if this type can be used
                                                            //       in a "safe" interpreter.
                                                            //
                                                            localError = null;

                                                            if ((interpreter != null) && interpreter.InternalIsSafe() &&
                                                                !PolicyOps.IsTrustedType(
                                                                    interpreter, text, localObjectType, valueFlags,
                                                                    ref localError))
                                                            {
                                                                if (localError != null)
                                                                {
                                                                    if (errors == null)
                                                                        errors = new ResultList();

                                                                    errors.Insert(0, localError);
                                                                }

                                                                error = MaybeInvokeErrorCallback(errors);

                                                                return ReturnCode.Error;
                                                            }

                                                            //
                                                            // NOTE: Reset the object flags because types cannot have
                                                            //       object flags.
                                                            //
                                                            objectFlags = ObjectFlags.None;

                                                            //
                                                            // NOTE: Advance the index to the index we are currently on.
                                                            //       This value will be incremented at the top of the
                                                            //       outer loop prior to the next part lookup, which is
                                                            //       the desired outcome.
                                                            //
                                                            index = lastIndex;

                                                            //
                                                            // NOTE: Indicate to the block below that we found a match in
                                                            //       this loop.
                                                            //
                                                            found = true;
                                                            break;
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did we find a type to use as the current part?
                                                    //
                                                    if (!found)
                                                    {
                                                        //
                                                        // NOTE: We failed to lookup any matching type based on any of
                                                        //       the parts (within the range).  This is considered a
                                                        //       failure unless the right flag is set.
                                                        //
                                                        if (FlagOps.HasFlags(
                                                                valueFlags, ValueFlags.StopOnNullType, true))
                                                        {
                                                            value = new TypedInstance(
                                                                null, ObjectFlags.None, part, text, null,
                                                                extraParts);

                                                            return ReturnCode.Continue;
                                                        }

                                                        //
                                                        // HACK: The error message here is somewhat of a "best guess"
                                                        //       because it does not really reflect all the type names
                                                        //       we tried; however, detailed errors occur after this
                                                        //       one in the list.
                                                        //
                                                        if (errors == null)
                                                            errors = new ResultList();

                                                        errors.Insert(0, String.Format(
                                                            "object or type {0} not found",
                                                            FormatOps.WrapOrNull(part)));

                                                        error = MaybeInvokeErrorCallback(errors);

                                                        return ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }

                                        //
                                        // NOTE: If we get to this point, we know that we have succeeded;
                                        //       therefore, set the caller's object value to the final
                                        //       object reference we fetched above.
                                        //
                                        value = new TypedInstance(
                                            (objectType != null) ? objectType : localObjectType,
                                            objectFlags, @object, text, text, extraParts);

                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        errors.Insert(0, String.Format(
                                            "no object parts in {0} to process",
                                            FormatOps.WrapOrNull(text)));

                                        error = MaybeInvokeErrorCallback(errors);
                                    }
                                }
                                else
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Insert(0, String.Format(
                                        "could not parse object parts from {0}",
                                        FormatOps.WrapOrNull(text)));

                                    error = MaybeInvokeErrorCallback(errors);
                                }
                            }
                            else
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Insert(0, String.Format(
                                    "object or type {0} not found",
                                    FormatOps.WrapOrNull(text)));

                                error = MaybeInvokeErrorCallback(errors);
                            }
                        }
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(
                            "invalid object name");
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetNestedObject",
                        typeof(Value).Name, false,
                        TracePriority.LockError,
                        interpreter.MaybeWhoHasLock());

                    error = MaybeInvokeErrorCallback(
                        "could not lock interpreter");
                }
            }
            finally
            {
                if (interpreter != null)
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an object by name and returns its underlying
        /// value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the object lookup is performed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the object identified by
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetObject( /* For use by GetVariant ONLY. */
            Interpreter interpreter,
            string text,
            LookupFlags lookupFlags,
            ref object value
            )
        {
            Result error = null; /* NOT USED */

            return GetObject(
                interpreter, text, lookupFlags, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an object by name and returns its underlying
        /// value.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the object lookup is performed.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the object identified by
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            LookupFlags lookupFlags,
            ref object value,
            ref Result error
            )
        {
            bool verbose = FlagOps.HasFlags(
                lookupFlags, LookupFlags.Verbose, true);

            if (interpreter == null)
            {
                if (verbose)
                {
                    error = MaybeInvokeErrorCallback(
                        "invalid interpreter");
                }

                return ReturnCode.Error;
            }

            if (text == null)
            {
                if (verbose)
                {
                    error = MaybeInvokeErrorCallback(
                        "invalid object name");
                }

                return ReturnCode.Error;
            }

            IObject @object = null;

            if (interpreter.GetObject(
                    text, lookupFlags, ref @object,
                    ref error) == ReturnCode.Ok)
            {
                if (@object != null)
                {
                    value = @object.Value;
                    return ReturnCode.Ok;
                }
                else if (verbose)
                {
                    error = MaybeInvokeErrorCallback(String.Format(
                        "invalid wrapper for object {0}",
                        FormatOps.WrapOrNull(text)));
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(error);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an object by name and returns its underlying
        /// value, using non-verbose lookup semantics.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the object identified by
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            ref object value
            )
        {
            Type type = null;
            ObjectFlags objectFlags = ObjectFlags.None;

            return GetObject(
                interpreter, text, LookupFlags.NoVerbose,
                ref type, ref objectFlags, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an object by name and returns its type, flags,
        /// and underlying value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the object lookup is performed.
        /// </param>
        /// <param name="type">
        /// Upon success, receives the type associated with the object.
        /// </param>
        /// <param name="objectFlags">
        /// Upon success, receives the flags associated with the object.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the object identified by
        /// <paramref name="text" />.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            LookupFlags lookupFlags,
            ref Type type,
            ref ObjectFlags objectFlags,
            ref object value
            )
        {
            Result error = null;

            return GetObject(
                interpreter, text, lookupFlags, ref type,
                ref objectFlags, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up an object by name and returns its type, flags,
        /// and underlying value.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the lookup.
        /// </param>
        /// <param name="text">
        /// The name of the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the object lookup is performed.
        /// </param>
        /// <param name="type">
        /// Upon success, receives the type associated with the object.
        /// </param>
        /// <param name="objectFlags">
        /// Upon success, receives the flags associated with the object.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the object identified by
        /// <paramref name="text" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            LookupFlags lookupFlags,
            ref Type type,
            ref ObjectFlags objectFlags,
            ref object value,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                //
                // NOTE: *WARNING* Empty opaque object handle names are allowed,
                //       please do not change this to "!String.IsNullOrEmpty".
                //
                if (text != null)
                {
                    IObject @object = null;

                    if (interpreter.GetObject(
                            text, lookupFlags, ref @object,
                            ref error) == ReturnCode.Ok)
                    {
                        if (@object != null)
                        {
                            object localValue = @object.Value;
                            Type localType = null;

                            if (FlagOps.HasFlags(lookupFlags,
                                    LookupFlags.NullForProxyType, true) &&
                                AppDomainOps.IsTransparentProxy(localValue))
                            {
                                localType = null;
                            }
                            else
                            {
                                localType = @object.Type;
                            }

                            type = localType;
                            objectFlags = @object.ObjectFlags;
                            value = localValue;

                            return ReturnCode.Ok;
                        }
                        else if (FlagOps.HasFlags(
                                lookupFlags, LookupFlags.Verbose, true))
                        {
                            error = MaybeInvokeErrorCallback(String.Format(
                                "invalid wrapper for object {0}",
                                FormatOps.WrapOrNull(text)));
                        }
                    }
                    else
                    {
                        error = MaybeInvokeErrorCallback(error);
                    }
                }
                else if (FlagOps.HasFlags(
                        lookupFlags, LookupFlags.Verbose, true))
                {
                    error = MaybeInvokeErrorCallback(
                        "invalid object name");
                }
            }
            else if (FlagOps.HasFlags(
                    lookupFlags, LookupFlags.Verbose, true))
            {
                error = MaybeInvokeErrorCallback(
                    "invalid interpreter");
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string into a value
        /// of the most appropriate type, based on the specified conversion
        /// flags.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="format">
        /// The format string to use when parsing date/time values.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="kind">
        /// The <see cref="DateTimeKind" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="styles">
        /// The <see cref="DateTimeStyles" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetValue(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref object value
            )
        {
            Result error = null;

            return GetValue(
                text, format, flags, kind, styles, cultureInfo, ref value,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: For external use only.  May be removed in the future.
        //
        /// <summary>
        /// This method attempts to convert the specified string into a value
        /// of the most appropriate type, based on the specified conversion
        /// flags.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="format">
        /// The format string to use when parsing date/time values.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="kind">
        /// The <see cref="DateTimeKind" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        [Obsolete()]
        public static ReturnCode GetValue(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref object value,
            ref Result error
            )
        {
            DateTimeStyles styles;

            GetDateTimeStyles(out styles);

            Exception exception = null;

            return GetValue(
                text, format, flags, kind, styles, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string into a value
        /// of the most appropriate type, based on the specified conversion
        /// flags.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="format">
        /// The format string to use when parsing date/time values.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="kind">
        /// The <see cref="DateTimeKind" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="styles">
        /// The <see cref="DateTimeStyles" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetValue(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref object value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetValue(
                text, format, flags, kind, styles, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string into a value
        /// of the most appropriate type, based on the specified conversion
        /// flags.  Each candidate type is attempted in turn, subject to the
        /// conversion flags.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="format">
        /// The format string to use when parsing date/time values.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="kind">
        /// The <see cref="DateTimeKind" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="styles">
        /// The <see cref="DateTimeStyles" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetValue(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref object value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            byte byteValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Byte, true) &&
                (GetByte2(
                    text, flags, cultureInfo,
                    ref byteValue) == ReturnCode.Ok))
            {
                value = byteValue;

                return ReturnCode.Ok;
            }

            short shortValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.NarrowInteger, true) &&
                (GetNarrowInteger2(
                    text, flags, cultureInfo,
                    ref shortValue) == ReturnCode.Ok))
            {
                value = shortValue;

                return ReturnCode.Ok;
            }

            char charValue = Characters.Null;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Character, true) &&
                (GetCharacter2(
                    text, flags, cultureInfo,
                    ref charValue) == ReturnCode.Ok))
            {
                value = charValue;

                return ReturnCode.Ok;
            }


            int intValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Integer, true) &&
                (GetInteger2(
                    text, flags, cultureInfo,
                    ref intValue) == ReturnCode.Ok))
            {
                value = intValue;

                return ReturnCode.Ok;
            }


            long longValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetWideInteger2(
                    text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok))
            {
                value = longValue;

                return ReturnCode.Ok;
            }

            decimal decimalValue = Decimal.Zero;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Decimal, true) &&
                (GetDecimal(
                    text, flags, cultureInfo,
                    ref decimalValue) == ReturnCode.Ok))
            {
                value = decimalValue;

                return ReturnCode.Ok;
            }


            float floatValue = 0.0f;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Single, true) &&
                (GetSingle(
                    text, cultureInfo,
                    ref floatValue) == ReturnCode.Ok))
            {
                value = floatValue;

                return ReturnCode.Ok;
            }

            double doubleValue = 0.0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Double, true) &&
                (GetDouble(
                    text, flags, cultureInfo,
                    ref doubleValue) == ReturnCode.Ok))
            {
                value = doubleValue;

                return ReturnCode.Ok;
            }

            //
            // NOTE: *SPECIAL CASE*: This converts everything that looks
            //       numeric in addition to the special boolean strings
            //       (such as "true", "false", "yes", "no", etc).  Also,
            //       since the .NET Framework will never perform widening
            //       conversion from bool, this must be last among the
            //       pure numeric conversion attempts.
            //
            bool boolValue = false;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Boolean, true) &&
                (GetBoolean2(
                    text, flags, cultureInfo,
                    ref boolValue) == ReturnCode.Ok))
            {
                value = boolValue;

                return ReturnCode.Ok;
            }

            DateTime dateTime = DateTime.MinValue;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.DateTime, true) &&
                (GetDateTime2(
                    text, format, flags, kind, styles,
                    cultureInfo, ref dateTime) == ReturnCode.Ok))
            {
                value = dateTime;

                return ReturnCode.Ok;
            }

            TimeSpan timeSpan = TimeSpan.Zero;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.TimeSpan, true) &&
                (GetTimeSpan2(
                    text, flags, cultureInfo,
                    ref timeSpan) == ReturnCode.Ok))
            {
                value = timeSpan;

                return ReturnCode.Ok;
            }

            Guid guid = Guid.Empty;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Guid, true) &&
                (GetGuid(
                    text, cultureInfo,
                    ref guid) == ReturnCode.Ok))
            {
                value = guid;

                return ReturnCode.Ok;
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string into a numeric
        /// value, based on the specified conversion flags.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which numeric types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted numeric value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNumber(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber value
            )
        {
            Result error = null;

            return GetNumber(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string into a numeric
        /// value, based on the specified conversion flags.  Upon failure, an
        /// error message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which numeric types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted numeric value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetNumber(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNumber(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified string into a numeric
        /// value, based on the specified conversion flags.  Each candidate
        /// numeric type is attempted in turn, subject to the conversion flags.
        /// Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which numeric types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted numeric value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNumber(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (FlagOps.HasFlags(
                    flags, ValueFlags.IntegerOrWideInteger, true) &&
                (GetIntegerOrWideInteger(
                    text, flags, cultureInfo,
                    ref value) == ReturnCode.Ok))
            {
                return ReturnCode.Ok;
            }

            uint uintValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Integer, true) &&
                (GetUnsignedInteger2(
                    text, flags, cultureInfo,
                    ref uintValue) == ReturnCode.Ok))
            {
                value = new Variant(ConversionOps.ToInt(uintValue));

                return ReturnCode.Ok;
            }

            ulong ulongValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetUnsignedWideInteger2(
                    text, flags, cultureInfo,
                    ref ulongValue) == ReturnCode.Ok))
            {
                value = new Variant(ConversionOps.ToLong(ulongValue));

                return ReturnCode.Ok;
            }

            decimal decimalValue = Decimal.Zero;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Decimal, true) &&
                (GetDecimal(
                    text, flags, cultureInfo,
                    ref decimalValue) == ReturnCode.Ok))
            {
                value = new Variant(decimalValue);

                return ReturnCode.Ok;
            }

            double doubleValue = 0.0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Double, true) &&
                (GetDouble(
                    text, flags, cultureInfo,
                    ref doubleValue) == ReturnCode.Ok))
            {
                value = new Variant(doubleValue);

                return ReturnCode.Ok;
            }

            //
            // NOTE: *SPECIAL CASE*: This converts everything that looks
            //       numeric in addition to the special boolean strings
            //       (such as "true", "false", "yes", "no", etc).  Also,
            //       since the .NET Framework will never perform widening
            //       conversion from bool, this must be last among the
            //       pure numeric conversion attempts.
            //
            bool boolValue = false;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Boolean, true) &&
                (GetBoolean2(
                    text, flags, cultureInfo,
                    ref boolValue) == ReturnCode.Ok))
            {
                value = new Variant(boolValue);

                return ReturnCode.Ok;
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected number but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method attempts to convert the longest valid prefix of the
        /// specified string into a numeric value, based on the specified
        /// conversion flags.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which numeric types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted numeric value.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNumber2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber value,
            ref int stopIndex
            )
        {
            Result error = null;

            return GetNumber2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the longest valid prefix of the
        /// specified string into a numeric value, based on the specified
        /// conversion flags.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which numeric types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted numeric value.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNumber2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNumber2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the longest valid prefix of the
        /// specified string into a numeric value, based on the specified
        /// conversion flags.  Upon failure, an error message is stored in
        /// the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which numeric types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the converted numeric value.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetNumber2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref INumber value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            int intValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Integer, true) &&
                (GetInteger2(
                    text, flags, cultureInfo,
                    ref intValue) == ReturnCode.Ok))
            {
                value = new Variant(intValue);

                stopIndex = Index.Invalid;

                return ReturnCode.Ok;
            }

            long longValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetWideInteger2(
                    text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok))
            {
                value = new Variant(longValue);

                stopIndex = Index.Invalid;

                return ReturnCode.Ok;
            }

            decimal decimalValue = Decimal.Zero;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Decimal, true) &&
                (GetDecimal2(
                    text, flags, cultureInfo, ref decimalValue,
                    ref stopIndex) == ReturnCode.Ok))
            {
                value = new Variant(decimalValue);

                stopIndex = Index.Invalid;

                return ReturnCode.Ok;
            }

            double doubleValue = 0.0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Double, true) &&
                (GetDouble2(
                    text, flags, cultureInfo, ref doubleValue,
                    ref stopIndex) == ReturnCode.Ok))
            {
                value = new Variant(doubleValue);

                stopIndex = Index.Invalid;

                return ReturnCode.Ok;
            }

            //
            // NOTE: *SPECIAL CASE*: This converts everything that looks
            //       numeric in addition to the special boolean strings
            //       (such as "true", "false", "yes", "no", etc).  Also,
            //       since the .NET Framework will never perform widening
            //       conversion from bool, this must be last among the
            //       pure numeric conversion attempts.
            //
            bool boolValue = false;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.Boolean, true) &&
                (GetBoolean2(
                    text, flags, cultureInfo,
                    ref boolValue) == ReturnCode.Ok))
            {
                value = new Variant(boolValue);

                stopIndex = Index.Invalid;

                return ReturnCode.Ok;
            }

            error = MaybeInvokeErrorCallback(String.Format(
                "expected number but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string into a callback, suitable
        /// for use as an <see cref="ICallback" />.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="type">
        /// The type of the callback to create.  This parameter is optional and
        /// may be null, in which case the type is parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="text">
        /// The string describing the callback to create.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to use when resolving the type.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="options">
        /// The options used to control creation of the callback.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="valueFlags">
        /// This parameter is not used.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the callback.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="callback">
        /// Upon success, receives the created callback.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetCallback(
            Interpreter interpreter,       /* in */
            Type type,                     /* in: OPTIONAL */
            string text,                   /* in */
            AppDomain appDomain,           /* in: OPTIONAL */
            OptionDictionary options,      /* in: OPTIONAL */
            ValueFlags valueFlags,         /* in: NOT USED */
            CultureInfo cultureInfo,       /* in: OPTIONAL */
            IClientData clientData,        /* in: OPTIONAL */
            ref ICallback callback,        /* out */
            ref Result error               /* out */
            )
        {
            MarshalFlags marshalFlags = ObjectOps.GetDefaultMarshalFlags();

            marshalFlags |= MarshalFlags.ReturnICallback; /* NOTE: Required. */

            return GetCallback(
                interpreter, type, text, appDomain, options, valueFlags,
                cultureInfo, clientData, ref callback, ref marshalFlags,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string into a callback, suitable
        /// for use as an <see cref="ICallback" />.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="type">
        /// The type of the callback to create.  This parameter is optional and
        /// may be null, in which case the type is parsed from
        /// <paramref name="text" />.
        /// </param>
        /// <param name="text">
        /// The string describing the callback to create.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to use when resolving the type.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="options">
        /// The options used to control creation of the callback.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="valueFlags">
        /// This parameter is not used.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the callback.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="callback">
        /// Upon success, receives the created callback.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags used to control creation of the callback.
        /// Upon return, receives the marshalling flags as modified during the
        /// conversion.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetCallback(
            Interpreter interpreter,       /* in */
            Type type,                     /* in: OPTIONAL */
            string text,                   /* in */
            AppDomain appDomain,           /* in: OPTIONAL */
            OptionDictionary options,      /* in: OPTIONAL */
            ValueFlags valueFlags,         /* in: NOT USED */
            CultureInfo cultureInfo,       /* in: OPTIONAL */
            IClientData clientData,        /* in: OPTIONAL */
            ref ICallback callback,        /* out */
            ref MarshalFlags marshalFlags, /* in, out */
            ref Result error               /* out */
            )
        {
            Type localType;
            string localText;

            if (type != null)
            {
                //
                // NOTE: The type and text parameters will be passed to the
                //       ToCommandCallback method verbatim.
                //
                localType = type;
                localText = text;
            }
            else
            {
                //
                // NOTE: The text must be a two element list.  The first
                //       element must be (some kind of) a valid type name.
                //       The second element will be passed verbatim to the
                //       ToCommandCallback method as its text parameter.
                //
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        interpreter, text, 0, Length.Invalid, true,
                        ref list, ref error) != ReturnCode.Ok)
                {
                    error = MaybeInvokeErrorCallback(error);
                    return ReturnCode.Error;
                }

                if (list.Count != 2)
                {
                    error = MaybeInvokeErrorCallback(String.Format(
                        "expected two element callback list but got {0}",
                        FormatOps.WrapOrNull(text)));

                    return ReturnCode.Error;
                }

                ResultList errors = null;

                localType = null;

                if (GetAnyType(
                        interpreter, list[0], null, appDomain,
                        valueFlags, cultureInfo, ref localType,
                        ref errors) != ReturnCode.Ok)
                {
                    error = MaybeInvokeErrorCallback(errors);
                    return ReturnCode.Error;
                }

                localText = list[1];
            }

            object value = null;

            if (ConversionOps.Dynamic.ChangeType.ToCommandCallback(
                    interpreter, localType, localText, options,
                    cultureInfo, null, ref marshalFlags, ref value,
                    ref error) != ReturnCode.Ok)
            {
                error = MaybeInvokeErrorCallback(error);
                return ReturnCode.Error;
            }

            if (value is ICallback) /* NOTE: Cannot (currently) fail... */
            {
                callback = (ICallback)value;
                return ReturnCode.Ok;
            }
            else
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "wrong type {0} for callback from {1}, should be {2}",
                    MarshalOps.GetErrorValueTypeName(value),
                    FormatOps.WrapOrNull(text),
                    MarshalOps.GetErrorTypeName(typeof(ICallback))));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the default date/time parsing parameters,
        /// optionally overriding them with those configured for the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose date/time parameters should be used.
        /// This parameter is optional and may be null, in which case the
        /// default parameters are used.
        /// </param>
        /// <param name="kind">
        /// Upon return, receives the <see cref="DateTimeKind" /> to use when
        /// parsing date/time values.
        /// </param>
        /// <param name="styles">
        /// Upon return, receives the <see cref="DateTimeStyles" /> to use when
        /// parsing date/time values.
        /// </param>
        /// <param name="provider">
        /// Upon return, receives the format provider to use when parsing
        /// date/time values.
        /// </param>
        private static void MaybeGetDateTimeParameters(
            Interpreter interpreter,
            out DateTimeKind kind,
            out DateTimeStyles styles,
            out IFormatProvider provider
            )
        {
            string format = null;

            MaybeGetDateTimeParameters(
                interpreter, out format, out kind, out styles, out provider);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the default date/time parsing parameters,
        /// optionally overriding them with those configured for the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose date/time parameters should be used.
        /// This parameter is optional and may be null, in which case the
        /// default parameters are used.
        /// </param>
        /// <param name="format">
        /// Upon return, receives the format string to use when parsing
        /// date/time values.
        /// </param>
        /// <param name="kind">
        /// Upon return, receives the <see cref="DateTimeKind" /> to use when
        /// parsing date/time values.
        /// </param>
        /// <param name="styles">
        /// Upon return, receives the <see cref="DateTimeStyles" /> to use when
        /// parsing date/time values.
        /// </param>
        private static void MaybeGetDateTimeParameters(
            Interpreter interpreter,
            out string format,
            out DateTimeKind kind,
            out DateTimeStyles styles
            )
        {
            IFormatProvider provider = null;

            MaybeGetDateTimeParameters(
                interpreter, out format, out kind, out styles, out provider);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the default date/time parsing parameters,
        /// optionally overriding them with those configured for the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose date/time parameters should be used.
        /// This parameter is optional and may be null, in which case the
        /// default parameters are used.
        /// </param>
        /// <param name="format">
        /// Upon return, receives the format string to use when parsing
        /// date/time values.
        /// </param>
        /// <param name="kind">
        /// Upon return, receives the <see cref="DateTimeKind" /> to use when
        /// parsing date/time values.
        /// </param>
        /// <param name="styles">
        /// Upon return, receives the <see cref="DateTimeStyles" /> to use when
        /// parsing date/time values.
        /// </param>
        /// <param name="provider">
        /// Upon return, receives the format provider to use when parsing
        /// date/time values.
        /// </param>
        private static void MaybeGetDateTimeParameters(
            Interpreter interpreter,
            out string format,
            out DateTimeKind kind,
            out DateTimeStyles styles,
            out IFormatProvider provider
            )
        {
            format = ObjectOps.GetDefaultDateTimeFormat();
            kind = ObjectOps.GetDefaultDateTimeKind();
            styles = ObjectOps.GetDefaultDateTimeStyles();
            provider = GetDateTimeFormatProvider();

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalHardTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        try
                        {
                            format = interpreter.DateTimeFormat;
                            kind = interpreter.DateTimeKind;
                            styles = interpreter.DateTimeStyles;
                            provider = interpreter.CultureInfo;
                        }
                        catch (Exception e)
                        {
                            DebugOps.Complain(
                                interpreter, ReturnCode.Error, e);
                        }
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "MaybeGetDateTimeParameters",
                            typeof(Value).Name, false,
                            TracePriority.LockWarning,
                            interpreter.MaybeWhoHasLock());
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the value provided by the specified
        /// <see cref="IGetValue" /> into a variant, attempting numeric,
        /// date/time, time span, list, and string interpretations as
        /// appropriate.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="getValue">
        /// The object that provides the value to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting variant.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetVariant(
            Interpreter interpreter,
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IVariant value,
            ref Result error
            )
        {
            if (getValue == null)
            {
                error = MaybeInvokeErrorCallback(
                    "expected variant value but got null");

                return ReturnCode.Error;
            }

            object innerValue = getValue.Value;

            if (NumberOps.HaveType(innerValue))
            {
                value = new Variant(innerValue);

                return ReturnCode.Ok;
            }
            else if (innerValue is DateTime)
            {
                value = new Variant((DateTime)innerValue);

                return ReturnCode.Ok;
            }
            else if (innerValue is TimeSpan)
            {
                value = new Variant((TimeSpan)innerValue);

                return ReturnCode.Ok;
            }
            else
            {
                //
                // BUGFIX: Only use the StringList internal value if the
                //         element count is NOT equal to one; Otherwise we
                //         would never attempt to convert a valid number
                //         that just so happens to be "contained" within a
                //         list (e.g. "65756") to the actual numeric type.
                //         We are [ab]using the knowledge that *NO* valid
                //         number of any kind may contain a space (or be an
                //         empty string).
                //
                StringList list = innerValue as StringList;

                if ((list != null) && (list.Count != 1))
                {
                    value = new Variant(list);

                    return ReturnCode.Ok;
                }
                else
                {
                    //
                    // NOTE: Fallback to normal string-based processing.
                    //
                    ResultList errors = null;
                    string stringValue = getValue.String;
                    object objectValue = null;
                    Result localError; /* REUSED */

                    localError = null;

                    if (GetNumeric(interpreter,
                            stringValue, flags, cultureInfo, ref objectValue,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = new Variant(objectValue);

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: Record the error message provided by the
                        //       GetNumeric method, if any.
                        //
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        //
                        // NOTE: Mask off the types that should have already
                        //       been handled by the GetNumeric call (above).
                        //
                        flags &= ~ValueFlags.NumericMask;

                        //
                        // NOTE: Reset the error message so we can determine
                        //       if *this* method actually set it.
                        //
                        localError = null;

                        //
                        // NOTE: Attempt to obtain DateTime related settings
                        //       from the interpreter.
                        //
                        string format;
                        DateTimeKind kind;
                        DateTimeStyles styles;

                        MaybeGetDateTimeParameters(
                            interpreter, out format, out kind, out styles);

                        if (GetVariant(
                                interpreter, stringValue, format, flags,
                                kind, styles, cultureInfo, ref value,
                                ref localError) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                        else if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        error = MaybeInvokeErrorCallback(errors);
                        return ReturnCode.Error;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For ConversionOps.Dynamic.ChangeType.ToVariant USE ONLY.
        //
        /// <summary>
        /// This method converts the specified string into a variant, attempting
        /// numeric, date/time, time span, and string interpretations as
        /// appropriate.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting variant.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetVariant(
            Interpreter interpreter,
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IVariant value,
            ref Result error
            )
        {
            string format;
            DateTimeKind kind;
            DateTimeStyles styles;

            MaybeGetDateTimeParameters(
                interpreter, out format, out kind, out styles);

            return GetVariant(
                interpreter, text, format, flags, kind, styles,
                cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string into a variant, attempting
        /// numeric, date/time, time span, and string interpretations as
        /// appropriate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="format">
        /// The format string to use when parsing date/time values.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="kind">
        /// The <see cref="DateTimeKind" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="styles">
        /// The <see cref="DateTimeStyles" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting variant.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetVariant( /* FOR [string] USE ONLY. */
            Interpreter interpreter,
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref IVariant value
            )
        {
            Result error = null;

            return GetVariant(
                interpreter, text, format, flags, kind, styles, cultureInfo,
                ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string into a variant, attempting
        /// object, numeric, date/time, time span, and string interpretations as
        /// appropriate.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="format">
        /// The format string to use when parsing date/time values.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="kind">
        /// The <see cref="DateTimeKind" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="styles">
        /// The <see cref="DateTimeStyles" /> to use when parsing date/time
        /// values.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting variant.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetVariant(
            Interpreter interpreter,
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            DateTimeStyles styles,
            CultureInfo cultureInfo,
            ref IVariant value,
            ref Result error
            )
        {
            object @object = null;

            if (FlagOps.HasFlags(flags, ValueFlags.Object, true) &&
                (GetObject(
                    interpreter, text, LookupFlags.NoVerbose,
                    ref @object) == ReturnCode.Ok) &&
                NumberOps.HaveType(@object))
            {
                try
                {
                    value = new Variant(@object); /* throw */
                }
                catch (Exception e)
                {
                    //
                    // HACK: It should not be possible to get into this
                    //       catch block.
                    //
                    error = MaybeInvokeErrorCallback(e);
                    return ReturnCode.Error;
                }
            }
            else
            {
                INumber number = null;

                if (FlagOps.HasFlags(flags, ValueFlags.Number, true) &&
                    (GetNumber(
                        text, flags, cultureInfo,
                        ref number) == ReturnCode.Ok))
                {
                    value = new Variant(number);
                }
                else
                {
                    DateTime dateTime = DateTime.MinValue;

                    if (FlagOps.HasFlags(flags, ValueFlags.DateTime, true) &&
                        (GetDateTime2(
                            text, format, flags, kind, styles, cultureInfo,
                            ref dateTime) == ReturnCode.Ok))
                    {
                        value = new Variant(dateTime);
                    }
                    else
                    {
                        TimeSpan timeSpan = TimeSpan.Zero;

                        if (FlagOps.HasFlags(flags, ValueFlags.TimeSpan, true) &&
                            (GetTimeSpan2(
                                text, flags, cultureInfo,
                                ref timeSpan) == ReturnCode.Ok))
                        {
                            value = new Variant(timeSpan);
                        }
                        else
                        {
                            //
                            // NOTE: Cannot parse as list, use string.
                            //
                            value = new Variant(text);
                        }
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified string into a variant,
        /// attempting a numeric interpretation and falling back to a string
        /// interpretation as appropriate.  Upon failure, an error message is
        /// stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting variant.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetVariant2( /* NOT USED */
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IVariant value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetVariant2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string into a variant,
        /// attempting a numeric interpretation and falling back to a string
        /// interpretation as appropriate.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which value types are attempted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resulting variant.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, receives the index one character past the last
        /// character of <paramref name="text" /> that was successfully
        /// parsed.
        /// </param>
        /// <param name="error">
        /// This parameter is not used.
        /// </param>
        /// <param name="exception">
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetVariant2( /* NOT USED */
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IVariant value,
            ref int stopIndex,
            ref Result error,       /* NOT USED */
            ref Exception exception /* NOT USED */
            )
        {
            INumber number = null;

            if (GetNumber2(text, flags, cultureInfo,
                    ref number, ref stopIndex) == ReturnCode.Ok)
            {
                value = new Variant(number);

                stopIndex = Index.Invalid;
            }
            else
            {
                value = new Variant(text);

                stopIndex = Index.Invalid;
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method coerces both of the specified operands to the string
        /// type, as required by the specified operator or function.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="identifierName">
        /// The operator or function that the operands belong to.
        /// </param>
        /// <param name="variant1">
        /// The first operand to be coerced.
        /// </param>
        /// <param name="variant2">
        /// The second operand to be coerced.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode FixupStringVariants(
            IIdentifierName identifierName,
            IVariant variant1,
            IVariant variant2,
            ref Result error
            )
        {
            //
            // NOTE: Perform type-promotion/coercion on one or both operands based on
            //       the allowed types for this operator or function...
            //
            if ((variant1 != null) && (variant2 != null))
            {
                if (variant1.ConvertTo(TypeCode.String) &&
                    variant2.ConvertTo(TypeCode.String))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = MaybeInvokeErrorCallback(String.Format(
                        "failed to convert operand to type {0}",
                        MarshalOps.GetErrorTypeName(typeof(string))));

                    return ReturnCode.Error;
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "one or more operands for operator {0} are invalid",
                    FormatOps.IdentifierName(identifierName)));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the operands for the specified operator from
        /// the provided argument list, converting each into a variant.  Upon
        /// failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="operator">
        /// The operator whose operands are being extracted.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments containing the operator and its operands.
        /// </param>
        /// <param name="flags">
        /// The flags used to control conversion of both operands.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the resulting operands should be treated as read-only.
        /// </param>
        /// <param name="operand1">
        /// Upon success, receives the first operand.
        /// </param>
        /// <param name="operand2">
        /// Upon success, receives the second operand.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetOperandsFromArguments(
            Interpreter interpreter,
            IOperator @operator,
            ArgumentList arguments,
            ValueFlags flags,
            CultureInfo cultureInfo,
            bool readOnly,
            ref IVariant operand1,
            ref IVariant operand2,
            ref Result error
            )
        {
            return GetOperandsFromArguments(
                interpreter, @operator, arguments, flags, flags,
                cultureInfo, readOnly, ref operand1, ref operand2,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the operands for the specified operator from
        /// the provided argument list, converting each into a variant using
        /// separate conversion flags for each operand.  Upon failure, an error
        /// message is stored in the <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for the conversion.
        /// </param>
        /// <param name="operator">
        /// The operator whose operands are being extracted.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments containing the operator and its operands.
        /// </param>
        /// <param name="flags1">
        /// The flags used to control conversion of the first operand.
        /// </param>
        /// <param name="flags2">
        /// The flags used to control conversion of the second operand.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the resulting operands should be treated as read-only.
        /// </param>
        /// <param name="operand1">
        /// Upon success, receives the first operand.
        /// </param>
        /// <param name="operand2">
        /// Upon success, receives the second operand.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode GetOperandsFromArguments(
            Interpreter interpreter,
            IOperator @operator,
            ArgumentList arguments,
            ValueFlags flags1,
            ValueFlags flags2,
            CultureInfo cultureInfo,
            bool readOnly,
            ref IVariant operand1,
            ref IVariant operand2,
            ref Result error
            )
        {
            ReturnCode code;

            if (@operator != null)
            {
                if (arguments != null)
                {
                    //
                    // NOTE: Must be "operator arg ?arg?" unless number of operands is
                    //       less than zero (i.e. those values are reserved for special
                    //       cases).
                    //
                    int operands = @operator.Operands;
                    int argumentCount = arguments.Count;

                    if ((operands < 0) || (argumentCount == (operands + 1)))
                    {
                        code = ReturnCode.Ok;

                        IVariant localOperand1 = null;

                        if ((code == ReturnCode.Ok) && (argumentCount >= 2))
                        {
                            if (flags1 == ValueFlags.String)
                            {
                                string operand1String = arguments[1];

                                if ((operand1String == null) && !FlagOps.HasFlags(
                                        flags1, ValueFlags.AllowNull, true))
                                {
                                    operand1String = String.Empty;
                                }

                                localOperand1 = new Variant(operand1String);
                            }
                            else if (flags1 == ValueFlags.List)
                            {
                                Argument operand1Argument = arguments[1];

                                if ((operand1Argument == null) && !FlagOps.HasFlags(
                                        flags1, ValueFlags.AllowNull, true))
                                {
                                    operand1Argument = Argument.Empty;
                                }

                                StringList operand1List = null;

                                code = ListOps.GetOrCopyOrSplitList(
                                    interpreter, operand1Argument, readOnly,
                                    ref operand1List, ref error);

                                if (code == ReturnCode.Ok)
                                    localOperand1 = new Variant(operand1List);
                                else
                                    error = MaybeInvokeErrorCallback(error);
                            }
                            else
                            {
                                IGetValue operand1GetValue = arguments[1];

                                code = GetVariant(
                                    interpreter, operand1GetValue, flags1,
                                    cultureInfo, ref localOperand1, ref error);

                                if (code != ReturnCode.Ok)
                                {
                                    error = MaybeInvokeErrorCallback(
                                        String.Format("operand1: {0}", error));
                                }
                            }
                        }

                        IVariant localOperand2 = null;

                        if ((code == ReturnCode.Ok) && (argumentCount >= 3))
                        {
                            if (flags2 == ValueFlags.String)
                            {
                                string operand2String = arguments[2];

                                if ((operand2String == null) && !FlagOps.HasFlags(
                                        flags2, ValueFlags.AllowNull, true))
                                {
                                    operand2String = String.Empty;
                                }

                                localOperand2 = new Variant(operand2String);
                            }
                            else if (flags2 == ValueFlags.List)
                            {
                                Argument operand2Argument = arguments[2];

                                if ((operand2Argument == null) && !FlagOps.HasFlags(
                                        flags2, ValueFlags.AllowNull, true))
                                {
                                    operand2Argument = Argument.Empty;
                                }

                                StringList operand2List = null;

                                code = ListOps.GetOrCopyOrSplitList(
                                    interpreter, operand2Argument, readOnly,
                                    ref operand2List, ref error);

                                if (code == ReturnCode.Ok)
                                    localOperand2 = new Variant(operand2List);
                                else
                                    error = MaybeInvokeErrorCallback(error);
                            }
                            else
                            {
                                IGetValue operand2GetValue = arguments[2];

                                code = GetVariant(
                                    interpreter, operand2GetValue, flags2,
                                    cultureInfo, ref localOperand2, ref error);

                                if (code != ReturnCode.Ok)
                                {
                                    error = MaybeInvokeErrorCallback(
                                        String.Format("operand2: {0}", error));
                                }
                            }
                        }

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Commit changes to the variables provided by
                            //       the caller.
                            //
                            operand1 = localOperand1;
                            operand2 = localOperand2;
                        }
                    }
                    else
                    {
                        string localOperatorName = (argumentCount > 0) ?
                            (string)arguments[0] : @operator.Name;

                        if (operands == 2)
                        {
                            if (ExpressionParser.IsOperatorNameOnly(localOperatorName))
                            {
                                error = MaybeInvokeErrorCallback(String.Format(
                                    "wrong # args: should be \"operand1 {0} operand2\"",
                                    FormatOps.OperatorName(localOperatorName)));
                            }
                            else
                            {
                                error = MaybeInvokeErrorCallback(String.Format(
                                    "wrong # args: should be \"{0} operand1 operand2\"",
                                    FormatOps.OperatorName(localOperatorName)));
                            }
                        }
                        else if (operands == 1)
                        {
                            error = MaybeInvokeErrorCallback(String.Format(
                                "wrong # args: should be \"{0} operand\"",
                                FormatOps.OperatorName(localOperatorName)));
                        }
                        else
                        {
                            error = MaybeInvokeErrorCallback(String.Format(
                                "unsupported number of operands for operator {0}",
                                FormatOps.OperatorName(localOperatorName,
                                @operator.Lexeme)));
                        }

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = MaybeInvokeErrorCallback(
                        "invalid argument list");

                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = MaybeInvokeErrorCallback(
                    "invalid operator");

                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs type-promotion or coercion on one or both of
        /// the specified operands, as required by the specified operator or
        /// function.
        /// </summary>
        /// <param name="identifierName">
        /// The operator or function that the operands belong to.
        /// </param>
        /// <param name="variant1">
        /// The first operand to be converted.
        /// </param>
        /// <param name="variant2">
        /// The second operand to be converted.
        /// </param>
        /// <param name="type1">
        /// The type to which the first operand should be converted, if any.
        /// </param>
        /// <param name="type2">
        /// The type to which the second operand should be converted, if any.
        /// </param>
        /// <param name="noConvert1">
        /// Non-zero to prevent numeric conversion of the first operand.
        /// </param>
        /// <param name="noConvert2">
        /// Non-zero to prevent numeric conversion of the second operand.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode FixupVariants(
            IIdentifierName identifierName,
            IVariant variant1,
            IVariant variant2,
            Type type1,
            Type type2,
            bool noConvert1,
            bool noConvert2
            )
        {
            Result error = null;

            return FixupVariants(
                identifierName, variant1, variant2, type1,
                type2, noConvert1, noConvert2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs type-promotion or coercion on one or both of
        /// the specified operands, as required by the specified operator or
        /// function.  Upon failure, an error message is stored in the
        /// <paramref name="error" /> parameter.
        /// </summary>
        /// <param name="identifierName">
        /// The operator or function that the operands belong to.
        /// </param>
        /// <param name="variant1">
        /// The first operand to be converted.
        /// </param>
        /// <param name="variant2">
        /// The second operand to be converted.
        /// </param>
        /// <param name="type1">
        /// The type to which the first operand should be converted, if any.
        /// </param>
        /// <param name="type2">
        /// The type to which the second operand should be converted, if any.
        /// </param>
        /// <param name="noConvert1">
        /// Non-zero to prevent numeric conversion of the first operand.
        /// </param>
        /// <param name="noConvert2">
        /// Non-zero to prevent numeric conversion of the second operand.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        internal static ReturnCode FixupVariants(
            IIdentifierName identifierName,
            IVariant variant1,
            IVariant variant2,
            Type type1,
            Type type2,
            bool noConvert1,
            bool noConvert2,
            ref Result error
            )
        {
            //
            // NOTE: Perform type-promotion/coercion on one -OR-
            //       both variants based on the allowed types for
            //       this operator or function...
            //
            if ((variant1 == null) || (variant2 == null))
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "one or more operands for {0} are invalid",
                    FormatOps.IdentifierName(identifierName)));

                return ReturnCode.Error;
            }

            //
            // WARNING: These checks for the caller-specified type
            //          conversions must occur before the number
            //          checks below; otherwise, if there custom
            //          conversion semantics for a particular type,
            //          they won't be honored in numeric contexts.
            //
            if ((type1 != null) && !variant1.ConvertTo(type1))
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "failed to convert variant1 to type {0}",
                    MarshalOps.GetErrorTypeName(type1)));

                return ReturnCode.Error;
            }

            if ((type2 != null) && !variant2.ConvertTo(type2))
            {
                error = MaybeInvokeErrorCallback(String.Format(
                    "failed to convert variant2 to type {0}",
                    MarshalOps.GetErrorTypeName(type2)));

                return ReturnCode.Error;
            }

            if (!variant1.IsNumber() || !variant2.IsNumber())
            {
                if ((type1 == null) || (type2 == null))
                    goto error;

                return ReturnCode.Ok;
            }

            bool skip1 = noConvert1 || (type1 != null);
            bool skip2 = noConvert2 || (type2 != null);

            if (variant1.MaybeConvertWith(variant2, skip1, skip2))
                return ReturnCode.Ok;

        error:

            error = MaybeInvokeErrorCallback(String.Format(
                "can't use non-numeric string as operand of {0}",
                FormatOps.IdentifierName(identifierName)));

            return ReturnCode.Error;
        }
    }
}
