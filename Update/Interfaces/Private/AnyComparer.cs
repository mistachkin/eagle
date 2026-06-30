/*
 * AnyComparer.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by entities that can both order and test
    /// the equality of values of a given type, by combining the
    /// <see cref="IComparer{T}" /> and <see cref="IEqualityComparer{T}" />
    /// interfaces.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the values to be compared.
    /// </typeparam>
    [Guid("9f6d5089-825f-4514-b539-589a1784717f")]
    internal interface IAnyComparer<T> : IComparer<T>, IEqualityComparer<T>
    {
        // nothing.
    }
}
