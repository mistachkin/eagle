/*
 * Binder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Reflection;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("eb785af2-b3d2-4ad0-a7f0-5085a87b092e")]
    public interface IBinder
    {
        #region Binder Members
        //
        // NOTE: These members are really from the System.Reflection.Binder
        //       type; however, there is no formal interface defined for them
        //       by the .NET Framework.
        //
        FieldInfo BindToField(
            BindingFlags bindingAttr, FieldInfo[] match, object value,
            CultureInfo culture);

        MethodBase BindToMethod(
            BindingFlags bindingAttr, MethodBase[] match,
            ref object[] args, ParameterModifier[] modifiers,
            CultureInfo culture, string[] names, out object state);

        object ChangeType(
            object value, Type type, CultureInfo culture);

        void ReorderArgumentArray(
            ref object[] args, object state);

        MethodBase SelectMethod(
            BindingFlags bindingAttr, MethodBase[] match,
            Type[] types, ParameterModifier[] modifiers);

        PropertyInfo SelectProperty(
            BindingFlags bindingAttr, PropertyInfo[] match,
            Type returnType, Type[] indexes,
            ParameterModifier[] modifiers);
        #endregion
    }
}
