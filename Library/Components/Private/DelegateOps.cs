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
    /// <summary>
    /// This class provides a collection of static helper methods used to build
    /// dynamic delegate, delegate-wrapper, and (optionally) native delegate
    /// types at runtime via reflection emit, along with the supporting IL
    /// generation and name-generation logic.
    /// </summary>
    [ObjectId("cb1b3474-f840-4b2d-bcbc-5502f7a82232")]
    internal static class DelegateOps
    {
        #region Private Constants
        /// <summary>
        /// The default type attributes used when emitting a dynamic class or
        /// delegate type.
        /// </summary>
        private const TypeAttributes DefaultClassTypeAttributes =
            TypeAttributes.AnsiClass | TypeAttributes.AutoLayout |
            TypeAttributes.NotPublic | TypeAttributes.Sealed;

        /// <summary>
        /// The default method attributes used when emitting a dynamic instance
        /// method.
        /// </summary>
        private const MethodAttributes DefaultInstanceMethodAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.Virtual | MethodAttributes.NewSlot;

        /// <summary>
        /// The default method attributes used when emitting a dynamic static
        /// method.
        /// </summary>
        private const MethodAttributes DefaultStaticMethodAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.Static;

        /// <summary>
        /// The default field attributes used when emitting a dynamic field.
        /// </summary>
        private const FieldAttributes DefaultFieldAttributes =
            FieldAttributes.Private | FieldAttributes.Static;

        /// <summary>
        /// The default calling conventions used when emitting a dynamic method
        /// or constructor.
        /// </summary>
        private const CallingConventions DefaultCallingConventions =
            CallingConventions.Standard;

        /// <summary>
        /// The method attributes used when emitting a dynamic delegate
        /// constructor.
        /// </summary>
        private const MethodAttributes ConstructorMethodAttributes =
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        /// <summary>
        /// The method implementation attributes used for the runtime-supplied
        /// methods of a dynamic delegate type.
        /// </summary>
        private const MethodImplAttributes DefaultMethodImplAttributes =
            MethodImplAttributes.Managed | MethodImplAttributes.Runtime;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The assembly builder access mode used when emitting a dynamic managed
        /// delegate or wrapper assembly.
        /// </summary>
        private const AssemblyBuilderAccess DefaultManagedAssemblyBuilderAccess =
#if NET_40
            AssemblyBuilderAccess.RunAndCollect;
#else
            AssemblyBuilderAccess.Run;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        /// <summary>
        /// The assembly builder access mode used when emitting a dynamic native
        /// delegate assembly.
        /// </summary>
        private const AssemblyBuilderAccess DefaultNativeAssemblyBuilderAccess =
            AssemblyBuilderAccess.Run;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the static field, emitted into a wrapper type, that holds
        /// the first argument bound to the wrapped method.
        /// </summary>
        internal const string FirstArgumentFieldName = "firstArgument";

        /// <summary>
        /// The name of the primary invocation method emitted into a dynamic
        /// delegate or wrapper type.
        /// </summary>
        internal const string InvokeMethodName = "Invoke";

        /// <summary>
        /// The name of the asynchronous begin-invocation method emitted into a
        /// dynamic delegate type.
        /// </summary>
        private const string BeginInvokeMethodName = "BeginInvoke";

        /// <summary>
        /// The name of the asynchronous end-invocation method emitted into a
        /// dynamic delegate type.
        /// </summary>
        private const string EndInvokeMethodName = "EndInvoke";

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        /// <summary>
        /// The name of the best-fit-mapping field on the unmanaged function
        /// pointer attribute, used when applying it to a native delegate type.
        /// </summary>
        private const string BestFitMappingFieldName = "BestFitMapping";

        /// <summary>
        /// The name of the character set field on the unmanaged function pointer
        /// attribute, used when applying it to a native delegate type.
        /// </summary>
        private const string CharSetFieldName = "CharSet";

        /// <summary>
        /// The name of the set-last-error field on the unmanaged function
        /// pointer attribute, used when applying it to a native delegate type.
        /// </summary>
        private const string SetLastErrorFieldName = "SetLastError";

        /// <summary>
        /// The name of the throw-on-unmappable-char field on the unmanaged
        /// function pointer attribute, used when applying it to a native
        /// delegate type.
        /// </summary>
        private const string ThrowOnUnmappableCharFieldName = "ThrowOnUnmappableChar";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the public instance invocation method of the
        /// specified delegate type.
        /// </summary>
        /// <param name="type">
        /// The delegate type whose invocation method is queried.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The invocation method of the delegate type, or null if the type is
        /// null or has no such method.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified type represents a
        /// meaningful return value (i.e. is non-null and is not the void type).
        /// </summary>
        /// <param name="type">
        /// The candidate return type to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the type represents a meaningful return value; otherwise,
        /// false.
        /// </returns>
        private static bool NeedReturnType(
            Type type /* in */
            )
        {
            return (type != null) && (type != typeof(void));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified delegate has a
        /// meaningful return type and, if so, reports that type.
        /// </summary>
        /// <param name="delegate">
        /// The delegate whose method return type is examined.  This parameter
        /// may be null.
        /// </param>
        /// <param name="type">
        /// Upon success, this receives the return type of the delegate's method.
        /// </param>
        /// <returns>
        /// True if the delegate has a meaningful return type; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a value of the specified type must be
        /// boxed (or unboxed) when stored into or read from an object array,
        /// i.e. whether it is a value type or a generic type parameter.
        /// </summary>
        /// <param name="type">
        /// The type to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if a box or unbox opcode is required for the type; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method selects the indirect-load (Ldind) opcode appropriate for
        /// dereferencing a pointer to a value of the specified type.
        /// </summary>
        /// <param name="type">
        /// The element type being dereferenced.
        /// </param>
        /// <returns>
        /// The indirect-load opcode for the type, defaulting to the
        /// reference-load opcode for non-primitive types.
        /// </returns>
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

        /// <summary>
        /// This method selects the indirect-store (Stind) opcode appropriate for
        /// writing a value of the specified type through a pointer.
        /// </summary>
        /// <param name="type">
        /// The element type being stored.
        /// </param>
        /// <returns>
        /// The indirect-store opcode for the type, defaulting to the
        /// reference-store opcode for non-primitive types.
        /// </returns>
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

        /// <summary>
        /// This method verifies that the specified method has the signature
        /// required to back a dynamically emitted delegate or wrapper, namely a
        /// return type of object and exactly two parameters: a first argument
        /// (a callback or object) and an object array of remaining arguments.
        /// </summary>
        /// <param name="methodInfo">
        /// The method to verify.  This parameter may not be null.
        /// </param>
        /// <param name="callbackOnly">
        /// Non-zero if the first parameter must be a callback type; zero if it
        /// may be any object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="methodInfo" /> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the method does not have the required signature.
        /// </exception>
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
        /// <summary>
        /// This method gets the intermediate language conversion opcode that
        /// corresponds to the specified type.
        /// </summary>
        /// <param name="type">
        /// The type for which the conversion opcode is to be returned.
        /// </param>
        /// <returns>
        /// The conversion opcode for the type, or null if the type has no
        /// corresponding conversion opcode.
        /// </returns>
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

        /// <summary>
        /// This method emits the IL body of a wrapper method that packs its
        /// parameters into an object array, invokes the backing method (passing
        /// either a static first-argument field or the instance), unpacks the
        /// result and any by-reference parameters, and returns.
        /// </summary>
        /// <param name="generator">
        /// The IL generator into which the method body is emitted.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="methodInfo">
        /// The backing method that the emitted wrapper invokes.  This parameter
        /// may not be null.
        /// </param>
        /// <param name="fieldInfo">
        /// The optional static field holding the first argument; when supplied,
        /// the wrapper is treated as static.  This parameter may be null.
        /// </param>
        /// <param name="returnType">
        /// The return type of the wrapper method.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the wrapper method.  This parameter
        /// may be null.
        /// </param>
        /// <param name="callbackOnly">
        /// Non-zero if the backing method's first parameter must be a callback
        /// type; zero if it may be any object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generator" /> or
        /// <paramref name="methodInfo" /> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="fieldInfo" /> is supplied but is not
        /// static.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when <paramref name="returnType" /> is a by-reference type.
        /// </exception>
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

        /// <summary>
        /// This method emits the IL body of a delegate wrapper method that packs
        /// its parameters into an object array, invokes the backing method on the
        /// instance, unpacks the result and any by-reference parameters, and
        /// returns.
        /// </summary>
        /// <param name="generator">
        /// The IL generator into which the method body is emitted.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="methodInfo">
        /// The backing method that the emitted wrapper invokes.  This parameter
        /// may not be null.
        /// </param>
        /// <param name="returnType">
        /// The return type of the wrapper method.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the wrapper method.  This parameter
        /// may be null.
        /// </param>
        /// <param name="callbackOnly">
        /// Non-zero if the backing method's first parameter must be a callback
        /// type; zero if it may be any object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="generator" /> or
        /// <paramref name="methodInfo" /> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when <paramref name="returnType" /> is a by-reference type.
        /// </exception>
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
        /// <summary>
        /// This method generates a unique name for a native delegate, based on
        /// the delegate interface type name and a per-interpreter identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the next unique identifier.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The generated native delegate name.
        /// </returns>
        private static string MakeIDelegateName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(IDelegate).Name, null,
                GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates a unique name for a native module, based on the
        /// module interface type name and a per-interpreter identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the next unique identifier.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The generated native module name.
        /// </returns>
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

        /// <summary>
        /// This method generates a unique name for a delegate, based on the
        /// delegate type name and a per-interpreter identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the next unique identifier.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The generated delegate name.
        /// </returns>
        public static string MakeDelegateName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(Delegate).Name, null,
                GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates a unique assembly name for a dynamic assembly,
        /// based on a per-interpreter type identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the next unique type identifier.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The generated assembly name.
        /// </returns>
        private static AssemblyName MakeAssemblyName(
            Interpreter interpreter /* in */
            )
        {
            return new AssemblyName(FormatOps.Id(
                typeof(AssemblyName).Name, null,
                GlobalState.NextTypeId(interpreter)));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates a unique module name for a dynamic module, based
        /// on a per-interpreter type identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the next unique type identifier.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The generated module name.
        /// </returns>
        private static string MakeModuleName(
            Interpreter interpreter /* in */
            )
        {
            return FormatOps.Id(
                typeof(Module).Name, null,
                GlobalState.NextTypeId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates a unique type name for a dynamic type, based on
        /// a per-interpreter type identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the next unique type identifier.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The generated type name.
        /// </returns>
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
        /// <summary>
        /// This method loads a native module from the specified file, generating
        /// a module name when one is not supplied.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter on whose behalf the module is loaded.  This parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the native module is loaded.
        /// </param>
        /// <param name="fileName">
        /// The file name of the native module to load.
        /// </param>
        /// <param name="moduleName">
        /// The name to assign to the loaded module, or null to generate one.
        /// </param>
        /// <param name="module">
        /// Upon success, this receives the loaded native module.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method creates a dynamic type containing a delegate wrapper
        /// method, resolving the application domain, assembly name, module name,
        /// and type name (generating any that are not supplied).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the application domain and to generate
        /// names.  This parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to emit the type, or null to use the
        /// interpreter's application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use, or null to generate one.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use, or null to generate one.
        /// </param>
        /// <param name="typeName">
        /// The type name to use, or null to generate one.
        /// </param>
        /// <param name="methodInfo">
        /// The backing method that the emitted wrapper invokes.
        /// </param>
        /// <param name="returnType">
        /// The return type of the wrapper method.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the wrapper method.  This parameter
        /// may be null.
        /// </param>
        /// <param name="type">
        /// Upon success, this receives the created wrapper type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method emits a dynamic type containing a delegate wrapper method,
        /// using the fully resolved application domain, assembly name, module
        /// name, and type name.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain in which to emit the type.  This parameter may
        /// not be null and must be the current application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use for the emitted assembly.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use for the emitted module.  This parameter may not
        /// be null or empty.
        /// </param>
        /// <param name="typeName">
        /// The type name to use for the emitted type.  This parameter may not be
        /// null or empty.
        /// </param>
        /// <param name="methodInfo">
        /// The backing method that the emitted wrapper invokes.
        /// </param>
        /// <param name="returnType">
        /// The return type of the wrapper method.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the wrapper method.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The created wrapper type, or null on failure.
        /// </returns>
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

        /// <summary>
        /// This method creates a dynamic type containing a wrapper method (with
        /// an optional static first-argument field), resolving the application
        /// domain, assembly name, module name, and type name (generating any that
        /// are not supplied).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the application domain and to generate
        /// names.  This parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to emit the type, or null to use the
        /// interpreter's application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use, or null to generate one.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use, or null to generate one.
        /// </param>
        /// <param name="typeName">
        /// The type name to use, or null to generate one.
        /// </param>
        /// <param name="methodInfo">
        /// The backing method that the emitted wrapper invokes.
        /// </param>
        /// <param name="returnType">
        /// The return type of the wrapper method.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the wrapper method.  This parameter
        /// may be null.
        /// </param>
        /// <param name="useStaticMethod">
        /// Non-zero to emit a static wrapper method backed by a static
        /// first-argument field; zero to emit an instance wrapper method.
        /// </param>
        /// <param name="type">
        /// Upon success, this receives the created wrapper type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method emits a dynamic type containing a wrapper method (with an
        /// optional static first-argument field), using the fully resolved
        /// application domain, assembly name, module name, and type name.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain in which to emit the type.  This parameter may
        /// not be null and must be the current application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use for the emitted assembly.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use for the emitted module.  This parameter may not
        /// be null or empty.
        /// </param>
        /// <param name="typeName">
        /// The type name to use for the emitted type.  This parameter may not be
        /// null or empty.
        /// </param>
        /// <param name="methodInfo">
        /// The backing method that the emitted wrapper invokes.
        /// </param>
        /// <param name="returnType">
        /// The return type of the wrapper method.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the wrapper method.  This parameter
        /// may be null.
        /// </param>
        /// <param name="useStaticMethod">
        /// Non-zero to emit a static wrapper method backed by a static
        /// first-argument field; zero to emit an instance wrapper method.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The created wrapper type, or null on failure.
        /// </returns>
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

        /// <summary>
        /// This method creates a managed delegate type whose signature matches
        /// the return and parameter types of the specified method.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the application domain and to generate
        /// names.  This parameter may be null.
        /// </param>
        /// <param name="method">
        /// The method whose return and parameter types define the delegate
        /// signature.
        /// </param>
        /// <param name="type">
        /// Upon success, this receives the created delegate type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the delegate type was created successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method creates a managed delegate type with the specified return
        /// and parameter types, resolving the application domain, assembly name,
        /// module name, and type name (generating any that are not supplied).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the application domain and to generate
        /// names.  This parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to emit the type, or null to use the
        /// interpreter's application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use, or null to generate one.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use, or null to generate one.
        /// </param>
        /// <param name="typeName">
        /// The type name to use, or null to generate one.
        /// </param>
        /// <param name="returnType">
        /// The return type of the delegate.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the delegate.  This parameter may be
        /// null.
        /// </param>
        /// <param name="type">
        /// Upon success, this receives the created delegate type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method emits a managed delegate type (with its constructor and
        /// the runtime-supplied Invoke, BeginInvoke, and EndInvoke methods) with
        /// the specified return and parameter types, using the fully resolved
        /// application domain, assembly name, module name, and type name.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain in which to emit the type.  This parameter may
        /// not be null and must be the current application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use for the emitted assembly.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use for the emitted module.  This parameter may not
        /// be null or empty.
        /// </param>
        /// <param name="typeName">
        /// The type name to use for the emitted type.  This parameter may not be
        /// null or empty.
        /// </param>
        /// <param name="returnType">
        /// The return type of the delegate.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the delegate.  This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The created delegate type, or null on failure.
        /// </returns>
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
        /// <summary>
        /// This method creates a native (interop) delegate type and wraps it in a
        /// native delegate bound to the specified module function or address,
        /// resolving the application domain, assembly name, module name, type
        /// name, and delegate name (generating any that are not supplied).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve the application domain and to generate
        /// names.  This parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to emit the type, or null to use the
        /// interpreter's application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use, or null to generate one.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use, or null to generate one.
        /// </param>
        /// <param name="typeName">
        /// The type name to use, or null to generate one.
        /// </param>
        /// <param name="callingConvention">
        /// The unmanaged calling convention applied to the native delegate type.
        /// </param>
        /// <param name="bestFitMapping">
        /// Non-zero to enable best-fit mapping of characters during marshaling.
        /// </param>
        /// <param name="charSet">
        /// The character set used when marshaling string arguments.
        /// </param>
        /// <param name="setLastError">
        /// Non-zero if the last error code should be preserved after the call.
        /// </param>
        /// <param name="throwOnUnmappableChar">
        /// Non-zero to throw when an unmappable character is encountered during
        /// marshaling.
        /// </param>
        /// <param name="returnType">
        /// The return type of the native delegate.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the native delegate.  This parameter
        /// may be null.
        /// </param>
        /// <param name="delegateName">
        /// The name to assign to the created native delegate, or null to generate
        /// one.
        /// </param>
        /// <param name="module">
        /// The native module that exports the target function, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="functionName">
        /// The name of the exported function to bind, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="address">
        /// The address of the target function to bind, if known.
        /// </param>
        /// <param name="delegate">
        /// Upon success, this receives the created native delegate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method emits a native (interop) delegate type (with its
        /// constructor, the runtime-supplied Invoke, BeginInvoke, and EndInvoke
        /// methods, and an unmanaged function pointer attribute describing the
        /// marshaling behavior), using the fully resolved application domain,
        /// assembly name, module name, and type name.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain in which to emit the type.  This parameter may
        /// not be null and must be the current application domain.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to use for the emitted assembly.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="moduleName">
        /// The module name to use for the emitted module.  This parameter may not
        /// be null or empty.
        /// </param>
        /// <param name="typeName">
        /// The type name to use for the emitted type.  This parameter may not be
        /// null or empty.
        /// </param>
        /// <param name="callingConvention">
        /// The unmanaged calling convention applied to the native delegate type.
        /// </param>
        /// <param name="bestFitMapping">
        /// Non-zero to enable best-fit mapping of characters during marshaling.
        /// </param>
        /// <param name="charSet">
        /// The character set used when marshaling string arguments.
        /// </param>
        /// <param name="setLastError">
        /// Non-zero if the last error code should be preserved after the call.
        /// </param>
        /// <param name="throwOnUnmappableChar">
        /// Non-zero to throw when an unmappable character is encountered during
        /// marshaling.
        /// </param>
        /// <param name="returnType">
        /// The return type of the native delegate.  This parameter may be null.
        /// </param>
        /// <param name="parameterTypes">
        /// The list of parameter types of the native delegate.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The created native delegate type, or null on failure.
        /// </returns>
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
