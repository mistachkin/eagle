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
    /// <summary>
    /// This attribute is used to associate a lexeme with the class it is
    /// applied to, such as an operator or function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("fde70ba7-2de8-42b5-9d36-3bb348c0fdd8")]
    public sealed class LexemeAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified lexeme.
        /// </summary>
        /// <param name="lexeme">
        /// The lexeme to associate with the marked class.
        /// </param>
        public LexemeAttribute(Lexeme lexeme)
        {
            this.lexeme = lexeme;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the lexeme.
        /// </summary>
        /// <param name="value">
        /// The lexeme, as a string, to associate with the marked class.  This
        /// value must be parsable as a value of the <see cref="Lexeme" />
        /// enumeration.
        /// </param>
        public LexemeAttribute(string value)
        {
            lexeme = (Lexeme)Enum.Parse(
                typeof(Lexeme), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The lexeme associated with the marked class.
        /// </summary>
        private Lexeme lexeme;
        /// <summary>
        /// Gets the lexeme associated with the marked class.
        /// </summary>
        public Lexeme Lexeme
        {
            get { return lexeme; }
        }
    }
}
