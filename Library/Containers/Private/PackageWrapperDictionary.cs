/*
 * PackageWrapperDictionary.cs --
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
using Eagle._Containers.Public;

using PackagePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Package>;

namespace Eagle._Containers.Private
{
    [ObjectId("cea47a6d-b9e1-4bdd-b1c7-1dc0be1967af")]
    internal sealed class PackageWrapperDictionary :
            WrapperDictionary<string, _Wrappers.Package>
    {
        #region Public Constructors
        public PackageWrapperDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public StringList NamesAndVersions(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = null;

            foreach (PackagePair pair in this)
            {
                if (list == null)
                    list = new StringList();

                list.Add(pair.Key);

                _Wrappers.Package wrapper = pair.Value;

                if (wrapper != null)
                {
                    VersionStringDictionary ifNeeded = wrapper.IfNeeded;

                    if (ifNeeded != null)
                    {
                        list.Add(ifNeeded.ToString(pattern, noCase));
                        continue;
                    }
                }

                list.Add((string)null);
            }

            return list;
        }
        #endregion
    }
}
