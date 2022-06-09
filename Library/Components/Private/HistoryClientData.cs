/*
 * HistoryClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("92cf4d84-f479-4a02-8017-8e1ca7d6137c")]
    internal sealed class HistoryClientData : ClientData
    {
        #region Private Constructors
        private HistoryClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public HistoryClientData(
            object data,
            ArgumentList arguments,
            int levels,
            HistoryFlags flags
            )
            : this(data)
        {
            this.arguments = arguments;
            this.levels = levels;
            this.flags = flags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private ArgumentList arguments;
        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

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
    }
}
