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
    [ObjectId("55fbf1be-3c15-470c-acc0-e3ef767c5e54")]
    internal sealed class InterpreterStackList :
            StackList<IAnyPair<Interpreter, IClientData>>, ICloneable
    {
        public InterpreterStackList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterStackList(
            IEnumerable<IAnyPair<Interpreter, IClientData>> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<IAnyPair<Interpreter, IClientData>>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
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

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new InterpreterStackList(this);
        }
        #endregion
    }
}
