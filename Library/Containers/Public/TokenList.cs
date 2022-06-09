/*
 * TokenList.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("7b5d31ce-0b64-45e4-ba5d-6073e6104e7b")]
    public sealed class TokenList : List<IToken>
    {
        public TokenList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public TokenList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public TokenList(
            IEnumerable<IToken> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int Last
        {
            get { return (this.Count > 0) ? this.Count - 1 : Index.Invalid; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void Add(
            IToken item,
            IParseState parseState
            )
        {
            if ((item != null) && (parseState != null))
                item.EndLine = parseState.CurrentLine;

            this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void Insert(
            int index,
            IToken item,
            IParseState parseState
            )
        {
            if ((item != null) && (parseState != null))
                item.EndLine = parseState.CurrentLine;

            this.Insert(index, item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void InsertRange(
            int index,
            IEnumerable<IToken> collection,
            IParseState parseState
            )
        {
            if (parseState != null)
                foreach (IToken item in collection)
                    if (item != null)
                        item.EndLine = parseState.CurrentLine;

            this.InsertRange(index, collection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void RemoveAt(
            int index,
            int count
            )
        {
            while (count-- > 0)
                this.RemoveAt(index);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            for (int index = 0; index < this.Count; index++)
            {
                result.Append(this[index].ToString());

                if ((index + 1) < this.Count)
                    result.Append(Characters.LineFeed);
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
