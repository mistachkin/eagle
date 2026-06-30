/*
 * IntPtrInterpreterDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    System.IntPtr, Eagle._Components.Public.Interpreter>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    System.IntPtr, Eagle._Components.Public.Interpreter>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps native pointer keys to
    /// interpreter values.  It extends the underlying generic dictionary of
    /// <see cref="Interpreter" /> objects, supports cloning, and provides a
    /// helper for producing a filtered string form of its keys.
    /// </summary>
    [ObjectId("f2367147-1421-40a9-8a4a-1558432232d1")]
    internal sealed class IntPtrInterpreterDictionary : SomeDictionary, ICloneable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty native-pointer-to-interpreter dictionary.
        /// </summary>
        public IntPtrInterpreterDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a native-pointer-to-interpreter dictionary that is
        /// initialized with the entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public IntPtrInterpreterDictionary(
            IDictionary<IntPtr, Interpreter> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new dictionary that is a shallow copy of this
        /// dictionary.
        /// </summary>
        /// <returns>
        /// A new dictionary containing the same key/value pairs as this
        /// dictionary.
        /// </returns>
        public object Clone()
        {
            return new IntPtrInterpreterDictionary(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null to include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all the keys of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The list of keys formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
