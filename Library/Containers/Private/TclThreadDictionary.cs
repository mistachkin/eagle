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

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Components.Private.Tcl.TclThread>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Private.Tcl.TclThread>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private.Tcl
{
    /// <summary>
    /// This class represents a dictionary that maps string names to native Tcl
    /// thread objects (<see cref="TclThread" />).  It extends the underlying
    /// generic dictionary with helpers for bulk-adding entries and producing a
    /// string form of its keys.
    /// </summary>
    [ObjectId("ef97f200-0d7d-4ea4-936a-748bafebc413")]
    internal sealed class TclThreadDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty Tcl thread dictionary.
        /// </summary>
        public TclThreadDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a Tcl thread dictionary that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
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
        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys that are included in the result.
        /// This parameter may be null, in which case all keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
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
        public void Add(IDictionary<string, TclThread> dictionary)
        {
            foreach (KeyValuePair<string, TclThread> pair in dictionary)
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
        /// <summary>
        /// This method produces a string containing all of the keys of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The keys of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
