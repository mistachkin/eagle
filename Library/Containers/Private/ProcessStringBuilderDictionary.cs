/*
 * ProcessStringBuilderDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("8fd6cd25-318b-44a6-8d2e-a4466e23617f")]
    internal sealed class ProcessStringBuilderDictionary :
            Dictionary<Process, StringBuilder>
    {
        #region Public Constructors
        public ProcessStringBuilderDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool TryGetBuilder(
            Process process,
            bool nullOk,
            out StringBuilder builder
            )
        {
            if (process == null)
            {
                builder = null;
                return false;
            }

            if (!TryGetValue(process, out builder))
                return false;

            if (!nullOk && (builder == null))
                return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public bool NewData(
            Process process,
            int? capacity
            )
        {
            StringBuilder builder;

            if (TryGetBuilder(process, true, out builder))
                return false;

            /* NO RESULT */
            Add(process, (capacity != null) ?
                StringOps.NewStringBuilder((int)capacity) :
                StringOps.NewStringBuilder());

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AppendData(
            Process process,
            string data
            )
        {
            StringBuilder builder;

            if (!TryGetBuilder(process, false, out builder))
                return false;

            builder.AppendLine(data);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public string GetData(
            Process process
            )
        {
            StringBuilder builder;

            if (!TryGetBuilder(process, false, out builder))
                return null;

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public bool RemoveData(
            Process process
            )
        {
            StringBuilder builder;

            if (!TryGetBuilder(process, true, out builder))
                return false;

            if (builder != null)
            {
                builder.Length = 0;
                builder = null;
            }

            return Remove(process);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            IList<Process> list = new List<Process>(this.Keys);

            return ParserOps<Process>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
