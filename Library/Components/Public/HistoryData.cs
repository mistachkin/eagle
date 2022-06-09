/*
 * HistoryData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("01ea058e-6e10-41db-b89b-c61f8fe7988e")]
    public sealed class HistoryData : IHistoryData
    {
        #region Public Constructors
        public HistoryData()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public HistoryData(
            int levels,
            HistoryFlags flags
            )
            : this()
        {
            this.levels = levels;
            this.flags = flags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHistoryData Members
        private int levels;
        public int Levels
        {
            get { return levels; }
            set { levels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private HistoryFlags flags;
        public HistoryFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList("levels", levels, "flags", flags);
        }
        #endregion
    }
}
