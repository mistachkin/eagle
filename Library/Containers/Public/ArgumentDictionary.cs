/*
 * ArgumentDictionary.cs --
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
using System.Security.Permissions;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _Count = Eagle._Constants.Count;

using IntArgumentPair = Eagle._Interfaces.Public.IAnyPair<
    int, Eagle._Components.Public.Argument>;

using StringIntArgumentPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IAnyPair<
        int, Eagle._Components.Public.Argument>>;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Interfaces.Public.IAnyPair<int,
        Eagle._Components.Public.Argument>>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IAnyPair<int,
        Eagle._Components.Public.Argument>>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents an ordered dictionary of named arguments, keyed by
    /// argument name.  Each entry associates a name with a pair containing the
    /// zero-based ordinal position at which the argument was added and the
    /// <see cref="Argument" /> value itself.  It provides helpers for computing
    /// the minimum and maximum permitted argument counts and for detecting the
    /// presence of a variadic (final, catch-all) argument.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("79128dfe-c60b-441b-8bdf-709a6d483954")]
    public sealed class ArgumentDictionary :
            SomeDictionary, IDictionary<string, IntArgumentPair>
    {
        #region Private Data
        /// <summary>
        /// The next ordinal position to assign to an added argument; this also
        /// reflects the total number of arguments that have been added.
        /// </summary>
        private int maximumId;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ArgumentDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is a copy of the specified
        /// dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries are copied into the new instance.  This
        /// parameter may be null.
        /// </param>
        public ArgumentDictionary(
            ArgumentDictionary dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class containing an entry, with a null
        /// argument value, for each of the specified argument names.
        /// </summary>
        /// <param name="names">
        /// The argument names to add.  This parameter may be null; any null name
        /// within the collection is skipped.
        /// </param>
        public ArgumentDictionary(
            IEnumerable<string> names /* in */
            )
            : this()
        {
            if (names != null)
            {
                foreach (string name in names)
                {
                    if (name == null)
                        continue;

                    Add(name, (Argument)null);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class containing an entry, with a null
        /// argument value, for the name of each of the specified arguments.
        /// </summary>
        /// <param name="arguments">
        /// The arguments whose names are added.  This parameter may be null; any
        /// null argument within the collection is skipped.
        /// </param>
        public ArgumentDictionary(
            IEnumerable<Argument> arguments /* in */
            )
            : this()
        {
            if (arguments != null)
            {
                foreach (Argument argument in arguments)
                {
                    if (argument == null)
                        continue;

                    Add(argument.Name, (Argument)null);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized stream.
        /// </param>
        private ArgumentDictionary(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method gets the next ordinal position that will be assigned to
        /// an added argument, which also reflects the total number of arguments
        /// that have been added.
        /// </summary>
        /// <returns>
        /// The next ordinal position that will be assigned to an added argument.
        /// </returns>
        internal int GetMaximumId()
        {
            return maximumId;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name used to identify the variadic (final,
        /// catch-all) argument.
        /// </summary>
        /// <returns>
        /// The name used to identify the variadic argument.
        /// </returns>
        internal string GetVariadicName()
        {
            return TclVars.Core.Arguments;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the minimum and maximum number of values that
        /// constitute a valid set of arguments for this dictionary, taking into
        /// account any default values and any variadic argument.
        /// </summary>
        /// <param name="withNames">
        /// Non-zero if each argument is expected to be accompanied by its name,
        /// in which case the computed counts are doubled.
        /// </param>
        /// <param name="minimumCount">
        /// Upon success, receives the minimum number of values required, or the
        /// invalid count sentinel when there is no lower bound.
        /// </param>
        /// <param name="maximumCount">
        /// Upon success, receives the maximum number of values permitted, or the
        /// invalid count sentinel when there is no upper bound (e.g. due to a
        /// variadic argument).
        /// </param>
        private void GetCounts(
            bool withNames,       /* in */
            out int minimumCount, /* out */
            out int maximumCount  /* out */
            )
        {
            minimumCount = 0;
            maximumCount = 0;

            string variadicName = GetVariadicName();

            foreach (StringIntArgumentPair pair in this)
            {
                IntArgumentPair anyPair = pair.Value;

                if (anyPair == null)
                    continue;

                Argument element = anyPair.Y;

                if (element == null)
                    continue;

                if ((variadicName != null) &&
                    SharedStringOps.SystemEquals(
                        pair.Key, variadicName) &&
                    (anyPair.X == (maximumId - 1)))
                {
                    maximumCount = _Count.Invalid;
                }
                else
                {
                    if (!element.HasFlags(
                            ArgumentFlags.HasDefault, true))
                    {
                        minimumCount++;
                    }

                    if (maximumCount != _Count.Invalid)
                        maximumCount++;
                }
            }

            if ((minimumCount == 0) &&
                (maximumCount == _Count.Invalid))
            {
                minimumCount = _Count.Invalid;
            }

            if (withNames)
            {
                if (minimumCount != _Count.Invalid)
                    minimumCount *= 2;

                if (maximumCount != _Count.Invalid)
                    maximumCount *= 2;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method adds an argument with the specified name and value,
        /// assigning it the next available ordinal position.
        /// </summary>
        /// <param name="key">
        /// The name of the argument to add.
        /// </param>
        /// <param name="value">
        /// The argument value to add.  This parameter may be null.
        /// </param>
        public void Add(
            string key,    /* in */
            Argument value /* in */
            )
        {
            base.Add(key, new AnyPair<int, Argument>(maximumId, value));
            maximumId++;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified name is the name used to
        /// identify the variadic argument.
        /// </summary>
        /// <param name="key">
        /// The name to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified name is the variadic argument name; otherwise,
        /// false.
        /// </returns>
        public bool IsVariadicName(
            string key /* in: OPTIONAL */
            )
        {
            if (key == null)
                return false;

            string variadicName = GetVariadicName();

            if (variadicName == null)
                return false;

            return SharedStringOps.SystemEquals(key, variadicName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this dictionary contains a variadic
        /// argument, optionally restricted to a specific argument name.  The
        /// variadic argument, if present, must be the most recently added one.
        /// </summary>
        /// <param name="key">
        /// When non-null, the name that the variadic argument must match in
        /// order for this method to succeed.  This parameter may be null to
        /// check for any variadic argument.
        /// </param>
        /// <param name="setFlags">
        /// Non-zero to set flags during the check.  This parameter is not used.
        /// </param>
        /// <returns>
        /// True if this dictionary contains a matching variadic argument;
        /// otherwise, false.
        /// </returns>
        public bool IsVariadic(
            string key,   /* in: OPTIONAL */
            bool setFlags /* in: NOT USED */
            )
        {
            if (maximumId <= 0)
                return false;

            string variadicName = GetVariadicName();

            if ((key != null) &&
                (variadicName != null) &&
                !SharedStringOps.SystemEquals(key, variadicName))
            {
                return false;
            }

            IntArgumentPair anyPair;

            if ((variadicName == null) ||
                !this.TryGetValue(variadicName, out anyPair))
            {
                return false;
            }

            return anyPair.X == (maximumId - 1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified number of values falls
        /// within the minimum and maximum counts permitted by this dictionary.
        /// </summary>
        /// <param name="haveCount">
        /// The number of values to validate against the permitted counts.
        /// </param>
        /// <param name="withNames">
        /// Non-zero if each value is expected to be accompanied by its name,
        /// which affects the permitted counts.
        /// </param>
        /// <returns>
        /// True if the specified count is within the permitted range; otherwise,
        /// false.
        /// </returns>
        public bool IsGoodCount(
            int haveCount, /* in */
            bool withNames /* in */
            )
        {
            int minimumCount;
            int maximumCount;

            GetCounts(withNames,
                out minimumCount, out maximumCount);

            if ((minimumCount != _Count.Invalid) &&
                (haveCount < minimumCount))
            {
                return false;
            }

            if ((maximumCount != _Count.Invalid) &&
                (haveCount > maximumCount))
            {
                return false;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<StringIntArgumentPair> Overrides
        /// <summary>
        /// This method is not supported and always throws an exception; entries
        /// must be added via the strongly typed <see cref="Add(string, Argument)" />
        /// method so that an ordinal position can be assigned.
        /// </summary>
        /// <param name="item">
        /// The item that would be added.
        /// </param>
        void ICollection<StringIntArgumentPair>.Add(
            StringIntArgumentPair item /* in */
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<string, IntArgumentPair> Overrides
        /// <summary>
        /// Gets the ordinal position and argument value pair associated with the
        /// specified name; Setting this property is not supported and always
        /// throws an exception.
        /// </summary>
        /// <param name="key">
        /// The name of the argument whose pair is retrieved.
        /// </param>
        /// <returns>
        /// The ordinal position and argument value pair associated with the
        /// specified name.
        /// </returns>
        IntArgumentPair IDictionary<string, IntArgumentPair>.this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws an exception; entries
        /// must be added via the strongly typed <see cref="Add(string, Argument)" />
        /// method so that an ordinal position can be assigned.
        /// </summary>
        /// <param name="key">
        /// The name that would be added.
        /// </param>
        /// <param name="value">
        /// The ordinal position and argument value pair that would be added.
        /// </param>
        void IDictionary<string, IntArgumentPair>.Add(
            string key,           /* in */
            IntArgumentPair value /* in */
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<string, IntArgumentPair> Overrides
        /// <summary>
        /// Gets the ordinal position and argument value pair associated with the
        /// specified name; Setting this property is not supported and always
        /// throws an exception.
        /// </summary>
        /// <param name="key">
        /// The name of the argument whose pair is retrieved.
        /// </param>
        /// <returns>
        /// The ordinal position and argument value pair associated with the
        /// specified name.
        /// </returns>
        public new IntArgumentPair this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws an exception; entries
        /// must be added via the strongly typed <see cref="Add(string, Argument)" />
        /// method so that an ordinal position can be assigned.
        /// </summary>
        /// <param name="key">
        /// The name that would be added.
        /// </param>
        /// <param name="value">
        /// The ordinal position and argument value pair that would be added.
        /// </param>
        public new void Add(
            string key,           /* in */
            IntArgumentPair value /* in */
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        /// <summary>
        /// This method populates the specified serialization information with
        /// the data needed to serialize this dictionary.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized stream.
        /// </param>
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
        {
            info.AddValue("maximumId", maximumId);

            base.GetObjectData(info, context);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method builds a string representation of the argument values in
        /// this dictionary, separating successive values with the specified
        /// separator.
        /// </summary>
        /// <param name="toStringFlags">
        /// The flags used to control how each argument value is converted to its
        /// string representation.
        /// </param>
        /// <param name="separator">
        /// The string used to separate successive argument values.  This
        /// parameter may be null, in which case no separator is inserted.
        /// </param>
        /// <returns>
        /// The string representation of the argument values in this dictionary.
        /// </returns>
        public string ToRawString(
            ToStringFlags toStringFlags, /* in */
            string separator             /* in */
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (StringIntArgumentPair pair in this)
            {
                IntArgumentPair anyPair = pair.Value;

                if (anyPair == null)
                    continue;

                Argument element = anyPair.Y;

                if (element != null)
                {
                    if ((separator != null) && (result.Length > 0))
                        result.Append(separator);

                    result.Append(element.ToString(toStringFlags));
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a string representation of the argument names in
        /// this dictionary, optionally filtered by a match pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the argument names.  This parameter may be
        /// null to include all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string representation of the (optionally filtered) argument names
        /// in this dictionary.
        /// </returns>
        public string ToString(
            string pattern, /* in */
            bool noCase     /* in */
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
        /// This method builds a string representation of the argument names in
        /// this dictionary.
        /// </summary>
        /// <returns>
        /// The string representation of the argument names in this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
