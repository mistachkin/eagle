/*
 * TclThreadDictionary.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Private.Tcl;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private.Tcl
{
    [ObjectId("ef97f200-0d7d-4ea4-936a-748bafebc413")]
    internal sealed class TclThreadDictionary : Dictionary<string, TclThread>
    {
        #region Public Constructors
        public TclThreadDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public TclThreadDictionary(
            IDictionary<string, TclThread> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public string ToString(string pattern, bool noCase)
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(IDictionary<string, TclThread> dictionary)
        {
            foreach (KeyValuePair<string, TclThread> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void MaybeAdd(
            IDictionary<string, TclThread> inputDictionary,
            ref TclThreadDictionary outputDictionary
            )
        {
            foreach (KeyValuePair<string, TclThread> pair in inputDictionary)
            {
                if (this.ContainsKey(pair.Key))
                {
                    if (outputDictionary == null)
                        outputDictionary = new TclThreadDictionary();

                    outputDictionary.Add(pair.Key, pair.Value);
                    continue;
                }

                this.Add(pair.Key, pair.Value);
            }
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
