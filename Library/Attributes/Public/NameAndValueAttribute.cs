/*
 * NameAndValueAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Components.Public;

namespace Eagle._Attributes
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Property |
        AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    [ObjectId("b94b9a58-deae-480b-8a5a-074ecd857fbb")]
    public sealed class NameAndValueAttribute : Attribute
    {
        #region Private Data
        private string name;

        ///////////////////////////////////////////////////////////////////////

        private string value;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public NameAndValueAttribute(
            string name, /* in */
            string value /* in */
            )
        {
            this.name = name;
            this.value = value;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Value
        {
            get { return value; }
        }
        #endregion
    }
}
