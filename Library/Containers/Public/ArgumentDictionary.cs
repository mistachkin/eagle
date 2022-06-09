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

using IntArgumentPair = Eagle._Interfaces.Public.IAnyPair<
    int, Eagle._Components.Public.Argument>;

using StringIntArgumentPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IAnyPair<
        int, Eagle._Components.Public.Argument>>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("79128dfe-c60b-441b-8bdf-709a6d483954")]
    public sealed class ArgumentDictionary :
            Dictionary<string, IntArgumentPair>,
            IDictionary<string, IntArgumentPair>
    {
        #region Private Data
        private int maximumId;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ArgumentDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private ArgumentDictionary(
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

        #region Private Methods
        internal int GetMaximumId()
        {
            return maximumId;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Add(
            string key,
            Argument value
            )
        {
            base.Add(key, new AnyPair<int, Argument>(maximumId, value));
            maximumId++;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsVariadic(
            bool setFlags /* NOT USED */
            )
        {
            if (maximumId <= 0)
                return false;

            IntArgumentPair anyPair;

            if (!this.TryGetValue(TclVars.Core.Arguments, out anyPair))
                return false;

            return anyPair.X == (maximumId - 1);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<StringIntArgumentPair> Overrides
        void ICollection<StringIntArgumentPair>.Add(
            StringIntArgumentPair item
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<string, IntArgumentPair> Overrides
        IntArgumentPair IDictionary<string, IntArgumentPair>.this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        void IDictionary<string, IntArgumentPair>.Add(
            string key,
            IntArgumentPair value
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<string, IntArgumentPair> Overrides
        public new IntArgumentPair this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Add(
            string key,
            IntArgumentPair value
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
            )
        {
            info.AddValue("maximumId", maximumId);

            base.GetObjectData(info, context);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToRawString(
            ToStringFlags toStringFlags,
            string separator
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

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

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
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
