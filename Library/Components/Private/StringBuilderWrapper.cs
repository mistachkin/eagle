/*
 * StringBuilderWrapper.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class wraps a <see cref="StringBuilder" /> so that it can be shared
    /// among multiple arguments while supporting copy-on-write semantics: when
    /// the wrapped builder is about to be modified, any arguments that still
    /// reference the original builder are given private copies instead.  It
    /// implements <see cref="IHaveStringBuilder" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("60d8d811-67d9-4a0e-a9ba-1db653599b08")]
    internal sealed class StringBuilderWrapper : IHaveStringBuilder
    {
        #region Private Data
        /// <summary>
        /// The unique identifier assigned to this wrapper instance.
        /// </summary>
        private long id;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of times the wrapped builder has been obtained for
        /// read/write access since the last read-only access.
        /// </summary>
        private int readWriteCount;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The wrapped <see cref="StringBuilder" /> instance.
        /// </summary>
        private StringBuilder builder;
        /// <summary>
        /// The arguments that may share the wrapped builder and are subject to
        /// copy-on-write handling.
        /// </summary>
        private ArgumentList arguments;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a wrapper around the specified
        /// <see cref="StringBuilder" />, assigning it a new unique identifier.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder" /> to wrap.  This parameter may be
        /// null.
        /// </param>
        public StringBuilderWrapper(
            StringBuilder builder
            )
        {
            this.id = GlobalState.NextId();
            this.readWriteCount = 0;
            this.builder = builder;
            this.arguments = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Implements copy-on-write by replacing the wrapped builder, within any
        /// argument that still references it, with a private copy so that
        /// subsequent modifications do not affect those arguments.
        /// </summary>
        private void MaybeReplaceStringBuilders()
        {
            if (arguments == null)
                return;

            foreach (Argument argument in arguments)
            {
                if (argument == null)
                    continue;

                StringBuilder oldBuilder = argument.Value as StringBuilder;

                if ((oldBuilder == null) ||
                    !Object.ReferenceEquals(oldBuilder, builder))
                {
                    continue;
                }

                StringBuilder newBuilder = StringOps.CopyStringBuilder(
                    oldBuilder);

                argument.ResetValue(newBuilder);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveStringBuilder Members
        /// <summary>
        /// Gets the unique identifier assigned to this wrapper instance.
        /// </summary>
        public long Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of times the wrapped builder has been obtained for
        /// read/write access since the last read-only access.
        /// </summary>
        public int ReadWriteCount
        {
            get { return readWriteCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the arguments that may share the wrapped builder and are
        /// subject to copy-on-write handling.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the wrapped <see cref="StringBuilder" /> instance.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public StringBuilder Builder
        {
            get { return builder; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the wrapped <see cref="StringBuilder" /> for read/write access,
        /// first performing copy-on-write so that sharing arguments are not
        /// affected by subsequent modifications.
        /// </summary>
        public StringBuilder BuilderForReadWrite
        {
            get { MaybeReplaceStringBuilders(); return builder; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the wrapped <see cref="StringBuilder" /> for read-only access.
        /// </summary>
        public StringBuilder BuilderForReadOnly
        {
            get { return builder; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that a read/write access of the wrapped builder
        /// has completed.
        /// </summary>
        public void DoneWithReadWrite()
        {
            readWriteCount++;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that a read-only access of the wrapped builder
        /// has completed, resetting the read/write access count.
        /// </summary>
        public void DoneWithReadOnly()
        {
            readWriteCount = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Determines whether the specified object is equal to the wrapped
        /// builder.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the wrapped builder.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// True if the specified object is equal to the wrapped builder;
        /// otherwise, false.  Returns false if there is no wrapped builder.
        /// </returns>
        public override bool Equals(
            object obj
            )
        {
            if (builder == null)
                return false;

            return builder.Equals(obj);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a hash code for this instance, based on the wrapped builder.
        /// </summary>
        /// <returns>
        /// The hash code of the wrapped builder, or zero if there is no wrapped
        /// builder.
        /// </returns>
        public override int GetHashCode()
        {
            if (builder == null)
                return 0;

            return builder.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the string representation of the wrapped builder.
        /// </summary>
        /// <returns>
        /// The string value of the wrapped builder, or null if there is no
        /// wrapped builder.
        /// </returns>
        public override string ToString()
        {
            if (builder == null)
                return null;

            return builder.ToString();
        }
        #endregion
    }
}
