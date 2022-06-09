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
    [ObjectId("3b3ca143-25c9-4976-a971-107ae0824220")]
    internal sealed class DefaultBinder : Binder, IBinder
    {
        #region Private Data
        private Binder binder;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public DefaultBinder(
            Binder binder
            )
        {
            this.binder = binder;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Binder / IBinder Members
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

        public override object ChangeType(
            object value,       /* in */
            Type type,          /* in */
            CultureInfo culture /* in */
            ) /* throw */
        {
            return binder.ChangeType(value, type, culture);
        }

        ///////////////////////////////////////////////////////////////////////

        public override void ReorderArgumentArray(
            ref object[] args, /* in, out */
            object state       /* in */
            )
        {
            binder.ReorderArgumentArray(ref args, state);
        }

        ///////////////////////////////////////////////////////////////////////

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
