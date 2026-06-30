/*
 * AttributeOps.cs --
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
using System.Reflection;
using System.Threading;

#if NET_40
using System.Runtime.Versioning;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private helper routines used to query the custom
    /// attributes applied to assemblies, types, and other members, including
    /// the various Eagle-specific attributes (command, function, operator,
    /// plugin, object, and notify flags, object identifiers and names, etc.) as
    /// well as standard framework attributes.  It also provides reflection-only
    /// attribute querying and helpers for caching attribute-derived metadata.
    /// </summary>
    [ObjectId("846102ad-f175-4611-b35c-1c32bbdcc227")]
    internal static class AttributeOps
    {
        #region Private Constants
        //
        // HACK: This value must be kept synchronized with the UpdateUriName
        //       of the in the Eagle._Components.Shared.AttributeOps class.
        //
        /// <summary>
        /// The name used to select the assembly URI attribute that specifies the
        /// base URI for updates.
        /// </summary>
        private static readonly string UpdateUriName = "update";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // HACK: When this is non-zero, any exceptions that are encountered
        //       by this class will be reported in detail.
        //
        /// <summary>
        /// When non-zero, any exceptions encountered by this class are reported
        /// in detail.
        /// </summary>
        private static bool VerboseExceptions = true;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached random token used to build a unique prefix for cached
        /// method data names; this is initialized lazily on first use.
        /// </summary>
        private static long RandomMethodPrefix = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Assembly Attribute Methods
        /// <summary>
        /// This method returns the configuration string declared by the
        /// assembly configuration attribute of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose configuration attribute is queried.
        /// </param>
        /// <returns>
        /// The configuration string, or null if the assembly is null, has no
        /// configuration attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblyConfiguration(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyConfigurationAttribute), false))
                    {
                        AssemblyConfigurationAttribute configuration =
                            (AssemblyConfigurationAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyConfigurationAttribute),
                                false)[0];

                        return configuration.Configuration;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the target framework name of the specified
        /// assembly.  On newer frameworks this is read from the target framework
        /// attribute; on older frameworks a hard-coded framework name is
        /// returned.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose target framework name is queried.
        /// </param>
        /// <returns>
        /// The target framework name, or null if the assembly is null, has no
        /// target framework attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblyTargetFramework(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
#if NET_40
                try
                {
                    if (assembly.IsDefined(
                            typeof(TargetFrameworkAttribute), false))
                    {
                        TargetFrameworkAttribute targetFramework =
                            (TargetFrameworkAttribute)
                            assembly.GetCustomAttributes(
                                typeof(TargetFrameworkAttribute), false)[0];

                        return targetFramework.FrameworkName;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
#elif NET_35
                return ".NETFramework,Version=v3.5";
#elif NET_20
                return ".NETFramework,Version=v2.0";
#endif
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// This method returns the copyright string declared by the assembly
        /// copyright attribute of the specified assembly, optionally replacing
        /// the Unicode copyright character with its ANSI equivalent.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose copyright attribute is queried.
        /// </param>
        /// <param name="noUnicode">
        /// When true, the Unicode copyright character in the result is replaced
        /// with its ANSI equivalent.
        /// </param>
        /// <returns>
        /// The copyright string, or null if the assembly is null, has no
        /// copyright attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblyCopyright(
            Assembly assembly,
            bool noUnicode
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyCopyrightAttribute), false))
                    {
                        AssemblyCopyrightAttribute copyright =
                            (AssemblyCopyrightAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyCopyrightAttribute), false)[0];

                        string result = copyright.Copyright;

                        if (noUnicode && !String.IsNullOrEmpty(result))
                        {
                            result = result.Replace(
                                Characters.Copyright.ToString(),
                                Characters.CopyrightAnsi);
                        }

                        return result;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// This method returns the license text or license summary declared by
        /// the assembly license attribute of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose license attribute is queried.
        /// </param>
        /// <param name="summary">
        /// When true, the license summary is returned; otherwise, the full
        /// license text is returned.
        /// </param>
        /// <returns>
        /// The license summary or full license text, or null if the assembly is
        /// null, has no license attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblyLicense(
            Assembly assembly,
            bool summary
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyLicenseAttribute), false))
                    {
                        AssemblyLicenseAttribute license =
                            (AssemblyLicenseAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyLicenseAttribute), false)[0];

                        if (summary)
                            return license.Summary;
                        else
                            return license.Text;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the description string declared by the assembly
        /// description attribute of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose description attribute is queried.
        /// </param>
        /// <returns>
        /// The description string, or null if the assembly is null, has no
        /// description attribute, or an exception is encountered.
        /// </returns>
        public static string GetAssemblyDescription(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyDescriptionAttribute), false))
                    {
                        AssemblyDescriptionAttribute description =
                            (AssemblyDescriptionAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyDescriptionAttribute), false)[0];

                        return description.Description;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default assembly URI declared by a custom
        /// attribute of the specified assembly, querying it in a manner that
        /// works with reflection-only loaded assemblies.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose URI attribute is queried.
        /// </param>
        /// <returns>
        /// The assembly URI, or null if no matching attribute is found or an
        /// exception is encountered.
        /// </returns>
        public static Uri GetReflectionOnlyAssemblyUri(
            Assembly assembly
            )
        {
            return GetReflectionOnlyAssemblyUri(assembly, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the update base URI declared by a named custom
        /// attribute of the specified assembly, querying it in a manner that
        /// works with reflection-only loaded assemblies.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose update base URI attribute is queried.
        /// </param>
        /// <returns>
        /// The update base URI, or null if no matching attribute is found or an
        /// exception is encountered.
        /// </returns>
        public static Uri GetReflectionOnlyAssemblyUpdateBaseUri(
            Assembly assembly
            )
        {
            return GetReflectionOnlyAssemblyUri(assembly, UpdateUriName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the custom attributes of the specified assembly
        /// for one whose constructor matches a two-string signature and,
        /// optionally, a leading name argument, and returns the resulting URI.
        /// This is performed in a manner that works with reflection-only loaded
        /// assemblies.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose URI attributes are searched.
        /// </param>
        /// <param name="name">
        /// The name argument that the matching attribute must begin with, or
        /// null to match an attribute with no leading name argument.
        /// </param>
        /// <returns>
        /// The matching assembly URI, or null if no matching attribute is found,
        /// the value is not a valid absolute URI, or an exception is
        /// encountered.
        /// </returns>
        public static Uri GetReflectionOnlyAssemblyUri(
            Assembly assembly,
            string name
            )
        {
            try
            {
                Type[] parameterTypes = {
                    typeof(string), typeof(string)
                };

                object[] parameterValues = (name != null) ?
                    new object[] { name } : null;

                foreach (CustomAttributeData attributeData in
                        GetCustomAttributes(assembly))
                {
                    object value = null;

                    if (!MatchConstructor(
                            attributeData, null, Index.Invalid,
                            parameterTypes, parameterValues,
                            ref value))
                    {
                        continue;
                    }

                    Uri uri;

                    if (Uri.TryCreate(
                            (string)value, UriKind.Absolute,
                            out uri))
                    {
                        return uri;
                    }
                }
            }
            catch (Exception e)
            {
                /* IGNORED */
                RuntimeOps.MaybeGrabAndReportExceptions(
                    e, VerboseExceptions);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the custom attributes of the specified assembly
        /// for one declared by a named attribute type and returns the
        /// constructor argument value at the given index.  This is performed in
        /// a manner that works with reflection-only loaded assemblies.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose custom attributes are searched.
        /// </param>
        /// <param name="typeName">
        /// The name of the attribute type to match.
        /// </param>
        /// <param name="valueIndex">
        /// The index of the constructor argument whose value is returned.
        /// </param>
        /// <returns>
        /// The matching constructor argument value, or null if no matching
        /// attribute is found or an exception is encountered.
        /// </returns>
        private static object GetReflectionOnlyAssemblyValue( /* NOT USED */
            Assembly assembly,
            string typeName,
            int valueIndex
            )
        {
            try
            {
                foreach (CustomAttributeData attributeData
                        in GetCustomAttributes(assembly))
                {
                    object value = null;

                    if (!MatchConstructor(
                            attributeData, typeName, valueIndex,
                            null, null, ref value))
                    {
                        continue;
                    }

                    return value;
                }
            }
            catch (Exception e)
            {
                /* IGNORED */
                RuntimeOps.MaybeGrabAndReportExceptions(
                    e, VerboseExceptions);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the custom attribute data for the specified
        /// assembly, suitable for use with reflection-only loaded assemblies.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose custom attribute data is returned.
        /// </param>
        /// <returns>
        /// The list of custom attribute data, or null if the assembly is null.
        /// </returns>
        private static IList<CustomAttributeData> GetCustomAttributes(
            Assembly assembly
            )
        {
            if (assembly == null)
                return null;

            return CustomAttributeData.GetCustomAttributes(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the custom attribute data for the specified
        /// member, suitable for use with reflection-only loaded assemblies.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose custom attribute data is returned.
        /// </param>
        /// <returns>
        /// The list of custom attribute data, or null if the member is null.
        /// </returns>
        private static IList<CustomAttributeData> GetCustomAttributes(
            MemberInfo memberInfo
            )
        {
            if (memberInfo == null)
                return null;

            return CustomAttributeData.GetCustomAttributes(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a single typed constructor argument
        /// matches the expected type and/or value at the specified position.
        /// </summary>
        /// <param name="typedArgument">
        /// The typed constructor argument to test.
        /// </param>
        /// <param name="parameterTypes">
        /// The array of expected parameter types, or null to skip type
        /// matching.
        /// </param>
        /// <param name="parameterValues">
        /// The array of expected parameter values, or null to skip value
        /// matching.
        /// </param>
        /// <param name="argumentIndex">
        /// The position of the argument within the constructor signature.
        /// </param>
        /// <returns>
        /// True if the argument matches the expected type and value (if
        /// specified) at the given position; otherwise, false.
        /// </returns>
        private static bool MatchArgument(
            CustomAttributeTypedArgument typedArgument, /* in */
            Type[] parameterTypes,                      /* in: OPTIONAL */
            object[] parameterValues,                   /* in: OPTIONAL */
            int argumentIndex                           /* in */
            )
        {
#pragma warning disable 162 // NOTE: Used to be struct, now class?
#pragma warning disable 472 // NOTE: Used to be struct, now class?
            if (typedArgument == null)
                return false;
#pragma warning restore 472
#pragma warning restore 162

            if (parameterTypes != null)
            {
                if ((argumentIndex >= 0) &&
                    (argumentIndex < parameterTypes.Length) &&
                    !MarshalOps.IsSameTypeName(
                        typedArgument.ArgumentType,
                        parameterTypes[argumentIndex]))
                {
                    return false;
                }
            }

            if (parameterValues != null)
            {
                if ((argumentIndex >= 0) &&
                    (argumentIndex < parameterValues.Length) &&
                    !Object.ReferenceEquals(
                        typedArgument.Value,
                        parameterValues[argumentIndex]) &&
                    !SharedStringOps.SystemEquals(
                        StringOps.GetStringFromObject(
                            typedArgument.Value),
                        StringOps.GetStringFromObject(
                            parameterValues[argumentIndex])))
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the constructor described by the
        /// specified custom attribute data matches the optional declaring type
        /// name and parameter type/value constraints, and optionally extracts a
        /// constructor argument value.
        /// </summary>
        /// <param name="attributeData">
        /// The custom attribute data describing the constructor to test.
        /// </param>
        /// <param name="typeName">
        /// The required name of the attribute's declaring type, or null to skip
        /// type-name matching.
        /// </param>
        /// <param name="valueIndex">
        /// The index of the constructor argument whose value is extracted into
        /// <paramref name="value" />, or null to skip extraction.  A value of
        /// negative one selects the last argument.
        /// </param>
        /// <param name="parameterTypes">
        /// The array of expected parameter types, or null to skip type
        /// matching.
        /// </param>
        /// <param name="parameterValues">
        /// The array of expected parameter values, or null to skip value
        /// matching.
        /// </param>
        /// <param name="value">
        /// Upon success, and when <paramref name="valueIndex" /> is not null,
        /// this receives the extracted constructor argument value.
        /// </param>
        /// <returns>
        /// True if the constructor matches all of the specified constraints;
        /// otherwise, false.
        /// </returns>
        private static bool MatchConstructor(
            CustomAttributeData attributeData, /* in */
            string typeName,                   /* in: OPTIONAL */
            int? valueIndex,                   /* in: OPTIONAL */
            Type[] parameterTypes,             /* in: OPTIONAL */
            object[] parameterValues,          /* in: OPTIONAL */
            ref object value                   /* out */
            )
        {
            if (attributeData == null)
                return false;

            ConstructorInfo constructorInfo = attributeData.Constructor;

            if (constructorInfo == null)
                return false;

            if (typeName != null)
            {
                Type type = constructorInfo.DeclaringType;

                if (type == null)
                    return false;

                if (!SharedStringOps.SystemEquals(type.Name, typeName))
                    return false;
            }

            IList<CustomAttributeTypedArgument> typedArguments =
                attributeData.ConstructorArguments;

            int argumentCount = typedArguments.Count;

            if (argumentCount > 0)
            {
                for (int argumentIndex = 0; argumentIndex < argumentCount;
                        argumentIndex++)
                {
                    if (!MatchArgument(
                            typedArguments[argumentIndex], parameterTypes,
                            parameterValues, argumentIndex))
                    {
                        return false;
                    }
                }
            }
            else if ((parameterTypes != null) || (parameterValues != null))
            {
                //
                // NOTE: This constructor cannot be a match because it has
                //       no arguments -AND- our caller wants some matching
                //       against the arguments.
                //
                return false;
            }

            if (valueIndex != null)
            {
                int localValueIndex = (int)valueIndex;

                if (localValueIndex == Index.Invalid) /* -1 == LAST */
                    localValueIndex = argumentCount - 1;

                if ((localValueIndex < 0) ||
                    (localValueIndex >= argumentCount))
                {
                    return false; /* NOTE: Out-of-bounds. */
                }

                value = typedArguments[localValueIndex].Value;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the name used to cache attribute-derived metadata
        /// (such as command flags) for a specific method overload, optionally
        /// including a process-unique random prefix.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain a random number for the prefix, or
        /// null to use the global state instead.
        /// </param>
        /// <param name="index">
        /// The index of the method overload.
        /// </param>
        /// <param name="method">
        /// The method whose cached data name is built.
        /// </param>
        /// <param name="noPrefix">
        /// When true, the process-unique random prefix is omitted.
        /// </param>
        /// <returns>
        /// The cached data name for the method, or null if the method is null.
        /// </returns>
        public static string GetMethodDataName(
            Interpreter interpreter, /* in */
            int index,               /* in */
            MethodBase method,       /* in */
            bool noPrefix            /* in */
            )
        {
            if (method == null)
                return null;

            ParameterInfo returnInfo;
            ParameterInfo[] parameterInfos;

            MarshalOps.GetParameterInfos(method,
                out returnInfo, out parameterInfos);

            string prefix = null;

            if (!noPrefix)
            {
                long token = Interlocked.CompareExchange(
                    ref RandomMethodPrefix, 0, 0);

                if (token == 0)
                {
                    if (interpreter != null)
                        token = interpreter.GetSignedRandomNumber();
                    else
                        token = GlobalState.GetSignedRandomNumber();

                    Interlocked.CompareExchange(
                        ref RandomMethodPrefix, token, 0);
                }

                prefix = String.Format(
                    "Process_{0}_Method_{1}",
                    FormatOps.Hexadecimal(token, true),
                    typeof(CommandFlags).Name);
            }

            return String.Format("{0} {1}",
                prefix, FormatOps.MethodOverload(
                    index, FormatOps.TypeName(
                    method.DeclaringType, false),
                method.Name, returnInfo, parameterInfos,
                MarshalFlags.ShowSignatures)).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the command flags for a specific method overload
        /// in the data slots of the specified application domain, keyed by the
        /// method's cached data name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when building the cached data name.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which the command flags are stored.
        /// </param>
        /// <param name="index">
        /// The index of the method overload.
        /// </param>
        /// <param name="method">
        /// The method whose command flags are stored.
        /// </param>
        /// <param name="commandFlags">
        /// The command flags to store, which may be null.
        /// </param>
        /// <returns>
        /// True if the command flags were stored successfully; otherwise, false.
        /// </returns>
        public static bool SetCachedCommandFlags(
            Interpreter interpreter,   /* in */
            AppDomain appDomain,       /* in */
            int index,                 /* in */
            MethodBase method,         /* in */
            CommandFlags? commandFlags /* in */
            )
        {
            if (appDomain == null)
                return false;

            string name = GetMethodDataName(
                interpreter, index, method, false);

            if (name == null)
                return false;

            try
            {
                appDomain.SetData(name, commandFlags);
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(AttributeOps).Name,
                    TracePriority.RemotingError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the command flags for a specific method
        /// overload from the data slots of the specified application domain,
        /// keyed by the method's cached data name.  A cached string value is
        /// parsed into command flags using the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when building the cached data name and when
        /// parsing a cached string value into command flags.
        /// </param>
        /// <param name="appDomain">
        /// The application domain from which the command flags are retrieved.
        /// </param>
        /// <param name="index">
        /// The index of the method overload.
        /// </param>
        /// <param name="method">
        /// The method whose command flags are retrieved.
        /// </param>
        /// <returns>
        /// The cached command flags, or null if none are cached or an exception
        /// is encountered.
        /// </returns>
        public static CommandFlags? GetCachedCommandFlags(
            Interpreter interpreter, /* in */
            AppDomain appDomain,     /* in */
            int index,               /* in */
            MethodBase method        /* in */
            )
        {
            if (appDomain == null)
                return null;

            string name = GetMethodDataName(
                interpreter, index, method, false);

            if (name == null)
                return null;

            object enumValue;

            try
            {
                enumValue = appDomain.GetData(name);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(AttributeOps).Name,
                    TracePriority.RemotingError);

                return null;
            }

            if (enumValue is CommandFlags)
                return (CommandFlags)enumValue;

            if ((enumValue is string) &&
                (interpreter != null))
            {
                enumValue = EnumOps.TryParseFlags(
                    interpreter, typeof(CommandFlags),
                    null, (string)enumValue,
                    interpreter.InternalCultureInfo,
                    true, true, true);

                if (enumValue is CommandFlags)
                    return (CommandFlags)enumValue;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region MemberInfo (Mostly Type) Attribute Methods
        /// <summary>
        /// This method returns the CLS-compliance flag declared by the
        /// CLS-compliant attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose CLS-compliant attribute is queried.
        /// </param>
        /// <returns>
        /// The CLS-compliance flag, or null if the member is null, has no
        /// CLS-compliant attribute, or an exception is encountered.
        /// </returns>
        public static bool? GetClsCompliant(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(CLSCompliantAttribute), false))
                    {
                        CLSCompliantAttribute compliant =
                            (CLSCompliantAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(CLSCompliantAttribute), false)[0];

                        return compliant.IsCompliant;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of arguments declared by the
        /// arguments attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose arguments attribute is queried.
        /// </param>
        /// <returns>
        /// The declared number of arguments, or the value of
        /// <see cref="Arity.None" /> if the member is null, has no arguments
        /// attribute, or an exception is encountered.
        /// </returns>
        public static int GetArguments(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ArgumentsAttribute), false))
                    {
                        ArgumentsAttribute arguments =
                            (ArgumentsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ArgumentsAttribute), false)[0];

                        return arguments.Arguments;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return (int)Arity.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the number of arguments declared by the
        /// arguments attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's arguments attribute is queried.
        /// </param>
        /// <returns>
        /// The declared number of arguments, or the value of
        /// <see cref="Arity.None" /> if the object is null, its type has no
        /// arguments attribute, or an exception is encountered.
        /// </returns>
        public static int GetArguments(
            object @object
            )
        {
            if (@object != null)
                return GetArguments(@object.GetType());
            else
                return (int)Arity.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the command represented by the
        /// specified member is considered safe, based on its command flags.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose command flags are examined.
        /// </param>
        /// <returns>
        /// True if the member's command flags indicate that it is safe and not
        /// unsafe; otherwise, false.
        /// </returns>
        public static bool IsSafe(
            MemberInfo memberInfo
            )
        {
            CommandFlags commandFlags = GetCommandFlags(memberInfo);

            if (FlagOps.HasFlags(commandFlags, CommandFlags.Unsafe, true))
                return false;

            if (!FlagOps.HasFlags(commandFlags, CommandFlags.Safe, true))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the method represented by the
        /// specified method base is considered safe, based on its cached command
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the application domain and to look up
        /// the cached command flags, or null to use the current application
        /// domain.
        /// </param>
        /// <param name="index">
        /// The index of the method overload, or null to use zero.
        /// </param>
        /// <param name="method">
        /// The method whose cached command flags are examined.
        /// </param>
        /// <returns>
        /// True if the method's cached command flags indicate that it is safe
        /// and not unsafe; otherwise, false.
        /// </returns>
        public static bool IsCachedSafe(
            Interpreter interpreter,
            int? index,
            MethodBase method
            )
        {
            AppDomain appDomain = (interpreter != null) ?
                interpreter.GetAppDomain() : AppDomainOps.GetCurrent();

            CommandFlags? commandFlags = GetCachedCommandFlags(
                interpreter, appDomain, (index != null) ? (int)index : 0,
                method);

            if (commandFlags == null)
                return false;

            if (FlagOps.HasFlags(
                    (CommandFlags)commandFlags, CommandFlags.Unsafe, true))
            {
                return false;
            }

            if (!FlagOps.HasFlags(
                    (CommandFlags)commandFlags, CommandFlags.Safe, true))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the command flags declared by the command flags
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose command flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared command flags, or <see cref="CommandFlags.None" /> if
        /// the member is null, has no command flags attribute, or an exception
        /// is encountered.
        /// </returns>
        public static CommandFlags GetCommandFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(CommandFlagsAttribute), false))
                    {
                        CommandFlagsAttribute flags =
                            (CommandFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(CommandFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return CommandFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the command flags declared by the command flags
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's command flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared command flags, or <see cref="CommandFlags.None" /> if
        /// the object is null, its type has no command flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static CommandFlags GetCommandFlags(
            object @object
            )
        {
            if (@object != null)
                return GetCommandFlags(@object.GetType());
            else
                return CommandFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the function flags declared by the function flags
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose function flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared function flags, or <see cref="FunctionFlags.None" /> if
        /// the member is null, has no function flags attribute, or an exception
        /// is encountered.
        /// </returns>
        public static FunctionFlags GetFunctionFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(FunctionFlagsAttribute), false))
                    {
                        FunctionFlagsAttribute flags =
                            (FunctionFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(FunctionFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return FunctionFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the function flags declared by the function flags
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's function flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared function flags, or <see cref="FunctionFlags.None" /> if
        /// the object is null, its type has no function flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static FunctionFlags GetFunctionFlags(
            object @object
            )
        {
            if (@object != null)
                return GetFunctionFlags(@object.GetType());
            else
                return FunctionFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the operator flags declared by the operator flags
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose operator flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared operator flags, or <see cref="OperatorFlags.None" /> if
        /// the member is null, has no operator flags attribute, or an exception
        /// is encountered.
        /// </returns>
        public static OperatorFlags GetOperatorFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(OperatorFlagsAttribute), false))
                    {
                        OperatorFlagsAttribute flags =
                            (OperatorFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(OperatorFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return OperatorFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the operator flags declared by the operator flags
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's operator flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared operator flags, or <see cref="OperatorFlags.None" /> if
        /// the object is null, its type has no operator flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static OperatorFlags GetOperatorFlags(
            object @object
            )
        {
            if (@object != null)
                return GetOperatorFlags(@object.GetType());
            else
                return OperatorFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the lexeme declared by the lexeme attribute of
        /// the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose lexeme attribute is queried.
        /// </param>
        /// <returns>
        /// The declared lexeme, or <see cref="Lexeme.Unknown" /> if the member
        /// is null, has no lexeme attribute, or an exception is encountered.
        /// </returns>
        public static Lexeme GetLexeme(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(LexemeAttribute), false))
                    {
                        LexemeAttribute flags =
                            (LexemeAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(LexemeAttribute), false)[0];

                        return flags.Lexeme;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return Lexeme.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the lexeme declared by the lexeme attribute of
        /// the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's lexeme attribute is queried.
        /// </param>
        /// <returns>
        /// The declared lexeme, or <see cref="Lexeme.Unknown" /> if the object
        /// is null, its type has no lexeme attribute, or an exception is
        /// encountered.
        /// </returns>
        public static Lexeme GetLexeme(
            object @object
            )
        {
            if (@object != null)
                return GetLexeme(@object.GetType());
            else
                return Lexeme.Unknown;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the type list flags declared by the type list
        /// flags attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose type list flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared type list flags, or <see cref="TypeListFlags.None" /> if
        /// the member is null, has no type list flags attribute, or an exception
        /// is encountered.
        /// </returns>
        public static TypeListFlags GetTypeListFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(TypeListFlagsAttribute), false))
                    {
                        TypeListFlagsAttribute flags =
                            (TypeListFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(TypeListFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return TypeListFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the type list flags declared by the type list
        /// flags attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's type list flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared type list flags, or <see cref="TypeListFlags.None" /> if
        /// the object is null, its type has no type list flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static TypeListFlags GetTypeListFlags(
            object @object
            )
        {
            if (@object != null)
                return GetTypeListFlags(@object.GetType());
            else
                return TypeListFlags.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the method flags declared by the method flags
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose method flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared method flags, or <see cref="MethodFlags.None" /> if the
        /// member is null, has no method flags attribute, or an exception is
        /// encountered.
        /// </returns>
        public static MethodFlags GetMethodFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(MethodFlagsAttribute), false))
                    {
                        MethodFlagsAttribute flags =
                            (MethodFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(MethodFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return MethodFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the method flags declared by the method flags
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's method flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared method flags, or <see cref="MethodFlags.None" /> if the
        /// object is null, its type has no method flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static MethodFlags GetMethodFlags(
            object @object
            )
        {
            if (@object != null)
                return GetMethodFlags(@object.GetType());
            else
                return MethodFlags.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Notifier Attribute Methods
#if NOTIFY || NOTIFY_OBJECT
        /// <summary>
        /// This method returns the notify flags declared by the notify flags
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose notify flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared notify flags, or <see cref="NotifyFlags.None" /> if the
        /// member is null, has no notify flags attribute, or an exception is
        /// encountered.
        /// </returns>
        public static NotifyFlags GetNotifyFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(NotifyFlagsAttribute), false))
                    {
                        NotifyFlagsAttribute flags =
                            (NotifyFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(NotifyFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the notify flags declared by the notify flags
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's notify flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared notify flags, or <see cref="NotifyFlags.None" /> if the
        /// object is null, its type has no notify flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static NotifyFlags GetNotifyFlags(
            object @object
            )
        {
            if (@object != null)
                return GetNotifyFlags(@object.GetType());
            else
                return NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the notify types declared by the notify types
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose notify types attribute is queried.
        /// </param>
        /// <returns>
        /// The declared notify types, or <see cref="NotifyType.None" /> if the
        /// member is null, has no notify types attribute, or an exception is
        /// encountered.
        /// </returns>
        public static NotifyType GetNotifyTypes(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(NotifyTypesAttribute), false))
                    {
                        NotifyTypesAttribute types =
                            (NotifyTypesAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(NotifyTypesAttribute), false)[0];

                        return types.Types;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return NotifyType.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the notify types declared by the notify types
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's notify types attribute is queried.
        /// </param>
        /// <returns>
        /// The declared notify types, or <see cref="NotifyType.None" /> if the
        /// object is null, its type has no notify types attribute, or an
        /// exception is encountered.
        /// </returns>
        public static NotifyType GetNotifyTypes(
            object @object
            )
        {
            if (@object != null)
                return GetNotifyTypes(@object.GetType());
            else
                return NotifyType.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object flags declared by the object flags
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose object flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared object flags, or <see cref="ObjectFlags.None" /> if the
        /// member is null, has no object flags attribute, or an exception is
        /// encountered.
        /// </returns>
        public static ObjectFlags GetObjectFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectFlagsAttribute), false))
                    {
                        ObjectFlagsAttribute flags =
                            (ObjectFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ObjectFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return ObjectFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the object flags declared by the object flags
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's object flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared object flags, or <see cref="ObjectFlags.None" /> if the
        /// object is null, its type has no object flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static ObjectFlags GetObjectFlags(
            object @object
            )
        {
            if (@object != null)
                return GetObjectFlags(@object.GetType());
            else
                return ObjectFlags.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object group names declared by the object
        /// group attributes of the specified member, formatted as a string.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose object group attributes are queried.
        /// </param>
        /// <param name="inherit">
        /// When true, inherited object group attributes are also considered.
        /// </param>
        /// <param name="primaryOnly">
        /// When true, only the first (primary) object group is returned.
        /// </param>
        /// <returns>
        /// The formatted list of object group names, or null if the member is
        /// null, has no object group attributes, or an exception is encountered.
        /// </returns>
        public static string GetObjectGroups(
            MemberInfo memberInfo,
            bool inherit,
            bool primaryOnly
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectGroupAttribute), inherit))
                    {
                        object[] attributes = memberInfo.GetCustomAttributes(
                            typeof(ObjectGroupAttribute), inherit);

                        if (attributes != null)
                        {
                            StringList list = null;

                            foreach (object attribute in attributes)
                            {
                                ObjectGroupAttribute group =
                                    attribute as ObjectGroupAttribute;

                                if (group != null)
                                {
                                    string value = group.Group;

                                    if (value != null)
                                    {
                                        if (list == null)
                                            list = new StringList();

                                        list.Add(value);

                                        if (primaryOnly)
                                            break;
                                    }
                                }
                            }

                            if (list != null)
                                return list.ToString();
                        }
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object group names declared by the object
        /// group attributes of the runtime type of the specified object,
        /// formatted as a string.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's object group attributes are queried.
        /// </param>
        /// <returns>
        /// The formatted list of object group names, or null if the object is
        /// null, its type has no object group attributes, or an exception is
        /// encountered.
        /// </returns>
        public static string GetObjectGroups(
            object @object
            )
        {
            if (@object != null)
            {
                return GetObjectGroups(@object.GetType());
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns all of the object group names declared by the
        /// object group attributes of the specified member, including inherited
        /// ones, formatted as a string.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose object group attributes are queried.
        /// </param>
        /// <returns>
        /// The formatted list of object group names, or null if the member is
        /// null, has no object group attributes, or an exception is encountered.
        /// </returns>
        public static string GetObjectGroups(
            MemberInfo memberInfo
            )
        {
            return GetObjectGroups(memberInfo, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifier declared by the object
        /// identifier attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose object identifier attribute is queried.
        /// </param>
        /// <returns>
        /// The declared object identifier, or <see cref="Guid.Empty" /> if the
        /// member is null, has no object identifier attribute, or an exception
        /// is encountered.
        /// </returns>
        public static Guid GetObjectId(
            MemberInfo memberInfo
            )
        {
            bool defined = false;

            return GetObjectId(memberInfo, ref defined);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifier declared by the object
        /// identifier attribute of the specified member, also indicating whether
        /// such an attribute was actually present.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose object identifier attribute is queried.
        /// </param>
        /// <param name="defined">
        /// Upon return, this is set to true if the object identifier attribute
        /// was present on the member; otherwise, it is left unchanged.
        /// </param>
        /// <returns>
        /// The declared object identifier, or <see cref="Guid.Empty" /> if the
        /// member is null, has no object identifier attribute, or an exception
        /// is encountered.
        /// </returns>
        public static Guid GetObjectId(
            MemberInfo memberInfo,
            ref bool defined
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectIdAttribute), false))
                    {
                        defined = true;

                        ObjectIdAttribute id =
                            (ObjectIdAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ObjectIdAttribute), false)[0];

                        return id.Id;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifier declared by the object
        /// identifier attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's object identifier attribute is
        /// queried.
        /// </param>
        /// <returns>
        /// The declared object identifier, or <see cref="Guid.Empty" /> if the
        /// object is null, its type has no object identifier attribute, or an
        /// exception is encountered.
        /// </returns>
        public static Guid GetObjectId(
            object @object
            )
        {
            if (@object != null)
                return GetObjectId(@object.GetType());
            else
                return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object name declared by the object name
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose object name attribute is queried.
        /// </param>
        /// <returns>
        /// The declared object name, or null if the member is null, has no
        /// object name attribute, or an exception is encountered.
        /// </returns>
        public static string GetObjectName(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectNameAttribute), false))
                    {
                        ObjectNameAttribute name =
                            (ObjectNameAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ObjectNameAttribute), false)[0];

                        return name.Name;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the object name declared by the object name
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's object name attribute is queried.
        /// </param>
        /// <returns>
        /// The declared object name, or null if the object is null, its type
        /// has no object name attribute, or an exception is encountered.
        /// </returns>
        public static string GetObjectName(
            object @object
            )
        {
            if (@object != null)
                return GetObjectName(@object.GetType());
            else
                return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of operands declared by the operands
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose operands attribute is queried.
        /// </param>
        /// <returns>
        /// The declared number of operands, or the value of
        /// <see cref="Arity.None" /> if the member is null, has no operands
        /// attribute, or an exception is encountered.
        /// </returns>
        public static int GetOperands(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(OperandsAttribute), false))
                    {
                        OperandsAttribute operands =
                            (OperandsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(OperandsAttribute), false)[0];

                        return operands.Operands;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return (int)Arity.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the number of operands declared by the operands
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's operands attribute is queried.
        /// </param>
        /// <returns>
        /// The declared number of operands, or the value of
        /// <see cref="Arity.None" /> if the object is null, its type has no
        /// operands attribute, or an exception is encountered.
        /// </returns>
        public static int GetOperands(
            object @object
            )
        {
            if (@object != null)
                return GetOperands(@object.GetType());
            else
                return (int)Arity.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the parameter index declared by the parameter
        /// index attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose parameter index attribute is queried.
        /// </param>
        /// <returns>
        /// The declared parameter index, or <see cref="Index.Invalid" /> if the
        /// member is null, has no parameter index attribute, or an exception is
        /// encountered.
        /// </returns>
        private static int GetParameterIndex(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ParameterIndexAttribute), false))
                    {
                        ParameterIndexAttribute parameterIndex =
                            (ParameterIndexAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ParameterIndexAttribute), false)[0];

                        return parameterIndex.Index;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a list of enumeration value names into their
        /// declared parameter indexes, using the parameter index attribute
        /// applied to each corresponding enumeration field.
        /// </summary>
        /// <param name="enumType">
        /// The enumeration type whose fields are queried.
        /// </param>
        /// <param name="enumNames">
        /// The list of enumeration value names to resolve.
        /// </param>
        /// <param name="parameterIndexes">
        /// Upon success, this receives the array of resolved parameter indexes,
        /// parallel to <paramref name="enumNames" />, with null entries for
        /// names that have no parameter index.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetParameterIndexes(
            Type enumType,
            StringList enumNames,
            ref int?[] parameterIndexes,
            ref Result error
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    FormatOps.TypeName(enumType));

                return ReturnCode.Error;
            }

            if (enumNames == null)
            {
                error = "invalid enumeration names";
                return ReturnCode.Error;
            }

            int count = enumNames.Count;
            int?[] localParameterIndexes = new int?[count];

            for (int index = 0; index < count; index++)
            {
                string enumName = enumNames[index];

                if (enumName == null)
                    continue;

                FieldInfo fieldInfo = enumType.GetField(
                    enumName);

                if (fieldInfo == null)
                    continue;

                int parameterIndex = GetParameterIndex(
                    fieldInfo);

                if (parameterIndex == Index.Invalid)
                    continue;

                localParameterIndexes[index] = parameterIndex;
            }

            parameterIndexes = localParameterIndexes;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the plugin flags declared by a plugin flags
        /// attribute of the specified member, querying it in a manner that works
        /// with reflection-only loaded assemblies.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose plugin flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared plugin flags, or <see cref="PluginFlags.None" /> if no
        /// matching attribute is found or an exception is encountered.
        /// </returns>
        public static PluginFlags GetReflectionOnlyPluginFlags(
            MemberInfo memberInfo
            )
        {
            try
            {
                Type[] parameterTypes = { typeof(PluginFlags) };

                foreach (CustomAttributeData attributeData in
                        GetCustomAttributes(memberInfo))
                {
                    object value = null;

                    if (!MatchConstructor(
                            attributeData, null, Index.Invalid,
                            parameterTypes, null, ref value))
                    {
                        continue;
                    }

                    return (PluginFlags)Enum.ToObject(
                        typeof(PluginFlags), (ulong)value);
                }
            }
            catch (Exception e)
            {
                /* IGNORED */
                RuntimeOps.MaybeGrabAndReportExceptions(
                    e, VerboseExceptions);
            }

            return PluginFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the plugin flags declared by the plugin flags
        /// attribute of the specified member.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose plugin flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared plugin flags, or <see cref="PluginFlags.None" /> if the
        /// member is null, has no plugin flags attribute, or an exception is
        /// encountered.
        /// </returns>
        public static PluginFlags GetPluginFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(PluginFlagsAttribute), false))
                    {
                        PluginFlagsAttribute flags =
                            (PluginFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(PluginFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch (Exception e)
                {
                    /* IGNORED */
                    RuntimeOps.MaybeGrabAndReportExceptions(
                        e, VerboseExceptions);
                }
            }

            return PluginFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the plugin flags declared by the plugin flags
        /// attribute of the runtime type of the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose runtime type's plugin flags attribute is queried.
        /// </param>
        /// <returns>
        /// The declared plugin flags, or <see cref="PluginFlags.None" /> if the
        /// object is null, its type has no plugin flags attribute, or an
        /// exception is encountered.
        /// </returns>
        public static PluginFlags GetPluginFlags(
            object @object
            )
        {
            if (@object != null)
                return GetPluginFlags(@object.GetType());
            else
                return PluginFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ObjectId Attribute Methods
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the object identifiers and full type names of all
        /// of the types defined in all of the assemblies loaded into the
        /// specified application domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain whose assemblies are examined.
        /// </param>
        /// <param name="all">
        /// When true, all types are included; otherwise, only those types that
        /// declare an object identifier (or have a non-empty one) are included.
        /// </param>
        /// <returns>
        /// The list of object identifier and full type name pairs, or null if
        /// an error is encountered.
        /// </returns>
        public static StringPairList GetObjectIds(
            AppDomain appDomain,
            bool all
            )
        {
            Result error = null;

            return GetObjectIds(appDomain, all, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifiers and full type names of all
        /// of the types defined in all of the assemblies loaded into the
        /// specified application domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain whose assemblies are examined.
        /// </param>
        /// <param name="all">
        /// When true, all types are included; otherwise, only those types that
        /// declare an object identifier (or have a non-empty one) are included.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The list of object identifier and full type name pairs, or null if
        /// an error is encountered.
        /// </returns>
        public static StringPairList GetObjectIds(
            AppDomain appDomain,
            bool all,
            ref Result error
            )
        {
            if (appDomain != null)
            {
                try
                {
                    Assembly[] assemblies = appDomain.GetAssemblies();

                    if (assemblies != null)
                    {
                        StringPairList list = new StringPairList();

                        foreach (Assembly assembly in assemblies)
                        {
                            if (assembly != null)
                            {
                                StringPairList list2 = GetObjectIds(
                                    assembly, all, ref error);

                                if (list2 == null)
                                    return null;

                                list.AddRange(list2);
                            }
                        }

                        return list;
                    }
                    else
                    {
                        error = "invalid assemblies";
                    }
                }
                catch (Exception e)
                {
                    ResultList errors = null;

                    /* IGNORED */
                    RuntimeOps.MaybeGrabExceptions(
                        e, VerboseExceptions, ref errors);

                    if (errors != null)
                        error = errors;
                    else
                        error = e;
                }
            }
            else
            {
                error = "invalid application domain";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifiers and full type names of all
        /// of the types defined in the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose types are examined.
        /// </param>
        /// <param name="all">
        /// When true, all types are included; otherwise, only those types that
        /// declare an object identifier (or have a non-empty one) are included.
        /// </param>
        /// <returns>
        /// The list of object identifier and full type name pairs, or null if
        /// an error is encountered.
        /// </returns>
        public static StringPairList GetObjectIds(
            Assembly assembly,
            bool all
            )
        {
            Result error = null;

            return GetObjectIds(assembly, all, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the object identifiers and full type names of all
        /// of the types defined in the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose types are examined.
        /// </param>
        /// <param name="all">
        /// When true, all types are included; otherwise, only those types that
        /// declare an object identifier (or have a non-empty one) are included.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The list of object identifier and full type name pairs, or null if
        /// an error is encountered.
        /// </returns>
        public static StringPairList GetObjectIds(
            Assembly assembly,
            bool all,
            ref Result error
            )
        {
            if (assembly != null)
            {
                try
                {
                    StringPairList list = new StringPairList();
                    Type[] types = assembly.GetTypes(); /* throw */

                    foreach (Type type in types)
                    {
                        bool defined = false;

                        Guid id = GetObjectId(type, ref defined);

                        if (all || defined || !id.Equals(Guid.Empty))
                            list.Add(id.ToString(), type.FullName);
                    }

                    return list;
                }
                catch (Exception e)
                {
                    ResultList errors = null;

                    /* IGNORED */
                    RuntimeOps.MaybeGrabExceptions(
                        e, VerboseExceptions, ref errors);

                    if (errors != null)
                        error = errors;
                    else
                        error = e;
                }
            }
            else
            {
                error = "invalid assembly";
            }

            return null;
        }
        #endregion
    }
}
