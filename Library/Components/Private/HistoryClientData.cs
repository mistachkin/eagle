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
    /// <summary>
    /// This class carries the client data associated with a single command
    /// history entry, bundling the command arguments, the call stack level at
    /// which the command was executed, and the history flags that classify the
    /// entry.
    /// </summary>
    [ObjectId("92cf4d84-f479-4a02-8017-8e1ca7d6137c")]
    internal sealed class HistoryClientData : ClientData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance with only the underlying client data,
        /// leaving the history-specific fields at their default values.  The
        /// public constructor overload delegates to this one.
        /// </summary>
        /// <param name="data">
        /// The opaque client data to associate with this instance.  This
        /// parameter may be null.
        /// </param>
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
        /// <summary>
        /// Constructs an instance from the underlying client data together with
        /// the command arguments, call stack level, and history flags for a
        /// command history entry.
        /// </summary>
        /// <param name="data">
        /// The opaque client data to associate with this instance.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list for the command being recorded in history.
        /// </param>
        /// <param name="levels">
        /// The call stack level at which the command was executed.
        /// </param>
        /// <param name="flags">
        /// The history flags that classify this command history entry.
        /// </param>
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
        /// <summary>
        /// The argument list for the command recorded in this history entry.
        /// </summary>
        private ArgumentList arguments;

        /// <summary>
        /// Gets or sets the argument list for the command recorded in this
        /// history entry.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The call stack level at which the command recorded in this history
        /// entry was executed.
        /// </summary>
        private int levels;

        /// <summary>
        /// Gets or sets the call stack level at which the command recorded in
        /// this history entry was executed.
        /// </summary>
        public int Levels
        {
            get { return levels; }
            set { levels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The history flags that classify the command recorded in this history
        /// entry.
        /// </summary>
        private HistoryFlags flags;

        /// <summary>
        /// Gets or sets the history flags that classify the command recorded in
        /// this history entry.
        /// </summary>
        public HistoryFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion
    }
}
