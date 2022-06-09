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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("a3ce6313-fbc8-4afe-9ddb-2aeec0219f27")]
    public abstract class MessageEventArgs : EventArgs
    {
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
        private SourceLineNumberCollection sourceLineNumbers;
        public virtual SourceLineNumberCollection SourceLineNumbers
        {
            get { return sourceLineNumbers; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long id;
        public virtual long Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string resourceName;
        public virtual string ResourceName
        {
            get { return resourceName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object[] messageArgs;
        public virtual object[] MessageArgs
        {
            get { return messageArgs; }
        }

        ///////////////////////////////////////////////////////////////////////

        public abstract ResourceManager ResourceManager { get; }
        #endregion
    }
}
