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
    [Guid("31162bc6-cc67-4147-9bbc-a6df3064d729")]
    internal sealed class AnyPair<T1, T2>
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        public AnyPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyPair(T1 x)
            : this()
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyPair(T1 x, T2 y)
            : this(x)
        {
            this.y = y;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private T1 x;
        public T1 X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T2 y;
        public T2 Y
        {
            get { return y; }
        }
        #endregion
    }
}
