/*
 * Math.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface extends <see cref="IConvert" /> with the operations used
    /// to evaluate the operators supported by the expression engine, including
    /// arithmetic calculation, string comparison, and list membership testing.
    /// </summary>
    [ObjectId("91ea6aa3-8646-43f0-bd93-9d8c846cdcc6")]
    public interface IMath : IConvert
    {
        /// <summary>
        /// Calculates the result of applying the specified operator to its
        /// operands.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the operator being applied.
        /// </param>
        /// <param name="lexeme">
        /// The <see cref="Lexeme" /> that identifies the operator being
        /// applied.
        /// </param>
        /// <param name="convert">
        /// The object used to convert the operands to and from the types
        /// required by the operator.  This parameter may be null.
        /// </param>
        /// <param name="bits">
        /// The number of bits of precision to use for the calculation, or null
        /// to use the default precision.
        /// </param>
        /// <param name="result">
        /// Upon success, this will contain the result of the calculation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Calculate(
            IIdentifierName identifierName,
            Lexeme lexeme,
            IConvert convert,
            int? bits,
            ref Argument result,
            ref Result error
        );

        /// <summary>
        /// Compares two operands as strings using the specified operator and
        /// comparison type.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the operator being applied.
        /// </param>
        /// <param name="lexeme">
        /// The <see cref="Lexeme" /> that identifies the operator being
        /// applied.
        /// </param>
        /// <param name="convert">
        /// The object used to convert the operands to and from the types
        /// required by the operator.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The <see cref="StringComparison" /> that controls how the strings
        /// are compared.
        /// </param>
        /// <param name="result">
        /// Upon success, this will contain the result of the comparison.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode StringCompare(
            IIdentifierName identifierName,
            Lexeme lexeme,
            IConvert convert,
            StringComparison comparisonType,
            ref Argument result,
            ref Result error
        );

        /// <summary>
        /// Determines whether a list operand may contain the specified element
        /// operand using the specified operator and comparison type.
        /// </summary>
        /// <param name="identifierName">
        /// The name of the operator being applied.
        /// </param>
        /// <param name="lexeme">
        /// The <see cref="Lexeme" /> that identifies the operator being
        /// applied.
        /// </param>
        /// <param name="convert">
        /// The object used to convert the operands to and from the types
        /// required by the operator.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The <see cref="StringComparison" /> that controls how the list
        /// elements are compared.
        /// </param>
        /// <param name="result">
        /// Upon success, this will contain the result of the membership test.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListMayContain(
            IIdentifierName identifierName,
            Lexeme lexeme,
            IConvert convert,
            StringComparison comparisonType,
            ref Argument result,
            ref Result error
        );
    }
}
