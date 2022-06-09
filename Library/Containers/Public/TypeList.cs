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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6002e634-1f30-4e30-b147-6aab6fab3c6a")]
    public sealed class TypeList : List<Type>
    {
        #region Public Constructors
        public TypeList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        internal TypeList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
                    Characters.Space.ToString(), pattern, noCase);
            }
            else
            {
                return ParserOps<Type>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), pattern, noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
