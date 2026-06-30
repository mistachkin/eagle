/*
 * Triplet.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a triplet, i.e. an ordered grouping of three
    /// values.  It is an aggregate of <see cref="IAnyTriplet" /> and is
    /// reserved for internal use; most code should not implement or consume
    /// this interface directly.
    /// </summary>
    [ObjectId("4e3c58cc-3358-42c2-ab26-f16f7b205651")]
    public interface ITriplet : IAnyTriplet /* INTERNAL: DO NOT USE */
    {
        // nothing.
    }

    ///////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This interface represents a strongly typed triplet, i.e. an ordered
    /// grouping of three values that all share the same type.  It is an
    /// aggregate of <see cref="IAnyTriplet{T1, T2, T3}" /> with all three
    /// type arguments set to the same type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of all three values in the triplet.
    /// </typeparam>
    [ObjectId("23f69942-2259-403c-8474-53c3b740423c")]
    public interface ITriplet<T> : IAnyTriplet<T, T, T>
    {
        // nothing.
    }
}
