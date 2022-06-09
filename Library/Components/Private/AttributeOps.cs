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
    [ObjectId("846102ad-f175-4611-b35c-1c32bbdcc227")]
    internal static class AttributeOps
    {
        #region Private Constants
        //
        // HACK: This value must be kept synchronized with the UpdateUriName
        //       of the in the Eagle._Components.Shared.AttributeOps class.
        //
        private static readonly string UpdateUriName = "update";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // HACK: When this is non-zero, any exceptions that are encountered
        //       by this class will be reported in detail.
        //
        private static bool VerboseExceptions = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Assembly Attribute Methods
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

        public static Uri GetReflectionOnlyAssemblyUri(
            Assembly assembly
            )
        {
            return GetReflectionOnlyAssemblyUri(assembly, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetReflectionOnlyAssemblyUpdateBaseUri(
            Assembly assembly
            )
        {
            return GetReflectionOnlyAssemblyUri(assembly, UpdateUriName);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static IList<CustomAttributeData> GetCustomAttributes(
            Assembly assembly
            )
        {
            if (assembly == null)
                return null;

            return CustomAttributeData.GetCustomAttributes(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IList<CustomAttributeData> GetCustomAttributes(
            MemberInfo memberInfo
            )
        {
            if (memberInfo == null)
                return null;

            return CustomAttributeData.GetCustomAttributes(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MatchArgument(
            CustomAttributeTypedArgument typedArgument, /* in */
            Type[] parameterTypes,                      /* in: OPTIONAL */
            object[] parameterValues,                   /* in: OPTIONAL */
            int argumentIndex                           /* in */
            )
        {
#pragma warning disable 162 // NOTE: Used to be struct, now class?
            if (typedArgument == null)
                return false;
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region MemberInfo (Mostly Type) Attribute Methods
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

        public static string GetObjectGroups(
            MemberInfo memberInfo
            )
        {
            return GetObjectGroups(memberInfo, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            MemberInfo memberInfo
            )
        {
            bool defined = false;

            return GetObjectId(memberInfo, ref defined);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static StringPairList GetObjectIds(
            Assembly assembly,
            bool all
            )
        {
            Result error = null;

            return GetObjectIds(assembly, all, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
