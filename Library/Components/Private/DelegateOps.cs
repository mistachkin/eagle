/*
 * DelegateOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Reflection.Emit;

#if NATIVE && LIBRARY
using System.Runtime.InteropServices;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if NATIVE && LIBRARY
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("cb1b3474-f840-4b2d-bcbc-5502f7a82232")]
    internal static class DelegateOps
    {
        #region Private Constants
        private const TypeAttributes DefaultClassTypeAttributes =
            TypeAttributes.AnsiClass | TypeAttributes.AutoLayout |
            TypeAttributes.NotPublic | TypeAttributes.Sealed;

        private const MethodAttributes DefaultInstanceMethodAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.Virtual | MethodAttributes.NewSlot;

        private const MethodAttributes DefaultStaticMethodAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.Static;

        private const FieldAttributes DefaultFieldAttributes =
            FieldAttributes.Private | FieldAttributes.Static;

        private const CallingConventions DefaultCallingConventions =
            CallingConventions.Standard;

        private const MethodAttributes ConstructorMethodAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        private const MethodImplAttributes DefaultMethodImplAttributes =
            MethodImplAttributes.Managed | MethodImplAttributes.Runtime;

        ///////////////////////////////////////////////////////////////////////

        private const AssemblyBuilderAccess DefaultManagedAssemblyBuilderAccess =
#if NET_40
            AssemblyBuilderAccess.RunAndCollect;
#else
            AssemblyBuilderAccess.Run;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        private const AssemblyBuilderAccess DefaultNativeAssemblyBuilderAccess =
            AssemblyBuilderAccess.Run;
#endif

        ///////////////////////////////////////////////////////////////////////

        internal const string FirstArgumentFieldName = "firstArgument";
        internal const string InvokeMethodName = "Invoke";

        private const string BeginInvokeMethodName = "BeginInvoke";
        private const string EndInvokeMethodName = "EndInvoke";

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        private const string BestFitMappingFieldName = "BestFitMapping";
        private const string CharSetFieldName = "CharSet";
        private const string SetLastErrorFieldName = "SetLastError";
        private const string ThrowOnUnmappableCharFieldName = "ThrowOnUnmappableChar";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static MethodInfo GetInvokeMethod(
            Type type /* in */
            )
        {
            if (type == null)
                return null;

            return type.GetMethod(
                InvokeMethodName, ObjectOps.GetBindingFlags(
                MetaBindingFlags.PublicInstanceMethod, true));
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool NeedReturnType(
            Type type /* in */
            )
        {
            return (type != null) && (type != typeof(void));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool NeedReturnType(
            Delegate @delegate, /* in */
            ref Type type       /* out */
            )
        {
            if (@delegate == null)
                return false;

            MethodInfo methodInfo = @delegate.Method;

            if (methodInfo == null)
                return false;

            type = methodInfo.ReturnType;

            return NeedReturnType(type);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool NeedBoxOpCode(
            Type type /* in */
            )
        {
            if (type == null)
                return false;

            if (type.IsValueType)
                return true;

            if (type.IsGenericParameter)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static OpCode GetOpCodeForLdind(
            Type type /* in */
            )
        {
            if (type == typeof(System.Boolean))
                return OpCodes.Ldind_I1;
            else if (type == typeof(SByte))
                return OpCodes.Ldind_I1;
            else if (type == typeof(Byte))
                return OpCodes.Ldind_U1;
            else if (type == typeof(Char))
                return OpCodes.Ldind_U2;
            else if (type == typeof(Int16))
                return OpCodes.Ldind_I2;
            else if (type == typeof(UInt16))
                return OpCodes.Ldind_U2;
            else if (type == typeof(Int32))
                return OpCodes.Ldind_I4;
            else if (type == typeof(UInt32))
                return OpCodes.Ldind_U4;
            else if (type == typeof(Int64))
                return OpCodes.Ldind_I8;
            else if (type == typeof(UInt64))
                return OpCodes.Ldind_I8; /* Ldind_U8 */
            else if (type == typeof(IntPtr))
                return OpCodes.Ldind_I;
            else if (type == typeof(Single))
                return OpCodes.Ldind_R4;
            else if (type == typeof(Double))
                return OpCodes.Ldind_R8;
            else
                return OpCodes.Ldind_Ref;
        }

        ///////////////////////////////////////////////////////////////////////

        private static OpCode GetOpCodeForStind(
            Type type /* in */
            )
        {
            if (type == typeof(System.Boolean))
                return OpCodes.Stind_I1;
            else if (type == typeof(SByte))
                return OpCodes.Stind_I1;
            else if (type == typeof(Byte))
                return OpCodes.Stind_I1; /* Stind_U1 */
            else if (type == typeof(Char))
                return OpCodes.Stind_I2; /* Stind_U2 */
            else if (type == typeof(Int16))
                return OpCodes.Stind_I2;
            else if (type == typeof(UInt16))
                return OpCodes.Stind_I2; /* Stind_U2 */
            else if (type == typeof(Int32))
                return OpCodes.Stind_I4;
            else if (type == typeof(UInt32))
                return OpCodes.Stind_I4; /* Stind_U4 */
            else if (type == typeof(Int64))
                return OpCodes.Stind_I8;
            else if (type == typeof(UInt64))
                return OpCodes.Stind_I8; /* Stind_U8 */
            else if (type == typeof(IntPtr))
                return OpCodes.Stind_I;
            else if (type == typeof(Single))
                return OpCodes.Stind_R4;
            else if (type == typeof(Double))
                return OpCodes.Stind_R8;
            else
                return OpCodes.Stind_Ref;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void VerifyDynamicDelegateMethodInfo(
            MethodInfo methodInfo, /* in */
            bool callbackOnly      /* in */
            )
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            Type returnType = methodInfo.ReturnType;

            if (returnType != typeof(object))
            {
                throw new ArgumentException(String.Format(
                    "return type mismatch {0} versus {1}",
                    FormatOps.WrapOrNull(returnType),
                    FormatOps.WrapOrNull(typeof(object))));
            }

            ParameterInfo[] parameterInfo = methodInfo.GetParameters();

            if (parameterInfo == null)
                throw new ArgumentException("missing parameters");

            int parameterCount = parameterInfo.Length;

            if (parameterCount != 2)
                throw new ArgumentException("parameter count mismatch");

            Type[] parameterTypes = {
                parameterInfo[0].ParameterType,
                parameterInfo[1].ParameterType
            };

            Type firstArgumentType = callbackOnly ?
                typeof(ICallback) : typeof(object);

            if (parameterTypes[0] != firstArgumentType)
            {
                throw new ArgumentException(String.Format(
                    "parameter #0 type mismatch {0} versus {1}",
                    FormatOps.WrapOrNull(parameterTypes[0]),
                    FormatOps.WrapOrNull(firstArgumentType)));
            }

            if (parameterTypes[1] != typeof(object[]))
            {
                throw new ArgumentException(String.Format(
                    "parameter #1 type mismatch {0} versus {1}",
                    FormatOps.WrapOrNull(parameterTypes[1]),
                    FormatOps.WrapOrNull(typeof(object[]))));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static OpCode? GetOpCodeForConv(
            Type type /* in */
            )
        {
            if (type == typeof(System.Boolean))
                return OpCodes.Conv_I1;
            else if (type == typeof(SByte))
                return OpCodes.Conv_I1;
            else if (type == typeof(Byte))
                return OpCodes.Conv_U1;
            else if (type == typeof(Char))
                return OpCodes.Conv_U2;
            else if (type == typeof(Int16))
                return OpCodes.Conv_I2;
            else if (type == typeof(UInt16))
                return OpCodes.Conv_U2;
            else if (type == typeof(Int32))
                return OpCodes.Conv_I4;
            else if (type == typeof(UInt32))
                return OpCodes.Conv_U4;
            else if (type == typeof(Int64))
                return OpCodes.Conv_I8;
            else if (type == typeof(UInt64))
                return OpCodes.Conv_U8;
            else if (type == typeof(IntPtr))
                return OpCodes.Conv_I;
            else if (type == typeof(UIntPtr))
                return OpCodes.Conv_U;
            else if (type == typeof(Single))
                return OpCodes.Conv_R4;
            else if (type == typeof(Double))
                return OpCodes.Conv_R8;
            else
                return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static void EmitWrapperMethodBody(
            ILGenerator generator,   /* in */
            MethodInfo methodInfo,   /* in */
            FieldInfo fieldInfo,     /* in */
            Type returnType,         /* in */
            TypeList parameterTypes, /* in */
            bool callbackOnly        /* in */
            )
        {
            if (generator == null)
                throw new ArgumentNullException("generator");

            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            if ((fieldInfo != null) && !fieldInfo.IsStatic)
                throw new ArgumentException("must be static", "fieldInfo");

            if ((returnType != null) && returnType.IsByRef)
                throw new NotSupportedException("ref-return unsupported");

            VerifyDynamicDelegateMethodInfo(methodInfo, callbackOnly);

            LocalBuilder args = generator.DeclareLocal(typeof(object[]));
            LocalBuilder result = null;

            if (NeedReturnType(returnType))
                result = generator.DeclareLocal(returnType);

            generator.Emit(OpCodes.Nop);

            bool haveFieldInfo = (fieldInfo != null);
            bool haveParameters = (parameterTypes != null);
            int parameterCount = haveParameters ? parameterTypes.Count : 0;

            generator.Emit(OpCodes.Ldc_I4, parameterCount);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc, args);

            int baseIndex = haveFieldInfo ? 0 : 1;

            if (haveParameters)
            {
                for (int index = 0; index < parameterCount; index++)
                {
                    Type parameterType = parameterTypes[index];

                    if (parameterType == null)
                        continue;

                    generator.Emit(OpCodes.Ldloc, args);
                    generator.Emit(OpCodes.Ldc_I4, index);
                    generator.Emit(OpCodes.Ldarg, index + baseIndex);

                    bool output = parameterType.IsByRef;

                    Type elementType = output ?
                        parameterType.GetElementType() : parameterType;

                    if (output)
                        generator.Emit(GetOpCodeForLdind(elementType));

                    if (NeedBoxOpCode(elementType))
                        generator.Emit(OpCodes.Box, elementType);

                    generator.Emit(OpCodes.Stelem_Ref); /* object[] */
                }
            }

            if (haveFieldInfo)
                generator.Emit(OpCodes.Ldsfld, fieldInfo);
            else
                generator.Emit(OpCodes.Ldarg_0); /* this */

            generator.Emit(OpCodes.Ldloc, args);

            if (methodInfo.IsStatic)
                generator.Emit(OpCodes.Call, methodInfo); /* Invoke */
            else
                generator.Emit(OpCodes.Callvirt, methodInfo); /* Invoke */

            if (NeedReturnType(returnType))
            {
                if (NeedBoxOpCode(returnType))
                    generator.Emit(OpCodes.Unbox_Any, returnType);
                else if (returnType != typeof(object))
                    generator.Emit(OpCodes.Castclass, returnType);

                generator.Emit(OpCodes.Stloc, result);
            }
            else
            {
                generator.Emit(OpCodes.Pop);
            }

            if (haveParameters)
            {
                for (int index = 0; index < parameterCount; index++)
                {
                    Type parameterType = parameterTypes[index];

                    if ((parameterType == null) || !parameterType.IsByRef)
                        continue;

                    generator.Emit(OpCodes.Ldarg, index + baseIndex);
                    generator.Emit(OpCodes.Ldloc, args);
                    generator.Emit(OpCodes.Ldc_I4, index);
                    generator.Emit(OpCodes.Ldelem_Ref); /* object[] */

                    Type elementType = parameterType.GetElementType();

                    if (NeedBoxOpCode(elementType))
                        generator.Emit(OpCodes.Unbox_Any, elementType);
                    else if (elementType != typeof(object))
                        generator.Emit(OpCodes.Castclass, elementType);

                    generator.Emit(GetOpCodeForStind(elementType));
                }
            }

            if (NeedReturnType(returnType))
                generator.Emit(OpCodes.Ldloc, result);

            generator.Emit(OpCodes.Ret);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void EmitDelegateWrapperMethodBody(
            ILGenerator generator,   /* in */
            MethodInfo methodInfo,   /* in */
            Type returnType,         /* in */
            TypeList parameterTypes, /* in */
            bool callbackOnly        /* in */
            )
        {
            if (generator == null)
                throw new ArgumentNullException("generator");

            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            if ((returnType != null) && returnType.IsByRef)
                throw new NotSupportedException("ref-return unsupported");

            VerifyDynamicDelegateMethodInfo(methodInfo, callbackOnly);

            LocalBuilder args = generator.DeclareLocal(typeof(object[]));
            LocalBuilder result = null;

            if (NeedReturnType(returnType))
                result = generator.DeclareLocal(returnType);

            generator.Emit(OpCodes.Nop);

            bool haveParameters = (parameterTypes != null);
            int parameterCount = haveParameters ? parameterTypes.Count : 0;

            generator.Emit(OpCodes.Ldc_I4, parameterCount);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc, args);

            if (haveParameters)
            {
                for (int index = 0; index < parameterCount; index++)
                {
                    Type parameterType = parameterTypes[index];

                    if (parameterType == null)
                        continue;

                    generator.Emit(OpCodes.Ldloc, args);
                    generator.Emit(OpCodes.Ldc_I4, index);
                    generator.Emit(OpCodes.Ldarg, index + 1);

                    bool output = parameterType.IsByRef;

                    Type elementType = output ?
                        parameterType.GetElementType() : parameterType;

                    if (output)
                        generator.Emit(GetOpCodeForLdind(elementType));

                    if (NeedBoxOpCode(elementType))
                        generator.Emit(OpCodes.Box, elementType);

                    generator.Emit(OpCodes.Stelem_Ref); /* object[] */
                }
            }

            generator.Emit(OpCodes.Ldarg_0); /* this */
            generator.Emit(OpCodes.Ldloc, args);

            if (methodInfo.IsStatic)
                generator.Emit(OpCodes.Call, methodInfo); /* Invoke */
            else
                generator.Emit(OpCodes.Callvirt, methodInfo); /* Invoke */

            if (NeedReturnType(returnType))
            {
                if (NeedBoxOpCode(returnType))
                    generator.Emit(OpCodes.Unbox_Any, returnType);
                else if (returnType != typeof(object))
                    generator.Emit(OpCodes.Castclass, returnType);

                generator.Emit(OpCodes.Stloc, result);
            }
            else
            {
                generator.Emit(OpCodes.Pop);
            }

            if (haveParameters)
            {
                for (int index = 0; index < parameterCount; index++)
                {
                    Type parameterType = parameterTypes[index];

                    if ((parameterType == null) || !parameterType.IsByRef)
                        continue;

                    generator.Emit(OpCodes.Ldarg, index + 1);
                    generator.Emit(OpCodes.Ldloc, args);
                    generator.Emit(OpCodes.Ldc_I4, index);
                    generator.Emit(OpCodes.Ldelem_Ref); /* object[] */

                    Type elementType = parameterType.GetElementType();

                    if (NeedBoxOpCode(elementType))
                        generator.Emit(OpCodes.Unbox_Any, elementType);
                    else if (elementType != typeof(object))
                        generator.Emit(OpCodes.Castclass, elementType);

                    generator.Emit(GetOpCodeForStind(elementType));
                }
            }

            if (NeedReturnType(returnType))
                generator.Emit(OpCodes.Ldloc, result);

            generator.Emit(OpCodes.Ret);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        private static string MakeIDelegateName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(IDelegate).Name, null,
                GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeIModuleName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(IModule).Name, null,
                GlobalState.NextId(interpreter));
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string MakeDelegateName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(Delegate).Name, null,
                GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        private static AssemblyName MakeAssemblyName(
            Interpreter interpreter /* in */
            )
        {
            return new AssemblyName(FormatOps.Id(
                typeof(AssemblyName).Name, null,
                GlobalState.NextTypeId(interpreter)));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string MakeModuleName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(Module).Name, null,
                GlobalState.NextTypeId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string MakeTypeName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(Type).Name, null,
                GlobalState.NextTypeId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        public static ReturnCode LoadNativeModule(
            Interpreter interpreter, /* in */
            ModuleFlags flags,       /* in */
            string fileName,         /* in */
            string moduleName,       /* in */
            ref IModule module,      /* out */
            ref Result error         /* out */
            )
        {
            int loaded = 0;

            return NativeModule.Load(
                interpreter, (moduleName != null) ? moduleName :
                MakeIModuleName(interpreter), flags, fileName,
                ref loaded, ref module, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateDelegateWrapperMethod(
            Interpreter interpreter,   /* in */
            AppDomain appDomain,       /* in */
            AssemblyName assemblyName, /* in */
            string moduleName,         /* in */
            string typeName,           /* in */
            MethodInfo methodInfo,     /* in */
            Type returnType,           /* in */
            TypeList parameterTypes,   /* in */
            ref Type type,             /* out */
            ref Result error           /* out */
            )
        {
            AppDomain localAppDomain;

            if (appDomain != null)
                localAppDomain = appDomain;
            else if (interpreter != null)
                localAppDomain = interpreter.GetAppDomain();
            else
                localAppDomain = null;

            type = CreateDelegateWrapperMethod(localAppDomain,
                (assemblyName != null) ?
                    assemblyName : MakeAssemblyName(interpreter),
                (moduleName != null) ?
                    moduleName : MakeModuleName(interpreter),
                (typeName != null) ?
                    typeName : MakeTypeName(interpreter),
                methodInfo, returnType, parameterTypes,
                ref error);

            return (type != null) ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Type CreateDelegateWrapperMethod(
            AppDomain appDomain,       /* in */
            AssemblyName assemblyName, /* in */
            string moduleName,         /* in */
            string typeName,           /* in */
            MethodInfo methodInfo,     /* in */
            Type returnType,           /* in */
            TypeList parameterTypes,   /* in */
            ref Result error           /* out */
            )
        {
            if (appDomain == null)
            {
                error = "invalid application domain";
                return null;
            }

            if (!AppDomainOps.IsCurrent(appDomain))
            {
                error = "application domain must be current";
                return null;
            }

            if (assemblyName == null)
            {
                error = "invalid assembly name";
                return null;
            }

            if (String.IsNullOrEmpty(moduleName))
            {
                error = "invalid module name";
                return null;
            }

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return null;
            }

            Type type = null;

            try
            {
#if NET_STANDARD_20 && NET_CORE_REFERENCES
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName, DefaultManagedAssemblyBuilderAccess);
#else
                AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(
                    assemblyName, DefaultManagedAssemblyBuilderAccess);
#endif

                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(
                    moduleName);

                TypeBuilder typeBuilder = moduleBuilder.DefineType(
                    typeName, DefaultClassTypeAttributes, typeof(object));

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    InvokeMethodName, DefaultInstanceMethodAttributes,
                    DefaultCallingConventions, returnType,
                    (parameterTypes != null) ? parameterTypes.ToArray() : null);

                ILGenerator generator = methodBuilder.GetILGenerator();

                EmitDelegateWrapperMethodBody(
                    generator, methodInfo, returnType, parameterTypes, false);

#if NET_STANDARD_20 && NET_CORE_REFERENCES
                type = typeBuilder.CreateTypeInfo();
#else
                type = typeBuilder.CreateType();
#endif
            }
            catch (Exception e)
            {
                error = e;
            }

            return type;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateWrapperMethod(
            Interpreter interpreter,   /* in */
            AppDomain appDomain,       /* in */
            AssemblyName assemblyName, /* in */
            string moduleName,         /* in */
            string typeName,           /* in */
            MethodInfo methodInfo,     /* in */
            Type returnType,           /* in */
            TypeList parameterTypes,   /* in */
            bool useStaticMethod,      /* in */
            ref Type type,             /* out */
            ref Result error           /* out */
            )
        {
            AppDomain localAppDomain;

            if (appDomain != null)
                localAppDomain = appDomain;
            else if (interpreter != null)
                localAppDomain = interpreter.GetAppDomain();
            else
                localAppDomain = null;

            type = CreateWrapperMethod(localAppDomain,
                (assemblyName != null) ?
                    assemblyName : MakeAssemblyName(interpreter),
                (moduleName != null) ?
                    moduleName : MakeModuleName(interpreter),
                (typeName != null) ?
                    typeName : MakeTypeName(interpreter),
                methodInfo, returnType, parameterTypes,
                useStaticMethod, ref error);

            return (type != null) ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Type CreateWrapperMethod(
            AppDomain appDomain,       /* in */
            AssemblyName assemblyName, /* in */
            string moduleName,         /* in */
            string typeName,           /* in */
            MethodInfo methodInfo,     /* in */
            Type returnType,           /* in */
            TypeList parameterTypes,   /* in */
            bool useStaticMethod,      /* in */
            ref Result error           /* out */
            )
        {
            if (appDomain == null)
            {
                error = "invalid application domain";
                return null;
            }

            if (!AppDomainOps.IsCurrent(appDomain))
            {
                error = "application domain must be current";
                return null;
            }

            if (assemblyName == null)
            {
                error = "invalid assembly name";
                return null;
            }

            if (String.IsNullOrEmpty(moduleName))
            {
                error = "invalid module name";
                return null;
            }

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return null;
            }

            Type type = null;

            try
            {
#if NET_STANDARD_20 && NET_CORE_REFERENCES
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName, DefaultManagedAssemblyBuilderAccess);
#else
                AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(
                    assemblyName, DefaultManagedAssemblyBuilderAccess);
#endif

                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(
                    moduleName);

                TypeBuilder typeBuilder = moduleBuilder.DefineType(
                    typeName, DefaultClassTypeAttributes, typeof(object));

                FieldBuilder fieldBuilder = useStaticMethod ? typeBuilder.DefineField(
                    FirstArgumentFieldName, typeof(object), DefaultFieldAttributes) : null;

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    InvokeMethodName, useStaticMethod ?
                        DefaultStaticMethodAttributes : DefaultInstanceMethodAttributes,
                    DefaultCallingConventions, returnType,
                    (parameterTypes != null) ? parameterTypes.ToArray() : null);

                ILGenerator generator = methodBuilder.GetILGenerator();

                EmitWrapperMethodBody(
                    generator, methodInfo, fieldBuilder, returnType,
                    parameterTypes, false);

#if NET_STANDARD_20 && NET_CORE_REFERENCES
                type = typeBuilder.CreateTypeInfo();
#else
                type = typeBuilder.CreateType();
#endif
            }
            catch (Exception e)
            {
                error = e;
            }

            return type;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CreateDelegateType(
            Interpreter interpreter, /* in */
            MethodBase method,       /* in */
            ref Type type,           /* out */
            ref Result error         /* out */
            )
        {
            Type returnType;
            TypeList parameterTypes;

            MarshalOps.GetReturnAndParameterTypes(
                method as MethodInfo, out returnType,
                out parameterTypes);

            Result localError = null;

            if ((CreateManagedDelegateType(
                    interpreter, null, null, null, null,
                    returnType, parameterTypes, ref type,
                    ref localError) != ReturnCode.Ok) ||
                (type == null))
            {
                if (localError == null)
                {
                    localError = String.Format(
                        "failed delegate type creation for {0}",
                        FormatOps.WrapOrNull(method));
                }

                error = localError;
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateManagedDelegateType(
            Interpreter interpreter,   /* in */
            AppDomain appDomain,       /* in */
            AssemblyName assemblyName, /* in */
            string moduleName,         /* in */
            string typeName,           /* in */
            Type returnType,           /* in */
            TypeList parameterTypes,   /* in */
            ref Type type,             /* out */
            ref Result error           /* out */
            )
        {
            AppDomain localAppDomain;

            if (appDomain != null)
                localAppDomain = appDomain;
            else if (interpreter != null)
                localAppDomain = interpreter.GetAppDomain();
            else
                localAppDomain = null;

            type = CreateManagedDelegateType(localAppDomain,
                (assemblyName != null) ?
                    assemblyName : MakeAssemblyName(interpreter),
                (moduleName != null) ?
                    moduleName : MakeModuleName(interpreter),
                (typeName != null) ?
                    typeName : MakeTypeName(interpreter),
                returnType, parameterTypes, ref error);

            return (type != null) ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Type CreateManagedDelegateType(
            AppDomain appDomain,       /* in */
            AssemblyName assemblyName, /* in */
            string moduleName,         /* in */
            string typeName,           /* in */
            Type returnType,           /* in */
            TypeList parameterTypes,   /* in */
            ref Result error           /* out */
            )
        {
            if (appDomain == null)
            {
                error = "invalid application domain";
                return null;
            }

            if (!AppDomainOps.IsCurrent(appDomain))
            {
                error = "application domain must be current";
                return null;
            }

            if (assemblyName == null)
            {
                error = "invalid assembly name";
                return null;
            }

            if (String.IsNullOrEmpty(moduleName))
            {
                error = "invalid module name";
                return null;
            }

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return null;
            }

            Type type = null;

            try
            {
#if NET_STANDARD_20 && NET_CORE_REFERENCES
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName, DefaultManagedAssemblyBuilderAccess);
#else
                AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(
                    assemblyName, DefaultManagedAssemblyBuilderAccess);
#endif

                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(
                    moduleName);

                TypeBuilder typeBuilder = moduleBuilder.DefineType(
                    typeName, DefaultClassTypeAttributes, typeof(MulticastDelegate));

                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                    ConstructorMethodAttributes, DefaultCallingConventions,
                    new Type[] { typeof(object), typeof(IntPtr) });

                constructorBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    InvokeMethodName, DefaultInstanceMethodAttributes,
                    DefaultCallingConventions, returnType,
                    (parameterTypes != null) ? parameterTypes.ToArray() : null);

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList beginParameterTypes = (parameterTypes != null) ?
                    new TypeList(parameterTypes) : new TypeList();

                beginParameterTypes.Add(typeof(AsyncCallback));
                beginParameterTypes.Add(typeof(object));

                methodBuilder = typeBuilder.DefineMethod(BeginInvokeMethodName,
                    DefaultInstanceMethodAttributes, DefaultCallingConventions,
                    typeof(IAsyncResult), beginParameterTypes.ToArray());

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList endParameterTypes = new TypeList();

                if (parameterTypes != null)
                    foreach (Type parameterType in parameterTypes)
                        if (parameterType.IsByRef)
                            endParameterTypes.Add(parameterType);

                endParameterTypes.Add(typeof(IAsyncResult));

                methodBuilder = typeBuilder.DefineMethod(EndInvokeMethodName,
                    DefaultInstanceMethodAttributes, DefaultCallingConventions,
                    returnType, endParameterTypes.ToArray());

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

#if NET_STANDARD_20 && NET_CORE_REFERENCES
                type = typeBuilder.CreateTypeInfo();
#else
                type = typeBuilder.CreateType();
#endif
            }
            catch (Exception e)
            {
                error = e;
            }

            return type;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        public static ReturnCode CreateNativeDelegateType(
            Interpreter interpreter,             /* in */
            AppDomain appDomain,                 /* in */
            AssemblyName assemblyName,           /* in */
            string moduleName,                   /* in */
            string typeName,                     /* in */
            CallingConvention callingConvention, /* in */
            bool bestFitMapping,                 /* in */
            CharSet charSet,                     /* in */
            bool setLastError,                   /* in */
            bool throwOnUnmappableChar,          /* in */
            Type returnType,                     /* in */
            TypeList parameterTypes,             /* in */
            string delegateName,                 /* in */
            IModule module,                      /* in */
            string functionName,                 /* in */
            IntPtr address,                      /* in */
            ref IDelegate @delegate,             /* out */
            ref Result error                     /* out */
            )
        {
            AppDomain localAppDomain;

            if (appDomain != null)
                localAppDomain = appDomain;
            else if (interpreter != null)
                localAppDomain = interpreter.GetAppDomain();
            else
                localAppDomain = null;

            Type type = CreateNativeDelegateType(localAppDomain,
                (assemblyName != null) ?
                    assemblyName : MakeAssemblyName(interpreter),
                (moduleName != null) ?
                    moduleName : MakeModuleName(interpreter),
                (typeName != null) ?
                    typeName : MakeTypeName(interpreter),
                callingConvention, bestFitMapping, charSet,
                setLastError, throwOnUnmappableChar, returnType,
                parameterTypes, ref error);

            if (type == null)
                return ReturnCode.Error;

            @delegate = new NativeDelegate(
                (delegateName != null) ?
                    delegateName : MakeIDelegateName(interpreter),
                null, null, ClientData.Empty, interpreter,
                callingConvention, returnType, parameterTypes,
                type, module, functionName, address, 0);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Type CreateNativeDelegateType(
            AppDomain appDomain,                 /* in */
            AssemblyName assemblyName,           /* in */
            string moduleName,                   /* in */
            string typeName,                     /* in */
            CallingConvention callingConvention, /* in */
            bool bestFitMapping,                 /* in */
            CharSet charSet,                     /* in */
            bool setLastError,                   /* in */
            bool throwOnUnmappableChar,          /* in */
            Type returnType,                     /* in */
            TypeList parameterTypes,             /* in */
            ref Result error                     /* out */
            )
        {
            if (appDomain == null)
            {
                error = "invalid application domain";
                return null;
            }

            if (!AppDomainOps.IsCurrent(appDomain))
            {
                error = "application domain must be current";
                return null;
            }

            if (assemblyName == null)
            {
                error = "invalid assembly name";
                return null;
            }

            if (String.IsNullOrEmpty(moduleName))
            {
                error = "invalid module name";
                return null;
            }

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return null;
            }

            Type type = null;

            try
            {
#if NET_STANDARD_20 && NET_CORE_REFERENCES
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    assemblyName, DefaultNativeAssemblyBuilderAccess);
#else
                AssemblyBuilder assemblyBuilder = appDomain.DefineDynamicAssembly(
                    assemblyName, DefaultNativeAssemblyBuilderAccess);
#endif

                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(
                    moduleName);

                TypeBuilder typeBuilder = moduleBuilder.DefineType(
                    typeName, DefaultClassTypeAttributes, typeof(MulticastDelegate));

                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                    ConstructorMethodAttributes, DefaultCallingConventions,
                    new Type[] { typeof(object), typeof(IntPtr) });

                constructorBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    InvokeMethodName, DefaultInstanceMethodAttributes,
                    DefaultCallingConventions, returnType,
                    (parameterTypes != null) ? parameterTypes.ToArray() : null);

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList beginParameterTypes = (parameterTypes != null) ?
                    new TypeList(parameterTypes) : new TypeList();

                beginParameterTypes.Add(typeof(AsyncCallback));
                beginParameterTypes.Add(typeof(object));

                methodBuilder = typeBuilder.DefineMethod(BeginInvokeMethodName,
                    DefaultInstanceMethodAttributes, DefaultCallingConventions,
                    typeof(IAsyncResult), beginParameterTypes.ToArray());

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList endParameterTypes = new TypeList();

                if (parameterTypes != null)
                    foreach (Type parameterType in parameterTypes)
                        if (parameterType.IsByRef)
                            endParameterTypes.Add(parameterType);

                endParameterTypes.Add(typeof(IAsyncResult));

                methodBuilder = typeBuilder.DefineMethod(EndInvokeMethodName,
                    DefaultInstanceMethodAttributes, DefaultCallingConventions,
                    returnType, endParameterTypes.ToArray());

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                Type attributeType = typeof(UnmanagedFunctionPointerAttribute);

                BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PublicInstance, true);

                ConstructorInfo constructorInfo = attributeType.GetConstructor(
                    bindingFlags, null, new Type[] { typeof(CallingConvention) },
                    null);

                FieldInfo[] fieldInfo = {
                    attributeType.GetField(
                        BestFitMappingFieldName, bindingFlags),
                    attributeType.GetField(
                        CharSetFieldName, bindingFlags),
                    attributeType.GetField(
                        SetLastErrorFieldName, bindingFlags),
                    attributeType.GetField(
                        ThrowOnUnmappableCharFieldName, bindingFlags)
                };

                object[] fieldValues = {
                    bestFitMapping,       // default: true
                    charSet,              // default: (CharSet)0
                    setLastError,         // default: false
                    throwOnUnmappableChar // default: false
                };

                CustomAttributeBuilder customAttributeBuilder =
                    new CustomAttributeBuilder(constructorInfo,
                    new object[] { callingConvention }, fieldInfo,
                    fieldValues);

                typeBuilder.SetCustomAttribute(customAttributeBuilder);

#if NET_STANDARD_20 && NET_CORE_REFERENCES
                type = typeBuilder.CreateTypeInfo();
#else
                type = typeBuilder.CreateType();
#endif
            }
            catch (Exception e)
            {
                error = e;
            }

            return type;
        }
#endif
    }
}
