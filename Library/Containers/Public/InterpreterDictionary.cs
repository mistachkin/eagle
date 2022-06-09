/*
 * InterpreterDictionary.cs --
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    [ObjectId("59be82f4-ecdc-4faa-96ff-4405dddcfdf5")]
    public sealed class InterpreterDictionary :
            Dictionary<string, Interpreter>, ICloneable
    {
        public InterpreterDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDictionary(
            IDictionary<string, Interpreter> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDictionary(
            IEnumerable<Interpreter> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Interpreter> collection
            )
        {
            foreach (Interpreter item in collection)
                this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            Interpreter interpreter
            )
        {
            this.Add(interpreter.IdNoThrow.ToString(), interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Remove(
            Interpreter interpreter
            )
        {
            return this.Remove(interpreter.IdNoThrow.ToString());
        }

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new InterpreterDictionary(this);
        }
        #endregion

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

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
