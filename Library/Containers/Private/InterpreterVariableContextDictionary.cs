/*
 * InterpreterVariableContextDictionary.cs --
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
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    Eagle._Interfaces.Public.IInterpreter,
    Eagle._Interfaces.Private.IVariableContext>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    Eagle._Interfaces.Public.IInterpreter,
    Eagle._Interfaces.Private.IVariableContext>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps interpreters to their
    /// associated variable context.  Interpreter keys are compared using the
    /// <c>_Comparers._Interpreter</c> equality comparer.
    /// </summary>
    [ObjectId("18aaa0f8-4a6b-45f5-8680-0fccbca53f81")]
    internal sealed class InterpreterVariableContextDictionary : SomeDictionary
    {
        /// <summary>
        /// Constructs an empty instance of this class that compares its
        /// interpreter keys using the interpreter equality comparer.
        /// </summary>
        public InterpreterVariableContextDictionary()
            : base(new _Comparers._Interpreter())
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the entry with the specified interpreter key
        /// from the dictionary, returning any value that was associated with
        /// it.
        /// </summary>
        /// <param name="key">
        /// The interpreter key of the entry to remove.
        /// </param>
        /// <param name="value">
        /// Upon return, receives the variable context that was associated with
        /// the specified key, or null if the key was not present.
        /// </param>
        /// <returns>
        /// True if the entry was present and removed; otherwise, false.
        /// </returns>
        public bool RemoveAndReturn(
            IInterpreter key,
            out IVariableContext value
            )
        {
            /* IGNORED */
            base.TryGetValue(key, out value);

            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the interpreter keys of
        /// the dictionary that match the specified pattern.
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
        /// The list of matching interpreter keys formatted as a string.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            InterpreterList list = new InterpreterList(this.Keys);

            return ParserOps<IInterpreter>.ListToString(
                list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString,
                pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all of the interpreter
        /// keys of the dictionary.
        /// </summary>
        /// <returns>
        /// The interpreter keys of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
