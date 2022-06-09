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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("18aaa0f8-4a6b-45f5-8680-0fccbca53f81")]
    internal sealed class InterpreterVariableContextDictionary
            : Dictionary<IInterpreter, IVariableContext>
    {
        public InterpreterVariableContextDictionary()
            : base(new _Comparers._Interpreter())
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            InterpreterList list = new InterpreterList(this.Keys);

            return ParserOps<IInterpreter>.ListToString(
                list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(),
                pattern, noCase);
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
