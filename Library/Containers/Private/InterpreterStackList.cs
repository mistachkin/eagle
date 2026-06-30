/*
 * InterpreterStackList.cs --
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
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a stack of interpreters, where each entry pairs an
    /// interpreter with its associated client data.  It adds helpers for
    /// searching the stack, copying it, and producing a string form of its
    /// contents.
    /// </summary>
    [ObjectId("55fbf1be-3c15-470c-acc0-e3ef767c5e54")]
    internal sealed class InterpreterStackList :
            StackList<IAnyPair<Interpreter, IClientData>>
#if DEAD_CODE
            , ICloneable
#endif
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public InterpreterStackList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of interpreter and client data pairs whose elements
        /// are copied into the new stack.
        /// </param>
        public InterpreterStackList(
            IEnumerable<IAnyPair<Interpreter, IClientData>> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the specified interpreter is present
        /// anywhere on the stack.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to search for.
        /// </param>
        /// <returns>
        /// True if the interpreter was found on the stack; otherwise, false.
        /// </returns>
        public bool ContainsInterpreter(
            Interpreter interpreter
            )
        {
            foreach (IAnyPair<Interpreter, IClientData> anyPair in this)
            {
                if (anyPair == null)
                    continue;

                if (Object.ReferenceEquals(anyPair.X, interpreter))
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new stack that contains the same interpreter
        /// and client data pairs as this stack.
        /// </summary>
        /// <returns>
        /// The newly created copy of this stack.
        /// </returns>
        public InterpreterStackList DeepCopy()
        {
            return new InterpreterStackList(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the entries of the stack
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the entries that are included in the
        /// result.  This parameter may be null, in which case all entries are
        /// included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The matching entries of the stack formatted as a string.
        /// </returns>
        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<IAnyPair<Interpreter, IClientData>>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all of the entries of the
        /// stack.
        /// </summary>
        /// <returns>
        /// The entries of the stack formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method creates a new stack that is a copy of this stack.
        /// </summary>
        /// <returns>
        /// The newly created copy of this stack.
        /// </returns>
        public object Clone()
        {
            return DeepCopy();
        }
#endif
        #endregion
        #endregion
    }
}
