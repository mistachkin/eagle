/*
 * ScriptLocationIntDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    Eagle._Interfaces.Public.IScriptLocation, int>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    Eagle._Interfaces.Public.IScriptLocation, int>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps script locations to integer
    /// hit counts.  It is used to implement script-location breakpoints,
    /// tracking how many times each location has been matched, and supports the
    /// special "any line" and "no line" sentinel locations.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c153b7e8-af76-42e9-aab2-3c1b9b227f32")]
    internal sealed class ScriptLocationIntDictionary : SomeDictionary
    {
        #region Private Constants
        /// <summary>
        /// The sentinel script location used to indicate that any line should
        /// be matched.
        /// </summary>
        private static readonly IScriptLocation AnyLineLocation =
            ScriptLocation.Create(null, null, Parser.AnyLine, Parser.AnyLine,
            false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The sentinel script location used to indicate that no line should be
        /// matched.
        /// </summary>
        private static readonly IScriptLocation NoLineLocation =
            ScriptLocation.Create(null, null, Parser.NoLine, Parser.NoLine,
            false);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        //
        // HACK: This public constructor is only required for use with
        //       PathDictionary<T> via the BreakpointDictionary class.
        //
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ScriptLocationIntDictionary() /* NOT USED */
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class that initially contains the
        /// specified script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when adding the location.
        /// </param>
        /// <param name="location">
        /// The script location to add to the new dictionary.
        /// </param>
        private ScriptLocationIntDictionary(
            Interpreter interpreter,
            IScriptLocation location
            )
            : this()
        {
            Set(interpreter, location);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new instance of this class that initially
        /// contains the specified script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when adding the location.
        /// </param>
        /// <param name="location">
        /// The script location to add to the new dictionary.
        /// </param>
        /// <returns>
        /// The newly created dictionary instance.
        /// </returns>
        public static ScriptLocationIntDictionary Create(
            Interpreter interpreter,
            IScriptLocation location
            )
        {
            return new ScriptLocationIntDictionary(interpreter, location);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private ScriptLocationIntDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method produces a string containing the keys and their
        /// associated hit-count values for the entries that match the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the entries that are included in the
        /// result.  This parameter may be null, in which case all entries are
        /// included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The matching keys and values formatted as a string.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<IScriptLocation, int>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method produces a string containing the keys and their
        /// associated hit-count values for the entries that match the specified
        /// regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to filter the entries that are
        /// included in the result.  This parameter may be null, in which case
        /// all entries are included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching the pattern.
        /// </param>
        /// <returns>
        /// The matching keys and values formatted as a string.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<IScriptLocation, int>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all of the keys and their
        /// associated hit-count values.
        /// </summary>
        /// <returns>
        /// The keys and values of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return KeysAndValuesToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method adds the specified script location to the dictionary,
        /// initializing its hit count to zero if it is not already present.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when adding the location.
        /// </param>
        /// <param name="location">
        /// The script location to add to the dictionary.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private ReturnCode Set(
            Interpreter interpreter,
            IScriptLocation location
            )
        {
            bool match = false;
            Result error = null;

            return Set(interpreter, location, ref match, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public "IBreakpoint" Members
        /// <summary>
        /// This method determines whether the specified script location matches
        /// any of the locations in the dictionary, incrementing the hit count
        /// of a matching location.  The special "no line" and "any line"
        /// sentinel locations are honored.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when comparing the locations.
        /// </param>
        /// <param name="location">
        /// The script location to look for in the dictionary.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the location matched an entry
        /// in the dictionary; otherwise, it will be false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public ReturnCode Match(
            Interpreter interpreter,
            IScriptLocation location,
            ref bool match,
            ref Result error
            )
        {
            if (location == null)
            {
                error = "invalid script location";
                return ReturnCode.Error;
            }

            if (this.ContainsKey(NoLineLocation))
            {
                match = false;
                return ReturnCode.Ok;
            }

            if (this.ContainsKey(AnyLineLocation))
            {
                this[AnyLineLocation]++; // NOTE: Another hit.

                match = true;
                return ReturnCode.Ok;
            }

            //
            // BUGBUG: This does not work because ParseToken does
            //         not currently implement IEqualityComparer.
            //
            // if (this.ContainsKey(location))
            // {
            //     match = true;
            //     return ReturnCode.Ok;
            // }

            foreach (KeyValuePair<IScriptLocation, int> pair in this)
            {
                if (ScriptLocation.Match(
                        interpreter, location, pair.Key, true))
                {
                    this[pair.Key]++; // NOTE: Another hit.

                    match = true;
                    return ReturnCode.Ok;
                }
            }

            match = false;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified script location from the
        /// dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when removing the location.
        /// </param>
        /// <param name="location">
        /// The script location to remove from the dictionary.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the location was not present
        /// in the dictionary (i.e. nothing was removed); otherwise, it will be
        /// false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public ReturnCode Clear(
            Interpreter interpreter,
            IScriptLocation location,
            ref bool match,
            ref Result error
            )
        {
            if (location == null)
            {
                error = "invalid script location";
                return ReturnCode.Error;
            }

            match = !this.Remove(location);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified script location to the dictionary,
        /// initializing its hit count to zero if it is not already present.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when adding the location.
        /// </param>
        /// <param name="location">
        /// The script location to add to the dictionary.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the location was already
        /// present in the dictionary; otherwise, it will be false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public ReturnCode Set(
            Interpreter interpreter,
            IScriptLocation location,
            ref bool match,
            ref Result error
            )
        {
            if (location == null)
            {
                error = "invalid script location";
                return ReturnCode.Error;
            }

            if (this.ContainsKey(location))
            {
                match = true; // NOTE: It was already found.
                return ReturnCode.Ok;
            }

            this.Add(location, 0);

            match = false; // NOTE: It was not already found.
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list containing the script locations in this
        /// dictionary along with their associated hit counts.
        /// </summary>
        /// <returns>
        /// The list of script locations and their hit counts.
        /// </returns>
        public IStringList ToList()
        {
            IStringList list = new StringPairList();

            foreach (KeyValuePair<IScriptLocation, int> pair in this)
            {
                list.Add(pair.Key.ToList());
                list.Add("HitCount", pair.Value.ToString());
            }

            return list;
        }
        #endregion
    }
}
