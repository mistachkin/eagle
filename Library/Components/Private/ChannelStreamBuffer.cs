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
    /// <summary>
    /// This class provides a thread-safe accumulating buffer of bytes used to
    /// stage data for a channel stream, supporting appending bytes and
    /// atomically taking the buffered contents.
    /// </summary>
    [ObjectId("ff40efcd-0788-47be-a583-cdcd0a72b66b")]
    internal class ChannelStreamBuffer
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the buffer across threads.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The underlying list of accumulated bytes.
        /// </summary>
        private ByteList buffer;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of the channel stream buffer.
        /// </summary>
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
        /// <summary>
        /// This method returns the number of bytes currently held in the
        /// buffer.
        /// </summary>
        /// <returns>
        /// The number of buffered bytes, or zero when the buffer is empty.
        /// </returns>
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

        /// <summary>
        /// This method appends a collection of bytes to the end of the buffer.
        /// </summary>
        /// <param name="collection">
        /// The bytes to append; if this is null, no action is taken.
        /// </param>
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

        /// <summary>
        /// This method atomically removes all buffered bytes and transfers them
        /// to the supplied byte list.
        /// </summary>
        /// <param name="buffer">
        /// When this references an existing list, the buffered bytes are added
        /// to it and the internal buffer is cleared; when this is null, it
        /// receives the internal list directly and the internal buffer is
        /// replaced with a new empty list.
        /// </param>
        /// <returns>
        /// True if any bytes were taken; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method atomically removes all buffered bytes and returns them as
        /// a newly allocated byte array.
        /// </summary>
        /// <param name="bytes">
        /// Upon success, receives a new array containing the buffered bytes;
        /// upon failure, receives null.
        /// </param>
        /// <returns>
        /// True if any bytes were taken; otherwise, false.
        /// </returns>
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
