/*
 * TypeChangeTypeCallbackDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    System.Type, Eagle._Components.Public.Delegates.ChangeTypeCallback>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    System.Type, Eagle._Components.Public.Delegates.ChangeTypeCallback>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps runtime types
    /// (<see cref="Type" />) to the callbacks (<see cref="ChangeTypeCallback" />)
    /// used to convert values to those types.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e5b4fd82-6b1f-4a20-8074-45629b66c413")]
    internal sealed class TypeChangeTypeCallbackDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty type/change-type-callback dictionary.
        /// </summary>
        public TypeChangeTypeCallbackDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a type/change-type-callback dictionary that is initialized
        /// with the entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public TypeChangeTypeCallbackDictionary(
            IDictionary<Type, ChangeTypeCallback> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a type/change-type-callback dictionary from previously
        /// serialized data.  This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private TypeChangeTypeCallbackDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ToString Methods
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
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
