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
    [ObjectId("5bf807e6-d673-4e5c-b400-0b4e47d4d449")]
    internal static class Operators
    {
        public const string Exponent = "**";
        public const string Multiply = "*";
        public const string Divide = "/";
        public const string Modulus = "%";
        public const string Plus = "+";
        public const string Minus = "-";
        public const string LeftShift = "<<";
        public const string RightShift = ">>";
        public const string LeftRotate = "<<<";
        public const string RightRotate = ">>>";
        public const string LessThan = "<";
        public const string GreaterThan = ">";
        public const string LessThanOrEqualTo = "<=";
        public const string GreaterThanOrEqualTo = ">=";
        public const string Equal = "==";
        public const string NotEqual = "!=";
        public const string BitwiseAnd = "&";
        public const string BitwiseXor = "^";
        public const string BitwiseOr = "|";
        public const string BitwiseEqv = "<->";
        public const string BitwiseImp = "->";
        public const string LogicalAnd = "&&";
        public const string LogicalXor = "^^";
        public const string LogicalOr = "||";
        public const string LogicalEqv = "<=>";
        public const string LogicalImp = "=>";
        public const string Question = "?";
        public const string LogicalNot = "!";
        public const string BitwiseNot = "~";
        public const string StringEqual = "eq";
        public const string StringGreaterThan = "gt";
        public const string StringGreaterThanOrEqualTo = "ge";
        public const string StringLessThan = "lt";
        public const string StringLessThanOrEqualTo = "le";
        public const string StringNotEqual = "ne";
        public const string ListIn = "in";
        public const string ListNotIn = "ni";
        public const string VariableAssignment = ":=";
    }
}
