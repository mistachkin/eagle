/*
 * MessageEventArgs.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Resources;
using Eagle._Attributes;
using Eagle._Messages;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class is the abstract base class for the event argument types used
    /// to convey a localizable message (for example, a warning or an error)
    /// raised during processing.  It carries the source line numbers, message
    /// identifier, resource name, and message arguments needed to format the
    /// message, and it derives from <see cref="EventArgs" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("a3ce6313-fbc8-4afe-9ddb-2aeec0219f27")]
    public abstract class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs message event arguments from the specified source line
        /// numbers, message identifier, resource name, and message arguments.
        /// </summary>
        /// <param name="sourceLineNumbers">
        /// The collection of source line numbers associated with the message.
        /// </param>
        /// <param name="id">
        /// The identifier of the message.
        /// </param>
        /// <param name="resourceName">
        /// The name of the resource containing the message text.
        /// </param>
        /// <param name="messageArgs">
        /// The arguments used when formatting the message text.
        /// </param>
        internal MessageEventArgs(
            SourceLineNumberCollection sourceLineNumbers,
            long id,
            string resourceName,
            params object[] messageArgs
            )
        {
            this.sourceLineNumbers = sourceLineNumbers;
            this.id = id;
            this.resourceName = resourceName;
            this.messageArgs = messageArgs;
        }

        ///////////////////////////////////////////////////////////////////////

        #region MessageEventArgs Members
        /// <summary>
        /// The collection of source line numbers associated with the message.
        /// </summary>
        private SourceLineNumberCollection sourceLineNumbers;
        /// <summary>
        /// Gets the collection of source line numbers associated with the
        /// message.
        /// </summary>
        public virtual SourceLineNumberCollection SourceLineNumbers
        {
            get { return sourceLineNumbers; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The identifier of the message.
        /// </summary>
        private long id;
        /// <summary>
        /// Gets the identifier of the message.
        /// </summary>
        public virtual long Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the resource containing the message text.
        /// </summary>
        private string resourceName;
        /// <summary>
        /// Gets the name of the resource containing the message text.
        /// </summary>
        public virtual string ResourceName
        {
            get { return resourceName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The arguments used when formatting the message text.
        /// </summary>
        private object[] messageArgs;
        /// <summary>
        /// Gets the arguments used when formatting the message text.
        /// </summary>
        public virtual object[] MessageArgs
        {
            get { return messageArgs; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the resource manager used to look up the message text.
        /// </summary>
        public abstract ResourceManager ResourceManager { get; }
        #endregion
    }
}
