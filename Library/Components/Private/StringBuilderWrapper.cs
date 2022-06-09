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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("60d8d811-67d9-4a0e-a9ba-1db653599b08")]
    internal sealed class StringBuilderWrapper : IHaveStringBuilder
    {
        #region Private Data
        private long id;

        ///////////////////////////////////////////////////////////////////////

        private int readWriteCount;

        ///////////////////////////////////////////////////////////////////////

        private StringBuilder builder;
        private ArgumentList arguments;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        public long Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        public int ReadWriteCount
        {
            get { return readWriteCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public StringBuilder Builder
        {
            get { return builder; }
            set { throw new NotImplementedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public StringBuilder BuilderForReadWrite
        {
            get { MaybeReplaceStringBuilders(); return builder; }
        }

        ///////////////////////////////////////////////////////////////////////

        public StringBuilder BuilderForReadOnly
        {
            get { return builder; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void DoneWithReadWrite()
        {
            readWriteCount++;
        }

        ///////////////////////////////////////////////////////////////////////

        public void DoneWithReadOnly()
        {
            readWriteCount = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(
            object obj
            )
        {
            if (builder == null)
                return false;

            return builder.Equals(obj);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            if (builder == null)
                return 0;

            return builder.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            if (builder == null)
                return null;

            return builder.ToString();
        }
        #endregion
    }
}
