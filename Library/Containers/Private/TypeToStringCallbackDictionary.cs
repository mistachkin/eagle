/*
 * TypeToStringCallbackDictionary.cs --
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
    System.Type, Eagle._Components.Public.Delegates.ToStringCallback>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    System.Type, Eagle._Components.Public.Delegates.ToStringCallback>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps types to the callback
    /// delegates used to produce the string representations of objects of
    /// those types.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e2e05049-0fe5-48d1-836a-4fd4562c4adb")]
    internal sealed class TypeToStringCallbackDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public TypeToStringCallbackDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the type-to-callback
        /// mappings copied from another dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary containing the initial type-to-callback mappings to
        /// copy into the newly created dictionary.
        /// </param>
        public TypeToStringCallbackDictionary(
            IDictionary<Type, ToStringCallback> dictionary
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
        /// Constructs an instance of this class from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary being
        /// constructed.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized data associated with
        /// the dictionary being constructed.
        /// </param>
        private TypeToStringCallbackDictionary(
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
        /// This method builds a string representation of the type names in this
        /// dictionary, optionally filtered by a match pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the type names.  This parameter may be
        /// null to include all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string representation of the (optionally filtered) type names in
        /// this dictionary.
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
        /// This method builds a string representation of the type names in this
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The string representation of the type names in this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
