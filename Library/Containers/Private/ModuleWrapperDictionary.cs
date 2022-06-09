/*
 * ModuleWrapperDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

#if DEAD_CODE
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("10badaa0-3d77-4dc1-9e9c-1a79467113ec")]
    internal sealed class ModuleWrapperDictionary : WrapperDictionary<string, _Wrappers._Module>
    {
        public ModuleWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private ReturnCode ToList(
            string pattern,
            bool noCase,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList = new StringList(this.Keys);

            if (list == null)
                list = new StringList();

            return GenericOps<string>.FilterList(inputList, list, Index.Invalid,
                Index.Invalid, ToStringFlags.None, pattern, noCase, ref error);
        }
#endif
        #endregion
    }
}
