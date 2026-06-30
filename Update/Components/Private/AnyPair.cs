/*
 * AnyPair.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class represents an immutable pair of values of arbitrary types.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first value in the pair.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second value in the pair.
    /// </typeparam>
    [Guid("31162bc6-cc67-4147-9bbc-a6df3064d729")]
    internal sealed class AnyPair<T1, T2>
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        /// <summary>
        /// Constructs a new instance of this class whose values are both the
        /// default for their respective types.
        /// </summary>
        public AnyPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class with the specified first
        /// value and the default second value.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        public AnyPair(T1 x)
            : this()
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class with the specified first and
        /// second values.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        /// <param name="y">
        /// The second value of the pair.
        /// </param>
        public AnyPair(T1 x, T2 y)
            : this(x)
        {
            this.y = y;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The first value of the pair.
        /// </summary>
        private T1 x;

        /// <summary>
        /// Gets the first value of the pair.
        /// </summary>
        public T1 X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The second value of the pair.
        /// </summary>
        private T2 y;

        /// <summary>
        /// Gets the second value of the pair.
        /// </summary>
        public T2 Y
        {
            get { return y; }
        }
        #endregion
    }
}
