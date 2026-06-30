/*
 * Operators.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the canonical string literals for the expression
    /// operators recognized by the Eagle expression engine.
    /// </summary>
    [ObjectId("5bf807e6-d673-4e5c-b400-0b4e47d4d449")]
    internal static class Operators
    {
        /// <summary>
        /// The exponentiation operator.
        /// </summary>
        public const string Exponent = "**";
        /// <summary>
        /// The multiplication operator.
        /// </summary>
        public const string Multiply = "*";
        /// <summary>
        /// The division operator.
        /// </summary>
        public const string Divide = "/";
        /// <summary>
        /// The modulus (remainder) operator.
        /// </summary>
        public const string Modulus = "%";
        /// <summary>
        /// The addition operator.
        /// </summary>
        public const string Plus = "+";
        /// <summary>
        /// The subtraction operator.
        /// </summary>
        public const string Minus = "-";
        /// <summary>
        /// The left-shift operator.
        /// </summary>
        public const string LeftShift = "<<";
        /// <summary>
        /// The right-shift operator.
        /// </summary>
        public const string RightShift = ">>";
        /// <summary>
        /// The left-rotate operator.
        /// </summary>
        public const string LeftRotate = "<<<";
        /// <summary>
        /// The right-rotate operator.
        /// </summary>
        public const string RightRotate = ">>>";
        /// <summary>
        /// The less-than relational operator.
        /// </summary>
        public const string LessThan = "<";
        /// <summary>
        /// The greater-than relational operator.
        /// </summary>
        public const string GreaterThan = ">";
        /// <summary>
        /// The less-than-or-equal-to relational operator.
        /// </summary>
        public const string LessThanOrEqualTo = "<=";
        /// <summary>
        /// The greater-than-or-equal-to relational operator.
        /// </summary>
        public const string GreaterThanOrEqualTo = ">=";
        /// <summary>
        /// The equality operator.
        /// </summary>
        public const string Equal = "==";
        /// <summary>
        /// The inequality operator.
        /// </summary>
        public const string NotEqual = "!=";
        /// <summary>
        /// The bitwise-and operator.
        /// </summary>
        public const string BitwiseAnd = "&";
        /// <summary>
        /// The bitwise-exclusive-or operator.
        /// </summary>
        public const string BitwiseXor = "^";
        /// <summary>
        /// The bitwise-or operator.
        /// </summary>
        public const string BitwiseOr = "|";
        /// <summary>
        /// The bitwise-equivalence operator.
        /// </summary>
        public const string BitwiseEqv = "<->";
        /// <summary>
        /// The bitwise-implication operator.
        /// </summary>
        public const string BitwiseImp = "->";
        /// <summary>
        /// The logical-and operator.
        /// </summary>
        public const string LogicalAnd = "&&";
        /// <summary>
        /// The logical-exclusive-or operator.
        /// </summary>
        public const string LogicalXor = "^^";
        /// <summary>
        /// The logical-or operator.
        /// </summary>
        public const string LogicalOr = "||";
        /// <summary>
        /// The logical-equivalence operator.
        /// </summary>
        public const string LogicalEqv = "<=>";
        /// <summary>
        /// The logical-implication operator.
        /// </summary>
        public const string LogicalImp = "=>";
        /// <summary>
        /// The ternary conditional operator.
        /// </summary>
        public const string Question = "?";
        /// <summary>
        /// The logical-not (negation) operator.
        /// </summary>
        public const string LogicalNot = "!";
        /// <summary>
        /// The bitwise-not (complement) operator.
        /// </summary>
        public const string BitwiseNot = "~";
        /// <summary>
        /// The string-equality operator.
        /// </summary>
        public const string StringEqual = "eq";
        /// <summary>
        /// The string greater-than operator.
        /// </summary>
        public const string StringGreaterThan = "gt";
        /// <summary>
        /// The string greater-than-or-equal-to operator.
        /// </summary>
        public const string StringGreaterThanOrEqualTo = "ge";
        /// <summary>
        /// The string less-than operator.
        /// </summary>
        public const string StringLessThan = "lt";
        /// <summary>
        /// The string less-than-or-equal-to operator.
        /// </summary>
        public const string StringLessThanOrEqualTo = "le";
        /// <summary>
        /// The string-inequality operator.
        /// </summary>
        public const string StringNotEqual = "ne";
        /// <summary>
        /// The list-membership operator.
        /// </summary>
        public const string ListIn = "in";
        /// <summary>
        /// The list-non-membership operator.
        /// </summary>
        public const string ListNotIn = "ni";
        /// <summary>
        /// The variable-assignment operator.
        /// </summary>
        public const string VariableAssignment = ":=";
    }
}
