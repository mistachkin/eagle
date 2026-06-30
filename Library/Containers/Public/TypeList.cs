/*
 * TypeList.cs --
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
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a list of types (<see cref="Type" />).
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6002e634-1f30-4e30-b147-6aab6fab3c6a")]
    public sealed class TypeList : List<Type>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty list of types.
        /// </summary>
        public TypeList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of types that contains the types copied from the
        /// specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of types whose elements are copied into the new
        /// list.
        /// </param>
        public TypeList(
            IEnumerable<Type> collection
            )
            : base(collection)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty list of types that has the specified initial
        /// capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of types the new list can initially store without
        /// resizing.
        /// </param>
        internal TypeList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of types from the parameter types of the specified
        /// collection of parameter information.
        /// </summary>
        /// <param name="collection">
        /// The collection of parameter information whose parameter types are
        /// added to the new list.  This parameter may be null.
        /// </param>
        internal TypeList(
            IEnumerable<ParameterInfo> collection
            )
            : base()
        {
            /* IGNORED */
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method determines whether two lists of types are equal,
        /// comparing them by reference and then element by element.
        /// </summary>
        /// <param name="types1">
        /// The first list of types to compare.  This parameter may be null.
        /// </param>
        /// <param name="types2">
        /// The second list of types to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two lists are the same reference or contain the same
        /// types in the same order; otherwise, false.
        /// </returns>
        internal static bool Equals(
            TypeList types1,
            TypeList types2
            )
        {
            if (Object.ReferenceEquals(types1, types2))
                return true;

            if ((types1 == null) || (types2 == null))
                return false;

            if (types1.Count != types2.Count)
                return false;

            for (int index = 0; index < types1.Count; index++)
                if (types1[index] != types2[index])
                    return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Methods
        /// <summary>
        /// This method adds the parameter types of the specified collection of
        /// parameter information to this list.
        /// </summary>
        /// <param name="collection">
        /// The collection of parameter information whose parameter types are
        /// added to this list.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The number of types added to this list.
        /// </returns>
        private int Add(
            IEnumerable<ParameterInfo> collection
            )
        {
            int count = 0;

            if (collection != null)
            {
                foreach (ParameterInfo item in collection)
                {
                    if (item == null)
                        continue;

                    this.Add(item.ParameterType);
                    count++;
                }
            }

            return count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method formats the types in this list as a list of strings,
        /// optionally filtering them by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the type names included in the
        /// result.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <param name="fullName">
        /// Non-zero to use the full name of each type.
        /// </param>
        /// <param name="qualified">
        /// Non-zero to use the assembly-qualified name of each type.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of formatted type names; a new list
        /// is created when this parameter is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ToList(
            string pattern,
            bool noCase,
            bool fullName,
            bool qualified,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList = new StringList();

            foreach (Type type in this)
                if (type != null)
                    inputList.Add(FormatOps.QualifiedAndOrFullName(
                        type, fullName, qualified, false));

            if (list == null)
                list = new StringList();

            return GenericOps<string>.FilterList(
                inputList, list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this list, with the
        /// types separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the types included in the
        /// result.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <param name="qualified">
        /// Non-zero to use the qualified name of each type.
        /// </param>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase,
            bool qualified
            )
        {
            if (qualified)
            {
                StringList list = new StringList();

                foreach (Type type in this)
                    if (type != null)
                        list.Add(FormatOps.QualifiedName(type));

                return ParserOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.SpaceString, pattern, noCase);
            }
            else
            {
                return ParserOps<Type>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.SpaceString, pattern, noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this list, with the
        /// types separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the types included in the
        /// result.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(pattern, noCase, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this list, with the
        /// types separated by spaces.
        /// </summary>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
