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

        private const MethodAttributes DefaultMethodAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.Virtual | MethodAttributes.NewSlot;

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
            Type type
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
            Type type
            )
        {
            return (type != null) && (type != typeof(void));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool NeedReturnType(
            Delegate @delegate,
            ref Type type
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
            Type type
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
            Type type
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
            Type type
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
            MethodInfo methodInfo,
            bool callbackOnly
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

            if (callbackOnly &&
                (parameterTypes[0] != typeof(ICallback)))
            {
                throw new ArgumentException(String.Format(
                    "parameter #0 type mismatch {0} versus {1}",
                    FormatOps.WrapOrNull(parameterTypes[0]),
                    FormatOps.WrapOrNull(typeof(ICallback))));
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
            Type type
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

        public static void EmitDelegateWrapperMethodBody(
            ILGenerator generator,
            MethodInfo methodInfo,
            Type returnType,
            TypeList parameterTypes,
            bool callbackOnly
            )
        {
            if (generator == null)
                throw new ArgumentNullException("generator");

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

            if (methodInfo.IsVirtual && !methodInfo.IsFinal)
                generator.Emit(OpCodes.Callvirt, methodInfo); /* Invoke */
            else
                generator.Emit(OpCodes.Call, methodInfo); /* Invoke */

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
            Interpreter interpreter
            )
        {
            return FormatOps.Id(
                typeof(IDelegate).Name, null,
                GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeIModuleName(
            Interpreter interpreter
            )
        {
            return FormatOps.Id(
                typeof(IModule).Name, null,
                GlobalState.NextId(interpreter));
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string MakeDelegateName(
            Interpreter interpreter
            )
        {
            return FormatOps.Id(
                typeof(Delegate).Name, null,
                GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        private static AssemblyName MakeAssemblyName(
            Interpreter interpreter
            )
        {
            return new AssemblyName(FormatOps.Id(
                typeof(AssemblyName).Name, null,
                GlobalState.NextTypeId(interpreter)));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string MakeModuleName(
            Interpreter interpreter
            )
        {
            return FormatOps.Id(
                typeof(Module).Name, null,
                GlobalState.NextTypeId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string MakeTypeName(
            Interpreter interpreter
            )
        {
            return FormatOps.Id(
                typeof(Type).Name, null,
                GlobalState.NextTypeId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        public static ReturnCode LoadNativeModule(
            Interpreter interpreter,
            ModuleFlags flags,
            string fileName,
            string moduleName,
            ref IModule module,
            ref Result error
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
            Interpreter interpreter,
            AppDomain appDomain,
            AssemblyName assemblyName,
            string moduleName,
            string typeName,
            MethodInfo methodInfo,
            Type returnType,
            TypeList parameterTypes,
            ref Type type,
            ref Result error
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
                methodInfo, returnType, parameterTypes, ref error);

            return (type != null) ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Type CreateDelegateWrapperMethod(
            AppDomain appDomain,
            AssemblyName assemblyName,
            string moduleName,
            string typeName,
            MethodInfo methodInfo,
            Type returnType,
            TypeList parameterTypes,
            ref Result error
            )
        {
            if (appDomain == null)
            {
                error = "invalid application domain";
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
                    InvokeMethodName, DefaultMethodAttributes,
                    DefaultCallingConventions, returnType,
                    (parameterTypes != null) ? parameterTypes.ToArray() :
                    null);

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

        public static ReturnCode CreateManagedDelegateType(
            Interpreter interpreter,
            AppDomain appDomain,
            AssemblyName assemblyName,
            string moduleName,
            string typeName,
            Type returnType,
            TypeList parameterTypes,
            ref Type type,
            ref Result error
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
            AppDomain appDomain,
            AssemblyName assemblyName,
            string moduleName,
            string typeName,
            Type returnType,
            TypeList parameterTypes,
            ref Result error
            )
        {
            if (appDomain == null)
            {
                error = "invalid application domain";
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
                    InvokeMethodName, DefaultMethodAttributes, DefaultCallingConventions,
                    returnType, (parameterTypes != null) ? parameterTypes.ToArray() : null);

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList beginParameterTypes = (parameterTypes != null) ?
                    new TypeList(parameterTypes) : new TypeList();

                beginParameterTypes.Add(typeof(AsyncCallback));
                beginParameterTypes.Add(typeof(object));

                methodBuilder = typeBuilder.DefineMethod(BeginInvokeMethodName,
                    DefaultMethodAttributes, DefaultCallingConventions,
                    typeof(IAsyncResult), beginParameterTypes.ToArray());

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList endParameterTypes = new TypeList();

                if (parameterTypes != null)
                    foreach (Type parameterType in parameterTypes)
                        if (parameterType.IsByRef)
                            endParameterTypes.Add(parameterType);

                endParameterTypes.Add(typeof(IAsyncResult));

                methodBuilder = typeBuilder.DefineMethod(EndInvokeMethodName,
                    DefaultMethodAttributes, DefaultCallingConventions,
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
            Interpreter interpreter,
            AppDomain appDomain,
            AssemblyName assemblyName,
            string moduleName,
            string typeName,
            CallingConvention callingConvention,
            bool bestFitMapping,
            CharSet charSet,
            bool setLastError,
            bool throwOnUnmappableChar,
            Type returnType,
            TypeList parameterTypes,
            string delegateName,
            IModule module,
            string functionName,
            IntPtr address,
            ref IDelegate @delegate,
            ref Result error
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
            AppDomain appDomain,
            AssemblyName assemblyName,
            string moduleName,
            string typeName,
            CallingConvention callingConvention,
            bool bestFitMapping,
            CharSet charSet,
            bool setLastError,
            bool throwOnUnmappableChar,
            Type returnType,
            TypeList parameterTypes,
            ref Result error
            )
        {
            if (appDomain == null)
            {
                error = "invalid application domain";
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
                    InvokeMethodName, DefaultMethodAttributes, DefaultCallingConventions,
                    returnType, (parameterTypes != null) ? parameterTypes.ToArray() : null);

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList beginParameterTypes = (parameterTypes != null) ?
                    new TypeList(parameterTypes) : new TypeList();

                beginParameterTypes.Add(typeof(AsyncCallback));
                beginParameterTypes.Add(typeof(object));

                methodBuilder = typeBuilder.DefineMethod(BeginInvokeMethodName,
                    DefaultMethodAttributes, DefaultCallingConventions,
                    typeof(IAsyncResult), beginParameterTypes.ToArray());

                methodBuilder.SetImplementationFlags(DefaultMethodImplAttributes);

                TypeList endParameterTypes = new TypeList();

                if (parameterTypes != null)
                    foreach (Type parameterType in parameterTypes)
                        if (parameterType.IsByRef)
                            endParameterTypes.Add(parameterType);

                endParameterTypes.Add(typeof(IAsyncResult));

                methodBuilder = typeBuilder.DefineMethod(EndInvokeMethodName,
                    DefaultMethodAttributes, DefaultCallingConventions,
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
