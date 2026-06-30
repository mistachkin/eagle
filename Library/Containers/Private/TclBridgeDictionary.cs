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

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Components.Private.Tcl.TclBridge>;

#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Private.Tcl.TclBridge>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private.Tcl
{
    /// <summary>
    /// This class represents a dictionary that maps string names to native Tcl
    /// bridge objects (<see cref="TclBridge" />).  It extends the underlying
    /// generic dictionary with helpers for bulk-adding entries and producing a
    /// string form of its entries.
    /// </summary>
    [ObjectId("44c35e4c-8d85-4758-8482-5658d2555cbf")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBridgeDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty Tcl bridge dictionary.
        /// </summary>
        public TclBridgeDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a Tcl bridge dictionary that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
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
        /// <summary>
        /// This method produces a string containing the keys and values of the
        /// dictionary whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys that are included in the result.
        /// This parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The matching keys and values formatted as a string.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            return GenericOps<string, TclBridge>.DictionaryToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all of the key/value pairs from the specified
        /// dictionary to this dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are added.
        /// </param>
        public void Add(IDictionary<string, TclBridge> dictionary)
        {
            foreach (KeyValuePair<string, TclBridge> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the key/value pairs from the specified input
        /// dictionary to this dictionary, skipping any keys that are already
        /// present.  Any skipped pairs are collected into the output dictionary,
        /// which is created on demand.
        /// </summary>
        /// <param name="inputDictionary">
        /// The dictionary whose key/value pairs are added.
        /// </param>
        /// <param name="outputDictionary">
        /// Receives the key/value pairs that were skipped because their keys
        /// were already present.  This dictionary is created if necessary.
        /// </param>
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
        /// <summary>
        /// This method produces a string containing all of the keys and values
        /// of the dictionary.
        /// </summary>
        /// <returns>
        /// The keys and values of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
