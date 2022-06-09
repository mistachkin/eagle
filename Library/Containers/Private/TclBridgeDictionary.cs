/*
 * TclBridgeDictionary.cs --
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private.Tcl
{
    [ObjectId("44c35e4c-8d85-4758-8482-5658d2555cbf")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBridgeDictionary : Dictionary<string, TclBridge>
    {
        #region Public Constructors
        public TclBridgeDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public TclBridgeDictionary(
            IDictionary<string, TclBridge> dictionary
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
            return GenericOps<string, TclBridge>.DictionaryToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(IDictionary<string, TclBridge> dictionary)
        {
            foreach (KeyValuePair<string, TclBridge> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void MaybeAdd(
            IDictionary<string, TclBridge> inputDictionary,
            ref TclBridgeDictionary outputDictionary
            )
        {
            foreach (KeyValuePair<string, TclBridge> pair in inputDictionary)
            {
                if (this.ContainsKey(pair.Key))
                {
                    if (outputDictionary == null)
                        outputDictionary = new TclBridgeDictionary();

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
