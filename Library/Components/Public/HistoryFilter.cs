/*
 * HistoryFilter.cs --
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
    [ObjectId("a785bf4d-3017-4d2b-b148-564aaa63e904")]
    public sealed class HistoryFilter : IHistoryFilter
    {
        #region Public Constructors
        public HistoryFilter()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public HistoryFilter(
            int startLevel,
            int stopLevel,
            HistoryFlags hasFlags,
            HistoryFlags notHasFlags,
            bool hasAll,
            bool notHasAll
            )
            : this()
        {
            this.startLevel = startLevel;
            this.stopLevel = stopLevel;
            this.hasFlags = hasFlags;
            this.notHasFlags = notHasFlags;
            this.hasAll = hasAll;
            this.notHasAll = notHasAll;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHistoryFilter Members
        private int startLevel;
        public int StartLevel
        {
            get { return startLevel; }
            set { startLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int stopLevel;
        public int StopLevel
        {
            get { return stopLevel; }
            set { stopLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private HistoryFlags hasFlags;
        public HistoryFlags HasFlags
        {
            get { return hasFlags; }
            set { hasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private HistoryFlags notHasFlags;
        public HistoryFlags NotHasFlags
        {
            get { return notHasFlags; }
            set { notHasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hasAll;
        public bool HasAll
        {
            get { return hasAll; }
            set { hasAll = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool notHasAll;
        public bool NotHasAll
        {
            get { return notHasAll; }
            set { notHasAll = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList(
                "startLevel", startLevel, "stopLevel", stopLevel, "stopLevel",
                "hasFlags", hasFlags, "notHasFlags", notHasFlags, "hasAll",
                hasAll, "notHasAll", notHasAll);
        }
        #endregion
    }
}
