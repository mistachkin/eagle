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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c153b7e8-af76-42e9-aab2-3c1b9b227f32")]
    internal sealed class ScriptLocationIntDictionary
            : Dictionary<IScriptLocation, int>
    {
        #region Private Constants
        private static readonly IScriptLocation AnyLineLocation =
            ScriptLocation.Create(null, null, Parser.AnyLine, Parser.AnyLine,
            false);

        ///////////////////////////////////////////////////////////////////////

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
        public ScriptLocationIntDictionary() /* NOT USED */
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
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
                Characters.Space.ToString(), null, false);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return KeysAndValuesToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
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
