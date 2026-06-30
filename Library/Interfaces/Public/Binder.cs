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
    /// <summary>
    /// This interface defines the contract for selecting and binding to
    /// reflection members (fields, methods, and properties) and for coercing
    /// argument values during such binding.  Its members mirror those of the
    /// <see cref="System.Reflection.Binder" /> type, for which the .NET
    /// Framework does not define a formal interface.
    /// </summary>
    [ObjectId("eb785af2-b3d2-4ad0-a7f0-5085a87b092e")]
    public interface IBinder
    {
        #region Binder Members
        //
        // NOTE: These members are really from the System.Reflection.Binder
        //       type; however, there is no formal interface defined for them
        //       by the .NET Framework.
        //
        /// <summary>
        /// Selects a field from the specified set of candidates based on the
        /// specified criteria.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how the candidate fields are
        /// filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate fields to select from.
        /// </param>
        /// <param name="value">
        /// The value that is intended to be assigned to the selected field.
        /// </param>
        /// <param name="culture">
        /// The culture used to control any value coercion.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The selected field, or null if no suitable field is found.
        /// </returns>
        FieldInfo BindToField(
            BindingFlags bindingAttr, FieldInfo[] match, object value,
            CultureInfo culture);

        /// <summary>
        /// Selects a method from the specified set of candidates based on the
        /// specified criteria and prepares its arguments for invocation.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how the candidate methods are
        /// filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate methods to select from.
        /// </param>
        /// <param name="args">
        /// On input, the arguments intended for the method; upon return, the
        /// arguments possibly reordered and coerced to match the selected
        /// method.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the arguments.  This
        /// parameter may be null.
        /// </param>
        /// <param name="culture">
        /// The culture used to control any value coercion.  This parameter may
        /// be null.
        /// </param>
        /// <param name="names">
        /// The names of the arguments, used to support named arguments.  This
        /// parameter may be null.
        /// </param>
        /// <param name="state">
        /// Upon return, receives an opaque object that records any argument
        /// reordering performed, for later use by
        /// <see cref="ReorderArgumentArray" />.
        /// </param>
        /// <returns>
        /// The selected method, or null if no suitable method is found.
        /// </returns>
        MethodBase BindToMethod(
            BindingFlags bindingAttr, MethodBase[] match,
            ref object[] args, ParameterModifier[] modifiers,
            CultureInfo culture, string[] names, out object state);

        /// <summary>
        /// Converts the specified value to the specified type, using the
        /// specified culture.
        /// </summary>
        /// <param name="value">
        /// The value to convert.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type to convert the value to.
        /// </param>
        /// <param name="culture">
        /// The culture used to control the conversion.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The converted value.
        /// </returns>
        object ChangeType(
            object value, Type type, CultureInfo culture);

        /// <summary>
        /// Restores the argument array to its original order after a method
        /// invocation, reversing any reordering performed by
        /// <see cref="BindToMethod" />.
        /// </summary>
        /// <param name="args">
        /// The argument array to restore to its original order.
        /// </param>
        /// <param name="state">
        /// The opaque state object that was produced by
        /// <see cref="BindToMethod" />.
        /// </param>
        void ReorderArgumentArray(
            ref object[] args, object state);

        /// <summary>
        /// Selects a method from the specified set of candidates based on the
        /// specified parameter types.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how the candidate methods are
        /// filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate methods to select from.
        /// </param>
        /// <param name="types">
        /// The parameter types used to match against the candidate methods.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the parameter types.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The selected method, or null if no suitable method is found.
        /// </returns>
        MethodBase SelectMethod(
            BindingFlags bindingAttr, MethodBase[] match,
            Type[] types, ParameterModifier[] modifiers);

        /// <summary>
        /// Selects a property from the specified set of candidates based on the
        /// specified criteria.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags that control how the candidate properties are
        /// filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate properties to select from.
        /// </param>
        /// <param name="returnType">
        /// The return type that the selected property must have.  This
        /// parameter may be null.
        /// </param>
        /// <param name="indexes">
        /// The types of the index parameters for an indexed property.  This
        /// parameter may be null.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the index parameters.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The selected property, or null if no suitable property is found.
        /// </returns>
        PropertyInfo SelectProperty(
            BindingFlags bindingAttr, PropertyInfo[] match,
            Type returnType, Type[] indexes,
            ParameterModifier[] modifiers);
        #endregion
    }
}
