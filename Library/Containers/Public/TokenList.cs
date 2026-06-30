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
    /// <summary>
    /// This class represents a list of script tokens (<see cref="IToken" />),
    /// each describing a lexical or syntactic component produced while parsing
    /// a script.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("7b5d31ce-0b64-45e4-ba5d-6073e6104e7b")]
    public sealed class TokenList : List<IToken>
    {
        /// <summary>
        /// Constructs an empty list of tokens.
        /// </summary>
        public TokenList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty list of tokens that has the specified initial
        /// capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of tokens the new list can initially store without
        /// resizing.
        /// </param>
        public TokenList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of tokens that contains the tokens copied from the
        /// specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of tokens whose elements are copied into the new
        /// list.
        /// </param>
        public TokenList(
            IEnumerable<IToken> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the index of the last token in this list, or an invalid index
        /// if the list is empty.
        /// </summary>
        public int Last
        {
            get { return (this.Count > 0) ? this.Count - 1 : Index.Invalid; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a token to the end of this list, optionally setting
        /// its ending line from the supplied parser state.
        /// </summary>
        /// <param name="item">
        /// The token to add to this list.  This parameter may be null.
        /// </param>
        /// <param name="parseState">
        /// The current parser state used to set the ending line of the token,
        /// if both it and the token are non-null.  This parameter may be null.
        /// </param>
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

        /// <summary>
        /// This method inserts a token at the specified index in this list,
        /// optionally setting its ending line from the supplied parser state.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the token is inserted.
        /// </param>
        /// <param name="item">
        /// The token to insert into this list.  This parameter may be null.
        /// </param>
        /// <param name="parseState">
        /// The current parser state used to set the ending line of the token,
        /// if both it and the token are non-null.  This parameter may be null.
        /// </param>
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

        /// <summary>
        /// This method inserts a range of tokens at the specified index in this
        /// list, optionally setting the ending line of each token from the
        /// supplied parser state.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the tokens are inserted.
        /// </param>
        /// <param name="collection">
        /// The collection of tokens to insert into this list.
        /// </param>
        /// <param name="parseState">
        /// The current parser state used to set the ending line of each
        /// non-null token, if it is non-null.  This parameter may be null.
        /// </param>
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

        /// <summary>
        /// This method removes a consecutive range of tokens from this list,
        /// starting at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the first token to remove.
        /// </param>
        /// <param name="count">
        /// The number of tokens to remove starting at the specified index.
        /// </param>
        public void RemoveAt(
            int index,
            int count
            )
        {
            while (count-- > 0)
                this.RemoveAt(index);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this list, with the
        /// tokens separated by line feeds.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the tokens included in the
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
            StringBuilder result = StringBuilderFactory.Create();

            for (int index = 0; index < this.Count; index++)
            {
                result.Append(this[index].ToString());

                if ((index + 1) < this.Count)
                    result.Append(Characters.LineFeed);
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this list, with the
        /// tokens separated by line feeds.
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
