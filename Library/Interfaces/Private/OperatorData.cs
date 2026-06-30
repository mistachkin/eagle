/*
 * OperatorData.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by entities that carry the identity and
    /// metadata describing an expression operator.  In addition to the
    /// identity (<see cref="IIdentifier" />), wrapper bookkeeping
    /// (<see cref="IWrapperData" />), owning plugin
    /// (<see cref="IHavePlugin" />), and type-and-name
    /// (<see cref="ITypeAndName" />) information it composes, it exposes the
    /// operator-specific attributes used when the expression engine recognizes
    /// and dispatches the operator.
    /// </summary>
    [ObjectId("f9854ec8-39f3-489a-a32e-7da95a51e264")]
    internal interface IOperatorData : IIdentifier, IWrapperData, IHavePlugin, ITypeAndName
    {
        /// <summary>
        /// Gets or sets the lexeme that identifies this operator to the
        /// expression parser.
        /// </summary>
        Lexeme Lexeme { get; set; }

        /// <summary>
        /// Gets or sets the number of operands this operator accepts (e.g. one
        /// for a unary operator, two for a binary operator).
        /// </summary>
        int Operands { get; set; }

        /// <summary>
        /// Gets or sets the list of operand types supported by this operator.
        /// </summary>
        TypeList Types { get; set; }

        /// <summary>
        /// Gets or sets the flags that control the behavior and capabilities
        /// of this operator.
        /// </summary>
        OperatorFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets the string comparison type used by this operator when
        /// it compares string operands.
        /// </summary>
        StringComparison ComparisonType { get; set; }
    }
}
