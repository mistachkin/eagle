/*
 * InterpreterDictionary.cs --
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

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Components.Public.Interpreter>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Public.Interpreter>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps interpreter identifiers (as
    /// strings) to their associated interpreters.  It extends the standard
    /// dictionary with convenience methods for adding and removing interpreters
    /// by value and for conversion to the Eagle string list format.
    /// </summary>
    [ObjectId("59be82f4-ecdc-4faa-96ff-4405dddcfdf5")]
    public sealed class InterpreterDictionary :
            SomeDictionary
#if DEAD_CODE
            , ICloneable
#endif
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public InterpreterDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new dictionary.
        /// </param>
        public InterpreterDictionary(
            IDictionary<string, Interpreter> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// equality comparer for its keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        public InterpreterDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the specified
        /// interpreters, each keyed by its identifier.
        /// </summary>
        /// <param name="collection">
        /// The collection of interpreters used to populate the new dictionary.
        /// </param>
        public InterpreterDictionary(
            IEnumerable<Interpreter> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds each interpreter in the specified collection to this
        /// dictionary, keyed by its identifier.
        /// </summary>
        /// <param name="collection">
        /// The collection of interpreters to add to this dictionary.
        /// </param>
        public void Add(
            IEnumerable<Interpreter> collection
            )
        {
            foreach (Interpreter item in collection)
                this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified interpreter to this dictionary, keyed by its
        /// identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to add to this dictionary.
        /// </param>
        public void Add(
            Interpreter interpreter
            )
        {
            this.Add(interpreter.IdNoThrow.ToString(), interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the specified interpreter from this dictionary, using its
        /// identifier as the key.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to remove from this dictionary.
        /// </param>
        /// <returns>
        /// True if the interpreter was found and removed; otherwise, false.
        /// </returns>
        public bool Remove(
            Interpreter interpreter
            )
        {
            return this.Remove(interpreter.IdNoThrow.ToString());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new dictionary that is a copy of this dictionary.  The
        /// interpreter values themselves are not cloned.
        /// </summary>
        /// <returns>
        /// The newly created copy of this dictionary.
        /// </returns>
        public InterpreterDictionary DeepCopy()
        {
            return new InterpreterDictionary(this);
        }

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Creates a new dictionary that is a copy of this dictionary.
        /// </summary>
        /// <returns>
        /// The newly created copy of this dictionary.
        /// </returns>
        public object Clone()
        {
            return DeepCopy();
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format, optionally including only those keys matching the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included in the
        /// resulting string.  This parameter may be null, in which case all
        /// keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this dictionary.
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

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format.
        /// </summary>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
