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
    [ObjectId("72c65ff9-8391-4f2a-8b8a-c9815cfb8cae")]
    internal interface IDelegate : IIdentifier, IWrapperData
    {
        CallingConvention CallingConvention { get; }
        Type ReturnType { get; }
        TypeList ParameterTypes { get; }
        Type Type { get; }
        IModule Module { get; }
        string FunctionName { get; }
        IntPtr Address { get; }
        MethodInfo MethodInfo { get; }

        ReturnCode Resolve(IModule module, string functionName, ref Result error);
        ReturnCode Resolve(IModule module, string functionName, ref Result error, ref Exception exception);

        ReturnCode Unresolve(ref Result error);
        ReturnCode Unresolve(ref Result error, ref Exception exception);

        ReturnCode Invoke(object[] arguments, ref object returnValue, ref Result error);
        ReturnCode Invoke(object[] arguments, ref object returnValue, ref Result error, ref Exception exception);
    }
}
