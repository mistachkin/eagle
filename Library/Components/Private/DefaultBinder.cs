/*
 * DefaultBinder.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides an <see cref="IBinder" /> implementation that wraps
    /// an existing .NET reflection <see cref="Binder" /> and forwards every
    /// member binding operation to it.  It is used to adapt a standard binder
    /// so that it can be consumed wherever an <see cref="IBinder" /> is
    /// expected.
    /// </summary>
    [ObjectId("3b3ca143-25c9-4976-a971-107ae0824220")]
    internal sealed class DefaultBinder : Binder, IBinder
    {
        #region Private Data
        /// <summary>
        /// The underlying reflection binder to which all binding operations are
        /// forwarded.
        /// </summary>
        private Binder binder;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class that wraps the specified
        /// reflection binder.
        /// </summary>
        /// <param name="binder">
        /// The underlying reflection binder to forward all binding operations
        /// to.
        /// </param>
        public DefaultBinder(
            Binder binder
            )
        {
            this.binder = binder;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Binder / IBinder Members
        /// <summary>
        /// This method selects a field from the supplied set of candidates that
        /// matches the specified criteria, forwarding the request to the wrapped
        /// binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags controlling how the candidate fields are filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate fields to select from.
        /// </param>
        /// <param name="value">
        /// The value that will be assigned to the selected field.  This
        /// parameter may be null.
        /// </param>
        /// <param name="culture">
        /// The culture to use when interpreting the value, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The matching field, or null if no suitable field was found.
        /// </returns>
        public override FieldInfo BindToField(
            BindingFlags bindingAttr, /* in */
            FieldInfo[] match,        /* in, out */
            object value,             /* in */
            CultureInfo culture       /* in */
            )
        {
            return binder.BindToField(bindingAttr, match, value, culture);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a method from the supplied set of candidates
        /// that matches the specified arguments, forwarding the request to the
        /// wrapped binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags controlling how the candidate methods are filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate methods to select from.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the selected method; these may be
        /// reordered or coerced by the binder.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the arguments, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="culture">
        /// The culture to use when coercing the arguments, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="names">
        /// The names of the arguments, used to support named-argument binding,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="state">
        /// Upon return, receives the binder-specific state object that records
        /// any argument reordering performed; this is passed to
        /// <see cref="ReorderArgumentArray" />.
        /// </param>
        /// <returns>
        /// The matching method, or null if no suitable method was found.
        /// </returns>
        public override MethodBase BindToMethod(
            BindingFlags bindingAttr,      /* in */
            MethodBase[] match,            /* in, out */
            ref object[] args,             /* in, out */
            ParameterModifier[] modifiers, /* in, out */
            CultureInfo culture,           /* in */
            string[] names,                /* in */
            out object state               /* out */
            )
        {
            return binder.BindToMethod(
                bindingAttr, match, ref args, modifiers, culture, names,
                out state);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to the specified type,
        /// forwarding the request to the wrapped binder.
        /// </summary>
        /// <param name="value">
        /// The value to convert.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type to convert the value to.
        /// </param>
        /// <param name="culture">
        /// The culture to use when performing the conversion, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The converted value.
        /// </returns>
        public override object ChangeType(
            object value,       /* in */
            Type type,          /* in */
            CultureInfo culture /* in */
            ) /* throw */
        {
            return binder.ChangeType(value, type, culture);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the argument array to its original order after a
        /// method invocation, forwarding the request to the wrapped binder.
        /// </summary>
        /// <param name="args">
        /// The argument array to restore to its original order.
        /// </param>
        /// <param name="state">
        /// The binder-specific state object that was produced by
        /// <see cref="BindToMethod" /> and describes the reordering to undo.
        /// </param>
        public override void ReorderArgumentArray(
            ref object[] args, /* in, out */
            object state       /* in */
            )
        {
            binder.ReorderArgumentArray(ref args, state);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a method from the supplied set of candidates
        /// based on the specified parameter types, forwarding the request to the
        /// wrapped binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags controlling how the candidate methods are filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate methods to select from.
        /// </param>
        /// <param name="types">
        /// The parameter types used to select the matching method.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the parameter types, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The matching method, or null if no suitable method was found.
        /// </returns>
        public override MethodBase SelectMethod(
            BindingFlags bindingAttr,     /* in */
            MethodBase[] match,           /* in, out */
            Type[] types,                 /* in */
            ParameterModifier[] modifiers /* in, out */
            )
        {
            return binder.SelectMethod(bindingAttr, match, types, modifiers);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a property from the supplied set of candidates
        /// based on the specified return type and index types, forwarding the
        /// request to the wrapped binder.
        /// </summary>
        /// <param name="bindingAttr">
        /// The binding flags controlling how the candidate properties are
        /// filtered.
        /// </param>
        /// <param name="match">
        /// The set of candidate properties to select from.
        /// </param>
        /// <param name="returnType">
        /// The return type used to select the matching property, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="indexes">
        /// The index parameter types used to select the matching property, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="modifiers">
        /// The parameter modifiers associated with the index types, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The matching property, or null if no suitable property was found.
        /// </returns>
        public override PropertyInfo SelectProperty(
            BindingFlags bindingAttr,     /* in */
            PropertyInfo[] match,         /* in, out */
            Type returnType,              /* in */
            Type[] indexes,               /* in */
            ParameterModifier[] modifiers /* in, out */
            )
        {
            return binder.SelectProperty(
                bindingAttr, match, returnType, indexes, modifiers);
        }
        #endregion
    }
}
