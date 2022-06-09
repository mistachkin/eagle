/*
 * LexemeAttribute.cs --
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
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("fde70ba7-2de8-42b5-9d36-3bb348c0fdd8")]
    public sealed class LexemeAttribute : Attribute
    {
        public LexemeAttribute(Lexeme lexeme)
        {
            this.lexeme = lexeme;
        }

        ///////////////////////////////////////////////////////////////////////

        public LexemeAttribute(string value)
        {
            lexeme = (Lexeme)Enum.Parse(
                typeof(Lexeme), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private Lexeme lexeme;
        public Lexeme Lexeme
        {
            get { return lexeme; }
        }
    }
}
