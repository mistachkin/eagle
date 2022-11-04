/*
 * ChannelStreamBuffer.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("ff40efcd-0788-47be-a583-cdcd0a72b66b")]
    internal class ChannelStreamBuffer
    {
        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private ByteList buffer;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ChannelStreamBuffer()
        {
            lock (syncRoot)
            {
                buffer = new ByteList();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public int GetCount()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (buffer == null)
                    return 0;

                return buffer.Count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Append(
            IEnumerable<byte> collection /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (collection == null)
                    return;

                if (buffer == null) /* IMPOSSIBLE? */
                    return;

                buffer.AddRange(collection);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Take(
            ref ByteList buffer /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (this.buffer == null)
                    return false;

                int count = this.buffer.Count;

                if (count == 0)
                    return false;

                if (buffer != null)
                {
                    buffer.AddRange(this.buffer);
                    this.buffer.Clear();
                }
                else
                {
                    buffer = this.buffer;
                    this.buffer = new ByteList();
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Take(
            out byte[] bytes /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (buffer == null)
                {
                    bytes = null;
                    return false;
                }

                int count = buffer.Count;

                if (count == 0)
                {
                    bytes = null;
                    return false;
                }

                bytes = ArrayOps.GetArray<byte>(buffer, true);
                buffer = new ByteList();

                return true;
            }
        }
        #endregion
    }
}
