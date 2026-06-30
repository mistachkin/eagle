/*
 * Delegate.cs --
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
using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface represents a managed wrapper around a native function
    /// exported from a module, exposing its calling convention, signature, and
    /// address, and supporting resolution, unresolution, and invocation of that
    /// native function.
    /// </summary>
    [ObjectId("72c65ff9-8391-4f2a-8b8a-c9815cfb8cae")]
    internal interface IDelegate : IIdentifier, IWrapperData
    {
        /// <summary>
        /// Gets the calling convention used by the native function.
        /// </summary>
        CallingConvention CallingConvention { get; }

        /// <summary>
        /// Gets the managed type that the native function's return value is
        /// marshaled to.
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// Gets the list of managed types that the native function's parameters
        /// are marshaled from.
        /// </summary>
        TypeList ParameterTypes { get; }

        /// <summary>
        /// Gets the managed delegate type used to invoke the native function.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the module that exports the native function.
        /// </summary>
        IModule Module { get; }

        /// <summary>
        /// Gets the name of the native function within its module.
        /// </summary>
        string FunctionName { get; }

        /// <summary>
        /// Gets the native address of the resolved function.
        /// </summary>
        IntPtr Address { get; }

        /// <summary>
        /// Gets the method information used to invoke the native function.
        /// </summary>
        MethodInfo MethodInfo { get; }

        /// <summary>
        /// This method resolves the native function within the specified
        /// module, obtaining its address.
        /// </summary>
        /// <param name="module">
        /// The module that exports the native function.
        /// </param>
        /// <param name="functionName">
        /// The name of the native function to resolve.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Resolve(IModule module, string functionName, ref Result error);

        /// <summary>
        /// This method resolves the native function within the specified
        /// module, obtaining its address, and capturing any exception that is
        /// caught.
        /// </summary>
        /// <param name="module">
        /// The module that exports the native function.
        /// </param>
        /// <param name="functionName">
        /// The name of the native function to resolve.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <param name="exception">
        /// Upon failure, this receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Resolve(IModule module, string functionName, ref Result error, ref Exception exception);

        /// <summary>
        /// This method unresolves the native function, releasing its address.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Unresolve(ref Result error);

        /// <summary>
        /// This method unresolves the native function, releasing its address,
        /// and capturing any exception that is caught.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <param name="exception">
        /// Upon failure, this receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Unresolve(ref Result error, ref Exception exception);

        /// <summary>
        /// This method invokes the resolved native function with the specified
        /// arguments.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to pass to the native function.  This parameter may be
        /// null.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, this receives the value returned by the native
        /// function.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Invoke(object[] arguments, ref object returnValue, ref Result error);

        /// <summary>
        /// This method invokes the resolved native function with the specified
        /// arguments, capturing any exception that is caught.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to pass to the native function.  This parameter may be
        /// null.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, this receives the value returned by the native
        /// function.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <param name="exception">
        /// Upon failure, this receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Invoke(object[] arguments, ref object returnValue, ref Result error, ref Exception exception);
    }
}
